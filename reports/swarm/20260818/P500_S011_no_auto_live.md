# P500_S011 — CanPromoteToLive is hard-false (keep it)

| Field | Value |
|---|---|
| Slot | S011 |
| Evidence | `BaselineScorer.TraderStateMachine` L209–211, `docs/scoring.md`, architecture §3 |

```
public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;
public static bool CanPromoteToLive(TraderState current) => false;
```

Trade #3 → `EARLY_SCORE` / `SHADOW`, never `LIVE`. Tests assert this (`BaselineScorerTests`).

Architecture: first-3 is **not** skill. Target is **future destination-net PnL** inside risk limits.

## Profit implication

Flipping `CanPromoteToLive` to true without OOS destination expectancy is how you lose. This flag is **lower-loss**, not a profit blocker.
