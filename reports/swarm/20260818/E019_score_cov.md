# E019 — `BaselineScorerTests` coverage (3 facts / 7 asserts, not A22)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E019_score_cov.md` |
| Agent | E019 (score coverage) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:49:25+05:30 (`dotnet test` start) … 13:49:27 (`3 passed / 0.3819 s`) |
| Assigned | **List `BaselineScorerTests`.** Write this file. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| Tests listed | `D:\Prop\tests\Unit\BaselineScorerTests.cs` |
| Tests SHA-256 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` |
| Tests size / lines / mtime | 2414 bytes / 74 file lines (68 non-blank) / 2026-08-18 13:17:42 +05:30 |
| Git | **untracked** (`?? tests/Unit/BaselineScorerTests.cs`) |
| SUT (read, not edited) | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| SUT size / lines / mtime | 8143 bytes / 212 file lines (187 non-blank) / 2026-08-18 13:08:10 +05:30 |
| Adjacent (read) | `ReconstructedTradeResult.cs` `EF41E774…` (2042 B); `TraderState.cs` `E509C59F…` (264 B); `FeatureQuality.cs`; `PriceSource.cs`; `DealIngestionService.RebuildTraderAsync`; `SeedingAndStoreTests`; `TradeReconstructionTests`; A22 / A27 / A69 / A89; B35; C02 / C17 / C32; D12 / D34 |
| Method | Full re-read of the test class + helper. Enumerate every `[Fact]`, every FluentAssertions call, every helper freeze. Map each `BaselineScore` / `FeatureSnapshot` / `TraderState` / SUT public member / `Score`/`ComputeFeatures`/`FromBaseline` branch to asserted / exercised / untouched. Hand-evaluate the three books against the live stub (same arithmetic as B35). Run `dotnet test --filter FullyQualifiedName~BaselineScorerTests`. Grep `tests/` for other `BaselineScorer` callers. Nothing answered from memory. Coverlet **not** collected this pass — percentages below are **assertion / branch inventories**, not instrumented line coverage. |
| `dotnet test` this pass | **2026-08-18 13:49:27 +05:30** — **3 passed / 0 failed / 0 skipped** / 0.3819 s / 0 Warning / 0 Error |

**One-line:** `BaselineScorerTests` is three `[Fact]`s and seven asserts. They lock B35 qualitative gates (N=2 → `INSUFFICIENT_DATA`; N=3 clean winners → `SHADOW`; losing 2×/2× → `Martingale` + `RISK_BLOCKED`). They do **not** lock scores, CVs, PF, drawdown, SL rate, averaging, escalation, `WATCH` / `EARLY_SCORE` / N=0, A22 `baseline.v1`, or the C32 winning-martingale `SHADOW` hole.

Do **not** claim “scoring is unit-tested.” Do **not** claim A22 Case A–F gold. Do **not** claim martingale cannot be `SHADOW`. Do **not** treat A89 `EXISTS` as a test class on disk.

---

## 0. Verdict (binding — do not greenwash)

| Check | Class | One-line |
|---|---|---|
| File exists / compiles / runs | **PASS** | 3/3 facts green this pass (0.38 s). |
| Inventory complete | **YES** | 1 class, 0 theories, 3 facts, 1 helper, 7 asserts. Listed in §1. |
| Facts == B35 qualitative trio | **PASS** | Same reconstructed books as FX-01 / FX-02 / FX-03. |
| Facts == B35 numeric gold | **NO** | Zero asserts on `RiskScore` / `BehaviorScore` / `EarlyQualityScore`. FX-01 `40`, FX-02 `100.00`, FX-03 `47.00` are unlocked. |
| Feature field coverage | **1 / 18** | Only `Features.Martingale` on fact 3. |
| Score-record field coverage | **3 / 6** (one partial) | `SuggestedState` ×3, `EarlyScoreEligible` ×2, `Features.Martingale` ×1. Numerics never. |
| Reachable `TraderState` locked | **3 / 5** | `INSUFFICIENT_DATA`, `SHADOW`, `RISK_BLOCKED`. **`WATCH` and `EARLY_SCORE` have zero facts.** |
| A22 `baseline.v1` locked | **NO** | Tests pin the **stub**. A `U(N)` / flag-floor / 1.80-ratio land stays green. |
| A89 #26–#41, FSM #75–#79 | **MISSING** | No `tests/Unit/Scoring/`. Named `ThreeTradeSafetyGateTests` is **not on disk**. |
| §60 drawdown / MFE / averaging-in-score | **MISSING / PARTIAL** | Drawdown never asserted. MFE never asserted. `AveragingDown` never asserted. Martingale is one **losing** 2×/2× book. |
| LIVE promotion locked for ∀ N=3 books | **PARTIAL** | One fixture asserts `== SHADOW` + `CanPromoteToLive(SHADOW)==false`. `AfterHighEarlyScore` never called. |
| Winning martingale / mild 1.26× | **UNLOCKED** | C32: quality **85.25 SHADOW** (mild) / **77.00 WATCH** (Case B). No fact. |
| Green tests ⇒ scoring done | **NO** | 7 assertions. Helper freezes SL / XAU / completed / no-avg-down. Most of the surface cannot fail. |

---

## 1. Complete list — `BaselineScorerTests`

Single public class. Namespace `TraderIntelligence.Tests.Unit` (not `.Scoring`). No `[Trait]`. No `[Theory]`. No `IClassFixture`. No `Skip`. No `InlineData`. Moq unused. No replay JSON. No `tests/Unit/Scoring/` folder.

Project: `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` (xUnit 2.5.3, FluentAssertions 6.12.0, TFM `net8.0`). Refs: Domain, Application, Fix.CTrader. **Not** Infrastructure / Mt5 — this class never needs them (it hand-builds `ReconstructedTradeResult`).

### 1.1 Members

| Kind | Name | Lines | Role |
|---|---|---|---|
| field | `_s` | 10 | `new BaselineScorer()` once per instance |
| `[Fact]` | `Two_trades_remain_insufficient` | 12–18 | N=2 winners → not eligible + `INSUFFICIENT_DATA` |
| `[Fact]` | `Three_disciplined_winners_go_to_shadow_not_live` | 20–27 | N=3 flat winners → eligible + `SHADOW` + pin false |
| `[Fact]` | `Martingale_after_losses_is_risk_blocked` | 29–41 | 0.10/0.20/0.40 losers → `Martingale` + `RISK_BLOCKED` |
| helper | `Closed(int n, decimal pnl, decimal lots = 0.10m)` | 43–73 | hand-built completed XAU long with SL |

### 1.2 Fully-qualified names (the list)

```text
TraderIntelligence.Tests.Unit.BaselineScorerTests.Two_trades_remain_insufficient
TraderIntelligence.Tests.Unit.BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live
TraderIntelligence.Tests.Unit.BaselineScorerTests.Martingale_after_losses_is_risk_blocked
```

Discovery this pass (xUnit adapter 2.5.3.1, testhost net8.0.30): **3 discovered, 3 started, 3 finished, 3 passed.**

### 1.3 Measured run (this pass)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~BaselineScorerTests --nologo --verbosity normal --no-restore

  Passed TraderIntelligence.Tests.Unit.BaselineScorerTests.Martingale_after_losses_is_risk_blocked [13 ms]
  Passed TraderIntelligence.Tests.Unit.BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live [< 1 ms]
  Passed TraderIntelligence.Tests.Unit.BaselineScorerTests.Two_trades_remain_insufficient [< 1 ms]

Test Run Successful.
Total tests: 3
     Passed: 3
 Total time: 0.3819 Seconds
Build succeeded. 0 Warning(s). 0 Error(s).
Time Elapsed 00:00:01.60
```

