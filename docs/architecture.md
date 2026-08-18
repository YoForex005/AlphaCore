# Trader Intelligence architecture (implementation map)

Source of truth: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.

## What exists now

| Layer | Path | Status |
|---|---|---|
| Domain algorithms | `src/Domain` | Reconstruction, symbol map, baseline score, risk, shadow, FIX FSM |
| Application | `src/Application` | Broker ports, ingestion, scoring, dashboard contracts |
| Persistence | `src/Infrastructure` | EF Core DbContext, in-memory fallback, demo seed |
| MT5 | `src/Mt5` + `mt5-sdk` | Fake connector for tests; C++ SDK preserved for Windows local mode |
| FIX | `src/Fix.CTrader` | Independent QUOTE/TRADE options, simulator harness. Real NewOrderSingle off |
| API | `apps/api` | Operational endpoints, no secrets |
| Workers | `apps/mt5-worker`, `apps/fix-worker` | Ingest/score loop; FIX heartbeat/status only |
| Web | `apps/web` | React + Vite dashboard pages |

## Safety defaults

- `REAL_COPY_EXECUTION_ENABLED=false`
- Trade #3 → SHADOW / EARLY_SCORE, never LIVE
- TargetCompID = `cServer` (case preserved)
- Volume default scale = 10_000 (`IMTDeal.Volume()`)
- Plan-group mappings are labels, not fetch filters

## Phases

Implemented toward first useful version (architecture §69): deterministic path through reconstruction, scoring, dashboard, and FIX session *state*. Live TRADE send and ML are explicitly not enabled.
