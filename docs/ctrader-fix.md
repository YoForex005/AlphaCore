# cTrader FIX 4.4 Integration

## Overview

cTrader is used as a **hedging execution venue** — not a liquidity provider. The prop firm's challenge accounts run on MT5; winning trades are copied to cTrader for real-money hedging via FIX 4.4 protocol (QuickFIX/N engine).

## Two FIX Sessions

cTrader requires two separate FIX sessions on different ports:

| Session | SSL Port | Plain Port | Purpose |
|---------|----------|------------|---------|
| QUOTE | 5211 | 5201 | Market data (SecurityList, MarketDataRequest) |
| TRADE | 5212 | 5202 | Order execution (NewOrderSingle, OrderStatusRequest) |

Both sessions share the same credentials but have distinct session qualifiers.

## FIX Header Mapping

```
Tag 49 (SenderCompID) = live.pepperstone.1369850
Tag 56 (TargetCompID) = cServer                    ← always "cServer"
Tag 50 (SenderSubID)  = <BROKER_ISSUED_VALUE>       ← per-session
Tag 57 (TargetSubID)  = QUOTE or TRADE              ← identifies session
Tag 1   (Account)     = 1369850
```

Session qualifier in QuickFIX config distinguishes the two sessions sharing the same SenderCompID/TargetCompID pair.

## Instrument Discovery (SecurityList)

Before trading, the QUOTE session sends a SecurityListRequest (MsgType=x) to discover available instruments and their specifications:

1. Send SecurityListRequest with SecurityListRequestType=0 (all securities)
2. cServer responds with SecurityList (MsgType=y) containing symbol, contract size, digits, min/max volume
3. Build local instrument map keyed by SecurityID
4. Re-request periodically or on session reconnect

## Quote Feed

Subscribe to market data via MarketDataRequest (MsgType=V) on the QUOTE session:

1. Subscribe with MDReqID, symbol, and requested depth
2. cServer streams MarketDataSnapshotFullRefresh (MsgType=W) and MarketDataIncrementalRefresh (MsgType=X)
3. Maintain local order book for pre-trade price validation

## Trade Execution Flow

On the TRADE session:

1. **NewOrderSingle** (MsgType=D) — market or limit order with ClOrdID, symbol, side, quantity, price
2. **ExecutionReport** (MsgType=8) — cServer confirms fill with ExecType, AvgPx, CumQty, LeavesQty
3. Match ExecutionReport.ClOrdID back to the originating MT5 copy-trade request
4. Record fill in PostgreSQL with reference to source MT5 deal tickets

## Reconciliation

Every copied trade must be reconciled:

- MT5 source deal ticket → cTrader ClOrdID → cTrader ExecID
- Compare fill price, volume, and timing
- Flag slippage exceeding threshold (default: 30 points)
- Unreconciled trades after timeout trigger alert

## Unknown State Handling

FIX has a critical failure mode: the order may have been accepted by the server but the ExecutionReport was lost (network partition, session disconnect). The system handles this:

1. On session reconnect, send OrderStatusRequest (MsgType=H) for all pending ClOrdIDs
2. If the order exists on cServer, process the ExecutionReport normally
3. If the order is unknown, mark the copy-trade as failed and alert
4. **Never re-submit** a trade in unknown state — manual intervention required
5. `REAL_COPY_EXECUTION_ENABLED=false` disables live execution; orders are simulated for testing
