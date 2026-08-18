# W500_RESEARCH_79 — Does `RiskEngine` sit between `CopyIntent` and `ExecutionIntent`?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_79.md` |
| Agent / slot | W500 research **79** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (C# product) + `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (second-product check) |
| Topic | Check `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. |
| Goal context | Fetch ALL Achiever + Starwave groups and ALL manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** |
| Test source modified | **No.** |
| Secrets printed | **None.** No passwords, proxy auth, FIX tag 554, or `.env` values. |
| Live attach this slot | **No.** No Manager connect, no FIX TLS, no order send. Census numbers are cited from prior measured `CREDENTIALS_AND_COPY_STATUS.md` (same calendar day). |
| Method | Full `read_file` of `RiskEngine.cs` (189 lines), `CopyIntent.cs`, `ExecutionIntent.cs`, `ShadowCopyEngine.cs`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService` / `ReconstructionScoringService`, `DependencyInjection.cs`, `LiveIngestHostedService.cs`, `LiveMt5Registration.cs`, `NativeMt5BrokerConnector` group/account walk, `CTraderFixSession.cs`, `CTraderFixLogonHostedService.cs`, `apps/fix-worker/Worker.cs`, `TraderDbContext.cs`, architecture §§4/32/41/75, `RiskEngineTests.cs`, YoPips `trade_execution_service.h`. Targeted `grep` of product `*.cs` for `RiskEngine.Evaluate`, `new CopyIntent`, `new ExecutionIntent`, `35=D`, `NewOrderSingle`, DI registration. |
| Binding law | Architecture `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §4, §32, §39, §41, §63, §70.11, §75. Siblings: A23, A006, A009, B13/D13, C03/D35, E005, A003, E002, W500_RESEARCH_50. |
| SHA this slot | Not re-hashed (no shell). Prior E005 measured `RiskEngine.cs` SHA-256 `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` (189 lines). File re-read this pass matches that Evaluate / AllowFixSend body. |

**Honesty rule:** a domain class, an EF table, a FK column, a dashboard DTO named `RiskEngine`, or a unit test that constructs `new RiskEngine()` is **not** a pipeline seat. Architecture diagrams are **law**, not runtime. `AllowFixSend` is a DTO bit, not a socket. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a seated risk gate. Do **not** tick Architecture §68 / §70 from this slot.

---

## 0. Verdict (binding)

**NOT_WIRED — `RiskEngine` is designed to sit between `CopyIntent` and `ExecutionIntent`, but it does not sit there at runtime.**

| Claim | Result | Class |
|---|---|---|
| Architecture seat (`CopyIntent → Risk Engine → ExecutionIntent`) | **YES** — §4 L211, §32 L1275–1283, §75 L2843–2847 | **LAW** |
| Domain type exists (`RiskEngine.Evaluate`) | **YES** — `D:\Prop\src\Domain\Risk\RiskEngine.cs` L67–172 | **STUB / DEAD** |
| Request carries `CopyIntentId` | **YES** — `RiskEvaluationRequest.CopyIntentId` L34; decision echoes it L60 | **DTO** |
| `Evaluate` constructs / persists `ExecutionIntent` | **NO** — returns `RiskDecision` record only | **MISSING orchestrator** |
| Product caller of `RiskEngine.Evaluate` | **0** (only `tests/Unit/RiskEngineTests.cs`, 6 calls) | **DEAD** |
| `AddSingleton` / `AddScoped` / `AddTransient` of `RiskEngine` | **0** in `AddTraderIntelligence` | **NOT IN DI** |
| Product `new CopyIntent` | **1** — `EfTradingStore.PersistDemoShadowAsync` L295, `Status="SHADOW_ONLY"` | **SHADOW only** |
| Product `new ExecutionIntent` / `ExecutionIntents.Add` | **0** | **NEVER CREATED** |
| Product `RiskDecisions.Add` / persist of `RiskDecisionRecord` | **0** | **TABLE UNUSED** |
| `CopyIntent.RiskDecisionId` / `ExecutionIntentId` assigned | **Never** on the persist path | **FKs unused** |
| `CopyIntentExpiry` on persist path | **0** product callers (unit test only) | **DEAD helper** |
| `ClOrdIdFactory` on persist / send path | **0** product callers (unit test only) | **DEAD helper** |
| FIX worker consults `RiskEngine` | **NO** — stamps `Disconnected`, refuses NOS | **NOT A CONSUMER** |
| Live `35=D` / `NewOrderSingle` builder | **0** in product `*.cs` | **`SAFE_BY_ABSENCE`** |
| `RealCopyEnabled` | **forced `false`** (`DependencyInjection` L41; logon host L68) | **NO-LOSS GATE** |
| YoPips C++ `CopyIntent` / `RiskEngine` / `ExecutionIntent` | **0** hits under C++ backend path | **DIFFERENT PRODUCT** |
| This slot live-attached Manager / FIX | **No** | census cited, not re-proven |

One-line:

```text
LAW: CopyIntent → RiskEngine → ExecutionIntent. RUNTIME: CopyIntent(SHADOW_ONLY) → ShadowOrder; Evaluate() callers=0; ExecutionIntent ctors=0; 35=D=0. NOT_WIRED. Capital risk NONE (SAFE_BY_ABSENCE).
```

Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** add `35=D`. Wiring `Evaluate` is a later increment; this slot only measured the seat.

---

## 1. What “sits between” means (the assigned question)

A seated engine would be observable as **control flow**, not as vocabulary:

```text
persist CopyIntent
      ↓
RiskEngine.Evaluate(request with that CopyIntentId)
      ↓
persist RiskDecision / RiskDecisionRecord
      ↓
if AllowFixSend && REAL_COPY && other gates:
      persist ExecutionIntent (unique ClOrdId) BEFORE any socket write
      ↓
FIX worker send 35=D
```

Architecture states that sequence twice.

### 1.1 §4 (high-level)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L211:

```text
Shadow copy ──> CopyIntent ──> Risk Engine
```

### 1.2 §32 (production FIX flow)

Same file L1269–1287:

```text
Create CopyIntent
      ↓
Persist
      ↓
RiskEngine evaluates
      ↓
ApprovedExecutionIntent
      ↓
Persist
      ↓
FIX Execution Worker
      ↓
NewOrderSingle
```

Law: “Never send a FIX order directly from an MT5 event callback.”

### 1.3 §75 (end-to-end stack)

Same file L2843–2856:

```text
CopyIntent
      ↓
Risk Engine
      ↓
ExecutionIntent
      ↓
cTrader TRADE FIX SSL :5212
      ↓
NewOrderSingle
```

A23 (`A23_risk_engine_spec.md`) restates the same sandwich and names Risk the **final authority** (§39). That is specification. This slot measured whether the sandwich is **code**.

---

## 2. Domain types (exist; do not form a pipeline by themselves)

### 2.1 `CopyIntent` entity

Path: `D:\Prop\src\Domain\Entities\CopyIntent.cs` (24 lines).

Relevant fields:

| Field | Role vs seat |
|---|---|
| `Action` (`CopyIntentAction`) | Input family for Evaluate |
| `RequestedQuantity` / `ExpectedPrice` / `SourceEventTime` / `ExpiresAt` | Risk inputs / §63 expiry |
| `Status` (string, default `"Pending"`) | Persist path writes `"SHADOW_ONLY"` |
| `RiskDecisionId` (`Guid?`) | **Never assigned** in product persist |
| `ExecutionIntentId` (`Guid?`) | **Never assigned** in product persist |
| `IdempotencyKey` | Used: `shadow:{brokerId}:{login}:{positionId}` |

The FK slots are the **schema implication** of the sandwich. Empty FKs are evidence the sandwich is not run.

### 2.2 `ExecutionIntent` entity

Path: `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` (21 lines).

```text
CopyIntentId     (required Guid)
RiskDecisionId   (required Guid)
ClOrdId          (nullable string; unique index in EF)
Status           default "Pending"
SentAt / FilledAt / FixOrderId / RejectReason
```

The type **declares** that an execution row is born from a copy row **and** a risk row. Nothing in product code constructs it. `ClOrdIdFactory` is only constructed in `ExecutionAndSizingTests`.

### 2.3 `RiskEngine` (pure function)

Path: `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189 lines).

