# P500_S003 — RiskEngine reduces loss only if a sender exists

| Field | Value |
|---|---|
| Slot | S003 |
| Evidence | `src/Domain/Risk/RiskEngine.cs` |

`AllowFixSend` is a **DTO boolean**. Nothing in `Fix.CTrader` reads it. Caps that *would* cut loss if wired:

- `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` block increasing exposure
- Unreconciled / unhealthy venue
- Quote missing / stale (3s) / spread / $3 price move
- Signal age > 15s
- Max loss/trader $500, daily exec −$2000, DD $3000
- Max open 20, max qty 5, gross XAU 20, net 10
- Margin > 70%
- Martingale / abnormal sizing

`MaxSlippage` is **unused**. 5-lot / 70% margin / $2 spread are **too loose** for a single Pepperstone gold account.

## Profit implication

RiskEngine is a **loss cap**, not an edge. It is not on a send choke today (`SAFE_BY_ABSENCE` is what actually protects capital).
