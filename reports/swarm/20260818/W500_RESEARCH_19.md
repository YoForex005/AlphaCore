# W500_RESEARCH_19 — Does `RiskEngine` sit between `CopyIntent` and `ExecutionIntent`?

| Field | Value |
|---|---|
| Slot | **19** |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_19.md` |
| Date | 2026-08-18 |
| Topic | Check `RiskEngine` sits between `CopyIntent` and `ExecutionIntent` |
| Goal context | Fetch **ALL** Achiever + StarwaveFX groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** |
| Method | Read-only `read_file` / `grep` on `D:\Prop` product C#, architecture, prior measured census, and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. No live Manager re-probe this pass. Prefer false negatives over fake PASS. |

---

## 0. Verdict (binding)

**Architecture: YES. Runtime product: NO. Live capital: SAFE_BY_ABSENCE (no loss from this copy path).**

`RiskEngine` is specified as the only legal authority **between** a persisted `CopyIntent` and a persisted `ExecutionIntent` / `ApprovedExecutionIntent` (architecture §4, §32, §39, §75). The domain helper `TraderIntelligence.Domain.Risk.RiskEngine.Evaluate` exists. **Nothing in Application / Infrastructure / API / workers calls it.** The only `CopyIntent` writer skips risk and never writes `ExecutionIntent`. There is **no** `35=D` NewOrderSingle builder.

| Question | Measured answer | Class |
|---|---|---|
| Must RiskEngine sit between CopyIntent and ExecutionIntent? | **Yes** — architecture law | SPEC |
| Do the three types exist? | **Yes** — entities + `RiskEngine` class | EXISTS |
| Is Evaluate on the production path? | **No** — 0 product callers | DEAD |
| Is RiskEngine registered in DI? | **No** — `AddTraderIntelligence` does not add it | MISSING |
| Does any writer persist `RiskDecisionRecord` after Evaluate? | **No** | MISSING |
| Does any writer persist `ExecutionIntent`? | **No** (`new ExecutionIntent` / `ExecutionIntents.Add` = **0**) | MISSING |
| Does the only CopyIntent writer consult RiskEngine? | **No** — `PersistDemoShadowAsync` → `SHADOW_ONLY` + `ShadowOrder` | BYPASS |
| Can copy send a live cTrader order now? | **No** — no `35=D` encoder; flag forced false | SAFE_BY_ABSENCE |
| Can we fetch ALL Achiever + Starwave groups + manager logins? | **Code path yes.** Prior measured census **18 / 8460**. This pass did not re-run the probe. | FETCH_OK (prior) |

**Do not claim** “risk sits between intents.” A DTO bit named `AllowFixSend` is not a socket and is not on the ingest path. **Do not claim** live copy is gated by RiskEngine. Live copy is impossible because the sender does not exist.

One-line:

```text
SPEC: CopyIntent → RiskEngine → ExecutionIntent.
CODE: score → SHADOW_ONLY CopyIntent + ShadowOrder (no Evaluate, no ExecutionIntent, no 35=D).
```

---

## 1. Architecture law (what “sits between” means)

Source of truth: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.

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

### 1.3 §39 (lines 1496–1507)

“The risk engine is the final authority.” Scoring/ML may emit only `candidate` / `confidence` / `suggested allocation`. Risk decides `approve` / `reduce size` / `reject` / `pause trader` / `pause venue` / `global stop`.

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

Binding spec sibling: `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` (same hop; spec only).

**PASS condition for this slot:** a worker must persist `CopyIntent` → call `RiskEngine.Evaluate` → persist `risk_decisions` → persist `ExecutionIntent` (only on APPROVE / REDUCE_SIZE) → FIX worker may send **only if** `AllowFixSend && REAL_COPY && READY_FOR_EXECUTION`. That conjunction is **not** implemented.

---

## 2. What exists on disk (types, not a pipeline)

### 2.1 Domain helper — `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189 lines)

