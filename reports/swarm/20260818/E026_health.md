# E026 — `GET /api/health` mapping and demo vs live wording

| Field | Value |
|---|---|
| Agent | E026 (senior engineer, `/api/health` mapping + wording only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:19:57Z (2026-08-18T13:49:57+05:30) |
| Artifact | `D:\Prop\reports\swarm\20260818\E026_health.md` |
| Assigned | Read `/api/health` mapping. Demo vs live wording. Write this file. **Do not modify product source.** |
| Workspace | API host is `D:\Prop\apps\api`. Vite app is `D:\Prop\apps\web`. Domain/infra under `D:\Prop\src`. |
| Product source modified | **No.** This report is the only required write. |
| Test source modified | **No.** |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Binding law | Architecture v2 §46 nav + §58 metrics; A26 §6.14 `GET /api/v1/health`; A63 §5.1; A77 probe vs dashboard split |
| Prior (do not collapse) | A77 (probes), A26 §6.14, A63 §5.1, B06/C04 (stale QUOTE `healthy: true`), D06/D30 (current strings), D41/D42 (honesty ≠ health), D45 (`outboxBacklog = 0` lie), E003 (route matrix) |
| Method | Full read of `apps/api/Program.cs` (all 15 maps), `hooks.ts` `useHealth`, `SystemHealthPage.tsx`, `types/index.ts` `HealthStatus`, `DashboardModels.cs`, `EfDashboardQueries.GetOverviewAsync` / `GetFixSessionsAsync` / `GetBrokersAsync`, `DemoSeeder` FIX rows, `apps/fix-worker/Worker.cs`, `FakeMt5BrokerConnector`, `DependencyInjection`, `TraderIntelligence.Api.http`, A26 §6.14, A63 §5.1, A77 §2. PowerShell SHA-256 + bytes + last-write. `git rev-parse` + `git status --short`. **API process was not launched.** Body is from source literals, not HTTP capture. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **source map of one anonymous handler** and the **demo vs live words it prints**. It is **not** a live Achiever Manager proof, a live cTrader TLS Logon proof, or A26/A63 catalog compliance.

---

## 0. Verdict

**`GET /api/health` is a hardcoded anonymous inventory. It is not a probe, not a DB read, not a socket check, and not A26/A63 `GET /api/v1/health`.** The *strings* now admit demo / no-live. The *booleans* still paint Achiever and the database green. Redis is the only component whose bool and caption agree that the thing is unused. `outboxBacklog` is the literal integer `0`.

| Question | Measured answer |
|---|---|
| Where is it mapped? | `D:\Prop\apps\api\Program.cs` lines 26–33. Single `MapGet`. No service, no `IDashboardQueries`, no `HealthCheck`. |
| Auth / RBAC | **None.** Anonymous. Same as `/health` and `/ready`. |
| Versioned catalog path | **MISSING.** Zero `/api/v1/health` or `/api/v1/system/health`. |
| Probe? | **No.** A77 liveness is `GET /health`. A77 readiness is `GET /ready`. A77 retired `/api/health` as “dashboard client debt.” |
| Live inventory? | **No.** Every field is a C# object initializer. `lastCheck` is `DateTimeOffset.UtcNow` at serialize time. |
| React consumer | `useHealth()` → `GET /api/health` every 10 s → `SystemHealthPage` `JSON.stringify`. |
| Demo wording present? | **Yes.** `"demo FakeMt5BrokerConnector — not live Manager"` and `"not required for demo"`. |
| Live wording present? | **Denial only.** `"not live Manager"` and `"no live TLS socket"`. No claim of a live Manager or live TLS session. |
| Can an operator misread it as live? | **Yes** on Achiever (`healthy: true`) and database (`healthy: true`) if they skip `details`. |
| C04 / C14 “QUOTE `healthy: true`”? | **Stale.** Current QUOTE is `healthy: false`. |

§73.B for this **demo BFF handler**: **EXISTS_NEEDS_REFACTOR**.  
§73.B for A26 §6.14 / A63 §5.1 authenticated inventory: **MISSING**.  
§73.B as **live-venue evidence**: **UNSAFE**.  
§73.B for `outboxBacklog: 0`: **UNSAFE** as an ops metric (literal after `EfTradingStore` writes `OutboxEvents`).

Do **not** treat `healthy: true` + a demo footnote as Achiever Manager. Do **not** treat `healthy: false` + `"no live TLS socket"` as a measured TCP drop. Do **not** treat `outboxBacklog: 0` as an empty outbox.

---

## 1. Measured files

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) |
|---|---:|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 |
| `D:\Prop\apps\web\src\pages\SystemHealthPage.tsx` | 369 | 11 | `03BDBC76CBEFEE4ADC125B491E9B46BC62E35E052AD99BE874E8BCF0E25EDD2A` | 2026-08-18T13:16:43+05:30 |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 205 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 2026-08-18T13:35:15+05:30 |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 5082 | 140 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 2026-08-18T13:34:59+05:30 |
| `D:\Prop\apps\fix-worker\Worker.cs` | 2093 | 51 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | 2026-08-18T13:34:48+05:30 |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | 170 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 2026-08-18T13:13:42+05:30 |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | 2026-08-18T13:20:38+05:30 |

