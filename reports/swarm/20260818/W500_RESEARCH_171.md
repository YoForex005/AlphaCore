# W500_RESEARCH_171 — Program.cs vs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Field | Value |
|---|---|
| Slot | **171** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_171 |
| Topic | Search every product `Program.cs` for `DemoSeeder`, `FakeMt5`, logins `10001`/`10002`, dummy seed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password values not read, not quoted. Boolean flag name `REAL_COPY_EXECUTION_ENABLED` only. |
| Live attach this slot | **No.** Census re-summed from prior `LiveBrokerProbe` dump only. No Manager socket. No FIX `35=D`. |
| Method | Full `read_file` of API / mt5-worker / fix-worker / LiveBrokerProbe / DemoFixTestTrade `Program.cs`, `DemoSeeder`, `BrokerCatalogSeed`, `FakeMt5BrokerConnector`, `NativeMt5BrokerConnector`, DI, `LiveMt5Registration`, `LiveIngestHostedService`, `CopyTradingHostedService`, `CopyTradingService`, `DealIngestionService`, `EfTradingStore`, `EfDashboardQueries`, `CTraderFixSession`, `CTraderFixDemoTestTrade`, FIX hosted service, both worker loops, `EnvFile`, `RiskEngine`, `CTraderFixOptions`. Targeted `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Dummy-login scan of `LIVE_GROUPS_AND_TRADERS.json`. |
| Siblings (same search, earlier slots) | `W500_RESEARCH_11.md`, `W500_RESEARCH_71.md`, `W500_RESEARCH_91.md`, `W500_RESEARCH_111.md`, `W500_RESEARCH_131.md`, `W500_RESEARCH_151.md` — **still correct on host `Program.cs` = 0 dummy tokens.** This slot re-reads disk independently. **91 / 111 / CREDENTIALS “`RealCopyEnabled` forced false” are stale.** |

**Honesty rule:** older swarm notes (A002, A005, A010, C42, D22) that said “API still calls `DemoSeeder` / health still says FakeMt5 / DI always `CreateDefault()`” are **stale vs current disk**. A comment or `LastError` that names `NewOrderSingle` is not a `35=D` builder. `DemoSeeder` existing in the tree is not the same as a host calling it. `.env` `REAL_COPY_EXECUTION_ENABLED=true` is an **operator arm**, not a ticket. This slot did not open Manager or FIX sockets.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| Any product `Program.cs` still calls `DemoSeeder` | **No** | **ABSENT** on API + both workers + LiveBrokerProbe |
| Any product `Program.cs` names `FakeMt5` / `10001` / `10002` / `dummy` | **No** (`0` hits, four host files) | **ABSENT** |
| Dummy FakeMt5 seed on API startup | **OFF** | `BrokerCatalogSeed.EnsureAsync` only |
| DI can register FakeMt5 when host starts | **No** | fail-closed: real passwords required; connectors are `NativeMt5BrokerConnector` ×2 only |
| Fetch ALL manager-visible groups | **Implemented** on live path | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback |
| Fetch ALL manager traders | **Implemented** on catalog path | `UserRequestArray` per group + `UserLogins` fallback; `GetAccountsAsync(null)` |
| Dashboard paints all catalog logins | **Yes** | `GetTradersAsync` walks **every** `Mt5Accounts` row (left-join scores) |
| Measured live census (prior probe JSON; not re-attached) | Achiever **8 / 6512**; Starwave **10 / 1948**; total **18 / 8460** | `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00` |
| Dummy logins `10001`/`10002`/`10003`/`99001` in that live dump | **0 hits** | Fake-only; not live Manager users |
| Copy pipeline exists on host | **Yes (SHADOW)** | `CopyTradingHostedService` + `/api/copy/status` + `/api/copy/intents` |
| `FEATURE_COPY_TRADING_ENABLED` | **true** literal in `/api/settings` L77 | shadow pipeline ON |
| `RealCopyEnabled` process pin | **Env-bound, not hardcoded false** | DI L41 binds `REAL_COPY_EXECUTION_ENABLED`. Lab `.env` L73 is `true`. FIX logon **does not** re-pin false. |
| Copy to cTrader can send a live order from API/workers | **No** | **`SAFE_BY_ABSENCE`** — product `src`+`apps` `35=D` = **0**; `NewOrderSingleImplemented=false`; `VenueReconciled=false`; persist `AllowFixSend=false`; **0** `ExecutionIntent` writers |
| Residual dummy scoring set | **Yes** | `apps/mt5-worker/Worker.cs` L31 still rebuilds only `{10001,10002,10003,99001}` |
| Residual dummy class in tree | **Yes** | `DemoSeeder` + `DemoBrokerFactory` still exist; product callers = **tests only** |
| Residual demo FIX `35=D` builder | **Yes, off host** | `CTraderFixDemoTestTrade.Build("D")` — demo-host gated; only `tools/DemoFixTestTrade` calls it |
| Auto-score every catalog login | **Split** | `/api/ops/resync` = `ListLoginsAsync` (all). `LiveIngestHostedService` = `ListLoginsWithDealsAsync` (deals-only). |

**One-line:** Host `Program.cs` files have **zero** `DemoSeeder` / `FakeMt5` / `10001` / `10002` / `dummy` tokens; API startup seeds catalog rows only; live Manager walk can enumerate all groups/traders; live `35=D` is unbuildable on the hosted copy path so this process cannot take a cTrader loss even though the copy **feature** is on and the lab env arm is `true`.

Slot verdict: **`PASS_HOST_NO_DUMMY`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — no NewOrderSingle encoder on API/workers; persist-before-send hop writes `SHADOW_ONLY` / `LIVE_SEND_BLOCKED_UNIMPLEMENTED`; no `ExecutionIntent` rows.

---

## 1. Every product `Program.cs` (assigned search)

Grep of `D:\Prop\apps\**\Program.cs` and `D:\Prop\tools\LiveBrokerProbe\Program.cs` for `DemoSeeder|FakeMt5|10001|10002|dummy` (this slot, independently):

| Host | Path | Lines | Hits | Startup seed |
|---|---|---:|---:|---|
| API | `D:\Prop\apps\api\Program.cs` | **160** | **0** | `BrokerCatalogSeed.EnsureAsync` (L156) |
| MT5 worker | `D:\Prop\apps\mt5-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| FIX worker | `D:\Prop\apps\fix-worker\Program.cs` | **18** | **0** | `BrokerCatalogSeed.EnsureAsync` (L15) |
| Live probe | `D:\Prop\tools\LiveBrokerProbe\Program.cs` | **86** | **0** | none; `LiveMt5Registration.CreateConnectorsFromEnvironment()` |
| Demo FIX tool | `D:\Prop\tools\DemoFixTestTrade\Program.cs` | **38** | **0** of assigned tokens | none (not a host) |

