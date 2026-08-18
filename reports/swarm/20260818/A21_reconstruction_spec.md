# A21 — Deterministic MT5 Deal Reconstruction Spec

**Status:** implementation contract (Phase 2, XAUUSD only)  
**Date:** 2026-08-18  
**Scope:** reconstruct completed XAUUSD position lifecycles from MT5 deals; count the first 3.  
**Non-scope:** product source, ML, MFE/MAE, destination copy, merging distinct hedge tickets.  
**Architecture:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§14–17 (also §§10, 11, 15 trigger, 17 feature-quality, 18/23/35/60/64).

This document is the single source of truth for the reconstruction algorithm. An implementation must match these rules and fixtures bit-for-bit (same trade keys, flags, integer volumes, decimal VWAP/PnL, first-3 events). Do not invent extra “logical trades.”

---

## 1. Binding architecture rules

### 1.1 §14 — Reconstruction is mandatory

```text
Order != Deal != Position != Logical Trade
```

One MT5 **position** may contain multiple entries, partial fills, scale-ins, partial closes, SL/TP modifications (not deals), and multiple closing deals.

The canonical output is **`ReconstructedTrade`**: one **completed position lifecycle**, not one deal and not one order.

### 1.2 §15 — “First 3 trades” means only this

Count only:

```text
3 completed reconstructed XAUUSD position lifecycles
```

Do **not** count as a trade:

| Event | Counts as a trade? |
|---|---|
| Order placement / pending | No |
| Deal fill (IN, still open) | No |
| Partial close (remaining > 0) | No |
| SL modification | No (not a deal) |
| TP modification | No (not a deal) |
| Balance / credit / commission / bonus / tax / SO-comp | No |
| Non-XAUUSD lifecycle | No |
| Still-open XAUUSD lifecycle | No |
| Failed / dirty lifecycle | No |

Closure of reconstructed XAUUSD trade **#3** emits:

```text
EARLY_SCORE_ELIGIBLE
```

It does **not** emit `PROVEN_PROFITABLE`.

### 1.3 §16 — Canonical symbol is `XAUUSD`

Broker strings `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD` (and persisted per-broker overrides) map to `CanonicalInstrument = XAUUSD`. Unmapped symbols are not XAUUSD. Never assume a destination FIX tag 55 equals `"XAUUSD"`.

### 1.4 §17 — Do not fabricate excursion features

This algorithm **does not** compute MFE, MAE, entry spread, or in-trade volatility. Those require a time-series while the lifecycle is open, tagged with `price_source` + `feature_quality`. Closed-deal OHLC guesses are forbidden.

### 1.5 §10 — Compound identity

Tickets are not globally unique. Every key carries `broker_id`.

```text
broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id
```

A reconstructed trade key is:

```text
(broker_id, login, position_id, lifecycle_seq)
```

`lifecycle_seq` starts at 1 for each `(broker_id, login, position_id)` and increments each time that position_id goes flat and later reopens (netting reuse or `ENTRY_INOUT` reversal).

### 1.6 Position lifecycle ≠ hedge basket

Hedging mode: each `position_id` is its own lifecycle. Three separate BUY tickets are **three** trades, not one scale-in.

Netting mode / same `position_id`: additional `ENTRY_IN` on an open lifecycle is scale-in. `ENTRY_INOUT` **completes** the current lifecycle and **opens** the next `lifecycle_seq` on the same `position_id`.

This matches §15 “position lifecycles” and §35 (initial, scale-in, partial close, full close, reversal).

---

## 2. Volume unit: integer hundredths of a lot

All reconstruction arithmetic uses **`volume_h`**: signed-safe **unsigned integer hundredths of one lot**.

| Lots | `volume_h` |
|---:|---:|
| 0.01 | 1 |
| 0.10 | 10 |
| 1.00 | 100 |
| 1.50 | 150 |

**Never** use IEEE lots (`0.1 + 0.2`) for remaining, VWAP weights, or completion tests. Remaining is an `int64` in hundredths. Completion is `remaining_h == 0`.

### 2.1 Converting raw Manager API volume

Official SDK (`MT5APIMath.h`):

```text
IMTDeal::Volume()     unit = 1/10000 lot   (MTAPI_VOLUME_DIV = 10000)
IMTDeal::VolumeExt()  unit = 1/1e8 lot
```

This repo’s `PositionData.volume` comment says “hundredths,” but `MT5Manager::extractDeal` copies `deal->Volume()` **unchanged** (SDK 1/10000). The reconstruction **input adapter** must convert explicitly. The algorithm never inspects raw SDK integers.

```text
volume_unit = "hundredths" | "mtapi_1e-4" | "mtapi_ext_1e-8"

to_hundredths(raw, unit):
  if unit == "hundredths":      return raw            # already 1.00 lot = 100
  if unit == "mtapi_1e-4":      require raw % 100 == 0; return raw / 100
  if unit == "mtapi_ext_1e-8":  require raw % 1_000_000 == 0; return raw / 1_000_000
  FAIL RECON_VOLUME_UNIT_UNKNOWN
```

If `mtapi_1e-4` raw is not divisible by 100, fail `RECON_VOLUME_NOT_QUANTIZED` (sub-0.01 lot). Do not round.

`volume_closed_h` uses the same conversion from `VolumeClosed()` / `VolumeClosedExt()`.

**Fixtures in this file are already `volume_h` / `volume_closed_h`.**

---

## 3. Input deal (normalized)

Caller supplies the **latest revision per** `(broker_id, deal_ticket)` — not the revision log. One row per ticket.

```text
DealIn
  broker_id            string     required
  login                uint64     required  > 0
  ticket               uint64     required  > 0
  order_ticket         uint64     0 allowed
  position_id          uint64     0 only for non-trade actions
  position_by_id       uint64     0 unless ENTRY_OUT_BY
  source_symbol        string     required (broker string, unnormalized)
  action               uint32     IMTDeal::EnDealAction
  entry                uint32     IMTDeal::EnDealEntry
  reason               uint32     IMTDeal::EnDealReason (optional, default 0)
  volume_h             uint64     hundredths; 0 only for non-trade / cancel
  volume_closed_h      uint64?    absent => infer per §7
  price                decimal    fill price; 0 only for non-trade
  profit               decimal    deal.Profit
  commission           decimal    deal.Commission   (usually ≤ 0)
  storage              decimal    deal.Storage      (swap)
  fee                  decimal    deal.Fee          (0 if ingest lacks it)
  time_msc             int64      deal.TimeMsc; if absent, time_sec * 1000
  price_sl             decimal?   deal.PriceSL if ingest has it
  price_tp             decimal?   deal.PriceTP if ingest has it
  comment              string
```

Enums (from `MT5APIDeal.h`, do not re-number):

