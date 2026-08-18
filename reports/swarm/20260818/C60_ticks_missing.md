# C60 — `mt5_xau_ticks` not in `TraderDbContext`; MFE unavailable

| Field | Value |
|---|---|
| Agent | C60 (ticks / MFE availability, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (read-only pass; no product compile required) |
| Artifact | `D:\Prop\reports\swarm\20260818\C60_ticks_missing.md` |
| Workspace | `D:\Prop` |
| Subject | `TraderDbContext` vs Architecture §11 / §17 / §45 `mt5_xau_ticks` |
| Assigned finding | **`mt5_xau_ticks` table is not in `DbContext`. MFE unavailable.** |
| Product source modified | **No.** This report is the only write. |
| Law | Architecture v2 **§1.5**, **§11**, **§17**, **§18**, **§45**, **§51**, **§60**; A17, A20, A45, A61 |
| Siblings | A17 (tick bridge vs ledger), A20 (table catalog), A45 (MFE/MAE quality), A61 (EF contract), B19 / B33 / C06 (DbContext gaps), C17 (unit coverage), C29 (migrations) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

---

## 0. Verdict

**`mt5_xau_ticks` is MISSING from `TraderDbContext`. Exact MFE is UNAVAILABLE.**

This is not a mapping typo and not a silent fabrication bug. The entire source-tick stack is absent from the C# persistence model:

| Layer | Expected (A61 / §45) | Measured 2026-08-18 | Class |
|---|---|---|---|
| Domain entity | `Mt5XauTick` | **no file** (`Domain\Entities\Mt5XauTick.cs` = False) | **MISSING** |
| `DbSet` | `DbSet<Mt5XauTick> Mt5XauTicks` | **0** of 20 `DbSet`s | **MISSING** |
| `ToTable` | `mt5_xau_ticks` | **0** of 20 `ToTable` maps | **MISSING** |
| Fluent / `IEntityTypeConfiguration` | `Mt5XauTickConfiguration` + `mt5_xau_ticks_uk` | `Persistence\Configurations\` is **empty** | **MISSING** |
| EF migration | A30 `0005` `SymbolsAndSourceTicks` | **no** `Migrations/` folder (C29) | **MISSING** |
| Durable writer | append-only source tick tape | C++ `MT5TickBridge` is in-memory only; C# store never writes ticks | **MISSING** |
| Calculator | `MfeMaeCalculator` | **no** type, **no** tests | **MISSING** |
| Feature row | `trader_feature_snapshots` | **no** entity, **no** `DbSet` | **MISSING** |
| Published MFE | numbers only when `feature_quality ∈ {EXACT, APPROXIMATE}` | `BaselineScorer` **always** stamps `FeatureQuality.Unavailable`; `AverageMfe` / `AverageMae` never assigned | **UNAVAILABLE** (correct omission) |

Honest state: **MFE cannot be computed or persisted today.** That is the right interim behavior under §1.5 / §17 (omit, do not invent). It is **not** a passing implementation of “MFE/MAE when valid.” Dashboard, scores, and API have no tick window to validate against.

Do **not** treat `destination_quotes`, `mt5_deals.Price`, `ReconstructedTrade.EntryVwap` / `ExitVwap`, or session last-tick polls as a substitute tape.

---

## 1. Direct answer

```text
Q: Is mt5_xau_ticks in TraderDbContext?
A: No.

    TraderDbContext (174 lines, 20 DbSets, 20 ToTable maps)
    has zero of:
      - DbSet<Mt5XauTick>
      - ToTable("mt5_xau_ticks")
      - ToTable("mt5_ticks_xauusd")   // §11 alias; A20/A61 forbid a second table
      - any entity type whose name contains Tick / XauTick

    Therefore EF cannot query, insert, migrate, or EnsureCreated this table.

Q: Is MFE available?
A: No. Exact MFE is unavailable. Approximate MFE is also unpublished.

    Causal chain (all must exist; none do):
      source MT5 tick subscription
        → durable mt5_xau_ticks rows (broker_id + source_symbol + time_msc)
          → window covering [opened_at, closed_at]
            → MfeMaeCalculator
              → trader_feature_snapshots (price_source + feature_quality)
                → dashboard / score terms only when quality is legal

    Today the chain breaks at the first durable step: no table, no DbSet,
    no writer. BaselineScorer hardcodes MaeMfeQuality = Unavailable
    and leaves AverageMfe / AverageMae null. That is omission, not EXACT.
```

---

## 2. Method (read-only)

Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/src/`) were **not** edited.

