# D56 — `mt5_xau_ticks` table: **MISSING**; exact MFE remains **UNAVAILABLE**

| Field | Value |
|---|---|
| Agent | D56 (source tick table / MFE tape re-measure, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (second independent pass after C60) |
| Artifact | `D:\Prop\reports\swarm\20260818\D56_ticks.md` |
| Assigned | `mt5_xau_ticks` table? Write this file. Do not modify product source. |
| Workspace | `D:\Prop` |
| Subject | Architecture §45 / A20 / A61 table `mt5_xau_ticks` vs current product tree |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No** |
| Law | Architecture v2 **§1.5**, **§11**, **§14**, **§17**, **§18**, **§45**, **§51**, **§60**; A20 catalog; A45 quality; A61 EF contract; A22 I7 |
| Prior same finding | C60 (`mt5_xau_ticks` not in `TraderDbContext`); A17 (tick bridge vs ledger); A20 #11; A61 §8.11; B19 #6; B33 #11; D19 row 11 |
| Classification | architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` |

---

## 0. Direct answer

**There is no `mt5_xau_ticks` table in this product.**

Not as a Domain entity, not as a `DbSet`, not as a `ToTable`, not as an EF configuration, not as a versioned migration, not as a `.sql` file, not as a C++ ledger insert, not as a C# ingest writer, not as a docker-compose init script.

```text
Q: Does mt5_xau_ticks exist?
A: No. It is a named architecture table only.

    Architecture §45 lists it (line 1688).
    A20 catalog #11 and A61 DbSet #11 specify the shape.
    Product source has zero of those names.

Q: Can Postgres grow this table from TraderDbContext / EnsureCreated?
A: No. The model does not know the type.

Q: Is exact MFE/MAE available?
A: No. Approximate MFE is also unpublished.
    BaselineScorer still hard-stamps FeatureQuality.Unavailable
    and leaves AverageMfe / AverageMae null. That is omission,
    not EXACT.
```

**Class of the tape: `MISSING`.**  
**Class of published MFE numbers: `UNAVAILABLE` (correct under §1.5 / §17).**  
**Class of scorer omission: `EXISTS_AND_GOOD` (does not fabricate).**

Do **not** treat `destination_quotes`, `mt5_deals.Price`, `ReconstructedTrade.EntryVwap` / `ExitVwap`, `Mt5Position.PriceCurrent`, `GetTickLast`, session high/low, or `GetChart` OHLC as this table.

---

## 1. Method (read-only)

Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/src/`) were **not** edited. Postgres was **not** started. `dotnet ef` was **not** run. No tick row was fabricated to “prove” a table.

