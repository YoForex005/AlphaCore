# C24 — Vite `:3000` vs API `:5000`

| Field | Value |
|---|---|
| Agent | C24 (senior engineer, local-dev listen pair only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:26:44+05:30 (TCP) / 2026-08-18T07:57:08Z (HTTP) |
| Artifact | `D:\Prop\reports\swarm\20260818\C24_dev_ports.md` |
| Workspace | `D:\Prop` (Vite app is **`apps/web`**, not under `D:\Prop\src`) |
| Assigned | Vite 3000 vs API 5000. Write this report. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Method | Read `vite.config.ts`, `package.json`, axios/SignalR clients, `launchSettings.json`, `Program.cs`, `docker-compose.yml`, `README.md`. SHA-256 + byte census. `Get-NetTCPConnection` on 3000/5000/5173/4173/5160/7294. Live HTTP against the already-running Kestrel process. No `dotnet run`, no `npm run`, no product edit. |
| Relates | B41 (API profile vs client `:5000`), B30, B10, B37, C04, C12, A54, A62, A65, A75 |
| Classification | Architecture §73.B |

C24 is **not** a re-run of B41. B41 asked whether Kestrel’s listen port matches the web client’s fallback `:5000`. C24 asks whether **Vite `:3000` and API `:5000` are a conflict**. They are not.

---

## 0. Verdict

**`:3000` vs `:5000` is the intended two-process local split, not a port mismatch.**

| Origin | Process | Bind (on disk) | Role |
|---|---|---|---|
| `http://localhost:3000` | Vite 5 (`npm run dev`) | `apps/web/vite.config.ts` → `server.port: 3000` | SPA + HMR |
| `http://localhost:5000` | ASP.NET 8 Kestrel (`dotnet run` / Compose) | `launchSettings` `http` + Compose `--urls` + axios/SignalR fallback | JSON API + (planned) hubs |

The browser **loads** the dashboard from `:3000` and **calls** the API at `:5000`. That is cross-origin by design. README documents both URLs. CORS `AllowAnyOrigin` is the glue. There is **no** Vite `server.proxy`.

**Do not “fix” this by moving Vite onto 5000 or the API onto 3000.** That would collide the two listeners.

| Question | Answer |
|---|---|
| Are 3000 and 5000 supposed to be different? | **Yes.** |
| Does product source still have the stale `:5160` API bind? | **No.** Closed. Lives only in old swarm text + the A62 proxy snippet. |
| Does a live API exist on this host right now? | **Yes** — `TraderIntelligence.Api.exe --urls http://127.0.0.1:5000` (pid 54744). `GET /health` = **200**. |
| Is Vite listening on 3000 right now? | **No.** `GET http://127.0.0.1:3000/` timed out. Dashboard origin is down; API origin is up. |
| Does CORS from `Origin: http://localhost:3000` work? | **Yes (measured).** OPTIONS `/api/overview` → **204** `Access-Control-Allow-Origin: *`. GET with that Origin → **200** + `*`. |
| Does SignalR on `:5000` work? | **No.** `POST /hubs/dashboard/negotiate` → **404**. Port is right; hub is missing (B30 / C04). |
| Product source edited? | **No.** |

Class of the pair: **`EXISTS_AND_GOOD`** (local-dev topology). Adjacent holes (IIS Express `:18720`, `https` first URL `:7294`, no web in Compose, `AllowAnyOrigin`, missing hub) are **not** “3000 vs 5000 is wrong.”

---

## 1. Method

1. Read the Vite bind: `D:\Prop\apps\web\vite.config.ts`, `package.json` scripts.
2. Read the browser consumers: `src/api/client.ts`, `signalr.ts`, `hooks.ts`, `OverviewPage.tsx`, `DashboardLayout.tsx`.
3. Read the API bind: `apps/api/Properties/launchSettings.json`, `Program.cs` (CORS + `app.Run()`, no `UseUrls`), `.http` sample, `.csproj`.
4. Read operators: `D:\Prop\README.md`, `D:\Prop\docker-compose.yml`, `D:\Prop\.env.example`. Confirm **no** `apps/web/.env*`.
5. Confirm workers do **not** claim 3000/5000 (`mt5-worker` / `fix-worker` `launchSettings` have no `applicationUrl`).
6. Hash the measured files. Cross-check B41 / B30 / A62 / A65 against **today’s** hashes.
7. `Get-NetTCPConnection` listen table. HTTP GET/OPTIONS against the already-running API. Did **not** start Vite. Did **not** kill pid 54744.

Architecture v2 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`) has **no** lab HTTP port law and **no** `:3000` / `:5000` literals. The pair is a **local convention**, not §73 production.

---

## 2. Vite is `:3000` (measured)

Path: `D:\Prop\apps\web\vite.config.ts`  
169 bytes, 8 physical lines, SHA-256 `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1`  
LastWrite 2026-08-18T13:06:19

```ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: { port: 3000 },
});
```

| Vite key | On disk | Effect |
|---|---|---|
| `server.port` | **3000** | Overrides Vite 5 `DEFAULT_DEV_PORT` **5173** (confirmed in `node_modules/vite/dist/node/constants.js`) |
| `server.strictPort` | **absent** | Default false. If `:3000` is taken, Vite walks 3001, 3002… README would then be stale; CORS `*` would still accept the new Origin |
| `server.host` | **absent** | Default localhost |
| `server.proxy` | **absent** | `/api` and `/hubs` are **not** rewritten. Browser talks to `:5000` itself |
| `preview` | **absent** | `npm run preview` uses Vite `DEFAULT_PREVIEW_PORT` **4173**, **not** 3000 |

`package.json` (739 bytes, SHA-256 `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6`):

```json
"dev": "vite",
"build": "tsc && vite build",
"preview": "vite preview"
```

`npm run dev` → Vite CLI → this config → **`:3000`**. No `--port` override in the script.

`index.html` is a static shell (no port). `main.tsx` does not start SignalR. `DashboardLayout.tsx` (1854 bytes, SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`) calls `startConnection()` on mount — that handshake targets `:5000`, not `:3000`.

