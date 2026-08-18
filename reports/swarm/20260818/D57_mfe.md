# D57 — Does the scorer fabricate MFE?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D57_mfe.md` |
| Agent | D57 (scorer MFE honesty) |
| Date | 2026-08-18 |
| Assigned question | **Does scorer fabricate MFE?** Write this report. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (`FeatureSnapshot` + `BaselineScorer.ComputeFeatures` + `Score`) |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| SUT size / mtime | 8143 bytes / 212 lines / 2026-08-18 13:08:10 |
| Tests read | `D:\Prop\tests\Unit\BaselineScorerTests.cs` (SHA-256 `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408`, 2414 bytes) |
| Callers read | `ReconstructionScoringService.RebuildTraderAsync`; `EfTradingStore.UpsertScoreAsync`; `TraderScore` / `TraderScoreHistory`; `EfDashboardQueries`; `DashboardModels`; `TradeReconstructor`; `ReconstructedTrade` / `ReconstructedTradeResult`; `FeatureQuality`; `PriceSource` |
| Law | Architecture v2 **§1.5**, **§17**, **§18**, **§51**, **§60**; A22 I7 / §3.1 / §3.7; A45 fabrication + `feature_quality` catalog |
| Prior (same SHA, not copied as verdict) | A17, A45, B12 L5, C17 area 10, C60, D12, D34 |
| Measurement harness | `D:\Prop\reports\swarm\20260818\_tmp_d57_mfe\` (Domain-only `dotnet run`, **not** product) |
| Measured dump | `D:\Prop\reports\swarm\20260818\_tmp_d57_mfe\D57_measured.tsv` |
| Method | Full re-read of `BaselineScorer.cs`. Count every `AverageMfe` / `AverageMae` / `MaeMfeQuality` write. Trace persist + dashboard + web. Grep product `*.cs` for `FeatureQuality.Exact`, `MfeMaeCalculator`, ticks. Execute the live stub on empty / deals-only / VWAP-mutated books. Nothing answered from memory. |

**Assigned answer:** **No. `BaselineScorer` does not fabricate MFE.**

**One-line:** the scorer **omits** excursion. `AverageMfe` / `AverageMae` stay **null** on every measured book (including a 2000→3000 VWAP mutation that would have produced a 1000-point fake MFE). `MaeMfeQuality` is always `Unavailable`. Score terms never read those slots. Persist and the dashboard have **no MFE columns**. That is §1.5 / A22 I7 / A45 **correct omission**, not `EXACT`, and not a passing “MFE/MAE when valid” implementation.

---

## 0. Verdict

| Check | Class | One-line |
|---|---|---|
| Does `Score()` / `ComputeFeatures()` emit an MFE number from closed deals? | **NO** | `AverageMfe` assignment count in the SUT = **0**. Measured 8 books → `NULL`. |
| Does it emit MAE from closed deals? | **NO** | `AverageMae` assignment count = **0**. Measured 8 books → `NULL`. |
| Does it stamp `feature_quality=EXACT` or `APPROXIMATE` without a tape? | **NO** | Only write is `MaeMfeQuality = FeatureQuality.Unavailable` (non-empty path). Empty path uses the record default, also `Unavailable`. `FeatureQuality.Exact` is **never assigned** in product C#. |
| Does it derive excursion from `EntryVwap` / `ExitVwap`? | **NO** | Scorer never reads VWAP. Mutating 2300/2301 → 2000/3000 left risk/behavior/quality/state **identical** (`VWAP_MUTATION_SCORES_IDENTICAL=True`). |
| Does it mix cTrader quotes / session highs into source MFE? | **NO** | `DestinationQuotes` are FIX UI + last-quote paint only. Not an input to `BaselineScorer`. |
| Do A22 optional 0.07 / 0.08 MFE terms fire? | **NO** | Those terms are not in the stub. Effective `mfe_mae_used = false` forever. |
| Is MFE persisted or shown? | **NO** | `TraderScore` has no MFE fields. `TraderRowDto` / `TraderDetailDto` have none. `apps/web/src` has **0** `mfe`/`mae` hits. |
| Is this a PASS of “MFE/MAE when valid”? | **NO** | Tape, table, calculator, feature row, and §60 tests are **MISSING**. Omit is honest; the feature is **UNAVAILABLE**. |
| Is the omit locked by a unit test? | **NO** | `MfeMaeCalculatorTests` / `MfeMaeFeatureQualityTests` are not on disk. D34: MFE never asserted. |
| Product source changed by D57 | **NO** | Report + throwaway eval only. |

**Do not claim** “MFE is implemented.” **Do not claim** “MFE is exact.” **Do not claim** “§60 MFE/MAE where data exists is covered.” **Do not claim** A22 `mfe_capture_score` is wired. **Do not claim** a future edit cannot fill the unused slots from VWAP — the slots exist; nothing today writes them.

A45 definition of **fabrication** (quoted, then applied):

> Emitting MFE/MAE from inputs that are not a price path over the window (closed deals only, entry+exit VWAP, last mark, session high/low, interpolated ticks, “typical XAU range”).

The SUT **does not emit** those numbers. Omission ≠ fabrication.

---

## 1. Binding rule

Architecture §1.5:

> Do not calculate MFE/MAE from closed deals alone. Exact MFE/MAE requires price/tick observations while a position is open. If source-side tick data is not available, do not fabricate these features.

Architecture §17: exact MFE / MAE / excursion / entry spread / in-trade volatility need a **source-side time series while the position is open**. Preferred input is the source MT5 tick feed. If missing: store the best available series **explicitly**, label `price_source` + `feature_quality`, and **never pretend** a cTrader quote stream is the source book.

A22 I7:

> Do not fabricate MFE/MAE from closed deals. Use them in scores only when `feature_quality == EXACT`. (§17)

A22 §3.1: if `feature_quality != EXACT` or either value is null, set `mfe_mae_used = false` and **drop** every MFE/MAE term. Do **not** approximate from `entry_vwap`/`exit_vwap`.

A45 §4: `EXACT` requires a single-broker source tick tape over `[opened_at, closed_at]` with bid **and** ask. No such tape exists in this tree (`mt5_xau_ticks` is not a `DbSet`; `Mt5XauTick.cs` is False; `MfeMaeCalculator` is False — C60, remeasured).

Legal published states today:

| State | When legal | What the stub does |
|---|---|---|
| omit + `UNAVAILABLE` + nulls | no covering source tape | **This is what runs** |
| `APPROXIMATE` + labeled bars | bars cover the window, never sold as ticks | **Not implemented** |
| `EXACT` + source ticks | A45 §4.1 all true | **Impossible today** |

---

## 2. What is actually in the SUT

One compilation unit, four types. MFE lives only on `FeatureSnapshot`.

```5:26:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public sealed record FeatureSnapshot
{
    public required int CompletedXauTrades { get; init; }
    // ... deal-derived aggregates ...
    public FeatureQuality MaeMfeQuality { get; init; } = FeatureQuality.Unavailable;
    public decimal? AverageMfe { get; init; }
    public decimal? AverageMae { get; init; }
    public PriceSource PriceSource { get; init; } = PriceSource.Unknown;
}
```

`decimal?` defaults to **null**. There is no `= 0m`. A zero MFE would have been a fabrication smell; that default is not present.

### 2.1 Empty book (`N == 0`)

```45:63:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (trades.Count == 0)
        {
            return new FeatureSnapshot
            {
                CompletedXauTrades = 0,
                NetPnl = 0,
                // ... zeros / false flags ...
                TradeFrequencyPerDay = 0
            };
        }
