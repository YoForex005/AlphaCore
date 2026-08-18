# D53 — Any auth on the API? **No.**

| Field | Value |
|---|---|
| Agent | D53 (senior engineer, inbound dashboard-API auth / RBAC re-measure) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:39:07+05:30 |
| Assigned | Any auth on API? Write this file. Do **not** modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D53_rbac.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| API launched | **No.** Route list is from `MapGet` / `MapPost` as read. 401/403 were **not** HTTP-probed. |
| Law | Architecture v2 **§59** (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2200–2224). Supporting: §§40, 45, 55, 68, 69, 72.19. Specs A51 / A63 / A26 remain design-only. |
| Relates | C18 (RBAC MISSING — still true), D06 (15 anonymous maps, no weatherforecast), C22 (CORS `*`), C04 (no secret sanitizer), C38 (Audit page stub), A51 (role matrix, not code) |
| Supersedes | C18 **file hashes** for `Program.cs` (now D06 `61B1E0D1…`) and `appsettings.json` (grew; see §2). Does **not** supersede C18’s verdict. Does **not** implement anything. |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (the asked question)

**No. `apps/api` has no authentication and no RBAC.**

Every live HTTP map is anonymous. There is no JWT, cookie, API-key middleware, Basic auth, Windows auth, Identity store, role claim, authorization policy, login route, or 401/403 path. An unauthenticated caller who can reach `:5000` can read the whole demo dashboard surface and `POST /api/ops/resync`.

| Control | Required by | Status |
|---|---|---|
| Inbound auth on `/api/**` | §59, A51 §11, A63 §1 | **MISSING** |
| Roles `SuperAdmin` / `RiskManager` / `Analyst` / `ReadOnly` | §59 | **MISSING** — zero C# / TS identifiers in product source |
| `AddAuthentication` / `UseAuthentication` | ASP.NET | **0** hits in `Program.cs` and in Debug `TraderIntelligence.Api.dll` |
| `AddAuthorization` / `UseAuthorization` / `RequireAuthorization` / `[Authorize]` | A51 §12 | **0** hits in product `.cs` |
| JWT / cookie / Identity package | A51 §11 | **MISSING** — no `Microsoft.AspNetCore.Authentication*`, no `JwtBearer`, no Identity on any product `.csproj` |
| `POST /api/v1/auth/login` (+ logout / refresh / me) | A63 §4 | **MISSING** — no `/api/v1` prefix at all |
| Fallback deny policy | A51 “default deny” | **MISSING** — minimal APIs default to anonymous |
| React Bearer / Login / `RequireRole` | A26 / A62 | **MISSING** |
| `users` / `user_sessions` / `step_up_challenges` | A51 §6 | **MISSING** |
| `UserRole` enum | A51 | **MISSING** (`src/Domain/Enums` has 15 files; none is a dashboard role) |
| Seed SuperAdmin | A51 §6.1 | **MISSING** |
| Audit writer for mutations | §59, §72.19 | **MISSING** (`AuditLog` is an unused stub) |
| Public self-registration | A51 | correctly **absent** |

Honest one-liner: **the dashboard API is an open demo BFF. Specs describe four roles. Code enforces none.**

Do **not** mark auth “done” because:

- `AuditLog.Role` exists (free `string`, never written),
- `RiskEngine:EmergencyFlattenApiKey` exists in JSON (empty, **never read**),
- `Cors:AllowedOrigins` exists in JSON (**not bound**; live policy is `AllowAnyOrigin`),
- `Mt5BrokerOptions.ApiKey` exists (unused options slot),
- C++ `X-API-Key` exists in `mt5-sdk` (**outbound** to a remote Manager bridge, not inbound on `apps/api`),
- A51 / A63 text exists (design, not code).

---

## 1. Method

