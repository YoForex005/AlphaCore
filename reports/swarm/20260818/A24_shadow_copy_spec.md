# A24 — Shadow Copy Specification

**Artifact:** `D:\Prop\reports\swarm\20260818\A24_shadow_copy_spec.md`  
**Date:** 2026-08-18  
**Status:** Binding implementation spec (Phase 5). No product source was modified to produce this document.  
**Architecture law:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Primary sections:** 24, 31, 36–38, 63–64  
**Supporting sections:** 14, 16, 17, 23, 27, 32–35, 39–41, 44–45, 57–58, 60–62, 67 (Phase 4–5), 68–70, 72

This spec defines how the shadow-copy engine simulates **destination-venue** orders, fills, positions, and P&L using the **cTrader QUOTE FIX feed only**, and how **OPEN/INCREASE** policy is separated from **REDUCE/CLOSE** policy.

---

## 1. Purpose and non-goals

### 1.1 Purpose

Shadow copy must model the actual destination venue (Pepperstone cTrader / cServer) as closely as practical **before** any real `NewOrderSingle` is allowed (§24, §23, Phase 5).

Input is a **source MT5 trade event** (after reconstruction / outbox — never a raw MT5 callback). Pricing, slippage, freshness, and mark-to-market use the **destination QUOTE session**, not the source broker tape.

Persist at least:

```text
shadow_copy_order
shadow_copy_fill
shadow_position
shadow_pnl
source_vs_shadow_slippage
```

### 1.2 What shadow is

A **deterministic, durable simulator** that answers:

> If we had copied this reconstructed source event onto the destination XAUUSD instrument, at the destination bid/ask that was live then, after configured delay and costs, what order / fill / position / P&L would we have?

It is the evidence gate for later live copy (§23, §68).

### 1.3 What shadow is not

| Forbidden | Why |
|---|---|
| Sending FIX TRADE `NewOrderSingle` / cancel / replace | Shadow is QUOTE-only. `REAL_COPY_EXECUTION_ENABLED` stays false (§41, Phase 5). |
| Using source MT5 last/deal price as the shadow fill | That is source economics, not destination venue modeling (§24, §31). |
| Using destination quotes as if they were source ticks for MFE/MAE | Different feed; must not be silently mixed (§17). |
| `source_lots == destination OrderQty` | Quantity must be normalized (§38). |
| Blind catch-up of stale **opens** after disconnect | §63. |
| Applying OPEN guards to risk-reducing closes | §64: separate policy. |
| Inventing fills when no destination quote has ever existed | Fail closed on unpriced OPEN; see CLOSE unpriced rule. |
| Sharing QUOTE and TRADE sequence / session state | Independent sessions (§27). |
| Redis as authority for shadow orders / positions / P&L | PostgreSQL is durable truth (§5). |

### 1.4 Pricing authority (non-negotiable)

```text
AUTHORITATIVE_SHADOW_PRICE = cTrader QUOTE FIX  (destination bid/ask)
INFORMATIONAL_SOURCE_PRICE = reconstructed source deal / VWAP
```

- **Fill price, expected destination price, spread, quote age, mark-to-market, slippage reference, pre-trade (pre-sim) checks** all read the destination quote store (§31).
- Source price is stored on the intent and used only for `source_vs_shadow_slippage` and optional informational deviation. It never becomes the fill.

---

## 2. Destination QUOTE feed contract (§31)

### 2.1 Session

Use the independent `CTraderQuoteSession` (SSL default port 5211). Shadow does **not** require the TRADE session.

Required feature flags for shadow pricing:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=<unrelated to shadow fills>
REAL_COPY_EXECUTION_ENABLED=false
SHADOW_COPY_ENABLED=true
```

If `CTRADER_FIX_QUOTE_ENABLED=false` or the QUOTE session is not logged on: **do not open/increase** shadow exposure (§62 QUOTE-unavailable rule, applied to shadow).

### 2.2 Latest-quote object

Maintain (memory cache **and** durable `destination_quotes`):

| Field | Required | Notes |
|---|---|---|
| `venue` | yes | e.g. `PEPPERSTONE_CSERVER` |
| `canonical_symbol` | yes | `XAUUSD` (§16) |
| `destination_symbol_id` | yes | cTrader instrument ID from Security List. Never hardcode. |
| `bid` | yes | Best available destination bid |
| `ask` | yes | Best available destination ask |
| `quote_received_at` | yes | Local UTC when the FIX message was accepted |
| `venue_timestamp` | if present | Venue-supplied time; never invent |
| `fix_msg_seq_num` | yes | For replay / audit |
| `md_entry_source` | yes | `SNAPSHOT` \| `INCREMENTAL` |
| `spread` | derived | `ask - bid` |
| `mid` | derived | `(bid + ask) / 2` — display / deviation only, **not** the default fill |

Reject a quote snapshot as unusable when any of: `bid <= 0`, `ask <= 0`, `ask < bid`, instrument ID not mapped to canonical XAUUSD.

### 2.3 Quote age

```text
quote_age = now_utc - quote_received_at
```

Optional secondary: `venue_quote_age = now_utc - venue_timestamp` when the venue timestamp is present and sane (not in the future by more than a small skew, not older than `quote_received_at` by an absurd margin). Policy thresholds use **`quote_age` unless config selects venue time**.

Thresholds are **configurable and measured** (§31). Do not hardcode magic numbers into business logic.

### 2.4 Stale-quote example (architecture)

```text
if quote_age > configured_max_quote_age:
    reject new copy order
