# D16 — `ShadowCopyEngine` file review

| Field | Value |
|---|---|
| Agent | D16 (senior engineer; file-level review of the shadow simulator) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\D16_shadow.md` |
| Product source modified | **No.** This report is the only write. |
| SUT | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` |
| Lines / bytes | **91** lines / **3249** bytes |
| SHA-256 | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` |
| LastWriteTimeUtc | 2026-08-18T07:38:10Z |
| Namespace | `TraderIntelligence.Domain.Shadow` |
| Binding law | Architecture **§24 / §31 / §36–§38 / §63–§64**; `A24_shadow_copy_spec.md`; `A72_quote_guards.md`; `A73_copy_latency.md` |
| Prior reviews (do not treat as this file) | B18 dest-quote slice; A57 / C13 item 11; C14 G14 |
| Scope | The 91-line file, its types, and measured call/persist/test surface. No product edit. |

---

## 0. Verdict (honest, measured)

**Classification: `EXISTS_NEEDS_REFACTOR` as a pure taker-touch calculator. `MISSING` as Architecture §24 / A24 Phase-5 shadow copy.**

The file compiles. It is three public methods plus two records. On an untouched, well-formed book it **does** take the destination bid or ask (not mid, not the source print). That is the only §24 sentence it satisfies.

It then violates the rest of the shadow law:

| Required (A24 / §24) | Measured in this file |
|---|---|
| Fill price = persisted dest QUOTE bid/ask | **Broken** when `modeledDelay > 250ms`: fill is dest touch **± 0.05 price units** |
| Fail closed on missing / unusable / stale / wide dest book | **Missing.** Always returns a `ShadowFill` |
| Post-delay **re-read** of dest quote | **Missing.** One caller-supplied snapshot; delay only flips a hardcoded overlay |
| OPEN vs CLOSE policy | **Missing.** Two methods, no `action_class`, no reject path |
| CLOSE waterfall / `UNPRICED` | **Missing.** `SimulateExit` always invents `Price` |
| Conservative MTM + quality; unpriced ≠ 0 | **Partial** touch (bid/ask). Always a `decimal`. No quality. |
| Persist order / fill / position / pnl / slippage | **Not this type.** `ShadowPosition` is a dead record. Engine never writes. |
| Destination cost model | **Missing** |
| Quantity normalization | **Missing.** Qty is echoed. `QuantityNormalizer` is unused (test skip says so). |
| Wired pipeline | **Missing.** Zero product callers. Not in DI. No unit tests. |

Do **not** claim §24, A24, §69 item 11, or §68 “shadow copy has sufficient sample” / “stale quote rejection works” from this class. A function that will fill a crossed book, a 30-second-old book, a zero book, or a book it just mutated by five cents is not a destination-quote engine.

A57’s “no dest quotes” is **stale on types** (`DestinationQuote` + `DestinationQuoteSnapshot` exist) and **still true on the pipeline** (nobody reads `destination_quotes` into this engine).

---

## 1. Method

Read, did not edit:

| Path | SHA-256 / note | Why |
|---|---|---|
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | SUT |
| `D:\Prop\src\Domain\Entities\ShadowOrder.cs` | `8EF2D2372CFC01A27CBCA4A1855A322B54A4439FCB6B11AA3A5404FD0D1F8B86` | Persist shape |
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | Durable snapshot the engine does **not** take |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | `DestinationQuote` record the engine **does** take; OPEN guards it does **not** call |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | `336123499E347CAF355D483C77F4E724661A90D95206B28B814DCB3E0EA628E5` | Intent the engine never sees |
| `D:\Prop\src\Domain\Enums\TradeDirection.cs` | Long=0, Short=1 | Side enum |
| `D:\Prop\src\Domain\Enums\CopyIntentAction.cs` | Open/Increase/Reduce/Close | Not a parameter |
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | unused by SUT | A24 §9 |
| `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` | unused by SUT | A24 §6 |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` L27, L143–153 | `shadow_orders` PK-only; `destination_quotes` PK-only | No fill/position/pnl tables |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L21 | sums `SourceVsShadowSlippage` as “Shadow P&L” | Wrong grain even if rows existed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L103–111 | seeded dest quote, `VenueInstrumentId = null` | Unusable under A24 §2.2 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | registers reconstructor + scorer; **not** this engine | Dead path |
| `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx` | static copy | No book |
| `D:\Prop\tests\Unit\` | **no** `ShadowCopy*` tests | One skip names this SUT as unused |
| A24, A72, A73, A20, A27, A57, A89 | binding / inventory | Law |

Repo grep of product `*.cs` for `ShadowCopyEngine` / `SimulateEntry` / `SimulateExit` / `MarkToMarket` / `ShadowFill` / `DefaultLatencySlippagePoints`: **definitions only** (this file). Application, workers, API, DI, seeder, and tests do not construct the engine.

`ShadowCopyIntent` exists only as `OutboxEventType = 2`. No writer emits it.

---

## 2. File fingerprint

```text
Path:   D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
Bytes:  3249
Lines:  91 (including usings + blank lines)
Hash:   SHA256 F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9
UTC:    2026-08-18T07:38:10Z
TFM:    net8.0 (SDK-style Domain project; file is included by glob)
```

Usings (both actually used):

- `TraderIntelligence.Domain.Enums` → `TradeDirection`
- `TraderIntelligence.Domain.Risk` → `DestinationQuote` **record**, not `Entities.DestinationQuoteSnapshot`

Public surface:

| Symbol | Kind | Role |
|---|---|---|
| `ShadowFill` | `sealed record` | Return of entry/exit. 7 required fields. |
| `ShadowPosition` | `sealed record` | Declared in this file. **Never constructed. Never returned.** Dead API. |
| `ShadowCopyEngine` | `sealed class` | Stateless. Implicit public ctor. No options, no clock, no store. |
| `DefaultLatencySlippagePoints` | `const decimal = 0.05m` | Hidden fill fudge. Named “Points”. Applied as **raw dest price units**. |
| `SimulateEntry` | method | Always returns `ShadowFill`. |
| `SimulateExit` | method | Always returns `ShadowFill`. No delay parameter. |
| `MarkToMarket` | method | Always returns `decimal`. |

No interface. No reject/result type. No `model_version`. No `fill_quality`. No quote id.

---

## 3. Line-by-line (what the source actually does)

### 3.1 `ShadowFill` (L6–15)

```6:15:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
public sealed record ShadowFill
{
    public required string ShadowOrderId { get; init; }
    public required decimal Price { get; init; }
    public required decimal Quantity { get; init; }
    public required DateTimeOffset FilledAt { get; init; }
    public required decimal Spread { get; init; }
    public required TimeSpan QuoteAge { get; init; }
    public required decimal SourceVsShadowSlippage { get; init; }
}
```

Exact members:

| Field | Type | Written from |
|---|---|---|
| `ShadowOrderId` | `string` | caller arg (not `Guid`) |
| `Price` | `decimal` | possibly **mutated** dest touch |
| `Quantity` | `decimal` | caller arg, unchecked |
| `FilledAt` | `DateTimeOffset` | caller `now` (delay is **not** added) |
| `Spread` | `decimal` | `quote.Ask - quote.Bid` (can be ≤ 0) |
| `QuoteAge` | `TimeSpan` | `now - quote.ReceivedAt` (can be negative) |
| `SourceVsShadowSlippage` | `decimal` | signed vs source print, on **mutated** `raw` |

Missing vs A24 §10.2 / §10.5: `fill_quote_id`, dest bid, dest ask, `fill_quality` (`LIVE` \| `STALE_QUOTE`), `quote_age_ms` (int), `model_version`, `liquidity=TAKER`, commission, `assumption_notes`, `fill_seq`.

`ShadowOrderId` is a `string`. Persist entity `ShadowOrder.Id` is a `Guid`. There is no conversion in this file.

### 3.2 `ShadowPosition` (L17–29) — dead

```17:29:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
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

