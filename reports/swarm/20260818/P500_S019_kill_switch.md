# P500_S019 — Kill switch: `STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S019_kill_switch.md` |
| Agent | P500_S019 (kill-switch law + measured tree) |
| Date | 2026-08-18 |
| Assigned | Read RiskEngine kill switch, `docs/risk.md`, A48 if present. Write this file. Product source **not** edited. |
| Product source edited | **No** |
| Test source edited | **No** |
| Binding law | Architecture **§40**, **§41**, **§53**, **§59**, **§64**, **§68**, **§70.13**, **§72.18–19** |
| Spec | `D:\Prop\reports\swarm\20260818\A48_kill_switch.md` (authoritative design) |
| Docs | `D:\Prop\docs\risk.md` (operator-facing; **conflicts** with A48 on auto-flatten) |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` + `KillSwitchMode` + `KillSwitch` entity |
| Priors | B13, D13, D70, A23, A71, A95, A100 G16, A101 item 13, C33 |
| Method | Re-read §40, A48, `docs/risk.md`, engine, entity, dashboard, DI, fix-worker, tests. Quote current lines. Nothing from memory. |

Classification: `LAW` / `EXISTS_NEEDS_REFACTOR` / `UNSAFE` / `MISSING` / `DEAD` / `SAFE_BY_ABSENCE`.

---

## 0. Verdict

**Specified: two independent controls. Implemented: one exclusive enum. Not wired to any sender. Do not flatten source MT5. Lower loss on destination only.**

| Claim | Class | One-line |
|---|---|---|
| `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN` | **LAW** (§40 / A48) | Separate effect, permission, confirmation, and dashboard indicator. |
| Never flatten MT5 source | **LAW** | Kill scope is the **destination** cTrader/cServer copy book. Source positions are not ours to close. Shadow has no live orders. |
| Lower loss on destination only | **LAW** | Flatten / reduce / close emit dest `CLOSE_EXPOSURE` sized from **known dest remaining**, not source lots. Daily-loss `GLOBAL_STOP` must **not** auto-flatten. |
| Wired to a sender today | **NO** — `SAFE_BY_ABSENCE` | `Evaluate` **is** called from `CopyTradingService` (kill switch forced `None`; `AllowFixSend` persisted false). No `35=D` builder. |
| `docs/risk.md` auto-flatten on total-loss | **DO NOT IMPLEMENT** | A48 / §39: `GLOBAL_STOP` engages **stop-new only**. Threshold auto-flatten is unauthorized. |
| Exclusive `KillSwitchMode` as SoT | **UNSAFE** if treated as live state | Cannot store `{stop-new ON × flatten ACTIVE}`. Seeded `None` is fail-open. |
| Flatten actually flattens | **MISSING** | Enum value only blocks new opens (`EMERGENCY_FLATTEN_BLOCKS_NEW`). No run, no dest snapshot, no `35=D`. |
| §68 “kill switch tested” / §70.13 | **`[ ]` FAIL** | Labels exist; independent controls, audit, harness, dest-book proof do not. |

**Bottom line:** stop-new is the safe first brake (leave dest book). Flatten is a SuperAdmin + step-up **run** that closes **known destination** positions only. They must both be representable at once. Today neither is on a send path. Do not “simplify” them into one button. Do not implement `docs/risk.md` total-loss → flatten.

## Profit implication

Kill switch is a **loss cap**, not an edge. `STOP_NEW_EXECUTION` stops new dest risk. `EMERGENCY_FLATTEN` must close **dest only** — never source MT5 (those positions are not ours). Synthesis daily dest loss **$200–500** then stop-new (lab `MaxDailyExecutionLoss = 2000` is too loose). Auto-flatten on total-loss (`docs/risk.md` L36) is **unauthorized** and can realize a huge dest hole at the worst print.

**Remeasured 2026-08-18:** `RiskEngine` **is** in DI (`DependencyInjection.cs` L45). `CopyTradingService` also does `private readonly RiskEngine _risk = new();` and **does** call `Evaluate` (L159). But it **hard-sets** `KillSwitch = None` and persists `AllowFixSend = false`. Exclusive enum still cannot store `{stop-new ON × flatten ACTIVE}`. Flatten executor still **MISSING**. `docs/risk.md` still says total-loss → flatten.

---

## 1. Binding law

### 1.1 Architecture §40 (verbatim)

Source: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1542–1560.

```text
STOP_NEW_EXECUTION
```

and a separately permissioned:

```text
EMERGENCY_FLATTEN
```

- Do not conflate them.
- `STOP_NEW_EXECUTION` prevents new copy orders but leaves existing positions untouched.
- `EMERGENCY_FLATTEN` attempts to close destination positions and therefore requires stronger authorization/confirmation.

§53 (L1976–1977) requires **both** indicators on the Risk page: `STOP_NEW_EXECUTION` **state** and `EMERGENCY_FLATTEN` **availability**. One chip is a display violation.

§59 lists two verbs: `activate stop-new-orders` vs `request emergency flatten`. All audited.

### 1.2 A48 (binding design; specification only — no product change)

A48 is on disk and is the implementer’s contract. It does **not** implement.

| Control | Kind | Does | Must not |
|---|---|---|---|
| `STOP_NEW_EXECUTION` | Durable 2-state **latch** | Block `OPEN_EXPOSURE` / `INCREASE_EXPOSURE`. Dest book **untouched**. Reduce/close of **mapped dest** allowed by default (`allow_risk_reduction_while_stop_new=true`). | Close, cancel-all, send flatten `35=D`, flip `REAL_COPY`, rewrite trader state, touch source MT5. |
| `EMERGENCY_FLATTEN` | Separately permissioned **run** (phase machine, not a bool) | Force stop-new ON (side-effect `STOP_NEW.ENGAGED_BY_FLATTEN`). Snapshot **known dest ids**. Persist `CLOSE_EXPOSURE` before send. Close dest remaining qty. | Be an alias of stop-new. Auto-fire from `GLOBAL_STOP`. Run without SuperAdmin + step-up. Blind-retry unknowns. Flatten source MT5 or shadow. Size from source lots. |

Invariant (A48 §2): at any instant `{stop-new off\|on} × {flatten idle\|confirm-pending\|active\|partial-failed}`. Flatten phases **never replace** the stop-new bit. `{stop-new ON + flatten ACTIVE}` is the **normal** in-progress pair.

One-way coupling is **not** identity:

```text
flatten request     →  stop-new ON     (audited side-effect)
flatten complete    →  stop-new stays ON   (never auto-clear)
deactivate stop-new while flatten not idle  →  409
```

`REAL_COPY_EXECUTION_ENABLED` is a **third** control (§41 / A49). Not a kill switch. Default **false**. Flatten (when Phase 8 ships) may send **reducing** NOS even if the copy flag is false, because it is exit, not new copy.

### 1.3 Who may do what

| Actor | Stop-new activate | Flatten request |
|---|---|---|
| Human `RiskManager` | yes | **no** |
| Human `SuperAdmin` | yes | yes + step-up + typed phrase + single-use token |
| System `risk-engine` (`GLOBAL_STOP`) | yes (`engine_global_stop`) | **never** |
| `fix-worker` / `mt5-worker` | never (honor only) | **never** |

Engine daily-loss / portfolio-DD raise **stop-new only**. Auto-flatten from a threshold is a trading-policy decision the architecture **did not authorize**.

---

## 2. Never flatten MT5 source — lower loss on destination only

This is the capital-safety sentence for this slot.

| Book | Kill-switch action |
|---|---|
| Source MT5 (Achiever / Starwave / any manager book) | **Never close, reduce, or hedge.** We observe and reconstruct. Positions belong to the prop traders / broker, not this copy desk. |
| Shadow book | No live orders. Do not emit dest closes “for” shadow. |
| Destination cTrader execution account (Pepperstone / copy dest) | **Only** book that flatten or source-driven reduce/close may touch. Qty = dest remaining on a **known** `destination_position_id`. |

Consequences:

1. A source close does **not** become a dest flatten of the whole book. It becomes `CLOSE_EXPOSURE` / `REDUCE_EXPOSURE` of the **mapped dest remainder** (A43 / A74). Unmapped dest ids are recon issues, not flatten targets (A48 §12.3).
2. `EMERGENCY_FLATTEN` snapshots dest positions **once**. Positions opened after the snapshot cannot exist if stop-new + flatten-active are honored. Externals become recon, not silent extra targets.
3. Flatten does not guess qty from source lots (A48 §3.2: “qty comes from the mapped dest position”).
4. Unknown dest order state → `blocked_unknown`. No second `35=D` until §34 recovery says the first never landed.
5. TRADE down mid-run: pause send, keep phase `active`, alert. No unbounded flatten blast (§62).

**Lower loss** means: stop new dest risk immediately (`STOP_NEW_EXECUTION`); if an authorized human then chooses to cut dest exposure, close **dest** size only. It does **not** mean “panic-close everything we can see, including source.”

---

## 3. `docs/risk.md` — useful vocabulary, wrong auto-flatten

File: `D:\Prop\docs\risk.md` (68 lines). Operator-facing. **Not** the implementer’s SoT when it conflicts with §40 / A48.

What it gets right:

- Two named mechanisms: Kill Switch vs Emergency Flatten (L18–32).
- Daily-loss → stop-new, positions stay (L35).
- Operator can choose either, depending on the button (L37).
- Manual reversal of the latch (L26).

What it gets **wrong** (do not code):

| `docs/risk.md` text | Why it is rejected |
|---|---|
| “Closes all open positions immediately at market” (L29) | Does not say **destination only**. Closing source MT5 is forbidden. |
| “Total loss breach → Emergency Flatten → then Kill Switch ON” (L36) | A48 / §39: total-loss / daily-loss / DD engage **stop-new only**. Auto-flatten is unauthorized. |
| “kill switch auto-enabled after” flatten (L31) | Directionally OK as the one-way side-effect, **if** flatten was a human-confirmed run. Not after an engine threshold. |
| Hard % defaults (5% daily / 10% total) as if they fire flatten | Engine `GLOBAL_STOP` must latch stop-new. Numbers are config (A23), not flatten triggers. |
| Copy timing / slippage / volume-scale notes | Adjacent copy policy; not kill-switch state. |

**Instruction for later coding:** treat `docs/risk.md` as a stale brochure where it auto-flattens. Implement A48. Do not add an engine path that market-closes dest (or source) on `MaxDailyExecutionLoss` / `MaxPortfolioDrawdown`.

---

## 4. Measured product (2026-08-18)

### 4.1 Exclusive enum as SoT — A48 violation

```1:8:D:\Prop\src\Domain\Enums\KillSwitchMode.cs
namespace TraderIntelligence.Domain.Enums;

