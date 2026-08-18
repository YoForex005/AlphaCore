# E030 — Honest live vs demo scorecard

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E030_honesty.md` |
| Agent | E030 (honesty / live-vs-demo scorecard only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:16+05:30 (hashes); HTTP 2026-08-18T13:51:13+05:30 / 08:21:14Z |
| Assigned | Write an honest **live vs demo** scorecard. Write this file. Do not modify product source. |
| Product source modified | **No.** Writes: this file + `reports/SWARM_LOG.md` entry. |
| Test source modified | **No.** |
| Authority | Architecture v2 **§68 / §69 / §70** (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`, SHA `0B3C0EDC…`, 50 966 B) |
| Current scorecard siblings | D41 (§69 0/12), D42 (§68 0/19), D43 (§70 0/14), E002 (no send), E008 (FIX `Disconnected`), C42 / C43, D95 (not 5 000), D97 (`CanPromoteToLive` false) |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Running hosts this pass | API `127.0.0.1:5000` (PID 54468 `TraderIntelligence.Api.exe`); web `127.0.0.1:3000` (PID 49100). **No** `mt5-worker`. **No** `fix-worker`. |

**Honesty rule:** a demo table that paints is **not** a live venue. A boolean `healthy=true` with a demo footnote is still a **health lie**. An honest `Disconnected` row is **not** Logon. `SAFE_BY_ABSENCE` is the current send outcome, **not** a §68 / §70 PASS. Fake connector + EF InMemory + `EnsureCreated` + four canned logins **cannot** become accepted §69. Do not greenwash.

---

## 0. Binding verdict

| Bar | Demo (this process, canned book) | Live (Achiever / StarwaveFX / Pepperstone) |
|---|---|---|
| First useful version **§69** | **shape 7/12**, **accepted 0/12** | **0 / 12** |
| Go-live license **§68** | n/a (demo cannot license send) | **0 PASS / 19 FAIL** |
| Live FIX acceptance **§70** | n/a | **0 / 14 FAIL** |
| LIVE trader state | **0** of 4 scored logins | promotion **hard-false** |
| `REAL_COPY_EXECUTION_ENABLED` | display / default **false** | **false**; unread as a send gate |
| Live `NewOrderSingle` / `35=D` | impossible | **SAFE_BY_ABSENCE** (no builder, no initiator, no socket) |
| Live MT5 Manager / HTTP | **not attempted** | **NOT PROVEN** |
| Live QUOTE/TRADE `35=A` | **not attempted** | **NOT PROVEN** |
| Safe to enable real copy? | **No** | **No** |

One-liner:

```text
DEMO works: 4 logins / 18 canned deals / 9 reconstructed XAU / 2 SHADOW + 1 RISK_BLOCKED + 1 INSUFFICIENT_DATA / 0 LIVE.
LIVE does not: no Manager, no FIX TLS, no discovered tag 55, no dest book, no send path.
Accepted §69 = 0/12.  §68 = 0/19.  §70 = 0/14.
```

Do **not** tick any §68 / §69 / §70 checkbox from this file. Do **not** treat a green Overview “MT5 health OK” tile as Achiever connected.

---

## 1. What the two columns mean (binding)

| Token | Meaning | Counts as live? |
|---|---|---|
| **LIVE_PASS** | Running system talks to the named venue, or persists data that originated from that venue / a recorded replay of that venue. | Yes |
| **DEMO_OK** | Fake connector + InMemory/`EnsureCreated` + seeder implements the *shape*. Integration may be green. | **No** |
| **HONEST_ENUM** | Status string admits the socket is down. Still not a session. | **No** |
| **LIE** | API / UI boolean claims connected / healthy / reconciled without a socket or a comparer. | **No** (anti-evidence) |
| **MISSING** | Type, package, table, or caller absent. | **No** |
| **SAFE_BY_ABSENCE** | Cannot send because nothing can send. Correct *outcome*, not a coded gate PASS. | **No** |
| **PARTIAL** | Algorithm or chrome exists; pipeline or truthfulness incomplete. | **No** |