B10’s “no `node_modules` / no lockfile” census is **stale**. Both exist now. That does not change the port.

---

## 3. API is `:5000` (measured)

### 3.1 On-disk bind

`D:\Prop\apps\api\Properties\launchSettings.json`  
1133 bytes, SHA-256 `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` (unchanged since B06 / B41)

| Profile | `applicationUrl` | vs Vite `:3000` |
|---|---|---|
| `http` (first profile → default `dotnet run`) | `http://localhost:5000` | **different port, intended** |
| `https` | `https://localhost:7294;http://localhost:5000` | HTTP half still `:5000` |
| IIS Express | `http://localhost:18720` + ssl `44389` | **wrong API port** if F5 uses this profile (B41) |

`Program.cs` (4658 bytes, SHA-256 `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9`):

- `app.Run()` only. **No** `UseUrls`, **no** `Listen(`, **no** `UseHttpsRedirection`.
- Listen inherits launch profile / `--urls` / `ASPNETCORE_URLS`.
- CORS default policy: `AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()` then `app.UseCors()`.

`TraderIntelligence.Api.csproj` / `Directory.Build.props` set **no** `LaunchProfile` / `applicationUrl`.

`TraderIntelligence.Api.http` (193 bytes, SHA-256 `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651`):

```http
@api=http://localhost:5000
```

Workers: `apps/mt5-worker/Properties/launchSettings.json` and `apps/fix-worker/Properties/launchSettings.json` have **no** `applicationUrl`. They are not competing for 3000 or 5000.

### 3.2 Live process (this host, this check)

