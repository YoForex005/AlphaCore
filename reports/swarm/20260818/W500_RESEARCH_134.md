# W500_RESEARCH_134 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **134** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_134 |
| Assigned | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This file + `INDEX.md` / `SWARM_LOG.md` pins only. |
| Secrets printed | **None.** Password / proxy / FIX values never quoted. Key **names**, lengths, and placeholder-token classification only. |
| Method | Full `read_file` of `LiveMt5Registration.cs` (94/94), `DependencyInjection.cs` (62/62), API / mt5-worker / fix-worker `Program.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `LiveIngestHostedService.cs`, `DealIngestionService.cs`, `NativeMt5BrokerConnector` catalog cores, `CTraderFixSession.cs` (135/135), `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `EnvFile.cs`, `LiveBrokerProbe\Program.cs`, `FakeMt5BrokerConnector` / `DemoSeeder` (existence only), `RiskEngine.cs` allow-send tail, `BaselineScorer.CanPromoteToLive`. `grep` of `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `DemoSeeder` / `35=D` / `NewOrderSingle` / `REAL_COPY` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`. YoPips C++ `config\app_config.cpp` + `HasRealPasswords` grep (**0**). On-disk census `LIVE_GROUPS_AND_TRADERS.json` (counts + group names only; **not re-attached**). `.env` classified by **key presence / length / placeholder token**; values **not** copied. |

**Honesty rule:** fail-closed is **not** “the function exists.” It is: missing / placeholder / one-sided secrets **must not** start a dummy FakeMt5 census, **must not** start a half-broker live graph, and **must not** emit a live cTrader `35=D`. `StringComparison.Ordinal` is **not** case-insensitive. `HasRealPasswords == true` is **not** an order arm. Presence of a real-looking password string is **not** proof of a live Manager session. Sibling slots 14 / 34 / 54 / 74 / 94 / **114** that still say `RealCopyEnabled` is **hardcoded `false`** are **stale vs current `DependencyInjection.cs`**. This slot did **not** launch `dotnet` and did **not** re-attach LiveBrokerProbe; the decision table is a **source-faithful replica** of the 3-clause `IsSecret` predicate.

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
| `RealCopyEnabled` on DI path | **env-bound** | `configuration["REAL_COPY_EXECUTION_ENABLED"] == "true"` (ignore-case) |
| Root `.env` `REAL_COPY_EXECUTION_ENABLED` | **`true`** | flag **armed** if API loads `.env`; **not** a send license |
| After gate passes, ingest asks for ALL groups + ALL manager logins | **Yes** | `GetGroupsAsync` + `GetAccountsAsync(null)` |

**One-line:** `HasRealPasswords` **is** fail-closed for missing / exact-`<SECRET>` / `(a/c` / one-sided keys: DI throws and never substitutes FakeMt5. It is **not** a complete secret validator (`Ordinal` case hole, dummy words, no login/server check, factory bypass). After it returns true, both native managers register and ingest fetches **all** groups/traders. Copy still cannot spend capital because **no NewOrderSingle encoder exists** (`35=D` count in product `*.cs`/`*.json`/`*.csproj` = **0**). `REAL_COPY_EXECUTION_ENABLED=true` in `.env` **does** arm the runtime bool on the API path — that is **not** fail-closed for the flag, and it is **not** a ticket.

Slot verdict: **`PASS_FAIL_CLOSED_DI`**.

Risk to capital: **`NONE` (`SAFE_BY_ABSENCE`)** — this factory never sends; `CopyTradingService.NewOrderSingleImplemented` is `const false`; persisted `AllowFixSend` is hardcoded `false`; `CTraderFixSession` builds `35=A` only; `CanPromoteToLive => false`.

---

## 1. Assigned type (current disk, 94 lines)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` — static factory, **not** a Manager session.

Members (measured, full-file read):

| Member | Role |
|---|---|
| `HasRealPasswords(IConfiguration)` | public dual-password **presence** gate |
| `CreateConnectors(IConfiguration)` | public factory: **exactly two** `NativeMt5BrokerConnector` |
| `CreateConnectorsFromEnvironment()` | public wrapper over process env (`EnvConfiguration`) |
| `IsSecret(string?)` | **private** 3-clause heuristic; only `HasRealPasswords` calls it |
| `EnvConfiguration` | sealed process-env `IConfiguration` (no children / no sections) |

