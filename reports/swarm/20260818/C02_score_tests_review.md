# C02 — BaselineScorer unit-test review (no LIVE promotion)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C02_score_tests_review.md` |
| Agent | C02 (score tests review) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| SUT | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| Tests read | `D:\Prop\tests\Unit\BaselineScorerTests.cs` (3 facts; SHA-256 `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408`; 2414 bytes) |
| Scorer SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (8143 bytes) |
| Law | Architecture §§1.4, 15, 18–23, 39, 60, 69; A22 `baseline.v1`; A69; A27; A89 G5 / #26–#41 / #75–#79 |
| Adjacent (read, not SUT) | `TraderState` enum; `ReconstructionScoringService`; `EfTradingStore.UpsertScoreAsync`; `SeedingAndStoreTests` login 10001 `NotBe(LIVE)` |
| Method | Read both files in full. Trace every `TraderState` token `FromBaseline` can emit. Grep product `*.cs` for `LIVE`, `CanPromoteToLive`, `Promote`. Run `dotnet test --filter FullyQualifiedName~BaselineScorerTests`. Hand-evaluate the three fixtures. Nothing answered from memory. |

**Assigned question:** confirm **no LIVE promotion**.

---

## 0. Verdict

| Assigned check | Class | One-line |
|---|---|---|
| LIVE / LIVE_CANDIDATE promotion from `BaselineScorer` / `TraderStateMachine` | **ABSENT (vacuous lock)** | `FromBaseline` never returns those tokens. `CanPromoteToLive(_) => false`. `AfterHighEarlyScore() => SHADOW`. Persist copies `SuggestedState` only. |
| Unit tests prove the lock | **PARTIAL** | 3/3 facts **PASS**. Happy path asserts `SHADOW` + `CanPromoteToLive == false`. No property that `SuggestedState ∉ {LIVE, LIVE_CANDIDATE}` for all N. No theory over every enum value. Named A27/A89 class `ThreeTradeSafetyGateTests` is **not on disk**. |
| Trade #3 + high score → SHADOW, not LIVE | **PASS on the one fixture** | `Three_disciplined_winners_go_to_shadow_not_live` (3 flat 0.10 winners + SL). Does **not** cover A22 Case B (winning martingale) or the mild-martingale SHADOW hole (B12). |
| `baseline.v1` implemented / gold-fileable | **No** | Tests lock the **stub**, not A22. Martingale `> 1.25×` vs spec `>= 1.80`. Additive scores vs Lerp + floors + `U(N)`. |

**One-line:** trade #3 cannot be `LIVE` today because **nothing can be `LIVE`**, and the three green facts only pin the happy-path SHADOW landing plus a losing-martingale `RISK_BLOCKED`. Do not claim A22 I4/I5 or A89 #76 are covered.

Do not claim “LIVE promotion is gated.” There is no R5-before-R6, no `MIN_LIVE_TRADES > 3` loader, no RBAC `manual_live_approve`, no `risk_engine_live_ok`. Safety is “we forgot live,” plus a hard-false pin that **Application never calls** on persist.

---

## 1. Measured test run

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~BaselineScorerTests --nologo