Same SHA-256 as D34 / C02 / C17. Bytes have not moved since 13:17:42.

---

## 2. Assertion inventory (complete — 7 calls)

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
        var trades = new[]
        {
            Closed(1, -100, lots: 0.10m),
            Closed(2, -200, lots: 0.20m),
            Closed(3, -400, lots: 0.40m)
        };
        var score = _s.Score(trades);
        score.Features.Martingale.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.RISK_BLOCKED);
    }
```

| # | Fact | Assert | What it can fail |
|---|---|---|---|
| 1 | `Two_trades_remain_insufficient` | `EarlyScoreEligible == false` | `EarlyScoreTradeCount` dropped to 2 (or eligibility formula changed) |
| 2 | same | `SuggestedState == INSUFFICIENT_DATA` | N=2 started landing `EARLY_SCORE` / `SHADOW` / anything else |
| 3 | `Three_disciplined_winners_go_to_shadow_not_live` | `EarlyScoreEligible == true` | threshold raised above 3 |
| 4 | same | `SuggestedState == SHADOW` | SHADOW predicate moved; **also the only unit lock against `LIVE` on this book** |
| 5 | same | `CanPromoteToLive(SHADOW) == false` | pin flipped; **only SHADOW is passed** |
| 6 | `Martingale_after_losses_is_risk_blocked` | `Features.Martingale == true` | detector ratio raised past 2.00, or pair walk broken |
| 7 | same | `SuggestedState == RISK_BLOCKED` | losing-martingale clause removed **and** `risk` stays `< 80` (this book is risk **60**) |

No `BeApproximately`. No numeric compare. No forbidden-state pair (`!= LIVE` **and** `!= LIVE_CANDIDATE`). No `CompletedXauTrades` count. No `ComputeFeatures` direct call.

| Entry | Called by tests? |
|---|---|
| `Score(IReadOnlyList<ReconstructedTradeResult>)` | **Yes** — only entry (3 times) |
| `ComputeFeatures` | **No** (only via `Score`) |
| `TraderStateMachine.FromBaseline` | **No** (only via `Score`) |
| `AfterHighEarlyScore` | **No** |
| `CanPromoteToLive` | **Once**, argument = the SHADOW landing |
| `EarlyScoreTradeCount` | **Not asserted** (implicit via N=2 / N=3) |

---

## 3. Coverage matrix — records

### 3.1 `BaselineScore` (6 fields)

| Field | Asserted? | Where | Coverage |
|---|---|---|---|
| `Features` (object) | only `.Martingale` | fact 3 | **PARTIAL** |
| `RiskScore` | **never** | — | **0** |
| `BehaviorScore` | **never** | — | **0** |
| `EarlyQualityScore` | **never** | — | **0** |
| `SuggestedState` | yes | all 3 facts | **YES** (3 tokens) |
| `EarlyScoreEligible` | yes | facts 1–2; **not** fact 3 | **PARTIAL** |

Fact 3 is eligible (`N=3`) and does not say so. A later bug that set `EarlyScoreEligible=false` on a blocked book would stay green.

**Asserted field ratio: 3 / 6** if `Features` counts as one hit; **2 / 6 + 1 nested bool** if scored honestly.

### 3.2 `FeatureSnapshot` (18 fields)

| Field | Asserted in this class? | Exercised by a fixture? |
|---|---|---|
| `Martingale` | **yes** (fact 3 only) | fact 3 true; facts 1–2 false (unasserted) |
| `CompletedXauTrades` | no | 2 / 3 / 3 internally |
| `NetPnl` | no | +20 / +240 / −700 |
| `GrossProfit` | no | 20 / 240 / 0 |
| `GrossLoss` | no | 0 / 0 / 700 |
| `ProfitFactor` | no | 99 / 99 / 0 |
| `LotCv` | no | 0 / 0 / 0.5345 (fact 3 > 0.5, unasserted) |
| `LossSizeCv` | no | 0 / 0 / 0.5345 (not > 0.80) |
| `AveragingDown` | no | always false (helper freeze) |
| `LotEscalation` | no | **true on fact 3**, never asserted |
| `AverageHoldSeconds` | no | always 1800 |
| `SlUseRate` | no | always 1 (helper `InitialSl = 2290`) |
| `MaxDrawdown` | no | 0 / 0 / 700 |
| `TradeFrequencyPerDay` | no | 2 / 3 / 3 (`spanDays` floored at 1) |
| `MaeMfeQuality` | no | default `Unavailable` |
| `AverageMfe` | no | null |
| `AverageMae` | no | null |
| `PriceSource` | no | default `Unknown` |

**1 / 18 snapshot fields have a fact.** The equity path, population CV, PF=99 cap, SL rate, hold time, and MFE-unavailable default are implementation-only.

---

## 4. Coverage matrix — SUT members and branches

`BaselineScorer.cs` is one compilation unit, four types: `FeatureSnapshot`, `BaselineScore`, `BaselineScorer`, `TraderStateMachine`.

### 4.1 Public / internal surface

| Member | Hit by this class? | Locked? |
|---|---|---|
| `BaselineScorer.EarlyScoreTradeCount` (`= 3`) | implicit | **PARTIAL** (N=2 / N=3 pair) |
| `ComputeFeatures` empty-list early return (L45–64) | **no** | **NO** |
| `ComputeFeatures` filter `Completed && IsXauUsd` | never fed junk | **NO** |
| `ComputeFeatures` `OrderBy(ClosedAt)` only | helper times unique | **NO** |
| equity / `MaxDrawdown` walk | fact 3 produces 700 | **NO** (unasserted) |
| martingale pair `> 1.25×` after loss | fact 3 2.00× | **PARTIAL** (one polarity) |
| lot-escalation pair `> 1.50×` | fact 3 true | **NO** |
| hold-seconds average | always 1800 | **NO** |
| `spanDays = Max(1, days)` | all books < 1 day | **NO** (multi-day untested) |
| PF: `GL<=0 && GP>0 → 99` | facts 1–2 | **NO** |
| PF: `GL<=0 && GP==0 → 0` | fact 3 | **NO** |
| PF: `GL>0 → GP/GL` | **no mixed book** | **NO** |
| `CoefficientOfVariation` `Count<2 → 0` | facts 1–2 lots; empty losses | **NO** |
| `CoefficientOfVariation` `mean==0 → 0` | **no** | **NO** |
| `CoefficientOfVariation` population stdev | fact 3 lots/losses | **NO** |
| `AveragingDown = Any(WasAveragedDown)` | helper forces false | **NO** |
| `SlUseRate` (`InitialSl > 0`) | helper always 2290 | **NO** |
| `MaeMfeQuality = Unavailable` / `PriceSource = Unknown` | always | **NO** |
| `Score` risk addends (35/20/15/10/10/10, cap 100) | fact 3 hits 35+15+10=60 | **NO** (60 unasserted) |
| `Score` behavior deductions | fact 3 → 60 | **NO** |
| `Score` quality `NET>0` / PF steps / `U(N)`-less 50-base | facts 1–2 | **NO** |
| `Score` `N<3` quality cap 40 | fact 1 internally | **NO** |
| `FromBaseline` `N==0` | **no** | **NO** |
| `FromBaseline` `risk>=80` | **no** | **NO** |
| `FromBaseline` martingale ∧ DD>0 ∧ NET<0 | **fact 3** | **YES** (state only) |
| `FromBaseline` `!earlyEligible` | **fact 1** | **YES** (state + eligible) |
| `FromBaseline` `quality>=70 && risk<40` → SHADOW | **fact 2** | **YES** (state) |
| `FromBaseline` `quality>=55` → WATCH | **no** | **NO** |
| `FromBaseline` else → EARLY_SCORE | **no** | **NO** |
| `AfterHighEarlyScore() => SHADOW` | **never called** | **NO** |
| `CanPromoteToLive(_) => false` | once, SHADOW | **PARTIAL** |

### 4.2 `TraderState` tokens

`FromBaseline` reachable set: `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. Enum also has `{LIVE_CANDIDATE, LIVE, PAUSED, DISQUALIFIED}` (`TraderState.cs` values 0–8).

