# A99 — Redis key catalog (live scores, quote cache, session lease)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A99_redis_keys.md` |
| Date | 2026-08-18 |
| Agent | A99 |
| Product source modified | **None** |
| Status | Binding key catalog for the next Redis façade. **Not implemented.** |
| Binding source | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §5, §28, §31, §37, §55, §62 |
| Adjacent swarm | A03 (Redis SoT ban), A20 (Postgres catalog), A22 (scores), A23 (risk + quote age), A24 (destination quotes), A26 (dashboard / SignalR), A30 (planned `RedisDestinationQuoteCache`), A41 (`ops:events` notify), A46 (TRADE/QUOTE lease — **wins** on lease TTL), A64 (worker quote cache), A65 (Redis 7, no AOF) |

This document names every Redis key this product is allowed to write, the TTL on each, the JSON payload, the Postgres rebuild path, and the hard ban: **Redis is not authoritative for orders, positions, or balances.**

It does **not** add `StackExchange.Redis` usage, a multiplexer, or any product-source type.

---

## 0. Binding law

Architecture §5 (quoted intent):

```text
PostgreSQL remains the durable source of truth.

Redis is for:
  - live scores
  - short-lived cache
  - distributed execution-session ownership
  - short-lived locks
  - live dashboard data

Do not use Redis as the authoritative store for orders, positions, or balances.
```

§28: a Redis lease **with fencing token** is a legal ownership mechanism. **The database remains the authority for execution state.**

§62: if the database is unavailable, execution **fails closed**. Do not run critical real execution solely from volatile memory (Redis included).

A46 conjunction (do not weaken):

```text
may_own_trade_socket =
    Postgres row says I am owner
    AND my fencing_token == Postgres current token
    AND Redis key exists
    AND Redis value.instance_id == me
    AND Redis value.fencing_token == Postgres current token
    AND Redis PTTL >= min_remaining
```

Redis presence is **liveness**. It is never a substitute for `fix_orders`, `execution_intents`, `destination_positions`, `mt5_positions_current`, `copy_intents`, `trader_scores`, or `destination_quotes`.

---

## 1. Measured current state (2026-08-18)

| Question | Evidence | Answer |
|---|---|---|
| Redis client package? | `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` — `StackExchange.Redis` 2.8.0 | Present |
| Multiplexer / options / `ConnectionStrings:Redis`? | hosts + Infrastructure `.cs` | **None** |
| Any `StringSet` / `HashSet` / Lua in product `.cs`? | grep of `D:\Prop\src` | **0** |
| Lease implementation? | `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` | In-memory `ConcurrentDictionary` only. Comment says “Replace with a Redis-backed lock.” |
| Score SoT? | `TraderDbContext` → `trader_scores` / `trader_score_history`; `Domain/Entities/TraderScore.cs` | **Postgres.** No Redis score key. |
| Quote SoT? | `TraderDbContext` → `destination_quotes`; `Domain/Entities/DestinationQuote.cs` (`DestinationQuoteSnapshot`) | **Postgres.** No Redis quote key. |
| Order / position / balance in Redis? | no writes | **No** (vacuous). Vacuous ≠ designed. |

Classification: **MISSING** allowed cache/lease features. **Not UNSAFE** today because nothing writes. The façade in §11 is the control that must exist **before** the first `SET`.

A65 lab Redis (`redis:7`) is `--save "" --appendonly no`, `allkeys-lru`, 256 mb, **no volume**. That is correct for a cache. It is **not** a book. Do not enable AOF to “save” leases or quotes.

---

## 2. Key grammar

```text
ti:{family}:{rest}
```

| Token | Rule |
|---|---|
| `ti` | Product prefix. Reserved. Never `order:`, `pos:`, `acct:`. |
| `{family}` | Allow-list only: `score` \| `quote` \| `fix` \| `dash` \| `lock` \| `hb`. Channels use `ti:ops:…` (§8). |
| environment | Required on every key. Values: `live` \| `demo` \| `staging`. For leases this is already inside A46 `session_key`. |
| separators | `:`. No spaces. No FIX password, Redis AUTH, or account password in the key **or** the value (§55). |
| logical DB | **DB 0 only.** Numbered DBs are not a security boundary. |

A46 lease key is already frozen. Do not restyle it:

```text
ti:fix:lease:{session_key}
session_key = {environment}:{broker_uid}:{account_id}:{qualifier}
```

