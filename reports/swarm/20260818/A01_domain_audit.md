# A01 — Domain Layer Audit (`src/Domain`)

| Field | Value |
|---|---|
| Agent | A01 (Domain-only) |
| Date | 2026-08-18 |
| Scope | `D:\Prop\src\Domain\**` + `TraderIntelligence.Domain.csproj` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§10, 14, 16, 22, 39, 45, 66 |
| Product source edited | **No** (report only) |
| Method | `list_dir` + `read_file` + `grep` of Domain, solution, consumers, and the named architecture sections. Nothing answered from memory. |

**Overall verdict:** `TraderIntelligence.Domain` is a `dotnet new classlib` scaffold. It is wired into the solution and referenced by Application / Infrastructure / Mt5 / Fix.CTrader / Api / workers / tests, but it contains **zero** domain types. Every architecture concept required by §§10, 14, 16, 22, 39, 45 is **MISSING**. `Class1.cs` is a **dead template**.

Layer classification: **EXISTS_NEEDS_REFACTOR** (folder + project exist; contents are template-only).

---

## 1. Current files (source of truth)

`list_dir D:\Prop\src\Domain` returned only:

| Path | Role | Classification |
|---|---|---|
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | SDK-style net8.0 class library | **EXISTS_AND_GOOD** (as a *skeleton* only) |
| `D:\Prop\src\Domain\Class1.cs` | Empty public class, no members | **DEPRECATED** (dead `dotnet new` template) |
| `D:\Prop\src\Domain\bin\**` | Debug build outputs | build artifact, ignore |
| `D:\Prop\src\Domain\obj\**` | MSBuild / restore cache | build artifact, ignore |

**There are no folders** `Entities/`, `ValueObjects/`, `Enums/`, `Identifiers/`, `Aggregates/`, `Events/`, `Repositories/`, or `Exceptions/`.

**There are no** `.cs` files other than `Class1.cs`.

`grep namespace TraderIntelligence` across `D:\Prop` found exactly one Domain source type:

```
D:\Prop\src\Domain\Class1.cs
```

Sibling `src` projects (`Application`, `Infrastructure`, `Mt5`, `Fix.CTrader`) also contain only their own `Class1.cs`. Domain types have **not** leaked into other layers; they simply do not exist anywhere.

---

## 2. `Class1.cs` — dead template? **YES**

Full file (`D:\Prop\src\Domain\Class1.cs`):

```csharp
namespace TraderIntelligence.Domain;

public class Class1
{

}
```

Evidence it is dead template, not a stub entity:

1. Empty body. No properties, methods, interfaces, XML docs, or TODOs.
2. Name `Class1` is the default `dotnet new classlib` filename. Same empty class exists in:
   - `D:\Prop\src\Application\Class1.cs` (`namespace TraderIntelligence.Application`)
   - `D:\Prop\src\Infrastructure\Class1.cs`
   - `D:\Prop\src\Mt5\Class1.cs`
   - `D:\Prop\src\Fix.CTrader\Class1.cs`
3. `grep Class1` over `*.cs` under `D:\Prop` hits **only those five declarations**. No consumer references `TraderIntelligence.Domain.Class1`.
4. Compile list (`obj\Debug\net8.0\TraderIntelligence.Domain.csproj.FileListAbsolute.txt`) contains only the assembly + `Class1` compile outputs. No other types were ever compiled.

**Action:** delete `Class1.cs` when the first real Domain type is added. Do not rename it. Do not put business fields on it.

Classification: **DEPRECATED**.

---

## 3. Project file assessment

