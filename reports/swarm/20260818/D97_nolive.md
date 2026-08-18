# D97 — Confirm `CanPromoteToLive` is **false**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D97_nolive.md` |
| Agent | D97 (no-live pin; `CanPromoteToLive` recensus) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:18+05:30 |
| Assigned | Confirm `CanPromoteToLive` is false. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| SUT | `TraderIntelligence.Domain.Scoring.TraderStateMachine.CanPromoteToLive` |
| SUT file | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` line **211** |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| SUT size / lines / mtime | 8143 B / 212 lines / 2026-08-18 07:38:10Z |
| Git blob (`git hash-object`) | `26095e9b58fed40d57e767c9c9676d8409e87350` |
| Binding law | Architecture §§1.4, 15, 18–23, 39, 68–69; `docs/scoring.md`; A22 I4–I5 / R5–R6 / §9.2; A69 S4–S7 / TS22 |
| Same-SHA predecessors (not copied as this verdict) | D12 (no LIVE promotion), D34 (3 facts), B12, C02, C13, D41 |
| Method | Re-read `BaselineScorer.cs` L187–211. Grep `src/`, `apps/`, `tests/` for `CanPromoteToLive` and `TraderState.LIVE`. Trace persist. Hash files. Run `dotnet test --filter FullyQualifiedName~BaselineScorerTests`. Nothing from memory. |

**Assigned answer: CONFIRMED. `CanPromoteToLive` is unconditionally `false`.**

**One-line:** `public static bool CanPromoteToLive(TraderState current) => false;` — the parameter is discarded; every `TraderState` returns `false`; Application never calls it; one unit fact locks only the `SHADOW` argument.

---

## 0. Verdict (binding)

| Check | Result | Class |
|---|---|---|
| Body is compile-time `false` | **Yes** (`=> false`) | `EXISTS_AND_GOOD` for §69 / A22 I4 *as a pin* |
| Inspects `current` | **No** | discarded parameter |
| Can return `true` for any enum value | **No** | hard constant |
| Product callers (`src/`, `apps/`) | **Zero** | definition only — `DEAD` API |
| Test callers | **One** (`CanPromoteToLive(SHADOW).Should().BeFalse()`) | `PARTIAL` lock |
| `FromBaseline` can emit `LIVE` / `LIVE_CANDIDATE` | **No** | reachable set excludes both |
| Persist consults this pin | **No** | `CurrentState = score.SuggestedState` blindly |
| A22 R5-before-R6 implemented | **No** | vacuous lock (“we forgot live”), not the machine |
| This file flips §68 / §70 | **No** | A100/C14/D42 stay **0/19**; A101/D43 stay **0/14** |
| Product source edited by D97 | **No** | report only |

```text
CanPromoteToLive(*) == false     -- measured 2026-08-18T13:45:18+05:30
                                 -- SHA ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34
                                 -- unchanged vs D12 / D34
