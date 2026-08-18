# W500_RESEARCH_94 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **94** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_94 |
| Topic | Check `LiveMt5Registration.HasRealPasswords` fail-closed |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** Password / proxy / FIX values never quoted. Key **names**, lengths (from prior classified pin), and placeholder tokens only. |
| Method | Full `read_file` of `LiveMt5Registration.cs` (94/94) + `DependencyInjection.cs` (59) + hosts + probe + ingest + native connector + FIX session + C++ `AppConfig::load`. Grep `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `35=D` / `NewOrderSingle` / `OrderSend` / `DealerSend` / `TradeRequest` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`, and YoPips C++ `config` + `src`. |

**Honesty rule:** fail-closed is not “the function exists.” It is: missing / placeholder / one-sided secrets must not start a FakeMt5 10001 census, must not start a half-broker live graph, and must not arm live cTrader send. `StringComparison.Ordinal` is **not** case-insensitive. `HasRealPasswords == true` is **not** an order arm. `PositionCreateArray` is an SDK **read** factory, not a send. C++ `DealerSend` exists in a **different process** and is not this gate. This slot did **not** launch `dotnet` or LiveBrokerProbe (no shell). Branch table is a **source-faithful replica** of `IsSecret`. Census numbers are from on-disk JSON dated `2026-08-18T08:42:16.8519545+00:00`. `_tmp_r14_gate\RESULT.json` and `_tmp_r54_gate\HASHES_AND_REPLICA.json` were **absent** at read time.

Stale-vs-disk pins (do not reuse): A010 / C42 / D04 / D23 claiming API still registers `DemoBrokerFactory`; W500_SLICE_52 “53 lines”; W500_SLICE_53 `REAL_COPY_EXECUTION_ENABLED` bound into `RealCopyEnabled`. Current disk: 94-line factory, DI throw, `RealCopyEnabled = false` literal.

---

## 0. Verdict (binding)

| Claim | Measured | Class |
|---|---|---|
| `HasRealPasswords` is dual-AND of Achiever + Starwave password keys | **Yes** | `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD` |
| Empty / whitespace / missing either key → `false` | **Yes** | fail-closed |
| Exact `<SECRET>` (`StringComparison.Ordinal`) → `false` | **Yes** | fail-closed |
| Substring `(a/c` (`Ordinal`) → `false` | **Yes** | fail-closed (blocks `.env` comment paint) |
| One broker real + other dummy → `false` | **Yes** | AND; graph cannot start half-live |
| `AddTraderIntelligence` throws **before** `CreateConnectors` when `false` | **Yes** | exact message in §2 |
| Dummy `FakeMt5BrokerConnector` registered on that throw path | **No** | host never builds; no 10001 tape |
| After gate: both native managers constructed | **Yes** | `ACHIEVER` + `STARWAVEFX` only |
| After gate: catalog asks for **all** groups + **all** manager traders | **Yes** | `GroupRequestArray("*")` + `GetAccountsAsync(null)` |
| Product unit/integration tests of `HasRealPasswords` | **0 hits** under `D:\Prop\tests` | untested in CI |
| `<secret>` / `<Secret>` / `(A/C` / `dummy` / `x` treated as real | **Yes (`true`)** | **fail-open residual** |
| `CreateConnectors` / `CreateConnectorsFromEnvironment` re-check the gate | **No** | public factory is ungated |
| LiveBrokerProbe uses the same `IsSecret` | **No** | whitespace-only |
| C++ `AppConfig::load` has an equivalent dual-password refuse | **No** | single `MT5_PASSWORD`; password **not** in fatal list |
| `IMt5BrokerConnector` can place an order | **No** | read-only contract (slot-94 pin) |
| Prop C# `35=D` / `NewOrderSingle` encoder | **No** | **`SAFE_BY_ABSENCE`** (`src` grep `35=D` = **0**) |
| Prop C# `OrderSend` / `DealerSend` / `TradeRequest` | **No** | `src` grep = **0** |
| `RealCopyEnabled` on DI path | **hardcoded `false`** | not env-bound |
| C++ YoPips `DealerSend` | **Exists** | different process; not called from Prop DI |

**One-line:** `HasRealPasswords` **is** fail-closed for missing / exact-`<SECRET>` / `(a/c` / one-sided keys: DI throws and never substitutes FakeMt5. It is **not** a complete secret validator (Ordinal case hole, dummy words, no login/server check, factory bypass, 0 tests). After the gate passes, both native connectors are registered and ingest asks for **all** groups/traders. Copy still cannot spend capital from this process because the registered connector is read-only, no NewOrderSingle encoder exists, and `RealCopyEnabled` is forced off.

