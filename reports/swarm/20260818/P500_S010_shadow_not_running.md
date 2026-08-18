# P500 S010 — Shadow is not running (not marking to Pepperstone)

| Field | Value |
|---|---|
| Agent | P500_S010 (read-only; shadow / dest expectancy) |
| Date | 2026-08-18 |
| Assigned | Read `ShadowCopyEngine.cs`, `PersistDemoShadowAsync` in `EfTradingStore`, `OverviewDto` `shadowPnl`. Live overview `destinationRealPnl=0` `shadowPnl=0`. Write this report. **Do not edit product.** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S010_shadow_not_running.md` |
| Product source modified | **No.** |
| Binding law | Architecture §§1.4, 15, 18–24, 31, 36–39, 63–64, 68–70; A24; A72; A73; A91 |
| Remeasured | 2026-08-18 this pass (`read_file` + `grep`) |

**Honesty first.** Live overview zeros are not a UI glitch. They are the measured consequence of a pipeline that never persists Pepperstone bid/ask, never marks a shadow book, and hard-codes destination real P&L to `0`. There is **no measured destination expectancy**. Enabling live send now is **gambling**, not profit.

## Profit implication

`shadowPnl=0` / `destinationRealPnl=0` is **not** a measured break-even book. It is an empty / wrong-grain ledger. You cannot raise dest profit from a number that was never computed. Sending `35=D` because SHADOW counts look good donates the Pepperstone account to unmeasured spread. Lower loss = keep send off until a standing QUOTE tape actually fills and marks.

**Drift vs first draft:** `CopyTradingHostedService` (8s delay, 20s tick) now also calls `CopyTradingService.GenerateShadowIntentsAsync`. That path still **returns no fills** if `DestinationQuotes` is empty. `grep DestinationQuotes.Add` in product `*.cs` = **1** (`DemoSeeder` invented 2399). `MarkToMarket` / `SimulateExit` still definition-only. `GetOverviewAsync` still passes literal `0` for dest real P&L (`EfDashboardQueries.cs` L44). `AllowFixSend` is persisted **false**. `NewOrderSingleImplemented = false`.

---

## 0. Verdict (binding)

| Claim | Result |
|---|---|
| Is shadow marking to live Pepperstone quotes? | **NO.** |
| Does live bootstrap write `destination_quotes`? | **NO.** Only `DemoSeeder` (tests) inserts one invented 2399 book. |
| Does any FIX path write `DestinationQuoteSnapshot`? | **NO.** Grep of product `*.cs`: single `DestinationQuotes.Add` = `DemoSeeder` L105. |
| Does `MarkToMarket` run in product? | **NO.** Definition only. Zero callers. |
| Does `SimulateExit` run in product? | **NO.** Definition only. |
| What does live `GET /api/overview` report for dest P&L? | **Constructor literal `0`.** Always. Independent of DB. |
| What does live `shadowPnl` become with empty `shadow_orders`? | **`0`.** `Sum(SourceVsShadowSlippage)` over empty set. |
| Is that sum even P&L if rows existed? | **NO.** It is entry **slippage in price units**, not dest $ P&L. |
| Is there a measured dest expectancy sample? | **NO.** |
| Is enabling `NewOrderSingle` / real copy justified? | **NO. Gambling.** |

```text
SHADOW_RUNNING_VS_PEPPERSTONE     NO
DESTINATION_QUOTE_LIVE_WRITER     ABSENT
MARK_TO_MARKET_WIRED              ABSENT
SIMULATE_EXIT_WIRED               ABSENT
OVERVIEW.destinationRealPnl       LITERAL 0
OVERVIEW.shadowPnl (live path)    0  (empty shadow_orders OR wrong grain)
DESTINATION_EXPECTANCY            UNMEASURED
LIVE_SEND_NOW                     GAMBLING_NOT_PROFIT
PRODUCT_EDITED                    NO
```

Do **not** treat Overview “Shadow” trader **count** as evidence the shadow book is live. That tile is `TraderScores` with `CurrentState == SHADOW` (source scorer). It does not require a dest quote, a fill, or a P&L.

Do **not** treat a non-zero `shadowPnl` after `DemoSeeder` as expectancy. Prior D48 measured `248.20` = Σ entry slippage vs a seeder-invented 2399 book. That is not Pepperstone tape.

---

## 1. What was read (no product edits)

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Engine: `SimulateEntry` / `SimulateExit` / `MarkToMarket` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` L251–337 | Only persist path: `PersistDemoShadowAsync` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `OverviewDto.ShadowPnl` / `DestinationRealPnl` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L20–53 | Live overview numbers |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L119–145 | Rebuild → persist |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Live scoring caller |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | **Live** seed — **no quotes** |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L105–113 | **Only** quote writer — fake 2399 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false`; no engine in DI |
| `D:\Prop\apps\api\Program.cs` | `BrokerCatalogSeed` only; `/api/overview` |
| `D:\Prop\apps\fix-worker\Worker.cs` | Stamps FIX `Disconnected`; no MD |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; no quote persist |
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | In-memory bid/ask; never hits EF |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` then done |
| `D:\Prop\src\Domain\Entities\ShadowOrder.cs` | Persist shape: no realized/unrealized P&L |
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | `DestinationQuoteSnapshot` |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` L187–211 | `SHADOW` from source quality/risk |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` L26–27 | Renders the two zeros |
| `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx` | Static copy; no book |

