# D46 — Is `SyncCheckpoint` written?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D46_checkpoint.md` |
| Agent | D46 (senior engineer, checkpoint writer census, read-only) |
| Date | 2026-08-18 |
| Assigned | Is `SyncCheckpoint` written? Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No** |
| Snapshot UTC | 2026-08-18 (hashes below; entity LastWriteTimeUtc `2026-08-18T07:39:03Z`) |
| Law | Architecture v2 **§11** raw table `sync_checkpoints`; **§12** `Read checkpoint → fetch → normalize → upsert → Persist checkpoint`; A20 §5.8; A59 design; A61 §8.40 |
| Classification vocab | §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` |

**Honesty rule:** a Domain type plus `DbSet` is not a written cursor. `EnsureCreated` creating an empty `sync_checkpoints` table is not backfill. A 30-day host-clock window is not a checkpoint. Do not treat A59’s “no entity type” sentence as current — the type landed; the writer did not.

---

## 0. Verdict

**No. The `SyncCheckpoint` type is written. No product path writes a row.**

| Question | Measured answer |
|---|---|
| Does `Domain.Entities.SyncCheckpoint` exist on disk? | **Yes.** 12 lines / 391 bytes. SHA-256 `15FF40719E5FE3ADBA8B2F0E6D7215C02D2B813EC84A1E092EC1D5BE9CB83056`. |
| Is it mapped in `TraderDbContext`? | **Yes.** `DbSet<SyncCheckpoint> SyncCheckpoints` + inline `ToTable("sync_checkpoints")` + unique `(BrokerId, Login, Stream)`. |
| Does any C# construct `new SyncCheckpoint`? | **No.** Zero hits under `src/`, `apps/`, `tests/`. |
| Does any C# `Add` / `Update` / `Remove` `SyncCheckpoints`? | **No.** |
| Does `ITradingStore` / `EfTradingStore` read or write it? | **No.** Store SHA-256 `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` (338 lines). Grep of that file: **0** `SyncCheckpoint`. |
| Does `DealIngestionService.SyncBrokerAsync` persist a checkpoint after upserts? | **No.** Caller-supplied `[from,to]` only. Returns inserted-deal count. |
| Does `DemoSeeder` seed checkpoint rows? | **No.** Seeds brokers / instrument / FIX / quote / kill-switch, then ingest + score. Leaves `SyncCheckpoints` empty. |
| Does `apps/mt5-worker` read a cursor? | **No.** Every 30 s uses host-clock `[UtcNow-30d, UtcNow+1m]`. |
| Does the dashboard query it? | **No.** `EfDashboardQueries` never touches `SyncCheckpoints` (D21 still holds). |
| Is `ISyncCheckpointStore` / `SyncCheckpointStore` in the tree? | **No.** `Application/Abstractions/` does not exist. `Persistence/Repositories/` does not exist. |
| Is the A20/A59/A61 shape implemented? | **No.** Current unique is login-forced `(BrokerId, Login, Stream)`, not `(scope_type, scope_id, stream_name)` / A59 `(scope_type, scope_id, login, stream_name)`. No lease, fencing, overlap, status, or payload. |
| Are there migrations or tests? | **No.** `Configurations/` is empty. No `Migrations/`. Tests: **0** `SyncCheckpoint` hits. |

**One-line:** entity-only skeleton. Restart-safe §12 ingestion is **MISSING**.

Classification of what exists:

| Surface | Class |
|---|---|
| `SyncCheckpoint.cs` type | `EXISTS_NEEDS_REFACTOR` (thin; wrong unique; unused) |
| `TraderDbContext` `DbSet` + `ToTable` | `EXISTS_NEEDS_REFACTOR` (name present; columns/UK/FKs wrong) |
| Named `sync_checkpoints_uk` / snake_case / FK | `MISSING` |
| `ISyncCheckpointStore` port (A02 O2) | `MISSING` |
| `SyncCheckpointStore` + `SyncCheckpointConfiguration` | `MISSING` |
| Ingest / worker / seeder **writer** | `MISSING` |
| Dashboard / recon-page **reader** | `MISSING` |
| `IHistoricalBackfillService` / three-loop §12 | `MISSING` |
| Checkpoint tests (A90 / A59) | `MISSING` |
| Versioned migration `0007` | `MISSING` |
| C++ `mt5-sdk` cursor table | `MISSING` (no `sync_checkpoint` symbol) |
| React / API mention | `MISSING` |

Do **not** score this as “checkpoints exist” on a first-useful-version card. C13’s “entity + unique exist; writers unused” is still the measured state.

---

## 1. What was read (no product edits)

| Path | Role | Measured |
|---|---|---|
| `D:\Prop\src\Domain\Entities\SyncCheckpoint.cs` | subject type | **12** lines; **391** bytes; SHA-256 `15FF40719E5FE3ADBA8B2F0E6D7215C02D2B813EC84A1E092EC1D5BE9CB83056`; LastWriteTimeUtc `2026-08-18T07:39:03Z` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | `DbSet` + fluent map | **174** lines; **5951** bytes; SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (same as D19) |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | only persist writer | **338** lines; **12097** bytes; SHA-256 `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `ITradingStore` + `SyncBrokerAsync` | **106** lines; **4535** bytes; SHA-256 `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | boot seed | **140** lines; **5082** bytes; SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 30 s loop | **45** lines; **1882** bytes; SHA-256 `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | registrations | SHA-256 `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380`. **No** checkpoint store. |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dashboard reads | SHA-256 `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60`. **0** checkpoint LINQ. |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | only store/seed facts | SHA-256 `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD`. **0** checkpoint asserts. |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\` | A61 split maps | **0** files |
| `D:\Prop\src\Application\` | ports | `Contracts/`, `Dashboard/`, `Ingestion/` only. **No** `Abstractions/`. |
| Architecture §11–12, §45 | law | quoted in §2 |
| A20 / A59 / A61 | target shape | used to judge “present but wrong” |

Grep (`SyncCheckpoint` / `ISyncCheckpointStore` / `SyncCheckpointStore` / `new SyncCheckpoint` / `SyncCheckpoints.Add`) under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`:

