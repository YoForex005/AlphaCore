# W500_RESEARCH_151 — Program.cs vs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Field | Value |
|---|---|
| Slot | **151** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_151 |
| Topic | Search every product `Program.cs` for `DemoSeeder`, `FakeMt5`, logins `10001`/`10002`, dummy seed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password values not read, not quoted. Boolean flag name `REAL_COPY_EXECUTION_ENABLED` only. |
| Live attach this slot | **No.** Census cited from prior `LiveBrokerProbe` dump only. |
| Method | Full `read_file` of API / mt5-worker / fix-worker / LiveBrokerProbe `Program.cs`, `DemoSeeder`, `BrokerCatalogSeed`, `FakeMt5BrokerConnector`, `NativeMt5BrokerConnector`, DI, `LiveMt5Registration`, `LiveIngestHostedService`, `CopyTradingHostedService`, `CopyTradingService`, `DealIngestionService`, `EfTradingStore`, `EfDashboardQueries`, `CTraderFixSession`, FIX hosted service, both worker loops. Targeted `grep`. Census from `LIVE_GROUPS_AND_TRADERS.json` (not re-attached). YoPips grep: no product `DemoSeeder`. |
| Siblings (same search, earlier slots) | `W500_RESEARCH_11.md`, `W500_RESEARCH_71.md`, `W500_RESEARCH_91.md`, `W500_RESEARCH_111.md` — **still correct on host Program.cs = 0 dummy tokens.** This slot re-reads disk independently. **111 is stale on copy-arm:** DI no longer hard-false; `/api/settings` `FEATURE_COPY_TRADING_ENABLED=true`; copy hosted service exists. |

**Honesty rule:** older swarm notes (A002, A005, A010, C42, D22) that said “API still calls `DemoSeeder` / health still says FakeMt5 / DI always `CreateDefault()`” are **stale vs current disk**. Slot 111’s “`RealCopyEnabled` forced false in DI L41 and again after FIX logon L68” and “`FEATURE_COPY_TRADING_ENABLED=false`” are **stale vs this tree**. A comment or `LastError` that names `NewOrderSingle` is not a `35=D` builder. `DemoSeeder` existing in the tree is not the same as a host calling it. This slot did not open Manager or FIX sockets.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| Any product `Program.cs` still calls `DemoSeeder` | **No** | **ABSENT** on API + both workers + LiveBrokerProbe |
| Any product `Program.cs` names `FakeMt5` / `10001` / `10002` / `dummy` | **No** (`0` hits, four files) | **ABSENT** |
| Dummy FakeMt5 seed on API startup | **OFF** | `BrokerCatalogSeed.EnsureAsync` only |
| DI can register FakeMt5 when host starts | **No** | fail-closed: real passwords required; connectors are `NativeMt5BrokerConnector` ×2 only |
| Fetch ALL manager-visible groups | **Implemented** on live path | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback |
| Fetch ALL manager traders | **Implemented** on catalog path | `UserRequestArray` per group + `UserLogins` fallback; `GetAccountsAsync(null)` |
| Measured live census (prior probe JSON; not re-attached) | Achiever **8 / 6512**; Starwave **10 / 1948**; total **18 / 8460** | `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16Z` |
| Dummy logins `10001`/`10002`/`10003`/`99001` in that live dump | **0 hits** | Fake-only; not live Manager users |
| Copy pipeline exists on host | **Yes (SHADOW)** | `CopyTradingHostedService` + `/api/copy/status` + `/api/copy/intents` |
| `FEATURE_COPY_TRADING_ENABLED` | **true** literal in `/api/settings` L77 | shadow pipeline ON |
| `RealCopyEnabled` process pin | **Env-bound, not hardcoded false** | DI L41: `REAL_COPY_EXECUTION_ENABLED` equals `"true"` (ignore-case). Lab `.env` L73 is `true`. FIX logon **does not** re-pin false. |
| Copy to cTrader can send a live order | **No** | **`SAFE_BY_ABSENCE`** — product `src`+`apps` `35=D` = **0**; `NewOrderSingleImplemented=false`; `VenueReconciled=false`; persist `AllowFixSend=false`; 0 `ExecutionIntent` writers |
| Residual dummy scoring set | **Yes** | `apps/mt5-worker/Worker.cs` L31 still rebuilds only `{10001,10002,10003,99001}` |
| Residual dummy class in tree | **Yes** | `DemoSeeder` + `DemoBrokerFactory` still exist; product callers = **tests only** |
| Auto-score every catalog login | **Split** | `/api/ops/resync` = `ListLoginsAsync` (all). `LiveIngestHostedService` = `ListLoginsWithDealsAsync` (deals-only). |

