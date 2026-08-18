# B18 — ShadowCopyEngine destination-quote review

| Field | Value |
|---|---|
| Agent | B18 (senior engineer; destination quotes only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\B18_shadow_review.md` |
| Product source modified | **No.** This report is the only write. |
| SUT | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` (91 lines, 3249 bytes) |
| SHA-256 | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` |
| LastWriteTimeUtc | 2026-08-18T07:38:10Z |
| Quote DTO | `TraderIntelligence.Domain.Risk.DestinationQuote` in `D:\Prop\src\Domain\Risk\RiskEngine.cs` (SHA-256 `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D`) |
| Quote entity | `DestinationQuoteSnapshot` in `D:\Prop\src\Domain\Entities\DestinationQuote.cs` (SHA-256 `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726`) |
| Binding law | Architecture §24 / §31; `A24_shadow_copy_spec.md`; `A72_quote_guards.md` |
| Scope | **Destination quotes only.** Sizing, reconstruction, FIX TRADE, commission/swap, and live send are out of scope except where they collide with dest-quote pricing. |

---

## 0. Verdict (honest, measured)

**Classification: `EXISTS_NEEDS_REFACTOR` as a taker-touch calculator. `MISSING` as a destination-quote engine.**

`ShadowCopyEngine` is the only type that prices a shadow fill. On the happy path it **does** take bid/ask from a `DestinationQuote` (not the source deal, not mid). That is the one thing it gets right.

It then **fails every other destination-quote law** A24 / A72 / §24 / §31 require:

| Law | Measured |
|---|---|
| Fill = persisted dest QUOTE snapshot (A24 §11.1, §19.1) | **Broken** when `modeledDelay > 250ms`: fill is dest touch ± hardcoded `0.05` |
| Fail closed on unusable / missing quote (A24 §2.2, §7.1) | **Missing.** Quote is required, never validated, never nullable |
| OPEN reject `QUOTE_STALE` / `SPREAD_TOO_WIDE` / `PRICE_MOVED_TOO_FAR` (A24 §7, A72 §6 moment 2) | **Missing.** Age and spread are recorded, never blocking |
| Post-delay **re-read** dest quote (A24 §7.4, §11.2) | **Missing.** Caller-supplied snapshot is used as-is |
| CLOSE pricing waterfall `LIVE` / `STALE_QUOTE` / `UNPRICED` (A24 §8.3) | **Missing.** `SimulateExit` always invents a fill |
| Conservative MTM + `mark_quality`; unpriced ≠ 0 (A24 §12) | **Partial** touch (bid/ask). Always a number. No quality. Invalid book → a number |
| Store `fill_quote_id` / dest bid/ask on the fill (A24 §2.5, §10.2, §10.5) | **Missing.** `ShadowFill` keeps Price / Spread / QuoteAge only |
| Dual clock `ReceivedAt` + sane `VenueTimestamp` (A72 §3.3) | **Missing.** Receive clock only; venue ts ignored |
| Wired to `destination_quotes` | **Missing.** Zero callers. Two incompatible quote types. |

Do **not** claim §24, A24, or §69 item 11 (“shadow-copy selected traders using destination quotes”) are implemented. A method that will fill a crossed book, a 30-second-old book, or a book it just mutated by 5 cents is not destination-quote authority.

A57’s “no dest quotes” is **stale on types** (the DTO + snapshot exist) and **still true on the pipeline** (nobody reads `destination_quotes` into this engine).

---

## 1. Method

Read, did not edit:

| Path | Why |
|---|---|
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | SUT |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` lines 24–30, 95–111 | `DestinationQuote` record + OPEN quote guards the engine does **not** call |
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | Durable snapshot type the engine does **not** take |
| `D:\Prop\src\Domain\Entities\ShadowOrder.cs` | Persist shape: Spread + slippage, **no** quote id / age |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` lines 28, 149–153 | `destination_quotes` latest-only table, no history, no UK |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` lines 103–111 | Seeded dest quote (null instrument id) |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` lines 128–146 | Latest snapshot used for FIX card only |
| `D:\Prop\src\Domain\Enums\PriceSource.cs` | `CTraderQuoteSession` unused by shadow |
| `D:\Prop\tests\Unit\` | **No** `ShadowCopy*` tests |
| A24, A72, A20 §5 `destination_quotes`, A61 §8.33, A57 item 11, A73, architecture §24 / §31 | Binding law |

Repo grep (`ShadowCopyEngine` / `SimulateEntry` / `SimulateExit` / `MarkToMarket`): **definitions only**. Application, workers, API, and tests do not construct the engine.

---

## 2. What the SUT actually does with a destination quote

Full type surface:

```31:91:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
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
        // ...
    }

    public ShadowFill SimulateExit(/* ... DestinationQuote quote, DateTimeOffset now */)
    {
        var raw = openDirection == TradeDirection.Long ? quote.Bid : quote.Ask;
        // ...
    }

    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
}
```

### 2.1 Taker-touch map (correct on an untouched book)

| Method | Long | Short | Spec |
|---|---|---|---|
| `SimulateEntry` | `quote.Ask` | `quote.Bid` | A24 §11.1 OPEN/INCREASE |
| `SimulateExit` | `quote.Bid` | `quote.Ask` | A24 §11.1 REDUCE/CLOSE |
| `MarkToMarket` | `quote.Bid` | `quote.Ask` | A24 §12 conservative mark |

Mid is not the fill. `sourcePrice` is **not** the fill. Signed `SourceVsShadowSlippage` is `raw - source` (buy) / `source - raw` (sell), matching A24 §11.4 **measurement** formula.

That is the entire destination-quote success column.

### 2.2 Fields the engine actually reads

`DestinationQuote` record (Risk):

```24:30:D:\Prop\src\Domain\Risk\RiskEngine.cs
public sealed record DestinationQuote(
    string CanonicalSymbol,
    string? VenueInstrumentId,
    decimal Bid,
    decimal Ask,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? VenueTimestamp);