```

This example is the **OPEN/INCREASE** rule. REDUCE/CLOSE uses a separate threshold / fallback (§7).

### 2.5 Quote persistence

Every shadow decision and every shadow fill must store the **exact quote snapshot** used (or a FK to `destination_quotes.id`). Later P&L reconstruction must not re-price historical fills from “whatever the latest quote is now”.

---

## 3. Trigger path (no callback coupling)

Shadow follows the same durable path as live copy, minus TRADE send (§32):

```text
Source MT5 event
      ↓
validate / deduplicate / persist raw + outbox
      ↓
Trade reconstruction (Order ≠ Deal ≠ Position ≠ Logical Trade)
      ↓
Shadow-eligible reconstructed event?
      ↓
Classify action_class  (OPEN | INCREASE | REDUCE | CLOSE)
      ↓
Create CopyIntent  (expires_at, max_signal_age, action_class)
      ↓
Persist CopyIntent
      ↓
ShadowPolicy + RiskEngine evaluate  (stricter for OPEN/INCREASE)
      ↓
shadow_copy_order  (persisted before simulated send)
      ↓
simulated execution delay
      ↓
re-read destination quote
      ↓
re-check OPEN/INCREASE guards  (not applied as blockers to CLOSE)
      ↓
shadow_copy_fill(s)
      ↓
update shadow_position
      ↓
update shadow_pnl
      ↓
persist source_vs_shadow_slippage
```

Never construct a shadow fill directly inside an MT5 event callback.

---

## 4. Action classification — OPEN vs CLOSE are different laws (§64)

Every source event that can change copied exposure is classified **before** policy:

| `action_class` | Meaning | Typical source events | Policy family |
|---|---|---|---|
| `OPEN_EXPOSURE` | Create new destination shadow position where none is linked | First entry of a reconstructed trade | **OPEN** (strict) |
| `INCREASE_EXPOSURE` | Add size in the same direction | Scale-in, add-on deal | **OPEN** (strict) |
| `REDUCE_EXPOSURE` | Reduce size, remain open | Partial close | **CLOSE** (lenient) |
| `CLOSE_EXPOSURE` | Flatten the linked shadow position | Full close, remainder close | **CLOSE** (lenient) |

Risk engine **must be stricter about opening/increasing than reducing/closing** (§64).

### 4.1 Reversal

A source reversal is **two** intents, in order, never one blended order:

1. `CLOSE_EXPOSURE` for remaining linked shadow qty (CLOSE policy).
2. `OPEN_EXPOSURE` for leftover opposite qty (OPEN policy, new `shadow_position`).

If step 1 cannot be priced at all, do not open the reverse. If step 1 succeeds and step 2 is rejected, remain **flat**. Never leave the old direction open after a source reversal that we successfully closed.

### 4.2 Mapping (§35 analog)

Maintain explicit links:

```text
source reconstructed trade
        ↓
shadow_copy_order(s)
        ↓
