# W500_RESEARCH_131 — Program.cs vs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Field | Value |
|---|---|
| Slot | **131** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_131 |
| Topic | Search every product `Program.cs` for `DemoSeeder`, `FakeMt5`, logins `10001`/`10002`, dummy seed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password values not read, not quoted. Feature-flag literal `.env` `REAL_COPY_EXECUTION_ENABLED=true` is named as a flag, not a secret. |
| Live attach this slot | **No.** Census cited from prior `LiveBrokerProbe` dump only. |
| Method | Full `read_file` of API / mt5-worker / fix-worker / LiveBrokerProbe `Program.cs`, `DemoSeeder`, `BrokerCatalogSeed`, `FakeMt5BrokerConnector`, `NativeMt5BrokerConnector`, DI, `LiveMt5Registration`, `LiveIngestHostedService`, `DealIngestionService`, `CopyTradingService`, `CopyTradingHostedService`, `CTraderFixSession`, FIX hosted service, both worker loops, `EnvFile`. Targeted `grep`. Census from `LIVE_GROUPS_AND_TRADERS.json` (not re-attached). YoPips grep: no product `DemoSeeder`. |
| Siblings (same search, earlier slots) | `W500_RESEARCH_11.md`, `W500_RESEARCH_71.md`, `W500_RESEARCH_91.md`, `W500_RESEARCH_111.md` — **still correct on host Program.cs = 0 dummy tokens.** This slot re-reads disk independently. **Do not inherit** 91/111 claims that DI hardcodes `RealCopyEnabled=false` or that FIX logon re-pins it. |

**Honesty rule:** older swarm notes (A002, A005, A010, C42, D22) that said “API still calls `DemoSeeder` / health still says FakeMt5 / DI always `CreateDefault()`” are **stale vs current disk**. A comment or `LastError` that names `NewOrderSingle` is not a `35=D` builder. `DemoSeeder` existing in the tree is not the same as a host calling it. `.env` `REAL_COPY_EXECUTION_ENABLED=true` is an **operator arm**, not a ticket. This slot did not open Manager or FIX sockets.

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
| Copy to cTrader can send a live order | **No** | **`SAFE_BY_ABSENCE`** — product `src`+`apps` `35=D` = **0**; `NewOrderSingleImplemented = false`; **0** `ExecutionIntent` writers |
| Residual dummy scoring set | **Yes** | `apps/mt5-worker/Worker.cs` L31 still rebuilds only `{10001,10002,10003,99001}` |
| Residual dummy class in tree | **Yes** | `DemoSeeder` + `DemoBrokerFactory` still exist; product callers = **tests only** |
| Auto-score every catalog login | **Split** | `/api/ops/resync` = `ListLoginsAsync` (all). `LiveIngestHostedService` = `ListLoginsWithDealsAsync` (deals-only). |
| `REAL_COPY` still forced false in process | **No (delta vs 91/111)** | DI L41 binds `configuration["REAL_COPY_EXECUTION_ENABLED"]`. `.env` L73 is **`true`**. FIX hosted service **no longer** overwrites the flag. Pipeline is SHADOW only. |

**One-line:** Host `Program.cs` files have **zero** `DemoSeeder` / `FakeMt5` / `10001` / `10002` / `dummy` tokens; API startup seeds catalog rows only; live Manager walk can enumerate all groups/traders; live `35=D` is unbuildable so this process cannot take a cTrader loss even if `.env` arms REAL_COPY.

Slot verdict: **`PASS_HOST_NO_DUMMY`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — no NewOrderSingle encoder; `CopyTradingService.NewOrderSingleImplemented` const false; persist `AllowFixSend = false`; shadow/CopyIntent only. Flag-pin defence is **gone**; absence of `35=D` is the remaining capital lock.

---

## 1. Every product `Program.cs` (assigned search)

Grep of `D:\Prop\apps\**\Program.cs` and `D:\Prop\tools\LiveBrokerProbe\Program.cs` for `DemoSeeder|FakeMt5|10001|10002|dummy` (this slot, independently):

