# W500_RESEARCH_34 — `LiveMt5Registration.HasRealPasswords` fail-closed

| Field | Value |
|---|---|
| Slot | **34** |
| Date | 2026-08-18 |
| Role | Senior engineer — re-read current disk (no product edit) |
| Topic | Check `LiveMt5Registration.HasRealPasswords` fail-closed |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Test source modified | **No** |
| Secrets written / printed | **No.** Key **names** only. No password / proxy / FIX / DB values. |
| Assigned type | `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94/94 lines) |
| Composition | `D:\Prop\src\Infrastructure\DependencyInjection.cs` (59/59) |
| Method | Full `read_file` of the assigned file + DI + API/worker/probe `Program.cs` + FIX logon session + `EnvFile` + `LiveIngestHostedService`. Grep `HasRealPasswords` / `IsSecret` / `CreateConnectors` / `FakeMt5` / `35=D` / `NewOrderSingle` / `DealerSend` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\tools`, and YoPips C++ `src`. Decision table is **source-equivalent** of the 3-line `IsSecret` predicate (no process launched this slot). Pre-existing hermetic probe `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` encodes the same table; `RESULT.json` was **absent** at read time. |

**Honesty rule:** quote files as they sit **now**. A002 / A010 / C05 / C42 / R003 that say “DI always registers FakeMt5” or “API still seeds DemoSeeder / EnvFile unused” are **stale vs `D:\Prop\src`**. A comment or flag name containing `NewOrderSingle` is **not** a `35=D` builder. `HasRealPasswords == true` is **not** an order arm. Presence of a real-looking password string is **not** proof of a live Manager session.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| Product hosts start with dummy/Fake MT5 when passwords are missing / `<SECRET>` / `(a/c` | **No.** `AddTraderIntelligence` **throws** | `FAIL_CLOSED` |
| Both Achiever **and** StarwaveFX password keys required | **Yes.** `IsSecret(a) && IsSecret(s)` | `FAIL_CLOSED` (no one-broker start) |
| DI can register `FakeMt5BrokerConnector` / `DemoBrokerFactory` | **No.** 0 hits in `DependencyInjection.cs`. `CreateConnectors` builds `NativeMt5BrokerConnector` ×2 only | `FAIL_CLOSED` (no fake fallback) |
| Connect fail substitutes Fake / 10001 tape | **No.** Ingest logs “No dummy data will be substituted.” | `FAIL_CLOSED` |
| `IsSecret` rejects every dummy/template string | **No.** Case-sensitive tokens only; `dummy` / `<secret>` / `replace_with_manager_password` pass | `HEURISTIC_NARROW` (residual fail-open vs template) |
| `CreateConnectors` / `CreateConnectorsFromEnvironment` self-gate | **No.** Gate is a **caller** check | residual (probe bypasses `IsSecret`) |
| Product unit/integration test of `HasRealPasswords` | **0** hits under `D:\Prop\tests` | `MISSING` lock |
| Fetch ALL manager-visible groups + traders after a real start | **Yes** (code + prior probe census) | `EXISTS_AND_GOOD` / `MEASURED` |
| Copy to cTrader can send a live order | **No** | `SAFE_BY_ABSENCE` |

**One-line:**

```text
HasRealPasswords is fail-closed at AddTraderIntelligence (AND both keys; throw; Native only; no Fake).
IsSecret is a narrow Ordinal placeholder heuristic. CreateConnectors* is not self-gated.
No 35=D. RealCopyEnabled forced false. Capital not at risk from this gate.
```

**Slot verdict:** `PASS_FAIL_CLOSED_DI`

**Risk to capital:** `NONE` (`SAFE_BY_ABSENCE`)

This gate is a **dummy-block**, not an order arm. A weak `IsSecret` hit can only construct native Manager readers (or throw / fail `Connect`). It cannot emit FIX `35=D`.

---

## 1. Assigned implementation (94 lines, full read)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` is a **static manager-connection factory** plus a secret-**presence** predicate. Members:

| Member | Visibility | Role |
|---|---|---|
| `HasRealPasswords(IConfiguration)` | public | dual-broker presence gate |
| `CreateConnectors(IConfiguration)` | public | build Achiever + StarwaveFX natives |
| `CreateConnectorsFromEnvironment()` | public | same via process env adapter |
| `IsSecret(string?)` | **private** | placeholder heuristic |
| `EnvConfiguration` | private nested | `Environment.GetEnvironmentVariable` only |