shadow_position id(s)
```

Support: initial entry, scale-in, partial close, full close, reversal.  
**Do not assume one source event equals one shadow order forever.**

---

## 5. CopyIntent fields required for shadow (§36, §63)

Created and persisted **before** simulation:

| Field | Required | Notes |
|---|---|---|
| `copy_intent_id` | yes | Unique |
| `correlation_id` | yes | Logging (§57) |
| `source_broker_id` | yes | |
| `source_login` | yes | |
| `source_trade_id` | yes | Reconstructed trade id |
| `source_event_id` | yes | Deal / reconstruction event |
| `source_position_id` | yes | |
| `canonical_symbol` | yes | `XAUUSD` |
| `action_class` | yes | One of the four |
| `source_side` | yes | Buy/Sell of the **event** (entry vs exit) |
| `position_side` | yes | Long/Short of the **resulting** source position after the event |
| `source_event_volume` | yes | Raw source volume |
| `source_price` | yes | Informational |
| `source_event_time` | yes | Venue/source event time |
| `collector_receive_time` | yes | |
| `decision_time` | set at eval | |
| `expires_at` | yes | §63 |
| `max_signal_age` | yes | Duration; family-specific |
| `trader_state` | yes | Only `SHADOW` / `LIVE_CANDIDATE` / `LIVE` traders generate shadow (config) |
| `status` | yes | See §10 |

`expires_at` and `max_signal_age` are **mandatory**. Missing expiry is a defect, not a “never expire” open.

---

## 6. Timing rules (§36)

Each shadow lifecycle records:

```text
source_event_time
collector_receive_time
decision_time
shadow_sim_send_time     # analog of fix_send_time
shadow_fill_time         # analog of execution_time
```

Measure (even though no TRADE send occurs):

```text
MT5 → collector latency
collector → scoring latency     # if scoring already ran; else null
risk / policy latency
shadow sim-send latency
shadow fill latency
total source-to-shadow-fill latency
```

### 6.1 Signal age

```text
signal_age = decision_time - source_event_time
```

**OPEN/INCREASE:** reject `SIGNAL_TOO_STALE` when `signal_age > max_signal_age_open` **or** `now > expires_at`.

**REDUCE/CLOSE:** use `max_signal_age_close` (much larger; may be hours) so a delayed close of an **already-open shadow position** is not discarded. A close intent still has `expires_at`, but expiry must be long enough that reconnect / restart cannot strand an open shadow against a closed source. See §8.3.

Stale **entries** destroy XAUUSD edge (§36). Stale **closes** leave residual simulated risk. Those are opposite errors and must not share one threshold.

---

## 7. OPEN / INCREASE policy (strict)

Applies to `OPEN_EXPOSURE` and `INCREASE_EXPOSURE` only.

All checks below are **blocking**. First failure wins; persist `risk_decisions` / reject reason; **do not** create a fill.

### 7.1 Hard preconditions

1. `SHADOW_COPY_ENABLED=true`.
2. Trader state allows shadow.
3. Canonical symbol is XAUUSD and destination instrument is mapped (§16, §30).
4. QUOTE session logged on; latest quote exists for that instrument.
5. Quote usable (bid/ask valid).
6. `quote_age <= max_quote_age_open`.
7. `signal_age <= max_signal_age_open` and `now < expires_at`.
8. Kill / pause: `STOP_NEW_SHADOW_OPENS` (shadow analog of `STOP_NEW_EXECUTION`, §40) blocks these classes only.
9. Destination sizing produces qty `>= min_qty` and aligned to step (§38). If normalized qty rounds to **zero**, reject `QTY_BELOW_MIN` — do not open a notional-zero position.
10. No unresolved shadow-position inconsistency for this source trade (e.g. already fully closed).

### 7.2 Pre-sim price guard (§37)

Before simulated send, compute:

```text
expected_destination_price  = taker_touch(action, side, latest_quote)
current_destination_quote   = latest_quote
source_price                = intent.source_price
destination_spread          = ask - bid
```

Taker touch for **opening**:

| New / add direction | Expected dest price |
|---|---|
| Long (buy to open/increase) | `ask` |
| Short (sell to open/increase) | `bid` |

Reject when:

| Reason | Condition |
|---|---|
| `QUOTE_STALE` | `quote_age > max_quote_age_open` |
| `SPREAD_TOO_WIDE` | `ask - bid > max_spread_open` |
| `PRICE_MOVED_TOO_FAR` | adverse move vs expected dest price **or** vs source price exceeds configured ticks / price (see §7.3) |
| `QUOTE_UNAVAILABLE` | no usable quote / QUOTE session down |

Especially important around XAUUSD news (§37).

### 7.3 Price-move definition

Use **signed adverse** deviation, not unsigned noise that helps the copy:

```text
# Open long: worse if dest ask rose
adverse_vs_expected = dest_ask_now - expected_ask
# Open short: worse if dest bid fell
adverse_vs_expected = expected_bid - dest_bid_now