`DemoSeeder` token under `D:\Prop\apps`: **0**.

Product `Program.cs` hits for the assigned tokens = **0 / 4 host files**. The only `Program.cs` files in the tree that still name `DemoSeeder` / `10001` / `10002` live under `D:\Prop\reports\swarm\20260818\_tmp_*` (eval junk, not hosts). Integration tests call `DemoSeeder` from `SeedingAndStoreTests.cs`, not from a host `Program.cs`.

YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `DemoSeeder` and **no** `FakeMt5BrokerConnector`. Its `10001`/`10002` hits are official Manager retcodes (`MT_RET_REQUEST_INWAY=10001` / `MT_RET_REQUEST_ACCEPTED=10002`), test `FakeMt5Client` fixtures, and `TERMINAL_ISSUES.md` warm-session notes — not this product's dummy book.

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

`GET /api/trades` still `Take(200)` — a reconstructed-row **page cap**, not a Manager enumeration cap.

Copy endpoints exist and are **not** senders: `GET /api/copy/status`, `GET /api/copy/intents`.

### 1.2 Worker hosts — same catalog seed, no dummy tokens

`D:\Prop\apps\mt5-worker\Program.cs` (18 lines) and `D:\Prop\apps\fix-worker\Program.cs` (18 lines) both:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

**Residual (not `Program.cs`):** `D:\Prop\apps\mt5-worker\Worker.cs` L31 still scores the four Fake logins after a **live** `SyncBrokerAsync` of both brokers:

```29:35:D:\Prop\apps\mt5-worker\Worker.cs
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
```

That loop cannot invent those logins on a live Manager book (they are absent from the 08:42Z dump). It can write `INSUFFICIENT_DATA` score rows for missing logins if the worker is running. Hosted API ingest does **not** use this set.

FIX worker loop stamps `Disconnected` + `"NewOrderSingle remains off."` and never builds a FIX body (`D:\Prop\apps\fix-worker\Worker.cs` L21–46). Its `CTrader:RealCopyExecutionEnabled` read is **log-only**.

### 1.3 LiveBrokerProbe — native only, writes census JSON

`D:\Prop\tools\LiveBrokerProbe\Program.cs` (86 lines): loads `.env`, refuses if either password is blank, constructs `LiveMt5Registration.CreateConnectorsFromEnvironment()` (Native ×2), then `GetGroupsAsync` + `GetAccountsAsync(null)` + bulk positions `"*"`. Writes `LIVE_GROUPS_AND_TRADERS.json`. Zero dummy tokens. This slot did **not** re-run the probe.

---

## 2. What still exists in the tree (not on the host path)

### 2.1 `DemoSeeder` — tests only

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (140 lines) still:

- early-returns if any `Brokers` row exists (L22–23);
- writes Achiever/Starwave catalog + XAUUSD + kill switch;
- writes FIX rows as **`Disconnected`** with `LastError` “NewOrderSingle off.” (L90–101) — **not** the old forged `LoggedOn` (D22/E008: current file is Disconnected);
- then `DemoBrokerFactory.CreateDefault()` + scores `{10001,10002,10003,99001}` (L126–138).

Product callers of `DemoSeeder.SeedAsync`:

| Path | Role |
|---|---|
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | InMemory fixture |
| `D:\Prop\reports\swarm\20260818\_tmp_*\Program.cs` | eval junk |

Zero callers under `D:\Prop\apps` and zero under `D:\Prop\src` except the class itself.

### 2.2 `FakeMt5BrokerConnector` / `DemoBrokerFactory`

`D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`:

- `ConnectAsync` flips `_connected = true` (L30–34). No socket, no DLL, no password.
- `CreateDefault()` hardcodes Achiever groups `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step` and logins **10001 / 10002 / 10003**; Starwave `real\standard` + **99001**; 18 canned XAUUSD deals.

DI never registers this type. `AddTraderIntelligence` throws unless both live passwords pass `IsSecret`, then registers only Native connectors.

### 2.3 `BrokerCatalogSeed` — the actual startup writer

`D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`:

- two broker rows if missing (Achiever `57.128.141.65:443` login **2027**, Starwave `84.201.6.142:443` login **9904**);
- one `CanonicalInstrument` XAUUSD;
- one `KillSwitch` `None`;
- two `FixSessionState` rows **Disconnected**, demo host `demo-us-eqx-01.p.c-trader.com`, TRADE `LastError` = `"session up for logon/recon only; NewOrderSingle off"`;
- **no** logins 10001–10003 / 99001, **no** canned deals, **no** `LoggedOn` forge.

---

## 3. DI fail-closed — Native only

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
        // ...
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        services.AddHostedService<CopyTradingHostedService>();
```

`CreateConnectors` builds **only** `NativeMt5BrokerConnector` for Achiever (proxy optional from env) and Starwave (`ProxyEnabled = false` hard pin). No `FakeMt5BrokerConnector`. No `DemoBrokerFactory`.

`HasRealPasswords` requires both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` to be non-blank, not contain exact `<SECRET>`, and not contain `(a/c`. Residual (not this slot’s fix): `IsSecret` is ordinal / does not reject the word `dummy` as a password.

`.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. After `EnvFile.FindAndLoad()` that value is **bound** onto `LiveRuntimeStatus.RealCopyEnabled`. That is an **arm**, not a sender. CREDENTIALS_AND_COPY_STATUS “forced false” and slots 91/111 “DI hardcodes false” are **stale**.

---

## 4. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 4.1 Native walk (implemented)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`:

| Step | API | Lines |
|---|---|---|
| Groups primary | `GroupRequestArray("*")` | L152–167 |
| Groups fallback | `GroupTotal` + `GroupNext` | L169–183 |
| Accounts when `group == null` | every group name from `GetGroupsCore` | L189–213 |
| Users per group | `UserRequestArray` then `UserGetByGroup` then `UserLogins`+`UserRequestByLogins` | L223–233 |
| Accounts overlay | `UserAccountRequestArray` / `UserAccountGetByGroup` | L235–237 |
| Deals (live ingest) | `DealRequestByGroup` via `IMt5BulkDealReader` | L296–316 |
| Positions | `PositionRequestByGroup("*")` via `IMt5BulkPositionReader` | L335–352 |

`DealIngestionService.SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L38–51):

1. `GetGroupsAsync` → `UpsertGroupsBatchAsync` (all groups);
2. `GetAccountsAsync(null, ct)` → `UpsertAccountsBatchAsync` (all manager users).

No `Take(200)` on the ingest path. Sole product `Take(200)` is `GET /api/trades` reconstructed-row paging.

### 4.2 Who gets scored (split, not a hide)

| Surface | Login set | Meaning |
|---|---|---|
| `LiveIngestHostedService` L106 | `store.ListLoginsWithDealsAsync` | distinct logins that have **deals** |
| `/api/ops/resync` L134 | `store.ListLoginsAsync` | **all** persisted `Mt5Accounts` |
| `mt5-worker/Worker.cs` L31 | `{10001,10002,10003,99001}` | **residual dummy scorer** |
| `GetTradersAsync` L99 | `foreach (var account in accounts)` | **all catalog logins**, score left-join; missing score → `INSUFFICIENT_DATA` |

`ListLoginsAsync` = `Mt5Accounts` for broker. `ListLoginsWithDealsAsync` = distinct `Mt5Deals.Login`. Unscored catalog rows still appear on `/api/traders`. That is a **score-freshness** split, not a census drop.

### 4.3 Measured census (re-summed, not re-probed)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

- `utc`: `2026-08-18T08:42:16.8519545+00:00`
- `probe`: `LiveBrokerProbe`
- `note`: `"Passwords never written. Groups and manager logins only."`

| Broker | Connected | Groups | Accounts | Open positions | Elapsed |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | **8** | **6512** | 1506 | 7212.6 ms |
| STARWAVEFX | true | **10** | **1948** | 478 | 6413.5 ms |
| **Total** | | **18** | **8460** | **1984** | |

Achiever group counts (this slot re-added):

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

Starwave group counts (this slot re-added):

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

Grep of that JSON for `"login": 10001`, `"login": 10002`, `"login": 10003`, `"login": 99001`: **0 / 0 / 0 / 0**. Dummy FakeMt5 logins are **not** live Manager users.

First live Achiever logins in the dump are `301106` / `301107` on `contest\yo-1step` — not the demo book.

This slot did **not** re-attach. Counts are the 08:42Z snapshot.

---

## 5. Copy to cTrader must not send live orders (no loss)

### 5.1 Hosted FIX session — Logon only, then dispose

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines):

- sole outbound MsgType is `(35, "A")` in `BuildLogon` L96;
- one `ssl.WriteAsync` of that Logon (L49);
- `using` TcpClient + SslStream → sockets **disposed** after the Logon reply;
- **0** `NewOrderSingle` tokens, **0** `35=D`, **0** tag 38.

`CTraderFixLogonHostedService` calls `TryLogonAsync` for QUOTE 5211 and TRADE 5212, copies LoggedOn into `LiveRuntimeStatus`, persists existing FIX rows. It **does not** overwrite `RealCopyEnabled`. Log line L69: `"NewOrderSingle still unimplemented"`.

### 5.2 Copy hop — SHADOW only, persist `AllowFixSend=false`

`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`:

```15:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = 0.05m;
```

- `GenerateShadowIntentsAsync` writes `CopyIntent` + `RiskDecisionRecord` with **`AllowFixSend = false`** (L192) **regardless** of `RiskEngine.Evaluate`.
- Live-send branch L198 requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. The last two consts are **false**, so the branch can only stamp `LIVE_SEND_BLOCKED_UNIMPLEMENTED` if the first two ever become true — and even then there is **no encoder**.
- Else status = `SHADOW_ONLY` + optional in-process `ShadowOrder`.
- Grep of `D:\Prop` `*.cs` for `new ExecutionIntent` / `ExecutionIntents.Add`: **0**.

`CopyTradingHostedService` L30: `"Live NewOrderSingle still blocked."`

`LiveRuntimeStatus.Snapshot` copyNote when armed: `"REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."`

### 5.3 Product `35=D` inventory

| Surface | `35=D` / `Build("D")` | On hosted copy path? |
|---|---|---|
| `src` + `apps` product hosts | **0** | n/a |
| `CTraderFixSession` | 0 (only `"A"`) | host Logon |
| `CTraderFixDemoTestTrade` L124, L155 | **yes** `Build("D", …)` | **No** — only `tools/DemoFixTestTrade` |
| Demo tool gate | refuses unless host starts `demo-` **and** SenderCompId starts `demo.` **and** not `live.` / `live-` **and** account ≠ `1369850` | demo-only if someone runs the tool |

Running the hosted API/workers cannot reach `CTraderFixDemoTestTrade`. Residual risk is **operator running the standalone tool against a demo gateway**, not the copy pipeline taking a live Pepperstone loss.

### 5.4 Flag vs sender

| Layer | REAL_COPY state | Can send `35=D`? |
|---|---|---|
| Architecture / POCO `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** | no binder onto POCO |
| Lab `.env` L73 | **`true`** | arm only |
| DI `LiveRuntimeStatus.RealCopyEnabled` | **bound** from env | no encoder |
| FIX hosted service | does **not** re-pin false | no encoder |
| FIX worker `CTrader:RealCopyExecutionEnabled` | default false, log-only | stamps Disconnected |
| Copy consts | NOS=false, Venue=false | persist AllowFixSend=false |