Example: `ti:fix:lease:live:pepperstone:1369850:TRADE`

All other families follow the same `ti:` + environment discipline so a shared lab Redis cannot mix `demo` scores with `live` quotes.

**Identity law (A20):** source traders are `(broker_id, login)`. Never key a score by login alone.

---

## 3. Allow-list vs deny-list (code-review gate)

### 3.1 Allowed families

| Family | Purpose | SoT if Redis is empty / down |
|---|---|---|
| `ti:score:…` | Live score / leaderboard **projection** | `trader_scores` (+ history for charts) |
| `ti:quote:…` | Latest destination bid/ask **projection** | `destination_quotes` |
| `ti:fix:lease:…` | FIX session **liveness** + echoed fence | `fix_session_leases` (A46). No send if Redis down. |
| `ti:dash:…` | Short dashboard aggregates | Postgres queries (A26) |
| `ti:lock:…` | Non-execution short mutex | Lock absent ⇒ do not run the protected job |
| `ti:hb:…` | Worker heartbeat for `/health/ready` | Heartbeat missing ⇒ degraded. Not a send grant. |
| `ti:ops:events` | Pub/sub notify only (A41) | `system_events` + REST poll |

### 3.2 Forbidden key prefixes (P0 if written)

```text
order:          ti:order:         fix:order:        clordid:
position:       ti:position:      dest:position:    mt5:position:
balance:        ti:balance:       account:          equity:
deal:           ti:deal:          fill:             ti:fill:
intent:         ti:intent:        copy:             execution:
ready:          ti:ready:         book:             ledger:
```

Also forbidden as Redis **documents** (any prefix):

- `fix_orders` / `execution_intents` / `copy_intents` / `risk_decisions`
- `destination_positions` / `mt5_positions_current` / `mt5_account_snapshots`
- `shadow_orders` / `shadow_fills` / `shadow_positions`
- `READY_FOR_EXECUTION` as a Redis-only flag
- `ClOrdID` allocation sequences
- Redis Streams / RedisJSON / hashes used as the live book
- `mt5-sdk` “Redis fast outbox” (`terminal_fast_outbox_*`) ported as the §13 bus (A03, A41)

The façade **must not** expose generic `StringSet` / `HashSet`. Reviewers reject any helper that can write `order:…`.

---

## 4. Live scores — `ti:score:…`

### 4.1 Role

Architecture §5 “live scores.” Dashboard A26 `trader.score` / leaderboard / trader detail. Write-through **after** a successful `trader_scores` upsert (A22 §10).

Redis is a **projection**. Official scores, Trade-#3 snapshots, and trader state live in Postgres (`trader_scores`, `trader_score_history`, `trader_states`). The risk engine (A23) consumes durable `trader_state` / flags, not this cache.

### 4.2 Keys

| Key | Type | TTL | Writer | Readers |
|---|---|---|---|---|
| `ti:score:live:{env}:{broker_id}:{login}` | STRING JSON | `SCORE_LIVE_TTL_MS` **60000** | score worker, after PG commit | API, SignalR mapper |
| `ti:score:board:{env}:{score_kind}` | ZSET `member={broker_id}:{login}` `score=early_quality` | `SCORE_BOARD_TTL_MS` **15000** | same, optional | leaderboard GET |
| `ti:score:state:{env}:{state}` | SET of `{broker_id}:{login}` | `SCORE_BOARD_TTL_MS` **15000** | optional filter index | `GET /traders?state=` |

`score_kind` for the board is `early_quality` (A22 rank key). Do **not** rank by raw NET P&L (A22 I9).

v1 may ship **only** `ti:score:live:…` and query Postgres for lists. The ZSET/SET are an optimization. They are still rebuildable: `FLUSH` + `SELECT` from `trader_scores` where `n >= 3`.

### 4.3 Payload (`ti:score:live:…`)

Schema versioned. No secrets. Canonical numeric scale is **A22 `Round2` 0–100**, not the A26 sample `0.71`. The API may divide by 100 for display; Redis must not mix scales.

```json
{
  "v": 1,
  "environment": "live",
  "broker_id": "a1111111-0000-4000-8000-000000000001",
  "login": 6100421,
  "n": 7,
  "as_of": "2026-08-18T11:40:00.000Z",
  "score_version": "baseline.v1",
  "window": "EXPANDING",
  "risk_score": 22.00,
  "behavior_score": 68.00,
  "early_quality_score": 71.00,
  "state": "SHADOW",
  "martingale": false,
  "averaging_down": true,
  "lot_escalation": false,
  "severe_risk": false,
  "last_scored_at": "2026-08-18T11:40:00.000Z",
  "score_row_id": "uuid"
}
```

