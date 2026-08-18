# D102 — Emergency flatten vs close (risk edges)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D102_risk_edges.md` |
| Agent | D102 (flatten vs close risk edges) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:47:10+05:30 |
| Assigned | Emergency flatten vs close. Write this file. Do not modify product source. |
| Product source edited | **No** |
| Test source edited | **No** |
| Binding law | Architecture **§35**, **§38**, **§40**, **§41**, **§53**, **§59**, **§62–§64**, **§68**, **§70.13**, **§72.18–19** |
| Sibling specs | **A23** §4.3 / §8.2, **A25** §6.5, **A48** (flatten run), **A49** §5, **A71** (CLOSE family + G04–G09 + §8.2 qty), A51 / A95 (RBAC / Risk DTO) |
| Prior measurements (same SUT hashes) | D13, D35, D70, C33, A48 §0, A71 §2 / §10 / §14 |
| Method | Re-read `RiskEngine.Evaluate`, `KillSwitchMode`, `CopyIntentAction`, close-path entities, dashboard/API/web, tests. Contrast **three** close-like operations the tree currently collapses. Quote line numbers. Nothing from memory. |

Classification: `LAW` / `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `UNSAFE` / `MISSING` / `DEAD` / `SAFE_BY_ABSENCE`.

This file is **not** a re-litigation of D70 (`STOP_NEW` vs `FLATTEN` as two kill levers). D70 asked whether those levers are distinct. D102 asks the next collision: **is `EMERGENCY_FLATTEN` the same thing as `CLOSE_EXPOSURE`?** It is not. Sharing the dest-close *class* does not make them one control.

---

## 0. Verdict

**Specified: three different close-like operations. Implemented: one reducing passthrough plus an exclusive enum label. Do not treat `KillSwitchMode.EmergencyFlatten` as a flatten. Do not treat `CopyIntentAction.CloseExposure` as flatten-or-source-close.**

| Operation | Trigger | Scope | Qty law | Auth | `REAL_COPY=false` | Owner of dest id |
|---|---|---|---|---|---|---|
| Source-driven `CLOSE_EXPOSURE` / `REDUCE_EXPOSURE` | MT5 reconstructed `ENTRY_OUT` / `OUT_BY` | **One** mapped dest id | Dest remaining × source fraction (A71 §8.2) | Copy pipeline (risk + persist-before-send) | **No** NOS in v1 (A71 G04) | Source link |
| Remainder flatten (`REMAINDER_FLATTENED`) | REDUCE would leave dest `< min_qty` | That **one** dest | Remainder = dest remaining | Same as source-driven close | Same as source-driven | Source link |
| `EMERGENCY_FLATTEN` | SuperAdmin + step-up + typed phrase | **Whole dest XAU book** (known ids, snapshotted once) | Dest remaining **exactly**. Ignore source lots | SuperAdmin only (A48 / A51). RiskManager **cannot** | **May** send reducing NOS (A25 §6.5 / A49 §5) | Flatten run (`flatten_targets`) |

Architecture §40: flatten **attempts to close destination positions**. Architecture §64: source close is a **risk-reduction copy action**. They share the dest-side *class* `CLOSE_EXPOSURE`. They do **not** share trigger, permission, book scope, quantity, send conjunction, or owner.

Current product (hashes in §1) has:

| Layer | What exists | Flatten vs close distinct? | Class |
|---|---|---|---|
| Architecture §40 / §64; A48; A71 §3.3 / §8.2 / §10 | Two owners, two qty laws, two send conjunctions | **Yes (law)** | `LAW` |
| `CopyIntentAction.CloseExposure` | One enum value used for both (and for remainder flatten) | **Labels only** | `EXISTS_AND_GOOD` as a class; **UNSAFE** as SoT of *which* close |
| `KillSwitchMode.EmergencyFlatten` | Exclusive mode; engine only blocks `IsIncreasing` | **No flatten effect** | `UNSAFE` if treated as a closer |
| `RiskEngine.Evaluate` | One `IsReducing` fall-through; reason `RISK_REDUCTION` | **No owner, no dest id, no coalesce** | `EXISTS_NEEDS_REFACTOR` + send **FAIL** |
| `AllowFixSend` | `Real && KillSwitch==None && Reconciled && VenueHealthy` | Flatten close and source close **both** fail the send bit | **UNSAFE** (spec inversion) |
| Loss / DD / daily-loss | Apply to **every** action (L117–124) | Both flatten CLOSE and source CLOSE freeze | **UNSAFE** |
| Flatten executor / run / targets | Absent | n/a | `MISSING` |
| `source_destination_links` / dest position entity | Absent (`Mt5Position` is **source** book) | Cannot size either close | `MISSING` |
| API flatten mutations | No `POST …/emergency-flatten*` | Correct **v1 absence** (A48 §11 / A57) | `MISSING` (safe omission) |
| Tests | One Open+Close fact; **zero** `EmergencyFlatten` | **No proof** | `MISSING` |
| Live send path | `Evaluate(` not registered; no NOS | n/a | `SAFE_BY_ABSENCE` |

**One-line answer:** emergency flatten is **not** “close with a louder name.” Source close is **not** “flatten one trader.” Remainder flatten is **not** a kill switch. The tree implements **none** of the three as a control. Setting `Mode = EmergencyFlatten` never closes a destination position; approving `CloseExposure` never proves a dest id exists.

Do **not** claim §40 flatten, §64 close policy, §68 “kill switch tested,” or A71 E6/E8/E10. Those boxes stay `[ ]`.

**Do not “fix” this by making every `CloseExposure` sendable under `EmergencyFlatten`.** That is the live-account landmine (C33 F02+F06 / D13-02+D13-10): unmapped, unclipped, possibly unknown qty, no confirm, double-close with a source event.

---

## 1. File identity (measured)

| Path | SHA-256 | Size | Lines | LastWriteUtc |
|---|---|---:|---:|---|
| `src\Domain\Risk\RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | 8567 B | 189 | 2026-08-18 07:38:10Z |
| `src\Domain\Enums\KillSwitchMode.cs` | `528429B0DF8023E3DAB465BC6C8D1C025DCE651EA31E11A2E8FA68DDE8BFBC82` | 140 B | 8 | 2026-08-18 07:36:08Z |
| `src\Domain\Enums\CopyIntentAction.cs` | `94BA143D84459E2DB8C04E5E9199A4D548443A5C4BF99C015046E995E22C7AF6` | 182 B | 10 | 2026-08-18 07:34:00Z |
| `src\Domain\Enums\RiskDecisionOutcome.cs` | `A0753C0FAA97261E1E26717AB3E6465F30C9F2D9024A3FF3675B1377C7D26951` | 206 B | 11 | 2026-08-18 07:34:04Z |
| `src\Domain\Entities\KillSwitch.cs` | `68EA2D92E88AD7CEFE37C20ADD56AEBA988E1A3D1424EF0D5EE45A961C2EEC4D` | 329 B | 12 | 2026-08-18 07:39:03Z |
| `src\Domain\Entities\CopyIntent.cs` | `C9AE3FF95058B72FC00A4DDBCBF2DFD68B7637D00D321244C376E2A1D6D9148B` | 951 B | 24 | 2026-08-18 08:10:10Z |
| `src\Domain\Entities\ExecutionIntent.cs` | `56DC9ED8E4DAC442A66620386864F919B34F851FF22974CA2FBC23B0A5CC3617` | 783 B | 21 | 2026-08-18 08:07:43Z |
| `src\Domain\Entities\RiskDecisionRecord.cs` | `C8FA95BF79339579B049CE74135052AED507C90E2055B350C0E7C8B1F728B4CE` | 457 B | 14 | 2026-08-18 07:39:03Z |
| `src\Domain\Entities\AuditLog.cs` | `FD5CD6EC5D18D3A0350891A21EF0B8E5FD33323035FD88ED06A95356B8EA4BA6` | 403 B | 12 | 2026-08-18 07:39:03Z |
| `src\Domain\Entities\Mt5Position.cs` | (source book only — not dest) | — | 22 | — |
| `src\Domain\Execution\ExecutionOrderStateMachine.cs` | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` | 2177 B | 56 | 2026-08-18 07:38:10Z |
| `src\Domain\Execution\QuantityNormalizer.cs` | `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` | 1041 B | 31 | 2026-08-18 07:38:10Z |
| `src\Domain\Execution\CopyIntentExpiry.cs` | `76B82E4F0C6F6B43988D5E50EE5E5D229CC451C7E8267AD6DF56271790531D38` | 246 B | 7 | 2026-08-18 07:38:10Z |
| `src\Domain\Shadow\ShadowCopyEngine.cs` | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | 3249 B | 91 | 2026-08-18 07:38:10Z |
| `src\Application\Dashboard\DashboardModels.cs` | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 3088 B | 114 | 2026-08-18 08:04:59Z |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 8708 B | 205 | 2026-08-18 08:05:15Z |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 5082 B | 140 | 2026-08-18 08:04:59Z |
| `src\Infrastructure\DependencyInjection.cs` | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 1900 B | 44 | 2026-08-18 07:44:18Z |
| `apps\api\Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 4731 B | 95 | 2026-08-18 08:05:15Z |
| `apps\api\appsettings.json` | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 1254 B | 50 | 2026-08-18 08:07:36Z |
| `apps\web\src\pages\RiskPage.tsx` | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` | 1148 B | 25 | 2026-08-18 07:46:43Z |
| `apps\web\src\types\index.ts` | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2905 B | 135 | 2026-08-18 07:38:18Z |
| `tests\Unit\RiskEngineTests.cs` | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | 2909 B | 87 | 2026-08-18 07:47:42Z |
| `docs\risk.md` | `26ACB40F63AFFB0F41042143ABAA9B3362B3653ED71F0CB64C529DC71BA510CE` | 2678 B | 68 | 2026-08-18 08:07:34Z |

`RiskEngine.cs` / `KillSwitchMode.cs` / `RiskEngineTests.cs` hashes match D13 / D35 / D70 / C33. No product change on this path since those recensus files.

`grep Evaluate(` under product `src\**\*.cs` hits **only** `RiskEngine.Evaluate` (definition). Tests call it. `AddTraderIntelligence` does **not** register `RiskEngine`, `IKillSwitch*`, `IFlattenExecutor`, or a close orchestrator.

Absent types this edge needs (A48 §15 / A71 / A74) — **none on disk**:

```text
Domain/Enums/FlattenPhase.cs
Domain/Risk/FlattenRun.cs
Domain/Risk/FlattenTarget.cs
Domain/Entities/SourceDestinationLink.cs
Domain/Entities/DestinationPosition.cs
Application/Risk/IFlattenExecutor.cs
```

`Mt5Position` is the **source** MT5 book (`Login`, `PositionTicket`, `VolumeNative`). Flatten and source-driven close both target **destination** cTrader positions. Closing `mt5_positions` would be a §40 / A48 anti-pattern (*never flatten source MT5*).

---

## 2. Binding law — they share a class, not an identity

### 2.1 Architecture (verbatim)

§40 (`Architecture_v2.md` L1542–1560):

```text
STOP_NEW_EXECUTION
  prevents new copy orders but leaves existing positions untouched.

EMERGENCY_FLATTEN
  attempts to close destination positions and therefore requires
  stronger authorization/confirmation.

Do not conflate them.
```

§64 (`Architecture_v2.md` L2379–2392):

```text
Closing an existing copied position is a risk-reduction action and may
deserve different treatment from new entries.

OPEN_EXPOSURE / INCREASE_EXPOSURE / REDUCE_EXPOSURE / CLOSE_EXPOSURE
```

§64 is **source-close handling**. It does not authorize a book-wide market blast. Flatten is the §40 run that *emits* `CLOSE_EXPOSURE` intents (A48 §3.2, A71 §3.3).

### 2.2 Four levers, two close owners (A49 §5 / A71 §10)

| Lever | New copy `35=D` | Dest book | Source-driven reduce/close `35=D` | Flatten `CLOSE_EXPOSURE` |
|---|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED=false` | blocked | untouched | **No** NOS in v1 | **May** send (A25 §6.5) |
| `STOP_NEW_EXECUTION` | blocked | untouched | Allowed by default | Allowed (flatten is not “new copy”) |
| `EMERGENCY_FLATTEN` active | blocked | close **attempted** | **Coalesce** if flatten owns that dest id | The flatten orders themselves |
| Engine `GLOBAL_STOP` | engages stop-new | untouched | **Must still approve** mapped close | **Must not auto-fire** |

D70 owns the first two rows as *distinct kill levers*. This file owns the last two columns: **who is allowed to emit the reducing `35=D`, and with what qty, on which dest id.**

### 2.3 Quantity is the first identity test (A71 §8.2)

```text
source-driven CLOSE:
    dest_qty = floor_to_step(dest_remaining * source_frac)

remainder flatten (G32):
    dest_qty = dest_remaining          # leftover < min_qty

emergency flatten:
    dest_qty = dest_remaining exactly  # ignore source lots entirely
```

`QuantityNormalizer.Normalize(sourceLots * allocationFactor)` is an **OPEN-family** function. Using it for either close is a §38 / §64 defect.

### 2.4 Reason codes that must stay separate

| Situation | Binding code | Engine today |
|---|---|---|
| Flatten run in progress, increasing action | `EMERGENCY_FLATTEN_ACTIVE` (A23 / A48 / A71) | `EMERGENCY_FLATTEN_BLOCKS_NEW` (L82) — adjacent, not the catalog name |
| Flatten owns this dest; source close arrived | `FLATTEN_OWNS_POSITION` (A71 G06) | **MISSING** — fall-through `RISK_REDUCTION` |
| Source close, no dest link | `NO_DESTINATION_POSITION` (A71 E6) | **MISSING** — `APPROVE` + passthrough qty |
| REDUCE leftover `< min` | `REMAINDER_FLATTENED` (info) | **MISSING** |
| Source-driven reduce/close approved | `RISK_REDUCTION` is acceptable | Emitted for **any** reducing action, including a would-be flatten close |
| Stop-new, increasing | `STOP_NEW_EXECUTION` | `STOP_NEW_EXECUTION` (L79) |

---

## 3. Measured product — one reducing pipe

### 3.1 The only flatten/close sites in `Evaluate`

```78:82:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");
```

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

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

`CopyIntent` has `Action` + `RequestedQuantity`. **No** dest link, **no** `flatten_run_id`, **no** remaining dest qty. `ExecutionIntent` has no dest position id and no exposure class. The engine cannot tell flatten CLOSE from source CLOSE from remainder flatten.

### 3.2 Request fields the spec needs and the request does not have

```text
linked_destination_position_id
dest_remaining_qty
dest_side
flatten_run_id / flatten_phase / flatten_owner(this dest id)
unknown_on_this_dest_id
TRADE logged on / lease owned          -- only VenueHealthy
stop_new_execution  (independent bit)
allow_risk_reduction_while_stop_new
close_kind          -- source | remainder | emergency_flatten
```

Without `close_kind` + dest identity, every close approval is an opinion about the caller’s snapshot.

### 3.3 Close-family trace (what actually fires)

| # | Gate | Source CLOSE | Flatten CLOSE | Required |
|---:|---|---|---|---|
| 1–2 | Kill `IsIncreasing` | skipped | skipped — flatten **does not own** the close | Flatten run should **emit** the close, not wait for this `if` |
| 3–4 | Recon / venue health (`IsIncreasing`) | skipped | skipped | Per-id unknown still blocks (G09) |
| 5 | empty `Real==false && Action!=Close` | condition false | same | Dead. Comment is a foot-gun |
| 6–9 | quote / spread / move / signal | skipped | skipped | Directionally OK as **entry**-guard skip (A23 §8.2) |
| 10–12 | loss / daily-loss / DD | **YES — rejects** | **YES — rejects** | **Must allow** (A71 G21–G23 / E8) |
| 13–19 | caps / martingale | skipped | skipped | Directionally OK |
| 20 | `allowSend` | requires `None` + `Real` | requires `None` + `Real` | Source close: stop-new may be on. Flatten: `REAL_COPY` may be off; flatten-active is **required** |
| 21 | `IsReducing` | `APPROVE` + passthrough qty | same — **no dest snapshot** | Flatten must not use this fall-through as the closer |

Setting `KillSwitch = EmergencyFlatten` and sending `Action = CloseExposure` is **not** flatten. It is “block new + approve a caller-invented close that cannot send.”

---

## 4. Risk-edge catalog

Each edge is a place a later coder, operator, or threshold can confuse flatten with close. “Live effect” is what would happen **if** a worker honored `AllowFixSend`. Today the worker never sends — recorded as `SAFE_BY_ABSENCE`, not a pass.

### RE-01 — Shared `CloseExposure` class without owner

**Edge:** One C# token (`CopyIntentAction.CloseExposure = 3`) is the dest-side class for source close, remainder flatten, **and** emergency flatten (A71 §3.3).

**Hazard:** A single `if (Action == CloseExposure)` will apply the wrong send conjunction (G04: source close needs `REAL_COPY`; flatten does not) and the wrong qty (source fraction vs dest remaining).

**Measured:** `IsReducing` unions Reduce+Close. No `flatten_run_id`. Reason always `RISK_REDUCTION`.

**Class:** `UNSAFE` as SoT of *which* close. The enum value itself is `EXISTS_AND_GOOD`.

### RE-02 — Exclusive enum erases the in-progress pair

A48 invariant: flatten request **forces stop-new ON**. Required in-progress state is `{stop-new ON} × {flatten ACTIVE}`.

`KillSwitchMode` is one column. Persisting `EmergencyFlatten` **erases** `StopNewExecution`. Completing flatten by writing `None` **clears stop-new** — the explicit A48 anti-pattern.

Source-driven close under that pair is the **normal** residual-risk path (G07: unowned dests still closeable; G06: owned dests coalesce). It is **unrepresentable**.

**Class:** `UNSAFE`. Owned as *state shape* by D70; restated here because **source close cannot be licensed** without the pair.

### RE-03 — `AllowFixSend` requires `KillSwitch == None`

```text
allowSend ⇔ RealExecutionEnabled ∧ KillSwitch==None ∧ Reconciled ∧ VenueHealthy
```

| Cell | Real | Kill | Action | Code send | Spec source-close | Spec flatten CLOSE |
|---|---|---|---|---|---|---|
| C1 | T | None | Close | **true** | true **if dest known** | n/a (idle) |
| C2 | T | StopNew | Close | **false** | **true** (default) | n/a |
| C3 | T | Flatten | Close | **false** | coalesce or true | **true** |
| C4 | F | Flatten | Close | **false** | false (v1) | **true** (A25 §6.5) |
| C5 | T | None | Close | n/a (rejected if daily −2000) | **true** | **true** |

C1 is the **only** close cell that can set send true, and it has **no dest identity** (RE-07). C3/C4 are the cells flatten exists for; both are false.

`RiskEngineTests.Stop_new_execution_blocks_opens_not_closes` **pins C2** (`AllowFixSend=false`). Implementing A71 §10 **breaks that fact** (D35).

**Class:** `UNSAFE`. Test-locked.

### RE-04 — `REAL_COPY` exception is flatten-only

A25 §6.5 / A49 §5 / A71 G04:

- Flatten CLOSE **may** send reducing NOS with the copy flag false (exit, not new copy).
- Source-driven live close in v1 **still needs** the flag.
- Shadow never sends.

The empty `if` (L90–93) special-cases `CloseExposure` in the *condition* and does nothing. A reader can conclude “close is exempt from the flag.” The send bit still requires `RealExecutionEnabled` for **all** actions.

`apps\api\appsettings.json` `FeatureFlags:LiveCopyEnabled=false` and dashboard `RealCopyEnabled` hardcoded `false` are the **correct** floor. They are not a flatten exception.

**Class:** `MISSING` exception. Empty `if` is a foot-gun (`EXISTS_NEEDS_REFACTOR`).

### RE-05 — Red day freezes **both** exits (A71 E8 inverted)

L117–124 apply to every action. Default caps: trader loss `500`, daily `2000`, DD `3000`.

On the day you are already losing:

- Source full close of a mapped dest → `GlobalStop` / `MAX_DAILY_EXECUTION_LOSS` / qty 0.
- Flatten CLOSE of the same dest → same reject.

A71 G21–G23 / E8: daily-loss **raises stop-new**; mapped CLOSE **APPROVE**s. A48 §5.3: engine `GLOBAL_STOP` **never** requests flatten — and must not **block** flatten or source-close either.

This is the highest-severity *logic* defect on the path. Residual dest risk is trapped exactly when the desk needs it gone.

**Class:** `UNSAFE`.

### RE-06 — Missing coalesce → double-close

A48 §7 / A71 G06 / classifier §4.2: flatten is the **exclusive closer** of snapshotted dest ids. Source CLOSE of an owned dest **coalesces** (`FLATTEN_OWNS_POSITION`; attach `source_event_id`; **no second NOS**).

Engine: no dest id, no `flatten_owner`. Two callers can both get `Approve` + passthrough qty.

Naïve send fix (drop `KillSwitch==None` from `allowSend`) makes **both** intents sendable → two reducing `35=D` on one dest (partial fill + overshoot / `EXECUTION_STATE_UNKNOWN`).

G07 is the complement: REDUCE/CLOSE of dests flatten does **not** own stay allowed. Engine cannot distinguish G06 from G07.

**Class:** `MISSING` coalesce. `UNSAFE` under the naïve send fix.

### RE-07 — Phantom close (A71 E6)

Source `ENTRY_OUT` full, **no** dest link. Engine approves `RequestedQuantity` and, in C1, can set `AllowFixSend=true`.

Spec: persist terminal `NO_DESTINATION_POSITION`, **zero** FIX. Do not invent a dest so a late source close has something to hit. Do not flatten source MT5 to “satisfy” the close.

If anyone wires C1 to a builder, this is a close of a position that does not exist (venue reject, or a close of the **wrong** netting slot).

**Class:** `UNSAFE` once send exists. `SAFE_BY_ABSENCE` today.

### RE-08 — Three qty laws, one passthrough

`ApprovedQuantity = request.RequestedQuantity`. No dest remaining, no min/step, no clip. `QuantityNormalizer` is allocation-only and is **never called**.

| Input | Spec | Engine |
|---|---|---|
| Flatten of dest 1.20 | qty `1.20` dest units | whatever the caller stuffed in `RequestedQuantity` |
| Source CLOSE 50 lots vs dest 0.10 | clip to `0.10`, class CLOSE | approve `50` |
| REDUCE leaving dest `< min` | promote CLOSE remainder | approve the partial; orphan remainder |
| `RequestedQuantity = 0` or negative | reject | approve |

**Class:** `UNSAFE`.

### RE-09 — Remainder flatten ≠ emergency flatten

A71 G32 / §7.6: if a source REDUCE would leave dest `< min_qty`, **promote the class** to `CLOSE_EXPOSURE` and flatten **that remainder**. Info code `REMAINDER_FLATTENED`.

This is a **qty promotion** on one mapped dest. It:

- does **not** start an `EMERGENCY_FLATTEN` run
- does **not** force stop-new
- does **not** require SuperAdmin
- still needs `REAL_COPY` in v1
- must not be logged / dashboarded as flatten-active

Naming collision is the edge. A dashboard that lights “flatten” on G32 will panic the desk. An executor that treats G32 as a book-wide run will close every dest.

**Class:** `MISSING` promotion. `UNSAFE` if later aliased to `KillSwitchMode.EmergencyFlatten`.

### RE-10 — `docs/risk.md` auto-flatten on “total loss”

```34:37:D:\Prop\docs\risk.md
Daily loss breach   → Kill Switch ON (positions stay)
Total loss breach   → Emergency Flatten → then Kill Switch ON
Operator action     → Either, depending on button pressed
```

A23 §8.3 / A48 §14 `[DO NOT]`: daily-loss / drawdown / “total loss” engage **stop-new only**. Auto-flatten from a threshold is unauthorized. The same page says flatten “Closes all open positions immediately at market” — architecture: **known dest ids only**, persist-before-send, no blind market blast, never source MT5.

Do **not** implement `docs/risk.md` L34–37. The one-pager’s *labels* (§18–32) are closer to law than its *trigger table*.

**Class:** `UNSAFE` documentation.

### RE-11 — `GLOBAL_STOP` outcome is not flatten (good) and not a latch (bad)

S05/RE-05 returns `RiskDecisionOutcome.GlobalStop` and **forgets** it. No `IKillSwitchCommands.ActivateStopNew`. No audit row.

Positive: the engine **cannot** auto-flatten from a threshold (A48). Negative: the same return **is** the thing that freezes the close.

**Class:** `EXISTS_NEEDS_REFACTOR` (no latch) + `UNSAFE` (exit freeze).

### RE-12 — Scope: dest book vs one mapped dest vs source MT5

| Book | Source close | Remainder flatten | Emergency flatten |
|---|---|---|---|
| Dest execution XAU (known id) | That id only | That id only | **All** snapshotted known dest ids |
| Dest id opened **after** flatten snapshot | Still source-closeable if stop-new leaked (should not) | n/a | Reconciliation issue, **not** a silent extra target (A48 §12.2) |
| Source MT5 | Never | Never | **Never** |
| Shadow | Shadow analog (A24); no live NOS | Shadow remainder | Test-only `SHADOW_BOOK_FLATTEN`; not live |

`Mt5Position` + `FakeMt5BrokerConnector.GetPositionsAsync` are the **source** book. A “flatten all positions” helper that iterates `Mt5Positions` would close the wrong venue.

**Class:** `MISSING` dest book. Source entity present is a **scope trap**.

### RE-13 — Authorization is not the copy pipeline

| Who | Source close | Emergency flatten |
|---|---|---|
| Reconstruction + risk + FIX worker | Yes (mapped dest) | **Never** auto |
| RiskManager | May activate stop-new | **No** (A48 / A51 supersede A26 RiskManager `W`) |
| SuperAdmin + step-up + phrase + single-use 90s token | Not required | **Required** |
| System `risk-engine` | May raise stop-new | **Never** |

No flatten POST. No policies `Risk.Flatten.Write`. `AuditLog` exists; nothing writes `KILL_SWITCH.FLATTEN.*`. Correct **v1 omission** (A48 §11: 409 until Phase 8). Not an implementation.

`appsettings.json` `RiskEngine:EmergencyFlattenApiKey=""` is **not** A48 step-up. Settings GET does not even expose it. A shared API key as flatten auth is an anti-pattern (A48: SuperAdmin + TOTP/password, hash-only token).

**Class:** `MISSING` (authorized). `SAFE_BY_ABSENCE` (no button). `UNSAFE` documentation key.

### RE-14 — Unknown-on-this-id vs global `Reconciled`

A71 G08–G09: CLOSE of a **known** dest with **no** unknown on that id may proceed even if the **global** book is not `READY`. Unknown-on-this-id is **always** blocking (do not “fix” with a second close — §34). Flatten of unknowns is how you get a second disaster during an ack gap (A48 §2).

Engine: `!Reconciled` does not reject reduce/close (good direction for G08), then `allowSend` requires global `Reconciled` (blocks legitimate flatten of known ids), and there is **no** per-id unknown bit (C1 would send on a globally-green book with one unknown 11).

**Class:** `UNSAFE` (missing per-id). Global send gate is a blunt fail-closed that also **blocks** flatten.

### RE-15 — TRADE down vs QUOTE down

Flatten / source-close need **TRADE** + lease. QUOTE down is a waterfall / wait, not an OPEN-style reject (A71 G10–G11). Flatten mid-run: **pause sending**, keep phase `active`, alert — do not fail remaining targets (A48 §12.5).

Engine has one `VenueHealthy` bool. Close skips the reject, then `allowSend` requires that same bool. Cannot pause-vs-reject. Cannot tell QUOTE-down (still flatten) from TRADE-down (must not blast).

**Class:** `EXISTS_NEEDS_REFACTOR`.

### RE-16 — Reversal CLOSE leftover vs flatten-all

A71 E5: `ENTRY_INOUT` sell 1.80 vs dest long 1.20 → **CLOSE 1.20 then OPEN leftover**. Under stop-new + rotten quote: close APPROVE, open REJECT, dest **flat**. That leftover OPEN is **not** flatten.

Engine has no classifier and no dest remaining. A single `CloseExposure` of `1.80` would `Approve` qty `1.80` (RE-08) — over-close, not flatten.

**Class:** `MISSING` classifier. Over-close if fed source volume.

### RE-17 — Shadow `SimulateExit` is not live flatten

`ShadowCopyEngine.SimulateExit` prices an exit from a quote. It does **not** call `RiskEngine`. It does not persist a flatten run. It does not send FIX. Using it as a “flatten preview” that later becomes a live blast is a phase-skip.

**Class:** `SAFE_BY_ABSENCE` as live. `SHADOW BYPASS` of risk (D13).

### RE-18 — `MayRetry(Rejected)` is a flatten storm

```35:36:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;
```

A23 §8.2 / A48 §12.7: flatten failure is an **alert**; unknown after send → §34 recovery only; **one** replacement `cl_ord_id` after proof the first never landed. Retrying `Rejected` is a storm. Same hole applies to source-driven close of a dest that just rejected.

`AfterDisconnectWithUnknownAck` correctly requires reconciliation and is **not** in `MayRetry`. Keep that. Do not hang flatten on `MayRetry(Rejected)`.

**Class:** `UNSAFE` if flatten is later hung on this FSM unchanged.

### RE-19 — Operator surface lies

| Layer | Flatten vs close signal |
|---|---|
| DB `kill_switches.Mode` | exclusive enum, seeded `None` |
| `RiskDashboardDto.KillSwitch` | that enum’s `.ToString()` — **one string** (§40 / A95 violation) |
| `RiskDashboardDto.RealCopyEnabled` | hardcoded `false` |
| Web `RiskStatus` type | `stopNewExecution` + `emergencyFlatten` **booleans** (A95: a flatten bool is itself a conflation) |
| Web `RiskPage` | renders `data.killSwitch` (the C# string). Does **not** read the two bools |
| Close / dest residual | **not displayed** |

Operator can believe flatten is *on* while residual dest risk remains (C33 S01). Frontend types advertise two bits the API does not send.

**Class:** `UNSAFE` as an operator control surface.

### RE-20 — Fail-open seed

```115:122:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.KillSwitches.Add(new KillSwitch
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
            Mode = KillSwitchMode.None,
            ...
        });
```

A48 §3.3: missing / fresh row → treat stop-new as **ON**. Seeded `None` is fail-**open** for new copy. Dashboard missing-row path is the same (`ks?.Mode ?? None`). Live send is currently `SAFE_BY_ABSENCE`, not because the latch is closed.

**Class:** `UNSAFE` as SoT. `SAFE_BY_ABSENCE` at send.

### RE-21 — Caller-trusted snapshot

Every gate is a field the **caller** sets. No `IKillSwitchQuery` re-read (A48 §5.2). A buggy worker can pass `KillSwitch=None`, `Reconciled=true`, `VenueHealthy=true`, `RealExecutionEnabled=true`, `Action=CloseExposure`, `RequestedQuantity=99` and receive `AllowFixSend=true`.

Risk approval is specified as **not** a capability token (A49 §5). Here it is the only token.

**Class:** `UNSAFE` once on a send path. `DEAD` today.

### RE-22 — Positions after the flatten snapshot

A48 §12.2: snapshot **once** at `START`. Positions opened after the snapshot cannot exist if stop-new + flatten-active are honored; if they appear (external / leaked OPEN), they become a **reconciliation issue**, not silent extra flatten targets, and not a source-driven close that races the run.

Engine has no snapshot and no dest book. Cannot implement this edge.

**Class:** `MISSING`.

### RE-23 — Abort flatten vs in-flight source close

A48 abort: cancel `queued` / `intent_persisted` only. Leave `sent` / `unknown` to §34. A source close that arrived mid-run and was coalesced must **not** be “aborted” into a second NOS, and must **not** be dropped so residual dest stays open with no owner.

No run, no abort, no coalesce.

**Class:** `MISSING`.

### RE-24 — Config keys that invite one-RPC flatten

Unread / overlapping (`apps\api\appsettings.json` L35–42):

```text
RiskEngine:KillSwitchEnabled = true
RiskEngine:KillSwitchOn = false
RiskEngine:StopNewExecutionOn = false
RiskEngine:EmergencyFlattenApiKey = ""
```

Settings GET exposes only `KillSwitchEnabled`. PUT does not write kill/flatten. `.env.example` is **absent** on this worktree (D62). A65 historically copied `RISK_EMERGENCY_FLATTEN_ENABLED=true` — default-true “flatten enabled” is the wrong polarity (A48: availability is **computed**).

**Class:** `UNSAFE` documentation. Dead as runtime.

### RE-25 — Tests do not constrain the edge

`RiskEngineTests`: 5 facts. `EmergencyFlatten` **never** appears. `IncreaseExposure` / `ReduceExposure` never appear. `AllowFixSend=true` never appears.

A27 / A48 required classes **not on disk**:

```text
Risk.KillSwitchEmergencyFlattenAuthorizationTests
Risk.OpenVsCloseExposurePolicyTests
Risk.FlattenSkipsUnknownPositionsTests
Risk.FlattenPersistsBeforeSendTests
Risk.FlattenDoesNotRequireRealCopyFlagTests
Domain.KillSwitchSeparationTests
```

The one close fact **locks RE-03**. Flatten vs close has **zero** facts.

**Class:** `MISSING`.

---

## 5. What is *not* the same vs what *is*

### 5.1 Distinct today (vocabulary only)

- Two enum **names** (`EmergencyFlatten` vs `CloseExposure`).
- Two engine **reason strings** for the *open* family (`EMERGENCY_FLATTEN_BLOCKS_NEW` vs `STOP_NEW_EXECUTION`).
- Architecture / A48 / A71 **text**.
- First-useful policy correctly **omits** flatten mutation (A57 / A63). Absence of flatten POST is not a distinctness win; it is the right v1 cut.
- Flatten-in-progress does **not** auto-fire dest closes from `Evaluate`. Auto-flatten from a threshold is forbidden; the missing piece is a **separate** authorized executor.

### 5.2 Not distinct today (owner / qty / send / proof)

- Persisted state (one `Mode`).
- Risk request (one `KillSwitch` + one `Action` with no `close_kind`).
- Send license (`== None` ∧ `Real`).
- Qty (caller `RequestedQuantity`).
- Dest identity (none).
- Coalesce (none).
- Operator verbs / RBAC / audit.
- Tests (flatten = 0 facts; close fact pins the send defect).
- Docs trigger table (auto-flatten on total loss).

### 5.3 What a later coding wave must **not** do

```text
[DO NOT] Treat KillSwitchMode.EmergencyFlatten as “close everything”
[DO NOT] Make AllowFixSend=true for every CloseExposure when mode==EmergencyFlatten
[DO NOT] Use QuantityNormalizer / source lots to size flatten
[DO NOT] Iterate Mt5Positions (source) as flatten targets
[DO NOT] Auto-flatten from daily loss / DD / docs/risk.md “total loss”
[DO NOT] Alias G32 remainder flatten to the emergency flatten run
[DO NOT] Let source CLOSE emit a second NOS on a flatten-owned dest
[DO NOT] Require KillSwitch==None for source-driven close send
[DO NOT] Require REAL_COPY for flatten CLOSE (A25 §6.5)
[DO NOT] Skip REAL_COPY for v1 source-driven close
[DO NOT] Ship POST …/emergency-flatten before Phase 8 + TRADE reconcile
[DO NOT] Give RiskManager flatten write (A26 is stale; A48/A51 win)
[DO NOT] Hang flatten on MayRetry(Rejected)
[DO NOT] Flatten source MT5 or shadow-as-live
[DO NOT] Invent a dest so a late source close has something to hit
```

Minimum distinctness bar (not implemented here):

1. `close_kind ∈ {source, remainder, emergency_flatten}` on the intent / risk request.
2. Dest id + remaining qty **required** for every reducing approval.
3. Flatten run (phase machine) independent of the stop-new bit (A48). Flatten **emits** `CLOSE_EXPOSURE` rows with `flatten_run_id`; it does not flip `Mode` and hope.
4. `AllowFixSend` **per kind** (A71 §10 matrix). Source close sendable under stop-new. Flatten sendable with `REAL_COPY=false`. Both still need TRADE + lease + known dest + persist-before-send + no unknown-on-this-id.
5. Coalesce G06 / allow G07.
6. Loss/DD raise stop-new only; never reject mapped CLOSE; never start a flatten run.
7. Tests that fail if someone aliases them: flatten request does not emit dest closes without confirm; source CLOSE of owned dest does not emit a second `cl_ord_id`; remainder flatten does not set `flattenPhase=active`; `GLOBAL_STOP` dest book unchanged.

---

## 6. Go-live / first-useful boxes this edge owns

All remain **unchecked**. Evidence is vocabulary, not a control.

```text
[ ] STOP_NEW_EXECUTION does not flatten          -- true by omission; not proven
[ ] EMERGENCY_FLATTEN permission is distinct
[ ] GLOBAL_STOP engages stop-new only            -- outcome exists; latch + exit-allow missing
[ ] Flatten unknown-state does not blindly resend
[ ] Source CLOSE of flatten-owned dest coalesces
[ ] Source CLOSE without dest link is NO_DESTINATION_POSITION
[ ] Flatten qty = dest remaining (not source lots)
[ ] Remainder flatten ≠ emergency flatten
[ ] Kill switch tested (§68 / A100 G16)
[ ] Global stop-new-orders works (§70.13)
```

A101 item 13 proof (when someone codes stop-new): dest positions unchanged, **no** `35=F/G`, `EmergencyFlatten` **must not** flip on. Do **not** implement live flatten to pass stop-new. Do **not** implement live flatten to pass “close works.”

---

## 7. Honesty

- This file **did not** modify product source under `D:\Prop\src` or `D:\Prop\apps`.
- Live book is protected because **nothing sends**, not because flatten or close works.
- D70 answered “are stop-new and flatten distinct?” **No** in the tree. D102 answers “is flatten the same as close?” **No** in the law; **yes they are collapsed** in the tree.
- C33 S01–S20 remain valid against these hashes. This report adds the **three-way** taxonomy (source close / remainder flatten / emergency flatten) and the **RE-*** collision list a coder will hit first.
- A naïve change that sets `AllowFixSend=true` under `EmergencyFlatten` would authorize blind closes of unmapped, unclipped qty with no confirm (C33). Distinctness is not “make flatten send.”
- Do not implement `docs/risk.md` L34–37.

**Bottom line:** `EMERGENCY_FLATTEN` is a SuperAdmin persist-before-send **run** that emits dest-remaining `CLOSE_EXPOSURE` for known destination ids. Source-driven `CLOSE_EXPOSURE` is a copy-pipeline exit of **one** mapped dest. Remainder flatten is a **qty promotion**. The product has one reducing fall-through and a kill-mode label that never closes anything. Never one close path.

---

*End of D102. Product source was not modified.*
