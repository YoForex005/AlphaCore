# E038 — Settings flag API: `featureFlags.REAL_COPY_EXECUTION_ENABLED=false`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E038_flag_api.md` |
| Agent | E038 (settings feature-flag API) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:56+05:30 / 2026-08-18T08:22:14Z |
| Host | live API `http://127.0.0.1:5000` (HTTP 200 on `/health`) |
| Assigned | Settings `featureFlags` `REAL_COPY` **false**. Write this file. **Do not modify product source.** |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Binding law | Architecture §41 / §56 (`REAL_COPY_EXECUTION_ENABLED=false`); §55 / §72.5 (no secrets on the browser); A26 §6.16 Settings; A49 §3.4 config floor; A63 §4.2 `/settings/public` + §7.2 park writes |
| Siblings (do not collapse) | D69 (POCO default), D80 (Settings page chrome), E002 (no `35=D` sender), E016 (live copy OFF / demo shadow), A49 (worker enforcement design), A101 item 12 / §15.3 binder |
| Method | Full read of live `MapGet("/api/settings")`, unmapped `SettingsController`, `appsettings.json` `FeatureFlags`, `CTraderFixOptions`, fix-worker `GetValue`, `EfDashboardQueries` literals, Settings/Live pages + `useSettings`. SHA-256 via `Get-FileHash`. Live HTTP: `GET /api/settings`, `/api/overview`, `/api/risk`, `/api/fix/sessions`, catalog `/api/v1/settings*`, write verbs. **No product edit. No `35=D` attempted. No flag flipped.** |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |

**Honesty rule:** a hardcoded JSON `false` on `GET /api/settings` is a **display floor**, not a send gate. `LiveCopyEnabled` is **not** `REAL_COPY_EXECUTION_ENABLED`. `405` on PUT is **absence of a write route**, not an audited refuse of `ENABLE_REAL_EXECUTION`. Absence of `NewOrderSingle` is **SAFE_BY_ABSENCE** (E002). Do not tick A101 “Dashboard `RealCopyEnabled` reads the binder.”

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict (binding)

**CONFIRMED: the only live settings flag API returns `featureFlags.REAL_COPY_EXECUTION_ENABLED = false`.**

That value is a **C# literal** in `Program.cs` L45. It is **not** read from env `REAL_COPY_EXECUTION_ENABLED`, **not** `CTraderFixOptions.RealCopyExecutionEnabled`, **not** `FeatureFlags:LiveCopyEnabled`, **not** Redis `settings:flags:live_copy`.

| Assigned claim | Measured | Class |
|---|---|---|
| Settings `featureFlags.REAL_COPY_*` is **false** | **Yes.** Live `GET /api/settings` 200, key present, value `false` | display floor `EXISTS_AND_GOOD` vs §41 default |
| That GET is a real flag **read** (config / POCO / binder) | **No** | `EXISTS_NEEDS_REFACTOR` (correct value, **wrong source**) |
| Catalog `GET /api/v1/settings` / `/settings/public` exists | **No** — both **404** this pass | `MISSING` |
| Write / flip (`PATCH …/execution`, `PUT /api/settings`) | **405** on unversioned URL (`Allow=GET`); **404** on v1 | write correctly **out of §69**; not an audited 409/412 |
| Dashboard can enable live copy | **No** | React has **0** mutations; controller PUT is **unmapped** |
| Flipping any settings key would send `35=D` | **No** | no sender (E002); worker key is a different name |

One-line:

```text
GET /api/settings → featureFlags.REAL_COPY_EXECUTION_ENABLED=false
  (literal; not bound)
AND PUT/PATCH/DELETE → 405 Allow=GET
AND GET /api/v1/settings* → 404
AND LiveCopyEnabled ≠ REAL_COPY_EXECUTION_ENABLED
```

Do **not** treat this file as A101 item 12 PASS. Do **not** `MapControllers` `SettingsController` to “finish” the flag API.

---

## 1. Live HTTP (this pass)

`GET http://127.0.0.1:5000/health` → `200` `{"status":"ok","utc":"2026-08-18T08:21:56.3197039+00:00"}`.

### 1.1 Authoritative settings body

`GET http://127.0.0.1:5000/api/settings` → **200** (captured 2026-08-18T08:22:14Z):

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

