# D44 — `DealReason` persist gap

| Field | Value |
|---|---|
| Agent | D44 (senior engineer, persist path only) |
| Date | 2026-08-18 |
| Assigned | Read A82 and `NormalizedDeal`. Is `DealReason` persisted? Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D44_reason_gap.md` |
| Workspace | `D:\Prop` |
| Law | `D:\Prop\reports\swarm\20260818\A82_deal_reasons.md` (SHA-256 `F0F03DF134996B99F6E446B6DA69EE286B6E9FF38020E6C2F66EDD2311260619`, 27727 bytes) |
| Companions | A21 (`DealIn.reason` / `close_reason`), A13 (`DealData` has no reason), A61 §8.8 (`mt5_deals` column list), A20 `mt5_deals`, B26 (stale “do not invent reason”), C29 (no migrations), D19 (`TraderDbContext`), D20 (`EfTradingStore`), D24 (Fake deals have no reason) |
| Product source modified | **No.** This report is the only write. |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `STALE_REPORT`.

---

## 0. Verdict

**No. `DealReason` is not persisted.**

It exists on the in-memory reconstruction record (`NormalizedDeal.Reason`) and as `TraderIntelligence.Domain.Enums.DealReason`. The production ingest → store → reload → reconstruct path never carries it. `Mt5DealDto`, `Mt5Deal`, `mt5_deals`, `LoadDealsAsync`, `ReconstructedTrade`, and `ReconstructedTradeResult` have **zero** reason / `close_reason` fields. C++ `DealData` / `extractDeal` still omit `IMTDeal::Reason()`. The C++ ledger SQL column `reason_code` is declared but has **no extractor and no C# consumer**.

Because `DealReasons.CountsAsTraderActivity(null)` returns **true**, a missing persist column is not a conservative UNKNOWN (A82 §0 / §7.1). Reloaded deals look like real trading. The A82 filter is dead on every store-backed reconstruct.

| Surface | Has `DealReason` / `reason`? | Class |
|---|---|---|
| `NormalizedDeal.Reason` | **yes**, `DealReason?`, default **null** | `EXISTS_NEEDS_REFACTOR` (memory only) |
| `DealReason` enum + `DealReasons` | **yes**, 0–19, REAL_TRADING set matches A82 | `EXISTS_AND_GOOD` as vocabulary |
| `IsTradingDeal` | action **and** reason (null counts as trading) | `EXISTS_NEEDS_REFACTOR` vs A82 UNKNOWN |
| `Mt5DealDto` | **no** | `MISSING` |
| `IMt5BrokerConnector.GetDealsAsync` | cannot return a reason | `MISSING` |
| `FakeMt5BrokerConnector` / `DemoBrokerFactory` | no reason on 18 canned deals | `MISSING` |
| `Mt5Deal` entity | **no** | `MISSING` |
| `TraderDbContext` `mt5_deals` fluent map | no `Reason` property, no column | `MISSING` |
| EF `Migrations/` | **0** (C29 still holds) | `MISSING` |
| `EfTradingStore.UpsertDealAsync` | does not write reason | `MISSING` |
| `EfTradingStore.LoadDealsAsync` | does not map `Reason` (stays null) | `MISSING` |
| `ReconstructedTrade` / `ReconstructedTradeResult` | no `close_reason`, no `was_service_close` | `MISSING` |
| C++ `DealData` / `extractDeal` | omit `deal->Reason()` | `MISSING` |
| C++ `mt5_deals_ledger.reason_code` | column + `DealRevision.reasonCode` exist; never filled from `IMTDeal` | `EXISTS_NEEDS_REFACTOR` (orphan column) |
| A61 planned `mt5_deals` | **no** `reason` column in §8.8 | planned schema also omits it |
| A21 `DealIn.reason` default **0** | spec still unsafe (A82: do not coerce missing → CLIENT) | `UNSAFE` as a persist default |

**One-line:** domain can *talk* about deal reason; the database and every ingest DTO cannot *store* it.

---

## 1. What was read (no product edits)

| Path | Role | Measured |
|---|---|---|
| `D:\Prop\reports\swarm\20260818\A82_deal_reasons.md` | binding reason policy | 27727 bytes; SHA-256 `F0F03DF134996B99F6E446B6DA69EE286B6E9FF38020E6C2F66EDD2311260619` |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | in-memory deal | **1171** bytes / **29** lines; SHA-256 `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` |
| `D:\Prop\src\Domain\Enums\DealReason.cs` | 0–19 + `CountsAsTraderActivity` | **1149** / **50**; SHA-256 `3A4D92122D72155ACA3C0D9174758A966741A8B6E830917E6463CD905659E593` |
| `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | persist entity | **836** / **24**; SHA-256 `C81AEE8F15DA0EB1449DA3549A0FDD809D8C1607B9964F908830DD8F371F5487` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | `Mt5DealDto` (14 fields, no reason) | **1858** / **69**; SHA-256 `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | upsert + load | **12097** / **338**; SHA-256 `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | `mt5_deals` map | **5951** / **174**; SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (same as D19) |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | DTO → store, no transform | **4535** / **106**; SHA-256 `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | persisted lifecycle | **1430** / **36**; SHA-256 `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | recon output | **2042** / **43**; SHA-256 `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | uses `IsTradingDeal` only | **12768** / **347**; SHA-256 `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | only C# connector | **7049** / **170**; SHA-256 `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` (same as D24) |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | ingest via Fake DTO | `KillSwitch.Reason` is a string, not `DealReason` |
| `D:\Prop\tests\Unit\DealReasonTests.cs` | in-memory only | **1333** / **44**; SHA-256 `2B660B79B2D9BF812F637AF5200894FBA74536E7AB28439F8187CA956BAEB0E9` |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | upsert + seed; no reason assert | `Mt5DealDto` ctor has no reason slot |
| `D:\Prop\mt5-sdk\src\core\mt5_types.h` | `DealData` | **25328** bytes; SHA-256 `1D3BE309AC89141C82EFD8F775812913412B5AA293C9B300D948B65329A99C63` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | `extractDeal` | **62958**; SHA-256 `C25AD8CA9ACFBC5B64AB101C5BCDFCD1CF3CA6FE362BFCD2FC84EDC2EA2AFA98` |
| `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.h` / `.cpp` | `reasonCode` SQL | header SHA-256 `7C3683AC9063A284731AE51E61D1E650C397048230D93E3FC800C2216AA5A15F`; cpp SHA-256 `0BB2CD478EE1643FE886F6ECD4097742A0C0E759D43F4A4270470041937AF5BF` |
| `Persistence\Configurations\` | split EF maps | **0** files |
| Product `Migrations/` under `D:\Prop` excluding `bin`/`obj`/`node_modules` | versioned schema | **0** directories |

Grep of `Reason` / `DealReason` under `D:\Prop\src\Infrastructure` (except `KillSwitch.Reason` and `RiskDecisionRecord.Reason` strings) and under `D:\Prop\src\Mt5`: **0** deal-reason hits.

Grep of `reason` on `Mt5Deal.cs`, `Mt5Contracts.cs`, `TraderDbContext.cs`, `EfTradingStore.cs`: **0** hits.

---

## 2. A82 vs the working tree (what aged)

A82 §6 / §10 measured a tree where `NormalizedDeal` had **no** reason field and `IsTradingDeal` was action-only:

```25:25:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public bool IsTradingDeal => Action is DealAction.Buy or DealAction.Sell;
```

That quote is **STALE**. Current file:

```24:28:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public DealReason? Reason { get; init; }

    public bool IsTradingDeal =>
        Action is DealAction.Buy or DealAction.Sell
        && DealReasons.CountsAsTraderActivity(Reason);
