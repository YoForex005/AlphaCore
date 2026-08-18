# W500_RESEARCH_17 — `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults

| Field | Value |
|---|---|
| Slot | **17** |
| Date | 2026-08-18 |
| Agent | W500 research 17 (flag defaults vs live-copy no-loss) |
| Assigned | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Trees read | `D:\Prop` product + docs; `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (name search only) |
| Product source modified | **No** |
| Secrets printed | **None.** `.env` quoted only as flag names `=false`. No MT5 / FIX / proxy / DB passwords. |
| Method | `grep` + `read_file` on C# / JSON / env / architecture / YoPips. No shell hash. Nothing answered from memory. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A hardcoded API `false` is a **display floor**, not a send gate. Absence of `NewOrderSingle` / `35=D` is **SAFE_BY_ABSENCE**, not proof that the env token is wired. `FEATURE_COPY_TRADING_ENABLED` is **not** architecture §41.

---

## 0. Verdict (binding)

| Question | Measured answer |
|---|---|
| `FEATURE_COPY_TRADING_ENABLED` default | **`false`** |
| `REAL_COPY_EXECUTION_ENABLED` default | **`false`** |
| Any committed / local product config `=true`? | **No** (`*.cs` / `*.json` / `.env` / `docker-compose.yml` / `launchSettings.json`: **0** hits of either `=true`) |
| Does fetch of ALL Achiever+Starwave groups/traders consult either flag? | **No** |
| Can this process emit live cTrader `35=D` / NewOrderSingle? | **No** — builder **absent**; runtime send bit **pinned false** |
| Risk to capital from copy path | **NONE** (this process) |

**Slot-17 verdict: `PASS_DEFAULTS_FALSE_NO_LIVE_SEND`.**

Both named flags default **OFF**. Manager catalog ingest is **read-only** and is **not** gated by either flag, so the “fetch ALL groups + ALL manager traders” goal is not blocked by copy flags. Live copy cannot open a losing Pepperstone/cTrader position from this tree: there is no `35=D` assembler, `LiveRuntimeStatus.RealCopyEnabled` is constructed `false` and **re-forced false after FIX logon**, and risk `AllowFixSend` requires a caller bit that unit tests keep `false`.

**Gate honesty:** `REAL_COPY_EXECUTION_ENABLED` (architecture name) is **not** bound onto `CTraderFixOptions` by ASP.NET env convention. `FEATURE_COPY_TRADING_ENABLED` is **never** `GetValue`’d in C#. Safety today is **default-false + pin-false + SAFE_BY_ABSENCE**, not a single named choke that a unit test proves refuses `35=D` on a logged-on TRADE socket.

---

## 1. The two names are not the same control

| Name | Kind | Architecture? | Consumed by a worker? |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | §41 / §56 **config floor** for new real `NewOrderSingle` | **Yes** (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1572, L2101) | **No env bind.** Worker reads **`CTrader:RealCopyExecutionEnabled`**, default `false`. API exposes `runtime.RealCopyEnabled` (pinned `false`). |
| `FEATURE_COPY_TRADING_ENABLED` | Extra “Feature Flags” env + API dictionary key | **No.** A75: do **not** invent this as an architecture name. D61: **not** a substitute for `REAL_COPY_EXECUTION_ENABLED`. | **No.** Sole C# hit is a **literal `false`** in `GET /api/settings`. Env value is unused. |

Flipping `FEATURE_COPY_TRADING_ENABLED=true` in `.env` **cannot** change `/api/settings` (literal) and **cannot** create a send path.

Flipping `REAL_COPY_EXECUTION_ENABLED=true` in `.env` **cannot** set `CTraderFixOptions.RealCopyExecutionEnabled` (wrong key) and **cannot** override DI/`CTraderFixLogonHostedService` pins. Even if `CTrader:RealCopyExecutionEnabled=true`, `apps/fix-worker` only **logs** and still has **no** `35=D` function.

---

## 2. Measured defaults (product)

### 2.1 Architecture law (design default)

Architecture §41 default block (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1568–1572):

- `CTRADER_FIX_ENABLED=true`
- `CTRADER_FIX_QUOTE_ENABLED=true`
- `CTRADER_FIX_TRADE_SESSION_ENABLED=true`
- `REAL_COPY_EXECUTION_ENABLED=false`

§56 example ends with the same floor (`L2101`): `REAL_COPY_EXECUTION_ENABLED=false`.

`FEATURE_COPY_TRADING_ENABLED` does **not** appear in that architecture file (`grep`: **0** hits).

Docs that restate the architecture floor: `D:\Prop\docs\architecture.md` L20, `D:\Prop\docs\ctrader-fix.md` L73, `D:\Prop\docs\deployment.md` L82, `D:\Prop\README.md` L28.

### 2.2 C# initializer — send-license property

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

### 2.3 Runtime pin (API / ingest host) — cannot be armed by env

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

After optional QUOTE/TRADE **logon only** (`35=A`), the hosted FIX service **forces the bit back to false**:

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

`LiveRuntimeStatus.Snapshot()` copy note when false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` (`LiveRuntimeStatus.cs` L42–44).

