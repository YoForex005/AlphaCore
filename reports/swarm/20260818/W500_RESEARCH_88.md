# W500_RESEARCH_88 — `REAL_COPY_EXECUTION_ENABLED` must stay **false**; no `35=D` until risk/recon gates

| Field | Value |
|---|---|
| Slot | **88** |
| Date | 2026-08-18 |
| Agent | W500 research 88 (flag floor vs live-copy no-loss) |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_88.md` |
| Assigned | Confirm `REAL_COPY_EXECUTION_ENABLED` **must stay false**. **No** `35=D` NewOrderSingle until risk/recon gates. Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Trees read | `D:\Prop` product + architecture + live-census artifacts; `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`src` + `CMakeLists.txt`) |
| Product source modified | **No** |
| Test source modified | **No** |
| Secrets printed | **None.** `.env` quoted only as flag names `=false`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Method | `read_file` + `grep` on C# / JSON / env / architecture / YoPips. This slot did **not** live-attach Manager or FIX and did **not** flip any flag. Census numbers are the already-measured 2026-08-18 probe. |
| Binding law | Architecture **§41** (flag floor), **§42** (block new executions until recon), **§68** (19 go-live boxes), **§70** (14 live-FIX boxes); A009 / A100 / A101 / C14; E002. |
| Siblings (do not collapse) | W500_RESEARCH_57 (flag *defaults*), W500_RESEARCH_50 (`CTraderFixSession` `35=D` census), W500_RESEARCH_59 (risk hop missing), A003 (no-loss), E002 (no live send). This file answers **must it stay false, and may we emit 35=D yet?** |

**Honesty rule:** a compile-time `= false` is a default. A DI pin is a runtime floor. A log line that *names* NewOrderSingle is **not** a builder. A TLS **Logon `35=A`** is **not** a NewOrderSingle. `AllowFixSend` on a DTO is **not** a socket write. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §68 / §70 from this file. Session-on is **not** a send license. `REAL_COPY_EXECUTION_ENABLED` is necessary and **not sufficient**.

---

## 0. Verdict (binding)

**`REAL_COPY_EXECUTION_ENABLED` must stay `false`. Do not emit `35=D` NewOrderSingle until risk + recon gates (and the rest of §68 / §70) are measured PASS.**

| Question | Measured answer |
|---|---|
| Must the flag stay false **today**? | **YES** |
| Any product assignment `REAL_COPY_EXECUTION_ENABLED=true` / `RealCopyEnabled=true`? | **No** in `apps/` + `src/` + `.env` + `appsettings*.json` + `docker-compose.yml` + `launchSettings.json` |
| Architecture §41 design default | **`false`** (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1572; L1587 is the *enable example* only) |
| §68 go-live (live-copy license) | **0 / 19 PASS** (A100 / C14) — one FAIL blocks enablement |
| §70 live FIX acceptance | **0 / 14 PASS** (A101) |
| Risk hop `CopyIntent → RiskEngine.Evaluate → ExecutionIntent` | **MISSING** (0 product `Evaluate` callers; 0 `ExecutionIntent` writers) |
| Recon gate on a send path | **MISSING** (API `/api/reconciliation/status` is a stub note; no OrderMassStatus / positions request) |
| Can this process emit live cTrader `35=D`? | **No** — builder **absent**; runtime send bit **pinned false** |
| Does fetch of ALL Achiever+Starwave groups/traders consult the flag? | **No** — flag-blind Manager **read** |
| Risk to capital from this copy path | **NONE** (this process) |

**Slot-88 verdict: `CONFIRMED_MUST_STAY_FALSE_NO_35D`.**

Fetch-all is allowed and is already the live catalog walk. Live copy is **not** allowed. The only honest operating mode is:

```text
ALLOW:  GroupRequestArray("*") + UserRequestArray / fallbacks (ALL manager-visible groups + logins)
ALLOW:  deal/position ingest, reconstruct, score, SHADOW_ONLY CopyIntent
ALLOW:  optional FIX QUOTE/TRADE TLS Logon 35=A (session proof / future recon)
FORBID: 35=D NewOrderSingle, 35=F/G cancel/replace
FORBID: REAL_COPY_EXECUTION_ENABLED=true   -- until §68 19/19 AND §70 14/14
        AND risk hop wired AND recon clean AND persist-before-send
        AND explicit reviewed enable
```

