# Live Manager fetch — measured 2026-08-18

## Result

Native MT5 Manager API connected to **both** owned brokers. Dummy/seeded dashboard path is off.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct | 10 | 1948 | 478 | same |

**Total: 18 groups, 8460 manager traders.**

Artifact (full login list, no passwords): `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

## Achiever groups

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |

These are **all groups this manager login can see**. If the server has more groups, they are outside this manager's permission set.

## Starwave groups

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |

## Copy to cTrader / no loss

- `REAL_COPY_EXECUTION_ENABLED` forced **false**.
- No `35=D` NewOrderSingle exists in `CTraderFixSession`.
- First FIX logon used SenderCompID as tag 553 → reject `Could not parse Username. Expecting a integer value.`
- Fix applied: tag 553 = integer account id `1369850`. Logon is for quotes/recon only.
- Live send stays off until: TRADE logon + recon + risk approve + explicit go-live flag.

## Dashboard

- Vite: `http://127.0.0.1:3000/`
- API: `http://127.0.0.1:5000/`
- `/api/groups` and `/api/traders` read live Manager catalog (not FakeMt5 10001/10002).
- In-memory DB: restart re-fetches. `DATABASE_URL` is still a placeholder.

## Honesty

- This is **not** "EX5 decompiled" and not 95% copy-trading live.
- It **is** a measured live Manager census of every group and every login the two manager accounts can see.