Git vs HEAD `398a142`: **unstaged** `M` on `apps/api/Program.cs` and `apps/web/src/api/hooks.ts`. `SystemHealthPage.tsx` is **untracked** (`??`). Quote the **worktree**. File SHAs of `Program.cs` / `hooks.ts` match E003 / D30.

`TraderIntelligence.Api.http` samples `GET /health` and does **not** sample `GET /api/health`.

---

## 2. The mapping (authoritative: `Program.cs`)

Three health-adjacent maps sit at the top of the host. Only the middle one is `/api/health`.

```25:52:D:\Prop\apps\api\Program.cs
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow, details = "demo FakeMt5BrokerConnector — not live Manager" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "no live TLS socket" } },
    database = new { name = "postgres-or-inmemory", healthy = true, lastCheck = DateTimeOffset.UtcNow },
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
    outboxBacklog = 0
}));
// ... other maps ...
app.MapGet("/ready", async (TraderDbContext db, CancellationToken ct) =>
{
    var brokers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Brokers, ct);
    return Results.Ok(new { ready = true, brokers });
});
```

| Map | Lines | Closure I/O | Body | Role vs A77 / A26 |
|---|---|---|---|---|
| `GET /health` | 25 | none | `{ status: "ok", utc }` | A77 liveness, extra `utc` vs A77 `{status:"ok"}` only |
| `GET /api/health` | 26–33 | **none** | hardcoded inventory below | **Not** a probe. **Not** A26 `GET /api/v1/health`. React System Health polls this. |
| `GET /ready` | 48–52 | `CountAsync(db.Brokers)` | `{ ready: true, brokers }` | A77 readiness **shape-adjacent**. `ready` is **always** `true`. In-memory DB still 200s. |

Handler facts for `/api/health`:

- Minimal-API lambda. No `IDashboardQueries`. No `TraderDbContext`. No `IMt5BrokerConnector`. No Redis. No FIX session table.
- No `MapGroup`, no `/api/v1` prefix, no `RequireAuthorization`.
- `AddHealthChecks` is **absent** from this host.
- `lastCheck` is the request clock, not a last successful Manager ping or TLS Heartbeat.
- `SettingsController` (`[Route("api/settings")]`) is **unmapped** (`AddControllers` / `MapControllers` absent). It does not own health.
- `mt5-worker` and `fix-worker` expose **no** HTTP `/health` / `/api/health`.

Wire JSON (field names as serialized; timestamps vary):

```json
{
  "mt5Connections": [
    {
      "name": "ACHIEVER",
      "healthy": true,
      "lastCheck": "<request UtcNow>",
      "details": "demo FakeMt5BrokerConnector — not live Manager"
    }
  ],
  "fixSessions": [
    {
      "name": "QUOTE",
      "healthy": false,
      "lastCheck": "<request UtcNow>",
      "details": "no live TLS socket"
    }
  ],
  "database": {
    "name": "postgres-or-inmemory",
    "healthy": true,
    "lastCheck": "<request UtcNow>"
  },
  "redis": {
    "name": "redis",
    "healthy": false,
    "lastCheck": "<request UtcNow>",
    "details": "not required for demo"
  },
  "outboxBacklog": 0
}
```

Absent keys (correct to omit unless a real collector exists): `ml`, `reconstruction`, `scoring`, `trade` / `TRADE`, `STARWAVEFX` / `STARWAVE`, `generatedAt`, A26 `data` envelope, §58 Prometheus names.

