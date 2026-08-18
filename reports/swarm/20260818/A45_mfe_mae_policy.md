# A45 — MFE/MAE `feature_quality` policy (Architecture §17)

**Artifact:** `D:\Prop\reports\swarm\20260818\A45_mfe_mae_policy.md`  
**Date:** 2026-08-18  
**Agent:** A45 (policy only)  
**Authority:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §17 (lines 735–782)  
**Reinforcing clauses:** §1.5 (lines 85–86), §11, §14, §18, §45, §51, §60, §67 Phase 3–5  
**Companion audits (read-only):** `A17_ticks_and_ledger.md`, `A09_unit_tests_audit.md`, `A27_test_inventory.md`, `A06_api_audit.md`, `A28_phases_gates.md`  
**Product source:** **not modified**

This file is the implementer-binding rule for `feature_quality` on source-side excursion features. It does not invent MFE/MAE numbers. It forbids silent mixing of source MT5 ticks with cTrader quotes.

---

## 1. Purpose

Architecture §17 exists so that **MFE, MAE, price excursion, entry spread, and in-trade volatility** are never published as if they were measured on the source book when they were not.

Two metadata fields are mandatory on every persisted excursion feature row:

```text
price_source
feature_quality
```

`feature_quality` is a closed set. The only legal published values are:

| Value | Meaning |
|---|---|
| `EXACT` | Computed from a **single** source-broker MT5 tick tape that covers the open window to the thresholds in §6. |
| `APPROXIMATE` | Computed from a **single**, explicitly poorer substitute (or an incomplete same-broker tick tape) labeled in `price_source`. |
| *(omit)* | No number is stored or returned. Quality is absent. Dashboard / API treat the feature as invalid. |

There is no `MIXED`. There is no `UNKNOWN` that still carries a number. There is no silent default to `EXACT`.

**Never mix `EXACT` and `APPROXIMATE` on one row. Never mix source MT5 ticks with cTrader quotes in one path. Never upgrade quality after the fact.**

---

## 2. Binding Architecture §17 (quoted requirement)

v2 §17, lines 735–782:

If we want exact features such as:

```text
MFE
MAE
price excursion
entry spread
volatility during trade
```

we need time-series price data **while each source trade is open**.

Preferred:

```text
MT5 source broker tick feed / Manager symbol tick subscription
```

If unavailable:

1. store the best available price feed **explicitly**,
2. mark the feature source,
3. **do not pretend** another broker's cTrader quote feed is identical to the source MT5 price stream.

Feature metadata must include:

```text
price_source
feature_quality
```

Architecture examples:

```text
price_source=ACHIEVER_MT5_TICKS
feature_quality=EXACT
```

or:

```text
price_source=BAR_APPROXIMATION
feature_quality=APPROXIMATE
```

**Never silently mix them.**

Reinforcing law:

| Location | Rule |
|---|---|
| §1.5 | Do **not** calculate MFE/MAE from closed deals alone. If source-side tick data is not available, **do not fabricate**. |
| §11 | Raw `mt5_ticks_xauusd` *if* the source SDK/feed supports it. Raw layer immutable as practical. |
| §14 `ReconstructedTrade` | No MFE/MAE fields. Reconstruction is deal/order/position lifecycle, not excursion. |
| §18 | Baseline **may** use MAE/MFE as score inputs — still subject to this quality law. |
| §45 | Core table `mt5_xau_ticks` (name differs from §11; unify in a later migration). Separate table `destination_quotes`. Separate table `trader_feature_snapshots`. |
| §51 | Dashboard shows **“MFE/MAE when valid”**. |
| §60 | Unit-test **“MFE/MAE where data exists”** — omit / refuse when data does not exist. |
| §67 Phase 4 | cTrader QUOTE persistence is **destination** market data. |
| §67 Phase 5 | Shadow fills use **destination** quotes. That is not a license to back-fill source MFE/MAE. |

