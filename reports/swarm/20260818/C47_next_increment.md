# C47 — Next increment: Windows live MT5 connect, QuickFIXn net8 QUOTE logon, EF migrations, RBAC

| Field | Value |
|---|---|
| Agent | C47 (senior engineer, increment design only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C47_next_increment.md` |
| Assigned | Propose next increment: Windows live MT5 connect, QuickFIXn net8 QUOTE logon, EF migrations, RBAC. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only product of this agent. |
| Authority | Architecture v2 §§5–8, 10, 25–31, 41, 45, 55–59, 67–72 |
| Binding siblings | A14, A16, A20, A25, A30 (I1 leftover / I2 start / I7 start), A35, A36, A51, A54, A58, A61, A75, A100, A101, A105, B04, B05, C13, C14, C18, C19, C20, C22, C29, C42 |
| Classification | PLAN — not implemented |

**Honesty line:** this increment is the first *live-foundation* coding wave. It does **not** produce a first useful version (§69 still 0/12 until measured), does **not** flip any §68 go-live box, and does **not** authorize `REAL_COPY_EXECUTION_ENABLED=true` or any `35=D`.

```text
REAL_COPY_EXECUTION_ENABLED = false     -- stays false in every committed config
CTRADER_FIX_TRADE_SESSION_ENABLED       -- stays false / unused this increment
Live NewOrderSingle (35=D)              -- FORBIDDEN
C++ mt5-sdk                             -- reuse, do not rewrite (C20)
Product source in this agent            -- not touched
```

---

## 0. Verdict

**Next increment id: `C47` / `I-Live-Foundation`.**

Four slices, one increment, **strict internal order**:

| Slice | Name | Unlocks | Maps to |
|---|---|---|---|
| **C47.1** | Versioned EF migrations + replace `EnsureCreated` | Durable Postgres for every later slice | A30 I1 leftover, A61, C29 |
| **C47.2** | First-useful RBAC + audit writer | Stop anonymous mutations before live data | A51 §14.1, C18 |
| **C47.3** | Windows live MT5 connect (both brokers) | §69.1 start; honest health | A14/A16/A58/A105, C42 |
| **C47.4** | QuickFIXn 1.14.1 QUOTE TLS Logon | §69.9 start; kill stamp-health | A25/A35/A36, C19 |

**Why these four, now:** Domain algorithms, FakeMt5 demo ingest, reconstruction/scoring tests, and 15 React pages already exist (C13). The remaining §69 blockers that a single increment can legally start are **venue truth** (MT5 + QUOTE) plus the two prerequisites those sockets must not skip: **versioned schema** and **identity**. Doing sockets on InMemory/`EnsureCreated` with an anonymous `POST /api/ops/resync` is how the lab greenwashes.

**What “done” means for C47 (conjunction):**

1. Empty Postgres 16 applies **versioned** migrations (no `EnsureCreated` in hosts).
2. Unauthenticated `/api/**` (except login / liveness) returns **401**. First-useful writes persist + write `audit_logs` in the same transaction.
3. A **Windows x64** process maps `MT5APIManager64.dll` and opens Manager TCP to **both** Achiever and StarwaveFX. Dashboard `Connected` comes from `IsConnectedAsync`, not a literal `true`.
4. A QuickFIX/n **QUOTE** initiator completes TLS Logon on 5211. `fix_sessions` status is driven by the session object. TRADE initiator is **not** started. `35=D` count = 0.

Any slice FAIL leaves the increment **not accepted**. Partial demo remains the current tree.

---

## 1. Measured baseline (do not re-litigate)

Re-checked 2026-08-18 against the worktree. Stale A05/A07 “Class1 / 1 Hz loop” claims are ignored.

| Surface | Path | Measured now | Class |
|---|---|---|---|
| Persistence | `src/Infrastructure/Persistence/` | 20 `DbSet`s, inline fluent, **0** `Migrations/`, empty `Configurations/` | `EXISTS_NEEDS_REFACTOR` |
| Host schema | `apps/{api,mt5-worker,fix-worker}/Program.cs` | `EnsureCreatedAsync()` × 3 | `UNSAFE` |
| Default store | `DependencyInjection.cs` | empty / `<SECRET>` CS → `UseInMemoryDatabase` | `UNSAFE` |
| MT5 C# | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | **Only** `IMt5BrokerConnector`. `ConnectAsync` sets a bool. 4 logins, 18 canned deals | demo only |
| MT5 DI | `DependencyInjection.cs` L31–34 | **Always** `DemoBrokerFactory.CreateDefault()` | `UNSAFE` for live |
| MT5 worker | `apps/mt5-worker/Worker.cs` | 30 s fake sync; no RID; 0 vendor DLLs in `bin` | `MISSING` live |
| C++ SDK | `D:\Prop\mt5-sdk` | `MT5Manager` / `LoadLibraryW` preserved (C20). **Not** called by C# | `EXISTS_AND_GOOD` unused |
| FIX packages | `Fix.CTrader.csproj` | **No** `QuickFIXn.*`. Worktree dropped unofficial `QuickFix.Net` 1.8.0 | `MISSING` |
| FIX worker | `apps/fix-worker/Worker.cs` | 15 s stamp `ReadyForMarketData` / `LoggedOn`. **No socket** | `UNSAFE` health lie |
| Options | `CTraderFixOptions.cs` | Live host default; `RealCopyExecutionEnabled=false`; **not bound** by worker | `EXISTS_NEEDS_REFACTOR` |
| API | `apps/api/Program.cs` | 15 anonymous maps; CORS `*`; `POST /api/ops/resync` unaudited | `UNSAFE` |
| Identity | Domain / API / web | No `UserRole`, no login, no `[Authorize]` (C18) | `MISSING` |
| Audit | `AuditLog.cs` + `audit_logs` | Stub entity, **0 writers** | `EXISTS_NEEDS_REFACTOR` |
| Seeder | `DemoSeeder.cs` | Plants live hosts + QUOTE `ReadyForMarketData` + TRADE `LoggedOn` | `UNSAFE` |
| Dashboard | `EfDashboardQueries.GetBrokersAsync` | `Connected = true` literal | `UNSAFE` |
| §69 accepted | C13 | **0 / 12** | still 0 |
| §68 live | C14 | **0 / 19** | still 0 |
| §70 FIX | A101 | **0 / 14** | still 0 |

C42 remains the honesty pin: live Achiever / StarwaveFX are **not proven**.

---

## 2. What this increment is not

Do **not** pull these into C47. They are later increments or forbidden.

| Out | Why |
|---|---|
| TRADE FIX initiator / `CTraderTradeSession` live socket | Phase 7. QUOTE-only this increment (A30 I7). |
| `NewOrderSingle` / `35=D` / `35=F` / `35=G` | Phase 8. Safe by absence **and** by flag. |
| `REAL_COPY_EXECUTION_ENABLED=true` in any committed file | A49 / A101. |
| Full ~5k account census + checkpointed deal backfill | A30 I2–I3 remainder. C47 proves **connect + group walk + one real deal persist per broker**. |
| Pepperstone instrument discovery as a **gate** | §69.10 is a stretch after QUOTE Logon, not a C47 fail if Logon is proven. Never hardcode tag `55`. |
| Shadow-copy pipeline / `ShadowCopyEngine` wiring | A30 I8. Needs dest quotes first. |
| Full A61 43-table model / ML tables / `fix_orders` / `destination_positions` | Phase 7–8 / Phase 6. |
| `EMERGENCY_FLATTEN`, execution.enable, model.promote, identity admin API | A51 §14.2 later. |
| Kafka / K8s / ClickHouse / LLM | §71 / A80. |
| Rewrite `mt5-sdk` or P/Invoke `MT5APIManager64` from C# | C20 + A14: reuse C++ `IMT5Client`. |
| Linux `LoadLibrary` / Wine / copy DLLs into compose | A105 `UNSAFE`. |
| `TcpClient` FIX engine / `QuickFix.Net` / generic `FIX44.xml` only | A35 / A36. |
| Treating FakeMt5, `EnsureCreated`, or stamped `LoggedOn` as PASS | A100 vacuous law. |

---

## 3. Binding decisions (freeze before coding)

### 3.1 Slice order

```text
C47.1 EF migrations
   └─ C47.2 RBAC  (needs users / audit_logs / Migrate)
         ├─ C47.3 Windows MT5   ┐  may proceed in parallel after 47.1
         └─ C47.4 QUOTE logon   ┘  if 47.2 is in-flight; MUST NOT ship
                                    live sockets to an anonymous API
```

Ship rule: **no live collector or QUOTE process is pointed at a committed demo API** until C47.2 policies are on. Lab-only diagnostic sockets on a throwaway host are allowed for engineers, not for the shared dashboard.

### 3.2 MT5 transport (one path)

**Primary (binding for C47.3):**

```text
Windows x64  apps/mt5-collector  --broker=achiever   :9101
Windows x64  apps/mt5-collector  --broker=starwavefx :9102
        │                              │
        │  LoadLibraryW(MT5APIManager64.dll) + MT5Manager::Connect
        │  read-only HTTP (A16 literals)
        ▼                              ▼
C# apps/mt5-worker  (Windows preferred; HTTP client is portable)
        Mt5CollectorClient × 2   implements IMt5BrokerConnector
        Mt5BrokerSlotBinder      §56 keys only (A58)
```

Reasons:

- `mt5-sdk` `AppConfig` is **single-broker**. Two processes beat a C++ rewrite (C20).
- Architecture §5 / A105: native DLL is PE32+ AMD64. Collector **must** be Windows.
- A30 I2 already specified this sidecar. A16 already inventoried `/mt5/*`.
- C# does **not** reimplement Manager API and does **not** `DllImport` the native DLL this increment.

**Forbidden alternatives this increment:** Wine, Linux container + PE, pointing `Mode=remote` at a missing YoPips URL, a second `Mt5AchieverConnector` type.

### 3.3 FIX engine (one pin)

```xml
<PackageReference Include="QuickFIXn.Core" Version="1.14.1" />
<PackageReference Include="QuickFIXn.FIX44" Version="1.14.1" />
```

Data dictionary = vendored **cTrader** `FIX44-CSERVER.xml` (A36), **not** stock `FIX44.xml` alone. SHA-256 of the XML recorded at fetch.

`VenueMode`: `InProcess` (tests) | `LiveQuickFix` (staging QUOTE). Worker never registers the simulator (A68 / A101).

### 3.4 Auth shape

Cookie BFF (same-site React `:3000` → API `:5000`). Access cookie 15 min; refresh 12 h rotate. **No** public register. First SuperAdmin from user-secrets / OS secret store, never a committed password.

CORS: drop `AllowAnyOrigin`. Explicit Vite origin + `AllowCredentials`. C22 current `*` is incompatible with cookies.

### 3.5 Schema runner

```text
dotnet ef` against TraderIntelligence.Infrastructure
startup project = apps/api
folder = src/Infrastructure/Persistence/Migrations/
hosts call Database.MigrateAsync()  -- never EnsureCreated
tests: Testcontainers postgres:16
```

Keep `UseInMemoryDatabase` **only** for unit tests that opt in. Production / worker / API default: refuse to start if `ConnectionStrings:TraderIntelligence` is missing or contains `<SECRET>`.

### 3.6 Health honesty

| Today | C47 required |
|---|---|
| `GetBrokersAsync` `Connected = true` | `IMt5BrokerConnector.IsConnectedAsync()` |
| `/api/health` `"demo connector"` | per-broker collector `/mt5/health` + last error |
| FIX worker stamps `LastInboundAt` | only `IApplication.FromAdmin` / session object writes status |
| Seeder QUOTE `ReadyForMarketData`, TRADE `LoggedOn` | seed `Disconnected` (or omit TRADE row) |

---

## 4. C47.1 — EF migrations

### 4.1 Goal

Make PostgreSQL the schema authority. Name the unique indexes A20/A61 already require on tables that exist. Add identity + audit columns RBAC needs. Add the small live-connect tables QUOTE/MT5 need. **Do not** generate the full 43-table §45 model.

### 4.2 Work

| Step | Action |
|---|---|
| 1 | Add `TraderDbContextFactory` (`IDesignTimeDbContextFactory`). Env `TI_POSTGRES` / `ConnectionStrings__TraderIntelligence`. Placeholder CS only in `.env.example`. |
| 2 | Add `EFCore.NamingConventions` 8.0.3. `UseSnakeCaseNamingConvention()` + explicit `ToTable` (A61 §2.2). Pin `Npgsql` 8.0.4+ (do not leave transitive 8.0.3). |
| 3 | Extract inline fluent into `IEntityTypeConfiguration<T>` under `Persistence/Configurations/` binding **existing singular** Domain types (`Broker`, `Mt5Deal`, …). **Do not** restore HEAD plural `BrokersConfiguration` (B26 CS0246). |
| 4 | Name unique indexes: `mt5_accounts_identity_uk`, `mt5_deals_identity_uk`, `mt5_groups_broker_name_uk`, `brokers_code_uk`, `source_symbol_mappings_uk`, `trader_scores_uk`. Change `fix_sessions` unique from global `Qualifier` to `(venue_id, session_qualifier)` once `execution_venues` exists; until then keep one-row-per-qualifier but **stop claiming** TRADE is live. |
| 5 | Generate **new** migrations (do not hand-edit after apply): |

| Migration | Tables / deltas |
|---|---|
| `20260818C4701_StabilizeExistingModel` | Snapshot of the 20 current tables + named UKs + `HasDatabaseName` |
| `20260818C4702_IdentityAndAudit` | `users`, `user_sessions`, `step_up_challenges`; expand `audit_logs` toward A51 §8 (add missing columns; keep Guid PK this increment — bigint IDENTITY is a later alter, do not dual-PK); append-only trigger SQL |
| `20260818C4703_LiveConnectSupport` | `broker_connections` (no password columns); `fix_session_events`; `destination_symbols`; optional `system_events` |

Companion reviewed SQL under `Persistence/Sql/` (A30). Down scripts not required for prod rollback policy; Up must be versioned.

| 6 | Hosts: delete `EnsureCreatedAsync`. Call `MigrateAsync` then a **non-lying** seed (catalog brokers + `Disconnected` FIX rows + kill-switch `None`). Demo Fake deals seed **only** when `TI_SEED_DEMO=true` (default false on live). |
| 7 | Fail-closed DI: missing CS → process **does not start** (API / workers). Tests may still use InMemory via an explicit test host. |

### 4.3 Files

```text
src/Infrastructure/Persistence/TraderDbContextFactory.cs
src/Infrastructure/Persistence/Conventions/PgModelConventions.cs
src/Infrastructure/Persistence/Converters/PgUInt64.cs
src/Infrastructure/Persistence/Configurations/*Configuration.cs
src/Infrastructure/Persistence/Migrations/20260818C4701_*.cs
src/Infrastructure/Persistence/Migrations/20260818C4702_*.cs
src/Infrastructure/Persistence/Migrations/20260818C4703_*.cs
src/Infrastructure/Persistence/Sql/20260818C4701_*.sql
src/Infrastructure/Persistence/Sql/20260818C4702_*.sql
src/Infrastructure/Persistence/Sql/20260818C4703_*.sql
apps/{api,mt5-worker,fix-worker}/Program.cs   -- Migrate, no EnsureCreated
src/Infrastructure/DependencyInjection.cs     -- refuse empty CS
```

### 4.4 Tests (C47.1 exit)

```text
tests/Integration/Persistence/PostgresMigrationTests.cs
  - apply C4701–C4703 on empty Testcontainers postgres:16
  - second apply is no-op
  - unique (broker_id, deal_ticket) rejects a duplicate
  - UPDATE audit_logs fails (append-only trigger)
tests/Integration/Persistence/EnsureCreatedGoneTests.cs
  - grep-equivalent: product hosts must not call EnsureCreated (CI script or source assert)
```

**Exit:** `dotnet ef migrations list` shows three names; compose Postgres is created **only** by `Migrate`; InMemory is not the default host store.

---

## 5. C47.2 — RBAC (first useful only)

### 5.1 Goal

Architecture §59 four roles + A51 §14.1. Close C18. Gate every mapped route. Audit the three first-useful writes. **Do not** implement flatten / live-enable / model promote.

### 5.2 Roles and policies (verbatim A51)

```text
SuperAdmin ⊇ RiskManager ⊇ Analyst ⊇ ReadOnly

Authenticated, ReadOnlyPlus, AnalystPlus, RiskManagerPlus, SuperAdminOnly, AuditReader
```

Unauthenticated `/api/**` → **401**. Wrong role → **403**.

### 5.3 HTTP surface (this increment)

| Method | Path | Policy | Audit |
|---|---|---|---|
| POST | `/api/v1/auth/login` | anonymous | `auth.login.ok` / `auth.login.fail` (no password) |
| POST | `/api/v1/auth/refresh` | refresh cookie | reuse → revoke family |
| POST | `/api/v1/auth/logout` | Authenticated | `auth.logout` |
| GET | `/api/v1/auth/me` | Authenticated | no |
| GET | `/health` | anonymous | no |
| GET | `/ready` | anonymous (DB ping only) | no |
| GET | `/api/v1/**` reads | ReadOnlyPlus | no |
| PATCH | `/api/v1/mt5/groups/{id}` `enabledForAnalysis` | AnalystPlus | `group.analysis.toggle` |
| PATCH | `/api/v1/traders/{id}/state` Watch/Shadow/Paused/RiskBlocked | RiskManagerPlus | `trader.state.write` |
| POST | `/api/v1/risk/stop-new-execution` set/clear | RiskManagerPlus | `risk.stop_new.activate` |
| GET | `/api/v1/audit/logs` | AuditReader | no (row filter A51 §8.5) |
| POST | `/api/v1/risk/emergency-flatten` | **absent** (404) | — |
| POST | `/api/v1/execution/enable` | **absent** or 409 | — |
| POST | `/api/ops/resync` | **remove or SuperAdminOnly + audit** | must not stay anonymous |

Keep existing unversioned `/api/*` **either** redirected under `/api/v1` with the same policies **or** deleted. Do not leave a second anonymous surface.

`LIVE` / `LIVE_CANDIDATE` as a PATCH target while the copy flag is false → **409** + audit `failed`.

### 5.4 Files

```text
src/Domain/Enums/UserRole.cs
src/Domain/Entities/User.cs                 -- no password hash on domain
src/Domain/Entities/UserSession.cs
src/Domain/Entities/StepUpChallenge.cs      -- table now; consume later
src/Domain/Entities/AuditLog.cs             -- expand toward A51 §8.2
src/Infrastructure/Identity/PasswordHasher.cs          -- argon2id
src/Infrastructure/Identity/EfUserStore.cs
src/Infrastructure/Audit/EfAuditWriter.cs              -- same Tx as mutation
src/Infrastructure/Audit/AuditRedactor.cs
apps/api/Auth/AuthEndpoints.cs
apps/api/Auth/DashboardAuthExtensions.cs    -- cookie + policies
apps/api/Auth/SeedSuperAdmin.cs             -- user-secrets
apps/web/src/pages/LoginPage.tsx
apps/web/src/auth/AuthProvider.tsx
apps/web/src/auth/RequireAuth.tsx
apps/web/src/auth/RequireRole.tsx
apps/web/src/auth/can.ts
apps/web/src/api/client.ts                  -- withCredentials
apps/web/src/App.tsx                        -- login route; guard dashboard
apps/web/src/layouts/DashboardLayout.tsx    -- hide Audit from ReadOnly/Analyst
```

Packages (API): `Microsoft.AspNetCore.Authentication.Cookies` (inbox) or JWT bearer if a later review picks bearer — **pick cookies this increment** (React same-site). Add `Konscious.Security.Cryptography.Argon2` (or the reviewed argon2id library). Do **not** add Identity UI / EF Identity schema (A51 is a four-role table, not ASP.NET Identity).

CORS change is **in this slice** (C22).

### 5.5 Tests (C47.2 exit)

Minimum of A51 §15 (do not mark RBAC done on one `[Authorize]`):

```text
tests/Unit/Auth/RolePolicyMatrixTests.cs
tests/Unit/Auth/AuditRedactorTests.cs
tests/Integration/Auth/LoginMeLogoutTests.cs
tests/Integration/Auth/AnonymousApiIs401Tests.cs
tests/Integration/Auth/TraderStateWriteAuditedTests.cs
tests/Integration/Auth/StopNewExecutionAuditedTests.cs
tests/Integration/Auth/AuditGetReadOnlyIs403Tests.cs
tests/Integration/Auth/LastSuperAdminDemoteRejectedTests.cs
```

**Exit:** curl without cookie → 401 on `/api/v1/overview`. RiskManager can set stop-new; Analyst cannot. One SuperAdmin exists that is **not** in git.

---

## 6. C47.3 — Windows live MT5 connect

### 6.1 Goal

Prove **both** Manager sessions. Close C42 as a *coding* target (the honesty pin stays until evidence exists). This slice is **connect + discover groups + persist at least one real deal ticket per broker**. It is **not** the 5k backfill.

### 6.2 Collector (Windows)

New C++ host linking `mt5sdk::mt5sdk`. Reuse `IMT5Client` / `MT5Manager` / `MT5Pool` / `MT5Watchdog` (A14, A15). Call `mt5sdk_copy_runtime_dlls` (A105 trio):

```text
MT5APIManager64.dll
MetaQuotes.MT5ManagerAPI64.dll
MetaQuotes.MT5CommonAPI64.dll
```

from `mt5-sdk/vendor/MetaTrader5SDK/Libs`. Absolute `dllPath`. Log loaded path + SHA-256 + `MTManagerAPIVersion` at start (A56 R11).

One binary, `--broker=` selects which §56 slot to read (process env). Two services / two windows:

```text
mt5-collector --broker=achiever    --http 127.0.0.1:9101
mt5-collector --broker=starwavefx  --http 127.0.0.1:9102
```

Read-only routes (A16 literals + A30 I2 adds). **Refuse** dealer / provision / password / balance POSTs with 405.

Minimum routes for C47.3:

```text
GET /mt5/health
GET /mt5/groups
GET /mt5/groups/count
GET /mt5/groups/{name}/logins
GET /mt5/users/{login}
GET /mt5/accounts/{login}
GET /mt5/accounts/{login}/positions
GET /mt5/accounts/{login}/deals?from=&to=
```

`X-API-Key` required. Group discovery = `GroupTotal` / `GroupNext` / `GroupGet`. **Do not** filter by `MT5_GROUP_*` (A39/A40/C10).

Achiever proxy from `ACHIEVER_PROXY_*` when enabled. StarwaveFX `PROXY_ENABLED=false` still uses the same code path.

### 6.3 C# adapter

```text
src/Mt5/Http/Mt5CollectorClient.cs          -- IMt5BrokerConnector
src/Mt5/Http/Mt5CollectorOptions.cs
src/Mt5/Http/Mt5Json.cs                     -- A13 snake_case; Volume() = / 10_000 lots
src/Mt5/Registry/Mt5BrokerSlotBinder.cs     -- §56 names only (A58)
src/Mt5/Registry/Mt5BrokerRegistry.cs       -- replace dictionary stuffed in Fake file
src/Mt5/DependencyInjection.cs
```

Factory:

```text
TI_MT5_TRANSPORT=fake | live
```

- `fake` (CI default): existing `FakeMt5BrokerConnector`.
- `live`: two `Mt5CollectorClient` instances. Missing password / collector health fail → **process fail-closed**, dashboard `Connected=false`.

Do **not** invent `Brokers:Achiever:CollectorBaseUrl` as a *required* §56 key. Bind loopback ports from a **non-secret** worker section `Mt5Collectors:Achiever:BaseUrl` (local process topology, not a broker secret). A58 forbids inventing *broker* keys; localhost collector URLs are deployment, not venue credentials. Document them in `.env.example` as `TI_MT5_ACHIEVER_COLLECTOR=http://127.0.0.1:9101`.

Delete or isolate dead `IBrokerConnector` (B24). Do not ship two ports.

`Mt5BrokerOptions.Mode` default `"remote"` contradicts §7 `local`. Collector is `local` Manager + HTTP to C#. Rename in comments: C# talks HTTP to a **local-mode** sidecar.

### 6.4 Worker / dashboard

| Change | Detail |
|---|---|
| `TraderIntelligence.Mt5Worker.csproj` | `RuntimeIdentifier=win-x64` when publishing the **collector host** docs; C# worker itself is net8 portable HTTP. Publish notes: collector is the Windows RID artifact. |
| `Worker.cs` | Call `ConnectAsync` once; `IsConnectedAsync` each cycle; stop hard-coding logins `10001…` when `live` — score logins that exist in `mt5_accounts` (cap N this increment, e.g. 50, not 5k). |
| `DealIngestionService` | Keep one loop over `registry.All()`. Persist groups/accounts/deals via existing upserts. Write `sync_checkpoints` for the deals stream (entity exists, unused). |
| `EfDashboardQueries.GetBrokersAsync` | `Connected` from registry. `LastEventAt` from last persisted deal/account, not `UtcNow`. |
| `/api/health` | Per-broker collector health JSON. No `"demo connector"` when `live`. |
| Compose | **Still no** mt5-worker/collector on Linux (C12). Comment stays. |

### 6.5 Secrets

Passwords only in user-secrets / env. `DemoSeeder` may keep **non-secret** host/login numbers as catalog (already there) but must not be treated as a connection. A75: `.env.example` uses `<SECRET>` sentinels.

### 6.6 Tests + live probe

```text
tests/Unit/Mt5/Mt5JsonMappingTests.cs
tests/Unit/Mt5/Mt5BrokerSlotBinderTests.cs          -- §56 names; no invented keys
tests/Integration/Mt5/FakeStillDefaultInCiTests.cs
tests/Integration/Mt5/LiveConnectProbe.md           -- operator runbook, not CI
```

**Live probe (Windows lab, not CI, not committed output):**

```text
1. copy-dlls beside mt5-collector.exe; SHA-256 match A105 §2.1
2. start two collectors with user-secrets
3. GET /mt5/health → connected=true on both
4. GET /mt5/groups → count > canned 3+1
5. C# worker TI_MT5_TRANSPORT=live against compose Postgres
6. mt5_groups rows for both broker_id; at least one mt5_deals ticket
   that is not 10501 / 10502 / … canned set
7. kill one collector: other broker stays Connected; dashboard matches
```

Record command, timestamp, hashes, exit codes under `reports/swarm/` (not product). **Do not** commit manager passwords or deal payloads with customer names.

**Exit:** C42’s sentence “C# cannot talk to MT5” becomes false **with a probe log on disk**. Until that log exists, C47.3 is not PASS.

---

## 7. C47.4 — QuickFIXn net8 QUOTE logon

### 7.1 Goal

Independent QUOTE TLS session reaches `LOGON_OK` and stays there across Heartbeat / a clean reconnect (`141=Y`). Kill the 15 s stamp. TRADE session object may exist as a **type** but the worker **must not** start a TRADE initiator.

This satisfies the *start* of §69.9 and A101 item 1 **for QUOTE only**. A101 item 1 as written is TRADE Logon — **do not tick A101.1**. Tick a C47-specific QUOTE box instead.

### 7.2 Packages and dictionary

- Add A35 pair 1.14.1 to `TraderIntelligence.Fix.CTrader.csproj` only.
- Vendor `Spec/FIX44-CSERVER.xml` (fetch official URL; record SHA-256 + date). Point `DataDictionary=` at the project copy, not the NuGet cache.
- Copy XML to output.
- `BeginString=FIX.4.4`. `SSLEnable=Y`. `SocketConnectHost` / `SocketConnectPort=5211`.
- `UseSSL=true` default. `98=0`.
- `TargetCompID` configurable; default issued `cServer`; **no silent case fold** (C09/C21). Persist exactly what was sent.

Layering: move FIX **ports** into Application (`IFixQuoteSession`). `Fix.CTrader` implements them and should stop depending on Application if that creates a cycle — host (`fix-worker`) composes both. Do not put Redis in `Fix.CTrader` (C27). QUOTE lease can stay process-local this increment; document A46 as follow-on before a second worker is deployed.

### 7.3 Session

```text
src/Application/Ports/Fix/IFixQuoteSession.cs
src/Fix.CTrader/Sessions/CTraderQuoteSession.cs
src/Fix.CTrader/Sessions/CTraderFixSessionSettingsFactory.cs
src/Fix.CTrader/Sessions/CTraderQuoteApplication.cs     -- QuickFix.IApplication
src/Fix.CTrader/Headers/CTraderHeaderOptions.cs
src/Fix.CTrader/Logging/FixLogRedactor.cs               -- tags 553/554 → ***
src/Fix.CTrader/DependencyInjection.cs
apps/fix-worker/Hosting/QuoteSessionHost.cs
apps/fix-worker/Worker.cs                               -- host session; delete stamp loop
```

Logon fields (A25 / RoE):

```text
35=A
49 = CTRADER_FIX_QUOTE_SENDER_COMP_ID
56 = CTRADER_FIX_QUOTE_TARGET_COMP_ID   (issued case)
57 = QUOTE
50 = CTRADER_FIX_QUOTE_SENDER_SUB_ID    (issued; default QUOTE if form says so)
98=0  108=30  141=Y
553 = numeric login
554 = secret (never logged, never DTO)
```

Persist `fix_session_events` `LOGON_OK` / `LOGON_REJECT` / `LOGOUT` with exact 49/50/56/57.

Flags (A49 names):

```text
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=false
CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=true
REAL_COPY_EXECUTION_ENABLED=false
VenueMode=LiveQuickFix | InProcess
```

Construction guard: `VenueMode=InProcess` + host `*.c-trader.com` → refuse (tests).

Do **not** send `35=V` / `35=x` as a C47 **gate**. After Logon, Heartbeat / TestRequest / Logout are enough. SecurityList (`35=x`) is a **stretch** that may persist `destination_symbols` if it lands; never write `55=123456` (A86).

### 7.4 Worker / seeder / dashboard

- Delete `LastInboundAt = UtcNow` and `Status = ReadyForMarketData` assignments.
- Seeder: QUOTE `Disconnected`; do **not** insert TRADE `LoggedOn`. Prefer omit TRADE row until Phase 7.
- `QuoteHealthy` = session `LoggedOn` **and** (if any quote exists) age ≤ configured max. Logged-on ≠ fresh (A72).
- Bind `CTraderFixOptions` from env / `CTrader` section. Empty password → do not start LiveQuickFix (fail-closed).
- Sequence files under a gitignored store path (A103).

### 7.5 Tests

```text
tests/Fix/TraderIntelligence.Tests.Fix.csproj          -- new, InProcess only
tests/Fix/QuickFixnPackagesAreOfficialTests.cs         -- csproj pin 1.14.1
tests/Fix/QuoteLogonInProcessTests.cs
tests/Fix/QuoteDoesNotExposeNewOrderSingleTests.cs
tests/Fix/FixAdapterTestModeDoesNotHitVenueTests.cs
tests/Fix/FixLogRedactorTests.cs
tests/Unit/Fix/CServerCaseIsNotFoldedTests.cs
```

`FixSimulationHarness` stays a fixture builder; do not register it in the worker.

**Live diagnostic (optional, still no orders):** `VenueMode=LiveQuickFix`, quote flag true, trade flag false, copy flag false, password from secrets. After `LOGON_OK`, Heartbeat, Logout. Persist event. **Refuse** `35=D/F/G/V` at the application.

**Exit:** InProcess QUOTE Logon green in CI. If a live diagnostic is run, `fix_session_events` has `LOGON_OK` with case-preserved headers. Dashboard cannot show QUOTE healthy from a timer. `35=D` count = 0.

---

## 8. File / package checklist (create or change)

### 8.1 Create

| Slice | Paths |
|---|---|
| 47.1 | factory, conventions, converters, `Configurations/*`, `Migrations/*`, `Sql/*` |
| 47.2 | `UserRole`, `User`, `UserSession`, `StepUpChallenge`, hasher, stores, audit writer, API Auth/*, React Login/Auth* |
| 47.3 | `apps/mt5-collector/**` (CMake), `src/Mt5/Http/*`, `src/Mt5/Registry/Mt5BrokerSlotBinder.cs` |
| 47.4 | `QuickFIXn.*` refs, `Spec/FIX44-CSERVER.xml`, `Sessions/CTraderQuote*`, `tests/Fix/*` |

### 8.2 Change

| Path | Change |
|---|---|
| `src/Infrastructure/DependencyInjection.cs` | Npgsql+Migrate; refuse empty CS; slot binder; stop always-fake when `live` |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | honest Connected / QuoteHealthy |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | Disconnected FIX; demo deals gated |
| `apps/api/Program.cs` | auth, CORS, `/api/v1`, no EnsureCreated, no anonymous resync |
| `apps/api/appsettings.json` | empty password stays; `RealCopyExecutionEnabled=false`; no new secrets |
| `apps/mt5-worker/Worker.cs` | live registry; no 4-login hardcode when live |
| `apps/fix-worker/Worker.cs` | QuoteSessionHost; delete stamp |
| `apps/fix-worker/appsettings.json` | bind CTrader flags; trade session false |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | QuickFIXn 1.14.1 pair |
| `apps/web/src/App.tsx` | Login + RequireAuth |
| `docker-compose.yml` | still Postgres+Redis+optional Linux API; **no** collector image |
| `Mt5TraderIntelligence.sln` | add `tests/Fix` if created; collector is CMake, not csproj |

### 8.3 Do not restore

HEAD `QuickFix.Net 1.8.0` and HEAD plural `BrokersConfiguration` / `Mt5DealsConfiguration`.

---

## 9. Quality loop (mandatory per slice)

```text
CODER → REVIEWER (unbiased) → [fix] → REVIEWER → TEST
  on TEST FAIL: RESEARCHER (docs / RoE / SDK) → CODER → REVIEWER → TEST
PASS+PASS = slice DONE
```

| Slice | Reviewer looks for | Test command |
|---|---|---|
| 47.1 | no EnsureCreated; named UKs; no password columns | `dotnet test` Integration Persistence + Testcontainers |
| 47.2 | 401/403 matrix; same-Tx audit; CORS not `*` | Auth unit + integration |
| 47.3 | one connector type; read-only collector; copy-dlls trio; no Linux PE | mapping tests + live probe log |
| 47.4 | official packages; CSERVER XML; no TRADE start; redaction | `tests/Fix` + package assert |

Do not merge a slice on “it compiles.”

---

## 10. Scoreboard impact (honest)

| Bar | Now | After C47 if **all** exits measured | Still not |
|---|---|---|---|
| §69 accepted | 0/12 | **at most 2/12** (items 1 and 9) if live probe + QUOTE Logon logs exist. Item 2 may become PARTIAL. Items 3–8, 10–12 stay DEMO/FAIL/PARTIAL. | 5k, discovery, shadow, honest React of all 12 |
| §68 G01 | FAIL | PARTIAL at best (connect + some deals ≠ “stable ingestion”) | leave `[ ]` |
| §68 G05 | FAIL | PARTIAL (QUOTE Logon ≠ “quote session stable” without MD + age) | leave `[ ]` |
| §68 G06–G19 | FAIL | still FAIL | — |
| §70 | 0/14 | still 0 (item 1 is TRADE) | — |
| C18 RBAC | MISSING | first-useful **implemented** if tests green | flatten / enable |
| C19 QuickFIX | not referenced | 1.14.1 referenced **and used** | TRADE |
| C29 migrations | 0 | 3 versioned | full A30 0001–0015 |
| C42 live MT5 | NOT PROVEN | proven only with probe artifact | CI cannot prove brokers |

**Do not edit A100/A101 checkboxes from this file.** A later dated successor flips boxes with test class, command, timestamp, SHA-256.

---

## 11. Anti-greenwash (reviewer reject list)

| Claim after C47 | Reject unless |
|---|---|
| “Migrations done” | `Migrations/` on disk **and** Testcontainers apply **and** hosts call `Migrate` |
| “RBAC done” | 401 on anonymous overview **and** audit row on stop-new |
| “Achiever connected” | collector log `Connect` OK **and** `IsConnectedAsync` true **and** dashboard matches **and** probe file exists |
| “QUOTE logged on” | `fix_session_events.LOGON_OK` from `IApplication` **and** worker no longer writes `LastInboundAt` on a timer |
| “We used QuickFIX” | `using QuickFix` + `SocketInitiator` **and** `deps.json` lists `QuickFIXn.Core` 1.14.1 |
| “First useful version” | still **no** — 12/12 not met |
| “Go-live” | still **no** — 0/19 live |
| “Safe to send 35=D” | still **no** |

---

## 12. Suggested coding waves (after this report)

One increment, four PRs / four review cycles:

```text
PR C47.1  migrations + fail-closed CS + seeder honesty
PR C47.2  cookie auth + /api/v1 + first-useful writes + React Login
PR C47.3  mt5-collector + slot binder + honest broker health
PR C47.4  QuickFIXn QUOTE + delete stamp loop
```

47.3 and 47.4 may overlap after 47.1 lands. 47.2 should merge before operators are pointed at live sockets.

Operator lab on this Windows box:

```text
docker compose up -d postgres redis
dotnet ef database update --project src/Infrastructure --startup-project apps/api
# user-secrets: MT5_* passwords, CTRADER_FIX_PASSWORD, TI_SUPERADMIN_PASSWORD
# two collectors (Windows)
# mt5-worker TI_MT5_TRANSPORT=live
# fix-worker VenueMode=LiveQuickFix QUOTE only
# api + web
```

---

## 13. Residual risks

1. **Manager IP allowlist** (`MT_RET_AUTH_MANAGER_IPBLOCK` = 1012). Achiever egress `81.29.145.69` (C55, non-secret). Lab machine must egress that path or Connect fails. That is an ops fact, not a code defect.
2. **Two manager slots.** Collectors consume two licenses. Do not also run YoPips / probes against the same login.
3. **cServer vs CSERVER.** Only a diagnostic Logon resolves it. No silent fold.
4. **EnsureCreated leftovers** on a dev laptop that already has an InMemory-shaped local Postgres will not match migrations. Document “drop dev DB once.”
5. **Cookie + CORS.** Forgetting to drop `*` leaves auth unshippable (C22).
6. **Fake default in CI** must remain so GitHub/Linux agents never need the PE.
7. **A26 vs A51 flatten-role conflict** is irrelevant this increment (no flatten endpoint).

---

## 14. Sources (read, not modified)

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5–8, 10, 25–31, 41, 45, 55–59, 67–72
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`, `Persistence\TraderDbContext.cs`, `Seeding\DemoSeeder.cs`, `Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`, `Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`, `Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`, `apps\mt5-worker\Worker.cs`, `apps\fix-worker\Worker.cs`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`, `CMakeLists.txt`, `config\app_config.h`
- `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md`, `A35_quickfixn_packages.md`, `A36_ctrader_data_dictionary.md`, `A51_rbac_audit.md`, `A58_broker_registry.md`, `A61_efcore_schema.md`, `A105_windows_dlls.md`
- `D:\Prop\reports\swarm\20260818\C13_fuv_scorecard.md`, `C14_golive_still_fail.md`, `C18_rbac_missing.md`, `C19_quickfix_not_wired.md`, `C20_sdk_preserved.md`, `C22_cors.md`, `C29_migrations_gap.md`, `C42_honesty_no_live_mt5.md`

---

## 15. One-page operator view

```text
C47 I-Live-Foundation                         2026-08-18  PLAN ONLY
=========================================================
C47.1  EF migrations (3 versioned)            TODO
C47.2  RBAC first-useful + audit writer       TODO
C47.3  Windows collectors + both MT5 Connect  TODO
C47.4  QuickFIXn 1.14.1 QUOTE TLS Logon       TODO
---------------------------------------------------------
§69 accepted after success                    ≤ 2 / 12
§68 live-copy license                         still 0 / 19
§70 TRADE acceptance                          still 0 / 14
REAL_COPY_EXECUTION_ENABLED                   false
Live 35=D                                     forbidden
mt5-sdk rewrite                               forbidden
EnsureCreated                                 must die
Fake connector in CI                          stays
=========================================================
```

**Bottom line:** the next increment is not more demo pages. It is versioned Postgres, a real login, two Windows Manager sessions, and one official QuickFIX/n QUOTE Logon — in that dependency order — with honest health and no TRADE send path.

*End of C47. Product source was not modified.*
