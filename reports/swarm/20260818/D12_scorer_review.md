# D12 — BaselineScorer review: no LIVE promotion

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D12_scorer_review.md` |
| Agent | D12 (scorer review) |
| Date | 2026-08-18 |
| Assigned question | Read `BaselineScorer.cs`. **Confirm no LIVE promotion.** |
| Product source edited | **No** |
| SUT | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| SUT size | 8143 bytes / 212 lines / last write 2026-08-18 13:08:10 |
| Tests read | `D:\Prop\tests\Unit\BaselineScorerTests.cs` (SHA-256 `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408`, 2414 bytes) |
| Callers read | `ReconstructionScoringService` (`DealIngestionService.cs`); `EfTradingStore.UpsertScoreAsync`; `EfDashboardQueries.GetOverviewAsync`; `DemoSeeder`; `SeedingAndStoreTests` |
| Law | Architecture §§1.4, 15, 18–23, 39, 68–69; `docs/scoring.md`; A22 `baseline.v1` I4–I5 / §9.2; A69 S4–S7 |
| Prior (same SHA, not copied as verdict) | B12 (formulas/leakage); C02 (unit tests); C16 (seed `NotBe(LIVE)`); C32 (martingale can still score ≥70) |
| Method | Full re-read of `BaselineScorer.cs`. Enumerate every `return` in `FromBaseline`. Grep product `*.cs` for `TraderState.LIVE`, `LIVE_CANDIDATE`, `CanPromoteToLive`, `Promote`. Trace persist. Hand-evaluate the three unit fixtures plus A22 Case A/B. Nothing answered from memory. |

**Assigned answer:** **Confirmed. `BaselineScorer` / `TraderStateMachine` do not promote anyone to `LIVE` or `LIVE_CANDIDATE`.**

**One-line:** live is unreachable at **every** `N` because `FromBaseline` never emits those tokens and `CanPromoteToLive(_) => false`. That is a **vacuous lock** (“we forgot live”), not A22 R5-before-R6. Persist copies `SuggestedState` blindly. Product source was not modified.

---

## 0. Verdict

| Check | Class | One-line |
|---|---|---|
| `FromBaseline` can return `LIVE` | **ABSENT** | Reachable set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` only. |
| `FromBaseline` can return `LIVE_CANDIDATE` | **ABSENT** | Same. No branch names either token. |
| `AfterHighEarlyScore()` | **SHADOW pin** | Hardcoded `=> TraderState.SHADOW`. Never called by `Score()`. |
| `CanPromoteToLive(current)` | **hard-`false`** | Ignores `current`. Safer than a premature live path. **Application never calls it.** |
| Persist writes LIVE | **NO path today** | `CurrentState = score.SuggestedState`. Suggested cannot be LIVE. |
| Trade #3 + high score → LIVE | **NO** | Clean winners → `SHADOW`. Quality can be **100** (not A22 ≤82). |
| Winning martingale → LIVE | **NO** | Lands `WATCH` (A22 Case B) or even `SHADOW` (mild 1.26–1.49×). Wrong vs A22, still not LIVE. |
| N=20 + perfect book → LIVE | **NO** | Live unreachable at all N. A22 Case E (`LIVE_CANDIDATE` after shadow sample) is **MISSING**, safely. |
| A22 I4 / A69 S4 implemented as a gate | **VACUOUS PASS** | There is no `if (N == 3) forbid LIVE`. Safety is absence of a live branch. |
| `baseline.v1` implemented | **NO** | Unversioned additive stub. Do not gold-file A22 Cases A–F against it. |
| Product source changed by D12 | **NO** | Report only. |

**Do not claim** “LIVE promotion is gated.” There is no R5-before-R6, no `ScoreConfig.MIN_LIVE_TRADES > 3` loader, no `manual_live_approve`, no `risk_engine_live_ok`. **Do not claim** trade #3 is correctly gated to SHADOW for every book. **Do not claim** go-live scoring is done.

---

## 1. What is actually in the file

One compilation unit, four types:

