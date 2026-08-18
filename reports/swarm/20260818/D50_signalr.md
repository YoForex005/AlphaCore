# D50 — API `MapHub`: is a SignalR hub mapped?

| Field | Value |
|---|---|
| Agent | D50 (senior engineer, SignalR hub-map verify only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:00+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\D50_signalr.md` |
| Ask | **API map hub?** Confirm whether `apps/api` calls `MapHub` (or otherwise exposes `/hubs/ops` / `/hubs/dashboard`). Write this report. |
| Product source modified | **No.** Report / index / swarm-log only. |
| Method | Full re-read of `D:\Prop\apps\api\Program.cs` (4731 B, SHA `61B1E0D1…`, same blob as D06). Token counts. `Get-ChildItem` + SHA-256 of API + web SignalR client + workers. Product-tree grep of `apps/` + `src/` + `tests/` (`*.cs` / `*.csproj`, exclude `bin`/`obj`/`node_modules`/`_tmp_`/`vendor`) for `AddSignalR`, `MapHub`, `UseSignalR`, `IHubContext`, `: Hub`, `class OpsHub`. DLL ASCII + UTF-16 string scan of `TraderIntelligence.Api.dll`. Folders `Hubs/` / `Realtime/` probed. Cross-read A26 §7, A63 §6, A97 (binding event contract), C28 (earlier gap), D06 (host census). **API process was not launched.** No negotiate / WebSocket probe. |
| Law | Architecture v2 §5 (React + ASP.NET Core + SignalR), §55 / §72.5 (no secrets on the wire). A26 §7, A63 §6, A97 (binding hub contract; **wins for SignalR events**). A51 `OpsHub` = `ReadOnlyPlus`. Hub is **recommended**, not a §69 first-useful gate. |
| Relates | C28 (package yes / hub no — **Program.cs hash is stale**; conclusion is not), D06 (15-map host; no `MapHub`; **tree is stale** — `Controllers/` appeared after D06), A97, A41 / A99 (Redis relay names), C22 (CORS `*`), C27 (Redis unused at C27 time), C58 (outbox not drained) |
| Supersedes | C28 **hashes and “6 product files / no Controllers”** for this question. **Does not** supersede A97 as the hub-event contract, A63 as the REST catalog, or D06’s weatherforecast-gone result. |
| Classification | architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE` |

---

## 0. Headline answer (measured)

**No. The API does not map a SignalR hub.**

There is no `AddSignalR()`, no `MapHub<T>()`, no `UseWebSockets()`, no `Hub` subclass, no `IHubContext`, no `OpsHub`, no `/hubs/ops`, and no `/hubs/dashboard` on the server. A running process would expose those paths as **unmapped 404s**. That 404 was **not** observed live — the process was not started. The conclusion is from the route table and the compiled Debug DLL as read.

| Question | Answer | Class | Evidence |
|---|---|---|---|
| Does `Program.cs` call `MapHub`? | **No. 0 hits.** | `MISSING` | §3 |
| Does it call `AddSignalR`? | **No. 0 hits.** | `MISSING` | §3 |
| Binding hub `OpsHub` at `/hubs/ops`? | **MISSING** | `MISSING` | A26 §7, A63 §6, A97 §2.1 |
| Stub path `/hubs/dashboard` on the server? | **Not mapped** (client still targets it) | client `EXISTS_NEEDS_REFACTOR` | §6 |
| SignalR **NuGet** on the API? | **Yes** — unused `Microsoft.AspNetCore.SignalR.Common` **8.0.4** | `EXISTS_NEEDS_REFACTOR` | §4 |
| Is Common a hub **host**? | **No.** Host APIs live in the Web SDK shared framework `Microsoft.AspNetCore.App`, already implied and unused. | wrong package | §4 |
| `OpsHub` / `hubs/ops` / `MapHub` in Debug DLL? | **0** (ASCII and UTF-16) | `MISSING` | §3.2 |
| Workers host SignalR? | **No.** Generic `Host` workers. Correct per A97. | `EXISTS_AND_GOOD` (absence) | §7 |
| Live tiles / negotiate 200? | **Not implemented. Do not claim.** | `MISSING` | this file |

Honest one-liner: **the API still ships a dead `SignalR.Common` 8.0.4 reference and maps no hub. The React shell still dials `/hubs/dashboard` and swallows the failure. REST polling is the only live path.**

Do not treat the package, the web stub, A97, or the new unmapped `SettingsController` as an implemented hub.

---

## 1. Files hashed (this pass)

| Path | Bytes | SHA-256 | LastWriteUtc |
|---|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `2026-08-18T08:05:15.0457194Z` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | `2026-08-18T07:25:15.5522783Z` |
| `D:\Prop\apps\api\appsettings.json` | 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | `2026-08-18T08:07:36.8795807Z` |
| `D:\Prop\apps\api\appsettings.Development.json` | 478 | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | `2026-08-18T08:07:35.0172143Z` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | `2026-08-18T08:02:01.4042458Z` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | `2026-08-18T07:50:38.2749244Z` |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | 3732 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | `2026-08-18T08:07:39.5181186Z` |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | `2026-08-18T07:38:02.8638868Z` |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | `2026-08-18T07:50:38.3009629Z` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | `2026-08-18T07:46:00.6845376Z` |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | `2026-08-18T07:38:06.3731432Z` |
| `D:\Prop\apps\web\package.json` | 739 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` | `2026-08-18T07:36:29.6945552Z` |
| `D:\Prop\apps\web\vite.config.ts` | 169 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | `2026-08-18T07:36:19.1714380Z` |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | `2026-08-18T07:45:01.3618241Z` |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `2026-08-18T07:45:01.3638263Z` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `2026-08-18T07:44:18.2038981Z` |
| `D:\Prop\src\Domain\Enums\OutboxEventType.cs` | 211 | `163ED842EE9AF0C94EA912A91845F31C8644F2A1A373A67C77E7FA16154BAADA` | `2026-08-18T07:34:07.2199822Z` |

`Program.cs` line endings: **LF only** (95 LF, 0 CR). 95 physical lines / 86 non-blank. **Same SHA as D06.** Hub conclusion is therefore on the same composition root D06 already token-counted.

### 1.1 Drift vs C28 (same day, earlier snapshot)

| File | C28 | D50 | Meaning |
|---|---|---|---|
| `Program.cs` | 4658 B / `E914FA98…` | 4731 B / `61B1E0D1…` | Host grew (D06: QUOTE health flipped to `false`). **Still 0 `MapHub`.** |
| `.csproj` | `A5868FA8…` | **same** | Common 8.0.4 pin unchanged |
| `signalr.ts` / `DashboardLayout` / `package.json` | same | same | Client stub unchanged |
| `appsettings.json` | 431 B / `8DCE4CBE…` | 1254 B / `69D41CAD…` | Config dump landed **after** D06. Not a hub. |
| `Controllers/SettingsController.cs` | **absent** | 3732 B / `B19274DC…` | New MVC type. **Not compiled into the Debug DLL. Not mapped.** See §5. |
| Worker `Program.cs` | same | same | Still no `MapHub` |

### 1.2 Folders that must exist for a host (all absent)

| Path | `Test-Path` |
|---|---|
| `D:\Prop\apps\api\Hubs` | **False** |
| `D:\Prop\apps\api\Realtime` | **False** |
| `D:\Prop\src\Application\Realtime` | **False** |
| `D:\Prop\apps\web\src\hubs` | **False** |

Authored `.cs` under `apps/api` (exclude `bin`/`obj`): **2** — `Program.cs` + `Controllers\SettingsController.cs`. Neither is a `Hub`.

---

## 2. Binding contract (what “map a hub” means)

A97 wins **for SignalR only**. REST paths stay with A26 / A63. First useful React **may poll** REST until the hub exists (A06 §4.14, A63 §6, A97). The hub is **not** a §69 gate.

| Item | Binding | Must be this, not that |
|---|---|---|
| Class | `OpsHub : Hub` | Not `ScoresHub` / `FixHub` / `QuotesHub` / `AlertsHub` (forbidden in v1) |
| Map | `app.MapHub<OpsHub>("/hubs/ops")` | **Do not** map `/hubs/dashboard` |
| DI | `builder.Services.AddSignalR()` | Web SDK shared framework. Do **not** add a second host PackageReference on top of Common |
| Protocol | JSON. MessagePack **off** | A97 §2.1 |
| Auth | JWT `Authorization` or negotiate `access_token`. Role in {ReadOnly, Analyst, RiskManager, SuperAdmin} | Anonymous negotiate is **401** |
| Host | `apps/api` only | Workers **never** `MapHub` |
| Client methods | `Subscribe`, `Unsubscribe`, `WatchTrader`, `UnwatchTrader` only | `SendOrder` / `SetPassword` / flatten **forbidden** |
| Produce | persist Postgres → outbox / last-value → Redis pub/sub → API relay → sanitizer → `Clients.Group` | Never MT5 callback → `IHubContext`. Never raw FIX → hub |

v1 topics that must ship with the hub: `header`, `scores`, `fix`, `quotes`, `alerts`. Event names (A97 §18): `ops.header`, `trader.score`, `trader.score.batch`, `trader.state`, `scoring.summary`, `fix.session`, `fix.health`, `quote.xauusd`, `quote.stale`, `alert.raised`, `alert.cleared`, `alert.snapshot`, `hub.error`.

Redis last-value / notify names (A99 wins on **key spelling**; A41’s `ops:events` is the same family):

| A97 / A41 sketch | A99 implement |
|---|---|
| `ops:events` | **`ti:ops:events`** only |
| `ops:last:{topic}` | `ti:ops:last:{topic}` family in A99 |

Do not re-litigate the hub path. Do not invent a second hub to “match the web stub.”

Suggested composition (coding task later — **not created from this file**):

```text
apps/api/Hubs/OpsHub.cs
apps/api/Realtime/OpsEventRelay.cs
apps/api/Realtime/HubPayloadSanitizer.cs
src/Application/Realtime/OpsEventEnvelope.cs
src/Application/Realtime/IOpsEventPublisher.cs
```

---

## 3. No hub mapped (confirmed)

`D:\Prop\apps\api\Program.cs` composition, in order:

1. `AddTraderIntelligence(builder.Configuration)` — EF / Fake MT5 / dashboard queries / ingestion. **No** SignalR, **no** Redis multiplexer (`DependencyInjection.cs` SHA `EF0E0E46…`).
2. `ConfigureHttpJsonOptions` + `JsonStringEnumConverter`.
3. `AddEndpointsApiExplorer` + `AddSwaggerGen`.
4. CORS default: `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`.
5. `UseCors()`. Development: `UseSwagger()` only (no UI).
6. Fourteen `MapGet` + one `MapPost` (table §3.1).
7. Startup scope: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`.
8. `app.Run()`.

**Token counts in this file (case-sensitive, SHA `61B1E0D1…`):**

| Token | Hits |
|---|---:|
| `AddSignalR` | **0** |
| `MapHub` | **0** |
| `UseSignalR` | **0** |
| `UseWebSockets` | **0** |
| `IHubContext` | **0** |
| `Hub` | **0** |
| `SignalR` | **0** |
| `AddControllers` | **0** |
| `MapControllers` | **0** |
| `AddAuthentication` / `AddAuthorization` | **0** |
| `MapGet` | **14** |
| `MapPost` | **1** |
| `MapPut` | **0** |
| `weatherforecast` | **0** |
| `AllowAnyOrigin` | **1** |

Repo-wide product `*.cs` / `*.csproj` (exclude `bin`/`obj`/`node_modules`/`_tmp_`/`vendor`) for `AddSignalR|MapHub|UseSignalR|IHubContext|class OpsHub|: Hub` → **zero matches**. The only product-source `hubs/` string is the **web client** URL in `signalr.ts`.

### 3.1 Live maps (complete). None is a hub.

| # | Method | Path |
|---|---|---|
| 1 | `GET` | `/health` |
| 2 | `GET` | `/api/health` |
| 3 | `GET` | `/api/risk/status` |
| 4 | `GET` | `/api/reconciliation/status` |
| 5 | `GET` | `/api/settings` |
| 6 | `GET` | `/ready` |
| 7 | `GET` | `/api/overview` |
| 8 | `GET` | `/api/brokers` |
| 9 | `GET` | `/api/groups` |
| 10 | `GET` | `/api/traders` |
| 11 | `GET` | `/api/traders/{broker}/{login}` |
| 12 | `GET` | `/api/fix/sessions` |
| 13 | `GET` | `/api/risk` |
| 14 | `GET` | `/api/trades` |
| 15 | `POST` | `/api/ops/resync` |

Framework-adjacent: Development `UseSwagger()` → `/swagger/v1/swagger.json`. Not a hub.

Expected HTTP if anyone still probes a running process (not executed this pass):

| Path | Expected |
|---|---|
| `POST /hubs/ops/negotiate` | **404** unmapped |
| `POST /hubs/dashboard/negotiate` | **404** unmapped |
| WebSocket `/hubs/ops` | **404** |
| `GET /weatherforecast` | **404** (D06) |

`.http` samples 7 REST paths on `:5000`. It does **not** sample a hub.

### 3.2 Debug DLL string scan

`D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.dll` — 32 256 bytes, LastWriteUtc `2026-08-18T07:52:26.2473946Z` (**older than** `Program.cs` `08:05:15Z` and **older than** `SettingsController` `08:07:39Z`).

| String | ASCII | UTF-16 |
|---|---|---|
| `OpsHub` | **false** | **false** |
| `MapHub` | **false** | **false** |
| `AddSignalR` | **false** | **false** |
| `hubs/ops` | **false** | **false** |
| `hubs/dashboard` | **false** | **false** |
| `weatherforecast` | **false** | **false** |
| `SettingsController` | **false** | **false** |
| `IConnectionMultiplexer` | **false** | **false** |

The last successful Debug compile predates both the QUOTE-health edit and the Settings controller. It still contains **no** hub symbols. Do not treat this DLL as proof the controller is live; treat it as proof **no hub was ever compiled in**.

---

## 4. SignalR package — present, unused, not a host

Exact csproj (`D:\Prop\apps\api\TraderIntelligence.Api.csproj`, SHA `A5868FA8…`):

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="8.0.4" />
```

| Check | Result |
|---|---|
| Direct `PackageReference` | **Yes** — `Microsoft.AspNetCore.SignalR.Common` 8.0.4 |
| Other SignalR PackageReference in the solution | **None.** This is the only product csproj line. |
| `Microsoft.AspNetCore.SignalR` (legacy host package) | **Not referenced.** Not needed on `Microsoft.NET.Sdk.Web`. |
| Restore / runtime copy | `bin\Debug\net8.0\Microsoft.AspNetCore.SignalR.Common.dll` (42 672 B). `TraderIntelligence.Api.deps.json` lists Common 8.0.4 as a **direct** dependency. |
| Used by product C# | **Zero** `using Microsoft.AspNetCore.SignalR`. Dead reference. |
| Central pin (A102) | **Not applied.** `Directory.Build.props` has no SignalR `PackageVersion`. |

| Package / framework | What it is | What this repo has |
|---|---|---|
| `Microsoft.NET.Sdk.Web` → `Microsoft.AspNetCore.App` | Contains `Hub`, `AddSignalR()`, `MapHub<T>()`. **This is the host.** | Already implied. **Unused** for SignalR. |
| `Microsoft.AspNetCore.SignalR.Common` 8.0.4 | Protocol / shared types. **Not** a hub host. | **Referenced, unused.** |
| `Microsoft.AspNetCore.SignalR.Client` | .NET client | **Not** on the API |
| npm `@microsoft/signalr` | Browser client | `^8.0.0` → lockfile **8.0.29** |
| `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` | Binary protocol | **Not** referenced. A97 v1 forbids it. |

A package on the csproj is not `MapHub`.

---

## 5. Adjacent: unmapped `SettingsController` is not a hub

D06 §1 / §5.2 (`Controllers/` **False**) is **stale**. After D06’s `Program.cs` hash, this file appeared:

`D:\Prop\apps\api\Controllers\SettingsController.cs` (3732 B, SHA `B19274DC…`, 08:07:39Z).

| Fact | Measured |
|---|---|
| Base class | `ControllerBase` (`[ApiController]`, `[Route("api/settings")]`) |
| SignalR types | **None** |
| `AddControllers` / `MapControllers` | **0** in `Program.cs` |
| In Debug DLL | **No** (DLL older than the file) |
| DI | Constructor requires `IConnectionMultiplexer` — **not registered** in `AddTraderIntelligence` |
| Collision | `Program.cs` already `MapGet("/api/settings")` with a **hardcoded** anonymous object |

If a later wave maps controllers without deleting the minimal-API GET, `/api/settings` becomes a dual route. That is a REST problem, not a hub. Recorded so “API grew a controller” is not misread as “API grew `OpsHub`.”

C27 “0 `using StackExchange.Redis` in product `.cs`” is **stale** as of this file. The new using does **not** register a multiplexer and does **not** publish `ti:ops:events`.

`appsettings.json` also grew after D06 (1254 B, Redis + `CTraderFix` + empty `Password=` / `EmergencyFlattenApiKey`). `Program.cs` still does not bind that section onto any SignalR frame. Secret-sanitizer remains **safe-by-absence** on the live maps (C04), not fail-closed.

---

## 6. Web client — package yes, path wrong, no consumers

| Item | Measured |
|---|---|
| npm | `@microsoft/signalr` `^8.0.0`; lockfile **8.0.29** |
| File | `D:\Prop\apps\web\src\api\signalr.ts` (899 B, SHA `AB913FF7…`) |
| URL | `` `${VITE_API_URL \|\| 'http://localhost:5000'}/hubs/dashboard` `` |
| Binding | **`/hubs/ops`**. Stub path is **forbidden** (A63 §6: “Do not implement `/hubs/dashboard`”). |
| Auth | **None.** No `accessTokenFactory`, no `Authorization`. |
| Vite proxy | **None** (`vite.config.ts` is `server.port = 3000` only). Browser talks to `:5000` cross-origin. |
| Start | `DashboardLayout` `useEffect` → `startConnection()`. Failure is `console.warn` and swallowed. |
| `onEvent` | Defined. **Zero** importers besides its own file. No page invalidates TanStack Query from the hub. |
| REST fallback | `hooks.ts` polls `/api/overview`, `/api/fix/sessions` (5s), `/api/risk` (5s), `/api/health` (10s). This is how the shell “lives” today. |

Until the API maps **`/hubs/ops`** and the client is retargeted, the shell connection is a guaranteed fail hidden from the UI.

---

## 7. Workers do not host SignalR (correct)

`apps/mt5-worker` and `apps/fix-worker` are `Host.CreateApplicationBuilder` generic hosts. They register `AddTraderIntelligence` + one `BackgroundService`. They are **not** Web hosts.

| Check | mt5-worker | fix-worker |
|---|---|---|
| SignalR package | **No** | **No** |
| `MapHub` / `AddSignalR` | **0** | **0** |
| What they do | 30s Fake ingest + score (logins 10001–10003, 99001) | 15s stamp `FixSessionStatus.Disconnected` + “no live socket” |
| Publish `ti:ops:events` | **No** | **No** |

A97 §2.1: workers publish to Redis / `system_events`; they **never** `MapHub`. Current absence matches the contract.

`OutboxEventType.NotificationEvent = 4` exists on disk. C58: nothing inserts or drains outbox rows. Grep of `src/` for `system_events` / `SystemEvent` → **0**. There is no produce path even if `MapHub` were added tomorrow.

`docker-compose.yml` runs the API on `:5000` and depends on Redis. It does not map a hub, does not set SignalR env, and does not run workers.

---

## 8. Gap vs binding contract

| Contract item | Binding | Today | Class |
|---|---|---|---|
| `class OpsHub : Hub` | A97 §2.1 | no type | `MISSING` |
| `builder.Services.AddSignalR()` | A30 I6 / A97 | not called | `MISSING` |
| `app.MapHub<OpsHub>("/hubs/ops")` | A26 §7 | not called | `MISSING` |
| JWT on negotiate | A51 / A97 | no auth on host at all | `MISSING` |
| JSON protocol, MessagePack off | A97 | N/A (no hub) | — |
| Events in A97 §18 | A97 / A63 §6.1 | none | `MISSING` |
| Mutations over hub | **Forbidden** | none exist | `EXISTS_AND_GOOD` (absence) |
| Payload sanitizer | A26 §3 / A76 / A97 §10 | none | `MISSING` |
| Redis `ti:ops:events` subscriber in API | A41 / A99 / A97 | none (and no multiplexer in DI) | `MISSING` |
| `system_events` persist | A20 / A97 | no entity / no table in `TraderDbContext` | `MISSING` |
| Client `/hubs/ops` | A63 §6 | `/hubs/dashboard` | `EXISTS_NEEDS_REFACTOR` |
| `SignalR.Common` as proof of feature | — | dead PackageReference | `EXISTS_NEEDS_REFACTOR` |
| CORS for a future hub | A51: explicit Vite origin; no `*` with credentials | `AllowAnyOrigin` (C22 `UNSAFE`); `appsettings` `Cors:AllowedOrigins` is `http://localhost:5173` **unread** (Vite is `:3000`) | `UNSAFE` for prod; demo GET works |
| Workers without `MapHub` | A97 | none | `EXISTS_AND_GOOD` |

§69 first-useful paint may poll REST. The hub is **not** a go-live gate. It is still a real gap for live header / quote / score tiles.

---

## 9. Findings

| ID | Sev | Finding |
|---|---|---|
| D50-01 | **PASS (gap confirm)** | **No hub is mapped.** Zero `AddSignalR`, zero `MapHub`, zero `Hub` subclass, zero `/hubs/ops`, zero `/hubs/dashboard`. Debug DLL has none of those strings. |
| D50-02 | **INFO** | API **has** `Microsoft.AspNetCore.SignalR.Common` 8.0.4. Restored. Copied to `bin`. Unused. |
| D50-03 | **MED** | Common is the **wrong** package to treat as a hub host. Web SDK already includes host types. Do not add a redundant `Microsoft.AspNetCore.SignalR` PackageReference. |
| D50-04 | **MED** | Web stub dials `/hubs/dashboard` (forbidden) and swallows start failure. `onEvent` has no consumers. Retarget to `/hubs/ops` in the same coding wave that maps the hub (A97 checklist #8). |
| D50-05 | **INFO** | Workers correctly do **not** host SignalR. |
| D50-06 | **INFO** | C28 `Program.cs` hash is stale (now `61B1E0D1…`, same as D06). SignalR measured state is **unchanged**. |
| D50-07 | **INFO** | D06 “no `Controllers/`” is stale. `SettingsController` exists, is **not** a hub, is **not** in the Debug DLL, and is **not** mapped. |
| D50-08 | **INFO** | Produce path is missing independently of `MapHub`: no Redis multiplexer, no `ti:ops:events` publisher, no `system_events`, outbox `NotificationEvent` is an unused enum (C58). |

---

## 10. What this file does **not** authorize

- Implementing `OpsHub` from this report (coding task; follow A97 + A99 key names, not this gap note).
- Adding `Microsoft.AspNetCore.SignalR` as a redundant PackageReference.
- Mapping `/hubs/dashboard` “to match the stub.”
- Hosting a hub on either worker.
- Enabling MessagePack.
- Sending secrets / FIX passwords / `SenderSubId` / `CTraderFixOptions` on any future frame.
- Wiring `SettingsController` or claiming Redis is live because that file `using`s `StackExchange.Redis`.
- Claiming live tiles, negotiate 200, or “SignalR done” because Common is in the csproj.

---

## 11. Checklist (this pass)

- [x] Re-hashed `Program.cs` (matches D06 `61B1E0D1…`; drifted from C28).
- [x] Token count: `MapHub` **0**, `AddSignalR` **0**.
- [x] Product-tree grep: no hub type.
- [x] Debug DLL: no `OpsHub` / `hubs/ops` / `MapHub`.
- [x] Folders `Hubs/` / `Realtime/` absent.
- [x] Binding path `/hubs/ops` vs client `/hubs/dashboard` recorded.
- [x] Workers have no `MapHub`.
- [x] Package identified (Common 8.0.4, unused).
- [x] Adjacent `SettingsController` recorded as **not** a hub.
- [x] Product source **not** modified.

---

*End of D50. Product source was not modified. No secrets in this file.*