```text
EnDealAction
  DEAL_BUY                    = 0
  DEAL_SELL                   = 1
  DEAL_BALANCE                = 2
  DEAL_CREDIT                 = 3
  DEAL_CHARGE                 = 4
  DEAL_CORRECTION             = 5
  DEAL_BONUS                  = 6
  DEAL_COMMISSION             = 7
  DEAL_COMMISSION_DAILY       = 8
  DEAL_COMMISSION_MONTHLY     = 9
  DEAL_AGENT_DAILY            = 10
  DEAL_AGENT_MONTHLY          = 11
  DEAL_INTERESTRATE           = 12
  DEAL_BUY_CANCELED           = 13
  DEAL_SELL_CANCELED          = 14
  DEAL_DIVIDEND               = 15
  DEAL_DIVIDEND_FRANKED       = 16
  DEAL_TAX                    = 17
  DEAL_AGENT                  = 18
  DEAL_SO_COMPENSATION        = 19
  DEAL_SO_COMPENSATION_CREDIT = 20

EnDealEntry
  ENTRY_IN     = 0
  ENTRY_OUT    = 1
  ENTRY_INOUT  = 2
  ENTRY_OUT_BY = 3
```

`DealData` in `mt5_types.h` already has `action`, `entry`, `volume`, `position`. Ingest **must** persist `position`; the current JSON helper omits it. This spec does not change product source — the adapter that feeds reconstruction is responsible.

---

## 4. Output

### 4.1 `ReconstructedTrade` (§14 field list + keys)

```text
id                    = hash or UUID derived from key; tests may use key string
broker_id
login
position_id
lifecycle_seq
canonical_symbol      = "XAUUSD" for every trade this algorithm emits
source_symbol         = source_symbol of the opening deal (first IN / INOUT-open)

direction             = LONG | SHORT     # opening side; never flips mid-lifecycle

opened_at_msc
closed_at_msc         = null if !completed

entry_vwap            = Σ(open_price * open_vol_h) / Σ(open_vol_h)
exit_vwap             = Σ(close_price * close_vol_h) / Σ(close_vol_h)   # null if no close

initial_volume_h      = volume of the first opening event
max_volume_h          = max |remaining_h| observed after each applied deal
closed_volume_h       = Σ close legs

gross_realized_pnl    = Σ deal.profit              on deals applied to this lifecycle
commission            = Σ deal.commission
swap                  = Σ deal.storage
fees                  = Σ deal.fee
net_realized_pnl      = gross + commission + swap + fees

deal_count            = number of applied tradeable deals
order_count           = distinct order_ticket values > 0
deal_tickets[]        = applied tickets in apply order
order_tickets[]

initial_sl, initial_tp   # from first opening deal if present, else null
final_sl, final_tp       # from last applied deal that carries sl/tp, else initial

was_scaled_in         = true iff opening_event_count >= 2
was_partial_close     = true iff at least one OUT/OUT_BY left remaining_h > 0
was_averaged_down     = true iff any scale-in price was worse than entry VWAP *before* that fill
                         LONG:  price < vwap_before
                         SHORT: price > vwap_before

completed             = remaining_h == 0 AND closed_volume_h > 0 AND opened_at_msc != null
dirty                 = true if a failure touched this key (not first-3 eligible)

close_reason          = reason of the completing deal (optional)
```

VWAP uses **exact decimal**: `DECIMAL(24,12)` = `Σ(price * volume_h) / Σ(volume_h)`. Prices are used as given (no tick rounding). Division is decimal, not binary float. Tests compare to 12 dp.

`was_scaled_in` includes same-order partial fills (two `ENTRY_IN` on one order). That is volume added after the first fill of the lifecycle. Optional diagnostic `was_same_order_partial_fill` may be stored but is not a §14 field.

### 4.2 First-3 cursor (per `broker_id + login`)

```text
First3State
  completed_count          uint32     # XAUUSD completed, non-dirty, in closed_at order
  first3_keys[0..2]        trade keys # only the first three
  early_score_eligible     bool       # latches true at completed_count reaching 3
  early_score_at_msc       int64?     # closed_at_msc of trade #3
  early_score_deal_ticket  uint64?    # completing deal of trade #3
```

Latch is **idempotent**. Trade #4+ does not clear it and does not emit `PROVEN_PROFITABLE`.

### 4.3 Events (for outbox later; this spec only defines them)

| Event | When |
|---|---|
| `XAU_LIFECYCLE_OPENED` | first IN / INOUT-open of a new seq |
| `XAU_LIFECYCLE_INCREASED` | scale-in |
| `XAU_LIFECYCLE_REDUCED` | OUT / OUT_BY with remaining > 0 |
| `XAU_LIFECYCLE_COMPLETED` | remaining hits 0 |
| `EARLY_SCORE_ELIGIBLE` | 3rd XAUUSD completion for that login |

These map later to §64 `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` / `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE`. Reconstruction itself does not copy.

### 4.4 Failures

A failure marks the affected `(broker_id, login, position_id, lifecycle_seq)` **dirty** and increments `reconstruction_failures_total`. Dirty lifecycles are persisted for audit and **excluded** from first-3. Subsequent deals on a dirty open lifecycle stay dirty; they are still applied if the volume rules allow, otherwise the lifecycle stays dirty and further volume violations re-emit the same code (idempotent).

| Code | Meaning |
|---|---|
| `RECON_VOLUME_UNIT_UNKNOWN` | adapter unit not in the allow-list |
| `RECON_VOLUME_NOT_QUANTIZED` | cannot convert to integer hundredths |
| `RECON_NON_TRADE_ON_BOOK` | tradeable path saw non BUY/SELL |
| `RECON_CANCELED_DEAL` | `DEAL_BUY_CANCELED` / `DEAL_SELL_CANCELED` on an XAUUSD book |
| `RECON_MISSING_POSITION_ID` | tradeable deal with `position_id == 0` |
| `RECON_ZERO_VOLUME` | tradeable deal with `volume_h == 0` |
| `RECON_BAD_PRICE` | tradeable deal with `price <= 0` |
| `RECON_IN_ON_FLAT_INOUT` | (reserved) |
| `RECON_IN_OPPOSITE_DIRECTION` | `ENTRY_IN` would flip sign; must be OUT/INOUT |
| `RECON_OUT_FLAT` | `ENTRY_OUT` / `OUT_BY` with no open lifecycle |
| `RECON_OUT_SAME_DIRECTION` | OUT action has the same sign as remaining |
| `RECON_OUT_OVERCLOSE` | close volume > remaining (should have been INOUT) |
| `RECON_OUT_CLOSED_MISMATCH` | provided `volume_closed_h` ≠ `volume_h` on OUT |
| `RECON_INOUT_FLAT` | INOUT with no open lifecycle |
| `RECON_INOUT_SAME_DIRECTION` | INOUT action has the same sign as remaining |
| `RECON_INOUT_CLOSED_MISMATCH` | provided `volume_closed_h` ≠ remaining |
| `RECON_INOUT_NO_NEW_VOLUME` | `volume_h <= volume_closed_h` (not a reversal) |
| `RECON_UNKNOWN_ENTRY` | entry not in {0,1,2,3} |

