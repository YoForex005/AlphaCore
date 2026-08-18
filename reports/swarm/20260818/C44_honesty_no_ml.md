# C44 — Honesty: ML is not built (and that is correct)

| Field | Value |
|---|---|
| Agent | C44 (senior engineer; honesty / Phase-6 hold only) |
| Date | 2026-08-18 13:28:29 +05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\C44_honesty_no_ml.md` |
| Assigned | Confirm ML is **not** built, and that absence is **correct**. Write this report. **Do not modify product source.** |
| Workspace | `D:\Prop` |
| Product source edited | **No.** This report (plus a one-line `SWARM_LOG.md` / `INDEX.md` catalog line) are the only writes. |
| Source of law | Architecture v2 §1.3, §5, §12, §18–§21, §39 / §72.15, §62, §66, **§67 Phase 6**, **§69**, §71 |
| Binding siblings | **A52** (hold), **A104** (health-stub-only if folder ever reserved), **B39** (folder snapshot), A22 / B12 (C# baseline ≠ ML), A23 / A53 (`ML_NOT_IN_USE` vs `ML_UNAVAILABLE`), A26 / A63 (`mlProbability: null`), A28, A30, A57, A65, A75, A80 |
| Does not supersede | A52 (Phase 6 contract), A104 (stub contract), B12 (baseline review). **Re-measures** B39’s tree snapshot; hashes match. |
| Classification (§73.B) | ML service / training / `mlProbability` fill = **MISSING**. Relative to Phase 6 being **closed**, that absence is **EXISTS_AND_GOOD** (preferred state A104-A). |

Honesty vocabulary for this file:

```text
MEASURED     — observed on disk at the timestamp above
NOT ML       — present C# rules stub; must not be sold as a model
CORRECT      — required by §1 / §18 / §67 / §69; not a §69 defect
POLICY FAIL  — would be wrong even if it compiled and tests were green
GREENWASH    — claiming Phase 6 / “ML ready” / a numeric probability without a beaten OOS baseline
```

---

## 0. Verdict

**ML is not built. That is the correct state.**

`Get-ChildItem -Force D:\Prop\services` returned **zero children**. `D:\Prop\services\ml-service` **does not exist**. There is no FastAPI host, no `pyproject.toml` / `requirements.txt`, no XGBoost / scikit-learn / Polars / MLflow / Torch / TensorFlow package, no booster / ONNX / pickle, no `model_*` entity or `DbSet`, no `/predict` or `/v1/score` route, and no compose service that would start one.

The ranker that exists is in-process C# `TraderIntelligence.Domain.Scoring.BaselineScorer`, called by `ReconstructionScoringService`. It is **NOT ML**. It is **not** A22 `baseline.v1` (B12). Dashboard `MlProbability` is a **literal `null`**. `TraderStateMachine.CanPromoteToLive` is hard-`false`. Risk does not call a model. `/api/health` has **no** `ml` probe.

Leaving ML unbuilt is **compliance** with architecture §1 (“Do not use ML first”), §18 (deterministic baseline before XGBoost), §67 Phase 6 (only after data quality is proven), and §69 (“The first genuinely useful system does **not** need ML”).

Do **not** create `/services/ml-service` from this report. Do **not** train. Do **not** stub `0` / `0.5` / last source PnL into `mlProbability`. Do **not** paint A29 L07 / A11 “MISSING `ml-service`” as a first-useful-version fail.

| Question | Honest answer |
|---|---|
| Is ML built? | **No.** |
| Is that a defect for §69 / Phases 0–5? | **No. Correct absence.** |
| Is `BaselineScorer` a model? | **No.** Named-constant rules over reconstructed XAU trades. |
| Is `mlProbability` filled? | **No.** `EfDashboardQueries.GetTradersAsync` passes `null`. |
| Reason code while Phase 6 is closed | **`ML_NOT_IN_USE`**, not `ML_UNAVAILABLE` (A53 §2.2). |
| First useful version needs this folder? | **No** (§69, A30, A57). |
| Phase 6 open? | **No.** All six §67 delivers still unchecked. |

`PHASE0_AUDIT.md` already records: `ML | MISSING (correct — Phase 6)`. C44 re-measures the same fact. It is still true.

---

## 1. Method

Read-only. No `dotnet` / `npm` / `docker` / training command. **No product edit.**

| Source | Action |
|---|---|
| `D:\Prop\services` | `Get-Item` + `Get-ChildItem -Force` (hidden included) |
| Python / model artifacts | Recursive `*.py` / lockfiles / `*.ipynb` / `*.ubj` / `*.pkl` / `*.onnx` / `*.pt` / `*.h5` / `*.joblib` under `D:\Prop`, exclude `vendor` / `obj` / `bin` / `node_modules` / `.git` |
| Product C# / web / compose / sln / env | Read + `Select-String` for `mlProbability`, `IScoringService`, `XGBoost`, `ml-service`, `model_*`, `FEATURE_ML_*`, `sklearn`, `mlflow` |
| Scoring / ingest / dashboard / DI / API | `BaselineScorer.cs`, `DealIngestionService.cs` (`ReconstructionScoringService`), `EfDashboardQueries.cs`, `TraderDbContext.cs`, `DependencyInjection.cs`, `apps/api/Program.cs` |
| Prior law | A52, A104, B39, A22, B12, A53, A65, A75, A80, architecture §§18–21 / 66 / 67 / 69 |

SHA-256 of files cited as evidence (PowerShell `Get-FileHash -Algorithm SHA256`). **Byte-identical to B39’s table** for the overlapping paths — the ML surface has not drifted since that snapshot.

| Path | Bytes | SHA-256 | Last write (local) |
|---|---:|---|---|
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 8143 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 2026-08-18 13:08:10 |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4277 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` | 2026-08-18 13:09:51 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 2577 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` | 2026-08-18 13:09:51 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | 2026-08-18 13:14:18 |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 2026-08-18 13:12:48 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 2026-08-18 13:14:18 |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | 652 | `48E4C10B5E5A356DA5BB824A32D0A4C857AA2208FA9E4EDE7D145BCCB401ECBA` | 2026-08-18 13:08:41 |
| `D:\Prop\src\Domain\Entities\TraderScoreHistory.cs` | 473 | `3AFA422B6FAFC36994C99CBD8A4C0BB5FB7997688FDB4BEC11F8CA0A7F2CEFD1` | 2026-08-18 13:08:41 |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | 2414 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` | 2026-08-18 13:17:42 |
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | 2026-08-18 13:22:04 |
| `D:\Prop\apps\web\src\pages\ScoringPage.tsx` | 1288 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` | 2026-08-18 13:16:43 |
| `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` | 1592 | `6CAE0FC902D8DFDB5AAC974564D918602EBD3D780C5FAA272BBEF281B19E406D` | 2026-08-18 13:16:00 |
| `D:\Prop\docs\scoring.md` | 327 | `91558CDB4F153379DE5234812116A7EC2569927B9BF45D03F86D4176E0225889` | 2026-08-18 13:20:12 |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | 2026-08-18 13:18:40 |
| `D:\Prop\.env.example` | 3408 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | 2026-08-18 13:06:45 |
| `D:\Prop\.gitignore` | 1107 | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` | 2026-08-18 13:08:02 |
| `D:\Prop\Mt5TraderIntelligence.sln` | 7019 | `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` | 2026-08-18 13:02:00 |