| Type | Role | LIVE path? |
|---|---|---|
| `FeatureSnapshot` | Window aggregates + unused MFE/MAE slots (`FeatureQuality.Unavailable`, `PriceSource.Unknown`) | No state |
| `BaselineScore` | Three scores + `SuggestedState` + `EarlyScoreEligible` | Field only |
| `BaselineScorer` | `EarlyScoreTradeCount = 3`; `ComputeFeatures`; `Score`; population CV | Calls resolver; never assigns LIVE |
| `TraderStateMachine` | Stub resolver + two pins | **Cannot emit LIVE** |

`Score()` always:

1. Re-filters `Completed && IsXauUsd`, sorts by `ClosedAt` only (no `opened_at`, no `id` tie-break).
2. Builds features on the **entire** caller-supplied list (no `FIRST3` / `EXPANDING` / `as_of`).
3. Sets `EarlyScoreEligible = N >= 3` (sticky bool, **not** the §15 one-shot `EARLY_SCORE_ELIGIBLE` event).
4. Adds/subtracts flat points for risk / behavior / quality.
5. Calls `TraderStateMachine.FromBaseline(eligible, quality, risk, features)`.
6. Returns `SuggestedState = state`.

No `DateTime.UtcNow` inside the calculator (A22 I11 **pass** for the SUT). No `ScoreConfig`. No flags table. No R0–R9. No `PROVEN_PROFITABLE` token anywhere (that name is not in the enum).

---

## 2. Assigned check — no LIVE promotion

### 2.1 Binding rule (not optional)

Architecture §1.4 / §23, `docs/scoring.md` (“High quality + low risk ⇒ `SHADOW`, never `LIVE`”), A22 I4–I5 / §9.2, A69 S4–S5:

```text
WHEN N == 3:
    next_state ∈ { EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED }
    next_state ∉ { LIVE, LIVE_CANDIDATE }
    even if early_quality == 82 and NET is huge:
        maximum automatic promotion = SHADOW
```

`LIVE` / `LIVE_CANDIDATE` exist on the enum (`TraderState.cs` values 4 and 5). That is **vocabulary**, not a path.

A22 R6 (the only legal live nomination) requires all of: `N >= MIN_LIVE_TRADES` (must be `> 3`), shadow sample, `N > 3` conjunct, not severe, then `LIVE` only if `manual_live_approve` **and** `risk_engine_live_ok`; else `LIVE_CANDIDATE`. **None of that exists in this file.**

### 2.2 Resolver — every return (measured)

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

| Token | `FromBaseline` reachable? | How |
|---|---|---|
| `INSUFFICIENT_DATA` | **Yes** | `N==0`, or `N` in `{1,2}` after the block check |
| `RISK_BLOCKED` | **Yes** | `risk >= 80` **or** (`Martingale` ∧ `MaxDrawdown > 0` ∧ `NetPnl < 0`) |
| `SHADOW` | **Yes** | `N>=3`, not blocked, `quality >= 70` **and** `risk < 40` |
| `WATCH` | **Yes** | `N>=3`, not blocked, `quality >= 55`, not SHADOW |
| `EARLY_SCORE` | **Yes** | else (`N>=3`, quality `< 55`) |
| `LIVE` | **No** | no `return` |
| `LIVE_CANDIDATE` | **No** | no `return` |
| `PAUSED` | **No** | no manual-pause input |
| `DISQUALIFIED` | **No** | no DQ input |

There is **no** `if (N == 3) forbid LIVE` line. There is no inspection of `current` / `prev_state`. Live is unreachable at **N=3 and N=300**. A later coder who adds `return TraderState.LIVE` under `quality >= 90` would open the hole at trade #3 unless R5 is written first.

`CanPromoteToLive` discards `current` and is hard-`false`. That is the only named promotion API. Grep of `D:\Prop\src\**\*.cs`:

| Symbol | Hits in product C# |
|---|---|
| `CanPromoteToLive` | **Definition only** (`BaselineScorer.cs:211`). Application never calls it. |
| `TraderState.LIVE` assignment | **None.** Dashboard **counts** `CurrentState == LIVE`. Seed test asserts `NotBe(LIVE)`. |
| `TraderState.LIVE_CANDIDATE` assignment | **None.** Dashboard counts only. |
| `Promote` / `PromoteToLive` | **None** in Domain/Application/Infrastructure. |
| `AfterHighEarlyScore` | **Definition only.** `Score()` does not call it. Tests do not call it. |

