# W500_RESEARCH_77 — `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults

| Field | Value |
|---|---|
| Slot | **77** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_77 |
| Assigned | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` **defaults**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** Flags were **not** flipped. |
| Secret values printed | **None.** Presence + `false`/`true` only. Passwords, proxy auth, FIX password, connection strings are not echoed. |
| C++ tree consulted | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` — **not** the copy-to-cTrader sender. **0** hits for either env token. `copy_trade_*` there is **cluster detection / restriction**, not a FIX `35=D` path. |
| Method | Re-read current product `*.cs` / `appsettings*` / `launchSettings` / `docker-compose.yml` / local `.env` (flag lines only) / architecture §41 + §56. Grep `FEATURE_COPY_TRADING_ENABLED`, `REAL_COPY_EXECUTION_ENABLED`, `RealCopyExecutionEnabled`, `LiveCopyEnabled`, `35=D`, `NewOrderSingle`, `GroupRequestArray`, `GetAccountsAsync`. Re-read `CTraderFixSession.BuildLogon` tag list. No live attach this slot. No password echo. |
| Prior same-angle pins | W500_RESEARCH_17 / 37 (same two names); E038 (settings display); A003 / A008 (env vs POCO); CREDENTIALS_AND_COPY_STATUS + INDEX live census **18 / 8460**. This slot recenses **current disk** (including deltas vs stale reports). |
| This slot live-attached | **No.** Census numbers below are **cited prior measure**, not a new Manager connect. |

**Honesty rule:** a compile-time `= false`, a `GetValue(..., false)` fallback, a hardcoded API `false`, and an env line `=false` are **defaults / display floors**, not a unit-tested refuse-on-LoggedOn-TRADE socket. `FEATURE_COPY_TRADING_ENABLED` is **not** architecture §41. `LiveCopyEnabled` is a **third** identifier. Absence of `35=D` is **`SAFE_BY_ABSENCE`**. Do **not** treat this file as §68 / §70 PASS. Do **not** flip either flag.

---

## 0. Verdict (binding)

**CONFIRMED: both named flags default `false`. Live cTrader `NewOrderSingle` cannot fire from this process. Manager catalog fetch of ALL groups / ALL traders is the only live I/O that is implemented.**

| Flag | Default on disk | Who owns it | Bound to send? |
|---|---|---|---|
| `FEATURE_COPY_TRADING_ENABLED` | **`false`** | Local `.env` L106 + API **literal** `false` (`apps/api/Program.cs` L76) | **No.** Env is loaded into the process (`EnvFile`) and **never `GetValue`d**. Only product C# hit is the settings dictionary literal. **Not** in architecture §41 (`0` hits in `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`). |
| `REAL_COPY_EXECUTION_ENABLED` | **`false`** | Architecture §41 L1572 / §56 L2101; `.env` L73; `CTraderFixOptions.RealCopyExecutionEnabled = false`; `LiveRuntimeStatus.RealCopyEnabled` **pinned false** in DI and **re-pinned false** after FIX logon | **Not a wired choke on a sender.** Worker reads a **different** key (`CTrader:RealCopyExecutionEnabled`, fallback `false`). Architecture env name is **not** mapped onto the POCO (would need `CTrader__RealCopyExecutionEnabled`). Send is **`SAFE_BY_ABSENCE`**: no `35=D` builder exists. |

One-line:

```text
FEATURE_COPY_TRADING_ENABLED=false (env L106 + API literal L76; unread)
AND REAL_COPY_EXECUTION_ENABLED=false (env L73 + POCO + DI pin + logon re-pin)
AND no function emits 35=D (BuildLogon is 35=A only)
THEREFORE copy-to-cTrader cannot open a live losing position
AND ALL-group / ALL-trader Manager fetch remains legal (read-only).
```