- Constructor: `RiskEngine(RiskLimits? limits = null)` — no I/O, no DbContext, no FIX.
- Single public method: `Evaluate(RiskEvaluationRequest) → RiskDecision`.
- `RiskDecision` here is a **record in `TraderIntelligence.Domain.Risk`**, not the unused entity `Domain.Entities.RiskDecision`, and not `RiskDecisionRecord`.

`AllowFixSend` conjunction (L147–150):

```csharp
var allowSend = request.RealExecutionEnabled
                && request.KillSwitch == KillSwitchMode.None
                && request.Reconciled
                && request.VenueHealthy;
```

Rejects set `AllowFixSend = false` (L187). Reducing actions can `Approve` with `AllowFixSend = allowSend` (L152–161). Opens can `Approve` with `AllowFixSend = false` when `RealExecutionEnabled == false` (unit fact `Real_flag_false_never_allows_fix_send`).

The empty branch at L90–93 is a comment, not a persist:

```csharp
if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
{
    // Shadow path still evaluates risk but never allows FIX send.
}
```

That comment describes the **intended** shadow evaluation. The live persist path never calls `Evaluate` at all.

Name collision (do not confuse):

| Type | Namespace | Mapped? | Callers |
|---|---|---|---|
| `RiskEngine` | `Domain.Risk` | n/a (not an entity) | **tests only** |
| `RiskDecision` record | `Domain.Risk` | no | returned by Evaluate |
| `RiskDecision` entity | `Domain.Entities` | **no** (not in `TraderDbContext`) | **none** |
| `RiskDecisionRecord` | `Domain.Entities` | yes → `risk_decisions` | **read** on dashboard; **never written** |
| `SettingsController.RiskEngine` | API Redis DTO | Redis keys | **not** the domain engine; live `/api/settings` is a **minimal-API lambda** in `Program.cs` L70–83, not this controller |