| Type | Role |
|---|---|
| `RiskLimits` | Hardcoded lab caps (`MaxQuoteAge=3s`, `MaxSourceSignalAge=15s`, book/PnL/qty caps). Not bound from `appsettings` `RiskEngine:*`. |
| `RiskEvaluationRequest` | Caller-assembled snapshot; includes `CopyIntentId`, action, qty, quote, `RealExecutionEnabled`, `Reconciled`, `KillSwitch`. |
| `TraderIntelligence.Domain.Risk.RiskDecision` | **Record** return: `Outcome`, `ApprovedQuantity`, `Reason`, `AllowFixSend`. **Not** the EF entity. |
| `RiskEngine.Evaluate` | First-match reject sequence; `AllowFixSend` is a **bool** computed at L147–150. |

`AllowFixSend` conjunction (L147–150):

```text
RealExecutionEnabled && KillSwitch == None && Reconciled && VenueHealthy
```

Empty no-op at L90–93: when `RealExecutionEnabled == false` and action is not `CloseExposure`, the method **comments** “Shadow path still evaluates risk but never allows FIX send” and then **falls through**. APPROVE + `AllowFixSend=false` is possible. That is not a send gate because nobody sends.

Name collision (do not confuse):

| Type | Namespace | Mapped? |
|---|---|---|
| `RiskDecision` record | `Domain.Risk` | Evaluate return only |
| `RiskDecision` class | `Domain.Entities` | **Orphan** — not in `TraderDbContext` |
| `RiskDecisionRecord` | `Domain.Entities` | EF `risk_decisions` (`DbSet<RiskDecisionRecord> RiskDecisions`) |

Prior SHA pin (E005, unchanged body vs this read): `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D`.

### 2.2 Intent entities

`CopyIntent` (`D:\Prop\src\Domain\Entities\CopyIntent.cs`) has FK slots that **imply** the hop:

```text
RiskDecisionId     Guid?
ExecutionIntentId  Guid?
Status             default "Pending"
ExpiresAt          required field
```

`ExecutionIntent` (`D:\Prop\src\Domain\Entities\ExecutionIntent.cs`) has:

```text
CopyIntentId, RiskDecisionId, ClOrdId, Status="Pending", SentAt, FilledAt
```

EF (`TraderDbContext` L122–141): `copy_intents` unique `IdempotencyKey`; `risk_decisions` index on `CopyIntentId` (**not unique**); `execution_intents` unique `ClOrdId`.

### 2.3 Adjacent helpers that are also off-path

| Helper | Path | Used by product? |
|---|---|---|
| `CopyIntentExpiry.IsExpired` | `src/Domain/Execution/CopyIntentExpiry.cs` | **Tests only** (`ExecutionAndSizingTests`) |
| `FixSessionOwnership.ExecutionIntentsAllowed` | `src/Fix.CTrader/Services/FixSessionOwnership.cs` L111 | **Unused** by worker |
| `ShadowCopyEngine.SimulateEntry` | `src/Domain/Shadow/ShadowCopyEngine.cs` | Ad-hoc `new` inside store, not DI |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (`CTraderFixOptions.cs` L35) | Not bound onto RiskEngine |

---

## 3. Measured call graph (this is the slot)

### 3.1 `Evaluate(` census (product + tests `*.cs`)

