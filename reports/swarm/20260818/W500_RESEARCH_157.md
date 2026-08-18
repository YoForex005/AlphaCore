# W500_RESEARCH_157 — `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults

| Field | Value |
|---|---|
| Slot | **157** |
| Date | 2026-08-18 |
| Agent | W500 research 157 (flag defaults vs live-copy no-loss) |
| Assigned | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Trees read | `D:\Prop` product (`src/`, `apps/`, `tests/`, docs, architecture, `.env` names+booleans only, live-census artifacts); `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`config/app_config.h` + `src` / `config` token search) |
| Product source modified | **No** |
| Test source modified | **No** |
| Config / `.env` edited | **No** |
| Secrets printed | **None.** `.env` quoted only as flag names `=true`/`=false`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Method | Independent `grep` + `read_file` of DI, API `Program.cs`, `CopyTradingService`, FIX logon host, `CTraderFixSession`, `CTraderFixOptions`, fix-worker, `LiveRuntimeStatus`, `RiskEngine`, `DealIngestionService`, `NativeMt5BrokerConnector`, `LiveMt5Registration`, `LiveIngestHostedService`, dashboard queries, `.env` flag lines, appsettings, YoPips AppConfig. **No** Manager re-attach. **No** FIX TLS this slot. **No** loopback `GET /api/settings` (loopback blocked). Census is the already-measured 2026-08-18 `LiveBrokerProbe` JSON. |
| Binding law | Architecture **§41** (L1564–1590), **§42**, **§56** (L2101), **§68**, **§70**. A75: do not invent `FEATURE_COPY_TRADING_ENABLED` as an architecture name. |
| Siblings (do not collapse) | Same two names: `W500_RESEARCH_17` / `37` / `57` / `77` / `97` / `117` / `137`. Related: `W500_108` / `68` (must-stay-false), `W500_123` (DI-bind residual), `A015`, `E038`, `CREDENTIALS_AND_COPY_STATUS`. This file is a **re-measure of current HEAD for slot 157**, not a copy. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A hardcoded API `true` is a **display / pipeline floor**, not a send gate. Binding env `REAL_COPY_EXECUTION_ENABLED=true` onto `LiveRuntimeStatus.RealCopyEnabled` **arms a bit**; it does **not** create a `NewOrderSingle`. Absence of a `35=D` assembler is **`SAFE_BY_ABSENCE`**. `FEATURE_COPY_TRADING_ENABLED` is **not** architecture §41. Fetching all Manager groups/traders is **read-only** and does **not** license send.

---

## 0. Verdict (binding)

| Question | Measured answer (HEAD this pass) |
|---|---|
| Architecture / POCO / worker-fallback **design default** | `FEATURE_COPY` **absent from §41**. `REAL_COPY` **false** (§41 L1572; §56 L2101; `CTraderFixOptions.RealCopyExecutionEnabled=false`; worker `GetValue("CTrader:RealCopyExecutionEnabled", false)`). |
| Product C# **display / pipeline** | `FEATURE_COPY_TRADING_ENABLED` **literal `true`** in `GET /api/settings` L77 and `CopyTradingService.FeatureCopyEnabled`. |
| Lab `.env` (gitignored operator file) | **Both `=true`**: `REAL_COPY` L73, `FEATURE_COPY` L106. |
| C# default if env **unset** | `string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true")` → **`false`**. `bool RealCopyEnabled` field default **false**. |
| Does DI still **pin** `RealCopyEnabled=false`? | **No.** `DependencyInjection.cs` L41 **binds** the env token. `CTraderFixLogonHostedService` **does not** re-assign false. |
| Does fetch of ALL Achiever+Starwave groups/traders consult either flag? | **No** — `GroupRequestArray("*")` + `GetAccountsAsync(null)` are flag-blind. |
| Can this process emit live cTrader `35=D` / NewOrderSingle? | **No** — builder **absent**; persist `AllowFixSend=false`; `NewOrderSingleImplemented=false`; `VenueReconciled=false`; `CanPromoteToLive => false`; **0** `ExecutionIntent` writers. |
| Risk to capital from copy path | **NONE** (this process) — `SAFE_BY_ABSENCE` |

**Slot-157 verdict: `PASS_NO_LIVE_SEND_ENV_ARMED`.**

Do **not** repeat slot-17/37/57/77/97/108 language (“both flags default false / DI+logon pin / `.env=false` / API FEATURE literal false”). That is **stale against current disk**. Current HEAD:

1. **Shadow copy pipeline is ON.** `FEATURE_COPY` is a display/status literal `true`. `CopyTradingHostedService` ticks SHADOW intents with **no** env read. The env key is **never** `GetValue`’d.
2. **Operator `REAL_COPY` is armed** in lab `.env` L73 and **will set** `LiveRuntimeStatus.RealCopyEnabled=true` on any host that loads that file (`EnvFile.FindAndLoad` + `AddEnvironmentVariables` + DI L41). Architecture law still says the floor is **false**.
3. **Live Pepperstone fill is still impossible.** There is no `35=D` assembler. The flag is necessary-not-sufficient. Capital safety today is **`SAFE_BY_ABSENCE` + persist `AllowFixSend=false` + const `NewOrderSingleImplemented=false`**, not “defaults are false.”

Manager catalog ingest is **read-only** and is **not** gated by either flag, so the “fetch ALL groups + ALL manager traders” goal is not blocked. Prior live census: **18 groups / 8460 traders** (Achiever 8/6512 + Starwave 10/1948) at `2026-08-18T08:42:16Z`.

**Residual (honest):** the next engineer who adds a sender that trusts `runtime.RealCopyEnabled` will find it **true** on the API host. Do **not** add that sender in this task. Do **not** treat env-true as a go-live waiver.

---

## 1. The two names are not the same control

| Name | Kind | Architecture §41? | Consumed? |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | Operator **arm** for new real `NewOrderSingle` | **Yes** (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1572 default `false`, L1587 enable-example only, L2101 sample `false`) | **Yes, as a runtime bit.** `AddTraderIntelligence` copies env onto `LiveRuntimeStatus.RealCopyEnabled`. Copy service passes it into `RiskEngine.Evaluate` as `RealExecutionEnabled`, then **overwrites** persist `AllowFixSend=false`. FIX worker reads a **different** key. |
| `FEATURE_COPY_TRADING_ENABLED` | Extra “Feature Flags” env + API dictionary key | **No.** Architecture file: **0** hits. A75: do **not** invent this as an architecture name. | **No env bind.** Sole product C# *assignment of the name* is a **literal `true`** in `GET /api/settings`. `CopyTradingService` hardcodes `FeatureCopyEnabled: true`. Hosted copy tick has **no** flag check. |

A third leftover name is also **not** the send license:

| Name | Where | Default | Live? |
|---|---|---|---|
| `FeatureFlags:LiveCopyEnabled` | `apps/api/appsettings.json` L46; dead `SettingsController` L38 | **`false`** | **No.** Product `*.cs` has **0** `AddControllers` / `MapControllers`. Minimal `MapGet("/api/settings")` wins. |

A fourth leftover name is **log-only**:

| Name | Where | Default | Live? |
|---|---|---|---|
| `CTrader:RealCopyExecutionEnabled` | `apps/fix-worker/Worker.cs` L21; `CTraderFixOptions` L35 | **`false`** | Worker only **logs**. No `CTrader` section in worker `appsettings.json`. **Not** bound from `REAL_COPY_EXECUTION_ENABLED` (`Configure<CTraderFixOptions>` = **0**). |

Flipping `FEATURE_COPY_TRADING_ENABLED` in `.env` **cannot** change `/api/settings` (literal `true`) and **cannot** create a send path.

Flipping `REAL_COPY_EXECUTION_ENABLED=true` in `.env` **does** set `runtime.RealCopyEnabled` and therefore `/api/settings` + `/api/health` + `/api/copy/status.realCopyArmed`. It still **cannot** set `CTraderFixOptions.RealCopyExecutionEnabled` and **cannot** emit `35=D`.

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

Docs that restate the architecture floor (not binders; now **stale vs lab `.env` and DI**):