| Probe | Result |
|---|---|
| TCP listen `:5000` | **Yes** — `127.0.0.1:5000` Listen, pid **54744**, `TraderIntelligence.Api` |
| Command line | `"D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.exe" --urls http://127.0.0.1:5000` |
| TCP listen `:3000` | **No** |
| TCP listen `:5173` / `:4173` / `:5160` / `:7294` | **No** |
| `GET http://127.0.0.1:5000/health` | **200** `{"status":"ok","utc":"2026-08-18T07:57:08.533287+00:00"}` `Server: Kestrel` |
| `GET http://127.0.0.1:5000/api/overview` | **200** demo JSON (`totalAccounts: 4`, `realCopyEnabled: false`) |
| `GET http://127.0.0.1:5000/swagger` | **404** (C04: `UseSwagger()` without `UseSwaggerUI()`) |
| `POST http://127.0.0.1:5000/hubs/dashboard/negotiate` | **404** (no `AddSignalR` / `MapHub`) |
| `GET http://127.0.0.1:3000/` | **timeout** — Vite not started |

The running API was launched with **`--urls http://127.0.0.1:5000`**, not via the `https` / IIS Express profiles. Loopback-only. Fine for a dashboard on `localhost:3000`. Compose uses `0.0.0.0:5000` instead (all interfaces).

---

## 4. How `:3000` talks to `:5000`

