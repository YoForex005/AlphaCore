# P500_CODE_2 — first-3 / earlyScore 95.5 is not future destination PnL

| Field | Value |
|---|---|
| Slot | **2** |
| Agent | P500_CODE_2 (senior trading-systems; this file + adjacent score/dest-PnL wiring) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| File | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` |
| Angle | Does first-3 / earlyScore 95.5 on demo `yo-2step` measure future destination PnL? |
| Verdict | **NO / FAIL_MEASUREMENT.** File was read in full (91 lines). This unit does not compute first-3, `earlyScore`, `yo-2step`, or destination realized PnL. Adjacent `EarlyQualityScore` is a **source-book** heuristic on reconstructed MT5 `NetRealizedPnl`. Dashboard `DestinationRealPnl` is the literal **0**. |
| Product source modified | **No.** Report only. |
| Method | Full `read_file` of `ShadowCopyEngine.cs`. Grep of this file for first-3 / `earlyScore` / `yo-2step` / `destinationRealPnl` / `95.5`. Grep of `src` for `EarlyQualityScore`, `DestinationRealPnl`, `MarkToMarket`, `PersistDemoShadowAsync`. Read `BaselineScorer.cs`, `EfDashboardQueries.cs`, `DealIngestionService.cs`, `EfTradingStore.PersistDemoShadowAsync`, `FakeMt5BrokerConnector` demo `yo-2step` seed. **No** `NewOrderSingle`. **No** passwords printed. |

Measured live (caller, not re-probed here): **8463** accounts; Achiever scoring; Starwave deals-done scored **0**; SHADOW all demo; `destinationRealPnl` **0**; FIX **LoggedOn**; `REAL_COPY` **false**.

Empty PASS is **not** used. The file was read. The measurement claim is false.

---

## Angle

Does first-3 / `earlyScore` **95.5** on demo `yo-2step` measure **future destination PnL**?

---

## Verdict

**NO.** First-3 / `earlyScore` 95.5 does **not** measure future destination (cTrader / Pepperstone) PnL.

| Claim | Measured |
|---|---|
| This file computes first-3 | **No.** Zero `EligibleForFirstThree` / first-3 tokens |
| This file computes `earlyScore` / 95.5 | **No.** Zero score math |
| This file knows `demo\yo-2step` | **No.** No group / login / broker string |
| This file books future dest realized PnL | **No.** `SimulateEntry` / `SimulateExit` return a fill **price**. `MarkToMarket` is instantaneous dest-touch MTM of a caller-supplied position. `ShadowPosition` is never constructed |
| `EarlyQualityScore` (dashboard `EarlyScore`) is dest PnL | **No.** Source reconstructed `NetRealizedPnl` + PF + behavior − risk |
| Overview `DestinationRealPnl` | **Literal `0`** in `GetOverviewAsync` |
| Per-trader `ShadowPnl` on leaderboard | **Literal `0`** |
| Overview `ShadowPnl` | `SUM(ShadowOrders.SourceVsShadowSlippage)` — modeled **entry slippage vs source**, not dest realized |
| A22 I8 (spec) | Three scores are **source-behavior**. Dest P&L is not inside the formulas |
| A22 I6 (spec) | As-of `T` may not use future trades / future dest book |
| Live dest real PnL | **0** (caller). Consistent with no dest PnL path |
| Seed `demo\yo-2step` login 10002 | Losing martingale (−200 / −500 / −1400, lots 0.10 → 0.20 → 0.40). That book is **not** 95.5 |

`95.5` is the scorer blend `50 + 15 + 10 + 5 + 0.2×90 − 0.25×10` (net>0, PF≥1.8, SL-use < 0.3 so behavior 90 / risk 10). That arithmetic lives in `BaselineScorer.Score`, **not** here, and it uses **source** `NetRealizedPnl`.

---

## What this file actually is

Three types, no I/O, no scoring, no group filter:

1. `ShadowFill` — dest-touch fill: id, price, qty, time, spread, quote age, `SourceVsShadowSlippage`.
2. `ShadowPosition` — dead record with `UnrealizedPnl` / `RealizedPnl` fields. Grep of product `*.cs`: **definition only**. Never constructed.
3. `ShadowCopyEngine` — `SimulateEntry`, `SimulateExit`, `MarkToMarket`.

Pricing rule:

- Long entry / short exit → dest **Ask**.
- Short entry / long exit / long MTM → dest **Bid**.
- If `modeledDelay > 250ms`, entry price is mutated by a hardcoded **±0.05** “latency” overlay. Persist path passes **80 ms**, so overlay is **off**.
- Always returns a number. No fail-closed on missing / stale / wide / crossed book.

Sole product caller (not this file): `EfTradingStore.PersistDemoShadowAsync` constructs `new ShadowCopyEngine()` and calls **`SimulateEntry` only** for already-**completed** source XAU trades when `TraderState == SHADOW`. It does **not** call `SimulateExit` or `MarkToMarket`. It does **not** persist dest realized PnL.

---

## Evidence quotes

### 1. This file — dest-touch fill, not first-3 / not score / not dest PnL

```31:60:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
public sealed class ShadowCopyEngine
{
    public const decimal DefaultLatencySlippagePoints = 0.05m;

