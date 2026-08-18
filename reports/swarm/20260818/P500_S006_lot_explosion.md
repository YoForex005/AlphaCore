# P500_S006 — Top SHADOW 303310 is a lot explosion, not an edge

| Field | Value |
|---|---|
| Slot | S006 |
| Login | ACHIEVER 303310 |
| Group | `demo\yo-2step` |
| State | SHADOW |
| earlyScore | 91.75 |
| riskScore | 25 |
| lotEscalation | **true** |
| completedXauTrades | 22 |
| netSourcePnl | +41,634.21 |

Live detail: 43 reconstructed rows (FX + BTC + XAU). Lots **0.01 → 2.0**. One ticket PnL **+13,692**. XAU examples: +603 at 1.0 lot in ~18 minutes, −67 at 1.0 lot.

`QuantityNormalizer` exists and is **not wired** to a FIX sender. Copying source size onto Pepperstone is how the dest account dies.

## Profit implication

allocationFactor must be **0.01–0.05** with a hard dest cap **0.05** lot gold until shadow after costs is green. Keep `CanPromoteToLive=false`.
