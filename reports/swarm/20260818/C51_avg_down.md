# C51 — TradeReconstructor `ScaleIn` after long/short averaging-down change

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C51_avg_down.md` |
| Agent | C51 (averaging-down polarity confirm) |
| Date | 2026-08-18 |
| Assigned | Read `TradeReconstructor.ScaleIn` after the long/short averaging-down change. Confirm long add-lower is averaging down. Write this report. Do not modify product source. |
| Product source modified | **No.** Read-only. Working-tree polarity change was already on disk; this agent did not edit it. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (initial `6c41447` is the only commit that contains this file) |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| SUT SHA-256 (working tree) | `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD` |
| Working-tree status | **Uncommitted** polarity flip on lines 237–240 (`git blame` = `Not Committed Yet` 2026-08-18 13:28:37 +0530) |
| Test run | `dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --filter FullyQualifiedName~TradeReconstructionTests.Scale_in_and_partial_close` |
| Test result | **Passed! Failed 0 / Passed 1 / Skipped 0** (Duration < 1 ms) |
| Law | Architecture v2 §14 `was_averaged_down`; A21 §4.1 + §7.4 + F02/F07/F08/F12; `docs/trade-reconstruction.md` |

---

## 0. Verdict

**CONFIRMED. After the long/short change, a long add-lower is averaging down.**

`OpenTrade.ScaleIn` now treats a same-side long scale-in whose fill price is **strictly below** the entry VWAP **before that fill** as `worse`. That sets `WasAveragedDown = true` and it stays latched on the lifecycle.

| Check | Required (A21) | Working tree | Result |
|---|---|---|---|
| LONG add **below** prior VWAP | `was_averaged_down = true` | `deal.Price < EntryVwap` → `worse` | **PASS** |
| LONG add **above** prior VWAP | `was_averaged_down` unchanged | `deal.Price < EntryVwap` is false | **PASS (code)** — no isolated fact |
| SHORT add **above** prior VWAP | `was_averaged_down = true` | `deal.Price > EntryVwap` → `worse` | **PASS (code)** — no isolated fact |
| SHORT add **below** prior VWAP | not averaging down | `deal.Price > EntryVwap` is false | **PASS (code)** — no isolated fact |
| Compare **before** updating VWAP | A21 `vwap_before` | comparison is before `_entryNotional` / `_entryLots` mutate | **PASS** |
| Existing long add-lower fact | `Scale_in_and_partial_close` expects `WasAveragedDown == true` | measured **Passed** | **PASS** |

Do **not** claim A21 F07/F08 fixtures are ported. Do **not** claim averaging-down detection is §60 COVERED. The polarity of the SUT is now correct; the test surface is still one fused happy-path fact.

**Stale reports (keep on disk; do not delete):**

- B08 §3.2 / A89 G1 quote the **inverted** HEAD comparison (`LONG price > VWAP`). That is no longer the working tree.
- B11 honesty box and C17 §9 already said the flip was on disk. This file re-reads the SUT + measures the long add-lower fact after that change.

---

## 1. The long/short change (HEAD vs working tree)

Committed `ScaleIn` (HEAD `6c41447`, still what `git show HEAD` contains):

```csharp
var worse = Direction == TradeDirection.Long
    ? deal.Price > EntryVwap
    : deal.Price < EntryVwap;
