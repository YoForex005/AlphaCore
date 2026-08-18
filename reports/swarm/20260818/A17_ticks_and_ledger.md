# A17 — Source ticks vs ledger (Architecture §17: MFE/MAE needs ticks; do not fabricate)

**Date:** 2026-08-18  
**Auditor:** senior engineer (swarm A17)  
**Scope:** `MT5TickBridge` + `mt5_ledger::Store` against Architecture §17 (and the §1.5 / §11 / §45 / §51 / §60 MFE/MAE rules).  
**Sources:**
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.5 (lines 85–86), §11 (500–517), §14 (599–663), §17 (735–782), §18 (786–816), §45 (1672–1731), §51 (1874–1899), §60 (2228–2256)
- `D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.h`
- `D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.cpp`
- `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.h`
- `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.cpp`
- Supporting (read-only): `imt5_client.h`, `mt5_types.h` (`TickData`, `DealData`, `PositionData`, `ChartBarData`), `mt5_manager.cpp` (`GetTickLast` / `SubscribeTicks` / `GetAllTicksLast` / `GetChart`), `mt5_pool.cpp` (`MT5Session::GetTickLast`), `mt5_http_client.cpp` (`GetTickLast`), `tests/mt5_ledger_store_test.cpp`, `CMakeLists.txt`, `vendor/MetaTrader5SDK/Include/MT5APIManager.h` (`TickHistoryRequest`), `vendor/MetaTrader5SDK/Include/Bases/MT5APITick.h`

**Product source:** not modified.

---

## Verdict

**FAIL / not ready for exact MFE/MAE.**

Architecture §17 is explicit: exact MFE, MAE, price excursion, entry spread, and in-trade volatility require a **source-side time series while the position is open**. Closed deals are not that series. If the source tick (or an explicitly labeled poorer substitute) is missing, **do not fabricate the features**.

What exists today:

| Layer | Exists? | Persists ticks? | Can compute exact MFE/MAE? |
|---|---|---|---|
| `MT5TickBridge` | Yes (opt-in `MT5SDK_WITH_DROGON`) | **No** — in-memory queue, drain to `IQuoteSink` only | **No** |
| `IMT5Client` live last-tick | Yes (`GetTickLast`, optional `SubscribeTicks`) | No | **No** (snapshot, not a window) |
| `IMT5Client` historical ticks | **No** (`TickHistoryRequest` is not wrapped) | No | **No** |
| `mt5_ledger::Store` | Yes (opt-in `MT5SDK_WITH_POSTGRES`) | **No** — raw events + deal revisions only | **No** |
| Planned `mt5_ticks_xauusd` / `mt5_xau_ticks` | Named in §11 / §45 only | No table, no writer, no migration in this tree | **No** |
| C# feature engine / `MfeMaeCalculator` | Not present (`Class1` stubs / empty worker) | n/a | **No** |

Honest state: the SDK can **see** live ticks for a web-terminal quote hub. It does **not** store a source tick tape, does **not** attach `price_source` / `feature_quality`, and does **not** compute MFE/MAE. That is the correct interim position. The failure is the **gap vs §17**, not a finding that someone is already inventing excursion numbers in this tree (repo-wide search for `MFE`/`MAE`/`excursion` hits the architecture document only).

---

## Architecture §17 (binding rule)

Quoted requirement (v2 §17, lines 735–782):

If we want exact features such as:

```text
MFE
MAE
price excursion
entry spread
volatility during trade
```

we need time-series price data **while each source trade is open**.

Preferred:

```text
MT5 source broker tick feed / Manager symbol tick subscription
```

If unavailable:

1. store the best available price feed **explicitly**,
2. mark the feature source,
3. **do not pretend** another broker's cTrader quote feed is identical to the source MT5 price stream.

Feature metadata must include:

```text
price_source
feature_quality
```

Examples given by the architecture:

```text
price_source=ACHIEVER_MT5_TICKS
feature_quality=EXACT
```

or:

```text
price_source=BAR_APPROXIMATION
feature_quality=APPROXIMATE
```

