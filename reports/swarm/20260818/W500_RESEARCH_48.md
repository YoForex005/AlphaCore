# W500_RESEARCH_48 — `REAL_COPY_EXECUTION_ENABLED` must stay **false**

| Field | Value |
|---|---|
| Slot | **48** |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_48.md` |
| Date | 2026-08-18 |
| Assigned | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secret values printed | **None.** Flag booleans and secret *names* only. No MT5 / FIX / proxy / DB passwords. Account id `1369850` is already in committed seed/options, not a password. |
| C++ tree consulted | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` — **not** the Pepperstone/cTrader sender. |
| Method | Independent re-read + product-tree grep of `D:\Prop` (`src/`, `apps/`, `tests/`, docs, architecture, local `.env` flag lines) and YoPips C++ `src/`. No TLS opened. No Manager re-attach. No `35=D` sent. Local `127.0.0.1` HTTP not re-hit (fetch blocked). |

**Honesty rule:** wanting both “copy to cTrader” **and** “no loss” does not make live `35=D` legal. A TLS Logon (`35=A`) is not a NewOrderSingle. A log line that *names* NewOrderSingle is not a builder. `AllowFixSend` on a risk DTO is not a socket write. `SAFE_BY_ABSENCE` is the current no-loss outcome, **not** a passed Architecture §68 / §70 review. Do **not** flip the flag to “try one lot.”

Sibling slot 8 (`W500_RESEARCH_8.md`) reached the same law. This file is a **slot-48 remeasure** of the live product bytes, not a copy of that report.

---

## 0. Verdict (binding)

**CONFIRMED: `REAL_COPY_EXECUTION_ENABLED` must stay `false`. No live `35=D` until risk + recon gates are measured PASS.**

| Claim | Measured result | Class |
|---|---|---|
| Flag default / runtime pin is **false** | **Yes** — every product surface is `false` or fallback `false`; local `.env` L73 is `false`; **0** product `=true` | `EXISTS_AND_GOOD` vs architecture §41 |
| Product can emit FIX `35=D` today | **No** — zero builders in `src/` + `apps/` + `tests/` `*.cs` | **`SAFE_BY_ABSENCE`** |
| Official QuickFIX/n initiator | **Absent** from `TraderIntelligence.Fix.CTrader.csproj` (Hosting/Config/Logging/EF only) | no live initiator |
| Risk engine is a wired send choke | **No** — `RiskEngine.Evaluate` exists; **not** registered in `AddTraderIntelligence`; only `tests/Unit/RiskEngineTests.cs` constructs it | `GATE_INCOMPLETE` |
| Venue recon is a wired send choke | **No** — `GET /api/reconciliation/status` is a **stub** of zeros; no `35=H` / `35=AF` / `35=AN` on the wire | `MISSING` |
| §68 go-live (19 items) | **0 / 19 PASS** (A100 / C14 / D42) | one FAIL blocks send |
| §70 live FIX (14 items) | **0 / 14 PASS** (A101 / D43) | one FAIL blocks send |
| Fetch ALL Achiever + Starwave groups/traders | **Allowed and implemented** as Manager **read** (`GroupRequestArray("*")` + `UserRequestArray`); last measured census **18 / 8460** | read-only |
| Copy to Pepperstone/cTrader live orders | **Forbidden today** | flag **must stay false** |
| Flipping the flag would place an order **today** | **No** (still no builder) | still **do not flip** — next engineer who adds a sender must find the flag off |
| Safe to enable `REAL_COPY_EXECUTION_ENABLED=true` | **No** | residual capital risk = Pepperstone TRADE account if a sender is added later |
| YoPips C++ is a substitute copy executor | **No** — `SendTrade` is challenge-terminal MT5, not cTrader FIX | do not hijack |

One-line:

```text
REAL_COPY_EXECUTION_ENABLED=false
AND no function emits 35=D
AND risk/recon are not wired send gates
THEREFORE live copy stays OFF (no loss)
AND Manager catalog fetch of ALL groups/traders is the only live I/O allowed.
```

---

## 1. Goal split (do not collapse)

The user goal is two independent jobs:

| Job | Live I/O allowed? | Capital at risk? |
|---|---|---|
| **A.** Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders | **Yes** — native Manager **read** (`GroupRequestArray` / `UserRequestArray` / `UserLogins`) | **No** (no destination order) |
| **B.** Copy those traders to cTrader | **Not yet** — SHADOW / CopyIntent only | **Would be yes** the moment `35=D` exists |

Job B is **blocked** by architecture §41 + §42 + §68 + §70. Job A does **not** license Job B.

```text
MT5 Manager census (read)     = ALLOWED now
FIX 35=A Logon (QUOTE/TRADE)  = allowed for session proof / future recon
FIX 35=H / AF / AN            = Phase 7 recon (not built)
FIX 35=D NewOrderSingle       = FORBIDDEN until gates + flag
FIX 35=F / G cancel/replace   = FORBIDDEN (same license)
```

---

## 2. Architecture law — flag is necessary and **not** sufficient

Source of truth: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.

### 2.1 §41 — default OFF; send requires flag **plus** healthy risk

```1568:1590:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

This allows connect / prices / order-status / position **reads** / connectivity proof **without** placing new real orders. Actual NewOrderSingle requires `REAL_COPY_EXECUTION_ENABLED=true` **plus** runtime risk-engine healthy. The `=true` token at architecture L1587 is the *future* license, not a committed product value.

§56 example block ends the same way (`L2101`): `REAL_COPY_EXECUTION_ENABLED=false`.

Restated in product docs:

| File | Line | Text |
|---|---|---|
| `D:\Prop\docs\architecture.md` | 20 | `` `REAL_COPY_EXECUTION_ENABLED=false` `` |
| `D:\Prop\docs\ctrader-fix.md` | 73 | flag **false** disables live execution |
| `D:\Prop\docs\deployment.md` | 82 | keep false until cTrader integration is verified |
| `D:\Prop\README.md` | 28 | Real NewOrderSingle is **off** |

### 2.2 §42 — TRADE logon must **block** new executions until recon

After TRADE login: block new executions → OrderMassStatus → positions → compare DB → only then `READY_FOR_EXECUTION`. **None** of those FIX reads exist in product C# (see §4.3).

### 2.3 §68 — 19 boxes, all FAIL for live

```2607:2628:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
Do not enable real copying until all of these are true:
[ ] MT5 historical/live ingestion is stable
...
[ ] cTrader reconciliation works after restart
...
[ ] risk engine unit/integration tests pass
...
[ ] manual review completed
```

Working scorecards (same integer): A100 / C14 / D42 = **0 PASS / 19 FAIL**. One FAIL blocks enablement.

### 2.4 §70 — 14 boxes, all FAIL for live

```2660:2676:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
1. TRADE FIX Logon is stable.
...
11. Risk-engine rejection happens before FIX send.
12. Real execution is feature flagged.
13. Global stop-new-orders works.
14. Reconciliation blocks execution while inconsistent.
```

A101 / D43 = **0 / 14 PASS**. Item 12 is **not** a coded `GuardedNewOrderSingle` refuse path. Vacuous “cannot send because nothing can send” is **not** §70.12 PASS.

Conjunction for the **first** live `35=D`:

```text
19/19 §68
AND 14/14 §70
AND REAL_COPY_EXECUTION_ENABLED=true   (explicit, reviewed — default remains false)
AND RiskEngine.AllowFixSend == true    (Reconciled && VenueHealthy && KillSwitch==None && flag)
AND TRADE READY_FOR_EXECUTION
AND persist unique ClOrdID before send
AND MayRetryNewOrderSingle false on unknown
```

**Today that conjunction is false.** Do not send.

---

## 3. Flag must stay false — measured surfaces (this pass)

| Surface | What it does | Value | Bound to send? |
|---|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | C# initializer; comment “Default OFF” | **`false`** | **is** the POCO; **no caller sends** |
| `AddTraderIntelligence` | pins `LiveRuntimeStatus.RealCopyEnabled` | **`false`** (comment: do not arm a flag that cannot be honored) | runtime pin |
| `CTraderFixLogonHostedService` | after optional TLS logon `35=A` | **`_runtime.RealCopyEnabled = false`** | re-pins **false** even if logon succeeds |
| `apps/fix-worker` `GetValue("CTrader:RealCopyExecutionEnabled", false)` | log + warning only | fallback **`false`**; `appsettings` has **no** `CTrader` block | **not** a sender |
| `GET /api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | reads `runtime.RealCopyEnabled` | **`false`** (pinned) | display |
| `GET /api/settings` `FEATURE_COPY_TRADING_ENABLED` | hardcoded literal | **`false`** | display; **not** §41 |
| `GET /api/health` `realCopyEnabled` | `runtime.RealCopyEnabled` | **`false`** | display |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | different name | **`false`** | **not** the architecture token |
| Unmapped `SettingsController` `LiveCopyEnabled` | Redis `settings:flags:live_copy` | `GetValue(..., false)` | **dead** — `AddControllers` / `MapControllers` absent |
| `EfDashboardQueries.GetOverviewAsync` | 16th field `_runtime.RealCopyEnabled` | **`false`** | display |
| `EfDashboardQueries.GetFixSessionsAsync` | `ExecutionEnabled` | literal **`false`** (L195) | display |
| `EfDashboardQueries.GetRiskAsync` | 7th bool | literal **`false`** (L208) | display |
| `LiveCopyPage.tsx` | static warning | names remaining gates | display |
| Local `.env` (gitignored) | architecture env name | **`false`** (L73) | worker does **not** bind this env name |
| Local `.env` `FEATURE_COPY_TRADING_ENABLED` | extra name | **`false`** (L106) | unused in C# |
| `apps/fix-worker/appsettings.json` | logging only | key **absent** | fallback applies |
| `docker-compose.yml` | api + postgres + redis | key **absent** | N/A |
| `launchSettings.json` (all hosts) | | **0** hits | N/A |
| Product `*.cs` / `*.json` / `*.yml` `=true` for either flag | — | **0 hits** | — |