`PROVISIONAL` (`n < 3`) **must not** enter `ti:score:board:…`. Official publish starts at Trade #3 (A22 I3).

### 4.4 Write / read / miss

```text
Compute (pure) → persist trader_scores + history + state (one TX)
              → SET ti:score:live:… PX 60000
              → optional ZADD board + PEXPIRE 15000
              → publish ti:ops:events (trader.score) — notify only
```

Miss:

```text
GET live key → miss → SELECT trader_scores WHERE broker_id+login
                    → SET PX (if row exists)
                    → else 404 / INSUFFICIENT_DATA
```

Redis down: API reads Postgres. Scoring still commits. **Do not** fail a rescore because the cache SET failed.

### 4.5 What scores are not

- Not a risk-engine input path that can approve `35=D`.
- Not a substitute for `trader_score_history` (charts / ML later).
- Not a place to store feature snapshots, deal lists, or balances.
- Not authoritative if it disagrees with Postgres — **Postgres wins**; overwrite the key.

---

## 5. Quote cache — `ti:quote:…`

### 5.1 Role

Architecture §31 latest destination bid/ask for freshness, shadow pricing, slippage reference, pre-trade checks. A24: memory cache **and** durable `destination_quotes`. A30 planned type: `RedisDestinationQuoteCache`.

**Pricing / freshness authority is the quote snapshot fields** (`quote_received_at`, bid, ask), persisted in Postgres. Redis is a hot copy so risk/shadow/API do not hit the table on every MD incremental.

Quote **age** is **not** Redis `PTTL`:

```text
quote_age = decision_time - quote_received_at
```

A23: if a venue timestamp is present, also compute `venue_quote_age` and reject OPEN/INCREASE if **either** exceeds `max_quote_age`. Key presence ≠ fresh. `PTTL` remaining ≠ usable.

### 5.2 Keys

| Key | Type | TTL | Writer | Readers |
|---|---|---|---|---|
| `ti:quote:latest:{env}:{venue_code}:{canonical_symbol}` | STRING JSON | `QUOTE_CACHE_TTL_MS` **30000** | fix-worker QUOTE ingest, **after** PG upsert | risk, shadow, API, SignalR `quote.xauusd` |
| `ti:quote:id:{env}:{venue_code}:{instrument_id}` | STRING (same JSON) or alias GET | same | same | FIX path before canonical map |

v1 product universe is XAUUSD. Still key by `canonical_symbol` after Security List. **Never** hardcode instrument id `185` (A20 / A24 / A64).

Example: `ti:quote:latest:live:pepperstone:XAUUSD`

One latest row per venue×symbol. Tick **history** is not a Redis stream. If quote history is ever needed, it is a **new Postgres table** (A20 §958), not this key.

### 5.3 Payload

Matches A24 §2.2 + `DestinationQuoteSnapshot` (bid/ask/`ReceivedAt`/`VenueTimestamp`/`VenueInstrumentId`).

```json
{
  "v": 1,
  "environment": "live",
  "venue_code": "pepperstone",
  "venue_id": "uuid",
  "canonical_symbol": "XAUUSD",
  "instrument_id": "185",
  "bid": 2401.12,
  "ask": 2401.28,
  "spread": 0.16,
  "mid": 2401.20,
  "quote_received_at": "2026-08-18T12:00:00.200Z",
  "venue_timestamp": "2026-08-18T12:00:00.180Z",
  "fix_msg_seq_num": 18801,
  "md_entry_source": "INCREMENTAL",
  "quote_session_healthy": true,
  "usable": true,
  "destination_quote_id": "uuid"
}
```

Reject **before** write when `bid <= 0` OR `ask <= 0` OR `ask < bid` OR instrument not mapped to canonical XAUUSD (A24). Do not cache garbage.

`mid` is display / deviation only. Default fill is the **taker touch**, not mid (A24).

### 5.4 TTL vs policy thresholds