### 1.1 `HasRealPasswords` — AND of two key names

```10:15:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

### 1.2 `IsSecret` — three clauses, all `Ordinal`

```52:55:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

What it **does** check:

1. Non-null, non-empty, not whitespace-only.
2. Does not contain the exact token `<SECRET>` (case-sensitive).
3. Does not contain the exact substring `(a/c` (case-sensitive; intended to reject `.env` comment-style paint).

What it **does not** check (residuals):

| Residual | Effect |
|---|---|
| Case of `<SECRET>` | `<secret>` / `<Secret>` / `<SeCrEt>` → **true** |
| Case of `(a/c` | `(A/C` / `(A/c` → **true** |
| Dummy words | `password`, `dummy`, `replace_with_manager_password`, `x` → **true** |
| Login / server / port | `MT5_LOGIN` unparseable → `Login = 0` later; **not** this gate |
| Connect success | gate can pass and `ConnectAsync` can still throw |
| Length / entropy | length-1 `x` is “real” |

`HasRealPasswords` never enumerates groups or logins. Completeness is the **next** layer, only reachable if the gate passed (or if a caller bypasses it via `CreateConnectors*`).

### 1.3 Factory always builds Native ×2 (ungated)

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // ... MT5_SERVER / MT5_PORT / MT5_LOGIN / MT5_PASSWORD / ACHIEVER_PROXY_*
            NativeDllDirectory = dllDir
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // ... MT5_STARWAVEFX_* ; ProxyEnabled = false
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

Measured pins (no secret values):

| Slot | BrokerCode | Password key | Proxy |
|---|---|---|---|
| Achiever | `"ACHIEVER"` (`BrokerCodes.Achiever`) | `MT5_PASSWORD` | optional HTTP via `ACHIEVER_PROXY_ENABLED` |
| StarwaveFX | `"STARWAVEFX"` (`BrokerCodes.StarwaveFx`) | `MT5_STARWAVEFX_PASSWORD` | **hard** `ProxyEnabled = false` (L45) |

`CreateConnectors` / `CreateConnectorsFromEnvironment` **do not** call `HasRealPasswords`. Empty passwords become `Password = ""`. `Login = 0` when `ulong.TryParse` fails. That is fail-on-`Connect`, not a silent FakeMt5 census. `FakeMt5BrokerConnector` is **not referenced** in this file.

`USE_REAL_MT5` is present in root `.env` but has **0** hits under `D:\Prop\src` and `D:\Prop\apps`. It is **not** this gate.

---

## 2. Product DI is the only caller of the gate

`grep` `HasRealPasswords` / `CreateConnectors` / `CreateConnectorsFromEnvironment` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tools` + `D:\Prop\tests`:

| Site | Calls `HasRealPasswords`? | Calls `CreateConnectors*`? |
|---|---|---|
| `src/Infrastructure/Mt5Live/LiveMt5Registration.cs` | defines | defines both |
| `src/Infrastructure/DependencyInjection.cs` L36 / L47 | **YES** | `CreateConnectors` after throw-gate |
| `tools/LiveBrokerProbe/Program.cs` L19 | **NO** | `CreateConnectorsFromEnvironment` |
| `D:\Prop\tests` (`*.cs`) | **0** | **0** |
| `D:\Prop\apps` (hosts) | only via `AddTraderIntelligence` | only via DI |

`AddTraderIntelligence` (`D:\Prop\src\Infrastructure\DependencyInjection.cs`, **62** lines — sibling 114 cited **59**; current file added `CopyTradingService` + `CopyTradingHostedService` + `RiskEngine`):

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

        // ... store / ingest / scoring ...
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        services.AddHostedService<CopyTradingHostedService>();
```

Fail-closed sequence on `HasRealPasswords == false`:

1. Throw. No `IMt5BrokerConnector`. No `IBrokerRegistry`. No hosted ingest. No FIX logon host. No copy host.
2. **No** `FakeMt5BrokerConnector` substitution (type exists on disk; not registered here).
3. `DemoSeeder` is **not** called on this path.