Full file (`D:\Prop\src\Domain\TraderIntelligence.Domain.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

| Check | Result |
|---|---|
| Target framework | `net8.0` — matches rest of solution |
| Nullable | enabled — good |
| Implicit usings | enabled — acceptable; generated usings are BCL only (`System`, `Linq`, `Http`, `Threading`) |
| PackageReference | **none** — correct for a pure Domain project |
| ProjectReference | **none** — correct (Domain is the innermost ring) |
| `InternalsVisibleTo` | absent (add later for unit tests if internals are used) |
| Root namespace / folders | default = project name. No folder conventions encoded |

`obj\project.assets.json` confirms:

```json
"projectFileDependencyGroups": {
  "net8.0": []
}
```

and `"projectReferences": {}`.

**Keep this csproj.** Do not add EF, FIX, Redis, Serilog, or ASP.NET packages here. Persistence mappings belong in Infrastructure.

Classification: **EXISTS_AND_GOOD** (skeleton). Contents of the project: **MISSING**.

---

## 4. Solution wiring (not Domain content, but relevant)

`D:\Prop\Mt5TraderIntelligence.sln` includes:

```
Project(...) = "TraderIntelligence.Domain", "src\Domain\TraderIntelligence.Domain.csproj", "{A70A2194-62B9-4B2E-96DB-9725BEA5D9D7}"
```

**Inbound references** (Domain is already the shared kernel):

| Consumer | Evidence |
|---|---|
| `src\Application\TraderIntelligence.Application.csproj` | `ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj"` |
| `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | `ProjectReference Include="..\Domain\..."` |
| `src\Mt5\TraderIntelligence.Mt5.csproj` | `ProjectReference Include="..\Domain\..."` |
| `src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | `ProjectReference Include="..\Domain\..."` |
| `apps\api\TraderIntelligence.Api.csproj` | `ProjectReference Include="..\..\src\Domain\..."` |
| `apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | `ProjectReference Include="..\..\src\Domain\..."` |
| `apps\fix-worker\TraderIntelligence.FixWorker.csproj` | `ProjectReference Include="..\..\src\Domain\..."` |
| `tests\Unit\TraderIntelligence.Tests.Unit.csproj` | `ProjectReference Include="..\..\src\Domain\..."` |
| `tests\Integration\TraderIntelligence.Tests.Integration.csproj` | `ProjectReference Include="..\..\src\Domain\..."` |

§66 (`/src/Domain`) is **partially satisfied** at the *folder/project* level. The Domain *model* is empty.

§66 also lists sibling projects that do **not** exist yet under `D:\Prop\src`:

```
/TradeReconstruction
/Scoring
/Shadow
/Risk
/Execution
```

Those are out of Domain-file scope, but they are the intended *homes for services* that will **consume** Domain types. Until Domain exists, those projects cannot be implemented honestly.

---

## 5. Classification of every architecture concept in scope

Legend (architecture §73): `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

### 5.1 Layer / project

| Component | Classification | Notes |
|---|---|---|
| `src/Domain` folder | **EXISTS_AND_GOOD** | Matches §66 `/src/Domain` |
| `TraderIntelligence.Domain.csproj` | **EXISTS_AND_GOOD** | net8, nullable, zero deps |
| Domain model (entities / VOs / enums) | **MISSING** | empty |
| `Class1.cs` | **DEPRECATED** | dead template |
| Domain-level unsafe code / secrets | **n/a** | no implementation that could be `UNSAFE` |
| Identity-safety *encoding* (compound IDs) | **MISSING** | safety gap, not an unsafe implementation |

Nothing in Domain is `UNSAFE`. The danger is *absence*: if Application/Mt5 later invent `login`-only keys, they will violate §10. Domain must own the compound IDs **before** ingestion or EF mappings are written.

### 5.2 §10 Multi-Broker Identity Rules — all **MISSING**

Architecture:

```text
broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id
```

and “All source-side tables must carry `broker_id`.”

| Type | Kind | Classification |
|---|---|---|
| `BrokerId` | identifier / VO | **MISSING** |
| `LoginId` | identifier / VO | **MISSING** |
| `BrokerLoginId` (`broker_id + login`) | compound identity | **MISSING** |
| `BrokerDealTicket` (`broker_id + deal_ticket`) | compound identity | **MISSING** |
| `BrokerOrderTicket` (`broker_id + order_ticket`) | compound identity | **MISSING** |
| `BrokerPositionId` (`broker_id + position_id`) | compound identity | **MISSING** |
| Invariant: source entities always carry `BrokerId` | domain rule | **MISSING** |

### 5.3 §14 Trade Reconstruction — all **MISSING**

Architecture: `Order != Deal != Position != Logical Trade` and canonical `ReconstructedTrade` with fields `id, broker_id, login, position_id, canonical_symbol, source_symbol, direction, opened_at, closed_at, entry_vwap, exit_vwap, initial_volume, max_volume, closed_volume, gross_realized_pnl, commission, swap, fees, net_realized_pnl, deal_count, order_count, initial_sl, initial_tp, final_sl, final_tp, was_scaled_in, was_partial_close, was_averaged_down, completed`.

| Type | Kind | Classification |
|---|---|---|
| `Mt5Order` | entity (raw) | **MISSING** |
| `Mt5Deal` | entity (raw) | **MISSING** |
| `Mt5PositionCurrent` | entity (raw) | **MISSING** |
| `ReconstructedTrade` | aggregate / entity | **MISSING** |
| `TradeDirection` | enum | **MISSING** |
| `Volume` / `Vwap` / `Money` / `Price` | value objects | **MISSING** |
| `CompletedXauUsdLifecycle` rule (used by §15 “first 3 trades”) | domain service / spec | **MISSING** |

### 5.4 §16 XAUUSD Symbol Normalization — all **MISSING**

Architecture: `CanonicalInstrument XAUUSD` plus mappings `broker/source symbol → canonical XAUUSD` and `cTrader instrument ID → canonical XAUUSD`. “Never assume FIX tag 55 is the string `"XAUUSD"`.” Persist the mapping.

| Type | Kind | Classification |
|---|---|---|
| `CanonicalInstrument` | entity | **MISSING** |
| `CanonicalSymbol` (value, e.g. `XAUUSD`) | value object | **MISSING** |
| `SourceSymbol` | value object | **MISSING** |
| `SourceSymbolMapping` | entity | **MISSING** |
| `DestinationInstrumentId` (cTrader numeric) | value object | **MISSING** |
| `DestinationSymbol` / `DestinationSymbolMapping` | entity | **MISSING** |

### 5.5 §22 Continuous Rescoring — all **MISSING**

Architecture suggested states:

```text
INSUFFICIENT_DATA
EARLY_SCORE
WATCH
SHADOW
LIVE_CANDIDATE
LIVE
PAUSED
RISK_BLOCKED
DISQUALIFIED
```

Plus “Trade #3 is the first score” then rescore after 4, 5, 6… and “Maintain score history.”

| Type | Kind | Classification |
|---|---|---|
| `TraderLifecycleState` | enum | **MISSING** |
| `TraderState` | entity | **MISSING** |
| `TraderScore` | entity | **MISSING** |
| `TraderScoreHistory` | entity | **MISSING** |
| `TraderFeatureSnapshot` | entity | **MISSING** |
| `ScoreEligibility` (`EARLY_SCORE_ELIGIBLE` vs not `PROVEN_PROFITABLE`) | enum | **MISSING** |
| `TraderRiskFlag` | entity | **MISSING** |

### 5.6 §39 Risk Engine — all **MISSING**

Scoring/ML may only produce `candidate / confidence / suggested allocation`. Risk decides:

```text
approve
reduce size
reject
pause trader
pause venue
global stop
```

Hard limits listed in §39 (max loss per trader, max daily execution-account loss, max portfolio drawdown, max XAUUSD gross/net exposure, max position quantity, max open positions, max spread, max quote age, max source-signal age, max price move, max slippage, max margin usage, martingale block, abnormal sizing block, venue health).

§40 (adjacent, required by risk model): `STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN` must not be conflated.

| Type | Kind | Classification |
|---|---|---|
| `RiskCandidate` / score output VO | value object | **MISSING** |
| `RiskDecisionAction` | enum | **MISSING** |
| `RiskDecision` | entity / aggregate | **MISSING** |
| `RiskEvent` | entity | **MISSING** |
| `RiskLimits` | value object / policy | **MISSING** |
| `KillSwitchMode` | enum | **MISSING** |
| Martingale / abnormal-sizing block rules | domain specs | **MISSING** |

### 5.7 §45 Recommended Core Database Tables → Domain entities — all **MISSING**

§45 is the persistence *projection* of the Domain model. None of these types exist under `src/Domain`. Persistence (EF) must not invent them first.

| §45 table | Recommended Domain type | Classification |
|---|---|---|
| `brokers` | `Broker` | **MISSING** |
| `broker_connections` | `BrokerConnection` | **MISSING** |
| `mt5_groups` | `Mt5Group` | **MISSING** |
| `plan_group_mappings` | `PlanGroupMapping` | **MISSING** |
| `mt5_accounts` | `Mt5Account` | **MISSING** |
| `mt5_account_snapshots` | `Mt5AccountSnapshot` | **MISSING** |
| `mt5_orders` | `Mt5Order` | **MISSING** |
| `mt5_deals` | `Mt5Deal` | **MISSING** |
| `mt5_positions_current` | `Mt5PositionCurrent` | **MISSING** |
| `mt5_symbols` | `Mt5Symbol` | **MISSING** |
| `mt5_xau_ticks` | `Mt5XauTick` | **MISSING** |
| `reconstructed_trades` | `ReconstructedTrade` | **MISSING** |
| `canonical_instruments` | `CanonicalInstrument` | **MISSING** |
| `source_symbol_mappings` | `SourceSymbolMapping` | **MISSING** |
| `trader_feature_snapshots` | `TraderFeatureSnapshot` | **MISSING** |
| `trader_scores` | `TraderScore` | **MISSING** |
| `trader_score_history` | `TraderScoreHistory` | **MISSING** |
| `trader_states` | `TraderState` | **MISSING** |
| `trader_risk_flags` | `TraderRiskFlag` | **MISSING** |
| `model_versions` | `ModelVersion` | **MISSING** |
| `model_predictions` | `ModelPrediction` | **MISSING** |
| `model_evaluations` | `ModelEvaluation` | **MISSING** |
| `shadow_orders` | `ShadowOrder` | **MISSING** |
| `shadow_fills` | `ShadowFill` | **MISSING** |
| `shadow_positions` | `ShadowPosition` | **MISSING** |
| `shadow_performance` | `ShadowPerformance` | **MISSING** |
| `copy_intents` | `CopyIntent` | **MISSING** |
| `copy_allocations` | `CopyAllocation` | **MISSING** |
| `risk_decisions` | `RiskDecision` | **MISSING** |
| `risk_events` | `RiskEvent` | **MISSING** |
| `execution_venues` | `ExecutionVenue` | **MISSING** |
| `destination_symbols` | `DestinationSymbol` | **MISSING** |
| `destination_quotes` | `DestinationQuote` | **MISSING** |
| `fix_sessions` | `FixSession` | **MISSING** |
| `fix_session_events` | `FixSessionEvent` | **MISSING** |
| `fix_orders` | `FixOrder` | **MISSING** |
| `fix_execution_reports` | `FixExecutionReport` | **MISSING** |
| `destination_positions` | `DestinationPosition` | **MISSING** |
| `source_destination_links` | `SourceDestinationLink` | **MISSING** |
| `sync_checkpoints` | `SyncCheckpoint` | **MISSING** |
| `outbox_events` | `OutboxEvent` | **MISSING** |
| `audit_logs` | `AuditLog` | **MISSING** |
| `system_events` | `SystemEvent` | **MISSING** |

§44 also names `execution_intents` / `execution_reconciliation_*` — **MISSING** (`ExecutionIntent`, `ExecutionReconciliationRun`, `ExecutionReconciliationIssue`).

### 5.8 §66 Suggested Repository Structure — Domain slice

| §66 item | Classification |
|---|---|
| `/src/Domain` present | **EXISTS_AND_GOOD** |
| Domain used as innermost ring | **EXISTS_AND_GOOD** (references only; no types) |
| Domain contents matching the rest of §66 (reconstruction, scoring, risk, execution types) | **MISSING** |
| `/docs/architecture.md` etc. (docs, not Domain) | out of scope; `D:\Prop\docs\` is empty |

---

## 6. Missing entities / enums / value objects (inventory)

### Enums (Domain)

- `TradeDirection` — buy/sell for reconstructed + copy + FIX side
- `TraderLifecycleState` — exact §22 list (do not invent synonyms)
- `ScoreEligibility` — `EARLY_SCORE_ELIGIBLE` (and explicitly **not** `PROVEN_PROFITABLE`)
- `RiskDecisionAction` — `Approve`, `ReduceSize`, `Reject`, `PauseTrader`, `PauseVenue`, `GlobalStop`
- `KillSwitchMode` — `StopNewExecution`, `EmergencyFlatten` (never one flag)
- `ExecutionState` — `NotSent`, `SentAckUnknown`, `Accepted`, `PartiallyFilled`, `Filled`, `Rejected`, `Cancelled`, `Unknown` (`EXECUTION_STATE_UNKNOWN` from §34)
- `CopyIntentExpiry` policy types as needed (`expires_at`, `max_signal_age` from §63)
- `BrokerKind` / `VenueKind` — source MT5 vs destination cTrader (helps §10 + §45 `brokers` / `execution_venues`)

### Value objects / identifiers (Domain)

- `BrokerId`, `LoginId`
- `BrokerLoginId`, `BrokerDealTicket`, `BrokerOrderTicket`, `BrokerPositionId` (§10 — **must be equality-by-both-parts**)
- `CanonicalSymbol` (seed `XAUUSD`), `SourceSymbol`, `DestinationInstrumentId`
- `Money`, `Price`, `Volume`, `Vwap`
- `Spread`, `Slippage`, `SignalAge`, `QuoteAge`
- `RiskLimits` (all §39 hard limits as a single immutable policy object)
- `RiskCandidate` (`candidate`, `confidence`, `suggestedAllocation`)
- `ClOrdId`, `CopyIntentId`, `ExecutionIntentId`, `ReconstructedTradeId`
- `Notional` / sizing intermediates (source volume → canonical notional → dest qty; §38, consumed by risk)

### Entities / aggregates (Domain)

**Identity / brokers:** `Broker`, `BrokerConnection`

**MT5 raw (immutable-as-practical, §11/§45):** `Mt5Group`, `PlanGroupMapping`, `Mt5Account`, `Mt5AccountSnapshot`, `Mt5Order`, `Mt5Deal`, `Mt5PositionCurrent`, `Mt5Symbol`, `Mt5XauTick`

**Reconstruction (§14):** `ReconstructedTrade`

**Normalization (§16):** `CanonicalInstrument`, `SourceSymbolMapping`, `DestinationSymbol`

**Scoring (§22):** `TraderFeatureSnapshot`, `TraderScore`, `TraderScoreHistory`, `TraderState`, `TraderRiskFlag`

**ML tables (§45; keep as data, no training logic):** `ModelVersion`, `ModelPrediction`, `ModelEvaluation`

**Shadow (§45 / §24):** `ShadowOrder`, `ShadowFill`, `ShadowPosition`, `ShadowPerformance`

**Copy / risk / execution:** `CopyIntent` (must include `expires_at`, `max_signal_age`), `CopyAllocation`, `RiskDecision`, `RiskEvent`, `ExecutionVenue`, `DestinationQuote`, `FixSession`, `FixSessionEvent`, `FixOrder`, `FixExecutionReport`, `DestinationPosition`, `SourceDestinationLink`, `ExecutionIntent` (fields from §33)

**Platform:** `SyncCheckpoint`, `OutboxEvent`, `AuditLog`, `SystemEvent`

### Domain services / specs (interfaces live in Domain; implementations later)

- `ITradeReconstruction` / reconstruction invariants (`Order != Deal != Position != LogicalTrade`)
- `ISymbolNormalizer` (source suffix / GOLD / cTrader numeric ID → `XAUUSD`)
- `ICompletedTradeCounter` (only completed reconstructed XAUUSD lifecycles count toward “first 3”)
- `IRiskPolicy` (final authority; scoring cannot approve)
- Kill-switch separation invariant

Do **not** put FIX session I/O, EF DbContext, Redis, or HTTP in Domain.

---

## 7. Exact recommended files to create

Create these under `D:\Prop\src\Domain\`. Delete `Class1.cs` in the same change-set.

Keep `TraderIntelligence.Domain.csproj` as-is (still zero package refs).

### Wave 1 — identity + XAUUSD + reconstruction (unblocks Phase 1–2)

```
D:\Prop\src\Domain\Identifiers\BrokerId.cs
D:\Prop\src\Domain\Identifiers\LoginId.cs
D:\Prop\src\Domain\Identifiers\BrokerLoginId.cs
D:\Prop\src\Domain\Identifiers\BrokerDealTicket.cs
D:\Prop\src\Domain\Identifiers\BrokerOrderTicket.cs
D:\Prop\src\Domain\Identifiers\BrokerPositionId.cs
D:\Prop\src\Domain\Identifiers\ReconstructedTradeId.cs
D:\Prop\src\Domain\Identifiers\ClOrdId.cs
D:\Prop\src\Domain\Identifiers\CopyIntentId.cs
D:\Prop\src\Domain\Identifiers\ExecutionIntentId.cs

D:\Prop\src\Domain\ValueObjects\CanonicalSymbol.cs
D:\Prop\src\Domain\ValueObjects\SourceSymbol.cs
D:\Prop\src\Domain\ValueObjects\DestinationInstrumentId.cs
D:\Prop\src\Domain\ValueObjects\Money.cs
D:\Prop\src\Domain\ValueObjects\Price.cs
D:\Prop\src\Domain\ValueObjects\Volume.cs
D:\Prop\src\Domain\ValueObjects\Vwap.cs
D:\Prop\src\Domain\ValueObjects\Spread.cs
D:\Prop\src\Domain\ValueObjects\Slippage.cs
D:\Prop\src\Domain\ValueObjects\SignalAge.cs
D:\Prop\src\Domain\ValueObjects\QuoteAge.cs

D:\Prop\src\Domain\Enums\TradeDirection.cs
D:\Prop\src\Domain\Enums\TraderLifecycleState.cs
D:\Prop\src\Domain\Enums\ScoreEligibility.cs
D:\Prop\src\Domain\Enums\RiskDecisionAction.cs
D:\Prop\src\Domain\Enums\KillSwitchMode.cs
D:\Prop\src\Domain\Enums\ExecutionState.cs

D:\Prop\src\Domain\Brokers\Broker.cs
D:\Prop\src\Domain\Brokers\BrokerConnection.cs

D:\Prop\src\Domain\Mt5\Mt5Group.cs
D:\Prop\src\Domain\Mt5\PlanGroupMapping.cs
D:\Prop\src\Domain\Mt5\Mt5Account.cs
D:\Prop\src\Domain\Mt5\Mt5AccountSnapshot.cs
D:\Prop\src\Domain\Mt5\Mt5Order.cs
D:\Prop\src\Domain\Mt5\Mt5Deal.cs
D:\Prop\src\Domain\Mt5\Mt5PositionCurrent.cs
D:\Prop\src\Domain\Mt5\Mt5Symbol.cs
D:\Prop\src\Domain\Mt5\Mt5XauTick.cs
D:\Prop\src\Domain\Mt5\SyncCheckpoint.cs

D:\Prop\src\Domain\Instruments\CanonicalInstrument.cs
D:\Prop\src\Domain\Instruments\SourceSymbolMapping.cs
D:\Prop\src\Domain\Instruments\DestinationSymbol.cs

D:\Prop\src\Domain\Reconstruction\ReconstructedTrade.cs
```

**Required invariants on Wave 1 types:**

- Every MT5 raw entity constructor requires `BrokerId`. Equality of tickets/positions is `(BrokerId, ticket)`.
- `ReconstructedTrade` is the only “logical trade.” `completed` is explicit. Partial closes / scale-ins are flags, not extra trades.
- `CanonicalInstrument` seeds `XAUUSD`. Source aliases (`XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`) live only in `SourceSymbolMapping`. cTrader numeric IDs live only in `DestinationSymbol`. No type may assume FIX tag 55 equals `"XAUUSD"`.

### Wave 2 — scoring + risk + copy (unblocks Phase 3, 5, 8)

```
D:\Prop\src\Domain\Scoring\TraderFeatureSnapshot.cs
D:\Prop\src\Domain\Scoring\TraderScore.cs
D:\Prop\src\Domain\Scoring\TraderScoreHistory.cs
D:\Prop\src\Domain\Scoring\TraderState.cs
D:\Prop\src\Domain\Scoring\TraderRiskFlag.cs
D:\Prop\src\Domain\Scoring\ModelVersion.cs
D:\Prop\src\Domain\Scoring\ModelPrediction.cs
D:\Prop\src\Domain\Scoring\ModelEvaluation.cs

D:\Prop\src\Domain\Risk\RiskCandidate.cs
D:\Prop\src\Domain\Risk\RiskLimits.cs
D:\Prop\src\Domain\Risk\RiskDecision.cs
D:\Prop\src\Domain\Risk\RiskEvent.cs
D:\Prop\src\Domain\Risk\KillSwitch.cs

D:\Prop\src\Domain\Copy\CopyIntent.cs
D:\Prop\src\Domain\Copy\CopyAllocation.cs
D:\Prop\src\Domain\Copy\SourceDestinationLink.cs

D:\Prop\src\Domain\Shadow\ShadowOrder.cs
D:\Prop\src\Domain\Shadow\ShadowFill.cs
D:\Prop\src\Domain\Shadow\ShadowPosition.cs
D:\Prop\src\Domain\Shadow\ShadowPerformance.cs
```

**Required invariants on Wave 2 types:**

- `TraderLifecycleState` values must match §22 **verbatim**.
- First score is only allowed after 3 *completed reconstructed XAUUSD* lifecycles (`EARLY_SCORE` / `EARLY_SCORE_ELIGIBLE`, never `PROVEN_PROFITABLE`).
- `TraderScore` is a candidate. It cannot transition a trader to `LIVE` without a `RiskDecision`.
- `CopyIntent` has `expires_at` + `max_signal_age`. Stale intents must not execute (§63).
- `RiskDecisionAction` is the only path to execution. Scoring fields are `candidate`, `confidence`, `suggestedAllocation` only.

### Wave 3 — execution / FIX / outbox (unblocks Phase 4, 7, 8)

```
D:\Prop\src\Domain\Execution\ExecutionVenue.cs
D:\Prop\src\Domain\Execution\DestinationQuote.cs
D:\Prop\src\Domain\Execution\ExecutionIntent.cs
D:\Prop\src\Domain\Execution\FixSession.cs
D:\Prop\src\Domain\Execution\FixSessionEvent.cs
D:\Prop\src\Domain\Execution\FixOrder.cs
D:\Prop\src\Domain\Execution\FixExecutionReport.cs
D:\Prop\src\Domain\Execution\DestinationPosition.cs
D:\Prop\src\Domain\Execution\ExecutionReconciliationRun.cs
D:\Prop\src\Domain\Execution\ExecutionReconciliationIssue.cs

D:\Prop\src\Domain\Platform\OutboxEvent.cs
D:\Prop\src\Domain\Platform\AuditLog.cs
D:\Prop\src\Domain\Platform\SystemEvent.cs
```

**Required invariants on Wave 3 types:**

- `ExecutionIntent` persisted **before** `NewOrderSingle` with unique `ClOrdId` and §33 fields (`execution_intent_id`, `cl_ord_id`, `source_broker_id`, `source_login`, `source_trade_id`, `source_event_id`, `destination_account`, `canonical_symbol`, `side`, `requested_quantity`, `created_at`, `status`).
- TCP drop → `ExecutionState.Unknown`. No blind resend.
- `KillSwitchMode.StopNewExecution` ≠ flatten.

### Tests (not Domain source, but required before LIVE)

```
D:\Prop\tests\Unit\Domain\CompoundIdentityTests.cs
D:\Prop\tests\Unit\Domain\ReconstructedTradeCompletionTests.cs
D:\Prop\tests\Unit\Domain\CanonicalSymbolNormalizationTests.cs
D:\Prop\tests\Unit\Domain\TraderLifecycleStateTests.cs
D:\Prop\tests\Unit\Domain\RiskDecisionAuthorityTests.cs
D:\Prop\tests\Unit\Domain\CopyIntentExpiryTests.cs
D:\Prop\tests\Unit\Domain\KillSwitchSeparationTests.cs
```

Unit project already references Domain.

---

## 8. Quoted evidence

### 8.1 Domain is empty template

`D:\Prop\src\Domain\Class1.cs`:

```csharp
namespace TraderIntelligence.Domain;

public class Class1
{

}
```

`D:\Prop\src\Domain\TraderIntelligence.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

### 8.2 §10 — compound identities (no Domain type implements this)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §10:

```text
Never assume login or ticket IDs are globally unique.

Use compound identities:

broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id

All source-side tables must carry:

broker_id
```

### 8.3 §14 — `ReconstructedTrade` (not present)

Architecture §14:

```text
Order != Deal != Position != Logical Trade
...
Create a canonical:
ReconstructedTrade
```

Example fields include `broker_id`, `canonical_symbol`, `entry_vwap`, `exit_vwap`, `was_scaled_in`, `was_partial_close`, `was_averaged_down`, `completed`.

`grep ReconstructedTrade` over `D:\Prop\src\Domain` = no hits (only architecture + this report).

### 8.4 §16 — `CanonicalInstrument` (not present)

Architecture §16:

```text
CanonicalInstrument
  XAUUSD

broker/source symbol → canonical XAUUSD
cTrader instrument ID → canonical XAUUSD

Never assume FIX tag 55 is the string "XAUUSD".
```

Aliases listed: `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`.

### 8.5 §22 — trader states (not present)

Architecture §22:

```text
INSUFFICIENT_DATA
EARLY_SCORE
WATCH
SHADOW
LIVE_CANDIDATE
LIVE
PAUSED
RISK_BLOCKED
DISQUALIFIED
```

### 8.6 §39 — risk is final authority (not present)

Architecture §39:

```text
Scoring/ML may only produce:
candidate
confidence
suggested allocation

Risk engine decides:
approve
reduce size
reject
pause trader
pause venue
global stop
```

### 8.7 §45 — core tables (no matching types)

Architecture §45 lists 44 tables from `brokers` through `system_events`. Domain has none of them.

### 8.8 §66 — folder exists, model does not

Architecture §66:

```text
/src
  /Domain
  /Application
  /Infrastructure
  /Mt5
  /TradeReconstruction
  /Scoring
  /Shadow
  /Risk
  /Execution
  /Fix.CTrader
```

`D:\Prop\src` today: `Domain`, `Application`, `Infrastructure`, `Mt5`, `Fix.CTrader` only. All five are Class1 scaffolds.

### 8.9 Adjacent flow Domain must encode (quoted for file design)

§4 pipeline:

```text
Shadow copy ──> CopyIntent ──> Risk Engine
```

§32: never send FIX from an MT5 callback; persist `CopyIntent` then `ApprovedExecutionIntent`.

§33 `ExecutionIntent` fields: `execution_intent_id`, `cl_ord_id`, `source_broker_id`, `source_login`, `source_trade_id`, `source_event_id`, `destination_account`, `canonical_symbol`, `side`, `requested_quantity`, `created_at`, `status`.

§63: every `CopyIntent` has `expires_at` and `max_signal_age`.

§15: count only `3 completed reconstructed XAUUSD position lifecycles`; trade #3 → `EARLY_SCORE_ELIGIBLE`, not `PROVEN_PROFITABLE`.

---

## 9. What Domain must *not* become

| Anti-pattern | Why |
|---|---|
| Add EF / Npgsql / Redis / QuickFIX / Serilog to Domain.csproj | Infrastructure leakage. Domain currently has **zero** packages — keep it. |
| Put table names / `[Column]` / `DbContext` in Domain | §45 tables are *persistence*; Domain owns behavior + identities. |
| Treat `Class1` as a grab-bag | DELETE it. |
| Assume `login` uniqueness | Violates §10. |
| Count deals/orders as “trades” | Violates §14/§15. |
| Hard-code `"XAUUSD"` as FIX symbol | Violates §16. |
| Let a score approve live copy | Violates §39. |
| Single kill-switch bool | Violates §40 (`STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN`). |

---

## 10. Honesty metrics

| Metric | Measured value |
|---|---|
| Domain `.cs` source files | **1** (`Class1.cs`) |
| Domain types with behavior | **0** |
| Architecture §10 identities implemented | **0 / 4** |
| `ReconstructedTrade` present | **no** |
| `CanonicalInstrument` present | **no** |
| `TraderLifecycleState` present | **no** |
| `RiskDecision` present | **no** |
| §45 table-aligned entities present | **0 / 44** |
| Domain package dependencies | **0** (correct) |
| Solution references to Domain | **9 projects** (wired, unused) |
| `UNSAFE` findings in Domain source | **none** (empty) |
| `EXISTS_AND_GOOD` product types | **none** |
| Ready for Phase 1 ingestion | **no** — compound IDs + raw MT5 entities must exist first |

**Phase 0 status for Domain:** audit complete. Domain is a named empty ring. Do not claim a domain model exists.

---

## 11. Recommended next implementation sequence (Domain only)

1. Delete `Class1.cs`.
2. Add Wave 1 identifiers + `CanonicalSymbol` + `ReconstructedTrade` + MT5 raw entities with mandatory `BrokerId`.
3. Add unit tests for compound-identity equality and XAUUSD alias mapping.
4. Only then let Infrastructure add EF configurations that *map* these types to §45 tables.

No Domain files were created by this audit.
)
