# P500_S052 — Unit tests encode the live-block: `CanPromoteToLive` false; `MayRetryNewOrderSingle` false after send

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S052_tests_block_live.md` |
| Agent | P500_S052 (read-only test close-read; product-law-in-tests) |
| Slot | **S052** |
| Date | 2026-08-18 |
| Assigned | Read `tests/Unit/BaselineScorerTests.cs`, `ExecutionAndSizingTests.cs`, `RiskEngineTests.cs`. Write this file. Tests assert `CanPromoteToLive` false and `MayRetryNewOrderSingle` false after send. Product law is encoded in tests. **Do not edit product.** |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Method | Full re-read of the three unit files (74 + 62 + 87 lines). Cross-read SUT bodies only to name what the asserts lock (`TraderStateMachine` L211, `ExecutionOrderStateMachine` L17–40, `RiskEngine.Evaluate`). Grep of the three facts. No `dotnet` run this slot (source-level lock; predecessors D97/D98/C02/D36 already executed the same facts). Nothing answered from memory. |

**Assigned answer: CONFIRMED. The three unit classes are the on-disk product law for “do not go live” and “do not fire another `35=D` after send.”**

**One-line:** `Three_disciplined_winners_go_to_shadow_not_live` requires `SuggestedState == SHADOW` **and** `CanPromoteToLive(SHADOW) == false`. `Unknown_ack_cannot_retry_new_order` requires `AfterSendAttempt() == SentAcknowledgementUnknown` **and** `MayRetryNewOrderSingle(sent) == false` **and** `RequiresReconciliation(sent) == true`. `Real_flag_false_never_allows_fix_send` requires `AllowFixSend == false` even on `Approve`. Together they are the regression tripwire for flipping live or retrying NOS.

---

## 0. Verdict (binding)

| Check | Result | Class |
|---|---|---|
| `CanPromoteToLive` asserted false | **Yes** — one fact, argument is `SHADOW` | `EXISTS_AND_GOOD` as a pin for the happy path |
| `MayRetryNewOrderSingle` asserted false after send | **Yes** — `AfterSendAttempt()` → `sent` → `BeFalse()` | `EXISTS_AND_GOOD` for after-send |
| Risk never allows FIX send with flag off | **Yes** — `AllowFixSend.Should().BeFalse()` on default `Approve` | complementary send choke |
| Product law lives in tests | **Yes** — these three classes are the only unit callers of the two helpers | tests are the contract |
| Product callers of either helper | **Zero** (`src/`, `apps/`) | helpers are dead APIs; tests are the only consumers |
| Theory over every `TraderState` | **No** | only `SHADOW` is passed to `CanPromoteToLive` |
| After-disconnect retry asserted false | **No** | disconnect fact only locks the enum token |
| `MayRetry` false for Filled / Cancelled / Partial | **No named fact** | fill fact only locks `Filled` |
| This slot flips §68 / §70 / A100 | **No** | tests do not create a send path |
| Product edited this slot | **No** | report only |

```text
TEST  BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live
      SuggestedState               == SHADOW
      CanPromoteToLive(SHADOW)     == false          -- L26

TEST  ExecutionAndSizingTests.Unknown_ack_cannot_retry_new_order
      AfterSendAttempt()           == SentAcknowledgementUnknown
      MayRetryNewOrderSingle(sent) == false          -- L14
      RequiresReconciliation(sent) == true

TEST  RiskEngineTests.Real_flag_false_never_allows_fix_send
      Outcome                      == Approve
      AllowFixSend                 == false          -- L24
      (Base fixture RealExecutionEnabled = false)

LIVE / 35=D still SAFE_BY_ABSENCE. Tests lock the Domain pins.
They are necessary. They are not a live gate machine.
```

Do **not** claim “LIVE promotion is gated by a full R5/R6 suite.” There is one SHADOW fixture. Do **not** claim “no-blind-retry is a system.” There is one after-send fact and zero product callers. Do **not** treat `AllowFixSend == false` as a socket choke: nothing in product reads the bit.

---

## 1. Files in scope (tests only; product not touched)

| Path | Role | Facts | Lines |
|---|---|---:|---:|
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | Score → state; **no LIVE** | 3 | 74 |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | FSM + qty + ClOrdID + expiry; **no NOS retry after send** | 6 | 62 |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | Reject / flag-off send bit | 5 | 87 |

SUT bodies the asserts compile against (read only; **not edited**):

| Path | Locked surface |
|---|---|
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` L187–211 | `FromBaseline`, `AfterHighEarlyScore`, `CanPromoteToLive(_) => false` |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` L17–40 | `AfterSendAttempt`, `MayRetryNewOrderSingle` (`NotSent` \| `Rejected` only), `RequiresReconciliation` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` L76–171 | `Evaluate`; `AllowFixSend` conjunction; every `Reject` sets send false |