```

Do **not** claim “LIVE promotion is gated.” There is no `if (N == 3) forbid LIVE`, no `MIN_LIVE_TRADES > 3` loader, no `manual_live_approve`, no `risk_engine_live_ok`. Do **not** claim this pin is a send gate. Live `35=D` is **SAFE_BY_ABSENCE** (D32/D43/D69), a different control.

---

## 1. File identity (measured this pass)

| Path | SHA-256 | Size | Lines | LastWriteUtc |
|---|---|---:|---:|---|
| `src\Domain\Scoring\BaselineScorer.cs` | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 8143 | 212 | 2026-08-18 07:38:10Z |
| `tests\Unit\BaselineScorerTests.cs` | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` | 2414 | 74 | 2026-08-18 07:47:42Z |
| `src\Domain\Enums\TraderState.cs` | `E509C59F1597EE0F424A9F9408D0B27B2C7063D724B3D0A63880E2558B930D68` | 264 | 15 | 2026-08-18 07:33:45Z |
| `src\Application\Ingestion\DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 4535 | 106 | 2026-08-18 08:05:29Z |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 12097 | — | — |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 8708 | — | — |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 5082 | — | — |
| `tests\Integration\SeedingAndStoreTests.cs` | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | 3119 | — | — |

Git (this pass):

| Ref | Note |
|---|---|
| `BaselineScorer.cs` worktree | clean vs HEAD (`git status --porcelain` empty for this path) |
| `BaselineScorer.cs` blob | `26095e9b58fed40d57e767c9c9676d8409e87350` |
| `BaselineScorerTests.cs` | **untracked** (`?? tests/Unit/BaselineScorerTests.cs`) |

SUT SHA matches D12 and D34. This file is a **reconfirm**, not a new implementation.

---

## 2. The method (verbatim)

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

Facts that follow from the text, not from a comment:

1. Return type is `bool`. The only expression is the literal `false`.
2. `current` is unused. C# does not warn this away into a live path.
3. There is no `switch`, no `N` argument, no config, no RBAC, no risk consult.
4. Sibling pin `AfterHighEarlyScore()` is hard-`SHADOW`. `Score()` does **not** call either helper; it calls `FromBaseline` only (L162).

`FromBaseline` reachable set (every `return`, L189–206):

| Token | Reachable? |
|---|---|
| `INSUFFICIENT_DATA` | Yes (`N==0` or `N∈{1,2}` after the block check) |
| `RISK_BLOCKED` | Yes (`risk >= 80` or losing martingale) |
| `SHADOW` | Yes (`N>=3`, not blocked, `quality >= 70` ∧ `risk < 40`) |
| `WATCH` | Yes (`N>=3`, not blocked, `quality >= 55`) |
| `EARLY_SCORE` | Yes (else) |
| `LIVE` | **No** |
| `LIVE_CANDIDATE` | **No** |
| `PAUSED` / `DISQUALIFIED` | **No** |

`LIVE` / `LIVE_CANDIDATE` exist on the enum (`TraderState.cs` values 4 and 5). That is **vocabulary**, not a path.

---

## 3. Call-site census (product + tests)

Grep this pass of `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` (`*.cs`):

| Location | Line | Role |
|---|---:|---|
| `src\Domain\Scoring\BaselineScorer.cs` | 211 | **definition** |
| `tests\Unit\BaselineScorerTests.cs` | 26 | **only caller** |

No hit in Application, Infrastructure, workers, API, or web C#/TS (the TS string “never auto-promotes to LIVE” on `OverviewPage.tsx` is copy, not this method).

`TraderState.LIVE` in product `*.cs` (not tests):

| Location | Line | Role |
|---|---:|---|
| `EfDashboardQueries.GetOverviewAsync` | 32 | **count** `LIVE_CANDIDATE` |
| `EfDashboardQueries.GetOverviewAsync` | 33 | **count** `LIVE` |

Those counters stay 0 unless a row is written by hand. Overview last constructor arg is a literal `false` (`RealCopyEnabled`). Counting is not promotion.

`TraderState.LIVE` assignment: **none** in `src/` or `apps/`.

---

## 4. Persist does not consult the pin

`ReconstructionScoringService.RebuildTraderAsync`:

```86:104:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

`EfTradingStore.UpsertScoreAsync` copies `score.CurrentState` onto `trader_scores` and `TraderScoreHistory.State`. No `CanPromoteToLive` check. No risk engine. No RBAC.

`PersistDemoShadowAsync` writes shadow rows only when `state == TraderState.SHADOW`. It never writes `LIVE`.

**Today:** `SuggestedState` cannot be LIVE ⇒ persisted state cannot be LIVE.  
**Tomorrow:** if `FromBaseline` grows `return TraderState.LIVE`, persist will write it. This hard-false pin is **not on that path**.

`DemoSeeder` only rebuilds logins `10001, 10002, 10003, 99001`. It never stamps `LIVE`.

---

## 5. Measured tests