| Area | Hits |
|---|---|
| `src/Domain/Entities/SyncCheckpoint.cs` | type declaration + `LastTimestamp` / `LastTicket` |
| `src/Infrastructure/Persistence/TraderDbContext.cs` | `DbSet` L23; fluent L115–120 |
| All other `*.cs` | **0** |
| `apps/web` (`*.ts` / `*.tsx`) | **0** |
| `mt5-sdk` (`*.cpp` / `*.h`) | **0** (`sync_checkpoint`) |
| `tests/**/*.cs` | **0** |

No `Domain/Ingestion/SyncCheckpoint.cs`, no `Domain/Mt5/SyncCheckpoint.cs` (those A01/A30 paths were never created). The live type is `TraderIntelligence.Domain.Entities.SyncCheckpoint`.

---

## 2. Binding law (what “written” means)

Architecture v2 §11 names `sync_checkpoints` as a raw-layer table. §12 defines the only legal backfill:

```text
Read checkpoint
    ↓
Fetch history
    ↓
Normalize
    ↓
Upsert idempotently
    ↓
Persist checkpoint
```

A59 L5 / L10 (still binding):

- Persist checkpoint **per broker/account** after a **complete** fetch.
- Incomplete `GetDeals` (`false` / throw / partial page) **must not** advance the cursor.
- Two brokers must not share a deals cursor. No single global checkpoint.
- Cursor lives in PostgreSQL, not Redis (L12).

A20 UNIQUE: `(scope_type, scope_id, stream_name)`.  
A59 pin (unifies A20 + A30): `(scope_type, scope_id, login, stream_name)` with `NULLS NOT DISTINCT`.  
A61 §8.40: `ScopeType` / `ScopeId` / `StreamName` / cursor bounds / `LastEntityTicket` / `Status` / `UpdatedAt`; named UK `sync_checkpoints_uk`. Advance **only after** successful idempotent upsert of the corresponding raw rows.

A row is “written” only if some production path (ingest, reconcile, seeder, or a store method called by those) inserts or updates `sync_checkpoints` **in the same commit as** (or strictly after a visible commit of) the raw upserts it describes. A type sitting unused does not satisfy §12.

---

## 3. The type as measured (entire file)

