# W500_RESEARCH_99 — Does `RiskEngine` sit between `CopyIntent` and `ExecutionIntent`?

| Field | Value |
|---|---|
| Slot | **99** |
| Agent | W500_RESEARCH_99 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_99.md` |
| Date | 2026-08-18 |
| Topic | Check `RiskEngine` sits between `CopyIntent` and `ExecutionIntent` |
| Goal context | Fetch **ALL** Achiever + StarwaveFX groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (no MT5 / FIX / proxy passwords, no tag 554, no `.env` values) |
| Live attach this slot | **No.** No Manager Connect, no FIX TLS, no order send. |
| Method | Independent recensus. Full `read_file` of `RiskEngine.cs` (closes L189), `CopyIntent.cs`, `ExecutionIntent.cs`, `RiskDecision*.cs`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService` / `ReconstructionScoringService`, `DependencyInjection.cs`, `LiveIngestHostedService.cs`, `LiveMt5Registration.cs`, `NativeMt5BrokerConnector` group/user walk, `CTraderFixSession.cs` (135 lines), `CTraderFixLogonHostedService.cs`, `apps/fix-worker/Worker.cs`, `TraderDbContext.cs`, `LiveRuntimeStatus.cs`, `Program.cs` `/api/settings` + `/api/ops/resync`, `SettingsController`, `RiskEngineTests`, architecture §§4/32/39/75, A23. `grep` of `Evaluate(`, `IRiskEngine`, `new CopyIntent`, `new ExecutionIntent`, `ExecutionIntents.Add`, `35=D`, `NewOrderSingle` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`. Contrast read of YoPips `migrations/059_risk_decisions_signals_cases.sql`. **No TLS. No Manager re-probe. No product edit.** |
| Binding law | Architecture §4 / §32 / §39 / §75; A23; E005 R001; E002 / E034 |
| Same-question siblings (do not inherit) | `W500_RESEARCH_19.md`, `W500_RESEARCH_39.md`, `W500_RESEARCH_59.md`, `W500_RESEARCH_79.md` — **this file re-reads the tree** |
| SHA this slot | Not re-hashed (no shell). Prior E005 pin `RiskEngine.cs` SHA-256 `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D`. Body re-read this pass matches that Evaluate / `AllowFixSend` conjunction. File closes at L189 (optional empty L190). |

**Honesty rule:** a Domain `Evaluate` method is **not** a seated gate. A `DbSet<ExecutionIntent>` is **not** persist-before-send. `AllowFixSend` is a DTO bit, **not** a socket. Architecture diagrams are **law**, not proof the process implements them. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not “risk-gated live copy.” Do **not** tick §68 / §70 from this slot. Do **not** print secrets.

---

## 0. Verdict (binding)

**Architecture: YES. Runtime product: NO_HOP. Live capital: NONE (`SAFE_BY_ABSENCE`).**

Slot answer: `RiskEngine` does **not** sit between `CopyIntent` and `ExecutionIntent` in running code. The hop exists as architecture law (§4 / §32 / §39 / §75) and as unused Domain types. There is **no** Application orchestrator that persists `CopyIntent` → calls `Evaluate` → persists `risk_decisions` → persists `ExecutionIntent`. The only `CopyIntent` writer skips risk and never writes `ExecutionIntent`. There is **no** FIX `35=D` builder.

| Question | Measured answer | Class |
|---|---|---|
| Must RiskEngine sit between CopyIntent and ExecutionIntent? | **Yes** — architecture + A23 | SPEC |
| Do the three types exist? | **Yes** — entities + `RiskEngine` class (closes L189) | EXISTS |
| Is `Evaluate` on any production path? | **No** — 0 product callers; 6 test calls / 5 facts | DEAD |
| Is `RiskEngine` / `IRiskEngine` registered in DI? | **No.** `IRiskEngine` type **does not exist** | MISSING |
| Does any writer persist `RiskDecisionRecord` after Evaluate? | **No** (`RiskDecisions.Add` = **0**) | MISSING |
| Does any writer persist `ExecutionIntent`? | **No** (`new ExecutionIntent` / `ExecutionIntents.Add` = **0**) | MISSING |
| Does the only CopyIntent writer consult RiskEngine? | **No** — `PersistDemoShadowAsync` → `SHADOW_ONLY` + `ShadowOrder` | BYPASS |
| Can copy send a live cTrader order now? | **No** — no `35=D` encoder; flag forced false | SAFE_BY_ABSENCE |
| Can we fetch ALL Achiever + Starwave groups + manager logins? | **Code path yes.** Prior measured census **18 / 8460**. This slot did not re-run the probe. | FETCH_OK (prior) |

Do **not** claim “risk sits between intents.” A DTO bit named `AllowFixSend` is not a socket and is not on the ingest path. Do **not** claim live copy is gated by RiskEngine. Live copy is impossible because the sender does not exist.

One-line:

```text
SPEC: CopyIntent → RiskEngine → ExecutionIntent.
CODE: score → SHADOW_ONLY CopyIntent + ShadowOrder (no Evaluate, no ExecutionIntent, no 35=D).
CAPITAL: SAFE_BY_ABSENCE. Fetch-all catalog is separate from copy send.
```

**Slot-99 vs 19 / 39 / 59 / 79:** same hop still missing. No product drift that wires `Evaluate`, registers `IRiskEngine`, writes `ExecutionIntent`, or adds `35=D`. Extra precision this pass: hosted scoring still uses `ListLoginsWithDealsAsync`; manual `/api/ops/resync` still uses `ListLoginsAsync` (all catalog logins). Neither path calls risk. `CanPromoteToLive => false` (`BaselineScorer.cs` L211). Application tree still has no `Risk/` / `Copy/` / `Execution/` ports.

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

`RiskDecisionOutcome` on disk (`D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs`) matches that six-way enum. Matching names ≠ a wired hop.

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

**PASS condition for this slot:** a worker must persist `CopyIntent` → call `RiskEngine.Evaluate` → persist `risk_decisions` → persist `ExecutionIntent` (only on APPROVE / REDUCE_SIZE) → FIX worker may send **only if** `AllowFixSend && REAL_COPY && READY_FOR_EXECUTION`. That conjunction is **not** implemented.

---

## 2. What exists on disk (types, not a pipeline)

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

Empty no-op at L90–93: when `RealExecutionEnabled == false` and action is not `CloseExposure`, the method **comments** “Shadow path still evaluates risk but never allows FIX send” and then **falls through**. APPROVE + `AllowFixSend=false` is possible. That is not a send gate because nobody sends.

Reducing actions (`ReduceExposure` / `CloseExposure`) fall through to `Reason = "RISK_REDUCTION"` with `ApprovedQuantity = RequestedQuantity` and the same send bit. That is **not** an emergency flatten executor.

Name collision (do not confuse):

| Type | Namespace | Mapped? |
|---|---|---|
| `RiskDecision` record | `Domain.Risk` | Evaluate return only |
| `RiskDecision` class | `Domain.Entities` (`RiskDecision.cs`) | **Orphan** — not in `TraderDbContext` |
| `RiskDecisionRecord` | `Domain.Entities` | EF `risk_decisions` (`DbSet<RiskDecisionRecord> RiskDecisions`) |

### 2.2 Intent entities (FK slots imply the hop; nothing fills them)

`CopyIntent` (`D:\Prop\src\Domain\Entities\CopyIntent.cs`, 24 lines):

```text
RiskDecisionId     Guid?     // never set by writer
ExecutionIntentId  Guid?     // never set
Status             default "Pending"; writer uses "SHADOW_ONLY"
ExpiresAt          required field; writer sets OpenedAt+15s (already stale for history)
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
| `copy_intents` | `IdempotencyKey` | **one** — `PersistDemoShadowAsync` |
| `risk_decisions` | index on `CopyIntentId` (**not unique**) | **none** |
| `execution_intents` | `ClOrdId` unique (nullable) | **none** |