Name inconsistency (`mt5_ticks_xauusd` vs `mt5_xau_ticks`) is unresolved. Neither table exists in product SQL in this tree. This policy uses **source tick tape** as the logical name and requires `broker_id` + `source_symbol` + broker time on every print.

---

## 3. Definitions

| Term | Definition |
|---|---|
| **Source trade window** | Closed interval `[opened_at, closed_at]` of one completed `ReconstructedTrade` on one `(broker_id, login, position_id / lifecycle)`. Open trades use `[opened_at, as_of]` and can never be published as `EXACT` until close *or* until a documented open-trade snapshot policy is added (none exists today — omit). |
| **Source book** | The MT5 server the trader was filled on (Achiever **or** StarwaveFX **or** a future `broker_id`). Ticks from any other server are a different book. |
| **Source MT5 ticks** | Bid/ask(/last) prints from that source book: Manager `TickSubscribe` / `TickHistoryRequest` / durable `mt5_xau_ticks` row with matching `broker_id` and mapped `source_symbol`. |
| **cTrader quotes** | FIX 4.4 QUOTE-session market data (`MarketDataSnapshotFullRefresh` / `MarketDataIncrementalRefresh`) persisted in `destination_quotes`. Different venue, different book, different clock. |
| **Silent mix** | Any computation that consumes more than one `price_source` (or more than one `broker_id`) and emits a single MFE/MAE **without** refusing, **or** that writes a single `price_source` that does not name every contributing feed. Gap-fill of a source tape with destination quotes is a silent mix even if someone later stamps `APPROXIMATE`. |
| **Fabrication** | Emitting MFE/MAE from inputs that are not a price path over the window (closed deals only, entry+exit VWAP, last mark, session high/low, interpolated ticks, “typical XAU range”). |
| **Valid (dashboard / API)** | `feature_quality ∈ {EXACT, APPROXIMATE}` **and** both MFE and MAE are non-null **and** `price_source` is a catalog value from §5. Otherwise `mfeMaeValid = false` and values are null. |

`ReconstructedTrade` as it exists today (`D:\Prop\src\Domain\Entities\ReconstructedTrade.cs`) correctly has **no** MFE/MAE fields. Do not add them. Excursion lives on `trader_feature_snapshots` (or an equivalent feature row), never on the lifecycle record.

`Mt5Deal.Price` is one fill. `Mt5Position` / `PositionData.price_current` is one mark. Neither is a path.

---

## 4. Closed `feature_quality` catalog

Implement as a Domain enum (name suggestion: `FeatureQuality`). Do not use free-text. Do not store integers without the enum.

### 4.1 `EXACT`

All of the following must be true. If any clause fails, the row is **not** `EXACT`.

1. **Single source.** Exactly one `price_source` from the `*_MT5_TICKS` or `*_MT5_TICK_HISTORY` families in §5.
2. **Same broker.** Every tick’s `broker_id` equals the reconstructed trade’s `broker_id`. Achiever ticks never score a StarwaveFX trade (or the reverse).
3. **Same book symbol.** Ticks are for the trade’s `source_symbol`, or for a symbol that `SourceSymbolMapping` maps to the same canonical instrument **on that same broker**. Canonical `XAUUSD` is not a license to pull another broker’s gold tape.
4. **Bid and ask present.** Every tick used for excursion has finite `bid > 0` and `ask > 0` and `ask >= bid`. Last-only prints may be stored but do not admit `EXACT`.
5. **Broker time, not wall clock.** `time_msc` is MT5 server time. Wall-clock backfill (see `MT5TickBridge::onSdkTick` when SDK datetime is zero — A17) disqualifies those prints from `EXACT`. If any print in the window has a fabricated timestamp, the row cannot be `EXACT`.
6. **Window coverage (policy thresholds; architecture does not give numbers):**
   - first usable tick at or before `opened_at + 1s`
   - last usable tick at or after `closed_at - 1s`
   - no intra-window gap `> 2s` during a continuous XAU session
   - weekend / daily session-break gaps are **not** counted if the source market was closed (use MT5 symbol session, not a guess)
   - `dropped_ticks` / queue-overflow / “drain with no sink” counted in the window = **0**
