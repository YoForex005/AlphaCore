# W500_RESEARCH_28 — `REAL_COPY_EXECUTION_ENABLED` must stay **false**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_28.md` |
| Slot | **28** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (current worktree; source re-read this pass) |
| Assigned | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **No.** Credential names only. |
| Binding law | Architecture **§41 / §42 / §56 / §61 / §68 / §70**; `docs/architecture.md`; `README.md` Safety |
| Sibling pins (do not treat as this file) | A003 (no-loss gate), A009, A100/C14/D42 (§68 0/19), A101/D43 (§70 0/14), A47 (recon MISSING), D69/E002 (flag default), E034 (`35=D` census; **stale on transport**), LIVE_MANAGER_FETCH_MEASURED.md |
| Method | Read product C# under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`. Grep `35=D`, `NewOrderSingle`, `REAL_COPY*`, `GroupRequestArray`, `Evaluate(`. Read architecture §41/§68/§70. Read live census JSON + CREDENTIALS. Search YoPips C++ backend for FIX `35=D` / cTrader. **No product edit. No live `35=D` attempted. No secret values copied.** |

**Honesty rule:** wanting copy **and** no loss does not authorize a send. A TLS Logon (`35=A`) is **not** a NewOrderSingle. `AllowFixSend` on a risk DTO is **not** a socket write. A Manager `GroupRequestArray("*")` is **read-only**. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. One FAIL on §68 or §70 blocks enablement.

---

## 0. Verdict (binding)

**CONFIRMED: `REAL_COPY_EXECUTION_ENABLED` must stay `false`. No live `35=D` NewOrderSingle until risk + recon gates are measured PASS.**

Fetch-all Manager catalog is **allowed and required**. Live copy send is **forbidden**.

```text
MUST_STAY_FALSE
  REAL_COPY_EXECUTION_ENABLED = false
  CTrader:RealCopyExecutionEnabled = false
  LiveRuntimeStatus.RealCopyEnabled pinned false in DI + after FIX logon
  FEATURE_COPY_TRADING_ENABLED = false
  FeatureFlags:LiveCopyEnabled = false
  BaselineScorer.CanPromoteToLive = false

ALLOWED NOW (read-only / diagnostic):
  Achiever + Starwave GroupRequestArray("*") + UserRequestArray (ALL groups / ALL logins)
  Optional FIX QUOTE/TRADE 35=A logon + later 35=H/AF/AN recon when built

FORBIDDEN NOW:
  35=D NewOrderSingle
  35=F / 35=G cancel/replace
  any live OrderQty on *.c-trader.com
  flipping the flag “to try one lot”
```

| Claim | Result | Class |
|---|---|---|
| Must the flag stay false? | **Yes** | architecture + current code |
| Product `35=D` / `(35, "D")` / `MsgType="D"` | **0 hits** | **MISSING** builder |
| `GuardedNewOrderSingle` / persist-before-send | **0 hits** | **MISSING** |
| QuickFIX/n package | **absent** (`Fix.CTrader.csproj` has no QuickFIXn) | **MISSING** initiator |
| Current FIX wire writer | `CTraderFixSession.BuildLogon` **`(35, "A")` only** | diagnostic logon |
| RiskEngine on a send path | **0 production `Evaluate(` callers** (tests only) | **GATE_INCOMPLETE** |
| Venue recon (`35=AF` / `35=AN` / READY_FOR_EXECUTION) | **MISSING**; API stub | **GATE_INCOMPLETE** |
| §68 go-live license | still **0 PASS / 19 FAIL** (A100 / C14 / D42) | one FAIL blocks send |
| §70 live FIX acceptance | still **0 / 14 FAIL** (A101 / D43) | one FAIL blocks send |
| Achiever+Starwave full catalog | **measured 18 groups / 8460 traders** | fetch-all, not send |
| Safe to set flag `true`? | **No** | would not send today, but would lie |

One-line:

```text
flag=false AND no 35=D builder; fetch ALL manager groups/traders; do not arm live copy until risk+recon PASS.
```

Do **not** enable the flag in this task. Do **not** add a NewOrderSingle. Copy-to-cTrader is the destination. **No-loss is the constraint. The constraint wins.**

---

## 1. Why the flag must stay false (law + measured gaps)

### 1.1 Architecture §41 (necessary, not sufficient)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1564–1590:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

