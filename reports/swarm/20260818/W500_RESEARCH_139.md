# W500_RESEARCH_139 — Does `RiskEngine` sit between `CopyIntent` and `ExecutionIntent`?

| Field | Value |
|---|---|
| Slot | **139** |
| Agent | W500_RESEARCH_139 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_139.md` |
| Date | 2026-08-18 |
| Topic | Check `RiskEngine` sits between `CopyIntent` and `ExecutionIntent` |
| Goal context | Fetch **ALL** Achiever + StarwaveFX groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (no MT5 / FIX / proxy passwords, no tag 554, no `.env` values) |
| Live attach this slot | **No.** No Manager Connect, no FIX TLS, no order send. |
| Method | Independent recensus of the **current** tree. Full `read_file` of `CopyTradingService.cs` (257 lines), `CopyTradingHostedService.cs`, `RiskEngine.cs` (closes L189), `CopyIntent.cs`, `ExecutionIntent.cs`, `RiskDecision*.cs`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService` / `ReconstructionScoringService`, `DependencyInjection.cs`, `LiveIngestHostedService.cs`, `LiveMt5Registration.cs`, `NativeMt5BrokerConnector` group/user walk, `CTraderFixSession.cs` (135 lines), `CTraderFixLogonHostedService.cs`, `apps/fix-worker/Worker.cs`, `TraderDbContext.cs`, `LiveRuntimeStatus.cs`, `Program.cs` `/api/settings` + `/api/copy/*` + `/api/ops/resync`, `CTraderFixOptions`, `BaselineScorer.CanPromoteToLive`, `RiskEngineTests`, architecture §§4/32/39/75, A23. `grep` of `Evaluate(`, `IRiskEngine`, `new CopyIntent`, `new ExecutionIntent`, `ExecutionIntents.Add`, `35=D`, `NewOrderSingle` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`. Contrast grep on `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` + read of `migrations/059_risk_decisions_signals_cases.sql`. **No TLS. No Manager re-probe. No product edit.** |
| Binding law | Architecture §4 / §32 / §39 / §75; A23; E005 R001 |
| Same-question siblings (do not inherit) | `W500_RESEARCH_19.md`, `W500_RESEARCH_39.md`, `W500_RESEARCH_59.md`, `W500_RESEARCH_99.md` — **this file re-reads the tree**. **Slots 19/39/59/99 are stale on Evaluate caller count and DI.** They remain correct on `ExecutionIntent` writers = 0 and `35=D` absence. |
| SHA this slot | Not re-hashed (no shell). Prior E005 pin `RiskEngine.cs` SHA-256 `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D`. Body re-read this pass still matches that Evaluate / `AllowFixSend` conjunction. File closes at L189. |

**Honesty rule:** a Domain `Evaluate` method is **not** a seated §32 gate unless an `ExecutionIntent` is persisted after it. A `DbSet<ExecutionIntent>` is **not** persist-before-send. `AllowFixSend` is a DTO bit, **not** a socket. Calling `Evaluate` and then writing `SHADOW_ONLY` is a **sidecar**, not “risk sits between CopyIntent and ExecutionIntent.” Architecture diagrams are **law**, not proof the process implements them. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not “risk-gated live copy.” Do **not** tick §68 / §70 from this slot. Do **not** print secrets.

---

## 0. Verdict (binding)

**Architecture: YES. Runtime product: NO_HOP (ExecutionIntent never written). Live capital: NONE (`SAFE_BY_ABSENCE`).**

Slot answer: `RiskEngine` does **not** sit between `CopyIntent` and `ExecutionIntent`. The third node is never constructed. What **did** land since slots 59/99 is a **shadow sidecar**: `CopyTradingService.GenerateShadowIntentsAsync` persists a `CopyIntent`, calls `Evaluate`, persists `RiskDecisionRecord` with **`AllowFixSend = false` hardcoded**, then sets `Status = "SHADOW_ONLY"`. It never writes `ExecutionIntent`. The older ingest writer `PersistDemoShadowAsync` still **bypasses** Evaluate entirely.

| Question | Measured answer (slot 139) | Class |
|---|---|---|
| Must RiskEngine sit between CopyIntent and ExecutionIntent? | **Yes** — architecture + A23 | SPEC |
| Do the three types exist? | **Yes** — entities + `RiskEngine` class (closes L189) | EXISTS |
| Is `Evaluate` on any production path? | **Yes, one sidecar.** `CopyTradingService` L159. **Not** the ingest/score path. | PARTIAL_SIDECAR |
| Does that sidecar persist `ExecutionIntent`? | **No.** `new ExecutionIntent` / `ExecutionIntents.Add` = **0** | NO_HOP |
| Is `IRiskEngine` present? | **No.** Type does not exist. DI registers the concrete class; the only product caller `new()`s its own | MISSING_PORT |
| Does `PersistDemoShadowAsync` consult RiskEngine? | **No** — `SHADOW_ONLY` + `ShadowOrder` | BYPASS |
| Can copy send a live cTrader order now? | **No** — no `35=D` encoder; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false` | SAFE_BY_ABSENCE |
| Can we fetch ALL Achiever + Starwave groups + manager logins? | **Code path yes.** Prior measured census **18 / 8460**. This slot did not re-run the probe. | FETCH_OK (prior) |