# Optional informational vs source
adverse_vs_source_long  = dest_ask_now - source_price
adverse_vs_source_short = source_price - dest_bid_now
```

Reject `PRICE_MOVED_TOO_FAR` if `adverse_vs_expected > max_adverse_move_open`  
**or** (if enabled) `adverse_vs_source_* > max_adverse_vs_source_open`.

Favorable move does **not** reject. Mid is not the fill and is not the default guard.

### 7.4 Post-delay re-check

After `simulated_execution_delay` (§11.2), **re-read** the destination quote and **repeat** §7.1–§7.3. A quote that was fresh at decision can be stale or moved at fill time.

If the re-check fails: mark order `REJECTED` / `EXPIRED` with the new reason. **No fill.** Do not fall back to the decision-time quote for OPEN.

### 7.5 Catch-up (§63)

If QUOTE was disconnected (example: 3 minutes) while many source traders opened:

```text
Do NOT reconnect and blindly shadow-open the backlog.
```

On reconnect:

- Expire every `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` intent with `now >= expires_at` or `signal_age > max_signal_age_open`.
- Do **not** open-then-immediately-close a source trade that both opened and closed during the gap (see §8.4).
- Never “make the book match source history” by replaying stale entries.

### 7.6 OPEN reject codes (closed set)

```text
QUOTE_STALE
QUOTE_UNAVAILABLE
QUOTE_INVALID
SPREAD_TOO_WIDE
PRICE_MOVED_TOO_FAR
SIGNAL_TOO_STALE
INTENT_EXPIRED
QTY_BELOW_MIN
QTY_STEP_INVALID
TRADER_NOT_SHADOW_ELIGIBLE
SHADOW_OPENS_STOPPED
SYMBOL_UNMAPPED
POSITION_INCONSISTENT
CATCH_UP_SUPPRESSED
```

---

## 8. REDUCE / CLOSE policy (lenient, risk-reduction)

Applies to `REDUCE_EXPOSURE` and `CLOSE_EXPOSURE` only.

Closing an existing **shadow** position is a risk-reduction action and **must not** share OPEN thresholds (§64).

### 8.1 What CLOSE still requires

1. A linked `shadow_position` with remaining qty `> 0`. If none: `NO_SHADOW_POSITION` — **do not invent** a short-lived open just to close it. This is how catch-up of never-shadowed entries stays inert.
2. Requested close qty after normalization `<= remaining shadow qty`. Excess is clipped; never flip by over-close.
3. A destination price **if one exists**. CLOSE does not invent ticks.

### 8.2 Guards that do **not** block CLOSE

The following **must not** reject a close of an existing shadow position:

- `SPREAD_TOO_WIDE` (OPEN threshold)
- `PRICE_MOVED_TOO_FAR` (OPEN threshold)
- `SIGNAL_TOO_STALE` at OPEN’s `max_signal_age_open`
- `STOP_NEW_SHADOW_OPENS` / `STOP_NEW_EXECUTION`
- QUOTE session currently disconnected, **if** a last usable quote exists (see §8.3)

They **may** be recorded as **fill-quality flags**, not as blockers.

### 8.3 Close pricing waterfall

1. If QUOTE is up and `quote_age <= max_quote_age_close`: fill at current taker touch (see §11.1). Quality = `LIVE`.
2. Else if a last usable quote exists and `quote_age <= max_quote_age_close_stale_fallback`: fill at that last touch. Quality = `STALE_QUOTE`. Persist quote age.
3. Else: **do not invent a price**. Transition position to `SOURCE_CLOSED_UNPRICED` (or `SOURCE_REDUCED_UNPRICED`), freeze remaining qty, emit `shadow_pnl` with `unrealized_quality=UNPRICED`. Ops/reconciliation must see this. No `shadow_copy_fill` row with a fabricated px.

`max_quote_age_close` >> `max_quote_age_open`.  
`max_quote_age_close_stale_fallback` is larger still (last-quote close after a brief QUOTE outage). Both configurable.

Taker touch for **closing**:

| Close direction | Expected dest price |
|---|---|
| Close long (sell to reduce/close) | `bid` |
| Close short (buy to reduce/close) | `ask` |

### 8.4 Catch-up of closes is required

After QUOTE/process restart, **pending REDUCE/CLOSE intents against existing shadow positions must be processed**. Leaving a simulated long after the source has flattened falsifies shadow P&L and would later mis-train promotion gates (§23).

Rules:

- Process closes **before** any surviving opens in the same replay batch.
- If a source trade opened **and** fully closed during an outage **and** no shadow position exists: drop both intents (`CATCH_UP_SUPPRESSED` on the open; `NO_SHADOW_POSITION` on the close).
- If a shadow position exists and source fully closed during the outage: close via §8.3 even if the close signal is older than `max_signal_age_open`.

### 8.5 Partial close / scale-out

`REDUCE_EXPOSURE` creates a fill of `min(normalized_event_qty, remaining_qty)` and leaves the position open. Realized P&L is booked on the closed slice only. VWAP of the remaining position is unchanged (average-cost). Do not reopen.

### 8.6 CLOSE reject / terminal codes

```text
NO_SHADOW_POSITION          # not an error if open was never accepted
QTY_CLIPPED                 # informational on fill, not a reject
UNPRICED_CLOSE_HELD         # position flagged; no fill
POSITION_INCONSISTENT
```

Spread / price-move / open-staleness are **not** in this set.

---

## 9. Position sizing (§38)

Never:

```text
source 0.10 MT5 lots  =  destination OrderQty 0.10
```

Normalized layer:

```text
source volume
    ↓
canonical notional / risk
    ↓
portfolio allocation (shadow book, not live margin)
    ↓
