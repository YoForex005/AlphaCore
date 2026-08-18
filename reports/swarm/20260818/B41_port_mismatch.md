# B41 — API `launchSettings` vs web client port 5000

| Field | Value |
|---|---|
| Agent | B41 (senior engineer, port-mismatch only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\B41_port_mismatch.md` |
| Workspace | `D:\Prop` |
| Left | `D:\Prop\apps\api\Properties\launchSettings.json` |
| Right | `apps/web` axios / SignalR fallback `http://localhost:5000` |
| Product source modified | **No.** This report is the only write. |
| Method | Read launchSettings + every web/API/compose/README bind that names a localhost port. SHA-256 + byte census. No `dotnet run`. No HTTP probe. |
| Relates | A06 (stale `:5160`), A54, A62, A63, A65, B06, B10, B23, B37 |
| Classification vocabulary | Architecture §73.B |

---

## 0. Verdict

**The historically cited Kestrel `:5160` vs web `:5000` mismatch is CLOSED.**

On disk today the default `http` profile, README, `.http` sample, and Compose `api` service all bind **`http://localhost:5000`**, which is exactly what the web client uses when `VITE_API_URL` is unset.

**A live profile mismatch remains and is flagged:**

| Listener | Port | vs web `:5000` |
|---|---|---|
| `profiles.http.applicationUrl` | `http://localhost:5000` | **MATCH** |
| `profiles.https.applicationUrl` | `https://localhost:7294` **and** `http://localhost:5000` | **PARTIAL** — HTTP half matches; first URL is `:7294` |
| `iisSettings.iisExpress.applicationUrl` | `http://localhost:18720` | **MISMATCH** |
| `iisSettings.iisExpress.sslPort` | `44389` | **MISMATCH** |

If Visual Studio / Rider launches **IIS Express**, the API is **not** on `:5000`. The dashboard still calls `:5000`. Overview then shows “API unavailable. Start the ASP.NET API on port 5000.”

Do **not** treat A06 / A54 / A63 / A65 “API is `:5160`” as current. Those reports are stale. Do **not** implement A62’s Vite proxy target `http://localhost:5160` — that would **re-open** a closed mismatch.

Do **not** edit product source from this agent.

---

## 1. Method

1. Read `D:\Prop\apps\api\Properties\launchSettings.json` in full (authoritative listen table).
2. Read web consumers: `client.ts`, `signalr.ts`, `OverviewPage.tsx`, `vite.config.ts`. Confirmed `hooks.ts` `5000` values are **refetch intervals (ms)**, not ports.
3. Read adjacent operators: `TraderIntelligence.Api.http`, `Program.cs` (`app.Run()` only — no `UseUrls` / `Listen`), `docker-compose.yml`, `README.md`.
4. Confirm absence of `apps/web/.env*`, Vite `server.proxy`, `ASPNETCORE_URLS` in launchSettings, and `LaunchProfile` / `applicationUrl` in `Directory.Build.props` / `.csproj`.
5. Hash the measured files. Cross-check A-wave / B06 / B10 / B23 / B37 claims against **today’s** hashes, not their quoted snippets.

API was **not** started. Occupancy of host `:5000` was **not** measured.

---

## 2. `launchSettings.json` as measured

Path: `D:\Prop\apps\api\Properties\launchSettings.json`  
1133 bytes, 41 lines, SHA-256 `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78`  
(same hash as B06 / B23 — this file has **not** moved since those reports.)

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:18720",
      "sslPort": 44389
    }
  },
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7294;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "weatherforecast",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Notes that are **not** ports but sit next to them:

| Setting | Value | Port impact |
|---|---|---|
| First JSON profile | `http` | `dotnet run` with no `--launch-profile` uses this → **`:5000`** |
| `ASPNETCORE_URLS` in env | **absent** | `applicationUrl` wins |
| `http` / `https` `launchUrl` | `swagger` | path leftover (B06: `UseSwagger()` without `UseSwaggerUI()`). Not a port bug. |
| IIS Express `launchUrl` | `weatherforecast` | **dead path** on a **wrong port**. Double leftover. |
| Auth | anonymous, Windows auth off | CORS `AllowAnyOrigin` on the host lets `:3000` call `:5000` today |

`TraderIntelligence.Api.csproj` and `Directory.Build.props` set **no** `LaunchProfile` / `applicationUrl`. There is no `*.csproj.user` and no VS `launch.json` under the API.

---

## 3. Web client uses `:5000` (measured)

No `apps/web/.env`, `.env.local`, or `.env.example`. `VITE_API_URL` is **unset on disk**. Fallback is live.