7. **Not a poll snapshot series.** `GetTickLast` / 250 ms / 64-symbol poll is never `EXACT` (A17).
8. **Not bars.** `GetChart` OHLC is never `EXACT`.
9. **Not destination.** No cTrader quote, no shadow mark, no FIX `MDEntryPx` enters the path.
10. **Not deals.** Entry VWAP, exit VWAP, deal `price`, and realized PnL do not enter the path except as the **entry reference** (see §8). The path itself is ticks.

`EXACT` means: “this number is the excursion of **this** trader’s **source** book while the position was open, to the coverage bar above.” It does not mean “we have some gold prices from somewhere.”

### 4.2 `APPROXIMATE`

All of the following must be true.

1. **Single labeled substitute or incomplete same-broker series.** Exactly one `price_source` from the APPROXIMATE-capable rows in §5.
2. **Explicit label.** `price_source` names the actual feed (`BAR_APPROXIMATION`, `{BROKER}_MT5_TICK_POLL`, `{BROKER}_MT5_TICK_HISTORY` with gaps, `{BROKER}_MT5_TICKS` that failed §4.1 coverage). Never write `ACHIEVER_MT5_TICKS` + `APPROXIMATE` unless the ticks really are Achiever ticks (incomplete). Never write `ACHIEVER_MT5_TICKS` when the series is bars or cTrader.
3. **Still one book.** Incomplete Achiever ticks stay Achiever. Do not patch holes with StarwaveFX, bars, **or** cTrader quotes and keep the same `price_source`.
4. **Minimum evidence.** At least one of:
   - same-broker ticks covering ≥ 50% of the open window by time, **or**
   - same-broker OHLC bars whose union covers the open window (bar open ≤ `opened_at`, last bar close ≥ `closed_at`), **or**
   - same-broker poll samples with `n >= 3` distinct timestamps inside the window.
   Below that: **omit**, do not stamp `APPROXIMATE` on a two-point deal path.
5. **Consumers treat it as approximate.** Scoring, ranking, and the dashboard must not present it as exact. See §10.

`APPROXIMATE` is honest degradation. It is not a dump bucket for mixed tapes.

### 4.3 Omit (no quality, no number)

Omit when:

- no source-side series exists for the window
- only closed deals / positions / session `TickStat` highs and lows exist
- the only available prices are cTrader / destination quotes
- any mix of two `price_source` values would be required to cover the window
- coverage fails both §4.1 and §4.2
- the calculator cannot prove `broker_id` on the tape
- the trade is still open and no open-trade snapshot policy has been approved

Omission is the **correct** outcome. It is not a bug. §1.5 / §51 / §60 all prefer null over a lie.

Do **not** emit `0`, `abs(exit_vwap - entry_vwap)`, or `null` quality with a non-null number.

---

## 5. Closed `price_source` catalog

Implement as a Domain enum (name suggestion: `PriceSource`). Values are uppercase snake. `{BROKER}` is the broker code already used on the ledger (`ACHIEVER`, `STARWAVEFX`, …), not a display name.