```

| Field | Used? | How |
|---|---|---|
| `Bid` | yes | Short entry, long exit, long mark |
| `Ask` | yes | Long entry, short exit, short mark; `Spread = Ask - Bid` |
| `ReceivedAt` | yes | `QuoteAge = now - ReceivedAt` (telemetry only) |
| `VenueTimestamp` | **no** | Dual-clock age never computed |
| `CanonicalSymbol` | **no** | Wrong-symbol book accepted |
| `VenueInstrumentId` | **no** | Unmapped / null instrument accepted |

No `Id`. No `venue_id`. No `fix_msg_seq_num`. No `md_entry_source`. The engine cannot name the snapshot it priced from.

### 2.3 What it writes back onto `ShadowFill`

```7:14:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
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

Missing vs A24 §10.2 / §10.5: `fill_quote_id`, dest bid, dest ask, `fill_quality` (`LIVE` \| `STALE_QUOTE`), `quote_age_ms` (int), `model_version`.

`ShadowOrder` persist (`D:\Prop\src\Domain\Entities\ShadowOrder.cs`) keeps `Price`, `Spread`, `SourceVsShadowSlippage`, `FilledAt` — **drops `QuoteAge`**. Even if a caller persisted the fill-shaped row, later reconstruction cannot recover the book.

---

## 3. Finding list (destination quotes only)

Severity: **BLOCK** = A24 acceptance fail if this shipped as shadow pricing. **HIGH** = silent wrong dest economics. **MED** = audit / replay hole.

### F1 — BLOCK — Fill is not the destination quote after 250 ms

```33:48:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public const decimal DefaultLatencySlippagePoints = 0.05m;
    // ...
        var raw = useAsk ? quote.Ask : quote.Bid;
        var adverse = direction == TradeDirection.Long ? DefaultLatencySlippagePoints : -DefaultLatencySlippagePoints;
        if (modeledDelay > TimeSpan.FromMilliseconds(250))
            raw += adverse;
```

A24 §11.4: optional `shadow_adverse_ticks` **default 0**, only if explicitly enabled **and versioned**. Do not hide a fudge inside the fill without `model_version`.

A24 §19.1: *every shadow fill price is taken from a persisted destination QUOTE snapshot*.

Measured:

- Threshold `250ms` is a compile-time magic number, not `SHADOW_EXECUTION_DELAY_MS` / `SHADOW_ADVERSE_TICKS`.
- Overlay `0.05` is named “Points” and applied as **raw dest price units** (USD/oz on XAU). That is 5 cents, not 0.05 tick, and not measured.
- After the add, `Price` is no longer `quote.Ask` / `quote.Bid`. Slippage vs source is then computed on the **mutated** number, so `source_vs_shadow_slippage` mixes dest tape + hidden model.
- `SimulateExit` has **no** delay parameter and **never** re-reads. Entry and exit are not the same dest-quote contract.