| Token | Reachable from stub? | Locked by a fact? |
|---|---|---|
| `INSUFFICIENT_DATA` | yes (`N==0` or `N<3`) | **yes** — N=2 winners only (N=0 / N=1 untested) |
| `SHADOW` | yes (`N>=3`, `quality>=70`, `risk<40`) | **yes** — N=3 flat winners + SL |
| `RISK_BLOCKED` | yes (`risk>=80` **or** losing martingale with DD) | **yes** — losing 2×/2× (`risk=60`, clause 2) |
| `WATCH` | yes | **no** |
| `EARLY_SCORE` | yes | **no** |
| `LIVE` / `LIVE_CANDIDATE` | **no** (vacuous) | only indirectly (`== SHADOW` on one book) |
| `PAUSED` / `DISQUALIFIED` | **no** | **no** |

N=0 empty list → `INSUFFICIENT_DATA` is **untested**. N=1 is untested. N=4+ rescore is untested.

### 4.3 Helper `Closed` — what the suite cannot see

```43:73:D:\Prop\tests\Unit\BaselineScorerTests.cs
    private static ReconstructedTradeResult Closed(int n, decimal pnl, decimal lots = 0.10m) =>
        new()
        {
            CanonicalSymbol = "XAUUSD",
            Completed = true,
            InitialSl = 2290,
            WasAveragedDown = false,
            Direction = TradeDirection.Long,
            OpenedAt = DateTimeOffset.UnixEpoch.AddHours(n),
            ClosedAt = DateTimeOffset.UnixEpoch.AddHours(n).AddMinutes(30),
            // lots default 0.10; Net == Gross == pnl; commission/swap/fees = 0
        };
```