---

## 3. Demo vs live wording (this handler)

Every operator-visible word on `/api/health` is a compile-time string except `lastCheck` and the bools.

### 3.1 Token inventory

| Token | Where | Class | What it admits | What it does **not** prove |
|---|---|---|---|---|
| `demo` | Achiever `details` | **demo-admit** | Connector is the Fake | Not a live Manager API session |
| `FakeMt5BrokerConnector` | Achiever `details` | **demo-admit** (type name) | Matches `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | `IsConnectedAsync` is still an in-process bool |
| `not live Manager` | Achiever `details` | **live-deny** | No Manager API claim | Not a failed live connect (no connect was attempted) |
| `no live TLS socket` | QUOTE `details` | **live-deny** | No TLS session | Not a measured TCP/TLS failure |
| `not required for demo` | Redis `details` | **demo-excuse** | Redis is out of the demo story | Compose `redis:` may still exist unused (C27 theater) |
| `postgres-or-inmemory` | Database `name` | **ambiguous store** | Could be either | Does not say which `AddTraderIntelligence` chose |
| `ACHIEVER` | MT5 `name` | identity | One broker code | Second broker `STARWAVEFX` is **omitted** |
| `QUOTE` | FIX `name` | identity | One qualifier | `TRADE` is **omitted** |
| `healthy: true` (ACHIEVER) | MT5 bool | **demo-green** | Fake is treated as “up” | Live Manager |
| `healthy: true` (database) | DB bool | **demo-green** | No ping | Postgres liveness |
| `healthy: false` (QUOTE) | FIX bool | **live-red / honest-ish** | Matches “no socket” caption | Live disconnect event |
| `healthy: false` (redis) | Redis bool | **demo-honest** | Unused | A real Redis `PING` |
| `outboxBacklog: 0` | integer | **demo-zero** | Looks idle | Empty `outbox_events` |
| `lastCheck: UtcNow` | four objects | **clock theater** | Looks freshly probed | Any dependency I/O |

Em-dash in Achiever details is U+2014 (`—`), not ASCII `-`.

### 3.2 Pairing: bool vs caption

| Component | `healthy` | Caption / name | Pairing | Operator risk |
|---|---|---|---|---|
| ACHIEVER | `true` | `demo FakeMt5BrokerConnector — not live Manager` | **Split.** Green bool + live-deny text. | High if the UI ever badges on `healthy` alone. Today the page dumps JSON, so both are visible. |
| QUOTE | `false` | `no live TLS socket` | **Aligned** (both say down). | Low for “is it live?”. Residual: looks like a live check. |
| database | `true` | `postgres-or-inmemory` (no `details`) | **Underspecified green.** | Medium. In-memory after `<SECRET>` / empty connection still reads healthy. |
| redis | `false` | `not required for demo` | **Aligned** for demo. | Low. Do not invert this to “Redis is down, page red, production incident.” |
| outbox | n/a | literal `0` | **No caption.** | High as an ops number once seed/scoring writes rows. |

Honesty of the *strings* improved vs C04 / C14 / C46 (those files still describe QUOTE `healthy: true` and Achiever `"demo connector"`). Honesty of the *booleans* did **not** become a live probe.

### 3.3 Sibling wording (not this handler — do not merge)

| Surface | SHA (this measure) | Wording | Same as `/api/health`? |
|---|---|---|---|
| `DemoSeeder` QUOTE `LastError` | `A6416491…` | `"No live QUOTE socket. Demo seed only."` | **No.** Adds “Demo seed only.” Health says “no live TLS socket.” |
| `DemoSeeder` TRADE `LastError` | same | `"No live TRADE socket. NewOrderSingle off."` | **N/A** — TRADE is missing from `/api/health`. |
| `fix-worker` QUOTE `LastError` | `92A8F492…` | `"No live QUOTE socket. Simulator/demo only."` | **No.** “Simulator/demo only” vs “no live TLS socket.” |
| `fix-worker` TRADE `LastError` | same | `"No live TRADE socket. NewOrderSingle remains off."` | **N/A** — TRADE missing. |
| Overview page chrome | — | `"Live FIX send is off."` / `"Trade #3 never auto-promotes to LIVE."` | Different page. Not `/api/health`. |
| `mt5-worker` log | — | `"Execution copy is not performed here."` | Not exposed on `/api/health`. |

Four independently edited captions all deny a live QUOTE socket. None of them is a TLS stack. `/api/health` does **not** read `FixSessionState.LastError`; it cannot drift with the worker string.

---

## 4. What `/api/health` does not map

| Expected by spec / product | On `/api/health` |
|---|---|
| A26 §6.14 `GET /api/v1/health` `{ data: { mt5, reconstruction, scoring, fix } }` | **MISSING** |
| A63 §5.1 per-broker `connectionStatus`, pool, backfill lag | **MISSING** |
| A77 `GET /health` / `GET /ready` semantics | **Different maps.** Do not poll `/api/health` as a probe. |
| Architecture §58 `mt5_connected`, `fix_quote_connected`, … | **No** Prometheus names. Hardcoded JSON is not a gauge. |
| Second source broker `STARWAVEFX` (seeded + `DemoBrokerFactory` + worker sync) | **Omitted.** Inventory is Achiever-only. |
| FIX `TRADE` session | **Omitted.** |
| `ml` object | **Absent — correct** (C44 / B39: ML not in use). |
| Auth / role (A26 ReadOnly+) | **Absent.** |
| `IDashboardQueries` / EF | **Not called.** |
| `FakeMt5BrokerConnector.IsConnectedAsync` | **Not called.** Fake `ConnectAsync` only flips an in-memory flag. |
| Postgres `SELECT 1` / Redis `PING` / outbox `COUNT` | **Not executed.** |
| Secrets (`Password`, connection strings) | **Not returned.** (B25 / D40 still hold for this body.) |

`AddTraderIntelligence` always registers **two** `FakeMt5BrokerConnector` singletons (`DemoBrokerFactory.CreateDefault()`). The health array lists **one** name. That is an inventory hole, not a live-down for Starwave.

---

## 5. React + DTO mapping

```47:49:D:\Prop\apps\web\src\api\hooks.ts
export function useHealth() {
  return useQuery({ queryKey: ['health'], queryFn: () => client.get('/api/health').then(r => r.data), refetchInterval: 10000 });
}
```

```1:10:D:\Prop\apps\web\src\pages\SystemHealthPage.tsx
import { useHealth } from '../api/hooks';

