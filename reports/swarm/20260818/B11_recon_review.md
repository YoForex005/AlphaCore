# B11 — Adversarial review: `TradeReconstructor` IN / OUT / INOUT / scale-in / partial / reverse

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\B11_recon_review.md` |
| Agent | B11 (reconstruction review) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (334 lines, read in full as of 13:20 local) |
| Adjacent types (read, not edited) | `NormalizedDeal.cs`, `ReconstructedTradeResult.cs`, `DealEntry.cs`, `DealAction.cs`, `VolumeConverter.cs`, `SymbolNormalizer.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `Mt5Deal.cs`, `ReconstructedTrade.cs`, `BaselineScorer.cs` |
| Binding contract | `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` §§1–7, fixtures F01–F25 |
| SDK semantics | `A37_mt5_deal_enums.md` (`ENTRY_IN/OUT/INOUT/OUT_BY`, `Volume` vs `VolumeClosed`) |
| Tests executed | `dotnet test tests/Unit --filter FullyQualifiedName~TradeReconstructionTests` → **5/5 passed** (2026-08-18 13:21). Those five facts do **not** cover the bugs below. |
| Method | Line-by-line adversarial walk of every `DealEntry` arm and `OpenTrade` mutator. Numeric traces against A21. Prefer false negatives over fake PASS. |

---

## 0. Verdict

`TradeReconstructor` is a **working happy-path netting book**: same-side `ENTRY_IN` scales, `ENTRY_OUT`/`OUT_BY` reduces, `ENTRY_INOUT` with `volume > remaining` splits into a completed lifecycle plus an opposite leftover. Simple long/short round-trips, scale-in VWAP, partial-then-flat, hedge-by-`position_id`, and netting reuse after flatten all behave.

It is **not** the A21 engine. It has **no dirty/failure channel**, **no `volume_closed`**, **no direction check**, **no `lifecycle_seq`**, and two logic holes that corrupt first-3 / scoring on any real reverse or any malformed entry:

| # | Class | One line |
|---|---|---|
| C1 | **P0 money** | `ApplyReverse` then `OpenTrade.Start` **re-applies** the INOUT deal’s profit/commission/swap onto the new side. A21 §7.6 / F04: money stays on the closed lifecycle only. F05 net on the new side is roughly **2×**. |
| C2 | **P0 data loss** | Opposite-side `ENTRY_IN` calls `ApplyReverse` and **discards** the closed trade (`_ = closed`). A21 F19 must dirty, not invent a reverse and delete the prior lifecycle. |
| C3 | **P0 inventory** | `ApplyReverse` **always** closes **all** `RemainingLots`, ignoring deal volume. INOUT with `volume < remaining` silently flattens the whole book. |
| C4 | **P0 overclose** | `CloseOut` does `Math.Min(lots, RemainingLots)`. OUT larger than remaining looks like a clean full close; the extra volume (the reverse that should have been INOUT) is dropped. A21 F18. |
| C5 | **P0 sign** | OUT / INOUT never check `DealAction` against open direction. Same-sign INOUT **completes** the current lifecycle and opens another same-side book (phantom first-3 trade). |

`WasAveragedDown` in the file **as it exists now** matches A21 (LONG add below prior VWAP; SHORT add above). The existing unit test `Scale_in_and_partial_close` only covers the long-cheaper case. Do not treat “5/5 green” as A21 coverage.

**A21 fixture scorecard (static + the 5 facts, not a fixture runner): 11 PASS / 4 PARTIAL / 10 FAIL.** That is not ≥95% behavioral parity. Do not ship first-3 / early-score on accounts that reverse.

---

## 1. What the file actually does

One book per `(brokerId, login, positionId)` after a global `Time, DealTicket` sort. Non-`Buy`/`Sell` rows are dropped before the switch (`IsTradingDeal`). Volume is converted to **decimal lots** via `VolumeConverter.Manager` (`native / 10_000`) on every deal.

