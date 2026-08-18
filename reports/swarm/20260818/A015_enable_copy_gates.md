# A015 — Enable copy gates (what “enable it” actually means)

| Field | Value |
|---|---|
| Agent | A015 |
| Date | 2026-08-18 |
| Role | Senior engineer, copy-to-cTrader enablement gates |
| Assigned | User said **“enable it”** meaning `REAL_COPY` / copy-to-cTrader. Read `RiskEngine`, `BaselineScorer` (`CanPromoteToLive=false`), `CTraderFixSession` (no `35=D`), `LiveRuntimeStatus`, `.env` **flag names only**. |
| Product source | **Not modified** by this agent. Report only. |
| Secrets | **None** in this file. Env **keys** listed; values never copied. |

**Honesty rule:** wanting live copy does not create a NewOrderSingle. An env bit named `REAL_COPY_*` is **not** a send license. `AllowFixSend` on a DTO is **not** a socket write. A one-shot `35=A` logon is **not** a persistent TRADE session. Promoting `SHADOW` to `LIVE` automatically is **FORBIDDEN**.

---

## Verdict — the ONLY safe enablement

**Enable copy as SHADOW.** Do **not** enable live send.

| Allowed now | Forbidden now |
|---|---|
| `FEATURE_COPY_TRADING_ENABLED=true` | Treating env `REAL_COPY_EXECUTION_ENABLED=true` as a go-live |
| Generate **SHADOW** `CopyIntent` rows (`Status=SHADOW_ONLY`) | Auto-promote `SHADOW` → `LIVE` / `LIVE_CANDIDATE` |
| Call `RiskEngine.Evaluate` on every intent | Set `AllowFixSend=true` |
| Persist `RiskDecision` with `AllowFixSend=false` | Emit FIX `35=D` (NewOrderSingle) |
| Keep FIX QUOTE/TRADE **logon-only** | Bind process `RealCopyEnabled=true` while no LIVE traders, no real recon, no persistent TRADE sender |

`REAL_COPY_EXECUTION_ENABLED` **may already be armed in `.env`**. That is an operator wish. Process law still pins `LiveRuntimeStatus.RealCopyEnabled=false` and `AllowFixSend` must stay **false** until **all three** are true:

1. **LIVE traders exist** (scorer may emit `LIVE` only after an explicit human promote; `CanPromoteToLive` is hardcoded `false`).
2. **Recon is real** (not the `/api/reconciliation/status` stub of zeros).
3. A **persistent TRADE session** exists that can actually send `35=D` (today: **no builder**, logon socket is disposed after `35=A`).

Until then, “enable it” = **feature on + SHADOW intents + risk evaluate**. Capital at risk from this process: **NONE** (`SAFE_BY_ABSENCE` of `35=D`).

---

## 1. What the user asked vs what the code can honor

Copy-to-cTrader is two different products:

| Product | Meaning | Current measured state |
|---|---|---|
| Feature copy | Ingest → reconstruct → score → emit SHADOW intents → evaluate risk → simulate fills | Partial. Shadow path exists; **RiskEngine is not on the persist path**. |
| Real copy | Persist-before-send → `AllowFixSend` → persistent TRADE → `35=D` | **Impossible.** No `35=D` assembler. Runtime pin `RealCopyEnabled=false`. |

Flipping `.env` cannot place an order. Adding a `35=D` builder **before** LIVE + real recon + persist-before-send would be a capital-risk defect.

---

## 2. `.env` flag names only (no values)

Read `D:\Prop\.env` **keys**. Values omitted on purpose.

### Copy / FIX session flags