| Claim | Measured | Class |
|---|---|---|
| `FEATURE_COPY_TRADING_ENABLED` default is false | **Yes** — `.env` `false`; `/api/settings` **literal** `false` | display floor |
| That env token is a coded send gate | **No** — **0** `GetValue("FEATURE_COPY_TRADING_ENABLED")` in product `*.cs` | **unbound** |
| `REAL_COPY_EXECUTION_ENABLED` default is false | **Yes** — every product surface is `false` or fallback `false`; **0** product `=true` | `EXISTS_AND_GOOD` vs §41 |
| That env token is bound to `CTraderFixOptions` / worker | **No** — worker key is `CTrader:RealCopyExecutionEnabled`; POCO is **not** `Configure<>`d | `GATE_INCOMPLETE` |
| Runtime can be armed by flipping `.env` to `true` | **No** — DI L41 + logon L68 **force** `RealCopyEnabled = false` | pin |
| Product can emit FIX `35=D` today | **No** — `CTraderFixSession` builds **only** `35=A`; no QuickFIX package | **`SAFE_BY_ABSENCE`** |
| Fetch ALL Achiever + Starwave groups/traders | **Implemented** as Manager **read**; prior live census **18 groups / 8460 traders** | read-only |
| Copy to Pepperstone/cTrader live orders | **Forbidden today** | both flags stay false |
| Safe to set either flag `true` | **No** | next engineer who adds a sender must find them **off** |
| This file is §68 / §70 PASS | **No** | go-live still **0/19** / live send still **0** |

---

## 1. Goal split (do not collapse)

The user goal is two independent jobs:

| Job | Live I/O allowed? | Capital at risk? |
|---|---|---|
| **A.** Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders | **Yes** — native Manager **read** (`GroupRequestArray("*")` + `GroupTotal`/`GroupNext` fallback + `UserRequestArray` / `UserLogins` / `UserRequestByLogins`) | **No** (no destination order) |
| **B.** Copy those traders onto cTrader | **Not yet** — SHADOW / CopyIntent only (`Status = "SHADOW_ONLY"`) | **Would be yes** the moment `35=D` exists |

Job A does **not** license Job B. Architecture §41: sessions may connect; `NewOrderSingle` requires `REAL_COPY_EXECUTION_ENABLED=true` **plus** a healthy risk engine. §68 / §70 remain unchecked. Neither flag is consulted by the Manager walk.

```text
MT5 Manager census (read)     = ALLOWED now
FIX 35=A Logon (QUOTE/TRADE)  = allowed for session proof / future recon
FIX 35=H / AF / AN            = Phase 7 recon (not built)
FIX 35=D NewOrderSingle       = FORBIDDEN until gates + REAL_COPY=true
FIX 35=F / G cancel/replace   = FORBIDDEN (same license)
FEATURE_COPY_TRADING_ENABLED  = inventory / UI name only; not a send license
FeatureFlags:LiveCopyEnabled  = third name; dead SettingsController; not §41
```

---

## 2. `FEATURE_COPY_TRADING_ENABLED` — measured default **false**, unbound

### 2.1 Product C# — one hit, a display literal

Grep of `FEATURE_COPY_TRADING_ENABLED` against product `*.cs` (hosts under `D:\Prop\apps` + `D:\Prop\src`, not reports):

| File | Line | What it does |
|---|---:|---|
| `D:\Prop\apps\api\Program.cs` | 76 | `["FEATURE_COPY_TRADING_ENABLED"] = false` inside `GET /api/settings` |

That is the **only** product C# occurrence. There is **no** `GetValue("FEATURE_COPY_TRADING_ENABLED")`, **no** POCO property, **no** options binder.

```70:83:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
    brokerConfigs = new[]
    {
        new { id = "ACHIEVER", name = "Achiever", enabled = true },
        new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true }
    }
}));
```

Contrast: `REAL_COPY_EXECUTION_ENABLED` on the same payload is **not** a literal — it is `runtime.RealCopyEnabled` (which DI + logon pin **false**; §3). `FEATURE_COPY_TRADING_ENABLED` **cannot** become `true` without a source edit, even if `.env` is flipped.

`GET /api/health` (L54) exposes `realCopyEnabled = runtime.RealCopyEnabled` and does **not** mention `FEATURE_COPY_TRADING_ENABLED`.

### 2.2 Local `.env` — present, loaded, unused

`D:\Prop\.env` Feature Flags block (values only; no secrets):