| Entry | Arm | Intended | Actual |
|---|---|---|---|
| `In` (0) | `ApplyIn` | Open or same-side scale-in | Same. Opposite-side IN is a **synthetic reverse** whose closed half is thrown away. |
| `Out` (1) | `ApplyOut` → `CloseOut` | Reduce; complete iff remaining ~ 0 | Same on well-formed opposite-side OUTs. No sign check. Over-volume clipped. Flat OUT dropped. |
| `InOut` (2) | `ApplyReverse` | Close remaining + open leftover opposite | Closes **all** remaining at deal price; leftover = `dealLots - remaining`. Money applied twice. No sign / no `volume_closed` / no “must have leftover” check. Flat INOUT opens as IN. |
| `OutBy` (3) | same as `Out` | Close this `position_id` only | Same as OUT. No `position_by_id` (field does not exist on `NormalizedDeal`). Hedge counterpart is a separate group — this part is correct. |
| other (e.g. WebAPI `255`) | no `default` | `RECON_UNKNOWN_ENTRY` dirty | **Silent no-op.** Book may never flatten. |

Happy path that **does** work (keep these):

- F01-style IN + opposite OUT → one completed trade, VWAP = fill prices, `Completed=true`.
- Same-side second IN → `WasScaledIn`, `MaxVolumeLots` = peak remaining, entry VWAP = `Σ(px×lots)/Σ(lots)` using **decimal**.
- OUT with `lots < remaining` → `WasPartialClose`, not completed, not counted by `CountCompletedXauUsdTrades`.
- Later OUT that finishes the book → **one** trade, not two.
- INOUT with `dealLots > remaining` and opposite `Action` → two results, opposite directions, leftover size `dealLots - remaining` (unit test `Reverse_inout_closes_then_opens_opposite`).
- Flat then new IN on the same `position_id` → new `OpenTrade` (F11 shape). Distinct `Id` via `OpenedAt` ms.
- Distinct `position_id`s never merge (hedge).
- Balance / credit / commission actions never open a book.

---

## 2. Critical bugs (IN / OUT / INOUT / scale-in / partial / reverse)

### C1 — INOUT money is applied to **both** lifecycles (P0)

A21 §7.6: the INOUT deal’s profit / commission / storage / fee are assigned **entirely** to the **closed** lifecycle. The new seq must list the same ticket and **must not** `apply_money` again. F04: leftover SHORT `net = 0`.

What the code does:

1. `ApplyReverse` → `CloseOut` → `ApplyCommon` adds `Profit`/`Commission`/`Swap` to the old book (correct).
2. Leftover `OpenTrade.Start` **also** does:

```226:231:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            trade.AddDealMeta(deal);
            trade._entryNotional = deal.Price * lots;
            trade._entryLots = lots;
            trade.Commission += deal.Commission;
            trade.Swap += deal.Swap;
            trade.GrossRealizedPnl += deal.Profit;
```

`Start` has no “reversal open, skip money” path.

**F04 numeric (volume_h translated to Manager native: 10_000 / 15_000):**

| Ticket | Entry | Action | Lots | Price | Profit | Comm |
|---:|---|---|---:|---:|---:|---:|
| 401 | IN | BUY | 1.00 | 2400 | 0 | −0.70 |
| 402 | INOUT | SELL | 1.50 | 2410 | 10 | −1.05 |

| Lifecycle | Spec net | Code net | Tickets |
|---|---:|---:|---|
| seq1 LONG completed | **8.25** = 10 − 0.70 − 1.05 | 8.25 | 401, 402 |
| seq2 SHORT open | **0** | **8.95** = 10 − 1.05 | 402 |

VWAP leftover is correct (`2410` on `0.50` lots). Only money is wrong.

**F05** then closes the short (BUY OUT 0.50 @ 2390, profit 10, comm −0.35):

| Lifecycle | Spec net | Code net |
|---|---:|---:|
| seq1 LONG | 8.25 | 8.25 |
| seq2 SHORT | **9.65** = 10 − 0.35 | **18.60** = (10+10) + (−1.05−0.35) |
| Σ completed | **17.90** | **26.85** |

`BaselineScorer` sums `NetRealizedPnl` across completed XAU. Any trader who reverses **inflates profit factor, flips martingale (sign of net), and warps drawdown**. The unit test `Reverse_inout_closes_then_opens_opposite` asserts direction and leftover **lots only** — this bug is green today.

---

### C2 — Opposite `ENTRY_IN` synthesizes a reverse and **deletes** the closed trade (P0)

A21 §7.4 / F19: `ENTRY_IN` whose sign ≠ remaining is `RECON_IN_OPPOSITE_DIRECTION`, dirty, stop. It is **not** a reverse. Reverse is `ENTRY_INOUT` only.

