# W500_RESEARCH_117 — `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults

| Field | Value |
|---|---|
| Slot | **117** |
| Date | 2026-08-18 |
| Agent | W500 research 117 (flag defaults vs live-copy no-loss) |
| Assigned | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` **defaults**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Trees read | `D:\Prop` product (`src/`, `apps/`, `tests/`, docs, `.env` names+booleans only, census artifacts); `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (name search) |
| Product source modified | **No** |
| Secrets printed | **None.** `.env` quoted only as flag names `=true`/`=false`. No MT5 / FIX / proxy / DB passwords. |
| Method | `grep` + `read_file` on C# / JSON / env / architecture / YoPips. This slot did **not** live-attach Manager or FIX and did **not** `GET` loopback `/api/settings`. Census numbers are the already-measured 2026-08-18 probe. Worktree **mutated during this slot** (first read vs last read). This file is the **last measured HEAD**. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A hardcoded API `true`/`false` is a **display floor**, not a send gate. Binding `configuration["REAL_COPY_EXECUTION_ENABLED"]` **is** a runtime arm. Absence of a `NewOrderSingle` / `35=D` assembler is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE choke. `FEATURE_COPY_TRADING_ENABLED` is **not** architecture §41.

Siblings (do not collapse): `W500_RESEARCH_17.md`, `W500_RESEARCH_37.md`, `W500_RESEARCH_57.md`, `W500_RESEARCH_77.md`, `W500_RESEARCH_97.md` asked the same flag question. This file is a **re-measure of current HEAD for slot 117**. Drift vs those files is the point of §7.

---

## 0. Verdict (binding)

| Question | Measured answer (last HEAD) |
|---|---|
| Architecture / POCO **design** default for `REAL_COPY_EXECUTION_ENABLED` | **`false`** (§41 L1572, §56 L2101, `CTraderFixOptions.RealCopyExecutionEnabled = false`) |
| Local `.env` `REAL_COPY_EXECUTION_ENABLED` | **`true`** (`D:\Prop\.env` L73) |
| Local `.env` `FEATURE_COPY_TRADING_ENABLED` | **`true`** (L106) |
| API display `FEATURE_COPY_TRADING_ENABLED` | **literal `true`** (`apps/api/Program.cs` L77) |
| Does DI bind `REAL_COPY_EXECUTION_ENABLED`? | **Yes now.** `DependencyInjection.cs` L41 sets `LiveRuntimeStatus.RealCopyEnabled` from that env token |
| Does FIX logon re-pin `RealCopyEnabled=false`? | **No longer.** Pin **removed**. Logon only **logs** `RealCopyArmed={Armed}` |
| Does fetch of ALL Achiever+Starwave groups/traders consult either flag? | **No** |
| Can this process emit live cTrader `35=D` / NewOrderSingle? | **No** — builder **absent**; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; **0** `ExecutionIntent` writers |
| Risk to capital from copy path | **NONE** (this process) — `SAFE_BY_ABSENCE` |

**Slot-117 verdict: `PASS_NO_LIVE_SEND_ENV_ARMED`.**

Do **not** repeat the slot-97 sentence “both named flags default OFF in `.env` and API.” That is **stale**. Design/POCO floor for `REAL_COPY` is still **false**. Local `.env` and the API **display** for `FEATURE_COPY` are **armed true**. DI now **honors** `REAL_COPY_EXECUTION_ENABLED=true` on any host that loads that env (API does, via `EnvFile.FindAndLoad()`). Live Pepperstone fill is still impossible because there is **no** `35=D` assembler.

**Gate honesty:** safety today is **`SAFE_BY_ABSENCE` + hardcoded copy-pipeline blockers**, not “defaults are false.” Residual capital risk is **higher** than slot 97: the next engineer who adds a sender that reads `runtime.RealCopyEnabled` will find it **true** on the API host.

---

## 1. The two names are not the same control