| Action | Result |
|---|---|
| Grep `D:\Prop\src` for `mt5_xau_ticks`, `Mt5XauTick`, `mt5_ticks_xauusd`, `MfeMae` | **0** product hits |
| Grep `D:\Prop\apps` for the same | **0** |
| Grep `D:\Prop\tests` for `MfeMae` / `mt5_xau_ticks` | **0** |
| Read `TraderDbContext.cs` | **20** `DbSet` / **20** `ToTable`; none is ticks |
| List `Domain\Entities\*.cs` | **21** files; none is `Mt5XauTick` |
| List `Persistence\Configurations\` | directory exists, **0** files |
| `Test-Path` `Persistence\Migrations`, `Persistence\Sql`, `Mt5XauTick.cs`, `MfeMaeCalculator.cs`, `MfeMaeCalculatorTests.cs` | all **False** |
| Recurse `D:\Prop` for `*.sql` outside `bin`/`obj`/`node_modules`/`vendor` | **0** files |
| Read C++ `mt5_tick_bridge.*`, `mt5_ledger_store.cpp`, `imt5_client.h` | live last-tick / optional subscribe; **no** tick INSERT; **no** `TickHistoryRequest` wrap |
| Read `IMt5BrokerConnector`, `FakeMt5BrokerConnector`, `apps/mt5-worker/Worker.cs` | deals + positions only; no tick method |
| Read `BaselineScorer`, `FeatureQuality`, `PriceSource`, `ReconstructedTrade`, dashboard DTOs | MFE slots unused; no API field |
| Hash the persist + scorer files | §10 |
| Cross-read Architecture §11 / §17 / §45; A17, A20, A22 I7, A45, A59, A61 §8.11, A98, C60, D19 | expected name + quality law |

Absence of the CLR mapping is sufficient to say EF cannot create the table. Absence of any `.sql` / C++ DML is sufficient to say nothing else creates it either.

---

## 2. Verdict vs C60 (what moved, what did not)

C60 already scored this **MISSING**. D56 re-measured. The tape has **not** landed.

| Item | C60 | D56 (this file) |
|---|---|---|
| `TraderDbContext` SHA-256 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | **same** (5951 bytes, 174 lines) |
| `Mt5XauTick` type | absent | **absent** |
| `DbSet` / `ToTable("mt5_xau_ticks")` | 0 / 20 | **0 / 20** |
| `IEntityTypeConfiguration` for ticks | 0 | **0** (`Configurations\` still empty) |
| EF `Migrations/` | 0 | **0** |
| `MfeMaeCalculator` | absent | **absent** |
| `BaselineScorer` SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | **same** |
| Scorer `MaeMfeQuality` | always `Unavailable` | **same** |
| `destination_quotes` mapped | yes (`DestinationQuoteSnapshot`) | **yes** (same file SHA `E5CFED15…`) |
| Dashboard DTO MFE fields | none | **none** (DTO file hash moved; still no `mfe`/`mae`) |

Dashboard hashes **did** move (`DashboardModels` 2577→3088 bytes; `EfDashboardQueries` 7407→8708 bytes). That is extra demo query surface, **not** a tick table. C60’s quote that `docs/trade-reconstruction.md` says “MFE/MAE omitted unless `FeatureQuality.Exact`” is **stale**: the current 2293-byte doc (SHA `500B4FF1…`) describes deal grouping only and **never mentions** MFE/MAE.

Use **this file** for “does `mt5_xau_ticks` exist today.” Use C60 as the first DbContext-only finding. Use A17 for the C++ transport. Use A45 for the quality law.

---

## 3. Architecture contract (why the table exists)

### 3.1 Name (one table, two aliases)

| Source | Name | Meaning |
|---|---|---|
| §11 raw layer (line 514) | `mt5_ticks_xauusd` | optional “if source SDK/feed supports it” |
| §45 core set (line 1688) | `mt5_xau_ticks` | recommended initial catalog (43 names) |
| A20 §1 / A61 header | **`mt5_xau_ticks`** | **canonical**. `mt5_ticks_xauusd` is an alias, **not** a second table |

Do **not** create both.

### 3.2 Why it is not optional for *exact* MFE

§17 (lines 735–782): exact **MFE, MAE, price excursion, entry spread, in-trade volatility** need a **source-side time series while the position is open**. Preferred input is the MT5 source-broker tick feed. If that feed is missing: store the best available series **explicitly**, mark `price_source` + `feature_quality`, and **never pretend** a cTrader quote stream is the source book.

§1.5 (lines 85–86): **do not calculate MFE/MAE from closed deals alone. Do not fabricate.**

§14 / A21 / A45: `ReconstructedTrade` is a lifecycle record. It must **not** grow MFE columns. Excursion lives on `trader_feature_snapshots`.

§18 / A22 I7: baseline **may** use MAE/MFE only when `feature_quality == EXACT`. Optional 0.07 / 0.08 score terms stay dropped while the tape is missing.

§51: dashboard shows **“MFE/MAE when valid.”** Invalid → hide or explicit unavailable. Never `0.00`.

§60: unit test **“MFE/MAE where data exists”** — compute only with a labeled window; refuse when data does not exist.

The table is **optional as a Phase-1 ingest gate** (A30: populate only if the source yields ticks; else empty + `system_events` note). It is **required** before anyone may publish `feature_quality=EXACT`.

---

## 4. Expected shape (A20 #11 / A61 §8.11) — **not implemented**

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | **bigint IDENTITY** | NO PK |
| `BrokerId` | `broker_id` | uuid NOT NULL → `brokers.id` | NO |
| `SourceSymbol` | `source_symbol` | text | NO |
| `TickTime` | `tick_time` | timestamptz | NO |
| `TimeMsc` | `time_msc` | bigint | NO |
| `Bid` / `Ask` / `Last` | | numeric(20,8) | YES |
| `Volume` | `volume` | bigint | YES |
| `Flags` | `flags` | integer | NO |
| `IngestSeq` | `ingest_seq` | integer | NO — collector tie-break |
| `IngestedAt` | `ingested_at` | timestamptz | NO |

| Constraint / index | Unique | Name |
|---|---|---|
| `(broker_id, source_symbol, time_msc, flags, ingest_seq)` | YES | `mt5_xau_ticks_uk` |
| `(broker_id, source_symbol, time_msc)` | no | `mt5_xau_ticks_lookup_ix` |

Rules that belong with the table when it lands:

- Side = **source**. `broker_id` is mandatory (§10). Never unique `source_symbol` globally.
- Mutability = **append-only**. Exact-duplicate deliveries: `ON CONFLICT DO NOTHING`.
- `ingest_seq` is collector-local, not a broker ticket. Two prints in the same millisecond stay unique.
- Partitioning (`RANGE (tick_time)` or `time_msc`) is **out of first migration** (A61).
- A30 planned this as **`0005_SymbolsAndSourceTicks`** together with `mt5_symbols`. That file does not exist.
- A59 stream `ticks_xau` (BROKER, `TIME_TICKET`) is the planned checkpoint. No such stream is written.
- A98’s 5,000-account index contract does **not** even list this table. That is consistent with “optional until a real feed exists,” not evidence the table is present.

Identity is **bigint**, not `Guid`. A61 is explicit. Do not copy the demo `Guid` PK pattern onto a tick tape.

---

## 5. What `TraderDbContext` actually maps

File: `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`  
174 lines / 5951 bytes / SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (unchanged vs C60 / D19).

`OnModelCreating` is entirely inline. **0** `ApplyConfigurationsFromAssembly`. `Persistence\Configurations\` exists and is empty. **0** `HasDatabaseName("mt5_xau_ticks_uk")`. **0** `HasForeignKey`.

| # | `DbSet` | CLR type | `ToTable` |
|---:|---|---|---|
| 1 | `Brokers` | `Broker` | `brokers` |
| 2 | `Mt5Groups` | `Mt5Group` | `mt5_groups` |
| 3 | `Mt5Accounts` | `Mt5Account` | `mt5_accounts` |
| 4 | `Mt5Deals` | `Mt5Deal` | `mt5_deals` |
| 5 | `Mt5Positions` | `Mt5Position` | `mt5_positions_current` |
| 6 | `ReconstructedTrades` | `ReconstructedTrade` | `reconstructed_trades` |
| 7 | `CanonicalInstruments` | `CanonicalInstrument` | `canonical_instruments` |
| 8 | `SourceSymbolMappings` | `SourceSymbolMapping` | `source_symbol_mappings` |
| 9 | `TraderScores` | `TraderScore` | `trader_scores` |
| 10 | `TraderScoreHistory` | `TraderScoreHistory` | `trader_score_history` |
| 11 | `OutboxEvents` | `OutboxEvent` | `outbox_events` |
| 12 | `SyncCheckpoints` | `SyncCheckpoint` | `sync_checkpoints` |
| 13 | `CopyIntents` | `CopyIntent` | `copy_intents` |
| 14 | `RiskDecisions` | `RiskDecisionRecord` | `risk_decisions` |
| 15 | `ExecutionIntents` | `ExecutionIntent` | `execution_intents` |
| 16 | `ShadowOrders` | `ShadowOrder` | `shadow_orders` |
| 17 | `DestinationQuotes` | `DestinationQuoteSnapshot` | `destination_quotes` |
| 18 | `FixSessionStates` | `FixSessionState` | `fix_sessions` |
| 19 | `AuditLogs` | `AuditLog` | `audit_logs` |
| 20 | `KillSwitches` | `KillSwitch` | `kill_switches` |

**Not in this list:** `Mt5XauTicks` / `mt5_xau_ticks`. Also missing the A61 neighbors MFE depends on: `Mt5Symbols` / `mt5_symbols`, `TraderFeatureSnapshots` / `trader_feature_snapshots`, `system_events` (the A30 “feed unsupported” note target).

`EnsureCreatedAsync` (api / mt5-worker / fix-worker) can only create tables the model knows. A missing `DbSet` + missing `modelBuilder.Entity<Mt5XauTick>` means **Postgres will never grow `mt5_xau_ticks` from this context**.

There is still **no** `Migrations/` folder (C29 / D19). `docker-compose.yml` starts `postgres:16` with database `trader_intelligence` and **no** init SQL.

---

## 6. Domain / store evidence (no tick type)

### 6.1 Entities on disk (`D:\Prop\src\Domain\Entities\`)

**21** files. None is `Mt5XauTick`. Ticket-shaped fields that **are** present are broker tickets / one marks, not a path:

| File | Field | What it is |
|---|---|---|
| `Mt5Deal.cs` | `DealTicket`, `OrderTicket`, `Price` | one fill |
| `Mt5Position.cs` | `PositionTicket`, `PriceCurrent` | one current mark |
| `SyncCheckpoint.cs` | (stream cursor) | ingest cursor, not prints |
| `ReconstructedTrade.cs` | `EntryVwap` / `ExitVwap` | two points of a lifecycle |

`ReconstructedTrade` (1430 bytes, SHA-256 `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014`) has **no** MFE/MAE columns. That absence is **correct** (A45 / §14). It is not a tick tape.

`ReconstructedTradeResult` (2042 bytes, SHA `EF41E774…`) likewise has no MFE fields. Hash moved vs C60 (1980 / `22487086…`) — extra first-3 / deal-ticket bookkeeping, still no excursion.

`DestinationQuoteSnapshot` **does** exist (`DestinationQuote.cs`, 421 bytes, SHA `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726`) and **is** mapped to `destination_quotes`. That is the **destination** book (cTrader QUOTE). A45 forbids writing those rows into `mt5_xau_ticks` or into source `mfe` / `mae`.

`FeatureSnapshot` lives in `BaselineScorer.cs` as a scorer DTO, **not** as `Domain.Entities.TraderFeatureSnapshot`. There is no persist type for excursion output even if ticks arrived.

`RiskDecision.cs` is a 21st entity file unused by `TraderDbContext` (the mapped type is `RiskDecisionRecord`). Irrelevant to ticks; counted so the entity census is honest.

### 6.2 Planned files that still do not exist

| Planned path | `Test-Path` |
|---|---|
| `D:\Prop\src\Domain\Entities\Mt5XauTick.cs` | False |
| `D:\Prop\src\Domain\Mt5\Mt5XauTick.cs` | False |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\Mt5XauTickConfiguration.cs` | False |
| `D:\Prop\src\Scoring\Features\MfeMaeCalculator.cs` | False |
| `D:\Prop\src\Domain\Scoring\MfeMaeCalculator.cs` | False |
| `D:\Prop\tests\Unit\Features\MfeMaeCalculatorTests.cs` | False |
| `D:\Prop\src\Infrastructure\Persistence\Migrations` | False |
| `D:\Prop\src\Infrastructure\Persistence\Sql` | False |
| `D:\Prop\src\Infrastructure\Persistence\Sql\202608180005_SymbolsAndSourceTicks.sql` | False |

