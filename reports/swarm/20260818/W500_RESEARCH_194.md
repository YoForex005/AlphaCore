# W500_RESEARCH_194 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **194** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_194 |
| Assigned | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report + INDEX / SWARM_LOG pins only. |
| Secrets printed | **None.** Password / proxy / FIX / connection-string **values** never quoted. Key **names**, lengths, and placeholder-token classification only. |
| Method | Independent full `read_file` of `LiveMt5Registration.cs` (**94/94**), `DependencyInjection.cs` (**62/62**), API / mt5-worker / fix-worker `Program.cs`, `CopyTradingService.cs` (**257/257**), `CopyTradingHostedService.cs`, `LiveIngestHostedService.cs` (**141/141**), `DealIngestionService.cs` catalog path, `NativeMt5BrokerConnector` catalog cores (**458/458**), `CTraderFixSession.cs` (**135/135**), `CTraderFixLogonHostedService.cs` (**112/112**), `CTraderFixOptions.cs`, `LiveRuntimeStatus.cs`, `EnvFile.cs`, `LiveBrokerProbe\Program.cs` (**86/86**), `FakeMt5BrokerConnector` / `DemoSeeder` / `BrokerCatalogSeed`, `RiskEngine` allow-send tail, `BaselineScorer.CanPromoteToLive`. `grep` of `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `DemoSeeder` / `35=D` / `NewOrderSingle` / `DealerSend` / `OrderSend` / `AllowFixSend` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`. YoPips `config\app_config.cpp` + `app_config.h` (single `MT5_PASSWORD`, empty default). On-disk census `LIVE_GROUPS_AND_TRADERS.json` (counts + group **names** only; login rows not recopied). `.env` classified by **key presence / length / placeholder token**. This slot did **not** launch `dotnet`, did **not** re-attach LiveBrokerProbe, did **not** open FIX TLS. |

**Honesty rule:** fail-closed is **not** “the function exists.” It is: missing / placeholder / one-sided secrets **must not** start a dummy FakeMt5 census, **must not** start a half-broker live graph, and **must not** emit a live cTrader `35=D`. `StringComparison.Ordinal` is **not** case-insensitive. `HasRealPasswords == true` is **not** an order arm. Presence of a real-looking password string is **not** proof of a live Manager session. Slots **14 / 34 / 54 / 114** that pin `RealCopyEnabled` as **hardcoded `false`** are **stale vs current `DependencyInjection.cs` L39–42** (env-bound). A002 / A010 / C05 / C42 / R003 that say DI always registers `DemoBrokerFactory.CreateDefault()` are **stale vs current `D:\Prop\src`**. Decision table below is a **source-faithful replica** of the 3-clause `IsSecret` predicate, not a compiled test run (`RESULT.json` still **absent** under `_tmp_r14_gate` / `_tmp_r74_gate`).

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
| This type can emit live `35=D` / NewOrderSingle | **No** | **`SAFE_BY_ABSENCE`** on the copy hop |
| `RealCopyEnabled` on DI path | **env-bound** | `string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", OrdinalIgnoreCase)` |
| Lab `.env` `REAL_COPY_EXECUTION_ENABLED` | **`true`** | arms a **flag**, not a sender |
| Hosted FIX logon re-pins `RealCopyEnabled=false` | **No** | `CTraderFixLogonHostedService` only writes Quote/Trade status |
| After gate: ALL groups + ALL manager traders | **Yes** | `GroupRequestArray("*")` + `GetAccountsAsync(null)` |
| Prior live census (not re-attached) | **18 / 8460 / 1984** | Achiever 8/6512/1506 + Starwave 10/1948/478 |