```

This object initializer **does not mention** MFE. Record defaults apply: `MaeMfeQuality=Unavailable`, `AverageMfe=null`, `AverageMae=null`, `PriceSource=Unknown`. Measured: `EMPTY` row in `D57_measured.tsv`.

### 2.2 Non-empty book

```108:126:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        return new FeatureSnapshot
        {
            CompletedXauTrades = trades.Count,
            // ... NetPnl, PF, CVs, flags, hold, SL, DD, frequency ...
            MaeMfeQuality = FeatureQuality.Unavailable,
            PriceSource = PriceSource.Unknown
        };
```

`AverageMfe` and `AverageMae` are **not written**. They stay null. Quality is **forced Unavailable** even if a caller later tried to pre-fill a snapshot — `ComputeFeatures` always constructs a new record.

`PriceSource.Unknown` with **no number** is the A45 naming smell B12 already flagged (UNKNOWN *with* a number is forbidden; omit-with-Unknown is not a silent mix). It is **not** `AchieverMt5Ticks`, not `BarApproximation`, not `CTraderQuoteSession`.

### 2.3 `Score()` does not consume MFE

`Score()` builds features, then:

```134:160:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var risk = 0m;
        if (features.Martingale) risk += 35;
        if (features.AveragingDown) risk += 20;
        if (features.LotEscalation) risk += 15;
        if (features.LotCv > 0.5m) risk += 10;
        if (features.SlUseRate < 0.3m) risk += 10;
        if (features.MaxDrawdown > 0 && features.GrossProfit > 0 && features.MaxDrawdown > features.GrossProfit)
            risk += 10;
        // behavior: martingale / averaging / lot CV / SL / loss-size CV
        // quality: NetPnl sign, ProfitFactor, behavior, risk, N<3 cap
