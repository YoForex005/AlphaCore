# P500_S027 — Grid / same-second tickets miss the martingale detector

| Field | Value |
|---|---|
| Slot | S027 |
| Evidence | `BaselineScorer` martingale loop + `TradeReconstructor.ScaleIn` + live 303274 |

Martingale flag: consecutive **completed trades** where prior PnL < 0 and next `MaxVolumeLots > 1.25 * prior`.

Averaging: `WasAveragedDown` only when **the same position** scales in at a worse price.

303274 opens many **distinct positionIds** at 0.05 lot in the same second. That is a grid, not a scale-in, not a 1.25× lot step. Scorer: `martingale=false`, `averagingDown=false`, SHADOW 93.50.

## Profit implication

Lower loss = flag same-minute multi-ticket same-symbol same-direction as concentration/grid and **do not copy**.
