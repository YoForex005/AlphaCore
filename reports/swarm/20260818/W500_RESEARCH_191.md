# W500_RESEARCH_191 — Search `Program.cs` for `DemoSeeder` / `FakeMt5` / `10001` / `10002` / `dummy`

| Field | Value |
|---|---|
| Slot | **191** |
| Agent | W500_RESEARCH_191 |
| Date | 2026-08-18 |
| Topic | Search every product `Program.cs` for `DemoSeeder`, `FakeMt5`, `10001`, `10002`, `dummy`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords, no `.env` values. Group names and counts only. |
| Method | `grep` + `read_file` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of all five product `Program.cs` files, `DemoSeeder.cs` (140/140), `FakeMt5BrokerConnector.cs` (151/151), `LiveMt5Registration.cs`, `DependencyInjection.cs`, `NativeMt5BrokerConnector.cs` (458/458), `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `CopyTradingService.cs`, `CTraderFixSession.cs`, `CTraderFixDemoTestTrade.cs`, `RiskEngine.cs`, `BaselineScorer.cs` / `TraderStateMachine`, workers, live probe JSON. **This slot did not re-attach Manager or FIX and did not send orders.** |
| Sibling reports (same angle; re-read, not inherited) | `A002_api_dummy_path.md` (**stale** vs current `Program.cs`), `A014_live_path_now.md`, `D22_seeder.md` (FIX status **stale**), `D24_fake.md` (**stale** — Native now exists), `E002_no_live_send.md`, `W500_RESEARCH_100.md`, `W500_RESEARCH_109.md`. |

**Honesty rule:** a `Program.cs` with zero dummy tokens is **not** proof the dummy book is deleted. `DemoSeeder` + `FakeMt5BrokerConnector` still compile. `apps/mt5-worker/Worker.cs` (not `Program.cs`) still scores `{10001,10002,10003,99001}`. A live 18/8460 census does **not** mean those four fake logins exist on the servers. Absence of `35=D` in the copy pipeline is **SAFE_BY_ABSENCE**, not a unit-tested send gate.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| Product `Program.cs` still calls `DemoSeeder` / wires `FakeMt5` / hardcodes `10001`/`10002`/`dummy` | **No** | **PASS** — 0 hits in all 5 product `Program.cs` files |
| Dummy book still exists off-startup | **Yes** | `DemoSeeder` + `DemoBrokerFactory` + integration test only |
| DI can still start on FakeMt5 / dummy passwords | **No Fake substitution** | Throws unless `HasRealPasswords`; **gap:** literal `dummy` is accepted as a “real” secret |
| Live path fetches **ALL** Achiever + Starwave groups + manager traders | **Yes (API + probe)** | `GroupRequestArray("*")` + `UserRequestArray` per group; measured **18 / 8460** |
| Residual dummy scorer still in tree | **Yes** | `apps/mt5-worker/Worker.cs` L31 — **not** `Program.cs` |
| Copy-to-cTrader can place a **live** order from the product hosts | **No** | `NewOrderSingleImplemented = false`; `AllowFixSend` persisted **false**; `CanPromoteToLive = false`; FIX session emits **35=A only** |
| Risk to capital from fetch + this copy path | **None** | Read-only Manager request + in-process shadow. Isolated demo tool can emit `35=D` **only** to `demo-` host / `demo.` sender |

One-line:

```text
Product Program.cs is clean of DemoSeeder/FakeMt5/10001/10002/dummy.
Live ingest already lists ALL 18 groups / 8460 manager traders.
cTrader copy cannot emit live NewOrderSingle.
Leftover: DemoSeeder fixture + mt5-worker dummy scoring loop.
```

**Slot verdict:** `PARTIAL_PASS` — assigned `Program.cs` search is clean and the no-loss copy goal holds; dummy leftover is **off** `Program.cs` (seeder + worker loop).

---

## 1. Product `Program.cs` census (assigned search)

Grep of `DemoSeeder|FakeMt5|10001|10002|dummy` against product `Program.cs` only (exclude `reports/_tmp_*`, vendor SDK, YoPips tests):

| Path | Lines | `DemoSeeder` | `FakeMt5` | `10001` | `10002` | `dummy` | Startup seed |
|---|---:|---|---|---|---|---|---|
| `D:\Prop\apps\api\Program.cs` | 159 | **0** | **0** | **0** | **0** | **0** | `BrokerCatalogSeed.EnsureAsync` L155 |
| `D:\Prop\apps\mt5-worker\Program.cs` | 18 | **0** | **0** | **0** | **0** | **0** | `BrokerCatalogSeed.EnsureAsync` L15 |
| `D:\Prop\apps\fix-worker\Program.cs` | 18 | **0** | **0** | **0** | **0** | **0** | `BrokerCatalogSeed.EnsureAsync` L15 |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | 85 | **0** | **0** | **0** | **0** | **0** | none (read-only probe) |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | 38 | **0** | **0** | **0** | **0** | **0** | none (demo FIX tool) |

**5 / 5 product `Program.cs` files are clean.**

Hits that **look** like `Program.cs` but are **not product**:

| Path | Why it is not evidence |
|---|---|
| `reports\swarm\20260818\_tmp_d48_shadow\Program.cs` | Scratch harness; calls `DemoSeeder` + `FakeMt5BrokerConnector` 10001/10002 |
| `reports\swarm\20260818\_tmp_c23_empty\Program.cs` | Scratch; dummy logins + `DemoSeeder` |
| `reports\swarm\20260818\_tmp_d37_eval\Program.cs` | Scratch; `DemoSeeder` only |
| `reports\swarm\20260818\_tmp_e023_noshadow\Program.cs` | Scratch; 10002 RISK_BLOCKED claims |
| `reports\swarm\20260818\_tmp_r14_gate\Program.cs` / `_tmp_r74_gate\Program.cs` | Password-gate probes; case name `dummy_word` |

YoPips `Program.cs`: **no file**. That tree is C++ (`src/main.cpp`). `FakeMt5Client` there is a **unit-test double** (`tests/legacy_close_compatibility_test.cpp`, `tests/mt5_time_window_test.cpp`). SDK `10001`/`10002` are `MT_RET_REQUEST_INWAY` / `MT_RET_REQUEST_ACCEPTED`, not trader logins.

---

## 2. What the three hosts actually start

### 2.1 API — `D:\Prop\apps\api\Program.cs`

```152:158:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