“Demo” in this tree is **not** a broker demo account. It is `FakeMt5BrokerConnector` + `DemoSeeder` + invented dest quote `2399.45` / `2399.85` with `VenueInstrumentId = null`. Seeded group name `demo\Maxmaster` is a **string**, not a live Achiever demo book.

---

## 2. Running surface (measured, not remembered)

| Process | Measured |
|---|---|
| API | `dotnet run --project D:\Prop\apps\api\TraderIntelligence.Api.csproj --urls http://127.0.0.1:5000 --no-launch-profile` → `TraderIntelligence.Api.exe` PID **54468**, listen **127.0.0.1:5000** |
| Web | listen **127.0.0.1:3000**, `GET /` HTTP **200**, 624 B shell |
| `apps/mt5-worker` | **not running** |
| `apps/fix-worker` | **not running** |
| Persistence of the live API | **EF InMemory** (`ConnectionStrings:TraderIntelligence` / `DATABASE_URL` absent; appsettings key is `Postgres`, unused by DI) |
| Postgres / Redis | not required for this API process; `/api/health` redis `healthy=false` |
| Process env `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` / `DATABASE_URL` / `REAL_COPY_EXECUTION_ENABLED` | **ABSENT** (Process / User / Machine) |
| Gitignored `D:\Prop\.env` | **exists** (3408 B, SHA `56C81786…`); API `Program.cs` does **not** load it |
| Tracked `.env.example` | **missing** from worktree (` D` vs HEAD) |

Workers, even if started, would get a **different** InMemory database. They cannot see the API’s seeded book. The 15 s FIX stamp and 30 s MT5 resync are **not** happening on this running API.

FIX session `lastInbound` on the API is the **seed timestamp** `2026-08-18T08:12:16.7895326Z`. `LastError` is still the seeder sentence (“Demo seed only.”), not the worker sentence (“Simulator/demo only.”). That confirms **fix-worker is not attached**.

---

## 3. Live HTTP vs truth (same API, 08:21Z)

`GET` all HTTP **200**. Values are the **demo book**, not venues.

| Endpoint | What it returned | Honest reading |
|---|---|---|
| `/health` | `{ status: ok }` | Process is up. Not venue health. |
| `/ready` | `{ ready: true, brokers: 2 }` | **2 seeded broker rows.** Not Manager ready. |
| `/api/health` | ACHIEVER `healthy: true` + footnote “demo FakeMt5BrokerConnector — not live Manager”; QUOTE `healthy: false`; db `healthy: true`; redis `healthy: false` | MT5 boolean is a **LIE**. FIX boolean is **honest**. |
| `/api/overview` | accounts **4**, connectedBrokers **2**, xauTraders **3**, ≥3 trades **3**, watch **0**, shadow **2**, liveCandidates **0**, live **0**, riskBlocked **1**, shadowPnl **248.20**, dest real P&L **0**, xauGross/Net **0**, mt5Healthy **true**, quoteHealthy **false**, tradeHealthy **false**, realCopyEnabled **false** | Census is the fake factory. `mt5Healthy` is `brokers > 0`. `248.20` is **Σ slippage**, not shadow P&L. FIX bits honest. Copy flag honest. |
| `/api/brokers` | ACHIEVER `57.128.141.65` `connected: true` groups **3** accounts **3**; STARWAVEFX `84.201.6.142` `connected: true` groups **1** accounts **1** | IPs are **catalog paint**. `connected` is a **literal `true`**. |
| `/api/groups` | 4 names: `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`, `real\standard` | Hard-coded in `DemoBrokerFactory`. Not `GroupTotal`. |
| `/api/traders` | 10001 **SHADOW** 95.50 / 223.60; 99001 **SHADOW** 95.50 / 108.2; 10002 **RISK_BLOCKED** 42.50 / −2107 martingale+lotEscalation; 10003 **INSUFFICIENT_DATA** 40 / 0. All `mlProbability: null`. All row `shadowPnl: 0`. | Real scorer on canned tape. Row shadow P&L **not** the overview 248.20. **0 LIVE.** |
| `/api/traders/ACHIEVER/10001` | 3 completed XAU, all `isFirstThree: true` | Demo first-3 highlight. Not `EligibleForFirstThree`. |
| `/api/trades` | **9** completed reconstructed rows (10001×3, 10002×3, 99001×3), all `XAUUSD`, June 2026 | Seeder year window. Not a live tape. |
| `/api/fix/sessions` | QUOTE/TRADE `Disconnected`, `loggedOn: false`, `connected: false`, host `live-us-eqx-01.p.c-trader.com` :5211/:5212, `Sender` implied live.pepperstone.1369850, `instrumentId: null`, bid **2399.45** ask **2399.85**, quoteAge **~538 s**, `executionEnabled: false` | Honest status. **Forged book.** Live identifiers in a demo row. Quote is stale by construction. |
| `/api/risk` | zeros, killSwitch `None`, `realCopyEnabled: false`, `recentRejectReasons: []` | Kill-switch **seed default**. RiskEngine **never called** on this path. |
| `/api/reconciliation/status` | `lastReconciliation: <request UtcNow>`, unknown/mismatch/orphan **0** | **FORGED.** Hardcoded in `Program.cs`. No comparer. |
| `/api/settings` | `REAL_COPY_EXECUTION_ENABLED: false` | Display floor, not a wired choke. |