---

## 2. `BaselineScorerTests` — no auto-LIVE is a unit fact

### 2.1 Fact inventory

| Fact | Asserts | Law encoded |
|---|---|---|
| `Two_trades_remain_insufficient` | `EarlyScoreEligible == false`; `SuggestedState == INSUFFICIENT_DATA` | N=2 is not a score, not SHADOW, not LIVE |
| `Three_disciplined_winners_go_to_shadow_not_live` | `EarlyScoreEligible == true`; `SuggestedState == SHADOW`; **`CanPromoteToLive(SuggestedState) == false`** | Trade #3 + high quality + SL → **SHADOW only**. Promotion helper must refuse. |
| `Martingale_after_losses_is_risk_blocked` | `Features.Martingale == true`; `SuggestedState == RISK_BLOCKED` | 0.10 / 0.20 / 0.40 losers cannot become copyable |

The load-bearing live-block is L21–27:

```21:27:D:\Prop\tests\Unit\BaselineScorerTests.cs
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }
```

That is product law in xUnit form:

1. Three completed XAU winners with SL (`InitialSl = 2290` in the fixture) **must** land `SHADOW`.
2. The name of the test (`go_to_shadow_not_live`) is the spec title.
3. `CanPromoteToLive` is invoked on the suggested state and **must** be false. A later implementer who returns `LIVE` from `FromBaseline` fails the SHADOW assert. A later implementer who keeps `SHADOW` but flips `CanPromoteToLive` to `true` fails L26.

### 2.2 What the fixture actually builds

`Closed(n, pnl, lots=0.10)` is a completed long XAUUSD ticket: `Completed=true`, remaining 0, SL set, no scale-in / partial / average-down. PnL is injected as both gross and net. There is **no deal tape**. The scorer sees three reconstructed results, not MT5 deals.

| Input | N | PnL | Lots | Expected state |
|---|---:|---:|---:|---|
| Two winners | 2 | +10, +10 | 0.10 | `INSUFFICIENT_DATA` |
| Three winners | 3 | +80, +70, +90 | 0.10 | `SHADOW` + promote false |
| Three losers, doubled size | 3 | −100, −200, −400 | 0.10 / 0.20 / 0.40 | `RISK_BLOCKED` |

### 2.3 Honesty about the lock

| Gap | Why it matters |
|---|---|
| `CanPromoteToLive` is only called with `SHADOW` | A89 #76 / A69 TS22 want **always false** until an audited R6 type exists. `WATCH`, `EARLY_SCORE`, `RISK_BLOCKED`, `INSUFFICIENT_DATA`, unused `LIVE` / `LIVE_CANDIDATE` are not passed. |
| No `Should().NotBe(LIVE)` / `NotBe(LIVE_CANDIDATE)` pair | SHADOW implies both today. If someone adds a second return of `LIVE` on another branch, only this one fixture fails. |
| No `AfterHighEarlyScore() == SHADOW` fact | The pin exists on the SUT (L209) and is **untested** in this class. |
| Martingale fact does not mention LIVE | Correct landing is `RISK_BLOCKED`; it does not re-assert `CanPromoteToLive`. |
| Persist / Application never called | Tests do not prove `ReconstructionScoringService` or `EfTradingStore` refuse a `LIVE` write. Persist copies `SuggestedState` blindly (D97). |

