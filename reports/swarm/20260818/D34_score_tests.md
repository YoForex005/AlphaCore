# D34 — `BaselineScorerTests` surface (3 facts, not A22)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D34_score_tests.md` |
| Agent | D34 (score tests) |
| Date | 2026-08-18 |
| Assigned | Read `BaselineScorerTests.cs`. Write this report. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| Tests read | `D:\Prop\tests\Unit\BaselineScorerTests.cs` |
| Tests SHA-256 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` |
| Tests size / mtime | 2414 bytes / 74 lines / 2026-08-18 13:17:42 |
| SUT (read, not edited) | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| SUT size / mtime | 8143 bytes / 212 lines / 2026-08-18 13:08:10 |
| Adjacent (read) | `TraderState.cs`; `ReconstructedTradeResult.cs`; `TraderScore.cs`; `DealIngestionService.RebuildTraderAsync`; `TradeReconstructor.IsEarlyScoreEligible`; `SeedingAndStoreTests`; `TradeReconstructionTests.First_three_completed_xau_unlocks_early_score`; `docs/scoring.md`; A22 / A27 / A69 / A89; B35; C02 / C17 / C32; D12 |
| Method | Full re-read of the test file and helper. Enumerate every FluentAssertions call. Map every `BaselineScore` / `FeatureSnapshot` / `TraderState` field to “asserted / not”. Hand-evaluate the three fixtures against the live stub arithmetic. Run `dotnet test --filter FullyQualifiedName~BaselineScorerTests`. Grep `tests/` for other scorer callers. Nothing answered from memory. |
| `dotnet test` this pass | **2026-08-18 13:36:43** — 3 passed / 0 failed / 0 skipped |

**One-line:** three green facts lock B35’s three **qualitative** gates (N=2 insufficient; N=3 clean winners → `SHADOW`; losing 2×/2× → `RISK_BLOCKED`). They do **not** lock scores, CVs, PF, drawdown, SL rate, averaging, escalation, `WATCH` / `EARLY_SCORE` / N=0, A22 `baseline.v1`, or the C32 winning-martingale SHADOW hole.

---

## 0. Verdict

| Check | Class | One-line |
|---|---|---|
| File exists / compiles / runs | **PASS** | 3/3 facts green this pass (0.35 s). |
| Facts == B35 qualitative trio | **PASS** | Same reconstructed books as FX-01 / FX-02 / FX-03. |
| Facts == B35 numeric gold | **NO** | Zero asserts on `RiskScore` / `BehaviorScore` / `EarlyQualityScore`. FX-02 `100.00` and FX-03 `47.00` are unlocked. |
| A22 `baseline.v1` locked | **NO** | Tests pin the **stub**. A `U(N)` / flag-floor / 1.80-ratio land would stay green until someone writes new gold. |
| A89 scoring classes #26–#41, FSM #75–#79 | **MISSING** | One collapsed class in `tests/Unit/`, not `tests/Unit/Scoring/`. Named `ThreeTradeSafetyGateTests` is **not on disk**. |
| §60 drawdown / MFE / averaging-in-score | **MISSING / PARTIAL** | Drawdown never asserted. MFE never asserted. `FeatureSnapshot.AveragingDown` never asserted. Martingale is one **losing** 2×/2× book. |
| LIVE promotion locked for ∀ N=3 books | **PARTIAL** | One fixture asserts `== SHADOW` + `CanPromoteToLive(SHADOW)==false`. `AfterHighEarlyScore` never called. Theory over enum values: none. |
| Winning martingale / mild 1.26× | **UNLOCKED** | C32: quality **85.25 SHADOW** (mild) / **77.00 WATCH** (Case B). No fact. |
| Green tests ⇒ scoring done | **NO** | 7 assertions. Helper freezes SL / XAU / completed / no-avg-down. Most of the surface cannot fail. |

**Do not claim** “scoring is unit-tested.” **Do not claim** A22 Case A–F gold. **Do not claim** martingale cannot be `SHADOW`. **Do not claim** A89 #26–#41 exist because A89’s status column says `EXISTS` — that column is SUT existence, not test existence.

---

## 1. Measured test run

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~BaselineScorerTests --nologo --verbosity normal

  Passed BaselineScorerTests.Martingale_after_losses_is_risk_blocked [12 ms]
  Passed BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live [< 1 ms]
  Passed BaselineScorerTests.Two_trades_remain_insufficient [< 1 ms]

Test Run Successful.
Total tests: 3
     Passed: 3
Total time: 0.3493 Seconds
Build succeeded. 0 Warning(s). 0 Error(s).
```

