# A65 — Docker Compose design (Postgres, Redis, API, web)

| Field | Value |
|---|---|
| Agent | A65 |
| Date | 2026-08-18 |
| Scope | Design only. Proposed `docker-compose.yml`, Dockerfiles, nginx, `.dockerignore`, and `.env.example`. |
| Product source modified | **No.** This file is the only write. |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §5 Deployment, §13 no Kafka, §55 placeholders-only, §71 no K8s |
| Sequence | A30 I0 = Postgres + Redis only. This design **adds** API + web behind a Compose profile so I0 still works. |
| Secrets | `.env.example` uses **placeholders only**. No live MT5 IPs, manager logins, cTrader account IDs, or passwords. |

**Verdict:** Compose is **MISSING** on disk. Below is the implementable lab topology. Default `docker compose up -d` must stay I0-safe (Postgres + Redis). `docker compose --profile apps up -d --build` adds API + web. **`mt5-worker` / `mt5-collector` stay off Compose** — MetaQuotes Manager API is Windows-native.

---

## 1. Measured state (do not greenwash)

| Artifact | On disk 2026-08-18 |
|---|---|
| `D:\Prop\docker-compose.yml` | **MISSING** |
| `apps/api/Dockerfile`, `apps/web/Dockerfile` | **MISSING** |
| `apps/web/nginx.conf`, root `.dockerignore` | **MISSING** |
| `D:\Prop\.env.example` | **EXISTS** — not placeholder-only (live Achiever/StarwaveFX/cTrader targeting identifiers). A65 does **not** rewrite it. Replacement text is in §7. |
| `D:\Prop\.gitignore` | EXISTS — ignores `.env` / `.env.*`, un-ignores `.env.example` |
| `apps/api` | net8.0 weatherforecast template. Listens `http://localhost:5160` in `launchSettings.json`. `TraderIntelligence.Api.http` uses `:5160`. **No** `ConnectionStrings`, health, CORS, SignalR map, EF registration. `UseHttpsRedirection()` is on. |
| `apps/web` | Vite + React. Dev server **port 3000**. Axios + SignalR default `VITE_API_URL \|\| http://localhost:5000`. Hub path `/hubs/dashboard`. Hooks call `/api/...` (not A26 `/api/v1/...`). |
| `src/Infrastructure` | `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4 + `StackExchange.Redis` 2.8.0. `TraderDbContext` exists. **No** `Migrations/` folder. |
| `apps/mt5-worker` | Template delay loop. Project refs `src/Mt5`. **No** native DLL copy, **no** P/Invoke. |
| `mt5-sdk` | Local transport = `MT5APIManager64.dll` via `LoadLibrary`. **Windows x64 only.** Remote HTTP client exists; **no in-repo MT5 HTTP server**. |
| Kafka / K8s / Helm | Correctly absent (§71). Do not add. |

Port collision to resolve in Compose (not by editing product today):

| Consumer | Default |
|---|---|
| Vite | `3000` |
| Web axios / SignalR fallback | `http://localhost:5000` |
| `dotnet run` API (launchSettings) | `5160` / `7294` |
| A26 orchestrator live probe | `GET /api/v1/health/live` (**not implemented**) |
| A26 dashboard health | `GET /api/v1/health` (auth) |
| Web hooks today | `GET /api/health` |
| mt5-sdk remote example | `http://127.0.0.1:9100` (Windows sidecar, **not** Compose) |

**Compose publishes API as `5000:8080`** so the existing browser fallback works without changing `client.ts`. Local `dotnet run` may keep `:5160`.

---

## 2. Topology

```text
Windows host (Docker Desktop = Linux engine)
│
│  docker compose                 docker compose --profile apps
│  ┌─────────────┐                ┌──────────────────────────┐
│  │ postgres:16 │◄───────────────│ api  (.NET 8, :8080)     │
│  │   :5432     │                │  ASPNETCORE_URLS=http    │
│  └─────────────┘                └──────────▲───────────────┘
│  ┌─────────────┐                           │ /api /hubs
│  │ redis:7     │◄───────────────           │ (browser may
│  │   :6379     │                ┌──────────┴───────────────┐
│  └─────────────┘                │ web  (nginx :80 → :3000) │
│                                 │  VITE_API_URL=           │
│                                 │  http://localhost:5000   │
│                                 └──────────────────────────┘
│
│  NOT in Compose (native Windows)
│  ┌─────────────────────────────────────────────────────────┐
│  │ apps/mt5-worker   (future)  +  mt5-sdk local Manager    │
│  │   MT5APIManager64.dll / MetaQuotes.MT5ManagerAPI64.dll  │
│  │   MetaQuotes.MT5CommonAPI64.dll                         │
│  │   connects to localhost:5432 and localhost:6379         │
│  │ apps/fix-worker   (future; QuickFIX/n can be Linux later)│
│  └─────────────────────────────────────────────────────────┘
```