| Name | Kind | Architecture §41? | Consumed? |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | Config **floor** for new real `NewOrderSingle` | **Yes** (L1572 default `false`; L1587 enable-example only; L2101 sample `false`) | **Yes, now bound.** `AddTraderIntelligence` L41. API `/api/settings` echoes `runtime.RealCopyEnabled`. `CopyTradingService` passes it into `RiskEngine.Evaluate` as `RealExecutionEnabled`. |
| `FEATURE_COPY_TRADING_ENABLED` | Extra “Feature Flags” env + API dictionary key | **No.** Architecture file: **0** hits. A75: do **not** invent this as an architecture name. D61: **not** a substitute for `REAL_COPY`. | **Display only.** Sole product C# *read* of the **name** is a **literal `true`** in `GET /api/settings`. Env value is loaded by `EnvFile` then **ignored**. `CopyTradingService.GetStatusAsync` hardcodes `FeatureCopyEnabled: true` and does **not** `GetValue` the env token. |

A third leftover name is still **not** the send license:

| Name | Where | Default | Live? |
|---|---|---|---|
| `FeatureFlags:LiveCopyEnabled` | `apps/api/appsettings.json` L46; dead `SettingsController` | **`false`** | **No.** `Program.cs` has **0** `MapControllers` / `AddControllers`. |

`apps/fix-worker` still reads a **fourth** key, `CTrader:RealCopyExecutionEnabled`, default **`false`**. That is **not** `REAL_COPY_EXECUTION_ENABLED`. The worker only **logs**; it still has **no** `35=D` function.

---

## 2. Measured defaults (product)

### 2.1 Architecture law (design default — unchanged)

Architecture §41 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1564–1590):

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

In-file meaning: connect, receive prices, request orders/positions, validate FIX — **without automatically placing new real orders**. The `=true` block at L1587 is the **enable example**, not a committed product value.

§56 sample repeats the floor (`L2101`): `REAL_COPY_EXECUTION_ENABLED=false`.

`FEATURE_COPY_TRADING_ENABLED` does **not** appear in that architecture file (`grep`: **0** hits).

Docs that restate the architecture floor (not binders):

- `D:\Prop\docs\architecture.md` L20
- `D:\Prop\docs\ctrader-fix.md` L73
- `D:\Prop\README.md` L28 (`REAL_COPY_EXECUTION_ENABLED=false`)

### 2.2 POCO compile-time default — still false, still unbound

`D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L32–35:

```csharp
/// When true, allow placing new orders (NewOrderSingle). Default OFF.
public bool RealCopyExecutionEnabled { get; set; } = false;
```

`Configure<CTraderFixOptions>`: **0** hits under `D:\Prop` `*.cs`. Architecture env name `REAL_COPY_EXECUTION_ENABLED` is **not** mapped onto this POCO (would need `CTrader__RealCopyExecutionEnabled`). `new CTraderFixOptions()` still yields **false**. The live runtime bit is **`LiveRuntimeStatus.RealCopyEnabled`**, not this property.

### 2.3 DI — **DRIFT**: env token is now bound

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–45 (last read):

```csharp
var runtime = new LiveRuntimeStatus
{
    RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
};
services.AddSingleton(runtime);
services.AddScoped<CopyTradingService>();
```

Slot 97 measured a **hardcoded** `RealCopyEnabled = false` plus a comment “Do not arm a flag that cannot be honored safely.” That pin is **gone**. Any host that sees `REAL_COPY_EXECUTION_ENABLED=true` in `IConfiguration` constructs the singleton **armed**.

`AddTraderIntelligence` is called by:

| Host | Loads `D:\Prop\.env` via `EnvFile.FindAndLoad()`? | Effective `RealCopyEnabled` if started from a clean VS profile |
|---|---|---|
| `apps/api/Program.cs` L10–15 | **Yes** (L10) then `AddEnvironmentVariables()` | **`true`** (`.env` L73) |
| `apps/fix-worker/Program.cs` | **No** | **`false`** unless the parent process already exported the var |
| `apps/mt5-worker/Program.cs` | **No** | **`false`** unless exported |

API is the dashboard ingest host. That is the process that matters.

`RealCopyEnabled =` assignments in product `*.cs`: **exactly one** (`DependencyInjection.cs` L41). The old logon re-pin is **absent**.

### 2.4 FIX logon — **DRIFT**: pin removed

`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L60–70 (last read):