Project: `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` (xUnit 2.5.3, FluentAssertions 6.12.0, TFM `net8.0`). Refs: Domain, Application, Fix.CTrader. **Not** Infrastructure / Mt5 — this class never needs them (it hand-builds `ReconstructedTradeResult`).

No `[Theory]`. No `[Trait]`. No `IClassFixture`. No `Skip`. Moq unused here. No `tests/Unit/Scoring/` folder. No replay JSON.

---

## 2. What the file actually is

Single public class, one private helper, three `[Fact]` methods, **seven** FluentAssertions calls.

```1:74:D:\Prop\tests\Unit\BaselineScorerTests.cs
using FluentAssertions;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;

namespace TraderIntelligence.Tests.Unit;

public class BaselineScorerTests
{
    private readonly BaselineScorer _s = new();

    [Fact]
    public void Two_trades_remain_insufficient() { ... }

    [Fact]
    public void Three_disciplined_winners_go_to_shadow_not_live() { ... }

    [Fact]
    public void Martingale_after_losses_is_risk_blocked() { ... }

    private static ReconstructedTradeResult Closed(int n, decimal pnl, decimal lots = 0.10m) => ...
}
```

| Item | Measured |
|---|---|
| Namespace | `TraderIntelligence.Tests.Unit` (not `.Scoring`) |
| SUT constructed | `new BaselineScorer()` once as a field |
| Entry point used | `Score(IReadOnlyList<ReconstructedTradeResult>)` only |
| `ComputeFeatures` called by tests? | **No** (only via `Score`) |
| `TraderStateMachine.FromBaseline` called by tests? | **No** (only via `Score`) |
| `AfterHighEarlyScore` called? | **No** |
| `CanPromoteToLive` called? | **Once**, argument = the SHADOW landing |
| `EarlyScoreTradeCount` asserted? | **No** (implicit via N=2 / N=3) |

### 2.1 Assertion inventory (complete)

| Fact | Calls | Exact asserts |
|---|---:|---|
| `Two_trades_remain_insufficient` | 2 | `EarlyScoreEligible == false`; `SuggestedState == INSUFFICIENT_DATA` |
| `Three_disciplined_winners_go_to_shadow_not_live` | 3 | `EarlyScoreEligible == true`; `SuggestedState == SHADOW`; `CanPromoteToLive(SHADOW) == false` |
| `Martingale_after_losses_is_risk_blocked` | 2 | `Features.Martingale == true`; `SuggestedState == RISK_BLOCKED` |
| **Total** | **7** | state × 3, eligibility × 2, one flag, one pin |

No numeric compare. No `BeApproximately`. No forbidden-state pair (`!= LIVE` **and** `!= LIVE_CANDIDATE`). No `CompletedXauTrades` count.

### 2.2 Field coverage — `BaselineScore`

| Field | Asserted? | Where |
|---|---|---|
| `Features` (object) | only `.Martingale` | fact 3 |
| `RiskScore` | **never** | — |
| `BehaviorScore` | **never** | — |
| `EarlyQualityScore` | **never** | — |
| `SuggestedState` | yes | all 3 facts |
| `EarlyScoreEligible` | yes | facts 1–2; **not** fact 3 |

Fact 3 is eligible (`N=3`) and does not say so. A later bug that set `EarlyScoreEligible=false` on a blocked book would stay green.

### 2.3 Field coverage — `FeatureSnapshot` (18 fields)

| Field | Asserted in this class? |
|---|---|
| `Martingale` | **yes** (fact 3 only) |
| `CompletedXauTrades` | no |
| `NetPnl` | no |
| `GrossProfit` | no |
| `GrossLoss` | no |
| `ProfitFactor` | no |
| `LotCv` | no |
| `LossSizeCv` | no |
| `AveragingDown` | no |
| `LotEscalation` | no |
| `AverageHoldSeconds` | no |
| `SlUseRate` | no |
| `MaxDrawdown` | no |
| `TradeFrequencyPerDay` | no |
| `MaeMfeQuality` | no |
| `AverageMfe` | no |
| `AverageMae` | no |
| `PriceSource` | no |