Flipping the env token **cannot** place an order today (wrong bind + no builder). It **must still stay false**: (1) architecture §41 / §68 / §70 law; (2) `LiveRuntimeStatus` would otherwise advertise `LIVE SEND ARMED — unexpected`; (3) a later sender must not inherit an already-true license; (4) risk + recon are **not** PASS.

---

## 1. Why the flag is required to stay false (law, not taste)

### 1.1 Architecture §41 — necessary, not sufficient

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1564–1590:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

In-file meaning: connect, receive prices, request orders/positions, validate FIX — **without automatically placing new real orders**. Actual NewOrderSingle **requires** `REAL_COPY_EXECUTION_ENABLED=true` **plus** runtime risk-engine healthy.

§56 sample repeats the floor (`L2101`): `REAL_COPY_EXECUTION_ENABLED=false`.

Docs that restate the floor (not product binders): `D:\Prop\docs\architecture.md` L20; `D:\Prop\docs\ctrader-fix.md` L73; `D:\Prop\docs\deployment.md` L82; `D:\Prop\README.md` L28.

### 1.2 Architecture §42 — recon before new executions

Same architecture file L1594–1606: after TRADE login, **block new executions** → OrderMassStatusRequest → RequestForPositions → consume reports. Product code has **no** those MsgTypes on the wire (see §4). Therefore recon is **not** a passed gate.

### 1.3 Architecture §68 + §70 — conjunction still FAIL

§68 (`L2605–2628`): **Do not enable real copying until all 19 boxes are true.** Last live-copy scorecard: **0 PASS / 19 FAIL** (`C14_golive_still_fail.md`, same integer as `A100_golive_gates.md`). Boxes that name this topic include: trade-session stable, cTrader reconciliation after restart, unknown-state recovery, position-sizing conversion, risk unit/integration, stale quote/signal, kill switch, manual review.

§70 (`L2658–2676`): 14 live-FIX items. Last scorecard: **0 / 14 FAIL** (`A101_live_fix_acceptance.md`). Items 11 and 14 are exactly this slot: *risk-engine rejection happens before FIX send*; *reconciliation blocks execution while inconsistent*. Item 12 is the flag itself.

**Conjunction for the first legal `35=D` (A009 / A101, restated):**

```text
§68 19/19 PASS
AND §70 14/14 PASS
AND REAL_COPY_EXECUTION_ENABLED=true   (explicit, reviewed — not today's state)
AND RiskEngine.Evaluate on the live hop with AllowFixSend
AND TRADE READY_FOR_EXECUTION (Logon + recon clean)
AND persist unique ClOrdID before send
AND MayRetryNewOrderSingle false on unknown
```

One FAIL blocks send. Today the conjunction is **false** on every conjunct except “flag is currently false.”

---

## 2. Measured product floors (the flag is false in every live surface)

### 2.1 POCO default

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

`Configure<CTraderFixOptions>`: **0** hits under `D:\Prop` `*.cs`. Architecture env name `REAL_COPY_EXECUTION_ENABLED` is **not** bound onto this POCO (would need `CTrader__RealCopyExecutionEnabled`).

### 2.2 DI pin — cannot be armed by env

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

### 2.3 Re-pin after optional FIX Logon

`CTraderFixLogonHostedService` may open TLS and send **only** `35=A`. After that it **forces the bit back to false**:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
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

Logon **true** is still **not** a send license. The hosted service says so in the same log line.

### 2.4 Settings / health API

`GET /api/settings` (`apps/api/Program.cs` L70–77):

