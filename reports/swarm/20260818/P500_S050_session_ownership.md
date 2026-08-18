# P500_S050 — Two TRADE senders = duplicate orders = instant loss. In-memory fence unused.

| Field | Value |
|---|---|
| Slot | **P500_S050** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S050_session_ownership.md` |
| Agent | P500_S050 (FIX TRADE session ownership / dual-sender capital risk) |
| Date | 2026-08-18 |
| Assigned | Read `FixSessionOwnership.cs` and architecture single-TRADE-session law. Write this file. **Do not edit product.** |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT | `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.7–1.9, §27, §28, §42 |
| Spec already on disk | `A46_session_ownership.md` (Redis lease + Postgres fence). **Not implemented.** |
| Prior reconfirm | `D29_ownership.md`, `B28_fix_parser_review.md`, `C27_redis_gap.md` |
| Method | Full re-read of the 134-line type. Grep of all product `*.cs` for construction/use. Cross-check worker, DI, hosted logon, `FixSessionState`, `ExecutionIntent`, tests. |

**Honesty rule:** a `ConcurrentDictionary` is not a Redis lease. `Interlocked.Increment` is not a Postgres fencing token. `owned && reconciled` in process memory is not `READY_FOR_EXECUTION`. Unused code does not protect a live account. Vacuous safety (no `35=D` today) is **not** an implemented control. Two TRADE senders on the same cTrader account is **instant loss**, not a failover pattern.

---

## 0. Verdict

**FAIL for §28. EXISTS_NEEDS_REFACTOR. Unused. Unsafe if wired as the production TRADE lock.**

`FixSessionOwnership` is a process-local try-once wrapper around `InMemoryDistributedLockWithFencing`. The *shape* of the send gate is the only thing worth keeping:

```text
ExecutionIntentsAllowed = _hasOwnership && _reconciled
```

That conjunction is **never evaluated** outside the file that defines it. `grep` of product `*.cs` for `FixSessionOwnership` / `ExecutionIntentsAllowed` / `MarkReconciled` / `InMemoryDistributedLockWithFencing` / `IDistributedLockWithFencing` returns **definition-only** hits in `FixSessionOwnership.cs`. Zero constructors. Zero DI registrations. Zero worker calls.

Architecture §1.9 / §28: **one** active TRADE session per FIX account. Official cTrader FAQ: the server copies every FIX response to **every** active connection. Two TRADE sockets on `live.pepperstone.1369850` therefore means:

- two senders of `NewOrderSingle` (`35=D`) for the same copy intent → **duplicate live orders → instant doubled exposure / instant loss**,
- two copies of every `ExecutionReport` (`35=8`) → book mapping lies unless keyed on server IDs,
- no single owner for sequence, cancel, or reconcile.

The in-memory fence cannot stop that even if someone later registers it: each process constructs its own `ConcurrentDictionary`. Two `fix-worker` replicas, or `api` + `fix-worker` + `mt5-worker` (all three call `AddTraderIntelligence`), each acquire independently.

**Capital today:** `SAFE_BY_ABSENCE` of a live `35=D` writer only (`CTraderFixSession` outbound is Logon `35=A`; socket disposed after one read). That is **not** §28. Adding a TRADE sender on top of this type is a **loss path**.

| Question | Measured answer |
|---|---|
| Does the type exist? | **Yes.** 134 lines. |
| Is it architecture §28 ownership? | **No.** |
| Is it A46 (Redis + Postgres fence)? | **No.** Comment *says* Redis. Code is a dictionary. |
| Registered in DI? | **No.** `AddTraderIntelligence` does not mention it. |
| Does `apps/fix-worker` acquire/renew/release? | **No.** 15 s heartbeat stamps `Disconnected`. |
| Any other product `.cs` reference? | **No.** Definition only. |
| Tests on disk (`*Ownership*`, `FixSessionOwnership*`)? | **None** under `D:\Prop\tests`. A89 #70/#71/#84 mark EXISTS — **false**. |
| Redis / Postgres lease tables / Lua? | **Missing.** |
| Two processes both acquire? | **Yes**, if each constructs its own in-memory lock. |
| Dual-owner incident possible **today** (fills)? | **No `35=D`.** Vacuous. |
| Dual TRADE **logon** possible today? | **Yes, if password is set.** `CTraderFixLogonHostedService` is registered in shared DI and calls `TryLogonAsync(..., Trade, port 5212)` with **no** ownership check. API + both workers all host it. |
| Safe to add a TRADE initiator on this type? | **No. UNSAFE. Instant-loss class.** |