| Key | Role |
|---|---|
| `FEATURE_COPY_TRADING_ENABLED` | **Safe feature switch.** Turns on SHADOW intent generation + risk evaluation. |
| `REAL_COPY_EXECUTION_ENABLED` | Operator arm for live send. **Not sufficient.** Must not drive `AllowFixSend`. |
| `FEATURE_TRADE_RECONSTRUCTION_ENABLED` | Reconstruction (upstream of copy). |
| `FEATURE_CTRADER_HEDGING_ENABLED` | Adjacent; not a send license. |
| `FEATURE_ML_SCORING_ENABLED` | Adjacent; not a send license. |
| `FEATURE_NEWS_FILTER_ENABLED` | Adjacent; not a send license. |
| `CTRADER_FIX_ENABLED` | Session-on. |
| `CTRADER_FIX_QUOTE_ENABLED` | QUOTE logon allowed. |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | TRADE **logon** allowed. Not NewOrderSingle. |
| `CTRADER_FIX_HOST` | Gateway host name. |
| `CTRADER_FIX_ACCOUNT_ID` | Account id (tag 553). |
| `CTRADER_FIX_PASSWORD` | Secret — **name only**. |
| `CTRADER_FIX_USE_SSL` | TLS. |
| `CTRADER_FIX_QUOTE_SSL_PORT` / `CTRADER_FIX_QUOTE_PLAIN_PORT` | QUOTE ports. |
| `CTRADER_FIX_TRADE_SSL_PORT` / `CTRADER_FIX_TRADE_PLAIN_PORT` | TRADE ports. |
| `CTRADER_FIX_QUOTE_SENDER_COMP_ID` / `CTRADER_FIX_QUOTE_TARGET_COMP_ID` | QUOTE CompIDs. |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` / `CTRADER_FIX_TRADE_TARGET_COMP_ID` | TRADE CompIDs. |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` / `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | QUOTE SubIDs (`QUOTE`). |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` / `CTRADER_FIX_TRADE_TARGET_SUB_ID` | TRADE SubIDs (`TRADE`). |
| `CTRADER_FIX_QUOTE_SESSION_QUALIFIER` / `CTRADER_FIX_TRADE_SESSION_QUALIFIER` | Qualifier labels. |

### Risk-named keys (limits, not send)

`RISK_MAX_DAILY_LOSS_PCT`, `RISK_MAX_TOTAL_LOSS_PCT`, `RISK_MAX_POSITION_SIZE_LOTS`, `RISK_MAX_OPEN_POSITIONS`, `RISK_MAX_DAILY_TRADES`, `RISK_SLIPPAGE_TOLERANCE_POINTS`, `RISK_COPY_MIN_DELAY_MS`, `RISK_COPY_MAX_DELAY_MS`, `RISK_EMERGENCY_FLATTEN_ENABLED`, `RISK_KILL_SWITCH_ENABLED`.

These are **not** bound onto `RiskLimits` today. Domain `RiskEngine` uses its own POCO defaults.

### Wiring honesty

| Observation | Evidence |
|---|---|
| `FEATURE_COPY_TRADING_ENABLED` is **not read** by product C# | `/api/settings` hardcodes `false`. Zero `GetValue("FEATURE_COPY_TRADING_ENABLED")`. |
| `REAL_COPY_EXECUTION_ENABLED` is **not bound** to `CTraderFixOptions` | Binder would need `CTrader__RealCopyExecutionEnabled`. Worker reads `CTrader:RealCopyExecutionEnabled` (nested) **log-only**. |
| Process pin overrides env | `DependencyInjection` constructs `LiveRuntimeStatus { RealCopyEnabled = false }`. `CTraderFixLogonHostedService` sets `_runtime.RealCopyEnabled = false` after logon. |

So `.env` can say REAL_COPY is armed and FEATURE is on; the running API still reports both **off**. That pin is **correct** until the three live-send preconditions exist.

---

## 3. BaselineScorer — promotion to LIVE is forbidden

File: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`

`TraderStateMachine.FromBaseline` ceiling is **SHADOW**:

```189:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;

        if (!earlyEligible)
            return TraderState.INSUFFICIENT_DATA;

        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;

        if (quality >= 55)
            return TraderState.WATCH;

        return TraderState.EARLY_SCORE;
    }

    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

Enum has `LIVE_CANDIDATE` and `LIVE` (`D:\Prop\src\Domain\Enums\TraderState.cs`) but **no scorer path writes them**.

Unit lock: `D:\Prop\tests\Unit\BaselineScorerTests.cs` asserts `CanPromoteToLive(...) == false`.

**Law:** SHADOW is the highest automatic state. Any “enable copy” change that maps SHADOW→LIVE on a quality threshold is a **policy violation**, not a feature.

---

## 4. RiskEngine — `AllowFixSend` is the only send authority (and it stays false)

File: `D:\Prop\src\Domain\Risk\RiskEngine.cs`

Conjunction for a true send bit:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Every reject path hardcodes `AllowFixSend = false`.

When `RealExecutionEnabled == false` the engine **still evaluates** (comment: “Shadow path still evaluates risk but never allows FIX send”). Tests lock this:

```20:26:D:\Prop\tests\Unit\RiskEngineTests.cs
    public void Real_flag_false_never_allows_fix_send()
    {
        var d = _e.Evaluate(Base());
        d.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        d.AllowFixSend.Should().BeFalse();
    }
```

`Base()` sets `RealExecutionEnabled = false`. Approve + no send is the SHADOW contract.

Other fail-closed reasons (increasing actions): `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN_BLOCKS_NEW`, `VENUE_NOT_RECONCILED`, `VENUE_UNHEALTHY`, `QUOTE_MISSING`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `SIGNAL_STALE`, loss/DD/position/qty/XAU/margin/martingale/abnormal sizing.

**Gap (do not paper over):** `PersistDemoShadowAsync` (`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` L251–323) writes `CopyIntent` with `Status = "SHADOW_ONLY"` and a `ShadowCopyEngine` fill. It **does not** construct `RiskEvaluationRequest` or call `RiskEngine.Evaluate`. Safe enablement of FEATURE_COPY must **add** that evaluate step, not skip it because shadow “cannot send anyway.”

`RiskEngine` is **not** registered in `AddTraderIntelligence`. Shadow persist new’s `ShadowCopyEngine` locally. Wiring FEATURE on = register engine + persist `RiskDecisionRecord` with `AllowFixSend=false`.

---

## 5. CTraderFixSession — no `35=D`

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

Only outbound MsgType is **Logon `35=A`**:

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
            (553, username),
            (554, password)
        };
```

Behavior:

- One `TcpClient` + TLS + one `WriteAsync` of logon + one `ReadAsync` of reply.
- `using` disposes the socket. **Not a persistent TRADE session.**
- No Heartbeat loop, no `35=0`/`35=1`, no `35=D`/`35=F`/`35=G`/`35=H`, no `OrderQty` (tag 38), no ClOrdID send.

`CTraderFixLogonHostedService` logs: `FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled)` and forces `_runtime.RealCopyEnabled = false`.

`CTraderFixOptions.RealCopyExecutionEnabled` default is **false** (comment: NewOrderSingle default OFF). Nothing in product binds env `REAL_COPY_EXECUTION_ENABLED` onto that POCO.

`apps/fix-worker/Worker.cs` reads `CTrader:RealCopyExecutionEnabled` only to log. If true it **warns** and still stamps TRADE `Disconnected` / “NewOrderSingle remains off.” No socket.

**Therefore:** even if REAL_COPY is armed in env, this process **cannot** send `35=D`. Adding a builder is a **separate, later** change that requires the three preconditions below.

---

## 6. LiveRuntimeStatus — process-level refuse

File: `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`

```32:44:D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs
    public bool RealCopyEnabled { get; set; }
    ...
        copyNote = RealCopyEnabled
            ? "LIVE SEND ARMED — unexpected"
            : "NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.",