No `apps/web/.env`, `.env.local`, `.env.development`, or `apps/web/.env.example`. Root `D:\Prop\.env.example` (3408 bytes, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA`) has **no** `VITE_API_URL` and **no** `:3000` / `:5000`. `VITE_API_URL` is unset. The hardcoded fallback is live.

| File | Bytes | SHA-256 | Bind |
|---|---:|---|---|
| `apps/web/src/api/client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | `baseURL: import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` |
| `apps/web/src/api/signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | same BASE, then `${BASE}/hubs/dashboard` |
| `apps/web/src/pages/OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | error copy: “Start the ASP.NET API on port **5000**.” |
| `apps/web/src/api/hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | relative `/api/*` on that axios client |

`hooks.ts` `refetchInterval: 5000` is **milliseconds**, not a port. Do not file it as a 3000-vs-5000 finding.

Browser data path when both processes are up:

```text
operator
  → http://localhost:3000          Vite (HTML/JS/HMR websocket)
       axios GET /api/overview
  → http://localhost:5000/api/...  Kestrel (CORS *)
       SignalR /hubs/dashboard
  → http://localhost:5000/hubs/... 404 today (hub not mapped)
```

Same-origin `/api` on `:3000` would **404** (no proxy). The client never uses that path unless `VITE_API_URL` is set to the Vite origin.

`client.ts` always sends `Content-Type: application/json`, so even GET is a CORS **preflight**. That is why `AllowAnyOrigin` is not optional for the current stub.

### 4.1 Live CORS (Origin `:3000` → API `:5000`)

| Request | Status | CORS headers |
|---|---|---|
| `OPTIONS /api/overview` + `Origin: http://localhost:3000` + `Access-Control-Request-Method: GET` + `Access-Control-Request-Headers: content-type` | **204** | `Access-Control-Allow-Origin: *` `Access-Control-Allow-Methods: GET` `Access-Control-Allow-Headers: content-type` |
| `GET /api/overview` + `Origin: http://localhost:3000` | **200** | `Access-Control-Allow-Origin: *` |

The split-origin pair **works** for XHR **right now**, with Vite down or up. Starting Vite is not required to prove the API half.

`AllowAnyOrigin` cannot later be combined with `AllowCredentials()`. Cookie/JWT-cookie auth (A62) will **force** a specific origin list (`http://localhost:3000`, plus the prod SPA origin). Do not treat `*` as the production CORS story.

---

## 5. Operators agree on the split

| Operator | Vite / dashboard | API | vs intended pair |
|---|---|---|---|
| `README.md` (1746 B, SHA-256 `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764`) | `Dashboard: http://localhost:3000` | `API: http://localhost:5000` | **MATCH** |
| README run recipe | `cd apps/web; npm run dev` | `dotnet run --project apps/api/...` | two processes |
| `docker-compose.yml` (687 B, SHA-256 `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1`) | **no `web` service** | `--urls http://0.0.0.0:5000` + `5000:5000` | API half only |
| `TraderIntelligence.Api.http` | n/a | `@api=http://localhost:5000` | API half |
| launchSettings `http` | n/a | `http://localhost:5000` | API half |
| `apps/web/.env*` | **absent** | fallback `:5000` | API half |

README recipe (quoted):

```powershell
dotnet run --project D:\Prop\apps\api\TraderIntelligence.Api.csproj
cd D:\Prop\apps\web
npm install
npm run dev
```

Two terminals. Two ports. That is the documented lab path.

Compose does **not** publish `:3000`. A65’s proposed `web` service is still **MISSING** (B37 / C12). Lab dashboard remains **host** `npm run dev` even if Compose `api` is used. Host `dotnet run` and Compose `api` **cannot** both bind host `:5000` (B37). That is an API-vs-API collision, not Vite-vs-API.

---

## 6. What `:3000` vs `:5000` is **not**

| Claim | Reality |
|---|---|
| “Ports disagree; one of them is wrong.” | **False.** Different processes, different roles. |
| “Move Vite to 5000 so they match.” | Would fight Kestrel for the socket. |
| “Move API to 3000 so they match.” | Would fight Vite; README, `.http`, axios, Overview copy all hard-code 5000. |
| “`:5160` is still the API.” | **Stale A-wave text.** Product source is `:5000`. |
| “`hooks.ts` uses port 5000 as a poll.” | `refetchInterval: 5000` is **ms**. |
| “CORS will block `:3000` → `:5000`.” | **Measured false** for the current `*` policy. |
| “SignalR failure means the port is wrong.” | Hub path is unmapped (**404** on `:5000`). Same host:port. |
| “`/swagger` 404 means API is not on 5000.” | API is on 5000 (`/health` 200). Swagger UI is not registered (C04). |
| “Empty `VITE_API_URL=` means same-origin / proxy.” | **False with current `\|\|`.** Empty string is falsy → still `http://localhost:5000`. A62 §2 / B10 §9.1 are wrong for this client. Correct **today** (no proxy). Wrong **if** a later wave adds a proxy and expects empty = same origin. |
| Architecture requires these numbers | **No.** Local convention only. Prod (A54) is same-origin nginx + `VITE_API_URL` baked at build. |

---

## 7. Adjacent issues (do not reclassify the pair)

These are real. They are **not** “3000 conflicts with 5000.”

### 7.1 OPEN — IIS Express `:18720` / `:44389` (B41)

If Visual Studio F5 uses **IIS Express**, the API is not on `:5000`. Vite `:3000` still calls `:5000`. Overview shows “API unavailable. Start the ASP.NET API on port 5000.” Lab path is Kestrel / Compose, not IIS Express.

### 7.2 OPEN (soft) — `https` profile first URL `:7294`

Kestrel binds both `:7294` and `:5000`. Axios still hits `:5000`. `launchBrowser` opens Swagger on `:7294`, which is **404** without `UseSwaggerUI()`. Split muscle memory, not a hard miss for the current client.

### 7.3 CLOSED — Kestrel `:5160` vs web `:5000`

A06 / A54 / A63 / A65 / A77 still say the API is `:5160`. **On disk that is false.** Do **not** implement A62’s proxy target:

```ts
// plan only in A62 — would re-open a closed miss
'/api':  { target: process.env.VITE_API_PROXY ?? 'http://localhost:5160', ... }
```

B10 retargeted the same snippet to `:5000`. If a coding wave pastes **A62** instead of **B10**, Vite would proxy to a port nothing listens on (`:5160` LISTEN=no on this host).

### 7.4 LATENT — adding a proxy without changing `client.ts` is a no-op

Vite `server.proxy` only rewrites requests whose URL is **same-origin** (`http://localhost:3000/api/...`). Axios `baseURL` is absolute `:5000`, so the browser **never** hits the proxy. A later proxy must ship with a **relative** axios/SignalR base (or `VITE_API_URL` set to `http://localhost:3000`). Use `??` / explicit empty handling, not `||`, if empty is supposed to mean same-origin.

### 7.5 OPS — Compose `:5000` vs host `:5000`

Both claim the API port. Second binder fails. Vite `:3000` is not in that fight.

### 7.6 `npm run preview` is `:4173`

Production-like static preview is **not** the README dashboard URL. Operators who only start `vite preview` + API will not find the UI on `:3000` unless they pass `--port 3000`.

### 7.7 CORS `*` is lab-wide

It makes the 3000→5000 split work. It also allows any webpage to call the demo API. Fine for anonymous InMemory seed. **UNSAFE** as a shipped default once cookies or operator actions exist.

---

## 8. File census (this check)

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `D:\Prop\apps\web\vite.config.ts` | 169 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | UI `:3000`, no proxy |
| `D:\Prop\apps\web\package.json` | 739 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` | `dev` = `vite` |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | axios → `:5000` |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | hub → `:5000` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | `/api/*` + 5000 **ms** |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | operator copy names 5000 |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | starts SignalR |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1133 | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` | Kestrel `:5000` |
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | CORS `*`, no listen override |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | sample `:5000` |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | API `5000:5000` |
| `D:\Prop\README.md` | 1746 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | documents both |
| `D:\Prop\.env.example` | 3408 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | no `VITE_*` |

README hash **moved** vs B41 (`1A4D7C72…` → `C023B422…`, same 1746 bytes). Port lines are unchanged (`API: http://localhost:5000` / `Dashboard: http://localhost:3000`).

---

## 9. Authorized later work (do **not** apply in C24)

When a coding wave is authorized:

1. **Keep the split** for local `npm run dev` + `dotnet run`. Do not collapse onto one port.
2. If a Vite proxy is added, target **`http://localhost:5000`** (B10), never A62’s `:5160`, and change axios/SignalR to a **relative** base. Fix the `||` empty-string trap.
3. Add `apps/web/.env.example` with `VITE_API_URL=` **only if** the client treats empty as same-origin. Today empty still means `:5000`.
4. Add `server.strictPort: true` if README `:3000` must be a hard fail rather than a silent 3001.
5. `vite preview --port 3000` (or document 4173) so preview matches the dashboard URL.
6. Tighten CORS to `http://localhost:3000` (and the prod SPA origin) **before** cookies / privileged POST. Do not add `AllowCredentials` while `*` is set.
7. IIS Express / `:7294` first-URL cleanup stays with B41. Not a Vite-port change.
8. **Do not** change `client.ts` / `signalr.ts` fallback away from `:5000` unless launchSettings, README, Compose, and `.http` move in the same commit.

---

## 10. Honest limits

- Did not start `npm run dev`. Occupancy of `:3000` was measured as **empty**; Vite behavior (HMR, exact `localhost` vs `127.0.0.1` Origin spelling) was not captured from a live Vite process.
- Did not use a browser. CORS was proven with `Invoke-WebRequest` `Origin` / `OPTIONS`, not Chromium.
- Did not hit `:18720` / IIS Express.
- Did not start Compose (Docker absent per B37).
- Did not change product source. Did not stop pid 54744.
- `127.0.0.1` vs `localhost` Origin is a real browser distinction. Preflight was sent as `Origin: http://localhost:3000` (README spelling). `*` accepts either.

---

## 11. One-line scorecard

| Question | Answer |
|---|---|
| Vite port? | **3000** (`vite.config.ts`). Default 5173 is overridden. Not listening now. |
| API port? | **5000** (launchSettings `http`, Compose, axios, SignalR, README, `.http`). **Listening now** on `127.0.0.1`. |
| Is 3000 vs 5000 a bug? | **No.** Intended two-origin lab. |
| Can the browser call across that split? | **Yes** — CORS `*` measured 204/200. |
| Is the dashboard usable this minute? | **API yes, UI no** (Vite down). Start `npm run dev` for `:3000`. |
| Stale `:5160` still in product source? | **No.** |
| Product source edited? | **No.** |