---

## 7. Writers that do **not** write this table

### 7.1 C# ingest path

`IMt5BrokerConnector` (`Application\Contracts\Mt5Contracts.cs`) exposes:

```text
Connect / Disconnect / IsConnected
GetGroupsAsync
GetAccountsAsync
GetDealsAsync
GetPositionsAsync
```

**No** `GetTickLast`, **no** `SubscribeTicks`, **no** `GetTickHistory`.  
`FakeMt5BrokerConnector` implements that surface with in-memory deal/position lists.  
`apps/mt5-worker/Worker.cs` loops `DealIngestionService.SyncBrokerAsync` + `ReconstructionScoringService.RebuildTraderAsync` every 30 s for four demo logins. **No tick job.**

A64 / A59 `ticks_xau` is therefore **0/1**.

### 7.2 C++ live path is not a warehouse

`D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.h` (10553 bytes, SHA `B759D636…`): adapter from `IMTTickSink::OnTick` to `IQuoteSink`. Binding constraint: **no DB call on the pump thread**. Queue is in-memory (`deque`, cap 50_000, drop oldest). Drain is for a web-terminal hub. Build flag `MT5SDK_WITH_DROGON` (default off).

`TickData` (`mt5_types.h`) already has the right *shape* for a later tape: `symbol`, `bid`, `ask`, `last`, `volume`, `time`, `time_msc`, `flags`. The series is **not kept**.