Slot verdict: **`PASS_WITH_RESIDUALS`**.

Risk to capital (this process): **`NONE` (`SAFE_BY_ABSENCE`)**.

---

## 1. Assigned type (current disk, 94 lines)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` — static factory, **not** a Manager session.

Members (full-file read, lines 1–94):

| Member | Lines | Role |
|---|---|---|
| `HasRealPasswords(IConfiguration)` | 10–15 | dual password **presence** gate |
| `CreateConnectorsFromEnvironment()` | 17–18 | process-env wrapper via private `EnvConfiguration` |
| `CreateConnectors(IConfiguration)` | 20–50 | constructs **exactly two** `NativeMt5BrokerConnector`s |
| `IsSecret(string?)` | 52–55 | private predicate |
| `EnvConfiguration` + nested no-ops | 57–93 | `IConfiguration` that reads `Environment.GetEnvironmentVariable` only |

It does **not** call `Connect`, `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `DealRequest*`, `PositionRequest*`, `DealerSend`, or any FIX writer. It cannot subset the trader universe.

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

What the gate **does not** read: `MT5_LOGIN`, `MT5_STARWAVEFX_LOGIN`, `MT5_SERVER`, `MT5_STARWAVEFX_SERVER`, ports, `ACHIEVER_PROXY_*`, `CTRADER_FIX_PASSWORD`, `DATABASE_URL`, `REAL_COPY_EXECUTION_ENABLED`, `FeatureFlags:LiveCopyEnabled`.

Missing `IConfiguration` key → `null` → `IsNullOrWhiteSpace` → `false`. That is fail-closed for absent keys.

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options { /* ACHIEVER + ACHIEVER_PROXY_* */ });
        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // MT5_STARWAVEFX_* bound here (values not quoted)
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });
        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

`BrokerCodes` is two names only (`"ACHIEVER"`, `"STARWAVEFX"`). No third live source-MT5 slot.

`CreateConnectors` still **constructs** both slots even if login/server fail parse (`Login = 0`, `Server = ""`, default port 443). That is **outside** `HasRealPasswords`. A `Login = 0` connector then **fails at `ConnectAsync`**, not by silently ingesting only the manager login.

`CreateConnectorsFromEnvironment()` (`L17–18`) wraps `new EnvConfiguration()` and **does not** call `HasRealPasswords`.

---

## 2. DI composition: throw is the fail-closed choke

`AddTraderIntelligence` is the **only** product caller of `HasRealPasswords`. Grep of `HasRealPasswords` / `CreateConnectors` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tools` + `D:\Prop\tests`:

| Site | Calls `HasRealPasswords`? | Calls `CreateConnectors*`? | Loads `EnvFile.FindAndLoad()`? |
|---|---|---|---|
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L35 / L45 | **Yes** (the choke) | **Yes** (after throw) | n/a |
| `D:\Prop\apps\api\Program.cs` L9 / L14 | via `AddTraderIntelligence` | via DI | **Yes** then `AddEnvironmentVariables` |
| `D:\Prop\apps\mt5-worker\Program.cs` L7 | via DI | via DI | **No** |
| `D:\Prop\apps\fix-worker\Program.cs` L7 | via DI | via DI | **No** |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` L6–19 | **No** (weaker whitespace check) | `CreateConnectorsFromEnvironment()` | **Yes** |
| `D:\Prop\tests` | **0 hits** | **0 hits** | n/a |

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

In-memory DB fallback is **independent** of the password throw (`DATABASE_URL` empty/`<SECRET>` → `UseInMemoryDatabase("trader-intelligence-live")`). Real passwords + placeholder DB still start the host against InMemory. That is a **persist** hole, not a FakeMt5 hole, and not a send hole.

`RealCopyEnabled` is **not** read from `REAL_COPY_EXECUTION_ENABLED` or `FeatureFlags:LiveCopyEnabled` on this path. Literal `false`.

`apps/*/appsettings.json` contain **no** `MT5_PASSWORD` keys. Worker appsettings are logging-only. If secrets live only in `D:\Prop\.env` and a worker is started without inheriting process env, `HasRealPasswords` is `false` → **throw** (fail-closed start). That is correct refuse, not a dummy fill.

---

## 3. Branch table (source-faithful replica of `IsSecret`)

Predicate (same operators as L52–55):

`IsSecret(v) = !IsNullOrWhiteSpace(v) && !OrdinalContains(v, "<SECRET>") && !OrdinalContains(v, "(a/c")`

`HasRealPasswords = IsSecret(MT5_PASSWORD) && IsSecret(MT5_STARWAVEFX_PASSWORD)`

Synthetic tokens only. No operator secrets. Expected column is the **literal C# result**, including the documented fail-open residuals.

| Case | Achiever token | Starwave token | Result | Fail-closed vs dummy policy? |
|---|---|---|---|---|
| both missing / null | *(absent)* | *(absent)* | `false` | **Yes** |
| both empty | `""` | `""` | `false` | **Yes** |
| both whitespace | `"  "` | `"\t"` | `false` | **Yes** |
| achiever only | non-placeholder | `""` | `false` | **Yes** (AND) |
| starwave only | `""` | non-placeholder | `false` | **Yes** (AND) |
| both exact `<SECRET>` | `<SECRET>` | `<SECRET>` | `false` | **Yes** |
| mixed `<SECRET>` + real-looking | `<SECRET>` | token | `false` | **Yes** |
| mixed real-looking + `<SECRET>` | token | `<SECRET>` | `false` | **Yes** |
| both `(a/c` comment | `pw (a/c 1)` | `pw (a/c 2)` | `false` | **Yes** |
| both synthetic non-placeholder | `not-a-placeholder-token` | same family | `true` | intended allow |
| lowercase `<secret>` | `<secret>` | `<secret>` | **`true`** | **No** — Ordinal hole |
| mixed-case `<Secret>` | `<Secret>` | `<Secret>` | **`true`** | **No** |
| dummy words | `dummy` | `changeme` | **`true`** | **No** vs dummy-word policy |
| single char | `x` | `y` | **`true`** | **No** vs strength |
| uppercase `(A/C` | `pw (A/C 1)` | `pw (A/C 2)` | **`true`** | **No** — case hole |

On-disk replica (not executed this session): `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` (15 cases + DI throw assertion). `RESULT.json` **absent**. Product `D:\Prop\tests` grep `HasRealPasswords|IsSecret` = **0**. The gate is **untested in CI**.

`mt5-sdk/.env.example` style `MT5_PASSWORD=replace_with_manager_password` would pass `IsSecret` (no `<SECRET>`, no `(a/c`). DI would **start** two native connectors. `Connect` would then fail (wrong password). Ingest would **not** paint Fake 10001 (`LiveIngestHostedService` L70). That is fail-on-connect, not fail-on-registration.

---

## 4. Bypass / weaker gates (residuals)

### 4.1 Factory is public and ungated

`CreateConnectors` and `CreateConnectorsFromEnvironment` do **not** call `HasRealPasswords`. Empty passwords become `Password = ""` on `NativeMt5Options`. Connect then fails (auth), unless a caller skips Connect. Product hosts cannot skip the throw; the probe can skip `IsSecret`.

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

### 4.3 `DemoSeeder` still on disk

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still calls `DemoBrokerFactory.CreateDefault()` (FakeMt5 10001–10003 / 99001). Product `Program.cs` files do **not** call it (API / workers seed `BrokerCatalogSeed.EnsureAsync` only). `apps` grep `DemoSeeder` = **0**. Tests still do (`tests/Integration/SeedingAndStoreTests.cs`). Residual **policy** risk if someone removes the DI throw later — not current host behavior.

### 4.4 `apps/mt5-worker/Worker.cs` leftover scorer

After `SyncBrokerAsync` for **both** broker codes (full catalog via `GetAccountsAsync(null)`), the worker still rebuilds only `{10001,10002,10003,99001}`. That does **not** shrink the catalog. It is a leftover dummy **score set**, not a Fake registration, and not a `HasRealPasswords` defect. Hosted ingest (`LiveIngestHostedService` L106) scores `ListLoginsWithDealsAsync` instead. API `/api/ops/resync` scores **all** `ListLoginsAsync`.

### 4.5 Dead `SettingsController` cannot arm send

`D:\Prop\apps\api\Controllers\SettingsController.cs` can PUT Redis `settings:flags:live_copy`. `apps/api/Program.cs` does **not** call `AddControllers` / `MapControllers`. Live `/api/settings` is the **minimal API** at L70–83: `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (false) and `FEATURE_COPY_TRADING_ENABLED = false`. Redis flag writes are **not** on the live graph and would not flip DI’s hardcoded `false` even if they were.

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

`GetAccountsAsync(null)` → every group from `GetGroupsCore`:

| Step | File:lines | Behavior |
|---|---|---|
| Groups | `NativeMt5BrokerConnector.cs` 144–185 | `GroupRequestArray("*")`; if empty, `GroupTotal` + `GroupNext` |
| Accounts (null group) | 189–214 | iterate **every** `GetGroupsCore()` name |
| Per group | 216–233 | `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins` |
| Positions | `DealIngestionService` L84 | `GetGroupPositionsAsync("*")` |
| Deals | L67–70 | `GetGroupDealsAsync` **per group name** |

No account-count knob. No `MT5_DEFAULT_GROUP` filter in C# ingest. `Take(200)` is gone from ingest.

Live ingest on catalog failure: **no dummy substitution**:

```70:70:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    _log.LogError(ex, "{Broker} catalog failed. No dummy data will be substituted.", connector.BrokerCode);
```

One broker can fail Connect while the other proceeds (`st.Connected` skip). That is **per-broker fail-closed ingest**, not Fake fill. The **startup** gate still required **both** passwords to look real, so the failed slot is a **connect** miss, not a dummy password slot.

Connect pump modes (`NativeMt5BrokerConnector.cs` L89–101): `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS`, then fallback `PUMP_MODE_NONE`. There is **no** dealer/trade pump. `Connect` uses the Manager password for **admin API auth**, not for placing orders.

### 5.1 Measured live census (on-disk; not re-probed this slot)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe`, utc `2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, note “Passwords never written.”

That probe used `CreateConnectorsFromEnvironment()` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")` — the same two factory slots this gate unlocks.

| Broker | connected | elapsedMs | groups | accounts | open positions |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | 7212.5885 | 8 | 6512 | 1506 |
| STARWAVEFX | true | 6413.478 | 10 | 1948 | 478 |
| **Total** | | | **18** | **8460** | **1984** |

Achiever groups (name → account count in that JSON):

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

Starwave groups:

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

Empty groups (Achiever 1×0; Starwave 3×0) are **manager-visible empty groups**, not a password-gate cut. Groups the manager ACL cannot see are outside this login’s permission set. `HasRealPasswords` cannot enlarge that ACL.

Prior classified pin (`CREDENTIALS_AND_COPY_STATUS.md`, values unused here): `.env` exists; `MT5_PASSWORD` present (len 8); `MT5_STARWAVEFX_PASSWORD` present (len 11); neither is the exact `<SECRET>` token (Starwave length 11; Achiever length 8 but classified as real-looking). Replica of `HasRealPasswords` on that file: **true**. If the API process loads `.env` (it does via `EnvFile.FindAndLoad` + `AddEnvironmentVariables`), DI **will not throw**. That is intended fail-**open** after both slots look real.

---

## 6. Copy to cTrader: still cannot send (no loss)

Slot-94 extra pin: the object registered after a passing gate is **read-only**.

`IMt5BrokerConnector` (`D:\Prop\src\Application\Contracts\Mt5Contracts.cs` L53–63) exposes only: `Connect` / `Disconnect` / `IsConnected` / `GetGroups` / `GetAccounts` / `GetDeals` / `GetPositions`. No `PlaceOrder`, no `DealerSend`, no `OrderSend`. Bulk extras are `GetGroupDealsAsync` / `GetGroupPositionsAsync`. `PositionCreateArray` at `NativeMt5BrokerConnector.cs` L324 / L341 is the Manager SDK **array factory for PositionRequest / PositionRequestByGroup** (GET). It is not an order send.

`HasRealPasswords` has **zero** FIX / order symbols. Adjacent no-send pins (remeasured this pass):

| Surface | Evidence |
|---|---|
| DI | `RealCopyEnabled = false` hardcoded; comment “do not arm a flag that cannot be honored safely” |
| FIX hosted service | `_runtime.RealCopyEnabled = false` after logon (L68); log “NewOrderSingle still disabled” |
| `CTraderFixSession.BuildLogon` | fields include `(35, "A")` only (L96); **no** `(35, "D")` |
| `src` grep `35=D` | **0 hits** |
| `src` grep `NewOrderSingle` | comments, flags, FSM `MayRetryNewOrderSingle`, seeder LastError — **no encoder** |
| `src` grep `OrderSend` / `DealerSend` / `TradeRequest` | **0 hits** |
| Other FIX `35=` builders | `A` logon; harness `0`/`3`/`8`/`X`/`y`; quote `V`/`y` — none `D` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false`; **not** bound in `AddTraderIntelligence` |
| API `/api/settings` (live minimal API) | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` = **false** |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false`; **unread** by DI |
| `RiskEngine.AllowFixSend` | requires `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy`; **not registered** as a sender |
| `ShadowCopyEngine` | in-process `SimulateEntry` only |
| fix-worker `Worker` | stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."`; even if `CTrader:RealCopyExecutionEnabled` is true it **still refuses** (L45–46) |

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

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `HasRealPasswords` / `LiveMt5Registration` (config grep).

`AppConfig::load` (`config\app_config.cpp`):

- Reads **one** `MT5_PASSWORD` (L150), default `""`.
- Local-mode refuse: `MT5_SERVER` empty or `MT5_LOGIN == 0` (L332–339). **`mt5_password` is not in that fatal list.**
- Production checks `MT5_PASSWORD_ENCRYPTION_KEY` length (L377), not the manager password.
- Proxy vocabulary is `IS_MT5_PROXY_ENABLED` / `MT5_PROXY_*` — **unread** by C# `LiveMt5Registration` (Achiever uses `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled = false`).

Standalone `tests\mt5_group_probe.cpp` `hasLocalConfig` **does** require `!mt5_password.empty()` (plus server + login ≠ 0) and emits `ERROR: missing_manager_credentials`. That is the C++ **probe** fail-closed, single-broker, empty-string only — no `<SECRET>` / `(a/c` scan, no Starwave second key.

C++ **does** have live send: `MT5Session::DealerSend` / `DealerSendOrder` in `src\core\mt5_pool.cpp` (L473, L737) used by `trade_execution_service`. That process is the **prop-firm trading backend**. It is **not** registered by `AddTraderIntelligence` and is **not** the Prop → cTrader copy path. Do not treat C++ startup as the Prop C# dual-broker gate. Do not treat this slot as a review of C++ DealerSend safety.

---

## 8. What this slot does **not** claim

- Did **not** re-run LiveBrokerProbe or `dotnet` `HasRealPasswords` cases this session (no shell). Census numbers are from the on-disk JSON dated `2026-08-18T08:42:16Z`.
- Did **not** print or classify live secret **values**. `D:\Prop\.env` exists (prior reports); values unused here.
- Did **not** prove Manager ACL completeness beyond “request APIs + `group: null`.”
- Did **not** greenwash “fully fail-closed secret hygiene.” Residuals in §3–§4 remain.
- Did **not** audit or disable C++ `DealerSend` (different binary).

---

## 9. Residual list (honest)

1. `StringComparison.Ordinal` → `<secret>`, `<Secret>`, `(A/C` pass.
2. `dummy` / `changeme` / single-char / `replace_with_manager_password` pass.
3. Login / server / port not part of the gate (`Login = 0` is connect-fail later).
4. Public factory bypass; LiveBrokerProbe whitespace-only.
5. Zero product tests of `HasRealPasswords`.
6. `DemoSeeder` + FakeMt5 still in tree (tests / unused host path).
7. mt5-worker leftover score set `{10001,10002,10003,99001}` after a full live catalog.
8. C++ `AppConfig::load` will start local mode with empty `MT5_PASSWORD` (different process).
9. FIX logon uses the same weak `<SECRET>` Ordinal check (logon only; still no send).
10. In-memory DB fallback is independent of the password throw (persist hole only).

None of these emit `35=D` from the Prop process.

---

## 10. Files read (absolute)

- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\Controllers\SettingsController.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + both broker group tables)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (classified presence/lengths only)
- `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs`

---

## 11. Slot close

`HasRealPasswords` **does** fail-closed the product host against missing / exact-placeholder / one-sided MT5 passwords: throw, no FakeMt5, no half-dummy graph. After it returns true, the factory registers **both** native managers and ingest fetches **all** groups/traders (`GetAccountsAsync(null)`). Measured prior census: Achiever 8/6512 + Starwave 10/1948. Copy-to-cTrader remains **unarmed** (`IMt5BrokerConnector` read-only; no `35=D` builder; `RealCopyEnabled` forced false). Residuals are validator weakness + untested CI + factory/probe bypass — **not** a live-order path in this process.

**DONE for slot 94.** Reviewer should treat `PASS_WITH_RESIDUALS` as a refuse to greenwash “bulletproof secret gate,” not as a capital-risk FAIL.
