# W500_RESEARCH_91 — Program.cs vs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Field | Value |
|---|---|
| Slot | **91** |
| Agent | W500_RESEARCH_91 |
| Date | 2026-08-18 |
| Topic | Search every product `Program.cs` for `DemoSeeder`, `FakeMt5`, logins `10001`/`10002`, dummy seed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** Password values not read, not quoted. |
| Live attach this slot | **No.** Census cited from prior probe dump only. |
| Method | `read_file` + `grep` on `D:\Prop` product `Program.cs`, seeder, Fake/Native connectors, DI, ingest, FIX session, live census JSON. Supporting read of YoPips `mt5_manager.cpp` (group walk recipe only). |

**Honesty rule:** older swarm notes (A002, A005, C42, D22) that said “API still calls `DemoSeeder` / health still says FakeMt5 / live Manager not proven” are **stale vs current disk**. Sibling `W500_RESEARCH_11.md` covered the same search earlier today; this slot **re-reads the files as they sit now** and records one completeness delta (API live scorer uses `ListLoginsWithDealsAsync`, not every `Mt5Account`). A comment that names `NewOrderSingle` is not a `35=D` builder. `DemoSeeder` existing in the tree is not the same as a host calling it.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| Any product `Program.cs` still calls `DemoSeeder` | **No** | **ABSENT** on API + both workers + probe |
| Any product `Program.cs` names `FakeMt5` / `10001` / `10002` / `dummy` | **No** (`0` hits, four files) | **ABSENT** |
| Dummy FakeMt5 seed on API startup | **OFF** | `BrokerCatalogSeed.EnsureAsync` only |
| DI can register FakeMt5 when host starts | **No** | fail-closed: real passwords required; connectors are `NativeMt5BrokerConnector` only |
| Fetch ALL manager-visible groups | **Implemented** on live path | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback |
| Fetch ALL manager traders into catalog | **Implemented** on live path | `UserRequestArray` per group + `UserLogins` fallback; `GetAccountsAsync(null)` |
| Auto-score every catalog login | **No** | `LiveIngestHostedService` scores `ListLoginsWithDealsAsync` only |
| Measured live census (probe JSON) | Achiever **8 / 6512**; Starwave **10 / 1948**; total **18 / 8460** | `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16Z` |
| Dummy logins `10001`/`10002`/`10003`/`99001` in that live dump | **0 hits** | Fake-only; not live Manager users |
| Copy to cTrader can send a live order | **No** | **`SAFE_BY_ABSENCE`** — no `35=D` builder; `RealCopyEnabled = false` hardcoded |
| Residual dummy scoring set | **Yes** | `apps/mt5-worker/Worker.cs` L31 still rebuilds only `{10001,10002,10003,99001}` |
| Residual dummy class in tree | **Yes** | `DemoSeeder` + `DemoBrokerFactory` still exist; called by **tests** only |

**One-line:** Host `Program.cs` files no longer seed FakeMt5 10001/10002; live ingest can enumerate all Manager groups/traders; live `35=D` is unbuildable so this process cannot take a cTrader loss. Do not treat the leftover `DemoSeeder` file or the mt5-worker four-login scorer as the API live path.

Slot verdict: **`PASS_HOST_NO_DUMMY`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — no NewOrderSingle encoder; copy flag forced off; shadow/CopyIntent only.

---

## 1. Every product `Program.cs` (assigned search)

Grep of these four files for `DemoSeeder|FakeMt5|10001|10002|dummy` (this slot, independently):

| Host | Path | Lines | Hits | Startup seed |
|---|---|---:|---:|---|
| API | `D:\Prop\apps\api\Program.cs` | 156 | **0** | `BrokerCatalogSeed.EnsureAsync` |
| MT5 worker | `D:\Prop\apps\mt5-worker\Program.cs` | 18 | **0** | `BrokerCatalogSeed.EnsureAsync` |
| FIX worker | `D:\Prop\apps\fix-worker\Program.cs` | 18 | **0** | `BrokerCatalogSeed.EnsureAsync` |
| Live probe | `D:\Prop\tools\LiveBrokerProbe\Program.cs` | 86 | **0** | no seed; `LiveMt5Registration.CreateConnectorsFromEnvironment()` |