**Never silently mix them.**

Reinforcing clauses elsewhere:

| Location | Rule |
|---|---|
| §1.5 | Do **not** calculate MFE/MAE from closed deals alone. If source-side tick data is not available, **do not fabricate**. |
| §11 | Raw table `mt5_ticks_xauusd` — *if* source SDK/feed supports it. Raw layer immutable as practical. |
| §14 `ReconstructedTrade` | No MFE/MAE fields. Reconstruction is deal/order/position lifecycle, not excursion. |
| §18 | Baseline **may** use MAE/MFE only as score inputs — still subject to §17 quality labels. |
| §45 | Core table `mt5_xau_ticks` (name differs from §11). |
| §51 | Dashboard shows **“MFE/MAE when valid”**. |
| §60 | Unit-test **“MFE/MAE where data exists”** — omit / refuse when data does not exist. |

Name inconsistency (`mt5_ticks_xauusd` vs `mt5_xau_ticks`) is unresolved. Neither table exists in product SQL in this repository.

---

## What exact MFE/MAE actually requires

For one reconstructed XAUUSD lifecycle `[opened_at, closed_at]` on a **source** broker:

| Input | Why |
|---|---|
| Source symbol (or canonical map) | Ticks must be the **same book** the trader was filled on |
| Bid/ask (and optionally last) at millisecond time | Long MAE uses adverse ask/bid path; MFE uses favorable path; entry spread needs bid **and** ask at/near fill |
| Coverage across the **open window**, not one last tick | A single `TickLast` cannot see intra-trade extremes |
| Completeness / drop accounting | A dropped or polled-sparse tape is at best `APPROXIMATE` |
| `price_source` + `feature_quality` on the feature row | §17 forbids silent mixing of source ticks, bars, and destination FIX quotes |

**Closed `DealData` is not this input.** A deal is one fill: `price`, `volume`, `profit`, `time`. `PositionData.price_current` is the last mark, not the high/low path. `DealRevision.price` is the same single fill. Realized P&L is not MFE; max floating profit/loss is not recoverable from entry VWAP + exit VWAP.

**Do not substitute any of the following and call it `EXACT`:**

1. Closed-deal high/low invented from `{entry, exit}` (two points).
2. `IMT5Client::GetTickLast` / 250 ms poll snapshots.
3. Session `MTTickStat` bid_high/bid_low (day/session stats, not the trade window). Used today only as a “symbol has a tick” probe in `MT5Manager` symbol enumeration — correct. Using those highs/lows as trade MFE/MAE would be fabrication.
4. Destination cTrader FIX quotes (`destination_quotes`, Phase 4). Different venue, different book.
5. `GetChart` OHLC bars without labeling `price_source=BAR_APPROXIMATION`, `feature_quality=APPROXIMATE`.
6. Interpolated / synthetic ticks, Brownian bridges, or “typical XAU range” fillers.

If the window is missing: **leave MFE/MAE null**, set quality to absent/invalid, and keep scoring on features that deals *can* support (net PnL, hold time, scale-in flags, etc.).

---

## `MT5TickBridge` — live quote fan-out, not a market-data warehouse

### Role (as implemented)

Header contract (`mt5_tick_bridge.h` lines 4–32): adapter from the SDK pump-thread `IMTTickSink::OnTick` to a downstream `IQuoteSink` (documented as `TerminalQuoteHub`). Design constraint #1: **OnTick must not block** — no socket send, no DB, no `IMT5Client` re-entry under the manager mutex.

That constraint is **correct** and is compatible with §17 **only if some other component**, off the pump thread, persists a durable tape. No such component exists in this tree.

Build flag: `MT5SDK_WITH_DROGON` (default **OFF**). Without Drogon the bridge is not even compiled.

### Data path

```text
SDK pump thread
  IMTTickSink::OnTick
    → MT5TickBridge::onSdkTick   (normalize MTTickShort → TickData, enqueue)
    → bounded deque (cap 50_000; drop oldest)

Drogon event loop
  drainLoop every 20 ms  → IQuoteSink::onTick for still-subscribed symbols
  pollLoop  every 250 ms → only if SubscribeTicks failed (remote HTTP, etc.)
```

