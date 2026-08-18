# D93 — A57’s 0/12 inventory is **STALE** (the §69 gate is not)

| Field | Value |
|---|---|
| Agent | D93 (A57 stale-pin; read-only of product) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:45+05:30 / 2026-08-18T08:14:45Z |
| Artifact | `D:\Prop\reports\swarm\20260818\D93_a57_stale.md` |
| Assigned | **A57 0/12 is stale.** Write this file. Do not modify product source. |
| Subject | `D:\Prop\reports\swarm\20260818\A57_first_useful_version.md` |
| Subject SHA-256 | `C1E94C992B28818FAF23D9D6923E2EF56877FE205BA1D64334E5294BC784455E` (36 916 B, 636 lines, LastWriteUtc `2026-08-18T07:42:27.9498701Z`) |
| Current scorecard | `D:\Prop\reports\swarm\20260818\D41_fuv_now.md` (SHA-256 `A9B68AB9A48794148FF472CE8B504E8473BE445B8B54BD611C6B0691EFE951BF`) |
| Intermediate recensus | `C13_fuv_scorecard.md` (also stale on item-11 FAIL + forged LoggedOn) |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§69** (lines 2633–2654) |
| Product source modified | **No.** This report is the only write. |
| Method | Re-read A57’s executive table + “what exists” census. Re-hash the files A57 named. Confirm `Class1.cs` / plural EF configs / `weatherforecast` / empty `pages/` / empty tests are gone. Cross-check D41 hashes (unchanged). Prefer false negatives over fake PASS. |

**Assigned answer:** **Yes — A57’s 0/12 is a stale *inventory*.** It measured an empty Application `Class1`, a non-compiling plural-type `TraderDbContext`, `GET /weatherforecast`, 1 Hz worker templates, **0** React pages, and **0** tests. That tree is gone. **Do not paste A57’s item table as the current repo.**

**The §69 acceptance integer is still 0/12.** D41 re-scored the *current* tree and kept the gate. Stale inventory ≠ flipped acceptance.

---

## 0. Verdict (do not greenwash)

| Claim | Status |
|---|---|
| Cite A57 as the current first-useful scorecard | **FORBIDDEN** — inventory **STALE** |
| Cite A57 “running system does not exist” / “Phase 0 → early Phase 2 sketches” | **STALE** — a **demo** ingest → reconstruct → score → rank → some React tables now exists |
| Cite A57 item cells (Application `Class1`, EF will not compile, ranking **MISSING**, 0 pages, 0 tests, unused shadow) | **STALE** |
| Cite A57 **accepted 0/12** as the *gate* | **Still true** (D41). The *reasons* A57 gave for 0/12 are not. |
| Current accepted §69 | **0 / 12** (D41) |
| Current demo-wired (not accepted) | **7 / 12** — items **2, 4, 5, 6, 7, 8, 11** (D41) |
| Current UI | item **12 PARTIAL** (15 pages; cannot show 1/3/9–10 truthfully) |
| Current FAIL | items **1, 3, 9, 10** |
| Use instead | **D41** for the living scorecard. A57 remains useful as §69 item *definitions*, Done-when lines, and the I0–I6 sequence. |

```text
A57 0/12  =  historical empty-tree snapshot   → STALE as inventory
D41 0/12  =  current accepted gate            → BINDING
C13 0/12  =  mid-wave recensus                → STALE on item 11 FAIL + forged LoggedOn
```

Vacuous / demo law (binding, same as C13/D41): Fake connector, InMemory/`EnsureCreated`, canned 18 deals / 4 logins, a more honest `Disconnected` stamp, or 15 React routes over lying health **cannot** become PASS.

---

## 1. What A57 actually recorded

A57 (same calendar day, earlier write) scored Architecture §69 against a **scaffold**:

| # | A57 classification | A57 accepted? |
|---|---|---|
| 1 | C++ EXISTS_NEEDS_REFACTOR (one broker); C# connector **MISSING** impl | **No** |
| 2 | C++ group APIs exist; collector persist **MISSING** | **No** |
| 3 | C++ user/account reads exist; checkpointed sync **MISSING** | **No** |
| 4 | C++ `GetDeals` + ledger helper; C# ingest/XAU filter **MISSING** | **No** |
| 5 | In-memory `TradeReconstructor` EXISTS_NEEDS_REFACTOR; persist/worker **MISSING** | **No** |
| 6 | Counter methods exist; durable first-3 **MISSING** | **No** |
| 7 | `BaselineScorer` EXISTS_NEEDS_REFACTOR; no persist/job/tests | **No** |
| 8 | **MISSING** ranking query/API | **No** |
| 9 | Options + parser/harness only; no QuickFIX/n session | **No** |
| 10 | **MISSING** (harness hardcodes `123456`) | **No** |
| 11 | In-memory `ShadowCopyEngine` stub; no dest quotes, no persist | **No** |
| 12 | Vite shell + routes to **missing pages**; API is weatherforecast | **No** |