```text
FEATURE_COPY_TRADING_ENABLED=false          # L106
FEATURE_CTRADER_HEDGING_ENABLED=false       # L107
FEATURE_ML_SCORING_ENABLED=false            # L108
FEATURE_NEWS_FILTER_ENABLED=false           # L109
FEATURE_TRADE_RECONSTRUCTION_ENABLED=true   # L110  (reconstruction, not send)
```

`EnvFile.FindAndLoad()` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–40) copies **every** `KEY=value` into the process, including a hard path `D:\Prop\.env`. That does **not** bind the key to a POCO. No product type has a `CopyTradingEnabled` property.

### 2.3 Not an architecture name

Grep of `FEATURE_COPY_TRADING_ENABLED` in `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`: **0** hits.

A75 already warned: do **not** invent this as a §56 deliverable. D61: it is **not** a substitute for `REAL_COPY_EXECUTION_ENABLED`.

`docs/architecture.md` L20 lists only `REAL_COPY_EXECUTION_ENABLED=false`. `docs/ctrader-fix.md` L73 and `docs/deployment.md` L82 same. **Zero** `FEATURE_COPY_*` in `D:\Prop\docs`.

### 2.4 Not in committed host config

| Surface | `FEATURE_COPY_TRADING_ENABLED` |
|---|---|
| `apps/api/appsettings.json` | **absent** (has `FeatureFlags:LiveCopyEnabled=false` instead) |
| `apps/api/appsettings.Development.json` | **absent** |
| `apps/fix-worker/appsettings.json` | logging only |
| `apps/fix-worker/launchSettings.json` | `DOTNET_ENVIRONMENT=Development` only |
| `apps/api/Properties/launchSettings.json` | `ASPNETCORE_ENVIRONMENT=Development` only |
| `D:\Prop\docker-compose.yml` | **no** feature-flag env (api service sets only `ASPNETCORE_ENVIRONMENT`) |
| `D:\Prop\.env.example` | **does not exist** (INDEX: deleted in worktree) |
| `D:\Prop\mt5-sdk` | **0** hits |

Flipping `FEATURE_COPY_TRADING_ENABLED=true` in `.env` **cannot** change `/api/settings` (literal) and **cannot** create a send path.

---

## 3. `REAL_COPY_EXECUTION_ENABLED` — measured default **false**, pin + absence

### 3.1 Architecture law (§41 / §56)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`:

```1568:1590:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

This allows connecting / prices / requesting orders/positions / validating connectivity
without automatically placing new real orders.

Actual NewOrderSingle submission should require REAL_COPY_EXECUTION_ENABLED=true
plus runtime risk-engine healthy state.
```

§56 example block L2101: `REAL_COPY_EXECUTION_ENABLED=false`.

README L28 and `docs/architecture.md` L20 restate the same floor.

### 3.2 Local `.env` L73

```text
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false          # L73
```

ASP.NET Core env convention would bind `REAL_COPY_EXECUTION_ENABLED` to configuration key `REAL_COPY_EXECUTION_ENABLED` — **not** to `CTrader:RealCopyExecutionEnabled`. Binding the POCO would require `CTrader__RealCopyExecutionEnabled`. **That twin is not in `.env`.**

### 3.3 POCO default — `false`, unused by hosts

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

Grep of `Configure<CTraderFixOptions>` / `new CTraderFixOptions`: **0** in product. `CTraderQuoteService` accepts the type but is **never constructed**. Logon host reads `CTRADER_FIX_*` keys **directly** (`CTraderFixLogonHostedService.cs` L33–58) and never inspects this property.

### 3.4 Runtime pin — forced `false` twice

DI constructs the singleton already off:

```38:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
        services.AddSingleton(runtime);
```

After optional QUOTE/TRADE TLS logon, the host **overwrites** any mutation:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        ...
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        ...
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

`LiveRuntimeStatus.Snapshot()` (`D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` L41–44) labels the false case: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

Overview DTO (`EfDashboardQueries.cs` L52) reports `_runtime.RealCopyEnabled` — the same pinned false.

### 3.5 FIX worker — different key, log-only, no socket

