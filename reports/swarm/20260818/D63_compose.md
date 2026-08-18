# D63 — `docker-compose.yml`: MT5 is not on Linux

| Field | Value |
|---|---|
| Agent | D63 (senior engineer, compose re-read only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:45+05:30 |
| Workspace | `D:\Prop` |
| Assigned question | Read `docker-compose.yml`. Is MT5 **not** in Linux? Write this report. |
| Product source modified | **No.** This report (plus swarm catalog/log) is the only write. |
| Binding law | Architecture §5 Deployment (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 310–318) |
| Relates | A14, A54, A65, A105, B37, C12 |
| SUT | `D:\Prop\docker-compose.yml` |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**YES — MT5 is not in Linux.** `D:\Prop\docker-compose.yml` does **not** declare, build, or run `apps/mt5-worker` (or any Manager-DLL process) as a Linux container.

The file Linux-izes only the §5-allowed slice: `postgres`, `redis`, and `api`. Native MetaQuotes Manager stays a Windows host process. Line 30 is an explicit stay-on-Windows comment.

| Question | Answer | Evidence |
|---|---|---|
| Does `docker-compose.yml` exist? | **Yes.** | `D:\Prop\docker-compose.yml` — 687 bytes, 30 LF, SHA-256 below |
| Is `mt5-worker` / `mt5-collector` / `mt5` a service? | **No.** | Service keys: `postgres`, `redis`, `api` only |
| Is any command `dotnet run --project apps/mt5-worker`? | **No.** | Only `apps/api/TraderIntelligence.Api.csproj` |
| Is there `platform: linux` on a worker? | **No.** | Zero `platform:` keys |
| Does the file tell operators to keep Manager off Linux? | **Yes.** | Line 30 comment (quoted in §2) |
| Does this satisfy architecture §5? | **Yes, for Compose.** | Native SDK omitted, not containerized |
| Is the stack proven running? | **No.** | Docker CLI **MISSING** on this host |

Honest one-liner: **Compose Linux-izes API + Postgres + Redis only. The MT5 worker is not a Compose service and is not forced onto Linux. That split is correct. Do not add the worker to this file later.**

Hash is unchanged vs B37 / C12 (`1ED8787F…`). This D63 pass re-measured; it does not invent a new file.

---

## 1. Method

1. Read `D:\Prop\docker-compose.yml` in full (30 content lines).
2. Re-measure size, SHA-256, LastWriteUtc, CR/LF, BOM (`Get-FileHash` + byte scan). Did **not** reuse C12’s hash without recomputing.
3. Inventory sibling compose names, override, Dockerfiles, `.dockerignore` (recurse `D:\Prop`, skip `bin` / `obj` / `node_modules` / `.git`).
4. Regex the compose body for `mt5-worker`, `mt5-collector`, `fix-worker`, `platform:`, `apps/mt5-worker`, `MT5API`, Wine.
5. Cross-check `apps/mt5-worker` (csproj, `Program.cs`, `Worker.cs`) so “not in Compose” is not confused with “does not exist.”
6. Cross-check architecture §5, A54 split, A65 proposed compose, A105 DLL law, `mt5-sdk/CMakeLists.txt` `if(NOT WIN32) return()`.
7. Measure PE machine of the three local-mode Manager DLLs (`MZ` + PE `0x8664` AMD64).
8. Probe `docker` / `docker-compose` on PATH. **Did not** start containers. **Did not** edit product source.

---

## 2. On-disk compose (read in full)

| Field | Value |
|---|---|
| Path | `D:\Prop\docker-compose.yml` |
| Bytes | **687** |
| Content lines | **30** (30 LF, 0 CR; PowerShell `-split "`n"` yields 31 because of the trailing newline) |
| SHA-256 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` |
| LastWriteUtc | `2026-08-18T07:48:40.1339443Z` |
| Line endings | **LF only** (no CR) |
| UTF-8 BOM | **No** |
| Compose `version:` key | **Absent** (current spec; good) |
| Project `name:` | **Absent** (Compose would default to directory name `prop`) |
| `platform:` keys | **None** |
| `profiles:` | **None** |
| `build:` / Dockerfiles | **None** |

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

The words `linux` and `Windows` appear **only** in that comment. They do not pin a worker to a Linux engine.

Sibling names (all **MISSING**): `docker-compose.yaml`, `compose.yml`, `compose.yaml`, `docker-compose.override.yml`. A54’s suggested `deploy/linux/docker-compose.yml` is also **MISSING**. Recurse found **one** compose/Dockerfile artifact: this root hyphen file. Zero `Dockerfile*`. Zero `.dockerignore`.

---

## 3. Service census

| Service | Image | Host ports | `platform:` | Restart | Healthcheck | Volume | Profile |
|---|---|---|---|---|---|---|---|
| `postgres` | `postgres:16` (tag, not digest) | `5432:5432` | **none** | default | **none** | **none** | none (always starts) |
| `redis` | `redis:7` (tag, not digest) | `6379:6379` | **none** | default | **none** | **none** | none (always starts) |
| `api` | `mcr.microsoft.com/dotnet/sdk:8.0` | `5000:5000` | **none** | default | **none** | bind `./:/src` | none (always starts) |

| Not listed | Status vs §5 / A54 / A65 | Note |
|---|---|---|
| `mt5-worker` / `mt5-collector` | **ABSENT (correct)** | Native Manager DLL is Windows PE32+ AMD64 |
| `fix-worker` | **ABSENT** | Allowed on Linux later; not required in this file |
| `web` | **MISSING** vs A65 | React is not Compose-hosted yet |
| `ml-service` | **ABSENT (correct)** | A104 stub; do not add |
| Kafka / K8s / Helm | **ABSENT (correct)** | architecture §13 / §71 |

Default `mcr.microsoft.com/dotnet/sdk:8.0` is a **Linux** image. `working_dir: /src` is a POSIX path. On Docker Desktop that means the **API** runs in a Linux container. Architecture §5 allows that. It is **not** the MT5 worker.

Regex of the compose body (this pass):

| Pattern | Hit |
|---|---|
| `mt5-worker` | **False** |
| `mt5-collector` | **False** |
| `fix-worker` | **False** |
| `apps/mt5-worker` | **False** |
| `MT5API` | **False** |
| `(?m)^\s*platform:` | **False** |
| Wine | **False** |

---

## 4. Assigned check: is MT5 in Linux?

### 4.1 What “MT5 in Linux” would look like (none of these exist)

Any of the following in compose would fail this review:

| Anti-pattern | Present? |
|---|---|
| Service key `mt5-worker` / `mt5-collector` / `mt5` | **No** |
| `command: dotnet run --project apps/mt5-worker/...` | **No** |
| `build:` of an `apps/mt5-worker/Dockerfile` | **No** (no Dockerfiles in the tree) |
| `platform: linux` (or `linux/amd64`) on a worker service | **No** `platform:` anywhere |
| `image:` that `COPY`s `MT5APIManager64.dll` into Debian/Alpine | **No** |
| Wine / Proton / `box64` sidecar for the Manager DLL | **No** |
| Compose env `MT5_MODE=local` on the Linux `api` service | **No** |

### 4.2 What the file actually does

1. **Omits** the worker. Compose cannot start a process that is not declared.
2. **Comments** the rule in-file so a later edit has to delete a warning to do the wrong thing:

```text
# Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers.
```

3. Linux-izes only `postgres`, `redis`, and `api` — the §5-allowed set.

### 4.3 Why putting MT5 on Linux would be UNSAFE (not folklore)

Architecture §5 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 310–318):

```text
Docker where compatible
Windows Worker if MT5 Manager DLL requires Windows
Linux for API/Postgres/Redis/Python/React if appropriate
```

```text
Do not force native MT5 SDK components into Linux containers if the SDK does not support it cleanly.
```

Measured SDK facts (this pass + A14 / A54 / A105):

| Fact | Path / measurement |
|---|---|
| Local-mode loader | `LoadLibraryW` / `CMTManagerAPIFactory` — Win32, not `dlopen` |
| Runtime DLLs (PE32+ AMD64) | `mt5-sdk/vendor/MetaTrader5SDK/Libs/MT5APIManager64.dll` — MZ + PE sig `00004550`, machine **`0x8664`** |
| Sibling PE | `MetaQuotes.MT5ManagerAPI64.dll` and `MetaQuotes.MT5CommonAPI64.dll` — also **`0x8664` AMD64** |
| CMake copy gate | `mt5sdk_copy_runtime_dlls` **returns immediately unless `WIN32`** (`D:\Prop\mt5-sdk\CMakeLists.txt` lines 120–123) |
| Local-mode sources | `mt5_manager.cpp` / `mt5_pool.cpp` / `mt5_watchdog.cpp` appended **only** `if(WIN32)` (same file lines 52–58) |
| README | “`mt5-sdk` is Windows-only for local Manager API. Keep the worker on Windows. Do not Linux-container the native DLL.” (`D:\Prop\README.md` lines 47–49) |
| Worker project RID | `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` is portable `net8.0` — **no** `RuntimeIdentifier=win-x64`. Compiles on any OS. **Load of Manager DLL is still Windows-only.** Portable TFM ≠ Linux-safe local mode. |

A Linux `mt5-worker` container could compile and even `dotnet run` today’s C# (it currently talks to `IMt5BrokerConnector` fakes via `AddTraderIntelligence`). That would **look** healthy and still violate §5 the moment `MT5_MODE=local` is wired. Compose correctly refuses to create that foot-gun as a service.

### 4.4 Where the worker actually lives

| Artifact | Status | SHA-256 / size (this pass) |
|---|---|---|
| `D:\Prop\apps\mt5-worker\` | **EXISTS** as a host `Microsoft.NET.Sdk.Worker` project | csproj 840 B / `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` |
| `Program.cs` | Host + `AddTraderIntelligence` + `EnsureCreatedAsync` + `DemoSeeder`. No native DLL copy. | 859 B / `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` |
| `Worker.cs` | 30 s Fake ingest + score of 4 demo logins. **Not** Manager `LoadLibrary`. | 1882 B / `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` |
| Compose membership | **None** | — |
| Intended OS (A54 / A105) | **Windows Server x64** (VM or bare metal) | — |

So: the process exists on the Windows tree; Compose does not claim it; Linux is not applied to it.

**PASS** on the assigned confirmation.

---

## 5. Compose quality (honest; out of the assigned yes/no)

The Linux-split is right. The file is still a **lab draft**, not A65.

| Check | Class | Note |
|---|---|---|
| MT5 / native SDK not in Compose | **EXISTS_AND_GOOD** | This D63 question |
| Data-plane images `postgres:16` + `redis:7` | **EXISTS** in YAML | Not proven pulled/running (no Docker CLI) |
| `api` as always-on SDK `dotnet run` | **EXISTS_NEEDS_REFACTOR** | A65 wanted `profiles: [apps]` + multi-stage `aspnet:8.0` |
| Default profile = Postgres + Redis only (A30 I0) | **FAIL vs A65** | `api` always starts |
| Named volume `postgres_data` | **MISSING** | Recreate wipes lab DB |
| Healthchecks / `depends_on: condition: service_healthy` | **MISSING** | Start-order only |
| `${POSTGRES_*}` / Redis password from gitignored `.env` | **MISSING** | Literal `ti_dev_only` committed |
| `ConnectionStrings__TraderIntelligence` / `DATABASE_URL` on `api` | **MISSING** | Binder falls back to InMemory (`src/Infrastructure/DependencyInjection.cs` 19–25) if the string is empty or contains `<SECRET>` |
| `REAL_COPY_EXECUTION_ENABLED=false` pinned in compose | **MISSING** | Must stay false; compose does not pin it |
| `web` + nginx | **MISSING** | A65 `--profile apps` |
| Dockerfiles / `.dockerignore` | **MISSING** | Recurse found **zero** `Dockerfile*` |
| Docker Engine / CLI on this host | **MISSING** | `docker` and `docker-compose` not on PATH |

`api` bind-mounts the **entire repo** at `/src`. That **makes PE Manager DLLs visible** in the Linux filesystem view (`mt5-sdk/vendor/.../Libs/*.dll`). They are **not executed** by the `api` command. Harmless today. Do **not** treat that mount as permission to `LoadLibrary` those files inside the container. Do **not** add a second command in the same service that runs the worker.

Host `launchSettings.json` already binds `http://localhost:5000`. Compose publishes `5000:5000`. Host `dotnet run` of the API and Compose `api` **cannot** share that port.

---

## 6. Drift vs A65 proposed compose

A65 (`A65_docker_compose.md`) is **design**. This file is a smaller draft. B37 / C12 tabulated the gap; re-checked, still true:

| A65 requirement | On-disk file |
|---|---|
| `name: ti` | **MISSING** |
| Comment: never `mt5-worker` / `mt5-collector` / `fix-worker` / ML / Kafka | **Partial** — one-line MT5 comment; no FIX/ML/Kafka mention |
| Default profile = postgres + redis only | **FAIL** — `api` always starts |
| `--profile apps` for api + web | **MISSING** |
| Env interpolation from `.env` | **FAIL** — literals |
| `postgres_data` volume + healthchecks | **MISSING** |
| Redis `--requirepass` | **MISSING** |
| `api` built from `apps/api/Dockerfile` | **FAIL** — stock SDK + `dotnet run` |
| `web` | **MISSING** |
| Explicit `networks: [ti]` | **MISSING** |

D63 does **not** implement A65. Product source stays untouched.

---

## 7. Residual risks (do not regress the PASS)

| ID | Risk | If it happens | Class |
|---|---|---|---|
| D63-R1 | A later edit adds `mt5-worker:` with `image: mcr.microsoft.com/dotnet/sdk:8.0` and `working_dir: /src` (copy-paste of `api`) | Forces the worker onto Linux. Violates §5. | **UNSAFE** if added |
| D63-R2 | `COPY` of `MT5APIManager64.dll` into a Linux image “to have it nearby” | ELF host cannot load PE. Fake “connected.” | **UNSAFE** |
| D63-R3 | Wine / Windows-container-for-DLL | Unsupported, non-deterministic, forbidden by A54 | **UNSAFE** |
| D63-R4 | `dotnet publish -r linux-x64` of `apps/mt5-worker` called “done” | Portable host still cannot `LoadLibrary` the Manager DLL | **UNSAFE** as a local-mode ship |
| D63-R5 | Operator assumes bind-mount `./:/src` means Manager DLLs are usable in `api` | They are visible files only | Do not load |
| D63-R6 | `MT5_MODE=remote` Linux worker without a Windows sidecar | No in-repo HTTP Manager server (A16). Remote client with nothing to call. | **MISSING** sidecar; not an excuse to Linux-local |

Guard: keep line 30. Keep workers off `services:`. Run `apps/mt5-worker` on the Windows host against published `localhost:5432` / `localhost:6379`. Do not set Compose DNS names (`postgres`, `redis`) on the native worker unless it is on the same user-defined network — it is not, and must not be.

---

## 8. Host Docker status (measured this pass)

| Command | Result |
|---|---|
| `Get-Command docker` | **MISSING** — term not recognized |
| `Get-Command docker-compose` | **MISSING** — term not recognized |

YAML was **not** validated by `docker compose config`. Images were **not** pulled. Ports **5432 / 6379 / 5000** were **not** proven bound. Do **not** claim the stack is up.

---

## 9. Assigned answer (one line)

**CONFIRMED: MT5 is not in Linux.** `D:\Prop\docker-compose.yml` (SHA-256 `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1`) lists `postgres`, `redis`, and Linux `api` only — **no** `mt5-worker` service, **no** `platform: linux` on a worker, explicit Windows-host comment at line 30. Native Manager stays off Linux Compose.

---

## 10. Out of scope (not done)

- No product-source edit (`docker-compose.yml` left unchanged).
- No `docker compose up`.
- No Dockerfile / `.dockerignore` / override created.
- No A65 implementation.