```1:12:D:\Prop\src\Domain\Entities\SyncCheckpoint.cs
namespace TraderIntelligence.Domain.Entities;

public sealed class SyncCheckpoint
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long Login { get; set; }
    public string Stream { get; set; } = "deals";
    public DateTimeOffset? LastTimestamp { get; set; }
    public long? LastTicket { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

Seven properties. No methods. No `scope_type`. `Login` is a **required** `long` (CLR default `0`, EF non-nullable). Default stream is the literal `"deals"`, which is **not** in A59’s Phase-1 catalog (`deals_backfill` / `deals_live` / `positions_reconcile` / `accounts` / `groups` / …).

`Mt5Account.Login` is also `long` today — the old A29 `ulong` vs `long` drift on this property is gone. That does not make the unique key usable for broker/venue/global streams.

---

## 4. EF mapping as measured

```115:120:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
        modelBuilder.Entity<SyncCheckpoint>(e =>
        {
            e.ToTable("sync_checkpoints");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.Login, x.Stream }).IsUnique();
        });
```

`DbSet` at L23: `public DbSet<SyncCheckpoint> SyncCheckpoints => Set<SyncCheckpoint>();`

Present:

- Table name `sync_checkpoints` (matches §45 / A20 name).
- Surrogate PK `Id`.
- Unique composite `(BrokerId, Login, Stream)`.

Absent (vs A20 / A59 / A61 / D19):

| Expected | Measured |
|---|---|
| `IEntityTypeConfiguration<SyncCheckpoint>` | **0** files in `Configurations/` |
| Named UK `sync_checkpoints_uk` (`HasDatabaseName`) | **No** |
| Snake_case columns (`broker_id`, `last_timestamp`, …) | **No.** No `EFCore.NamingConventions`. `EnsureCreated` on Npgsql would emit PascalCase `BrokerId` / `Login` / `Stream` / … |
| `ScopeType` / `ScopeId` / `StreamName` | **No** (has `BrokerId` + required `Login` + `Stream`) |
| `cursor_from` / `cursor_to` / `cursor_kind` / overlap | **No** (`LastTimestamp` + `LastTicket` only) |
| `status` / `lease_owner` / `lease_until` / `fencing_token` | **No** |
| `payload` jsonb / row counters / last error | **No** |
| FK `BrokerId → brokers.id` | **No** (`HasOne` / `HasForeignKey` = 0 in this context) |
| `HasMaxLength` on `Stream` | **No** |
| Versioned migration | **No** (`EnsureCreatedAsync` on three hosts) |

`Login` on the unique index is mandatory. The table **cannot** represent:

- broker-wide `groups` / `accounts` / `symbols` / `ticks_xau` (`login` should be NULL);
- venue `security_list` / `order_mass_status`;
- `GLOBAL` reserved rows.

Stuffing `Login = 0` for those streams would collide every broker-level stream onto one fake account. That is not A59.

If someone pointed Npgsql at an empty database, `EnsureCreated` would create an **empty** table of this wrong shape. That is still zero written cursors.

---

## 5. Writer census — every persist path

### 5.1 `ITradingStore` (Application port)

Nine members on `DealIngestionService.cs` L8–18. None mention a checkpoint:

`UpsertGroupAsync`, `UpsertAccountAsync`, `UpsertDealAsync`, `ReplacePositionsAsync`, `LoadDealsAsync`, `ReplaceReconstructedAsync`, `UpsertScoreAsync`, `PersistDemoShadowAsync`, `ResolveBrokerIdAsync`.

There is no `GetCheckpointAsync` / `AdvanceCheckpointAsync` / `SaveCheckpointAsync`.

### 5.2 `EfTradingStore` (only Infrastructure writer)

338 lines. Six-plus `SaveChangesAsync` sites. Touches: groups, accounts, deals, positions, reconstructed trades, scores + history, and (in `PersistDemoShadowAsync`) `OutboxEvents` / `CopyIntents` / `ShadowOrders`.

Grep of this file: **0** `SyncCheckpoint`, **0** `SyncCheckpoints`, **0** `checkpoint`.

Deal insert is still sequential `AnyAsync` + `Add` (D20). Even if a checkpoint write were appended later, it is **not** in the same transaction as the deal batch today (`BeginTransaction` = 0).

### 5.3 `DealIngestionService.SyncBrokerAsync`

```32:59:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;
        // GetGroups → UpsertGroup; GetAccounts → per login:
        //   UpsertAccount; GetDeals(login, from, to) → UpsertDeal; GetPositions → ReplacePositions
        return insertedDeals;
    }