Trader-state census from `/api/traders` **equals** overview: 2 SHADOW, 1 RISK_BLOCKED, 1 INSUFFICIENT_DATA, **0 LIVE / 0 LIVE_CANDIDATE**.

---

## 4. Capability scorecard (the assigned table)

| Capability | Demo | Live | Class | Evidence (current bytes / HTTP) |
|---|---|---|---|---|
| Connect both MT5 brokers | Fake `ConnectAsync` sets `_connected = true`; dashboard `Connected = true` | **No socket, no DLL, no password** | **LIE** / **FAIL** | Fake L30–42; DI L31–34 always `DemoBrokerFactory`; queries L53 literal `true`; C++ `MT5Manager` unused |
| Discover all groups | 4 fixture names upserted | Manager `GroupTotal` unused | **DEMO_OK** | Factory L101–124; live `/api/groups` = those 4 |
| Sync ~5 000 accounts | **4** logins (0.08% of §69.3) | No 5k path; `SyncCheckpoint` unused | **FAIL** | Factory L107–124; worker/API hard-code the same four |
| Capture XAUUSD deals | **18** canned `XAUUSD` deals, scale 10 000 | No Manager history / live events | **DEMO_OK** | 6+3 closed round-trips × 2 legs |
| Reconstruct logical trades | **9** completed XAU persisted | Not proven on venue tape | **DEMO_OK** | `/api/trades` count 9; `TradeReconstructor` real algorithm |
| First 3 completed XAU | 10001 / 99001 N=3; 10003 N=0 | Same helper, no live book | **DEMO_OK** | Detail `isFirstThree`; persist ignores `EligibleForFirstThree` |
| Deterministic score | 95.50 / 42.50 / 40.00 as above | Same scorer, no live book | **DEMO_OK** | `BaselineScorer`; ML null; `services/` empty |
| Rank traders | `OrderByDescending(EarlyScore)` on 4 rows | Not a 5k leaderboard | **DEMO_OK** | `/api/traders` |
| Promote to LIVE | `CanPromoteToLive => false`; `FromBaseline` cannot emit LIVE | Same pin | **HONEST** pin, **vacuous** machine | `BaselineScorer.cs` L189–211; zero product callers |
| cTrader QUOTE TLS Logon | Status **Disconnected** | **NOT PROVEN** | **HONEST_ENUM** / **FAIL** | Seeder L73; worker would stamp same; no QuickFIX/n |
| Discover Pepperstone tag 55 | `VenueInstrumentId = null`; harness `123456` must not seed | **FAIL** | **FAIL** | Seeder L109; `/api/fix/sessions` `instrumentId: null` |
| Dest quotes | Invented 2399.45 / 2399.85, age 538 s+ | No MD Incremental | **FORGED BOOK** | Seeder L105–113 |
| Shadow copy | 6 `SHADOW_ONLY` fills from invented quote; Σ slip **248.20** | Not dest-tape shadow | **DEMO_OK** | `PersistDemoShadowAsync`; D48; overview `shadowPnl` |
| Live copy / dest positions | `/live` stub; flag false | No dest book, no send | **MISSING** | `LiveCopyPage.tsx` 321 B |
| FIX TRADE Logon | Disconnected + “NewOrderSingle off.” | **NOT PROVEN** | **HONEST_ENUM** / **FAIL** | Seeder L91–101 |
| `NewOrderSingle` send | No function emits `35=D` | **SAFE_BY_ABSENCE** | **MISSING** sender | Product grep 0 `GuardedNewOrderSingle` / `SocketInitiator` / `QuickFIXn` / `TcpClient` / `SslStream` |
| Risk engine before send | 5 unit facts on dead `Evaluate` | Zero product callers | **MISSING** path | Tests only |
| Kill switch | Seeded `None` | Not on a send path | **DEMO_OK** seed | `/api/risk` |
| Reconciliation | Hardcoded zeros + `UtcNow` | **LIE** | **LIE** | `Program.cs` L35–41 |
| RBAC / audit writer | None | None | **MISSING** | C18 / D53 |
| EF migrations | **0** (axios doc only) | **0** | **MISSING** | `EnsureCreatedAsync` |
| Postgres SoT | Optional; this API is InMemory | Not wired (`Postgres` key ≠ `TraderIntelligence`) | **DEMO_OK** default | DI L19–29 |
| SignalR live scores | Client hits `/hubs/dashboard`; API **no** `MapHub` | None | **MISSING** | D50 |
| Ticks / MFE-MAE | Not fabricated (correct) | No tick bridge | **MISSING** | C60 / D56 |
| ML | Correctly not built | Correctly not built | **N/A** (out of §69) | `D:\Prop\services` empty |
| React chrome | 15 pages; overview/traders/groups/FIX paint demo | Cannot show items 1/3/9/10 truthfully | **PARTIAL** | Overview 11/18 tiles; Live/Shadow/Audit stubs |