Rules:

1. PostgreSQL is the durable source of truth (arch §5 / §254–260).
2. Redis is cache, live scores, short locks, FIX-session **lease + fencing token** only. **Never** orders / positions / balances (arch §5 / §28).
3. Outbox = Postgres table + worker poll. **No Kafka** (A30, §13).
4. React never receives MT5 / FIX / DB / Redis / proxy secrets (arch §55).
5. `REAL_COPY_EXECUTION_ENABLED=false` in every example.

### 2.1 Why MT5 is not a Compose service

Architecture §5:

```text
Docker where compatible
Windows Worker if MT5 Manager DLL requires Windows
Linux for API/Postgres/Redis/Python/React if appropriate
Do not force native MT5 SDK components into Linux containers
if the SDK does not support it cleanly.
```

Evidence (A14, `mt5-sdk/README.md`):

- `MT5_MODE=local` loads PE32+ Manager DLLs with `LoadLibrary` / `CMTManagerAPIFactory`.
- Required runtime copies (CMake `mt5sdk_copy_runtime_dlls`):
  - `vendor/MetaTrader5SDK/Libs/MT5APIManager64.dll`
  - `vendor/MetaTrader5SDK/Libs/MetaQuotes.MT5ManagerAPI64.dll`
  - `vendor/MetaTrader5SDK/Libs/MetaQuotes.MT5CommonAPI64.dll`
- MSVC 2022 + `x64-windows` triplet. ARM64 Manager DLLs exist in the SDK tree; this lab is x64.
- There is **no** Linux Manager API. Wine / Windows-container-for-DLL is **out of scope** (unsupported, unclean).
- `MT5_MODE=remote` is a **client** of an HTTP sidecar. That sidecar is **not in this repo**. If it is built later, it still must run **on Windows** (`:9100`) so it can load the same DLLs. A Linux `mt5-worker` may then talk HTTP to `host.docker.internal:9100`. Do not add that service until the sidecar exists.

**Windows operator runbook (native worker + Compose data plane):**

```powershell
# 1) Linux engine (not Windows containers)
docker compose up -d

# 2) From an elevated or normal PowerShell on the host
#    Worker uses host-published ports, not Docker DNS names.
$env:ConnectionStrings__Postgres = "Host=127.0.0.1;Port=5432;Database=trader_intelligence;Username=ti;Password=<SECRET>"
$env:ConnectionStrings__Redis    = "127.0.0.1:6379,password=<SECRET>,abortConnect=false"
$env:MT5_MODE                    = "local"
# Copy Manager DLLs beside the worker exe when that wiring exists.
dotnet run --project apps/mt5-worker -c Release
```

Do **not** set `MT5_SERVER=postgres` or any Compose hostname on the worker. Broker IPs are **egress from the Windows host**. Compose does not NAT the Manager session.

---

## 3. Proposed files (do not create in this agent)

| Path | Role |
|---|---|
| `D:\Prop\docker-compose.yml` | Four services. Default profile = data plane. `apps` profile = api + web. |
| `D:\Prop\.dockerignore` | Keep image context small; never copy `.env`. |
| `D:\Prop\apps\api\Dockerfile` | Multi-stage `sdk:8.0` → `aspnet:8.0`. Context = repo root. |
| `D:\Prop\apps\web\Dockerfile` | Multi-stage `node:20-alpine` → `nginx:1.27-alpine`. |
| `D:\Prop\apps\web\nginx.conf` | SPA fallback. Optional `/api` + `/hubs` reverse proxy (see §5.3). |
| `D:\Prop\.env.example` | Placeholders only (replace current live identifiers when I0 lands). |

Optional later (not I0, not this design’s required set):