That matrix is intentional: connect / prices / request orders-positions / validate connectivity — **without automatically placing new real orders**.

`NewOrderSingle` requires:

```env
REAL_COPY_EXECUTION_ENABLED=true
```

**plus** runtime risk-engine healthy. Session-on is **not** a send license. §56 repeats the same `false` (line 2101). `docs/architecture.md` L20, `README.md` L28, `docs/deployment.md` L82, `docs/ctrader-fix.md` L73 all keep the flag off.

### 1.2 Architecture §42 — recon before READY_FOR_EXECUTION

On TRADE login: **block new executions** → `OrderMassStatusRequest` → `RequestForPositions` → consume reports → compare DB → **only if reconciled** `READY_FOR_EXECUTION`. Never assume the DB is correct after restart.

Current product: **no** `35=AF`, **no** `35=AN`, **no** `StartupReconciliationCoordinator`. `GET /api/reconciliation/status` is a stub that admits this:

```62:69:D:\Prop\apps\api\Program.cs
app.MapGet("/api/reconciliation/status", () => Results.Ok(new
{
    lastReconciliation = DateTimeOffset.UtcNow,
    unknownPositions = 0,
    mismatches = 0,
    orphanFills = 0,
    note = "recon runs only after FIX TRADE logon; NewOrderSingle still off"
}));
```

Those zeros are **not** a clean book. They are an unimplemented page.

### 1.3 Architecture §68 + §70 — one FAIL blocks enablement

§68 (lines 2605–2628): do not enable real copying until **all 19** boxes are true.

§70 (lines 2658–2676): before production live execution, **all 14** items must be true, including:

- 3. Position reports reconcile after restart
- 11. Risk-engine rejection happens **before** FIX send
- 12. Real execution is feature flagged
- 14. Reconciliation blocks execution while inconsistent

Prior live scorecards (A100, C14, D42, A101, D43) remain **0/19** and **0/14** as a **send license**. Later live Manager census and a one-shot `35=A` writer **do not** flip those boxes. This pass re-checked the send-blocking items against current source; they are still FAIL (see §6).

### 1.4 §61 — do not use the real Pepperstone account as the first integration test

A live `35=D` to `live-us-eqx-01.p.c-trader.com` is forbidden until the in-process simulator + recon + ClOrdID uniqueness are proven. There is still no `tests/Fix` lane that can authorize a live OrderQty.

---

## 2. Measured flag surfaces (all false / fail-closed)

| Surface | Value | Bound to a sender? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | C# initializer **`= false`** | owning POCO; **no** `IOptions` bind in DI |
| Architecture §41 / §56 | **`false`** | design law |
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=false` | **not** the worker key (`CTrader:RealCopyExecutionEnabled`) |
| `apps/fix-worker` `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback **`false`** | log + warning only |
| `apps/fix-worker/appsettings*.json` | key **absent** | fail-closed fallback |
| `AddTraderIntelligence` | `LiveRuntimeStatus.RealCopyEnabled = false` | **pinned**; comment forbids arming |
| `CTraderFixLogonHostedService` after TLS | `_runtime.RealCopyEnabled = false` | re-pins even if logon succeeds |
| `GET /api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` | display of the pin |
| same endpoint | `FEATURE_COPY_TRADING_ENABLED = false` | hardcoded |
| `apps/api/appsettings.json` | `FeatureFlags:LiveCopyEnabled: false` | different name |
| `SettingsController` | `LiveCopyEnabled` Redis alias | **unmapped** (`AddControllers` / `MapControllers` absent) |
| `EfDashboardQueries.GetFixSessionsAsync` last arg | literal **`false`** → `ExecutionEnabled` | display |
| `EfDashboardQueries.GetRiskAsync` | `RealCopyEnabled` **`false`** | display |
| `BaselineScorer.CanPromoteToLive` | **`=> false`** | scorer cannot raise LIVE |
| `docker-compose.yml` / launchSettings | flag **absent** | N/A |
| `tests/` `RealCopyExecutionEnabled` | **0 hits** | fixture uses `RealExecutionEnabled=false` on the risk request |

Owning property:

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

DI pin (cannot honor a true flag because there is no sender):

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

Hosted FIX service re-pin after optional logon:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        ...
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        ...
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

API display (reads the pin, not `.env`):

```70:77:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    ...
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
```

Worker: flag only logs. If `true`, it **still** stamps TRADE `Disconnected` and does not send.

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