| Wire field | Value | Source on this host |
|---|---|---|
| `featureFlags.REAL_COPY_EXECUTION_ENABLED` | **`false`** | `new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false }` — **literal** |
| `riskLimits.maxQuoteAgeSeconds` | `3` | literal (matches `RiskLimits.MaxQuoteAge` default, **not** read from it) |
| `riskLimits.maxSignalAgeSeconds` | `15` | literal (matches `MaxSourceSignalAge`, **not** read from it) |
| `brokerConfigs[].enabled` | `true` | demo labels; **not** a live Manager session (C42) |

Dictionary key is **preserved** as `REAL_COPY_EXECUTION_ENABLED` (screaming snake). It is **not** camelCased to `realCopyExecutionEnabled`. That is why the live body does **not** match A63 §4.2 (`realCopyExecutionEnabled`) even though the boolean is the same.

No password, API key, connection string, or `EmergencyFlattenApiKey` on this body (D40). That is **omission**, not a sanitizer.

### 1.2 Verb / catalog matrix (same host, this pass)

| Method | URL | HTTP | `Allow` | Body / note |
|---|---|---:|---|---|
| `GET` | `/api/settings` | **200** | — | §1.1 |
| `OPTIONS` | `/api/settings` | **405** | `GET` | CORS `*` still answers preflight from the browser via the default policy; raw OPTIONS on this map is 405 |
| `PUT` | `/api/settings` | **405** | `GET` | body with `LiveCopyEnabled:true` **ignored** (no handler) |
| `PATCH` | `/api/settings` | **405** | `GET` | same |
| `POST` | `/api/settings` | **405** | `GET` | same |
| `DELETE` | `/api/settings` | **405** | `GET` | same |
| `GET` | `/api/v1/settings` | **404** | — | A26 §6.16 **MISSING** |
| `GET` | `/api/v1/settings/public` | **404** | — | A63 §4.2 **MISSING** |
| `PATCH` | `/api/v1/settings/execution` | **404** | — | A26 SuperAdmin enable **MISSING** (correctly out of §69) |
| `GET` | `/swagger/index.html` | **404** | — | `UseSwagger()` without `UseSwaggerUI` (D30) |

Host maps: **14** `MapGet` + **1** `MapPost` (`/api/ops/resync`). **0** `MapPut` / `MapPatch` / `MapDelete` / `MapControllers` / `MapGroup("/api/v1")`.

### 1.3 Sibling display floors (same process, not the settings route)

| Endpoint | HTTP | Flag field | Value | Source |
|---|---:|---|---|---|
| `GET /api/overview` | 200 | `realCopyEnabled` | **`false`** | `EfDashboardQueries` last ctor arg **literal** `false` (L42) |
| `GET /api/risk` | 200 | `realCopyEnabled` | **`false`** | `RiskDashboardDto` 7th arg **literal** `false` (L196) |
| `GET /api/fix/sessions` | 200 | `executionEnabled` | **`false`** (QUOTE + TRADE) | `FixSessionDto` last arg **literal** `false` (L183) |

Overview this pass: `shadow=2`, `live=0`, `liveCandidates=0`, `destinationRealPnl=0`, `quoteHealthy=false`, `tradeHealthy=false`, **`realCopyEnabled=false`**. TRADE `status=Disconnected`, lastError `No live TRADE socket. NewOrderSingle off.`

Three JSON names, one concept, **zero binders**:

| JSON name | Where | Bound to options? |
|---|---|---|
| `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `/api/settings` | **No** |
| `realCopyEnabled` | `/api/overview`, `/api/risk` | **No** |
| `executionEnabled` | `/api/fix/sessions` | **No** |
| `featureFlags.liveCopyEnabled` | dead controller GET (unmapped) | **No** — different identifier |

---

## 2. Two backends, one URL — only one is live

### 2.1 Live: `apps/api/Program.cs` L42–47

SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B / 95 phys. / 86 non-blank). Unstaged `M` vs HEAD. Last write `2026-08-18T13:35:15`.

```42:47:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    brokerConfigs = new[] { new { id = "ACHIEVER", name = "Achiever", enabled = true }, new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true } }
}));
```

No `IConfiguration`. No `IOptions<CTraderFixOptions>`. No Redis. Handler is a closure over compile-time constants.

`AddTraderIntelligence` (`DependencyInjection.cs` SHA `EF0E0E46…`) does **not** `Configure<CTraderFixOptions>` and does **not** register `IConnectionMultiplexer`.

### 2.2 Dead: `Controllers/SettingsController.cs`

SHA-256 `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` (3732 B / 94 phys. / 83 non-blank). **`??` untracked.** Last write `2026-08-18T13:37:39`.

`[ApiController] [Route("api/settings")]` + `HttpGet` + `HttpPut`.

**Why it cannot serve today**

1. `Program.cs` never calls `AddControllers()` / `MapControllers()`.
2. Constructor requires `IConnectionMultiplexer` — **not registered**.
3. If both were added, two `GET /api/settings` would collide. Do not find out in production.

**Would-be GET** (Pascal → camel on the wire):

```json
{
  "riskEngine": {
    "maxDailyDrawdownPct": 5.0,
    "maxPositionSize": 10.0,
    "maxOpenPositions": 20,
    "killSwitchEnabled": true
  },
  "featureFlags": {
    "shadowTradingEnabled": true,
    "liveCopyEnabled": false,
    "autoPromotionEnabled": false
  }
}
```

| Config key | Controller default | `appsettings.json` now |
|---|---|---|
| `FeatureFlags:ShadowTradingEnabled` | `true` | `true` |
| **`FeatureFlags:LiveCopyEnabled`** | **`false`** | **`false`** |
| `FeatureFlags:AutoPromotionEnabled` | `false` | `false` |

`LiveCopyEnabled` is **not** the architecture name. Wiring this GET would **drop** `REAL_COPY_EXECUTION_ENABLED` from the live body and **break** the only TS `Settings` shape that matches the stub (D76 / D80).

**Would-be PUT** writes Redis strings, returns `{ updated: true }`:

| Body field | Redis key | Read back on next GET? |
|---|---|---|
| `FeatureFlags.LiveCopyEnabled` | `settings:flags:live_copy` | **No** — GET reads `IConfiguration`, not Redis |
| `FeatureFlags.ShadowTradingEnabled` | `settings:flags:shadow_trading` | **No** |
| `FeatureFlags.AutoPromotionEnabled` | `settings:flags:auto_promotion` | **No** |

No `[Authorize]`, no `confirmPhrase: ENABLE_REAL_EXECUTION`, no audit row, no config-floor check (A49 §3.4). CORS on the host is `AllowAnyOrigin` + `AllowAnyMethod`. **If mapped as-is: `UNSAFE`.** A99 / D55: `settings:*` is not an allow-listed family; Redis is coordination/cache, not SoT.

`KillSwitchEnabled` is on GET and **absent** from PUT.

### 2.3 `appsettings.json` is not the live GET

SHA-256 `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` (1254 B). Unstaged `M`.

```44:48:D:\Prop\apps\api\appsettings.json
  "FeatureFlags": {
    "ShadowTradingEnabled": true,
    "LiveCopyEnabled": false,
    "AutoPromotionEnabled": false
  },