This is the opposite of “destination quotes only.” It is dest quotes **plus an unversioned lie**.

### F2 — BLOCK — Unusable books still fill

A24 §2.2 / A72 §3.2: reject snapshot before arithmetic when `bid <= 0`, `ask <= 0`, `ask < bid`, or instrument not mapped to canonical XAUUSD. Reason `QUOTE_INVALID` / `QUOTE_UNAVAILABLE`. Do not run spread math on a crossed book.

Engine: no check. `Spread` can be negative. `Price` can be `0` or inverted. `MarkToMarket` will happily emit a large P&L off a zero ask.

`DestinationQuote` is a **required** parameter (not `DestinationQuote?`). There is no API for “no dest quote exists.” A24 §1.3: *inventing fills when no destination quote has ever existed* is forbidden. Callers cannot express that state without fabricating a book.

### F3 — BLOCK — OPEN never fails closed on dest quote

A72 §6, evaluation moment 2: *after shadow simulated delay, `ShadowCopyEngine` re-reads the quote and repeats usability / age / spread / signed-adverse. No fill on fail.*

A24 §7.1–§7.4: blocking `QUOTE_UNAVAILABLE`, `QUOTE_STALE` (`quote_age > max_quote_age_open`), `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`. Do **not** fall back to the decision-time quote for OPEN.

Engine:

- Always returns a `ShadowFill`.
- Computes `QuoteAge = now - quote.ReceivedAt` and `Spread = Ask - Bid` and stores them as decoration.
- Does not compare age to any `max_quote_age_*`.
- Does not compare spread to any `max_spread_open`.
- Does not compute signed adverse vs expected dest touch. (`sourcePrice` is only the slippage print.)
- Does not know `action_class`. Entry vs exit are different methods; OPEN vs INCREASE vs REDUCE vs CLOSE is not a quote-policy input.

`RiskEngine` already has `QUOTE_STALE` / `SPREAD_TOO_WIDE` / `PRICE_MOVED_TOO_FAR` for `IsIncreasing` — **and this engine does not call it**. Even if it did, Risk’s move predicate is unsigned mid (A72 §10.1, forbidden). Shadow cannot inherit that as a substitute.

`CTraderFixOptions.MaxQuoteAgeMs = 5000` and `RiskLimits.MaxQuoteAge = 3s` already disagree (A72 §7.3). Shadow adds a third clock: none.

### F4 — BLOCK — CLOSE invents a dest price

A24 §8.3 waterfall:

1. QUOTE up and `quote_age <= max_quote_age_close` → fill taker, quality `LIVE`.
2. Else last usable quote and age `<= max_quote_age_close_stale_fallback` → fill, quality `STALE_QUOTE`.
3. Else **do not invent a tick**. Flag `SOURCE_CLOSED_UNPRICED`. No `shadow_copy_fill` row.

`SimulateExit` is path (1) with no age test and no quality enum. A 3-hour-old seed quote, a crossed book, or a caller-made dummy still produces `Price`. That is an invented dest print.

### F5 — BLOCK — Post-delay re-read is not implemented

A24 §11.2:

```text
Wait delay
Read latest destination quote with quote_received_at >= sim_send_at if available; else latest
Apply post-delay policy
Fill
```

`SimulateEntry` takes `now` and `modeledDelay` and uses them only for the 250 ms overlay. It does not:

- select a quote from `destination_quotes` / a tape,
- require `ReceivedAt >= sim_send_at`,
- distinguish decision quote vs fill quote,
- persist `decision_quote_id` vs `fill_quote_id`.

Whoever later “wires” this will be tempted to pass the **same** snapshot twice. That is the A24 §7.4 defect (OPEN fallback to decision-time book).

### F6 — HIGH — MTM is dest-touch only in name

Correct: long marks bid, short marks ask. Mid is not used.

Wrong vs A24 §12:

| Required | Measured |
|---|---|
| `mark_quote_id` | none |
| `mark_bid` / `mark_ask` stored | discarded after arithmetic |
| `mark_quality = STALE_QUOTE` when age > `max_quote_age_mark` | never |
| no quote ever → `unrealized = null` (not 0) | `decimal` always; `qty=0` or `px==entry` or `bid=0` all look like real P&L |
| `dest_contract_value` | implicit 1.0 |
| display mid isolated | N/A (good) / but no quality flag for dashboards |

Zero is a lie when the dest book is missing or dead. This method cannot say “unpriced.”

