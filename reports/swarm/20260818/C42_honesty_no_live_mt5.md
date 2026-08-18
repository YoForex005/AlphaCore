# C42 — Honesty pin: live Achiever / StarwaveFX connections are NOT proven

| Field | Value |
|---|---|
| Agent | C42 (senior engineer, honesty / go-live gate only) |
| Date | 2026-08-18 |
| Assigned | State clearly live Achiever/Starwave connections are **NOT** proven; fake connector only. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\C42_honesty_no_live_mt5.md` |
| Product source modified | **No.** This report is the only write. |
| Binding gates | Architecture §7–§8, §67 Phase 1, §68 G01 (`A100_golive_gates.md`), `PHASE0_AUDIT.md` “Live MT5 connect = MISSING” |
| Siblings (do not contradict) | B04 (C# Mt5 inventory), C10 (Fake is the only implementor), C05 (DI always wires `DemoBrokerFactory`), C20 (C++ SDK preserved, **not** the C# collector), A100 G01 FAIL, A105 (C# does not load `MT5APIManager64.dll`) |

Classification of the assigned claim: **CONFIRMED.** Live Achiever and live StarwaveFX Manager (or HTTP-bridge) sessions are **not proven**. The only `IMt5BrokerConnector` in the C# product is `FakeMt5BrokerConnector`.

---

## 0. Verdict (binding, do not greenwash)

**Live Achiever connection: NOT PROVEN.**  
**Live StarwaveFX connection: NOT PROVEN.**  
**What exists: an in-memory fake connector, registered as production DI, labeled with those two broker codes.**

| Claim someone might write | Measured truth |
|---|---|
| “Achiever is connected” | **False** as a Manager/HTTP fact. `ConnectAsync` sets `_connected = true` in process memory. No socket. No `LoadLibrary`. No password. |
| “StarwaveFX is connected” | **False** as a Manager/HTTP fact. Same fake type, second instance, code `"STARWAVEFX"`. |
| “mt5-worker syncs Achiever and Starwave every 30 s” | **True and misleading.** It calls `DealIngestionService.SyncBrokerAsync` on the **two fakes**. That is a demo loop, not a broker session. |
| “Dashboard shows brokers connected” | **True and a lie.** `EfDashboardQueries.GetBrokersAsync` hard-codes `Connected = true` and `LastEventAt = DateTimeOffset.UtcNow`. It never calls `IsConnectedAsync`. |
| “C++ `mt5-sdk` can talk to Manager API” | **Capability of a separate tree**, not a measured live session for this product. C# does not call it. No probe JSON in this pass proves Achiever or StarwaveFX `connection.success`. |
| “Seeded IPs mean we are on those hosts” | **False.** `DemoSeeder` writes `57.128.141.65` / `84.201.6.142` onto `Broker` rows. `FakeMt5BrokerConnector` never reads `Server`, `Port`, `ManagerLogin`, or any password. |

**Honest one-liner:** C# can demo-ingest **18 canned XAUUSD deals** across **4 hard-coded logins** on **2 fake brokers**. C# cannot talk to MT5. Phase 1 “Achiever connected / StarwaveFX connected” remains **FAIL**. A100 G01 remains **FAIL**. Vacuous / demo evidence cannot become PASS.

Do **not** treat a green `dotnet build`, a running worker log line, or an emerald “connected” cell on `BrokersPage` as a live Manager proof.

---

## 1. Method

1. Read the current C# connector, contracts, DI, seeder, ingestion, dashboard, and `apps/mt5-worker` in full.
2. Grep product `*.cs` for a second `IMt5BrokerConnector` implementor, `HttpClient`, `DllImport`, `PInvoke`, `NativeLibrary`, `MT5APIManager64`, `TcpClient`, `CIMTManagerAPI`.
3. Confirm seeded host/login fields are catalog decoration, not constructor inputs to the fake.
4. Confirm dashboard “connected” is a literal `true`.
5. Hash the files this verdict stands on.
6. Cross-check siblings B04, C05, C10, C20, A100 G01, A105, `PHASE0_AUDIT.md`.
7. **Did not** edit anything under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.
8. **Did not** open a Manager TCP session, run `mt5_group_probe`, or claim a C++ live attach that was not measured here.

A04 / A07 inventory that still says “`IMt5BrokerConnector` MISSING / worker is a 1 s template loop” is **stale**. Current measured worker **does** call ingestion — against **fakes**. Stale “no connector” is not this report’s excuse, and current “worker syncs ACHIEVER/STARWAVEFX” is not a live proof.

---

## 2. File hashes (measured 2026-08-18)

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | — | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | — | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | — | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 1882 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` |
| `D:\Prop\apps\mt5-worker\Program.cs` | — | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` |
| `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | — | `6B7AA65F293AF43A548D09BC92332286A5123EDB03DCCD498C2217490CCBC5BC` |
| `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | — | `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7` |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | — | `CF4165CE7A317B0282B9149B078E5D1E630F72524190AB20E0952BECBBAE1182` |

Hashes for Fake / DI / seeder match C05 and C10. The tree did not grow a live connector between those reviews and this pin.

---

## 3. Only implementor is the fake

Workspace grep of product C# for `: IMt5BrokerConnector` / `class …Connector`:

| Type | Path | Role |
|---|---|---|
| `FakeMt5BrokerConnector` | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **Only** implementor of `IMt5BrokerConnector` |
| `BrokerRegistry` | same file | string dictionary; no transport |
| `DemoBrokerFactory.CreateDefault()` | same file | builds **two Fake instances** named Achiever / Starwave |
| `IBrokerConnector` | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | unused draft; **zero** implementors |
| `Mt5ManagerBrokerConnector` | — | **MISSING** |
| `Mt5HttpBrokerConnector` / `Mt5CollectorClient` | — | **MISSING** |

`TraderIntelligence.Mt5.csproj` references Domain + Application only. No `Http*` package, no `AllowUnsafeBlocks`, no native DLL copy, no C++/CLI.

Product `*.cs` grep (under `D:\Prop\src` + `D:\Prop\apps`, excluding vendor examples):

| Needle | Hits |
|---|---|
| `HttpClient` / `IHttpClientFactory` / `text/event-stream` / `/mt5/` | **0** |
| `DllImport` / `PInvoke` / `NativeLibrary` / `MT5APIManager64` | **0** |
| `TcpClient` / `Socket(` / `CIMTManagerAPI` | **0** |
| `Mt5BrokerOptions` used outside its own file | **0** (options sketch is dead) |

There is no second connector waiting behind a feature flag.

---

## 4. `ConnectAsync` is a boolean flip

```30:42:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public Task ConnectAsync(CancellationToken ct)
    {
        _connected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct)
    {
        _connected = false;
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync(CancellationToken ct) => Task.FromResult(_connected);
```

Measured:

- No host, port, login, password, proxy, or `RemoteUrl` on the type.
- Constructor takes `brokerCode` + four optional in-memory lists.
- `GetGroupsAsync` / `GetAccountsAsync` / `GetDealsAsync` / `GetPositionsAsync` return those lists. They do **not** throw when `_connected` is false (fail-open vs A58/A79 — adjacent gap, not a live session).
- `AddDeal` mutates the in-process list. That is a test/demo seed API, not a broker write.

A call to `ConnectAsync` that returns `Task.CompletedTask` after `_connected = true` is **not** a Manager `Connect` / `Initialize` / HTTP health. It is a flag.

---

## 5. Factory labels, not venues

```95:127:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public static (FakeMt5BrokerConnector Achiever, FakeMt5BrokerConnector Starwave) CreateDefault()
    {
        var t0 = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

        var achiever = new FakeMt5BrokerConnector(
            "ACHIEVER",
            groups: new[]
            {
                new Mt5GroupDto(@"demo\Maxmaster", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"demo\yo-2step", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"contest\yo-2step", "USD", 2, "Achiever", 100, 50, true)
            },
            accounts: new[]
            {
                new Mt5AccountDto(10001, @"demo\Maxmaster", 100, 10_000, 10_240, 200, 9_800, 240),
                new Mt5AccountDto(10002, @"demo\yo-2step", 100, 5_000, 4_820, 150, 4_670, -180),
                new Mt5AccountDto(10003, @"contest\yo-2step", 200, 25_000, 25_000, 0, 25_000, 0)
            },
            deals: BuildAchieverDeals(t0));

        var starwave = new FakeMt5BrokerConnector(
            "STARWAVEFX",
            groups: new[]
            {
                new Mt5GroupDto(@"real\standard", "USD", 2, "StarwaveFX", 80, 50, true)
            },
            accounts: new[]
            {
                new Mt5AccountDto(99001, @"real\standard", 100, 8_000, 8_110, 80, 7_920, 110)
            },
            deals: BuildStarwaveDeals(t0));

        return (achiever, starwave);
    }
```

Census of the canned book (complete; not a sample):

| Instance `BrokerCode` | Groups | Logins | Closed XAU round-trips | Deal rows |
|---|---:|---|---:|---:|
| `ACHIEVER` | 3 (`demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`) | `10001`, `10002`, `10003` | 3 + 3 + **0** | **12** |
| `STARWAVEFX` | 1 (`real\standard`) | `99001` | 3 | **6** |
| **Total** | **4** | **4** | **9** | **18** |

`10003` has an account and **zero** deals (C23). That empty login is still not a live Starwave/Achiever miss — it is a fixture.

Deal times are frozen at `2026-06-01T08:00:00Z` (+ hours/days in the factory). They are not broker history.

`BrokerCodes.Achiever = "ACHIEVER"` and `BrokerCodes.StarwaveFx = "STARWAVEFX"` are string constants (`D:\Prop\src\Domain\Brokers\BrokerCodes.cs`). They name the fake keys. They do not attach a transport.

`PriceSource.AchieverMt5Ticks` / `StarwaveMt5Ticks` are enum slots. Nothing in the Fake publishes ticks. No C# tick subscribe exists.

---

## 6. Production DI always registers the fake

```31:34:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

There is no `if (UseLiveManager)`, no `Mt5BrokerSlotBinder`, no options bind of §56 `MT5_*` / `MT5_STARWAVEFX_*` keys.

Default persistence when `ConnectionStrings:TraderIntelligence` is empty or contains `"<SECRET>"`: **EF InMemory** (`"trader-intelligence"`). `apps/api/appsettings.json` ships an empty connection string. A host that “runs” therefore typically never leaves process memory.

`DemoSeeder.SeedAsync` calls `DemoBrokerFactory.CreateDefault()` **again**, `new BrokerRegistry(...)`, `new DealIngestionService(...)` — a second independent fake pair (C05 split graph). Both graphs are still fakes.

A79 specified `InMemoryMt5BrokerConnector` under `tests/` only, never registered in `apps/mt5-worker` except a test host. Current tree **violates that placement law**: the Fake **is** the worker’s broker. That makes a live-looking loop easier to misread as production attach. It is still a fake.

---

## 7. Seeded IPs / manager logins are catalog paint

```29:58:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.Brokers.AddRange(
            new Broker
            {
                Id = achieverId,
                Code = BrokerCodes.Achiever,
                DisplayName = "Achiever",
                Server = "57.128.141.65",
                Port = 443,
                ManagerLogin = 2027,
                ServerName = "AchieverGlobalMarkets-Server",
                Mode = "local",
                ...
            },
            new Broker
            {
                Id = starwaveId,
                Code = BrokerCodes.StarwaveFx,
                DisplayName = "StarwaveFX",
                Server = "84.201.6.142",
                Port = 443,
                ManagerLogin = 9904,
                ServerName = "StarwaveFX",
                Mode = "local",
                ...
            });
```

These fields are written to `brokers` and later shown on the dashboard. They are **not** passed into `FakeMt5BrokerConnector`. Grep of product C# for `57.128.141.65` / `84.201.6.142`: **only** `DemoSeeder.cs`.

`Mt5BrokerOptions` documents `Server` / `Port` / `Login` / `Password` / `RemoteUrl` / proxy / `ApiKey`. **No** `IOptions<Mt5BrokerOptions>` registration. The sketch is unused. Password on the options type is a placeholder comment, not a live secret load.

`apps/mt5-worker/appsettings.json` has logging only. No `MT5_*` block.

Presence of a real-looking IP in a demo row is **not** a TCP SYN, TLS handshake, Manager logon, or group pump.

---

## 8. Worker loop is fake ingest

```17:44:D:\Prop\apps\mt5-worker\Worker.cs
        _logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<DealIngestionService>();
                ...
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    ...
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
            }
            ...
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
```

`DealIngestionService.SyncBrokerAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` 31–58):

1. `registry.Get(brokerCode)` → Fake
2. `ConnectAsync` → boolean
3. `GetGroupsAsync` / `GetAccountsAsync(null)` / per-login `GetDealsAsync` + `GetPositionsAsync`
4. upsert into `ITradingStore`

That is a correct **port** shape. The port is bound to canned lists.

Worker window is `UtcNow.AddDays(-30)` … `UtcNow.AddMinutes(1)`. Seed instead uses `2026-01-01` … `2026-12-31`. On 2026-08-18 the canned `2026-06-01` tape is **outside** the worker’s 30-day window, so a later poll does not even re-read the fixture deals. Neither window is a Manager `DealRequest`.

Scoring is hard-coded to the four demo logins. That is not “all accounts on two live brokers.”

`docker-compose.yml` runs postgres, redis, and the API SDK image. Comment on disk: “Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers.” Compose does **not** start `mt5-worker`, does **not** mount `MT5APIManager64.dll`, and does **not** pass broker passwords.

---

## 9. Dashboard “connected” is forged

```46:54:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<IReadOnlyList<BrokerStatusDto>> GetBrokersAsync(CancellationToken ct)
    {
        var brokers = await _db.Brokers.OrderBy(b => b.Code).ToListAsync(ct);
        var result = new List<BrokerStatusDto>();
        foreach (var b in brokers)
        {
            var groups = await _db.Mt5Groups.CountAsync(g => g.BrokerId == b.Id, ct);
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == b.Id, ct);
            result.Add(new BrokerStatusDto(b.Code, b.DisplayName, b.Server, MaskLogin(b.ManagerLogin), true, groups, accounts, DateTimeOffset.UtcNow));
        }
        return result;
    }
```

`BrokerStatusDto` 5th argument is `bool Connected`. The value is the literal `true`. `LastEventAt` is `DateTimeOffset.UtcNow` (clock, not a deal/event).

Overview `Mt5Healthy` is `brokers > 0` (a seeded row exists), not a Manager ping.

`apps/web/src/pages/BrokersPage.tsx` paints `b.connected ? 'connected' : 'down'` in emerald. After demo seed this is **always** emerald.

That UI is **not** evidence. Treating it as G01 PASS is a policy fail.

---

## 10. C++ SDK exists; it is not this product’s live attach

`D:\Prop\mt5-sdk` is preserved (C20): `IMT5Client`, `MT5Manager` (local `LoadLibraryW` of `MT5APIManager64.dll`), `MT5HttpClient` (curl + SSE). That tree is **capability**, not a measured Achiever/Starwave session for Trader Intelligence.

| Check | Measured |
|---|---|
| C# `DllImport` / `NativeLibrary` of Manager DLL | **None** (C20, A105, this grep) |
| C# `HttpClient` to `/mt5/*` | **None** (B04, this grep) |
| C++ `AppConfig` dual-broker `MT5_STARWAVEFX_*` | **Absent** (A04) — C++ config is single-broker |
| C# worker copy-dlls of `MT5APIManager64.dll` | **Absent** (A105) |
| CTest hermetic suite talks to Achiever/Starwave | **No** (A18: “no MT5 server”) |
| Live probes `mt5_group_probe` / `mt5_news_calendar_probe` | Opt-in, Windows-only, **not** `add_test`. This C42 pass did **not** run them and found **no** on-disk probe JSON proving `connection.success` for either venue. |

Even a future successful C++ probe against one Manager login would still **not** prove the C# worker is live, and would not by itself prove **both** Achiever **and** StarwaveFX (C++ is single-slot today).

`PHASE0_AUDIT.md` already classifies: **Live MT5 connect = MISSING.** This report reaffirms that line against the current Fake-wired tree.

---

## 11. What “proven” would require (so this pin cannot be walked back)

A100 G01: fake factory, in-memory DB, unused method, or seeded rows **cannot** become PASS.

Minimum evidence for **either** venue, then **both**:

| # | Required evidence | Present now? |
|---|---|---|
| 1 | A non-fake `IMt5BrokerConnector` (Manager P/Invoke / C++/CLI / HTTP collector) registered **instead of** `DemoBrokerFactory` for that slot | **No** |
| 2 | Process actually loads `MT5APIManager64.dll` **or** completes TLS+auth to a documented HTTP bridge | **No** |
| 3 | Manager login / password / server come from secret config, not `DemoSeeder` paint | **No** (worker appsettings have no MT5 block; API password fields empty) |
| 4 | Log/probe JSON with host, login (masked), `IsConnected=true`, last error empty, timestamp, exit code 0 | **No** |
| 5 | `GetGroupsAsync` returns a Manager-visible set larger/different than the 3+1 canned names (or a recorded live dump hashed on disk) | **No** — 4 hard-coded groups |
| 6 | `GetDealsAsync` returns tickets that exist on the broker for a measured window (not `BuildAchieverDeals` / `BuildStarwaveDeals`) | **No** — 18 canned rows |
| 7 | Disconnect / reconnect observed against the real session; empty success ≠ “broker down” | **No** |
| 8 | Dual-broker: Achiever **and** StarwaveFX sessions independently up (C++ today cannot do two slots in one `AppConfig`) | **No** |
| 9 | Dashboard `Connected` reads `IsConnectedAsync` (or a health gauge), not `true` | **No** |
| 10 | Persistence is Postgres (or another real store), not EF InMemory fallback | **Not default** |

Until those are on disk, the honest status string is:

```text
ACHIEVER    = FAKE_ONLY   (not live, not proven)
STARWAVEFX  = FAKE_ONLY   (not live, not proven)
```

---

## 12. Forbidden claims (hard)

| Forbidden sentence | Why it is false today |
|---|---|
| “Phase 1 complete — both brokers connected” | G01 FAIL. Fake factory. |
| “We ingest live Achiever deals” | Tape is `ClosedRoundTrip(...)` at a frozen `t0`. |
| “StarwaveFX real\standard is a live book” | One canned login `99001`, six canned deals. |
| “Worker health = MT5 health” | Worker can be healthy while flipping a bool. |
| “UI connected ⇒ Manager connected” | Literal `true` in `GetBrokersAsync`. |
| “SDK in tree ⇒ we are connected” | C20 preserve ≠ C# attach. |
| “Seeded 2027 / 9904 means those managers logged on” | Integers on a `Broker` row. Fake never uses them. |
| “InMemory 18 deals is a 5k account sync” | A79 / §69.3 still unmet. |
| “A04 said no connector, so this Fake is the live one” | Fake filled the **file** gap, not the **transport** gap. |

---

## 13. Adjacent facts (do not confuse with a live PASS)

These are real and already measured by siblings. They do **not** flip this verdict.

| Fact | Source | Live? |
|---|---|---|
| Fake `GetGroupsAsync` is not plan-filtered | C10 **PASS** | No |
| DI graph is acyclic; seeder forks a second fake pair | C05 | No |
| C++ SDK not deleted | C20 | No |
| Real FIX send is off | C07 | n/a (destination, not MT5 source) |
| No live passwords in appsettings | B25 | Absence of secrets also means no live logon material in-tree |
| Reconstruction / scoring can run on canned deals | seeder + worker | Demo only |
| `IBrokerConnector` draft still on disk | B24 DEPRECATED | Unused |

---

## 14. Answers to the assigned question

| Question | Answer |
|---|---|
| Are live Achiever connections proven? | **No.** |
| Are live StarwaveFX connections proven? | **No.** |
| What connector does the C# product use? | **`FakeMt5BrokerConnector` only** — two in-memory instances from `DemoBrokerFactory.CreateDefault()`. |
| Does `ConnectAsync` open MT5? | **No.** It sets `_connected = true`. |
| Does the worker talking to `ACHIEVER` / `STARWAVEFX` prove venues? | **No.** Those are fake registry keys. |
| Does the Brokers page “connected” badge prove venues? | **No.** Hard-coded `true`. |
| Does preserved `mt5-sdk` prove venues? | **No.** Not wired; no measured probe output in this pass. |
| Product source changed by this agent? | **No.** |

**Overall class:** live MT5 connect **MISSING**. Fake connector **EXISTS** (demo / test double, wrongly registered as the production broker). Phase 1 / A100 G01 **FAIL**.

---

## 15. Files read (not modified)

- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Enums\PriceSource.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\web\src\pages\BrokersPage.tsx`
- `D:\Prop\docker-compose.yml`
- `D:\Prop\mt5-sdk\src\core\imt5_client.h` (capability only)
- `D:\Prop\mt5-sdk\README.md`
- `D:\Prop\reports\PHASE0_AUDIT.md`
- Sibling reports: B04, C05, C10, C20, A04 (stale inventory), A07 (stale worker), A18, A100 G01, A105

**Written:** `D:\Prop\reports\swarm\20260818\C42_honesty_no_live_mt5.md` (this file).  
**Product source modified:** none.