**One-line:** Host `Program.cs` files have **zero** `DemoSeeder` / `FakeMt5` / `10001` / `10002` / `dummy` tokens; API startup seeds catalog rows only; live Manager walk can enumerate all groups/traders; live `35=D` is unbuildable so this process cannot take a cTrader loss even though the copy **feature** is on and the lab env arm is `true`.

Slot verdict: **`PASS_HOST_NO_DUMMY`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — no NewOrderSingle encoder; persist-before-send hop writes `SHADOW_ONLY` / `LIVE_SEND_BLOCKED_UNIMPLEMENTED`; no `ExecutionIntent` rows.

---

## 1. Every product `Program.cs` (assigned search)

Grep of `D:\Prop\apps\**\Program.cs` and `D:\Prop\tools\LiveBrokerProbe\Program.cs` for `DemoSeeder|FakeMt5|10001|10002|dummy` (this slot, independently):

| Host | Path | Lines | Hits | Startup seed |
|---|---|---:|---:|---|
| API | `D:\Prop\apps\api\Program.cs` | **159** | **0** | `BrokerCatalogSeed.EnsureAsync` (L156) |
| MT5 worker | `D:\Prop\apps\mt5-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| FIX worker | `D:\Prop\apps\fix-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| Live probe | `D:\Prop\tools\LiveBrokerProbe\Program.cs` | **85** | **0** | none; `LiveMt5Registration.CreateConnectorsFromEnvironment()` |

`DemoSeeder` token under `D:\Prop\apps`: **0** (except historical report text; zero in `*.cs` except `mt5-worker/Worker.cs` dummy login set — **not** `Program.cs`).

Product `Program.cs` hits for the assigned tokens = **0 / 4 files**. The only `Program.cs` files in the tree that still name `DemoSeeder` / `10001` / `10002` / `dummy` live under `D:\Prop\reports\swarm\20260818\_tmp_*` (eval junk, not hosts).

YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `DemoSeeder`. Its `10001`/`10002` hits are official Manager retcodes (`MT_RET_REQUEST_INWAY` / `MT_RET_REQUEST_ACCEPTED`), test `FakeMt5Client` fixtures, and `TERMINAL_ISSUES.md` warm-session notes — not this product's dummy book.

### 1.1 API host — catalog seed, not Fake tape

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

`using TraderIntelligence.Infrastructure.Seeding;` at L7 exists solely for `BrokerCatalogSeed`. There is **no** `DemoSeeder.SeedAsync`.

Health no longer advertises FakeMt5. It reports `LiveRuntimeStatus`:

```39:41:D:\Prop\apps\api\Program.cs
        details = b.Connected
            ? $"live Manager groups={b.Groups} accounts={b.Accounts} phase={b.Phase}"
            : (b.LastError ?? "not connected")
```

`/api/settings` feature flags (L71–77):