Grep of product `*.cs`: this record is **only** declared. The engine has no `Open` / `ScaleIn` / `Reduce` / `Close` / `Mark` method that returns it.

Type collisions with persist / spec:

| Field here | Persist / spec |
|---|---|
| `BrokerId` **string** | `ShadowOrder.BrokerId` **Guid**; A24 `source_broker_id` text |
| `SourceTradeId` **string** | `CopyIntent.SourceTradeId` **Guid?** |
| `Open` bool | A24 status `OPEN` \| `PARTIAL` \| `CLOSED` \| `SOURCE_CLOSED_UNPRICED` \| `SOURCE_REDUCED_UNPRICED` |
| `UnrealizedPnl` required `decimal` | A24: `null` when unpriced. **Zero is a lie.** |
| no `entry_vwap` / `exit_vwap` / `closed_qty` / `max_qty` | A24 §10.3 |

This is a sketch of a book, not a book.

### 3.3 `SimulateEntry` (L35–61)

```35:61:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
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

Control flow, in order:

1. Pick dest **ask** if long, dest **bid** if short. Correct taker map for OPEN/INCREASE (A24 §11.1).
2. If `modeledDelay > 250ms` (strict greater; **250ms exact does not overlay**), add `+0.05` (long) or `-0.05` (short) to that touch.
3. Signed slippage vs `sourcePrice` on the **possibly mutated** number. Formula matches A24 §11.4 **measurement** (buy: fill − source; sell: source − fill). After step 2 it is no longer a measurement of the dest tape.
4. Stamp `FilledAt = now`. `modeledDelay` is **not** added to the clock.
5. Stamp `Spread` and `QuoteAge` as decoration. Never compared to a limit.
6. Return. No reject. No second quote. No action class.

What the method **does not** read on `DestinationQuote`:

| Field | Used? |
|---|---|
| `Bid` / `Ask` | yes (as touch + spread) |
| `ReceivedAt` | yes (age telemetry only) |
| `VenueTimestamp` | **no** |
| `CanonicalSymbol` | **no** — EURUSD book can price an XAU fill |
| `VenueInstrumentId` | **no** — null / unmapped accepted |

`DestinationQuote` has no `Id`. The fill cannot name the snapshot.

What the method **does not** take: `action_class`, decision quote vs fill quote, `expires_at`, signal age, min/step qty, cost model, `SHADOW_COPY_ENABLED`, kill analog, linked position.

### 3.4 `SimulateExit` (L63–83)

```63:83:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
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
        return new ShadowFill { /* same seven fields */ };
    }
