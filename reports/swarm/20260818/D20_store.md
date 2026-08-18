# D20 — `EfTradingStore` idempotency (measured)

| Field | Value |
|---|---|
| Agent | D20 (senior engineer, store idempotency only) |
| Date | 2026-08-18 |
| Assigned | Read `EfTradingStore.cs`. Idempotency? Write this report. Do not modify product source. |
| Primary SUT | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| Bytes / lines / SHA-256 | **9020** / **250** / `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` |
| Interface | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`ITradingStore`, lines 8–18) |
| Schema | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` |
| Product source modified | **No.** This report is the only write. |

Companions (do not treat as current measured code unless noted): `A78_deal_idempotency.md` (binding law; §10 tree snapshot is **stale** — the store now exists), `C06_dbcontext_review.md` (unique indexes), `C16_seed_test_review.md` (InMemory `Deal_upsert_is_idempotent`), `B03_infra_gap.md` (same store hash), `B32_ingestion_review.md` (caller), `A83_canceled_deals.md` (first-write-wins vs cancel mutation), `A59_ingestion_checkpoints.md`.

**Honesty rule:** sequential `AnyAsync` + `Add` is not `ON CONFLICT DO NOTHING`. An InMemory fact is not a Postgres unique-index proof. One current-row upsert is not an idempotent scoring pipeline if history appends every call.

---

## 0. Verdict

**PARTIAL. Sequential single-writer deal *identity* is first-write-wins. The store is not Phase-1 “idempotency proven.”**

`EfTradingStore` is a real 250-line `ITradingStore` (not a missing port — A78 §0/§10 is stale on that point). Natural keys include `broker_id`. Deal replay on the **same** `DbContext` does not insert a second `mt5_deals` row. That is the only proven slice (`SeedingAndStoreTests.Deal_upsert_is_idempotent`, InMemory).

It is **not**:

- durable `INSERT … ON CONFLICT (broker_id, deal_ticket) DO NOTHING`;
- concurrency-safe (check-then-insert TOCTOU on every write method);
- a four-way `DealUpsertOutcome` (Inserted / DuplicateSame / DuplicateConflict / Rejected);
- payload-hash first-write-wins with conflict audit;
- transactional with `ingestion_events` / outbox / `sync_checkpoints`;
- idempotent for score **history**, reconstructed-trade **surrogate ids**, or position **TimeUpdate**.

Classification vocabulary: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

| Method | Sequential identity | Concurrent writers | Payload policy | Class |
|---|---|---|---|---|
| `ResolveBrokerIdAsync` | Read-only | Safe (throws if missing) | n/a | `EXISTS_AND_GOOD` |
| `UpsertGroupAsync` | One row `(BrokerId, Name)` | TOCTOU → 23505 | Mixed FWW / LWW | `EXISTS_NEEDS_REFACTOR` |
| `UpsertAccountAsync` | One row `(BrokerId, Login)` | TOCTOU → 23505 | Mixed snapshot LWW | `EXISTS_NEEDS_REFACTOR` |
| `UpsertDealAsync` | One row `(BrokerId, DealTicket)` if check sees it | TOCTOU → 23505 (PG) or **two rows** (InMemory) | **First-write-wins; no hash** | `EXISTS_NEEDS_REFACTOR` |
| `ReplacePositionsAsync` | Snapshot replace per `(BrokerId, Login)` | Two replaces race unique `(BrokerId, PositionTicket)` | Last snapshot wins; new Guids | `UNSAFE` as upsert |
| `LoadDealsAsync` | Read, ordered | Safe | Maps `BrokerId` Guid → **broker code string** | `EXISTS_AND_GOOD` as read |
| `ReplaceReconstructedAsync` | Wipe + insert per login | **No unique** on recon table → **duplicate rows** | Last writer not even constrained | `UNSAFE` |
| `UpsertScoreAsync` | One current `(BrokerId, Login)` | TOCTOU → 23505 | LWW current; **always appends history** | `UNSAFE` for history |

**One-line:** safe enough for a single-threaded Fake replay of deals; **not** safe for the 30 s worker loop under two processes, not A78-complete, not a claim of “idempotency proven.”

---

## 1. What was read (no product edits)

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Entire file (8 public methods, 6 `SaveChangesAsync`) |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Port + `SyncBrokerAsync` + `RebuildTraderAsync` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | Unique / non-unique indexes the store relies on |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\` | **Empty** (0 files). All fluent maps live in the context. |
| `D:\Prop\src\Domain\Entities\Mt5{Deal,Group,Account,Position}.cs` | Persist shapes |
| `D:\Prop\src\Domain\Entities\{ReconstructedTrade,TraderScore,TraderScoreHistory,SyncCheckpoint}.cs` | Rebuild / score / unused checkpoint |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | DTO field sets (position DTO has **no** `Swap`) |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Seed → store |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Scoped store; InMemory unless a real connection string |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 30 s `SyncBrokerAsync` + `RebuildTraderAsync` |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Only store test |

Grep of the store file: **0** `ON CONFLICT`, **0** `BeginTransaction`, **0** `ExecuteUpdate` / `ExecuteDelete`, **0** `payload_hash`, **0** `SyncCheckpoint`, **0** `OutboxEvent`, **0** `ingestion_events`, **6** `SaveChangesAsync`.

---

## 2. Binding law this file is judged against

Architecture v2 §10 / §11 / §12 and A78 (persist-time deal law):

```text
Identity:           (broker_id, deal_ticket)   — never ticket alone
Raw deals:          immutable; first payload wins
SQL:                INSERT … ON CONFLICT (broker_id, deal_ticket) DO NOTHING
Duplicate:          same key, counted, not an error
Conflict:           same key, different hash — still no overwrite; audit
Not an upsert:      ON CONFLICT DO UPDATE of price/profit/volume  — forbidden
Phase 1 proven:     replay does not grow mt5_deals; unique index on real Postgres
```

Positions / scores are **not** the same law. Positions are a current snapshot (wipe-replace is a legal *shape*, but must be atomic and login-scoped). Scores are last-value current + append-only history; history must not double on identical rebuilds if the caller is a poll loop.

---

## 3. Method-by-method

### 3.1 `ResolveBrokerIdAsync` — read

```16:20:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task<Guid> ResolveBrokerIdAsync(string brokerCode, CancellationToken ct)
    {
        var broker = await _db.Brokers.SingleAsync(b => b.Code == brokerCode, ct);
        return broker.Id;
    }
