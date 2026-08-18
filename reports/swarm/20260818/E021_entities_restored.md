# E021 — `FixSessionState` / `CopyIntent` / `OutboxEvent` / `DestinationQuoteSnapshot` are classes matching the store

| Field | Value |
|---|---|
| Agent | E021 (entity kind + store-shape restore check only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (read-only; product source **not** edited) |
| Artifact | `D:\Prop\reports\swarm\20260818\E021_entities_restored.md` |
| Assigned | Confirm `FixSessionState`, `CopyIntent`, `OutboxEvent`, `DestinationQuoteSnapshot` are **classes** matching the store. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Workspace | `D:\Prop` |
| Prior incident | D47 / D48 / D51: mid-wave rewrite turned `DestinationQuote.cs` into `record DestinationQuote` and slimmed `CopyIntent` / `OutboxEvent` so `TraderDbContext` + `EfTradingStore` no longer compiled (`CS0246` `DestinationQuoteSnapshot`, then `CS0117` on store-assigned members). |
| Method | Full read of the four Entities files + `TraderDbContext` + `EfTradingStore` + `DemoSeeder` + `EfDashboardQueries` + fix-worker. Kind/`record` grep. Property inventory vs every store assignment. SHA-256. `dotnet build` Domain then Infrastructure Release. Prefer false negatives over fake PASS. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

---

## 0. Verdict

**CONFIRMED.** All four persist types are `public sealed class` with `{ get; set; }` members. Every name the store / `TraderDbContext` / seeder / dashboard / FIX worker uses exists on the matching class. Domain + Infrastructure Release builds are **0 errors / 0 warnings**.

| Type | Kind now | File | Store / `DbSet<T>` | Assigned members exist? | Class |
|---|---|---|---|---|---|
| `FixSessionState` | **class** | `Domain\Entities\FixSessionState.cs` | `DbSet<FixSessionState> FixSessionStates` | **Yes** (seeder + dashboard + fix-worker; store does not construct it) | `EXISTS_AND_GOOD` as a store CLR type |
| `CopyIntent` | **class** | `Domain\Entities\CopyIntent.cs` | `DbSet<CopyIntent> CopyIntents` | **Yes** — all 11 members `PersistDemoShadowAsync` assigns | `EXISTS_AND_GOOD` as a store CLR type |
| `OutboxEvent` | **class** | `Domain\Entities\OutboxEvent.cs` | `DbSet<OutboxEvent> OutboxEvents` | **Yes** — `Type` / `AggregateId` / `PayloadJson` / `OccurredAt` | `EXISTS_AND_GOOD` as a store CLR type |
| `DestinationQuoteSnapshot` | **class** | `Domain\Entities\DestinationQuote.cs` *(stem ≠ type)* | `DbSet<DestinationQuoteSnapshot> DestinationQuotes` | **Yes** — seeder write + store/dashboard read (`VenueInstrumentId`, `ReceivedAt`, …) | `EXISTS_AND_GOOD` as a store CLR type |

**One-line:** the D47 rewrite is **gone**. These four are mutable EF entity **classes** again, and their property names match `EfTradingStore` / `TraderDbContext` / `DemoSeeder`. That is a type-shape restore, not a claim that copy/outbox/FIX are production-complete.

Do **not** treat this as:

- A24 / A41 flow complete
- §45 / A20 / A61 schema complete
- `record DestinationQuote` in `Domain.Risk` being the persist type (it is not)
- Filename `DestinationQuote.cs` being the class name (the class is `DestinationQuoteSnapshot`)

---

## 1. Kind check (the assigned “are they classes?”)

Grep of `D:\Prop\src\Domain\Entities` for `public sealed (class|record|struct)`: **21** types, **all** `class`. Zero persist `record` / `struct` in that folder.

The four assigned declarations, verbatim:

```5:5:D:\Prop\src\Domain\Entities\FixSessionState.cs
public sealed class FixSessionState
```

```5:5:D:\Prop\src\Domain\Entities\CopyIntent.cs
public sealed class CopyIntent
```

```5:5:D:\Prop\src\Domain\Entities\OutboxEvent.cs
public sealed class OutboxEvent
```

```3:3:D:\Prop\src\Domain\Entities\DestinationQuote.cs
public sealed class DestinationQuoteSnapshot
```

`Select-String \brecord\b` over those four files: **no hits**.

Every persist property is `{ get; set; }` (EF-mutable). None is `init`-only. That is the shape `EfTradingStore` object-initializers and the fix-worker in-place mutation (`quote.UpdatedAt = …`) require.

Homonym that must **not** be counted as the persist type:

```24:30:D:\Prop\src\Domain\Risk\RiskEngine.cs
public sealed record DestinationQuote(
    string CanonicalSymbol,
    string? VenueInstrumentId,
    decimal Bid,
    decimal Ask,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? VenueTimestamp);
```

That record is the risk-engine value object. `TraderDbContext` maps `DestinationQuoteSnapshot`. The store **reads** the snapshot class, then **constructs** the risk record from matching property names (`CanonicalSymbol`, `VenueInstrumentId`, `Bid`, `Ask`, `ReceivedAt`, `VenueTimestamp`). Both names exist on the restored class.

---

## 2. Measured hashes (so later waves can detect another slimming)

| Path | Bytes | SHA-256 | LastWriteTimeUtc |
|---|---:|---|---|
| `D:\Prop\src\Domain\Entities\FixSessionState.cs` | 979 | `46C20D6A1BF5F84769DB483FD17A0EBEB8BDA8C1C56BBA2B8B30A59FCE44697E` | 2026-08-18T08:10:10.4801102Z |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | 951 | `C9AE3FF95058B72FC00A4DDBCBF2DFD68B7637D00D321244C376E2A1D6D9148B` | 2026-08-18T08:10:10.4801102Z |
| `D:\Prop\src\Domain\Entities\OutboxEvent.cs` | 546 | `78108643D4C8E25DBEA767C30145366B3337C59D6E39EA3F613B480CDE6649A8` | 2026-08-18T08:10:10.4801102Z |
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | 421 | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | 2026-08-18T08:09:32.7339916Z |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 2026-08-18T07:42:48.0601582Z |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 12097 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 2026-08-18T08:05:59.4835707Z |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 2026-08-18T08:04:59.2131544Z |

Versus B33 (pre-churn census) and D47 (broken 13:37 rewrite):

| Type | B33 hash / bytes | D47 broken | E021 now |
|---|---|---|---|
| `FixSessionState` | `46C20D6A…` / 979 | not rewritten | **same as B33** |
| `CopyIntent` | `33612349…` / 759 | `9BBDB6C1…` / 680 — dropped store members | **new** `C9AE3FF9…` / 951 — **superset** of store + B33 |
| `OutboxEvent` | `78108643…` / 546 | `0F5CDDF3…` / 485 — `EventType`/`Payload`/`CreatedAt` | **same as B33** |
| `DestinationQuoteSnapshot` | `E5CFED15…` / 421 | `47EDF6BD…` / 349 — `record DestinationQuote` | **same as B33** |

`TraderDbContext` hash is unchanged since D19/D20. The restore was **Entities only**. `Persistence\Configurations\` is **empty** (the D47 `ReconstructedTradesConfiguration.cs` CS0246 file is gone).

`CopyIntent` is not a byte-identical revert of B33. It **kept** the extra members the 13:37 rewrite introduced (`SourcePositionId`, `Direction`, `RiskDecisionId`, `ExecutionIntentId`) **and restored** every member the store assigns. That is a union restore. It still **matches** the store.

---

## 3. `TraderDbContext` pairing (the store’s EF surface)

```22:29:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<SyncCheckpoint> SyncCheckpoints => Set<SyncCheckpoint>();
    public DbSet<CopyIntent> CopyIntents => Set<CopyIntent>();
    …
    public DbSet<DestinationQuoteSnapshot> DestinationQuotes => Set<DestinationQuoteSnapshot>();
    public DbSet<FixSessionState> FixSessionStates => Set<FixSessionState>();
```

Fluent maps (same file):

| `T` | `ToTable` | Key | Extra index |
|---|---|---|---|
| `OutboxEvent` | `outbox_events` | `Id` | non-unique `ProcessedAt` |
| `CopyIntent` | `copy_intents` | `Id` | **unique** `IdempotencyKey` |
| `DestinationQuoteSnapshot` | `destination_quotes` | `Id` | none |
| `FixSessionState` | `fix_sessions` | `Id` | **unique** `Qualifier` |

Those fluent members exist on the classes: `OutboxEvent.ProcessedAt`, `CopyIntent.IdempotencyKey`, `FixSessionState.Qualifier`, `*.Id`. D47’s slim `CopyIntent` had **no** `IdempotencyKey` — `HasIndex(x => x.IdempotencyKey)` would not compile. It compiles now.

---

## 4. Property match vs every store writer / reader

### 4.1 `OutboxEvent` ← `EfTradingStore.PersistDemoShadowAsync`

Store assigns:

| Member assigned | On class? |
|---|---|
| `Id` | yes `Guid` |
| `Type = OutboxEventType.ScoreUpdate` | yes `OutboxEventType Type` (not `EventType`) |
| `AggregateId` | yes `string` |
| `PayloadJson` | yes `string` (not `Payload`) |
| `OccurredAt` | yes `DateTimeOffset` (not `CreatedAt`) |

Unassigned but present (harmless defaults): `ProcessedAt`, `Attempts`, `LastError`, `CorrelationId`. The `ProcessedAt` index in `TraderDbContext` is valid.

### 4.2 `CopyIntent` ← `PersistDemoShadowAsync`

Store assigns:

| Member assigned | On class? |
|---|---|
| `Id` | yes |
| `BrokerId` | yes |
| `SourceLogin` | yes |
| `CanonicalSymbol` | yes (default `"XAUUSD"`) |
| `Action = CopyIntentAction.OpenExposure` | yes `CopyIntentAction` |
| `RequestedQuantity` | yes (D47 had renamed this to `VolumeLots`) |
| `ExpectedPrice` | yes (D47 had renamed this to `SourcePrice`) |
| `SourceEventTime` | yes (D47 deleted) |
| `CreatedAt` | yes |
| `ExpiresAt` | yes (D47 deleted) |
| `Status = "SHADOW_ONLY"` | yes (D47 deleted) |
| `IdempotencyKey` | yes — also the unique index + `AnyAsync` predicate (D47 deleted) |

Lookup uses `c.IdempotencyKey == key`. That property exists.

Unassigned extras (defaults only; do not break the store): `SourceTradeId`, `SourcePositionId` (0), `Direction` (`Long=0`), `RiskDecisionId`, `ExecutionIntentId`.

### 4.3 `DestinationQuoteSnapshot` ← seeder write + store/dashboard read

Seeder (`DemoSeeder` L105–113) assigns: `Id`, `CanonicalSymbol`, `VenueInstrumentId`, `Bid`, `Ask`, `ReceivedAt`.

Store reads (`EfTradingStore` L273–287): `ReceivedAt` (order + map), `CanonicalSymbol`, `VenueInstrumentId`, `Bid`, `Ask`, `VenueTimestamp`.

Dashboard (`EfDashboardQueries` L165–182): `ReceivedAt`, `VenueInstrumentId`, `Bid`, `Ask`.

| Member used | On class? |
|---|---|
| `Id` | yes |
| `CanonicalSymbol` | yes |
| `VenueInstrumentId` | yes (D47 used `CTraderInstrumentId`) |
| `Bid` / `Ask` | yes |
| `ReceivedAt` | yes (D47 used `QuoteReceivedAt`) |
| `VenueTimestamp` | yes |

Type name used at every persist site is `DestinationQuoteSnapshot`, not `DestinationQuote`. `new DestinationQuoteSnapshot { … }` in the seeder compiles.

### 4.4 `FixSessionState` ← seeder + dashboard + FIX worker

`EfTradingStore` does **not** construct `FixSessionState`. The persistence store still owns the type via `DbSet<FixSessionState>`.

Seeder assigns two rows (`Qualifier` Quote / Trade): `Id`, `Qualifier`, `Status`, `Host`, `Port`, `SenderCompId`, `TargetCompId`, `SenderSubId`/`TargetSubId`, `InboundSeq`, `OutboundSeq`, `LastInboundAt`, `LastOutboundAt`, `LastError`, `UpdatedAt`.

Dashboard reads: `Qualifier`, `Status`, `Host`, `Port`, `LastInboundAt`, `LastOutboundAt`, `InboundSeq`, `OutboundSeq`, `ReconnectCount`, `LastError`.

FIX worker mutates: `UpdatedAt`, `Status`, `LastError`; filters on `Qualifier`.

All of those members exist. Also present, unused by current writers: `OwnerHeld`, `OwnerInstance`, `ReconnectCount` (read by dashboard, default 0).

---

## 5. What D47 broke (so “restored” is measurable)

D47 §5 (13:37 rewrite, **no longer on disk**):

| Type then | Why the store broke |
|---|---|
| `CopyIntent` slim class | Writer still set `RequestedQuantity`, `ExpectedPrice`, `SourceEventTime`, `ExpiresAt`, `Status`, `IdempotencyKey`. Context still indexed `IdempotencyKey`. Those members were gone. |
| `OutboxEvent` renamed | Writer set `Type` / `AggregateId` / `PayloadJson` / `OccurredAt`. Entity had `EventType` / `Payload` / `CreatedAt`. |
| `DestinationQuote.cs` | Became `record DestinationQuote(...)`. Context L28 `DbSet<DestinationQuoteSnapshot>` → **CS0246**. Seeder `new DestinationQuoteSnapshot` → CS0246. Store `quoteRow.ReceivedAt` / `VenueInstrumentId` → CS0117. |
| `FixSessionState` | Not part of that rewrite. Remained a class (this pass: same SHA as B33). |

Those three defects are **absent** on the current tree. The restore timestamps (08:09–08:10Z) are **after** the store writer (08:05:59Z) and **after** D47’s 13:37 local rewrite.

---

## 6. Compile measurement (this pass)

```text
dotnet build D:\Prop\src\Domain\TraderIntelligence.Domain.csproj -c Release
  Build succeeded.  0 Warning(s)  0 Error(s)

dotnet build D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj -c Release
  Build succeeded.  0 Warning(s)  0 Error(s)
```

D47’s Infrastructure build was **exit 1** (`CS0246` `DestinationQuoteSnapshot` + leftover `ReconstructedTrades` configuration). That is **stale**.

A green Infrastructure build is the binding proof that:

1. `TraderDbContext` can name all four types.
2. `EfTradingStore` object initializers bind to real members (`Type`, `PayloadJson`, `RequestedQuantity`, `IdempotencyKey`, `VenueInstrumentId`, `ReceivedAt`, …).
3. `DemoSeeder` can `new FixSessionState` / `new DestinationQuoteSnapshot`.
4. `EfDashboardQueries` can read `FixSessionStates` / `DestinationQuotes`.

No `CS0246`, no `CS0117`.

---

## 7. What this does **not** claim

The assigned question is kind + store-shape. Adjacent honesty (do not launder into PASS):

| Topic | Measured |
|---|---|
| A24 production emit | Still demo-only `PersistDemoShadowAsync` (D47 control-flow still holds; C59 “zero writers” is stale). |
| Outbox `ShadowCopyIntent` | Writer still stamps `OutboxEventType.ScoreUpdate`. No dispatcher (C58). |
| Unique keys vs A20 | `IdempotencyKey` string UK; `destination_quotes` PK only; `fix_sessions.Qualifier` globally unique. Unchanged from D19. |
| Schema completeness | 18/43 §45 table **names**. This restore did not add tables. |
| `CopyIntent.Direction` / `SourcePositionId` | Exist, default 0. Store never sets them. Shadow fill uses `trade.Direction`, not `intent.Direction`. |
| `Risk.DestinationQuote` record | Still a separate type. Correct. Do not merge. |
| Filename rename | `DestinationQuote.cs` still ≠ `DestinationQuoteSnapshot`. B21 §4.1 still applies. |
| Live FIX / live MT5 | Not attempted. Irrelevant to this type check. |

Class of the **four types as store entities**: `EXISTS_AND_GOOD` for “is a mutable class the current store can compile against.” Class of the **copy/outbox/FIX features** those types sit in: still `EXISTS_NEEDS_REFACTOR` (demo skeleton). Those are different tickets.

---

## 8. Direct answer

**Yes.**

1. `FixSessionState` is a `public sealed class`. It matches `TraderDbContext.FixSessionStates`, `DemoSeeder`, `EfDashboardQueries`, and `apps/fix-worker/Worker.cs`.
2. `CopyIntent` is a `public sealed class`. It matches `TraderDbContext.CopyIntents` (including unique `IdempotencyKey`) and every member `EfTradingStore.PersistDemoShadowAsync` assigns.
3. `OutboxEvent` is a `public sealed class`. It matches `TraderDbContext.OutboxEvents` and the store’s `Type` / `AggregateId` / `PayloadJson` / `OccurredAt` initializer.
4. `DestinationQuoteSnapshot` is a `public sealed class` (file `DestinationQuote.cs`). It matches `TraderDbContext.DestinationQuotes`, the seeder insert, and store/dashboard reads (`VenueInstrumentId`, `ReceivedAt`, bid/ask).

The D47 record/slimming incident is **restored**. Domain + Infrastructure Release compile. Product source was not modified to produce this document.

---

## 9. Files cited

- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\OutboxEvent.cs`
- `D:\Prop\src\Domain\Entities\DestinationQuote.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (`record DestinationQuote` homonym)
- `D:\Prop\src\Domain\Enums\{CopyIntentAction,OutboxEventType,FixSessionQualifier,FixSessionStatus,TradeDirection}.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\reports\swarm\20260818\B21_dbcontext_type_mismatch.md`
- `D:\Prop\reports\swarm\20260818\B33_entity_table_gap.md`
- `D:\Prop\reports\swarm\20260818\D19_dbcontext.md`
- `D:\Prop\reports\swarm\20260818\D47_copyintent.md` (stale on compile; documents the break)
- `D:\Prop\reports\swarm\20260818\D48_shadow_rows.md` (noted mid-review restore)

---

*End of E021. Product source was not modified. Answer: all four are classes and match the store.*
