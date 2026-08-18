# P500_S046 — `WasAveragedDown` is rarely set; averaging filter is weak

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S046_averaging_flag.md` |
| Slot | **P500_S046** |
| Date | 2026-08-18 |
| Assigned | Find `WasAveragedDown` in reconstruction. If rarely set, averaging filter is weak. Login **303274** multi-ticket same-second may be averaging. Do **not** edit product. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** |
| Method | Full read of `TradeReconstructor.OpenTrade.ScaleIn`, `ReconstructedTradeResult`, `ReconstructedTrade`, `BaselineScorer`, `TraderStateMachine`, `DealIngestionService.RebuildTraderAsync`, `RiskEngine` (zero averaging hits), A21 §1.6 / §4.1, A22 §4.1 / §8, `TradeReconstructionTests`, catalog pin of 303274. No Manager re-attach. No deal-tape replay this slot. |
| Binding law | A21 `was_averaged_down`; A21 §1.6 hedge basket ≠ scale-in; A22 `averaging_down_risk` + `FLAG_AVERAGING_DOWN` floor; Architecture §60 averaging-down detection |

**Honesty:** The 303274 tape numbers (102 XAU trades, source PnL +1,228, first-3 **−0.35 / −55.30 / +25.90**, later **SHADOW / 93.50**, 0.05-lot same-second multi-ticket grid, scorer did **not** flag averaging) are **assigned live facts** from `P500_PROFIT_SYNTHESIS.md` / `P500_S020`. This workspace has **no** persisted reconstructed-trade dump for login `303274`. Catalog confirms the login. Code path is measured here; the deal tape is **not** re-pulled.

**One-line:** `WasAveragedDown` latches only on same-`PositionId` scale-in at a strictly worse pre-fill VWAP. Hedging-mode same-second multi-ticket grids never call `ScaleIn`, so the averaging filter stays **false** on the books that actually average.

---

## 0. Verdict

**CONFIRMED WEAK.** The averaging “filter” is a one-bit latch on a netting book. It is **not** a cross-ticket grid detector. If it is rarely set on live prop books, that is because **most XAU recovery is multi-ticket**, not because traders do not average.

| Claim | Result |
|---|---|
| Where is `WasAveragedDown` set? | **Only** `OpenTrade.ScaleIn` in `TradeReconstructor.cs` (line 255). Never on `Start`, `CloseOut`, or `ApplyReverse`. |
| When does `ScaleIn` run? | Same `PositionId`, same-side `ENTRY_IN` while a book is already open. |
| Hedging / new ticket / new `PositionId`? | **Never** `ScaleIn`. A21 §1.6: three BUY tickets = **three** trades. Flag stays **false** on every row. |
| Same-second multi-ticket (303274 class)? | Distinct `PositionId`s processed in independent groups. **Invisible** as averaging. |
| How often is the flag expected on Achiever demo? | **Rare** on hedge-mode challenge books (default retail/prop MT5). Common only if the server is **netting** and the EA adds to one ticket. |
| Downstream filter strength | `trades.Any(t => t.WasAveragedDown)` → **+20 risk / −15 behavior**. Cannot alone hit `RISK_BLOCKED` (needs risk ≥ 80). Cannot alone block `SHADOW` (needs risk < 40). |
| A22 implemented? | **No.** No `avg_down_n / N` lerp. No `FLAG_AVERAGING_DOWN` floor 75. No severe-at-N≤3. |
| `RiskEngine` | **Zero** references. Does not read the flag. |
| Product edited this slot | **No.** |

Empty PASS refused. The SUT was read in full (`TradeReconstructor.cs` 347 lines). The 303274 hole is the same hole A21 §1.6 already documented as reconstruction identity — and the same hole P500_CODE_23 / P500_PROFIT_SYNTHESIS already named as a **tail-screen** failure.

---

## 1. The only write of the flag

`D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`

Field lives on the private netting book:

```207:209:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public bool WasScaledIn { get; set; }
        public bool WasPartialClose { get; set; }
        public bool WasAveragedDown { get; set; }
```

It is written in **one** place:

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

Copied onto the result, never cleared:

```339:341:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
                WasScaledIn = WasScaledIn,
                WasPartialClose = WasPartialClose,
                WasAveragedDown = WasAveragedDown,