---

## 2. Measured: ML is not built

### 2.1 `D:\Prop\services` (authoritative)

```text
FullName      : D:\Prop\services
Attributes    : Directory
CreationTime  : 2026-08-18T12:54:14.1476221+05:30
LastWriteTime : 2026-08-18T12:54:14.1476221+05:30
child_count   : 0
(empty directory — no children including hidden)
Test-Path D:\Prop\services\ml-service  => False
```

No `README`, no `.gitkeep` visible to `-Force`, no `ml-service/`, no `__pycache__`, no `.venv`.

Architecture §66 *suggests* `/services/ml-service`. A52 / A30 / A80 / A88 / A104: **do not create that folder for the first useful version.** A104 preference order:

| State | When | Class now |
|---|---|---|
| **A. Absent** (`services/` empty) | Phases 0–5, §69 | **Preferred. Measured.** |
| B. FastAPI health stub | Optional later reserve | Not present. Not opened by C44. |
| C. Phase 6 scorer | After Phase 5 exit + frozen extract + measured beat of baseline | Later. Not present. |

### 2.2 Python / training / model files

| Artifact | Measured |
|---|---|
| Product `*.py` excluding vendor/obj/bin/node_modules/.git | **One file:** `D:\Prop\scripts\svg_to_png.py` (2477 B) — SVG→PNG for docs. **Not** a scorer. |
| `pyproject.toml` / `requirements.txt` / `Pipfile` / `environment.yml` | **None** |
| `*.ipynb` / `*.ubj` / `*.pkl` / `*.joblib` / `*.onnx` / `*.pt` / `*.h5` | **None** |
| XGBoost / sklearn / mlflow / FastAPI / uvicorn / torch / tensorflow in product lockfiles or `*.csproj` | **None.** Product packages are EF Core, Npgsql, Redis, FluentValidation, Serilog, Swashbuckle, xUnit. |
| `IScoringService` / `IScoreUpdateRequestHandler` | **No symbol** under `D:\Prop\src` |
| `ModelVersion` / `ModelPrediction` / `ModelEvaluation` / `trader_feature_snapshots` | **No entity, no DbSet, no table mapping** |
| `POST /api/v1/models/{id}/promote` / Models React page | **Absent.** `apps/web/src/pages/` has 15 modules; no `ModelsPage.tsx`. |
| Compose `ml-service` | **Absent.** Services are `postgres`, `redis`, `api` only. |
| `/api/health` `ml` object | **Absent.** Keys: `mt5Connections`, `fixSessions`, `database`, `redis`, `outboxBacklog`. |
| `FEATURE_ML_SCORING_ENABLED` in product `.cs` / `.ts` / `.tsx` | **Zero references.** Only `.env.example` line 108 `=false`. Unused. A75: not architecture. |