- `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (**env-bound** in DI; lab `.env` is `true` — flag only, not a password)
- `FEATURE_COPY_TRADING_ENABLED` = **true** literal (L77) — **delta vs slot 111**

New vs 111: copy query surfaces exist (not senders):

```102:103:D:\Prop\apps\api\Program.cs
app.MapGet("/api/copy/status", (CopyTradingService copy, CancellationToken ct) => copy.GetStatusAsync(ct));
app.MapGet("/api/copy/intents", (CopyTradingService copy, CancellationToken ct) => copy.ListIntentsAsync(200, ct));
```

Recon endpoint note (L69): `"recon runs only after FIX TRADE logon; NewOrderSingle still off"`.

`GET /api/trades` still `Take(200)` — a reconstructed-row **page cap**, not a Manager enumeration cap.

### 1.2 Manual resync walks both brokers and every persisted login

```114:150:D:\Prop\apps\api\Program.cs
app.MapPost("/api/ops/resync", async (
    DealIngestionService ingestion,
    ReconstructionScoringService scoring,
    ITradingStore store,
    LiveRuntimeStatus runtime,
    CancellationToken ct) =>
{
    // ...
    foreach (var code in new[] { "ACHIEVER", "STARWAVEFX" })
    {
        var catalog = await ingestion.SyncCatalogAsync(code, ct);
        var deals = await ingestion.SyncBrokerAsync(code, from, to, ct);
        var brokerId = await store.ResolveBrokerIdAsync(code, ct);
        var logins = await store.ListLoginsAsync(brokerId, ct);
        foreach (var login in logins)
            await scoring.RebuildTraderAsync(code, login, ct);
        // ...
    }
});
```

This is **not** the four dummy numbers. `ListLoginsAsync` = all `Mt5Accounts` for that broker (`EfTradingStore` L339–341).

### 1.3 Worker hosts — same catalog seed

```10:16:D:\Prop\apps\mt5-worker\Program.cs
var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`apps/fix-worker/Program.cs` is the same 18-line pattern. Neither worker `Program.cs` seeds deals, groups, or logins.

`BrokerCatalogSeed` writes broker catalog rows + XAUUSD + kill switch + two FIX session rows (`Disconnected`, TRADE `LastError` = `"session up for logon/recon only; NewOrderSingle off"`). **No** `10001`/`10002`, **no** canned deals, **no** `LoggedOn` forge. Achiever catalog row records proxy `81.29.145.69:49527`; Starwave has no proxy fields. FIX host literals are **demo** `demo-us-eqx-01.p.c-trader.com` / `demo.pepperstone.5328266` (not the live CompID that `DemoSeeder` still paints for tests).

### 1.4 LiveBrokerProbe — native only, no dummy seed

`D:\Prop\tools\LiveBrokerProbe\Program.cs` (85 lines) refuses to run if either password env is whitespace. It walks `CreateConnectorsFromEnvironment()` (Native ×2), calls `GetGroupsAsync` + `GetAccountsAsync(null)`, writes `LIVE_GROUPS_AND_TRADERS.json`. Note on the dump: `"Passwords never written. Groups and manager logins only."` Zero `DemoSeeder`/`FakeMt5`/`10001`/`10002`/`dummy` tokens.

### 1.5 DI refuse-dummy (runs before any Program.cs seed)

```36:59:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        services.AddHostedService<CopyTradingHostedService>();
```

`CreateConnectors` builds **only** two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). Gate: both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` must be non-empty and not a `<SECRET>` / `(a/c` placeholder (`LiveMt5Registration.IsSecret`). Password **values are not quoted here**.

`CreateDefault()` / `FakeMt5BrokerConnector` have **0** callers under `apps/` or `DependencyInjection.cs`. Product callers of `DemoBrokerFactory.CreateDefault()`: `DemoSeeder.cs` L126 + report `_tmp_*` harnesses only.

`DATABASE_URL` placeholder → InMemory (`DependencyInjection` L27–29). Census is process-local unless Postgres is wired.

**Delta vs 111:** `RealCopyEnabled` is **not** a compile-time false. Lab `.env` L73 is `true`, so a host that loads that env will advertise armed. That is **not** a sender.

---

## 2. Where FakeMt5 10001/10002 **still** live (not on host Program.cs)

### 2.1 `DemoSeeder` — test/dev tape, four logins

```126:138:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        var registry = new BrokerRegistry(new IMt5BrokerConnector[] { achiever, starwave });
        var ingestion = new DealIngestionService(registry, store);
        await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, ct);
        await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, ct);

        foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        {
            var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
            await scoring.RebuildTraderAsync(code, login, ct);
        }
