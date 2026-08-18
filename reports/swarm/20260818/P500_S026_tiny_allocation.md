# P500_S026 — Tiny allocation is the only rational first size

| Field | Value |
|---|---|
| Slot | S026 |
| Evidence | `QuantityNormalizer`, live 303310 max 2.0 lots, `RiskLimits.MaxPositionQuantity=5` |

`Normalize(sourceLots * allocationFactor)` truncates to step and returns **0** below dest min.

Policy (not implemented as a live sender):

- `allocationFactor` 0.01–0.05
- Hard dest cap **0.05** XAU until 30+ shadow days +EV after costs
- Never use `MaxPositionQuantity=5` as a working size (that is a last-resort reject, and still too large)

Copying 303310 at 1.0–2.0 lots, or 70 SHADOW names at once, saturates one retail Pepperstone account.