### 2.4 Live settings API (the host operators actually hit)

`Program.cs` maps **minimal** `GET /api/settings`. There is **no** `AddControllers` / `MapControllers` anywhere under `D:\Prop` (`grep` on `*.cs`: **0**). The MVC `SettingsController` is **dead code**.

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
| `REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` | **`false`** (DI pin + logon pin) |
| `FEATURE_COPY_TRADING_ENABLED` | **literal `false`** | **`false`** (ignores env) |

`/api/health` also publishes `realCopyEnabled = runtime.RealCopyEnabled` (L54). `/api/reconciliation/status` notes `"NewOrderSingle still off"` (L68).

### 2.5 FIX worker (status loop, not a sender)

```21:22:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

`apps/fix-worker/appsettings.json` is **logging only**. Key **absent** → fallback **`false`**. Even if `real` is true, the loop only stamps QUOTE/TRADE `Disconnected` and logs a warning; it does not build FIX.

### 2.6 Alias names that are also default-false (not the assigned tokens)

| Alias | Location | Default | Wired to send? |
|---|---|---|---|
| `FeatureFlags:LiveCopyEnabled` | `apps/api/appsettings.json` L44–47 | **`false`** | **No.** Read only by unmapped `SettingsController`. |
| `FeatureFlags:LiveCopyEnabled` GetValue fallback | `SettingsController.cs` L38 | **`false`** | **No.** Controller not mapped. PUT would write Redis `settings:flags:live_copy` only. |
| `RiskEvaluationRequest.RealExecutionEnabled` | `RiskEngine.cs` L147–150 | caller-supplied; unit `Base()` **`false`** (`RiskEngineTests.cs` L72) | Sets DTO `AllowFixSend`. **Zero live FIX callers.** `RiskEngine` is **not** registered in `AddTraderIntelligence`. Test `Real_flag_false_never_allows_fix_send` asserts `AllowFixSend == false`. |

`AllowFixSend = RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy`. When the flag is false, Approve may still occur for shadow evaluation; **send bit stays false**.

### 2.7 Local env (gitignored) — names only

`D:\Prop\.env` (no values other than the boolean):

- L73: `REAL_COPY_EXECUTION_ENABLED=false`
- L106: `FEATURE_COPY_TRADING_ENABLED=false`

Sibling extras on the same “Feature Flags” block (not this slot’s send license): `FEATURE_CTRADER_HEDGING_ENABLED=false`, `FEATURE_ML_SCORING_ENABLED=false`, `FEATURE_NEWS_FILTER_ENABLED=false`, `FEATURE_TRADE_RECONSTRUCTION_ENABLED=true`.

Worktree has **no** `.env.example` (`grep` `*.env*`: only `.env`). `docker-compose.yml` and all `launchSettings.json`: **0** hits for either token. `apps/api/appsettings.Development.json`: **no** feature-flag keys.

---

## 3. Live send is off (no-loss for copy)

### 3.1 No `35=D` builder

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` `BuildLogon` emits **only** `35=A` (Logon) plus heartbeat/reset/username tags. `grep` of `Fix.CTrader` `*.cs` for `35=D` / `NewOrderSingle` / `OrderQty`: **comments and log strings only** in `CTraderFixOptions` and `CTraderFixLogonHostedService`. No `MsgType=D` field list.

Product `*.cs` `OrderSend` / `DealerSend` / `TradeRequest`: **0** hits.

### 3.2 Logon ≠ license

`CTraderFixLogonHostedService` may TLS-connect `5211`/`5212` and send Logon. That can leave QUOTE/TRADE `LoggedOn=true` (prior live census: both true). It **still** sets `RealCopyEnabled = false` and never sends NewOrderSingle.

### 3.3 Web copy page

`LiveCopyPage.tsx` is static copy: SHADOW only; NewOrderSingle disabled; names `REAL_COPY_EXECUTION_ENABLED` as a still-required gate. It does not read `FEATURE_COPY_TRADING_ENABLED`. Settings page dumps `/api/settings` JSON (both flags false as in §2.4).

---

## 4. Fetch ALL Achiever + Starwave groups and ALL manager traders — flags do not block

### 4.1 Both brokers are registered regardless of copy flags

```23:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
```

`BrokerCodes.Achiever = "ACHIEVER"`, `BrokerCodes.StarwaveFx = "STARWAVEFX"`. `FEATURE_*` / `REAL_COPY_*` are **not** read in this file.

`LiveIngestHostedService` loops `registry.All()` and calls `SyncCatalogAsync` then `SyncBrokerAsync` + score **every login**. `DealIngestionService` / `LiveIngestHostedService` contain **zero** references to either flag.

`POST /api/ops/resync` hard-loops `new[] { "ACHIEVER", "STARWAVEFX" }` (`Program.cs` L121).

### 4.2 Groups = Manager `*` (not plan-filtered)