destination instrument quantity
```

Inputs (all persisted on the order or intent):

```text
source symbol contract size
destination symbol quantity convention
destination minimum quantity
destination step size
account leverage            # informational for later live; shadow may use a configured virtual equity
available margin            # shadow uses virtual / measured dest if available
risk allocation
current XAU exposure        # of the shadow book (and live book separately)
trader confidence           # optional scaler; default 1.0 until scoring exists
```

### 9.1 Rounding

```text
raw_dest_qty = f(source_volume, contract_sizes, allocation)
dest_qty     = floor_to_step(raw_dest_qty, step)
if dest_qty < min_qty:
    OPEN/INCREASE → reject QTY_BELOW_MIN
    REDUCE/CLOSE  → if remaining < min_qty, close remaining in full (flatten remainder)
```

Unit tests must use **real known examples** before any live execution (§38). Shadow is where those examples are proven.

### 9.2 Exposure accounting

Shadow book tracks its own:

```text
gross_xau_qty
net_xau_qty
open_position_count
```

OPEN/INCREASE may optionally reject `SHADOW_GROSS_CAP` / `SHADOW_NET_CAP` / `SHADOW_MAX_POSITIONS` so the simulated book resembles the live risk engine (§39). REDUCE/CLOSE never rejected for caps.

---

## 10. Entity specifications

Logical names from §24. Physical table aliases from §45 are listed. Both names must appear in schema comments so they are not implemented twice.

### 10.1 `shadow_copy_order`  (`shadow_orders`)

Simulated destination order. Persisted **before** simulated send (idempotency analog of §33).

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `copy_intent_id` | uuid | FK, unique per successful create |
| `source_broker_id` | text | |
| `source_login` | text | |
| `source_trade_id` | uuid | |
| `source_event_id` | text | |
| `destination_account` | text | Configured dest account id (e.g. 1369850) — **identity only**, no TRADE |
| `canonical_symbol` | text | |
| `destination_symbol_id` | text | |
| `action_class` | enum | OPEN/INCREASE/REDUCE/CLOSE |
| `side` | enum | Buy/Sell of **this order** |
| `requested_qty` | numeric | Destination units |
| `remaining_qty` | numeric | |
| `expected_destination_px` | numeric | Taker touch at persist |
| `decision_quote_id` | uuid | FK `destination_quotes` |
| `source_price` | numeric | Informational |
| `cl_ord_id` | text | Unique simulated client id (same generator family as live, `SHDW-` prefix) |
| `status` | enum | §10.5 |
| `reject_reason` | text | nullable |
| `created_at` | timestamptz | |
| `sim_send_at` | timestamptz | nullable until sim send |
| `completed_at` | timestamptz | |
| `correlation_id` | text | |

Unique: `(source_broker_id, source_login, source_event_id, action_class)` so one event cannot spawn two shadow orders.

### 10.2 `shadow_copy_fill`  (`shadow_fills`)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `shadow_order_id` | uuid | FK |
| `shadow_position_id` | uuid | FK |
| `fill_seq` | int | 1..N for partials |
| `qty` | numeric | |
| `price` | numeric | **Destination taker touch after delay** |
| `fill_quote_id` | uuid | FK — quote actually used |
| `fill_quality` | enum | `LIVE` \| `STALE_QUOTE` |
| `liquidity` | enum | `TAKER` (shadow never assumes maker) |
| `commission` | numeric | Destination cost model |
| `commission_ccy` | text | |
| `model_version` | text | Cost/slippage model id |
| `filled_at` | timestamptz | `shadow_fill_time` |
| `assumption_notes` | text | e.g. `SINGLE_FILL_FULL_QTY` |

Never write a fill without `fill_quote_id` (UNPRICED close has no fill row).

### 10.3 `shadow_position`  (`shadow_positions`)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | PK |
| `source_broker_id` / `source_login` / `source_trade_id` | | Link to reconstructed trade |
| `destination_symbol_id` | text | |
| `side` | enum | Long/Short |
| `open_qty` | numeric | Remaining |
| `max_qty` | numeric | High-water |
| `closed_qty` | numeric | |
| `entry_vwap` | numeric | Destination fill VWAP |
| `exit_vwap` | numeric | nullable until any close |
| `opened_at` / `closed_at` | timestamptz | |
| `status` | enum | `OPEN` \| `PARTIAL` \| `CLOSED` \| `SOURCE_CLOSED_UNPRICED` \| `SOURCE_REDUCED_UNPRICED` |
| `linked_order_ids` | | via child table, not a blob |

Scale-in updates `entry_vwap` and `max_qty`. Partial close updates `exit_vwap` on the closed slice only.

### 10.4 `shadow_pnl`  (`shadow_performance` + per-position snapshot)

Two grains, both required.

**Per-position, point-in-time (`shadow_pnl`):**

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | |
| `shadow_position_id` | uuid | |
| `as_of` | timestamptz | |
| `mark_quote_id` | uuid | nullable if UNPRICED |
| `mark_bid` / `mark_ask` | numeric | |
| `unrealized` | numeric | Conservative mark (§12) |
| `realized` | numeric | Fills + dest commission + dest swap |
| `commission` | numeric | Cumulative dest |
| `swap` | numeric | Cumulative dest model |
| `net` | numeric | `realized + unrealized -` already included costs |
| `mark_quality` | enum | `LIVE` \| `STALE_QUOTE` \| `UNPRICED` |

**Book rollup (`shadow_performance`):** per trader, per day, per canonical symbol: realized, unrealized, net, max DD, trade count, win/loss, gross/net XAU. This is what promotion gates read (§23: minimum shadow trades, minimum shadow net P&L, maximum shadow DD).

Source-broker commission/swap from reconstruction are **not** copied into `shadow_pnl`. They remain on `reconstructed_trades` for source skill features.

### 10.5 `source_vs_shadow_slippage`

One row per shadow fill (entry and exit).

| Column | Type | Notes |
|---|---|---|
| `id` | uuid | |
| `shadow_fill_id` | uuid | |
| `leg` | enum | `ENTRY` \| `EXIT` |
| `source_price` | numeric | |
| `shadow_fill_price` | numeric | |
| `dest_bid` / `dest_ask` / `dest_spread` | numeric | At fill |
| `signed_slippage` | numeric | See §11.4 |
| `adverse_slippage` | numeric | `max(signed_in_adverse_direction, 0)` |
| `quote_age_ms` | int | |
| `signal_age_ms` | int | |
| `fill_quality` | enum | |

### 10.6 Order status machine

```text
PENDING_PERSISTED      # row exists, sim send not started
SIM_SENT               # delay window started
PARTIAL                # at least one fill, qty remains (assumption path)
FILLED
REJECTED               # policy / guard
EXPIRED                # expires_at or post-delay stale open
CANCELLED              # operator / trader pause before fill
UNPRICED_CLOSE_HELD    # close path, no quote
```

Shadow has no `EXECUTION_STATE_UNKNOWN` from TCP loss (§34) because nothing is sent. Restart during `SIM_SENT`: reload the order, re-read quote, re-apply the **same** action_class policy (OPEN may now reject; CLOSE still tries waterfall).

---

## 11. Simulation model (§24)

Simulate, using destination quotes:

- entry price
- spread
- quote freshness
- slippage
- execution delay
- partial-fill assumptions where applicable
- swap / commission model

### 11.1 Fill price (taker)

```text
OPEN/INCREASE long  → dest ask
OPEN/INCREASE short → dest bid
REDUCE/CLOSE  long  → dest bid
REDUCE/CLOSE  short → dest ask
```

No mid fills. No source-price fills. No “we were the bid”.

### 11.2 Execution delay

Configurable `shadow_execution_delay_ms` (decision → fill quote). Implementation:

1. Persist order `PENDING_PERSISTED` with decision quote.
2. Set `sim_send_at = now`.
3. Wait delay (in replay: advance synthetic clock; do not sleep in deterministic tests).
4. Read latest destination quote with `quote_received_at >= sim_send_at` if available; else latest quote.
5. Apply post-delay policy (§7.4 or §8.3).
6. Fill.

Delay is a **model of FIX RTT + venue accept**, not a live TRADE measurement, until Phase 8 produces real fill latency. Store `model_version`.

### 11.3 Partial fills

Default assumption (must be labeled on the fill):

```text
SINGLE_FILL_FULL_QTY
```

Optional, config-gated, only if destination step / a configured `max_immediate_shadow_qty` is exceeded:

- Split into N fills of `step`-aligned slices.
- Each slice re-reads the quote (or uses the same snapshot if no newer quote arrived).
- OPEN abort remainder if a later slice fails OPEN guards; keep earlier slices (true partial). CLOSE does not abort remainder for spread/move.

Do not invent a depth-of-book. If the model cannot justify a split, do not split.

### 11.4 Slippage

Slippage is **measured**, not a random overlay.

```text
# Buy fill (open long or close short)
signed_slippage = shadow_fill_price - source_price

