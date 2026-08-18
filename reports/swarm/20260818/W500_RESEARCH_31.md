# W500_RESEARCH_31 — Program.cs: DemoSeeder / FakeMt5 / 10001 / 10002 / dummy

| Field | Value |
|---|---|
| Slot | **31** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_31 |
| Assigned | Search product `Program.cs` for `DemoSeeder`, `FakeMt5`, `10001`, `10002`, `dummy`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Secret values printed | **None.** |
| Prior same-angle pins | `A002_api_dummy_path.md`, `W500_SLICE_31.md` — **both stale** on current disk (they still quote `DemoSeeder.SeedAsync` in API startup). |

## Verdict

**PASS_PROGRAM_CS_NO_DUMMY**

All four product `Program.cs` files have **zero** tokens for `DemoSeeder`, `FakeMt5`, `10001`, `10002`, `dummy`. API / worker startup seeds **catalog rows only** (`BrokerCatalogSeed.EnsureAsync`). Manual resync and live ingest walk **every** catalog group and **every** stored login — not the four Fake logins. Live `35=D` NewOrderSingle **does not exist**. `RealCopyEnabled` is forced **false**. Dummy FakeMt5 tape still exists on disk for **tests**, and `apps/mt5-worker/Worker.cs` (not `Program.cs`) still scores `{10001,10002,10003,99001}` — that leftover is **outside** this slot's assigned files.

`W500_SLICE_31` FAIL (“API `Program.cs` still calls `DemoSeeder`”) is **false on current sources**.

## Token census (measured this pass)

Grep of `DemoSeeder|FakeMt5|10001|10002|dummy` (case-sensitive) against every product `Program.cs`:

| File | Lines | `DemoSeeder` | `FakeMt5` | `10001` | `10002` | `dummy` |
|---|---:|---:|---:|---:|---:|---:|
| `D:\Prop\apps\api\Program.cs` | 156 | **0** | **0** | **0** | **0** | **0** |
| `D:\Prop\apps\mt5-worker\Program.cs` | 18 | **0** | **0** | **0** | **0** | **0** |
| `D:\Prop\apps\fix-worker\Program.cs` | 18 | **0** | **0** | **0** | **0** | **0** |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | 86 | **0** | **0** | **0** | **0** | **0** |

Hits that **do** exist are **not** product hosts:

| Location | What |
|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14, L134 | Class still on disk; scores `{10001,10002,10003,99001}` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` L6, L76–L119 | In-process 4-group / 4-login tape |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25, L31–L33 | Test-only `SeedAsync`; asserts 10001 SHADOW-not-LIVE, 10002 `RISK_BLOCKED` |
| `D:\Prop\reports\swarm\20260818\_tmp_*\Program.cs` | Eval harnesses, not the running API |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\...` | Unrelated: `MT_RET_REQUEST_INWAY=10001`, `MT_RET_REQUEST_ACCEPTED=10002`, plus C++ `FakeMt5Client` test doubles |

`DependencyInjection` is the only product mention of the word dummy, and it is a **refuse** string, not a seed:

```35:36:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

`CreateDefault()` / `FakeMt5BrokerConnector` under `D:\Prop\src\Infrastructure` = **1 hit**, inside `DemoSeeder.cs` L126. DI never registers Fake.

## 1. API `Program.cs` — dummy seed is gone

Startup is `EnsureCreated` + **catalog** seed. There is no `DemoSeeder.SeedAsync`.

```149:154:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`using TraderIntelligence.Infrastructure.Seeding;` exists only so `BrokerCatalogSeed` resolves. `BrokerCatalogSeed` writes two `Brokers` rows (Achiever / StarwaveFX), one `CanonicalInstrument` (XAUUSD), one `KillSwitch`, two `FixSessionState` rows (`Disconnected`). TRADE `LastError` is the no-send pin:

```105:105:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
                    LastError = "session up for logon/recon only; NewOrderSingle off",
```

No demo logins 10001–10003 / 99001. No canned deals. No `LoggedOn` forge.

