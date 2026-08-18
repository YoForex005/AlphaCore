# W500_RESEARCH_137 — `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults

| Field | Value |
|---|---|
| Slot | **137** |
| Date | 2026-08-18 |
| Agent | W500 research 137 (flag defaults vs live-copy no-loss) |
| Assigned | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Trees read | `D:\Prop` product (`src/`, `apps/`, `tests/`, docs, `.env` names+booleans only, census artifacts); `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (name search) |
| Product source modified | **No** |
| Secrets printed | **None.** `.env` quoted only as flag names `=true`/`=false`. No MT5 / FIX / proxy / DB passwords. |
| Method | `grep` + `read_file` on C# / JSON / env / architecture / YoPips. This slot did **not** live-attach Manager or FIX. Local `GET http://127.0.0.1:5000/api/settings` was **not** executed. Census numbers are from the already-measured 2026-08-18 `LiveBrokerProbe`. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A hardcoded API `true` is a **display/pipeline floor**, not a send gate. Binding env `REAL_COPY_EXECUTION_ENABLED=true` onto `LiveRuntimeStatus.RealCopyEnabled` **arms a bit**; it does **not** create a `NewOrderSingle`. Absence of a `35=D` assembler is **`SAFE_BY_ABSENCE`**. `FEATURE_COPY_TRADING_ENABLED` is **not** architecture §41.

Siblings (do not collapse): `W500_RESEARCH_17.md`, `W500_RESEARCH_37.md`, `W500_RESEARCH_57.md`, `W500_RESEARCH_97.md`, `W500_RESEARCH_117.md` asked the same flag question. This file is a **re-measure of current HEAD for slot 137**, not a copy. Drift vs those files is called out in §7. Later residual notes in `W500_RESEARCH_118.md` / `123.md` / `124.md` / `126.md` already flagged the DI bind + lab `.env=true`; this slot measures the **two named flags** end-to-end. Slot 117 already landed the same verdict class (`PASS_NO_LIVE_SEND_ENV_ARMED`); 137 independently re-read HEAD and agrees.

---

## 0. Verdict (binding)

| Question | Measured answer |
|---|---|
| Architecture / POCO / worker-fallback **defaults** | `FEATURE_COPY` **absent from §41**. `REAL_COPY` **false** (arch L1572; `CTraderFixOptions.RealCopyExecutionEnabled=false`; worker `GetValue(..., false)`). |
| Product C# **display / pipeline** values | `FEATURE_COPY_TRADING_ENABLED` **literal `true`** in `GET /api/settings` and `CopyTradingService.FeatureCopyEnabled`. |
| Lab `.env` (gitignored operator file) | **Both `=true`** (`REAL_COPY` L73, `FEATURE_COPY` L106). |
| Does DI still **pin** `RealCopyEnabled=false`? | **No.** `DependencyInjection` now **binds** `configuration["REAL_COPY_EXECUTION_ENABLED"]=="true"`. Logon host **does not** re-pin false. |
| Does fetch of ALL Achiever+Starwave groups/traders consult either flag? | **No** |
| Can this process emit live cTrader `35=D` / NewOrderSingle? | **No** — builder **absent**; persist `AllowFixSend=false`; `NewOrderSingleImplemented=false`; `VenueReconciled=false`; `0` `ExecutionIntent` writers |
| Risk to capital from copy path | **NONE** (this process) — `SAFE_BY_ABSENCE` |

**Slot-137 verdict: `PASS_NO_LIVE_SEND_ENV_ARMED`.**

Do **not** repeat slot-57/97/108 language (“both flags default false / DI+logon pin / `.env=false`”). That is **stale**. Current HEAD:

1. **Shadow copy pipeline is ON** (`FEATURE_COPY` display+hosted tick hardcoded; env also `true`; env is **never** `GetValue`’d).
2. **Operator REAL_COPY is armed** in lab `.env` and **will set** `LiveRuntimeStatus.RealCopyEnabled=true` when the API host loads that file.
3. **Live Pepperstone fill is still impossible** because there is no `35=D` assembler. The flag is necessary-not-sufficient and today is **not** the choke that keeps capital safe.