| Host | Path | Lines | Hits | Startup seed |
|---|---|---:|---:|---|
| API | `D:\Prop\apps\api\Program.cs` | **160** | **0** | `BrokerCatalogSeed.EnsureAsync` (L156) |
| MT5 worker | `D:\Prop\apps\mt5-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| FIX worker | `D:\Prop\apps\fix-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| Live probe | `D:\Prop\tools\LiveBrokerProbe\Program.cs` | **86** | **0** | none; `LiveMt5Registration.CreateConnectorsFromEnvironment()` |

`DemoSeeder` token under `D:\Prop\apps`: **0**.

Product `Program.cs` hits for the assigned tokens = **0 / 4 files**. The only `Program.cs` files in the tree that still name `DemoSeeder` / `10001` / `10002` live under `D:\Prop\reports\swarm\20260818\_tmp_*` (eval junk, not hosts) and `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` (InMemory fixture).

YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `DemoSeeder` and **no** `FakeMt5BrokerConnector`. Its `10001`/`10002` hits are official Manager retcodes (`MT_RET_REQUEST_INWAY` / `MT_RET_REQUEST_ACCEPTED`) plus test-fixture logins — not this product's dummy book.

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

`using TraderIntelligence.Infrastructure.Seeding;` at L6 exists solely for `BrokerCatalogSeed`. There is **no** `DemoSeeder.SeedAsync`.

`EnvFile.FindAndLoad()` at L10 loads `D:\Prop\.env` into process environment **before** `AddTraderIntelligence`. Health no longer advertises FakeMt5. It reports `LiveRuntimeStatus`:

```39:42:D:\Prop\apps\api\Program.cs
        details = b.Connected
            ? $"live Manager groups={b.Groups} accounts={b.Accounts} phase={b.Phase}"
            : (b.LastError ?? "not connected")
```

Feature flags on `/api/settings` (L74–77):

- `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (**config-bound**, not a hardcoded `false`)
- `FEATURE_COPY_TRADING_ENABLED` = **`true` literal** (SHADOW pipeline on)

Manual resync walks **both** live broker codes and **every** login already in the store — not the four dummy numbers:

```114:147:D:\Prop\apps\api\Program.cs
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

Recon endpoint note (L68): `"recon runs only after FIX TRADE logon; NewOrderSingle still off"`.

`GET /api/trades` still `Take(200)` — a reconstructed-row **page cap**, not a Manager enumeration cap. Grep of `D:\Prop\src` for `Take(200)`: **0**. Sole product `Take(200)` is this API page (+ copy-intents list `Take(take)` default 200).

Copy endpoints exist and are **not** senders: `GET /api/copy/status`, `GET /api/copy/intents`.

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

`apps/fix-worker/Program.cs` is the same 18-line pattern. Neither worker `Program.cs` seeds deals, groups, or logins. Neither worker calls `EnvFile.FindAndLoad()` (only API + probe do).

`BrokerCatalogSeed` writes broker catalog rows + XAUUSD + kill switch + two FIX session rows (`Disconnected`, TRADE `LastError` = `"session up for logon/recon only; NewOrderSingle off"`). **No** `10001`/`10002`, **no** canned deals, **no** `LoggedOn` forge. Achiever catalog row records proxy `81.29.145.69:49527`; Starwave has no proxy fields.

### 1.3 LiveBrokerProbe — native only, no dummy seed

`D:\Prop\tools\LiveBrokerProbe\Program.cs` (86 lines) refuses to run if either password env is whitespace. It walks `CreateConnectorsFromEnvironment()` (Native ×2), calls `GetGroupsAsync` + `GetAccountsAsync(null)`, writes `LIVE_GROUPS_AND_TRADERS.json`. Note on the dump: `"Passwords never written. Groups and manager logins only."` Zero `DemoSeeder`/`FakeMt5`/`10001`/`10002`/`dummy` tokens.

### 1.4 DI refuse-dummy (runs before any Program.cs seed)

```36:59:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
        services.AddScoped<CopyTradingService>();
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
        // ...
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        services.AddHostedService<CopyTradingHostedService>();
```