**One-line:** `HasRealPasswords` **is** fail-closed for missing / exact-`<SECRET>` / `(a/c` / one-sided keys: DI throws and never substitutes FakeMt5. It is **not** a complete secret validator (`Ordinal` case hole, dummy words, no login/server check, factory bypass). After the gate passes, both native connectors are registered and ingest asks for **all** groups/traders. Copy still cannot spend capital because the product hop has **no NewOrderSingle encoder** (`SAFE_BY_ABSENCE`), even though `REAL_COPY_EXECUTION_ENABLED` is now env-bound and the lab `.env` key is `true`.

Slot verdict: **`PASS_FAIL_CLOSED_DI`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — this factory never sends; `CTraderFixSession` builds `35=A` only; `CopyTradingService.NewOrderSingleImplemented = false`; persisted `AllowFixSend` is forced `false`. Flag-true is **not** a ticket. Residual demo helper `CTraderFixDemoTestTrade.Build("D")` is **off-hop**, tools-only, and demo-gated.

---

## 1. Assigned type (current disk, 94 lines)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` — static manager-connection **factory** plus a secret-**presence** predicate. Not a Manager session. Not a FIX writer.

Members (measured, full-file read):

| Member | Role |
|---|---|
| `HasRealPasswords(IConfiguration)` | public dual-password **presence** gate |
| `CreateConnectorsFromEnvironment()` | process-env wrapper via private `EnvConfiguration` |
| `CreateConnectors(IConfiguration)` | constructs **exactly two** `NativeMt5BrokerConnector`s |
| `IsSecret(string?)` | **private** 3-clause heuristic; only `HasRealPasswords` calls it |
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

### 1.1 Factory always builds both brokers (ungated)

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // ...
            Password = config["MT5_PASSWORD"] ?? "",
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            // ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // ...
            Password = config["MT5_STARWAVEFX_PASSWORD"] ?? "",
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

Measured pins:

| Pin | Evidence |
|---|---|
| Exactly two connectors | `BrokerCodes.Achiever` (`"ACHIEVER"`) + `BrokerCodes.StarwaveFx` (`"STARWAVEFX"`) |
| Starwave proxy | **hard `false`** (L45). `MT5_STARWAVEFX_PROXY*` unread. |
| Achiever proxy | env `ACHIEVER_PROXY_*` only if `ACHIEVER_PROXY_ENABLED` parses true |
| FakeMt5 in this file | **0 references** |
| `CreateConnectorsFromEnvironment` (L17–18) | `new EnvConfiguration()` — **does not** call `HasRealPasswords` |
| Empty / unparseable login | `Login = 0`, `Server = ""`, default port **443** — **outside** the gate |

A `Login = 0` / empty-password connector **fails at `ConnectAsync`**, not by silently ingesting only the manager login or substituting FakeMt5.

---

## 2. Product DI is the only fail-closed caller

`D:\Prop\src\Infrastructure\DependencyInjection.cs` — **62** physical lines this pass (slot 154 wrote 61; current disk is 62).

```36:49:D:\Prop\src\Infrastructure\DependencyInjection.cs
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
```

| On `HasRealPasswords == false` | **throw** before `CreateConnectors` |
|---|---|
| Fake / `DemoBrokerFactory` substitution | **none** |
| In-memory DB fallback | happens **before** the password throw (L27–34). Placeholder DB still does **not** start a dummy broker graph — the throw aborts composition. |
| `RealCopyEnabled` bind | **after** the throw. A refused host never arms the flag. |
| Hosted services registered after a pass | `LiveIngestHostedService`, `CTraderFixLogonHostedService`, `CopyTradingHostedService` |

`AddTraderIntelligence` is the **only** product caller of `HasRealPasswords`. Hosts that use it:

| Host | Loads `D:\Prop\.env`? | If gate false |
|---|---|---|
| `apps/api/Program.cs` L15 | **Yes** — `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` | throw at `AddTraderIntelligence` |
| `apps/mt5-worker/Program.cs` L7 | **No** `EnvFile` (this-slot grep of `D:\Prop\apps` = API only) | throw unless process/machine env already holds both keys |
| `apps/fix-worker/Program.cs` L7 | **No** `EnvFile` | same |
| `tools/LiveBrokerProbe` | Yes (`EnvFile.FindAndLoad`) | **does not** call `HasRealPasswords` |