### F7 — HIGH — Two quote types; engine bound to the one that cannot persist

| Type | Path | Engine? | Id / venue / seq / md source |
|---|---|---|---|
| `DestinationQuote` record | `Domain\Risk\RiskEngine.cs` | **yes** (parameter) | no / no / no / no |
| `DestinationQuoteSnapshot` | `Domain\Entities\DestinationQuote.cs` | **no** | `Id` yes; `VenueInstrumentId` nullable; **no** `venue_id`, seq, md source |
| A20 / A61 `destination_quotes` | spec | **no** | UK `(venue_id, instrument_id)`; `quote_received_at`; computed age |

EF maps the snapshot to `destination_quotes` with **only** PK `Id` (TraderDbContext 149–153). No unique `(venue_id, instrument_id)`. No history table (A20: latest-only is correct; fills must still FK the snapshot **as used**).

Demo seed (`DemoSeeder.cs` 103–111):

```text
CanonicalSymbol = XAUUSD
VenueInstrumentId = null
Bid = 2399.45  Ask = 2399.85
ReceivedAt = now
VenueTimestamp = default null
```

That row is unusable under A24 §2.2 (instrument id not mapped) and would still price a fill if passed through.

Dashboard `GetFixSessionsAsync` takes `OrderByDescending(ReceivedAt).FirstOrDefault()` — **global latest**, not by instrument. Adjacent leak: even the UI quote is not “the dest book for this XAU id.” Shadow has no better selector.

### F8 — HIGH — Identity / clock fields ignored

- `CanonicalSymbol` unused → a `EURUSD` dest book can price an XAU shadow fill.
- `VenueInstrumentId` unused → A24 / A86 “never hardcode / never guess instrument” is unenforceable here.
- `VenueTimestamp` unused → A72 dual-clock OPEN reject cannot fire.
- `QuoteAge` can be **negative** if `now < ReceivedAt` (skew). No clamp, no `QUOTE_INVALID`.
- `PriceSource.CTraderQuoteSession` exists and is never stamped on a shadow fill. Replay can silently mix tapes (A24 §14, A45).

### F9 — MED — No dest-quote tests

`D:\Prop\tests\Unit` has `RiskEngineTests.Stale_quote_rejects_open` (Risk only). Grep of tests for `ShadowCopyEngine` / `SimulateEntry` / `MarkToMarket`: **zero**.

A24 §19.12 and A72 §11 that this SUT must prove, and does not:

| Required proof | Exists? |
|---|---|
| Open reject stale dest quote | no |
| Open reject `bid<=0` / crossed book (`QUOTE_INVALID`) | no |
| Open reject wide dest spread | no |
| Open long: dest ask improves → still fill (favorable); dest ask jumps → no fill | no |
| Decision-time pass, post-delay dest book stale → **no fill** | no |
| Close still fills on dest quote older than OPEN max, quality `STALE_QUOTE` | no |
| No dest quote on close of existing shadow → no fill, `UNPRICED` | no |
| Fill price == persisted snapshot bid or ask (no 0.05 overlay unless versioned) | no |
| Replay: recorded dest tape, not source last-deal (`Replay.ShadowCopyFromReplayTests`) | no |

§68 “stale quote rejection works” remains **false** for shadow.

### F10 — MED — Dead path

No Application / worker / outbox consumer constructs `ShadowCopyEngine`. QUOTE FIX worker is a heartbeat stub (A08 / A72). Even a perfect dest-quote function would not see a live book.

This is not an excuse for F1–F8. It is why the stub has not yet corrupted a shadow book: **there is no shadow book**.

---

## 4. A24 acceptance vs this file (dest-quote slice)

| # | A24 §19 | Dest-quote status on this SUT |
|---|---|---|
| 1 | Every fill from persisted dest QUOTE bid/ask | **FAIL** — no persist id; ±0.05 overlay; always fills |
| 3 | OPEN reject stale / wide / move / QUOTE down | **FAIL** — records age/spread, never rejects |
| 4 | CLOSE waterfall; never invent a tick | **FAIL** — always invents `Price` |
| 10 | Conservative MTM dest bid/ask; null ≠ 0 when unpriced | **FAIL** — touch correct, quality/null missing |
| 11 | No TRADE `NewOrderSingle` | **PASS** (engine cannot send; unused) |
| 12–13 | Unit + replay dest-tape tests | **FAIL** — no tests |