| Source | Action |
|---|---|
| API host | Full read of `D:\Prop\apps\api\Program.cs` (95 lines / 86 non-blank / LF) |
| API project + config | `TraderIntelligence.Api.csproj`, both `appsettings*.json`, `launchSettings.json`, `TraderIntelligence.Api.http` |
| Identity / EF | `src/Domain/Entities`, `src/Domain/Enums`, `TraderDbContext`, `DependencyInjection`, `DemoSeeder` |
| React | `apps/web/src/{App,api,layouts,pages}` |
| Workers / compose | `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`, `docker-compose.yml` |
| Adjacent lookalikes | `Mt5BrokerOptions.ApiKey`; `mt5-sdk/src/core/mt5_http_client.cpp` `X-API-Key` |
| Tests | `tests/Unit` + `tests/Integration` authored `*.cs` (no `*Auth*` / `*Rbac*`) |
| Package walk | every product `*.csproj` under `D:\Prop` excluding `bin`/`obj`/`vendor`/`_tmp_`/`node_modules` |
| Filename walk | `User*.cs`, `*Role*.cs`, `*Auth*.cs`, `*Rbac*.cs`, `RequireRole*`, `can.ts`, `AuthProvider*` under `src`/`apps`/`tests` → **0 files** |
| Token counts | `AddAuthentication`, `AddAuthorization`, `UseAuthentication`, `UseAuthorization`, `Authorize`, `JwtBearer`, `RequireAuthorization` in `Program.cs` → **0** |
| Debug DLL | ASCII + UTF-16 string scan of `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.dll` |
| Architecture §59 | lines 2200–2224 |

No `dotnet run`, no `curl`, no product edit.

---

## 2. File hashes (this measure)

| Path | Bytes | LastWrite UTC | SHA-256 |
|---|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | 4731 | 2026-08-18T08:05:15Z | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 2026-08-18T07:25:15Z | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 2026-08-18T07:50:38Z | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` |
| `D:\Prop\apps\api\appsettings.json` | 1254 | 2026-08-18T08:07:36Z | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` |
| `D:\Prop\apps\api\appsettings.Development.json` | 478 | 2026-08-18T08:07:35Z | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 2026-08-18T08:02:01Z | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` |
| `D:\Prop\src\Domain\Entities\AuditLog.cs` | 403 | 2026-08-18T07:39:03Z | `FD5CD6EC5D18D3A0350891A21EF0B8E5FD33323035FD88ED06A95356B8EA4BA6` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | 2026-08-18T07:42:48Z | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | 2026-08-18T07:44:18Z | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 5082 | 2026-08-18T08:04:59Z | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 2026-08-18T07:38:06Z | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 2026-08-18T07:50:38Z | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| `D:\Prop\apps\web\src\pages\AuditPage.tsx` | 324 | 2026-08-18T07:50:38Z | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` |
| `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.dll` | 32256 | 2026-08-18T07:52:26Z | `C03959FFCF1BBED89AC573CB47792CD5E4468B56373150FFF6AEDBF12DE5E6CD` |

### 2.1 Drift vs earlier same-day reports

| File | C18 / D06 | This file | Auth impact |
|---|---|---|---|
| `Program.cs` | C18: 4658 B `E914FA98…` → D06: 4731 B `61B1E0D1…` | **same as D06** | Still **0** auth tokens. Host grew honesty strings + `GetTraderDetailAsync`, not auth. |
| `appsettings.json` | D06: 431 B `8DCE4CBE…` | **1254 B `69D41CAD…`** | New unused slots: `Cors:AllowedOrigins`, `RiskEngine:EmergencyFlattenApiKey=""`, `FeatureFlags`, `CTraderFix`. **None are read by `Program.cs`.** Not auth. |
| `appsettings.Development.json` | D06: 127 B | **478 B `81B5E6DC…`** | Adds unused `Cors:AllowedOrigins`. |
| `DemoSeeder.cs` | C18: 4942 B `139D8F87…` | **5082 B `A6416491…`** | Still no user / audit seed. |
| Debug API DLL | D06: older than `Program.cs` | **still older** (07:52 vs 08:05) | Scan still **0** `Authorize` / `JwtBearer` / `SuperAdmin`. Contains `AllowAnyOrigin` (ASCII) and `/api/ops/resync` (UTF-16). |

`UserSecretsId` is still **absent** from the API `.csproj`.

---

## 3. What `Program.cs` actually does (complete host)

Composition, in order (`D:\Prop\apps\api\Program.cs`):

1. `WebApplication.CreateBuilder`
2. `AddTraderIntelligence` — EF + fake MT5 + ingest + dashboard queries. **No identity services.**
3. JSON enum converter
4. `AddEndpointsApiExplorer` + `AddSwaggerGen`
5. CORS **default** policy: `AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()`
6. `UseCors()` (first middleware)
7. `UseSwagger()` only when `IsDevelopment()` — **no** `UseSwaggerUI()`, **no** `UseHttpsRedirection()`, **no** `UseAuthentication()`, **no** `UseAuthorization()`, **no** `UseSerilog()`, **no** `AddSignalR` / `MapHub`
8. Fifteen anonymous maps
9. Startup `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`
10. `app.Run()`

`Program.cs` token counts (this hash):