`TickData` fields forwarded (`mt5_types.h`): `symbol`, `bid`, `ask`, `last`, `volume`, `time`, `time_msc`, `flags`. That **shape** is enough to *compute* MFE/MAE later. The series is **not kept**.

### Push vs poll (neither is a ledger)

| Mode | How it arms | Cadence | What you actually get |
|---|---|---|---|
| Push (preferred) | First `ensureSubscribed` → `IMT5Client::SubscribeTicks` | SDK tick rate; drain 20 ms | Live ticks for **all** symbols the manager streams; **forwarded only** if per-symbol refcount > 0 |
| Poll fallback | `SubscribeTicks` returns false (default on `IMT5Client`; HTTP client does not override) | 250 ms, **≤ 64 symbols / tick**, round-robin | `GetTickLast` **snapshot**, not every print |

Poll path, when a pool is wired, borrows one `MT5Pool` session (200 ms timeout) so it never contends on the pump mutex. If the pool is saturated, **the cycle is skipped**. That is the right trading-grade isolation for a quote hub. It is **not** a complete source tape.

Remote HTTP: `MT5HttpClient::GetTickLast` is `GET /mt5/symbols/{sym}/tick` — last tick only. No subscribe. Poll fallback is the only live path.

### Loss / fabrication already present in the *transport* (not prices)

These are **not** MFE/MAE bugs, but they make the live stream **unfit to relabel as `EXACT` evidence** if someone later tees the drain into Postgres without more work:

1. **Oldest-first drop** at `kMaxQueuedTicks = 50000`. `droppedTicks_` is logged on shutdown only. No durable gap marker.
2. **Drain with no sink** drops the popped batch (`drainLoop`: if `sink_` is null, return after swap). Those ticks are gone.
3. **Unsubscribe between enqueue and drain** discards the tick (refcount gate). Correct for a quote hub; fatal for a tape.
4. **Timestamp backfill** in `onSdkTick`: if SDK `datetime` / `datetime_msc` are zero, wall clock (`system_clock`) is written. That is a **fabricated timestamp**, not a fabricated price. Excursion aligned to wall clock is not broker time.
5. **`GetTickLast` / `GetAllTicksLast` omit `volume`** (and `volume_ext`). Push `onSdkTick` copies `tick.volume`. Last-tick snapshots are incomplete vs `MTTickShort`.
6. **No historical API.** Manager SDK exposes `TickHistoryRequest` / `TickHistoryRequestRaw` (`MT5APIManager.h` lines 309, 512). `IMT5Client` has no corresponding method. Backfill of ticks for already-closed trades is impossible through the product client.

`GetChart` exists on `MT5Manager` (M1 source, aggregated). That is the **only** durable-capable historical price path, and it is bars, not ticks. Allowed by §17 only as `BAR_APPROXIMATION` / `APPROXIMATE`. It is not labeled, not stored as features, and not wired to MFE/MAE — which is correct (omission ≠ fabrication).

### Subscription model vs 5,000 XAUUSD accounts

`ensureSubscribed` / `maybeUnsubscribe` are **per-symbol refcounts**, not per-account. For §17 you need XAUUSD (plus broker aliases) subscribed for the **union of open source positions**, then a writer that keys ticks by `{server_key, symbol, time_msc}`. The bridge does not know accounts, logins, or open windows. It must not grow a DB write on the pump thread to learn them.

---

## `mt5_ledger::Store` — immutable deal evidence, not a tick store

### Role (as implemented)

Header (`mt5_ledger_store.h`): write-side boundary for the **canonical MT5 evidence ledger**. Accepts no credentials. Never updates historical broker evidence: duplicate deliveries return the original row; corrected broker records require a **new revision number** and a **new source event**.

Two operations only:

| API | Table | Semantics |
|---|---|---|
| `recordRawEvent` | `mt5_raw_events` | Insert-or-return existing UUID on `(server_key, source_event_id)` |
| `recordDealRevision` | `mt5_deals_ledger` | Insert revision; `ON CONFLICT (server_key, deal_ticket, revision_no) DO NOTHING` |

Build flag: `MT5SDK_WITH_POSTGRES` (default **OFF**). No in-repo `.sql` migration defines these tables; the C++ embeds the DML and assumes the schema already exists.

### What a `DealRevision` holds (and does not)

Stored: tickets (deal/order/position), action/entry/reason codes, symbol, volume, **single** `price`, profit/commission/swap/fee, currency, `broker_time`, `raw_event_id`, `payload_hash`.

**Not stored:** bid, ask, last, tick time, tick flags, high/low path, MFE, MAE, spread, `price_source`, `feature_quality`.

A closed deal revision is exactly the thing §1.5 / §17 say **must not** be turned into MFE/MAE.

### Raw events cannot smuggle a tick tape

`RawEvent` validation (`Store::isValid`):

- non-empty `serverKey`, `sourceEventId`, `entityType`
- JSON object payload + SHA-256 hex `payloadHash`
- `eventKind ∈ {add, update, delete, clean, sync, snapshot}`
- `payloadSchemaVersion > 0`

`entityType` is an unconstrained non-empty string. One *could* write `entityType="tick"` into `mt5_raw_events`. That would be the wrong table (event log, not a time-series), would not give range queries for `[opened_at, closed_at]`, and **is not implemented**. No caller of `recordRawEvent` / `recordDealRevision` exists in this repository outside `mt5_ledger_store.cpp` and the hermetic validator test.

### Tests do not prove persistence or ticks

`tests/mt5_ledger_store_test.cpp`: hash format, `eventKind` allow-list, currency must be 3-letter uppercase. **No Postgres.** **No tick fixture.** README: tests are hermetic — no MT5 server, no database.

### Immutability is the right deal-ledger law

`ON CONFLICT DO NOTHING` + revision-only corrections match §11 (“raw layer as immutable as practical”) and the store comment (“never updates historical broker evidence”). When a tick table is added, it should follow the same law: append-only, content-addressed or `(server_key, symbol, time_msc, source)` unique, **no overwrite of a prior print**. That work is not done.

---

## Cross-layer gap vs §17

```text
§17 wants:

  source MT5 ticks (or labeled bars)
       ↓ persist mt5_xau_ticks / mt5_ticks_xauusd
       ↓ join reconstructed trade window
       ↓ MFE/MAE + price_source + feature_quality
       ↓ score / dashboard "when valid"

What is built:

  Manager TickSubscribe / TickLast
       ↓ MT5TickBridge (RAM queue, optional)
       ↓ IQuoteSink (quote hub; not in this repo)
       ✗ no tick table
       ✗ no TickHistoryRequest on IMT5Client
       ✗ ledger = deals/raw events only
       ✗ no MfeMaeCalculator
       ✗ C# worker is a 1s log loop
```

| §17 / related demand | Measured state |
|---|---|
| Source tick feed while trades are open | Live **transport** exists (local SDK push). Not scoped to open XAUUSD positions. Not durable. |
| Historical tick backfill | SDK has `TickHistoryRequest`. Product client does **not** expose it. |
| Persist `mt5_ticks_xauusd` / `mt5_xau_ticks` | **Missing** (name not even unified). |
| Label `price_source` / `feature_quality` | **No types, no columns, no writers.** |
| Never mix cTrader quotes with source ticks | Safe today only because **neither** MFE nor destination quotes are implemented. |
| Do not compute MFE/MAE from deals alone | Safe today because **no calculator exists**. Ledger cannot even hold the answer. |
| Dashboard “MFE/MAE when valid” | No feature snapshot table, no UI. |
| §60 test: compute if window exists; refuse if not | Named in swarm A09 as future `MfeMaeCalculatorTests`. **Not written.** |

---

## What must not be built (fabrication blacklist)

