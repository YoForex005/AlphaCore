# W500_RESEARCH_119 — Does `RiskEngine` sit between `CopyIntent` and `ExecutionIntent`?

| Field | Value |
|---|---|
| Slot | **119** |
| Agent | W500_RESEARCH_119 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_119.md` |
| Date | 2026-08-18 |
| Topic | Check `RiskEngine` sits between `CopyIntent` and `ExecutionIntent` |
| Goal context | Fetch **ALL** Achiever + StarwaveFX groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (no MT5 / FIX / proxy passwords; `.env` values other than the public boolean flag name are not quoted) |
| Method | Independent recensus. `read_file` + `grep` on current `D:\Prop` product C#, architecture, tests, apps, and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. No Manager re-probe. No TLS. No product edit. Prefer false negatives over fake PASS. |
| Siblings (same question) | `W500_RESEARCH_19.md`, `W500_RESEARCH_59.md` — **stale on “0 Evaluate callers.”** This file re-reads the tree. |

---

## 0. Verdict (binding)

**Architecture: YES. Product hop to `ExecutionIntent`: NO. Evaluate after `CopyIntent`: YES on one writer only. Live capital: NONE (`SAFE_BY_ABSENCE` + welded send constants).**

Slot answer: `RiskEngine` does **not** sit between persisted `CopyIntent` and persisted `ExecutionIntent`. There is still **no** `ExecutionIntent` writer. What landed after slots 19/59 is a **shadow-only** side path that constructs a `CopyIntent`, calls `RiskEngine.Evaluate`, persists `RiskDecisionRecord` with `AllowFixSend` **forced false**, then labels the row `SHADOW_ONLY`. That is **not** `CopyIntent → Evaluate → ExecutionIntent → 35=D`.

A second writer (`PersistDemoShadowAsync`) still creates `CopyIntent` + `ShadowOrder` **without** Evaluate.

| Question | Measured answer | Class |
|---|---|---|
| Must RiskEngine sit between CopyIntent and ExecutionIntent? | **Yes** — architecture §4 / §32 / §39 / §75 + A23 | SPEC |
| Do the three types exist? | **Yes** — entities + `RiskEngine` class | EXISTS |
| Is `Evaluate` on any production path? | **Yes, one** — `CopyTradingService.GenerateShadowIntentsAsync` L159 | PARTIAL |
| Is `IRiskEngine` present? | **No.** Type still missing. DI registers concrete `RiskEngine` singleton that the hop does **not** inject | MISSING |
| Does any writer persist `RiskDecisionRecord` after Evaluate? | **Yes** — same method L185–196 | PARTIAL |
| Does any writer persist `ExecutionIntent`? | **No** (`new ExecutionIntent` / `ExecutionIntents.Add` = **0**) | MISSING |
| Does every CopyIntent writer consult RiskEngine? | **No** — `PersistDemoShadowAsync` still bypasses | BYPASS |
| Can copy send a live cTrader order now? | **No** — no `35=D` encoder; `NewOrderSingleImplemented=false`; `VenueReconciled=false` | SAFE_BY_ABSENCE |
| Can we fetch ALL Achiever + Starwave groups + manager logins? | **Code path yes.** Prior measured census **18 / 8460**. This slot did not re-run the probe. | FETCH_OK (prior) |

Do **not** claim “risk sits between intents.” Persisting a reject row after Evaluate is not `ApprovedExecutionIntent`. A DTO bit named `AllowFixSend` is not a socket; this writer **overwrites it to false** on disk. Do **not** claim live copy is gated by RiskEngine. Live copy is impossible because the sender does not exist.

One-line:

```text
SPEC: CopyIntent → RiskEngine → ExecutionIntent → 35=D.
CODE: (A) score → SHADOW_ONLY CopyIntent + ShadowOrder (no Evaluate)
      (B) hosted copy tick → CopyIntent → Evaluate → risk_decisions (AllowFixSend:=false) → SHADOW_ONLY; 0 ExecutionIntent; 0 35=D.