| Knob | Default | Meaning |
|---|---|---|
| `QUOTE_CACHE_TTL_MS` | `30000` | Redis eviction / rebuild. **Not** `max_quote_age`. |
| `MAX_QUOTE_AGE_OPEN_MS` | config, measured (A23/A24 — **not** hardcoded here) | OPEN/INCREASE reject (`QUOTE_STALE`) |
| `MAX_QUOTE_AGE_CLOSE_MS` | config, `>>` open | REDUCE/CLOSE still priced |
| `MAX_QUOTE_AGE_CLOSE_STALE_FALLBACK_MS` | config, larger | last-touch close after brief QUOTE outage |
| `MAX_QUOTE_AGE_MARK_MS` | config | dashboard mark quality |

`QUOTE_CACHE_TTL_MS` must be **≥** the largest close-fallback age you still want a last-touch to survive a writer pause. Consumers still apply the **policy** threshold for the action class. A 25 s old cached quote can legally CLOSE and still reject OPEN.

### 5.5 Write / read / session death

**Persist-before-cache** (same spirit as persist-before-send):

```text
MD Snapshot / Incremental
  → validate
  → UPSERT destination_quotes (Postgres)     -- SoT
  → Lua SET-if-newer on both quote keys PX 30000
  → in-process memory (fix-worker only)
  → publish quote.xauusd (notify)
```

Lua **SET-if-newer** (`KEYS[1]` latest key, `ARGV[1]` JSON, `ARGV[2]` TTL ms, `ARGV[3]` `quote_received_at` ISO / ticks):

```lua
local raw = redis.call("GET", KEYS[1])
if raw then
  local cur = cjson.decode(raw)
  if cur["quote_received_at"] and cur["quote_received_at"] > ARGV[3] then
    return 0
  end
end
redis.call("SET", KEYS[1], ARGV[1], "PX", ARGV[2])
return 1
```

Prevents a delayed snapshot from clobbering a newer incremental.

**Read** (risk / shadow / pre-trade):

```text
1. same-process memory if this is fix-worker
2. else GET Redis
3. else SELECT destination_quotes
4. compute quote_age from payload.quote_received_at
5. if missing OR session unhealthy OR unusable → QUOTE_UNAVAILABLE
6. if quote_age > max for this action class → QUOTE_STALE
```

Never treat “Redis GET succeeded” as “quote is fresh.”

**QUOTE session death** (A64): immediately `usable=false` **and** `quote_session_healthy=false` on the key (or `DEL`). Do not wait for TTL. Last bid after socket death is not a live quote; CLOSE fallback may still read `quote_age` against the last payload if policy allows.

### 5.6 What the quote cache is not

- Not the book of destination positions or shadow P&L (A24: Redis is not authority for shadow orders / positions / P&L).
- Not a source tick substitute for MFE/MAE (A22 I7 / A24).
- Not a grant to send `35=D`. Owning the **QUOTE lease** (`ti:fix:lease:…:QUOTE`) never authorizes TRADE (A46 §12).
- Not usable as the **only** pre-trade price if Postgres is down **and** this is a live send path — §62 fail closed. Shadow may use last durable row + age.

---

## 6. Session lease — `ti:fix:lease:…`

Normative protocol is **A46**. This section is the key/TTL extract so implementers do not invent a second lock.

### 6.1 Keys

```text
ti:fix:lease:{environment}:{broker_uid}:{account_id}:{qualifier}
```

| Example | Qualifier |
|---|---|
| `ti:fix:lease:live:pepperstone:1369850:TRADE` | TRADE — exclusive send / Logon |
| `ti:fix:lease:live:pepperstone:1369850:QUOTE` | QUOTE — exclusive MD / seq store |

One key per `(execution_venue, destination_account, qualifier)`. Not per process, not per host. One worker holding both sessions holds **two** keys.

Prefix `ti:fix:lease:` is reserved. The façade must not let this path write `order:` documents.

### 6.2 Value (echo only)

JSON, schema versioned, **no secrets**. Fencing token is **minted in Postgres**, echoed here (A46 §3).

```json
{
  "v": 1,
  "session_key": "live:pepperstone:1369850:TRADE",
  "instance_id": "8f2c0a6e-0000-4000-8000-000000000001",
  "lease_id": "c91d2b10-0000-4000-8000-000000000002",
  "fencing_token": 42,
  "acquired_at": "2026-08-18T12:00:00.000Z",
  "hostname": "fix-worker-a",
  "pid": 4120
}
```

### 6.3 TTL and renew (A46 defaults — binding)

