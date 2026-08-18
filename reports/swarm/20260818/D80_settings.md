# D80 — `SettingsPage.tsx` (route `/settings`)

| Field | Value |
|---|---|
| Agent | D80 (senior engineer, Settings page only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:22+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `SettingsPage.tsx`. Write this file. **Do not modify product source.** |
| Target | `D:\Prop\apps\web\src\pages\SettingsPage.tsx` |
| Adjacent (read, not edited) | `hooks.ts`, `types/index.ts`, `client.ts`, `App.tsx`, `DashboardLayout.tsx`, `Program.cs`, `SettingsController.cs`, `appsettings.json`, `TraderIntelligence.Api.http`, `Api.csproj` |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No** |
| API launched | **No.** Wire body is from `Program.cs` source, not an HTTP capture. |
| Method | Full `read_file` of the page + hook + types + router + layout + live `MapGet` + unmapped controller. PowerShell `Get-FileHash SHA256` + byte / physical-line / non-blank / last-write. Grep for `useMutation` / `PUT` / `PATCH` / `AddControllers` / `MapControllers`. Cross-check A26 §5.2 / §6.16 / §9–10, A49, A62, A63 §4.2 / §7.2, A57, D08 §7.15, D30, D39, D40, D53, D55, D69. |
| Binding law | Architecture v2 **§46** (nav leaf `Settings`), **§41** (flags), **§55** / **§72.5** (never expose secrets to the browser). A26 is the page/API contract. A63 decides what first-useful (§69) must ship. |
| Precedence | On-disk `Program.cs` is the **live** `GET /api/settings`. `SettingsController` is compiled but **unmapped**. D08 / C08 page SHA is still this file. D55 already classified the controller vs Redis. |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **page census**. It is **not** a claim that A26 widgets are painted, that `/api/v1/settings` exists, that RBAC is on, or that Redis backs runtime flags.

---

## 0. Verdict

**`/settings` is a 12-line JSON dump of a hardcoded demo GET. The §46 leaf and A26 path exist. The A26 Settings page does not.**

The caption *“Secrets are never returned to the browser.”* is **true of the live payload** (D40). It is **not** a sanitizer, not a contract, and not a guarantee if `SettingsController` is ever `MapControllers`’d.

| Question | Measured answer |
|---|---|
| Does `SettingsPage.tsx` exist? | **Yes.** 459 B, 12 physical / 11 non-blank, SHA-256 `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` (unchanged vs C08 / D08 / B20). Last write `2026-08-18T13:16:43`. **Untracked** (`??` vs HEAD). |
| Is it routed? | **Yes.** `App.tsx` L17 import + L38 `<Route path="settings" element={<SettingsPage />} />` under `DashboardLayout`. |
| Sidebar? | **Yes.** Last nav row: `{ to: '/settings', label: 'Settings', icon: '⚙' }`. Label is an **exact** §46 string (unlike Live / Recon / Health / Groups). |
| A26 path? | **Yes.** `/settings` matches A26 §5.2. |
| A26 page contract? | **No.** No execution-flag form, no symbol-mapping table, no FIX non-secret host/port/comp-id form. One `<pre>{JSON.stringify(data, null, 2)}</pre>`. |
| Catalog GET? | **No.** Hook is `GET /api/settings`. A26 is `GET /api/v1/settings`. A63 first-useful is `GET /api/v1/settings/public`. Catalog hits **0**. |
| Mutation from this page? | **None.** No `useMutation`, no `<form>`, no `client.put` / `patch`. **Aligned** with A63 §7.2 (park `PUT /api/v1/settings` and `PATCH …/execution`). |
| Live host body? | `Program.cs` L42–47 hardcoded `{ riskLimits, featureFlags.REAL_COPY_EXECUTION_ENABLED=false, brokerConfigs: ACHIEVER + STARWAVEFX }`. **Not** `IConfiguration`. **Not** Redis. |
| Is `SettingsController` live? | **No.** `[Route("api/settings")]` exists (`??` untracked, SHA `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F`). Host has **0** `AddControllers` / `MapControllers`. Constructor needs `IConnectionMultiplexer`, which is **not** registered (D55). |
| Password / API-key fields on the page? | **0.** No input. Dead TS `Settings` has none. Live GET has none. Controller GET omits `EmergencyFlattenApiKey`. |
| Tests? | **0** hits under `D:\Prop\tests` for `SettingsPage` / `useSettings` / `/api/settings`. |
| Product source edited this pass? | **0** |

**One-liner:** Settings chrome is present and secret-safe by absence; it is a `<pre>` of a stub, not the Settings page.

§73.B for the **file + route + exact nav label**: `EXISTS_AND_GOOD` as chrome.  
§73.B for the **page vs A26 §6.16 / A62 §10.9**: `EXISTS_NEEDS_REFACTOR` (file) / **`MISSING`** (widgets).  
§73.B for **§69 first-useful Settings read**: `EXISTS_NEEDS_REFACTOR` — a read-only flag dump is the A57/A63 shape; this dump is the wrong path and the wrong JSON.  
§73.B for **live `GET /api/settings`**: `EXISTS_NEEDS_REFACTOR` (honest-enough demo object; flag is a **literal** `false`, D69).  
§73.B for **`SettingsController`**: `EXISTS_NEEDS_REFACTOR` (compiled, unmapped). Its `PUT` is **`UNSAFE` if wired** (anonymous Redis `settings:*`, no confirm phrase, GET does not read what PUT wrote).  
§73.B for **`types/index.ts` `Settings`**: `DEPRECATED` (zero imports; shape happens to match the stub).

Do **not** recreate the file. Do **not** `MapControllers` this controller. Do **not** add a whole-object `PUT` to “finish” Settings. Do **not** treat the caption as a sanitizer.

---

## 1. Measured files

| Path | Bytes | Phys. | Non-blank | SHA-256 | Last write (local) | Git |
|---|---:|---:|---:|---|---|---|
| `D:\Prop\apps\web\src\pages\SettingsPage.tsx` | 459 | 12 | 11 | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` | 2026-08-18T13:16:43 | **`??` untracked** |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | 42 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00 | `M` unstaged |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | 123 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18 | (clean vs this blob) |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | 41 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38 | `M` unstaged |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | 41 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38 | (same wave as App) |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | 7 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06 | clean |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | 86 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15 | `M` unstaged |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | 3732 | 94 | 83 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | 2026-08-18T13:37:39 | **`??` untracked** |
| `D:\Prop\apps\api\appsettings.json` | 1254 | 50 | 50 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 2026-08-18T13:37:36 | grew vs D30 `8DCE4CBE…` / 431 B |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | 8 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | 2026-08-18T13:20:38 | **no** `/api/settings` sample |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 21 | 17 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | 2026-08-18T12:55:15 | no MVC extras |

Encoding: page + hooks + `Program.cs` = UTF-8 no BOM, **LF**. Controller + types + App = UTF-8 no BOM, **CRLF**.

Grep `AddControllers` / `MapControllers` under `apps/api`: **0**.  
Grep `useMutation` / `put(` / `patch(` / `post(` in `hooks.ts`: **0**.  
Grep `from ['"].*types` under `apps/web/src`: **0** (D39). The page does not import `Settings`.

---

## 2. Entire page (12 lines)

```tsx
import { useSettings } from '../api/hooks';

export default function SettingsPage() {
  const { data } = useSettings();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">Settings</h1>
      <p className="text-sm text-gray-400 mb-4">Secrets are never returned to the browser.</p>
      <pre className="bg-gray-950 border border-gray-800 rounded p-4 text-sm text-gray-200">{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
}
```

| Fact | Count / value |
|---|---|
| Imports | **1** (`useSettings`) |
| Hooks used | **1** (`useQuery` wrapper) |
| Fields read off `data` | **0** — whole object stringified |
| `isLoading` / `isError` / `error` | **unused** |
| Loading UI | **none.** `JSON.stringify(undefined)` is JS `undefined`; React renders an empty `<pre>` until the first success |
| Error UI | **none.** Failed query → same empty `<pre>` |
| `<form>` / `<input>` / `<button>` | **0** |
| `password` / `apiKey` / `secret` tokens in JSX | **0** (caption word “Secrets” only) |
| Typed `useQuery<Settings>` | **no** |
| Sibling dump pages | `ReconciliationPage` (same 12-line pattern + different sentence); `SystemHealthPage` (title + `<pre>` only, 11 lines) |

Class vs siblings: **JSON dump**, same family as Recon / Health. Not a static stub (Audit / Live / Shadow have no hook). Not a widget page (Risk has `MetricCard`s).

---

## 3. Router + nav (this leaf only)

```text
/settings  →  SettingsPage
sidebar    →  Settings  ⚙     (14th / last row)
```

| Check | Required | Now | Class |
|---|---|---|---|
| §46 label | `Settings` | `Settings` | **EXISTS_AND_GOOD** |
| A26 §5.2 path | `/settings` | `/settings` | **EXISTS_AND_GOOD** |
| A62 folder | `pages/settings/SettingsPage.tsx` | `pages/SettingsPage.tsx` | path **WRONG**, file **present** |
| A26 §5.3 header strip | `REAL_COPY` + `STOP_NEW` + FIX/MT5 health | layout is sidebar + `<Outlet />` only | **MISSING** on every page, including this one |
| Catch-all → `/overview` | A26 §5.2 | **0** `path="*"` | unknown URLs empty `<Outlet />` (D38) |
| Auth wrapper | A26 / A51 | **none** | **MISSING** (D53) |

This is one of **7 / 16** exact §46 labels (D38). Do not “fix” the Settings label.

---

## 4. Hook the page actually calls

`hooks.ts` L51–53 (SHA `5FDC969C…`, unchanged vs D08 / D39):

```ts
export function useSettings() {
  return useQuery({ queryKey: ['settings'], queryFn: () => client.get('/api/settings').then(r => r.data) });
}
```

| Item | Measured |
|---|---|
| Query key | `['settings']` — matches A62 query-key sketch |
| HTTP | `GET /api/settings` via axios `client` |
| `baseURL` | `import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` |
| Vite `/api` proxy | **none** (`vite.config.ts` port 3000 only) |
| `refetchInterval` | **none** (correct; settings are not a live tape) |
| Inherited QueryClient | retry 2, `refetchOnWindowFocus: false`, `staleTime: 30_000` (`main.tsx`) |
| Envelope unwrap | `r.data` = raw axios body. Host returns a bare object, not `{ data: … }` |
| Mutation hook | **MISSING** — A62-era `useUpdateSettings` / `PUT /api/settings` is **gone**. Absence is **correct** for §69 (A57, A63 §7.2) |
| Catalog path | A26 `GET /api/v1/settings` / A63 `GET /api/v1/settings/public` — **0** hits |

D39: hook→host path hit for this row is **yes** (demo). Catalog hit is **no**.

---

## 5. Two backends, one URL — only one is live

### 5.1 Live: `Program.cs` L42–47

```csharp
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    brokerConfigs = new[] { new { id = "ACHIEVER", name = "Achiever", enabled = true }, new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true } }
}));
```

Expected camelCase wire body (minimal-API JSON + dictionary keys as written):

```json
{
  "riskLimits": {
    "maxQuoteAgeSeconds": 3,
    "maxSignalAgeSeconds": 15
  },
  "featureFlags": {
    "REAL_COPY_EXECUTION_ENABLED": false
  },
  "brokerConfigs": [
    { "id": "ACHIEVER", "name": "Achiever", "enabled": true },
    { "id": "STARWAVEFX", "name": "StarwaveFX", "enabled": true }
  ]
}
```

| Property | Source | Honest? |
|---|---|---|
| `maxQuoteAgeSeconds = 3` | literal | Not read from A49 `MAX_QUOTE_AGE_MS` / risk engine |
| `maxSignalAgeSeconds = 15` | literal | Not a bound option |
| `REAL_COPY_EXECUTION_ENABLED = false` | literal | **Correct default** (D69). **Not** `CTraderFixOptions.RealCopyExecutionEnabled`, **not** `FeatureFlags:LiveCopyEnabled`, **not** env `REAL_COPY_EXECUTION_ENABLED` |
| Broker ids | literals | Match demo seeder codes. Not a live broker registry probe |

Does **not** dump `appsettings` / `CTraderFix` / `ConnectionStrings` / `EmergencyFlattenApiKey`. Caption holds.

Does **not** include A26 `execution.*`, `fixNonSecret.*`, or `symbolMappings[]`.

### 5.2 Dead: `Controllers/SettingsController.cs`

`[ApiController] [Route("api/settings")]` + `HttpGet` + `HttpPut`.

**Why it cannot serve today**

1. `Program.cs` never calls `AddControllers()` / `MapControllers()`.
2. Constructor requires `IConnectionMultiplexer`. `AddTraderIntelligence` does not register one (D23 / D55).
3. Even if both were added, ASP.NET would have **two** `GET /api/settings` (minimal map + controller). Startup would fail or the map would win — do not find out in production.

**GET (would-be body)** reads `IConfiguration` defaults, **not** Redis:

| Config key | Default in controller | `appsettings.json` now |
|---|---|---|
| `RiskEngine:MaxDailyDrawdownPct` | `5.0m` | `5.0` |
| `RiskEngine:MaxPositionSize` | `10.0m` | `10.0` |
| `RiskEngine:MaxOpenPositions` | `20` | `20` |
| `RiskEngine:KillSwitchEnabled` | `true` | `true` |
| `FeatureFlags:ShadowTradingEnabled` | `true` | `true` |
| `FeatureFlags:LiveCopyEnabled` | `false` | `false` |
| `FeatureFlags:AutoPromotionEnabled` | `false` | `false` |

`KillSwitchOn`, `StopNewExecutionOn`, `EmergencyFlattenApiKey` are **not** projected. Empty API-key slot stays off the wire — **good**, and **not** a sanitizer (the property is simply omitted).

**PUT** writes Redis strings and returns `{ Updated: true }`:

| Body field | Redis key |
|---|---|
| `RiskEngine.MaxDailyDrawdownPct` | `settings:risk:max_daily_drawdown_pct` |
| `RiskEngine.MaxPositionSize` | `settings:risk:max_position_size` |
| `RiskEngine.MaxOpenPositions` | `settings:risk:max_open_positions` |
| `FeatureFlags.ShadowTradingEnabled` | `settings:flags:shadow_trading` |
| `FeatureFlags.LiveCopyEnabled` | `settings:flags:live_copy` |
| `FeatureFlags.AutoPromotionEnabled` | `settings:flags:auto_promotion` |

`KillSwitchEnabled` is on GET and **absent** from PUT. GET never `StringGet`s those keys. A successful PUT would be **invisible** on the next GET.

A99 / D55: `settings:*` is **not** an allow-listed family (`ti:` + env + TTL). Generic `StringSetAsync` of flags would make Redis a settings book — architecture §5 says Redis is coordination/cache, **not** SoT.

No `[Authorize]`, no `confirmPhrase`, no audit row, no floor that prevents raising live-copy above the config default (A49: SuperAdmin PATCH cannot raise `REAL_COPY` above the config floor). `LiveCopyEnabled` is **not** `RealCopyExecutionEnabled` (D69). Wiring this PUT would still **not** enable 35=D — and would still be an anonymous write on `AllowAnyOrigin`.

### 5.3 `appsettings.json` (SHA `69D41CAD…`) is not the live GET

The file now has `RiskEngine` + `FeatureFlags` + `CTraderFix` + `ConnectionStrings:Redis`. Live `MapGet` **ignores all of it**. B13 / A101 claims that API `appsettings` contains `CTrader:RealCopyExecutionEnabled` are **stale** (D69). `Cors:AllowedOrigins` is `http://localhost:5173`; live CORS is still `AllowAnyOrigin` (D53). Vite is **:3000**.

---

## 6. Dead TypeScript `Settings` vs both wires

`types/index.ts` L131–135 (never imported):

```ts
export interface Settings {
  riskLimits: Record<string, number>;
  featureFlags: Record<string, boolean>;
  brokerConfigs: { id: string; name: string; enabled: boolean }[];
}
```

| Shape | Matches live `Program.cs`? | Matches controller GET? | Matches A26 §6.16? | Matches A63 §4.2? |
|---|---|---|---|---|
| TS `Settings` | **yes** (demo coincidence) | **no** (`riskEngine` / nested flags, no `brokerConfigs`) | **no** | **no** |
| A26 `{ execution, fixNonSecret, symbolMappings }` | no | no | — | different first-useful slice |
| A63 `{ realCopyExecutionEnabled, fixQuoteEnabled, fixTradeSessionEnabled, shadowCopyEnabled, canonicalSymbol, xauMapped, destinationInstrumentId }` | only the false live-copy bit, under a different name | `LiveCopyEnabled` ≠ that name | — | — |

Do **not** type `useSettings` against this interface and call the contract done. Do **not** rewrite the interface to the controller shape either — the controller is not the catalog.

---

## 7. A26 / A62 / A63 / §69 matrix

Architecture **§46** lists `Settings` as the last main-nav leaf and does **not** enumerate widgets (unlike §§47–54). Widget law is A26 + A62.

| Required surface | Spec | Disk | Class |
|---|---|---|---|
| Nav label `Settings` | §46, A26 §5.3 | exact | `EXISTS_AND_GOOD` |
| Route `/settings` | A26 §5.2 | exact | `EXISTS_AND_GOOD` |
| `GET /api/v1/settings` envelope `{ execution, fixNonSecret, symbolMappings }` | A26 §6.16 | **MISSING** | `MISSING` |
| `GET /api/v1/settings/public` (small flag set) | A63 §4.2, A57 | **MISSING** | `MISSING` (demo GET is a stand-in) |
| Execution-flag widgets + `passwordConfigured` (boolean only) | A26 / A62 §10.9 | **MISSING** | `MISSING` |
| Symbol-mapping table / upsert | A26 `PATCH /settings/symbol-mappings` | **MISSING** | `MISSING` |
| FIX non-secret host / ports / SSL / sender+target ids | A26 `PATCH /settings/fix` | **MISSING** | `MISSING` |
| Password field | **must not exist**; 422 if sent | **absent** | `EXISTS_AND_GOOD` |
| `PATCH /settings/execution` + phrase `ENABLE_REAL_EXECUTION` | A26 §6.16 / §10.2 SuperAdmin | **absent** | **correctly out of §69** (A63 §7.2) |
| Whole-object `PUT /api/settings` | A62 stub; A63 “secret-leak vector” | **absent** from React | `EXISTS_AND_GOOD` as absence |
| Controller `PUT` Redis | not in any catalog | compiled, unmapped | do not ship |
| RBAC on GET (ReadOnly+) | A26 §10.1 | anonymous | `MISSING` (D53) |
| Header strip flags | A26 §5.3 | **MISSING** | shell gap, not this file |
| Client denylist `components/secrets/denylist.ts` | A62 | **MISSING** | `MISSING` |
| `.http` sample | C50 / D30 | **no** `/api/settings` | `MISSING` |

A30 increment / A57: first-useful Settings is a **read** of public flags, not a write console. Growing this `<pre>` into PATCH forms would **violate** A63 §7.2, not close §69.

---

## 8. Secrets (this page’s only hard invariant)

Architecture §72.5: *Never expose secrets to the browser.* §55 denylist: MT5 passwords, proxy credentials, cTrader account password, FIX password, database passwords, Redis passwords.

| Surface | Secret on the wire? |
|---|---|
| Live `GET /api/settings` | **No.** Flags + two broker **names**. |
| Caption on the page | Marketing of the above. Not a filter. |
| Controller GET (if mapped) | **No password fields.** Omits `EmergencyFlattenApiKey`. Still not a sanitizer. |
| Controller PUT (if mapped) | Writes booleans/decimals to Redis. Does not accept a password property **today**. Anonymous + `AllowAnyOrigin` is still **UNSAFE** as an ops door. |
| `appsettings.json` `EmergencyFlattenApiKey` | `""` on disk. **Not** returned by either GET. |
| `appsettings.json` `ConnectionStrings:Postgres` | `Password=` empty slot. **Not** on the settings GET. |
| React types / page | No password property. |

D40 remains correct for this leaf. Caption is **true today**.

---

## 9. What a browser session actually does

```text
Vite :3000  →  /settings
  DashboardLayout.startConnection() → /hubs/dashboard  (host: no MapHub → fail swallowed)
  SettingsPage
    useSettings
      GET http://localhost:5000/api/settings     (CORS *)
        → Program.cs anonymous object
        → <pre> pretty JSON
```

No loading spinner. No error toast. No refetch on SignalR (hub missing — D50). After 30 s the query is stale but does not poll. A failed API leaves a blank dark `<pre>`.

`.http` does not exercise this path. Swagger UI is not mounted (`UseSwagger` without `UseSwaggerUI`; D30).

---

## 10. Stale prior reports (do not copy blindly)

| Report | Claim | Now |
|---|---|---|
| A62 §0 / §3 | `pages/` empty; hooks still have `PUT /api/settings` | **Stale.** File exists. Hook is GET-only. |
| A62 folder plan | `pages/settings/SettingsPage.tsx` | Flat `pages/SettingsPage.tsx` |
| B10 / B22 “13 pages” | Settings present in that 13 | Still present; Live/Audit added later. SHA of this file **unchanged**. |
| D02 / D39 “zero controllers” | API has no `Controllers/` | **Stale as of 13:37.** File exists; **still unmapped.** D55 / D69 already noted it. |
| D30 `appsettings` SHA `8DCE4CBE…` / 431 B | small CTrader block | File is now 1254 B / `69D41CAD…` with `RiskEngine` + `FeatureFlags`. **Live GET still ignores it.** |
| “Settings is done because the caption is honest” | — | Caption ≠ A26 page. |

Unchanged and still true: D08 §7.15 (JSON dump, no PATCH); C18 (no write buttons to gate); D39 row 11; D40 caption/payload; D53 anonymous GET; D55 do-not-map; D69 flag is a literal.

---

## 11. Classification roll-up

| Slice | Class |
|---|---|
| File exists + default export `SettingsPage` | `EXISTS_AND_GOOD` |
| `/settings` route + exact §46 nav label | `EXISTS_AND_GOOD` |
| Page widgets vs A26 §6.16 / A62 §10.9 | `MISSING` |
| JSON-dump implementation | `EXISTS_NEEDS_REFACTOR` |
| Loading / error UX | `MISSING` |
| `useSettings` → live `MapGet` string | `EXISTS_AND_GOOD` (demo path only) |
| Unversioned `/api/settings` as the contract | `DEPRECATED` |
| `GET /api/v1/settings` / `/settings/public` | `MISSING` |
| Live flag body (`REAL_COPY…=false`) | `EXISTS_NEEDS_REFACTOR` (correct value, wrong source) |
| Password field on the page | correctly **absent** — `EXISTS_AND_GOOD` |
| React mutation / A62 `PUT` | correctly **absent** for §69 — `EXISTS_AND_GOOD` |
| `SettingsController` mapping | `MISSING` (file `EXISTS_NEEDS_REFACTOR`) |
| Controller `PUT` if wired | `UNSAFE` |
| `types/index.ts` `Settings` | `DEPRECATED` |
| Tests | `MISSING` |
| Auth on this GET | `MISSING` |

---

## 12. Honesty — not claimed

- Not claimed: first-useful dashboard (§69 still 0/12 — D41).
- Not claimed: A26 Settings page is implemented.
- Not claimed: `/api/v1` exists.
- Not claimed: workers read these flags (A49 / D69: they do not).
- Not claimed: Redis is a settings store (D55: no multiplexer).
- Not claimed: the caption is enforced by a sanitizer.
- Not claimed: `brokerConfigs.enabled` is a live Manager session (C42).
- API process was **not** started this pass.

**Counts to quote:** page **12** lines / **459** B / SHA `57D41B90…`; hooks used **1**; mutations **0**; catalog path hits **0**; live host maps for this URL **1** (`Program.cs`); unmapped controller files **1**; password fields **0**; tests **0**.

**Do not:** recreate the page; `MapControllers` `SettingsController`; add a whole-object PUT; put `EmergencyFlattenApiKey` or FIX password on this screen; treat `LiveCopyEnabled` as `REAL_COPY_EXECUTION_ENABLED`.

*End of D80. Product source was not modified.*
