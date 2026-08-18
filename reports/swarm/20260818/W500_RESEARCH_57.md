# W500_RESEARCH_57 — `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults

| Field | Value |
|---|---|
| Slot | **57** |
| Date | 2026-08-18 |
| Agent | W500 research 57 (flag defaults vs live-copy no-loss) |
| Assigned | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Trees read | `D:\Prop` product + docs + live-census artifacts; `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (name search only) |
| Product source modified | **No** |
| Secrets printed | **None.** `.env` quoted only as flag names `=false`. No MT5 / FIX / proxy / DB passwords. |
| Method | `grep` + `read_file` on C# / JSON / env / architecture / YoPips. This slot did **not** live-attach Manager or FIX. Census numbers are from the already-measured 2026-08-18 probe. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A hardcoded API `false` is a **display floor**, not a send gate. Absence of a `NewOrderSingle` / `35=D` assembler is **SAFE_BY_ABSENCE**, not proof that the env token is a tested choke. `FEATURE_COPY_TRADING_ENABLED` is **not** architecture §41.

Sibling (do not collapse): `W500_RESEARCH_17.md` asked the same flag question earlier. This file is a **re-measure of current HEAD**, not a copy. Drift vs E038 is called out in §7.

---

## 0. Verdict (binding)

| Question | Measured answer |
|---|---|
| `FEATURE_COPY_TRADING_ENABLED` default | **`false`** |
| `REAL_COPY_EXECUTION_ENABLED` default | **`false`** |
| Any product config / C# assignment `=true` for either name? | **No** in `apps/` + `src/` + `.env` + `docker-compose.yml` + `launchSettings.json` |
| Does fetch of ALL Achiever+Starwave groups/traders consult either flag? | **No** |
| Can this process emit live cTrader `35=D` / NewOrderSingle? | **No** — builder **absent**; runtime send bit **pinned false** |
| Risk to capital from copy path | **NONE** (this process) |

**Slot-57 verdict: `PASS_DEFAULTS_FALSE_NO_LIVE_SEND`.**

Both named flags default **OFF**. Manager catalog ingest is **read-only** (`GroupRequestArray("*")` + `UserRequestArray` / fallbacks) and is **not** gated by either flag, so the “fetch ALL groups + ALL manager traders” goal is not blocked by copy flags. Live copy cannot open a losing Pepperstone/cTrader position from this tree: `CTraderFixSession.BuildLogon` emits only `35=A`; there is no `35=D` assembler; `LiveRuntimeStatus.RealCopyEnabled` is constructed `false` and **re-forced false after FIX logon**.

**Gate honesty:** `REAL_COPY_EXECUTION_ENABLED` (architecture name) is **not** bound onto `CTraderFixOptions` by ASP.NET env convention (`CTrader__RealCopyExecutionEnabled` would be required). `FEATURE_COPY_TRADING_ENABLED` is **never** `GetValue`’d in C#. Safety today is **default-false + pin-false + SAFE_BY_ABSENCE**, not a single named choke that a unit test proves refuses `35=D` on a logged-on TRADE socket.

---

## 1. The two names are not the same control

| Name | Kind | Architecture §41? | Consumed by a worker? |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | Config **floor** for new real `NewOrderSingle` | **Yes** (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1572 default, L1587 enable-example only, L2101 sample) | **No env bind.** Worker reads **`CTrader:RealCopyExecutionEnabled`**, default `false`. API exposes `runtime.RealCopyEnabled` (pinned `false`). |
| `FEATURE_COPY_TRADING_ENABLED` | Extra “Feature Flags” env + API dictionary key | **No.** Architecture file: **0** hits. A75: do **not** invent this as an architecture name. D61: **not** a substitute for `REAL_COPY_EXECUTION_ENABLED`. | **No.** Sole product C# hit is a **literal `false`** in `GET /api/settings`. Env value is loaded by `EnvFile` then **ignored**. |

A third leftover name exists and is also **not** the send license:

| Name | Where | Default | Live? |
|---|---|---|---|
| `FeatureFlags:LiveCopyEnabled` | `apps/api/appsettings.json` L46; dead `SettingsController` | **`false`** | **No.** `Program.cs` has **0** `MapControllers` / `AddControllers`. Controller PUT would write Redis `settings:flags:live_copy` if mapped — it is not. |

Flipping `FEATURE_COPY_TRADING_ENABLED=true` in `.env` **cannot** change `/api/settings` (literal) and **cannot** create a send path.

Flipping `REAL_COPY_EXECUTION_ENABLED=true` in `.env` **cannot** set `CTraderFixOptions.RealCopyExecutionEnabled` (wrong key) and **cannot** override the DI / logon pins. Even if `CTrader:RealCopyExecutionEnabled=true`, `apps/fix-worker` only **logs a warning** and still has **no** `35=D` function.

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

Docs that restate the architecture floor (not product binders):

- `D:\Prop\docs\architecture.md` L20
- `D:\Prop\docs\ctrader-fix.md` L73
- `D:\Prop\docs\deployment.md` L82
- `D:\Prop\README.md` L28

### 2.2 C# initializer — send-license property

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

`Configure<CTraderFixOptions>`: **0** hits under `D:\Prop` `*.cs`. The POCO default is unused by a binder today.

### 2.3 Runtime pin (API / ingest host) — cannot be armed by env

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

After optional QUOTE/TRADE **logon only** (`35=A` on TLS 5211/5212), the hosted FIX service **forces the bit back to false**:

```60:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
```

`LiveRuntimeStatus.Snapshot()` when false (`LiveRuntimeStatus.cs` L42–44): `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