| Action | Result |
|---|---|
| Read `TraderDbContext.cs` | 20 `DbSet` / 20 `ToTable`; no tick type |
| `Test-Path` `Domain\Entities\Mt5XauTick.cs`, `Domain\Mt5\Mt5XauTick.cs`, `Configurations\Mt5XauTickConfiguration.cs` | all **False** |
| `Test-Path` `Persistence\Migrations`, `MfeMaeCalculator.cs` (two candidate paths), `tests\Unit\Features\MfeMaeCalculatorTests.cs` | all **False** |
| List `Domain\Entities\*.cs` | **20** types; none is a tick |
| List `Persistence\Configurations\` | **empty** |
| Grep `src` for `Mt5XauTick`, `mt5_xau_ticks`, `MfeMaeCalculator` | **0** product hits |
| Grep `Infrastructure` for `mfe` / `MaeMfe` / `AverageMfe` / `FeatureQuality` | **0** |
| Grep `apps/web/src` for MFE/MAE UI | **0** (only “ticket” + a shadow-page warning that QUOTE ≠ source ticks) |
| Cross-read Architecture §11 / §17 / §45; A17, A20, A45, A61, B19, B33, C06, C17, C29 | expected name + quality law |

Did **not** start Postgres, did **not** run `dotnet ef`, did **not** fabricate a tick row to “prove” the table. Absence of the CLR mapping is sufficient.

---

## 3. What `TraderDbContext` actually maps (2026-08-18)

File: `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`  
174 lines. SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (5951 bytes). Same hash as C29.

`DbSet` properties, in source order:

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

**Not in this list:** `Mt5XauTicks` / `mt5_xau_ticks`. Also missing the A61 neighbors that MFE depends on: `Mt5Symbols` / `mt5_symbols`, `TraderFeatureSnapshots` / `trader_feature_snapshots`.

`OnModelCreating` is entirely inline (no `ApplyConfigurationsFromAssembly`). A case-insensitive search of this file for `xau_tick` / `XauTick` is empty. The only `*Ticket*` indexes are deal/position **tickets**, not price ticks.

`EnsureCreated` (api / mt5-worker / fix-worker) can only create tables the model knows. A missing `DbSet` + missing `modelBuilder.Entity<Mt5XauTick>` means **Postgres will never grow `mt5_xau_ticks` from this context**, even when the connection string is real Npgsql.

---

## 4. Architecture contract (binding)

### 4.1 §11 — raw layer (optional tape, required name if stored)

Architecture §11 (lines 500–517) lists:

```text
mt5_ticks_xauusd   # if source SDK/feed supports it
```

A20 / A61 alias this to **`mt5_xau_ticks`** (§45). Do **not** create both tables.

### 4.2 §45 — core table name

Architecture §45 (lines 1672–1731) includes `mt5_xau_ticks` in the recommended core set (43 names). A61 §3.1 row 11:

| `DbSet<T>` | Entity | Table |
|---|---|---|
| `Mt5XauTicks` | `Mt5XauTick` | `mt5_xau_ticks` |

A61 §8.11 / A20 required shape (not present in product):

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | **bigint IDENTITY** | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `SourceSymbol` | `source_symbol` | text | NO |
| `TickTime` | `tick_time` | timestamptz | NO |
| `TimeMsc` | `time_msc` | bigint | NO |
| `Bid` / `Ask` / `Last` | | numeric(20,8) | YES |
| `Volume` | `volume` | bigint | YES |
| `Flags` | `flags` | integer | NO |
| `IngestSeq` | `ingest_seq` | integer | NO |
| `IngestedAt` | `ingested_at` | timestamptz | NO |

Unique: `mt5_xau_ticks_uk (broker_id, source_symbol, time_msc, flags, ingest_seq)`.  
Lookup: `mt5_xau_ticks_lookup_ix (broker_id, source_symbol, time_msc)`.

A30 planned this as migration **`0005_SymbolsAndSourceTicks`**. That file does not exist (C29: migrations **0 / 15**).

### 4.3 §17 — why the table exists at all

Architecture §17 (lines 735–782): exact **MFE, MAE, price excursion, entry spread, in-trade volatility** require a **source-side time series while the position is open**. Preferred input is the MT5 source-broker tick feed. If that feed is missing: store the best available series **explicitly**, mark `price_source` + `feature_quality`, and **never pretend** a cTrader quote stream is the source book.

§1.5 (lines 85–86): **do not calculate MFE/MAE from closed deals alone. Do not fabricate.**

§14 / A21 / A45: `ReconstructedTrade` is a lifecycle record. It must **not** grow MFE columns. Excursion lives on a feature snapshot, not on the trade.

§51: dashboard shows **“MFE/MAE when valid.”** Invalid → hide or explicit unavailable. Never `0.00`.

§60: unit test **“MFE/MAE where data exists”** — compute only with a labeled window; refuse when data does not exist. C17 measures this area **MISSING**.

---

## 5. Domain / store evidence (no tick type anywhere)

### 5.1 Entities on disk (`D:\Prop\src\Domain\Entities\`)

20 files. None is `Mt5XauTick`. Ticket-shaped fields that **are** present are broker tickets, not prints:

| File | Field | What it is |
|---|---|---|
| `Mt5Deal.cs` | `DealTicket`, `OrderTicket`, `Price` | one fill |
| `Mt5Position.cs` | `PositionTicket` | one current mark (not a path) |
| `SyncCheckpoint.cs` | `LastTicket` | ingest cursor |

`ReconstructedTrade.cs` (1430 bytes, SHA-256 `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014`) has `EntryVwap` / `ExitVwap` / PnL / SL-TP flags and **no** MFE/MAE. That absence is **correct** (A45). It is not a tick tape.

`ReconstructedTradeResult` (in-memory recon DTO) likewise has no MFE fields.

`DestinationQuoteSnapshot` **does** exist and **is** mapped to `destination_quotes`. That is the **destination** book (cTrader QUOTE). A45 forbids writing those rows into `mt5_xau_ticks` or into source `mfe` / `mae`.

`FeatureSnapshot` lives in `BaselineScorer.cs` as a scorer DTO, **not** as `Domain.Entities.TraderFeatureSnapshot`. There is no persist type for excursion output even if ticks arrived.

### 5.2 Files A01 / A30 named that do not exist

| Planned path | `Test-Path` |
|---|---|
| `D:\Prop\src\Domain\Entities\Mt5XauTick.cs` | False |
| `D:\Prop\src\Domain\Mt5\Mt5XauTick.cs` (A01 leftover name) | False |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\Mt5XauTickConfiguration.cs` | False |
| `D:\Prop\src\Scoring\Features\MfeMaeCalculator.cs` | False |
| `D:\Prop\src\Domain\Scoring\MfeMaeCalculator.cs` | False |
| `D:\Prop\tests\Unit\Features\MfeMaeCalculatorTests.cs` | False |
| `D:\Prop\src\Infrastructure\Persistence\Migrations` | False |

