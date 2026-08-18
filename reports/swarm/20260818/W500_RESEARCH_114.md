# W500_RESEARCH_114 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **114** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_114 |
| Assigned | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report + index/log pins only. |
| Secrets printed | **None.** Password / proxy / FIX values never quoted. Key **names**, lengths, and placeholder-token classification only. |
| Method | Full `read_file` of `LiveMt5Registration.cs` (94/94), `DependencyInjection.cs` (59/59), host `Program.cs` trio, `LiveIngestHostedService`, `DealIngestionService`, `NativeMt5BrokerConnector` catalog cores, `CTraderFixSession`, `CTraderFixLogonHostedService`, `LiveRuntimeStatus`, `EnvFile`, `LiveBrokerProbe`, `FakeMt5BrokerConnector`/`DemoSeeder` (existence only). `grep` of `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `DemoSeeder` / `35=D` / `NewOrderSingle` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`. YoPips C++ `config\app_config.cpp` + `HasRealPasswords` grep (0). On-disk census `LIVE_GROUPS_AND_TRADERS.json` (counts + group names only). `.env` classified by **key presence / length / placeholder token**, values **not** copied. |

**Honesty rule:** fail-closed is **not** “the function exists.” It is: missing / placeholder / one-sided secrets **must not** start a dummy FakeMt5 census, **must not** start a half-broker live graph, and **must not** arm live cTrader send. `StringComparison.Ordinal` is **not** case-insensitive. `HasRealPasswords == true` is **not** an order arm. Presence of a real-looking password string is **not** proof of a live Manager session. Older slices that say DI always registers `DemoBrokerFactory.CreateDefault()` (A002 / A010 / C05 / C42 / R003) are **stale vs current `D:\Prop\src`**. This slot did **not** launch `dotnet` or re-attach LiveBrokerProbe; the decision table is a **source-faithful replica** of the 3-clause `IsSecret` predicate.

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
| `RealCopyEnabled` on DI path | **hardcoded `false`** | not env-bound |

**One-line:** `HasRealPasswords` **is** fail-closed for missing / exact-`<SECRET>` / `(a/c` / one-sided keys: DI throws and never substitutes FakeMt5. It is **not** a complete secret validator (`Ordinal` case hole, dummy words, no login/server check, factory bypass). After the gate passes, both native connectors are registered and ingest asks for **all** groups/traders (`GroupRequestArray("*")` + `GetAccountsAsync(null)`). Copy still cannot spend capital because no NewOrderSingle encoder exists and `RealCopyEnabled` is forced off.

Slot verdict: **`PASS_FAIL_CLOSED_DI`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — this factory never sends; DI pins `RealCopyEnabled = false`; `CTraderFixSession` builds `35=A` only.

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

What the gate **does not** read: `MT5_LOGIN`, `MT5_STARWAVEFX_LOGIN`, `MT5_SERVER`, `MT5_STARWAVEFX_SERVER`, ports, proxy keys, `CTRADER_FIX_PASSWORD`, `DATABASE_URL`, `REAL_COPY_EXECUTION_ENABLED`.

`CreateConnectors` still **constructs** both slots even if those other fields are empty / unparseable (`Login = 0`, `Server = ""`, default port 443). That is **outside** `HasRealPasswords`. A `Login = 0` connector then **fails at `ConnectAsync`**, not by silently ingesting only the manager login.

### 1.1 Factory always builds Native ×2 (no Fake)

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
            // MT5_STARWAVEFX_* ; ProxyEnabled = false hardcoded
            ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

Measured pins (this file, current disk):

| Slot | Construction |
|---|---|
| Achiever | `BrokerCodes.Achiever` (`"ACHIEVER"`); proxy from `ACHIEVER_PROXY_ENABLED` (must parse `true`) |
| StarwaveFX | `BrokerCodes.StarwaveFx` (`"STARWAVEFX"`); `ProxyEnabled = false` **literal** — env unread |
| Count | **exactly two** `NativeMt5BrokerConnector` instances |
| Fake | **0** references in this file |

`CreateConnectorsFromEnvironment()` (`L17–18`) wraps `new EnvConfiguration()` and **does not** call `HasRealPasswords`.

---

## 2. Product DI is the only fail-closed caller

`D:\Prop\src\Infrastructure\DependencyInjection.cs` (59/59 lines):

