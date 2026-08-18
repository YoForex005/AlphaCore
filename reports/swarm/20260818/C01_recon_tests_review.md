# C01 — Trade reconstruction tests review (scale-in, partial, reverse, first-3)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C01_recon_tests_review.md` |
| Agent | C01 (recon tests review) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Question | Are `TradeReconstructionTests` sufficient for **scale-in**, **partial**, **reverse**, **first-3**? |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| Tests read | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` (5 `[Fact]`s). No sibling reconstruction classes on disk. |
| Law | Architecture v2 §§14–17, §35, §60; `D:\Prop\docs\trade-reconstruction.md`; `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` (F01–F21); A09 / A27 inventory. |
| Method | Read the two assigned files and adjacent types. Mapped every assertion to SUT branches. Ran the class. Nothing answered from memory. |

---

## 0. Verdict

**NO. Tests are not sufficient.**

They are five passing **happy-path smokes**, not a reconstruction contract. Scale-in and partial are fused into one fact. Reverse and first-3 assert flags/counts only. **0 of 21 A21 fixtures** are encoded. **0 of the 4 named A27 reconstruction classes** exist. A green `dotnet test` here does **not** prove §14 / §15 / §35 / §60.

| Assigned area | On-disk fact | Isolated? | Bit-for-bit vs A21? | Verdict |
|---|---|---|---|---|
| Scale-in | `Scale_in_and_partial_close` (shared) | **No** | **No** (not F02 / F07 / F08 / F10) | **INSUFFICIENT** |
| Partial close | same fact (shared) | **No** | **No** (not F03) | **INSUFFICIENT** |
| Reverse (`ENTRY_INOUT`) | `Reverse_inout_closes_then_opens_opposite` | Smoke only | **No** (not F04 / F05 / F12 / F20) | **INSUFFICIENT** |
| First 3 completed XAUUSD | `First_three_completed_xau_unlocks_early_score` | Positive latch only | **No** (not F06 / F12 / F13) | **INSUFFICIENT** |

**One-line:** 5/5 pass because they never ask the numbers the spec requires, and they never ask the negative cases that decide first-3.

Do **not** claim “reconstruction unit tests cover scale-in / partial / reverse / first-3.”

---

## 1. What was actually read

### 1.1 Test file (entire, 113 lines)

`D:\Prop\tests\Unit\TradeReconstructionTests.cs`

| Line | Fact | Role |
|---:|---|---|
| 13–30 | `Reconstructs_simple_round_trip` | One long IN/OUT. Baseline, not one of the four assigned areas. |
| 33–49 | `Scale_in_and_partial_close` | **Both** extra `ENTRY_IN` and two `ENTRY_OUT` on one `position_id`. |
| 51–67 | `Reverse_inout_closes_then_opens_opposite` | Long 0.10 + `InOut` sell 0.20 → 2 trades. |
| 69–81 | `First_three_completed_xau_unlocks_early_score` | Three independent XAUUSDm round-trips → count 3, eligible true. |
| 84–91 | `Ignores_balance_deals` | `DealAction.Balance` only. Noise filter, not first-3 isolation. |
| 93–111 | `Deal(...)` helper | Always `ACHIEVER` / login 1 / `XAUUSDm` / commission 0 / swap 0 / no SL/TP / `OrderTicket = DealTicket`. |

Signature trap (easy to misread the scale-in fixture):

```93:94:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    private static NormalizedDeal Deal(
        long ticket, long position, DealAction action, DealEntry entry, ulong volume, decimal price, decimal profit, int t) =>
