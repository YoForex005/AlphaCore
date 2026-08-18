# W500_RESEARCH_54 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **54** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_54 |
| Assigned | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report + helper script only. |
| Secrets printed | **None.** Password / proxy / FIX values never quoted. Key **names** only. |
| Method | Full `read_file` of `LiveMt5Registration.cs` (94/94 lines) + `DependencyInjection.cs`; `grep` of `HasRealPasswords` / `IsSecret` / `35=D` / `FakeMt5` / `EnvFile` / `AddTraderIntelligence` on `D:\Prop` and YoPips `config\app_config.*`; supporting reads of ingest, native connector, FIX logon, hosts, LiveBrokerProbe, C++ `AppConfig::load`. |

**Honesty rule:** fail-closed is **not** “the function exists.” It is: missing / placeholder / one-sided secrets **must not** start a dummy FakeMt5 census, **must not** start a half-broker live graph, and **must not** arm live cTrader send. `StringComparison.Ordinal` is **not** case-insensitive. Older slices that say this file is 53 lines (`W500_SLICE_52`) or that `RealCopyEnabled` is bound from `REAL_COPY_EXECUTION_ENABLED` (`W500_SLICE_53`) are **stale vs current disk**. This session could not execute `dotnet` (no shell); branch table below is a **source-faithful replica** of `IsSecret`, not a compiled test run.

---

## 0. Verdict (binding)

| Claim | Measured | Class |
|---|---|---|
| `HasRealPasswords` is dual-AND of Achiever + Starwave password keys | **Yes** | `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD` |
| Empty / whitespace / missing either key → `false` | **Yes** | fail-closed |
| Exact `<SECRET>` (Ordinal) → `false` | **Yes** | fail-closed |
| Substring `(a/c` (Ordinal) → `false` | **Yes** | fail-closed (blocks `.env` comment paint) |
| One broker real + other dummy → `false` | **Yes** | fail-closed; graph cannot start half-live |
| `AddTraderIntelligence` throws before `CreateConnectors` when `false` | **Yes** | `InvalidOperationException` exact message below |
| Dummy `FakeMt5BrokerConnector` registered on that throw path | **No** | host never builds; no 10001 tape |
| Product unit/integration tests of `HasRealPasswords` | **0 hits** | untested in `D:\Prop\tests` |
| `<secret>` / `<Secret>` / `(A/C` / `dummy` / `x` treated as real | **Yes (true)** | **fail-open residual** |
| `CreateConnectors` / `CreateConnectorsFromEnvironment` re-check the gate | **No** | factory is ungated if called directly |
| LiveBrokerProbe uses the same `IsSecret` | **No** | whitespace-only check |
| C++ `AppConfig::load` has an equivalent dual-password refuse | **No** | single `MT5_PASSWORD`; password **not** required at startup |
| This type can emit live `35=D` / NewOrderSingle | **No** | **`SAFE_BY_ABSENCE`** |
| `RealCopyEnabled` on DI path | **hardcoded `false`** | not env-bound |

**One-line:** `HasRealPasswords` **is** fail-closed for missing / exact-`<SECRET>` / `(a/c` / one-sided keys: DI throws and never substitutes FakeMt5. It is **not** a complete secret validator (`Ordinal` case hole, dummy words, no login/server check, factory bypass). After the gate passes, both native connectors are registered and ingest asks for **all** groups/traders. Copy still cannot spend capital because no NewOrderSingle encoder exists and `RealCopyEnabled` is forced off.

Slot verdict: **`PASS_WITH_RESIDUALS`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — this factory never sends; DI pins `RealCopyEnabled = false`; `CTraderFixSession` builds `35=A` only.

---

## 1. Assigned type (current disk, 94 lines)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` — static factory, **not** a Manager session.

Members (measured, full-file read):

| Member | Role |
|---|---|
| `HasRealPasswords(IConfiguration)` | dual password **presence** gate |
| `CreateConnectorsFromEnvironment()` | process-env wrapper via private `EnvConfiguration` |
| `CreateConnectors(IConfiguration)` | constructs **exactly two** `NativeMt5BrokerConnector`s |
| `IsSecret(string?)` | private predicate |
| `EnvConfiguration` | `IConfiguration` that reads `Environment.GetEnvironmentVariable` only |