Do **not** claim “risk sits between intents.” The hop named in the question requires `CopyIntent → Evaluate → ExecutionIntent`. Only the first two exist on one writer; the third is a `DbSet` with no writer. Do **not** claim live copy is gated by RiskEngine. Live copy is impossible because the sender does not exist.

One-line:

```text
SPEC: CopyIntent → RiskEngine → ExecutionIntent → 35=D.
CODE: two writers. (A) score → SHADOW_ONLY CopyIntent + ShadowOrder (no Evaluate).
      (B) hosted copy → CopyIntent → Evaluate → RiskDecision (AllowFixSend forced false) → SHADOW_ONLY
          (Reconciled const false ⇒ VENUE_NOT_RECONCILED; 0 ExecutionIntent; 0 35=D).
CAPITAL: SAFE_BY_ABSENCE. Fetch-all catalog is separate from copy send.
```

**Slot-139 vs 19 / 39 / 59 / 99 (measured drift, do not inherit):**

| Claim in 59/99 | Slot 139 re-read |
|---|---|
| Product `Evaluate(` callers = 0 | **STALE.** 1 product caller: `CopyTradingService` L159 |
| `RiskEngine` not in DI | **STALE.** `AddSingleton<RiskEngine>()` at `DependencyInjection.cs` L45. Unused by the caller (field `new()`) |
| `FEATURE_COPY_TRADING_ENABLED=false` | **STALE.** `Program.cs` L77 literal **true** |
| `RealCopyEnabled = false` hardcoded in DI | **STALE.** Bound from `REAL_COPY_EXECUTION_ENABLED` (L41). Value not printed. Flag is **not** a sender. |
| Hosted FIX service resets flag to false | **STALE.** `CTraderFixLogonHostedService` no longer writes `_runtime.RealCopyEnabled` |
| 0 `ExecutionIntent` writers | **STILL TRUE** |
| 0 `35=D` builders | **STILL TRUE** |
| `IRiskEngine` missing | **STILL TRUE** |

---

## 1. Architecture law (what “sits between” means)

Source of truth: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.  
Binding sibling spec: `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` (spec only; no product).

### 1.1 §4 diagram (line 211)

```text
Shadow copy ──> CopyIntent ──> Risk Engine ─────────┘  (then cTrader FIX)
```

### 1.2 §32 production flow (lines 1268–1298)

```text
Source MT5 event
      ↓
Copy candidate?
      ↓
Create CopyIntent
      ↓
Persist
      ↓
RiskEngine evaluates          ← required hop
      ↓
ApprovedExecutionIntent
      ↓
Persist                       ← persist BEFORE any FIX send
      ↓
FIX Execution Worker
      ↓
NewOrderSingle
```

Law: **Never send a FIX order directly from an MT5 event callback.**

### 1.3 §39 (lines 1496–1517)

“The risk engine is the final authority.” Scoring/ML may emit only `candidate` / `confidence` / `suggested allocation`. Risk decides `approve` / `reduce size` / `reject` / `pause trader` / `pause venue` / `global stop`.

`RiskDecisionOutcome` on disk (`D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs`) matches that six-way enum. Matching names ≠ a wired hop to `ExecutionIntent`.

### 1.4 §75 final target (lines 2843–2847)

```text
                 CopyIntent
                      ↓
                 Risk Engine
                      ↓
              ExecutionIntent
                      ↓
          cTrader QUOTE / TRADE FIX
                      ↓
                 NewOrderSingle
```

**PASS condition for this slot:** a worker must persist `CopyIntent` → call `RiskEngine.Evaluate` → persist `risk_decisions` → persist `ExecutionIntent` (only on APPROVE / REDUCE_SIZE) → FIX worker may send **only if** `AllowFixSend && REAL_COPY && READY_FOR_EXECUTION`. Steps 1–3 exist on **one** sidecar. Step 4 (`ExecutionIntent` persist) and step 5 (`35=D`) **do not exist**.