| Knob | Frozen | Branch hidden |
|---|---|---|
| Symbol | always `XAUUSD` | `IsXauUsd` filter never strips a EURUSD spacer |
| `Completed` | always `true` | incomplete rows never dropped |
| `ClosedAt` | unique, +n hours +30 min | equal-close order flip (C32 `SAME_CLOSE_*`) unreachable |
| `InitialSl` | `2290` | `SlUseRate<0.3` +10 risk / `<0.5` −10 behavior never fire |
| `WasAveragedDown` | `false` | +20 risk averaging path dead here (C51) |
| `lots` default | `0.10` | facts 1–2 never size-up |
| Direction | Long | shorts untested for no reason (stub ignores side) |
| VWAP | 2300 / 2301 | scorer does not read VWAP; fabricated-MFE refusal (A45) untested |
| Fees | 0 | `Net == Gross`; fee sign flip of `NET>0` +15 untested |

The helper is the right shape for B35 reconstructed gold. It is a **bad** shape for a feature suite.

---

## 5. The three facts vs live stub gold (numbers **not** locked)

Constants = current additive stub. Gold source = B35 measured / C32 re-measured.

### 5.1 `Two_trades_remain_insufficient` ≡ B35-FX-01 ≡ A22 Case D (qualitative)

```text
input: Closed(1, +10), Closed(2, +10)     // lots 0.10, SL yes
N=2  NET=+20  GP=20  GL=0  PF=99  lot_cv=0  sl=1  mart=false  dd=0
risk=0  behavior=100
quality raw = 50+15+10+5+20 = 100  →  N<3 cap → 40
eligible=false  →  INSUFFICIENT_DATA
```

