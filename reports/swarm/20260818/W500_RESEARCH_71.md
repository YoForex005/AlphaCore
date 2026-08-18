# W500_RESEARCH_71 — Program.cs vs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Field | Value |
|---|---|
| Slot | **71** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_71 |
| Topic | Search every product `Program.cs` for `DemoSeeder`, `FakeMt5`, logins `10001`/`10002`, dummy seed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password values not read, not quoted. |
| Method | Full `read_file` of API / mt5-worker / fix-worker / LiveBrokerProbe `Program.cs`, `DemoSeeder`, `BrokerCatalogSeed`, `FakeMt5BrokerConnector`, DI, `LiveMt5Registration`, `LiveIngestHostedService`, `DealIngestionService`, `CTraderFixSession`, FIX hosted service, worker loops. Targeted `grep`. Census from prior `LIVE_GROUPS_AND_TRADERS.json` (this slot did **not** live-attach). Supporting YoPips grep: no product `DemoSeeder`. |
| Sibling (same question, earlier slot) | `W500_RESEARCH_11.md` — **still correct on host Program.cs = 0 dummy tokens.** Stale on one scoring claim (see §2.4). |

**Honesty rule:** older swarm notes (A002, A005, A010, C42, D22) that said “API still calls `DemoSeeder` / health still says FakeMt5 / DI always `CreateDefault()`” are **stale vs current disk**. A comment or `LastError` that names `NewOrderSingle` is not a `35=D` builder. `DemoSeeder` existing in the tree is not the same as a host calling it. This slot did not open Manager or FIX sockets.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| Any product `Program.cs` still calls `DemoSeeder` | **No** | **ABSENT** on API + both workers + LiveBrokerProbe |
| Any product `Program.cs` names `FakeMt5` / `10001` / `10002` / `dummy` | **No** (`0` hits) | **ABSENT** |
| Dummy FakeMt5 seed on API startup | **OFF** | `BrokerCatalogSeed.EnsureAsync` only |
| DI can register FakeMt5 when host starts | **No** | fail-closed: real passwords required; connectors are `NativeMt5BrokerConnector` ×2 only |
| Fetch ALL manager-visible groups | **Implemented** on live path | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback |
| Fetch ALL manager traders | **Implemented** on catalog path | `UserRequestArray` per group + `UserLogins` fallback; `GetAccountsAsync(null)` |
| Measured live census (prior probe JSON; not re-attached) | Achiever **8 / 6512**; Starwave **10 / 1948**; total **18 / 8460** | `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16Z` |
| Dummy logins `10001`/`10002`/`10003`/`99001` in that live dump | **0 hits** | Fake-only; not live Manager users |
| Copy to cTrader can send a live order | **No** | **`SAFE_BY_ABSENCE`** — product `src`+`apps` `35=D` = **0**; `RealCopyEnabled = false` hardcoded |
| Residual dummy scoring set | **Yes** | `apps/mt5-worker/Worker.cs` L31 still rebuilds only `{10001,10002,10003,99001}` |
| Residual dummy class in tree | **Yes** | `DemoSeeder` + `DemoBrokerFactory` still exist; product callers = **tests only** |
| Auto-score every catalog login | **Split** | `/api/ops/resync` = `ListLoginsAsync` (all). `LiveIngestHostedService` = `ListLoginsWithDealsAsync` (deals-only). Slot 11 said hosted service walks `ListLoginsAsync` — **stale**. |

**One-line:** Host `Program.cs` files have **zero** `DemoSeeder` / `FakeMt5` / `10001` / `10002` / `dummy` tokens; API startup seeds catalog rows only; live Manager walk can enumerate all groups/traders; live `35=D` is unbuildable so this process cannot take a cTrader loss.

Slot verdict: **`PASS_HOST_NO_DUMMY`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — no NewOrderSingle encoder; copy flag forced off in DI and again after FIX logon; shadow/CopyIntent only.

---

## 1. Every product `Program.cs` (assigned search)

Grep of `D:\Prop\apps\**\Program.cs` and `D:\Prop\tools\LiveBrokerProbe\Program.cs` for `DemoSeeder|FakeMt5|10001|10002|dummy|Dummy`:

| Host | Path | Lines | Hits | Startup seed |
|---|---|---:|---:|---|
| API | `D:\Prop\apps\api\Program.cs` | **156** | **0** | `BrokerCatalogSeed.EnsureAsync` (L153) |
| MT5 worker | `D:\Prop\apps\mt5-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| FIX worker | `D:\Prop\apps\fix-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| Live probe | `D:\Prop\tools\LiveBrokerProbe\Program.cs` | **85** | **0** | none; `LiveMt5Registration.CreateConnectorsFromEnvironment()` |