It does **not** call `Connect`, `UserLogins`, `GetGroups`, `DealerSend`, or any FIX writer. It cannot enumerate traders or place orders.

### 1.1 `HasRealPasswords` — AND of two key names

```10:15:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

Keys checked (names only):

| Key | Broker slot |
|---|---|
| `MT5_PASSWORD` | Achiever Manager password |
| `MT5_STARWAVEFX_PASSWORD` | StarwaveFX Manager password |

**Not** checked: `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN`, servers, ports, proxy flags, `CTRADER_FIX_PASSWORD`, `DATABASE_URL`. A missing/unparseable login still constructs `Login = 0` (connect-fail later). That is fail-on-connect for that slot, not a silent one-trader census.

### 1.2 `IsSecret` — three-clause heuristic

```52:55:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

| Clause | Comparison | Rejects |
|---|---|---|
| non-null / non-whitespace | `IsNullOrWhiteSpace` | `null`, `""`, `"   "`, `"\t"` |
| architecture placeholder | `Contains("<SECRET>", Ordinal)` | exact mixed-case token anywhere in the string |
| account-comment leftover | `Contains("(a/c", Ordinal)` | lowercase `c` only |

`StringComparison.Ordinal` is **case-sensitive**. `<secret>`, `<Secret>`, `(A/C` do **not** match. That is the residual.

### 1.3 `CreateConnectors` — Native ×2, no Fake, no self-gate

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // MT5_SERVER / MT5_PORT / MT5_LOGIN / MT5_PASSWORD + ACHIEVER_PROXY_*
            NativeDllDirectory = dllDir
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // MT5_STARWAVEFX_* ; ProxyEnabled = false
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

Constructor of `NativeMt5BrokerConnector` stores options only (`NativeMt5BrokerConnector.cs` L32). Empty password still constructs. The **throw** in DI is what prevents dummy start.

`CreateConnectorsFromEnvironment()` (`L17–18`) wraps `new EnvConfiguration()` and **does not** call `HasRealPasswords`.

---

## 2. Host composition — this is where fail-closed is enforced

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

Measured composition facts:

| Check | Result |
|---|---|
| `FakeMt5` / `DemoBrokerFactory` in this file | **0** |
| Connector type registered | `NativeMt5BrokerConnector` ×2 (Achiever + StarwaveFX) |
| On `HasRealPasswords == false` | **throw** before `CreateConnectors` |
| `RealCopyEnabled` | **forced `false`** (not bound from env / `FeatureFlags:LiveCopyEnabled`) |
| Hosted services after the gate | `LiveIngestHostedService` (Manager **read**) + `CTraderFixLogonHostedService` (FIX **35=A** only) |

Three product hosts call `AddTraderIntelligence`:

| Host | Path | `.env` load | Gate |
|---|---|---|---|
| API | `D:\Prop\apps\api\Program.cs` L9 + L14 | **Yes** — `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` | throw if keys dummy |
| mt5-worker | `D:\Prop\apps\mt5-worker\Program.cs` L7 | **No** `EnvFile` | throw if process env dummy/absent |
| fix-worker | `D:\Prop\apps\fix-worker\Program.cs` L7 | **No** `EnvFile` | same throw (then FIX logon host) |

`launchSettings.json` for API / mt5-worker set only `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT`. **No** `MT5_PASSWORD` in appsettings or launch profiles (`apps/api/appsettings.json` has no those keys). Workers launched from VS without user/process env **cannot start**. That is fail-closed by absence.

API is the only host that hydrates process env from gitignored `D:\Prop\.env` (`EnvFile.cs` L14 hard-path + cwd walk). Values are **not** quoted here. `CREDENTIALS_AND_COPY_STATUS.md` already recorded both MT5 password **keys** as PRESENT (lengths only).

Independent of this gate: empty / `<SECRET>` `DATABASE_URL` still selects **InMemory** EF (`DependencyInjection.cs` L26–28). That is a persistence fallback, **not** a Fake-MT5 fallback.

---

## 3. Decision table (source-equivalent of `IsSecret` ∧ `IsSecret`)