| File | Hits | Kind |
|---|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` L76 | 1 | **definition** |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 6 | unit smoke only |

**Product callers: 0.** Confirmed by workspace grep of `Evaluate(` under `D:\Prop`. No Application, no store, no API, no fix-worker, no mt5-worker.

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
| Always | `OutboxEvent` `ScoreUpdate` (not `ShadowCopyIntent`) |
| `state != TraderState.SHADOW` | return — **0** intents |
| no `destination_quotes` row | return — **0** intents |
| SHADOW + quote | `new CopyIntent` with `Status = "SHADOW_ONLY"`, `Action = OpenExposure`, qty = `MaxVolumeLots` (source lots, **not** dest OrderQty), `ExpiresAt = trade.OpenedAt.AddSeconds(15)` (already stale for historical trades) |
| then | `new ShadowCopyEngine().SimulateEntry` → `ShadowOrder` |

**Not set:** `Direction`, `SourcePositionId`, `SourceTradeId`, `RiskDecisionId`, `ExecutionIntentId`.

**Not called:** `RiskEngine.Evaluate`, `CopyIntentExpiry.IsExpired`.

**Not written:** `RiskDecisionRecord`, `ExecutionIntent`, FIX `35=D`.

Grep: `new ExecutionIntent` / `ExecutionIntents.Add` / `RiskDecisions.Add` / `new RiskDecisionRecord` / `IRiskEngine` = **0** in product `*.cs`.

### 3.4 Ingest host does not reach execution

`LiveIngestHostedService` (`src/Infrastructure/Hosting/LiveIngestHostedService.cs`): connect → `SyncCatalogAsync` → `SyncBrokerAsync` (deals/positions) → `RebuildTraderAsync` per login. Stops at score + optional shadow rows.

`POST /api/ops/resync` (`apps/api/Program.cs` L111–147) is the same catalog/deals/score loop for `ACHIEVER` then `STARWAVEFX`. No risk. No execution intent.

### 3.5 Dashboard “risk” is not the engine

`EfDashboardQueries.GetRiskAsync` (L198–208): reads latest `KillSwitch` + last 20 `RiskDecisionRecord.Reason` where outcome ≠ Approve. Returns `RealCopyEnabled: false` as a **literal**. Exposure/PnL fields are **0**. Empty table → empty rejects. This is a paint endpoint, not a gate.

`GET /api/settings` (Program.cs L70–83) hardcodes `FEATURE_COPY_TRADING_ENABLED=false` and echoes `runtime.RealCopyEnabled` (forced false).

`SettingsController` (`apps/api/Controllers/SettingsController.cs`) exposes a **different** Redis DTO also named `RiskEngine` (drawdown %, max size). **Not** `Domain.Risk.RiskEngine`. `Program.cs` does **not** `AddControllers` / `MapControllers`. Dead leftover.

### 3.6 Intended vs actual

```text
INTENDED (§32 / §75)
  source event → CopyIntent persist → RiskEngine.Evaluate
    → RiskDecision persist → ExecutionIntent persist
    → FIX worker → 35=D (only if AllowFixSend ∧ flag ∧ recon)

ACTUAL (2026-08-18 product)
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

This is the capital constraint for the fetch-all-traders goal.

### 4.1 No NewOrderSingle builder

`CTraderFixSession.BuildLogon` (`src/Fix.CTrader/Sessions/CTraderFixSession.cs` L89–109) emits tag **35=A** only (plus 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554). **0** hits for `35=D` / `(35, "D")` / `OrderQty` / tag 38 in `Fix.CTrader`.

`CTraderFixLogonHostedService` (L68–71) forces `_runtime.RealCopyEnabled = false` after optional TLS logon and logs `NewOrderSingle still disabled`.

Prior pin E034: 83 product `*.cs`, literal `35=D` = **0**, `NewOrderSingle` = name/log/comment only.

### 4.2 FIX worker cannot send even if the flag is flipped

`D:\Prop\apps\fix-worker\Worker.cs` L21–46:

- Reads `CTrader:RealCopyExecutionEnabled` default **false**.
- Stamps QUOTE/TRADE `Disconnected` with “No live TRADE socket. NewOrderSingle remains off.”
- If `real==true`, **logs a warning** and still has **no function** that can emit `35=D`.

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

### 4.4 What *can* touch the venue

TLS **Logon 35=A** on QUOTE `:5211` and TRADE `:5212` if `CTRADER_FIX_PASSWORD` is present. That is session proof / future recon, **not** copy. Password values are not written here.

**Risk to capital from the Prop copy path: NONE.** Safety is **absence of a sender**, not a proven RiskEngine refuse-on-LoggedOn-TRADE test. `SAFE_BY_ABSENCE` ≠ go-live PASS (A100/A101 still fail).

---

## 5. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 5.1 Code path (current product)

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector` instances: `ACHIEVER` (`MT5_*` + `ACHIEVER_PROXY_*`) and `STARWAVEFX` (`MT5_STARWAVEFX_*`, proxy off). Dummy/fake connectors are refused: DI throws if either password is missing / `<SECRET>` / `(a/c`.

Catalog (`DealIngestionService.SyncCatalogAsync` L37–51):

1. `GetGroupsAsync` — **all groups the manager can see**.
2. `GetAccountsAsync(null)` — **all logins in every group**.

Native implementation (`NativeMt5BrokerConnector.cs`):

| Step | API | Fallback |
|---|---|---|
| Groups | `GroupRequestArray("*")` | `GroupTotal` / `GroupNext` |
| Users per group | `UserRequestArray` / `UserGetByGroup` | `UserLogins` + `UserRequestByLogins` |
| Account money | `UserAccountRequestArray` | `UserAccountGetByGroup` |
| Deals (later) | `DealRequestByGroup` per group | per-login `DealRequest` |
| Positions | `PositionRequestByGroup("*")` | per-login |

`GetAccountsAsync(null)` unions every group name from `GetGroupsCore()`. There is **no** `Take(200)` on the catalog path. Logins are unique **per broker** (`(BrokerId, Login)`).

Achiever HTTP proxy (`ProxySet` HTTP `address=host:port`) is applied only when `ACHIEVER_PROXY_ENABLED=true` and host is set. Starwave is direct. Passwords / proxy auth are **not** printed.

### 5.2 Prior measured census (not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`  
JSON (logins, no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe UTC: **2026-08-18T08:42:16.8519545+00:00** (`probe=LiveBrokerProbe`, `connected=true` both brokers).

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK via HTTP proxy | **8** | **6512** | 1506 |
| STARWAVEFX | OK direct | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (manager-visible universe): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

Starwave groups: `Starwave\cent\FX1\grp1/2`, `Starwave\demo\FX2\grp1/2`, `Starwave\real\FX3\grp1–5` + `LP` (counts in measured report).

Honesty: those counts are **this manager login’s permission set**, not a claim that the broker has no other groups. This research slot **did not** reconnect; it confirms the **same fetch APIs are still the live path**.

Dashboard: `GET /api/groups`, `GET /api/traders` read EF catalog filled by that ingest. `/ready` returns group/account counts.

### 5.3 Fetch does not imply copy

Ingest + score + optional `SHADOW_ONLY` rows **cannot** promote to LIVE send. Trade #3 / baseline still cannot emit LIVE as a copy order. Scoring is not allowed to bypass RiskEngine — and today it also cannot reach a sender.

---

## 6. YoPips C++ backend (relevant, not the copy hop)

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\`.

Workspace grep of `CopyIntent` / `ExecutionIntent` / `RiskEngine` / `AllowFixSend` / `NewOrderSingle` under that tree: **0** (those names are a Prop C# design).

What YoPips *does* have:

| Piece | Role | Copy-to-cTrader? |
|---|---|---|
| `GroupService` | Challenge/phase/size → `mt5_group_mappings` | **No** — assignment, not census |
| `mt5_group_probe` / Prop `mt5-sdk` | Manager `GetAllGroups` JSON | Fetch helper, no orders |
| `TradeExecutionService` | Guarded **MT5 `SendTrade`** for challenge terminal (`placeOrder` / close / modify) | **Different product.** Live **MT5** mutations for owned challenge accounts, **not** cTrader FIX copy |

YoPips challenge `SendTrade` is **out of scope** for “copy to cTrader must not send live orders.” Do **not** route Achiever/Starwave manager copy through that service. Do **not** treat YoPips risk-policy tables (`052_risk_policy_enforcement.sql`, `059_risk_decisions_signals_cases.sql`) as `Domain.Risk.RiskEngine`.

Prop `mt5-sdk` is the extracted Manager client (group probe). It is a **reader**. It is not wired as a FIX sender.

---

## 7. Tests do not prove the hop

`D:\Prop\tests\Unit\RiskEngineTests.cs` — **5** facts, in-process `new RiskEngine()`:

| Test | Asserts |
|---|---|
| Stale quote | `QUOTE_STALE`, `AllowFixSend=false` |
| Real flag false | `Approve` + `AllowFixSend=false` |
| Stop-new | blocks open, approves close with send still false |
| Unreconciled | `VENUE_NOT_RECONCILED` |
| Stale signal | `SIGNAL_STALE` |

No test constructs `CopyIntent` → `Evaluate` → `ExecutionIntent`. No test that a worker refuses `35=D` after reject. A89 named suites (`RiskEngineHardLimitTests`, `RiskRejectionBeforeFixSendTests`) are **not on disk** (E005: 0/10). `AllowFixSend=true` facts: **0**.

---

## 8. Honesty / gaps (do not greenwash)

1. **“RiskEngine sits between intents” is FALSE in product.** True only as architecture text and as unused types.
2. **`AllowFixSend` is not a control.** Zero sockets read it.
3. **`SAFE_BY_ABSENCE` is the no-loss mechanism today.** It is the correct operating mode for “fetch all traders, do not lose money.” It is **not** §68/§70 PASS.
4. **Shadow page copy is slightly wrong.** `ShadowPortfolioPage.tsx` says “after a CopyIntent is **approved**.” The writer never calls Evaluate; status is the literal `SHADOW_ONLY`.
5. **SettingsController `RiskEngine` JSON is a decoy** (unbound Redis keys; controller not mapped).
6. **Orphan `Entities.RiskDecision`** vs mapped `RiskDecisionRecord` — two types, one table, zero writers.
7. **This slot did not re-measure 8460.** Cite `LIVE_GROUPS_AND_TRADERS.json` timestamp above. Code still requests `*` groups and all users.
8. **Do not enable `REAL_COPY_EXECUTION_ENABLED`.** Do not add `35=D` until Evaluate is on the persist path and A100/A101 measure PASS.

---

## 9. What “wired” would look like (not built)

Required before any live copy (still **off**):

1. Persist `CopyIntent` per source **event** (not completed-trade backfill).
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
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | Evaluate definition; only product Evaluate |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | FK slots unused |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | No writer |
| `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs` | Mapped table; no writer |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Only CopyIntent writer; no risk |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog-all + score + shadow |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | RiskEngine not registered; flag false |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Fetch/score only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | ALL groups + ALL users |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever + Starwave pair |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send; no builder |
| `D:\Prop\apps\api\Program.cs` | Resync = catalog/score; copy flags false |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 5 smoke facts; not a pipeline test |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups, 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\E005_rules_matrix.md` | 0 product Evaluate callers |
| `D:\Prop\reports\swarm\20260818\E034_no_35d.md` | No 35=D builder |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\trade_execution_service.h` | Different product (MT5 SendTrade) |

---

## 11. Slot JSON (machine)

```json
{
  "slot": 19,
  "verdict": "NOT_WIRED_SAFE_BY_ABSENCE",
  "evidence": "Architecture §32/§75 requires CopyIntent→RiskEngine.Evaluate→ExecutionIntent. Product Evaluate callers=0; RiskEngine not in DI; only CopyIntent writer is PersistDemoShadowAsync (SHADOW_ONLY, no Evaluate, no ExecutionIntent writer). 35=D builder=0. Prior census Achiever 8/6512 + Starwave 10/1948.",
  "risk_to_capital": "NONE_SAFE_BY_ABSENCE"
}
```
