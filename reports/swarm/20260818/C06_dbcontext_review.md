# C06 — `TraderDbContext` compound-key review

| Field | Value |
|---|---|
| Agent | C06 (senior engineer, compound-key / identity review only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (read-only pass) |
| Workspace | `D:\Prop` |
| Subject | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` (174 lines) |
| Entities | `D:\Prop\src\Domain\Entities\*.cs` (20 types) |
| Consumers | `EfTradingStore.cs`, `EfDashboardQueries.cs`, `apps\api\Program.cs` |
| Law | Architecture v2 **§10** (lines 479–496); A20 catalog; A21 reconstruction identity; A61 EF contract; A98 index contract |
| Siblings | B19 (table gap), B21 (type existence), B33 (entity ↔ §45) |
| Product source modified | **No.** This report is the only write. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

---

## 0. Verdict — Compound keys?

**No composite primary keys. Compound identity is unique indexes, not `HasKey` tuples.**

Every mapped entity uses a surrogate `Guid Id` via `e.HasKey(x => x.Id)`. There is **zero** `HasKey(x => new { … })`, **zero** `HasAlternateKey`, **zero** `HasPrincipalKey`, **zero** `HasForeignKey`, **zero** `HasDatabaseName`. Architecture §10 + A61 **want** that surrogate-PK shape; the real identity is supposed to be a **named UNIQUE** on `(broker_id, ticket)` (or the other compound laws). That UNIQUE layer is only **partially** present.

| Question | Measured answer |
|---|---|
| Composite `HasKey` (multi-column PK) | **0** |
| `HasAlternateKey` | **0** |
| Compound **unique** indexes (the actual compound keys) | **7** |
| Compound **non-unique** indexes (lookups, not keys) | **3** |
| Single-column unique indexes | **5** |
| Named A20 `*_uk` / A61 `*_ix` (`HasDatabaseName`) | **0** |
| Compound FKs (`HasForeignKey` + `HasPrincipalKey`) | **0** |
| Forbidden global unique on `Login` / `DealTicket` / `PositionTicket` alone | **0** (clean) |
| §10 laws fully enforced in this model | **2 of 4 tables that exist; 2 required tables missing** |

Honest class of the current identity model: **`EXISTS_NEEDS_REFACTOR`**. Do not call this “§10 complete.” Two source ticket identities (`mt5_accounts`, `mt5_deals`) match the law. Positions are approximate. Reconstructed trades **fail**. Orders and trader-states tables **do not exist**.

---

## 1. Direct answer

```text
Q: Does the new TraderDbContext use compound keys?
A: Not as primary keys. All 20 PKs are Guid Id.

    Compound identity is implemented as unique indexes on 7 tables:
      mt5_groups              (BrokerId, Name)
      mt5_accounts            (BrokerId, Login)            ← §10 YES
      mt5_deals               (BrokerId, DealTicket)       ← §10 YES
      mt5_positions_current   (BrokerId, PositionTicket)   ← §10 APPROX
      source_symbol_mappings  (BrokerId, SourceSymbol)
      trader_scores           (BrokerId, Login)
      sync_checkpoints        (BrokerId, Login, Stream)    ← WRONG SHAPE

    Three more multi-column indexes exist but are NOT unique
    (so they are not keys):
      mt5_deals               (BrokerId, Login, DealTime)
      reconstructed_trades    (BrokerId, Login, PositionId, OpenedAt)  ← IDENTITY FAIL
      trader_score_history    (BrokerId, Login, RecordedAt)
```

This is the A61-intended pattern (surrogate UUID PK + natural UNIQUE). It is **not** a composite-PK design, and it is **not** finished.

---

## 2. What was read (no product edits)

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 20 `DbSet`s; all fluent maps inline in `OnModelCreating` (lines 33–173) |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\` | **Empty** (0 files). No `IEntityTypeConfiguration<T>`. |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Upserts by LINQ `(BrokerId, …)`, not `ON CONFLICT` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Leaderboard groups by `(BrokerId, Login)` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | InMemory or `UseNpgsql`; no snake_case, no migrations |
| `D:\Prop\src\Domain\Entities\*.cs` | 20 types; every type has `Guid Id` |
| `D:\Prop\apps\api\Program.cs` | `GET /api/trades` filters `Login` **without** `broker_id` |
| Architecture §10 | Compound identity law (quoted §3) |
| A20 / A21 / A61 / A98 / B19 | Expected UNIQUE names and the reconstructed-trade conflict |

Grep of `D:\Prop\src` for `HasAlternateKey`, `HasPrincipalKey`, `HasForeignKey`, `HasDatabaseName`, `IEntityTypeConfiguration`, `ApplyConfigurationsFromAssembly`, `UseSnakeCaseNamingConvention`: **0 hits**.

---

## 3. Architecture §10 — binding law

Source: `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 479–496.

```text
Never assume login or ticket IDs are globally unique.

Use compound identities:

broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id

All source-side tables must carry:

broker_id
```

A20 / A61 implement that as **unique indexes**, not composite PKs:

```csharp
builder.HasKey(x => x.Id);                                 // surrogate
builder.HasIndex(x => new { x.BrokerId, x.DealTicket })
    .IsUnique()
    .HasDatabaseName("mt5_deals_identity_uk");             // the real key
```

A21 **overrides** A20 on reconstructed trades: UNIQUE is `(broker_id, login, position_id, lifecycle_seq)`, **not** `(broker_id, position_id)` alone (ticket reuse / `ENTRY_INOUT`). A98 pins the same A21 key. C06 judges reconstructed trades against **A21**, not the older A20 position-only UK.

---

## 4. Full fluent inventory (every key and index)

Source of truth: `TraderDbContext.OnModelCreating`. No configuration classes. No named constraints.

| # | Table | CLR | PK | Unique index(es) | Other index(es) | Compound-key class |
|---|---|---|---|---|---|---|
| 1 | `brokers` | `Broker` | `Id` | `Code` | — | Single-col UK. Not a §10 compound. |
| 2 | `mt5_groups` | `Mt5Group` | `Id` | **`(BrokerId, Name)`** | — | **YES** — matches A20 `(broker_id, group_name)` (property is `Name`) |
| 3 | `mt5_accounts` | `Mt5Account` | `Id` | **`(BrokerId, Login)`** | — | **YES — §10** |
| 4 | `mt5_deals` | `Mt5Deal` | `Id` | **`(BrokerId, DealTicket)`** | `(BrokerId, Login, DealTime)` non-unique | **YES — §10**. Lookup is 3-col; A98 wants 4-col `+ DealTicket` |
| 5 | `mt5_positions_current` | `Mt5Position` | `Id` | **`(BrokerId, PositionTicket)`** | — | **APPROX — §10**. Property is `PositionTicket`, not `PositionId`. Missing `(BrokerId, Login)` replace index |
| 6 | `reconstructed_trades` | `ReconstructedTrade` | `Id` | **none** | `(BrokerId, Login, PositionId, OpenedAt)` **non-unique** | **FAIL.** Not a key. No `lifecycle_seq`. A21 UK missing |
| 7 | `canonical_instruments` | `CanonicalInstrument` | `Id` | `Code` | — | Single-col UK (`canonical_symbol` analog) |
| 8 | `source_symbol_mappings` | `SourceSymbolMapping` | `Id` | **`(BrokerId, SourceSymbol)`** | — | **YES** — A20 shape |
| 9 | `trader_scores` | `TraderScore` | `Id` | **`(BrokerId, Login)`** | — | **YES** as wide current-row collapse (A20/A61 allow). No `score_kind`. Folds `trader_states` + flags |
| 10 | `trader_score_history` | `TraderScoreHistory` | `Id` | **none** | `(BrokerId, Login, RecordedAt)` non-unique | **Not a key.** A20 wants `(broker_id, login, completed_trade_count, score_kind, model_version_id)` |
| 11 | `outbox_events` | `OutboxEvent` | `Id` | **none** | `ProcessedAt` non-unique | **FAIL.** No dedupe UK. Fat drain index |
| 12 | `sync_checkpoints` | `SyncCheckpoint` | `Id` | **`(BrokerId, Login, Stream)`** | — | **WRONG SHAPE.** Forces a login. Cannot represent broker/venue/global scopes (A20 `(scope_type, scope_id, stream_name)`) |
| 13 | `copy_intents` | `CopyIntent` | `Id` | `IdempotencyKey` | — | Opaque string, not A20 `(source_broker_id, source_login, source_trade_id, source_event_id, action)` |
| 14 | `risk_decisions` | `RiskDecisionRecord` | `Id` | **none** | `CopyIntentId` non-unique | **FAIL.** Missing `(copy_intent_id, decision_seq)` |
| 15 | `execution_intents` | `ExecutionIntent` | `Id` | `ClOrdId` | — | **YES** for §33 / A20 `execution_intents_clord_uk`. Missing `execution_intents_risk_uk` |
| 16 | `shadow_orders` | `ShadowOrder` | `Id` | **none** | — | **FAIL.** No `shadow_cl_ord_id` |
| 17 | `destination_quotes` | `DestinationQuoteSnapshot` | `Id` | **none** | — | **FAIL.** No `(venue_id, instrument_id)`. No `VenueId` on the type |
| 18 | `fix_sessions` | `FixSessionState` | `Id` | `Qualifier` **global** | — | **UNSAFE shape.** One QUOTE + one TRADE for the whole platform. Must be `(venue_id, session_qualifier)` |
| 19 | `audit_logs` | `AuditLog` | `Id` | none (correct for an append stream) | — | PK-only OK. A20 wants `bigint IDENTITY`, not Guid |
| 20 | `kill_switches` | `KillSwitch` | `Id` | **none** | — | Extra vs §45. No singleton / current-mode unique |

**Count:** 20 `HasKey(Id)` + **17** `HasIndex` call sites (7 unique compound + 5 unique single + 3 non-unique compound + 2 non-unique single).

`HasIndex` call sites in the file (measured):

1. `Broker.Code` unique
2. `Mt5Group (BrokerId, Name)` unique
3. `Mt5Account (BrokerId, Login)` unique
4. `Mt5Deal (BrokerId, DealTicket)` unique
5. `Mt5Deal (BrokerId, Login, DealTime)` not unique
6. `Mt5Position (BrokerId, PositionTicket)` unique
7. `ReconstructedTrade (BrokerId, Login, PositionId, OpenedAt)` not unique
8. `CanonicalInstrument.Code` unique
9. `SourceSymbolMapping (BrokerId, SourceSymbol)` unique
10. `TraderScore (BrokerId, Login)` unique
11. `TraderScoreHistory (BrokerId, Login, RecordedAt)` not unique
12. `OutboxEvent.ProcessedAt` not unique
13. `SyncCheckpoint (BrokerId, Login, Stream)` unique
14. `CopyIntent.IdempotencyKey` unique
15. `RiskDecisionRecord.CopyIntentId` not unique
16. `ExecutionIntent.ClOrdId` unique
17. `FixSessionState.Qualifier` unique

**17 indexes.** Shadow / destination quotes / audit / kill-switch: PK only.

---

## 5. §10 compound-identity matrix

| Law | Required table / UNIQUE | In `TraderDbContext` | Class |
|---|---|---|---|
| `broker_id + login` | `mt5_accounts` → `mt5_accounts_identity_uk` | **Yes** `(BrokerId, Login)` unique, unnamed | `EXISTS_AND_GOOD` (name missing) |
| `broker_id + login` | `trader_states` → `trader_states_identity_uk` | **No table.** State lives on `TraderScore.CurrentState` | `MISSING` |
| `broker_id + deal_ticket` | `mt5_deals` → `mt5_deals_identity_uk` | **Yes** `(BrokerId, DealTicket)` unique, unnamed | `EXISTS_AND_GOOD` (name missing) |
| `broker_id + order_ticket` | `mt5_orders` → `mt5_orders_identity_uk` | **No table, no type** | `MISSING` |
| `broker_id + position_id` | `mt5_positions_current` → `mt5_positions_current_identity_uk` | **Approx** `(BrokerId, PositionTicket)` unique | `EXISTS_NEEDS_REFACTOR` |
| `broker_id + position_id` (+ A21 `login` + `lifecycle_seq`) | `reconstructed_trades` | **No unique.** 4-col lookup includes `OpenedAt` | `UNSAFE` as identity |

Same-law **correlation** columns that exist on mapped types but are **not** unique (and must not be uniqued alone):

| Type | Carries | Used as unique? | Required? |
|---|---|---|---|
| `CopyIntent` | `BrokerId`, `SourceLogin`, `SourceTradeId` | No — unique is `IdempotencyKey` | A20 wants the 5-col natural UK |
| `ExecutionIntent` | `BrokerId`, `SourceLogin`, `SourceTradeId` | No — unique is `ClOrdId` (correct) | Correlation only |
| `ShadowOrder` | `BrokerId`, `SourceLogin` | No | Need `shadow_cl_ord_id` |
| `Mt5Deal` | `Login`, `OrderTicket`, `PositionId` | Correctly **not** unique | Must stay non-unique |

Destination analogues (must **not** use `broker_id` as the uniqueness root):

| Destination law | Required UNIQUE | In this model |
|---|---|---|
| venue + instrument | `destination_quotes (venue_id, instrument_id)` | **Missing.** Type has no `VenueId` |
| venue + session | `fix_sessions (venue_id, session_qualifier)` | **Wrong.** Global `Qualifier` unique |
| client order id | `execution_intents.cl_ord_id` | **Present** (unnamed) |
| venue + ExecID | `fix_execution_reports` | **No table** |
| venue + account + dest position | `destination_positions` | **No table** |

---

## 6. What is correct (do not “fix” these)

1. **Surrogate `Id` PK on every table.** Matches A20/A61. Do **not** convert the 7 unique indexes into composite PKs. Outbox, audit, SignalR, and `source_trade_id` need a stable UUID.
2. **No global unique on `Login`, `DealTicket`, `OrderTicket`, `PositionTicket`, or `Symbol`.** Grep confirms none. Achiever `1001` and StarwaveFX `1001` can coexist. This is the one §10 invariant the fluent map already respects.
3. **`mt5_deals` does not unique `(BrokerId, OrderTicket)` or `(BrokerId, PositionId)`.** One order → many deals; many deals per lifecycle. Correct.
4. **`mt5_accounts` + `mt5_deals` unique compounds match §10 exactly** (column names aside). `EfTradingStore` upserts/dedups on those same pairs.
5. **`execution_intents.ClOrdId` unique** is the one FIX identity that already matches A20/§33.
6. **`source_symbol_mappings (BrokerId, SourceSymbol)`** is the right natural key. `XAUUSD` / `XAUUSD.` / `GOLD` stay broker-local.

---

## 7. Compound-key defects (ordered)

### 7.1 `reconstructed_trades` — identity FAIL (highest)

```csharp
e.HasKey(x => x.Id);
e.HasIndex(x => new { x.BrokerId, x.Login, x.PositionId, x.OpenedAt }); // NOT unique
```

- Not unique → two lifecycles can collide silently.
- `OpenedAt` is not `lifecycle_seq`. Two reopenings can share a coarse timestamp.
- Domain `ReconstructedTrade` has **no** `LifecycleSeq`.
- `EfTradingStore.ReplaceReconstructedAsync` **deletes all rows for `(BrokerId, Login)` and inserts new Guids.** `Id` is therefore **not** a stable `source_trade_id`. Copy/shadow/execution rows cannot hang off it.
- A21 / A98 required UK: `(broker_id, login, position_id, lifecycle_seq)` named `reconstructed_trades_lifecycle_uk`.
- Do **not** ship A20’s older `(broker_id, position_id)` unique — A21 closed that as wrong under netting reuse.

Class: **`UNSAFE`**.

### 7.2 `fix_sessions.Qualifier` globally unique — UNSAFE

```csharp
e.HasIndex(x => x.Qualifier).IsUnique();
```

`FixSessionQualifier` is a 2-value enum (`Quote` / `Trade`). This unique says the whole platform may have **one** QUOTE row and **one** TRADE row forever. A20/A61 require `(venue_id, session_qualifier)`. There is no `VenueId` on `FixSessionState` and no `execution_venues` table. `EfDashboardQueries.GetOverviewAsync` already does `SingleOrDefault` by qualifier — that query only works because the unique is global.

Class: **`UNSAFE`** as soon as a second venue exists; **wrong even for one venue** vs the catalog.

### 7.3 `sync_checkpoints` unique is too narrow

```csharp
e.HasIndex(x => new { x.BrokerId, x.Login, x.Stream }).IsUnique();
```

`SyncCheckpoint.Login` is `long` (required). Cannot represent:

- broker-level `deals_backfill` / `groups`
- venue-level `security_list` / `order_mass_status`
- global streams

A20: `(scope_type, scope_id, stream_name)`. Current UK will force a fake login (`0`?) for those streams and then collide.

Class: **`EXISTS_NEEDS_REFACTOR`**.

### 7.4 Outbox has no compound (or any) producer key

```csharp
e.HasIndex(x => x.ProcessedAt); // not unique, not partial
```

`OutboxEvent` has `Type`, `AggregateId` (string), no `AggregateType`, no `DedupeKey`, no `IdempotencyKey`, no `BrokerId`. A20/A61 `outbox_events_dedupe_uk (aggregate_type, aggregate_id, event_type, dedupe_key)` is **absent**. Same-commit idempotent insert cannot be expressed. The `ProcessedAt` index is the fat drain index A98 forbids.

Class: **`MISSING`** (dedupe UK) + **`UNSAFE`** (drain index).

### 7.5 Destination / shadow / risk — no upsert key

| Table | Required compound / business UK | Actual |
|---|---|---|
| `destination_quotes` | `(venue_id, instrument_id)` | PK only. Type has `CanonicalSymbol` + optional `VenueInstrumentId`, **no** `VenueId` |
| `shadow_orders` | `shadow_cl_ord_id` | PK only. Type is a flattened fill |
| `risk_decisions` | `(copy_intent_id, decision_seq)` | non-unique `CopyIntentId`. No `DecisionSeq` |
| `copy_intents` | 5-col natural key | opaque `IdempotencyKey` string |

Class: **`MISSING`** keys on types that exist.

### 7.6 Missing tables that *are* the compound keys

These §10 / A20 identities cannot be configured because the types do not exist:

| Missing table | Missing compound UK |
|---|---|
| `mt5_orders` | `(broker_id, order_ticket)` |
| `trader_states` | `(broker_id, login)` |
| `mt5_account_snapshots` | `(broker_id, login, snapshot_at)` |
| `trader_risk_flags` | `(broker_id, login, flag_code)` |
| `mt5_symbols` | `(broker_id, source_symbol)` |
| `execution_venues` | `venue_code` (root for all dest uniques) |
| `fix_execution_reports` | `(venue_id, exec_id)` |
| `destination_positions` | `(venue_id, destination_account, destination_position_id)` |
| `source_destination_links` | `(source_broker_id, source_trade_id, link_role, execution_intent_id)` |

Adding unique indexes to the current 20 types cannot close these.

### 7.7 Unnamed + PascalCase — indexes are intent, not DDL

No `HasDatabaseName`. No `EFCore.NamingConventions`. `ToTable("mt5_deals")` is snake_case; columns would persist as `"BrokerId"`, `"DealTicket"` if `UseNpgsql` + `EnsureCreated` ever ran. InMemory (the actual DI path when the connection is empty/`<SECRET>`) does **not** prove unique indexes the same way Postgres does.

Treat every fluent unique as **intent**. Nothing is applied as `mt5_deals_identity_uk`.

---

## 8. Store and API vs the keys (do they even use them?)

`EfTradingStore` looks up by the same compounds the fluent map uniques, **except** reconstructed trades:

| Method | Predicate | Matches a unique? |
|---|---|---|
| `ResolveBrokerIdAsync` | `Code` | Yes (`brokers_code`) |
| `UpsertGroupAsync` | `(BrokerId, Name)` | Yes |
| `UpsertAccountAsync` | `(BrokerId, Login)` | Yes |
| `UpsertDealAsync` | `(BrokerId, DealTicket)` | Yes (LINQ `Any` then insert — **not** `ON CONFLICT`) |
| `ReplacePositionsAsync` | delete `(BrokerId, Login)` then insert | Login filter has **no** supporting index |
| `LoadDealsAsync` | `(BrokerId, Login)` order `DealTime, DealTicket` | 3-col index; `ThenBy DealTicket` not covered |
| `ReplaceReconstructedAsync` | delete `(BrokerId, Login)` + new Guids | **Ignores** any position identity |
| `UpsertScoreAsync` | `(BrokerId, Login)` | Yes |

Race: two ingest workers can both miss `AnyAsync` on deals and then collide. InMemory last-write-wins; Postgres would throw an unnamed unique violation. There is no `ON CONFLICT ON CONSTRAINT mt5_deals_identity_uk DO NOTHING` (A78).

`GET /api/trades` (`apps\api\Program.cs` 63–70):

```csharp
if (login.HasValue)
    query = query.Where(t => t.Login == login.Value);
```

`broker` query-string is accepted and **ignored**. Filter is `Login` alone. That is a §10 violation in the API and **cannot use** any legal compound index. `GetTraderAsync` then filters the in-memory list by `login` after a broker-scoped load — better, but still not a keyed lookup.

---

## 9. Forbidden uniques — measured clean

A20 §8 / A61 §4.3 / A98 §9. None of these appear in `TraderDbContext`:

| Tempting unique | Present? |
|---|---|
| `Login` alone | **No** |
| `DealTicket` / `OrderTicket` / `PositionTicket` / `PositionId` alone | **No** |
| `Symbol` / `SourceSymbol` globally | **No** |
| `mt5_deals (BrokerId, OrderTicket)` unique | **No** |
| `mt5_deals (BrokerId, PositionId)` unique | **No** |
| `reconstructed_trades (BrokerId, PositionId)` unique | **No** (also missing the *correct* UK) |

The model does not make the classic multi-broker collision. It fails by **omission** (missing UKs) and by **wrong unique shape** (`Qualifier`, `SyncCheckpoint`, reconstructed non-unique), not by globally uniquing tickets.

---

## 10. Entity properties vs the compounds they claim

| Unique fluent tuple | Properties exist? | Type notes that break A20 names |
|---|---|---|
| `(BrokerId, Name)` groups | Yes | `Name` not `GroupName` |
| `(BrokerId, Login)` accounts / scores | Yes | `Login` is `long` (A61 wants `ulong`→`bigint` converter). Fine for lab values |
| `(BrokerId, DealTicket)` | Yes | `DealTicket` is `long`. `PositionId` exists on the deal (good) |
| `(BrokerId, PositionTicket)` | Yes | **Not** `PositionId`. A61 column pin is `position_id` |
| `(BrokerId, SourceSymbol)` mappings | Yes | Maps to `CanonicalInstrumentId` Guid, not `canonical_symbol` text |
| `(BrokerId, Login, Stream)` checkpoints | Yes | `Stream` not `StreamName`. No `ScopeType` / `ScopeId` |
| `(BrokerId, Login, PositionId, OpenedAt)` trades | Yes | No `LifecycleSeq` — cannot form the A21 UK without a Domain change |
| `ClOrdId` | Yes | Best single-col identity in the file |
| `Qualifier` | Yes | Enum int, not `QUOTE`/`TRADE` text. No `VenueId` |
| `IdempotencyKey` | Yes | Free string; no `SourceEventId` |

No fluent index references a missing property. The compounds that exist compile. The compounds that A21/A20 need often require **new properties first**.

---

## 11. Scorecard

```text
Composite primary keys                         0
HasAlternateKey                                0
Compound unique indexes                        7
  §10-correct among them                       2   (accounts, deals)
  §10-approximate                              1   (positions / PositionTicket)
  other-correct                                2   (groups, source symbols)
  allowed collapse                             1   (trader_scores wide row)
  wrong shape                                  1   (sync_checkpoints)
Compound non-unique indexes                    3
  of which should have been unique             1   (reconstructed_trades)
Single-col unique                              5
  correct                                      3   (broker code, instrument code, ClOrdId)
  opaque stand-in                              1   (copy IdempotencyKey)
  wrong                                        1   (fix Qualifier global)
PK-only tables with a required business UK     3   (shadow, dest quotes, risk seq)
§10 tables missing entirely                    2   (mt5_orders, trader_states)
Named *_uk                                     0
Compound FKs                                   0
Migrations                                     0
Configurations/*.cs                            0
```

**Do not convert the 7 unique indexes into composite PKs.** Name them, add the missing UKs (especially reconstructed `lifecycle_seq` + outbox dedupe + venue-scoped FIX), add compound FKs from deals/positions/trades → `mt5_accounts (BrokerId, Login)`, and stop filtering `/api/trades` by `login` alone.

---

## 12. Evidence quotes

`TraderDbContext` — every PK is `Id`; the only §10 uniques that are actually unique:

```csharp
e.ToTable("mt5_accounts");
e.HasKey(x => x.Id);
e.HasIndex(x => new { x.BrokerId, x.Login }).IsUnique();

e.ToTable("mt5_deals");
e.HasKey(x => x.Id);
e.HasIndex(x => new { x.BrokerId, x.DealTicket }).IsUnique();
e.HasIndex(x => new { x.BrokerId, x.Login, x.DealTime });

e.ToTable("mt5_positions_current");
e.HasKey(x => x.Id);
e.HasIndex(x => new { x.BrokerId, x.PositionTicket }).IsUnique();
```

The identity that is **not** a key:

```csharp
e.ToTable("reconstructed_trades");
e.HasKey(x => x.Id);
e.HasIndex(x => new { x.BrokerId, x.Login, x.PositionId, x.OpenedAt }); // no .IsUnique()
```

The destination unique that is globally wrong:

```csharp
e.ToTable("fix_sessions");
e.HasKey(x => x.Id);
e.HasIndex(x => x.Qualifier).IsUnique();
```

---

## 13. Relation to B19 / A61 / A98

| Doc | What it already said | What C06 adds |
|---|---|---|
| B19 | 18/43 tables; no FKs; reconstructed non-unique; outbox fail | Restricts the question to **keys**. Confirms still no composite PK. Counts 7 compound UKs vs 3 compound lookups |
| A61 | Target: surrogate PK + named `*_uk`; compound FKs | Current file is still the stub A61 said to replace. No naming, no configs |
| A98 | Deal login-time should be 4-col; drop `ProcessedAt`; lifecycle UK | Fluent deal lookup is still 3-col; outbox still `ProcessedAt`; reconstructed still the temporary 4-col |
| A21 | UNIQUE `(broker_id, login, position_id, lifecycle_seq)` | Domain + fluent still have no `lifecycle_seq` |

B19’s table-presence gap is unchanged. C06 does not re-litigate the 25 missing §45 tables except where they **are** a missing compound key (`mt5_orders`, `trader_states`).

---

## 14. What this agent did not do

- Did not edit any file under `D:\Prop\src`.
- Did not add `HasDatabaseName`, composite PKs, or configuration classes.
- Did not invent `LifecycleSeq` or `VenueId` on Domain types.
- Did not claim “schema ready,” “§10 complete,” or “≥95% mapped.”
- Did not run `dotnet ef` or hit Postgres — there are **0** migrations; DI is InMemory when the connection is empty/`<SECRET>`.

This file is the C06 compound-key record only.
