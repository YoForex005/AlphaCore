# P500_CODE_3 — Martingale / averaging filters vs left tail

| Field | Value |
|---|---|
| Slot | 3 |
| File | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| Angle | Are martingale and averaging filters strong enough to cut the left tail? |
| Date | 2026-08-18 |
| Product source edited | **No.** `read_file` / `grep` only. |
| SUT read | Full file, 347 lines (`TradeReconstructor` + nested `OpenTrade`). |
| Adjacent read | `ReconstructedTradeResult.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `DealIngestionService.cs` (`ReconstructionScoringService`), `EfTradingStore.PersistDemoShadowAsync`, `TradeReconstructionTests.cs`, `BaselineScorerTests.cs`, `docs/scoring.md`, A21/A22, `CREDENTIALS_AND_COPY_STATUS.md` |
| Live snapshot (assigned) | 8463 accounts; Achiever scoring; Starwave deals-done scored 0; SHADOW all demo; `destinationRealPnl` 0; FIX LoggedOn; `REAL_COPY` false |
| Live census on disk | `reports/CREDENTIALS_AND_COPY_STATUS.md`: Achiever 6512 + Starwave 1948 = **8460** (this slot did not re-probe) |
| Empty PASS? | **No.** File was read. Verdict is **FAIL**. |

---

## Verdict

**FAIL — not strong enough to cut the left tail.**

`TradeReconstructor` is a **netting book with two boolean latches**. It is **not** a martingale filter and **not** an averaging-down rejector. Same-side adds are always booked. `WasAveragedDown` is a one-bit flag with no size, count, or distance. There is **no** `Martingale` field on the reconstructed trade. Downstream scoring and risk do not close the gap: averaging never hard-blocks; intra-position doubling (the XAU blow-up) is invisible as martingale; a recovered martingale stays copy-eligible. Capital is unhurt today only because `REAL_COPY` is false, SHADOW is demo-only, and `destinationRealPnl` is 0 — not because these filters work.

---

## What the file actually does (no filter)

`Reconstruct` groups trading deals by `PositionId`, nets IN / OUT / INOUT, and emits `ReconstructedTradeResult` rows. The only eligibility cut is canceled-deal dirtiness on first-three **count**, not on averaging or size-up:

```34:51:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var dirtyPositions = scoped
            .Where(d => d.Action is DealAction.BuyCanceled or DealAction.SellCanceled)
            .Select(d => d.PositionId)
            .ToHashSet();
        // ...
            if (dirtyPositions.Contains(group.Key))
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
```

`CompletedXauUsdTrades` requires `Completed && IsXauUsd && EligibleForFirstThree`. It does **not** drop `WasAveragedDown` or large `MaxVolumeLots`. Production scoring (`ReconstructionScoringService.RebuildTraderAsync`) then **ignores** `EligibleForFirstThree` and scores every `Completed && IsXauUsd` row.

There is no `if (WasAveragedDown) skip`, no lot-ratio reject, no peak-lot cap, no “do not emit this lifecycle” path.

---

## Averaging: detect-only, zero severity

```248:264:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public void ScaleIn(NormalizedDeal deal, decimal lots)
        {
            // Averaging down: add to a long after price fell, or to a short after price rose.
            var worse = Direction == TradeDirection.Long
                ? deal.Price < EntryVwap
                : deal.Price > EntryVwap;
            if (worse)
                WasAveragedDown = true;

            WasScaledIn = true;
            RemainingLots += lots;
            if (RemainingLots > MaxVolumeLots)
                MaxVolumeLots = RemainingLots;
            _entryNotional += deal.Price * lots;
            _entryLots += lots;
            ApplyCommon(deal);
        }
