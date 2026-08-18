# A46 — Single-active TRADE session ownership

**Artifact:** `D:\Prop\reports\swarm\20260818\A46_session_ownership.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Primary section:** §28 FIX Session Ownership  
**Supporting sections:** §1.9, §5 (Redis coordination), §27, §32–§34, §40–§45, §52, §55, §57–§58, §62, §70, §71, §72.9–12  
**Related swarm notes:** `A03_infrastructure_audit.md`, `A05_fix_ctrader_audit.md`, `A08_fix_worker_audit.md`, `A23_risk_engine_spec.md`, `A25_fix_session_spec.md`, `A26_dashboard_api_spec.md`, `A27_test_inventory.md`, `A32_ctrader_fix_specification.md`  
**Date:** 2026-08-18  
**Status:** binding implementation spec — **no product source modified**  
**Chosen §28 mechanism:** Redis lease **with fencing token**, and **PostgreSQL as authority**  
**Safety stance:** **fail closed**

This document selects one of the four §28 options and specifies it completely. It does **not** implement code. It supersedes `A25` §4.3’s “Postgres-first, no Redis required” *recommendation* for this repo: architecture §5 already assigns Redis to “distributed execution-session ownership”, Infrastructure already references `StackExchange.Redis` 2.8.0, and this task requires the Redis+fence path. Postgres remains the **only** authority for execution state. Redis is liveness, not the book.

---

## 0. Binding invariant

For **one** cTrader trading account, **two production instances must never simultaneously own the same TRADE session.**

Architecture §1.9 / §28. Official cTrader FAQ (quoted via `A05` / `A25`): multiple simultaneous FIX connections cause the server to **copy every report to every active connection**. Two TRADE sockets on account `1369850` therefore means:

- two copies of every `ExecutionReport` (`35=8`),
- two possible senders of `NewOrderSingle` (`35=D`),
- duplicate fills, unknown mapping, and a broken destination book.

QUOTE and TRADE stay independent session objects (§27). This lease is **per session qualifier**, not per process. Owning QUOTE does **not** authorize TRADE sends.

The FIX TCP socket is **not** the authority. Process memory is **not** the authority. Redis is **not** the authority for orders, positions, balances, intents, or `READY_FOR_EXECUTION` (§5, §62).

If any required proof is missing, the instance is **not** the owner and **must not** send.

---

## 1. Why both Redis and the database

§28 lists four legal mechanisms. This design uses **two layers at once**:

| Layer | Job | Not its job |
|---|---|---|
| PostgreSQL | Mint the monotonic **fencing token**. Record owner, epoch, readiness, and every execution write. Reject stale-token writes. Survive process death. | Detect a live-but-partitioned owner faster than a TTL. |
| Redis lease | Fast **liveness**. Exclusive bind of “who may hold the TRADE socket **now**”. Auto-expire a dead process. | Store orders / positions / balances. Authorize a send by itself. Outlive a DB outage. |

A Redis-only lock (no fence) is **unsafe**: a GC pause or partition expires the key, a second instance logs on, the first wakes and still has a socket. Both send.

A DB-only lease (no Redis) can work (A25 §4.3) but cannot evict a wedged owner faster than `leased_until`, and this repo already allocated Redis for this exact job. Advisory locks die with the session — useful as a *third* belt, never as the only fence.

A deployment singleton is **not sufficient**. A second replica, a laptop diagnostic, or a forgotten staging box with the live password bypasses replica-count. §71 also forbids cross-region active-active FIX.

**Chosen conjunction (fail closed):**

```text
may_own_trade_socket =
    Postgres row says I am owner
    AND my fencing_token == Postgres current token
    AND Redis key exists
    AND Redis value.instance_id == me
    AND Redis value.fencing_token == Postgres current token
    AND Redis PTTL >= min_remaining

may_send_application_message =
    may_own_trade_socket
    AND persist-before-send succeeded under that same token
    AND session state + flags + risk gates in §10
