# C38 — Architecture §46 Audit page

| Field | Value |
|---|---|
| Agent | C38 (senior engineer, §46 Audit page only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:27:42+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Question | Architecture **§46** lists **Audit**. Is the Audit page missing? |
| Product source modified | **No.** This report is the only write. |
| Method | Read architecture §46 + §45 `audit_logs` + §59 + §72.19; A26 §5.2/§5.3/§6.15/§9/§10.1; A51; A62; A63; A61 §8.42; A20; A57. Census `AuditPage.tsx`, `App.tsx`, `DashboardLayout.tsx`, `hooks.ts`, `types/index.ts`, `Program.cs`, `AuditLog.cs`, `TraderDbContext`, `IDashboardQueries`. SHA-256 + byte/line census. Grep `useAudit` / `ErrorState` / `/api/v1/audit` / `AuditLogs.Add`. No `tsc`, no `npm`, no HTTP, no product edit. |
| Precedence | On-disk tree supersedes A51/A57/A62/B10/B22 file-existence claims. Architecture §46 wins for the **label**. A26 wins for the **route** `/audit`. A63 wins for first-useful **GET** spelling (`/api/v1/audit/logs`). A51 wins for **writer + RBAC + append-only** law. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict

**The §46 Audit *leaf* is not missing. The Audit *page* is.**

`Audit` is on the sidebar with the **exact** §46 string, routed at A26 `/audit`, and backed by `pages/AuditPage.tsx` (324 bytes, 8 physical lines). That file is a static heading plus one sentence. It does not fetch, filter, paginate, or render `audit_logs`. There is no `useAudit`, no `GET /api/v1/audit` or `GET /api/v1/audit/logs`, no `ErrorState` for ReadOnly 403, no auth, and no writer that inserts `AuditLog` rows.

Do **not** recreate `AuditPage.tsx` from B10/B22’s “never created” list. Those snapshots are **stale** as of 13:20:38. Do **not** treat the 324-byte stub as a §46/A26/A51 page.

| Surface | Required | Measured | Class |
|---|---|---|---|
| §46 nav label `Audit` | exact string, after System Health, before Settings | sidebar label **`Audit`** (exact) | **EXISTS_AND_GOOD** (label only) |
| A26 route `/audit` | yes | `App.tsx` `path="audit"` | **EXISTS_AND_GOOD** (route only) |
| Page module | A62 `pages/audit/AuditPage.tsx` | `pages/AuditPage.tsx` (flat, not a folder) | **EXISTS_NEEDS_REFACTOR** (file) |
| Reader | A26 §6.15 / A63 `GET` + table + filters | no hook, no table, no filters | **MISSING** |
| API | A63 first-useful `GET /api/v1/audit/logs` | no `MapGet` | **MISSING** |
| RBAC | §59 / A51 `AuditReader` | anonymous shell; link always shown | **MISSING** |
| Persistence | §45 `audit_logs` append-only + sanitizer | `DbSet<AuditLog>` mapped; **0 writers, 0 readers**, no trigger | **EXISTS_NEEDS_REFACTOR** (entity) / **MISSING** (trail) |
| Invented rows | forbidden | stub invents **none** (good) | safe-by-absence |

**One-line:** chrome PASS, contract FAIL. First-useful Audit GET (A63 / A51 §14.1 / A57 “Audit is required for overrides”) is a **gap**. Live `NewOrderSingle` is not implicated.

Overall: **FAIL** for “architecture §46 Audit page exists.” **PASS** for “`/audit` no longer 404s on a missing module.”

---

## 1. Binding sources

| Role | Path / lines |
|---|---|
| Nav law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §46 lines 1735–1758 — label **`Audit`** |
| Table name | same file §45 line 1729 `audit_logs` |
| Privilege + “all actions must be audited” | §59 lines 2200–2224; §72.19 line 2721 |
| Route + page GET | `A26_dashboard_api_spec.md` §5.2 `/audit`; §6.15 `GET /api/v1/audit`; §9 page matrix; §10.1 Audit GET |
| First-useful GET spelling | `A63_api_catalog.md` §4 / §7.1 `GET /api/v1/audit/logs` RiskManager+ |
| Writer, append-only, redaction, row filter | `A51_rbac_audit.md` §4.3, §5.1, §8, §14.1 |
| Target tree | `A62_react_scaffold.md` `src/pages/audit/AuditPage.tsx` + `src/api/audit.ts` + `ErrorState` |
| EF column contract (narrower) | `A61_efcore_schema.md` §8.42 |
| Catalog row | `A20_table_catalog.md` table 46 |
| First-useful UI note | `A57_first_useful_version.md` item 12: Audit nav **required** for overrides (Models/Live may omit) |
| Stale “file missing” | `B10_web_gap.md`, `B22_web_missing_pages.md` (pre-13:20:38) |
| Stale “entity missing” | `A51` §1 / §16 (`audit_logs` **MISSING**) — entity now exists |
| Sibling census | `C08_web_pages_review.md`, `B20_web_pages_gap.md`, `B31_nav_gaps.md` |

When documents disagree (recorded, not resolved here):

| Topic | A26 | A51 / A63 | This report |
|---|---|---|---|
| GET path | `/api/v1/audit` | `/api/v1/audit/logs` | **Do not ship both.** First-useful host work binds **A63**. A26 page route stays `/audit`. |
| Analyst GET | **R** (§10.1, A62) | **403** (`audit.read` = RiskManager+) | Spec conflict. Implementation must pick **one** catalog. A51 is the RBAC law; A26 is the page-role matrix. |
| `AuditLog` shape | DTO: `auditId`, `actorEmail`, `before`/`after` objects | Table: UUID + `before_json`/`after_json` + categories | Domain type on disk is a **third**, narrower shape (`Guid` + `PayloadJson`). None of the three is wired to HTTP. |

---

## 2. What §46 actually requires

Architecture §46 is a **main-navigation list**. It does not give Audit a widget table (those start at §47 Overview). Verbatim order, last two leaves:

```text
System Health
Audit
Settings
```

So the architecture-document question “is Audit missing from §46?” splits:

| Question | Answer |
|---|---|
| Is the **name** on the nav? | **Yes.** Label exact. |
| Is the **page** the operational reader §59 / A26 / A51 describe? | **No.** |

A26 §5.3: left-nav labels **exactly** as §46. Audit is one of the seven current labels that already satisfy that law (`Overview`, `Brokers`, `Traders`, `Scoring`, `Risk`, `Audit`, `Settings`). That is chrome, not a dashboard.

A57: Models / Live Copy may be omitted from first-useful nav; **Audit may not**, because every manual override must be visible (§72.19). The stub does not display overrides.

---

## 3. Disk — Audit chrome (this pass)

### 3.1 Files hashed

| Path | Bytes | Phys. lines | Written | SHA-256 |
|---|---:|---:|---|---|
| `D:\Prop\apps\web\src\pages\AuditPage.tsx` | 324 | 8 | 2026-08-18T13:20:38+05:30 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | 2026-08-18T13:20:38+05:30 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | 2026-08-18T13:20:38+05:30 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | 2026-08-18T13:16:00+05:30 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | 2026-08-18T13:08:06+05:30 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | 2026-08-18T13:08:18+05:30 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` |
| `D:\Prop\apps\api\Program.cs` | 4658 | 95 | 2026-08-18T13:22:04+05:30 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| `D:\Prop\src\Domain\Entities\AuditLog.cs` | 403 | 12 | 2026-08-18T13:09:03+05:30 | `FD5CD6EC5D18D3A0350891A21EF0B8E5FD33323035FD88ED06A95356B8EA4BA6` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | 174 | 2026-08-18T13:12:48+05:30 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 2577 | 97 | 2026-08-18T13:09:51+05:30 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` |

`list_dir` `D:\Prop\apps\web\src\pages` = **15** `.tsx` files, including `AuditPage.tsx`. No `pages/audit/` folder. No `LoginPage.tsx`. No `ModelsPage.tsx`.

Grep under `D:\Prop\apps\web` for `useAudit`, `ErrorState`, `pages/audit`, `api/audit`: **zero** hits besides the three chrome files above (`DashboardLayout` `/audit`, `App.tsx` import/route, `AuditPage.tsx` itself).

### 3.2 Entire page module

`AuditPage.tsx` (324 bytes) is the complete implementation:

```tsx
export default function AuditPage() {
  return (
    <div className="space-y-3">
      <h1 className="text-2xl font-semibold text-white">Audit</h1>
      <p className="text-gray-400 text-sm">Manual overrides, kill-switch changes, and mapping edits must land here. RBAC is not enabled in the demo seed.</p>
    </div>
  );
}
```

Honest about RBAC being off. **No table. No filters. No fetch. Invents no rows.** Title matches §46. That is a placeholder, not a reader.

### 3.3 Router + sidebar

`App.tsx` line 16 imports `AuditPage`; line 37 `<Route path="audit" element={<AuditPage />} />`. No `/login`. No catch-all → `/overview`.

`DashboardLayout.tsx` line 18: `{ to: '/audit', label: 'Audit', icon: '☰' }` — 13th of 14 `NavLink`s (Models absent). `main.tsx` is `QueryClient` + `BrowserRouter` only. No `AuthProvider`, no `RequireRole`.

---

## 4. Required page contract vs now

A26 §6.15 + §9 + A62 + A51 §8.5. Architecture §46 does not list columns; these come from the dashboard contract that §46’s Audit leaf is supposed to host.

### 4.1 Shell / route

| Fact | Binding | Painted? | Evidence |
|---|---|---|---|
| Nav label `Audit` | §46 + A26 §5.3 | **YES** | `DashboardLayout` line 18 |
| Order after System Health, before Settings | §46 | **YES** | Health → Audit → Settings |
| Route `/audit` | A26 §5.2 | **YES** | `App.tsx` line 37 |
| Folder `pages/audit/AuditPage.tsx` | A62 | **NO** | flat `pages/AuditPage.tsx` |
| `paths.ts` / `NAV_ITEMS` single source | A62 §6 | **NO** | raw array in the layout |
| Header strip (5 chips) | A26 §5.3 | **NO** | layout has no header |
| Hide nav from unauthenticated | A26 `/login` | **NO** | no login, no auth |
| Hide or ErrorState for ReadOnly | A62; A51 Analyst also 403 | **NO** | always visible; no roles |

### 4.2 Table columns (A26 §6.15 example row)

| Column | Painted? | Notes |
|---|---|---|
| `auditId` | **NO** | |
| `at` | **NO** | |
| `actorId` | **NO** | |
| `actorEmail` | **NO** | Domain has `Actor` string, not email |
| `role` | **NO** | |
| `action` | **NO** | |
| `entityType` | **NO** | |
| `entityId` | **NO** | Domain has `Target` |
| `reason` | **NO** | |
| `correlationId` | **NO** | not on Domain type |
| `before` (sanitized) | **NO** | Domain has opaque `PayloadJson` |
| `after` (sanitized) | **NO** | |
| pagination `page` / `pageSize` / `totalItems` / `totalPages` | **NO** | A26 default pageSize 50 |

**Score: 0 / 12 painted facts + 0 / 4 pagination fields.**

### 4.3 Filters (A26 query; A51 adds more)

| Query | A26 | A51 §8.5 | UI? | API? |
|---|---|---|---|---|
| `actorId` / `actor` | yes | `actor` | **NO** | **NO** |
| `action` | yes | yes | **NO** | **NO** |
| `from` / `to` | yes | yes | **NO** | **NO** |
| `entityType` / `entityId` | yes | yes | **NO** | **NO** |
| `outcome` | no | yes | **NO** | **NO** |
| `correlationId` | no | yes | **NO** | **NO** |
| `cursor` / `page` (max 200) | page | both | **NO** | **NO** |

**Filters: 0 / 6 A26, 0 / 9 A51.** Default sort `occurred_at DESC` is unimplemented.

### 4.4 Client module (A62)

| File | Required | Disk |
|---|---|---|
| `src/api/audit.ts` | yes | **MISSING** |
| `useAudit` in `hooks.ts` | implied | **MISSING** (11 hooks; none audit) |
| `queryKeys` audit entry | A62 | **MISSING** (`queryKeys.ts` absent) |
| `ErrorState` on 403 | A62 | **MISSING** (no such component) |
| Client secret denylist | §55 / A62 `secrets/denylist.ts` | **MISSING** |
| Typed Audit DTO | A26 row | **MISSING** (`types/index.ts` has no Audit interface; file is unused) |

`hooks.ts` calls unversioned `/api/overview`, `/api/brokers`, … `/api/settings`. Adding `useAudit` against `/api/audit` would still miss A26/A63 `/api/v1/...`.

---

## 5. API host — no Audit map

`D:\Prop\apps\api\Program.cs` live maps (anonymous, CORS `AllowAnyOrigin`):

```text
GET  /health
GET  /api/health
GET  /api/risk/status
GET  /api/reconciliation/status
GET  /api/settings
GET  /ready
GET  /api/overview
GET  /api/brokers
GET  /api/groups
GET  /api/traders
GET  /api/traders/{broker}/{login}
GET  /api/fix/sessions
GET  /api/risk
GET  /api/trades
POST /api/ops/resync
```

**Zero** of: `/api/v1/audit`, `/api/v1/audit/logs`, `/api/audit`, `/api/audit/logs`.

`IDashboardQueries` (`DashboardModels.cs` lines 88–97) has Overview / Brokers / Groups / Traders / Trader / FIX / Risk. **No** `GetAuditAsync`.

`POST /api/ops/resync` is an anonymous privileged-adjacent mutation (ingest + reconstruct + score). It writes **no** `AuditLog` row. That is a §72.19 hole on the host that the Audit page would have to show if a writer existed.

C04 already recorded: no `[Authorize]`, no sanitizer middleware, weatherforecast **gone** from `Program.cs`. Safe-by-absence for secrets; **not** an Audit API.

---

## 6. Persistence — entity exists, trail does not

### 6.1 Domain type on disk

`D:\Prop\src\Domain\Entities\AuditLog.cs` (403 B):

```csharp
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset At { get; set; }
}
```

### 6.2 EF map

`TraderDbContext` line 30 `DbSet<AuditLog> AuditLogs`; lines 162–166:

```csharp
modelBuilder.Entity<AuditLog>(e =>
{
    e.ToTable("audit_logs");
    e.HasKey(x => x.Id);
});
```

No indexes. No `HasDatabaseName`. No append-only trigger. No `REVOKE UPDATE`. `EnsureCreated` at API boot will create a **Guid PK** table, not A20’s `bigint IDENTITY` and not A51’s UUID-plus-category schema.

Grep of `D:\Prop\src` for `AuditLogs` / `new AuditLog` / `AuditLog(`: **only** the entity and the `DbSet` + `ToTable`. **Zero inserts. Zero queries.**

### 6.3 Three schemas, zero HTTP

| Field (intent) | Domain now | A61 §8.42 | A51 §8.2 | A26 §6.15 DTO |
|---|---|---|---|---|
| PK | `Guid Id` | `bigint IDENTITY` | `uuid id` | `auditId` |
| Time | `At` | `OccurredAt` | `occurred_at` | `at` |
| Actor | `Actor` text | `ActorId` text | `actor_user_id` + snapshot username | `actorId` + `actorEmail` |
| Role | `Role` unconstrained | — | `actor_role` check | `role` |
| Action | `Action` | `Action` | permission code + `action_category` | `action` |
| Target | `Target` | `EntityType`/`EntityId` | both required | `entityType`/`entityId` |
| Payload | `PayloadJson` | `Details` jsonb | `before_json`/`after_json` + `metadata_json` | `before`/`after` objects |
| Correlation | **absent** | `CorrelationId` | required | `correlationId` |
| Outcome | **absent** | — | `succeeded`/`failed`/`denied` | **absent** |
| Immutability | none | none specified beyond “append” | trigger + REVOKE | — |

A51 §1 still says `audit_logs` **MISSING**. **Stale for the type.** Still true for the **trail** (no migration, no trigger, no writer, no reader).

A51: `AuditLog` is **not** a navigation collection on other aggregates. Current model respects that (no `Trader.AuditLogs`). That is the only A51 persistence rule this tree already meets.

---

## 7. RBAC vs the always-on `/audit` link

| Rule | Binding | Now |
|---|---|---|
| Four roles | §59 | **MISSING** |
| `GET` Audit: SuperAdmin + RiskManager (+ Analyst in A26) | A26 §10.1 / A51 `AuditReader` | **no auth** — anyone who can load Vite sees the stub |
| ReadOnly → 403 + `ErrorState`, not empty table | A62 | n/a (anonymous); stub is also not a table |
| Successful reads not written to `audit_logs` | A51 §4.3 | n/a (no GET) |
| Denied privileged mutation **is** audited | A51 | no mutations are authorized; `POST /api/ops/resync` is anonymous and unaudited |
| SuperAdmin cannot UPDATE/DELETE audit | A51 §5.1 / §8.3 | no API either way |
| Nav visible to all authenticated roles | A26 §5.3 | visible to **everyone**, including no-role |

The stub’s sentence “RBAC is not enabled in the demo seed” is **true**. It is not a substitute for `RequireRole` or a 403 page.

---

## 8. First-useful vs later

| Surface | §69 / A63 / A51 §14.1 | Full A26 |
|---|---|---|
| Audit **nav** | **required** (A57: do not omit) | required exact label |
| Audit **GET** | **required** `GET /api/v1/audit/logs` RiskManager+ | `GET /api/v1/audit` + Analyst R |
| Audit **writer** on group toggle / trader.state / stop-new | **required** same transaction as mutate | + every §59 verb |
| Models / Live books | out of A63 v1 | later |
| Flatten / enable execution / promote | later; still must be audited **when** added | same |

Unlike Models and Live Copy, **an empty-safe Audit table is in first useful**. The current stub does not satisfy A63. Shipping `/audit` as two sentences leaves §72.19 unauditable from the operator UI.

---

## 9. What earlier reports claimed vs this pass

| Prior claim | This pass |
|---|---|
| A51 §16 / A62 era: “React Audit page does not exist yet” | **Stale for the file.** Still true for the **reader**. |
| A57: “No Models / Live Copy / Audit nav items” | **Stale.** Live + Audit are in the 14-link sidebar. Models still absent. |
| A62 `pages/` EMPTY | **Stale** (B22/C08). |
| B10 / B22: Audit file + route **MISSING** | **Stale as of 13:20:38.** File + `/audit` + nav exist. |
| B20 / B31 / C08: 324 B stub, no hook, no API | **Still true.** Hashes unchanged. |
| B30: `GET /api/v1/audit` hook **MISSING** | **Still true.** |
| A51 §1: `audit_logs` entity **MISSING** | **Stale.** `AuditLog` + `DbSet` + `ToTable("audit_logs")` exist. Trail still missing. |

Use **this file** for the Audit-page question. Use B31 for the three-item (Models / Live / Audit) nav roll-up. Do not recreate the stub.

---

## 10. What a later coding wave must not do

This report does **not** authorize product edits.

1. Do **not** recreate `AuditPage.tsx` as if the module were absent.
2. Do **not** invent audit rows in the React tree or in a mock.
3. Do **not** dump `AuditLog` / EF entities / `PayloadJson` unsanitized to the browser.
4. Do **not** implement **both** `GET /api/v1/audit` and `GET /api/v1/audit/logs`. Pick A63 for the host; keep the page route `/audit`.
5. Do **not** treat `system_events` or `risk_decisions` as a substitute for `audit_logs` (A51 §17).
6. Do **not** add `UPDATE`/`DELETE` on `audit_logs`. Corrections are new rows.
7. Do **not** hide the route with UI-only checks; API is the authority (A51).
8. Do **not** put FIX/MT5/Redis/DB passwords in `before`/`after` (A51 §8.4 denylist).
9. Do **not** mark RBAC done because the stub mentions it.
10. Do **not** invent a second `pages/` tree under `D:\Prop\src`.
11. Do **not** enable live `NewOrderSingle` to “have something to audit.”
12. Resolve Analyst GET (A26 R vs A51 403) in the RBAC catalog **before** painting the page, and do not ship two behaviors.

---

## 11. Direct answers

### Is the Architecture §46 Audit page missing?

| Layer | Missing? |
|---|---|
| §46 sidebar **label** | **No** — exact `Audit` |
| A26 **route** `/audit` | **No** |
| **File** `AuditPage.tsx` | **No** (324 B stub since 13:20:38) |
| A62 folder `pages/audit/` | **Yes** (flat file instead) |
| **Reader** (table, filters, pagination, sanitizer) | **Yes — missing** |
| **Hook** `useAudit` / `api/audit.ts` | **Yes — missing** |
| **API** `GET /api/v1/audit[/logs]` | **Yes — missing** (first-useful gap) |
| **RBAC** + ReadOnly `ErrorState` | **Yes — missing** |
| **Writer** + append-only trail | **Yes — missing** (entity only) |

**Copy-out:** Architecture §46 Audit is present as chrome and **missing as a page**. Classification: **`EXISTS_NEEDS_REFACTOR`** (file/route/label) and **`MISSING`** (contract, API, RBAC, trail). `EXISTS_AND_GOOD` count for the Audit page: **0**.

### Did this pass change product source?

**No.**

---

## Evidence pins

- Architecture §46: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1735–1758 (`Audit` nav).
- Architecture §45 `audit_logs`: same file line 1729.
- Architecture §59 / §72.19: lines 2200–2224, 2721.
- A26 Audit GET: `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2, §6.15, §9, §10.1.
- A63 first-useful GET: `D:\Prop\reports\swarm\20260818\A63_api_catalog.md` §4 line `GET /api/v1/audit/logs`.
- A51 trail law: `D:\Prop\reports\swarm\20260818\A51_rbac_audit.md` §8, §14.1.
- Page: `D:\Prop\apps\web\src\pages\AuditPage.tsx` SHA-256 `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` (324 B, 8 lines).
- Router: `D:\Prop\apps\web\src\App.tsx` lines 16, 37. SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` line 18. SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` — 11 queries, no audit. SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20`.
- API: `D:\Prop\apps\api\Program.cs` — 15 maps; no audit. SHA-256 `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9`.
- Queries: `D:\Prop\src\Application\Dashboard\DashboardModels.cs` `IDashboardQueries` — 7 methods, none audit.
- Entity: `D:\Prop\src\Domain\Entities\AuditLog.cs` SHA-256 `FD5CD6EC5D18D3A0350891A21EF0B8E5FD33323035FD88ED06A95356B8EA4BA6`.
- Map: `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` lines 30, 162–166.
- Supersedes B10/B22 on Audit **file** absence. Does not supersede their “no reader” finding.
- Complements B31 §7 (three-item nav) with a dedicated Audit-page depth audit.