```

Correct taker map for REDUCE/CLOSE (long sells bid; short buys ask).

Then:

- **No** `modeledDelay`. Entry and exit are not the same dest-quote contract.
- **No** CLOSE waterfall (A24 §8.3). A 3-hour-old seed quote still produces `Price`.
- **No** “no linked shadow position” check (A24 §8.1). The method cannot see a book.
- Slippage polarity is consistent with §11.4 for a sell-to-close (long) / buy-to-close (short).

### 3.5 `MarkToMarket` (L85–90)

```85:90:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
```

Algebra:

```text
long  = (bid - entry) * qty
short = (ask - entry) * (-1) * qty  = (entry - ask) * qty
```

That **is** A24 §12 conservative dest-touch, with `dest_contract_value` implicit **1.0** and **no** accrued dest costs.

It cannot say UNPRICED. `qty=0`, `px==entry`, missing book (`bid=0`), and a real flat mark all return `0m`. Zero is a lie when the dest book is dead.

No `mark_quote_id`. No `mark_quality`. No age test vs `MAX_QUOTE_AGE_MARK_MS`.

---

## 4. Worked examples (arithmetic of this file, not a hoped-for spec)

Seeded demo book (`DemoSeeder` L103–111): `Bid = 2399.45`, `Ask = 2399.85`. Use `sourcePrice = 2399.50`, `qty = 1`.

| Case | Inputs | `Price` | `Spread` | `SourceVsShadowSlippage` |
|---|---|---|---|---|
| Long entry, delay 100 ms | ask, no overlay | **2399.85** | 0.40 | 2399.85 − 2399.50 = **0.35** |
| Long entry, delay **250 ms** | `>` is false | **2399.85** | 0.40 | **0.35** |
| Long entry, delay 251 ms | ask + 0.05 | **2399.90** | 0.40 | **0.40** |
| Short entry, delay 251 ms | bid − 0.05 | **2399.40** | 0.40 | 2399.50 − 2399.40 = **0.10** |
| Long exit | bid | **2399.45** | 0.40 | sourceExit − 2399.45 |
| Short exit | ask | **2399.85** | 0.40 | 2399.85 − sourceExit |
| Long MTM, entry 2399.85 | bid | n/a | n/a | unrealized = (2399.45 − 2399.85) × 1 = **−0.40** |
| Short MTM, entry 2399.45 | ask | n/a | n/a | unrealized = (2399.45 − 2399.85) × 1 = **−0.40** |

The 251 ms long fill is **not** a dest ask. It is dest ask plus five **cents of USD/oz**, not 0.05 tick, not a measured Pepperstone tick size. A24 §11.4: optional `shadow_adverse_ticks` **default 0**, only if enabled **and versioned**. This file hides `0.05` in `Price` with no `model_version`.

Adversarial books the file will still “fill”:

| Book | What happens |
|---|---|
| `Bid=0`, `Ask=0` | Long entry `Price=0` (or `0.05` after delay). MTM can look like a huge loss vs a 2399 entry. |
| Crossed `Bid=2400`, `Ask=2399` | `Spread = -1`. Still a fill. |
| `now < ReceivedAt` | `QuoteAge` negative. Still a fill. |
| `CanonicalSymbol = "EURUSD"` | Ignored. Prices an XAU shadow. |
| `quantity = 0` or negative | Echoed onto the fill. No `QTY_BELOW_MIN`. |
| `quote` cannot be null | Caller must fabricate a book to call the method. A24 forbids inventing fills when no dest quote has ever existed. |

250 ms threshold is a compile-time magic number. It is not `SHADOW_EXECUTION_DELAY_MS`. It is not bound to `RiskLimits.MaxQuoteAge` (3 s) or `CTraderFixOptions.MaxQuoteAgeMs` (5000). Shadow adds a **third** clock that is not an age clock at all — it is an overlay trigger.

---

## 5. Signature is not a shadow lifecycle

A24 §11.2 required sequence:

```text
Persist order PENDING_PERSISTED with decision quote
sim_send_at = now
Wait delay  (replay: advance synthetic clock; do not sleep in tests)
Read latest dest quote with quote_received_at >= sim_send_at (else latest)
Apply post-delay OPEN guards or CLOSE waterfall
Fill
```

`SimulateEntry` takes **one** `DestinationQuote`, **one** `now`, and a `modeledDelay` used only for the overlay. It cannot:

- distinguish decision quote vs fill quote,
- require `ReceivedAt >= sim_send_at`,
- persist `decision_quote_id` / `fill_quote_id`,
- fail OPEN after delay,
- advance `FilledAt` by the delay (unless the caller already did that in `now`, which is undocumented).

If a later caller passes the **same** snapshot as both decision and fill, that is the A24 §7.4 defect (OPEN fallback to decision-time book). The signature invites that bug.

`SimulateExit` does not even take delay. Entry and exit clocks are different types.

`now` as a parameter is the one design point worth keeping: the engine is a **pure function of its arguments**. Replay does not need `DateTimeOffset.UtcNow` inside this class. That does not make the policy correct.

---

## 6. Persistence / dashboard / DI (measured around the file)

### 6.1 What exists

`TraderDbContext` maps `ShadowOrder` → table `shadow_orders` with **only** PK `Id`. No unique `(source_broker, login, source_event, action_class)`. No `cl_ord_id`. No `decision_quote_id`. No status machine.

`ShadowOrder` fields: `CopyIntentId`, `BrokerId`, `SourceLogin`, `Direction`, `Quantity`, `Price`, `Spread`, `SourceVsShadowSlippage`, `FilledAt`. It **drops** `QuoteAge`. It has no quote FK.

A20 / A24 required tables **not** on the DbContext:

| Spec table | On disk |
|---|---|
| `shadow_orders` | yes, thin |
| `shadow_fills` | **no** |
| `shadow_positions` | **no** |
| `shadow_pnl` / `shadow_performance` | **no** |
| `source_vs_shadow_slippage` | **no** (one column on the order row) |

`destination_quotes` exists as latest-only PK table. No UK `(venue_id, instrument_id)`. No history. Fills cannot FK “the snapshot as used.”

### 6.2 Dashboard lie (adjacent, but this file’s number)

```21:21:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

