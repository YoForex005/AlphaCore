# C11 — `D:\Prop\docs` vs architecture §66 required docs

| Field | Value |
|---|---|
| Agent | C11 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C11_docs_gap.md` |
| Target | `D:\Prop\docs` listed and compared to architecture **§66** `/docs` file list |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` heading `# 66. Suggested Repository Structure` (lines 2423–2470) |
| Quality bar (not law) | `D:\Prop\reports\swarm\20260818\A66_docs_outline.md` — write plan / done-when; v2 wins on conflict |
| Prior snapshot | `D:\Prop\reports\swarm\20260818\B38_docs_status.md` — **stale on extras** (no `architecture.png`; older SVG hash). Markdown set is the same. |
| Product source modified | **No.** This report is the only write. `D:\Prop\docs`, `D:\Prop\src`, `D:\Prop\apps` were not edited. |
| Classification | Architecture §73.B |

---

## 0. Verdict

**`D:\Prop\docs` exists and is not empty. It does not satisfy architecture §66.**

§66 names **eleven** Markdown files. **Six** of those names are on disk as **short stubs**. **Five** required names are **absent** anywhere under `D:\Prop`. **Zero** of the eleven meet the A66 operational “done when” bar.

Two extra files (`architecture.svg`, `architecture.png`) sit in the same folder. They are **not** in the §66 list. The PNG is a Pillow **placeholder**, not a rendered SVG.

Do not greenwash “docs started” as “§66 docs complete.” Existence of a filename is not a product document.

| Question | Measured answer |
|---|---|
| Files in `D:\Prop\docs` | **8** (6 `*.md` + `architecture.svg` + `architecture.png`) |
| §66 required Markdown files | **11** |
| Required files present | **6 / 11** |
| Required files missing | **5 / 11** |
| Present files that are `CURRENT` vs A66 | **0 / 11** |
| Extra files not in §66 | **2** |
| Secrets / live passwords in `docs/` | **0** |
| v2 spec cloned into `docs/` | **No** |
| Subfolders under `docs/` | **None** |

**§66 documentation obligation: not discharged.**

---

## 1. Method

| Step | Action |
|---|---|
| List | `list_dir` on `D:\Prop\docs` (first pass: 7 names, PNG not yet present) |
| Census | PowerShell `Get-ChildItem -Force` + `Get-FileHash SHA256` + line/non-blank counts (final pass) |
| Content | Full read of all 6 `*.md`, `architecture.svg`; visual read of `architecture.png` |
| Law | Architecture v2 §66 lines 2458–2469 (quoted below) |
| Quality | A66 outlines §2–§18 |
| Name search | Recursive `*.md` under `D:\Prop` for the five missing filenames — **zero hits** |
| Other `docs/` trees | `D:\Prop\docs` (product) and `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Docs` (vendor CHM only) |
| Cross-check | `README.md` embed; `scripts/svg_to_png.py`; Application folder vs `architecture.md` claims |

No `dotnet`, no `npm`, no product edit, no new `docs/*.md`.

---

## 2. Architecture §66 required `/docs` list