```

The window is an **argument**. The method never reads a prior cursor and never persists one after the loop. Incomplete-fetch fail-closed (A59 L10) cannot be implemented here: `GetDealsAsync` on the Fake returns a list; there is no `false` completeness flag on this C# path.

This is **not** `IHistoricalBackfillService`. A90’s warning still holds: do not rename this loop into a false-green backfill test.

### 5.4 `DemoSeeder`

Writes: 2 `Broker`, 1 `CanonicalInstrument`, 2 `FixSessionState`, 1 `DestinationQuoteSnapshot`, 1 `KillSwitch`, then `SyncBrokerAsync` (fixed calendar 2026-01-01 … 2026-12-31) and `RebuildTraderAsync` for `{10001,10002,10003,99001}`.

**Does not** `db.SyncCheckpoints.Add`. C16’s seed-fact table still applies: `SyncCheckpoints` expected count **0**. The seed test does not even assert that zero — a future writer would not fail `Demo_seed_discovers_groups_reconstructs_and_scores`.

### 5.5 `apps/mt5-worker/Worker.cs`

```27:35:D:\Prop\apps\mt5-worker\Worker.cs
                var from = DateTimeOffset.UtcNow.AddDays(-30);
                var to = DateTimeOffset.UtcNow.AddMinutes(1);
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
```

Host wall clock, last 30 days, four hard-coded logins. D31 already scored this host **0 checkpoints read or written**. Reconfirmed: still 0. Fixture Fake deals stamped 2026-06-01/02 fall **outside** a 2026-08-18 − 30d window (D31), so the worker loop does not even re-pull the demo tape — and still does not write a cursor about that fact.

### 5.6 DI

`AddTraderIntelligence` registers `ITradingStore → EfTradingStore`, `IDashboardQueries`, reconstructor, scorer, `DealIngestionService`, `ReconstructionScoringService`. **No** `ISyncCheckpointStore`.

### 5.7 Dashboard / API / web

`EfDashboardQueries.GetOverviewAsync` health tiles do **not** consult `max(sync_checkpoints.UpdatedAt)`. A91’s `health.mt5Ingestion` from checkpoints is unimplemented. A96’s recon-page `lastSuccessfulAt` from streams `deals_reconcile` / `positions_reconcile` is unimplemented. `apps/web` has **0** `checkpoint` strings.

### 5.8 Contrast: things that *are* written

| Row type | Writer today | Checkpoint? |
|---|---|---|
| `mt5_groups` / `mt5_accounts` / `mt5_deals` / `mt5_positions_current` | `EfTradingStore` via ingest | no cursor advanced |
| `trader_scores` / `trader_score_history` / `reconstructed_trades` | scoring path | no |
| `outbox_events` / `copy_intents` / `shadow_orders` | `PersistDemoShadowAsync` (when that path compiles against current entity shapes) | no |
| `sync_checkpoints` | **nobody** | — |

---

## 6. Shape vs A20 / A59 / A61 (field map)

| A59 / A61 column | Current property | Fit |
|---|---|---|
| `id` uuid PK | `Id` | OK |
| `scope_type` | — | **MISSING** |
| `scope_id` | `BrokerId` only | Too narrow; cannot be a venue |
| `login` NULL-able | `Login` **required long** | **WRONG** — blocks broker/venue/global |
| `stream_name` | `Stream` default `"deals"` | Wrong default; no catalog check |
| `cursor_kind` | — | **MISSING** |
| `cursor_from_sec` / `cursor_to_sec` | `LastTimestamp` (one timestamptz) | Incomplete; host clock, not MT5 server seconds |
| `cursor_ticket` / `high_water_ticket` | `LastTicket` | Partial (one field, never set) |
| `overlap_sec` | — | **MISSING** (mandatory for `TIME_TICKET`) |
| `status` / lease / `fencing_token` | — | **MISSING** |
| `last_success_at` / `last_error` / counters / `payload` | `UpdatedAt` only | **MISSING** |
| UNIQUE | `(BrokerId, Login, Stream)` unnamed | **WRONG SHAPE** vs `sync_checkpoints_uk` |

A61 also wants `PayloadHash` + `xmin` concurrency. Neither exists.

---

## 7. Planned files that are still absent

From A30 / A59 §14 / A90 — none of these paths exist:

| Planned path | State |
|---|---|
| `src/Application/Abstractions/ISyncCheckpointStore.cs` | **MISSING** |
| `src/Application/Ingestion/IHistoricalBackfillService.cs` + `HistoricalBackfillService.cs` | **MISSING** |
| `src/Infrastructure/Persistence/Repositories/SyncCheckpointStore.cs` | **MISSING** (`Repositories/` dir absent) |
| `src/Infrastructure/Persistence/Configurations/SyncCheckpointConfiguration.cs` | **MISSING** |
| Migration `0007` `sync_checkpoints` + outbox + ingestion_events | **MISSING** |
| `tests/Integration/Mt5/Mt5BackfillRestartTests.cs` | **MISSING** |
| `tests/Integration/Mt5/Mt5IncompleteFetchDoesNotAdvanceCheckpointTests.cs` | **MISSING** |
| `tests/Integration/Mt5/Mt5AccountSyncCheckpointTests.cs` | **MISSING** |
| `apps/mt5-worker` `Mt5HistoryBackfillHostedService` | **MISSING** (D07 / D31: 0/7 A64 jobs) |

---

## 8. Stale reports (do not recycle as current type-existence)

| File | Stale claim | Current measured |
|---|---|---|
| A01 / A03 / A59 §0 / A59 §90 | No entity; `DbSet<SyncCheckpoints>` + `SyncCheckpointsConfiguration` stub | Entity **exists**. `DbSet<SyncCheckpoint>`. **No** `ApplyConfiguration`. |
| A29 D27 | Types exist, **no tables** | Table **name** is mapped inline; still no migration / no rows. |
| A57 / C13 / C54 | Entity unused | **Still unused.** Rechecked 2026-08-18 this file. |
| D20 store hash `05103CE5…` / 250 lines | Older store snapshot | Store is now **338** lines / `DC03BBE6…`. Still **0** checkpoint I/O. |
| D22 “seeder does not write Outbox/Copy/Shadow” | Pre-`PersistDemoShadowAsync` | Those rows may now be attempted on score; **`SyncCheckpoints` remains unwritten.** |

Use **this file** for “is a checkpoint written?”. Use A59 for the **target** schema and advance rules. Use D19/D03 for table-name census. Use D31 for the worker window.

---

## 9. What would count as “written” (acceptance, not a plan to implement here)

All of the following, measured, not asserted:

1. A store method (port `ISyncCheckpointStore`) that **reads** `(scope, login, stream)` and **upserts** the cursor.
2. Backfill (or account-sync) calls **read → fetch → idempotent upsert → persist cursor** in one visible commit (or persist only after the upsert commit).
3. Incomplete `GetDeals` does **not** update `LastTimestamp` / `LastTicket` / A59 cursors. A test proves it (`Mt5IncompleteFetchDoesNotAdvanceCheckpointTests`).
4. Crash mid-batch + restart resumes from the last **committed** cursor and does not duplicate `(broker_id, deal_ticket)` (`Mt5BackfillRestartTests`).
5. Two brokers never share a row (A20 L3).
6. Seed or first backfill leaves `SyncCheckpoints.Count() > 0` for the streams it actually ran, and the seed test **fails** if that count is 0 while claiming backfill.
7. Worker `from/to` is derived from the cursor + overlap, **not** `UtcNow.AddDays(-30)`.

Until then the honest FUV line remains A57: *"`SyncCheckpoint` entity exists … but is unused."*

---

## 10. Not claimed

- That `EnsureCreated` + InMemory proves a PostgreSQL `sync_checkpoints` table.
- That the unique `(BrokerId, Login, Stream)` is “good enough” for Phase 1 (it is not: no broker-level `accounts` stream, no fencing).
- That `LastTimestamp` could be filled later from `GetServerTime` host fallback and called a server-time cursor (A04 / A58: that write is a known foot-gun).
- That outbox `PersistDemoShadowAsync` is a substitute cursor.
- That C++ `mt5_ledger_store` `ON CONFLICT` is this table (it is a sibling ledger keyed by `server_key`, A59 §0).
- That this report implemented any of A59.

**Answer to the assigned question:** `SyncCheckpoint` is a mapped Domain entity. It is **not written** by ingest, store, seeder, worker, dashboard, tests, or C++. Classification of the writer: **MISSING**.