Passed!  Failed: 0, Passed: 3, Skipped: 0, Total: 3, Duration: 1 ms
```

| Fact | Result |
|---|---|
| `Two_trades_remain_insufficient` | PASS |
| `Three_disciplined_winners_go_to_shadow_not_live` | PASS |
| `Martingale_after_losses_is_risk_blocked` | PASS |

B08 already listed these three names. B12’s “Tests? None” is **stale** as of this file’s write time. A09’s “0 of 17” is also stale.

---

## 2. What the two files actually are

### 2.1 Product (`BaselineScorer.cs`)

One compilation unit, four types:

| Type | Role |
|---|---|
| `FeatureSnapshot` | Window aggregates + unused MFE/MAE slots (`Unavailable` / `Unknown`) |
| `BaselineScore` | `RiskScore`, `BehaviorScore`, `EarlyQualityScore`, `SuggestedState`, `EarlyScoreEligible` |
| `BaselineScorer` | `EarlyScoreTradeCount = 3`; `ComputeFeatures`; `Score`; population CV |
| `TraderStateMachine` | Stub resolver + two pins |

`Score()` always:

1. Re-filters `Completed && IsXauUsd`, sorts by `ClosedAt` only.
2. Builds features on the entire caller-supplied list (no `FIRST3` / `EXPANDING` / `as_of`).
3. Sets `EarlyScoreEligible = N >= 3` (sticky bool, **not** the §15 one-shot `EARLY_SCORE_ELIGIBLE` event).
4. Adds/subtracts flat points for risk / behavior / quality.
5. Calls `TraderStateMachine.FromBaseline`.

No `DateTime.UtcNow` inside the calculator (A22 I11 **pass** for the SUT).

### 2.2 Tests (`BaselineScorerTests.cs`)

Single class, one helper `Closed(n, pnl, lots=0.10)`:

- Always `CanonicalSymbol = "XAUUSD"`, `Completed = true`, `InitialSl = 2290`, `WasAveragedDown = false`.
- Flat volume unless `lots` is passed.
- `OpenedAt`/`ClosedAt` = UnixEpoch + n hours / +30 minutes (stable order).
- Asserts **state + eligibility + one feature bit**. Never asserts the three numeric scores, PF, CV, drawdown, SL rate, MFE quality, or `AfterHighEarlyScore`.

No `[Theory]`. No folder `tests/Unit/Scoring/`. No `ScoreConfig`.

---

## 3. Assigned check — no LIVE promotion

### 3.1 Binding rule (A22 §9.2 / I4–I5 / architecture §1.4, §23)

```text
WHEN N == 3:
    next_state ∈ { EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED }
    next_state ∉ { LIVE, LIVE_CANDIDATE }
    even if early_quality == 82 and NET is huge:
        maximum automatic promotion = SHADOW
```

`LIVE` / `LIVE_CANDIDATE` exist on the enum (`TraderState.cs` values 4 and 5). That is vocabulary, not a path.

### 3.2 Resolver — reachable tokens (measured)

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

| Token | Reachable from `FromBaseline`? |
|---|---|
| `INSUFFICIENT_DATA` | Yes (`N==0` or `N<3`) |
| `RISK_BLOCKED` | Yes (`risk>=80` **or** losing martingale with DD) |
| `SHADOW` | Yes (`N>=3`, `quality>=70`, `risk<40`) |
| `WATCH` | Yes (`N>=3`, `quality>=55`, not SHADOW, not blocked) |
| `EARLY_SCORE` | Yes (else) |
| `LIVE` | **No** |
| `LIVE_CANDIDATE` | **No** |
| `PAUSED` | **No** |
| `DISQUALIFIED` | **No** |

There is **no** `if (N == 3) forbid LIVE` line. There is no R5-before-R6. Live is unreachable at **every** `N`, including `N=20`. A69 already classified this stub `EXISTS_NEEDS_REFACTOR`. That still holds.

`CanPromoteToLive` ignores `current` and is hard-`false`. That is safer than a premature live path. It is **not** the A22 R6 + RBAC + risk-engine conjunction. A later implementer who adds `return LIVE` to `FromBaseline` and forgets to change this pin still has an Application hole (see §3.4).

### 3.3 What the unit tests actually lock

```12:41:D:\Prop\tests\Unit\BaselineScorerTests.cs
    [Fact]
    public void Two_trades_remain_insufficient()
    {
        var score = _s.Score(new[] { Closed(1, 10), Closed(2, 10) });
        score.EarlyScoreEligible.Should().BeFalse();
        score.SuggestedState.Should().Be(TraderState.INSUFFICIENT_DATA);
    }

    [Fact]
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }

    [Fact]
    public void Martingale_after_losses_is_risk_blocked()
    {
        // 0.10 / 0.20 / 0.40 after losses → Martingale true, RISK_BLOCKED
    }