That “refuse” is English in a log line, **not** a choke function.

**Binding gap (do not greenwash):** ASP.NET will **not** map env `REAL_COPY_EXECUTION_ENABLED` onto `CTrader:RealCopyExecutionEnabled`. Setting the architecture name `true` does not flip the worker `GetValue`. Even if it did, there is no `35=D` builder. **Do not treat this as permission to flip the env.** The required posture is: keep every committed and local config **false**, and do not add a sender.

---

## 3. No `35=D` NewOrderSingle (remeasured this pass)

E034’s “`TcpClient` / `SslStream` = 0” is **stale**. `CTraderFixSession` now opens TLS. That does **not** create a NewOrderSingle.

### 3.1 Literal census (`D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`, `*.cs`)

| Pattern | Hits | Meaning |
|---|---:|---|
| `35=D` / `(35, "D")` / `new(35, "D")` / `MsgType = "D"` | **0** | no outbound NewOrderSingle |
| `GuardedNewOrder` / `SubmitNewOrder` / `MaySendNewOrder` | **0** | A101 choke **MISSING** |
| `QuickFix` / `QuickFIX` / `SocketInitiator` / `SendToTarget` | **0** | official initiator **MISSING** |
| `OrderSend` / `DealerSend` in Prop C# | **0** | no MT5 venue send either |
| `NewOrderSingle` token | comments / logs / helper **name** only | not a sender |

`NewOrderSingle` product hits this pass (none encode tag 35=`D`):

| File:line | Kind |
|---|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs:33` | XML comment |
| `src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs:70` | info log “still disabled” |
| `src/Infrastructure/DependencyInjection.cs:40` | comment forbidding arming |
| `src/Infrastructure/Seeding/BrokerCatalogSeed.cs:105` | TRADE `LastError` “logon/recon only; NewOrderSingle off” |
| `src/Infrastructure/Seeding/DemoSeeder.cs:101` | TRADE `LastError` (not on API startup path) |
| `src/Application/Runtime/LiveRuntimeStatus.cs:44` | copyNote when flag false |
| `src/Domain/Execution/ExecutionOrderStateMachine.cs:35` | `MayRetryNewOrderSingle` status math |
| `apps/api/Program.cs:68` | recon stub note |
| `apps/fix-worker/Worker.cs:22,41,46` | log / LastError / warning |
| `apps/web/src/pages/LiveCopyPage.tsx:5` | UI: gates still required |
| `apps/web/src/pages/ShadowPortfolioPage.tsx:7` | UI: live NOS disabled |
| `tests/Unit/ExecutionAndSizingTests.cs:14` | asserts retry helper false after send-attempt **state** |

### 3.2 Every product tag-35 builder

| Builder | Tag 35 | Written to venue? |
|---|---|---|
| `CTraderFixSession.BuildLogon` | **`A`** | **Yes** (TLS 5211/5212) if password present |
| `CTraderQuoteService.BuildSecurityListRequestTags` | `y` | **No** (tag list only; unused by hosted service) |
| `CTraderQuoteService.BuildMarketDataRequestTags` | `V` | **No** |
| `FixSimulationHarness` Logon / Reject / ER / MD | `A` / `3` / `8` / `V` / `y` / `X` / `0` | **No** (in-process `|` strings) |

Logon body (the only live outbound FIX this process can emit):

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
```

Missing (required before any honest live copy): `D` NewOrderSingle, `F` cancel, `G` replace, `H` status, `AF` mass status, `AN` positions. Order body tags `(38` OrderQty / `(40` OrdType / `(54` Side: **0** construction hits for a live send.

`Fix.CTrader.csproj` references Hosting/Configuration/Logging/EF abstractions only. **No** `QuickFIXn.Core` / `QuickFIXn.FIX44`.

### 3.3 Hosts that look like senders (they are not)