```

Second argument is **`PositionId`**, not volume. All four scale/partial/reverse deals use `VolumeNative = 1000` (= 0.10 lot under `VolumeConverter.Manager` scale 10_000). That part of the fixture is consistent with the simple-round-trip fact (`1000 → 0.10m`).

### 1.2 SUT (entire, 334 lines)

`D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`

| Surface | Behavior that tests must lock |
|---|---|
| `Reconstruct` | Filter BUY/SELL + broker + login; sort `(Time, DealTicket)`; **group by `PositionId`**; emit open remainder as `completed=false`. |
| `CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` | Reconstruct all, then `Completed && IsXauUsd`, sort `(ClosedAt, OpenedAt)`, eligible iff count `>= 3`. |
| `ApplyIn` | Open, or same-side `ScaleIn`, or **silent reverse** on opposite `ENTRY_IN`. |
| `ApplyOut` / `OutBy` | Reduce; complete only when `RemainingLots <= 1e-7`. `Out` on flat = **skip**. Overclose = **clamp**, not fail. |
| `ApplyReverse` | Close remaining at deal price; leftover lots start opposite. `OpenTrade.Start` **re-applies** the same deal’s profit/commission/swap. |
| `ScaleIn` / `CloseOut` | Set `WasScaledIn` / `WasAveragedDown` / `WasPartialClose`; VWAP via decimal lot notional. |

Adjacent types inspected (not edited): `NormalizedDeal` (no `volume_closed_h`, no fee), `ReconstructedTradeResult` (no `lifecycle_seq`, no `dirty`, no first-3 keys), `ReconstructedTrade` entity, `VolumeConverter.Manager`, `SymbolNormalizer`, `docs/trade-reconstruction.md`, A21, A09, A27. `BaselineScorerTests` builds **hand-made** `ReconstructedTradeResult`s and does not exercise the reconstructor. Integration `SeedingAndStoreTests` only asserts `Any(completed XAU)` after seed.

Named A09 / A27 classes **absent** from `D:\Prop\tests\Unit`:

- `PartialCloseReconstructionTests`
- `ScaleInReconstructionTests`
- `FullCloseReconstructionTests`
- `PositionReversalReconstructionTests`
- `FirstThreeCompletedXauTradesTests`

### 1.3 Measured run (this review)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~TradeReconstructionTests --nologo
```

```text
Passed  Ignores_balance_deals
Passed  First_three_completed_xau_unlocks_early_score
Passed  Scale_in_and_partial_close
Passed  Reverse_inout_closes_then_opens_opposite
Passed  Reconstructs_simple_round_trip

Total tests: 5
Passed: 5
```

**5/5 green is not coverage.** The facts do not assert VWAP, remaining/closed/initial on the combined path, reverse money split, first-3 negatives, or any A21 failure code.

---

## 2. Assertion map (what is actually locked)

| Field / rule | Simple | Scale+partial | Reverse | First-3 | Balance |
|---|---|---|---|---|---|
| Trade count | 1 | 1 | 2 | — | 0 |
| `Completed` | T | T | T then F | via count helpers | — |
| `Direction` | Long | — | Long then Short | — | — |
| `IsXauUsd` | T | — | — | implicit (`XAUUSDm`) | — |
| `InitialVolumeLots` | 0.10 | — | — | — | — |
| `MaxVolumeLots` | — | 0.20 | — | — | — |
| `ClosedVolumeLots` | — | — | — | — | — |
| `RemainingVolumeLots` | — | — | 0.10 on #2 | — | — |
| `EntryVwap` / `ExitVwap` | 2320 / 2330 | — | — | — | — |
| `NetRealizedPnl` | 100 | — | — | — | — |
| Gross / commission / swap / fees | — | — | — | — | — |
| `WasScaledIn` | — | T | — | — | — |
| `WasPartialClose` | — | T | — | — | — |
| `WasAveragedDown` | — | T | — | — | — |
| `DealCount` / tickets / orders | — | — | — | — | — |
| `PositionId` / `lifecycle_seq` | — | — | — | — | — |
| `CountCompletedXauUsdTrades` | — | — | — | `== 3` | — |
| `IsEarlyScoreEligible` | — | — | — | `true` | — |
| Eligible is **false** at N=0/1/2 | — | — | — | **NO** | — |
| N=4 latch / not `PROVEN_PROFITABLE` | — | — | — | **NO** | — |
| Non-XAU / open / partial excluded | — | — | — | **NO** | — |

Dashes are unasserted. That is the review.

---

## 3. Scale-in — **INSUFFICIENT**

### 3.1 What the fact does

```33:48:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    public void Scale_in_and_partial_close()
    {
        var deals = new[]
        {
            Deal(1, 20, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 1),
            Deal(2, 20, DealAction.Buy, DealEntry.In, 1000, 2290m, 0, t: 2),
            Deal(3, 20, DealAction.Sell, DealEntry.Out, 1000, 2310m, 20, t: 3),
            Deal(4, 20, DealAction.Sell, DealEntry.Out, 1000, 2320m, 40, t: 4)
        };
        // ...
        trade.WasScaledIn.Should().BeTrue();
        trade.WasPartialClose.Should().BeTrue();
        trade.WasAveragedDown.Should().BeTrue();
        trade.Completed.Should().BeTrue();
        trade.MaxVolumeLots.Should().Be(0.20m);
    }
```