---

## 5. Symbol normalization (§16)

Applied **before** the book. Persist `(broker_id, source_symbol) → canonical` on first sight.

```
normalize(broker_id, source_symbol) -> optional Canonical
  raw = trim(source_symbol)
  if raw is empty: return None

  if mapping exists for (broker_id, raw):
    return mapping.canonical          # may be XAUUSD or something else

  key = ASCII-uppercase(raw)
  while key ends with '.': key = key without last char

  # Built-in XAUUSD allow-list only (architecture examples + trivial suffixes).
  if key == "GOLD":
    persist mapping raw -> XAUUSD
    return XAUUSD
  if key == "XAUUSD":
    persist mapping raw -> XAUUSD
    return XAUUSD
  if key starts with "XAUUSD":
    suffix = key[6:]
    if suffix in {"", "M", "A", ".A", ".M", "PRO", ".PRO", "I", ".I"}:
      persist mapping raw -> XAUUSD
      return XAUUSD

  persist mapping raw -> UNMAPPED     # not XAUUSD; do not guess
  return None
```

Broker-specific rows **override** the allow-list (e.g. a venue where `GOLD` is not XAU). Reconstruction never writes destination/cTrader IDs; that is a separate mapping table (§16).

This algorithm **drops** deals whose canonical is not `XAUUSD` (counted in `skipped_non_xau`). They do not open books and do not occupy first-3 slots.

---

## 6. Deal classification

```
is_tradeable(action):
  action in {DEAL_BUY, DEAL_SELL}

is_canceled(action):
  action in {DEAL_BUY_CANCELED, DEAL_SELL_CANCELED}

is_balance_like(action):
  not is_tradeable and not is_canceled
```

Pipeline per deal after sort:

1. Duplicate `(broker_id, ticket)` already applied → ignore (`duplicate_deals_total++`). No state change.
2. `is_balance_like` → skip (`skipped_non_trade++`). Includes empty symbol / zero position.
3. Normalize symbol. If not XAUUSD → skip (`skipped_non_xau++`).
4. `is_canceled` → `RECON_CANCELED_DEAL`, dirty that `position_id`’s current (or last) lifecycle. Do **not** invent an inverse fill. First-3 will ignore it.
5. Else tradeable → apply §7.

Canceled is conservative: prop/manager cancel rows are rare and unsafe to net without a pairing key. Dirty + exclude beats a silent wrong first-3.

---

## 7. Deterministic apply engine

### 7.1 Sort (mandatory before any apply)

Full rebuild for `(broker_id, login)` is the source of truth.

```
sort key ascending:
  1. time_msc
  2. ticket
```

Do not use ingest arrival order. Live tail may apply a deal only if its sort key is **strictly greater** than the last applied key for that login; otherwise schedule a full rebuild.

### 7.2 Books

One book per `(broker_id, login, position_id)`:

```
PositionBook
  lifecycle_seq        uint32     next seq to open (1-based)
  current              ReconstructedTrade?   # open or dirty-open
  remaining_h          int64      +LONG / −SHORT / 0
  open_vol_sum_h       uint64     Σ opening legs (VWAP denominator)
  open_px_vol          decimal    Σ price * open_vol_h
  close_vol_sum_h      uint64
  close_px_vol         decimal
  seen_partial_close   bool
  seen_scale_in        bool
  seen_avg_down        bool
  opening_event_count  uint32
```

Account state: `Map<(broker, login), First3State>` plus `Set<(broker, ticket)>` applied.

Direction helpers:

```
sign_action(action):
  DEAL_BUY  -> +1
  DEAL_SELL -> -1

sign_remaining(rem):
  rem > 0 -> +1
  rem < 0 -> -1
  rem == 0 -> 0

direction_of(rem):
  rem > 0 -> LONG
  rem < 0 -> SHORT
```

### 7.3 Open a lifecycle

Called when `remaining_h == 0` and an opening event arrives (`ENTRY_IN` or the open half of `ENTRY_INOUT`).

```
open_lifecycle(book, deal, open_vol_h, open_price, open_dir_sign):
  book.lifecycle_seq += 1           # first call: 0 -> 1
  t = new ReconstructedTrade
  t.broker_id, t.login, t.position_id = deal's
  t.lifecycle_seq = book.lifecycle_seq
  t.canonical_symbol = XAUUSD
  t.source_symbol = deal.source_symbol
  t.direction = open_dir_sign > 0 ? LONG : SHORT
  t.opened_at_msc = deal.time_msc
  t.initial_volume_h = open_vol_h
  t.max_volume_h = open_vol_h
  t.initial_sl/tp = deal.price_sl/tp
  t.final_sl/tp = deal.price_sl/tp
  book.current = t
  book.remaining_h = open_dir_sign * open_vol_h
  book.open_vol_sum_h = open_vol_h
  book.open_px_vol = open_price * open_vol_h
  book.close_vol_sum_h = 0
  book.close_px_vol = 0
  book.seen_* = false
  book.opening_event_count = 1
  apply_money(t, deal)              # INOUT: see §7.6 (money stays on closed side)
  record_deal(t, deal)
  emit XAU_LIFECYCLE_OPENED
```

### 7.4 `ENTRY_IN` (open or scale-in)

```
require is_tradeable, volume_h > 0, price > 0, position_id > 0
s = sign_action(deal.action)

if book.remaining_h == 0:
  open_lifecycle(book, deal, deal.volume_h, deal.price, s)
  return

if sign_remaining(book.remaining_h) != s:
  fail RECON_IN_OPPOSITE_DIRECTION; return

# scale-in / additional fill
vwap_before = book.open_px_vol / book.open_vol_sum_h
if t.direction == LONG  and deal.price < vwap_before: book.seen_avg_down = true
if t.direction == SHORT and deal.price > vwap_before: book.seen_avg_down = true

book.remaining_h += s * deal.volume_h
book.open_vol_sum_h += deal.volume_h
book.open_px_vol += deal.price * deal.volume_h
book.opening_event_count += 1
book.seen_scale_in = true
t.max_volume_h = max(t.max_volume_h, abs(book.remaining_h))
t.was_scaled_in = true
t.was_averaged_down = book.seen_avg_down
apply_money(t, deal)
record_deal(t, deal)
maybe_update_sl_tp(t, deal)
emit XAU_LIFECYCLE_INCREASED
```

### 7.5 `ENTRY_OUT` and `ENTRY_OUT_BY` (reduce / complete)