```

| Claim the test name suggests | What is asserted | Hole |
|---|---|---|
| “go_to_shadow_not_live” | `SuggestedState == SHADOW` and `CanPromoteToLive(SHADOW) == false` | Does not assert `!= LIVE` and `!= LIVE_CANDIDATE` as a pair (SHADOW implies both, but a future change to `LIVE` would fail the SHADOW assert — **this one fixture is load-bearing**). |
| CanPromoteToLive is the gate | Only called with `SHADOW` | Never called with `WATCH`, `EARLY_SCORE`, `RISK_BLOCKED`, `INSUFFICIENT_DATA`, or the unused `LIVE` / `LIVE_CANDIDATE` tokens. A89 #76 wants **always false** until an audited promotion type exists. |
| AfterHighEarlyScore = SHADOW | **Not called** | Dead pin in product (B12 already noted). A89 #76 requires it. |
| N==3 cannot be LIVE for *any* book | Only the clean-winner book | Winning martingale (A22 Case B) and mild 1.30× martingale are untested. Those must not be LIVE either; today they land `WATCH` / `SHADOW` (B12 §2.4). |
| N<3 cannot be LIVE | `INSUFFICIENT_DATA` on N=2 | Does not assert `!= LIVE`. Huge-NET two-trade book is untested (quality still computed, capped at 40, state still insufficient). |

**Confirmed:** the only high-score N=3 fixture in unit tests lands `SHADOW`, not `LIVE`, and the pin returns false for that state.

**Not confirmed by tests:** ∀ books at N=3, ∀ N, ∀ enum values, persist path, AfterHighEarlyScore.

### 3.4 Persist path does not ask the pin

`ReconstructionScoringService.RebuildTraderAsync` (Application, not modified here):

```csharp
CurrentState = score.SuggestedState,
```

No `CanPromoteToLive`. No risk engine. No RBAC. `EfTradingStore.UpsertScoreAsync` copies `CurrentState` onto `trader_scores` and appends history.

Dashboard `EfDashboardQueries` **counts** `LIVE` and `LIVE_CANDIDATE`. Counting is not promotion. The scorer cannot produce those tokens, so the counters stay 0 unless a row is written by hand.

Integration `SeedingAndStoreTests` asserts login **10001** `CurrentState.Should().NotBe(LIVE)` (not `== SHADOW`) and 10002 `== RISK_BLOCKED`. That is a second, weaker LIVE check on the demo tape, not a unit property.

Grep of product `src/**/*.cs` for `CanPromoteToLive`: **definition only**. Tests are the only callers.

### 3.5 LIVE-promotion checklist

| ID | Required | Product | Unit test | Result |
|---|---|---|---|---|
| I4 / A22 §9.2 | N==3 ∉ {LIVE, LIVE_CANDIDATE} | Tokens unrepresentable in `FromBaseline` | One fixture asserts SHADOW | **PASS (vacuous) / PARTIAL test** |
| I5 | High score @3 → SHADOW only | Iff `quality>=70 && risk<40` | Happy-path only | **PARTIAL** |
| A69 S5 | quality=82 cannot be live | Live unreachable | Not asserted | **PASS (vacuous)** |
| A69 TS22 | `CanPromoteToLive` false when N<=3 | False for all N / all states | False only for SHADOW | **PASS-safer / PARTIAL test** |
| A89 #76 | `AfterHighEarlyScore()==SHADOW` | Hardcoded | **MISSING** | pin exists, unlocked |
| A22 R6 | LIVE only via N>3 + shadow sample + manual approve + risk ok | **MISSING** | **MISSING** | safe by absence |
| A22 Case B | Winning 2×/2× martingale @3 → RISK_BLOCKED | Lands **WATCH** (risk=70, NET>0) | **MISSING** | **PRODUCT FAIL** (not LIVE, wrong state) |
| Mild 1.30× martingale | FLAG_MARTINGALE → RISK_BLOCKED | Lands **SHADOW** | **MISSING** | **PRODUCT FAIL** (SHADOW on a martingale book) |
| `PROVEN_PROFITABLE` | Never emit | Token does not exist | Not asserted | **PASS** |
| Application persist | Must not write LIVE without pin + risk | Writes `SuggestedState` blindly | Integration NotBe(LIVE) on 10001 only | **SAFE TODAY**, ungated tomorrow |

**Assigned answer, without greenwash:** **No LIVE promotion exists in `BaselineScorer` / `TraderStateMachine`.** Tests confirm that for one clean three-winner book. They do not implement A89 `ThreeTradeSafetyGateTests`. A winning martingale at N=3 is still not LIVE, but it is also not correctly blocked.

---

## 4. Hand-evaluation of the three fixtures (stub arithmetic)

Constants = stub additives. Helper always sets SL=2290, no averaging.

### 4.1 `Two_trades_remain_insufficient`

```text
N=2  NET=+20  GP=20  GL=0  PF=99  lot_cv=0  sl=1  mart=false
risk=0  behavior=100
quality raw=50+15+10+5+20-0=100 → N<3 cap → 40
eligible=false → INSUFFICIENT_DATA
```

Asserts only eligible + state. Does **not** assert quality still published at 40 (A22: provisional, not official; persist will still write it).

### 4.2 `Three_disciplined_winners_go_to_shadow_not_live`

```text
N=3  NET=+240  GP=240  GL=0  PF=99  lot_cv=0  sl=1  max_dd=0
risk=0  behavior=100
quality=50+15+10+5+20-0=100.00     // A22 cap after U(3)=18 is ≤82
eligible=true  quality>=70 ∧ risk<40 → SHADOW
CanPromoteToLive(SHADOW)=false
```

State: **PASS vs I5 for this book.** Numbers: **FAIL vs A22** (quality 100 vs ≤82; PF 99 vs PF_CAP=5). Tests do not lock the numbers, so they will stay green after a correct `U(N)` lands *or* stay green forever on the stub.

### 4.3 `Martingale_after_losses_is_risk_blocked`

```text
lots 0.10 → 0.20 → 0.40 after losses (−100, −200, −400)
0.20 > 0.10*1.25 and 0.40 > 0.20*1.25 → Martingale=true
0.20 > 0.10*1.50 and 0.40 > 0.20*1.50 → LotEscalation=true
lot_cv ≈ 0.5345 > 0.5 → +10 risk, >0.4 → −10 behavior
GP=0 so max_dd>GP term is off
risk = 35+15+10 = 60   (not ≥ 80)
behavior = 100-30-10 = 60
NET<0 ∧ martingale ∧ max_dd>0 → RISK_BLOCKED   // second clause, not the risk floor
```

A22: ratio `>= 1.80` (2.00× still hits). Flag floors risk at **80**. Stub blocks only because the book **lost**. Flip trade 3 to +800 (A22 Case B) → same pattern, `risk=70`, `WATCH`. **That fixture is not in this class.**

---

## 5. Coverage vs A89 / A27 / §60 (honest)

On disk: **one** class, **three** facts. A89 lists **16 scoring classes** (#26–#41) plus FSM #75–#79 as `EXISTS`. They are **specified, not present**.

| A89 # | Specified class | Covered by `BaselineScorerTests`? |
|---|---|---|
| 26 | `BaselineScorerFeatureSnapshotTests` | **No** (empty / non-XAU / incomplete / ClosedAt order untested) |
| 27 | `DrawdownCalculatorTests` | **No** |
| 28 | `MartingaleDetectorTests` (G2: 1.80; size-up after **win** is not martingale) | **Partial** — losing 2×/2× only; threshold 1.25 unchallenged |
| 29 | `AveragingDownDetectorTests` | **No** (`WasAveragedDown` always false) |
| 30 | `LotEscalationDetectorTests` (G3: 2.00) | **No** (escalation is a side-effect of the martingale fixture, never asserted) |
| 31 | `ProfitFactorAndNetPnlTests` | **No** |
| 32 | `LotCvAndLossSizeCvTests` (sample vs population) | **No** |
| 33 | `SlUseAndHoldTimeTests` | **No** |
| 34 | `TraderScoreCalculatorTests` (scores in [0,100], not rank-by-NET) | **No** numeric asserts; I9 violated in product (`NetPnl>0 → +15`) |
| 35 | `EarlyQualityUncertaintyPenaltyTests` (`U(3)=18`) | **MISSING** — stub has no `U(N)` |
| 36 | `MfeMaeFeatureQualityTests` | **No** (product correctly omits; unlocked) |
| 38 | `ScoringAsOfNoFutureLeakageTests` | **No** |
| 40 | `ScoringCannotBypassRiskContractTests` | **No** (RiskEngine never composed) |
| 41 | `ReconstructionScoringServiceScoreFieldsTests` | Integration seed only |
| 75 | `TraderStateMachineFromBaselineTests` (N=0, WATCH, EARLY_SCORE) | **Partial** — N=2 and two N=3 landings; N=0 / WATCH / EARLY_SCORE **missing** |
| 76 | `ThreeTradeSafetyGateTests` | **Partial** — one SHADOW fixture + pin on that state |
| 77–79 | transition graph / DQ / rescoring 3 vs 4 vs 5 | **No** |

§60 items this class touches:

| §60 area | Status here |
|---|---|
| 9 drawdown | untested |
| 10 MFE/MAE | untested (omit is correct) |
| 11 martingale detection | **PARTIAL** |
| 12 averaging-down | untested |
| 13 score-state transitions | **PARTIAL** (3 of 5 reachable tokens) |

---

## 6. Product defects the tests will not catch (still not LIVE)

These are **not** LIVE promotion. They are why “tests are green” is not “scoring is done.”

| Defect | Evidence | Test gap |
|---|---|---|
| Martingale ratio `> 1.25` vs A22 `>= 1.80` | `BaselineScorer.cs` line 90 | 1.26× after loss would be martingale here, not in spec |
| Escalation `> 1.50` vs `>= 2.00` | line 92 | untested |
| Winning martingale → `WATCH` not `RISK_BLOCKED` | B12 Case B; risk additive 70, NET>0 skips the block clause | **no fact** |
| Mild martingale → `SHADOW` | 1.30× after loss, risk=35<40, quality high | **no fact** |
| `U(N)` missing; quality can be 100 at N=3 | lines 152–160 | happy-path would fail a correct A22 gold |
| NET sign inside quality (I9) | `if (features.NetPnl > 0) quality += 15` | no pair of process-identical +1/−1 books |
| Population CV (`.Average()` of squares) | lines 174–184 | A89 #32 PARTIAL |
| PF cap 99 vs 5; all-BE is 0 vs 1 | line 114 | untested |
| Order by `ClosedAt` only | line 44 | equal close times can flip adjacent ratios |
| Persist publishes N<3 quality | Application | N=2 test ignores scores |
| `AfterHighEarlyScore` unused | line 209 | unused by `Score()`, unused by tests |

Formulas remain an unversioned stub. Do not gold-file A22 Cases A–F against this class.

---

## 7. What is *not* a LIVE path (do not over-report)

- `TraderState.LIVE` / `LIVE_CANDIDATE` enum members — vocabulary required by A22 §9 / A69.
- Dashboard counters for those states — read model.
- `REAL_COPY_EXECUTION_ENABLED` — execution flag; scorer does not read it (A22 §9.3 note). Still default false elsewhere (B13).
- `CanPromoteToLive` existing as an API — the body is `false`.
- Integration `NotBe(LIVE)` on demo 10001 — agrees with this review; does not promote.

---

## 8. Minimum tests still required to honestly claim the LIVE gate

Do **not** implement in this change-set (product source frozen for C02). When a later coder touches scoring, these are the P0 facts A89 #76 already named:

1. Theory: every `FromBaseline` landing at `N==3` has `SuggestedState` ∉ `{LIVE, LIVE_CANDIDATE}`.
2. `AfterHighEarlyScore() == SHADOW`.
3. `CanPromoteToLive` is false for **every** `TraderState` value (lock until an audited R6 type exists).
4. A22 Case B (2.00×/2.00× after losses, last trade large win) → **not LIVE**; spec `RISK_BLOCKED` (will **fail** today’s stub — that is the point).
5. N=0 empty list → `INSUFFICIENT_DATA`; N=4 does not unlock LIVE either (vacuous today; keep after R6 lands).
6. Persist/application: `ReconstructionScoringService` must refuse `CurrentState = LIVE` unless `CanPromoteToLive` **and** risk/RBAC say so. Today it cannot be shown.

Until (1)+(3) exist, “no LIVE promotion” is a **code-reading claim**, not a regression lock.

---

## 9. Honesty / do-not-claim

| Claim | Allowed? |
|---|---|
| `BaselineScorer` / `TraderStateMachine` promote anyone to LIVE | **No. They do not.** |
| Unit tests prove the A22 Trade-#3 safety gate | **No.** Three facts; one SHADOW fixture; pin tested once. |
| `baseline.v1` / A22 §§5–7 implemented | **No.** |
| A89 scoring classes 26–41 exist on disk | **No.** Status column in A89 is a plan, not an inventory. |
| Green `BaselineScorerTests` ⇒ go-live scoring box | **No.** A100 scoring/risk boxes stay unchecked. |
| B12 “Tests? None” | **Stale.** Use this file for the test surface. |

**C02 done.** Product source untouched.
