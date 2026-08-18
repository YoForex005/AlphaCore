# A83 — `DEAL_BUY_CANCELED` / `DEAL_SELL_CANCELED`: how reconstruction must treat them

**Agent:** A83  
**Date:** 2026-08-18  
**Status:** Binding reconstruction addendum to A21. Not product source.  
**Scope:** official meaning of Manager `IMTDeal::DEAL_BUY_CANCELED` (13) and `DEAL_SELL_CANCELED` (14), and the exact apply/skip/dirty rules the Phase-2 reconstructor must use.  
**Non-scope:** product-source edits, FIX `ExecType=4` order cancel, pending-order `ORDER_STATE_CANCELED`, inventing inverse fills, MFE/MAE.  
**Product source was not modified.**

**Sibling law (do not contradict):**

| Doc | Path | Owns |
|---|---|---|
| Reconstruction contract | `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` | apply engine, F17, `RECON_CANCELED_DEAL` |
| Deal enums | `D:\Prop\reports\swarm\20260818\A37_mt5_deal_enums.md` | numeric constants |
| Scoring universe | `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` | first-3 eligible rows |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§14–17 | lifecycle ≠ deal |

When this file and A21 differ on a cancel row, **this file clarifies encoding; A21 F17 remains the conservative first-3 fixture.** Volume book never applies a canceled action as a fill.

---

## 0. Verdict

Canceled deals are **not trades**. They are **not inverse fills**. They are the **same deal ticket** whose action was rewritten after the venue voided an already-executed buy or sell.

Reconstruction must:

1. **Never** treat action `13` / `14` as `DEAL_BUY` / `DEAL_SELL`.
2. **Never** apply their volume to remaining / VWAP / completion.
3. **Never** invent a compensating opposite fill.
4. **Never** fold their money into a lifecycle (profit is already zeroed; any clawback arrives as a **separate balance** deal).
5. **Never** count them as a completed XAUUSD trade.
6. On an XAUUSD book, emit **`RECON_CANCELED_DEAL`**, mark that `position_id`’s current (or last) lifecycle **dirty**, and **exclude** it from first-3.
7. On `OnDealUpdate` of the **same ticket** (`BUY`→`BUY_CANCELED` or `SELL`→`SELL_CANCELED`), **full-rebuild** that `(broker_id, login)`. Incremental apply of a mutated ticket is illegal.

Official MetaQuotes model is **in-place type mutation**, not a second ticket. A21 fixture F17 is the **unsafe extra-ticket** encoding. Both encodings skip volume; only the extra-ticket case can leave remaining wrong — that is why dirty + exclude beats a silent net.

**Current product does not implement this.** `NormalizedDeal.IsTradingDeal` drops 13/14 silently; `TradeReconstructor` never dirties; `EfTradingStore.UpsertDealAsync` is first-write-wins and will keep a stale `BUY` after the server rewrites the ticket to `BUY_CANCELED`.

---

## 1. Official semantics (authoritative)

### 1.1 Manager SDK numbers (do not re-number)

From `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` (`IMTDeal::EnDealAction`):

| Constant | Value | SDK comment |
|---|---:|---|
| `IMTDeal::DEAL_BUY` | 0 | buy |
| `IMTDeal::DEAL_SELL` | 1 | sell |
| `IMTDeal::DEAL_BUY_CANCELED` | **13** | canceled buy deal |
| `IMTDeal::DEAL_SELL_CANCELED` | **14** | canceled sell deal |

C# mirror already matches (`D:\Prop\src\Domain\Enums\DealAction.cs`): `BuyCanceled = 13`, `SellCanceled = 14`.

Format strings (`SMTFormat::FormatDealAction`): `"canceled buy"` / `"canceled sell"`.

MQL5 client aliases (same integers, **not** identifiers in this Manager SDK): `DEAL_TYPE_BUY_CANCELED`, `DEAL_TYPE_SELL_CANCELED`.

### 1.2 What MetaQuotes says happens