| Knob | Production default | Rule |
|---|---|---|
| `TRADE_LEASE_TTL_MS` | `10000` | Redis `PX` **and** DB `leased_until` horizon |
| `TRADE_LEASE_RENEW_MS` | `3000` | ≤ ⅓ of TTL |
| `TRADE_LEASE_MIN_REMAINING_MS` | `2000` | Send / Logon / reconnect refused below this |
| `TRADE_LEASE_ACQUIRE_BACKOFF_MS` | `1000` (cap `5000`) | Standby retry |
| QUOTE knobs | **same numbers** (`QUOTE_LEASE_*` may alias) | Duplicate quotes corrupt freshness; same shape |
| Steal grace | **0** | Fail closed |

A64’s “TTL 15 s / renew 5 s” and A25’s Postgres-only recommendation are **superseded** for this repo (A46 header). Do not implement 15 s in the Redis path.

Liveness is Redis `PTTL`, not the worker’s wall clock. Steal requires **both** Postgres `leased_until < now()` **and** Redis key absent.

Lua acquire / renew / release: A46 §4.3–4.5. Renew is `PEXPIRE` only. Release `DEL`s only on matching `instance_id` **and** token.

### 6.4 Authority split

| Fact | Where it is true | Redis role |
|---|---|---|
| Who may hold the TRADE socket **now** | Redis key + Postgres row + matching token | Fast expire of a dead owner |
| Fencing token value | **Postgres only** mints | Echo |
| `ready_for_execution` | Postgres `fix_session_leases` | Must **not** be Redis-only |
| Orders / ER / dest positions | `fix_orders`, `fix_execution_reports`, `destination_positions` | **Forbidden** |
| Persist-before-send | Postgres `INSERT … SELECT` gated on token | Re-check GET + PTTL after persist, before socket |

If Redis is down: **do not** open TRADE, **do not** send (`TRADE_OWNERSHIP_ALLOW_DB_ONLY` stays false). If Postgres is down: **do not** send from a still-live Redis key (§62).

Current code (`FixSessionOwnership` + `InMemoryDistributedLockWithFencing`) is a test double. It must not ship as production ownership.

---

## 7. Adjacent allowed keys (keep tiny)

These are §5 “short-lived cache / locks / live dashboard data.” They are **not** the three headline families, but implementers will need names so they do not invent `order:`.

| Key / channel | Type | TTL | Notes |
|---|---|---|---|
| `ti:dash:overview:{env}` | STRING JSON (`OverviewDto` subset) | `DASH_TTL_MS` **5000** | Rebuild from Postgres. Optional. |
| `ti:hb:{env}:{worker}` | STRING `{instance_id, at}` | `HB_TTL_MS` **15000** | `mt5-worker` \| `fix-worker` \| `api`. Ready-check only. |
| `ti:lock:{env}:{name}` | STRING owner | `LOCK_TTL_MS` **10000** | Non-execution jobs only (board rebuild, seed). **Never** TRADE send. |
| `ti:ops:events` | PUBLISH channel | n/a | A41 notify. Canonical name is **`ti:ops:events`** (A41’s `ops:events` is the same family — implement the `ti:` name only). Payload = A26 allow-listed DTO. No secrets. |

Outbox **claim/lease** stays on Postgres `outbox_events` (A41). Do not move it to Redis.

---

## 8. Master TTL table

| Key pattern | Default TTL | Refresh | Miss / expiry behavior |
|---|---|---|---|
| `ti:score:live:{env}:{broker_id}:{login}` | 60 s | write-through on rescore | rebuild from `trader_scores` |
| `ti:score:board:{env}:{kind}` | 15 s | rewrite on rescore / timer | rebuild from `trader_scores` |
| `ti:score:state:{env}:{state}` | 15 s | same | rebuild |
| `ti:quote:latest:{env}:{venue}:{symbol}` | 30 s | every accepted MD after PG upsert | rebuild from `destination_quotes`; then apply `quote_age` |
| `ti:quote:id:{env}:{venue}:{instrument_id}` | 30 s | same | same |
| `ti:fix:lease:{session_key}` | **10 s** | renew ≤ 3 s | **fail closed** for TRADE; do not steal until DB expired **and** key gone |
| `ti:dash:overview:{env}` | 5 s | timer or notify | rebuild from dashboard queries |
| `ti:hb:{env}:{worker}` | 15 s | worker loop | health degraded |
| `ti:lock:{env}:{name}` | 10 s | holder extends or lets die | job does not run |

