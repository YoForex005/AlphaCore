# D01 — Domain census (`src/Domain`)

| Field | Value |
|---|---|
| Agent | D01 (senior engineer, Domain file/type census) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\D01_domain_census.md` |
| Scope | Every `.cs` file under `D:\Prop\src\Domain` |
| Product source modified | **No** |
| Method | `list_dir` of `D:\Prop\src\Domain`; `Get-ChildItem -Recurse -Filter *.cs`; `grep` for `namespace` / type declarations; `read_file` of every authored source |
| Adjacent (read, not rewritten) | `B01_domain_compile_audit.md` (compile + 59-type inventory); `A01_domain_audit.md` (stale Class1 snapshot) |
| Project | `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` — `net8.0`, ImplicitUsings, Nullable, **no** PackageReference, **no** ProjectReference |

This is a **file/type census**, not a compile audit. Use **B01** for the 0/0 build result. Use **A01** only as a historical “was a Class1 scaffold” note; A01 is stale.

---

## 0. Counts (measured)

| Bucket | Count |
|---|---:|
| Authored product `.cs` files (compile inputs) | **47** |
| Generated `obj\` `.cs` files (Debug + Release) | **6** |
| **All `.cs` files under `D:\Prop\src\Domain`** | **53** |
| `.cs` files under `bin\` | **0** |
| Public types (classes / records / enums) | **59** |
| Private nested types | **1** (`TradeReconstructor.OpenTrade`) |
| Namespaces (all children; no root `TraderIntelligence.Domain`) | **10** |
| Interfaces / `partial` / `abstract` / `file`-local types | **0** |
| `Class1.cs` | **absent** |

Folders of authored sources: `Brokers/` (1), `Entities/` (20), `Enums/` (14), `Execution/` (4), `Instruments/` (1), `Reconstruction/` (3), `Risk/` (1), `Scoring/` (1), `Shadow/` (1), `Volume/` (1).

---

## 1. Authored product files — path, type names, one-line role

Paths are absolute. Kind is the C# declaration kind of each public type in the file.

### 1.1 Brokers (1)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `BrokerCodes` (static class) | String constants for the two owned source brokers: `ACHIEVER`, `STARWAVEFX`. |

### 1.2 Entities (20 persist shapes)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Entities\AuditLog.cs` | `AuditLog` (sealed class) | Persist row for actor/role/action/target/payload audit events. |
| `D:\Prop\src\Domain\Entities\Broker.cs` | `Broker` (sealed class) | Persist row for a source MT5 Manager endpoint (code, host, port, pool, proxy). |
| `D:\Prop\src\Domain\Entities\CanonicalInstrument.cs` | `CanonicalInstrument` (sealed class) | Persist identity of a canonical symbol (`Id` + `Code`, e.g. XAUUSD). |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | `CopyIntent` (sealed class) | Persist proposed source→destination copy action with expiry and string `Status`. |
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | `DestinationQuoteSnapshot` (sealed class) | Persist cTrader QUOTE bid/ask snapshot; **filename ≠ type name**. |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | `ExecutionIntent` (sealed class) | Persist FIX-bound order intent keyed by `ClOrdId` and `ExecutionOrderStatus`. |
| `D:\Prop\src\Domain\Entities\FixSessionState.cs` | `FixSessionState` (sealed class) | Persist QUOTE/TRADE session status, seq nums, and single-owner lease fields. |
| `D:\Prop\src\Domain\Entities\KillSwitch.cs` | `KillSwitch` (sealed class) | Persist current global kill-switch mode, setter, and reason. |
| `D:\Prop\src\Domain\Entities\Mt5Account.cs` | `Mt5Account` (sealed class) | Persist source login census: group, leverage, balance/equity/margin snapshot. |
| `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | `Mt5Deal` (sealed class) | Persist ingested Manager deal (ticket, action/entry, native volume, PnL). |
| `D:\Prop\src\Domain\Entities\Mt5Group.cs` | `Mt5Group` (sealed class) | Persist discovered Manager group metadata and analysis-enable flag. |
| `D:\Prop\src\Domain\Entities\Mt5Position.cs` | `Mt5Position` (sealed class) | Persist current source open-position snapshot (volume, SL/TP, profit). |
| `D:\Prop\src\Domain\Entities\OutboxEvent.cs` | `OutboxEvent` (sealed class) | Persist transactional outbox row (type, payload JSON, attempts, processed-at). |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | `ReconstructedTrade` (sealed class) | Persist reconstructed position lifecycle (VWAP, lots, flags, net PnL). |
| `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs` | `RiskDecisionRecord` (sealed class) | Persist risk-engine verdict against a copy intent (`AllowFixSend` included). |
| `D:\Prop\src\Domain\Entities\ShadowOrder.cs` | `ShadowOrder` (sealed class) | Persist simulated destination fill (qty, price, spread, source-vs-shadow slip). |
| `D:\Prop\src\Domain\Entities\SourceSymbolMapping.cs` | `SourceSymbolMapping` (sealed class) | Persist broker-specific source ticker → `CanonicalInstrument` mapping. |
| `D:\Prop\src\Domain\Entities\SyncCheckpoint.cs` | `SyncCheckpoint` (sealed class) | Persist per-broker/login stream cursor (`LastTimestamp` / `LastTicket`). |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | `TraderScore` (sealed class) | Persist latest baseline scores, flags, completed-XAU count, and `TraderState`. |
| `D:\Prop\src\Domain\Entities\TraderScoreHistory.cs` | `TraderScoreHistory` (sealed class) | Persist point-in-time score/state history row for a login. |

### 1.3 Enums (14)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Enums\CopyIntentAction.cs` | `CopyIntentAction` | Open / Increase / Reduce / Close exposure actions for copy + risk. |
| `D:\Prop\src\Domain\Enums\DealAction.cs` | `DealAction` (`: uint`) | Mirrors `IMTDeal::EnDealAction` (Buy/Sell plus balance/credit/cancel/etc.). |
| `D:\Prop\src\Domain\Enums\DealEntry.cs` | `DealEntry` (`: uint`) | Mirrors `IMTDeal::EnDealEntry` (`In`, `Out`, `InOut`, `OutBy`). |
| `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` | `ExecutionOrderStatus` | FIX order FSM states including `SentAcknowledgementUnknown` / `ExecutionStateUnknown`. |
| `D:\Prop\src\Domain\Enums\FeatureQuality.cs` | `FeatureQuality` | Exact / Approximate / Unavailable quality tag for MAE/MFE features. |
| `D:\Prop\src\Domain\Enums\FixSessionQualifier.cs` | `FixSessionQualifier` | Distinguishes the independent QUOTE vs TRADE FIX sessions. |
| `D:\Prop\src\Domain\Enums\FixSessionStatus.cs` | `FixSessionStatus` | Session lifecycle from Disconnected through ReadyForExecution / Error. |
| `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` | `KillSwitchMode` | `None` / `StopNewExecution` / `EmergencyFlatten` (never flatten source MT5). |
| `D:\Prop\src\Domain\Enums\OutboxEventType.cs` | `OutboxEventType` | Outbox payload kinds: trade completed, score, shadow, risk, notification. |
| `D:\Prop\src\Domain\Enums\PriceSource.cs` | `PriceSource` | Provenance of tick/quote used for features (MT5 ticks vs bar vs cTrader QUOTE). |
| `D:\Prop\src\Domain\Enums\ReconciliationIssueType.cs` | `ReconciliationIssueType` | Venue vs book mismatch kinds (qty/side/orphan/unexpected/unresolved). |
| `D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs` | `RiskDecisionOutcome` | Approve / ReduceSize / Reject / PauseTrader / PauseVenue / GlobalStop. |
| `D:\Prop\src\Domain\Enums\TradeDirection.cs` | `TradeDirection` | Long vs Short side used by positions, intents, shadow, reconstruction. |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | `TraderState` | Trader lifecycle (`INSUFFICIENT_DATA` … `DISQUALIFIED`; SCREAMING_SNAKE members). |

