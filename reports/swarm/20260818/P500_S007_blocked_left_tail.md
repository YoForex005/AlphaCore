# P500_S007 — RISK_BLOCKED left tail dominates the book

| Field | Value |
|---|---|
| Slot | S007 |
| Date | 2026-08-18 |
| Evidence | live `/api/traders` + `TraderStateMachine` |

## Measured (mid-scoring, Achiever only)

| Bucket | N | Source PnL sum |
|---|---:|---:|
| SHADOW | 70 | +78,276 |
| WATCH | 79 | +8,178 |
| RISK_BLOCKED | 29 | **−241,580** |
| All scored XAU | 197 | **−154,425** |

All 29 blocked rows had `martingale=true`.

`FromBaseline`: `RISK_BLOCKED` if `risk >= 80` or `(Martingale && MaxDrawdown > 0 && NetPnl < 0)`.

## Profit implication

Copy-all XAU **is** copy-the-left-tail. Lower loss = never copy `RISK_BLOCKED`. Higher profit = only the residual after costs, not the +78k demo SHADOW headline.
