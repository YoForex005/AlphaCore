# C54 — Remaining gaps vs §69: live MT5, live QUOTE logon, real shadow fills

| Field | Value |
|---|---|
| Agent | C54 (senior engineer; remaining-gap re-measure only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C54_remaining_gaps.md` |
| Assigned | Honest remaining gaps vs §69: live MT5, live QUOTE logon, real shadow fills from quotes. Write this report. Do not modify product source. |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§69** (lines 2633–2654) |
| Supporting | §§6–8, 24, 27, 31; A24, A30 I2/I3 + I7 + I8, A35, A57, A72, A86 |
| Predecessors | C13 (§69 0/12 scorecard), C42 (no live MT5), C19 (no QuickFIX/n), B04 / B05 / B18, C07 (send off / LoggedOn forged) |
| Product source modified | **No.** This report is the only write. |

**Bar (verbatim §69):** the first genuinely useful system does **not** need ML. It is accepted only when **all 12 items are true** as an operating system.

```text
1. Connect to both MT5 brokers.
2. Discover all groups.
3. Synchronize ~5,000 accounts.
4. Capture XAUUSD trades correctly.
5. Reconstruct logical trades.
6. Detect the first 3 completed XAUUSD trades.
7. Produce a deterministic trader/risk score.
8. Rank traders.
9. Connect to cTrader QUOTE FIX securely.
10. Discover the Pepperstone XAUUSD instrument ID.
11. Shadow-copy selected traders using destination quotes.
12. Show all of this in React.
```

Live `NewOrderSingle` and Phase 6 ML are **out of this bar**. `REAL_COPY_EXECUTION_ENABLED=false` is the correct default.

---

## 0. Verdict (do not greenwash)

**§69 accepted: still 0 / 12.**  
**The three remaining *venue* gaps that keep the first useful version closed are exactly the assigned three:**

| Remaining live gap | §69 items blocked | Measured class |
|---|---|---|
| **A. Live MT5** — Achiever + StarwaveFX Manager (or HTTP bridge) sessions | **1** (direct); **2–4** cannot become PASS; **5–8** and **12** stay DEMO | **FAIL** |
| **B. Live QUOTE logon** — TLS initiator on 5211, session-driven status, SecurityList | **9** (direct); **10** cannot run; **11** has no dest tape | **FAIL** |
| **C. Real shadow fills from quotes** — persisted fills priced from usable dest QUOTE snapshots | **11** (direct); **12** cannot show a shadow book | **FAIL** |

A demo ingest → reconstruct → score → rank path exists on `FakeMt5BrokerConnector` + InMemory/`EnsureCreated`. That is **not** first useful version. C13’s **accepted 0/12** still holds. This file does not increment any cell.

**Honest one-liner:** C# can replay **18 canned XAUUSD deals** across **4 logins** on **2 in-memory fakes**, stamp `ReadyForMarketData` / `LoggedOn` every 15 s with **no socket**, and display an invented bid/ask whose `VenueInstrumentId` is **null**. It cannot talk to Achiever, StarwaveFX, or Pepperstone QUOTE, and it has never written a shadow fill from a destination quote.

Do **not** treat:

- `FakeMt5BrokerConnector.ConnectAsync` setting `_connected = true`
- `EfDashboardQueries` hard-coding broker `Connected = true`
- `/api/health` `{ healthy: true, details: "demo connector" }`
- `apps/fix-worker` writing `ReadyForMarketData` / `LoggedOn` on a 15 s timer
- seeded `2399.45` / `2399.85` with `VenueInstrumentId = null`
- unused `ShadowCopyEngine` + empty `shadow_orders`
- `docs/architecture.md` “Implemented toward first useful version … FIX session *state*”

as §69 acceptance.

Siblings this file does **not** walk back: C42 (live MT5 not proven), C19 (QuickFIX/n absent), B18 (engine is a taker-touch calculator), C07 (send off by absence; LoggedOn forged), C13 (0/12).

---

## 1. Method

Read-only re-measure of the current worktree against §69. Did **not** open Achiever `:443`, StarwaveFX `:443`, or `live-us-eqx-01.p.c-trader.com:5211`. Did **not** edit `src/`, `apps/`, `tests/`, or `mt5-sdk/`.

| Step | What |
|---|---|
| 1 | Read §69 + §§6–8, 24, 27, 31 and A24 acceptance (item 11). |
| 2 | Re-read C13, C42, C19, B04, B05, B18, C07, A30 I2/I7/I8. |
| 3 | Full-read Fake connector, DI, seeder, both workers, API health, dashboard queries, `ShadowCopyEngine`, `CTraderFixOptions`, Fix.CTrader csproj, `DealIngestionService`. |
| 4 | Grep product `*.cs` / `*.csproj` for a second `IMt5BrokerConnector`, `HttpClient`, `DllImport`, `QuickFix` / `QuickFIXn`, `CTraderQuoteSession`, `SocketInitiator`, `ShadowCopyEngine` callers, `DestinationQuotes` writers. |
| 5 | Confirm C++ `MT5Manager::Connect` exists adjacent and is **not** called from C#. |
| 6 | SHA-256 the files this verdict stands on. |

---

## 2. Evidence hashes (measured 2026-08-18)

Hashes match C13 / C14 / C42 for the overlapping set. The tree has **not** grown a live transport since those reports.

| SHA-256 | Path |
|---|---|
| `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` |
| `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | `D:\Prop\apps\mt5-worker\Worker.cs` |
| `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | `D:\Prop\apps\fix-worker\Worker.cs` |
| `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | `D:\Prop\apps\api\Program.cs` |
| `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` |
| `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` |
| `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` |
| `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| `2EE8B969C4B069A340053D6F5A868D1E7E38769F6A8A2AD74C80626D1FF38B83` | `D:\Prop\mt5-sdk\config\app_config.h` |
| `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` | `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx` |

Grep of `D:\Prop\src` + `D:\Prop\apps` `*.cs` / `*.csproj` for `HttpClient`, `DllImport`, `QuickFix`, `QuickFIXn`, `SocketInitiator`, `CTraderQuoteSession`: **zero hits**.

---

## 3. Full §69 scoreboard (what remains vs what is demo)

Rubric copied from C13. **DEMO / PARTIAL / FAIL do not count as accepted.**

| # | §69 item | Maturity now | Remaining live gap? |
|---|---|---|---|
| 1 | Connect to both MT5 brokers | **FAIL** — fake `ConnectAsync` + health lie | **Yes — Gap A** |
| 2 | Discover all groups | **DEMO** — 4 canned names persisted | Blocked by A (no Manager `GetAllGroups`) |
| 3 | Synchronize ~5,000 accounts | **FAIL** — 4 logins; `SyncCheckpoint` unused | Blocked by A + no checkpointed 5k path |
| 4 | Capture XAUUSD trades correctly | **DEMO** — 18 canned `"XAUUSD"` deals | Blocked by A (no live `GetDeals`) |
| 5 | Reconstruct logical trades | **DEMO** — real algorithm on canned deals | Blocked by A (no live/recorded venue tape) |
| 6 | Detect first 3 completed XAUUSD trades | **DEMO** — counter + persist on fixtures | Blocked by A |
| 7 | Deterministic trader/risk score | **DEMO** — `baseline.v1`, ML null (correct) | Blocked by A |
| 8 | Rank traders | **DEMO** — sort 4 scores by early quality | Blocked by A |
| 9 | Connect to cTrader QUOTE FIX securely | **FAIL** — no initiator; worker stamps healthy | **Yes — Gap B** |
| 10 | Discover Pepperstone XAUUSD instrument ID | **FAIL** — null + forbidden harness `123456` | Blocked by B |
| 11 | Shadow-copy selected traders using dest quotes | **FAIL** — engine unused; no dest tape | **Yes — Gap C** (depends on B) |
| 12 | Show all of this in React | **PARTIAL** — 15 pages over demo + lies | Blocked by A+B+C; health lies are worse than empty |

**Accepted: 0 / 12.**  
**Demo-wired (not accepted): 2, 4, 5, 6, 7, 8.**  
**The three assigned remaining live gaps: 1, 9, 11** (and they keep 2–8, 10, 12 from flipping).

---

## 4. Gap A — Live MT5 (Achiever + StarwaveFX)

### Law

§69.1 + §§6–8, 10. Two independent Manager sessions, reconnect-safe, `broker_id` isolation. §7: enumerate **all** Manager-visible groups. §69.3: ~5k accounts, checkpointed. §69.4: capture real XAU (or alias) deals.

A30 delivering increment: **I2** (collectors + C# HTTP adapter) then **I3** (deals / checkpoints).

### What exists (not live)

| Surface | Path | Measured |
|---|---|---|
| Only C# implementor | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` L6–42 | `ConnectAsync` sets `_connected = true`. No socket, no password, no host. |
| Dual “brokers” | same file `DemoBrokerFactory.CreateDefault` L95–127 | Two in-memory instances labeled `"ACHIEVER"` / `"STARWAVEFX"`. **4 groups, 4 logins, 18 deals.** |
| Production DI | `src/Infrastructure/DependencyInjection.cs` L31–34 | **Always** `DemoBrokerFactory.CreateDefault()`. No env switch, no slot binder. |
| Worker | `apps/mt5-worker/Worker.cs` L29–35 | Every 30 s: `SyncBrokerAsync` both codes, then rebuild **the same four logins**. |
| Worker config | `apps/mt5-worker/appsettings.json` | Logging only. **No** `MT5_*` / `MT5_STARWAVEFX_*` / collector URLs. |
| Ingest | `DealIngestionService.SyncBrokerAsync` L31–58 | Connect → groups → accounts → deals → positions. Shape is right; transport is fake. |
| Seed metadata | `DemoSeeder.cs` L29–59 | Writes Achiever `57.128.141.65:443` login `2027` and StarwaveFX `84.201.6.142:443` login `9904` as **rows**, not connections. Fake never reads `Server` / `Port` / `ManagerLogin`. |
| Dashboard lie | `EfDashboardQueries.GetBrokersAsync` L53 | `Connected = true`, `LastEventAt = DateTimeOffset.UtcNow`. Never calls `IsConnectedAsync`. |
| API lie | `apps/api/Program.cs` L26–33 | `mt5Connections: [{ name: "ACHIEVER", healthy: true, details: "demo connector" }]`. |
| Options unused | `src/Mt5/Configuration/Mt5BrokerOptions.cs` | Documents remote/local. **Not bound.** |
| Dead port | `src/Mt5/Connectors/IBrokerConnector.cs` | Zero implementers (B24). |
| C# project | `src/Mt5/TraderIntelligence.Mt5.csproj` | Domain + Application refs only. No `HttpClient`, no native, no P/Invoke. |
| Checkpoints | `TraderDbContext` `SyncCheckpoints` | Entity + unique `(BrokerId, Login, Stream)` exist. **No writer.** |
| C++ (adjacent) | `mt5-sdk` `MT5Manager::Connect`, `MT5Pool`, `IMT5Client` | Real Manager client. **Single-broker** `AppConfig` (`mt5_server` / `mt5_login` only). **Not called by any C# host.** |

`IMt5BrokerConnector` (`Mt5Contracts.cs` L53–63) is thinner than §6: no `GetOrdersAsync`, no `SubscribeAsync`, no `GetServerTimeAsync`. That is a secondary port gap; the primary remaining gap is **no live implementation at all**.

### Why items 2–8 cannot flip without A

- **2** — `GetGroupsAsync` returns the fixture list. C++ `GroupTotal` / `GetAllGroups` is unused. C10 is correct that the fake is **not plan-filtered**; that does not make discovery live.
- **3** — Four logins. No paging, no 5k timing, no `sync_checkpoints` stream `'accounts'`.
- **4** — Every fixture symbol is the literal `"XAUUSD"`. No Manager `GetSymbol*`, no observed aliases, no live ticket stream.
- **5–8** — Algorithms persist and unit-test on those 18 deals. Accepted only when they run on live or **recorded** venue history (C13 rubric). They have not.

### Done when (Gap A)

1. Two real `IMt5BrokerConnector` instances (A30: Windows collectors wrapping `MT5Manager`/`MT5Pool` + C# `Mt5CollectorClient`, **or** a measured equivalent). Same implementation, two configs.
2. Process logs a **real** Manager Connect for `ACHIEVER` and `STARWAVEFX` after restart. Health is `IsConnectedAsync` / watchdog, not a constant.
3. Killing one socket does not drop the other.
4. `mt5_groups` contains every Manager-visible group on both brokers, including names **not** in `MT5_GROUP_*`.
5. Account backfill is checkpointed, unique on `(broker_id, login)`, ~5k order of magnitude.
6. A known XAU (or alias) deal on **each** live broker survives restart exactly once; native volume recoverable at scale **10 000**.
7. Dashboard / `/api/health` stop claiming connected when the socket is down.

Passwords stay in env/user-secrets. Never log proxy secrets. Do not copy dealer / create-user / `SendTrade` onto the source collector (A04 / A85).

---

## 5. Gap B — Live QUOTE logon

### Law

§69.9 + §§25–27, 31. Independent QUOTE session, TLS, issued Comp/Sub IDs, `TargetCompID = cServer` (case preserved). TRADE send is **not** required for this bar. §69.10: discover Pepperstone XAU via `35=x` / `35=y`; **never hardcode tag 55** (A86).

A30 delivering increment: **I7**. Exit: “QUOTE Logon over TLS; Security List persisted; Pepperstone XAU instrument ID in `destination_symbols`; live bid/ask + `quote_age` on dashboard; TRADE send still impossible.”

Official package pin (A35 / C19): `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1**. Unofficial `QuickFix.Net` is forbidden.

### What exists (not a session)

| Surface | Path | Measured |
|---|---|---|
| Session type | `src/Fix.CTrader/` | Four sources: options, pipe parser, in-memory ownership, string harness. **`CTraderQuoteSession` absent.** |
| Engine package | `TraderIntelligence.Fix.CTrader.csproj` | **No** `PackageReference` at all (worktree). C19: official QuickFIX/n never referenced; unofficial `QuickFix.Net` removed from worktree, unused on HEAD. |
| Options | `CTraderFixOptions.cs` L10–57 | Host `live-us-eqx-01.p.c-trader.com`, QUOTE SSL **5211**, `TargetCompId = "cServer"` (good), `UseSsl = true`. Password empty. `TradeSessionEnabled = true` (should stay dark for FUV). `QuoteEnabled` unused by any host. |
| Seeded lie | `DemoSeeder.cs` L68–101 | QUOTE row `Status = ReadyForMarketData`; TRADE `LoggedOn`; seq 1; timestamps = now. |
| Worker lie | `apps/fix-worker/Worker.cs` L28–40 | Every 15 s: `LastInboundAt = UtcNow`; QUOTE → `ReadyForMarketData`; TRADE → `LoggedOn` **regardless of `real`**. No socket. Never constructs the harness. |
| FIX worker config | `apps/fix-worker/appsettings.json` | Logging only. No host/port/CompIDs. |
| Dashboard | `EfDashboardQueries` L40–41, L125–147 | Treats those statuses as healthy / logged on. Surfaces seeded bid/ask; `VenueInstrumentId` **null**. |
| UI | `FixSessionsPage.tsx` L14–19 | Renders the lie as “Connected / Logged on”. Instrument line honestly says `not discovered yet` when null. |
| API health | `Program.cs` L28–29 | `{ name: "QUOTE", healthy: true }` with no session object. |
| Dictionary / cfg | product tree | **No** `FIX44-CSERVER.xml`, no `quote.cfg`, no `tests/Fix`. |
| Forbidden fixture | `FixSimulationHarness.SimulateSecurityList` L129–143 | Hard-codes `(55, "123456")` + `(1007, "XAUUSD")`. A86 forbids treating this as the venue id. |
| Seeded dest quote | `DemoSeeder.cs` L103–111 | One snapshot: `VenueInstrumentId = null`, bid `2399.45`, ask `2399.85`. **Only writer** of `DestinationQuotes`. |

`cServer` case is preserved in the worktree (C09 / C21). That is header hygiene, not Logon.

TRADE `LoggedOn` in seed + worker is an extra **health lie**. §69 does **not** require TRADE logon. The remaining gap is QUOTE. TRADE may stay “not started” / disconnected.

### Dependent item 10

SecurityList cannot run without a QUOTE session (live or recorded replay). Until then:

- `destination_quotes.venue_instrument_id` stays null.
- Tag 55 in later MD/ER cannot be a discovered id.
- `123456` must not be written to `destination_quotes`.

### Done when (Gap B)

1. QuickFIXn 1.14.1 QUOTE initiator completes Logon on **TLS 5211** with issued Sender/Target Comp/Sub IDs (`57=QUOTE` and `50=QUOTE` per A08/A25).
2. `fix_sessions` status / last inbound / seq are driven by the session, **not** a 15 s timer or seeder.
3. Password never logged or returned to React.
4. A SecurityList (live or recorded) yields a **persisted** numeric Pepperstone XAU id; later MD uses that id.
5. Live bid/ask + receive clock + quote age persist to `destination_quotes` (history, not a single overwritten seed row).
6. TRADE session is not started for FUV. `REAL_COPY_EXECUTION_ENABLED` stays false. Worker must **refuse** to start TRADE if someone flips the flag (A30 I7 guard).
7. Dashboard QUOTE card is red when Logon is down.

---

## 6. Gap C — Real shadow fills from destination quotes

### Law

§69.11 + §24 + A24 §19 + A72. Selected `SHADOW` traders; every fill price from a **persisted** destination QUOTE snapshot (bid or ask); OPEN/INCREASE fail closed on stale / wide / moved / QUOTE-down; REDUCE/CLOSE uses the close waterfall; no TRADE `35=D`.

A30 delivering increment: **I8**. Exit: “selected `SHADOW` traders generate idempotent `copy_intents`; shadow entries/exits priced from `destination_quotes`; stale quote/signal rejected; source-vs-shadow slippage rows exist; no FIX order leaves the process.”

A24 §19.1 (binding): *Every shadow fill price is taken from a persisted destination QUOTE snapshot. No source-price fills.*

### What exists (not a pipeline)

| Surface | Path | Measured |
|---|---|---|
| Engine | `src/Domain/Shadow/ShadowCopyEngine.cs` | In-memory `SimulateEntry` / `SimulateExit` / `MarkToMarket`. Happy path **does** take dest bid/ask (not source). Then **mutates** price by `0.05` if delay > 250 ms. **No quote guards.** Age/spread recorded, never blocking. |
| Callers | grep `ShadowCopyEngine` / `SimulateEntry` in `*.cs` | **Definition only.** Not in DI. Not called from worker, seeder, API, or tests. |
| Persist shape | `Domain/Entities/ShadowOrder.cs` | Price, spread, slippage. **No** `fill_quote_id`, no quote age, no bid/ask snapshot, no mark quality. |
| Table use | `EfDashboardQueries` L21 | `Sum(SourceVsShadowSlippage)` over empty set → **0**. |
| Dest quote SoT | `DestinationQuotes` | Seeded once (`VenueInstrumentId = null`). Never updated from FIX. Dashboard reads latest for the FIX card only. Engine does **not** read this table. |
| Two quote types | `Risk.DestinationQuote` vs `DestinationQuoteSnapshot` | Incompatible. Engine takes the Risk DTO. Entity is unused by the engine (B18). |
| Risk guards | `RiskEngine.Evaluate` L95–111 | Has `QUOTE_MISSING` / `QUOTE_STALE` / `SPREAD_TOO_WIDE` / `PRICE_MOVED_TOO_FAR` for **OPEN**. **Not registered in DI.** Unused by any shadow pipeline. |
| CopyIntent | `CopyIntents` DbSet + expiry helper | No writer that emits SHADOW intents from reconstructed events. |
| Quantity | `QuantityNormalizer` | Unused by `ShadowCopyEngine` (unit test even asserts this). 0.10 source lots would be blindly 0.10 dest if anyone called it. |
| UI | `ShadowPortfolioPage.tsx` | Static paragraph. **No API.** Copy says fills appear after approved CopyIntent — none are written. |
| Tests | `tests/Unit`, `tests/Replay` | **No** `ShadowCopy*` tests. **No** replay of recorded quotes + MT5 events. |

B18 classification still holds: **`EXISTS_NEEDS_REFACTOR` as a taker-touch calculator. `MISSING` as a destination-quote engine.**

Even if someone called `SimulateEntry` today, it would still fail A24/A72:

1. Delay > 250 ms **invents** a fill 5 cents off the dest touch — that is not the persisted snapshot.
2. Crossed, 30-second-old, or missing books still produce a number.
3. No post-delay re-read of the quote tape.
4. No `fill_quote_id` stored.
5. CLOSE always invents a fill (no `STALE_QUOTE` / `UNPRICED` waterfall).
6. Source deal price is used for slippage math; that is allowed as a **comparison**, not as the fill.

Gap C **cannot** pass before Gap B: there is no usable dest QUOTE tape. Wiring the unused engine to the seeded `2399.45/2399.85` row would be another demo, not item 11.

### Done when (Gap C)

1. Traders in `SHADOW` emit persisted `copy_intents` (idempotent) from reconstructed source events — not from a seeder.
2. Every shadow fill price is the dest bid (sell/exit-long / entry-short) or dest ask (buy/entry-long / exit-short) of a **persisted** snapshot, with FK or embedded bid/ask/`quote_received_at`/`quote_age_ms`/`spread`.
3. OPEN/INCREASE reject `QUOTE_STALE` / `SPREAD_TOO_WIDE` / `PRICE_MOVED_TOO_FAR` / `QUOTE_MISSING` / QUOTE-down / stale signal / catch-up backlog.
4. REDUCE/CLOSE of an existing shadow position is not blocked by OPEN guards; unpriced ≠ 0.
5. Quantity goes through the documented converter + step/min/max. Source lots are not dest lots.
6. Conservative MTM uses dest bid (longs) / dest ask (shorts).
7. Shadow page lists those fills. Source ticks are never the fill price.
8. **No** `35=D`. `REAL_COPY_EXECUTION_ENABLED` remains false.
9. Unit + replay tests lock A24 §19.12–13.

---

## 7. How the three gaps keep the rest of §69 closed

```text
Gap A  live MT5
   ├─ 1 connect both brokers
   ├─ 2 discover all groups
   ├─ 3 ~5k accounts + checkpoints
   ├─ 4 capture XAU deals
   ├─ 5 reconstruct   ─┐
   ├─ 6 first-3        ├─ algorithms exist; input is canned
   ├─ 7 baseline score ┤
   └─ 8 rank           ┘

Gap B  live QUOTE logon
   ├─ 9 TLS Logon 5211 (session-driven status)
   ├─ 10 SecurityList → persisted instrument id
   └─ dest quote tape (bid/ask/age)

Gap C  real shadow fills   (requires Gap B tape + Gap A/5 events)
   └─ 11 SHADOW book priced from destination_quotes

12 React   can only show 1–11 truthfully after A+B+C
           current green badges are anti-evidence
```

Item 12 is **PARTIAL** chrome over lying APIs. Deleting the health lies is required, but empty-honest tiles are still not “show all of this” until A–C exist.

---

## 8. What is **not** a remaining §69 gap

| Topic | Status | Why it is not a §69 remaining gap |
|---|---|---|
| ML / XGBoost / `services/ml-service` | Empty `D:\Prop\services` | §69 first sentence; A52 / A104 / B39 / C44. Absence is **correct**. |
| Live `NewOrderSingle` / TRADE send | No send path | Out of bar (§70). C07 `SAFE_BY_ABSENCE`. Do not build this to close §69. |
| TRADE Logon | Seeded/stamped `LoggedOn` is a lie | §69 needs QUOTE only. TRADE card may say “not started.” |
| Kafka, K8s, ClickHouse, LLM, DNN, RL | Absent | §71 / A80. |
| Domain reconstructor / `BaselineScorer` / rank query | Exist and persist on demo data | Remaining work is **venue input**, not “write another scorer.” |
| C++ `mt5-sdk` deleted | Preserved (C20) | Remaining work is **wire it** (or the HTTP bridge), not rewrite Manager API in C#. |

Secondary product gaps (migrations, RBAC, SignalR hub, Serilog/OTel, `/api/v1` envelopes) are real but they are **not** the assigned remaining §69 venue gaps. C29: **0** EF `Migrations/`; hosts `EnsureCreatedAsync`. Default DI is InMemory when the connection string is empty. Those block a durable operating system; they do not substitute for live sockets.

---

## 9. Flip list (minimum to move the three cells)

Follow A30. Do **not** enable TRADE `NewOrderSingle` as part of this list.

| Gap | A30 | Flip to PASS |
|---|---|---|
| A live MT5 | I2 then I3 | Two collectors (Achiever `:9101`, StarwaveFX `:9102`) wrapping existing `MT5Manager`/`MT5Pool`; C# `Mt5CollectorClient` implements `IMt5BrokerConnector`; DI stops hard-wiring `DemoBrokerFactory`; worker jobs for connect / groups / checkpointed accounts / deals; dashboard reads `IsConnectedAsync`. |
| B live QUOTE | I7 | `QuickFIXn.Core` + `QuickFIXn.FIX44` 1.14.1; `CTraderQuoteSession`; cTrader RoE dictionary; TLS 5211 Logon; SecurityList → persisted XAU id; MD → `destination_quotes`; fix-worker **throws** if TRADE/real-copy flags are on; delete the 15 s status stamp. |
| C shadow fills | I8 | Outbox / reconstructed events → `CopyIntent` → risk (shadow subset) → `ShadowCopyEngine` that **fails closed** and stores `fill_quote_id`; persist orders/fills/positions/P&L; React Shadow table; replay fixtures of recorded quotes + MT5 events. |

Operator probes against live venues stay **out of CI**. Recorded FIX / Manager fixtures are the first proof (A27 / A67). A live attach is required before calling items 1 and 9 accepted (C13 rubric: intended venues or recorded replay of those venues — a fake connect is neither).

---

## 10. Honesty line

**First useful version accepted: 0 / 12.**

| Assigned remaining gap | Status |
|---|---|
| Live MT5 (Achiever + StarwaveFX) | **NOT PRESENT.** Fake connector + health lie. C++ SDK unused by C#. |
| Live QUOTE logon | **NOT PRESENT.** No QuickFIX/n, no initiator, worker forges `ReadyForMarketData`. |
| Real shadow fills from quotes | **NOT PRESENT.** Engine unused; dest quote is a seeded invention with null instrument id; `shadow_orders` empty. |

**Demo pipeline (fake MT5 → reconstruct → baseline score → rank → some React tables): items 2, 4, 5, 6, 7, 8.**  
**ML: correctly not built.**  
**Live copy: off by absence.**

C54 does not authorize product edits. Re-score after a live (or recorded-replay) MT5 transport **and** a QUOTE initiator exist; do not increment the accepted count for more demo rows, more stamped timestamps, or calling `SimulateEntry` on the seed quote.
