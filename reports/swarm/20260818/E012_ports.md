# E012 — API `:5000` / web `:3000`

| Field | Value |
|---|---|
| Agent | E012 (senior engineer, listen-pair recensus only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:09+05:30 (hashes + TCP) / 2026-08-18T08:18:29Z (HTTP `/health` utc) |
| Artifact | `D:\Prop\reports\swarm\20260818\E012_ports.md` |
| Workspace | `D:\Prop` (Vite app is **`apps/web`**, not under `D:\Prop\src`) |
| Assigned | API 5000 web 3000. Write this report. **Do not modify product source.** |
| Product source modified | **No.** This report (plus `INDEX.md` / `SWARM_LOG.md` catalog) is the only write. |
| Method | Re-hash Vite / axios / SignalR / launchSettings / `Program.cs` / `.http` / compose / README. `Get-NetTCPConnection` on 3000/5000/5173/4173/5160/7294/18720. Live GET/OPTIONS against already-running Kestrel + Vite. Confirm no `apps/web/.env*`. Did **not** `dotnet run`, `npm run`, or edit product source. |
| Relates | B41 (API profile vs client `:5000`), C24 (3000 vs 5000 is not a conflict), D75 (`launchSettings` SHA / IIS leftover), C22 (CORS), D50 (no `MapHub`), C12/D63 (compose has no web) |
| Supersedes | C24 §3.2 **live occupancy only** (C24: Vite down; this pass: Vite **up**). Does **not** supersede B41 on IIS Express / `:7294`, C22 on `AllowAnyOrigin`, or D50 on the missing hub. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Verdict

**API is `:5000`. Web is `:3000`. That pair is the intended two-process local split, not a port bug.**

| Origin | Process (this host, this check) | On-disk bind | Role |
|---|---|---|---|
| `http://127.0.0.1:5000` | `TraderIntelligence.Api.exe` pid **54468** `--urls http://127.0.0.1:5000` | launchSettings `http` + Compose `--urls` + axios/SignalR fallback | JSON API |
| `http://127.0.0.1:3000` | `node` pid **49100** `vite.js --host 127.0.0.1 --port 3000` | `apps/web/vite.config.ts` → `server.port: 3000` | SPA + HMR |

The browser **loads** the dashboard from `:3000` and **calls** the API at `:5000`. README documents both URLs. CORS `AllowAnyOrigin` is the glue. There is **no** Vite `server.proxy`.

**Do not “fix” this by moving Vite onto 5000 or the API onto 3000.** That would collide the two listeners. Both sockets are **occupied right now**.

| Question | Answer |
|---|---|
| API port? | **5000** |
| Web / Vite port? | **3000** |
| Are they supposed to be different? | **Yes.** |
| Is a live API on `:5000` this minute? | **Yes.** `GET /health` = **200**. |
| Is Vite on `:3000` this minute? | **Yes.** `GET /` = **200**, title `MT5 Trader Intelligence`, Vite HMR inject present. |
| Does CORS from `Origin: http://localhost:3000` work? | **Yes (measured).** OPTIONS `/api/overview` → **204** `Access-Control-Allow-Origin: *`. GET with that Origin → **200** + `*`. |
| Does SignalR on `:5000` work? | **No.** `POST /hubs/dashboard/negotiate` → **404**. Port is right; hub is missing (D50). |
| Stale `:5160` still in product source? | **No** (worktree). `GET :5160/health` = connect fail. Lives in `HEAD` launchSettings + old swarm prose + A62 proxy snippet. |
| Product source edited? | **No.** |

Class of the pair: **`EXISTS_AND_GOOD`** (local-dev topology). Adjacent holes (IIS Express `:18720`, `https` first URL `:7294`, no web in Compose, `AllowAnyOrigin`, missing hub) are **not** “5000 vs 3000 is wrong.”

---

## 1. Method

1. Hash + LastWrite of Vite config, `package.json`, axios/SignalR/hooks, Overview copy, API `launchSettings.json`, `Program.cs`, `.http`, `.csproj`, worker launch files, `docker-compose.yml`, `README.md`.
2. Confirm **no** `apps/web/.env*` and **no** root `.env.example` (D62: deleted in worktree).
3. Confirm workers have **no** `applicationUrl`.
4. `Get-NetTCPConnection -State Listen` on 3000 / 5000 / 5173 / 4173 / 5160 / 7294 / 18720.
5. `Win32_Process.CommandLine` for the two owning pids.
6. HTTP GET `/health`, `/api/overview`, `/swagger` on `:5000`; OPTIONS + Origin GET; POST `/hubs/dashboard/negotiate`; GET `:3000/`; GET `:5160/health`.
7. Cross-check B41 / C24 hashes against **this** snapshot.

Did **not** start or kill either process. Did **not** start Compose. Did **not** launch IIS Express.

Architecture v2 has **no** lab HTTP port law and **no** `:3000` / `:5000` literals. The pair is a **local convention**, not §73 production.

---

## 2. Web is `:3000` (measured)

Path: `D:\Prop\apps\web\vite.config.ts`  
169 bytes, SHA-256 `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1`  
LastWrite 2026-08-18 13:06:19 — **unchanged vs C24**.

```ts
export default defineConfig({
  plugins: [react()],
  server: { port: 3000 },
});
```

| Vite key | On disk | Effect |
|---|---|---|
| `server.port` | **3000** | Overrides Vite 5 default **5173** |
| `server.strictPort` | **absent** | If `:3000` is taken, Vite walks 3001… |
| `server.host` | **absent** in file | This live process was started with **`--host 127.0.0.1 --port 3000`** (loopback only) |
| `server.proxy` | **absent** | Browser talks to `:5000` itself |
| `preview` | **absent** | `npm run preview` → Vite default **4173**, not 3000 |

`package.json` (739 B, SHA-256 `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6`): `"dev": "vite"` — no `--port` in the script. The running cmdline added `--host 127.0.0.1 --port 3000` at launch (operator / wrapper), which **matches** the config.

Live `GET http://127.0.0.1:3000/`:

- **200**, 624 bytes
- `<title>MT5 Trader Intelligence</title>`
- Vite `/@react-refresh` inject present

`:5173` and `:4173` are **not** listening.

---

## 3. API is `:5000` (measured)

### 3.1 On-disk bind

`D:\Prop\apps\api\Properties\launchSettings.json`  
**1125** bytes, SHA-256 `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0`  
LastWrite 2026-08-18 13:32:01 — **same as D75 / D06**. B41 / C24 quoted the older 1133 B / `E092DE59…` snapshot (IIS `launchUrl` still `weatherforecast`). That byte change is **not** a port change: `http.applicationUrl` was already `:5000`.

| Profile | `applicationUrl` / listen | vs web `:3000` |
|---|---|---|
| `http` (first profile → default `dotnet run`) | `http://localhost:5000` | **different port, intended** |
| `https` | `https://localhost:7294;http://localhost:5000` | HTTP half still `:5000` |
| IIS Express | `http://localhost:18720` + ssl `44389` | **wrong API port** if F5 uses this profile (B41) |

`Program.cs` (4731 B, SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E`, LastWrite 13:35:15):

- `app.Run()` only. **No** `UseUrls`, **no** `Listen(`, **no** `UseHttpsRedirection`.
- CORS default policy: `AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()` then `app.UseCors()`.
- `UseSwagger()` in Development; **`UseSwaggerUI()` absent**; **`AddSignalR` / `MapHub` absent**.

`TraderIntelligence.Api.http` (193 B, SHA-256 `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651`):

```http
@api=http://localhost:5000
```

Workers: `apps/mt5-worker/Properties/launchSettings.json` (`8E2A7548…`) and `apps/fix-worker/Properties/launchSettings.json` (`25A750D8…`) have **no** `applicationUrl`. They do not claim 3000 or 5000.

`git status --short`: **`M apps/api/Properties/launchSettings.json`**. Worktree is `:5000`; `HEAD` blob still has the template `:5160` (D75). Checking out HEAD would **re-open** the old miss.

### 3.2 Live process (this host, this check)

| Probe | Result |
|---|---|
| TCP listen `:5000` | **Yes** — `127.0.0.1:5000` Listen, pid **54468**, `TraderIntelligence.Api` |
| Command line | `"D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.exe" --urls http://127.0.0.1:5000` |
| TCP listen `:3000` | **Yes** — `127.0.0.1:3000` Listen, pid **49100**, `node` / Vite |
| TCP listen `:5173` / `:4173` / `:5160` / `:7294` / `:18720` | **No** |
| `GET http://127.0.0.1:5000/health` | **200** `{"status":"ok","utc":"2026-08-18T08:18:29.3234276+00:00"}` `Server: Kestrel` |
| `GET http://127.0.0.1:5000/api/overview` | **200** demo JSON (`totalAccounts: 4`, `connectedBrokers: 2`, `shadow: 2`, `live: 0`) |
| `GET http://127.0.0.1:5000/swagger` | **404** (`UseSwagger()` without `UseSwaggerUI()`) |
| `POST http://127.0.0.1:5000/hubs/dashboard/negotiate` | **404** (no hub) |
| `GET http://127.0.0.1:3000/` | **200** Vite HTML |
| `GET http://127.0.0.1:5160/health` | **connect fail** — nothing on the old A-wave port |

The running API was launched with **`--urls http://127.0.0.1:5000`**, not via the `https` / IIS Express profiles. Loopback-only. Fine for a dashboard on `localhost:3000`. Compose uses `0.0.0.0:5000` instead (all interfaces). Host `dotnet run` and Compose `api` **cannot** both bind host `:5000` (D63 / B37).

---

## 4. How `:3000` talks to `:5000`

No `apps/web/.env`, `.env.local`, `.env.development`. Root `D:\Prop\.env.example` is **MISSING** (D62). `VITE_API_URL` is unset. The hardcoded fallback is live.

| File | Bytes | SHA-256 | Bind |
|---|---:|---|---|
| `apps/web/src/api/client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | `baseURL: import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` |
| `apps/web/src/api/signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | same BASE, then `${BASE}/hubs/dashboard` |
| `apps/web/src/pages/OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | error copy: “Start the ASP.NET API on port **5000**.” |
| `apps/web/src/api/hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | relative `/api/*` on that axios client |

`hooks.ts` `refetchInterval: 5000` is **milliseconds**, not a port. Do not file it as a 3000-vs-5000 finding.

Browser data path when both processes are up (both **are** up):

```text
operator
  → http://localhost:3000          Vite (HTML/JS/HMR)
       axios GET /api/overview
  → http://localhost:5000/api/...  Kestrel (CORS *)
       SignalR /hubs/dashboard
  → http://localhost:5000/hubs/... 404 today (hub not mapped)
```

Same-origin `/api` on `:3000` would **404** (no proxy). The client never uses that path unless `VITE_API_URL` is set to the Vite origin.

`client.ts` always sends `Content-Type: application/json`, so even GET is a CORS **preflight**.

### 4.1 Live CORS (Origin `:3000` → API `:5000`)

| Request | Status | CORS |
|---|---|---|
| `OPTIONS /api/overview` + `Origin: http://localhost:3000` + `Access-Control-Request-Method: GET` + `Access-Control-Request-Headers: content-type` | **204** | `Access-Control-Allow-Origin: *` |
| `GET /api/overview` + `Origin: http://localhost:3000` | **200** | `Access-Control-Allow-Origin: *` |

The split-origin pair **works** for XHR **right now**. `AllowAnyOrigin` cannot later be combined with `AllowCredentials()`. Cookie auth will force an origin list (`http://localhost:3000` + prod SPA). Do not treat `*` as the production CORS story (C22: **UNSAFE** as a shipped default).

---

## 5. Operators agree on the split

| Operator | Web / dashboard | API | vs intended pair |
|---|---|---|---|
| `README.md` (1746 B, SHA-256 `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764`) | `Dashboard: http://localhost:3000` | `API: http://localhost:5000` | **MATCH** |
| README run recipe | `cd apps/web; npm run dev` | `dotnet run --project apps/api/...` | two processes |
| `docker-compose.yml` (687 B, SHA-256 `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1`) | **no `web` service** | `--urls http://0.0.0.0:5000` + `5000:5000` | API half only |
| `docs/deployment.md` sample | — | `ports: ["5000:5000"]` | API half (sketch) |
| `TraderIntelligence.Api.http` | n/a | `@api=http://localhost:5000` | API half |
| launchSettings `http` | n/a | `http://localhost:5000` | API half |
| `apps/web/.env*` | **absent** | fallback `:5000` | API half |

Compose does **not** publish `:3000`. Lab dashboard remains **host** `npm run dev` even if Compose `api` is used. That is an API-vs-API collision risk with host Kestrel, not Vite-vs-API.

---

## 6. What API 5000 / web 3000 is **not**

| Claim | Reality |
|---|---|
| “Ports disagree; one of them is wrong.” | **False.** Different processes, different roles. |
| “Move Vite to 5000 so they match.” | Would fight Kestrel for the socket. **Both are bound now.** |
| “Move API to 3000 so they match.” | Would fight Vite; README, `.http`, axios, Overview copy all hard-code 5000. |
| “`:5160` is still the API.” | **Stale A-wave text.** Worktree product source is `:5000`. `HEAD` launchSettings still `:5160`. |
| “`hooks.ts` uses port 5000 as a poll.” | `refetchInterval: 5000` is **ms**. |
| “CORS will block `:3000` → `:5000`.” | **Measured false** for the current `*` policy. |
| “SignalR failure means the port is wrong.” | Hub path is unmapped (**404** on `:5000`). Same host:port. |
| “`/swagger` 404 means API is not on 5000.” | API is on 5000 (`/health` 200). Swagger UI is not registered. |
| Architecture requires these numbers | **No.** Local convention only. |

---

## 7. Adjacent issues (do not reclassify the pair)

These are real. They are **not** “5000 conflicts with 3000.”

| ID | Class | Note |
|---|---|---|
| IIS Express `:18720` / `:44389` | **EXISTS_NEEDS_REFACTOR** | B41. F5 on that profile leaves Vite calling a dead `:5000`. |
| `https` first URL `:7294` | **EXISTS_NEEDS_REFACTOR** | Kestrel would bind both; axios still hits `:5000`. Not listening now. |
| Kestrel `:5160` vs web `:5000` | **GONE** in worktree product source | A06 / A54 / A63 / A65 / A77 text is historical. A62 proxy snippet still names `:5160` — do **not** implement that snippet. |
| Vite proxy without relative axios | latent | `server.proxy` only sees same-origin `/api`. Absolute `:5000` base never hits a proxy. |
| Compose `:5000` vs host `:5000` | ops | Second binder fails. Vite is not in that fight. |
| `npm run preview` → `:4173` | docs | Preview is not the README dashboard URL unless `--port 3000`. |
| CORS `*` | **UNSAFE** as shipped default | Makes the split work. Fine for anonymous InMemory seed. |
| `HEAD` launchSettings `:5160` | **DEPRECATED** | Uncommitted worktree is the live lab bind. |

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
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | Kestrel `:5000` |
| `D:\Prop\apps\api\Program.cs` | 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | CORS `*`, no listen override |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | sample `:5000` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | no LaunchProfile |
| `D:\Prop\apps\fix-worker\Properties\launchSettings.json` | 296 | `25A750D823B04F229FBC49A690F997E969276FFB1A5E5F4EF198DF7DE7CBEF9B` | no URL |
| `D:\Prop\apps\mt5-worker\Properties\launchSettings.json` | 296 | `8E2A7548E3EBFF12FDB3E078E06ADA944E3ABB83BA8F9128746542CAA8AA3E36` | no URL |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | API `5000:5000` |
| `D:\Prop\README.md` | 1746 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | documents both |
| `D:\Prop\.env.example` | — | **MISSING** | no `VITE_*` file to read |

Hash drift vs C24 (same day, later snapshot):

| File | C24 | E012 |
|---|---|---|
| `launchSettings.json` | 1133 / `E092DE59…` | **1125 / `BC022898…`** (IIS `launchUrl` → `swagger`; port still 5000) |
| `Program.cs` | 4658 / `E914FA98…` | **4731 / `61B1E0D1…`** (still no `UseUrls` / no HTTPS redirect) |
| `.env.example` | 3408 / `56C81786…` | **gone** |
| Vite / client / SignalR / Overview / compose / README | same SHAs | same SHAs |

---

## 9. Authorized later work (do **not** apply in E012)

When a coding wave is authorized:

1. **Keep the split** for local `npm run dev` + `dotnet run`. Do not collapse onto one port.
2. If a Vite proxy is added, target **`http://localhost:5000`**, never A62’s `:5160`, and change axios/SignalR to a **relative** base. Fix the `||` empty-string trap.
3. Add `server.strictPort: true` if README `:3000` must be a hard fail rather than a silent 3001.
4. `vite preview --port 3000` (or document 4173).
5. Tighten CORS to `http://localhost:3000` (and the prod SPA origin) **before** cookies / privileged POST.
6. IIS Express / `:7294` cleanup stays with B41.
7. **Do not** change `client.ts` / `signalr.ts` fallback away from `:5000` unless launchSettings, README, Compose, and `.http` move in the same commit.
8. Commit the dirty `launchSettings.json` so `HEAD` is no longer `:5160`.

---

## 10. Honest limits

- Did not start or stop pid 54468 / 49100.
- Did not use a browser. CORS was proven with `Invoke-WebRequest` `Origin` / `OPTIONS`, not Chromium.
- Did not hit `:18720` / IIS Express.
- Did not start Compose.
- Did not change product source.
- `127.0.0.1` vs `localhost` Origin is a real browser distinction. Preflight used `Origin: http://localhost:3000` (README spelling). `*` accepts either. Live binds are loopback `127.0.0.1`.

---

## 11. One-line scorecard

| Question | Answer |
|---|---|
| API port? | **5000** (launchSettings `http`, Compose, axios, SignalR, README, `.http`). **Listening now** on `127.0.0.1`. |
| Web port? | **3000** (`vite.config.ts`). **Listening now** on `127.0.0.1`. |
| Is 5000 vs 3000 a bug? | **No.** Intended two-origin lab. |
| Can the browser call across that split? | **Yes** — CORS `*` measured 204/200. |
| Is the dashboard usable this minute? | **Yes for XHR chrome** (both up). SignalR still 404. Demo data, not live MT5/FIX. |
| Stale `:5160` still in product worktree? | **No.** Yes in `HEAD` launchSettings. |
| Product source edited? | **No.** |
