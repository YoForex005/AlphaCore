# D70 — Are `STOP_NEW` and `FLATTEN` distinct?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D70_kill.md` |
| Agent | D70 (kill-switch distinctness) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:42:01+05:30 |
| Assigned | `STOP_NEW` vs `FLATTEN` distinct? Write this file. Do not modify product source. |
| Product source edited | **No** |
| Test source edited | **No** |
| Binding law | Architecture **§40**, **§41**, **§53**, **§59**, **§64**, **§68**, **§70.13**, **§72.18–19** |
| Sibling specs | A23 §8, **A48** (authoritative design), A49 §5, A51 §5.2, A71, A95, A100 G16, A101 item 13 |
| Prior measurements (same SUT hashes) | D13, D35, C33, A48 §0, C18 |
| Method | Re-read architecture §40, A48, current `KillSwitchMode` / `KillSwitch` / `RiskEngine` / dashboard / API / web / tests. Quote line numbers. Nothing from memory. |

Classification: `LAW` / `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `UNSAFE` / `MISSING` / `DEAD` / `SAFE_BY_ABSENCE`.

---

## 0. Verdict

**Specified: YES — two independent controls. Implemented: NO — one exclusive mode. Do not treat the tree as §40-compliant.**

Architecture §40 names `STOP_NEW_EXECUTION` and a separately permissioned `EMERGENCY_FLATTEN`, then says **do not conflate them**. A48 restates the invariant: at any instant the system may be any combination of `{stop-new off\|on} × {flatten idle\|confirm-pending\|active\|partial-failed}`. A mutually exclusive `KillSwitchMode` used as *the* persisted state is **the same §40 violation as a single `bool killSwitch`**, wearing an enum.

Current product (hashes below) has:

| Layer | What exists | Distinct? | Class |
|---|---|---|---|
| Architecture §40 / A48 / A23 §8 | Two names, two effects, two permissions, two reason codes | **Yes (law)** | `LAW` |
| `KillSwitchMode` | `None \| StopNewExecution \| EmergencyFlatten` | **No — exclusive** | `UNSAFE` if treated as SoT |
| `KillSwitch` entity / `kill_switches` | One `Mode` column | **No** | `UNSAFE` / `EXISTS_NEEDS_REFACTOR` |
| `RiskEngine.Evaluate` | Two `if`s, two reason strings; then `AllowFixSend` requires `None` | **Labels yes, state no** | `EXISTS_NEEDS_REFACTOR` + send **FAIL** |
| Flatten executor / run / targets | Absent | n/a | `MISSING` |
| API mutations | No `POST …/stop-new-execution`, no `POST …/emergency-flatten` | n/a | `MISSING` |
| Risk GET | One `string KillSwitch` | **No** | `UNSAFE` stub |
| Risk page | One MetricCard `"Kill switch"` | **No** | `EXISTS_NEEDS_REFACTOR` |
| Web TS types | Two bools (`stopNewExecution`, `emergencyFlatten`) | **Shape yes, unused** | DTO **mismatch** vs C# |
| Tests | One Open+Close fact; **zero** `EmergencyFlatten` | **No proof** | `MISSING` coverage |
| Live send path | `Evaluate(` not registered; no NOS | n/a | `SAFE_BY_ABSENCE` |

**One-line answer:** they **must** be distinct; they **are not** independently representable in the current tree. Stop-new does not flatten (by omission — the engine never mutates a book). Flatten does not flatten (the enum value only blocks new opens). `{stop-new ON + flatten ACTIVE}` cannot be stored.

Do **not** claim §40 / §68 “kill switch tested” / §70.13 “global stop-new-orders works.” Those boxes stay `[ ]` (A100 G16, A101 item 13).

---

## 1. File identity (measured)

| Path | SHA-256 | Size | Lines | LastWriteUtc |
|---|---|---:|---:|---|
| `src\Domain\Enums\KillSwitchMode.cs` | `528429B0DF8023E3DAB465BC6C8D1C025DCE651EA31E11A2E8FA68DDE8BFBC82` | 140 B | 8 | 2026-08-18 07:36:08Z |
| `src\Domain\Entities\KillSwitch.cs` | `68EA2D92E88AD7CEFE37C20ADD56AEBA988E1A3D1424EF0D5EE45A961C2EEC4D` | 329 B | 12 | 2026-08-18 07:39:03Z |
| `src\Domain\Risk\RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | 8567 B | 189 | 2026-08-18 07:38:10Z |
| `src\Domain\Enums\CopyIntentAction.cs` | `94BA143D84459E2DB8C04E5E9199A4D548443A5C4BF99C015046E995E22C7AF6` | 182 B | 10 | 2026-08-18 07:34:00Z |
| `src\Application\Dashboard\DashboardModels.cs` | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 3088 B | 114 | 2026-08-18 08:04:59Z |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 8708 B | 205 | 2026-08-18 08:05:15Z |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 5082 B | 140 | 2026-08-18 08:04:59Z |
| `apps\api\Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 4731 B | 95 | 2026-08-18 08:05:15Z |
| `apps\api\appsettings.json` | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 1254 B | 50 | 2026-08-18 08:07:36Z |
| `apps\web\src\pages\RiskPage.tsx` | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` | 1148 B | 25 | 2026-08-18 07:46:43Z |
| `apps\web\src\types\index.ts` | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2905 B | 135 | 2026-08-18 07:38:18Z |
| `tests\Unit\RiskEngineTests.cs` | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | 2909 B | 87 | 2026-08-18 07:47:42Z |
| `docs\risk.md` | `26ACB40F63AFFB0F41042143ABAA9B3362B3653ED71F0CB64C529DC71BA510CE` | 2678 B | 68 | 2026-08-18 08:07:34Z |

`RiskEngine.cs` / `KillSwitchMode.cs` / `KillSwitch.cs` / `RiskEngineTests.cs` hashes match D13 / D35 / C33. No product change since those recensus files.

`grep Evaluate(` under product `*.cs` hits **only** `RiskEngine.Evaluate` and `tests\Unit\RiskEngineTests.cs`. `AddTraderIntelligence` does **not** register `RiskEngine`, `IKillSwitch*`, or a flatten executor.

Absent types (A48 §15 recommended map — **none on disk**):

```text
Domain/Enums/KillSwitchControl.cs
Domain/Enums/FlattenPhase.cs
Domain/Risk/KillSwitchState.cs
Domain/Risk/FlattenRun.cs
Application/Risk/IKillSwitchQuery.cs
Application/Risk/IKillSwitchCommands.cs
Application/Risk/IFlattenExecutor.cs
```

---

## 2. Binding law — they are distinct

### 2.1 Architecture §40 (verbatim contract)

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

Source: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §40 (L1542–1560).

### 2.2 Adjacent clauses that force two objects, not two labels

| Clause | Obligation |
|---|---|
| §41 | `REAL_COPY_EXECUTION_ENABLED` is a **third** control. Not an alias of either kill lever (A49 §5). |
| §53 | Risk dashboard must show **both** `STOP_NEW_EXECUTION` **state** and `EMERGENCY_FLATTEN` **availability** (L1976–1977). One chip is a display violation. |
| §59 | Separate authorized verbs: `activate stop-new-orders` vs `request emergency flatten`. All audited. |
| §64 / §72.18 | Reduce/close ≠ open-more. Flatten is `CLOSE_EXPOSURE`. Stop-new must not freeze exits. |
| §39 / A23 §8.3 | Engine `GLOBAL_STOP` **engages stop-new only**. Auto-flatten from a threshold is unauthorized. |
| §68 | `[ ] kill switch tested` — A100 G16: pass only when stop-new proven **not** to flatten **and** flatten is a distinct authorized path. |
| §70.13 | `[ ] Global stop-new-orders works` — dest book untouched; `EmergencyFlatten` must **not** flip on as a side effect (A101 §16.3). |
| §72.19 | Every manual override audited. |

### 2.3 A48 definition (binding design; not implemented)

| Control | Kind | Effect | Must not |
|---|---|---|---|
| `STOP_NEW_EXECUTION` | Durable 2-state **latch** | Blocks `OPEN_EXPOSURE` / `INCREASE_EXPOSURE`. Dest book **untouched**. Reduce/close allowed by default (`allow_risk_reduction_while_stop_new=true`). | Close, reduce-all, cancel-all, send flatten `35=D`, flip `REAL_COPY`. |
| `EMERGENCY_FLATTEN` | Separately permissioned **run** (phase machine, not a bool) | Snapshot known dest ids → persist `CLOSE_EXPOSURE` intents → send under TRADE+lease. One-way side-effect: force stop-new ON (`STOP_NEW.ENGAGED_BY_FLATTEN`). | Be an alias of stop-new. Auto-fire from `GLOBAL_STOP`. Run without SuperAdmin + step-up. Blind-retry unknowns. Flatten source MT5 or shadow. |

Invariant (A48 §2): **four flatten phases never replace the stop-new bit.** Flatten-active + stop-new-on is the **normal** in-progress pair.

One-way coupling is **not** identity:

```text
flatten request  →  stop-new ON   (recorded side-effect)
flatten complete →  stop-new stays ON   (never auto-clear)
deactivate stop-new while flatten not idle →  409
```

### 2.4 RBAC — two permissions (A48 §8 / A51 supersede A26)

| Action | RiskManager | SuperAdmin |
|---|---|---|
| Activate / clear `STOP_NEW_EXECUTION` | Yes | Yes |
| Request / confirm `EMERGENCY_FLATTEN` | **No** | Yes + step-up + typed phrase + single-use token |

A26 §10.2 still lists flatten as RiskManager `W`. **A48 / A51 / A95 win** for flatten role. Spec conflict is documented; implementers must not ship A26’s RiskManager flatten.

### 2.5 Reason codes (must stay separate)

| Situation | Binding code | Engine today |
|---|---|---|
| Stop-new latch on, increasing action | `STOP_NEW_EXECUTION` | `STOP_NEW_EXECUTION` (L79) |
| Flatten run in progress, increasing action | `EMERGENCY_FLATTEN_ACTIVE` (A23 / A48 / A71) | `EMERGENCY_FLATTEN_BLOCKS_NEW` (L82) — **adjacent, not the catalog name** |
| Flatten owns this dest id (source-driven close) | `FLATTEN_OWNS_POSITION` (A71) | **MISSING** |

---

## 3. Measured product — they are not independently representable

### 3.1 Exclusive enum as SoT

```1:8:D:\Prop\src\Domain\Enums\KillSwitchMode.cs
namespace TraderIntelligence.Domain.Enums;

public enum KillSwitchMode
{
    None = 0,
    StopNewExecution = 1,
    EmergencyFlatten = 2
}
```

A C# enum is one value. There is no flags attribute. There is no second field. `None = 0` invites “safe / off.” A48 §14: **do not** treat `KillSwitchMode.None` as the safe default.

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

EF maps this to `kill_switches` (PK only). A20 / A61: current kill state should be derived from distinct `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` **events**, not a single mode column. A48: if `KillSwitchMode` remains, it is a **control label**, not runtime state.

**Cartesian product that cannot be stored:**

| stop-new | flatten | Representable as `KillSwitchMode`? |
|---|---|---|
| off | idle | `None` — **fail-open** vs A48 §3.3 |
| on | idle | `StopNewExecution` |
| off | active | **Impossible** (and illegal: flatten must force stop-new on) |
| on | active | **Impossible** — this is the required in-progress pair |
| on | confirm-pending / partial-failed / completed | **Impossible** (flatten is not a bool) |

### 3.2 Seed is fail-open

```115:122:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.KillSwitches.Add(new KillSwitch
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
            Mode = KillSwitchMode.None,
            SetBy = "system",
            Reason = "default",
            UpdatedAt = now
        });
```

A48 §3.3: missing / fresh row → treat stop-new as **ON**. Seeded `None` is fail-**open** for new copy. Live send is currently `SAFE_BY_ABSENCE` (no NOS), not because the latch is closed.

### 3.3 RiskEngine: two labels, one send gate

```78:82:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");
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

| Required | Measured | Pass? |
|---|---|---|
| Stop-new blocks only increasing | `&& IsIncreasing` | **Yes** (predicate) |
| Flatten-in-progress blocks only increasing | Same shape | **Yes** as “block new”; **No** as flatten (no dest snapshot, no `cl_ord_id`, no run) |
| Both on at once | Exclusive enum | **FAIL** |
| Stop-new leaves dest book untouched | Engine never sees dest positions | **Pass by omission** — not a control proof |
| Reduce/close still **approvable** under stop-new | Reducing skips the two `if`s | **Yes** on `Outcome` |
| Reduce/close still **sendable** under stop-new | `allowSend` requires `KillSwitch == None` | **FAIL** |
| Flatten closes may send even if `REAL_COPY=false` (A25 §6.5) | `allowSend` requires `RealExecutionEnabled` | **FAIL** |
| Loss / DD engage **stop-new only**, do not freeze exits | L117–124 apply to **every** action (`MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`) | **FAIL** — freezes close |
| Engine `GLOBAL_STOP` writes the latch | Outcome returned, forgotten | **MISSING** (A23 §8.3) |
| Unknown `KillSwitchMode` (e.g. `(KillSwitchMode)99`) | Falls through to `APPROVED` | **UNSAFE** if on a send path |

Setting `Mode = EmergencyFlatten` **never closes a destination position**. It is stop-new with a different string.

`IsIncreasing` = `OpenExposure | IncreaseExposure`. `IsReducing` = `ReduceExposure | CloseExposure`. Correct §64 **family split**. Wrong **state model**.

### 3.4 Dashboard / API / UI collapse to one string

C# DTO (`DashboardModels.cs` L94–102):

```text
RiskDashboardDto(
    DailyPnl, Drawdown, XauLong, XauShort, XauNet,
    string KillSwitch,          -- §40 / A95 violation
    bool RealCopyEnabled,
    IReadOnlyList<string> RecentRejectReasons)
```

Query (`EfDashboardQueries.cs` L186–196):

```text
latest kill_switches.Mode.ToString()   -- "None" | "StopNewExecution" | "EmergencyFlatten"
missing row → KillSwitchMode.None      -- fail-open
DailyPnl/Drawdown/Xau* hardcoded 0
RealCopyEnabled hardcoded false
```

API (`Program.cs`):

| Map | Distinctness |
|---|---|
| `GET /api/risk` | Same stub as `/api/risk/status` |
| `GET /api/risk/status` | Same stub |
| `POST /api/v1/risk/stop-new-execution` | **MISSING** |
| `POST /api/v1/risk/emergency-flatten*` | **MISSING** (correct **absence** for first-useful / Phase 8; A57 / A63) |
| Settings `KillSwitchEnabled` | Unrelated single bool (`SettingsController` L33). `Put` does **not** write it. |

`appsettings.json` L35–42 carries **four** unread/overlapping keys:

```text
RiskEngine:KillSwitchEnabled = true
RiskEngine:KillSwitchOn = false          -- third single-bool metaphor
RiskEngine:StopNewExecutionOn = false    -- name looks right; nothing reads it
RiskEngine:EmergencyFlattenApiKey = ""   -- not A48 step-up; unused
```

SettingsController GET exposes only `KillSwitchEnabled`. Development appsettings has **none** of these keys.

React Risk page (`RiskPage.tsx` L11): **one** card `label="Kill switch" value={data.killSwitch}`.

Web types (`index.ts` L105–106) declare two bools:

```text
stopNewExecution: boolean;
emergencyFlatten: boolean;    -- A95: a flatten bool is itself a conflation (run ≠ bool)
```

`useRiskStatus` fetches `GET /api/risk` (D39: path matches). The C# payload is `{ killSwitch: "None", … }`, **not** those two bools. TypeScript and JSON disagree. The page renders the C# string.

A95 required nested object: `data.killSwitch.stopNewExecution` **plus** `flattenPhase` / `emergencyFlattenAvailable` / `emergencyFlattenAllowedForMe`. Current GET is not that contract.

### 3.5 Tests do not prove distinctness

`RiskEngineTests` has **5** facts. Kill coverage is **one**:

```28:41:D:\Prop\tests\Unit\RiskEngineTests.cs
    public void Stop_new_execution_blocks_opens_not_closes()
    {
        var open = _e.Evaluate(Base(q => q with { KillSwitch = KillSwitchMode.StopNewExecution }));
        open.Outcome.Should().Be(RiskDecisionOutcome.GlobalStop);

        var close = _e.Evaluate(Base(q => q with
        {
            Action = CopyIntentAction.CloseExposure,
            KillSwitch = KillSwitchMode.StopNewExecution
        }));
        close.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        close.AllowFixSend.Should().BeFalse();
    }
```

| Asserted | Not asserted |
|---|---|
| Open `Outcome=GlobalStop` | Reason `STOP_NEW_EXECUTION` |
| Close `Approve` + `AllowFixSend=false` | Dest book unchanged (engine has no dest book) |
| | `IncreaseExposure` |
| | `ReduceExposure` |
| | `KillSwitchMode.EmergencyFlatten` (**zero** mentions in `tests/`) |
| | Simultaneous stop-new + flatten |
| | Flatten does not emit dest closes |
| | `GLOBAL_STOP` does not flatten |
| | RBAC / audit / confirm |

`close.AllowFixSend.Should().BeFalse()` **locks the send defect**. A48 / A71: approved CLOSE under stop-new **must** be sendable when TRADE+lease+mapped dest+`allow_risk_reduction_while_stop_new` hold. Implementing the spec **breaks this fact** (D35 §3.3).

A27 required classes **not on disk**:

```text
Risk.KillSwitchStopNewExecutionTests
Risk.KillSwitchEmergencyFlattenAuthorizationTests
Harness.GlobalStopNewOrdersTests
```

### 3.6 `docs/risk.md` names them separately, then contradicts §40

`docs/risk.md` L18–37 titles “Kill Switch vs Emergency Flatten” and describes two effects. Then:

```text
Daily loss breach   → Kill Switch ON (positions stay)
Total loss breach   → Emergency Flatten → then Kill Switch ON
```

**Unauthorized auto-flatten.** A23 §8.3 / A48 §14: daily-loss / drawdown / “total loss” engage **stop-new only**. Auto-flatten from a threshold is an explicit anti-pattern. The doc also says flatten “Closes all open positions immediately at market” — architecture: **known destination ids only**, persist-before-send, no blind market blast, never source MT5.

Do not implement `docs/risk.md` L34–37.

---

## 4. Third control — do not make it a fourth alias

A49 §5:

| Control | New copy `35=D` | Dest positions | Reduce/close `35=D` |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED=false` | blocked | untouched | Not routine copy-close; flatten is different |
| `STOP_NEW_EXECUTION` | blocked | untouched | Allowed under reduce/close policy if TRADE ready |
| `EMERGENCY_FLATTEN` | blocked (opens) | close **attempted** | May send reducing orders even if `REAL_COPY=false` (A25 §6.5) |

Today `AllowFixSend` ANDs `RealExecutionEnabled` **and** `KillSwitch == None`. That collapses all three into “may I send anything?” — illegal for flatten, illegal for source-driven close under stop-new.

Dashboard hardcodes `RealCopyEnabled=false` independently of the kill string. That default is **correct** for the flag. It is not a substitute for the latch.

---

## 5. What is *not* distinct vs what *is*

### 5.1 Distinct today (vocabulary only)

- Two enum **names**.
- Two engine **reason strings** (`STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN_BLOCKS_NEW`).
- Architecture / A48 / A23 / A51 / A95 **text**.
- First-useful policy correctly **omits** the flatten mutation (A57 / A63). Absence of flatten POST is not a distinctness win; it is the right v1 cut.

### 5.2 Not distinct today (state / effect / permission / proof)

- Persisted state (one `Mode`).
- Risk request (one `KillSwitch` field).
- Send license (`== None`).
- Dashboard DTO / Risk page chip.
- Config keys (`KillSwitchOn` sits beside `StopNewExecutionOn`).
- Operator verbs (no endpoints, no RBAC policies `Risk.StopNew.Write` / `Risk.Flatten.Write`).
- Tests (flatten = 0 facts).
- Engine-raised `GLOBAL_STOP` (does not persist either latch; also blocks exits).

### 5.3 Scope reminder (do not “simplify” later)

Kill switches apply to the **destination cTrader/cServer copy book only**.

| Book | Stop-new | Flatten |
|---|---|---|
| Dest execution XAUUSD | Blocks new copy | Close **known** dest ids only |
| Source MT5 | Never | **Never** — we do not own those positions |
| Shadow book | `STOP_NEW_SHADOW_OPENS` analog (A24) | `SHADOW_BOOK_FLATTEN` test-only; not live |

---

## 6. Spec conflicts implementers must not re-litigate

| Topic | Stale / wrong | Binding |
|---|---|---|
| One exclusive mode | Current `KillSwitchMode` as SoT | A48: two independent objects; enum is a label at most |
| Flatten role | A26 §10.2 RiskManager `W` | A48 / A51 / A95: **SuperAdmin + step-up only** |
| Flatten GET shape | `emergencyFlatten: boolean` (web types) / `string KillSwitch` (C#) | A95: `flattenPhase` + desk availability + caller `allowedForMe` |
| Flatten POST | A26 single `POST /emergency-flatten` + phrase | A48: request / confirm / abort / ack. v1: **404/409**, do not ship |
| Auto-flatten on total loss | `docs/risk.md` L34–37 | A48 §14 **DO NOT** |
| Missing kill row | Code → `"None"` | A48 §3.3 → stop-new **ON** (`boot_fail_closed`) |
| Reason while flatten active | Engine `EMERGENCY_FLATTEN_BLOCKS_NEW` | A23 / A71 catalog `EMERGENCY_FLATTEN_ACTIVE` |
| Stop-new Close send | Test asserts `AllowFixSend=false` | A48 / A71: sendable when close conjunction holds |

---

## 7. Go-live / first-useful boxes this question owns

All remain **unchecked**. Evidence is vocabulary, not a control.

```text
[ ] STOP_NEW_EXECUTION does not flatten          -- true by omission; not proven
[ ] EMERGENCY_FLATTEN permission is distinct
[ ] GLOBAL_STOP engages stop-new only
[ ] All kill-switch mutations audited
[ ] Global stop-new-orders works (§70.13)
[ ] Kill switch tested (§68 / A100 G16)
[ ] Flatten unknown-state does not blindly resend
```

A101 item 13 proof (when someone codes it): in-process, stop-new true → Submit = 0, reason `STOP_NEW_EXECUTION`, dest positions unchanged, **no** `35=F/G`, `EmergencyFlatten` **must not** flip on. Do **not** implement live flatten to pass stop-new.

---

## 8. What a later coding wave must change (not done here)

A48 §15 file map. Minimum distinctness bar:

1. Split SoT: `stop_new` bit × `flatten_phase` (not `KillSwitchMode` as the only row).
2. Risk request: two inputs (or a snapshot object), not one exclusive enum.
3. `AllowFixSend` for reduce/close **must not** require `KillSwitch == None`. Flatten closes skip `REAL_COPY`; they still require TRADE + lease + known dest id + persist-before-send.
4. GET Risk: two indicators (A95). Never one traffic light.
5. Mutations: separate endpoints, separate policies, append-only audit in the **same** transaction as the latch/run write.
6. Tests that would fail if someone aliases them: stop-new ON + flatten ACTIVE representable; stop-new does not emit dest closes; flatten request forces stop-new without being the same flag; deactivate stop-new during flatten → 409; `GLOBAL_STOP` does not start a flatten run.
7. Do not add `POST …/emergency-flatten` before Phase 8 + TRADE reconcile (A48 / A57).

**Do not** “fix” distinctness by flipping `Mode` between the two values. That is still one lever.

---

## 9. Honesty

- This file **did not** modify product source under `D:\Prop\src` or `D:\Prop\apps`.
- Live book is protected because **nothing sends**, not because kill switches work.
- A naïve change that sets `AllowFixSend=true` under `EmergencyFlatten` would authorize blind closes of unmapped, unclipped qty with no confirm (C33). Distinctness is not “make flatten send.”
- D13 / C33 / A48 already named the exclusive-enum defect. This report re-measured the same hashes and answers the question in one place.

**Bottom line:** `STOP_NEW_EXECUTION` and `EMERGENCY_FLATTEN` are **specified as distinct** and **implemented as one exclusive mode**. Labels are not independence. Never one flag.

---

*End of D70. Product source was not modified.*
