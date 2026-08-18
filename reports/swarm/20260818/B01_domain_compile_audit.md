# B01 — Domain compile audit (`src/Domain`)

| Field | Value |
|---|---|
| Agent | B01 (Domain compile / naming / usings / type completeness) |
| Date | 2026-08-18 |
| Scope | All product C# under `D:\Prop\src\Domain` + `TraderIntelligence.Domain.csproj` |
| Product source edited | **No** (report only) |
| Method | Read every Domain `.cs` file; inventory types/namespaces/usings; `dotnet build --no-incremental` with `WarningLevel=999` and `Nullable=enable`; confirm Application still compiles against Domain. Downstream Infrastructure / Unit-test failures were recorded only when they are **not** Domain-caused. |

**Verdict: Domain compiles clean. 0 errors, 0 warnings.**

No missing usings. No duplicate type names inside the Domain assembly (no CS0104). Every declared type has a complete C# body (no `partial` stubs, no `#error`, no `NotImplementedException`). Remaining items are **naming near-collisions**, **file/type mismatches**, and **semantic incompleteness** — they do not block `TraderIntelligence.Domain.dll`.

This report supersedes `A01_domain_audit.md` for *current tree contents*. A01 described a template `Class1.cs` project; that file is gone. Domain now has 47 product sources and 59 public types.

---

## 1. Measured compile result

Command:

```text
dotnet build D:\Prop\src\Domain\TraderIntelligence.Domain.csproj --no-incremental -p:WarningLevel=999 -p:Nullable=enable
```

| Metric | Result |
|---|---|
| SDK / Roslyn | `dotnet` 8.0.424 (`csc` via `Microsoft.NET.Sdk`) |
| TFM | `net8.0` |
| LangVersion | `latest` (`D:\Prop\Directory.Build.props`) |
| Nullable | enabled (csproj + Directory.Build.props) |
| ImplicitUsings | enabled |
| PackageReference | none |
| ProjectReference | none |
| Errors | **0** |
| Warnings | **0** (including warning level 999) |
| Output | `D:\Prop\src\Domain\bin\Debug\net8.0\TraderIntelligence.Domain.dll` |

`CoreCompile` compiled exactly these 47 product files (plus generated `GlobalUsings.g.cs` / `AssemblyInfo.cs` / TFM attributes):

```
Brokers\BrokerCodes.cs
Entities\AuditLog.cs
Entities\Broker.cs
Entities\CanonicalInstrument.cs
Entities\CopyIntent.cs
Entities\DestinationQuote.cs
Entities\ExecutionIntent.cs
Entities\FixSessionState.cs
Entities\KillSwitch.cs
Entities\Mt5Account.cs
Entities\Mt5Deal.cs
Entities\Mt5Group.cs
Entities\Mt5Position.cs
Entities\OutboxEvent.cs
Entities\ReconstructedTrade.cs
Entities\RiskDecisionRecord.cs
Entities\ShadowOrder.cs
Entities\SourceSymbolMapping.cs
Entities\SyncCheckpoint.cs
Entities\TraderScore.cs
Entities\TraderScoreHistory.cs
Enums\CopyIntentAction.cs
Enums\DealAction.cs
Enums\DealEntry.cs
Enums\ExecutionOrderStatus.cs
Enums\FeatureQuality.cs
Enums\FixSessionQualifier.cs
Enums\FixSessionStatus.cs
Enums\KillSwitchMode.cs
Enums\OutboxEventType.cs
Enums\PriceSource.cs
Enums\ReconciliationIssueType.cs
Enums\RiskDecisionOutcome.cs
Enums\TradeDirection.cs
Enums\TraderState.cs
Execution\ClOrdIdFactory.cs
Execution\CopyIntentExpiry.cs
Execution\ExecutionOrderStateMachine.cs
Execution\QuantityNormalizer.cs
Instruments\SymbolNormalizer.cs
Reconstruction\NormalizedDeal.cs
Reconstruction\ReconstructedTradeResult.cs
Reconstruction\TradeReconstructor.cs
Risk\RiskEngine.cs
Scoring\BaselineScorer.cs
Shadow\ShadowCopyEngine.cs
Volume\VolumeConverter.cs
```

