# D45 — Is `OutboxEvent` written anywhere?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D45_outbox.md` |
| Agent | D45 (outbox produce census, read-only) |
| Date | 2026-08-18 |
| Assigned | **Is OutboxEvent written anywhere?** Write this file. Do not modify product source. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Binding law | Architecture v2 **§12** (persist raw → write transactional outbox → **one** commit; then workers drain) and **§13** (PostgreSQL outbox; five v1 kinds). Specs: A41, A61 §5, A64, A90. |
| Method | Full read of entity / enum / `TraderDbContext` / `EfTradingStore` / `DealIngestionService` / `DemoSeeder` / both workers / API `Program.cs` / DI. `Select-String` of `src/`, `apps/`, `tests/` `*.cs` for `new OutboxEvent`, `OutboxEvents.Add`, `OutboxEventType.`, `IOutbox*`, `ProcessedAt =`. SHA-256 via `Get-FileHash`. Measured seed counts via existing scratch `D37SeedEval` (InMemory; **not** product). Did **not** hit Compose Postgres. |

**Honesty rule:** one `DbSet.Add` is a write. It is **not** a transactional outbox. A hardcoded `/api/health.outboxBacklog = 0` is an ops lie once rows exist. C58 / B03 / D03 / D20 / D22 / D31 / C59 “never written / 0 `OutboxEvents.Add`” are **stale** as of this hash.

---

## 0. Verdict (honest)

**Yes. One product write site. It is not §12/§13.**

`EfTradingStore.PersistDemoShadowAsync` is the **only** `new OutboxEvent` / `OutboxEvents.Add` in product C#. It always inserts `OutboxEventType.ScoreUpdate` (int `1`) and `SaveChangesAsync`s it. Measured after `DemoSeeder.SeedAsync`: **4 rows**, one per rebuilt login.

It is **not**:

- same-transaction as raw deal persist (`UpsertDealAsync` commits alone, no outbox);
- same-transaction as the score row (`UpsertScoreAsync` already committed);
- a drain (`ProcessedAt` is never assigned anywhere);
- the five §13 kinds (only `ScoreUpdate`; never `TradeCompleted` / `ShadowCopyIntent` / `RiskCheckRequest` / `NotificationEvent`);
- idempotent (new `Guid` every call; no `dedupe_key`; worker `RebuildTraderAsync` every 30 s will **append** forever);
- queried by health (`outboxBacklog` is a literal `0`).

| Slice | Measured | Class |
|---|---|---|
| Domain type `OutboxEvent` | 10 properties, `Domain/Entities` | `EXISTS_NEEDS_REFACTOR` (shape) |
| Domain enum `OutboxEventType` | 5 §13 kinds, stored as **int** | `EXISTS_NEEDS_REFACTOR` |
| EF `DbSet` + `ToTable("outbox_events")` | yes | `EXISTS_AND_GOOD` (name only) |
| Product insert | **1** site: `PersistDemoShadowAsync` L258 | **written** |
| `IOutboxWriter` / same-TX as deal | **0** | `MISSING` vs §12 |
| Dispatcher / `ProcessedAt` ack / SKIP LOCKED | **0** | `MISSING` (C58 drain claim **still holds**) |
| Tests asserting outbox | **0** | `MISSING` |
| `/api/health.outboxBacklog` | literal `0` after seed writes 4 | `UNSAFE` (ops lie) |
| Kafka / MassTransit / NATS | **0** | correctly absent |

**One-liner:** `OutboxEvent` **is** written — as a demo `ScoreUpdate` souvenir on every trader rebuild — not as a transactional bus.

---

## 1. Files hashed (this pass)

| Bytes | Lines | SHA-256 | Path |
|---:|---:|---|---|
| 485 | 15 | `0F5CDDF38EA37DEA27D30E7E6A33C9516EA7149C8B14D8F9D442F0267F4471C3` | `D:\Prop\src\Domain\Entities\OutboxEvent.cs` |
| 211 | 11 | `163ED842EE9AF0C94EA912A91845F31C8644F2A1A373A67C77E7FA16154BAADA` | `D:\Prop\src\Domain\Enums\OutboxEventType.cs` |
| 12097 | 338 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| 5951 | 174 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 4535 | 106 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| 5082 | 140 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| 1900 | — | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| — | — | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `D:\Prop\apps\api\Program.cs` |
| — | — | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | `D:\Prop\apps\mt5-worker\Worker.cs` |
| — | — | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | `D:\Prop\apps\mt5-worker\Program.cs` |
| — | — | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | `D:\Prop\apps\fix-worker\Worker.cs` |
| — | — | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `D:\Prop\apps\fix-worker\Program.cs` |
| — | — | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |

`OutboxEventType` and `TraderDbContext` hashes match C58. **`EfTradingStore` does not:** C58/D20 hashed `05103CE5…` / 9020 bytes / 250 lines with **0** outbox touches. Current store is **338** lines / 12097 bytes and contains the write.

Product files named `*Outbox*`: only the entity + enum. No `Persistence/Outbox/`, no `Persistence/Migrations/` (`Test-Path` **False**).

---

## 2. The only write site

```251:270:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task PersistDemoShadowAsync(
        Guid brokerId,
        long login,
        TraderState state,
        IReadOnlyList<ReconstructedTradeResult> completedXau,
        CancellationToken ct)
    {
        _db.OutboxEvents.Add(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            Type = OutboxEventType.ScoreUpdate,
            AggregateId = $"{brokerId}:{login}",
            PayloadJson = $"{{\"state\":\"{state}\",\"completed\":{completedXau.Count}}}",
            OccurredAt = DateTimeOffset.UtcNow
        });

        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
```

All three exits of this method call `SaveChangesAsync` **after** the `Add` (L269, L276, L336). The `ScoreUpdate` row is therefore persisted whenever the method completes:

| Exit | Condition | Also written in same `SaveChanges` |
|---|---|---|
| L269 | `state != SHADOW` | outbox only |
| L276 | no `DestinationQuotes` row | outbox only |
| L336 | `SHADOW` + quote present | outbox + new `CopyIntent` / `ShadowOrder` pairs (skipped if `IdempotencyKey` exists) |

What is **not** set on insert: `ProcessedAt` (null), `Attempts` (0), `LastError` (null), `CorrelationId` (null). Payload is string interpolation, not `System.Text.Json`. No `BeginTransaction`. No `ON CONFLICT`.

`Select-String` of `src/` + `apps/` + `tests/` `*.cs`:

| Pattern | Hits |
|---|---|
| `new OutboxEvent` | **1** (`EfTradingStore.cs:258`) |
| `OutboxEvents.Add` / `AddRange` | **1** (same) |
| `OutboxEventType.` | **1** (`ScoreUpdate` only) |
| `OutboxEventType.TradeCompleted` / `ShadowCopyIntent` / `RiskCheckRequest` / `NotificationEvent` | **0** |
| `IOutbox*` / `IEventBus` / `OutboxProcessor` / `TransactionalOutbox` / `SKIP LOCKED` | **0** |
| `ProcessedAt =` | **0** |
| `tests/**` `Outbox` | **0** |
| `mt5-sdk/src` `OutboxEvent` | **0** (C++ “fast outbox” metrics are a **different** tree — do not count) |

Sole `ITradingStore` implementation: `EfTradingStore` (DI L36). No second writer.

---

## 3. Call graph (who causes the write)

```text
PersistDemoShadowAsync          ← only Add
        ▲
RebuildTraderAsync              DealIngestionService.cs:104
        ▲
        ├── DemoSeeder.SeedAsync          4 logins {10001,10002,10003,99001}
        │     ▲
        │     ├── apps/api/Program.cs boot
        │     ├── apps/mt5-worker/Program.cs boot
        │     └── apps/fix-worker/Program.cs boot
        ├── POST /api/ops/resync          same 4 logins
        └── mt5-worker Worker 30 s loop   same 4 logins, unbounded
```

`DealIngestionService.SyncBrokerAsync` / `UpsertDealAsync` do **not** write outbox. Reconstruction + scoring still run **in-process on the ingest/rebuild tick** (opposite of §12). The outbox row is appended **after** score persist, as a side effect of the demo-shadow helper.

`fix-worker/Worker.cs` never reads or writes `OutboxEvents`.

---

## 4. Measured seed (InMemory, D37SeedEval)

Scratch host: `D:\Prop\reports\swarm\20260818\_tmp_d37_eval` (not product). `DemoSeeder.SeedAsync` then `COUNT(*)`.

| Table | Count after first seed | After second `SeedAsync` |
|---|---:|---:|
| Brokers | 2 | 2 (guard: brokers exist → return) |
| Mt5Deals | 18 | 18 |
| TraderScores | 4 | 4 |
| **OutboxEvents** | **4** | **4** |
| CopyIntents / ShadowOrders | 6 / 6 | 6 / 6 |
| SyncCheckpoints | 0 | 0 |

Dumped outbox rows (all `Type = ScoreUpdate`, `ProcessedAt` unset):

