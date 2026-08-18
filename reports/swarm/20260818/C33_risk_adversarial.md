# C33 — RiskEngine adversarial: emergency flatten + close path

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C33_risk_adversarial.md` |
| Agent | C33 (risk adversarial — flatten + close only) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (`Evaluate`, `IsIncreasing`, `IsReducing`, `Reject`, `AllowFixSend` conjunction) |
| Adjacent product read | `KillSwitchMode.cs`, `CopyIntentAction.cs`, `RiskDecisionOutcome.cs`, `KillSwitch.cs`, `CopyIntent.cs`, `ExecutionIntent.cs`, `RiskDecisionRecord.cs`, `AuditLog.cs`, `ExecutionOrderStateMachine.cs`, `QuantityNormalizer.cs`, `CopyIntentExpiry.cs`, `ShadowCopyEngine.cs`, `DemoSeeder.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `TraderDbContext.cs`, `DependencyInjection.cs`, `CTraderFixOptions.cs`, `apps/api/Program.cs`, `apps/fix-worker/Worker.cs`, `apps/web` Risk page + types, `.env.example` |
| Tests read | `D:\Prop\tests\Unit\RiskEngineTests.cs` (5 facts; **zero** `EmergencyFlatten`) |
| Binding law | Architecture §33–§35, §39–§41, §53, §59, §62–§64, §68, §70.13, §72.18–19; `docs/risk.md`; A23 §5 / §8.2, A25 §6.5, A48, A49 §5, A70 §13, A71 §5 / §10 / §13–14 |
| Method | Read the current `Evaluate` body. Trace every `CloseExposure` / `ReduceExposure` / `KillSwitchMode.EmergencyFlatten` branch with the request fields the engine actually has. Contrast with A48 flatten-run law and A71 close-family law. No product edits. Nothing answered from memory. |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `SAFE_BY_ABSENCE`.

---

## 0. Verdict

**UNSAFE as a flatten/close control. SAFE_BY_ABSENCE as a live send path.**

`RiskEngine.Evaluate` sketches the right *family split* (`IsIncreasing` vs `IsReducing`) and the right *open-family* flatten block (`EMERGENCY_FLATTEN_BLOCKS_NEW`). It does **not** implement `EMERGENCY_FLATTEN`. Setting `KillSwitchMode.EmergencyFlatten` **never closes a destination position**. The reducing fall-through then `APPROVE`s any `CloseExposure` / `ReduceExposure` that survives three book-loss checks, with `ApprovedQuantity = RequestedQuantity` and **no dest identity**. The send bit is then forced off whenever the kill enum is not `None`.

That is the opposite of the architecture:

| Required (A48 §3.2 / A71 §10) | Measured in `Evaluate` today |
|---|---|
| Flatten is a SuperAdmin + step-up **run** that emits `CLOSE_EXPOSURE` | A single exclusive enum value. No run, no targets, no confirm, no audit write |
| Flatten-active **blocks new** copy | **PASS** on the predicate (`IsIncreasing` only) |
| Flatten **closes known dest ids** (persist-before-send) | **MISSING** — engine never sees a dest id |
| Flatten closes **may send** even if `REAL_COPY=false` (A25 §6.5) | **FAIL** — `AllowFixSend` requires `RealExecutionEnabled` |
| Flatten / source-close **must send** while stop-new is on | **FAIL** — `AllowFixSend` requires `KillSwitch == None` |
| Flatten-owns-this-id **coalesces** source close (no second NOS) | **MISSING** |
| Daily-loss / trader-loss / DD **must not freeze exits** (A71 E8) | **FAIL** — lines 117–124 apply to **every** action |
| `{stop-new on} × {flatten active}` representable | **FAIL** — exclusive `KillSwitchMode` |

`grep Evaluate(` under product `*.cs` hits only `RiskEngine.Evaluate` and `tests/Unit/RiskEngineTests.cs`. `AddTraderIntelligence` does **not** register the engine. The FIX worker still refuses `NewOrderSingle`. There is no flatten executor, no `source_destination_links`, no dest-position field on the request.

**Do not claim §40 / §64 / A48 flatten are implemented.** The book is currently protected because **nothing sends**, not because flatten works. A naïve “make `AllowFixSend` true under flatten” change would authorize blind closes of unmapped, unclipped, possibly unknown qty with no confirm.

Go-live boxes this path owns remain **unchecked** (A48 §13, A100, §68):

```text
[ ] STOP_NEW_EXECUTION does not flatten          -- true by omission; not proven as a control
[ ] EMERGENCY_FLATTEN permission is distinct
[ ] GLOBAL_STOP engages stop-new only
[ ] Flatten unknown-state does not blindly resend
[ ] Kill switch tested
```

---

## 1. What exists (measured)

### 1.1 Engine surface that flatten/close can touch

`D:\Prop\src\Domain\Risk\RiskEngine.cs` — 189 lines. Pure function. No I/O. No dest book.

Request fields **relevant to flatten/close** (lines 32–56):