**Demo-wired (not accepted): 7** — groups, capture, reconstruct, first-3, score, rank, shadow.  
**Live-wired: 0.**

---

## 5. Architecture §69 — first useful version (live vs demo)

Verbatim items. Demo column = “does the canned path implement the shape?” Live column = “accepted against venues?”

| # | Item | Demo | Live accepted | Now |
|---|---|---|---|---|
| 1 | Connect to both MT5 brokers | Fake bool + catalog IPs | **No** | **FAIL** |
| 2 | Discover all groups | 4 fixture names | **No** | **DEMO** |
| 3 | Synchronize ~5,000 accounts | 4 logins | **No** | **FAIL** |
| 4 | Capture XAUUSD trades correctly | 18 canned deals | **No** | **DEMO** |
| 5 | Reconstruct logical trades | Algorithm + 9 rows | **No** | **DEMO** |
| 6 | Detect first 3 completed XAUUSD | N=3 on 10001/99001 | **No** | **DEMO** |
| 7 | Deterministic trader/risk score | Baseline persisted | **No** | **DEMO** |
| 8 | Rank traders | EarlyScore sort | **No** | **DEMO** |
| 9 | Connect to cTrader QUOTE FIX securely | Honest `Disconnected` | **No** | **FAIL** |
| 10 | Discover Pepperstone XAUUSD instrument ID | `null` | **No** | **FAIL** |
| 11 | Shadow-copy using destination quotes | Engine + invented quote | **No** | **DEMO** |
| 12 | Show all of this in React | 15 pages; cannot tell truth on 1/3/9/10 | **No** | **PARTIAL** |

**Accepted: 0 / 12.** Same integer as D41. A57 inventory (Class1 / weatherforecast / 0 pages) is **stale** — do not paste it. The gate did **not** increment.

---

## 6. Architecture §68 — go-live license (all live FAIL)

Demo tests are **not** 19/19. One FAIL blocks `REAL_COPY_EXECUTION_ENABLED`.

