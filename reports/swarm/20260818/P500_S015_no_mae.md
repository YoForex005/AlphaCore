# P500_S015 — No MAE-based stops / no MFE capture for copy sizing

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S015_no_mae.md` |
| Agent | P500_S015 (MAE/MFE honesty for copy risk) |
| Date | 2026-08-18 |
| Slot | **S015** |
| Angle | `FeatureSnapshot.MaeMfeQuality` default + Architecture §1.5 / §17 do-not-fabricate ticks → what copy sizing and lower-loss stops may **not** claim |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| SUT read | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (full, 212 lines) |
| Enums read | `D:\Prop\src\Domain\Enums\FeatureQuality.cs`, `D:\Prop\src\Domain\Enums\PriceSource.cs` |
| Risk / dest quote read | `D:\Prop\src\Domain\Risk\RiskEngine.cs`, `D:\Prop\src\Domain\Entities\DestinationQuote.cs` |
| Persist read | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` (`destination_quotes` map only) |
| Law | Architecture v2 **§1.5**, **§17**, **§18**, **§24**, **§31**, **§37**, **§38**, **§51**, **§60**; A22 I7 / §3.7; A43 §15; A45; A72 |
| Prior (not copied as verdict) | A17, A45, C60, D57, A006, A009 |

**One-line:** `MaeMfeQuality` is **Unavailable** by record default and by every `ComputeFeatures` write. There is no source tick tape. **Do not claim MAE-based copy stops. Do not claim MFE-capture copy sizing.** Lower-loss stops, when they exist, must be priced from **destination quotes**, never from fabricated source excursion.

## Profit implication

Inventing MAE stops or MFE-capture size from closed-deal VWAP is a **false risk number**. A stop that is not on the dest book does not cut dest loss. A size scaled by a fabricated capture ratio is not +EV. Lower loss = omit source excursion; later dest stops from dest bid/ask only. Do not send sized “because MAE is small.”

**Remeasured 2026-08-18:** `MaeMfeQuality` default + write = `Unavailable` (`BaselineScorer.cs` L22, L124). `AverageMfe` / `AverageMae` never assigned. `grep FeatureQuality.Exact` / `Approximate` in `src/` = **0**. `CopyTradingService` does not read MAE. Still no tick `DbSet`.

---

## 0. Verdict

| Claim someone might write | Allowed today? | Why |
|---|---|---|
| `FeatureSnapshot.MaeMfeQuality` defaults to `Unavailable` | **YES — measured** | Property initializer `= FeatureQuality.Unavailable` (`BaselineScorer.cs` L22). Empty-book path never overrides it. Non-empty path **forces** `Unavailable` (L124). |
| Exact MFE/MAE exists for scored traders | **NO** | `AverageMfe` / `AverageMae` are never assigned. They stay `null`. `FeatureQuality.Exact` is never written in product C#. |
| Closed deals / entry–exit VWAP can stand in for MAE | **NO** | Architecture §1.5 / §17: do **not** fabricate ticks. A22 I7: use excursion in scores only when `feature_quality == EXACT`. A45: `DEAL_PATH` is forbidden. |
| Copy stops may be set from source MAE (e.g. stop at `entry ± MAE`) | **NO — forbidden** | MAE is omitted, not measured. A MAE-distance stop would be a fabricated path. |
| Copy size may be scaled by MFE capture (`realized / MFE`) | **NO — forbidden** | A22 `mfe_capture_score` / `mean_mfe_capture` are **not** in `BaselineScorer.Score`. A43 converter must not invent MFE/MAE. |
| Destination quotes may back-fill source MFE/MAE | **NO — silent mix** | §17 / A45: cTrader QUOTE is a different book. `CTRADER_FIX_QUOTES` is illegal for source excursion. |
| Lower-loss / protective stops on the copy, once quotes exist | **YES, later — dest book only** | Price the stop from the **destination** bid/ask (and dest fill), not from source MAE. Quotes do not exist as a live feed today; do not pretend they do. |

**Honesty class:** correct **omission** of source excursion (D57), plus a **hard consumer ban** on using those empty slots for stops or sizing.

