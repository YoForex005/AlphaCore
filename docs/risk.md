# Risk Engine

## Hard Limits

All limits are configurable via environment variables with safe defaults:

| Limit | Default | Description |
|-------|---------|-------------|
| Max Daily Loss | 5% | Maximum drawdown from day-start equity |
| Max Total Loss | 10% | Maximum drawdown from initial balance |
| Max Position Size | 50 lots | Per-position volume cap |
| Max Open Positions | 25 | Concurrent open position limit |
| Max Daily Trades | 100 | Trade count circuit breaker |
| Slippage Tolerance | 30 points | Max acceptable copy-trade slippage |

Every trade request is evaluated against all limits **before** submission to MT5 or cTrader. A breach rejects the trade with a specific reason code.

## Kill Switch vs Emergency Flatten

Two distinct protective mechanisms:

### Kill Switch
- **What**: Disables all new trade submission for an account
- **When**: Daily loss limit breach, suspicious pattern detected, manual operator action
- **Effect**: Existing positions remain open; no new orders accepted
- **Reversal**: Manual only — operator must explicitly re-enable

### Emergency Flatten
- **What**: Closes all open positions immediately at market
- **When**: Total loss limit breach, margin emergency, operator panic button
- **Effect**: Market-order close of every open position; kill switch auto-enabled after
- **Reversal**: Positions are gone; kill switch must be manually cleared

```
Daily loss breach   → Kill Switch ON (positions stay)
Total loss breach   → Emergency Flatten → then Kill Switch ON
Operator action     → Either, depending on button pressed
```

## Copy Timing Rules

When mirroring MT5 trades to cTrader:

- **Minimum delay**: 100ms — prevents front-running detection by brokers
- **Maximum delay**: 2000ms — stale trades are rejected, not copied
- Delay is measured from MT5 deal timestamp to cTrader NewOrderSingle send time
- Trades outside the window are logged but not executed

## Slippage Guard

Copy-trade slippage is the difference between the MT5 fill price and the cTrader fill price:

```
slippage_points = abs(mt5_price - ctrader_price) / point_size
```

- Trades exceeding `RISK_SLIPPAGE_TOLERANCE_POINTS` are flagged
- Persistent high slippage triggers automatic copy-trade suspension
- Slippage is tracked per-symbol with rolling statistics

## Position Sizing Normalization

MT5 and cTrader use different volume conventions. The risk engine normalizes before comparison:

- MT5: volume in hundredths of lots (50000 = 5.0 lots)
- cTrader: volume in units specific to the symbol contract
- All internal calculations use standard lots as the canonical unit
- `MT5_VOLUME_SCALE=10000` converts MT5 native → lots
