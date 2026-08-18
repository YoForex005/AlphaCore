# C04 — API host review: secrets to the browser? weatherforecast gone?

| Field | Value |
|---|---|
| Agent | C04 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C04_api_review.md` |
| Ask | Read `apps/api/Program.cs`. Answer: **secrets to browser?** **weatherforecast gone?** |
| Product source modified | **No.** Report only. |
| Method | Full read of `D:\Prop\apps\api\Program.cs` (91 lines / 86 non-blank). SHA-256 of every non-`bin`/`obj` file under `apps/api`. Grep `weatherforecast` / `Password` / `Secret` / `IConfiguration` on that tree. Cross-read dashboard DTOs, `EfDashboardQueries`, `AddTraderIntelligence`, `TraderDbContext`, `ReconstructedTrade`, `Broker`, `FixSessionState`, `DemoSeeder`, `apps/web` settings consumer, architecture §§55 / 72.5, A19 / A63 / B06 / B23. **API process was not launched.** No HTTP capture. Route list is from `MapGet` / `MapPost` as read. |
| Law | Architecture v2 §55 (never expose the password denylist to React), §52 (never show FIX password), §72.5 (never expose secrets to the browser), A63 §2 sanitizer, A77 probes. |
| Relates | A06 (stale: host is weatherforecast-only), B06 (stale on `.http` leftover), B23 (stale on `.http` leftover), A19, A26, A63, A77 |
| Supersedes | A06 host-state (weatherforecast is the only route). B06 §2.2 and B23 “2 non-`.cs` leftovers” **for `TraderIntelligence.Api.http` only** — that file no longer names weatherforecast. |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Headline answers (measured)

| Question | Answer | Evidence |
|---|---|---|
| **Secrets to the browser?** | **No §55 secret is serialized by any live map.** `Program.cs` never injects `IConfiguration`, never returns `CTrader` options, never returns `ConnectionStrings`, never returns a password / proxy user / Redis AUTH / FIX `RawData` / `SenderSubId`. | §2, §3 |
| **Is that because of a sanitizer?** | **No.** There is **no** allow-list serializer and **no** A63 §2.3 redaction middleware. Today is **safe by absence** of secret-bearing types on the wire, not by a fail-closed filter. | §4 |
| **weatherforecast gone?** | **Gone from `Program.cs`.** No `MapGet("/weatherforecast")`. No `record WeatherForecast`. No `WeatherForecast.cs`. **One leftover string remains:** IIS Express `launchUrl` in `launchSettings.json`. `GET /weatherforecast` is therefore an unmapped path (expected **404** if the process is running). | §5 |

Honest one-liner: **the template forecast route is dead; the browser does not receive vault secrets; the host is still an anonymous demo BFF with no secret-sanitizer, so the §55 guarantee is accidental and fragile.**

---

## 1. Files hashed (non-`bin` / non-`obj`)

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1133 | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\appsettings.json` | 431 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` |
| `D:\Prop\apps\api\appsettings.Development.json` | 127 | `73F95F9E0CEB205FC1C4DC50C07697FCFA29D7087868C2AEF1D504CB38C771EC` |

Drift vs B06 (same day, earlier snapshot):

| File | B06 SHA-256 | C04 SHA-256 | Meaning |
|---|---|---|---|
| `Program.cs` | `13CF8003…` (4503 B) | `E914FA98…` (4658 B) | Host grew; weather route **still absent** |
| `.http` | `353BB5D9…` (stock `GET /weatherforecast/` on `:5160`) | `2AEC0F4A…` | **Leftover A is gone.** File now samples `/health` + `/api/*` on `:5000` |
| `launchSettings.json` | `E092DE59…` | `E092DE59…` **unchanged** | IIS Express `launchUrl` still `weatherforecast` |
| `appsettings.json` / `.csproj` / Development json | same | same | — |

`UserSecretsId` is **absent** from the API `.csproj` (workers have one; API does not). Adjacent, not a browser leak.

---

## 2. What `Program.cs` actually is

`D:\Prop\apps\api\Program.cs` is a minimal-API host. Composition, in order:

1. `AddTraderIntelligence(builder.Configuration)` — server-side DI only (`D:\Prop\src\Infrastructure\DependencyInjection.cs`). Reads `ConnectionStrings:TraderIntelligence` or `DATABASE_URL`. Empty / `<SECRET>` → in-memory EF. **Nothing from that call is returned as HTTP.**
2. `AddEndpointsApiExplorer` + `AddSwaggerGen`.
3. CORS default policy: `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`.
4. `UseCors()`. Development: `UseSwagger()` **only** (no `UseSwaggerUI()`).
5. Fifteen anonymous maps (table below).
6. Startup scope: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`.
7. `app.Run()`.

**Not present (grep of this file, case-sensitive tokens):**

| Token | Hits |
|---|---:|
| `weatherforecast` / `WeatherForecast` | **0** |
| `Password` / `Secret` / `AccountId` / `ConnectionString` | **0** |
| `IConfiguration` | **0** |
| `AddAuthentication` / `AddAuthorization` / `UseSerilog` / `AddSignalR` / `MapHub` | **0** |
| `UseHttpsRedirection` | **0** |

### 2.1 Live maps (complete)

Every handler is anonymous. CORS `AllowAnyOrigin` applies to all of them.

| # | Method | Path | Lines | What the browser / any origin receives |
|---|---|---|---|---|
| 1 | `GET` | `/health` | 21 | `{ status: "ok", utc }` — process liveness. **No secrets.** Extra `utc` vs A77 `{status:"ok"}` only. |
| 2 | `GET` | `/api/health` | 22–29 | **Hardcoded** demo inventory (`ACHIEVER` healthy, `QUOTE` healthy, redis `healthy: false`, `outboxBacklog: 0`). **No secrets.** Not a real probe. |
| 3 | `GET` | `/api/risk/status` | 30 | `IDashboardQueries.GetRiskAsync` → `RiskDashboardDto` (PnL zeros, kill-switch mode, reject reasons). **No secrets.** |
| 4 | `GET` | `/api/reconciliation/status` | 31–37 | **Hardcoded** zeros + `lastReconciliation: now`. **No secrets.** |
| 5 | `GET` | `/api/settings` | 38–43 | **Hardcoded** anonymous object. Closest “config to React” surface. See §3.1. **No secrets today.** |
| 6 | `GET` | `/ready` | 44–48 | `{ ready: true, brokers }` via `CountAsync(db.Brokers)`. Count only. **No secrets.** Always `ready: true` even if the store is the in-memory demo — A77 **PARTIAL**. |
| 7 | `GET` | `/api/overview` | 50 | `OverviewDto` aggregates. `RealCopyEnabled` is hardcoded `false` in the query impl. **No secrets.** |
| 8 | `GET` | `/api/brokers` | 51 | `BrokerStatusDto` with `ManagerLoginMasked`. **No password field exists on `Broker`.** |
| 9 | `GET` | `/api/groups` | 52 | `GroupRowDto` (names, plan mapping, counts). **No secrets.** |
| 10 | `GET` | `/api/traders` | 53–54 | `TraderRowDto` list (login, scores, flags). Trader **login numbers**, not manager passwords. |
| 11 | `GET` | `/api/traders/{broker}/{login}` | 55–56 | Same row DTO (not A26/A93 detail). **No secrets.** |
| 12 | `GET` | `/api/fix/sessions` | 57 | `FixSessionDto`: host, port, seq, status, last error, quote. **Password / `SenderSubId` / `SenderCompId` not in the DTO.** |
| 13 | `GET` | `/api/risk` | 58 | Alias of `/api/risk/status`. |
| 14 | `GET` | `/api/trades` | 59–67 | **Raw `ReconstructedTrade` EF entities**, last 200, `broker` query unused. Entity has **no secret columns**. Allow-list law violated; vault law not. |
| 15 | `POST` | `/api/ops/resync` | 69–78 | Mutation: syncs `ACHIEVER` + `STARWAVEFX` from 2026-01-01; rebuilds logins `10001,10002,10003,99001`. Response `{ achieverDeals, starwaveDeals }`. **No secrets in the body.** Anonymous write — **UNSAFE** as an ops door, not as a credential dump. |

`apps/web/src/api/hooks.ts` already GETs this unversioned set (`/api/settings` included). Settings page dumps the JSON with the caption *“Secrets are never returned to the browser.”* That caption is **true of the current payload**, not of a guaranteed contract.

---

## 3. Secrets to the browser — §55 denylist vs each response

Architecture §55 (doc lines 2004–2015) forbids sending these to React:

```text
MT5 passwords
proxy credentials
cTrader account password
FIX password
database passwords
Redis passwords
```

§52: “Never show FIX password.” §72.5: “Never expose secrets to the browser.” A63 §2.1 adds `SenderSubId`, connection-string fragments, vault keys, refresh tokens.

### 3.1 `GET /api/settings` — the only “config” map (must not become a vault)

Exact handler (`Program.cs` 38–43):

```csharp
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    brokerConfigs = new[] { new { id = "ACHIEVER", name = "Achiever", enabled = true }, new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true } }
}));
```

| Could have leaked | Does it? |
|---|---|
| `CTrader:Password` | **No.** Not read. |
| `CTrader:AccountId` (`1369850` on disk) | **No.** Not read. |
| `CTrader:Host` | **No.** Not read. |
| `ConnectionStrings:TraderIntelligence` | **No.** Not read. |
| `IConfiguration` / `GetSection("CTrader")` | **No.** Literal anonymous object. |
| `REAL_COPY_EXECUTION_ENABLED` | Hardcoded `false`. Not bound from `CTrader:RealCopyExecutionEnabled`. Honest as a **demo flag**, not as live options. |

This is the correct *shape* of a public settings DTO (A63 `GET /api/v1/settings/public`): flags and names only. It is **not** wired to options, so a later “bind CTrader into settings so the page is live” change would be the leak. **Do not `return configuration.GetSection("CTrader").Get<CTraderFixOptions>()`.** `CTraderFixOptions` carries `Password` and `AccountId` (`D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`).

`AddTraderIntelligence` does **not** even `Configure<CTraderFixOptions>`. The API host does not bind that options type today.

### 3.2 `GET /api/brokers` — manager login, not password

`Broker` (`D:\Prop\src\Domain\Entities\Broker.cs`) has **no password property**. It has `ManagerLogin`, `Server`, `ProxyHost`, `ProxyPort`.

`EfDashboardQueries.GetBrokersAsync` projects:

`BrokerStatusDto(Code, DisplayName, Server, MaskLogin(ManagerLogin), Connected=true, GroupCount, AccountCount, LastEventAt)`.

`MaskLogin`: if `login >= 100` then `login / 100 * 100` (so seeded `2027` → `2000`, `9904` → `9900`). A63 wants last-two-digits (`2027` → `"**27"`). **Identifier-mask quality is wrong; it is still not a password.** `ProxyHost` / `ProxyPort` are **not** copied onto the DTO. Demo seeder does not set proxy credentials (entity cannot store a proxy password anyway).

### 3.3 `GET /api/fix/sessions` — host/port, not logon secret

`FixSessionState` **does** persist `SenderCompId`, `TargetCompId`, `SenderSubId`, `TargetSubId`. Demo seed writes `SenderCompId = "live.pepperstone.1369850"` and `TargetSubId` `QUOTE`/`TRADE`. A63 **forbids** `senderSubId` on the wire; allows host / port / (optionally) sender/target CompID.

`GetFixSessionsAsync` projects `FixSessionDto`:

`Qualifier, Host, Port, Connected, LoggedOn, Status, LastInbound, LastOutbound, InboundSeq, OutboundSeq, ReconnectCount, LastError, InstrumentId, Bid, Ask, QuoteAgeSeconds, ExecutionEnabled=false`.

| Field on entity | On DTO? | Class |
|---|---|---|
| `Password` | entity has **none** | n/a |
| `SenderSubId` | **omitted** | A63 forbidden — **good** |
| `SenderCompId` / `TargetCompId` | **omitted** | allowed by A63; not sent |
| `Host` / `Port` | sent | architecture treats FIX host/ports as **non-secret** |
| `LastError` | sent | **future risk** if a logon exception stringifies `Password=` / tag 96 |

Today `LastError` is unset in the seeder. **No FIX password reaches React.**

### 3.4 `GET /api/trades` — raw EF, still no vault columns

`ReconstructedTrade` fields: ids, login, position, symbols, direction, timestamps, VWAPs, volumes, PnL, SL/TP, scale flags. **Zero** password / connection / token properties.

Serializing the entity violates A63 allow-list discipline (and leaks internal `BrokerId` GUIDs). It does **not** violate §55.

`broker` query parameter is declared and **unused**. Filter is login-only. Not a secret issue.

### 3.5 Disk config that is **not** served

`D:\Prop\apps\api\appsettings.json` (431 B) contains:

- `ConnectionStrings:TraderIntelligence` = `""`
- `CTrader:Host` = `live-us-eqx-01.p.c-trader.com`
- `CTrader:AccountId` = `1369850` (live destination **number**, architecture-non-secret)
- `CTrader:Password` = `""` (empty slot, not a live secret)
- `CTrader:UseSsl` / `QuoteEnabled` / `TradeSessionEnabled` / `RealCopyExecutionEnabled: false`

A19’s “API appsettings is logging-only” snapshot is **stale**. The CTrader block is on disk now. **It is not mapped to any HTTP response.** Empty `Password` is still a **slot**; filling it and later binding the section into `/api/settings` would become a §55 FAIL.

`appsettings.Development.json` is logging only.

### 3.6 Denylist scoreboard (this host, this hash)

| §55 / A63 item | In any `Program.cs` response? | Notes |
|---|---|---|
| MT5 manager password | **NO** | No property on `Broker`; not in any DTO |
| Proxy user / password | **NO** | Not on DTO; entity has host/port only |
| cTrader account password | **NO** | `CTrader:Password` not read by any map |
| FIX password / tag 96 / `RawData` | **NO** | Not stored on `FixSessionState` |
| `SenderSubId` | **NO** | Entity yes, DTO no |
| Database password / connection string | **NO** | Used only inside `AddTraderIntelligence` |
| Redis password | **NO** | Redis not wired; `/api/health` invents `healthy: false` |
| Refresh / access tokens | **NO** | No auth |
| `CTrader:AccountId` `1369850` | **NO** (HTTP) | On disk in appsettings + in seeded `SenderCompId` (not projected) |
| FIX host / SSL port | **YES** | Allowed operational field (A63 §2.2) |
| Masked manager login | **YES** (numeric truncation) | Not A63 `**27` format |
| Trader logins / PnL / scores | **YES** | Dashboard data, not §55 secrets |

**Verdict on the asked question: secrets are not sent to the browser.**

---

## 4. Why this is not a green PASS

§55 is a **standing** rule. Current compliance is **EXISTS_AND_GOOD for the payload, MISSING for the mechanism.**

| Control | Status |
|---|---|
| Allow-list DTO on every map | **PARTIAL.** Dashboard queries use records. `/api/trades` returns EF. `/api/settings` is an ad-hoc anonymous type. |
| A63 §2.3 sanitizer (drop `(?i)password|secret|connectionstring|…`) | **MISSING** |
| Auth / RBAC (§59) | **MISSING.** Entire surface is anonymous, including `POST /api/ops/resync` |
| CORS | **UNSAFE** for a LAN/demo that later holds live books: any webpage can read `/api/*` |
| `AllowedHosts` | `*` |
| Swagger in Development | `UseSwagger()` publishes `/swagger/v1/swagger.json` (schemas, not secret *values*) |
| Settings page contract | Web dumps whatever `/api/settings` returns. One bad bind and the caption lies. |
| `LastError` on FIX DTO | Unfiltered string. Future logon failures must not echo the password. |
| `UserSecretsId` on API | **MISSING** |

Classification:

| Component | Class |
|---|---|
| §55 secret leak **today** | **ABSENT** (`EXISTS_AND_GOOD` for current JSON) |
| Secret-safe contract / sanitizer | **MISSING** |
| `/api/settings` as hardcoded public flags | **EXISTS_AND_GOOD** (do not “improve” it by binding options) |
| Anonymous + `AllowAnyOrigin` dashboard | **UNSAFE** for anything beyond local demo data |
| `POST /api/ops/resync` | **UNSAFE** (mutation), not a secret leak |

Do not claim “production-safe secret handling.” Claim: **no denylist value is on the wire in this snapshot.**

---

## 5. weatherforecast — gone from the host, one launch leftover

### 5.1 `Program.cs` — **GONE**

Grep of `D:\Prop\apps\api` (product source, including `.http` / json / csproj):

| Pattern | File | Line |
|---|---|---|
| `weatherforecast` | `Properties\launchSettings.json` | **35 only** (`IIS Express` → `"launchUrl": "weatherforecast"`) |
| `WeatherForecast` type / route | — | **0** |

No `WeatherForecast.cs`. No `Controllers/WeatherForecastController.cs`. The stock block documented by A06 / B06 §2.4 is **not** in the current file:

```text
app.MapGet("/weatherforecast", () => …);
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);
```

`GET /weatherforecast` is therefore **not a live route**. Expected status if anyone still probes it: **404**. Do **not** add a compat alias.

Replacement for the template’s only honest job (liveness) is `GET /health` (A77). That map exists.

### 5.2 `.http` leftover — **GONE** (B06 / B23 stale)

B06 §2.2 and B23 counted `TraderIntelligence.Api.http` as leftover A (`GET {{host}}/weatherforecast/` on `:5160`). Current file (193 B, SHA `2AEC0F4A…`) is:

```http
@api=http://localhost:5000

GET {{api}}/health
GET {{api}}/api/overview
GET {{api}}/api/brokers
GET {{api}}/api/groups
GET {{api}}/api/traders
GET {{api}}/api/fix/sessions
GET {{api}}/api/risk
```

No weatherforecast token. Host matches the `http` profile (`:5000`). **B06-01 is closed on disk.** Samples still omit `/ready` and still use unversioned `/api/*` (not A63 `/api/v1`). Out of this task’s two questions.

### 5.3 IIS Express leftover — **REMAINS**

`D:\Prop\apps\api\Properties\launchSettings.json`:

| Profile | `launchUrl` | `applicationUrl` | Status |
|---|---|---|---|
| `http` | `swagger` | `http://localhost:5000` | Weatherforecast **removed**. Half-migration: `UseSwagger()` is on in Development; **`UseSwaggerUI()` is not**, so a launched browser on `/swagger` is likely **404** (OpenAPI JSON may still live under `/swagger/v1/swagger.json`). |
| `https` | `swagger` | `https://localhost:7294;http://localhost:5000` | Same. |
| `IIS Express` | **`weatherforecast`** | IIS `:18720` / SSL `44389` | **DEPRECATED leftover.** Browser opens a path the host does not map. |

IIS Express remains `windowsAuthentication: false`, `anonymousAuthentication: true`.

**Acceptance to call weatherforecast fully gone from `apps/api`:** `rg -i weatherforecast D:\Prop\apps\api` excluding `bin`/`obj` → **0**. Today that command still hits line 35 of `launchSettings.json`.

### 5.4 Docs still naming the dead route

Not product leftovers. Listed so compose / A-series text is not re-used as a probe:

| Location | Note |
|---|---|
| A06, A26 intro, A55, A63/A77 host-state paragraphs | Still describe stock weatherforecast as the API. **Stale.** |
| A65 compose sketch | Temporary `curl …/weatherforecast`. Live `D:\Prop\docker-compose.yml` must not (and should not) probe that path — A77 says `GET /health`. |
| B06 §2.2 / B23 “2 leftovers” | Stale on `.http`. Still correct on IIS `launchUrl`. |

---

## 6. Findings (this review only)

| ID | Sev | Finding |
|---|---|---|
| C04-01 | **INFO / PASS** | No §55 secret (MT5 / proxy / cTrader / FIX / DB / Redis password) is returned by any `Program.cs` map. |
| C04-02 | **PASS** | `MapGet("/weatherforecast")` and `record WeatherForecast` are **gone**. |
| C04-03 | **MED** | IIS Express `launchUrl` is still `weatherforecast`. Only remaining product-source hit. Retarget to `health` (or Dev Swagger UI after `UseSwaggerUI()`). |
| C04-04 | **HIGH (regression)** | No sanitizer. `/api/settings` is one `GetSection("CTrader")` away from shipping `Password`. Build the allow-list **before** binding real options. |
| C04-05 | **MED** | `/api/trades` serializes EF entities. Safe today only because `ReconstructedTrade` has no secret columns. |
| C04-06 | **MED** | `FixSessionDto.LastError` is an unsanitized string. Logon failures must not echo passwords. |
| C04-07 | **HIGH (ops, not vault)** | Anonymous `POST /api/ops/resync` + `AllowAnyOrigin`. Not a secret leak; it is an unauthenticated write. |
| C04-08 | **LOW** | Manager-login mask is `2027→2000`, not A63 `**27`. Identifier hygiene, not §55. |
| C04-09 | **LOW** | B06-01 (`.http` weatherforecast) is **fixed**. Do not re-open it from B06 text. |

---

## 7. What this file does **not** claim

- Did not `dotnet run` or `curl` `:5000`. 404 on `/weatherforecast` is inferred from the absence of a map, not observed.
- Did not re-scan `bin/` / `obj/` DLLs for the `WeatherForecast` type name (B23 already reported Debug API DLL clean at an earlier hash).
- Did not declare first-useful `/api/v1` complete. Unversioned `/api/*` is still the demo surface (B06).
- Did not treat live identifiers (`57.128.141.65`, manager `2027`/`9904`, FIX host, account `1369850`) as passwords. Architecture §§7–8 call those non-secret. They appear in seeder / appsettings / FIX DTO host — **not** as the CTrader account password.

---

## 8. Bottom line

| Ask | Measured answer |
|---|---|
| **Secrets to browser?** | **No.** Current handlers do not send the §55 denylist to React. Compliance is **by not putting secrets on the DTO**, not by a sanitizer. `/api/settings` is hardcoded flags. Do not bind `CTraderFixOptions` or `IConfiguration` into it. |
| **weatherforecast gone?** | **Gone from `Program.cs` and from `.http`.** **Not** gone from `Properties/launchSettings.json` (IIS Express `launchUrl`). The HTTP route itself is dead. |

Product source was not modified.