```

If Redis is down, **do not** fall back to “DB lease only” in production. Config `TRADE_OWNERSHIP_ALLOW_DB_ONLY` defaults **false** and must stay false. If Postgres is down, **do not** send from Redis or memory (§62).

Do **not** implement Redlock. One Redis primary is enough because we fail closed when Redis cannot be written. `IDistributedCache` is the wrong abstraction for fencing (A03).

---

## 2. Identity and keying

### 2.1 Ownership unit

One lease row and one Redis key per:

```text
(execution_venue_id, destination_account_id, session_qualifier)
```

`session_qualifier` is `TRADE` or `QUOTE` (`FixSessionQualifier`). This document’s hard requirement is **TRADE**. QUOTE uses the same shape (see §12) so sequence files and quote freshness are not dual-written; it is **never** a substitute for the TRADE lease.

Do **not** lease “the fix-worker process” or “the host”. Two workers on one host are still two owners. One worker managing QUOTE+TRADE holds **two** leases.

### 2.2 Stable session key

```text
{environment}:{broker_uid}:{account_id}:{qualifier}
```

Production example for the Pepperstone venue in architecture §25:

```text
live:pepperstone:1369850:TRADE
live:pepperstone:1369850:QUOTE
```

`environment` / `broker_uid` / `account_id` come from the issued `SenderCompID` (`<Environment>.<BrokerUID>.<Trader Login>`), not from a guessed instrument id.

### 2.3 Instance identity

`owner_instance_id` is a UUID minted **once per process boot**, plus audit fields:

```text
instance_id          = UUIDv4 at process start (never reused across restarts)
hostname
pid
started_at           = UTC
lease_id             = UUIDv4 per successful acquire (new on every epoch)
```

A restart is a **new** instance. It must acquire a **new** fencing token. It must not reuse the previous process’s Redis value.

### 2.4 Redis key

```text
ti:fix:lease:{session_key}
```

Example: `ti:fix:lease:live:pepperstone:1369850:TRADE`

Prefix `ti:fix:lease:` is reserved for this protocol. The Redis façade must **not** expose a generic `StringSet` that can write `order:`, `position:`, or `balance:` documents (A03 §7.1).

---

## 3. Fencing token

The fencing token is a **monotonic `bigint`**, starting at `0` on row insert, **never reused, never decremented**.

**Postgres is the only mint.** Redis only **echoes** the token that Postgres just granted.

Rules:

1. Every successful acquire increments the token by exactly `1` in the same `UPDATE … RETURNING` that writes the new owner.
2. Release / yield / fault **also increments** the token (or increments and nulls the owner). The outgoing owner is fenced even if it still has a TCP socket.
3. Redis-bind failure after a successful mint **must** increment again and clear the owner (see §7.2). Do not leave a zombie “owner” that never bound the lease.
4. Every durable execution mutation that can affect the venue book carries `fencing_token` and is rejected if it does not equal the **current** lease token.
5. A fenced writer **drops the send**. It does not send “anyway.” It does not reconnect. It does not mint a new `ClOrdID` for the same intent.

Value object (planned): `FencingToken` — `readonly record struct` wrapping `long`, comparable, never defaulted to `0` on a send path (`0` means “never acquired”).

---

## 4. Redis lease contract

### 4.1 Value

JSON, schema versioned, no secrets:

```json
{
  "v": 1,
  "session_key": "live:pepperstone:1369850:TRADE",
  "instance_id": "8f2c0a6e-…",
  "lease_id": "c91d2b10-…",
  "fencing_token": 42,
  "acquired_at": "2026-08-18T12:00:00.000Z",
  "hostname": "fix-worker-a",
  "pid": 4120
}
```

Never put FIX passwords, account passwords, or Redis passwords in the value or in logs (§55, §57).

### 4.2 TTL and renew (defaults; all configurable)

| Knob | Production default | Rule |
|---|---|---|
| `TRADE_LEASE_TTL_MS` | `10000` | Redis `PX` and the DB `leased_until` horizon |
| `TRADE_LEASE_RENEW_MS` | `3000` | ≤ ⅓ of TTL |
| `TRADE_LEASE_MIN_REMAINING_MS` | `2000` | Send / Logon / reconnect refused below this |
| `TRADE_LEASE_ACQUIRE_BACKOFF_MS` | `1000` (cap `5000`) | Standby retry |
| Steal grace | **0** | Fail closed; no “maybe still mine” window |

Local clocks are used **only** to schedule the next renew attempt (with jitter). Liveness is Redis `PTTL`. Expiry for steal is `now()` **on Postgres** **and** Redis key absent. Do not steal because the local clock says so.

### 4.3 Lua — acquire / bind (after Postgres mint)

`KEYS[1]` = lease key  
`ARGV[1]` = payload JSON  
`ARGV[2]` = TTL ms  
`ARGV[3]` = `instance_id`  
`ARGV[4]` = `fencing_token` (string of the minted value)

```lua
-- bind only if absent, or same instance with token <= minted (reclaim after retry)
local function token_of(raw)
  return tonumber(cjson.decode(raw)["fencing_token"])
end

if redis.call("EXISTS", KEYS[1]) == 0 then
  redis.call("SET", KEYS[1], ARGV[1], "PX", ARGV[2])
  return 1
end

local raw = redis.call("GET", KEYS[1])
if not raw then
  redis.call("SET", KEYS[1], ARGV[1], "PX", ARGV[2])
  return 1
end

local cur = cjson.decode(raw)
if cur["instance_id"] == ARGV[3] and token_of(raw) <= tonumber(ARGV[4]) then
  redis.call("SET", KEYS[1], ARGV[1], "PX", ARGV[2])
  return 1
end

return 0
```

A **different** instance must never overwrite a live key, even with a higher token. Higher token + live Redis key is a **split-brain alarm** (§11.3): the new minter must roll back the DB claim and stay standby until the key expires.

### 4.4 Lua — renew

```lua
local raw = redis.call("GET", KEYS[1])
if not raw then return 0 end
local cur = cjson.decode(raw)
if cur["instance_id"] == ARGV[1] and tonumber(cur["fencing_token"]) == tonumber(ARGV[2]) then
  redis.call("PEXPIRE", KEYS[1], ARGV[3])
  return 1
end
return 0
```

`PEXPIRE` only. Do not `SET` on renew (avoids clobbering a newer bind if the check races). Return `0` ⇒ **lost lease** (§8).

### 4.5 Lua — release

```lua
local raw = redis.call("GET", KEYS[1])
if not raw then return 1 end
local cur = cjson.decode(raw)
if cur["instance_id"] == ARGV[1] and tonumber(cur["fencing_token"]) == tonumber(ARGV[2]) then
  redis.call("DEL", KEYS[1])
  return 1