| Locked | Unlocked gold |
|---|---|
| `EarlyScoreEligible == false` | `CompletedXauTrades == 2` |
| `SuggestedState == INSUFFICIENT_DATA` | `EarlyQualityScore == 40`, `RiskScore == 0`, `BehaviorScore == 100` |
| | `!= LIVE`, `!= EARLY_SCORE`, `!= SHADOW` |
| | huge-NET cousin (`+800,+900`) still capped at 40 |

Architecture §15 / A22 I3 “N=2 is not official”: **qualitative lock yes.** Persist still writes quality=40 (`RebuildTraderAsync` always upserts) — this class never looks at persist.

### 5.2 `Three_disciplined_winners_go_to_shadow_not_live` ≡ B35-FX-02 (not A22 Case A)

```text
input: Closed(1, +80), Closed(2, +70), Closed(3, +90)
N=3  NET=+240  GP=240  GL=0  PF=99  lot_cv=0  sl=1  dd=0
risk=0  behavior=100
quality = 50+15+10+5+20 = 100.00     // A22 after U(3)=18 is ≤82
eligible=true  quality>=70 ∧ risk<40  →  SHADOW
CanPromoteToLive(SHADOW)=false
```

A22 Case A is `+80,−40,+60` (mixed). This fixture is **three winners**. Both land `SHADOW` on the stub; only this winner book is tested.

| Locked | Unlocked |
|---|---|
| eligible true | `RiskScore == 0`, `BehaviorScore == 100`, `EarlyQualityScore == 100.00` |
| `== SHADOW` | `!= LIVE` **and** `!= LIVE_CANDIDATE` as a pair |
| `CanPromoteToLive(SHADOW)==false` | `AfterHighEarlyScore() == SHADOW` |
| | `CanPromoteToLive` for the other 8 enum values |

The name says “not live.” The SHADOW equality is the **only** unit lock that would go red if `FromBaseline` started returning `LIVE` on this book.

Landing `U(N)` will **keep this fact green** because 100.00 is not asserted.

### 5.3 `Martingale_after_losses_is_risk_blocked` ≡ B35-FX-03 (not A22 Case B)

```text
input: 0.10/−100, 0.20/−200, 0.40/−400   (SL yes, no avg-down)
pair 1→2: −100 < 0 and 0.20 > 0.10*1.25  → Martingale
           0.20 > 0.10*1.50               → LotEscalation
lots {0.10,0.20,0.40} LotCv = 0.5345 > 0.5
losses {100,200,400} LossSizeCv = 0.5345  (not > 0.80)
GP=0 so DD>GP addend is off
risk = 35+15+10 = 60     // not ≥ 80
behavior = 100−30−10 = 60
quality = 50 + 12 − 15 = 47.00
block via: Martingale ∧ MaxDrawdown>0 ∧ NetPnl<0
```

| Locked | Unlocked |
|---|---|
| `Martingale == true` | `LotEscalation == true` (true on this book) |
| `SuggestedState == RISK_BLOCKED` | `RiskScore == 60` (a `>= 80` assert would **fail**) |
| | `EarlyQualityScore == 47.00`, `NetPnl == −700`, `MaxDrawdown == 700` |
| | `EarlyScoreEligible == true` |

