# W500_RESEARCH_74 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **74** |
| Date | 2026-08-18 |
| Agent | W500 research worker (slot 74) |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_74.md` |
| Topic | Check `LiveMt5Registration.HasRealPasswords` fail-closed |
| Goal context | Fetch **ALL** Achiever + StarwaveFX groups and **ALL** manager traders; copy to cTrader must **not** send live orders (no loss) |
| Product source modified | **No.** Report + unused local harness only. |
| Assigned file | `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94 lines, full read) |
| Supporting reads | `DependencyInjection.cs`, `apps/api/Program.cs`, `apps/mt5-worker/Program.cs` + `Worker.cs`, `apps/fix-worker/Program.cs` + `Worker.cs`, `tools/LiveBrokerProbe/Program.cs`, `EnvFile.cs`, `LiveIngestHostedService.cs`, `DealIngestionService.cs`, `EfTradingStore.cs` (`ListLogins*`), `CTraderFixLogonHostedService.cs`, `CTraderFixSession.cs`, `CTraderFixOptions.cs`, `NativeMt5BrokerConnector.cs`, `FakeMt5BrokerConnector.cs`, `BrokerRegistry.cs`, `Mt5Contracts.cs`, `LiveRuntimeStatus.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs`, `BrokerCodes.cs`, `apps/api/appsettings.json` FeatureFlags, `apps/api/Controllers/SettingsController.cs`, C++ `config/app_config.cpp` |
| Secrets | **Not printed.** On-disk `.env` classified by key name, class, and length only. |
| Harness | `D:\Prop\reports\swarm\20260818\_tmp_r74_gate\` (synthetic tokens only). This worker has **no shell tool**; harness was **not executed**. Truth table below is a static evaluation of the three-clause predicate as written. Independent of slot 14’s unexecuted `_tmp_r14_gate`. |
| Sibling (do not collapse) | `W500_RESEARCH_14.md` same topic. This slot re-read current tree; did **not** treat 14 as current truth without re-measurement. |

---

## 0. Verdict

**PASS** — product DI **fail-closes** when either Achiever or StarwaveFX manager password is missing, whitespace, or one of the two known placeholder tokens. Dummy/`FakeMt5` is **not** substituted. Both manager connectors are constructed together or not at all. Opening the gate does **not** arm cTrader `NewOrderSingle`.

This is **not** a cryptographic password-quality check. Residual predicate holes (case variants, dummy words) can open the gate without being a real secret. Those holes still lead to **native** `Connect` (fail on the wire), not FakeMt5, and still cannot send a live destination order.

| Assigned claim | Measured |
|---|---|
| Missing / placeholder MT5 passwords refuse dummy data | **YES** — `AddTraderIntelligence` throws **before** any connector is registered |
| One filled broker cannot start a one-sided graph | **YES** — `IsSecret(a) && IsSecret(s)` |
| Gate opening substitutes FakeMt5 / logins 10001 | **NO** — `CreateConnectors` returns `NativeMt5BrokerConnector` × 2 only |
| Gate opening sends live cTrader orders | **NO** — `RealCopyEnabled` forced `false`; `Fix.CTrader` has **0** `35=D` / `NewOrderSingle` builders |
| Automated test of `HasRealPasswords` | **NONE** (`D:\Prop\tests` `*.cs` grep = **0**) |
| Current on-disk `.env` replica | **true** (both MT5 slots `NON_PLACEHOLDER`) — API that loads the file **will not throw** |

---

## 1. Method

1. Full `read_file` of `LiveMt5Registration.cs` (lines 1–94).
2. Grep `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `DemoSeeder` / `NewOrderSingle` / `35=D` / `OrderSend` / `DealerSend` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`.
3. Follow every product caller of `AddTraderIntelligence` and every caller of `CreateConnectors` / `CreateConnectorsFromEnvironment`.
4. Classify `D:\Prop\.env` **without reprinting values**: exact `<SECRET>` match, `(a/c` match, length, `IsSecret` replica.
5. Confirm C++ YoPips backend has **no** `HasRealPasswords` and **no** Starwave dual-password AND.
6. Did **not** connect to Manager, did **not** send FIX, did **not** edit product source.

Stale reports **not reused as current truth**:

- **A002** — API still `DemoSeeder` / FakeMt5 health / `net8.0` only. Current `apps/api/Program.cs` loads `.env`, calls `AddTraderIntelligence`, seeds `BrokerCatalogSeed` only, health reads `LiveRuntimeStatus`.
- **A008 / E011** — “hosts do not load `.env`” / “password slots are `<SECRET>` len 8”. Current API **does** `EnvFile.FindAndLoad()`. MT5 password slots are **not** the `<SECRET>` token (see §6).
- **C05** — `AddTraderIntelligence` always Fake. Current DI throws or registers native pair.

---

## 2. The gate as written

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

Properties (measured from source):

| Property | Value |
|---|---|
| Keys read | `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD` only |
| Other keys ignored | `MT5_LOGIN`, `MT5_SERVER`, `MT5_PORT`, Starwave login/server/port, proxy, `CTRADER_FIX_PASSWORD` |
| Combinator | **AND** — one dummy/missing password fails the whole host |
| Placeholder tokens | substring `<SECRET>` (Ordinal) and substring `(a/c` (Ordinal) |
| Null / `""` / whitespace | `false` (`IsNullOrWhiteSpace`) |
| Side effects | none — bool only; no connect; no log of the value |
| `IsSecret` visibility | **private** — only `HasRealPasswords` uses it |

`CreateConnectors` is a **separate** public method. It does **not** call `HasRealPasswords`. It always builds **exactly two** `NativeMt5BrokerConnector` instances (`BrokerCodes.Achiever` = `"ACHIEVER"`, `BrokerCodes.StarwaveFx` = `"STARWAVEFX"`). `FakeMt5BrokerConnector` is not referenced in this file.

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // ...
            Password = config["MT5_PASSWORD"] ?? "",
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

`Login = 0` when `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN` fail `ulong.TryParse`. That is **not** checked by `HasRealPasswords`. It is fail-on-`Connect` later (`CIMTManagerAPI.Connect` with login 0), not a silent FakeMt5 census.

Grep of `HasRealPasswords` / `CreateConnectors` under `D:\Prop\src` (product C# only): **6 hits**, all in `LiveMt5Registration.cs` + `DependencyInjection.cs`. No other product type calls the gate.

---

## 3. Product DI is the fail-closed enforcer

```35:48:D:\Prop\src\Infrastructure\DependencyInjection.cs
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

        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

Order is the fail-closed contract:

1. If `HasRealPasswords` is false → **throw**. No `IMt5BrokerConnector`, no `IBrokerRegistry`, no hosted ingest, no FIX logon host.
2. If true → `RealCopyEnabled = false` **before** connectors are registered.
3. Then `CreateConnectors` — native pair only.

`FakeMt5BrokerConnector` / `DemoBrokerFactory` / `DemoSeeder` are **not** referenced in this method. Grep of `D:\Prop\apps` for `DemoSeeder` / `FakeMt5` / `DemoBrokerFactory` = **0**. Grep of `DependencyInjection.cs` for `FakeMt5` = **0**.

Asymmetry (honest): empty/`<SECRET>` `DATABASE_URL` / `ConnectionStrings:TraderIntelligence` **fail-open** to `UseInMemoryDatabase("trader-intelligence-live")` (DI lines 26–33). That is persistence, not dummy broker data. MT5 secrets do **not** share that fallback.

`BrokerRegistry.All()` returns whatever DI registered. After a successful gate that is the two native managers (`BrokerRegistry.cs` L21).

---

## 4. Static `IsSecret` / `HasRealPasswords` truth table

Predicate copied from lines 52–55. Inputs are **synthetic**; none are operator secrets. C# `string.IsNullOrWhiteSpace` + `Contains(..., Ordinal)` semantics.

| Case | `MT5_PASSWORD` | `MT5_STARWAVEFX_PASSWORD` | `HasRealPasswords` | Role |
|---|---|---|---|---|
| both missing | `null` | `null` | **false** | fail-closed |
| both empty | `""` | `""` | **false** | fail-closed |
| both whitespace | `"  "` | `"\t"` | **false** | fail-closed |
| Achiever only | non-placeholder | `""` | **false** | dual-AND; no one-broker start |
| Starwave only | `""` | non-placeholder | **false** | dual-AND |
| both `<SECRET>` | `<SECRET>` | `<SECRET>` | **false** | documented placeholder |
| mixed `<SECRET>` | either side | other real-looking | **false** | dual-AND |
| sheet comment | `pw (a/c 1)` | `pw (a/c 2)` | **false** | documented `(a/c` token |
| embedded `<SECRET>` | `pre<SECRET>post` | ok | **false** | substring, not exact-line |
| both synthetic ok | `not-a-placeholder-token` | same class | **true** | intended open |
| lowercase token | `<secret>` | `<secret>` | **true** | **residual** — `Ordinal`, not ignore-case |
| mixed-case token | `<Secret>` | `<Secret>` | **true** | **residual** |
| dummy words | `dummy` | `changeme` | **true** | **residual** — no denylist |
| single char | `x` | `y` | **true** | **residual** |
| upper comment | `pw (A/C 1)` | `pw (A/C 2)` | **true** | **residual** — only `(a/c` |

`StringComparison.Ordinal` is case-sensitive. `<secret>` / `<Secret>` / `(A/C` **do not** trip the gate. That is a real predicate hole. It does **not** register FakeMt5.

Unexecuted harness (synthetic only): `D:\Prop\reports\swarm\20260818\_tmp_r74_gate\Program.cs` would call the real `HasRealPasswords`, prove the DI throw message (including achiever-only), inspect connector types + `RealCopyEnabled` after a synthetic open, and prove `CreateConnectors` is unguarded on empty cfg. Not executed this slot (no shell).

---

## 5. Who calls the gate — and who bypasses it

| Caller | Loads `D:\Prop\.env`? | Calls `HasRealPasswords`? | If gate false |
|---|---|---|---|
| `apps/api/Program.cs` L9 + L14 | **YES** — `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` | **YES** via `AddTraderIntelligence` | process does not start |
| `apps/mt5-worker/Program.cs` L7 | **NO** — `Host.CreateApplicationBuilder` only | **YES** via DI | throw unless process/user env already has both keys |
| `apps/fix-worker/Program.cs` L7 | **NO** | **YES** via DI | same throw (FIX host cannot start without **both** MT5 passwords) |
| `tools/LiveBrokerProbe/Program.cs` | **YES** — `EnvFile.FindAndLoad()` | **NO** | weaker whitespace-only check; then `CreateConnectorsFromEnvironment()` |
| `tests/Integration/SeedingAndStoreTests.cs` | n/a | **NO** | builds `DemoSeeder` / FakeMt5 by hand |

API load path (current; E011 stale on this point):

```9:14:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.Load` writes keys into **process** environment (values never logged). `FindAndLoad` candidates include `D:\Prop\.env` (`EnvFile.cs` lines 8–15).

LiveBrokerProbe is **weaker** than `HasRealPasswords`:

```7:13:D:\Prop\tools\LiveBrokerProbe\Program.cs
var aPass = Environment.GetEnvironmentVariable("MT5_PASSWORD");
var sPass = Environment.GetEnvironmentVariable("MT5_STARWAVEFX_PASSWORD");
if (string.IsNullOrWhiteSpace(aPass) || string.IsNullOrWhiteSpace(sPass))
{
    Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "real_passwords_missing", env = envPath }));
    return 2;
}
```

A `.env` whose passwords are exactly `<SECRET>` (len 8, non-whitespace) would **fail** DI and **pass** the probe, which would then `Connect` with the placeholder. That is a tool-path hole, not a product-host hole.

`CreateConnectors` / `CreateConnectorsFromEnvironment` remain public. Any future host that skips `AddTraderIntelligence` skips the throw.

`launchSettings.json` for api / mt5-worker / fix-worker (prior A008 / this-slot grep of product hosts) set process env to framework keys only. **No** password env names in appsettings.

`apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` is **`false`** and is **not** bound to `LiveRuntimeStatus.RealCopyEnabled`.

---

## 6. On-disk `.env` classification (values discarded)

File `D:\Prop\.env` **exists** (gitignored). Classification only. Values were read for class/length and **not** copied here.

| Key | Present | Class | Length | `IsSecret` replica |
|---|---|---|---:|---|
| `MT5_PASSWORD` | yes | `NON_PLACEHOLDER` (not empty, not `<SECRET>`, no `(a/c`) | 8 | **true** |
| `MT5_STARWAVEFX_PASSWORD` | yes | `NON_PLACEHOLDER` | 11 | **true** |
| `CTRADER_FIX_PASSWORD` | yes | `NON_PLACEHOLDER` | 10 | n/a (not in this gate) |
| `DATABASE_URL` `Password=` slot | yes | `PLACEHOLDER_SECRET_EXACT` (`<SECRET>`) | 8 | n/a — DI uses in-memory EF |
| `REAL_COPY_EXECUTION_ENABLED` | yes | flag | — | value **`false`** (safe to print) |
| `FEATURE_COPY_TRADING_ENABLED` | yes | flag | — | value **`false`** |
| `CTRADER_FIX_ENABLED` | yes | flag | — | value **`true`** (logon allow, not send) |
| `(a/c` anywhere in file | **no** | — | — | — |

Exact-line greps `^MT5_PASSWORD=<SECRET>\s*$` and `^MT5_STARWAVEFX_PASSWORD=<SECRET>\s*$`: **0 matches** (only `CTRADER_FIX_ENABLED` / `REAL_COPY_*` / `FEATURE_COPY_*` hit that pattern family).

**Replica of `HasRealPasswords` on this file: `true`.** If the API process loads this file (it does), DI **will not throw**. That is intended fail-**open** after both slots look real. E011’s “password slots are `<SECRET>` len 8” is **stale** for the two MT5 keys (Starwave length 11; Achiever length 8 but **not** the `<SECRET>` token).

This worker did **not** snapshot process/user/machine environment (no shell). Classification is the on-disk file the API loader will ingest.

---

## 7. Dual-AND vs “fetch ALL groups / ALL traders”

`HasRealPasswords` does **not** enumerate groups or logins. Completeness lives downstream:

| Layer | All-groups / all-traders behavior |
|---|---|
| `HasRealPasswords` | Blocks a **one-broker** start when the other password is dummy. Prevents a FakeMt5 3+1 login universe. |
| `CreateConnectors` | Always returns **both** native managers. `BrokerRegistry.All()` is that pair. |
| `DealIngestionService.SyncCatalogAsync` | `GetGroupsAsync` then `GetAccountsAsync(null, ct)` — no group mask, no `Take`. |
| `NativeMt5BrokerConnector.GetGroupsCore` | `GroupRequestArray("*")`, fallback `GroupTotal`/`GroupNext`. |
| `GetAccountsCore(null)` | walks **every** group from `GetGroupsCore`. |
| `LiveIngestHostedService` | `registry.All()`, catalog then deals; scores `store.ListLoginsWithDealsAsync` (logins that have deals, not the full account set). |
| `apps/api` `/api/ops/resync` | both codes `ACHIEVER` + `STARWAVEFX`; scores **all** `ListLoginsAsync` (not 10001). |
| `apps/mt5-worker/Worker.cs` | `SyncBrokerAsync` both brokers (full catalog/deals) but still scores **hardcoded** `10001,10002,10003,99001`. **Adjacent hole**, not this gate. |

On-disk live census (probe, not this slot’s connect): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`:

| Broker | connected | groups | accounts | openPositions |
|---|---|---:|---:|---:|
| ACHIEVER | true | 8 | 6512 | 1506 |
| STARWAVEFX | true | 10 | 1948 | 478 |

Achiever groups: `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.  
Starwave groups: `Starwave\cent\FX1\grp1/2`, `Starwave\demo\FX2\grp1/2`, `Starwave\real\FX3\grp1..5`, `Starwave\real\FX3\LP`.

`IMt5BrokerConnector` surface is read-only: Connect / Disconnect / groups / accounts / deals / positions (`Mt5Contracts.cs` 53–63). No send method.

Scoring caveat (not a password-gate FAIL): `LiveIngestHostedService` scores `ListLoginsWithDealsAsync` (distinct logins on `Mt5Deals`). Catalog still upserts **all** accounts. Traders with zero deals in the window are stored, not scored. API manual resync uses `ListLoginsAsync` (full stored set).

---

## 8. Copy to cTrader — still no live send

Gate success ≠ copy armed.

| Check | Measured |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` constructed | **false** (DI L41) |
| `CTraderFixLogonHostedService` after logon | sets `_runtime.RealCopyEnabled = false` again (L68) |
| `/api/settings` `FEATURE_COPY_TRADING_ENABLED` | hardcoded **false** (`Program.cs` L76) |
| `/api/settings` `REAL_COPY_EXECUTION_ENABLED` | mirrors `runtime.RealCopyEnabled` (false) |
| `.env` `REAL_COPY_EXECUTION_ENABLED` | **`false`** |
| `.env` `FEATURE_COPY_TRADING_ENABLED` | **`false`** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false**; **not** bound in `AddTraderIntelligence` |
| `Fix.CTrader` `35=D` | **0 hits** |
| `CTraderFixSession` | `35=A` Logon only (`BuildLogon` field 35 = `"A"`); `using` disposes the TLS socket after the 20s probe |
| `src/Mt5` `OrderSend` / `DealerSend` / `TradeTrans` / `DealerBalance` | **0 product hits** (SDK C++ has DealerSend; not on the Prop C# copy path) |
| `apps/fix-worker/Worker.cs` | stamps `NewOrderSingle remains off`; even if `CTrader:RealCopyExecutionEnabled` were true it only logs a warning |
| `SettingsController` | can PUT Redis `settings:flags:live_copy`; API `Program.cs` has **no** `AddControllers` / `MapControllers`. Dead type. Does not flip `LiveRuntimeStatus.RealCopyEnabled`. |

---

## 9. C++ YoPips backend (out of this gate)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` grep `HasRealPasswords`: **0**.

`config/app_config.cpp` L150: `cfg.mt5_password = get("MT5_PASSWORD", "");` — empty default, **no** `MT5_STARWAVEFX_PASSWORD`, **no** dual-AND, **no** `<SECRET>` reject. Production fail-closed checks JWT / encryption-key length / CORS / admin IP — **not** “both manager passwords present.”

That process is the **prop-firm trading backend** (`DealerSend` lives in `mt5-sdk` / C++ manager). It is **not** registered by `AddTraderIntelligence` and is **not** the Prop → cTrader copy path.

Slot 74 does **not** claim the C++ backend is fail-closed. It is a different binary.

---

## 10. Residuals (do not greenwash)

| Residual | Severity | Effect |
|---|---|---|
| `IsSecret` Ordinal only; `<secret>` / `(A/C` / `dummy` / `x` open the gate | medium (predicate) | Native connectors constructed; Connect fails or succeeds on the wire. **Not** FakeMt5. |
| `CreateConnectors` public, unguarded | low | Probe / future host can skip the throw |
| LiveBrokerProbe whitespace-only | low (tool) | `<SECRET>` would attempt Manager Connect |
| Workers do not load `.env` | ops | Isolated `dotnet run` of mt5/fix worker throws (fail-closed) unless process env is set |
| Logins/servers not gated | low | `Login=0` / empty server → Connect throw, not dummy census |
| Zero unit tests | test gap | gate can regress unnoticed |
| C# process probe not executed this slot | measurement | static predicate eval only; harness on disk at `_tmp_r74_gate` |
| DB `<SECRET>` → InMemory | unrelated | dashboard can run without Postgres |
| Live ingest scores `ListLoginsWithDealsAsync` | adjacent | zero-deal accounts catalogued, not scored |
| mt5-worker scores 4 demo logins | adjacent | not a `HasRealPasswords` defect |
| `FakeMt5BrokerConnector` still in tree | hygiene | unused by product hosts; tests/seeder + tmp harnesses only |

---

## 11. No-loss implication

`HasRealPasswords` cannot place, flatten, or size a destination order. Fail-closed throw prevents a FakeMt5 / 10001 graph from becoming the live API registry. Fail-open (current filled `.env` replica = true) starts **read-only** Manager ingest + optional FIX `35=A` logon. `RealCopyEnabled` stays false. There is no `35=D` builder under `src/Fix.CTrader`.

**Risk to capital from this slot: none** (no live send path). Do not treat PASS as “go-live for copy.” Do not treat PASS as “password quality validated.” Do not treat PASS as “this slot re-fetched 6512+1948 live logins” — that census is a prior probe artifact; this slot measured the **gate**.

---

## 12. Headline for slot 74

`LiveMt5Registration.HasRealPasswords` **is fail-closed for product DI**: both Achiever and StarwaveFX passwords must look non-placeholder or `AddTraderIntelligence` throws `Real MT5 passwords are required. Dummy/fake broker data is disabled.` Dummy broker substitution is disabled. Both managers register together. cTrader copy remains unarmed.
