# D29 — `FixSessionOwnership` is not §28 ownership

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D29_ownership.md` |
| Agent | D29 (FIX TRADE session ownership) |
| Date | 2026-08-18 |
| Assigned | Read `FixSessionOwnership.cs`. Write this file. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT | `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` |
| Method | Full re-read of the 134-line type. Independent of B28/C27. Cross-check A46, A25 §4, architecture §28, worker, DI, schema, tests. SHA-256 + byte counts measured this pass. |
| Binding law | Architecture v2 §1.9 / §28 (single-active TRADE). Official cTrader FAQ: two TRADE sockets copy every `35=8` to every connection. A46: Redis lease **echoes** a Postgres-minted fence; fail closed. |

**Honesty rule:** a `ConcurrentDictionary` is not a Redis lease. A monotonic `Interlocked.Increment` is not a Postgres fencing token. `owned && reconciled` in process memory is not `READY_FOR_EXECUTION`. Unused code does not protect a live account. Vacuous safety (no TRADE socket today) is **not** an implemented control.

---

## 0. Verdict

**EXISTS_NEEDS_REFACTOR. Unused. Unsafe if wired as the production TRADE lock.**

`FixSessionOwnership` is a process-local try-once wrapper around `InMemoryDistributedLockWithFencing`. The *shape* of the send gate is the only thing worth keeping:

```text
ExecutionIntentsAllowed = _hasOwnership && _reconciled
```

Everything A46 requires for a real owner is absent: Postgres mint, Redis bind/renew/release Lua, per-epoch reconcile reset, persist-before-send under the fence, worker lease loop. The in-memory lock itself is racy **inside one process**.

| Question | Measured answer |
|---|---|
| Does the type exist? | **Yes.** 134 lines, 4 719 bytes. |
| Is it A46 / §28 ownership? | **No.** |
| Is it registered in DI? | **No.** `AddTraderIntelligence` does not mention it. |
| Does `apps/fix-worker` call it? | **No.** Worker stamps `fix_sessions` every 15 s and never acquires. |
| Any other product `.cs` reference? | **No.** Definition only. |
| Tests on disk (`*Ownership*`, `*FixSession*`)? | **None** under `D:\Prop\tests`. |
| Redis / Postgres lease tables / Lua? | **Missing.** |
| Two `fix-worker` replicas both acquire? | **Yes**, if each constructs its own in-memory lock. |
| Dual-owner incident possible **today**? | **No TRADE socket.** `SAFE_BY_ABSENCE` only. |
| Safe to add a TRADE initiator on top of this type? | **No. UNSAFE.** |

Classification:

| Slice | Class |
|---|---|
| `owned && reconciled` conjunction | **EXISTS_AND_GOOD** (local shape only) |
| Release requires `ownerId` **and** token | **EXISTS_AND_GOOD** (intent; delete is not CAS) |
| `IDistributedLockWithFencing` port idea | **EXISTS_NEEDS_REFACTOR** — nested in the concrete class; belongs in Application |
| `InMemoryDistributedLockWithFencing` | **EXISTS_NEEDS_REFACTOR** — test double in the product assembly; racy |
| `FixSessionOwnership` wrapper | **EXISTS_NEEDS_REFACTOR** — no renew, no TTL watch, reconcile survives epoch |
| Redis `ti:fix:lease:{session_key}` | **MISSING** |
| Postgres `fix_session_leases` + mint | **MISSING** |
| Application `ITradeSessionOwnership` / `IFencingTokenStore` / `IRedisLease` | **MISSING** |
| Worker lease loop | **MISSING** |
| Persist-before-send carries fence | **MISSING** (`ExecutionIntent` has no `FencingToken`) |
| A89 #70 / #71 / #84 | **MISSING** (A89 marks EXISTS — **false**) |
| Dual-owner protection in production | **MISSING** (vacuous today) |

Do **not** treat this file as §28 done. Do **not** harden the dictionary into a production lock. Do **not** open TRADE until A46 is implemented.

File hash is **unchanged** since B05 / B28 / C27. This report reconfirms; it does not claim new product work.

---

## 1. Inventory (measured 2026-08-18)

| Path | Bytes | Lines | SHA-256 | Role |
|---|---:|---:|---|---|
| `src/Fix.CTrader/Services/FixSessionOwnership.cs` | 4719 | 134 | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` | SUT |
| `src/Domain/Entities/FixSessionState.cs` | 979 | 25 | `46C20D6A1BF5F84769DB483FD17A0EBEB8BDA8C1C56BBA2B8B30A59FCE44697E` | `OwnerHeld` / `OwnerInstance`; **no** fence |
| `src/Domain/Enums/FixSessionStatus.cs` | — | 14 | — | Transport FSM; not a lease |
| `src/Domain/Enums/FixSessionQualifier.cs` | — | 7 | — | `Quote` / `Trade` |
| `src/Domain/Entities/ExecutionIntent.cs` | — | 20 | — | No `FencingToken` |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | — | 80 | — | Flags + ports; **no** lease TTL knobs |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | 419 | — | — | Domain + Application only. **No** Redis package (correct) |
| `src/Infrastructure/DependencyInjection.cs` | — | 44 | — | No lock / ownership registration |
| `apps/fix-worker/Worker.cs` | — | 49 | — | 15 s heartbeat. **No** acquire/renew |
| `apps/fix-worker/Program.cs` | — | 22 | — | `AddTraderIntelligence` + seeder. No Fix.CTrader type constructed |