| Path | Role |
|---|---|
| `docker-compose.override.yml` | Local bind-mounts / extra host ports. Gitignored if it ever holds secrets. |
| `apps/api/Dockerfile` health | Switch probe from `/weatherforecast` to `/api/v1/health/live` in I6. |

---

## 4. `docker-compose.yml` (proposed)

Compose Specification (no obsolete `version:` key). Project name `ti`. Images pinned to the tags A30 named (`postgres:16`, `redis:7`).

```yaml
name: ti

# Default: postgres + redis          →  docker compose up -d
# Plus API + web:                    →  docker compose --profile apps up -d --build
# Never: mt5-worker, mt5-collector, fix-worker, ml-service, Kafka, K8s

services:
  postgres:
    image: postgres:16
    container_name: ti-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      TZ: UTC
      PGTZ: UTC
    ports:
      - "${POSTGRES_PORT}:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 10s
    networks: [ti]

  redis:
    image: redis:7
    container_name: ti-redis
    restart: unless-stopped
    command:
      - redis-server
      - --requirepass
      - ${REDIS_PASSWORD}
      - --maxmemory
      - 256mb
      - --maxmemory-policy
      - allkeys-lru
      - --save
      - ""
      - --appendonly
      - "no"
    ports:
      - "${REDIS_PORT}:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD}", "ping"]
      interval: 5s
      timeout: 3s
      retries: 10
      start_period: 5s
    networks: [ti]
    # No volume. Redis is not the source of truth. Evict freely.
    # A fencing-token lease must survive only as long as the lock holder;
    # Postgres remains authority for execution state (arch §28 / §62).

  api:
    profiles: [apps]
    build:
      context: .
      dockerfile: apps/api/Dockerfile
    image: ti-api:local
    container_name: ti-api
    restart: unless-stopped
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT}
      ASPNETCORE_URLS: http://+:8080
      # Template Program.cs calls UseHttpsRedirection(). HTTP-only
      # compose will 30x-loop until that call is gated off in I6.
      ConnectionStrings__Postgres: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ConnectionStrings__Redis: redis:6379,password=${REDIS_PASSWORD},abortConnect=false
      DATABASE_URL: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      REDIS_URL: redis:6379,password=${REDIS_PASSWORD},abortConnect=false
      REAL_COPY_EXECUTION_ENABLED: "false"
    ports:
      - "${API_PORT}:8080"
    healthcheck:
      # TEMP until A26 GET /api/v1/health/live exists (I6).
      # Replace the path; do not keep probing /weatherforecast after I6.
      test: ["CMD-SHELL", "curl -fsS http://127.0.0.1:8080/weatherforecast >/dev/null || exit 1"]
      interval: 15s
      timeout: 5s
      retries: 8
      start_period: 30s
    networks: [ti]

  web:
    profiles: [apps]
    build:
      context: ./apps/web
      dockerfile: Dockerfile
      args:
        # Browser-visible URL. Never http://api:8080 — that hostname
        # does not resolve in the operator's browser.
        VITE_API_URL: ${VITE_API_URL}
    image: ti-web:local
    container_name: ti-web
    restart: unless-stopped
    depends_on:
      api:
        condition: service_started
    ports:
      - "${WEB_PORT}:80"
    networks: [ti]
    # Intentionally no DATABASE_URL / REDIS_* / MT5_* / CTRADER_* here.

volumes:
  postgres_data:

networks:
  ti:
    driver: bridge
```

**Not in this file (explicit):**

- `mt5-worker`, `mt5-collector`, `fix-worker`, `ml-service`
- Kafka / Redpanda / NATS / Rabbit / MassTransit
- ClickHouse, Loki, Prometheus stack (add later only if ops asks)
- Windows container service for Manager DLLs
- Bind-mount of `vendor/MetaTrader5SDK/Libs`

### 4.1 Operator commands

```powershell
cd D:\Prop
copy .env.example .env
# edit .env — fill <SECRET> only; never commit .env

# I0 / everyday data plane
docker compose up -d
docker compose ps
docker compose exec postgres pg_isready -U ti -d trader_intelligence
docker compose exec redis redis-cli -a <SECRET> ping

# I6+ dashboard stack
docker compose --profile apps up -d --build

# teardown (keep volume)
docker compose --profile apps down

# wipe Postgres (destroys lab data)
docker compose down -v
```

---

## 5. Dockerfiles and nginx (proposed)