The SUT body the fact compiles against:

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` reachable set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. No `return LIVE`. The test does not enumerate that set. It locks the **happy path** and the **promotion predicate**.

**Classification:** vacuous-but-correct pin, encoded as a test. Safer than a premature live path. Not A22 R5-before-R6.

---

## 3. `ExecutionAndSizingTests` — no NOS retry after send is a unit fact

### 3.1 Fact inventory

| Fact | Asserts | Law encoded |
|---|---|---|
| **`Unknown_ack_cannot_retry_new_order`** | `AfterSendAttempt() == SentAcknowledgementUnknown`; **`MayRetryNewOrderSingle(sent) == false`**; `RequiresReconciliation(sent) == true` | After a send attempt: park unknown-ack, **do not** fire another NewOrderSingle, **do** reconcile |
| `Disconnect_after_send_is_unknown_state` | `AfterDisconnectWithUnknownAck() == ExecutionStateUnknown` | Disconnect ≠ “retry”; it is unknown. **Does not** call `MayRetry` |
| `Filled_report_is_terminal` | `Apply(Accepted, FILL/2) == Filled` | Fill is terminal. **Does not** call `MayRetry` |
| `Quantity_normalizer_steps_and_min` | 0.10×1 stays 0.10; 0.10×0.05 floors to 0; 0.333 steps to 0.33 | Size math, not send |
| `ClOrdId_is_deterministic_and_unique_per_sequence` | seq 0 ≠ seq 1; prefix `TI20260818120000` | New attempt = new 11 (factory exists). Not wired to retry |
| `Copy_intent_expires` | +16s / 15s TTL expired; +5s not | Stale copy intents die. Not a send path |

The load-bearing after-send lock is L9–16:

```9:16:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    public void Unknown_ack_cannot_retry_new_order()
    {
        var sent = ExecutionOrderStateMachine.AfterSendAttempt();
        sent.Should().Be(ExecutionOrderStatus.SentAcknowledgementUnknown);
        ExecutionOrderStateMachine.MayRetryNewOrderSingle(sent).Should().BeFalse();
        ExecutionOrderStateMachine.RequiresReconciliation(sent).Should().BeTrue();
    }