Quoted from `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §66:

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

Preamble in the same section: *“Adapt to the existing repo; do not create duplicates unnecessarily.”*

That sentence applies to **code folders**, not to dropping named product docs. Count is **11** Markdown files (A11’s “10 named docs” is still wrong). `docs/architecture.md` is an implementer map that **points at** the repo-root v2 spec; it is not a second copy of the 75-section spec.

§66 does **not** require `architecture.svg`, `architecture.png`, or `docs/README.md`.

---

## 3. `D:\Prop\docs` listing (final measurement)

Authoritative disk list after the PNG landed (`Get-ChildItem -Force`):

```text
architecture.md
architecture.png
architecture.svg
ctrader-fix.md
risk.md
scoring.md
trade-reconstruction.md
xauusd-normalization.md
```

No subdirectories. No `README.md`. No `index.md`. No drafts of the five missing names.

### 3.1 Census

| Name | Bytes | Total lines | Non-blank | SHA-256 | LastWrite (local) |
|---|---:|---:|---:|---|---|
| `architecture.md` | 1379 | 28 | 21 | `A5FB4FEFD9EFECDDCECDD884D1F1FA2042658AB06989F2155BF35B67BBFE5B3D` | 2026-08-18T13:18:40+05:30 |
| `architecture.png` | 12081 | — | — | `0F7BAF6D2461A5A055C83C278FCD0A8F718B3C2B86C19886221FDCB259EC98C9` | 2026-08-18T13:23:59+05:30 |
| `architecture.svg` | 2697 | 52 | 41 | `23F51B89D6CA6FC4A649E9A3F7DC04AFCB42485892D8604E3ACAD18EAFEB4327` | 2026-08-18T13:22:35+05:30 |
| `ctrader-fix.md` | 686 | 17 | 13 | `9CA8063974C24C64D5522162189E02C85C4B63B66A35A1C5E3135A3CA709621D` | 2026-08-18T13:18:40+05:30 |
| `risk.md` | 404 | 11 | 6 | `01B1EE832264CFCE11EC52E15F871BCBD49592583B39E5F186D5288872DDCD62` | 2026-08-18T13:20:12+05:30 |
| `scoring.md` | 327 | 11 | 6 | `91558CDB4F153379DE5234812116A7EC2569927B9BF45D03F86D4176E0225889` | 2026-08-18T13:20:12+05:30 |
| `trade-reconstruction.md` | 545 | 11 | 9 | `8CD041EB044F943818A01DFAE363E4B281E9D12B8FA6F0CF3625BCAEDED8A2A3` | 2026-08-18T13:18:40+05:30 |
| `xauusd-normalization.md` | 263 | 7 | 4 | `28C228F1EB089D718FB7DBE3E8556FC51EC145BB4167F0675F905EAD39F5989A` | 2026-08-18T13:20:12+05:30 |

**Markdown total:** 3604 bytes, 85 lines, **59 non-blank lines** across six files.

Six Markdown files share one write window (13:18:40–13:20:12). SVG is two minutes later. PNG is one minute after the SVG.

B38 measured SVG as 2753 bytes / SHA `98A969AE…` at 13:20:55. Current SVG is **2697** bytes / SHA `23F51B89…` at 13:22:35. Treat B38’s SVG row as superseded. All six Markdown hashes match B38.

---

## 4. Coverage matrix (§66 vs disk)

| # | §66 path | On disk? | §73.B | One-line |
|---|---|---|---|---|
| 1 | `docs/architecture.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | Honest ~1-page map. Missing pipeline-in-md, identity, stack, sibling index, is/is-not. |
| 2 | `docs/mt5-integration.md` | **No** | **MISSING** | No file of this name under `D:\Prop`. |
| 3 | `docs/trade-reconstruction.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | 9 non-blank lines. First-3 + deal-entry bullets only. |
| 4 | `docs/xauusd-normalization.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | 4 non-blank lines. Canonical + aliases + Security List. |
| 5 | `docs/scoring.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | 6 non-blank lines. No formulas, no full state table. |
| 6 | `docs/ml.md` | **No** | **MISSING** | Even the “do not train until §69” stub is absent. |
| 7 | `docs/shadow-copy.md` | **No** | **MISSING** | Policy lives in Domain + A24 only. |
| 8 | `docs/risk.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | 6 non-blank lines. Authority + kill-switch split only. |
| 9 | `docs/ctrader-fix.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | Official URLs + 6 safety rules. No message set / harness. |
| 10 | `docs/reconciliation.md` | **No** | **MISSING** | No startup READY_FOR_EXECUTION runbook. |
| 11 | `docs/deployment.md` | **No** | **MISSING** | Topology / secrets / flags not a product doc. |

**Score:** present **6 / 11**. Complete **0 / 11**. Missing **5 / 11**.

Optional A66 non-§66 `docs/README.md`: **absent**. Root `README.md` already points at the v2 spec and `docs/architecture.md`.

### 4.1 Extra files (not scored against the 11)

| Path | §73.B (as extra) | Note |
|---|---|---|
| `docs/architecture.svg` | Extra / **aligned helper** | §4-direction pipeline boxes. README used to embed this; current README embeds the PNG. |
| `docs/architecture.png` | Extra / **placeholder** | Generated by `D:\Prop\scripts\svg_to_png.py` Pillow fallback after cairosvg failed. Not a §66 deliverable. |

---

## 5. Per-file notes (present Markdown)

A66 required shared front-matter on every file (title, last-updated, owner, source-of-law §, related code, related swarm spec, `DRAFT` / `CURRENT` / `SUPERSEDED`). **None** of the six files has it.

### 5.1 `architecture.md` — EXISTS_NEEDS_REFACTOR

Current text (28 lines) is **not** the A66 §3 unsafe document (API Gateway, account provisioning, “Phase 1 Done”, news filter). Those sentences are gone. SHA `A5FB4FEF…`.

What it does:

- Declares source of truth = repo-root v2 spec.
- Table maps Domain / Application / Infrastructure / Mt5+sdk / Fix.CTrader / api / both workers / web.
- Safety: `REAL_COPY_EXECUTION_ENABLED=false`; trade #3 → SHADOW / EARLY_SCORE never LIVE; `TargetCompID = cServer`; volume scale `10_000`; plan-group mappings are labels.
- Phases: “toward first useful version (§69)”; live TRADE send and ML “explicitly not enabled.”

Spot-check vs tree (this pass):

| Claim | Tree |
|---|---|
| Domain recon / symbol / baseline / risk / shadow / FIX FSM | **True** — matching folders under `src/Domain` |
| Application “broker ports, ingestion, scoring, dashboard contracts” | **Overclaim** — Application has `Contracts/`, `Ingestion/`, `Dashboard/` only. Scoring lives in `src/Domain/Scoring`, not Application |
| Persistence EF + in-memory + demo seed | **True** — `TraderDbContext`, `DemoSeeder` |
| Fake MT5 + C++ SDK preserved | **True** |
| Real NewOrderSingle off | **True** (default / absence of send) |

A66 file-1 still missing: system is/is-not, Markdown pipeline (SVG is a sibling, **not linked** from this file), identity law (`broker_id + login`), §5 tech-stack table, index of the other ten docs, 4–8 page length.

**Done when (A66):** a new hire can name the pipeline, find the code, find v2, and open the correct specialist doc. **Partial.** Specialist siblings are stubs or missing.

### 5.2 `trade-reconstruction.md` — EXISTS_NEEDS_REFACTOR

States: reconstructed trade = position lifecycle, not one deal; count `Buy`/`Sell`; `In` / `Out` / `OutBy` / `InOut`; first 3 = first 3 **completed XAUUSD** reconstructed trades; volume `native / 10_000`; MFE/MAE omitted unless `FeatureQuality.Exact`.

**Direction is correct.** Missing vs A66 §6: `ReconstructedTrade` field list, lifecycle table, ordering `ORDER BY closed_at, opened_at, id`, input = normalized deals, `completed=false` exclusion, failure metric, tables, tests.

Honors must-not: no score formulas; does not treat `deal_count==3` as three trades.

### 5.3 `xauusd-normalization.md` — EXISTS_NEEDS_REFACTOR

Four sentences: canonical `XAUUSD`; aliases `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `GOLD`, dotted/pro suffixes; cTrader IDs discovered via Security List; never hardcoded from another account.