### 2.4 EF mapping (tables exist)

`D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`:

| DbSet | Table | Index of interest |
|---|---|---|
| `CopyIntents` L24 | `copy_intents` L124 | unique `IdempotencyKey` |
| `RiskDecisions` (`RiskDecisionRecord`) L25 | `risk_decisions` L131 | `CopyIntentId` (non-unique) |
| `ExecutionIntents` L26 | `execution_intents` L138 | unique `ClOrdId` |

Schema is ready for the sandwich. Writers for the last two tables are **absent**.

---

## 3. Measured runtime path (the actual seat)

### 3.1 Ingest → score → shadow (no risk, no execution)

`LiveIngestHostedService` (`D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`):

1. Per connector: `ConnectAsync` → `SyncCatalogAsync` (groups + accounts).
2. Per connected broker: `SyncBrokerAsync` (deals + positions).
3. Per broker: `ListLoginsWithDealsAsync` → `ReconstructionScoringService.RebuildTraderAsync`.

`RebuildTraderAsync` (`DealIngestionService.cs` L119–145):

```text
LoadDeals → Reconstruct → ReplaceReconstructed
  → BaselineScorer.Score
  → UpsertScore
  → PersistDemoShadowAsync(...)
```

There is **no** `RiskEngine` argument, **no** `Evaluate`, **no** `ExecutionIntent`.

### 3.2 The only `new CopyIntent` in product

`EfTradingStore.PersistDemoShadowAsync` L251–337:

| Gate | Behavior |
|---|---|
| Always | write `OutboxEvent` `ScoreUpdate` |
| `state != SHADOW` | save and **return** — no CopyIntent |
| no `DestinationQuotes` row | save and **return** — no CopyIntent |
| else, per completed XAU trade | idempotent `shadow:{broker}:{login}:{positionId}` |

Created row (L295–309):

- `Action = OpenExposure`
- `RequestedQuantity = trade.MaxVolumeLots` (source lots, **not** FIX OrderQty)
- `ExpiresAt = trade.OpenedAt.AddSeconds(15)` (set, **not** checked)
- `Status = "SHADOW_ONLY"`
- `RiskDecisionId` omitted → null
- `ExecutionIntentId` omitted → null
- `Direction` / `SourcePositionId` omitted → defaults

Then `new ShadowCopyEngine().SimulateEntry(...)` writes a `ShadowOrder`. That is **CopyIntent → ShadowOrder**, skipping RiskEngine and ExecutionIntent.

`CopyIntentExpiry.IsExpired` is **not** called here (only `ExecutionAndSizingTests` L59–60).

### 3.3 Who calls `Evaluate`?

`grep` of `RiskEngine.Evaluate` / `_e.Evaluate` / `new RiskEngine` on `D:\Prop` `*.cs`:

| Location | Hits |
|---|---:|
| `tests/Unit/RiskEngineTests.cs` | **6** `Evaluate` + **1** `new RiskEngine()` |
| `src/**/*.cs` product | **0** callers |
| `apps/**/*.cs` | **0** |
| `Infrastructure/DependencyInjection.cs` | **0** registrations |

E005 already counted product `Evaluate(` callers = **0**. This pass reconfirms.

`RiskEngineTests` (87 lines) is **5 smoke facts**, not a seated-pipeline suite:

- stale quote → `QUOTE_STALE`, `AllowFixSend=false`
- `RealExecutionEnabled=false` → `Approve` + `AllowFixSend=false`
- kill switch blocks open, not close
- unreconciled → `VENUE_NOT_RECONCILED`
- stale signal → `SIGNAL_STALE`

None persist `CopyIntent` or `ExecutionIntent`. None open a socket.