`Mt5TraderIntelligence.sln` projects: Domain, Application, Infrastructure, Mt5, Fix.CTrader, Api, Mt5Worker, FixWorker, Tests.Unit, Tests.Integration. **No Python project.**

DI (`DependencyInjection.cs` 39–41) registers `BaselineScorer` singleton + `ReconstructionScoringService` scoped. **No HTTP client** to a Python host.

### 2.3 `mlProbability` path (honest)

Contract allows a nullable column (A26 / A63). That is **not** a model:

```43:57:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public sealed record TraderRowDto(
    string Broker,
    long Login,
    string? Group,
    int CompletedXauTrades,
    decimal NetSourcePnl,
    decimal EarlyScore,
    decimal? MlProbability,
    decimal RiskScore,
    bool Martingale,
    bool AveragingDown,
    bool LotEscalation,
    TraderState State,
    decimal ShadowPnl,
    DateTimeOffset LastScored);
```

Mapper always:

```93:107:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            mapped.Add(new TraderRowDto(
                b.Code,
                s.Login,
                account?.GroupName,
                s.CompletedXauTrades,
                pnl,
                s.EarlyQualityScore,
                null,
                s.RiskScore,
                s.Martingale,
                s.AveragingDown,
                s.LotEscalation,
                s.CurrentState,
                0,
                s.LastScoredAt));
```

`TraderScore` / `TraderScoreHistory` have **no** ML field. There is nothing to persist a probability into. UI `data.mlProbability ?? 'not trained'` (`TraderDetailPage.tsx`) is acceptable copy. It must **not** become `0`. Scoring page banner is honest: “XGBoost is not active. ML must beat this baseline out of sample before it is used.”