Classification:

| Slice | Class |
|---|---|
| `owned && reconciled` conjunction | **EXISTS_AND_GOOD** (local shape only) |
| Release requires `ownerId` **and** token | **EXISTS_AND_GOOD** (intent; delete is not CAS) |
| `IDistributedLockWithFencing` port idea | **EXISTS_NEEDS_REFACTOR** — nested in the concrete class; belongs in Application |
| `InMemoryDistributedLockWithFencing` | **EXISTS_NEEDS_REFACTOR** — test double in the product assembly; racy; no renew |
| `FixSessionOwnership` wrapper | **EXISTS_NEEDS_REFACTOR** — one-shot acquire, no TTL watch, reconcile survives epoch |
| Worker / hosted-service use | **UNUSED** |
| Redis `ti:fix:lease:{session_key}` | **MISSING** |
| Postgres `fix_session_leases` + mint | **MISSING** |
| Application `ITradeSessionOwnership` | **MISSING** |
| Persist-before-send carries fence | **MISSING** (`ExecutionIntent` has no `FencingToken`) |
| Dual-owner protection in production | **MISSING** (vacuous send-absence only) |

Do **not** treat this file as §28 done. Do **not** harden the dictionary into a production lock. Do **not** open TRADE send until A46 is implemented **and** wired **and** tested. Product source was not modified this pass.

---

## 1. Architecture: one TRADE session, not two senders

Source of truth: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.

### 1.1 Non-negotiable rules (§1)

§1.7 — Use **two independent** cTrader FIX sessions. QUOTE and TRADE are separate connections with independent session state.

§1.9 — **Do not use multiple simultaneous active TRADE sessions for the same FIX account.** Use active/passive ownership or a distributed lock. Duplicate execution reports can otherwise occur.

One QUOTE + one TRADE is the intended split. Two TRADE sockets is the failure mode.

### 1.2 Session objects (§27)

Maintain independent `CTraderQuoteSession` / `CTraderTradeSession` objects (connection, sequence, heartbeat, inbound/outbound timestamps, reconnect, metrics, logs). Do **not** share one sequence counter. Owning QUOTE does **not** authorize TRADE sends.

### 1.3 Ownership (§28) — binding

> For one cTrader trading account, do not allow two production instances to simultaneously own the same TRADE session.

Legal mechanisms listed:

- deployment singleton,
- database advisory lock,
- Redis lease with fencing token,
- leader election.

A46 selected **Redis lease + fencing token**, PostgreSQL remains authority. Deployment singleton is **not sufficient**: a second replica, a laptop diagnostic, or a forgotten staging box with the live password bypasses replica-count.

If leadership changes:

```text
new instance
    ↓
establish FIX session
    ↓
reconcile positions/orders
    ↓
only then accept new execution intents
```

### 1.4 Logon ≠ ready (§42)

On TRADE login: block new executions → OrderMassStatusRequest → RequestForPositions → compare with DB → **only if reconciled** set `READY_FOR_EXECUTION`.

`FixSessionOwnership.ExecutionIntentsAllowed` is the local encoding of that last step. Nothing in the worker or send path consults it.

### 1.5 Official venue behaviour (A34 / cTrader FAQ)

Verbatim from https://help.ctrader.com/fix/faqs/ :

> FIX API reports will be duplicated if you have multiple connections to the API open simultaneously. The server will send a copy of the FIX response to each active connection.