`SourceVsShadowSlippage` is **not** P&L. It is dest-vs-source print difference (and after 251 ms it includes the 0.05 lie). Overview `ShadowPnl` and trader `ShadowPnl` (hardcoded `0` on trader rows) are not A24 `shadow_performance`. Empty table → `0`, which looks like a flat book.

Web `ShadowPortfolioPage` is a static paragraph. It claims shadow orders appear after an approved CopyIntent. **No product writer creates `CopyIntent` or `ShadowOrder`.**

### 6.3 DI

`AddTraderIntelligence` registers `TradeReconstructor` and `BaselineScorer` as singletons. **`ShadowCopyEngine` is not registered.** Even a perfect function would not run.

---

## 7. Finding list

Severity: **BLOCK** = A24 acceptance fail if this shipped as shadow pricing. **HIGH** = silent wrong dest economics. **MED** = audit / replay hole.

### F1 — BLOCK — Fill is not the destination quote after 250 ms

`DefaultLatencySlippagePoints = 0.05m` added to dest touch when `modeledDelay > 250ms`. Named “Points”, applied as XAU USD/oz. Threshold and size are compile-time. Not `SHADOW_ADVERSE_TICKS=0`. Not versioned. Slippage is then computed on the mutated number, so the audit field mixes dest tape + hidden model.