```csharp
_runtime.Quote.LoggedOn = quote.LoggedOn;
// ... TRADE fields ...
_log.LogInformation(
    "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
    quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

There is **no** `_runtime.RealCopyEnabled = false` in this file. Slot 57/97 cited that pin at L68. Current file **logs** the armed bit and leaves it.

If `CTRADER_FIX_PASSWORD` is missing/`<SECRET>`, the service **returns at L37** and never touches the flag — API stays at the DI value (**true** when `.env` loaded).

`CTraderFixSession.BuildLogon` (`Sessions\CTraderFixSession.cs` L94–108) still emits only `(35, "A")` plus seq/Comp/Sub/time/`98`/`108`/`141`/`553`/`554`. One `WriteAsync`, one read, dispose. **No** `35=D`/`F`/`G`/`H`/`V`.

Adjacent (not this slot’s question): code fallbacks for host/account/sender changed to `demo-us-eqx-01.p.c-trader.com` / `5328266` / `demo.pepperstone.5328266` (L40–42). Env keys still override. This is **not** a send license.

### 2.5 API display floors

`GET /api/settings` (`apps/api/Program.cs` L71–84):

| Key | Binding | Effective when API loaded `.env` |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` | **`true`** |
| `FEATURE_COPY_TRADING_ENABLED` | **literal `true`** | **`true`** (ignores `.env`) |

Slot 97 had FEATURE as literal **`false`**. That display floor **flipped**.

`GET /api/health` L55 still exposes `realCopyEnabled = runtime.RealCopyEnabled`.

`GET /api/copy/status` (`Program.cs` L102) returns `CopyTradingService.GetStatusAsync`:

```43:59:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
        return new CopyGateStatus(
            FeatureCopyEnabled: true,
            RealCopyArmed: _runtime.RealCopyEnabled,
            ...
            NewOrderSingleImplemented: NewOrderSingleImplemented,  // const false
            ...
            Summary: blockers.Count == 0
                ? "All gates open — live send would be legal. Unexpected."
                : "Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle.");
```

`FeatureCopyEnabled: true` is a **status const**, not a `GetValue("FEATURE_COPY_TRADING_ENABLED")`.

This slot did **not** re-GET `:5000`. Prior same-day live GET in `SWARM_LOG` reported `REAL_COPY=false` against an older `Program.cs`. Treat that live GET as **stale vs this HEAD**.

### 2.6 FIX worker — different key, still log-only

`apps/fix-worker/Worker.cs` L21–22 / L45–46:

```csharp
var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
_logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

`apps/fix-worker/appsettings.json` is logging only. Fallback **`false`**. If `real` is true the worker **logs a warning** and still stamps `FixSessionStatus.Disconnected` / “NewOrderSingle remains off.” **No** socket send.

### 2.7 Local `.env` (names + booleans only)

`D:\Prop\.env` (gitignored):

| Line | Token (last read) |
|---|---|
| 70–72 | `CTRADER_FIX_ENABLED` / `QUOTE_ENABLED` / `TRADE_SESSION_ENABLED` = `true` |
| **73** | **`REAL_COPY_EXECUTION_ENABLED=true`** |
| **106** | **`FEATURE_COPY_TRADING_ENABLED=true`** |
| 107–109 | other `FEATURE_*` `false` except reconstruction (`true` at L110) |

Slot 97 table said both copy flags were `=false` at those same line numbers. **The file flipped.** `EnvFile.Load` (`src/Mt5/Env/EnvFile.cs` L23–39) `SetEnvironmentVariable`s every `KEY=VALUE`. API `Program.cs` L10–12 loads it, then `AddEnvironmentVariables()`, then DI L41 **reads** `REAL_COPY_EXECUTION_ENABLED`.

`.env.example`: **absent** from `D:\Prop` root. `docker-compose.yml` sets only `ASPNETCORE_ENVIRONMENT=Development` — **neither** flag. `launchSettings.json` (api + fix-worker) does **not** set either flag.

### 2.8 Dead JSON leftover

`apps/api/appsettings.json` L44–48: `FeatureFlags.LiveCopyEnabled = false`. Dead controller schema. **Not** either assigned name.

---

## 3. Fetch ALL groups + ALL manager traders is flag-blind

Neither flag is referenced in `NativeMt5BrokerConnector`, `DealIngestionService`, `LiveIngestHostedService`, `LiveMt5Registration`, or `EfDashboardQueries.GetTradersAsync` / `GetGroupsAsync`.

| Step | Code | Flag consult? |
|---|---|---|
| Brokers | `LiveMt5Registration.CreateConnectors` always builds **ACHIEVER** + **STARWAVEFX** (`BrokerCodes`) | No |
| Groups | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback (`NativeMt5BrokerConnector.cs` L155–182) | No |
| Traders | `GetAccountsAsync(null)` walks **every** group; `UserRequestArray` → `UserGetByGroup` → `UserLogins`+`UserRequestByLogins` (`L189–233`) | No |
| Persist | `UpsertGroupsBatchAsync` / `UpsertAccountsBatchAsync` | No |
| Dashboard | `GetTradersAsync` is **account-driven** left-join of all `Mt5Accounts` (`EfDashboardQueries.cs` L85–128). Optional `broker`/`state` query only. | No |
| Manual resync | `POST /api/ops/resync` loops `ACHIEVER` + `STARWAVEFX` (`Program.cs` L124) | No |

Copy flags **cannot** shrink the Manager catalog. They also **cannot** authorize a live destination order.

Prior measured census (not re-probed this slot): Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 groups / 8460 traders** (`reports/CREDENTIALS_AND_COPY_STATUS.md`, `LIVE_GROUPS_AND_TRADERS.json` ~`2026-08-18T08:42Z`). Hosted auto-score is still `ListLoginsWithDealsAsync` (deals-only). Catalog persist is **all** accounts.

---

## 4. Copy-to-cTrader cannot send live orders (no loss)

### 4.1 No `35=D` builder

Product `src/` + `apps/` `grep` for `35=D` / `(35, "D")`: **0**.

`NewOrderSingle` hits are comments, log strings, `ExecutionOrderStateMachine.MayRetryNewOrderSingle` (status predicate), and `CopyTradingService` consts. `ShadowCopyEngine.SimulateEntry` is paper.

**0** `new ExecutionIntent` / `ExecutionIntents.Add` in `*.cs`.

### 4.2 New copy pipeline is SHADOW-only even when flags are true

`CopyTradingHostedService` ticks every 20 s (`Infrastructure/Hosting/CopyTradingHostedService.cs`) and calls `GenerateShadowIntentsAsync`. Registered on **every** `AddTraderIntelligence` host (API + both workers).

Hard blockers inside `CopyTradingService`:

| Gate | Value | Effect |
|---|---|---|
| `NewOrderSingleImplemented` | **`const false`** (L16) | `BuildBlockers` always includes “No NewOrderSingle sender — SAFE_BY_ABSENCE”. Live branch at L198 cannot run. |
| `VenueReconciled` | **`const false`** (L15) | `RiskEngine.Evaluate` rejects increasing actions with `VENUE_NOT_RECONCILED` (RiskEngine.cs L84–85). |
| Persist `AllowFixSend` | **hardcoded `false`** (CopyTradingService.cs L192) | Even if `Evaluate` later returned true, the row is forced false. |
| Live branch | `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` | Unreachable. Else status = `SHADOW_ONLY`. |

`Evaluate` is now a **product caller** (slot 99 “0 Evaluate callers” is **stale**). It is still not a sender. `RealExecutionEnabled` is passed from `_runtime.RealCopyEnabled` (can be **true** on API). `allowSend` conjunction (`RiskEngine.cs` L147–150) still requires `Reconciled && VenueHealthy && RealExecutionEnabled && KillSwitch==None`. `Reconciled` is const false → `AllowFixSend` from the engine is false for opens.

Unit test `Real_flag_false_never_allows_fix_send` (`tests/Unit/RiskEngineTests.cs` L21–26) still uses `Base()` with `RealExecutionEnabled = false`. It does **not** cover the new DI bind.

`EfTradingStore.PersistDemoShadowAsync` independently writes `CopyIntent` `SHADOW_ONLY` (no risk hop). Scoring still uses that path.

### 4.3 UI

`LiveCopyPage.tsx` now **reads** `/api/copy/status` + `/api/copy/intents` (no longer a JSX-only “flag is false” sentence). It shows `REAL_COPY armed` as YES/NO from `status.realCopyArmed` and lists blockers. That page can honestly show **YES** if the API loaded `.env`. It still cannot place a Pepperstone order.

`SettingsPage.tsx` dumps `/api/settings` JSON (FEATURE literal true; REAL_COPY = runtime).

---

## 5. YoPips C++ backend — not these flags, not a cTrader sender

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm` `src/` + `config/` for `FEATURE_COPY_TRADING_ENABLED` / `REAL_COPY_EXECUTION_ENABLED` / `RealCopyExecutionEnabled`: **0** hits.