`Get-ChildItem D:\Prop\tests -Recurse -Filter *Ownership*` → empty.  
`Get-ChildItem D:\Prop\tests -Recurse -Filter *FixSession*` → empty.

Grep of product `*.cs` for `FixSessionOwnership` / `IDistributedLockWithFencing` / `InMemoryDistributedLockWithFencing` / `ExecutionIntentsAllowed` / `MarkReconciled`: **hits only in the SUT**.

Grep of product `*.cs` for `fix_session_leases` / `ITradeSessionOwnership` / `IRedisLease` / `IFencingTokenStore` / `TRADE_LEASE`: **0 hits**.

---

## 2. What the file actually does

Namespace: `TraderIntelligence.Fix.CTrader.Services`.

Three types in one file:

```text
FixSessionOwnership                          -- wrapper (local flags)
  IDistributedLockWithFencing                -- nested port
  InMemoryDistributedLockWithFencing         -- nested test double
```

### 2.1 Port

```17:32:D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs
    public interface IDistributedLockWithFencing
    {
        Task<(bool acquired, long fencingToken)> TryAcquireAsync(
            string lockKey,
            string ownerId,
            TimeSpan ttl,
            CancellationToken cancellationToken);

        Task ReleaseAsync(
            string lockKey,
            string ownerId,
            long fencingToken,
            CancellationToken cancellationToken);
    }
```

No `Renew`. No `Get`. No `PTTL`. No return value on release (cannot tell DEL-miss from success). Name says distributed; the only implementation is not.

### 2.2 In-memory lock (entire acquire)

```44:62:D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs
        public Task<(bool acquired, long fencingToken)> TryAcquireAsync(
            string lockKey,
            string ownerId,
            TimeSpan ttl,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var now = DateTimeOffset.UtcNow;
            _locks.TryGetValue(lockKey, out var current);
            var expired = current.expiresAt != default && current.expiresAt <= now;

            if (!expired && current.ownerId != null)
                return Task.FromResult((false, current.fencingToken));

            var fencing = Interlocked.Increment(ref _globalToken);
            _locks[lockKey] = (ownerId, fencing, now.Add(ttl));
            return Task.FromResult((true, fencing));
        }
```

Release:

```64:77:D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs
        public Task ReleaseAsync(...)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_locks.TryGetValue(lockKey, out var current) && current.ownerId == ownerId && current.fencingToken == fencingToken)
            {
                _locks.TryRemove(lockKey, out _);
            }
            return Task.CompletedTask;
        }
```

### 2.3 Wrapper

Constructor null-checks `lockProvider`, `ownerId`, `lockKey`. Does **not** reject empty strings or non-positive `ttl`.

Public surface:

| Member | Meaning |
|---|---|
| `HasOwnership` | last `TryAcquire` returned `acquired: true` (stale after TTL) |
| `FencingToken` | last token written into the field — **including the winner’s token on a failed acquire** |
| `ExecutionIntentsAllowed` | `HasOwnership && _reconciled` |
| `AcquireAsync` | one try; overwrites both flags/token |
| `MarkReconciled` | sets `_reconciled = true` with no ownership check |
| `ReleaseAsync` | no-op if `!_hasOwnership`; else release + clear all three fields |

Comments on the type are honest: production “should be backed by Redis”; in-memory is “fallback for development/unit tests.” Those comments do not implement A46.

---

## 3. Defects (even as a test double)

Numbering continues B28 R1–R7 so later agents do not fork the list. All re-verified on the current SHA.

