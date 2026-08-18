# Trade Reconstruction

## Overview

MT5 records individual **deals** (executions), not logical trades. A single trade lifecycle produces multiple deals:

```
Deal 1: BUY  0.5 lot XAUUSD @ 2340.50  (entry=IN,  position=12345)
Deal 2: SELL 0.3 lot XAUUSD @ 2345.00  (entry=OUT, position=12345)
Deal 3: SELL 0.2 lot XAUUSD @ 2343.00  (entry=OUT, position=12345)
```

Trade reconstruction groups deals by `position_ticket` to compute:
- Entry price (volume-weighted average of IN deals)
- Exit price (volume-weighted average of OUT deals)
- Total P&L (sum of deal profits + commission + swap)
- Duration (first IN deal time → last OUT deal time)
- Status: open (has IN but not fully closed) or closed

## Deal Entry Types

| Entry Code | Meaning |
|-----------|---------|
| 0 (IN) | Opening or adding to a position |
| 1 (OUT) | Partial or full close |
| 2 (INOUT) | Reverse (close + open in opposite direction) |
| 3 (OUT_BY) | Close by opposite position |

## XAUUSD Normalization

Gold (XAUUSD) volume on MT5 is in hundredths of lots where 1 lot = 100 oz. The system normalizes all volumes through `MT5_VOLUME_SCALE` (default 10000) to convert native MT5 volume integers to standard lot sizes:

```
display_lots = native_volume / MT5_VOLUME_SCALE
```

Example: MT5 volume `50000` = 5.0 standard lots = 500 oz of gold.

Price is quoted per troy ounce. P&L for 1 lot (100 oz) with a 10-point move = $1,000.

## First-3-Trade Semantics

For prop firm challenge evaluation, the system tracks the **first 3 completed trades** on an account. These trades determine:

1. Whether the trader followed opening rules (minimum size, correct instrument set)
2. Initial risk behavior (did they immediately max out position size?)
3. Pattern detection for pass/fail algorithms

A "completed trade" means all IN volume for a position ticket has been fully matched by OUT volume. Partial closes count toward the same trade — only fully closed positions increment the trade counter.

## Reconstruction Pipeline

1. Query deals for account within evaluation window
2. Group by `position_ticket`
3. For each group, separate IN vs OUT deals
4. Compute VWAP entry/exit, total P&L, net volume
5. Mark as open (remaining IN volume > 0) or closed
6. Store reconstructed trade with reference to source deal tickets