```

Polarity matches A21 §4.1 (compare **before** VWAP update). That is **detection correctness for netting scale-in**, not filter coverage.

What the latch does **not** encode:

| Missing | Effect |
|---|---|
| Add count | 1 tick below VWAP ≡ 12 recovery adds |
| Distance | 0.01 below VWAP ≡ $80/oz crash |
| Lot ratio | `MaxVolumeLots / InitialVolumeLots` is stored, **never compared** |
| Equality | Add **at** VWAP (same-price grid) is not averaging (`<` / `>`, not `<=` / `>=`) |
| Unrealized P&L | “Worse price vs VWAP” ≠ “add while in loss” if VWAP is already averaged |
| Open books | Incomplete pyramids are reconstructed then **dropped** by the scorer (`Completed && IsXauUsd`) |
| Cross-`PositionId` | Hedging grid never enters this method |

`Start` always leaves `WasAveragedDown = false` (bool default). Opposite-side `ENTRY_IN` is `ApplyReverse`, not `ScaleIn`. Reverse cannot set the flag.

---

## 2. Why the flag is rarely set: grouping key

```45:52:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var results = new List<ReconstructedTradeResult>();
        foreach (var group in trading.GroupBy(d => d.PositionId))
        {
            var rows = ReconstructPosition(brokerId, login, group.Key, group.ToList());
            if (dirtyPositions.Contains(group.Key))
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
            results.AddRange(rows);
        }
```

`ScaleIn` is reached only here:

```123:133:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        if (open is null)
            return OpenTrade.Start(brokerId, login, positionId, deal, lots, direction, _symbols);

        if (open.Direction == direction)
        {
            open.ScaleIn(deal, lots);
            return open;
        }
```

A21 §1.6 (`D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md`):

> Hedging mode: each `position_id` is its own lifecycle. Three separate BUY tickets are **three** trades, not one scale-in.

That is **correct reconstruction identity** and **wrong as a tail screen**. Prop / retail MT5 gold is almost always **hedging**. An EA that opens 0.05 + 0.05 + 0.05 in the same second (or ladders 0.05 every $1) produces N distinct `PositionId`s. Each book:

- `WasScaledIn = false`
- `WasAveragedDown = false`
- `DealCount = 2` (IN + OUT)
- `MaxVolumeLots = InitialVolumeLots = 0.05`

The scorer then sees a **flat-lot, no-averaging** book. Sequential martingale only fires if a **later completed** ticket is `> 1.25×` the prior ticket’s `MaxVolumeLots` **and** the prior ticket lost. Same-size 0.05 grid never trips martingale. Same-second opens are not “after a loss” in time order if they share a timestamp and all open before any close.

`docs/trade-reconstruction.md` repeats the same grouping (`position_ticket`) and never mentions averaging-down at all.

---

## 3. Live pin: login 303274 is the missing-flag case

Catalog (`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`):

| Field | Value |
|---|---|
| login | **303274** |
| group | `demo\yo-2step` |
| leverage | 100 |
| balance / equity | 16228.24 / 16228.24 |

Assigned tape (`P500_PROFIT_SYNTHESIS` §2.4, `P500_S020`):

| Field | Value |
|---|---|
| XAU completed | **102** |
| Dashboard / source PnL | **+1,228** |
| Pattern | **0.05 lot same-second multi-ticket grid** |
| First 3 XAU | **−0.35, −55.30, +25.90** (net **−29.75**) |
| Later state / quality | **SHADOW / 93.50** |
| Averaging flag (assigned) | **Not set.** Scorer did not treat the grid as averaging. |

Why the code **cannot** set the flag on that pattern:

1. Same-second multi-ticket ⇒ distinct MT5 hedge `PositionId`s (ticket ≈ position).
2. `GroupBy(PositionId)` never merges them.
3. Each IN is `open is null` → `Start`, not `ScaleIn`.
4. `WasAveragedDown` stays default **false** on all 102 rows.
5. `BaselineScorer.ComputeFeatures` → `AveragingDown = trades.Any(...)` → **false**.
6. Risk does **not** get +20. Quality can still clear **70** on later NET>0 / high PF (first-3 is **not** the score window — see S020). `SHADOW` requires `risk < 40`; with averaging false and no martingale/escalation, only the standing no-SL +10 (if `InitialSl` is null — S030) remains. **93.50 is reachable.**

The first-3 itself already **looks like averaging / grid recovery** (−0.35 scratch, −55.30 the grid that ran, +25.90 the bounce). Reconstruction names that as three unrelated 0.05 tickets.

This slot does **not** re-claim a measured `WasAveragedDown` rate across 8,463 accounts. There is no reconstructed-trade dump in-tree to count `true` bits. The structural claim is stronger than a missing dump: **any hedge-mode same-second grid will measure 0% averaging**, regardless of how many tickets share a second.

---

## 4. Downstream is not a filter either

### 4.1 Scorer — one bit, +20, no floor

```117:118:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
            Martingale = martingale,
            AveragingDown = trades.Any(t => t.WasAveragedDown),