`apps/*/appsettings.json` contain **no** `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD` keys (this-slot grep = 0). `launchSettings.json` for mt5-worker sets only `DOTNET_ENVIRONMENT`. If secrets live only in `D:\Prop\.env` and a worker is started without inheriting process env, `HasRealPasswords` is `false` → **throw**. That is correct refuse, not a dummy fill.

Fail-closed sequence on `HasRealPasswords == false`:

1. Throw `InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.")`.
2. No `IMt5BrokerConnector`, no `IBrokerRegistry`, no hosted ingest, no FIX logon host, no copy host.
3. No FakeMt5 10001/10002/10003/99001 universe on the live graph.

---

## 3. Static `IsSecret` / `HasRealPasswords` truth table

`HasRealPasswords = IsSecret(MT5_PASSWORD) && IsSecret(MT5_STARWAVEFX_PASSWORD)`

Source-faithful replica (not executed this session). Synthetic tokens only.

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
- `D:\Prop\reports\swarm\20260818\_tmp_r74_gate\Program.cs` — exists (`Program.cs` + `.csproj`); still no `RESULT.json`

Product `D:\Prop\tests` grep `HasRealPasswords|IsSecret` = **0**. The gate is **untested in CI**.

A `replace_with_manager_password` style placeholder would pass `IsSecret` (no `<SECRET>`, no `(a/c`). DI would **start** two native connectors. `Connect` would then fail (wrong password). Ingest would **not** paint Fake 10001 (`LiveIngestHostedService` L70: “No dummy data will be substituted.”). That is fail-on-connect, not fail-on-registration.

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