`/api/settings` feature flags: only `REAL_COPY_EXECUTION_ENABLED=false`. Flipping `.env.example` `FEATURE_ML_SCORING_ENABLED` would do **nothing**.

### 2.4 Domain entities present (none are ML)

`AuditLog`, `Broker`, `CanonicalInstrument`, `CopyIntent`, `DestinationQuote`, `ExecutionIntent`, `FixSessionState`, `KillSwitch`, `Mt5Account`, `Mt5Deal`, `Mt5Group`, `Mt5Position`, `OutboxEvent`, `ReconstructedTrade`, `RiskDecisionRecord`, `ShadowOrder`, `SourceSymbolMapping`, `SyncCheckpoint`, `TraderScore`, `TraderScoreHistory`.

`OutboxEventType.ScoreUpdate` is an **enum token only**. No writer/handler scores via ML.

---

## 3. Why “not built” is **correct** (law, not a gap)

Quoted / restated from architecture (do not weaken):

| Location | Binding statement | Implication now |
|---|---|---|
| §1 change #3 | “Do not use ML first. Build a deterministic statistical baseline first. ML should have to beat that baseline out-of-sample.” | Starting XGBoost this week is a **policy FAIL**. |
| §18 | Before XGBoost, build `risk_score`, `behavior_score`, `early_quality_score`. That baseline is the benchmark ML must beat. | A C# stub exists. B12: it is **not** locked `baseline.v1`. ML still has nothing legal to beat. |
| §19–§21 | Train XGBoost on as-of trade-#3 features; label next 20 **copy-horizon** trades by **venue-net** P&L + DD cap; split **chronologically**; accept only if top-N **economics** beat the baseline. No DNN first. | Inputs do not exist (see §5). |
| §12 | Do not couple MT5 callbacks to ML. | Fake ingest → persist → later `RebuildTraderAsync` is C# rules only. Correct. |
| §39 / §72.15 | Scoring/ML may emit only candidate / confidence / suggested allocation. Risk is last authority. ML never bypasses risk. | RiskEngine does not call a model. Correct. |
| §62 | ML unavailable: continue ingest/reconstruction; do not promote new traders to live; hard limits stay on. | Live promotion is hard-off (`CanPromoteToLive => false`). |
| §67 Phase 6 | Deliver **only after data quality is proven**: dataset, chronological split, XGBoost, calibration, top-N, comparison vs baseline. | All six still `[ ]`. Phase **closed**. |
| §69 | “The first genuinely useful system does **not** need ML.” Judge ML only after items 1–12 work. | Empty `services/` is **not** a §69 fail. |
| §71 | Do not add LLM/AI API, deep learning, RL, automated model self-promotion. | Absent. **EXISTS_AND_GOOD** (A80). |
| A30 I0–I9 | `services/ml-service/**` is on the do-not-create list. | Creating it now would violate the sequence. |
| A26 / A63 | `mlProbability` nullable until Phase 6. **Do not stub a number.** | Literal `null` is the honest fill. |
| A53 §2.2 | Phase 0–5 / FUV on deterministic baseline only = `ML_NOT_IN_USE`, **not** down. | Do not add a red ML outage for a service that must not exist. |
| A27 | No XGBoost / leakage-training suites as a §69 gate. | Missing ML tests are **correct**. |

A29 L07 (`services/ml-service` MISSING) and A29 T12 (Python/XGBoost MISSING) are **proposed extras**, not first-useful-version defects. A104: empty `D:\Prop\services` is still the **correct default**.

---

## 4. What exists that is **NOT ML** (do not greenwash)