`DemoSeeder` token under `D:\Prop\apps`: **0**.

YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `DemoSeeder`. Its `10001`/`10002` hits are official Manager retcodes (`MT_RET_REQUEST_INWAY` / `MT_RET_REQUEST_ACCEPTED`) and test `FakeMt5Client` fixtures — not this product's dummy book.

### 1.1 API host — catalog seed, not Fake tape

```149:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

`using TraderIntelligence.Infrastructure.Seeding;` at L6 exists solely for `BrokerCatalogSeed`. There is **no** `DemoSeeder.SeedAsync`.

Health no longer advertises FakeMt5. It reports `LiveRuntimeStatus`:

```39:41:D:\Prop\apps\api\Program.cs
        details = b.Connected
            ? $"live Manager groups={b.Groups} accounts={b.Accounts} phase={b.Phase}"
            : (b.LastError ?? "not connected")
```

`/api/settings` feature flags:

- `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (forced **false** in DI L41, re-forced **false** after FIX logon)
- `FEATURE_COPY_TRADING_ENABLED` = **false** literal (L76)

Recon endpoint note (L68): `"recon runs only after FIX TRADE logon; NewOrderSingle still off"`.

`GET /api/trades` still `Take(200)` — a reconstructed-row **page cap**, not a Manager enumeration cap.

### 1.2 Manual resync walks both brokers and every persisted login

```111:147:D:\Prop\apps\api\Program.cs
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

`BrokerCatalogSeed` writes broker catalog rows + XAUUSD + kill switch + two FIX session rows (`Disconnected`, TRADE `LastError` = `"session up for logon/recon only; NewOrderSingle off"`). **No** `10001`/`10002`, **no** canned deals, **no** `LoggedOn` forge. Achiever catalog row records proxy `81.29.145.69:49527`; Starwave has no proxy fields.

### 1.4 DI refuse-dummy (runs before any Program.cs seed)

```35:56:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
```

`CreateConnectors` builds **only** two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). Gate: both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` must be non-empty and not a `<SECRET>` / `(a/c` placeholder (`LiveMt5Registration.IsSecret`). Password **values are not quoted here**.

`CreateDefault()` / `FakeMt5BrokerConnector` have **0** callers under `apps/` or `DependencyInjection.cs`.

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

Seeder FIX TRADE `LastError` is `"No live TRADE socket. NewOrderSingle off."` (honest for that tape). Seeder still paints live-looking IPs / manager logins / Pepperstone CompIDs into whatever store it is pointed at — **do not** run it against a shared live Postgres.

Integration fixture asserts 10001 is **not** `LIVE` and 10002 is `RISK_BLOCKED`. That is tape scoring, not a live send.

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

If this worker is the **only** scorer, it will ingest the live catalog (via native `SyncBrokerAsync`) and then rebuild scores for four **non-existent** dummy logins — **0 / 8460** live traders scored. The API process does **not** have this dummy loop. Completeness defect on the standalone worker, **not** a capital path.

### 2.4 Hosted auto-score is deals-only (correction vs slot 11)

Slot 11 claimed `LiveIngestHostedService` walks `store.ListLoginsAsync` for every persisted account. **Current disk:**

```105:113:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    // ...
                    foreach (var login in logins)
                    {
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
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

### 3.2 Ingest + dashboard

`DealIngestionService.SyncCatalogAsync` upserts **whatever** `GetGroupsAsync` / `GetAccountsAsync(null)` return. `SyncBrokerAsync` then pulls deals by group (`IMt5BulkDealReader`) and positions via `GetGroupPositionsAsync("*")` (or per-account if the connector is not bulk).

`GET /api/groups` = all `Mt5Groups` (no plan-name filter). `GET /api/traders` = all `Mt5Accounts`.

### 3.3 Measured live census (do not invent; not re-probed this slot)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` utc **2026-08-18T08:42:16.8519545+00:00**. Passwords never written. This slot did **not** re-open Manager.

| Broker | Connect | Groups | Accounts | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | true (HTTP proxy) | 8 | 6512 | 1506 |
| STARWAVEFX | true (direct) | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

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

Grep of that JSON for `"login": 10001` / `10002` / `10003` / `99001`: **0**. Dummy IDs are **not** live Manager traders. First live Achiever contest logins in the dump are **301106 / 301107**.

Fake vs live overlap is only the **name** `contest\yo-2step` / `demo\yo-2step` on Achiever.

---