| `price_source` | May be `EXACT`? | May be `APPROXIMATE`? | Legal use for **source** MFE/MAE? | Notes |
|---|---|---|---|---|
| `ACHIEVER_MT5_TICKS` | Yes, if §4.1 | Yes, if §4.2 (gaps) | **Yes**, Achiever trades only | Architecture example. Manager push + durable writer, no unexplained drops for `EXACT`. |
| `STARWAVEFX_MT5_TICKS` | Yes, if §4.1 | Yes, if §4.2 | **Yes**, StarwaveFX trades only | Same family, other book. |
| `{BROKER}_MT5_TICKS` | Yes, if §4.1 | Yes, if §4.2 | **Yes**, that broker only | Future brokers. Do not invent a code per symbol. |
| `{BROKER}_MT5_TICK_HISTORY` | Yes, if complete | Yes, if gappy | **Yes**, that broker only | `TickHistoryRequest` / `TickHistoryRequestRaw`. Not wrapped on `IMT5Client` today (A17). |
| `{BROKER}_MT5_TICK_POLL` | **No** | Yes, if `n >= 3` | Conditional | `GetTickLast` / 250 ms / ≤64 symbols. Usually too sparse for XAUUSD — prefer omit. |
| `BAR_APPROXIMATION` | **No** | Yes | Conditional | `GetChart` OHLC over the window. High/low of bars ≠ tick MFE/MAE. Architecture example. |
| `CTRADER_FIX_QUOTES` | **No** | **No** (for source features) | **No** | Destination book only. May label **destination / shadow** excursion in a **separate** column family. Never written into source MFE/MAE. |
| `DEAL_PATH` / `ENTRY_EXIT_ONLY` | **No** | **No** | **No** | Forbidden. That is fabrication (§1.5). |
| `MIXED` / `UNKNOWN` / `BEST_AVAILABLE` | **No** | **No** | **No** | Forbidden. “Best available” without a name is a silent mix. |

One feature row, one `price_source`. If a future design needs both a source MFE and a destination excursion, that is **two rows or two column families**, each with its own pair `(price_source, feature_quality)`.

---

## 6. Isolation law — never mix (binding)

This is the whole point of §17 sentence 3 and “Never silently mix them.”

### 6.1 What “mix” means

A mix occurs if any of the following happen inside one source-MFE/MAE computation:

1. Source MT5 ticks **and** cTrader FIX quotes in the same path (including gap-fill).
2. Achiever ticks **and** StarwaveFX ticks in the same path.
3. Ticks **and** bars in the same path (e.g. ticks where present, bar high/low in the holes).
4. Push ticks **and** poll snapshots silently concatenated and labeled as `{BROKER}_MT5_TICKS`.
5. Two candidate MFEs computed (source vs dest, or ticks vs bars) and the larger/smoother one kept.
6. Mid = average(source mid, dest mid), or any cross-venue blend.
7. `price_source` set to the preferred feed while some samples came from another.
8. `feature_quality=EXACT` while any contributing sample would only have admitted `APPROXIMATE` or omit.

### 6.2 Required behavior when two tapes exist

| Situation | Required action |
|---|---|
| Source ticks admit `EXACT` | Use **only** source ticks. Ignore cTrader quotes for this feature. |
| Source ticks admit `APPROXIMATE` only | Use **only** that source series. Do **not** improve coverage with cTrader. |
| Source ticks missing; bars exist | `BAR_APPROXIMATION` + `APPROXIMATE`, **or** omit. Never “upgrade” with dest quotes. |
| Source ticks missing; only cTrader quotes exist | **Omit** source MFE/MAE. Optionally compute a **destination** excursion under `CTRADER_FIX_QUOTES` in a separate field (`dest_mfe` / `shadow_mfe`), never in `mfe` / `mae`. |
| Source ticks missing; only deals exist | **Omit**. |
| Partial source ticks + dest quotes that would complete the window | **Do not complete the window.** Either `APPROXIMATE` on the partial source series if §4.2 passes, or omit. |

cTrader quotes are the correct tape for:

- Phase 4 quote cache / dashboard venue health
- Phase 5 shadow fill marks and shadow P&L
- Risk-engine `max quote age` / `max allowed spread` on the **destination**
- Source-vs-shadow **analysis** (two labeled series compared, not fused)

They are the wrong tape for source-trader skill features.

### 6.3 Quality is min, never max — and mix is still forbidden

If a future change ever allowed multiple **same-family** inputs (it should not), quality would be the **worst** contributing quality. That clause is defensive. **Mixing families remains forbidden** even if the result would be stamped `APPROXIMATE`. `APPROXIMATE` is not a pardon for a silent mix.

### 6.4 Clock and identity isolation

- Do not align a source fill to a destination quote by wall clock and call the dest print a source observation.
- Do not key ticks by canonical symbol alone. Key at least `(broker_id, source_symbol, time_msc)` (plus payload hash / source id when persisted).
- Do not reuse a tick row across brokers because the millisecond and bid match.

