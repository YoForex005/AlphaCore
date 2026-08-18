# B35 — Scoring fixtures (N=2 insufficient; N=3 good → SHADOW; martingale → RISK_BLOCKED)

| Field | Value |
|---|---|
| Agent | B35 (senior engineer, fixture design only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Scorer under test | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` + `TraderStateMachine` |
| Reconstruction | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| Existing unit surface | `D:\Prop\tests\Unit\BaselineScorerTests.cs` |
| Existing demo tape | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (`DemoBrokerFactory`) |
| Spec (target, not current gold) | `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` |
| Replay envelope | `D:\Prop\reports\swarm\20260818\A67_replay_harness.md` |
| Score family pinned here | **`baseline_v0`** = code as of this date (measured) |

---

## 0. Verdict

Three **acceptance fixtures** are locked. Qualitative outcomes are **hard** (a flip is a build break). Numeric gold is **measured** from `BaselineScorer.Score` on 2026-08-18 (Release, `net8.0`, invariant culture).

| Fixture id | N completed XAU | Pattern | `EarlyScoreEligible` | `SuggestedState` | Never |
|---|---|---|---|---|---|
| `B35-FX-01` `score.n2.insufficient` | **2** | two disciplined winners | `false` | **`INSUFFICIENT_DATA`** | `EARLY_SCORE`, `WATCH`, `SHADOW`, `LIVE*`, `RISK_BLOCKED` |
| `B35-FX-02` `score.n3.shadow` | **3** | three disciplined winners | `true` | **`SHADOW`** | `LIVE`, `LIVE_CANDIDATE`, `PROVEN_PROFITABLE` |
| `B35-FX-03` `score.n3.martingale.risk_blocked` | **3** | 2× size-up after each loss | `true` | **`RISK_BLOCKED`** | `SHADOW`, `LIVE`, `LIVE_CANDIDATE` |

These three already exist as unit facts in `BaselineScorerTests` (reconstructed-trade layer). This document is the **canonical fixture catalog**: reconstructed rows, deal tapes, expected `FeatureSnapshot` / scores, noise rules, and the A22 deltas so a later `baseline.v1` calculator cannot silently rewrite the cases.

**Do not implement new product code from this ticket.** Gold JSON under `tests/Replay/Fixtures/` is specified, not created.

---

## 1. Why these three

Architecture §15 / §22–23 and `docs/scoring.md`:

1. Trade #3 is the first official score (`EARLY_SCORE_ELIGIBLE`). Two completed XAU lifecycles are **not** a score.
2. Trade #3 + high quality + low risk → **SHADOW only**. Never automatic real capital. `TraderStateMachine.CanPromoteToLive` is hard-`false`.
3. Sequential size-up after losses is martingale → **`RISK_BLOCKED`**. Dollars do not buy a live seat.

The fixtures isolate those three gates. They are not a full A22 Case A–F matrix (see §11).

---

## 2. Source of law (binding for these gold files)

| Layer | Binding? | Path |
|---|---|---|
| Implemented scorer | **Yes — numeric + state gold** | `BaselineScorer` / `TraderStateMachine.FromBaseline` |
| Implemented reconstructor | **Yes — deal-tape gold** | `TradeReconstructor` + `VolumeConverter.Manager` (`scale = 10_000`) |
| Unit tests | **Yes — already encode the three facts** | `Two_trades_remain_insufficient`, `Three_disciplined_winners_go_to_shadow_not_live`, `Martingale_after_losses_is_risk_blocked` |
| Demo seed | **Qualitative cousin, not gold** | logins `10001` / `10002` / `99001` / `10003` — no SL on IN deals; `10003` is N=0 not N=2 |
| A22 `baseline.v1` | **Not gold for this document** | Different martingale ratio (1.80 vs 1.25), sample CV, U(N) penalty, flag floors. Outcomes for FX-01/02/03 still match qualitatively |
| A67 replay spec | **Envelope only** | Use `ti.replay.fixture/v1` when JSON is later written. Set `config.score.version = "baseline_v0"` and `expect.scores.mode = "numeric"` |

If A22 lands as a new calculator, **do not overwrite** these fixtures. Add `score.n2.insufficient.v1` etc. and keep `baseline_v0` as the regression pin.

---

## 3. Implemented rules that the fixtures exercise

Copied from the current scorer (do not treat A22 numbers as live):

```text
EarlyScoreTradeCount = 3

Universe:
  completed && IsXauUsd, ordered by ClosedAt
  (incomplete, non-XAU, balance deals are dropped before N)

Martingale (adjacent pair i-1, i):
  prev.NetRealizedPnl < 0
  AND curr.MaxVolumeLots > prev.MaxVolumeLots * 1.25

LotEscalation (adjacent pair):
  curr.MaxVolumeLots > prev.MaxVolumeLots * 1.50

SL counts if InitialSl.GetValueOrDefault() > 0

risk:
  +35 Martingale
  +20 AveragingDown
  +15 LotEscalation
  +10 LotCv > 0.50
  +10 SlUseRate < 0.30
  +10 MaxDrawdown > 0 AND GrossProfit > 0 AND MaxDrawdown > GrossProfit
  clamp 0..100

behavior:
  100
  −30 Martingale
  −15 AveragingDown
  −10 LotCv > 0.40
  −10 SlUseRate < 0.50
  −10 LossSizeCv > 0.80
  clamp 0..100

quality:
  50
  +15 NetPnl > 0
  +10 ProfitFactor >= 1.2
  +5  ProfitFactor >= 1.8
  +0.20 * behavior
  −0.25 * risk
  if N < 3: quality = Min(quality, 40)
  Round2, clamp 0..100

ProfitFactor:
  GL <= 0 && GP > 0 → 99
  GL <= 0 && GP == 0 → 0
  else Round4(GP / GL)

LotCv / LossSizeCv:
  population CV = stdev_pop / mean, Round4
  n < 2 → 0

State (first match):
  N == 0                              → INSUFFICIENT_DATA
  risk >= 80
    OR (Martingale && MaxDrawdown > 0 && NetPnl < 0)
                                      → RISK_BLOCKED
  !eligible (N < 3)                   → INSUFFICIENT_DATA
  quality >= 70 && risk < 40          → SHADOW
  quality >= 55                       → WATCH
  else                                → EARLY_SCORE

CanPromoteToLive(*) = false always
```

**Load-bearing order:** the martingale+losing-book clause runs **before** the N<3 clause. Two losing martingale trades are `RISK_BLOCKED`, not `INSUFFICIENT_DATA`. FX-01 therefore uses **winners, constant lots** so N=2 is a pure insufficient-data case.

---

## 4. Shared constants (all three gold fixtures)

| Constant | Value |
|---|---|
| `broker_code` | `ACHIEVER` |
| `score_version` | `baseline_v0` |
| `canonical_symbol` | `XAUUSD` |
| `source_symbol` | `XAUUSD` |
| Volume scale | `10_000` → `0.10` lots = native `1000` |
| Direction | Long (`Buy` IN / `Sell` OUT) |
| Hold | 30 minutes (`1800` s) every lifecycle |
| `InitialSl` | `2290` on the IN deal (`price_sl`) so `SlUseRate = 1` |
| Commission / swap / fees | `0` (net = deal `profit` on OUT) |
| Clock | `2026-06-01T08:00:00.000Z` + 1 hour per trade |
| `OpenedAt` | `t0 + (n-1) hours` |
| `ClosedAt` | `OpenedAt + 30 minutes` |
| `EntryVwap` / `ExitVwap` | see tapes; scorer does not use prices for these three |
| MFE/MAE | unused (`FeatureQuality.Unavailable`) |
| Group / plan | ignored (not a score input) |

Reserved lab logins (do not collide with demo `10001–10003`, `99001`):

| Login | Fixture |
|---|---|
| `35001` | FX-01 |
| `35002` | FX-02 |
| `35003` | FX-03 |

Position ids `501–503` / `601–603` / `701–703`. Deal tickets `10001+seq` (IN) and `10501+seq` (OUT).

---

## 5. Fixture `B35-FX-01` — two trades remain insufficient

### 5.1 Intent

Prove that **N=2 never unlocks an official state**, even with perfect process and positive P&L. Quality is computed then **capped at 40**. Leaderboard must not rank this book.

### 5.2 Reconstructed trades (scorer input)

| n | `PositionId` | `MaxVolumeLots` | `NetRealizedPnl` | `InitialSl` | `Completed` | `CanonicalSymbol` |
|---|---|---|---|---|---|---|
| 1 | 501 | `0.10` | `+10` | `2290` | true | XAUUSD |
| 2 | 502 | `0.10` | `+10` | `2290` | true | XAUUSD |

Same shape as `BaselineScorerTests.Two_trades_remain_insufficient` (`Closed(1, 10), Closed(2, 10)`).

### 5.3 Measured `FeatureSnapshot`

| Field | Gold |
|---|---|
| `CompletedXauTrades` | `2` |
| `NetPnl` | `20` |
| `GrossProfit` | `20` |
| `GrossLoss` | `0` |
| `ProfitFactor` | `99` |
| `LotCv` | `0` |
| `LossSizeCv` | `0` |
| `Martingale` | `false` |
| `AveragingDown` | `false` |
| `LotEscalation` | `false` |
| `AverageHoldSeconds` | `1800` |
| `SlUseRate` | `1` |
| `MaxDrawdown` | `0` |
| `TradeFrequencyPerDay` | `2` (span < 1 day → divisor forced to `1`) |
| `MaeMfeQuality` | `Unavailable` |

### 5.4 Measured `BaselineScore`

| Field | Gold | Why |
|---|---|---|
| `EarlyScoreEligible` | `false` | `2 < EarlyScoreTradeCount` |
| `RiskScore` | `0` | no flags |
| `BehaviorScore` | `100` | no deductions |
| `EarlyQualityScore` | `40` | raw would be `100`; **N<3 cap** |
| `SuggestedState` | **`INSUFFICIENT_DATA`** | `!eligible` after empty/risk checks |

Raw quality before cap: `50 + 15 (net>0) + 10 (PF≥1.2) + 5 (PF≥1.8) + 20 (behavior) − 0 = 100` → `Min(100, 40) = 40`.

### 5.5 Hard asserts

```text
N == 2
EarlyScoreEligible == false
SuggestedState == INSUFFICIENT_DATA
SuggestedState ∉ { EARLY_SCORE, WATCH, SHADOW, LIVE_CANDIDATE, LIVE, RISK_BLOCKED, DISQUALIFIED }
CanPromoteToLive == false
Martingale == false
```

Huge P&L must not escape the cap. Control (measured, not a primary gold file): `Closed(1, 800), Closed(2, 900)` still `INSUFFICIENT_DATA`, quality still `40`.

### 5.6 Noise tape (same login, still N=2)

Append to the FX-01 deal tape. Scorer must still report `CompletedXauTrades = 2`, state `INSUFFICIENT_DATA`.

| Noise | Why it is not trade #3 |
|---|---|
| `DealAction.Balance` credit `+1000` | `IsTradingDeal == false` |
| Completed `EURUSD` 1.00 lot round-trip | `IsXauUsd == false` |
| XAU open `0.20` + partial out `0.10` (left open) | `Completed == false`; partial is not a lifecycle |
| Isolated SL modify (order only, no deal) | not a reconstructed trade |

Measured on this noise set: `completedXau = 2`, incomplete XAU = 1, total reconstructed rows = 4, scored state = `INSUFFICIENT_DATA`, scores identical to §5.4.

---

## 6. Fixture `B35-FX-02` — three good trades → SHADOW, never LIVE

### 6.1 Intent

Prove the §23 safety gate: first official score on a clean three-trade book is **SHADOW**. Quality may be 100. Live promotion is structurally impossible.

### 6.2 Reconstructed trades

| n | `PositionId` | `MaxVolumeLots` | `NetRealizedPnl` | `InitialSl` |
|---|---|---|---|---|
| 1 | 601 | `0.10` | `+80` | `2290` |
| 2 | 602 | `0.10` | `+70` | `2290` |
| 3 | 603 | `0.10` | `+90` | `2290` |

Same shape as `Three_disciplined_winners_go_to_shadow_not_live`.

### 6.3 Measured `FeatureSnapshot`

| Field | Gold |
|---|---|
| `CompletedXauTrades` | `3` |
| `NetPnl` | `240` |
| `GrossProfit` | `240` |
| `GrossLoss` | `0` |
| `ProfitFactor` | `99` |
| `LotCv` | `0` |
| `LossSizeCv` | `0` |
| `Martingale` | `false` |
| `AveragingDown` | `false` |
| `LotEscalation` | `false` |
| `AverageHoldSeconds` | `1800` |
| `SlUseRate` | `1` |
| `MaxDrawdown` | `0` |
| `TradeFrequencyPerDay` | `3` |

### 6.4 Measured `BaselineScore`

| Field | Gold | Why |
|---|---|---|
| `EarlyScoreEligible` | `true` | `N == 3` |
| `RiskScore` | `0` | no flags |
| `BehaviorScore` | `100` | no deductions |
| `EarlyQualityScore` | `100.00` | `50+15+10+5+20` (no N<3 cap) |
| `SuggestedState` | **`SHADOW`** | `quality >= 70 && risk < 40` |

### 6.5 Hard asserts

```text
N == 3
EarlyScoreEligible == true
SuggestedState == SHADOW
SuggestedState ∉ { LIVE, LIVE_CANDIDATE }
CanPromoteToLive(SHADOW) == false
no FLAG equivalent: Martingale/AveragingDown/LotEscalation all false
event (when persistence exists): EARLY_SCORE_ELIGIBLE once on first crossing N=3
```

A22 would subtract `U(3)=18` and cap quality at 82. **`baseline_v0` does not.** Gold is `100.00`. Do not “fix” the fixture to 82 without a new score version.

### 6.6 Close cousins (not gold, document so tests do not over-fit)

| Book | Measured state | Note |
|---|---|---|
| `+80,+70,+90` no SL | `SHADOW` (risk 10, quality 95.50) | demo `99001` shape |
| A22 Case A `+80,−40,+60` with SL | `SHADOW` (risk 0, quality 100.00) | still SHADOW; PF=3.5 |
| `+30,+10,−25` with SL | `SHADOW` (quality 95.00) | A22 called this EARLY_SCORE/WATCH; v0 is generous when SL exists and lots are flat |
| averaging-down on all three winners | `SHADOW` (risk 20, quality 92.00) | averaging is **not** enough to leave SHADOW in v0 |
| size-up 0.10→0.16→0.25 after **wins** | `SHADOW` (escalation true, risk 15, quality 96.25) | not martingale (no prior loss) |

FX-02 stays the **constant-lot three-winner** book so the SHADOW gate is unambiguous.

---

## 7. Fixture `B35-FX-03` — martingale after losses → RISK_BLOCKED

### 7.1 Intent

Prove the load-bearing anti-pattern: doubling (or 1.25×+) after a loss blocks the trader even if N=3 (eligible). Destination copy must not start. Risk engine separately rejects `MartingaleFlag` on new exposure (`MARTINGALE_BLOCK`); this fixture is the **score-state** gate, not the FIX gate.

### 7.2 Reconstructed trades

| n | `PositionId` | `MaxVolumeLots` | `NetRealizedPnl` | `InitialSl` |
|---|---|---|---|---|
| 1 | 701 | `0.10` | `−100` | `2290` |
| 2 | 702 | `0.20` | `−200` | `2290` |
| 3 | 703 | `0.40` | `−400` | `2290` |

Same shape as `Martingale_after_losses_is_risk_blocked`.

### 7.3 Adjacent-pair detection (must be true)

```text
pair (1→2): pnl_1 = -100 < 0  AND  0.20 > 0.10 * 1.25 = 0.125   → Martingale
             0.20 > 0.10 * 1.50 = 0.150                         → LotEscalation
pair (2→3): pnl_2 = -200 < 0  AND  0.40 > 0.20 * 1.25 = 0.250   → Martingale
             0.40 > 0.20 * 1.50 = 0.300                         → LotEscalation
```

Lot CV (population, measured): lots `{0.10, 0.20, 0.40}` → `LotCv = 0.5345` (> 0.50 and > 0.40).  
Loss-size CV of `{100, 200, 400}` → `LossSizeCv = 0.5345` (**not** > 0.80, so no behavior −10).

Equity path: `0 → −100 → −300 → −700`. Peak stays `0`. `MaxDrawdown = 700`. `GrossProfit = 0` so the “DD > GP” risk addend **does not fire** (it requires `GrossProfit > 0`).

### 7.4 Measured `FeatureSnapshot`

| Field | Gold |
|---|---|
| `CompletedXauTrades` | `3` |
| `NetPnl` | `−700` |
| `GrossProfit` | `0` |
| `GrossLoss` | `700` |
| `ProfitFactor` | `0` |
| `LotCv` | `0.5345` |
| `LossSizeCv` | `0.5345` |
| `Martingale` | **`true`** |
| `AveragingDown` | `false` |
| `LotEscalation` | **`true`** |
| `AverageHoldSeconds` | `1800` |
| `SlUseRate` | `1` |
| `MaxDrawdown` | `700` |
| `TradeFrequencyPerDay` | `3` |

### 7.5 Measured `BaselineScore`

| Field | Gold | Why |
|---|---|---|
| `EarlyScoreEligible` | `true` | N=3 (eligibility is independent of block) |
| `RiskScore` | `60` | 35 + 15 + 10 (lot CV). Not the A22 floor of 80 |
| `BehaviorScore` | `60` | 100 − 30 − 10 (lot CV) |
| `EarlyQualityScore` | `47.00` | `50 + 0.20*60 − 0.25*60 = 47` |
| `SuggestedState` | **`RISK_BLOCKED`** | `Martingale && MaxDrawdown>0 && NetPnl<0` |

State is **not** from `risk >= 80`. A test that only asserts `RiskScore >= 80` will **fail** this gold. Assert the flag + the losing-book clause.

### 7.6 Hard asserts

```text
N == 3
EarlyScoreEligible == true
Features.Martingale == true
Features.LotEscalation == true
Features.NetPnl < 0
Features.MaxDrawdown > 0
SuggestedState == RISK_BLOCKED
SuggestedState ∉ { SHADOW, WATCH, EARLY_SCORE, LIVE, LIVE_CANDIDATE, INSUFFICIENT_DATA }
CanPromoteToLive == false
```

### 7.7 Trap: lucky martingale is **not** this fixture

Measured control `−50 @0.10, −100 @0.20, +800 @0.40` (A22 Case B shape, with SL):

| Field | Measured |
|---|---|
| `Martingale` | `true` |
| `LotEscalation` | `true` |
| `NetPnl` | `+650` |
| `RiskScore` | `60` |
| `BehaviorScore` | `60` |
| `EarlyQualityScore` | `77.00` |
| `SuggestedState` | **`WATCH`** (not `RISK_BLOCKED`) |

v0 only hard-blocks martingale when the **book is still net-negative** (or `risk >= 80`, which this path never reaches: max from flags is 35+15+10+10 SL = 70 without averaging). A22 floors `FLAG_MARTINGALE` at risk 80 and forces `RISK_BLOCKED`. **Do not “fix” FX-03 by using the lucky book** — it will not land `RISK_BLOCKED` on the current scorer.

### 7.8 Trap: two-trade martingale **is** blocked

`Closed(1, −100, 0.10), Closed(2, −200, 0.20)` → `N=2`, `eligible=false`, but state = **`RISK_BLOCKED`** (risk 50, quality 40). FX-01 must not use this tape.

---

## 8. Deal tapes (reconstruction layer)

Native volume = `lots * 10_000`. IN `action=0` `entry=0`. OUT `action=1` `entry=1`. `price_sl=2290` on **both** legs so `InitialSl` survives even if a later mapper prefers the OUT deal.

`t0 = 2026-06-01T08:00:00.000Z`.

### 8.1 FX-01 (`login=35001`) — 4 deals

| seq | ticket | order | position | t_utc | entry | volume | price | profit | price_sl |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 10001 | 20001 | 501 | `2026-06-01T08:00:00.000Z` | IN | 1000 | 2300 | 0 | 2290 |
| 2 | 10501 | 20501 | 501 | `2026-06-01T08:30:00.000Z` | OUT | 1000 | 2310 | +10 | 2290 |
| 3 | 10002 | 20002 | 502 | `2026-06-01T09:00:00.000Z` | IN | 1000 | 2310 | 0 | 2290 |
| 4 | 10502 | 20502 | 502 | `2026-06-01T09:30:00.000Z` | OUT | 1000 | 2320 | +10 | 2290 |

Expect: 2 completed XAU trades, nets `+10,+10`, then §5.4.

### 8.2 FX-02 (`login=35002`) — 6 deals

| seq | ticket | order | position | t_utc | entry | volume | price | profit | price_sl |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 10001 | 20001 | 601 | `2026-06-01T08:00:00.000Z` | IN | 1000 | 2300 | 0 | 2290 |
| 2 | 10501 | 20501 | 601 | `2026-06-01T08:30:00.000Z` | OUT | 1000 | 2308 | +80 | 2290 |
| 3 | 10002 | 20002 | 602 | `2026-06-01T09:00:00.000Z` | IN | 1000 | 2308 | 0 | 2290 |
| 4 | 10502 | 20502 | 602 | `2026-06-01T09:30:00.000Z` | OUT | 1000 | 2315 | +70 | 2290 |
| 5 | 10003 | 20003 | 603 | `2026-06-01T10:00:00.000Z` | IN | 1000 | 2315 | 0 | 2290 |
| 6 | 10503 | 20503 | 603 | `2026-06-01T10:30:00.000Z` | OUT | 1000 | 2324 | +90 | 2290 |

Expect: 3 completed XAU trades, nets `+80,+70,+90`, then §6.4. Replay of the same 6 deals twice must not change scores (deal upsert idempotent).

### 8.3 FX-03 (`login=35003`) — 6 deals

| seq | ticket | order | position | t_utc | entry | volume | price | profit | price_sl |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 10001 | 20001 | 701 | `2026-06-01T08:00:00.000Z` | IN | 1000 | 2320 | 0 | 2290 |
| 2 | 10501 | 20501 | 701 | `2026-06-01T08:30:00.000Z` | OUT | 1000 | 2310 | −100 | 2290 |
| 3 | 10002 | 20002 | 702 | `2026-06-01T09:00:00.000Z` | IN | **2000** | 2310 | 0 | 2290 |
| 4 | 10502 | 20502 | 702 | `2026-06-01T09:30:00.000Z` | OUT | **2000** | 2290 | −200 | 2290 |
| 5 | 10003 | 20003 | 703 | `2026-06-01T10:00:00.000Z` | IN | **4000** | 2290 | 0 | 2290 |
| 6 | 10503 | 20503 | 703 | `2026-06-01T10:30:00.000Z` | OUT | **4000** | 2250 | −400 | 2290 |

Expect: 3 completed XAU trades, lots `0.10 / 0.20 / 0.40`, nets `−100,−200,−400`, then §7.4–7.5.

### 8.4 Reconstruction invariants for all tapes

```text
TradeReconstructor.Reconstruct("ACHIEVER", login, deals)
  .Count(t => t.Completed && t.IsXauUsd) == N

NetRealizedPnl == GrossRealizedPnl + Commission + Swap + Fees
               == OUT.profit                    (commission=swap=fees=0)

InitialSl == 2290
WasScaledIn == false
WasPartialClose == false
WasAveragedDown == false
RemainingVolumeLots == 0
```

Scorer input is **only** `completed && IsXauUsd`. Passing the full reconstruct list is allowed; `ComputeFeatures` filters again.

---

## 9. Reconstructed-trade helper (unit layer)

Matches `BaselineScorerTests.Closed` so unit gold and deal-tape gold stay one family.

```text
Closed(n, pnl, lots=0.10, sl=2290):
  Id                 = n.ToString()
  BrokerId           = "ACHIEVER"
  Login              = 1                    // unit tests; replay uses 3500x
  PositionId         = n
  CanonicalSymbol    = "XAUUSD"
  SourceSymbol       = "XAUUSD"
  Direction          = Long
  OpenedAt           = UnixEpoch + n hours
  ClosedAt           = OpenedAt + 30 minutes
  EntryVwap          = 2300
  ExitVwap           = 2301
  Initial/Max/Closed = lots
  Remaining          = 0
  Gross = Net        = pnl
  Commission=Swap=Fees = 0
  DealCount=OrderCount = 2
  InitialSl          = sl
  flags              = false
  Completed          = true
```

Unix-epoch hours vs `2026-06-01` tapes change hold/frequency the same way (hold always 1800 s; span < 1 day → frequency = N). Scores are therefore **identical** across the two clocks for these three books.

---

## 10. Existing coverage map (do not duplicate blindly)

| Location | What it already proves | Gap vs B35 gold |
|---|---|---|
| `BaselineScorerTests.Two_trades_remain_insufficient` | FX-01 reconstructed | no deal tape; no noise |
| `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` | FX-02 reconstructed + `CanPromoteToLive==false` | no deal tape; no numeric pin (`100.00` / `0`) |
| `BaselineScorerTests.Martingale_after_losses_is_risk_blocked` | FX-03 reconstructed + `Martingale==true` | no numeric pin (`60` / `60` / `47.00`) |
| `TradeReconstructionTests.First_three_completed_xau_unlocks_early_score` | N=3 eligibility on deals | does not score |
| `SeedingAndStoreTests` login `10001` | 3 completed XAU, state ≠ `LIVE` | **not** the FX-02 tape (mixed +80/−89/+162, **no SL**, quality `95.50`, still SHADOW) |
| `SeedingAndStoreTests` login `10002` | demo martingale → `RISK_BLOCKED` | lots 0.10/0.20/0.40 but P&L `−201/−502/−1404`, no SL, risk **70** / behavior **50** / quality **42.50** |
| demo login `99001` | 3 small winners, no SL → SHADOW `95.50` | cousin of FX-02 |
| demo login `10003` | **N=0** `INSUFFICIENT_DATA` | **not** FX-01. Empty book also yields `risk=10, behavior=90, quality=40` because `SlUseRate=0` still scores |

Demo `10002` is a valid **integration** cousin of FX-03 (same state). Do not treat its scores as FX-03 gold.

There is **no** seeded login with exactly two completed XAU trades. FX-01 is the missing seed if someone later extends `DemoBrokerFactory` — that is a later ticket.

---

## 11. A22 / A67 deltas (honest)

| Topic | A22 `baseline.v1` | Implemented `baseline_v0` | Fixture impact |
|---|---|---|---|
| Martingale ratio | `>= 1.80` | `> 1.25` | 2.0× still trips both |
| Escalation ratio | `>= 2.00` | `> 1.50` | 2.0× trips both |
| CV | sample (`n−1`) | population `Average` of squares | FX-03 `LotCv=0.5345` |
| Martingale risk floor | `Max(body, 80)` | additive +35, no floor | FX-03 risk **60**, not ≥80 |
| Lucky martingale | `RISK_BLOCKED` | `WATCH` if net > 0 | FX-03 **must stay net-negative** |
| `U(3)` quality penalty | `−18` (cap 82) | **absent** | FX-02 quality **100.00** |
| SHADOW threshold | `early_quality >= 62` after U(N) | `quality >= 70 && risk < 40` | FX-02 still SHADOW |
| N<3 | always `INSUFFICIENT_DATA` | except martingale-losing clause | FX-01 uses winners |
| MFE/MAE | optional EXACT terms | always `Unavailable` | unused |
| LIVE at N=3 | forbidden | forbidden (`CanPromoteToLive=false`; SHADOW max) | FX-02 |

A67 currently says score gold should wait for A22 and stay qualitative. **This ticket overrides that for the three cases:** qualitative outcomes are production law today, and v0 numbers are pinned so a formula edit fails loudly.

---

## 12. Proposed on-disk gold (do not create in this pass)

A67 layout, `expect.scores.mode = "numeric"`, `config.score.version = "baseline_v0"`:

```text
tests/Replay/Fixtures/
  score_n2_insufficient.json
  score_n3_shadow.json
  score_n3_martingale_risk_blocked.json
```

### 12.1 Shared envelope

```json
{
  "$schema": "ti.replay.fixture/v1",
  "schema_version": "1.0.0",
  "fixture_id": "score_n2_insufficient",
  "title": "Two completed XAUUSD winners remain INSUFFICIENT_DATA",
  "architecture_refs": ["§15", "§18", "§22", "§23", "§60", "§69"],
  "a27_tests": [
    "Scoring.TraderScoreCalculatorTests",
    "Scoring.ScoreStateTransitionTests",
    "Scoring.ThreeTradeSafetyGateTests",
    "Replay.ScoreComputationFromReplayTests"
  ],
  "clock": { "start_utc": "2026-06-01T08:00:00.000Z", "tz": "UTC", "auto_advance": "event_time" },
  "universe": {
    "brokers": [{ "code": "ACHIEVER", "display_name": "Achiever" }],
    "accounts": [{ "broker_code": "ACHIEVER", "login": 35001, "group_name": "demo\\b35", "leverage": 100, "currency": "USD" }],
    "canonical_instruments": [{ "code": "XAUUSD" }]
  },
  "config": {
    "score": { "version": "baseline_v0" },
    "volume": { "scale": 10000, "scale_name": "MTAPI_VOLUME_DIV" },
    "flags": { "real_copy_execution_enabled": false }
  },
  "events": [],
  "expect": {}
}
```

### 12.2 `expect.scores` gold (numeric)

**FX-01** (`login` 35001):

```json
"expect": {
  "reconstruction": {
    "completed_xau_n": 2,
    "early_score_eligible": false
  },
  "scores": {
    "mode": "numeric",
    "version": "baseline_v0",
    "completed_xau_trades": 2,
    "early_score_eligible": false,
    "risk_score": "0.00",
    "behavior_score": "100.00",
    "early_quality_score": "40.00",
    "state": "INSUFFICIENT_DATA",
    "martingale": false,
    "averaging_down": false,
    "lot_escalation": false,
    "forbidden_states": ["EARLY_SCORE", "WATCH", "SHADOW", "LIVE_CANDIDATE", "LIVE", "RISK_BLOCKED"]
  }
}
```

**FX-02** (`login` 35002):

```json
"expect": {
  "reconstruction": { "completed_xau_n": 3, "early_score_eligible": true },
  "scores": {
    "mode": "numeric",
    "version": "baseline_v0",
    "completed_xau_trades": 3,
    "early_score_eligible": true,
    "risk_score": "0.00",
    "behavior_score": "100.00",
    "early_quality_score": "100.00",
    "state": "SHADOW",
    "can_promote_to_live": false,
    "martingale": false,
    "averaging_down": false,
    "lot_escalation": false,
    "forbidden_states": ["LIVE", "LIVE_CANDIDATE"],
    "event": "EARLY_SCORE_ELIGIBLE"
  }
}
```

**FX-03** (`login` 35003):

```json
"expect": {
  "reconstruction": { "completed_xau_n": 3, "early_score_eligible": true },
  "scores": {
    "mode": "numeric",
    "version": "baseline_v0",
    "completed_xau_trades": 3,
    "early_score_eligible": true,
    "risk_score": "60.00",
    "behavior_score": "60.00",
    "early_quality_score": "47.00",
    "lot_cv": "0.5345",
    "loss_size_cv": "0.5345",
    "max_drawdown": "700",
    "net_pnl": "-700",
    "state": "RISK_BLOCKED",
    "martingale": true,
    "averaging_down": false,
    "lot_escalation": true,
    "forbidden_states": ["SHADOW", "WATCH", "EARLY_SCORE", "LIVE", "LIVE_CANDIDATE", "INSUFFICIENT_DATA"]
  }
}
```

Money/score fields as **strings** (A67 §4.3) so gold is bit-identical.

---

## 13. Unit / integration assertion matrix (later implementers)

Do **not** add these tests in this ticket. When someone does, reuse the gold above.

| Test | Input | Must |
|---|---|---|
| `Two_trades_remain_insufficient` | FX-01 reconstructed | already exists — keep |
| `Three_disciplined_winners_go_to_shadow_not_live` | FX-02 reconstructed | already exists — keep; optionally pin 0 / 100 / 100.00 |
| `Martingale_after_losses_is_risk_blocked` | FX-03 reconstructed | already exists — keep; optionally pin 60 / 60 / 47.00 |
| `N2_deal_tape_reconstructs_then_scores` | §8.1 | N=2, `INSUFFICIENT_DATA` |
| `N3_deal_tape_goes_shadow_not_live` | §8.2 | `SHADOW`, `CanPromoteToLive==false` |
| `N3_martingale_deal_tape_risk_blocked` | §8.3 | `Martingale`, `RISK_BLOCKED` |
| `N2_noise_does_not_unlock` | §5.6 | still N=2 |
| `N3_property_never_live` | any N=3 book | state ∉ {`LIVE`,`LIVE_CANDIDATE`} |
| `Idempotent_rescore` | FX-02 twice | identical Round2 scores |
| `Demo_10002_still_blocked` | seed | integration cousin; do not overwrite FX-03 numbers |

xUnit names already in tree should stay. New replay files must not replace them.

---

## 14. Persistence / dashboard expectations (when wired)

`ReconstructionScoringService.RebuildTraderAsync` writes `TraderScore`:

| Login | `CompletedXauTrades` | `Martingale` | `LotEscalation` | `RiskScore` | `BehaviorScore` | `EarlyQualityScore` | `CurrentState` |
|---|---|---|---|---|---|---|---|
| 35001 | 2 | false | false | 0 | 100 | 40 | `INSUFFICIENT_DATA` |
| 35002 | 3 | false | false | 0 | 100 | 100.00 | `SHADOW` |
| 35003 | 3 | true | true | 60 | 60 | 47.00 | `RISK_BLOCKED` |

Dashboard counters (`EfDashboardQueries`): FX-02 increments `SHADOW`, FX-03 increments `RISK_BLOCKED`, FX-01 increments neither (insufficient). No fixture may increment `LIVE`.

Copy / FIX: none of the three may produce `AllowFixSend=true`. FX-03 additionally trips `RiskEngine` `MARTINGALE_BLOCK` if a later test sets `MartingaleFlag=true` on `OpenExposure`. That is a **risk** fixture, not a scoring fixture — do not fold it into FX-03 gold.

---

## 15. What this report does **not** claim

- A22 `baseline.v1` numeric parity.
- That demo `10001` **is** FX-02 (it is a SHADOW cousin without SL).
- That `risk >= 80` is how FX-03 blocks (it is not).
- MFE/MAE, session, burst, U(N), or live-candidate paths.
- Schema migrations, Replay project creation, or seeder changes.
- Any product-source edit.

---

## 16. Binding list (repeat)

```text
B35-FX-01  N=2 disciplined winners     → INSUFFICIENT_DATA   quality=40
B35-FX-02  N=3 disciplined winners     → SHADOW              quality=100.00   never LIVE
B35-FX-03  N=3 0.10→0.20→0.40 losers   → RISK_BLOCKED        martingale=true  never SHADOW/LIVE
```

Measured 2026-08-18 against `TraderIntelligence.Domain` `BaselineScorer`. Product source was not modified.