This is the opposite of “destination quotes only.”

### F2 — BLOCK — Unusable / missing books still fill

No check for `bid<=0`, `ask<=0`, `ask<bid`, unmapped instrument, wrong symbol, or null quote. `DestinationQuote` is required, so “no dest quote exists” is inexpressible without fabricating a book. A24 §1.3 forbids inventing fills in that state.

### F3 — BLOCK — OPEN never fails closed

Age and spread are recorded, never blocking. No `QUOTE_STALE` / `SPREAD_TOO_WIDE` / `PRICE_MOVED_TOO_FAR` / `QUOTE_UNAVAILABLE` / `SIGNAL_TOO_STALE`. `RiskEngine` already has three of those codes for `IsIncreasing` and this class does not call it. Even if it did, Risk’s move test is unsigned mid (A72 forbids that as the default). Shadow cannot inherit that as a substitute.

No `action_class`. OPEN vs INCREASE vs REDUCE vs CLOSE is not an input.

### F4 — BLOCK — CLOSE invents a dest price

A24 §8.3: LIVE → STALE_QUOTE → **no fill**, `SOURCE_CLOSED_UNPRICED`. `SimulateExit` is path (1) with no age test and no quality enum. Always a `Price`.

### F5 — BLOCK — Post-delay re-read is not implemented

Delay does not select a later snapshot. It does not require `ReceivedAt >= sim_send_at`. It does not change `FilledAt`. It only flips F1.

### F6 — HIGH — MTM cannot be unpriced

Touch polarity is correct. Return type `decimal` cannot be null. No `mark_quality`. Implicit contract value 1.0. Zero is overloaded.

### F7 — HIGH — Two quote types; engine bound to the one that cannot persist

| Type | Path | Engine? | Id |
|---|---|---|---|
| `DestinationQuote` record | `RiskEngine.cs` L24–30 | **parameter** | no |
| `DestinationQuoteSnapshot` | `Entities\DestinationQuote.cs` | **no** | yes (`Id`) |

A fill cannot store `fill_quote_id` from the type it prices.

### F8 — HIGH — Identity / clocks ignored

`CanonicalSymbol`, `VenueInstrumentId`, `VenueTimestamp` unused. `QuoteAge` can be negative. `PriceSource.CTraderQuoteSession` never stamped. Replay can silently mix tapes (A24 §14, A45).

### F9 — HIGH — Quantity is a pass-through

A24 §9 / §38: never `source lots == dest OrderQty`. `QuantityNormalizer` exists and is unused. `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` has an explicit skip: *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”*. Negative / zero qty still fills.

### F10 — HIGH — No book, no lifecycle

`ShadowPosition` is dead. No VWAP, no scale-in, no partial close, no reversal = CLOSE then OPEN, no catch-up rules, no `NO_SHADOW_POSITION`, no dest commission/swap, no `SINGLE_FILL_FULL_QTY` label. §24’s persist list is not produced by this type.

### F11 — MED — No dest-quote / shadow tests