### 3.4 FIX / worker still cannot consume an ExecutionIntent

| Component | What it does | RiskEngine? | 35=D? |
|---|---|---|---|
| `CTraderFixSession.TryLogonAsync` (135 lines) | TLS Logon `(35,"A")` only; `using` dispose | no | **0** |
| `CTraderFixLogonHostedService` | optional QUOTE 5211 / TRADE 5212 logon; `_runtime.RealCopyEnabled = false` L68 | no | log string only |
| `apps/fix-worker/Worker.cs` | every 15s stamps both sessions `Disconnected`; `LastError` “NewOrderSingle remains off.” | **does not query CopyIntents / ExecutionIntents / RiskEngine** | refuses even if config `real==true` (L45–46 warning) |
| `AddTraderIntelligence` | `RealCopyEnabled = false` L41 with comment “Live NewOrderSingle is not implemented” | RiskEngine **not registered** | n/a |
| `LiveRuntimeStatus.Snapshot` | copyNote = “NewOrderSingle disabled. SHADOW/CopyIntent only.” | n/a | n/a |
| `Program.cs` `/api/settings` | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (forced false) | n/a | n/a |
| `FixSessionOwnership.ExecutionIntentsAllowed` | in-memory fence bit | **unused by worker** | n/a |

Product `grep` `35=D` / `NewOrderSingle` / `GuardedNewOrderSingle`: **no builder**. `NewOrderSingle` hits are comments, logs, `LastError` English, and `MayRetryNewOrderSingle` status math.

W500_RESEARCH_50 (same day, assigned `CTraderFixSession.cs`): **PASS**, `SAFE_BY_ABSENCE`. This slot does not re-open TLS.

---

## 4. Achiever + Starwave: ALL groups / ALL manager traders

Goal constraint (do not shrink the census). This slot **did not** live-attach. Code path vs prior measured census:

### 4.1 Connectors registered

`LiveMt5Registration.CreateConnectors` returns **exactly two** `NativeMt5BrokerConnector` instances:

| Broker | Proxy | Password env (name only) |
|---|---|---|
| Achiever | `ACHIEVER_PROXY_*` when `ACHIEVER_PROXY_ENABLED` parses true | `MT5_PASSWORD` |
| StarwaveFX | **`ProxyEnabled = false` hardcoded** L45 | `MT5_STARWAVEFX_PASSWORD` |

Dummy/fake is refused: `HasRealPasswords` must be true or DI throws (`DependencyInjection` L35–36).

### 4.2 Group walk (ALL groups)

`NativeMt5BrokerConnector.GetGroupsCore` L144–186:

1. `GroupRequestArray("*", arr)` — request-complete, not pump-cache-only.
2. If empty: fallback `GroupTotal` + `GroupNext`.

`DealIngestionService.SyncCatalogAsync` L45–46: `GetGroupsAsync` → `UpsertGroupsBatchAsync` for **every** returned group.

### 4.3 Trader walk (ALL manager traders in catalog)

`GetAccountsAsync(null)` L189–213:

- group filter null → `foreach (var g in GetGroupsCore())` then `ReadAccountsForGroup`.
- `ReadAccountsForGroup`: `UserRequestArray(gname)` → else `UserGetByGroup` → else `UserLogins` + `UserRequestByLogins`.

`SyncCatalogAsync` L48–49: `GetAccountsAsync(null)` → `UpsertAccountsBatchAsync` — **all logins the manager can see**, not a 200-row cap on this method.

Honest gap (not hidden): **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106). Traders with zero deals in the ingest window are catalogued, **not** scored, **not** given CopyIntents. That is a scoring-scope gap, not a live-order path.

### 4.4 Prior same-day live census (not re-measured here)

`D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`:

| Broker | Connect | Groups | Traders |
|---|---|---:|---:|
| Achiever | HTTP proxy (allow-list) | 8 | 6512 |
| StarwaveFX | direct | 10 | 1948 |
| **Total** | | **18** | **8460** |

`/api/traders` **8460**, `/api/groups` **18** on that measurement. This slot does not claim a new attach.

---

## 5. YoPips C++ backend (out of the copy sandwich)

Path grepped: `D:\Projects\YoPips\Backend\C++ Backend PropFirm`.

| Token | Hits |
|---|---:|
| `RiskEngine` | **0** |
| `CopyIntent` | **0** |
| `ExecutionIntent` | **0** |

`src/services/trade_execution_service.h` is a **different product**: YoPips challenge-account **MT5 `SendTrade`** after HTTP `/api/mt5/accounts/{login}/orders`. Guards are ownership / challenge rules / HFT / idempotency. It is **not** MT5-source → cTrader-destination copy. It is not a second cTrader FIX sender (W500_RESEARCH_50 already: C++ `src` `35=D` / `NewOrderSingle` / `FIX.4` = 0).