| Gate | Text | Demo lab | Live |
|---|---|---|---|
| G01 | MT5 historical/live ingestion stable | Fake 30 s loop; worker **not running**; 30-day window **misses** June 2026 canned deals | **FAIL** |
| G02 | Duplicate event handling proven | InMemory select-then-insert (unique index **not** enforced) | **FAIL** |
| G03 | Trade reconstruction tests pass | 6 synthetic facts | **FAIL** as venue proof |
| G04 | XAU symbol mappings verified | Hardcoded aliases; `SourceSymbolMappings` unseeded | **FAIL** |
| G05 | Quote session stable | No TLS | **FAIL** |
| G06 | Trade session stable | No TLS | **FAIL** |
| G07 | cTrader recon after restart | Endpoint **forges** zeros | **FAIL** |
| G08 | Copy intents idempotent | Shadow key `shadow:{broker}:{login}:{pos}`; no send | **FAIL** |
| G09 | Unknown-state recovery | FSM helper; zero venue callers | **FAIL** |
| G10 | Position sizing verified | Passing facts **passthrough** 0.10; 21 A43 skips | **FAIL** |
| G11 | Risk unit/integration pass | 5 unit facts, **dead** `Evaluate` | **FAIL** |
| G12 | Stale quote rejection | Unit only | **FAIL** |
| G13 | Stale signal rejection | Unit only | **FAIL** |
| G14 | Shadow sample sufficient | 6 invented fills | **FAIL** |
| G15 | Dest costs/slippage measured | Slip vs **seed** 2399 book | **FAIL** |
| G16 | Kill switch tested | Seed `None`; no send coupling | **FAIL** |
| G17 | Secrets out of repo/logs | No live passwords in process env; `.env` exists (gitignored); `appsettings` CTrader password empty | **FAIL** as a completed control |
| G18 | Dashboard venue health/risk | Paints **lies** (MT5 OK) and **honest** FIX down | **FAIL** |
| G19 | Manual review completed | This file is a review, **not** a go-live sign-off | **FAIL** |

**0 / 19 PASS** for live. Same integer as A100 / C14 / D42.

---

## 7. Architecture §70 — live FIX (all FAIL)

| # | Item | Live | What exists (not proof) |
|---|---|---|---|
| 1 | TRADE FIX Logon stable | **FAIL** | Honest `Disconnected`; no `35=A` |
| 2 | ExecutionReports persisted | **FAIL** | No table / applier |
| 3 | Position reports reconcile after restart | **FAIL** | Forged recon JSON |
| 4 | Unique ClOrdID proven | **FAIL** | Factory + unique index; no send |
| 5 | Duplicate report handling | **FAIL** | Harness identity |
| 6 | Unknown-state recovery | **FAIL** | SM helpers only |
| 7 | Partial fills | **FAIL** | Enum map only |
| 8 | Order rejects | **FAIL** | No persist |
| 9 | Cancel/replace | **FAIL** | **MISSING** |
| 10 | Dest position mapping | **FAIL** | **MISSING** type |
| 11 | Risk reject before FIX send | **FAIL** | `Evaluate` unused |
| 12 | Real execution feature flagged | **FAIL** as a **wired** gate; default **false** | POCO `= false`; no sender to choke |
| 13 | Global stop-new-orders | **FAIL** | Unit only |
| 14 | Recon blocks execution while inconsistent | **FAIL** | No recon, no send |

**0 / 14.** Same integer as A101 / D43. `SAFE_BY_ABSENCE` ≠ item 12 PASS.

---

## 8. Safety that is real (do not oversell)

These are **true** and **narrow**:

1. **No live send function.** Product `*.cs` has 0 `GuardedNewOrderSingle`, 0 `NewOrderSingle(`, 0 `35=D` builders, 0 `SocketInitiator`, 0 `QuickFIXn` package, 0 `TcpClient` / `SslStream` in `src/` + `apps/`.
2. **Flag default is false.** `CTraderFixOptions.RealCopyExecutionEnabled = false`. API settings dictionary hardcodes `false`. Overview last arg `false`. Worker `GetValue(..., false)` is log-only.
3. **Scorer cannot emit LIVE.** `TraderStateMachine.FromBaseline` reachable set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. `CanPromoteToLive(*) => false`. Persist copies `SuggestedState`. Live API: `live=0`.
4. **FIX status writers currently say Disconnected.** Seeder L73/L91; worker L32/L40. Mid-wave `LoggedOn` forge is **gone**. That is honesty, not Logon.
5. **ML is not built.** Correct for §69.
6. **C++ `mt5-sdk` is preserved**, not deleted — and **not** the C# collector.