```

Product C# callers of `DemoSeeder.SeedAsync` (this pass):

| Caller | Live host? |
|---|---|
| `tests/Integration/SeedingAndStoreTests.cs` L25 | **No** — InMemory fixture |
| `reports/swarm/20260818/_tmp_*` harnesses | **No** — eval junk |

Seeder FIX TRADE `LastError` is `"No live TRADE socket. NewOrderSingle off."` (honest for that tape). Seeder still paints **live-looking** FIX host `live-us-eqx-01.p.c-trader.com` + SenderCompId `live.pepperstone.1369850` into whatever store it is pointed at — **do not** run it against a shared live Postgres. Host catalog seed uses the **demo** CompID, not this live one.

Integration fixture asserts 10001 is **not** `LIVE` and 10002 is `RISK_BLOCKED`. That is tape scoring, not a live send.

Current seeder TRADE/QUOTE statuses are `Disconnected` (D22 “forges LoggedOn” is stale).

### 2.2 `DemoBrokerFactory` — canned 4 groups / 4 accounts / 18 deals

| Broker | Fake groups | Fake logins |
|---|---|---|
| ACHIEVER | `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step` | **10001**, **10002**, 10003 |
| STARWAVEFX | `real\standard` | 99001 |

`FakeMt5BrokerConnector.ConnectAsync` only flips `_connected = true`. No socket, no Manager64, no password.

`10002` tape is a losing martingale (lots 0.10 → 0.20 → 0.40, profits −200 / −500 / −1400). That is fixture risk-block evidence, not capital at risk.

### 2.3 Residual host debt — mt5-worker **scores** the dummy set

`Program.cs` is clean. `Worker.cs` is not:

```29:35:D:\Prop\apps\mt5-worker\Worker.cs
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
```

If this worker is the **only** scorer, it will ingest the live catalog (via native `SyncBrokerAsync`) and then rebuild scores for four **non-existent** dummy logins — **0 / 8460** live traders scored. The API process does **not** have this dummy loop (`LiveIngestHostedService` + `/api/ops/resync` are the live scorers). Completeness defect on the standalone worker, **not** a capital path.

### 2.4 Hosted auto-score is deals-only

```105:125:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    // ...
                    foreach (var login in logins)
                    {
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                        scored++;
                    }
                    _log.LogInformation("{Broker} scored {Scored} logins that have deals", connector.BrokerCode, scored);
```

`ListLoginsWithDealsAsync` = distinct logins that already have `Mt5Deals` rows (`EfTradingStore` L343–345). Catalog fetch of **all** groups/accounts still happens (`SyncCatalogAsync` → `GetGroupsAsync` + `GetAccountsAsync(null)`). Dashboard `GetTradersAsync` iterates **all** `Mt5Accounts` (left-join scores; unscored = `INSUFFICIENT_DATA`).

On catalog failure the hosted service logs `"No dummy data will be substituted."` (L70).

---

## 3. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Native connector (production implementor)

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` L144–186):

1. `GroupRequestArray("*", arr)` — Manager set A, mapping-blind.
2. If that list is empty: `GroupTotal()` + `GroupNext`.
3. Dedup by name (`HashSet` ordinal-ignore-case).

`GetAccountsCore(null)` (L189–214) walks **every** group name from `GetGroupsCore`, then `ReadAccountsForGroup`:

1. `UserRequestArray(gname, users)`
2. fallback `UserGetByGroup`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. `UserAccountRequestArray` / `UserAccountGetByGroup` for balances

No `Take(200)` on this walk. `Ensure()` throws if not connected — **no** Fake fallback.

`DealIngestionService` (146 lines) has **0** `Take(` / `Skip(`. `SyncCatalogAsync` upserts whatever `GetGroupsAsync` / `GetAccountsAsync(null)` return. Positions use `GetGroupPositionsAsync("*")` when bulk.

### 3.2 Dashboard lists the catalog, not the dummy four

`GET /api/groups` = all `Mt5Groups` (no plan-name filter). `GET /api/traders` = all `Mt5Accounts` left-joined to scores (`EfDashboardQueries` L85–128). Unscored catalog rows render as `INSUFFICIENT_DATA`.

### 3.3 Measured live census (do not invent; not re-probed this slot)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` utc **2026-08-18T08:42:16.8519545+00:00**. Passwords never written. This slot did **not** re-open Manager.

Group-count rows re-summed this slot (name list only; logins not copied):

| Broker | Connect | Groups | Accounts | Open positions | Elapsed |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true (HTTP proxy) | 8 | 6512 | 1506 | 7212.6 ms |
| STARWAVEFX | true (direct) | 10 | 1948 | 478 | 6413.5 ms |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (live; **not** Fake `demo\Maxmaster`):

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |
| **sum** | **6512** |

Starwave groups (live; **not** Fake `real\standard`):