Zero `ShadowCopy*` facts. A24 §19.12 and A72 §11 that this SUT must prove, and does not:

| Required proof | Exists? |
|---|---|
| Open reject stale dest quote | no |
| Open reject `bid<=0` / crossed book | no |
| Open reject wide dest spread | no |
| Favorable dest ask improve still fills; adverse jump rejects | no |
| Decision-time pass, post-delay dest book stale → **no fill** | no |
| Close still fills on dest quote older than OPEN max, quality `STALE_QUOTE` | no |
| No dest quote on close of existing shadow → no fill, `UNPRICED` | no |
| Fill price == persisted snapshot bid or ask (no 0.05 unless versioned) | no |
| Delay **250 ms exact** does not overlay; 251 ms does (current bug lock) | no |
| Replay recorded dest tape, not source last-deal (`Replay.ShadowCopyFromReplayTests`) | no |

A89’s 92-class unit backlog **does not even name** a `ShadowCopyEngineTests` class (scope excluded shadow). A27 still requires `Replay.ShadowCopyFromReplayTests`. §68 “stale quote rejection works” remains **false** for shadow.

### F12 — MED — Dead path (explains why F1–F10 have not yet corrupted a book)

No Application / worker / outbox consumer constructs `ShadowCopyEngine`. QUOTE FIX worker is a heartbeat stub. `CopyIntent` is unused. There is **no shadow book**. That is not an excuse for F1–F11. It is why the stub is still safe-by-absence.

---

## 8. A24 §19 acceptance vs this file

| # | A24 §19 | Status on this SUT |
|---|---|---|
| 1 | Every fill from persisted dest QUOTE bid/ask | **FAIL** — no persist id; ±0.05 overlay; always fills |
| 2 | Durable order / fill / position / pnl / slippage | **FAIL** — not this type; persist is a thin `shadow_orders` row nobody writes |
| 3 | OPEN reject stale / wide / move / QUOTE down / catch-up | **FAIL** — records age/spread, never rejects |
| 4 | CLOSE waterfall; never invent a tick | **FAIL** — always invents `Price` |
| 5 | Outage open+close, no prior shadow → zero positions | **FAIL** — no book / no catch-up logic |
| 6 | Source close after shadow open flattens or `UNPRICED` | **FAIL** |
| 7 | Reversal = CLOSE then OPEN; rejected OPEN leaves flat | **FAIL** |
| 8 | Qty normalized; 0.10 source ≠ 0.10 dest | **FAIL** — qty echoed |
| 9 | Dest commission/swap versioned; source costs not copied | **FAIL** — no cost model |
| 10 | Conservative MTM dest bid/ask; null ≠ 0 when unpriced | **FAIL** — touch correct, quality/null missing |
| 11 | No TRADE `NewOrderSingle` | **PASS** (engine cannot send; unused; `REAL_COPY_EXECUTION_ENABLED` default false elsewhere) |
| 12 | Unit list (stale quote/signal, stale close fill, unpriced hold, no catch-up open, partial VWAP, reversal, sizing, idempotent replay) | **FAIL** — no tests |
| 13 | Replay recorded quotes + MT5 events → golden fills/P&L | **FAIL** |

**Measured score for this file as Phase 5 / §69.11: 1/13 (item 11 only, by inability to send).** Do not round that up.

---

## 9. What is correct (do not “fix” these into mid or source fills)

These dest-touch rules are right on the current source and must stay:

- Long open / short close buy the **ask**.
- Short open / long close sell the **bid**.
- Long mark = bid; short mark = ask. Mid is not the fill.
- Source deal price is informational slippage only (**until F1’s overlay poisons `raw`**).
- Engine does not send FIX TRADE.
- `now` is injected (pure function) — keep that for replay.

Those six lines are necessary and nowhere near sufficient.

---

## 10. What must change (implementation later; not this artifact)

When someone implements dest-quote shadow pricing, A24 is already the contract. Do not invent a second one.