### 5.1 `apps/api/Dockerfile`

Context **must** be the repo root: `TraderIntelligence.Api` references `src/Domain`, `src/Application`, `src/Infrastructure`.

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY apps/api/TraderIntelligence.Api.csproj apps/api/
COPY src/Domain/TraderIntelligence.Domain.csproj src/Domain/
COPY src/Application/TraderIntelligence.Application.csproj src/Application/
COPY src/Infrastructure/TraderIntelligence.Infrastructure.csproj src/Infrastructure/

RUN dotnet restore apps/api/TraderIntelligence.Api.csproj

COPY apps/api/ apps/api/
COPY src/Domain/ src/Domain/
COPY src/Application/ src/Application/
COPY src/Infrastructure/ src/Infrastructure/

RUN dotnet publish apps/api/TraderIntelligence.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "TraderIntelligence.Api.dll"]
```

`aspnet:8.0` already defines a non-root `app` user. Do not copy `appsettings.Development.json` secrets (gitignore already excludes that file).

**Known break until I6:** current `Program.cs` calls `UseHttpsRedirection()` and has no `/weatherforecast` dependency on Postgres. The container will start, but browsers hitting `http://localhost:5000` may be redirected to HTTPS and fail. Gate redirection on `ASPNETCORE_URLS` containing `https` when API work starts. Do not “fix” that in this report’s agent.

Official runtime image **does not include curl** — the `apt-get` line is only for the temporary healthcheck. After `/api/v1/health/live` exists, prefer a tiny built-in probe or keep curl.

### 5.2 `apps/web/Dockerfile`

```dockerfile
# syntax=docker/dockerfile:1
FROM node:20-alpine AS build
WORKDIR /web
COPY package.json package-lock.json* ./
RUN npm ci --ignore-scripts || npm install --ignore-scripts
COPY . .
ARG VITE_API_URL=http://localhost:5000
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=15s --timeout=3s --retries=5 \
  CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
```

There is **no** `package-lock.json` today. The `npm ci || npm install` fallback is honest. Add a lockfile in the same increment that adds the Dockerfile.

`VITE_*` is baked in at `npm run build`. Changing `.env` after the image is built does nothing until rebuild. That is why the URL must be the **browser** URL (`http://localhost:5000`), not `http://api:8080`.

### 5.3 `apps/web/nginx.conf`

Minimal SPA. Proxy is **optional**. Today `client.ts` ignores same-origin unless `VITE_API_URL` is set, so the default path is: browser → `:5000` API directly, plus CORS on the API in I6.

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    gzip on;
    gzip_types text/css application/javascript application/json;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Ready for a later same-origin cutover (requires client.ts to
    # treat empty VITE_API_URL as "" / window.location.origin, not
    # falling back to http://localhost:5000).
    location /api/ {
        proxy_pass http://api:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /hubs/ {
        proxy_pass http://api:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_read_timeout 3600s;
    }
}
```

Until `client.ts` is changed, operators open `http://localhost:3000` (UI) and the UI calls `http://localhost:5000` (API). SignalR needs API CORS + WebSockets.

### 5.4 Root `.dockerignore`

```gitignore
**/.git
**/.vs
**/.idea
**/bin
**/obj
**/node_modules
**/dist
**/.vite
**/.env
**/.env.*
!**/.env.example
**/appsettings.Development.json
**/appsettings.*.local.json
**/*.user
**/*.pfx
reports/
docs/
mt5-sdk/
vendor/
apps/mt5-worker/
apps/fix-worker/
apps/web/node_modules/
tests/
*.log
```

Do not send Manager DLLs into the Linux API/web build context.

---

## 6. Ports, env binding, health

| Service | Container | Host (from `.env`) | Health |
|---|---|---|---|
| postgres | 5432 | `POSTGRES_PORT` default `5432` | `pg_isready` |
| redis | 6379 | `REDIS_PORT` default `6379` | `redis-cli -a … ping` |
| api | 8080 | `API_PORT` default `5000` | TEMP `/weatherforecast` → I6 `/api/v1/health/live` |
| web | 80 | `WEB_PORT` default `3000` | `GET /` |

ASP.NET configuration binding (when I1/I6 wires it):

