# D103 — CORS `AllowAnyOrigin` — is it OK for demo?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D103_cors.md` |
| Agent | D103 (senior engineer, CORS demo-policy only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:47:42+05:30 / 2026-08-18T08:17:42Z |
| Assigned | `CORS AllowAnyOrigin OK for demo?` Write this file. Do not modify product source. |
| Primary SUT | `D:\Prop\apps\api\Program.cs` (host is **not** under `D:\Prop\src`) |
| Product source modified | **No.** This report is the only write. |
| Method | Full read of `Program.cs`; SHA-256 + bytes + line endings of API/web/compose files; `git show HEAD:apps/api/Program.cs`; `git blame -L 16,21`; token census; live `Invoke-WebRequest` against the running host on `127.0.0.1:5000`; cross-read Vite client, dead `Cors:AllowedOrigins`, A51/A63/C22/C24/D30/D53. **Did not** POST `/api/ops/resync`. **Did not** edit product source. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |
| Relates | C22 (CORS+Swagger; **stale** on `appsettings` `Cors:` section), C24 (ports + earlier live CORS), D30 (15 maps), D53 (dead `Cors:AllowedOrigins`), A51 §11, A63 §10.6 |
| Supersedes | C22 on **config existence** (`Cors:AllowedOrigins` is now on disk and still unread). Does **not** supersede C22’s Swagger half-wire finding. |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Verdict

**No. `AllowAnyOrigin` is not OK for demo.**

It is **functionally sufficient** to make the README demo load (Vite `http://localhost:3000` → API `http://localhost:5000`). It is **not** an acceptable demo policy. The demo only needs one origin. The host ships `*`, unauthenticated, not env-gated, including the only write (`POST /api/ops/resync`).

| Question | Answer | Class |
|---|---|---|
| Does the Vite demo **need** some CORS? | **Yes.** Split origin, no Vite proxy, axios always sends `Content-Type: application/json` → every GET is a preflight. | `EXISTS_AND_GOOD` (the split) |
| Does the demo **need** `AllowAnyOrigin()`? | **No.** `WithOrigins("http://localhost:3000")` is enough. | — |
| Is worktree CORS `AllowAnyOrigin`? | **Yes.** `Program.cs` L16–17 + L21. Measured live `Access-Control-Allow-Origin: *`. | `UNSAFE` |
| Is that OK because “it is only a demo”? | **No.** Demo data is still an ops surface. `*` lets **any tab** in the operator’s browser read `/api/*` and preflight the resync POST. Compose publishes `0.0.0.0:5000`. Production is not tightened. | `UNSAFE` |
| Is committed HEAD this policy? | **No.** HEAD `apps/api/Program.cs` is still the weatherforecast template: **0** `AddCors`. | `MISSING` on HEAD |
| Dead allow-list in JSON? | **Yes.** `Cors:AllowedOrigins: ["http://localhost:5173"]` in both appsettings files. **Unread.** Wrong port (Vite is `:3000`). | `MISSING` binding / dead config |

Honest one-liner: **`*` makes the demo load. It is not required for the demo. It is UNSAFE. Demo-OK is `http://localhost:3000` only.**

Do **not** treat “CORS exists” or “the dashboard loaded” as PASS. Do **not** treat the unused `Cors:AllowedOrigins` JSON as the live policy.

---

## 1. Binding law (quoted)

A51 §11 (`D:\Prop\reports\swarm\20260818\A51_rbac_audit.md`):

> CORS | Explicit Vite origin; no `*` with credentials.

A63 §10 item 6 (`D:\Prop\reports\swarm\20260818\A63_api_catalog.md`):

> CORS: allow the Vite origin only. Do not leave `AllowedHosts=*` + anonymous dashboard.

README “Run (demo)” (`D:\Prop\README.md`):

> API: http://localhost:5000
> Dashboard: http://localhost:3000

That pair is **cross-origin**. Same-origin / Vite `server.proxy` (A62 / A65) is **not** implemented.

`AllowAnyOrigin` + `AllowCredentials` is illegal in ASP.NET Core (startup throw). Worktree omits credentials, so the host starts. Cookie BFF later **cannot** keep `*`.

---

## 2. File identity (remeasured this pass)

| Path | Bytes | Lines | Enc | SHA-256 | mtime |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\api\Program.cs` | **4731** | **95** | UTF-8 no BOM, LF | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 21 | UTF-8 no BOM, CRLF | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | 2026-08-18T12:55:15+05:30 |
| `D:\Prop\apps\api\appsettings.json` | **1254** | **50** | UTF-8 no BOM, LF | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 2026-08-18T13:37:36+05:30 |
| `D:\Prop\apps\api\appsettings.Development.json` | **478** | **21** | UTF-8 no BOM, CRLF | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | 2026-08-18T13:37:35+05:30 |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 41 | UTF-8 BOM, CRLF | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | 2026-08-18T13:32:01+05:30 |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | UTF-8 no BOM, LF | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | 2026-08-18T13:20:38+05:30 |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | UTF-8 no BOM, CRLF | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 |
| `D:\Prop\apps\web\vite.config.ts` | 169 | 7 | UTF-8 no BOM, CRLF | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | 2026-08-18T13:06:19+05:30 |
| `D:\Prop\docker-compose.yml` | 687 | 30 | UTF-8 no BOM, LF | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | 2026-08-18T13:18:40+05:30 |

C22’s `appsettings.json` row (`431` B / `8DCE4CBE…`) is **stale**. D30’s same hash is **stale**. Current JSON grew the unread `Cors:` / `RiskEngine` / `FeatureFlags` / `CTraderFix` slots (D53). `Program.cs` SHA matches D30 (`61B1E0D1…` / 95 lines).

`git status --porcelain` at measure: `M apps/api/Program.cs`, `M apps/api/Properties/launchSettings.json`, `M apps/api/appsettings.json`. CORS lines blame `Not Committed Yet`.

---

## 3. What the host actually registers

`D:\Prop\apps\api\Program.cs` lines 16–21:

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

app.UseCors();
```

Token census on this file (case-sensitive, exact):

| Token | Count |
|---|---:|
| `AddCors` | 1 |
| `AllowAnyOrigin` | 1 |
| `AllowAnyHeader` | 1 |
| `AllowAnyMethod` | 1 |
| `UseCors` | 1 |
| `WithOrigins` | **0** |
| `AllowCredentials` | **0** |
| `AddAuthentication` / `AddAuthorization` | **0** |
| `MapControllers` / `AddSignalR` / `MapHub` | **0** |
| `UseSwagger` | 1 |
| `UseSwaggerUI` | **0** |

| Property | Measured |
|---|---|
| Policy name | default (`AddDefaultPolicy`) — nameless `UseCors()` applies it to every endpoint |
| Origins | `AllowAnyOrigin()` → `Access-Control-Allow-Origin: *` |
| Headers / methods | any |
| Credentials | omitted (required: cannot combine with `*`) |
| Env gate | **none** — same `*` if `ASPNETCORE_ENVIRONMENT=Production` |
| Bound from `IConfiguration` / `Cors:AllowedOrigins` | **no** |
| Named SignalR policy | **no** |
| `AllowedHosts` | `"*"` in `appsettings.json` (host filter, not CORS; same hole class) |

`SettingsController.cs` exists under `apps/api/Controllers/` and is compiled by the Web SDK, but the host never `AddControllers` / `MapControllers`. Live `/api/settings` is the anonymous **minimal-API GET** in `Program.cs`, not the controller PUT. CORS still answers `OPTIONS` for `PUT` (see §5).

HEAD `apps/api/Program.cs` (first 30 lines measured via `git show`): weatherforecast template, `UseHttpsRedirection`, **zero** CORS. Shipping HEAD ≠ shipping this worktree.

---

## 4. Why the demo needs *some* CORS (not `*`)

| Piece | Path | Measured value |
|---|---|---|
| Vite listen | `apps/web/vite.config.ts` L6 | `server: { port: 3000 }` — **no** `proxy` |
| REST client | `apps/web/src/api/client.ts` L3–6 | `baseURL: import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` — **no** `withCredentials` |
| SignalR client | `apps/web/src/api/signalr.ts` L3, L10 | same base; `withUrl(\`${BASE}/hubs/dashboard\`)` — **no** hub on the API |
| API listen (launch `http`) | `launchSettings.json` | `http://localhost:5000` |
| API listen (this process) | PID 54468 `TraderIntelligence.Api.exe` | `--urls http://127.0.0.1:5000` — **loopback only** |
| Compose API | `docker-compose.yml` L21–25 | `--urls http://0.0.0.0:5000`, publish `5000:5000`, `ASPNETCORE_ENVIRONMENT: Development` — **no `web` service** |

Browser origin `http://localhost:3000` ≠ API origin `http://localhost:5000`. Axios sets `Content-Type: application/json` on the instance, so even GET triggers a CORS **preflight**. Without CORS middleware the demo dashboard cannot read `/api/overview`.

That requirement is satisfied by an **explicit** origin. `*` is a shortcut, not a demo constraint.

---

## 5. Live CORS (measured this pass — did not mutate)

Process: `TraderIntelligence.Api.exe` PID **54468**, command line `--urls http://127.0.0.1:5000`. Listen table: **`127.0.0.1:5000` only**. Debug DLL `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.dll` (42496 B, 2026-08-18 13:40:38) is newer than `Program.cs` and contains ASCII `AllowAnyOrigin` / `AllowAnyHeader` / `AllowAnyMethod` (1 each). No request was sent to execute `POST /api/ops/resync`.

| Request | Origin | Status | CORS headers |
|---|---|---:|---|
| `OPTIONS /api/overview` + `ACRM: GET` + `ACRH: content-type` | `http://localhost:3000` | **204** | `ACAO: *` `ACAM: GET` `ACAH: content-type` — no `ACAC` |
| `GET /api/overview` | `http://localhost:3000` | **200** | `ACAO: *` |
| `GET /api/overview` | `http://localhost:5173` (dead JSON origin) | **200** | `ACAO: *` |
| `GET /api/overview` | `http://127.0.0.1:3000` | **200** | `ACAO: *` |
| `GET /api/overview` | `http://evil.example` | **200** | `ACAO: *` |
| `GET /api/overview` | `null` | **200** | `ACAO: *` |
| `GET /api/overview` | `https://attacker.tld` | **200** | `ACAO: *` |
| `GET /health` | `http://evil.example` | **200** | `ACAO: *` |
| `OPTIONS /api/ops/resync` + `ACRM: POST` | `http://localhost:3000` | **204** | `ACAO: *` `ACAM: POST` `ACAH: content-type` |
| `OPTIONS /api/ops/resync` + `ACRM: POST` | `http://evil.example` | **204** | `ACAO: *` `ACAM: POST` `ACAH: content-type` |
| `OPTIONS /api/settings` + `ACRM: PUT` | `http://evil.example` | **204** | `ACAO: *` `ACAM: PUT` `ACAH: content-type` |

Reconfirms C24 §4.1 (204/200 + `*`) and extends it: **any Origin, including `null` and attacker hosts, is reflected as `*`.** Preflight for the unauthenticated write is allowed from `http://evil.example`. CORS answers `PUT` even though no PUT handler is mapped.

`localhost` vs `127.0.0.1` are **different** origins. A locked list of only `http://localhost:3000` would reject the `http://127.0.0.1:3000` SPA. That is correct. `*` hides that distinction.

---

## 6. Dead `Cors:AllowedOrigins` (wrong port, unread)

`appsettings.json` L20–22 and `appsettings.Development.json` L18–20:

```json
"Cors": {
  "AllowedOrigins": [ "http://localhost:5173" ]
}
```

| Fact | Measured |
|---|---|
| Bound in `Program.cs`? | **No** (`GetSection("Cors")` / `AllowedOrigins` / `WithOrigins` = 0 in product `*.cs`) |
| Vite default port 5173 used? | **No.** `vite.config.ts` forces **3000** |
| If someone wired `WithOrigins(config)` tomorrow without editing JSON? | Demo from `:3000` would **break**; only a stock Vite `:5173` origin would pass |
| Live policy today | `AllowAnyOrigin()` — the JSON cannot save you and cannot hurt you until it is bound |

Treat the JSON as **intent leftover**, not as a control. Binding it as-is is a foot-gun.

---

## 7. Why “it is only a demo” is not a waiver

### 7.1 CORS is not authentication

The host is fully anonymous (D30 / D53). Fourteen `MapGet` + one `MapPost`. No JWT, no cookie, no API key on inbound dashboard routes. `*` + anonymous means: **whoever can reach the TCP port can read the dashboard from browser JS on any page.**

Loopback bind on **this** process (`127.0.0.1:5000`) stops the WAN from connecting directly. It does **not** stop another origin open in the **same** browser from calling `http://127.0.0.1:5000`. That is the classic “dev server + `*`” hole. Because handlers are anonymous, the attacker page does not need cookies.

### 7.2 The write is in scope of `*`

`POST /api/ops/resync` reseeds/resyncs demo brokers and rebuilds scores for logins `10001, 10002, 10003, 99001`. Preflight from `http://evil.example` is **204 + `ACAO: *` + `ACAM: POST`**. That is an unauthenticated ops door (C04-07 / D06-05 / D30). Classification stays **UNSAFE** even if the body is fake Achiever/StarwaveFX data.

### 7.3 Compose / LAN is not loopback

`docker-compose.yml` publishes `0.0.0.0:5000`. Same `Program.cs` policy. A demo brought up with Compose is a LAN-open anonymous API with `*`. `AllowedHosts: "*"` does not compensate.

### 7.4 Production is not a different policy

`UseCors()` is **not** inside `if (app.Environment.IsDevelopment())`. Flipping the environment variable still ships `AllowAnyOrigin`. Compose even keeps `Development` while binding all interfaces.

### 7.5 Next slices are pre-broken

| Next slice | Collision with `*` |
|---|---|
| Cookie BFF / `AllowCredentials` | Framework throw; must drop `*` first (A51, C22, C47) |
| SignalR `/hubs/dashboard` (client already points here; API has no hub) | WebSockets + credentials need a named origin list; `*` is not enough (C28 / D50) |
| Mapping dead `SettingsController.Put` | Anonymous PUT of feature flags on `*` (D80) |
| Any live book / real password | Browser-readable from any origin that can reach the port |

### 7.6 What *would* be OK for demo

| Demo-OK | Not demo-OK |
|---|---|
| `WithOrigins("http://localhost:3000")` (optionally also `http://127.0.0.1:3000`) | `AllowAnyOrigin()` in any environment that serves `/api/*` |
| Bind `Cors:AllowedOrigins` from config **after** changing `5173` → `3000` | Bind the current `5173` list and call it done |
| Keep CORS **on** until a Vite proxy / same-origin BFF exists | Delete `UseCors` and hope |
| Gate `POST /api/ops/resync` (auth or loopback-only or remove) | Treat CORS as the security boundary |
| Env-stricter Production (fail closed if list empty) | Same `*` in Production |

`AllowAnyHeader` + `AllowAnyMethod` is acceptable **only** while the host stays a throwaway GET dashboard. Once mutations exist (they do), methods should be the ones the routes actually use.

---

## 8. Adjacent surfaces (not CORS, same hole class)

| Surface | Measured | CORS interaction |
|---|---|---|
| Auth / RBAC | `MISSING` | `*` multiplies anonymous reads/writes |
| `AllowedHosts` | `*` | Host-header filter open |
| Swagger | `AddSwaggerGen` always; `UseSwagger()` Dev only; **no** `UseSwaggerUI`; `launchUrl: swagger` → 404 | `*` would let another origin fetch `/swagger/v1/swagger.json` in Dev (C22) |
| SignalR | client only; no `MapHub` | future hub cannot keep `*` + credentials |
| `SettingsController` | compiled, unmapped | `OPTIONS PUT` already 204; mapping it would be another anonymous write |
| Secrets on the wire | none today (safe **by absence**, D30) | `*` does not leak a password that is not serialized; it **will** leak whatever is added later |

---

## 9. Drift vs earlier reports

| Report | Claim | D103 |
|---|---|---|
| A06 | no `AddCors`; Vite will be blocked | **True of HEAD. False of worktree.** |
| C22 | `AllowAnyOrigin`; no `Cors:` config bind | Policy **reconfirmed**. Config claim **stale** — JSON now has `5173`, still unread. |
| C22 / D30 `appsettings.json` SHA `8DCE4CBE…` / 431 B | old | **Stale.** Now `69D41CAD…` / 1254 B. |
| C24 §4.1 | OPTIONS 204 / GET 200 + `ACAO: *` from `:3000` | **Reconfirmed** on PID 54468. Extended: attacker Origin, `null`, POST resync preflight. |
| D30 | 15 anonymous maps; `*` UNSAFE as ops door | **Reconfirmed.** Same `Program.cs` SHA. |
| D53 | dead `Cors:AllowedOrigins` `5173`; Vite `:3000` | **Reconfirmed.** |
| C46 | `AllowAnyOrigin` UNSAFE (demo) | **Reconfirmed.** “Demo” is not a green cell. |

---

## 10. Findings

| ID | Sev | Finding |
|---|---|---|
| D103-01 | **HIGH** | Default CORS is `AllowAnyOrigin` + any header + any method, **not** env-gated. Violates A51 / A63. **Not OK for demo.** |
| D103-02 | **HIGH** | Live preflight: `OPTIONS /api/ops/resync` from `http://evil.example` → **204** `ACAO: *` `ACAM: POST`. Anonymous write is browser-callable from any origin that can reach the port. |
| D103-03 | **MED** | `Cors:AllowedOrigins` is dead and names **`:5173`**, not the real Vite **`:3000`**. Wiring it blindly breaks the demo. |
| D103-04 | **MED** | `AllowAnyOrigin` makes cookie BFF / credentialed SignalR illegal without a rewrite. |
| D103-05 | **MED** | Compose `0.0.0.0:5000` + this policy = LAN-open demo API. Current PID is loopback-only; that is a **process** accident, not a policy. |
| D103-06 | **INFO** | Some CORS **is** required for the documented `:3000` / `:5000` split. Do not delete `UseCors`. Replace `*` with the Vite origin. |
| D103-07 | **INFO** | HEAD still has neither CORS nor dashboard routes. Operators must not be told “CORS is done” from the committed tree. |
| D103-08 | **INFO** | This pass did **not** execute `POST /api/ops/resync`. Preflight is measured; the write body is inferred from `Program.cs` L73–81. |

---

## 11. Acceptance (CORS only — none met)

- [ ] Zero `AllowAnyOrigin()` on any dashboard-serving environment, including local demo.
- [ ] `WithOrigins` from configuration; default includes `http://localhost:3000` (not `5173`).
- [ ] Production fail-closed if the origin list is empty.
- [ ] No `AllowCredentials` while any `*` remains.
- [ ] `POST /api/ops/resync` is not anonymous (or is gone) **before** calling the demo “safe.”
- [ ] Named policy ready for SignalR when a hub is mapped.
- [ ] Committed HEAD matches the host operators are told to run, or docs say “worktree only.”

---

## 12. Non-goals / what this pass did not do

- Did not modify `Program.cs`, appsettings, Vite, or any product file.
- Did not add a Vite proxy.
- Did not launch Chromium; CORS was proven with `Invoke-WebRequest` `Origin` / `OPTIONS`.
- Did not re-audit Swagger UI (C22 still stands: JSON Dev-only, no UI).
- Did not treat INDEX / SWARM_LOG updates as this agent’s write (parent catalog).

---

## 13. Direct answer for the ticket

**`AllowAnyOrigin` is OK only as a one-line explanation of why the demo currently loads. It is not OK as a demo security or go-live policy.**

Replace it with an explicit Vite origin. Keep CORS enabled. Do not bind `http://localhost:5173` without also fixing the port. Do not ship `*` to Compose or Production. Do not call this PASS.