These do **not** authorize `REAL_COPY_EXECUTION_ENABLED=true`.

---

## 9. Lies, latent lies, and anti-evidence

| Surface | Why it is not live proof |
|---|---|
| `/api/health` `mt5Connections[0].healthy = true` | Footnote admits Fake; boolean is still green |
| Overview `mt5Healthy` | `brokers > 0` (queries L39) |
| Brokers `connected: true` | Literal fifth ctor arg (queries L53); `IsConnectedAsync` never read |
| Seeded `57.128.141.65` / `84.201.6.142` | Never passed into the fake |
| Seeded FIX host / `live.pepperstone.1369850` | Live **identifiers** in a demo row |
| Dest bid/ask 2399.45 / 2399.85 | Invented; `VenueInstrumentId = null` |
| Overview `shadowPnl = 248.20` | Σ `SourceVsShadowSlippage`, not P&L; trader rows show `0` |
| `/api/reconciliation/status` | `UtcNow` + zeros every request |
| `/ready` `ready: true` | Broker row count |
| README “~5,000 MT5 accounts” | Measured **4** |
| `TradeSessionEnabled = true` default | Unused, but would be unsafe if a session existed |
| appsettings `CTraderFix:TargetCompId = CSERVER` | Unbound leftover; options POCO uses `cServer` |
| Dashboard maps `LoggedOn` → healthy | **Latent lie** — green again if any writer restores `LoggedOn` |
| `CREDENTIALS_AND_COPY_STATUS.md` “`.env` No” | **Stale.** File exists (E001). |

---

## 10. Demo path that actually works (so the FAIL is not “empty tree”)

On this API process, after `EnsureCreated` + `DemoSeeder`:

```text
DemoBrokerFactory
  ACHIEVER  10001  demo\Maxmaster     3 XAU  → SHADOW          q=95.50  r=10
  ACHIEVER  10002  demo\yo-2step      3 XAU  → RISK_BLOCKED    q=42.50  r=70  martingale
  ACHIEVER  10003  contest\yo-2step   0 XAU  → INSUFFICIENT_DATA q=40   r=10
  STARWAVEFX 99001 real\standard      3 XAU  → SHADOW          q=95.50  r=10
18 deals → 9 reconstructed → 4 scores → 6 shadow fills (10001×3 + 99001×3)
```

`DealIngestionService.SyncBrokerAsync` calls `ConnectAsync` then upserts groups/accounts/deals/positions. `ReconstructionScoringService.RebuildTraderAsync` reconstructs, scores, persists, then `PersistDemoShadowAsync` (SHADOW only). Integration `Demo_seed_discovers_groups_reconstructs_and_scores` locks 2 brokers, N=3 on 10001, **not LIVE**, 10002 `RISK_BLOCKED`, TargetCompId `cServer`.

That is a **lab slice**. It is the right shape for local UI. It is **not** Phase 1 / 4 / 7.

Worker clock hazard: canned deals are `2026-06-01` / `2026-06-02`. `mt5-worker` uses `UtcNow.AddDays(-30)`. On 2026-08-18 that window **excludes** the tape. A started worker would ingest **0** deals from the fake filter. The running API is fine only because the seeder used `2026-01-01` … `2026-12-31`.

---

## 11. What would flip a cell (do not pretend it flipped)

