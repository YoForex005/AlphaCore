# P500_S032 — Dashboard LoggedOn is a one-shot probe

| Field | Value |
|---|---|
| Slot | S032 |
| Evidence | `CTraderFixSession.TryLogonAsync` |

The method `using var tcp` / `await using var ssl`, writes Logon, reads once, returns. Caller does not keep the stream. Hosted service runs this twice (QUOTE 5211, TRADE 5212) and stores booleans.

A disposed socket cannot heartbeat (`35=0`), cannot `35=V`, cannot `35=D`, cannot `35=H` recon.

## Profit implication

`LoggedOn=true` is **not** a living venue session. It is a connectivity probe. Treat UI “healthy TRADE” as **logon-once**, not ready-for-execution.