```122:125:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        // Unexpected opposite IN on same position id: treat remaining as reverse remainder.
        var (closed, next) = ApplyReverse(open, deal, lots + open.RemainingLots, brokerId, login, positionId);
        _ = closed;
        return next ?? OpenTrade.Start(brokerId, login, positionId, deal, lots, direction, _symbols);
```

`_ = closed` is an explicit throw-away. `ReconstructPosition` never sees that completed lifecycle.

**F19 trace (long 1.00, then IN SELL 0.50):**

| Spec | Code |
|---|---|
| Dirty `5019/1`, `completed_count=0` | `ApplyReverse(dealLots=1.50)` closes 1.00, leftover 0.50 SHORT |
| Prior LONG still on the book (dirty) | Prior LONG **absent** from results |
| No new lifecycle | One **open SHORT 0.50** whose `GrossRealizedPnl` is the discarded close’s profit (C1) |

First-3 **undercounts** (a real closed long never happened as a trade). If the leftover later closes, only the synthetic short counts — wrong direction, wrong size, wrong PnL.

`lots + RemainingLots` is also the wrong volume model: a 1.50 opposite IN against 1.00 remaining becomes leftover **1.50**, not a 0.50 net reverse. The engine is inventing close volume that was not on the deal.

---

### C3 — INOUT always closes **all** remaining, even when deal volume is smaller (P0)

```155:161:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var closeLots = open.RemainingLots;
        open.CloseOut(deal, closeLots);
        var closed = open.ToResult(completed: true);

        var leftover = dealLots - closeLots;
        if (leftover <= FlatEpsilon)
            return (closed, null);
```

`closeLots` is **not** `min(dealLots, remaining)` and **not** `volume_closed`. It is always the full book.

| Deal | Remaining | Spec | Code |
|---|---:|---|---|
| INOUT SELL 1.50 vs long 1.00 | 1.00 | close 1.00, open short 0.50 | same (happy path) |
| INOUT SELL 1.00 vs long 1.00 | 1.00 | `RECON_INOUT_NO_NEW_VOLUME` (F20) | leftover 0 → **one completed** clean trade |
| INOUT SELL 0.30 vs long 1.00 | 1.00 | invalid (should have been OUT 0.30) | leftover −0.70 ≤ ε → **flatten 1.00**, 0.70 inventory vanished |

A mis-tagged partial (or a feed that puts **new-side only** in `Volume` and closed size in `VolumeClosed`) **wipes the position**. `NormalizedDeal` has **no** `VolumeClosed` / `volume_closed_h`, so the engine cannot implement A21’s `closed_h = volume_closed_h ?? remaining` or F21.

---

### C4 — OUT over-close is clipped; extra volume (the reverse) is lost (P0)

```252:255:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public void CloseOut(NormalizedDeal deal, decimal lots)
        {
            var closeLots = Math.Min(lots, RemainingLots);
```

A21 F18: long 1.00 then OUT SELL 1.50 → `RECON_OUT_OVERCLOSE` (broker would have sent INOUT). Code: `min(1.50, 1.00) = 1.00`, remaining 0, `WasPartialClose` stays **false** (because `closeLots == remaining`), result looks like a perfect full close. The 0.50 that should have opened the opposite side **does not exist**.

Same clip applies to `OUT_BY`.

---

### C5 — No sign check on OUT / INOUT (P0)

A21 §7.5–7.6: LONG remaining must be reduced by **SELL**; SHORT by **BUY**. Same-sign is `RECON_OUT_SAME_DIRECTION` / `RECON_INOUT_SAME_DIRECTION`.

Code derives the **new** INOUT direction from `deal.Action` and never compares it to `open.Direction`.

**Same-sign INOUT (long 1.00, INOUT BUY 1.50):**

1. `newDirection = Long`
2. Close **all** 1.00 at the BUY price (economically a buy cannot close a long)
3. leftover 0.50 → new **Long**
4. One **completed** long + one open long on the same `position_id`

That completed row increments `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible`. First-3 can latch on phantom closes.

**Same-sign OUT (long 1.00, OUT BUY 0.40):** `CloseOut` reduces to 0.60 and writes a BUY into exit VWAP. Inventory and exit VWAP are both wrong; no dirty.

---

## 3. High (first-3, identity, failures A21 requires)

### H1 — There is no dirty / failure model

A21 §4.4 lists 18 `RECON_*` codes. `ReconstructedTradeResult` has no `Dirty`, no `FailureCode`, no `LifecycleSeq`. Every completed row is first-3 eligible.