```35:46:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
        services.AddSingleton(runtime);

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

| On `HasRealPasswords == false` | **throw** before `CreateConnectors` |
|---|---|
| Exception type | `InvalidOperationException` |
| Exception message (exact) | `Real MT5 passwords are required. Dummy/fake broker data is disabled.` |
| Fake substitution | **None.** No `else` branch. No `DemoBrokerFactory`. |
| `FakeMt5BrokerConnector` / `DemoBrokerFactory` tokens in this file | **0** (grep) |
| `RealCopyEnabled` | **literal `false`**, not bound from env |

Independent of this gate: empty / `<SECRET>` `DATABASE_URL` still selects **InMemory** EF (`DependencyInjection.cs` L26–28). That is a persistence fallback, **not** a Fake-MT5 fallback.

`AddTraderIntelligence` is used by:

| Host | Calls `AddTraderIntelligence` | Loads `EnvFile.FindAndLoad()` before DI |
|---|---|---|
| `D:\Prop\apps\api\Program.cs` L14 | **Yes** | **Yes** L9, then `AddEnvironmentVariables()` L12 |
| `D:\Prop\apps\mt5-worker\Program.cs` L7 | **Yes** | **No** |
| `D:\Prop\apps\fix-worker\Program.cs` L7 | **Yes** | **No** |

Only API + LiveBrokerProbe call `EnvFile.FindAndLoad()`. `mt5-worker` / `fix-worker` see process / machine / `appsettings` only. `apps/*/appsettings.json` contain **no** `MT5_PASSWORD` keys (grep 0). If secrets live only in `D:\Prop\.env` and a worker is started without inheriting process env, `HasRealPasswords` is `false` → **throw** (fail-closed start). That is correct refuse, not a dummy fill.

API startup seed (`Program.cs` L149–154) is `EnsureCreatedAsync` + `BrokerCatalogSeed.EnsureAsync` only. Product `apps/**` grep `DemoSeeder` = **0**.

---

## 3. Decision table (source-equivalent of `IsSecret` ∧ `IsSecret`)

Predicate is three boolean clauses with no I/O. **Actual** = current code. **Strict fail-closed** = what a complete dummy-block should return.

| Case | `MT5_PASSWORD` | `MT5_STARWAVEFX_PASSWORD` | Actual `HasRealPasswords` | Strict fail-closed | Match? |
|---|---|---|---|---|---|
| both missing | `null` | `null` | **false** | false | yes |
| both empty | `""` | `""` | **false** | false | yes |
| both whitespace | `"  "` | `"\t"` | **false** | false | yes |
| Achiever only | non-placeholder | `""` | **false** | false | yes |
| Starwave only | `""` | non-placeholder | **false** | false | yes |
| both `<SECRET>` | `<SECRET>` | `<SECRET>` | **false** | false | yes |
| Achiever `<SECRET>`, Starwave ok | `<SECRET>` | non-placeholder | **false** | false | yes |
| Achiever ok, Starwave `<SECRET>` | non-placeholder | `<SECRET>` | **false** | false | yes |
| both contain `(a/c` | `pw (a/c 1)` | `pw (a/c 2)` | **false** | false | yes |
| token embedded | `x<SECRET>y` | `x<SECRET>y` | **false** | false | yes |
| both synthetic non-placeholder | `not-a-placeholder-token` | same | **true** | true | yes |
| lowercase token | `<secret>` | `<secret>` | **true** | **false** | **NO** |
| mixed-case token | `<Secret>` | `<Secret>` | **true** | **false** | **NO** |
| dummy words | `dummy` | `changeme` | **true** | **false** | **NO** |
| single character | `x` | `y` | **true** | **false** | **NO** |
| uppercase account comment | `pw (A/C 1)` | `pw (A/C 2)` | **true** | **false** | **NO** |

On `false` the product host **does not start**. On residual `true` the host starts **two native connectors**. Connect then fails if the string is not a real Manager password. Ingest will **not** paint Fake 10001 (`LiveIngestHostedService` L70: “No dummy data will be substituted.”). That is fail-on-connect, not fail-on-registration.

Pre-existing hermetic probe `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` encodes 15 synthetic cases + DI throw assertion. `RESULT.json` was **absent** at read time. Product `D:\Prop\tests` grep `HasRealPasswords|IsSecret` = **0**. The gate is **untested in CI**.

---

## 4. Callers — who is actually fail-closed

Grep `HasRealPasswords` / `CreateConnectors` / `CreateConnectorsFromEnvironment` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tools` + `D:\Prop\tests`:

| Site | Calls `HasRealPasswords`? | Calls `CreateConnectors*`? |
|---|---|---|
| `LiveMt5Registration.cs` | definition | definition |
| `DependencyInjection.AddTraderIntelligence` | **Yes** — throw | **Yes** — after the throw |
| `tools/LiveBrokerProbe/Program.cs` | **No** | **Yes** `CreateConnectorsFromEnvironment()` |
| `D:\Prop\tests` | **0** | **0** |

### 4.1 LiveBrokerProbe is weaker than DI

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

Whitespace-only. Exact `<SECRET>` or `(a/c` would **pass** the probe and then fail at `ConnectAsync`. Probe is a lab tool, not the product host. It still cannot emit `35=D`.

---

## 5. `.env` classification (names / lengths only — values never quoted)

`D:\Prop\.env` exists. This slot classified **keys**, not values:

| Key | Present | Length | `IsNullOrWhiteSpace` | Contains `<SECRET>` (`Ordinal`) | Contains `(a/c` (`Ordinal`) | Replica `IsSecret` |
|---|---|---:|---|---|---|---|
| `MT5_PASSWORD` | yes | 8 | no | no | no | **true** |
| `MT5_STARWAVEFX_PASSWORD` | yes | 11 | no | no | no | **true** |

Replica `HasRealPasswords` on this file: **`true`**. If the API process loads this file (it does, via `EnvFile.FindAndLoad` + `AddEnvironmentVariables`), DI **will not throw**. That is intended fail-**open after both slots look real**. E011’s “password slots are `<SECRET>` len 8” is **stale** for the two MT5 keys (Starwave length 11; Achiever length 8 but **not** the `<SECRET>` token).

Also present (names only, used by `CreateConnectors` not by the gate): `MT5_SERVER`, `MT5_LOGIN`, `MT5_STARWAVEFX_SERVER`, `MT5_STARWAVEFX_LOGIN`.

---

## 6. After the gate: ALL groups + ALL manager traders

`HasRealPasswords` itself never enumerates users. Completeness is the **next** layer, only reachable if the gate passed.

### 6.1 Ingest contract

`DealIngestionService.SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L45–49):

- `GetGroupsAsync` — no mask, no `Take(`
- `GetAccountsAsync(null, ct)` — `null` = **every** group

`SyncBrokerAsync` repeats `GetAccountsAsync(null)` then walks **all** groups (`GetGroupDealsAsync`) or **all** accounts. Grep `Take(` / `MaxAccounts` in this file: **0**.

### 6.2 Native request path

`NativeMt5BrokerConnector.GetGroupsCore` (`L155`): `GroupRequestArray("*", arr)` first; pump `GroupTotal`/`GroupNext` only if the request array is empty.

`GetAccountsCore` (`L189–213`): if `group` is null/whitespace, iterates **every** name from `GetGroupsCore()`, then `UserRequestArray` first (`L223`), with `UserGetByGroup` / `UserLogins`+`UserRequestByLogins` fallbacks. Dedupes by login. **No account-count knob.**

### 6.3 Hosted ingest

`LiveIngestHostedService` walks `registry.All()` (the two native connectors). Catalog via `SyncCatalogAsync`. Scoring uses `ListLoginsWithDealsAsync` (deals-only), **not** a book cap. Fail path logs “No dummy data will be substituted.”

Residual (adjacent, **not** a `HasRealPasswords` bypass): `apps/mt5-worker/Worker.cs` L31 still **scores** `{10001,10002,10003,99001}` after live `SyncBrokerAsync`. That leftover dummy **score set** does not shrink `GetAccountsAsync(null)` and does not register Fake.

### 6.4 On-disk live census (not re-probed this slot)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` — `probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`. Probe calls `CreateConnectorsFromEnvironment()` → same Native pair. Dummy logins `10001` / `10002` / `10003` / `99001` as **login numbers**: **0 hits**.

| Broker | Connected | Groups | Accounts | Open positions | Hop |
|---|---|---:|---:|---:|---|
| ACHIEVER | true | 8 | 6512 | 1506 | HTTP proxy (env) |
| STARWAVEFX | true | 10 | 1948 | 478 | direct (`ProxyEnabled=false`) |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (names + account counts; re-summed 2+179+4+5+4+6295+0+23 = **6512**):

`contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

StarwaveFX groups (re-summed 11+4+170+1735+22+0+0+4+0+2 = **1948**):

`Starwave\cent\FX1\grp1` 11, `...\grp2` 4, `Starwave\demo\FX2\grp1` 170, `...\grp2` 1735, `Starwave\real\FX3\grp1` 22, `...\grp2` 0, `...\grp3` 0, `...\grp4` 4, `...\grp5` 0, `Starwave\real\FX3\LP` 2.

This is **all groups + all logins this manager ACL can see**. Groups the manager cannot see are outside this login’s permission set. `HasRealPasswords` cannot shrink that census; it only decides whether the host is allowed to construct the two native readers.

---

## 7. Copy to cTrader must not send live orders (no loss)

`HasRealPasswords` has **zero** FIX / order symbols. Adjacent no-send pins (remeasured this pass):

| Pin | Measured |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` at DI | **literal `false`** (`DependencyInjection.cs` L41) |
| `CTraderFixLogonHostedService` after logon | **forces `_runtime.RealCopyEnabled = false`** (L68) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (`CTraderFixOptions.cs` L35) |
| `CTraderFixSession.BuildLogon` | only wire write is `(35, "A")` (`CTraderFixSession.cs` L96) |
| `35=D` / `NewOrderSingle` encoder under `src/Fix.CTrader` | **0 hits** (grep) |
| `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` | **`false`** (unbound by DI) |
| `apps/fix-worker/Worker.cs` | stamps TRADE `Disconnected` + “NewOrderSingle remains off”; even if `CTrader:RealCopyExecutionEnabled` is true it **logs a warning and still does not send** |
| API `/api/settings` | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (forced false); `FEATURE_COPY_TRADING_ENABLED` = **literal `false`** |
| `RiskEngine.Evaluate` | exists; **0 product callers** on a live send hop (vocabulary stub) |

`HasRealPasswords` passing with live Manager secrets still only arms **ingest + optional FIX Logon (`35=A`)**. It does **not** flip `RealCopyEnabled`. There is no `35=D` builder. Capital cannot be lost by this gate succeeding.

---

## 8. YoPips C++ is not this gate

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` grep `HasRealPasswords` / `LiveMt5Registration` / `STARWAVE` (config): **0** dual-broker AND.

`config/app_config.cpp` L150: `cfg.mt5_password = get("MT5_PASSWORD", "");` — empty default.

Startup refuse (`L307–353`) requires JWT, DB, Postmark, and for `MT5_MODE=local` **`MT5_SERVER` + `MT5_LOGIN != 0`**. **`mt5_password` empty is not fatal.** That is fail-open at registration for the password itself (connect would fail later). No `<SECRET>` / `(a/c` reject. Single-broker only.

That process **does** implement `DealerSend` (`trade_execution_service.cpp` / `mt5_manager.cpp`). It is the **prop-firm trading backend**. It is **not** registered by `AddTraderIntelligence` and is **not** the Prop → cTrader copy path. Slot 114 does not treat C++ `DealerSend` as a Prop copy-to-cTrader send.

---

## 9. Residuals (honest, not a live-order path)

1. **`StringComparison.Ordinal` case hole** — `<secret>`, `<Secret>`, `(A/C` pass `IsSecret`.
2. **Dummy words / one-char / SDK template strings** pass if both slots are filled.
3. **No login / server / port check** — `Login = 0` still constructs; fails at `Connect`.
4. **`CreateConnectors*` is ungated** — any direct caller skips the throw.
5. **LiveBrokerProbe** uses whitespace-only, not `IsSecret`.
6. **Workers do not load `.env`** — fail-closed start if process env is empty (correct refuse).
7. **Zero product tests** of `HasRealPasswords` / `IsSecret`.
8. **`DemoSeeder` + `FakeMt5BrokerConnector` remain on disk** for integration tests (`tests/Integration/SeedingAndStoreTests.cs`). Product hosts do not call them.
9. **`mt5-worker/Worker.cs` four-login scorer leftover** — not a Fake registration, not an order send.
10. **This slot did not execute** the hermetic `_tmp_r14_gate` harness or re-run LiveBrokerProbe.

None of these residuals can place, flatten, or size a destination order.

---

## 10. Files read (absolute)

- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (existence / not on host path)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
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
- `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` (unexecuted harness)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`

---

## 11. Conclusion

`LiveMt5Registration.HasRealPasswords` **is fail-closed on the product DI path**: missing / whitespace / exact `<SECRET>` / `(a/c` on **either** Achiever or StarwaveFX password → host **does not start** → **no** Fake connector → **no** dummy 10001 universe on the live graph.

After it returns true, the factory registers **both** native managers and ingest fetches **all** groups/traders (`GetAccountsAsync(null)` / `GroupRequestArray("*")`). Prior measured census: **8+10 = 18 groups, 6512+1948 = 8460 accounts**.

Copy-to-cTrader remains **unarmed** (`RealCopyEnabled` forced false; no `35=D` encoder). Residuals are validator weakness + untested CI + factory/probe bypass — **not** a live-order path.

**Verdict: `PASS_FAIL_CLOSED_DI`. Risk to capital: `NONE`.**