**Invariants are correct.** Missing: `source_symbol_mappings` / `destination_symbols`, **unmapped policy** (listed aliases can be misread as implicit substring maps), §38 qty chain, price-source / `feature_quality`, RBAC for mapping changes, never-guess tag 55 as the string `"XAUUSD"`.

### 5.4 `scoring.md` — EXISTS_NEEDS_REFACTOR

Trade #3 ⇒ `EARLY_SCORE_ELIGIBLE`; three outputs; high quality + low risk ⇒ `SHADOW` never `LIVE`; martingale / sequential size-up ⇒ `RISK_BLOCKED`; ML is Phase 6 and must beat baseline OOS.

**No ML-as-shipped claim. Trade #3 does not fund live capital.** Missing: §18 feature list, full §22 state vocabulary, as-of/leakage, risk-is-final, `baseline.v1`, tables, tests, pointer to A22 as formula lock.

### 5.5 `risk.md` — EXISTS_NEEDS_REFACTOR

“Scoring proposes. Risk decides.” Reject families; `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN`; reduce/close more permissive than open/increase; FIX send needs `REAL_COPY_EXECUTION_ENABLED=true` **and** `AllowFixSend`.

**Vocabulary is correct.** Missing: never-FIX-from-MT5-callback flow, hard-limits table with config keys, §37 price guards, full §41 flag block, intent expiry / no catch-up, correlation reserved, fail-closed, RBAC, tables, tests. Does not warn that `RiskEngine` may still be unwired (B13).

