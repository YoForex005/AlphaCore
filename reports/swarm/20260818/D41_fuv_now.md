# D41 — Architecture §69 first useful version, scored against CURRENT repo

| Field | Value |
|---|---|
| Agent | D41 |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (worktree; not A57, not C13 memory) |
| Artifact | `D:\Prop\reports\swarm\20260818\D41_fuv_now.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §69 (lines 2633–2654) |
| Product source modified | **No** |
| Method | Re-read §69, then current `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`, `D:\Prop\docs`. Quoted live files. SHA-256 of the files this verdict stands on. A57 / C13 / C54 / D16 / D22 were treated as **historical**, not as the score. |

**Bar (verbatim §69):** the first genuinely useful system does **not** need ML. It is accepted only when **all 12 items are true**.

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

## 0. Scoring rubric (do not greenwash)

| Token | Meaning | Counts as §69 accepted? |
|---|---|---|
| **PASS** | Running system does the item against the intended venues, or against persisted data that originated from those venues / a recorded replay of those venues. | **Yes** |
| **DEMO** | Fake connector + `EnsureCreated` / EF InMemory + seeder implements the *shape*. Integration tests may pass on canned rows. | **No** |
| **PARTIAL** | Algorithm or UI exists, but is not a complete pipeline, or the UI is a stub / JSON dump / cannot show items 1–11 truthfully. | **No** |
| **FAIL** | Missing, unused, or a **health lie** (row / API claims connected / logged-on / discovered without a socket). | **No** |

Demo fixtures and a boolean `_connected = true` are **not** first useful version. A more honest FIX `Disconnected` stamp is still **not** a QUOTE Logon.

---

## 1. Executive verdict (current tree)

**Accepted: 0 / 12.**  
**Demo-wired (not accepted): 7 / 12** — items **2, 4, 5, 6, 7, 8, 11**.  
**UI shell (not accepted): item 12 PARTIAL.**  
**Fail: items 1, 3, 9, 10.**

A57’s **0/12** was measured against an empty Application `Class1`, a non-compiling EF plural-type DbContext, `/weatherforecast`, 1 Hz workers, **0** React pages, and **0** tests. That tree is gone. **The acceptance gate is still 0/12.**

What exists now is a **demo ingest → reconstruct → baseline score → rank → some React tables** path on `FakeMt5BrokerConnector` plus InMemory/`EnsureCreated`. C# still cannot talk to Achiever, StarwaveFX, or Pepperstone QUOTE. The Pepperstone instrument id is still **null**. Shadow rows, when written, are priced from an **invented** dest snapshot (`2399.45` / `2399.85`, `VenueInstrumentId = null`).

Do not treat:

- `FakeMt5BrokerConnector.ConnectAsync` setting `_connected = true`
- `EfDashboardQueries.GetBrokersAsync` hard-coding `Connected = true`
- Overview `mt5Healthy = brokers > 0`
- `/api/health` `mt5Connections[0].healthy = true` with a demo footnote
- 4 canned accounts / 18 canned `XAUUSD` deals
- `VenueInstrumentId = null` plus harness tag `55=123456`
- `PersistDemoShadowAsync` filling from a seeded fake quote
- 15 React pages bound to unversioned `/api/*`

as §69 acceptance.

ML is correctly **not** required. Live copy is **safe by absence** of a send path: `grep` of `D:\Prop\src` finds **zero** `HttpClient`, **zero** `QuickFix` / `QuickFIXn`, **zero** EF `Migration` folders. `Fix.CTrader.csproj` has **no** QuickFIX package. `CanPromoteToLive` is hard-false.

| # | §69 item (verbatim) | Maturity **now** | Accepted? |
|---|---|---|---|
| 1 | Connect to both MT5 brokers | **FAIL** (fake connect + broker `Connected=true` lie) | **No** |
| 2 | Discover all groups | **DEMO** (4 fixture group names upserted) | **No** |
| 3 | Synchronize ~5,000 accounts | **FAIL** (4 logins; `SyncCheckpoint` unused) | **No** |
| 4 | Capture XAUUSD trades correctly | **DEMO** (18 canned deals, all literal `"XAUUSD"`) | **No** |
| 5 | Reconstruct logical trades | **DEMO** (algorithm + persist + unit/integration) | **No** |
| 6 | Detect the first 3 completed XAUUSD trades | **DEMO** (counter + persist + detail highlight) | **No** |
| 7 | Produce a deterministic trader/risk score | **DEMO** (`BaselineScorer`; no ML; persisted) | **No** |
| 8 | Rank traders | **DEMO** (`OrderByDescending(EarlyScore)`) | **No** |
| 9 | Connect to cTrader QUOTE FIX securely | **FAIL** (no socket; status now honestly `Disconnected`) | **No** |
| 10 | Discover the Pepperstone XAUUSD instrument ID | **FAIL** (null persist + forbidden `123456` harness) | **No** |
| 11 | Shadow-copy selected traders using destination quotes | **DEMO** (engine now called; invented dest quote) | **No** |
| 12 | Show all of this in React | **PARTIAL** (15 pages; cannot show 1/3/9–10 truthfully) | **No** |

---

## 2. Why A57 / C13 / C54 / D16 / D22 are stale (do not copy their cell text blindly)

| Prior | What it claimed | What the **current** files actually do |
|---|---|---|
| **A57** | Application `Class1`; EF plural types will not compile; weatherforecast; 1 Hz workers; 0 React pages; 0 tests; shadow unused | All of that is gone. Ports, seeder, 15 pages, unit+integration tests exist. |
| **C13** | FIX worker stamps `ReadyForMarketData` / `LoggedOn` every 15 s; shadow engine **definition only**; `/api/health` FIX `healthy: true` | FIX worker + seeder now persist **`Disconnected`**. `/api/health` FIX `healthy: false`. `PersistDemoShadowAsync` **does** write `CopyIntents` + `ShadowOrders`. |
| **C54** | “has never written a shadow fill”; worker forges LoggedOn | Shadow fills are written from the **seeded** dest quote. FIX status is no longer forged LoggedOn. Live venues still absent. |
| **D16** | “Zero product callers. Not in DI.” | Still not in DI as a service. **Caller exists:** `EfTradingStore.PersistDemoShadowAsync` constructs `new ShadowCopyEngine()`. |
| **D22** | `DemoSeeder` writes TRADE `LoggedOn` / QUOTE `ReadyForMarketData` | Current seeder writes **both** sessions `FixSessionStatus.Disconnected` with “No live … socket” errors. |

**Accepted count did not increment.** Honesty on FIX **did** improve. Item 11 moved **FAIL → DEMO**. That is not PASS.

---

## 3. Cross-cutting facts (apply to every item)

| Surface | Path | Measured now |
|---|---|---|
| Domain algorithms | `D:\Prop\src\Domain\` | Reconstruction (incl. canceled-deal first-3 exclusion), `SymbolNormalizer`, `BaselineScorer`, `RiskEngine`, `ShadowCopyEngine`, volume 10_000, FIX FSM helpers. Real code. |
| Application | `D:\Prop\src\Application\` | `IMt5BrokerConnector`, `IBrokerRegistry`, `ITradingStore`, `IDashboardQueries`, `DealIngestionService`, `ReconstructionScoringService`. FluentValidation referenced, unused. |
| Persistence | `TraderDbContext.cs` | Fluent map of first-useful tables + compound uniques. **0 migrations.** `Configurations/` is empty. Hosts call `EnsureCreatedAsync`. Empty/`<SECRET>` connection → `UseInMemoryDatabase("trader-intelligence")`. DI key is `ConnectionStrings:TraderIntelligence` / `DATABASE_URL`; API `appsettings.json` only has `Postgres` — so **default host is InMemory**. |
| C# MT5 | `FakeMt5BrokerConnector.cs` | **Only** `IMt5BrokerConnector` implementer. DI **always** `DemoBrokerFactory.CreateDefault()`. Dead unused `IBrokerConnector`. Unused `Mt5BrokerOptions`. **No HTTP adapter. No P/Invoke.** |
| C++ SDK | `mt5-sdk\config\app_config.h` | Real `MT5Manager::Connect` / `GroupTotal` / `GetDeals`. **Single-broker** `mt5_server` / `mt5_login`. **Not wired** into C# hosts. |
| FIX | `src\Fix.CTrader\` | Options + pipe parser + in-memory ownership + harness + unused `CTraderQuoteService`. **No** QuickFIXn. **No** `CTraderQuoteSession`. |
| API | `apps\api\Program.cs` | Unversioned `/api/*` (not `/api/v1`). Seeds demo on startup. No `MapHub`. No RBAC. `SettingsController` exists, is **not** mapped (`no AddControllers`). |
| MT5 worker | `apps\mt5-worker\Worker.cs` | 30 s loop: sync both **fakes**, rebuild 4 hard-coded logins. Empty `appsettings.json`. |
| FIX worker | `apps\fix-worker\Worker.cs` | 15 s stamp of session rows to **`Disconnected`**. No socket. |
| React | `apps\web\src\pages\` | **15** page modules + 14 nav labels. Shadow / Live / Audit are stubs. SignalR client hits `/hubs/dashboard`; API maps **no** hub. |
| Tests | `tests\Unit`, `tests\Integration` | Reconstruction, scorer, symbol, volume, risk, sizing (several **Skipped**), `SeedingAndStoreTests`. **No** live-broker / FIX-session / 5k / dest-quote-shadow tests. |
| Compose | `docker-compose.yml` | Postgres + Redis + `dotnet run` API. Native MT5 worker correctly kept off Linux. |
| ML | `D:\Prop\services` | **Empty.** Correct for this bar. |

Identity leftover: reconstructor keys `BrokerId` as **string** (`"ACHIEVER"`); EF entities use **Guid**. Application maps at `LoadDealsAsync` / `ResolveBrokerIdAsync`. Acceptable at the boundary; it is not a dual live session.

### SHA-256 (this verdict)

| File | SHA-256 |
|---|---|
| `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `src\Infrastructure\DependencyInjection.cs` | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `apps\api\Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| `apps\fix-worker\Worker.cs` | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` |
| `src\Application\Ingestion\DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `src\Domain\Reconstruction\TradeReconstructor.cs` | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `src\Fix.CTrader\Services\CTraderQuoteService.cs` | `7D2FDE1D33B47D619EA8BB0EC5F943BC21D8D97B46BEA269D70D46A20859B44A` |

---

## 4. Item 1 — Connect to both MT5 brokers

**Accepted: No. Score: FAIL.**

### Law

§69.1 + §§6–8, 10. Two independent Manager sessions (Achiever + StarwaveFX), reconnect-safe, `broker_id` isolation.

### Evidence (current)

```30:42:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public Task ConnectAsync(CancellationToken ct)
    {
        _connected = true;
        return Task.CompletedTask;
    }
    // ...
    public Task<bool> IsConnectedAsync(CancellationToken ct) => Task.FromResult(_connected);
```

```31:34:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

There is **one** implementer of `IMt5BrokerConnector` (`FakeMt5BrokerConnector`). `IBrokerConnector` has **zero** implementers. `IsConnectedAsync` is **never called** by the dashboard (grep: definition + fake only).

```49:53:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            result.Add(new BrokerStatusDto(b.Code, b.DisplayName, b.Server, MaskLogin(b.ManagerLogin), true, groups, accounts, DateTimeOffset.UtcNow));
```

The fifth argument is `Connected` and is the literal **`true`**. Overview health is “any broker row exists”:

```39:39:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            brokers > 0,
```

API health still paints MT5 green, with a demo footnote that does **not** flip the boolean:

```26:29:D:\Prop\apps\api\Program.cs
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow, details = "demo FakeMt5BrokerConnector — not live Manager" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "no live TLS socket" } },
```

C++ `MT5Manager::Connect` exists (`mt5-sdk\src\core\mt5_manager.h` L31–32) and `AppConfig` is still **one** broker (`mt5_server` / `mt5_login` / `mt5_password` — `app_config.h` L19–22). `apps/mt5-worker/appsettings.json` has logging only. Worker “connect” is `SyncBrokerAsync` against the fakes (`DealIngestionService` L34–35).

### Done when

Process logs a real Manager Connect for **both** broker codes after restart; dashboard `Connected` / `mt5Healthy` is derived from `IsConnectedAsync` (or the C++ watchdog), not a constant; killing one socket does not drop the other.

---

## 5. Item 2 — Discover all groups

**Accepted: No. Score: DEMO.**

### Law

§69.2 + §§7, 9, 11. Enumerate **all** Manager-visible groups. `MT5_GROUP_*` is a **label**, never the fetch filter.

### Evidence (current)

`DemoBrokerFactory.CreateDefault` hard-codes four names:

```99:120:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
        var achiever = new FakeMt5BrokerConnector(
            "ACHIEVER",
            groups: new[]
            {
                new Mt5GroupDto(@"demo\Maxmaster", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"demo\yo-2step", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"contest\yo-2step", "USD", 2, "Achiever", 100, 50, true)
            },
            // ...
        var starwave = new FakeMt5BrokerConnector(
            "STARWAVEFX",
            groups: new[]
            {
                new Mt5GroupDto(@"real\standard", "USD", 2, "StarwaveFX", 80, 50, true)
            },
```

Ingest upserts whatever the connector returns (`DealIngestionService` L39–41). `EfTradingStore.UpsertGroupAsync` keys `(BrokerId, Name)` (`EfTradingStore.cs` L22–50). Schema unique `(BrokerId, Name)` (`TraderDbContext.cs` L44–48). Integration: `db.Mt5Groups.Count() > 2` (`SeedingAndStoreTests.cs` L28). Groups UI copy claims “Discovered dynamically” (`GroupsPage.tsx` L8–9) — true of the **upsert loop**, false of the **source**.

C++ `GroupTotal` / `GetAllGroups` (`imt5_client.h` L164–165; `mt5_manager.cpp` L956–980) is unused by C#.

### Done when

After one resync, `mt5_groups` contains every Manager-visible group on **both** live brokers, including groups **not** in `MT5_GROUP_*`.

---

## 6. Item 3 — Synchronize ~5,000 accounts

**Accepted: No. Score: FAIL.**

### Law

§69.3 + §§7, 10–12. ~5k-scale, checkpointed, unique `(broker_id, login)`, restart-safe.

### Evidence (current)

Four logins in the factory (`10001`, `10002`, `10003`, `99001` — `FakeMt5BrokerConnector.cs` L107–124). Worker and API resync hard-code the same four:

```31:35:D:\Prop\apps\mt5-worker\Worker.cs
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
```

```79:80:D:\Prop\apps\api\Program.cs
    foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        await scoring.RebuildTraderAsync(login >= 99000 ? "STARWAVEFX" : "ACHIEVER", login, ct);
```

`SyncCheckpoint` entity + unique `(BrokerId, Login, Stream)` exist (`TraderDbContext.cs` L23, L115–120; `Domain\Entities\SyncCheckpoint.cs`). Product `*.cs` **writers**: **none** (grep hits only the entity + DbSet + fluent map).

Schema uniqueness `(BrokerId, Login)` is the right shape (`TraderDbContext.cs` L51–56). There is no 5k path, no paging, no measured timing.

### Done when

Both brokers’ accessible accounts are in Postgres at ~5k order of magnitude, unique on `(broker_id, login)`, checkpointed so a killed mid-sync restart does not duplicate, and Overview counts that population.

---

## 7. Item 4 — Capture XAUUSD trades correctly

**Accepted: No. Score: DEMO.**

### Law

§69.4 + §§11–12, 16. Raw deals immutable; XAU aliases map to canonical `XAUUSD`; native volume recoverable; no fabricated ticks.

### Evidence (current)

18 canned deals: 6 Achiever round-trips + 3 Starwave (`BuildAchieverDeals` / `BuildStarwaveDeals`, `FakeMt5BrokerConnector.cs` L130–148). Every row’s symbol is the literal `"XAUUSD"` (`ClosedRoundTrip` L167–168). Volume scale `1 lot = 10_000` (`DemoBrokerFactory.VolumeScale`, L91–93) matches `VolumeConverter.ManagerVolumeScale`.

Persist + dedupe: insert-if-absent on `(BrokerId, DealTicket)` (`EfTradingStore.cs` L85–114). Integration proves second upsert returns false (`SeedingAndStoreTests.cs` L38–62). Schema unique + `(BrokerId, Login, DealTime)` index (`TraderDbContext.cs` L58–64).

`SymbolNormalizer` maps `XAUUSD`, `XAUUSDm`, `GOLD`, … (`SymbolNormalizer.cs` L12–16). Used by the **reconstructor**, not by ingest. `SourceSymbolMappings` DbSet is mapped and **never seeded**. Ticks are not fabricated (correct). MFE/MAE stays `Unavailable` in the scorer (correct).

Worker re-pulls the same 18 rows every 30 s (`from = UtcNow.AddDays(-30)` — the canned deals are dated 2026-06-01, so a wall clock **after** 2026-07-01 would ingest **zero** deals from the fake filter `d.Time >= from`). That is a demo-clock hazard, not live capture.

### Done when

A known XAUUSD **or alias** deal on **each live broker** survives restart exactly once, mapped to canonical `XAUUSD`, native volume recoverable via the documented 10_000 scale.

---

## 8. Item 5 — Reconstruct logical trades

**Accepted: No. Score: DEMO** (strongest algorithm item).

### Law

§69.5 + §14. Order ≠ Deal ≠ Position ≠ Logical Trade. Count completed **position lifecycles**, including scale-in, partial close, SL/TP, reversal.

### Evidence (current)

`TradeReconstructor` groups by `PositionId`; handles `In` / `Out` / `OutBy` / `InOut`; scale-in; partial close; reverse leftover; open leftover as `Completed=false` (`TradeReconstructor.cs` L24–121). Canceled deals mark the position `EligibleForFirstThree = false` (L34–51). `IsXauUsd` iff canonical `XAUUSD` (`ReconstructedTradeResult.cs` L41–42). Balance deals ignored (`NormalizedDeal.IsTradingDeal`; unit `Ignores_balance_deals`).

Persist: `ReconstructionScoringService.RebuildTraderAsync` loads deals, reconstructs, `ReplaceReconstructedAsync` (`DealIngestionService.cs` L79–84; `EfTradingStore.cs` L172–213). Unit tests: round-trip, scale-in + partial + average-down, reverse INOUT, first-3, canceled-deal exclusion, ignore balance (`TradeReconstructionTests.cs`). Integration: seed produces completed XAU reconstructed trades (`SeedingAndStoreTests.cs` L30). API / UI: last 200 reconstructed rows (`Program.cs` L63–70; `TradeExplorerPage.tsx`).

This is a **real reconstructor** on **canned** deals. It is not proven on live Achiever/Starwave books, mixed non-XAU books, or Manager `VolumeExt`.

### Done when

Completed XAU position lifecycles from **live or recorded** Manager deals persist 1:1 with A21 fixtures, including scale-in / partial / reverse, and survive worker restart without inventing trades.

---

## 9. Item 6 — Detect the first 3 completed XAUUSD trades

**Accepted: No. Score: DEMO.**

### Law

§69.6 + §15. Count **completed reconstructed XAUUSD** lifecycles only. Trade #3 unlocks early score / SHADOW — **never LIVE**.

### Evidence (current)

Helpers: `CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` (`>= 3`) filter `Completed && IsXauUsd && EligibleForFirstThree` (`TradeReconstructor.cs` L60–76). Unit: 3 completed → eligible; a canceled-deal position is excluded so 3 completed lifecycles with one dirty position → count 2 (`TradeReconstructionTests.cs` L70–99).

**Persist path does not use that helper.** Scoring counts `Completed && IsXauUsd` only:

```86:96:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            // ...
            CompletedXauTrades = score.Features.CompletedXauTrades,
```

Trader-detail “first 3” is the first three completed XAU rows by time, **not** `EligibleForFirstThree`:

```140:156:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var firstThree = 0;
        var highlights = trades.Select(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && firstThree < 3;
            if (first)
                firstThree++;
            return new TradeHighlightDto(/* ... */, first);
        }).ToList();
