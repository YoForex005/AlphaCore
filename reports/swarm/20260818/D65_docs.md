# D65 — `docs/*.md` completeness vs architecture §66

| Field | Value |
|---|---|
| Agent | D65 (docs completeness only) |
| Date | 2026-08-18 |
| Measured at (local) | 2026-08-18T13:40:54.2382031+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\D65_docs.md` |
| Target | `D:\Prop\docs\*.md` vs architecture **§66** `/docs` list |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` heading `# 66. Suggested Repository Structure` (lines 2423–2470; file list 2458–2469) |
| Quality bar (not law) | `D:\Prop\reports\swarm\20260818\A66_docs_outline.md` must-include / done-when / shared skeleton. **v2 wins on conflict.** |
| Prior snapshots | A66 (plan; disk-state stale). B38 (6 stubs + SVG). C11 (same 6 stubs + SVG + placeholder PNG). D10 (same 6 stubs; **no** `deployment.md`). |
| Product source modified | **No.** This report is the only write. `D:\Prop\docs`, `D:\Prop\src`, `D:\Prop\apps` were not edited. |
| Classification vocab | Architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` |

---

## 0. Verdict

**`D:\Prop\docs` is not empty and does not satisfy architecture §66.**

§66 names **eleven** product Markdown files. **Seven** of those names are on disk. **Four** required names are **absent** anywhere under `D:\Prop` (excluding `node_modules` / `bin` / `obj` / `vendor` / `.git`). **Zero** of the eleven meet the A66 operational “done when” bar. **Zero** of the seven present files carry A66 front-matter or the shared skeleton.

This is **not** the D10 snapshot. After D10 (13:35:21) four files were written in a ~9-second cluster at 13:37:32–13:37:41:

| Path | vs D10 |
|---|---|
| `docs/deployment.md` | **NEW** (was MISSING) |
| `docs/ctrader-fix.md` | **Rewritten** (686 → 3195 bytes) |
| `docs/risk.md` | **Rewritten** (404 → 2678 bytes) |
| `docs/trade-reconstruction.md` | **Rewritten** (545 → 2293 bytes) |

Length is not completeness. The three rewrites added useful vocabulary **and** reintroduced v2 contradictions (challenge / pass-fail first-3, “API Gateway”, “submit to MT5”, hardcoded `TargetSubID = QUOTE/TRADE`, invented 100 ms “front-running” delay). Treat D10/C11 “stubs are the right direction” as **stale for those three files**.

| Question | Measured answer |
|---|---|
| Files in `D:\Prop\docs` | **9** (7 `*.md` + `architecture.svg` + `architecture.png`) |
| `docs/*.md` | **7** |
| §66 required Markdown files | **11** |
| Required files present | **7 / 11** |
| Required files missing | **4 / 11** |
| Present files that are `CURRENT` vs A66 done-when | **0 / 11** |
| A66 must-include items scored **YES** | **7 / 126** |
| A66 must-include items scored **PARTIAL** | **27 / 126** |
| A66 must-include items scored **NO** | **92 / 126** |
| Present files with A66 front-matter | **0 / 7** |
| Extra files not in §66 | **2** (`architecture.svg`, `architecture.png`) |
| Optional `docs/README.md` | **Absent** (not required) |
| Secrets / live passwords in `docs/*.md` | **0** (no password values; IPs / logins / account id are published v2 shape) |
| v2 spec cloned into `docs/` | **No** |
| Subfolders under `docs/` | **None** |
| Product source edited this pass | **0** |

**§66 documentation obligation: not discharged.** Do not greenwash “7 files exist” as “§66 docs complete.”

---

## 1. Method

| Step | Action |
|---|---|
| List | `list_dir` on `D:\Prop\docs` — 9 names |
| Census | PowerShell `Get-ChildItem -Force` + `Get-FileHash SHA256` + line / non-blank / encoding / EOL |
| Content | Full read of all 7 `*.md` and `architecture.svg`; visual read of `architecture.png` |
| Law | Architecture v2 §66 lines 2458–2469 (quoted below) |
| Quality | A66 §§2–18 must-include / must-not / done-when; shared skeleton |
| Missing-name search | Recursive filename search under `D:\Prop` for the four absent §66 names (exclude `node_modules` / `bin` / `obj` / `vendor` / `.git`) — **zero hits** |
| As-built spot-check | `CTraderFixOptions`, `VolumeConverter`, `RiskEngine`/`RiskLimits`, `TradeReconstructor`, `SymbolNormalizer`, `BaselineScorer`, `TraderState`, `KillSwitchMode`, `apps/api/Program.cs` `/health`, `docker-compose.yml`, Application folder, `mt5-sdk` Libs, `.env*` |
| Prior | A66, B38, C11, D10 — used as history, not law |
| Not done | No `dotnet`, no `npm`, no product edit, no new `docs/*.md`, no `INDEX.md` rewrite. Root `.env` was **not** opened. |

---

## 2. Architecture §66 required `/docs` list

Quoted from `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` heading `# 66. Suggested Repository Structure`:

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

A66 is the writing-quality bar for those 11 files. Swarm reports (A20–A28, A22–A25, …) are distillation sources, not substitutes.

---

## 3. `D:\Prop\docs` listing (this measurement)

Authoritative disk list (`Get-ChildItem -Force`) at 13:40:54:

```text
architecture.md
architecture.png
architecture.svg
ctrader-fix.md
deployment.md
risk.md
scoring.md
trade-reconstruction.md
xauusd-normalization.md
```

No subdirectories. No `README.md`. No `index.md`. No drafts of the four missing names. Attributes: `Archive` only.

Empty-directory check: **FAIL** (directory has content). File count: **9**.

There is **no** second product `docs/` under `D:\Prop\src`. Vendor CHMs at `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Docs\` are MetaQuotes SDK help, **not** §66 product docs.

Sibling Markdown that is **not** `docs/` (do not count toward §66):

| Path | Role |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | Binding v2 spec (must stay at repo root) |
| `D:\Prop\README.md` | Root landing page; embeds `docs/architecture.png` |
| `D:\Prop\mt5-sdk\README.md` | SDK-local readme |

### 3.1 File census (measured)

| Name | Bytes | Total lines | Non-blank | SHA-256 | LastWrite (local) | Encoding / EOL |
|---|---:|---:|---:|---|---|---|
| `architecture.md` | 1379 | 28 | 21 | `A5FB4FEFD9EFECDDCECDD884D1F1FA2042658AB06989F2155BF35B67BBFE5B3D` | 2026-08-18T13:18:40.1390945+05:30 | UTF-8 no BOM / LF |
| `architecture.png` | 12081 | — | — | `0F7BAF6D2461A5A055C83C278FCD0A8F718B3C2B86C19886221FDCB259EC98C9` | 2026-08-18T13:23:59.8107142+05:30 | PNG binary |
| `architecture.svg` | 2697 | 52 | 41 | `23F51B89D6CA6FC4A649E9A3F7DC04AFCB42485892D8604E3ACAD18EAFEB4327` | 2026-08-18T13:26:07.6594355+05:30 | UTF-8 no BOM / CRLF |
| `ctrader-fix.md` | 3195 | 73 | 49 | `52E80263C4D1672121842F17A382FFC691CB9350A1B26BF53EE8252C5ABD0C77` | 2026-08-18T13:37:38.1420345+05:30 | UTF-8 no BOM / LF |
| `deployment.md` | 2997 | 103 | 79 | `7F4B6130A8665910C78C2F6350F78D85DA9C34979C0B0E3966E2671A8499B999` | 2026-08-18T13:37:32.7768960+05:30 | UTF-8 no BOM / CRLF |
| `risk.md` | 2678 | 68 | 49 | `26ACB40F63AFFB0F41042143ABAA9B3362B3653ED71F0CB64C529DC71BA510CE` | 2026-08-18T13:37:34.0992433+05:30 | UTF-8 no BOM / LF |
| `scoring.md` | 327 | 11 | 6 | `91558CDB4F153379DE5234812116A7EC2569927B9BF45D03F86D4176E0225889` | 2026-08-18T13:20:12.5779925+05:30 | UTF-8 no BOM / LF |
| `trade-reconstruction.md` | 2293 | 58 | 41 | `500B4FF1C538EAFDEBBCAF189DA24DD4BF0E41A285E64ED68C86BC7C7E2008A1` | 2026-08-18T13:37:41.8968709+05:30 | UTF-8 no BOM / LF |
| `xauusd-normalization.md` | 263 | 7 | 4 | `28C228F1EB089D718FB7DBE3E8556FC51EC145BB4167F0675F905EAD39F5989A` | 2026-08-18T13:20:12.5799960+05:30 | UTF-8 no BOM / LF |

**Markdown total:** 13132 bytes, 348 lines, **249 non-blank lines** across seven files.  
**Folder total:** 27910 bytes (Markdown + SVG + PNG).

Unchanged vs D10 (same SHA-256): `architecture.md`, `scoring.md`, `xauusd-normalization.md`, `architecture.png`, `architecture.svg`.

---

## 4. Coverage matrix (§66 vs disk)

| # | §66 path | On disk? | §73.B | A66 done-when | Must-include YES/P/N | One-line |
|---|---|---|---|---|---|---|
| 1 | `docs/architecture.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | **No** | 0 / 5 / 5 | Honest ~1-page map. Same SHA as D10. Missing is/is-not, identity, stack, sibling index. |
| 2 | `docs/mt5-integration.md` | **No** | **MISSING** | **No** | 0 / 0 / 14 | No file of this name under `D:\Prop`. |
| 3 | `docs/trade-reconstruction.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | **No** | 0 / 3 / 6 | Thickened. **Wrong first-3** (challenge / pass-fail). |
| 4 | `docs/xauusd-normalization.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | **No** | 1 / 2 / 5 | Still 4 non-blank lines. Canonical + aliases + Security List. |
| 5 | `docs/scoring.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | **No** | 1 / 4 / 8 | Still 6 non-blank lines. No formulas, no full §22 table. |
| 6 | `docs/ml.md` | **No** | **MISSING** | **No** | 0 / 0 / 9 | Even the “do not train until §69” stub is absent. |
| 7 | `docs/shadow-copy.md` | **No** | **MISSING** | **No** | 0 / 0 / 12 | Policy lives in Domain + A24 only. |
| 8 | `docs/risk.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | **No** | 0 / 1 / 11 | Thickened. **Invented limits** + “submit to MT5”. |
| 9 | `docs/ctrader-fix.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | **No** | 3 / 7 / 6 | Thickened. Header inference + live-hedge narrative. |
| 10 | `docs/reconciliation.md` | **No** | **MISSING** | **No** | 0 / 0 / 9 | No startup READY_FOR_EXECUTION runbook. |
| 11 | `docs/deployment.md` | **Yes** | **EXISTS_NEEDS_REFACTOR** | **No** | 2 / 5 / 7 | **New.** Windows/Linux split is right; “API Gateway” + compose paths are wrong. |

**Score:** present **7 / 11**. Complete **0 / 11**. Missing **4 / 11**. Must-include **YES 7 / 126**.

### 4.1 Extra files (not scored against the 11)

| Path | §73.B (as extra) | Note |
|---|---|---|
| `docs/architecture.svg` | Extra / **aligned helper** | §4-direction pipeline boxes. **Not** linked from `docs/architecture.md`. |
| `docs/architecture.png` | Extra / **placeholder** | Pillow fallback: truncated “… → Scori” + “Architecture diagram (placeholder)”. Root `README.md` embeds **this** file. |

---

## 5. A66 global writing rules (apply to every file)

| # | A66 rule | Measured |
|---|---|---|
| 1 | Do not clone the v2 spec | **PASS** — longest Markdown is 3195 bytes |
| 2 | v2 wins over swarm | N/A for writers of this report; present files do **not** cite v2 section numbers except `architecture.md` §69 |
| 3 | No secrets / use `<SECRET>` | **PASS** for password values. `deployment.md` uses `MT5_PASSWORD=...` and `<SECRET>`. Account `1369850` / Achiever IP / login `2027` are published v2 shape (A66 file 2 / file 9 allow this) |
| 4 | No greenwash | **FAIL** on the 13:37 rewrites (live-hedge, challenge first-3, health-check overclaim) |
| 5 | Adapt to existing tree | **FAIL** in `deployment.md` (`./src/api`, `TraderIntelligence.dll`, C++ `mt5_worker.exe` as the worker) |
| 6 | Compound identity `broker_id + login` | **FAIL** — not stated in any present file |
| 7 | XAUUSD-first | **PARTIAL** — scoring/recon mention XAUUSD; first-3 section says “on an account” |
| 8 | Execution default off | **PASS** where mentioned (`architecture.md`, `ctrader-fix.md`, `deployment.md`) |
| 9 | Venue is not an LP | **PARTIAL** — `ctrader-fix.md` says not an LP, then “hedging execution venue” |
| 10 | Audience = implementer/ops | **PARTIAL** |
| 11 | Front-matter on every file | **FAIL 0 / 7** |
| 12 | Independently readable | **PARTIAL** |

Shared A66 skeleton (`Status / source of law`, Purpose, Binding invariants, Concepts, Flows, Persistence, Configuration, Failure, Metrics, Tests, Related docs): **0 / 7** files.

---

## 6. Per-file completeness (A66 must-include)

Legend: **Y** = present and aligned with v2. **P** = mentioned, incomplete or slightly off. **N** = absent or contradicts v2.

### 6.1 `docs/architecture.md` — EXISTS_NEEDS_REFACTOR — 0Y / 5P / 5N

SHA `A5FB4FEF…` **unchanged since B38/C11/D10**. Still **not** the A66 §3 unsafe API-gateway / “Phase 1 Done” document.

| # | A66 file-1 must-include | Score | Evidence |
|---|---|---|---|
| 1 | Status / source of law → v2 | **P** | Line 3 points at repo-root v2. No owner / date / `DRAFT`/`CURRENT` |
| 2 | What the system is / is not | **N** | No is-not (not an LP, not a password console, not MT5→ML→FIX) |
| 3 | Correct pipeline diagram | **N** | No Markdown diagram. Sibling SVG is **not linked** |
| 4 | Component map vs this repo | **P** | Table maps Domain / Application / Infra / Mt5+sdk / Fix / api / workers / web |
| 5 | Identity law `broker_id + login` | **N** | Absent |
| 6 | Tech-stack table from §5 | **N** | No C#/.NET 8 / QuickFIX/n / Postgres / Redis / React pin table |
| 7 | Safety defaults | **P** | Flag off, trade #3 not LIVE, `cServer` case, volume 10_000, plan-groups are labels. Missing: two FIX sessions, no send from MT5 callback, no Kafka/K8s/ClickHouse/LLM |
| 8 | Index of the other ten docs | **N** | No links to siblings (and four siblings do not exist) |
| 9 | Phase pointer §67 / A28 / A30 | **P** | Mentions §69 only |
| 10 | Honest current-state box | **P** | “What exists now” table; overclaims Application “scoring”; “Implemented toward” first useful version is softer than C13 **0/12** |

As-built spot-check of the table:

| Claim | Tree |
|---|---|
| Domain recon / symbol / baseline / risk / shadow / FIX FSM | **True** — matching folders under `src/Domain` |
| Application “broker ports, ingestion, scoring, dashboard contracts” | **Overclaim** — Application has `Contracts/`, `Ingestion/`, `Dashboard/` only. Scoring is `src/Domain/Scoring` |
| Persistence EF + in-memory + demo seed | **True** |
| Fake MT5 + C++ SDK preserved | **True** |
| Real NewOrderSingle off | **True** (default / absence of send) |
| Volume default 10_000 | **True** — `VolumeConverter.ManagerVolumeScale` |
| `TargetCompID = cServer` | **True** — `CTraderFixOptions` |

**Done when (A66):** a new hire can name the pipeline, find the code, find v2, and open the correct specialist doc. **Not met.**

### 6.2 `docs/mt5-integration.md` — MISSING — 0 / 14

Recursive filename search: **no file**. A66 file-2 (broker registry, group discovery, outbox, compound keys, `IMt5BrokerConnector`, Achiever/StarwaveFX connection **shape**, never invent trades when disconnected) is entirely absent.

Engineers must use v2 §6–13 + A04/A07/A12–A18. `deployment.md` leaked Achiever `57.128.141.65` / login `2027` / egress `81.29.145.69` but is **not** this document.

### 6.3 `docs/trade-reconstruction.md` — EXISTS_NEEDS_REFACTOR — 0Y / 3P / 6N

Rewritten after D10 (SHA `8CD041EB…` → `500B4FF1…`). Deal-vs-trade and IN/OUT/INOUT/OUT_BY are still the right core. First-3 meaning is now **wrong**.

| # | A66 file-3 must-include | Score | Evidence |
|---|---|---|---|
| 1 | Why mandatory (multi-deal lifecycle) | **P** | Overview + example of 0.5 / 0.3 / 0.2 lots |
| 2 | `ReconstructedTrade` field list from §14 | **N** | Entity in `src/Domain/Entities/ReconstructedTrade.cs` has 26 fields; doc lists a handful of computed values |
| 3 | Lifecycle (open / scale-in / partial / full / reversal; SL/TP is not a trade) | **P** | Entry table; no “SL/TP modification is not a trade”; INOUT not expanded |
| 4 | Exact “first 3” = 3 **completed reconstructed XAUUSD** lifecycles → `EARLY_SCORE_ELIGIBLE`, never `PROVEN_PROFITABLE` | **N** | **Contradicts v2.** Lines 43–47: “prop firm challenge evaluation” / “pass/fail algorithms” / “opening rules”. Does **not** say EARLY_SCORE / SHADOW-only. First-3 is “on an account”, not XAUUSD-only |
| 5 | Ordering `ORDER BY closed_at, opened_at, id` | **N** | Code `CompletedXauUsdTrades` orders `ClosedAt`, `OpenedAt`. Doc is silent |
| 6 | Input = normalized deals, not dashboard aggregates | **N** | “Query deals for account within evaluation window” |
| 7 | Output tables; `completed=false` excluded from first-3 | **P** | Open vs closed; no `reconstructed_trades` name |
| 8 | Failure metric / do not drop scale-ins | **N** | Absent. Code also excludes canceled-deal positions from first-3 (`EligibleForFirstThree = false`) — undocumented |
| 9 | Tests (§60 / A21 / A89) | **N** | Absent |

**Must-not:** no score formulas (honored). Does **not** say `deal_count==3` means three trades (honored). Introduces challenge/pass-fail fiction (A66 §3 pattern).

Compound identity: reconstruction in code is scoped `brokerId + login + positionId`. Doc groups by `position_ticket` only.

### 6.4 `docs/xauusd-normalization.md` — EXISTS_NEEDS_REFACTOR — 1Y / 2P / 5N

SHA `28C228F1…` **unchanged**. Four sentences.

| # | A66 file-4 must-include | Score |
|---|---|---|
| 1 | Problem (source aliases vs numeric dest id) | **P** |
| 2 | Canonical `XAUUSD` | **Y** |
| 3 | Two mapping tables `source_symbol_mappings` / `destination_symbols` | **N** |
| 4 | Unmapped policy (no `"XAU"`/`"GOLD"` substring guess) | **N** — listed aliases can be misread as implicit maps. Code `SymbolNormalizer.TryMapSource` **does** prefix-match `XAUUSD*` / `GOLD`, which v2/A66 forbid; the stub does not warn |
| 5 | Never assume FIX tag 55 is the string `"XAUUSD"` | **P** — “never hardcoded from another account” |
| 6 | Volume / qty chain §38 | **N** |
| 7 | Price source / `feature_quality` | **N** |
| 8 | Operator / RBAC procedure | **N** |

### 6.5 `docs/scoring.md` — EXISTS_NEEDS_REFACTOR — 1Y / 4P / 8N

SHA `91558CDB…` **unchanged**. Six non-blank lines. **No ML-as-shipped claim.** Trade #3 does not fund live capital.

| # | A66 file-5 must-include | Score |
|---|---|---|
| 1 | Deterministic baseline before XGBoost | **P** |
| 2 | Non-goals (no XGBoost, no first-3 $ PnL rank, no live send) | **P** |
| 3 | Universe = completed reconstructed XAUUSD only | **N** |
| 4 | Outputs three scores + a single trader state | **P** — three scores; state list incomplete |
| 5 | §18 feature inputs | **N** — `BaselineScorer`/`FeatureSnapshot` has them; doc does not |
| 6 | Trade #3 high score → SHADOW only | **Y** |
| 7 | Full §22 vocabulary (9 states) | **P** — mentions SHADOW, RISK_BLOCKED, `EARLY_SCORE_ELIGIBLE`. Missing INSUFFICIENT_DATA, EARLY_SCORE, WATCH, LIVE_CANDIDATE, LIVE, PAUSED, DISQUALIFIED |
| 8 | Continuous rescoring + freeze Trade-#3 snapshot | **N** |
| 9 | As-of / leakage | **N** |
| 10 | Authority: risk is final | **N** |
| 11 | `ScoreConfig` / `baseline.v1` | **N** |
| 12 | Tables | **N** |
| 13 | Tests | **N** |

Does not point at A22 as formula lock. Does not say Domain `CanPromoteToLive` is hard-false (if still true — not re-verified here).

### 6.6 `docs/ml.md` — MISSING — 0 / 9

No “do not train until §69” stub. A52 / A104 / C44 exist only as swarm reports. `D:\Prop\services\` is empty (correct for Phase 6; does **not** discharge this doc).

### 6.7 `docs/shadow-copy.md` — MISSING — 0 / 12

Destination-quote-only fill, no-catch-up, OPEN vs REDUCE/CLOSE, same risk engine in SHADOW mode: not operator-facing. Code: `src/Domain/Shadow/ShadowCopyEngine.cs` + A24.

### 6.8 `docs/risk.md` — EXISTS_NEEDS_REFACTOR — 0Y / 1P / 11N

Rewritten after D10 (SHA `01B1EE83…` → `26ACB40F…`). Old stub’s “Scoring proposes. Risk decides.” and OPEN vs CLOSE permissiveness were **removed**. New text invents a different limit set than §39 / `RiskLimits`.

| # | A66 file-8 must-include | Score | Evidence |
|---|---|---|---|
| 1 | Authority: scores are candidate only; risk decides | **N** | Sentence removed |
| 2 | Production flow; **never FIX from an MT5 callback** | **N** | Line 16: “before submission to **MT5 or cTrader**.” Source MT5 is observe-only |
| 3 | Every §39 hard limit + config key + reject code | **N** | Invented table: 5% daily / 10% total / 50 lots / 25 positions / 100 daily trades / 30 points. Code `RiskLimits` is dollars + XAU lots + 3 s quote age + 15 s signal age + martingale/abnormal/venue. **No overlap of units or names** |
| 4 | Pre-trade price guard §37 (`PRICE_MOVED_TOO_FAR`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`) | **N** | Post-fill MT5-vs-cTrader slippage formula |
| 5 | Kill switch split `STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN` | **P** | Concept split exists. Names are “Kill Switch” / “Emergency Flatten”, not the enum `KillSwitchMode.StopNewExecution` / `EmergencyFlatten` |
| 6 | §41 flag block (`CTRADER_FIX_*` + `REAL_COPY_EXECUTION_ENABLED`) | **N** | |
| 7 | Intent expiry / no catch-up / OPEN vs CLOSE (§63–64) | **N** | Removed from old stub. Invented 100–2000 ms window instead |
| 8 | Correlation reserved (§65) | **N** | |
| 9 | Fail-closed (§62) | **N** | |
| 10 | RBAC + `audit_logs` | **N** | |
| 11 | Tables `copy_intents` / `risk_decisions` | **N** | |
| 12 | Tests | **N** | |

**Invented policy (not in v2):** “Minimum delay: 100ms — prevents front-running detection by brokers.” That sentence must not be treated as law.

**Volume contradiction inside the same file:** “MT5: volume in hundredths of lots (50000 = 5.0 lots)” **and** `MT5_VOLUME_SCALE=10000`. Hundredths would be scale 100. Code comment in `VolumeConverter.cs`: *the mt5_types.h “hundredths of lots” comment is incorrect*; default scale is **10_000**.

Does not warn that `RiskEngine` is in-process and not a wired production gate on the live send path.

### 6.9 `docs/ctrader-fix.md` — EXISTS_NEEDS_REFACTOR — 3Y / 7P / 6N

Rewritten after D10 (SHA `9CA80639…` → `52E80263…`). Old stub’s four official help.ctrader.com URLs were **removed**. Ports, Security List, quote subscribe, never-resubmit, flag default false remain useful.

Opening sentence is a v2 conflict: “winning trades are copied to cTrader for real-money hedging” + “prop firm's challenge accounts”. Default after trade #3 is **SHADOW**. Venue is an **external execution venue**, not an LP **and** not described as a hedge-by-default product.

| # | A66 file-9 must-include | Score | Evidence |
|---|---|---|---|
| 1 | Venue statement (Pepperstone / host / account / not an LP) | **P** | Not-an-LP yes. Host `live-us-eqx-01.p.c-trader.com` **absent**. “Hedging” framing |
| 2 | Two independent sessions (conn, sequences, heartbeats, metrics) | **P** | Ports + “distinct session qualifiers.” Does not say do not share a sequence counter |
| 3 | Production ports QUOTE TLS 5211 / TRADE TLS 5212 | **Y** | Also lists plain 5201/5202 (OK as non-default) |
| 4 | Header warning: **both** SenderSubID and TargetSubID configurable; do not infer from labels | **N** | **Hardcodes** `TargetSubID = QUOTE or TRADE` and `SenderSubID = <BROKER_ISSUED_VALUE>`. This is the inference §26 / A66 warn against. Code currently defaults the same way (`CTraderFixOptions`) but both **are** settable — doc does not say “configurable; prove Logon” |
| 5 | Secret-safe config from §56 | **P** | Shows `live.pepperstone.1369850` + Account `1369850`. No password. No `<SECRET>` password line |
| 6 | QuickFIX/n + **cTrader RoE dictionary** (generic FIX 4.4 insufficient) | **P** | Mentions QuickFIX/N. No dictionary warning. No “do not write TcpClient engine” |
| 7 | Single-active TRADE ownership | **N** | `FixSessionOwnership.cs` exists; doc silent |
| 8 | Minimum §29 message set | **P** | Has Logon-adjacent flow, SecurityList, MD, NOS, ER, OrderStatus. Missing Logout/Heartbeat/TestRequest/Resend/Reject, MassStatus, RequestForPositions/PositionReport, cancel/replace, BusinessMessageReject |
| 9 | Instrument discovery via Security List | **Y** | |
| 10 | QUOTE feed (bid/ask, ts; risk rejects stale) | **P** | Subscribe flow; no freshness/reject rule |
| 11 | Persist ClOrdID **before** send; never retry 35=D because TCP broke | **P** | Mentions ClOrdID match. No persist-before-send state machine (`not sent` / `sent-ack-unknown` / …) |
| 12 | `EXECUTION_STATE_UNKNOWN` procedure | **P** | Never re-submit is right. “If unknown, mark failed” skips MassStatus / positions |
| 13 | Source↔dest mapping (scale-in / partial / reverse; persist dest position id) | **N** | “Match ER.ClOrdID back to the originating MT5 copy-trade request” skips CopyIntent → Risk → ExecutionIntent |
| 14 | §61 harness; do not use the real account as first test | **N** | `FixSimulationHarness.cs` exists; doc silent. “orders are simulated for testing” is not the harness |
| 15 | Official links (§74) | **N** | **Regression** vs the D10 stub |
| 16 | §58 FIX metrics | **N** | |

A tiny “Reconciliation” section (slippage 30 points) does **not** discharge `reconciliation.md`.

### 6.10 `docs/reconciliation.md` — MISSING — 0 / 9

No startup checklist: Logon → block new executions → MassStatus + RequestForPositions → compare DB → only then `READY_FOR_EXECUTION`. A47 is swarm-only. `ctrader-fix.md` slippage bullets are not this runbook.

### 6.11 `docs/deployment.md` — EXISTS_NEEDS_REFACTOR — 2Y / 5P / 7N

**Did not exist at D10.** Creating the filename is progress. Content is a first draft with several as-built errors.

| # | A66 file-11 must-include | Score | Evidence |
|---|---|---|---|
| 1 | Topology: Windows MT5 worker; Linux OK for API/Postgres/Redis/React | **Y** | |
| 2 | What **not** to deploy (Kafka, K8s, ClickHouse, mesh, dual TRADE) | **N** | |
| 3 | Processes: `apps/api`, `apps/mt5-worker`, `apps/fix-worker`, `apps/web`; one TRADE owner | **P** | Names a C++ `mt5_worker.exe` via cmake. Calls API an **“API Gateway”** (A66 §3 fiction). `apps/fix-worker` not listed as a process |
| 4 | Postgres = authority; Redis = cache/leases only | **P** | Lists both. No authority rule |
| 5 | Secret handling; `.env.example` placeholders; dashboard never gets passwords | **P** | Instructs “Copy `.env.example` to `.env`”. **Repo-root `.env.example` does not exist.** `D:\Prop\.env` **does** exist (contents not read). Only `mt5-sdk/.env.example` is on disk |
| 6 | Achiever egress `81.29.145.69` | **Y** | |
| 7 | FIX TLS default; sequence-file backup; one TRADE session | **N** | |
| 8 | Feature-flag matrix (connect/read vs `REAL_COPY_EXECUTION_ENABLED`) | **P** | Only the last flag, set false |
| 9 | Serilog / OTel / §57–58 / System Health | **P** | Health section overclaims. `/health` in `Program.cs` returns `{ status, utc }` only. `/api/health` is a **demo stub** (FakeMt5, FIX “no live TLS”). It does **not** check DB + Redis + MT5 worker |
| 10 | RBAC deploy notes | **N** | |
| 11 | Fail-closed if DB down | **N** | |
| 12 | Link §67–70 / A28 gates (do not fork a checklist) | **N** | |
| 13 | First useful version §69 (no ML, no live NOS) | **N** | |
| 14 | Live FIX acceptance §70 before flipping the flag | **N** | |

As-built contradictions (do not implement from this file as written):

| `deployment.md` claim | Repo fact |
|---|---|
| `build: ./src/api` | Host is `apps/api/TraderIntelligence.Api.csproj` |
| `dotnet out/TraderIntelligence.dll` | Assembly is `TraderIntelligence.Api` |
| compose services `api` + `db` + `redis` as shown | Real `D:\Prop\docker-compose.yml` is postgres + redis + `dotnet run --project apps/api/...`. Image is the SDK image, not a built `./src/api` |
| `vendor/MetaTrader5SDK/Libs/MT5APIManager64.dll` | File exists under **`mt5-sdk/vendor/...`**. Same folder also has `MetaQuotes.MT5ManagerAPI64.dll` |
| cmake `mt5_worker.exe` is the product worker | Product ingest host is `apps/mt5-worker` (C#). `mt5-sdk` is the native library |
| `POSTGRES_PASSWORD: ${DB_PASSWORD}` | Compose uses `ti_dev_only` (dev only; still a committed password in compose, not in this doc) |

---

## 7. Extra diagrams (not §66)

### 7.1 `architecture.svg`

900×420 SVG: Achiever/StarwaveFX → `apps/mt5-worker` → Postgres/Outbox → Reconstruction → Scoring → “Shadow Copy & Risk” → `src/Fix.CTrader` (QUOTE + TRADE). Footer points at `docs/` and the v2 spec.

Aligned with §4 direction. Collapses CopyIntent / RiskEngine / ExecutionIntent / reconciliation. **Not** a substitute for `architecture.md` or `reconciliation.md`. **Not linked** from `docs/architecture.md`.

### 7.2 `architecture.png`

Visual: light-grey 900×420 canvas, truncated pipeline sentence ending mid-word (“… → Scori”) and the label **“Architecture diagram (placeholder)”**.

Produced by `D:\Prop\scripts\svg_to_png.py` Pillow fallback after cairosvg was unavailable. Current `README.md` line 5 embeds **this PNG**, not the SVG.

This file does not discharge any §66 row.

---

## 8. Missing required files

Recursive search for these exact filenames under `D:\Prop` (excluding `node_modules` / `bin` / `obj` / `vendor` / `.git`) returned **no files**:

| Missing | Consequence if left absent |
|---|---|
| `docs/mt5-integration.md` | No product runbook for two Manager collectors, group discovery, outbox, compound keys. Leaked Achiever IP/login in `deployment.md` is not a substitute. |
| `docs/ml.md` | No written “do not train until §69.” A later contributor can treat ML as unspecified. |
| `docs/shadow-copy.md` | Destination-quote-only fill and no-catch-up are not operator-facing. |
| `docs/reconciliation.md` | No startup checklist that blocks `READY_FOR_EXECUTION` on mismatch. |

Vendor CHMs under `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Docs` are **not** substitutes.

---

## 9. Secrets / greenwash / clone checks (`docs/`)

Needles searched in every `docs/*.md` this pass: `1369850`, `password=`, `Password=`, `SenderCompID`, `81.29.145.69`, `live-us-eqx`, `<SECRET>`, `api-key`, `ApiKey`, `57.128.141.65`, `2027`, `9904`, `MT5_PASSWORD`, `FIX_PASSWORD`, `cServer`, `CSERVER`, `REAL_COPY_EXECUTION`.

Hits (not passwords):

| File | Needle | Count | Classification |
|---|---|---:|---|
| `architecture.md` | `cServer`, `REAL_COPY_EXECUTION` | 1+1 | Allowed safety text |
| `ctrader-fix.md` | `1369850`, `SenderCompID`, `cServer`, `REAL_COPY_EXECUTION` | 2+2+6+1 | Published v2 FIX shape. **No password.** Host omitted |
| `deployment.md` | `81.29.145.69`, `57.128.141.65`, `2027`, `MT5_PASSWORD`, `<SECRET>`, `REAL_COPY_EXECUTION` | 1+1+1+2+1+1 | Published Achiever shape. Password shown as `...` / `<SECRET>` only |

| Check | Result |
|---|---|
| Live FIX **password** in `docs/` | **Absent** |
| MT5 / DB / Redis / proxy **password values** | **Absent** |
| Account id `1369850` | **Present** in `ctrader-fix.md` (A66 file 9 asks for this venue statement) |
| Achiever IP / manager login | **Present** in `deployment.md` (A66 file 2 allows non-secret shape; belongs primarily in `mt5-integration.md`) |
| “Phase 1 Done” | **Absent** |
| “API Gateway” | **Reintroduced** in `deployment.md` line 8 |
| Challenge / pass-fail / live-by-default copy | **Reintroduced** in `trade-reconstruction.md` + `ctrader-fix.md` |
| Wholesale paste of v2 §§1–75 | **No** |
| v2 spec relocated into `docs/` | **No** — still only at repo root |
| ML claimed as shipped | **No** |
| Live NewOrderSingle claimed **on** | **No** (flag still documented false) |
| Front-matter / shared skeleton | **0 / 7** |

Root `D:\Prop\.env` exists (3408 bytes). **Not opened.** Out of `docs/` scope; relevant only because `deployment.md` tells operators to copy a missing `.env.example`.

---

## 10. A66 §18 acceptance (docs set)

Status **as of this measurement**:

```text
[x] docs/architecture.md rewritten (no longer the A66 §3 unsafe file)
[ ] docs/architecture.md complete vs A66 file-1 must-include
[ ] docs/mt5-integration.md exists
[x] docs/trade-reconstruction.md exists (thickened; first-3 WRONG; not done)
[x] docs/xauusd-normalization.md exists (stub; not done)
[x] docs/scoring.md exists (no ML claims; not done)
[ ] docs/ml.md exists (at least the “do not train until §69” stub)
[ ] docs/shadow-copy.md exists
[x] docs/risk.md exists (thickened; limits WRONG; not done)
[x] docs/ctrader-fix.md exists (thickened; QUOTE section not complete; header inference)
[ ] docs/reconciliation.md exists
[x] docs/deployment.md exists (new; not done; API Gateway / path errors)
[x] none of the *present* files contain real password values
[x] none of the *present* files is a paste of the v2 spec
[x] v2 spec remains only at
    D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
[ ] none of the eleven files contradicts v2 on first-3 / authority / header tags / source-MT5-is-observe-only
```

**§66 documentation obligation is not discharged.**

Delta vs D10 checkboxes: `deployment.md exists` flipped **[ ] → [x]**. Completeness of the other ten names did **not** flip to done. Three existing files got longer and **less** aligned on first-3 / risk limits / venue narrative.

---

## 11. Must-include scoreboard (A66 file-1…11)

| File | Items | Y | P | N | % Y only |
|---|---:|---:|---:|---:|---:|
| `architecture.md` | 10 | 0 | 5 | 5 | 0 |
| `mt5-integration.md` | 14 | 0 | 0 | 14 | 0 |
| `trade-reconstruction.md` | 9 | 0 | 3 | 6 | 0 |
| `xauusd-normalization.md` | 8 | 1 | 2 | 5 | 12.5 |
| `scoring.md` | 13 | 1 | 4 | 8 | 7.7 |
| `ml.md` | 9 | 0 | 0 | 9 | 0 |
| `shadow-copy.md` | 12 | 0 | 0 | 12 | 0 |
| `risk.md` | 12 | 0 | 1 | 11 | 0 |
| `ctrader-fix.md` | 16 | 3 | 7 | 6 | 18.8 |
| `reconciliation.md` | 9 | 0 | 0 | 9 | 0 |
| `deployment.md` | 14 | 2 | 5 | 7 | 14.3 |
| **Total** | **126** | **7** | **27** | **92** | **5.6** |

Do **not** add PARTIAL into a “~27% complete” headline. Partial is not done.

---

## 12. Stale swarm sentences (do not reuse)

| Prior claim | Now |
|---|---|
| A66 §1: only `docs/architecture.md` exists | **Stale.** Seven `*.md` exist. Four still missing. |
| A66 §3: `architecture.md` is the API-gateway / Phase-1-Done file | **Stale for that file.** SHA `A5FB4FEF…` is the honest stub. **“API Gateway” returned in `deployment.md`.** |
| A08: `D:\Prop\docs` is empty | **Stale.** |
| A11: “10 named docs” | Still **wrong**. Count is **11**. |
| B38: 7 files, no PNG, 6 stubs, 5 missing | **Stale extras + missing-count.** |
| C11 / D10: present **6 / 11**, missing **5 / 11**, Markdown hashes for fix/risk/recon as in D10 | **Stale.** Now **7 / 11** present, **4** missing. `ctrader-fix.md` / `risk.md` / `trade-reconstruction.md` hashes changed. `deployment.md` exists. |
| D10 / C11: those three stubs are “correct direction” only | **Stale.** Rewrites added v2 conflicts. |
| D10: no `1369850` in `docs/` | **Stale.** Now in `ctrader-fix.md` (allowed venue statement). |

Do not recreate files that exist. Do not mark any present file `CURRENT`. Do not treat 7/11 filenames as 7/11 complete.

---

## 13. Recommended next writes (later authorized docs task — **not this agent**)

A66 write order, updated for disk **as of 13:40:54**:

| Order | File | Action |
|---|---|---|
| 1 | `architecture.md` | Expand: is/is-not, identity, stack, sibling index, link the **SVG** (not the placeholder PNG). Fix Application “scoring” overclaim. Do not restore A66 §3 fiction. |
| 2 | `deployment.md` | **Thicken in place.** Drop “API Gateway.” Document `apps/api`, `apps/mt5-worker`, `apps/fix-worker`. Match real `docker-compose.yml`. Point at a real `.env.example` or stop claiming it. Add §69/§70 + fail-closed. |
| 3 | `mt5-integration.md` | **Create.** Move Achiever/StarwaveFX connection **shape** here. |
| 4 | `trade-reconstruction.md` | **Fix first-3** to §15 (completed XAUUSD lifecycles → EARLY_SCORE, never challenge pass/fail). Add field list + `ORDER BY closed_at, opened_at, id`. |
| 5 | `xauusd-normalization.md` | Thicken; unmapped policy; §38 qty; never-guess tag 55 string. |
| 6 | `scoring.md` | Thicken; point at A22; full §22 table; risk is final. |
| 7 | `shadow-copy.md` | **Create.** Destination quotes only. |
| 8 | `risk.md` | **Replace invented limits** with §39 / `RiskLimits`. Restore “scoring proposes / risk decides.” Delete 100 ms front-running delay. Never submit to source MT5. |
| 9 | `ctrader-fix.md` | Fix venue narrative. Both SubIDs configurable. Restore official links. Add dictionary + ownership + persist-before-send + harness. QUOTE first. |
| 10 | `reconciliation.md` | **Create.** Startup block until READY_FOR_EXECUTION. |
| 11 | `ml.md` | **Create the forbid-training stub now.** |

Keep `architecture.svg`. Replace or stop embedding `architecture.png` until it is a real raster of the SVG. Optional 20-line `docs/README.md` that **only** points at v2 + the 11 files. Do **not** create `docs/dashboard.md` (A66 §17).

---

## 14. What this agent did not do

- Did not edit any file under `D:\Prop\docs`, `D:\Prop\src`, `D:\Prop\apps`, or the v2 spec.
- Did not author missing `docs/*.md`.
- Did not thicken or correct the 13:37 rewrites.
- Did not rewrite `INDEX.md` or `SWARM_LOG.md`.
- Did not open `D:\Prop\.env`.
- Did not run tests or builds.

**Deliverable:** `D:\Prop\reports\swarm\20260818\D65_docs.md` only.