`DemoSeeder` token under `D:\Prop\apps`: **0**.

Hits that **do** exist are **not** hosts:

| Location | Why it does not count as host dummy |
|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still on disk; no product host calls it |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | test/factory tape: 10001/10002/10003 + 99001 |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | InMemory fixture only |
| `D:\Prop\apps\mt5-worker\Worker.cs` L31 | residual scorer (not `Program.cs`) |
| `D:\Prop\reports\swarm\20260818\_tmp_*\Program.cs` | eval junk, not shipped |

### 1.1 API host — catalog seed, not Fake tape

```149:155:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`using TraderIntelligence.Infrastructure.Seeding;` exists only for `BrokerCatalogSeed`. There is no `DemoSeeder.SeedAsync`.

Manual resync walks **both** live broker codes and **every** login already in the store — not the four dummy numbers:

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

Health no longer advertises FakeMt5. It reports `LiveRuntimeStatus` (`live Manager groups=… accounts=…` or `LastError`). Feature flags on `/api/settings`:

- `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (forced **false** in DI)
- `FEATURE_COPY_TRADING_ENABLED` = **false** literal (`Program.cs` L76)

Recon endpoint note (L68): `"recon runs only after FIX TRADE logon; NewOrderSingle still off"`.

`GET /api/trades` still `Take(200)` — a reconstructed-row **page cap**, not a Manager enumeration cap. Grep of `D:\Prop\src` for `Take(200)`: **0**. Sole product `Take(200)` is this API page.

### 1.2 Worker hosts — same catalog seed

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

`BrokerCatalogSeed` writes broker catalog rows + XAUUSD + kill switch + two FIX session rows (`Disconnected`, TRADE `LastError` = `"session up for logon/recon only; NewOrderSingle off"`). **No** `10001`/`10002`, **no** canned deals, **no** `LoggedOn` forge.

### 1.3 DI refuse-dummy (runs before any Program.cs seed)

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

`CreateConnectors` builds **only** two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). Gate: both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` must be non-empty and not a `<SECRET>` / `(a/c` placeholder. Password **values are not quoted here**.

`FakeMt5BrokerConnector` / `DemoBrokerFactory.CreateDefault` product callers: **`DemoSeeder` only**. DI never registers them.

---

## 2. Where FakeMt5 10001/10002 **still** live (not on host Program.cs)

### 2.1 `DemoSeeder` — test/dev tape, four logins

```126:138:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        var registry = new BrokerRegistry(new IMt5BrokerConnector[] { achiever, starwave });
        var ingestion = new DealIngestionService(registry, store);
        // ...
        await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, ct);
        await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, ct);

        foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        {
            var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
            await scoring.RebuildTraderAsync(code, login, ct);
        }