`IMT5Client::GetTickLast` exists. `SubscribeTicks` defaults to **false**. `TickHistoryRequest` exists on the vendor Manager SDK (`MT5APIManager.h` lines 309 / 512) and is **not** wrapped on `IMT5Client`. Backfill of `[opened_at, closed_at]` through the product client is impossible.

Poll fallback (`GetTickLast` every 250 ms, ≤ 64 symbols) is a snapshot series. A17 / A45: **never** `EXACT`.

### 7.3 C++ ledger is deals only

`mt5_ledger_store.cpp` (SHA `0BB2CD47…`) inserts only:

| API | Table |
|---|---|
| `recordRawEvent` | `mt5_raw_events` |
| `recordDealRevision` | `mt5_deals_ledger` |

`ON CONFLICT DO NOTHING` on those keys. **No** `INSERT INTO mt5_xau_ticks`. Those C++ table names are a **sibling** schema, not A20. A raw-event `entityType="tick"` would still be the wrong table (A17). No in-repo caller of the store except the hermetic validator.

### 7.4 Compose / SQL

`docker-compose.yml` defines Postgres + Redis + api. **No** volume-mounted schema, **no** flyway/liquibase, **no** `*.sql` in the repo (product tree). An empty `trader_intelligence` database does not contain `mt5_xau_ticks`.