```

```134:147:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var risk = 0m;
        if (features.Martingale) risk += 35;
        if (features.AveragingDown) risk += 20;
        if (features.LotEscalation) risk += 15;
        ...
        if (features.Martingale) behavior -= 30;
        if (features.AveragingDown) behavior -= 15;
```

```194:201:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

| If the flag **is** set (netting add-lower) | Consequence |
|---|---|
| Risk | **+20** only |
| `RISK_BLOCKED` | **No** (20 < 80; averaging is not in the martingale conjunct) |
| `SHADOW` | **Still allowed** (20 < 40) if quality ≥ 70 |
| A22 `FLAG_AVERAGING_DOWN` | Spec: severe at `N≤3` with one averaged trade; floor **75**. Product: **absent** (`grep FLAG_AVERAGING` in `src/Domain` = 0) |
| A22 `averaging_down_risk` | Spec: lerp `avg_down_n / N`. Product: boolean |

| If the flag **is not** set (303274 class) | Consequence |
|---|---|
| Risk from averaging | **0** |
| Grid / same-second hedge | Invisible |
| Winning recovered grid | Can sit in **SHADOW** |

`RebuildTraderAsync` copies the scorer bit onto `TraderScore.AveragingDown` and does **not** drop averaged (or gridded) lifecycles from shadow persist:

```126:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        ...
            AveragingDown = score.Features.AveragingDown,
```

`CompletedXauUsdTrades` (which also requires `EligibleForFirstThree`) is **not** used in production scoring.

### 4.2 Risk engine — does not participate

`D:\Prop\src\Domain\Risk\RiskEngine.cs`: **no** `WasAveragedDown` / `AveragingDown` references. Copy / increase / block decisions do not see the latch even when it is true.

### 4.3 Persistence / dashboard

| File | Role |
|---|---|
| `ReconstructedTradeResult.WasAveragedDown` | required bool on the domain result |
| `ReconstructedTrade.WasAveragedDown` | EF column, copied in `EfTradingStore` line 207 |
| `TraderScore.AveragingDown` | book-level OR, dashboard bool |
| `EfDashboardQueries` | `s?.AveragingDown ?? false` |

A false negative at reconstruction becomes a false negative on the trader row. There is no second detector.

---

## 5. Tests lock the rare path, not the common path

`D:\Prop\tests\Unit\TradeReconstructionTests.cs` — one fused fact:

```33:48:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    public void Scale_in_and_partial_close()
    {
        // same PositionId=20, 0.10 @ 2300 then 0.10 @ 2290
        trade.WasAveragedDown.Should().BeTrue();
    }
```

Helper `Deal(...)` **forces the same `position` on every deal of a case**. That is netting. It cannot fail a hedge-grid regression.

| Required fact (A21 / A89 / A27) | On disk? |
|---|---|
| Long add-lower → true | Yes, fused with partial-close |
| Long add-higher → false | **No** |
| Short add-higher → true | **No** |
| Add at VWAP → false | **No** |
| Two same-second IN, **different** `PositionId`, worse prices → should flag as grid/averaging | **No** (and product would leave both false) |
| `FeatureSnapshot.AveragingDown` from reconstructed flag | Scorer tests hard-code `WasAveragedDown = false` |
| `AveragingDownFlagReconstructionTests` / `AveragingDownDetectorTests` | **Named in A89, not present** |

`BaselineScorerTests` never construct a true averaging book. The +20 path is unasserted.

---

## 6. Worked cases

Assume completed XAU ≥ 3, NET>0, PF≥1.8, no martingale, no lot-escalation, SL missing (+10 risk, −10 behavior) — the S030 standing penalty.