Quoted from the official MQL5 reference
[Deal Properties](https://www.mql5.com/en/docs/constants/tradingconstants/dealproperties)
(`ENUM_DEAL_TYPE`):

> **DEAL_TYPE_BUY_CANCELED** — Canceled buy deal. There can be a situation when a previously executed buy deal is canceled. In this case, the type of the previously executed deal (`DEAL_TYPE_BUY`) is changed to `DEAL_TYPE_BUY_CANCELED`, and its profit/loss is zeroized. Previously obtained profit/loss is charged/withdrawn using a separated balance operation.

> **DEAL_TYPE_SELL_CANCELED** — Canceled sell deal. There can be a situation when a previously executed sell deal is canceled. In this case, the type of the previously executed deal (`DEAL_TYPE_SELL`) is changed to `DEAL_TYPE_SELL_CANCELED`, and its profit/loss is zeroized. Previously obtained profit/loss is charged/withdrawn using a separated balance operation.

The MQL5 book
([Deal properties](https://www.mql5.com/en/book/automation/experts/experts_deal_properties))
restates the same model: earlier deal type is **changed**, P/L is **reset to zero**, previously received P/L is credited/debited as a **separate balance operation**.

### 1.3 Consequences that reconstruction must assume

| Fact | Implication |
|---|---|
| **Same ticket.** The canceled row **is** the original fill, not a child. | Latest-revision-per-`(broker_id, deal_ticket)` already removes the tradeable `BUY`/`SELL`. There is nothing left to invert. |
| **Action is rewritten.** `0→13` or `1→14`. | Ingest must persist the new action. First-write-wins freezes the stale fill. |
| **Profit/loss is zeroed on that ticket.** | Do not apply `profit` / leftover `commission` / `storage` / `fee` from a canceled row into a lifecycle. |
| **Clawback is a different deal** (`DEAL_BALANCE` / `DEAL_CHARGE` / `DEAL_CORRECTION`, typically `position_id = 0`). | Already classified `is_balance_like` in A21 §6 / F14. Do **not** attribute that cash to the XAUUSD book. |
| **Volume and entry are not documented as zeroed.** | `volume_h` and `ENTRY_*` on a canceled row are **forensic**, not apply inputs. Entry may still say `IN`/`OUT`. Ignore it. |
| **History still returns the ticket.** | `DealRequest` / `HistoryDealGetTicket` include it. Official **trading reports** skip it by requiring `Action ∈ {BUY, SELL}`. |
| **Typical venue.** Exchange / gateway / corporate-action / external-service void of an already-matched deal. Rare on OTC CFD XAUUSD; still mandatory. | Do not special-case “gold never cancels.” |

`IMTDeal::ExternalID()` is the exchange id of **this** ticket. It is **not** a pairing key to a second inverse ticket. Reconstruction must not join on `ExternalID`.

---

## 2. What these actions are **not**

| Lookalike | Why it is different |
|---|---|
| `IMTOrder::ORDER_STATE_CANCELED` | Pending/unfilled **order** canceled. No deal, or remainder never filled (`MT_RET_REQUEST_DONE_CANCEL`). Not a deal action. |
| FIX `ExecType=4` / `OrdStatus=4` | Destination cTrader order cancel. Different venue, different table. |
| `ENTRY_OUT` / `ENTRY_INOUT` of a live `DEAL_SELL`/`DEAL_BUY` | Real close / reverse. Applies volume. |
| `DEAL_CORRECTION` (5) / `DEAL_CHARGE` (4) | Ledger cash. `is_balance_like`. |
| `DEAL_REASON_SO` stop-out | Still a `BUY`/`SELL` fill (plus later `DEAL_SO_COMPENSATION*` cash). |
| A new opposite market deal | Would be a **new ticket** with action 0/1. Official cancel does not do this. |

Confusing any of the above with 13/14 produces a fake first-3 trade or a phantom remaining position.

---

## 3. Identity and ingest contract (prerequisite)

A21 §3: caller supplies the **latest revision per** `(broker_id, deal_ticket)`. One row per ticket.

### 3.1 Required persist behavior

```text
key            = (broker_id, deal_ticket)
on DealAdd     = insert revision 1 (action = 0 or 1, …)
on DealUpdate  = new revision N+1 if any persisted field changed
                 (action, profit, volume, entry, comment, …)
reconstruction = latest revision only
```

Ledger already speaks this language (`A17`: `recordDealRevision`, never overwrite historical evidence). The working `mt5_deals` snapshot used by reconstruction **must** reflect the latest action.

### 3.2 Current product (gap — do not paper over)

`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` `UpsertDealAsync`:

- unique `(BrokerId, DealTicket)`
- if the row exists → **return false, do not update `Action`**
- first-write-wins

So a live `OnDealUpdate` that rewrites ticket T from `DEAL_BUY` to `DEAL_BUY_CANCELED` is **dropped**. Reconstruction will keep applying a fill the venue voided.

`MT5Manager::cacheRecentDeal` **does** update-in-place by ticket (`mt5_manager.cpp`). The C++ ring is closer to official semantics than the C# store.

**Binding for reconstruction:** the algorithm assumes latest-action-per-ticket. If ingest is first-write-wins, a measured cancel is invisible and F17 cannot pass. That is an ingest defect, not a reason to invent inverse fills.

### 3.3 `DealUpdate` of a mutated ticket is a rebuild, not a tail apply

A21 §7.1: live tail may apply only if `(time_msc, ticket)` is **strictly greater** than the last applied key. A cancel mutation has the **same ticket** and usually the **same `Time`/`TimeMsc`**. Sort key does not advance.

```text
on ingest of action in {13,14}:
  persist latest revision
  schedule FULL rebuild of (broker_id, login)
  do not incrementally “apply cancel” onto an already-applied BUY/SELL
```

After rebuild, §5 applies.

---

## 4. Classification (A21 §6, restated)

```text
is_tradeable(action) := action ∈ {DEAL_BUY=0, DEAL_SELL=1}

is_canceled(action)  := action ∈ {DEAL_BUY_CANCELED=13, DEAL_SELL_CANCELED=14}

is_balance_like(action) := not is_tradeable and not is_canceled
```

Canceled is a **third class**. It is not tradeable. It is not silent balance-like (balance-like does not dirty a book).

`NormalizedDeal.IsTradingDeal` today:

```25:25:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public bool IsTradingDeal => Action is DealAction.Buy or DealAction.Sell;
```

That predicate is **correct for the volume book** and **insufficient for the pipeline** — canceled rows must still be seen so the engine can emit `RECON_CANCELED_DEAL`.

`TradeReconstructor.Reconstruct` currently `.Where(d => d.IsTradingDeal)` **before** grouping. Canceled rows never reach apply. That is the product bug relative to A21 F17.

---

## 5. How reconstruction must treat them

### 5.1 Volume book — skip, do not invert

```text
apply_canceled(book, deal):
  # deal.action ∈ {13,14}
  do not change remaining_h
  do not change open_px_vol / close_px_vol
  do not apply_money
  do not record_deal on a clean trade as a fill
  do not interpret deal.entry
  do not use deal.volume_h even if > 0
  do not pair by ExternalID / comment / time / equal volume
```

Why skip is correct under official encoding:

- Latest snapshot **no longer contains** the original `BUY`/`SELL`.
- Remaining sibling `BUY`/`SELL` tickets on that `position_id` already describe the surviving book.
- Adding an inverse would **subtract volume that is not in the book**.

Worked official case (in-place, one ticket):

| rev | ticket | action | entry | vol_h | meaning |
|---:|---:|---:|---:|---:|---|
| 1 | 500 | 0 BUY | IN | 10 | opened long 0.10 |
| 2 | 500 | 13 BUY_CANCELED | IN | 10 | venue voided that fill; profit 0 |

Reconstruction input = rev 2 only. Skip. Book empty. **Zero completed trades.** Correct: the long never existed in the latest history.

Worked official case (scale-in, one of two tickets canceled):

| ticket | latest action | vol_h |
|---:|---|---:|
| 501 | BUY IN | 10 |
| 502 | BUY_CANCELED IN | 20 |

Apply 501 only (`remaining = +10`). 502 is skip+dirty (XAUUSD). Do **not** subtract 20 from 10 (`RECON_OUT_OVERCLOSE` would be a lie).

Worked official case (close canceled):

| ticket | latest action | entry | vol_h |
|---:|---|---|---:|
| 601 | BUY | IN | 10 |
| 602 | SELL_CANCELED | OUT | 10 |

Apply 601. Skip 602. Lifecycle **open**, `completed=false`. Correct: the close was voided.

### 5.2 Money — ignore the canceled row; ignore the clawback cash on the book

- Canceled ticket: `profit` is 0 by spec. Commission/swap/fee are unspecified. **Apply none.**
- Separate balance/charge/correction: A21 `is_balance_like` → `skipped_non_trade++`. **Do not** search for “the matching XAUUSD lifecycle” to attach that cash. First-3 scoring uses deal money on **tradeable** legs only.

### 5.3 Dirty + first-3 — conservative, binding

A21 §4.4 / §6 / F17:

```text
is_canceled on an XAUUSD book
  → failure RECON_CANCELED_DEAL
  → dirty current (or last) lifecycle of that position_id
  → increment reconstruction_failures_total{reason=RECON_CANCELED_DEAL}
  → increment canceled_deals_total
  → do not invent an inverse fill
  → first-3 ignores dirty lifecycles
```

Why dirty even when latest-per-ticket already yields a correct remaining:

- Exchange/gateway void is a **data-quality** event on that position.
- Extra-ticket encodings (F17) leave remaining **wrong** if we only skip.
- Prop first-3 must prefer a false negative over a silent wrong trade (`A21` §6 last line).

Dirty scope:

| Scope | Rule |
|---|---|
| Affected key | `(broker_id, login, position_id, lifecycle_seq)` current if open, else last seq on that `position_id` |
| If no lifecycle exists yet (only canceled rows on that id) | persist a **dirty stub** (no `completed`, not first-3). Do not invent direction from action 13/14 |
| Other `position_id`s on the same login | **untouched** |
| Non-XAUUSD canceled (canonical ≠ `XAUUSD`) | A21 step 3: `skipped_non_xau++`, **no** dirty (XAUUSD first-3 only) |
| Subsequent tradeable deals on a dirty open book | stay dirty; still apply if volume rules allow (A21 §4.4) |
| Completed-then-dirty after rebuild | `completed` may become false (close canceled) or stay completed+dirty (extra-ticket cancel). Either way **not** first-3 |

### 5.4 First3State after a cancel rebuild

A21 latch (“trade #4+ does not clear `EARLY_SCORE_ELIGIBLE`”) applies to **additional completions**, not to a rebuild that **retracts** a completion.

```text
full rebuild of (broker_id, login):
  First3State := recompute from remaining completed ∧ ¬dirty ∧ XAUUSD lifecycles
  if completed_count < 3: early_score_eligible = false
  if completed_count >= 3: latch true at the 3rd in apply order
```

A trader who reached 3, then has trade #2’s close canceled in-place, must drop back to `INSUFFICIENT_DATA` / not-eligible. Sticky latch across that rebuild would score a voided book.

A22 §2.1 eligible-trade predicate must include `dirty == false` (A22 as written omits it; reconstruction is the gate).

### 5.5 Shadow / copy

Dirty or canceled-tainted lifecycles emit **no** `XAU_LIFECYCLE_COMPLETED` that is first-3 eligible and **no** copy `CLOSE_EXPOSURE`/`OPEN_EXPOSURE` derived from the canceled row. Do not flatten or open from a skip.

---

## 6. Two encodings the engine will see

### 6.1 Encoding A — official in-place (same ticket)

Server mutates ticket T. Latest row action ∈ {13,14}. Original 0/1 is gone.

| Step | Result |
|---|---|
| Volume | skip T |
| Remaining | reconstructed from surviving 0/1 tickets only — **correct** |
| Dirty | still set on that `position_id` (first-3 conservatism) |
| Completed | only if surviving legs flatten a non-empty open; that lifecycle is dirty |

### 6.2 Encoding B — extra ticket (A21 F17)

Ingest or a non-standard gateway emits **two** tickets: original `BUY`/`SELL` **and** a later `*_CANCELED` with a **new** ticket (F17: 961 BUY, 962 action=13).

Official docs do **not** describe this. It is still observed in naive snapshots (revision log stored as separate tickets) and is the A21 fixture.

| Step | Result |
|---|---|
| Volume | apply 961; **do not** invert 962 |
| Remaining | **may be wrong** vs venue (long still +10 after a “cancel”) |
| Dirty | **required** — `RECON_CANCELED_DEAL` on 5017 |
| First-3 | 5017 excluded even if later flattened |
| Repair | full DealRequest rebuild + latest-per-ticket should collapse Encoding B → A if it was a revision bug. If the server truly has two tickets, stay dirty forever for that position |

**Do not** heuristically pair 961 and 962 by equal volume / same price / Δt < N ms / matching comment. That is inventing a fill.

### 6.3 Encoding C — incremental stale BUY then update

1. Applied ticket T as `BUY`.
2. Later `DealUpdate` T action=13.

Illegal to “apply 13 as OUT.” Rebuild. After rebuild, Encoding A.

---

## 7. Binding pipeline (insert canceled **explicitly**)

Per deal after `(time_msc, ticket)` sort. Replaces A21 §6 steps with the same numbers plus canceled visibility:

```text
1. Duplicate (broker_id, ticket) already applied
     → duplicate_deals_total++; ignore
     Exception: if the new row is a later revision of the same ticket
     (action/profit/volume changed), this is NOT a duplicate — rebuild.

2. is_balance_like
     → skipped_non_trade++; ignore
     (includes the P/L clawback cash)

3. normalize(symbol)
     if canonical != XAUUSD → skipped_non_xau++; ignore
     (canceled EURUSD does not dirty an XAUUSD book)

4. is_canceled
     → RECON_CANCELED_DEAL
     → dirty current/last lifecycle on deal.position_id
     → canceled_deals_total++
     → STOP (no §7 apply)

5. is_tradeable → A21 §7 apply
     else → RECON_NON_TRADE_ON_BOOK (should be unreachable)
```

Empty symbol on a canceled row: A21 `normalize` returns `None` → step 3 skip, **no dirty**. Edge case. Prefer: if `action` is canceled **and** `position_id > 0` **and** that book is already open as XAUUSD, run step 4 even when symbol is empty. Do not open a book from a canceled row to make this work.

`volume_h == 0` or `price <= 0` on a canceled row: **not** `RECON_ZERO_VOLUME` / `RECON_BAD_PRICE` (those are tradeable-only).

---

## 8. Official plugins already skip them as fills

Manager report examples treat 13/14 as **non-market**:

- `DailyTradeReport.cpp`, `DailyDetailed.cpp`, `ExecutionType.cpp`, `MoneyFlowReport.cpp`: continue unless `Action() ∈ {DEAL_BUY, DEAL_SELL}`.
- `Trades.Transaction.Reports` `OnDealPerform`: market path only accepts `DEAL_BUY`/`DEAL_SELL`. `ProcessBalance` **maps** `DEAL_BUY_CANCELED` → `TYPE_BUY_CANCELED` (a labeled non-fill transaction), it does **not** treat it as a buy.

MetaQuotes article code that rebuilds history for charts typically `continue` unless type is `BUY` or `SELL` (sometimes also `BALANCE` for equity curves). That matches skip-for-volume. They do **not** apply an inverse.

---

## 9. Current product vs this spec

| Component | Path | Behavior today | Required |
|---|---|---|---|
| Enum | `src\Domain\Enums\DealAction.cs` | 13/14 present | keep |
| Filter | `NormalizedDeal.IsTradingDeal` | excludes 13/14 | keep for volume; **do not** drop before dirty check |
| Engine | `TradeReconstructor.Reconstruct` | `.Where(IsTradingDeal)` then apply IN/OUT by entry | see canceled **before** that filter; F17 |
| Dirty | `ReconstructedTradeResult` / entity | **no `Dirty` field** | add in implementation (not this write) |
| First-3 | `IsEarlyScoreEligible` | `completed && IsXauUsd` count ≥ 3 | also `!Dirty` |
| Ingest | `EfTradingStore.UpsertDealAsync` | first-write-wins | latest revision / new revision row |
| C++ ring | `MT5Manager::cacheRecentDeal` | update-in-place by ticket | already correct locally |
| Tests | `tests\Unit` | **no F17 / no cancel cases** | A21 matrix row “Cancel dirty” |
| Scoring | `BaselineScorer` | no dirty filter | consume only non-dirty completed XAUUSD |

`TradeReconstructor` additionally uses IEEE lots + `FlatEpsilon` and will treat an unexpected opposite `IN` as a reverse — orthogonal debt, but it makes a mistaken “apply 13 as SELL” even more dangerous.

---

## 10. Fixtures (engine-assertable)

Shared A21 constants (`broker_id=ACH`, hundredths, fee=0 unless set).

### F17 — Extra-ticket cancel dirties and excludes (A21, binding)

Already specified in A21 §10. Restated:

| ticket | time_msc | symbol | action | entry | volume_h | price | position |
|---:|---:|---|---:|---:|---:|---:|---:|
| 961 | 1_000 | XAUUSD | 0 | 0 | 10 | 2400 | 5017 |
| 962 | 1_100 | XAUUSD | **13** | 0 | 10 | 2400 | 5017 |
| 963 | 2_000 | XAUUSD | 0 | 0 | 10 | 2401 | 5018 |
| 964 | 3_000 | XAUUSD | 1 | 1 | 10 | 2411 | 5018 |

- 962 → `RECON_CANCELED_DEAL`. `5017/1` dirty. **Not** first-3. Do **not** flatten 5017 via inverse.
- 964 completes clean `5018/1`. `completed_count = 1`.
- `canceled_deals_total = 1`. `reconstruction_failures_total += 1`.

### F17b — Official in-place (latest row only)

Input (already latest-per-ticket):

| ticket | time_msc | symbol | action | entry | volume_h | price | profit | position |
|---:|---:|---|---:|---:|---:|---:|---:|---:|
| 970 | 1_000 | XAUUSD | 13 | 0 | 10 | 2400 | 0 | 5027 |

- No tradeable deal. No open lifecycle (or dirty stub only). `completed_count = 0`.
- `RECON_CANCELED_DEAL`. No inverse. No `XAU_LIFECYCLE_OPENED`.

### F17c — In-place cancel of one scale-in

| ticket | time_msc | action | entry | volume_h | price | position |
|---:|---:|---:|---:|---:|---:|---:|
| 971 | 1_000 | 0 | 0 | 10 | 2400 | 5028 |
| 972 | 1_500 | **13** | 0 | 20 | 2390 | 5028 |
| 973 | 2_000 | 1 | 1 | 10 | 2410 | 5028 |

- Apply 971 (+10). Skip 972 (dirty 5028). Apply 973 OUT 10 → complete.
- One completed LONG, **dirty**, `entry_vwap=2400`, `exit_vwap=2410`, `deal_tickets=[971,973]` (972 not a fill).
- `completed_count = 0` (dirty excluded).
- Remaining never goes to −10. Must **not** `RECON_OUT_OVERCLOSE`.

### F17d — Close canceled → lifecycle stays open

| ticket | time_msc | action | entry | volume_h | price | position |
|---:|---:|---:|---:|---:|---:|---:|
| 981 | 1_000 | 0 | 0 | 10 | 2400 | 5029 |
| 982 | 2_000 | **14** | 1 | 10 | 2410 | 5029 |

- Apply 981. Skip 982, dirty. `completed=false`, remaining=+10.
- `completed_count = 0`.

### F17e — Clawback cash is not a fill

F17b + :

| ticket | time_msc | action | volume_h | profit | position | symbol |
|---:|---:|---:|---:|---:|---:|---|
| 983 | 2_100 | 2 BALANCE | 0 | -1.00 | 0 | |

- `skipped_non_trade += 1`. Still no XAUUSD trade. Do not attach −1.00 to 5027.

### F17f — Non-XAU canceled is invisible to first-3

| ticket | symbol | action | entry | volume_h | position |
|---:|---|---:|---:|---:|---:|
| 984 | EURUSD | 13 | 0 | 100 | 9 |

- `skipped_non_xau += 1`. No `RECON_CANCELED_DEAL`. No dirty XAUUSD book.

### F17g — Rebuild retracts first-3 latch

Three clean XAUUSD completes (A21 F25), then latest revision of the **third close** becomes `SELL_CANCELED` (same ticket). Rebuild:

- Trade #3 `completed=false` (or missing close) and/or dirty.
- `completed_count = 2`.
- `early_score_eligible = false`.

---

## 11. What reconstruction refuses (this topic)

1. Treat `13` as `BUY` or `14` as `SELL`.
2. Treat `13` as “cancel the last buy by applying a synthetic SELL OUT.”
3. Pair tickets by volume, price, `ExternalID`, comment, or Δt.
4. Count a canceled row toward `deal_count` of a **clean** trade (dirty stub may list it for audit as a non-fill event; it is not a fill).
5. Apply canceled money or the subsequent balance clawback onto VWAP/PnL.
6. Let a dirty canceled lifecycle occupy a first-3 slot.
7. Incrementally apply a same-ticket action mutation.
8. Keep a first-write-wins `BUY` after the server published `BUY_CANCELED` and still claim measured reconstruction.
9. Use canceled rows to open a new `lifecycle_seq`.
10. Confuse this with FIX/order cancel or with `DEAL_CORRECTION`.

---

## 12. Test matrix (add to A21 §11)

| Test | Fixture | Assert |
|---|---|---|
| Extra-ticket dirty | F17 | 5017 dirty excluded; 5018 counts; no inverse |
| In-place only row | F17b | 0 trades; `RECON_CANCELED_DEAL` |
| Cancelled scale-in | F17c | remaining path +10 → 0; dirty; no overclose |
| Cancelled close | F17d | open long remains; not completed |
| Clawback cash | F17e | balance skipped; not on book |
| Non-XAU cancel | F17f | no XAU dirty |
| Latch retract | F17g | eligible false after rebuild |
| IsTradingDeal | unit | 13/14 false; 0/1 true |
| Pipeline sees cancel | unit | 13/14 not dropped before dirty |
| Replay | F17–F17g | second pass identical |

---

## 13. Metrics and persist (when implemented)

| Name | Type | When |
|---|---|---|
| `canceled_deals_total` | counter `{broker,action}` | each 13/14 after XAU filter (or all, labeled) |
| `reconstruction_failures_total` | counter `{reason=RECON_CANCELED_DEAL}` | each XAUUSD dirty from cancel |
| `reconstructed_trades.dirty` | bool | persist; unique key unchanged |
| `reconstructed_trades.failure_code` | text nullable | `RECON_CANCELED_DEAL` |

Do not delete the canceled deal from `mt5_deals`. It is evidence.

---

## 14. Open risks (honest)

1. **XAUUSD CFD prop** almost never emits 13/14. The code path will be cold. F17* must stay in CI so a later exchange-backed gold feed does not silently poison first-3.
2. **Some brokers** may zero `volume` or `symbol` on cancel. Volume is ignored anyway. Empty symbol can miss dirty (A21 order); §7 empty-symbol carve-out is the mitigation.
3. **Encoding B remaining is wrong by construction.** Dirty+exclude is the accepted loss. A later human/recon job may compare `mt5_positions_current` and raise `RECON_POSITION_MISMATCH` (out of scope here).
4. **A22 §2.1** does not yet say `dirty == false`. Scoring implementers must not count F17’s 5017 if they query `completed=true` only.
5. **Ingest first-write-wins** (`UpsertDealAsync`) is the highest-probability way this rule fails in production. Reconstruction cannot see a cancel that was never stored.

---

## 15. Sources

| Source | What it proves |
|---|---|
| https://www.mql5.com/en/docs/constants/tradingconstants/dealproperties | In-place type change; P/L zeroed; separate balance clawback |
| https://www.mql5.com/en/book/automation/experts/experts_deal_properties | Same model, book wording |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` | `DEAL_BUY_CANCELED=13`, `DEAL_SELL_CANCELED=14` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h` | `"canceled buy"` / `"canceled sell"` |
| Official report plugins under `mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\` | Market stats require action ∈ {0,1} |
| `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` §4.4, §6, F17 | `RECON_CANCELED_DEAL`, dirty, no inverse |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | `IsTradingDeal` excludes 13/14 |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | silent drop today |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | first-write-wins upsert |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` `cacheRecentDeal` / `OnDealUpdate` | in-place ticket update on the C++ ring |

---

**Product source was not modified.** This file is the only write from A83.