```
require volume_h > 0, price > 0, position_id > 0
if book.remaining_h == 0: fail RECON_OUT_FLAT; return

s = sign_action(deal.action)
if sign_remaining(book.remaining_h) == s:
  fail RECON_OUT_SAME_DIRECTION; return
  # LONG remaining>0 must be closed by SELL; SHORT by BUY

if deal.volume_closed_h is present and deal.volume_closed_h != deal.volume_h:
  fail RECON_OUT_CLOSED_MISMATCH; return

close_h = deal.volume_h
if close_h > abs(book.remaining_h):
  fail RECON_OUT_OVERCLOSE; return   # broker would have used INOUT

book.remaining_h += s * close_h      # moves toward 0
book.close_vol_sum_h += close_h
book.close_px_vol += deal.price * close_h
t.closed_volume_h = book.close_vol_sum_h
apply_money(t, deal)
record_deal(t, deal)
maybe_update_sl_tp(t, deal)

if book.remaining_h != 0:
  book.seen_partial_close = true
  t.was_partial_close = true
  emit XAU_LIFECYCLE_REDUCED
  return

complete_lifecycle(book, deal)
```

`ENTRY_OUT_BY` is identical for **this** `position_id`. Store `position_by_id` on the trade for audit. The counterparty deal is a separate row on the other `position_id` and completes that book independently. Do not auto-apply a synthetic opposite deal.

### 7.6 `ENTRY_INOUT` (reversal = complete + open)

MT5 netting reversal: one deal closes the current position and opens the opposite.

**Volume convention (this spec):**

```text
volume_h         = total fill hundredths          (closed + new)
volume_closed_h  = hundredths that close the old position
new_h            = volume_h - volume_closed_h     # must be > 0
```

This matches Manager semantics: a 1.00 long reversed by selling 1.50 is one `ENTRY_INOUT` `DEAL_SELL` with `volume_h=150`, `volume_closed_h=100`, new short `50`. Same-size flatten is `ENTRY_OUT`, never `INOUT`.

```
require volume_h > 0, price > 0, position_id > 0
if book.remaining_h == 0: fail RECON_INOUT_FLAT; return

s = sign_action(deal.action)         # direction of the NEW side
if sign_remaining(book.remaining_h) == s:
  fail RECON_INOUT_SAME_DIRECTION; return

closed_h = deal.volume_closed_h if present else abs(book.remaining_h)

if deal.volume_closed_h is present and closed_h != abs(book.remaining_h):
  fail RECON_INOUT_CLOSED_MISMATCH; return

if volume_h <= closed_h:
  fail RECON_INOUT_NO_NEW_VOLUME; return

new_h = volume_h - closed_h

# --- close old lifecycle at deal.price for closed_h ---
# money on the INOUT deal is assigned entirely to the CLOSED lifecycle
book.close_vol_sum_h += closed_h
book.close_px_vol += deal.price * closed_h
t.closed_volume_h = book.close_vol_sum_h
apply_money(t, deal)
record_deal(t, deal)
maybe_update_sl_tp(t, deal)
book.remaining_h = 0
complete_lifecycle(book, deal)       # may emit EARLY_SCORE_ELIGIBLE

# --- open next seq; do NOT apply_money again ---
open_lifecycle_from_reversal(book, deal, new_h, deal.price, s)
  # same as open_lifecycle but apply_money is skipped
  # deal_count starts at 1 (the INOUT ticket is listed on BOTH trades)
  # opening deal ticket is the same ticket
```

The same deal ticket appearing on both the completed and the new trade is required and deterministic.

### 7.7 Complete + first-3

```
complete_lifecycle(book, deal):
  t = book.current
  t.completed = true
  t.closed_at_msc = deal.time_msc
  t.entry_vwap = book.open_px_vol / book.open_vol_sum_h
  t.exit_vwap  = book.close_px_vol / book.close_vol_sum_h
  t.was_scaled_in = book.seen_scale_in
  t.was_partial_close = book.seen_partial_close
  t.was_averaged_down = book.seen_avg_down
  t.close_reason = deal.reason
  persist t
  book.current = null
  emit XAU_LIFECYCLE_COMPLETED

  if t.dirty: return

  st = first3[broker, login]
  st.completed_count += 1
  if st.completed_count <= 3:
    st.first3_keys.append(t.key)
  if st.completed_count == 3 and not st.early_score_eligible:
    st.early_score_eligible = true
    st.early_score_at_msc = t.closed_at_msc
    st.early_score_deal_ticket = deal.ticket
    emit EARLY_SCORE_ELIGIBLE
```

Ordering of first-3 **is** apply order, which **is** `(closed_at_msc, ticket)` because completion is applied in global sort order. Do not re-sort by `position_id` after the fact.

### 7.8 Money and SL/TP

```
apply_money(t, deal):
  t.gross_realized_pnl += deal.profit
  t.commission         += deal.commission
  t.swap               += deal.storage
  t.fees               += deal.fee
  t.net_realized_pnl    = t.gross_realized_pnl + t.commission + t.swap + t.fees
```

Use broker-provided money. **Do not** recompute PnL from price × contract × tick value.

SL/TP on deals are optional. Modifications that never produce a deal stay null. Do not backfill from `mt5_positions_current` inside this algorithm (mixed source). A later feature job may attach them with its own `price_source`.

### 7.9 Invariants (assert after every successful apply)

For each open book:

```
abs(remaining_h) + closed_volume_h == open_vol_sum_h
sign(remaining_h) == 0 or matches t.direction
max_volume_h >= initial_volume_h
max_volume_h >= abs(remaining_h)
```

For each completed non-dirty trade:

```
remaining was 0
closed_volume_h == open_vol_sum_h
closed_volume_h >= initial_volume_h
entry_vwap and exit_vwap are non-null
completed == true
```

Replay of the same sorted deal list must produce identical trades, flags, decimals, and first-3 keys (byte-stable serialization of decimals at 12 dp).

---

## 8. What this algorithm refuses to do

1. Merge multiple hedge `position_id`s into one “scaled” trade.
2. Count a partial close as a completed trade.
3. Count order placement, SL/TP modify, or non-XAUUSD.
4. Emit `PROVEN_PROFITABLE` at trade #3.
5. Compute MFE/MAE/spread/volatility from deals or from a foreign cTrader quote stream.
6. Silently mix `mtapi_1e-4` volumes with hundredths.
7. Use floating-lot equality to test flatten.
8. Apply live deals that sort *before* the last applied key without a full rebuild.
9. Attribute `DEAL_COMMISSION*` / daily swap deals onto a position unless they are BUY/SELL with a position_id (they are skipped as balance-like).

---

## 9. Suggested persist shape (not a migration)

`reconstructed_trades` unique on `(broker_id, login, position_id, lifecycle_seq)`.  
`trader_states` holds `completed_xauusd_count`, `early_score_eligible`, `early_score_at`.  
Rebuild is delete-by-`(broker_id, login)` + replay, or versioned snapshot replace. Incremental apply is an optimization of the same function.