| Key | Source | Effective value |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` | **`false`** (DI + logon pin) |
| `FEATURE_COPY_TRADING_ENABLED` | **literal `false`** | **`false`** (ignores `.env`) |

`GET /api/health` L54: `realCopyEnabled = runtime.RealCopyEnabled`.  
`GET /api/reconciliation/status` L62–68: stub zeros + `note = "recon runs only after FIX TRADE logon; NewOrderSingle still off"`.

No `AddControllers` / `MapControllers` under `D:\Prop` `*.cs`. MVC `SettingsController` (`FeatureFlags:LiveCopyEnabled` default false) is **dead code**.

`LiveRuntimeStatus.Snapshot()` when false (`LiveRuntimeStatus.cs` L42–44): `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` The true branch is `"LIVE SEND ARMED — unexpected"` — another reason not to flip the bit.

### 2.5 FIX worker — different key, log-only refuse

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

`apps/fix-worker/appsettings.json` is logging only — **no** `CTrader` section. `GetValue` fallback is therefore **`false`**. Even if an operator injects `CTrader__RealCopyExecutionEnabled=true`, the worker **only logs** and still has **no** `35=D` function. The English “refuses until risk/reconciliation gates pass” is the **correct policy**; it is **not** a coded choke on a sender (the sender does not exist).

### 2.6 `.env` floor (names + booleans only)

`D:\Prop\.env` (gitignored):

| Line | Token (value only as written) |
|---|---|
| 73 | `REAL_COPY_EXECUTION_ENABLED=false` |
| 70–72 | `CTRADER_FIX_ENABLED` / `QUOTE` / `TRADE_SESSION` = `true` (session on ≠ send) |
| 106 | `FEATURE_COPY_TRADING_ENABLED=false` |

`EnvFile.FindAndLoad()` loads every `KEY=VALUE` into the process environment. That surfaces `REAL_COPY_EXECUTION_ENABLED` as a **top-level** config key. **No** product `GetValue` reads that exact name. Loading ≠ binding ≠ send license.

`apps/api/appsettings.json` leftover `FeatureFlags.LiveCopyEnabled: false` is a **different** name and is unbound by the live minimal API.

### 2.7 UI

`apps/web/src/pages/LiveCopyPage.tsx` L5 (JSX literal, not a settings read): SHADOW only; Pepperstone/cTrader NewOrderSingle disabled; gates named: FIX TRADE logon + recon + risk approve + `REAL_COPY_EXECUTION_ENABLED`.

`OverviewPage.tsx` L15: “Live FIX NewOrderSingle is off — no capital at risk from this dashboard.”

---

## 3. Fetch ALL Achiever + Starwave groups / traders — flag-blind, read-only

The goal “fetch ALL groups and ALL manager traders” is **orthogonal** to the send flag. The catalog must stay complete **while** live copy stays off.

### 3.1 Both brokers, password-gated, flag-unread

`LiveMt5Registration.CreateConnectors` (`src/Infrastructure/Mt5Live/LiveMt5Registration.cs` L20–49) always constructs **two** `NativeMt5BrokerConnector`s:

- `ACHIEVER` — `MT5_*` + optional HTTP proxy (`ACHIEVER_PROXY_*`)
- `STARWAVEFX` — `MT5_STARWAVEFX_*`, **`ProxyEnabled = false` hardcoded** (L45)

Zero reads of `REAL_COPY_EXECUTION_ENABLED`. `AddTraderIntelligence` **throws** unless both manager passwords pass `IsSecret` (no Fake/dummy on the live host).

### 3.2 Catalog walk

`DealIngestionService.SyncCatalogAsync` (`src/Application/Ingestion/DealIngestionService.cs` L38–51):

1. `GetGroupsAsync`
2. `GetAccountsAsync(null, …)` — `null` = **every** group just fetched

`NativeMt5BrokerConnector.GetGroupsCore` L155: `GroupRequestArray("*", arr)`; fallback `GroupTotal` + `GroupNext`.

`GetAccountsCore` with `group == null` walks every group name. Per group `ReadAccountsForGroup` L223: `UserRequestArray`; fallbacks `UserGetByGroup` / `UserLogins` + `UserRequestByLogins`.

No `Take`/`Skip` on this walk. No copy-flag check. Positions ingest uses `GetGroupPositionsAsync("*")` or every account.

`LiveIngestHostedService` iterates `registry.All()` (both connectors). `/api/ops/resync` hard-codes `["ACHIEVER", "STARWAVEFX"]`. Dashboard `GetTradersAsync` (`EfDashboardQueries.cs` L85–118) is `foreach (var account in accounts)` left-join scores — **all** `Mt5Accounts`, not scores-only.

### 3.3 Already-measured census (this slot did not re-attach)

From `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` and `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (2026-08-18):