---

## 7. Admission algorithm (calculator must implement this order)

Pseudocode — policy, not product source:

```text
inputs: ReconstructedTrade t, tick tape T, optional bars B, optional dest quotes Q

assert T, B, Q each carry broker_id / venue id
reject if T contains more than one broker_id
reject if any sample in T is a cTrader quote

if T is same-broker ticks and passes §4.1:
    return compute(t, T) with price_source={BROKER}_MT5_TICKS (or _TICK_HISTORY),
           feature_quality=EXACT

if T is same-broker ticks/poll/history and passes §4.2:
    return compute(t, T) with the true price_source, feature_quality=APPROXIMATE

if B is same-broker bars covering the window:
    return compute_from_bars(t, B) with price_source=BAR_APPROXIMATION,
           feature_quality=APPROXIMATE

# Q is never consulted for source MFE/MAE
return omit   # mfe=null, mae=null, no feature_quality, mfeMaeValid=false
```

Fail closed. No fallback that “just uses whatever prices we have.”

---

## 8. Computation rules (only after admission)

These formulas apply once a **single** admitted series exists. They do not create quality.

### 8.1 Entry reference

- Direction from `ReconstructedTrade.Direction`.
- Entry reference = `entry_vwap` (already reconstructed). Do not replace it with the first tick.
- Scale-ins: keep using trade `entry_vwap` for the completed-lifecycle snapshot unless a later spec defines running VWAP path features. Do not invent per-add MFE without a new labeled feature name.

### 8.2 Mark convention (source book)

Use the side the trader could have closed against on the **source** book:

| Direction | Favorable (MFE) | Adverse (MAE) |
|---|---|---|
| Long | max over window of `(bid - entry_vwap)` | max of `(entry_vwap - bid)` |
| Short | max over window of `(entry_vwap - ask)` | max of `(ask - entry_vwap)` |

Bid for a long close, ask for a short close. Do not use last, mid, or destination bid/ask for source `EXACT`.

Bars (`APPROXIMATE` only):

| Direction | MFE proxy | MAE proxy |
|---|---|---|
| Long | `max(high) - entry_vwap` | `entry_vwap - min(low)` |
| Short | `entry_vwap - min(low)` | `max(high) - entry_vwap` |

Bar high/low are not bid/ask. That is why quality cannot be `EXACT`.

Clamp negative MFE to 0 and negative MAE to 0 after the max. Persist in **price units** of the source symbol (XAUUSD dollars per ounce), not account currency PnL, unless a separately named `mfe_money` / `mae_money` is added with its own quality (same `price_source` / `feature_quality` pair). Do not convert with destination contract size.

### 8.3 Related §17 features

| Feature | `EXACT` extra rule | Else |
|---|---|---|
| Entry spread | `ask - bid` on a source tick within 1s of `opened_at` (first in-window tick if it qualifies) | omit or `APPROXIMATE` from first bar spread / first poll; never dest spread |
| In-trade volatility | computed on the same admitted series only (e.g. realized variance of mid or of the mark used above) | same quality as MFE/MAE; omit if series omitted |
| Price excursion | alias of the signed path; store with the same pair | — |

All four features on one snapshot share one `(price_source, feature_quality)`. Do not mark MFE `EXACT` and volatility `APPROXIMATE` from different tapes.

### 8.4 What must not be used as a path

From A17 fabrication blacklist, restated as policy:

1. `mt5_deals_ledger` / `Mt5Deal` / entry+exit VWAP as the high/low path.
2. `PositionData.price_current` or session `MTTickStat` bid_high / bid_low.
3. `GetTickLast` labeled `EXACT`.
4. Destination FIX quotes in `mt5_xau_ticks` or in source MFE columns.
5. Interpolated mid, Brownian bridge, last close, bar typical price labeled `EXACT`.
6. Overwriting a deal revision with computed excursion (breaks ledger immutability **and** §17).
7. Writing the tape from `MT5TickBridge::onSdkTick` (pump-thread constraint). Persistence is off-thread, later work.