```19:48:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

| Fact | Measured |
|---|---|
| Key actually read | `CTrader:RealCopyExecutionEnabled` |
| Fallback | **`false`** |
| Present in `apps/fix-worker/appsettings.json` | **No** (logging only) |
| Present in `apps/api/appsettings.json` | **No** (stale reports that quoted `"RealCopyExecutionEnabled": false` under `CTrader` are **wrong on current disk**; current API JSON has `CTraderFix` host/ports + `FeatureFlags.LiveCopyEnabled=false`) |
| Effect if forced `true` | **warning log only**; loop still stamps `Disconnected`; **no** `WriteAsync` of `35=D` |
| Worker also calls `AddTraderIntelligence` | **Yes** (`apps/fix-worker/Program.cs` L7) — so DI pin + logon service apply here too |

### 3.6 Risk engine — AllowFixSend follows `RealExecutionEnabled`, but **no product caller**

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

That `if` is an **empty comment**. The actual send bit is computed later:

```147:170:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        ...
                AllowFixSend = allowSend
```

Unit test `Real_flag_false_never_allows_fix_send` (`tests/Unit/RiskEngineTests.cs` L21–26) uses `Base()` with `RealExecutionEnabled = false` (L72) and asserts `AllowFixSend == false`.

Grep of `Evaluate(` / `new RiskEngine` in product `*.cs`: **only** `RiskEngine.cs` itself. **Zero** host callers. `AddTraderIntelligence` does **not** register `RiskEngine`. So this is a **library default**, not a live choke in front of a socket.

### 3.7 UI

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` L5 is a **static** sentence: SHADOW only; NewOrderSingle disabled; names `REAL_COPY_EXECUTION_ENABLED` as a still-required gate. It does **not** read `FEATURE_COPY_TRADING_ENABLED` or `useSettings`.

---

## 4. Three names, none of them a live send license

| Identifier | Kind | Default | Wired to `35=D`? |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | architecture §41 env | **false** (`.env` L73) | **No** (unbound to POCO / worker) |
| `CTrader:RealCopyExecutionEnabled` / `CTraderFixOptions.RealCopyExecutionEnabled` | nested config / POCO | **false** | **No** (log / unused object) |
| `LiveRuntimeStatus.RealCopyEnabled` | process singleton | **false** (DI + logon pin) | **No** (display + snapshot) |
| `FEATURE_COPY_TRADING_ENABLED` | extra Feature Flags env + API key | **false** (literal) | **No** (unread) |
| `FeatureFlags:LiveCopyEnabled` | API `appsettings.json` L46 | **false** | **No** |

Dead twin: `D:\Prop\apps\api\Controllers\SettingsController.cs` reads `FeatureFlags:LiveCopyEnabled` default **false** (L38) and would PUT Redis `settings:flags:live_copy`. Product `Program.cs` has **0** `AddControllers` / `MapControllers` (grep across `D:\Prop\**\*.cs`: **0**). The live route is the **minimal-API** `MapGet("/api/settings")`. The controller cannot arm send.

`GET /api/reconciliation/status` (Program.cs L62–68) is a stub that still says `"NewOrderSingle still off"`.

---

## 5. `SAFE_BY_ABSENCE` — no `35=D` builder

`CTraderFixSession.BuildLogon` (`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` L89–109) emits **only**:

| Tag | Value |
|---|---|
| 35 | **`A`** (Logon) |
| 34 | seq |
| 49 / 56 / 50 / 57 | Comp/Sub IDs |
| 52 | UTC sending time |
| 98 / 108 / 141 | Encrypt / heartbeat / reset |
| 553 / 554 | username / password |

One `ssl.WriteAsync` of that logon (L49). Reply is parsed for `35=A` vs reject. Socket is disposed (`using TcpClient` / `await using SslStream`). **No** keep-alive, **no** `35=D`, **no** `35=F`, **no** `35=G`, **no** `38=` OrderQty.

`TraderIntelligence.Fix.CTrader.csproj` has **no** QuickFIX/n package — only Hosting/Configuration/Logging/EF abstractions.

Product `*.cs` hits for `NewOrderSingle` under `D:\Prop\src` are comments, log strings, `MayRetryNewOrderSingle` FSM helper, and the POCO xmldoc. **Zero** message builders.

`EfTradingStore.PersistDemoShadowAsync` writes `CopyIntent.Status = "SHADOW_ONLY"` and a simulated `ShadowOrder` (`EfTradingStore.cs` L295–321). That is **database shadow**, not a venue order.

Therefore: even if every flag were `true`, **this tree still cannot place a Pepperstone order**. That is **vacuous safety**, not a passed go-live gate. Keep the flags **false** so the first future sender is fail-closed.

---

## 6. Job A — fetch ALL Achiever + Starwave groups and ALL manager traders

Neither flag is read by ingest. Catalog does **not** filter on `FEATURE_COPY_*` or `REAL_COPY_*`.

### 6.1 Both brokers are registered

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) always returns **two** native connectors: `BrokerCodes.Achiever` + `BrokerCodes.StarwaveFx` (`D:\Prop\src\Domain\Brokers\BrokerCodes.cs` L5–6).