This file does **not** implement stops. It does **not** add ticks. It does **not** change `BaselineScorer`.

---

## 1. Binding architecture (do not fabricate ticks)

§1.5 (executive change #5):

> Do not calculate MFE/MAE from closed deals alone. Exact MFE/MAE requires price/tick observations while a position is open. If source-side tick data is not available, do not fabricate these features.

§17 (source-side market data):

Exact MFE, MAE, price excursion, entry spread, and in-trade volatility need a **source-side time series while each source trade is open**. Preferred input is the source MT5 tick / Manager symbol subscription. If that tape is missing: store the best available series **explicitly**, label `price_source` + `feature_quality`, and **never pretend** another broker’s cTrader quote feed is the source MT5 book. Never silently mix them.

A22 I7:

> Do not fabricate MFE/MAE from closed deals. Use them in scores only when `feature_quality == EXACT`.

A22 §3.1 / §3.7: if quality is not `EXACT` or either value is null, `mfe_mae_used = false` and **every** MFE/MAE score term is dropped. Do not approximate from `entry_vwap` / `exit_vwap`. Optional terms that therefore **must not fire** today:

```text
mae_overrun_risk
mfe_capture_score          // LerpScore(mean_mfe_capture, …)
mfe_capture_i = realized_favorable_i / mfe_i
```

A45 fabrication definition (applied, not restated as a new law):

> Emitting MFE/MAE from inputs that are not a price path over the window (closed deals only, entry+exit VWAP, last mark, session high/low, interpolated ticks, “typical XAU range”).

A45 also forbids gap-filling a source window with destination quotes. That mix stays illegal even if someone later stamps `APPROXIMATE`.

A43 §15 (quantity converter must not):

> Invent MFE/MAE or destination costs.

---

## 2. Measured `FeatureSnapshot` default

```5:26:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public sealed record FeatureSnapshot
{
    public required int CompletedXauTrades { get; init; }
    public required decimal NetPnl { get; init; }
    public required decimal GrossProfit { get; init; }
    public required decimal GrossLoss { get; init; }
    public required decimal ProfitFactor { get; init; }
    public required decimal LotCv { get; init; }
    public required decimal LossSizeCv { get; init; }
    public required bool Martingale { get; init; }
    public required bool AveragingDown { get; init; }
    public required bool LotEscalation { get; init; }
    public required decimal AverageHoldSeconds { get; init; }
    public required decimal SlUseRate { get; init; }
    public required decimal MaxDrawdown { get; init; }
    public required decimal TradeFrequencyPerDay { get; init; }
    public FeatureQuality MaeMfeQuality { get; init; } = FeatureQuality.Unavailable;
    public decimal? AverageMfe { get; init; }
    public decimal? AverageMae { get; init; }
    public PriceSource PriceSource { get; init; } = PriceSource.Unknown;
}
```

Closed enum (`D:\Prop\src\Domain\Enums\FeatureQuality.cs`):

```text
Exact = 0
Approximate = 1
Unavailable = 2
```

The property initializer is load-bearing. Without it, a C# enum field would default to **`Exact = 0`** and a future caller could publish a null MFE as exact. The empty-book initializer (L47–63) does **not** mention MFE; the record default is the only quality on `N == 0`.

Non-empty path **re-asserts** the omit:

```108:126:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        return new FeatureSnapshot
        {
            CompletedXauTrades = trades.Count,
            // … deal-derived aggregates only …
            MaeMfeQuality = FeatureQuality.Unavailable,
            PriceSource = PriceSource.Unknown
        };
```

`AverageMfe` and `AverageMae` are not in the initializer. `decimal?` stays **null** (not `0`). `PriceSource.Unknown` carries **no number** — A45 forbids UNKNOWN *with* a number; omit-with-Unknown is a naming smell, not a silent mix (B12 L5).

`Score()` never reads `AverageMfe`, `AverageMae`, or `MaeMfeQuality`. Risk / behavior / early-quality use martingale, averaging, lot CV, SL-use rate, realized DD vs gross profit, loss-size CV, net PnL, profit factor. **No excursion term.** `TraderStateMachine.FromBaseline` also ignores the MFE slots.

Grep of product `*.cs` (this pass, `D:\Prop\src`):

| Needle | Hits |
|---|---|
| `MaeMfeQuality` | **only** `BaselineScorer.cs` (default + one write) |
| `AverageMfe` / `AverageMae` | **only** the two nullable slots on `FeatureSnapshot` |
| `FeatureQuality.Exact` / `FeatureQuality.Approximate` | **0** |
| `MfeMaeCalculator` | **0** |
| `Mt5XauTick` / `mt5_xau_ticks` | **0** (C60 still holds) |

There is no source tick `DbSet`. There is no calculator. There is no persisted `trader_feature_snapshots` entity. D57 measured a 2000→3000 VWAP mutation that would have invented a 1000-point fake MFE; scores did not move. That measurement is not re-run here; the write sites have not changed.

---

## 3. Why this blocks MAE-based copy stops

A “MAE-based stop” means: take the source trade’s maximum adverse excursion (or a multiple of it) and place a destination stop that many price units from copy entry.

That construction requires **all** of:

1. A legal source MAE (`feature_quality == EXACT`, non-null, same `broker_id` tape over `[opened_at, closed_at]` — A45 §4.1).
2. A mapping from source price units to destination price units (not in A43; A43 explicitly does **not** convert SL/TP distances).
3. A destination mark to hang the stop on (dest entry / dest bid-ask).

Today (1) is false: quality is `Unavailable`, MAE is null, tape is missing. Using (a) closed-deal loss size, (b) `|exit_vwap − entry_vwap|`, (c) source `InitialSl`, or (d) “typical XAU MAE” as the stop distance is **fabrication** under A45. Source `InitialSl` is a **declared** broker stop on the leader, not an observed excursion; `SlUseRate` may count it, but it is not MAE and it is not a dest price.

Therefore:

```text
FORBIDDEN:
  dest_stop = dest_entry ± k * source_MAE
  dest_stop = dest_entry ± |exit_vwap - entry_vwap|
  dest_stop = dest_entry ± “typical MAE”
  dest_stop = source_InitialSl copied as a price

NOT A SUBSTITUTE:
  FeatureSnapshot.AverageMae   // always null
  FeatureSnapshot.MaeMfeQuality // always Unavailable
```

Do not document, dashboard, or size a “MAE stop” while this default holds.

---

## 4. Why this blocks MFE-capture copy sizing

A22 §3.7 `mfe_capture_i = realized_favorable_i / mfe_i` is a **score term**, and only when `mfe_mae_used`. It is not a lot multiplier.

Even if someone later wires `mfe_capture_score`, Architecture §38 / A43 still convert source lots → canonical ounces → dest `OrderQty` through allocation and dest min/step/max. Capture ratio is **not** an input to that pipeline. A43 §15 forbids inventing MFE/MAE to feed the converter.

```text
FORBIDDEN:
  dest_qty *= mean_mfe_capture
  dest_qty *= AverageMfe / AverageMae
  “scale up because they capture MFE”
  “scale down because MAE overrun”

LEGAL SIZING INPUTS (when those specs exist):
  source lots, source contract size, allocation fraction,
  dest min / step / max, risk caps (A23 §6.4)
```

`RiskEngine` today sizes by **requested quantity vs hard caps** (`MaxPositionQuantity`, gross/net XAU, margin, martingale/abnormal flags). It never reads `FeatureSnapshot`. It cannot apply MFE capture because the number does not exist and the engine does not ask for it.

---

## 5. Lower-loss stops: destination quotes, once they exist

Protective / lower-loss stops on **our** copy are a **destination** problem. Architecture already splits the books:

| Book | Tape | Legal use |
|---|---|---|
| Source MT5 (Achiever / StarwaveFX / future) | `mt5_xau_ticks` (MISSING) | Source MFE/MAE **only** when A45 `EXACT`/`APPROXIMATE` |
| Destination cTrader / Pepperstone | `destination_quotes` (table mapped; **no live QUOTE writer**) | Shadow marks, slippage, pre-trade guards, **copy stop prices** |

§24 / §31: shadow and live copy price off the cTrader QUOTE session (bid/ask, freshness, slippage). §37 / A23 §6.3: taker-touch is dest bid (sell) / dest ask (buy), not source mid, not source MAE.

When (and only when) a usable destination quote exists:

```text
lower_loss_stop is computed on the destination book:
  BUY  copy  → stop below dest entry, referenced to dest Bid (and dest fill)
  SELL copy  → stop above dest entry, referenced to dest Ask (and dest fill)

quote must pass the same family of guards as OPEN/INCREASE:
  present, fresh (MaxQuoteAge), bid>0, ask>=bid, spread, adverse move

source MAE / source SL / source VWAP MUST NOT set dest StopPx
destination quotes MUST NOT be written into FeatureSnapshot.AverageMae
```

“Lower loss” here means a protective dest stop that caps **our** copy loss. It is not “copy the leader’s MAE.” Leader MAE, if it ever becomes `EXACT`, may inform **scoring** (A22 optional terms). It still does not print dest `StopPx`.

### 5.1 Quotes do not exist as a feed today — do not claim they do

Measured persist:

```149:153:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
        modelBuilder.Entity<DestinationQuoteSnapshot>(e =>
        {
            e.ToTable("destination_quotes");
            e.HasKey(x => x.Id);
        });
```

`DestinationQuoteSnapshot` is `{ Bid, Ask, ReceivedAt, VenueTimestamp, CanonicalSymbol, VenueInstrumentId }`. `RiskEngine` rejects increasing exposure when `Quote is null` (`QUOTE_MISSING`) or stale / wide / moved. A72 already recorded: `Evaluate` is **not** called from Application/workers; FIX QUOTE MD is not persisted; two disagreeing age defaults. So:

- You **may not** say “MAE stops are live.”
- You **may not** say “dest lower-loss stops are live.”
- You **may** say: the **only legal future path** for copy stops is dest quotes + dest fills; the **only legal path** for source MFE/MAE is a labeled source tape.

Until a dest quote row is present **and** fresh, there is no dest stop price to emit. Omit the stop. Do not invent one from source excursion.

---

## 6. Consumer contract (implementers)

| Consumer | Required behavior while `MaeMfeQuality == Unavailable` |
|---|---|
| `BaselineScorer` | Keep forcing `Unavailable`; never assign averages from VWAP/deals. |
| Dashboard / API | `mfe` / `mae` null; do not show “MAE stop” or “MFE capture %”. §51: “MFE/MAE **when valid**.” |
| Copy / shadow sizing | A43 ounces + caps only. No MFE ratio. |
| Copy / shadow stops | Dest bid/ask (and dest fill) only, after quotes exist. Else omit. |
| Risk engine | Continue to require dest `Quote` on OPEN/INCREASE. Never add an MAE field as a substitute quote. |
| Persist | Do not write `feature_quality=EXACT` or a non-null MAE/MFE without A45 §4.1. |

---

## 7. What this report does **not** claim

- That MFE/MAE is implemented.
- That §60 “MFE/MAE where data exists” is tested (D34 / C17: still missing).
- That `destination_quotes` is a live QUOTE cache.
- That `RiskEngine.Evaluate` is on the send path.
- That source `InitialSl` is a dest stop.
- That `PriceSource.Unknown` is a legal published source for a **number** (it is only legal next to nulls).

---

## 8. Cross-references

| Doc | Use |
|---|---|
| Architecture §1.5 / §17 | Do not fabricate ticks; do not mix dest quotes into source excursion |
| Architecture §24 / §31 / §37 | Dest quote feed = shadow + pre-trade + (future) dest stops |
| A22 I7 / §3.7 | `mfe_capture_*` only if `EXACT` |
| A43 §15 | Converter must not invent MFE/MAE |
| A45 | `feature_quality` catalog; dest quotes illegal for source MAE |
| A72 | Quote guards; feed still missing |
| C60 / D57 | Tape missing; scorer omit measured |

**Product tree not edited.** Permanent save is this file only.