Fan-out, not retry. Two TRADE connections ⇒ two copies of every `35=8`. Two *senders* ⇒ two `35=D` for the same source deal unless an external fence exists. That is doubled live size on the first copy signal.

`docs/ctrader-fix.md` restates the intended pair: QUOTE 5211 / TRADE 5212, same credentials, distinct `TargetSubID`. It does **not** implement ownership.

---

## 2. What `FixSessionOwnership.cs` actually is

Path: `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` (134 lines).

### 2.1 Public surface

| Member | Behaviour |
|---|---|
| ctor(`IDistributedLockWithFencing`, `ownerId`, `lockKey`, `ttl`) | Stores fields. No acquire. |
| `HasOwnership` | Local `_hasOwnership` bool |
| `FencingToken` | Local `_fencingToken` long |
| `ExecutionIntentsAllowed` | `_hasOwnership && _reconciled` |
| `AcquireAsync` | One `TryAcquireAsync`. Sets local flags. **No renew. No retry. No throw on fail.** |
| `MarkReconciled` | Sets `_reconciled = true` with **no** ownership check |
| `ReleaseAsync` | No-op if not owner; else `ReleaseAsync` then zeros flags |

File-header comment (lines 8–14) admits production must be Redis + monotonic fence. That comment is not an implementation.

### 2.2 In-memory lock (the unused fence)

`InMemoryDistributedLockWithFencing` holds:

```text
ConcurrentDictionary<string, (string ownerId, long fencingToken, DateTimeOffset expiresAt)>
```

plus `long _globalToken` incremented with `Interlocked.Increment`.

`TryAcquireAsync`:

1. Throws if cancelled.
2. Reads current entry.
3. If not expired and `ownerId != null` → `(false, current.token)`.
4. Else increment token, **overwrite** `_locks[lockKey]`, return `(true, token)`.

Defects (measured from the code, not theory):

| Defect | Why it loses money if used as the TRADE lock |
|---|---|
| Process-local dictionary | Two OS processes never see each other. Both acquire. Two TRADE sockets. |
| No CAS on acquire | Two threads in one process can both observe expired/empty and both write. Last write wins; both believe they own. |
| Same owner cannot renew | Unexpired + same `ownerId` returns **`acquired=false`**. A heartbeat that re-calls `AcquireAsync` **drops** `_hasOwnership`. |
| No renew API | TTL is write-once. After expiry another caller can acquire while this instance still has `_hasOwnership == true`. |
| Release is not CAS-delete | `TryGetValue` then `TryRemove` without comparing the removed value. A third writer can slip in. |
| Token is process-local | Restart resets `_globalToken` to 0. Stale tokens can collide across process lives. |
| No expiry watch | `FixSessionOwnership` never re-reads the dictionary. After TTL, `ExecutionIntentsAllowed` can stay true if `MarkReconciled` was called. |
| `MarkReconciled` ungated | Caller can flip `_reconciled` without owning. Combined with a later acquire, intents become allowed without a new reconcile. |
| Reconcile survives epoch | New fencing token does not clear `_reconciled`. A46 requires per-epoch reset. |
| Nested port | `IDistributedLockWithFencing` lives inside the concrete class. Awkward DI. Redis+PG belongs in Infrastructure, not `Fix.CTrader`. |

The class comment says “Replace with a Redis-backed lock in real deployments.” That replacement does not exist. `TraderIntelligence.Fix.CTrader.csproj` has **no** Redis package (correct layering). Infrastructure pins `StackExchange.Redis` 2.8.0; **0** `using StackExchange.Redis` / multiplexer / Lua in product C#.

### 2.3 What the type is *not*

- Not a FIX session.
- Not QuickFIX/n.
- Not a leader-election service.
- Not a Postgres advisory lock.
- Not A46.
- Not consulted before `CTraderFixSession.TryLogonAsync` on port 5212.

---

## 3. Call graph: the fence is unused

### 3.1 Grep (product C#)