---

## 8. Why MFE is unavailable (causal)

Exact MFE for one completed XAU lifecycle needs **all** of the following on the **same source book**:

1. Bid/ask (/last) prints while the position is open (`[opened_at, closed_at]`).
2. Those prints stored with `broker_id` + `source_symbol` + `time_msc` (and flags / `ingest_seq`).
3. Coverage over the window above A45 §6 thresholds (first tick ≤ `opened_at+1s`, last ≥ `closed_at-1s`, no intra-session gap > 2s, zero unexplained drops, broker time not wall-clock backfill).
4. A calculator that emits **both** MFE and MAE, plus `price_source` + `feature_quality`.
5. A feature row (`trader_feature_snapshots`) — never the reconstructed-trade row.
6. UI/API that publish numbers only when quality is `EXACT` or (display-only) `APPROXIMATE`.

| Step | Status |
|---|---|
| 1 Live last-tick in SDK | Partial: `GetTickLast` / optional `SubscribeTicks`. Snapshot ≠ window. History request not wrapped. |
| 2 Persist `mt5_xau_ticks` | **FAIL** — no entity, no `DbSet`, no migration, no writer. |
| 3 Window coverage | **FAIL** — nothing to query for `[opened_at, closed_at]`. |
| 4 `MfeMaeCalculator` | **FAIL** — type missing. |
| 5 Feature snapshot table | **FAIL** — `trader_feature_snapshots` also unmapped (B19 / D19). |
| 6 Dashboard / API | **FAIL** — `DashboardModels` / `EfDashboardQueries` have no `mfe` / `mae` / `mfeMaeValid`. Web has no MFE widget. |

`BaselineScorer.ComputeFeatures` always writes:

```csharp
MaeMfeQuality = FeatureQuality.Unavailable,
PriceSource = PriceSource.Unknown
```

`AverageMfe` and `AverageMae` are nullable and **never set**. Score terms do not consume MFE. A22 optional 0.07/0.08 terms never fire. D12 already classified the scorer as a stub that does not promote LIVE; D56 adds: it also does not invent excursion.

Enums exist without a tape:

| Enum | Values | File SHA |
|---|---|---|
| `FeatureQuality` | `Exact`, `Approximate`, `Unavailable` | `474EA06D…` |
| `PriceSource` | `Unknown`, `AchieverMt5Ticks`, `StarwaveMt5Ticks`, `BarApproximation`, `CTraderQuoteSession` | `0ACB91DC…` |

A45’s published set is `{EXACT, APPROXIMATE, omit}`. The product’s `Unavailable` token is the honest persist/DTO stand-in for omit. It must **not** carry a number. Today it does not.

`apps/web/src/pages/ShadowPortfolioPage.tsx` already warns: shadow fills use the cTrader QUOTE session, **not** source MT5 ticks. That warning must stay true for MFE.

---

## 9. What must **not** be used as a stand-in

These exist (or can be derived) today and are **illegal** as source MFE:

| Input | Where it lives | Why it is not MFE |
|---|---|---|
| `Mt5Deal.Price` + `Profit` | `mt5_deals` | One fill. Realized PnL ≠ excursion path. §1.5 / §17 / A22 I7. |
| `EntryVwap` / `ExitVwap` | `reconstructed_trades` | Two points. High/low of the path is not recoverable. |
| `Mt5Position.PriceCurrent` | `mt5_positions_current` | Last mark, not the window. |
| Session / `GetTickLast` | C++ manager / HTTP | One print (or day high/low). Not the trade window. Never `EXACT`. |
| `GetChart` OHLC | C++ `MT5Manager` | Bars only. Legal later as `BAR_APPROXIMATION` / `APPROXIMATE`, **never** `EXACT`, and **not stored**. |
| `destination_quotes` | mapped `DestinationQuotes` | Destination FIX book. A45: illegal for source `mfe`/`mae`. Shadow marks only. |
| cTrader QUOTE on FIX session DTO | `FixSessionDto.Bid/Ask` | Same foreign book; dashboard clones the latest dest snapshot onto **both** cards (D21). |
| C++ `mt5_raw_events` with `entityType=tick` | ledger (unused) | Event log, not a time-series. Not implemented. |
| Invented “typical XAU range” | — | Fabrication. |

Name rule: if a future writer lands, persist source prints only in `mt5_xau_ticks`. Do **not** open a second table named `mt5_ticks_xauusd`. Do **not** copy dest quotes into this table.

---

## 10. Measured files (hashes)

Product files only (SHA-256, sizes bytes). Measured this pass.

| Bytes | SHA-256 | Path |
|---:|---|---|
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 8143 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| 131 | `474EA06DCDE7B3F8E20F8A8B9E0DECD50D87C1D8D9DF8F0519A9FCE609AA9D20` | `D:\Prop\src\Domain\Enums\FeatureQuality.cs` |
| 195 | `0ACB91DC3A5EF2CAF09E65B656A0CFCF9D10E200FB13AE1BBDE399805E3F4AFA` | `D:\Prop\src\Domain\Enums\PriceSource.cs` |
| 1430 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` | `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` |
| 2042 | `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` | `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` |
| 421 | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | `D:\Prop\src\Domain\Entities\DestinationQuote.cs` |
| 3088 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` |
| 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| 836 | `C81AEE8F15DA0EB1449DA3549A0FDD809D8C1607B9964F908830DD8F371F5487` | `D:\Prop\src\Domain\Entities\Mt5Deal.cs` |
| 776 | `C1C8A7E66A1CE40C574A5A9D0B0C95F1E6D7C163C2F896A6D0FE7AFC7FAAF6FE` | `D:\Prop\src\Domain\Entities\Mt5Position.cs` |
| 10553 | `B759D636D8F51D24FA15CA1BDA6A65D2E98958CE73193E53AF5ACBC337C91E68` | `D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.h` |
| 14271 | `F18FB606AE465921D3F80A6507A8615F4FF820EDA048ACDD16CDF042666D5720` | `D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.cpp` |
| 5226 | `0BB2CD478EE1643FE886F6ECD4097742A0C0E759D43F4A4270470041937AF5BF` | `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.cpp` |
| 2293 | `500B4FF1C538EAFDEBBCAF189DA24DD4BF0E41A285E64ED68C86BC7C7E2008A1` | `D:\Prop\docs\trade-reconstruction.md` |

---

## 11. Scorecard

| Question | Answer | Class |
|---|---|---|
| Is `mt5_xau_ticks` a physical Postgres table in this repo? | **No** — no SQL, no migration, no `EnsureCreated` map | **MISSING** |
| Is it a `ToTable` in `TraderDbContext`? | **No** | **MISSING** |
| Is there a `DbSet<Mt5XauTick>`? | **No** | **MISSING** |
| Is there a `Mt5XauTick` entity? | **No** | **MISSING** |
| Is A30 `0005_SymbolsAndSourceTicks` on disk? | **No** | **MISSING** |
| Does C++ ledger write ticks? | **No** — `mt5_raw_events` + `mt5_deals_ledger` only | **MISSING** |
| Does C# connector fetch ticks? | **No** | **MISSING** |
| Is exact MFE computable from current product state? | **No** | **UNAVAILABLE** |
| Is approximate bar-MFE published? | **No** (must stay unpublished until labeled) | **UNAVAILABLE** |
| Are MFE numbers being fabricated from deals or dest quotes? | **No** — scorer forces `Unavailable`, averages null | **EXISTS_AND_GOOD** (omission) |
| Does `ReconstructedTrade` wrongly hold MFE columns? | **No** | **EXISTS_AND_GOOD** |
| Is `destination_quotes` mapped? | **Yes** — wrong book for source MFE | **EXISTS_NEEDS_REFACTOR** (dest only; do not reuse) |
| §60 “MFE/MAE where data exists” tests? | **0** | **MISSING** |
| C60 finding still true? | **Yes** (`TraderDbContext` SHA unchanged) | confirmed |