```text
Action, RequestedQuantity, ExpectedPrice, Quote,
VenueHealthy, RealExecutionEnabled, Reconciled, KillSwitch,
TraderRealizedLoss, DailyExecutionPnl, PortfolioDrawdown,
CurrentGrossXau, CurrentNetXau, OpenPositions, MarginUsage,
MartingaleFlag, AbnormalSizing
```

Request fields the **spec** needs and the **request does not have**:

```text
linked_destination_position_id
dest_remaining_qty
dest_side
flatten_run_id
flatten_owner(this dest id)
flatten_phase
unknown_on_this_dest_id
TRADE logged on / lease owned          -- only a bool VenueHealthy
stop_new_execution  (independent bit)
allow_risk_reduction_while_stop_new
```

Without those, the engine **cannot** implement A48 §7 / A71 G06–G09 / G31. Any close approval is an opinion about the caller’s snapshot, not about a venue position.

### 1.2 The three flatten/close sites in `Evaluate`

Kill (open family only):

```78:82:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");
```

Dead `if` that *looks* like a close exception:

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

Send conjunction + reducing fall-through (the entire “close path”):

```147:178:D:\Prop\src\Domain\Risk\RiskEngine.cs
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
        ...
    private static bool IsReducing(CopyIntentAction action) =>
        action is CopyIntentAction.ReduceExposure or CopyIntentAction.CloseExposure;
```

`Reject(...)` always `ApprovedQuantity=0`, `AllowFixSend=false` (lines 180–188).

### 1.3 Adjacent types (flatten-relevant)

| Path | What it is | Class |
|---|---|---|
| `Domain/Enums/KillSwitchMode.cs` | `None=0`, `StopNewExecution=1`, `EmergencyFlatten=2` — **mutually exclusive** | **UNSAFE** as SoT (A48 §0, §5) |
| `Domain/Entities/KillSwitch.cs` | One `Mode` column | **UNSAFE** if treated as runtime state |
| `DemoSeeder.cs` 113–120 | Seeds `Mode = None` | **UNSAFE** vs A48 §3.3 fail-closed ON |
| `CopyIntent.cs` | `Action` + `RequestedQuantity`; **no** dest link, **no** `flatten_run_id` | **EXISTS_NEEDS_REFACTOR** |
| `ExecutionIntent.cs` | `ClOrdId`, qty, status; **no** dest position id, **no** exposure class, **no** flatten run | **EXISTS_NEEDS_REFACTOR** |
| `QuantityNormalizer.cs` | `sourceLots * allocationFactor` only | **MISSING** dest-remaining close path (A71 §8) |
| `CopyIntentExpiry.cs` | One `maxSignalAge` for all classes | **EXISTS_NEEDS_REFACTOR** (A71 §9) |
| `ExecutionOrderStateMachine.MayRetryNewOrderSingle` | `NotSent` **or** `Rejected` | **UNSAFE** if flatten ever uses it (retry storm) |
| `Infrastructure/DependencyInjection.cs` | No `RiskEngine`, no `IKillSwitch*`, no `IFlattenExecutor` | **MISSING** |
| `apps/fix-worker/Worker.cs` | Heartbeat + “refuses NewOrderSingle”; no flatten | **MISSING** send + flatten |
| `apps/api/Program.cs` | `GET /api/risk` read-only; **no** flatten POST | **MISSING** (A06: flatten not in v1 — correct omission) |
| `EfDashboardQueries.GetRiskAsync` | `(ks?.Mode ?? None).ToString()` + `RealCopyEnabled=false` literal | **EXISTS_NEEDS_REFACTOR** — one chip, not two controls |
| `apps/web/.../RiskPage.tsx` | One “Kill switch” metric = that string | **EXISTS_NEEDS_REFACTOR** |
| `apps/web/src/types/index.ts` | `stopNewExecution` + `emergencyFlatten` booleans — **not** what the API returns | **UNSAFE** as a contract |
| `.env.example` L100 | `RISK_EMERGENCY_FLATTEN_ENABLED=true` | **UNSAFE** documentation — **no product reader** |
| Flatten run / target / confirm token / RBAC | nowhere under `src/` / `apps/` | **MISSING** |