Repo grep of product `*.cs`:

| Pattern | Product hits |
|---|---|
| `DestinationQuotes.Add` / `new DestinationQuoteSnapshot` | **1** — `DemoSeeder` |
| `ShadowOrders.Add` / `new ShadowOrder` | **1** — `PersistDemoShadowAsync` |
| `new ShadowCopyEngine` | **2** — `PersistDemoShadowAsync` + `CopyTradingService` field |
| `MarkToMarket(` | definition only (`ShadowCopyEngine.cs` L85) |
| `SimulateExit(` | definition only (`ShadowCopyEngine.cs` L63) |
| `SimulateEntry(` | persist path + `CopyTradingService` (both need a quote row) |

---

## 2. Why live overview is `destinationRealPnl=0` `shadowPnl=0`

### 2.1 `destinationRealPnl` is not queried

`GetOverviewAsync` constructs:

```csharp
return new OverviewDto(
    accounts,
    brokers,
    xauTraders,
    three,
    /* WATCH / SHADOW / LIVE_CANDIDATE / LIVE / RISK_BLOCKED counts */,
    shadowPnl,
    0,   // DestinationRealPnl
    0,   // XauGross
    0,   // XauNet
    ...
);
```

File: `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L33–47.

There is **no** destination fill table, **no** dest position P&L, **no** Pepperstone execution ledger. The DTO field exists so the UI can paint a number. The number is always `0`. Live or demo. This is not “we measured zero profit.” This is “we never compute dest real P&L.”

`RiskDashboardDto` is the same lie grain: `DailyPnl=0`, `Drawdown=0`, `XauLong=0`, `XauShort=0`, `XauNet=0` (`GetRiskAsync` L208).

### 2.2 `shadowPnl` is Σ entry slippage, then empty on live

```csharp
var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

File: `EfDashboardQueries.cs` L29.

Even if rows exist, this is **not** shadow P&L:

- `SourceVsShadowSlippage` = dest touch − source VWAP (or inverse for shorts). Price-unit delta at **entry**.
- No `qty` multiply in the sum.
- No contract / lot / tick-value conversion (`QuantityNormalizer` unused; unit test skip says so).
- No exit fill, no swap, no commission, no dest cost model (A24: dest costs must not be copied from source).
- `MarkToMarket` exists on the engine and is never called, so open exposure is never revalued.

Trader-grid `ShadowPnl` is worse: `GetTradersAsync` hard-codes **`0`** on every `TraderRowDto` (`EfDashboardQueries.cs` L118). Per-trader shadow is structurally blank.

### 2.3 Live path produces **zero** `shadow_orders`

Causal chain (measured in source, not inferred from a green dashboard):

```text
apps/api/Program.cs
  EnsureCreated + BrokerCatalogSeed.EnsureAsync
       │
       ├─ brokers, XAU instrument, kill switch, FIX session rows (Disconnected)
       └─ DestinationQuotes: NOT TOUCHED

LiveIngestHostedService
  catalog → deals → ReconstructionScoringService.RebuildTraderAsync
       │
       └─ PersistDemoShadowAsync(state=SuggestedState, completedXau)

PersistDemoShadowAsync
  always: Outbox ScoreUpdate
  if state != SHADOW: return          ← most live logins
  quote = DestinationQuotes.OrderByDescending(ReceivedAt).FirstOrDefault()
  if quote is null: return            ← LIVE PATH STOPS HERE
  else: SimulateEntry(now, latest quote) → ShadowOrder
```

`BrokerCatalogSeed` (`D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`) writes Achiever + StarwaveFX + kill switch + two FIX rows. It never inserts `destination_quotes`.

`DemoSeeder` is **not** called from API / MT5 worker / FIX worker. Integration tests only.

Therefore a live (or live-shaped) database after catalog seed has:

| Table | Typical live after seed+ingest |
|---|---|
| `destination_quotes` | **0 rows** |
| `shadow_orders` | **0 rows** (quote gate) |
| `copy_intents` | **0 SHADOW_ONLY** (same gate) |
| Overview `shadowPnl` | **0** |
| Overview `destinationRealPnl` | **0** (literal) |

If a leftover demo DB still has the 2399 seed quote, `shadowPnl` may become a non-zero **slippage sum**. That is still **not** Pepperstone mark-to-market.