| Book | `WasAveragedDown` | Averaging risk | Typical state |
|---|---|---|---|
| Netting: 0.10 @ 2300 then 0.10 @ 2290, then close green | **true** | +20 → risk **30** | **SHADOW** still allowed (30 < 40) |
| Hedging: 0.05 / 0.05 / 0.05 same second, prices 2300 / 2290 / 2280 | **false** on all three | **0** → risk **10** | **SHADOW** (303274 class) |
| Hedging: 0.05 lose, next 0.05 lose, next 0.05 win | **false**; martingale false (1.00× < 1.25×) | **0** | **SHADOW** if later book is green |
| Netting pyramid 0.10→0.20→0.40→0.80 one ticket | **true**, N=1 completed | +20; martingale **false** (one row) | **SHADOW** if close is green |
| Add **at** same VWAP on one ticket | **false** (strict inequality) | 0 | clean |

A22 would mark the first two as **severe** averaging (`N≤3` + one avg-down, or `avg_down_n/N` high). Product does not.

---

## 7. What “rarely set” means for capital

The averaging filter is weak in **two stacked ways**:

1. **Coverage (this slot).** The bit is structurally rare on hedge-mode XAU — the mode the 8k Achiever demo book actually uses. 303274 is the exhibit: same-second 0.05 grid, 102 trades, SHADOW 93.50, flag off.
2. **Severity (P500_CODE_23, restated).** Even when the bit **is** set, +20 cannot block SHADOW and cannot `RISK_BLOCKED`. A22 floors are not implemented. `RiskEngine` ignores it. Open pyramids are dropped.

If this flag is later used as “we already screen averaging,” capital will concentrate in the **multi-ticket recovery cluster** — the cluster that dies on the first one-way gold trend. Destination is unhurt today only because `REAL_COPY` is false, `CanPromoteToLive == false`, and `destinationRealPnl` is 0.

---

## 8. What a real averaging screen would need (not built)

Do **not** implement in this slot. Recorded so the hole is named:

1. Cross-`PositionId` same-symbol same-side opens within a short window (same second / same minute) ⇒ grid / averaging, even if each ticket is flat-sized.
2. Cross-ticket add after an **open** same-side XAU book is already in unrealized loss.
3. Count + lot-sum + price-vs-first-fill, not one bool.
4. A22 `avg_down_n / N` + `FLAG_AVERAGING_DOWN` floor (75) so one grid book cannot enter `SHADOW`.
5. `RiskEngine`: block `IncreaseExposure` when source lifecycle is averaged **or** a same-second grid is live.
6. Tests: hedge two-ticket same-second fixture (303274 shape) must fail until (1) exists.

A21 §1.6 can stay as reconstruction identity. The **score / risk** layer must not treat “not the same `PositionId`” as “not averaging.”

---

## 9. Files read (absolute)

| Path | Use |
|---|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | Only write of `WasAveragedDown`; `GroupBy(PositionId)`; `ScaleIn` polarity |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | required bool line 36 |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | `PositionId` is the group key |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | persist column line 34 |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | book-level `AveragingDown` |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `Any()` +20 / −15; `TraderStateMachine` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | no averaging |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | production score window; copies flag |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | persist copy line 207 |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | one fused netting fact |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | `WasAveragedDown = false` constant |
| `D:\Prop\docs\trade-reconstruction.md` | groups by `position_ticket`; no averaging |
| `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` | §1.6 hedge ≠ scale-in; §4.1 predicate |
| `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` | `averaging_down_risk`; `FLAG_AVERAGING_DOWN` |
| `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` | 303274 same-second grid, flag not set |
| `D:\Prop\reports\swarm\20260818\P500_S020_first3_not_skill.md` | 303274 first-3 / SHADOW 93.50 |
| `D:\Prop\reports\swarm\20260818\P500_CODE_23.md` | prior tail-filter FAIL (same latch) |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | login 303274 catalog |

---

## 10. Bottom line

```text
WasAveragedDown := same PositionId AND same-side IN AND price strictly worse than pre-fill VWAP
303274            := same-second multi-ticket 0.05 grid  :=  different PositionIds  :=  flag never set
Averaging filter  := Any(flag) +20                       :=  cannot block SHADOW
Therefore         := WEAK. Rarely set on the books that average. Not a copy screen.
```

Product source was not edited.