| Token | Hits |
|---|---:|
| `AddAuthentication` / `AddAuthorization` / `UseAuthentication` / `UseAuthorization` | **0** |
| `Authorize` / `JwtBearer` / `RequireAuthorization` / `AllowAnonymous` | **0** |
| `MapGet` | **14** |
| `MapPost` | **1** |
| `MapPut` / `MapPatch` / `MapDelete` / `MapHub` / `MapControllers` / `MapGroup` | **0** |
| `/api/v1` | **0** |
| `AllowAnyOrigin` | **1** |
| `UseSwagger` | **1** |
| `UseHttpsRedirection` | **0** |

There is no `app.Map…().RequireAuthorization()`, no global `FallbackPolicy`, no header check, no `HttpContext.User` read.

### 3.1 Live maps — all anonymous

| # | Method | Path | Should be (A51 / A63) | What happens |
|---:|---|---|---|---|
| 1 | GET | `/health` | anonymous liveness | `{ status, utc }` — allowed-anonymous by spec |
| 2 | GET | `/ready` | optional anonymous | `{ ready: true, brokers }` — always ready |
| 3 | GET | `/api/health` | ReadOnly+ | hardcoded demo inventory |
| 4 | GET | `/api/overview` | ReadOnly+ | `GetOverviewAsync` |
| 5 | GET | `/api/brokers` | ReadOnly+ | `GetBrokersAsync` |
| 6 | GET | `/api/groups` | ReadOnly+ | `GetGroupsAsync` |
| 7 | GET | `/api/traders` | ReadOnly+ | `GetTradersAsync` |
| 8 | GET | `/api/traders/{broker}/{login}` | ReadOnly+ | `GetTraderDetailAsync` |
| 9 | GET | `/api/trades` | ReadOnly+ | raw EF `ReconstructedTrade` (last 200) |
| 10 | GET | `/api/fix/sessions` | ReadOnly+ | `GetFixSessionsAsync` |
| 11 | GET | `/api/risk` | ReadOnly+ | `GetRiskAsync` |
| 12 | GET | `/api/risk/status` | ReadOnly+ | alias of `/api/risk` |
| 13 | GET | `/api/reconciliation/status` | ReadOnly+ | hardcoded zeros |
| 14 | GET | `/api/settings` | ReadOnly+ | hardcoded flags + broker names |
| 15 | POST | `/api/ops/resync` | **must not be anonymous** | Fake ingest + score rebuild. **No actor. No audit. Not in §59. Not in A63.** |

Unauthenticated `/api/**` is required to be **401**. Measured host: every map is anonymous (expected **200** if the process is up). That 200 was **not** captured this pass.

`GET /weatherforecast` is **not** mapped (D06). Irrelevant to auth.

Development OpenAPI JSON (`/swagger/v1/swagger.json`) is also ungated. `UseSwaggerUI()` is absent, so `launchUrl: swagger` is a likely **404** (C22). Production does not map Swagger (good vs A63 prod-UI rule; not an auth control).

---

## 4. Packages and launch — no auth stack

`TraderIntelligence.Api.csproj` packages:

| Package | Version | Auth? |
|---|---|---|
| `Microsoft.AspNetCore.SignalR.Common` | 8.0.4 | No (and no hub is mapped) |
| `Serilog.AspNetCore` | 8.0.2 | No (`UseSerilog` not called) |
| `Swashbuckle.AspNetCore` | 6.6.2 | No |

Shared framework `Microsoft.AspNetCore.App` **contains** authentication types. The host **never registers** them. Presence of the shared framework is not a control.

Product-tree `.csproj` walk (exclude `bin`/`obj`/`vendor`/`_tmp_`/`node_modules`): **0** `Authentication` / `JwtBearer` / `Identity` / `OpenIddict` / `Duende` package references.

`Properties/launchSettings.json`:

```text
windowsAuthentication: false
anonymousAuthentication: true
```

All three profiles (`http` `:5000`, `https` `:7294`, IIS Express) launch Swagger, not a login page.

`TraderIntelligence.Api.http` samples seven **unauthenticated** GETs. No `Authorization` header.

`docker-compose.yml` publishes the API as `http://0.0.0.0:5000` with `ASPNETCORE_ENVIRONMENT=Development`. No JWT / cookie / API-key env. Combined with `AllowAnyOrigin` + `AllowedHosts: "*"`, the container is an open LAN surface.

Workers (`mt5-worker`, `fix-worker`) are generic hosts. They do not mint dashboard identities and do not write `audit_logs`.

---

## 5. Config that looks like auth — and is not