    public ShadowFill SimulateEntry(
        string shadowOrderId,
        TradeDirection direction,
        decimal quantity,
        decimal sourcePrice,
        DestinationQuote quote,
        DateTimeOffset now,
        TimeSpan modeledDelay)
    {
        var useAsk = direction == TradeDirection.Long;
        var raw = useAsk ? quote.Ask : quote.Bid;
        var adverse = direction == TradeDirection.Long ? DefaultLatencySlippagePoints : -DefaultLatencySlippagePoints;
        if (modeledDelay > TimeSpan.FromMilliseconds(250))
            raw += adverse;

        var slippage = direction == TradeDirection.Long ? raw - sourcePrice : sourcePrice - raw;
        return new ShadowFill
        {
            ShadowOrderId = shadowOrderId,
            Price = raw,
            Quantity = quantity,
            FilledAt = now,
            Spread = quote.Ask - quote.Bid,
            QuoteAge = now - quote.ReceivedAt,
            SourceVsShadowSlippage = slippage
        };
    }
```

Exit is the same grain (a fill price). MTM is conservative dest-touch of **this** position **now**, not a future dest book:

```63:90:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public ShadowFill SimulateExit(
        string shadowOrderId,
        TradeDirection openDirection,
        decimal quantity,
        decimal sourceExitPrice,
        DestinationQuote quote,
        DateTimeOffset now)
    {
        var raw = openDirection == TradeDirection.Long ? quote.Bid : quote.Ask;
        var slippage = openDirection == TradeDirection.Long ? sourceExitPrice - raw : raw - sourceExitPrice;
        return new ShadowFill { /* Price = raw; SourceVsShadowSlippage = slippage */ };
    }

    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
```

`ShadowPosition` carries PnL fields that this class **never writes**:

```17:28:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
public sealed record ShadowPosition
{
    public required string BrokerId { get; init; }
    public required long SourceLogin { get; init; }
    public required string SourceTradeId { get; init; }
    public required TradeDirection Direction { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal EntryPrice { get; init; }
    public decimal? ExitPrice { get; init; }
    public required decimal UnrealizedPnl { get; init; }
    public required decimal RealizedPnl { get; init; }
    public required bool Open { get; init; }
}
```

Grep on **this file**: `first-3` / `FirstThree` / `earlyScore` / `EarlyScore` / `yo-2step` / `destinationRealPnl` / `DestinationRealPnl` / `95.5` / `REAL_COPY` = **0**. Only `UnrealizedPnl` / `RealizedPnl` appear, as unused record fields.

Grep of product `*.cs` for `MarkToMarket` / `ShadowPosition` constructors: **definitions only** (this file).

### 2. Adjacent scorer — first-3 is a count gate; 95.5 is source quality

```40:40:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public const int EarlyScoreTradeCount = 3;
```

```66:68:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var net = trades.Sum(t => t.NetRealizedPnl);
        var wins = trades.Where(t => t.NetRealizedPnl > 0).Select(t => t.NetRealizedPnl).ToList();
        var losses = trades.Where(t => t.NetRealizedPnl < 0).Select(t => -t.NetRealizedPnl).ToList();
```

```152:170:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
        if (features.ProfitFactor >= 1.8m) quality += 5;
        quality += behavior * 0.2m;
        quality -= risk * 0.25m;
        if (features.CompletedXauTrades < EarlyScoreTradeCount)
            quality = Math.Min(quality, 40m);
        quality = Math.Clamp(decimal.Round(quality, 2), 0m, 100m);
        // ...
            EarlyQualityScore = quality,
            EarlyScoreEligible = eligible
```

`NetRealizedPnl` is reconstructed from **source** MT5 deals (`GrossRealizedPnl + Commission + Swap + fees` in `TradeReconstructor`), not dest fills.

`ReconstructionScoringService` scores **all** completed XAU (does not slice first-3, does not filter `EligibleForFirstThree`):

```126:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore { /* EarlyQualityScore = score.EarlyQualityScore */ }, ct);
        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

High early quality → **SHADOW**, never dest PnL:

```200:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
        // ...
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;
    public static bool CanPromoteToLive(TraderState current) => false;
```

Spec pin (A22 I8): the three scores are **source-behavior**; shadow/live dest P&L is **not** inside the formulas.

### 3. Dashboard — dest real PnL is hardcoded 0; EarlyScore is the source quality number

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            /* Watch / Shadow / LiveCandidates / Live / RiskBlocked */,
            shadowPnl,
            0,   // DestinationRealPnl
            0,   // XauGross
            0,   // XauNet
            /* health */,
            _runtime.RealCopyEnabled);
