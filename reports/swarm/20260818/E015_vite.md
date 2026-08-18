# E015 — Vite must listen on `:3000`

| Field | Value |
|---|---|
| Agent | E015 (senior engineer, Vite listen-port pin only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:55+05:30 (hash/TCP) / 2026-08-18T08:19:13Z (HTTP) |
| Artifact | `D:\Prop\reports\swarm\20260818\E015_vite.md` |
| Workspace | `D:\Prop` (Vite app is **`apps/web`**, not under `D:\Prop\src`) |
| Assigned | Note Vite should be on **3000**. Write this report. **Do not modify product source.** |
| Product source modified | **No.** This report is the only write. |
| INDEX / SWARM_LOG rewritten by this agent | **No.** Orchestrator catalog duty (same rule as D99). |
| Method | Read `vite.config.ts`, `package.json`, `index.html`, axios/SignalR clients, `launchSettings.json`, `Program.cs`, both `appsettings*.json`, `docker-compose.yml`, `README.md`. SHA-256 + byte / newline census. Confirm Vite 5 `DEFAULT_DEV_PORT` in `node_modules/vite/dist/node/constants.js`. `Get-NetTCPConnection` on 3000/5000/5173/4173/5160/7294/18720. `Win32_Process` command lines. Live HTTP against the already-running Vite + Kestrel. CORS OPTIONS/GET with `Origin: http://localhost:3000` and `:5173`. No `npm run`, no `dotnet run`, no product edit, no process kill. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Relates | C24 (ports pair; **stale** on “Vite not listening”), C22 (CORS `*`), B41 (API `:5000`), B10 / A62 (scaffold + proxy plan), C40 (`index.html`), C49 (npm present), D53 / D64 / D80 (`Cors:AllowedOrigins` still `:5173`, unread), D50 (no hub) |
| Classification | Architecture §73.B |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

E015 is **not** a re-run of C24’s “is 3000 vs 5000 a conflict?” question. C24 answered **no**. E015 pins the **operator law**: the React/Vite dashboard origin is **`http://localhost:3000`**, not Vite’s factory default **5173**, not preview **4173**, not Kestrel **5000**.

---

## 0. Verdict

**Vite should be on `:3000`. On disk it is. Live on this host it is.**

| Surface | Measured |
|---|---|
| Law | Dashboard origin = `http://localhost:3000` |
| On-disk pin | `apps/web/vite.config.ts` → `server: { port: 3000 }` |
| Script | `package.json` `"dev": "vite"` — no `--port` override, so the config wins |
| README | `Dashboard: http://localhost:3000` |
| Vite 5 factory default | `DEFAULT_DEV_PORT = 5173` — **overridden** |
| Live listener | `127.0.0.1:3000` Listen, pid **49100**, `node` |
| Live command line | `"node" "D:\Prop\apps\web\node_modules\.bin\\..\vite\bin\vite.js" --host 127.0.0.1 --port 3000` |
| Live HTTP | `GET http://127.0.0.1:3000/` → **200** `text/html` (Vite-injected `/@vite/client` + React Refresh), title `MT5 Trader Intelligence` |
| `:5173` | **No listen.** `GET http://127.0.0.1:5173/` connect fail |
| `:4173` (`vite preview`) | **No listen.** Preview is not the README dashboard URL |
| API | Separate process, `:5000` (pid **54468**). **Not** Vite |

Class of the port pin: **`EXISTS_AND_GOOD`**.

Do **not** “fix” Vite onto 5173 because `appsettings` `Cors:AllowedOrigins` still lists that origin. That JSON is **dead config** (unread; live CORS is `AllowAnyOrigin()`). Do **not** move Vite onto 5000 (would collide with Kestrel). Do **not** treat C24’s “Vite not listening” sentence as current — that occupancy check is **stale**.

| Question | Answer |
|---|---|
| What port should Vite use? | **3000** |
| Is that written in product source? | **Yes** — `vite.config.ts` line 6 |
| Is it documented? | **Yes** — README line 43 |
| Is it listening now? | **Yes** — `127.0.0.1:3000` |
| Is Vite’s default 5173 in use? | **No** (overridden; not listening) |
| Is 3000 vs 5000 a bug? | **No.** Two-process lab. Browser loads `:3000`, calls `:5000` |
| Product source edited this pass? | **No** |

---

## 1. Law (binding for later waves)

1. Local `npm run dev` **must** serve the dashboard at **`http://localhost:3000`**.
2. `apps/web/vite.config.ts` **must** keep `server.port: 3000` until README, CORS, Compose, and operator runbooks move in the **same** change.
3. Vite 5’s `DEFAULT_DEV_PORT` **5173** is **not** this lab’s dashboard URL. A coding wave that deletes `server.port` silently reverts to 5173 and makes README + operator muscle memory **false**.
4. Kestrel / Compose API stays **`:5000`**. Collapse onto one port is a listener collision, not a cleanup.
5. `vite preview` without `--port 3000` binds **4173**. That is **not** the dashboard URL unless documented.
6. `appsettings*.json` `Cors:AllowedOrigins: ["http://localhost:5173"]` is a **leftover of the factory default**. If CORS is ever bound from config, the list must start with **`http://localhost:3000`**, not 5173.
7. This report does **not** authorize a product edit.

Architecture v2 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`) has **no** lab HTTP port law and **no** `:3000` / `:5173` literals. The pin is a **local convention** recorded in `vite.config.ts` + README, not §73 production.

---

## 2. Method (what was actually run)

1. Full read of `D:\Prop\apps\web\vite.config.ts`, `package.json`, `index.html`.
2. Read browser consumers: `src/api/client.ts`, `src/api/signalr.ts`, `src/api/hooks.ts` (first 30 lines), `src/main.tsx`.
3. Read API bind + CORS: `apps/api/Program.cs` (CORS block), `Properties/launchSettings.json`, `appsettings.json`, `appsettings.Development.json`, `TraderIntelligence.Api.http`.
4. Read operators: `D:\Prop\README.md`, `D:\Prop\docker-compose.yml`. Confirm **no** `apps/web/.env*`.
5. Confirm Vite 5 constants: `DEFAULT_DEV_PORT = 5173`, `DEFAULT_PREVIEW_PORT = 4173` in `apps/web/node_modules/vite/dist/node/constants.js`.
6. `Get-FileHash SHA256` + byte length + newline count + LastWrite on the census table in §8.
7. `Get-NetTCPConnection -State Listen` on 3000 / 5000 / 5173 / 4173 / 5160 / 7294 / 18720.
8. `Get-CimInstance Win32_Process` command lines for the two listeners.
9. `Invoke-WebRequest` `GET http://127.0.0.1:3000/` (body + headers), `:5173/`, `:4173/`, `:5000/health`.
10. `HttpWebRequest` OPTIONS `/api/overview` with `Origin: http://localhost:3000` and `Origin: http://localhost:5173`; GET `/health` with Origin 3000.

Did **not** start or stop Vite or Kestrel. Did **not** run `npm` / `dotnet`. Did **not** use a browser. Did **not** start Compose. Did **not** edit product source.

---

## 3. On-disk pin (`vite.config.ts`)

Path: `D:\Prop\apps\web\vite.config.ts`  
**169** bytes, **7** newlines (8 physical lines), SHA-256 `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1`  
LastWrite 2026-08-18T13:06:19 (unchanged vs C24 / D38 / D39)

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
| `server.port` | **3000** | Overrides Vite 5 `DEFAULT_DEV_PORT` **5173** |
| `server.strictPort` | **absent** | Default false. If `:3000` is taken, Vite walks 3001, 3002… README would then be stale |
| `server.host` | **absent** in config | Default localhost. **This live process** was started with `--host 127.0.0.1` (loopback only) |
| `server.proxy` | **absent** | `/api` and `/hubs` are **not** rewritten. Browser talks to `:5000` itself |
| `preview.port` | **absent** | `npm run preview` uses `DEFAULT_PREVIEW_PORT` **4173**, **not** 3000 |
| `base` | **absent** | Default `/` |

`package.json` (739 bytes, 30 newlines, SHA-256 `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6`):

```json
"dev": "vite",
"build": "tsc && vite build",
"preview": "vite preview"
```

`npm run dev` → Vite CLI → this config → **`:3000`**. No `--port` in the script. The **running** process added `--host 127.0.0.1 --port 3000` on the command line (explicit, matches config).

`index.html` (369 bytes, SHA-256 `80656C860AC6F8C1FAB242789DEEF0803EC278028D8B0F24115A14536FDB8FD`) is a static shell: `#root` + `/src/main.tsx`. No port literal. Vite serves it as the default app entry.

`tsconfig.node.json` includes `vite.config.ts`. That is typecheck wiring, not a listen port.

No second `vite.config.*` under `D:\Prop\apps`.

---

## 4. Live occupancy (this host, this check)

| Probe | Result |
|---|---|
| TCP listen `:3000` | **Yes** — `127.0.0.1:3000` Listen, pid **49100**, `node` |
| Command line | `node …\vite\bin\vite.js --host 127.0.0.1 --port 3000` |
| TCP listen `:5000` | **Yes** — `127.0.0.1:5000` Listen, pid **54468**, `TraderIntelligence.Api` `--urls http://127.0.0.1:5000` |
| TCP listen `:5173` / `:4173` / `:5160` / `:7294` / `:18720` | **No** |
| `GET http://127.0.0.1:3000/` | **200** `Content-Type: text/html` `Content-Length: 624` `Cache-Control: no-cache` |
| Injected scripts | `/@react-refresh`, `/@vite/client` — this is the **dev** server, not `vite preview` |
| Title | `MT5 Trader Intelligence` (matches `index.html`) |
| `GET http://127.0.0.1:5173/` | connect fail |
| `GET http://127.0.0.1:4173/` | connect fail |
| `GET http://127.0.0.1:5000/health` | **200** Kestrel `{"status":"ok",…}` |

C24 §0 / §11 (“Is Vite listening on 3000 right now? **No.**”) is **stale occupancy**, not a stale port law. The config hash is the same (`626F3F34…`). Someone started `npm run dev` after C24.

Loopback-only Vite (`127.0.0.1`) matches loopback-only API. Fine for README `localhost` (Windows resolves both). A browser on another machine cannot reach this pair without rebinding host.

---

## 5. How `:3000` talks to `:5000` (not a Vite-port change)

No `apps/web/.env`, `.env.local`, `.env.development`, or `apps/web/.env.example`. `VITE_API_URL` is unset.

| Consumer | Path | SHA-256 | Port meaning |
|---|---|---|---|
| axios | `apps/web/src/api/client.ts` | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | `baseURL = VITE_API_URL \|\| 'http://localhost:5000'` |
| SignalR | `apps/web/src/api/signalr.ts` | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | same base + `/hubs/dashboard` |
| hooks | `apps/web/src/api/hooks.ts` | (not re-hashed; C24 `5FDC969C…`) | relative `/api/*` on that base. Any `5000` in this file is a **refetch interval in ms**, not a port (B41) |

Topology:

```
browser
  → http://localhost:3000          Vite (HTML/JS/HMR websocket)
  → http://localhost:5000/api/…    axios (cross-origin, no proxy)
  → http://localhost:5000/hubs/…   SignalR (cross-origin; hub still 404 — D50)
```

`vite.config.ts` has **no** `server.proxy`. A later proxy that targets A62’s `:5160` would be a regression (B41 / C24 §7.3). If a proxy is added, target **`:5000`** and change axios/SignalR to a **relative** base, or the proxy is a no-op.

---

## 6. CORS vs the 3000 pin

`Program.cs` (4731 bytes, 95 newlines, SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` — **moved** vs C22 `E914FA98…` / 4658 B; CORS lines themselves are the same `AllowAnyOrigin` default):

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
…
app.UseCors();
```

Live:

| Probe | Result |
|---|---|
| `OPTIONS /api/overview` + `Origin: http://localhost:3000` | **204** `Access-Control-Allow-Origin: *` `Access-Control-Allow-Methods: GET` `Server: Kestrel` |
| `OPTIONS /api/overview` + `Origin: http://localhost:5173` | **204** `*` (same — `*` does not care) |
| `GET /health` + `Origin: http://localhost:3000` | **200** `Access-Control-Allow-Origin: *` |

Dead config (unread by `Program.cs`):

```json
"Cors": {
  "AllowedOrigins": [ "http://localhost:5173" ]
}
```

Present in both:

| File | Bytes | SHA-256 | LastWrite |
|---|---:|---|---|
| `apps/api/appsettings.json` | 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 2026-08-18T13:37:36 |
| `apps/api/appsettings.Development.json` | 478 | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | 2026-08-18T13:37:35 |

D53 / D64 / D80 already called this **DEAD CONFIG**. E015 adds the port-law consequence: **if that list is ever bound, it is the wrong origin.** Vite is **not** on 5173. Binding `5173` without `3000` would **break** the dashboard the moment `AllowAnyOrigin` is removed.

C22 class **`UNSAFE`** for `*` still stands. E015 does not re-score CORS. The 3000 pin is independent: the *correct* explicit origin, when someone tightens CORS, is **`http://localhost:3000`**.

---

## 7. Adjacent ports (do not confuse with Vite)

| Port | Role | Should Vite use it? |
|---|---|---|
| **3000** | Vite `npm run dev` / README dashboard | **Yes. This is the pin.** |
| **5000** | Kestrel `http` profile, Compose `--urls`, axios/SignalR fallback, `.http` `@api` | **No.** API. |
| **5173** | Vite 5 factory default + dead `Cors:AllowedOrigins` | **No.** Leftover. |
| **4173** | Vite 5 `preview` default | **No**, unless `vite preview --port 3000` or README is updated. |
| **5160** | Old API template (HEAD `launchSettings` only; worktree `http` is 5000) | **No.** Closed on disk. |
| **7294** | `https` profile first URL | **No.** |
| **18720** | IIS Express HTTP | **No.** |

`docker-compose.yml` (687 bytes, SHA-256 `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1`) publishes **`5000:5000`** for `api` only. **No `web` service.** Compose does not claim 3000. Dashboard remains a host-side `npm run dev`.

Workers (`mt5-worker` / `fix-worker` `launchSettings`) have **no** `applicationUrl`. They are not competing for 3000.

README (1746 bytes, SHA-256 `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764`), lines 42–43:

```
API: http://localhost:5000
Dashboard: http://localhost:3000
```

That pair is **TRUE** against current bytes and against the live listeners.

---

## 8. File census (this check)

| Path | Bytes | NL | SHA-256 | Role |
|---|---:|---:|---|---|
| `D:\Prop\apps\web\vite.config.ts` | 169 | 7 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | **`server.port: 3000`**, no proxy |
| `D:\Prop\apps\web\package.json` | 739 | 30 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` | `dev` = `vite` |
| `D:\Prop\apps\web\index.html` | 369 | 12 | `80656C860AC6F8C1FAB242789DEEF0803EC278028D8B0F24115A14536FDB8FD` | SPA shell, no port |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | axios → `:5000` |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | 28 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | hub → `:5000` |
| `D:\Prop\apps\web\src\main.tsx` | 648 | 22 | `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3` | no port |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | CORS `*`, no `UseUrls` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 41 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | Kestrel `:5000` |
| `D:\Prop\apps\api\appsettings.json` | 1254 | 50 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | dead `AllowedOrigins` **5173** |
| `D:\Prop\apps\api\appsettings.Development.json` | 478 | 21 | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | same dead 5173 |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | sample `:5000` |
| `D:\Prop\docker-compose.yml` | 687 | 30 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | API `5000:5000`, no web |
| `D:\Prop\README.md` | 1746 | 49 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | documents 5000 + **3000** |

`git status --short` on these paths: `M` `Program.cs`, `launchSettings.json`, `appsettings.json`. **`vite.config.ts` and `package.json` are clean vs HEAD** (or untracked-as-new; they were not listed as modified). Vite port pin is not a dirty-tree surprise.

C24’s `launchSettings` hash `E092DE59…` / 1133 B is **stale** (D75 closed weather leftover → `BC022898…` / 1125 B). Port on the `http` profile is still `:5000`.

---

## 9. What later waves must not do

When a coding wave is authorized (not this pass):

1. **Keep `server.port: 3000`.** Deleting it reverts to 5173 and falsifies README.
2. Do **not** bind Vite to 5000. Kestrel already owns it.
3. Do **not** “fix” `Cors:AllowedOrigins` by leaving it at 5173. If bound, use **`http://localhost:3000`**.
4. Do **not** paste A62’s proxy `target: …:5160`. If a proxy is added, target **`:5000`** and switch clients to a relative base.
5. Add `server.strictPort: true` only if README `:3000` must hard-fail when occupied.
6. `vite preview --port 3000` (or document 4173) if preview is an operator path.
7. Tighten CORS off `*` **before** cookies / privileged POST. Explicit origin = Vite **3000**, plus the real SPA origin later. Do not combine `AllowCredentials` with `*`.
8. Compose still has no `web` service. Adding one does not change the host-dev pin unless README changes with it.

---

## 10. Honest limits

- Did not start or stop pid 49100 / 54468. Occupancy is a snapshot; a later reboot can empty `:3000` without changing the pin.
- Did not use Chromium. HTML/CORS proven with `Invoke-WebRequest` / `HttpWebRequest`, not a real HMR websocket or SPA route paint.
- Did not execute `npm run build` or `vite preview`. Preview-port **4173** is from Vite constants + missing `preview.port`, not a live bind.
- Did not hit IIS Express `:18720` or HTTPS `:7294`.
- Did not start Compose (no web service to start anyway).
- `127.0.0.1` vs `localhost` Origin is a real browser distinction. Preflight used README spelling `http://localhost:3000`. `*` accepts either.
- `Program.cs` grew vs C22 (4658 → 4731 B). CORS policy text is still `AllowAnyOrigin`. E015 did not re-audit the rest of the pipeline.
- Architecture §§68 / §69 / §70 scores are **unchanged**. A dashboard origin is not a go-live gate.

---

## 11. One-line scorecard

| Question | Answer |
|---|---|
| Vite should be on? | **3000** |
| On-disk? | **Yes** — `vite.config.ts` `server.port: 3000` (`626F3F34…`) |
| Documented? | **Yes** — README `Dashboard: http://localhost:3000` |
| Live now? | **Yes** — pid 49100, `vite.js --port 3000`, `GET /` **200** |
| Factory 5173 in use? | **No.** Overridden. Not listening. Dead JSON only. |
| API port? | **5000** (separate process, also live) |
| 3000 vs 5000 a bug? | **No** |
| C24 “Vite down”? | **Stale occupancy.** Port law still correct. |
| Product source edited? | **No** |