```

That inverted A21. Classic long average-down `0.10 @ 2300` then `0.10 @ 2290` evaluated `2290 > 2300` → false → `WasAveragedDown` stayed false. That is the B08 red fact.

Working-tree `git diff` for this file is **only** this polarity plus a comment (index `ad776cf` → `d7c6477`):

```diff
         public void ScaleIn(NormalizedDeal deal, decimal lots)
         {
+            // Averaging down: add to a long after price fell, or to a short after price rose.
             var worse = Direction == TradeDirection.Long
-                ? deal.Price > EntryVwap
-                : deal.Price < EntryVwap;
+                ? deal.Price < EntryVwap
+                : deal.Price > EntryVwap;
             if (worse)
                 WasAveragedDown = true;
```

Comment on disk (line 237) matches A21 language: add to a long after price fell, or to a short after price rose.

---

## 2. What was read (product source not edited)

| File | Role |
|---|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | SUT. `ApplyIn` same-side → `ScaleIn`. `OpenTrade.ScaleIn` lines 235–251. `EntryVwap` line 292. Flag copied out at line 328. |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | `WasAveragedDown` required bool (line 36). |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | Persist column `WasAveragedDown` (line 34). |
| `D:\Prop\src\Domain\Enums\TradeDirection.cs` | `Long = 0`, `Short = 1`. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `AveragingDown = trades.Any(t => t.WasAveragedDown)` (line 118). Scorer does **not** re-derive VWAP. |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Copies `WasAveragedDown` through on persist (line 207). |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | `Scale_in_and_partial_close` is the only on-disk averaging-down assertion. |
| `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` | Binding: §4.1, §7.4, F02, F07, F08, F12. |
| `D:\Prop\docs\trade-reconstruction.md` | Scale-in is `Entry.In`; no polarity text. |

`ApplyIn` (lines 110–119) is the only production call site:

```110:119:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    private OpenTrade ApplyIn(OpenTrade? open, NormalizedDeal deal, decimal lots, string brokerId, long login, long positionId)
    {
        var direction = deal.Action == DealAction.Buy ? TradeDirection.Long : TradeDirection.Short;
        if (open is null)
            return OpenTrade.Start(brokerId, login, positionId, deal, lots, direction, _symbols);

        if (open.Direction == direction)
        {
            open.ScaleIn(deal, lots);
            return open;
        }
```

`Buy` + existing long → `ScaleIn`. Opposite `ENTRY_IN` is the reverse hack, **not** a scale-in, so it cannot set `WasAveragedDown`.

Current `ScaleIn` (working tree):

```235:251:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
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

`EntryVwap` is `_entryNotional / _entryLots` (0 if `_entryLots <= 0`). The comparison runs **before** the new fill is added to notional/lots. That is A21 `vwap_before`, not the post-fill VWAP.

Latch: `if (worse) WasAveragedDown = true` never clears. One worse add is enough. Later better adds do not wash it out. A21 `book.seen_avg_down` is the same latch.

Equality: operators are **strict**. Add at exactly the prior VWAP is not averaging down. A21 says “worse than entry VWAP *before* that fill,” so equal is correctly not worse.

---

## 3. Long add-lower walk (the assigned question)

Definition used here (and in A21 F07): **long add-lower** = same-side `ENTRY_IN` buy at `price < entry VWAP before that fill`.

### 3.1 On-disk unit fixture (measured)

`TradeReconstructionTests.Scale_in_and_partial_close`:

| Deal | Action / Entry | Native vol | Lots (`/ 10_000`) | Price | Book before fill |
|---:|---|---:|---:|---:|---|
| 1 | Buy / In | 1000 | 0.10 | 2300 | open long, VWAP 2300 |
| 2 | Buy / In | 1000 | 0.10 | **2290** | same `position_id` 10 → `ScaleIn` |
| 3 | Sell / Out | 1000 | 0.10 | 2310 | remaining 0.20 → partial |
| 4 | Sell / Out | 1000 | 0.10 | 2320 | remaining 0.10 → complete |

Deal 2: `Direction == Long`, `2290 < 2300` → `worse == true` → `WasAveragedDown = true`. Then VWAP updates to `(2300×0.10 + 2290×0.10) / 0.20 = 2295`. The flag is already latched; the new VWAP is not used for this fill.

Assertions on the single completed trade: `WasScaledIn`, `WasPartialClose`, **`WasAveragedDown`**, `Completed`, `MaxVolumeLots == 0.20`.

Measured this session: **Passed**. Under the inverted HEAD comparison this same fact is the B08 red case (`2290 > 2300` is false). The pass is evidence the polarity change is live in the compiled Domain assembly (`TraderIntelligence.Domain -> …\bin\Debug\net8.0\TraderIntelligence.Domain.dll` immediately before the test).

### 3.2 A21 F07 (spec, not ported as its own fact)

| Ticket | Side | Price | `vwap_before` | Decision |
|---:|---|---:|---:|---|
| 701 | LONG IN | 2400 | (open) | start |
| 702 | LONG IN | **2380** | 2400 | `2380 < 2400` → avg-down |
| 703 | OUT | 2390 | — | complete |

Working-tree `ScaleIn` makes the same decision as A21 §7.4:

```
if t.direction == LONG  and deal.price < vwap_before: book.seen_avg_down = true
if t.direction == SHORT and deal.price > vwap_before: book.seen_avg_down = true
```

F02 (long add **higher** 2400 then 2410) must stay `avg_dn = F`. Code: `2410 < 2400` is false. **No unit fact asserts this negative.**

F08 (short add **higher** 2400 then 2415) must be `avg_dn = T`. Code: short branch `2415 > 2400`. **No unit fact.**

F12 trade #2 (short 2410 then add 2400) is **better** for a short (sold lower). A21 self-corrects: `was_averaged_down=F`. Code: `2400 > 2410` is false. **No unit fact.**

---

## 4. Four-cell polarity (code, not a 4-fact suite)

| Direction | Add vs prior VWAP | `worse` expression | `WasAveragedDown` | Name |
|---|---|---|---|---|
| Long | `price < vwap` | `deal.Price < EntryVwap` | **true** | **add-lower = averaging down** |
| Long | `price > vwap` | false | unchanged | add-in-profit / scale-up |
| Long | `price == vwap` | false | unchanged | not worse |
| Short | `price > vwap` | `deal.Price > EntryVwap` | **true** | add-higher = averaging down |
| Short | `price < vwap` | false | unchanged | sold cheaper = better |
| Short | `price == vwap` | false | unchanged | not worse |

This is the opposite of HEAD. It matches A21 §4.1:

> LONG: price < vwap_before  
> SHORT: price > vwap_before

It is **price vs VWAP**, not mark-to-market PnL, not deal.Profit, not “any second IN.” A long that adds lower while the first deal still shows profit 0 is still averaging down (F07 and the unit fixture both use profit 0 on the add).

---

## 5. Downstream (not re-derived)

`BaselineScorer.ComputeFeatures` sets `AveragingDown = trades.Any(t => t.WasAveragedDown)`. If reconstruction polarity were still inverted, the risk flag would miss every long add-lower and would false-flag long add-higher. With the working-tree flip, a completed long add-lower lifecycle will raise `FeatureSnapshot.AveragingDown` and add the stub’s +20 risk points. That path was **not** re-executed here (no scorer fact uses a reconstructed add-lower book; `BaselineScorerTests` hand-builds `WasAveragedDown = false`).

`EfTradingStore` copies the reconstructor flag onto `ReconstructedTrade.WasAveragedDown`. Persistence was not exercised.

---

## 6. Honesty box — what this does **not** prove

| Claim | Measured |
|---|---|
| Long add-lower is averaging down in working-tree `ScaleIn` | **Yes** (code + passing fused fact) |
| Averaging-down detection is §60 COVERED | **No.** Still PARTIAL (C17). One fused fact; no add-in-profit negative; no short cell; no A21 F07/F08. |
| A89 `AveragingDownFlagReconstructionTests` / `AveragingDownDetectorTests` exist | **No.** Still missing as dedicated classes. |
| Change is committed | **No.** Uncommitted on `TradeReconstructor.cs`. HEAD still inverted. |
| F07 numbers (VWAP 2390, net −12.80) locked | **No.** Unit fixture uses 2300/2290 and never asserts VWAP. |
| Deal-reason / SPLIT cannot false-flag (A82) | **Not reviewed.** Reason-blind `ENTRY_IN` still looks like a scale-in. |
| Product source edited by C51 | **No.** |

---

## 7. Files cited

- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (SHA-256 `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD`)
- `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs`
- `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs`
- `D:\Prop\src\Domain\Enums\TradeDirection.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\docs\trade-reconstruction.md`
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md`
- `D:\Prop\reports\swarm\20260818\A89_unit_class_list.md` (G1 stale vs working tree)
- `D:\Prop\reports\swarm\20260818\B08_tests_gap.md` (inverted HEAD)
- `D:\Prop\reports\swarm\20260818\B11_recon_review.md`
- `D:\Prop\reports\swarm\20260818\C17_unit_coverage.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §14 / §60