| Env var | Binds to |
|---|---|
| `ConnectionStrings__Postgres` | `ConnectionStrings:Postgres` |
| `ConnectionStrings__Redis` | `ConnectionStrings:Redis` |
| `DATABASE_URL` | same Npgsql string; matches `mt5-sdk` / existing root key |
| `REDIS_URL` | StackExchange.Redis config string |

Inside Compose, hostnames are `postgres` and `redis`. On the Windows host (native workers, `dotnet ef`, ad-hoc `psql`) hostnames are `127.0.0.1`.

**Redis policy in this file:** `--save "" --appendonly no` + `allkeys-lru`. If a future lease implementation needs TTL keys to survive a Redis **process** crash, that is still not durability for orders — persist the lease intent in Postgres. Do not turn on AOF to “be safe.”

**Postgres:** single database `trader_intelligence`. EF migrations (A30 I1, `src/Infrastructure/Persistence/Migrations/`) are **not** auto-applied by this Compose. Add a one-shot `migrate` service only after `dotnet ef` has a real `DbContext` factory. Until then `docker compose up` gives an empty 16 cluster.

---

## 7. `.env.example` — placeholders only

Architecture §55: “Create only placeholders in `.env.example`.” A19 flagged §56 for publishing live hosts/logins. A65 follows §55, not the live §56 block.

This is the **intended** tracked file. Current `D:\Prop\.env.example` is **not** this text (it contains live targeting identifiers). This agent does not overwrite it.