| File | Bytes | SHA-256 | Bind |
|---|---:|---|---|
| `D:\Prop\apps\web\src\api\client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | `baseURL: import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | `const BASE = import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` then `${BASE}/hubs/dashboard` |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | error copy: “Start the ASP.NET API on port **5000**.” |
| `D:\Prop\apps\web\vite.config.ts` | 169 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | `server: { port: 3000 }` — **SPA origin**, no `proxy` |

`hooks.ts` SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` uses relative `/api/*` on that axios client. The two `5000` literals there are `refetchInterval: 5000` (ms). **Not ports.**

Consequence: without a runtime `VITE_API_URL`, **every** dashboard XHR and the SignalR handshake go to `http://localhost:5000`. Vite does **not** rewrite `/api` to the API. A listen on any other port is a hard miss.

---

## 4. Match matrix (operators vs web fallback)

| Operator | Measured URL | vs web `:5000` | §73 |
|---|---|---|---|
| launchSettings `http` | `http://localhost:5000` | **MATCH** | **EXISTS_AND_GOOD** (this pair) |
| launchSettings `https` HTTP half | `http://localhost:5000` | **MATCH** (also bound) | **EXISTS_NEEDS_REFACTOR** (see §5.2) |
| launchSettings `https` first URL | `https://localhost:7294` | **MISMATCH** if a client used this | **EXISTS_NEEDS_REFACTOR** |
| launchSettings IIS Express HTTP | `http://localhost:18720` | **MISMATCH** | **EXISTS_NEEDS_REFACTOR** |
| launchSettings IIS Express SSL | `https://localhost:44389` | **MISMATCH** | **EXISTS_NEEDS_REFACTOR** |
| `dotnet run` (README, no profile) | first profile = `http` → `:5000` | **MATCH** | — |
| `dotnet run --no-launch-profile` | framework default `:5000` (not probed) | expected MATCH | unverified |
| `TraderIntelligence.Api.http` | `@api=http://localhost:5000` | **MATCH** | **EXISTS_AND_GOOD** (port only) |
| `docker-compose.yml` `api` | `--urls http://0.0.0.0:5000` + `5000:5000` | **MATCH** (and **collides** with host `dotnet run` — B37) | **EXISTS_NEEDS_REFACTOR** (ops) |
| `README.md` | `API: http://localhost:5000` / dashboard `:3000` | **MATCH** | **EXISTS_AND_GOOD** |
| Vite dev server | `:3000` | N/A — UI origin, not API | **EXISTS_AND_GOOD** |
| `Program.cs` | `app.Run()` — no hardcoded listen | inherits launch / `--urls` | — |
| A06 / A54 / A63 / A65 “API is `:5160`” | stale text | **DOC MISMATCH** | treat as historical |
| A62 proposed Vite proxy | `http://localhost:5160` | **would MISMATCH** if coded as written | do not implement that snippet |

`Program.cs` SHA-256 `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` (4658 bytes) — **newer** than B06’s `13CF8003…` / 4503 bytes. Still no `UseHttpsRedirection()`, so `http://localhost:5000` on the `https` profile is not bounced to `:7294`. A65’s “HTTPS redirect will break `:5000`” claim is **stale**.

---

## 5. Flagged mismatches

### 5.1 OPEN — IIS Express `:18720` / `:44389` vs web `:5000`  **(runtime)**

`commandName: IISExpress` ignores `profiles.*.applicationUrl` and uses `iisSettings.iisExpress`:

- HTTP `http://localhost:18720`
- HTTPS `https://localhost:44389`

The web client does not know those ports. There is no Vite proxy and no `.env` override.

**Break condition:** F5 / debug with profile **IIS Express** (common VS default on older web templates; last-selected profile is not persisted here because no `*.csproj.user` exists). Dashboard on `:3000` → axios → `:5000` → connection refused. API is actually on `:18720`.

**Adjacent leftover:** IIS Express `launchUrl` is still `weatherforecast`. That path is **not mapped** (B06 / B23). Wrong port **and** dead path.

### 5.2 OPEN (soft) — `https` profile advertises `:7294` first

`applicationUrl`: `https://localhost:7294;http://localhost:5000`

| Fact | Implication |
|---|---|
| Kestrel binds **both** | axios default `:5000` still works if this profile is used |
| `launchBrowser` opens the **first** URL | operator lands on `https://localhost:7294/swagger` |
| Web never talks to `:7294` | two “API URLs” in the same profile; docs / muscle memory split |
| No HTTPS redirect in `Program.cs` | `:5000` remains usable |

Flag as **profile-surface mismatch**, not a hard miss for the current axios fallback.

### 5.3 CLOSED — Kestrel `:5160` vs web `:5000`

A06 §2.3, A54 §table (`apps/web`), A63 §1, A65 §inventory, A62 proxy snippet, A77 listen table, A19 `.http` note all still say the API is **`:5160`**.

**On disk that is false.** `http.applicationUrl` is `:5000`. Hash `E092DE59…` already reflected that when B06 ran. The A-wave text was never rewritten.

B06 / B10 / B23 then claimed `TraderIntelligence.Api.http` was still `@… = http://localhost:5160` + `GET /weatherforecast/`. **That is also false now.**

Current `.http` (193 bytes, SHA-256 `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651`; B06/B23 had 157 bytes / `353BB5D9…`):

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

Port in the REST client sample **matches** the web client. B06-01 (stale `:5160` `.http`) is **done**. Do not re-open it.

### 5.4 LATENT — A62 proxy snippet would re-break the pair

`A62_react_scaffold.md` still recommends:

```ts
'/api':  { target: process.env.VITE_API_PROXY ?? 'http://localhost:5160', ... }
```

`vite.config.ts` has **no** proxy today. B10’s later plan correctly retargeted the same snippet to `:5000`. If a coding wave pastes **A62** instead of **B10**, the SPA would proxy to a port nothing listens on.

### 5.5 OPS — Compose and host `dotnet run` both claim `:5000`

Not a web-vs-API mismatch (both sides agree on 5000). B37 already flagged: host `dotnet run` (launchSettings `:5000`) and Compose `5000:5000` **cannot run together**. Second binder fails. Mentioned so B41 is not read as “5000 is universally free.”

---

## 6. What is **not** a port mismatch

| Item | Why it is out |
|---|---|
| Vite `:3000` vs API `:5000` | Intended split. README documents both. CORS currently allows it. |
| `hooks.ts` `refetchInterval: 5000` | Milliseconds. |
| `CTraderFixOptions.MaxQuoteAgeMs = 5000` | Quote-age default (A72). Unrelated. |
| Missing SignalR hub / `/api` vs `/api/v1` | Protocol / contract gaps (B06, B10). Same host:port. |
| `launchUrl: swagger` without Swagger UI | Path 404, same port. |
| Architecture v2 | No lab HTTP port law. `:5000` is a local convention, not §73 production. |

---

## 7. File census (this check)

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1133 | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` | listen table |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | REST sample now `:5000` |
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | no listen override |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | no LaunchProfile |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | axios `:5000` |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | hub `:5000` |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | operator copy |
| `D:\Prop\apps\web\vite.config.ts` | 169 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | UI `:3000`, no proxy |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `5000:5000` |
| `D:\Prop\README.md` | 1746 | `1A4D7C724733E8370AAB489D8A76E97F24150AE16FCD76A4AB6B46424A5FF3E0` | documents `:5000` |

---

## 8. Authorized later fix (do **not** apply in B41)

When a coding wave is authorized, pick **one** listen story and make every profile tell it:

1. **IIS Express** — set `iisSettings.iisExpress.applicationUrl` to `http://localhost:5000` and drop or align `sslPort`, **or** delete the IIS Express profile. Lab path is Kestrel (`dotnet run` / Compose).
2. **`https` profile** — put `http://localhost:5000` first, or drop `:7294` until TLS is a real requirement. Do not add `UseHttpsRedirection()` while the browser talks cleartext `:5000`.
3. **IIS Express `launchUrl`** — `health`, never `weatherforecast`.
4. **Docs** — A06 / A54 / A63 / A65 / A62 / A77 `:5160` sentences are historical. A62 proxy, if added, must target `:5000` (B10), not `:5160`.
5. **Do not** change `client.ts` / `signalr.ts` fallback away from `:5000` unless launchSettings, README, Compose, and `.http` move in the same commit.

---

## 9. Honest limits

- Did not `dotnet run`. Did not hit `:5000`, `:18720`, `:7294`, or `:5160`.
- Did not inspect whether Windows already binds host `:5000` (SSDP / other). Bind failure would look like the same Overview error as a profile miss.
- Did not launch IIS Express. The mismatch is from the JSON, which is how that profile is defined.
- Did not start Compose (Docker is absent on this host per B37).

---

## 10. One-line scorecard

| Question | Answer |
|---|---|
| Does the web client use port 5000? | **Yes** (`VITE_API_URL` fallback + Overview copy). |
| Does launchSettings `http` match? | **Yes.** |
| Is there still a mismatch to flag? | **Yes — IIS Express `:18720`/`:44389`. Soft: `https` first URL `:7294`.** |
| Is the old `:5160` vs `:5000` gap still live in product source? | **No.** Closed on Kestrel + `.http` + README + Compose. Lives only in stale swarm text and the A62 snippet. |
| Product source edited? | **No.** |
