# W500_RESEARCH_174 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **174** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_174 |
| Assigned | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report only. |
| Secrets printed | **None.** Password / proxy / FIX values never quoted. Key **names**, lengths, and placeholder-token classification only. |
| Method | Full `read_file` of `LiveMt5Registration.cs` (94/94), `DependencyInjection.cs` (62/62), API / mt5-worker / fix-worker `Program.cs` + `Worker.cs`, `CopyTradingService.cs` (257/257), `CopyTradingHostedService.cs`, `LiveIngestHostedService.cs`, `DealIngestionService.cs` catalog path, `NativeMt5BrokerConnector` group/user walk, `CTraderFixSession.cs` (135/135), `CTraderFixLogonHostedService.cs`, `CTraderFixDemoTestTrade.cs` (existence + demo gate), `LiveRuntimeStatus.cs`, `EnvFile.cs`, `LiveBrokerProbe\Program.cs`, `FakeMt5BrokerConnector` / `DemoSeeder` (existence + call graph), `BrokerCatalogSeed.cs`, `RiskEngine.cs` allow-send tail, `BaselineScorer.CanPromoteToLive`, `CTraderFixOptions.cs`. `grep` of `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `DemoSeeder` / `35=D` / `NewOrderSingle` / `REAL_COPY` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`. YoPips C++ `config\app_config.cpp` + `HasRealPasswords` / `LiveMt5Registration` grep in `src` (**0**). On-disk census `LIVE_GROUPS_AND_TRADERS.json` (counts + group names only; **not re-attached**). `.env` classified by **key presence / length / placeholder token**; values **not** copied. Hermetic probes `_tmp_r14_gate` / `_tmp_r74_gate` read as **source tables**; `RESULT.json` **absent** (not re-run). |

**Honesty rule:** fail-closed is **not** “the function exists.” It is: missing / placeholder / one-sided secrets **must not** start a dummy FakeMt5 census, **must not** start a half-broker live graph, and **must not** emit a live cTrader `35=D` from the copy pipeline. `StringComparison.Ordinal` is **not** case-insensitive. `HasRealPasswords == true` is **not** an order arm. Presence of a real-looking password string is **not** proof of a live Manager session. Sibling slices that still say `RealCopyEnabled` is **hardcoded `false`** are **stale vs current `DependencyInjection.cs` L39–42**. This slot did **not** launch `dotnet` and did **not** re-attach LiveBrokerProbe; the decision table is a **source-faithful replica** of the 3-clause `IsSecret` predicate.

---

## 0. Verdict (binding)

| Claim | Measured | Class |
|---|---|---|
| `HasRealPasswords` is dual-AND of Achiever + Starwave password keys | **Yes** | `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD` |
| Empty / whitespace / missing either key → `false` | **Yes** | fail-closed |
| Exact `<SECRET>` (`StringComparison.Ordinal`) → `false` | **Yes** | fail-closed |
| Substring `(a/c` (`Ordinal`) → `false` | **Yes** | fail-closed (blocks `.env` comment paint) |
| One broker real + other dummy → `false` | **Yes** | fail-closed; graph cannot start half-live |
| `AddTraderIntelligence` throws **before** `CreateConnectors` when `false` | **Yes** | exact `InvalidOperationException` below |
| Dummy `FakeMt5BrokerConnector` registered on that throw path | **No** | host never builds; no 10001 tape |
| Product unit/integration tests of `HasRealPasswords` | **0 hits** | untested in `D:\Prop\tests` |
| `<secret>` / `<Secret>` / `(A/C` / `dummy` / `x` treated as real | **Yes (`true`)** | **fail-open residual** of the heuristic |
| `CreateConnectors` / `CreateConnectorsFromEnvironment` re-check the gate | **No** | factory is ungated if called directly |
| LiveBrokerProbe uses the same `IsSecret` | **No** | whitespace-only |
| C++ `AppConfig::load` has an equivalent dual-password refuse | **No** | single `MT5_PASSWORD`; empty default |
| Product copy / `CTraderFixSession` can emit live `35=D` | **No** | **`SAFE_BY_ABSENCE`** on session + copy |
| Demo helper `CTraderFixDemoTestTrade` can emit `35=D` | **Yes, demo-gated** | **not** on DI copy path; only `tools/DemoFixTestTrade` |
| `RealCopyEnabled` on DI path | **env-bound** | `configuration["REAL_COPY_EXECUTION_ENABLED"] == "true"` (ignore-case) |
| Root `.env` `REAL_COPY_EXECUTION_ENABLED` | **`true`** | flag **armed** if API loads `.env`; **not** a send license |
| After gate passes, ingest asks for ALL groups + ALL manager logins | **Yes** | `GroupRequestArray("*")` + `GetAccountsAsync(null)` |