YoPips `COPY_TRADING_RESTRICTION` (`src/services/admin_approval_final_review_service.cpp` L575: `"copy_trading"` / `"allow_copy_trading"`) is **challenge-account policy detection**. It does not define these defaults and cannot emit Pepperstone `35=D` for this lab.

---

## 6. Surface matrix (slot 117 last HEAD)

| Surface | `FEATURE_COPY_TRADING_ENABLED` | `REAL_COPY_EXECUTION_ENABLED` / twin |
|---|---|---|
| Architecture §41 / §56 | **absent** | **false** (L1572 / L2101) |
| docs / README | absent | **false** |
| `CTraderFixOptions` | absent | `RealCopyExecutionEnabled = false` (unbound) |
| DI `LiveRuntimeStatus` | absent | **bound** from env token; `.env` → **true** on API |
| FIX logon hosted service | unread | **no re-pin**; logs `RealCopyArmed` |
| `GET /api/settings` | **literal `true`** | `runtime.RealCopyEnabled` → **true** if `.env` loaded |
| `GET /api/copy/status` | `FeatureCopyEnabled: true` const | `RealCopyArmed` from runtime |
| `GET /api/health` | absent | `realCopyEnabled` from runtime |
| `apps/fix-worker` | unread | `GetValue("CTrader:RealCopyExecutionEnabled", false)` — **log only** |
| `D:\Prop\.env` | **`=true` (L106), unused by GetValue** | **`=true` (L73), bound by DI** |
| `docker-compose.yml` / `launchSettings.json` | unset | unset |
| `.env.example` | file **missing** | file **missing** |
| `appsettings.json` | unset | unset (`LiveCopyEnabled=false` is a **different** name) |
| Native Manager fetch | unread | unread |
| YoPips C++ | **0** hits | **0** hits |
| `35=D` builder | n/a | **absent** |

---

## 7. Drift vs earlier same-question reports (do not greenwash)

| Earlier claim (slots 17/37/57/97) | Slot 117 last HEAD |
|---|---|
| `.env` L73 / L106 both `=false` | **Both `=true`** |
| `Program.cs` FEATURE literal `false` | FEATURE literal **`true`** (L77) |
| DI `RealCopyEnabled = false` (hard pin) | DI **binds env**; API **armed** |
| Logon service re-pins `RealCopyEnabled = false` | **Pin deleted**; only logs armed |
| `GetValue("REAL_COPY…")` count **0** | Indexer bind at `DependencyInjection.cs` L41 |
| No product `RiskEngine.Evaluate` caller | `CopyTradingService.GenerateShadowIntentsAsync` **does** Evaluate; still no sender |
| LiveCopyPage static “flag is false” | Live page shows runtime `realCopyArmed` + blockers |
| “Both defaults false” as the one-liner | **False as a complete statement.** Design/POCO still false; local env + API display + DI bind are **armed**. Send still off by **absence**. |

E038 / `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false** (forced)” is **stale vs this HEAD**.

Do **not** set either flag to `true` in a committed sample. They are **already true** in the local `.env`. Flip them **back to false** before anyone adds a sender. Do **not** treat FIX LoggedOn as a send license. Do **not** treat `FEATURE_COPY_TRADING_ENABLED` as the architecture send floor.

---

## 8. What this slot did **not** do

- Did not flip any flag (`.env` was already `true` when last read).
- Did not add a `35=D` builder.
- Did not restore the DI / logon pins (research only).
- Did not live-attach Manager or FIX (census cited from prior 08:42Z probe).
- Did not live-GET `/api/settings` (loopback not fetched).
- Did not print secrets.
- Did not edit product source.

---

## 9. One-line

```text
DESIGN: REAL_COPY default false (arch §41 L1572 / POCO=false) ; FEATURE_COPY is not §41
HEAD: .env BOTH true (L73/L106) ; API FEATURE literal true (Program.cs L77)
     ; DI now binds REAL_COPY → API RealCopyEnabled=true ; logon pin GONE
FETCH: ALL groups/traders flag-blind (GroupRequestArray("*") + GetAccountsAsync(null); prior census 18/8460)
SEND: 35=D absent ; NewOrderSingleImplemented=false ; AllowFixSend persisted false
     ; 0 ExecutionIntent writers → SAFE_BY_ABSENCE → risk to capital NONE this process
```
