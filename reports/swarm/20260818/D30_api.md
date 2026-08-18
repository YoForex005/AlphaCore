# D30 — `apps/api` endpoints and secrets (measured from `Program.cs`)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D30_api.md` |
| Agent | D30 (senior engineer, API host surface + secrets only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:35:15+05:30 (`Program.cs` last write) |
| Assigned | Read `apps/api/Program.cs`. List endpoints. Secrets? Write this file. Do not modify product source. |
| Primary SUT | `D:\Prop\apps\api\Program.cs` (**not** `D:\Prop\src\apps\api` — that path does not exist) |
| Product source modified | **No.** Report only. |
| Method | Full read of every non-`bin`/`obj` file under `D:\Prop\apps\api`. SHA-256 + byte + line census. Token grep of `MapGet` / `MapPost` / `Password` / `weatherforecast` / auth / Swagger. Cross-read `IDashboardQueries` + DTOs, `EfDashboardQueries`, `AddTraderIntelligence`, `ReconstructedTrade`, `Broker`, `FixSessionState`, `CTraderFixOptions`, `DemoSeeder`, `apps/web` client/hooks, `docker-compose.yml` `api` service, architecture §§52 / 55 / 72.5, A19 / A63 / A77 / B25 / C04 / C22 / C50. **API process was not launched.** No HTTP capture. Route list is from `MapGet` / `MapPost` as read. |
| Law | Architecture v2 §55 (never expose the password denylist to React), §52 (never show FIX password), §72.5 (never expose secrets to the browser), A63 §2 sanitizer, A77 probes. |
| Relates | A06 (stale: weatherforecast-only), B06 (stale host gap), C04 (same 15 maps; **stale** on `Program.cs` hash, trader-detail method, `/api/health` honesty strings, `launchSettings` `launchUrl`), C22 (CORS/Swagger), C34 (usings), C50 (`.http` coverage) |
| Supersedes | C04 host-state for `Program.cs` SHA / `/api/traders/{broker}/{login}` return type / `/api/health` body / `launchSettings.json` SHA. Does **not** reopen weatherforecast as live — that route stays **GONE**. |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Headline answers (measured)

| Question | Answer |
|---|---|
| Where is the host? | `D:\Prop\apps\api\Program.cs` (95 physical lines, **4731** bytes). Workspace `D:\Prop\src` has **no** `apps/api`. |
| How many live maps? | **15** = **14** `MapGet` + **1** `MapPost`. Zero `MapPut` / `MapDelete` / `MapPatch` / `MapHub` / `MapControllers`. |
| `weatherforecast` still live? | **GONE** from `Program.cs`, `.http`, and `launchSettings.json`. Unmapped path → expected **404**. |
| Secrets to the browser? | **No §55 secret is serialized by any live map.** Handlers never inject `IConfiguration`, never return `CTrader` options, never return `ConnectionStrings`, never return a password / proxy user / Redis AUTH / FIX `RawData` / `SenderSubId`. |
| Is that because of a sanitizer? | **No.** There is **no** allow-list serializer and **no** A63 §2.3 redaction middleware. Today is **safe by absence** of secret-bearing types on the wire, not by a fail-closed filter. |
| Live password on disk in this project? | **NONE.** `CTrader:Password` is `""`. `ConnectionStrings:TraderIntelligence` is `""`. No `.env`. No `UserSecretsId`. No user-secrets folder. |
| Identifier (not password) committed? | **YES — FLAG.** `CTrader:AccountId` = `"1369850"` in `appsettings.json`. Not returned by any map. |
| Auth / RBAC? | **MISSING.** Entire host is anonymous, including `POST /api/ops/resync`. |
| Classification of this host | **EXISTS_NEEDS_REFACTOR** as a demo BFF. `POST /api/ops/resync` + CORS `*` = **UNSAFE** as an ops door (not as a credential dump). |

Honest one-liner: **fifteen anonymous unversioned routes, no weatherforecast, no vault secrets on the wire, no sanitizer, one empty password slot on disk, one live-looking cTrader account id committed, one unauthenticated mutation.**

---

## 1. Files hashed (non-`bin` / non-`obj`)

Measured 2026-08-18; `Get-FileHash -Algorithm SHA256` + `File.ReadAllBytes` / `ReadAllLines`.

| Path | Bytes | Lines | Enc | SHA-256 |
|---|---:|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | **4731** | **95** | UTF-8 no BOM | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 21 | UTF-8 no BOM | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\appsettings.json` | 431 | 21 | UTF-8 no BOM | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` |
| `D:\Prop\apps\api\appsettings.Development.json` | 127 | 8 | UTF-8 no BOM | `73F95F9E0CEB205FC1C4DC50C07697FCFA29D7087868C2AEF1D504CB38C771EC` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | UTF-8 no BOM | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 41 | **UTF-8 BOM** | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` |

`bin\Debug\net8.0\appsettings.json` SHA matches source (`8DCE4CBE…`). Published copy is not a second secret store.

### 1.1 Drift vs C04 (same day, earlier snapshot)

| File | C04 | D30 | Meaning |
|---|---|---|---|
| `Program.cs` | `E914FA98…` / **4658** B / 91 lines | `61B1E0D1…` / **4731** B / **95** lines | Host grew: `ConfigureHttpJsonOptions` + `JsonStringEnumConverter`; `/api/health` now says fake connector / no live TLS; trader detail calls `GetTraderDetailAsync`. |
| `launchSettings.json` | `E092DE59…` / 1133 B; IIS `launchUrl` still `weatherforecast` | `BC022898…` / 1125 B | All three profiles `launchUrl` = **`swagger`**. Weather leftover **GONE**. |
| `.http` / `appsettings*.json` / `.csproj` | same SHAs | same SHAs | Unchanged. |

`UserSecretsId` is still **absent** from the API `.csproj` (workers have one; API does not). Adjacent, not a browser leak.

---

## 2. What `Program.cs` actually is

`D:\Prop\apps\api\Program.cs` is a minimal-API host. Composition, in order:

1. `AddTraderIntelligence(builder.Configuration)` — server-side DI only (`D:\Prop\src\Infrastructure\DependencyInjection.cs`). Reads `ConnectionStrings:TraderIntelligence` or `DATABASE_URL`. Empty / `<SECRET>` → in-memory EF. **Nothing from that call is returned as HTTP.**
2. `ConfigureHttpJsonOptions` — `JsonStringEnumConverter` so enums (`TraderState`, `TradeDirection`, …) serialize as strings.
3. `AddEndpointsApiExplorer` + `AddSwaggerGen`.
4. CORS default policy: `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`.
5. `UseCors()`. Development: `UseSwagger()` **only** (**no** `UseSwaggerUI()`).
6. Fifteen anonymous maps (table §3).
7. Startup scope: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`.
8. `app.Run()`.

**Not present** (token counts on this file, case-sensitive):

| Token | Hits |
|---|---:|
| `MapGet` | **14** |
| `MapPost` | **1** |
| `MapPut` / `MapDelete` / `MapPatch` / `MapHub` / `MapControllers` | **0** |
| `weatherforecast` / `WeatherForecast` | **0** |
| `Password` / `Secret` / `AccountId` / `ConnectionString` | **0** |
| `IConfiguration` | **0** |
| `AddAuthentication` / `AddAuthorization` / `UseSerilog` / `AddSignalR` | **0** |
| `UseSwagger` | **1** (Dev only) |
| `UseSwaggerUI` | **0** |
| `UseHttpsRedirection` | **0** |
| `AllowAnyOrigin` | **1** |
| `EnsureCreatedAsync` | **1** |

Project references `Domain` + `Application` + `Infrastructure`. Packages `Swashbuckle.AspNetCore 6.6.2`, `Serilog.AspNetCore 8.0.2`, `Microsoft.AspNetCore.SignalR.Common 8.0.4`. **Serilog and SignalR are unused** in this file. Do not treat those package names as implemented features.

---

## 3. Complete live endpoint list

Every handler is anonymous. CORS `AllowAnyOrigin` applies to all of them. There is no `/api/v1` prefix (A63 catalog is **not** this host).

| # | Method | Path | `Program.cs` | What the browser / any origin receives | Secrets? |
|---|---|---|---|---|---|
| 1 | `GET` | `/health` | 25 | `{ status: "ok", utc }` — process liveness. Extra `utc` vs A77 `{status:"ok"}` only. | **No.** |
| 2 | `GET` | `/api/health` | 26–33 | **Hardcoded** inventory, now **honest-ish**: ACHIEVER `healthy: true`, details `"demo FakeMt5BrokerConnector — not live Manager"`; QUOTE `healthy: false`, details `"no live TLS socket"`; database `healthy: true`; redis `healthy: false`; `outboxBacklog: 0`. **Not** a real probe. | **No.** |
| 3 | `GET` | `/api/risk/status` | 34 | `IDashboardQueries.GetRiskAsync` → `RiskDashboardDto` (PnL zeros, kill-switch mode, reject reasons, `RealCopyEnabled: false`). | **No.** |
| 4 | `GET` | `/api/reconciliation/status` | 35–41 | **Hardcoded** zeros + `lastReconciliation: now`. | **No.** |
| 5 | `GET` | `/api/settings` | 42–47 | **Hardcoded** anonymous object: `riskLimits`, `featureFlags.REAL_COPY_EXECUTION_ENABLED=false`, broker ids `ACHIEVER` / `STARWAVEFX`. Closest “config to React” surface. **Does not** dump `appsettings` / `CTrader`. | **No.** |
| 6 | `GET` | `/ready` | 48–52 | `{ ready: true, brokers }` via `CountAsync(db.Brokers)`. Count only. Always `ready: true` even on in-memory demo — A77 **PARTIAL**. | **No.** |
| 7 | `GET` | `/api/overview` | 54 | `OverviewDto` aggregates. `RealCopyEnabled` hardcoded `false` in `EfDashboardQueries`. | **No.** |
| 8 | `GET` | `/api/brokers` | 55 | `BrokerStatusDto` with `ManagerLoginMasked` (`login / 100 * 100`, e.g. `2027` → `2000`). **No password field exists on `Broker`.** Mask algorithm is **not** A63 last-two (`"**27"`). | **No password.** Identifier-adjacent. |
| 9 | `GET` | `/api/groups` | 56 | `GroupRowDto` (names, plan mapping, counts). | **No.** |
| 10 | `GET` | `/api/traders` | 57–58 | `TraderRowDto` list. Query: `broker`, `state`. Trader **login numbers**, not manager passwords. | **No.** |
| 11 | `GET` | `/api/traders/{broker}/{login:long}` | 59–60 | `GetTraderDetailAsync` → `TraderDetailDto?` (`TraderRowDto` header + `TradeHighlightDto` list). C04’s “same row DTO” is **stale**. | **No.** |
| 12 | `GET` | `/api/fix/sessions` | 61 | `FixSessionDto`: host, port, seq, status, last error, quote bid/ask. Entity has `SenderCompId` / `SenderSubId`; **DTO omits both**. | **No password.** Host/port are operational (A63 §2.2 allows). |
| 13 | `GET` | `/api/risk` | 62 | Alias of `/api/risk/status`. | **No.** |
| 14 | `GET` | `/api/trades` | 63–71 | **Raw `ReconstructedTrade` EF entities**, last 200, `OrderByDescending(OpenedAt)`. `login` filter **is** applied. `broker` query **accepted and unused**. Entity has **no secret columns**. Allow-list law violated; vault law not. | **No.** |
| 15 | `POST` | `/api/ops/resync` | 73–82 | Mutation: `SyncBrokerAsync("ACHIEVER")` + `STARWAVEFX` from `2026-01-01`; rebuilds logins `10001,10002,10003,99001`. Response `{ achieverDeals, starwaveDeals }`. | **No secrets in the body.** Anonymous write — **UNSAFE** as an ops door. |

### 3.1 Implicit / unmapped (not `Map*`, still relevant)

| Method | Path | Expected if process is up | Notes |
|---|---|---|---|
| `GET` | `/swagger/v1/swagger.json` | **200 in Development only** | `UseSwagger()` without UI. Schema of the 15 maps. No options types on those maps → **no password fields in the document**. |
| `GET` | `/swagger` | **404** | `UseSwaggerUI()` is **never** called. All three `launchSettings` profiles still `launchUrl: "swagger"`. |
| `GET` | `/weatherforecast` | **404** | Template route **GONE**. |
| `GET` | `/hubs/dashboard` | **404** | `apps/web/src/api/signalr.ts` targets this. Host has **no** `AddSignalR` / `MapHub`. |
| `*` | `/api/v1/**` | **404** | A63 first-useful catalog is **MISSING** on this host. |
| `POST` | `/api/v1/auth/login` | **404** | Auth surface **MISSING**. |

`.http` samples 7 of 15 maps on `:5000` and is still a poorly-formed REST Client file (C50). It contains **no** secrets.

`apps/web/src/api/hooks.ts` GETs this unversioned set (`/api/settings` included). Settings page dumps the JSON with the caption *“Secrets are never returned to the browser.”* That caption is **true of the current payload**, not of a guaranteed contract.

---

## 4. Secrets — disk vs wire

Architecture §55 denylist (never to React): MT5 passwords, proxy credentials, cTrader account password, FIX password, database passwords, Redis passwords. §52: never show FIX password. A63 §2.1 also forbids `SenderSubId`, connection-string fragments, vault keys, refresh tokens.

### 4.1 On disk in `apps/api` (complete secret-adjacent content)

`appsettings.json` (source == published Debug copy):

```json
{
  "AllowedHosts": "*",
  "ConnectionStrings": { "TraderIntelligence": "" },
  "CTrader": {
    "Host": "live-us-eqx-01.p.c-trader.com",
    "AccountId": "1369850",
    "Password": "",
    "UseSsl": true,
    "QuoteEnabled": true,
    "TradeSessionEnabled": true,
    "RealCopyExecutionEnabled": false
  }
}
```

| Slot | Value | Class |
|---|---|---|
| `CTrader:Password` | `""` | Empty **slot**. Not a live secret. |
| `ConnectionStrings:TraderIntelligence` | `""` | Empty → `AddTraderIntelligence` uses in-memory EF. |
| `CTrader:AccountId` | `"1369850"` | **Live-looking destination account number.** Identifier, not a password. **FLAG** (same as B25). |
| `CTrader:Host` | `live-us-eqx-01.p.c-trader.com` | Public venue host. |
| `AllowedHosts` | `*` | Host-header wide open. Not a secret. **FAIL** as a production default. |
| `appsettings.Development.json` | Logging only | Clean. |
| `UserSecretsId` | **absent** | API cannot load user-secrets by id. |
| `D:\Prop\.env` / `apps/api/.env` / `appsettings.Production.json` | **absent** | — |
| `%APPDATA%\Microsoft\UserSecrets` entries for this project | **none observed** | — |

`AddTraderIntelligence` **does not** `Configure<CTraderFixOptions>`. The `CTrader` JSON section is therefore **dead config** for this host: loaded by the generic configuration system, never bound, never returned.

`docker-compose.yml` `api` service sets only `ASPNETCORE_ENVIRONMENT=Development`. It does **not** pass a connection string or FIX password into the API. Adjacent compose `postgres` uses `POSTGRES_PASSWORD=ti_dev_only` — **not** in the API project; not served by any map.

### 4.2 On the wire (every map)

| Candidate leak | Present on a response type? |
|---|---|
| `Password` / `ProxyPassword` / `ApiKey` / `RawData` | **No.** |
| `ConnectionString` / `DATABASE_URL` / Redis AUTH | **No.** |
| `CTraderFixOptions` / `Mt5BrokerOptions` / `IConfiguration` | **No.** |
| `FixSessionState.SenderCompId` / `SenderSubId` | On the **entity**; **omitted** from `FixSessionDto`. |
| `Broker.ManagerLogin` raw | **Masked** (integer floor, not A63 string mask). No password column on `Broker`. |
| `ReconstructedTrade` | Full EF entity on `/api/trades`. Columns are trade economics only. |
| `TraderDetailDto` / `TradeHighlightDto` | Header + trade highlights. No secrets. |

No map returns the `CTrader` section. A future “dump configuration” handler, or serializing `FixSessionState` / options types, would break §55 **immediately**. There is no middleware to stop that.

### 4.3 What is *not* a secret but looks like one

| Value | Where | Why it is not a §55 fail |
|---|---|---|
| Trader logins `10001` / `99001` | `/api/traders`, `/api/ops/resync` | Source account numbers; architecture identity is `{broker, login}`. |
| Manager login masked `2000` / `9900` | `/api/brokers` | Operational; A63 allows masked login. Algorithm is weaker than spec. |
| FIX host + port | `/api/fix/sessions` | A63 §2.2 allow-list. |
| Venue account `1369850` | **On disk only** (`appsettings` + seeder `SenderCompId` default). Not in any HTTP DTO. | Destination account **number** is allowed on the wire; password is not. Still do not put the password next to it. |

### 4.4 Sanitizer / allow-list law (A63 §1 / §2)

| Control | State |
|---|---|
| Allow-list DTOs only | **FAIL** — `/api/trades` returns `ReconstructedTrade` entities. |
| Reject denylisted request keys with 422 | **MISSING.** |
| Redaction middleware | **MISSING.** |
| Audit `after` blob sanitizer | **MISSING** (no auth, no audit writer on these maps). |
| Settings page contract | Caption is true **today**; payload is a hardcoded anonymous type, not `settings/public`. |

**Verdict:** **no live password in this project; no secret on the current wire; guarantee is accidental.** Classification of the secret posture: **EXISTS_NEEDS_REFACTOR** (empty slots + identifier FLAG + missing sanitizer). Not `UNSAFE` as a credential dump. Do not greenwash this as a §55 implementation.

---

## 5. Adjacent security (not secrets, still binding)

| Surface | Measured | Class |
|---|---|---|
| Authentication | None | **MISSING** |
| RBAC (§59 / A51) | None | **MISSING** |
| CORS | `AllowAnyOrigin` + any header/method; **not** env-gated | **UNSAFE** (C22) |
| `POST /api/ops/resync` | Anonymous mutation from any origin | **UNSAFE** |
| HTTPS redirect | Not called | Dev HTTP `:5000` is the documented web fallback |
| Swagger UI | Not mapped; `launchUrl` still `swagger` | **EXISTS_NEEDS_REFACTOR** |
| Swagger in Production | `UseSwagger` is Development-only | **PASS** vs A63 “no UI in prod” |
| SignalR | Package present; no hub | **MISSING** (`/hubs/dashboard` 404) |
| Serilog | Package present; default MEL logging | **MISSING** as a wired sink |
| Health vs A77 | `/health` extra `utc`; `/ready` always `true`; `/api/health` is a fake inventory | **EXISTS_NEEDS_REFACTOR** |
| A63 `/api/v1` catalog | **0** of those routes | **MISSING** |
| EF migrations | `EnsureCreatedAsync` at boot | **MISSING** (C29) |

---

## 6. First-useful / React bind (honest)

| Check | Result |
|---|---|
| Dashboard APIs A63 `/api/v1/**` | **0 / required. MISSING.** Unversioned `/api/*` is a demo sketch. |
| Auth + RBAC | **MISSING.** |
| Secrets to React (§55) | **No leak today.** **No redaction layer** for when secret-bearing types appear. |
| First useful version (§69 item 12) | React can bind the sketch. It cannot claim §69: health is hardcoded, FIX is disconnected/fake, resync hits `FakeMt5BrokerConnector`, `RealCopyEnabled` is forced false. |
| Production-safe default | **FAIL.** `AllowedHosts=*`, CORS `*`, anonymous mutation, empty password slot sitting next to a real account id. |

Do not treat “15 endpoints exist” as A63 complete. Do not treat empty `CTrader:Password` as “secrets configured.” Do not treat `/api/health` `healthy: true` on ACHIEVER as live Manager.

---

## 7. What a later coding wave must not break

1. **Never** add `IConfiguration` / options / `FixSessionState` / `Broker` (raw) as a response type.
2. **Never** return `CTrader:Password`, `SenderSubId`, connection strings, or proxy credentials. Empty string is still a denylisted **key**.
3. Replace `/api/trades` entity dump with an allow-list DTO before any new column lands on `ReconstructedTrade`.
4. Gate `POST /api/ops/resync` (or delete it) before any real connector is registered.
5. Keep `REAL_COPY_EXECUTION_ENABLED` off on this host until A100 + A101 are measured PASS.
6. Sample file `TraderIntelligence.Api.http` must stay secret-free (C50).

---

## 8. Classification summary

| Aspect | Class |
|---|---|
| Host as demo BFF | **EXISTS_NEEDS_REFACTOR** |
| Endpoint count (15 maps) | **EXISTS_NEEDS_REFACTOR** (unversioned, unauthenticated, mixed hardcoded/EF) |
| `weatherforecast` | **GONE** |
| §55 secret on the wire | **GONE** (safe by absence) |
| §55 sanitizer | **MISSING** |
| Live password in `apps/api` | **GONE** (empty slot only) |
| `CTrader:AccountId` / venue host committed | **FLAG** (identifier, not password) |
| Auth / RBAC / `/api/v1` | **MISSING** |
| Anonymous `POST /api/ops/resync` | **UNSAFE** |
| CORS `*` | **UNSAFE** |

**D30 done.** Product source was not modified. API was not launched.
