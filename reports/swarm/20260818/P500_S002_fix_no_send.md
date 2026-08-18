# P500_S002 — Live NewOrderSingle is absent

| Field | Value |
|---|---|
| Slot | S002 |
| Date | 2026-08-18 |
| Evidence | `src/Fix.CTrader/Sessions/CTraderFixSession.cs`, `Hosting/CTraderFixLogonHostedService.cs` |
| Product source modified | No |
| Live 35=D | **No** |

## Verdict

**`SAFE_BY_ABSENCE`.** Outbound FIX from this process is **Logon `35=A` only**. After one read, `TcpClient`/`SslStream` are disposed. Hosted service sets `RealCopyEnabled = false` and logs `NewOrderSingle still disabled`.

Live `/api/fix/sessions`: QUOTE+TRADE `LoggedOn=true`, `executionEnabled=false`, bid/ask null.

Official cTrader FIX 4.4 has NewOrderSingle on TRADE. This repo has no builder, no tag 38, no persist-before-send socket write.

## Profit implication

You cannot “send trades to make profit” today. Adding a sender without quotes/recon/sizing is a **loss** path, not a profit path.