CAPITAL: NONE. Fetch-all catalog is separate from copy send.
```

**Slot-119 vs slot-19/59:** those reports said product Evaluate callers = 0 and RiskEngine unregistered. That is **stale**. Current tree: `CopyTradingService` + `CopyTradingHostedService` + `AddSingleton<RiskEngine>()` + `/api/copy/*`. The **ExecutionIntent** gap and **no `35=D`** have **not** closed.

---

## 1. Architecture law (what “sits between” means)

Source of truth: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.  
Binding sibling spec: `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` (spec only; not fully implemented).

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

`RiskDecisionOutcome` (`D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs`) matches that six-way enum. Matching names ≠ a complete hop.

### 1.4 §75 final target (lines 2843–2857)

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

**PASS condition for this slot:** a worker must persist `CopyIntent` → call `RiskEngine.Evaluate` → persist `risk_decisions` → persist `ExecutionIntent` (only on APPROVE / REDUCE_SIZE) → FIX worker may send **only if** `AllowFixSend && REAL_COPY && READY_FOR_EXECUTION`. That conjunction is **not** implemented. Evaluate + `risk_decisions` exist on **one** writer; `ExecutionIntent` and send do **not**.

---

## 2. What exists on disk (types + new copy service)

### 2.1 Domain helper — `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189 lines)

| Type | Role |
|---|---|
| `RiskLimits` | Hardcoded lab caps (`MaxQuoteAge=3s`, `MaxSourceSignalAge=15s`). **Not** bound from `appsettings` `RiskEngine:*`. |
| `RiskEvaluationRequest` | Caller-assembled snapshot including `RealExecutionEnabled`, `Reconciled`, `KillSwitch`. |
| `TraderIntelligence.Domain.Risk.RiskDecision` | **Record** return: `Outcome`, `ApprovedQuantity`, `Reason`, `AllowFixSend`. **Not** the EF entity. |
| `RiskEngine.Evaluate` | First-match reject sequence. `AllowFixSend` computed at L147–150. |

`AllowFixSend` conjunction (L147–150):

```text
RealExecutionEnabled && KillSwitch == None && Reconciled && VenueHealthy
```

Increasing actions with `Reconciled == false` reject immediately (`VENUE_NOT_RECONCILED`, L84–85). The live copy service **always** passes `Reconciled = VenueReconciled` and `VenueReconciled` is a **const false** (see §3.3). Every `OpenExposure` from that writer is therefore a reject.

Name collision (do not confuse):

| Type | Namespace | Mapped? |
|---|---|---|
| `RiskDecision` record | `Domain.Risk` | Evaluate return only |
| `RiskDecision` class | `Domain.Entities` (`RiskDecision.cs`) | **Orphan** — not in `TraderDbContext` |
| `RiskDecisionRecord` | `Domain.Entities` | EF `risk_decisions` (`DbSet<RiskDecisionRecord> RiskDecisions`) |

### 2.2 Intent entities (FK slots)

`CopyIntent` (`D:\Prop\src\Domain\Entities\CopyIntent.cs`, 24 lines):

```text
RiskDecisionId     Guid?     // set by CopyTradingService; unset by PersistDemoShadowAsync
ExecutionIntentId  Guid?     // never set by any writer
Status             default "Pending"; writers use "SHADOW_ONLY" / "PENDING_RISK"
ExpiresAt          required; demo writer uses OpenedAt+15s (already stale for history)
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
| `copy_intents` | `IdempotencyKey` | **two** — `PersistDemoShadowAsync` (`shadow:…`) and `GenerateShadowIntentsAsync` (`copy:…`) |
| `risk_decisions` | index on `CopyIntentId` (**not unique**) | **one** — `GenerateShadowIntentsAsync` |
| `execution_intents` | `ClOrdId` unique (nullable) | **none** |

Different idempotency prefixes mean the **same source trade can produce two CopyIntent rows**. The demo path does not consult the copy path’s key, and vice versa.

### 2.3 Application / Infrastructure tree (drift vs slot 59)

Application (`D:\Prop\src\Application\`): `Contracts/`, **`Copy/CopyTradingModels.cs`**, `Dashboard/`, `Ingestion/`, `Runtime/`. Still **no** `IRiskEngine`, **no** `ICopyIntentFactory`, **no** `IApprovedExecutionIntentService`, **no** `IExecutionIntentStore`.

Infrastructure now has:

| File | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Evaluate + risk persist + shadow; **no** ExecutionIntent |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick after 8s delay; calls `GenerateShadowIntentsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Registers both + `AddSingleton<RiskEngine>()` |

Grep of `IRiskEngine` / `ICopyIntentFactory` / `IApprovedExecution` / `IExecutionIntent` under product `*.cs`: **0**.

---

## 3. Measured call graph (this is the slot)

### 3.1 `Evaluate(` census (product + tests `*.cs`)

Workspace grep of `Evaluate(` on 2026-08-18 this pass:

| File | Hits | Kind |
|---|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` L76 | 1 | **definition** |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` L159 | 1 | **product caller** |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 6 | unit smoke only (`_e.Evaluate`) |

**Product callers: 1** (was 0 in slot 59). No API, FIX session, or fix-worker consults Evaluate. The mt5-worker / ingest scoring path still does **not**.

### 3.2 DI — `D:\Prop\src\Infrastructure\DependencyInjection.cs`

Current `AddTraderIntelligence`:

| Registration | Notes |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | **Env-bound:** `REAL_COPY_EXECUTION_ENABLED` equals `"true"` (ordinal ignore-case). **No longer hardcoded false.** |
| `AddScoped<CopyTradingService>()` | L44 |
| `AddSingleton<RiskEngine>()` | L45 — **unused** by `CopyTradingService` |
| `AddHostedService<CopyTradingHostedService>()` | L59 |
| Ingest + FIX logon hosted | still present |

`CopyTradingService` constructs **`private readonly RiskEngine _risk = new();`** (L23). The DI singleton is a second unused instance. `IRiskEngine` still does not exist.

`CTraderFixLogonHostedService` **no longer** assigns `_runtime.RealCopyEnabled = false` after logon (re-read L60–70). It logs `RealCopyArmed={Armed}` and “NewOrderSingle still unimplemented.” Flag pin from slot 59 is **gone**.

Local `D:\Prop\.env` contains the key `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no other `.env` values are quoted here). `EnvFile.FindAndLoad()` (`src/Mt5/Env/EnvFile.cs`) loads that file into process environment; API `Program.cs` calls it before `AddTraderIntelligence`. **If the process starts with that file, `RealCopyEnabled` can be true.** That does **not** create a sender.

`appsettings.json` `FeatureFlags:LiveCopyEnabled` remains **false** (different name, unused by DI). `GET /api/settings` still hardcodes `FEATURE_COPY_TRADING_ENABLED=false` and echoes `runtime.RealCopyEnabled` for `REAL_COPY_EXECUTION_ENABLED`.

### 3.3 Writer B — `CopyTradingService.GenerateShadowIntentsAsync` (the new hop)

`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (257 lines).

Welded constants (L15–17):

```text
VenueReconciled            = false
NewOrderSingleImplemented  = false
AllocationFactor           = 0.05m
```

Flow:

```text
TraderScores where state ∈ {SHADOW, LIVE_CANDIDATE, LIVE}
  → completed XAU reconstructed trades
  → skip if IdempotencyKey copy:{broker}:{login}:{positionId} exists
  → QuantityNormalizer(sourceLots, 0.05, gold 0.01/5/0.01)
  → skip if qty ≤ 0
  → new CopyIntent Status="PENDING_RISK"
  → _risk.Evaluate(...)   Reconciled := false (const)
  → new RiskDecisionRecord { AllowFixSend = false }   ← discards decision.AllowFixSend
  → intent.RiskDecisionId = rec.Id
  → if AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled
        Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED"   ← DEAD (two const false)
    else
        Status = "SHADOW_ONLY"
        if quote != null && Outcome == Approve → ShadowOrder
```

Because `VenueReconciled` is const **false** and action is `OpenExposure` (increasing), Evaluate returns **`Reject` / `VENUE_NOT_RECONCILED`**. Then:

- persisted `AllowFixSend` is **false** (hardcoded, L192)
- live branch is compile-time dead
- `Outcome != Approve` ⇒ **no** shadow fill from this writer
- **no** `ExecutionIntent` row in either branch

Even if someone flipped both constants **and** armed REAL_COPY **and** had a LIVE trader **and** Evaluate returned `AllowFixSend=true`, the live branch **only writes a status string**. It still does not construct `ExecutionIntent` and still cannot emit `35=D`.

`GetStatusAsync` reports `FeatureCopyEnabled: true` (L44) while `/api/settings` reports `FEATURE_COPY_TRADING_ENABLED=false`. Paint contradiction; not a send gate.

`ListIntentsAsync` joins latest `RiskDecisionRecord` for the dashboard. Read path only.

Hosted loop (`CopyTradingHostedService.cs` L21–38): delay 8s, then every 20s `GenerateShadowIntentsAsync`. Log: “SHADOW intents. Live NewOrderSingle still blocked.”

API (read-only):

- `GET /api/copy/status` → `GetStatusAsync`
- `GET /api/copy/intents` → `ListIntentsAsync(200)`

Web: `LiveCopyPage.tsx` polls those two endpoints. Blockers copy includes “No NewOrderSingle sender — SAFE_BY_ABSENCE”.

**Tests:** grep of `CopyTradingService` / `GenerateShadowIntents` under `D:\Prop\tests`: **0**.

### 3.4 Writer A — `EfTradingStore.PersistDemoShadowAsync` (still a bypass)

Called after score persist from `ReconstructionScoringService.RebuildTraderAsync` (`DealIngestionService.cs` L118–144):

```text
LoadDeals → Reconstruct → ReplaceReconstructed
  → Score → UpsertScoreAsync
  → PersistDemoShadowAsync(suggestedState, completedXau)
```

Writer behavior (`EfTradingStore.cs` L251–337) **unchanged** vs slot 59:

| Gate | Effect |
|---|---|
| Always | `OutboxEvent` `ScoreUpdate` (not `ShadowCopyIntent`, not `RiskCheckRequest`) |
| `state != TraderState.SHADOW` | return — **0** intents |
| no `destination_quotes` row | return — **0** intents |
| SHADOW + quote | `new CopyIntent` `Status = "SHADOW_ONLY"`, `Action = OpenExposure`, qty = `MaxVolumeLots` 1:1, key `shadow:…` |
| then | `new ShadowCopyEngine().SimulateEntry` → `ShadowOrder` **same SaveChanges** |

**Not called:** `RiskEngine.Evaluate`, `CopyIntentExpiry.IsExpired`.  
**Not written:** `RiskDecisionRecord`, `ExecutionIntent`, FIX `35=D`.

This path still runs from `LiveIngestHostedService` (deals-only logins) and `POST /api/ops/resync` (all catalog logins).

### 3.5 Intended vs actual

```text
INTENDED (§32 / §75 / A23)
  source event → CopyIntent persist → RiskEngine.Evaluate
    → RiskDecision persist → ExecutionIntent persist
    → FIX worker → 35=D (only if AllowFixSend ∧ flag ∧ recon)

ACTUAL (2026-08-18 product, slot 119 re-read)
  Manager catalog/deals → reconstruct → BaselineScorer
    → PersistDemoShadowAsync
         ├─ not SHADOW / no quote → outbox only
         └─ SHADOW + quote → CopyIntent Status=SHADOW_ONLY + ShadowOrder
              ✗ Evaluate never invoked
    AND (parallel hosted tick)
    → CopyTradingService.GenerateShadowIntentsAsync
         → CopyIntent PENDING_RISK
         → Evaluate (Reconciled:=false ⇒ VENUE_NOT_RECONCILED)
         → risk_decisions AllowFixSend:=false
         → Status SHADOW_ONLY (no fill; not Approve)
         ✗ execution_intents never written
         ✗ 35=D does not exist
```

**Slot answer restated:** RiskEngine sits **after** some CopyIntents. It does **not** sit **between** CopyIntent and ExecutionIntent, because the third stage has **zero** writers.

---

## 4. Copy to cTrader must not send live orders (no loss)

Capital constraint for the fetch-all-traders goal.

### 4.1 No NewOrderSingle builder

Product `*.cs` grep of literal `35=D`: **0**.

`CTraderFixSession.BuildLogon` (`src/Fix.CTrader/Sessions/CTraderFixSession.cs` L89–109) emits tag **35=A** only. `Fix.CTrader` has **0** hits for `35=D` / `NewOrderSingle` as a send. Sockets in `TryLogonAsync` are `using`/`await using` and disposed after one read — no TRADE keep-alive that could later emit `35=D`.

### 4.2 FIX worker cannot send even if the flag is flipped

`D:\Prop\apps\fix-worker\Worker.cs` L21–46:

- Reads `CTrader:RealCopyExecutionEnabled` default **false**.
- Stamps QUOTE/TRADE `Disconnected` with “No live TRADE socket. NewOrderSingle remains off.”
- If `real==true`, **logs a warning** and still has **no function** that can emit `35=D`.
- Does **not** query `ExecutionIntents`. Does **not** read `AllowFixSend`.

Flipping the flag cannot place an order.

### 4.3 Extra welded blockers on the new copy service

| Site | Value | Effect |
|---|---|---|
| `CopyTradingService.NewOrderSingleImplemented` | **const false** | live branch dead |
| `CopyTradingService.VenueReconciled` | **const false** | Evaluate rejects every OPEN |
| persist `AllowFixSend` | **literal false** (L192) | disk never stores a send license |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** | unused by the hop |
| `FeatureFlags:LiveCopyEnabled` | **false** | unused by DI |
| `GET /api/settings` FEATURE flag | **false** literal | paint |
| `LiveRuntimeStatus` when armed | note says NOS still unimplemented | honesty string |

`GetStatusAsync.BuildBlockers` always includes `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` and `"Venue not reconciled"`.

### 4.4 What *can* touch the venue

TLS **Logon 35=A** on QUOTE `:5211` and TRADE `:5212` if `CTRADER_FIX_PASSWORD` is present. Session proof / future recon, **not** copy. Password values are not written here.

**Risk to capital from the Prop copy path: NONE.** Safety is **absence of a sender** plus two compile-time false constants. `SAFE_BY_ABSENCE` ≠ go-live PASS (A100/A101 still fail).

Honesty: env can now **arm** `RealCopyEnabled` (slot-59 weld removed). Arming a flag without a builder is still not a live order. Do **not** add `35=D` until Evaluate is on **every** persist path, `ExecutionIntent` exists, and A100/A101 measure PASS. Prefer setting `REAL_COPY_EXECUTION_ENABLED=false` in local env so the dashboard does not look armed.

---

## 5. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 5.1 Code path (current product)

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector` instances: `ACHIEVER` (`MT5_*` + `ACHIEVER_PROXY_*`) and `STARWAVEFX` (`MT5_STARWAVEFX_*`, `ProxyEnabled = false`). Dummy/fake connectors are refused: DI throws if either password is missing / `<SECRET>` / `(a/c`.

Catalog (`DealIngestionService.SyncCatalogAsync` L37–51):

1. `GetGroupsAsync` — **all groups the manager can see**.
2. `GetAccountsAsync(null)` — **all logins in every group**.

Native implementation (`NativeMt5BrokerConnector.cs` L150–232):

| Step | API | Fallback |
|---|---|---|
| Groups | `GroupRequestArray("*")` | `GroupTotal` / `GroupNext` |
| Users per group | `UserRequestArray` / `UserGetByGroup` | `UserLogins` + `UserRequestByLogins` |
| Account money | `UserAccountRequestArray` | `UserAccountGetByGroup` |
| Deals | `DealRequestByGroup` per group | per-login |
| Positions | `PositionRequestByGroup("*")` | per-login |

`GetAccountsAsync(null)` unions every group name from `GetGroupsCore()`. There is **no** `Take(200)` on the catalog path. Logins unique per broker `(BrokerId, Login)`.

Achiever HTTP proxy is applied only when `ACHIEVER_PROXY_ENABLED=true` and host is set. Starwave is direct. Passwords / proxy auth are **not** printed.

### 5.2 Scoring / copy is not the census

| Loop | Login / trader set | Risk? | ExecutionIntent? |
|---|---|---|---|
| Hosted `LiveIngestHostedService` | `ListLoginsWithDealsAsync` | no (writer A) | no |
| Manual `POST /api/ops/resync` | `ListLoginsAsync` (all catalog) | no (writer A) | no |
| `CopyTradingHostedService` | scores in SHADOW / LIVE_CANDIDATE / LIVE | Evaluate (writer B) | no |
| Catalog persist | all groups + all users | n/a | n/a |

Fetch-all is the **catalog**. Scoring/shadow/copy ticks are **subsets**. That does **not** hide groups from `mt5_groups` / `mt5_accounts`.

### 5.3 Prior measured census (not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`  
JSON (logins, no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK via HTTP proxy | **8** | **6512** | 1506 |
| STARWAVEFX | OK direct | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (this manager’s visible universe): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

Honesty: those counts are **this manager login’s permission set**, not a claim the broker has no other groups. This slot **did not** reconnect; it confirms the **same fetch APIs are still the live path**.

Dashboard: `GET /api/groups`, `GET /api/traders` read EF catalog filled by that ingest. `/ready` returns group/account counts.

### 5.4 Fetch does not imply copy

Ingest + score + optional `SHADOW_ONLY` rows **cannot** promote to LIVE send. Writer B cannot emit `ExecutionIntent`. Scoring is not allowed to bypass RiskEngine — writer A still does, and neither writer can reach a sender.

---

## 6. YoPips C++ backend (relevant, not the copy hop)

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\`.

Workspace grep of `CopyIntent` / `ExecutionIntent` / `RiskEngine` / `AllowFixSend` / `NewOrderSingle` / `cTrader` / `35=D` under that `src/`: **0**. Those names are a Prop C# design.

What YoPips *does* have:

| Piece | Role | Copy-to-cTrader? |
|---|---|---|
| `migrations/059_risk_decisions_signals_cases.sql` | Challenge `risk_policy_versions` / `risk_decisions` / `risk_decision_rule_results` keyed by `challenge_id` | **No** — prop-firm compliance ledger |
| Admin risk controllers / final-review | Read those challenge rows | **No** |
| `mt5_group_probe` / Prop `mt5-sdk` | Manager `GetAllGroups` JSON | Fetch helper, no orders |
| Trade execution services | Guarded **MT5 `SendTrade`** for owned challenge terminals | **Different product.** Not cTrader FIX copy |

Do **not** route Achiever/Starwave manager copy through YoPips `SendTrade`. Do **not** treat YoPips `risk_decisions` as `Domain.Risk.RiskEngine`. Same table *name*, different product, different schema (`challenge_id` / `risk_policy_version_id` vs `CopyIntentId`).

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

No test constructs `CopyIntent` → `Evaluate` → `ExecutionIntent`. No test that a worker refuses `35=D` after reject. No `CopyTradingService` tests. A89 named suites (`RiskEngineHardLimitTests`, `RiskRejectionBeforeFixSendTests`) are **not on disk**. Product `AllowFixSend=true` facts: **0**.

`CopyIntentExpiry.IsExpired` is still **tests only** (`ExecutionAndSizingTests.cs`). Neither writer calls it. Writer B sets `ExpiresAt = now.AddSeconds(15)` but Evaluate uses `SourceEventTime = trade.OpenedAt` (historical), so completed-trade backfill is **always** `SIGNAL_STALE` *if it ever got past* `VENUE_NOT_RECONCILED`. Today it never does.

---

## 8. Honesty / gaps (do not greenwash)

1. **“RiskEngine sits between CopyIntent and ExecutionIntent” is FALSE.** Evaluate now runs after one CopyIntent writer. ExecutionIntent is still never persisted.
2. **Slots 19/59 “0 Evaluate callers / RiskEngine not in DI” are stale.** Do not copy those sentences forward.
3. **`AllowFixSend` is not a control.** Writer B **forces false** on persist. Zero sockets read the bit.
4. **`SAFE_BY_ABSENCE` is the no-loss mechanism today.** Correct operating mode for “fetch all traders, do not lose money.” **Not** §68/§70 PASS.
5. **REAL_COPY weld is weaker than slot 59.** DI reads env; logon host no longer pins false; local `.env` sets the flag `true`. Still no sender. Prefer false in env so UI does not look armed.
6. **Two CopyIntent writers, two idempotency namespaces.** Demo bypass + copy tick can double-row the same trade.
7. **`FeatureCopyEnabled: true` vs settings FEATURE false** — dashboard disagreement.
8. **SettingsController `RiskEngine` JSON is a decoy** (unbound Redis keys; controller not mapped). `appsettings.json` `"RiskEngine"` is that decoy, not `Domain.Risk.RiskEngine`.
9. **Orphan `Entities.RiskDecision`** vs mapped `RiskDecisionRecord`.
10. **This slot did not re-measure 8460.** Cite `LIVE_MANAGER_FETCH_MEASURED.md`. Code still requests `*` groups and all users.
11. **Hosted scoring ≠ all 8460.** `ListLoginsWithDealsAsync` is deals-only. Catalog persist is still all-groups/all-users.
12. **Do not enable a live `35=D` builder.** Do not treat writer B as go-live.

---

## 9. What “wired” would look like (not built)

Required before any live copy (still **off**):

1. Persist `CopyIntent` per source **event** (not completed-trade OPEN backfill).
2. **One** writer; delete or fold the `shadow:` bypass so Evaluate cannot be skipped.
3. Inject `IRiskEngine`; persist `RiskDecisionRecord` unique on `CopyIntentId`; **do not overwrite** `AllowFixSend`.
4. Persist `ExecutionIntent` + unique `ClOrdId` **only** on APPROVE/REDUCE_SIZE **and** `AllowFixSend`.
5. Set `CopyIntent.RiskDecisionId` / `ExecutionIntentId`.
6. FIX worker consumes persisted intents; consults Evaluate outcome + flag + recon + ownership fence.
7. Quantity: never passthrough MT5 lots as cTrader `OrderQty` (writer A still does 1:1).
8. Keep default flag **false** until measured gates pass. Restore a pin if env is true in the lab file.
9. Add pipeline tests: reject ⇒ 0 ExecutionIntent; approve+flag+recon ⇒ persist-before-send; never 35=D from MT5 callback.

Until then: fetch-all-traders is the mission; live cTrader send stays impossible.

---

## 10. Evidence index (absolute paths)

| Path | Why it matters |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §4 / §32 / §39 / §75 law |
| `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` | Binding hop spec (not fully implemented) |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | Evaluate definition |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | FK slots; ExecutionIntentId unused |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | No writer |
| `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs` | Mapped table; one writer |
| `D:\Prop\src\Domain\Entities\RiskDecision.cs` | Orphan entity; not mapped |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Product Evaluate caller; 0 ExecutionIntent |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s shadow tick |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Bypass CopyIntent writer |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog-all + score + writer A |
| `D:\Prop\src\Application\Copy\CopyTradingModels.cs` | Status/intent DTOs |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Env-bound REAL_COPY; RiskEngine singleton unused by hop |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Fetch/score only; deals-only scoring set |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | ALL groups + ALL users |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever + Starwave pair |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | No longer pins RealCopyEnabled false |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send; no builder |
| `D:\Prop\apps\api\Program.cs` | `/api/copy/*` read-only; resync = catalog/score |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | Paints blockers; not a sender |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Unmapped decoy RiskEngine DTO |
| `D:\Prop\apps\api\appsettings.json` | FeatureFlags.LiveCopyEnabled=false |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 5 smoke facts; not a pipeline test |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups, 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_59.md` | Prior same-question recensus (**stale** on 0 callers) |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_19.md` | Earlier same-question recensus (**stale** on 0 callers) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\migrations\059_risk_decisions_signals_cases.sql` | Different product’s risk tables |

---

*End of W500_RESEARCH_119. Product source was not modified.*