- `D:\Prop\docs\architecture.md` L20
- `D:\Prop\docs\ctrader-fix.md` L73
- `D:\Prop\docs\deployment.md` L82
- `D:\Prop\README.md` L28
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` L30 (`false (forced)` — **stale**; DI no longer forces)

### 2.2 C# initializer — send-license property (still fail-closed, still unbound)

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

`Configure<CTraderFixOptions>`: **0** hits under `D:\Prop` `*.cs`. The POCO default is unused by a binder today. Architecture env name would need `CTrader__RealCopyExecutionEnabled` to land here via ASP.NET convention.

### 2.3 Runtime bind (API / ingest host) — **env now honored**

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

This is the **slot-157 drift vs 57/97/108**. There is no comment pinning the bit false. Comparison is ordinal-ignore-case against the literal `"true"`; unset / empty / `false` / `1` / `TRUE ` (trailing space after `EnvFile` trim is OK; other tokens fail) stay **false**.

`LiveRuntimeStatus.RealCopyEnabled` is a plain `bool` (default false until assigned). Snapshot copy-note (`LiveRuntimeStatus.cs` L42–44):

- armed: `REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.`
- off: `NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.`

### 2.4 FIX logon hosted service — **no longer re-pins false**

`CTraderFixLogonHostedService` (`src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` L60–70) writes QUOTE/TRADE logon bits, then logs `RealCopyArmed={Armed} NewOrderSingle still unimplemented`. There is **no** `_runtime.RealCopyEnabled = false` assignment (slot-57 citation of L116 is gone).

`TryLogonAsync` is **one** `35=A` write + one read + dispose. It does not consult either copy flag. Password missing → logon skipped (L33–38).

### 2.5 Live settings API (the host operators actually hit)

`apps/api/Program.cs` L8–13 loads `D:\Prop\.env` (`EnvFile.FindAndLoad`, including hardcoded candidate `D:\Prop\.env`) then `AddEnvironmentVariables()`. Maps **minimal** `GET /api/settings`. There is **no** `AddControllers` / `MapControllers`. MVC `SettingsController` (`apps/api/Controllers/SettingsController.cs`) is **dead code** (would have exposed `FeatureFlags:LiveCopyEnabled`, not the architecture token).

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

`apps/fix-worker/appsettings.json` is **logging only** (no `CTrader` section). `GetValue` fallback is therefore **`false`** unless an operator injects `CTrader__RealCopyExecutionEnabled`. That flip still cannot emit `35=D`. The worker stamps QUOTE/TRADE `Disconnected` every 15 s.

### 2.7 Local `.env` (names + booleans only)

`D:\Prop\.env` (gitignored; values not printed except these flags):

| Line | Token |
|---|---|
| 73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| 106 | `FEATURE_COPY_TRADING_ENABLED=true` |
| 107 | `FEATURE_CTRADER_HEDGING_ENABLED=false` |
| 108 | `FEATURE_ML_SCORING_ENABLED=false` |
| 109 | `FEATURE_NEWS_FILTER_ENABLED=false` |
| 110 | `FEATURE_TRADE_RECONSTRUCTION_ENABLED=true` |

`CTrader:RealCopyExecutionEnabled` / `CTrader__RealCopyExecutionEnabled`: **absent** from `.env`.

`EnvFile.FindAndLoad()` (`src/Mt5/Env/EnvFile.cs` L5–20) walks cwd/`../`/`D:\Prop\.env` and `SetEnvironmentVariable`s every `KEY=VALUE`. Loading is binding **only** because DI now reads the top-level key.

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

`LiveMt5Registration.CreateConnectors` (`src/Infrastructure/Mt5Live/LiveMt5Registration.cs` L20–49) always returns **two** `NativeMt5BrokerConnector` instances (`ACHIEVER`, `STARWAVEFX`). It reads `MT5_*` / `ACHIEVER_PROXY_*` / `MT5_STARWAVEFX_*` only. **Zero** reads of either copy flag. Starwave `ProxyEnabled` is hardcoded `false` (L45).

`AddTraderIntelligence` **throws** if both manager passwords fail `IsSecret` (`HasRealPasswords` L10–15). Dummy/Fake path is disabled on the live host.

### 3.2 Catalog walk (ALL groups, ALL logins the manager can see)

`DealIngestionService.SyncCatalogAsync` (`src/Application/Ingestion/DealIngestionService.cs` L38–51):

1. `connector.GetGroupsAsync(ct)`
2. `connector.GetAccountsAsync(null, ct)` — `null` group means **every** group just fetched

`NativeMt5BrokerConnector.GetGroupsCore` (L144–183):

1. `GroupRequestArray("*", arr)`
2. Fallback: `GroupTotal` + `GroupNext`

`GetAccountsCore` (L189–213) with `group == null` walks **every** group name. Per group `ReadAccountsForGroup` (L216–233):

1. `UserRequestArray(gname, users)`
2. Fallback `UserGetByGroup` on hard fail
3. If still empty: `UserLogins` + `UserRequestByLogins`
4. Account money fields: `UserAccountRequestArray` / `UserAccountGetByGroup`

No `Take(` / `Skip` on this walk. No flag check.

`LiveIngestHostedService` iterates `registry.All()` (both connectors) and calls `SyncCatalogAsync` then `SyncBrokerAsync` without consulting copy flags. Hosted **score** is `ListLoginsWithDealsAsync` only (catalog still holds the rest as `INSUFFICIENT_DATA`). `/api/ops/resync` (`Program.cs` L124) does the same catalog+deals walk for `["ACHIEVER", "STARWAVEFX"]`.

Dashboard `GetGroupsAsync` / `GetTradersAsync` (`EfDashboardQueries.cs` L70–128) materialize **all** `Mt5Groups` / `Mt5Accounts` with a scores left-join (`foreach (var account in accounts)` L99). Traders endpoint has **no** `Take`.

`tools/LiveBrokerProbe/Program.cs` L25–26 uses the same `GetGroupsAsync` + `GetAccountsAsync(null)` pair.

### 3.3 Already-measured census (this slot did not re-attach)

From `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`utc`: `2026-08-18T08:42:16.8519545+00:00`) and `LIVE_MANAGER_FETCH_MEASURED.md`:

| Broker | Connect | Groups | Manager traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (manager-visible set): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23. Sum **6512**.

Starwave groups: `Starwave\cent\FX1\grp1` 11, `grp2` 4, `demo\FX2\grp1` 170, `grp2` 1735, `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2. Sum **1948**.

Copy flags did not, and cannot, shrink that walk.

---

## 4. Copy-to-cTrader cannot send live orders (no loss)

### 4.1 What FIX actually sends

`CTraderFixSession.BuildLogon` (`src/Fix.CTrader/Sessions/CTraderFixSession.cs` L89–109) assembles **only**:

- `35=A` (Logon)
- `34` seq, `49/56` CompIDs, `50/57` SubIDs, `52` time
- `98=0`, `108=30`, `141=Y`
- `553` username, `554` password

It writes that one frame (`WriteAsync` L49), reads one reply, returns. **No** `35=D`, `F`, `G`, `H`. Sockets disposed via `using`.

Product `src/` + `apps/` `grep` `35=D` / `(35, "D")`: **0** hits. Mentions of `NewOrderSingle` are **comments / log strings / FSM helpers / const false**, not a builder. `ExecutionOrderStateMachine.MayRetryNewOrderSingle` is a status predicate. `ShadowCopyEngine.SimulateEntry` is paper.

`CTraderQuoteService` can *construct* `35=y` / `35=V` lists. It is **not** registered in `AddTraderIntelligence` and is **not** on the live logon hop. Unused library ≠ live MarketDataRequest, and it is still not `35=D`.

`SendTrade` / `DealerSend` / `GuardedNewOrderSingle`: **0** under product `src/`.

### 4.2 Copy pipeline: SHADOW only, send bits forced off

`CopyTradingHostedService` always calls `GenerateShadowIntentsAsync` every 20 s after an 8 s delay. **No** `FEATURE_COPY` / `REAL_COPY` check.

`CopyTradingService` constants:

```15:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = 0.05m;
```

Intents are generated for `SHADOW` / `LIVE_CANDIDATE` / `LIVE` scores only. Risk is evaluated (`_risk.Evaluate`) with `RealExecutionEnabled = _runtime.RealCopyEnabled` and `Reconciled = VenueReconciled` (**false**). Persist then **overwrites**:

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

Live-send branch requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (L198). The last two are compile-time **false**, so the branch is dead. Intents land `SHADOW_ONLY`.

`BuildBlockers` lists `SAFE_BY_ABSENCE`, unreconciled venue, 0 LIVE traders, QUOTE/TRADE down, and `REAL_COPY_EXECUTION_ENABLED is false` **only if** the runtime bit is false. When env-armed, that last blocker **drops**. The other blockers remain. Summary still refuses Pepperstone fill.

`new ExecutionIntent` / `ExecutionIntents.Add`: **0** product hits. Entity exists (`src/Domain/Entities/ExecutionIntent.cs`); no writer. Live-sends counter is a count of `SentAt != null` rows that do not exist.

`PersistDemoShadowAsync` (`EfTradingStore.cs` L251–310) writes `CopyIntent` `Status = "SHADOW_ONLY"` + paper `ShadowOrder`. No FIX. Bypasses `RiskEngine` (separate residual; still not a send).

`BaselineScorer.CanPromoteToLive` (`src/Domain/Scoring/BaselineScorer.cs` L211) is **`=> false`**. Trade #3 cannot auto-LIVE.

### 4.3 RiskEngine conjunction (domain law; persist ignores AllowFixSend)

`RiskEngine.Evaluate` (`src/Domain/Risk/RiskEngine.cs` L84–90, L147–170):

- Increasing actions with `Reconciled=false` reject **`VENUE_NOT_RECONCILED`** before `allowSend` is computed.
- `allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`.

Because `CopyTradingService` always passes `Reconciled=false`, increasing actions cannot approve-to-send even when env REAL_COPY is true. Even if `allowSend` were true, persist forces `AllowFixSend=false` and there is still no sender.

`/api/reconciliation/status` (`Program.cs` L63–69) is a **stub** (`unknownPositions=0`, `mismatches=0`, English “NewOrderSingle still off”). `OrderMassStatusRequest` / `RequestForPositions` / `35=AF` / `35=AN` in product `*.cs` = **0**. §42 recon is **not** on the hop.

### 4.4 UI honesty

`apps/web/src/pages/LiveCopyPage.tsx` reads `/api/copy/status`. It shows `REAL_COPY armed` from `realCopyArmed` (can be YES when env is true) and lists `status.blockers` under “Pepperstone cannot be filled”. It does not send orders.

---

## 5. YoPips C++ backend — not these flags, not a cTrader sender

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` and `config` for `FEATURE_COPY_TRADING_ENABLED` / `REAL_COPY_EXECUTION_ENABLED` / `RealCopyExecutionEnabled` / `35=D` / `NewOrderSingle` / `cTrader` / `c-trader`: **0** hits.

