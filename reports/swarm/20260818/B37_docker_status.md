# B37 — Docker Compose status (`D:\Prop`)

| Field | Value |
|---|---|
| Agent | B37 (senior engineer, compose status only) |
| Date | 2026-08-18 |
| Measured at (UTC) | 2026-08-18T07:48:40Z file mtime; inventory after that |
| Workspace | `D:\Prop` |
| Assigned question | If `docker-compose` exists at `D:\Prop`, read it; else say **MISSING**. Write this report. |
| Product source modified | **No.** This report is the only write. |
| Relates | A30 I0, A54 §8, A65 (design; **stale MISSING claim**), A75, A77, A103, A105, B06 |

---

## 0. Verdict

**`docker-compose.yml` EXISTS.** It is **not** MISSING.

| Artifact | Path | Status |
|---|---|---|
| Compose file (hyphen name) | `D:\Prop\docker-compose.yml` | **EXISTS** — 687 bytes, 30 lines, LF, no BOM |
| Alternate names | `docker-compose.yaml`, `compose.yml`, `compose.yaml` | **MISSING** |
| Override | `D:\Prop\docker-compose.override.yml` | **MISSING** |
| Dockerfiles | any `Dockerfile` / `apps/*/Dockerfile` under `D:\Prop` (excluding bin/obj/node_modules/.git) | **MISSING** (0 files) |
| `.dockerignore` | `D:\Prop\.dockerignore` | **MISSING** |
| `.env` | `D:\Prop\.env` | **MISSING** |
| `.env.example` | `D:\Prop\.env.example` | **EXISTS** (not consumed by current compose) |
| Docker Engine / `docker` CLI / `docker compose` / `docker-compose` on this host | PATH | **MISSING** — `docker` and `docker-compose` are not recognized |

A65 §1 (`D:\Prop\docker-compose.yml` = MISSING) is **stale**. This file supersedes that existence claim. A65’s **proposed** topology is still the design target; the on-disk file is a smaller, different draft (see §4).

Classification:

| Component | Class |
|---|---|
| Root `docker-compose.yml` | **EXISTS_NEEDS_REFACTOR** |
| Data-plane images (`postgres:16`, `redis:7`) | **EXISTS** in YAML only — **not proven running** (no Docker CLI) |
| Compose `api` service | **EXISTS_NEEDS_REFACTOR** — SDK + `dotnet run` + no DB/Redis env |
| Compose `web` / profiles / healthchecks / named volume | **MISSING** vs A65 |
| `mt5-worker` / `fix-worker` in Compose | **ABSENT (correct)** — file comment matches A54 / A105 |
| Runnable stack on this machine | **UNVERIFIED / blocked** — Docker not installed |

---

## 1. Method

1. Recurse `D:\Prop` for `docker-compose*`, `compose.y*`, `Dockerfile*`, `.dockerignore` (skip `bin` / `obj` / `node_modules` / `.git`).
2. Read `D:\Prop\docker-compose.yml` in full (30 lines).
3. Hash, size, line-ending, BOM check.
4. Inventory siblings: `.env`, `.env.example`, `.gitignore`, override file.
5. Cross-check API wiring (`apps/api/appsettings.json`, `Program.cs`, `src/Infrastructure/DependencyInjection.cs`) so “compose would talk to Postgres” is evidence, not hope.
6. Probe `docker`, `docker compose`, `docker-compose` on PATH. **Did not** start containers. **Did not** edit product source.

---

## 2. On-disk file (read in full)

| Field | Value |
|---|---|
| Path | `D:\Prop\docker-compose.yml` |
| Bytes | **687** |
| Lines | **30** (file ends after the MT5 comment; grep of the tree showed the same 30-line body) |
| SHA-256 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` |
| LastWriteUtc | `2026-08-18T07:48:40.1339443Z` |
| Line endings | **LF only** (no CR). Good for `CMD-SHELL` if healthchecks are added later (A65 § line-ending note). |
| UTF-8 BOM | **No** |
| Compose `version:` key | **Absent** (current spec; good) |
| Project `name:` | **Absent** (Compose will default to directory name `prop`) |

Exact contents as of the hash above:

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: ti
      POSTGRES_PASSWORD: ti_dev_only
      POSTGRES_DB: trader_intelligence
    ports:
      - "5432:5432"

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  api:
    image: mcr.microsoft.com/dotnet/sdk:8.0
    working_dir: /src
    volumes:
      - ./:/src
    command: dotnet run --project apps/api/TraderIntelligence.Api.csproj --urls http://0.0.0.0:5000
    ports:
      - "5000:5000"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    depends_on:
      - postgres
      - redis

# Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers.
```