`RiskEngine.cs` has **zero** `LIVE` / `promote` matches. It cannot raise trader state.

### 2.3 Persist path does not add a live write

`ReconstructionScoringService.RebuildTraderAsync`:

```85:101:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);
```

`EfTradingStore.UpsertScoreAsync` copies `score.CurrentState` onto `trader_scores` and appends `TraderScoreHistory.State`. No pin check. No risk-engine consult. No RBAC.

**Today:** Suggested ∈ reachable set ⇒ persisted state cannot be LIVE.  
**Tomorrow:** if `FromBaseline` grows a live branch, persist will write it. The hard-false pin is **not on this path**.

`DemoSeeder` only calls `RebuildTraderAsync` for logins `10001, 10002, 10003, 99001`. It never stamps `LIVE`.

### 2.4 Dashboard / UI are not promotion

| Surface | What it does | Promotion? |
|---|---|---|
| `EfDashboardQueries` counts `LIVE` / `LIVE_CANDIDATE` | Read model. Counters stay 0 unless a row is written by hand. | **No** |
| `OverviewDto.Live` / `RealCopyEnabled` | `Live` = count; `RealCopyEnabled` **hard-coded `false`** (last arg of `OverviewDto`) | **No** |
| `apps/web/src/pages/OverviewPage.tsx` | Renders “Trade #3 never auto-promotes to LIVE.” | Copy, not a write |
| `TraderState` enum members 4 and 5 | A22/A69 vocabulary | **No** |
| `/live` Live Copy page | Chrome/stub (C37). No portfolio API. | **No** |

### 2.5 Checklist vs A22 / A69

| ID | Required | Product | D12 result |
|---|---|---|---|
| I4 / A22 §9.2 / A69 S4 | `N==3` ∉ `{LIVE, LIVE_CANDIDATE}` | Tokens unrepresentable in `FromBaseline` | **PASS (vacuous)** |
| I5 / A69 S5 | High score @3 → SHADOW only | Iff `quality>=70 && risk<40` | **PARTIAL** (thresholds ≠ `SHADOW_MIN=62`; martingale can still SHADOW) |
| A69 S6 | `MIN_LIVE_TRADES > 3` loader + R5 before R6 | **MISSING** | live still unreachable |
| A69 S7 | Scorer nominates `LIVE_CANDIDATE`; never assigns `LIVE` | Nominates **neither** | **PASS-safer** / incomplete |
| A69 TS22 | `CanPromoteToLive` false when `N<=3` | False for **all** states / all N | **PASS-safer** |
| T6 `AfterHighEarlyScore()==SHADOW` | Hardcoded | **PASS** (unlocked — no unit fact) |
| A22 Case E | N=20 + shadow sample → `LIVE_CANDIDATE` | Impossible | **MISSING** (safe) |
| A22 Case F | Illegal `MIN_LIVE_TRADES=3` still cannot live @3 | No config | **MISSING** (live still unreachable) |
| `PROVEN_PROFITABLE` | Never emit | Token does not exist | **PASS** |

**Assigned confirmation, without greenwash:** **No LIVE promotion exists in `BaselineScorer` / `TraderStateMachine` / the score persist path.** The lock is absence + a dead `false` pin, not the A22 machine.

---

## 3. Hand-evaluated landings (still not LIVE)

Constants = stub additives. Volumes in lots. P&L already net. No MFE.

### 3.1 Unit fixture — three disciplined winners (C02 fact)

```text
t1..t3  vol=0.10  pnl=+80/+70/+90  sl=yes
N=3  NET=+240  PF=99  lot_cv=0  max_dd=0
risk=0  behavior=100
quality=50+15+10+5+20-0 = 100.00     // A22 cap after U(3)=18 is ≤82
eligible=true  quality>=70 ∧ risk<40 → SHADOW
CanPromoteToLive(SHADOW)=false
```

**State vs I5 for this book: PASS. Numbers vs A22: FAIL.** Not LIVE.

### 3.2 A22 Case A — clean mixed book (must be SHADOW, never LIVE)