---

## 2. What exists on disk (types + one sidecar, not the §32 pipeline)

### 2.1 Domain helper — `D:\Prop\src\Domain\Risk\RiskEngine.cs` (closes L189)

| Type | Role |
|---|---|
| `RiskLimits` | Hardcoded lab caps (`MaxQuoteAge=3s`, `MaxSourceSignalAge=15s`, book/PnL/qty caps). **Not** bound from `appsettings` `RiskEngine:*`. |
| `RiskEvaluationRequest` | Caller-assembled snapshot: `CopyIntentId`, action, qty, quote, `RealExecutionEnabled`, `Reconciled`, `KillSwitch`. |
| `TraderIntelligence.Domain.Risk.RiskDecision` | **Record** return: `Outcome`, `ApprovedQuantity`, `Reason`, `AllowFixSend`. **Not** the EF entity. |
| `RiskEngine.Evaluate` | First-match reject sequence (L76–172). `AllowFixSend` computed at L147–150. |

`AllowFixSend` conjunction (L147–150):

```text
RealExecutionEnabled && KillSwitch == None && Reconciled && VenueHealthy
```

Empty no-op at L90–93: when `RealExecutionEnabled == false` and action is not `CloseExposure`, the method **comments** “Shadow path still evaluates risk but never allows FIX send” and then **falls through**. APPROVE + `AllowFixSend=false` is possible in unit tests. On the product sidecar it is **not** reached for opens: see §3.3 (`Reconciled` const false).

Reducing actions (`ReduceExposure` / `CloseExposure`) fall through to `Reason = "RISK_REDUCTION"`. The sidecar **never** emits those actions (always `OpenExposure`).

Name collision (do not confuse):

| Type | Namespace | Mapped? |
|---|---|---|
| `RiskDecision` record | `Domain.Risk` | Evaluate return only |
| `RiskDecision` class | `Domain.Entities` (`RiskDecision.cs`) | **Orphan** — not in `TraderDbContext` |
| `RiskDecisionRecord` | `Domain.Entities` | EF `risk_decisions` (`DbSet<RiskDecisionRecord> RiskDecisions`) |

### 2.2 Intent entities (FK slots imply the hop)

`CopyIntent` (`D:\Prop\src\Domain\Entities\CopyIntent.cs`, 24 lines):

```text
RiskDecisionId     Guid?     // set by CopyTradingService only
ExecutionIntentId  Guid?     // never set (grep product assignment = 0)
Status             default "Pending"; writers use "SHADOW_ONLY" (or "PENDING_RISK" then overwrite)
ExpiresAt          required
```

`ExecutionIntent` (`D:\Prop\src\Domain\Entities\ExecutionIntent.cs`, 21 lines):

```text
CopyIntentId, RiskDecisionId, ClOrdId, Status="Pending", SentAt, FilledAt
```

`RiskDecisionRecord` (`D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs`, 14 lines):

```text
CopyIntentId, Outcome, ApprovedQuantity, Reason, AllowFixSend, DecidedAt
```

EF (`TraderDbContext` L122–141):

| Table | Unique | Writer |
|---|---|---|
| `copy_intents` | `IdempotencyKey` | **two** — `PersistDemoShadowAsync` **and** `GenerateShadowIntentsAsync` |
| `risk_decisions` | index on `CopyIntentId` (**not unique**) | **one** — `CopyTradingService` L195 |
| `execution_intents` | `ClOrdId` unique (nullable) | **none** |

### 2.3 Adjacent helpers

| Helper | Path | Used by product? |
|---|---|---|
| `CopyIntentExpiry.IsExpired` | `src/Domain/Execution/CopyIntentExpiry.cs` | **Tests only** (`ExecutionAndSizingTests` L59–60) |
| `FixSessionOwnership.ExecutionIntentsAllowed` | `src/Fix.CTrader/Services/FixSessionOwnership.cs` L111 | **Unused** by worker |
| `ShadowCopyEngine.SimulateEntry` | `src/Domain/Shadow/ShadowCopyEngine.cs` | Ad-hoc `new` in store; field `new` in `CopyTradingService` |
| `QuantityNormalizer` | Domain | Used by `CopyTradingService` (allocation 0.05, gold spec). Unused by `PersistDemoShadowAsync` (passthrough `MaxVolumeLots`) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (L35) | Not bound onto `RiskEngine` |
| `OutboxEventType.RiskCheckRequest` | enum value 3 | **Never enqueued** (store writer uses `ScoreUpdate`) |
| `BaselineScorer.CanPromoteToLive` | L211 `=> false` | Hard pin; scoring cannot emit LIVE |

