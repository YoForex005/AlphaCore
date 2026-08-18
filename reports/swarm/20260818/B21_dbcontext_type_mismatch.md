# B21 — `TraderDbContext` DbSet types vs `Domain\Entities` class names

| Field | Value |
|---|---|
| Agent | B21 (senior engineer, type-existence only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Left | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| Right | `D:\Prop\src\Domain\Entities\*.cs` (class names, not filenames) |
| Product source modified | **No.** This report is the only write. |
| Question | Which `DbSet<T>` / `Entity<T>` types **do not exist** as a type declared under `Domain\Entities`? |

---

## 0. Verdict

**No `TraderDbContext` type is missing from `Domain\Entities` class names.**

| Question | Count | Answer |
|---|---|---|
| `DbSet<T>` generic arguments | **20** | `TraderDbContext.cs` lines 12–31 |
| `modelBuilder.Entity<T>` arguments | **20** | same 20 types; lines 35–172 |
| Distinct declared types in `Domain\Entities` (`class` / `record` / `struct`) | **20** | one type per `.cs` file |
| `DbSet<T>` types with **no** matching Entities class | **0** | empty list |
| `Entity<T>` types with **no** matching Entities class | **0** | empty list |
| Entities class names with **no** `DbSet<T>` | **0** | empty list |

The type-existence check **PASS**. Do not treat a filename mismatch as a missing type (see §4).

---

## 1. Method

1. Extract every `DbSet<T>` from `TraderDbContext` (`grep DbSet<`).
2. Extract every `modelBuilder.Entity<T>` from the same file.
3. Extract every `class` / `record` / `struct` declaration under `D:\Prop\src\Domain\Entities`.
4. Compare **identifiers**, not filenames.
5. Namespace on every Entities file is `TraderIntelligence.Domain.Entities`. `TraderDbContext` has `using TraderIntelligence.Domain.Entities;`. Resolution is that namespace only.

---

## 2. Types that do not exist

**None.**

Empty inventory (this is the assigned list):

```text
(no missing DbSet<T> type)
(no missing Entity<T> type)
```

There is **no** `DbSet<T>` whose `T` is undeclared in `Domain\Entities`.

---

## 3. Pair table (all 20 exist)

| # | `DbSet<T>` property | `T` | Entities class | File |
|---|---|---|---|---|
| 1 | `Brokers` | `Broker` | `Broker` | `Broker.cs` |
| 2 | `Mt5Groups` | `Mt5Group` | `Mt5Group` | `Mt5Group.cs` |
| 3 | `Mt5Accounts` | `Mt5Account` | `Mt5Account` | `Mt5Account.cs` |
| 4 | `Mt5Deals` | `Mt5Deal` | `Mt5Deal` | `Mt5Deal.cs` |
| 5 | `Mt5Positions` | `Mt5Position` | `Mt5Position` | `Mt5Position.cs` |
| 6 | `ReconstructedTrades` | `ReconstructedTrade` | `ReconstructedTrade` | `ReconstructedTrade.cs` |
| 7 | `CanonicalInstruments` | `CanonicalInstrument` | `CanonicalInstrument` | `CanonicalInstrument.cs` |
| 8 | `SourceSymbolMappings` | `SourceSymbolMapping` | `SourceSymbolMapping` | `SourceSymbolMapping.cs` |
| 9 | `TraderScores` | `TraderScore` | `TraderScore` | `TraderScore.cs` |
| 10 | `TraderScoreHistory` | `TraderScoreHistory` | `TraderScoreHistory` | `TraderScoreHistory.cs` |
| 11 | `OutboxEvents` | `OutboxEvent` | `OutboxEvent` | `OutboxEvent.cs` |
| 12 | `SyncCheckpoints` | `SyncCheckpoint` | `SyncCheckpoint` | `SyncCheckpoint.cs` |
| 13 | `CopyIntents` | `CopyIntent` | `CopyIntent` | `CopyIntent.cs` |
| 14 | `RiskDecisions` | `RiskDecisionRecord` | `RiskDecisionRecord` | `RiskDecisionRecord.cs` |
| 15 | `ExecutionIntents` | `ExecutionIntent` | `ExecutionIntent` | `ExecutionIntent.cs` |
| 16 | `ShadowOrders` | `ShadowOrder` | `ShadowOrder` | `ShadowOrder.cs` |
| 17 | `DestinationQuotes` | `DestinationQuoteSnapshot` | `DestinationQuoteSnapshot` | `DestinationQuote.cs` |
| 18 | `FixSessionStates` | `FixSessionState` | `FixSessionState` | `FixSessionState.cs` |
| 19 | `AuditLogs` | `AuditLog` | `AuditLog` | `AuditLog.cs` |
| 20 | `KillSwitches` | `KillSwitch` | `KillSwitch` | `KillSwitch.cs` |

`OnModelCreating` uses the same 20 `T` values. No extra / no missing.

---

## 4. Not a missing type (do not merge into §2)

These are naming facts. They are **not** “type does not exist.”

### 4.1 Filename ≠ class name (one file)

| File | Declared class | `DbSet<T>` / `Entity<T>` |
|---|---|---|
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | `DestinationQuoteSnapshot` | `DestinationQuoteSnapshot` |

The class **exists**. The file stem is `DestinationQuote`. Anyone who compares `DbSet<T>` to **filenames** will falsely report `DestinationQuoteSnapshot` as missing.

There is **no** Entities type named `DestinationQuote`.

### 4.2 Homonym outside Entities

`D:\Prop\src\Domain\Risk\RiskEngine.cs` declares `public sealed record DestinationQuote(...)` in namespace `TraderIntelligence.Domain.Risk`. That is a risk-engine value object, not an EF entity, and it is **not** in the `Entities` folder. `TraderDbContext` does not reference it.

### 4.3 Property name ≠ type name (normal EF style)

| Property | Type | Note |
|---|---|---|
| `DestinationQuotes` | `DestinationQuoteSnapshot` | plural property, `Snapshot` suffix on type |
| `RiskDecisions` | `RiskDecisionRecord` | table-ish property, `Record` suffix on type |
| `TraderScoreHistory` | `TraderScoreHistory` | property is singular; others are plural |

Property identifiers are not types. A61’s note that `Brokers` / `OutboxEvents` / `CopyIntents` “do not exist in Domain” is stale: those are **property** names; the types `Broker`, `OutboxEvent`, `CopyIntent` exist.

---

## 5. Entities folder census (20 files, 20 types)

```text
AuditLog.cs                 -> AuditLog
Broker.cs                   -> Broker
CanonicalInstrument.cs      -> CanonicalInstrument
CopyIntent.cs               -> CopyIntent
DestinationQuote.cs         -> DestinationQuoteSnapshot   ← only stem/class split
ExecutionIntent.cs          -> ExecutionIntent
FixSessionState.cs          -> FixSessionState
KillSwitch.cs               -> KillSwitch
Mt5Account.cs               -> Mt5Account
Mt5Deal.cs                  -> Mt5Deal
Mt5Group.cs                 -> Mt5Group
Mt5Position.cs              -> Mt5Position
OutboxEvent.cs              -> OutboxEvent
ReconstructedTrade.cs       -> ReconstructedTrade
RiskDecisionRecord.cs       -> RiskDecisionRecord
ShadowOrder.cs              -> ShadowOrder
SourceSymbolMapping.cs      -> SourceSymbolMapping
SyncCheckpoint.cs           -> SyncCheckpoint
TraderScore.cs              -> TraderScore
TraderScoreHistory.cs       -> TraderScoreHistory
```

No extra entity type. No orphan file without a type declaration.

---

## 6. What this report does **not** claim

- Schema completeness vs architecture §45 / A20 / A61 (43-table catalog).
- Property-level column parity.
- Build or migration success.
- Whether `DestinationQuote.cs` should be renamed to `DestinationQuoteSnapshot.cs`.

Those are other tickets. This ticket is type existence only.

---

## 7. Binding list (repeat)

**Types that do not exist: (none).**