`using TraderIntelligence.Infrastructure.Seeding;` exists **only** for `BrokerCatalogSeed`. There is **no** `DemoSeeder.SeedAsync`.

`GET /api/health` no longer advertises FakeMt5. It reports `LiveRuntimeStatus` (`live Manager groups=… accounts=… phase=…` or `LastError`) and `realCopyEnabled = runtime.RealCopyEnabled`.

`POST /api/ops/resync` walks **both** brokers and **every** catalog login — not the four dummy logins:

```124:146:D:\Prop\apps\api\Program.cs
    foreach (var code in new[] { "ACHIEVER", "STARWAVEFX" })
    {
        var status = runtime.Broker(code);
        status.Phase = "manual-resync";
        // SyncCatalogAsync → SyncBrokerAsync → ListLoginsAsync → RebuildTraderAsync per login
        result[code] = new { catalog.Groups, catalog.Accounts, deals, scored, logins = logins.Count };
    }
```

Display-only leftover (not a Manager filter): `GET /api/trades` still `.Take(200)` at L110.

API TFM is `net8.0-windows` + `PlatformTarget` x64 (`TraderIntelligence.Api.csproj` L18–19) — required for Manager64.

API loads env names via `EnvFile.FindAndLoad()` at L10. **Values not printed.**

### 2.2 MT5 worker — `Program.cs` clean; `Worker.cs` is the leftover

`apps/mt5-worker/Program.cs` is 18 lines: `AddTraderIntelligence` + `BrokerCatalogSeed`. Zero dummy tokens.

The **scoring** loop still lives in `Worker.cs` (different file, same process if this host is launched):

```29:35:D:\Prop\apps\mt5-worker\Worker.cs
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
```

Honesty split:

| Step | What it does on a live Manager |
|---|---|
| `SyncBrokerAsync` | **ALL** groups + **ALL** accounts + group deals (native bulk reader) |
| `RebuildTraderAsync` loop | **Only** the four Fake logins. Those logins are **absent** from `LIVE_GROUPS_AND_TRADERS.json` (grep `"login": 10001` / `10002` / `10003` / `99001` = **0**) |

If the standalone mt5-worker is started against live brokers, it **will fetch** the full book and then **score nothing useful**. The API hosted ingest (`LiveIngestHostedService`) is the path that scores `ListLoginsWithDealsAsync`.