# Sell fill (open short or close long)
signed_slippage = source_price - shadow_fill_price
```

Positive `signed_slippage` = destination worse than source print.

Optional extra model `shadow_adverse_ticks` added to the dest touch **after** delay, only if explicitly enabled and versioned. Default **0**. Do not hide a fudge factor inside the fill without `model_version`.

`source_vs_shadow_slippage` is the audit table for this measurement. Promotion / live risk later consume the distribution; shadow itself does not “improve” fills to look good.

### 11.5 Spread

`spread = ask - bid` at the fill quote. OPEN rejected above `max_spread_open`. CLOSE records spread only.

### 11.6 Commission and swap

Versioned `DestinationCostModel`:

```text
model_id
commission_mode     # PER_MILLION | PER_LOT | BPS | FLAT
commission_open
commission_close
swap_long_per_night
swap_short_per_night
swap_triple_day     # weekday config
currency
```

Until the real Pepperstone schedule is measured on the account, the model is **ASSUMED** and must be flagged `cost_quality=ASSUMED`. Never copy source `commission` / `swap` into shadow P&L.

Swap accrues on positions still open across destination rollover (config timezone). Accrual writes a `shadow_pnl` increment, not a fake fill.

### 11.7 Quote freshness inside the model

Freshness is not only a reject rule. Every fill and mark stores `quote_age_ms` and `fill_quality` so later analysis can slice “live-quote shadow P&L” vs “stale-close shadow P&L”. Promotion gates (§23) should default to **excluding** `UNPRICED` and optionally excluding `STALE_QUOTE` exits.

---

## 12. Mark-to-market P&L

Use the **destination QUOTE** only.

Conservative (default, used for DD / kill-analog / promotion):

```text
long  unrealized = (mark_bid - entry_vwap) * open_qty * dest_contract_value - accrued_costs_alloc
short unrealized = (entry_vwap - mark_ask) * open_qty * dest_contract_value - accrued_costs_alloc
```

Display-only mid mark may be stored as `unrealized_mid` but **must not** drive `shadow_performance` DD or go-live gates.

If `quote_age > max_quote_age_mark`: set `mark_quality=STALE_QUOTE` and still publish last mark (dashboard needs a number) with quote age visible. If no quote ever: `UNPRICED`, `unrealized=null` (not zero). **Zero is a lie.**

Realized on a close fill:

```text
realized_slice = direction * (exit_px - entry_vwap) * closed_qty * dest_contract_value
               - commission_on_slice