| Host | What it does | `35=D`? |
|---|---|---|
| `CTraderFixLogonHostedService` | one-shot TLS `35=A` QUOTE 5211 + TRADE 5212; forces `RealCopyEnabled=false` | **No** |
| `apps/fix-worker/Worker` | 15 s loop stamps `Disconnected` | **No** |
| `apps/mt5-worker/Worker` | ingest/score; logs “Execution copy is not performed here.” | **No** |
| `LiveIngestHostedService` | catalog + deals + score for **both** brokers | **No** |
| `ShadowCopyEngine` | in-process `SimulateEntry` / `SimulateExit` | **No** venue |
| `RiskEngine` | may set `AllowFixSend`; **zero** production callers | **No** |
| `ClOrdIdFactory` / `ExecutionOrderStateMachine` | id + status math | **No** |
| `FixSessionOwnership.ExecutionIntentsAllowed` | in-memory fence; unused by worker | **No** |
| `apps/api` `/api/ops/resync` | Manager catalog + score | **No** order endpoint |
| `NativeMt5BrokerConnector` | `GroupRequestArray` / `UserRequestArray` / `DealRequest*` / `PositionRequest*` | **read-only**; `PositionCreateArray` is a **read buffer** |

`MayRetryNewOrderSingle` is **not** a retry sender:

```35:36:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;
```

Unknown-ack after a future send is **not** retryable (`AfterSendAttempt` → `SentAcknowledgementUnknown`; `MayRetry` false). That is correct math with **no socket**.

---

## 4. Risk + recon gates are **not** PASS (so send stays off)

### 4.1 Conjunction required before any future `35=D`

Copied from architecture + A25 / A101 (necessary **and** not sufficient individually):

```text
TRADE LoggedOn
AND lease owned (FixSessionOwnership)
AND recon clean → READY_FOR_EXECUTION   (§42 / §70.3 / §70.14)
AND RiskEngine.Evaluate on the send path
AND decision.AllowFixSend == true
AND REAL_COPY_EXECUTION_ENABLED == true (explicit review)
AND §68 19/19 PASS
AND §70 14/14 PASS
AND persist unique ClOrdID BEFORE write
AND quote fresh / venue healthy / kill-switch None
AND quantity conversion verified
```

**Zero** of the send-path items are wired. Flipping the flag cannot complete this conjunction.

### 4.2 RiskEngine — vocabulary exists; not on a send path