Worker TFM is still `net8.0` (not `net8.0-windows`) — a **compile/RID** risk for Manager64 if this host is the one that loads the native DLL. API host is the intended live path.

### 2.3 FIX worker — catalog seed + refuse send

`apps/fix-worker/Program.cs` seeds `BrokerCatalogSeed` only. `Worker.cs` stamps both FIX rows `Disconnected` and `LastError = "No live TRADE socket. NewOrderSingle remains off."` even if `CTrader:RealCopyExecutionEnabled` is true (it only logs a warning).

### 2.4 Probe / demo tools

`LiveBrokerProbe/Program.cs` builds **native** connectors via `LiveMt5Registration.CreateConnectorsFromEnvironment()`, then `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. Note on disk: `"Passwords never written. Groups and manager logins only."`

`DemoFixTestTrade/Program.cs` is **not** the copy pipeline. It calls `CTraderFixDemoTestTrade.SendAsync` (see §6).

---

## 3. Dummy leftover (exists, not on `Program.cs` startup)

### 3.1 `DemoSeeder` — test/dev bootstrap only

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (140 lines). Guard: `if (await db.Brokers.AnyAsync(ct)) return;`

It still:

1. Writes Achiever (`57.128.141.65:443`, manager **2027**) + StarwaveFX (`84.201.6.142:443`, manager **9904**) catalog rows.
2. Writes FIX rows as **`Disconnected`** (not `LoggedOn` — D22 is **stale**). TRADE `LastError = "No live TRADE socket. NewOrderSingle off."`
3. **Ignores DI** and builds a second in-process pair: `DemoBrokerFactory.CreateDefault()`.
4. Ingests the Fake tape, then scores **only** `{10001, 10002, 10003, 99001}`.

Callers of `DemoSeeder.SeedAsync` in the product tree:

| Caller | Role |
|---|---|
| `tests/Integration/SeedingAndStoreTests.cs` L25 | Fixture: 10001 has 3 XAU, **not LIVE**; 10002 is `RISK_BLOCKED` |
| Product `Program.cs` | **0** |

FIX identifiers in the seeder still paint **live** Pepperstone host / `live.pepperstone.1369850` even though status is `Disconnected`. `BrokerCatalogSeed` (the real startup path) uses **demo** host `demo-us-eqx-01.p.c-trader.com` / `demo.pepperstone.5328266`. Do not treat seeder host strings as a session.

### 3.2 `FakeMt5BrokerConnector` / `DemoBrokerFactory`

`D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`. `ConnectAsync` flips `_connected = true`. No socket, no Manager DLL, no password.

Default Fake census (not live):

| Broker | Groups | Logins | Closed XAU round-trips |
|---|---:|---|---|
| ACHIEVER | 3 (`demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`) | **10001**, **10002**, 10003 | 10001 ×3; 10002 ×3 (0.10 → 0.20 → 0.40 lots, all losers) |
| STARWAVEFX | 1 (`real\standard`) | 99001 | 3 small winners |

10002 is a **martingale tape**. `TraderStateMachine.FromBaseline` returns `RISK_BLOCKED` when `Martingale && MaxDrawdown > 0 && NetPnl < 0`. Integration test asserts that. `CanPromoteToLive` is **hard `false`** — 10001 cannot become LIVE even with quality ≥ 70.

`IMt5BrokerConnector` implementors in product C#:

| Type | Registered in DI? |
|---|---|
| `NativeMt5BrokerConnector` | **Yes** — `LiveMt5Registration.CreateConnectors` |
| `FakeMt5BrokerConnector` | **No** — only `DemoSeeder` / tests |

A002 / D24 claims that Fake is the only production connector are **stale**.

### 3.3 Dummy password word is **not** rejected

```52:55:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

Rejects empty / `<SECRET>` / `(a/c`. Does **not** reject the literal `dummy`.

`DependencyInjection.AddTraderIntelligence` L36–37:

```text
if (!LiveMt5Registration.HasRealPasswords(configuration))
    throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

Gate-probe `_tmp_r74_gate\Program.cs` L36 encodes the measured contract:

```text
Case("dummy_word", true, Cfg(("MT5_PASSWORD", "dummy"), ("MT5_STARWAVEFX_PASSWORD", "changeme")));
```

So `dummy` **opens** DI (expected `true`). That does **not** load FakeMt5 — `CreateConnectors` still builds two `NativeMt5BrokerConnector`s. A dummy password would fail **Connect**, and `LiveIngestHostedService` L70 logs `"No dummy data will be substituted."`

`CreateConnectors` itself does **not** call `HasRealPasswords` (unguarded empty cfg still constructs two Native objects). The throw is only in `AddTraderIntelligence`.

---

## 4. ALL groups + ALL manager traders (measured)

### 4.1 Code path (native, no plan filter)

`DealIngestionService.SyncCatalogAsync`:

1. `connector.GetGroupsAsync`
2. `connector.GetAccountsAsync(null)` — `null` = **every** group (`NativeMt5BrokerConnector.GetAccountsCore` L189–213)

Native fetch order:

1. `GroupRequestArray("*")` (L155). Fallback `GroupTotal` / `GroupNext` only if that list is empty.
2. Per group: `UserRequestArray(gname)` (L223); fallback `UserGetByGroup`; if still empty `UserLogins` + `UserRequestByLogins`.
3. Dedup by login.
4. Deals: `IMt5BulkDealReader.GetGroupDealsAsync` **per group** (native implements this; Fake does not).
5. Positions: `GetGroupPositionsAsync("*")`.

`EnabledForAnalysis` defaults **true** and is **not** a fetch filter. Plan-mapping labels do not shrink the census.

`LiveIngestHostedService` runs that catalog + deals for **every** `registry.All()` connector (Achiever + StarwaveFX). Scoring uses `ListLoginsWithDealsAsync` (deal-bearing logins), not the dummy four.

### 4.2 Live census (on disk; this slot did not re-attach)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` at **2026-08-18T08:42:16.8519545+00:00**, `envLoaded = true`.

| Broker | Connected | Groups | Accounts | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | true (7213 ms) | **8** | **6512** | 1506 |
| STARWAVEFX | true (6413 ms) | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever (2+179+4+5+4+6295+0+23 = **6512**):

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |

Starwave (11+4+170+1735+22+0+0+4+0+2 = **1948**):

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |

Empty groups **are** listed. These are all groups **this manager login can see**. Dummy logins **10001 / 10002 / 10003 / 99001 are not in the JSON**. Fake `demo\Maxmaster` is **not** a live Achiever group (live has `demo\yo-*` / `contest\yo-*` only).

Do not dump the 8460 logins. Dashboard `GetTradersAsync` iterates all `Mt5Accounts`; unscored rows paint `INSUFFICIENT_DATA`. Fetching them does not promote anyone to LIVE send.

### 4.3 YoPips C++ (related Manager, not this copy path)

`tests/mt5_group_probe.cpp` enumerates `GetAllGroups()` and prints names + counts. Credentials never echoed. That probe is **local-manager only** (`MT5_MODE=remote` returns failure). YoPips `src` has **0** hits for `cTrader` / `NewOrderSingle` / `35=D`. YoPips **does** implement MT5 `SendTrade` for the **challenge** backend — that is **not** wired into `D:\Prop` DI and is not cTrader FIX. Do not treat YoPips group totals as this product’s census.

---

## 5. Scoring vs fetch (do not greenwash)

| Path | Groups fetched | Logins scored |
|---|---|---|
| `LiveIngestHostedService` (API process) | ALL | `ListLoginsWithDealsAsync` — deal-bearing only |
| `POST /api/ops/resync` (`Program.cs`) | ALL | `ListLoginsAsync` — **all catalog logins** |
| `apps/mt5-worker/Worker.cs` | ALL (sync) | **hard-coded dummy four** |
| `DemoSeeder` | Fake 4 groups | dummy four |

`TraderStateMachine.CanPromoteToLive` = **false**. Highest auto state is `SHADOW`. `LIVE` / `LIVE_CANDIDATE` are enum values only; nothing in this path writes them.

---

## 6. Copy to cTrader — no live orders (no loss)

### 6.1 Product copy pipeline

`CopyTradingService`:

| Constant / write | Value |
|---|---|
| `NewOrderSingleImplemented` | **`false`** (L16) |
| `VenueReconciled` | **`false`** (L15) |
| `RiskDecisionRecord.AllowFixSend` | **hardcoded `false`** (L192) even if `RiskEngine` would allow |
| On the dead branch `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` | status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — still no socket write |
| Else | status `SHADOW_ONLY` + in-process `ShadowCopyEngine.SimulateEntry` |

