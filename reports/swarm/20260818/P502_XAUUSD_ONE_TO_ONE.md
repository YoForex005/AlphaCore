# P502 — How profitable XAUUSD trades are selected (and what was tested)

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Code | `src/Domain/Copy/XauUsdOneToOneCopyPolicy.cs` |
| Tests | `tests/Unit/XauUsdOneToOneCopyPolicyTests.cs` **12/12 PASS** |
| Demo FIX | account `5328266` host `demo-us-eqx-01` instrument **41=XAUUSD** |
| Live 1369850 send | **No** |

## The rule you asked for, stated honestly

You cannot copy a ticket **only after it is profitable** and also copy its **exact open time**. Profit is only known at close. Waiting for the close to decide “this was a winner, now copy it” is **lookahead**. That is not a live trade; it is a history replay.

What **is** implementable and what this repo now does:

```
1. Select TRADERS (not closed tickets)
   SHADOW+  AND  ≥20 completed XAU  AND  XAU net PnL > 0
   AND not martingale / averaging / lot-escalation
   AND group is not demo\ or contest\

2. Copy only XAUUSD (aliases GOLD / XAUxxx)

3. 1:1 lots  (allocationFactor = 1)
   FIX units = lots × 100 oz  (measured: 0.01 lot = 38=1 filled)

4. Same side, same SL/TP if the source deal carried them
   Limit needs tag 44, Stop needs tag 99

5. Open when the source position is STILL OPEN
   Close when the source CLOSES (event), not at a guessed time

6. Do not open a copy of a trade that is already closed
   (that is the “only copy winners” cheat — rejected as NO_LOOKAHEAD_CLOSED_WINNER)
```

“Same exact closing time” means: we send the dest close **when the source close deal arrives**, not earlier. Network+FIX is milliseconds to tens of ms, not identical timestamps.

Pending/limits: reconstruction today is **deals** (fills). A working limit/stop on MT5 is an **order**, not a deal. 1:1 pending copy needs the Manager order pump. The dest FIX path **does** accept limit (`40=2`) and stop (`40=3`) — measured on demo.

## Measured dest FIX (demo)

| Scenario | Result |
|---|---|
| TRADE logon | PASS `35=A` |
| SecurityList XAUUSD | PASS id **41** |
| Market **buy** 1 oz @ ~4391 (prior single test) | **FILL** `150=F` `6=4391.47` pos `237338293` |
| Market **sell** 1 oz | **FILL** `6=4392.86` then flatten **FILL** `6=4392.81` |
| Limit buy 1000 (far) | **RESTING** `40=2` `150=0` `59=1` |
| Limit sell 9000 (far) | **RESTING** |
| Stop buy 9000 | **RESTING** `40=3` `99=9000` |
| Stop sell 1000 | **RESTING** |
| Bad symbol | **REJECT** `35=j` `Symbol(55) 99999999 does not exist.` |
| Cancel `35=F` **with** tag 54 | **REJECT** `Tag not defined for this message type, field=54` |
| Market buy with tags 1000/1002 on `35=D` | **REJECT** `Tag not defined for this message type, field=1000` — SL/TP are **not** legal on NewOrderSingle here |
| Market buy + flatten (no SL tags) | **FILL** 4391.89 then flatten **FILL** 4391.73 |
| Cancel `35=F` with tag 60 | **REJECT** `field=60` not defined on F — venue cancel is a tiny field set |

Cancel without tag 54 is the official-shaped fix; re-run pending.

## Unit tests (selection)

12/12 passed: 1:1 lots+SL/TP, lookahead closed-winner reject, EURUSD reject, martingale, negative XAU PnL, <20 trades, demo group, limit/stop missing price, close 1:1, qty below min.

## What this will **not** do

- Guarantee profit.
- Copy demo `yo-2step` SHADOW names (those are blocked by group).
- Copy EUR/BTC.
- Fire live Pepperstone `1369850`.
- Copy a closed winner after the fact.