export default function SystemHealthPage() {
  const { data } = useHealth();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-4">System health</h1>
      <pre className="bg-gray-950 border border-gray-800 rounded p-4 text-sm text-gray-200">{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
}
```

| Client piece | Mapping |
|---|---|
| Route | `App.tsx` `path="health"` → `SystemHealthPage` (sidebar “Health”) |
| Hook | `useHealth` / query key `['health']` / **10 s** poll |
| Axios | `baseURL` `import.meta.env.VITE_API_URL \|\| 'http://localhost:5000'` |
| Render | Raw JSON. **No** `StatusBadge`. **No** component grid. **No** §58 tiles. |
| TS `HealthStatus` | `mt5Connections` / `fixSessions` / `database` / `redis` / `outboxBacklog` + optional `details` — **shape match** to the anonymous object. Not declared in `DashboardModels.cs`. |
| A26/A63 target | `GET /api/v1/health` (authenticated envelope). Hook path is **wrong** vs catalog (B30 / D39). |

Because the page dumps JSON, the demo-admit / live-deny strings **are** visible. There is no green badge that hides `details`. Overview **does** hide the footnote:

| Overview card (`OverviewPage.tsx`) | Source | Caption |
|---|---|---|
| “MT5 health” `OK` / `DOWN` | `GET /api/overview` → `Mt5Healthy` = `brokers > 0` | **No** “FakeMt5 / not live Manager” text |
| “QUOTE / TRADE” `Q/-` / `T/-` | same DTO → EF session enum | **No** “no live TLS” text |

Two pages, two sources, two stories. System Health is the only place the demo caption is printed.

---

## 6. Split-brain vs other live maps

`/api/health` is **not** the overview health strip and **not** the FIX page.

| Fact | `/api/health` | `/api/overview` (`EfDashboardQueries` L39–42) | `/api/fix/sessions` | `/api/brokers` |
|---|---|---|---|---|
| Source | literals | EF counts + `FixSessionStates` | EF `FixSessionStates` | EF brokers + **`Connected: true` hardcoded** |
| Achiever | `healthy: true` + demo caption | folded into `Mt5Healthy = brokers > 0` | n/a | `Connected = true` |
| Starwave | **absent** | same `brokers > 0` | n/a | `Connected = true` |
| QUOTE | `healthy: false` | `QuoteHealthy` iff `LoggedOn` / `ReadyForMarketData` / `ReadyForExecution` | `Connected`/`LoggedOn` from enum; seed+worker = `Disconnected` | n/a |
| TRADE | **absent** | `TradeHealthy` iff `LoggedOn` / `Reconciling` / `ReadyForExecution` | same | n/a |
| After current seed + fix-worker | QUOTE false (literal) | QUOTE/TRADE **false** (enum) | both `Disconnected`, worker/seeder `LastError` | both brokers “connected” |

After seed, Overview “MT5 health” = **OK** with **no** demo footnote, while System Health shows Achiever `healthy: true` **with** the FakeMt5 caption. Brokers page independently hard-codes `Connected = true` for both names. That is three MT5 “up” signals, one of which mentions demo.

QUOTE `healthy: false` on `/api/health` **agrees** with Overview `quoteHealthy` after seed, but **not because they share a query**. Change the seeder to `LoggedOn` and Overview would flip; `/api/health` would stay false until someone edits the literal. The reverse C14 lie (health green, no socket) is gone for QUOTE only.

`/ready` counts brokers and still returns `ready: true`. That is A77-shaped I/O with a hardcoded success bit. It is not `/api/health`.

---

## 7. Spec delta (do not implement in this task)

| Spec | Required | Worktree |
|---|---|---|
| A77 | `GET /health` = liveness, no inventory; `GET /ready` = Postgres 200/503; React must **not** poll a probe for System Health | `/health` exists (extra `utc`). `/ready` always `true`. React polls `/api/health` (allowed as dashboard debt, **not** as a probe). |
| A26 §6.14 | Authenticated `GET /api/v1/health` with mt5/recon/scoring/fix counters | **MISSING.** Unversioned hardcoded object instead. |
| A26 | `GET /api/v1/health/live` `{status:"ok"}` | **Do not add** per A77 (retired). Use `/health`. |
| A63 §5.1 | `/api/v1/health` envelope + per-broker `DISCONNECTED` + `fix.quote.loggedOn: false` | **MISSING.** |
| A63 anonymous | only `GET /health` + login | `/api/health` is a third anonymous surface. |
| §58 | named counters | **MISSING** on this host (C26). |

Replacement law already on disk (C47 / D30 / A77): keep `/health` + `/ready` as probes; replace `/api/health` with authenticated `/api/v1/health` (or `/api/v1/system/health`) that **reads** collectors; do not print `"demo connector"` when a live profile is claimed; do not keep `outboxBacklog = 0` after a writer exists (D45).

This agent **did not** change product source.

---

## 8. Stale reports (use this file for the `/api/health` body)

| Report | Claim about `/api/health` | Current bytes |
|---|---|---|
| B06, B10, C04, C14, C46 | Achiever + QUOTE both `healthy: true`; details `"demo connector"` | **Stale.** QUOTE is `false` / `"no live TLS socket"`. Achiever details name `FakeMt5BrokerConnector`. |
| D07 | `/api/health` still hardcodes `healthy: true` and ignores the table (implies FIX too) | **Half-stale.** Handler still ignores the table. FIX bool is now `false`. |
| D06, D30, D41, D42 | Achiever demo-green + QUOTE live-red + redis false + `outboxBacklog: 0` | **Still current** at SHA `61B1E0D1…`. |
| A77 “today no `/health`” | template-era | **Stale** for path existence. Binding law (probe vs inventory) **still binds**. |
| A63 “host is weatherforecast” | template-era | **Stale** for path existence. Catalog paths still **MISSING**. |

---

## 9. Honesty close

Measured: one anonymous `MapGet("/api/health")` returns a five-field object built in the lambda. Demo words: `demo`, `FakeMt5BrokerConnector`, `not required for demo`. Live words: only denials (`not live Manager`, `no live TLS socket`). Achiever and database stay `healthy: true`. QUOTE and redis stay `healthy: false`. `outboxBacklog` is `0`. No Manager, no TLS, no `PING`, no outbox count.

**Live Achiever / StarwaveFX Manager: NOT PROVEN.**  
**Live cTrader FIX Logon (`35=A`): NOT PROVEN.**  
**A26/A63 System Health contract: MISSING.**

Product source was not modified.