Owning POCO:

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

Runtime pin (cannot be honored safely):

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

Logon host **re-forces** the pin after QUOTE/TRADE `35=A`:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        ...
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        ...
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

`Snapshot()` copy-note when false (honest):

```42:44:D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs
        copyNote = RealCopyEnabled
            ? "LIVE SEND ARMED — unexpected"
            : "NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.",
```

Settings API (mapped Minimal API — **this** is what operators hit; MVC controller is dead):

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

Worker: even if someone sets `CTrader:RealCopyExecutionEnabled=true`, the loop **stamps `Disconnected`** and **does not send**:

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        ...
                trade.Status = FixSessionStatus.Disconnected;
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

Architecture env name `REAL_COPY_EXECUTION_ENABLED` is **not** bound onto `CTraderFixOptions` by ASP.NET convention (would need `CTrader__RealCopyExecutionEnabled`). Unbound env `true` would still not create a builder. That is **GATE_INCOMPLETE**, not a reason to set it true.

**Why the flag must still stay false (not only “because nothing can send”):**

1. Architecture §41 names it as the **only** live-send license. Turning it on is a production event.
2. The next increment that *does* add `GuardedNewOrderSingle` will read this flag. If it is `true` in `.env` / appsettings, the first builder ships **armed**.
3. `RiskEngine` treats `RealExecutionEnabled=false` as “evaluate but `AllowFixSend=false`” — **it does not `Reject`**. A future sender that checks only `Outcome == Approve` would fire (`Real_flag_false_never_allows_fix_send` asserts Approve + `AllowFixSend==false`). The flag **and** `AllowFixSend` **and** recon **must** all be coded before any builder.
4. §68 is **0/19**. §70 is **0/14**. One FAIL blocks enablement.
5. TRADE logon (if proven) is **Phase 7 read**, not Phase 8 send.