Manager catalog ingest is **read-only** (`GroupRequestArray("*")` + `UserRequestArray` / fallbacks) and is **not** gated by either flag, so the “fetch ALL groups + ALL manager traders” goal is not blocked by copy flags.

**Gate honesty:** `REAL_COPY_EXECUTION_ENABLED` is still **not** bound onto `CTraderFixOptions` (`Configure<CTraderFixOptions>` = **0** hits; worker reads the **different** key `CTrader:RealCopyExecutionEnabled`). `FEATURE_COPY_TRADING_ENABLED` has **0** `GetValue` / `configuration["FEATURE_COPY…"]` hits. Safety today is **`SAFE_BY_ABSENCE` + persist `AllowFixSend=false` + `NewOrderSingleImplemented=false`**, not a unit-tested refuse of `35=D` on a LoggedOn TRADE socket.

---

## 1. The two names are not the same control

| Name | Kind | Architecture §41? | Consumed by a worker? |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | Operator **arm** for new real `NewOrderSingle` | **Yes** (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1572 default `false`, L1587 enable-example only, L2101 sample `false`) | **Yes, as a runtime bit only.** `AddTraderIntelligence` copies env onto `LiveRuntimeStatus.RealCopyEnabled`. FIX worker reads **`CTrader:RealCopyExecutionEnabled`** (default `false`) and **only logs**. Copy service uses the runtime bit for status + risk request, then **overwrites** persist `AllowFixSend=false`. |
| `FEATURE_COPY_TRADING_ENABLED` | Extra “Feature Flags” env + API dictionary key | **No.** Architecture file: **0** hits. A75: do **not** invent this as an architecture name. | **No env bind.** Sole product C# assignment is a **literal `true`** in `GET /api/settings`. `CopyTradingService` hardcodes `FeatureCopyEnabled: true`. `CopyTradingHostedService` ticks SHADOW intents with **no flag check**. |

A third leftover name exists and is also **not** the send license:

| Name | Where | Default | Live? |
|---|---|---|---|
| `FeatureFlags:LiveCopyEnabled` | `apps/api/appsettings.json` L46; dead `SettingsController` | **`false`** | **No.** Product `*.cs` has **0** `MapControllers` / `AddControllers`. Controller PUT would write Redis `settings:flags:live_copy` if mapped — it is not. |

Flipping `FEATURE_COPY_TRADING_ENABLED` in `.env` **cannot** change `/api/settings` (literal `true`) and **cannot** create a send path.

Flipping `REAL_COPY_EXECUTION_ENABLED=true` in `.env` **does** set `runtime.RealCopyEnabled` and therefore `/api/settings` + `/api/health` + `/api/copy/status.realCopyArmed`. It still **cannot** set `CTraderFixOptions.RealCopyExecutionEnabled` (wrong key / no binder) and **cannot** emit `35=D`. Even if `CTrader:RealCopyExecutionEnabled=true`, `apps/fix-worker` only **logs a warning** and still has **no** `35=D` function.

---

## 2. Measured defaults (product)

### 2.1 Architecture law (design default)

Architecture §41 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1564–1590):

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

Meaning stated in-file: connect, receive prices, request orders/positions, validate FIX — **without automatically placing new real orders**. The `=true` block at L1587 is the **enable example**, not a committed product value.

§56 sample repeats the floor (`L2101`): `REAL_COPY_EXECUTION_ENABLED=false`.

`FEATURE_COPY_TRADING_ENABLED` does **not** appear in that architecture file (`grep`: **0** hits).

Docs that restate the architecture floor (not product binders; now **stale vs lab `.env`**):