```

| Left-tail property | Code | Consequence |
|---|---|---|
| Reject add-in-loss | **None** | 0.01 or 10.00 lots both book |
| Distance / MAE of add | **None** | $0.01 worse == $50 worse |
| Add count | **None** | 1 add == 20-grid ladder |
| Lot multiplier vs initial | **None** | 2× / 8× / 32× same bit |
| Equal-price add | `worse` is strict `<` / `>` | Grid at same print not flagged |
| Cross-`PositionId` grid / hedge | Groups never merge | Classic multi-ticket average is invisible |
| INOUT reverse-and-reload | `ApplyReverse` + `Start` | New side starts clean; no average flag |
| Unbounded peak | `MaxVolumeLots` only records remaining peak | No cap, no reject |

Polarity is now correct (long add **below** VWAP; short add **above**). That makes the **label** honest. It does not make a **filter**. Unit `Scale_in_and_partial_close` asserts the bit is true on `0.10 @ 2300` then `0.10 @ 2290`; it never asserts “do not copy” or “do not score as SHADOW.”

---

## Martingale: not in this file

Grep of `src/Domain/Reconstruction`: `Martingale` does not exist. Intra-position doubling is the usual XAU left tail (one ticket, adds while underwater). This class records that as **one** completed trade with `WasAveragedDown=true` and a larger `MaxVolumeLots`. Adjacent-trade martingale is computed later, and only on **completed** lifecycles:

```86:94:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var martingale = false;
        var lotEscalation = false;
        for (var i = 1; i < trades.Count; i++)
        {
            if (trades[i - 1].NetRealizedPnl < 0 && trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.25m)
                martingale = true;
            if (trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.5m)
                lotEscalation = true;
        }
```

Holes that let the left tail through:

1. **Intra-position martingale ≠ martingale.** One averaged ticket that 8×s then closes as a small win is `Martingale=false`. Next trade at the same peak size is also not a size-up-after-loss.
2. **Threshold 1.25 vs A22 `MARTINGALE_VOLUME_RATIO = 1.80`.** Scorer is looser than spec on ratio but still **boolean**, not event count, and still **not** a hard block by itself.
3. **Winner recovery.** `RISK_BLOCKED` only if `risk >= 80` **or** `(Martingale && MaxDrawdown > 0 && NetPnl < 0)`:

```194:201:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        // ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

   A martingale that prints green after the last double (`NetPnl > 0`) stays out of `RISK_BLOCKED`. That **is** the left tail: many small recovered grids, one unrecovered blow-up.

4. **Averaging never reaches 80 alone.** `AveragingDown` adds **+20** risk / **−15** behavior. Martingale + averaging + lot-escalation = **35+20+15 = 70 < 80**. Averaging-only 3-winner books can still land `SHADOW` (`risk=20 < 40`, quality boosted by `NetPnl > 0`).

5. **`ReconstructionScoringService` does not apply first-three dirtiness.** Averaged / canceled-tainted XAU still enter `Score()`.

6. **Risk engine has no averaging block.** `BlockMartingale` rejects **increasing** actions only when a caller sets `MartingaleFlag`. There is no `AveragingDown` field on `RiskEvaluationRequest`. Shadow persist copies **every** completed XAU at `MaxVolumeLots` when state is `SHADOW`, including averaged tickets (`EfTradingStore` ~289–309, `Status = "SHADOW_ONLY"`).

7. **Open underwater books are invisible.** Features are completed-trade equity. The growing averaged loser does not raise `MaxDrawdown` until it is closed. Copy-on-open of the add would happen **before** the scorer sees the tail.

---

## Evidence quotes (SUT + chain)

**No reject on scale-in — always mutate the book:**

> `WasScaledIn = true;` then `RemainingLots += lots;` (`TradeReconstructor.cs` `ScaleIn`)

**Flag only, latch only:**

> `if (worse) WasAveragedDown = true;`

**Martingale absent from reconstructor; present only as scorer adjacent-pair boolean at 1.25× after a completed loss.**

**Averaging is `Any(WasAveragedDown)` — one 0.01 add poisons the bit the same as a 32× ladder, and neither rejects copy.**

**Hard block requires net-losing martingale or risk ≥ 80. Averaging cannot get there by itself.**