```

`CTraderFix` block has hosts/ports only — **no** `RealCopyExecutionEnabled`. B13 / A101 claims that API `appsettings` contains `CTrader:RealCopyExecutionEnabled` are **stale** (D69). `appsettings.Development.json` (SHA `81B5E6DC…`) has **no** `FeatureFlags` / `CTrader*` keys. `launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development` only.

Live `MapGet` **ignores all of it**.

---

## 3. Flag name census (do not conflate)

| Identifier | Kind | Default | Consumed by send path? |
|---|---|---|---|
| Architecture `REAL_COPY_EXECUTION_ENABLED` | env / §41 law | **`false`** | **No binder** for this flat name |
| `CTraderFixOptions.RealCopyExecutionEnabled` | C# initializer L35 | **`false`** | POCO unused (`IOptions<>` absent) |
| Worker `CTrader:RealCopyExecutionEnabled` | `GetValue(..., false)` | fallback **`false`** | log + unused `if (real)` warning only |
| Settings stub `featureFlags["REAL_COPY_EXECUTION_ENABLED"]` | compile-time dict | **`false`** | display |
| `FeatureFlags:LiveCopyEnabled` | appsettings + dead controller | **`false`** | **different name** |
| Redis `settings:flags:live_copy` | dead PUT | n/a | **nobody reads** |
| `OverviewDto.RealCopyEnabled` / `RiskDashboardDto.RealCopyEnabled` | record field | literal **`false`** | display |
| `FixSessionDto.ExecutionEnabled` | record field | literal **`false`** | display |
| `RiskEvaluationRequest.RealExecutionEnabled` | caller bit | fixture **`false`** | empty `if` when false; `AllowFixSend` ANDs it — **not** this API |
| Local `D:\Prop\.env` L73 | gitignored | `REAL_COPY_EXECUTION_ENABLED=false` | **not** the worker key; **not** the settings GET |
| `.env.example` | **missing from worktree** | — | HEAD blob had the same `false` (D69) |
| `docker-compose.yml` | api env | key **absent** (`ASPNETCORE_ENVIRONMENT` only) | N/A |

ASP.NET Core will **not** map env `REAL_COPY_EXECUTION_ENABLED` onto `CTrader:RealCopyExecutionEnabled`. Setting the architecture name to `true` would **not** flip the worker `GetValue` unless `CTrader__RealCopyExecutionEnabled` is also set, and would **not** change `GET /api/settings` at all (literal).

Effective conjunction required by A49 §3.4 is **not implemented**:

```text
effective_real_copy =
      config.REAL_COPY_EXECUTION_ENABLED     -- not bound
  AND settings_store.real_copy               -- no store
  AND NOT STOP_NEW_EXECUTION                 -- kill switch exists as entity; not on this GET
```

---

## 4. React consumer

| File | SHA-256 | Bytes | Role |
|---|---|---:|---|
| `apps/web/src/pages/SettingsPage.tsx` | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` | 459 | `<pre>` dump of `useSettings()` — **`??` untracked** |
| `apps/web/src/api/hooks.ts` | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 1935 | `GET /api/settings` only — unstaged `M` |
| `apps/web/src/types/index.ts` | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2905 | `Settings.featureFlags: Record<string, boolean>` — **zero imports** |
| `apps/web/src/pages/LiveCopyPage.tsx` | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | 321 | static sentence; **does not** call `useSettings` |
| `apps/web/src/api/client.ts` | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 232 | `baseURL` `VITE_API_URL \|\| http://localhost:5000` |

```51:53:D:\Prop\apps\web\src\api\hooks.ts
export function useSettings() {
  return useQuery({ queryKey: ['settings'], queryFn: () => client.get('/api/settings').then(r => r.data) });
}
```

Mutations: **0** (`useMutation` / `put` / `patch` absent). A62-era `useUpdateSettings` is **gone**. Absence is **correct** for §69 (A63 §7.2 parks `PUT /api/v1/settings` and `PATCH …/execution`).

`LiveCopyPage` hardcodes “REAL_COPY_EXECUTION_ENABLED is false” and does **not** read the API. Header strip (A26 §5.3: `REAL_COPY` + `STOP_NEW` + FIX/MT5) is **MISSING** on every page (D80).

`.http` sample (`2AEC0F4A…`) has **no** `/api/settings` line.

---

## 5. Catalog vs disk

| Required surface | Spec | Live | Class |
|---|---|---|---|
| `GET /api/v1/settings` `{ execution, fixNonSecret, symbolMappings }` | A26 §6.16 | **404** | `MISSING` |
| `GET /api/v1/settings/public` `{ realCopyExecutionEnabled, fixQuoteEnabled, … }` | A63 §4.2 | **404** | `MISSING` |
| Unversioned `GET /api/settings` `{ featureFlags.REAL_COPY…=false }` | demo stand-in | **200** | `EXISTS_NEEDS_REFACTOR` / `DEPRECATED` as contract |
| `PATCH /api/v1/settings/execution` + `ENABLE_REAL_EXECUTION` | A26 SuperAdmin | **404** | **correctly out of §69** |
| Whole-object `PUT /api/settings` | A62 stub; A63 secret-leak vector | **405** | `EXISTS_AND_GOOD` as absence |
| Controller Redis PUT | not in any catalog | compiled, unmapped | do not ship |
| Config floor: PATCH cannot raise above env `false` | A49 §3.4 / A101 §15.3 | **not coded** | `MISSING` |
| RBAC ReadOnly+ on GET | A26 §10.1 | anonymous + CORS `*` | `MISSING` (D53) |
| Tests of this GET / refuse | A27 `RealExecutionFeatureFlagTests` | **0** hits under `tests/` | `MISSING` |

