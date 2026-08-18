# C18 — Confirm RBAC is not implemented

| Field | Value |
|---|---|
| Agent | C18 (senior engineer, identity / RBAC confirmation only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:25:40+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\C18_rbac_missing.md` |
| Authority | Architecture v2 **§59**; supporting §§40, 45, 46, 55, 68, 69, 72.19 |
| Binding specs | A51 (role model + `audit_logs` contract), A26 §10 (page/action matrix), A48 §8 (kill-switch RBAC), A63 §4 (auth routes) |
| Product source modified | **No.** This report is the only write. |
| Method | Source inspection of `apps/api`, `apps/web/src`, `src/Domain`, `src/Application`, `src/Infrastructure`, `apps/mt5-worker`, `apps/fix-worker`, `tests`. SHA-256 of key files. Recursive search for `User*` / `*Role*` / `*Auth*` / `*Rbac*` product files, EF `Migrations`, and ASP.NET auth packages. API **not** launched. No HTTP traffic captured. |
| Relates | A06 / B06 (anonymous host), A51 (design only), B10 §8 (React auth MISSING), B33 (`AuditLog` stub), C08 (Login page never written) |

---

## 0. Verdict

**RBAC is not implemented.** Architecture §59 names four dashboard roles and eight privileged mutations that must be role-gated **and** audited. Product source has none of: authentication, authorization policies, identity tables, role enum, login/me endpoints, React role gates, step-up confirmation, or an `audit_logs` writer.

Honest measured state (2026-08-18, files as hashed below):

| Control | Required by | Status |
|---|---|---|
| Roles `SuperAdmin`, `RiskManager`, `Analyst`, `ReadOnly` | §59, A51 §3 | **MISSING** — strings exist only in architecture + reports. Zero C# / TS identifiers. |
| Authentication (`login` / `refresh` / `me`) | A51 §11, A63 §4 | **MISSING** |
| Authorization policies (`ReadOnlyPlus` … `SuperAdminOnly`) | A51 §3.1 | **MISSING** |
| `users` / `user_sessions` / `step_up_challenges` | A51 §6 | **MISSING** (no entity, no `DbSet`, no migration) |
| `UserRole` enum | A51 §3 (`Domain/Enums/UserRole.cs`) | **MISSING** |
| JWT / cookie / Identity packages | A51 §11 | **MISSING** — no `Microsoft.AspNetCore.Authentication*`, no `JwtBearer`, no Identity |
| `[Authorize]` / `RequireAuthorization` / `IAuthorizationHandler` | A51 §16 | **MISSING** (0 hits in product `.cs`) |
| `AddAuthentication` / `AddAuthorization` / `UseAuthentication` | A51 | **MISSING** from every `Program.cs` |
| Seed SuperAdmin | A51 §6.1 | **MISSING** — `DemoSeeder` seeds brokers / FIX / kill-switch `SetBy = "system"` only |
| `POST /api/v1/auth/login` (+ logout / refresh / me) | A63 §4 | **MISSING** — no `/api/v1/**` prefix at all |
| React Login / `AuthProvider` / `RequireRole` / `can.ts` | A26 §11, A62, B10 §8 | **MISSING** |
| Role-gated writes for the eight §59 verbs | §59, A26 §10.2 | **MISSING** — those endpoints do not exist |
| `audit_logs` writer + append-only + redaction | §59, §72.19, A51 §7–8 | **MISSING** |
| `AuditLog` POCO + `DbSet` + `ToTable("audit_logs")` | §45, B33 | **EXISTS_NEEDS_REFACTOR** — schema stub only. Not RBAC. |
| Public self-registration | A51 §2.6 | correctly **absent** |
| `localStorage` role | A26 §11 | correctly **unused** |

Classification (arch §73):

| Component | Class |
|---|---|
| Identity + RBAC enforcement | **MISSING** |
| Auth HTTP surface | **MISSING** |
| React auth / role UI | **MISSING** |
| RBAC unit / integration tests (A51 §15) | **MISSING** |
| `AuditLog` entity mapping | **EXISTS_NEEDS_REFACTOR** |
| `apps/api` HTTP host | **EXISTS_NEEDS_REFACTOR** — 15 anonymous maps, including `POST /api/ops/resync` |
| A51 / A26 / A48 / A63 RBAC text | design / spec only — **not** code |

**Do not mark RBAC “done” because `AuditLog` exists, because a page is named Audit, or because reports describe a matrix.** A51 §15: do not mark RBAC done because an `[Authorize]` attribute exists on one controller — and no such attribute exists.

This file **supersedes** A51 §16 row “`audit_logs` = MISSING (named in §45)” for the **entity**. The entity is now a stub. The **control** (auth + policies + writer) remains **MISSING**. A51’s claim “RBAC is not implemented” is still true.

---

## 1. Method

| Source | Path / action |
|---|---|
| Architecture §59 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2200–2224 |
| API host | `D:\Prop\apps\api\Program.cs` (95 physical lines, 4658 bytes) |
| API project | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` |
| Domain entities / enums | `D:\Prop\src\Domain\Entities`, `D:\Prop\src\Domain\Enums` |
| EF | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`; `Persistence\Configurations\` **empty**; no `Migrations` directory under `D:\Prop` product tree |
| DI | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| Seed | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| Workers | `D:\Prop\apps\mt5-worker\Program.cs`, `D:\Prop\apps\fix-worker\Program.cs` |
| React | `apps/web/src/{App,main,layouts,api,pages}` |
| Tests | `D:\Prop\tests\Unit`, `D:\Prop\tests\Integration` — 11 `*.cs` files, none auth/RBAC |
| Package search | every product `*.csproj`: 0 `Authentication` / `JwtBearer` / `Identity` references |
| Identifier search | `Authorize`, `AddAuthentication`, `JwtBearer`, `UserRole`, `SuperAdmin`, `RequireRole`, `AuthProvider`, `/auth/` in `*.cs` / `*.ts` / `*.tsx` (exclude `bin`/`obj`/`vendor`/`node_modules`) |
| File-name search | `User*.cs`, `*Role*.cs`, `*Auth*.cs`, `*Rbac*.cs`, `RequireRole*`, `can.ts`, `AuthProvider*` under `src` / `apps` / `tests` → **0 files** |

No `tsc`, no `dotnet`, no product edit.

---

## 2. File hashes (product, read-only)

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\src\Domain\Entities\AuditLog.cs` | 403 | `FD5CD6EC5D18D3A0350891A21EF0B8E5FD33323035FD88ED06A95356B8EA4BA6` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` |
| `D:\Prop\apps\web\src\pages\AuditPage.tsx` | 324 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` |
| `D:\Prop\apps\web\package.json` | 739 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` |

`Program.cs` SHA-256 **changed** from B06 `13CF8003…` (4503 bytes) to `E914FA98…` (4658 bytes). Auth was **not** added in that delta — the host is still fully anonymous (section 4).

---

## 3. What §59 requires (binding, not implemented)

Architecture §59 (verbatim contract this confirmation is measured against):

Dashboard roles:

```text
SuperAdmin
RiskManager
Analyst
ReadOnly
```

Only authorized roles may:

```text
enable real execution
change risk limits
pause/resume trader copying
change symbol mapping
activate stop-new-orders
request emergency flatten
promote a model
change broker/FIX configuration
```

All actions must be audited.

A51 freezes policy names, one-role-per-user, hierarchy `ReadOnly ⊂ Analyst ⊂ RiskManager ⊂ SuperAdmin`, permission codes, and `audit_logs` rules. A26 §10 and A51 §5 **disagree** on who may request `EMERGENCY_FLATTEN` (A26: RiskManager; A51: SuperAdmin + step-up). That is a **spec** conflict. Neither matrix is in code.

Unauthenticated `/api/**` must be **401**. Authenticated but wrong role must be **403**. Measured host: every mapped route is anonymous **200** (no challenge).

---

## 4. API — entire surface is anonymous

`D:\Prop\apps\api\Program.cs` (hashed above) does **not** call `AddAuthentication`, `AddAuthorization`, `UseAuthentication`, or `UseAuthorization`. There is no JWT bearer, no cookie scheme, no policy registration.

CORS is `AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()`. `appsettings.json` has `"AllowedHosts": "*"`. Swagger is enabled whenever `IsDevelopment()` — also ungated.

Live maps (15), all anonymous, none under `/api/v1`:

| # | Method | Path | Notes |
|---:|---|---|---|
| 1 | GET | `/health` | liveness; A51 allows this anonymous |
| 2 | GET | `/ready` | readiness; A51 optionally allows |
| 3 | GET | `/api/health` | dashboard health |
| 4 | GET | `/api/overview` | should be `ReadOnly+` |
| 5 | GET | `/api/brokers` | should be `ReadOnly+` |
| 6 | GET | `/api/groups` | should be `ReadOnly+` |
| 7 | GET | `/api/traders` | should be `ReadOnly+` |
| 8 | GET | `/api/traders/{broker}/{login}` | should be `ReadOnly+` |
| 9 | GET | `/api/trades` | should be `ReadOnly+` |
| 10 | GET | `/api/fix/sessions` | should be `ReadOnly+` |
| 11 | GET | `/api/risk` | should be `ReadOnly+` |
| 12 | GET | `/api/risk/status` | duplicate of `/api/risk` |
| 13 | GET | `/api/reconciliation/status` | should be `ReadOnly+` |
| 14 | GET | `/api/settings` | flags + broker names; no auth |
| 15 | POST | `/api/ops/resync` | **mutation**, demo ingest+score rebuild, **no actor, no audit** |

Absent vs A63 §4 (required to make RBAC real):

| Method | Path | Status |
|---|---|---|
| POST | `/api/v1/auth/login` | **MISSING** |
| POST | `/api/v1/auth/logout` | **MISSING** |
| POST | `/api/v1/auth/refresh` | **MISSING** |
| GET | `/api/v1/auth/me` | **MISSING** |
| GET | `/api/v1/audit/logs` | **MISSING** |

`TraderIntelligence.Api.csproj` packages: `Microsoft.AspNetCore.SignalR.Common` 8.0.4, `Serilog.AspNetCore` 8.0.2, `Swashbuckle.AspNetCore` 6.6.2. **No** auth package. `AddSignalR` / `MapHub` are not called; React still opens `/hubs/dashboard` without an `access_token` (`apps/web/src/api/signalr.ts`).

`TraderIntelligence.Api.http` lists unauthenticated GETs only. No Authorization header sample.

Workers (`mt5-worker`, `fix-worker`) are generic hosts. They do not mint dashboard identities and do not write `audit_logs`.

---

## 5. Domain / Infrastructure — no identity, no enforcement

### 5.1 Files that A51 named and that are still absent

| A51 path | Status |
|---|---|
| `Domain/Enums/UserRole.cs` | **MISSING** |
| `users` entity + table | **MISSING** |
| `user_sessions` | **MISSING** |
| `step_up_challenges` | **MISSING** |
| Authorization handlers / policy registrar | **MISSING** |
| Audit writer (same transaction as mutation) | **MISSING** |
| Append-only trigger / interceptor | **MISSING** |
| Versioned EF migration for identity + `audit_logs` | **MISSING** (`EnsureCreated` only) |

`src/Domain/Enums` contains 15 files (`CopyIntentAction` … `TraderState`). None is a dashboard role.

`src/Domain/Entities` contains 20 files. None is `User`. `AuditLog` and `KillSwitch` exist (below).

`DependencyInjection.AddTraderIntelligence` registers EF, fake MT5 connectors, ingest, scoring, dashboard queries. It does **not** register an identity store or auth services.

### 5.2 `AuditLog` is a stub, not an audit trail

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

`TraderDbContext` exposes `DbSet<AuditLog> AuditLogs` mapped to `audit_logs` with a PK only (no indexes, no check on `Role`, no immutability). B33: **EXISTS_NEEDS_REFACTOR** (A20 wanted `bigint GENERATED ALWAYS AS IDENTITY`; A51 wanted actor id, outcome, correlation id, redaction).

Grep of product `*.cs` for `new AuditLog` / `AuditLogs.Add` / `AuditLogs.AddRange`: **0 writers**. `DemoSeeder` does not insert audit rows. `IDashboardQueries` has no audit query. There is no API to read `audit_logs`.

`Role` is a free `string`. It is **not** `UserRole` and it is never validated against the four §59 names.

### 5.3 Kill switch is not RBAC-gated

`KillSwitch` has `SetBy` / `Reason` / `Mode`. Seeded as `Mode = None`, `SetBy = "system"`. Dashboard **reads** the latest row (`EfDashboardQueries.GetRiskAsync`). There is **no** `POST /risk/stop-new-execution` or `POST /risk/emergency-flatten`. RiskEngine consumes a caller-supplied `KillSwitchMode` in unit tests. A48 §8 matrix is unimplemented.

---

## 6. §59 privileged mutations — 0 / 8 endpoints

| §59 action | A51 permission | Specified route (A26 / A51) | Product |
|---|---|---|---|
| Enable real execution | `execution.enable` | `PATCH /settings/execution` or `POST /api/v1/execution/enable` | **MISSING.** `GET /api/settings` hardcodes `REAL_COPY_EXECUTION_ENABLED = false`. No PATCH. |
| Change risk limits | `risk.limits.write` | `PATCH /risk/limits` | **MISSING.** Settings GET returns a hardcoded `maxQuoteAgeSeconds` dict. |
| Pause / resume trader copying | `trader.copy.pause_resume` | `POST .../copy-control` / `PATCH .../state` | **MISSING.** |
| Change symbol mapping | `mapping.symbol.write` | `PATCH /settings/symbol-mappings` | **MISSING.** |
| Activate stop-new-orders | `risk.stop_new.activate` | `POST /risk/stop-new-execution` | **MISSING.** |
| Request emergency flatten | `risk.flatten.request` | `POST /risk/emergency-flatten` | **MISSING.** |
| Promote a model | `model.promote` | `POST /models/{id}/promote` | **MISSING** (correctly out of v1; still no 404/409 stub). |
| Change broker / FIX configuration | `config.broker_fix.write` | not via React | **MISSING.** No config write API. |

The only write on the host is `POST /api/ops/resync`. It is **not** in the §59 list. It is **not** authenticated. It is **not** audited. B10 already said: do not wire a React button to it until RBAC exists.

Derived A51 first-useful writes (`group.analysis.toggle`, `trader.state.write`) are also **MISSING**.

---

## 7. React — no login, no role, all nav open

| Control | Path | Status |
|---|---|---|
| Login page | A26 §5.2 / C08 | **MISSING** — not in `pages/`, not in `App.tsx` |
| `AuthProvider` / `RequireAuth` / `RequireRole` | A62 / B10 | **MISSING** |
| `can.ts` / `useAuth` / `useRole` | A26 §11 | **MISSING** |
| `GET /auth/me` client | A26 §11 | **MISSING** |
| Axios `Authorization` interceptor | `apps/web/src/api/client.ts` | **MISSING** — JSON client, no token |
| Confirm-phrase modal | A48 / B10 | **MISSING** (and no flatten/enable buttons — correct absence) |
| Auth libraries | `package.json` | React, router, query, axios, recharts, SignalR only |

`App.tsx` wraps every route in `DashboardLayout` with **no** auth guard. Index redirects to `/overview`. C08: 15/15 page imports match; Login and Models were never written.

`DashboardLayout` renders the full §46-ish nav (including **Audit**) to whoever opens the SPA. A51 §5.3: ReadOnly and Analyst must **not** see Audit. UI hiding is not security; here even the hide is absent.

`AuditPage.tsx` is a static stub. It does not fetch. It states the measured fact in the UI:

> Manual overrides, kill-switch changes, and mapping edits must land here. RBAC is not enabled in the demo seed.

`RiskPage` and `SettingsPage` are read-only displays. `LiveCopyPage` is a banner that real copy is off. There are no write buttons to gate — and no API that would reject an ungated write.

`main.tsx` mounts `QueryClientProvider` + `BrowserRouter` only.

---

## 8. Tests — A51 §15 inventory is 0 / 12

Required by A51 §15 and still absent:

| Test (A51) | Status |
|---|---|
| Policy matrix (role × permission) | **MISSING** |
| Redaction of password / connection string / refresh hash | **MISSING** |
| Hierarchy (Analyst cannot `trader.state.write`; RiskManager cannot flatten) | **MISSING** |
| Last SuperAdmin demote/disable rejected | **MISSING** |
| Step-up deny (missing / wrong purpose / expired) | **MISSING** |
| LIVE while flag false → 409 | **MISSING** |
| Mutation + audit same transaction | **MISSING** |
| Append-only `UPDATE audit_logs` fails | **MISSING** |
| Idempotency key → one audit row | **MISSING** |
| Denied PATCH → 403 + `outcome=denied` | **MISSING** |
| Audit GET ReadOnly 403; RiskManager cannot see `identity.user.role.write` | **MISSING** |
| Login fail never stores password | **MISSING** |

On-disk test classes: reconstruction, scoring, risk engine, volume, symbols, sizing, seeding/store, leftover `UnitTest1`. **Zero** `*Rbac*` / `*Auth*` tests.

---

## 9. Prior reports (keep; do not treat as implementation)

| Report | What it claimed | Still true? |
|---|---|---|
| A51 | Identity + RBAC + `audit_logs` = **MISSING**; design only | RBAC **yes**. `AuditLog` entity now exists as a stub (this file). |
| A06 / B06 | Entire API anonymous; no `/api/v1` | **Yes** (B06’s weatherforecast-gone snapshot still holds). |
| A26 §10 | Normative matrix | Spec only. |
| A29 U12 | SuperAdmin/RiskManager/Analyst/ReadOnly **MISSING** | **Yes.** |
| A57 / A63 | First-useful API is RBAC-gated | Routes still **0**. |
| B10 §8 | React auth stack **MISSING** | **Yes.** |
| B13 / A48 | Kill switch has no RBAC | **Yes.** |
| B20 / C08 | Login page **MISSING** | **Yes.** |
| B33 | `audit_logs` **EXISTS_NEEDS_REFACTOR** | **Yes** — mapping only. |
| PHASE0_AUDIT | “React … polish/RBAC later” | Still later. |

---

## 10. What would have to exist before anyone may say “RBAC implemented”

Minimum, matching A51 “do not weaken” + first useful (A51 §14 / A63):

1. `users` + password hash (argon2id) + one of four roles; seed one SuperAdmin out of band; no public register.
2. `POST /api/v1/auth/login` + refresh cookie + `GET /api/v1/auth/me`.
3. ASP.NET policies on **every** `/api/**` except login / health (and optionally ready).
4. First-useful writes (`group.analysis.toggle`, Watch/Shadow/Pause, `STOP_NEW_EXECUTION`) persist + write `audit_logs` in the **same** transaction.
5. `GET /api/v1/audit/logs` = RiskManager+; ReadOnly/Analyst = 403.
6. React Login + `RequireAuth`; hide Audit from ReadOnly/Analyst; **API remains authority**.
7. A51 §15 tests green. A single `[Authorize]` is not enough.

Until then the honest label is **MISSING**.

---

## 11. What this document does **not** do

- Does not modify `apps/api`, Domain, Infrastructure, React, workers, or tests.
- Does not implement JWT vs cookie BFF.
- Does not resolve A26 vs A51 flatten-role conflict.
- Does not treat `AuditLog.cs` or the Audit page stub as a control.
- Does not treat `KillSwitch.SetBy` as an actor.
- Does not claim the demo is safe to expose. It is an anonymous read API plus an anonymous resync POST.

**Bottom line:** §59 RBAC is **not implemented**. Specs (A51 / A26 / A48 / A63) exist. Product source has an anonymous API, an unguarded React shell, no identity schema, no policies, no tests, and an unused `audit_logs` stub. Confirm **MISSING**.