```env
# Copy to .env and fill locally. Never commit .env.
# Placeholders only. No live broker IPs, logins, account IDs, or passwords.

# -----------------------------------------------------------------------------
# Compose data plane
# -----------------------------------------------------------------------------
POSTGRES_DB=trader_intelligence
POSTGRES_USER=ti
POSTGRES_PASSWORD=<SECRET>
POSTGRES_PORT=5432

REDIS_PASSWORD=<SECRET>
REDIS_PORT=6379

API_PORT=5000
WEB_PORT=3000
VITE_API_URL=http://localhost:5000

ASPNETCORE_ENVIRONMENT=Development

# Host-side / container-side connection strings.
# Inside Compose, api overrides these to Host=postgres / redis:6379.
# Native Windows workers MUST use 127.0.0.1 (published ports).
DATABASE_URL=Host=127.0.0.1;Port=5432;Database=trader_intelligence;Username=ti;Password=<SECRET>
REDIS_URL=127.0.0.1:6379,password=<SECRET>,abortConnect=false
ConnectionStrings__Postgres=Host=127.0.0.1;Port=5432;Database=trader_intelligence;Username=ti;Password=<SECRET>
ConnectionStrings__Redis=127.0.0.1:6379,password=<SECRET>,abortConnect=false

# -----------------------------------------------------------------------------
# Achiever MT5 (Windows-native worker only — not a Compose service)
# -----------------------------------------------------------------------------
MT5_SERVER=<MT5_HOST>
MT5_PORT=443
MT5_LOGIN=<MANAGER_LOGIN>
MT5_PASSWORD=<SECRET>
MT5_DEFAULT_GROUP=demo\default
MT5_MODE=local
MT5_POOL_SIZE=8
MT5_SERVER_NAME=<MT5_SERVER_NAME>
ACHIEVER_EGRESS_IP=<WHITELISTED_EGRESS_IP>
ACHIEVER_PROXY_ENABLED=false
ACHIEVER_PROXY_HOST=
ACHIEVER_PROXY_PORT=
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>

# -----------------------------------------------------------------------------
# StarwaveFX MT5 (Windows-native worker only)
# -----------------------------------------------------------------------------
MT5_STARWAVEFX_DISPLAY_NAME=StarwaveFX
MT5_STARWAVEFX_PROVISIONING_ENABLED=true
MT5_STARWAVEFX_MODE=local
MT5_STARWAVEFX_SERVER=<MT5_HOST>
MT5_STARWAVEFX_PORT=443
MT5_STARWAVEFX_LOGIN=<MANAGER_LOGIN>
MT5_STARWAVEFX_PASSWORD=<SECRET>
MT5_STARWAVEFX_SERVER_NAME=StarwaveFX
MT5_STARWAVEFX_POOL_SIZE=4
MT5_STARWAVEFX_PROXY_ENABLED=false

MT5_GROUP_2STEP_DEMO=
MT5_GROUP_1STEP_DEMO=
MT5_GROUP_2STEP_REAL=
MT5_GROUP_1STEP_REAL=
MT5_GROUP_INSTANT_REAL=
MT5_GROUP_CORE_DEMO=
MT5_GROUP_CORE_REAL=
MT5_GROUP_PASSFIRST_DEMO=
MT5_GROUP_PASSFIRST_REAL=

# Optional later: Linux worker → Windows sidecar (sidecar is not in Compose)
MT5_REMOTE_URL=http://127.0.0.1:9100
MT5_API_KEY=<SECRET>

MT5_PASSWORD_ENCRYPTION_KEY=<64_HEX_OR_BASE64_256BIT_KEY>
MT5_VOLUME_SCALE=10000

# -----------------------------------------------------------------------------
# cTrader FIX (fix-worker on host; not in this Compose file)
# -----------------------------------------------------------------------------
CTRADER_FIX_HOST=<FIX_HOST>
CTRADER_FIX_ACCOUNT_ID=<ACCOUNT_ID>
CTRADER_FIX_PASSWORD=<SECRET>
CTRADER_FIX_USE_SSL=true

CTRADER_FIX_QUOTE_SSL_PORT=5211
CTRADER_FIX_QUOTE_PLAIN_PORT=5201
CTRADER_FIX_QUOTE_SENDER_COMP_ID=<SENDER_COMP_ID>
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_QUOTE_SESSION_QUALIFIER=QUOTE
CTRADER_FIX_QUOTE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_QUOTE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>

CTRADER_FIX_TRADE_SSL_PORT=5212
CTRADER_FIX_TRADE_PLAIN_PORT=5202
CTRADER_FIX_TRADE_SENDER_COMP_ID=<SENDER_COMP_ID>
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_SESSION_QUALIFIER=TRADE
CTRADER_FIX_TRADE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_TRADE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>

CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=false
REAL_COPY_EXECUTION_ENABLED=false

# -----------------------------------------------------------------------------
# Risk / features (defaults are the safety story; A30 §16)
# -----------------------------------------------------------------------------
RISK_MAX_DAILY_LOSS_PCT=5.0
RISK_MAX_TOTAL_LOSS_PCT=10.0
RISK_MAX_POSITION_SIZE_LOTS=50.0
RISK_MAX_OPEN_POSITIONS=25
RISK_MAX_DAILY_TRADES=100
RISK_SLIPPAGE_TOLERANCE_POINTS=30
RISK_COPY_MIN_DELAY_MS=100
RISK_COPY_MAX_DELAY_MS=2000
RISK_EMERGENCY_FLATTEN_ENABLED=true
RISK_KILL_SWITCH_ENABLED=true

FEATURE_COPY_TRADING_ENABLED=false
FEATURE_CTRADER_HEDGING_ENABLED=false
FEATURE_ML_SCORING_ENABLED=false
FEATURE_NEWS_FILTER_ENABLED=false
FEATURE_TRADE_RECONSTRUCTION_ENABLED=true
SHADOW_COPY_ENABLED=true
STOP_NEW_SHADOW=false

LOG_LEVEL=info
LOG_FORMAT=text
```

**Never put in `.env.example`:** real passwords, proxy users, FIX passwords, JWT signing keys with entropy, live manager IPs, live account numbers, live SenderCompIDs.

**Never send to the `web` service:** any `MT5_*PASSWORD*`, `CTRADER_FIX_PASSWORD`, `POSTGRES_PASSWORD`, `REDIS_PASSWORD`, `MT5_API_KEY`, `MT5_PASSWORD_ENCRYPTION_KEY`, proxy credentials.

`.gitignore` already has:

```gitignore
.env
.env.*
!.env.example
```

That is correct. Keep it.

---

## 8. Windows Docker Desktop notes