---

## 4. No `35=D` NewOrderSingle — measured grep (this pass)

Product `*.cs` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`:

| Pattern | Hits that emit wire `MsgType=D` |
|---|---|
| `35=D` | **0** |
| `(35, "D")` / `new(35, "D")` | **0** |
| `MsgType = "D"` | **0** |
| `OrderQty` / persist-before-send `GuardedNewOrderSingle` | **0** in `Fix.CTrader` |
| `QuickFix` / `QuickFIXn` / `SendToTarget` | **0** in product `*.cs` / `*.csproj` |
| `SendTrade` / `OrderSend` / `DealerSend` in `src/Mt5` | **0** |

`NewOrderSingle` token in product `*.cs` is **name / comment / log / LastError only**:

| File | Kind | Emits `35=D`? |
|---|---|---|
| `ExecutionOrderStateMachine.MayRetryNewOrderSingle` | status predicate | **No** |
| `CTraderFixOptions` XML comment | comment | **No** |
| `CTraderFixLogonHostedService` log | “still disabled” | **No** |
| `LiveRuntimeStatus.Snapshot` copyNote | string | **No** |
| `BrokerCatalogSeed` TRADE `LastError` | “NewOrderSingle off” | **No** |
| `DemoSeeder` TRADE `LastError` | string (not on API startup) | **No** |
| `apps/fix-worker/Worker.cs` | log + LastError | **No** |
| `apps/api/Program.cs` recon note | string | **No** |
| `DependencyInjection` comment | comment | **No** |
| `tests/Unit/ExecutionAndSizingTests.cs` | asserts retry helper false | **No** |

Product outbound tag **35** values found under `D:\Prop\src` `*.cs`:

| Encoding | File | MsgType | Wire send? |
|---|---|---|---|
| `(35, "A")` | `CTraderFixSession.BuildLogon` L96 | Logon | **Yes** — one-shot TLS write, then read, then dispose |
| `new(35, "y")` | `CTraderQuoteService.BuildSecurityListRequestTags` | SecurityListRequest | **No** caller writes it |
| `new(35, "V")` | `CTraderQuoteService.BuildMarketDataRequestTags` | MarketDataRequest | **No** caller writes it |
| harness `(35, "A"\|"3"\|"0"\|"y"\|"X"\|"8")` | `FixSimulationHarness` | test doubles | **in-process only** |

**Zero** `D`. `CTraderFixSession` public surface is **only** `TryLogonAsync` → TCP+TLS → `BuildLogon` (`35=A` + 553 username + 554 password) → one 4 KiB read → classify. No `OrderQty`, no `ClOrdID` persist-before-send, no heartbeat loop, no `35=D/F/G/H/AF/AN`.

```89:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        ...
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(...)),
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
        return Assemble(fields);
    }