Metrics (§58): `reconstructed_trades_total`, `reconstruction_failures_total`, `trade_completion_latency` = `closed_at_msc - opened_at_msc`.

---

## 10. Test fixtures

Shared constants unless a fixture overrides them:

```text
volume_unit          = hundredths
broker_id            = ACH
default profit/comm  = listed per deal
fee                  = 0 when omitted
storage              = 0 when omitted
reason               = 0
price_sl/tp          = null
time_msc             = integer milliseconds (not wall-clock)
```

Action/entry in fixtures are integer enums. `volume_closed_h` is omitted unless shown (`—`).

Expected decimals are **exactly 12 fractional digits**.

---

### F01 — Simple long open/close (1 completed)

**Purpose:** baseline lifecycle; first-3 count = 1; not early-score.

| ticket | time_msc | symbol | action | entry | volume_h | closed_h | price | profit | comm | storage | order | position |
|---:|---:|---|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|
| 101 | 1_000 | XAUUSD | 0 | 0 | 100 | — | 2400.00 | 0 | -0.70 | 0 | 801 | 5001 |
| 102 | 2_000 | XAUUSD | 1 | 1 | 100 | — | 2410.00 | 10.00 | -0.70 | -0.20 | 802 | 5001 |

**Expected trades**

| key | dir | seq | init | max | closed | entry_vwap | exit_vwap | gross | comm | swap | net | deals | scaled | partial | avg_dn | completed |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|---|
| ACH/1001/5001/1 | LONG | 1 | 100 | 100 | 100 | 2400.000000000000 | 2410.000000000000 | 10.00 | -1.40 | -0.20 | 8.40 | 2 | F | F | F | T |

`opened_at_msc=1000`, `closed_at_msc=2000`.  
`First3State.completed_count=1`, `early_score_eligible=false`.

**Apply trace**

```text
101 IN  +100 remaining=+100 open
102 OUT -100 remaining=0    complete
```

---

### F02 — Scale-in then full close

**Purpose:** `was_scaled_in`; VWAP; not averaged down (add at a better/higher long price).

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | order | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 201 | 1_000 | XAUUSD | 0 | 0 | 100 | 2400.00 | 0 | -0.70 | 811 | 5002 |
| 202 | 1_500 | XAUUSD | 0 | 0 | 50 | 2410.00 | 0 | -0.35 | 812 | 5002 |
| 203 | 2_000 | XAUUSD | 1 | 1 | 150 | 2420.00 | 25.00 | -1.05 | 813 | 5002 |

```text
entry_vwap = (2400*100 + 2410*50) / 150 = 360500 / 150 = 2403.333333333333
exit_vwap  = 2420.000000000000
```

| key | dir | init | max | closed | entry_vwap | exit_vwap | scaled | partial | avg_dn | completed |
|---|---|---:|---:|---:|---:|---:|---|---|---|---|
| ACH/1001/5002/1 | LONG | 100 | 150 | 150 | 2403.333333333333 | 2420.000000000000 | T | F | F | T |

`net = 25.00 + (-2.10) + 0 + 0 = 22.90`.  
`opening_event_count=2`. After 202: `remaining=+150`.

---

### F03 — Partial close then remainder (still one trade)

**Purpose:** two OUT deals, one lifecycle; partial close is not trade #2.

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | order | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 301 | 1_000 | XAUUSD | 0 | 0 | 100 | 2400.00 | 0 | -0.70 | 821 | 5003 |
| 302 | 1_400 | XAUUSD | 1 | 1 | 40 | 2410.00 | 4.00 | -0.28 | 822 | 5003 |
| 303 | 2_000 | XAUUSD | 1 | 1 | 60 | 2420.00 | 12.00 | -0.42 | 823 | 5003 |

After 302: `remaining=+60`, `completed=false`, `completed_count=0`.  
After 303: one completed trade.

```text
exit_vwap = (2410*40 + 2420*60) / 100 = 241600 / 100 = 2416.000000000000
```

| key | init | max | closed | entry_vwap | exit_vwap | scaled | partial | deals | orders | net |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|---:|
| ACH/1001/5003/1 | 100 | 100 | 100 | 2400.000000000000 | 2416.000000000000 | F | T | 3 | 3 | 14.60 |

`net = 16.00 - 1.40 = 14.60`.  
`First3State.completed_count=1`.

---

### F04 — Reversal leaves a new open lifecycle

**Purpose:** `ENTRY_INOUT` completes trade A and opens trade B (same `position_id`, seq 2). Money stays on A.

| ticket | time_msc | symbol | action | entry | volume_h | closed_h | price | profit | comm | order | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 401 | 1_000 | XAUUSD | 0 | 0 | 100 | — | 2400.00 | 0 | -0.70 | 831 | 5004 |
| 402 | 2_000 | XAUUSD | 1 | 2 | 150 | 100 | 2410.00 | 10.00 | -1.05 | 832 | 5004 |

Infer path (omit `closed_h`): `closed_h = remaining = 100`, `new_h = 50`. Same result.

| key | dir | seq | completed | init | max | closed | entry_vwap | exit_vwap | net | tickets |
|---|---|---:|---|---:|---:|---:|---:|---|---:|---|
| ACH/1001/5004/1 | LONG | 1 | T | 100 | 100 | 100 | 2400.000000000000 | 2410.000000000000 | 8.25 | 401,402 |
| ACH/1001/5004/2 | SHORT | 2 | F | 50 | 50 | 0 | 2410.000000000000 | null | 0 | 402 |

`completed_count=1`. Trade B has **no** money from 402.  
Ticket 402 is listed on **both** trades.

---

### F05 — Reversal then close of the new side (2 completed)

F04 + :

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | order | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 403 | 3_000 | XAUUSD | 0 | 1 | 50 | 2390.00 | 10.00 | -0.35 | 833 | 5004 |

| key | dir | seq | completed | entry_vwap | exit_vwap | net | tickets |
|---|---|---:|---|---:|---:|---:|---|
| ACH/1001/5004/1 | LONG | 1 | T | 2400.000000000000 | 2410.000000000000 | 8.25 | 401,402 |
| ACH/1001/5004/2 | SHORT | 2 | T | 2410.000000000000 | 2390.000000000000 | 9.65 | 402,403 |

`completed_count=2`, `early_score_eligible=false`.

---

### F06 — First 3 completed XAUUSD only (core §15 fixture)

`login=2001`. Interleaved EURUSD, partial GOLD, suffix symbols, and a 4th XAUUSD.