A63 first-useful Settings is a **read** of public flags typed `false`. This host has a read, on the **wrong path**, with the **wrong JSON name**, from a **literal**. That is a stand-in, not item-done.

---

## 6. Owning default (not this API — cited so the floor is not folklore)

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

SHA-256 `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` (2344 B). Unstaged `M` is `TargetCompId` `cServer` (D26), **not** this bool. Default **false** on HEAD and worktree (D69).

Fix-worker (SHA `92A8F492…`) reads a **different** key and never talks to `/api/settings`:

```21:22:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

`apps/fix-worker/appsettings.json` is logging-only (SHA `AB16B7B7…`). If `real` is true the loop **only logs a warning** and still stamps TRADE `Disconnected`. No socket. No `35=D` (E002).

`RiskEngine` (`AE0F9FAE…`) empty-`if`s `RealExecutionEnabled == false` and still can `Approve` with `AllowFixSend=false`. That bit is a **caller field**, not this HTTP flag.

---

## 7. Classification roll-up

| Slice | Class |
|---|---|
| Live `GET /api/settings` exists | `EXISTS_AND_GOOD` as a demo map |
| `featureFlags.REAL_COPY_EXECUTION_ENABLED=false` value vs §41 | `EXISTS_AND_GOOD` (default) |
| Value **source** (literal vs binder) | `EXISTS_NEEDS_REFACTOR` |
| Unversioned `/api/settings` as the contract | `DEPRECATED` |
| A26 / A63 versioned settings GET | `MISSING` |
| Write routes on this host | correctly **absent** for §69 — `EXISTS_AND_GOOD` as absence |
| `SettingsController` file | `EXISTS_NEEDS_REFACTOR` (compiled, unmapped) |
| Controller `PUT` if wired | `UNSAFE` |
| `LiveCopyEnabled` as an alias of `REAL_COPY` | **wrong name** — do not merge |
| React `useSettings` → live GET | `EXISTS_AND_GOOD` (demo path only) |
| React mutation / enable button | correctly **absent** |
| Auth on this GET | `MISSING` |
| Tests | `MISSING` |
| Send gate that **reads** this API | `MISSING` / live send **SAFE_BY_ABSENCE** |

---

## 8. Do-not / honesty

**Not claimed**

- First-useful dashboard (§69 still 0/12 — D41).
- A26 Settings page / `/api/v1` catalog.
- Workers read this GET (they do not).
- Redis is a settings store (no multiplexer).
- The caption “Secrets are never returned” is a sanitizer.
- `brokerConfigs.enabled` is a live Manager session.
- A101 “Dashboard `RealCopyEnabled` reads the binder.”
- Safe to set `REAL_COPY_EXECUTION_ENABLED=true`.

**Do not**

1. `MapControllers` this `SettingsController`.
2. Treat `LiveCopyEnabled` / Redis `settings:flags:live_copy` as `REAL_COPY_EXECUTION_ENABLED`.
3. Add a whole-object `PUT /api/settings` (A63 §7.2 secret-leak vector).
4. Put `EmergencyFlattenApiKey` or FIX password on this response.
5. Flip the literal to `true` “to see the UI.”
6. Treat `405 Allow=GET` as the A26 409/412 config-floor refuse.
7. Enable real copy from the dashboard while config is `false`.

**Counts to quote:** live `GET /api/settings` **200**; flag key **1** / value **`false`**; write verbs **4 × 405**; catalog settings paths **3 × 404**; mapped controllers **0**; React mutations **0**; tests **0**; product source edits this pass **0**.

**Measured files (this pass)**

| Bytes | SHA-256 | Path |
|---:|---|---|
| 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `apps/api/Program.cs` |
| 3732 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | `apps/api/Controllers/SettingsController.cs` |
| 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | `apps/api/appsettings.json` |
| 2344 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` |
| 2093 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | `apps/fix-worker/Worker.cs` |
| 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` |
| 459 | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` | `apps/web/src/pages/SettingsPage.tsx` |
| 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | `apps/web/src/api/hooks.ts` |

*End of E038. Product source was not modified.*