**Trap (B35 §7.7 / C32 `CASEB_SL`):** flip the last print to `+800` → same 2×/2× pattern, `Martingale==true`, state becomes **`WATCH`**, quality **77.00**. This class has no such fact. `docs/scoring.md` L9 (“Martingale / large sequential size-up after losses ⇒ `RISK_BLOCKED`”) over-claims relative to the stub **and** relative to this test (the test only feeds a **losing** book).

---

## 6. A22 Case A–F vs this class

| A22 case | Spec outcome | Stub today | Locked by `BaselineScorerTests`? |
|---|---|---|---|
| A clean mixed `+80,−40,+60` | `SHADOW`, quality ≤ 82 | `SHADOW` / **100.00** | **No** (winner cousin only) |
| B winning 2×/2× last +800 | `RISK_BLOCKED`, quality low | **`WATCH` / 77.00** | **No** (losing cousin only) |
| C mixed, no SL, 1.40× after win | `EARLY_SCORE` or `WATCH` | generous SHADOW if SL exists | **No** |
| D N=2 | `INSUFFICIENT_DATA` | `INSUFFICIENT_DATA` / quality 40 | **Qualitative yes** |
| E N=20 good shadow | `LIVE_CANDIDATE`, never auto LIVE | still cannot emit LIVE* | **No** (no N=20, no LIVE_CANDIDATE path) |
| F `MIN_LIVE_TRADES=3` attack | loader reject + R5 | no `ScoreConfig` | **No** |

Invariant lock (same as D34; re-checked against current bytes):

| ID | Invariant | Test lock? |
|---|---|---|
| I1 | completed XAU only | **No** (helper never feeds junk) |
| I2 | Trade #3 emits `EARLY_SCORE_ELIGIBLE` event | **No** (bool only; no outbox) |
| I3 | first official score then rescore 4, 5, … | **No** (no N=4) |
| I4 | N=3 ∉ {LIVE, LIVE_CANDIDATE} | **PARTIAL** — one book `== SHADOW` |
| I5 | high score @3 → SHADOW | **PARTIAL** — clean winners only |
| I6 | as-of / no future leakage | **No** |
| I7 | do not fabricate MFE | **No** |
| I8 | destination P&L out of formulas | **No** (never fed) |
| I9 | do not rank by raw NET | **FAIL in product** (`NetPnl>0 → +15`); **no test** |
| I10 | risk engine final authority | **No** compose |
| I11 | pure function, no `UtcNow` | **PASS in SUT**; no determinism fact |
| I12 | versioned `ScoreConfig` | **MISSING** in SUT and tests |

---

## 7. A89 named scoring classes vs on-disk

A89 §5.3 / §5.6 status `EXISTS` means **the SUT type exists**, not the test class. On disk for scoring: **this one smoke file**.

| A89 # | Specified class | On disk? | Covered by `BaselineScorerTests`? |
|---|---|---|---|
| 26 | `BaselineScorerFeatureSnapshotTests` | **No** | empty-list / filter / order — **no** |
| 27 | `DrawdownCalculatorTests` | **No** | **no** |
| 28 | `MartingaleDetectorTests` | **No** | one losing 2× sliver |
| 29 | `AveragingDownDetectorTests` | **No** | helper forces false |
| 30 | `LotEscalationDetectorTests` | **No** | true on FX-03, unasserted |
| 31 | `ProfitFactorAndNetPnlTests` | **No** | **no** |
| 32 | `LotCvAndLossSizeCvTests` | **No** | **no** |
| 33 | `SlUseAndHoldTimeTests` | **No** | helper freezes both |
| 34 | `TraderScoreCalculatorTests` | **No** | no 0–100 / I9 pair |
| 35 | `EarlyQualityUncertaintyPenaltyTests` | **No** | no `U(N)` in SUT |
| 36–37 | MFE/MAE classes | **No** | **no** |
| 38 | `ScoringAsOfNoFutureLeakageTests` | **No** | **no** |
| 39 | `ScoreHistoryAppendContractTests` | **No** | persist out of this class |
| 40 | `ScoringCannotBypassRiskContractTests` | **No** | never composes `RiskEngine` |
| 41 | `ReconstructionScoringServiceScoreFieldsTests` | **No** | integration seed only |
| 75 | `TraderStateMachineFromBaselineTests` | **No** | 3 of 5 reachable tokens collapsed here |
| 76 | `ThreeTradeSafetyGateTests` | **No** | one SHADOW pin |
| 77–79 | graph / DQ / rescoring N=3/4/5 | **No** | **no** |

