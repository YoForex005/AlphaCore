# E028 — `client.ts` `baseURL` is `http://localhost:5000`

| Field | Value |
|---|---|
| Agent | E028 (senior engineer, axios `client.ts` baseURL only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:15+05:30 (hashes, env, TCP, HTTP) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Artifact | `D:\Prop\reports\swarm\20260818\E028_client.md` |
| Assigned | `client.ts` baseURL 5000. Write this file. **Do not modify product source.** |
| Workspace | Vite app is `D:\Prop\apps\web` (not under `D:\Prop\src`). Axios module is `apps/web/src/api/client.ts`. |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Method | Full read of `client.ts` (9 lines). SHA-256 + byte / physical-line / last-write / BOM. `git hash-object` vs `HEAD:apps/web/src/api/client.ts`. Confirm sole importer (`hooks.ts`). Confirm `VITE_API_URL` absent in Process / User / Machine and no `apps/web/.env*`. Hash launchSettings / `.http` / `Program.cs` / Vite / SignalR / Overview / README / compose. `Get-NetTCPConnection` on 3000/5000/5160/5173/4173/7294/18720. Live GET `/health` + `/api/overview` and OPTIONS preflight against already-running Kestrel. Node eval of `\|\|` vs `??` empty-string. Did **not** `dotnet run`, `npm run`, or edit product source. |
| Binding law | A26 §2.1 transport (`/api/v1` + Bearer + envelope); A62 §2 empty `VITE_API_URL` = same origin / Vite proxy; A62 §5 `client.ts` (Bearer, `X-Correlation-Id`, 401→refresh, envelope unwrap) |
| Relates | B30 (data-layer), B41 (launch vs `:5000`), C24 / E012 (3000 vs 5000 split), D39 (hooks), D75 (`launchSettings` HEAD vs worktree), E003 (route matrix) |
| Does **not** supersede | E012 (listen-pair occupancy), B41 (IIS Express `:18720`), A62 (replacement client), D50 (missing hub) |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This file is the **axios module**. It is **not** a re-run of E012 (ports) or B30 (hooks + SignalR). The assigned claim is: the React HTTP client’s `baseURL` is port **5000**.

---

## 0. Verdict (binding)

**CONFIRMED.** `D:\Prop\apps\web\src\api\client.ts` creates one axios instance whose `baseURL` is `import.meta.env.VITE_API_URL || 'http://localhost:5000'`. `VITE_API_URL` is unset everywhere this process can see, so the **live** base is the hardcoded fallback `http://localhost:5000`.

That fallback **matches** the worktree lab API bind (`launchSettings` `http` profile, Compose `--urls`, README, `.http`, Overview error copy) and the already-running Kestrel process (`--urls http://127.0.0.1:5000`, `GET /health` = 200). It does **not** match `HEAD` `launchSettings` / `.http`, which still say **`:5160`**. The mismatch is on those uncommitted-vs-HEAD files, **not** on `client.ts`. `client.ts` is clean vs HEAD and has pointed at `:5000` since the initial commit.

| Question | Measured answer |
|---|---|
| Does `client.ts` exist? | **Yes** — `D:\Prop\apps\web\src\api\client.ts` (not under `D:\Prop\src`) |
| What is `baseURL` when `VITE_API_URL` is unset? | **`http://localhost:5000`** |
| Is `VITE_API_URL` set in this process / User / Machine? | **No** (name absent) |
| Is there `apps/web/.env*`? | **No** |
| Does Vite inject any `VITE_*` from root `D:\Prop\.env`? | **No** — no `VITE_` keys in that file; Vite `envDir` is default (`apps/web`) |
| Does worktree Kestrel `http` profile match? | **Yes** — `http://localhost:5000` |
| Does live Kestrel match? | **Yes** — pid **54468**, `127.0.0.1:5000`, `/health` 200 |
| Does `HEAD` `launchSettings` `http` match? | **No** — committed `applicationUrl` is `http://localhost:5160` |
| Is `:5160` listening now? | **No** |
| Is this the A26 / A62 production client? | **No** — demo transport only |
| Product source edited this pass? | **No** |

| Slice | Class |
|---|---|
| Lab fallback `:5000` vs worktree listen + live process | **`EXISTS_AND_GOOD`** |
| Lab fallback `:5000` vs `HEAD` launchSettings / `.http` `:5160` | **worktree closed; HEAD still MISMATCH** (on those files) |
| A62 §2 “empty `VITE_API_URL` = same origin” | **`EXISTS_NEEDS_REFACTOR`** (`\|\|` treats `""` as `:5000`) |
| A26 §2.1 / A62 §5 catalog client | **`EXISTS_NEEDS_REFACTOR`** (no `/api/v1`, no Bearer, no envelope, no interceptors) |

**Do not** change `client.ts` away from `:5000` unless launchSettings, README, Compose, `.http`, SignalR, and Overview copy move in the same commit. **Do not** treat “pages paint against port 5000” as `/api/v1` compliance.

---

## 1. The file (entire source)

Path: `D:\Prop\apps\web\src\api\client.ts`  
232 bytes, 9 physical lines, **no BOM** (first bytes `69 6D 70` = `imp`).  
SHA-256 `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78`  
LastWrite 2026-08-18T13:08:06.3731432+05:30  
`git hash-object` = `41b0a7849776eec878bbe08b20fdec39053aec61` = `HEAD:apps/web/src/api/client.ts`  
`git status --short` on this path: **clean**. Last commit touching it: `6c41447` 2026-08-18 13:12:17 +0530 `Initial commit`.

```1:9:D:\Prop\apps\web\src\api\client.ts
import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

export default client;
```

`api/` contains exactly three TypeScript files. This one is the only `axios.create`. Grep of `apps/web/src` for `from './client'` / `from '../api/client'` = **one** hit: `hooks.ts` L2.

Installed axios (from `apps/web/node_modules/axios/package.json`): **1.19.0** (`package.json` range `^1.7.0`).

There is **no** `src/vite-env.d.ts`, **no** `src/env.d.ts`, **no** authored `*.d.ts` under `apps/web` outside `node_modules`. `tsconfig.json` has no `"types": ["vite/client"]`. `import.meta.env.VITE_API_URL` is an untyped escape (`skipLibCheck: true`).

---

## 2. How `baseURL` actually resolves

Vite only injects `VITE_*` from `apps/web/.env*`. Those files **do not exist**. Root `D:\Prop\.env` exists (3408 B, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA`, gitignored) and contains **zero** `VITE_` / `5000` / `API_URL` keys.

| Name | Process | User | Machine | `Test-Path Env:` |
|---|---|---|---|---|
| `VITE_API_URL` | **absent** | **absent** | **absent** | False |
| `ASPNETCORE_URLS` | absent | absent | absent | False |

So at bundle time `import.meta.env.VITE_API_URL` is `undefined`. `undefined || 'http://localhost:5000'` → **`http://localhost:5000`**.

Every `hooks.ts` call is a **relative** path (`/api/overview`, …). Axios joins `baseURL` (no trailing slash) + path (leading slash) → `http://localhost:5000/api/…`. There is **no** Vite `server.proxy`, so the browser talks to `:5000` itself.

`timeout: 15000` is **milliseconds** (15 s), not a port. `hooks.ts` `refetchInterval: 5000` is also **ms**. `QuoteDisplay.tsx` `age > 5000` is quote-age ms. Do not file those as port 5000.

### 2.1 The `||` empty-string trap (A62 §2)

A62 §2 / B10 / A65 require: empty `VITE_API_URL=` means **same origin / Vite proxy**.

Node eval this pass:

| Input | `v \|\| 'http://localhost:5000'` | `v ?? 'http://localhost:5000'` |
|---|---|---|
| `undefined` | `http://localhost:5000` | `http://localhost:5000` |
| `""` | **`http://localhost:5000`** | `""` (same origin) |
| `"http://localhost:5000"` | `http://localhost:5000` | `http://localhost:5000` |
| `"http://localhost:3000"` | `http://localhost:3000` | `http://localhost:3000` |

Today there is **no** `.env` setting the empty string, so the trap is **latent**. It becomes a hard miss the moment a later wave adds `apps/web/.env.example` with `VITE_API_URL=` and a Vite proxy, without changing `||` to `??` (or explicit empty handling). C24 §7.4 / A65 §664 already named this. Still true. File SHA unchanged.

`signalr.ts` L3 **duplicates** the same expression (`const BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000'`) and does **not** import `client.ts`. Two independent fallbacks. They match today. They can drift.

---

## 3. Match matrix — `:5000` vs every operator

`client.ts` fallback = `http://localhost:5000` (HEAD **and** worktree; file is clean).

| Operator | Measured bind | vs client `:5000` | Git |
|---|---|---|---|
| `launchSettings` `http` **worktree** | `http://localhost:5000` | **MATCH** | **dirty** vs HEAD |
| `launchSettings` `https` HTTP half **worktree** | `http://localhost:5000` | **MATCH** (also binds `:7294` first) | dirty |
| `launchSettings` IIS Express | `http://localhost:18720` + ssl `44389` | **MISMATCH** if F5 uses this profile | same ports on HEAD |
| `launchSettings` `http` **HEAD** | `http://localhost:5160` | **MISMATCH** | committed stock template |
| `TraderIntelligence.Api.http` **worktree** | `@api=http://localhost:5000` | **MATCH** | **dirty** vs HEAD |
| `TraderIntelligence.Api.http` **HEAD** | `@…_HostAddress = http://localhost:5160` + `GET /weatherforecast/` | **MISMATCH** | committed |
| `README.md` | `API: http://localhost:5000` | **MATCH** | **clean** (HEAD already 5000) |
| `docker-compose.yml` | `--urls http://0.0.0.0:5000` + `5000:5000` | **MATCH** | **untracked** |
| `vite.config.ts` | `server.port: 3000`, **no** `proxy` | N/A — SPA origin | clean |
| `signalr.ts` | same `\|\| 'http://localhost:5000'` then `/hubs/dashboard` | **MATCH** (port); hub path 404 | clean |
| `OverviewPage.tsx` | “Start the ASP.NET API on port **5000**.” | **MATCH** (operator copy) | — |
| `Program.cs` | `app.Run()` — no `UseUrls` / `Listen` | inherits launch / `--urls` | — |
| Live process pid 54468 | `--urls http://127.0.0.1:5000` | **MATCH** | running from worktree bin |
| A06 / A54 / A62 / A63 / A65 prose `:5160` | stale swarm text | **DOC MISMATCH** | do not implement A62’s proxy target |

Worktree `launchSettings.json`: 1125 B, BOM `EF BB BF`, SHA-256 `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` (same as D75 / E003 / E012). `http.applicationUrl` is `:5000`. `IIS Express.launchUrl` is `swagger` (weather leftover **closed on disk**).

HEAD `launchSettings.json` is still the `dotnet new webapi` template: `http`/`https` **`:5160`**, all three `launchUrl` = `weatherforecast`. A clean checkout + `dotnet run` (default first profile) would listen on **`:5160`** while this client (also in that checkout) would still call **`:5000`**. That is the **only** remaining `:5160` vs `:5000` product-tree split, and it is **uncommitted launchSettings / `.http`**, not `client.ts`.

Do **not** “fix” the committed pair by moving `client.ts` to `:5160`. Move launchSettings / `.http` to `:5000` (already done in the worktree) and commit them.

---

## 4. Live probe (this host, this check)

Did not start or kill processes. Occupancy:

| Port | Listen? | Owner |
|---|---|---|
| **5000** | **Yes** `127.0.0.1` | pid **54468** `TraderIntelligence.Api` — `"D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.exe" --urls http://127.0.0.1:5000` |
| **3000** | **Yes** `127.0.0.1` | pid **49100** `node` — `vite.js --host 127.0.0.1 --port 3000` |
| 5160 / 5173 / 4173 / 7294 / 18720 | **No** | — |

| Request | Result |
|---|---|
| `GET http://127.0.0.1:5000/health` | **200** 57 B `Server: Kestrel` |
| `GET http://localhost:5000/health` | **200** (hostname spelling the client uses) |
| `GET http://127.0.0.1:5000/api/overview` | **200** 297 B demo JSON (`totalAccounts: 4`, …) |
| `OPTIONS /api/overview` + `Origin: http://localhost:3000` + `Access-Control-Request-Headers: content-type` | **204** `Access-Control-Allow-Origin: *` `Allow-Methods: GET` `Allow-Headers: content-type` |
| `GET /api/overview` + `Origin: http://localhost:3000` + `Content-Type: application/json` | **200** + `*` |
| `GET http://127.0.0.1:3000/` | **200** Vite HTML (HMR inject present) |

`client.ts` always sets `Content-Type: application/json` on the instance. Axios applies that as a default header, so even **GET** is a CORS **preflight**. That is why `AllowAnyOrigin` + `AllowAnyHeader` is not optional for this stub. Measured: the preflight **succeeds** against the running API.

Vite was launched with `--host 127.0.0.1`. An operator who opens `http://127.0.0.1:3000` has Origin `http://127.0.0.1:3000`, while axios still targets `http://localhost:5000`. CORS `*` accepts either today. Cookie/`AllowCredentials` later will **not**.

---

## 5. What this client is **not** (A26 §2.1 / A62 §5)

| Capability | Disk |
|---|---|
| One HTTP client | **Yes** — this file, axios 1.19 |
| Base URL | hardcoded fallback `:5000` (lab) |
| Vite proxy `/api` + `/hubs` | **MISSING** |
| `Authorization: Bearer` interceptor | **MISSING** |
| `X-Correlation-Id` | **MISSING** |
| 401 → `POST /api/v1/auth/refresh` → `/login` | **MISSING** |
| A26 envelope unwrap (`body.data`) | **MISSING** — hooks use raw `r.data` |
| `ApiError` from `error.code` / `correlationId` | **MISSING** |
| `Idempotency-Key` on POST | **N/A** — this module has no POST helper; hooks have 0 mutations |
| Secret-field client denylist | **MISSING** |
| `withCredentials` | **MISSING** (correct while CORS is `*`) |
| Typed `axios.create<…>` / generics | **MISSING** |
| Default export only | **Yes** — no named `api` / `getBaseUrl` |

A26 §2.1 API base is **`/api/v1`**. This client does not encode that prefix; `hooks.ts` hard-codes unversioned `/api/…`. Demo `Program.cs` has **zero** `/api/v1/**` maps (E003). Retargeting `baseURL` to `/api/v1` **or** prefixing paths against today’s host would 404 every tile.

Class as a **catalog** client: **`EXISTS_NEEDS_REFACTOR`**. Class as a **lab transport to the demo host on :5000**: **`EXISTS_AND_GOOD`**.

B30 already said this. SHA of `client.ts` is **unchanged** (`9A04E60C…`). E028 re-measures the **port** claim and the HEAD-vs-worktree split B30 did not freeze.

---

## 6. Consumers (port path only)

| File | Bytes | SHA-256 | How it uses `:5000` |
|---|---:|---|---|
| `apps/web/src/api/hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | **only** importer. 11 `client.get('/api/…')`. Dirty vs HEAD (not a port change). |
| `apps/web/src/api/signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | **does not** import client. Own `BASE` fallback `:5000` + `/hubs/dashboard` (404; D50). |
| `apps/web/src/pages/OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | error string names port **5000**. Uses `useOverview`, not `client` directly. |

No other `axios` / `fetch(` / `baseURL` under `apps/web/src`.

---

## 7. Adjacent issues (do **not** reclassify the fallback)

1. **HEAD `:5160` vs client `:5000`** — closed on the worktree (`launchSettings` + `.http` dirty). A clean clone still has the miss until those two files are committed. `client.ts` is already right.
2. **IIS Express `:18720`** — B41 still open. Client will not find the API if F5 uses that profile.
3. **`https` first URL `:7294`** — Kestrel binds both; axios never uses 7294. Soft docs split.
4. **A62 proxy snippet still says `:5160`.** Pasting it would reopen a closed miss. B10 retargeted the same snippet to `:5000`. A proxy without changing `baseURL` to relative/`""` is a **no-op** (browser never hits Vite).
5. **Compose `5000:5000` vs host `dotnet run`** — both claim the API port (B37 / D63). Not a client bug. Compose file is **untracked**.
6. **CORS `*`** — required by this client’s absolute `:5000` + JSON Content-Type. **UNSAFE** once cookies or privileged POST exist (C22 / D103).
7. **SignalR 404** — same host:port, unmapped hub. Do not retarget `baseURL` to “fix” the hub.
8. **Do not** collapse Vite onto 5000 or the API onto 3000 (E012 / C24).

---

## 8. Authorized later work (do **not** apply in E028)

When a coding wave is authorized:

1. **Keep** the `:5000` fallback until the host listen story is one number in launchSettings (committed), `.http`, README, Compose, Overview copy, and SignalR.
2. **Commit** the already-dirty worktree `launchSettings.json` + `.http` (`:5160` → `:5000`) so HEAD matches `client.ts`.
3. If adding a Vite proxy: target **`http://localhost:5000`** (never A62’s `:5160`); change axios/SignalR to a **relative** base; use `??` / explicit empty handling, not `||`.
4. Add `apps/web/.env.example` + `vite-env.d.ts` **only** with the client change in the same commit.
5. Replace this file against A26 §2 / A62 §5 (Bearer, correlation, envelope, `ApiError`) when `/api/v1` exists on the host. Do not retarget paths first.
6. Deduplicate SignalR `BASE` — import a single `getApiBase()` from this module.

None of these were implemented this pass.

---

## 9. File census (this check)

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `D:\Prop\apps\web\src\api\client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | **SUT** — axios `:5000` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | sole importer |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | twin fallback |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | operator copy |
| `D:\Prop\apps\web\vite.config.ts` | 169 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | UI `:3000`, no proxy |
| `D:\Prop\apps\web\package.json` | 739 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` | `axios ^1.7.0` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | worktree `:5000` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | worktree `:5000` |
| `D:\Prop\apps\api\Program.cs` | 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | CORS `*`, no listen override |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `5000:5000` untracked |
| `D:\Prop\README.md` | 1746 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | documents `:5000` |
| `D:\Prop\.env` | 3408 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | no `VITE_*` |

`client.ts` SHA is **identical** to B30 / B41 / C24 / E003 / E012. The module has not moved.

---

## 10. Honest limits

- Did not execute the TypeScript through Vite’s env replacement beyond the measured absence of `VITE_API_URL`. Did not dump the bundled `dist` (no `npm run build` this pass).
- Did not drive Chromium. CORS was `Invoke-WebRequest` with `Origin` / `OPTIONS`, not a real axios XHR from the SPA.
- Did not launch IIS Express or Compose. Did not hit `:18720`.
- Did not stop pids 54468 / 49100.
- `HEAD` `.http` / `launchSettings` were read via `git show`; worktree hashes via `Get-FileHash`.
- Did not print any secret values. Root `.env` was scanned for **key names** only.

---

## 11. One-line scorecard

| Question | Answer |
|---|---|
| `client.ts` `baseURL` fallback? | **`http://localhost:5000`** |
| Is that the live value today? | **Yes** — `VITE_API_URL` unset, no web `.env*` |
| Does the running API sit there? | **Yes** — Kestrel `:5000`, `/health` 200 |
| Does worktree launchSettings agree? | **Yes** |
| Does `HEAD` launchSettings agree? | **No** — still `:5160` until the dirty file is committed |
| Is this the A26 client? | **No** |
| Product source edited? | **No** |