### 2.4 Live settings API (the host operators actually hit)

`apps/api/Program.cs` maps **minimal** `GET /api/settings`. There is **no** `AddControllers` / `MapControllers` anywhere under `D:\Prop` (`grep` on `*.cs`: **0**). The MVC `SettingsController` is **dead code**.

```70:77:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
```

| Flag in `/api/settings` | Source | Effective default |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` | **`false`** (DI construct + logon re-pin; env unread) |
| `FEATURE_COPY_TRADING_ENABLED` | **literal `false`** | **`false`** (ignores `.env`) |

`/api/health` also exposes `realCopyEnabled = runtime.RealCopyEnabled` (`Program.cs` L54).

### 2.5 FIX worker — different key, log-only

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

`apps/fix-worker/appsettings.json` and `appsettings.Development.json` contain **logging only**. No `CTrader` section. `GetValue` fallback is therefore **`false`** unless an operator injects `CTrader__RealCopyExecutionEnabled`. That flip still cannot emit `35=D`.

### 2.6 Local `.env` (names + booleans only)

`D:\Prop\.env` (gitignored; values not printed except these floors):

| Line | Token |
|---|---|
| 73 | `REAL_COPY_EXECUTION_ENABLED=false` |
| 106 | `FEATURE_COPY_TRADING_ENABLED=false` |
| 107–109 | other `FEATURE_*` also `false` except reconstruction |

`EnvFile.FindAndLoad()` (`src/Mt5/Env/EnvFile.cs`) walks to `D:\Prop\.env` and `SetEnvironmentVariable`s **every** `KEY=VALUE`. Loading is **not** binding. `Program.cs` L8–12 calls `FindAndLoad()` then `AddEnvironmentVariables()`. That would surface `REAL_COPY_EXECUTION_ENABLED` as a **top-level** config key, which **no** product `GetValue` reads.

`.env.example`: **deleted** in this worktree (INDEX notes D62 / A103). `docker-compose.yml` does **not** set either flag (API service only sets `ASPNETCORE_ENVIRONMENT=Development`). `apps/api/Properties/launchSettings.json` does **not** set either flag.

### 2.7 Committed JSON leftover (not the architecture name)

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

`LiveMt5Registration.CreateConnectors` always returns **two** `NativeMt5BrokerConnector` instances (`ACHIEVER`, `STARWAVEFX`). It reads `MT5_*` / `ACHIEVER_PROXY_*` / `MT5_STARWAVEFX_*` only. **Zero** reads of either copy flag.

`AddTraderIntelligence` **throws** if both manager passwords are missing (`HasRealPasswords`). Dummy/Fake path is disabled on the live host.

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

`LiveIngestHostedService` iterates `registry.All()` (both connectors) and calls `SyncCatalogAsync` then `SyncBrokerAsync` without consulting copy flags. `/api/ops/resync` does the same for `["ACHIEVER", "STARWAVEFX"]`.

Dashboard `GetGroupsAsync` / `GetTradersAsync` (`EfDashboardQueries.cs`) materialize **all** `Mt5Groups` / `Mt5Accounts`. Traders endpoint has **no** `Take`.

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

It writes that one frame, reads one reply, returns. **No** `35=D`, `F`, `G`, `H`, `V`. `CTraderQuoteService` is an in-memory SecurityList/quote mapper — it never opens a socket.

Product `*.cs` mentions of `NewOrderSingle` are **comments / log strings / FSM helpers**, not a builder. `ExecutionOrderStateMachine.MayRetryNewOrderSingle` is a status predicate. `ShadowCopyEngine.SimulateEntry` is paper.

### 4.2 Risk bit is fail-closed and unwired

`RiskEngine.Evaluate` (`src/Domain/Risk/RiskEngine.cs` L147–170) sets:

```csharp
var allowSend = request.RealExecutionEnabled
                && request.KillSwitch == KillSwitchMode.None
                && request.Reconciled
                && request.VenueHealthy;