`RiskEngine.Evaluate` *can* set `AllowFixSend=true` if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. The copy hop **throws that bit away** and writes `false`. Combined with `VenueReconciled=false` const, Evaluate cannot license a wire send.

---

## 6. Stale claims this slot does **not** inherit

| Older claim | Current disk |
|---|---|
| A002 / A005: API startup calls `DemoSeeder`; health says FakeMt5; resync scores 10001… | `BrokerCatalogSeed`; live Manager health string; resync = `ListLoginsAsync` |
| C42: sole connector is Fake; `CreateDefault()` always | DI Native ×2; Fake unused at host start |
| D22: seeder forges `LoggedOn` | current `DemoSeeder` writes `Disconnected` (tests only) |
| W500_91 / 111 / CREDENTIALS: `RealCopyEnabled` forced false | DI binds env; `.env` is `true` |
| W500_15: hosted service scores all `ListLoginsAsync` | hosted scores `ListLoginsWithDealsAsync` |
| W500_151 line counts API 159 / probe 85 | this slot counted **160** / **86** (last source line) |

---

## 7. Residuals (honest, not blockers for no-loss)

1. `DemoSeeder` + `FakeMt5BrokerConnector` + `DemoBrokerFactory` remain on disk for tests.
2. `mt5-worker/Worker.cs` still hardcodes the four dummy logins for scoring (catalog sync is live).
3. Hosted auto-score is deals-only; empty catalog accounts stay `INSUFFICIENT_DATA` until `/api/ops/resync` or deals arrive.
4. `.env` REAL_COPY is **armed**; next person who implements a sender would see the runtime flag true. Absence of `35=D` is the remaining capital lock.
5. Standalone `tools/DemoFixTestTrade` can emit demo-gateway `35=D` if invoked. Not wired to API/workers. Demo-host gate present.
6. `IsSecret` does not reject password value `dummy` (gate tests exist under `_tmp_r*_gate`).
7. This slot did not live-attach; 18/8460 is the 08:42Z probe, independently re-summed.

---

## 8. Files read (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\tools\DemoFixTestTrade\Program.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (ListLogins*)
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + group tables + dummy-login grep)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (stale “forced false” pin noted)

Product source not edited. Test source not edited. Secret values not printed.