It does **not** call `Connect`, `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `DealRequest*`, `PositionRequest*`, or any FIX writer. It cannot subset the trader universe. Prior “53/53 lines” counts are stale: `CreateConnectorsFromEnvironment` + `EnvConfiguration` were added after those slices.

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
            // ACHIEVER_PROXY_HOST / PORT / USERNAME / PASSWORD bound here (values not quoted)
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

`BrokerCodes` is two names only (`ACHIEVER`, `STARWAVEFX`). No third live source-MT5 slot.

---

## 2. DI composition: throw is the fail-closed choke

`AddTraderIntelligence` is the only product caller of `HasRealPasswords`. Hosts that use it:

| Host | Calls `AddTraderIntelligence` | Loads `EnvFile.FindAndLoad()` before DI |
|---|---|---|
| `D:\Prop\apps\api\Program.cs` L14 | **Yes** (L9 env load, then L14) | **Yes** |
| `D:\Prop\apps\mt5-worker\Program.cs` L7 | **Yes** | **No** |
| `D:\Prop\apps\fix-worker\Program.cs` L7 | **Yes** | **No** |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | **No** | **Yes**, then weaker check |

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

Fail-closed consequences when the throw fires:

1. `CreateConnectors` is **not** reached.
2. `FakeMt5BrokerConnector` / `DemoBrokerFactory` are **not** registered (they are not on this path at all).
3. `LiveIngestHostedService` / `CTraderFixLogonHostedService` never start.
4. API never reaches `BrokerCatalogSeed.EnsureAsync` (that seed is after `app.Build()`).
5. No dummy 10001/10002/10003/99001 tape is painted as “live census.”

That is the intended “dummy disabled” policy. It is **stronger** than C++ `AppConfig::load` (see §7).

In-memory DB fallback is **independent** of the password throw (`DATABASE_URL` empty/`<SECRET>` → `UseInMemoryDatabase("trader-intelligence-live")`). Real passwords + placeholder DB still start the host against InMemory. That is a **persist** hole, not a FakeMt5 hole, and not a send hole.

`RealCopyEnabled` is **not** read from `REAL_COPY_EXECUTION_ENABLED` on this path. `W500_SLICE_53` quoting `string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` is **false on current disk**.

---

## 3. Branch table (source-faithful replica of `IsSecret`)

Predicate (same operators as L52–55):

`IsSecret(v) = !IsNullOrWhiteSpace(v) && !OrdinalContains(v, "<SECRET>") && !OrdinalContains(v, "(a/c")`

`HasRealPasswords = IsSecret(MT5_PASSWORD) && IsSecret(MT5_STARWAVEFX_PASSWORD)`

Synthetic tokens only. No operator secrets.

| Case | Achiever token | Starwave token | Result | Fail-closed? |
|---|---|---|---|---|
| both missing / null | *(absent)* | *(absent)* | `false` | **Yes** |
| both empty | `""` | `""` | `false` | **Yes** |
| both whitespace | `"  "` | `"\t"` | `false` | **Yes** |
| achiever only | non-placeholder | `""` | `false` | **Yes** (AND) |
| starwave only | `""` | non-placeholder | `false` | **Yes** (AND) |
| both exact `<SECRET>` | `<SECRET>` | `<SECRET>` | `false` | **Yes** |
| mixed `<SECRET>` + real-looking | `<SECRET>` | token | `false` | **Yes** |
| both `(a/c` comment | `pw (a/c 1)` | `pw (a/c 2)` | `false` | **Yes** |
| both synthetic non-placeholder | `not-a-placeholder-token` | same family | `true` | intended allow |
| lowercase `<secret>` | `<secret>` | `<secret>` | **`true`** | **No** — Ordinal hole |
| mixed-case `<Secret>` | `<Secret>` | `<Secret>` | **`true`** | **No** |
| dummy words | `dummy` | `changeme` | **`true`** | **No** vs dummy-word policy |
| single char | `x` | `y` | **`true`** | **No** vs strength |
| uppercase `(A/C` | `pw (A/C 1)` | `pw (A/C 2)` | **`true`** | **No** — case hole |

On-disk replica of this table (not executed this session): `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` (15 cases + DI throw assertion). Product `D:\Prop\tests` grep `HasRealPasswords|IsSecret` = **0**. The gate is **untested in CI**.

---

## 4. Bypass / weaker gates (residuals)

### 4.1 Factory is public and ungated

`CreateConnectors` and `CreateConnectorsFromEnvironment` do **not** call `HasRealPasswords`. Empty passwords become `Password = ""` on `NativeMt5Options`. Connect then fails (auth), unless a caller skips Connect.

### 4.2 LiveBrokerProbe is weaker than DI

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

Whitespace-only. A literal `<SECRET>` or `pw (a/c …)` would **pass** this probe and call `CreateConnectorsFromEnvironment()`. That is a **tool** bypass of `IsSecret`, not a product-host bypass. Probe still cannot send `35=D`.

### 4.3 Workers do not load `.env`

Only API + LiveBrokerProbe call `EnvFile.FindAndLoad()`. `mt5-worker` / `fix-worker` see process / machine / `appsettings` only. `apps/*/appsettings.json` contain **no** `MT5_PASSWORD` keys. If secrets live only in `D:\Prop\.env` and the worker is started without inheriting process env, `HasRealPasswords` is `false` → **throw** (fail-closed start). That is correct refuse, not a dummy fill.

### 4.4 `DemoSeeder` still on disk

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still calls `DemoBrokerFactory.CreateDefault()` (FakeMt5 10001–10003 / 99001). Product `Program.cs` files do **not** call it (remeasured: API seeds `BrokerCatalogSeed.EnsureAsync` only). Tests still do (`tests/Integration/SeedingAndStoreTests.cs`). If someone **removes** the DI throw later, Fake tape is one call away. Residual **policy** risk, not current host behavior.

### 4.5 `apps/mt5-worker/Worker.cs` leftover scorer

After `SyncBrokerAsync` for **both** broker codes (full catalog), the worker still rebuilds only `{10001,10002,10003,99001}`. That does **not** shrink `GetAccountsAsync(null)`. It is a leftover dummy **score set**, not a Fake registration, and not a `HasRealPasswords` defect. Hosted ingest (`LiveIngestHostedService`) scores `ListLoginsWithDealsAsync` instead.

---

## 5. After the gate: ALL groups + ALL manager traders

`HasRealPasswords` itself never enumerates users. Completeness is the **next** layer, only reachable if the gate passed.

Ingest catalog (no group mask, no `Take(`):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` → every group from `GetGroupsCore` (`GroupRequestArray("*")`, then `GroupTotal`/`GroupNext` if empty) → `ReadAccountsForGroup` (`UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`). Positions: `GetGroupPositionsAsync("*")`. Deals: `DealRequestByGroup` per group. `Take(200)` is **gone** from ingest (`src` `Take(` hits: FIX checksum `Take` + dashboard `Take(20)` only).

Live ingest on catalog failure: **no dummy substitution**:

```70:70:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    _log.LogError(ex, "{Broker} catalog failed. No dummy data will be substituted.", connector.BrokerCode);
```

One broker can fail Connect while the other proceeds (`st.Connected` skip). That is **per-broker fail-closed ingest**, not Fake fill. The **startup** gate still required **both** passwords to look real, so the failed slot is a **connect** miss, not a dummy password slot.

Measured live census already on disk (`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`, utc `2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, passwords not written):

| Broker | connected | groups | accounts | open positions |
|---|---|---:|---:|---:|
| ACHIEVER | true | 8 | 6512 | 1506 |
| STARWAVEFX | true | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

That probe used `CreateConnectorsFromEnvironment()` + `GetAccountsAsync(null)` — the same two factory slots this gate unlocks.

---

## 6. Copy to cTrader: still cannot send (no loss)

`HasRealPasswords` has **zero** FIX / order symbols. Adjacent no-send pins (remeasured this pass):

| Surface | Evidence |
|---|---|
| DI | `RealCopyEnabled = false` hardcoded; comment “do not arm a flag that cannot be honored safely” |
| FIX hosted service | `_runtime.RealCopyEnabled = false` after logon; log “NewOrderSingle still disabled” |
| `CTraderFixSession` | `BuildLogon` fields include `(35, "A")` only; **no** `(35, "D")` |
| `src` grep `35=D` / `NewOrderSingle` send | comments, flags, FSM `MayRetryNewOrderSingle`, seeder LastError — **no encoder** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false`; **not** bound in `AddTraderIntelligence` |
| API `/api/settings` | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` = **false** |
| `RiskEngine.AllowFixSend` | requires `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy`; **not registered** as a sender |
| `ShadowCopyEngine` | in-process simulate only |
| fix-worker `Worker` | stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."`; even if `CTrader:RealCopyExecutionEnabled` is true it **still refuses** |

cTrader FIX password is a **separate** fail-closed (logon skip, not this function):

```33:38:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var password = _config["CTRADER_FIX_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password) || password.Contains("<SECRET>", StringComparison.Ordinal))
        {
            _log.LogWarning("cTrader FIX password missing. QUOTE/TRADE logon skipped.");
            return;
        }
```

Same Ordinal `<SECRET>` hole as `IsSecret`. Worst case if FIX password is present: TLS **logon** `35=A` on 5211/5212. Still no `35=D`. Prior live pin: QUOTE+TRADE LoggedOn, NewOrderSingle off (`CREDENTIALS_AND_COPY_STATUS.md`).

---

## 7. C++ YoPips backend (supporting; not this gate)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `HasRealPasswords` / `LiveMt5Registration`.

`AppConfig::load` (`config\app_config.cpp`):

- Reads **one** `MT5_PASSWORD` (L150), default `""`.
- Local-mode refuse: `MT5_SERVER` empty or `MT5_LOGIN == 0` (L332–339). **`mt5_password` is not in that fatal list.**
- Production checks `MT5_PASSWORD_ENCRYPTION_KEY` length, not the manager password.

Standalone `tests\mt5_group_probe.cpp` `hasLocalConfig` **does** require `!mt5_password.empty()` (plus server + login ≠ 0) and exits `2` with `ERROR: missing_manager_credentials`. That is the C++ **probe** fail-closed, single-broker, empty-string only — no `<SECRET>` / `(a/c` scan, no Starwave second key.

Do not treat C++ startup as the Prop C# dual-broker gate. C++ `IS_MT5_PROXY_*` names are **unread** by `LiveMt5Registration` (Achiever uses `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled = false`).

---

## 8. What this slot does **not** claim

- Did **not** re-run LiveBrokerProbe or `dotnet` `HasRealPasswords` cases this session (no shell). Census numbers are from the on-disk JSON dated `2026-08-18T08:42:16Z`.
- Did **not** print or classify live secret **values**. `D:\Prop\.env` exists (prior reports); values unused here.
- Did **not** prove Manager ACL completeness beyond “request APIs + `group: null`.” Empty groups (Achiever 1 group with 0 accounts in the JSON; Starwave 3×0) are **manager-visible empty groups**, not a password-gate cut.
- Did **not** greenwash “fully fail-closed secret hygiene.” Residuals in §3–§4 remain.

---

## 9. Residual list (honest)

1. `StringComparison.Ordinal` → `<secret>`, `<Secret>`, `(A/C` pass.
2. `dummy` / `changeme` / single-char pass.
3. Login / server / port not part of the gate (`Login = 0` is connect-fail later).
4. Public factory bypass; LiveBrokerProbe whitespace-only.
5. Zero product tests of `HasRealPasswords`.
6. `DemoSeeder` + FakeMt5 still in tree (tests / unused host path).
7. C++ `AppConfig::load` will start local mode with empty `MT5_PASSWORD` (different process).
8. FIX logon uses the same weak `<SECRET>` Ordinal check (logon only; still no send).

None of these emit `35=D`.

---

## 10. Files read (absolute)

- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + broker totals)
- `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs`

---

## 11. Slot close

`HasRealPasswords` **does** fail-closed the product host against missing / exact-placeholder / one-sided MT5 passwords: throw, no FakeMt5, no half-dummy graph. After it returns true, the factory registers **both** native managers and ingest fetches **all** groups/traders (`GetAccountsAsync(null)`). Copy-to-cTrader remains **unarmed**. Residuals are validator weakness + untested CI + factory bypass — **not** a live-order path.

**DONE for slot 54.** Reviewer should treat `PASS_WITH_RESIDUALS` as a refuse to greenwash “bulletproof secret gate,” not as a capital-risk FAIL.
