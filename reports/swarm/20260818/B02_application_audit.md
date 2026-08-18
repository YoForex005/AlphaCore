# B02 — `src/Application` audit vs architecture v2 §§6, 12, 32, 39

| Field | Value |
|---|---|
| Agent | B02 senior engineer (read-only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\src\Application` (product C# + csproj only; `bin/` / `obj/` are compile evidence, not design) |
| Spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Sections compared | **§6** Source Broker Architecture; **§12** Ingestion Pattern; **§32** FIX Trade Execution Flow; **§39** Risk Engine |
| Adjacent evidence (not scored as those sections) | Domain entities/engines, `src/Mt5`, `src/Infrastructure`, `apps/*` hosts — cited only to prove whether Application ports are used, duplicated, or bypassed |
| Supersedes | `D:\Prop\reports\swarm\20260818\A02_application_audit.md` (measured `Class1` + empty project; **stale**) |
| Product source modified | **No.** This audit wrote only this report. |
| Verdict | **FAIL — incomplete Application layer.** Ports for §6 exist and are used. A polling backfill use-case exists and is **not** the §12 three-loop. §§32 and 39 have **zero** Application orchestration. Scoring is invoked in the same ingest cycle, which is the opposite of §12’s “do not couple MT5 to ML/execution.” |

Classification vocabulary is architecture §73.B:

```text
EXISTS_AND_GOOD
EXISTS_NEEDS_REFACTOR
MISSING
DEPRECATED
UNSAFE
```

Layering note used below: **WRONG_LAYER** means the capability exists in Domain / Mt5 / Infrastructure / a host, but Application does not own the required port or use-case. That is not a §73.B class; it is recorded so absence in Application is not confused with “the system has nothing.”

---

## 1. Inventory of `src/Application`

Hand-authored product files (complete; `Class1.cs` is gone):

| Path | Bytes | Non-blank lines (PS) | SHA-256 | Last write | Role |
|---|---:|---:|---|---|---|
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | 433 | 13 | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` | 2026-08-18 12:55:09 | class library |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | 1,858 | 62 | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` | 2026-08-18 13:09:51 | §6 connector + registry ports + DTOs |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 2,577 | 89 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` | 2026-08-18 13:09:51 | dashboard query port + DTOs (§§46–53, **not** 6/12/32/39) |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4,277 | 90 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` | 2026-08-18 13:09:51 | `ITradingStore` + poll sync + reconstruction/scoring use-case |

No other product `.cs` files. No `Abstractions/`, `Ports/`, `Validators/`, `Outbox/`, `Copy/`, `Risk/`, `Scoring/`, `Execution/` folders under Application.

Public Application types (16):

| Namespace | Type | Kind |
|---|---|---|
| `TraderIntelligence.Application.Contracts` | `Mt5GroupDto` | record |
| | `Mt5AccountDto` | record |
| | `Mt5DealDto` | record |
| | `Mt5PositionDto` | record |
| | `IMt5BrokerConnector` | interface |
| | `IBrokerRegistry` | interface |
| `TraderIntelligence.Application.Dashboard` | `OverviewDto` | record |
| | `BrokerStatusDto` | record |
| | `GroupRowDto` | record |
| | `TraderRowDto` | record |
| | `FixSessionDto` | record |
| | `RiskDashboardDto` | record |
| | `IDashboardQueries` | interface |
| `TraderIntelligence.Application.Ingestion` | `ITradingStore` | interface |
| | `DealIngestionService` | class |
| | `ReconstructionScoringService` | class |

`grep` of Application product C# for `FluentValidation`, `IValidator`, `AbstractValidator`, `Outbox`, `CopyIntent`, `ExecutionIntent`, `IRiskEngine`, `Subscribe`, `GetOrders`, `checkpoint`, `IEventBus`: **zero matches**.

### 1.1 Project / layering

`TraderIntelligence.Application.csproj` in full:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.9.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

| ID | Item | Class | Evidence |
|---|---|---|---|
| P1 | `/src/Application` project exists | **EXISTS_AND_GOOD** (shell) | §66 lists `/src/Application`. Folder + csproj + real types (not `Class1`). |
| P2 | Layering: Application → Domain only | **EXISTS_AND_GOOD** | csproj references only `..\Domain\TraderIntelligence.Domain.csproj`. No Infrastructure / Mt5 / FIX refs. Downstream (`Infrastructure`, `Mt5`, `apps/*`) reference Application. Direction is correct. |
| P3 | `net8.0` + nullable | **EXISTS_AND_GOOD** | Matches §5 “C# / .NET 8+ compatible stack.” |
| P4 | `FluentValidation` 11.9.2 | **EXISTS_NEEDS_REFACTOR** | Reasonable for §12 “validate”; **zero** validators in the whole `D:\Prop` C# tree. |
| P5 | `Class1` | gone (was **DEPRECATED** in A02) | Not present. |
| P6 | Application unit tests | **MISSING** | `D:\Prop\tests` has no references to `DealIngestionService`, `IMt5BrokerConnector`, `ITradingStore`, or `IDashboardQueries`. |

Nothing in Application is **UNSAFE** (no secrets, no FIX send, no SDK P/Invoke). The residual risk is **absence plus a wrong ingest/score coupling**, not a live-order path.

---

## 2. What architecture v2 requires of Application

§66 places `/src/Application` next to Domain and Infrastructure and says: “Adapt to the existing repo; do not create duplicates unnecessarily.” Sibling folders (`TradeReconstruction`, `Scoring`, `Shadow`, `Risk`, `Execution`) are **optional later extracts**. Until they exist as projects, Application is the home for **use-cases and ports** that §§6, 12, 32, 39 name.

The only C# interface the architecture actually writes is `IMt5BrokerConnector` (§6). Everything else is a required port/service inferred from the named flows. Implementations belong in Infrastructure / Mt5 / Fix.CTrader / workers — **contracts and orchestration belong here**.

Quoted mandates used below:

**§6 (lines 322–355):** two source brokers (Achiever, StarwaveFX); “support more brokers without duplicating business logic”; “Create a broker registry.”; conceptual `IMt5BrokerConnector` with Connect / Disconnect / GetGroups / GetAccounts / GetDeals / **GetOrders** / GetPositions / **SubscribeAsync**; “The exact interface may be adjusted to the actual SDK.”; “Do not build two mostly identical connector codebases.”

**§12 (lines 525–571):** three loops — Historical Backfill + Live Event Subscription + Periodic Reconciliation. Backfill per broker/account: `Read checkpoint → Fetch history → Normalize → Upsert idempotently → Persist checkpoint`. Live: `MT5 event → validate → deduplicate → persist raw record → write transactional outbox event → commit`. “Then background workers process the outbox.” “This avoids coupling MT5 callbacks directly to ML or execution.”

**§32 (lines 1266–1298):** `Source MT5 event → Copy candidate? → Create CopyIntent → Persist → RiskEngine evaluates → ApprovedExecutionIntent → Persist → FIX Execution Worker → NewOrderSingle → ExecutionReport(s) → Persist fills/order state → Update destination position → Reconcile`. “Never send a FIX order directly from an MT5 event callback.”

**§39 (lines 1496–1538):** “The risk engine is the final authority.” Scoring/ML may only produce `candidate`, `confidence`, `suggested allocation`. Risk decides `approve / reduce size / reject / pause trader / pause venue / global stop`. Sixteen named hard limits (loss, daily loss, drawdown, XAU gross/net, position qty, open positions, spread, quote age, signal age, price move, slippage, margin, martingale, abnormal sizing, venue health).

---

## 3. Section 6 — Source Broker Architecture

### 3.1 Application surface that exists

`IMt5BrokerConnector` and `IBrokerRegistry` live in Application, as required:

```53:69:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
public interface IMt5BrokerConnector
{
    string BrokerCode { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<bool> IsConnectedAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct);
    Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct);
}

public interface IBrokerRegistry
{
    IMt5BrokerConnector Get(string brokerCode);
    IReadOnlyList<IMt5BrokerConnector> All();
}
```

One business path uses the registry (`DealIngestionService.SyncBrokerAsync` → `_registry.Get(brokerCode)`). That is the §6 “do not duplicate business logic” shape.

DTO coverage vs the conceptual types named on the §6 sketch:

| §6 type | Application DTO | Class |
|---|---|---|
| `Mt5Group` | `Mt5GroupDto` (name, currency, digits, company, margin call/stopout, connections) | **EXISTS_AND_GOOD** as a port DTO |
| `Mt5Account` | `Mt5AccountDto` (login, group, leverage, balance/equity/margin/profit) | **EXISTS_AND_GOOD** |
| `Mt5Deal` | `Mt5DealDto` (ticket, login, order, position, symbol, action, entry, native volume, price, P/L, commission, swap, time, comment) | **EXISTS_AND_GOOD** |
| `Mt5Position` | `Mt5PositionDto` (ticket, login, symbol, direction, native volume, prices, SL/TP, profit, create time) | **EXISTS_AND_GOOD** |
| `Mt5Order` | none | **MISSING** |
| `Mt5Event` | none | **MISSING** |

Allowed extras vs the sketch (architecture: “exact interface may be adjusted”): `BrokerCode`, `IsConnectedAsync`, `GetAccountsAsync(string? group)`, `IReadOnlyList` instead of `IReadOnlyCollection`, `long` logins, `DateTimeOffset` deal window. Those are reasonable SDK adjustments.

### 3.2 Completeness vs the written §6 interface

| ID | Needed member / type | In Application | Class | Notes |
|---|---|---|---|---|
| B1 | `IMt5BrokerConnector` port | yes | **EXISTS_NEEDS_REFACTOR** | Present and referenced. Incomplete vs sketch (orders + subscribe). |
| B1a | `ConnectAsync` / `DisconnectAsync` | yes | **EXISTS_AND_GOOD** | `DealIngestionService` connects and **never disconnects**. |
| B1b | `GetGroupsAsync` | yes | **EXISTS_AND_GOOD** | Used to upsert every group the connector returns (dynamic enumerate). |
| B1c | `GetAccountsAsync` | yes | **EXISTS_AND_GOOD** | Called with `group: null` (all accounts). |
| B1d | `GetDealsAsync(login, from, to)` | yes | **EXISTS_AND_GOOD** | Window supplied by caller, not by checkpoint. |
| B1e | `GetPositionsAsync` | yes | **EXISTS_AND_GOOD** | |
| B1f | `GetOrdersAsync` | no | **MISSING** | Named in §6. No `Mt5OrderDto` anywhere under `D:\Prop\src`. |
| B1g | `SubscribeAsync` / live events | no | **MISSING** | Required for §12 live loop. |
| B2 | Broker registry | `IBrokerRegistry` | **EXISTS_AND_GOOD** | Lookup by `BrokerCode`; `All()`. Implementation is `TraderIntelligence.Mt5.Connectors.BrokerRegistry` (correct layer). |
| B3 | Two named brokers, one connector type | DI + demo factory | **EXISTS_NEEDS_REFACTOR** (implementation, not Application) | `DependencyInjection` registers two `FakeMt5BrokerConnector` instances (`ACHIEVER`, `STARWAVEFX`). **No live Manager connector.** Application itself is correctly broker-agnostic. |
| B4 | Do not build two connector codebases / two ports | — | **EXISTS_NEEDS_REFACTOR** | Parallel unused `IBrokerConnector` in `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` (Domain entities, `SubscribeEventsAsync`, `GetServerTimeAsync`, `GetOrders` still missing). Grep: **definition only, zero implementers/callers.** A58 already marked this **DEPRECATED**. It is still on disk. |

### 3.3 §6 verdict

**PARTIAL / EXISTS_NEEDS_REFACTOR.** Application now owns the registry + collector port A02 said was missing. That is real progress. It is **not** complete: no orders, no subscribe, only fake connectors behind the port, and a leftover second interface in `src/Mt5`.

Measured member coverage of the §6 sketch (8 members): **6/8 present** (`Connect`, `Disconnect`, `GetGroups`, `GetAccounts`, `GetDeals`, `GetPositions`). **2/8 missing** (`GetOrders`, `Subscribe`). Extra members do not count against completeness.

---

## 4. Section 12 — Ingestion Pattern

### 4.1 What Application actually does

`DealIngestionService.SyncBrokerAsync`:

```31:59:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        foreach (var group in groups)
            await _store.UpsertGroupAsync(brokerId, group, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        foreach (var account in accounts)
        {
            await _store.UpsertAccountAsync(brokerId, account, now, ct);
            var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
            foreach (var deal in deals)
            {
                if (await _store.UpsertDealAsync(brokerId, deal, now, ct))
                    insertedDeals++;
            }

            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }

        return insertedDeals;
    }
```

`ITradingStore` is the persistence port:

```8:18:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
public interface ITradingStore
{
    Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct);
    Task UpsertAccountAsync(Guid brokerId, Mt5AccountDto account, DateTimeOffset now, CancellationToken ct);
    Task<bool> UpsertDealAsync(Guid brokerId, Mt5DealDto deal, DateTimeOffset now, CancellationToken ct);
    Task ReplacePositionsAsync(Guid brokerId, long login, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct);
    Task<IReadOnlyList<NormalizedDeal>> LoadDealsAsync(Guid brokerId, string brokerCode, long login, CancellationToken ct);
    Task ReplaceReconstructedAsync(Guid brokerId, long login, IReadOnlyList<ReconstructedTradeResult> trades, CancellationToken ct);
    Task UpsertScoreAsync(TraderScore score, CancellationToken ct);
    Task<Guid> ResolveBrokerIdAsync(string brokerCode, CancellationToken ct);
}
```

Hosts call this as a **30-second poll** (`apps/mt5-worker/Worker.cs`) and as a manual `/api/ops/resync`. Both then immediately call `ReconstructionScoringService.RebuildTraderAsync` for a **hard-coded login list** (`10001, 10002, 10003, 99001`).

### 4.2 Three-loop mandate

| ID | §12 loop | Application type | Class | Evidence |
|---|---|---|---|---|
| I1 | Historical Backfill | `DealIngestionService.SyncBrokerAsync` | **EXISTS_NEEDS_REFACTOR** | Fetches + upserts groups/accounts/deals and replaces positions. **Does not** read/persist checkpoints. Window is caller-supplied (`AddDays(-30)` in the worker, or `2026-01-01` in `/api/ops/resync`). Not incremental. |
| I2 | Live Event Subscription | — | **MISSING** | No subscribe on `IMt5BrokerConnector`. No `ILiveIngestionService`. No event DTO. |
| I3 | Periodic Reconciliation | — | **MISSING** | No `IIngestionReconciliationService`. Do not confuse destination `/api/reconciliation/status` (hard-coded zeros in the API) or FIX venue reconcile (§42) with source-vs-raw reconcile. Polling `SyncBrokerAsync` is not a reconcile job. |

### 4.3 Historical backfill steps (per broker/account)

| ID | Step | Class | Evidence |
|---|---|---|---|
| H1 | Read checkpoint | **MISSING** | Domain `SyncCheckpoint` + EF `sync_checkpoints` exist. `ITradingStore` has no checkpoint members. Application never reads them. |
| H2 | Fetch history | **EXISTS_AND_GOOD** | Per-account `GetDealsAsync(login, from, to)`. Also fetches groups + accounts + positions. |
| H3 | Normalize | **MISSING** in this service | No symbol/volume normalize on the ingest path. `NormalizedDeal` is built later inside `EfTradingStore.LoadDealsAsync` for reconstruction, not before raw upsert. Architecture: persist raw first (§11), then interpret. Raw persist without a named normalize step is acceptable **if** later reconstruction owns normalize — but §12 still lists Normalize as a backfill box. No Application type implements that box. |
| H4 | Upsert idempotently | **EXISTS_NEEDS_REFACTOR** | `UpsertDealAsync` returns `false` if `(brokerId, dealTicket)` exists (insert-only). Positions are **wipe-and-replace**, not upsert. Groups/accounts update some fields. Each call `SaveChangesAsync` independently (Infrastructure) — not one backfill transaction. No `ingestion_events` writer. No orders table writer. |
| H5 | Persist checkpoint | **MISSING** | Same as H1. A crash mid-window re-fetches; insert-only deals make that mostly safe, but the cursor never advances and the worker always re-asks 30 days. |

### 4.4 Live flow steps

| ID | Step | Class | Evidence |
|---|---|---|---|
| L1 | MT5 event | **MISSING** | No subscribe port. |
| L2 | validate | **MISSING** | `FluentValidation` unused. No deal/account validators. |
| L3 | deduplicate | **MISSING** as a live step | Deal insert-if-exists is a persistence uniqueness check, not a live-path dedupe of `ingestion_events`. |
| L4 | persist raw record | **MISSING** on a live path | Only the poll upsert exists. |
| L5 | write transactional outbox event | **MISSING** | Domain `OutboxEvent` + `OutboxEventType` + EF `outbox_events` exist. Application has no `IOutboxWriter` / `ITransactionalOutbox`. `ITradingStore` cannot write outbox rows. |
| L6 | commit (single transaction) | **MISSING** | Each upsert is its own `SaveChangesAsync`. No unit-of-work / ambient transaction port. |
| L7 | background workers process the outbox | **MISSING** | `mt5-worker` calls ingestion + scoring directly. `fix-worker` pokes `FixSessionState` timestamps and **refuses** `NewOrderSingle`. Neither drains `outbox_events`. |
| L8 | Do not couple MT5 callbacks to ML or execution | **EXISTS_NEEDS_REFACTOR** (violated in spirit) | There is no callback, so the letter of “callback” is vacuously unmet rather than broken. The **worker loop** still does ingest then `BaselineScorer` in the same cycle. That is the coupling §12 exists to prevent. |

`ReconstructionScoringService` (same file) loads deals, reconstructs, replaces reconstructed trades, scores, and upserts `TraderScore`. It is an Application use-case, but it belongs to reconstruction/scoring — not to the §12 ingest transaction. Hosts treating it as “step 2 of ingest” is a completeness failure against §12.

### 4.5 Implied Application ports still missing (from §12 + A02/A59)

| ID | Needed Application type | Class |
|---|---|---|
| O1 | `IHistoricalBackfillService` with checkpointed window | **EXISTS_NEEDS_REFACTOR** as `DealIngestionService` (wrong name, no checkpoint) |
| O2 | `ISyncCheckpointStore` | **MISSING** (entity only, WRONG_LAYER in Domain/EF) |
| O3 | `ILiveIngestionService` | **MISSING** |
| O4 | Raw writer covering deals **and** orders **and** account snapshots **and** ingestion_events | **EXISTS_NEEDS_REFACTOR** (`ITradingStore` is a grab-bag: raw + reconstructed + scores) |
| O5 | `IIngestionReconciliationService` | **MISSING** |
| O6 | `ITransactionalOutbox` / `IOutboxWriter` | **MISSING** |
| O7 | Typed outbox payloads for the five §13 kinds | **MISSING** (enum exists in Domain: `TradeCompleted`, `ScoreUpdate`, `ShadowCopyIntent`, `RiskCheckRequest`, `NotificationEvent`) |
| O8 | `IOutboxProcessor` | **MISSING** |
| O9 | `IEventBus` seam (outbox now, Kafka later) | **MISSING** |

§13 is **not** in the requested section set. It is cited only because §12’s live box is “write transactional outbox event.” Without that port, §12 live cannot be completed.

### 4.6 §12 verdict

**FAIL.** One of three loops exists, and that one is a full-window poll with insert-only deals and snapshot replace of positions. Checkpoints, live subscribe, validate, outbox, single commit, and source reconcile are absent from Application. Hosts couple scoring to ingest.

Approximate step coverage (I1–I3 + H1–H5 + L1–L8 = 16 boxes): **2 good** (H2 fetch; partial H4 upsert), **3 needs-refactor** (I1, H4, L8), **11 missing**.

---

## 5. Section 32 — FIX Trade Execution Flow

Application contains **no** type whose name or members mention copy candidate, CopyIntent, ExecutionIntent, FIX send, fills, or destination positions.

Domain **does** have persistence-shaped types the flow would use (`CopyIntent`, `ExecutionIntent`, `RiskDecisionRecord`, `ShadowOrder`) and a pure `CopyIntentExpiry.IsExpired`. EF maps `copy_intents`, `execution_intents`, `risk_decisions`. **Nothing in Application creates, evaluates, or persists them.** `ITradingStore` cannot write them. `fix-worker` does not consume `ExecutionIntent`.

| ID | §32 box | Needed Application type | Class | Evidence |
|---|---|---|---|---|
| C1 | Copy candidate? | `ICopyCandidateEvaluator` | **MISSING** | No gate after a source event. Scoring writes `TraderState` (WATCH/SHADOW/…) but no Application service asks “is this event a copy candidate?” |
| C2 | Create CopyIntent | `ICopyIntentFactory` / create use-case | **MISSING** | Domain entity has `ExpiresAt`, `IdempotencyKey`, `ExpectedPrice`, `SourceEventTime`. No factory. |
| C3 | Persist CopyIntent | `ICopyIntentStore` | **MISSING** | EF `DbSet<CopyIntent>` only. |
| C4 | RiskEngine evaluates | Application port wrapping Domain engine | **MISSING** | `TraderIntelligence.Domain.Risk.RiskEngine` exists. Application does not reference it. No DI registration. No tests. |
| C5 | ApprovedExecutionIntent | `IApprovedExecutionIntentService` | **MISSING** | Domain `ExecutionIntent` exists (`ClOrdId`, `CopyIntentId`, `RiskDecisionId`, qty, side, status). No Application writer. |
| C6 | Persist execution intent (before send) | `IExecutionIntentStore` | **MISSING** | Adjacent §33 fields (`source_event_id`, “sent but acknowledgement unknown”, etc.) are not all on the entity; Application adds no contract. |
| C7 | FIX Execution Worker (port) | `IFixExecutionWorker` or “consume approved intent” use-case | **MISSING** | `apps/fix-worker/Worker.cs` updates session heartbeats and logs that it **refuses** `NewOrderSingle`. It does not read Application ports. |
| C8 | Persist fills / order state | — | **MISSING** | No Application type. |
| C9 | Update destination position | — | **MISSING** | |
| C10 | Reconcile (post-fill) | — | **MISSING** | Destination reconcile is §42–43; still no Application port. |
| C11 | Never send FIX from an MT5 callback | policy | **EXISTS_AND_GOOD** (vacuous) | No MT5 callback and no FIX send in Application. Worker comment: “Execution copy is not performed here.” This is compliance-by-absence, not an implemented guard. |

Shadow copy (§24, adjacent): Domain `ShadowCopyEngine` exists; Application has no shadow use-case; `IDashboardQueries` exposes `ShadowPnl` as a read model only.

### 5.1 §32 verdict

**FAIL / MISSING.** The production flow is specified end-to-end and is not present as Application orchestration. Persistence tables and a Domain risk function are not a substitute for “Create CopyIntent → persist → risk → persist ApprovedExecutionIntent → worker.”

Boxes C1–C10: **0/10 implemented** in Application. C11 holds only because nothing executes.

---

## 6. Section 39 — Risk Engine

### 6.1 What Application has that looks like “risk”

Only a **dashboard read model**:

```78:97:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public sealed record RiskDashboardDto(
    decimal DailyPnl,
    decimal Drawdown,
    decimal XauLong,
    decimal XauShort,
    decimal XauNet,
    string KillSwitch,
    bool RealCopyEnabled,
    IReadOnlyList<string> RecentRejectReasons);

public interface IDashboardQueries
{
    // ...
    Task<RiskDashboardDto> GetRiskAsync(CancellationToken ct);
}
```

That is a §53 page contract, not the §39 engine. Infrastructure fills most numerics with `0` and `RealCopyEnabled: false`.

There is **no** `IRiskEngine`, no `RiskEvaluationRequest` / `RiskDecision` in Application, no hard-limit policy ports, no outbox consumer for `risk-check requests`.

### 6.2 Scoring output vs the §39 triple

§39: Scoring/ML may **only** produce `candidate`, `confidence`, `suggested allocation`.

`ReconstructionScoringService` persists:

```87:101:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            Login = login,
            RiskScore = score.RiskScore,
            BehaviorScore = score.BehaviorScore,
            EarlyQualityScore = score.EarlyQualityScore,
            CompletedXauTrades = score.Features.CompletedXauTrades,
            Martingale = score.Features.Martingale,
            AveragingDown = score.Features.AveragingDown,
            LotEscalation = score.Features.LotEscalation,
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);
```

That is **features + composite scores + trader state**, not the allowed triple. Application is the layer that should constrain the scoring port. It does the opposite: it writes Domain `TraderScore` and lets `BaselineScorer` suggest `TraderState` (`INSUFFICIENT_DATA` … `RISK_BLOCKED`). `TraderStateMachine.CanPromoteToLive` is hard-`false` in Domain (good safety), but Application never asks risk for promotion.

| ID | Needed Application type | Class | Evidence |
|---|---|---|---|
| R1 | `IRiskEngine` (final authority) | **MISSING** | Domain `RiskEngine.Evaluate` is the closest implementation. **WRONG_LAYER** until Application exposes a port and a use-case that **must** run before any execution persist. |
| R2 | Scoring → risk DTO (`candidate`, `confidence`, `suggested allocation`) | **MISSING** | Current Application scoring output violates the constraint. |
| R3 | `RiskDecision` vocabulary | **MISSING** in Application | Domain `RiskDecision` + `RiskDecisionOutcome` already enumerate approve / reduce size / reject / pause trader / pause venue / global stop. |
| R4 | Hard-limit evaluators (one engine, many checks) | **MISSING** in Application | Domain `RiskLimits` lists 14 of 16 §39 limits as fields. `MaxSlippage` is **declared and never read** in `RiskEngine.Evaluate`. Venue health / kill switch / real-exec flag are request fields, not separate Application policy ports. |
| R5 | Outbox consumer `risk-check requests` | **MISSING** | |
| S1 | `IScoringService` limited to the §39 triple | **EXISTS_NEEDS_REFACTOR** as `ReconstructionScoringService` | Wrong output contract; mixed with reconstruction; invoked from ingest hosts. |
| S2 | `IScoreUpdateRequestHandler` (outbox) | **MISSING** | |
| S3 | Score/state write port separate from raw ingest | **EXISTS_NEEDS_REFACTOR** | `ITradingStore.UpsertScoreAsync` shares the raw-ingest port. |

### 6.3 §39 hard-limit checklist (Application ownership)

Application does not evaluate any of these. Domain engine coverage is recorded only so “missing” is not overstated for the **system**:

| §39 hard limit | In Application | In Domain `RiskEngine` (not in scope to certify) |
|---|---|---|
| max loss per selected trader | **MISSING** | used (`TraderRealizedLoss`) |
| max daily execution-account loss | **MISSING** | used |
| max portfolio drawdown | **MISSING** | used |
| max XAUUSD gross exposure | **MISSING** | used |
| max XAUUSD net exposure | **MISSING** | used (returns `ReduceSize`) |
| max position quantity | **MISSING** | used |
| max number of open positions | **MISSING** | used |
| max allowed spread | **MISSING** | used |
| max quote age | **MISSING** | used |
| max source-signal age | **MISSING** | used |
| max tolerated price move | **MISSING** | used |
| max slippage | **MISSING** | field only, **not applied** |
| max execution account margin usage | **MISSING** | used |
| martingale block | **MISSING** | used (`PauseTrader`) |
| abnormal sizing block | **MISSING** | used |
| venue health requirement | **MISSING** | used (`PauseVenue`) |

Kill switch / `REAL_COPY_EXECUTION_ENABLED` are §40–41 (adjacent). Domain `RiskEngine` reads `KillSwitchMode` and `RealExecutionEnabled`. Application has no `IKillSwitch` port. Dashboard surfaces kill-switch **mode as a string**.

### 6.4 §39 verdict

**FAIL / MISSING** as an Application concern. The Domain function is a start on the **algorithm**, not on “risk is the final authority in the production flow.” Nothing in Application invokes it. Scoring is not constrained to the allowed triple. There is no path `CopyIntent → RiskEngine → ApprovedExecutionIntent`.

---

## 7. Extra Application surface (not in §§6, 12, 32, 39)

`IDashboardQueries` + six DTOs are a real Application port for §§46–53. They are **out of the scored section set**. Honest note so the layer is not described as “ingest-only”:

- Overview / brokers / groups / traders / trader detail / FIX sessions / risk page **query** shapes exist.
- API maps them 1:1 (`/api/overview`, `/api/brokers`, `/api/groups`, `/api/traders`, `/api/fix/sessions`, `/api/risk`).
- Several §47/§48 fields are absent from the DTOs (destination free margin / margin level, deal ingest rate, pool usage, reconnect count on brokers).
- This does **not** complete §32 or §39.

`ITradingStore` mixing raw upserts, reconstruction replace, and score upsert is a layering smell: one port spanning §12 raw, §14 reconstruction, and scoring writes.

---

## 8. Duplicate / bypass contracts (Application completeness impact)

| Item | Path | Impact on Application |
|---|---|---|
| Unused `IBrokerConnector` | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | Second collector shape. Has `SubscribeEventsAsync` and `GetServerTimeAsync` that Application’s `IMt5BrokerConnector` lacks. **DEPRECATED** leftover. Completing §6/§12 on the Application port must not fork this interface. |
| Fake-only registry fill | `DemoBrokerFactory` + `AddTraderIntelligence` | Application registry is real; the **only** implementers are in-memory fakes. §6 “Achiever / StarwaveFX” as live sources is not an Application defect, but Application has no `IMt5BrokerConnectorFactory` / options bind port either. |
| Domain `RiskEngine` / `ShadowCopyEngine` / `BaselineScorer` | `src/Domain` | Engines without Application ports. Hosts/DI reach `BaselineScorer` through `ReconstructionScoringService`; risk/shadow are unused. |
| EF tables without Application writers | `outbox_events`, `sync_checkpoints`, `copy_intents`, `execution_intents`, `risk_decisions` | Persistence ahead of use-cases. Application completeness cannot be inferred from `TraderDbContext`. |
| Hosts bypass Application for some reads | `apps/api/Program.cs` `/api/trades` queries `TraderDbContext` directly | Dashboard port is incomplete; not a §6/12/32/39 fail by itself. |

---

## 9. Gap matrix vs the four requested sections

| § | Architecture mandate (abbrev.) | In `src/Application` | Class |
|---|---|---|---|
| 6 | `IMt5BrokerConnector` + broker registry; one business path; orders + subscribe | Registry + partial connector + DTOs; used by `DealIngestionService`. No orders, no subscribe. Parallel unused `IBrokerConnector` in Mt5. | **EXISTS_NEEDS_REFACTOR** |
| 12 | Backfill + live subscribe + periodic reconcile; checkpoint; validate/dedupe/raw/outbox/commit; no MT5→ML coupling | Poll upsert of groups/accounts/deals + position replace. No checkpoint, live, validate, outbox, reconcile. Scoring in the same worker cycle. | **EXISTS_NEEDS_REFACTOR** (backfill stub) + **MISSING** (live, reconcile, outbox) |
| 32 | CopyIntent persist → RiskEngine → ApprovedExecutionIntent persist → FIX worker; never FIX from MT5 callback | No copy/execution use-cases. Vacuous “no FIX from callback.” | **MISSING** |
| 39 | Risk is final authority; scoring emits only candidate/confidence/allocation; enumerated hard limits | No `IRiskEngine`. Dashboard `GetRiskAsync` is a read model. Scoring writes features/scores/state. | **MISSING** |

### 9.1 Counts (requested scope only)

Scored Application items (B1–B4, I1–I3, H1–H5, L1–L8, C1–C11, R1–R5, S1–S3, P1–P6):

| Class | Count | What |
|---|---:|---|
| **EXISTS_AND_GOOD** | 10 | P1–P3; B1a–B1e; B2; H2; C11 (vacuous) |
| **EXISTS_NEEDS_REFACTOR** | 10 | P4 FluentValidation; B1 connector; B3 fakes; B4 duplicate port; I1 backfill; H4 upsert; L8 coupling; O1/O4/S1/S3-style mixed store + scoring service |
| **MISSING** | 28 | B1f–B1g; I2–I3; H1, H3, H5; L1–L7; C1–C10; R1–R5; S2; P6 tests; plus implied O2/O3/O5–O9 if counted separately |
| **DEPRECATED** | 0 **in Application** | `Class1` removed. Unused `IBrokerConnector` is **outside** Application. |
| **UNSAFE** | 0 | No secrets, no live send. |

If the implied outbox/checkpoint ports O2–O9 are included (they are required to finish §12 live): **MISSING +8**.

Honest rolled-up completeness (equal weight on the four sections, not lines of code):

| Section | Completeness | Basis |
|---|---:|---|
| §6 | **~55%** | 6/8 sketch members + registry + one ingest path; no orders/subscribe; fakes only |
| §12 | **~20%** | 1/3 loops; 1/5 backfill steps good; 0/7 live steps |
| §32 | **~5%** | only the negative constraint “do not FIX from callback” |
| §39 | **~0%** | no Application engine/port; scoring contract wrong |
| **Four-section mean** | **~20%** | not a test measurement; a checklist fraction |

This is **not** “≥95%.” A02’s “name-only layer” is no longer true; the layer is a **thin ingest + dashboard + scoring driver**, not the architecture’s Application layer.

---

## 10. Honest verdict

`TraderIntelligence.Application` is no longer an empty `Class1` project. It has:

1. A usable §6 **port + registry** (`IMt5BrokerConnector`, `IBrokerRegistry`, four DTOs).
2. A **poll-and-upsert** use-case (`DealIngestionService`) and a persistence façade (`ITradingStore`).
3. A **reconstruction + baseline score** use-case that hosts run immediately after ingest.
4. A **dashboard query** port used by the API.

It does **not** implement architecture v2 §§6, 12, 32, 39 as specified:

- §6 is the only section with a real Application contract, and it is missing live subscribe and orders.
- §12 is a single polling loop without checkpoints, validation, outbox, or source reconcile, and it is coupled to scoring.
- §32 has no Application flow. Copy/execution tables are unused.
- §39 has no Application authority. Domain `RiskEngine` is dark. Scoring emits more than it is allowed to emit.

**PASS/FAIL for architecture v2 §§6, 12, 32, 39: FAIL.**

### 10.1 Next Application work (when implementation is authorized; not done here)

Priority order that matches the four sections and does not invent sibling empty projects (§66):

1. Extend `IMt5BrokerConnector` with `GetOrdersAsync` + `SubscribeAsync` (or an explicit `IMt5Event` stream). Delete or fold `IBrokerConnector` so there is **one** port.
2. Split `ITradingStore`: raw ingest vs reconstruction vs scores. Add `ISyncCheckpointStore` and actually drive `from/to` from `sync_checkpoints`. Persist checkpoint only after a complete page/fetch.
3. Add `ITransactionalOutbox` and a live ingest use-case: validate (wire FluentValidation) → dedupe → raw persist + outbox → one commit. Move `ReconstructionScoringService` to an outbox consumer (`ScoreUpdate`).
4. Add `IIngestionReconciliationService` (source history vs `mt5_*`), distinct from destination FIX reconcile.
5. Add copy ports: `ICopyCandidateEvaluator`, `ICopyIntentStore`, `IRiskEngine` (wrap Domain), `IExecutionIntentStore`. Scoring port must emit only `candidate / confidence / suggested allocation`. Risk remains the only approver of size/send.
6. Add `IFixExecutionWorker` as a port; keep QuickFIX in `Fix.CTrader`. Still never send from an MT5 callback.

Do not treat Domain `RiskEngine` + EF `copy_intents` as Application completeness. They are prerequisites, not the use-case.

This audit did not modify product source.