`BaselineScorerTests` is a **name A89 does not list**. It is a pre-inventory smoke that happened to land.

Architecture §60 scoring bullets (C17 unchanged this re-read):

| §60 item | This class | Status |
|---|---|---|
| drawdown | no `MaxDrawdown` assert | **MISSING** |
| MFE/MAE | no `MaeMfeQuality` assert | **MISSING** |
| martingale detection | one positive 2×/2× loser | **PARTIAL** |
| averaging-down (score flag) | helper forces false | **MISSING** |
| score-state transitions | 3 of 5 reachable tokens | **PARTIAL** |

---

## 8. Adjacent tests that are *not* this class

Grep of `D:\Prop\tests` for `BaselineScorer` / `CanPromoteToLive` / `SuggestedState`: **this file** plus integration `SeedingAndStoreTests` constructing `new BaselineScorer()`. That is the entire automated score surface.

| Location | What it proves | Overlap / gap |
|---|---|---|
| `TradeReconstructionTests.First_three_completed_xau_unlocks_early_score` | reconstructor `CountCompletedXauUsdTrades==3` and `IsEarlyScoreEligible==true` | Dual latch: reconstructor `>= 3` vs scorer `CompletedXauTrades >= EarlyScoreTradeCount`. A drift (reconstructor 3, scorer 4) would split eligibility. |
| `TradeReconstructionTests.Canceled_deal_on_a_position_excludes_it_from_first_three` | canceled deal drops first-3 eligibility | Never scores the remaining book |
| `SeedingAndStoreTests` login `10001` | `CompletedXauTrades==3` and `CurrentState != LIVE` | **Not** `== SHADOW`. Demo tape is mixed P&L, **no SL**. Vacuous vs LIVE. |
| `SeedingAndStoreTests` login `10002` | `== RISK_BLOCKED` | Demo cousin of FX-03 (lots 0.10/0.20/0.40, different P&L, no SL). Must not overwrite FX-03 gold (`60/60/47.00`). |
| `RiskEngineTests` | `MartingaleFlag` default false on the happy request | FIX/copy brake, **not** score state. No compose with `BaselineScorer`. |
| C32 harness `_tmp_c32_score/` | 43 adversarial books | **Not** a unit test. Permanent measurement, not a regression lock. |

Persist path (`DealIngestionService.RebuildTraderAsync` L86–101) copies `RiskScore` / `BehaviorScore` / `EarlyQualityScore` / flags / `CurrentState = score.SuggestedState`. **No `CanPromoteToLive` call.** This class cannot catch a persist-side LIVE write.

---

## 9. Mutation / hole list these facts will not catch

If a later coder changes only product (tests frozen):

| Product edit | This class | Notes |
|---|---|---|
| `EarlyScoreTradeCount` 3 → 2 | **fact 1 red** | N=2 would become eligible + SHADOW |
| `EarlyScoreTradeCount` 3 → 4 | **fact 2 red** | N=3 would stay insufficient |
| SHADOW predicate never | **fact 2 red** | landing WATCH/EARLY_SCORE |
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
| Persist writes LIVE without pin | **stays green** | Application not called |
| `MaeMfeQuality` fabricated from VWAP | **stays green** | MFE unused |

**Read:** the suite is a **state-machine smoke** for three canned books. It is not a formula lock and not an adversarial lock.

Uncovered holes already named by B12 / C02 / C32 / D12 / D34 (still current vs these bytes):

- Mild winning martingale → `SHADOW` (quality 85.25, risk 35)
- A22 Case B winning 2×/2× → `WATCH` (77.00) not `RISK_BLOCKED`
- Exact `1.25×` after loss is **not** martingale (`>` not `>=`)
- Exact `1.50×` is martingale **without** escalation
- Size-up after a **win** is escalation, not martingale
- Breakeven / +0.01 spacer defeats adjacent-pair detector
- `N<3` losing martingale is `RISK_BLOCKED` (clause before eligibility) — untested
- `risk >= 80` stack (avg+esc+no-SL)
- Empty book: C23 `SlUseRate=0` still scores risk 10 / behavior 90 / quality 40, state `INSUFFICIENT_DATA`
- `AfterHighEarlyScore` dead pin

---

## 10. Coverage scorecard (honest fractions)