Command (this pass, 2026-08-18T13:45):

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~BaselineScorerTests --nologo --verbosity minimal
```

| Result | Count |
|---|---:|
| Passed | **3** |
| Failed | **0** |
| Skipped | **0** |
| Exit | **0** |

The load-bearing fact:

```21:27:D:\Prop\tests\Unit\BaselineScorerTests.cs
    [Fact]
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }
```

| What is locked | What is **not** locked |
|---|---|
| N=3 clean winners → `SHADOW` | `SuggestedState ∉ {LIVE, LIVE_CANDIDATE}` for all books |
| `CanPromoteToLive(SHADOW) == false` | `CanPromoteToLive` for the other 8 enum values |
| N=2 → `INSUFFICIENT_DATA` | `AfterHighEarlyScore() == SHADOW` |
| Losing 2×/2× → `RISK_BLOCKED` | A22 Case B (winning martingale) / mild 1.26× SHADOW hole |

Integration `SeedingAndStoreTests` asserts login `10001` `CurrentState.Should().NotBe(LIVE)` — vacuous while `FromBaseline` cannot emit LIVE (C16/D37). Named A89 #76 `ThreeTradeSafetyGateTests` is **not on disk**.

---

## 6. Honesty / anti-greenwash

| Claim someone might write | Measured |
|---|---|
| “`CanPromoteToLive` is false” | **True.** Hard constant. |
| “Therefore LIVE promotion is correctly gated” | **False.** Vacuous absence, not A22 R5-before-R6. |
| “Application refuses LIVE unless this returns true” | **False.** Application never calls it. |
| “Trade #3 is always SHADOW” | **False.** Can be `EARLY_SCORE` / `WATCH` / `SHADOW` / `RISK_BLOCKED`. Never LIVE. |
| “`CanPromoteToLive == false` is the ML / send gate” | **False.** ML is not built (C44). Send is absent (D32/D69). |
| “§68 / §70 may tick because of this pin” | **False.** 0/19 and 0/14 still hold. |
| D12 / D34 are stale on this SHA | **False.** Same `ECA2EEE8…` / `61E34A07…`. |

A later coder who adds `return TraderState.LIVE` under `quality >= 90` and forgets this pin still has an Application hole: persist will write LIVE. Keep `CanPromoteToLive` false at `N==3` when the real R6 function is written; do not treat today’s body as that function.

---

## 7. Related bars (not substitutes)

| Bar | Score | Relation to this pin |
|---|---|---|
| A22 I4 / A69 TS22 (`CanPromoteToLive` false at `N<=3`) | **PASS-safer** (false at **all** N) | This file |
| A22 R6 + manual approve + risk | **MISSING** | pin is not that conjunction |
| §69 first useful version (D41) | **0 / 12 accepted** | pin is correct for “no live send”; FUV is not 12/12 |
| §68 go-live (A100 / C14 / D42) | **0 / 19** | this pin is not a gate |
| §70 live FIX (A101 / D43) | **0 / 14** | this pin is not a session |
| `RealCopyExecutionEnabled` (D69) | default **`false`** | different identifier; also unused as a send gate |

---

## 8. Sign-off

```text
[x] CanPromoteToLive body is `=> false` (SHA ECA2EEE8…)
[x] Product callers: none
[x] Unit Three_disciplined_winners_go_to_shadow_not_live: PASS this pass
[ ] CanPromoteToLive false for every TraderState (theory) — NOT ON DISK
[ ] Persist refuses LIVE unless pin + risk + RBAC — NOT IMPLEMENTED
[ ] A22 R5-before-R6 machine — NOT IMPLEMENTED
[ ] §68 19/19 / §70 14/14 — still FAIL
[x] Product source unmodified
```

**Current:** `CanPromoteToLive` is **false**. Automatic LIVE promotion does **not** exist. Real copy stays **DISABLED**.

---

## 9. Sources

- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\docs\scoring.md`
- `D:\Prop\reports\swarm\20260818\D12_scorer_review.md`
- `D:\Prop\reports\swarm\20260818\D34_score_tests.md`
- `D:\Prop\reports\swarm\20260818\D69_flag.md`
- `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md`
- `D:\Prop\reports\swarm\20260818\A69_trader_states.md`

---

*End of D97. Product source was not modified. `TraderStateMachine.CanPromoteToLive` remains hard-`false`.*