end
return 0
```

Never `DEL` without matching `instance_id` **and** token. A fenced loser that `DEL`s the winner’s key is a P0 bug.

### 4.6 What Redis must not contain

Orders, positions, balances, `ClOrdID` allocations, `READY_FOR_EXECUTION`, risk decisions, destination quotes used as the **only** pre-trade price. Those stay in PostgreSQL (quotes may be cached with TTL + DB rebuild; not this lease).

---

## 5. Database as authority

### 5.1 Table `fix_session_leases`

One row per ownership unit. Created by migration (not `EnsureCreated`). Architecture §44/§45 already list `fix_sessions` / `fix_session_events`; this table is the lease/epoch store those sessions point at. `A25` named it `fix_session_leases` — keep that name.

```sql
CREATE TABLE fix_session_leases (
    id                    uuid PRIMARY KEY,
    execution_venue_id    uuid NOT NULL,
    destination_account   text NOT NULL,
    session_qualifier     text NOT NULL,          -- 'TRADE' | 'QUOTE'
    session_key           text NOT NULL,          -- unique, see §2.2
    fencing_token         bigint NOT NULL DEFAULT 0,
    owner_instance_id     uuid NULL,
    owner_hostname        text NULL,
    owner_pid             integer NULL,
    lease_id              uuid NULL,
    ownership_state       text NOT NULL,          -- §6.1
    leased_until          timestamptz NULL,
    acquired_at           timestamptz NULL,
    last_renew_at         timestamptz NULL,
    last_redis_ok_at      timestamptz NULL,
    ready_for_execution   boolean NOT NULL DEFAULT false,
    epoch_reason          text NULL,
    row_version           integer NOT NULL DEFAULT 0,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_fix_session_leases_key UNIQUE (session_key),
    CONSTRAINT uq_fix_session_leases_unit
        UNIQUE (execution_venue_id, destination_account, session_qualifier),
    CONSTRAINT ck_fix_session_leases_qual
        CHECK (session_qualifier IN ('TRADE', 'QUOTE')),
    CONSTRAINT ck_fix_session_leases_token
        CHECK (fencing_token >= 0)
);

CREATE INDEX ix_fix_session_leases_owner
    ON fix_session_leases (owner_instance_id)
    WHERE owner_instance_id IS NOT NULL;