`CreateConnectors` builds **only** two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). Gate: both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` must be non-empty and not a `<SECRET>` / `(a/c` placeholder (`LiveMt5Registration.IsSecret`, Ordinal). Password **values are not quoted here**.

`CreateDefault()` / `FakeMt5BrokerConnector` have **0** callers under `apps/` or `DependencyInjection.cs`.

`DATABASE_URL` placeholder → InMemory (`DependencyInjection` L27–29). Census is process-local unless Postgres is wired.

**Delta vs W500_RESEARCH_91 / 111:** those reports quoted DI as `RealCopyEnabled = false` with a “do not arm” comment. That pin is **gone**. Current L41 copies the env/config string. Combined with API `EnvFile.FindAndLoad()` + `D:\Prop\.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`, a live API process **will advertise `realCopyEnabled=true`**. That is **not** a `35=D`. `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” is stale.

`CTraderFixLogonHostedService` L68–70 **logs** `RealCopyArmed` and does **not** write `_runtime.RealCopyEnabled = false` (slot 91 snippet of that overwrite is stale).

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

Seeder FIX rows are `Disconnected` (D22 “forges LoggedOn” is stale).

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

If this worker is the **only** scorer, it will ingest the live catalog (via native `SyncBrokerAsync`) and then rebuild scores for four **non-existent** dummy logins — **0 / 8460** live traders scored. The API process does **not** have this four-login loop (`LiveIngestHostedService` + `/api/ops/resync`).

This is a **completeness** defect on the standalone worker, not a capital path.

---

## 3. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Native connector (production implementor)

Connect (`NativeMt5BrokerConnector.cs` L88–111): try pump `GROUPS|USERS|POSITIONS`; on fail retry `PUMP_MODE_NONE`. Fetch does **not** switch to Fake.

`Ensure()` (L436–439) throws if not connected — **no** Fake fallback.

`GetGroupsCore` (L144–186):

1. `GroupRequestArray("*", arr)` — Manager set A, mapping-blind.
2. If that list is empty: `GroupTotal()` + `GroupNext`.
3. Dedup by name (`HashSet` ordinal-ignore-case).

`GetAccountsCore(null)` (L189–214) walks **every** group name from `GetGroupsCore`, then `ReadAccountsForGroup`:

1. `UserRequestArray(gname, users)`
2. fallback `UserGetByGroup`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. `UserAccountRequestArray` / `UserAccountGetByGroup` for balances

No `Take(200)` on this walk.

### 3.2 YoPips (recipe only; not this product’s live enumerator)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

- `GetAllGroups` (`L962–982`) is **cache-only** `GroupTotal` + `GroupNext`. It does **not** call `GroupRequestArray`.
- Traders: `UserLogins` (`L315+`) is a true request API.

Do **not** treat YoPips `GetAllGroups` after pump-none as “ALL groups.” The C# live path is the ALL-groups collector.

### 3.3 Ingest + dashboard

`DealIngestionService.SyncCatalogAsync` upserts **whatever** `GetGroupsAsync` / `GetAccountsAsync(null)` return. `SyncBrokerAsync` then pulls deals by group (`IMt5BulkDealReader`) and positions via `GetGroupPositionsAsync("*")`.

`LiveIngestHostedService`: connect → catalog → (if connected) deals → **`ListLoginsWithDealsAsync`** → `RebuildTraderAsync` per that subset. On catalog failure it logs `"No dummy data will be substituted."` (L70).

| Surface | Login set scored / listed |
|---|---|
| `/api/ops/resync` | `ListLoginsAsync` = every `Mt5Accounts` row |
| `LiveIngestHostedService` | `ListLoginsWithDealsAsync` = distinct `Mt5Deals.Login` only |
| `EfDashboardQueries.GetTradersAsync` | **all** `Mt5Accounts` (unscored → `INSUFFICIENT_DATA`) |
| `EfDashboardQueries.GetGroupsAsync` | **all** `Mt5Groups` (no plan-name filter) |
| standalone mt5-worker | `{10001,10002,10003,99001}` only |

`ListLoginsAsync` (`EfTradingStore.cs` L339–341) = `_db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login)`.

`ListLoginsWithDealsAsync` (L343–345) = `_db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct()`.

Catalog fetch is still ALL groups + ALL traders. Auto-score is **not** ALL traders. Dashboard still lists every account. This is a scoring completeness gap, not a Manager drop, and **not** a capital path.

### 3.4 Measured live census (do not invent; this slot did not re-attach)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` utc **2026-08-18T08:42:16.8519545+00:00**. Passwords never written.  
Confirmed this slot: grep of that JSON for `"login": 10001` / `10002` / `10003` / `99001` = **0** (only unrelated balance amounts containing the digit string `10001`).

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

First live Achiever logins in the dump are **301106 / 301107** (`contest\yo-1step`), not 10001. First live Starwave login in the dump is **2081218** (`Starwave\cent\FX1\grp1`), not 99001. Fake vs live overlap is only the **name** `contest\yo-2step` / `demo\yo-2step` on Achiever.

Dashboard `/api/traders` = 8460, `/api/groups` = 18 (`CREDENTIALS_AND_COPY_STATUS.md`). These are **all groups this manager login can see**. If the server has more groups, they are outside this manager's permission set.

---

## 4. Copy to cTrader must not send live orders (no loss)

### 4.1 Outbound FIX from this process

`CTraderFixSession.BuildLogon` is the **only** wire builder. Tag 35 is **`"A"`** (Logon). Fields: 49/56/50/57/52/98/108/141/553/554. No tag 38 (`OrderQty`), no `35=D`. Socket is disposed after the logon reply.

`CTraderFixOptions.RealCopyExecutionEnabled` default **false**. Nothing binds env `REAL_COPY_EXECUTION_ENABLED` onto that POCO (would need `CTrader__RealCopyExecutionEnabled`). The **runtime** flag is a different object (`LiveRuntimeStatus`).

Product C# grep this pass:

| Pattern | Hits in `src` + `apps` `*.cs` |
|---|---|
| `35=D` / `(35, "D")` / `MsgType="D"` | **0** |
| `DealerSend` / `OrderAdd` / `TradeBalance` / `DealerBalance` | **0** |
| `new ExecutionIntent` / `ExecutionIntents.Add` | **0** |
| `NewOrderSingle` | name / log / `LastError` / `MayRetryNewOrderSingle` / `NewOrderSingleImplemented` const only |

fix-worker loop **overwrites** FIX rows to `Disconnected` + `"No live TRADE socket. NewOrderSingle remains off."` even if nested `CTrader:RealCopyExecutionEnabled` is true. It never opens a TRADE socket.

LiveBrokerProbe is MT5 read-only. It does not touch FIX.

### 4.2 Copy pipeline is SHADOW-only (even when REAL_COPY is armed)

`CopyTradingService`:

- `NewOrderSingleImplemented = false` (const)
- `VenueReconciled = false` (const)
- persisted `RiskDecisionRecord.AllowFixSend = false` always (L192)
- live-send branch (L198) requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — two of those are compile-time false, so the branch is dead
- else: `intent.Status = "SHADOW_ONLY"` + in-memory `ShadowCopyEngine.SimulateEntry` (no TCP)
- `CopyTradingHostedService` log: `"Copy pipeline created {Count} SHADOW intents. Live NewOrderSingle still blocked."`

`RiskEngine.Evaluate` **is** called (W500_RESEARCH_99 “0 product callers” is stale). OpenExposure + `Reconciled=false` rejects `VENUE_NOT_RECONCILED` before `AllowFixSend` can become true. Even an Approve cannot emit `35=D`.

`LiveRuntimeStatus.Snapshot()` when armed: `"REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."`

UI (`LiveCopyPage.tsx`): blockers list titled “Live send blockers (Pepperstone cannot be filled)”.

### 4.3 What “no loss” is **not**

Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Risk + recon + quantity conversion are **not** a wired send choke, because there is no sender. Do not tick Architecture §70 / A101 from this file. Do **not** add a NewOrderSingle in response to this research.

The process-level “forced false” pin claimed by W500_108 / CREDENTIALS is **no longer true**. Safety is the missing encoder.

Risk to **source** traders: none — this path does not `UserUpdate` / `DealAdd` / `PositionUpdate`.

---

## 5. Stale reports (do not inherit)

| Report | Stale claim | Current disk |
|---|---|---|
| `A002_api_dummy_path.md` | API `Program.cs` calls `DemoSeeder`; health says FakeMt5; resync hardcodes 4 logins | All three **gone** |
| `A005_dashboard_traders.md` | same health string; ingest `Take(200)` | Health is `LiveRuntimeStatus`; ingest `Take` = 0 |
| `C42_honesty_no_live_mt5.md` | sole connector is Fake; DI always `CreateDefault()` | DI registers Native only; live probe connected |
| `D22_seeder.md` | DemoSeeder forges `LoggedOn` | Current seeder TRADE/QUOTE are `Disconnected` (and seeder is not on host startup) |
| `W500_RESEARCH_91.md` / `111.md` | DI `RealCopyEnabled = false`; FIX host re-pins false; API 156 / probe 85 lines | DI binds env; no re-pin; API **160**; probe **86** |
| `W500_RESEARCH_99.md` | 0 `RiskEngine.Evaluate` callers | `CopyTradingService.GenerateShadowIntentsAsync` calls it |
| `CREDENTIALS_AND_COPY_STATUS.md` | `REAL_COPY` **false (forced)** | `.env` is `true`; DI copies it |
| `W500_RESEARCH_108.md` | `.env` L73 false; DI/hosted pin | `.env` L73 **true**; pins removed |

Superseding live-path note: `A014_live_path_now.md`. Sibling no-send notes: `A003_fix_noloss.md`, `W500_RESEARCH_110.md`. Same-search siblings: 11 / 71 / 91 / 111.

---

## 6. Residual (honest, not a license to send)

1. `DemoSeeder` + `FakeMt5BrokerConnector` remain in `src` for tests. Keep them **off** every host `Program.cs`.
2. `apps/mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` after a live `SyncBrokerAsync`. API `/api/ops/resync` is the fetch-all scorer; hosted ingest scores deal-bearing logins only.
3. `DATABASE_URL` placeholder → InMemory. Census is process-local unless Postgres is wired.
4. `GET /api/trades` `Take(200)` can hide older reconstructed rows from the explorer. Not a Manager drop.
5. `.env` `REAL_COPY_EXECUTION_ENABLED=true` is now **bound**. Dashboard will show REAL_COPY armed. That is an operator-wish / honesty residual, **not** a send path. Policy still says keep it false until §68 + §70.
6. Workers do not call `EnvFile.FindAndLoad()`; they will throw at DI if launched without real MT5 passwords in the process environment.
7. Live FIX logon (QUOTE/TRADE) is a **separate** measurement (credentials present per `CREDENTIALS_AND_COPY_STATUS.md`; this slot did not re-open sockets). Even if LoggedOn, send remains off.

---

## 7. Files read

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json` / `appsettings.Development.json` / `Properties\launchSettings.json`
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
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`ListLoginsAsync`, `ListLoginsWithDealsAsync`)
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Copy\CopyTradingModels.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + group names only)
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- Supporting: YoPips `src\core\mt5_manager.cpp` `GetAllGroups` / `UserLogins`; siblings `W500_RESEARCH_91.md`, `W500_RESEARCH_111.md`

---

## 8. Slot 131 close

```text
Program.cs (API 160 + workers 18/18 + probe 86): DemoSeeder/FakeMt5/10001/10002/dummy = 0 hits.
Dummy seed OFF. Live catalog = GroupRequestArray("*") + all UserRequestArray.
Measured 18 groups / 8460 traders. Dummy logins absent from live dump.
Hosted auto-score = logins-with-deals only; /api/ops/resync scores all accounts.
mt5-worker still loops 10001/10002/10003/99001 (completeness, not capital).
cTrader copy = logon-only; 35=D missing; NewOrderSingleImplemented=false.
.env REAL_COPY=true is now DI-bound (91/111 "forced false" is stale) — still no ticket.
Capital at risk from this process: NONE (SAFE_BY_ABSENCE).
```