No extra `.cs` files exist under `D:\Prop\src\Domain` outside `bin\` / `obj\`. `Class1.cs` is absent.

### 1.1 Downstream (not Domain, recorded so the floor is honest)

| Project | Result vs Domain |
|---|---|
| `TraderIntelligence.Application` | **PASS** 0/0 — Domain public API is consumable |
| `TraderIntelligence.Infrastructure` | **FAIL** `DemoSeeder.cs(124)` CS0246 `IMt5BrokerConnector` — missing `using TraderIntelligence.Application.Contracts`, **not** a Domain type |
| `TraderIntelligence.Tests.Unit` | **FAIL** in `Fix.CTrader` (`FixMessageParser.cs` CS1503 char→string) — **not** Domain |

---

## 2. Project / using surface

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

Generated implicit usings (`obj\Debug\net8.0\TraderIntelligence.Domain.GlobalUsings.g.cs`):

- `System`
- `System.Collections.Generic`
- `System.IO`
- `System.Linq`
- `System.Net.Http`
- `System.Threading`
- `System.Threading.Tasks`

That set covers every BCL type Domain uses (`Guid`, `DateTimeOffset`, `TimeSpan`, `decimal`, `Math`, `HashSet`, `Dictionary`, `IReadOnlyList`, `ArgumentException`, `Array`, `StringComparison`, `MidpointRounding`). No Domain file is missing a `System.*` using.

### 2.1 Explicit usings (all required)

| File | Using | Why required |
|---|---|---|
| 12 Entities (`CopyIntent`, `ExecutionIntent`, `FixSessionState`, `KillSwitch`, `Mt5Deal`, `Mt5Position`, `OutboxEvent`, `ReconstructedTrade`, `RiskDecisionRecord`, `ShadowOrder`, `TraderScore`, `TraderScoreHistory`) | `TraderIntelligence.Domain.Enums` | enum-typed properties |
| `NormalizedDeal.cs` | `Enums` | `DealAction`, `DealEntry` |
| `ReconstructedTradeResult.cs` | `Enums`, `Instruments` | `TradeDirection`, `CanonicalInstrumentRef` |
| `TradeReconstructor.cs` | `Enums`, `Instruments`, `Volume` | reconstruct logic |
| `RiskEngine.cs` | `Enums` | `CopyIntentAction`, `KillSwitchMode`, `RiskDecisionOutcome` |
| `BaselineScorer.cs` | `Enums`, `Reconstruction` | `FeatureQuality`, `PriceSource`, `TraderState`, `ReconstructedTradeResult` |
| `ShadowCopyEngine.cs` | `Enums`, `Risk` | `TradeDirection`, `DestinationQuote` |
| `ExecutionOrderStateMachine.cs` | `Enums` | `ExecutionOrderStatus` |

No Domain file has an unused explicit using that would be CS8019 if that warning were enabled. Files without usings (`Broker`, `AuditLog`, `CanonicalInstrument`, `Mt5Account`, `Mt5Group`, `SourceSymbolMapping`, `SyncCheckpoint`, all enums, `BrokerCodes`, `ClOrdIdFactory`, `CopyIntentExpiry`, `QuantityNormalizer`, `SymbolNormalizer`, `VolumeConverter`) only use implicit BCL types or types in their own namespace.

There is **no** root namespace `TraderIntelligence.Domain`. All types live in a child namespace (see §3). That is legal and compiles; it just means `using TraderIntelligence.Domain;` imports nothing.

---

## 3. Type inventory (59 public types, 11 namespaces)

Simple names are **unique across the entire Domain assembly**. That is why there is no CS0104 today.

| Namespace | Types |
|---|---|
| `TraderIntelligence.Domain.Brokers` | `BrokerCodes` |
| `TraderIntelligence.Domain.Entities` | `AuditLog`, `Broker`, `CanonicalInstrument`, `CopyIntent`, `DestinationQuoteSnapshot`, `ExecutionIntent`, `FixSessionState`, `KillSwitch`, `Mt5Account`, `Mt5Deal`, `Mt5Group`, `Mt5Position`, `OutboxEvent`, `ReconstructedTrade`, `RiskDecisionRecord`, `ShadowOrder`, `SourceSymbolMapping`, `SyncCheckpoint`, `TraderScore`, `TraderScoreHistory` (20) |
| `TraderIntelligence.Domain.Enums` | `CopyIntentAction`, `DealAction`, `DealEntry`, `ExecutionOrderStatus`, `FeatureQuality`, `FixSessionQualifier`, `FixSessionStatus`, `KillSwitchMode`, `OutboxEventType`, `PriceSource`, `ReconciliationIssueType`, `RiskDecisionOutcome`, `TradeDirection`, `TraderState` (14) |
| `TraderIntelligence.Domain.Execution` | `ClOrdIdFactory`, `CopyIntentExpiry`, `ExecutionOrderStateMachine`, `ExecutionReportInput`, `InstrumentQuantitySpec`, `QuantityNormalizer` |
| `TraderIntelligence.Domain.Instruments` | `CanonicalInstrumentRef`, `SymbolNormalizer` |
| `TraderIntelligence.Domain.Reconstruction` | `NormalizedDeal`, `ReconstructedTradeResult`, `TradeReconstructor` |
| `TraderIntelligence.Domain.Risk` | `DestinationQuote`, `RiskDecision`, `RiskEngine`, `RiskEvaluationRequest`, `RiskLimits` |
| `TraderIntelligence.Domain.Scoring` | `BaselineScore`, `BaselineScorer`, `FeatureSnapshot`, `TraderStateMachine` |
| `TraderIntelligence.Domain.Shadow` | `ShadowCopyEngine`, `ShadowFill`, `ShadowPosition` |
| `TraderIntelligence.Domain.Volume` | `VolumeConverter` |

Private nested type (complete, not public): `TradeReconstructor.OpenTrade`.

No `interface`, no `partial`, no `abstract`, no `file`-local types.

---

## 4. Naming conflicts

### 4.1 Compile-time conflicts (CS0104 / CS0101 / CS0111)

**None.**

- No two types share a simple name.
- No two types share a fully-qualified name.
- No member overloads collide.
- Enums do not collide with same-named classes.

### 4.2 File name ≠ type name (1 real mismatch)

| File | Declared type | Conflict? |
|---|---|---|
| `Entities\DestinationQuote.cs` | `DestinationQuoteSnapshot` | **Yes — filename advertises a type that lives in another namespace** |

`TraderIntelligence.Domain.Risk.DestinationQuote` is a positional record in `Risk\RiskEngine.cs`. The persist entity is `DestinationQuoteSnapshot`. Infrastructure already uses the snapshot (`TraderDbContext.DestinationQuotes`, `DemoSeeder`). This is **not** a compiler error. It **will** become CS0104 the moment anyone adds `class DestinationQuote` under `Entities` while a consumer imports both `Entities` and `Risk`.

### 4.3 Near-collisions (same stem, different namespace)

These compile today. They are the highest-risk naming debt in Domain.

| Pair | Namespaces | Risk |
|---|---|---|
| `CanonicalInstrument` vs `CanonicalInstrumentRef` | Entities vs Instruments | Two “canonical XAUUSD” identities: persist row (`Id`+`Code`) vs in-memory `record(string Code)` with static `XauUsd` |
| `DestinationQuote` vs `DestinationQuoteSnapshot` | Risk vs Entities | In-memory quote DTO vs EF snapshot; file is named after the DTO |
| `ReconstructedTrade` vs `ReconstructedTradeResult` | Entities vs Reconstruction | Persist shape vs engine output; field sets **differ** (see §6.2) |
| `RiskDecision` vs `RiskDecisionRecord` | Risk vs Entities | Engine result vs persist row |
| `ShadowOrder` vs `ShadowFill` / `ShadowPosition` | Entities vs Shadow | Persist fill-ish row vs in-memory fill/position; `ShadowPosition` is unused |
| `KillSwitch` vs `KillSwitchMode` | Entities vs Enums | Fine (entity + enum) |
| `TraderScore` vs `TraderScoreHistory` | Entities | Fine |

### 4.4 Identifier-style inconsistency (not a compile error)

`TraderState` members are `SCREAMING_SNAKE_CASE` (`INSUFFICIENT_DATA`, `EARLY_SCORE`, `LIVE_CANDIDATE`, `RISK_BLOCKED`). Every other Domain enum is `PascalCase`. Legal C#. Consumers already depend on the snake names (`EfDashboardQueries` compares `TraderState.WATCH` / `SHADOW` / `LIVE`).

`CopyIntent.Status` is a **string** defaulting to `"Pending"`, while `ExecutionIntent.Status` is `ExecutionOrderStatus`. Not a naming collision; it is an incomplete type (no `CopyIntentStatus` enum).

### 4.5 Identity-type split (`BrokerId`)

Not a name clash, but a type-identity trap at call sites:

| Shape | `BrokerId` type |
|---|---|
| All persist entities | `Guid` |
| `NormalizedDeal`, `ReconstructedTradeResult`, `RiskEvaluationRequest`, `ShadowPosition` | `string` (broker **code**) |

`TradeReconstructor.Reconstruct(string brokerId, …)` therefore cannot take `Mt5Deal.BrokerId` without a conversion. Application already does this (`EfTradingStore.LoadDealsAsync` writes `BrokerId = brokerCode`). Compiles. Easy to pass the wrong id at a new call site (`Guid.ToString()` vs code).

---

## 5. Missing usings — Domain: none

Checked:

1. Every identifier used in Domain resolves (rebuild 0/0 with nullable).
2. Every explicit `using` maps to a type actually referenced in that file.
3. Implicit usings cover BCL.
4. Cross-namespace references (`ShadowCopyEngine` → `Risk.DestinationQuote`, `ReconstructedTradeResult` → `Instruments.CanonicalInstrumentRef`, `TradeReconstructor` → `VolumeConverter` / `SymbolNormalizer`) all have the matching `using`.

No Domain file needs `using static`, aliases, or extern aliases.

---

## 6. Incomplete types

### 6.1 Compile-incomplete (missing members, unfinished bodies, `partial` without other part)

**None.** Every public type is a finished declaration. `grep` over Domain `*.cs` found no `TODO`, `FIXME`, `HACK`, `NotImplemented`, `#error`, `#warning`, or `partial class`.