---

## 3. `ShadowCopyEngine` — calculator, not a running book

File: `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` (91 lines).

| Method | What it does | Wired? |
|---|---|---|
| `SimulateEntry` | Taker touch: Long=Ask, Short=Bid. If `modeledDelay > 250ms`, ±`0.05` overlay. Slippage vs **source** price. | Persist path only |
| `SimulateExit` | Close touch: Long=Bid, Short=Ask. | **Never called** |
| `MarkToMarket` | `(px - entry) * sign * qty` on dest bid/ask. | **Never called** |

Persist always passes `TimeSpan.FromMilliseconds(80)` (`EfTradingStore.cs` L319). **80 < 250**, so the latency overlay never applies. Fills are “dest touch at rebuild time,” not delayed dest tape.

Fail-open vs A24 / A72:

- Missing quote is handled **only** by the store (`quoteRow is null` → skip). The engine itself never rejects.
- No stale-age reject inside `SimulateEntry` (engine records `QuoteAge` but still fills).
- No wide-spread reject.
- No crossed/zero book reject.
- No instrument-id check (`DemoSeeder` sets `VenueInstrumentId = null`).
- One caller-supplied snapshot; no post-delay re-read of dest book (A73).

`ShadowPosition` is a dead record. The engine never persists positions, realized, or unrealized.

`QuantityNormalizer` is unused (`tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` skip: “QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”). Dest lot size is echoed source `MaxVolumeLots`.

Not in DI. Instantiated with `new` inside the store.

This is **not** Architecture §24 shadow copy.

---

## 4. `PersistDemoShadowAsync` is demo OPEN backfill

File: `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` L251–337.

Name is honest. Behavior is not a live shadow loop.

| Step | Measured |
|---|---|
| Trigger | Every `RebuildTraderAsync` (ingest / resync), not a dest-quote tick |
| Outbox | Always `ScoreUpdate` |
| Shadow rows | Only `state == TraderState.SHADOW` |
| Quote used | **Latest** `destination_quotes` row, any symbol, any age |
| Trades used | **Completed** XAU only (`completedXau.Where(t => t.Completed)`) |
| Action | `CopyIntentAction.OpenExposure` only |
| Timing | `FilledAt = DateTimeOffset.UtcNow` vs `ExpectedPrice = trade.EntryVwap` (historical) |
| Expiry | `ExpiresAt = trade.OpenedAt.AddSeconds(15)` — already dead for historical opens |
| Status | Literal `"SHADOW_ONLY"` — nothing in product reads this string |
| Idempotency | `shadow:{brokerId}:{login}:{positionId}` on `CopyIntents` |
| Risk | **No** `RiskEngine.Evaluate` |
| Exit | **No** `SimulateExit` when the source trade is already closed |
| MTM | **No** `MarkToMarket` on still-open source positions |
| Live send | **No** `ExecutionIntent`, no `35=D` |

So even when the quote gate passes, the “shadow fill” prices a **closed** source trade against **today’s** (or seed-time’s) dest print. That is not:

- as-of dest quote at source open,
- dest quote at modeled delay,
- dest quote at source close,
- or a running mark of open dest exposure.

It cannot produce destination expectancy.

`TraderState.SHADOW` itself is **source scoring**, not dest proof:

```csharp
if (quality >= 70 && risk < 40)
    return TraderState.SHADOW;
```

`TraderStateMachine.CanPromoteToLive` is hard `false`. Good. Promotion to LIVE is not implemented. That does **not** make the shadow book real.

---

## 5. Pepperstone quotes are not in this system

### 5.1 The only quote row in the repo

`DemoSeeder.cs` L105–113:

```csharp
db.DestinationQuotes.Add(new DestinationQuoteSnapshot
{
    CanonicalSymbol = "XAUUSD",
    VenueInstrumentId = null,   // unusable under A24 instrument discovery
    Bid = 2399.45m,
    Ask = 2399.85m,
    ReceivedAt = now
});
```

Invented. Not from `35=W` / `35=X`. Not from Pepperstone. Stale the moment seed finishes.

### 5.2 FIX surfaces that could have written quotes — do not

| Surface | What it actually does |
|---|---|
| `CTraderFixSession.TryLogonAsync` | TCP+TLS, send `35=A`, read one reply. Stop. |
| `CTraderFixLogonHostedService` | Logon QUOTE+TRADE if password present. Persist **session status only**. `RealCopyEnabled=false`. No `35=x`/`35=y`, no `35=V`, no MD snapshot persist. |
| `CTraderQuoteService` | In-memory latest bid/ask from a **harness** tag set (1320/1321). Never registered in live DI as a quote→EF writer. |
| `apps/fix-worker/Worker.cs` | Every 15s: stamp both sessions `Disconnected`, “No live QUOTE socket.” Does **not** open TLS. |
| `CTraderQuoteService.TryAcceptMarketDataSnapshot` | Would reject stale vs `MaxQuoteAgeMs` — **if anyone fed it**. Nobody persists the accepted book. |