All TTLs are milliseconds in config (`*_TTL_MS`). Defaults above are lab/production starting points. **Quote-age policy thresholds stay independent** and measured.

---

## 9. Authority matrix (orders are not here)

| Data | Postgres | Redis | Memory | If Redis empty | If Postgres empty |
|---|---|---|---|---|---|
| Live scores | **SoT** `trader_scores` | projection | — | rebuild | no official score |
| Score history | **SoT** | **do not store** | — | — | — |
| Latest dest quote | **SoT** `destination_quotes` | projection | fix-worker hot | rebuild | QUOTE_UNAVAILABLE for new exposure |
| Quote history | not required (A20) | **forbidden stream** | — | — | — |
| TRADE/QUOTE owner liveness | `leased_until` + token | lease key | — | treat as not owner | **no send** |
| Fencing token | **mint** | echo | — | — | **no send** |
| READY_FOR_EXECUTION | **SoT** | forbidden as sole flag | gate | — | **no send** |
| Orders / ClOrdID | **SoT** `fix_orders` | **forbidden** | — | n/a | **no send** |
| Dest / MT5 positions | **SoT** | **forbidden** | — | n/a | fail closed |
| Balances / equity | **SoT** snapshots | **forbidden** | — | n/a | fail closed |
| Copy / execution intents | **SoT** | **forbidden** | — | n/a | fail closed |
| Shadow book / P&L | **SoT** | **forbidden** | — | n/a | no shadow fill |
| Outbox | **SoT** | notify channel only | — | REST poll | no ingest |

---

## 10. Failure, eviction, persistence

| Event | Scores | Quotes | TRADE lease |
|---|---|---|---|
| Redis process down | API → Postgres | memory → Postgres; else `QUOTE_UNAVAILABLE` | **no socket, no `35=D`** |
| Redis SET fails after PG write | log; PG already committed | log; PG already committed | bind fail → A46 rollback mint |
| Redis GET miss | rebuild | rebuild then age-check | not owner |
| `PTTL` &lt; min remaining (lease) | n/a | n/a | refuse send (`TRADE_LEASE_TTL_LOW`) |
| QUOTE socket dies | n/a | mark unusable / DEL immediately | QUOTE lease still renewed or yielded separately |
| Postgres down | no new official score | no durable quote; live send **fail closed** | **fail closed** even if lease key still live |
| `FLUSHALL` / lab wipe | rebuild | rebuild | owners yield; dual-expiry steal |
| LRU evicts a key | rebuild | rebuild | owner’s next renew → `LEASE_LOST` (fail closed) |

**A65 `allkeys-lru` vs leases.** LRU can delete `ti:fix:lease:…` while the owner still has a socket. Safety direction is fail closed (A46 §8), but it is operationally noisy and opens a race until the next renew.

When TRADE Logon is first implemented:

1. Set Redis `maxmemory-policy` to **`noeviction`** on the shared instance, **or**
2. Run a dedicated Redis for leases (still no AOF).

Do **not** turn on AOF/RDB to “protect” leases or quotes. A65: no volume; evict freely; Postgres is the book. Cache keys all have TTL; 5 000 score documents × ~1 KiB plus one XAU quote plus two leases fit in tens of megabytes. 256 mb is enough without LRU if the deny-list holds.

`IDistributedCache` is the wrong abstraction for fencing (A03, A46). Do not implement Redlock.

---

## 11. Façade (planned types — do not implement in this task)

| Type | Layer | Allowed operations |
|---|---|---|
| `IRedisConnection` | Infrastructure | multiplexer, ping, redact AUTH in logs (A50) |
| `ILiveScoreCache` | Application port | `Get` / `Set` / `Invalidate` live key; optional board rebuild |
| `IDestinationQuoteCache` | Application port | `GetLatest(canonical)` / `SetIfNewer` / `MarkUnusable` |
| `IRedisLease` | Application port | bind / renew / release / get **only** `ti:fix:lease:*` (A46) |
| `RedisTradeSessionLease` | Infrastructure | Lua in A46 §4 |
| `RedisDestinationQuoteCache` | Infrastructure | Lua in §5.5 |
| `RedisLiveScoreCache` | Infrastructure | `SET PX` / `GET` / miss-fill |

Forbidden types: generic `IRedis.Set(string key, …)`, `IDistributedCache` lease, `RedLockNet` as the fence, any `*OrderBookRedis*`.