```

Product C# callers of `DemoSeeder.SeedAsync`:

| Caller | Live host? |
|---|---|
| `tests/Integration/SeedingAndStoreTests.cs` L25 | **No** — InMemory fixture |
| `reports/swarm/20260818/_tmp_*` harnesses | **No** — eval junk |

Integration fixture asserts 10001 is **not** `LIVE` and 10002 is `RISK_BLOCKED`. That is scoring of canned tape, not a live send.

Seeder FIX TRADE `LastError` is `"No live TRADE socket. NewOrderSingle off."` (honest for that tape). Seeder still paints live-looking IPs / manager logins / Pepperstone CompIDs into whatever store it is pointed at — **do not** run it against a shared live Postgres.

### 2.2 `DemoBrokerFactory` — canned 4 groups / 4 accounts / 18 deals

| Broker | Fake groups | Fake logins |
|---|---|---|
| ACHIEVER | `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step` | **10001**, **10002**, 10003 |
| STARWAVEFX | `real\standard` | 99001 |

`FakeMt5BrokerConnector.ConnectAsync` only flips `_connected = true`. No socket, no Manager64, no password.

`10002` tape is a losing martingale (lots 0.10 → 0.20 → 0.40, profits −200 / −500 / −1400).

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

If this worker is the **only** scorer, it will ingest the live catalog (via native `SyncBrokerAsync`) and then rebuild scores for four **non-existent** dummy logins — **0 / 8460** live traders scored. The API process does **not** have this four-login loop.

This is a **completeness** defect on the standalone worker, not a capital path.

---

## 3. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Native connector (production implementor)

Connect (`NativeMt5BrokerConnector.cs` L88–111): try pump `GROUPS|USERS|POSITIONS`; on fail retry `PUMP_MODE_NONE`. Fetch does **not** switch to Fake.

`GetGroupsCore` (L144–186):

1. `GroupRequestArray("*", arr)` — Manager set A, mapping-blind.
2. If that list is empty: `GroupTotal()` + `GroupNext`.
3. Dedup by name (`HashSet` ordinal-ignore-case).

`GetAccountsCore(null)` (L189–214) walks **every** group name from `GetGroupsCore`, then `ReadAccountsForGroup`:

1. `UserRequestArray(gname, users)`
2. fallback `UserGetByGroup`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. `UserAccountRequestArray` / `UserAccountGetByGroup` for balances

No `Take(200)` on this walk. `Ensure()` throws if not connected — **no** Fake fallback.

### 3.2 YoPips (recipe only; not this product’s live enumerator)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

- Connect remaps `pumpMode==0` to USERS|ORDERS|POSITIONS|SYMBOLS, then retries true `0` (`L103–135`).
- `GetAllGroups` (`L962–982`) is **cache-only** `GroupTotal` + `GroupNext`. It does **not** call `GroupRequestArray`.
- Traders: `UserLogins` (`L315+`) is a true request API.

Do **not** treat YoPips `GetAllGroups` after pump-none as “ALL groups.” The C# live path is the ALL-groups collector.

### 3.3 Ingest + dashboard

`DealIngestionService.SyncCatalogAsync` upserts **whatever** `GetGroupsAsync` / `GetAccountsAsync(null)` return. `SyncBrokerAsync` then pulls deals by group (`IMt5BulkDealReader`) and positions via `GetGroupPositionsAsync("*")`.

`LiveIngestHostedService`: connect → catalog → (if connected) deals → **`ListLoginsWithDealsAsync`** → `RebuildTraderAsync` per that subset. On catalog failure it logs `"No dummy data will be substituted."` (L70).

**Slot-91 completeness pin (delta vs W500_11):**

| Surface | Login set scored |
|---|---|
| `/api/ops/resync` | `ListLoginsAsync` = every `Mt5Accounts` row |
| `LiveIngestHostedService` | `ListLoginsWithDealsAsync` = distinct `Mt5Deals.Login` only |
| `EfDashboardQueries.GetTradersAsync` | **all** `Mt5Accounts` (unscored → `INSUFFICIENT_DATA`) |
| standalone mt5-worker | `{10001,10002,10003,99001}` only |

`ListLoginsAsync` (`EfTradingStore.cs` L339–341) = `_db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login)`.

`ListLoginsWithDealsAsync` (L343–345) = `_db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct()`.

Catalog fetch is still ALL groups + ALL traders. Auto-score is **not** ALL traders. Dashboard still lists every account. This is a scoring completeness gap, not a Manager drop, and **not** a capital path.

`GetGroupsAsync` dashboard = all `Mt5Groups` (no plan-name filter).

### 3.4 Measured live census (do not invent; this slot did not re-attach)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` utc **2026-08-18T08:42:16.8519545+00:00**. Passwords never written.  
Confirmed this slot: grep of that JSON for `"login": 10001` / `10002` / `10003` / `99001` = **0**.

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

First live Achiever logins in the dump are **301106 / 301107** (`contest\yo-1step`), not 10001. Fake vs live overlap is only the **name** `contest\yo-2step` / `demo\yo-2step` on Achiever.

Dashboard `/api/traders` = 8460, `/api/groups` = 18 (`CREDENTIALS_AND_COPY_STATUS.md`).

---

## 4. Copy to cTrader must not send live orders (no loss)

### 4.1 Outbound FIX from this process

`CTraderFixSession.BuildLogon` is the **only** wire builder. Tag 35 is **`"A"`** (Logon). Fields: 49/56/50/57/52/98/108/141/553/554. No tag 38 (`OrderQty`), no `35=D`.