| Topic | Requirement |
|---|---|
| Engine | **Linux containers.** Settings → General → Use the WSL 2 based engine. Do not switch to Windows containers for this file. |
| File share | Share `D:\Prop` (or the WSL path) so build context works. |
| Line endings | Keep `docker-compose.yml` / Dockerfiles LF. CRLF in `CMD-SHELL` scripts can break `pg_isready`. |
| Host ports | `5432`, `6379`, `5000`, `3000` must be free. A local Postgres/Redis Windows service will collide — stop it or change `POSTGRES_PORT` / `REDIS_PORT`. |
| `host.docker.internal` | Available on Docker Desktop. Only needed if a **container** must call a **host** process (future Windows MT5 HTTP sidecar on `:9100`). Default topology does not need it. |
| Native worker → Compose | Always `127.0.0.1`, never `postgres` / `redis` DNS. Those names exist only on network `ti`. |
| Firewall | Manager API is **outbound** from the Windows host to the broker (`MT5_PORT`, usually 443). Compose publish of 5432/6379 is inbound on localhost. Do not publish 5432/6379 past the lab NIC without a bind address. Tighten later with `127.0.0.1:${POSTGRES_PORT}:5432` if the lab is multi-user. |
| Resource | 2 CPU / 2 GB is enough for empty API + Postgres 16 + Redis 7. |

Suggested tighter publish (lab on a shared machine):

```yaml
ports:
  - "127.0.0.1:${POSTGRES_PORT}:5432"
  - "127.0.0.1:${REDIS_PORT}:6379"
```

Leave API/web on `0.0.0.0` only if the operator needs another device to open the dashboard. Default of “all interfaces” is acceptable on a single-user Windows lab.

---

## 9. Gaps the implementer must not paper over

1. **`GET /api/v1/health/live` does not exist.** Compose healthcheck on API is a temporary `/weatherforecast` probe. I6 replaces it. A26 is the contract: unauthenticated `{ "status": "ok" }`.
2. **`UseHttpsRedirection()`** in current `Program.cs` fights HTTP Compose. Gate it when API is implemented.
3. **No EF migrations.** `postgres_data` comes up empty. I1 owns `src/Infrastructure/Persistence/Migrations/`. Do not `CREATE TABLE` by hand in an init `.sql` that then races EF.
4. **Web path vs A26:** hooks use `/api/...`; A26 specifies `/api/v1/...`. Compose cannot reconcile that. I6 must pick one prefix.
5. **`VITE_API_URL` empty-string trap:** `client.ts` uses `import.meta.env.VITE_API_URL || 'http://localhost:5000'`. Empty is falsy. Same-origin nginx proxy will not work until that `||` is changed. This design therefore publishes API `:5000` and bakes that URL into the web image.
6. **CORS + SignalR** are unimplemented. Profile `apps` will show a UI that cannot load data until I6.
7. **Root `.env.example` is not placeholder-only today.** Replacing it is I0 work (A30). Use §7 text. Do not copy architecture §56 live IPs into the tracked example.
8. **Do not add `mt5-worker` to Compose** to “make the stack complete.” That would violate §5 and fail at `LoadLibrary`.
9. **Do not add Kafka/K8s** to this file to look production-ready (§71, A30 non-goals).
10. **Integration tests** (A10) still need Testcontainers **or** this Compose + a real connection string convention. Compose alone is not a test suite.

---

## 10. Alignment

| Source | How this design complies |
|---|---|
| Arch §5 Deployment | Linux: Postgres, Redis, API, React. Windows: MT5 Manager worker. |
| Arch §5 Redis ban | No Redis volume; LRU; no AOF. Orders/positions/balances stay in Postgres. |
| Arch §13 / A30 | No message broker service. |
| Arch §55 | `.env.example` placeholders only; web service gets no secrets. |
| Arch §71 | No K8s/Helm. One Compose file. |
| A26 | Target live probe `/api/v1/health/live`; documented as missing. |
| A30 I0 | `docker compose up -d` → Postgres 16 + Redis 7 only. |
| A30 I6 | `--profile apps` brings API + web when those Dockerfiles exist. |
| A14 / mt5-sdk README | Local Manager = Windows x64 DLLs. Called out, not containerized. |

---

## 11. Apply order (when a later increment is allowed to touch product)

1. Replace `D:\Prop\.env.example` with §7 (placeholders only).
2. Add `D:\Prop\docker-compose.yml` (§4) and `D:\Prop\.dockerignore` (§5.4).
3. `docker compose up -d` — prove `pg_isready` + Redis `PONG`. I0 exit (A30).
4. Do **not** add API/web Dockerfiles until I6 (API has a real health endpoint and HTTP-only mode). Then add §5.1–5.3 and `--profile apps`.
5. Keep `apps/mt5-worker` as a Windows host process forever for `MT5_MODE=local`.

**This agent created only** `D:\Prop\reports\swarm\20260818\A65_docker_compose.md`.