```

UI shows the flag (`TraderDetailPage.tsx` L31–44). `CanPromoteToLive` is **always false** (`BaselineScorer.cs` L210–211). Integration: login 10001 `CompletedXauTrades == 3` and state is **not** `LIVE`; 10002 is `RISK_BLOCKED` (`SeedingAndStoreTests.cs` L31–33).

### Done when

A trader with 0 / 2 / 3 completed XAU lifecycles is classified correctly after restart from persisted deals; canceled / partial / non-XAU do not increment the durable counter; trade #3 never writes `LIVE`.

---

## 10. Item 7 — Produce a deterministic trader/risk score

**Accepted: No. Score: DEMO.**

### Law

§69.7 + §§18, 22–23. Deterministic baseline. `mlProbability` may be null. No XGBoost. Trade #3 + high score → **SHADOW only**.

### Evidence (current)

`BaselineScorer` computes net, PF, lot CV, martingale (1.25× after a loss), averaging-down, lot escalation, hold, SL rate, DD → risk / behavior / early quality → `TraderStateMachine` (`BaselineScorer.cs` L42–212). `EarlyScoreTradeCount = 3`; `< 3` caps quality and stays `INSUFFICIENT_DATA`. Persist: current row + `trader_score_history` (`EfTradingStore.cs` L215–248). Unit: 2 trades insufficient; 3 winners → **SHADOW not LIVE**; martingale → `RISK_BLOCKED` (`BaselineScorerTests.cs`). `mlProbability` is **null** on the DTO (`EfDashboardQueries.cs` L100; `TraderRowDto`). `D:\Prop\services` is empty. UI: “XGBoost is not active.” (`ScoringPage.tsx` L7). **`behaviorScore` is not on `TraderRowDto`** — the Scoring page column is `t.behaviorScore ?? 0` and will render **0**.

Deterministic path exists and is the production ranker in the demo. It is not accepted until it scores **captured** (item 4) reconstructed (item 5) books.

### Done when

Persisted scores are a pure function of persisted completed XAU trades; same deals → same numbers; ML remains null; trade #3 + high score is SHADOW only.

---

## 11. Item 8 — Rank traders

**Accepted: No. Score: DEMO.**

### Law

§69.8 + §§21, 50. Leaderboard of deterministic scores.

### Evidence (current)

```110:116:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        IEnumerable<TraderRowDto> filtered = mapped;
        if (!string.IsNullOrWhiteSpace(broker))
            filtered = filtered.Where(t => t.Broker.Equals(broker, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(state) && Enum.TryParse<TraderState>(state, true, out var st))
            filtered = filtered.Where(t => t.State == st);

        return filtered.OrderByDescending(t => t.EarlyScore).ToList();
```

API: `GET /api/traders` (`Program.cs` L57–58). Not `/api/v1`, no A92 envelope. UI: `TradersPage.tsx` table + detail links. Population: **4** scored logins (10003 has 0 deals → `INSUFFICIENT_DATA`).

Ranking **works on the demo set**. It is not a useful ranker of the ~5k book.

### Done when

`GET /api/v1/traders` (A92) returns the scored population ordered by the documented sort, including filters, without fabricating ML.

---

## 12. Item 9 — Connect to cTrader QUOTE FIX securely

**Accepted: No. Score: FAIL.**

### Law

§69.9 + §§25–28, 31. Independent QUOTE session, TLS 5211, issued Comp/Sub IDs, `TargetCompID = cServer` (case preserved). TRADE send is **not** required.

### Evidence (current)

`TraderIntelligence.Fix.CTrader.csproj` has **no** package references (only Domain + Application). **No** `using QuickFix`. **No** session type. `CTraderQuoteService` is an in-memory mapper; **zero callers** outside its file.

Options on disk: host `live-us-eqx-01.p.c-trader.com`, QUOTE SSL 5211, `TargetCompId = "cServer"`, `UseSsl = true`, `RealCopyExecutionEnabled = false`, but `TradeSessionEnabled = true` (`CTraderFixOptions.cs` L10–35, L47–49). API `appsettings.json` leftover `CTraderFix:TargetCompId = "CSERVER"` is **not bound** by `Program.cs`.

Honesty improved vs C13/D22. Seeder:

```68:85:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.FixSessionStates.AddRange(
            new FixSessionState
            {
                Qualifier = FixSessionQualifier.Quote,
                Status = FixSessionStatus.Disconnected,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5211,
                TargetCompId = "cServer",
                // ...
                LastError = "No live QUOTE socket. Demo seed only.",
```

Worker (every 15 s, no socket):

```28:41:D:\Prop\apps\fix-worker\Worker.cs
            if (quote is not null)
            {
                quote.UpdatedAt = DateTimeOffset.UtcNow;
                quote.Status = FixSessionStatus.Disconnected;
                quote.LastError = "No live QUOTE socket. Simulator/demo only.";
            }
            // TRADE → Disconnected, "No live TRADE socket. NewOrderSingle remains off."
```

`/api/health` FIX `healthy: false` (`Program.cs` L29). Overview `quoteHealthy` is true only for `LoggedOn` / `ReadyForMarketData` / `ReadyForExecution` (`EfDashboardQueries.cs` L40) — so after seed+worker it is **false**. That is honest. It is still **not a TLS Logon**.

### Done when

QUOTE initiator completes Logon on TLS 5211 with issued IDs; `fix_sessions` status is driven by the session, not a timer; password never logged or returned to React; TRADE may remain disconnected / “not started.”

---

## 13. Item 10 — Discover the Pepperstone XAUUSD instrument ID

**Accepted: No. Score: FAIL.**

### Law

§69.10 + §§16, 30, 72.13. Discover via SecurityList. **Never hardcode tag 55.**

### Evidence (current)

Seeded dest quote:

```105:113:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.DestinationQuotes.Add(new DestinationQuoteSnapshot
        {
            CanonicalSymbol = "XAUUSD",
            VenueInstrumentId = null,
            Bid = 2399.45m,
            Ask = 2399.85m,
            ReceivedAt = now
        });
```

Dashboard surfaces `quote?.VenueInstrumentId` (null). UI: `Instrument ID: not discovered yet` (`FixSessionsPage.tsx` L19). Honest for this field.

Forbidden fixture:

```141:142:D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs
            (55, "123456"), // XAUUSD instrument numeric ID (as string)
            (1007, "XAUUSD")
```

`CTraderQuoteService.OnSecurityListResponse` can parse tag 55/1007 **if handed dictionaries** (`CTraderQuoteService.cs` L46–66). It is not wired to a session. `BuildSecurityListRequestTags` emits **`(35, "y")`** (SecurityList **response** type), not `35=x` (L111–114) — unusable as a request builder. Venue map on `SymbolNormalizer` starts empty; unit test registers `"123456"` only after `RegisterVenueInstrument` (`SymbolNormalizerTests.cs` L26–28).

No `destination_symbols` table persist from a real or recorded SecurityList.

### Done when

A SecurityList (live or recorded) yields a **persisted** numeric instrument id for Pepperstone XAUUSD on account `1369850`; tag 55 in later MD/ER is that discovered id; `123456` is not written to `destination_quotes`.

---

## 14. Item 11 — Shadow-copy selected traders using destination quotes

**Accepted: No. Score: DEMO** (C13 **FAIL** is stale).

### Law

§69.11 + §§23–24, 31, 36–38. Selected SHADOW traders; fills from **destination QUOTE** only; OPEN vs REDUCE/CLOSE policies; fail closed on stale/wide quotes. No TRADE `NewOrderSingle`.

### Evidence (current)

Engine is still a taker-touch calculator: dest bid/ask, then **mutates** price by `0.05` if delay > 250 ms; **no** quote-age / spread / missing-book reject (`ShadowCopyEngine.cs` L35–60). `RiskEngine` **does** encode `QUOTE_MISSING` / `QUOTE_STALE` / `SPREAD_TOO_WIDE` / `SIGNAL_STALE` (`RiskEngine.cs` L95–115) and is **not** called from the persist path. `QuantityNormalizer` unused (skipped test says so).

**New vs C13:** `ReconstructionScoringService` always calls `PersistDemoShadowAsync` (`DealIngestionService.cs` L104). For `TraderState.SHADOW` it writes a `CopyIntent` + `ShadowOrder` priced from the **latest** `destination_quotes` row (`EfTradingStore.cs` L251–337). That row is the seeded invented book (`VenueInstrumentId = null`, 2399.45 / 2399.85). `ExpiresAt = trade.OpenedAt.AddSeconds(15)` — already expired vs `UtcNow`; expiry is not checked. Modeled delay is a constant 80 ms. `RiskEngine` is not consulted. Destination commission/swap model is absent.

Shadow UI is still a **static paragraph** with **no API** (`ShadowPortfolioPage.tsx` L1–14). Overview `shadowPnl` sums `SourceVsShadowSlippage` (`EfDashboardQueries.cs` L21). Live page correctly says the flag is false (`LiveCopyPage.tsx` L5).

### Done when

Traders in `SHADOW` emit persisted shadow fills priced from a **usable** dest QUOTE snapshot (discovered instrument id, age/spread/move guards), marked to that tape, visible on the Shadow page. Source ticks are never the fill price. Grep of the send path shows zero `NewOrderSingle`.

---

## 15. Item 12 — Show all of this in React

**Accepted: No. Score: PARTIAL.**

### Law

§69.12 + §§46–54. The dashboard must **show items 1–11**. A Vite route table over missing or lying APIs is not enough. SignalR is recommended, not a gate.

### Evidence — what exists

| Page | Path | Bound to |
|---|---|---|
| Overview | `OverviewPage.tsx` | `GET /api/overview` — demo counts; `mt5Healthy` from `brokers > 0` |
| Brokers | `BrokersPage.tsx` | `GET /api/brokers` — always “connected” |
| Groups | `GroupsPage.tsx` | `GET /api/groups` — 4 fixture groups |
| Traders | `TradersPage.tsx` | `GET /api/traders` — ranked demo scores |
| Trader detail | `TraderDetailPage.tsx` | `GET /api/traders/{broker}/{login}` — `TraderDetailDto` + first-3 column |
| Trades | `TradeExplorerPage.tsx` | `GET /api/trades` — reconstructed demo trades |
| Scoring | `ScoringPage.tsx` | Reuses traders; `behaviorScore` missing on wire |
| FIX | `FixSessionsPage.tsx` | `GET /api/fix/sessions` — Disconnected + “not discovered yet” (honest) |
| Risk | `RiskPage.tsx` | `GET /api/risk` — kill switch + zeros |
| Health | `SystemHealthPage.tsx` | `GET /api/health` JSON dump (MT5 green / FIX red) |
| Recon | `ReconciliationPage.tsx` | `GET /api/reconciliation/status` — **static zeros**, not DB |
| Settings | `SettingsPage.tsx` | `GET /api/settings` — flags + broker ids (minimal-API map, not `SettingsController`) |
| Shadow | `ShadowPortfolioPage.tsx` | **No API** (even though `shadow_orders` may now exist) |
| Live | `LiveCopyPage.tsx` | Static “flag false” (correct for live, not a §69 show) |
| Audit | `AuditPage.tsx` | **No API** |

Router: `App.tsx` (15 routes). Nav: `DashboardLayout.tsx` (14 labels). Hooks: `hooks.ts` → unversioned `/api/*` (D39: 11/11 hook GETs match `MapGet`; **0/11** use `/api/v1`). SignalR: `signalr.ts` → `/hubs/dashboard`; `Program.cs` maps **none**.

### Why it is not item 12

1. Items 1, 3, 9, 10 are not true, so they cannot be shown.
2. Where 1 is “shown,” the value is **wrong** (`connected`, MT5 OK).
3. Item 11 may have DB rows the Shadow page **does not read**.
4. No `/api/v1`, no RBAC, no login page, no hub.
5. `docs/architecture.md` L28 (“Implemented toward first useful version … FIX session *state*”) describes a demo pipeline, not §69 acceptance.

### Done when

An operator can open React and see **true** dual-broker health, discovered groups, ~5k account counts, captured/reconstructed XAU, first-3, baseline ranks, QUOTE logon, discovered instrument id, and shadow fills — without a green badge the process invented.

---

## 16. What is correctly **not** required

| Topic | Status | Pin |
|---|---|---|
| ML / XGBoost / `services/ml-service` | Empty `D:\Prop\services` | §69 first sentence |
| Live `NewOrderSingle` | No send path; worker refuses even if flag true | §70 / §69 (out of bar) |
| Kafka, K8s, ClickHouse, LLM, DNN, RL | Absent | §71 |
| Emergency flatten from React | Live page is a stub | A06 / A48 |

Absence of ML is **PASS for this bar**, not a gap.

---

## 17. Delta vs A57 and vs C13

| Area | A57 | C13 | **D41 (now)** |
|---|---|---|---|
| Accepted §69 | 0/12 | 0/12 | **still 0/12** |
| Application | `Class1` | ingest + scoring ports | same + `PersistDemoShadowAsync` on the store port |
| Persistence | EF did not compile | compiles; InMemory / EnsureCreated | **same** (0 migrations) |
| Fake dual-broker ingest | missing | 18 deals / 4 accounts | **same 18 / 4** |
| Reconstructor / scorer / rank | in-memory only | persisted + tests | persisted + canceled-deal first-3 + detail highlights |
| React pages | 0 files | 15 modules | **15**; detail now binds first-3 |
| FIX session row | unused | **forged** LoggedOn / ReadyForMarketData | **honest Disconnected** (still no socket) |
| Shadow pipeline | unused stub | unused stub | **DEMO writes** from invented dest quote |
| Live MT5 / QUOTE / instrument | missing | missing | **still missing** |
| Health honesty | N/A | worse (green lies) | FIX honesty **improved**; MT5 broker `Connected=true` **still a lie** |

Progress is real. Acceptance is not.

---

## 18. Flip list (minimum to move each cell to PASS)

| # | Flip to PASS |
|---|---|
| 1 | Two real `IMt5BrokerConnector` instances (HTTP bridge or native host) with measured Connect; dashboard reads `IsConnectedAsync`. Delete the `true` literal in `GetBrokersAsync`. |
| 2 | Startup `GetGroups` from Manager on both brokers → upsert all names. |
| 3 | Checkpointed account backfill; measured ~5k; unique `(broker_id, login)`. |
| 4 | Checkpointed `GetDeals`; persist native tickets; map observed aliases; no invented ticks. |
| 5 | Same reconstructor on those deals; A21 replay green. |
| 6 | Durable completed-XAU count using `EligibleForFirstThree`; #3 ≠ LIVE. |
| 7 | `BaselineScorer` job on persisted trades; `behaviorScore` on the wire; tests locked. |
| 8 | A92 leaderboard over that population. |
| 9 | QuickFIXn 1.14.1 QUOTE TLS Logon; status from session. |
| 10 | Persist SecurityList XAU id; never `123456`. |
| 11 | Shadow pipeline writes fills from dest quotes with A24/A72 guards; Shadow page reads them. |
| 12 | React binds the true read models for 1–11; delete remaining health lies. |

Do not enable TRADE `NewOrderSingle` as part of this list.

---

## 19. Honesty line

**First useful version accepted: 0 / 12.**  
**Demo pipeline (fake MT5 → reconstruct → baseline score → rank → some React tables + demo shadow rows): items 2, 4, 5, 6, 7, 8, 11.**  
**Live Achiever, live StarwaveFX, live Pepperstone QUOTE, discovered tag 55: not present.**  
**FIX status rows are no longer forged LoggedOn; they are still not a session.**  
**ML: correctly not built.**  
**Live copy: off by absence.**

D41 does not authorize product edits. Re-score after a live (or recorded-replay) transport exists; do not increment the accepted count for more demo rows or for a more honest `Disconnected` stamp.

---

## Sign-off (first useful version)

```text
[ ]  1. Connect to both MT5 brokers.
[ ]  2. Discover all groups.
[ ]  3. Synchronize ~5,000 accounts.
[ ]  4. Capture XAUUSD trades correctly.
[ ]  5. Reconstruct logical trades.
[ ]  6. Detect the first 3 completed XAUUSD trades.
[ ]  7. Produce a deterministic trader / risk score.
[ ]  8. Rank traders.
[ ]  9. Connect to cTrader QUOTE FIX securely.
[ ] 10. Discover the Pepperstone XAUUSD instrument ID.
[ ] 11. Shadow-copy selected traders using destination quotes.
[ ] 12. Show all of this in React.

[ ] Phase 0–5 exits
[ ] Reviewer PASS + test PASS with on-disk evidence
[x] ML not required
[x] REAL_COPY_EXECUTION_ENABLED remains false
```

**D41 conclusion:** treat the tree as a **working demo of items 2/4–8/11** plus a **React shell**. The first useful version is **not** accepted. A57 is not the current inventory. C13’s 0/12 gate still holds; C13’s item-11 FAIL and FIX-forged-LoggedOn cells do not.
