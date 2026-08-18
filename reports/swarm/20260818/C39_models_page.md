# C39 — Architecture §46 Models page is missing by design (Phase 6 closed)

| Field | Value |
|---|---|
| Agent | C39 (senior engineer; Models-page / Phase 6 hold only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:28:04+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned action | Architecture **§46** Models page — missing **by design** because **Phase 6** is closed. Write this report. **Do not modify product source.** |
| Product source edited | **No.** This file (plus a one-line `SWARM_LOG.md` / `INDEX.md` catalog note) are the only writes. |
| Source of law | Architecture v2 **§46** (nav label `Models`), **§18** (deterministic baseline first), **§19–21** (ML objective / leakage / evaluation), **§45** (`model_*` tables), **§59** (promote is SuperAdmin), **§66** (`/services/ml-service` later), **§67 Phase 6**, **§69** (first useful version does **not** need ML), **§71** (no self-promotion / no DNN-first) |
| Binding siblings | **A52** (Phase 6 hold), **A28** (phase gates), **A30** I6 “do not add Models as a working page”, **A57** (omit Models is acceptable for §69), **A63** (`GET /api/v1/models` **out of v1**), **A104** (if a page exists, say “Phase 6 not open”), **A80**, **B39** (tree: `services/` empty), **C44** (ML not built — correctly), **A26** §5.2 / §6.8 (later contract), **C08** / **B20** / **B31** (file census: no `ModelsPage`). Adjacent §46 leaves: **C37** (Live Copy = chrome, not book), **C38** (Audit). |
| Supersedes (classification only) | B20 / B22 / B31 / C08 / A29 **U06** calling Models **MISSING** as if it were a first-useful coding remainder. Those reports remain correct as a **file census**. This file reclassifies the remainder: **MISSING_BY_DESIGN / EXISTS_AND_GOOD (absence)** until Phase 6 opens. |
| Classification vocabulary | Architecture §73.B: `EXISTS_AND_GOOD` \| `EXISTS_NEEDS_REFACTOR` \| `MISSING` \| `DEPRECATED` \| `UNSAFE` |

---

## 0. Verdict

**The architecture §46 `Models` leaf is absent from the React shell because Phase 6 is closed. That absence is the correct default, not a §69 defect.**

`list_dir` of `D:\Prop\apps\web\src\pages` returned **15** `*.tsx` files. There is **no** `ModelsPage.tsx`. `App.tsx` has **no** `path="models"`. `DashboardLayout` has **no** `{ to: '/models', label: 'Models' }`. `hooks.ts` has **no** `useModels`. `apps/api/Program.cs` has **no** `/api/models` and **no** `/api/v1/models`. `TraderDbContext` has **no** `model_versions` / `model_predictions` / `model_evaluations`. `D:\Prop\services` is an **empty** directory (`child_count=0`). `ml-service` does not exist.

Architecture §46 **names** Models in the main navigation. It does **not** give Models a widget list (there is no “# Models Page” heading after §47–54). Evaluation law lives in **§21**. Delivery law lives in **§67 Phase 6**. First-useful law (**§69**) says the system does **not** need ML. A63 puts `GET /api/v1/models` + promote in the **out-of-v1** table. A30 increment 6: *“Do not add Models … as working pages.”* A57: omitting Models from the sidebar is **acceptable** for §69.

| Question | Answer |
|---|---|
| Is there a Models page / route / nav / hook / API? | **No** (measured 2026-08-18T13:28:04+05:30). |
| Is that a first-useful coding gap? | **No.** Phase 6 is closed. |
| §73.B class for the **working** Models surface (promote, versions, probabilities, `model_*` tables, FastAPI scorer) | **EXISTS_AND_GOOD** as **absence**. Creating it now would be a policy FAIL. |
| §73.B class for an honest placeholder (`/models` → “Phase 6 not open”, `data: []`) | **Optional remainder.** A104 / A62 / A30 allow it. A57 / A63 do **not** require it. Current absence is legal. |
| What is the Phase 0–5 substitute? | `/scoring` + C# `BaselineScorer` + `mlProbability = null`. |
| Did this pass create `ModelsPage.tsx`? | **No.** Product source was not touched. |

Do **not** treat C08/B20/B31 “Models MISSING” as a ticket to implement A26 §6.8. Do **not** stub fake model rows, a promote button, or a non-null `mlProbability`.

---

## 1. Method

Read-only. No `dotnet` / `npm` / `docker` / training command. No product edit.

| Source | Action |
|---|---|
| Architecture §46 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1735–1758 (nav). Confirmed **no** dedicated Models page section in the `# N.` heading list. |
| Architecture Phase 6 / §69 | Same file: §18 (L786–817), §19–21 (L821–921), §45 `model_*` (L1700–1702), §59 promote (L2220), §67 Phase 6 (L2558–2569), §69 (L2633–2654), §71 |
| Router / nav / pages | Read `App.tsx`, `DashboardLayout.tsx`, `list_dir` of `apps/web/src/pages` (15 files) |
| Web search | PowerShell `Select-String` on `apps/web/src` for `ModelsPage`, `/models`, `useModels`, `modelVersion`, `PROMOTE_MODEL` → **0 hits** |
| API | Read `apps/api/Program.cs` (14 `MapGet` / 1 `MapPost`; none are models) |
| Dashboard contract | `DashboardModels.cs` `TraderRowDto.MlProbability`; `EfDashboardQueries` literal `null` (line 100) |
| Persistence | `TraderDbContext` 20 `DbSet`s; no `model_*`. `Domain\Entities` listing: 20 files, no `ModelVersion` / `ModelPrediction` / `ModelEvaluation` |
| ML tree | `Get-ChildItem -Force D:\Prop\services` → **0 children**. `Test-Path D:\Prop\services\ml-service` → **False** |
| Compose / env | `docker-compose.yml` (postgres, redis, api only). `.env.example` L108 `FEATURE_ML_SCORING_ENABLED=false` (non-architecture name, unused by product C#/TS) |
| Prior law | A26 §5.2 / §6.8, A28 Phase 6, A30 I6, A52, A57, A63 §7.2, A80, A104, B39, B20, B31, C08 |

SHA-256 of files cited as evidence (PowerShell `Get-FileHash -Algorithm SHA256`):

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) |
|---|---:|---:|---|---|
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 41 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38 |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 41 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38 |
| `D:\Prop\apps\web\src\pages\ScoringPage.tsx` | 1288 | 32 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` | 2026-08-18T13:16:43 |
| `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` | 1592 | 32 | `6CAE0FC902D8DFDB5AAC974564D918602EBD3D780C5FAA272BBEF281B19E406D` | 2026-08-18T13:16:00 |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 42 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00 |
| `D:\Prop\apps\api\Program.cs` | 4658 | 86 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | 2026-08-18T13:22:04 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 2577 | 89 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` | 2026-08-18T13:09:51 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 7407 | 150 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | 2026-08-18T13:14:18 |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | 151 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 2026-08-18T13:12:48 |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | 652 | 17 | `48E4C10B5E5A356DA5BB824A32D0A4C857AA2208FA9E4EDE7D145BCCB401ECBA` | 2026-08-18T13:08:41 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 8143 | 187 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 2026-08-18T13:08:10 |
| `D:\Prop\docs\scoring.md` | 327 | 6 | `91558CDB4F153379DE5234812116A7EC2569927B9BF45D03F86D4176E0225889` | 2026-08-18T13:20:12 |
| `D:\Prop\docker-compose.yml` | 687 | 27 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | 2026-08-18T13:18:40 |
| `D:\Prop\.env.example` | 3408 | 103 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | 2026-08-18T13:06:45 |

---

## 2. What architecture §46 actually requires

### 2.1 Nav label, not a widget contract

Architecture `# 46. React Dashboard` / `Main navigation` (verbatim, L1741–1758):

```text
Overview
Brokers
MT5 Groups
Traders
Trader Detail
Trade Explorer
Scoring
Models
Shadow Portfolio
Live Copy Portfolio
cTrader FIX
Risk
Reconciliation
System Health
Audit
Settings
```

`Models` is item 8. The next numbered headings are **§47 Overview … §54 Reconciliation**. There is **no** `# Models Page` widget list. Trade Explorer / Scoring / Models / Shadow / Live / Health / Audit / Settings are named in §46 and specified later (or not) by sibling sections and by A26.

A62 already recorded this split (A62 L188–195):

| §46 label | Route | First-useful? |
|---|---|---|
| Scoring | `/scoring` | Yes (baseline summary; no weights) |
| Models | `/models` | **Nav yes, promote later.** Empty list is honest. No self-promotion. |

So §46 is a **future leaf** for §21 evaluation, not a Phase 3 operating page.

### 2.2 Where Models is actually specified

| Layer | Where | What it is | When |
|---|---|---|---|
| Objective / label | §19 | XGBoost on trade-#3 features → future copy P&L | Phase 6 |
| Leakage | §20 | As-of t3 only; chronological split | Phase 6 |
| Evaluation | §21 | Top 1/5/10/20% vs baselines; **only justified if it beats them OOS** | Phase 6 |
| Tables | §45 / A20 | `model_versions`, `model_predictions`, `model_evaluations` | Phase 6 (A30: **do not create** in I0–I9) |
| Promote verb | §59 | SuperAdmin only; audited | Phase 6 |
| Page + API | A26 §5.2 / §6.8 / §9 | `/models`, `GET /api/v1/models`, `POST …/promote` | Later catalog, **not** A63 v1 |
| Service | §66 / A104 / B39 | `/services/ml-service` | Closed; empty `services/` is preferred |
| Phase gate | §67 Phase 6 / A28 | Dataset, split, XGBoost, calibration, top-N, vs baseline | After Phase 5 **and** data quality |
| First useful | §69 / A57 | **Does not need ML** | Now |

A26 is a **full-dashboard** spec. A63 is the **implementable first-useful** subset. A63 §7.2:

| Path | Why |
|---|---|
| `GET /api/v1/models` + promote | **Phase 6** |

Precedence when they disagree: **§69 + A63 + A52 win for what to ship now.** A26 §6.8 remains the **later** contract. It is not a license to greenwash a model table.

---

## 3. Phase 6 is closed (six independent holds)

Any one of these is enough to keep a working Models page off the tree. All six are true today.

| # | Hold | Evidence |
|---|---|---|
| 1 | §1 / §18: do not use ML first | Production ranker is C# `BaselineScorer` (`risk` / `behavior` / `early_quality`). Not XGBoost. |
| 2 | §67 / A28: Phase 6 after Phase 5 **and** proven data quality | First useful bar is **0/12** (A57). Shadow labels, as-of snapshots, chronological split hashes do not exist. All six §67 Phase 6 delivers remain `[ ]`. |
| 3 | §69 / A57: first useful version **does not need ML** | A57 L414: “No Models / Live Copy / Audit nav items (**acceptable to omit Models/Live for §69**)”. |
| 4 | A30 I6: do not add Models as a **working** page | A30 L775. Stop after I9. Do not create `model_*` tables or `services/ml-service/**`. |
| 5 | A52 / B39: Phase 6 hold; `services/` empty | B39: `child_count=0`. This pass reconfirmed. No Python, no booster, no MLflow. |
| 6 | A63: `/models` out of v1 | Shipping `GET /models` + promote now would expand the first-useful API against catalog law. |

A28 Phase 6 exit: if ML does not beat the baseline out of sample, **the baseline remains the production scorer**. There is no baseline-beat evidence because there is no dataset. A Models page that listed a `PROMOTED` row would be a lie.

---

## 4. Measured product surface (2026-08-18T13:28:04+05:30)

### 4.1 Pages on disk (authoritative)

`D:\Prop\apps\web\src\pages\` — **15** files, alphabetical:

```text
AuditPage.tsx
BrokersPage.tsx
FixSessionsPage.tsx
GroupsPage.tsx
LiveCopyPage.tsx
OverviewPage.tsx
ReconciliationPage.tsx
RiskPage.tsx
ScoringPage.tsx
SettingsPage.tsx
ShadowPortfolioPage.tsx
SystemHealthPage.tsx
TradeExplorerPage.tsx
TraderDetailPage.tsx
TradersPage.tsx
```

`Test-Path D:\Prop\apps\web\src\pages\ModelsPage.tsx` → **False**.  
Recursive `Models*` under `D:\Prop\apps\web` excluding `node_modules` → **none**.

This matches C08’s 15-file census. B22’s 13-file snapshot is **stale** (Live + Audit landed). Models was **never** added in that wave either.

### 4.2 Router and sidebar

`App.tsx` imports 15 pages and mounts 15 routes. Routes present:

```text
/ → /overview
/overview /brokers /groups /traders
/traders/:brokerId/:login
/trades /scoring /shadow /live /fix /risk
/reconciliation /health /audit /settings
```

**No** `path="models"`. **No** `path="login"`. Unknown paths render the layout with an empty `<Outlet />` (A26 “unknown → `/overview`” is still not implemented; that is a **router** remainder, not a Models defect).

`DashboardLayout` nav = **14** links. Labels are abbreviated vs §46 (`Groups`, `Trades`, `Shadow`, `Live`, `FIX`, `Recon`, `Health`). **Models is not among them.** Scoring **is**.

### 4.3 Web search (zero Models surface)

`Select-String` on `apps/web/src/**/*.ts{,x}` for:

```text
ModelsPage
/models
useModels
modelVersion
PROMOTE_MODEL
```

**Zero hits.** ScoringPage copy is the only ML-adjacent UI (see §5).

`hooks.ts` (11 hooks): overview, brokers, groups, traders, trader detail, trades, fix sessions, risk, reconciliation, health, settings. **No** models / promote / scoring-summary hook.

`types/index.ts` `Trader` interface has **no** `mlProbability` field. The orphan types file is unused by pages (B20 / B29). `TraderDetailPage` reads `data.mlProbability` off the live JSON anyway.

### 4.4 API

`Program.cs` mapped endpoints (complete):

| Method | Path | Models? |
|---|---|---|
| GET | `/health`, `/api/health`, `/ready` | no |
| GET | `/api/overview`, `/api/brokers`, `/api/groups` | no |
| GET | `/api/traders`, `/api/traders/{broker}/{login}` | trader rows; `MlProbability` field exists on DTO |
| GET | `/api/trades`, `/api/fix/sessions`, `/api/risk`, `/api/risk/status` | no |
| GET | `/api/reconciliation/status`, `/api/settings` | no |
| POST | `/api/ops/resync` | ingest + baseline rescore |

**Zero** of: `/api/v1/models`, `/api/models`, `/api/v1/models/{id}/promote`, `/api/scoring/summary`.

`TraderRowDto` (DashboardModels.cs L43–57) includes `decimal? MlProbability`. That is the **nullable slot** A26 requires. `EfDashboardQueries.GetTradersAsync` passes **literal `null`** as the 7th constructor argument (file L93–107). There is no column, no join, no service call.

JSON name is `mlProbability` under the default camelCase serializer. `TraderDetailPage` L20:

```tsx
<Info k="ML probability" v={data.mlProbability ?? 'not trained'} />
```

Honest value is always the fallback **`not trained`**. A104 prefers the phrase **“Phase 6 not open”** / no production model. The current string slightly implies a training attempt. That is **copy**, on an existing page — **not** a reason to add `ModelsPage`. This report does not edit it.

### 4.5 Persistence and entities

`TraderDbContext` maps 20 sets. None of:

```text
model_versions
model_predictions
model_evaluations
trader_feature_snapshots
```

`D:\Prop\src\Domain\Entities` (20 files) has `TraderScore` / `TraderScoreHistory` and **no** `ModelVersion` / `ModelPrediction` / `ModelEvaluation`.

`TraderScore` fields: `RiskScore`, `BehaviorScore`, `EarlyQualityScore`, `CompletedXauTrades`, martingale / averaging / lot-escalation flags, `CurrentState`, `LastScoredAt`. **No** `MlProbability`.

A29 C09 / D20 calling `model_*` **MISSING** is a **proposed later table**, not a Phase 0–5 schema fail. A30 explicitly forbids creating those tables in the first-useful sequence.

### 4.6 Python / compose / flags

| Check | Result | Class |
|---|---|---|
| `D:\Prop\services` | Empty directory, created 2026-08-18 12:54:14 +05:30 | **EXISTS_AND_GOOD** (A104 state A, B39) |
| `services/ml-service` | Absent | Preferred pre–Phase 6 |
| Default compose | `postgres`, `redis`, `api` only | **EXISTS_AND_GOOD** vs A65 “never start ml-service” |
| `.env.example` L108 | `FEATURE_ML_SCORING_ENABLED=false` | Non-architecture name (A75). Unused by product C#/TS. **False** does not open Phase 6. Do not flip it. |
| `docs/scoring.md` | “ML is Phase 6 and must beat this baseline out of sample.” | Correct one-screen hold |

---

## 5. Scoring is not Models (Phase 0–5 substitute)

| Surface | Scoring (`/scoring`) | Models (`/models`) |
|---|---|---|
| Architecture | §18, §22, §46, §69.7–8 | §19–21, §46 label, §67 Phase 6 |
| First useful? | **Yes** (deterministic rank) | **No** (A57 / A63) |
| On disk | `ScoringPage.tsx` (1288 B) | **none** |
| Route / nav | `/scoring`, sidebar **Scoring** | none |
| Engine | `BaselineScorer` | would be XGBoost + calibration |
| Outputs | `earlyScore` / `behaviorScore` / `riskScore` / `state` | `modelVersionId`, `status`, `beatsBaselineOutOfSample`, `promotedAt` |
| Mutation | none | SuperAdmin `PROMOTE_MODEL` (forbidden to automate, §71) |

`ScoringPage.tsx` title and helper (L6–7):

```text
Deterministic scoring
XGBoost is not active. ML must beat this baseline out of sample before it is used.
```

That is the **correct** operator message. Do **not** merge Scoring into a fake Models page. Do **not** rename Scoring to Models.

---

## 6. File census vs design (do not conflate)

Prior reports measured the missing **file**. They were right about the tree. They are easy to misread as a build ticket.

| Report | What it correctly measured | What it must not be used for |
|---|---|---|
| A29 U06 | Trade explorer / Scoring / Models all missing (early snapshot) | Trade explorer + Scoring **exist now**. Models remainder is Phase 6. |
| A62 | Scaffold plan: “Nav yes, promote later” | Creating `pages/models/ModelsPage.tsx` **now** is optional chrome, not I6 exit. |
| B20 | 15 pages; Models is the only missing **§46 page module** | Not a §69 FAIL. Widget depth on other pages is a different question. |
| B22 | 13 files (stale); listed Models among 4 never-created | Live + Audit landed. Models still absent **on purpose**. |
| B31 | Models: no nav, no route, no file, no hook, no API. Scoring ≠ Models | “Honest gap: the **nav leaf** is missing” = **optional** placeholder, not a required page. |
| C08 | 15/15 imports; Login + Models never written | Login is **not** Phase 6. Models **is**. Do not lump them. |
| **C39 (this file)** | Same census + Phase 6 / A63 / A57 law | Working Models is **forbidden** until Phase 6 exit inputs exist. |

### 6.1 Login is a different remainder

| Item | Models | Login |
|---|---|---|
| Architecture home | §46 label + §21 / Phase 6 | §59 (not in the §46 list) |
| A26 route | `/models` | `/login` |
| First useful? | **No** (A63 out) | Auth is in the A26/A51/A57 operating bar |
| Classification | **MISSING_BY_DESIGN** | Still **MISSING** as a product page (out of this report’s fix scope) |

Do **not** cite this file to skip Login.

### 6.2 Live Copy is also not Models

Live Copy is Phase 8 / `REAL_COPY_EXECUTION_ENABLED`. A stub page **exists**. Models has **no** stub. Both are correctly non-working in v1. Only Models is a Phase 6 (ML) hold.

---

## 7. What a later wave may add (and must not)

### 7.1 Legal before Phase 6 opens (optional, not §69)

A104: *“Models page (if present) shows **‘Phase 6 not open’** / no production model. Do not draw a sparkline of fake probabilities.”*

If someone adds chrome later, the **maximum** legal surface is:

```text
Nav label: Models          (exact §46 string)
Route:     /models
GET:       404 or { "data": [], "phaseState": "PHASE_6_CLOSED", "reason": "ML_NOT_IN_USE" }
UI:        “Phase 6 not open. Production scorer is baseline.v1. No production model.”
Promote:   hidden / not routed
mlProbability on traders: still null
```

That placeholder is **not** required. A57 already blesses omitting the nav item. This report does **not** authorize creating it.

### 7.2 Forbidden now (policy FAIL even if it compiles)

1. `ModelsPage.tsx` that lists invented `TRAINED` / `PROMOTED` rows.
2. `GET /api/v1/models` returning a demo `xgb-xau-early3` object (A26 §6.8 sample is **illustrative**, not seed data).
3. `POST /models/{id}/promote` (no model exists; self-promotion stays forbidden even after Phase 6).
4. Non-null `mlProbability` (literal, random, 0.5, last source PnL, or stub FastAPI `/predict`).
5. `model_versions` / `model_predictions` / `model_evaluations` tables or EF types.
6. `services/ml-service` training loop, XGBoost lockfile, booster files, trading-DB credentials.
7. Flipping `FEATURE_ML_SCORING_ENABLED=true` (not architecture; A75 / A104).
8. Calling the C# `BaselineScorer` a “model” on a Models page.
9. DNN / LLM / RL as a stand-in (A80 / §71). Phase 6, when it opens, is still trees + calibration + top-N economics.
10. Treating A26 §6.8 or §46’s nav list as a Phase 3 coding ticket.

### 7.3 Legal only after Phase 6 actually opens

A28 / A52 / A30 §17 — **do not start** until Phase 5 exit, frozen leakage-checked extract, split hashes, and a written plan to beat `baseline.v1` on locked top-N **copy** economics (not source PnL).

Then, and only then:

```text
services/ml-service/pyproject.toml          (+ xgboost / sklearn / polars)
migrations for model_versions / predictions / evaluations
GET /api/v1/models                          (real rows)
POST /api/v1/models/{id}/promote            (SuperAdmin + confirmPhrase + audit)
Models page                                 (version table, metrics, no self-promote)
mlProbability                               filled only for a human-promoted model
```

If the challenger loses OOS, **baseline stays production** and the page must keep saying so.

---

## 8. Direct answers

### Is the architecture §46 Models page missing?

**Yes, as a file / route / nav / API.** Measured: no `ModelsPage.tsx`, no `/models`, no `useModels`, no `GET /models`.

### Is that a defect to close in the first useful version?

**No.** Phase 6 is closed. §69 does not include ML. A63 lists `/models` as out of v1. A30 forbids a working Models page in I0–I9. A57 explicitly allows omitting the nav item.

### What is the §73.B class?

| Surface | Class |
|---|---|
| Working Models page + promote + `model_*` + ml-service + non-null `mlProbability` | **EXISTS_AND_GOOD (absence)** while Phase 6 is closed |
| Honest “Phase 6 not open” placeholder | Optional; current absence is **legal** |
| `/scoring` + `BaselineScorer` + null `MlProbability` slot | Present substitute (depth vs A22 is B12 / C02, not this file) |

Calling the working surface **MISSING** as a §69 fail is a **misclassification**. C08/B20/B31 stay valid as inventory.

### Did this pass change product source?

**No.**

---

## 9. What later waves must not do with this file

1. Do **not** create `ModelsPage.tsx` from this report.
2. Do **not** add `/models` or promote endpoints to `Program.cs`.
3. Do **not** add `model_*` entities to `TraderDbContext`.
4. Do **not** create `services/ml-service` to “fill the gap.”
5. Do **not** fill `mlProbability` so Trader Detail looks complete.
6. Do **not** reopen Phase 6 because §46 lists the word `Models`.
7. Do **not** treat Login, Live Copy, or Audit as covered by this hold.
8. Do **not** overwrite Scoring and call it Models.

---

## Evidence pins

- Architecture §46 nav: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1735–1758. Phase 6: L2558–2569. §69: L2633–2654. §21: L885–921.
- Router: `D:\Prop\apps\web\src\App.tsx` SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` L5–20 (14 links; no Models).
- Pages: `D:\Prop\apps\web\src\pages\` — 15 files; `ModelsPage.tsx` absent. Measured 2026-08-18T13:28:04+05:30.
- API: `D:\Prop\apps\api\Program.cs` — no models map. `EfDashboardQueries` L100 literal `null` for `MlProbability`.
- Services: `Get-ChildItem -Force D:\Prop\services` → 0 children (reconfirmed; B39).
- Law: A52, A28 Phase 6, A30 L775, A57 L414, A63 §7.2, A104 L233, A80, B39.
- Census (not class): C08, B20, B31.

---

## 10. One-sentence close

**Architecture §46 lists Models as a future dashboard leaf for §21 / Phase 6; the page is absent by design while Phase 6 is closed, the first-useful scorer is the C# baseline, `mlProbability` stays null, and no product file should be added to “complete” that nav item.**