No other compose file exists at `D:\Prop` or in a 3-level recurse of the product tree.

---

## 3. Service census (what the YAML actually declares)

| Service | Image | Host ports | Restart | Healthcheck | Volume | Networks | Profile |
|---|---|---|---|---|---|---|---|
| `postgres` | `postgres:16` (tag, not digest) | `5432:5432` | default | **none** | **none** (data dies with container) | default | none (always starts) |
| `redis` | `redis:7` (tag, not digest) | `6379:6379` | default | **none** | **none** (A65 wanted this) | default | none (always starts) |
| `api` | `mcr.microsoft.com/dotnet/sdk:8.0` | `5000:5000` | default | **none** | bind `./:/src` | default | none (always starts) |

| Not listed | Status | Note |
|---|---|---|
| `web` | **MISSING** | A65 `apps` profile |
| `mt5-worker` / `mt5-collector` | **ABSENT (correct)** | Windows Manager DLLs (A54, A105) |
| `fix-worker` | **ABSENT** | Allowed later on Linux; not in this file |
| `ml-service` | **ABSENT** | A104 still stub-only |
| Kafka / K8s / Helm | **ABSENT (correct)** | architecture §13 / §71 |

### 3.1 `postgres`

- User `ti`, database `trader_intelligence`, password **hardcoded** `ti_dev_only`.
- Password is labeled as lab-only in the value itself. It is still a committed secret-shaped string. A65 / A75 wanted `${POSTGRES_PASSWORD}` from gitignored `.env`.
- No `TZ` / `PGTZ`.
- No named volume → `docker compose down` (or recreate) **wipes** the lab DB.
- No `pg_isready` healthcheck → `depends_on` is start-order only, not ready-order.

### 3.2 `redis`

- Stock `redis:7`. **No `--requirepass`.** Open on `localhost:6379` if the engine is up.
- No maxmemory policy. No AOF/RDB disable. Matches “Redis is not SoT” only by having no volume.
- API **does not** register StackExchange.Redis in `DependencyInjection.cs`. Compose `depends_on: redis` is unused by current product code.

### 3.3 `api`

- Dev image (`sdk:8.0`), not `aspnet:8.0` runtime. Not a published artifact.
- Command: `dotnet run --project apps/api/TraderIntelligence.Api.csproj --urls http://0.0.0.0:5000`.
- Bind-mounts the **entire repo** at `/src`. `.dockerignore` is **MISSING**, so a later `build:` would send vendor SDK DLLs, `bin/`, `reports/`, etc. into the context.
- Env set: `ASPNETCORE_ENVIRONMENT=Development` only.
- **Not set:** `ConnectionStrings__TraderIntelligence`, `DATABASE_URL`, `ConnectionStrings__Redis`, `REDIS_URL`, `REAL_COPY_EXECUTION_ENABLED`, `ASPNETCORE_URLS`.
- Product binder (`DependencyInjection.cs` lines 19–29): empty `ConnectionStrings:TraderIntelligence` (`appsettings.json` is `""`) and no `DATABASE_URL` → **`UseInMemoryDatabase("trader-intelligence")`**. A containerized API next to Compose Postgres would **not** use that Postgres unless an operator injects the connection string.
- `Program.cs` maps `GET /health` and `GET /ready` (B06). Compose has no healthcheck against either. A65’s proposed `/weatherforecast` probe is also absent (route is gone).
- `depends_on: [postgres, redis]` without `condition: service_healthy`.

`launchSettings.json` host profile already uses `http://localhost:5000`. Compose publishes the same port. Host `dotnet run` and Compose `api` **cannot** share `:5000`.

---

## 4. Drift vs A65 proposed compose (design still not implemented)

| A65 §4 requirement | On-disk `docker-compose.yml` |
|---|---|
| `name: ti` | **MISSING** |
| Default profile = postgres + redis only | **FAIL** — `api` always starts |
| `--profile apps` for api + web | **MISSING** |
| `${POSTGRES_*}` / `${REDIS_PASSWORD}` from `.env` | **FAIL** — literals, including `ti_dev_only` |
| `postgres_data` named volume | **MISSING** |
| `pg_isready` + redis `PING` healthchecks | **MISSING** |
| Redis `--requirepass` | **MISSING** |
| `api` built from `apps/api/Dockerfile` → `aspnet:8.0` | **FAIL** — stock SDK + `dotnet run` |
| `ASPNETCORE_URLS=http://+:8080`, publish `5000:8080` | **FAIL** — process listens **5000** in-container |
| `ConnectionStrings__Postgres` / `DATABASE_URL` to hostname `postgres` | **MISSING** (and product key is `TraderIntelligence`, not `Postgres`) |
| `REAL_COPY_EXECUTION_ENABLED=false` in compose env | **MISSING** (appsettings has `false`; compose does not pin it) |
| `web` + nginx | **MISSING** |
| Explicit `networks: [ti]` | **MISSING** |
| Comment: never MT5/FIX/ML/Kafka | **Partial** — one-line MT5 comment only; no FIX/ML mention |