`HasRealPasswords` fail-closes dummy/empty/`<SECRET>` on **both** `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` before DI proceeds (DependencyInjection.cs L35–36). This slot did not print those values.

Starwave `ProxyEnabled` is **hardcoded `false`** (L45). Achiever proxy comes from `ACHIEVER_PROXY_*`. Irrelevant to the flag question; relevant to *how* ALL groups are reached.

### 6.2 Groups = Manager `*` + pump fallback

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

1. `GroupRequestArray("*", arr)` — **all** manager-visible groups.
2. If that list is empty: `GroupTotal` + `GroupNext` pump-cache walk.

No plan-group allowlist is applied at fetch time (`docs/architecture.md` L24: plan mappings are labels, not fetch filters).

### 6.3 Traders = every group, `GetAccountsAsync(null)`

`GetAccountsCore(null)` (L189–213) walks **every** group from `GetGroupsCore()` and unions logins via:

- `UserRequestArray(gname)`
- fallback `UserGetByGroup`
- if still empty: `UserLogins` + `UserRequestByLogins`

`DealIngestionService.SyncCatalogAsync` (L45–49) calls `GetGroupsAsync` then `GetAccountsAsync(null)` and batch-upserts. `LiveIngestHostedService` runs that for **each** registered connector (ACHIEVER then STARWAVEFX). Manual `POST /api/ops/resync` (Program.cs L121) iterates the same two codes.

`GetTradersAsync` (`EfDashboardQueries.cs` L85–128) iterates **`foreach (var account in accounts)`** with **no `Take()`**. Optional query-string `broker` / `state` filters the **response**, not the catalog write.

### 6.4 Scoring is a subset; listing is ALL

Auto-score in `LiveIngestHostedService` L106 uses `ListLoginsWithDealsAsync` (deals table, not every login). Manual resync scores `ListLoginsAsync` (every persisted account). **Fetch/list ALL traders is implemented.** Scoring every login is **not** the same job and is **not** required to keep capital safe.

### 6.5 Prior live census (not re-run this slot)

From `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` and `reports/INDEX.md` (remeasured 2026-08-18):

| Broker | Connect | Groups | Traders |
|---|---|---:|---:|
| Achiever | OK via whitelist HTTP proxy | 8 | 6512 |
| StarwaveFX | OK direct | 10 | 1948 |
| **Total** | | **18** | **8460** |

Dashboard `/api/traders` returned **8460**. `/api/groups` returned **18**. `LiveBrokerProbe` uses the same `GetAccountsAsync(null)` (`tools/LiveBrokerProbe/Program.cs` L25–26).

This slot **did not** re-attach. Treat 18/8460 as the last measured pin, not a new count.

---

