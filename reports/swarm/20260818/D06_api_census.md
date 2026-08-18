# D06 — `apps/api` census (confirm no weatherforecast route)

| Field | Value |
|---|---|
| Agent | D06 (senior engineer, API host census) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\D06_api_census.md` |
| Scope | `D:\Prop\apps\api` product source + measured Debug output. Adjacent consumers (`apps/web`, `docker-compose.yml`, `src/Application`, `src/Infrastructure`) read-only. |
| Ask | Inventory `D:\Prop\apps\api`. Confirm **no weatherforecast route**. Do **not** modify product source. |
| Product source modified | **No.** This report (plus INDEX / SWARM_LOG catalog) is the only write. |
| Method | Full read of every non-`bin`/`obj` file under `D:\Prop\apps\api`. `Get-ChildItem` tree + SHA-256. `Select-String` for `weatherforecast` / `WeatherForecast` including `bin`/`obj`. ASCII + UTF-16 scan of `TraderIntelligence.Api.dll`. Token counts on `Program.cs`. Cross-read `IDashboardQueries`, `EfDashboardQueries`, `DependencyInjection`, `DemoSeeder`, `hooks.ts`, `client.ts`, `signalr.ts`, `docker-compose.yml`, `Mt5TraderIntelligence.sln`. Product-tree string walk of `apps/` + `src/` + `tests/` + compose (exclude `bin`/`obj`/`node_modules`). **API process was not launched.** No HTTP capture. Route list is from `MapGet` / `MapPost` as read. |
| Law | Architecture v2 §§46–55, 59, 69; A63 first-useful catalog; A77 probes. Classification vocabulary is §73. |
| Relates | A06 (stale: host is weatherforecast-only), B06 (stale: 2 leftover weather strings), C04 / C15 (IIS `launchUrl` leftover — **closed on disk now**), C22 (CORS/Swagger), C28 (no hub), C50 (`.http` incomplete), C18 (no RBAC) |
| Supersedes | A06 / A26 / A55 / A63 host-state (“only `/weatherforecast`”). B06 leftover-A (`.http`) and leftover-B (IIS `launchUrl`). C04 §5.3 / C15 “one leftover launchUrl” **for `launchSettings.json` only**. Does **not** supersede A63 as the v1 contract, or C04 on secret-sanitizer absence. |

---

## 0. Headline (measured)

**Confirmed: there is no weatherforecast HTTP route.**  
`Program.cs` does not map `GET /weatherforecast`. There is no `WeatherForecast` type, no `WeatherForecast.cs`, no `Controllers/` folder. The Debug API DLL has **0** `weatherforecast` / `WeatherForecast` strings (ASCII and UTF-16). Product source under `D:\Prop\apps\api` has **0** hits. A walk of `D:\Prop\apps`, `D:\Prop\src`, `D:\Prop\tests`, and `D:\Prop\docker-compose.yml` (excluding `bin`/`obj`/`node_modules`) also has **0** hits.

The host is an **anonymous unversioned demo BFF**: **14** `MapGet` + **1** `MapPost` in `Program.cs`. It is **not** the A63 `/api/v1` catalog. `GET /weatherforecast` is an unmapped path (expected **404** if anyone still probes a running process). That 404 was **not** observed this pass — the process was not started.

Honest one-liner: **weatherforecast is gone from the route table, the type system, the `.http` sample, every launch profile, the Debug DLL, and the rest of `apps`/`src`/`tests`. What remains is a 15-map anonymous demo host, not first-useful v1.**

| Question | Answer |
|---|---|
| `MapGet("/weatherforecast")`? | **No.** |
| `record WeatherForecast` / `WeatherForecast.cs`? | **No.** |
| `weatherforecast` string in `apps/api` product source? | **0** (including `launchSettings.json` and `.http`) |
| `weatherforecast` in Debug `TraderIntelligence.Api.dll`? | **0** |
| `weatherforecast` in `apps` / `src` / `tests` product files? | **0** |
| Live HTTP surface | **15** anonymous unversioned maps |
| `/api/v1/**` | **0** |
| Auth / RBAC / SignalR hub | **MISSING** |

---

## 1. Tree (what exists on disk)

`D:\Prop\apps\api` is a single-file minimal-API host. **No** `Controllers/`, `Hubs/`, `Auth/`, `Endpoints/`, `Dockerfile`, `.env`, `.env.example`, or `appsettings.Production.json`.

```
D:\Prop\apps\api\
  Program.cs
  TraderIntelligence.Api.csproj
  TraderIntelligence.Api.http
  appsettings.json
  appsettings.Development.json
  Properties\launchSettings.json
  bin\Debug\net8.0\          (build output; 42 files)
  obj\                       (28 files)
```

| Bucket | Count |
|---|---:|
| Product source files (exclude `bin`/`obj`) | **6** |
| Product folders besides `Properties` | **0** authored (no Controllers/Hubs) |
| `bin` files | **42** (12 234 413 bytes) |
| `obj` files | **28** (409 816 bytes) |
| Authored `.cs` in this project | **1** (`Program.cs`) |

---

## 2. Product-source hashes (this census)

Measured `Get-FileHash -Algorithm SHA256` after the last write on each file.

| Path | Bytes | LastWrite (local) | SHA-256 |
|---|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | 4731 | 2026-08-18 13:35:15 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 2026-08-18 12:55:15 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 2026-08-18 13:20:38 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` |
| `D:\Prop\apps\api\appsettings.json` | 431 | 2026-08-18 13:15:01 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` |
| `D:\Prop\apps\api\appsettings.Development.json` | 127 | 2026-08-18 12:54:17 | `73F95F9E0CEB205FC1C4DC50C07697FCFA29D7087868C2AEF1D504CB38C771EC` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 2026-08-18 13:32:01 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` |

`Program.cs` line endings: **LF only** (95 LF, 0 CRLF). 95 lines / 86 non-blank.

### 2.1 Drift vs C04 (same day, earlier snapshot)

| File | C04 | D06 | Meaning |
|---|---|---|---|
| `Program.cs` | 4658 B `E914FA98…` | 4731 B `61B1E0D1…` | Host grew. Weather route **still absent**. Trader detail now calls `GetTraderDetailAsync`. `/api/health` strings more honest (QUOTE `healthy: false`). |
| `launchSettings.json` | 1133 B `E092DE59…` IIS `launchUrl` = `weatherforecast` | 1125 B `BC022898…` IIS `launchUrl` = `swagger` | **Last weather leftover closed.** 8-byte shrink is `weatherforecast` (15) → `swagger` (7). |
| `.http` / `.csproj` / both appsettings | same | same | Unchanged since C04 / C50 |

Debug `TraderIntelligence.Api.dll` last write **2026-08-18 13:22:26** (32 256 bytes) is **older** than current `Program.cs` (13:35:15). The on-disk DLL does **not** include the 13:35 host edits. It still has **0** weatherforecast strings.

---

## 3. Project identity

| Item | Value |
|---|---|
| Project | `TraderIntelligence.Api` |
| Path | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` |
| SDK | `Microsoft.NET.Sdk.Web` |
| TFM | `net8.0` |
| Nullable / implicit usings | enabled (csproj + root `Directory.Build.props`) |
| `UserSecretsId` | **ABSENT** |
| Solution | `D:\Prop\Mt5TraderIntelligence.sln` project `{D17266FA-2F65-4F00-9701-BC5DD52B8439}` |
| Direct project refs | `src\Domain`, `src\Application`, `src\Infrastructure` |
| Does **not** reference | `Fix.CTrader` (Infrastructure references `Mt5`; API does not reference Fix) |
| Direct packages | `Microsoft.AspNetCore.SignalR.Common` 8.0.4, `Serilog.AspNetCore` 8.0.2, `Swashbuckle.AspNetCore` 6.6.2 |
| Shared framework | `Microsoft.AspNetCore.App` (from Web SDK) |
| Informational version (generated) | `1.0.0+398a14200ec65714c4077eed55c46808382ca1e3` (`obj\Debug\net8.0\TraderIntelligence.Api.AssemblyInfo.cs`) |

Transitive on the Debug output that matter for honesty: `FluentValidation` 11.9.2 (Application), EF Core 8.0.4 + InMemory + Npgsql 8.0.4, `StackExchange.Redis` 2.8.0 (Infrastructure — **not used by this host**), Swashbuckle UI DLL present even though `UseSwaggerUI()` is never called.

---

## 4. HTTP surface (complete)

Every handler is anonymous. CORS `AllowAnyOrigin` applies. There is **no** `MapControllers`, `MapHub`, `MapGroup("/api/v1")`, `MapPut`, `MapPatch`, or `MapDelete`.

| # | Method | Path | Source | What it returns |
|---|---|---|---|---|
| 1 | `GET` | `/health` | literal | `{ status: "ok", utc }` — process liveness. Extra `utc` vs A77. **No DB I/O.** |
| 2 | `GET` | `/ready` | `TraderDbContext` | `{ ready: true, brokers }` via `CountAsync(db.Brokers)`. Always `ready: true`. In-memory when connection string is empty. A77 **PARTIAL**. |
| 3 | `GET` | `/api/health` | **hardcoded** | Demo inventory. ACHIEVER `healthy: true` with details `demo FakeMt5BrokerConnector — not live Manager`. QUOTE `healthy: false`, details `no live TLS socket`. Redis `healthy: false`. `outboxBacklog: 0`. **Not a real probe.** Honesty of the *strings* improved vs C04 (`QUOTE` was `healthy: true`). |
| 4 | `GET` | `/api/overview` | `IDashboardQueries.GetOverviewAsync` | Flat `OverviewDto`. |
| 5 | `GET` | `/api/brokers` | `GetBrokersAsync` | `BrokerStatusDto` list. `Connected` hardcoded `true` in the query impl. |
| 6 | `GET` | `/api/groups` | `GetGroupsAsync` | `GroupRowDto` list. Path is **not** A63 `/api/v1/mt5/groups`. |
| 7 | `GET` | `/api/traders` | `GetTradersAsync(broker, state)` | `TraderRowDto` list. `MlProbability` is null (correct). |
| 8 | `GET` | `/api/traders/{broker}/{login}` | `GetTraderDetailAsync` | `TraderDetailDto` = header row + `TradeHighlightDto` list (`IsFirstThree` on first three completed XAU). **Not** A93 full detail (no features / lot-timeline / scores history / shadow). |
| 9 | `GET` | `/api/trades` | raw EF | `ReconstructedTrade` entities, last 200, login filter only. `broker` query declared and **unused**. Allow-list law violated; entity has no secret columns. |
| 10 | `GET` | `/api/fix/sessions` | `GetFixSessionsAsync` | `FixSessionDto`. Password / CompIDs / SubIDs not projected. `ExecutionEnabled` hardcoded `false`. Seeded session enums can still paint “healthy” on overview (D22). |
| 11 | `GET` | `/api/risk` | `GetRiskAsync` | `RiskDashboardDto`. PnL / exposure fields 0 in the query impl. |
| 12 | `GET` | `/api/risk/status` | same as `/api/risk` | Alias. |
| 13 | `GET` | `/api/reconciliation/status` | **hardcoded** | `{ lastReconciliation: now, unknownPositions: 0, mismatches: 0, orphanFills: 0 }`. Invented clean recon. |
| 14 | `GET` | `/api/settings` | **hardcoded** | Flags + broker names only. Does **not** bind `CTrader` options. `REAL_COPY_EXECUTION_ENABLED: false`. |
| 15 | `POST` | `/api/ops/resync` | ingestion + scoring | Syncs `ACHIEVER` + `STARWAVEFX` from 2026-01-01; rebuilds logins `10001,10002,10003,99001`. Response `{ achieverDeals, starwaveDeals }`. **Anonymous mutation.** Not in A63. |

Framework-adjacent (Development only): `UseSwagger()` → `/swagger/v1/swagger.json`. **`UseSwaggerUI()` is not called.** Browser `launchUrl: swagger` is therefore a likely **404** (OpenAPI JSON may still exist). Production does not map Swagger (good vs A63 “no prod Swagger UI”).

---

## 5. Weatherforecast confirmation (binding ask)

### 5.1 Tokens in `Program.cs` (this hash)

| Token | Hits |
|---|---:|
| `weatherforecast` / `WeatherForecast` | **0** |
| `MapGet` | **14** |
| `MapPost` | **1** |
| `MapPut` / `MapPatch` / `MapDelete` / `MapHub` / `MapControllers` | **0** |
| `AddAuthentication` / `AddAuthorization` / `AddSignalR` / `UseSerilog` / `UseSwaggerUI` / `UseHttpsRedirection` | **0** |
| `api/v1` | **0** |
| `AddCors` / `AllowAnyOrigin` / `UseSwagger` / `EnsureCreated` / `DemoSeeder` | **1** each |

The stock A06 / B06 block is **not** in the file:

```text
app.MapGet("/weatherforecast", () => …);
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);
```

### 5.2 String search

| Where | Hits |
|---|---:|
| `D:\Prop\apps\api` product source (6 files) | **0** |
| `D:\Prop\apps\api\bin` + `obj` text `Select-String` | **0** |
| Debug `TraderIntelligence.Api.dll` ASCII | **0** |
| Same DLL UTF-16 | **0** |
| `D:\Prop\apps` + `D:\Prop\src` + `D:\Prop\tests` authored files (no `bin`/`obj`/`node_modules`) | **0** |
| `D:\Prop\docker-compose.yml` | **0** (no healthcheck at all; does **not** probe `/weatherforecast`) |

`Test-Path` of historical leftovers:

| Path | Exists |
|---|---|
| `D:\Prop\apps\api\WeatherForecast.cs` | **False** |
| `D:\Prop\apps\api\Controllers\WeatherForecastController.cs` | **False** |
| `D:\Prop\apps\api\Controllers\` | **False** |

### 5.3 Launch / REST-client leftovers — **closed**

| Artifact | C04 / C15 | D06 now |
|---|---|---|
| `.http` `GET …/weatherforecast/` on `:5160` | already **GONE** (C04/C50) | still **GONE**. File samples `/health` + 6 unversioned `/api/*` on `:5000`. |
| `http` / `https` `launchUrl` | `swagger` | `swagger` |
| IIS Express `launchUrl` | **`weatherforecast`** (last leftover) | **`swagger`** |

All three profiles now launch `swagger`. That is **not** a weatherforecast route. It is a half-migration: Swagger UI is not wired.

### 5.4 Verdict line for the ask

**No weatherforecast route exists.** Do **not** add a compat alias. A GET that 404s is the correct end state. Replacement for the template’s only honest job (liveness) is `GET /health` (A77). That map exists.

Reports under `D:\Prop\reports\` still *name* the dead path (A06, A55, A63, A65 sketches, etc.). Those are historical / stale prose, not a live route.

---

## 6. Composition (what `Program.cs` actually does)

Order:

1. `WebApplication.CreateBuilder`
2. `AddTraderIntelligence(builder.Configuration)` — `D:\Prop\src\Infrastructure\DependencyInjection.cs`
3. `ConfigureHttpJsonOptions` + `JsonStringEnumConverter`
4. `AddEndpointsApiExplorer` + `AddSwaggerGen`
5. CORS default policy: `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`
6. `UseCors()`. Development: `UseSwagger()` only
7. Fifteen maps (§4)
8. Startup scope: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync` (resolves `TraderDbContext`, `ITradingStore`, `ReconstructionScoringService`)
9. `app.Run()`

`AddTraderIntelligence` (not in `apps/api`, but this is the host’s entire backend):

- Empty / `<SECRET>` `ConnectionStrings:TraderIntelligence` **or** `DATABASE_URL` → `UseInMemoryDatabase("trader-intelligence")`
- Else Npgsql
- Two singleton `IMt5BrokerConnector` from `DemoBrokerFactory.CreateDefault()` (fake connectors)
- `ITradingStore` / `IDashboardQueries` / `DealIngestionService` / `ReconstructionScoringService` / `TradeReconstructor` / `BaselineScorer`

Current `appsettings.json` connection string is `""` → **in-memory** on a default `dotnet run`. Compose does **not** pass `ConnectionStrings__TraderIntelligence`, so a compose-started API is also in-memory unless the environment is set elsewhere. That is a deployment fact, not a weatherforecast fact.

`ITradingStore` resolves via `using TraderIntelligence.Application.Ingestion;` (C34 still true).

---

## 7. Config / listen table

### 7.1 `appsettings.json` (431 B)

- `Logging` defaults
- `AllowedHosts`: `*`
- `ConnectionStrings:TraderIntelligence`: `""`
- `CTrader` block: `Host=live-us-eqx-01.p.c-trader.com`, `AccountId=1369850`, `Password=""`, `UseSsl=true`, `QuoteEnabled=true`, `TradeSessionEnabled=true`, `RealCopyExecutionEnabled=false`

**No map reads this section.** Empty `Password` is a **slot**. Do not later `return configuration.GetSection("CTrader")`.

### 7.2 `appsettings.Development.json`

Logging only.

### 7.3 `launchSettings.json` (all three `launchUrl` = `swagger`)

| Profile | URLs | Auth |
|---|---|---|
| `http` | `http://localhost:5000` | Kestrel, Development |
| `https` | `https://localhost:7294` + `http://localhost:5000` | same |
| IIS Express | `http://localhost:18720`, SSL `44389` | `windowsAuthentication: false`, `anonymousAuthentication: true` |

Stock `dotnet new webapi` port `:5160` is **gone** from launch settings and from `.http`.

### 7.4 Compose (`D:\Prop\docker-compose.yml`)

`api` service: `dotnet run --project apps/api/TraderIntelligence.Api.csproj --urls http://0.0.0.0:5000`, port `5000:5000`, depends on postgres + redis. **No** `healthcheck`. **No** `/weatherforecast` probe. **No** `apps/api/Dockerfile`.

---

## 8. Packages vs features (do not greenwash)

| Package / capability | Wired? |
|---|---|
| Swashbuckle 6.6.2 | Partial: gen + `UseSwagger`. **No UI.** |
| Serilog.AspNetCore 8.0.2 | **No** `UseSerilog` (C25) |
| SignalR.Common 8.0.4 | **No** hub. Wrong package for hosting. `AddSignalR` / `MapHub` **absent** (C28) |
| JWT / Identity / OpenIddict | **Not referenced** |
| Health checks package | **Not referenced** (hand-rolled `/health` + `/ready`) |
| OpenTelemetry | **Not referenced** (C26) |
| Redis multiplexer on this host | **Not registered** (package is on Infrastructure) |
| EF migrations | **None.** `EnsureCreated` only (C29) |

---

## 9. Consumer (read-only)

`D:\Prop\apps\web\src\api\client.ts`: axios `baseURL = VITE_API_URL || http://localhost:5000`. Vite `:3000` has **no** proxy (`vite.config.ts` is `server.port = 3000` only). CORS `*` is why the browser can call `:5000` today (C22 / C24).

`hooks.ts` GETs the unversioned set: `/api/overview`, `/brokers`, `/groups`, `/traders`, `/traders/{broker}/{login}`, `/trades`, `/fix/sessions`, `/risk`, `/reconciliation/status`, `/health`, `/settings`. Does **not** call `/health` (liveness), `/ready`, `/api/risk/status`, or `POST /api/ops/resync`.

`signalr.ts` dials `${BASE}/hubs/dashboard`. **That hub is not mapped.** Binding name is `/hubs/ops` (A63). Client swallows start failure.

`.http` samples 7 of 15 live maps and has **no** `###` separators (C50: update still needed; not a weather leftover).

---

## 10. vs A63 first-useful catalog

A63 §7.1 is **2 probes + 46 HTTP routes + 1 hub**. This host implements **none** of the versioned paths.

| A63 surface | This host |
|---|---|
| `GET /health` | **PRESENT** (extra `utc`) |
| `GET /ready` (A77) / `GET /health/ready` (A63) | `/ready` **PRESENT**, always `ready: true` |
| `/api/v1/**` (46 routes) | **0 / 46** |
| Auth login / logout / refresh / me | **MISSING** |
| `GET /api/v1/overview` envelope | unversioned flat DTO |
| `GET /api/v1/mt5/groups` + PATCH + accounts | `/api/groups` GET only |
| Trader sub-resources (features, flags, lot, holding, scores, shadow, PATCH state) | **MISSING** (detail is header + highlights only) |
| Shadow / copy-intents / scoring summary | **MISSING** |
| FIX `/quote` `/trade` / events | **MISSING** (combined `/api/fix/sessions` only) |
| Risk snapshot / rejections / kill-switch POST | `/api/risk` stub only |
| Reconciliation runs / issues / ack | hardcoded `/api/reconciliation/status` |
| Hub `/hubs/ops` | **MISSING** |
| `GET /weatherforecast` | **ABSENT (correct)** |

---

## 11. Classification (arch §73)

| Component | Class |
|---|---|
| `apps/api` host | **EXISTS_NEEDS_REFACTOR** |
| `GET /weatherforecast` + `WeatherForecast` type | **GONE** |
| `.http` weather sample | **GONE** |
| IIS / Kestrel `launchUrl` weather string | **GONE** |
| Unversioned `/api/*` demo maps | **EXISTS_NEEDS_REFACTOR** — not the v1 contract |
| `/api/v1` + auth + sanitizer + hub | **MISSING** |
| `POST /api/ops/resync` anonymous | **UNSAFE** (ops door, not a secret dump) |
| CORS `AllowAnyOrigin` + `AllowedHosts=*` | **UNSAFE** for anything beyond local demo |
| `/api/trades` EF dump | **EXISTS_NEEDS_REFACTOR** (allow-list miss) |
| `/api/health` + `/api/reconciliation/status` hardcoded | **UNSAFE** as evidence (invented / demo bits) |
| `/api/settings` hardcoded flags | **EXISTS_AND_GOOD** *as a public-flag shape* — do not bind options |
| Swagger UI vs `launchUrl: swagger` | **EXISTS_NEEDS_REFACTOR** |
| Serilog / SignalR.Common package refs | **EXISTS_NEEDS_REFACTOR** (dead refs) |

---

## 12. Findings (this census only)

| ID | Sev | Finding |
|---|---|---|
| D06-01 | **PASS** | **No weatherforecast route.** No type, no controller, no product-source string, no DLL string, no compose probe. |
| D06-02 | **PASS** (leftover) | C04/C15 IIS Express `launchUrl=weatherforecast` is **gone**. All profiles are `swagger`. |
| D06-03 | **INFO** | `Program.cs` SHA moved since C04 (`E914FA98…` → `61B1E0D1…`). Detail route now `GetTraderDetailAsync`. `/api/health` no longer claims a healthy QUOTE session. Weather still absent. |
| D06-04 | **BLOCKER** (product, not leftover) | **0** `/api/v1` routes, **0** auth, **0** hub. Unversioned `/api/*` is a demo BFF. |
| D06-05 | **HIGH** | Anonymous `POST /api/ops/resync` + `AllowAnyOrigin`. |
| D06-06 | **MED** | `launchUrl: swagger` without `UseSwaggerUI()`. Half-migration from weatherforecast. |
| D06-07 | **MED** | Debug DLL is stale vs `Program.cs` (13:22 vs 13:35). Do not treat the 13:22 DLL as proof of the 13:35 handlers. Weather absence holds on **both**. |
| D06-08 | **LOW** | `.http` still 7/15 maps, no `###` (C50). Not a weather issue. |

---

## 13. What this census did **not** do

- Did not modify `apps/api` or any product source.
- Did not `dotnet run` / `curl` `:5000`. The 404 on `/weatherforecast` is inferred from the absence of a map, not observed.
- Did not rebuild the stale Debug DLL.
- Did not implement `/api/v1`, auth, sanitizer, or `/hubs/ops`.
- Did not treat hardcoded `/api/health` “not live Manager / no live TLS” captions as a live-connection test. Those are **strings**. Live MT5 / live FIX remain **not proven** (C42 / C43).
- Did not claim first-useful §69.12 is met because weatherforecast is gone.

**Bottom line:** `TraderIntelligence.Api` is a 15-endpoint anonymous demo host on `:5000`. **Confirm: no weatherforecast route.**