| Cell | Flip when |
|---|---|
| §69.1 LIVE_PASS | Both Manager sessions survive restart; dashboard `Connected` derived from `IsConnectedAsync` / watchdog, not `true` |
| §69.3 | ~5k unique `(broker_id, login)` in Postgres, checkpointed |
| §69.9 | QUOTE TLS `35=A` → `LOGON_OK` record (A25 §3.6) on `live-us-eqx-01.p.c-trader.com:5211` |
| §69.10 | Persisted venue instrument id from SecurityList, **not** `123456`, **not** null |
| §69.11 accepted | Shadow fills from **that** QUOTE tape, not 2399.45/2399.85 |
| §69 accepted | **All 12** live-true. Demo 7/12 does not increment |
| §68 / send | **19/19** and **14/14** and explicit prod review. Then a **wired** flag, then a sender |
| `mt5Healthy` | Must be allowed to go **false** when Fake is the only connector |

---

## 12. File hashes this verdict stands on

Measured 2026-08-18T13:50:16+05:30.

| SHA-256 | Bytes | Path |
|---|---:|---|
| `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 7049 | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` |
| `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 1900 | `src/Infrastructure/DependencyInjection.cs` |
| `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 5082 | `src/Infrastructure/Seeding/DemoSeeder.cs` |
| `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 8708 | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` |
| `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 12097 | `src/Infrastructure/Persistence/EfTradingStore.cs` |
| `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 5951 | `src/Infrastructure/Persistence/TraderDbContext.cs` |
| `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 4535 | `src/Application/Ingestion/DealIngestionService.cs` |
| `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 8143 | `src/Domain/Scoring/BaselineScorer.cs` |
| `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | 8567 | `src/Domain/Risk/RiskEngine.cs` |
| `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | 3249 | `src/Domain/Shadow/ShadowCopyEngine.cs` |
| `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` | 12768 | `src/Domain/Reconstruction/TradeReconstructor.cs` |
| `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | 2344 | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` |
| `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | 419 | `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` |
| `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 4731 | `apps/api/Program.cs` |
| `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | 2093 | `apps/fix-worker/Worker.cs` |
| `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 1882 | `apps/mt5-worker/Worker.cs` |
| `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | 321 | `apps/web/src/pages/LiveCopyPage.tsx` |
| `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` | 628 | `apps/web/src/pages/ShadowPortfolioPage.tsx` |
| `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | 2078 | `apps/web/src/pages/OverviewPage.tsx` |
| `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` | 50966 | architecture v2 |

Hashes match D41 / D42 / E008 for Fake / DI / seeder / queries / workers / API. Tree did not grow a live connector or a QuickFIX package between those pins and this scorecard.

`TraderDbContext` exposes **20** `DbSet`s. Architecture §45 catalogs **43** tables. 20/43 is a demo slice, not the live schema.

---

## 13. Stale-vs-current (do not copy blindly)

| Prior | Stale claim | Current |
|---|---|---|
| A57 item table | Empty Class1 tree, 0 pages | **Forbidden.** Use D41 / this file. Accepted still 0/12 |
| D22 / C43 “seeder LoggedOn” | TRADE `LoggedOn` | Seeder SHA `A6416491…` writes **Disconnected** (E008) |
| B07 / C07 worker LoggedOn | 15 s forge | Worker SHA `92A8F492…` stamps **Disconnected** (D94) |
| CREDENTIALS “`.env` No” | File absent | File **exists**, unused by API (E001) |
| README ~5 000 accounts | Implied census | **4** logins (D95) |
| Overview “Shadow P&L” | Real shadow P&L | Σ slippage 248.20; row field 0 |
| A08 “flag not in C#” | Missing | POCO exists; still not a sender |

---

## 14. What this file does not do

- Does **not** modify product source.
- Does **not** send FIX or open Manager.
- Does **not** print `.env` values.
- Does **not** increment §69 / §68 / §70.
- Does **not** authorize `REAL_COPY_EXECUTION_ENABLED=true`.
- Does **not** claim the React dashboard is “live” because port 3000 returned 200.

```text
DEMO  = canned Fake + InMemory + invented dest quote. Works as a local UI.
LIVE  = Achiever + StarwaveFX Manager + Pepperstone QUOTE/TRADE. Not proven.
SEND  = SAFE_BY_ABSENCE.
PROMOTION = CanPromoteToLive false; live=0.
GATES = 0/12, 0/19, 0/14.
```