**One-line:** `HasRealPasswords` **is** fail-closed for missing / exact-`<SECRET>` / `(a/c` / one-sided keys: DI throws and never substitutes FakeMt5. It is **not** a complete secret validator (`Ordinal` case hole, dummy words, no login/server check, factory bypass). After it returns true, both native managers register and ingest fetches **all** groups/traders. Copy still cannot spend capital because the product session encoder has **no** `35=D`, `CopyTradingService.NewOrderSingleImplemented` is `const false`, persisted `AllowFixSend` is hardcoded `false`, and `CanPromoteToLive => false`. `.env` `REAL_COPY_EXECUTION_ENABLED=true` **does** arm the runtime bool on the API path — that is **not** fail-closed for the flag, and it is **not** a ticket.

Slot verdict: **`PASS_FAIL_CLOSED_DI`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — this factory never sends; copy persist path cannot emit a live NewOrderSingle.

---

## 1. Assigned type (current disk, 94 lines)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` — static manager-connection **factory** plus a secret-**presence** predicate. Not a Manager session. Not a FIX writer.

| Member | Role |
|---|---|
| `HasRealPasswords(IConfiguration)` | public dual-password **presence** gate |
| `CreateConnectors(IConfiguration)` | always builds **exactly two** `NativeMt5BrokerConnector`s |
| `CreateConnectorsFromEnvironment()` | same factory over process env (`EnvConfiguration`) |
| `IsSecret(string?)` | **private** 3-clause heuristic; only `HasRealPasswords` calls it |
| `EnvConfiguration` | minimal `IConfiguration` over `Environment.GetEnvironmentVariable` |

### 1.1 `HasRealPasswords` — AND of two key names

```10:15:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

Login, server, port, proxy, and cTrader FIX keys are **not** in this predicate. A filled Achiever password with an empty Starwave password is **`false`**. That is the dual-broker refuse.

### 1.2 `IsSecret` — three clauses, `Ordinal`

```52:55:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

| Clause | Fail-closed when | Hole |
|---|---|---|
| `IsNullOrWhiteSpace` | missing / `""` / `"  "` / `"\t"` | none for those |
| `Contains("<SECRET>", Ordinal)` | exact mixed token `<SECRET>` anywhere | `<secret>` / `<Secret>` pass |
| `Contains("(a/c", Ordinal)` | lowercase account-comment paint | `(A/C` / `(A/c` pass |

`Contains` is a **substring** test: `pre<SECRET>post` is **not** a secret. Dummy words (`dummy`, `changeme`, `replace_with_manager_password`) and single characters pass. There is **no** login/server/port/connect proof.

### 1.3 `CreateConnectors` — Native ×2, no Fake, no gate

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // MT5_SERVER / MT5_PORT / MT5_LOGIN / MT5_PASSWORD + ACHIEVER_PROXY_*
            ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // MT5_STARWAVEFX_* ; ProxyEnabled = false hard pin
            ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

Measured pins:

- Return length is **always 2**. Codes: `BrokerCodes.Achiever` = `"ACHIEVER"`, `BrokerCodes.StarwaveFx` = `"STARWAVEFX"` (`D:\Prop\src\Domain\Brokers\BrokerCodes.cs`).
- Starwave `ProxyEnabled = false` is a **hard pin** (L45). Achiever proxy is env-bound (`ACHIEVER_PROXY_ENABLED`).
- `CreateConnectors` / `CreateConnectorsFromEnvironment` **do not** call `HasRealPasswords`. Empty passwords become `Password = ""`. `Login = 0` when `ulong.TryParse` fails. That is fail-on-`Connect`, not a silent FakeMt5 census.
- `FakeMt5BrokerConnector` is **not referenced** in this file.

`CreateConnectorsFromEnvironment()` (L17–18) wraps `new EnvConfiguration()` and **does not** call `HasRealPasswords`.

---

## 2. Product DI — throw, then Native only

`D:\Prop\src\Infrastructure\DependencyInjection.cs` (62/62).

```36:59:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
        services.AddScoped<CopyTradingService>();
        services.AddSingleton<TraderIntelligence.Domain.Risk.RiskEngine>();

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
        // ... LiveIngestHostedService + CTraderFixLogonHostedService + CopyTradingHostedService
```