```text
t1 0.10 +80 sl=yes
t2 0.10 -40 sl=yes
t3 0.10 +60 sl=yes
NET=+100  PF=3.50  lot_cv=0  martingale=false
risk=0  behavior=100  quality=100.00 → SHADOW
```

**State: SHADOW. LIVE: no.** Quality should be ≤82.

### 3.3 A22 Case B — huge profit + 2×/2× martingale (must be RISK_BLOCKED, never LIVE)

```text
t1 0.10 -50  sl=no
t2 0.20 -100 sl=no
t3 0.40 +800 sl=no
martingale=true, lotEscalation=true, lotCv≈0.5345, sl_rate=0
risk = 35+15+10+10 = 70
behavior = 100-30-10-10 = 50
quality = 50+15+10+5 +10 -17.5 = 72.50
RISK_BLOCKED? risk>=80? no.  (martingale ∧ DD>0 ∧ NET<0)? NET>0 → no
quality>=70 ∧ risk<40? 70 is not <40 → no
quality>=55 → WATCH
```

**State: `WATCH`. Spec: `RISK_BLOCKED`.** Dollars do not buy a live seat (vacuous) but they **do** buy a mid-band operational state. C32 measured the same book at **72.50** (no SL) / **77.00** (with SL).

### 3.4 Mild winning martingale (SHADOW hole, still not LIVE)

```text
t1 0.10 -50 sl=yes
t2 0.13 -50 sl=yes    // 1.30× after loss ≥ 1.25, < 1.50
t3 0.13 +400 sl=yes
risk=35  behavior=70  quality=85.25
85.25>=70 ∧ 35<40 → SHADOW
```

A22: `FLAG_MARTINGALE` floors risk at 80 → `RISK_BLOCKED`. Stub will mark this book shadow-eligible (A24 eligibility is `{SHADOW, LIVE_CANDIDATE, LIVE}`). **Still not LIVE.** C32 `MILD_1.26_SL` measured **85.25 / SHADOW**.

### 3.5 Losing 2×/2× (unit fact)

```text
0.10/-100 → 0.20/-200 → 0.40/-400
risk=60 (not ≥80)
NET<0 ∧ martingale ∧ DD>0 → RISK_BLOCKED
```

Blocked because the book **lost**, not because martingale floors risk at 80.

### 3.6 N<3 and N=0

| Book | Eligible | State | LIVE? |
|---|---|---|---|
| Empty list / N=0 | false | `INSUFFICIENT_DATA` | no |
| Two winners | false | `INSUFFICIENT_DATA` (quality still computed, capped 40, and **persisted**) | no |

### 3.7 Demo seed (qualitative; SL never ingested)

| Login | Stub landing (prior measured) | LIVE? |
|---|---|---|
| 10001 | `SHADOW` (N=3, no SL → risk 10) | **No** — integration asserts `NotBe(LIVE)` only |
| 10002 | `RISK_BLOCKED` (losing 2×/2×) | **No** |
| 10003 | `INSUFFICIENT_DATA` (0 trades; quality 40 published) | **No** |
| 99001 | `SHADOW` (small winners, PF stub=99, no SL) | **No** |

---

## 4. Tests — what they lock, what they do not

Three facts in `BaselineScorerTests`. Load-bearing LIVE-adjacent fact:

```20:27:D:\Prop\tests\Unit\BaselineScorerTests.cs
    [Fact]
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }
```

| Claim the name suggests | What is asserted | Hole |
|---|---|---|
| “not live” | `== SHADOW` and `CanPromoteToLive(SHADOW)==false` | Does not assert `!= LIVE` **and** `!= LIVE_CANDIDATE` as a pair. Changing the landing to `LIVE` would fail the SHADOW assert — **this one fixture is the only unit lock.** |
| Pin is the gate | Called only with `SHADOW` | Never theory-tested for every `TraderState` |
| AfterHighEarlyScore | **Not called** | Dead pin |
| ∀ books at N=3 | Only the clean-winner book | Case B / mild martingale untested |
| Persist refuses LIVE | Integration `10001 NotBe(LIVE)` | Vacuous; does not lock `== SHADOW` |