| Group | Accounts |
|---|---:|
| `Starwave\cent\FX1\grp1` | 11 |
| `Starwave\cent\FX1\grp2` | 4 |
| `Starwave\demo\FX2\grp1` | 170 |
| `Starwave\demo\FX2\grp2` | 1735 |
| `Starwave\real\FX3\grp1` | 22 |
| `Starwave\real\FX3\grp2` | 0 |
| `Starwave\real\FX3\grp3` | 0 |
| `Starwave\real\FX3\grp4` | 4 |
| `Starwave\real\FX3\grp5` | 0 |
| `Starwave\real\FX3\LP` | 2 |
| **sum** | **1948** |

Grep of that JSON for `"login": 10001` / `10002` / `10003` / `99001`: **0**. Dummy IDs are **not** live Manager traders. First live Achiever contest logins in the dump are **301106 / 301107**. First live Starwave cent login is **2081218**.

Fake vs live overlap is only the **name** `contest\yo-2step` / `demo\yo-2step` on Achiever.

Dashboard pin (`CREDENTIALS_AND_COPY_STATUS.md`): `/api/traders` returned **8460**; `/api/groups` returned **18**. Dummy FakeMt5 seed on API: **OFF**. That file’s “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**” line is **stale** vs current DI (see §4).

---

## 4. Copy to cTrader must not send live orders (no loss)

### 4.1 Outbound FIX from this process

`CTraderFixSession.BuildLogon` is the **only** wire builder. Tag 35 is **`"A"`** (Logon). Fields: 49/56/50/57/52/98/108/141/553/554. No tag 38 (`OrderQty`), no `35=D`. One `WriteAsync` (L49); sockets disposed.

`CTraderFixLogonHostedService` after optional TLS logon **does not** set `RealCopyEnabled = false` (slot 111 L68 claim is stale):

```68:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

`CTraderFixOptions.RealCopyExecutionEnabled` default **false**. That POCO is **not** what DI binds to `LiveRuntimeStatus` (env key is the flat `REAL_COPY_EXECUTION_ENABLED`).

Product grep this pass (`D:\Prop\src` + `D:\Prop\apps` `*.cs`):

| Pattern | Hits |
|---|---|
| `35=D` / `(35, "D")` | **0** |
| `new ExecutionIntent` / `ExecutionIntents.Add` | **0** |
| `NewOrderSingle` | name / log / `LastError` / `MayRetryNewOrderSingle` / `NewOrderSingleImplemented` const only — **not** a builder |
| `WriteAsync` in `Fix.CTrader` | **1** (Logon bytes only) |

fix-worker loop **overwrites** FIX rows to `Disconnected` + `"No live TRADE socket. NewOrderSingle remains off."` even if `CTrader:RealCopyExecutionEnabled` is true. It never opens a TRADE socket. If that nested config is true, it only logs a warning.

### 4.2 Copy pipeline is SHADOW-only (new vs 111)

`CopyTradingHostedService` ticks every 20s and only calls `GenerateShadowIntentsAsync`. Log: `"Copy pipeline created {Count} SHADOW intents. Live NewOrderSingle still blocked."`

`CopyTradingService` constants:

- `VenueReconciled = false`
- `NewOrderSingleImplemented = false`
- persist `AllowFixSend = false` (L192) regardless of `RiskEngine.Evaluate`
- even the “live” branch writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED` (L198–200) — still no TCP
- else `SHADOW_ONLY` + in-process `ShadowCopyEngine.SimulateEntry`

`PersistDemoShadowAsync` writes `CopyIntent.Status = "SHADOW_ONLY"` (`EfTradingStore` L307) and simulated `ShadowOrders`. No TCP. That path **bypasses** `RiskEngine.Evaluate` (slot 119 residual).

Runtime snapshot copy note when flag is true: `"REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."`

`GetStatusAsync` summary when blockers remain: `"Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle."` Blockers always include `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` and `"Venue not reconciled"`.

`RiskEngine.Evaluate` *can* set `AllowFixSend=true` if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Product caller always passes `Reconciled: VenueReconciled` (**false**), so `AllowFixSend` from Evaluate is false on increasing actions (`VENUE_NOT_RECONCILED`). Persist then forces false again.