Fail-closed sequence on `HasRealPasswords == false`:

1. Throw **before** `CreateConnectors`.
2. No `IMt5BrokerConnector`, no `IBrokerRegistry`, no hosted ingest, no FIX logon host, no copy host.
3. No `DemoBrokerFactory.CreateDefault()` / `FakeMt5BrokerConnector` substitution.
4. `FakeMt5BrokerConnector` exists at `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (demo logins **10001 / 10002 / 10003 / 99001**) but is **not** registered on this path.
5. `DemoSeeder` is called from `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` and swarm `_tmp_*` harnesses only — **not** from API / worker `Program.cs`.

Hosts that call `AddTraderIntelligence` (therefore inherit the throw):

| Host | Loads `D:\Prop\.env` via `EnvFile`? | If gate false |
|---|---|---|
| `D:\Prop\apps\api\Program.cs` L10 + L15 | **Yes** (`FindAndLoad` then `AddEnvironmentVariables`) | process does not start |
| `D:\Prop\apps\mt5-worker\Program.cs` L7 | **No** | throw unless process/machine env already holds both keys |
| `D:\Prop\apps\fix-worker\Program.cs` L7 | **No** | same |

`apps/*/appsettings.json` contain **no** `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD` keys (grep 0). `launchSettings.json` for the API sets only `ASPNETCORE_ENVIRONMENT`. If secrets live only in `D:\Prop\.env` and a worker is started without inheriting process env, `HasRealPasswords` is `false` → **throw**. That is correct refuse, not a dummy fill.

Residual (adjacent, **not** a `HasRealPasswords` bypass): `D:\Prop\apps\mt5-worker\Worker.cs` L31 still **scores** `{10001,10002,10003,99001}` after live `SyncBrokerAsync` of **both** codes. That leftover dummy **score set** does not shrink `GetAccountsAsync(null)` and does not register Fake. Hosted ingest (`LiveIngestHostedService` L106) scores `ListLoginsWithDealsAsync` instead.

`BrokerCatalogSeed.EnsureAsync` (API L155, workers after `EnsureCreated`) paints **catalog rows only** (broker host/port/manager login, XAUUSD instrument, kill switch, FIX session stubs). It does **not** seed Fake 10001 accounts. Seed `ProxyEnabled=true` / `81.29.145.69:49527` is catalog paint; live `ProxySet` uses `LiveMt5Registration` options.

---

## 3. Static `IsSecret` / `HasRealPasswords` truth table

Source-faithful replica of L10–15 + L52–55. **Not** a compiled run (`RESULT.json` absent under `_tmp_r14_gate` and `_tmp_r74_gate`). Pre-existing harnesses encode the same cases plus DI throw + unguarded factory.

| # | `MT5_PASSWORD` | `MT5_STARWAVEFX_PASSWORD` | `HasRealPasswords` | Role |
|---|---|---|---|---|
| 1 | missing | missing | `false` | fail-closed |
| 2 | `""` | `""` | `false` | fail-closed |
| 3 | `"  "` | `"\t"` | `false` | fail-closed |
| 4 | non-placeholder | `""` | `false` | one-sided refuse |
| 5 | `""` | non-placeholder | `false` | one-sided refuse |
| 6 | `<SECRET>` | `<SECRET>` | `false` | placeholder refuse |
| 7 | `<SECRET>` | non-placeholder | `false` | AND |
| 8 | non-placeholder | `<SECRET>` | `false` | AND |
| 9 | `pw (a/c 1)` | `pw (a/c 2)` | `false` | comment paint refuse |
| 10 | `pre<SECRET>post` | non-placeholder | `false` | substring |
| 11 | non-placeholder | non-placeholder | `true` | intended open |
| 12 | `<secret>` | `<secret>` | **`true`** | **Ordinal case hole** |
| 13 | `<Secret>` | `<Secret>` | **`true`** | same |
| 14 | `dummy` | `changeme` | **`true`** | word hole |
| 15 | `x` | `y` | **`true`** | length hole |
| 16 | `pw (A/C 1)` | `pw (A/C 2)` | **`true`** | case hole on `(a/c` |

DI throw message (L37), exact:

`Real MT5 passwords are required. Dummy/fake broker data is disabled.`

Unguarded factory (r74 harness documents this): `CreateConnectors(empty cfg)` still returns **two** `NativeMt5BrokerConnector` instances. That is a **caller-discipline** residual, not a DI bypass.

---

## 4. Call-site census (`grep` `*.cs`)

| Site | Calls `HasRealPasswords`? | Calls `CreateConnectors*`? |
|---|---|---|
| `LiveMt5Registration.cs` | definition | definition |
| `DependencyInjection.cs` L36 / L47 | **Yes** | **Yes**, after throw |
| `apps/api/Program.cs` | via DI | via DI |
| `apps/mt5-worker/Program.cs` | via DI | via DI |
| `apps/fix-worker/Program.cs` | via DI | via DI |
| `tools/LiveBrokerProbe/Program.cs` | **No** | `CreateConnectorsFromEnvironment()` |
| `D:\Prop\tests` | **0** | **0** |
| `_tmp_r14_gate` / `_tmp_r74_gate` | synthetic only | r74 also calls unguarded factory |

### 4.1 LiveBrokerProbe is weaker than `IsSecret`

```6:13:D:\Prop\tools\LiveBrokerProbe\Program.cs
var envPath = EnvFile.FindAndLoad();
var aPass = Environment.GetEnvironmentVariable("MT5_PASSWORD");
var sPass = Environment.GetEnvironmentVariable("MT5_STARWAVEFX_PASSWORD");
if (string.IsNullOrWhiteSpace(aPass) || string.IsNullOrWhiteSpace(sPass))
{
    Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "real_passwords_missing", env = envPath }));
    return 2;
}
```

Probe accepts `<SECRET>` / `(a/c` / `dummy` as long as they are non-whitespace, then walks **all** groups + **all** accounts. It is an operator tool, **not** the product host. It does **not** send FIX.

---

## 5. Root `.env` classification (values not copied)

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs`) searches cwd / parents / hardcoded `D:\Prop\.env`, then `SetEnvironmentVariable` for each `KEY=value` (optional surrounding quotes stripped).

Replica of `IsSecret` on this file (classification only):

| Key | Present | Length | `<SECRET>` Ordinal | `(a/c` Ordinal | `IsSecret` replica |
|---|---|---|---|---|---|
| `MT5_PASSWORD` | yes | **8** | no | no | **true** |
| `MT5_STARWAVEFX_PASSWORD` | yes | **11** | no | no | **true** |

Replica `HasRealPasswords` on this file: **`true`**. If the API process loads this file (it does, via `EnvFile.FindAndLoad` + `AddEnvironmentVariables`), DI **will not throw**. That is intended fail-**open after both slots look real**. Older E011 “password slots are `<SECRET>` len 8” is **stale** for these two keys (Starwave length 11; Achiever length 8 but **not** the `<SECRET>` token).

Non-secret adjacent flags (names + booleans only):

| Key | Measured |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **`true`** — arms `LiveRuntimeStatus.RealCopyEnabled` on API |
| `FEATURE_COPY_TRADING_ENABLED` | **`true`** — **unread** by `HasRealPasswords` / DI; `CopyTradingService.GetStatusAsync` hardcodes `FeatureCopyEnabled: true` |
| `CTRADER_FIX_HOST` | `demo-us-eqx-01.p.c-trader.com` (demo host; not a live `live-` gate fail) |
| `CTRADER_FIX_ACCOUNT_ID` | `5328266` (matches demo default in `CTraderFixLogonHostedService` L41) |
| `ACHIEVER_PROXY_ENABLED` | `true` (Achiever hop only) |
| `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN` | present (`2027` / `9904`) — **not** checked by `HasRealPasswords` |
| `MT5_SERVER` / `MT5_STARWAVEFX_SERVER` | present — **not** checked by `HasRealPasswords` |

`apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` is **`false`** and is **not** the DI bind. The live bind is env `REAL_COPY_EXECUTION_ENABLED`.

---

## 6. After the gate: ALL groups + ALL manager traders

`HasRealPasswords` never enumerates users. Completeness is the **next** layer, only reachable if the gate passed (or if a caller bypasses via `CreateConnectors*`).

| Layer | Call | Window |
|---|---|---|
| Native groups | `GroupRequestArray("*")`; fallback `GroupTotal`/`GroupNext` | **all** groups this manager can see |
| Native accounts | `GetAccountsAsync(null)` → every group name → `UserRequestArray` / `UserGetByGroup` / `UserLogins` + `UserRequestByLogins` | **all** logins in those groups |
| Catalog persist | `DealIngestionService.SyncCatalogAsync` L45–49 | `GetGroupsAsync` + `GetAccountsAsync(null)` |
| Hosted ingest | `LiveIngestHostedService` L39–56 `registry.All()` | **both** registered connectors |
| Manual resync | `apps/api/Program.cs` L124 `ACHIEVER` + `STARWAVEFX` | same catalog walk |
| Operator dump | `LiveBrokerProbe` L25–26 | same |

```144:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5GroupDto> GetGroupsCore()
    {
        // GroupRequestArray("*") then GroupTotal/GroupNext fallback
    }

    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // null/whitespace group → foreach GetGroupsCore() name
        // ReadAccountsForGroup: UserRequestArray / UserGetByGroup / UserLogins
    }
```

```38:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        ...
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);
        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
        return new BrokerSyncResult(groups.Count, accounts.Count, 0, 0);
    }
```

On catalog / deal failure, ingest logs **“No dummy data will be substituted”** (`LiveIngestHostedService` L70). That is fail-closed for Fake fill after a live connect miss.

### 6.1 Last measured live census (not re-attached this slot)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

| Field | Value |
|---|---|
| Probe | `LiveBrokerProbe` |
| UTC | `2026-08-18T08:42:16.8519545+00:00` |
| `envLoaded` | `true` |
| Achiever | `connected=true`, **8 groups**, **6512 accounts**, **1506** open positions |
| StarwaveFX | `connected=true`, **10 groups**, **1948 accounts**, **478** open positions |
| **Total** | **18 groups / 8460 manager traders** |

Achiever group names (account counts sum **6512**):

| Group | Accounts |
|---|---|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |

StarwaveFX group names (account counts sum **1948**):

| Group | Accounts |
|---|---|
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

Dummy logins `{10001,10002,10003,99001}` are **not** in that census. Groups the manager cannot see are outside this login’s permission set — that is ACL, not a `HasRealPasswords` cap. JSON note: “Passwords never written. Groups and manager logins only.” This slot did **not** dump trader rows.

---

## 7. Copy → cTrader must not send live orders

`HasRealPasswords` has **zero** FIX / order symbols. Adjacent no-send pins (remeasured this pass):

| Pin | File | Measured |
|---|---|---|
| Product session encoder | `CTraderFixSession.BuildLogon` L94–108 | **`(35, "A")` only**. No `D`. 135/135 lines. |
| FIX hosted service | `CTraderFixLogonHostedService` | Logon `35=A` on QUOTE 5211 + TRADE 5212; log line “NewOrderSingle still unimplemented” |
| Copy const | `CopyTradingService.NewOrderSingleImplemented` L16 | **`const false`** |
| Venue const | `CopyTradingService.VenueReconciled` L15 | **`const false`** |
| Persist | `CopyTradingService` L192 | `AllowFixSend = false` **hardcoded** on `RiskDecisionRecord` |
| Live-send branch | L198–201 | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` → then status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — **still no send** |
| Else | L204 | `SHADOW_ONLY` + in-memory `ShadowCopyEngine.SimulateEntry` |
| Hosted copy | `CopyTradingHostedService` L28–30 | `GenerateShadowIntentsAsync` only; log “Live NewOrderSingle still blocked” |
| Promotion | `BaselineScorer.CanPromoteToLive` L211 | **`=> false`** |
| Risk `AllowFixSend` formula | `RiskEngine` L147–150 | `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` — copy passes `Reconciled = VenueReconciled = false` → formula **false** even if `RealCopyEnabled` |
| Snapshot | `LiveRuntimeStatus.Snapshot` L42–44 | armed text still says “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” |
| FIX worker | `apps/fix-worker/Worker.cs` L40–46 | paints TRADE `Disconnected` / “NewOrderSingle remains off”; even if `CTrader:RealCopyExecutionEnabled` is true, **refuses send** |
| Options default | `CTraderFixOptions.RealCopyExecutionEnabled` L35 | **`false`** (comment: Default OFF). DI does **not** bind this type for the copy bool; it binds env `REAL_COPY_EXECUTION_ENABLED` onto `LiveRuntimeStatus`. |

`CTraderFixDemoTestTrade` (`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`) **can** `Build("D", …)` (L124, L155). Gates: host must start with `demo-`, sender with `demo.`, must not contain `live.`, account must not be `1369850`. **Callers:** only `D:\Prop\tools\DemoFixTestTrade\Program.cs`. **Not** registered in `AddTraderIntelligence`. **Not** called from `CopyTradingService` / hosted copy / FIX logon. Running that **tool** against the demo host in `.env` could place a **demo** ticket; that is **outside** the product copy pipeline and is **not** live Pepperstone capital from Achiever/Starwave copy.

`grep` `35=D` as a literal in `D:\Prop\src` product copy/session files: **0**. NewOrderSingle mentions are comments, status strings, and the `const false` flag.

`HasRealPasswords` passing with live Manager secrets still only arms **ingest + optional FIX Logon (`35=A`)**. It does **not** implement NewOrderSingle. Capital cannot be lost by this gate succeeding.

---

## 8. YoPips C++ backend (not this gate)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm`

| Probe | Result |
|---|---|
| `HasRealPasswords` in `src\` | **0** |
| `LiveMt5Registration` in `src\` | **0** |
| `STARWAVE` dual-password AND in config | **0** |
| `config\app_config.cpp` L150 | `cfg.mt5_password = get("MT5_PASSWORD", "");` — empty default, **no** refuse, **no** `<SECRET>` reject, **single** broker |

That process is the **prop-firm trading backend** (native `DealerSend` / `trade_execution_service`). It is **not** registered by `AddTraderIntelligence` and is **not** the Prop → cTrader copy path. Dual-broker Achiever+Starwave live census is the C# `LiveMt5Registration` path only.

Prop `mt5-sdk\config\app_config.cpp` likewise has **no** `HasRealPasswords`.

---

## 9. What this slot did **not** do

- Did **not** edit product source.
- Did **not** print password / proxy / FIX secret values.
- Did **not** re-run LiveBrokerProbe or a compiled `HasRealPasswords` harness (no shell). Census numbers are from the on-disk JSON dated `2026-08-18T08:42:16Z`.
- Did **not** send FIX, place, flatten, or size a destination order.
- Did **not** start YoPips C++.

---

## 10. Residuals (honest, ranked)

1. **`StringComparison.Ordinal` case hole** — `<secret>`, `<Secret>`, `(A/C` pass `IsSecret`.
2. **Word / length hole** — `dummy`, `changeme`, `x`, `replace_with_manager_password` pass.
3. **No login / server / connect proof** — `HasRealPasswords == true` is not a live session.
4. **`CreateConnectors*` ungated** — probe and any future caller can build Native connectors with empty / placeholder passwords.
5. **LiveBrokerProbe whitespace-only** — weaker than `IsSecret`.
6. **Zero product tests** of `HasRealPasswords` / `IsSecret` under `D:\Prop\tests`.
7. **`.env` `REAL_COPY_EXECUTION_ENABLED=true`** arms `LiveRuntimeStatus.RealCopyEnabled` on the API path. **Not** a ticket (`SAFE_BY_ABSENCE` still holds).
8. **mt5-worker leftover dummy score set** `{10001…}` — adjacent hygiene, not a Fake registration.
9. **Workers do not load `EnvFile`** — correct throw if `.env` is the only secret store; operators must inject process env.
10. **`CTraderFixDemoTestTrade` exists** — demo-only `35=D` helper, not on copy DI. Do not wire it into `CopyTradingService`.

None of these residuals let `AddTraderIntelligence` start a FakeMt5 10001 universe when either password is missing / exact-`<SECRET>` / `(a/c`. None of them emit a **live** copy NewOrderSingle.

---

## 11. Files read (absolute)

- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\Properties\launchSettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\tools\DemoFixTestTrade\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + group-name rows only)
- `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs`
- `D:\Prop\reports\swarm\20260818\_tmp_r74_gate\Program.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`

---

## 12. Conclusion

`LiveMt5Registration.HasRealPasswords` **does** fail-closed the product host against missing / exact-placeholder / one-sided MT5 passwords: throw, no FakeMt5, no half-dummy graph. After it returns true, the factory registers **both** native managers and ingest fetches **all** groups/traders (`GroupRequestArray("*")` + `GetAccountsAsync(null)`; last measured **18 / 8460**). Copy-to-cTrader remains **unarmed on the wire** (`SAFE_BY_ABSENCE` of a product `35=D` encoder + `NewOrderSingleImplemented = false` + persisted `AllowFixSend = false` + `CanPromoteToLive => false`). Residuals are validator weakness + untested CI + factory/probe bypass + **env-armed `RealCopyEnabled`** — **not** a live-order path.

**Slot 174 verdict: `PASS_FAIL_CLOSED_DI`. Risk to capital: `NONE`.**