Same `PositionId=20`. Two 0.10 buys (second worse for a long → avg-down), then two 0.10 sells. One completed lifecycle. Flags + `MaxVolumeLots` only.

### 3.2 Why that is not scale-in coverage

Architecture §60 and A27 list **scale-in as its own required area**. A21 splits it:

| Fixture | Intent | Present? |
|---|---|---|
| **F02** | Scale-in then **one** full close; VWAP; **not** averaged down | **No** |
| **F07** | Long avg-down + exact `entry_vwap=2390` / net | Only the boolean |
| **F08** | **Short** avg-down (`price > vwap`) | **No** |
| **F10** | Same-order partial fills still `was_scaled_in`, `order_count=2` | **No** |
| Hedge (A21 §1.6) | Distinct `position_id`s are **not** one scale-in | **No** |

Unasserted quantities this fixture **already produces** (so omitting them is a choice, not a missing SUT):

```text
InitialVolumeLots     = 0.10
ClosedVolumeLots      = 0.20
RemainingVolumeLots   = 0
EntryVwap             = (2300*0.10 + 2290*0.10) / 0.20 = 2295
ExitVwap              = (2310*0.10 + 2320*0.10) / 0.20 = 2315
NetRealizedPnl        = 20 + 40 = 60
DealCount             = 4
Direction             = Long
```

A21 F02 also requires scale-in that is **not** averaged down (add long at a **higher** price). This fact **cannot** fail a regression that sets `WasAveragedDown = true` on every scale-in, because the only scale-in it uses **is** worse.

Short scale-in never runs `ScaleIn`’s other branch:

```236:242:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public void ScaleIn(NormalizedDeal deal, decimal lots)
        {
            // Averaging down: add to a long after price fell, or to a short after price rose.
            var worse = Direction == TradeDirection.Long
                ? deal.Price < EntryVwap
                : deal.Price > EntryVwap;
```

Also missing: three+ adds; scale-in then leave **open**; `max_volume` after add-then-partial-then-add; same `OrderTicket` two INs; broker/login mismatch ignored.

**Scale-in sufficiency: FAIL.** Flag smoke + max lots. Not VWAP, not isolated from partial, not short, not hedge, not F02.

---

## 4. Partial close — **INSUFFICIENT**

### 4.1 Collapsed into the same fact

There is **no** `PartialCloseReconstructionTests` and **no** partial-only deal list. The only `WasPartialClose` assert shares the book with two `ENTRY_IN`s.

A21 **F03** (the binding partial fixture) is:

```text
IN 100h @ 2400
OUT 40h @ 2410   → remaining > 0, completed_count still 0
OUT 60h @ 2420   → one trade, was_partial_close, exit_vwap = 2416
```

That isolation is the whole point of §15: **a partial close is not trade #2**.

### 4.2 What is not locked

| Required proof (A21 §1.2 / §7.5 / A27) | In tests? |
|---|---|
| Partial-only lifecycle (no scale-in) | **No** |
| After first OUT: still **one** open trade, `Completed=false`, first-3 count **0** | **No** (batch reconstruct only sees the final flat book) |
| `exit_vwap` weighted across two OUT prices | **No** |
| `WasPartialClose=false` on a single full OUT | **No** (simple fact never asserts the flag is false) |
| `ENTRY_OUT_BY` same as OUT for this `position_id` (F09 two books) | **No** |
| `Out` on flat skipped / should be `RECON_OUT_FLAT` | **No** |
| Overclose clamped (`Math.Min`) vs spec `RECON_OUT_OVERCLOSE` (F18) | **No** |
| Partial remaining **does not** increment `CountCompletedXauUsdTrades` | **No** |

SUT sets the flag only when `closeLots < RemainingLots - FlatEpsilon` (`CloseOut`, lines 262–263). A one-shot full close never sets it. Because no test asserts `WasPartialClose == false` on the simple round-trip, a bug that sets the flag on every OUT would still be green.

`DealEntry.OutBy` is compiled (`ApplyOut` shared case) and **never constructed** in this class.

