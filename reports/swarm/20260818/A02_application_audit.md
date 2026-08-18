# A02 — Application layer audit

**Agent:** A02 (senior engineer, Application-only)  
**Scope:** `D:\Prop\src\Application` (product source + csproj). Build `bin/` / `obj/` cited only as compile evidence, not as design.  
**Compared to:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§ 6, 12, 13, 32, 39, 66 (brokers, outbox, risk, scoring, copy intents).  
**Classification scheme:** architecture §73.B — `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.  
**Product source modified:** no.

---

## 1. Inventory of `src/Application`

Hand-authored product files (complete):

| Path | Role |
|---|---|
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | class library project |
| `D:\Prop\src\Application\Class1.cs` | empty SDK template type |

No other `.cs` files. No `Abstractions/`, `Ports/`, `Interfaces/`, `Services/`, `UseCases/`, `Validators/`, `DTOs/`, `Features/`, `Copy/`, `Risk/`, `Scoring/`, `Outbox/`, `Brokers/` folders.

`Class1.cs` in full:

```csharp
namespace TraderIntelligence.Application;

public class Class1
{

}
```

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

Compile/restore evidence (not design):

- Target: `net8.0`, nullable on, implicit usings on.
- Project reference: `TraderIntelligence.Domain` only (clean-architecture direction is correct).
- Package: `FluentValidation` 11.9.2 restored; **zero** `IValidator<T>` / `AbstractValidator<T>` types exist under Application.
- Downstream refs exist (`apps/api`, `apps/mt5-worker`, `apps/fix-worker`, `src/Infrastructure`, `src/Mt5`, `src/Fix.CTrader`, unit/integration tests) but they consume an empty assembly.

`grep` of Application product source for `interface`, `IMt5`, `Outbox`, `Risk`, `Score`, `CopyIntent`, `IValidator` returns nothing except the empty `Class1` namespace declaration.

---

## 2. What architecture v2 requires of Application

§66 places `/src/Application` next to Domain and Infrastructure and says: “Adapt to the existing repo; do not create duplicates unnecessarily.” Sibling folders (`TradeReconstruction`, `Scoring`, `Shadow`, `Risk`, `Execution`) are **optional later extracts**. Until they exist, Application is the home for **use-cases and ports** that those sections name.

The only C# interface the architecture actually writes is `IMt5BrokerConnector` (§6). Everything else is a required port/service inferred from the named flows. Implementations belong in Infrastructure / Mt5 / Fix.CTrader / workers — **contracts belong here**.

---

## 3. Classification of each needed service / interface

Status for every row below is **MISSING** unless noted. Evidence is quoted from architecture v2. Application evidence is the empty `Class1` + csproj above.

### 3.1 Brokers — §6 (and §66 `/src/Mt5` adapter)

| ID | Needed Application type | Class | Why it belongs in Application | Architecture evidence |
|---|---|---|---|---|
| B1 | `IMt5BrokerConnector` | **MISSING** | Explicit port. SDK/native code must not leak into use-cases. Exact members may be adjusted to the SDK. | §6: “Create a broker registry.” Then: `public interface IMt5BrokerConnector { Task ConnectAsync(...); Task DisconnectAsync(...); Task<IReadOnlyCollection<Mt5Group>> GetGroupsAsync(...); Task<IReadOnlyCollection<Mt5Account>> GetAccountsAsync(...); Task<IReadOnlyCollection<Mt5Deal>> GetDealsAsync(...); Task<IReadOnlyCollection<Mt5Order>> GetOrdersAsync(...); Task<IReadOnlyCollection<Mt5Position>> GetPositionsAsync(...); IAsyncEnumerable<Mt5Event> SubscribeAsync(...); }` “The exact interface may be adjusted to the actual SDK.” |
| B2 | `IMt5BrokerRegistry` (or equivalent broker lookup by `broker_id`) | **MISSING** | “Support more brokers without duplicating business logic.” Registry is the Application composition point; Achiever/StarwaveFX are Infrastructure registrations. | §6: “The design must support more brokers without duplicating business logic. Create a broker registry.” “Do not build two mostly identical connector codebases.” Brokers named: Achiever, StarwaveFX. |
| B3 | Connector DTOs consumed by the port (`Mt5Group`, `Mt5Account`, `Mt5Deal`, `Mt5Order`, `Mt5Position`, `Mt5Event`) | **MISSING** in Application; Domain `Class1` is also empty | Types may live in Domain; Application must still *use* them on the port. Today neither layer has them. | §6 interface signatures cite those types. §10: compound identities `broker_id + login` / ticket / position. |

**Not Application (implementation only):** concrete Achiever/StarwaveFX SDK wrappers — §66 `/src/Mt5`.

### 3.2 Ingestion + transactional outbox — §§ 12, 13

| ID | Needed Application type | Class | Why | Architecture evidence |
|---|---|---|---|---|
| O1 | `IHistoricalBackfillService` (per broker/account) | **MISSING** | Orchestrates checkpoint → fetch → normalize → idempotent upsert → persist checkpoint. Uses `IMt5BrokerConnector`, does not own SDK. | §12 Historical backfill: `Read checkpoint → Fetch history → Normalize → Upsert idempotently → Persist checkpoint`. “For each broker/account”. |
| O2 | `ISyncCheckpointStore` | **MISSING** | Port for backfill/live resume. Persistence is Infrastructure. | §12 “Read checkpoint” / “Persist checkpoint”. §11 raw table `sync_checkpoints`. |
| O3 | `ILiveIngestionService` (validate → dedupe → persist raw → outbox → commit) | **MISSING** | Live path must be a single Application transaction boundary. MT5 callbacks must not call ML or FIX. | §12 Live flow: `MT5 event → validate → deduplicate → persist raw record → write transactional outbox event → commit`. “Then background workers process the outbox.” “This avoids coupling MT5 callbacks directly to ML or execution.” |
| O4 | `IRawMt5RecordWriter` (idempotent upsert of raw deals/orders/positions/accounts) | **MISSING** | Application names the write; Infrastructure implements. | §12 “persist raw record” / “Upsert idempotently”. §11 raw tables `mt5_accounts`, `mt5_deals`, `mt5_orders`, `mt5_positions_current`, `ingestion_events`. |
| O5 | `IIngestionReconciliationService` | **MISSING** | Third leg of the mandated pattern. | §12: “Historical Backfill + Live Event Subscription + Periodic Reconciliation”. |
| O6 | `ITransactionalOutbox` / `IOutboxWriter` | **MISSING** | Same DB transaction as raw persist. This is the §13 substitute for Kafka. | §13: “Use: PostgreSQL transactional outbox” for “trade-completed events, score-update requests, shadow-copy intents, risk-check requests, notification events.” |
| O7 | Outbox message contracts (at least five) | **MISSING** | Typed payloads so workers stay behind one abstraction. | §13 list (same five event kinds). |
| O8 | `IOutboxProcessor` (background drain) | **MISSING** | Application use-case; hosted in `apps/mt5-worker` / `apps/fix-worker`. | §12: “Then background workers process the outbox.” |
| O9 | `IEventBus` (thin abstraction over outbox now, Kafka later) | **MISSING** | Explicit migration seam. | §13: “If measured throughput later requires a dedicated broker, migrate behind an event-bus abstraction. Do not preemptively introduce distributed infrastructure.” |

`FluentValidation` in the csproj is the only hint that live-path `validate` was anticipated. **No validators exist.** Classify the package as **EXISTS_NEEDS_REFACTOR** (present, unused, not wired to §12 validate).

### 3.3 Copy intents + execution orchestration — §32 (plus §13 outbox kind “shadow-copy intents”)

| ID | Needed Application type | Class | Why | Architecture evidence |
|---|---|---|---|---|
| C1 | `ICopyCandidateEvaluator` | **MISSING** | First gate after source event: “Copy candidate?” | §32 flow: `Source MT5 event → Copy candidate? → Create CopyIntent`. |
| C2 | `ICopyIntentFactory` / `CreateCopyIntent` use-case | **MISSING** | Must persist intent **before** risk and **never** emit FIX from the MT5 callback. | §32: `Create CopyIntent → Persist`. “Never send a FIX order directly from an MT5 event callback.” §4 diagram: `Shadow copy ──> CopyIntent ──> Risk Engine`. |
| C3 | `ICopyIntentStore` | **MISSING** | Persistence port for the intent record. | §32: `Persist` after `Create CopyIntent`. §63: each CopyIntent must have `expires_at` and `max_signal_age`. |
| C4 | `CopyIntent` application/domain model (incl. expiry) | **MISSING** | Named first-class object in §4, §32, §63, §75. | §63: “Each CopyIntent must have: expires_at, max_signal_age. Stale entries expire.” |
| C5 | `IApprovedExecutionIntentService` (persist after risk) | **MISSING** | Second persist in the production flow; input to FIX worker. | §32: `RiskEngine evaluates → ApprovedExecutionIntent → Persist → FIX Execution Worker → NewOrderSingle`. §75: `CopyIntent → Risk Engine → ExecutionIntent`. |
| C6 | `IExecutionIntentStore` + client-order identity | **MISSING** | Idempotency fields are Application contracts even though §33 is adjacent. | §33 persist-before-send: `execution_intent_id`, `cl_ord_id`, `source_broker_id`, `source_login`, `source_trade_id`, `source_event_id`, `destination_account`, `canonical_symbol`, `side`, `requested_quantity`, `created_at`, `status`. Statuses: `not sent` / `sent but acknowledgement unknown` / `accepted` / `partially filled` / `filled` / `rejected` / `cancelled`. |
| C7 | `IFixExecutionWorker` **port** (not the worker host) | **MISSING** | Application defines “consume approved intent → send later”; `apps/fix-worker` hosts it. | §32: `FIX Execution Worker`. §66: `/apps/fix-worker` + `/src/Fix.CTrader` + `/src/Execution`. |

**Not Application:** QuickFIX session, NewOrderSingle encoding — Infrastructure / Fix.CTrader.

### 3.4 Risk — §39 (final authority; Application port)

| ID | Needed Application type | Class | Why | Architecture evidence |
|---|---|---|---|---|
| R1 | `IRiskEngine` | **MISSING** | Final authority. Scoring may not approve orders. | §39: “The risk engine is the final authority.” §32: `RiskEngine evaluates`. |
| R2 | Scoring → risk input DTO (`candidate`, `confidence`, `suggested allocation`) | **MISSING** | Constrains what ML/scoring is allowed to emit. | §39: “Scoring/ML may only produce: candidate, confidence, suggested allocation.” |
| R3 | `RiskDecision` (approve / reduce size / reject / pause trader / pause venue / global stop) | **MISSING** | Decision vocabulary used by C5. | §39: “Risk engine decides: approve, reduce size, reject, pause trader, pause venue, global stop.” |
| R4 | Hard-limit policy ports / evaluators (one engine, many checks) | **MISSING** | Limits are enumerated; they are Application policy, not FIX-layer ad hoc. | §39 hard limits: max loss per selected trader; max daily execution-account loss; max portfolio drawdown; max XAUUSD gross/net exposure; max position quantity; max number of open positions; max allowed spread; max quote age; max source-signal age; max tolerated price move; max slippage; max execution account margin usage; martingale block; abnormal sizing block; venue health requirement. |
| R5 | Outbox kind `risk-check requests` consumer | **MISSING** | Risk must be invoked from outbox, not from MT5 callback. | §13 outbox list includes “risk-check requests.” §12 forbids callback→execution coupling. |

Kill-switch / feature flags (§40–41) are adjacent, not in the requested section set. Application will eventually need `IKillSwitch` (`STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN`) and a `REAL_COPY_EXECUTION_ENABLED` gate; they are **out of §39 scope** but `IRiskEngine` cannot be complete without a venue-health / global-stop input. Flagged as **MISSING (adjacent, not scored as a §39 deliverable)**.

§66 optional `/src/Risk` extract: do **not** create it until `IRiskEngine` exists in Application. Creating an empty sibling project would violate “do not create duplicates unnecessarily.”

### 3.5 Scoring — §39 constraint + §66 `/src/Scoring` (and implied by §13 `score-update requests`)

§§ 6 / 12 / 13 / 32 / 39 / 66 do **not** write an `IScoringService` type. They do require Application-owned scoring **ports** so risk and outbox stay decoupled:

| ID | Needed Application type | Class | Why | Architecture evidence |
|---|---|---|---|---|
| S1 | `IScoringService` (baseline now, ML later) | **MISSING** | Produces only the §39 triple. Implementation may later move to `/src/Scoring` or `/services/ml-service`. | §39 output contract. §4: `Features + scoring`. §66: `/src/Scoring`, `/services/ml-service`. §13 outbox: “score-update requests.” |
| S2 | `IScoreUpdateRequestHandler` (outbox consumer) | **MISSING** | Continuous rescoring is async; must not run in the MT5 callback. | §13 “score-update requests.” §12 “avoids coupling MT5 callbacks directly to ML.” |
| S3 | Score result + trader-state write port | **MISSING** | Downstream of scoring; risk reads `candidate` / state, does not compute score. | §4 / §75: scoring → trader state → shadow → CopyIntent → risk. §66 `/src/Scoring`. |

Detailed score *features* and state machine (`INSUFFICIENT_DATA` … `DISQUALIFIED`) live in §§ 18 / 22, outside the requested section set. They are **not classified here** except to note Application has no hook those sections could attach to.

### 3.6 Application project / scaffold — §66

| ID | Item | Class | Evidence |
|---|---|---|---|
| P1 | `/src/Application` project exists | **EXISTS_NEEDS_REFACTOR** | §66 lists `/src/Application`. Folder + csproj exist; they contain no ports or use-cases. |
| P2 | Layering: Application → Domain only | **EXISTS_AND_GOOD** | csproj references only `..\Domain\TraderIntelligence.Domain.csproj`. No Infrastructure / Mt5 / FIX project refs (correct). Domain itself is also an empty `Class1`, so the reference is structurally right and functionally empty. |
| P3 | `net8.0` + nullable | **EXISTS_AND_GOOD** | Matches §5 “C# / .NET 8+ compatible stack.” |
| P4 | `FluentValidation` 11.9.2 | **EXISTS_NEEDS_REFACTOR** | Reasonable for §12 “validate”; unused. |
| P5 | `Class1` | **DEPRECATED** | Default `dotnet new classlib` leftover. Not a use-case. Dead once any real type is added. |
| P6 | Separate `/src/{TradeReconstruction,Scoring,Shadow,Risk,Execution}` projects | **MISSING** (and **must not be stub-duplicated**) | §66: “Adapt to the existing repo; do not create duplicates unnecessarily.” Empty Application is the existing adapt point. |
| P7 | Application use-case / port surface for §§ 6, 12, 13, 32, 39 | **MISSING** | Zero interfaces. |

Nothing in Application is **UNSAFE** (no secrets, no live-order path). The **risk** is absence: workers and API already reference this assembly, so a future implementer can put FIX sends or SDK calls here with no existing port discipline to stop them.

---

## 4. Gap matrix vs the six architecture sections

| § | Architecture mandate (abbrev.) | In `src/Application` | Class |
|---|---|---|---|
| 6 | `IMt5BrokerConnector` + broker registry; one business path for Achiever/StarwaveFX/future | No interface, no registry, no broker_id types | **MISSING** |
| 12 | Backfill + live subscribe + periodic reconcile; validate/dedupe/raw persist/outbox/commit | No services, no checkpoint port, no validators | **MISSING** |
| 13 | PostgreSQL transactional outbox + five event kinds + event-bus abstraction; no Kafka-first | No outbox writer/processor, no event types, no `IEventBus` | **MISSING** |
| 32 | CopyIntent persist → RiskEngine → ApprovedExecutionIntent persist → FIX worker; never FIX from MT5 callback | No CopyIntent, no execution-intent port, no “copy candidate” gate | **MISSING** |
| 39 | Risk is final authority; scoring emits only candidate/confidence/allocation; enumerated hard limits | No `IRiskEngine`, no decision type, no scoring port | **MISSING** |
| 66 | `/src/Application` as the application layer; don’t duplicate empty sibling projects | Project shell only (`Class1` + FluentValidation) | **EXISTS_NEEDS_REFACTOR** (shell) / **MISSING** (contents) |

---

## 5. Honest verdict

`TraderIntelligence.Application` is a **name-only layer**. It satisfies §66’s folder slot and the dependency arrow Application → Domain. It does **not** satisfy any interface or use-case required by §§ 6, 12, 13, 32, or 39.

**Counts (requested scope):**

- **EXISTS_AND_GOOD:** 2 (net8.0/nullable; Application→Domain only).
- **EXISTS_NEEDS_REFACTOR:** 2 (project shell; unused FluentValidation).
- **DEPRECATED:** 1 (`Class1`).
- **UNSAFE:** 0 in-tree.
- **MISSING:** 24 scored ports/services/models (B1–B3, O1–O9, C1–C7, R1–R5, S1–S3). Adjacent kill-switch not scored.

There is **no** measured application behavior to compare to architecture. Next Application work (when implementation is authorized) is to replace `Class1` with the ports above — starting with `IMt5BrokerConnector` + `IMt5BrokerRegistry` + `ITransactionalOutbox` + `ILiveIngestionService` — and keep risk/scoring/copy-intent as ports so Infrastructure and workers cannot invent a second contract.

This audit did not modify product source.
