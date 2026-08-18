# A66 — Outline of `docs/*.md` that must be written (Architecture §66)

**Artifact:** `D:\Prop\reports\swarm\20260818\A66_docs_outline.md`  
**Date:** 2026-08-18  
**Agent:** A66  
**Status:** Documentation plan only. No product source was modified. No `docs/*.md` file was authored or overwritten by this agent.  
**Source of law:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§66** (file list) plus the cited sections that each file must implement.  
**Existing repo layout hint (binding):** “Adapt to the existing repo; do not create duplicates unnecessarily.”

---

## 0. What this file is / is not

### In scope

Produce a write-plan for the **eleven** Markdown files named under `/docs` in architecture **§66**. For each file: purpose, audience, required H2/H3 outline, binding invariants, source sections, related swarm specs to distill (not copy), phase when it becomes required, and a done-check.

### Explicitly out of scope (do not write as `docs/*`)

| Path | Why excluded |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | **This is the v2 spec.** It already exists at repo root. §66 does **not** ask for a second copy. Do not rewrite, relocate, or paste it into `docs/`. |
| `docs/README.md` | Suggested in A30 Increment 0 as an index. **Not** in the §66 list. Optional later; not a must-write for this outline. |
| Swarm reports under `D:\Prop\reports\swarm\20260818\` | Internal audit/specs. They feed the docs; they are not the product docs. |
| `mt5-sdk/README.md` | SDK-local readme. Cite from `mt5-integration.md`; do not duplicate as a §66 file. |
| Extra ADRs, runbooks, OpenAPI, MQ5, Kafka/K8s/ClickHouse docs | §71 “What Not to Build Yet.” |

`docs/architecture.md` in §66 is **not** the v2 spec. It is a short implementer-facing system map that **points at** the v2 spec.

---

## 1. §66 file inventory (must write)

Quoted from architecture §66:

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

**Count: 11.** (A11 called this “10 named docs”; that under-count is wrong. Do not drop `architecture.md` or `deployment.md`.)

| # | Path to write | One-line job | Primary v2 sections | Earliest phase | Current disk state (2026-08-18) |
|---|---|---|---|---|---|
| 1 | `docs/architecture.md` | As-built/as-target system map + index of the other 10 docs. **Not** a restatement of 75 sections. | §1–5, §66, §71–75 | Phase 0 | **Present but non-compliant** — see §3 |
| 2 | `docs/mt5-integration.md` | Multi-broker Manager API collectors, groups, ingest, outbox | §6–13, §62 MT5, §57–58 MT5 | Phase 1 | **Missing** |
| 3 | `docs/trade-reconstruction.md` | Order ≠ Deal ≠ Position ≠ logical trade; first-3 definition | §14–15, §60 reconstruction tests | Phase 2 | **Missing** |
| 4 | `docs/xauusd-normalization.md` | Canonical XAUUSD + venue mappings; never guess tag 55 | §16–17, §30, §38 | Phase 2 | **Missing** |
| 5 | `docs/scoring.md` | Deterministic baseline + trader states. **No ML claims.** | §15, §18, §22–23 | Phase 3 | **Missing** |
| 6 | `docs/ml.md` | Objective, leakage, eval, when ML is allowed | §19–21, §67 Phase 6, §71 | After first useful version (§69) | **Missing** |
| 7 | `docs/shadow-copy.md` | Destination-quote shadow book before live send | §24, §31, §36–38, §63–64 | Phase 5 | **Missing** |
| 8 | `docs/risk.md` | Risk is final authority; limits, kill switch, flags | §32–41, §62–65 | Phase 5 (shadow) / 8 (live) | **Missing** |
| 9 | `docs/ctrader-fix.md` | Two TLS sessions, headers, ownership, message set | §25–35, §41, §61 | Phase 4 QUOTE / 7 TRADE | **Missing** |
| 10 | `docs/reconciliation.md` | Startup + periodic venue vs DB; block while inconsistent | §42–44, §34, §70 | Phase 7 | **Missing** |
| 11 | `docs/deployment.md` | Windows MT5 worker, Linux services, secrets, flags, gates | §5 deploy, §55–59, §67–70 | Phase 0 map; live before Phase 8 | **Missing** |

`D:\Prop\docs\` exists. Only `docs/architecture.md` is on disk; the other ten files do not exist.

---

## 2. Global writing rules (apply to every file)

1. **Do not clone the v2 spec.** Each `docs/*.md` is operational: contracts, flows, invariants, config keys, failure rules, test pointers. Cross-link `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md#<section>` instead of pasting chapters.
2. **Do not invent a second architecture.** If swarm specs (A20–A28, A22–A25) and v2 disagree, **v2 wins**. Swarm files are distillation sources, not law.
3. **No secrets.** Never write real MT5 / proxy / FIX / DB / Redis passwords. Use `<SECRET>` sentinels. Never put account password `1369850` material in git. Config examples copy the **shape** of §56 only.
4. **No greenwash.** Do not mark a phase “Done” unless measured (tests, logs, hashes). The current `docs/architecture.md` violates this (see §3).
5. **Adapt to the existing tree.** Document `apps/`, `src/`, `mt5-sdk/`, `tests/` as they are. Do not prescribe duplicate folders already present (`src/Domain` already holds Scoring/Risk/Shadow). §66 extra folders (`src/TradeReconstruction`, `services/ml-service`, …) are **proposed**, not mandatory copies.
6. **Compound identity everywhere.** `broker_id + login` / ticket / position. Login is never globally unique (§10).
7. **XAUUSD-first.** Other symbols may be ingested raw; scoring/copy/docs default to canonical `XAUUSD`.
8. **Execution default is off.** `REAL_COPY_EXECUTION_ENABLED=false` until §68 and §70 are true.
9. **Pepperstone/cServer is an external execution venue, not an LP** (§1.6).
10. **Audience:** engineers implementing or operating this repo. Not a sales deck. Not an LLM prompt dump.
11. **Front-matter on every file:** title, last-updated date, owner role, “source of law = v2 §…”, related code paths, related swarm spec (if any), status (`DRAFT` / `CURRENT` / `SUPERSEDED`).
12. **Keep each file independently readable.** A FIX engineer should not have to read `ml.md` to operate QUOTE/TRADE.

Recommended shared skeleton:

```text
# <Title>
## Status / source of law
## Purpose and non-goals
## Binding invariants (table, testable)
## Concepts and identities
## Flows (sequence, never from MT5 callback to FIX send)
## Persistence (tables by §45 canonical names)
## Configuration (secret-safe)
## Failure rules
## Metrics and logs (correlation ids; no auth tags)
## Tests required (§60 / A27)
## Related docs (links only)
```

---

## 3. Current `docs/architecture.md` is not the §66 document

Path: `D:\Prop\docs\architecture.md` (already on disk).

It must be **rewritten** (later coding/docs task — not this agent). It is **not** acceptable as the §66 architecture map. Measured contradictions vs v2:

| Existing claim | v2 / repo fact |
|---|---|
| API Gateway containing Account / Trade Recon / Risk / Copy-Trade services | Pipeline is MT5 collectors → raw immutable data → reconstruction → scoring → shadow → CopyIntent → risk → ExecutionIntent → FIX adapter (§1, §4, §32). Workers are `apps/mt5-worker` and `apps/fix-worker`. |
| “Account Management — MT5 user provisioning, password management” | Out of product goal. We **observe** ~5,000 source accounts; we do not provision trader passwords (§3, §6–9). |
| “prop challenge → live account mirroring via FIX” | Default after trade #3 is **SHADOW**, never automatic live capital (§1.4, §23). |
| Phase 1 “Done”, 4-phase plan | §67 is **Phase 0–8**. Phase 0 audit is still the swarm work. Ingestion/FIX/risk are not done. |
| C++17 “MT5 Worker” as the architecture | Native SDK lives in `mt5-sdk/`; product services are C#/.NET 8 (§5). |
| “news filter” in Phase 4 | §71: do not add extras; no LLM; ML only after baseline. |
| Missing QUOTE vs TRADE, outbox, first-3 rule, kill-switch split | Required in §7–13, §15, §25–28, §40. |

Until rewritten, treat `docs/architecture.md` as **MISSING/UNSAFE**, not EXISTS_AND_GOOD.

---

## 4. File 1 — `docs/architecture.md`

**Audience:** any engineer landing in the repo.  
**Length target:** short (about 4–8 pages). If it grows past that, content belongs in one of the other 10 files.  
**Do not:** paste §§1–75. Do not replace the v2 spec.

### Must include

1. **Status / source of law** — link to repo-root v2 spec; state that v2 is binding.
2. **What this system is / is not**
   - Is: identify copyable XAUUSD traders from Achiever + StarwaveFX (~5,000 accounts), score them, shadow-copy, then optionally route approved trades to Pepperstone cTrader FIX 4.4.
   - Is not: MT5 → ML → FIX in one hop; an LP; a challenge-pass predictor; a password-provisioning console.
3. **Correct pipeline diagram** — copy the §1 / §4 / §75 text diagrams (source brokers → collectors → raw data → reconstruction → features/scoring → shadow → CopyIntent → risk → ExecutionIntent → QUOTE+TRADE FIX → reconciliation). Do not invent a microservice mesh.
4. **Component map vs this repo** (adapt, do not duplicate):

   | Role | Path today |
   |---|---|
   | React dashboard | `apps/web` |
   | ASP.NET Core API | `apps/api` |
   | MT5 collector worker | `apps/mt5-worker` + `src/Mt5` + `mt5-sdk/` |
   | FIX worker | `apps/fix-worker` + `src/Fix.CTrader` |
   | Domain (recon/score/risk/shadow/execution helpers) | `src/Domain` (do not also create empty `src/Scoring` etc. unless a later increment splits them) |
   | Application / Infrastructure | `src/Application`, `src/Infrastructure` |
   | ML service | `services/ml-service` — **do not build until Phase 6** |
   | Tests | `tests/Unit`, `tests/Integration`; Replay/Fix/Risk folders are proposed |

5. **Identity law** — one paragraph + examples (`broker_id + login`, tickets). Point to table catalog A20 / §45.
6. **Tech stack table** from §5 (C# / .NET 8, QuickFIX/n + cTrader dictionary, PostgreSQL authority, Redis cache/leases only, Python later, React/Vite). Pin: do not fashion-upgrade runtime if native MT5 DLL constrains it.
7. **Safety defaults** — live `NewOrderSingle` off; two FIX sessions; no send from MT5 callback; no Kafka/K8s/ClickHouse/LLM (§71).
8. **Doc index** — table linking the other 10 `docs/*.md` files with one sentence each.
9. **Phase pointer** — “implementation order is §67 / A28 / A30; this file does not redefine phases.”
10. **Honest current-state box** — what exists vs stub (Domain has recon/score/risk/shadow types; API is still weather-forecast-level; FIX project is a stub). Update this box when measured state changes.

### Must not include

- Full FIX tag tables (that is `ctrader-fix.md`).
- Score formulas (that is `scoring.md`).
- Broker password/proxy values.
- A second phase checklist that diverges from §67.

### Distill from (do not copy wholesale)

A01–A11 (repo map), A28 (phases), A30 (increments). Source of law remains v2.

### Done when

A new hire can name the pipeline, find the code, find the v2 spec, and open the correct specialist doc — without believing live copy is already on.

---

## 5. File 2 — `docs/mt5-integration.md`

**Audience:** MT5 / collector engineers.  
**Primary law:** §6–13, §9 group discovery, §55–58, §62 (MT5 unavailable).

### Must include

1. **Purpose / non-goals**
   - Purpose: one connector abstraction, two (then N) broker instances; raw immutable ingest.
   - Non-goals: executing on MT5; using plan mappings as the group allow-list; inventing trades when disconnected.
2. **Broker registry** — Achiever + StarwaveFX + future brokers. Single `IMt5BrokerConnector` (or the real SDK-shaped interface). Do not fork two connector codebases (§6).
3. **Non-secret connection shape** (values may be documented as already published in v2; passwords stay `<SECRET>`):
   - Achiever: server `57.128.141.65:443`, manager login `2027`, `MT5_MODE=local`, pool 8, server name, default group `demo\Maxmaster` **is not the only group**, required egress IP `81.29.145.69`, optional proxy.
   - StarwaveFX: `84.201.6.142:443`, login `9904`, pool 4, no IP whitelist today, still design for proxy later.
4. **Startup / resync sequence** (§7): Connect → enumerate **all** groups the Manager login can see → upsert groups → enumerate accounts → associate broker+group → sync history.
5. **Plan-to-group map** (§9) — preserve the `MT5_GROUP_*` env keys as **optional labels**. Incorrect: “only sync known plan groups.” Correct: discover all, then optionally tag.
6. **Raw layer tables** (use §45 names): `brokers`, `broker_connections`, `mt5_groups`, `plan_group_mappings`, `mt5_accounts`, `mt5_account_snapshots`, `mt5_orders`, `mt5_deals`, `mt5_positions_current`, `mt5_symbols`, `mt5_xau_ticks`, `sync_checkpoints`, `ingestion_events`, `outbox_events`.
7. **Ingest pattern** (§12): Historical backfill (checkpoint → fetch → normalize → idempotent upsert → persist checkpoint) **plus** live subscribe **plus** periodic reconcile.
8. **Live callback rule** (§12, §72.6–7): validate → dedup → persist raw → write transactional outbox → commit. **Never** score, reconstruct heavily, or send FIX inside the Manager callback.
9. **Why outbox, not Kafka** (§13, §71). Event types: trade-completed, score-update, shadow-copy, risk-check, notifications.
10. **Native vs HTTP transport** — document `mt5-sdk` `IMT5Client` (`local` Manager DLL vs `http` client), Windows constraint, pool/watchdog. Point at `D:\Prop\mt5-sdk\README.md` and swarm A12–A18. Do not re-document every C++ header.
11. **Idempotency / compound keys** — `broker_id + deal_ticket` etc.
12. **Failure:** MT5 down → do not invent source trades; retry; expose stale-source; **do not open new copies from stale source** (§62).
13. **Metrics / logs** — `mt5_connected`, reconnects, events, deals, duplicate deals, backfill lag, outbox backlog; correlation ids; never log manager/proxy passwords.
14. **Tests** — deal dedup, backfill/restart, outbox processing (§60).

### Must not include

- Reconstruction algorithms (file 3).
- cTrader session details (file 9).
- Claiming the C# `src/Mt5` layer already equals the C++ SDK (A04: it does not).

### Distill from

A04, A07, A12–A18, A19 (secrets), A20 (tables), A37–A39 (enums, volume, groups).

### Done when

An engineer can add a third MT5 broker by config + registry row, not by cloning a connector, and knows the ingest/outbox contract.

---

## 6. File 3 — `docs/trade-reconstruction.md`

**Audience:** domain / quant engineers.  
**Primary law:** §14–15, reconstruction fields, §60 unit/replay tests.  
**A30 note:** write this in Increment 4 (Phase 2). Contract sentence: **Order ≠ Deal ≠ Position ≠ logical trade.**

### Must include

1. **Why mandatory** — one MT5 position can have multiple entries, partial fills, scale-ins, partial closes, SL/TP mods, multiple closing deals.
2. **Canonical entity `ReconstructedTrade`** — field list from §14 (`id`, `broker_id`, `login`, `position_id`, `canonical_symbol`, `source_symbol`, `direction`, open/close times, entry/exit VWAP, volumes, PnL breakdown, deal/order counts, SL/TP initial+final, `was_scaled_in` / `was_partial_close` / `was_averaged_down`, `completed`).
3. **Lifecycle rules**
   - Open, scale-in, partial close, full close, reversal.
   - Partial close is **not** a new trade.
   - SL/TP modification is **not** a trade.
4. **Exact meaning of “first 3 trades”** (§15)
   - Count only **3 completed reconstructed XAUUSD position lifecycles**.
   - Trade #3 closure emits `EARLY_SCORE_ELIGIBLE`, **never** `PROVEN_PROFITABLE`.
5. **Ordering for trade number n** — recommend `ORDER BY closed_at, opened_at, id` (locked in A22); same order must be used by scoring.
6. **Input** — normalized deals from raw `mt5_deals` (not dashboard aggregates).
7. **Output tables** — `reconstructed_trades` (+ link back to deals). Incomplete reconstructions stay `completed=false` and are excluded from first-3 / scoring.
8. **Failure / quality** — reconstruction_failures metric; do not silently drop scale-ins.
9. **Required tests** (§60): partial close, scale-in, full close, reversal, first-3 counter fixtures, replay of historical MT5 events.

### Must not include

- Score formulas.
- Destination OrderQty conversion (file 4 + file 8).
- Treating deal_count==3 as three trades.

### Distill from

A22 §2 (eligible trade), Domain `TradeReconstructor`, A09 reconstruction tests.

### Done when

A fixture of mixed deals produces a stable trade count and Trade #3 meaning that scoring and the dashboard can share.

---

## 7. File 4 — `docs/xauusd-normalization.md`

**Audience:** instruments / FIX / collector.  
**Primary law:** §16–17, §30, §38.  
**A30:** Increment 4; “never guess FIX tag 55.” Record real broker aliases against `mt5_symbols`.

### Must include

1. **Problem** — source symbols vary: `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`. Destination is a **numeric cTrader instrument ID**.
2. **Canonical instrument** — one row: `CanonicalInstrument = XAUUSD`.
3. **Two mapping tables**
   - `source_symbol_mappings`: `(broker_id, source_symbol) → canonical XAUUSD` (or unmapped).
   - `destination_symbols`: cTrader instrument ID + name + digits/precision + qty step/min/max → canonical XAUUSD.
4. **Unmapped policy** — unknown source symbol stays unmapped. It does **not** become XAUUSD by string contains `"XAU"` or `"GOLD"` without an explicit reviewed row.
5. **Never assume FIX tag 55 is the string `"XAUUSD"`.** Discover via Security List on the live venue account; persist; do not copy an instrument ID from another cTrader account (§16, §30, §72.13).
6. **Volume / quantity** — never `0.10 MT5 lots = 0.10 OrderQty` (§38). Document the conversion chain: source volume → canonical notional/risk → allocation → destination qty (min/step). Point to unit-test examples. Cite A38.
7. **Price source / feature quality** (§17) — MFE/MAE/spread/vol need ticks **while the source trade is open**.
   - Preferred: source MT5 tick subscription → `mt5_xau_ticks`, `price_source=…_MT5_TICKS`, `feature_quality=EXACT`.
   - If missing: store best available feed explicitly; `APPROXIMATE` / `UNAVAILABLE`.
   - **Never** silently use cTrader quotes as if they were Achiever/StarwaveFX ticks.
8. **Operator procedure** — how to add a new alias; who may change mappings (RBAC §59: not ReadOnly).

### Must not include

- Full Security List message spec (summary + pointer to `ctrader-fix.md`).
- Fabricated MFE/MAE recipes from OHLC of another venue.

### Distill from

A38, A22 I7, A32/A25 instrument discovery.

### Done when

A table of known Achiever + StarwaveFX gold aliases exists, unmapped symbols are visible, and destination ID is documented as discovered-not-guessed.

---

## 8. File 5 — `docs/scoring.md`

**Audience:** scoring / dashboard / risk.  
**Primary law:** §15, §18, §22–23.  
**A30:** Increment 5. “formula + state machine; no ML claims.”

### Must include

1. **Purpose** — deterministic statistical baseline **before** XGBoost. This is the benchmark ML must beat out-of-sample (§18, §21).
2. **Non-goals** — no XGBoost, no learned weights, no ranking by raw first-3 dollar PnL, no live send path.
3. **Universe** — completed reconstructed XAUUSD trades only (same eligibility as A22).
4. **Outputs** — `risk_score`, `behavior_score`, `early_quality_score` plus a **single** trader state.
5. **Feature inputs** (§18 list): net pnl, profit/loss ratio, lot consistency, loss-size consistency, martingale, averaging down, holding time, SL use, MFE/MAE only if `feature_quality==EXACT`, risk escalation after loss, drawdown, frequency, session.
6. **Trade #3 gate** (§23) — high score → **SHADOW only**. Forbidden at n==3: `LIVE`, `LIVE_CANDIDATE`, `PROVEN_PROFITABLE`.
7. **State vocabulary** (§22): `INSUFFICIENT_DATA`, `EARLY_SCORE`, `WATCH`, `SHADOW`, `LIVE_CANDIDATE`, `LIVE`, `PAUSED`, `RISK_BLOCKED`, `DISQUALIFIED`.
8. **Continuous rescoring** — first official score at trade #3, then after 4, 5, 6, … Persist `trader_scores` + `trader_score_history`. Freeze a Trade-#3 snapshot for later ML comparison.
9. **As-of / leakage** — score at T uses only information available at T (§20 applied to baseline).
10. **Authority** — scores are candidate/confidence/suggested allocation only. Risk engine is final (§39).
11. **Config** — versioned `ScoreConfig` / `baseline.v1`; do not hardcode “optimal” thresholds before backtest (§23).
12. **Tables** — `trader_feature_snapshots`, `trader_scores`, `trader_score_history`, `trader_states`, `trader_risk_flags`.
13. **Tests** — state transitions, martingale/averaging-down detectors, refuse fabricated MFE/MAE.

### Must not include

- Training pipeline, chronological split details, XGBoost hyperparams (those belong in `ml.md`).
- Copy sizing or FIX flags.

### Distill from

**A22 is the formula lock.** `scoring.md` should be the public/shorter product doc that points at A22 for exact equations rather than forking numbers.

### Done when

A reader can implement or review `baseline.v1` and cannot conclude that trade #3 funds live capital.

---

## 9. File 6 — `docs/ml.md`

**Audience:** research / later Phase 6.  
**Primary law:** §19–21, §67 Phase 6, §69, §71.  
**Write a stub that forbids premature ML**, then expand when Phase 3–5 data is proven.

### Must include

1. **When this doc becomes active** — only after the first useful version (§69) works **without** ML. Phase 6 deliverable, not Phase 0.
2. **Objective**
   - Input: features observable through completed trade #3.
   - Target: future **execution-venue-net** profitability on trades #4–#23 under drawdown constraint.
   - Initial label: `1` iff `future_net_copy_pnl > 0` AND `future_max_drawdown <= limit`, else `0`.
3. **Model** — XGBoost first. No DNN, no RL, no LLM API (§19, §71).
4. **Leakage protection** (§20) — a trade-#3 sample may not include challenge result, future balance/DD, trade #4+, future market data. Chronological split example: oldest 70% train / next 15% val / newest 15% untouched test.
5. **Evaluation** (§21) — do not optimize raw accuracy. Report top 1/5/10/20% on future copied PnL, max DD, profit factor, vol, CVaR, trade count, execution cost, slippage sensitivity. Baselines: all, random, rules, highest historical PnL, highest win-rate. **ML is justified only if it beats the deterministic baseline out-of-sample.**
6. **Serving rules** — `model_versions`, `model_predictions`, `model_evaluations`. Promotion is a privileged audited action (§59). **No automated self-promotion** (§71). ML never bypasses risk (§72.15).
7. **ML unavailable** (§62) — keep ingest/reconstruction; do not promote new live traders on missing scores; hard risk stays on.
8. **Stack** — Python, FastAPI, XGBoost, scikit-learn, Polars, NumPy; MLflow optional later; path `services/ml-service` when created.
9. **Non-goals** — clustering-as-skill, ranking by first-3 cash, using destination quotes as source features.

### Must not include

- A claim that ML is already in the product.
- Training notebooks committed with account secrets.

### Distill from

v2 §19–21 only, until a Phase 6 research note exists.

### Done when (stub)

The file exists, states “do not train until §69 is true,” and defines objective/leakage/eval so a later data scientist cannot “just ship XGBoost.”

---

## 10. File 7 — `docs/shadow-copy.md`

**Audience:** copy-engine / risk / dashboard.  
**Primary law:** §24, §31, §36–38, §63–64.  
**A30:** Increment 8/9. “pricing uses destination quotes only.”

### Must include

1. **Purpose** — model the **destination venue** (Pepperstone cTrader/cServer) as closely as practical **before** any real `NewOrderSingle`. Default action after a strong early score is SHADOW (§23).
2. **Input** — source MT5 trade **after** reconstruction/outbox. Never a raw Manager callback.
3. **Pricing** — **cTrader QUOTE session only** for entry, spread, freshness, slippage, delay, partial-fill assumptions, swap/commission model. Source tape is not the shadow fill price.
4. **Persist** (canonical §45 names): `shadow_orders`, `shadow_fills`, `shadow_positions`, `shadow_performance` (aliases in §24: order/fill/position/pnl/source_vs_shadow_slippage — do not create a second set).
5. **CopyIntent** — created and persisted **before** shadow or live work. Fields include `expires_at`, `max_signal_age` (§63).
6. **Timing** (§36) — record `source_event_time`, `collector_receive_time`, `decision_time`, (shadow) send/fill times. Reject stale entries by configurable policy. Measure the latency chain even in shadow.
7. **OPEN vs REDUCE/CLOSE** (§64) — separate policies: `OPEN_EXPOSURE`, `INCREASE_EXPOSURE`, `REDUCE_EXPOSURE`, `CLOSE_EXPOSURE`. Stricter on open/increase. Source close of a copied (or shadowed) position is risk-reduction, not a new entry.
8. **No blind catch-up** (§63) — if QUOTE/processing was down, do **not** fire 20 stale opens. Closings may use a different expiry policy.
9. **Sizing** — same normalized layer as live (§38); shadow must use destination min/step so live later matches.
10. **Risk still runs** — shadow should call the same risk engine in a `SHADOW` mode so rejects/flags are visible without sending FIX TRADE.
11. **Feature flags** — QUOTE on, TRADE session may be on for read, `REAL_COPY_EXECUTION_ENABLED=false`.
12. **Tests** — shadow fill from quote, PnL, stale intent expiry, idempotent intents, replay.

### Must not include

- ClOrdID / NewOrderSingle send procedures (those are `ctrader-fix.md` + `risk.md`).
- Using source mid as fill “because QUOTE was down.”

### Distill from

**A24** (binding shadow spec). Keep `docs/shadow-copy.md` shorter and operator-facing; do not fork policy numbers.

### Done when

An operator can explain why shadow PnL can differ from source MT5 PnL and which table holds that drift.

---

## 11. File 8 — `docs/risk.md`

**Audience:** risk managers + execution engineers.  
**Primary law:** §32–41, §62–65, §53, §59, §68, §70.

### Must include

1. **Authority** — scoring/ML emit only `candidate`, `confidence`, `suggested allocation`. Risk decides `approve` / `reduce size` / `reject` / `pause trader` / `pause venue` / `global stop` (§39).
2. **Production flow** (§32) — Source event → copy candidate? → persist CopyIntent → RiskEngine → persist ApprovedExecutionIntent → FIX worker → NewOrderSingle → ExecutionReports → positions → reconcile. **Never FIX from an MT5 callback.**
3. **Hard limits table** (every §39 bullet, each with config key, default “conservative / TBD from data”, reject code):
   - max loss per selected trader
   - max daily execution-account loss
   - max portfolio drawdown
   - max XAUUSD gross / net exposure
   - max position quantity / max open positions
   - max allowed spread / max quote age / max source-signal age
   - max tolerated price move / max slippage
   - max execution-account margin usage
   - martingale block / abnormal sizing block
   - venue health requirement
4. **Pre-trade price guard** (§37) — compare expected dest price, current dest quote, source price. Reject `PRICE_MOVED_TOO_FAR`, `QUOTE_STALE`, `SPREAD_TOO_WIDE` (XAU news).
5. **Kill switch split** (§40) — `STOP_NEW_EXECUTION` vs separately permissioned `EMERGENCY_FLATTEN`. Do not conflate. Flatten needs stronger auth + confirm + audit.
6. **Feature flags** (§41):

   ```text
   CTRADER_FIX_ENABLED=true
   CTRADER_FIX_QUOTE_ENABLED=true
   CTRADER_FIX_TRADE_SESSION_ENABLED=true
   REAL_COPY_EXECUTION_ENABLED=false
   ```

   NewOrderSingle requires the last flag **and** runtime risk healthy **and** reconciled.

7. **Intent expiry / no catch-up** (§63) and **OPEN vs CLOSE policy** (§64).
8. **Correlation / concentration** (§65) — Phase-2-after-stable-copy: cap allocation per correlated cluster (direction, entry time, hold, returns, session, lot behavior). Document as **not required for first useful version**, but reserve the flags table.
9. **Failure rules** (§62) — QUOTE down: no new live that needs fresh price. TRADE down: no unlimited stale backlog; no blind resend. DB down: **fail closed**. ML down: limits stay on.
10. **RBAC** (§59) — who may change limits, pause, stop-new, flatten. Every override → `audit_logs`.
11. **Tables** — `copy_intents`, `copy_allocations`, `risk_decisions`, `risk_events`.
12. **Tests** — each hard limit, stale quote/signal, kill switch, fail-closed.

### Must not include

- FIX header tag mapping (file 9).
- Baseline score algebra (file 5).

### Distill from

**A23.** Product `risk.md` is the operator/runbook view of that spec.

### Done when

A RiskManager can operate limits and kill switches from the doc + dashboard without reading QuickFIX code.

---

## 12. File 9 — `docs/ctrader-fix.md`

**Audience:** FIX engineers.  
**Primary law:** §25–35, §41, §56, §61.  
**A30:** first useful version is **QUOTE-only runbook**; expand TRADE read in Phase 7 and send in Phase 8. One file, clearly sectioned so QUOTE can ship first.

### Must include

1. **Venue statement** — Pepperstone / cServer FIX 4.4, account `1369850`, host `live-us-eqx-01.p.c-trader.com`. Not an LP.
2. **Two independent sessions** (§27) — `CTraderQuoteSession` and `CTraderTradeSession` with separate connection, sequences, heartbeats, timestamps, reconnect, metrics, logs. **Do not share a sequence counter.**
3. **Production ports** — QUOTE TLS `5211`, TRADE TLS `5212`. Plain `5201`/`5202` must not be the production default (§25).
4. **Header mapping warning** (§26, critical)
   - Do not infer tag placement from the human-readable form (`SenderSubID = QUOTE/TRADE`).
   - Make **both** `SenderSubID` and `TargetSubID` configurable.
   - Preserve issued case (`cServer` ≠ `CSERVER` unless spec/credential requires).
   - Prove Logon for **both** sessions in diagnostics before any execution.
5. **Secret-safe config block** — copy **placeholders** from §56 only (`<SECRET>`, `<BROKER_ISSUED_VALUE>`).
6. **Engine** — QuickFIX/n + **cTrader Rules-of-Engagement dictionary**. Generic FIX 4.4 dictionary is not sufficient. Do not write a raw `TcpClient` engine (§1.8, §5).
7. **Single-active TRADE ownership** (§28) — singleton / DB advisory lock / Redis lease+fencing. On leader change: establish session → reconcile → only then accept intents. DB remains authority for execution state. No dual production TRADE sessions.
8. **Minimum message set** (§29) — Logon/Logout/Heartbeat/TestRequest/Resend/Reject; SecurityListRequest/List; MarketDataRequest/Snapshot/Incremental; NewOrderSingle; ExecutionReport; OrderStatusRequest; OrderMassStatusRequest; RequestForPositions; PositionReport; OrderCancelRequest/Reject; OrderCancelReplaceRequest; BusinessMessageReject. “Send market order” is **not** a complete integration.
9. **Instrument discovery** (§30) — Security List after session up → persist XAU instrument id, name, digits.
10. **QUOTE feed** (§31) — latest bid/ask, receive ts, venue ts, symbol id; risk rejects stale quotes.
11. **Idempotent send** (§33) — persist `execution_intent_id`, `cl_ord_id`, source keys, qty, status **before** send. States: not sent / sent-ack-unknown / accepted / partial / filled / rejected / cancelled. **Never retry NewOrderSingle because TCP broke — reconcile first.**
12. **Unknown execution state** (§34) — `EXECUTION_STATE_UNKNOWN` → OrderStatus / MassStatus / ERs / positions. Only then decide if another order is required.
13. **Source↔destination mapping** (§35) — reconstructed trade → dest orders → dest position id(s). Support scale-in, partial close, full close, reversal. Persist cTrader Position ID.
14. **Harness** (§61) — adapter test mode: replay ERs and MD, simulate disconnect, dup ER, partial, reject, unknown-state. **Do not use the real account as the first integration test.**
15. **Official links** (§74) — https://help.ctrader.com/fix/ and specification / send-recv / FAQ. Plus QuickFIX/n.
16. **Metrics** — the §58 FIX block.

### Must not include

- Pasting the entire Spotware RoE.
- Hardcoded instrument IDs from another account.
- Real FIX password.

### Distill from

A25, A31–A34. Keep runbook + this-repo config here; leave full RoE extracts in A31–A34.

### Done when

A FIX engineer can configure two TLS sessions, know the header warning, and refuse to enable `REAL_COPY_EXECUTION_ENABLED` until Logon + harness exist.

---

## 13. File 10 — `docs/reconciliation.md`

**Audience:** execution + ops.  
**Primary law:** §42–44, §34, §43 alerts, §70.4–14, §72.10.

### Must include

1. **Rule** — never assume the DB is correct after process restart or FIX reconnect. Reconciliation **blocks** new execution while inconsistent (§42, §70.14).
2. **Startup TRADE reconcile** (§42):

   ```text
   Login successful
     → block new executions
     → OrderMassStatusRequest
     → RequestForPositions
     → consume Execution/Position reports
     → compare with internal DB
     → repair/update state
     → only if reconciled: READY_FOR_EXECUTION
   ```

3. **Periodic / daily** (§43) — compare internal open orders + dest positions vs cServer. Alert:
   - unknown external position
   - missing internal position
   - quantity mismatch
   - side mismatch
   - orphan execution report
   - unexpected fill
4. **Unknown-state recovery** — this file owns the **ops procedure**; `ctrader-fix.md` owns the message sequences. Link both ways.
5. **MT5-side reconcile** (short section + pointer to `mt5-integration.md`) — source positions/deals vs DB; do not mix into the same run as cTrader without a discriminator.
6. **Tables** — `execution_reconciliation_runs`, `execution_reconciliation_issues`, plus reads of `fix_orders`, `fix_execution_reports`, `destination_positions`, `source_destination_links`.
7. **Dashboard** — what §54 must show; no secrets.
8. **Go-live checks** — §68 “reconciliation works after restart”; §70 items 3, 6, 10, 14.
9. **Tests** — restart reconcile, unknown-execution recovery, duplicate ER, position mismatch fixtures (§60, §61).

### Must not include

- Shadow-vs-source PnL analysis (that is `shadow-copy.md`).
- Enabling flatten as part of “repair” without RBAC.

### Distill from

A23/A25 reconcile chapters, A20 extra tables.

### Done when

On-call can run the startup checklist and knows the system must stay `NOT READY_FOR_EXECUTION` on mismatch.

---

## 14. File 11 — `docs/deployment.md`

**Audience:** whoever runs Windows workers + Linux services.  
**Primary law:** §5 Deployment, §55–59, §62, §67–70.

### Must include

1. **Topology**
   - Windows worker host for native MT5 Manager DLL (`apps/mt5-worker` + `mt5-sdk`).
   - Linux (or Docker) acceptable for API, Postgres, Redis, React, later Python.
   - **Do not** force the native MT5 SDK into Linux containers if it does not run cleanly (§5).
2. **What not to deploy yet** — Kafka, Kubernetes, ClickHouse, mesh, cross-region active-active FIX (§71).
3. **Processes** — `apps/api`, `apps/mt5-worker`, `apps/fix-worker`, `apps/web`; one active TRADE owner.
4. **Data stores** — PostgreSQL = authority; Redis = cache, dashboard, **FIX session lease only**. Never Redis as order/position/balance store.
5. **Secret handling** (§55–56) — env / OS secret store / Vault. `.env.example` placeholders only. Production secrets not in git. Dashboard never receives MT5/FIX/DB/Redis passwords.
6. **Achiever egress** — outbound IP `81.29.145.69`; proxy credentials if used; never log them.
7. **FIX transport** — TLS default; sequence/store file locations; backup/restore of sequence files; one TRADE session.
8. **Feature-flag matrix** — connect/read vs `REAL_COPY_EXECUTION_ENABLED`.
9. **Logging / metrics / health** — Serilog + OpenTelemetry; §57 identifiers; §58 metric names; System Health page.
10. **RBAC deploy notes** — SuperAdmin / RiskManager / Analyst / ReadOnly; privileged actions audited.
11. **Failure / fail-closed** — DB down stops new real orders; do not run live execution only from memory (§62).
12. **Phase / gate checklists** — **link** A28 / §67–70; do not maintain a drifting copy. Include the §68 box as a pre-live gate that ops must evidence.
13. **First useful version** — §69 (no ML, no live NewOrderSingle).
14. **Live FIX acceptance** — §70 before flipping the real-copy flag.

### Must not include

- Real passwords, connection strings with credentials, or “Phase 1 Done” fiction.
- A Kubernetes manifest “for later.”

### Distill from

A19 (secrets scan), A28 (gates), A30 Increment 0 (compose: postgres+redis only).

### Done when

Ops can stand up Postgres/Redis/API/workers with secret-safe config and knows the exact flag that still keeps live orders off.

---

## 15. Recommended write order (do not write all on day one as fiction)

Align with §67 / A30. Files may be **stubbed** early with “not in force until phase X,” but must not claim features that do not exist.

| Order | File | When content becomes binding |
|---|---|---|
| 1 | `architecture.md` | Phase 0 — rewrite the non-compliant file first |
| 2 | `deployment.md` | Phase 0 closeout / Increment 0 (compose, secrets, topology) |
| 3 | `mt5-integration.md` | Phase 1 |
| 4 | `trade-reconstruction.md` | Phase 2 |
| 5 | `xauusd-normalization.md` | Phase 2 (same increment) |
| 6 | `scoring.md` | Phase 3 |
| 7 | `shadow-copy.md` | Phase 5 |
| 8 | `risk.md` | Phase 5 (shadow mode) then extend for Phase 8 |
| 9 | `ctrader-fix.md` | Phase 4 QUOTE section; Phase 7–8 TRADE sections |
| 10 | `reconciliation.md` | Phase 7 (cTrader); MT5 subset can land in Phase 1 inside this file or via pointer |
| 11 | `ml.md` | Stub in Phase 0–3 (“do not train yet”); full text only Phase 6 |

Optional non-§66: `docs/README.md` as a 20-line index that **points at the v2 spec and these 11 files**. Do not let it become a 12th architecture.

---

## 16. Cross-link map (keep one owner per topic)

```text
architecture.md          → index + pipeline + honest status
        ├─ mt5-integration.md
        ├─ trade-reconstruction.md
        │        └─ xauusd-normalization.md
        ├─ scoring.md  ──stub──► ml.md
        ├─ shadow-copy.md
        ├─ risk.md
        ├─ ctrader-fix.md
        │        └─ reconciliation.md
        └─ deployment.md
```

Shared topics (write once, link twice):

| Topic | Owner file | Also mentioned in |
|---|---|---|
| Compound identity | `architecture.md` | every data doc |
| First-3 definition | `trade-reconstruction.md` | `scoring.md` |
| Tag 55 / Security List | `xauusd-normalization.md` | `ctrader-fix.md` |
| Quantity conversion | `xauusd-normalization.md` | `risk.md`, `shadow-copy.md` |
| Stale signal/quote | `risk.md` | `shadow-copy.md`, `ctrader-fix.md` |
| Unknown execution | `ctrader-fix.md` | `reconciliation.md` |
| Kill switch | `risk.md` | `deployment.md` |
| Feature flags | `risk.md` + `deployment.md` | `ctrader-fix.md` |
| Secrets | `deployment.md` | all config examples |

---

## 17. Related swarm artifacts (inputs, not substitutes)

| Swarm file | Use when writing |
|---|---|
| `A01`–`A11` | Honest as-built map for `architecture.md` |
| `A12`–`A18`, `A37`–`A39` | `mt5-integration.md`, volume/groups |
| `A19` | `deployment.md` secret denylist |
| `A20` | table names in every persistence section |
| `A22` | `scoring.md` formulas |
| `A23` | `risk.md` |
| `A24` | `shadow-copy.md` |
| `A25`, `A31`–`A34` | `ctrader-fix.md` |
| `A26` | dashboard mentions only (no separate dashboard.md in §66) |
| `A27` | Tests subsections |
| `A28`, `A30` | `deployment.md` phases; write-order above |

There is **no** §66 file for the React dashboard. Dashboard contracts stay in A26 + code. `architecture.md` may link A26; do not create `docs/dashboard.md` unless a later architecture revision adds it.

---

## 18. Acceptance for the **docs set** (not for this outline)

The §66 documentation obligation is **not** discharged by this outline file.

The set is written when all of the following are true:

```text
[ ] docs/architecture.md rewritten; no longer contradicts §4/§67; points at v2 spec
[ ] docs/mt5-integration.md exists
[ ] docs/trade-reconstruction.md exists
[ ] docs/xauusd-normalization.md exists
[ ] docs/scoring.md exists (no ML claims)
[ ] docs/ml.md exists (at least the “do not train until §69” stub)
[ ] docs/shadow-copy.md exists
[ ] docs/risk.md exists
[ ] docs/ctrader-fix.md exists (QUOTE section complete before Phase 4 exit)
[ ] docs/reconciliation.md exists
[ ] docs/deployment.md exists
[ ] none of the eleven files contain real secrets
[ ] none of the eleven files is a paste of the v2 spec
[ ] v2 spec remains only at
    D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
```

**This agent’s deliverable is only** `D:\Prop\reports\swarm\20260818\A66_docs_outline.md`. Product source and `docs/*.md` were not modified here.