| Path | What it is | What it is not |
|---|---|---|
| `Domain/Scoring/BaselineScorer.cs` | In-process rules: `FeatureSnapshot` + risk / behavior / early_quality + `TraderStateMachine` | XGBoost, learned weights, as-of snapshot store, official `y` |
| `ReconstructionScoringService` | Rebuilds reconstructed trades, scores **all** completed XAUUSD lifecycles, upserts `TraderScore` | Outbox `ScoreUpdate` consumer; `IScoringService` port; Phase 6 inference |
| `FeatureSnapshot` | Transient C# record computed on the **entire** completed-XAU list at rebuild time | Durable `trader_feature_snapshots` keyed `(broker, login, n, schema)` |
| `MaeMfeQuality = Unavailable` | Honest: no source tick tape (A17). Do not fabricate. | A trained MFE/MAE feature |
| `TraderScore` three numbers + flags + state | Deterministic stub persist | `MlProbability`; model version |
| `CanPromoteToLive => false` | Live unreachable at every N | An ML live-gate |
| `ScoringPage.tsx` / `docs/scoring.md` | Operator copy: XGBoost not active; Phase 6 must beat OOS | A trainer |
| `.gitignore` `services/ml-service/.venv/` | Forward-looking ignore | Proof the folder exists |
| `.env.example` `FEATURE_ML_SCORING_ENABLED=false` | Unused name | A Phase 6 control plane (A75) |
| `scripts/svg_to_png.py` | Docs PNG fallback | Product Python / ML |
| `OutboxEventType.ScoreUpdate` | Enum | An ML worker |

B12 remains the scoring review. C44 only records the honesty implication: **a rules stub is progress toward §18, not a license to open Phase 6.**

---

## 5. Honesty table — claims that would be a **false PASS**

| Claim | True measured state | Class |
|---|---|---|
| “We have an ML service / XGBoost is wired” | Folder empty. No booster. No HTTP scorer. | **False.** |
| “ML is a gap we must close for first useful version” | §69 first sentence: FUV does **not** need ML. | **False as §69.** Correct absence. |
| “`FeatureSnapshot` means Phase 6 features exist” | Transient; no as-of cut; no persist. | **Greenwash.** |
| “`BaselineScorer` means we can train now” | Challenger needs a **versioned** baseline, official copy-PnL labels, chronological split hashes. | **False as Phase 6.** |
| “UI ‘not trained’ means a model failed” | `ML_NOT_IN_USE`. `ML_UNAVAILABLE` is only for a **promoted** scorer that is down (A53). | **False.** |
| “Health should show ML red until we add FastAPI” | Absent probe = unused by design. A red light would **lie**. | **Policy FAIL if added.** |
| “Stub `mlProbability=0` or `0.5` so the column is not empty” | Fake score (A26, A80). Would look like a calibrated probability. | **Policy FAIL.** |
| “Train a quick XGBoost on MT5 deals / latest `TraderScore`” | Deals ≠ trades; source PnL ≠ copy PnL; no as-of cut; shuffle leaks future (A52 §§4–8). | **False as Phase 6.** |
| “`.gitignore` / `FEATURE_ML_*` means half-built ML” | Ignore + unused env name. | **False.** |
| “B03 wants `model_*` tables, so create them to look ready” | A52 §9.3: do not create empty `model_*` mappings just to look ready. Measured: **no** those DbSets. | **Not this agent.** Leave uncreated. |
| “Flip `FEATURE_ML_SCORING_ENABLED=true` to enable scoring” | Flag is **not wired**. Flip does nothing today and must not sneak a scorer later. | **False.** |
| “A104 health stub is required for §69” | Optional later reserve. Preferred state remains **absent**. | **False.** |
| “EX5 / ML fully ready / ≥95% model” | No model, no dataset, no split, no eval table. | **False.** Prefer this missing folder over a leaked PASS. |
| “Deep net / LLM / RL to rank traders” | §71 / A80 banned even after Phase 6 opens. Phase 6 is XGBoost first. | **Policy FAIL.** |

Prefer a **missing** folder and a **null** probability over a compiled fake.

---

## 6. Phase 6 is still closed (six blockers; any one is enough)

A52 §4 still holds. Update only the “a C# stub exists” row versus the original A52 “baseline does not exist” wording.