```

Inputs actually read from each `ReconstructedTradeResult`:

| Field | Used by scorer? | Could it fake MFE? |
|---|---|---|
| `Completed`, `IsXauUsd`, `ClosedAt`, `OpenedAt` | yes (filter, order, hold, frequency) | no |
| `NetRealizedPnl` | yes (NET, PF, DD, martingale pair) | **realized P&L ≠ MFE** |
| `MaxVolumeLots` | yes (CV, martingale, escalation) | no |
| `WasAveragedDown` | yes (flag) | no — reconstructor compares fill vs entry VWAP, not excursion |
| `InitialSl` | yes (rate) | no |
| `EntryVwap`, `ExitVwap` | **never** | the A22-forbidden proxy is unused |
| ticks / bid / ask / high / low | **no such fields on the trade record** | cannot invent a path from a record that has none |

`FromBaseline` also ignores `AverageMfe` / `MaeMfeQuality`. A22 §3.7 `mfe_capture_score` / `mae_overrun_risk` are **absent**.

### 2.4 Grep of product C# (remeasured)

| Needle | Hits in `D:\Prop\src\**\*.cs` |
|---|---|
| `AverageMfe` / `AverageMae` / `MaeMfeQuality` | **only** `BaselineScorer.cs` (declaration + one `Unavailable` write) |
| `FeatureQuality.Exact` / `FeatureQuality.Approximate` | **0** |
| `MfeMaeCalculator` | **0** (paths `Domain\Scoring\MfeMaeCalculator.cs` and `src\Scoring\Features\MfeMaeCalculator.cs` = False) |
| `Mt5XauTick` / `mt5_xau_ticks` | **0** |
| `mfe` / `mae` / `MFE` / `MAE` / `excursion` in `apps\web\src` | **0** |
| `mfe` / `MAE` / `excursion` in `mt5-sdk\src` | **0** |

`FeatureQuality` exists as a closed enum (`Exact=0`, `Approximate=1`, `Unavailable=2`). Only `Unavailable` is ever stored on a snapshot.

---

## 3. Measured: deals-only + VWAP mutation

Harness: `dotnet run -c Release --project D:\Prop\reports\swarm\20260818\_tmp_d57_mfe\D57MfeEval.csproj` against the **same** Domain DLL (SUT SHA above). Product source not referenced for edit.

| id | N | AvgMfe | AvgMae | MaeMfeQuality | PriceSource | Risk | Behavior | EQ | State |
|---|---:|---|---|---|---|---:|---:|---:|---|
| `EMPTY` | 0 | NULL | NULL | Unavailable | Unknown | 10 | 90 | 40 | INSUFFICIENT_DATA |
| `N1_DEAL_ONLY` | 1 | NULL | NULL | Unavailable | Unknown | 0 | 100 | 40 | INSUFFICIENT_DATA |
| `N2_DEAL_ONLY` | 2 | NULL | NULL | Unavailable | Unknown | 0 | 100 | 40 | INSUFFICIENT_DATA |
| `FX02_WINNERS_VWAP_2300_2301` | 3 | NULL | NULL | Unavailable | Unknown | 0 | 100 | 100 | SHADOW |
| `FX02_WINNERS_VWAP_2000_3000` | 3 | NULL | NULL | Unavailable | Unknown | 0 | 100 | 100 | SHADOW |
| `FX02_WINNERS_VWAP_NULL_EXIT` | 3 | NULL | NULL | Unavailable | Unknown | 0 | 100 | 100 | SHADOW |
| `FX03_LOSING_MART` | 3 | NULL | NULL | Unavailable | Unknown | 60 | 60 | 47 | RISK_BLOCKED |
| `MILD_MART_WIN` | 3 | NULL | NULL | Unavailable | Unknown | 35 | 70 | 85.25 | SHADOW |

Predicate line from the same run:

```text
VWAP_MUTATION_SCORES_IDENTICAL=True
BOTH_AVERAGES_NULL=True
BOTH_QUALITY_UNAVAILABLE=True
```

If the stub had treated `|exit − entry|` (or favorable side of that delta) as MFE:

- 2300 → 2301 would have emitted **1**
- 2000 → 3000 would have emitted **1000**
- scores that consumed `mfe_capture` would have **moved**

None of that happened. The 2000/3000 book is bit-identical on the three scores and on state to the 2300/2301 book. Null exit VWAP also does not invent a number.

`EMPTY` risk=10 / behavior=90 is the known empty-book SL-rate penalty (C23). That is **not** an MFE number.

---

## 4. Downstream cannot invent what the scorer never wrote

### 4.1 Persist

`ReconstructionScoringService.RebuildTraderAsync` copies only:

`RiskScore`, `BehaviorScore`, `EarlyQualityScore`, `CompletedXauTrades`, `Martingale`, `AveragingDown`, `LotEscalation`, `CurrentState`, `LastScoredAt`.

`TraderScore` and `TraderScoreHistory` have **no** MFE / MAE / `feature_quality` / `price_source` / `mfe_mae_used` columns. `UpsertScoreAsync` cannot persist a fabricated excursion even if the in-memory snapshot later grew one.

### 4.2 Dashboard / API / web

`TraderRowDto` / `TraderDetailDto` / `TradeHighlightDto` have no `mfe`, `mae`, `mfeMaeValid`. `EfDashboardQueries` never reads `AverageMfe`. A26/A93 `MfeMaeMetaDto` is **spec only**.

`DestinationQuotes` are selected for FIX session paint (`Bid`/`Ask`/`QuoteAgeSeconds`). That is destination market data, not a source MFE backfill. A45 silent-mix: **CLEAN** because no MFE is emitted at all.

### 4.3 Reconstruction is not an excursion calculator

`ReconstructedTrade` / `ReconstructedTradeResult` correctly have **no** MFE/MAE fields (A21, A45). `EntryVwap` / `ExitVwap` are fill VWAP. The only VWAP comparison in Domain is averaging-down (`deal.Price` vs current entry VWAP inside `TradeReconstructor`). That flag is a lifecycle fact, not a high/low path.

### 4.4 Tests do not lock the omit

`BaselineScorerTests` (3 facts, D34) asserts eligibility + `SuggestedState` + one `Martingale` bool. Zero asserts on `AverageMfe`, `AverageMae`, `MaeMfeQuality`, `PriceSource`.

Named classes from A09 / A27 / A45 / A89 are **MISSING**:

- `tests/Unit/Features/MfeMaeCalculatorTests.cs` = False
- `tests/Unit/Features/MfeMaeFeatureQualityTests.cs` = False
- `tests/Unit/Features/MfeMaeMissingTickDataTests.cs` = False

C17 area 10 remains **MISSING**. Policy holds by **omission of code**, not by a red test if someone later writes `AverageMfe = ExitVwap - EntryVwap`.

---

## 5. What this is *not*

| Claim someone might make | Reality |
|---|---|
| “MFE is ready / exact / valid” | Tape + calculator + labeled feature row are **MISSING**. Quality is `Unavailable`. |
| “Scorer fabricates MFE from deals” | **False.** No number is emitted. |
| “Quality uses NET, therefore it fakes MFE” | NET is realized P&L (A22 §3.2 `pnl_i`). That is a different, allowed (if I9-dirty) feature. It is not excursion. |
| “Drawdown is MFE/MAE” | Peak-to-trough of **completed-trade equity**, not in-trade price excursion. |
| “A22 `mfe_capture_score` is on” | Stub has no such term. `mfe_mae_used` is effectively false. |
| “§60 item 10 is covered because omit is correct” | Coverage requires a test that **refuses** deals-only fabrication. That test is not written. |
| “`PriceSource.Unknown` is a silent mix” | Mix requires **two feeds and one number**. There is no number. Smell only. |
| “Dashboard shows MFE when valid” | Field family is absent. Honest blank, not a valid/invalid widget. |

---

## 6. What would count as fabrication later

A future change is fabrication (A45) if it does any of:

1. Sets `AverageMfe` / `AverageMae` from `EntryVwap`/`ExitVwap`, `NetRealizedPnl`, last mark, or session `TickStat` high/low.
2. Stamps `FeatureQuality.Exact` without a same-broker bid/ask tape covering the window.
3. Gap-fills source holes with `DestinationQuotes` and still emits one MFE.
4. Writes `"0"` instead of JSON `null` when the tape is missing (A67).
5. Feeds those numbers into risk/behavior/quality without `mfe_mae_used` (A22: only when `EXACT` and both present).

None of those exist in the SHA above.

Honest next increment (not this agent, not this change-set): keep nulls until `mt5_xau_ticks` + `MfeMaeCalculator` + `trader_feature_snapshots` exist; add `MfeMaeFeatureQualityTests.Deals_only_quality_is_Unavailable` / `Does_not_fill_average_mfe_from_vwap` so the omit cannot regress.

---

## 7. Files read (absolute)

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | SUT |
| `D:\Prop\src\Domain\Enums\FeatureQuality.cs` | closed catalog |
| `D:\Prop\src\Domain\Enums\PriceSource.cs` | closed catalog |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | persist shape — no MFE |
| `D:\Prop\src\Domain\Entities\TraderScoreHistory.cs` | history shape — no MFE |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | lifecycle — no MFE |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | scorer input — no MFE |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | VWAP for averaging-down only |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `RebuildTraderAsync` copy list |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | DTOs — no MFE |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `UpsertScoreAsync` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | no tick `DbSet` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | no MFE projection |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | no MFE asserts |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §1.5 / §17 |
| `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` | I7 |
| `D:\Prop\reports\swarm\20260818\A45_mfe_mae_policy.md` | fabrication definition |
| `D:\Prop\reports\swarm\20260818\_tmp_d57_mfe\D57_measured.tsv` | this pass |

---

## 8. Close

**Question:** Does the scorer fabricate MFE?

**Answer:** **No.**

The production scorer is an unversioned deals-only stub. It **labels MFE unavailable, leaves averages null, does not read VWAP, and does not score A22 MFE terms.** Persist, API, and web never display an excursion. That is the legally required behavior while the source tick tape is missing. It is **not** a gold-file of `baseline.v1` MFE, and it is **not** locked by a refuse-to-fabricate test.

Product source was not modified.