Whitespace-only. A literal `<SECRET>` or `(a/c` token would **pass** this probe and then fail at Manager `Connect`. After connect it walks `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. JSON note (L78): “Passwords never written. Groups and manager logins only.”

---

## 5. Lab `.env` classification (no values)

File: `D:\Prop\.env` (exists). Classification by this-slot `grep` of **key names only**; values inspected solely for length + `<SECRET>` / `(a/c` tokens and then discarded.

| Key | Present | Length / form | `<SECRET>` Ordinal | `(a/c` Ordinal | `IsSecret` replica |
|---|---|---|---|---|---|
| `MT5_PASSWORD` | **Yes** | 8 | no | no | **true** |
| `MT5_STARWAVEFX_PASSWORD` | **Yes** | 11 | no | no | **true** |
| `MT5_LOGIN` | Yes | 4 (manager login id) | n/a | n/a | not gated |
| `MT5_STARWAVEFX_LOGIN` | Yes | 4 (manager login id) | n/a | n/a | not gated |
| `MT5_SERVER` | Yes | IPv4 | n/a | n/a | not gated |
| `MT5_STARWAVEFX_SERVER` | Yes | IPv4 | n/a | n/a | not gated |
| `REAL_COPY_EXECUTION_ENABLED` | Yes | boolean | — | — | value **`true`** (not a secret) |
| `FEATURE_COPY_TRADING_ENABLED` | Yes | boolean | — | — | value **`true`** (unused by `HasRealPasswords`; unused under `D:\Prop\src` this-slot grep = 0) |

Replica `HasRealPasswords` on this file: **`true`**. If the API process loads this file (it does, via `EnvFile.FindAndLoad` + `AddEnvironmentVariables`), DI **will not throw**. That is intended fail-**open after both slots look real**. E011’s “password slots are `<SECRET>` len 8” is **stale** for the two MT5 keys (Starwave length 11; Achiever length 8 but **not** the `<SECRET>` token).

`HasRealPasswords` itself never enumerates users. Completeness is the **next** layer, only reachable if the gate passed (or if a caller bypasses it via `CreateConnectors*`).

---

## 6. After the gate: ALL groups + ALL manager traders

`HasRealPasswords` cannot shrink the census. It only decides whether the host may construct the two native readers. Fetch is **flag-blind** — `RealCopyEnabled` is not consulted by the Manager walk.

### 6.1 Native walk

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` L144–187):

1. `GroupRequestArray("*")` then `arr.Next(i)`
2. Fallback if empty: `GroupTotal` + `GroupNext`

`GetAccountsCore(null)` (L189–214): walks **every** group from `GetGroupsCore`, then per group (`ReadAccountsForGroup` L216–271):

1. `UserRequestArray(gname)`
2. else `UserGetByGroup` (only on hard fail)
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. account snapshot: `UserAccountRequestArray` / `UserAccountGetByGroup`

This is **all groups + all logins this manager ACL can see**. Groups the manager cannot see are outside this login’s permission set — that is ACL, not a `HasRealPasswords` cap.

Connect: pump `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS`, then fallback `PUMP_MODE_NONE`. Fetch does not require pump (request APIs first). Achiever may `ProxySet PROXY_HTTP` when enabled. Starwave `ProxyEnabled=false` so `ApplyProxy` returns immediately.

### 6.2 Ingest / hosted path

`DealIngestionService.SyncCatalogAsync` (`DealIngestionService.cs` L38–52):

- `GetGroupsAsync` → `UpsertGroupsBatchAsync`
- `GetAccountsAsync(null)` → `UpsertAccountsBatchAsync`

`SyncBrokerAsync` re-runs that catalog, then bulk deals per group + `GetGroupPositionsAsync("*")`. **No** `Take(200)` on the catalog (residual `Take(200)` is `GET /api/trades` page only).

`LiveIngestHostedService` loops `registry.All()` (the two native connectors), Connect → catalog → deals → score `ListLoginsWithDealsAsync`. On catalog failure it logs **“No dummy data will be substituted.”**

API `POST /api/ops/resync` walks both `"ACHIEVER"` and `"STARWAVEFX"` the same way and scores `ListLoginsAsync` (all persisted logins, not the dummy set).

Residual (adjacent, **not** a `HasRealPasswords` bypass): `apps/mt5-worker/Worker.cs` L31 still **scores** `{10001,10002,10003,99001}` after live `SyncBrokerAsync` of **both** live codes. That leftover dummy **score set** does not shrink `GetAccountsAsync(null)` and does not register Fake. Hosted ingest scores `ListLoginsWithDealsAsync` instead.

### 6.3 On-disk live census (prior measure; not re-attached)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` — `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, `probe=LiveBrokerProbe`. Group **names** + counts only (trader login / balance / equity rows **not** recopied here).

| Broker | connected | groups | accounts | openPositions | elapsedMs |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | 8 | 6512 | 1506 | 7212.5885 |
| STARWAVEFX | true | 10 | 1948 | 478 | 6413.478 |
| **sum** | | **18** | **8460** | **1984** | |

Achiever groups (name / account count; this-slot re-sum):

| Group | accounts |
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

Starwave groups (this-slot re-sum):

| Group | accounts |
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

Empty groups are still **fetched** (`demo\yo-instant` 0; three Starwave real groups 0). That is ALL-groups, not “groups with accounts only.” Dummy logins `{10001,10002,10003,99001}` are **not** in that census.

---

## 7. Copy / cTrader cannot send live orders

`HasRealPasswords` has **zero** FIX / order symbols. Adjacent no-send pins (remeasured this pass):

| Pin | Measured now |
|---|---|
| `CTraderFixSession.BuildLogon` | body starts `(35, "A")` then 34/49/56/50/57/52/98/108/141/553/554. **One** `WriteAsync` of that Logon. `TcpClient`/`SslStream` disposed before return. |
| `(35, "D")` / `"35=D"` / `MsgType = "D"` in `CTraderFixSession` | **0** |
| `DealerSend` / `OrderSend` / `TradeRequest` under `D:\Prop\src` | **0** |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** (L16) |
| `CopyTradingService.VenueReconciled` | **`const false`** (L15) |
| Persist path | `RiskDecisionRecord.AllowFixSend = false` **hardcoded** (`CopyTradingService.cs` L192) even if `RiskEngine` would compute `allowSend` |
| Live-send `if` (L198) | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — last two are const false; branch would only stamp `LIVE_SEND_BLOCKED_UNIMPLEMENTED` |
| Else | `Status = "SHADOW_ONLY"` + optional in-memory shadow fill |
| `CopyTradingHostedService` | 20s loop → `GenerateShadowIntentsAsync`; log: “Live NewOrderSingle still blocked.” |
| `BaselineScorer.CanPromoteToLive` | **`=> false`** (`BaselineScorer.cs` L211) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false**; **not** bound from env (`CTrader__RealCopyExecutionEnabled` unused by DI) |
| `CTraderFixLogonHostedService` | FIX password missing / `<SECRET>` → skip logon; else `TryLogonAsync` QUOTE 5211 + TRADE 5212; logs “NewOrderSingle still unimplemented”; **does not** write `RealCopyEnabled` |
| `apps/fix-worker/Worker.cs` | reads nested `CTrader:RealCopyExecutionEnabled` default **false** (log only); stamps FIX rows **Disconnected**; “worker still refuses NewOrderSingle” |
| API `/api/settings` | exposes `runtime.RealCopyEnabled` (so **can be true** if env is true) |
| API `/api/copy/status` | `GetStatusAsync` — blockers include `SAFE_BY_ABSENCE` while NOS unimplemented |
| `FEATURE_COPY_TRADING_ENABLED` under `D:\Prop\src` | **0 hits**; `GetStatusAsync` hardcodes `FeatureCopyEnabled: true` |

`HasRealPasswords` passing with live Manager secrets still only arms **ingest + optional FIX Logon (`35=A`) + SHADOW copy intents**. It does **not** create a `35=D` builder. Capital cannot be lost by this gate succeeding.

### 7.1 Off-hop residual: demo helper can assemble MsgType D

`CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. Caller this-slot: **`tools/DemoFixTestTrade/Program.cs` only**. Gate (L43–59) refuses unless host starts with `demo-`, SenderCompID starts with `demo.`, and account is **not** `1369850`. Live copy hop **does not** call it. This slot did **not** run that tool.

Older reports that say “product `35=D` count = 0 across all `src/Fix.CTrader`” are **stale** for the **sibling helper**. They remain true for `CTraderFixSession` and for the **hosted copy hop**.

### 7.2 Flag residual (do not confuse with fail-closed)

Slots 14/34/54/114: “`RealCopyEnabled` hardcoded false.” **Stale vs `DependencyInjection.cs` L41.**

Current: DI copies `.env` `REAL_COPY_EXECUTION_ENABLED=true` onto `LiveRuntimeStatus.RealCopyEnabled`. `RiskEngine` would set `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Copy always passes `Reconciled = VenueReconciled = false`, so engine `allowSend` is **false**, and the persist line **overwrites to false anyway**. Flag-true is a **policy leftover**, not a send path.

`LiveRuntimeStatus.Snapshot` even when armed: “REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”

---

## 8. YoPips C++ is not this gate

`D:\Projects\YoPips\Backend\C++ Backend PropFirm`:

| Search | Result |
|---|---|
| `HasRealPasswords` / `LiveMt5Registration` | **0** (read of `config/app_config.cpp` + `app_config.h`; no dual-AND symbol) |
| `MT5_STARWAVEFX` under `config/` | **0** dual-broker password |
| `AppConfig::load` (`config/app_config.cpp` L150) | `cfg.mt5_password = get("MT5_PASSWORD", "");` — empty default, **no** refuse, **no** `<SECRET>` reject |
| `app_config.h` | single `std::string mt5_password;` — **not** required at struct default; JWT/DB host are the “Required” comments |

That process is the **prop-firm trading backend** (it has native trade execution). It is **not** registered by `AddTraderIntelligence` and is **not** the Prop → cTrader copy path. Dual-broker fail-closed exists only in C# `LiveMt5Registration`.

---

## 9. Fake / demo leftovers (not on the live graph)

| Artifact | On live DI/API path? |
|---|---|
| `FakeMt5BrokerConnector` + `DemoBrokerFactory.CreateDefault()` | **No** — 3+1 dummy logins 10001/10002/10003/99001 exist on disk only |
| `DemoSeeder` | Integration test (`tests/Integration/SeedingAndStoreTests.cs`) + `_tmp_*` harnesses; **not** called from `apps/*/Program.cs` (this-slot grep of `D:\Prop\apps` = **0**) |
| `BrokerCatalogSeed` | Yes — **broker + FIX Disconnected rows only**, no dummy traders |
| mt5-worker score set `{10001,10002,10003,99001}` | leftover **score** after live sync; not a Fake registration |
| API startup (`Program.cs` L152–157) | `EnsureCreated` + `BrokerCatalogSeed.EnsureAsync` only |

---

## 10. Residuals (honest)

1. **`StringComparison.Ordinal` case hole** — `<secret>`, `<Secret>`, `(A/C` pass `IsSecret`.
2. **Dummy words / single-char** pass (`dummy`, `x`).
3. **No login / server / port check** — a filled password with `Login = 0` still returns `true`.
4. **`CreateConnectors*` is ungated** — probe and any direct caller skip the DI throw.
5. **LiveBrokerProbe is whitespace-only** — weaker than `IsSecret`.
6. **Zero product tests** of `HasRealPasswords` / `IsSecret`. `_tmp_r14_gate` / `_tmp_r74_gate` have **no** `RESULT.json`.
7. **Workers do not load `.env`** — fail-closed throw if process env is empty (correct refuse; easy to misread as a startup bug).
8. **`REAL_COPY_EXECUTION_ENABLED=true` in lab `.env` is bound** — flag armed, sender still missing. Slots that say DI hard-false are stale.
9. **Demo helper `Build("D")` exists** — not on the copy hop; demo-gated; unused by API / workers / `CopyTradingService`.
10. **mt5-worker leftover dummy score set** — does not shrink the live census.

None of these residuals let the **product copy hop** emit a live `35=D`.

---

## 11. What this slot did **not** do

- Did **not** edit product source.
- Did **not** launch `dotnet`, LiveBrokerProbe, DemoFixTestTrade, or any FIX TLS session.
- Did **not** print password / proxy / FIX / connection-string values.
- Census numbers are from the on-disk JSON dated `2026-08-18T08:42:16Z`, **re-summed** from group `accounts` fields, **not** re-probed.
- Decision table is a **source replica**, not a compiled harness run.

---

## 12. Files read (absolute)

- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (caller + gate only)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (allow-send tail)
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (`CanPromoteToLive`)
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Properties\launchSettings.json`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + names)
- `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h`
- `D:\Prop\.env` (key names / lengths / placeholder tokens only)

---

## 13. Conclusion

`LiveMt5Registration.HasRealPasswords` **does** fail-closed the product host against missing / exact-placeholder / one-sided MT5 passwords: throw, no FakeMt5, no half-dummy graph. After it returns true, the factory registers **both** native managers and ingest fetches **all** groups/traders (`GroupRequestArray("*")` + `GetAccountsAsync(null)`; last measured **18 / 8460 / 1984**). Copy-to-cTrader remains **unarmed on the wire**. Residuals are validator weakness + untested CI + factory/probe bypass + **env-armed `RealCopyEnabled`** + an off-hop demo `35=D` helper — **not** a live-order path.

**PASS_FAIL_CLOSED_DI. Risk to capital: NONE (`SAFE_BY_ABSENCE`).**
