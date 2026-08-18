# P500_S008 — No destination quote tape ⇒ shadow PnL stays 0

| Field | Value |
|---|---|
| Slot | S008 |
| Evidence | `EfTradingStore.PersistDemoShadowAsync`, live `/api/fix/sessions` |

`PersistDemoShadowAsync` loads `DestinationQuotes` newest-first. **If null, it saves the score outbox and returns** — no `ShadowOrder`, no modeled fill.

Live FIX DTO: `bid=null`, `ask=null`, `quoteAgeSeconds=null`. Logon probe does not send `35=V`.

Overview `shadowPnl=0`, `destinationRealPnl=0` is therefore **measured absence**, not “break-even trading.”

## Profit implication

There is **no** destination expectancy number. Enabling live send now is gambling.