Do not treat YoPips terminal order placement as this copy pipeline. Do not send live cTrader orders from Prop because C++ can SendTrade on MT5.

---

## 6. No-loss / capital risk (goal: copy must not send live yet)

| Gate | State this pass |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` / `RealCopyEnabled` | **false**, assigned in two hard pins (DI + logon host) |
| NOS encoder | **missing** |
| Persist-before-send | **missing** (`ExecutionIntent` never inserted) |
| RiskEngine on send path | **missing** (and would still need `AllowFixSend` **and** a builder) |
| FIX worker | refuses NOS even if config flipped |
| Shadow persist status | `"SHADOW_ONLY"` |
| This slot sockets | **none** |

**Risk to capital: NONE** (`SAFE_BY_ABSENCE`).

That is **not** the same as “RiskEngine is the last authority before send” (§70.11). §70.11 remains **FAIL** until an orchestrator persists CopyIntent → Evaluate → persist decision → persist ExecutionIntent → send only if `AllowFixSend` **and** flag **and** recon. E005 already refused to tick that box. This slot agrees.

If someone later wires `Evaluate` but forgets the persist-before-send and the NOS choke, the stub has known STUB_WRONG / PARTIAL rules (E005: 11 STUB_WRONG, 22 PARTIAL, 41 MISSING). **Do not enable live send** to “use” this engine.

`docs/risk.md` describes a **different** limits vocabulary (5% daily, 50 lots, delay 100–2000 ms, MT5 submission). That doc is **not** the seated C# engine. Do not treat it as implementation.

---

## 7. Call-graph (measured)

```text
LiveIngestHostedService
  ├─ DealIngestionService.SyncCatalogAsync     → ALL groups + ALL accounts
  ├─ DealIngestionService.SyncBrokerAsync      → deals / positions
  └─ ReconstructionScoringService.RebuildTraderAsync   (logins WITH deals only)
        ├─ TradeReconstructor
        ├─ BaselineScorer
        └─ EfTradingStore.PersistDemoShadowAsync
              ├─ Outbox ScoreUpdate            (always)
              ├─ new CopyIntent SHADOW_ONLY    (SHADOW + quote only)
              └─ ShadowCopyEngine.SimulateEntry → ShadowOrder
                    ✗  RiskEngine.Evaluate
                    ✗  RiskDecisionRecord persist
                    ✗  ExecutionIntent persist
                    ✗  CopyIntentExpiry
                    ✗  QuantityNormalizer / ClOrdIdFactory
                    ✗  FIX 35=D

CTraderFixLogonHostedService  → 35=A logon only; RealCopyEnabled=false
FixWorker                     → Disconnected paint; no intent drain
RiskEngine                    → constructed only in RiskEngineTests
```

---

## 8. What would change the verdict to SEATED

All of the following, measured, not claimed:

1. Product orchestrator (not a test) persists `CopyIntent` then calls `RiskEngine.Evaluate`.
2. Persist `RiskDecisionRecord`; set `CopyIntent.RiskDecisionId`.
3. Create `ExecutionIntent` **only** after an approving decision **and** `AllowFixSend` (or a named close/reduce policy) **and** `RealCopyEnabled`.
4. Unique `ClOrdId` persisted **before** any future `35=D`.
5. FIX worker drains `ExecutionIntent`, not MT5 callbacks.
6. Integration test: reject reasons never reach a send spy; approve+flag-off never send.

Until then the honest label is **NOT_WIRED**. Schema + stub + tests ≠ seat.

---

## 9. Files read (absolute)

- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`
- `D:\Prop\src\Domain\Entities\RiskDecision.cs`
- `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs`
- `D:\Prop\src\Domain\Enums\CopyIntentAction.cs`
- `D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (PersistDemoShadowAsync L251–337)
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` (`GetRiskAsync` reads empty `RiskDecisions`)
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (groups/accounts)
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\Controllers\SettingsController.cs` (name collision only)
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§4/32/41/75
- `D:\Prop\docs\risk.md` (doc-only; not the SUT)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\E005_rules_matrix.md`
- `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\trade_execution_service.h`

---

## 10. Slot close

| Item | Value |
|---|---|
| Slot | **79** |
| Verdict | **NOT_WIRED** |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE`) |
| Live orders this slot | **0** |
| Product source edited | **No** |
| Secrets | **None printed** |

*End of W500_RESEARCH_79. Product source was not modified. No secrets printed.*