```

- Unique `brokers.Code` exists. Idempotent read.
- Missing / duplicate code throws (`InvalidOperationException`). Fail-closed. Good.
- No cache. Fine.

### 3.2 `UpsertGroupAsync` — mixed FWW / LWW

Natural key `(BrokerId, Name)` matches unique index `mt5_groups (BrokerId, Name)`.

| Field | First insert | Later call |
|---|---|---|
| `Id` | `Guid.NewGuid()` | unchanged |
| `Currency` | set | **overwritten** (LWW) |
| `LastSyncedAt` | `now` | **overwritten** (LWW) |
| `CurrencyDigits`, `Company`, `MarginCall`, `MarginStopOut`, `ConnectionsAllowed` | set | **frozen** (accidental FWW) |
| `EnabledForAnalysis` | `true` | frozen |
| `LastDiscoveredAt` | `now` | frozen |

Replay of the same group: **one row**. Replay with a *changed* currency: last writer wins. Replay that only intended to refresh `CurrencyDigits` / margin: **silently ignored** after first insert. That is not a defined upsert policy.

TOCTOU: two workers both `SingleOrDefaultAsync` null → two `Add` → Postgres unique 23505 (uncaught) or InMemory **two rows**.

### 3.3 `UpsertAccountAsync` — mixed snapshot

Natural key `(BrokerId, Login)` matches unique index. §10 yes.

| Field | First insert | Later call |
|---|---|---|
| `GroupName`, `Balance`, `Equity`, `LastSyncedAt` | set | **overwritten** |
| `Leverage`, `Margin`, `MarginFree`, `Profit` | set | **frozen** |
| `RegistrationAt`, `LastAccessAt` | default | never set |

A 30 s poll therefore updates balance/equity but leaves `Profit` / `Margin` / `Leverage` at the **first** snapshot forever. Identity is idempotent; the snapshot is **not** a consistent last-write or first-write.

Same TOCTOU as groups.

### 3.4 `UpsertDealAsync` — the assigned question, narrow

```85:114:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task<bool> UpsertDealAsync(Guid brokerId, Mt5DealDto deal, DateTimeOffset now, CancellationToken ct)
    {
        var exists = await _db.Mt5Deals.AnyAsync(
            d => d.BrokerId == brokerId && d.DealTicket == deal.DealTicket, ct);
        if (exists)
            return false;

        _db.Mt5Deals.Add(new Mt5Deal
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            DealTicket = deal.DealTicket,
            // … all economic fields …
            IngestedAt = now
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }
```

**What is true (sequential, same context):**

1. Key is `(brokerId, deal.DealTicket)`, not ticket alone. §10 identity **shape** is correct.
2. Existing key → `false`, **no field update**. First-write-wins. Matches A78 “do not `DO UPDATE` economics.”
3. Schema unique index exists: `HasIndex(x => new { x.BrokerId, x.DealTicket }).IsUnique()`.
4. Cross-broker same numeric ticket is two rows (two `brokerId`s). Correct.
5. Integration fact `Deal_upsert_is_idempotent` (InMemory, same `db`): first `true`, second `false`, `Count == 1`.

**What is false / missing:**

| A78 / Phase-1 requirement | Measured |
|---|---|
| `ON CONFLICT DO NOTHING RETURNING` | **MISSING.** App-level `Any` then `Add`. |
| `payload_hash` + DuplicateSame vs DuplicateConflict | **MISSING.** Entity has no hash. Existing row is a blind skip. |
| Ticket `<= 0` reject | **MISSING.** Ticket `0` / negative persist. |
| `ingestion_events` in the same TX | **MISSING.** Store never writes it. Table/entity **absent**. |
| `mt5_deals_total` / `mt5_duplicate_deals_total` | **MISSING.** Bool only. |
| Concurrent two inserts of the same key | **UNSAFE.** Both can see `exists=false`. Postgres: one 23505 exception (caller sees failure, not “duplicate”). InMemory: unique indexes **not enforced** → **two rows**. |
| Unique-index proof on real Postgres | **MISSING.** Only InMemory test. EF InMemory does not honor `.IsUnique()`. |
| Same-TX outbox / checkpoint | **MISSING.** This `SaveChanges` is the whole TX. |

First-write-wins also means a later `OnDealUpdate` that rewrites action to `BUY_CANCELED` is **dropped** (A83). That is idempotent *identity* and **wrong* *latest-action* semantics. Do not call it a complete deal law.

`bool` is lossy (A78): `true` = inserted, `false` = any existing row. Caller (`DealIngestionService`) only counts inserts. Duplicates and conflicts are invisible.

### 3.5 `ReplacePositionsAsync` — snapshot, not an upsert

```116:142:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task ReplacePositionsAsync(Guid brokerId, long login, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct)
    {
        var existing = _db.Mt5Positions.Where(p => p.BrokerId == brokerId && p.Login == login);
        _db.Mt5Positions.RemoveRange(existing);
        foreach (var p in positions)
        {
            _db.Mt5Positions.Add(new Mt5Position { Id = Guid.NewGuid(), /* … */ TimeUpdate = DateTimeOffset.UtcNow });
        }
        await _db.SaveChangesAsync(ct);
    }
```

| Property | Effect on “idempotent replay” |
|---|---|
| Wipe then insert in **one** `SaveChanges` | Single call is atomic. Empty list = wipe. |
| `Id = Guid.NewGuid()` | Surrogate ids **change every poll**. Not byte-identical. |
| `TimeUpdate = DateTimeOffset.UtcNow` | Not the caller `now`. Replay always mutates this column. |
| `Swap` | **Never assigned.** Entity default `0`. DTO has no Swap (`Mt5PositionDto`). |
| Unique `(BrokerId, PositionTicket)` **not** including Login | Ticket reused across logins on one broker (rare but legal on some servers) collides. |
| Two concurrent replaces of the same login | Both load-to-delete, both insert → unique 23505 or leftover + new rows. |
| Two concurrent replaces of **different** logins that share a ticket | Unique is broker+ticket → one fails. |

Logical snapshot for a **single sequential writer** converges to the latest DTO list (except `Swap`/`TimeUpdate`/`Id`). That is last-snapshot-wins, **not** first-write-wins idempotency. The 30 s worker will rewrite the table every cycle.

`RemoveRange(IQueryable)` enumerates (loads) then marks deleted. It is not `ExecuteDelete`. Tracked entities from a previous replace in the same context can surprise if a caller reused the context across logins — scoped DI usually gives a fresh context per worker iteration (`using var scope`), so one cycle is OK.

### 3.6 `LoadDealsAsync` — read path for reconstruction

- Filter `(BrokerId, Login)`, order `DealTime` then `DealTicket`. Matches A21 input sort.
- Writes `NormalizedDeal.BrokerId = brokerCode` (**string code**, not Guid). Compiles; easy to pass the wrong identifier at a new call site (B01). Not an idempotency bug.
- Read is idempotent.

### 3.7 `ReplaceReconstructedAsync` — wipe without a unique key

Same wipe+insert pattern as positions, scoped `(BrokerId, Login)`.

Schema: `HasIndex(BrokerId, Login, PositionId, OpenedAt)` is **not unique** (C06: **UNSAFE** as identity). A21 wants `(broker_id, login, position_id, lifecycle_seq)`.

Consequences:

1. Sequential rebuild: one `SaveChanges` → one set of rows **for that call**. Replay replaces them. Logical content can match if the reconstructor is deterministic; **Ids are new Guids every time**.
2. Concurrent `RebuildTraderAsync` (two worker processes, or ingest + a manual rebuild): both `RemoveRange` + both `Add` → **duplicate reconstructed trades** because nothing unique stops the second insert after the first committed.
3. `DealTickets` from `ReconstructedTradeResult` are **not persisted**. Replay cannot prove ticket-set equality from the table.

### 3.8 `UpsertScoreAsync` — current row OK, history not idempotent

```215:249:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task UpsertScoreAsync(TraderScore score, CancellationToken ct)
    {
        var existing = await _db.TraderScores.SingleOrDefaultAsync(
            s => s.BrokerId == score.BrokerId && s.Login == score.Login, ct);
        // insert or copy fields onto existing …
        _db.TraderScoreHistory.Add(new TraderScoreHistory { Id = Guid.NewGuid(), /* … */ RecordedAt = score.LastScoredAt });
        await _db.SaveChangesAsync(ct);
    }
```

- Current `trader_scores`: unique `(BrokerId, Login)`. Sequential upsert keeps one row. LWW on all scored fields. Good as a *current* collapse.
- **Every call inserts a new `trader_score_history` row.** No hash, no `(broker, login, recorded_at)` unique, no “same scores → skip.”
- Caller `RebuildTraderAsync` sets `LastScoredAt = DateTimeOffset.UtcNow` and `Id = Guid.NewGuid()` every time.

`apps/mt5-worker` runs `RebuildTraderAsync` for four logins **every 30 seconds**. History grows ~8 rows/minute forever even if scores are unchanged. That is the opposite of idempotent.

TOCTOU on the current row is the same 23505 race as deals.

---

## 4. Transaction and caller surface

Every public write ends with its own `SaveChangesAsync`. There is **no** store-level batch and **no** ambient transaction.

`DealIngestionService.SyncBrokerAsync`:

```
ResolveBroker
for each group:    UpsertGroup + SaveChanges
for each account:  UpsertAccount + SaveChanges
                   for each deal: UpsertDeal + SaveChanges
                   ReplacePositions + SaveChanges
```

Crash mid-account: some deals committed, positions not replaced, later accounts untouched. Restart + overlap **does** skip already-inserted deals (sequential FWW) but **does not** write `sync_checkpoints` — the store never touches `SyncCheckpoint`. The next poll re-fetches the whole window (`Worker`: last 30 days). Overlap is cheap only if deal insert stays FWW **and** single-writer.

`ReconstructionScoringService.RebuildTraderAsync`: load → reconstruct → replace trades → upsert score+history. Separate from ingest. Worker does **both** every 30 s. Score history appends every cycle (see §3.8).

No `IRawMt5RecordWriter`. Growing a second persist path would violate A78; this store **is** the persist path today.

---

## 5. Schema the store actually leans on

From `TraderDbContext.OnModelCreating` (C06 inventory, re-verified):

| Table | Unique the store needs | Present? |
|---|---|---|
| `mt5_groups` | `(BrokerId, Name)` | **Yes** (unnamed) |
| `mt5_accounts` | `(BrokerId, Login)` | **Yes** |
| `mt5_deals` | `(BrokerId, DealTicket)` | **Yes** |
| `mt5_positions_current` | `(BrokerId, PositionTicket)` | **Yes** — not login-scoped |
| `reconstructed_trades` | A21 `(BrokerId, Login, PositionId, lifecycle_seq)` | **No unique** |
| `trader_scores` | `(BrokerId, Login)` | **Yes** |
| `trader_score_history` | none required for append; none to stop dup polls | **No unique** |
| `sync_checkpoints` | unused by store | unique `(BrokerId, Login, Stream)` exists, **never written** |
| `outbox_events` | unused | no dedupe UK; **never written** |

No `HasDatabaseName` (`mt5_deals_identity_uk` etc.). No `HasForeignKey` to `brokers`. No `payload_hash` column. No `ingestion_events` `DbSet`.

InMemory provider: unique indexes are **not** enforced. Production DI uses InMemory when the connection string is missing or contains `<SECRET>` (`DependencyInjection.cs`). So the default hosted path has **no** database backstop for the TOCTOU hole.

---

## 6. Tests that exist vs tests that would prove it

| Fact | What it proves | What it does not |
|---|---|---|
| `SeedingAndStoreTests.Deal_upsert_is_idempotent` | Sequential `Any`+`Add` on one InMemory context returns `false` the second time | Postgres unique, concurrency, hash conflict, ticket 0, two brokers, InMemory unique hole |
| `SeedingAndStoreTests.Demo_seed_…` | Happy-path seed once | Seed is `if (Brokers.Any()) return` — seeder-level, not store-level. Second `SeedAsync` is a no-op **before** any upsert |

Missing (A78 §9 / A90): `Mt5DealIdempotencyTests` on Postgres, `Unique_index_rejects_second_insert`, `Same_ticket_two_brokers`, `Conflict_does_not_overwrite`, concurrent two-context insert, `RebuildTrader` history-stable, `ReplacePositions` concurrent.

Do **not** tick A28 “idempotency proven” from the current suite.

---

## 7. Worker / multi-process reality

`D:\Prop\apps\mt5-worker\Worker.cs`: one hosted service, 30 s delay, `CreateScope` per iteration. **Single process** sequential: deal FWW holds; groups/accounts update the LWW subset; positions and reconstructed trades rewrite; score history **appends**.

Two replicas of `mt5-worker` against one Postgres (compose / ops mistake):

1. Deal insert race → 23505 → entire `SyncBrokerAsync` iteration’s remaining work aborted (`catch` on the worker loop).
2. Reconstruct race → duplicate `reconstructed_trades` (no unique).
3. Score insert race → 23505 or double history.

InMemory shared name `"trader-intelligence"` in DI: two scopes share one in-memory database **without** unique enforcement. Worse than Postgres.

---

## 8. Scorecard vs A78 persist-time machine

| Capability | Status |
|---|---|
| Compound identity `(broker_id, deal_ticket)` in the write predicate | **YES** |
| Unique index declared in EF model | **YES** (unnamed) |
| Unique index enforced on the default host (InMemory) | **NO** |
| `ON CONFLICT DO NOTHING` | **NO** |
| First-write-wins economics | **YES** (app-level skip) |
| Payload hash / conflict class | **NO** |
| Ticket validation | **NO** |
| `ingestion_events` | **NO** |
| Metrics `mt5_deals_total` / `mt5_duplicate_deals_total` | **NO** |
| Checkpoint after successful page | **NO** |
| Same-TX outbox | **NO** |
| Four-way outcome | **NO** (`bool`) |
| Concurrent-safe | **NO** |
| Postgres integration proof | **NO** |
| Group/account full-field policy | **NO** (subset LWW) |
| Position snapshot atomic per login | **YES** single-writer |
| Reconstructed unique | **NO** |
| Score history idempotent | **NO** |

**8 / 18** on this checklist, and the 8 are the cheap sequential ones.

---

## 9. Forbidden / do-not-claim

1. Do **not** claim “EX5-style 95%” or “idempotency proven.” Measured: sequential InMemory deal skip only.
2. Do **not** treat A78 §0 “`UpsertDealAsync` has no implementation” as current. Implementation **exists** (this file, hash above). The **SQL law** in A78 is still missing.
3. Do **not** add `ON CONFLICT DO UPDATE` of deal economics to “fix” cancels. A83 wants a **revision** / latest-read model, not a silent overwrite without audit.
4. Do **not** use Redis / an in-process `HashSet<long>` as the deal key.
5. Do **not** unique `deal_ticket` without `broker_id`.
6. Do **not** call `ReplacePositions` / `ReplaceReconstructed` “upserts.” They are snapshot replaces.
7. Do **not** count `TraderScoreHistory` growth as harmless telemetry while the worker rebuilds every 30 s.

---

## 10. Direct answer

**Is `EfTradingStore` idempotent?**

- **Deals, sequential, single writer:** **Yes, identity-level.** Same `(BrokerId, DealTicket)` does not insert again; payload is not compared; existing row is not updated.
- **Deals, concurrent or InMemory-as-prod:** **No.**
- **Deals vs A78 Phase-1 proof:** **No.**
- **Groups / accounts:** **Identity yes; fields inconsistent (partial LWW).**
- **Positions / reconstructed trades:** **Snapshot replace, not FWW idempotent; recon table can duplicate under concurrency.**
- **Scores:** **Current row yes; history no.**
- **Pipeline (ingest + rebuild every 30 s):** **Not idempotent** — history and snapshot tables churn; deal table is the only one that stays put.

Class of the type as a whole: **`EXISTS_NEEDS_REFACTOR`**. Safe enough for `DemoSeeder` + one test process. Not a collector you can run two of.

No product source was modified to produce this document.