### 5.3 C++ tick path is not a DbContext stand-in

`D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.h` is an in-memory fan-out from `IMTTickSink::OnTick` to `IQuoteSink`. Header constraint: **no DB call on the pump thread**; drain is for a web-terminal hub. A17 already measured: no persist, no `price_source`, no `TickHistoryRequest` wrap, ledger stores deal revisions only.

A C++ queue the C# `TraderDbContext` cannot see is **not** `mt5_xau_ticks`.

---

## 6. Why MFE is unavailable (causal, not theoretical)

Exact MFE for one completed XAU lifecycle needs all of the following on the **same source book**:

1. Bid/ask (/last) prints while the position is open (`[opened_at, closed_at]`).
2. Those prints stored with `broker_id` + `source_symbol` + `time_msc` (and flags / ingest_seq).
3. Coverage over the window above a documented threshold (A45 §6).
4. A calculator that emits **both** MFE and MAE, plus `price_source` + `feature_quality`.
5. A feature row (`trader_feature_snapshots`) — never the reconstructed-trade row.
6. UI/API that publish numbers only when quality is `EXACT` or (display-only) `APPROXIMATE`.

Measured breaks:

| Step | Status |
|---|---|
| 1 Live last-tick in SDK | Partial: `GetTickLast` / optional `SubscribeTicks` exist in C++. Snapshot ≠ window. History request not wrapped (A17). |
| 2 Persist `mt5_xau_ticks` | **FAIL** — no entity, no `DbSet`, no migration, no writer. |
| 3 Window coverage | **FAIL** — nothing to query for `[opened_at, closed_at]`. |
| 4 `MfeMaeCalculator` | **FAIL** — type missing. |
| 5 Feature snapshot table | **FAIL** — `trader_feature_snapshots` also missing from DbContext (B19 #7). |
| 6 Dashboard / API | **FAIL** — `DashboardModels` / `EfDashboardQueries` have no `mfe` / `mae` / `mfeMaeValid`. Web has no MFE widget. |

`BaselineScorer.ComputeFeatures` (`D:\Prop\src\Domain\Scoring\BaselineScorer.cs`, 212 lines, SHA-256 `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34`) always writes:

```csharp
MaeMfeQuality = FeatureQuality.Unavailable,
PriceSource = PriceSource.Unknown
```

`AverageMfe` and `AverageMae` are nullable and **never set** (remain null). Score terms do not consume MFE. B12 classified this as **CLEAN omission** — not fabrication. C60 agrees.

`FeatureQuality` enum exists (`Exact`, `Approximate`, `Unavailable`). `PriceSource` enum exists (`AchieverMt5Ticks`, `StarwaveMt5Ticks`, `BarApproximation`, `CTraderQuoteSession`, `Unknown`). Enums without a tape do not make MFE available.

Docs already state the policy: `D:\Prop\docs\trade-reconstruction.md` — “MFE/MAE are omitted unless a declared tick source exists (`FeatureQuality.Exact`).”

---

## 7. What must **not** be used as a stand-in

These exist (or can be derived) today and are **illegal** as source MFE:

| Input | Where it lives | Why it is not MFE |
|---|---|---|
| `Mt5Deal.Price` + `Profit` | `mt5_deals` | One fill. Realized PnL ≠ excursion path. §1.5 / §17 forbid deals-only MFE. |
| `EntryVwap` / `ExitVwap` | `reconstructed_trades` | Two points. High/low of the path is not recoverable. |
| `Mt5Position` current price | `mt5_positions_current` | Last mark, not the window. |
| Session / `GetTickLast` | C++ manager | One print (or day high/low). Not the trade window. |
| `GetChart` OHLC | C++ `MT5Manager` | Bars only. Legal later as `BAR_APPROXIMATION` / `APPROXIMATE`, **never** `EXACT`, and **not stored**. |
| `destination_quotes` | **mapped** `DestinationQuotes` | Destination FIX book. A45: illegal for source `mfe`/`mae`. Shadow marks only. |
| cTrader QUOTE on FIX session DTO | `FixSessionDto.Bid/Ask` | Same foreign book. |
| Invented “typical XAU range” | — | Fabrication. |

`apps/web/src/pages/ShadowPortfolioPage.tsx` already warns: shadow fills use the cTrader QUOTE session, **not** source MT5 ticks. That warning must stay true for MFE.

Name rule: if a future writer lands, persist source prints only in `mt5_xau_ticks`. Do **not** open a second table named `mt5_ticks_xauusd`.

---

## 8. Downstream impact

| Consumer | Impact |
|---|---|
| Scoring (`BaselineScorer`) | `mfe_mae_used` effectively false forever. A22 optional 0.07/0.08 MFE terms never fire. Traders remain scorable on deal features only. |
| `trader_feature_snapshots` | Cannot store an honest `EXACT` row; table itself is also unmapped. |
| Dashboard §51 / A93 | Cannot show “MFE/MAE when valid.” Correct display is hidden / “unavailable,” not `0`. Today the fields are simply absent from DTOs. |
| §60 tests | `MfeMaeCalculatorTests` / `MfeMaeMissingTickDataTests` **MISSING** (C17 area 10). |
| Phase 6 / ML (A52) | Training on fabricated MFE would leak and lie. Omit the column until a labeled tape exists. |
| Go-live §68 / first useful (A57/A100) | Exact MFE is **not** a Phase-1 ingest gate, but claiming “features complete” while this table is missing is greenwash. |
| Replay (A67) | Harness may inject an in-memory tape for tests; production SQL still has no table. |

---

## 9. What a later implementer must add (not done here)

Product source was **not** modified. When someone is allowed to implement, the minimum honest path is:

1. Domain type `Mt5XauTick` matching A61 §8.11 (identity `bigint`, not `Guid`).
2. `DbSet<Mt5XauTick> Mt5XauTicks` + named unique `mt5_xau_ticks_uk`. Prefer `IEntityTypeConfiguration`, not more inline fluent.
3. Versioned migration (A30 `0005` or next free number). **Do not** rely on `EnsureCreated` (C29: `UNSAFE`).
4. Writer only if the source SDK actually yields ticks. Empty table + `system_events` note if unsupported. `ON CONFLICT DO NOTHING` on the unique key.
5. Never copy `destination_quotes` into this table.
6. `MfeMaeCalculator` that **refuses** deals-only / mixed-book inputs. Tests from A09 / A45 / A89 (`Refuses_to_fabricate_from_closed_deals_only`, missing-tick + present cTrader quotes → omit source MFE).
7. Persist results on `trader_feature_snapshots` with `price_source` + `feature_quality`. Leave `ReconstructedTrade` unchanged.
8. Until coverage meets A45: keep `FeatureQuality.Unavailable` and null numbers.

Until step 4 produces a real window, **MFE stays unavailable** even after the `DbSet` exists. A mapped empty table is still not `EXACT`.

---

## 10. Measured files (hashes)

Product files only (SHA-256, sizes bytes):

| Bytes | SHA-256 | Path |
|---:|---|---|
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 8143 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| 131 | `474EA06DCDE7B3F8E20F8A8B9E0DECD50D87C1D8D9DF8F0519A9FCE609AA9D20` | `D:\Prop\src\Domain\Enums\FeatureQuality.cs` |
| 195 | `0ACB91DC3A5EF2CAF09E65B656A0CFCF9D10E200FB13AE1BBDE399805E3F4AFA` | `D:\Prop\src\Domain\Enums\PriceSource.cs` |
| 1430 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` | `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` |
| 1980 | `2248708669BC739A0EDE71318B764E7F34A07F906720FF36EE2762EB8FFABCDE` | `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` |
| 421 | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | `D:\Prop\src\Domain\Entities\DestinationQuote.cs` |
| 2577 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` |
| 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |

---

## 11. Scorecard

| Question | Answer | Class |
|---|---|---|
| Is `mt5_xau_ticks` a `ToTable` in `TraderDbContext`? | **No** | **MISSING** |
| Is there a `DbSet<Mt5XauTick>`? | **No** | **MISSING** |
| Is there a `Mt5XauTick` entity? | **No** | **MISSING** |
| Can `EnsureCreated` / a future `Migrate` create the table from this model? | **No** | **MISSING** |
| Is exact MFE computable from current product state? | **No** | **UNAVAILABLE** |
| Is approximate bar-MFE published? | **No** (and must stay unpublished until labeled) | **UNAVAILABLE** |
| Are MFE numbers being fabricated from deals or destination quotes? | **No** — scorer forces `Unavailable`, averages null | **EXISTS_AND_GOOD** (omission) |
| Is `destination_quotes` mapped? | **Yes** — wrong book for source MFE | **EXISTS_NEEDS_REFACTOR** (dest only; do not reuse) |
| §60 “MFE/MAE where data exists” tests? | **0** | **MISSING** |

**C60 class of the tick/MFE stack: `MISSING` (tape + calculator) + correct `UNAVAILABLE` publication.**

---

## 12. Cross-references (do not treat as this file)

| File | What it already said; C60 confirms |
|---|---|
| `A17_ticks_and_ledger.md` | SDK sees live ticks; does not store a tape. FAIL vs §17. |
| `A20_table_catalog.md` | `mt5_xau_ticks` catalog + unique key. |
| `A45_mfe_mae_policy.md` | Quality law; no silent mix; omit when tape missing. |
| `A61_efcore_schema.md` | Expected `DbSet` #11 `Mt5XauTicks`. |
| `B19_dbcontext_gap.md` | Missing table #6 `mt5_xau_ticks`. |
| `B33_entity_table_gap.md` | No `Mt5XauTick` type; MFE cannot be exact. |
| `C06_dbcontext_review.md` | 20 Guid PKs; ticks never entered the identity review because the type is absent. |
| `C17_unit_coverage.md` | §60 area 10 MISSING. |
| `C29_migrations_gap.md` | No `Migrations/`; `EnsureCreated` cannot invent unmapped tables. |

---

**Rule to carry forward:** `mt5_xau_ticks` is not in `TraderDbContext`. MFE is unavailable. Keep it unavailable until a labeled source tick window exists. Do not back-fill from deals, bars-without-label, or `destination_quotes`.