`AllowFixSend` is computed **only if** the caller already passed `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Unreconciled increasing exposure is rejected (`VENUE_NOT_RECONCILED`). Flag-false still **Approves** for shadow evaluation but `AllowFixSend` stays false (unit `Real_flag_false_never_allows_fix_send`).

Production `Evaluate(` call sites under `src/` + `apps/`: **0**. Hits are `RiskEngine.cs` itself and `tests/Unit/RiskEngineTests.cs` only.

The empty `if (request.RealExecutionEnabled == false …)` block (L90–93) is a comment site, not a hard reject. Do not call that a coded refuse-on-LoggedOn gate.

`docs/risk.md` says every request is evaluated **before** submission to cTrader. That document is **policy**, not a wired choke.

### 4.3 Recon — design only

A47 (`A47_reconciliation_design.md`): `StartupReconciliationCoordinator` **MISSING**; `35=AF` / `35=AN` **MISSING**; `READY_FOR_EXECUTION` gate **MISSING**. Current `FixSessionOwnership.MarkReconciled()` is an in-memory setter with **no** caller on the FIX host.

Broker seed TRADE row:

```104:106:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
                    TargetSubId = "TRADE",
                    LastError = "session up for logon/recon only; NewOrderSingle off",
                    UpdatedAt = now
```

### 4.4 Scorer cannot invent LIVE copy

```211:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static bool CanPromoteToLive(TraderState current) => false;
```

Trade #3 is `EARLY_SCORE` / `WATCH` / `SHADOW` / `RISK_BLOCKED`, never a send license. Architecture: scorer never reads `REAL_COPY_EXECUTION_ENABLED` to raise LIVE.

### 4.5 §70 remesure (this pass — live-copy license still 0/14)

| # | Item | Current evidence | Status |
|---|---|---|---|
| 1 | TRADE FIX Logon stable | one-shot `35=A` exists; no heartbeat loop; not a stability proof | **FAIL** |
| 2 | ExecutionReports persisted | no inbound `35=8` applier | **FAIL** |
| 3 | Positions reconcile after restart | no `35=AF/AN`; API stub zeros | **FAIL** |
| 4 | Unique ClOrdID proven | factory + index only; no persist-before-send | **FAIL** |
| 5 | Duplicate ER handling | harness / missing | **FAIL** |
| 6 | Unknown-state recovery | FSM helper only | **FAIL** |
| 7 | Partial fills | enum map only | **FAIL** |
| 8 | Order rejects | enum map only | **FAIL** |
| 9 | Cancel/replace | no `35=F/G` | **FAIL** |
| 10 | Destination position mapping | missing | **FAIL** |
| 11 | Risk rejection **before** FIX send | `Evaluate` unused by workers | **FAIL** |
| 12 | Real execution feature-flagged | default false; **GATE_INCOMPLETE** (no refuse-on-LoggedOn test) | **FAIL** |
| 13 | Global stop-new-orders | risk branch; no send hook / API | **FAIL** |
| 14 | Recon blocks while inconsistent | risk bit only; no venue recon | **FAIL** |

§68 remesure: still **0/19 as a send license**. G01 ingestion is **better than A100’s FakeMt5 quote** (live Manager path exists) but is **not** “stable” (in-memory DB when `DATABASE_URL` is a placeholder; one-shot hosted ingest; no restart-safe live subscription proof). Vacuous / demo law still holds: unused methods and a default-false flag cannot become PASS.

---

## 5. Fetch ALL Achiever + Starwave groups and ALL manager traders (allowed)

This is the **source** path. It is read-only. It does **not** authorize destination send.

### 5.1 Registration

`LiveMt5Registration.CreateConnectors` builds **both** `NativeMt5BrokerConnector`s (Achiever via whitelist HTTP proxy; Starwave direct). Dummy/fake data is refused if real passwords are missing (`HasRealPasswords`).

`LiveIngestHostedService` walks `registry.All()` — both brokers — `SyncCatalogAsync` then deals/score. `GET /api/ops/resync` repeats `ACHIEVER` + `STARWAVEFX`.

`DealIngestionService.SyncCatalogAsync`: `GetGroupsAsync` + `GetAccountsAsync(null)` then batch upsert. **No** `Take(N)` in Application ingestion or `src/Mt5`.

Dashboard `GetTradersAsync` / `GetGroupsAsync` enumerate **all** persisted rows (filter by broker/state only). Remaining `Take(200)` is `GET /api/trades` reconstructed explorer window — **not** a Manager login cap.

### 5.2 Native Manager calls (read-only)

```155:155:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.GroupRequestArray("*", arr);
```

Empty-array fallback: `GroupTotal` / `GroupNext`. Accounts: for each group, `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`. Positions: `PositionRequest` / `PositionRequestByGroup("*")` — **`PositionCreateArray` is a read buffer**, not an order.

Connect pump: `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS`. No dealer send.

### 5.3 Measured live census (do not invent)

`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc` 2026-08-18T08:42:16Z, probe `LiveBrokerProbe`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK via HTTP proxy | **8** | **6512** | 1506 |
| STARWAVEFX | OK direct | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | 1984 |

Achiever groups (this manager’s permission set): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups: `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

Honesty: these are **all groups these two manager logins can see**. Groups outside permission are invisible. `CREDENTIALS_AND_COPY_STATUS.md` records dashboard `/api/traders` **8460** and `/api/groups` **18**. Local API was **not** re-probed this pass (loopback fetch blocked). Treat the JSON + CREDENTIALS pin as the measured census, not a live re-GET.

`mt5-worker/Worker.cs` still scores demo logins `10001–10003/99001` in its loop. That is a **leftover**, not the API ingest host. Do not treat that worker as the live catalog authority.

---

## 6. Copy to cTrader must not send live orders yet (no loss)

User goal is **both** destination copy **and** no capital loss. Those cannot be delivered together **today**:

1. Copy requires a `35=D` with a real OrderQty.
2. No-loss requires §68 + §70 + risk-before-send + recon-before-READY + persist-before-send.
3. (2) is **not PASS**. Therefore (1) stays **off**.

Honest operating mode (A003, reconfirmed):

```text
ALLOW:  Manager fetch-all
        TLS 35=A diagnostic logon
        later TRADE read/recon (35=H/AF/AN) when built
        SHADOW / CopyIntent persistence
FORBID: 35=D / F / G
        REAL_COPY_EXECUTION_ENABLED=true
        any live OrderQty
```

UI already states the same gate list:

```5:5:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      <p className="text-amber-300 text-sm">Copy intents may be recorded as SHADOW only. Pepperstone/cTrader NewOrderSingle is disabled so this process cannot open a losing live position. Gates still required: FIX TRADE logon + recon + risk approve + REAL_COPY_EXECUTION_ENABLED.</p>
```

`LiveRuntimeStatus.Snapshot().copyNote` when false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

### 6.1 What “no loss” does **not** mean

- It does **not** mean live copy is implemented and hedged.
- It does **not** mean §70.12 is a tested refuse-on-LoggedOn-TRADE (there is no `GuardedNewOrderSingle`).
- It **does** mean this process cannot currently open a Pepperstone losing position via FIX, because **no function emits `MsgType=D`**.
- FIX `35=A` can still create a session; that is **not** exposure.

---

## 7. YoPips C++ PropFirm backend (out of this send path)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` is the **challenge / web-terminal** backend.

| Finding | Meaning |
|---|---|
| **0** hits for `35=D`, `NewOrderSingle`, `c-trader`, `pepperstone`, `REAL_COPY` | **not** the cTrader copy adapter |
| `copy_trade_clusters` / `COPY_TRADING_RESTRICTION` | **detects** copy-trading as a prop-firm rule violation |
| `MT5Manager::SendTrade` / `TradeExecutionService` | owner-scoped **challenge-account** MT5 dealer send (terminal), **not** Pepperstone FIX |

That `SendTrade` path is a **different product and a different capital book**. It is **not** a reason to flip `REAL_COPY_EXECUTION_ENABLED`. It is also **not** invoked by `D:\Prop` FIX/copy workers. Slot 28 does not authorize changing it.

---

## 8. Classification

| Slice | Class |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` must stay false | **CONFIRMED** |
| C# / env / API / DI / post-logon pins | **all false** (`EXISTS_AND_GOOD` vs §41) |
| Architecture env name bound onto worker POCO | **NOT WIRED** |
| Product `35=D` builder | **MISSING** |
| Live FIX `35=A` | **EXISTS** (diagnostic; not a send license) |
| Risk-before-send | **GATE_INCOMPLETE** |
| Recon-before-READY | **MISSING** |
| Live send if process starts now | **`SAFE_BY_ABSENCE`** |
| §68 / §70 live-copy license | **0/19** and **0/14** — still FAIL |
| Fetch-all Manager groups/traders | **ALLOWED** (measured 18 / 8460) |
| Enable flag / add `35=D` in this slot | **No** |
| Product source edited | **No** |

---

## 9. Assigned answers (do not paraphrase away)

1. **Must `REAL_COPY_EXECUTION_ENABLED` stay false?**  
   **Yes.** Architecture §41/§56/§68/§70, every product default, DI pin, post-logon re-pin, settings API, and `.env` L73 are false. Risk and recon gates are not PASS. Enabling the flag would be a lie (no sender) and a future landmine.

2. **May this process send `35=D` NewOrderSingle now?**  
   **No.** Zero builders. The only live FIX write is `35=A` Logon. Worker/API/shadow/risk/scorer cannot emit OrderQty.

3. **When may `35=D` exist?**  
   Only after **risk + recon gates** are measured PASS **and** §68 19/19 **and** §70 14/14 **and** an explicit reviewed `true` **and** a `GuardedNewOrderSingle` that refuses unless the full conjunction holds. Not before.

4. **May we fetch ALL Achiever + Starwave groups and ALL manager traders?**  
   **Yes — that is the job.** `GroupRequestArray("*")` + per-group `UserRequestArray`. Measured **8+10=18 groups, 6512+1948=8460 logins**. Fetch is read-only.

5. **Copy to cTrader without loss, today?**  
   **Copy send: off. Loss from this process’s `35=D`: none (`SAFE_BY_ABSENCE`).** Do not confuse that with a finished no-loss live copier.

**Do not enable the flag. Do not add a sender in this task.**

---

## 10. Residual risk (honest)

| Residual | Severity | Note |
|---|---|---|
| Someone adds `35=D` before gates | **High if done** | this report forbids it |
| Someone sets flag `true` in `.env` | Low **today** (no builder; DI pin) | still forbidden; would desync UI/law |
| Treat FIX LoggedOn as send license | Process | Session-on ≠ READY_FOR_EXECUTION |
| Treat 8460-trader census as copy-live | Process | catalog ≠ destination exposure |
| In-memory DB + placeholder `DATABASE_URL` | Ops | restart re-fetches; not a send risk |
| Unmapped `SettingsController` PUT `LiveCopyEnabled` | None to FIX | Redis alias, no sender reads it |
| YoPips C++ `SendTrade` | Separate book | challenge terminal, not this flag |

*End of W500_RESEARCH_28. Product source was not modified.*
