# Credentials and copy-trading status (remeasured 2026-08-18)

## Credentials (names only)

| Secret | Present? |
|---|---|
| `D:\Prop\.env` | **Yes** |
| `MT5_PASSWORD` | **Yes** (len 8) |
| `MT5_STARWAVEFX_PASSWORD` | **Yes** (len 11) |
| Achiever HTTP proxy user/pass | **Yes** |
| `CTRADER_FIX_PASSWORD` | **Yes** (len 10) |
| `DATABASE_URL` | placeholder → API uses in-memory DB |

Values are not written here.

## Live Manager census (measured)

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via whitelist HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | OK direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Dashboard `/api/traders` returned **8460**. `/api/groups` returned **18**.

## Copy trading to cTrader

| Check | Result |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **false** (forced) |
| FIX QUOTE TLS logon | **true** after tag 553 = integer account id |
| FIX TRADE TLS logon | **true** |
| Live `35=D` NewOrderSingle | **OFF** — method does not exist |
| Dummy FakeMt5 seed on API | **OFF** |
| Risk / first-3 / no auto LIVE | still in force |

Copy intents stay SHADOW-only. That is how this process avoids taking a live loss.

## Local dashboard

- UI: http://127.0.0.1:3000/
- API: http://127.0.0.1:5000/
- Ingest: http://127.0.0.1:5000/api/ingest/status
- Full login dump: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