### R1 — check-then-set is not a lock (P0)

`TryGetValue` then indexer write is TOCTOU. Two threads can both observe empty/expired, both `Interlocked.Increment`, both write. Both return `acquired: true` with **different** tokens. Last indexer write wins. Split-brain **inside one process**.

A correct fake uses `AddOrUpdate` / `TryAdd` + `TryUpdate` and reports acquire only if the stored `(owner, token)` is the tuple just written.

### R2 — current owner cannot renew; heartbeat drops the local flag (P0)

If the key is live, **every** caller — including the same `ownerId` — gets `(false, current.fencingToken)`. `AcquireAsync` then sets `_hasOwnership = false`. `ReleaseAsync` returns immediately (`if (!_hasOwnership) return`) → **lease leak until TTL**. There is no `Renew` (A46 Lua `PEXPIRE` only).

A worker that “heartbeats” by re-calling `AcquireAsync` evicts itself from the wrapper while leaving the dictionary entry in place.

### R3 — TTL is invisible to the wrapper (P0)

`FixSessionOwnership` never re-reads the dictionary. After `expiresAt`, another caller can acquire. This instance still has `_hasOwnership == true`. If it called `MarkReconciled`, **`ExecutionIntentsAllowed` stays true**. That is the GC-pause / expired-key split-brain A46 exists to kill.

### R4 — `_reconciled` survives a new fencing epoch (P0)

`AcquireAsync` does not clear `_reconciled`. `ReleaseAsync` does, but expiry and failed re-acquire do not go through `ReleaseAsync`.

```text
1. acquire token 1, MarkReconciled → intents allowed
2. TTL expires (wrapper unaware)
3. same instance AcquireAsync → dictionary expired → token 2 granted
4. _reconciled still true → intents allowed on a new fence with zero recon
```

A46 / A47 / architecture §28: every new token is a new epoch. `ready_for_execution` stays false until **this** epoch’s mass-status + positions succeed. There is no edge `LOGGED_ON → READY`.

### R5 — `MarkReconciled` is honor-system (P0)

No check that `_hasOwnership`. Call it first, then acquire → `ExecutionIntentsAllowed` becomes true with **zero** `35=AF` / `35=AN` / persist proof. The boolean is not reconciliation.

### R6 — release delete is not compare-and-remove (P0)

```csharp
if (TryGetValue && owner && token match)
    TryRemove(lockKey, out _);   // removes whatever is there now
```

Between the check and the remove, a third party can expire-steal and store a new tuple. `TryRemove(key)` deletes the **new** owner. A46 release Lua: `DEL` only if `instance_id` **and** token still match. Use `TryRemove(KeyValuePair)`.

### R7 — failed acquire pollutes `FencingToken` (P0 on a send path)

```csharp
_hasOwnership = acquired;
_fencingToken = fencing;   // winner's token when acquired == false
```

A46 value object: `0` means never acquired; never send with a defaulted or **foreign** token. Metrics / logs that read `FencingToken` after a deny attribute the wrong epoch. A later bug that gates send on `FencingToken != 0` would pass on a loser.

### R8 — steal uses the local clock (P1 vs A46)

`expired = expiresAt <= DateTimeOffset.UtcNow`. A46: steal only when Postgres `leased_until < now()` **and** Redis key absent. Local clock is for scheduling the next renew, not for authority.

### R9 — token is process-global, not per session (P1)

`_globalToken` is one counter per lock **instance**, shared by every `lockKey`. Postgres mint is per `session_key` row, increment-by-1 in the same `UPDATE … RETURNING`. A shared in-process counter is not that contract. Two lock instances (two workers) each start at 0 and both issue token 1.

### R10 — empty / zero TTL / empty owner are legal (P1)

Constructor rejects only `null`. `ownerId = ""` is a holder (`!= null`). `ttl <= 0` writes `expiresAt` in the past (or now); the next caller steals immediately. `lockKey = ""` collides every session.

### R11 — no qualifier on the type (P1)

TRADE vs QUOTE is entirely the caller’s `lockKey`. Nothing prevents using a QUOTE lease to set `ExecutionIntentsAllowed` for TRADE sends. A46 §0 / §12: owning QUOTE **never** authorizes TRADE.

### R12 — wrapper fields are unsynchronized (P2)

`_hasOwnership`, `_reconciled`, `_fencingToken` are ordinary fields. Concurrent `Acquire` / `Release` / `MarkReconciled` can tear. Fine only if a single loop owns the instance — which is undocumented and unenforced.