Hosts that hit the gate:

| Host | Loads `D:\Prop\.env`? | `AddTraderIntelligence`? | If gate false |
|---|---|---|---|
| `apps/api/Program.cs` L10 + L13–15 | **YES** — `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` | YES | process does not start |
| `apps/mt5-worker/Program.cs` L7 | **NO** `EnvFile` | YES | throw unless process/machine env already has both keys |
| `apps/fix-worker/Program.cs` L7 | **NO** `EnvFile` | YES | same throw |
| `tools/LiveBrokerProbe/Program.cs` | YES `EnvFile` | **NO** | weaker whitespace-only check; then ungated factory |

`apps/*/appsettings.json` contain **no** `MT5_PASSWORD` keys. `launchSettings.json` for all three hosts set only `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT`. If secrets live only in `D:\Prop\.env` and a worker is started without inheriting process env, `HasRealPasswords` is `false` → **throw**. That is correct refuse, not a dummy fill.

Startup seed on API + both workers is `BrokerCatalogSeed.EnsureAsync` only (catalog rows: Achiever manager login **2027**, Starwave **9904**, Achiever proxy host/port **names only**). Product `Program.cs` files have **0** `DemoSeeder` / `FakeMt5` / `10001` / `10002` hits.

Residual: `apps/mt5-worker/Worker.cs` L31 still **scores** `{10001,10002,10003,99001}` after `SyncBrokerAsync` of **both** live codes. That does **not** shrink `GetAccountsAsync(null)`. It is a leftover dummy **score set**, not a Fake registration, and not a `HasRealPasswords` defect. Hosted ingest (`LiveIngestHostedService` L106) scores `ListLoginsWithDealsAsync` instead.

---

## 3. Static `IsSecret` / `HasRealPasswords` truth table

Source-equivalent of the 3-clause predicate (not compiled this slot; `D:\Prop\tests` grep = **0**):

| # | `MT5_PASSWORD` | `MT5_STARWAVEFX_PASSWORD` | `HasRealPasswords` | Role |
|---:|---|---|---|---|
| 1 | `null` / missing | anything | `false` | fail-closed |
| 2 | anything | `null` / missing | `false` | fail-closed |
| 3 | `""` / whitespace | real-looking | `false` | fail-closed |
| 4 | real-looking | `""` / whitespace | `false` | fail-closed |
| 5 | `<SECRET>` | real-looking | `false` | fail-closed |
| 6 | real-looking | `<SECRET>` | `false` | fail-closed |
| 7 | contains `(a/c` | real-looking | `false` | fail-closed |
| 8 | real-looking | contains `(a/c` | `false` | fail-closed |
| 9 | real-looking | real-looking | `true` | intended fail-open |
| 10 | only Achiever filled | empty | `false` | one-sided refuse |
| 11 | `<secret>` (lower) | real-looking | **`true`** | case hole |
| 12 | `dummy` | `dummy` | **`true`** | word hole |
| 13 | `x` | `x` | **`true`** | entropy hole |
| 14 | `replace_with_manager_password` (both) | same | **`true`** | template hole; Connect will fail later |

If both keys are the SDK-example string `replace_with_manager_password`, DI **starts** two native connectors and `Connect` fails. Ingest does **not** paint Fake 10001 (`LiveIngestHostedService` L70: `"No dummy data will be substituted."`). That is fail-on-connect, not fail-on-registration.

---

## 4. Root `.env` replica (no values)

Classified from key presence + length + placeholder-token search. **Values not copied.**

| Key | Present | Length | Contains `<SECRET>` | Contains `(a/c` | `IsSecret` replica |
|---|---|---:|---|---|---|
| `MT5_PASSWORD` | yes | 8 | no | no | **true** |
| `MT5_STARWAVEFX_PASSWORD` | yes | 11 | no | no | **true** |

Replica `HasRealPasswords` on this file: **`true`**. If the API process loads this file (it does), DI **will not throw**. That is intended fail-**open after both slots look real**. E011’s “password slots are `<SECRET>` len 8” is **stale** for the two MT5 keys (Starwave length 11; Achiever length 8 but **not** the `<SECRET>` token).

Adjacent non-secret flags on the same file (booleans only):