### 4.3 What “no loss” is **not**

Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Lab env arm `true` + FIX logon proven (per `CREDENTIALS_AND_COPY_STATUS.md`) is **not** a §70 pass. Do not tick Architecture §70 / A101 from this file. Do **not** add a NewOrderSingle in response to this research. This slot did not open FIX TLS.

`CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**” and README “Real NewOrderSingle is off (`REAL_COPY_EXECUTION_ENABLED=false`)” are **stale vs `.env` + DI**. Safety now lives in the missing encoder, not in a forced-false pin.

---

## 5. Stale reports (do not inherit)

| Report | Stale claim | Current disk |
|---|---|---|
| `A002_api_dummy_path.md` | API `Program.cs` calls `DemoSeeder`; health says FakeMt5; resync hardcodes 4 logins | All three **gone** |
| `A005_dashboard_traders.md` | same health string; ingest `Take(200)` | Health is `LiveRuntimeStatus`; ingest `Take` = 0 |
| `A010_prior_swarm.md` | DemoSeeder still called from API + mt5-worker `Program.cs` | Both hosts call `BrokerCatalogSeed` only |
| `C42_honesty_no_live_mt5.md` | sole connector is Fake; DI always `CreateDefault()` | DI registers Native only; live probe previously connected |
| `D22_seeder.md` | DemoSeeder forges `LoggedOn` | Current seeder TRADE/QUOTE are `Disconnected` (and seeder is not on host startup) |
| `W500_RESEARCH_111.md` §1.1 / §1.5 | `FEATURE_COPY=false`; DI + logon force `RealCopyEnabled=false`; API 156 lines | `FEATURE_COPY=true`; DI binds env; logon does not re-pin; API **159** lines + copy endpoints |
| `W500_RESEARCH_108.md` / `CREDENTIALS_AND_COPY_STATUS.md` | flag pinned false everywhere including `.env` | `.env` L73 `true`; DI honors it |
| `W500_RESEARCH_127.md` | “logon re-pins false” | hosted service logs `RealCopyArmed` only |

Superseding live-path note: `A014_live_path_now.md` (seed/DI native path still correct; copy-arm paragraph stale). Sibling no-send notes: `W500_RESEARCH_110.md`, `A003_fix_noloss.md`. Same-topic remasures: slots **11 / 71 / 91 / 111** (this is **151**).

---

## 6. Residual (honest, not a license to send)

1. `DemoSeeder` + `FakeMt5BrokerConnector` remain in `src` for tests. Keep them **off** every host `Program.cs`.
2. `apps/mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` after a live `SyncBrokerAsync`. API `/api/ops/resync` is the fetch-all scorer; hosted ingest scores **deals-only**.
3. `DATABASE_URL` placeholder → InMemory (`DependencyInjection` L27–29). Census is process-local unless Postgres is wired.
4. `GET /api/trades` `Take(200)` can hide older reconstructed rows from the explorer. Not a Manager drop.
5. Lab `.env` `REAL_COPY_EXECUTION_ENABLED=true` is now **bound**. Do not treat that as a send license. Flip it back to `false` if operators want the dashboard to match the missing encoder.
6. Live FIX logon (QUOTE/TRADE) is a **separate** measurement (credentials present per `CREDENTIALS_AND_COPY_STATUS.md`; this slot did not re-open sockets). Even if LoggedOn, send remains unimplemented.

---

## 7. Files read

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`ListLoginsAsync`, `ListLoginsWithDealsAsync`, `SHADOW_ONLY`)
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (`GetGroupsCore` / `GetAccountsCore`)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\appsettings.json` (unbound dead `CSERVER`/5201/5202; `LiveCopyEnabled=false`)
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + group names only)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- Supporting YoPips grep: no `DemoSeeder`; `10001`/`10002` are SDK retcodes / test fixtures

---

## 8. Slot 151 close

```text
Program.cs (API + workers + probe): DemoSeeder/FakeMt5/10001/10002/dummy = 0 hits.
Dummy seed OFF. Live catalog = GroupRequestArray("*") + all UserRequestArray.
Prior measure 18 groups / 8460 traders. Dummy logins absent from live dump.
Hosted score = ListLoginsWithDealsAsync. Worker residual still scores four dummy logins.
Copy feature ON (SHADOW). Env REAL_COPY may be true; 35=D missing; NOS unimplemented.
Capital at risk from this process: NONE (SAFE_BY_ABSENCE).
```