- `D:\Prop\docs\architecture.md` L20
- `D:\Prop\docs\ctrader-fix.md` L73
- `D:\Prop\docs\deployment.md` L82
- `D:\Prop\README.md` L28
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` L30 (`false (forced)` — **stale**; DI no longer forces)

### 2.2 C# initializer — send-license property (still fail-closed)

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

`Configure<CTraderFixOptions>`: **0** hits under `D:\Prop` `*.cs`. The POCO default is unused by a binder today.

### 2.3 Runtime bind (API / ingest host) — **env now honored**

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

This is the **slot-137 drift vs 57/97/108**. There is no comment pinning the bit false. Any host that calls `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` will arm `RealCopyEnabled` when `.env` says `true`.

`LiveRuntimeStatus.RealCopyEnabled` default on the POCO is C# `false` (`bool` field). Snapshot copy-note when armed (`LiveRuntimeStatus.cs` L42–44):

> `REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.`

When false: `NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.`

### 2.4 FIX logon hosted service — **no longer re-pins false**

`CTraderFixLogonHostedService` (`src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` L60–70) writes QUOTE/TRADE logon bits, then logs `RealCopyArmed={Armed} NewOrderSingle still unimplemented`. There is **no** `_runtime.RealCopyEnabled = false` assignment (slot-57 L116 is gone).

`TryLogonAsync` is **one** `35=A` write + one read + dispose. It does not consult either copy flag.

### 2.5 Live settings API (the host operators actually hit)

`apps/api/Program.cs` L8–13 loads `D:\Prop\.env` then `AddEnvironmentVariables()`. Maps **minimal** `GET /api/settings`. There is **no** `AddControllers` / `MapControllers` anywhere under `D:\Prop` (`grep` on `*.cs`: **0**). The MVC `SettingsController` is **dead code**.

```71:77:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

| Flag in `/api/settings` | Source | Effective value when API loads lab `.env` |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` | **`true`** (DI bind of env L73) |
| `FEATURE_COPY_TRADING_ENABLED` | **literal `true`** | **`true`** (ignores `.env`) |

`/api/health` also exposes `realCopyEnabled = runtime.RealCopyEnabled` (`Program.cs` L55).

`CopyTradingService.GetStatusAsync` hardcodes `FeatureCopyEnabled: true` and `RealCopyArmed: _runtime.RealCopyEnabled` (`CopyTradingService.cs` L44–45). Summary when any blocker remains: `Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle.`

### 2.6 FIX worker — different key, log-only

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

`apps/fix-worker/appsettings.json` / `appsettings.Development.json` contain **logging only**. No `CTrader` section. `GetValue` fallback is therefore **`false`** unless an operator injects `CTrader__RealCopyExecutionEnabled`. That flip still cannot emit `35=D`. The worker stamps QUOTE/TRADE `Disconnected` every 15 s.

### 2.7 Local `.env` (names + booleans only)

`D:\Prop\.env` (gitignored; values not printed except these flags):

| Line | Token |
|---|---|
| 73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| 106 | `FEATURE_COPY_TRADING_ENABLED=true` |
| 107–109 | other `FEATURE_*` `false` except reconstruction |
| 110 | `FEATURE_TRADE_RECONSTRUCTION_ENABLED=true` |

`CTrader:RealCopyExecutionEnabled` / `CTrader__RealCopyExecutionEnabled`: **absent** from `.env`.

`EnvFile.FindAndLoad()` (`src/Mt5/Env/EnvFile.cs`) walks to `D:\Prop\.env` and `SetEnvironmentVariable`s **every** `KEY=VALUE`. API `Program.cs` L8–13 calls `FindAndLoad()` then `AddEnvironmentVariables()`. That **does** surface `REAL_COPY_EXECUTION_ENABLED` as a top-level config key, and DI **now reads it**.

`.env.example`: **deleted** in this worktree (INDEX notes D62 / A103). `docker-compose.yml` does **not** set either flag. `apps/api/Properties/launchSettings.json` does **not** set either flag.

### 2.8 Committed JSON leftover (not the architecture name)

`apps/api/appsettings.json` L44–48:

```json
  "FeatureFlags": {
    "ShadowTradingEnabled": true,
    "LiveCopyEnabled": false,
    "AutoPromotionEnabled": false
  }