Throws are only argument validation (`ClOrdIdFactory`, `QuantityNormalizer`, `SymbolNormalizer`, `VolumeConverter`).

### 6.2 Persist entity vs engine result — field gap (semantic)

`ReconstructedTradeResult` (engine) vs `ReconstructedTrade` (entity). Infrastructure copies what exists; the extra result fields are dropped.

| Member | Result | Entity | Effect |
|---|---|---|---|
| `RemainingVolumeLots` | yes | **no** | open remainder not persisted |
| `DealTickets` | yes | **no** | ticket list not persisted |
| `Id` | `string` composite | `Guid` | store allocates a new Guid (`EfTradingStore`) |
| `BrokerId` | `string` code | `Guid` | store uses the Guid from the call |

All other overlapping fields exist on both and compile.

### 6.3 Declared but unused inside Domain + `src` consumers

Complete types / members with **zero** references outside their declaring file:

| Symbol | Kind | Notes |
|---|---|---|
| `ReconciliationIssueType` | enum | 7 values; no issue entity, no engine use |
| `ShadowPosition` | record | defined next to `ShadowCopyEngine`; never constructed |
| `CopyIntentExpiry` | static class | `IsExpired` never called (RiskEngine inlines signal-age) |
| `ClOrdIdFactory` | class | never called |
| `QuantityNormalizer` / `InstrumentQuantitySpec` | class + record | never called |
| `VolumeConverter.HundredthsScale` | const | documented as the wrong MT5 comment; unused |
| `RiskLimits.MaxSlippage` | property | default `1.5m`; **never read** in `Evaluate` |
| `TraderStateMachine.CanPromoteToLive` | method | `=> false` — stub API, not unfinished syntax |