| Symbol | Hits |
|---|---|
| `FixSessionOwnership` | 1 file — definition |
| `InMemoryDistributedLockWithFencing` | same file |
| `IDistributedLockWithFencing` | same file |
| `ExecutionIntentsAllowed` | same file |
| `MarkReconciled` | same file |

No `new FixSessionOwnership(`. No `AddSingleton<FixSessionOwnership`. No `GetRequiredService<FixSessionOwnership`.

### 3.2 DI — `D:\Prop\src\Infrastructure\DependencyInjection.cs`

`AddTraderIntelligence` registers:

- DbContext (in-memory or Npgsql)
- live MT5 connectors
- `LiveRuntimeStatus` with `RealCopyEnabled = false`
- ingest + scoring
- `LiveIngestHostedService`
- **`CTraderFixLogonHostedService`**

It does **not** register:

- `FixSessionOwnership`
- `IDistributedLockWithFencing`
- any Redis multiplexer
- any lease hosted service

### 3.3 Three hosts, zero owners

All three process entry points call `AddTraderIntelligence`:

| Host | Path | Consequence |
|---|---|---|
| API | `D:\Prop\apps\api\Program.cs` | Hosts `CTraderFixLogonHostedService` |
| FIX worker | `D:\Prop\apps\fix-worker\Program.cs` | Same hosted service **plus** `Worker` |
| MT5 worker | `D:\Prop\apps\mt5-worker\Program.cs` | Same hosted service **plus** MT5 worker |

If `CTRADER_FIX_PASSWORD` is present and not a `<SECRET>` placeholder, **each** of those processes independently:

```text
CTraderFixSession.TryLogonAsync(Quote,  :5211, …)
CTraderFixSession.TryLogonAsync(Trade,  :5212, …)
```

No `AcquireAsync`. No check of `OwnerHeld`. No Redis. That is the dual-TRADE-socket path, live, gated only by password presence.

`CTraderFixLogonHostedService` then sets `RealCopyEnabled = false` and logs `NewOrderSingle still disabled`. Logon is still a **session**. Two successful TRADE logons is already the FAQ duplication case.

### 3.4 FIX worker does not own anything

`D:\Prop\apps\fix-worker\Worker.cs` every 15 s:

- loads QUOTE / TRADE `FixSessionState` rows,
- stamps `Status = Disconnected`,
- writes `LastError = "No live TRADE socket. NewOrderSingle remains off."`

It never constructs `FixSessionOwnership`. It never reads `OwnerHeld` / `OwnerInstance`. It fights the hosted logon service: one writes `LoggedOn` after TLS 35=A, the other overwrites `Disconnected` on a timer. That is a dashboard lie, not a lease.

### 3.5 Persistence columns that look like ownership

`D:\Prop\src\Domain\Entities\FixSessionState.cs` has:

```csharp
public bool OwnerHeld { get; set; }
public string? OwnerInstance { get; set; }
```

Grep of product `*.cs`: **definition only**. Seeders do not set them. Hosted logon does not set them. Dashboard does not display them. They are dead schema.

`ExecutionIntent` has no `FencingToken`. Persist-before-send cannot carry a fence that does not exist.

`FixSessionStatus` includes `Reconciling` and `ReadyForExecution`. Nothing in the send path (there is no send path) requires them. Dashboard `TradeHealthy` is true for `LoggedOn` **or** `Reconciling` **or** `ReadyForExecution` **or** in-memory `runtime.Trade.LoggedOn`. Logon is treated as healthy. §42 is inverted.

---

## 4. Loss model: why two TRADE senders is instant loss

This is a copy-hedge system. One MT5 source deal is supposed to become **one** destination order on Pepperstone/cTrader account `1369850`.

```text
MT5 deal (source)
    → CopyIntent
    → RiskDecision
    → ExecutionIntent + ClOrdID
    → one NewOrderSingle on the single TRADE socket
    → one fill
```

Two processes that both believe they own TRADE:

```text
same MT5 deal
    → worker A sends 35=D  ClOrdID=X  qty=Q
    → worker B sends 35=D  ClOrdID=Y  qty=Q
    → venue fills both
    → 2Q live exposure, 2× commission, 2× adverse tick
```

Even with identical ClOrdID, two sockets are two independent senders; the venue does not collapse them. Even if one send is rejected, two `35=8` copies on two sockets make the book think it saw two fills unless idempotency keys on server `OrderID`/`ExecID` (also **not** implemented as a send path).

cTrader FAQ makes the inbound side worse: **every** report is copied to **every** connection. Naive “on 35=8 increment filled qty” doubles again.

This is not a rare race. It is the default of “run API + fix-worker locally” or “scale fix-worker to 2” once a sender exists. The in-memory fence does **nothing** across those processes.

**Today’s only protection is the missing sender.** `CTraderFixSession.BuildLogon` is the only wire writer (`35=A`). No `35=D`, no tag 38, no persist-before-send. `LiveRuntimeStatus.RealCopyEnabled` is forced false in DI and again after logon. That is `SAFE_BY_ABSENCE`. It evaporates the day someone adds `NewOrderSingle` without A46.

---

## 5. Tests claimed vs tests on disk

A89 planned:

| # | Name | Claim | Disk |
|---|---|---|---|
| 70 | `FixSessionOwnershipLeaseTests` | EXISTS | **MISSING** |
| 71 | `FixSessionOwnershipFencingTokenTests` | EXISTS | **MISSING** |
| 84 | `FixSessionReadyForExecutionGateTests` | EXISTS | **MISSING** |

`D:\Prop\tests` has no `Fix\` folder and no `*Ownership*` / `*FixSession*` test class. Checksum and lease behaviour are untested. Do not treat A89 EXISTS as evidence.

---

## 6. What A46 still requires (not built)

Binding spec: `D:\Prop\reports\swarm\20260818\A46_session_ownership.md`.

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
    AND session state + flags + risk gates
```

Fail closed if Redis or Postgres is down. No Redlock. No “DB-only” fallback in production. Restart = new instance = new token. Reconcile is **per epoch**.

None of: `fix_session_leases` table, `ti:fix:lease:{env}:{broker}:{account}:TRADE` key, Lua bind/renew/release, worker lease loop, persist-before-send with fence column, fail-closed send gate.

`FixSessionOwnership` is a test-double *shape*. It must not ship as production ownership.

---

## 7. What must not be done

1. **Do not** register `InMemoryDistributedLockWithFencing` in `fix-worker` DI and call that §28 done.
2. **Do not** add `NewOrderSingle` until A46 is implemented, wired into the TRADE initiator, and tested for second-owner-fails + expire-then-stale-cannot-send.
3. **Do not** treat `CTraderFixLogonHostedService` succeeding on 5212 as ownership. Logon without a lease is how you get two TRADE sockets.
4. **Do not** host TRADE logon in API + mt5-worker + fix-worker. One TRADE owner process. QUOTE may be a different lease.
5. **Do not** use `OwnerHeld` on `FixSessionState` as a lock (no unique owner, no TTL, no fence, unread).
6. **Do not** treat dashboard `TradeHealthy` (`LoggedOn`) as `READY_FOR_EXECUTION`.
7. **Do not** edit product in this slot. Report only.

---

## 8. Evidence paths (absolute)

- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`
- `D:\Prop\src\Domain\Enums\FixSessionStatus.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (§1.9, §27, §28, §42)
- `D:\Prop\reports\swarm\20260818\A46_session_ownership.md`
- `D:\Prop\reports\swarm\20260818\A34_ctrader_fix_faq.md`
- `D:\Prop\reports\swarm\20260818\D29_ownership.md`

---

## 9. One-line pin

**Two TRADE senders on one cTrader account = duplicate `35=D` = instant loss. `FixSessionOwnership` is an unused process-local dictionary, not §28. Do not send until A46 is real.**

*End of P500_S050. Product source was not modified.*