Predicate is three boolean clauses with no I/O. Expected column = what a **strict** fail-closed dummy-block should return. Actual = current code.

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
| SDK example template | `replace_with_manager_password` | same | **true** | **false** | **NO** |
| dummy words | `dummy` | `changeme` | **true** | **false** | **NO** |
| single character | `x` | `y` | **true** | **false** | **NO** |
| uppercase account comment | `pw (A/C 1)` | `pw (A/C 2)` | **true** | **false** | **NO** |

`mt5-sdk/.env.example` L29 is literally `MT5_PASSWORD=replace_with_manager_password`. If an operator copied that string into **both** Prop keys, `HasRealPasswords` would return **true** and DI would **start** two native connectors. `Connect` would then fail (wrong password). Ingest would **not** paint Fake 10001 (`LiveIngestHostedService` L70). That is fail-on-connect, not fail-on-registration.

Pre-existing probe `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` uses the **current** (loose) behavior as `expected` for the last five rows and also asserts DI throws the exact `InvalidOperationException` message on both-`<SECRET>`. `RESULT.json` was not on disk this slot — do not treat that probe as a measured run.

---

## 4. Callers — who is actually fail-closed

Grep `HasRealPasswords` / `CreateConnectors` / `CreateConnectorsFromEnvironment` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tools` + `D:\Prop\tests`:

| Site | Calls `HasRealPasswords`? | Calls `CreateConnectors*`? |
|---|---|---|
| `LiveMt5Registration.cs` | definition | definition |
| `DependencyInjection.AddTraderIntelligence` | **Yes** — throw | **Yes** — after throw |
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

Probe rejects **whitespace only**. A value of `<SECRET>` would pass this `if` and still call `CreateConnectorsFromEnvironment()`. That is **not** the product host path. Probe is read-only census (groups/accounts/positions). It does not send FIX.

### 4.2 Tests still use DemoSeeder — off the live graph

`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 calls `DemoSeeder.SeedAsync` and asserts login **10001**. It never calls `AddTraderIntelligence`. Dummy tape in tests is **not** a host fail-open.

`DemoSeeder` + `DemoBrokerFactory` remain on disk (`src/Infrastructure/Seeding/DemoSeeder.cs`, `src/Mt5/Connectors/FakeMt5BrokerConnector.cs`). Product `Program.cs` files do **not** call them (W500_RESEARCH_11 / 31). Residual: `apps/mt5-worker/Worker.cs` L31 still **scores** `{10001,10002,10003,99001}` after live `SyncBrokerAsync`. That is a scoring-set leftover, **not** a `HasRealPasswords` bypass and **not** an order send.

---

## 5. Goal context — ALL groups / ALL traders / no live copy

Once the host **does** start (both keys pass `IsSecret`):

| Step | Code | ALL? |
|---|---|---|
| Register both managers | `CreateConnectors` returns Achiever + StarwaveFX | both owned brokers |
| Groups | `NativeMt5BrokerConnector.GetGroupsCore` → `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback (L155–183) | manager-ACL-visible |
| Traders | `GetAccountsAsync(null)` → every group name → `UserRequestArray` + `UserLogins`/`UserRequestByLogins` fallback (L189–232) | manager-ACL-visible |
| Positions | `GetGroupPositionsAsync("*")` or per-login (W500_RESEARCH_35) | not first-N |
| Prior live probe | `LIVE_GROUPS_AND_TRADERS.json` / `CREDENTIALS_AND_COPY_STATUS.md` | Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460** |

`HasRealPasswords` cannot shrink that census. It only decides whether the host is allowed to construct the two native readers.

YoPips C++ (`D:\Projects\YoPips\Backend\C++ Backend PropFirm`) has **0** hits for `HasRealPasswords` / `LiveMt5Registration`. Single-broker `AppConfig` `MT5_PASSWORD`. Not this product’s dual-broker gate.

---

## 6. Copy-to-cTrader cannot take a loss through this path

| Surface | Measured |
|---|---|
| `35=D` / `(35, "D")` under `D:\Prop\src` + `D:\Prop\apps` | **0** |
| `OrderSend` / `DealerSend` / `TradeRequest` under `D:\Prop\src` | **0** |
| `CTraderFixSession` outbound MsgType | **`"A"` Logon only** (`BuildLogon` L96); `ssl.WriteAsync` once; sockets disposed |
| `RealCopyEnabled =` assignments in `src` | **2**, both `false` (DI L41; FIX hosted service L68) |
| `CTraderFixOptions.RealCopyExecutionEnabled` default | `false` (L35) — type is **not** bound in DI |
| `/api/settings` `REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (forced false) |
| FIX password missing / `<SECRET>` | logon **skipped** (`CTraderFixLogonHostedService` L34–37) — process stays up; still no NOS |

