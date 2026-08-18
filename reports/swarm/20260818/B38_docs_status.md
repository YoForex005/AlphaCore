# B38 — `D:\Prop\docs` status vs architecture §66

| Field | Value |
|---|---|
| Agent | B38 (docs status only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:20:55+05:30 (newest file `architecture.svg` LastWrite) |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\docs\` (directory listing + content vs §66 / A66) |
| Product source modified | **No.** This report is the only write. |
| Method | `list_dir` on `D:\Prop\docs`; PowerShell `Get-ChildItem` + `Get-FileHash SHA256` + line/byte census; full read of every `docs/*` file; compare to architecture §66 and `A66_docs_outline.md`; spot-check as-built claims against `src/`, `apps/`, `README.md` |
| Precedence | On-disk `docs/` supersedes A66 §1 “disk state” (A66 saw only `architecture.md`). Architecture v2 §66 remains the required file list. A66 is the writing-quality bar, not a substitute for the files. |

---

## 0. Verdict

**`D:\Prop\docs` is not empty and is not §66-complete.**

`list_dir` returned **7** files: **6** Markdown stubs named in §66 plus **1** extra SVG. **5 of 11** required Markdown files are **missing**. Every present Markdown file is a **short note**, not the A66 operational doc. None of the eleven files is `CURRENT` against A66’s done-checks.

| Question | Count | Answer |
|---|---|---|
| Files under `D:\Prop\docs` | **7** | 6 `*.md` + 1 `*.svg` |
| §66 required Markdown files | **11** | quoted in architecture v2 lines 2458–2469 |
| §66 files present on disk | **6** | architecture, trade-reconstruction, xauusd-normalization, scoring, risk, ctrader-fix |
| §66 files missing | **5** | `mt5-integration.md`, `ml.md`, `shadow-copy.md`, `reconciliation.md`, `deployment.md` |
| Extra file not in §66 | **1** | `architecture.svg` (linked from `README.md`) |
| Present file that meets A66 “done when” | **0** | all six are stubs (4–21 non-blank lines) |
| Secrets / live passwords in `docs/` | **0** | no `1369850`, no FIX/MT5 passwords |
| v2 spec cloned into `docs/` | **No** | v2 remains only at repo root |
| A66 “only `architecture.md` exists” snapshot | **Stale** | five more `*.md` plus SVG landed later the same day |

**Do not treat the six files as discharged documentation.** Existence ≠ completeness. Do **not** invent the five missing files from this report. Do **not** rewrite product `docs/` in this agent.

A66 §3 called the *previous* `docs/architecture.md` **MISSING/UNSAFE** (API-gateway fiction, “Phase 1 Done”). That file has been **replaced**. The replacement is **honest and short**, still **not** the §66 architecture map.

---

## 1. Method

| Source | Path / action |
|---|---|
| Directory listing | `list_dir` `D:\Prop\docs` — **7** names below |
| File census | PowerShell `Get-ChildItem -Force` + `Get-FileHash SHA256` + `Measure-Object -Line` (non-blank) |
| Content | Full read of all 6 `*.md` and `architecture.svg` |
| Binding file list | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §66 (lines 2458–2469) |
| Quality bar | `D:\Prop\reports\swarm\20260818\A66_docs_outline.md` |
| As-built spot-check | `src/Domain`, `src/Application`, `src/Fix.CTrader`, `apps/*`, `README.md` |
| Other `docs/` trees | recursive `*.md` under `D:\Prop` whose path contains `\docs\` — only this folder plus vendor CHMs under `mt5-sdk\vendor\MetaTrader5SDK\Docs\` |

No `dotnet`, no `npm`, no product edit, no new `docs/*.md`.

---

## 2. `list_dir` result (authoritative)

`D:\Prop\docs\` contains **exactly** these 7 files (as returned by `list_dir`):

```text
architecture.md
architecture.svg
ctrader-fix.md
risk.md
scoring.md
trade-reconstruction.md
xauusd-normalization.md
```

No subfolders. No `README.md`. No `index.md`. No drafts. Attributes: `Archive` only.

Empty-directory check: **FAIL** (directory has content). File count: **7**.

There is **no** second product `docs/` under `D:\Prop\src`. Vendor CHMs at `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Docs\` are MetaQuotes SDK help, **not** §66 product docs.

---

## 3. File census (measured)

| Name | Bytes | Non-blank lines | Total lines (file) | SHA-256 | LastWrite (local) |
|---|---:|---:|---:|---|---|
| `architecture.md` | 1379 | 21 | 28 | `A5FB4FEFD9EFECDDCECDD884D1F1FA2042658AB06989F2155BF35B67BBFE5B3D` | 2026-08-18T13:18:40.1390945+05:30 |
| `architecture.svg` | 2753 | 42 | 53 | `98A969AEF3E9A808DF838AD538D08463937A26A0F5323F58FE4BCCEB8FEFC43C` | 2026-08-18T13:20:55.0605436+05:30 |
| `ctrader-fix.md` | 686 | 13 | 17 | `9CA8063974C24C64D5522162189E02C85C4B63B66A35A1C5E3135A3CA709621D` | 2026-08-18T13:18:40.1355792+05:30 |
| `risk.md` | 404 | 6 | 11 | `01B1EE832264CFCE11EC52E15F871BCBD49592583B39E5F186D5288872DDCD62` | 2026-08-18T13:20:12.5799960+05:30 |
| `scoring.md` | 327 | 6 | 11 | `91558CDB4F153379DE5234812116A7EC2569927B9BF45D03F86D4176E0225889` | 2026-08-18T13:20:12.5779925+05:30 |
| `trade-reconstruction.md` | 545 | 9 | 11 | `8CD041EB044F943818A01DFAE363E4B281E9D12B8FA6F0CF3625BCAEDED8A2A3` | 2026-08-18T13:18:40.1390945+05:30 |
| `xauusd-normalization.md` | 263 | 4 | 7 | `28C228F1EB089D718FB7DBE3E8556FC51EC145BB4167F0675F905EAD39F5989A` | 2026-08-18T13:20:12.5799960+05:30 |

**Totals:** 6357 bytes on disk; 3604 bytes of Markdown; 59 non-blank Markdown lines across six files.

All six Markdown timestamps cluster 13:18:40–13:20:12. The SVG is two minutes later and is what `README.md` embeds.

---

## 4. §66 coverage matrix

Quoted required tree (architecture v2 §66):

```text
/docs
  architecture.md
  mt5-integration.md
  trade-reconstruction.md
  xauusd-normalization.md
  scoring.md
  ml.md
  shadow-copy.md
  risk.md
  ctrader-fix.md
  reconciliation.md
  deployment.md
```

| # | Required path | On disk? | Class | One-line |
|---|---|---|---|---|
| 1 | `docs/architecture.md` | **Yes** | **STUB / HONEST MAP** | Rewritten vs A66 §3; ~1 page; missing identity, stack, doc index, is/is-not |
| 2 | `docs/mt5-integration.md` | **No** | **MISSING** | No file anywhere under `D:\Prop` except this required name |
| 3 | `docs/trade-reconstruction.md` | **Yes** | **STUB** | 9 non-blank lines; first-3 + deal-entry rules only |
| 4 | `docs/xauusd-normalization.md` | **Yes** | **STUB** | 4 non-blank lines; no mapping tables, no qty chain |
| 5 | `docs/scoring.md` | **Yes** | **STUB** | 6 non-blank lines; no formulas, no state vocab table |
| 6 | `docs/ml.md` | **No** | **MISSING** | Even the “do not train until §69” stub is absent |
| 7 | `docs/shadow-copy.md` | **No** | **MISSING** | Shadow lives only in Domain + A24 |
| 8 | `docs/risk.md` | **Yes** | **STUB** | 6 non-blank lines; no limits table, no RBAC, no flags matrix |
| 9 | `docs/ctrader-fix.md` | **Yes** | **STUB** | 6 rules + official URLs; no header map, no message set, no harness |
| 10 | `docs/reconciliation.md` | **No** | **MISSING** | Startup/periodic reconcile runbook not written |
| 11 | `docs/deployment.md` | **No** | **MISSING** | Topology / secrets / flags not written as a product doc |

**Score:** 6/11 exist as files. **0/11** meet A66 completeness. **5/11** missing.

Optional A66 non-§66 index `docs/README.md`: **absent**. Root `README.md` already points at the v2 spec, `docs/architecture.md`, and the SVG.

---

## 5. Per-file quality vs A66

A66 required shared front-matter (title, last-updated, owner, source-of-law §, related code, related swarm spec, `DRAFT`/`CURRENT`/`SUPERSEDED`) on **every** file. **None** of the six Markdown files has that front-matter.

### 5.1 `architecture.md` — STUB / HONEST MAP

A66 said the *old* file was unsafe (API Gateway, account provisioning, “prop challenge → live”, Phase 1 Done, C++17-as-the-architecture, news filter). **Those sentences are gone.** Current file:

- Declares source of truth = repo-root v2 spec.
- Table maps layers to `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Mt5` + `mt5-sdk`, `src/Fix.CTrader`, `apps/api`, `apps/mt5-worker`, `apps/fix-worker`, `apps/web`.
- Safety defaults: `REAL_COPY_EXECUTION_ENABLED=false`; trade #3 → SHADOW / EARLY_SCORE never LIVE; `TargetCompID = cServer`; volume scale `10_000`; plan-group mappings are labels.
- Phases: “toward first useful version (architecture §69)”; live TRADE send and ML “explicitly not enabled.”

Spot-check vs tree (honest enough, with one overclaim):

| Claim | Tree fact |
|---|---|
| Domain has recon / symbol / baseline / risk / shadow / FIX FSM | **True** — `Reconstruction/`, `Instruments/`, `Scoring/`, `Risk/`, `Shadow/`, `Execution/ExecutionOrderStateMachine.cs` |
| Application “broker ports, ingestion, scoring, dashboard contracts” | **Partial** — `Contracts/Mt5Contracts.cs`, `Ingestion/DealIngestionService.cs`, `Dashboard/DashboardModels.cs`. Scoring is **not** an Application project; it is Domain |
| Persistence EF + in-memory + demo seed | **True** — `TraderDbContext`, `DemoSeeder` |
| Fake MT5 + C++ SDK preserved | **True** — `src/Mt5/Connectors/FakeMt5BrokerConnector.cs`, `mt5-sdk/` |
| Real NewOrderSingle off | **True** — no send path; `CTraderFixOptions` default off; worker logs refuse |
| Volume default 10_000 | **True** — `VolumeConverter.ManagerVolumeScale` |
| `cServer` case preserved | **True** — `CTraderFixOptions.TargetCompId` default `"cServer"`; seeder + harness |

A66 file-1 must-include still **missing**:

- What the system **is / is not** (not an LP; not a password console; not MT5→ML→FIX in one hop).
- Pipeline diagram in the Markdown (the SVG is a sibling, not linked from this file).
- Identity law (`broker_id + login`).
- Tech-stack table from §5.
- Index of the other ten `docs/*.md`.
- Length target 4–8 pages (this is ~28 lines).

**Done when (A66):** a new hire can name the pipeline, find the code, find v2, and open the correct specialist doc. **Partial.** Pipeline is in the SVG + one table; specialist docs are stubs or missing.

### 5.2 `trade-reconstruction.md` — STUB

States: reconstructed trade = position lifecycle, not one deal; count `Buy`/`Sell`; `In` / `Out` / `OutBy` / `InOut`; first 3 = first 3 **completed XAUUSD** reconstructed trades; volume `native / 10_000`; MFE/MAE omitted unless `FeatureQuality.Exact`.

**Correct direction.** Missing vs A66 §6: `ReconstructedTrade` field list, lifecycle table, ordering `ORDER BY closed_at, opened_at, id`, input = normalized deals not dashboard aggregates, `completed=false` exclusion, failure metric, required tests, tables.

**Must-not** is honored: no score formulas, no “deal_count==3 means three trades.”

### 5.3 `xauusd-normalization.md` — STUB

Four sentences: canonical `XAUUSD`; aliases `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `GOLD`, dotted/pro suffixes; cTrader IDs discovered via Security List; never hardcoded from another account.

**Correct invariants.** Missing: two mapping tables, **unmapped policy** (no `"XAU"`/`"GOLD"` substring guess — aliases listed here could be misread as “these strings always map”), volume/qty conversion chain (§38), price-source / `feature_quality`, operator/RBAC procedure, never-guess tag 55 as the *string* `XAUUSD`.

### 5.4 `scoring.md` — STUB

Trade #3 ⇒ `EARLY_SCORE_ELIGIBLE`; three score outputs; high quality + low risk ⇒ `SHADOW` never `LIVE`; martingale / sequential size-up ⇒ `RISK_BLOCKED`; ML is Phase 6 and must beat baseline OOS.

**No ML claims. Trade #3 does not fund live capital.** Missing: feature list, full §22 state vocabulary (`INSUFFICIENT_DATA` … `DISQUALIFIED`), as-of/leakage, authority (risk is final), `ScoreConfig` / `baseline.v1`, tables, tests, pointer to A22 as formula lock.

`CanPromoteToLive` in Domain is hard-`false`; the stub does not say that.

### 5.5 `risk.md` — STUB

“Scoring proposes. Risk decides.” Lists reject families; `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN`; reduce/close more permissive than open/increase; FIX send needs `REAL_COPY_EXECUTION_ENABLED=true` **and** `AllowFixSend`.

**Correct vocabulary.** Missing: production flow (never FIX from MT5 callback), hard-limits table with config keys, pre-trade price guards, full §41 flag block (`CTRADER_FIX_*` trio), intent expiry / no catch-up, correlation reserved, failure rules, RBAC, tables, tests.

B13 already measured `RiskEngine` as an unwired stub. This doc does **not** claim the engine is production-complete. It also does **not** warn that Evaluate is unwired.

### 5.6 `ctrader-fix.md` — STUB

Four official help.ctrader.com URLs + six repo rules: separate QUOTE 5211 / TRADE 5212 sequence spaces; configurable SenderSubID/TargetSubID; never rewrite `cServer`→`CSERVER`; no hardcoded instrument IDs; persist ClOrdID before send; disconnect after send = `EXECUTION_STATE_UNKNOWN`; `REAL_COPY_EXECUTION_ENABLED` defaults false.

**The six rules are the right safety core.** Missing vs A66 §12: venue statement (Pepperstone / host / “not an LP”), QuickFIX/n + **cTrader dictionary** (generic FIX 4.4 is insufficient), single-active TRADE ownership, minimum message set, Security List / QUOTE feed, persist-before-send state machine, unknown-state procedure, source↔dest mapping, harness (§61), metrics, secret-safe config placeholders.

No live account id, no password. Good.

### 5.7 `architecture.svg` — EXTRA (not §66)

900×420 SVG: MT5 sources (Achiever / StarwaveFX) → collectors (`apps/mt5-worker`) → raw events/DB (Postgres / Outbox) → reconstruction → scoring → “Shadow Copy & Risk” → “cTrader FIX Adapter (`src/Fix.CTrader` QUOTE + TRADE)”. Footer: see `docs/` and the v2 spec.

**Aligned with §4 direction.** Collapses CopyIntent / Risk / ExecutionIntent / reconciliation into two boxes. Not a substitute for `architecture.md` or `reconciliation.md`. Referenced by root `README.md` line 5; **not** referenced by `docs/architecture.md`.

### 5.8 Missing files — what is absent

| Missing file | Consequence |
|---|---|
| `mt5-integration.md` | No product runbook for two Manager collectors, group discovery, outbox, compound keys. Engineers must use v2 §6–13 + A04/A07/A12–A18. |
| `ml.md` | No written “do not train until §69.” A52 / A104 exist in swarm reports only. Risk: a later contributor “just ships XGBoost.” |
| `shadow-copy.md` | Destination-quote-only fill rule and no-catch-up policy are not operator-facing. Code: `src/Domain/Shadow/ShadowCopyEngine.cs` + A24. |
| `reconciliation.md` | No startup checklist that blocks `READY_FOR_EXECUTION` on mismatch. A47 is swarm-only. |
| `deployment.md` | No Windows-MT5 vs Linux-services topology, secret handling, or flag matrix as a product doc. Split across README + A54 + A65 + `.env.example`. |

---

## 6. Secrets / greenwash / clone checks

| Check | Result |
|---|---|
| Live FIX account `1369850` in `docs/` | **Absent** |
| MT5 / FIX / DB / Redis passwords | **Absent** |
| Broker manager logins / IPs in `docs/` | **Absent** (those belong in `mt5-integration.md` as non-secret shape only) |
| “Phase 1 Done” / API Gateway / live-by-default | **Absent** from current `docs/` |
| Wholesale paste of v2 §1–75 | **No.** Longest Markdown is 1379 bytes |
| v2 spec relocated into `docs/` | **No.** Still `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| ML claimed as shipped | **No.** `scoring.md` says Phase 6; `architecture.md` says ML not enabled |
| Live NewOrderSingle claimed on | **No.** All present files say flag default false / send not enabled |

---

## 7. A66 §18 acceptance (docs set, not this agent)

Copied from A66. Status **as of this measurement**:

```text
[x] docs/architecture.md rewritten (no longer the A66 §3 unsafe file); still not the full §66 map
[ ] docs/architecture.md no longer incomplete vs A66 file-1 must-include
[ ] docs/mt5-integration.md exists
[x] docs/trade-reconstruction.md exists (stub; not done)
[x] docs/xauusd-normalization.md exists (stub; not done)
[x] docs/scoring.md exists (no ML claims; not done)
[ ] docs/ml.md exists (at least the “do not train until §69” stub)
[ ] docs/shadow-copy.md exists
[x] docs/risk.md exists (stub; not done)
[x] docs/ctrader-fix.md exists (QUOTE section not complete)
[ ] docs/reconciliation.md exists
[ ] docs/deployment.md exists
[x] none of the *present* files contain real secrets
[x] none of the *present* files is a paste of the v2 spec
[x] v2 spec remains only at
    D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
```

**§66 documentation obligation is not discharged.**

---

## 8. Stale swarm claims

| Prior claim | Now |
|---|---|
| A66 §1: “Only `docs/architecture.md` is on disk; the other ten files do not exist.” | **Stale.** Five additional `*.md` exist as stubs. Five still missing. |
| A66 §3: current `architecture.md` is the API-gateway / Phase-1-Done document | **Stale.** File was rewritten (SHA-256 `A5FB4FEF…`). Treat as honest stub, not the old unsafe text. |
| A11 “10 named docs” | Still **wrong** (A66). Count is **11**. |

Do not recreate files that exist. Do not mark stubs `CURRENT`.

---

## 9. Recommended next writes (docs task, not this agent)

A66 write order, updated for what is already stubbed:

| Order | File | Action |
|---|---|---|
| 1 | `architecture.md` | Expand the honest map: is/is-not, identity, stack, index of 10 siblings, link the SVG. Do not restore the old fiction. |
| 2 | `deployment.md` | **Create.** Topology, secrets, flags, first-useful-version pointer. |
| 3 | `mt5-integration.md` | **Create.** |
| 4–6 | recon / xauusd / scoring | **Thicken** existing stubs; do not replace with a second file. |
| 7 | `shadow-copy.md` | **Create.** |
| 8 | `risk.md` | Thicken; keep kill-switch split. |
| 9 | `ctrader-fix.md` | Thicken QUOTE first. |
| 10 | `reconciliation.md` | **Create.** |
| 11 | `ml.md` | **Create the forbid-training stub now** so absence cannot be read as “ML unspecified.” |

Keep `architecture.svg`. Optionally add a 20-line `docs/README.md` that **only** points at v2 + the 11 files. Do not create `docs/dashboard.md` (A66 §17).

---

## 10. What this agent did not do

- Did not edit any file under `D:\Prop\docs`, `D:\Prop\src`, `D:\Prop\apps`, or the v2 spec.
- Did not author missing `docs/*.md`.
- Did not run tests or builds.

**Deliverable:** `D:\Prop\reports\swarm\20260818\B38_docs_status.md` only.