### 1.1 Health is live-status, not FakeMt5 paint

A002 quoted `details = "demo FakeMt5BrokerConnector — not live Manager"` and `healthy = true`. **That object is gone.** Current `/api/health` reports `LiveRuntimeStatus`:

```32:56:D:\Prop\apps\api\Program.cs
app.MapGet("/api/health", (LiveRuntimeStatus runtime) =>
{
    var brokers = runtime.Brokers.Values.Select(b => new
    {
        name = b.BrokerCode,
        healthy = b.Connected,
        lastCheck = b.UpdatedAt,
        details = b.Connected
            ? $"live Manager groups={b.Groups} accounts={b.Accounts} phase={b.Phase}"
            : (b.LastError ?? "not connected")
    }).ToArray();
    return Results.Ok(new
    {
        mt5Connections = brokers,
        // ...
        realCopyEnabled = runtime.RealCopyEnabled,
        envFile = loadedEnv is null ? "missing" : "loaded"
    });
});
```

`/ready` counts `Brokers` / `Mt5Groups` / `Mt5Accounts` from the store — not a four-login constant.

### 1.2 `/api/ops/resync` walks ALL stored logins

A002 / W500_SLICE_31 claimed resync still hardcodes `{10001,10002,10003,99001}`. **Current loop is the live catalog:**

```121:143:D:\Prop\apps\api\Program.cs
    foreach (var code in new[] { "ACHIEVER", "STARWAVEFX" })
    {
        var status = runtime.Broker(code);
        status.Phase = "manual-resync";
        status.UpdatedAt = DateTimeOffset.UtcNow;
        var catalog = await ingestion.SyncCatalogAsync(code, ct);
        status.Groups = catalog.Groups;
        status.Accounts = catalog.Accounts;
        var deals = await ingestion.SyncBrokerAsync(code, from, to, ct);
        var brokerId = await store.ResolveBrokerIdAsync(code, ct);
        var logins = await store.ListLoginsAsync(brokerId, ct);
        var scored = 0;
        foreach (var login in logins)
        {
            await scoring.RebuildTraderAsync(code, login, ct);
            scored++;
        }
        // ...
        result[code] = new { catalog.Groups, catalog.Accounts, deals, scored, logins = logins.Count };
    }
```

`ListLoginsAsync` is unbounded over `mt5_accounts` for that broker:

```339:341:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

`/api/traders` is the same universe: `EfDashboardQueries.GetTradersAsync` iterates **every** `Mt5Accounts` row (filter optional; no `Take(200)` on the trader list). The only remaining `Take(200)` in API `Program.cs` is `/api/trades` (reconstructed-trade **page**, not the census).

## 2. Worker / probe `Program.cs` — same catalog, no dummy

`mt5-worker` and `fix-worker` `Program.cs` are 18-line hosts: `AddTraderIntelligence` + `EnsureCreated` + `BrokerCatalogSeed.EnsureAsync`. No seeder, no Fake, no login literals.

`LiveBrokerProbe\Program.cs` is the measured all-groups/all-traders tool. It refuses to start without both real passwords (length/presence only; values not written), then:

```19:29:D:\Prop\tools\LiveBrokerProbe\Program.cs
foreach (var connector in LiveMt5Registration.CreateConnectorsFromEnvironment())
{
    // ...
        await connector.ConnectAsync(CancellationToken.None);
        var groups = await connector.GetGroupsAsync(CancellationToken.None);
        var accounts = await connector.GetAccountsAsync(null, CancellationToken.None);
        var positions = connector is IMt5BulkPositionReader bulk
            ? await bulk.GetGroupPositionsAsync("*", CancellationToken.None)
            : Array.Empty<Mt5PositionDto>();
```

`GetAccountsAsync(null)` = every group the connector just listed. Output is `LIVE_GROUPS_AND_TRADERS.json`. Probe note: `"Passwords never written. Groups and manager logins only."`

## 3. What actually fetches ALL groups + ALL manager traders

API `Program.cs` does **not** enumerate Manager itself. It wires:

1. `EnvFile.FindAndLoad()` before `CreateBuilder` so `MT5_*` keys exist.
2. `AddTraderIntelligence` → fail-closed unless both real passwords → register **two** `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). **Not** `DemoBrokerFactory.CreateDefault()`.
3. Hosted `LiveIngestHostedService` (catalog → deals → score every `ListLoginsAsync` login).
4. Hosted `CTraderFixLogonHostedService` (35=A only).
5. `/api/ops/resync` as a manual second door over the same two broker codes.