| Broker | Connect | Groups | Manager traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Login dump (no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`. `/api/traders` listed **8460**; `/api/groups` listed **18**.

Keeping `REAL_COPY_EXECUTION_ENABLED=false` does **not** shrink this walk. Do **not** add a plan-group filter as a “safety” substitute for the send flag.

---

## 4. No `35=D` NewOrderSingle (no-loss is `SAFE_BY_ABSENCE`)

### 4.1 Only outbound MsgType on a live socket is Logon `A`

`CTraderFixSession.BuildLogon` (`src/Fix.CTrader/Sessions/CTraderFixSession.cs` L89–109) assembles:

- `(35, "A")` Logon
- 34 seq, 49/56 CompIDs, 50/57 SubIDs, 52 time
- 98=0, 108=30, 141=Y
- 553 username, 554 password

One `ssl.WriteAsync` (L49), one read, sockets disposed (`using` / `await using`). **No** keep-alive TRADE session that could later send `D`.

| Pattern in `CTraderFixSession.cs` (135/135 lines) | Hits |
|---|---:|
| `35=D` / `(35, "D")` / `NewOrderSingle` | **0** |
| `OrderQty` / `ClOrdID` / `OrdType` / `Side` / `StopPx` | **0** |
| outbound tag 35 actually built | **`"A"` only** |

Other `Fix.CTrader` MsgTypes (`CTraderQuoteService` `y`/`V`; harness `A`/`3`/`0`/`y`/`X`/`8`) are **in-memory** mappers / a simulator. They do not open `*.c-trader.com`.

Product `*.cs` `NewOrderSingle` hits (this pass): comment (`CTraderFixOptions` L33), log (`CTraderFixLogonHostedService` L70), `LastError` strings (seeders + fix-worker), snapshot copy (`LiveRuntimeStatus` L44), FSM name (`MayRetryNewOrderSingle` — status math only). **Zero builders.**

`TraderIntelligence.Fix.CTrader.csproj`: Hosting + Configuration + Logging + EF only. **No** QuickFIX/n. `grep` `QuickFix|QuickFIX` on `D:\Prop` `*.cs`/`*.csproj`: **0**.

`grep` `35=D` / `(35, "D")` on `D:\Prop\src` + `D:\Prop\apps` product C#: **0** wire literals.

`grep` `SendTrade` / `DealerSend` / `OrderSend` on `D:\Prop\src` `*.cs`: **0**.

`grep` `new ExecutionIntent` / `ExecutionIntents.Add` on `D:\Prop` `*.cs`: **0**. Entity + `DbSet` only.

### 4.2 Risk bit is fail-closed **and unwired**

`RiskEngine.Evaluate` (`src/Domain/Risk/RiskEngine.cs`):

- L84–85: `!Reconciled && IsIncreasing` → reject `VENUE_NOT_RECONCILED`
- L90–93: `RealExecutionEnabled == false` comments “never allows FIX send” (does not early-return; `allowSend` still false)
- L147–150:

```csharp
var allowSend = request.RealExecutionEnabled
                && request.KillSwitch == KillSwitchMode.None
                && request.Reconciled
                && request.VenueHealthy;
```

Unit fact `Real_flag_false_never_allows_fix_send` (`tests/Unit/RiskEngineTests.cs` L21–26) keeps `RealExecutionEnabled = false` and asserts `AllowFixSend == false`.

Product callers of `RiskEngine` / `IRiskEngine` / `.Evaluate(`: **definition + 5 unit facts only**. `AddTraderIntelligence` does **not** register `RiskEngine`. `AllowFixSend` is written on the record and **never read** by a sender.

Only CopyIntent writer: `EfTradingStore.PersistDemoShadowAsync` (`L251–310`) stamps `Status = "SHADOW_ONLY"` and never calls Evaluate. `TraderStateMachine.CanPromoteToLive => false` (`BaselineScorer.cs` L211). `FromBaseline` reachable set has **no** `LIVE`.

So the **required** hop “risk + recon before `35=D`” is **spec-true and product-missing**. That is why the flag must stay false **and** why a sender must not be added yet. Safety today is absence + pin, not a green go-live review.

### 4.3 YoPips C++ is not a second cTrader sender

`D:\Projects\YoPips\Backend\C++ Backend PropFirm`:

- `CMakeLists.txt` links Drogon / Postgres / CURL / OpenSSL / MT5 Manager DLLs. **No** QuickFIX / cTrader package.
- `grep` of `src` for `cTrader` / `NewOrderSingle` / `35=D` / `REAL_COPY` / `FIX.4`: **0**.
- `src/core` hits on `FIX` are **bug-fix comments** (`mt5_http_client.cpp` “FIX #4”, `mt5_types.h` “FIX #6”), not FIX 4.4.

YoPips cannot place Pepperstone `35=D` for this lab.

---

## 5. What would have to become true before the flag may flip

Do **not** treat this list as implemented.

| # | Gate | Current measure |
|---|---|---|
| 1 | Architecture §68 19/19 | **0/19** (C14) |
| 2 | Architecture §70 14/14 | **0/14** (A101) |
| 3 | Wired `CopyIntent → Evaluate → ExecutionIntent` | **NO_HOP** (W500_59) |
| 4 | Recon on TRADE (mass status + positions) blocks send while dirty | **MISSING** (API stub) |
| 5 | Persist unique ClOrdID **before** send; no retry on unknown | FSM helper exists; **no send to persist** |
| 6 | Quantity conversion MT5 lots → FIX `OrderQty` | dest-grid only; G7/G10 FAIL (W500_58) |
| 7 | `GuardedNewOrderSingle` refuses unless flag **and** `AllowFixSend` **and** `READY_FOR_EXECUTION` | **MISSING** |
| 8 | Committed configs remain `false` until a named manual review | **true today** — keep it |
| 9 | Unit test: LoggedOn TRADE + flag false ⇒ `Submit=0` | **untestable** (no Submit) |

Until that conjunction is evidenced on disk, **`REAL_COPY_EXECUTION_ENABLED` stays false** in `.env`, API runtime, POCO default, and any future binder.

**Do not** enable the flag “to try one lot.” **Do not** add a `35=D` builder in a fetch/catalog task.

---

## 6. Surface matrix (slot 88)

| Surface | `REAL_COPY_EXECUTION_ENABLED` / twin | May send `35=D`? |
|---|---|---|
| Architecture §41 / §56 | **false** (L1572 / L2101) | No |
| `CTraderFixOptions.RealCopyExecutionEnabled` | `= false` | No (unbound) |
| DI `LiveRuntimeStatus.RealCopyEnabled` | **pinned false** | No |
| FIX logon hosted service | re-forced **false** after `35=A` | No |
| `GET /api/settings` | `runtime.RealCopyEnabled` → **false** | No |
| `GET /api/health` | `realCopyEnabled` → **false** | No |
| `apps/fix-worker` | `CTrader:RealCopyExecutionEnabled` default **false**; log-only | No |
| `D:\Prop\.env` L73 | `=false` | No (unread as a choke) |
| `appsettings.json` | unset (`LiveCopyEnabled=false` is a **different** name) | No |
| Native Manager fetch | **unread** | N/A (read-only) |
| YoPips C++ | **0** hits | No |
| `35=D` builder | **absent** | **`SAFE_BY_ABSENCE`** |

---

## 7. What this slot did **not** do

- Did not flip `REAL_COPY_EXECUTION_ENABLED` or `CTrader:RealCopyExecutionEnabled`.
- Did not add a `35=D` / `F` / `G` builder.
- Did not live-attach Manager or FIX (census cited from `LIVE_MANAGER_FETCH_MEASURED.md`).
- Did not print secrets.
- Did not edit product source.
- Did not tick §68 / §70.

---

## 8. One-line

```text
REAL_COPY_EXECUTION_ENABLED MUST STAY false (arch §41 + POCO + DI/logon pin + .env L73).
§68 0/19 and §70 0/14; risk/recon hop missing. 35=D absent (SAFE_BY_ABSENCE).
Fetch ALL groups/traders is flag-blind (18/8460 prior). Risk to capital NONE.
```
