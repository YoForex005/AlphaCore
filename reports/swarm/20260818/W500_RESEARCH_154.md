# W500_RESEARCH_154 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **154** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_154 |
| Assigned | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report + index/log pins only. |
| Secrets printed | **None.** Password / proxy / FIX / connection-string **values** never quoted. Key **names**, lengths, and placeholder-token classification only. |
| Method | Independent full `read_file` of `LiveMt5Registration.cs` (94/94), `DependencyInjection.cs` (61/61), host `Program.cs` trio, `LiveIngestHostedService`, `DealIngestionService`, `NativeMt5BrokerConnector` catalog cores, `CTraderFixSession` (135/135), `CTraderFixLogonHostedService`, `CTraderFixOptions`, `CopyTradingService`, `CopyTradingHostedService`, `LiveRuntimeStatus`, `EnvFile`, `LiveBrokerProbe`, `FakeMt5BrokerConnector`/`DemoSeeder`/`BrokerCatalogSeed`, `RiskEngine`, `BaselineScorer.CanPromoteToLive`, worker loops. `grep` of `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `DemoSeeder` / `35=D` / `NewOrderSingle` / `DealerSend` / `OrderSend` / `AllowFixSend` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`. YoPips `config\app_config.cpp` + `app_config.h` + `src/` (0 dual-broker AND). On-disk census `LIVE_GROUPS_AND_TRADERS.json` (counts + group **names** only; logins not recopied). `.env` classified by **key presence / length / placeholder token**. This slot did **not** launch `dotnet`, did **not** re-attach LiveBrokerProbe, did **not** open FIX TLS. |

**Honesty rule:** fail-closed is **not** “the function exists.” It is: missing / placeholder / one-sided secrets **must not** start a dummy FakeMt5 census, **must not** start a half-broker live graph, and **must not** emit a live cTrader `35=D`. `StringComparison.Ordinal` is **not** case-insensitive. `HasRealPasswords == true` is **not** an order arm. Presence of a real-looking password string is **not** proof of a live Manager session. Slots **14 / 34 / 54 / 114** that pin `RealCopyEnabled` as **hardcoded `false`** are **stale vs current `DependencyInjection.cs` L39–42** (env-bound). A002 / A010 / C05 / C42 / R003 that say DI always registers `DemoBrokerFactory.CreateDefault()` are **stale vs current `D:\Prop\src`**. Decision table below is a **source-faithful replica** of the 3-clause `IsSecret` predicate, not a compiled test run (`RESULT.json` still absent under `_tmp_r14_gate` / `_tmp_r74_gate`).

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
| C++ `AppConfig::load` has an equivalent dual-password refuse | **No** | single `MT5_PASSWORD`; password **not** required at startup |
| This type can emit live `35=D` / NewOrderSingle | **No** | **`SAFE_BY_ABSENCE`** |
| `RealCopyEnabled` on DI path | **env-bound** | `string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", OrdinalIgnoreCase)` |
| Lab `.env` `REAL_COPY_EXECUTION_ENABLED` | **`true`** | arms a **flag**, not a sender |
| Hosted FIX logon re-pins `RealCopyEnabled=false` | **No** | `CTraderFixLogonHostedService` only writes Quote/Trade status |

**One-line:** `HasRealPasswords` **is** fail-closed for missing / exact-`<SECRET>` / `(a/c` / one-sided keys: DI throws and never substitutes FakeMt5. It is **not** a complete secret validator (`Ordinal` case hole, dummy words, no login/server check, factory bypass). After the gate passes, both native connectors are registered and ingest asks for **all** groups/traders (`GroupRequestArray("*")` + `GetAccountsAsync(null)`). Copy still cannot spend capital because no NewOrderSingle encoder exists (`SAFE_BY_ABSENCE`), even though `REAL_COPY_EXECUTION_ENABLED` is now env-bound and the lab `.env` key is `true`.

Slot verdict: **`PASS_FAIL_CLOSED_DI`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — this factory never sends; `CTraderFixSession` builds `35=A` only; `CopyTradingService.NewOrderSingleImplemented = false`; persisted `AllowFixSend` is forced `false`. Flag-true is **not** a ticket.