A54 suggested path `deploy/linux/docker-compose.yml`. That path is **MISSING**. The live file is at repo root.

---

## 5. Sibling inventory (compose-adjacent, not edited)

| Path | Status | Relevance |
|---|---|---|
| `D:\Prop\.env.example` | EXISTS, 3408 bytes, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | `DATABASE_URL=Host=localhost;Port=5432;Database=trader_intelligence;Username=ti;Password=<SECRET>` and `REDIS_URL=localhost:6379`. Compose does not interpolate these. Password in compose (`ti_dev_only`) ≠ example placeholder. |
| `D:\Prop\.env` | **MISSING** | `docker compose` would not load a local overlay even if Docker existed. |
| `D:\Prop\.gitignore` | EXISTS; ignores `.env` / `.env.*`, un-ignores `.env.example` | Does **not** ignore `docker-compose.override.yml` (A103-03 still open). |
| `D:\Prop\README.md` | Runbook is host `dotnet run` + `npm run dev` | Mentions “Without Postgres the API uses EF InMemory”. **No** `docker compose` instructions. |
| `apps/api/Dockerfile` | **MISSING** | |
| `apps/web/Dockerfile` | **MISSING** | |
| `apps/web/nginx.conf` | **MISSING** | |

---

## 6. Host Docker status (measured)

Commands run in PowerShell on this Windows host:

| Command | Result |
|---|---|
| `docker version` | **FAIL** — `The term 'docker' is not recognized` |
| `docker compose version` | **FAIL** — same (`docker` missing) |
| `docker-compose version` | **FAIL** — `The term 'docker-compose' is not recognized` |
| `docker info` | **FAIL** — same |

Therefore:

- YAML **syntax / interpolate / `config`** was **not** validated by the Compose engine.
- Images were **not** pulled. Ports **5432 / 6379 / 5000** were **not** proven bound.
- Do **not** claim the stack is up, healthy, or “ready for I0.”

Blocker to exercise the file: install Docker Desktop (Linux engine, not Windows containers) **or** another Compose-capable engine, then `docker compose -f D:\Prop\docker-compose.yml config`.

---

## 7. Would this file work if Docker existed? (static only)

Honest static review. Not a substitute for `compose up`.

| Check | Likely result | Evidence |
|---|---|---|
| `postgres` + `redis` start | **Probably yes** on Linux engine | Official images; trivial service defs |
| Persist lab data across recreate | **No** | no volume |
| `api` Linux `dotnet run` against repo mount | **Probably compiles** (portable net8.0) | `TraderIntelligence.Api` is not win-x64-only |
| `api` uses Compose Postgres | **No** with current YAML + current binder | empty `TraderIntelligence` connection string → InMemory |
| `api` uses Compose Redis | **No** | no Redis DI |
| `depends_on` waits for Postgres accept | **No** | no healthcheck; InMemory hides this |
| Native MT5 in this file | **Correctly omitted** | comment line 30 |
| Bind-mount of `mt5-sdk/vendor/.../Libs` into Linux `api` | Harmless today (unused) | volume is `./:/src`; A105: do not `COPY` those DLLs into a Linux **image** |
| Compose + host API both on `:5000` | Collision if both run | `launchSettings.json` `applicationUrl` is `:5000` |

---

## 8. Secrets / safety (compose slice)

| Item | Measured |
|---|---|
| Live MT5 / FIX passwords in compose | **None** |
| Lab Postgres password in compose | **`ti_dev_only` committed in plaintext** |
| Redis auth | **None** — if published, unauthenticated on host |
| `REAL_COPY_EXECUTION_ENABLED` pinned in compose | **No** |
| React / web secrets | N/A (no `web` service) |

Do not treat `ti_dev_only` as a production secret. Do not copy it into a non-lab environment. Do not add Manager / FIX / proxy credentials to this file.

---

## 9. Assigned answer (one line)

**EXISTS:** `D:\Prop\docker-compose.yml` (SHA-256 `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1`) — three services (`postgres:16`, `redis:7`, SDK `dotnet run` API); no Docker CLI on this host to run it; A65 design still unimplemented.

---

## 10. Out of scope (not done)

- No product-source edit.
- No `docker compose up`.
- No Dockerfile / `.dockerignore` / override created.
- No `SWARM_LOG.md` / `INDEX.md` rewrite in this pass (this file is the assigned artifact).