## 7. YoPips C++ backend — not the cTrader copy sender

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm`.

| Search | Hits |
|---|---|
| `FEATURE_COPY_TRADING_ENABLED` | **0** (src + config + whole tree) |
| `REAL_COPY_EXECUTION_ENABLED` | **0** |
| `cTrader` / `NewOrderSingle` / `35=D` / `Pepperstone` / `FIX.4` in `src\` | **0** |
| `copy_trade_*` | **yes** — `copy_trade_clusters` / `copy_trade_cluster_members` / `COPY_TRADING_RESTRICTION` in admin risk analytics |

That `copy_trade_*` surface is **challenge-account cluster detection / restriction** (`admin_approval_final_review_service.cpp`, `admin_v2_risk_analytics_controller.cpp`). It is **not** a Pepperstone FIX adapter and **cannot** send `35=D`.

`D:\Prop\mt5-sdk` (sibling Manager helper) also has **0** hits for either flag.

---

## 8. Flip matrix (do not execute)

| Operator action | `/api/settings` FEATURE_COPY | `/api/settings` REAL_COPY | Wire `35=D` |
|---|---|---|---|
| Leave defaults | **false** (literal) | **false** (pin) | **none** |
| `.env` `FEATURE_COPY_TRADING_ENABLED=true` | still **false** | still **false** | **none** |
| `.env` `REAL_COPY_EXECUTION_ENABLED=true` | still **false** | still **false** (DI/logon pin) | **none** |
| env `CTrader__RealCopyExecutionEnabled=true` | still **false** | still **false** | worker **logs** warning; still **none** |
| `FeatureFlags:LiveCopyEnabled=true` + dead PUT | unused | unused | **none** |
| Future: add a `35=D` builder while any send-named flag is `true` | — | — | **capital at risk** |

**Do not enable** either flag. Safety today is default-false + pin-false + missing sender. The first sender must inherit **false**.

---

## 9. Residuals (honest, not greenwash)

1. **`GATE_INCOMPLETE`.** Architecture env name ≠ worker key ≠ POCO binder. A future `if (options.RealCopyExecutionEnabled)` on a new sender could miss the env token operators actually set.
2. **Empty `if (RealExecutionEnabled == false)`** in `RiskEngine` does not reject; it relies on `AllowFixSend` later. Fine while there is no caller.
3. **`SettingsController`** is a landmine if someone later adds `MapControllers()` — third flag name, Redis write, route clash with minimal API.
4. **Stale reports** that claim `apps/api/appsettings.json` contains `CTrader:RealCopyExecutionEnabled` are **out of date**. Current file has `CTraderFix` + `FeatureFlags.LiveCopyEnabled` only.
5. **No `.env.example`** on disk to lock the false floor for new clones.
6. **This is not §68 / §70.** LoggedOn (prior measure) ≠ license to send.

None of these residuals place an order **today**.

---

## 10. Files read (this slot)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\Controllers\SettingsController.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\appsettings.Development.json`
- `D:\Prop\apps\api\Properties\launchSettings.json`
- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\apps\fix-worker\Properties\launchSettings.json`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (shadow + `ListLogins*`)
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\docker-compose.yml`
- `D:\Prop\README.md`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (§41, §56)
- `D:\Prop\.env` (flag lines only; L73, L106–110)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (prior census)
- `D:\Prop\reports\INDEX.md` (census pin + `.env.example` deleted)
- YoPips: `src\` + `config\` name search only; no flag files to quote

---

## 11. JSON (slot contract)

```json
{
  "slot": 77,
  "verdict": "PASS_DEFAULTS_FALSE_NO_LIVE_SEND",
  "evidence": "FEATURE_COPY_TRADING_ENABLED default false (API literal Program.cs L76; .env L106; never GetValue'd; 0 hits in architecture). REAL_COPY_EXECUTION_ENABLED default false (arch §41 L1572 / §56 L2101; .env L73; CTraderFixOptions.RealCopyExecutionEnabled=false; DI L41 + FIX logon L68 pin RealCopyEnabled=false; worker GetValue CTrader:RealCopyExecutionEnabled false). No product =true. No 35=D builder (logon 35=A only; no QuickFIX). Ingest GroupRequestArray(*) + GetAccountsAsync(null) for ACHIEVER+STARWAVEFX ignores both flags. Prior census 18/8460. YoPips C++ 0 hits for either token.",
  "risk_to_capital": "NONE"
}
```