Catalog ingest (what resync and live host both call):

```37:50:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);

        return new BrokerSyncResult(groups.Count, accounts.Count, 0, 0);
    }
```

Native enumerator (not Fake):

- `GroupRequestArray("*")`, fallback `GroupTotal` / `GroupNext`.
- Per group: `UserRequestArray` / `UserGetByGroup` / `UserLogins` + `UserRequestByLogins`.
- Bulk deals: `DealRequestByGroup` in 14-day windows (`IMt5BulkDealReader`).
- Bulk positions: `PositionRequestByGroup("*")`.

`FakeMt5BrokerConnector` implements **neither** bulk interface. It is a constructor list of 3 Achiever groups + 1 Starwave group and 4 logins. DI never constructs it.

Live ingest fail-closed (no dummy substitution):

```64:70:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                catch (Exception ex)
                {
                    st.Connected = false;
                    st.LastError = ex.GetType().Name + ": " + ex.Message;
                    st.Phase = "failed";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogError(ex, "{Broker} catalog failed. No dummy data will be substituted.", connector.BrokerCode);
                }
```

### 3.1 Measured live census (prior probe artifact, not re-run this slot)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` header:

```
probe = LiveBrokerProbe
utc   = 2026-08-18T08:42:16.8519545+00:00
envLoaded = true
```

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK via HTTP proxy (`elapsedMs` 7212.6) | 8 | 6512 | 1506 |
| STARWAVEFX | OK direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever group names in that JSON (manager-visible set; not the Fake `demo\Maxmaster` / `real\standard` tape):

`contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

This slot did **not** re-execute the probe (no password print, no extra live connect). Counts are quoted from the on-disk JSON + `LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md`. If the server added a group after 08:42Z, this file does not claim a newer count.

Honesty: “ALL groups” = **all groups these two manager logins can see**. Groups outside the manager ACL are not in the JSON.

## 4. Copy to cTrader — no live orders, no loss

API `Program.cs` advertises the no-send pin in three places:

| Surface | Measured |
|---|---|
| `/api/reconciliation/status` L68 | `note = "recon runs only after FIX TRADE logon; NewOrderSingle still off"` |
| `/api/settings` L75–L76 | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` = **false** literal |
| DI L38–L41 | `RealCopyEnabled = false` with comment: live NewOrderSingle is **not implemented** |

After FIX logon the hosted service **re-forces** the flag:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        // ...
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        // ...
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

`CTraderFixSession` builds **only** `35=A` Logon (tags 34/49/56/50/57/52/98/108/141/553/554). There is no `35=D` / `(35, "D")` / `MsgType="D"` builder in `D:\Prop\src\Fix.CTrader\Sessions`. Grep of product C# for a NewOrderSingle encoder remains **0**. `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false**. `LiveRuntimeStatus.Snapshot().copyNote` when the flag is off: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

Shadow fills (`ShadowCopyEngine`) are **in-process price math**. They do not open a TRADE socket and do not emit an order.

fix-worker `Worker.cs` (not `Program.cs`) still stamps TRADE `Disconnected` / `"NewOrderSingle remains off."` even if config says real copy is on.

**Risk to capital from this process: none.** Logon ≠ send. A logged-on TRADE session without a `35=D` encoder cannot take a live loss.

## 5. Residual dummy **outside** `Program.cs` (do not greenwash)