### 5.1 `RiskEngine:EmergencyFlattenApiKey`

`appsettings.json` (1254 B, new vs D06):

```json
"RiskEngine": {
  "EmergencyFlattenApiKey": ""
}
```

Grep of product `*.cs`: **`EmergencyFlattenApiKey` is never read.** It is not in `Program.cs`. It is not in the Debug API DLL. There is no flatten endpoint. An empty unused key is **not** a control.

### 5.2 `Cors:AllowedOrigins`

Both appsettings files list `http://localhost:5173`. `Program.cs` **does not bind** that section. Live policy is `AllowAnyOrigin()`. Vite actually listens on **`:3000`** (`apps/web/vite.config.ts`). The JSON origin list is dead config.

### 5.3 `Mt5BrokerOptions.ApiKey`

`D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` line 44: `public string? ApiKey { get; set; }`. The type is **not** bound by `AddTraderIntelligence`. Fake connectors do not send it. Invented vs architecture §56 (A58 / D04). **Not inbound API auth.**

### 5.4 C++ `X-API-Key` (different process, outbound)

`D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp` lines 230 and 799 attach `X-API-Key: ` + `apiKey` on **outgoing** curl calls to a remote MT5 HTTP bridge (`/mt5/events/stream`, etc.). That is a **client** of some other service. It is **not** middleware on `TraderIntelligence.Api`. Do not cite it as “the API has an API key.”

### 5.5 FIX Logon / MT5 Manager password

Venue credentials (cTrader FIX tag 96, MT5 manager password) are **not** dashboard identity. They are also not implemented as live sockets on this host (C42 / C43 / D32). Out of this question.

---

## 6. Domain / EF — no identity, stub audit

`src/Domain/Entities`: 21 files. **No `User`.**  
`src/Domain/Enums`: 15 files. **No `UserRole`.**

`TraderDbContext` `DbSet`s (18 tables): brokers, groups, accounts, deals, positions, reconstructed trades, instruments, mappings, scores, history, outbox, checkpoints, copy intents, risk decisions, execution intents, shadow orders, destination quotes, fix sessions, **`audit_logs`**, kill switches. **No `users`, `user_sessions`, `step_up_challenges`.** No EF `Migrations/` directory. Schema is `EnsureCreated` only.

`AuditLog` (entire type):

```csharp
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset At { get; set; }
}
```

Mapped to `audit_logs` with a PK only. `Role` is an unconstrained string, not §59. Grep of product `*.cs` for `new AuditLog` / `AuditLogs.Add` / `AuditLogs.AddRange`: **0 writers** (only the `DbSet` declaration). `IDashboardQueries` has **no** audit query. There is no `GET /api/audit`.

`KillSwitch.SetBy` is a free string. Seeded as `"system"`. There is no stop-new / flatten HTTP write.

`AddTraderIntelligence` registers EF, two fake brokers, ingest, scoring, dashboard queries. It does **not** register authentication.

`DemoSeeder` (5082 B) returns early if any broker exists. It seeds brokers / FIX rows / kill switch. It does **not** insert users or audit rows.

---

## 7. React — no login, no token, all nav open

| File | Evidence |
|---|---|
| `apps/web/src/api/client.ts` | axios `baseURL` + `Content-Type` only. **No** `Authorization` interceptor, **no** `withCredentials`. |
| `apps/web/src/api/hooks.ts` | 10 GET hooks to unversioned `/api/*`. Zero POST/PATCH. Zero `/auth/me`. |
| `apps/web/src/api/signalr.ts` | `/hubs/dashboard` with **no** `access_token`. Hub is not mapped on the API (C28). |
| `apps/web/src/App.tsx` | Every route inside `DashboardLayout`. **No** `/login`. Index → `/overview`. |
| `apps/web/src/layouts/DashboardLayout.tsx` | 14 nav links including **Audit**. No role filter. |
| `apps/web/src/pages/AuditPage.tsx` | Static stub: *“RBAC is not enabled in the demo seed.”* True. |
| `apps/web/package.json` | React, router, query, axios, recharts, SignalR. **No** OIDC / JWT helper. |

UI hiding is not security. Here even the hide is absent. A51 §5.3: ReadOnly / Analyst must not see Audit. Anyone who opens the SPA sees it.

---

## 8. §59 privileged mutations — 0 / 8 endpoints

Architecture §59 (verbatim list this measure is scored against):

```text
enable real execution
change risk limits
pause/resume trader copying
change symbol mapping
activate stop-new-orders
request emergency flatten
promote a model
change broker/FIX configuration
```