| ticket | time_msc | login | symbol | action | entry | volume_h | price | profit | comm | position |
|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 601 | 1_000 | 2001 | EURUSD | 0 | 0 | 100 | 1.10000 | 0 | 0 | 1 |
| 602 | 2_000 | 2001 | EURUSD | 1 | 1 | 100 | 1.10100 | 10 | 0 | 1 |
| 603 | 3_000 | 2001 | XAUUSD | 0 | 0 | 10 | 2400.00 | 0 | -0.07 | 10 |
| 604 | 4_000 | 2001 | XAUUSD | 1 | 1 | 10 | 2405.00 | 5.00 | -0.07 | 10 |
| 605 | 5_000 | 2001 | XAUUSDm | 0 | 0 | 20 | 2410.00 | 0 | -0.14 | 11 |
| 606 | 6_000 | 2001 | XAUUSDm | 1 | 1 | 20 | 2400.00 | -20.00 | -0.14 | 11 |
| 607 | 7_000 | 2001 | GOLD | 0 | 0 | 30 | 2390.00 | 0 | -0.21 | 12 |
| 608 | 8_000 | 2001 | GOLD | 1 | 1 | 10 | 2395.00 | 5.00 | -0.07 | 12 |
| 609 | 9_000 | 2001 | GOLD | 1 | 1 | 20 | 2380.00 | -20.00 | -0.14 | 12 |
| 610 | 10_000 | 2001 | XAUUSD. | 0 | 0 | 40 | 2370.00 | 0 | -0.28 | 13 |
| 611 | 11_000 | 2001 | XAUUSD. | 1 | 1 | 40 | 2380.00 | 40.00 | -0.28 | 13 |

**Skipped:** 601, 602 (`skipped_non_xau=2`).  
**After 608:** GOLD remaining=20, `completed_count` still 2 (604, 606). Partial is not a trade.  
**After 609:** trade #3 completes → **`EARLY_SCORE_ELIGIBLE`** at `time_msc=9000`, deal `609`.  
**After 611:** trade #4 completes; still `EARLY_SCORE_ELIGIBLE`; **not** `PROVEN_PROFITABLE`.

| # | key | source_symbol | dir | closed_at | note |
|---:|---|---|---|---:|---|
| 1 | ACH/2001/10/1 | XAUUSD | LONG | 4000 | |
| 2 | ACH/2001/11/1 | XAUUSDm | LONG | 6000 | mapped |
| 3 | ACH/2001/12/1 | GOLD | LONG | 9000 | mapped; was_partial_close=T |
| 4 | ACH/2001/13/1 | XAUUSD. | LONG | 11000 | **not** in first3_keys |

```text
first3_keys = [ACH/2001/10/1, ACH/2001/11/1, ACH/2001/12/1]
completed_count = 4
early_score_eligible = true
early_score_at_msc = 9000
early_score_deal_ticket = 609
```

Trade #3 `was_partial_close=true`, `exit_vwap = (2395*10 + 2380*20)/30 = 71550/30 = 2385.000000000000`.

---

### F07 — Averaging down on a long

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 701 | 1_000 | XAUUSD | 0 | 0 | 100 | 2400.00 | 0 | -0.70 | 5007 |
| 702 | 1_500 | XAUUSD | 0 | 0 | 100 | 2380.00 | 0 | -0.70 | 5007 |
| 703 | 2_000 | XAUUSD | 1 | 1 | 200 | 2390.00 | -10.00 | -1.40 | 5007 |

Before 702: `vwap=2400`. `2380 < 2400` → `was_averaged_down=true`.  
`entry_vwap = (2400*100 + 2380*100)/200 = 2390.000000000000`.

| key | scaled | avg_dn | entry_vwap | exit_vwap | net |
|---|---|---|---:|---:|---:|
| ACH/1001/5007/1 | T | T | 2390.000000000000 | 2390.000000000000 | -12.80 |

---

### F08 — Averaging down on a short

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 711 | 1_000 | XAUUSD | 1 | 0 | 100 | 2400.00 | 0 | -0.70 | 5008 |
| 712 | 1_500 | XAUUSD | 1 | 0 | 50 | 2415.00 | 0 | -0.35 | 5008 |
| 713 | 2_000 | XAUUSD | 0 | 1 | 150 | 2405.00 | -7.50 | -1.05 | 5008 |

Before 712: `vwap=2400`. Short add at `2415 > 2400` → averaged down.  
`entry_vwap = (2400*100 + 2415*50)/150 = 360750/150 = 2405.000000000000`.

| key | dir | scaled | avg_dn | entry_vwap | exit_vwap |
|---|---|---|---|---:|---:|
| ACH/1001/5008/1 | SHORT | T | T | 2405.000000000000 | 2405.000000000000 |

---

### F09 — Hedge close-by (two lifecycles)

Hedging: two `position_id`s. Close-by emits one `ENTRY_OUT_BY` per side.

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | position | position_by |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 801 | 1_000 | XAUUSD | 0 | 0 | 50 | 2400.00 | 0 | -0.35 | 6001 | 0 |
| 802 | 1_100 | XAUUSD | 1 | 0 | 50 | 2410.00 | 0 | -0.35 | 6002 | 0 |
| 803 | 2_000 | XAUUSD | 1 | 3 | 50 | 2408.00 | 4.00 | -0.35 | 6001 | 6002 |
| 804 | 2_000 | XAUUSD | 0 | 3 | 50 | 2408.00 | 1.00 | -0.35 | 6002 | 6001 |

Sort: 803 then 804 (ticket). Both complete.

| key | dir | completed | entry_vwap | exit_vwap | net |
|---|---|---|---:|---:|---:|
| ACH/1001/6001/1 | LONG | T | 2400.000000000000 | 2408.000000000000 | 3.30 |
| ACH/1001/6002/1 | SHORT | T | 2410.000000000000 | 2408.000000000000 | 0.30 |

`completed_count=2`. These are **not** one scaled trade.

---

### F10 — Same-order partial fills (still scale-in)

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | order | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 901 | 1_000 | XAUUSD | 0 | 0 | 30 | 2400.00 | 0 | -0.21 | 9001 | 5009 |
| 902 | 1_001 | XAUUSD | 0 | 0 | 70 | 2401.00 | 0 | -0.49 | 9001 | 5009 |
| 903 | 2_000 | XAUUSD | 1 | 1 | 100 | 2410.00 | 9.30 | -0.70 | 9002 | 5009 |

`entry_vwap = (2400*30 + 2401*70)/100 = 2400.700000000000`.  
`was_scaled_in=true`, `order_count=2`, `deal_count=3`.  
Optional diagnostic `was_same_order_partial_fill=true`.

---

### F11 — Netting reuse of `position_id` after flatten

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | comm | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 911 | 1_000 | XAUUSD | 0 | 0 | 100 | 2400.00 | 0 | -0.70 | 7001 |
| 912 | 2_000 | XAUUSD | 1 | 1 | 100 | 2410.00 | 10.00 | -0.70 | 7001 |
| 913 | 3_000 | XAUUSD | 1 | 0 | 80 | 2412.00 | 0 | -0.56 | 7001 |
| 914 | 4_000 | XAUUSD | 0 | 1 | 80 | 2402.00 | 8.00 | -0.56 | 7001 |