---

## 9. Persistence and API contract

### 9.1 Feature row (`trader_feature_snapshots` or equivalent)

Every snapshot that may carry excursion stores:

```text
broker_id
login
reconstructed_trade_id
canonical_symbol
source_symbol

mfe                  # null if omitted
mae                  # null if omitted
entry_spread         # null if omitted
in_trade_volatility  # null if omitted

price_source         # null iff all four omitted
feature_quality      # null iff all four omitted

mfe_mae_valid        # false if omitted

tick_count           # 0 if omitted
coverage_ratio       # 0..1; null if omitted
max_gap_ms           # null if omitted
window_opened_at
window_closed_at
computed_at
```

Do not persist a number without the two metadata fields. Do not persist metadata without both MFE and MAE (compute both or omit both).

### 9.2 Tables that must stay separate

| Table (architecture names) | Allowed content | Forbidden content |
|---|---|---|
| `mt5_xau_ticks` / `mt5_ticks_xauusd` | Source MT5 ticks + `broker_id` | cTrader quotes, blended mids, fabricated prints |
| `destination_quotes` | FIX QUOTE-session XAU | Source MT5 ticks relabeled as dest |
| `mt5_deals` / ledger | Fills | Derived MFE/MAE |
| `reconstructed_trades` | Lifecycle | MFE/MAE columns |
| `trader_feature_snapshots` | Features + `price_source` + `feature_quality` | Unlabeled excursion |

### 9.3 API / dashboard (§51, A06 §5.4)

```text
mfe, mae          // number | null
mfeMaeValid       // boolean
priceSource       // string | null
featureQuality    // "EXACT" | "APPROXIMATE" | null
```

Rules:

- `mfeMaeValid == true` only when quality is `EXACT` or `APPROXIMATE` and both numbers are present.
- UI copy: **“MFE/MAE when valid.”** Invalid → hide or show an explicit “not available (no source tick window)” state. Never show `0.00` as a stand-in.
- `EXACT` and `APPROXIMATE` must be visually distinct if a number is shown (badge / subtitle). Do not render approximate as if it were exact.
- Do not invent fields in v1 to “complete” the chart.

---

## 10. Consumer rules

### 10.1 Deterministic baseline (§18)

MAE/MFE **may** enter `risk_score` / `behavior_score` / `early_quality_score` only when `mfe_mae_valid` is true.

Policy for the first baseline (Phase 3):

| Quality | Allowed in baseline? |
|---|---|
| `EXACT` | Yes. |
| `APPROXIMATE` | Yes only as a **separate**, documented input (e.g. `mae_approx`) or with a discount the score code records. Do not silently average with exact values across traders. Ranking that mixes exact and approximate MAE without a flag is a silent mix at the **population** level — forbidden. |
| omitted | Skip the input. Do not impute 0, median, or “typical gold MAE.” |

Traders with omitted MFE/MAE remain scorable on deal-supported features (net PnL, hold time, scale-in, averaging-down, lot consistency). That is the §1.5 / §18 intent.

### 10.2 Risk engine

Destination quote age / dest spread are **not** source MAE. Do not reject a copy because source MAE is omitted. Do not treat dest spread as source entry spread.

### 10.3 Shadow / live copy (Phase 5+)

Shadow marks come from `destination_quotes`. Source MFE/MAE stay source-labeled. Source-vs-shadow analysis compares two **named** series. A17 / A28 already require: “Shadow fills use destination quotes, not source ticks assumed equal.” The converse is this document: **source MFE/MAE use source ticks, not destination quotes assumed equal.**

### 10.4 ML (Phase 6)

No training target or feature named MFE/MAE may be built from unlabeled or mixed paths. Chronological split does not excuse a silent mix. If quality is omitted, the column is missing — not zero.

---

## 11. Test contract (§60, A09, A27)

Product tests are **not written** today (`MfeMaeCalculator` does not exist). When they are, they must prove this policy — not merely “a number comes out.”