**D56 class of the tick/MFE stack: `MISSING` (tape + calculator + feature table) + correct `UNAVAILABLE` publication.**

---

## 12. What a later implementer must add (not done here)

Product source was **not** modified. When someone is allowed to implement, the minimum honest path is:

1. Domain type `Mt5XauTick` matching A61 §8.11 (identity `bigint`, not `Guid`).
2. `DbSet<Mt5XauTick> Mt5XauTicks` + named unique `mt5_xau_ticks_uk`. Prefer `IEntityTypeConfiguration`, not more inline fluent.
3. Versioned migration (A30 `0005` or next free number) **and** reviewed SQL under `Persistence/Sql/`. **Do not** rely on `EnsureCreated` (C29: `UNSAFE`).
4. Writer **only if** the source SDK actually yields ticks. Empty table + `system_events` note if unsupported. `ON CONFLICT DO NOTHING` on the unique key. Off the C++ pump thread.
5. Wrap `TickHistoryRequest` on `IMT5Client` before claiming backfill. Fail closed on HTTP/remote.
6. Never copy `destination_quotes` into this table. Never mix Achiever ticks onto a StarwaveFX trade.
7. `MfeMaeCalculator` that **refuses** deals-only / mixed-book / poll-only / wall-clock-backfilled inputs. Tests from A09 / A45 / A89 (`Refuses_to_fabricate_from_closed_deals_only`; missing-tick + present cTrader quotes → omit source MFE; bars → `APPROXIMATE` only).
8. Persist results on `trader_feature_snapshots` with `price_source` + `feature_quality`. Leave `ReconstructedTrade` unchanged.
9. Until coverage meets A45 §6: keep `FeatureQuality.Unavailable` and null numbers. Dashboard: hide or “unavailable,” never `0.00`.

Until step 4 produces a real window, **MFE stays unavailable** even after the `DbSet` exists. A mapped empty table is still not `EXACT`.

---

## 13. Cross-references (do not treat as this file)

| File | What it already said; D56 confirms |
|---|---|
| `C60_ticks_missing.md` | First DbContext-only finding. `TraderDbContext` SHA unchanged. |
| `A17_ticks_and_ledger.md` | SDK sees live ticks; does not store a tape. FAIL vs §17. |
| `A20_table_catalog.md` | Catalog #11 + unique key. |
| `A45_mfe_mae_policy.md` | Quality law; no silent mix; omit when tape missing. |
| `A61_efcore_schema.md` | Expected `DbSet` #11 `Mt5XauTicks` + §8.11 columns. |
| `A59_ingestion_checkpoints.md` | Stream `ticks_xau` — not implemented. |
| `A30_implementation_sequence.md` | Migration `0005` — not written. |
| `A22_scoring_spec.md` | I7 + optional 0.07/0.08 terms only when EXACT. |
| `A98_pg_indexes.md` | Tick table not in the 5k hot-index set (consistent with optional). |
| `B19_dbcontext_gap.md` / `D19_dbcontext.md` | Missing table #6 / row 11. |
| `B33_entity_table_gap.md` | No `Mt5XauTick` type. |
| `D12_scorer_review.md` | Same scorer SHA; MFE slots unused. |

---

**Rule to carry forward:** `mt5_xau_ticks` is not in the product. Exact MFE is unavailable. Keep it unavailable until a labeled source tick window exists. Do not back-fill from deals, bars-without-label, last-tick polls, or `destination_quotes`. Do not create a second table named `mt5_ticks_xauusd`.