`CTraderFixLogonHostedService` after optional TLS logon:

```68:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;
        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

`CTraderFixOptions.RealCopyExecutionEnabled` default **false**. DI **never** sets `RealCopyEnabled` true.

Product C# grep this pass:

| Pattern | Hits in `src` + `apps` `*.cs` |
|---|---|
| `35=D` / `(35, "D")` / `MsgType="D"` | **0** |
| `DealerSend` / `OrderAdd` / `TradeBalance` / `DealerBalance` | **0** |
| `NewOrderSingle` | name / log / `LastError` / `MayRetryNewOrderSingle` only |

fix-worker loop **overwrites** FIX rows to `Disconnected` + `"No live TRADE socket. NewOrderSingle remains off."` even if config flag is true. It never opens a TRADE socket.

LiveBrokerProbe is MT5 read-only. It does not touch FIX.

### 4.2 Shadow is not a live send

`PersistDemoShadowAsync` writes `CopyIntent.Status = "SHADOW_ONLY"` and simulated `ShadowOrders` via `ShadowCopyEngine.SimulateEntry`. No TCP. Blocked / non-SHADOW states write **zero** shadow rows (10002 RISK_BLOCKED path).

UI (`LiveCopyPage.tsx`): “Pepperstone/cTrader NewOrderSingle is disabled so this process cannot open a losing live position.”

Runtime snapshot copy note when flag is false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

### 4.3 What “no loss” is **not**

Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Risk + recon + quantity conversion are **not** a wired send choke, because there is no sender. Do not tick Architecture §70 / A101 from this file. Do **not** add a NewOrderSingle in response to this research.

Risk to **source** traders: none — this path does not `UserUpdate` / `DealAdd` / `PositionUpdate`.

---

## 5. Stale reports (do not inherit)

| Report | Stale claim | Current disk |
|---|---|---|
| `A002_api_dummy_path.md` | API `Program.cs` calls `DemoSeeder`; health says FakeMt5; resync hardcodes 4 logins | All three **gone** |
| `A005_dashboard_traders.md` | same health string; ingest `Take(200)` | Health is `LiveRuntimeStatus`; ingest `Take` = 0 |
| `C42_honesty_no_live_mt5.md` | sole connector is Fake; DI always `CreateDefault()` | DI registers Native only; live probe connected |
| `D22_seeder.md` | DemoSeeder forges `LoggedOn` | Current seeder TRADE/QUOTE are `Disconnected` (and seeder is not on host startup) |
| `W500_RESEARCH_11.md` | live ingest scores `ListLoginsAsync` | Current `LiveIngestHostedService` L106 = `ListLoginsWithDealsAsync` |

Superseding live-path note: `A014_live_path_now.md`. Sibling no-send notes: `A003_fix_noloss.md`, `W500_RESEARCH_70.md`.

---

## 6. Residual (honest, not a license to send)

1. `DemoSeeder` + `FakeMt5BrokerConnector` remain in `src` for tests. Keep them **off** every host `Program.cs`.
2. `apps/mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` after a live `SyncBrokerAsync`. API `/api/ops/resync` is the fetch-all scorer; hosted ingest scores deal-bearing logins only.
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
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`PersistDemoShadowAsync`, `ListLoginsAsync`, `ListLoginsWithDealsAsync`)
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + group names only)
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- Supporting: YoPips `src\core\mt5_manager.cpp` `GetAllGroups` / Connect remap; sibling `W500_RESEARCH_11.md` (same topic, earlier slot)

---

## 8. Slot 91 close

```text
Program.cs (API + workers + probe): DemoSeeder/FakeMt5/10001/10002/dummy = 0 hits.
Dummy seed OFF. Live catalog = GroupRequestArray("*") + all UserRequestArray.
Measured 18 groups / 8460 traders. Dummy logins absent from live dump.
Hosted auto-score = logins-with-deals only; /api/ops/resync scores all accounts.
mt5-worker still loops 10001/10002/10003/99001 (completeness, not capital).
cTrader copy = logon-only; 35=D missing; RealCopyEnabled forced false.
Capital at risk from this process: NONE (SAFE_BY_ABSENCE).
```