```

That is architecture §33 / §34 / A42 / A70 §10 in xUnit form:

```text
AfterSendAttempt()                 → SentAcknowledgementUnknown
MayRetryNewOrderSingle(that)       → false     -- NotSent|Rejected allow-list excludes it
RequiresReconciliation(that)       → true
```

A later implementer who makes `AfterSendAttempt()` return `NotSent` fails L13. One who adds `SentAcknowledgementUnknown` to the retry allow-list fails L14. One who drops recon for sent-unknown fails L15.

### 3.2 SUT closed form the test compiles against

```17:40:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static ExecutionOrderStatus AfterSendAttempt() =>
        ExecutionOrderStatus.SentAcknowledgementUnknown;
    // ...
    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;

    public static bool RequiresReconciliation(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.SentAcknowledgementUnknown
            or ExecutionOrderStatus.ExecutionStateUnknown;
```

Closed form: `MayRetry(AfterSendAttempt())` is **always false**. The test is the permanent lock of that identity.

### 3.3 Honesty about the lock

| Gap | Why it matters |
|---|---|
| Disconnect fact does **not** assert `MayRetry == false` | `ExecutionStateUnknown` is also off the allow-list in source (D98). A future allow-list edit could add it and only the send fact would still pass. |
| No theory over the 8-status matrix | `NotSent` / `Rejected` retry **true** is untested. `Filled` / `Cancelled` / `PartiallyFilled` / `Accepted` retry **false** is untested. |
| `Rejected == true` is a foot-gun if a caller resends the **same** 11 | A42: new attempt = new ClOrdID. The helper name does not say so. Tests do not pair `MayRetry(Rejected)` with `ClOrdIdFactory.Next(..., seq+1)`. |
| Zero product callers | Worker does not ask the helper before a send. There is no `35=D` builder. Safety of “no second NOS” in production is **SAFE_BY_ABSENCE**, not this fact. |
| Qty / ClOrdID / expiry facts are not live-block | They sit in the same class. Do not inflate “six facts block live.” **One** fact is the after-send retry law. |

**Classification:** `EXISTS_AND_GOOD` for the after-send predicate. `MISSING` as a system (no persist-before-send, no worker consult, `ExecutionIntent.Status` is an unbound string).

---

## 4. `RiskEngineTests` — complementary send choke (flag off)

This class does **not** mention `CanPromoteToLive` or `MayRetryNewOrderSingle`. It encodes a third live-block: **even an approved decision must not set `AllowFixSend` when `RealExecutionEnabled` is false.**

### 4.1 Fact inventory

| Fact | Asserts | Law encoded |
|---|---|---|
| `Stale_quote_rejects_open` | `Reject` / `QUOTE_STALE` / **`AllowFixSend == false`** | Stale book cannot send |
| **`Real_flag_false_never_allows_fix_send`** | `Approve` **and** **`AllowFixSend == false`** | Default fixture (`RealExecutionEnabled = false`) may approve shadow-path risk and **must not** arm FIX |
| `Stop_new_execution_blocks_opens_not_closes` | Open → `GlobalStop`; close → `Approve` + **`AllowFixSend == false`** | Kill switch stops new exposure; flatten/close still cannot send with flag off |
| `Unreconciled_venue_blocks_new_exposure` | `Reason == VENUE_NOT_RECONCILED` | No new size on unreconded venue. Outcome/send bit not asserted |
| `Stale_signal_rejected` | `Reason == SIGNAL_STALE` | Copy latency cap. Outcome/send bit not asserted |

Load-bearing flag-off lock (L20–26) and fixture default (L72):

```20:26:D:\Prop\tests\Unit\RiskEngineTests.cs
    public void Real_flag_false_never_allows_fix_send()
    {
        var d = _e.Evaluate(Base());
        d.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        d.AllowFixSend.Should().BeFalse();
    }
```

```72:72:D:\Prop\tests\Unit\RiskEngineTests.cs
            RealExecutionEnabled = false,
```

SUT conjunction the fact depends on (`RiskEngine.cs` L147–150):

```text
allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy
```

`Base()` sets flag false, kill none, reconciled true, venue healthy. Approve path therefore yields `AllowFixSend == false`. That is product law: **shadow evaluation is allowed; live send is not.**

### 4.2 Honesty about the lock

| Gap | Why it matters |
|---|---|
| No fact with `RealExecutionEnabled = true` | The four-boolean conjunction is never shown to return `true`. A later change that hard-codes `AllowFixSend = false` would still pass every fact in this file. |
| `VENUE_NOT_RECONCILED` / `SIGNAL_STALE` do not assert `AllowFixSend` | Reject helper always sets it false in source. Untested. |
| Caps (`MaxLossPerTrader`, daily, drawdown, martingale, margin, XAU gross/net) have **zero** facts | Named limits exist on `RiskLimits`. Tests do not lock them. |
| `MaxSlippage` never evaluated in SUT | Cannot be locked from this class. |
| `RiskEngine` is not registered in product DI | Tests are the **only** constructor call site. The bit they lock is unread by workers. |

**Classification:** complementary **send-bit** law. Not a trader-state law. Not a NOS-retry law. Together with §§2–3 it is the third rail of “do not go live today.”

---

## 5. Product law as encoded (the contract the three files write down)

If a future change is “go live” or “retry the order,” **these asserts are the first red tests**. That is the point of S052.

```text
                    ┌─────────────────────────────────────────┐
  Score / state     │ N=3 winners → SHADOW                    │
                    │ CanPromoteToLive(SHADOW) === false      │  BaselineScorerTests
                    └─────────────────────────────────────────┘
                    ┌─────────────────────────────────────────┐
  Execution FSM     │ AfterSendAttempt → SentAckUnknown       │
                    │ MayRetryNewOrderSingle === false        │  ExecutionAndSizingTests
                    │ RequiresReconciliation === true         │
                    └─────────────────────────────────────────┘
                    ┌─────────────────────────────────────────┐
  Risk              │ RealExecutionEnabled=false              │
                    │ Approve is allowed                      │  RiskEngineTests
                    │ AllowFixSend === false                  │
                    └─────────────────────────────────────────┘
```

Mapped to architecture (read as **what the tests claim**, not what the system implements end-to-end):

| Law | Test encoding | End-to-end today |
|---|---|---|
| §1.4 / §3 / §23: trade #3 is not LIVE | `go_to_shadow_not_live` | `FromBaseline` has no LIVE branch; persist does not ask the pin |
| A22 I4 / A69 TS22: no auto-promote | `CanPromoteToLive == false` on SHADOW | pin is hard-`false` for all states; **untested** except SHADOW |
| §33 / §34 / A42 / A70 §10: no blind NOS retry | `MayRetry(AfterSendAttempt()) == false` | helper unused by workers; no `35=D` builder |
| §34: unknown after send → reconcile | `RequiresReconciliation(sent) == true` | no OrderStatusRequest / mass-status path |
| Flag-off / shadow-only execution | `AllowFixSend == false` on default Approve | engine not in DI; worker never reads the bit |

---

## 6. What would break if someone “turned live on”

| Change | First failing fact (if any) |
|---|---|
| `FromBaseline` returns `LIVE` for quality≥70 | `SuggestedState.Should().Be(SHADOW)` |
| `CanPromoteToLive` becomes `current == SHADOW` | `CanPromoteToLive(...).Should().BeFalse()` |
| `AfterSendAttempt()` returns `NotSent` | `sent.Should().Be(SentAcknowledgementUnknown)` **and** `MayRetry` would flip true |
| `MayRetry` allow-list adds `SentAcknowledgementUnknown` | `MayRetryNewOrderSingle(sent).Should().BeFalse()` |
| `RequiresReconciliation` drops sent-unknown | `RequiresReconciliation(sent).Should().BeTrue()` |
| Default `AllowFixSend = true` on Approve | `AllowFixSend.Should().BeFalse()` in `Real_flag_false_never_allows_fix_send` |
| `CanPromoteToLive` true only for `LIVE_CANDIDATE` | **Would still pass** — argument is SHADOW |
| Worker starts sending `35=D` without consulting helpers | **No test in these three files fails** |

The last row is the honesty constraint: **these tests lock Domain pins, not the send path.** Live `35=D` remains `SAFE_BY_ABSENCE` (no builder, flag default false, worker logs only). Turning the worker into a sender would not go red in `BaselineScorerTests` / `ExecutionAndSizingTests` / `RiskEngineTests`.

---

## 7. Count

| Class | Facts | Facts that block live / retry / send-bit |
|---|---:|---:|
| `BaselineScorerTests` | 3 | **1** direct (`CanPromoteToLive`); 2 adjacent (no LIVE landing on N=2 / martingale) |
| `ExecutionAndSizingTests` | 6 | **1** direct (`MayRetry` after send); 1 adjacent (disconnect → unknown); 4 unrelated |
| `RiskEngineTests` | 5 | **1** direct (flag-off send bit); 2 assert `AllowFixSend` false on reject/close; 2 reason-only |
| **Total** | **14** | **3 load-bearing** + several adjacent |

Named A89 classes `ThreeTradeSafetyGateTests` / `UnknownExecutionNoBlindRetryTests` / `ExecutionOrderRetryPolicyTests` are **not on disk**. These three files are the stand-ins.

---

## 8. Product not edited

| Action | Done |
|---|---|
| Read the three unit files | Yes |
| Read SUT bodies for citation | Yes (no writes) |
| Edit `src/**` | **No** |
| Edit `tests/**` | **No** |
| Flip `CanPromoteToLive` | **No** |
| Flip `MayRetryNewOrderSingle` | **No** |
| Add facts | **No** (out of scope; would be a later test-only slot) |

Predecessors (independent re-read, not copied as this verdict): D12, D97 (`CanPromoteToLive` false), D98 (`MayRetry` false after send), C02, D34, D35, D36, P500_S011.

---

## 9. Bottom line

Product law for this bar is **in the tests**:

1. **Do not promote to LIVE** — `TraderStateMachine.CanPromoteToLive` must stay false at least for the N=3 SHADOW happy path (`BaselineScorerTests` L26).
2. **Do not retry NewOrderSingle after send** — `MayRetryNewOrderSingle(AfterSendAttempt())` must stay false (`ExecutionAndSizingTests` L14).
3. **Do not arm FIX when the real flag is off** — `AllowFixSend` must stay false on the default Approve (`RiskEngineTests` L24).

Those three asserts are the tripwire. They are not a live-trading license, not A22 R6, and not a proof that a future worker will consult them. Capital is protected today by **absence of a send path** plus these Domain pins. This slot does not change either.

**P500_S052: tests block live. Product untouched.**