1. **Price authority.** `Price` is exactly `quote.Ask` or `quote.Bid` from a snapshot that has an `Id`. Adverse ticks are a separate, versioned, default-**0** addend. Delete or gate `DefaultLatencySlippagePoints` / `250ms`.
2. **Usability first.** `bid<=0` / `ask<=0` / `ask<bid` / unmapped / null quote → no `ShadowFill`. OPEN: `QUOTE_INVALID` / `QUOTE_UNAVAILABLE`. CLOSE: waterfall step 3, not a dummy px.
3. **Accept `DestinationQuote?` plus `action_class`.** One `max_quote_age` for open and close is a defect.
4. **Post-delay re-read.** Fill snapshot with `ReceivedAt >= sim_send_at` when one exists. OPEN repeats A72 on **that** snapshot. No silent reuse of the decision quote.
5. **One options object** (`MAX_QUOTE_AGE_OPEN_MS`, `MAX_QUOTE_AGE_CLOSE_MS`, `MAX_QUOTE_AGE_CLOSE_STALE_FALLBACK_MS`, `MAX_QUOTE_AGE_MARK_MS`, `MAX_SPREAD_OPEN`, `MAX_ADVERSE_MOVE_OPEN`, `SHADOW_EXECUTION_DELAY_MS`, `SHADOW_ADVERSE_TICKS`). Do not add a fourth hardcoded age next to Risk 3s and FIX 5000ms.
6. **Return quality, not just px.** `LIVE` / `STALE_QUOTE` / `UNPRICED`. MTM returns `decimal?` + mark snapshot. Persist `fill_quote_id` / `mark_quote_id`.
7. **Unify types.** Map `DestinationQuoteSnapshot` → Risk `DestinationQuote` **with Id**. Require venue + instrument id.
8. **Book methods.** Construct and persist A24 `shadow_position` (VWAP, partials, reversal split). Delete or replace the dead `ShadowPosition` record so it cannot be mistaken for the book.
9. **Call `QuantityNormalizer`** before fill. Reject OPEN at 0. Flatten remainder on CLOSE below min.
10. **Tests** in §7 F11, with recorded XAU-like books (A72: e.g. 2399.50 / 2399.66). No live Pepperstone.

Do not implement dest-quote history in `destination_quotes` (A20: latest-only). Do not use this engine’s quote as source MFE/MAE (A45). Do not treat FIX “logged on” as a dest quote (A25 / A72). Do not sum slippage and call it Shadow P&L.

---

## 11. Scoreboard this file does **not** move

| Gate | Still |
|---|---|
| §69.11 shadow-copy selected traders on dest quotes (A57 / C13) | **No / FAIL** |
| §68 shadow copy has sufficient sample (C14 G14) | **FAIL** (no book) |
| §68 stale quote rejection works | **FAIL** for shadow |
| Phase 5 deliverable (intents, entries/exits, dest pricing, shadow P&L, source-vs-shadow) | **not delivered** |
| SAFE_BY_ABSENCE on live `NewOrderSingle` | **holds** (this class cannot send; unused) |

---

## 12. Traceability

| Topic | Law |
|---|---|
| Dest QUOTE is shadow price authority | Architecture §24, §31; A24 §1.4, §11.1 |
| Persist the exact snapshot used | A24 §2.5, §10.2; A20 `destination_quotes` |
| Usable book | A24 §2.2; A72 §3.2 |
| OPEN dest-quote guards + post-delay re-check | A24 §7; A72 §4, §6 moment 2 |
| CLOSE dest-quote waterfall | A24 §8.3; A72 §5.1 |
| Conservative dest MTM; unpriced ≠ 0 | A24 §12 |
| No hidden dest-price fudge | A24 §11.4, §17 `SHADOW_ADVERSE_TICKS=0` |
| Delay is a clock + re-read, not an overlay | A24 §11.2; A73 shadow analogs |
| Sizing | A24 §9; A43; `QuantityNormalizer` |
| Dual clock | A72 §3.3 |
| One configured max age | A72 §7.3 |
| §69 item 11 | A57 / C13 — still **No** |
| Tests | A24 §19.12; A27 `Replay.ShadowCopyFromReplayTests`; A72 §11 |

---

## 13. One-line law

```text
A shadow fill price is a persisted destination bid or ask, or it is not a shadow fill.

ShadowCopyEngine today: dest touch ± 0.05 after 250ms, any book, any age, always a number,
qty echoed, no book, no callers, no tests.
That is not Architecture §24.
```

*End of D16. Product source was not modified.*