| Key | Value on disk | Product binding |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **`true`** | **YES** — DI L41 into `LiveRuntimeStatus.RealCopyEnabled` |
| `USE_REAL_MT5` | `true` | **NO** — 0 C# hits |

**Stale correction:** W500_RESEARCH_34 / 54 / 74 / 114 claim “`RealCopyEnabled` hardcoded `false` / not env-bound.” Current `DependencyInjection.cs` L41 **is** env-bound. P500_S045 already measured this overwrite as **absent**. Slot 134 reconfirms.

`CTraderFixOptions.RealCopyExecutionEnabled` still **defaults `false`** (`src/Fix.CTrader/Configuration/CTraderFixOptions.cs` L35). `apps/fix-worker/Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs. Neither of those pins the API runtime singleton.

---

## 5. After the gate: ALL groups + ALL manager traders

`HasRealPasswords` itself never walks the server. Completeness lives here (only if the host started):

| Layer | Call | Scope |
|---|---|---|
| Native groups | `GroupRequestArray("*")` then, if empty, `GroupTotal`/`GroupNext` | all groups this manager ACL can see |
| Native accounts | `GetAccountsAsync(null)` → every group name → `UserRequestArray` first, cache `UserGetByGroup` only on hard fail, empty → `UserLogins` + `UserRequestByLogins` | all users this manager ACL can see |
| Ingest catalog | `DealIngestionService.SyncCatalogAsync` L45–49 | `GetGroupsAsync` + `GetAccountsAsync(null)` |
| Ingest deals/pos | `SyncBrokerAsync` L61–84 | all groups (bulk deals) + `GetGroupPositionsAsync("*")` |
| Live ingest host | `LiveIngestHostedService` L41 `registry.All()` | **both** connectors |
| Probe | `LiveBrokerProbe` L25–26 | same `null` / `"*"` walks |
| Manual resync | `POST /api/ops/resync` loops `"ACHIEVER"` + `"STARWAVEFX"` | same catalog |

No `Take(`/`Skip` on the catalog walk. Residual `Take(200)` is `GET /api/trades` reconstructed rows only (API L110).

On-disk live census (probe=`LiveBrokerProbe`, utc=`2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, note “Passwords never written”). **This slot did not re-attach.** Re-summed from JSON headers + per-group `accounts` fields:

| Broker | connected | groups | traders | open positions | path |
|---|---|---:|---:|---:|---|
| ACHIEVER | true | 8 | 6512 | 1506 | HTTP proxy (factory option) |
| STARWAVEFX | true | 10 | 1948 | 478 | direct (`ProxyEnabled=false`) |
| **Total** | | **18** | **8460** | **1984** | |

Achiever group names (safe) + per-group account counts (sum **6512**):

`contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave group names (safe) + per-group account counts (sum **1948**):

`Starwave\cent\FX1\grp1` 11, `Starwave\cent\FX1\grp2` 4, `Starwave\demo\FX2\grp1` 170, `Starwave\demo\FX2\grp2` 1735, `Starwave\real\FX3\grp1` 22, `Starwave\real\FX3\grp2` 0, `Starwave\real\FX3\grp3` 0, `Starwave\real\FX3\grp4` 4, `Starwave\real\FX3\grp5` 0, `Starwave\real\FX3\LP` 2.

Dummy logins `{10001,10002,10003,99001}` are **not** in that census. Groups the manager cannot see are outside this login’s permission set — that is ACL, not a `HasRealPasswords` cap.

---

## 6. Copy to cTrader cannot send live orders

`HasRealPasswords` has **zero** FIX / order symbols. Adjacent no-send pins (remeasured this pass):

| Pin | Measured |
|---|---|
| Product `35=D` under `D:\Prop\src` + `D:\Prop\apps` (`*.cs`/`*.json`/`*.csproj`) | **0** |
| `CTraderFixSession.BuildLogon` outbound MsgType | **`(35, "A")` only** (L96); one `WriteAsync`; socket disposed |
| `CopyTradingService.NewOrderSingleImplemented` | `const bool` **`false`** (L16) |
| `CopyTradingService.VenueReconciled` | `const bool` **`false`** (L15) |
| Persisted `RiskDecisionRecord.AllowFixSend` | **hardcoded `false`** (L192) even if `Evaluate` would approve |
| Armed-branch body (L198–201) | sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — **still no send call** |
| `CopyTradingHostedService` | shadow intents only; log “Live NewOrderSingle still blocked.” |
| `BaselineScorer.CanPromoteToLive` | **`=> false`** (`src/Domain/Scoring/BaselineScorer.cs` L211) |
| `ExecutionIntent` writers | **0** create/send sites; copy path only `CountAsync` of `SentAt != null` |
| Other `CopyIntent` writer | `EfTradingStore.PersistDemoShadowAsync` status **`SHADOW_ONLY`** |
| `CTraderFixLogonHostedService` | optional `35=A` logon; log “NewOrderSingle still unimplemented” |
| `apps/fix-worker/Worker.cs` | stamps QUOTE/TRADE `Disconnected`; no socket |

`RiskEngine.Evaluate` **is** now called from `CopyTradingService.GenerateShadowIntentsAsync` (sibling W500_99 “0 Evaluate callers” is **stale**). That hop still cannot emit `35=D`. Incoming `Reconciled` is the const `false`, so increasing actions `Reject(..., "VENUE_NOT_RECONCILED")` with `AllowFixSend=false` before the approve tail. Even if that were flipped, `allowSend` still requires `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` — and the persist line then **overwrites** to `false`.

If API loads `.env`, `LiveRuntimeStatus.RealCopyEnabled` will be **`true`**. Dashboard `/api/settings` and `/api/copy/status` will report the flag armed. `copyNote` when armed is: “REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” Treat that as **honesty text**, not a go-live.

---

## 7. YoPips C++ is not this gate

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` grep `HasRealPasswords` / `LiveMt5Registration` = **0**.

`config/app_config.cpp` L150: `cfg.mt5_password = get("MT5_PASSWORD", "");` — empty default, **no** Starwave dual-AND, **no** `<SECRET>` reject. Production checks `MT5_PASSWORD_ENCRYPTION_KEY` length (L377), not the manager password itself. That process is the **prop-firm trading backend** (`trade_execution_service.cpp`). It is **not** registered by `AddTraderIntelligence` and is **not** the Prop → cTrader copy path.

---

## 8. What this slot did **not** do

- Did **not** re-run LiveBrokerProbe or a compiled `HasRealPasswords` harness (no shell). Census numbers are from the on-disk JSON dated `2026-08-18T08:42:16Z`.
- Did **not** print password / proxy / FIX secret values.
- Did **not** edit product source.
- Did **not** flip `REAL_COPY_EXECUTION_ENABLED`.
- Did **not** add a `35=D` builder.

---

## 9. Residuals (honest, not a live-order path)

1. `IsSecret` is a **heuristic**, not a secret store. Case hole + dummy words + 1-char tokens pass.
2. `CreateConnectors*` is public and **ungated**. Probe uses the weaker whitespace check.
3. Workers do not load `.env`; isolated start **throws** (fail-closed) if process env lacks both keys.
4. `mt5-worker` still scores four demo logins after a live sync (leftover, not a Fake registration).
5. Zero product tests of `HasRealPasswords`.
6. `REAL_COPY_EXECUTION_ENABLED=true` on disk **arms** the API runtime bool. That is a **flag residual**, not a sender. Do not add a sender. Do not treat `/api/settings` `true` as a license.

---

## 10. Files read (absolute)

- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (shadow CopyIntent writer)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs` + `Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs` + `Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts + group names)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`

---

## 11. Binding conclusion

`LiveMt5Registration.HasRealPasswords` **does** fail-closed the product host against missing / exact-placeholder / one-sided MT5 passwords: throw, no FakeMt5, no half-dummy graph. After it returns true, the factory registers **both** native managers and ingest fetches **all** groups/traders (`GroupRequestArray("*")` + `GetAccountsAsync(null)`; last measured **18 / 8460**). Copy-to-cTrader remains **unarmed on the wire**. Residuals are validator weakness + untested CI + factory/probe bypass + **env-armed `RealCopyEnabled`** — **not** a live-order path.

**Do not send. Do not add `35=D`. Do not treat a passing password gate as a copy license.**