| key | dir | seq | completed | entry_vwap | exit_vwap |
|---|---|---:|---|---:|---:|
| ACH/1001/7001/1 | LONG | 1 | T | 2400.000000000000 | 2410.000000000000 |
| ACH/1001/7001/2 | SHORT | 2 | T | 2412.000000000000 | 2402.000000000000 |

`completed_count=2`.

---

### F12 — First 3 with scale-in + reversal in the set

`login=2002`. Three completed XAUUSD: simple, scale-in, reversal-close.

| ticket | time_msc | symbol | action | entry | volume_h | closed_h | price | profit | comm | position |
|---:|---:|---|---:|---:|---:|---|---:|---:|---:|---:|
| 921 | 1_000 | XAUUSD | 0 | 0 | 10 | — | 2400 | 0 | -0.07 | 21 |
| 922 | 1_100 | XAUUSD | 1 | 1 | 10 | — | 2410 | 1.00 | -0.07 | 21 |
| 923 | 2_000 | XAUUSD | 1 | 0 | 20 | — | 2410 | 0 | -0.14 | 22 |
| 924 | 2_100 | XAUUSD | 1 | 0 | 10 | — | 2400 | 0 | -0.07 | 22 |
| 925 | 2_200 | XAUUSD | 0 | 1 | 30 | — | 2390 | 5.00 | -0.21 | 22 |
| 926 | 3_000 | XAUUSD | 0 | 0 | 40 | — | 2380 | 0 | -0.28 | 23 |
| 927 | 3_100 | XAUUSD | 1 | 2 | 70 | 40 | 2390 | 4.00 | -0.49 | 23 |
| 928 | 3_200 | XAUUSD | 0 | 1 | 30 | — | 2385 | 1.50 | -0.21 | 23 |

Trade #1: `21/1` LONG simple.  
Trade #2: `22/1` SHORT scaled + averaged down (`2400 < 2410` for a short? Wait: short vwap 2410, add at 2400. 2400 < 2410 is **better** for a short (sold lower). `was_averaged_down=F`, `was_scaled_in=T`.  
Trade #3: `23/1` LONG completed by INOUT.  
Trade #4: `23/2` SHORT completed by 928 — **not** in first3.

After 927: `completed_count=3` → `EARLY_SCORE_ELIGIBLE` at `3100`, ticket `927`.  
After 928: `completed_count=4`, latch unchanged.

`first3_keys = [ACH/2002/21/1, ACH/2002/22/1, ACH/2002/23/1]`.