| AggregateId | PayloadJson |
|---|---|
| `aaaaaaaa-…aaa1:10001` | `{"state":"SHADOW","completed":3}` |
| `aaaaaaaa-…aaa1:10002` | `{"state":"RISK_BLOCKED","completed":3}` |
| `aaaaaaaa-…aaa1:10003` | `{"state":"INSUFFICIENT_DATA","completed":0}` |
| `aaaaaaaa-…aaa2:99001` | `{"state":"SHADOW","completed":3}` |

Four rebuilds → four inserts. Seed re-entry does not grow the table (broker guard). A second `RebuildTraderAsync` for the same login **would** grow it: new `Guid`, no unique key on `(Type, AggregateId)`. The 30 s mt5-worker loop is that second (and Nth) caller.

`SeedingAndStoreTests` does **not** assert `db.OutboxEvents`. C16’s “seed writes 0 outbox” is **stale**.

---

## 5. What is mapped but never consumed

```22:22:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
```

```108:113:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
        modelBuilder.Entity<OutboxEvent>(e =>
        {
            e.ToTable("outbox_events");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProcessedAt);
        });
```

EF persists `Type` as **int** (no `HasConversion`, no `text`). Index on `ProcessedAt` is non-unique — the fat drain index A98 forbids; there is no pending partial, no dedupe UK. `EnsureCreated` will create this table on a real Postgres string; default DI is still InMemory when the connection string is missing/`<SECRET>` (`DependencyInjection.cs` L22–24).

```26:33:D:\Prop\apps\api\Program.cs
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow, details = "demo FakeMt5BrokerConnector — not live Manager" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "no live TLS socket" } },
    database = new { name = "postgres-or-inmemory", healthy = true, lastCheck = DateTimeOffset.UtcNow },
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
    outboxBacklog = 0
}));
```

After a successful seed the pending count is **4**, not 0. The React type `HealthStatus.outboxBacklog` (`apps/web/src/types/index.ts:121`) is the only web mention; no page queries the table.

---

## 6. Entity + enum (unchanged shape)

```1:16:D:\Prop\src\Domain\Entities\OutboxEvent.cs
using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class OutboxEvent
{
    public Guid Id { get; set; }
    public OutboxEventType Type { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public string? CorrelationId { get; set; }
}
```

```1:10:D:\Prop\src\Domain\Enums\OutboxEventType.cs
namespace TraderIntelligence.Domain.Enums;

public enum OutboxEventType
{
    TradeCompleted = 0,
    ScoreUpdate = 1,
    ShadowCopyIntent = 2,
    RiskCheckRequest = 3,
    NotificationEvent = 4
}
```

No Phase-1 ingest kind (`DealPersisted`). A41 kebab-case `event_type` text is not what EF stores.

---

## 7. Stale reports (produce claim only)

Use **this file** for “is it written?”. C58 remains valid for **drain** (no processor, no `ProcessedAt` writer, no `mt5_outbox_backlog` query).

| Report | Stale claim | Now |
|---|---|---|
| C58 §0 / §5.2 | `0` `OutboxEvents.Add`; seeder writes 0 | **1** Add; seed writes **4** |
| C59 | `OutboxEvents.Add` / `new OutboxEvent` = 0 | **1** |
| B03 row 41 / D03 | `EfTradingStore` never writes outbox | **writes** `ScoreUpdate` |
| D20 §1 grep | store: `0` `OutboxEvent`; 250 lines / `05103CE5…` | 338 lines / `DC03BBE6…` |
| D22 §? “not written: OutboxEvents” | seeder does not insert outbox | seeder **does**, via `RebuildTraderAsync` |
| D31 “Outbox produce / claim \| 0” | worker does not produce | worker **produces** every 30 s via rebuild |
| C16 seed table | `OutboxEvents` = 0 | **4** after seed |

---

## 8. What this is **not**

Architecture §12:

```text
MT5 event → validate → deduplicate → persist raw → write transactional outbox → commit
Then background workers process the outbox.
```

Measured:

1. Fake deal upsert commits **without** an outbox row.
2. Rebuild scores **in-process**, then writes a `ScoreUpdate` souvenir in a **later** `SaveChanges`.
3. Nobody claims `ProcessedAt IS NULL`.
4. Health always reports backlog 0.

Do **not** tick A41 / A90 `OutboxProcessingTests` / §58 `mt5_outbox_backlog` from this insert. Do **not** treat C++ `terminal_*outbox*` counters as this entity.

---

## 9. Answer

**Yes — `OutboxEvent` is written**, exactly once in product source (`EfTradingStore.PersistDemoShadowAsync`), as `OutboxEventType.ScoreUpdate`, on every `RebuildTraderAsync`. Seed produces **4** pending rows. Nothing else writes the type; nothing drains it.