# swap already accrued over life
```

---

## 13. Failure and disconnect rules (shadow application of §62–§63)

| Condition | OPEN / INCREASE | REDUCE / CLOSE of existing shadow |
|---|---|---|
| MT5 source unavailable | Do not invent source events. No new intents. | No new close intents until source says so. Existing marks continue on dest quotes. |
| QUOTE FIX down | No new opens. | Waterfall §8.3; last quote or UNPRICED hold. |
| TRADE FIX down | Irrelevant to shadow fills. | Irrelevant. |
| Database down | Fail closed. No in-memory-only shadow orders. | Same. |
| Process restart | Reconcile pending `SIM_SENT` with current policy. | Drain pending closes first. |
| 20 source opens during 3 min outage | Expire / do not catch up. | Close only those that already had shadow positions. |
| `STOP_NEW_SHADOW_OPENS` | Block. | Allow. |
| `EMERGENCY_FLATTEN` (shadow analog) | Block opens. | Flatten all shadow positions via CLOSE waterfall; requires stronger auth if ever wired to live. Shadow flatten for tests is a separate flag `SHADOW_BOOK_FLATTEN`. |

---

## 14. Idempotency and replay

- Persist `CopyIntent` then `shadow_copy_order` before delay.
- Unique event key: `(source_broker_id, source_login, source_event_id, action_class)`.
- Replay of historical MT5 events (§60) must produce **identical** orders/fills when fed the **recorded** `destination_quotes` stream and the same `model_version` / clock.
- Live shadow uses the live QUOTE stream; replay never silently substitutes a different feed without `price_source` metadata.

Replay pipeline:

```text
historical MT5 events
        ↓
replay reconstruction
        ↓
features / scores (optional)
        ↓
recorded destination quotes  (or recorded source ticks labeled separately)
        ↓
shadow copy
        ↓
assert orders / fills / positions / pnl
```

---

## 15. Interaction with live risk / promotion

Shadow is the default after trade #3 + high score (§23). Live copy stays off until gates in §68 include:

- sufficient shadow sample
- destination costs / slippage measured (`source_vs_shadow_slippage`)
- stale quote rejection proven
- stale signal rejection proven
- sizing conversion verified

Promotion reads `shadow_performance`, not source-broker net P&L.

When live execution is later enabled, **do not double-count**: a trader promoted to LIVE continues to have a shadow book only if config `SHADOW_PARALLEL_TO_LIVE=true` (default **false** after promotion, to avoid two books). Historical shadow rows remain immutable.

---

## 16. Logging and metrics

### 16.1 Log identifiers (§57)

Every shadow log line:

```text
correlation_id
broker_id
source_login
source_trade_id
copy_intent_id
action_class
shadow_order_id
cl_ord_id
shadow_position_id
fill_quality
quote_age_ms
reject_reason
```

Never log FIX passwords.

### 16.2 Metrics (§58 plus shadow)

```text
shadow_intents_total{action_class}
shadow_orders_total{status,action_class}
shadow_fills_total{fill_quality,leg}
shadow_rejections_total{reason,action_class}
shadow_unpriced_closes_total
shadow_open_positions
shadow_realized_pnl
shadow_unrealized_pnl
source_to_shadow_fill_latency
shadow_slippage{leg}
destination_quote_age
destination_spread
fix_quote_connected
```

Dashboard: Shadow Portfolio, trader-detail shadow positions / P&L, overview Shadow P&L vs destination real P&L (§47, §51).

---

## 17. Configuration (all measured; no silent hardcodes)

Suggested keys (values chosen later from data — §23, §31):

```env
SHADOW_COPY_ENABLED=false
SHADOW_PARALLEL_TO_LIVE=false
SHADOW_EXECUTION_DELAY_MS=
SHADOW_ADVERSE_TICKS=0
SHADOW_COST_MODEL_ID=
SHADOW_COST_QUALITY=ASSUMED