## 4. Copy to cTrader must not send live orders (no loss)

### 4.1 Outbound FIX from this process

`CTraderFixSession.BuildLogon` is the **only** wire builder. Tag 35 is **`"A"`** (Logon). Fields: 49/56/50/57/52/98/108/141/553/554. No tag 38 (`OrderQty`), no `35=D`. One `WriteAsync`; sockets disposed.

`CTraderFixLogonHostedService` after optional TLS logon:

```68:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;
        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

`CTraderFixOptions.RealCopyExecutionEnabled` default **false**. DI **never** sets `RealCopyEnabled` true.

Product grep this pass (`D:\Prop\src` + `D:\Prop\apps` `*.cs`):

| Pattern | Hits |
|---|---|
| `35=D` / `(35, "D")` | **0** |
| `NewOrderSingle` | name / log / `LastError` / `MayRetryNewOrderSingle` only — **not** a builder |

fix-worker loop **overwrites** FIX rows to `Disconnected` + `"No live TRADE socket. NewOrderSingle remains off."` even if config flag is true. It never opens a TRADE socket.

### 4.2 Shadow is not a live send

`PersistDemoShadowAsync` writes `CopyIntent.Status = "SHADOW_ONLY"` (`EfTradingStore` L307) and simulated `ShadowOrders`. No TCP.

Runtime snapshot copy note when flag is false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

### 4.3 What “no loss” is **not**

Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do not tick Architecture §70 / A101 from this file. Do **not** add a NewOrderSingle in response to this research. This slot did not open FIX TLS.

---

## 5. Stale reports (do not inherit)

| Report | Stale claim | Current disk |
|---|---|---|
| `A002_api_dummy_path.md` | API `Program.cs` calls `DemoSeeder`; health says FakeMt5; resync hardcodes 4 logins | All three **gone** |
| `A005_dashboard_traders.md` | same health string; ingest `Take(200)` | Health is `LiveRuntimeStatus`; ingest `Take` = 0 |
| `A010_prior_swarm.md` | DemoSeeder still called from API + mt5-worker `Program.cs` | Both hosts call `BrokerCatalogSeed` only |
| `C42_honesty_no_live_mt5.md` | sole connector is Fake; DI always `CreateDefault()` | DI registers Native only; live probe previously connected |
| `D22_seeder.md` | DemoSeeder forges `LoggedOn` | Current seeder TRADE/QUOTE are `Disconnected` (and seeder is not on host startup) |
| `W500_RESEARCH_11.md` §2.3 | hosted ingest scores `ListLoginsAsync` (every account) | hosted ingest scores `ListLoginsWithDealsAsync` |

Superseding live-path note: `A014_live_path_now.md`. Sibling no-send notes: `W500_RESEARCH_50.md`, `A003_fix_noloss.md`.

---

## 6. Residual (honest, not a license to send)

1. `DemoSeeder` + `FakeMt5BrokerConnector` remain in `src` for tests. Keep them **off** every host `Program.cs`.
2. `apps/mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` after a live `SyncBrokerAsync`. API `/api/ops/resync` is the fetch-all scorer; hosted ingest scores **deals-only**.
3. `DATABASE_URL` placeholder → InMemory (`DependencyInjection` L26–28). Census is process-local unless Postgres is wired.
4. `GET /api/trades` `Take(200)` can hide older reconstructed rows from the explorer. Not a Manager drop.
5. Live FIX logon (QUOTE/TRADE) is a **separate** measurement (credentials present per `CREDENTIALS_AND_COPY_STATUS.md`; this slot did not re-open sockets). Even if LoggedOn, send remains off.

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
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`ListLoginsAsync`, `ListLoginsWithDealsAsync`, `SHADOW_ONLY`)
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (`GetGroupsCore` / `GetAccountsCore`)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + group names only)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\W500_RESEARCH_11.md` (sibling; scoring claim remesured)
- Supporting YoPips grep: no `DemoSeeder`; `10001`/`10002` are SDK retcodes / test fixtures

---

## 8. Slot 71 close

```text
Program.cs (API + workers + probe): DemoSeeder/FakeMt5/10001/10002/dummy = 0 hits.
Dummy seed OFF. Live catalog = GroupRequestArray("*") + all UserRequestArray.
Prior measure 18 groups / 8460 traders. Dummy logins absent from live dump.
Hosted score = ListLoginsWithDealsAsync (slot 11 ListLoginsAsync claim stale).
cTrader copy = logon-only; 35=D missing; RealCopyEnabled forced false.
Capital at risk from this process: NONE (SAFE_BY_ABSENCE).
```