Headline sentences that must not be repeated as current:

> “Honest measured state: Domain algorithms are starting to appear; **the running system does not exist.**”
>
> “treat the tree as Phase 0 → early Phase 2 **sketches**. The first useful version has not started operating.”

Those were true of the A57 snapshot. They are **false** of the tree measured at 13:44:45 +05:30. What exists now is a **demo operating path**, still **not** a first useful version.

---

## 2. A57 “what exists” census vs disk now

Every row A57 used as a cross-cutting fact. **STALE** means the sentence is no longer true. **HOLDS** means the same defect is still on disk.

| A57 sentence | Measured now (13:44:45 +05:30) | A57 cell |
|---|---|---|
| Application is `Class1.cs` — empty `dotnet new` leftover. **No ports, no use-cases.** | `D:\Prop\src\Application\Class1.cs` **MISSING**. Product slice is 3 authored files: `Contracts\Mt5Contracts.cs` (1 858 B, SHA `8430978B…`), `Dashboard\DashboardModels.cs` (3 088 B, SHA `9A3888AE…`), `Ingestion\DealIngestionService.cs` (4 535 B, SHA `2637D97B…`). Ports: `IMt5BrokerConnector`, `IBrokerRegistry`, `ITradingStore`, `IDashboardQueries`. Use-cases: `DealIngestionService.SyncBrokerAsync`, `ReconstructionScoringService.RebuildTraderAsync`. | **STALE** |
| Infrastructure DbSets name plural types that **do not exist**. Remaining 16 `*Configuration` types **absent**. **This project will not compile.** | `TraderDbContext` binds singular Domain entities (`Broker`, `Mt5Group`, …). Fluent map in-file. `Persistence\Configurations\` has **0** files. SHA `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB`. Compiles (D03/D19/D41). **0 migrations** still. | **STALE** (compile). **HOLDS** (no migrations). |
| C# MT5: `IBrokerConnector` + options + `DeterministicGuid`. **No implementation.** No HTTP, no P/Invoke, no registry. | `FakeMt5BrokerConnector : IMt5BrokerConnector` (7 049 B, SHA `AE7C1B1B…`) is the **only** implementer. DI **always** `DemoBrokerFactory.CreateDefault()` (Achiever + StarwaveFX fakes). `IBrokerConnector` still has **zero** implementers. **No** `HttpClient` / `DllImport` / QuickFIX under `D:\Prop\src`. | **STALE** (impl exists). **HOLDS** (not live Manager). |
| API is stock `GET /weatherforecast`. No `/api/v1`, no DbContext, no SignalR. | `apps\api\Program.cs` SHA `61B1E0D1…` (4 731 B). Unversioned `/api/*` maps (health, brokers, groups, traders, trades, risk, FIX, settings, resync). Seeds demo on startup. **0** `weatherforecast` / `WeatherForecast` hits under `apps/`. `launchUrl` is `swagger`. No `MapHub`. | **STALE** (weatherforecast). **HOLDS** (not `/api/v1`; no hub). |
| MT5 worker is 1 Hz `LogInformation`. Empty `appsettings.json`. | `apps\mt5-worker\Worker.cs` SHA `57499700…`: 30 s loop, `SyncBrokerAsync` both fake brokers, rebuild logins `10001/10002/10003/99001`. `appsettings.json` still logging-only. | **STALE** (1 Hz template). **HOLDS** (Fake only; 4 logins). |
| FIX worker is the same 1 Hz template. | `apps\fix-worker\Worker.cs` SHA `92A8F492…`: 15 s stamp of QUOTE/TRADE rows to **`Disconnected`**. No socket. | **STALE** (1 Hz). **HOLDS** (no TLS Logon). |
| React: `App.tsx` routes 14 pages. **`src/pages` has 0 files.** | **15** page modules on disk (see §4). Router SHA `A0E92C97…`. | **STALE** |
| Tests: projects only. **No test classes.** | Unit: 10 authored `.cs` (recon, scorer, risk, volume, symbol, sizing, deal-reason, placeholder). Integration: `SeedingAndStoreTests.cs` + placeholder. | **STALE** |
| C++ `AppConfig` is one broker; no `broker_id`. | Unchanged. C++ still unused by C# hosts. | **HOLDS** |
| Shadow: no dest quotes, no persist. | `DemoSeeder` writes a dest snapshot (`VenueInstrumentId = null`, 2399.45 / 2399.85). `EfTradingStore.PersistDemoShadowAsync` (store SHA `DC03BBE6…`) writes `CopyIntent` + `ShadowOrder` for `SHADOW` from that row. Engine is constructed in-store, not DI. | **STALE** (unused stub). **HOLDS** (not a dest-quote venue tape). |
| Ranking query/API **MISSING**. | `EfDashboardQueries.GetTradersAsync` `OrderByDescending(t => t.EarlyScore)` (SHA `328D0924…`). `GET /api/traders`. Not A92 / not `/api/v1`. | **STALE** (missing). **HOLDS** (demo ranker of 4 rows). |
| `TargetCompId` defaults to `"CSERVER"`. | Worktree options default `cServer` (D26). API leftover JSON may still say `CSERVER` and is **not bound**. | **STALE** as a live default. Case hazard still documented. |

A57 evidence-index paths that **no longer exist**:

| A57 cited path | Now |
|---|---|
| `D:\Prop\src\Application\Class1.cs` | **MISSING** |
| `Configurations\BrokersConfiguration.cs` | **MISSING** |
| `Configurations\Mt5GroupsConfiguration.cs` | **MISSING** |
| `Configurations\Mt5AccountsConfiguration.cs` | **MISSING** |
| `Configurations\Mt5DealsConfiguration.cs` | **MISSING** |

---

## 3. Item-table delta (A57 → D41). Acceptance did not increment.

| # | §69 item | A57 | D41 (current; hashes match this pass) | Accepted? |
|---|---|---|---|---|
| 1 | Connect to both MT5 brokers | C# impl MISSING | **FAIL** — `ConnectAsync` sets `_connected = true`; dashboard `Connected = true` literal | **No** |
| 2 | Discover all groups | persist MISSING | **DEMO** — 4 fixture names upserted | **No** |
| 3 | Synchronize ~5,000 accounts | MISSING | **FAIL** — 4 logins; `SyncCheckpoint` unused | **No** |
| 4 | Capture XAUUSD trades correctly | C# ingest MISSING | **DEMO** — 18 canned `"XAUUSD"` deals | **No** |
| 5 | Reconstruct logical trades | persist/worker MISSING | **DEMO** — algorithm + persist + tests | **No** |
| 6 | Detect first 3 completed XAUUSD | durable MISSING | **DEMO** — counter + persist + detail highlight | **No** |
| 7 | Deterministic trader/risk score | no persist/job/tests | **DEMO** — `BaselineScorer` persisted; no ML | **No** |
| 8 | Rank traders | MISSING query/API | **DEMO** — `OrderByDescending(EarlyScore)` | **No** |
| 9 | cTrader QUOTE FIX securely | no session | **FAIL** — no socket; status honestly `Disconnected` | **No** |
| 10 | Discover Pepperstone XAU id | MISSING + `123456` | **FAIL** — null persist + harness `55=123456` | **No** |
| 11 | Shadow-copy on dest quotes | unused stub | **DEMO** — engine called; invented dest quote | **No** |
| 12 | Show all of this in React | 0 page files + weatherforecast | **PARTIAL** — 15 pages; cannot show 1/3/9–10 truthfully | **No** |

**0 cells flipped to PASS.** Item 11 moved **unused → DEMO**. Item 12 moved **0 files → PARTIAL**. Items 2/4–8 moved **MISSING → DEMO**. That is why A57’s *table* is stale and why the *gate* is not.

C13 is also stale as a cell source: it scored item 11 **FAIL** (engine unused) and FIX worker **forged** `ReadyForMarketData` / `LoggedOn`. Current worker + seeder stamp **`Disconnected`**. `/api/health` FIX `healthy: false`. C13’s **accepted 0/12** still holds.

---

## 4. React pages A57 said were missing

A57: “`src/pages` has 0 files.” Measured **15** modules:

| File | Bytes | SHA-256 |
|---|---:|---|
| `AuditPage.tsx` | 324 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` |
| `BrokersPage.tsx` | 1 266 | `274754E3DD14D4D89C62F14A8E8A69204C8DBDD7AF479CD367122E68CCB9C460` |
| `FixSessionsPage.tsx` | 1 312 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` |
| `GroupsPage.tsx` | 1 228 | `4F7874826403712D8AB6A0C5C85E9FD95D7C18F5E02B600E0C5384F387C91E65` |
| `LiveCopyPage.tsx` | 321 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |
| `OverviewPage.tsx` | 2 078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` |
| `ReconciliationPage.tsx` | 490 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` |
| `RiskPage.tsx` | 1 148 | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` |
| `ScoringPage.tsx` | 1 288 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` |
| `SettingsPage.tsx` | 459 | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` |
| `ShadowPortfolioPage.tsx` | 628 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` |
| `SystemHealthPage.tsx` | 369 | `03BDBC76CBEFEE4ADC125B491E9B46BC62E35E052AD99BE874E8BCF0E25EDD2A` |
| `TradeExplorerPage.tsx` | 1 321 | `7EE11EB97DBBE0ED077E72A5E15D0DA8EC9E53F8D60CBD76B48985449016AE31` |
| `TraderDetailPage.tsx` | 2 402 | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` |
| `TradersPage.tsx` | 1 604 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` |

Pages existing ≠ item 12 accepted. Shadow / Live / Audit are stubs. Brokers page shows a constant “connected.” This is why item 12 is **PARTIAL**, not PASS.

---

## 5. Hashes this pin stands on

Same blobs D41 used. Unchanged between D41 (`08:13:38Z`) and this pass (`08:14:45Z`).

| File | SHA-256 |
|---|---|
| `A57_first_useful_version.md` | `C1E94C992B28818FAF23D9D6923E2EF56877FE205BA1D64334E5294BC784455E` |
| `C13_fuv_scorecard.md` | `2D09D4D05261A5C881B712B38AEBAED62C8F54AAAF9610B54F7F8E561CD1209B` |
| `D41_fuv_now.md` | `A9B68AB9A48794148FF472CE8B504E8473BE445B8B54BD611C6B0691EFE951BF` |
| `src\Application\Ingestion\DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `src\Infrastructure\Persistence\TraderDbContext.cs` | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `src\Infrastructure\DependencyInjection.cs` | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `apps\api\Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| `apps\mt5-worker\Worker.cs` | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` |
| `apps\fix-worker\Worker.cs` | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` |
| `src\Domain\Reconstruction\TradeReconstructor.cs` | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` (Domain + Application refs only; **no** QuickFIX package) |

What still matches A57 (do **not** call these stale):

| Still true | Evidence |
|---|---|
| Live Achiever / StarwaveFX Connect **not** proven | Fake only; C++ unused (C42, D24, D41 item 1) |
| ~5k checkpointed account sync **MISSING** | 4 hard-coded logins; `SyncCheckpoint` writers = 0 (D41 item 3, D46) |
| Live QUOTE TLS Logon **not** proven | No QuickFIXn; worker stamps `Disconnected` (C43, D32, D52) |
| Pepperstone instrument id **not** discovered | `VenueInstrumentId = null`; harness `55=123456` (D28, D41 item 10) |
| EF `Migrations/` **MISSING** | D51 / C29 |
| ML correctly **not** built | empty `D:\Prop\services` (C44) |
| Live `NewOrderSingle` **off** | SAFE_BY_ABSENCE; `RealCopyExecutionEnabled = false` (D69) |

---

## 6. Citation rule (binding for later agents)

| Need | Use | Do not use |
|---|---|---|
| Current §69 accepted count | **D41** — **0/12** | A57 / C13 / INDEX wave-1 one-liners as inventory |
| Current item maturity (DEMO/FAIL/PARTIAL) | **D41** §1 table | A57 executive table |
| Proof that A57’s *tree* is gone | **This file (D93)** | Memory |
| §69 item definitions / Done-when / I0–I6 sequence | A57 §§item + sequence (design still good) | A57 “what exists” / evidence-index paths |
| C13 | Only for the mid-wave demo-6/12 snapshot | Item 11 FAIL; forged LoggedOn; `/api/health` FIX green |

INDEX stale-vs-later should read **A57 → C13 → D41**, with **D93** as the dedicated “do not cite A57 inventory” pin.

Do not “fix” the accepted count because pages or shadow rows appeared. Re-score only after a live or recorded-replay transport exists (D41 §19).

---

## 7. Honesty line

**A57 0/12 is stale as a description of this repository.**  
**§69 first useful version accepted is still 0/12 (D41).**  
**Demo pipeline now exists (items 2, 4–8, 11 + React shell).**  
**Live MT5, live QUOTE, discovered tag 55, 5k sync: still absent.**  
**ML: correctly not built. Live copy: off by absence.**

D93 does not authorize product edits and does not increment any §69 cell.

---

## Sign-off

```text
[x] A57 inventory (Class1 / weatherforecast / 0 pages / 0 tests / EF will-not-compile) is STALE
[x] A57 executive 12-row table is STALE as current maturity
[x] §69 accepted remains 0/12 (D41)
[x] Product source not modified
[ ] First useful version accepted
```
