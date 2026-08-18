# P500_CODE_4 — QuantityNormalizer vs shadow / dest real PnL

| Field | Value |
|---|---|
| Slot | **4** |
| File | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Angle | Is shadow PnL using real cTrader quotes or is `destinationRealPnl` still zero? |
| Date | 2026-08-18 |
| Agent | P500_CODE_4 (read-only; no product edit; no NewOrderSingle) |
| Product source modified | **No** |
| Test source modified | **No** |
| Live measured (operator) | 8463 accounts; Achiever scoring; Starwave deals-done scored 0; SHADOW all demo; `destinationRealPnl` 0; FIX LoggedOn; `REAL_COPY` false |
| Empty PASS | **Not used.** File was read in full (31 lines). Verdict is a measured FAIL of the live-quote / dest-PnL claim. |

---

## Verdict

**`DESTINATION_REAL_PNL_STILL_ZERO`. Shadow PnL is not marked to live cTrader quotes.**

`QuantityNormalizer` cannot answer the angle by itself: it never sees a quote, never computes PnL, and is unused on the shadow persist path. The dashboard field `destinationRealPnl` is a constructor literal `0`. Overview “Shadow P&L” is `Σ ShadowOrders.SourceVsShadowSlippage` against the **demo-seeded** `destination_quotes` book (`Bid=2399.45`, `Ask=2399.85`), not `ShadowCopyEngine.MarkToMarket` and not `CTraderQuoteService.LatestBid/Ask`. FIX LoggedOn does not write a live book. `REAL_COPY` stays false.

Classification: `MISSING` as a dest-PnL / live-quote engine. `EXISTS` only as a last-stage lot floor that product code never calls.

---

## File (SUT, read in full)

`D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` — 31 lines.

```1:31:D:\Prop\src\Domain\Execution\QuantityNormalizer.cs
namespace TraderIntelligence.Domain.Execution;

public sealed record InstrumentQuantitySpec(
    decimal MinQuantity,
    decimal MaxQuantity,
    decimal StepSize,
    int Precision);

public sealed class QuantityNormalizer
{
    public decimal Normalize(decimal sourceLots, decimal allocationFactor, InstrumentQuantitySpec dest)
    {
        if (sourceLots <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceLots));
        if (allocationFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(allocationFactor));
        if (dest.StepSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(dest.StepSize));

        var raw = sourceLots * allocationFactor;
        var steps = decimal.Truncate(raw / dest.StepSize);
        var qty = steps * dest.StepSize;
        qty = decimal.Round(qty, dest.Precision, MidpointRounding.ToZero);

        if (qty < dest.MinQuantity)
            return 0m;
        if (qty > dest.MaxQuantity)
            return dest.MaxQuantity;
        return qty;
    }
}
```

What this file is **not**:

| Capability | Present? |
|---|---|
| cTrader bid/ask input | **No** |
| `DestinationQuote` / `DestinationQuoteSnapshot` | **No** |
| `MarkToMarket` / realized dest PnL | **No** |
| `destinationRealPnl` | **No** |
| MT5 ticks / contract size / convention | **No** |
| Product callers | **Zero** (`new QuantityNormalizer` only under `tests/`) |

Skipped product test locks the unused fact:

```173:177:D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs
    [Fact(Skip = "A43 §6: shadow and live must call the same converter.")]
    public void Shadow_and_live_share_converter()
    {
        true.Should().BeFalse("QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine");
    }
```

Another skip: `"QuantityNormalizer has no quote/leverage"` (margin-room case). Shadow persist copies `trade.MaxVolumeLots` straight into `SimulateEntry` — no `Normalize`.

---

## Angle — evidence chain

### 1. `destinationRealPnl` is still zero (hardcoded)

DTO field exists:

```4:22:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public sealed record OverviewDto(
    int TotalAccounts,
    ...
    decimal ShadowPnl,
    decimal DestinationRealPnl,
    decimal XauGross,
    decimal XauNet,
    ...
    bool RealCopyEnabled);
```