```

`seq` is the literal `1`. Sockets are `using` / `await using` and **dispose before return**. A later copy intent has **no TRADE socket** to hang a `35=D` on.

`MayRetryNewOrderSingle` is **false** after send-attempt (unknown ack) — unit-tested, **no socket**:

```17:40:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static ExecutionOrderStatus AfterSendAttempt() =>
        ExecutionOrderStatus.SentAcknowledgementUnknown;
    ...
    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;

    public static bool RequiresReconciliation(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.SentAcknowledgementUnknown
            or ExecutionOrderStatus.ExecutionStateUnknown;
```

Official QuickFIX/n initiator is **not** the live send path. `TraderIntelligence.Fix.CTrader.csproj` references Domain + Application + Hosting/Config/Logging/EF abstractions only. Safety is **absence**, not a tested `GuardedNewOrderSingle` refuse-on-LoggedOn-TRADE.

---

## 5. Risk / recon gates are **not** PASS — therefore no `35=D`

### 5.1 RiskEngine — exists, unused on the live path

`Evaluate` **does** refuse increasing exposure when `Reconciled == false` (`VENUE_NOT_RECONCILED`) and sets `AllowFixSend` only when flag + kill + recon + venue are all true:

```84:85:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (!request.Reconciled && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "VENUE_NOT_RECONCILED");
```

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

When `RealExecutionEnabled == false` the engine **does not Reject** (empty shadow branch L91–93). Unit fixture `Base()` sets `RealExecutionEnabled = false` and `Real_flag_false_never_allows_fix_send` asserts `Outcome == Approve` **and** `AllowFixSend == false`.

Measured wiring:

| Check | Result |
|---|---|
| `AddTraderIntelligence` registers `RiskEngine` | **No** (`DependencyInjection.cs` L49–57: store, dashboard, reconstructor, scorer, ingest, logon — **no** risk) |
| Product `new RiskEngine` / `AddSingleton<RiskEngine>` | **0 hits** |
| Only constructor | `tests/Unit/RiskEngineTests.cs` → `private readonly RiskEngine _e = new();` |
| Worker / API / FIX session calls `Evaluate` | **No** |
| `GET /api/risk` | painted zeros + `RealCopyEnabled=false` (`EfDashboardQueries` L208) |

So risk **cannot** be a send gate until it is wired **in front of** a sender that does not yet exist.

### 5.2 Recon — stub, not a venue gate

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

No product `35=H` / `35=AF` / `35=AN`. `FixSessionOwnership.MarkReconciled()` is in-memory fence math, not cServer positions. Seeded TRADE `LastError` is “session up for logon/recon only; NewOrderSingle off” (`BrokerCatalogSeed` L105).

**A fake zero-mismatch JSON is anti-evidence.** It must not be treated as §70.14 PASS.

### 5.3 Scoring must not promote to LIVE send

`BaselineScorer.CanPromoteToLive` is **hard-false**. Trade #3 is early evidence → SHADOW, never LIVE execution:

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`ShadowCopyEngine` is in-memory bid/ask math (`SimulateEntry` / `SimulateExit`). It never opens a socket.

### 5.4 Quantity conversion is not a send path

`tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` L176: `"QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine"`. Lots are **not** a FIX `OrderQty` today.

---

## 6. Fetch ALL Achiever + Starwave groups / traders (read-only) — allowed

This is **Job A**. It is **not** copy execution. Copy flags are **not** consulted on this path.

### 6.1 Two live connectors; dummy path off

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector`s: Achiever (HTTP proxy optional) + StarwaveFX (direct). Missing real passwords throw:

```35:36:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

```23:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(... BrokerCodes.Achiever ...);
        var starwave = new NativeMt5BrokerConnector(... BrokerCodes.StarwaveFx ...);
        return new IMt5BrokerConnector[] { achiever, starwave };
```

`DemoSeeder` is **not** on API startup. Startup only `EnsureCreated` + `BrokerCatalogSeed` (two broker rows, one instrument, kill switch, two FIX session rows stamped **Disconnected** / NewOrderSingle off).

`LiveIngestHostedService` loops `registry.All()`, `SyncCatalogAsync`, then deals/score. Failures do **not** substitute FakeMt5 (`"No dummy data will be substituted."`). **Zero** references to `REAL_COPY_*` / `FEATURE_COPY_*`.

`POST /api/ops/resync` hard-loops `new[] { "ACHIEVER", "STARWAVEFX" }` (`Program.cs` L121). Still **no** `35=D`.

`apps/mt5-worker/Worker.cs` L19: `"Execution copy is not performed here."` It syncs both brokers. The leftover `10001/10002/10003/99001` scoring loop is a **demo-login leftover** and is **not** a Manager fetch cap.

### 6.2 Catalog = all groups the manager can see + all users in those groups

```155:155:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.GroupRequestArray("*", arr);
```

Fallback if the array is empty: `GroupTotal()` + `GroupNext`. `AddGroup` skips only blank/duplicate names — **no** `MT5_GROUP_*` / plan-path filter.

`GetAccountsCore(null)` walks **every** group, then `UserRequestArray` → fallback `UserGetByGroup` → fallback `UserLogins` + `UserRequestByLogins`. **No `Take(200)`** on this path.

`DealIngestionService.SyncCatalogAsync` upserts **all** groups + **all** accounts (`GetAccountsAsync(null)`):

```37:50:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Dashboard `GetTradersAsync` walks **all** `Mt5Accounts` (no Take). The only remaining `Take(200)` is `GET /api/trades` reconstructed-row explorer — **not** a Manager login cap. `GetRiskAsync` `Take(20)` is reject-reason preview only.

### 6.3 Last measured live census (this slot did **not** re-attach)

From `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` + `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (2026-08-18):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via whitelist HTTP proxy | **8** | **6512** | 1506 |
| StarwaveFX | OK direct | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups the manager can see: `contest\yo-1step|2step|instant|payp`, `demo\yo-1step|2step|instant|payp`.  
Starwave groups: `Starwave\cent\FX1\grp1|grp2`, `demo\FX2\grp1|grp2`, `real\FX3\grp1–5` + `LP`.

If the server has more groups, they are **outside this manager permission set**. That is completeness vs the login, not vs the whole broker.

Full login dump (no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`.

This census is **Job A**. It does **not** move §68 or authorize `35=D`.

---

## 7. C++ YoPips PropFirm backend — not a copy-to-cTrader sender

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm`.

| Search (this pass) | Result |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **0** |
| `NewOrderSingle` / `35=D` / `FIX.4` / `cServer` as FIX TargetCompID | **0** in product send path |
| What *does* send | `TradeExecutionService::placeOrder` → `dispatchSendTrade` → MT5 **`SendTrade`** for **challenge terminal** HTTP (`TradeExecutionController::placeOrder`) |
| Groups | challenge/phase/size → `mt5_group_mappings` (prop-account **assignment**), not Achiever/Starwave census |
| `copy_trade_*` tables | **detection / admin restriction** (`copy_trade_clusters`, `copy_trade_alerts`), not a cTrader FIX sender |

The C++ service is a **prop-firm dealer** for customer challenge accounts. It is **not** the Pepperstone/cTrader hedge path.

**Do not** use `SendTrade` as a substitute copy executor. That would put **challenge-account** capital at risk and still would not be cTrader `35=D`. Slot-48 “no loss” means: Prop C# must not send `35=D`, **and** the C++ dealer must not be hijacked to copy Achiever/Starwave managers onto live books.

---

## 8. Residual risk to capital (honest)

| Path | Can it lose money **today** if this process starts? |
|---|---|
| Prop C# → cTrader `35=D` | **No** — `SAFE_BY_ABSENCE` + flag false + runtime pin false |
| Prop C# → MT5 `SendTrade` | **No** — Manager API is **read** (groups/users/deals/positions) |
| FIX `35=A` logon | Session only. Not an order. Password never logged in this report. |
| Fake recon zeros / dashboard `TradeHealthy` if logon true | **Honesty risk**, not a fill |
| C++ `TradeExecutionService` (separate process) | **Yes, if that API is called** — different product, different venue (MT5 challenge). Out of this copy pipeline. |
| Flag flipped `true` **after** a future sender lands | **Yes** — Pepperstone TRADE account. This is why the flag **must stay false now**. |

`LiveCopyPage` states the operating constraint correctly:

> Copy intents may be recorded as SHADOW only. Pepperstone/cTrader NewOrderSingle is disabled so this process cannot open a losing live position. Gates still required: FIX TRADE logon + recon + risk approve + REAL_COPY_EXECUTION_ENABLED.

---

## 9. What this slot authorizes / forbids

**Authorize (Job A):**

- Keep fetching **all** Achiever + Starwave groups the two manager logins can see.
- Keep upserting **all** manager traders (no `Take(200)` on catalog).
- Dashboard `/api/groups` + `/api/traders` as a catalog (SHADOW / scores).
- Optional FIX `35=A` logon proof. Re-pin `RealCopyEnabled=false` after logon.

**Forbid (Job B — until gates):**

- `REAL_COPY_EXECUTION_ENABLED=true` in `.env`, appsettings, Docker, launchSettings, or runtime.
- Any `35=D` / `F` / `G` builder or QuickFIX `SendToTarget` of those types.
- Using C++ `SendTrade` to “copy” manager traders.
- Treating §68 0/19 or §70 0/14, or a green `dotnet test`, as a send license.
- Treating `FEATURE_COPY_TRADING_ENABLED` or `FeatureFlags:LiveCopyEnabled` as the §41 send license.
- Blind retry of unknown-ack as a second `35=D` (`MayRetryNewOrderSingle` is already false; keep it that way).

**Copy to cTrader is the destination. No-loss is the constraint. The constraint wins.**

---

## 10. What this file does **not** prove

- Does **not** re-prove live FIX Logon on the wire (no capture; local HTTP not re-hit).
- Does **not** re-prove Manager census still 18/8460 (that is `LIVE_MANAGER_FETCH_MEASURED.md`).
- Does **not** implement a refuse-on-LoggedOn-TRADE unit test — refuse is structural (no builder).
- Does **not** tick Architecture §68 / §69 / §70.

---

## 11. Cross-checks (siblings; not this file)

| Artifact | Use |
|---|---|
| `W500_RESEARCH_8.md` | same law, earlier slot |
| `W500_RESEARCH_10.md` | `CTraderFixSession` 35=A only |
| `W500_RESEARCH_17.md` | both flag names default false |
| `LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups / 6512+1948 traders |
| `E002_no_live_send.md` | flag false + no sender |
| `E034` / A003 | product `35=D` = 0 |
| `A009_arch_gates.md` / `A100` / `C14` / `D42` | §68 0/19 |
| `A101_live_fix_acceptance.md` / `D43` | §70 0/14 |
| `D69_flag.md` | POCO default false |
| `CREDENTIALS_AND_COPY_STATUS.md` | flag forced false; live `35=D` method does not exist |

---

## 12. Files read (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\Controllers\SettingsController.cs` (unmapped)
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (§41, §42, §56, §68, §70)
- `D:\Prop\docs\architecture.md`, `docs\ctrader-fix.md`, `docs\deployment.md`, `README.md`
- `D:\Prop\docker-compose.yml`
- `D:\Prop\.env` (flag lines only: L73, L106)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- YoPips: `src\services\trade_execution_service.*`, `src\core\mt5_manager.cpp` `SendTrade`, `src\http\controllers\trade_execution_controller.cpp`

---

## 13. Slot-48 close

| Item | Value |
|---|---|
| Slot | 48 |
| Verdict | **MUST_STAY_FALSE** — no `35=D` until risk + recon gates PASS |
| Evidence | this file + cited product paths |
| Risk to capital (Prop copy path **today**) | **None** (`SAFE_BY_ABSENCE` + pinned false) |
| Residual | **High if flag is armed before a sender + gates exist** |

```text
KEEP: REAL_COPY_EXECUTION_ENABLED=false
KEEP: no NewOrderSingle builder
KEEP: Manager ALL-groups / ALL-traders read
BLOCK: live 35=D
```