Silent substitutes in this file:

| Spec code | Code behavior |
|---|---|
| `RECON_IN_OPPOSITE_DIRECTION` | C2: invent reverse, drop closed |
| `RECON_OUT_FLAT` | `if (open is null) continue;` |
| `RECON_OUT_SAME_DIRECTION` | C5: apply anyway |
| `RECON_OUT_OVERCLOSE` | C4: clip |
| `RECON_INOUT_FLAT` | `ApplyReverse(null)` → `Start` as if IN |
| `RECON_INOUT_SAME_DIRECTION` | C5 |
| `RECON_INOUT_NO_NEW_VOLUME` | C3: treat as flatten |
| `RECON_INOUT_CLOSED_MISMATCH` | impossible (no `volume_closed`) |
| `RECON_ZERO_VOLUME` | `if (lots <= 0) continue;` |
| `RECON_BAD_PRICE` | never checked; `price=0` poisons VWAP |
| `RECON_MISSING_POSITION_ID` | `GroupBy(PositionId)` opens a book on `0` |
| `RECON_CANCELED_DEAL` | `IsTradingDeal` is false → skip; book stays **clean** (F17: 5017 would remain an open non-dirty long and is not excluded) |
| `RECON_UNKNOWN_ENTRY` | fall through the `switch` |
| `RECON_VOLUME_NOT_QUANTIZED` | any `ulong/10000` lot size accepted |

Canceled rows are correctly **not** inverted (A83). They are incorrectly **not** dirty. First-3 on an account with a manager cancel is optimistic.

### H2 — No `lifecycle_seq`; identity is `OpenedAt` milliseconds

```300:300:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
                Id = $"{BrokerId}:{Login}:{PositionId}:{OpenedAt.ToUnixTimeMilliseconds()}",
```

A21 key is `(broker_id, login, position_id, lifecycle_seq)` with seq incrementing on flatten+reopen **and** on INOUT. Two lifecycles that share a millisecond open (second-resolution ingest, or OUT+IN in the same `DealTime`) **collide**. `EfTradingStore.ReplaceReconstructedAsync` throws this `Id` away and inserts `Guid.NewGuid()`, so the DB key is not the A21 key either (`ReconstructedTrade` has no seq column).

### H3 — Duplicate tickets are applied twice (F16 FAIL)

A21: latest revision per `(broker_id, deal_ticket)`; replay `[101,102,101,102]` → `deal_count=2`. Code has no seen-ticket set. A duplicated OUT can flatten twice (second is H1 `OUT_FLAT` skip) or a duplicated IN can **double scale-in**. Ingest `UpsertDealAsync` is insert-if-missing, but `Reconstruct` is a pure function of the list the caller hands it. `LoadDealsAsync` does not distinct tickets.

### H4 — First-3 order is not apply order

- `Reconstruct` returns `OpenedAt, PositionId` (not close time, not ticket).
- `CompletedXauUsdTrades` re-sorts `ClosedAt, OpenedAt` — **no `ThenBy` ticket**.
- `ReconstructionScoringService.RebuildTraderAsync` scores `Reconstruct(...).Where(Completed && IsXauUsd)` and relies on `BaselineScorer` to `OrderBy(ClosedAt)` only.
- A21 first-3 is apply order = `(time_msc, ticket)` of the completing deal (F06 / F12 / F09 same-ms OUT_BY pair).

`IsEarlyScoreEligible` is a **count ≥ 3**, so latch *existence* is OK when every completed XAU is clean. Latch *identity* (`first3_keys`, `early_score_deal_ticket`) **does not exist**. Trade #4 is not excluded from scoring (`BaselineScorer` uses the **entire** completed XAU list). That last point is a scorer bug; reconstruction does not emit a first-3 slice.

### H5 — Volume is decimal lots + `1e-7` epsilon, not integer hundredths

A21 §2 / §8.7: remaining is `int64` hundredths; flatten is `remaining_h == 0`. Never IEEE lots.

This file converts immediately (`ToLots`) and tests flatten with `RemainingLots <= 0.0000001m`. With `VolumeConverter.Manager` and `decimal`, `k/10000` is exact, so **well-quantized Manager volumes will not drift**. The failure mode is contract, not float dust:

- Sub-0.01 lots are accepted (spec: `RECON_VOLUME_NOT_QUANTIZED`).
- There is no adapter that takes A21 fixture `volume_h` (1.00 lot = **100**). Feeding F01 `volume_h=100` as `VolumeNative` through Manager yields **0.01** lots, not 1.00. The five unit tests use Manager native `1000` = 0.10 lots — they are **not** A21 fixtures.
- `VolumeClosed` is never ingested (`Mt5Deal`, `Mt5DealDto`, `NormalizedDeal`).

Default scale `10_000` itself is correct for `IMTDeal::Volume()` (B14). The missing piece is the A21 hundredths book **after** that conversion.

### H6 — `Fees` are hardcoded `0`; fee/reason/time_msc are not on the deal

```297:318:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            var fees = 0m;
            ...
                Fees = fees,
                NetRealizedPnl = GrossRealizedPnl + Commission + Swap + fees,
```

A21 `net = gross + commission + swap + fee`. Ingest DTO has no `Fee`, `Reason`, `TimeMsc`, `PositionById`, `VolumeClosed`. SL/TP exist on `NormalizedDeal` but `LoadDealsAsync` never sets them, so production `InitialSl`/`FinalSl` are always null even though `ApplyCommon` would honor them.

---

## 4. Scale-in / partial / averaging — what is right, what is thin

### 4.1 Scale-in (mostly PASS)

Same-side `ENTRY_IN` → `ScaleIn`:

- `WasScaledIn = true` (including two INs on one `order_ticket` — A21 F10).
- `RemainingLots += lots`; `MaxVolumeLots` is peak **remaining**, not sum of all entries (correct if a partial happened in between).
- `InitialVolumeLots` stays the first open (correct).
- Entry VWAP updated **after** the avg-down compare (correct: “VWAP before that fill”).
- Direction does not flip mid-lifecycle (new `OpenTrade` only on reverse / new seq).

### 4.2 Averaging-down (PASS on current source)

```237:242:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            // Averaging down: add to a long after price fell, or to a short after price rose.
            var worse = Direction == TradeDirection.Long
                ? deal.Price < EntryVwap
                : deal.Price > EntryVwap;
            if (worse)
                WasAveragedDown = true;
```

Matches A21 §4.1 / F07 / F08. Equal price is not avg-down. Flag is sticky. `Scale_in_and_partial_close` buys 2290 after 2300 and expects `true` — **passes**.

Gaps: no unit fact for SHORT add-higher (F08), LONG add-higher must stay `false` (F02), or add-in-profit vs add-in-loss. Scoring treats **any** `WasAveragedDown` on the **lifetime** completed set as +20 risk / −15 behavior — a false positive here is expensive; the predicate itself is now the spec predicate.

### 4.3 Partial close (PASS on well-formed OUTs)

`closeLots < RemainingLots - ε` → `WasPartialClose`. A later full close keeps the flag. `CountCompletedXauUsdTrades` ignores the mid-book state. F03 shape is correct.

Holes (not the happy path):

- Over-close (C4) never sets `WasPartialClose` and never stays open.
- Zero-volume OUT is skipped; an otherwise-completable book stays open forever.
- Partial then scale-in keeps a **historical** entry VWAP (`Σ all IN lots`, not remaining inventory). That matches A21 (`open_vol_sum_h` is not reduced on OUT). Do not “fix” this to inventory VWAP without changing A21.

### 4.4 Reverse leftover size / tickets (PASS) vs money (FAIL)

Happy-path leftover lots, direction, `OpenedAt = INOUT time`, ticket listed on **both** trades (`AddDealMeta` in `CloseOut` and again in `Start`) match §7.6. Unit test checks leftover `0.10` lots for native `1000` then `2000`. Money / fees / dirty / `volume_closed` are untested.

---

## 5. A21 fixture scorecard

Interpretation: **PASS** = this function, given correctly scaled Manager-native deals and mapped symbols, would match the fixture’s trade count, sides, volumes, flags, and money. **PARTIAL** = shape OK, identity/events/money/order incomplete. **FAIL** = wrong trades or wrong money.