### 2.3 Adjacent helpers also off-path

| Helper | Path | Used by product? |
|---|---|---|
| `CopyIntentExpiry.IsExpired` | `src/Domain/Execution/CopyIntentExpiry.cs` | **Tests only** (`ExecutionAndSizingTests`) |
| `FixSessionOwnership.ExecutionIntentsAllowed` | `src/Fix.CTrader/Services/FixSessionOwnership.cs` L111 | **Unused** by worker |
| `ShadowCopyEngine.SimulateEntry` | `src/Domain/Shadow/ShadowCopyEngine.cs` | Ad-hoc `new` inside store, not DI |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (L35) | Not bound onto `RiskEngine` |
| `OutboxEventType.RiskCheckRequest` | enum value 3 | **Never enqueued** (writer uses `ScoreUpdate`) |
| `BaselineScorer.CanPromoteToLive` | L211 `=> false` | Hard pin; scoring cannot emit LIVE |

Application tree (`D:\Prop\src\Application\`): `Contracts/`, `Dashboard/`, `Ingestion/`, `Runtime/` only. **No** `Risk/`, **no** `Copy/`, **no** `Execution/` ports.

Grep of `IRiskEngine` under product `*.cs`: **0**.

---

## 3. Measured call graph (this is the slot)

### 3.1 `Evaluate(` census (product + tests `*.cs`)

Workspace grep of `Evaluate(` on 2026-08-18 this pass:

| File | Hits | Kind |
|---|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` L76 | 1 | **definition** |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 6 | unit smoke only (`_e.Evaluate`) — 5 `[Fact]`s, one fact calls twice |

**Product callers: 0.** No Application, store, API, fix-worker, mt5-worker, or FIX session consults Evaluate.

### 3.2 DI — `D:\Prop\src\Infrastructure\DependencyInjection.cs`

`AddTraderIntelligence` registers: DbContext, `LiveRuntimeStatus` with **`RealCopyEnabled = false` hardcoded** (L38–42), native connectors, store, dashboard, reconstructor, scorer, ingest, `LiveIngestHostedService`, `CTraderFixLogonHostedService`.

**Not registered:** `RiskEngine`, `IRiskEngine` (type does not exist), `ShadowCopyEngine`, `CopyIntentExpiry`, any execution-intent service.

Comment at L40: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.”

### 3.3 Only `CopyIntent` writer — `EfTradingStore.PersistDemoShadowAsync`

Called **after** score persist from `ReconstructionScoringService.RebuildTraderAsync` (`DealIngestionService.cs` L118–144):

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
| SHADOW + quote | `new CopyIntent` with `Status = "SHADOW_ONLY"`, `Action = OpenExposure`, qty = `MaxVolumeLots` (source lots, **not** dest OrderQty), `ExpiresAt = trade.OpenedAt.AddSeconds(15)` |
| then | `new ShadowCopyEngine().SimulateEntry` → `ShadowOrder` **same SaveChanges** |

**Not set:** `Direction`, `SourcePositionId`, `SourceTradeId`, `RiskDecisionId`, `ExecutionIntentId`.

**Not called:** `RiskEngine.Evaluate`, `CopyIntentExpiry.IsExpired`.

**Not written:** `RiskDecisionRecord`, `ExecutionIntent`, FIX `35=D`.

Grep of `new ExecutionIntent` / `ExecutionIntents.Add` / `RiskDecisions.Add` / `new RiskDecisionRecord` in product `src/` + `apps/` `*.cs`: **0**.  
`new RiskEngine` exists only as `private readonly RiskEngine _e = new();` in the test class.

### 3.4 Ingest host and resync never reach execution

`LiveIngestHostedService` (`src/Infrastructure/Hosting/LiveIngestHostedService.cs`):

```text
connect → SyncCatalogAsync → SyncBrokerAsync (deals/positions)
       → RebuildTraderAsync per ListLoginsWithDealsAsync
```

Stops at score + optional shadow rows. **Does not** walk `ExecutionIntents`. **Does not** call Evaluate.

`POST /api/ops/resync` (`apps/api/Program.cs` L111–147): same catalog/deals/score loop for `ACHIEVER` then `STARWAVEFX`, but scores **`ListLoginsAsync`** (every catalog login), not only logins-with-deals. Still **no** risk. **No** execution intent.

That hosted-vs-resync scoring-set difference is **not** a RiskEngine hop. It is a completeness difference for shadow backfill only.

### 3.5 Dashboard “risk” is not the engine

`EfDashboardQueries.GetRiskAsync` (L198–208): latest `KillSwitch` + last 20 `RiskDecisionRecord.Reason` where outcome ≠ Approve. Returns `RealCopyEnabled: false` as a **literal** (`new RiskDashboardDto(..., false, rejects)`). Exposure/PnL fields are **0**. Empty table → empty rejects. Paint endpoint, not a gate.

`GET /api/settings` (`Program.cs` L70–83) hardcodes `FEATURE_COPY_TRADING_ENABLED=false` and echoes `runtime.RealCopyEnabled` (forced false). Risk-limit numbers `maxQuoteAgeSeconds=3` / `maxSignalAgeSeconds=15` are **literals**, not `RiskLimits` binding.

`SettingsController` (`apps/api/Controllers/SettingsController.cs`) exposes a **different** Redis DTO also named `RiskEngine` (`MaxDailyDrawdownPct` / `MaxPositionSize`). `Program.cs` does **not** `AddControllers` / `MapControllers` (grep = **0**). Dead leftover. `appsettings.json` `"RiskEngine"` block is that decoy, **not** `Domain.Risk.RiskEngine`.

### 3.6 Intended vs actual

```text
INTENDED (§32 / §75 / A23)
  source event → CopyIntent persist → RiskEngine.Evaluate
    → RiskDecision persist → ExecutionIntent persist
    → FIX worker → 35=D (only if AllowFixSend ∧ flag ∧ recon)

ACTUAL (2026-08-18 product, slot 99 re-read)
  Manager catalog/deals → reconstruct → BaselineScorer
    → PersistDemoShadowAsync
         ├─ not SHADOW / no quote → outbox only
         └─ SHADOW + quote → CopyIntent Status=SHADOW_ONLY
                              + ShadowOrder (in-process fill)
    ✗ RiskEngine.Evaluate never invoked
    ✗ risk_decisions never written
    ✗ execution_intents never written
    ✗ 35=D does not exist
```

**Slot answer:** RiskEngine does **not** sit between CopyIntent and ExecutionIntent. The middle hop is missing. The third stage has no writer.

---

## 4. Copy to cTrader must not send live orders (no loss)

Capital constraint for the fetch-all-traders goal.

### 4.1 No NewOrderSingle builder

`CTraderFixSession.BuildLogon` (`src/Fix.CTrader/Sessions/CTraderFixSession.cs` L89–109) emits tag **35=A** only. Grep of `Fix.CTrader` product `*.cs` for `35=D` / `(35, "D")` / `NewOrderSingle` as a builder: **0**. Only outbound MsgType is `(35, "A")`. One `WriteAsync` (logon). Sockets are `using`/`await using` and disposed after one read.

`CTraderFixLogonHostedService` L68–71 forces `_runtime.RealCopyEnabled = false` after optional TLS logon and logs `NewOrderSingle still disabled`. No TRADE keep-alive that could later emit `35=D`.

### 4.2 FIX worker cannot send even if the flag is flipped

`D:\Prop\apps\fix-worker\Worker.cs` L21–46:

- Reads `CTrader:RealCopyExecutionEnabled` default **false**.
- Stamps QUOTE/TRADE `Disconnected` with “No live TRADE socket. NewOrderSingle remains off.”
- If `real==true`, **logs a warning** and still has **no function** that can emit `35=D`.
- Does **not** query `ExecutionIntents`. Does **not** read `AllowFixSend`.

Flipping the flag cannot place an order.

### 4.3 Runtime flag is welded off

| Site | Value |
|---|---|
| `AddTraderIntelligence` | `RealCopyEnabled = false` (comment: do not arm) |
| `CTraderFixLogonHostedService` | reset to `false` after logon |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` |
| `GET /api/settings` | `FEATURE_COPY_TRADING_ENABLED=false` |
| `LiveRuntimeStatus.Snapshot` when false | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |
| Risk dashboard DTO | `RealCopyEnabled` literal `false` |
| `BaselineScorer.CanPromoteToLive` | **always false** |

### 4.4 What *can* touch the venue

TLS **Logon 35=A** on QUOTE `:5211` and TRADE `:5212` if `CTRADER_FIX_PASSWORD` is present. Session proof / future recon, **not** copy. Password values are not written here.

**Risk to capital from the Prop copy path: NONE.** Safety is **absence of a sender**, not a proven RiskEngine refuse-on-LoggedOn-TRADE test. `SAFE_BY_ABSENCE` ≠ go-live PASS (A100/A101 still fail).

---

## 5. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 5.1 Code path (current product)

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector` instances: `ACHIEVER` (`MT5_*` + `ACHIEVER_PROXY_*`) and `STARWAVEFX` (`MT5_STARWAVEFX_*`, `ProxyEnabled = false`). Dummy/fake connectors are refused: DI throws if either password is missing / `<SECRET>` / `(a/c`.

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
| Hosted `LiveIngestHostedService` | `ListLoginsWithDealsAsync` | no | no |
| Manual `POST /api/ops/resync` | `ListLoginsAsync` (all catalog) | no | no |
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

Dashboard: `GET /api/groups`, `GET /api/traders` read EF catalog filled by that ingest. `/ready` returns group/account counts.

### 5.4 Fetch does not imply copy

Ingest + score + optional `SHADOW_ONLY` rows **cannot** promote to LIVE send. Scoring is not allowed to bypass RiskEngine — and today it also cannot reach a sender (`CanPromoteToLive => false`).

---

## 6. YoPips C++ backend (relevant, not the copy hop)

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\`.

Workspace / targeted read of `CopyIntent` / `ExecutionIntent` / `RiskEngine` / `AllowFixSend` / `NewOrderSingle` under that `src/`: **0**. Those names are a Prop C# design.

What YoPips *does* have:

| Piece | Role | Copy-to-cTrader? |
|---|---|---|
| `migrations/059_risk_decisions_signals_cases.sql` | Challenge `risk_policy_versions` / `risk_decisions` / `risk_decision_rule_results` | **No** — prop-firm compliance ledger (`challenge_id` / `risk_policy_version_id`) |
| `admin_v2_risk_controller.cpp` (prior) | Admin read of those challenge rows | **No** |
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

No test constructs `CopyIntent` → `Evaluate` → `ExecutionIntent`. No test that a worker refuses `35=D` after reject. A89 named suites (`RiskEngineHardLimitTests`, `RiskRejectionBeforeFixSendTests`) are **not on disk**. `AllowFixSend=true` facts: **0**.

---

## 8. Honesty / gaps (do not greenwash)

1. **“RiskEngine sits between intents” is FALSE in product.** True only as architecture text and unused types.
2. **`AllowFixSend` is not a control.** Zero sockets read it.
3. **`SAFE_BY_ABSENCE` is the no-loss mechanism today.** Correct operating mode for “fetch all traders, do not lose money.” **Not** §68/§70 PASS.
4. **Shadow page copy is slightly wrong.** `ShadowPortfolioPage.tsx` L10 says “after a CopyIntent is **approved**.” The writer never calls Evaluate; status is the literal `SHADOW_ONLY`.
5. **SettingsController `RiskEngine` JSON is a decoy** (unbound Redis keys; controller not mapped). `appsettings.json` `"RiskEngine"` is that decoy.
6. **Orphan `Entities.RiskDecision`** vs mapped `RiskDecisionRecord` — two types, one table, zero writers.
7. **This slot did not re-measure 8460.** Cite `LIVE_MANAGER_FETCH_MEASURED.md`. Code still requests `*` groups and all users.
8. **Hosted scoring ≠ all 8460.** `ListLoginsWithDealsAsync` is deals-only. Catalog persist is still all-groups/all-users.
9. **Do not enable `REAL_COPY_EXECUTION_ENABLED`.** Do not add `35=D` until Evaluate is on the persist path and A100/A101 measure PASS.
10. **Slots 19 / 39 / 59 / 79 are not stale on the hop.** This recensus agrees. Do not treat later wave numbers as “risk is now wired.”

---

## 9. What “wired” would look like (not built)

Required before any live copy (still **off**):

1. Persist `CopyIntent` per source **event** (not completed-trade OPEN backfill).
2. Register `RiskEngine`; persist `RiskDecisionRecord` unique on `CopyIntentId`.
3. Persist `ExecutionIntent` + unique `ClOrdId` **only** on APPROVE/REDUCE_SIZE **and** `AllowFixSend`.
4. Set `CopyIntent.RiskDecisionId` / `ExecutionIntentId`.
5. FIX worker consumes persisted intents; consults Evaluate outcome + flag + recon + ownership fence.
6. Quantity: never passthrough MT5 lots as cTrader `OrderQty`.
7. Keep default flag **false** until measured gates pass.

Until then: fetch-all-traders is the mission; live cTrader send stays impossible.

---

## 10. Evidence index (absolute paths)

| Path | Why it matters |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §4 / §32 / §39 / §75 law |
| `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` | Binding hop spec (not implemented) |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | Evaluate definition; only product Evaluate |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | FK slots unused |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | No writer |
| `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs` | Mapped table; no writer |
| `D:\Prop\src\Domain\Entities\RiskDecision.cs` | Orphan entity; not mapped |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Only CopyIntent writer; no risk |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog-all + score + shadow |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | RiskEngine not registered; flag false |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Fetch/score only; deals-only scoring set |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | ALL groups + ALL users |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever + Starwave pair |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Forces RealCopyEnabled=false |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send; no builder |
| `D:\Prop\apps\api\Program.cs` | Resync = catalog/score; copy flags false |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Unmapped decoy RiskEngine DTO |
| `D:\Prop\apps\api\appsettings.json` | FeatureFlags.LiveCopyEnabled=false |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false` |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 5 smoke facts; not a pipeline test |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups, 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_19.md` | Prior same-question recensus (agrees) |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_39.md` | Prior same-question recensus (agrees) |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_59.md` | Prior same-question recensus (agrees) |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_79.md` | Prior same-question recensus (agrees) |
| `D:\Prop\reports\swarm\20260818\E005_rules_matrix.md` | 0 product Evaluate callers |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\migrations\059_risk_decisions_signals_cases.sql` | Different product’s risk tables |

---

*End of W500_RESEARCH_99. Product source was not modified.*
