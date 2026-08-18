# Credentials and copy-trading status (measured 2026-08-18)

## Credentials

| Secret | Present on this machine? |
|---|---|
| `D:\Prop\.env` | **No** |
| App `.env` files | **No** |
| `MT5_PASSWORD` env | **No** |
| `MT5_STARWAVEFX_PASSWORD` env | **No** |
| `CTRADER_FIX_PASSWORD` env | **No** |
| .NET user-secrets store | **No** |

**Live Achiever / Starwave / Pepperstone logon cannot be tested.** Hosts and logins in `.env.example` are non-secret; passwords were never supplied.

I will not invent passwords or send a live `NewOrderSingle`.

## Copy trading to cTrader

| Check | Result |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **false** |
| Live FIX QUOTE TLS logon | **not proven** (no password) |
| Live FIX TRADE send | **OFF** (correct; §68 gates fail) |
| Demo shadow copy | **yes** — SHADOW traders only, destination-quote priced, no venue send |
| Risk rules | **unit-tested** (stale quote/signal, kill switch, martingale block) |
| First-3 / no LIVE auto-promote | **yes** |

## Local dashboard (running)

- UI: http://127.0.0.1:3000/  (all listed routes HTTP 200)
- API: http://127.0.0.1:5000/  (health, overview, brokers, groups, traders, trades, FIX, risk, settings)

Demo overview: 2 SHADOW, 1 RISK_BLOCKED, 0 LIVE, `realCopyEnabled=false`.