All of those **must** be role-gated **and** audited.

| §59 action | A51 code | Specified route | Product |
|---|---|---|---|
| Enable real execution | `execution.enable` | `POST /api/v1/execution/enable` | **MISSING.** `GET /api/settings` hardcodes the flag `false`. |
| Change risk limits | `risk.limits.write` | `PUT /api/v1/risk/limits` | **MISSING.** |
| Pause / resume copy | `trader.copy.pause_resume` | `PATCH .../state` | **MISSING.** |
| Change symbol mapping | `mapping.symbol.write` | mapping write | **MISSING.** |
| Activate stop-new | `risk.stop_new.activate` | `POST .../stop-new-execution` | **MISSING.** |
| Emergency flatten | `risk.flatten.request` | `POST .../emergency-flatten` | **MISSING.** Unused empty `EmergencyFlattenApiKey` is not this. |
| Promote a model | `model.promote` | `POST /models/{id}/promote` | **MISSING** (correctly out of v1; no 404 stub). |
| Broker / FIX config | `config.broker_fix.write` | not via React | **MISSING.** |

The only write on the host is `POST /api/ops/resync`. It is **not** in §59. It is **not** authenticated. It is **not** audited. **UNSAFE** as an ops door.

First-useful derived writes (`group.analysis.toggle`, `trader.state.write`) are also **MISSING**.

---

## 9. Tests — 0 auth / RBAC cases

Authored test classes: reconstruction, scoring, risk engine, volume, symbols, sizing, quantity conversion, seeding/store, leftover `UnitTest1`. **Zero** `*Auth*` / `*Rbac*` files. A51 §15 inventory is still **0 / 12**.

A single `[Authorize]` would not be enough (A51 §15). Today there is not even that.

---

## 10. Classification (§73)

| Component | Class |
|---|---|
| Inbound authentication on `apps/api` | **MISSING** |
| RBAC policies / four §59 roles | **MISSING** |
| Auth HTTP surface (`/api/v1/auth/*`) | **MISSING** |
| React auth / role UI | **MISSING** |
| Identity tables | **MISSING** |
| RBAC / auth tests | **MISSING** |
| `AuditLog` entity + `DbSet` | **EXISTS_NEEDS_REFACTOR** — unused stub, not a control |
| `apps/api` HTTP host | **EXISTS_NEEDS_REFACTOR** as a demo BFF; **UNSAFE** if exposed beyond local demo |
| `POST /api/ops/resync` | **UNSAFE** (anonymous mutation) |
| CORS `AllowAnyOrigin` + `AllowedHosts: *` | **UNSAFE** for anything that is not disposable demo data |
| `EmergencyFlattenApiKey` / `Cors:AllowedOrigins` JSON | **DEAD CONFIG** — not wired |
| C++ outbound `X-API-Key` | **N/A** to this API (different process) |
| Public register | correctly **absent** |
| A51 / A63 / A26 RBAC text | design only |

---

## 11. What would have to exist before anyone may say “the API has auth”

Minimum, matching A51 §14 + A63 (not implemented here):

1. `users` + argon2id (or equivalent) + exactly one of four roles; seed one SuperAdmin out of band; no public register.
2. `POST /api/v1/auth/login` + refresh + `GET /api/v1/auth/me`.
3. ASP.NET policies on **every** `/api/**` except login / `/health` (and optionally `/ready`). Unauthenticated → **401**. Wrong role → **403**.
4. First-useful writes persist + insert `audit_logs` in the **same** transaction.
5. React Login + Bearer or cookie BFF. UI hide is extra; API remains authority.
6. A51 §15 tests green.

Until then the honest label is **MISSING**.

---

## 12. What this document does **not** do

- Does not modify `apps/api`, Domain, Infrastructure, React, workers, tests, or compose.
- Does not implement JWT vs cookie BFF.
- Does not resolve A26 vs A51 flatten-role conflict (spec-only; neither is in code).
- Does not treat `AuditLog.cs`, the Audit page stub, `KillSwitch.SetBy`, or an empty `EmergencyFlattenApiKey` as a control.
- Did not `curl` `:5000`. Anonymous **200** is inferred from the absence of any auth middleware, not observed.

**Bottom line:** **There is no auth on the API.** §59 RBAC is **not implemented**. The live surface is 14 anonymous GETs plus one anonymous `POST /api/ops/resync`, CORS `*`, `AllowedHosts: *`, no `/api/v1`, no login, no roles, no audit writer. Confirm **MISSING**.