```

```105:119:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            mapped.Add(new TraderRowDto(
                b.Code,
                account.Login,
                account.GroupName,
                s?.CompletedXauTrades ?? 0,
                pnl,                       // source reconstructed NetRealizedPnl
                s?.EarlyQualityScore ?? 0, // dashboard EarlyScore
                null,
                s?.RiskScore ?? 0,
                /* flags */,
                s?.CurrentState ?? TraderState.INSUFFICIENT_DATA,
                0,                         // ShadowPnl
                s?.LastScoredAt ?? account.LastSyncedAt));
```

First-3 on the detail page is a **display counter** of completed XAU rows, still source `NetRealizedPnl`:

```152:168:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var firstThree = 0;
        var highlights = trades.Select(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && firstThree < 3;
            if (first)
                firstThree++;
            return new TradeHighlightDto( /* t.NetRealizedPnl */, first);
        }).ToList();
```

### 4. Demo `yo-2step` seed is a losing martingale — not a 95.5 dest-PnL proof

```85:119:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
                new Mt5GroupDto(@"demo\yo-2step", "USD", 2, "Achiever", 100, 50, true),
                // accounts: 10002 demo\yo-2step ...
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10002, 601, 11, t0, 2320m, 2300m, 0.10m, -200m, -1m, 0));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10002, 602, 12, t0.AddHours(2), 2300m, 2275m, 0.20m, -500m, -2m, 0));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10002, 603, 13, t0.AddHours(4), 2275m, 2240m, 0.40m, -1400m, -4m, 0));
```

That book trips martingale + lot-escalation + negative net. `FromBaseline` sends it to **`RISK_BLOCKED`**, not a 95.5 SHADOW dest-PnL series. Live 8463-account Achiever scoring can still paint some `demo\yo-2step` login with `EarlyQualityScore == 95.5` from **source** features. That paint is still not dest PnL. Starwave deals-done scored **0** (caller).

### 5. Persist path does not turn 95.5 into dest PnL

```267:319:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW) { await _db.SaveChangesAsync(ct); return; }
        // latest destination_quotes row, then:
            var fill = engine.SimulateEntry(
                intent.Id.ToString(),
                trade.Direction,
                trade.MaxVolumeLots,
                trade.EntryVwap,
                quote,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(80));
```

Replay of **already-closed source** trades as OPEN vs one latest dest quote, 80 ms delay (no 0.05 overlay). Status `"SHADOW_ONLY"`. No exit. No `MarkToMarket`. No dest realized ledger. `REAL_COPY` false. No `35=D` in this unit or its caller.

---

## Profit implication

**None from treating 95.5 as dest edge.** Ranking demo `yo-2step` (or any of the 8463 scored Achiever books) by first-3 / `EarlyQualityScore` does **not** measure future Pepperstone / cTrader PnL. `destinationRealPnl` remaining **0** is the honest dest book. This file cannot open dest size, cannot realize dest PnL, and cannot convert FIX LoggedOn into profit. Copying a 95.5 name would be copying a **source-behavior** winner (or a heuristic that is not even first-3-only — the scorer uses **all** completed XAU). Expected dest profit from this number is **unmeasured**, not 95.5.

Do not enable `REAL_COPY` because a demo `yo-2step` row shows 95.5.

---

## Lower-loss implication

**This leaf cannot lose dest capital** (`SAFE_BY_ABSENCE` of send + unused MTM). Combined with caller-measured `REAL_COPY false`, SHADOW-all-demo, and dest real PnL **0**, Pepperstone is not at risk from this calculator.

**Loss-reduction if someone later copies 95.5:** believing first-3 / earlyScore is future dest PnL would **raise** loss risk — source-book winners (and the seed `yo-2step` martingale is a source **loser**) are not dest-proven. The lower-loss policy is: keep `REAL_COPY` false; do not promote on 95.5; do not treat `SourceVsShadowSlippage` as dest P&L; do not call `SimulateEntry` a dest ledger. `CanPromoteToLive => false` is the scoring pin. This file does not implement that pin; it also does not bypass it.

---

## Binding one-liner

`ShadowCopyEngine.cs` is dest-touch fill math. First-3 / `earlyScore` 95.5 on demo `yo-2step` is **source** quality (or a losing seed, not 95.5). It does **not** measure future destination PnL. `DestinationRealPnl` is **0**.