W500_RESEARCH_30 already classified FIX send as `SAFE_BY_ABSENCE`. This slot does not reopen a TLS session.

`HasRealPasswords` passing with live Manager secrets still only arms **ingest + optional FIX Logon**. It does **not** flip `RealCopyEnabled`.

---

## 7. Stale reports (do not reuse)

| Report | Stale claim vs current disk |
|---|---|
| A002 | API still `DemoSeeder` / health FakeMt5; Infrastructure CS0246 |
| A010 | `EnvFile.Load` unused; `.env` unfilled example |
| C05 / C42 / R003 / D23-era | `AddTraderIntelligence` always `DemoBrokerFactory.CreateDefault()` |
| A005 health string | `/api/health` now reports `LiveRuntimeStatus` broker rows, not FakeMt5 |

Current API health (`Program.cs` L32–56) exposes `realCopyEnabled = runtime.RealCopyEnabled` and live Manager `groups=` / `accounts=` / `phase=`.

---

## 8. Residuals (not slot FAIL, not capital)

1. **`IsSecret` is narrow.** Case-sensitive `<SECRET>` / `(a/c`. SDK template `replace_with_manager_password` and words like `dummy` pass. Tighten to `OrdinalIgnoreCase` + a denylist (`replace_with_`, `changeme`, `dummy`, `<secret>`) **or** keep as-is and accept fail-on-connect.
2. **`CreateConnectors*` is public and ungated.** Only `AddTraderIntelligence` throws. Probe uses a weaker whitespace check.
3. **Workers do not load `.env`.** Fail-closed if process env empty (throw). They will not silently become Fake.
4. **No product test** locks the throw message or the AND-both-keys table.
5. **Logins not in the gate.** `Login = 0` is connect-fail, not a one-login universe.
6. **mt5-worker `Worker.cs` still scores four demo logins.** Completeness leftover after live ingest; not an order path.
7. **FIX password is outside this gate.** Missing FIX password skips logon; it does not fail the API process.

None of these emit `35=D` or register Fake MT5 on the live DI graph.

---

## 9. Files read (this slot)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | assigned gate + factory |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | throw + Native register + `RealCopyEnabled=false` |
| `D:\Prop\apps\api\Program.cs` | `EnvFile` + `AddTraderIntelligence` + health/resync |
| `D:\Prop\apps\mt5-worker\Program.cs` | same DI, no `.env` |
| `D:\Prop\apps\fix-worker\Program.cs` | same DI, no `.env` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | residual 4-login scorer |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | weaker probe gate |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | process-env hydrate |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | no dummy substitute |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `GroupRequestArray("*")` / `UserRequestArray` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | re-forces `RealCopyEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | default copy flag off |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | copy note / snapshot |
| `D:\Prop\apps\api\appsettings.json` + launchSettings | no MT5 password keys |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | DemoSeeder only |
| `D:\Prop\mt5-sdk\.env.example` | template string that would pass `IsSecret` |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | key presence + census counts (no values) |
| `D:\Prop\reports\swarm\20260818\_tmp_r14_gate\Program.cs` | unused this run (`RESULT.json` missing) |

YoPips C++ product source: **0** `HasRealPasswords` hits (grep). Not the Prop dual-broker gate.

---

## 10. Bottom line

`LiveMt5Registration.HasRealPasswords` **is fail-closed on the product DI path**: missing / whitespace / exact `<SECRET>` / `(a/c` on **either** Achiever or StarwaveFX password → host **does not start** → **no** Fake connector → **no** dummy 10001 universe on the live graph.

It is **not** a cryptographic “this is a real password” check. The residual is a **narrow placeholder heuristic**, plus an ungated public factory used by the read-only probe.

For the wave goal (ALL Achiever+Starwave groups and ALL manager traders; no live cTrader orders): this slot **does not block** the census path and **cannot** send live orders.

**Do not** enable `REAL_COPY_EXECUTION_ENABLED`. **Do not** add a `35=D` sender in this task. **Do not** weaken the DI throw to “start Fake if passwords missing.”