`CopyTradingHostedService` L30: `"Copy pipeline created {Count} SHADOW intents. Live NewOrderSingle still blocked."`

`GetStatusAsync` summary when blockers exist: `"Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle."`

Blockers always include `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` and `"Venue not reconciled"`. Also: 0 LIVE traders; FIX not logged on unless the logon host succeeds; `REAL_COPY_EXECUTION_ENABLED` must be the string `true` to arm the **flag** (it still cannot send).

`LiveRuntimeStatus.Snapshot` copyNote if the flag is armed:

```text
REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.
```

`CTraderFixLogonHostedService` sends **Logon only** (`CTraderFixSession.BuildLogon` tag 35=`A`). Log line: `"NewOrderSingle still unimplemented."` Missing/placeholder `CTRADER_FIX_PASSWORD` skips logon entirely.

`CTraderFixOptions.RealCopyExecutionEnabled` default **`false`**.

`PersistDemoShadowAsync` writes `SHADOW_ONLY` intents + simulated fills **only** when state == `SHADOW`. RISK_BLOCKED (10002) writes **no** shadow.

### 6.2 Where `35=D` **does** exist (not the copy host)

Product C# `Build("D"` / `35=D` hits: **only** `CTraderFixDemoTestTrade.cs` L139 / L163 / L197.

That type is invoked solely by `tools/DemoFixTestTrade/Program.cs`. Gate at L43–59 **refuses** unless:

- host starts with `demo-`
- sender starts with `demo.`
- host/sender do not contain `live-` / `live.`
- account is **not** `1369850` (live Pepperstone id)

A refused call returns `OrderSent = false`. This is a **manual demo probe**, not copy-trading.

`apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` = **false** (display; not the send path). `/api/settings` exposes `runtime.RealCopyEnabled` and hardcodes `FEATURE_COPY_TRADING_ENABLED = true` (shadow pipeline on).

### 6.3 Risk engine cannot reach a socket

`RiskEngine.Evaluate` may set `AllowFixSend = true` only if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Copy service **overwrites** the persisted bit to `false` and never constructs a FIX `D`. Even an Approve is a shadow row.

---

## 7. Stale reports (do not inherit)

| Report | Claim then | Measured now |
|---|---|---|
| `A002_api_dummy_path.md` | API `Program.cs` calls `DemoSeeder`; health says FakeMt5; resync scores 10001–99001 | **False.** Catalog seed; live health; resync = all logins |
| `D22_seeder.md` | DemoSeeder writes TRADE `LoggedOn` | **False.** Current seeder writes `Disconnected` |
| `D24_fake.md` | Fake is the only `IMt5BrokerConnector` | **False.** Native is the DI connector |
| `A014` DI quote | `RealCopyEnabled = false` forced | **Changed.** Now reads `REAL_COPY_EXECUTION_ENABLED == "true"` (still cannot send) |
| `E011_creds_block.md` | hosts do not load `.env`; no usable passwords | **Superseded for load.** API `EnvFile.FindAndLoad()` exists; probe `envLoaded=true` and both brokers connected. **This slot did not re-classify `.env` values.** |

---

## 8. Goal vs leftover

```text
Goal:  ALL Achiever+Starwave groups + ALL manager traders
       copy → cTrader must not send live orders (no loss)

API Program.cs:     CLEAN (no DemoSeeder / FakeMt5 / 10001 / 10002 / dummy)
API live ingest:    FETCHES ALL (18 groups / 8460 traders measured)
API copy path:      SHADOW ONLY; no 35=D
mt5-worker Program: CLEAN
mt5-worker Worker:  leftover dummy SCORE list (does not shrink fetch)
DemoSeeder/Fake:    leftover fixture; tests only
Demo FIX tool:      35=D on demo host only; live refused
```

**Risk to capital:** **none** from product fetch + copy. Manager APIs used here are request/read (`GroupRequestArray`, `UserRequestArray`, `DealRequestByGroup`, `PositionRequestByGroup`). No `DealerSend` / `OrderAdd` / `NewOrderSingle` on the live C# connector. Isolated demo tool cannot target the live Pepperstone account by construction.

---

## 9. Files read (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\tools\DemoFixTestTrade\Program.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (ListLogins* + PersistDemoShadow)
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\BrokerRegistry.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + group names only)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`

*End of W500_RESEARCH_191. Product source was not modified. No secrets printed. This slot did not live-attach and did not send orders.*