| Fx | Intent | Result | Why |
|---|---|---|---|
| F01 | Simple long | **PASS** | Covered by `Reconstructs_simple_round_trip` (different numbers). |
| F02 | Scale-in VWAP | **PASS** | Math and `WasScaledIn` / not avg-down (2410 > 2400 long). |
| F03 | Partial then rest | **PASS** | One trade, `WasPartialClose`, exit VWAP blend. |
| F04 | INOUT split + money on A | **FAIL** | Leftover size/dir OK; leftover **money ≠ 0** (C1). |
| F05 | Close leftover | **FAIL** | C1 doubles INOUT money onto seq2. |
| F06 | First-3 + aliases + partial GOLD | **PARTIAL** | Count / XAU map likely OK (`GOLD`/`XAUUSDm`/`XAUUSD.`). No `first3_keys`, no event on ticket 609, scorer uses all completed. |
| F07 | Avg-down long | **PASS** | Current `<` compare. |
| F08 | Avg-down short | **PASS** (untested) | Current `>` compare. No fact. |
| F09 | Hedge OUT_BY pair | **PASS** | Two `position_id`s, OUT_BY ≡ OUT. No `position_by` audit field. Close-time tie-break is not ticket. |
| F10 | Same-order partial fills | **PASS** | Two IN → scaled; `OrderCount=2`. |
| F11 | Netting reuse | **PASS** | `open=null` after complete; new IN starts seq-less new Id. |
| F12 | First-3 includes a reverse | **FAIL** | Seq2 money wrong (C1); no first3 slice (eligible count would still hit 3 at 927). |
| F13 | Open only | **PASS** | `Completed=false`, count 0. |
| F14 | Balance skip | **PASS** | `Ignores_balance_deals`. |
| F15 | Sort OUT-before-IN in list | **PASS** | Sorts `Time, DealTicket` before apply. |
| F16 | Duplicate tickets | **FAIL** | No dedupe. |
| F17 | Cancel dirty | **FAIL** | Cancel skipped; 5017 stays clean open; not excluded from anything because it never completes — but it is not marked dirty if later flattened by a later deal on the same id. |
| F18 | OUT overclose | **FAIL** | C4 clip + clean complete. |
| F19 | Opposite IN | **FAIL** | C2. |
| F20 | INOUT flatten (no new) | **FAIL** | Clean complete instead of dirty. |
| F21 | `volume_closed` ≠ remaining | **FAIL** | Field absent; inferred remaining. |
| F22 | `position_id=0` | **FAIL** | Opens a book. |
| F23 | Two brokers | **PASS** | `brokerId` filter. |
| F24 | Unmapped vs `XAUUSD.a` | **PASS** | `IsXauUsd` after `SymbolNormalizer` (note: normalizer is **more** permissive than A21 suffix allow-list — any `XAUUSD*` compact). |
| F25 | Three simples → eligible | **PASS** | `First_three_completed_xau_unlocks_early_score`. |

**Tally: 11 PASS / 4 PARTIAL / 10 FAIL** of 25. Replay-stability of **flags and leftover lots** is likely; replay-stability of **money on reverse** is stably **wrong**.

A21 §11 unit matrix (27 rows): this repo has **5** reconstruction facts. Named A89 classes (`PositionReversalReconstructionTests`, `AveragingDownFlagReconstructionTests`, …) are **not on disk**.

---

## 6. Worked adversarial traces (copy into tests; do not implement here)

Volumes below are **lots**. Native = lots × 10_000 if using `VolumeConverter.Manager`.

### T1 — C1 money (must fail today)

`IN BUY 1.00 @ 2400 comm −0.70` then `INOUT SELL 1.50 @ 2410 profit 10 comm −1.05`.

Assert: completed LONG `Net=8.25`, `DealTickets` contain both; open SHORT `Remaining=0.50`, `Gross=0`, `Commission=0`, `DealTickets==[inout]`.

Today: open SHORT `Gross=10`, `Commission=−1.05`.

### T2 — C2 opposite IN (must fail today)

`IN BUY 1.00` then `IN SELL 0.50`.

Assert: one dirty open LONG remaining 1.00, `completed_count=0`, no SHORT.

Today: one open SHORT 0.50; LONG gone.

### T3 — C3 under-volume INOUT (must fail today)

`IN BUY 1.00` then `INOUT SELL 0.30`.

Assert: dirty or still long 0.70, not completed.

Today: one completed LONG 1.00, no remainder.

### T4 — C4 OUT overclose (must fail today)

`IN BUY 1.00` then `OUT SELL 1.50`.

Assert: dirty, `completed_count=0`, not a clean 1.00 close.

Today: one completed LONG, `WasPartialClose=false`, no leftover SHORT.

