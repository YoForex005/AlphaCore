# W500_RESEARCH_37 — `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults

| Field | Value |
|---|---|
| Slot | **37** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_37 |
| Assigned | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` **defaults**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** Flags were **not** flipped. |
| Secret values printed | **None.** Presence + `false`/`true` only. |
| C++ tree consulted | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` — **not** the copy-to-cTrader sender; **0** hits for either token. |
| Method | Read product `*.cs` / `appsettings*` / `launchSettings` / `docker-compose.yml` / local `.env` (values only) / architecture §41. Grep `FEATURE_COPY_TRADING_ENABLED`, `REAL_COPY_EXECUTION_ENABLED`, `RealCopyExecutionEnabled`, `LiveCopyEnabled`, `35=D`, `NewOrderSingle`. Re-read `CTraderFixSession.BuildLogon` tag list. No live attach this slot. No password echo. |
| Prior same-angle pins | W500_RESEARCH_8 (REAL_COPY must stay false), D69 (POCO default), E038 (settings display), E034 / E002 (`SAFE_BY_ABSENCE`), LIVE_MANAGER_FETCH_MEASURED (18/8460). **This slot recenses both flag names together on current disk.** |

**Honesty rule:** a compile-time `= false`, a `GetValue(..., false)` fallback, a hardcoded API `false`, and an env line `=false` are **defaults / display floors**, not a unit-tested refuse-on-LoggedOn-TRADE gate. `FEATURE_COPY_TRADING_ENABLED` is **not** an architecture §41 name. `LiveCopyEnabled` is a **third** identifier. Absence of `35=D` is **`SAFE_BY_ABSENCE`**. Do not treat this file as §68 / §70 PASS. Do not flip either flag.

---

## 0. Verdict (binding)

**CONFIRMED: both flags default `false`. Live cTrader `NewOrderSingle` cannot fire from this process. Manager catalog fetch of ALL groups / ALL traders is the only live I/O that is implemented.**

| Flag | Default | Who owns it | Bound to send? |
|---|---|---|---|
| `FEATURE_COPY_TRADING_ENABLED` | **`false`** | Local `.env` L106 + API **literal** `false` | **No.** Env is loaded into the process and **never `GetValue`d**. Only C# hit is `Program.cs` L76 hardcoded `false`. **Not** in architecture §41. |
| `REAL_COPY_EXECUTION_ENABLED` | **`false`** | Architecture §41 / §56; `.env` L73; `CTraderFixOptions.RealCopyExecutionEnabled = false`; `LiveRuntimeStatus.RealCopyEnabled` **pinned false** | **Not a wired choke.** Worker reads a **different** key (`CTrader:RealCopyExecutionEnabled`, fallback `false`). Env name is **not** mapped onto the POCO or the runtime pin. Send is **`SAFE_BY_ABSENCE`**. |

One-line:

```text
FEATURE_COPY_TRADING_ENABLED=false (env + API literal; unread)
AND REAL_COPY_EXECUTION_ENABLED=false (env + POCO + DI pin + logon re-pin)
AND no function emits 35=D
THEREFORE copy-to-cTrader cannot open a live losing position
AND ALL-group / ALL-trader Manager fetch remains legal (read-only).
```

| Claim | Measured | Class |
|---|---|---|
| `FEATURE_COPY_TRADING_ENABLED` default is false | **Yes** — `.env` `false`; `/api/settings` **literal** `false` | display floor |
| That env token is a coded send gate | **No** — **0** `GetValue("FEATURE_COPY_TRADING_ENABLED")` | **unbound** |
| `REAL_COPY_EXECUTION_ENABLED` default is false | **Yes** — every product surface is `false` or fallback `false` | `EXISTS_AND_GOOD` vs §41 |
| That env token is bound to `CTraderFixOptions` / worker | **No** — would need `CTrader__RealCopyExecutionEnabled` | `GATE_INCOMPLETE` |
| Runtime can be armed by flipping `.env` to `true` | **No** — DI + logon **force** `RealCopyEnabled = false` | pin |
| Product can emit FIX `35=D` today | **No** — `CTraderFixSession` builds **only** `35=A` | **`SAFE_BY_ABSENCE`** |
| Fetch ALL Achiever + Starwave groups/traders | **Implemented** as Manager **read**; prior live census **18 / 8460** | read-only |
| Copy to Pepperstone/cTrader live orders | **Forbidden today** | both flags stay false |
| Safe to set either flag `true` | **No** | next engineer who adds a sender must find them **off** |

---

## 1. Goal split (do not collapse)

The user goal is two independent jobs:

| Job | Live I/O allowed? | Capital at risk? |
|---|---|---|
| **A.** Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders | **Yes** — native Manager **read** (`GroupRequestArray("*")` + `GroupTotal`/`GroupNext` fallback + `UserRequestArray` / `UserLogins`) | **No** (no destination order) |
| **B.** Copy those traders onto cTrader | **Not yet** — SHADOW / CopyIntent only | **Would be yes** the moment `35=D` exists |

Job A does **not** license Job B. Architecture §41: sessions may connect; `NewOrderSingle` requires `REAL_COPY_EXECUTION_ENABLED=true` **plus** a healthy risk engine. §68 / §70 are still **0/19** and **0/14**.

```text
MT5 Manager census (read)     = ALLOWED now
FIX 35=A Logon (QUOTE/TRADE)  = allowed for session proof / future recon
FIX 35=H / AF / AN            = Phase 7 recon (not built)
FIX 35=D NewOrderSingle       = FORBIDDEN until gates + REAL_COPY=true
FIX 35=F / G cancel/replace   = FORBIDDEN (same license)
FEATURE_COPY_TRADING_ENABLED  = inventory / UI name only; not a send license
```

---

## 2. `FEATURE_COPY_TRADING_ENABLED` — measured default **false**, unbound

### 2.1 Product C# — one hit, a display literal

Grep of `FEATURE_COPY_TRADING_ENABLED` against `D:\Prop\**\*.cs` (product hosts, not reports):

| File | Line | Kind | Reads env? |
|---|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | 76 | dictionary literal `false` | **No** |

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

`apps/api/Program.cs` has **no** `AddControllers` / `MapControllers`. The mapped settings route is this `MapGet`. The MVC twin is dead (see §4).

### 2.2 Local `.env` (gitignored) — present, `false`

`D:\Prop\.env` (loaded by `EnvFile.FindAndLoad()` → `Environment.SetEnvironmentVariable`; then `AddEnvironmentVariables()`):

```
FEATURE_COPY_TRADING_ENABLED=false          # L106, Feature Flags block
FEATURE_CTRADER_HEDGING_ENABLED=false       # sibling; not this slot
FEATURE_ML_SCORING_ENABLED=false
FEATURE_NEWS_FILTER_ENABLED=false
FEATURE_TRADE_RECONSTRUCTION_ENABLED=true
```

`EnvFile` copies **every** `KEY=value` into the process. That does **not** bind the key to a POCO. No product type has a `CopyTradingEnabled` property. No `GetValue("FEATURE_COPY_TRADING_ENABLED", …)` exists.

Flipping `.env` to `true` would:

1. Set a process environment variable.
2. Change **nothing** on `/api/settings` (literal).
3. Change **nothing** on FIX send (no reader, no builder).

### 2.3 Architecture / compose / launch — **absent**