### R13 — `AcquireAsync` name vs behaviour (P2)

One try. Does not wait, backoff, or throw on deny. A46 standby loop: 1 s backoff, cap 5 s. Callers that assume “after `AcquireAsync` I own it” are wrong; they must read `HasOwnership`.

---

## 4. A46 / §28 scorecard (fail closed)

Architecture §28 (verbatim requirement):

```text
For one cTrader trading account, do not allow two production
instances to simultaneously own the same TRADE session.

new instance → establish FIX session → reconcile → only then
accept new execution intents
```

A46 chosen mechanism: **Redis lease with fencing token, PostgreSQL as authority.** `TRADE_OWNERSHIP_ALLOW_DB_ONLY` stays **false**.

| A46 / §28 rule | Measured in this file | Score |
|---|---|---|
| One TRADE owner per `(venue, account, qualifier)` **across processes** | `ConcurrentDictionary` in one process | **FAIL** |
| Postgres **mints** the fence; Redis only echoes | `Interlocked.Increment(ref _globalToken)` | **FAIL** |
| Redis key `ti:fix:lease:{env}:{broker}:{account}:{qual}` | caller-supplied `lockKey`; no grammar | **FAIL** |
| Bind Lua: never overwrite a live foreign key | indexer overwrite | **FAIL** |
| TTL 10 s, renew ≤ ⅓, min remaining 2 s | one-shot `expiresAt`; **no Renew** | **FAIL** |
| Release / yield **increments** the token | `TryRemove` only | **FAIL** |
| Steal only if DB expired **and** Redis absent | local clock | **FAIL** |
| Fail closed if Redis or DB down | in-memory always “up” | **FAIL** |
| `0` means never acquired on a send path | deny copies winner’s token | **FAIL** |
| Reconcile after **this** epoch | `_reconciled` not cleared on re-acquire | **FAIL** |
| `ready_for_execution` only in READY | boolean honor system | **FAIL** |
| Persist-before-send carries token | `ExecutionIntent` has no column | **FAIL** |
| Wired to TRADE send | **zero callers** | **VACUOUS** |
| Split-brain alarm | none | **FAIL** |
| Tests A46 §16 / A89 #70 #71 #84 | files absent | **FAIL** |

Two `fix-worker` processes each `new InMemoryDistributedLockWithFencing()` and both acquire. cServer then copies every ExecutionReport to both sockets. That is the incident this type exists to prevent, and it does not.

---

## 5. Adjacent “owner” stories (uncoordinated)

### 5.1 `FixSessionState`

```22:24:D:\Prop\src\Domain\Entities\FixSessionState.cs
    public bool OwnerHeld { get; set; }
    public string? OwnerInstance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
```

Mapped to `fix_sessions`. Unique on `Qualifier` only (one QUOTE row, one TRADE row for the whole database — not per venue/account). **No** `fencing_token`, `leased_until`, `session_key`, `ownership_state`, `ready_for_execution`.

Demo seeder inserts TRADE as `LoggedOn` with `OwnerHeld` left default **false**. Worker never writes `OwnerHeld` / `OwnerInstance`.

### 5.2 Worker (does not consult the SUT)

```35:40:D:\Prop\apps\fix-worker\Worker.cs
            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
            if (trade is not null)
            {
                trade.LastInboundAt = DateTimeOffset.UtcNow;
                trade.Status = real ? FixSessionStatus.LoggedOn : FixSessionStatus.LoggedOn;
            }
```

Both branches are `LoggedOn`. `REAL_COPY_EXECUTION_ENABLED` does not change status. No acquire, no recon, no `ReadyForExecution`. Dashboard `GetFixSessionsAsync` treats `LoggedOn` as logged-on and hard-codes the last flag `false` (not this lease).

### 5.3 Risk engine

`RiskEvaluationRequest.Reconciled` is a caller-supplied bool. Risk does not read `FixSessionOwnership`. There is no `TRADE_SESSION_NOT_OWNED` / `TRADE_SESSION_FENCED` reason in this path.

### 5.4 Schema / DI / options

- `TraderDbContext`: no `fix_session_leases`, no `fix_session_ownership_events`.
- `DependencyInjection`: no Redis multiplexer, no lock, no coordinator.
- `CTraderFixOptions`: `RealCopyExecutionEnabled` default false (good). No `TRADE_LEASE_TTL_MS` / renew / min-remaining.
- `Fix.CTrader.csproj`: correctly has **no** `StackExchange.Redis`. A46 implementations (`PostgresFencingTokenStore`, `RedisTradeSessionLease`) belong in **Infrastructure**.