Materializer never reads dest positions / dest fills. After summing slippage as `shadowPnl`, the next three args are literals:

```29:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        ...
            shadowPnl,
            0,
            0,
            0,
            ...
            _runtime.RealCopyEnabled);
```

Argument order: `ShadowPnl`, **`DestinationRealPnl = 0`**, `XauGross = 0`, `XauNet = 0`.

UI paints that zero honestly as a number, dishonestly as “Dest. real P&L”:

```26:27:D:\Prop\apps\web\src\pages\OverviewPage.tsx
        <MetricCard label="Shadow P&L" value={Number(data.shadowPnl).toFixed(2)} />
        <MetricCard label="Dest. real P&L" value={Number(data.destinationRealPnl).toFixed(2)} />
```

Trader-row `ShadowPnl` is a second literal `0` (`GetTradersAsync` constructor). Risk board is also zeros + `RealCopyEnabled=false` (not even `_runtime`):

```208:208:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), false, rejects);
```

Live operator reading **`destinationRealPnl = 0` on 8463 accounts is exactly this constructor**, not a measured empty dest book.

### 2. Shadow “PnL” is not mark-to-market and not a live cTrader quote

`ShadowCopyEngine.MarkToMarket` exists:

```85:90:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
```

Repo grep of product `*.cs` for `MarkToMarket`: **definition only**. Overview does not call it.

What the dashboard *does* sum is `ShadowOrder.SourceVsShadowSlippage` — entry (and only entry) slip vs source VWAP:

```50:59:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
        var slippage = direction == TradeDirection.Long ? raw - sourcePrice : sourcePrice - raw;
        return new ShadowFill
        {
            ...
            SourceVsShadowSlippage = slippage
        };
```

Persist path (`PersistDemoShadowAsync`): latest `destination_quotes` row, or **skip fills entirely** if none. Delay hardcoded **80 ms** (no 0.05 overlay). Quantity is **un-normalized** `trade.MaxVolumeLots`. Status `"SHADOW_ONLY"`.

```273:319:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        var quoteRow = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);
        if (quoteRow is null)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
        ...
            var fill = engine.SimulateEntry(
                intent.Id.ToString(),
                trade.Direction,
                trade.MaxVolumeLots,
                trade.EntryVwap,
                quote,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(80));
```

### 3. The only writer of `destination_quotes` is the demo seeder, not FIX QUOTE

```105:113:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.DestinationQuotes.Add(new DestinationQuoteSnapshot
        {
            Id = Guid.NewGuid(),
            CanonicalSymbol = "XAUUSD",
            VenueInstrumentId = null,
            Bid = 2399.45m,
            Ask = 2399.85m,
            ReceivedAt = now
        });
```

Grep of product `*.cs` for `DestinationQuotes.Add` / `new DestinationQuoteSnapshot`: **DemoSeeder only**.

`CTraderQuoteService` keeps in-memory `_latestBid/_latestAsk` from a harness snapshot (tags 1320/1321). It is **never constructed in DI**. `CTraderFixLogonHostedService` logs on QUOTE/TRADE, persists **session status**, then **pins** `RealCopyEnabled = false`. It does not send `35=V`, does not call `TryAcceptMarketDataSnapshot`, does not insert `DestinationQuoteSnapshot`.

```60:68:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        ...
        _runtime.RealCopyEnabled = false;
```

```41:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
```

Therefore **FIX LoggedOn (measured live) ≠ live dest book**. `QuoteHealthy` is true when session status is `LoggedOn` **or** `_runtime.Quote.LoggedOn` — health is session-on, not “we have a fresh XAU bid/ask in `destination_quotes`.”

### 4. Live facts vs this tree