Application tree (`D:\Prop\src\Application\`): `Contracts/`, `Copy/CopyTradingModels.cs` (DTOs only), `Dashboard/`, `Ingestion/`, `Runtime/`. **No** `Risk/` port, **no** `IRiskEngine`, **no** `IExecutionIntentStore`.

Grep of `IRiskEngine` under product `src/**/*.cs`: **0**.

---

## 3. Measured call graph (this is the slot)

### 3.1 `Evaluate(` census (product + tests `*.cs`)

Workspace grep of `Evaluate(` on 2026-08-18 this pass:

| File | Hits | Kind |
|---|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` L76 | 1 | **definition** |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` L159 | 1 | **product sidecar** |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 6 | unit smoke (`_e.Evaluate`) — 5 `[Fact]`s, one fact calls twice |

**Product callers: 1** (`CopyTradingService`). Ingest, store, API, fix-worker, mt5-worker, and `CTraderFixSession` do **not** consult Evaluate.

`new RiskEngine`:

| Site | Kind |
|---|---|
| `CopyTradingService` L23 `private readonly RiskEngine _risk = new();` | product, **not** the DI singleton |
| `RiskEngineTests` L9 | test |
| `DependencyInjection.cs` L45 `AddSingleton<RiskEngine>()` | registered, **zero injectees** |

### 3.2 DI — `D:\Prop\src\Infrastructure\DependencyInjection.cs`

`AddTraderIntelligence` now registers:

- `LiveRuntimeStatus` with `RealCopyEnabled = (configuration["REAL_COPY_EXECUTION_ENABLED"] == "true")` — **env-bound**, not hardcoded false (L39–42).
- `AddScoped<CopyTradingService>()` (L44).
- `AddSingleton<RiskEngine>()` (L45) — unused (see §3.1).
- `AddHostedService<CopyTradingHostedService>()` (L59) — new vs slot 99.
- Native connectors, store, dashboard, reconstructor, scorer, ingest, `LiveIngestHostedService`, `CTraderFixLogonHostedService`.

Dummy/fake connectors still refused: throws if either MT5 password is missing / `<SECRET>` / `(a/c`.

### 3.3 Sidecar writer — `CopyTradingService.GenerateShadowIntentsAsync`

Hosted every 20s after an 8s delay (`CopyTradingHostedService`). Also painted by `GET /api/copy/status` and `GET /api/copy/intents`.

```text
TraderScores in {SHADOW, LIVE_CANDIDATE, LIVE}
  → completed XAU reconstructed trades
  → idempotency key copy:{broker}:{login}:{positionId}
  → new CopyIntent Status="PENDING_RISK"
  → _risk.Evaluate(...)
  → new RiskDecisionRecord { AllowFixSend = false }   ← HARDCODED, ignores decision.AllowFixSend
  → intent.RiskDecisionId = rec.Id
  → if (decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled)
        Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED"    ← DEAD: last two are const false
    else
        Status = "SHADOW_ONLY"
        ShadowOrder only if Outcome == Approve AND quote exists
```

Pinned constants on the class (L15–16):

```text
VenueReconciled            = false
NewOrderSingleImplemented  = false
AllocationFactor           = 0.05m
```

Evaluate request sets `Reconciled = VenueReconciled` (**false**) and `Action = OpenExposure` (increasing). Engine L84–85 therefore **always** returns `VENUE_NOT_RECONCILED` / `AllowFixSend=false` on this path. Consequence:

- `decision.Outcome` is **never** `Approve` here → sidecar **does not** write `ShadowOrder`.
- Dead live branch L198–201 is **unreachable**. Even if it ran, it would **only** change a status string. It does **not** `new ExecutionIntent`.
- Persist `AllowFixSend = false` at L192 would still win over a hypothetical engine true.

`GetStatusAsync` counts `ExecutionIntents.Where(SentAt != null)` — a read of an empty table. `LiveSends` is therefore 0 by construction.

### 3.4 Bypass writer — `EfTradingStore.PersistDemoShadowAsync`

Still called **after** score persist from `ReconstructionScoringService.RebuildTraderAsync` (`DealIngestionService.cs` L118–144):

```text
LoadDeals → Reconstruct → ReplaceReconstructed
  → Score → UpsertScoreAsync
  → PersistDemoShadowAsync(suggestedState, completedXau)
```

Writer behavior (`EfTradingStore.cs` L251–337):

| Gate | Effect |
|---|---|
| Always | `OutboxEvent` `ScoreUpdate` (not `ShadowCopyIntent`, not `RiskCheckRequest`) |
| `state != TraderState.SHADOW` | return — **0** intents |
| no `destination_quotes` row | return — **0** intents |
| SHADOW + quote | `new CopyIntent` with `Status = "SHADOW_ONLY"`, `Action = OpenExposure`, qty = `MaxVolumeLots` (source lots, **not** dest OrderQty), `ExpiresAt = trade.OpenedAt.AddSeconds(15)`, key `shadow:{broker}:{login}:{positionId}` |
| then | `new ShadowCopyEngine().SimulateEntry` → `ShadowOrder` **same SaveChanges** |

**Not set:** `Direction`, `SourcePositionId`, `SourceTradeId`, `RiskDecisionId`, `ExecutionIntentId`.

**Not called:** `RiskEngine.Evaluate`, `CopyIntentExpiry.IsExpired`.

**Not written:** `RiskDecisionRecord`, `ExecutionIntent`, FIX `35=D`.

The two writers use **different idempotency keys** (`shadow:…` vs `copy:…`). The same completed XAU trade can therefore produce **two** `CopyIntent` rows. Only the `copy:` row has a risk decision. Neither has an `ExecutionIntent`.

Grep of `new ExecutionIntent` / `ExecutionIntents.Add` in product `src/` + `apps/` `*.cs`: **0**.

### 3.5 Ingest host and resync never reach execution

`LiveIngestHostedService`:

```text
connect → SyncCatalogAsync → SyncBrokerAsync (deals/positions)
       → RebuildTraderAsync per ListLoginsWithDealsAsync
```

Stops at score + optional demo-shadow rows. **Does not** walk `ExecutionIntents`. **Does not** call Evaluate. The hosted **copy** loop is a **separate** service (`CopyTradingHostedService`).

`POST /api/ops/resync` (`apps/api/Program.cs` L114–150): catalog/deals/score for `ACHIEVER` then `STARWAVEFX`, scores **`ListLoginsAsync`** (every catalog login). Still **no** Evaluate. **No** execution intent. Copy sidecar is not invoked by resync (it runs on its own timer).

### 3.6 Dashboard “risk” is not the engine

`EfDashboardQueries.GetRiskAsync` (L198–208): latest `KillSwitch` + last 20 `RiskDecisionRecord.Reason` where outcome ≠ Approve. Now **can** show `VENUE_NOT_RECONCILED` if the sidecar has run. Exposure/PnL fields are still **0**. `RealCopyEnabled` comes from `_runtime` (env-bound). Paint endpoint, not a send gate.

`GET /api/settings` (`Program.cs` L71–84):

- `FEATURE_COPY_TRADING_ENABLED` = **true** (literal).
- `REAL_COPY_EXECUTION_ENABLED` echoes `runtime.RealCopyEnabled`.
- quote/signal ages are **literals** 3 / 15, not `RiskLimits` binding.

`SettingsController` (`apps/api/Controllers/SettingsController.cs`) still exposes a Redis DTO also named `RiskEngine`. `Program.cs` does **not** `AddControllers` / `MapControllers` (grep = **0**). Dead leftover. `appsettings.json` `"RiskEngine"` block is that decoy, **not** `Domain.Risk.RiskEngine`. `FeatureFlags:LiveCopyEnabled` in JSON is **false** and is **not** the env flag the copy service reads.

### 3.7 Intended vs actual

```text
INTENDED (§32 / §75 / A23)
  source event → CopyIntent persist → RiskEngine.Evaluate
    → RiskDecision persist → ExecutionIntent persist
    → FIX worker → 35=D (only if AllowFixSend ∧ flag ∧ recon)

ACTUAL (2026-08-18 product, slot 139 re-read)
  Manager catalog/deals → reconstruct → BaselineScorer
    → PersistDemoShadowAsync
         ├─ not SHADOW / no quote → outbox only
         └─ SHADOW + quote → CopyIntent Status=SHADOW_ONLY
                              + ShadowOrder (in-process fill)
         ✗ Evaluate never invoked on this path

  CopyTradingHostedService (every 20s)
    → GenerateShadowIntentsAsync
         → CopyIntent PENDING_RISK
         → Evaluate (Reconciled=false ⇒ VENUE_NOT_RECONCILED)
         → RiskDecisionRecord AllowFixSend=false (hardcoded)
         → Status SHADOW_ONLY
         ✗ ExecutionIntent never written
         ✗ 35=D does not exist
```

**Slot answer:** RiskEngine does **not** sit between CopyIntent and ExecutionIntent. The middle hop exists only as a shadow sidecar that cannot emit the third node. The third stage has no writer.

---

## 4. Copy to cTrader must not send live orders (no loss)

Capital constraint for the fetch-all-traders goal.

### 4.1 No NewOrderSingle builder

`CTraderFixSession.BuildLogon` (`src/Fix.CTrader/Sessions/CTraderFixSession.cs` L89–109) emits tag **35=A** only. Grep of product `*.cs` for the literal `35=D`: **0**. Only outbound MsgType constructor is `(35, "A")`. One `WriteAsync` (logon). Sockets are `using`/`await using` and disposed after one read.

`CTraderFixLogonHostedService` L68–70 logs `NewOrderSingle still unimplemented`. It **no longer** forces `_runtime.RealCopyEnabled = false` (drift vs slot 99). That is a **flag-honesty** change, not a sender. No TRADE keep-alive that could later emit `35=D`.

### 4.2 FIX worker cannot send even if the flag is flipped

`D:\Prop\apps\fix-worker\Worker.cs` L21–46:

- Reads `CTrader:RealCopyExecutionEnabled` default **false**.
- Stamps QUOTE/TRADE `Disconnected` with “No live TRADE socket. NewOrderSingle remains off.”
- If `real==true`, **logs a warning** and still has **no function** that can emit `35=D`.
- Does **not** query `ExecutionIntents`. Does **not** read `AllowFixSend`.

Flipping the flag cannot place an order.

### 4.3 Layered blockers (any one is enough)

| Site | Value |
|---|---|
| `CopyTradingService.NewOrderSingleImplemented` | **const false** |
| `CopyTradingService.VenueReconciled` | **const false** (also forces Evaluate reject on opens) |
| Persist `RiskDecisionRecord.AllowFixSend` | **literal false** |
| No `ExecutionIntent` writer | **0** |
| No `35=D` encoder | **0** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` (unbound to copy service) |
| `LiveRuntimeStatus.Snapshot` when armed | `"REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."` |
| `LiveRuntimeStatus.Snapshot` when disarmed | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |
| `BaselineScorer.CanPromoteToLive` | **always false** |
| `CopyTradingService` live branch | status string only; no send |

`REAL_COPY_EXECUTION_ENABLED` **may be true in `.env`** (slot 123 residual; this slot did not print the value). That does **not** authorize send. Do **not** treat env-true as capital at risk.

### 4.4 What *can* touch the venue

TLS **Logon 35=A** on QUOTE `:5211` and TRADE `:5212` if `CTRADER_FIX_PASSWORD` is present. Session proof / future recon, **not** copy. Password values are not written here.

**Risk to capital from the Prop copy path: NONE.** Safety is **absence of a sender**, plus const-false NOS / recon and forced-false persist bit. That is **not** a proven RiskEngine refuse-on-LoggedOn-TRADE test. `SAFE_BY_ABSENCE` ≠ go-live PASS (A100/A101 still fail).

---

## 5. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 5.1 Code path (current product)

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector` instances: `ACHIEVER` (`MT5_*` + `ACHIEVER_PROXY_*`) and `STARWAVEFX` (`MT5_STARWAVEFX_*`, `ProxyEnabled = false`). Dummy/fake connectors are refused.

Catalog (`DealIngestionService.SyncCatalogAsync` L37–51):

1. `GetGroupsAsync` — **all groups the manager can see**.
2. `GetAccountsAsync(null)` — **all logins in every group**.

Native implementation (`NativeMt5BrokerConnector.cs` L144–233):

| Step | API | Fallback |
|---|---|---|
| Groups | `GroupRequestArray("*")` L155 | `GroupTotal` / `GroupNext` |
| Users per group | `UserRequestArray` L223 / `UserGetByGroup` | `UserLogins` + `UserRequestByLogins` |
| Account money | `UserAccountRequestArray` | `UserAccountGetByGroup` |
| Deals | `DealRequestByGroup` per group | per-login |
| Positions | `GetGroupPositionsAsync("*")` | per-login |

`GetAccountsAsync(null)` unions every group name from `GetGroupsCore()`. There is **no** `Take(200)` on the catalog path. Logins unique per broker `(BrokerId, Login)`.

Achiever HTTP proxy is applied only when `ACHIEVER_PROXY_ENABLED=true` and host is set. Starwave is direct (`ProxyEnabled = false` hardcoded). Passwords / proxy auth are **not** printed.

### 5.2 Scoring is not the census

| Loop | Login set | Risk? | ExecutionIntent? |
|---|---|---|---|
| Hosted `LiveIngestHostedService` | `ListLoginsWithDealsAsync` | no (demo shadow only) | no |
| Manual `POST /api/ops/resync` | `ListLoginsAsync` (all catalog) | no | no |
| `CopyTradingHostedService` | scores in SHADOW / LIVE_CANDIDATE / LIVE | Evaluate sidecar | no |
| Catalog persist | all groups + all users | n/a | n/a |

Fetch-all is the **catalog**. Scoring/shadow is a **subset** on the hosted path (logins that have deals). That does **not** hide groups from `mt5_groups` / `mt5_accounts`.

### 5.3 Prior measured census (not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`  
JSON (logins, no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK via HTTP proxy | **8** | **6512** | 1506 |
| STARWAVEFX | OK direct | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (this manager’s visible universe): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

Starwave: `Starwave\cent\FX1\grp1/2`, `Starwave\demo\FX2\grp1/2`, `Starwave\real\FX3\grp1–5` + `LP`.

Honesty: those counts are **this manager login’s permission set**, not a claim the broker has no other groups. This slot **did not** reconnect; it confirms the **same fetch APIs are still the live path**.

Dashboard: `GET /api/groups`, `GET /api/traders` read EF catalog filled by that ingest. `/ready` returns group/account counts. `GET /api/copy/*` paints the sidecar, not live fills.

### 5.4 Fetch does not imply copy

Ingest + score + optional `SHADOW_ONLY` rows **cannot** promote to LIVE send. `CanPromoteToLive => false`. The copy sidecar cannot emit `ExecutionIntent`. There is no `35=D`.

---

## 6. YoPips C++ backend (relevant, not the copy hop)

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\`.

Targeted grep of `CopyIntent` / `ExecutionIntent` / `RiskEngine` / `AllowFixSend` / `NewOrderSingle` under that `src/`: **0**. Those names are a Prop C# design.

What YoPips *does* have:

| Piece | Role | Copy-to-cTrader? |
|---|---|---|
| `migrations/059_risk_decisions_signals_cases.sql` | Challenge `risk_policy_versions` / `risk_decisions` keyed by `challenge_id` / `risk_policy_version_id` | **No** — prop-firm compliance ledger |
| `mt5_group_probe` | Manager `GetAllGroups` JSON | Fetch helper, no orders |
| Trade execution services | Guarded **MT5 `SendTrade`** for owned challenge terminals | **Different product.** Not cTrader FIX copy |

Do **not** route Achiever/Starwave manager copy through YoPips `SendTrade`. Do **not** treat YoPips `risk_decisions` as `Domain.Risk.RiskEngine`. Same table *name*, different product, different schema.

---

## 7. Tests do not prove the hop

`D:\Prop\tests\Unit\RiskEngineTests.cs` — **5** facts, in-process `private readonly RiskEngine _e = new()`:

| Test | Asserts |
|---|---|
| Stale quote | `QUOTE_STALE`, `AllowFixSend=false` |
| Real flag false | `Approve` + `AllowFixSend=false` |
| Stop-new | blocks open, approves close with send still false |
| Unreconciled | `VENUE_NOT_RECONCILED` |
| Stale signal | `SIGNAL_STALE` |

No test constructs `CopyIntent` → `Evaluate` → `ExecutionIntent`. No test that a worker refuses `35=D` after reject. No test of `CopyTradingService`. A89 named suites (`RiskEngineHardLimitTests`, `RiskRejectionBeforeFixSendTests`) are **not on disk**. `AllowFixSend=true` facts: **0**.

The unreconciled fact is the one the sidecar always hits (const `VenueReconciled=false`). That is accidental alignment, not a pipeline test.

---

## 8. Honesty / gaps (do not greenwash)

1. **“RiskEngine sits between CopyIntent and ExecutionIntent” is FALSE in product.** True as architecture text. True as unused FK columns. False as a persist-before-send hop.
2. **Slots 19/39/59/99 are stale on Evaluate=0 and DI.** Do not copy those sentences forward. They remain correct on ExecutionIntent writers and `35=D`.
3. **Evaluate-on-sidecar ≠ seated gate.** The sidecar forces `AllowFixSend=false`, pins `VenueReconciled=false`, and has no `ExecutionIntent` constructor.
4. **`AllowFixSend` is not a control.** Zero sockets read it. Persist overwrites it to false.
5. **`SAFE_BY_ABSENCE` is the no-loss mechanism today.** Correct operating mode for “fetch all traders, do not lose money.” **Not** §68/§70 PASS.
6. **Two CopyIntent writers, two keys.** Demo shadow bypasses risk and writes fills. Hosted copy evaluates and (today) writes reject rows without fills. Both stay `SHADOW_ONLY`.
7. **Shadow page copy is still slightly wrong.** `ShadowPortfolioPage.tsx` L10 says “after a CopyIntent is **approved**.” Demo writer never calls Evaluate; sidecar never Approves (unreconciled).
8. **SettingsController `RiskEngine` JSON is a decoy** (unbound Redis keys; controller not mapped). `appsettings.json` `"RiskEngine"` is that decoy.
9. **Orphan `Entities.RiskDecision`** vs mapped `RiskDecisionRecord`.
10. **This slot did not re-measure 8460.** Cite `LIVE_MANAGER_FETCH_MEASURED.md`. Code still requests `*` groups and all users.
11. **Hosted scoring ≠ all 8460.** `ListLoginsWithDealsAsync` is deals-only. Catalog persist is still all-groups/all-users.
12. **Do not enable a `35=D` builder.** Do not treat env `REAL_COPY_EXECUTION_ENABLED=true` as authorization. Do not add send until Evaluate is on the persist-before-send path and A100/A101 measure PASS.
13. **DI `RiskEngine` singleton is dead.** The only product caller constructs its own instance.

---

## 9. What “wired” would look like (not built)

Required before any live copy (still **off**):

1. Persist `CopyIntent` per source **event** (not completed-trade OPEN backfill).
2. Inject the registered `RiskEngine` (or an `IRiskEngine` port); persist `RiskDecisionRecord` unique on `CopyIntentId`; persist the **engine** `AllowFixSend`, do not overwrite it with a literal.
3. Persist `ExecutionIntent` + unique `ClOrdId` **only** on APPROVE/REDUCE_SIZE **and** `AllowFixSend`.
4. Set `CopyIntent.RiskDecisionId` / `ExecutionIntentId`.
5. FIX worker consumes persisted intents; consults Evaluate outcome + flag + recon + ownership fence.
6. Quantity: never passthrough MT5 lots as cTrader `OrderQty` (sidecar already normalizes; demo writer does not).
7. Keep default send **impossible** until measured gates pass. Const-false `NewOrderSingleImplemented` must stay false until a real builder exists.

Until then: fetch-all-traders is the mission; live cTrader send stays impossible.

---

## 10. Evidence index (absolute paths)

| Path | Why it matters |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §4 / §32 / §39 / §75 law |
| `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` | Binding hop spec (not implemented) |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | Evaluate definition |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | **Only** product Evaluate caller; 0 ExecutionIntent writes; `AllowFixSend=false` persist |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s shadow-intent loop |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | `ExecutionIntentId` unused |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | No writer |
| `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs` | Mapped; sidecar writer |
| `D:\Prop\src\Domain\Entities\RiskDecision.cs` | Orphan entity; not mapped |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Demo CopyIntent writer; no risk |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog-all + score + shadow |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Registers unused RiskEngine singleton; env-bound RealCopy; hosted copy |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Fetch/score only; deals-only scoring set |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | ALL groups + ALL users |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever + Starwave pair |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; no NOS |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send; no builder |
| `D:\Prop\apps\api\Program.cs` | Resync = catalog/score; FEATURE_COPY true; copy GET endpoints |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Unmapped decoy RiskEngine DTO |
| `D:\Prop\apps\api\appsettings.json` | FeatureFlags.LiveCopyEnabled=false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Armed still “unimplemented; no ticket” |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false` |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 5 smoke facts; not a pipeline test |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups, 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_19.md` | Prior same-question recensus (Evaluate=0 **stale**) |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_39.md` | Prior same-question recensus (Evaluate=0 **stale**) |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_59.md` | Prior same-question recensus (Evaluate=0 **stale**) |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_99.md` | Prior same-question recensus (Evaluate=0 **stale**) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\migrations\059_risk_decisions_signals_cases.sql` | Different product’s risk tables |

---

*End of W500_RESEARCH_139. Product source was not modified. No secrets printed. This slot did not live-attach and did not send orders.*