public enum KillSwitchMode
{
    None = 0,
    StopNewExecution = 1,
    EmergencyFlatten = 2
}
```

No `[Flags]`. One value. `None = 0` invites “safe / off.” A48 §14: **do not** treat `KillSwitchMode.None` as the safe default. A48 §3.3: missing/fresh row → treat stop-new **ON**.

```5:12:D:\Prop\src\Domain\Entities\KillSwitch.cs
public sealed class KillSwitch
{
    public Guid Id { get; set; }
    public KillSwitchMode Mode { get; set; }
    public string? SetBy { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

EF table `kill_switches` (PK only). Seeders write `Mode = KillSwitchMode.None` (`DemoSeeder`, `BrokerCatalogSeed`). Fail-**open** for new copy.

| stop-new | flatten | Representable as `KillSwitchMode`? |
|---|---|---|
| off | idle | `None` — fail-open vs A48 boot policy |
| on | idle | `StopNewExecution` |
| on | active | **Impossible** — required in-progress pair |
| off | active | Impossible (and illegal: flatten must force stop-new) |
| any | confirm-pending / partial-failed | Impossible (flatten is not a bool) |

A48 recommended types (`KillSwitchState`, `FlattenRun`, `FlattenPhase`, `IKillSwitchQuery`, `IFlattenExecutor`) are **not on disk**.

### 4.2 `RiskEngine.Evaluate` — two labels, no flatten, send gate inverted

```78:82:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");
```

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

```147:161:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;

        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                CopyIntentId = request.CopyIntentId,
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }
```

| Required | Measured | Class |
|---|---|---|
| Stop-new blocks only increasing | `&& IsIncreasing` + reason `STOP_NEW_EXECUTION` | Directionally **PASS** |
| Flatten-in-progress also blocks increasing | Same shape, reason `EMERGENCY_FLATTEN_BLOCKS_NEW` (catalog name is `EMERGENCY_FLATTEN_ACTIVE`) | Block-new **PASS**; flatten execution **MISSING** |
| Both on at once | Impossible exclusive enum | **FAIL** (A48) |
| Reduce/close still sendable under stop-new | `AllowFixSend` requires `KillSwitch == None` | **FAIL** — approved close cannot send |
| Loss / DD latch stop-new, allow exits | Loss/DD checks apply to **all** actions (no `IsIncreasing`) | **UNSAFE** if this method were live — freezes dest exits |
| Engine auto-flatten on those caps | No dest close emission | **PASS by omission** (do not add) |
| Touch dest or source positions | Engine is a pure function | **PASS by omission** |

`IsIncreasing` = `OpenExposure` \| `IncreaseExposure`. `IsReducing` = `ReduceExposure` \| `CloseExposure`. Unknown enum values fall through to `APPROVED` if loss/DD pass.

### 4.3 Not wired to a sender

`grep Evaluate(` under product `*.cs` (this pass) hits:

- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (definition)
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (product caller; kill switch forced `None`)
- `D:\Prop\tests\Unit\RiskEngineTests.cs` (five facts)

`D:\Prop\src\Infrastructure\DependencyInjection.cs` `AddTraderIntelligence` (remeasured):

- Registers reconstructor, scorer, ingest, dashboard, FIX **logon** host, **`RiskEngine` singleton**, **`CopyTradingService`**, **`CopyTradingHostedService`**.
- Does **not** register `IKillSwitch*` or a flatten executor.
- Sets `RealCopyEnabled` from `REAL_COPY_EXECUTION_ENABLED=="true"` (not hard-false). Send still off: `NewOrderSingleImplemented = false`.

`D:\Prop\apps\fix-worker\Worker.cs`: 15s loop stamps FIX rows `Disconnected`, `NewOrderSingle remains off`. Reads `CTrader:RealCopyExecutionEnabled` default **false**. No builder, no `35=D`.

Dashboard `GetRiskAsync` returns one string `(ks?.Mode ?? None).ToString()` and `RealCopyEnabled=false`. Web `RiskPage.tsx` paints one MetricCard `"Kill switch"`. TS `RiskStatus` has two bools (`stopNewExecution`, `emergencyFlatten`) that the C# DTO does **not** emit (D76 mismatch). No `POST /api/v1/risk/stop-new-execution/*`. No flatten endpoints. No RBAC.

Settings `KillSwitchEnabled` is an appsettings bool, **not** the §40 latch.

### 4.4 Tests (not go-live proof)

`D:\Prop\tests\Unit\RiskEngineTests.cs` — 5 facts. Kill-related:

- `Stop_new_execution_blocks_opens_not_closes` — open → `GlobalStop`; close → `Approve` and `AllowFixSend=false` (because fixture `RealExecutionEnabled=false`, **and** would still be false if the flag were true while mode ≠ `None`).
- **Zero** facts for `KillSwitchMode.EmergencyFlatten`.
- Named A27/A48 classes (`KillSwitchStopNewExecutionTests`, `KillSwitchEmergencyFlattenAuthorizationTests`, `KillSwitchSeparationTests`, `GlobalStopNewOrdersTests`) are **not on disk**.

Go-live boxes this file owns stay unchecked:

```text
[ ] STOP_NEW_EXECUTION does not flatten
[ ] EMERGENCY_FLATTEN permission is distinct (SuperAdmin + confirm)
[ ] GLOBAL_STOP engages stop-new only
[ ] All kill-switch mutations audited
[ ] Global stop-new-orders works in FIX harness (§70.13)
[ ] Kill switch tested (§68 / A100 G16)
```

---

## 5. Reason-code catalog (keep separate)

| Situation | Binding code (A23 / A48 / A71) | Engine today |
|---|---|---|
| Stop-new on, increasing | `STOP_NEW_EXECUTION` | `STOP_NEW_EXECUTION` |
| Flatten run in progress, increasing | `EMERGENCY_FLATTEN_ACTIVE` | `EMERGENCY_FLATTEN_BLOCKS_NEW` (adjacent, not catalog) |
| Flatten owns this dest id (source-driven close) | `FLATTEN_OWNS_POSITION` | **MISSING** |
| Daily loss / portfolio DD | `GLOBAL_STOP` + engage stop-new | `MAX_DAILY_EXECUTION_LOSS` / `MAX_PORTFOLIO_DRAWDOWN` — **also blocks close** |

---

## 6. Anti-patterns (reject in review)

```text
[DO NOT] One bool, one exclusive enum-as-state, or one “halt trading” RPC that sometimes flattens
[DO NOT] Implement docs/risk.md “total loss → Emergency Flatten”
[DO NOT] Flatten source MT5 or shadow
[DO NOT] Size flatten from source lots
[DO NOT] RiskManager flatten, or flatten without typed confirm
[DO NOT] Auto-clear stop-new when flatten completes or when loss recovers
[DO NOT] AllowFixSend require KillSwitch.None (that freezes dest exits)
[DO NOT] Wire Evaluate onto a sender while loss/DD freeze closes
[DO NOT] Blind NewOrderSingle retry on flatten disconnect
[DO NOT] Ship flatten mutation before Phase 8 + TRADE reconcile + dest-id map
[DO NOT] Treat KillSwitchMode.None as “safe default off”
[DO NOT] Claim §40 / §68 / §70.13 implemented
```

---

## 7. What this artifact did not do

- Did not modify product source under `D:\Prop\src` or `D:\Prop\apps`.
- Did not modify tests.
- Did not implement kill-switch endpoints, two-bit state, flatten executor, or migrations.
- Did not authorize auto-flatten, multi-account flatten, or cancel-all-open-orders.
- Did not send or simulate `35=D`.

Live copy remains **SAFE_BY_ABSENCE**: no RiskEngine on the send path, no NOS builder, `REAL_COPY` default false. That is **not** a tested kill switch.

**Implement later (not this task):** two durable, independently permissioned, fully audited levers per A48. `STOP_NEW_EXECUTION` is the default safe halt. `EMERGENCY_FLATTEN` is SuperAdmin, step-up, persist-before-send close of **known destination** positions only. Never one flag. Never source MT5.