| Measured live | Code truth |
|---|---|
| 8463 accounts | Overview `TotalAccounts` = `Mt5Accounts.Count` — can be live Achiever catalog. Irrelevant to dest PnL. |
| Achiever scoring | Reconstruction + `BaselineScorer` on source deals. Not dest. |
| Starwave deals-done scored 0 | Scorer never ran / no completed XAU — still no dest PnL path. |
| SHADOW all demo | `PersistDemoShadowAsync` + seed book; intents `SHADOW_ONLY`. |
| `destinationRealPnl` 0 | Literal `0` in `GetOverviewAsync`. Cannot become non-zero without a code change. |
| FIX LoggedOn | Logon host can persist `LoggedOn`. Does not persist quotes. |
| `REAL_COPY` false | DI pin + logon pin + worker default. No `35=D` builder in this slot’s path. |

---

## Profit implication

- There is **no real destination profit** in this pipeline. `destinationRealPnl` cannot leave `0` while `GetOverviewAsync` passes a literal and no dest-position ledger is queried.
- Painted **Shadow P&L** is **source-vs-seed-slippage**, not copy P&amp;L. Against `2399.45 / 2399.85` it will look like a dollar P&amp;L if an operator treats the tile as MTM. That is a **label lie**, not a cTrader result.
- `QuantityNormalizer` is **off the shadow path**. Even a future live book would size `MaxVolumeLots` 1:1 (A43 G7: `0.10` lots would stay `0.10` instead of `10.00` BaseUnits). A later enable of copy would **under-size / mis-size**, not harvest the measured Achiever edge.
- 8463 live accounts + FIX LoggedOn do **not** create dest alpha. Starwave scored 0 contributes **zero** shadow rows from that broker.

Do **not** size risk or celebrate “copy working” from Overview Shadow P&amp;L.

---

## Lower-loss implication

- **Risk to capital today: NONE** from this file and this angle. `REAL_COPY` is pinned false; logon host re-pins false; no NOS builder consumes `Normalize`; dest real P&amp;L cannot go negative because it cannot change.
- `Normalize` returning `0m` below dest min is fail-closed **if wired**. It is not wired — so it does not protect live size either.
- The **loss mode** is **decision loss**, not cash: LoggedOn + non-zero Shadow P&amp;L + 8463 accounts can be misread as “book is live; copy is earning.” That is the path that would later flip `REAL_COPY` against a **stale seed book** (`VenueInstrumentId = null`) and unconverted lots.
- Keep `REAL_COPY` false until: (1) QUOTE MD actually **upserts** `destination_quotes`, (2) shadow MTM uses that book + `MarkToMarket` (not slippage Σ), (3) `destinationRealPnl` is a **query** of dest fills/positions, (4) shadow and live share a real `IQuantityConverter` (this class is only the last floor).

---

## Adjacent files read (no edit)

| Path | Role in verdict |
|---|---|
| `src\Domain\Execution\QuantityNormalizer.cs` | SUT — no quotes, no PnL |
| `src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` field |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `0` dest PnL; slippage as ShadowPnl |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | shadow fill from latest dest quote or skip |
| `src\Domain\Shadow\ShadowCopyEngine.cs` | unused `MarkToMarket`; slippage fill |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | only dest-quote writer; 2399.45/2399.85 |
| `src\Fix.CTrader\Services\CTraderQuoteService.cs` | in-memory book; not in DI |
| `src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | LoggedOn without quote persist; `RealCopyEnabled=false` |
| `src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false` |
| `src\Application\Ingestion\DealIngestionService.cs` | always calls `PersistDemoShadowAsync` after score |
| `apps\web\src\pages\OverviewPage.tsx` | paints dest real P&L from API |
| `tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | unused-by-shadow skip |

---

## Binding one-liner

**Slot 4 / `QuantityNormalizer.cs`: not a PnL object. Shadow is demo-seed bid/ask slippage. `destinationRealPnl` is still the literal `0`. FIX LoggedOn + 8463 accounts do not make dest P&amp;L real. `REAL_COPY` false → no capital at risk; do not treat Shadow P&amp;L as profit.**