**1 / 18** snapshot fields have a fact. The equity path, population CV, PF=99 cap, SL rate, hold time, and MFE-unavailable default are implementation-only.

### 2.4 `TraderState` tokens this class can fail

`FromBaseline` reachable set (D12 / this re-read): `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. Enum also has `{LIVE_CANDIDATE, LIVE, PAUSED, DISQUALIFIED}`.

| Token | Locked by a fact? |
|---|---|
| `INSUFFICIENT_DATA` | **yes** — N=2 winners only |
| `SHADOW` | **yes** — N=3 flat winners + SL |
| `RISK_BLOCKED` | **yes** — losing 2×/2× |
| `WATCH` | **no** |
| `EARLY_SCORE` | **no** |
| `LIVE` / `LIVE_CANDIDATE` | only indirectly (`== SHADOW` on one book) |
| `PAUSED` / `DISQUALIFIED` | **no** (unreachable in stub; unlocked) |

N=0 empty list → `INSUFFICIENT_DATA` is **untested**. N=1 is untested. N=4+ rescore is untested.

---

## 3. Helper `Closed` — the freeze that hides most of the SUT

```43:73:D:\Prop\tests\Unit\BaselineScorerTests.cs
    private static ReconstructedTradeResult Closed(int n, decimal pnl, decimal lots = 0.10m) =>
        new()
        {
            Id = n.ToString(),
            BrokerId = "ACHIEVER",
            Login = 1,
            PositionId = n,
            CanonicalSymbol = "XAUUSD",
            SourceSymbol = "XAUUSD",
            Direction = TradeDirection.Long,
            OpenedAt = DateTimeOffset.UnixEpoch.AddHours(n),
            ClosedAt = DateTimeOffset.UnixEpoch.AddHours(n).AddMinutes(30),
            EntryVwap = 2300,
            ExitVwap = 2301,
            InitialVolumeLots = lots,
            MaxVolumeLots = lots,
            ClosedVolumeLots = lots,
            RemainingVolumeLots = 0,
            GrossRealizedPnl = pnl,
            Commission = 0,
            Swap = 0,
            Fees = 0,
            NetRealizedPnl = pnl,
            DealCount = 2,
            OrderCount = 2,
            InitialSl = 2290,
            WasScaledIn = false,
            WasPartialClose = false,
            WasAveragedDown = false,
            Completed = true
        };