A23 reason `EMERGENCY_FLATTEN_ACTIVE` is **not** emitted. The engine string is `EMERGENCY_FLATTEN_BLOCKS_NEW` (A89 #50 locks the stub name; A95 maps it to the spec name).

---

## 2. Binding law (only what this path must do)

Architecture §40 (verbatim contract):

```text
STOP_NEW_EXECUTION
  prevents new copy orders but leaves existing positions untouched.

EMERGENCY_FLATTEN
  attempts to close destination positions and therefore requires
  stronger authorization/confirmation.

Do not conflate them.
```

A48 + A71 close-family / flatten extras (normative for this report):

1. Two independent bits. Flatten request **forces stop-new ON** (`STOP_NEW.ENGAGED_BY_FLATTEN`) and **never auto-clears** it.
2. Flatten is a **run**, not a boolean: snapshot dest positions once → persist `CLOSE_EXPOSURE` per known id → send only those. Qty = dest remaining, **not** source lots.
3. While flatten `active` / `confirm_pending`: OPEN family → `EMERGENCY_FLATTEN_ACTIVE`. Source CLOSE of an **owned** dest → **coalesce** (no second NOS).
4. Flatten closes skip **entry** quote-age / price-move / `REAL_COPY`. They still require TRADE logon, lease, known dest id, no `EXECUTION_STATE_UNKNOWN` on that id, persist-before-send.
5. `GLOBAL_STOP` / daily-loss / DD engage **stop-new only**. They **must not** reject mapped CLOSE (A71 E8/E9, G21–G23). They **must not** auto-flatten.
6. `AllowFixSend` on CLOSE-family **must not** require `KillSwitchMode.None` (A71 §10).
7. Never flatten source MT5 or shadow-as-live. Never invent a dest so a late source close has something to hit (`NO_DESTINATION_POSITION`).
8. Failure to flatten is an **alert**, not a retry storm (A23 §8.2). `MayRetry(Rejected)` is illegal for flatten.

---

## 3. Close-family trace (every gate)

Action ∈ `{ReduceExposure, CloseExposure}`. First-blocking-check order as coded.

| # | Gate | Applies to reduce/close? | Effect today |
|---:|---|---|---|
| 1 | `StopNew && IsIncreasing` | no | skipped |
| 2 | `EmergencyFlatten && IsIncreasing` | no | skipped — flatten **does not own** the close |
| 3 | `!Reconciled && IsIncreasing` | no | skipped — unreconciled close is not rejected here |
| 4 | `!VenueHealthy && IsIncreasing` | no | skipped |
| 5 | empty `RealExecutionEnabled==false && Action!=Close` | Close: condition false. Reduce: enters empty body | **no-op**. Comment can be misread as “close bypasses the flag.” It does not; send still requires the flag |
| 6–8 | quote missing / stale / spread / mid-move | no (`IsIncreasing`) | skipped — **directionally correct** for A23 §8.2 entry-guard skip |
| 9 | signal age | no | skipped — **directionally correct** vs open clock; **wrong** vs A71 close expiry (`expires_at` unused) |
| 10 | `TraderRealizedLoss <= -MaxLossPerTrader` | **YES — all actions** | `PauseTrader` / `MAX_LOSS_PER_TRADER` / send=false / qty=0 |
| 11 | `DailyExecutionPnl <= -MaxDailyExecutionLoss` | **YES** | `GlobalStop` / `MAX_DAILY_EXECUTION_LOSS` |
| 12 | `PortfolioDrawdown >= MaxPortfolioDrawdown` | **YES** | `GlobalStop` / `MAX_PORTFOLIO_DRAWDOWN` |
| 13–19 | open-position / qty / gross / net / margin / martingale / abnormal | no | skipped — **directionally correct** (G24–G30) |
| 20 | `allowSend` conjunction | yes | `Real && KillSwitch==None && Reconciled && VenueHealthy` |
| 21 | `IsReducing` fall-through | yes | `Approve` + `RISK_REDUCTION` + **passthrough qty** + `AllowFixSend=allowSend` |

Identity / flatten-ownership / unknown-on-this-id / dest-remaining clip / `NO_DESTINATION_POSITION` / TRADE-vs-QUOTE split / persist: **not in the function**.

`CopyIntentExpiry` is **never called**. A 3-minute TRADE gap with 20 stale OPENs and 3 source CLOSEs (A71 E11) is not modeled.

---

## 4. Adversarial scenarios

Each scenario is a concrete request against **current** `Evaluate`. “Live effect” is what would happen **if** a worker honored `AllowFixSend`. Today the worker never sends — that is recorded as `SAFE_BY_ABSENCE`, not a pass.

### S01 — Panic flatten: operator sets `EmergencyFlatten`, dest still long

**Setup:** dest 1.20 XAU long. `KillSwitch=EmergencyFlatten`. `Action=CloseExposure`, `RequestedQuantity=1.20`. `RealExecutionEnabled=true`, `Reconciled=true`, `VenueHealthy=true`. Loss/DD inside limits.

**Trace:** skip 1–19 increasing/loss gates → `allowSend = true && (EmergencyFlatten==None) && …` → **false** → `Approve` / `RISK_REDUCTION` / qty `1.20` / `AllowFixSend=false`.

**No code** snapshots dest positions, persists a flatten run, or emits a reducing `35=D`.

**Result:** new opens are blocked (`EMERGENCY_FLATTEN_BLOCKS_NEW`). The dest book is **untouched**. Dashboard shows kill switch `"EmergencyFlatten"`. An operator can believe flatten is *on* while residual risk remains.

**Class:** **UNSAFE** (control lies). **SAFE_BY_ABSENCE** (no send).

### S02 — Flatten is exactly the case that cannot send

Flatten conjunction required by A25 §6.5 / A49 §5:

```text
TRADE logged on + lease + authorized flatten + persist-before-send
REAL_COPY may be false
KillSwitch flatten-active (and stop-new forced on)
```

Engine conjunction:

```text
AllowFixSend ⇔ RealExecutionEnabled
            ∧ KillSwitch == None
            ∧ Reconciled
            ∧ VenueHealthy
```

`KillSwitch == EmergencyFlatten` **implies** `AllowFixSend=false`.  
`RealExecutionEnabled=false` **implies** `AllowFixSend=false`.

The privileged close path the architecture invented is **logically excluded** by the send bit.

**Class:** **UNSAFE** (spec inversion).

### S03 — Stop-new ON + source close (the default safe halt)

**Setup:** `KillSwitch=StopNewExecution`, `Action=CloseExposure`, `Real=true`, recon+venue ok.

**Trace:** same as S01. `Approve` + `AllowFixSend=false`.

A71 §10 / A48 §3.1 default `allow_risk_reduction_while_stop_new=true`: this close **must** be sendable. The only unit test of this case **asserts the defect**:

```34:40:D:\Prop\tests\Unit\RiskEngineTests.cs
        var close = _e.Evaluate(Base(q => q with
        {
            Action = CopyIntentAction.CloseExposure,
            KillSwitch = KillSwitchMode.StopNewExecution
        }));
        close.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        close.AllowFixSend.Should().BeFalse();
```

A later implementer can keep this green forever and never be able to exit under stop-new.

**Class:** **UNSAFE**. Test-locked.

### S04 — Exclusive enum: cannot be stop-new ON **and** flatten ACTIVE

A48 invariant: flatten request **forces** stop-new on. Required state is the product of two bits.

`KillSwitchMode` is one column. Persisting `EmergencyFlatten` **erases** `StopNewExecution`. Clearing flatten (`Mode=None`) **clears stop-new** as a side effect — A48 anti-pattern *“auto-clear stop-new when flatten completes.”*

Seed (`DemoSeeder` L116): `Mode=None` on a fresh DB. A48 §3.3: missing row ⇒ treat stop-new as **ON**. Boot is fail-**open** for new copy (the engine is dead, so this is currently `SAFE_BY_ABSENCE` at send, **UNSAFE** as SoT).

**Class:** **UNSAFE** (A48 §0, §14 `[DO NOT] one enum-as-state`).

### S05 — Red day freezes the exit (A71 E8 inverted)

**Setup:** dest 1.20 long. Source full close **or** flatten CLOSE. `DailyExecutionPnl = -2000` (default cap). `Action=CloseExposure`. `KillSwitch=None` or `EmergencyFlatten`.

**Trace:** skip increasing gates → line 120–121 fires **before** the reducing fall-through:

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

**Result:** `GlobalStop` / qty 0 / `AllowFixSend=false`. The dest stays long on the day you are already losing. Flatten CLOSE is rejected by the same three checks.

A71 E8: daily-loss **raises stop-new**; the close **APPROVE**s. A48 §5.3: engine `GLOBAL_STOP` **never** requests flatten — and must not **block** flatten/source-close either.

Same shape for `TraderRealizedLoss=-500` and `PortfolioDrawdown=3000`. Boundaries: loss uses `<=` (hits exactly at the cap); DD uses `>=` (hits exactly at the cap). No test.

**Class:** **UNSAFE**. Highest-severity logic defect on this path.

### S06 — `GLOBAL_STOP` outcome is not a latch (and is not flatten — good — but it kills the close)

S05 returns `RiskDecisionOutcome.GlobalStop` and **forgets** it. A23 §8.3: engaging stop-new is an audited persist, not a hot-path bool. There is no `IKillSwitchCommands.ActivateStopNew`.

Positive: the engine **cannot** auto-flatten from a threshold (A48 `[DO NOT] auto-flatten from daily loss`). Negative: the same return **is** the thing that freezes the close.

**Class:** **EXISTS_NEEDS_REFACTOR** (no latch) + **UNSAFE** (exit freeze).

### S07 — Phantom close (A71 E6)

**Setup:** source `ENTRY_OUT` full. **No** dest link. `Action=CloseExposure`, `RequestedQuantity=1.00` (or leftover source lots). Loss/DD ok. `KillSwitch=None`, `Real=true`.

**Trace:** no mapping field exists → fall-through `Approve` / `RISK_REDUCTION` / qty `1.00` / `AllowFixSend=true` (this is the **only** close conjunction that can set send true).

Spec: persist terminal `NO_DESTINATION_POSITION`, **zero** FIX.

If anyone wires `AllowFixSend` to a builder, this is a close of a position that does not exist (venue reject, or worse, a close of the **wrong** netting slot).

**Class:** **UNSAFE** once send exists. **SAFE_BY_ABSENCE** today.

### S08 — Over-close / source-lot close (qty law)

`ApprovedQuantity = request.RequestedQuantity` with no dest remaining, no min/step, no clip.

Adversarial inputs the engine will bless:

| RequestedQuantity | Dest remaining (unknown to engine) | Spec (A71 §8) | Engine |
|---:|---:|---|---|
| `50` (fat-finger / source lots) | `0.10` | clip to `0.10`, class CLOSE | approve `50` |
| `0.40` REDUCE | `1.20`, `min=1.0` leftover `0.80` | if leftover `< min`, promote CLOSE remainder | approve `0.40` (orphan remainder) |
| `0` | `1.20` | reject / no-op | approve `0` (and if send were on, a zero-qty NOS) |
| negative | anything | reject | approve the negative |

`QuantityNormalizer` is an **allocation** function (`sourceLots * allocationFactor`). Flatten law: ignore source lots. The engine never calls the normalizer anyway.

**Class:** **UNSAFE**.

### S09 — Double-close (flatten owns dest + source CLOSE arrives)

A48 §7 / A71 G06 / E10: flatten is the **exclusive closer**. Source CLOSE of an owned dest **coalesces** (attach `source_event_id` to the flatten target; no second intent).

Engine: no dest id, no `flatten_owner`, `Reduce` and `Close` share one branch. Two callers can both get `Approve` + passthrough qty.

If a naïve fix only drops `KillSwitch==None` from `allowSend`, both intents become sendable → two reducing NOS on one dest (partial fill + overshoot / unknown state).

**Class:** **MISSING** coalesce. **UNSAFE** under the naïve send fix.

### S10 — Flatten-active, **other** dest ids, source REDUCE

A71 G07: OPEN family blocked; REDUCE/CLOSE of dests flatten does **not** own stay allowed.

Engine: `EmergencyFlatten` + `ReduceExposure` → `Approve` / `RISK_REDUCTION` / `AllowFixSend=false` (S02). No per-id distinction. Cannot implement G06 vs G07.

**Class:** **MISSING**.

### S11 — Unreconciled / unknown book close

A71 G08–G09: CLOSE of a **known** dest with **no** unknown on that id may proceed even if the **global** book is not `READY`. Unknown-on-this-id is **always** blocking (do not “fix” with a second close — §34).

Engine:

- `!Reconciled` does **not** reject reduce/close (good direction for G08).
- Then `allowSend` requires `Reconciled` (global), so send is off.
- There is **no** `unknown_on_this_dest_id` bit. A globally-reconciled book with one unknown 11 on **this** dest would still `Approve` + `AllowFixSend=true` when `KillSwitch=None` (S07 conjunction).

Flatten of unknowns is how you get a second disaster during a quote/ack gap (A48 §2).

**Class:** **UNSAFE** (missing per-id unknown). Global `Reconciled` on send is a blunt fail-closed that also **blocks legitimate flatten** of known ids during a partial recon.

### S12 — TRADE down vs QUOTE down

Spec: flatten/source-close need **TRADE** + lease. QUOTE down is a waterfall / wait, not an OPEN-style reject (A71 G10–G11). Flatten must **pause sending**, keep phase `active`, alert — not fail remaining targets (A48 §12.5).

Engine has one `VenueHealthy` bool. Close skips the reject, then `allowSend` requires that same bool. Cannot pause-vs-reject. Cannot tell QUOTE-down (still flatten) from TRADE-down (must not blast).

**Class:** **EXISTS_NEEDS_REFACTOR**.

### S13 — `REAL_COPY=false` flatten exception vs the empty `if`

A25 §6.5: flatten **may** send reducing NOS with the copy flag false. Source-driven live close in v1 still **needs** the flag (A71 G04).

The empty `if` (lines 90–93) special-cases **only** `CloseExposure` in the *condition* and then does nothing. A reader can conclude “close is exempt from the flag.” The send bit still requires `RealExecutionEnabled`. Flatten cannot use the exception. Source close cannot send when flag is false (correct for v1) **and** cannot send when flatten is on (incorrect).

`CTraderFixOptions.RealCopyExecutionEnabled` default `false` (comment: “allow placing **new** orders”). Flatten is not new copy. Worker L21–22 / L43–44: even if config is true, “still refuses NewOrderSingle.” No flatten exception there either.

**Class:** **MISSING** exception. Empty `if` is a foot-gun.

### S14 — Reversal leftover short (A71 E5)

`ENTRY_INOUT` sell 1.80 vs dest long 1.20 must emit **CLOSE 1.20 then OPEN leftover**. Under stop-new + rotten quote: close APPROVE, open REJECT, dest **flat**.

Engine has no classifier, no two-intent protocol, no dest remaining. A single `CloseExposure` of `1.80` would `Approve` qty `1.80` (S08). A single `OpenExposure` leftover would be blocked only if `IsIncreasing` and a kill/quote gate fires.

`ShadowCopyEngine.SimulateExit` prices an exit; it does **not** call `RiskEngine` and does not flatten-as-live.

**Class:** **MISSING** (classifier). Engine would over-close if fed the source volume.

### S15 — Blind retry after flatten reject / disconnect

`ExecutionOrderStateMachine.MayRetryNewOrderSingle`:

```35:36:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;
```

A23 §8.2 / A48 §12.7: flatten failure is an alert; unknown after send → §34 recovery only; **one** replacement `cl_ord_id` after proof the first never landed. Retrying `Rejected` with the same or a new 11 is a storm.

No flatten-specific status. `AfterSendAttempt()` → `SentAcknowledgementUnknown`. Disconnect → `ExecutionStateUnknown`. Those correctly `RequiresReconciliation` and are **not** in `MayRetry`. The **Rejected** retry is the hole.

**Class:** **UNSAFE** if flatten is later hung on this FSM unchanged.

### S16 — Confirm / RBAC / audit bypass (by absence of the control)

A48 §8: SuperAdmin + step-up (60s) + typed phrase `FLATTEN {account} {yyyy-MM-dd} UTC` + single-use 90s token. RiskManager **cannot** flatten. Denied attempts still audit.

Measured:

- API: `GET /api/risk`, `GET /api/risk/status` only. No `POST .../emergency-flatten/*`. CORS is `AllowAnyOrigin` / any method / any header (`Program.cs` L16–17) — irrelevant until a mutation exists.
- No ASP.NET policies, no roles, no step-up.
- `AuditLog` entity exists; nothing writes `KILL_SWITCH.FLATTEN.*`.
- Dashboard one string; availability not computed (A48: TRADE up ∧ dest XAU ∧ idle ∧ Phase 8 shipped).

This is the **correct v1 omission** (A06 / A48 §11: flatten mutation 409 until Phase 8). It is **not** a flatten implementation.

**Class:** **MISSING** (authorized). **SAFE_BY_ABSENCE** (no button to press).

### S17 — UI / DTO lie

| Layer | Flatten signal |
|---|---|
| DB `kill_switches.Mode` | exclusive enum, seeded `None` |
| `RiskDashboardDto.KillSwitch` | that enum’s `.ToString()` |
| `RiskDashboardDto.RealCopyEnabled` | **hardcoded `false`** (`EfDashboardQueries.cs` L159) |
| Web `RiskStatus` type | `stopNewExecution: boolean`, `emergencyFlatten: boolean` |
| Web `RiskPage` | `data.killSwitch` (the string). **Does not read** the two booleans |

Operator cannot see `{stop-new, flatten-phase}` independently (§53). Frontend types advertise two bits the API does not send.

**Class:** **UNSAFE** as an operator control surface.

### S18 — Env flag that nothing reads

`.env.example` L100: `RISK_EMERGENCY_FLATTEN_ENABLED=true` next to `RISK_KILL_SWITCH_ENABLED=true`. Grep under `src/` / `apps/`: **no reader**. A65 copies the same keys.

Adversarial reading: an operator (or a future coder) treats “flatten enabled=true” as permission to wire a single kill RPC. A48: flatten availability is **computed**, not a stored “ready to fire” secret. Default **true** in an example file is the wrong polarity.

**Class:** **UNSAFE** documentation. Dead as runtime.

### S19 — Caller can lie (untrusted snapshot)

Every gate is a field the **caller** sets. There is no `IKillSwitchQuery` re-read (A48 §5.2, A25 pre-send). A buggy or hostile worker can pass `KillSwitch=None`, `Reconciled=true`, `VenueHealthy=true`, `RealExecutionEnabled=true`, `Action=CloseExposure`, `RequestedQuantity=99` and receive `AllowFixSend=true`.

Risk approval is specified as **not** a capability token that survives a later flag-off (A49 §5). Here it is the only token, and it is made of caller-supplied bits.

**Class:** **UNSAFE** (trust model) once on a send path. **DEAD** today.

### S20 — Tests do not constrain flatten

`RiskEngineTests`: 5 facts. `EmergencyFlatten` **never** appears. `IncreaseExposure` / `ReduceExposure` never appear. `AllowFixSend=true` never appears. `RealExecutionEnabled=true` never appears.

A89 `KillSwitchEmergencyFlattenTests` / A27 `Risk.KillSwitchEmergencyFlattenAuthorizationTests` / A71 `OpenVsCloseExposurePolicyTests`: **not on disk**.

C03 already counted this. Restated because flatten has **zero** facts, and the one close fact **pins S03**.

**Class:** **MISSING**.

---

## 5. `AllowFixSend` matrix (close / flatten only)

`allowSend` is computed **before** the reducing return and is **not** family-aware.

| # | Real | KillSwitch | Reconciled | VenueHealthy | Action | Loss/DD | Code `AllowFixSend` | Spec source-close | Spec flatten CLOSE |
|---:|---|---|---|---|---|---|---|---|---|
| C1 | T | None | T | T | Close | ok | **true** | true | n/a (idle) |
| C2 | T | StopNew | T | T | Close | ok | **false** | **true** (default) | n/a |
| C3 | T | Flatten | T | T | Close | ok | **false** | coalesce or true | **true** |
| C4 | F | Flatten | T | T | Close | ok | **false** | false (v1) | **true** (A25) |
| C5 | T | Flatten | F | T | Close | ok | **false** | per-id | per-id known only |
| C6 | T | Flatten | T | F | Close | ok | **false** | TRADE wait | pause, stay active |
| C7 | T | None | T | T | Close | daily −2000 | n/a (rejected) | **true** | **true** |
| C8 | T | Flatten | T | T | Reduce | ok | **false** | G07 allow | not a flatten order |
| C9 | T | None | T | T | Open | ok | **true** | n/a | must stay **false** |
| C10 | T | Flatten | T | T | Open | ok | n/a (`GlobalStop`) | n/a | block new — **PASS** |

C1 is the **only** close send=true cell. It has **no** dest identity (S07/S08).  
C3+C4 are the cells flatten exists for. Both are **false**.  
C7 is the red-day cell. Close never reaches `allowSend`.

Open + flatten (C10) is the one flatten behavior the stub gets right.

---

## 6. Reason / outcome naming drift (flatten)

| Spec (A23 §4.3 / A48 §7) | Engine today | When |
|---|---|---|
| `EMERGENCY_FLATTEN_ACTIVE` | `EMERGENCY_FLATTEN_BLOCKS_NEW` | Open/Increase + flatten enum |
| flatten CLOSE: still `APPROVE` / reducing reason, **not** `RISK_REDUCTION` as if it were a source close | `RISK_REDUCTION` for **all** reduce/close | Cannot tell flatten-emitted from source-driven |
| `NO_DESTINATION_POSITION` / `MAPPING_MISSING` | never | S07 |
| `EXECUTION_STATE_UNKNOWN` | never | S11 |
| `TRADE_FIX_UNAVAILABLE` | never (only `VENUE_UNHEALTHY` on **increasing**) | S12 |

Using `RISK_REDUCTION` for flatten hides the exclusive-closer rule in every log and in `RiskDecisionRecord.Reason`.

---

## 7. Naïve-fix traps (do not ship)

These are the changes a hurried coder will want after reading S01–S03. All of them are **wrong** in isolation.

| Trap | Why it fails |
|---|---|
| Drop `KillSwitch == None` from `allowSend` | Unlocks C2/C3 **and** C1’s identity hole. Double-close (S09). Phantom close (S07). Over-close (S08). No TRADE/lease. No confirm. |
| Special-case `if (KillSwitch==EmergencyFlatten) allowSend=true` | Same, plus exclusive enum still cannot hold stop-new. Clearing flatten re-opens copy (S04). Red-day still rejects (S05). |
| Treat `EmergencyFlatten` as “close everything in Evaluate” | Engine has no dest book. Would invent qty from `RequestedQuantity`. Violates persist-before-send and SuperAdmin confirm. |
| Map `GLOBAL_STOP` → set `KillSwitch=EmergencyFlatten` | A48 `[DO NOT] auto-flatten from daily loss`. Also exclusive enum clears any independent stop-new history. |
| Reuse `MayRetryNewOrderSingle(Rejected)` for flatten | Retry storm (S15). |
| Size flatten with `QuantityNormalizer(sourceLots, allocation)` | Source lots ≠ dest remaining (A23 §7, A48 §3.2). |
| Add `POST /api/risk/emergency-flatten` that flips `KillSwitch.Mode` | That is stop-new-shaped flatten. No run, no confirm, no audit catalog, CORS is wide open. |
| Flip `.env` `RISK_EMERGENCY_FLATTEN_ENABLED` and call it done | Nothing reads it (S18). |
| Keep `Stop_new_execution_blocks_opens_not_closes` as-is while “fixing” send | Test will go red or will keep the defect. Relabel `CURRENT_STUB` or change the assertion to A71 §10. |

**Correct order (not this task):** A48 `KillSwitchState` (two bits + flatten phase) → dest links → flatten run snapshot → family-aware `AllowFixSend` → loss/DD skip on close family → identity/unknown/qty clip → executor persist-before-send → then tests. Do not enable the mutation before Phase 8 + TRADE reconcile (A48 §11).

---

## 8. Finding table

| ID | Finding | Spec | Class | Severity if wired to send |
|---|---|---|---|---|
| F01 | `EmergencyFlatten` blocks opens only; never emits / authorizes a dest close | §40, A48 §3.2 | **MISSING** flatten | residual dest risk while UI says flatten |
| F02 | `AllowFixSend` requires `KillSwitch==None` | A71 §10, A48 §3.1 | **UNSAFE** | cannot exit under stop-new or flatten |
| F03 | `AllowFixSend` requires `RealExecutionEnabled` for flatten CLOSE | A25 §6.5, A49 §5 | **UNSAFE** | privileged exit disabled when copy is off |
| F04 | Exclusive `KillSwitchMode` / `KillSwitch.Mode` / seed `None` | A48 §0, §3.3, §5 | **UNSAFE** | cannot be both on; boot fail-open; clear-flatten clears stop-new |
| F05 | Loss / daily PnL / DD reject **close and flatten** | A71 E8, G21–G23 | **UNSAFE** | exits frozen on a red day — **worst logic bug** |
| F06 | Close fall-through always `APPROVE` + passthrough qty | A71 G31, §8, E6 | **UNSAFE** | phantom / over / zero / negative close |
| F07 | No flatten-owns-id coalesce | A48 §7, A71 G06, E10 | **MISSING** | double NOS after naïve F02 fix |
| F08 | No dest id / remaining / side / flatten_run_id on request or intents | A48 §3.2, A71 §12 | **MISSING** | cannot implement flatten at all in this function |
| F09 | No per-id `EXECUTION_STATE_UNKNOWN` | §34, A71 G09 | **UNSAFE** | second close into an unknown |
| F10 | One `VenueHealthy` bit | A71 G10–G11 | **EXISTS_NEEDS_REFACTOR** | cannot pause flatten on TRADE down |
| F11 | Empty `if` on `RealExecutionEnabled` special-cases Close | A49 / A71 G04 | **EXISTS_NEEDS_REFACTOR** | comment foot-gun |
| F12 | Reason `EMERGENCY_FLATTEN_BLOCKS_NEW` vs `EMERGENCY_FLATTEN_ACTIVE`; flatten closes logged as `RISK_REDUCTION` | A23 §4.3 | **EXISTS_NEEDS_REFACTOR** | telemetry lie |
| F13 | `GLOBAL_STOP` is a return value, not an audited latch; does not (and must not) flatten | A23 §8.3, A48 §5.3 | **EXISTS_NEEDS_REFACTOR** | latch forgotten; close still killed (F05) |
| F14 | No executor, no API mutation, no RBAC, no audit write, engine not in DI, worker refuses NOS | A48 §5–§12 | **MISSING** / **SAFE_BY_ABSENCE** | cannot flatten; also cannot accidentally flatten |
| F15 | Dashboard one Mode string; web types are two booleans | §53, A48 §10 | **UNSAFE** (ops) | operator cannot see two controls |
| F16 | `.env.example` `RISK_EMERGENCY_FLATTEN_ENABLED=true` unread | A48 §3.2 availability | **UNSAFE** (docs) | trains a stored “ready” flag |
| F17 | `MayRetryNewOrderSingle(Rejected)` | A23 §8.2, A48 §12.7 | **UNSAFE** if reused | flatten retry storm |
| F18 | Zero flatten unit facts; stop-new close test pins F02 | A27, A48 §13, C03 | **MISSING** | greenwash |
| F19 | Engine not on any send / copy / shadow path | A23 §5, A101 | **DEAD** | safety is absence |
| F20 | `IsReducing` aliases REDUCE and CLOSE | A71 §3.3 | **EXISTS_NEEDS_REFACTOR** | cannot promote remainder / mark flatten-emitted |

**Do not “fix” F02 without F06–F09 and F04.** That pairing is the live-account landmine.

---

## 9. What is actually good (do not regress)

These pieces are the right shape. Keep them when the stub is replaced.

- Two reason codes, not one bool, for stop-new vs flatten-in-progress (even if flatten is incomplete).
- `IsIncreasing` gating on quote / spread / mid-move / signal / recon / venue-health / book caps / martingale — close family is **not** failed by entry news guards. That matches A23 §8.2 / A71 G12–G16 **direction**.
- Flatten-in-progress does **not** auto-fire dest closes from `Evaluate`. Auto-flatten from a threshold is forbidden; the missing piece is a **separate** authorized executor, not a hidden loop in this method.
- `Reject` always `AllowFixSend=false`.
- API has **no** flatten POST in the first-useful host (A06). Do not add one to “complete” this review.
- FIX worker still does not send. Do not add flatten `35=D` to prove a unit test.

---

## 10. Recommended later tests (names already reserved; do not implement here)

Pin **spec**, not the stub, and label any temporary stub locks `CURRENT_STUB`.

| Class (A27 / A48 / A71) | Must prove on this path |
|---|---|
| `Risk.KillSwitchEmergencyFlattenAuthorizationTests` | Distinct control; no dest close from enum flip alone |
| `Risk.KillSwitchEngineGlobalStopDoesNotFlattenTests` | Daily-loss → stop-new request only; flatten idle; dest unchanged |
| `Risk.OpenVsCloseExposurePolicyTests` | E2 / E8 / E10: close of linked dest APPROVE + sendable under stop-new; flatten-owned dest coalesces |
| `Risk.FlattenDoesNotRequireRealCopyFlagTests` | A25 §6.5 |
| `Risk.FlattenPersistsBeforeSendTests` | Intent + audit before builder |
| `Risk.FlattenSkipsUnknownPositionsTests` | `blocked_unknown`, no second NOS |
| `Risk.KillSwitchSeparationTests` | Two bits; `KillSwitchMode` not persisted as exclusive SoT |

Minimum engine facts that would have caught F02/F05 **without** a full executor:

```text
Close_under_EmergencyFlatten_does_not_set_AllowFixSend_until_family_conjunction_exists   # CURRENT_STUB
Close_under_StopNew_must_allow_send_when_real_recon_venue_ok                             # SPEC — today FAILS
Close_under_daily_loss_still_approves                                                    # SPEC — today FAILS
Open_under_EmergencyFlatten_is_EMERGENCY_FLATTEN_BLOCKS_NEW                              # stub name
Close_without_dest_link_is_not_AllowFixSend                                              # SPEC — today FAILS (C1)
```

---

## 11. What this artifact did not do

- Did not modify product source under `D:\Prop\src` or `D:\Prop\apps`.
- Did not add or change tests.
- Did not implement flatten, kill-switch state, dest links, or an API mutation.
- Did not treat B13 / C03 as a substitute for a flatten+close trace; those reviews are siblings. This file owns the **adversarial** flatten/close path only.
- Did not authorize auto-flatten, multi-account flatten, cancel-all+flatten, or live `35=D`.

**Bottom line:** `EmergencyFlatten` is a **label that blocks opens**. The close path is `APPROVE` + passthrough qty, then a send bit that is **false precisely when flatten or stop-new is on**, and **true** for an unmapped close when the enum is `None`. Loss/DD will still reject that close. There is no flatten run. Residual destination risk cannot be exited through this engine. The only reason that is not a live incident is that **nobody calls `Evaluate` and nobody sends FIX.**
