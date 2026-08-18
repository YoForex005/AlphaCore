# P503_S02 — Auto-add: implicit SHADOW admission, no roster, WATCH never auto-promotes

**PARTIALLY SUPERSEDED.** `TickRosterAsync` now writes explicit `roster:{broker}:{login}` intents (`ADMITTED` / `REMOVED:…`). WATCH still does **not** auto-promote to LIVE (`CanPromoteToLive` false) — that part is still correct. Pin: `P503_AUTO_ROSTER.md`.

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P503_S02_auto_add.md` |
| Agent | P503_S02 (copy auto-add / admission) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned | Read `CopyTradingHostedService` and `GenerateShadowIntentsAsync`. Admission is implicit (any SHADOW score on the tick). There is no roster table. New eligible traders are picked up next 20s poll IF they already have SHADOW state. WATCH never auto-promotes (`CanPromoteToLive` false; `FromBaseline` only to SHADOW via scorer). Document that. Do not edit product. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Method | Full `read_file` of `CopyTradingHostedService.cs` (40/40), `CopyTradingService.GenerateShadowIntentsAsync` (L92–301), `TraderStateMachine` / `BaselineScorer.Score`, `XauUsdOneToOneCopyPolicy.IsTraderEligible`, `TraderScore`, `TraderDbContext` DbSets + `ToTable` map, `ReconstructionScoringService.RebuildTraderAsync`, `EfTradingStore.UpsertScoreAsync` + `PersistDemoShadowAsync`, `LiveIngestHostedService` score loop, `apps/mt5-worker/Worker.cs`, `TraderState` enum, `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live`. Product-tree grep: `roster` / `CopyRoster` / `EligibleTrader` / `AdmitTrader` / `PromoteTo` / `CurrentState =` / `RebuildTraderAsync`. Nothing from memory. |

**Honesty rule:** “auto-add” here means the **next copy tick sees a new `TraderScores` row already in `SHADOW`**. It is **not** an operator roster insert, **not** a WATCH→SHADOW promotion inside the hopper, and **not** a live send. `AllowFixSend` is persisted **false**. `NewOrderSingleImplemented` is `const false`. This slot does not measure how many SHADOW rows exist in a live DB.

---

## 0. Verdict (binding)

**CONFIRMED.** Copy admission is implicit. There is no roster table. The 20 s hosted poll loads whoever already has a copyable `TraderScore.CurrentState`. `WATCH` never auto-promotes.

| Claim | Result | Evidence |
|---|---|---|
| Admission is implicit (any SHADOW score on the tick) | **Yes** | `GenerateShadowIntentsAsync` L94–95 queries `TraderScores` where `CurrentState ∈ {SHADOW, LIVE_CANDIDATE, LIVE}`. No admit API, no allow-list, no operator confirm. |
| There is a `copy_roster` / eligible-trader table | **No** | `TraderDbContext` has 20 `DbSet`s. None is a roster. Grep `roster` / `CopyRoster` / `EligibleTrader` / `AdmitTrader` in product `*.cs` = **0 writers**. |
| New eligible traders appear on the **next 20 s poll** | **Yes, iff they already have SHADOW (or LIVE\*) state** | Host: delay 8 s then `Task.Delay(20s)` after each tick. Hopper does **not** score. Scoring is a different host (`RebuildTraderAsync` → `UpsertScoreAsync`). |
| WATCH auto-promotes to SHADOW or LIVE on the copy tick | **No** | WATCH is **not** in `copyable`. Policy rejects WATCH as `TRADER_NOT_SHADOW_YET`. `FromBaseline` can emit WATCH or SHADOW; it never emits LIVE. `CanPromoteToLive` is hard-`false`. |
| Copy tick writes `35=D` / live send | **No** | Persist `AllowFixSend = false`. LIVE branch also requires `NewOrderSingleImplemented && VenueReconciled` (both const false). Host log: *“Live NewOrderSingle still blocked.”* |

One-line:

```text
CopyTradingHostedService every 20s → GenerateShadowIntentsAsync(TraderScores WHERE state IN SHADOW|LIVE_CANDIDATE|LIVE).
No roster. New name appears only after scorer already wrote SHADOW. WATCH stays WATCH.
```

---

## 1. Hosted loop — poll, not an admit job

`D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` (registered in `DependencyInjection.AddTraderIntelligence` L59):

```19:38:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                if (n > 0)
                    _log.LogInformation("Copy pipeline created {Count} SHADOW intents. Live NewOrderSingle still blocked.", n);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Copy pipeline tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }
```

Facts:

1. First tick is **8 s** after host start, then **every 20 s**.
2. The only product call is `GenerateShadowIntentsAsync`. There is no `Admit`, `AddTrader`, `Promote`, or roster refresh.
3. A trader who is not yet in `TraderScores` with a copyable state is **invisible** this tick and the next, until a **separate** scoring write happens.

---

## 2. Admission is implicit — any copyable score on the tick

```92:121:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public async Task<int> GenerateShadowIntentsAsync(CancellationToken ct)
    {
        var copyable = new[] { TraderState.SHADOW, TraderState.LIVE_CANDIDATE, TraderState.LIVE };
        var scores = await _db.TraderScores.Where(s => copyable.Contains(s.CurrentState)).ToListAsync(ct);
        if (scores.Count == 0)
            return 0;
        // ...
        foreach (var score in scores)
        {
            // ...
            if (!_policy.IsTraderEligible(snapshot, out _))
                continue;
```

What this is:

- **Store-gate, not a roster.** Whoever already sits in `trader_scores` with `CurrentState` in `{SHADOW, LIVE_CANDIDATE, LIVE}` is in the hopper **this tick**.
- **No per-login opt-in.** `TraderScore` is `{BrokerId, Login, scores, flags, CurrentState, LastScoredAt}`. There is no `AdmittedAt`, `CopyEnabled`, `RosterSlot`, or operator note.
- **SHADOW is the only scorer-reachable copyable state.** `TraderStateMachine.FromBaseline` never returns `LIVE_CANDIDATE` or `LIVE` (see §5). Those two tokens are in the query for a future/manual writer that does not exist in product today. Status blocker text even says so: *“0 traders in LIVE (promotion is manual; trade #3 cannot auto-LIVE)”* (`CopyTradingService.BuildBlockers` L310–311).
- **WATCH / EARLY / INSUFFICIENT / RISK_BLOCKED / PAUSED / DISQUALIFIED** are never loaded by this query.

Second gate (policy), after the state filter:

```73:85:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
    public bool IsTraderEligible(CopyTraderSnapshot trader, out string reason)
    {
        if (trader.State is TraderState.RISK_BLOCKED or TraderState.DISQUALIFIED or TraderState.PAUSED)
        {
            reason = "TRADER_BLOCKED_" + trader.State;
            return false;
        }

        if (trader.State is TraderState.INSUFFICIENT_DATA or TraderState.EARLY_SCORE or TraderState.WATCH)
        {
            reason = "TRADER_NOT_SHADOW_YET";
            return false;
        }
```

Then: size-pattern block, `CompletedXauTrades >= 20`, `XauNetPnl > 0`, reject `demo\` / `contest\` group prefixes.

Implication for “auto-add”:

- A brand-new SHADOW row **is** admitted on the next poll **for hopper consideration**.
- That is **not** the same as “will emit an intent.” Policy can still `continue` (demo group, N<20, PnL≤0, martingale).
- A WATCH row is **not** admitted. It is not in `copyable`, and even if it were, policy would reject `TRADER_NOT_SHADOW_YET`.

---

## 3. There is no roster table

`TraderDbContext` tables (`D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`):

| Table | Role vs admission |
|---|---|
| `trader_scores` | **The only membership set the hopper reads.** Unique `(BrokerId, Login)`. |
| `trader_score_history` | Append-only score/state history. Hopper does not read it. |
| `copy_intents` | Output of the tick (idempotency key `copy:{broker}:{login}:{pos}:open\|close`). Not a roster. |
| `shadow_orders` | Optional shadow fill after Approve. Not a roster. |
| `risk_decisions` | Per-intent persist. `AllowFixSend` forced false. |
| `execution_intents` | Exists; this path never assigns `CopyIntent.ExecutionIntentId`. |
| `mt5_accounts` / `mt5_deals` / `reconstructed_trades` | Snapshot inputs (group name, XAU tape). Not admit lists. |
| `kill_switches` / `audit_logs` / `outbox_events` | Unrelated to membership. |

Grep of product `*.cs` for `roster`, `CopyRoster`, `EligibleTrader`, `AdmitTrader`, `PromoteTo` = **no type, no table, no API**.

Dashboard counts (`GetStatusAsync` / `EfDashboardQueries`) are **aggregates over `TraderScores`**, not a separate copy book.

---

## 4. Pickup timing: next 20 s poll **if SHADOW already exists**

Two independent loops:

| Host | Interval | Writes `CurrentState`? | Reads hopper set? |
|---|---|---|---|
| `LiveIngestHostedService` → `ReconstructionScoringService.RebuildTraderAsync` | After ingest pass (once per process start on that host; scores every login with deals) | **Yes** — `CurrentState = score.SuggestedState` | No |
| `apps/mt5-worker/Worker` → same `RebuildTraderAsync` | 30 s, hardcoded logins `{10001,10002,10003,99001}` | **Yes** (those four only) | No |
| `CopyTradingHostedService` → `GenerateShadowIntentsAsync` | 8 s then **20 s** | **No** | **Yes** |

Score write (`DealIngestionService.cs` / `ReconstructionScoringService` L126–142):

```126:142:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            // ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);
```

`UpsertScoreAsync` overwrites `CurrentState` on the existing `(BrokerId, Login)` row. There is no transition table and no “pending admit” flag.

Therefore a newly eligible trader is picked up **only** when **both** are true:

1. Scoring has already persisted `CurrentState == SHADOW` (or a non-existent product writer set LIVE / LIVE_CANDIDATE).
2. The next `CopyTradingHostedService` tick runs (≤ ~20 s after that write, plus the in-flight tick if one is already looping).

If scoring writes `WATCH`, the next 20 s poll **still does not see them**. Waiting is not promotion.

`PersistDemoShadowAsync` is a **separate** historical-shadow path (completed XAU only, gated `if (state != TraderState.SHADOW) return`). It is not the hosted hopper and it is not a roster.

---

## 5. WATCH never auto-promotes

### 5.1 Scorer outputs — `FromBaseline` only reaches SHADOW as the high state

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public static class TraderStateMachine
{
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
}
```

Reachable from `FromBaseline`:

| Condition | State | Copy hopper? |
|---|---|---|
| N=0 completed XAU | `INSUFFICIENT_DATA` | No |
| risk≥80 or (martingale ∧ DD>0 ∧ net<0) | `RISK_BLOCKED` | No |
| N<3 (`EarlyScoreTradeCount`) | `INSUFFICIENT_DATA` | No |
| quality≥70 ∧ risk<40 | **`SHADOW`** | **Yes (implicit)** |
| quality≥55 (else) | **`WATCH`** | **No** |
| else | `EARLY_SCORE` | No |
| — | `LIVE_CANDIDATE` | Query would include; **scorer never emits** |
| — | `LIVE` | Query would include; **scorer never emits**; `CanPromoteToLive` ≡ false |

`AfterHighEarlyScore()` also returns `SHADOW` only. Nothing in product calls a WATCH→SHADOW edge except a **later full rescore** that now meets `quality≥70 && risk<40`. That is a **new suggested state overwrite**, not a copy-tick promotion.

### 5.2 `CanPromoteToLive` is a hard false

```211:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static bool CanPromoteToLive(TraderState current) => false;
```

Unit pin (`tests/Unit/BaselineScorerTests.cs` L21–27): three disciplined winners → `SHADOW`, and `CanPromoteToLive(SHADOW)` is false. Trade #3 cannot auto-LIVE.

`CanPromoteToLive` is **not consulted** by `GenerateShadowIntentsAsync`. LIVE send is separately unreachable (`NewOrderSingleImplemented = false`).

### 5.3 Hopper + policy both ignore WATCH

- Query filter: WATCH not in `copyable`.
- Policy: WATCH → `TRADER_NOT_SHADOW_YET`.
- `GetStatusAsync` **counts** WATCH for the dashboard; it does not iterate them.

There is no timer, no “WATCH for N days then SHADOW,” and no operator promote endpoint in this tree.

---

## 6. What the tick does **after** implicit admit (not this slot’s change)

For each copyable + policy-eligible score:

1. Open XAU rows (`!Completed`) → idempotent `copy:…:open` intent, `RiskEngine.Evaluate`, persist `AllowFixSend = false`, status `SHADOW_ONLY` (LIVE send branch dead).
2. Closed XAU with a prior open intent → `copy:…:close` `SHADOW_ONLY` **without** `Evaluate`.

That is shadow bookkeeping. It is **not** dest capital at risk (`SAFE_BY_ABSENCE` of a sender). It is also **not** an admit/deny UI.

---

## 7. Operator-facing meaning (do not overclaim)

| Phrase | True meaning in this tree |
|---|---|
| “Auto-add” | Next 20 s tick will consider a login **already scored SHADOW**. |
| “Eligible trader” (hopper) | `CurrentState ∈ {SHADOW, LIVE_CANDIDATE, LIVE}`. Scorer only produces SHADOW. |
| “Eligible trader” (policy) | SHADOW-family **plus** N≥20, XAU net>0, no size-pattern, not demo/contest group. |
| “WATCH is in the pipeline” | Counted on `/api` status. **Not** copied. **Not** auto-promoted. |
| “Promotion” | Manual / nonexistent for LIVE. SHADOW only via scorer thresholds. |

---

## 8. Sources (absolute)

- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (`TraderStateMachine.FromBaseline`, `CanPromoteToLive`)
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Entities\TraderScore.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`UpsertScoreAsync`, `PersistDemoShadowAsync`)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`ReconstructionScoringService.RebuildTraderAsync`)
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`

**Product not edited.**