Dashboard `QuoteHealthy` can still go true if a session enum is `LoggedOn` **or** `_runtime.Quote.LoggedOn` (`EfDashboardQueries.cs` L48–49). Logon ≠ subscribed XAU book ≠ `destination_quotes` row ≠ shadow MTM.

`GetFixSessionsAsync` will show `quoteAgeSeconds` from the latest snapshot. On live seed that snapshot **does not exist** (`null` bid/ask/age).

### 5.3 What “marking to Pepperstone” would require (absent)

1. Live QUOTE session stays up (not a one-shot Logon).
2. SecurityList discovery of **this account’s** XAU instrument id (never hardcode `55=XAUUSD`).
3. Market-data subscribe; persist each accepted book to `destination_quotes` with venue timestamp + instrument id.
4. On source open (not on next rebuild of a completed trade): `SimulateEntry` vs **then-current** dest book, fail closed if stale/wide/missing.
5. On source close / reduce: `SimulateExit` vs dest book at that time.
6. While open: periodic `MarkToMarket` vs latest dest book; persist unrealized; never paint missing mark as `0` profit.
7. Dest cost model (commission/swap/spread) measured on **this** Pepperstone account; quality=`ASSUMED` until measured.
8. Overview `shadowPnl` = realized + conservative unrealized dest $, not Σ slippage.
9. Sample size + time in SHADOW before anyone discusses LIVE (Architecture §68 / §69.11).

None of 1–9 exist as a running loop.

---

## 6. Live send now is gambling, not profit

Capital-risk facts that remain true:

- `LiveRuntimeStatus.RealCopyEnabled` is env-read (`REAL_COPY_EXECUTION_ENABLED=="true"`). FIX logon host **logs** `RealCopyArmed` and does **not** force the flag false. Send is still off: `NewOrderSingleImplemented = false`, persisted `AllowFixSend = false`, no `35=D` builder.
- `CanPromoteToLive` always `false`.
- Zero `ExecutionIntent` writers in `src/` + `apps/`.
- FIX worker refuses `NewOrderSingle` even if the config flag is flipped (`Worker.cs` L45–46).
- Risk of dest capital from **this** dashboard: **none today**, by absence of send.

That safety is **not** a profit argument.

Expectancy facts:

| Question | Answer |
|---|---|
| Have we marked source SHADOW traders to Pepperstone bid/ask through open→close? | **No.** |
| Do we have dest realized P&L? | **No. Field is literal 0.** |
| Do we have dest unrealized P&L? | **No. `MarkToMarket` unwired.** |
| Do we know dest spread / reject / slippage vs source VWAP on a live tape? | **No.** |
| Do we know dest lot/step conversion? | **No. Qty echoed.** |
| Does source net P&L imply dest net P&L? | **No.** Different book, delay, cost, rejects, sizing. |
| If we enable send because source scores look good? | We would be **betting source hope** on an **unmeasured venue**. That is gambling. |

`OverviewPage` already says NewOrderSingle is off. The zeros next to that sentence are the honest dest ledger: **empty.**

---

## 7. What the zeros do **not** mean

| Misread | Correction |
|---|---|
| “Shadow P&L is flat / break-even.” | Empty sum or wrong grain. Not a measured 0 expectancy. |
| “Dest real P&L is flat.” | Never computed. |
| “N traders in Shadow means N books are being copied dest-side.” | Those are source `TraderState.SHADOW` scores. |
| “QUOTE healthy ⇒ dest marks are live.” | Logon/enum greenwash; no `destination_quotes` writer. |
| “After DemoSeeder, shadow works.” | Six entry-only rows vs invented 2399. D48. Not §24. |
| “Safe to flip real copy once MT5 ingest looks busy.” | Ingest is source. Dest expectancy is still unmeasured. |

---

## 8. Classification

| Slice | Class |
|---|---|
| Engine types compile | `EXISTS` (calculator) |
| Running dest shadow book | `MISSING` |
| Pepperstone quote persist | `MISSING` |
| Overview dest real P&L | `LITERAL_ZERO` (not measured) |
| Overview shadow P&L on live path | `ZERO_BECAUSE_NO_ROWS` + wrong grain even if rows |
| Live send | `SAFE_BY_ABSENCE` today |
| Dest expectancy | `UNMEASURED` |
| Enable live send | **GAMBLING** |

---

## 9. One-line close

**Shadow is not marking to Pepperstone quotes; live overview `destinationRealPnl=0` / `shadowPnl=0` is structural emptiness, not a measured dest edge — enabling live send now is gambling, not profit.**