---

## 1. Assigned type (current disk, 94 lines)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` — static manager-connection **factory** plus a secret-**presence** predicate. Not a Manager session. Not a FIX writer.

Members (measured, full-file read):

| Member | Role |
|---|---|
| `HasRealPasswords(IConfiguration)` | dual password **presence** gate |
| `CreateConnectorsFromEnvironment()` | process-env wrapper via private `EnvConfiguration` |
| `CreateConnectors(IConfiguration)` | constructs **exactly two** `NativeMt5BrokerConnector`s |
| `IsSecret(string?)` | private 3-clause predicate |
| `EnvConfiguration` | `IConfiguration` that reads `Environment.GetEnvironmentVariable` only |

It does **not** call `Connect`, `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `DealRequest*`, `PositionRequest*`, or any FIX writer. It cannot subset the trader universe.

```10:15:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

```52:55:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

What the gate **reads** (names only): `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`.

What the gate **does not** read: `MT5_LOGIN`, `MT5_STARWAVEFX_LOGIN`, `MT5_SERVER`, `MT5_STARWAVEFX_SERVER`, ports, proxy keys, `CTRADER_FIX_PASSWORD`, `DATABASE_URL`, `REAL_COPY_EXECUTION_ENABLED`, `FEATURE_COPY_TRADING_ENABLED`.

`CreateConnectors` still **constructs** both slots even if those other fields are empty / unparseable (`Login = 0`, `Server = ""`, default port 443). That is **outside** `HasRealPasswords`. A `Login = 0` connector then **fails at `ConnectAsync`**, not by silently ingesting only the manager login.

### 1.1 Dual native factory (exactly two brokers)

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            Server = config["MT5_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_PORT"], out var ap) ? ap : 443,
            Login = ulong.TryParse(config["MT5_LOGIN"], out var al) ? al : 0,
            Password = config["MT5_PASSWORD"] ?? "",
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            // ... ACHIEVER_PROXY_* ...
            NativeDllDirectory = dllDir
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            Server = config["MT5_STARWAVEFX_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_STARWAVEFX_PORT"], out var sp) ? sp : 443,
            Login = ulong.TryParse(config["MT5_STARWAVEFX_LOGIN"], out var sl) ? sl : 0,
            Password = config["MT5_STARWAVEFX_PASSWORD"] ?? "",
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

Measured pins:

| Slot | Broker code | Password key | Proxy |
|---|---|---|---|
| Achiever | `BrokerCodes.Achiever` = `"ACHIEVER"` | `MT5_PASSWORD` | optional `ACHIEVER_PROXY_*` → `PROXY_HTTP` |
| StarwaveFX | `BrokerCodes.StarwaveFx` = `"STARWAVEFX"` | `MT5_STARWAVEFX_PASSWORD` | **hard `ProxyEnabled = false`** |

`FakeMt5BrokerConnector` is **not referenced** in this file. `CreateConnectorsFromEnvironment()` (`L17–18`) wraps `new EnvConfiguration()` and **does not** call `HasRealPasswords`.

---

## 2. Product DI is the only fail-closed caller

`D:\Prop\src\Infrastructure\DependencyInjection.cs` — **61** physical lines (slots 54/114 that say **59** and “hardcoded false” are **stale**).

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

        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
        // ...
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        services.AddHostedService<CopyTradingHostedService>();
```

| On `HasRealPasswords == false` | **throw** before `CreateConnectors` |
|---|---|
| Fake / Demo substitution | **none** (`grep FakeMt5\|DemoBrokerFactory\|DemoSeeder` on this file = **0**) |
| Connectors if gate passes | **exactly two** `NativeMt5BrokerConnector` |
| Copy hosted service | registered (SHADOW intents only; see §7) |
| `RealCopyEnabled` | **reads env** (not pinned false) |

`AddTraderIntelligence` is the **only** product caller of `HasRealPasswords`. Hosts that use it:

| Host | Calls `AddTraderIntelligence`? | Loads `D:\Prop\.env`? |
|---|---|---|
| `apps/api/Program.cs` L10 + L15 | **Yes** | **Yes** — `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` |
| `apps/mt5-worker/Program.cs` L7 | **Yes** | **No** `EnvFile` (process / machine / `appsettings` only) |
| `apps/fix-worker/Program.cs` L7 | **Yes** | **No** `EnvFile` |
| `tools/LiveBrokerProbe/Program.cs` | **No** | **Yes** `EnvFile.FindAndLoad()`, then whitespace-only check |

`apps/*/appsettings.json` contain **no** `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD` keys (this-slot grep = 0). If secrets live only in `D:\Prop\.env` and a worker is started without inheriting process env, `HasRealPasswords` is `false` → **throw** (fail-closed start). That is correct refuse, not a dummy fill.

API startup after DI: `EnsureCreated` + `BrokerCatalogSeed.EnsureAsync` only (`apps/api/Program.cs` L152–156). Catalog seed writes **two broker rows** + XAU instrument + kill-switch + FIX session stubs. It does **not** invent logins 10001/10002/10003/99001.

---

## 3. Static `IsSecret` / `HasRealPasswords` truth table

`HasRealPasswords = IsSecret(MT5_PASSWORD) && IsSecret(MT5_STARWAVEFX_PASSWORD)`

Source-faithful replica (not executed this session):

| Case | `MT5_PASSWORD` | `MT5_STARWAVEFX_PASSWORD` | Actual `HasRealPasswords` | Strict fail-closed | Match? |
|---|---|---|---|---|---|
| both missing | `null` | `null` | **false** | false | yes |
| both empty | `""` | `""` | **false** | false | yes |
| both whitespace | `"  "` | `"\t"` | **false** | false | yes |
| Achiever only | non-placeholder | `""` | **false** | false | yes |
| Starwave only | `""` | non-placeholder | **false** | false | yes |
| both exact `<SECRET>` | `<SECRET>` | `<SECRET>` | **false** | false | yes |
| one-sided `<SECRET>` | `<SECRET>` / ok | ok / `<SECRET>` | **false** | false | yes |
| substring `<SECRET>` | `pre<SECRET>post` | ok | **false** | false | yes |
| `(a/c` comment paint | `pw (a/c 1)` | `pw (a/c 2)` | **false** | false | yes |
| both synthetic ok | `not-a-placeholder-token` | same | **true** | true | yes |
| lowercase `<secret>` | `<secret>` | `<secret>` | **true** | **false** | **NO — Ordinal hole** |
| mixed `<Secret>` | `<Secret>` | `<Secret>` | **true** | **false** | **NO — Ordinal hole** |
| dummy words | `dummy` | `changeme` | **true** | **false** | **NO — heuristic** |
| single char | `x` | `y` | **true** | **false** | **NO — heuristic** |
| uppercase `(A/C` | `pw (A/C 1)` | `pw (A/C 2)` | **true** | **false** | **NO — Ordinal hole** |

On-disk unexecuted harnesses (synthetic tokens only; **no** `RESULT.json` at read time):

- `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` — 15 cases + DI throw assertion
- `D:\Prop\reports\swarm\20260818\_tmp_r74_gate\Program.cs` — 17 cases + one-sided throw + open-gate connector census + ungated `CreateConnectors(empty)`

Product `D:\Prop\tests` grep `HasRealPasswords|IsSecret` = **0**. The gate is **untested in CI**.

`mt5-sdk/.env.example` style `MT5_PASSWORD=replace_with_manager_password` would pass `IsSecret` (no `<SECRET>`, no `(a/c`). DI would **start** two native connectors. `Connect` would then fail (wrong password). Ingest would **not** paint Fake 10001 (`LiveIngestHostedService` L70: “No dummy data will be substituted.”). That is fail-on-connect, not fail-on-registration.

---

## 4. Call-site census