Do **not**, in any follow-on PR:

1. Derive MFE/MAE from `mt5_deals_ledger` / `DealData` / entry+exit VWAP only and emit a number.
2. Copy `PositionData.price_current` or session `TickStat` high/low into an “MFE/MAE” column.
3. Treat `GetTickLast` or the 250 ms / 64-symbol poll as `feature_quality=EXACT`.
4. Persist destination FIX quotes into `mt5_xau_ticks` or score them as source excursion.
5. Backfill missing ticks with interpolated mid, last close, or bar typical price and leave quality `EXACT`.
6. Overwrite a deal revision in place with computed excursion (violates ledger immutability **and** §17).
7. Write the database from `onSdkTick` (violates pump-thread constraint #1).
8. Silently use Achiever ticks for a StarwaveFX trade (or the reverse). `server_key` is already on the ledger; ticks need the same key.

Allowed later, and only with labels:

| Source | `price_source` (example) | `feature_quality` | Notes |
|---|---|---|---|
| Manager push + durable writer, no unexplained drops | `{BROKER}_MT5_TICKS` | `EXACT` | Need drop/gap accounting |
| `TickHistoryRequest` for the open window | `{BROKER}_MT5_TICK_HISTORY` | `EXACT` if complete, else `APPROXIMATE` | Not wrapped today |
| `GetChart` OHLC over the window | `BAR_APPROXIMATION` | `APPROXIMATE` | High/low of bars ≠ tick MFE/MAE |
| Poll last-tick samples | `{BROKER}_MT5_TICK_POLL` | `APPROXIMATE` or **null** | Usually too sparse for XAUUSD |
| Missing window | n/a | **omit feature** | Dashboard: “when valid” |

---

## Recommended sequence (do not implement in this audit)

This is a gap list, not a license to edit product source.

1. **Keep MFE/MAE null** until a source tape exists. Reconstruction (§14) and deal ledger can proceed without them.
2. Unify the table name (`mt5_xau_ticks` vs `mt5_ticks_xauusd`) in a migration. Append-only. Key at least `(server_key, symbol, time_msc)` plus `payload_hash` / source id. Store bid, ask, last, volume, flags, broker time.
3. Add `IMT5Client::GetTickHistory(...)` wrapping `TickHistoryRequest` / `TickHistoryRequestRaw`. Fail closed (return false) on HTTP/remote until a real endpoint exists — same pattern as `SubscribeTicks`.
4. Persist **off** the pump thread: a dedicated `IQuoteSink` (or outbox) that writes XAUUSD-canonical symbols only. Never from `onSdkTick`.
5. Subscribe for source XAUUSD aliases for as long as any reconstructed position is open; run history fill for `[opened_at, now]` on reconnect.
6. Feature row: compute MFE/MAE **only** when coverage over the window meets a documented threshold; always persist `price_source` and `feature_quality`.
7. Tests (A09 class `MfeMaeCalculatorTests`): window present → numbers; deals-only → refuse; bars → `APPROXIMATE`; mixed sources → do not silently merge.

---

## Disposition

| Metric | Value |
|---|---|
| Architecture section | §17 (plus §1.5, §11, §45, §51, §60) |
| Exact MFE/MAE possible from current code + DB | **No** |
| Fabricated MFE/MAE found in product source | **No** (feature not implemented) |
| Live tick transport | `MT5TickBridge` (Drogon, optional); push or last-tick poll |
| Durable tick tape | **Missing** |
| `TickHistoryRequest` on `IMT5Client` | **Missing** |
| Ledger writes ticks | **No** — `mt5_raw_events` + `mt5_deals_ledger` only |
| Ledger callers in this repo | **None** (validator test only) |
| `price_source` / `feature_quality` | **Missing** |
| Product source changed | **No** |

**Rule to carry forward:** ticks are required for exact MFE/MAE; the ledger as built is deal evidence only; **do not fabricate** excursion from fills, last-tick polls, session stats, bars, or destination quotes. Omit the feature until a labeled source series exists.
