# Trader Intelligence

Short architecture overview and where to find implementation details.

![Architecture](docs/architecture.svg)

Summary: lightweight C#/.NET backend that ingests MT5 manager events, reconstructs trades, scores XAUUSD traders, shadow-copies approved trades and routes execution to a cTrader FIX 4.4 adapter.

Key components:

- **Ingest / Collectors:** `apps/mt5-worker`
- **API:** `apps/api`
- **Workers:** `apps/mt5-worker`, `apps/fix-worker`
- **Domain logic:** `src/Domain`
- **Persistence / Infrastructure:** `src/Infrastructure`
- **FIX adapter:** `src/Fix.CTrader`
- **Web dashboard:** `apps/web`

For the full architecture spec see `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` and `docs/architecture.md`.
# MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4

Identify copyable XAUUSD traders from ~5,000 MT5 accounts, shadow them, and only then (explicit flag) route risk-approved orders to Pepperstone/cServer FIX 4.4.

Architecture: `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

## Safety

- Real NewOrderSingle is **off** (`REAL_COPY_EXECUTION_ENABLED=false`).
- Trade #3 is early evidence, never LIVE promotion.
- Secrets stay in environment / `.env` (see `.env.example`). Never sent to React.

## Run (demo)

```powershell
dotnet test D:\Prop\Mt5TraderIntelligence.sln
dotnet run --project D:\Prop\apps\api\TraderIntelligence.Api.csproj
cd D:\Prop\apps\web
npm install
npm run dev
```

API: http://localhost:5000  
Dashboard: http://localhost:3000  

Without Postgres the API uses EF InMemory and seeds Achiever + StarwaveFX demo accounts.

## Native MT5

`mt5-sdk` is Windows-only for local Manager API. Keep the worker on Windows. Do not Linux-container the native DLL.
