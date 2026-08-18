# Deployment Guide

## Platform Requirements

| Component | OS | Reason |
|-----------|-----|--------|
| MT5 Worker | **Windows** | MetaTrader 5 Manager API is a native Windows DLL (requires `MT5APIManager.dll`) |
| .NET API Gateway | Linux or Windows | ASP.NET Core 8 is cross-platform |
| cTrader FIX Engine | Linux or Windows | QuickFIX/N is pure .NET |
| PostgreSQL | Linux | Standard deployment |
| Redis | Linux | Standard deployment |
| React Dashboard | Any (static files) | Vite build output served by any web server |

## MT5 Worker (Windows)

The MT5 Manager API SDK is Windows-only. The worker process must run on Windows with:

1. Visual C++ Runtime 2022 (x64)
2. The vendor SDK DLL at a known path: `vendor/MetaTrader5SDK/Libs/MT5APIManager64.dll`
3. Network access to broker MT5 servers on port 443
4. Sufficient outbound IP allowlisting (Achiever egress: `81.29.145.69`)

```
# Build (from mt5-sdk/)
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release

# Run
set MT5_SERVER=57.128.141.65
set MT5_PORT=443
set MT5_LOGIN=2027
set MT5_PASSWORD=...
build\Release\mt5_worker.exe
```

## .NET Services (Linux)

```bash
# Build
dotnet publish -c Release -o out/

# Run
export DATABASE_URL="Host=db;Port=5432;..."
export REDIS_URL="redis:6379"
dotnet out/TraderIntelligence.dll
```

## Docker (Linux components only)

The .NET gateway, PostgreSQL, and Redis can run in Docker. The MT5 worker **cannot** be containerized (the MT5 SDK DLL requires native Windows APIs not available in Windows containers reliably).

```yaml
# docker-compose.yml (simplified)
services:
  api:
    build: ./src/api
    ports: ["5000:5000"]
    env_file: .env
    depends_on: [db, redis]

  db:
    image: postgres:16
    volumes: ["pgdata:/var/lib/postgresql/data"]
    environment:
      POSTGRES_DB: trader_intelligence
      POSTGRES_USER: ti
      POSTGRES_PASSWORD: ${DB_PASSWORD}

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

volumes:
  pgdata:
```

## Environment Variable Setup

1. Copy `.env.example` to `.env`
2. Fill all `<SECRET>` placeholders with real credentials
3. Set `MT5_PASSWORD_ENCRYPTION_KEY` to a fresh 256-bit base64 key
4. Set `REAL_COPY_EXECUTION_ENABLED=false` until cTrader integration is verified
5. Verify network connectivity to both MT5 broker IPs on port 443

## Proxy Configuration

If the MT5 worker runs behind a proxy (e.g., for IP allowlisting):

```
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=proxy.example.com
ACHIEVER_PROXY_PORT=8080
ACHIEVER_PROXY_USERNAME=...
ACHIEVER_PROXY_PASSWORD=...
```

The `MT5Manager::SetProxy()` call is applied before every `Connect()` attempt, including automatic reconnections.

## Health Checks

- MT5 Worker: `IsConnected()` on each pool session
- .NET API: `/health` endpoint checks DB + Redis + MT5 worker connectivity
- FIX Engine: QuickFIX session state (logged in / disconnected)