```

| A82 §10 claim | Still true? |
|---|---|
| `IMTDeal::Reason()` exists on the SDK | **yes** |
| C++ `DealData` / `extractDeal` omit reason | **yes** (`mt5_types.h` 87–102; `extractDeal` 1508–1524 — no `deal->Reason()`) |
| C++ ledger `reason_code` declared, extractor does not fill it | **yes** — and still no C++ caller of `recordDealRevision` outside the store itself |
| `Mt5DealDto` / `Mt5Deal` have no reason | **yes** |
| `NormalizedDeal` has no reason | **STALE** — field added |
| No `DealReason` enum | **STALE** — `DealReason.cs` exists, 0–19 match `IMTDeal::EnDealReason` |
| `TradeReconstructor` is action-only | **partial** — it now honors `IsTradingDeal`, which reads `Reason` **if present** |
| A21 `reason` optional, default 0, stored only as `close_reason` | spec unchanged; persist of `close_reason` still **MISSING** |

A82 §11 persist suggestion (`NormalizedDeal` / `mt5_deals`: `reason uint32 NULL`; `ReconstructedTrade.close_reason` + `was_service_close`) is **not implemented** on any durable type.

B26 §3.4 (“Domain has **no** `Reason` (A82). Do not invent it in the stub”) is **STALE** for the in-memory record and **still correct** for `Mt5Deal`: do not invent a column by backfilling `CLIENT`.

---

## 3. In-memory support (not persistence)

`DealReason` mirrors A82’s C++ table (`CLIENT=0` … `CORPORATE_ACTION=19`). `DealReasons.CountsAsTraderActivity` accepts exactly A82 `REAL_TRADING`:

`0,1,2,3,4,5,7,9,10,16,17` (`Client, Expert, Dealer, StopLoss, TakeProfit, StopOut, ExternalClient, Gateway, Signal, Mobile, Web`).

Service / structural values (`Rollover, VariationMargin, Settlement, Transfer, Sync, ExternalService, Migration, Split, CorporateAction`) return false.

**Policy split vs A82 (binding for persist design):**

```32:35:D:\Prop\src\Domain\Enums\DealReason.cs
    public static bool CountsAsTraderActivity(DealReason? reason)
    {
        if (reason is null)
            return true;
```

A82 §0 / §7.1: reason **absent** = UNKNOWN = same as `SERVICE_STRUCTURAL` + `RECON_UNKNOWN_REASON`. **Do not** default missing to `CLIENT` (0). Current code treats null as trading. `DealReasonTests.Client_buy_still_counts` **locks that in**: `CountsAsTraderActivity(null).Should().BeTrue()`.

A82 §7.2–7.3 (money-only fold for rollover/vmargin; dirty-book structural apply; `was_service_close`) is **not** in `TradeReconstructor`. A non-trading reason is dropped by `.Where(d => d.IsTradingDeal)` — no money fold, no remaining update, no dirty flag. That is a recon policy gap, not a persist proof.

`TradeReconstructor` never reads `deal.Reason` except through `IsTradingDeal`. Completing deals do not set `close_reason`.

---

## 4. Persist path (C# product) — measured drop

```text
IMt5BrokerConnector.GetDealsAsync
        │  Mt5DealDto  (no Reason)
        ▼
DealIngestionService.SyncBrokerAsync     (pass-through)
        │
        ▼
EfTradingStore.UpsertDealAsync
        │  new Mt5Deal { … Comment, IngestedAt }   // no Reason
        ▼
TraderDbContext  table mt5_deals
        │  entity properties only; no Reason column can exist
        ▼
EfTradingStore.LoadDealsAsync
        │  new NormalizedDeal { … Comment }        // Reason omitted → null
        ▼
TradeReconstructor.Reconstruct
        │  IsTradingDeal: null reason ⇒ true
        ▼
ReplaceReconstructedAsync → ReconstructedTrade     // no close_reason
```

### 4.1 DTO

`Mt5DealDto` (`Mt5Contracts.cs` 24–38) is a positional record:

`DealTicket, Login, OrderTicket, PositionId, Symbol, Action, Entry, VolumeNative, Price, Profit, Commission, Swap, Time, Comment`.

No `DealReason`. No connector can supply one without a breaking DTO change.

### 4.2 Entity + EF

`Mt5Deal` properties: `Id, BrokerId, DealTicket, Login, OrderTicket, PositionId, Symbol, Action, Entry, VolumeNative, Price, Profit, Commission, Swap, DealTime, Comment, IngestedAt`.

`OnModelCreating` for `Mt5Deal` (TraderDbContext 58–64) sets table `mt5_deals`, PK, unique `(BrokerId, DealTicket)`, index `(BrokerId, Login, DealTime)`. No `Property(x => x.Reason)`, no column name, no converter. EF cannot persist a property that does not exist.

A61 §8.8 planned `mt5_deals` columns also omit `reason` (id, tickets, symbol, action, entry, volume, money, times, comment, payload_hash, payload, ingested_at). Payload jsonb is **not** on the current entity, so reason is not hidden there either.

No EF `Migrations/` folder exists (C29). `EnsureCreated` of the current model would not create a reason column.

### 4.3 Store write

`UpsertDealAsync` (EfTradingStore 85–114) copies DTO → entity fields listed above. First-write-wins on `(BrokerId, DealTicket)`. Even if a future DTO gained `Reason`, this method would still drop it until edited.

### 4.4 Store read

`LoadDealsAsync` (144–169) materializes `NormalizedDeal` with `BrokerId = brokerCode` and every persistable scalar **except** `StopLoss`, `TakeProfit`, and `Reason`. Those three stay default/null.

### 4.5 Reconstructed row

`ReconstructedTrade` has lifecycle / VWAP / flag columns. No `CloseReason`, no `WasServiceClose`, no `Dirty`. `ReplaceReconstructedAsync` (172–213) cannot write A82 §11 fields.

`ReconstructedTradeResult` likewise has no `close_reason`.

### 4.6 Seed / Fake / tests

`DemoBrokerFactory.ClosedRoundTrip` builds `Mt5DealDto(..., comment)` only. Integration `Deal_upsert_is_idempotent` constructs the same 14-arg DTO. Neither can persist a reason.

`DealReasonTests` constructs `NormalizedDeal` in process and never opens a `DbContext`. That is **not** a persist fact.

---

## 5. C++ ingest (still the same hole A82 recorded)

`DealData` (`mt5_types.h` 87–102): ticket, login, order, position, symbol, action, entry, volume, price, profit, commission, storage, time, comment. **No reason.**

`MT5Manager::extractDeal` (`mt5_manager.cpp` 1508–1524) copies those fields. `deal->Reason()` is never called (repo `src` grep of `deal->Reason` / `Reason()` on `mt5-sdk/src`: only `mt5ErrorReason` disconnect strings).

`mt5_ledger::DealRevision::reasonCode` (`mt5_ledger_store.h:45`) and SQL `reason_code` (`mt5_ledger_store.cpp` 80, 93) exist. `recordDealRevision` is the only writer. No `extractDeal` → `DealRevision` adapter lives in this tree. `mt5_ledger_store_test.cpp` has **0** `reason` matches. The column is an unused optional; it does not feed C# `NormalizedDeal`.

A13 §5 explicitly listed reason as a field **not** on `DealData` / the wire DTO.

---

## 6. What “persisted” would have to mean (A82 §11)

For the A82 predicates to run on a store-backed reconstruct, all of these must hold. **None** do today.

1. Extract `IMTDeal::Reason()` into `DealData` (nullable / omit ≠ 0).
2. Put `DealReason?` on `Mt5DealDto` and the Fake/live connector.
3. Put `DealReason?` (or `uint?`) on `Mt5Deal`; map `mt5_deals.reason` as `uint32 NULL`.
4. `UpsertDealAsync` write + `LoadDealsAsync` read the same field.
5. Do **not** backfill `CLIENT`. Absent ingest stays null / UNKNOWN.
6. Optionally persist `ReconstructedTrade.close_reason` + `was_service_close` after recon (A21 §4.1 / A82 §7.6).

Until (1)–(5) exist, unit tests that hand-set `Reason = DealReason.Rollover` do not describe production.

---

## 7. Effect of the gap (why this is not cosmetic)

`ReconstructionScoringService.RebuildTraderAsync` always loads deals from the store. Every reloaded `NormalizedDeal.Reason` is null. `IsTradingDeal` then reduces to “BUY or SELL”.

A82 E3 (overnight rollover OUT+IN) therefore still **completes a lifecycle and burns a first-3 slot** on any store-backed run. A82 E4 (migration IN) and E5 (SPLIT as fake scale-in) remain live. C51 already flagged “reason-blind `ENTRY_IN` still looks like a scale-in.”

Demo seed never emits service reasons, so InMemory seed tests stay green while the filter is unused.

---

## 8. Findings

1. **`DealReason` is not persisted.** No column, no DTO field, no store mapping, no reconstructed `close_reason`.
2. **In-memory only.** `NormalizedDeal.Reason` + `DealReasons` exist and match A82’s 0–19 / REAL_TRADING set.
3. **A82 §6/§10 is stale on the domain type, current on persist and C++ extract.**
4. **Null ≠ UNKNOWN.** `CountsAsTraderActivity(null) == true` plus a missing column means every ingested BUY/SELL is treated as trader activity. A82 forbids coercing missing reason to `CLIENT` and forbids treating absence as real trading.
5. **A61 `mt5_deals` plan also omits `reason`.** Adding the domain field without a schema change will not create a column.
6. **C++ `reason_code` is not a C# persist path.** Declared, unpopulated, untested, unwired.
7. **No test proves durability.** `DealReasonTests` is in-memory. Integration upsert cannot even name the field.

**Product source was not modified.** This file is the only write from D44.