### 1.4 Execution (4)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs` | `ClOrdIdFactory` (sealed class) | Deterministic `ClOrdID` builder: `TI` + timestamp + seq + truncated intent id. |
| `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` | `CopyIntentExpiry` (static class) | Pure `now - sourceEventTime > maxSignalAge` expiry predicate. |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | `ExecutionReportInput` (record), `ExecutionOrderStateMachine` (static class) | Maps FIX ExecType/OrdStatus onto `ExecutionOrderStatus`; gates NOS retry vs recon. |
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | `InstrumentQuantitySpec` (record), `QuantityNormalizer` (sealed class) | Source lots × allocation → dest step/min/max/precision (0 if below min). |

### 1.5 Instruments (1)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` | `CanonicalInstrumentRef` (record), `SymbolNormalizer` (sealed class) | Maps source aliases and venue numeric IDs onto canonical `XAUUSD`. |

### 1.6 Reconstruction (3)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | `NormalizedDeal` (record) | In-memory deal fed to reconstructor (`BrokerId` is **string code**, native volume). |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | `ReconstructedTradeResult` (sealed class) | Engine output of one position lifecycle (includes `DealTickets`, remaining lots). |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | `TradeReconstructor` (sealed class); private nested `OpenTrade` | Rebuilds In/Out/InOut/OutBy lifecycles; counts completed XAUUSD trades (≥3 = early score). |