Two uncoordinated owner flags (`OwnerHeld` vs `_hasOwnership`) plus a third (dashboard “connected”) is how a later agent will think ownership exists.

---

## 6. Tests claimed vs tests on disk

A89 marks these **EXISTS**. On disk 2026-08-18 they are **MISSING**:

| A89 id | Claimed class | Pri | Disk |
|---|---|---|---|
| 70 | `FixSessionOwnershipLeaseTests` | P0 | **MISSING** |
| 71 | `FixSessionOwnershipFencingTokenTests` | P1 | **MISSING** |
| 84 | `FixSessionReadyForExecutionGateTests` | P0 | **MISSING** |
| A46 §16.2 | `Ownership.SingleActiveAcquireTests` et al. | P0 | **MISSING** |

Unit product tests present: scorer, sizing, risk, symbol, reconstruction, volume. No FIX codec or lease coverage.

Do not treat A89 EXISTS as evidence.

---

## 7. What to keep vs replace

Keep (as a *contract*, not as this implementation):

- try-acquire returns `(acquired, fencingToken)`;
- release requires owner **and** token;
- send gate is `owned && reconciled` **and then** the rest of A46 §10 (flags, risk, PTTL, persist-before-send).

Replace:

1. Postgres `fix_session_leases` mint (`UPDATE … RETURNING` increment) — A46 §5.4.
2. Redis bind Lua on `ti:fix:lease:{session_key}` with the minted token — A46 §4.3.
3. Renew loop: Redis `PEXPIRE` then DB renew; one failure ⇒ fenced — A46 §7.4.
4. Release: Redis DEL on match, then fence SQL **increments** token — A46 §7.5.
5. `Acquire` / new epoch **always** clears ready/reconciled.
6. Every `fix_orders` / `execution_intents` write carries `fencing_token`; stale insert is 0 rows ⇒ do not send.
7. Only the owner constructs the TRADE initiator. Standbys do not open a socket.
8. `TRADE_OWNERSHIP_ALLOW_DB_ONLY` stays **false**.

The in-memory type may remain as a **unit-test fake** only after: CAS acquire, value-sensitive release, same-owner renew, epoch reset of `_reconciled`, failed acquire leaves `FencingToken == 0`. Move it to `tests/` or `Fix.CTrader.Testing`. Do not leave a public “distributed” lock in the product assembly that two workers can each `new`.

Forbidden (A46 §17, still binding):

- Redlock / K8s lease / replica-count as the only control
- `IDistributedCache` for this protocol
- Redis as SoT for orders, positions, balances, or READY
- Falling back to DB-only when Redis is down
- Connecting TRADE before mint **and** bind succeed
- Sending `35=D` because “we used to be owner”
- Treating QUOTE ownership as TRADE ownership

---

## 8. Do / do not

**Do**

- Implement A46 in Infrastructure + an Application coordinator. Keep `Fix.CTrader` free of Redis.
- Write A89 #70 / #71 / #84 against a corrected fake **and** Testcontainers Postgres+Redis contract tests (A46 §16.2). Do not Logon `live-us-eqx-01.p.c-trader.com:5212` for those tests.
- Keep `REAL_COPY_EXECUTION_ENABLED` default **false** until lease + recon + unknown-state are proven.

**Do not**

- Call this file “session ownership done.”
- Wire `InMemoryDistributedLockWithFencing` into `fix-worker` as a stopgap.
- “Fix” the dictionary and ship it.
- Set `OwnerHeld = true` from the 15 s heartbeat and call that a lease.
- Greenwash A89 EXISTS rows.

---

## 9. Relation to siblings

| Note | After this pass |
|---|---|
| A25 §4.3 (Postgres-first recommendation) | Superseded for *mechanism choice* by A46 (Redis+fence, Postgres authority). This file implements neither. |
| A46 | Binding spec. **Not implemented.** |
| A99 | Lease key grammar frozen. **Not written.** |
| B05 §1.4 / B28 §3 / C27 | **Still accurate.** Same SHA `30029E29…`. |
| A05 (`Class1` only) | Stale. |
| A89 #70 #71 #84 EXISTS | **False.** |

---

## 10. Close

`FixSessionOwnership.cs` is a 134-line sketch of the right gate and the wrong lock. Hash `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20`. Zero callers. Zero tests. Not §28.

Product source was not modified.