Items 2, 5–9 (durability, catch-up, reversal, sizing, dest costs) are **out of this review’s dest-quote charter** and are also unimplemented (A57 item 11).

---

## 5. What must change (implementation later; not this artifact)

When someone implements dest-quote pricing in this class, the contract is already written. Do not invent a second one.

1. **Price authority.** `Price` on a fill is exactly `quote.Ask` or `quote.Bid` from a snapshot that has an `Id`. Adverse ticks are a separate, versioned, default-**0** addend persisted on the fill (`model_version`). Delete or gate `DefaultLatencySlippagePoints` / `250ms`.
2. **Usability first.** `bid<=0` / `ask<=0` / `ask<bid` / unmapped instrument / null quote → no `ShadowFill`. OPEN: `QUOTE_INVALID` / `QUOTE_UNAVAILABLE`. CLOSE: waterfall step 3, not a dummy px.
3. **Accept `DestinationQuote?` plus action class.** Engine must see `OPEN_EXPOSURE|INCREASE_EXPOSURE` vs `REDUCE_EXPOSURE|CLOSE_EXPOSURE` so dest-quote guards can differ (A24 §7 vs §8). One `max_quote_age` is a defect.
4. **Post-delay re-read.** Caller (or a thin dest-quote store port) supplies **fill** snapshot with `ReceivedAt >= sim_send_at` when one exists. OPEN repeats A72 §4 on **that** snapshot. No silent reuse of decision quote.
5. **Bind one options object** (`MAX_QUOTE_AGE_OPEN_MS`, `MAX_QUOTE_AGE_CLOSE_MS`, `MAX_QUOTE_AGE_CLOSE_STALE_FALLBACK_MS`, `MAX_QUOTE_AGE_MARK_MS`, `MAX_SPREAD_OPEN`, `MAX_ADVERSE_MOVE_OPEN`). Do not add a fourth hardcoded age next to Risk 3s and FIX 5000ms.
6. **Return quality, not just px.** `LIVE` / `STALE_QUOTE` / `UNPRICED`. MTM returns `decimal?` + mark snapshot. Persist `fill_quote_id` / `mark_quote_id`.
7. **Unify types.** Map `DestinationQuoteSnapshot` → Risk `DestinationQuote` **with Id**. Add `venue_id`, instrument id (required), seq, md source before claiming A20.
8. **Tests** listed in §3 F9, with recorded XAU-like books (A72: e.g. 2399.50 / 2399.66). No live Pepperstone.

Do not implement dest-quote history in `destination_quotes` (A20 open question 2). Do not use this engine’s quote as source MFE/MAE (A45). Do not treat FIX “logged on” as a dest quote (A25 / A72).

---

## 6. Explicit non-findings (so this is not a rubber stamp)

These dest-quote items are **correct** on the current source and must not be “fixed” into mid or source-price fills:

- Long open / short close buy the **ask**.
- Short open / long close sell the **bid**.
- Long mark = bid; short mark = ask.
- Source deal price is informational slippage only (until F1’s overlay poisons `raw`).
- Engine does not send FIX TRADE.

Those four lines are necessary and nowhere near sufficient.

---

## 7. Traceability

| Topic | Law |
|---|---|
| Dest QUOTE is shadow price authority | Architecture §24, §31; A24 §1.4, §11.1 |
| Persist the exact snapshot used | A24 §2.5, §10.2; A20 `destination_quotes`; A61 §8.33 |
| Usable book | A24 §2.2; A72 §3.2 |
| OPEN dest-quote guards + post-delay re-check | A24 §7; A72 §4, §6 moment 2 |
| CLOSE dest-quote waterfall | A24 §8.3; A72 §5.1 |
| Conservative dest MTM; unpriced ≠ 0 | A24 §12 |
| No hidden dest-price fudge | A24 §11.4, §17 `SHADOW_ADVERSE_TICKS=0` |
| Dual clock | A72 §3.3; A23 |
| One configured max age | A72 §7.3 |
| §69 item 11 | A57 — still **No** |
| Tests | A24 §19.12; A27 `Replay.ShadowCopyFromReplayTests`; A72 §11 |

---

## 8. One-line law

```text
A shadow fill price is a persisted destination bid or ask, or it is not a shadow fill.

ShadowCopyEngine today: dest touch ± 0.05 after 250ms, any book, any age, always a number.
That is not destination-quote authority.
```

*End of B18. Product source was not modified.*