```

Pins:

| Site | Assignment |
|---|---|
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L38–41 | `RealCopyEnabled = false` with comment “Do not arm a flag that cannot be honored safely.” |
| `CTraderFixLogonHostedService` L68 | `_runtime.RealCopyEnabled = false` after logon |
| `/api/settings` | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (therefore false) |
| `/api/settings` | `FEATURE_COPY_TRADING_ENABLED` = **literal `false`** (ignores env) |
| `/api/health` | `realCopyEnabled = runtime.RealCopyEnabled` |
| `EfDashboardQueries` overview | passes `_runtime.RealCopyEnabled` |

If Snapshot ever shows `LIVE SEND ARMED — unexpected`, that is a **bug**, not success.

`/api/reconciliation/status` is a stub (`unknownPositions=0`, `mismatches=0`, `orphanFills=0`, note “recon runs only after FIX TRADE logon; NewOrderSingle still off”). That is **not** real recon. `RiskEvaluationRequest.Reconciled=true` must not be forged from this endpoint.

---

## 7. Gate table (measured)

| Gate | Required for SHADOW feature | Required for live `35=D` | Measured now |
|---|---|---|---|
| `FEATURE_COPY_TRADING_ENABLED` | ON | ON | Env key exists; API literal **false**; unused in workers |
| Generate SHADOW `CopyIntent` | Yes | Yes (upstream) | `PersistDemoShadowAsync` only when state==SHADOW and a dest quote exists |
| `RiskEngine.Evaluate` persisted | **Must add** | Yes | Engine exists; **not called** on persist path |
| `AllowFixSend` | **false** | true only if conjunction + LIVE + real recon + persistent sender | Conjunction coded; callers absent; tests force false |
| `CanPromoteToLive` | false | explicit human promote to LIVE | **always false** |
| LIVE traders in scores | No | **Yes** | Scorer never emits LIVE |
| Real recon | No (still evaluate `Reconciled`) | **Yes** | API stub zeros |
| Persistent TRADE session | No (logon-only OK) | **Yes** | Fire-and-forget `35=A`; socket disposed |
| `35=D` builder | **Must not exist** | Must exist **and** refuse unless gates | **Absent** (`SAFE_BY_ABSENCE`) |
| `REAL_COPY_EXECUTION_ENABLED` | May be armed in env | Necessary, not sufficient | Env key exists; process pin false; worker log-only |
| `LiveRuntimeStatus.RealCopyEnabled` | **false** | true only at go-live review | Forced false |

---

## 8. What to change if we “enable it” (safe scope)

This is the **only** enablement that matches the standing no-loss rule.

1. **Honor `FEATURE_COPY_TRADING_ENABLED`** in ingest/scoring after persist score:
   - If false: no new `CopyIntent`.
   - If true **and** `SuggestedState == SHADOW`: emit SHADOW intents (idempotent keys already `shadow:{broker}:{login}:{positionId}`).
2. **Always** `RiskEngine.Evaluate` those intents. Persist `RiskDecision` / `RiskDecisionRecord`. Force `RealExecutionEnabled=false` on the request so `AllowFixSend` cannot go true.
3. Simulate fills with `ShadowCopyEngine` only after evaluate (approve or reject both stay off-wire).
4. Leave `REAL_COPY_EXECUTION_ENABLED` alone as an env arm. **Do not** copy it onto `LiveRuntimeStatus.RealCopyEnabled`.
5. Do **not** implement `35=D`.
6. Do **not** change `CanPromoteToLive` to return true.
7. Do **not** treat FIX LoggedOn as `ReadyForExecution`.

### Still forbidden after that enablement

- Dashboard button / settings PUT that sets `RealCopyEnabled=true`.
- Worker sending because `CTrader:RealCopyExecutionEnabled` is true.
- Forging `Reconciled=true` from the stub.
- Auto-flatten / live reduce because `IsReducing` can Approve — Approve ≠ send.

---

## 9. When (later) live send could be discussed

All of the following, measured PASS, not hoped:

1. Human-reviewed LIVE traders (`CanPromoteToLive` remains false; promote is a **separate, audited** command, not scorer output).
2. Real dest recon: position/order/fill compare on a **kept** TRADE socket; mismatches block increasing actions (`VENUE_NOT_RECONCILED` already exists).
3. Persistent TRADE session: heartbeat, seq, persist-before-send, ClOrdID uniqueness, reject/fill handling.
4. A `35=D` builder that is unreachable unless `AllowFixSend && REAL_COPY && LIVE && recon-clean && session READY_FOR_EXECUTION`.
5. Architecture A100/A101 (or successor) checklists 100% on disk.

Until then, env REAL_COPY may stay armed as a **reminder that the operator wants copy**. The process must keep `AllowFixSend=false` and `RealCopyEnabled=false`.

---

## 10. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` conjunction; shadow still evaluates |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | SHADOW ceiling; `CanPromoteToLive => false` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Only `35=A`; no `35=D` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Pin + “LIVE SEND ARMED — unexpected” |
| `D:\Prop\.env` | **Flag names only** |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Re-pin after logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled` default false |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | SHADOW_ONLY intents; no risk call |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Simulated fills |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | Intent shape |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | LIVE exists; unused by scorer |
| `D:\Prop\apps\api\Program.cs` | Settings literals |
| `D:\Prop\apps\fix-worker\Worker.cs` | Log-only REAL_COPY read |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | `AllowFixSend` false when real flag false |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | promote lock |

---

## 11. One-line answer to “enable it”

**Turn on `FEATURE_COPY_TRADING_ENABLED`, emit SHADOW intents, run `RiskEngine.Evaluate`, keep `AllowFixSend=false`.** Env `REAL_COPY_EXECUTION_ENABLED` may already be true; that does **not** authorize send. Automatic SHADOW→LIVE is **FORBIDDEN**. Live `35=D` stays off until LIVE traders, real recon, and a persistent TRADE sender exist.