```

| Knob | Frozen value | Consequence |
|---|---|---|
| Symbol | always `XAUUSD` | Universe filter `IsXauUsd` never exercised. A EURUSD spacer that C32 showed **does not** hide martingale cannot be fed through this helper. |
| `Completed` | always `true` | Incomplete rows never stripped. |
| `ClosedAt` | always present; `UnixEpoch + n hours + 30 min` | Sort is strictly increasing. Equal-`ClosedAt` order flip (C32 `SAME_CLOSE_*`) is unreachable. |
| `InitialSl` | always `2290` | `SlUseRate` is always 1. The +10 risk / −10 behavior no-SL path is unreachable. Demo seed (no SL) is a different book. |
| `WasAveragedDown` | always `false` | `AveragingDown` feature +20 risk never fires. C51 already noted this. |
| `lots` default | `0.10` | Facts 1–2 never size-up. Escalation / lot CV only appear as a **side-effect** of fact 3 and are not asserted. |
| Direction | always Long | Irrelevant to the stub (lots + net only), so a short book is untested for no reason. |
| Prices | 2300 / 2301 | Scorer does not read VWAP. Fabricated MFE from VWAP is **not** refused by a test (A45 / A89 #36). |
| Commission / swap / fees | 0 | `Net == Gross`. Fee leakage into NET-sign quality (+15) is untested. |
| Login / broker | `1` / `ACHIEVER` | Scorer is not identity-aware. Fine. |

The helper is the right shape for B35 reconstructed gold (same family as B35 §9). It is a **bad** shape for a feature suite: almost every detector input is a constant.

---

## 4. The three facts vs the live stub (hand + B35 gold)

Constants = current `BaselineScorer` additives. Tests do **not** lock the numbers in this section. Gold is B35 measured / C32 re-measured, cited so a later numeric pin has a source.

### 4.1 `Two_trades_remain_insufficient` ≡ B35-FX-01

```text
input: Closed(1, +10), Closed(2, +10)     // lots 0.10, SL yes
N=2  NET=+20  GP=20  GL=0  PF=99  lot_cv=0  sl=1  mart=false  dd=0
risk=0  behavior=100
quality raw = 50+15+10+5+20 = 100  →  N<3 cap → 40
eligible=false  →  INSUFFICIENT_DATA
```

| Asserted | Unasserted (would be gold) |
|---|---|
| `EarlyScoreEligible == false` | `CompletedXauTrades == 2` |
| `SuggestedState == INSUFFICIENT_DATA` | `EarlyQualityScore == 40`, `RiskScore == 0`, `BehaviorScore == 100` |
| | `!= LIVE`, `!= EARLY_SCORE`, `!= SHADOW` |
| | huge-NET cousin (`+800,+900`) still capped at 40 |

Load-bearing vs architecture §15: N=2 is not official. **Locked.**

Not locked: persist still writes quality=40 (`RebuildTraderAsync` always upserts). The unit fact never looks at persist.

### 4.2 `Three_disciplined_winners_go_to_shadow_not_live` ≡ B35-FX-02

```text
input: Closed(1, +80), Closed(2, +70), Closed(3, +90)
N=3  NET=+240  GP=240  GL=0  PF=99  lot_cv=0  sl=1  dd=0
risk=0  behavior=100
quality = 50+15+10+5+20 = 100.00     // A22 after U(3)=18 is ≤82
eligible=true  quality>=70 ∧ risk<40  →  SHADOW
CanPromoteToLive(SHADOW)=false
```

| Asserted | Unasserted |
|---|---|
| eligible true | `RiskScore == 0`, `BehaviorScore == 100`, `EarlyQualityScore == 100.00` |
| `== SHADOW` | `!= LIVE` **and** `!= LIVE_CANDIDATE` as a pair |
| `CanPromoteToLive(SHADOW)==false` | `AfterHighEarlyScore() == SHADOW` |
| | `CanPromoteToLive` for the other 8 enum values |
| | A22 Case A mixed book (`+80,−40,+60`) — also SHADOW at 100.00 today |

The name says “not live.” The SHADOW equality is the **only** unit lock that would go red if `FromBaseline` started returning `LIVE` on this book. That is load-bearing and thin.

A22 I5 for **this** book: state PASS. Numbers FAIL the spec (100 vs ≤82; PF 99 vs cap 5). Because numbers are not asserted, landing `U(N)` will **keep this fact green**.

### 4.3 `Martingale_after_losses_is_risk_blocked` ≡ B35-FX-03

```text
input: 0.10/−100, 0.20/−200, 0.40/−400   (SL yes, no avg-down)
pair 1→2: −100 < 0 and 0.20 > 0.10*1.25  → Martingale
           0.20 > 0.10*1.50               → LotEscalation