### T5 — C5 same-sign INOUT (must fail today)

`IN BUY 1.00` then `INOUT BUY 1.50`.

Assert: dirty, still one long book.

Today: completed LONG 1.00 + open LONG 0.50 → `CountCompletedXauUsdTrades` += 1.

### T6 — F20 INOUT exact flatten

`IN BUY 1.00` then `INOUT SELL 1.00`.

Assert: `RECON_INOUT_NO_NEW_VOLUME`, not first-3.

Today: one completed trade (same as OUT).

### T7 — Partial is not trade #2 (already PASS)

`IN 1.00`, `OUT 0.40`, `OUT 0.60` → one completed, `WasPartialClose=true`, count 1.

### T8 — Reverse then close, Σ net (C1)

F05 numbers: Σ completed nets must be **17.90**, not **26.85**.

---

## 7. Adjacent holes that amplify these bugs (not inside the switch, still reconstruction)

| Hole | Path | Effect |
|---|---|---|
| Persist drops remainder / tickets / string Id | `EfTradingStore.ReplaceReconstructedAsync` | Open leftover after INOUT cannot be recovered from `reconstructed_trades`. `RemainingVolumeLots` and `DealTickets` are result-only. |
| No `VolumeClosed` on the wire type | `Mt5DealDto` / `Mt5Deal` / `NormalizedDeal` | C3/F21 cannot be implemented without a schema change. |
| SL/TP never loaded | `LoadDealsAsync` | `InitialSl`/`FinalSl` always null in prod; `SlUseRate` in scoring is 0. |
| INOUT FinalSl on the **closed** side | `ApplyCommon` during `CloseOut` | If the broker puts the **new** position SL on the INOUT deal, the closed trade’s `FinalSl` becomes the new side’s SL. A21 §7.6 also `maybe_update_sl_tp` on the close path — flag as economically odd, spec-aligned. |
| `IsEarlyScoreEligible` does not exclude dirty | this file | There is nothing to exclude. |
| Symbol filter is after the book | `CompletedXauUsdTrades` | Non-XAU books are still reconstructed (harmless extra). A21 drops them before the book. |

---

## 8. What a fix must change (guidance only — this review does not edit product source)

Priority if someone implements later, in this file only after `NormalizedDeal` grows `VolumeClosed` / `Fee` / a dirty bit:

1. Split `OpenTrade.Start` into “open from IN” (apply money) vs “open from INOUT leftover” (ticket + VWAP + SL/TP, **no** money). Fixes C1 / F04 / F05.
2. Delete the opposite-IN reverse hack. Opposite IN → dirty, keep the current book. Fixes C2 / F19.
3. INOUT: require open book, opposite sign, `dealLots > closeLots`, `closeLots = volume_closed ?? remaining`, `volume_closed` if present must equal remaining. Otherwise dirty, do not flatten. Fixes C3 / C5 / F20 / F21.
4. OUT: require open book, opposite sign, `lots <= remaining`; else dirty. Do not `Min`. Fixes C4 / C5 / F18.
5. Integer `remaining_h` (hundredths) after one adapter. `lifecycle_seq` on the result. Seen-ticket set. Canceled → dirty. `position_id==0` / `price<=0` / unknown entry → dirty.
6. Port A21 F01–F25 as facts **before** claiming reconstruction is done. The current 5 facts are a smoke suite.

---

## 9. Honesty box

| Claim someone might make | Measured |
|---|---|
| “EX5-style 95% reconstruction” | **No.** 10/25 A21 fixtures fail; reverse money is systematically wrong. |
| “Reversal is implemented” | **Lots/direction leftover: yes. Money/dirty/volume_closed: no.** |
| “Averaging-down is inverted” | **Not on the file as of this review.** LONG `<` / SHORT `>` matches A21. |
| “5 unit tests pass ⇒ recon is done” | **Those tests do not assert F04 money, F18–F22, or short avg-down.** |
| “First 3 trades are A21 first-3” | **Count of completed XAU ≥ 3 only.** No keys, no dirty exclude, no apply-order slice. |
| Product source modified for this review | **No.** |

---

## 10. Files cited

- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs`
- `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs`
- `D:\Prop\src\Domain\Enums\DealEntry.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`ReconstructionScoringService.RebuildTraderAsync`)
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md`
- `D:\Prop\docs\trade-reconstruction.md` (one-page summary; same happy-path contract, no failure rules)