```155:155:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.GroupRequestArray("*", arr);
```

Fallback if the array is empty: `GroupTotal()` + `GroupNext`. `AddGroup` skips only blank/duplicate names — **no** `MT5_GROUP_*` / plan-path filter.

### 4.3 Accounts = every group, every user

`SyncCatalogAsync` → `GetAccountsAsync(null, ct)`. `GetAccountsCore(null)` walks **all** `GetGroupsCore()` names, then `UserRequestArray` / `UserGetByGroup` / `UserLogins` + `UserRequestByLogins` per group.

`EnabledForAnalysis` defaults **`true`** on upsert (`EfTradingStore` / `Mt5Group`) and is **not** used as a fetch filter.

### 4.4 Prior measured census (do not treat as this pass’s live probe)

`D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (2026-08-18): Achiever 8 groups / 6512 traders; StarwaveFX 10 groups / 1948 traders; dashboard `/api/traders` **8460**, `/api/groups` **18**. This slot did **not** re-hit the Manager API.

---

## 5. YoPips C++ backend (requested tree)

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (and parent `D:\Projects\YoPips`) for `FEATURE_COPY_TRADING_ENABLED` / `REAL_COPY_EXECUTION_ENABLED`: **0** hits.

YoPips `copy_trade_*` tables / `COPY_TRADING_RESTRICTION` are **detection / admin restriction**, not a cTrader FIX sender. They do not define these defaults and cannot place Pepperstone orders for this lab.

---

## 6. Surface matrix (this pass)

| Surface | `FEATURE_COPY_TRADING_ENABLED` | `REAL_COPY_EXECUTION_ENABLED` / twin |
|---|---|---|
| Architecture §41 / §56 | **absent** | **`false`** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | n/a | **`= false`** |
| `LiveRuntimeStatus` / DI | n/a | **pinned `false`** |
| `CTraderFixLogonHostedService` | n/a | **forced `false` after logon** |
| `GET /api/settings` | **literal `false`** | **`runtime.RealCopyEnabled` → false** |
| `apps/fix-worker` | not read | `GetValue("CTrader:RealCopyExecutionEnabled", false)` |
| `appsettings*.json` / compose / launchSettings | **absent** | **absent** |
| Local `.env` | **`false`** | **`false`** |
| Any product `=true` | **0** | **0** |
| Ingest / GroupRequest `*` / UserRequest | **not consulted** | **not consulted** |
| YoPips C++ PropFirm | **0** | **0** |
| `35=D` builder | n/a | **missing** |

---

## 7. Goal mapping

| Goal | Status | Evidence |
|---|---|---|
| Fetch **ALL** Achiever + Starwave groups | **Allowed by flags** (ingest ignores them; mask is `*`) | §4 |
| Fetch **ALL** manager traders | **Allowed by flags** (`GetAccountsAsync(null)` per group) | §4 |
| Copy to cTrader must **not** send live orders (no loss) | **Met** | Defaults **false** + pin **false** + **SAFE_BY_ABSENCE** (§2–§3) |
| Treat `FEATURE_COPY_TRADING_ENABLED` as the send license | **Wrong** | Not §41; unused; literal API false |

**Do not** set either flag to `true` to “try one lot.” Session-on is not a send license. Fetch/score/resync may continue while both stay false.

---

## 8. Residual (not capital-at-risk from copy)

1. **GATE_INCOMPLETE:** env `REAL_COPY_EXECUTION_ENABLED` is not bound to the POCO. A future binder without a refuse test could change meaning. Today flipping it still cannot emit `35=D`.
2. **Dead `SettingsController`:** if someone later maps MVC controllers, `LiveCopyEnabled` PUT writes Redis only; still no FIX send.
3. **FIX logon** may succeed (`35=A`). That is connectivity, not an order.
4. This slot did not re-run `/api/settings` HTTP or Manager connect.

---

## 9. Files read (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\Controllers\SettingsController.cs` (unmapped)
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\appsettings.Development.json`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (§41, §56)
- `D:\Prop\docs\architecture.md`
- `D:\Prop\.env` (flag lines only)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (prior census)
- YoPips: name search only; no flag files to quote

---

## 10. JSON (slot contract)

```json
{
  "slot": 17,
  "verdict": "PASS_DEFAULTS_FALSE_NO_LIVE_SEND",
  "evidence": "FEATURE_COPY_TRADING_ENABLED default false (API literal Program.cs L76; .env L106; never GetValue'd). REAL_COPY_EXECUTION_ENABLED default false (arch §41 L1572 / §56 L2101; CTraderFixOptions.RealCopyExecutionEnabled=false; DI + FIX logon pin RealCopyEnabled=false; worker GetValue CTrader:RealCopyExecutionEnabled false). No product =true. No 35=D builder (logon 35=A only). Ingest GroupRequestArray(*) + GetAccountsAsync(null) for ACHIEVER+STARWAVEFX ignores both flags.",
  "risk_to_capital": "NONE"
}
```