```

This is the **dead** controller schema. It is **not** `REAL_COPY_EXECUTION_ENABLED` and **not** `FEATURE_COPY_TRADING_ENABLED`.

---

## 3. Fetch ALL Achiever + Starwave groups and manager traders — flags are irrelevant

### 3.1 Registration (both brokers, password-gated, flag-blind)

`LiveMt5Registration.CreateConnectors` always returns **two** `NativeMt5BrokerConnector` instances (`ACHIEVER`, `STARWAVEFX`). It reads `MT5_*` / `ACHIEVER_PROXY_*` / `MT5_STARWAVEFX_*` only. **Zero** reads of either copy flag. Starwave `ProxyEnabled` is hardcoded `false`.

`AddTraderIntelligence` **throws** if both manager passwords fail `IsSecret`. Dummy/Fake path is disabled on the live host.

### 3.2 Catalog walk (ALL groups, ALL logins the manager can see)

`DealIngestionService.SyncCatalogAsync` (`src/Application/Ingestion/DealIngestionService.cs` L38–51):

1. `connector.GetGroupsAsync(ct)`
2. `connector.GetAccountsAsync(null, ct)` — `null` group means **every** group just fetched

`NativeMt5BrokerConnector.GetGroupsCore` (`L144–183`):

1. `GroupRequestArray("*", arr)`
2. Fallback: `GroupTotal` + `GroupNext`

`GetAccountsCore` (`L189–213`) with `group == null` walks **every** group name. Per group `ReadAccountsForGroup` (`L216–271`):

1. `UserRequestArray(gname, users)`
2. Fallback `UserGetByGroup`
3. If still empty: `UserLogins` + `UserRequestByLogins`
4. Account money fields: `UserAccountRequestArray` / `UserAccountGetByGroup`

No `Take(` / `Skip` on this walk. No flag check.

`LiveIngestHostedService` iterates `registry.All()` (both connectors) and calls `SyncCatalogAsync` then `SyncBrokerAsync` without consulting copy flags. Hosted **score** is `ListLoginsWithDealsAsync` only (catalog still holds the rest as `INSUFFICIENT_DATA`). `/api/ops/resync` does the same catalog+deals walk for `["ACHIEVER", "STARWAVEFX"]`.

Dashboard `GetGroupsAsync` / `GetTradersAsync` (`EfDashboardQueries.cs` L70–128) materialize **all** `Mt5Groups` / `Mt5Accounts` with a scores left-join. Traders endpoint has **no** `Take`.

`tools/LiveBrokerProbe/Program.cs` L25–26 uses the same `GetGroupsAsync` + `GetAccountsAsync(null)` pair.

### 3.3 Already-measured census (this slot did not re-attach)

From `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` and `reports/CREDENTIALS_AND_COPY_STATUS.md` (2026-08-18):

| Broker | Connect | Groups | Manager traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Artifact (logins, no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`.

Copy flags did not, and cannot, shrink that walk.

---

## 4. Copy-to-cTrader cannot send live orders (no loss)

### 4.1 What FIX actually sends

`CTraderFixSession.BuildLogon` (`src/Fix.CTrader/Sessions/CTraderFixSession.cs` L89–109) assembles **only**:

- `35=A` (Logon)
- `34` seq, `49/56` CompIDs, `50/57` SubIDs, `52` time
- `98=0`, `108=30`, `141=Y`
- `553` username, `554` password

It writes that one frame (`WriteAsync` L49), reads one reply, returns. **No** `35=D`, `F`, `G`, `H`, `V`. Sockets disposed via `using`.

Product `src/` + `apps/` `grep` `35=D`: **0** hits. Mentions of `NewOrderSingle` are **comments / log strings / FSM helpers / const false**, not a builder. `ExecutionOrderStateMachine.MayRetryNewOrderSingle` is a status predicate. `ShadowCopyEngine.SimulateEntry` is paper.

### 4.2 Copy pipeline: SHADOW only, send bits forced off

`CopyTradingHostedService` always calls `GenerateShadowIntentsAsync` every 20 s after an 8 s delay. **No** `FEATURE_COPY` / `REAL_COPY` check.

`CopyTradingService` constants:

```15:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = 0.05m;
```

Intents are generated for `SHADOW` / `LIVE_CANDIDATE` / `LIVE` scores. Risk is evaluated (`_risk.Evaluate`) with `RealExecutionEnabled = _runtime.RealCopyEnabled` and `Reconciled = VenueReconciled` (**false**). Persist then **overwrites**:

```185:194:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
                    DecidedAt = now
                };
```

Live-send branch requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. The last two are compile-time **false**, so the branch is dead. Intents land `SHADOW_ONLY`.

`BuildBlockers` still lists `SAFE_BY_ABSENCE`, unreconciled venue, 0 LIVE traders, QUOTE/TRADE down, and `REAL_COPY_EXECUTION_ENABLED is false` **only if** the runtime bit is false. When env-armed, that last blocker **drops**. The other blockers remain. Summary still refuses Pepperstone fill.

`new ExecutionIntent` / `ExecutionIntents.Add` / `SentAt =`: **0** product hits. Live-sends counter is a count of rows that do not exist.

`PersistDemoShadowAsync` (`EfTradingStore.cs` L251–330) writes `CopyIntent` `Status = "SHADOW_ONLY"` + paper `ShadowOrder`. No FIX.

### 4.3 RiskEngine conjunction (domain law; persist ignores AllowFixSend)

`RiskEngine.Evaluate` (`src/Domain/Risk/RiskEngine.cs` L147–170):

```csharp
var allowSend = request.RealExecutionEnabled
                && request.KillSwitch == KillSwitchMode.None
                && request.Reconciled
                && request.VenueHealthy;
```

Because `CopyTradingService` always passes `Reconciled=false`, increasing actions reject with `VENUE_NOT_RECONCILED` **before** `allowSend` is computed. Unit test `Real_flag_false_never_allows_fix_send` (`tests/Unit/RiskEngineTests.cs` L21–26) keeps `RealExecutionEnabled=false` and asserts `AllowFixSend==false`. That test does **not** cover the env-armed process.

Even if `allowSend` were true, persist forces `AllowFixSend=false` and there is still no sender.

### 4.4 UI honesty

`apps/web/src/pages/LiveCopyPage.tsx` reads `/api/copy/status`. It shows `REAL_COPY armed` from `realCopyArmed` (can be YES when env is true) and lists `status.blockers`. It does not send orders.

---

## 5. YoPips C++ backend — not these flags, not a cTrader sender

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm` for `FEATURE_COPY_TRADING_ENABLED` / `REAL_COPY_EXECUTION_ENABLED` / `RealCopyExecutionEnabled` / `35=D` / `NewOrderSingle`: **0** hits.

YoPips `COPY_TRADING_RESTRICTION` (`admin_approval_final_review_service.cpp` L575) is **detection / admin restriction** of challenge-account copy-trading (`allow_copy_trading`, `copy`, `mirror`). It does not define these defaults and cannot place Pepperstone `35=D` for this lab.

---

## 6. Surface matrix (slot 137 re-measure)

| Surface | `FEATURE_COPY_TRADING_ENABLED` | `REAL_COPY_EXECUTION_ENABLED` / twin |
|---|---|---|
| Architecture §41 / §56 | **absent** | **false** (L1572 / L2101) |
| `CTraderFixOptions` | absent | `RealCopyExecutionEnabled = false` (unbound) |
| DI `LiveRuntimeStatus` | unread | **`configuration["REAL_COPY_EXECUTION_ENABLED"]=="true"`** |
| FIX logon hosted service | unread | **does not pin**; logs `RealCopyArmed` |
| `GET /api/settings` | **literal `true`** | `runtime.RealCopyEnabled` → **true if `.env` loaded** |
| `GET /api/health` | absent | `realCopyEnabled` from runtime |
| `CopyTradingService` | `FeatureCopyEnabled: true` | `RealCopyArmed` from runtime; persist `AllowFixSend=false` |
| `CopyTradingHostedService` | unread (always ticks) | unread |
| `apps/fix-worker` | unread | `GetValue("CTrader:RealCopyExecutionEnabled", false)` — **log only** |
| `D:\Prop\.env` | `=true` (L106), unused by C# `GetValue` | `=true` (L73), **read by DI** |
| `docker-compose.yml` / `launchSettings.json` | unset | unset |
| `.env.example` | file **missing** | file **missing** |
| `appsettings.json` | unset | unset (`LiveCopyEnabled=false` is a **different** name) |
| Native Manager fetch | unread | unread |
| YoPips C++ | **0** hits | **0** hits |
| `35=D` builder | n/a | **absent** (`src/`+`apps/` = **0**) |

---

## 7. Drift vs earlier reports (binding for later slots)

| Earlier claim | Current HEAD (slot 137) |
|---|---|
| W500_57 / 97: both flags default **false**; `.env` `=false`; no C# `=true` | **Stale.** API FEATURE literal **`true`**. Lab `.env` **both true**. DI binds REAL_COPY. |
| W500_57 / 97 / 108 / 68: DI constructs `RealCopyEnabled=false` + logon re-pin | **Stale.** DI L41 equals env `"true"`. Logon host does **not** assign false. |
| W500_57: `FEATURE_COPY` API literal **false** | **Stale.** `Program.cs` L77 literal **`true`**. |
| W500_57: `0` `GetValue("REAL_COPY_EXECUTION_ENABLED")` | **Partially stale.** Still no `GetValue`; there **is** `configuration["REAL_COPY_EXECUTION_ENABLED"]`. |
| W500_99 / 59: `0` product `RiskEngine.Evaluate` callers | **Stale.** `CopyTradingService.GenerateShadowIntentsAsync` calls `_risk.Evaluate`. Persist still forces `AllowFixSend=false`. `0` `ExecutionIntent` writers remains true. |
| CREDENTIALS L30: REAL_COPY **false (forced)** | **Stale.** Not forced. Env-armed + bound. |
| E038 / A015: FEATURE unused / API false; process pin false | **Stale** on those sentences. Shadow pipeline is ON. REAL_COPY bit can be true. |
| W500_118 / 123 / 124 / 126 residual: DI bind + `.env` true, sender missing | **Agrees** with this re-measure. |
| `35=D` absent / fetch-all flag-blind / YoPips 0 senders | **Still true.** |

Do **not** treat FIX LoggedOn as a send license. Do **not** treat `FEATURE_COPY_TRADING_ENABLED=true` as a Pepperstone arm. Do **not** treat env `REAL_COPY_EXECUTION_ENABLED=true` as §68/§70 PASS. Architecture still requires the flag to stay **false** until those gates are 19/19 + 14/14 **and** a sender exists that consults `AllowFixSend`.

---

## 8. What this slot did **not** do

- Did not flip any flag.
- Did not add or remove a `35=D` builder.
- Did not live-attach Manager or FIX (census cited from `LIVE_MANAGER_FETCH_MEASURED.md`).
- Did not call loopback `/api/settings`.
- Did not print secrets.
- Did not edit product source.

---

## 9. One-line

```text
FEATURE_COPY_TRADING_ENABLED display/pipeline ON (API literal true; .env true unused by GetValue)
AND REAL_COPY_EXECUTION_ENABLED lab .env=true NOW BOUND by DI (arch/POCO/worker-fallback still false)
AND fetch ALL groups/traders is flag-blind (18/8460 already measured)
AND 35=D absent + AllowFixSend persist-false + NewOrderSingleImplemented=false
→ risk to capital NONE (SAFE_BY_ABSENCE), not "defaults both false"
```