`FeatureQuality` and `PriceSource` **are** used, but only as constants `Unavailable` / `Unknown` written by `BaselineScorer`. Values `Exact` / `Approximate` / `AchieverMt5Ticks` / `CTraderQuoteSession` / etc. have no producer.

### 6.4 Stub / always-constant behavior (types are complete, behavior is not)

| Location | What is incomplete |
|---|---|
| `BaselineScorer.ComputeFeatures` | `MaeMfeQuality` always `Unavailable`; `AverageMfe` / `AverageMae` never set; `PriceSource` always `Unknown` |
| `TradeReconstructor.OpenTrade.ToResult` | `Fees` always `0m` |
| `TraderStateMachine.FromBaseline` | never returns `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, `DISQUALIFIED` |
| `TradeReconstructor.ApplyIn` | opposite-side `DealEntry.In` calls `ApplyReverse` then **discards** the closed trade (`_ = closed`) |
| `CopyIntent.Status` | untyped string, not an enum |
| `CanonicalInstrument` | `{Id, Code, Description?}` only — no contract size, digits, step, venue id |
| `DestinationQuoteSnapshot` | no venue id / session / uniqueness key beyond `Id` |
| `SourceSymbolMapping` | source→canonical only; no destination-symbol type in Domain |
| `ExecutionIntent` | create-time row only (no `VenueOrderId`, qty filled, last report) |

These are product-completeness gaps, not compiler holes.

### 6.5 Multi-type files (convention, not errors)

One file / one type is the usual C# convention. Compiler does not care. Files that host extra public types:

| File | Public types hosted |
|---|---|
| `Risk\RiskEngine.cs` | `RiskLimits`, `DestinationQuote`, `RiskEvaluationRequest`, `RiskDecision`, `RiskEngine` |
| `Scoring\BaselineScorer.cs` | `FeatureSnapshot`, `BaselineScore`, `BaselineScorer`, `TraderStateMachine` |
| `Shadow\ShadowCopyEngine.cs` | `ShadowFill`, `ShadowPosition`, `ShadowCopyEngine` |
| `Instruments\SymbolNormalizer.cs` | `CanonicalInstrumentRef`, `SymbolNormalizer` |
| `Execution\QuantityNormalizer.cs` | `InstrumentQuantitySpec`, `QuantityNormalizer` |
| `Execution\ExecutionOrderStateMachine.cs` | `ExecutionReportInput`, `ExecutionOrderStateMachine` |

---

## 7. Per-file compile notes

All files below **compile**. Notes are only residual risk.

### 7.1 Entities (20)

| File | Type complete? | Usings | Notes |
|---|---|---|---|
| `AuditLog.cs` | yes | implicit only | POCOs with defaults |
| `Broker.cs` | yes | implicit only | `Mode` default `"local"` |
| `CanonicalInstrument.cs` | yes (thin) | implicit only | no static `XauUsd` seed on the entity |
| `CopyIntent.cs` | yes | Enums | `Status` is `string` |
| `DestinationQuote.cs` | yes | implicit only | **type name ≠ file name** |
| `ExecutionIntent.cs` | yes | Enums | |
| `FixSessionState.cs` | yes | Enums | |
| `KillSwitch.cs` | yes | Enums | |
| `Mt5Account.cs` | yes | implicit only | `Login` is `long` (connector in Mt5 uses `ulong` — consumer issue) |
| `Mt5Deal.cs` | yes | Enums | `DealAction`/`DealEntry` `: uint` — legal on properties |
| `Mt5Group.cs` | yes | implicit only | |
| `Mt5Position.cs` | yes | Enums | `TimeUpdate` non-nullable |
| `OutboxEvent.cs` | yes | Enums | |
| `ReconstructedTrade.cs` | yes | Enums | missing result-only fields (§6.2) |
| `RiskDecisionRecord.cs` | yes | Enums | |
| `ShadowOrder.cs` | yes | Enums | no `ShadowPosition` entity twin |
| `SourceSymbolMapping.cs` | yes | implicit only | |
| `SyncCheckpoint.cs` | yes | implicit only | |
| `TraderScore.cs` | yes | Enums | |
| `TraderScoreHistory.cs` | yes | Enums | |

### 7.2 Enums (14)

All are complete closed sets. `DealAction` (0–20) and `DealEntry` (In/Out/InOut/OutBy) match the comments pointing at `MT5APIDeal.h`. No `[Flags]`. No missing comma / duplicate value.

### 7.3 Logic folders

| File | Compile | Residual |
|---|---|---|
| `BrokerCodes.cs` | pass | two string constants |
| `ClOrdIdFactory.cs` | pass | unused by rest of `src` |
| `CopyIntentExpiry.cs` | pass | unused |
| `ExecutionOrderStateMachine.cs` | pass | `Apply` ignores `LastQty`/`CumQty`/`LeavesQty` (present on input, unused) |
| `QuantityNormalizer.cs` | pass | unused |
| `SymbolNormalizer.cs` | pass | hard-coded XAU aliases; `TryGetValue(..., out canonical!)` is nullable-suppressed, legal |
| `NormalizedDeal.cs` | pass | `required` members; `IsTradingDeal` helper |
| `ReconstructedTradeResult.cs` | pass | `IsXauUsd` depends on `CanonicalInstrumentRef` |
| `TradeReconstructor.cs` | pass | `ApplyOut` writes `closed = null!` on the non-complete path; callers only read `closed` when the method returns true — nullable-safe enough for the compiler |
| `RiskEngine.cs` | pass | `MaxSlippage` unread; `required DestinationQuote? Quote` is intentionally nullable+required |
| `BaselineScorer.cs` | pass | MAE/MFE unused path |
| `ShadowCopyEngine.cs` | pass | `ShadowPosition` unused |
| `VolumeConverter.cs` | pass | `ulong / decimal` is legal; `HundredthsScale` unused |

---

## 8. What is *not* a Domain compile issue

Recorded so they are not mistaken for Domain holes:

- `IBrokerConnector` (`src\Mt5`) imports `Domain.Entities` and uses `ulong login` against entity `long Login` — that project’s type mismatch, not Domain’s.
- `DemoSeeder` CS0246 on `IMt5BrokerConnector` — Application contracts using, not Domain.
- `FixMessageParser` CS1503 — Fix.CTrader, not Domain.
- Unit test project has **no** product `*.cs` test files under `D:\Prop\tests\Unit` (only csproj). Domain is untested, not uncompilable.

---

## 9. Findings summary

| ID | Severity | Category | Finding | Blocks Domain compile? |
|---|---|---|---|---|
| B01-01 | info | compile | `dotnet build --no-incremental -p:WarningLevel=999` → **0/0** | no |
| B01-02 | info | usings | All explicit usings required; implicit BCL usings sufficient | no |
| B01-03 | info | names | 59 public simple names are unique in the assembly | no |
| B01-04 | medium | names | `Entities\DestinationQuote.cs` declares `DestinationQuoteSnapshot`; `Risk.DestinationQuote` is the other type | no (CS0104 if Entities later grows `DestinationQuote`) |
| B01-05 | medium | names / identity | Persist `BrokerId: Guid` vs engine `BrokerId: string` (code) | no |
| B01-06 | medium | incomplete (semantic) | `ReconstructedTrade` omits `RemainingVolumeLots` and `DealTickets` | no |
| B01-07 | low | incomplete (semantic) | Unused complete types: `ReconciliationIssueType`, `ShadowPosition`, `CopyIntentExpiry`, `ClOrdIdFactory`, `QuantityNormalizer` | no |
| B01-08 | low | incomplete (semantic) | `RiskLimits.MaxSlippage` never evaluated; MAE/MFE always unavailable; `CanPromoteToLive` always false; `Fees` always 0 | no |
| B01-09 | low | names | `TraderState` SCREAMING_SNAKE vs PascalCase enums; `CopyIntent.Status` is a string | no |
| B01-10 | info | completeness | No `partial`/TODO/`NotImplemented`; no missing types required for Domain to compile | no |

**Compile status: PASS.**

**Do not treat this as “Domain is feature-complete.”** It is compile-complete. The near-collisions in §4 and the persist/engine gaps in §6 are the items to fix when someone next touches these files — without adding a second `DestinationQuote` type under `Entities`.