**`CanPromoteToLive` is hard `false`.** That is a live-promotion pin, not a left-tail filter on the source book.

**Assigned live:** Achiever scoring on ~8463 accounts; Starwave deals-done scored **0** (that book does not even feed these flags); SHADOW demo-only; `destinationRealPnl` **0**; FIX LoggedOn; `REAL_COPY` **false**. Disk census 8460 (6512+1948). Filters were **not** what produced the zero destination PnL.

---

## Profit implication

These flags cannot select a clean XAU book. Averaging winners and recovered grids still reach `SHADOW` and would be the first names a later `REAL_COPY` flip would harvest. Win-rate looks high precisely because losers are averaged until they scratch. Destination would copy **peak** `MaxVolumeLots` (`PersistDemoShadowAsync` uses that size), so the rare unrecovered grid is also the largest copied size. Starwave scored 0 means any real edge on that 1948-login book is unused; Achiever scores that pass averaging/martingale soft penalties are not a proven edge (`destinationRealPnl` 0). Turning copy on without a hard intra-position size-up reject would monetize the **right** tail of grid traders and eat the **left**.

Over-flagging is the other profit leak: a single tiny add-in-loss sets `AveragingDown` for the whole account (`trades.Any`). A disciplined scale-in of 10% after a 0.1-point dip is scored the same as a martingale grid. That can park a usable trader in extra risk points or keep a true grid in `WATCH`/`SHADOW` instead of `RISK_BLOCKED`. Binary flags destroy ranking; they do not cut tail.

---

## Lower-loss implication

**Today’s loss cut is the copy pin, not these filters.** `REAL_COPY false` + SHADOW demo + `CanPromoteToLive => false` + `destinationRealPnl` 0. If those pins move, this file will **not** stop:

- Unbounded same-ticket adds (no lot cap in `ScaleIn`).
- Multi-ticket grids (not averaged, often not martingale until a completed loser is followed by a larger ticket).
- Recovered martingales (`NetPnl > 0` skips `RISK_BLOCKED`).
- The open averaged loser (not in the completed feature set when the next add would be copied).
- Averaging with SL unused: `SlUseRate` only adds +10 risk if `< 0.3`; still far from 80.

XAU left tail is inventory explosion on one idea. This reconstructor **records** that inventory (`MaxVolumeLots`) and **labels** worse-price adds, then hands the lifecycle to a scorer that treats the label as a 20-point nudge. That is a dashboard badge, not a tail cut.

---

## What would be strong enough (not implemented)

Not asked to implement. Gap list only:

1. Reconstructor: emit `AddCount`, `WorstAddDistance`, `PeakVsInitialLotRatio`; reject or dirty when `PeakVsInitial >= 1.8` or add-count ≥ N on a worse VWAP.
2. Treat intra-position `MaxVolumeLots / InitialVolumeLots` as martingale **on that trade**, not only adjacent completed tickets.
3. Hard-block averaging at risk/copy (symmetric to `BlockMartingale`); do not wait for `NetPnl < 0`.
4. Score only `EligibleForFirstThree` XAU; do not shadow-copy `WasAveragedDown` tickets at `MaxVolumeLots`.
5. Align scorer ratio to A22 1.80 and use event counts, not a single bool.
6. Until that exists, keep `REAL_COPY` false. Do not treat Achiever score volume (thousands of accounts) as evidence the tail is cut.

---

## Honesty box

| Claim | Status |
|---|---|
| File read in full | **Yes** |
| Empty PASS | **No** |
| Averaging polarity correct | **Yes** (long cheaper / short richer) |
| Averaging is a filter | **No** |
| Martingale detected here | **No** |
| Downstream hard-blocks averaging | **No** |
| Downstream hard-blocks recovered martingale | **No** |
| Intra-position double flagged as martingale | **No** |
| Live destination capital at risk now | **No** (`REAL_COPY` false, dest PnL 0) |
| Filters are why dest PnL is 0 | **No** |
| Starwave left tail observed in scores | **No** (deals-done scored 0) |