```

Seed the TRADE (and QUOTE) row at venue provision time with `fencing_token = 0`, `ownership_state = 'STANDBY'`, `ready_for_execution = false`. Do not lazily insert on the send path.

### 5.2 Table `fix_session_ownership_events`

Append-only. Also emit a `fix_session_events` row for the dashboard (A26) with **no** secrets.

```sql
CREATE TABLE fix_session_ownership_events (
    id                 uuid PRIMARY KEY,
    session_key        text NOT NULL,
    fencing_token      bigint NOT NULL,
    event_type         text NOT NULL,
    instance_id        uuid NULL,
    lease_id           uuid NULL,
    detail             jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at         timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_fix_own_events_key_time
    ON fix_session_ownership_events (session_key, created_at DESC);
```

`event_type` values:

```text
ACQUIRE_ATTEMPT
ACQUIRE_GRANTED
ACQUIRE_DENIED_HELD
ACQUIRE_DENIED_REDIS
ACQUIRE_ROLLBACK
RENEW_OK
RENEW_FAIL_REDIS
RENEW_FAIL_DB
LEASE_LOST
YIELD_BEGIN
YIELD_DONE
FENCED
STEAL_GRANTED
SPLIT_BRAIN
READY_FOR_EXECUTION
READY_CLEARED
FAULT
```

### 5.3 Columns on adjacent authority tables

| Table | Required columns / constraints |
|---|---|
| `fix_sessions` | `session_key`, `current_fencing_token`, `owner_instance_id`, `ownership_state`, connection/logon fields already planned in A25/A26 |
| `fix_orders` | `fencing_token NOT NULL`, unique `(destination_account, cl_ord_id)` |
| `execution_intents` | `fencing_token` on the row that is allowed to send; status transitions check token |
| `execution_reconciliation_runs` | `fencing_token` of the owner that ran the recon |

A `fix_orders` insert for an outbound TRADE application message **must** be in a transaction that re-reads the lease row (`SELECT … FOR UPDATE` or a single conditional `INSERT … SELECT`).

### 5.4 Acquire SQL (mint)

Steal **only** when the previous lease is expired **or** the row is unowned **or** this same instance is retrying an acquire that has not reached `READY_FOR_EXECUTION`. Never steal a `READY_FOR_EXECUTION` row whose `leased_until >= now()`.

```sql
UPDATE fix_session_leases AS l
SET fencing_token       = l.fencing_token + 1,
    owner_instance_id   = @instance_id,
    owner_hostname      = @hostname,
    owner_pid           = @pid,
    lease_id            = @lease_id,
    ownership_state     = 'LEASE_BOUND',
    leased_until        = now() + (@ttl_ms || ' milliseconds')::interval,
    acquired_at         = now(),
    last_renew_at       = now(),
    last_redis_ok_at    = NULL,
    ready_for_execution = false,
    epoch_reason        = @reason,
    row_version         = l.row_version + 1,
    updated_at          = now()
WHERE l.session_key = @session_key
  AND l.session_qualifier = 'TRADE'
  AND (
        l.owner_instance_id IS NULL
     OR l.leased_until IS NULL
     OR l.leased_until < now()
     OR (
            l.owner_instance_id = @instance_id
        AND l.ready_for_execution = false
        AND l.ownership_state IN ('STANDBY', 'ACQUIRING', 'LEASE_BOUND', 'FAULT', 'FENCED')
        )
      )
RETURNING l.fencing_token, l.lease_id, l.row_version;
```

`0` rows ⇒ denied. Stay `STANDBY`. Do not touch Redis (except optional `GET` for diagnostics).

Same-instance reclaim is **only** for a retry of *this* boot’s incomplete acquire, not for inheriting a previous process on the same host.

### 5.5 Renew SQL

```sql
UPDATE fix_session_leases
SET last_renew_at    = now(),
    last_redis_ok_at = now(),
    leased_until     = now() + (@ttl_ms || ' milliseconds')::interval,
    updated_at       = now()
WHERE session_key        = @session_key
  AND owner_instance_id  = @instance_id
  AND fencing_token      = @token
  AND ownership_state NOT IN ('FENCED', 'STANDBY')
RETURNING leased_until;
```

`0` rows ⇒ fenced or lost. Run §8 immediately. Do not treat Redis `PTTL` as proof of ownership.

### 5.6 Fence / rollback SQL

Used on yield, Redis-bind failure, renew failure, and split-brain:

```sql
UPDATE fix_session_leases
SET fencing_token       = fencing_token + 1,
    owner_instance_id   = NULL,
    owner_hostname      = NULL,
    owner_pid           = NULL,
    lease_id            = NULL,
    ownership_state     = @next_state,          -- 'STANDBY' | 'FENCED' | 'FAULT'
    leased_until        = NULL,
    ready_for_execution = false,
    epoch_reason        = @reason,
    row_version         = row_version + 1,
    updated_at          = now()
WHERE session_key       = @session_key
  AND owner_instance_id = @instance_id
  AND fencing_token     = @token
RETURNING fencing_token;
```

The increment is what fences in-flight writers. Clearing `ready_for_execution` is mandatory on every epoch change (§28 / §42).

Conditional write used by persist-before-send:

```sql
INSERT INTO fix_orders (…, fencing_token, status)
SELECT …, l.fencing_token, 'not_sent'
FROM fix_session_leases l
WHERE l.session_key = @session_key
  AND l.owner_instance_id = @instance_id
  AND l.fencing_token = @token
  AND l.ready_for_execution = true          -- false for flatten-only path: see §10.2
  AND l.leased_until > now();
```

`0` rows ⇒ **do not send**.

---

## 6. State machines

Keep **ownership** and **FIX transport** separate. Domain already has `FixSessionStatus` (`Disconnected` … `ReadyForExecution`). Do not overload it with lease TTL.

### 6.1 `OwnershipState`

```text
STANDBY          no claim; may attempt acquire
ACQUIRING        DB mint in flight
LEASE_BOUND      DB token + Redis key held; FIX socket must not be up yet
                 unless this is a same-epoch reconnect after transport drop
CONNECTING       initiator starting; still not allowed to send application orders
LOGGED_ON        35=A accepted; new copy still blocked
RECONCILING      §42 in progress; new copy blocked
READY            ownership + transport + recon all green
                 (maps to FixSessionStatus.ReadyForExecution on the TRADE session)
YIELDING         controlled logout + release
FENCED           token no longer current; socket must die; no reconnect
FAULT            protocol / split-brain / unexpected; human + alert
```

Legal transitions:

```text
STANDBY     → ACQUIRING
ACQUIRING   → LEASE_BOUND | STANDBY | FAULT
LEASE_BOUND → CONNECTING | YIELDING | FENCED | FAULT | STANDBY
CONNECTING  → LOGGED_ON | YIELDING | FENCED | FAULT
LOGGED_ON   → RECONCILING | YIELDING | FENCED | FAULT
RECONCILING → READY | YIELDING | FENCED | FAULT
READY       → RECONCILING | YIELDING | FENCED | FAULT
YIELDING    → STANDBY | FENCED | FAULT
FENCED      → STANDBY          (only after socket confirmed down / timeout)
FAULT       → STANDBY          (operator or next acquire after leased_until)
```

There is **no** edge `LEASE_BOUND → READY` and **no** edge `LOGGED_ON → READY`.

`ready_for_execution` in Postgres is `true` only in `READY`. Any transition out of `READY` sets it `false` in the same statement.

### 6.2 TRADE session vs ownership

| Ownership | `FixSessionStatus` allowed | May send `35=D` copy? |
|---|---|---|
| not `LEASE_BOUND`…`READY` | `Disconnected` only | no |
| `LEASE_BOUND` / `CONNECTING` | `Connecting`, `LogonSent` | no |
| `LOGGED_ON` / `RECONCILING` | `LoggedOn`, `Reconciling` | no (status/pos requests only) |
| `READY` | `ReadyForExecution` | only if §10 conjunction holds |
| `YIELDING` | `LogoutSent` | no |
| `FENCED` / `FAULT` | `Error` then `Disconnected` | no |

---

## 7. Protocols

### 7.1 Standby loop

Every `fix-worker` replica with `CTRADER_FIX_TRADE_SESSION_ENABLED=true` runs the loop. Replicas that are not owner stay in `STANDBY` and **do not** create a TRADE initiator.

```text
STANDBY
  → (optional) Redis GET: if live and not me, sleep
  → mint (§5.4)
       deny  → sleep backoff
       grant → bind Redis (§4.3)
                 fail → rollback (§5.6, ACQUIRE_ROLLBACK) → STANDBY
                 ok   → persist last_redis_ok_at
                      → LEASE_BOUND
                      → establish FIX TRADE (§7.3)
```

Do **not** open the TRADE socket in `STANDBY` “to warm the connection.”

### 7.2 Acquire (normative order)

```text
1. Write ACQUIRE_ATTEMPT
2. Mint token N in Postgres (§5.4)          -- linearizable fence
3. If mint denied: ACQUIRE_DENIED_HELD; stop
4. Bind Redis with token N (§4.3)
5. If bind denied:
      if Redis key held by other instance:
          rollback mint (§5.6)
          ACQUIRE_DENIED_REDIS or SPLIT_BRAIN
          STANDBY
      else (Redis error / timeout):
          rollback mint
          fail closed (do not keep DB ownership)
6. Confirm GET matches instance + token N
7. ACQUIRE_GRANTED
8. Only now construct CTraderTradeSession / initiator
```

**Mint first, bind second.** A mint that cannot bind must roll back. That briefly fences the previous owner (token advanced) even if they still had a socket — correct: they were already expired in Postgres or unowned. If they were *not* expired, mint would have been denied.

### 7.3 Leadership change (architecture §28 — mandatory order)

Quoted requirement:

```text
new instance
    ↓
establish FIX session
    ↓
reconcile positions/orders
    ↓
only then accept new execution intents
```

Expanded with the lease:

```text
new instance
    ↓
acquire lease (Postgres token N + Redis bind)
    ↓
block new executions          -- ready_for_execution stays false
    ↓
establish FIX TRADE session (TLS + Logon, 141=Y per RoE)
    ↓
OrderMassStatusRequest
    ↓
RequestForPositions
    ↓
consume Execution / Position reports
    ↓
compare with internal DB
    ↓
repair / update state
    ↓
only if reconciled AND lease still held at token N:
    READY_FOR_EXECUTION
    ↓
only then accept new execution intents
```

Never: Logon → drain `execution_intents` → `NewOrderSingle`.  
Never: steal → READY because the previous owner was READY.  
Never: skip recon because “we just lost the socket for 200 ms.” Transport drop after READY returns to `RECONCILING` (or `CONNECTING` then `RECONCILING`), not to READY.

Reconnect **same epoch** (same `instance_id` + same `fencing_token` + Redis still bound): allowed to re-Logon and re-reconcile. The loser of a new epoch **must not** reconnect.

### 7.4 Renew loop

Runs only while `ownership_state ∈ {LEASE_BOUND, CONNECTING, LOGGED_ON, RECONCILING, READY}`.

```text
every TRADE_LEASE_RENEW_MS (jittered):
    1. Redis renew Lua (§4.4)
         fail → LEASE_LOST (§8)
    2. Postgres renew (§5.5)
         fail → treat as fenced (§8); attempt Redis release of *our* token
    3. RENEW_OK
    4. If PTTL < TRADE_LEASE_MIN_REMAINING_MS:
         refuse new sends until the next successful renew
         (do not wait for full expiry to stop sending)
```

One failed renew is enough. No “miss two then yield.” Fail closed.

Renew **Redis first**, then DB. If Redis fails, the key is already dead or foreign — do not extend `leased_until`. If DB fails after Redis renew, still yield: we cannot persist a send, so we must not keep the socket.

### 7.5 Graceful yield (shutdown, flag off, operator)

```text
1. YIELD_BEGIN; set ready_for_execution = false (same token; no increment yet)
2. Stop dequeue of new execution intents
3. Do not start new 35=D / 35=F / 35=G
4. Logout TRADE (35=5). If Logout fails or times out, continue
5. Dispose initiator; confirm socket down (or timeout)
6. Redis release Lua (§4.5) with our instance + token
7. Fence SQL (§5.6) → STANDBY   -- increments token
8. YIELD_DONE
```

If Logout fails, **still** release the lease. The next owner reconciles (§28, A25 §4.3). In-flight `sent` rows become `EXECUTION_STATE_UNKNOWN` (§8.2).

### 7.6 Steal (only after dual expiry)

A standby instance may mint only when §5.4 matches. Independently, it must **not** bind Redis while the key exists.

If Postgres is expired but Redis is live: **wait**. The holder may be failing DB renews and is about to yield; or we are partitioned from Postgres. Stealing the DB row while the socket is still leased is how you get two TRADE sessions.

If Redis is absent but Postgres still shows a live owner: **wait** for `leased_until`. Do not `DEL` a missing key and mint early.

---

## 8. Losing the lease (fail closed)

Trigger any of:

- Redis renew returns `0` or errors
- Postgres renew returns `0` rows or errors
- Local observe: Redis `GET` token/instance mismatch
- `PTTL` missing
- Operator / shutdown yield
- Split-brain detector (§11.3)

Immediate actions, in order, **on the loser**:

```text
1. Local state → FENCED (memory). Stop the send gate first.
2. ready_for_execution = false if we can still write our row+token
3. Do not send any further TRADE application message
4. Do not reconnect this initiator
5. Logout + dispose socket (best effort, short timeout)
6. Classify in-flight orders (§8.2)
7. Attempt Redis release only if GET still matches us
8. If we still own the DB row at our token: fence SQL (§5.6)
9. LEASE_LOST / FENCED event
10. STANDBY or FAULT; alert
```

The **winner** does not need the loser’s cooperation. Winner’s mint already incremented the token; loser’s persist-before-send will fail the `INSERT … SELECT`.

### 8.2 In-flight classification

| Local / DB status at fence | Action |
|---|---|
| `not_sent` and socket write **not** started | leave `not_sent`; next owner may send **only after** recon proves the `ClOrdID` is unknown on the venue |
| socket write started or status `sent_ack_unknown` / accepted / partial | set `EXECUTION_STATE_UNKNOWN` (§33–§34). **Never** a second `35=D` for that `ClOrdID` |
| terminal (`filled` / `rejected` / `cancelled`) | leave terminal |

The loser **must not** reconnect “to finish the unknown.” That is the new owner’s job via `OrderStatusRequest` / `OrderMassStatusRequest` / positions.

---

## 9. Persist-before-send under the fence

Architecture §33: persist `cl_ord_id` and intent **before** the socket write. Combined with this lease:

```text
0. Load intent (not expired, status = not_sent)
1. Prove may_own_trade_socket (§1) including PTTL ≥ min remaining
2. Prove §10 conjunction (flags, risk, READY, kill switch)
3. BEGIN
     lock lease row
     INSERT fix_orders (status=not_sent, fencing_token=N) via §5.6 insert
     (same transaction may stamp execution_intents)
   COMMIT
   -- if 0 rows or commit fails: STOP. no send.
4. Re-check Redis GET (instance + token N) and PTTL
   -- fail: do not send; order stays not_sent
5. Re-check flags + READY + kill switch (TOCTOU)
6. Send 35=D (or F/G/H/AF/AN as applicable)
7. UPDATE fix_orders SET status = sent_ack_unknown
     WHERE id = @id AND fencing_token = N
   -- 0 rows: already fenced; treat as EXECUTION_STATE_UNKNOWN
```

Never generate a `ClOrdID` in memory and send first. Never retry a send because TCP broke. Never increment a retry counter into a **new** `ClOrdID` for the same intent unless reconciliation proved the first id never existed on cServer (A25 §5.1).

Outbound TRADE application messages that require the fence:

```text
D   NewOrderSingle
F   OrderCancelRequest
G   OrderCancelReplaceRequest
H   OrderStatusRequest          -- allowed in LOGGED_ON/RECONCILING; still fenced
AF  OrderMassStatusRequest      -- same
AN  RequestForPositions         -- same
x   SecurityListRequest         -- TRADE catalog; still requires lease, not READY
```

Session-level `0`/`1`/`2` (Heartbeat / TestRequest / Resend) may flow while `LOGGED_ON+` **and** the lease is held. They must stop the instant the instance is fenced.

---

## 10. Send conjunction (fail closed)

Re-check immediately before the socket write (A25 §6.3, plus this lease).

### 10.1 New exposure (`NewOrderSingle` for `OPEN` / `INCREASE`)

All must be true:

```text
CTRADER_FIX_ENABLED                  = true
CTRADER_FIX_TRADE_SESSION_ENABLED    = true
REAL_COPY_EXECUTION_ENABLED          = true
ownership_state                      = READY
ready_for_execution                  = true            -- Postgres
fencing_token matches Postgres and Redis
Redis PTTL                           >= min remaining
TRADE FixSessionStatus               = ReadyForExecution
risk engine healthy
STOP_NEW_EXECUTION                   = false
EMERGENCY_FLATTEN not blocking opens
QUOTE usable if the order needs a fresh price
  (session healthy AND quote_age <= configured max AND instrument mapped)
execution_intent persisted
cl_ord_id persisted (this epoch’s token)
status                               = not_sent
intent not expired
database reachable (the persist just succeeded)
```

Any miss ⇒ do not send. Persist a risk/execution decision. Codes to add beside A23 §4.3:

| Code | When |
|---|---|
| `TRADE_SESSION_NOT_OWNED` | no lease / not this instance |
| `TRADE_SESSION_FENCED` | token mismatch |
| `TRADE_LEASE_REDIS_UNAVAILABLE` | Redis down or renew failed |
| `TRADE_LEASE_TTL_LOW` | PTTL &lt; min remaining |
| `TRADE_OWNERSHIP_NOT_READY` | owned but not `READY` (recon / logon) |
| `DATABASE_UNAVAILABLE` | already specified; includes lease-row write failure |

### 10.2 `EMERGENCY_FLATTEN` / reduce-close

Architecture §40 / A25 §6.5: flatten may send reducing orders when `REAL_COPY_EXECUTION_ENABLED` is false, **only if** TRADE is logged on, the lease is owned, flatten is authorized, and persist-before-send + unknown-state still apply.

Flatten does **not** skip the fence. It may run from `LOGGED_ON` / `RECONCILING` / `READY` when destination position ids are known. It still fails closed if Redis or Postgres cannot prove ownership.

### 10.3 Risk engine

A23 already requires `trade_session_state` and `database_available`. Treat “not owner” as `TRADE_FIX_UNAVAILABLE` **or** the more specific codes above. Do not approve an intent because a *different* replica owns TRADE.

Approved intents sitting in the DB while nobody is `READY` **expire** (§63). Do not queue an unlimited stale backlog for the next owner to fire.

---

## 11. Failure matrix (fail closed)

| Failure | TRADE socket | New copy `35=D` | What the next owner does |
|---|---|---|---|
| Redis down at acquire | do not open | no | retry standby |
| Redis down while owner | Logout / drop | no | wait expiry + mint |
| Postgres down at acquire | do not open | no | retry |
| Postgres down while owner | drop (cannot persist) | no | wait; recon |
| Redis renew fail | §8 | no | steal after dual expiry |
| DB renew fail | §8 | no | same |
| Token mismatch on persist | no send | no | recon unknowns |
| Token mismatch after send | no retry | n/a | recon that `ClOrdID` |
| GC pause &gt; TTL | lease expires; loser fenced on wake | no (stale token) | recon then READY |
| Network partition vs Redis | renew fails | no | same |
| Network partition vs Postgres | yield | no | same |
| Network partition vs cTrader | keep lease; session not READY | no | existing §62 TRADE-down |
| Two replicas start | one mint wins | only winner, after recon | — |
| Laptop + production password | same lease; second denied | no | — |
| Process kill -9 | Redis TTL + `leased_until` | no | steal after dual expiry; recon |
| Split brain detected | both fail closed | no | operator; see §11.3 |
| Clock jump on worker | ignored for steal | send blocked if PTTL low | — |

Database unavailable ⇒ execution fail closed (§62). Redis unavailable ⇒ **this design also** fail closed for TRADE ownership. Do not run critical real execution from volatile memory.

### 11.3 Split brain

Conditions (any one):

- Redis value.instance_id ≠ Postgres owner, and both look live
- Redis token ≠ Postgres token, and Redis key exists
- Two `fix_sessions` rows claim `LoggedOn` for the same TRADE `session_key`

Response:

```text
1. SPLIT_BRAIN event + page (P0, equal to a double-send — A25)
2. Every local instance that can see the mismatch:
     stop send gate
     Logout TRADE
     do not reconnect
3. Do not mint a third epoch until Redis key is gone
     AND leased_until is cleared or expired
     AND no session reports LoggedOn
4. Next acquire must full-reconcile; READY stays false until clean
```

Do not “pick a winner” in the worker. Fail closed and make a human look at `fix_session_ownership_events`.

---

## 12. QUOTE vs TRADE

| | TRADE | QUOTE |
|---|---|---|
| §28 hard exclusive | **yes** | recommended same lease shape |
| Duplicate cost | duplicate orders / ERs | duplicate quotes / bad freshness |
| Required before `35=D` | this lease + READY | quote health, not this TRADE lease |
| Replica | **out of scope** (§71) | replica **out of scope** (A25) |

Implement QUOTE as `ti:fix:lease:…:QUOTE` so two workers do not share one QuickFIX store or double-subscribe. **Owning QUOTE never grants TRADE.** Diagnostics may run QUOTE while production holds TRADE only if they use a **different** account or production TRADE is disabled (`CTRADER_FIX_TRADE_SESSION_ENABLED=false`) **and** they do not take the TRADE key.

A developer laptop against live `1369850` TRADE is forbidden unless production has yielded the lease and the laptop is the sole acquirer (A25 §4.5).

---

## 13. Feature flags and worker wiring

Defaults remain architecture §41:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
TRADE_OWNERSHIP_ALLOW_DB_ONLY=false
```

`CTRADER_FIX_TRADE_SESSION_ENABLED=false`: run health/lease **standby only**; do not mint; do not open TRADE.

`CTRADER_FIX_ENABLED=false`: no sockets; lease loop may still run for observability, but must not mint a TRADE epoch that would block a later enabled owner without reason. Prefer: do not mint when the flag is off.

The current `apps/fix-worker` is a template heartbeat (`A08`). When implemented, the worker host owns:

```text
lease loop (all replicas)
   ↓
only the owner constructs CTraderTradeSession
   ↓
Logon → recon → READY
   ↓
execution dequeue (still gated by REAL_COPY_EXECUTION_ENABLED)
```

Do not put acquire/renew in `Program.cs`. Do not open TRADE from an MT5 callback (§32).

---

## 14. Planned types (do not implement in this task)

| Type | Layer | Role |
|---|---|---|
| `FencingToken` | Domain | monotonic `long`; reject `0` on send |
| `OwnerInstanceId` | Domain | boot UUID |
| `SessionOwnershipKey` | Domain | venue + account + qualifier + `session_key` |
| `OwnershipState` | Domain enum | §6.1 |
| `TradeSessionLeaseSnapshot` | Domain | token, instance, `leased_until`, state, ready |
| `ITradeSessionOwnership` | Application port | `TryAcquire`, `Renew`, `Yield`, `Current`, `IsOwned` |
| `IFencingTokenStore` | Application port | mint / renew / fence / persist-gated insert |
| `IRedisLease` | Application port | bind / renew / release / get (TRADE/QUOTE keys only) |
| `TradeSessionOwnershipCoordinator` | Application | §7 state machine; **the only** caller of initiator start |
| `TradeSessionSendGate` | Application | §10 conjunction |
| `PostgresFencingTokenStore` | Infrastructure | SQL in §5 |
| `RedisTradeSessionLease` | Infrastructure | Lua in §4; `StackExchange.Redis` |
| `ReadyForExecutionGate` | Application | A05: lease + logon + recon + flags + risk + quote |

Forbidden types: `IDistributedCache` lease, `RedLockNet` as the fence, generic `IRedis.Set("order:…")`.

Postgres advisory lock may be taken **in addition** on `hashtext(session_key)` inside the mint transaction as a same-database mutex. It is **not** a substitute for the token or the Redis TTL.

---

## 15. Observability

### 15.1 Logs (§57)

Every ownership event includes:

```text
correlation_id
session_key
session_qualifier
owner_instance_id
fencing_token
lease_id
ownership_state
ready_for_execution
redis_pttl_ms
```

Never log tag 554 / passwords / Redis connection strings.

### 15.2 Metrics (extend §58)

```text
fix_lease_held                          -- 1 if this process holds TRADE
fix_lease_fencing_token                 -- gauge, current token if owner else 0
fix_lease_pttl_ms
fix_lease_acquire_total{result}
fix_lease_renew_fail_total{reason}
fix_lease_lost_total
fix_fenced_sends_total
fix_split_brain_total
fix_ready_for_execution                 -- 1 only when §6 READY
```

Plus existing `fix_trade_connected`, `fix_unknown_execution_states`.

### 15.3 Dashboard (extend A26 TRADE card / §52)

```text
Owner instance
Fencing token
Lease TTL remaining
Ownership state
Ready for execution?
Last acquire / yield / fence
```

Never show FIX or Redis passwords.

---

## 16. Tests (must exist before live TRADE Logon)

Architecture §60 / A27 do not yet list a lease suite. Required:

### 16.1 Unit (`TraderIntelligence.Tests.Unit`)

| Class | Must prove |
|---|---|
| `Ownership.FencingTokenMonotonicTests` | increment only; `0` cannot pass the send gate |
| `Ownership.AcquireSqlPredicateTests` | cannot steal READY+unexpired; can take expired / null owner |
| `Ownership.SendGateConjunctionTests` | each §10 flag independently blocks |
| `Ownership.InFlightFenceClassificationTests` | not_sent vs unknown vs terminal (§8.2) |
| `Ownership.StateMachineTests` | no `LOGGED_ON → READY`; no send outside READY (except flatten policy) |

### 16.2 Integration (`TraderIntelligence.Tests.Integration`)

Use Testcontainers Postgres + Redis. **Do not** open `live-us-eqx-01.p.c-trader.com:5212`.

| Class | Must prove |
|---|---|
| `Ownership.SingleActiveAcquireTests` | second instance denied while first holds |
| `Ownership.RedisBindRollbackTests` | mint + Redis fail ⇒ token incremented, owner null, no socket |
| `Ownership.RenewFailureYieldsTests` | Redis or DB renew fail ⇒ fenced, `ready=false` |
| `Ownership.StaleTokenWriteRejectedTests` | persist-before-send with N−1 inserts 0 rows |
| `Ownership.LeadershipChangeReconcileTests` | winner cannot READY without recon stub success |
| `Ownership.LoserDoesNotReconnectTests` | after steal, loser send/reconnect APIs throw or no-op |
| `Ownership.RedisDownFailClosedTests` | no TRADE initiator, no `35=D` |
| `Ownership.PostgresDownFailClosedTests` | no send from Redis-only memory |
| `Ownership.Kill9ExpiryStealTests` | crash leaves key to TTL; next owner new token + recon |
| `Ownership.ReleaseDoesNotDeleteForeignKeyTests` | fenced DEL Lua returns 0; winner key remains |
| `Ownership.QuoteLeaseIndependentTests` | holding QUOTE does not allow TRADE send |

### 16.3 Harness (with FIX simulator, not production)

| Class | Must prove |
|---|---|
| `Harness.DualWorkerDuplicateLogonPreventedTests` | simulator receives at most one TRADE Logon |
| `Harness.FenceDuringNewOrderSingleTests` | token change between persist and send ⇒ no `35=D`; or send+unknown, never second D |
| `Harness.FlattenStillRequiresLeaseTests` | flatten without lease does not send |

A27’s `Two_active_TRADE_sessions_for_same_account_are_refused` (A10) is this suite.

---

## 17. What this design forbids

- Two `fix-worker` processes with `CTRADER_FIX_TRADE_SESSION_ENABLED=true` both logged on to the same account
- Redis as source of truth for orders, positions, balances, or READY
- Redis lease **without** a Postgres fencing token
- Falling back to DB-only ownership when Redis is down (`TRADE_OWNERSHIP_ALLOW_DB_ONLY` stays false)
- Redlock, K8s lease, or replica-count as the only control
- Cross-region active-active FIX (§71)
- Connecting TRADE before both mint and bind succeed
- `READY_FOR_EXECUTION` without §42 recon on **this** epoch
- Sending `35=D` because “we used to be owner”
- Decrementing or recycling fencing tokens
- `DEL` of a Redis key that is not ours
- Stealing while the Redis key is still live
- Sharing the live TRADE password with staging “just to watch”
- Treating QUOTE ownership as TRADE ownership
- `IDistributedCache` for this protocol
- Blind catch-up of expired intents by a new owner (§63)

---

## 18. Relation to other documents

| Doc | Relationship |
|---|---|
| Architecture §28 | This file is the selected mechanism + fail-closed protocol |
| Architecture §5 / §62 | Redis coordination only; DB fail-closed for execution |
| `A25` §4 | Same invariant and leadership order; **mechanism replaced** by Redis+DB as specified here |
| `A23` | Risk codes + fail-closed; add ownership codes in §10.1 |
| `A03` | Redis façade allow-list; no order/position/balance keys |
| `A05` / `A08` | Types still **MISSING** in product source; this spec is what they implement later |
| `A26` | Dashboard fields in §15.3 |
| `A27` | Tests in §16 to add when coding starts |
| `A32` | Header mapping; ownership does not change tags 49/56/50/57 |

Measured product state at spec time (do not greenwash):

- `apps/fix-worker` is a template loop; no lease, no FIX.
- `src/Fix.CTrader` has no session objects.
- `FixSessionQualifier` and `FixSessionStatus` (including `ReadyForExecution`) exist as enums only.
- `TraderDbContext` references `FixSessionStates` / several execution sets; **no** `fix_session_leases` mapping or Redis multiplexer is a completed ownership control.
- `StackExchange.Redis` is referenced and unused — vacuous compliance, not a control (A03).

---

## 19. Implementation order (later coding task; not this file)

1. Migration for `fix_session_leases` + `fix_session_ownership_events` + fence columns on `fix_orders` / `execution_intents`.
2. Typed Redis façade (lease + later score/dashboard only).
3. `PostgresFencingTokenStore` + `RedisTradeSessionLease` + coordinator.
4. Wire coordinator in `fix-worker` **before** any TRADE initiator exists.
5. Tests in §16 green on Testcontainers.
6. Only then TRADE Logon in diagnostics. `REAL_COPY_EXECUTION_ENABLED` stays **false** until §70.

Lease bugs are **P0**, equal to a double-send.

---

## 20. One-page algorithm (copy onto the implementer’s wall)

```text
STANDBY ──mint token N in Postgres──► bind Redis(N) ──fail──► rollback+STANDBY
                                      │
                                      ok
                                      ▼
                         open TRADE socket (not before)
                                      ▼
                         Logon → block sends → reconcile
                                      ▼
                    still owner at N? ─no─► Logout, do not send
                         yes
                          ▼
                  READY_FOR_EXECUTION (Postgres + memory)
                          │
          renew Redis then Postgres every ≤ TTL/3
                          │
         persist order with token N ─0 rows─► do not send
                          │
              Redis still N? ─no─► do not send
                          │
                     send FIX, then mark sent
                          │
              on any renew/token/Redis/DB failure:
                     STOP SENDS → Logout → fence → STANDBY
```

**If you cannot prove you own this epoch, you do not send.**
