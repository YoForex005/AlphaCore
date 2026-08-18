# D85 — Next increment ordered: I1 schema authority (then I4 P0 lock, then C47 remainder)

| Field | Value |
|---|---|
| Agent | D85 (senior engineer, increment design only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (worktree recensus after D-wave; hashes in §2) |
| Artifact | `D:\Prop\reports\swarm\20260818\D85_next.md` |
| Assigned | Propose next increment ordered. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only product of this agent. |
| Authority | Architecture v2 §§10–13, 45, 60, 67–73; A30 I1 leftover; A61; A72.3 |
| Binding siblings | A20, A21, A22, A30, A35, A51, A61, A100, A101, C13, C14, C29, C32, C47, C54, D11, D19, D32, D41, D42, D44, D45, D46, D51, D52, D53 |
| Supersedes as *immediate* next increment | `C47_next_increment.md` (C47 remains the **successor** live-foundation increment, not this one) |
| Classification | PLAN — not implemented |

**Honesty line:** this increment is **schema authority + health honesty**. It does **not** produce a first useful version (D41 still **0/12**), does **not** flip any §68 go-live box (D42 still **0/19**), does **not** add QuickFIX/n, does **not** open Achiever / StarwaveFX / Pepperstone sockets, and does **not** authorize `REAL_COPY_EXECUTION_ENABLED=true` or any `35=D`.

```text
REAL_COPY_EXECUTION_ENABLED = false     -- stays false in every committed config
CTRADER_FIX_TRADE_SESSION_ENABLED       -- stays unused / false
Live NewOrderSingle (35=D)              -- FORBIDDEN
C++ mt5-sdk                             -- reuse, do not rewrite (C20)
Product source in this agent            -- not touched
C47 I-Live-Foundation                   -- NOT started this increment
```

---

## 0. Verdict

**Next increment id: `D85` / `I1-Schema-Authority`.**

Do **not** start C47.3 Windows collectors or C47.4 QuickFIXn QUOTE Logon now. Do **not** start C47.2 RBAC now. C47 bundled four independent risk classes into one increment; D-wave measured that **none** of them landed, while I1 (A30, C29, D51) is still **0/15 migrations** and the demo reconstructor still has **P0 money bugs** (D11). Live ingest through that book would persist **wrong first-3** and **wrong SHADOW** onto durable tickets.

One increment, five slices, **strict internal order**:

| Slice | Name | Unlocks | Maps to |
|---|---|---|---|
| **D85.0** | Freeze the compile contract | Stop mid-swarm entity/store flux (D37/D42 CS8858 / missing members) | D37, D42 |
| **D85.1** | Split fluent maps + named UNIQUEs + snake_case | A61 shape on the **existing** 20 tables | A20, A61, D19 |
| **D85.2** | One versioned migration `20260818D8501_StabilizeExistingModel` | PostgreSQL is schema authority | A30 I1 leftover, C29, D51 |
| **D85.3** | Hosts: `MigrateAsync`; delete `EnsureCreated`; fail-closed CS; `TI_SEED_DEMO=false` | Empty Postgres is a real DB; canned first-3 is opt-in | C47.1 §3.5 (kept), D22/D41 |
| **D85.4** | Remaining health honesty | Dashboard / `/api/health` stop claiming venue truth | D21, D32, D41, D45 |

**Why this, now:** Domain types, FakeMt5 demo ingest, 15 React pages, and a more honest FIX `Disconnected` stamp already exist (D32, D41). The first unimplemented A30 increment is still **I1**. C47 was right that sockets on `EnsureCreated` + InMemory + anonymous `POST /api/ops/resync` is how the lab greenwashes. C47 was **wrong** to pull RBAC + two live transports into the same increment as the first migration. D11/C32 add a new sequencing constraint: **do not attach live venues until the reconstructor and scorer P0 holes are locked** (that is increment **D86**, immediately after D85 — see §8).

**What “done” means for D85 (conjunction):**

1. Empty Postgres 16 applies **one** versioned migration. `dotnet ef migrations list` shows `20260818D8501_StabilizeExistingModel`. `__EFMigrationsHistory` has one row.
2. All three hosts call `Database.MigrateAsync()`. Product C# contains **zero** `EnsureCreated` / `EnsureCreatedAsync`.
3. Missing / `<SECRET>` connection string → API and workers **do not start**. InMemory is test-only (explicit test host).
4. Unique `(broker_id, deal_ticket)` **rejects** a duplicate on Testcontainers Postgres (not EF InMemory).
5. `/api/health` does not report MT5 `healthy: true` for the Fake connector as if it were Manager. `GetBrokersAsync` does not hard-code `Connected = true`. `outboxBacklog` is a `COUNT`, not a literal `0`. `/api/reconciliation/status` does not invent a just-now run.
6. `TI_SEED_DEMO` default **false**. Catalog brokers + `Disconnected` FIX rows may seed; canned deals / invented dest quote / demo shadow rows **do not**, unless the flag is on.

Any slice FAIL leaves the increment **not accepted**. Partial demo remains the current tree.

---

## 1. Why C47 is not the immediate next increment

C47 (`I-Live-Foundation`) proposed four slices in one increment: migrations, RBAC, Windows collectors, QuickFIXn QUOTE Logon. That plan is **still correct as a successor**. It is **not** the next coding increment.

| C47 claim | D-wave / recensus | Consequence |
|---|---|---|
| Start live sockets in the same increment as the first migration | D51: **0** `Migrations/` folders, **0** `Migrate` call sites, **3** `EnsureCreatedAsync` | I1 is unfinished. Sockets wait. |
| Domain algorithms are good enough to ingest live | D11: P0 INOUT money double-count, reverse discards, overclose clip, same-sign phantom complete, no ticket idempotency. C32: winning martingale scores **70.25–85.25 SHADOW**. D73: scoring **ignores** `EligibleForFirstThree`. D44: `DealReason` never persisted (null counts as trading). | Live tickets through this book are **harmful**. |
| FIX worker still stamps `LoggedOn` | D32 / D41: worker + seeder now persist **`Disconnected`**. `/api/health` QUOTE `healthy: false`. | Honesty on FIX **started** without C47. Do not re-open that fight. |
| Bundle RBAC with I1 | D53: still **no** auth. Anonymous `POST /api/ops/resync` is unsafe **when live data exists**. D85 does not attach live data. | RBAC is D87, before D88 sockets, after schema. |
| Four PRs, one increment | None of the four landed. Mid-swarm Infrastructure compile went **RED** (D37/D42). | Shrink. One risk class per increment. |

**Keep from C47 (binding, not re-litigated):**

- Official `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** only (A35, D52). Never restore HEAD `QuickFix.Net` 1.8.0.
- Windows collectors wrap preserved `IMT5Client` / `MT5Manager`. No C# `DllImport` of `MT5APIManager64.dll`. No Linux PE (D63).
- `TargetCompId` stays issued `cServer` with **no silent case fold** (D26).
- `REAL_COPY_EXECUTION_ENABLED=false`. TRADE initiator not started. `35=D` count = 0.
- Fake connector stays the **CI default**.

---

## 2. Measured baseline (do not re-litigate)

Re-checked 2026-08-18 against the worktree. Stale A01/A07 “Class1 / 1 Hz loop / LoggedOn every 15 s / shadow unused” claims are ignored. Use D32 / D41 / D45 / D47 / D48 / D51 / D52 / D53 for those cells.

| Surface | Path | Measured now | Class |
|---|---|---|---|
| Persistence | `src/Infrastructure/Persistence/` | 20 `DbSet`s, inline fluent, **0** `Migrations/`, empty `Configurations/` | `EXISTS_NEEDS_REFACTOR` |
| Host schema | `apps/{api,mt5-worker,fix-worker}/Program.cs` | `EnsureCreatedAsync()` × **3**. `Migrate*` × **0** | `UNSAFE` |
| Default store | `DependencyInjection.cs` | empty / `<SECRET>` CS → `UseInMemoryDatabase("trader-intelligence")` | `UNSAFE` |
| §45 coverage | D19 | **18/43** tables by name; 25 missing; 0 named UNIQUEs (`HasDatabaseName`); 0 FKs | demo skeleton |
| MT5 C# | `FakeMt5BrokerConnector` | **Only** `IMt5BrokerConnector`. DI **always** `DemoBrokerFactory.CreateDefault()` | demo only |
| MT5 worker | `apps/mt5-worker/Worker.cs` | 30 s Fake sync of **4 hard-coded logins**; 30-day host-clock window; **0** checkpoint writes (D46) | `MISSING` live |
| C++ SDK | `D:\Prop\mt5-sdk` | `MT5Manager` preserved (C20). `MT5HttpClient::GetGroupDetails` hard-false stub (D67) | unused by C# |
| FIX packages | `Fix.CTrader.csproj` | **No** `PackageReference`. Official QuickFIX/n **never** referenced (D52) | `MISSING` |
| FIX worker | `apps/fix-worker/Worker.cs` | 15 s overwrite to **`Disconnected`**. No socket (D32) | honesty, not a session |
| Seeder | `DemoSeeder.cs` | FIX rows `Disconnected` (D22 **stale**). Still seeds invented dest quote `2399.45/2399.85`, `VenueInstrumentId = null` | `EXISTS_NEEDS_REFACTOR` |
| Dashboard lie | `EfDashboardQueries.GetBrokersAsync` | `Connected = true` **literal**. Overview `mt5Healthy = brokers > 0` | `UNSAFE` |
| API health | `apps/api/Program.cs` | MT5 `healthy: true` + “demo Fake…” footnote; QUOTE `healthy: false`; `outboxBacklog = 0` literal (D45: seed writes 4) | `UNSAFE` |
| RBAC | D53 | 15 anonymous maps; CORS `*`; `POST /api/ops/resync` unaudited | `MISSING` |
| Outbox | D45 | **1** writer (`PersistDemoShadowAsync` → `ScoreUpdate`); **0** drain; not same-TX as deals | souvenir, not §12 |
| Checkpoints | D46 | entity + unique exist; **0** `new SyncCheckpoint` | `MISSING` writer |
| Reconstructor | D11 | happy-path netting book; **6 P0** holes; 6/6 unit facts do not cover them | `UNSAFE` on reverse |
| Scorer | C32 / D12 | `CanPromoteToLive` hard-false (good). Winning martingale can be **SHADOW** | `UNSAFE` vs A22 |
| Shadow | D16 / D48 | engine called from store; **6** demo rows from invented quote; not A24 | `DEMO` |
| Plan filter | D68 | ingestion is **not** plan-filtered (required §7/§9 shape) | keep |
| Flag | D69 | `RealCopyExecutionEnabled` default **false** | keep |
| Compose | D63 | postgres + redis + Linux api; **no** mt5-worker | keep |
| §69 accepted | D41 | **0 / 12** | still 0 |
| §68 live | D42 | **0 / 19** | still 0 |
| §70 FIX | D43 | **0 / 14** | still 0 |

### 2.1 Evidence hashes (this recensus)

| SHA-256 | Path |
|---|---|
| `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `src/Infrastructure/Persistence/TraderDbContext.cs` (5951 B; same as D19/D51) |
| `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | `src/Infrastructure/Persistence/EfTradingStore.cs` (12097 B) |
| `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `src/Infrastructure/DependencyInjection.cs` |
| `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `src/Infrastructure/Seeding/DemoSeeder.cs` |
| `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` |
| `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `apps/api/Program.cs` |
| `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | `apps/mt5-worker/Program.cs` |
| `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | `apps/mt5-worker/Worker.cs` |
| `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `apps/fix-worker/Program.cs` |
| `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | `apps/fix-worker/Worker.cs` |
| `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` |
| `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` |
| `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` | `src/Domain/Reconstruction/TradeReconstructor.cs` |
| `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | `src/Domain/Scoring/BaselineScorer.cs` |
| `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `docker-compose.yml` |

Grep of `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests` product `*.cs` / `*.csproj` (this pass): `EnsureCreated` **3**; `MigrateAsync` / `Database.Migrate` **0**; `QuickFIXn` / `QuickFix` / `SocketInitiator` **0**; `AddAuthentication` / `MapHub` **0**.

`Test-Path` `Persistence/Migrations` = **False**. `Persistence/Sql` = **False**. `Persistence/Configurations/` = empty directory.

---

## 3. Ordered increment sequence (binding)

Do these **in this order**. Do not start a later increment until the prior increment’s exit is measured.

```text
D85  I1-Schema-Authority          ← THIS increment
        85.0 compile freeze
        85.1 split configs + named UKs + snake_case
        85.2 migration 20260818D8501
        85.3 Migrate / no EnsureCreated / fail-closed CS / TI_SEED_DEMO=false
        85.4 health honesty
        ── exit: Testcontainers apply + unique reject + hosts grepped clean

D86  I4-P0-Lock                   ← next increment (algorithms; no sockets)
        86.1 recon D11 B1–B6 + dirty/RECON_* channel
        86.2 scoring reads EligibleForFirstThree (D73)
        86.3 A22 martingale cap (C32); no SHADOW with FLAG_MARTINGALE
        86.4 persist DealReason on mt5_deals + DTO (D44)
        86.5 A21 fixtures F01–F05 + F17 minimum
        ── exit: measured gold on reverse/INOUT; winning 2× martingale is not SHADOW

D87  I-RBAC-FirstUseful           ← C47.2, unchanged in spirit
        cookie BFF; 401 on anonymous /api/** (except /health /ready /login)
        audit writer same-Tx; drop CORS *
        remove or SuperAdmin-gate POST /api/ops/resync
        ── exit: A51 §14.1 tests; no flatten / execution.enable

D88  I2-Live-MT5-Connect          ← C47.3 / A30 I2 start
        Windows mt5-collector ×2; C# Mt5CollectorClient; TI_MT5_TRANSPORT=fake|live
        connect + group walk + ≥1 real deal ticket per broker
        ── NOT 5k, NOT checkpoints-complete (that is D90)
        ── exit: probe log on disk; C42 stays until that log exists

D89  I7-QUOTE-Logon               ← C47.4 / A30 I7 start
        QuickFIXn 1.14.1 QUOTE TLS 5211; delete 15 s stamp loop
        TRADE initiator not started; 35=D count = 0
        SecurityList persist is stretch, not a D89 fail
        ── exit: InProcess Logon green; live diagnostic optional

D90  I3-Checkpoints-And-Backfill  ← A30 I3
        SyncCheckpoint writer; ~5k accounts; deal stream cursor
        transactional outbox same-TX as raw persist; dispatcher
        ── exit: restart does not re-fetch blindly; unique (broker, ticket) holds

D91  I8-Shadow-From-Dest-Quotes   ← A30 I8 / C54 Gap C
        only after D89 dest tape exists
        fail-closed A24/A72; fill_quote_id; no 35=D
```

**Stop after D91 for first useful version work.** Do not start Phase 6 (ML), Phase 7 (TRADE session / mass status), or Phase 8 (live send) in this sequence.

**Parallelism rule:** D88 and D89 may overlap **after** D85+D86+D87. They must not ship to the shared dashboard until D87 is on. Lab-only diagnostic sockets on a throwaway host are allowed for engineers.

---

## 4. D85.0 — Freeze the compile contract

### 4.1 Goal

D37/D42 observed Infrastructure **RED** mid-swarm (`CS8858` on `ReconstructedTradeResult`, missing `CopyIntent.IdempotencyKey`, leftover plural `IEntityTypeConfiguration<ReconstructedTrades>`, `FixSessionState` ctor drift). A later retry compiled. Flux is not a gate, and it is not allowed to return during I1.

### 4.2 Work

| Step | Action |
|---|---|
| 1 | Treat current Domain entity shapes as the migration input. No drive-by property rename/remove without a store + test update **in the same PR**. |
| 2 | Do not restore HEAD plural `BrokersConfiguration` / `Mt5DealsConfiguration` (B26 CS0246). |
| 3 | `dotnet build Mt5TraderIntelligence.sln -c Release` and both test projects stay green **before** the first `dotnet ef migrations add`. |
| 4 | Scratch `_tmp_*` trees stay out of the `.sln` (D59). |

### 4.3 Exit

Release build **0 errors**. Unit **≥ 64 pass / 0 fail** (skips may remain). Integration compiles. No new `CS0246` / `CS8858`.

---

## 5. D85.1 — Split maps, named UNIQUEs, snake_case

### 5.1 Goal

Make the **existing** 20 tables A61-shaped enough to migrate. **Do not** generate the full 43-table §45 model. **Do not** add `users` / `user_sessions` (D87). **Do not** add `broker_connections` / `fix_session_events` / `destination_symbols` (D88/D89). **Do not** add `mt5_xau_ticks` (D56; exact MFE stays UNAVAILABLE; omit is correct).

### 5.2 Allowed schema deltas (only these)

| Delta | Why now |
|---|---|
| `HasDatabaseName` on uniques that already exist as anonymous indexes | A20/A61 names; required for a stable snapshot |
| `UseSnakeCaseNamingConvention()` + keep explicit `ToTable` | A61 §2.2 |
| `mt5_deals.reason` nullable int (`DealReason?`) | Unblocks D86.4 without a second “oops” migration. **Writer may stay unused this increment.** Null must **not** be treated as REAL_TRADING on reload (D44 / A82: missing ≠ Client). If the writer is not ready, persist `UNKNOWN` or leave the column unused and document it — do **not** silently map null → trading. Prefer: add column, default SQL `NULL`, D86 owns the mapping. |
| Named pending-outbox index `(processed_at) WHERE processed_at IS NULL` if the provider allows; else keep `ProcessedAt` index | A41 / A98. No dispatcher this increment. |

Required unique names (create with the migration; they already exist unnamed):

```text
brokers_code_uk
mt5_groups_broker_name_uk
mt5_accounts_identity_uk
mt5_deals_identity_uk
mt5_positions_current_identity_uk
source_symbol_mappings_uk
trader_scores_uk
sync_checkpoints_uk
copy_intents_idempotency_uk
execution_intents_clordid_uk
fix_sessions_qualifier_uk          -- keep one-row-per-qualifier this increment
                                   -- do NOT claim TRADE is live
canonical_instruments_code_uk
```

`reconstructed_trades` currently indexes `(BrokerId, Login, PositionId, OpenedAt)` **non-unique**. Do **not** invent A30’s `(broker_id, login, position_id, lifecycle_seq)` unique until D86 adds `lifecycle_seq`. Adding a unique on `OpenedAt` would be wrong.

### 5.3 Files

```text
src/Infrastructure/Persistence/TraderDbContextFactory.cs
src/Infrastructure/Persistence/Conventions/PgModelConventions.cs
src/Infrastructure/Persistence/Configurations/*Configuration.cs   -- 20 types, singular
src/Infrastructure/TraderIntelligence.Infrastructure.csproj       -- EFCore.NamingConventions 8.0.3
                                                                  -- pin Npgsql 8.0.4+ (already 8.0.4)
```

Extract inline fluent from `TraderDbContext.OnModelCreating` into `IEntityTypeConfiguration<T>`. `ApplyConfigurationsFromAssembly`. Keep `DbSet`s.

### 5.4 Exit

`Configurations/` has 20 files. Grep `IEntityTypeConfiguration` **> 0**. Grep `HasDatabaseName` **> 0**. Still **0** product `Migrations/` until D85.2 generates them.

---

## 6. D85.2 — One versioned migration

### 6.1 Goal

PostgreSQL becomes the schema authority for the **current** model. Companion reviewed SQL is generated and committed, not hand-applied in prod.

### 6.2 Work

```text
dotnet ef migrations add 20260818D8501_StabilizeExistingModel
  --project src/Infrastructure
  --startup-project apps/api
  --output-dir Persistence/Migrations
```

| Migration | Tables / deltas |
|---|---|
| `20260818D8501_StabilizeExistingModel` | Snapshot of the 20 current tables + named UKs + snake_case + optional `mt5_deals.reason` |

Companion:

```text
src/Infrastructure/Persistence/Sql/20260818D8501_StabilizeExistingModel.sql
```

A30 files `202608180001`–`0007` are **not** retrofitted. That catalog assumed an empty Domain. This tree already has 20 tables. Do **not** squash later. D87/D88/D89 add **new** numbered migrations (`20260818D8701_…`, etc.).

Do **not** hand-edit the generated C# after apply. If the snapshot is wrong, delete the unapplied migration and regenerate.

### 6.3 Exit

`Test-Path Persistence/Migrations` = True. `dotnet ef migrations list` shows one name. No `ModelSnapshot` only without an Up.

---

## 7. D85.3 — Hosts, fail-closed CS, seed gate

### 7.1 Goal

`EnsureCreated` dies. Production / worker / API refuse to start on a missing store. Demo canned tape is opt-in so I1 does **not** freeze P0-wrong first-3 into durable Postgres (that is why D86 is next, not live sockets).

### 7.2 Work

| Path | Change |
|---|---|
| `apps/api/Program.cs` | `MigrateAsync`; delete `EnsureCreatedAsync` |
| `apps/mt5-worker/Program.cs` | same |
| `apps/fix-worker/Program.cs` | same |
| `src/Infrastructure/DependencyInjection.cs` | empty / `<SECRET>` CS → **throw** at host composition (not InMemory). Tests register InMemory themselves (already do). |
| `DemoSeeder.SeedAsync` | Split: `SeedCatalogAsync` (brokers + instrument + FIX `Disconnected` + kill-switch `None`) always safe. `SeedDemoTapeAsync` (Fake ingest + rebuild + invented dest quote + demo shadow) **only** if `TI_SEED_DEMO=true`. |
| Hosts | call catalog seed always after migrate; demo tape only when flagged. Default **false**. |
| `.env.example` | restore to the working tree (D61: path missing; HEAD still tracked). Password slots stay `<SECRET>`. Add `TI_SEED_DEMO=false`. Do not pretend the file is placeholder-only (live IPs remain; D61). |

Keep `UseInMemoryDatabase` **only** in `tests/Integration`.

### 7.3 Exit

Grep product hosts: `EnsureCreated` = **0**. A process with no CS exits non-zero. `TI_SEED_DEMO` unset → `mt5_deals` count **0** after migrate+catalog seed.

---

## 8. D85.4 — Remaining health honesty

### 8.1 Goal

FIX status is already `Disconnected` (D32). Broker / health / outbox / recon still lie. Empty-honest tiles are acceptable. Green badges on Fake are not.

### 8.2 Work

| Today | D85 required |
|---|---|
| `GetBrokersAsync` `Connected = true` | `IMt5BrokerConnector.IsConnectedAsync()` for that `Code`, else `false`. Fake will be true only after `ConnectAsync` — that is honest **as Fake**, and the DTO must carry `transport: "fake"` or details that say so. |
| Overview `mt5Healthy = brokers > 0` | false unless a connector reports connected **and** (if live) last persist is fresh. Catalog rows alone are not health. |
| `/api/health` MT5 `healthy: true` | `false` when transport is Fake / not live. Details may say `FakeMt5BrokerConnector`. |
| `/api/health` `database.healthy: true` | `db.Database.CanConnectAsync()` |
| `/api/health` `outboxBacklog = 0` | `COUNT(*)` where `ProcessedAt == null` |
| `/api/reconciliation/status` `lastReconciliation = UtcNow`, zeros | `lastReconciliation = null` (or last real run); do not stamp now |
| Seeded dest quote `2399.45/2399.85` | **omit** unless `TI_SEED_DEMO=true`. Never write `55=123456`. |
| FIX worker 15 s `Disconnected` overwrite | **leave** (honest). Deleting the loop is D89 when a session object exists. |

Do **not** flip dashboard FIX cards to green. Do **not** add SignalR (D50). Do **not** add Serilog wiring (D54). Do **not** add Redis leases (D29/D55).

### 8.3 Exit

With `TI_SEED_DEMO=false` and no live transport: Overview MT5 health is **false**; `/api/health` MT5 `healthy` is **false**; QUOTE stays **false**; outbox backlog is the real count (0 after catalog-only seed).

---

## 9. Tests (D85 exit)

```text
tests/Integration/Persistence/PostgresMigrationTests.cs
  - apply 20260818D8501 on empty Testcontainers postgres:16
  - second apply is no-op
  - unique (broker_id, deal_ticket) rejects a duplicate
  - named UK exists in pg_indexes (optional assert)

tests/Integration/Persistence/EnsureCreatedGoneTests.cs
  - source assert: product hosts must not call EnsureCreated

tests/Integration/Persistence/FailClosedConnectionStringTests.cs
  - host composition without CS throws / exits

tests/Integration/Persistence/SeedDemoGateTests.cs
  - TI_SEED_DEMO unset → 0 mt5_deals after catalog seed
  - TI_SEED_DEMO=true → canned deals appear (InMemory or Testcontainers)

tests/Unit/Dashboard/BrokerConnectedIsNotLiteralTrueTests.cs
  - GetBrokersAsync does not compile a `true` constant for Connected
```

Add `Testcontainers.PostgreSQL` to `tests/Integration`. Delete `PlaceholderRemoved.Integration_project_loads` / `Assert.True(true)` in the same PR if touched (D42: that fact is not G01).

**Exit command:**

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj
```

Migration tests require Docker. D63: Docker CLI was **MISSING** on the lab PATH at last measure. If still missing, D85.2/D85.3 code may land but the increment is **not accepted** until Testcontainers is run somewhere (this machine after `docker` is on PATH, or CI). Do not mark I1 done on InMemory.

---

## 10. File / package checklist (D85 only)

### 10.1 Create

```text
src/Infrastructure/Persistence/TraderDbContextFactory.cs
src/Infrastructure/Persistence/Conventions/PgModelConventions.cs
src/Infrastructure/Persistence/Configurations/*Configuration.cs
src/Infrastructure/Persistence/Migrations/20260818D8501_*
src/Infrastructure/Persistence/Sql/20260818D8501_*.sql
tests/Integration/Persistence/PostgresMigrationTests.cs
tests/Integration/Persistence/EnsureCreatedGoneTests.cs
tests/Integration/Persistence/FailClosedConnectionStringTests.cs
tests/Integration/Persistence/SeedDemoGateTests.cs
```

### 10.2 Change

```text
src/Infrastructure/Persistence/TraderDbContext.cs          -- ApplyConfigurationsFromAssembly
src/Infrastructure/TraderIntelligence.Infrastructure.csproj
src/Infrastructure/DependencyInjection.cs
src/Infrastructure/Seeding/DemoSeeder.cs
src/Infrastructure/Dashboard/EfDashboardQueries.cs
apps/api/Program.cs
apps/mt5-worker/Program.cs
apps/fix-worker/Program.cs
Directory.Build.props                                      -- optional I0 leftover: net8.0 + warnings-as-errors on product
.env.example                                               -- restore working-tree file
```

### 10.3 Do not create / change this increment

```text
apps/mt5-collector/**
src/Mt5/Http/Mt5CollectorClient.cs
QuickFIXn.* PackageReference
Spec/FIX44-CSERVER.xml
users / user_sessions / LoginPage
MapHub / Serilog UseSerilog / Redis lease
TradeReconstructor rewrite          -- D86
BaselineScorer A22 rewrite          -- D86
ShadowCopyEngine A24 rewrite        -- D91
mt5_xau_ticks                       -- later; MFE omit stays
Kafka / K8s / ClickHouse / services/ml-service
```

---

## 11. D86 (next after D85) — ordered P0 lock

Not this increment. Listed so D85 is not expanded “just one more fix.”

| Order | Item | Evidence | Exit |
|---|---|---|---|
| 86.1 | INOUT money on closed seq only; leftover Start does not `apply_money` | D11 B1 / A21 F04–F05 | F04 leftover net = spec |
| 86.2 | Opposite `ENTRY_IN` is dirty, not silent reverse that discards the closed trade | D11 B2 | `RECON_IN_OPPOSITE_DIRECTION` |
| 86.3 | Reverse closes deal volume / `volume_closed`, not all remaining | D11 B3 | leftover lots survive |
| 86.4 | Overclose is dirty, not `Math.Min` clip | D11 B4 | no clean flatten on extra volume |
| 86.5 | OUT/INOUT sign check vs open direction | D11 B5 | same-sign INOUT is not a complete |
| 86.6 | Seen-ticket set; replay is idempotent | D11 B6 | F01 deal_count stays 2 |
| 86.7 | `RebuildTraderAsync` filters `EligibleForFirstThree` | D73 | canceled position does not score as first-3 |
| 86.8 | A22: `FLAG_MARTINGALE` cannot yield `EarlyQualityScore >= 70` or `SHADOW` | C32 | Case B ≤ 24.75 at N=3; unit gold |
| 86.9 | `DealReason` on DTO + entity + upsert + reload | D44 | service reasons do not count as trading |
| 86.10 | Minimum A21 fixtures F01–F05, F17 | D33 0/25 | new test classes; do not claim 22/22 A89 |

D86 does **not** attach venues. It may keep Fake + Testcontainers.

---

## 12. What this increment is not

| Out | Why / when |
|---|---|
| C47.2 RBAC | D87. Needed before **live** sockets, not before I1. |
| C47.3 collectors / live Manager | D88. Blocked by I1 + D86 P0. |
| C47.4 QuickFIXn QUOTE | D89. |
| TRADE initiator / `35=D` / `35=F` / `35=G` | Phase 8. Safe by absence **and** by flag (D69). |
| `REAL_COPY_EXECUTION_ENABLED=true` | A49 / A101. |
| Full ~5k + checkpointed backfill | D90. |
| Outbox dispatcher / SKIP LOCKED drain | D90. D45 souvenir writer may stay gated behind `TI_SEED_DEMO`. |
| `SyncCheckpoint` writer | D90 (entity exists, D46). |
| Pepperstone SecurityList as a gate | D89 stretch / D91 need. Never hardcode tag 55 (D28). |
| A24 shadow pipeline | D91. D48’s 6 rows are demo. |
| Full A61 43 tables / ML tables / `fix_orders` | Phase 6–8. |
| Kafka / K8s / ClickHouse / LLM | §71 / A80. |
| Rewrite `mt5-sdk` | C20. |
| Linux `LoadLibrary` / collector in compose | A105 / D63 `UNSAFE`. |
| Treating Fake, `EnsureCreated`, or stamped `Disconnected` as §69.9 | A100 vacuous law. `Disconnected` is honest, not Logon. |
| Recon / score P0 rewrite | D86, immediately after. |

---

## 13. Quality loop (mandatory per slice)

```text
CODER → REVIEWER (unbiased) → [fix] → REVIEWER → TEST
  on TEST FAIL: RESEARCHER (A61 / EF docs / A21) → CODER → REVIEWER → TEST
PASS+PASS = slice DONE
```

| Slice | Reviewer looks for | Test |
|---|---|---|
| 85.0 | no entity/store drift; sln has no `_tmp_*` | `dotnet build` Release |
| 85.1 | singular types; named UKs; no password columns | compile + config count |
| 85.2 | one migration; no hand-edit after apply | `dotnet ef migrations list` |
| 85.3 | no EnsureCreated; fail-closed CS; demo gated | Integration Persistence |
| 85.4 | no literal Connected=true; health matches transport | unit + manual `/api/health` |

Do not merge a slice on “it compiles.”

---

## 14. Scoreboard impact (honest)

| Bar | Now | After D85 if **all** exits measured | Still not |
|---|---|---|---|
| §69 accepted | 0/12 (D41) | **still 0/12** | venues, recon P0, QUOTE, shadow |
| §68 live | 0/19 (D42) | still 0/19 | leave `[ ]` |
| §70 FIX | 0/14 (D43) | still 0/14 | — |
| C29 / D51 migrations | 0/15 A30 files; 0 folders | **1** versioned migration of the **existing** model | 0001–0015 catalog not retrofitted |
| C18 / D53 RBAC | MISSING | still MISSING | D87 |
| C19 / D52 QuickFIX | not referenced | still not | D89 |
| C42 live MT5 | NOT PROVEN | still not | D88 + probe log |
| D11 recon P0 | open | still open | D86 |
| C32 martingale SHADOW | open | still open | D86 |
| Health lies | FIX honest; MT5/outbox/recon not | MT5/outbox/recon honest as demo | not venue PASS |

**Do not edit A100/A101/D41 checkboxes from this file.** A later dated successor flips boxes with test class, command, timestamp, SHA-256.

---

## 15. Anti-greenwash (reviewer reject list)

| Claim after D85 | Reject unless |
|---|---|
| “Migrations done” | `Migrations/` on disk **and** Testcontainers apply **and** hosts call `Migrate` **and** `EnsureCreated` = 0 |
| “Postgres is the store” | fail-closed CS **and** InMemory only in tests |
| “I1 complete vs A30 0001–0007” | **No.** D85 snapshots the demo model. It is not the original seven-file catalog. |
| “First useful version” | still **no** — 12/12 not met |
| “Go-live” | still **no** — 0/19 |
| “Achiever connected” | still **no** — Fake only |
| “QUOTE logged on” | still **no** — `Disconnected` is honest |
| “We used QuickFIX” | still **no** — D52 holds |
| “RBAC done” | still **no** — D53 holds |
| “Reconstruction is A21” | still **no** — D11 holds until D86 |
| “Safe to send 35=D” | still **no** |
| “C47 started” | **No.** C47 is D87–D89. |

---

## 16. Residual risks

1. **Docker CLI missing** (D63). Testcontainers cannot run until `docker` is on PATH. Code can still be written; the increment cannot be **accepted**.
2. **Dev laptops with EnsureCreated-shaped local Postgres** will not match the snapshot. Document “drop the dev DB once.”
3. **`TI_SEED_DEMO=true` after D85 and before D86** still persists P0-wrong first-3 into durable Postgres. Default false exists to prevent that. Lab operators who flip the flag accept dirty rows.
4. **`mt5_deals.reason` unused** until D86. Do not claim A82 is implemented because a column exists.
5. **Anonymous `POST /api/ops/resync`** remains (D53). Acceptable only while transport is Fake and demo seed is gated. D87 must close it before D88.
6. **Fake default in CI** must remain so Linux agents never need the PE.
7. **`.env.example` live identifiers** (D61). D85 restores the file; it does not scrub non-secret hosts. Passwords stay tokens.
8. **Kill-switch exclusive enum** (D70) is not this increment. Do not “fix” it while adding the snapshot unless the column is untouched.

---

## 17. Sources (read, not modified)

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§10–13, 45, 60, 67–73
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`, `EfTradingStore.cs`, `DependencyInjection.cs`, `Seeding\DemoSeeder.cs`, `Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\api\Program.cs`, `apps\mt5-worker\Program.cs`, `apps\mt5-worker\Worker.cs`, `apps\fix-worker\Program.cs`, `apps\fix-worker\Worker.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md`, `A61_efcore_schema.md`, `C29_migrations_gap.md`, `C47_next_increment.md`, `C54_remaining_gaps.md`
- `D:\Prop\reports\swarm\20260818\D11_recon_bugs.md`, `D19_dbcontext.md`, `D32_fixw.md`, `D41_fuv_now.md`, `D42_gates_now.md`, `D44_reason_gap.md`, `D45_outbox.md`, `D46_checkpoint.md`, `D51_migrations.md`, `D52_qfn.md`, `D53_rbac.md`, `D63_compose.md`

---

## 18. One-page operator view

```text
D85 I1-Schema-Authority                      2026-08-18  PLAN ONLY
==============================================================
D85.0  Freeze compile contract               TODO
D85.1  Split configs + named UKs             TODO
D85.2  Migration 20260818D8501               TODO
D85.3  Migrate / no EnsureCreated / seed gate TODO
D85.4  Health honesty (MT5/outbox/recon)     TODO
--------------------------------------------------------------
Next increment (do not start now)
D86    I4-P0 recon + A22 martingale lock
Then   D87 RBAC → D88 live MT5 → D89 QUOTE
       → D90 checkpoints/outbox → D91 shadow
--------------------------------------------------------------
§69 accepted after D85                       still 0 / 12
§68 live-copy license                        still 0 / 19
§70 TRADE acceptance                         still 0 / 14
REAL_COPY_EXECUTION_ENABLED                  false
Live 35=D                                    forbidden
mt5-sdk rewrite                              forbidden
EnsureCreated                                must die
Fake connector in CI                         stays
C47 live-foundation                          not this increment
==============================================================
```

**Bottom line:** the next increment is not more demo pages, not RBAC, and not live sockets. It is **one versioned Postgres migration of the tables that already exist**, hosts that **Migrate** and **refuse** a missing store, a **gated** demo tape so P0-wrong first-3 does not become durable, and **honest** health bits. After that, lock reconstruction and scoring (D86) **before** C47’s collectors and QuickFIX/n QUOTE Logon.

*End of D85. Product source was not modified.*