| Blocker | Measured now |
|---|---|
| **Phase order** | Phases 1–5 are not exited. Shadow is a calculator stub (B18). No live-quality extract. |
| **Official label** | `future_net_copy_pnl` / copy-book DD of reconstructed XAUUSD #4–#23 **does not exist**. `ShadowCopyEngine` is unused by ingestion. Source `NetRealizedPnl` is the **wrong** `y`. |
| **As-of features** | Features computed on the **full** completed-XAU list at rebuild. No `trader_feature_snapshots` row. Latest `TraderScore` is not an as-of vector. |
| **Chronological population** | No dataset builder. No 70/15/15 by trade-#3 `closed_at`. No split hashes. |
| **Baseline to beat** | A stub exists (good we did not start with XGBoost). B12: formulas are **not** A22 `baseline.v1`. No locked top-N economics table. |
| **Data quality** | Fake MT5 demo connector + `EnsureCreated` + seeder. No proven dual-broker ledger of ~5k accounts. No source tick tape. MFE/MAE left `Unavailable` — **correct**. |

Training this week would leak “now” aggregates, invent `y` from source PnL, and shuffle. That would not be Phase 6. It would be a **false** research PASS.

§67 Phase 6 checklist (still all open):

```text
[ ] training dataset
[ ] chronological split
[ ] XGBoost
[ ] probability calibration
[ ] top-N evaluation
[ ] comparison against deterministic baseline
```

---

## 7. Reason-code discipline (do not confuse unused with down)

| Code | When | Current tree |
|---|---|---|
| **`ML_NOT_IN_USE`** | Phase 0–5 / FUV on deterministic baseline only (A53 §2.2). Scorer **must not** exist. | **This is the honest label.** |
| **`ML_UNAVAILABLE`** | A **promoted** Phase 6 host/model is configured and heartbeat / inference / freshness failed. Blocks **new promotion**, not hard limits. Continue ingest/reconstruction (§62). | **Must not be reported today.** There is no promoted scorer. |
| Fake `0` / `1` / last PnL on inference fail | Explicitly forbidden (A53 §4.2). | N/A — no inference path. |

`/api/health` omitting `ml` is **correct**. Do not add a red ML component “so operators notice.” Operators should notice **null** `mlProbability` and the scoring-page banner.

---

## 8. Stale snapshots (so C44 is not confused with old A-band wording)

| Older claim | Status |
|---|---|
| A52 / A02 / A11: no `FeatureSnapshot`, no `BaselineScorer`, Application is `Class1` | **Stale product claims.** Stub exists now. **Phase 6 hold is not stale.** |
| A80: `docker-compose.yml` MISSING | **Stale** (B37). Compose exists and still has **no** `ml-service` — that part remains true. |
| B12: zero `BaselineScorerTests` | **Stale.** Three facts exist: N=2 `INSUFFICIENT_DATA`; three winners → `SHADOW` + `CanPromoteToLive` false; martingale 0.10/0.20/0.40 → `RISK_BLOCKED`. Still **not** an ML suite. |
| A29 L07 “MISSING `ml-service`” as a build ticket | **Still not a §69 ticket.** C44 classifies it **EXISTS_AND_GOOD** (absent on purpose). |

B39’s folder listing and hashes **still match** this measurement. C44 does not reopen anything B39 closed.

---

## 9. Recommendation

**Do not build ML now.** Leave `D:\Prop\services` empty. Do not add FastAPI, XGBoost, `model_*` migrations, a Models page that implies a registry, a health `ml` probe, or a numeric `mlProbability`.

Keep ranking on the C# stub (and later a real locked `baseline.v1`). Keep reason code **`ML_NOT_IN_USE`**. Keep live promotion off. Reopen Phase 6 only after:

1. Phase 5 exit and proven data quality (§67),
2. a frozen, leakage-checked extract with as-of trade-#3 features,
3. official `future_net_copy_pnl` labels from the shadow/copy book (not source PnL),
4. chronological split hashes,
5. a measured top-N **copy-economics** beat of the locked baseline.

Until then, **“ML not built” is the honest status and the required status.**

**Product source was not modified.**