| Surface | `FEATURE_COPY_TRADING_ENABLED` |
|---|---|
| Architecture `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | **0 hits** (A75: **not** a §56 name) |
| `docs/architecture.md` | **0 hits** |
| `apps/api/appsettings.json` | **0** — uses `FeatureFlags:LiveCopyEnabled` instead |
| `apps/api/appsettings.Development.json` | **0** |
| `apps/fix-worker/appsettings.json` | **0** (logging only) |
| `apps/*/Properties/launchSettings.json` | **0** — only `ASPNETCORE_ENVIRONMENT` |
| `D:\Prop\docker-compose.yml` | **0** — API service sets only `ASPNETCORE_ENVIRONMENT=Development` |
| Tracked `.env.example` | **not present** in this worktree listing |

Classification: **inventory / UI alias**. It is **not** the live-send license. Do not invent it as a second master switch.

---

## 3. `REAL_COPY_EXECUTION_ENABLED` — measured default **false**, pin + unbound env name

### 3.1 Law (architecture §41)

```1568:1572:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

§41: connect, receive prices, request orders/positions, validate FIX — **without automatically placing new real orders**. `NewOrderSingle` requires `REAL_COPY_EXECUTION_ENABLED=true` **plus** runtime risk-engine healthy. §56 repeats `false` (line 2101). `docs/architecture.md` L20 and `README.md` L28 restated the same floor.

Sibling session flags on the **same** options type default **on** (sessions may logon); send stays **off**:

| `CTraderFixOptions` property | Compile default |
|---|---|
| `UseSsl` | `true` |
| `QuoteEnabled` | `true` |
| `TradeSessionEnabled` | `true` |
| **`RealCopyExecutionEnabled`** | **`false`** |

### 3.2 Owning POCO (canonical C# default)

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

`new CTraderFixOptions()` yields `false`. There is **no** `services.Configure<CTraderFixOptions>` in `AddTraderIntelligence`. The POCO default is unused as a binder; it is still the documented compile-time floor.

Architecture env name `REAL_COPY_EXECUTION_ENABLED` is **not** the ASP.NET nested key. Binding that name onto this property would require `CTrader__RealCopyExecutionEnabled` (or an explicit binder). **That binder does not exist.**

### 3.3 Runtime pin — cannot be armed from env

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

After optional TLS logon, the hosted service **re-pins** false even if QUOTE/TRADE `35=A` succeeded:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        ...
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        ...
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

`GET /api/settings` `REAL_COPY_EXECUTION_ENABLED` **reads this pin**, not `.env`. `GET /api/health` exposes `realCopyEnabled = runtime.RealCopyEnabled`. `GET /api/ingest/status` snapshot:

```39:44:D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs
        startedAt = StartedAt,
        realCopyEnabled = RealCopyEnabled,
        copyNote = RealCopyEnabled
            ? "LIVE SEND ARMED — unexpected"
            : "NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.",
```

`OverviewDto.RealCopyEnabled` is `_runtime.RealCopyEnabled` (`EfDashboardQueries` L52). `GetRiskAsync` **ignores** runtime and hardcodes the 7th bool **`false`** (L208). `GetFixSessionsAsync` hardcodes `ExecutionEnabled = false` (L195).

### 3.4 FIX worker — different key, log-only

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

| What the worker reads | What `.env` has |
|---|---|
| `CTrader:RealCopyExecutionEnabled` | `REAL_COPY_EXECUTION_ENABLED=false` |

Those are **not** the same configuration path. `apps/fix-worker/appsettings.json` is logging only — **no** `CTrader` section — so `GetValue` fallback **`false`** applies unless someone sets `CTrader__RealCopyExecutionEnabled`. Even if `real==true`, the loop **stamps `Disconnected`** and **does not send**. The log format *names* `REAL_COPY_EXECUTION_ENABLED`; it does not bind that env token.

### 3.5 Local `.env` + compose + launch

`D:\Prop\.env` L73 (cTrader block, next to `CTRADER_FIX_TRADE_SESSION_ENABLED=true`):

```
REAL_COPY_EXECUTION_ENABLED=false
```

| Surface | Value | Bound to send? |
|---|---|---|
| `.env` `REAL_COPY_EXECUTION_ENABLED` | **`false`** | **No** — process env only |
| `CTraderFixOptions.RealCopyExecutionEnabled` | **`false`** | POCO default; unused binder |
| DI `LiveRuntimeStatus.RealCopyEnabled` | **`false`** (forced) | runtime pin |
| Logon hosted service | **`false`** (re-pin) | after `35=A` |
| Worker `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback **`false`** | log only |
| `GET /api/settings` | `runtime.RealCopyEnabled` → **false** | display |
| `docker-compose.yml` / `launchSettings.json` | key **absent** | N/A |

Setting `REAL_COPY_EXECUTION_ENABLED=true` in `.env` **cannot** arm `/api/settings` and **cannot** emit `35=D`. Still **do not flip it** — the next sender must find the documented name **off**.

---

## 4. Name collision: `LiveCopyEnabled` is a third flag (dead)

```44:48:D:\Prop\apps\api\appsettings.json
  "FeatureFlags": {
    "ShadowTradingEnabled": true,
    "LiveCopyEnabled": false,
    "AutoPromotionEnabled": false
  },
```

`SettingsController` (`[Route("api/settings")]`) reads `FeatureFlags:LiveCopyEnabled` with default **`false`** and can PUT Redis `settings:flags:live_copy`. **That controller is not mapped** (`Program.cs` never calls `AddControllers` / `MapControllers`). E038: live `PUT /api/settings` is **405 Allow=GET**.

`LiveCopyEnabled` ≠ `FEATURE_COPY_TRADING_ENABLED` ≠ `REAL_COPY_EXECUTION_ENABLED`. Dashboard cannot enable live copy through any of them.

---

## 5. Copy-to-cTrader cannot send live orders (no loss)

### 5.1 Only outbound FIX type is Logon (`35=A`)

`CTraderFixSession.BuildLogon` tag list (measured this pass):

| Tag | Value | Meaning |
|---|---|---|
| 35 | **`A`** | Logon |
| 34 | seq | MsgSeqNum |
| 49 / 56 | sender / target | CompIDs (`cServer` default) |
| 50 / 57 | QUOTE or TRADE | SenderSubID / TargetSubID |
| 52 | UTC | SendingTime |
| 98 / 108 / 141 | 0 / 30 / Y | Encrypt / HeartBtInt / Reset |
| 553 / 554 | account id / password | credentials (not printed) |

**No** `(35, "D")`, **no** `OrderQty`, **no** `ClOrdID` on the wire. After `ssl.ReadAsync` of the logon reply, the method **returns**. No second write.

Product `NewOrderSingle` / `35=D` in `src` + `apps` `*.cs`: comments, logs, `LastError` strings, and `MayRetryNewOrderSingle` (status predicate). **Zero builders.** Matches E034 `SAFE_BY_ABSENCE`.

### 5.2 Risk engine is not a send path

`RiskEngine.Evaluate` computes `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). When `RealExecutionEnabled==false` and action is not `CloseExposure`, the `if` body is **empty** (L90–93) — shadow still evaluates, send bit stays false.

`AddTraderIntelligence` **does not register** `RiskEngine`. Only `tests/Unit/RiskEngineTests.cs` constructs it (fixture `RealExecutionEnabled = false`). `AllowFixSend` has **zero** socket writer.

`/api/reconciliation/status` is a stub (`unknownPositions=0`, note: `"NewOrderSingle still off"`).

### 5.3 UI honesty

`LiveCopyPage.tsx` is an 8-line stub. It tells the operator that NewOrderSingle is disabled and that gates still include `REAL_COPY_EXECUTION_ENABLED`. It does **not** read `useSettings` / env.

### 5.4 What flipping flags would *not* do

| Operator action | `/api/settings` FEATURE | `/api/settings` REAL_COPY | `35=D` |
|---|---|---|---|
| Default (current) | `false` literal | `false` (runtime pin) | **none** |
| `.env` `FEATURE_COPY_TRADING_ENABLED=true` | still **false** | still **false** | **none** |
| `.env` `REAL_COPY_EXECUTION_ENABLED=true` | still **false** | still **false** (pin) | **none** |
| env `CTrader__RealCopyExecutionEnabled=true` | still **false** | still **false** | worker **warns**, still **no send** |
| Source-edit `RealCopyEnabled = true` | would display **true** | would display **true** | **still none** (no builder) |

Residual capital risk is **only** the future day a sender is added while a flag is `true`. That is why both defaults must stay **false**.

---

## 6. ALL Achiever + Starwave groups / ALL manager traders (read-only)

Flags do **not** filter the catalog. Ingest does not consult either name.

### 6.1 Registration — both brokers, native Manager

`LiveMt5Registration.CreateConnectors` always returns **Achiever + StarwaveFX** `NativeMt5BrokerConnector` instances. Dummy/Fake is refused:

```35:36:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

`GetAccountsAsync(null)` walks **every** discovered group (`GetAccountsCore`: if `group` is null, `foreach (var g in GetGroupsCore())`).

### 6.2 Group walk — no plan-name filter

```152:185:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var arr = _manager!.GroupCreateArray();
            ...
                var res = _manager.GroupRequestArray("*", arr);
            ...
            if (list.Count == 0)
            {
                ...
                    var total = _manager.GroupTotal();
                    for (uint i = 0; i < total; i++)
                    {
                        if (_manager.GroupNext(i, grp) != MTRetCode.MT_RET_OK)
                            continue;
                        AddGroup(list, seen, grp);
                    }
```

`"*"` + `GroupTotal`/`GroupNext` fallback is **all groups this manager login can see**. Plan mappings (`MT5_GROUP_2STEP_DEMO`, …) are **labels**, not fetch filters (`docs/architecture.md` L24).

### 6.3 Trader walk — every login in every group

`ReadAccountsForGroup`: `UserRequestArray(gname)` → fallback `UserGetByGroup` → if empty, `UserLogins` + `UserRequestByLogins`. `SyncCatalogAsync` upserts **all** groups then `GetAccountsAsync(null, ct)` (all accounts).

`LiveIngestHostedService` iterates `registry.All()` (both connectors). `/api/ops/resync` iterates `{"ACHIEVER","STARWAVEFX"}`.

### 6.4 Prior live census (this slot did not re-attach)

From `LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md` (2026-08-18; dashboard `/api/groups` = 18, `/api/traders` = 8460):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via whitelist HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | OK direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (manager-visible): `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.

Starwave groups (manager-visible): `Starwave\cent\FX1\grp1/2`, `Starwave\demo\FX2\grp1/2`, `Starwave\real\FX3\grp1–5`, `Starwave\real\FX3\LP`.

If the server has more groups, they are **outside this manager permission set**. That is not a software filter and not a feature-flag filter.

Full login dump (no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`.

---

## 7. C++ YoPips PropFirm tree — not the copy flags

Consulted `D:\Projects\YoPips\Backend\C++ Backend PropFirm`:

| Probe | Result |
|---|---|
| `src/` + `config/` grep `FEATURE_COPY_TRADING_ENABLED` / `REAL_COPY_EXECUTION_ENABLED` / `CopyTrading` / `LiveCopy` | **0** |
| `config/app_config.h` | MT5 / proxy / terminal / payments. **No** copy-trading flag fields. |
| `.env` in that tree | **0** hits for either token (grep). Secrets **not** printed. |

`mt5_group_probe` / `GetAllGroups` (`GroupTotal`+`GroupNext`) is the **sibling read-only enumerator** used to prove group lists. C++ `SendTrade` exists for the **prop-firm terminal** product, not Prop `apps/fix-worker`. It is **not** wired as a cTrader `35=D` path. Do not treat YoPips `SendTrade` as copy execution.

---

## 8. Risk to capital (this process)

| Path | Can it open a Pepperstone/cTrader position? |
|---|---|
| Manager catalog / deals / scoring | **No** (read + local persist) |
| FIX `35=A` logon | **No** (session only) |
| `/api/ops/resync` | **No** (catalog + deals + score) |
| `FEATURE_COPY_TRADING_ENABLED` | **No** (unread / literal false) |
| `REAL_COPY_EXECUTION_ENABLED` | **No** (unbound env; runtime pinned false; no builder) |
| `CTrader:RealCopyExecutionEnabled=true` | **No** (worker warning only) |
| `RiskEngine.AllowFixSend` | **No** (unregistered; no socket) |
| Emergency flatten | **No** sender exists |

**Risk to capital from this process: NONE** (`SAFE_BY_ABSENCE` + both defaults `false` + runtime pin). Residual risk is **future**: adding a `35=D` builder while any send-named flag is `true`. Therefore **do not enable** `FEATURE_COPY_TRADING_ENABLED` or `REAL_COPY_EXECUTION_ENABLED`.

---

## 9. What this slot does **not** claim

- Not a live re-measure of 18/8460 (prior pin reused; this slot is flag-default research).
- Not A100 / A101 PASS. Session-on is **not** a send license.
- Not a proof that the env names are wired chokes. They are **not**.
- Not authorization to add `35=D`, flatten, or `MapControllers` on `SettingsController`.
- Localhost `GET /api/settings` was **not** re-hit this pass (SSRF/tooling block on `127.0.0.1`). Wire body is inferred from current `Program.cs` L70–83 + DI pin. E038's older quote of a **literal** `REAL_COPY=false` is **stale** — current source uses `runtime.RealCopyEnabled` (still false).

---

## 10. Files read (this pass)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | settings dictionary, health, resync, no controllers |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false`; dummy refuse |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | re-pin `false` after logon |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | only `35=A` |
| `D:\Prop\apps\fix-worker\Worker.cs` | `CTrader:RealCopyExecutionEnabled` fallback false |
| `D:\Prop\apps\fix-worker\appsettings.json` | no CTrader / flag keys |
| `D:\Prop\apps\api\appsettings.json` | `LiveCopyEnabled: false` (other name) |
| `D:\Prop\apps\api\appsettings.Development.json` | no flags |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unmapped `LiveCopyEnabled` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | flags absent |
| `D:\Prop\docker-compose.yml` | flags absent |
| `D:\Prop\.env` | L73 + L106 both `false` (values only) |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | loads all keys; no type bind |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | snapshot / unexpected-arm note |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | overview runtime; risk/FIX literals false |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | all connectors, catalog-first |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `GroupRequestArray("*")` + all users |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever + Starwave only |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` bit; not registered |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | TRADE `NewOrderSingle off` |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | static no-send copy |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §41 | law |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` | no copy flags |

---

## 11. Binding one-liner for the orchestrator

```text
SLOT 37 VERDICT=CONFIRMED_BOTH_DEFAULT_FALSE_NO_LIVE_SEND
FEATURE_COPY_TRADING_ENABLED default=false (env L106 + Program.cs L76 literal; UNBOUND)
REAL_COPY_EXECUTION_ENABLED default=false (env L73 + POCO=false + DI pin + logon re-pin; env name NOT bound)
35=D=ABSENT SAFE_BY_ABSENCE
MANAGER_FETCH=ALL_GROUPS_ALL_TRADERS (prior 18/8460)
RISK_TO_CAPITAL=NONE
DO_NOT_FLIP_EITHER_FLAG
```