Trade #2 `entry_vwap = (2410*20 + 2400*10)/30 = 72200/30 = 2406.666666666667`  
(12 dp: `2406.666666666667` — 72200/30 = 2406.6… = 2406.6666666666**6** repeating. DECIMAL 12 dp half-away-from-zero or exact remainder? 

72200 ÷ 30 = 2406.6666666666…  
`DECIMAL(24,12)` exact repeating 6s: `2406.666666666667` if round-half-away or banker's on the 13th digit 6→7.

**Rounding rule (binding):** divide with 16 extra digits, then round **half away from zero** to 12 fractional digits.

13th digit of 2406.6666666666666… is 6 ≥ 5 → `2406.666666666667`.

---

### F13 — Open only (does not count)

| ticket | time_msc | symbol | action | entry | volume_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|
| 931 | 1_000 | XAUUSD | 0 | 0 | 25 | 2400.00 | 5013 |

One open trade `completed=false`. `completed_count=0`. No events except `XAU_LIFECYCLE_OPENED`.

---

### F14 — Balance / commission noise ignored

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|
| 941 | 500 | | 2 | 0 | 0 | 0 | 10000.00 | 0 |
| 942 | 1_000 | XAUUSD | 0 | 0 | 10 | 2400 | 0 | 5014 |
| 943 | 1_100 | | 7 | 0 | 0 | 0 | -2.00 | 0 |
| 944 | 2_000 | XAUUSD | 1 | 1 | 10 | 2410 | 1.00 | 5014 |

941, 943 skipped (`skipped_non_trade=2`). One completed XAUUSD. Commission deal is **not** folded into the trade (`commission` on the trade is only from 942+944).

---

### F15 — Out-of-order ingest (sort, don’t apply as received)

Input presented as **[OUT, IN]**:

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|
| 952 | 2_000 | XAUUSD | 1 | 1 | 10 | 2410 | 1.00 | 5015 |
| 951 | 1_000 | XAUUSD | 0 | 0 | 10 | 2400 | 0 | 5015 |

After sort `(time_msc, ticket)`: 951 then 952. Same result as a normal long.  
If applied in presentation order: 952 would `RECON_OUT_FLAT` — that implementation is **wrong**.

---

### F16 — Duplicate ticket is idempotent

Replay F01 deals `[101, 102, 101, 102]`.  
`duplicate_deals_total=2`. Exactly one trade, `deal_count=2`. `completed_count=1`.

---

### F17 — Canceled deal dirties and excludes

| ticket | time_msc | symbol | action | entry | volume_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|
| 961 | 1_000 | XAUUSD | 0 | 0 | 10 | 2400 | 5017 |
| 962 | 1_100 | XAUUSD | 13 | 0 | 10 | 2400 | 5017 |
| 963 | 2_000 | XAUUSD | 0 | 0 | 10 | 2401 | 5018 |
| 964 | 3_000 | XAUUSD | 1 | 1 | 10 | 2411 | 5018 |

962 → `RECON_CANCELED_DEAL`, `5017/1` dirty, **not** first-3 eligible (and not completed unless later flattened; still dirty).  
964 completes `5018/1` cleanly. `completed_count=1` (only 5018).

---

### F18 — Failure: OUT overclose

| ticket | time_msc | symbol | action | entry | volume_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|
| 971 | 1_000 | XAUUSD | 0 | 0 | 100 | 2400 | 5018A |
| 972 | 2_000 | XAUUSD | 1 | 1 | 150 | 2410 | 5018A |

972 → `RECON_OUT_OVERCLOSE`. Lifecycle dirty. `completed_count=0`.  
Correct encoding would have been `ENTRY_INOUT` volume 150 closed 100.

---

### F19 — Failure: `ENTRY_IN` opposite the open side

| ticket | time_msc | symbol | action | entry | volume_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|
| 981 | 1_000 | XAUUSD | 0 | 0 | 100 | 2400 | 5019 |
| 982 | 2_000 | XAUUSD | 1 | 0 | 50 | 2410 | 5019 |

982 → `RECON_IN_OPPOSITE_DIRECTION`. Dirty. Must be OUT or INOUT.

---

### F20 — Failure: INOUT volume does not exceed remaining

| ticket | time_msc | symbol | action | entry | volume_h | closed_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|
| 991 | 1_000 | XAUUSD | 0 | 0 | 100 | — | 2400 | 5020 |
| 992 | 2_000 | XAUUSD | 1 | 2 | 100 | 100 | 2410 | 5020 |

992 → `RECON_INOUT_NO_NEW_VOLUME`. Same-size flatten must be `ENTRY_OUT`.

---

### F21 — Failure: provided `volume_closed_h` ≠ remaining on INOUT

| ticket | time_msc | symbol | action | entry | volume_h | closed_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|
| 993 | 1_000 | XAUUSD | 0 | 0 | 100 | — | 2400 | 5021 |
| 994 | 2_000 | XAUUSD | 1 | 2 | 150 | 80 | 2410 | 5021 |

994 → `RECON_INOUT_CLOSED_MISMATCH` (book remaining 100 ≠ 80).

---

### F22 — Failure: missing `position_id`

| ticket | time_msc | symbol | action | entry | volume_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|
| 995 | 1_000 | XAUUSD | 0 | 0 | 10 | 2400 | 0 |

`RECON_MISSING_POSITION_ID`. No trade. `completed_count=0`.

---

### F23 — Multi-broker isolation (§10)

Same login `1001`, same `position_id` `5001`, two brokers.

| broker | ticket | time_msc | action | entry | volume_h | price | profit | position |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| ACH | 101 | 1000 | 0 | 0 | 100 | 2400 | 0 | 5001 |
| ACH | 102 | 2000 | 1 | 1 | 100 | 2410 | 10 | 5001 |
| SWX | 101 | 1000 | 0 | 0 | 50 | 2390 | 0 | 5001 |
| SWX | 102 | 1500 | 1 | 1 | 50 | 2395 | 2.5 | 5001 |

Two trades: `ACH/1001/5001/1` and `SWX/1001/5001/1`. Separate `First3State` per `(broker_id, login)`. Duplicate tickets across brokers are **not** duplicates.

---

### F24 — Unmapped symbol is not XAUUSD

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|
| 996 | 1_000 | XAGUSD | 0 | 0 | 100 | 30.00 | 0 | 8801 |
| 997 | 2_000 | XAGUSD | 1 | 1 | 100 | 31.00 | 100 | 8801 |
| 998 | 3_000 | XAUUSD.a | 0 | 0 | 10 | 2400 | 0 | 8802 |
| 999 | 4_000 | XAUUSD.a | 1 | 1 | 10 | 2410 | 1 | 8802 |

XAGUSD → UNMAPPED, skipped. `XAUUSD.a` → `XAUUSD` (suffix `.A` after upper).  
`completed_count=1` (only 8802).

---

### F25 — Three identical simple XAUUSD (minimum early-score path)

`login=3001`. Positions 31, 32, 33. Each: IN 10 @ 2400 then OUT 10 @ 2410, times 1000/1100, 2000/2100, 3000/3100.

After the third OUT (`ticket` of pos 33 close): `EARLY_SCORE_ELIGIBLE`.  
`first3_keys` length 3. No fourth deal.

This is the smallest fixture that may flip a trader from `INSUFFICIENT_DATA` toward `EARLY_SCORE` (§22). Reconstruction **only** emits `EARLY_SCORE_ELIGIBLE`; score values are out of scope.

---

## 11. Unit-test matrix (§60)

An implementation is not done until all of these pass against the fixtures:

| Test | Fixture | Assert |
|---|---|---|
| Simple reconstruct | F01 | 1 completed LONG, net 8.40 |
| Scale-in | F02 | max=150, vwap 2403.333333333333, scaled T |
| Partial close | F03 | 1 trade not 2, partial T, exit 2416 |
| Reversal split | F04 | seq1 complete + seq2 open; money on seq1 only |
| Reversal close | F05 | 2 completed, opposite directions |
| First-3 + ignore noise | F06 | keys 10,11,12; event on 609; trade 13 excluded |
| Avg-down long | F07 | avg_dn T |
| Avg-down short | F08 | avg_dn T |
| Close-by hedge | F09 | 2 trades, 2 position_ids |
| Partial fills | F10 | scaled T, one position |
| Position reuse | F11 | seq 1 LONG + seq 2 SHORT |
| Mixed first-3 | F12 | event on 927; seq2 of 23 not in first3 |
| Open ignored | F13 | count 0 |
| Balance skip | F14 | count 1 |
| Sort | F15 | success; not OUT_FLAT |
| Dedupe | F16 | deal_count 2 |
| Cancel dirty | F17 | 5017 excluded |
| Overclose | F18 | OUT_OVERCLOSE |
| IN flip | F19 | IN_OPPOSITE |
| Fake INOUT | F20 | INOUT_NO_NEW_VOLUME |
| Closed mismatch | F21 | INOUT_CLOSED_MISMATCH |
| No position | F22 | MISSING_POSITION_ID |
| Broker isolation | F23 | 2 trades |
| Unmapped | F24 | XAG skipped; XAUUSD.a counted |
| Min early-score | F25 | eligible exactly at 3rd close |
| Replay stability | F01–F12 | second pass identical |
| No MFE/MAE fields | all | excursion features absent |

---

## 12. Worked VWAP / remainder checks (copy into tests)

```text
F02 remaining: +100 → +150 → 0
F02 opened_h 150 == closed_h 150
F03 remaining: +100 → +60 → 0
F03 closed legs 40+60 == 100
F04 remaining: +100 → 0 (seq1), then −50 (seq2)
F05 remaining seq2: −50 → 0
F09 books independent; each remaining 50 → 0
```

---

## 13. Implementation notes (do not weaken the algorithm)

1. Rebuild from `mt5_deals` (or `mt5_deals_ledger` latest revision per ticket) filtered by login, then this function. Do not reconstruct inside the MT5 callback.
2. Persist `position_id`, `entry`, `action`, `volume` (plus unit), `VolumeClosed` if the SDK provides it, `TimeMsc`, `Fee`, `Reason`.
3. Convert SDK volume **once** at the adapter. Fixtures and the engine speak hundredths only.
4. `DealData` JSON today omits `position` — fix that in a later ingest task, not by guessing `order` as `position_id`.
5. Destination quantity conversion (§1.10 of the architecture / cTrader OrderQty) is **not** this algorithm.
6. After trade #3: scoring may run; default safety gate is SHADOW only (§23). Reconstruction stops at `EARLY_SCORE_ELIGIBLE`.

---

## 14. Acceptance for this spec

This spec is the Phase 2 contract when:

- Every fixture in §10 has an expected row that an engine can assert.
- Scale-in, partial close, and reversal are explicit state transitions (§7.4–7.6).
- First-3 is defined only as completed XAUUSD lifecycles (§1.2, F06, F12, F25).
- Volume is integer hundredths; no float flatten tests.
- MFE/MAE are explicitly out of scope (§1.4).

No product source was modified for this document.
