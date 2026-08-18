# B39 — ML status (`D:\Prop\services` empty; Phase 6 still closed)

| Field | Value |
|---|---|
| Agent | B39 (senior engineer; ML / `services/` status only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (local `LastWriteTime` stamps below) |
| Workspace | `D:\Prop` |
| Assigned action | List `D:\Prop\services`. Write this report. **Do not modify product source.** |
| Product source edited | **No.** This file (and a one-line `SWARM_LOG.md` note) are the only writes. |
| Source of law | Architecture v2 §§1, 5, 12, 18–21, 39 / 72.15, 62, **66**, **67 Phase 6**, 69, 71 |
| Binding siblings | **A52** (Phase 6 hold), **A104** (health-stub-only contract if folder is ever reserved), A22 / **B12** (C# baseline), A23 / A53 (`ML_NOT_IN_USE` vs `ML_UNAVAILABLE`), A26 / A63 (`mlProbability: null`), A28, A30, A54, A65, A75, A80 |
| Supersedes (tree snapshot only) | A52 §5 / A104 §2 / A11 L07 **folder listing** — those still said “no `FeatureSnapshot` / no `BaselineScorer` / Application is `Class1`.” Those product claims are **stale**. The **Phase 6 hold is not stale.** |

Classification vocabulary is architecture §73.B:

```text
EXISTS_AND_GOOD
EXISTS_NEEDS_REFACTOR
MISSING
DEPRECATED
UNSAFE
```

---

## 0. Verdict

**Phase 6 is closed. `D:\Prop\services` is an empty directory. That emptiness is the correct default, not a §69 defect.**

`Get-ChildItem -Force D:\Prop\services` returned **zero children** (including hidden). `D:\Prop\services\ml-service` **does not exist**. There is no FastAPI process, no `pyproject.toml`, no XGBoost / scikit-learn / Polars / MLflow / booster file, and no compose service that would start one.

Production ranking that exists today is the **C# deterministic stub** `TraderIntelligence.Domain.Scoring.BaselineScorer` (wired by `ReconstructionScoringService`). It is **not** Phase 6. It is **not** `baseline.v1` (B12). Dashboard `mlProbability` is **hard-coded `null`**. `TraderStateMachine.CanPromoteToLive` is hard-`false`. Risk does not call a model.

Do **not** create `/services/ml-service` from this report. Do **not** train. Do **not** stub a probability. Empty `services/` remains **EXISTS_AND_GOOD** relative to §1 / §18 / §67 / §69 / A52 / A80.

| Question | Answer |
|---|---|
| `D:\Prop\services` listing | **Empty directory.** `child_count=0`. Created 2026-08-18 12:54:14 +05:30. |
| `services/ml-service` | **Absent.** Preferred pre–Phase 6 state (A104 state A). |
| Product `*.py` / `pyproject.toml` / `requirements.txt` / `*.ipynb` / `*.ubj` | **None** under `D:\Prop` excluding `vendor` / `obj` / `bin` / `node_modules` / `.git`. |
| Phase 6 (dataset / chronological split / XGBoost / calibration / top-N vs baseline) | **Closed.** All six §67 delivers still `[ ]`. |
| C# scorer | **Present** as an unversioned stub. Not ML. Not A22. |
| `mlProbability` on API rows | Always **`null`** (`EfDashboardQueries` literal). |
| First useful version needs this folder? | **No** (§69, A30, A57). |

---

## 1. Method

Read-only. No `dotnet` / `npm` / `docker` / training command. No product edit.

| Source | Action |
|---|---|
| `D:\Prop\services` | `Get-Item` + `Get-ChildItem -Force` (hidden included) |
| Python / lockfiles | Recursive file walk under `D:\Prop`, exclude `vendor` / `obj` / `bin` / `node_modules` / `.git` |
| Product C# / web / compose / env / sln | Read + `grep` for `mlProbability`, `IScoringService`, `XGBoost`, `ml-service`, `model_*`, `FEATURE_ML_*` |
| Scoring / ingest / dashboard | `BaselineScorer.cs`, `DealIngestionService.cs` (`ReconstructionScoringService`), `EfDashboardQueries.cs`, `TraderDbContext.cs` |
| Prior law | A52, A104, A22, B12, A65, A75, A80, architecture §§18–21 / 66 / 67 |

SHA-256 of files cited as evidence (PowerShell `Get-FileHash -Algorithm SHA256`):

| Path | Bytes | SHA-256 | Last write (local) |
|---|---:|---|---|
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 8143 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 2026-08-18 13:08:10 |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4277 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` | 2026-08-18 13:09:51 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 2577 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` | 2026-08-18 13:09:51 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | 2026-08-18 13:14:18 |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 2026-08-18 13:12:48 |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | 652 | `48E4C10B5E5A356DA5BB824A32D0A4C857AA2208FA9E4EDE7D145BCCB401ECBA` | 2026-08-18 13:08:41 |
| `D:\Prop\src\Domain\Entities\TraderScoreHistory.cs` | 473 | `3AFA422B6FAFC36994C99CBD8A4C0BB5FB7997688FDB4BEC11F8CA0A7F2CEFD1` | 2026-08-18 13:08:41 |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | 2414 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` | 2026-08-18 13:17:42 |
| `D:\Prop\apps\web\src\pages\ScoringPage.tsx` | 1288 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` | 2026-08-18 13:16:43 |
| `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` | 1592 | `6CAE0FC902D8DFDB5AAC974564D918602EBD3D780C5FAA272BBEF281B19E406D` | 2026-08-18 13:16:00 |
| `D:\Prop\docs\scoring.md` | 327 | `91558CDB4F153379DE5234812116A7EC2569927B9BF45D03F86D4176E0225889` | 2026-08-18 13:20:12 |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | 2026-08-18 13:18:40 |
| `D:\Prop\.env.example` | 3408 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | 2026-08-18 13:06:45 |
| `D:\Prop\.gitignore` | 1107 | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` | 2026-08-18 13:08:02 |

---

## 2. `D:\Prop\services` listing (authoritative)

PowerShell (`Get-Item` + `Get-ChildItem -Force -LiteralPath D:\Prop\services`):

```text
FullName      : D:\Prop\services
Attributes    : Directory
CreationTime  : 2026-08-18T12:54:14.1476221+05:30
LastWriteTime : 2026-08-18T12:54:14.1476221+05:30
child_count   : 0
(empty directory — no children including hidden)
Test-Path D:\Prop\services\ml-service  => False
```

There is no `README`, no `.gitkeep` visible to `-Force`, no `ml-service/`, no `__pycache__`, no `.venv`.

Architecture §66 *suggests*:

```text
/services
  /ml-service
```

A52 / A30 / A80 / A88 / A104: **do not create that folder for the first useful version.** A29 L07 “MISSING” is a **proposed extra**, not a §69 fail. A104 state preference:

| State | When | Class |
|---|---|---|
| **A. Absent** (`services/` empty) | Phases 0–5, §69 | **Preferred. Measured now.** |
| B. FastAPI health stub | Optional later reserve | Maximum pre–Phase 6 surface. Not present. |
| C. Phase 6 scorer | After Phase 5 exit + frozen extract + measured beat of baseline | Later. Not present. |

---

## 3. Measured product surface (2026-08-18)

### 3.1 Absent (Phase 6 / Python)

```text
D:\Prop\services\ml-service          directory does not exist
*.py / pyproject.toml / requirements.txt / Pipfile / environment.yml / *.ipynb / *.ubj
XGBoost / sklearn / mlflow / FastAPI / uvicorn / torch / tensorflow in product lockfiles
IScoringService / IScoreUpdateRequestHandler
src/Scoring as its own project          (Domain.Scoring/ exists instead — C# baseline)
trader_feature_snapshots table / entity
model_versions / model_predictions / model_evaluations
ScoreUpdate outbox handler
Models React page
ml object on GET /api/health
compose service ml-service
```

`Mt5TraderIntelligence.sln` projects: Domain, Application, Infrastructure, Mt5, Fix.CTrader, Api, Mt5Worker, FixWorker, Tests.Unit, Tests.Integration. **No Python project.**

`docker-compose.yml` services: `postgres`, `redis`, `api`. Comment: native MT5 workers stay on Windows. **No `ml-service`.** Matches A65 “Never: … ml-service”.

### 3.2 Present (not Phase 6)

| Path | What it is | What it is not |
|---|---|---|
| `Domain/Scoring/BaselineScorer.cs` | In-process rules stub: `FeatureSnapshot` + `risk/behavior/early_quality` + `TraderStateMachine` | XGBoost, as-of snapshot store, official `y` |
| `ReconstructionScoringService` | Rebuilds reconstructed trades, scores **all** completed XAUUSD lifecycles, upserts `TraderScore` | Outbox `ScoreUpdate` consumer; `IScoringService` port |
| `TraderScore` / `TraderScoreHistory` | Latest + history of the three baseline numbers + flags + state | `MlProbability`; as-of feature vector; `completed_trade_count` on history unique key |
| `FeatureQuality` / `PriceSource` enums | Vocabulary | Unused as durable columns. Scorer hard-sets `Unavailable` / `Unknown` and leaves MFE/MAE null |
| `TraderRowDto.MlProbability` | `decimal?` on the contract | Never filled |
| `EfDashboardQueries.GetTradersAsync` | Passes **`null`** as `MlProbability` | A fake 0 / 0.5 |
| `ScoringPage.tsx` | Banner: “XGBoost is not active…” | A trainer |
| `TraderDetailPage.tsx` | Renders `data.mlProbability ?? 'not trained'` | A number |
| `docs/scoring.md` | One screen: ML is Phase 6 and must beat the baseline | A training spec |
| `OutboxEventType.ScoreUpdate` | Enum token | No writer/handler that scores via ML |
| `.gitignore` `services/ml-service/.venv/` + `__pycache__/` | Forward-looking ignore | Proof the folder exists |
| `.env.example` `FEATURE_ML_SCORING_ENABLED=false` | Unused flag name | Architecture (A75: do not treat this key as law). **Zero** product `.cs` / `.ts` / `.tsx` references |

`DI` registers `BaselineScorer` as singleton and `ReconstructionScoringService` as scoped (`DependencyInjection.cs` lines 39–41). No HTTP client to a Python host.

### 3.3 `mlProbability` path (honest)

`TraderRowDto`:

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

```text
s.EarlyQualityScore,
null,          // MlProbability
s.RiskScore,
```

`TraderScore` has **no** ML field. There is nothing to persist a probability into. UI fallback `'not trained'` is acceptable copy; it must **not** be replaced with `0`.

API `/api/health` lists demo `mt5Connections` / `fixSessions` / `database` / `redis` / `outboxBacklog`. **No `ml` probe.** Correct default is absent = `ML_NOT_IN_USE`, not `ML_UNAVAILABLE` (A53 / A104). Do not add a red ML outage for a service that is not supposed to exist.

`/api/settings` feature flags: only `REAL_COPY_EXECUTION_ENABLED=false`. The `.env.example` `FEATURE_ML_SCORING_ENABLED` is **not** wired.

### 3.4 Tests

`tests/Unit/BaselineScorerTests.cs` exists (B12’s “zero tests” snapshot is **stale**). Three facts:

- N=2 → `INSUFFICIENT_DATA`
- three disciplined winners → `SHADOW`; `CanPromoteToLive` is false
- losing martingale 0.10 / 0.20 / 0.40 → `RISK_BLOCKED`

No XGBoost / leakage-split / calibration suite — and A27 says those are **not** a §69 gate.

---

## 4. Why Phase 6 is still closed (six blockers; any one is enough)

A52 §4 still holds against **this** tree. Update only the “baseline type exists” row.

| Blocker | Measured now |
|---|---|
| **Phase order** | Phases 1–5 are not exited. Shadow engine is a calculator stub (B18: not a destination-quote pipeline). No live-quality extract. |
| **Official label** | `future_net_copy_pnl` / copy-book DD of reconstructed XAUUSD #4–#23 **does not exist**. `ShadowCopyEngine` is unused by ingestion. Source `NetRealizedPnl` is the wrong `y`. |
| **As-of features** | In-memory `FeatureSnapshot` is computed on the **entire** completed-XAU list at rebuild time. No `trader_feature_snapshots` row keyed `(broker_id, login, completed_trade_count, feature_schema_version)`. Latest `TraderScore` is not an as-of vector. |
| **Chronological population** | No dataset builder. No 70/15/15 by trade-#3 `closed_at`. No split hashes. |
| **Baseline to beat** | A **stub** exists (good that we did not start with XGBoost). B12: formulas are **not** A22 `baseline.v1`. There is no locked top-N economics table. ML has nothing legal to beat. |
| **Data quality** | Fake MT5 demo connector + `EnsureCreated` + seeder. No proven dual-broker ledger of ~5k accounts. No source tick tape (A17). MFE/MAE left `Unavailable` — **correct** (do not fabricate). |

Training this week would leak “now” aggregates, invent `y` from source PnL or challenge outcome, and shuffle. That would not be Phase 6.

---

## 5. What the C# stub must not be mistaken for

B12 remains the scoring review. B39 only records ML implications:

| Claim | True state |
|---|---|
| “We have an ML service” | **False.** Folder empty. |
| “`FeatureSnapshot` means Phase 6 features exist” | **False.** Transient C# record; no as-of cut; no persistence. |
| “`BaselineScorer` means Phase 6 can start” | **False.** Challenger needs a **versioned** baseline and official labels. |
| “`CanPromoteToLive == false` means ML is the live gate” | **False.** Live is unreachable at every N. Vacuous vs A22 I4. |
| “UI ‘not trained’ means a model failed” | **False.** `ML_NOT_IN_USE`. Use `ML_UNAVAILABLE` only after a promoted scorer is configured and down. |
| “`.gitignore` paths mean `ml-service` is half-built” | **False.** Ignore lines for a future tree. |
| “`FEATURE_ML_SCORING_ENABLED=false` is a Phase 6 control plane” | **False.** Unused. A75: do not treat that name as architecture. Flipping it to true would do **nothing** today — and must not be used to sneak a scorer later. |
| “Empty `services/` is a gap to close in I0–I9” | **False.** A30 forbids `services/ml-service/**` in that sequence. |
| “B03 wants `model_*` tables, so create them now” | **Not this agent.** A52 §9.3: do not create empty `model_*` mappings just to look ready. Measured: `TraderDbContext` has **no** those `DbSet`s. Leave them uncreated until Phase 6 extract exists. |

Scoring that is allowed before Phase 6 (other agents / other phases):

- Persist reconstructed XAUUSD lifecycles and a first-3 counter.
- Deterministic as-of-n features + risk flags + `mlProbability=null` on the dashboard.
- Shadow copy book = future official label substrate.

B39 does not implement those.

---

## 6. A104 stub contract (still optional; still not to build)

If someone later reserves §66, the **maximum** legal Python surface before Phase 6 remains A104:

- FastAPI + Uvicorn **only**
- `GET /health` liveness; `GET /health/ready` → `status=not_in_use`, `reason=ML_NOT_IN_USE`, `phaseState=PHASE_6_CLOSED`, `mlProbability=null`
- `/predict` / `/score` / `/v1/score` / `/train` must **404**, not 501
- compose `profiles: [ml-stub]`, **off** by default
- no trading-DB credentials, no booster files, no `model_*` writers

That increment is **not** part of §69 and is **not** opened by this report. Preferred state remains **absent**.

---

## 7. Honest status vs a false PASS

| Claim | True measured state |
|---|---|
| “EX5 / ML fully ready” | **False.** No model, no dataset, no split. |
| “We can train a quick XGBoost on MT5 deals” | **False as Phase 6.** Deals ≠ trades; source PnL ≠ copy PnL; no as-of cut. |
| “200 on a future `/health` means the model is ready” | **False.** Would be process liveness only. |
| First useful version | Phases 0–5 + React. **No ML** (§69). |
| Leaving ML unbuilt | **Compliance** with §1, §18, §67, §69. |

Prefer a missing folder over a leaked PASS.

---

## 8. Recommendation

**Do not build ML now.** Leave `D:\Prop\services` empty. Do not add FastAPI, XGBoost, `model_*` migrations, or a numeric `mlProbability`. Keep ranking on the C# stub (and later a real `baseline.v1`), reason code `ML_NOT_IN_USE`, live promotion off. Reopen Phase 6 only after Phase 5 exit, a frozen leakage-checked extract, chronological split hashes, and a measured top-N copy-economics beat of the locked baseline.

**Product source was not modified.**