pair 2→3: same
lots {0.10,0.20,0.40} LotCv = 0.5345 > 0.5
losses {100,200,400} LossSizeCv = 0.5345  (not > 0.80)
GP=0 so DD>GP addend is off (requires GP>0)
risk = 35+15+10 = 60     // not ≥ 80
behavior = 100−30−10 = 60
quality = 50 + 12 − 15 = 47.00
block via: Martingale ∧ MaxDrawdown>0 ∧ NetPnl<0
```

| Asserted | Unasserted |
|---|---|
| `Martingale == true` | `LotEscalation == true` (true on this book) |
| `SuggestedState == RISK_BLOCKED` | `RiskScore == 60` (a `>= 80` assert would **fail**) |
| | `EarlyQualityScore == 47.00`, `NetPnl == −700`, `MaxDrawdown == 700` |
| | `EarlyScoreEligible == true` |
| | `!= SHADOW`, `!= WATCH` |

**Trap (B35 §7.7 / C32 `CASEB_SL`):** flip the last print to `+800` → same 2×/2× pattern, `Martingale==true`, state becomes **`WATCH`**, quality **77.00**. This class has no such fact. Docs (`docs/scoring.md` line 9: “Martingale / large sequential size-up after losses ⇒ `RISK_BLOCKED`”) over-claim relative to the stub **and** relative to this test (the test only feeds a **losing** book).

---

## 5. Mutation analysis — what stays green

If a later coder changes only product (tests frozen), which edits does this class catch?

| Product edit | This class | Notes |
|---|---|---|
| `EarlyScoreTradeCount` 3 → 2 | **fact 1 red** | N=2 would become eligible + SHADOW |
| `EarlyScoreTradeCount` 3 → 4 | **fact 2 red** | N=3 would stay insufficient |
| SHADOW predicate `quality>=70 && risk<40` → never | **fact 2 red** | landing would be WATCH/EARLY_SCORE |
| `FromBaseline` returns `LIVE` on the clean book | **fact 2 red** | `== SHADOW` fails |
| `CanPromoteToLive` → `true` | **fact 2 red** | only for SHADOW |
| `AfterHighEarlyScore` → `LIVE` | **stays green** | never called |
| Martingale ratio `> 1.25` → `>= 1.80` | **stays green** | fixture is 2.00× |
| Escalation ratio `> 1.50` → `>= 2.00` | **stays green** | never asserted |
| Drop `LotEscalation` / lot-CV addends | **stays green** | FX-03 still blocks on NET<0 |
| `risk` floor 80 on martingale (A22) | **stays green** | already RISK_BLOCKED |
| Winning Case B → still WATCH | **stays green** | no fact |
| Mild 1.26× → SHADOW at 85.25 | **stays green** | no fact |
| `U(3)=18` quality cap | **stays green** | 100 is not asserted |
| Remove NET>+15 (I9) | **stays green** | quality unused |
| Population CV → sample CV | **stays green** | CV unused |
| PF cap 99 → 5 | **stays green** | PF unused |
| Empty-list snapshot zeros | **stays green** | N=0 untested |
| Sort key add `opened_at, id` | **stays green** | helper times already unique |
| Persist writes LIVE without pin | **stays green** | this class never hits Application |
| `MaeMfeQuality` fabricated from VWAP | **stays green** | MFE unused |

**Read:** the suite is a **state-machine smoke** for three canned books. It is not a formula lock and not an adversarial lock.

---

## 6. Adjacent tests that are *not* this class

| Location | What it proves | Overlap / gap |
|---|---|---|
| `TradeReconstructionTests.First_three_completed_xau_unlocks_early_score` | reconstructor `CountCompletedXauUsdTrades==3` and `IsEarlyScoreEligible==true` | Dual latch: reconstructor `>= 3` vs scorer `CompletedXauTrades >= EarlyScoreTradeCount`. Neither side asserts the other. A drift (reconstructor 3, scorer 4) would split eligibility. |
| `SeedingAndStoreTests` login `10001` | `CompletedXauTrades==3` and `CurrentState != LIVE` | **Not** `== SHADOW`. Demo tape is mixed P&L, **no SL** (B35 §10: cousin quality `95.50`). Vacuous vs LIVE (D12). |
| `SeedingAndStoreTests` login `10002` | `== RISK_BLOCKED` | Demo cousin of FX-03 (lots 0.10/0.20/0.40, different P&L, no SL). Must not overwrite FX-03 gold (`60/60/47.00`). |
| `RiskEngineTests` | `MartingaleFlag` default false on the happy request | FIX/copy brake, **not** score state. No `MARTINGALE_BLOCK` fact in that file’s first 40 lines; scoring tests never compose `RiskEngine`. |
| C32 harness `_tmp_c32_score/` | 43 adversarial books | **Not** a unit test. Permanent measurement, not a regression lock in `tests/Unit`. |

Grep of `D:\Prop\tests` for `BaselineScorer` / `CanPromoteToLive` / `SuggestedState`: **this file** plus the integration seeder constructing `new BaselineScorer()`. That is the entire automated score surface.

---

## 7. Coverage vs inventories (honest)

### 7.1 Architecture §60 (unit bullets that scoring owns)

| §60 item | This class | Status |
|---|---|---|
| 9 drawdown | no `MaxDrawdown` assert | **MISSING** |
| 10 MFE/MAE | no `MaeMfeQuality` assert | **MISSING** (omit in product is correct; still unlocked) |
| 11 martingale detection | one positive 2×/2× loser | **PARTIAL** |
| 12 averaging-down (score flag) | helper forces false | **MISSING** here (recon has a polarity fact; snapshot flag unused) |
| 13 score-state transitions | 3 of 5 reachable tokens | **PARTIAL** |

C17 already scored the same: drawdown MISSING, martingale PARTIAL, state PARTIAL. This re-read does not upgrade any of those.

### 7.2 A89 named classes vs on-disk

A89 §5.3 / §5.6 status `EXISTS` means **the SUT type exists**, not the test class. On disk for scoring:

| A89 # | Specified class | On disk? |
|---|---|---|
| 26 | `BaselineScorerFeatureSnapshotTests` | **No** |
| 27 | `DrawdownCalculatorTests` | **No** |
| 28 | `MartingaleDetectorTests` | **No** (fact 3 is a sliver) |
| 29 | `AveragingDownDetectorTests` | **No** |
| 30 | `LotEscalationDetectorTests` | **No** |
| 31 | `ProfitFactorAndNetPnlTests` | **No** |
| 32 | `LotCvAndLossSizeCvTests` | **No** |
| 33 | `SlUseAndHoldTimeTests` | **No** |
| 34 | `TraderScoreCalculatorTests` | **No** (no 0–100 / I9 pair) |
| 35 | `EarlyQualityUncertaintyPenaltyTests` | **No** (no `U(N)` in SUT) |
| 36–37 | MFE/MAE classes | **No** |
| 38 | `ScoringAsOfNoFutureLeakageTests` | **No** |
| 39 | `ScoreHistoryAppendContractTests` | **No** |
| 40 | `ScoringCannotBypassRiskContractTests` | **No** |
| 41 | `ReconstructionScoringServiceScoreFieldsTests` | Integration seed only |
| 75 | `TraderStateMachineFromBaselineTests` | **No** (collapsed into this file) |
| 76 | `ThreeTradeSafetyGateTests` | **No** |
| 77–79 | graph / DQ / rescoring N=3/4/5 | **No** |

`BaselineScorerTests` is a **name that A89 does not even list**. It is a pre-inventory smoke that happened to land.

### 7.3 A22 invariants this class can speak to

| ID | Invariant | Test lock? |
|---|---|---|
| I1 | completed XAU only | **No** (helper never feeds junk) |
| I2 | Trade #3 emits `EARLY_SCORE_ELIGIBLE` event | **No** (bool only; no outbox event) |
| I3 | first official score then rescore 4,5,… | **No** (no N=4) |
| I4 | N=3 ∉ {LIVE, LIVE_CANDIDATE} | **PARTIAL** — one book `== SHADOW` |
| I5 | high score @3 → SHADOW | **PARTIAL** — clean winners only |
| I6 | as-of / no future leakage | **No** |
| I7 | do not fabricate MFE | **No** |
| I8 | destination P&L out of formulas | **No** (never fed) |
| I9 | do not rank by raw NET | **FAIL in product** (`NetPnl>0 → +15`); **no test** |
| I10 | risk engine final authority | **No** compose |
| I11 | pure function, no `UtcNow` | **PASS in SUT**; no determinism fact (same list twice) |
| I12 | versioned `ScoreConfig` | **MISSING** in SUT and tests |

---

## 8. Product holes these tests will not catch (still not a request to edit)

These are already named by B12 / C02 / C32 / D12. Listed here only as **test gaps**.

| Hole | Why this class is silent |
|---|---|
| Mild winning martingale → `SHADOW` (quality 85.25, risk 35) | no 1.26–1.50× after a loss |
| A22 Case B winning 2×/2× → `WATCH` (77.00 / 72.50) not `RISK_BLOCKED` | fact 3 is NET<0 only |
| Exact `1.25×` after loss is **not** martingale (`>` not `>=`) | no boundary |
| Exact `1.50×` is martingale **without** escalation | no boundary |
| Size-up after a **win** is escalation, not martingale | no book |
| Breakeven / +0.01 spacer defeats adjacent-pair detector | no book |
| Equal `ClosedAt` + caller order flips the pair | helper times unique |
| `N<3` losing martingale is `RISK_BLOCKED` (clause before eligibility) | FX-01 uses winners; N=2 loser untested |
| `risk >= 80` block path (avg+esc+no-SL stack) | unused |
| Empty book: `SlUseRate=0` still scores risk 10 / behavior 90 / quality 40, state `INSUFFICIENT_DATA` (C23) | no empty `Score(Array.Empty<…>())` |
| Persist publishes N<3 quality | Application out of this project’s usual focus; not called |
| `AfterHighEarlyScore` dead pin | never invoked |

---

## 9. Minimum facts still required (do **not** add in this change-set)

Product source and test source stay frozen for D34. When a later coder is allowed to touch `tests/Unit`, these are the P0 rows that would make “score tests exist” a true sentence. Prefer A89 names; do not grow this one file into a kitchen sink.

1. **Numeric pin** the three B35 golds (`0/100/40`, `0/100/100.00`, `60/60/47.00`) under `score_version = baseline_v0`.
2. **`AfterHighEarlyScore() == SHADOW`.**
3. **`CanPromoteToLive` false for every `TraderState`** (theory over `Enum.GetValues`).
4. **N=3 property:** `SuggestedState ∉ {LIVE, LIVE_CANDIDATE}` on the three existing books **plus** Case B **plus** mild 1.26× (those two will document the stub hole if golded against v0, or fail if golded against A22).
5. **N=0 empty list** → `INSUFFICIENT_DATA`; snapshot zeros; do not pretend quality is official.
6. **Martingale detector sliver:** flat-after-loss = false; size-up-after-win = false; `1.25` exact = false on the stub; `1.26` = true on the stub.
7. **`LotEscalation` asserted** on FX-03 (true) and on a win-then-1.51× book (true, not martingale).
8. **I9 pair:** two process-identical books at NET +1 vs −1 must not be ranked by the +15 (will **fail** today’s stub — that is the point if locking A22).
9. **Drawdown:** peak-to-trough on `+10,−30,+5`; empty = 0.
10. **MFE:** `MaeMfeQuality == Unavailable`, averages null, even when VWAP is present.
11. **Idempotent `Score`:** same list twice → bit-identical Round2 scores (I11).
12. **Deal-tape path** (B35 §8) lives in Replay/Integration, not this class.

Until (1)+(3)+(4) exist, “no LIVE / scoring works” is a **code-reading claim** plus three smokes.

---

## 10. Relation to sibling reports (do not treat as stale unless named)

| Report | Role vs this file |
|---|---|
| C02 | Same three facts; assigned question was **no LIVE promotion**. Still valid. D34 is the **test-surface / assertion / mutation** cut. |
| D12 | Scorer review (SUT). Confirmed no LIVE path. Tests section there is a subset of C02. |
| B35 | Canonical fixture catalog. This class **is** FX-01/02/03 at the reconstructed layer, without numeric gold or deal tapes. |
| C32 | Proves quality ≥ 70 **with** `Martingale==true`. This class does not constrain that. |
| C17 | §60 map: this class is the only scoring suite; 3 pass. Drawdown still MISSING. |
| A89 | Backlog. Status `EXISTS` ≠ test on disk. |
| B12 “Tests? None” | **Stale.** Use C02 / this file. |

---

## 11. Honesty / do-not-claim

| Claim | Allowed? |
|---|---|
| `BaselineScorerTests` exists and is 3/3 green (this pass) | **Yes. Measured.** |
| Those 3 facts match B35 qualitative outcomes | **Yes.** |
| Numeric scores are regression-locked | **No.** |
| A22 `baseline.v1` is tested | **No.** |
| A89 scoring / FSM classes are on disk | **No.** |
| Martingale cannot reach SHADOW / quality ≥ 70 | **False** (C32). Tests only lock the **losing** 2× book. |
| Trade #3 cannot be LIVE | **True in product** (vacuous). **Partially** locked by `== SHADOW` on one book. |
| `CanPromoteToLive` is tested for all states | **No.** Once, SHADOW. |
| `AfterHighEarlyScore` is tested | **No.** |
| Green score tests ⇒ go-live / §69 scoring box | **No.** |
| Product source was modified | **No.** |
| Test source was modified | **No.** |

**D34 answer, without greenwash:**

```text
BaselineScorerTests is a 74-line smoke: 3 facts, 7 asserts, 3/3 PASS.
It locks N=2 → INSUFFICIENT_DATA, N=3 clean winners → SHADOW + pin false,
and a losing 0.10→0.20→0.40 book → Martingale + RISK_BLOCKED.
It does not lock scores, detectors, A22, winning martingale, WATCH,
EARLY_SCORE, N=0, AfterHighEarlyScore, or ∀-state CanPromoteToLive.
Helper Closed() freezes SL / XAU / completed / no-avg-down, so most of
ComputeFeatures cannot go red.
```

*End of D34. Product source untouched. Test source untouched.*