# OPEN / INCREASE
MAX_QUOTE_AGE_OPEN_MS=
MAX_SIGNAL_AGE_OPEN_MS=
MAX_SPREAD_OPEN=
MAX_ADVERSE_MOVE_OPEN=
MAX_ADVERSE_VS_SOURCE_OPEN=
OPEN_EXPIRES_AT_OFFSET_MS=

# REDUCE / CLOSE
MAX_QUOTE_AGE_CLOSE_MS=
MAX_QUOTE_AGE_CLOSE_STALE_FALLBACK_MS=
MAX_SIGNAL_AGE_CLOSE_MS=
CLOSE_EXPIRES_AT_OFFSET_MS=

# Mark
MAX_QUOTE_AGE_MARK_MS=

# Book caps (optional)
SHADOW_MAX_GROSS_XAU=
SHADOW_MAX_NET_XAU=
SHADOW_MAX_POSITIONS=

STOP_NEW_SHADOW_OPENS=false
```

`expires_at = source_event_time + OPEN_EXPIRES_AT_OFFSET_MS` for opens (or `collector_receive_time + …` — pick one, persist which). Closes use `CLOSE_EXPIRES_AT_OFFSET_MS`.

---

## 18. Module boundary (when implemented; not in this change)

Suggested location per §66: `/src/Shadow` plus `/docs/shadow-copy.md` generated from this spec later.

Depends on:

- reconstruction events (outbox)
- `destination_quotes` from Phase 4 QUOTE integration
- `canonical_instruments` / destination symbol map
- risk/policy hooks (even if live TRADE is dark)

Must not depend on TRADE send path.

This swarm artifact **does not** create those modules.

---

## 19. Acceptance criteria (Phase 5 / first useful version item 11)

Shadow copy is accepted when **all** of the following are true:

1. Every shadow fill price is taken from a persisted destination QUOTE snapshot (bid or ask per §11.1). No source-price fills.
2. `shadow_copy_order`, `shadow_copy_fill`, `shadow_position`, `shadow_pnl`, `source_vs_shadow_slippage` are durable and replayable.
3. OPEN/INCREASE reject on stale quote, stale signal, wide spread, adverse price move, QUOTE down, and catch-up backlog.
4. REDUCE/CLOSE of an **existing** shadow position is not blocked by OPEN guards; uses the close pricing waterfall; never invents a tick.
5. Source open+close during an outage with no prior shadow position produces **zero** shadow positions.
6. Source close after a live shadow open, including after restart, flattens or flags `UNPRICED` — never silently remains open.
7. Reversal = CLOSE then OPEN; rejected OPEN leaves flat.
8. Quantity path is normalized; 0.10 source lots is not blindly 0.10 dest.
9. Commission/swap are destination-model and versioned; source costs are not copied.
10. Conservative MTM uses dest bid (longs) / dest ask (shorts). Null ≠ 0 when unpriced.
11. No TRADE `NewOrderSingle` is emitted. `REAL_COPY_EXECUTION_ENABLED` remains false.
12. Unit tests cover: open reject stale quote; open reject stale signal; close still fills on stale quote; unpriced close hold; no catch-up open; partial close VWAP; reversal split; sizing floor-to-step; idempotent event replay.
13. Replay tests feed recorded quotes + MT5 events and assert golden fills/P&L.

---

## 20. Decision summary

| Topic | Decision |
|---|---|
| Who prices shadow? | Destination cTrader QUOTE bid/ask only |
| Who does not price shadow? | Source MT5 deal, mid, invented ticks |
| OPEN/INCREASE | Fail closed: freshness, spread, adverse move, expiry, no catch-up |
| REDUCE/CLOSE | Fail open for risk: last quote + quality flag; UNPRICED hold if none |
| Same threshold for open and close? | **No** (§63–§64) |
| Fill assumption | Taker touch after configured delay; default single full fill |
| P&L | Destination fills + dest cost model + conservative dest mark |
| Live orders | Out of scope; TRADE session unused |

---

*End of A24. Architecture §§24, 31, 36–38, 63–64 implemented as an executable policy, not a paraphrase.*