Write path order is always **Postgres first, Redis second** except lease **bind**, which is **mint in Postgres first, then Redis bind**, with rollback if bind fails (A46 §7.2).

---

## 12. Config knobs (placeholders only)

```env
ConnectionStrings__Redis=127.0.0.1:6379,password=<SECRET>,abortConnect=false

SCORE_LIVE_TTL_MS=60000
SCORE_BOARD_TTL_MS=15000
QUOTE_CACHE_TTL_MS=30000
DASH_TTL_MS=5000
HB_TTL_MS=15000
LOCK_TTL_MS=10000

TRADE_LEASE_TTL_MS=10000
TRADE_LEASE_RENEW_MS=3000
TRADE_LEASE_MIN_REMAINING_MS=2000
QUOTE_LEASE_TTL_MS=10000
QUOTE_LEASE_RENEW_MS=3000
QUOTE_LEASE_MIN_REMAINING_MS=2000
TRADE_OWNERSHIP_ALLOW_DB_ONLY=false
```

Never commit a real Redis password (§55). Do not put Redis AUTH in React (`A65` web service has no `REDIS_*`).

`MAX_QUOTE_AGE_*` belong to risk/shadow config (A23/A24), **not** to this cache TTL list.

---

## 13. Tests required before any production Redis write

| Class | Must prove |
|---|---|
| `Redis.KeyAllowListTests` | façade rejects `order:`, `position:`, `balance:`, `clordid:` |
| `Redis.ScoreCacheMissRebuildsFromPostgresTests` | GET miss → `trader_scores` → SET |
| `Redis.ScoreCacheNotUsedForRiskApproveTests` | risk snapshot builder reads PG state, not the cache, as SoT |
| `Redis.QuoteSetIfNewerIgnoresStaleSnapshotTests` | older `quote_received_at` does not overwrite |
| `Redis.QuoteTtlIsNotFreshnessTests` | key with age &gt; `max_quote_age` still GET-able and still `QUOTE_STALE` |
| `Redis.QuoteUnusableOnSessionDeathTests` | disconnect ⇒ `usable=false` or DEL before TTL |
| `Redis.LeaseBindDoesNotWriteOrderKeysTests` | lease client cannot `SET` an order document |
| `Ownership.*` | full A46 §16 suite (fence, renew fail, Redis-down fail closed) |
| `Redis.NoAofRequiredForCorrectnessTests` | restart Redis; scores/quotes rebuild; leases require new mint; no order lost because it was never in Redis |

Integration: Testcontainers Redis + Postgres. Do not point these tests at `live-us-eqx-01.p.c-trader.com`.

---

## 14. Conflicts resolved here

| Other doc | Tension | Ruling |
|---|---|---|
| A25 / A64 “Postgres lease, Redis optional; TTL 15 s” | A46 chose Redis+fence, 10 s / 3 s | **A46 wins** for `ti:fix:lease:*` |
| A41 channel name `ops:events` | this catalog prefixes `ti:` | Implement **`ti:ops:events` only** |
| A26 sample scores `0.71` | A22 `Round2` 0–100 | Redis stores **0–100**; API maps |
| A65 `allkeys-lru` | can evict lease keys | Lab OK; before live TRADE, **`noeviction`** or dedicated Redis |
| A03 “vacuous compliance” | still true in product `.cs` | This file is the design; code remains unwritten |

---

## 15. Explicit exclusions

```text
No product-source edits in this change
No StackExchange.Redis multiplexer
No generic StringSet
No Redis as SoT for orders, positions, balances
No Redis Streams trading bus
No Redlock
No IDistributedCache fencing
No AOF “to be safe”
No READY_FOR_EXECUTION stored only in Redis
No QUOTE lease granting TRADE
No quote PTTL used as quote_age
No score cache used as risk authority
No mt5-sdk Redis fast-outbox port
```

---

## 16. One-page cheat sheet

```text
ti:score:live:{env}:{broker_id}:{login}          TTL 60s   rebuild trader_scores
ti:score:board:{env}:early_quality               TTL 15s   optional
ti:quote:latest:{env}:{venue}:{canonical}        TTL 30s   rebuild destination_quotes
                                                 age = now - quote_received_at
ti:fix:lease:{env}:{broker_uid}:{account}:{Q}    TTL 10s   liveness only; PG mints fence

Postgres = book
Redis    = cache + lease liveness + notify
Orders / positions / balances  →  never Redis
TRADE send                     →  PG + Redis lease + fence + READY; else no
```