### 5.6 `ctrader-fix.md` — EXISTS_NEEDS_REFACTOR

Four official https://help.ctrader.com/fix/ URLs + six rules: QUOTE TLS 5211 / TRADE TLS 5212 separate sequence spaces; configurable SenderSubID/TargetSubID; never rewrite `cServer`→`CSERVER`; no hardcoded instrument IDs; persist ClOrdID before send; disconnect after send = `EXECUTION_STATE_UNKNOWN`; `REAL_COPY_EXECUTION_ENABLED` defaults false.

**Safety core is right.** Missing vs A66 §12: Pepperstone / not-an-LP venue statement, QuickFIX/n + **cTrader dictionary** (generic FIX 4.4 insufficient), single-active TRADE ownership, minimum message set, Security List / QUOTE feed, persist-before-send FSM, unknown-state procedure, source↔dest mapping, §61 harness, metrics, secret-safe config placeholders.

No live account id `1369850`, no password. Good.

---

## 6. Extra diagrams (not §66)

### 6.1 `architecture.svg`

900×420 SVG: Achiever/StarwaveFX → `apps/mt5-worker` → Postgres/Outbox → Reconstruction → Scoring → “Shadow Copy & Risk” → `src/Fix.CTrader` (QUOTE + TRADE). Footer points at `docs/` and the v2 spec.

Aligned with §4 direction. Collapses CopyIntent / RiskEngine / ExecutionIntent / reconciliation. **Not** a substitute for `architecture.md` or `reconciliation.md`. **Not linked** from `docs/architecture.md`.

### 6.2 `architecture.png`

Visual: light-grey 900×420 canvas, one truncated pipeline sentence (“… → Scori”) and the label **“Architecture diagram (placeholder)”**.

Produced by `D:\Prop\scripts\svg_to_png.py` Pillow fallback (`created placeholder with pillow`) after cairosvg was unavailable. Current `README.md` line 5 embeds **this PNG**, not the SVG.

This file does not discharge any §66 row. It is weaker than the SVG it was meant to rasterize.

---

## 7. Missing required files

Recursive search for these exact filenames under `D:\Prop` returned **no files**:

| Missing | Consequence if left absent |
|---|---|
| `docs/mt5-integration.md` | No product runbook for two Manager collectors, group discovery, outbox, compound keys. Engineers must use v2 §6–13 + A04/A07/A12–A18. |
| `docs/ml.md` | No written “do not train until §69.” A52 / A104 exist only as swarm reports. A later contributor can treat ML as unspecified. |
| `docs/shadow-copy.md` | Destination-quote-only fill and no-catch-up are not operator-facing. Code: `src/Domain/Shadow/ShadowCopyEngine.cs` + A24. |
| `docs/reconciliation.md` | No startup checklist that blocks `READY_FOR_EXECUTION` on mismatch. A47 is swarm-only. |
| `docs/deployment.md` | No Windows-MT5 vs Linux-services topology, secret handling, or flag matrix as a product doc. Split across README + A54 + A65 + `.env.example`. |

