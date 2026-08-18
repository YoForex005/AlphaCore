# E027 — `AllowAnyOrigin` is **not** “demo only”

| Field | Value |
|---|---|
| Agent | E027 (senior engineer, CORS `AllowAnyOrigin` / demo-only claim only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:10+05:30 (hashes / git / DLL / listen) / 2026-08-18T08:22:13Z (live `OPTIONS`/`GET`) / 2026-08-18T13:52:38+05:30 (port recensus) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Artifact | `D:\Prop\reports\swarm\20260818\E027_cors.md` |
| Assigned | `AllowAnyOrigin demo only.` Write this file. **Do not modify product source.** |
| Primary SUT | `D:\Prop\apps\api\Program.cs` (host is **not** under `D:\Prop\src`) |
| Product source modified | **No.** This report is the only write. |
| Config / `.env*` / `appsettings` / launchSettings / Vite edited | **No.** |
| INDEX / SWARM_LOG rewritten by this agent | **No.** Orchestrator catalog duty. |
| Method | Full read of worktree `Program.cs`; SHA-256 + bytes + newline + LastWrite of API/web/compose/README; token census of every product `*.cs` under `apps/` + `src/` (exclude `obj`/`bin`); ASCII strings in Debug DLL; `git show HEAD:apps/api/Program.cs` + `git blame -L 16,21`; `Win32_Process` command lines; `Get-NetTCPConnection` on 3000/5000/5173/4173/5160/7294/18720; live `Invoke-WebRequest` GET/OPTIONS against the **already-running** host on `127.0.0.1:5000`. **Did not** `POST /api/ops/resync`. **Did not** start or kill Kestrel/Vite. **Did not** edit product source. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |
| Relates | C22 (CORS + Swagger; **stale** on `Cors:` JSON and on “did not launch”), C24 / E012 (port pair), E015 (Vite `:3000` pin), D30 (15 maps), D53 (dead `Cors:AllowedOrigins`), D103 (demo-policy; **reconfirmed**, this pass adds the Production-default smoke test), A51 §11, A63 §10.6 |
| Does not supersede | C22 Swagger-half-wire **source** claim. Live `/swagger/v1/swagger.json` is **404** on *this* process (see §5.2) because the host is not Development. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

This is a **read-only confirmation** of the assigned label. It does not rewrite `Program.cs`, `appsettings*.json`, Vite, or Compose.

---

## 0. Verdict (binding — do not greenwash)

**Rejected. `AllowAnyOrigin` is demo *glue*, not a demo-only *gate*.**

The worktree default CORS policy is `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`, applied globally by nameless `UseCors()`. That is enough for the README split (`http://localhost:3000` → `http://localhost:5000`). It is **not** restricted to demo, Development, loopback, or the Vite origin.

| Assigned / implied claim | Measured result | Class |
|---|---|---|
| Live policy is `AllowAnyOrigin()` | **TRUE.** `Program.cs` L16–17 + L21. Live `Access-Control-Allow-Origin: *`. Debug DLL contains the three `AllowAny*` ASCII strings (1 each). | `UNSAFE` |
| That policy is “demo only” (env-gated / Production-closed) | **FALSE.** `UseCors()` is **outside** `if (app.Environment.IsDevelopment())`. The only `IsDevelopment()` branch is `UseSwagger()`. | `UNSAFE` |
| The demo **needs** `*` | **FALSE.** The demo needs *some* CORS. `WithOrigins("http://localhost:3000")` is enough. Axios has **no** `withCredentials`. | — |
| The demo **needs** some CORS | **TRUE.** Split origin, no Vite `server.proxy`, axios always sends `Content-Type: application/json` → every GET is a preflight. | `EXISTS_AND_GOOD` (the split) |
| Committed HEAD is this policy | **FALSE.** HEAD `apps/api/Program.cs` is still the weatherforecast template: **0** `AddCors` / `UseCors` / `AllowAnyOrigin`. | `MISSING` on HEAD |
| Dead allow-list in JSON is the live policy | **FALSE.** `Cors:AllowedOrigins: ["http://localhost:5173"]` exists in both appsettings files and is **unread**. Vite is `:3000`. `:5173` is **not listening**. | `MISSING` binding / dead config |

**Honest one-liner:** `*` makes the local dashboard load. It is **not** required for that load. It is **not** demo-gated. The process measured on this host was started with `--no-launch-profile` and **no** `ASPNETCORE_ENVIRONMENT` (ASP.NET Core default = **Production**) and still answers `Access-Control-Allow-Origin: *`. That is the opposite of “demo only.”

Do **not** treat “CORS exists,” “the dashboard loaded,” or the unused `Cors:AllowedOrigins` JSON as PASS. Do **not** treat D103 / C22 as stale on the policy — they are **reconfirmed**. This pass’s new fact is the **Production-default live host**.

---

## 1. Binding law (quoted)

A51 §11 (`D:\Prop\reports\swarm\20260818\A51_rbac_audit.md`):

> CORS | Explicit Vite origin; no `*` with credentials.

A63 §10 item 6 (`D:\Prop\reports\swarm\20260818\A63_api_catalog.md`):

> CORS: allow the Vite origin only. Do not leave `AllowedHosts=*` + anonymous dashboard.

README “Run (demo)” (`D:\Prop\README.md` L32–L43):

> API: http://localhost:5000
> Dashboard: http://localhost:3000

That pair is **cross-origin**. Same-origin / Vite `server.proxy` (A62 / A65) is **not** implemented (`vite.config.ts` is 7 lines: plugin + `port: 3000` only).

ASP.NET Core 8: `AllowAnyOrigin()` + `AllowCredentials()` throws at startup. Worktree omits credentials, so the host starts. A later cookie BFF **cannot** keep `*`.

“Demo only” as a **security waiver** is not in A51, A63, or the README. README “Run (demo)” names the **two URLs**, not a CORS policy.

---

## 2. File identity (remeasured this pass)

| Path | Bytes | Phys. lines | Enc | SHA-256 | LastWrite local |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\api\Program.cs` | **4731** | **95** | UTF-8 no BOM, LF | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 21 | UTF-8 no BOM, CRLF | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | 2026-08-18T12:55:15+05:30 |
| `D:\Prop\apps\api\appsettings.json` | **1254** | **50** | UTF-8 no BOM, LF | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 2026-08-18T13:37:36+05:30 |
| `D:\Prop\apps\api\appsettings.Development.json` | **478** | **21** | UTF-8 no BOM, CRLF | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | 2026-08-18T13:37:35+05:30 |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 41 | UTF-8 BOM, CRLF | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | 2026-08-18T13:32:01+05:30 |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | UTF-8 no BOM, LF | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | 2026-08-18T13:20:38+05:30 |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | 3732 | 94 | UTF-8 no BOM, CRLF | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | 2026-08-18T13:37:39+05:30 |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | UTF-8 no BOM, CRLF | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | 28 | UTF-8 no BOM, CRLF | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | 2026-08-18T13:08:02+05:30 |
| `D:\Prop\apps\web\vite.config.ts` | 169 | 7 | UTF-8 no BOM, CRLF | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | 2026-08-18T13:06:19+05:30 |
| `D:\Prop\docker-compose.yml` | 687 | 30 | UTF-8 no BOM, LF | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | 2026-08-18T13:18:40+05:30 |
| `D:\Prop\README.md` | 1746 | 49 | UTF-8 no BOM, LF | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | 2026-08-18T13:26:07+05:30 |
| `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.dll` | 42496 | — | PE | `B346B3B7143779C648EBEB60325A7C0E3F7491BCBDEA89255DBC661C65F9ED75` | 2026-08-18T13:40:38+05:30 |

Git vs HEAD `398a142`:

| Path | Porcelain |
|---|---|
| `apps/api/Program.cs` | ` M` (unstaged). CORS lines `git blame -L 16,21` = **`Not Committed Yet`**. L18–20 still `^6c41447`. |
| `apps/api/Properties/launchSettings.json` | ` M` |
| `apps/api/appsettings.json` | ` M` (HEAD blob is template Logging + `AllowedHosts: "*"`, **no** `Cors:` section) |
| `apps/api/appsettings.Development.json` | **gitignored** (`.gitignore:33`); **not in HEAD**; present on disk |
| `docker-compose.yml` | `??` untracked (not ignored) |
| `apps/web/src/api/client.ts`, `vite.config.ts` | clean vs this status slice (already in tree or previously recorded) |

`Program.cs` SHA matches D30 / D103 / E003 (`61B1E0D1…` / 95 LF lines). C22’s older SHA `E914FA98…` / 4658 B is **stale** (same CORS block, later maps grew the file).

---

## 3. What the host actually registers

`D:\Prop\apps\api\Program.cs` L14–23:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

app.UseCors();
if (app.Environment.IsDevelopment())
    app.UseSwagger();
```

That is the entire CORS story. There is no second policy, no config bind, no env `if`.

### 3.1 Token census — `Program.cs`

| Token | Count |
|---:|---:|
| `AddCors` / `AddDefaultPolicy` | 1 / 1 |
| `AllowAnyOrigin` / `AllowAnyHeader` / `AllowAnyMethod` | 1 / 1 / 1 |
| `UseCors` | 1 |
| `WithOrigins` | **0** |
| `AllowCredentials` | **0** |
| `SetIsOriginAllowed` | **0** |
| `GetSection("Cors")` / `AllowedOrigins` | **0** / **0** |
| `AddAuthentication` / `AddAuthorization` | **0** / **0** |
| `AddSignalR` / `MapHub` / `AddControllers` / `MapControllers` | **0** |
| `UseSwagger` / `UseSwaggerUI` | 1 / **0** |
| `IsDevelopment` | **1** (wraps `UseSwagger` **only**) |
| `MapGet` / `MapPost` / `MapPut` | **14** / **1** / **0** |
| `RequireAuthorization` / `[Authorize]` | **0** |

### 3.2 Token census — all product `*.cs` under `D:\Prop\apps` + `D:\Prop\src` (exclude `obj`/`bin`)

| Token | Hits | Where |
|---|---:|---|
| `AllowAnyOrigin` | **1** | `apps/api/Program.cs:17` |
| `AddCors` | **1** | `apps/api/Program.cs:16` |
| `UseCors` | **1** | `apps/api/Program.cs:21` |
| `WithOrigins` | **0** | — |
| `AllowCredentials` | **0** | — |
| `GetSection("Cors")` | **0** | — |
| `AllowedOrigins` | **0** in `*.cs` (JSON only) | — |

CORS is **one file, three calls**. There is no “demo policy” and no “production policy.”

### 3.3 Policy object

| Knob | Value | Runtime effect (ASP.NET Core 8 default policy) |
|---|---|---|
| Policy name | default (`AddDefaultPolicy`) | nameless `UseCors()` applies it to **every** endpoint |
| Origins | `AllowAnyOrigin()` | `Access-Control-Allow-Origin: *` |
| Headers | `AllowAnyHeader()` | reflects `Access-Control-Request-Headers` |
| Methods | `AllowAnyMethod()` | GET / POST / PUT / PATCH / DELETE / OPTIONS / … |
| Credentials | omitted | **cannot** add without dropping `*` (startup throw) |
| Env gate | **none** | Production Kestrel gets `*` too |
| Bound from `IConfiguration` | **no** | `Cors:AllowedOrigins` is dead |
| Named SignalR policy | **no** | client already dials `/hubs/dashboard`; host has no hub |
| `AllowedHosts` | `"*"` in `appsettings.json` L49 | host-header filter, **not** CORS; same hole class |

`SettingsController` is compiled by the Web SDK and is **not** mapped (`AddControllers`/`MapControllers` = 0). Live `/api/settings` is the anonymous **minimal-API GET**. CORS still answers `OPTIONS` for `PUT` (see §5).

### 3.4 Built DLL (what this process is actually running)

`TraderIntelligence.Api.dll` (42496 B, 2026-08-18 13:40:38) is **newer** than `Program.cs` (13:35:15). ASCII string scan:

| String | Present |
|---|---|
| `AllowAnyOrigin` | **yes** (1) |
| `AllowAnyHeader` | **yes** (1) |
| `AllowAnyMethod` | **yes** (1) |
| `WithOrigins` / `AllowCredentials` | **no** |

The running binary matches the worktree policy.

### 3.5 HEAD (committed) is not this host

`git show HEAD:apps/api/Program.cs` is the weatherforecast template: `UseHttpsRedirection` + `MapGet("/weatherforecast")`. Token counts on that blob: `AddCors` = 0, `AllowAnyOrigin` = 0, `UseCors` = 0, `AddSwaggerGen` = 0, `UseSwagger` = 0.

HEAD `launchSettings.json`: `http` = `http://localhost:5160`, `launchUrl` = `weatherforecast` on all three profiles.

Operators told “CORS is `*` / demo only” from **HEAD** are being lied to. Shipping HEAD ≠ shipping this worktree.

---

## 4. Why the demo needs *some* CORS (not `*`)

| Piece | Path | Measured value |
|---|---|---|
| Vite listen (disk) | `apps/web/vite.config.ts` L6 | `server: { port: 3000 }` — **no** `proxy` |
| Vite listen (this host) | TCP | `127.0.0.1:3000` Listen, pid **49100** |
| REST client | `apps/web/src/api/client.ts` L3–6 | `baseURL: import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` — **no** `withCredentials` |
| SignalR client | `apps/web/src/api/signalr.ts` L3, L10 | same base; `withUrl(\`${BASE}/hubs/dashboard\`)` — **no** hub on the API |
| API listen (launch `http`) | `launchSettings.json` | `http://localhost:5000` |
| API listen (this process) | pid **54468** | `--urls http://127.0.0.1:5000` — **loopback only** |
| Parent launch | pid **53816** / **43488** | `dotnet run --project …\TraderIntelligence.Api.csproj --urls http://127.0.0.1:5000 --no-launch-profile` |
| Compose API (disk, not running) | `docker-compose.yml` L16–25 | `--urls http://0.0.0.0:5000`, publish `5000:5000`, `ASPNETCORE_ENVIRONMENT: Development` — **no `web` service** |
| Vite factory 5173 | this host | **NOLISTEN** |
| HEAD leftover 5160 / https 7294 / IIS 18720 | this host | **NOLISTEN** |

Browser origin `http://localhost:3000` ≠ API origin `http://localhost:5000`. Axios sets `Content-Type: application/json` on the instance, so even GET triggers a CORS **preflight**. Without CORS middleware the dashboard cannot read `/api/overview`.

That requirement is satisfied by an **explicit** origin. `*` is a shortcut, not a demo constraint. `localhost` and `127.0.0.1` are **different** origins; a locked list of only `http://localhost:3000` would correctly reject `http://127.0.0.1:3000`. `*` hides that distinction.

E012 / E015 already pin the port pair. E027 does **not** reopen “is 3000 vs 5000 a bug?” — it is not.

---

## 5. Live CORS (measured this pass — did not mutate)

### 5.1 Process

| Item | Measured |
|---|---|
| Binary | `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.exe` |
| PID | **54468** |
| Command line | `"…\TraderIntelligence.Api.exe" --urls http://127.0.0.1:5000` |
| Parent | `dotnet.exe` **53816** → `powershell.exe` **43488** `dotnet run … --no-launch-profile` |
| Listen | **`127.0.0.1:5000` only** |
| `ASPNETCORE_ENVIRONMENT` (this measuring shell: Process / User / Machine) | **absent** |
| Launch profile | **none** (`--no-launch-profile`) |
| Implied hosted environment | ASP.NET Core default **Production** when the env var and launch profile are both absent |
| Proof of non-Development | `GET /swagger/v1/swagger.json` → **404** (the only `IsDevelopment()` middleware is `UseSwagger()`) |

Loopback bind stops the WAN from connecting **directly**. It does **not** stop another origin open in the **same** browser from calling `http://127.0.0.1:5000`. Handlers are anonymous, so the attacker page does not need cookies.

### 5.2 Responses (no `POST /api/ops/resync` body was sent)

| Request | Origin | Status | CORS headers |
|---|---|---:|---|
| `GET /health` | `http://localhost:3000` | **200** | `ACAO: *` — no `ACAC` |
| `GET /health` | `http://evil.example` | **200** | `ACAO: *` |
| `GET /api/overview` | `http://localhost:3000` | **200** | `ACAO: *` |
| `GET /api/overview` | `http://localhost:5173` (dead JSON origin) | **200** | `ACAO: *` |
| `GET /api/overview` | `http://127.0.0.1:3000` | **200** | `ACAO: *` |
| `GET /api/overview` | `http://evil.example` | **200** | `ACAO: *` |
| `GET /api/overview` | `null` | **200** | `ACAO: *` |
| `GET /api/overview` | `https://attacker.tld` | **200** | `ACAO: *` |
| `GET /api/overview` | *(header omitted)* | **200** | **no** `ACAO` (correct: no CORS request) |
| `GET /api/settings` | `http://localhost:3000` | **200** | `ACAO: *` |
| `GET /ready` | `http://localhost:3000` | **200** | `ACAO: *` |
| `OPTIONS /api/overview` + `ACRM: GET` + `ACRH: content-type` | `http://localhost:3000` | **204** | `ACAO: *` `ACAM: GET` `ACAH: content-type` |
| `OPTIONS /api/overview` + `ACRM: GET` | `http://evil.example` | **204** | `ACAO: *` `ACAM: GET` `ACAH: content-type` |
| `OPTIONS /api/ops/resync` + `ACRM: POST` | `http://localhost:3000` | **204** | `ACAO: *` `ACAM: POST` `ACAH: content-type` |
| `OPTIONS /api/ops/resync` + `ACRM: POST` | `http://evil.example` | **204** | `ACAO: *` `ACAM: POST` `ACAH: content-type` |
| `OPTIONS /api/settings` + `ACRM: PUT` | `http://evil.example` | **204** | `ACAO: *` `ACAM: PUT` `ACAH: content-type` |
| `GET /swagger/v1/swagger.json` | `http://evil.example` | **404** | — |
| `GET /swagger` | `http://localhost:3000` | **404** | — |
| `GET /swagger/index.html` / `/openapi/v1.json` | — | **404** | — |

Exact `OPTIONS /api/ops/resync` response headers from `http://evil.example` (2026-08-18T08:22:13Z):

```text
Access-Control-Allow-Headers=content-type
Access-Control-Allow-Methods=POST
Access-Control-Allow-Origin=*
Server=Kestrel
```

No `Access-Control-Allow-Credentials`. No `Vary: Origin` (typical of `*`).

**Any Origin, including `null` and attacker hosts, is reflected as `*`.** Preflight for the unauthenticated write is allowed from `http://evil.example`. CORS answers `PUT` even though no PUT handler is mapped.

Swagger 404 on this process is **not** “Swagger is gone from source.” Source still has `UseSwagger()` inside `IsDevelopment()`. This process is not Development. CORS still fired. That is the “demo only” claim dying in production default.

---

## 6. Dead `Cors:AllowedOrigins` (wrong port, unread)

`appsettings.json` L20–22 and `appsettings.Development.json` L18–20 (disk; Dev file is **gitignored**):

```json
"Cors": {
  "AllowedOrigins": [ "http://localhost:5173" ]
}
```

| Fact | Measured |
|---|---|
| Bound in `Program.cs`? | **No** (`GetSection("Cors")` / `AllowedOrigins` / `WithOrigins` = 0 in product `*.cs`) |
| HEAD `appsettings.json` has `Cors:`? | **No.** HEAD is Logging + `AllowedHosts: "*"` only |
| Vite default port 5173 used? | **No.** `vite.config.ts` forces **3000**. TCP **NOLISTEN** on 5173 |
| If someone wired `WithOrigins(config)` tomorrow without editing JSON? | Demo from `:3000` would **break**; only a stock Vite `:5173` origin would pass |
| Live policy today | `AllowAnyOrigin()` — the JSON cannot save you and cannot hurt you until it is bound |

Treat the JSON as **intent leftover**, not as a control. Binding it as-is is a foot-gun. E015 already forbids “fix Vite onto 5173 because the JSON says so.”

`AllowedHosts: "*"` remains in worktree `appsettings.json` L49 (and in HEAD). Host filter `*` + CORS `*` + anonymous maps is the A63 §10.6 anti-pattern, not a demo exception.

---

## 7. Why “it is only a demo” is not a waiver

### 7.1 “Demo only” would require a gate. There is none.

A policy is demo-only if at least one of these is true:

| Gate that would make the label true | Present? |
|---|---|
| `UseCors()` / `AllowAnyOrigin()` inside `IsDevelopment()` only | **No.** `UseCors()` is unconditional. `IsDevelopment()` wraps **Swagger only**. |
| Production policy = `WithOrigins(config)` fail-closed | **No.** |
| Named `Demo` policy applied only to local maps | **No.** Default policy, every endpoint. |
| Compose / launch restricted to loopback **in source** | **No.** Compose publishes `0.0.0.0:5000`. This process is loopback by **operator command line**, not by policy. |
| Auth in front of `/api/*` | **No.** 14 `MapGet` + 1 `MapPost`, all anonymous. |

The live host this pass is the counter-example: `--no-launch-profile`, no `ASPNETCORE_ENVIRONMENT`, Swagger 404 (not Dev), CORS still `*`.

### 7.2 CORS is not authentication

The host is fully anonymous (D30 / D53 / E003). `*` + anonymous means: **whoever can reach the TCP port can read the dashboard from browser JS on any page.** Demo seed data is still an ops surface (broker ids, reconstructed trades, scores, the resync POST).

### 7.3 The write is in scope of `*`

`POST /api/ops/resync` (`Program.cs` L73–81) syncs `ACHIEVER` + `STARWAVEFX` from 2026-01-01 and rebuilds scores for logins `10001, 10002, 10003, 99001`. Preflight from `http://evil.example` is **204 + `ACAO: *` + `ACAM: POST`**. Classification stays **UNSAFE** even if the body is fake Achiever/StarwaveFX data. This pass did **not** execute the POST.

### 7.4 Compose / LAN is not loopback

`docker-compose.yml` (untracked, 687 B) publishes `0.0.0.0:5000` and sets `ASPNETCORE_ENVIRONMENT: Development`. Same `Program.cs` policy. A demo brought up with Compose is a LAN-open anonymous API with `*`. Compose keeping `Development` does **not** turn `AllowAnyOrigin` into a demo gate — CORS is not inside that `if`.

### 7.5 Next slices are pre-broken

| Next slice | Collision with `*` |
|---|---|
| Cookie BFF / `AllowCredentials` | Framework throw; must drop `*` first (A51, C22, C47) |
| SignalR `/hubs/dashboard` (client already points here; API has no hub) | WebSockets + credentials need a named origin list; `*` is not enough (C28 / D50) |
| Mapping dead `SettingsController.Put` | Anonymous PUT of feature flags on `*` (D80). `OPTIONS PUT` is already 204 from `evil.example`. |
| Any live book / real password | Browser-readable from any origin that can reach the port |

### 7.6 What *would* be OK for the local demo

| Demo-OK | Not demo-OK (current) |
|---|---|
| `WithOrigins("http://localhost:3000")` (optionally also `http://127.0.0.1:3000`) | `AllowAnyOrigin()` in **any** environment that serves `/api/*` |
| Bind `Cors:AllowedOrigins` from config **after** changing `5173` → `3000` | Bind the current `5173` list and call it done |
| Keep CORS **on** until a Vite proxy / same-origin BFF exists | Delete `UseCors` and hope |
| Gate `POST /api/ops/resync` (auth or loopback-only or remove) | Treat CORS as the security boundary |
| Env-stricter Production (fail closed if list empty) | Same `*` when env is Production (measured) |
| `AllowAnyHeader` + `AllowAnyMethod` only while the host is throwaway GETs | Mutations exist (`POST /api/ops/resync`); methods should be the ones the routes use |

---

## 8. Adjacent surfaces (not CORS, same hole class)

| Surface | Measured | CORS interaction |
|---|---|---|
| Auth / RBAC | `MISSING` | `*` multiplies anonymous reads/writes |
| `AllowedHosts` | `*` | Host-header filter open |
| Swagger **source** | `AddSwaggerGen` always; `UseSwagger()` Dev only; **no** `UseSwaggerUI`; `launchUrl: swagger` | C22 still stands as source audit |
| Swagger **this process** | `/swagger` and `/swagger/v1/swagger.json` = **404** | Because env is not Development. CORS still `*`. Do not cite this 404 as “Swagger removed.” |
| SignalR | client only; `POST /hubs/dashboard/negotiate` expected 404 (E012) | future hub cannot keep `*` + credentials |
| `SettingsController` | compiled, unmapped | `OPTIONS PUT` already 204 |
| Secrets on the wire | none today (safe **by absence**, E001 / D30) | `*` does not leak a password that is not serialized; it **will** leak whatever is added later |
| Real `35=D` | off (E002) | CORS is independent of FIX send |

---

## 9. Drift vs earlier reports

| Report | Claim | E027 |
|---|---|---|
| A06 | no `AddCors`; Vite will be blocked | **True of HEAD. False of worktree.** |
| C22 | `AllowAnyOrigin`; no `Cors:` config bind; did not launch Kestrel | Policy **reconfirmed**. Config claim **stale** — JSON now has `5173`, still unread. Live HTTP now exists. |
| C22 `Program.cs` SHA `E914FA98…` / 4658 B | old | **Stale.** Now `61B1E0D1…` / 4731 B / 95 lines. Same CORS block. |
| C24 §4.1 / E012 | OPTIONS 204 / GET 200 + `ACAO: *` from `:3000` | **Reconfirmed** on PID 54468. |
| D30 | 15 anonymous maps; `*` UNSAFE as ops door | **Reconfirmed.** Same `Program.cs` SHA. |
| D53 / D64 / E015 | dead `Cors:AllowedOrigins` `5173`; Vite `:3000` | **Reconfirmed.** 5173 **NOLISTEN**. |
| D103 | `AllowAnyOrigin` not OK for demo; PID 54468 | **Reconfirmed.** Same PID still up. E027 adds: `--no-launch-profile` + missing env + Swagger 404 = **Production default still serves `*`.** |
| C46 | `AllowAnyOrigin` UNSAFE (demo) | **Reconfirmed.** “Demo” is not a green cell. |

---

## 10. Findings

| ID | Sev | Finding |
|---|---|---|
| E027-01 | **HIGH** | Default CORS is `AllowAnyOrigin` + any header + any method, **not** env-gated. Violates A51 / A63. The assigned label “demo only” is **false**. |
| E027-02 | **HIGH** | Live host started `--no-launch-profile` with **no** `ASPNETCORE_ENVIRONMENT` (default Production). `UseSwagger()` correctly stays off (`/swagger/v1/swagger.json` = 404). `UseCors()` still emits `ACAO: *`. That is measured proof the policy is not Development-only. |
| E027-03 | **HIGH** | Live preflight: `OPTIONS /api/ops/resync` from `http://evil.example` → **204** `ACAO: *` `ACAM: POST`. Anonymous write is browser-callable from any origin that can reach the port. |
| E027-04 | **MED** | `Cors:AllowedOrigins` is dead and names **`:5173`**, not the real Vite **`:3000`**. Wiring it blindly breaks the demo. Dev JSON is gitignored; committed `appsettings.json` HEAD has **no** `Cors:` section. |
| E027-05 | **MED** | `AllowAnyOrigin` makes cookie BFF / credentialed SignalR illegal without a rewrite. |
| E027-06 | **MED** | Compose `0.0.0.0:5000` + this policy = LAN-open demo API. Current PID is loopback-only; that is a **process** accident (`--urls http://127.0.0.1:5000`), not a policy. |
| E027-07 | **INFO** | Some CORS **is** required for the documented `:3000` / `:5000` split. Do not delete `UseCors`. Replace `*` with the Vite origin. |
| E027-08 | **INFO** | HEAD still has neither CORS nor dashboard routes. Operators must not be told “CORS is demo-only” from the committed tree. |
| E027-09 | **INFO** | This pass did **not** execute `POST /api/ops/resync`. Preflight is measured; the write body is inferred from `Program.cs` L73–81. |

---

## 11. Acceptance (CORS only — **none** met)

- [ ] Zero `AllowAnyOrigin()` on any dashboard-serving environment, including local demo **and** Production default.
- [ ] `WithOrigins` from configuration; default includes `http://localhost:3000` (not `5173`).
- [ ] Production fail-closed if the origin list is empty.
- [ ] No `AllowCredentials` while any `*` remains.
- [ ] `POST /api/ops/resync` is not anonymous (or is gone) **before** calling the demo “safe.”
- [ ] Named policy ready for SignalR when a hub is mapped.
- [ ] Committed HEAD matches the host operators are told to run, or docs say “worktree only.”
- [ ] The phrase “demo only” is not used unless `UseCors` / origin list is actually env-gated.

---

## 12. Non-goals / what this pass did not do

- Did not modify `Program.cs`, appsettings, Vite, Compose, `.gitignore`, or any product file.
- Did not add a Vite proxy.
- Did not launch or kill Chromium, Kestrel, or Vite; CORS was proven with `Invoke-WebRequest` `Origin` / `OPTIONS` against the already-running PID 54468.
- Did not `POST /api/ops/resync`.
- Did not start Compose / IIS Express / the `https` profile.
- Did not treat INDEX / SWARM_LOG updates as this agent’s write.

---

## 13. Direct answer for the ticket

**`AllowAnyOrigin` is not “demo only.”**

It is the **unconditional** default policy of the uncommitted worktree host. It explains why the README demo currently loads. It is not a demo security policy, not an `IsDevelopment()` branch, and not go-live. The process measured on this machine was running under Production default and still served `*`.

Replace `*` with `http://localhost:3000`. Keep CORS enabled. Do not bind `http://localhost:5173` without also fixing the port. Do not ship `*` to Compose or Production. Do not call this PASS.

*End of E027. Product source was not modified.*
