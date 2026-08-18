# C22 — `apps/api` CORS and Swagger (measured from `Program.cs`)

| Field | Value |
|---|---|
| Agent | C22 (senior engineer, CORS + Swagger verify only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:25:22+05:30 |
| Assigned | Read `apps/api/Program.cs` CORS and swagger. Write this report. Do not modify product source. |
| Primary file | `D:\Prop\apps\api\Program.cs` (not under `D:\Prop\src`) |
| Product source modified | **No.** This report is the only write. |
| Method | Full read of worktree `Program.cs`; SHA-256 + line endings; `git show HEAD:apps/api/Program.cs`; `git blame -L 14,24`; csproj / launchSettings / appsettings / `.http`; Vite client origin; A51 §11, A63 §7.2 / §10, A06, B06, B41. **Did not** launch Kestrel. **Did not** send HTTP. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |
| Precedence | On-disk worktree supersedes A06 (“no `AddCors` / no `AddSwaggerGen`”). A51 + A63 remain binding for the *correct* policy. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**Worktree CORS: `UNSAFE`. Worktree Swagger: `EXISTS_NEEDS_REFACTOR`. Committed HEAD: both `MISSING`.**

On disk, `Program.cs` registers a **default** CORS policy of `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`, then `UseCors()` globally. That is enough for Vite `:3000` → API `:5000` today (no proxy, axios has no `withCredentials`). It is **not** the A51 / A63 policy. Combined with `AllowedHosts: "*"` and a fully anonymous host (including `POST /api/ops/resync`), any browser origin can read the dashboard and fire the only mutation.

Swagger is **half-wired**: `AddEndpointsApiExplorer` + `AddSwaggerGen` always; `UseSwagger()` only when `IsDevelopment()`; **`UseSwaggerUI()` is never called**. `http` / `https` `launchUrl` is `"swagger"` — that path is a **404** in this pipeline. OpenAPI JSON (`/swagger/v1/swagger.json`) is the only Swagger surface that would exist in Development. Production does **not** map Swagger middleware (good vs A63 “Swagger UI in production”). HEAD is still the weatherforecast template: no `AddCors`, no `AddSwaggerGen`, no `UseSwagger`.

Do **not** treat “CORS exists” or `launchUrl: swagger` as PASS. Do **not** treat the filename `C22_cors` as a locked-down origin list.

| Surface | Worktree (disk) | HEAD (committed) | vs A51 / A63 |
|---|---|---|---|
| CORS registration | `AddCors` default policy, lines 16–17 | **none** | required, but must be explicit origin |
| Origins | `AllowAnyOrigin()` (`*`) | n/a | **FAIL** — A51: “Explicit Vite origin; no `*` with credentials.” A63 §10.6: “allow the Vite origin only.” |
| Headers / methods | `AllowAnyHeader()` + `AllowAnyMethod()` | n/a | too wide once mutations exist |
| Credentials | **not** `AllowCredentials()` (cannot combine with `*`) | n/a | cookie BFF later is blocked by this policy |
| `UseCors()` | yes, line 21, first middleware | **none** | global default — no named policy, no SignalR policy |
| Env-gated CORS | **no** — same `*` in Production | n/a | **FAIL** |
| `AddSwaggerGen` | yes, line 15 | **none** (package unused) | Dev OK |
| `UseSwagger()` | Development only, line 23 | **none** | JSON only |
| `UseSwaggerUI()` | **absent** | **absent** | `launchUrl: swagger` will 404 |
| Swagger in Production | not mapped | not mapped | **PASS** vs A63 “Disable or SuperAdmin-only” |
| Auth on OpenAPI | none (host is anonymous) | n/a | would document `POST /api/ops/resync` to anyone who can hit Dev |

Honest one-liner: **Demo CORS is `*`. Swagger JSON is Dev-only and has no UI. Neither is production-ready. HEAD still has neither.**

---

## 1. Binding law (quoted)

A51 §11 (`D:\Prop\reports\swarm\20260818\A51_rbac_audit.md`):

> CORS | Explicit Vite origin; no `*` with credentials.

A63 §10 item 6 (`D:\Prop\reports\swarm\20260818\A63_api_catalog.md`):

> CORS: allow the Vite origin only. Do not leave `AllowedHosts=*` + anonymous dashboard.

A63 §7.2 / A06 §4.15:

> Swagger UI in production | Disable or SuperAdmin-only.

A63 §10 item 1 (stale host paragraph — superseded for *wiring*, not for the contract):

> `apps/api` today: `MapGet("/weatherforecast")` only. Package refs (Swashbuckle, Serilog, SignalR.Common) are unused.

Vite origin that CORS must allow (when the browser talks cross-origin):

| Piece | Path | Measured value |
|---|---|---|
| Vite port | `D:\Prop\apps\web\vite.config.ts` line 6 | `server: { port: 3000 }` — **no** `proxy` |
| REST client | `D:\Prop\apps\web\src\api\client.ts` lines 3–6 | `baseURL: import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` — no `withCredentials` |
| SignalR client | `D:\Prop\apps\web\src\api\signalr.ts` lines 3, 10 | same base; `withUrl(\`${BASE}/hubs/dashboard\`)` — **no** hub on the API |

Cross-origin is **required** today. Same-origin / Vite proxy was specified in A62 / A65 and is **not** implemented.

---

## 2. File identity

| Field | Value |
|---|---|
| Path | `D:\Prop\apps\api\Program.cs` |
| Bytes | **4658** |
| Physical lines | **95** (LF only, no CR; no UTF-8 BOM) |
| SHA-256 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| LastWriteUtc | `2026-08-18T07:52:04.8133238Z` |
| LastWrite local | `2026-08-18T13:22:04+05:30` |
| `git status` | ` M apps/api/Program.cs` (unstaged; LF→CRLF warning on next Git touch) |
| `git diff --stat` | `101 +81 −20` vs HEAD template |
| Blame on CORS/Swagger lines | `Not Committed Yet` (timestamp of this blame run) |
| HEAD blob | weatherforecast template (quoted §4) — **0** CORS, **0** Swagger calls |

Adjacent files (not edited by this agent):

| File | SHA-256 / note | Relevance |
|---|---|---|
| `TraderIntelligence.Api.csproj` | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | `Swashbuckle.AspNetCore` **6.6.2** (HEAD and worktree). CORS is the in-box `Microsoft.AspNetCore.Cors` — no extra package. |
| `Properties/launchSettings.json` | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` (worktree) | `http`/`https` `launchUrl`: **`swagger`**. IIS Express still **`weatherforecast`**. `http` URL `http://localhost:5000`. |
| `appsettings.json` | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` | `"AllowedHosts": "*"` — host filter, not CORS; same hole class as A63 §10.6. |
| `TraderIntelligence.Api.http` | worktree dirty | samples `:5000` `/health` + dashboard GETs; **no** swagger URL. |
| HEAD `launchSettings.json` | all three profiles `launchUrl: weatherforecast`; `http` was `:5160` | B41 / B23 already recorded the port + launchUrl move. |

---

## 3. Exact CORS + Swagger source (worktree)

`Program.cs` lines 7–23 (only the host pipeline that this task asked about):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

app.UseCors();
if (app.Environment.IsDevelopment())
    app.UseSwagger();
```

What this block **does not** contain (grep of `D:\Prop\apps` `*.cs`):

| Call | Present? |
|---|---|
| `AddCors` / `AddDefaultPolicy` | **yes** (only here) |
| `AddPolicy("…")` named policy | **no** |
| `WithOrigins(...)` | **no** |
| `SetIsOriginAllowed` / `SetIsOriginAllowedToAllowWildcardSubdomains` | **no** |
| `AllowCredentials()` | **no** |
| `WithExposedHeaders` / `SetPreflightMaxAge` | **no** |
| `UseCors("name")` | **no** — nameless `UseCors()` → default policy |
| CORS bound from `IConfiguration` / env (`Cors:AllowedOrigins`) | **no** |
| `UseSwaggerUI()` / `MapSwagger()` | **no** |
| `AddSwaggerGen(c => { … Title / Security / XML })` | **no** — parameterless |
| `UseAuthentication` / `UseAuthorization` | **no** |
| `UseHttpsRedirection` | **removed** vs HEAD |
| `MapHub` / `AddSignalR` | **no** |

`git blame -L 14,24` on the worktree: lines 14–17 and 21–23 are `Not Committed Yet`. Lines 18–20 and 24 are still `^6c41447` (`Initial commit` 2026-08-18 13:12:17 +0530) — blank lines / `var app = builder.Build()`.

---

## 4. HEAD `Program.cs` (committed — still the template)

`git show HEAD:apps/api/Program.cs` in full:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

HEAD has **zero** `AddCors` / `UseCors` / `AddSwaggerGen` / `UseSwagger`. Swashbuckle 6.6.2 is a package reference only (A06 / A55 were correct **for HEAD**). A browser on `:3000` talking to a process built from HEAD `:5160` would be blocked by the browser CORS check if any `Access-Control-Allow-Origin` were required — and HEAD also has no dashboard routes.

Treat A06 “CORS unused / Swagger unused” as **HEAD-true, worktree-stale**. Treat B06-05 / B06-06 as **worktree-true** (this pass re-measured the same policy, 95 lines vs B06’s 91-line count).

---

## 5. CORS — what the policy actually means

### 5.1 Policy object

| Knob | Value | Runtime effect (ASP.NET Core 8 default policy) |
|---|---|---|
| Policy name | default (`AddDefaultPolicy`) | `UseCors()` with no name applies it to every endpoint |
| Origins | `AllowAnyOrigin()` | `Access-Control-Allow-Origin: *` |
| Headers | `AllowAnyHeader()` | reflects any `Access-Control-Request-Headers` |
| Methods | `AllowAnyMethod()` | GET/POST/PUT/PATCH/DELETE/OPTIONS/… |
| Credentials | omitted | **cannot** be added without dropping `AllowAnyOrigin` (framework throws at startup if both are set) |
| Scope | global middleware, line 21, **before** endpoint maps | every mapped route, including `/health` and `POST /api/ops/resync` |
| Environment | not gated | Production Compose/Kestrel gets `*` too |

Preflight `OPTIONS` is handled by the CORS middleware. There is no explicit `MapMethods("OPTIONS")`. That part is fine.

### 5.2 Why this is `UNSAFE`, not `EXISTS_AND_GOOD`

The demo React app **does** need *some* CORS: Vite listens on `http://localhost:3000`, the API on `http://localhost:5000`, and `client.ts` always uses an absolute `baseURL`. `AllowAnyOrigin` makes that demo load. That is the only thing it gets right.

It fails the written contract:

1. **Origin is `*`, not Vite.** A51 / A63 require `http://localhost:3000` (and later the real dashboard origin). `*` also allows any published page, browser extension, or hostile site to call the API from a victim’s browser.
2. **The host is anonymous.** CORS is not authentication. `*` + no JWT/cookie means the “protection” is “hope nobody finds `:5000`.” `AllowedHosts: "*"` does not compensate.
3. **A mutation is on the same policy.** `POST /api/ops/resync` (lines 73–82) syncs `ACHIEVER` + `STARWAVEFX` from 2026-01-01 and rebuilds four logins. Any origin can trigger it. B06-04 / B06-05 still hold.
4. **Cookie BFF is pre-broken.** A51 allows cookie or bearer. `AllowAnyOrigin` + `AllowCredentials` is illegal. Switching on cookies later requires a **rewrite**, not a one-liner.
5. **SignalR is not saved by this policy.** `apps/web` already targets `/hubs/dashboard`. The API has **no** `AddSignalR` / `MapHub`. When a hub is added, WebSockets + `AllowCredentials` need a **named** origin list. `*` will not be enough.
6. **Production is not tightened.** CORS is not inside `if (app.Environment.IsDevelopment())`. Docker Compose (`D:\Prop\docker-compose.yml` lines 16–28) publishes `0.0.0.0:5000` with `ASPNETCORE_ENVIRONMENT: Development` — but even a Production environment variable would still ship `*`.

### 5.3 What a later coding task must do (not done here)

When a coding agent is authorized (this report does **not** edit product source):

| Do | Do not |
|---|---|
| `WithOrigins` from config, default `http://localhost:3000` | `AllowAnyOrigin()` in any environment that serves dashboard data |
| Separate Dev extra origins (Vite HTTPS / preview) via env | Hard-code production CDN origin in source without config |
| Named policy for REST; explicit SignalR CORS when `MapHub` lands | `*` + `AllowCredentials` (startup throw) |
| Keep CORS **on** — the Vite split is real until a proxy exists | Delete `UseCors` and hope same-origin |
| Gate mutations with auth **before** widening methods | Treat CORS as the security boundary |

---

## 6. Swagger — what is actually mapped

### 6.1 Registration vs middleware

| Step | Worktree | Notes |
|---|---|---|
| Package | `Swashbuckle.AspNetCore` 6.6.2 | HEAD and worktree. Enables Swagger + SwaggerUI + SwaggerGen. |
| `AddEndpointsApiExplorer()` | line 14 | required so minimal APIs appear in the document |
| `AddSwaggerGen()` | line 15 | default document name **`v1`**, no title, no security scheme, no XML comments |
| `UseSwagger()` | line 22–23, **Development only** | serves OpenAPI JSON (default route `/swagger/{documentName}/swagger.json`) |
| `UseSwaggerUI()` | **missing** | no `/swagger`, no `/swagger/index.html`, no redirect |
| `MapSwagger()` | **missing** | not using the endpoint-routing Swagger API |
| Production | no Swagger middleware | satisfies A63 “disable in production” **by omission** |

### 6.2 `launchUrl: swagger` is a 404

Worktree `Properties/launchSettings.json`:

| Profile | `applicationUrl` | `launchUrl` | What the browser hits |
|---|---|---|---|
| `http` | `http://localhost:5000` | `swagger` | `http://localhost:5000/swagger` → **404** (no UI middleware) |
| `https` | `https://localhost:7294;http://localhost:5000` | `swagger` | first URL `https://localhost:7294/swagger` → **404** |
| IIS Express | `http://localhost:18720` + SSL 44389 | **`weatherforecast`** | **404** (template route deleted in worktree `Program.cs`) |

HEAD `launchSettings.json` launched `weatherforecast` on all three profiles; `http` was `:5160`. The move to `swagger` + `:5000` is **uncommitted**, same as `Program.cs`.

In Development, the document that *would* exist (not probed this pass):

```text
GET /swagger/v1/swagger.json
```

That JSON would enumerate every anonymous map in this file (`/health`, `/ready`, `/api/*`, `POST /api/ops/resync`) plus the raw EF `/api/trades` shape. There is no Bearer security definition to even *document*. Compose `api` uses `ASPNETCORE_ENVIRONMENT: Development` on `0.0.0.0:5000`, so that JSON is on the published port if the process is that worktree.

### 6.3 Classification

| Question | Answer |
|---|---|
| Is Swashbuckle referenced? | **Yes** (6.6.2) |
| Is Swagger **used** on disk? | **Partial** — gen + `UseSwagger`, no UI |
| Is Swagger **committed**? | **No** |
| Does the operator get a clickable UI? | **No** — `launchUrl` is a lie |
| Is Production locked down? | **Yes, vacuously** — middleware not mapped |
| Is this A63-complete Dev OpenAPI? | **No** — no `/api/v1`, no auth scheme, no allow-list DTO contract |

B06-06 still holds: half-migration from weatherforecast. A06-09 (“Swagger not registered — avoids an extra anonymous surface”) is **stale for the worktree**. The extra surface now exists in Development as JSON only.

---

## 7. Pipeline position (why order matters)

Measured order after `builder.Build()`:

1. `UseCors()` — correct **before** endpoints; preflight can short-circuit.
2. `UseSwagger()` if Development — after CORS, so a browser on another origin can fetch `/swagger/v1/swagger.json` under the `*` policy.
3. Endpoint maps (`/health` … `POST /api/ops/resync`).
4. Startup `EnsureCreatedAsync` + `DemoSeeder.SeedAsync` (not HTTP middleware).
5. `app.Run()`.

Absent vs HEAD / vs a real BFF: `UseHttpsRedirection`, `UseAuthentication`, `UseAuthorization`, `UseSerilog`, `MapHub`. CORS sitting first without auth behind it is consistent with an open demo host, not with §59.

---

## 8. Prior reports — what this pass supersedes

| Report | Claim | This pass |
|---|---|---|
| A06 | No `AddCors` / no `AddSwaggerGen`; CORS “will block `apps/web` the day it appears”; Swagger unused | **True of HEAD. False of worktree.** |
| A55 / A63 §10.1 | Swashbuckle unused | **HEAD only.** Worktree uses gen + `UseSwagger`. |
| B06-05 | CORS `AllowAnyOrigin` + `AllowedHosts=*` + anonymous GETs | **Reconfirmed.** Still HIGH. Mutation makes it worse than GETs alone. |
| B06-06 | `launchUrl: swagger` without `UseSwaggerUI()` | **Reconfirmed.** |
| B08 / B10 / B30 / B41 | CORS any-origin lets `:3000` call `:5000` | **Reconfirmed.** That is a host hole, not a client feature. |
| B23 | IIS Express still `weatherforecast` | **Reconfirmed.** |

B06’s line count (91) ≠ this file (95). Same CORS/Swagger block. Do not treat the 4-line drift as a new policy.

---

## 9. Findings (this audit)

| ID | Sev | Finding |
|---|---|---|
| C22-01 | **HIGH** | Default CORS is `AllowAnyOrigin` + any header + any method, **not** env-gated. Violates A51 / A63. |
| C22-02 | **HIGH** | Same `*` policy covers anonymous `POST /api/ops/resync` and raw EF `GET /api/trades`. |
| C22-03 | **MED** | `AllowAnyOrigin` makes a future cookie BFF / credentialed SignalR illegal without a rewrite. |
| C22-04 | **MED** | `UseSwagger()` without `UseSwaggerUI()`; `launchUrl: swagger` is a 404. IIS Express still launches deleted `weatherforecast`. |
| C22-05 | **LOW** | Dev OpenAPI JSON (if the process is this worktree + Development) documents every unversioned route with no security scheme. Compose publishes `:5000` as Development. |
| C22-06 | **INFO** | HEAD still has neither CORS nor Swagger. Shipping from HEAD ≠ shipping this worktree. |
| C22-07 | **INFO** | Production Swagger UI is **not** mapped. Do not “fix” C22-04 by adding `UseSwaggerUI()` outside `IsDevelopment()` / SuperAdmin. |

---

## 10. Acceptance (later coding task — not this agent)

- [ ] `WithOrigins` from configuration; default includes `http://localhost:3000` only.
- [ ] Zero `AllowAnyOrigin()` on any dashboard-serving environment.
- [ ] `AllowedHosts` is not `*` outside local demo (A63 §10.6).
- [ ] Named CORS policy ready for SignalR when `/hubs/ops` (A63) is mapped; stub name `/hubs/dashboard` stays non-normative.
- [ ] `UseSwagger` + `UseSwaggerUI` **Development-only**, or Production UI locked to SuperAdmin (A63). Never anonymous Production UI.
- [ ] All three launch profiles: `launchUrl` is `health` **or** a working Dev swagger UI — never `weatherforecast`, never a 404 `swagger`.
- [ ] CORS + Swagger committed together with the host that needs them, or HEAD remains the template and operators must not be told “CORS is done.”

---

## 11. What this agent did **not** do

- Did not launch `TraderIntelligence.Api`, `dotnet run`, or Vite.
- Did not `curl` `/swagger`, `/swagger/v1/swagger.json`, or an `OPTIONS` preflight (C22-04 / default Swagger route are from source + Swashbuckle 6.6.2 conventions, not a live 404 capture).
- Did not edit `Program.cs`, `launchSettings.json`, `appsettings.json`, csproj, or `apps/web`.
- Did not rewrite `INDEX.md` / `SWARM_LOG.md` (this file is the assigned artifact).

*End of C22. Product source was not modified.*