```

Unit test `Real_flag_false_never_allows_fix_send` (`tests/Unit/RiskEngineTests.cs` L21–26) keeps `RealExecutionEnabled = false` and asserts `AllowFixSend == false`.

`grep` of product `*.cs` for `new RiskEngine` / `RiskEngine.Evaluate` / `IRiskEngine`: **0** outside the test file (tests use `_e = new()`). `AddTraderIntelligence` does **not** register `RiskEngine`. There is no caller that could pass `RealExecutionEnabled=true` into a sender, because **there is no sender**.

### 4.3 UI honesty

`apps/web/src/pages/LiveCopyPage.tsx` L5 is a **JSX literal**: SHADOW only; NewOrderSingle disabled; names `REAL_COPY_EXECUTION_ENABLED` as a still-required gate. It does not read `FEATURE_COPY_TRADING_ENABLED`.

---

## 5. YoPips C++ backend — not these flags, not a cTrader sender

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (and parent `D:\Projects\YoPips`) for `FEATURE_COPY_TRADING_ENABLED` / `REAL_COPY_EXECUTION_ENABLED` / `RealCopyExecutionEnabled`: **0** hits.

YoPips `copy_trade_clusters` / `copy_trade_cluster_members` / `COPY_TRADING_RESTRICTION` (`admin_approval_final_review_service.cpp`, `admin_v2_risk_analytics_controller.cpp`) are **detection / admin restriction** of challenge-account copy-trading. They do not define these defaults and cannot place Pepperstone `35=D` for this lab.

---

## 6. Surface matrix (slot 57 re-measure)

| Surface | `FEATURE_COPY_TRADING_ENABLED` | `REAL_COPY_EXECUTION_ENABLED` / twin |
|---|---|---|
| Architecture §41 / §56 | **absent** | **false** (L1572 / L2101) |
| `CTraderFixOptions` | absent | `RealCopyExecutionEnabled = false` |
| DI `LiveRuntimeStatus` | absent | `RealCopyEnabled = false` |
| FIX logon hosted service | unread | `_runtime.RealCopyEnabled = false` after `35=A` |
| `GET /api/settings` | **literal `false`** | `runtime.RealCopyEnabled` → **false** |
| `GET /api/health` | absent | `realCopyEnabled` from runtime |
| `apps/fix-worker` | unread | `GetValue("CTrader:RealCopyExecutionEnabled", false)` — **log only** |
| `D:\Prop\.env` | `=false` (L106), unused | `=false` (L73), unused by C# `GetValue` |
| `docker-compose.yml` / `launchSettings.json` | unset | unset |
| `.env.example` | file **missing** | file **missing** |
| `appsettings.json` | unset | unset (`LiveCopyEnabled=false` is a **different** name) |
| Native Manager fetch | unread | unread |
| YoPips C++ | **0** hits | **0** hits |
| `35=D` builder | n/a | **absent** |

---

## 7. Drift vs earlier reports (do not treat as contradiction of the default)

| Earlier claim | Current HEAD |
|---|---|
| E038: `/api/settings` only key is `REAL_COPY_EXECUTION_ENABLED` **literal** at `Program.cs` L45 | Now **two** keys at L75–76: REAL_COPY from **runtime**, FEATURE_COPY **literal false**. Default is still false. |
| A08 (early): flag absent from all C# | **Stale.** Worker + options + API now exist; still no sender. |
| W500_17 same question | Same defaults. This slot re-confirmed fetch independence + YoPips 0 + RiskEngine unwired. |

Do **not** set either flag to `true`. Do **not** treat FIX LoggedOn as a send license. Do **not** treat `FEATURE_COPY_TRADING_ENABLED` as the architecture send floor.

---

## 8. What this slot did **not** do

- Did not flip any flag.
- Did not add a `35=D` builder.
- Did not live-attach Manager or FIX (census cited from `LIVE_MANAGER_FETCH_MEASURED.md`).
- Did not print secrets.
- Did not edit product source.

---

## 9. One-line

```text
FEATURE_COPY_TRADING_ENABLED=false (API literal; .env unused)
AND REAL_COPY_EXECUTION_ENABLED=false (arch §41 + POCO + DI/logon pin + worker GetValue fallback)
AND fetch ALL groups/traders is flag-blind (18/8460 already measured)
AND 35=D absent (SAFE_BY_ABSENCE) → risk to capital NONE
```