`config/app_config.h` has MT5 connect/proxy/plan-group fields only. No copy-to-cTrader arm.

YoPips `COPY_TRADING_RESTRICTION` (`src/services/admin_approval_final_review_service.cpp` L575) is **detection / admin restriction** of challenge-account copy-trading (`allow_copy_trading`, `copy`, `mirror`). It does not define these Prop defaults and cannot place Pepperstone `35=D` for this lab.

---

## 6. Surface matrix (slot 157 re-measure)

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

| Earlier claim | Current HEAD (slot 157) |
|---|---|
| W500_17 / 37 / 57 / 77 / 97: both flags default **false**; `.env` `=false`; no C# `=true` | **Stale.** API FEATURE literal **`true`**. Lab `.env` **both true**. DI binds REAL_COPY. |
| W500_57 / 97 / 108 / 68: DI constructs `RealCopyEnabled=false` + logon re-pin | **Stale.** DI L41 equals env `"true"`. Logon host does **not** assign false. |
| W500_57: `FEATURE_COPY` API literal **false** | **Stale.** `Program.cs` L77 literal **`true`**. |
| W500_57: `0` `GetValue("REAL_COPY_EXECUTION_ENABLED")` | **Partially stale.** Still no `GetValue`; there **is** `configuration["REAL_COPY_EXECUTION_ENABLED"]`. |
| W500_59 / 99: `0` product `RiskEngine.Evaluate` callers | **Stale.** `CopyTradingService.GenerateShadowIntentsAsync` calls `_risk.Evaluate`. Persist still forces `AllowFixSend=false`. `0` `ExecutionIntent` writers remains true. |
| CREDENTIALS L30: REAL_COPY **false (forced)** | **Stale.** Not forced. Env-armed + bound. |
| LIVE_MANAGER_FETCH L48: REAL_COPY forced **false** | **Stale on the flag.** Still true that `35=D` is absent. |
| README L28 / docs/architecture L20 | Architecture **floor** still false; lab process **arm** is true. Do not quote docs as process state. |
| W500_117 / 137 | **Still current** on the two flags. Slot 157 independently re-read the same HEAD and agrees: `PASS_NO_LIVE_SEND_ENV_ARMED`. |

---

## 8. What this slot did **not** do

- Did **not** live-attach Achiever or Starwave Manager (census cited, not re-probed).
- Did **not** open cTrader FIX TLS / send `35=A` / send `35=D`.
- Did **not** `GET` loopback `:5000/api/settings`.
- Did **not** flip either flag.
- Did **not** implement a sender.
- Did **not** print secrets.

---

## 9. One-liner

```text
FEATURE_COPY is display/pipeline ON (API literal true; env unread).
REAL_COPY design default remains false; lab .env L73 is true and DI now binds it.
Fetch-all Achiever+Starwave is flag-blind (prior 18/8460).
No 35=D builder. AllowFixSend persist-false. NewOrderSingleImplemented=false.
Copy to cTrader cannot take a live loss. PASS_NO_LIVE_SEND_ENV_ARMED. Risk NONE.
```

Do **not** set `REAL_COPY_EXECUTION_ENABLED=true` in committed config. Do **not** add a `35=D` sender. Do **not** treat Logon, catalog fetch, `AllowFixSend`, `LiveCopyEnabled`, or env-armed `RealCopyEnabled` as a go-live waiver. §68 / §70 remain FAIL until independently re-scored.