**Partial sufficiency: FAIL.** One boolean on a mixed fixture. The §15 “partial is not a trade” rule is untested.

---

## 5. Reverse (`ENTRY_INOUT`) — **INSUFFICIENT**

### 5.1 What the fact does

```51:67:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    public void Reverse_inout_closes_then_opens_opposite()
    {
        var deals = new[]
        {
            Deal(1, 30, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 1),
            Deal(2, 30, DealAction.Sell, DealEntry.InOut, 2000, 2290m, -10, t: 2)
        };
        var trades = _r.Reconstruct("ACHIEVER", 1, deals);
        trades.Should().HaveCount(2);
        trades[0].Completed.Should().BeTrue();
        trades[0].Direction.Should().Be(TradeDirection.Long);
        trades[1].Completed.Should().BeFalse();
        trades[1].Direction.Should().Be(TradeDirection.Short);
        trades[1].RemainingVolumeLots.Should().Be(0.10m);
    }
```

Shape is in the same family as A21 **F04** (close long, open short leftover). Assertions stop at count / directions / leftover lots.

### 5.2 Unasserted F04 contract (this fixture already has the numbers)

A21 F04 / §7.6:

| Rule | Expected on this deal list | Asserted? |
|---|---|---|
| Same `PositionId`, two lifecycles | 30 / 30 | **No** |
| Closed #1 `Initial=Max=Closed=0.10`, `EntryVwap=2300`, `ExitVwap=2290` | yes | **No** |
| Open #2 `Initial=0.10`, `Closed=0`, `ExitVwap=null`, `EntryVwap=2290` | yes | **No** (only remaining) |
| Money of the INOUT deal stays on the **closed** side; new side **0** | #1 net −10; #2 net **0** | **No** |
| INOUT ticket listed on **both** trades | tickets `[1,2]` and `[2]` | **No** |
| Then close the new side → 2 completed, still not early-score (F05) | — | **No** |
| Short → long reverse | — | **No** |
| Same-size INOUT is illegal (`RECON_INOUT_NO_NEW_VOLUME`, F20) | — | **No** |
| INOUT on flat is illegal (`RECON_INOUT_FLAT`) | SUT **opens** a new book | **No** |
| `volume_closed_h` mismatch (F21) | `NormalizedDeal` has **no** closed-volume field | **No** |

### 5.3 Hidden SUT bug the current fact cannot catch

```155:163:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var closeLots = open.RemainingLots;
        open.CloseOut(deal, closeLots);          // ApplyCommon += deal.Profit
        var closed = open.ToResult(completed: true);
        var leftover = dealLots - closeLots;
        if (leftover <= FlatEpsilon)
            return (closed, null);
        return (closed, OpenTrade.Start(..., leftover, ...));  // Start += deal.Profit AGAIN
```

`OpenTrade.Start` (lines 229–232) always adds `deal.Profit` / `Commission` / `Swap`. On this fixture that means:

```text
trades[0].NetRealizedPnl = -10     // closed side — matches A21 “money on A”
trades[1].NetRealizedPnl = -10     // NEW side — A21 F04 requires 0
```

The fact never reads `NetRealizedPnl`. **Today’s reverse test is green with a double-count.** That is the definition of insufficient.

Related untested reverse paths:

| SUT branch | Spec | Test |
|---|---|---|
| Opposite `ENTRY_IN` → `ApplyReverse` with `lots + Remaining` (lines 122–125) | A21 F19: `RECON_IN_OPPOSITE_DIRECTION`, dirty, **not** a silent reverse | **None** |
| INOUT leftover `<= epsilon` | flatten-as-INOUT | **None** |
| Reverse then `OUT` of the new side (F05 / F12 trade #3/#4) | 2 or 4 completed | **None** |
| `lifecycle_seq` increment | A21 key `(broker, login, position, seq)` | Field **does not exist** on `ReconstructedTradeResult` |

**Reverse sufficiency: FAIL.** Directional smoke only. Money, tickets, seq, F05 close, and failure modes uncovered. A known money split bug is unguarded.

---

## 6. First-3 completed XAUUSD — **INSUFFICIENT**

### 6.1 What the fact does

```69:81:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    public void First_three_completed_xau_unlocks_early_score()
    {
        // 3× (Buy IN 0.10 + Sell OUT 0.10) on position_ids 100,101,102
        _r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals).Should().Be(3);
        _r.IsEarlyScoreEligible("ACHIEVER", 1, deals).Should().BeTrue();
    }
```

SUT implementation of the predicate is a one-liner:

```62:63:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public bool IsEarlyScoreEligible(...) =>
        CountCompletedXauUsdTrades(...) >= 3;
```

The fact only feeds the `true` side of `>= 3` with three already-clean XAUUSDm round-trips. It never reconstructs the trade list, never names first-3 keys, never checks suggested trader state, never mentions `PROVEN_PROFITABLE`.

`BaselineScorerTests.Two_trades_remain_insufficient` is **not** a reconstructor first-3 test: it injects pre-built results and never calls `TradeReconstructor`.

### 6.2 §15 table vs this class

| Event | Counts as a first-3 trade? | Tested? |
|---|---|---|
| Order / pending | No | **No** (no order-only input) |
| IN still open (F13) | No | **No** |
| Partial close remaining > 0 (F03 / F06 mid-GOLD) | No | **No** |
| SL/TP modify | No (not a deal) | **No** |
| Balance / credit / commission (F14) | No | Balance-only empty list; **not** mixed with XAU |
| Non-XAUUSD lifecycle (F06 EURUSD) | No | **No** — helper hardcodes `XAUUSDm` |
| Still-open reverse leftover (F04) | No | Reverse fact does not call `Count*` |
| Dirty / canceled (F17–F21) | No | SUT has **no** dirty flag |
| Completed XAUUSD #1,#2 | count 1/2, eligible **false** | **No** |
| Completed XAUUSD #3 | eligible **true** | **Yes** (this fact) |
| Completed XAUUSD #4 (F06 after 611) | still eligible; **not** `PROVEN_PROFITABLE` | **No** |
| GOLD / `XAUUSD.` / `XAUUSDm` map and count (F06) | Yes | Only `XAUUSDm` |
| Scale-in + reverse inside the first-3 set (F12) | Yes, with exact keys | **No** |
| Latch timestamp / completing deal ticket | F06 `early_score_at_msc=9000`, ticket 609 | **API absent** |
| Cross-login / cross-broker isolation | A21 §1.5 | **No** |

`Reconstruct` does **not** drop non-XAU (A21 §5 says drop). `CompletedXauUsdTrades` filters `IsXauUsd` after the fact. A EURUSD completed lifecycle would occupy a reconstructed slot and **must not** increment the counter. Untested.

`IsEarlyScoreEligible` is a recount, not a latch with `first3_keys[0..2]`. Changing `>= 3` to `>= 1` would **fail** this fact, but changing it to `>= 0`, counting **deals**, counting **open** trades, or counting **partials** would **not** be caught — the fixture has no opens, no partials, no foreign symbols, and no N<3 case.

If the SUT counted each OUT as a trade, this particular fixture would still show 3 (one OUT per lifecycle). The dangerous §15 failure mode is invisible here.

**First-3 sufficiency: FAIL.** Positive latch only. The definition of “first 3” is the exclusion list; that list is untested.

---

## 7. A21 fixture scoreboard (honest)

| ID | Topic | Encoded in `TradeReconstructionTests`? |
|---|---|---|
| F01 | Simple long | Partial cousin (different px/vol/comm; no first-3=1 assert) |
| F02 | Scale-in, not avg-down, VWAP | **Missing** |
| F03 | Partial-only, not a 2nd trade | **Missing** |
| F04 | INOUT leftover + money on A | Shape only; money/tickets **missing** |
| F05 | Reverse then close new side | **Missing** |
| F06 | First-3 with EURUSD / GOLD / 4th | **Missing** |
| F07 | Long avg-down VWAP | Boolean only |
| F08 | Short avg-down | **Missing** |
| F09 | Hedge `OUT_BY` two books | **Missing** |
| F10 | Same-order partial fill = scale-in | **Missing** |
| F11 | Netting reuse of `position_id` | **Missing** |
| F12 | First-3 contains scale-in + reverse | **Missing** |
| F13 | Open-only count 0 | **Missing** |
| F14 | Balance/commission noise mixed | Balance-only empty; mixed **missing** |
| F15 | Out-of-order ingest sort | **Missing** |
| F16 | Duplicate ticket idempotent | **Missing** |
| F17 | Canceled dirties / excludes | **Missing** |
| F18 | OUT overclose dirty | **Missing** (SUT clamps) |
| F19 | Opposite IN dirty | **Missing** (SUT silent-reverses) |
| F20 | INOUT no new volume | **Missing** |
| F21 | `volume_closed_h` mismatch | **Missing** (field absent) |

**0 / 21 fixtures are bit-for-bit.** F01 / F04 / F07 are loose cousins at best.

---

## 8. Implementation gaps the suite cannot see

These are **not** a request to edit product source. They explain why “tests passed” is not a reconstruction PASS.

1. **Decimal lots + `FlatEpsilon`** vs A21 integer `volume_h` / `remaining_h == 0`. Completion is float-shaped.
2. **No `lifecycle_seq`.** Netting reuse (F11) cannot key `(broker, login, position, seq)`. Result `Id` is `Broker:Login:Position:OpenedAtMs`.
3. **No dirty / failure codes.** Opposite IN, OUT-flat, OUT-overclose, INOUT-flat, canceled: SUT skips, clamps, or invents a reverse.
4. **INOUT money double-applied** on the new side (`CloseOut` + `Start`). Tests do not read PnL.
5. **No `volume_closed_h`** on `NormalizedDeal`. Infer-always-from-remaining is untested against F21.
6. **`Reconstruct` keeps non-XAU** books. Spec drops them before the book.
7. **Fees hardcoded 0** in `ToResult`. Commission/swap never non-zero in the helper.
8. **No first-3 cursor** (keys, `early_score_at`, completing ticket, events). Eligible is `count >= 3`.
9. **No events** (`XAU_LIFECYCLE_*`, `EARLY_SCORE_ELIGIBLE`).
10. **Helper cannot express** `OutBy`, canceled actions in a mixed book, other symbols, other logins, non-zero commission, SL/TP, out-of-order times, duplicate tickets.

---

## 9. What *would* be sufficient (do not implement here)

A27 §4.1 + A21 F01–F21, as **separate** facts (or `[Theory]` rows), each asserting the full trade row (key, dir, seq, volumes, VWAPs to 12 dp, money, flags, tickets) **and** `First3State`.

Minimum split:

| Class (A27 name) | First facts that must exist |
|---|---|
| `ScaleInReconstructionTests` | F02 VWAP / not avg-down; F07 long avg-down numbers; F08 short; F10 same-order; hedge three `position_id`s ≠ one scale-in |
| `PartialCloseReconstructionTests` | F03 one trade + `exit_vwap`; mid-book `completed_count=0`; simple full-close `WasPartialClose=false`; F09 `OUT_BY` |
| `PositionReversalReconstructionTests` | F04 money+tickets; F05 two completed; F20/F21 failures; short→long; **assert new-side PnL == 0** |
| `FirstThreeCompletedXauTradesTests` | N=0/1/2 false; F06 (EURUSD skip, GOLD maps, partial mid, 4th not in keys); F12; F13; reverse leftover excluded; login isolation |
| `TradeReconstructionTests` / `FullCloseReconstructionTests` | F01 with commission/swap/net; F11 reuse; F15 sort; F16 dup; F14 mixed noise |

Until those exist, §60 items “partial close / scale-in / position reversal” and A100 “A27 reconstruction fixtures pass” stay **unchecked**.

---

## 10. Disposition

| Metric | Value |
|---|---|
| Reconstruction `[Fact]`s on disk | **5** (one class) |
| Facts that even name the four areas | **3** (one of them fused) |
| Isolated scale-in / partial / reverse / first-3 classes | **0 / 4** |
| A21 fixtures encoded bit-for-bit | **0 / 21** |
| Measured `TradeReconstructionTests` | **5 passed / 5** (smoke) |
| Sufficient for scale-in? | **No** |
| Sufficient for partial? | **No** |
| Sufficient for reverse? | **No** |
| Sufficient for first-3? | **No** |
| Product source changed | **No** |

**FAIL / INSUFFICIENT.** Keep the five facts as a compile smoke if useful; do not treat them as the reconstruction gate. Next increment is A21 F01–F21 as isolated xUnit rows, especially F03 (partial ≠ trade), F04 money split, F06 first-3 exclusions, and F02 scale-in VWAP without avg-down.