| Surface | Hit | Locked | Denominator |
|---|---:|---:|---|
| `[Fact]` methods | 3 | 3 green | 3 on disk |
| FluentAssertions | 7 | 7 | 7 |
| `BaselineScore` fields | 3 | 2 full + 1 nested bool | 6 |
| `FeatureSnapshot` fields | 1 | 1 | 18 |
| `TraderState` reachable tokens | 3 | 3 | 5 |
| `TraderState` enum values | 3 | 3 (plus vacuous LIVE via SHADOW) | 9 |
| `FromBaseline` branches | 3 | 3 | 6 (`N==0`, `risk>=80`, losing-mart, `!eligible`, SHADOW, WATCH, else — 7 if split) |
| `ComputeFeatures` early-empty | 0 | 0 | 1 |
| Detector flags (`Martingale` / `AveragingDown` / `LotEscalation`) | 1 / 0 / 0 | 1 / 0 / 0 | 3 |
| Numeric scores | 0 | 0 | 3 (`Risk` / `Behavior` / `Quality`) |
| A22 cases A–F | 1 qualitative (D) | 0 numeric | 6 |
| A22 I1–I12 | 0 full | 2 partial (I4, I5) | 12 |
| A89 scoring/FSM classes #26–41 + #75–79 | 0 files | 0 | 21 |
| Persist / history / risk compose | 0 | 0 | — |

**Headline coverage of the scoring SUT by this class: smoke-level state gates only.** A line-coverage tool would over-report because `Score` walks almost every addend on the three books while **zero addends are asserted**.

Do not quote a “% line coverage” from this file. None was collected.

---

## 11. Relation to sibling reports

| Report | Role vs this file |
|---|---|
| **D34** | Same three facts; cut was **surface / mutation / what stays green**. Bytes unchanged (`61E34A07…` / `ECA2EEE8…`). E019 is the **coverage matrix** cut (fields, branches, A22 cases, A89, fractions). |
| C02 | Assigned question was **no LIVE promotion**. Still valid. |
| D12 | Scorer review (SUT). Tests section is a subset of C02. |
| B35 | Canonical fixture catalog. This class **is** FX-01/02/03 at the reconstructed layer, without numeric gold or deal tapes. |
| C32 | Proves quality ≥ 70 **with** `Martingale==true`. This class does not constrain that. |
| C17 | §60 map: this class is the only scoring suite; 3 pass. Drawdown still MISSING. |
| D97 | `CanPromoteToLive => false` re-measure. One SHADOW call here; no ∀-state theory. |
| A89 | Backlog. Status `EXISTS` ≠ test on disk. |
| B12 “Tests? None” | **Stale.** Use C02 / D34 / this file. |

---

## 12. Honesty / do-not-claim

| Claim | Allowed? |
|---|---|
| `BaselineScorerTests` exists and is 3/3 green (this pass) | **Yes. Measured** at 13:49:27 +05:30. |
| The complete list is the three FQNs in §1.2 | **Yes.** |
| Those 3 facts match B35 qualitative outcomes | **Yes.** |
| Numeric scores are regression-locked | **No.** |
| Feature coverage is complete | **No. 1 / 18.** |
| `WATCH` / `EARLY_SCORE` / N=0 are tested | **No.** |
| A22 `baseline.v1` is tested | **No.** |
| A89 scoring / FSM classes are on disk | **No.** |
| Martingale cannot reach SHADOW / quality ≥ 70 | **False** (C32). Tests only lock the **losing** 2× book. |
| Trade #3 cannot be LIVE | **True in product** (vacuous). **Partially** locked by `== SHADOW` on one book. |
| `CanPromoteToLive` is tested for all states | **No.** Once, SHADOW. |
| `AfterHighEarlyScore` is tested | **No.** |
| Green score tests ⇒ go-live / §69 scoring box | **No.** |
| Instrumented line coverage was collected | **No.** |
| Product source was modified | **No.** |
| Test source was modified | **No.** |

**E019 answer, without greenwash:**

```text
BaselineScorerTests list (complete):
  Two_trades_remain_insufficient
  Three_disciplined_winners_go_to_shadow_not_live
  Martingale_after_losses_is_risk_blocked
3 facts, 7 asserts, 3/3 PASS (13:49:27 +05:30).
Coverage: SuggestedState 3/5 reachable tokens; FeatureSnapshot 1/18;
numeric scores 0/3; A22 cases 1 qualitative / 6; A89 scoring classes 0/21.
Helper Closed() freezes SL / XAU / completed / no-avg-down.
This is a state-machine smoke, not a score-formula suite.
```

*End of E019. Product source untouched. Test source untouched.*
