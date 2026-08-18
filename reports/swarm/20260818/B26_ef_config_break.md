# B26 — EF `IEntityTypeConfiguration<T>` binds missing types

| Field | Value |
|---|---|
| Agent | B26 (senior engineer, EF configuration type-break only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:21:07+05:30 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Left (committed) | `HEAD:src/Infrastructure/Persistence/Configurations/*.cs` (5 files) + `HEAD:src/Infrastructure/Persistence/TraderDbContext.cs` |
| Left (working tree) | `D:\Prop\src\Infrastructure\Persistence\Configurations\` (**empty**) |
| Right | `D:\Prop\src\Domain\Entities\*.cs` class names (same as `HEAD:src/Domain/Entities`) |
| Question | Which `IEntityTypeConfiguration<T>` / `EntityTypeBuilder<T>` / `ApplyConfiguration` / `DbSet<T>` identifiers **do not exist** as Domain types? |
| Related (do not merge) | `B21_dbcontext_type_mismatch.md` = **working-tree** `TraderDbContext` vs Entities (**0** missing). `B03_infra_gap.md` / `B19_dbcontext_gap.md` = §45 table coverage. This file is the **committed config layer**. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**FAIL — every committed `IEntityTypeConfiguration<T>` binds a type that does not exist.**

The five files that *were* `BrokersConfiguration` and siblings are **not on disk**. `git status` shows them as deleted in the working tree (` D`). They still exist as blobs in `HEAD` (commit `6c41447`). Those blobs, plus the committed `TraderDbContext`, are the break.

| Layer | Bound identifier | Exists in `Domain.Entities`? |
|---|---|---|
| 5 committed configs | `Brokers`, `Mt5Groups`, `Mt5Accounts`, `Mt5Deals`, `Mt5Positions` | **No** (0 / 5) |
| Committed `DbSet<T>` | 20 types, 19 plural inventions + 1 real (`TraderScoreHistory`) | **1 / 20** |
| Committed `ApplyConfiguration(new X())` | 20 configuration classes | **5 files in HEAD, 0 on disk, 15 never created** |
| Working-tree `IEntityTypeConfiguration<T>` | — | **0 files** (folder empty) |
| Working-tree `TraderDbContext` `DbSet<T>` | 20 singular / real Domain types | **20 / 20** (B21) |

Restoring the five deleted files with `git checkout -- src/Infrastructure/Persistence/Configurations` would **re-introduce CS0246** into the current project, even if `OnModelCreating` never called them. SDK-style csproj compiles every `.cs` under the project.

Do **not** restore as-is. Do **not** invent Domain types `Brokers` / `Mt5Deals` to make the stubs compile. A78 / A61: entity names are singular (`Mt5Deal`, not `Mt5Deals`).

Working-tree Infrastructure **does compile** (`dotnet build TraderIntelligence.Infrastructure.csproj`: **0 errors, 0 warnings**). That is because the broken configs were deleted and `TraderDbContext` was rewritten to `DbSet<Broker>` + inline fluent. Compile-green is **not** a §45 model. See B03 / B19.

---

## 1. Method

1. `dir /a` on `D:\Prop\src\Infrastructure\Persistence\Configurations` — **0 files**, 837-byte empty directory, ACL open.
2. `git ls-files` / `git status --short` / `git show HEAD:…` for the five tracked configs + committed `TraderDbContext`.
3. `git grep IEntityTypeConfiguration HEAD -- *.cs` and `git grep ApplyConfiguration HEAD -- *.cs`.
4. Inventory every `class` / `record` / `struct` under `D:\Prop\src\Domain\Entities` (20 types). Confirm `HEAD:src/Domain/Entities` is the **same** singular set (`Broker`, `Mt5Group`, …).
5. Repo-wide `class Brokers` / `class Mt5Groups` / `class Mt5Deals` / `class Mt5Positions` / `class Mt5Accounts` over `*.cs` — **0** product hits.
6. Compare each committed shadow-property column set to the matching Domain CLR type and to A20 unique keys.
7. Build current Infrastructure (read-only). No product files written.

`git` identity of the deleted files (commit `6c41447`, single commit on this tree):

| Blob SHA-1 | Bytes | ~Lines | Path (tracked, deleted in WT) |
|---|---:|---:|---|
| `3c750cef2164aed16867f431f1102ec0f14d769d` | 837 | 23 | `src/Infrastructure/Persistence/Configurations/BrokersConfiguration.cs` |
| `6d5061c4d719470cd87c4f349d75a8094217e804` | 1042 | 26 | `src/Infrastructure/Persistence/Configurations/Mt5GroupsConfiguration.cs` |
| `9882f5daf3780ab43456a9a6351116cbd4e0cb95` | 1136 | 28 | `src/Infrastructure/Persistence/Configurations/Mt5AccountsConfiguration.cs` |
| `7f4544d7ffcf84e049b56deeaf5872422728377e` | 1868 | 38 | `src/Infrastructure/Persistence/Configurations/Mt5DealsConfiguration.cs` |
| `d1adf88966769b4dc6c50fb7026688d0011d9600` | 1820 | 37 | `src/Infrastructure/Persistence/Configurations/Mt5PositionsConfiguration.cs` |
| `6950c9373def993100f69e14210c02d016212883` | 3456 | 60 | `src/Infrastructure/Persistence/TraderDbContext.cs` (committed; **replaced** in WT) |

Working-tree `TraderDbContext.cs`: 5951 bytes, SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB`.

---

## 2. Types that do not exist (the assigned list)

### 2.1 `IEntityTypeConfiguration<T>` generic arguments (committed, 5 / 5 missing)

Every committed config has `using TraderIntelligence.Domain.Entities;` and then binds a **plural** name that was never declared there.

| File (HEAD) | Declaration | `T` | Closest real type | Status |
|---|---|---|---|---|
| `BrokersConfiguration.cs:7` | `IEntityTypeConfiguration<Brokers>` | `Brokers` | `Broker` | **MISSING** |
| `Mt5GroupsConfiguration.cs:7` | `IEntityTypeConfiguration<Mt5Groups>` | `Mt5Groups` | `Mt5Group` | **MISSING** |
| `Mt5AccountsConfiguration.cs:7` | `IEntityTypeConfiguration<Mt5Accounts>` | `Mt5Accounts` | `Mt5Account` | **MISSING** |
| `Mt5DealsConfiguration.cs:7` | `IEntityTypeConfiguration<Mt5Deals>` | `Mt5Deals` | `Mt5Deal` | **MISSING** |
| `Mt5PositionsConfiguration.cs:7` | `IEntityTypeConfiguration<Mt5Positions>` | `Mt5Positions` | `Mt5Position` | **MISSING** |

`Configure(EntityTypeBuilder<T> builder)` repeats the same five missing `T`s. That is a second CS0246 per file.

These are **not** property-name mistakes. They are type arguments. B21’s note that A61 “`Brokers` does not exist” is stale **for the working-tree DbSet property**. It is **true** for these configuration type arguments.

### 2.2 Committed `DbSet<T>` generic arguments (19 / 20 missing)

`HEAD:src/Infrastructure/Persistence/TraderDbContext.cs` lines 13–32:

| Line | `DbSet<T>` | `T` exists? | Real Domain type (if any) |
|---:|---|---|---|
| 13 | `DbSet<Brokers>` | **No** | `Broker` |
| 14 | `DbSet<Mt5Groups>` | **No** | `Mt5Group` |
| 15 | `DbSet<Mt5Accounts>` | **No** | `Mt5Account` |
| 16 | `DbSet<Mt5Deals>` | **No** | `Mt5Deal` |
| 17 | `DbSet<Mt5Positions>` | **No** | `Mt5Position` |
| 18 | `DbSet<ReconstructedTrades>` | **No** | `ReconstructedTrade` |
| 19 | `DbSet<CanonicalInstruments>` | **No** | `CanonicalInstrument` |
| 20 | `DbSet<SourceSymbolMappings>` | **No** | `SourceSymbolMapping` |
| 21 | `DbSet<TraderScores>` | **No** | `TraderScore` |
| 22 | `DbSet<TraderScoreHistory>` | **Yes** | `TraderScoreHistory` |
| 23 | `DbSet<TraderRiskFlags>` | **No** | none (A61 target `TraderRiskFlag` also absent) |
| 24 | `DbSet<OutboxEvents>` | **No** | `OutboxEvent` |
| 25 | `DbSet<SyncCheckpoints>` | **No** | `SyncCheckpoint` |
| 26 | `DbSet<CopyIntents>` | **No** | `CopyIntent` |
| 27 | `DbSet<RiskDecisions>` | **No** | `RiskDecisionRecord` (A61 wants `RiskDecision` — also absent) |
| 28 | `DbSet<ExecutionIntents>` | **No** | `ExecutionIntent` |
| 29 | `DbSet<ShadowOrders>` | **No** | `ShadowOrder` |
| 30 | `DbSet<ShadowFills>` | **No** | none (A61 target `ShadowFill` also absent) |
| 31 | `DbSet<DestinationQuotes>` | **No** | `DestinationQuoteSnapshot` (file `DestinationQuote.cs`) |
| 32 | `DbSet<FixSessionStates>` | **No** | `FixSessionState` |

**19 missing types. 1 hit (`TraderScoreHistory`).** Two of the 19 (`TraderRiskFlags`, `ShadowFills`) have **no** singular Domain stand-in at all.

### 2.3 Committed `new XxxConfiguration()` — 15 classes never in the tree

`OnModelCreating` (HEAD lines 36–55) constructs 20 configuration types. Only the first five had files. The other 15 have **never** existed as `.cs` files (`git ls-files` + recursive `*Configuration*.cs` under `D:\Prop` excluding `bin`/`obj`/`vendor` = only those five, and they are deleted in WT).

| HEAD line | Constructed type | File | Entity `T` it would have to bind (inferred) | Entity exists? |
|---:|---|---|---|---|
| 36 | `BrokersConfiguration` | HEAD only | `Brokers` | **No** |
| 37 | `Mt5GroupsConfiguration` | HEAD only | `Mt5Groups` | **No** |
| 38 | `Mt5AccountsConfiguration` | HEAD only | `Mt5Accounts` | **No** |
| 39 | `Mt5DealsConfiguration` | HEAD only | `Mt5Deals` | **No** |
| 40 | `Mt5PositionsConfiguration` | HEAD only | `Mt5Positions` | **No** |
| 41 | `ReconstructedTradesConfiguration` | **MISSING** | `ReconstructedTrades` | **No** |
| 42 | `CanonicalInstrumentsConfiguration` | **MISSING** | `CanonicalInstruments` | **No** |
| 43 | `SourceSymbolMappingsConfiguration` | **MISSING** | `SourceSymbolMappings` | **No** |
| 44 | `TraderScoresConfiguration` | **MISSING** | `TraderScores` | **No** |
| 45 | `TraderScoreHistoryConfiguration` | **MISSING** | `TraderScoreHistory` | Yes |
| 46 | `TraderRiskFlagsConfiguration` | **MISSING** | `TraderRiskFlags` | **No** |
| 47 | `OutboxEventsConfiguration` | **MISSING** | `OutboxEvents` | **No** |
| 48 | `SyncCheckpointsConfiguration` | **MISSING** | `SyncCheckpoints` | **No** |
| 49 | `CopyIntentsConfiguration` | **MISSING** | `CopyIntents` | **No** |
| 50 | `RiskDecisionsConfiguration` | **MISSING** | `RiskDecisions` | **No** |
| 51 | `ExecutionIntentsConfiguration` | **MISSING** | `ExecutionIntents` | **No** |
| 52 | `ShadowOrdersConfiguration` | **MISSING** | `ShadowOrders` | **No** |
| 53 | `ShadowFillsConfiguration` | **MISSING** | `ShadowFills` | **No** |
| 54 | `DestinationQuotesConfiguration` | **MISSING** | `DestinationQuotes` | **No** |
| 55 | `FixSessionStatesConfiguration` | **MISSING** | `FixSessionStates` | **No** |

Committed `TraderDbContext` therefore cannot compile. A64’s “does not compile as written” is correct **for HEAD**. It is **not** true of the working-tree rewrite.

### 2.4 Binding list (repeat — missing identifiers only)

```text
Brokers
Mt5Groups
Mt5Accounts
Mt5Deals
Mt5Positions
ReconstructedTrades
CanonicalInstruments
SourceSymbolMappings
TraderScores
TraderRiskFlags
OutboxEvents
SyncCheckpoints
CopyIntents
RiskDecisions
ExecutionIntents
ShadowOrders
ShadowFills
DestinationQuotes
FixSessionStates
ReconstructedTradesConfiguration
CanonicalInstrumentsConfiguration
SourceSymbolMappingsConfiguration
TraderScoresConfiguration
TraderScoreHistoryConfiguration
TraderRiskFlagsConfiguration
OutboxEventsConfiguration
SyncCheckpointsConfiguration
CopyIntentsConfiguration
RiskDecisionsConfiguration
ExecutionIntentsConfiguration
ShadowOrdersConfiguration
ShadowFillsConfiguration
DestinationQuotesConfiguration
FixSessionStatesConfiguration
```

`TraderScoreHistory` (the **entity**) exists. `TraderScoreHistoryConfiguration` (the **class**) does not.

---

## 3. What the five committed configs actually map (even if `T` existed)

All five are **property-bag / shadow-property** maps. They never write `x => x.Code`. They declare `builder.Property<TClr>("snake_name")` and `HasKey("id")`. The CLR type is only a marker.

Consequence: renaming `Brokers` → `Broker` is **not** enough.

- `Broker` already has CLR properties `Id`, `Code`, `DisplayName`, …
- The config adds **separate** shadows `id`, `code`, `name`, `created_at`.
- EF would emit **two column sets** (PascalCase CLR + snake_case shadows) unless every CLR property is ignored.
- `HasKey("id")` keys the shadow, not `Broker.Id`.

This is `UNSAFE` as a drop-in on the real Domain types.

No `HasOne` / `HasForeignKey`. No A20 `HasDatabaseName`. No `UseSnakeCaseNamingConvention` (and none in current DI either).

### 3.1 `BrokersConfiguration` → table `brokers`

| Shadow | Store type | Domain `Broker` | A20 / A61 |
|---|---|---|---|
| `id` PK uuid | uuid | `Id` | PK `id uuid` — name OK, mapping style not |
| `code` unique | text | `Code` (max 32 in WT inline) | `brokers_code_uk (code)` — unique is right; constraint unnamed |
| `name` | text | **no `Name`** — CLR is `DisplayName` | catalog is `code` + metadata; A61 wants `DisplayName` → `display_name` |
| `created_at` | timestamptz via `DateTime` | `CreatedAt` is `DateTimeOffset`; also `UpdatedAt` | `DateTime` + legacy timestamp switch is forbidden (A61 §2.2) |

Unmapped on `Broker`: `Server`, `Port`, `ManagerLogin`, `ServerName`, `Mode`, `PoolSize`, `Proxy*`, `Enabled`, `UpdatedAt`. Passwords are correctly absent (A58). Connection fields should not live on `brokers` long-term (`broker_connections`, A20 #2) — out of scope here.

### 3.2 `Mt5GroupsConfiguration` → table `mt5_groups`

| Shadow | Store type | Domain `Mt5Group` | A20 |
|---|---|---|---|
| `id` PK | uuid | `Id` | OK as surrogate |
| `broker_id` + index | uuid | `BrokerId` | required; **no FK** |
| `group_id` bigint + index | bigint | **no such property** | Groups are **string paths**, not integers (A39 / A57 / A59) |
| `name` | text | `Name` | A20 unique is `(broker_id, group_name)` |
| unique `(broker_id, group_id)` | | | **WRONG** vs `(broker_id, group_name)` |
| `created_at` | `DateTime` | no `CreatedAt`; has `LastDiscoveredAt` / `LastSyncedAt` | |

Unmapped: `Currency`, `CurrencyDigits`, `Company`, `MarginCall`, `MarginStopOut`, `ConnectionsAllowed`, `EnabledForAnalysis`, `PlanMapping`, discovery/sync timestamps.

### 3.3 `Mt5AccountsConfiguration` → table `mt5_accounts`

| Shadow | Domain `Mt5Account` | A20 |
|---|---|---|
| unique `(broker_id, login)` | `BrokerId` + `Login` | **correct identity** |
| `name` text | **no `Name`** (`GroupName` is the group path) | not an A20 identity column |
| `is_active` bool | **no such property** | not in current entity |
| `created_at` | `LastSyncedAt` / `RegistrationAt` / `LastAccessAt` | |

Unmapped money/risk fields: `Leverage`, `Balance`, `Equity`, `Margin`, `MarginFree`, `Profit`.

### 3.4 `Mt5DealsConfiguration` → table `mt5_deals`

A78 pin: entity **`Mt5Deal`**, unique `(broker_id, deal_ticket)`, `volume_native bigint`, `deal_time timestamptz`. **Do not invent `Mt5Deals`.**

| Shadow | Problem |
|---|---|
| type `Mt5Deals` | **MISSING** (A78 explicit ban) |
| unique `(broker_id, deal_ticket)` | identity is right |
| `volume` as `decimal` + `HasColumnType("bigint")` | **CLR/store type fight**. Domain is `ulong VolumeNative`. A38/A81: native integer units, not lots, not `decimal` |
| `open_time` / `close_time` | A deal is not a position. Domain has `DealTime` + `IngestedAt`. No close time on a deal row |
| `entry_type` int | Domain is `DealEntry Entry` (A37) |
| `reason` int | Domain has **no** `Reason` (A82). Do not invent it in the stub |
| missing | `OrderTicket`, `PositionId`, `Action`, `Swap`, `Comment`, `IngestedAt` |
| indexes on `login`, `symbol` alone | legal as non-unique; A20 still wants `(broker_id, login, deal_time)` (WT inline has this; HEAD config does not) |

### 3.5 `Mt5PositionsConfiguration` → table `mt5_positions`

| Item | HEAD config | Law / Domain |
|---|---|---|
| Table name | `mt5_positions` | A20 / §45 / WT: **`mt5_positions_current`**. Alias is not a second table (A20 §1 is about other aliases; this is a **wrong name**) |
| Type | `Mt5Positions` | **MISSING**; real type `Mt5Position` (A61 target name is `Mt5PositionCurrent`) |
| Unique | `(broker_id, position_ticket)` | A20: `(broker_id, position_id)` |
| `volume` decimal/`bigint` | same type fight as deals | `ulong VolumeNative` |
| `current_price` | Domain `PriceCurrent` | rename only |
| `commission` | **not on** `Mt5Position` | extra |
| `open_time` `DateTime` | `TimeCreate` / `TimeUpdate` `DateTimeOffset` | |
| missing | `Direction`, `PriceSl`, `PriceTp` | |

Live book is delete-on-close (A20 §3.5). Config does not say that; it is a mapping file, not a store. Note only.

---

## 4. Working tree vs HEAD (do not confuse with B21)

| Item | `HEAD` (committed) | Working tree now |
|---|---|---|
| `Configurations/` | 5 broken files | **empty** (`0 File(s)`) |
| `TraderDbContext` | `DbSet<Brokers>` … + 20 `ApplyConfiguration` | `DbSet<Broker>` … + **inline** `modelBuilder.Entity<T>` for 20 real types |
| Extra DbSets vs HEAD | — | `AuditLog`, `KillSwitch`; dropped `TraderRiskFlags`, `ShadowFills` |
| `DestinationQuotes` type | `DestinationQuotes` (**missing**) | `DestinationQuoteSnapshot` (**exists**; filename `DestinationQuote.cs`) |
| `RiskDecisions` type | `RiskDecisions` (**missing**) | `RiskDecisionRecord` (**exists**) |
| Compile | **cannot** (CS0246 × many) | **0 / 0** (measured this pass) |
| `IEntityTypeConfiguration<T>` | 5 broken + 15 missing | **0** |
| §45 coverage | not a model | 18 named tables, 0 proper maps (B03 / B19) |

B21 is correct **for the working-tree file**: no `DbSet<T>` is missing from `Domain\Entities`. B21 does **not** inspect the deleted configs or `HEAD` `TraderDbContext`. This file does.

A53 / A57 / A58 / A59 / A61 / A64 / A73 / A74 described the committed snapshot (configs present, plural types). Those paragraphs are **stale for the working tree** and **still accurate for `HEAD` blobs**.

---

## 5. Domain census used for the comparison

20 types, namespace `TraderIntelligence.Domain.Entities` (working tree = HEAD):

```text
AuditLog
Broker
CanonicalInstrument
CopyIntent
DestinationQuoteSnapshot     ← file DestinationQuote.cs
ExecutionIntent
FixSessionState
KillSwitch
Mt5Account
Mt5Deal
Mt5Group
Mt5Position
OutboxEvent
ReconstructedTrade
RiskDecisionRecord
ShadowOrder
SourceSymbolMapping
SyncCheckpoint
TraderScore
TraderScoreHistory
```

Not Entities, do not treat as EF `T`:

| Type | Where | Why it is not a hit |
|---|---|---|
| `BrokerCodes` | `Domain\Brokers` | string constants |
| `DestinationQuote` (record) | `Domain\Risk\RiskEngine.cs` | risk value object; not `DestinationQuotes` |
| `TraderState` | `Domain\Enums` | enum, not `TraderStates` entity |
| `RiskDecisionOutcome` | `Domain\Enums` | enum, not `RiskDecisions` |

`grep` for `class Brokers` / `class Mt5Groups` / `class Mt5Accounts` / `class Mt5Deals` / `class Mt5Positions` over `D:\Prop\**\*.cs` = **no product declarations**.

---

## 6. Compile evidence

Command (working tree, no file writes):

```text
dotnet build D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj --no-restore
```

Result: **Build succeeded. 0 Warning(s). 0 Error(s).** Elapsed ~0.58 s.

That success **depends on the five files staying deleted**. Re-adding them unchanged yields, per file:

- CS0246 `Brokers` / `Mt5Groups` / `Mt5Accounts` / `Mt5Deals` / `Mt5Positions` (interface type argument)
- CS0246 again on `EntityTypeBuilder<T>`

HEAD `TraderDbContext` would add CS0246 for every remaining plural `DbSet<T>` and every missing `*Configuration` class.

No attempt was made to compile HEAD in a scratch tree. The type identifiers are sufficient; inventing a compile sandbox is out of scope.

---

## 7. What must not be done

1. **Do not** `git checkout` the five configs to “put EF back.”
2. **Do not** add empty Domain classes named `Brokers`, `Mt5Deals`, … to satisfy the stubs.
3. **Do not** `ApplyConfiguration(new BrokersConfiguration())` by hand once a real assembly scan exists (A61 §3.1).
4. **Do not** keep table `mt5_positions` if the catalog name is `mt5_positions_current`.
5. **Do not** unique `(broker_id, group_id)` — group identity is `(broker_id, group_name)` (A20 §5.2, A59).
6. **Do not** map deal `volume` as `decimal` stored as `bigint`.
7. **Do not** treat working-tree inline fluent as the replacement contract. A61/B03 file list is the contract: `BrokerConfiguration` : `IEntityTypeConfiguration<Broker>`, etc.
8. **Do not** persist broker passwords. The stub did not; keep that.
9. Product source was **not** edited to delete or fix these files. The deletion is pre-existing working-tree state.

---

## 8. Replacement contract (names only — not implemented here)

When implementation is authorized, **replace** (do not extend) the five blobs. File names follow B03 §6.2 / A61 (singular entity, `*Configuration`):

| Delete / ignore | Create | `T` | `ToTable` | Identity UNIQUE (`HasDatabaseName`) |
|---|---|---|---|---|
| `BrokersConfiguration.cs` | `BrokerConfiguration.cs` | `Broker` | `brokers` | `brokers_code_uk (code)` |
| `Mt5GroupsConfiguration.cs` | `Mt5GroupConfiguration.cs` | `Mt5Group` | `mt5_groups` | `mt5_groups_broker_name_uk (broker_id, group_name)` |
| `Mt5AccountsConfiguration.cs` | `Mt5AccountConfiguration.cs` | `Mt5Account` | `mt5_accounts` | `mt5_accounts_identity_uk (broker_id, login)` |
| `Mt5DealsConfiguration.cs` | `Mt5DealConfiguration.cs` | `Mt5Deal` | `mt5_deals` | `mt5_deals_identity_uk (broker_id, deal_ticket)` |
| `Mt5PositionsConfiguration.cs` | `Mt5PositionCurrentConfiguration.cs` | `Mt5Position` today / `Mt5PositionCurrent` when renamed | `mt5_positions_current` | `mt5_positions_current_identity_uk (broker_id, position_id)` |

Map **CLR properties** (`x => x.DealTicket`), not shadows named `deal_ticket`. Use `DateTimeOffset` → `timestamptz`. Convert `ulong` tickets/volume with the A61 `PgUInt64` converter when that type is added. Scan via `ApplyConfigurationsFromAssembly`. One context name (`TraderIntelligenceDbContext` per A61/B03) — do not keep a second model.

The other 38 §45 configs are **MISSING** (B03 / B19). Not this ticket’s inventory beyond recording that HEAD already *named* 15 of them without files.

---

## 9. Honesty / classification

| Item | Class | Evidence |
|---|---|---|
| Committed 5 `IEntityTypeConfiguration<plural>` | `UNSAFE` + type **MISSING** | §2.1 — will not compile; shadow bags fight real entities |
| Committed `TraderDbContext` (plural `DbSet` + 20 `new *Configuration()`) | `UNSAFE` | §2.2–2.3 — 19 missing entity types, 15 missing classes |
| Working-tree empty `Configurations/` | `MISSING` vs A61 (43 files) | dir = 0 files |
| Working-tree `TraderDbContext` type existence | `EXISTS_NEEDS_REFACTOR` | B21 PASS; B19 FAIL on §45 |
| Domain types `Brokers` / `Mt5Deals` / … | `MISSING` (and **must stay missing**) | A61 / A78 singular law |
| Domain types `TraderRiskFlag` / `ShadowFill` | `MISSING` | no file; A52 does not cover these |
| §45-complete EF model | `MISSING` | 0 migrations, 0 `IEntityTypeConfiguration` on disk |

**Types the committed configs bind that do not exist: `Brokers`, `Mt5Groups`, `Mt5Accounts`, `Mt5Deals`, `Mt5Positions`.**

That is the whole break. Everything else in this file is why renaming the marker type is not a fix.