| Residual | Path | Why it still matters |
|---|---|---|
| `DemoSeeder` class | `src\Infrastructure\Seeding\DemoSeeder.cs` | Still builds Fake registry + scores 4 logins. Tests call it. **API process does not.** |
| `FakeMt5BrokerConnector` / `DemoBrokerFactory` | `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Closed list: Achiever groups `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step` + logins **10001 / 10002 / 10003**; Starwave `real\standard` + **99001**. 18 canned deals. `ConnectAsync` flips a bool. |
| mt5-worker score loop | `apps\mt5-worker\Worker.cs` L31 | **Still** `foreach (var login in new long[] { 10001, 10002, 10003, 99001 })`. If that worker is run against a live store it will **not** score the 8460 manager logins. API `LiveIngestHostedService` + `/api/ops/resync` **will**. |
| mt5-worker TFM | `TraderIntelligence.Mt5Worker.csproj` | Still `net8.0` (not `net8.0-windows` x64). API host **is** `net8.0-windows` + x64 and references `TraderIntelligence.Mt5`. |
| In-memory DB | `DependencyInjection` when `DATABASE_URL` is missing / contains `<SECRET>` | Restart drops the catalog. `CREDENTIALS_AND_COPY_STATUS.md` still records `DATABASE_URL` as placeholder. |
| `/api/trades` page | `apps\api\Program.cs` L107 | `Take(200)` on reconstructed trades only. Not a census cap. |
| Dest P&L tiles | `EfDashboardQueries` | Overview still emits dest/XAU P&L **0** literals (W500_SLICE_110). Not Fake logins. |

`W500_SLICE_31` (assigned `DemoSeeder.cs`, angle ALL-groups) remains a valid FAIL **for that file**: the seeder still cannot discover manager groups. It is **not** a valid FAIL for current API `Program.cs`.

## 6. Stale swarm claims this slot supersedes

| Prior artifact | Claim | Current disk |
|---|---|---|
| `A002_api_dummy_path.md` | API startup calls `DemoSeeder`; health says FakeMt5; resync hardcodes 4 logins; API TFM `net8.0` | Startup = `BrokerCatalogSeed`; health = `LiveRuntimeStatus`; resync = `ListLoginsAsync`; API TFM **`net8.0-windows` x64** |
| `W500_SLICE_31.md` L77 | Callers of `DemoSeeder.SeedAsync` include `apps\api\Program.cs`, both worker `Program.cs` | Product `Program.cs` callers of `DemoSeeder` = **0**. Remaining callers: integration test + `_tmp_*` harnesses |
| `A005` / `A010` / `A013` (partial) | `/api/ops/resync` loops `{10001…}` | Loop is gone from `Program.cs` |
| `C42` | Sole connector is Fake | DI registers `NativeMt5BrokerConnector` only |

## 7. Goal check

| Goal | Status | Evidence |
|---|---|---|
| Fetch ALL Achiever + Starwave groups | **CODED + previously measured** | Native `GroupRequestArray("*")` + `SyncCatalogAsync` + probe JSON 8 + 10 = 18 |
| Fetch ALL manager traders | **CODED + previously measured** | `GetAccountsAsync(null)` + `ListLoginsAsync` + probe JSON 6512 + 1948 = 8460. Dashboard `/api/traders` walks all `Mt5Accounts` |
| No dummy seed on API start | **PASS** | Product `Program.cs` token table all zeros; `BrokerCatalogSeed` only |
| Copy-to-cTrader without live loss | **PASS (safe-by-absence)** | No `35=D` builder; `RealCopyEnabled` forced false; recon note in `Program.cs` L68 |

Not claimed: “every deal for 8460 logins is already scored in the current API process,” “Postgres is durable,” or “mt5-worker scores the live book.” Those are other slots.

## Files read

- `D:\Prop\apps\api\Program.cs` (156/156)
- `D:\Prop\apps\mt5-worker\Program.cs` (18/18)
- `D:\Prop\apps\fix-worker\Program.cs` (18/18)
- `D:\Prop\tools\LiveBrokerProbe\Program.cs` (86/86)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + Achiever groups)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\W500_SLICE_31.md` (stale vs Program.cs)