Named A27/A89 class `ThreeTradeSafetyGateTests` is **not on disk**. “No LIVE promotion” is a **code-reading claim** plus one happy-path fact, not a property.

---

## 5. What is *not* a LIVE path (do not over-report)

- `TraderState.LIVE` / `LIVE_CANDIDATE` enum members — required vocabulary (A22 §9 / A69 S0).
- Dashboard integer counters for those states — read model.
- Overview copy “Trade #3 never auto-promotes to LIVE” — UI string; true today, not a compiler lock.
- `CanPromoteToLive` existing as an API — body is `false`.
- `REAL_COPY_EXECUTION_ENABLED` — execution flag; scorer does not read it (A22 §9.3 / A69 S8). Still default false elsewhere.
- Shadow simulation on `SHADOW` books — destination-quote paper, not real `NewOrderSingle`.
- Integration `NotBe(LIVE)` on demo 10001 — agrees; does not promote.

---

## 6. Adjacent defects that are *not* LIVE (honesty)

These must not be laundered into “scoring is safe / done.” They are why a vacuous no-LIVE lock is not A22.

| Defect | Evidence | LIVE? |
|---|---|---|
| Winning martingale → `WATCH` not `RISK_BLOCKED` | Case B; risk additive 70; NET>0 skips the block clause | No |
| Mild 1.26–1.49× martingale → `SHADOW` | risk=35<40; C32 quality **85.25** | No |
| Martingale ratio `> 1.25` vs A22 `>= 1.80` | lines 90–91 | No |
| Escalation `> 1.50` vs `>= 2.00` | line 92 | No |
| `U(N)` missing; quality can be **100** at N=3 | lines 152–160 | No (over-confident board) |
| NET sign inside quality (I9) | `if (NetPnl > 0) quality += 15` | No |
| Population CV (`.Average()` of squares) | lines 174–184 | No |
| PF cap 99 vs 5 | line 114 | No |
| Order by `ClosedAt` only | line 44 | Nondeterministic adjacent ratios |
| Persist publishes N<3 quality | Application always upserts | Leaderboard leak, not LIVE |
| SL never ingested | `Mt5DealDto` has no SL | Every live ingest looks like no-SL |
| No FIRST3 / as-of / `score_version` | API is `Score(list)` | Research leakage later, not LIVE |

Formulas remain an unversioned stub. Classification vs A69: `FromBaseline` is **EXISTS_NEEDS_REFACTOR**. The two pins are **EXISTS** (safer than a premature live path).

---

## 7. What a later implementer must not do

```text
Do not add return TraderState.LIVE (or LIVE_CANDIDATE) to FromBaseline
    without R5-before-R6, N>3, MIN_LIVE_TRADES>3 loader, shadow sample,
    manual_live_approve, and risk_engine_live_ok
Do not treat CanPromoteToLive => false as the A22 gate once a live branch exists
Do not persist SuggestedState without consulting the pin + risk + RBAC
Do not treat this stub as baseline.v1
Do not gold-file A22 Case A–F against these additives
Do not train ML on trader_scores.EarlyQualityScore as a FIRST3 feature
Do not enable REAL_COPY_EXECUTION_ENABLED because "the scorer cannot emit LIVE"
    (execution flag is independent; A69 S8)
Do not modify product source in the same change-set as this review
```

---

## 8. Decision summary

| Question | Decision |
|---|---|
| Does `BaselineScorer` promote to `LIVE`? | **No.** |
| Does it promote to `LIVE_CANDIDATE`? | **No.** |
| Does persist / seeder / risk engine / dashboard write LIVE? | **No** (counts only; persist copies a non-LIVE SuggestedState). |
| Is that the A22 Trade-#3 safety gate? | **No. Vacuous.** Live is unreachable at every N. |
| Is trade #3 correctly SHADOW / WATCH / BLOCKED? | **No** — Case B is WATCH; mild martingale is SHADOW. |
| Are unit tests a regression lock for ∀ N==3? | **No.** One SHADOW fixture + pin on that state. |
| Product source changed? | **No.** |

*End of D12. Confirmed: no LIVE promotion. Not confirmed: A22 gate, correct SHADOW landings, baseline.v1.*