### 1.7 Risk (1)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `RiskLimits`, `DestinationQuote` (record), `RiskEvaluationRequest` (record), `RiskDecision` (record), `RiskEngine` | Final authority: quote/age/spread/exposure/kill-switch gates; `AllowFixSend` only if live flags pass. |

### 1.8 Scoring (1)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `FeatureSnapshot` (record), `BaselineScore` (record), `BaselineScorer`, `TraderStateMachine` (static class) | `baseline.v1` features + risk/behavior/early-quality scores; first official score at 3 completed XAU trades; never promotes to LIVE. |

### 1.9 Shadow (1)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | `ShadowFill` (record), `ShadowPosition` (record), `ShadowCopyEngine` | Paper-fills entries/exits on destination QUOTE (ask for long entry, bid for exit) plus mark-to-market. |

### 1.10 Volume (1)

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\Volume\VolumeConverter.cs` | `VolumeConverter` (sealed class) | Native Manager volume → lots (`Scale=10_000` default; `Extended=1e8`; documents the wrong “hundredths” comment). |

---

## 2. Generated `obj\` `.cs` files (not product source)

These are MSBuild/SDK outputs. They are **not** compile-list “product sources” in B01’s 47-file list, but they **are** `.cs` files under `D:\Prop\src\Domain`.

| Path | Type names | One-line role |
|---|---|---|
| `D:\Prop\src\Domain\obj\Debug\net8.0\TraderIntelligence.Domain.AssemblyInfo.cs` | *(none — assembly attributes)* | Debug assembly metadata; informational version `1.0.0+398a14200ec65714c4077eed55c46808382ca1e3`. |
| `D:\Prop\src\Domain\obj\Debug\net8.0\TraderIntelligence.Domain.GlobalUsings.g.cs` | *(none — global usings)* | Implicit usings: `System`, `Collections.Generic`, `IO`, `Linq`, `Net.Http`, `Threading`, `Threading.Tasks`. |
| `D:\Prop\src\Domain\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs` | *(none — TFM attribute)* | `[assembly: TargetFramework(".NETCoreApp,Version=v8.0")]`. |
| `D:\Prop\src\Domain\obj\Release\net8.0\TraderIntelligence.Domain.AssemblyInfo.cs` | *(none — assembly attributes)* | Release twin of AssemblyInfo (`AssemblyConfiguration=Release`, same git hash suffix). |
| `D:\Prop\src\Domain\obj\Release\net8.0\TraderIntelligence.Domain.GlobalUsings.g.cs` | *(none — global usings)* | Identical implicit-using set to Debug. |
| `D:\Prop\src\Domain\obj\Release\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs` | *(none — TFM attribute)* | Release twin of TFM attribute. |

`bin\` contains only `TraderIntelligence.Domain.{dll,pdb,deps.json}` — **no** `.cs`.

---

## 3. Public type index (59) — namespace + role

Simple names are unique across the assembly (B01: no CS0104). There is **no** type in namespace `TraderIntelligence.Domain`.

| FQN | Kind | Declaring file | One-line role |
|---|---|---|---|
| `TraderIntelligence.Domain.Brokers.BrokerCodes` | static class | `Brokers\BrokerCodes.cs` | Owned broker code constants. |
| `TraderIntelligence.Domain.Entities.AuditLog` | sealed class | `Entities\AuditLog.cs` | Dashboard/RBAC audit persist row. |
| `TraderIntelligence.Domain.Entities.Broker` | sealed class | `Entities\Broker.cs` | Source Manager broker persist row. |
| `TraderIntelligence.Domain.Entities.CanonicalInstrument` | sealed class | `Entities\CanonicalInstrument.cs` | Canonical instrument persist identity. |
| `TraderIntelligence.Domain.Entities.CopyIntent` | sealed class | `Entities\CopyIntent.cs` | Proposed copy action persist row. |
| `TraderIntelligence.Domain.Entities.DestinationQuoteSnapshot` | sealed class | `Entities\DestinationQuote.cs` | Persisted destination bid/ask snapshot. |
| `TraderIntelligence.Domain.Entities.ExecutionIntent` | sealed class | `Entities\ExecutionIntent.cs` | Persist-before-send FIX order intent. |
| `TraderIntelligence.Domain.Entities.FixSessionState` | sealed class | `Entities\FixSessionState.cs` | QUOTE/TRADE session + ownership persist. |
| `TraderIntelligence.Domain.Entities.KillSwitch` | sealed class | `Entities\KillSwitch.cs` | Global execution halt persist row. |
| `TraderIntelligence.Domain.Entities.Mt5Account` | sealed class | `Entities\Mt5Account.cs` | Source login census persist row. |
| `TraderIntelligence.Domain.Entities.Mt5Deal` | sealed class | `Entities\Mt5Deal.cs` | Ingested Manager deal persist row. |
| `TraderIntelligence.Domain.Entities.Mt5Group` | sealed class | `Entities\Mt5Group.cs` | Discovered Manager group persist row. |
| `TraderIntelligence.Domain.Entities.Mt5Position` | sealed class | `Entities\Mt5Position.cs` | Source open-position persist snapshot. |
| `TraderIntelligence.Domain.Entities.OutboxEvent` | sealed class | `Entities\OutboxEvent.cs` | PostgreSQL transactional outbox persist row. |
| `TraderIntelligence.Domain.Entities.ReconstructedTrade` | sealed class | `Entities\ReconstructedTrade.cs` | Persisted reconstructed trade (subset of engine result). |
| `TraderIntelligence.Domain.Entities.RiskDecisionRecord` | sealed class | `Entities\RiskDecisionRecord.cs` | Persisted risk verdict. |
| `TraderIntelligence.Domain.Entities.ShadowOrder` | sealed class | `Entities\ShadowOrder.cs` | Persisted paper fill. |
| `TraderIntelligence.Domain.Entities.SourceSymbolMapping` | sealed class | `Entities\SourceSymbolMapping.cs` | Persist source-ticker → canonical map. |
| `TraderIntelligence.Domain.Entities.SyncCheckpoint` | sealed class | `Entities\SyncCheckpoint.cs` | Ingestion/recon cursor persist row. |
| `TraderIntelligence.Domain.Entities.TraderScore` | sealed class | `Entities\TraderScore.cs` | Latest scorecard persist row. |
| `TraderIntelligence.Domain.Entities.TraderScoreHistory` | sealed class | `Entities\TraderScoreHistory.cs` | Historical scorecard persist row. |
| `TraderIntelligence.Domain.Enums.CopyIntentAction` | enum | `Enums\CopyIntentAction.cs` | Copy/risk action vocabulary. |
| `TraderIntelligence.Domain.Enums.DealAction` | enum `: uint` | `Enums\DealAction.cs` | Official MT5 deal-action codes. |
| `TraderIntelligence.Domain.Enums.DealEntry` | enum `: uint` | `Enums\DealEntry.cs` | Official MT5 deal-entry codes. |
| `TraderIntelligence.Domain.Enums.ExecutionOrderStatus` | enum | `Enums\ExecutionOrderStatus.cs` | FIX order bookkeeping states. |
| `TraderIntelligence.Domain.Enums.FeatureQuality` | enum | `Enums\FeatureQuality.cs` | MAE/MFE quality tag. |
| `TraderIntelligence.Domain.Enums.FixSessionQualifier` | enum | `Enums\FixSessionQualifier.cs` | QUOTE vs TRADE session id. |
| `TraderIntelligence.Domain.Enums.FixSessionStatus` | enum | `Enums\FixSessionStatus.cs` | FIX session lifecycle. |
| `TraderIntelligence.Domain.Enums.KillSwitchMode` | enum | `Enums\KillSwitchMode.cs` | Global halt modes. |
| `TraderIntelligence.Domain.Enums.OutboxEventType` | enum | `Enums\OutboxEventType.cs` | Outbox event vocabulary. |
| `TraderIntelligence.Domain.Enums.PriceSource` | enum | `Enums\PriceSource.cs` | Feature price provenance. |
| `TraderIntelligence.Domain.Enums.ReconciliationIssueType` | enum | `Enums\ReconciliationIssueType.cs` | Venue-recon issue vocabulary (unused in Domain). |
| `TraderIntelligence.Domain.Enums.RiskDecisionOutcome` | enum | `Enums\RiskDecisionOutcome.cs` | Risk verdict vocabulary. |
| `TraderIntelligence.Domain.Enums.TradeDirection` | enum | `Enums\TradeDirection.cs` | Long/Short. |
| `TraderIntelligence.Domain.Enums.TraderState` | enum | `Enums\TraderState.cs` | Trader promotion/block states. |
| `TraderIntelligence.Domain.Execution.ClOrdIdFactory` | sealed class | `Execution\ClOrdIdFactory.cs` | Unique persist-before-send ClOrdID. |
| `TraderIntelligence.Domain.Execution.CopyIntentExpiry` | static class | `Execution\CopyIntentExpiry.cs` | Signal-age expiry helper. |
| `TraderIntelligence.Domain.Execution.ExecutionReportInput` | sealed record | `Execution\ExecutionOrderStateMachine.cs` | Normalized FIX execution-report input. |
| `TraderIntelligence.Domain.Execution.ExecutionOrderStateMachine` | static class | `Execution\ExecutionOrderStateMachine.cs` | Order-status transitions + retry/recon gates. |
| `TraderIntelligence.Domain.Execution.InstrumentQuantitySpec` | sealed record | `Execution\QuantityNormalizer.cs` | Destination min/max/step/precision. |
| `TraderIntelligence.Domain.Execution.QuantityNormalizer` | sealed class | `Execution\QuantityNormalizer.cs` | Source→destination quantity conversion. |
| `TraderIntelligence.Domain.Instruments.CanonicalInstrumentRef` | sealed record | `Instruments\SymbolNormalizer.cs` | In-memory canonical code (`XauUsd` = `"XAUUSD"`). |
| `TraderIntelligence.Domain.Instruments.SymbolNormalizer` | sealed class | `Instruments\SymbolNormalizer.cs` | Alias + venue-id → canonical mapper. |
| `TraderIntelligence.Domain.Reconstruction.NormalizedDeal` | sealed record | `Reconstruction\NormalizedDeal.cs` | Reconstructor input deal. |
| `TraderIntelligence.Domain.Reconstruction.ReconstructedTradeResult` | sealed class | `Reconstruction\ReconstructedTradeResult.cs` | Reconstructor output trade. |
| `TraderIntelligence.Domain.Reconstruction.TradeReconstructor` | sealed class | `Reconstruction\TradeReconstructor.cs` | Deterministic deal→trade rebuild. |
| `TraderIntelligence.Domain.Risk.RiskLimits` | sealed class | `Risk\RiskEngine.cs` | Configurable hard limits (spread, age, exposure, martingale). |
| `TraderIntelligence.Domain.Risk.DestinationQuote` | sealed record | `Risk\RiskEngine.cs` | In-memory destination quote DTO (not the persist entity). |
| `TraderIntelligence.Domain.Risk.RiskEvaluationRequest` | sealed record | `Risk\RiskEngine.cs` | Full risk-eval input (intent, quote, flags, exposures). |
| `TraderIntelligence.Domain.Risk.RiskDecision` | sealed record | `Risk\RiskEngine.cs` | In-memory risk verdict (`AllowFixSend`). |
| `TraderIntelligence.Domain.Risk.RiskEngine` | sealed class | `Risk\RiskEngine.cs` | Evaluates copy intents; blocks live send unless flags allow. |
| `TraderIntelligence.Domain.Scoring.FeatureSnapshot` | sealed record | `Scoring\BaselineScorer.cs` | Computed features over completed XAU trades. |
| `TraderIntelligence.Domain.Scoring.BaselineScore` | sealed record | `Scoring\BaselineScorer.cs` | Scored snapshot + suggested `TraderState`. |
| `TraderIntelligence.Domain.Scoring.BaselineScorer` | sealed class | `Scoring\BaselineScorer.cs` | Feature + score formulas (`EarlyScoreTradeCount = 3`). |
| `TraderIntelligence.Domain.Scoring.TraderStateMachine` | static class | `Scoring\BaselineScorer.cs` | Maps scores→state; `CanPromoteToLive` is always `false`. |
| `TraderIntelligence.Domain.Shadow.ShadowFill` | sealed record | `Shadow\ShadowCopyEngine.cs` | Simulated fill (price, slip, quote age). |
| `TraderIntelligence.Domain.Shadow.ShadowPosition` | sealed record | `Shadow\ShadowCopyEngine.cs` | In-memory shadow position (declared; unused inside Domain). |
| `TraderIntelligence.Domain.Shadow.ShadowCopyEngine` | sealed class | `Shadow\ShadowCopyEngine.cs` | Destination-QUOTE paper copy engine. |
| `TraderIntelligence.Domain.Volume.VolumeConverter` | sealed class | `Volume\VolumeConverter.cs` | Native integer volume ↔ lots. |

Private nested (not public, not in the 59):

| FQN | Kind | Role |
|---|---|---|
| `TraderIntelligence.Domain.Reconstruction.TradeReconstructor.OpenTrade` | private sealed class | Mutable working state while a position is still open (VWAP accumulators, flags). |

---

## 4. Types per namespace (check)

| Namespace | Public types | File count |
|---|---:|---:|
| `TraderIntelligence.Domain.Brokers` | 1 | 1 |
| `TraderIntelligence.Domain.Entities` | 20 | 20 |
| `TraderIntelligence.Domain.Enums` | 14 | 14 |
| `TraderIntelligence.Domain.Execution` | 6 | 4 |
| `TraderIntelligence.Domain.Instruments` | 2 | 1 |
| `TraderIntelligence.Domain.Reconstruction` | 3 | 3 |
| `TraderIntelligence.Domain.Risk` | 5 | 1 |
| `TraderIntelligence.Domain.Scoring` | 4 | 1 |
| `TraderIntelligence.Domain.Shadow` | 3 | 1 |
| `TraderIntelligence.Domain.Volume` | 1 | 1 |
| **Total** | **59** | **47** |

Multi-type files (7): `ExecutionOrderStateMachine.cs` (2), `QuantityNormalizer.cs` (2), `SymbolNormalizer.cs` (2), `RiskEngine.cs` (5), `BaselineScorer.cs` (4), `ShadowCopyEngine.cs` (3), `TradeReconstructor.cs` (1 public + 1 private nested).

---

## 5. File-name vs type-name mismatches

| File | Declared public type(s) | Note |
|---|---|---|
| `Entities\DestinationQuote.cs` | `DestinationQuoteSnapshot` | Only mismatch. `Risk.DestinationQuote` lives in `RiskEngine.cs`. |
| `Risk\RiskEngine.cs` | 5 types including `DestinationQuote` | File named for the engine, not the quote DTO. |
| `Scoring\BaselineScorer.cs` | 4 types including `TraderStateMachine` | State machine has no own file. |
| `Shadow\ShadowCopyEngine.cs` | 3 types | `ShadowPosition` unused in Domain. |
| All other 43 authored files | type name == file name (minus `.cs`) | 1:1 |

---

## 6. Near-collision stems (census only; compile-clean)

| Stem | Persist (`Entities`) | Engine / in-memory |
|---|---|---|
| Canonical instrument | `CanonicalInstrument` | `CanonicalInstrumentRef` |
| Destination quote | `DestinationQuoteSnapshot` | `Risk.DestinationQuote` |
| Reconstructed trade | `ReconstructedTrade` | `ReconstructedTradeResult` |
| Risk decision | `RiskDecisionRecord` | `Risk.RiskDecision` |
| Shadow | `ShadowOrder` | `ShadowFill`, `ShadowPosition` |
| Broker id type | `Guid` on persist entities | `string` (broker **code**) on `NormalizedDeal`, `ReconstructedTradeResult`, `RiskEvaluationRequest`, `ShadowPosition` |

---

## 7. Honesty / what this census is not

- **Not** a claim that Domain is “complete vs architecture.” Several types exist as vocabulary only (`ReconciliationIssueType`) or as unused helpers (`ClOrdIdFactory`, `QuantityNormalizer`, `CopyIntentExpiry`, `ShadowPosition`).
- **Not** a claim of ≥95% parity with any EX5. This tree is the C# Trader Intelligence domain, not a decompile.
- **Not** a live-trading license. `TraderStateMachine.CanPromoteToLive` is `false`; `RiskEngine` sets `AllowFixSend` only when `RealExecutionEnabled` and kill-switch / venue / recon flags pass.
- Product source under `D:\Prop\src\Domain` was **not** modified by this agent.

**Census complete:** 53 `.cs` files (47 authored + 6 generated), 59 public types, 10 namespaces, 1 private nested type.
