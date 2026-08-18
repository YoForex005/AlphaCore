# A54 — Deployment split: Windows `mt5-worker` + Linux API / Postgres / Redis / React

| Field | Value |
|---|---|
| Agent | A54 (senior engineer, design-only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Binding spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§5 Tech Stack** (Deployment) and **§7 Achiever Configuration** |
| Supporting spec | Same file §§4, 6, 8, 10–13, 55–58, 62, 66–67, 71–73 |
| Adjacent swarm notes (read, not rewritten) | `A07_mt5_worker_audit.md`, `A14_mt5_manager_local.md`, `A15_mt5_pool_watchdog.md`, `A16_mt5_http_client.md`, `A03_infrastructure_audit.md`, `A06_api_audit.md`, `A11_solution_coverage.md`, `A12_imt5_client_map.md`, `A19_security_secrets_scan.md`, `A28_phases_gates.md` |
| Product source modified | **No.** This file is the only write. |

Classification vocabulary is architecture §73.B:

```text
EXISTS_AND_GOOD
EXISTS_NEEDS_REFACTOR
MISSING
DEPRECATED
UNSAFE
```

---

## 0. Mandate (do not reinterpret)

Architecture **§5 Deployment** is the OS law for this product:

```text
Docker where compatible
Windows Worker if MT5 Manager DLL requires Windows
Linux for API/Postgres/Redis/Python/React if appropriate
```

And the sentence that this report exists to enforce:

```text
Do not force native MT5 SDK components into Linux containers
if the SDK does not support it cleanly.
```

Architecture **§7 Achiever** is the first live source that forces that law to be real:

```env
MT5_SERVER=57.128.141.65
MT5_PORT=443
MT5_LOGIN=2027
MT5_DEFAULT_GROUP=demo\Maxmaster
MT5_MODE=local
MT5_POOL_SIZE=8
MT5_SERVER_NAME=AchieverGlobalMarkets-Server
```

`MT5_MODE=local` means **in-process MetaQuotes Manager API** (`LoadLibrary` of `MT5APIManager64.dll`), not loopback HTTP, not Wine, not a Linux container with the DLL copied in.

Required Achiever egress:

```text
81.29.145.69
```

That IP is a **Windows-worker / proxy** constraint. It is not a reason to put Postgres or React on Windows.

**This report’s job:** pin the split, prove the DLL is Windows-only from artifacts in this tree, and design the Phase 1+ topology so nobody “simplifies” the stack by forcing the SDK onto Linux.

---

## 1. Verdict

| Question | Answer |
|---|---|
| Is the Manager API Windows-only? | **Yes. Measured.** PE `AMD64` (`0x8664`) DLLs + `LoadLibraryW` + `#include <Windows.h>` + CMake `if(WIN32)` gate. Not a preference. |
| Where does `apps/mt5-worker` run? | **Windows Server x64** (VM or bare metal). Publish `win-x64`. Copy the three Manager runtime DLLs beside the exe. |
| Where do API / Postgres / Redis / React run? | **Linux.** Docker-compose is appropriate here. |
| Where does `apps/fix-worker` run? | **Linux** (preferred). QuickFIX/n is managed code. It does not load the Manager DLL. |
| Where does Python/XGBoost run later? | **Linux.** Never next to `MT5APIManager64.dll`. |
| May we put `MT5APIManager64.dll` in a Linux image “just to try”? | **No. `UNSAFE`.** |
| May we Wine / Proton / `box64` the Manager DLL? | **No. `UNSAFE`.** Unsupported, non-deterministic, violates §5. |
| May we make Linux `mt5-worker` use `MT5_MODE=remote` so the C# process is “cross-platform”? | **Not as Phase 1 default.** That still requires a **Windows** sidecar that loads the DLL. It does not remove Windows; it adds a network hop and loses local-only APIs (`GetGroupDetails`, `GetOrders`, ticks). §7 says `local`. |
| Is the split implemented today? | **No.** There is no compose file, no RID, no ConnectionStrings, no DLL copy in the C# worker, no broker sessions. The **spec and the C++ gate exist**. The **deployment does not**. |

Honest one-liner: **Windows owns Manager TCP + native DLLs. Linux owns the product data plane and UI. PostgreSQL is the only contract that must cross the OS boundary on day one.**

---

## 2. Why the Manager DLL is Windows-only (measured, not folklore)

### 2.1 The files that must sit beside a local-mode process

CMake copies these three next to any local-mode consumer (`D:\Prop\mt5-sdk\CMakeLists.txt` lines 114–118):

| File | Size (bytes) | PE machine | SHA-256 |
|---|---:|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MT5APIManager64.dll` | 7,185,272 | `0x8664` (AMD64) | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll` | 396,872 | `0x8664` | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll` | 1,046,632 | `0x8664` | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

All three start `MZ` and have a PE optional-header machine of **`IMAGE_FILE_MACHINE_AMD64`**. They are native Windows x64 images. They are not ELF, not .NET `AnyCPU`, not a Linux `.so`.

Sibling files in the same `Libs/` folder (`MT5APIManager64avx.dll`, `MT5APIManager64avx2.dll`, `MT5APIManager64arm.dll`) are the factory’s CPU-variant search path (`FindLibrary` in `MT5APIManager.h`). Same OS family.

`mt5sdk_copy_runtime_dlls` is a **no-op unless `WIN32`** (`CMakeLists.txt` 120–123).

### 2.2 The loader is Win32, not POSIX

`CMTManagerAPIFactory::Initialize` (`vendor/MetaTrader5SDK/Include/MT5APIManager.h` 1719–1744):

```text
FindLibrary(...)
LoadLibraryW(path)
GetProcAddress(..., MTManagerVersion / MTManagerCreateExt / MTAdminCreateExt)
```

Shutdown is `FreeLibrary`. There is no `dlopen` path.

`MT5Manager` cannot even compile off Windows:

```3:4:D:\Prop\mt5-sdk\src\core\mt5_manager.h
#include <Windows.h>
#include "MT5APIManager.h"
```

### 2.3 The first-party build already refuses Linux local mode

`D:\Prop\mt5-sdk\CMakeLists.txt` 49–57:

```text
# MT5Manager, MT5Pool and MT5Watchdog bind the native MetaQuotes Manager API,
# which ships as Windows DLLs only. On other platforms the HTTP client remains
# available and the local-mode transport is simply absent.
if(WIN32)
    list(APPEND MT5SDK_SOURCES
        src/core/mt5_manager.cpp
        src/core/mt5_pool.cpp
        src/core/mt5_watchdog.cpp
    )
endif()
```

Operator probes (`mt5_group_probe`, `mt5_news_calendar_probe`) are `if(MT5SDK_BUILD_PROBES AND WIN32)` only.

`README.md`:

> **local** — native MetaQuotes Manager API. Lowest latency, Windows-only, consumes a manager connection slot on the broker.

> C++20 compiler. **MSVC 2022 for local mode (the Manager API is Windows x64 only).**

`mt5_types.h` 119–123 documents the same cut for translation units that must not include SDK headers:

> Kept as plain constants so that non-SDK translation units … can reference them without pulling in the **Windows-only SDK headers**.

### 2.4 What “local” actually is (so we do not fake it on Linux)

From A14 / `mt5_manager.cpp`:

```text
caller
  → MT5Manager
    → IMTManagerAPI*
      → MT5APIManager64*.dll   (LoadLibrary)
        → TCP to <server>:<port>   (optional ProxySet first)
```

There is no named pipe, no localhost sidecar implied by `MT5_MODE=local`. Local = **in-process native DLL + direct manager TCP**.

Connect timeout is 30 s. Default pump is `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS`. There is **no `PUMP_MODE_DEALS`**. IP block is `MT_RET_AUTH_MANAGER_IPBLOCK` (1012) — exactly the Achiever whitelist failure mode.

### 2.5 What is *not* Windows-only

These stay portable and belong on Linux:

| Piece | Why it is not the Manager DLL |
|---|---|
| `IMT5Client` / `mt5_types.h` | Header-only contract; no `Windows.h` |
| `MT5HttpClient` | libcurl + JSON; CMake builds it on every OS |
| `apps/api` | `net8.0` ASP.NET; **does not** reference `src/Mt5` today — keep that |
| `src/Infrastructure` | EF Core + Npgsql + StackExchange.Redis |
| `apps/web` | Vite/React static assets |
| `apps/fix-worker` + `src/Fix.CTrader` | managed FIX; no Manager |
| PostgreSQL / Redis | Linux first-class |

Portable **source** is not a license to load the **DLL** on Linux. The HTTP client exists so *other processes* can talk to a Windows owner of the DLL. It is not a Linux Manager.

---

## 3. Binding quotes from §5 and §7 (what we are implementing)

### 3.1 §5 stack that must survive the split

| Layer | Tech | OS implication |
|---|---|---|
| Backend | C# / .NET 8 / ASP.NET Core / Worker Services / EF Core / Npgsql / Serilog / OTel | API + FIX worker: Linux. MT5 worker: Windows. Shared libraries: `net8.0` portable. |
| FIX | QuickFIX/n 4.4 | Linux worker. |
| Database | PostgreSQL = durable SoT | Linux. |
| Cache | Redis — live scores, short cache, FIX session lock. **Not** orders/positions/balances | Linux. |
| ML | Python / FastAPI / XGBoost (later) | Linux. |
| UI | React / TS / Vite / TanStack Query / SignalR | Linux (nginx). |
| Deploy | Docker **where compatible**; Windows worker **if** Manager DLL requires Windows | This report. |

§5 also: do not fashion-upgrade the runtime if native DLL behavior depends on a known .NET/MSVC pairing. Pin **.NET 8** + **win-x64** for the worker; do not retarget the worker to native AOT or Linux RID “for smaller images.”

### 3.2 §7 Achiever facts the Windows host must satisfy

| Knob | Value | Lives on |
|---|---|---|
| Server | `57.128.141.65:443` | Windows worker config |
| Manager login | `2027` | Windows worker config |
| Password | secret | Windows worker only (User Secrets / DPAPI / env). **Never** in `apps/api`, **never** in React, **never** in the Linux compose file as a convenience copy |
| Default group | `demo\Maxmaster` | Label only. Worker must enumerate **all** Manager-visible groups |
| Mode | `local` | Windows worker |
| Pool | `8` | Windows worker. Plus **one** pump manager ≈ **9** Achiever manager slots (A15) |
| Server name | `AchieverGlobalMarkets-Server` | Windows worker / `brokers` row |
| Egress IP | `81.29.145.69` | Windows NIC, NAT, or SOCKS/HTTP proxy (`ACHIEVER_PROXY_*` in §56) |
| Startup | Connect → enumerate groups → upsert → accounts → history | Windows worker process |

§8 StarwaveFX is the second **local** connector on the **same Windows process**: `84.201.6.142:443`, login `9904`, pool `4`, no whitelist today. Do not stand up a second OS just for the second broker.

§9: plan maps (`MT5_GROUP_*`) are optional labels **after** discovery. They do not decide which groups are fetched. They are not a Linux concern.

---

## 4. Target topology (Phase 1 default)

```text
                         public HTTPS
                              │
                              ▼
                    ┌─────────────────────┐
                    │  Linux nginx        │
                    │  apps/web (static)  │
                    │  /api → api:8080    │
                    │  /hubs → SignalR    │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │  Linux api          │
                    │  ASP.NET Core       │
                    │  linux-x64          │
                    │  NO MT5 DLL         │
                    │  reads Postgres     │
                    │  cache Redis        │
                    └──────────┬──────────┘
             ┌─────────────────┼─────────────────┐
             ▼                 ▼                 ▼
      PostgreSQL            Redis          fix-worker
      (Linux)               (Linux)        (Linux, later)
      durable SoT           cache/locks    QuickFIX/n
             ▲
             │ Npgsql TLS, private net
             │ writes: raw MT5 + checkpoints + outbox
             │
┌────────────┴──────────────────────────────┐
│  Windows Server x64                       │
│  apps/mt5-worker  (win-x64)               │
│                                           │
│  MT5APIManager64.dll                      │
│  MetaQuotes.MT5ManagerAPI64.dll           │
│  MetaQuotes.MT5CommonAPI64.dll            │
│                                           │
│  MT5_MODE=local                           │
│  broker Achiever   pool 8 + 1 pump        │
│  broker StarwaveFX pool 4 + 1 pump        │
│                                           │
│  egress 81.29.145.69 (or proxy to it)     │
│  TCP 443 → 57.128.141.65                  │
│  TCP 443 → 84.201.6.142                   │
│                                           │
│  optional :9101 private /health/mt5       │
│  NO public inbound                        │
└───────────────────────────────────────────┘
```

Two machines (or a Windows VM + a Linux VM/compose) is the **minimum honest production shape**. A single Linux box cannot satisfy §7 `local`. A single Windows box *can* host everything in a lab, but that is **not** the architecture’s intended split and must not become the production story.

### 4.1 Process / OS placement

| Process | OS | RID / image | Native MT5? | Role |
|---|---|---|---|---|
| `apps/mt5-worker` | **Windows Server 2022 x64** (or Win11 lab) | `win-x64` | **Yes** | Collectors for Achiever + StarwaveFX. Writes Postgres. |
| C++ probes (`mt5_group_probe`) | Windows | MSVC x64 | Yes | Operator diagnostics only. Not a product service. |
| Optional HTTP sidecar | Windows only | win-x64 | Yes | Only if we later choose `MT5_MODE=remote`. Not Phase 1. |
| `apps/api` | Linux | `linux-x64` | **No** | Dashboard BFF, auth, SignalR, health aggregation. |
| `apps/web` | Linux (nginx) | static | **No** | React. `VITE_API_URL` → Linux API. |
| PostgreSQL 16 | Linux | official image | **No** | SoT. |
| Redis 7 | Linux | official image | **No** | Cache + FIX lock. Never SoT for deals. |
| `apps/fix-worker` | Linux | `linux-x64` | **No** | QUOTE + TRADE. Phase 4+. |
| `services/ml-service` | Linux | python | **No** | Phase 6. |

### 4.2 Shared .NET assemblies (portable) vs Windows-only binding

| Assembly | Portable compile? | May Linux **load** it? | Rule |
|---|---|---|---|
| `TraderIntelligence.Domain` | yes | yes | No IO, no native. |
| `TraderIntelligence.Application` | yes | yes | Ports only. `IMt5BrokerConnector` **interface** lives here (or Domain). **No** P/Invoke. |
| `TraderIntelligence.Infrastructure` | yes | yes | EF + Redis. Used by **both** Windows worker and Linux API. |
| `TraderIntelligence.Mt5` | compile as `net8.0` | **runtime-load native only on Windows** | ProjectReference **only** from `apps/mt5-worker`. **Never** from `apps/api`. |
| `TraderIntelligence.Fix.CTrader` | yes | yes | Referenced by `fix-worker` + tests. Not by `mt5-worker`. |

**Keep the current reference graph.** A11 already measured:

```text
Api        → Domain, Application, Infrastructure     (no Mt5)
Mt5Worker  → Domain, Application, Infrastructure, Mt5
FixWorker  → Domain, Application, Infrastructure, Fix.CTrader
```

If someone adds `ProjectReference` from Api → Mt5 after native interop lands, **Linux API publish will drag or break on the DLL**. That is a deployment defect, not a convenience.

---

## 5. Two legal modes — only one is Phase 1

### Mode A — Phase 1 default (matches §7 `MT5_MODE=local`)

```text
Windows mt5-worker process
    loads MT5APIManager64.dll in-process
    IMt5BrokerConnector implementation talks native
    persists via Infrastructure → Linux PostgreSQL
```

How C# talks to the native layer is an **implementation detail on Windows**, all of which are legal:

1. P/Invoke / C++/CLI / nint wrapper around first-party `mt5sdk` (preferred: reuse `MT5Manager` + `MT5Pool` + `MT5Watchdog`).
2. Same-box C++ process + loopback HTTP, C# still hosted on Windows (`MT5_MODE=remote` with `http://127.0.0.1:…`). DLL still Windows.
3. Direct P/Invoke of `MT5APIManager64.dll` from C# (higher risk; duplicates A14 work).

None of these move the DLL to Linux.

### Mode B — optional later (`MT5_MODE=remote`)

A Windows process exposes the HTTP surface already inventoried in A16 (`/mt5/health`, `/mt5/groups`, `/mt5/accounts/{login}/deals`, SSE `/mt5/events/stream`, …). Other processes — including a Linux process — may use `MT5HttpClient`.

This mode is **allowed** as a sharing/pooling tactic. It is **not** a way to claim “we run mt5-worker on Linux.”

Do **not** pick Mode B for Phase 1 because:

| Cost | Evidence |
|---|---|
| Extra service + auth (`X-API-Key`) | A16 |
| `GetGroupDetails` returns false remotely | A16 / `mt5_http_client.cpp` — “require direct SDK access” |
| `GetOrders` default false | A16 |
| Tick subscribe default false | A16 |
| §7 / §56 both say `MT5_MODE=local` | architecture |
| Completeness of live groups/accounts is a Phase 1 gate | §67 |

Mode B becomes interesting only if **multiple** Windows consumers must share one manager-slot budget. One collector process does not need it.

### Illegal modes (do not design, do not spike in prod)

| Idea | Class | Why |
|---|---|---|
| Copy `MT5APIManager64.dll` into `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux) | `UNSAFE` | ELF host cannot `LoadLibrary` a PE |
| `wine64` the worker in a Linux container | `UNSAFE` | Unsupported; pump threads + Win32 sync + broker TCP will lie |
| Build `mt5_manager.cpp` with MinGW and run under Wine | `UNSAFE` | Same |
| “Linux worker, Windows DLL via SMB mount” | `UNSAFE` | Still PE on ELF |
| Put Manager login/password on the Linux API so it can “proxy Manager calls” | `UNSAFE` | Expands secret blast radius; API is a browser BFF |
| Make React call a Windows Manager HTTP port | `UNSAFE` | §55 — never expose secrets or Manager to the browser |
| One mega `docker-compose` that includes `mt5-worker` on Linux | `DEPRECATED` | Violates §5 in a file that looks official |
| Kubernetes DaemonSet of the DLL | `DEPRECATED` | §71: do not build k8s yet; also wrong OS |

---

## 6. Cross-host contracts (what actually crosses the OS line)

The Windows worker and the Linux stack share **data**, not the SDK.

### 6.1 PostgreSQL is the seam (required, Phase 1)

| Writer | Reader | Tables (architecture §§11, 13, 45) |
|---|---|---|
| Windows `mt5-worker` | Linux `api`, later reconstruct/score/shadow | `brokers`, `broker_connections`, `mt5_groups`, `mt5_accounts`, `mt5_account_snapshots`, `mt5_orders`, `mt5_deals`, `mt5_positions_current`, `mt5_symbols` / `mt5_symbol_metadata`, `sync_checkpoints`, `ingestion_events`, `outbox_events` |
| Linux `api` / later workers | everyone | auth, audit, scores, shadow, FIX, risk — **not** raw deal identity |

Identity (architecture §10) is what makes one Linux database safe for two Windows-side brokers:

```text
broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id
```

Worker connection string lives **only** on the Windows host. API has its **own** role (read-mostly + its own writes). Do not share a superuser. Prefer:

```text
role ti_mt5_ingest   — INSERT/UPDATE raw + checkpoints + outbox
role ti_api          — SELECT raw; R/W dashboard/auth
role ti_migrator     — DDL from CI / ops, not from the worker loop
```

TLS to Postgres in any non-lab environment (§72.12 spirit). Private network only; do not publish `5432` to the internet so the Windows VM can “just connect.”

`src/Infrastructure` already references `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4` and now has `Persistence/TraderDbContext.cs`. That context is the **portable** write/read model. The worker on Windows and the API on Linux both use it. Migrations run from a designated Linux (or CI) job, not from `LoadLibrary` success.

### 6.2 Redis (optional for Phase 1 ingestion; required later)

Use from Linux API / FIX worker for:

- live scores
- short-lived dashboard cache
- FIX TRADE session ownership lock (§28)

The Windows worker **may** publish a `mt5:connected:{broker_id}` key for snappy health, but **`broker_connections` in Postgres is the durable status**. Redis must not be the only place “Achiever is connected” lives — a Redis flush must not look like a broker outage and must not look like a broker recovery.

Do not write deals to Redis.

### 6.3 Health (so the Linux dashboard can tell the truth)

Do **not** make the Linux API open a Manager socket.

Recommended:

1. Windows worker upserts `broker_connections` (`connected`, `pump_mode`, `stale_since`, `last_error_code`, `last_ok_utc`) on a short cadence and on state change.
2. Windows worker may expose **private** `GET /health/mt5` (Kestrel on `127.0.0.1` or a VPC NIC, not `0.0.0.0` public) for ops and for the API to scrape if Postgres is briefly stale.
3. Linux API `/health/ready` = Postgres + Redis.
4. Linux API `/health/sources` = **read** `broker_connections` (and optionally scrape 2). Dashboard “Brokers” / “System Health” pages bind here.
5. Metrics (§58) from the worker (`mt5_connected`, `mt5_reconnects`, `mt5_events_total`, `mt5_deals_total`, `mt5_duplicate_deals_total`, `mt5_backfill_lag`, `mt5_outbox_backlog`) scraped by a Linux Prometheus **or** pushed as rows. Scraping a Windows exporter on the private net is fine. Do not send Manager passwords as metric labels.

### 6.4 Outbox stays in Postgres (§13)

The collector (Windows) commits raw row + `outbox_events` in **one** transaction.

Who **processes** the outbox?

| Option | When |
|---|---|
| Same Windows process, second hosted service | Phase 1 — simplest, no extra hop. A07 already requires an outbox hosted service on the worker. |
| Linux outbox processor | Later, if handlers are scoring/shadow/FIX. They already live on Linux. |

Do not introduce Kafka (§13, §71).

### 6.5 What must **not** cross the OS line

| Thing | Stays on |
|---|---|
| `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, proxy user/password | Windows worker secret store |
| Manager DLL files | Windows worker directory |
| Pump callbacks / `MT5EventQueue` | Windows process memory |
| Achiever whitelist / proxy | Windows egress path |
| Browser-facing JWT / RBAC | Linux API |
| FIX SenderCompID / passwords | Linux `fix-worker` secrets (later) — **separate** from MT5 secrets |

Linux API `appsettings` must not grow `MT5_PASSWORD` “for completeness.”

---

## 7. Network and Achiever whitelist

### 7.1 Flows

| From | To | Port | Why |
|---|---|---|---|
| Windows worker | `57.128.141.65` | 443 | Achiever Manager |
| Windows worker | `84.201.6.142` | 443 | StarwaveFX Manager |
| Windows worker | Linux Postgres | 5432 | SoT writes |
| Windows worker | Linux Redis | 6379 | optional live keys |
| Linux nginx | Linux API | 8080 | BFF |
| Browser | Linux nginx | 443 | UI |
| Linux API | Postgres / Redis | 5432 / 6379 | reads/cache |
| Linux `fix-worker` (later) | `live-us-eqx-01.p.c-trader.com` | 5211 / 5212 SSL | QUOTE / TRADE |
| Linux API | Windows `:9101` | optional | private health scrape |

No path: Browser → Windows. No path: Linux API → `57.128.141.65`.

### 7.2 Achiever `81.29.145.69`

The Manager server will reject the Windows worker with **1012** if the source IP is not whitelisted (A14 maps this). Design choices, in order of preference:

1. Place the Windows worker on a host whose **default egress** is `81.29.145.69`.
2. Enable §56 proxy (`ACHIEVER_PROXY_HOST=81.29.145.69`, port `49527`, secrets in Windows-only env). `MT5Manager::SetProxy` already exists.
3. Do **not** SNAT Achiever through the Linux compose network. Linux has no reason to speak Manager TCP.

StarwaveFX has no whitelist today; still design proxy as optional (§8).

Linux outbound (API, FIX, apt, nuget) must **not** steal or share that NAT in a way that breaks the whitelist or exposes the broker to the web box.

### 7.3 Firewall sketch

Windows worker:

- inbound: none from internet; optional VPC `9101` from Linux API only
- outbound: 443 to the two broker IPs (or proxy), 5432/6379 to Linux data plane, Windows Update as needed

Linux:

- inbound: 443 (nginx) from operators / VPN; not the world until auth exists
- outbound: FIX later; package registries
- Postgres/Redis bound to compose/internal network only

---

## 8. Docker policy — “Docker where compatible”

### 8.1 Linux compose (appropriate)

A future `deploy/linux/docker-compose.yml` (name is illustrative; **not created in this pass**) should contain **only**:

```text
postgres
redis
api          # publish linux-x64
web          # nginx + dist
# later: fix-worker, ml-service
```

It must **not** list `mt5-worker`, must **not** `COPY` anything from `mt5-sdk/vendor/MetaTrader5SDK/Libs/`, must **not** set `MT5_MODE=local`.

Suggested service env (placeholders only):

```env
ConnectionStrings__TraderDb=Host=postgres;Database=trader;Username=ti_api;Password=<SECRET>
Redis__Configuration=redis:6379
ASPNETCORE_URLS=http://+:8080
```

No `MT5_*` passwords.

### 8.2 Windows worker — prefer VM, not a Linux container, not a clever Windows container

| Hosting | Verdict |
|---|---|
| Windows Server VM / bare metal, `sc.exe` or NSSM / Windows Service via `UseWindowsService` | **Preferred.** LoadLibrary, host NIC, proxy, dumps all behave. |
| Windows Server Core VM, same | Acceptable. |
| Windows container (process isolation) + host network + DLLs in the image | Possible later. Extra pain (licensing, isolation, AV). **Not Phase 1.** |
| Windows container (Hyper-V isolation) | Avoid for Manager TCP + whitelist IP. |
| Linux container | **Forbidden** for local mode. |

Publish command (when implementation is authorized — not run here):

```text
dotnet publish apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj -c Release -r win-x64 --self-contained false
```

Then copy the three runtime DLLs next to `TraderIntelligence.Mt5Worker.exe` (same contract as `mt5sdk_copy_runtime_dlls`). Pin `RuntimeIdentifier=win-x64` in the worker csproj when that work starts so a Linux CI agent cannot accidentally produce a Linux worker and call it done.

Linux API:

```text
dotnet publish apps/api/TraderIntelligence.Api.csproj -c Release -r linux-x64 --self-contained false
```

Base image: `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian). **Do not** switch the API image to `nanoserver` to “match the worker.”

### 8.3 CI split

| Job | Runner | Builds |
|---|---|---|
| `dotnet test` portable | Linux or Windows | Domain, Application, Infrastructure, Fix, Unit tests that do not load native |
| `mt5-sdk` local + probes | **Windows** MSVC + vcpkg `x64-windows` | `mt5_manager`, probes |
| `mt5-sdk` HTTP-only | Linux allowed | CMake without `WIN32` sources — proves Linux never needed the DLL |
| publish worker | Windows | `win-x64` + DLL copy smoke (`dumpbin /headers` machine 8664) |
| publish api/web | Linux | linux-x64 + nginx image |
| compose smoke | Linux | postgres healthy, api `/health/ready` |

A green Linux CI that never builds the worker is **not** “MT5 connected.”

---

## 9. Secret placement (§55 / §56 / A19)

A19 measured: no live passwords in the tree. Keep it that way.

| Secret | Windows worker | Linux API | Linux compose | React |
|---|---|---|---|---|
| `MT5_PASSWORD` | **yes** (UserSecrets / env / DPAPI) | no | no | no |
| `MT5_STARWAVEFX_PASSWORD` | **yes** | no | no | no |
| `ACHIEVER_PROXY_USERNAME/PASSWORD` | **yes** | no | no | no |
| Postgres ingest role | **yes** | no | yes (api uses a **different** role) | no |
| Postgres api role | no | **yes** | yes | no |
| Redis auth | optional | **yes** | yes | no |
| JWT signing | no | **yes** | yes | no |
| `CTRADER_FIX_PASSWORD` | no | no | fix-worker only | no |
| `VITE_API_URL` | n/a | n/a | build-arg (not a secret) | public |

Worker already has `UserSecretsId` (`dotnet-TraderIntelligence.Mt5Worker-6850a13e-…`) and does not use it. When implementation starts, that is the **lab** store. Production: Windows environment variables or a Windows-capable vault. Do not check `appsettings.Production.json` with passwords into the repo.

Never log proxy credentials or manager passwords (§7, §57).

---

## 10. Current measured state vs this design

| Item | Classification | Evidence |
|---|---|---|
| Architecture §5 / §7 split text | `EXISTS_AND_GOOD` | architecture lines 310–318, 359–400 |
| CMake `if(WIN32)` local transport | `EXISTS_AND_GOOD` | `mt5-sdk/CMakeLists.txt` 49–57 |
| Vendor Manager DLLs as PE x64 | `EXISTS_AND_GOOD` | §2.1 hashes / machine `0x8664` |
| `IMT5Client` portable + `MT5HttpClient` | `EXISTS_AND_GOOD` as **transport abstraction**; not a Linux Manager | A12, A16 |
| `apps/mt5-worker` as Windows collector | `EXISTS_NEEDS_REFACTOR` | A07: template `Task.Delay(1000)`; no RID; no DLL copy; no brokers |
| `src/Mt5` | `MISSING` (stub `Class1`) | must become the Windows-only adapter |
| Linux compose / Dockerfiles | `MISSING` | A08/A10: no compose in repo |
| API as Linux BFF | `EXISTS_NEEDS_REFACTOR` | A06: weatherforecast; `AllowedHosts=*`; no health |
| `apps/web` | `EXISTS_NEEDS_REFACTOR` | Vite/React shell exists (`App.tsx` routes); `VITE_API_URL` defaults `http://localhost:5000` while API launch is `:5160` |
| Infrastructure / Postgres | `EXISTS_NEEDS_REFACTOR` | packages + `TraderDbContext` present; no `ConnectionStrings`; hosts do not register DbContext (A03 was empty; context has since appeared — still not a running data plane) |
| Worker → Linux Postgres | `MISSING` | no connection string anywhere in appsettings |
| Windows Service install | `MISSING` | worker is a console `Host.CreateApplicationBuilder` |
| Forcing SDK into Linux | not present (good) | do not add it |
| `MT5_MODE=remote` Linux worker as default | `DEPRECATED` if proposed as the §7 path | §7 says `local` |

Do not treat “.NET is cross-platform” as “mt5-worker is cross-platform.” The **host TFM** is portable; the **native dependency** is not.

---

## 11. Failure and degraded operation across the split (§62)

| Failure | Windows worker | Linux API / UI |
|---|---|---|
| Achiever down / 1012 whitelist | Retry forever. Do not invent deals. Set `broker_connections.stale`. | Show source stale. Disable copy enablement that depends on that source. |
| Pump refused, request-only fallback | Stay up. Deal **poll** continues (`GetDeals`). Mark `pump_mode=false`. | “Connected, live pump degraded.” |
| StarwaveFX down, Achiever up | Other broker keeps ingesting. | Per-`broker_id` health. Never one red ball for both. |
| Linux Postgres unreachable | Stop advancing checkpoints. Buffer nothing that can lie. Alarm. | API unready. |
| Linux Redis down | Ingestion **continues** (Postgres only). | Dashboard cache miss; FIX lock later = fail closed on execution, not on MT5 ingest. |
| Windows worker process dead | No new raw rows. Checkpoints frozen. | Last `broker_connections` heartbeat ages out → stale. |
| Linux API dead | Ingestion **continues**. | UI down. Collectors do not care. |

This is the operational payoff of the split: **killing the website must not disconnect Manager.** Putting the DLL inside the API process would couple them. That is another reason not to “simplify” onto one Linux service.

---

## 12. Lab vs production (honest)

### Lab (this machine is already Windows)

It is legal to run **everything** on one Windows 11 box for development:

- Postgres/Redis via Linux VM, WSL2, or a remote Linux compose
- `dotnet run` API + web on Windows
- `dotnet run` mt5-worker on Windows next to the DLLs

WSL2 is **Linux**. Do not run local-mode worker *inside* WSL2. Run the worker as a **Windows** process; point it at Postgres on `localhost` forwarded from WSL if needed.

### Production

- Windows Server VM in the same private network as Linux data plane, **or** site-to-site with tight Postgres ACL.
- Egress IP `81.29.145.69` (or proxy) for Achiever.
- Linux compose/VM for api/web/postgres/redis/(fix).
- No Kubernetes until measurements demand it (§71).

---

## 13. Implementation sequence (authorized later — not this pass)

When a coding agent is allowed to touch product source, the deployment-shaped slice is:

1. **Do not** add a Linux Dockerfile for `mt5-worker`.
2. Pin `RuntimeIdentifier=win-x64` (or publish profile) on `TraderIntelligence.Mt5Worker`.
3. Add a documented post-build / publish step that copies the three Manager DLLs beside the exe (reuse `mt5sdk_copy_runtime_dlls` if the C++ lib is linked; otherwise an explicit `Content`/`CopyToOutputDirectory` from `mt5-sdk/vendor/.../Libs/`).
4. Keep `TraderIntelligence.Api` free of `src/Mt5`.
5. Add `ConnectionStrings` via env/user-secrets only; worker and API use different DB roles.
6. Add `deploy/linux/docker-compose.yml` for postgres + redis + api + web **only**.
7. Add worker health → `broker_connections` so Linux UI can render §48 Brokers without talking to Manager.
8. Windows Service hosting (`UseWindowsService`) so a reboot does not depend on an interactive session.
9. Only then: real `IMt5BrokerConnector` + the seven hosted services in A07.

Phase 1 acceptance (§67) is measured on the **Windows** process against live brokers, with rows landing in **Linux** Postgres. A Linux-only compose up is not Phase 1.

---

## 14. Risk list (deployment slice of §73.D)

| Risk | Severity | Mitigation |
|---|---|---|
| Someone publishes `mt5-worker` as `linux-x64` and files “SDK doesn’t work” | High | RID pin + CI job that fails if worker artifact is not PE |
| Wine spike declared success on a demo | High | Forbidden in this document; no lab exception for production claims |
| Mode B used to hide Windows, then `GetGroupDetails` is empty | High | Phase 1 = Mode A; remote is optional and incomplete (A16) |
| Manager passwords copied into Linux compose “so API can reconnect” | High | Secret matrix in §9; API has no Manager client |
| Achiever 1012 because worker egress is the Linux NAT | High | Worker host/proxy = `81.29.145.69` |
| Pool 8 + pump + Starwave 4 + pump exceeds manager licenses | Medium | Size to broker slot budget (A15); two brokers on one Windows box still consume **both** slot sets |
| `src/Mt5` referenced by Api after native interop | High | Reference graph lock; Linux publish must not copy `MT5API*.dll` |
| Vendor DLL redistributed in a public container registry | Medium | `mt5-sdk/README.md`: vendor SDK is **not** ours to sublicense; private images only |
| Worker writes to a Windows-local Postgres “temporarily” and never moves | Medium | SoT is Linux Postgres from the first real ingest; local PG is lab-only |
| Dashboard treats Redis heartbeat as Achiever health | Medium | Postgres `broker_connections` is source of status |
| Kubernetes proposal to “unify OS” | Low / policy | §71 forbids k8s for now; it would not anyway load a PE on a Linux node |

---

## 15. Direct answers

### Architecture §5 — how do we deploy?

- **Docker** the compatible pieces: API, web, Postgres, Redis, later FIX/ML — **on Linux**.
- **Windows Worker** for MT5 because the Manager DLL **does** require Windows (measured in §2).
- **Do not force** native SDK components into Linux containers. The CMake tree already refuses to compile them there. Product deploy must refuse too.

### Architecture §7 — where does Achiever `MT5_MODE=local` run?

On the Windows `mt5-worker` host, loading `MT5APIManager64.dll`, connecting to `57.128.141.65:443` as login `2027`, presenting `81.29.145.69` (or the configured proxy). It enumerates **all** groups, not only `demo\Maxmaster`. StarwaveFX is a second local connector in the **same** Windows process.

### What is the split?

```text
Windows:  mt5-worker + Manager DLLs + broker TCP + MT5 secrets
Linux:    api + postgres + redis + react (+ fix-worker + python later)
Seam:     PostgreSQL (and optional Redis / private health)
```

### Is this implemented?

**No.** This is the deployment map Phase 0 asked for (`§67` “deployment map”, `§73.D` “Windows/native DLL constraints”). Product source was not changed to invent compose files or RIDs.

---

## 16. Evidence pins

| Claim | Pin |
|---|---|
| §5 deployment law | architecture lines 310–318 |
| §7 Achiever `MT5_MODE=local`, whitelist, sequence | architecture lines 359–400 |
| §8 second local broker | architecture lines 413–436 |
| §56 secret-safe env | architecture lines 2031–2069 |
| §66 `apps/mt5-worker` vs `apps/api` / `apps/web` | architecture lines 2423–2434 |
| §67 Phase 1 = brokers connected, not “compose up” | architecture lines 2493–2506 |
| §71 no k8s/mesh yet | architecture lines 2681–2695 |
| CMake Windows-only local sources | `D:\Prop\mt5-sdk\CMakeLists.txt` 49–57, 114–123, 164 |
| `Windows.h` | `D:\Prop\mt5-sdk\src\core\mt5_manager.h:3` |
| `LoadLibraryW` | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h:1726` |
| README Windows x64 only | `D:\Prop\mt5-sdk\README.md` 39–40, 48 |
| PE AMD64 DLLs | this report §2.1 (machine `0x8664`) |
| Worker is still a template | `D:\Prop\apps\mt5-worker\Worker.cs`, A07 |
| API has no Mt5 reference | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` |
| Worker references Mt5 | `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` |
| Infrastructure has Npgsql + Redis packages | `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` |
| Web is Linux-friendly Vite | `D:\Prop\apps\web\package.json`, `vite.config.ts` port 3000 |
| No docker-compose in repo | A10 / tree search |
| HTTP remote is incomplete vs local | A16 |

---

## 17. Bottom line

The MetaQuotes Manager API in this repository is a **Windows x64 PE** loaded with **`LoadLibraryW`**. Architecture §5 already chose the only honest layout: **Windows `mt5-worker`**, **Linux API / Postgres / Redis / React**. Architecture §7’s `MT5_MODE=local` is that Windows process, not a Linux container and not Wine.

Do not force the SDK onto Linux. Do not move the worker to Linux by renaming the problem `remote`. Put the DLL next to a `win-x64` collector, point that collector at Linux PostgreSQL, and keep Manager secrets and broker TCP on the Windows side of the fence.

**Written:** `D:\Prop\reports\swarm\20260818\A54_deployment_split.md`  
**Product source modified:** none.