Grep `HasRealPasswords` / `CreateConnectors` / `CreateConnectorsFromEnvironment` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tools` + `D:\Prop\tests`:

| Site | Calls `HasRealPasswords`? | Calls `CreateConnectors*`? |
|---|---|---|
| `LiveMt5Registration.cs` | definition | definition |
| `DependencyInjection.cs` | **Yes** (throw gate) | **Yes** `CreateConnectors` after throw |
| `apps/api/Program.cs` | via DI | via DI |
| `apps/mt5-worker/Program.cs` | via DI | via DI |
| `apps/fix-worker/Program.cs` | via DI | via DI |
| `tools/LiveBrokerProbe/Program.cs` | **No** (whitespace only) | **Yes** `CreateConnectorsFromEnvironment` |
| `D:\Prop\tests` | **0** | **0** |

Factory bypass: any process that calls `CreateConnectors` / `CreateConnectorsFromEnvironment` **directly** builds two `NativeMt5BrokerConnector` instances with `Password = ""` when keys are missing. Connect then fails (auth), unless a caller skips Connect. LiveBrokerProbe does Connect after a **weaker** gate.

### 4.1 LiveBrokerProbe is weaker than `HasRealPasswords`

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

Whitespace-only. A literal `<SECRET>` or `(a/c` token would **pass** this probe and then fail at Manager `Connect`. Probe then walks `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. JSON note: “Passwords never written. Groups and manager logins only.”

---

## 5. Lab `.env` classification (no values)

File: `D:\Prop\.env` (exists). Classification by this-slot `grep` of **key names only**; values inspected solely for length + `<SECRET>` / `(a/c` tokens and then discarded.

| Key | Present | Length | `<SECRET>` Ordinal | `(a/c` Ordinal | `IsSecret` replica |
|---|---|---:|---|---|---|
| `MT5_PASSWORD` | **Yes** | 8 | no | no | **true** |
| `MT5_STARWAVEFX_PASSWORD` | **Yes** | 11 | no | no | **true** |
| `MT5_LOGIN` | Yes | 4 | n/a | n/a | not gated |
| `MT5_STARWAVEFX_LOGIN` | Yes | 4 | n/a | n/a | not gated |
| `MT5_SERVER` | Yes | (IPv4) | n/a | n/a | not gated |
| `MT5_STARWAVEFX_SERVER` | Yes | (IPv4) | n/a | n/a | not gated |
| `REAL_COPY_EXECUTION_ENABLED` | Yes | — | — | — | value **`true`** (boolean; not a secret) |
| `FEATURE_COPY_TRADING_ENABLED` | Yes | — | — | — | value **`true`** (boolean; unused by `HasRealPasswords`) |

Replica `HasRealPasswords` on this file: **`true`**. If the API process loads this file (it does, via `EnvFile.FindAndLoad` + `AddEnvironmentVariables`), DI **will not throw**. That is intended fail-**open after both slots look real**. E011’s “password slots are `<SECRET>` len 8” is **stale** for the two MT5 keys (Starwave length 11; Achiever length 8 but **not** the `<SECRET>` token).

`HasRealPasswords` itself never enumerates users. Completeness is the **next** layer, only reachable if the gate passed.

---

## 6. After the gate: ALL groups + ALL manager traders

`HasRealPasswords` cannot shrink the census. It only decides whether the host may construct the two native readers.

### 6.1 Native walk (flag-blind)

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` L144–187):

1. `GroupRequestArray("*")` then `arr.Next(i)`
2. Fallback if empty: `GroupTotal` + `GroupNext`

`GetAccountsCore(null)` (L189–214): walks **every** group from `GetGroupsCore`, then per group:

1. `UserRequestArray(gname)`
2. else `UserGetByGroup`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. account snapshot: `UserAccountRequestArray` / `UserAccountGetByGroup`

This is **all groups + all logins this manager ACL can see**. Groups the manager cannot see are outside this login’s permission set.

### 6.2 Ingest / hosted path

`DealIngestionService.SyncCatalogAsync` (`DealIngestionService.cs` L38–52):

- `GetGroupsAsync` → `UpsertGroupsBatchAsync`
- `GetAccountsAsync(null)` → `UpsertAccountsBatchAsync`

`SyncBrokerAsync` re-runs that catalog, then bulk deals per group + `GetGroupPositionsAsync("*")`.

`LiveIngestHostedService` loops `registry.All()` (the two native connectors), Connect → catalog → deals → score `ListLoginsWithDealsAsync`. On catalog failure it logs **“No dummy data will be substituted.”**

API `POST /api/ops/resync` walks both `"ACHIEVER"` and `"STARWAVEFX"` the same way and scores `ListLoginsAsync` (all persisted logins, not the dummy set).

Residual (adjacent, **not** a `HasRealPasswords` bypass): `apps/mt5-worker/Worker.cs` L31 still **scores** `{10001,10002,10003,99001}` after live `SyncBrokerAsync`. That leftover dummy **score set** does not shrink `GetAccountsAsync(null)` and does not register Fake.

### 6.3 On-disk live census (prior measure; not re-attached)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` — `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, `probe=LiveBrokerProbe`. Group **names** + counts only (trader login rows not recopied here).

| Broker | connected | groups | accounts | openPositions | elapsedMs |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | 8 | 6512 | 1506 | 7212.5885 |
| STARWAVEFX | true | 10 | 1948 | 478 | 6413.478 |
| **sum** | | **18** | **8460** | **1984** | |

Achiever groups (name / account count): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23. Re-sum **6512**.

Starwave groups: `Starwave\cent\FX1\grp1` 11, `grp2` 4, `Starwave\demo\FX2\grp1` 170, `grp2` 1735, `Starwave\real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `Starwave\real\FX3\LP` 2. Re-sum **1948**.

Empty groups are still **fetched** (`demo\yo-instant` 0; three Starwave real groups 0). That is ALL-groups, not “groups with accounts only.”

---

## 7. Copy / cTrader cannot send live orders

`HasRealPasswords` has **zero** FIX / order symbols. Adjacent no-send pins (remeasured this pass):

| Pin | Measured now |
|---|---|
| `CTraderFixSession.BuildLogon` | body starts `(35, "A")` then 34/49/56/50/57/52/98/108/141/553/554. **One** `WriteAsync` of that Logon. Socket disposed before return. |
| `(35, "D")` / `"35=D"` / `MsgType = "D"` under `D:\Prop\src` + `D:\Prop\apps` product `*.cs` | **0** builders (this-slot grep; mentions are comments / log / UI) |
| `DealerSend` / `OrderSend` / `TradeRequest` / `IMTRequest` under `D:\Prop\src` | **0** |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** |
| `CopyTradingService.VenueReconciled` | **`const false`** |
| Persist path | `RiskDecisionRecord.AllowFixSend = false` **hardcoded** (`CopyTradingService.cs` L192) even if `RiskEngine` would compute `allowSend` |
| Live-send `if` | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — last two are const false; branch would only stamp `LIVE_SEND_BLOCKED_UNIMPLEMENTED` |
| Else | `Status = "SHADOW_ONLY"` + optional in-memory shadow fill |
| `CopyTradingHostedService` | 20s loop → `GenerateShadowIntentsAsync`; log: “Live NewOrderSingle still blocked.” |
| `BaselineScorer.CanPromoteToLive` | **`=> false`** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false**; **not** bound from env (`CTrader__RealCopyExecutionEnabled` unused) |
| `CTraderFixLogonHostedService` | FIX password missing → skip logon; else `TryLogonAsync` QUOTE 5211 + TRADE 5212; logs “NewOrderSingle still unimplemented”; **does not** write `RealCopyEnabled` |
| `apps/fix-worker/Worker.cs` | reads nested `CTrader:RealCopyExecutionEnabled` default **false** (log only); stamps FIX rows **Disconnected**; “worker still refuses NewOrderSingle” |
| API `/api/settings` | exposes `runtime.RealCopyEnabled` (so **can be true** if env is true) |
| API `/api/copy/status` | `CopyTradingService.GetStatusAsync` — blockers include `SAFE_BY_ABSENCE` while NOS unimplemented |

`HasRealPasswords` passing with live Manager secrets still only arms **ingest + optional FIX Logon (`35=A`) + SHADOW copy intents**. It does **not** create a `35=D` builder. Capital cannot be lost by this gate succeeding.

### 7.1 Flag residual (do not confuse with fail-closed)

Slots 14/34/54/114: “`RealCopyEnabled` hardcoded false.” **Stale.**

Current: DI copies `.env` `REAL_COPY_EXECUTION_ENABLED=true` onto `LiveRuntimeStatus.RealCopyEnabled`. `RiskEngine` would set `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Copy always passes `Reconciled = VenueReconciled = false`, so engine `allowSend` is **false**, and the persist line **overwrites to false anyway**. Flag-true is a **policy leftover**, not a send path.

---

## 8. YoPips C++ is not this gate

`D:\Projects\YoPips\Backend\C++ Backend PropFirm`:

| Search | Result |
|---|---|
| `HasRealPasswords` / `LiveMt5Registration` under `src/` | **0** |
| `MT5_STARWAVEFX` under `config/` | **0** dual-broker password |
| `AppConfig::load` (`config/app_config.cpp` L150) | `cfg.mt5_password = get("MT5_PASSWORD", "");` — empty default, **no** refuse, **no** `<SECRET>` reject |
| `app_config.h` | single `mt5_password`; no Starwave slot |

That process is the **prop-firm trading backend** (it has native trade execution). It is **not** registered by `AddTraderIntelligence` and is **not** the Prop → cTrader copy path. Dual-broker fail-closed exists only in C# `LiveMt5Registration`.

---

## 9. Fake / demo leftovers (not on the live graph)

| Artifact | On live DI/API path? |
|---|---|
| `FakeMt5BrokerConnector` + `DemoBrokerFactory.CreateDefault()` (`src/Mt5/Connectors/FakeMt5BrokerConnector.cs`) | **No** — 3+1 dummy logins 10001/10002/10003/99001 exist on disk only |
| `DemoSeeder` | Integration test + `_tmp_*` harnesses; **not** called from `apps/*/Program.cs` |
| `BrokerCatalogSeed` | Yes — **broker rows only**, no dummy traders |
| mt5-worker score set `{10001,10002,10003,99001}` | leftover **score** after live sync; not a Fake registration |

---

## 10. Residuals (honest)

1. **`StringComparison.Ordinal` case hole** — `<secret>`, `<Secret>`, `(A/C` pass `IsSecret`.
2. **Dummy words / single-char** pass (`dummy`, `x`).
3. **No login / server / port check** — `Login=0` + empty server still constructs; fail is later at Connect.
4. **`CreateConnectors*` ungated** if called directly.
5. **LiveBrokerProbe** is whitespace-only, not `IsSecret`.
6. **Workers do not load `.env`** — fail-closed throw if process env empty (correct refuse).
7. **Zero product tests** of `HasRealPasswords` / `IsSecret`. Hermetic `RESULT.json` still **absent**.
8. **`REAL_COPY_EXECUTION_ENABLED=true` in lab `.env` is now bound by DI** (slots 14/34/54/114 hard-false pin is stale). Sender still missing.
9. **mt5-worker leftover dummy score set** — does not shrink the live catalog.
10. **This slot did not** re-run LiveBrokerProbe or `dotnet` gate cases (no process launch). Census numbers are the on-disk JSON dated `2026-08-18T08:42:16Z`.

---

## 11. Files read (absolute)

- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Copy\CopyTradingModels.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + group names)
- `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs`
- `D:\Prop\reports\swarm\20260818\_tmp_r74_gate\Program.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h`

---

## 12. Conclusion

`LiveMt5Registration.HasRealPasswords` **is fail-closed on the product DI path**: missing / whitespace / exact `<SECRET>` / `(a/c` on **either** Achiever or StarwaveFX password → host **does not start** → **no** Fake connector → **no** dummy 10001 universe on the live graph.

Both managers register **together**. Ingest / probe ask for **ALL** groups (`*`) and **ALL** users the manager ACL can see. Prior measured census: Achiever **8 / 6512** + Starwave **10 / 1948** = **18 / 8460**.

Copy to cTrader remains unarmed for live orders: **no** `35=D` builder (`SAFE_BY_ABSENCE`), `NewOrderSingleImplemented=false`, `VenueReconciled=false`, persisted `AllowFixSend=false`, `CanPromoteToLive=>false`. `HasRealPasswords==true` plus lab `REAL_COPY_EXECUTION_ENABLED=true` still cannot spend capital.
