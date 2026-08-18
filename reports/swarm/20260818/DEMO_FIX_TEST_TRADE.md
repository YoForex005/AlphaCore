# Demo FIX test trade — measured 2026-08-18

Venue: `demo-us-eqx-01.p.c-trader.com` account **5328266** (not live 1369850).
Password not recorded here.

## Result

| Step | Outcome |
|---|---|
| TRADE logon | **True** |
| Symbol | **XAUUSD** id **41** |
| NewOrderSingle | **sent** ClOrdID `T20260818090931176` |
| Fill | **Yes** ExecType=F OrdStatus=2 LastQty=1 AvgPx **4391.38** |
| Position | 721=237338294 |
| Flatten | close 35=D sell submitted; follow-up `--flatten-only` submitted for 237338241 |

First attempt on XAUJPY was rejected `TRADING_DISABLED`. Exact XAUUSD then filled.

This does **not** prove future profitability of copied MT5 traders.