| Test class (A27) | Must prove |
|---|---|
| `Features.MfeMaeCalculatorTests` | Window of same-broker ticks meeting §4.1 → numbers + `price_source={BROKER}_MT5_TICKS` + `feature_quality=EXACT`. |
| `Features.MfeMaeCalculatorTests` | Deals only → omit; **no** number; no `EXACT`. |
| `Features.MfeMaeCalculatorTests` | Bars only → `BAR_APPROXIMATION` + `APPROXIMATE`, or omit if bars do not cover the window. |
| `Features.MfeMaeMissingTickDataTests` | Missing source ticks + present cTrader quotes → **omit source MFE/MAE**. No Achiever/StarwaveFX/cTrader splice. |
| `Features.MfeMaeMissingTickDataTests` | Achiever ticks offered for a StarwaveFX trade → refuse / omit. |
| `Features.MfeMaeMissingTickDataTests` | Gappy source ticks + dest quotes that fill the holes → still no mix; `APPROXIMATE` on source only or omit. |
| `Replay.FeatureComputationFromReplayTests` | MFE/MAE only when the fixture includes a labeled source tick tape. |

Suggested fact names (extend A09; not implemented here):

```text
Computes_EXACT_when_source_tick_window_meets_coverage
Sets_APPROXIMATE_for_bar_approximation
Refuses_to_fabricate_from_closed_deals_only
Omits_when_only_ctrader_quotes_exist
Refuses_silent_mix_of_source_ticks_and_ctrader_quotes
Refuses_achiever_ticks_on_starwavefx_trade
Poll_snapshots_are_never_EXACT
Wall_clock_backfilled_timestamps_are_never_EXACT
Does_not_upgrade_APPROXIMATE_to_EXACT
```

A passing test that asserts only `mfe > 0` without asserting `feature_quality` is **not** a §60 test.

---

## 12. Current measured state (honest)

This policy is written against the tree as of 2026-08-18. Nothing below is a claim that excursion already works.

| Item | State |
|---|---|
| Architecture §17 | Binding; examples `ACHIEVER_MT5_TICKS`/`EXACT` and `BAR_APPROXIMATION`/`APPROXIMATE`; “never silently mix.” |
| Domain `FeatureQuality` / `PriceSource` | **Missing** |
| Domain `TraderFeatureSnapshot` | **Missing** (A01). `ReconstructedTrade` has no MFE/MAE (correct). |
| `MfeMaeCalculator` | **Missing** |
| Durable source tick tape | **Missing** (A17). `MT5TickBridge` is in-memory fan-out only. |
| `TickHistoryRequest` on `IMT5Client` | **Missing** (A17) |
| `destination_quotes` / FIX QUOTE session | **Not implemented** (Phase 4) |
| Fabricated MFE/MAE in product source | **Not found** (feature absent — omission ≠ fabrication) |
| Silent mix in product source | **Not found** (neither source MFE nor dest quotes are computed) |
| Unit tests | Named only. **Not written.** |

Safe today only because the calculator and dest quote path do not exist. The first implementation **must** land this policy in the same change as the calculator. Do not ship unlabeled numbers “temporarily.”

---

## 13. Disposition

| Metric | Value |
|---|---|
| Architecture section | §17 (plus §1.5, §11, §14, §18, §45, §51, §60) |
| `feature_quality` values | `EXACT` \| `APPROXIMATE` \| omit — **never mixed on one row** |
| `EXACT` | Same-broker source MT5 ticks, bid+ask, broker time, coverage §4.1, no dest quotes, no bars, no poll |
| `APPROXIMATE` | Single labeled poorer/incomplete source series (§4.2, §5) |
| Source MT5 ticks + cTrader quotes in one MFE/MAE | **Forbidden** (not even as `APPROXIMATE`) |
| Deals-only MFE/MAE | **Forbidden** |
| Product source changed | **No** |

**Rule to carry forward:** specify quality on every excursion feature; `EXACT` is source-book ticks only; `APPROXIMATE` is one named downgrade; **never** fill a source window with cTrader quotes or another broker’s tape; omit when the window is not there.