Vendor CHMs under `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Docs` are **not** substitutes.

---

## 8. Secrets / greenwash / clone checks

| Check | Result |
|---|---|
| Live FIX account `1369850` in `docs/` | **Absent** |
| MT5 / FIX / DB / Redis passwords | **Absent** |
| Broker manager logins / IPs in `docs/` | **Absent** |
| “Phase 1 Done” / API Gateway / live-by-default | **Absent** from current `docs/*.md` |
| Wholesale paste of v2 §§1–75 | **No.** Longest Markdown is 1379 bytes |
| v2 spec relocated into `docs/` | **No.** Still only at repo root |
| ML claimed as shipped | **No** |
| Live NewOrderSingle claimed on | **No** |
| Architecture.md Application “scoring” | **Mild overclaim** (see §5.1) |

---

## 9. A66 §18 acceptance (docs set)

Status **as of this measurement**:

```text
[x] docs/architecture.md rewritten (no longer the A66 §3 unsafe file)
[ ] docs/architecture.md complete vs A66 file-1 must-include
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

## 10. Stale swarm sentences (do not reuse)

| Prior claim | Now |
|---|---|
| A66 §1: only `docs/architecture.md` exists | **Stale.** Five more `*.md` exist as stubs. Five still missing. |
| A66 §3: `architecture.md` is the API-gateway / Phase-1-Done file | **Stale.** Rewritten (SHA `A5FB4FEF…`). Honest stub, not the old unsafe text. |
| A08: `D:\Prop\docs` is empty | **Stale.** |
| A11: “10 named docs” | Still **wrong**. Count is **11**. |
| B38: 7 files, no PNG, SVG SHA `98A969AE…` | **Stale extras.** Markdown hashes still match B38. SVG rewritten; PNG added; README now embeds PNG. |

Do not recreate files that exist. Do not mark stubs `CURRENT`.

---

## 11. Recommended next writes (later authorized docs task — **not this agent**)

A66 write order, updated for stubs already on disk:

| Order | File | Action |
|---|---|---|
| 1 | `architecture.md` | Expand: is/is-not, identity, stack, sibling index, link the **SVG** (not the placeholder PNG). Do not restore A66 §3 fiction. Fix Application “scoring” overclaim. |
| 2 | `deployment.md` | **Create.** Topology, secrets, flags, §69 pointer. |
| 3 | `mt5-integration.md` | **Create.** |
| 4–6 | recon / xauusd / scoring | **Thicken** existing stubs. Do not invent a second filename. |
| 7 | `shadow-copy.md` | **Create.** |
| 8 | `risk.md` | Thicken; keep kill-switch split. |
| 9 | `ctrader-fix.md` | Thicken QUOTE first. |
| 10 | `reconciliation.md` | **Create.** |
| 11 | `ml.md` | **Create the forbid-training stub now.** |

Keep `architecture.svg`. Replace or stop embedding `architecture.png` until it is a real raster of the SVG. Optional 20-line `docs/README.md` that **only** points at v2 + the 11 files. Do **not** create `docs/dashboard.md` (A66 §17).

---

## 12. What this agent did not do

- Did not edit any file under `D:\Prop\docs`, `D:\Prop\src`, `D:\Prop\apps`, or the v2 spec.
- Did not author missing `docs/*.md`.
- Did not run tests or builds.
- Did not update `INDEX.md` / `SWARM_LOG.md` (out of this ticket’s write set).

**Deliverable:** `D:\Prop\reports\swarm\20260818\C11_docs_gap.md` only.
