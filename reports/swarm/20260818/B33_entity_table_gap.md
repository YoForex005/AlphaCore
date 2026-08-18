# B33 — Domain/Entities vs architecture §45 missing tables

| Field | Value |
|---|---|
| Agent | B33 (Domain entity ↔ §45 table gap only) |
| Date | 2026-08-18 |
| Left | `D:\Prop\src\Domain\Entities\*.cs` (declared persist types, not filenames) |
| Right | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§45** “Recommended Core Database Tables” (lines 1672–1731) |
| Supporting catalogs | `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` (47-table union), `A61_efcore_schema.md` (target CLR names) |
| Sibling reports | B01 (Domain compile / type inventory), B03 (Infrastructure `ToTable` gap), B21 (DbSet ↔ Entities type existence) |
| Product source modified | **No.** This report is the only write. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest)

**18 of 43 §45 tables have a persist type under `Domain\Entities`. 25 §45 tables have no Domain entity. Coverage is 41.9% by name, 0% by A20/A61 completeness.**

This is a **type-existence** census, not a schema review. B03 already measured EF `ToTable` quality. B21 already measured `TraderDbContext` ↔ Entities type pairing. This file answers only: *which §45 tables have no class in `Domain\Entities`, and which Entities classes are not §45 tables.*

| Question | Count | Answer |
|---|---|---|
| §45 “full initial set” table names | **43** | verbatim list in §1 |
| Files in `Domain\Entities\` | **20** | one `.cs` per type |
| Declared persist types (`class` / `record` / `struct`) | **20** | all `public sealed class` in `TraderIntelligence.Domain.Entities` |
| §45 tables with a matching persist type | **18** | §3 PRESENT |
| §45 tables with **no** `Domain\Entities` type | **25** | §4 MISSING — **this is the assigned list** |
| Entities types that are **not** in §45 | **2** | `ExecutionIntent` (§44), `KillSwitch` (not §11/§44/§45) |
| In-memory types that look like tables but are **not** Entities | **4+** | `ShadowFill`, `ShadowPosition` (`Domain.Shadow`); `FeatureSnapshot` (`Domain.Scoring`); `TraderState` enum; `Risk.DestinationQuote` |
| A20 union extras still missing as Entities (not in §45) | **3** | `ingestion_events`, `execution_reconciliation_runs`, `execution_reconciliation_issues` |
| `EXISTS_AND_GOOD` vs A20/A61 field + identity contract | **0** | every present type is a demo stub |

Do **not** treat `FeatureSnapshot`, `ShadowFill`, or `ShadowPosition` as closing `trader_feature_snapshots` / `shadow_fills` / `shadow_positions`. They are engine DTOs, not persist entities, and they do not live under `Domain\Entities`.

Do **not** treat `TraderScore.CurrentState` + three bool flags as closing `trader_states` + `trader_risk_flags`. Those are inlined columns on another table’s stub.

---

## 1. What §45 actually lists (43 names)

Architecture §45 “Full initial set” (verbatim, grouped as printed):

```text
brokers
broker_connections
mt5_groups
plan_group_mappings

mt5_accounts
mt5_account_snapshots
mt5_orders
mt5_deals
mt5_positions_current
mt5_symbols
mt5_xau_ticks

reconstructed_trades
canonical_instruments
source_symbol_mappings

trader_feature_snapshots
trader_scores
trader_score_history
trader_states
trader_risk_flags

model_versions
model_predictions
model_evaluations

shadow_orders
shadow_fills
shadow_positions
shadow_performance

copy_intents
copy_allocations

risk_decisions
risk_events

execution_venues
destination_symbols
destination_quotes

fix_sessions
fix_session_events
fix_orders
fix_execution_reports
destination_positions

source_destination_links

sync_checkpoints
outbox_events
audit_logs
system_events
```

**43 names.** Counted from the architecture file, not from A20.

`model_*` **are in §45**. A52 forbids building the ML **service**; it does **not** drop the three schema types. Missing entity = `MISSING` (schema-only later).

Aliases that must **not** be counted as extra tables (A20 §1):

| Alias (other sections) | Canonical §45 name |
|---|---|
| `mt5_symbol_metadata` (§11) | `mt5_symbols` |
| `mt5_ticks_xauusd` (§11) | `mt5_xau_ticks` |
| `shadow_copy_order` / `shadow_copy_fill` / `shadow_position` (§24) | `shadow_orders` / `shadow_fills` / `shadow_positions` |
| `shadow_pnl` / `source_vs_shadow_slippage` (§44/§24) | `shadow_performance` |

Tables listed in §11 / §44 but **omitted from §45** are **out of this report’s primary 43**. They appear only in §6 so implementers do not invent a second name.

---

## 2. Measured `Domain\Entities` census (2026-08-18)

`list_dir` + `Get-FileHash SHA256` of `D:\Prop\src\Domain\Entities`. **20 files, 20 types.** `Class1.cs` is gone. No `partial`, no `record` persist types, no extra folders.

| Bytes | SHA-256 | File | Declared type |
|---|---|---|---|
| 403 | `FD5CD6EC5D18D3A0350891A21EF0B8E5FD33323035FD88ED06A95356B8EA4BA6` | `AuditLog.cs` | `AuditLog` |
| 778 | `412FF86681DF6189C3673762C38B22622A471C1578B5555E85827AAE02DEF19D` | `Broker.cs` | `Broker` |
| 222 | `E71ABCDE52601E0FAF21B56DAE450BB429584878841F8C22BAD067B5EE1608D9` | `CanonicalInstrument.cs` | `CanonicalInstrument` |
| 759 | `336123499E347CAF355D483C77F4E724661A90D95206B28B814DCB3E0EA628E5` | `CopyIntent.cs` | `CopyIntent` |
| 421 | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | `DestinationQuote.cs` | `DestinationQuoteSnapshot` *(filename ≠ type)* |
| 756 | `4E5EEDFAAED61B56573C5F1AC7D49F9C8424F27D5A4CEE023F756FA71F22D6B4` | `ExecutionIntent.cs` | `ExecutionIntent` |
| 979 | `46C20D6A1BF5F84769DB483FD17A0EBEB8BDA8C1C56BBA2B8B30A59FCE44697E` | `FixSessionState.cs` | `FixSessionState` |
| 329 | `68EA2D92E88AD7CEFE37C20ADD56AEBA988E1A3D1424EF0D5EE45A961C2EEC4D` | `KillSwitch.cs` | `KillSwitch` |
| 639 | `B13CB025741FB7DDF290B67070727C9FAFC0FDF071572FCD1DB7CCADDB6DA549` | `Mt5Account.cs` | `Mt5Account` |
| 836 | `C81AEE8F15DA0EB1449DA3549A0FDD809D8C1607B9964F908830DD8F371F5487` | `Mt5Deal.cs` | `Mt5Deal` |
| 693 | `05C07CA07C35FCE9D7A5E06B5BF302997E0C092E7E606B5511F43FE2B9623DB3` | `Mt5Group.cs` | `Mt5Group` |
| 776 | `C1C8A7E66A1CE40C574A5A9D0B0C95F1E6D7C163C2F896A6D0FE7AFC7FAAF6FE` | `Mt5Position.cs` | `Mt5Position` |
| 546 | `78108643D4C8E25DBEA767C30145366B3337C59D6E39EA3F613B480CDE6649A8` | `OutboxEvent.cs` | `OutboxEvent` |
| 1430 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` | `ReconstructedTrade.cs` | `ReconstructedTrade` |
| 457 | `C8FA95BF79339579B049CE74135052AED507C90E2055B350C0E7C8B1F728B4CE` | `RiskDecisionRecord.cs` | `RiskDecisionRecord` |
| 556 | `8EF2D2372CFC01A27CBCA4A1855A322B54A4439FCB6B11AA3A5404FD0D1F8B86` | `ShadowOrder.cs` | `ShadowOrder` |
| 276 | `6FACF2DCB5FF8C2F2BC9ECE4EB35C48DC2027231380F8F10EAE59D49D921FB73` | `SourceSymbolMapping.cs` | `SourceSymbolMapping` |
| 391 | `15FF40719E5FE3ADBA8B2F0E6D7215C02D2B813EC84A1E092EC1D5BE9CB83056` | `SyncCheckpoint.cs` | `SyncCheckpoint` |
| 652 | `48E4C10B5E5A356DA5BB824A32D0A4C857AA2208FA9E4EDE7D145BCCB401ECBA` | `TraderScore.cs` | `TraderScore` |
| 473 | `3AFA422B6FAFC36994C99CBD8A4C0BB5FB7997688FDB4BEC11F8CA0A7F2CEFD1` | `TraderScoreHistory.cs` | `TraderScoreHistory` |

Pairing rule used below (same as B21): **class name**, not filename. `DestinationQuote.cs` maps to `destination_quotes` via `DestinationQuoteSnapshot`.

Target CLR names in the PRESENT / MISSING tables come from A61 §3.1. They are the names implementers should add; they do **not** exist today unless the “Domain type now” column says so.

---

## 3. PRESENT — §45 table has a `Domain\Entities` type (18)

These are **name-level hits**. Every one is `EXISTS_NEEDS_REFACTOR` against A20 keys / A61 columns. None is `EXISTS_AND_GOOD`.

| # | §45 table | Domain type now | A61 target name | Why not GOOD |
|---|---|---|---|---|
| 1 | `brokers` | `Broker` | `Broker` | Connection fields mixed in (`Server`, `Port`, `ManagerLogin`, proxy). A61 splits those onto `broker_connections`. No `Kind` / `IsEnabled` naming match (`Enabled`). |
| 3 | `mt5_groups` | `Mt5Group` | `Mt5Group` | Property is `Name`, not `GroupName`. `PlanMapping` string lives on the group (belongs on `plan_group_mappings`). |
| 5 | `mt5_accounts` | `Mt5Account` | `Mt5Account` | Latest book denorm only; no snapshot entity. Missing several A61 columns (`Name`, `Currency`, `IsEnabled`, `LastEventAt`). |
| 8 | `mt5_deals` | `Mt5Deal` | `Mt5Deal` | `Symbol` not `SourceSymbol`. `Action`/`Entry` are Domain enums, not SDK `smallint`. No `TimeMsc` / `Payload` / `PayloadHash`. |
| 9 | `mt5_positions_current` | `Mt5Position` | `Mt5PositionCurrent` | **Wrong type name.** Identity property is `PositionTicket`, not `PositionId`. `Symbol` not `SourceSymbol`. |
| 12 | `reconstructed_trades` | `ReconstructedTrade` | `ReconstructedTrade` | Volumes stored as lots (`decimal`), A61 prefers native `bigint`. No ticket list / remaining volume. |
| 13 | `canonical_instruments` | `CanonicalInstrument` | `CanonicalInstrument` | Property is `Code`, not `CanonicalSymbol`. |
| 14 | `source_symbol_mappings` | `SourceSymbolMapping` | `SourceSymbolMapping` | FK is `CanonicalInstrumentId` uuid; A61 uses `canonical_symbol` text → instruments. |
| 16 | `trader_scores` | `TraderScore` | `TraderScore` | One blended row; no `score_kind`. Risk flags and `CurrentState` inlined (hides two missing tables). |
| 17 | `trader_score_history` | `TraderScoreHistory` | `TraderScoreHistory` | Keyed by `RecordedAt`, not `(completed_trade_count, score_kind, model_version_id)`. |
| 23 | `shadow_orders` | `ShadowOrder` | `ShadowOrder` | No `shadow_cl_ord_id`. Fill-ish fields (`FilledAt`, slippage) belong on `shadow_fills`. |
| 27 | `copy_intents` | `CopyIntent` | `CopyIntent` | Idempotency is a string `IdempotencyKey`, not A20 compound `(source_broker_id, source_login, source_trade_id, source_event_id, action)`. |
| 29 | `risk_decisions` | `RiskDecisionRecord` | `RiskDecision` | **Wrong type name.** No `decision_seq`. Collides in *meaning* with `Domain.Risk.RiskDecision` (engine record). |
| 33 | `destination_quotes` | `DestinationQuoteSnapshot` | `DestinationQuote` | No `venue_id`. Filename/type split with `Domain.Risk.DestinationQuote`. |
| 34 | `fix_sessions` | `FixSessionState` | `FixSession` | **Wrong type name.** No `venue_id`. Unique key in EF is `Qualifier` only. |
| 40 | `sync_checkpoints` | `SyncCheckpoint` | `SyncCheckpoint` | Shape is `(BrokerId, Login, Stream)`, not `(scope_type, scope_id, stream_name)`. Cannot represent venue/global cursors. |
| 41 | `outbox_events` | `OutboxEvent` | `OutboxEvent` | No `aggregate_type` / `dedupe_key` / `payload jsonb`. `Type` is an enum, not free `text`. |
| 42 | `audit_logs` | `AuditLog` | `AuditLog` | `Id` is `Guid`; A20 wants `bigint GENERATED ALWAYS AS IDENTITY`. |

### 3.1 PRESENT type → table (quick index)

```text
Broker                     → brokers
Mt5Group                   → mt5_groups
Mt5Account                 → mt5_accounts
Mt5Deal                    → mt5_deals
Mt5Position                → mt5_positions_current
ReconstructedTrade         → reconstructed_trades
CanonicalInstrument        → canonical_instruments
SourceSymbolMapping        → source_symbol_mappings
TraderScore                → trader_scores
TraderScoreHistory         → trader_score_history
ShadowOrder                → shadow_orders
CopyIntent                 → copy_intents
RiskDecisionRecord         → risk_decisions
DestinationQuoteSnapshot   → destination_quotes
FixSessionState            → fix_sessions
SyncCheckpoint             → sync_checkpoints
OutboxEvent                → outbox_events
AuditLog                   → audit_logs
```

---

## 4. MISSING — §45 tables with no `Domain\Entities` type (25)

**This is the assigned list.** No file, no class, no `record` under `Domain\Entities` maps to these tables. Suggested type names are A61 §3.1 (do not invent a second name).

| # | §45 table | Suggested entity (A61) | Side | Why it is missing (not “hidden”) |
|---|---|---|---|---|
| 2 | `broker_connections` | `BrokerConnection` | S | Connection fields are illegally parked on `Broker`. No separate type. |
| 4 | `plan_group_mappings` | `PlanGroupMapping` | S | Overlay only exists as `Mt5Group.PlanMapping` string. |
| 6 | `mt5_account_snapshots` | `Mt5AccountSnapshot` | S | Latest balance lives on `Mt5Account`. No append-only book. |
| 7 | `mt5_orders` | `Mt5Order` | S | Raw order layer absent. Reconstruction cannot persist `order_count` from a durable order store. |
| 10 | `mt5_symbols` | `Mt5Symbol` | S | No per-broker contract metadata (`digits`, volume min/step/max). |
| 11 | `mt5_xau_ticks` | `Mt5XauTick` | S | No source tick stream type. MFE/MAE cannot be exact. |
| 15 | `trader_feature_snapshots` | `TraderFeatureSnapshot` | S | `Domain.Scoring.FeatureSnapshot` is an in-memory scorer DTO, **not** an entity. No persist type. |
| 18 | `trader_states` | `TraderStateRecord` | S | `TraderState` is an **enum**. Current state is a column on `TraderScore`. No 1:1 state row. |
| 19 | `trader_risk_flags` | `TraderRiskFlag` | S | Three bools on `TraderScore` (`Martingale`, `AveragingDown`, `LotEscalation`). Not a flag table. |
| 20 | `model_versions` | `ModelVersion` | G | No ML schema types. A52: create empty schema later; do not add a service. |
| 21 | `model_predictions` | `ModelPrediction` | S | Same. |
| 22 | `model_evaluations` | `ModelEvaluation` | G | Same. |
| 24 | `shadow_fills` | `ShadowFill` | X | `Domain.Shadow.ShadowFill` is an engine `record` (`string ShadowOrderId`). Not under Entities, not mapped. |
| 25 | `shadow_positions` | `ShadowPosition` | X | `Domain.Shadow.ShadowPosition` is an unused engine `record` (`string BrokerId`). Not an entity. |
| 26 | `shadow_performance` | `ShadowPerformance` | X | Slippage is a column on `ShadowOrder`. No rollup grain type. |
| 28 | `copy_allocations` | `CopyAllocation` | X | Sizing result is not persisted. `CopyIntent.RequestedQuantity` is the only number. |
| 30 | `risk_events` | `RiskEvent` | X | No append-only kill-switch / pause / limit-breach stream. |
| 31 | `execution_venues` | `ExecutionVenue` | D | No venue registry. FIX session has no `venue_id`. |
| 32 | `destination_symbols` | `DestinationSymbol` | D | Security-list mapping absent. Quote row has optional `VenueInstrumentId` only. |
| 35 | `fix_session_events` | `FixSessionEvent` | D | Session mutations overwrite `FixSessionState`. No event log. |
| 36 | `fix_orders` | `FixOrder` | D | No venue-visible order type. `ExecutionIntent` is §44, not this table. |
| 37 | `fix_execution_reports` | `FixExecutionReport` | D | No durable ER type. Enum `ReconciliationIssueType` exists; no ER entity uses it. |
| 38 | `destination_positions` | `DestinationPosition` | D | No cTrader position book type. |
| 39 | `source_destination_links` | `SourceDestinationLink` | X | No explicit source-trade ↔ dest-order/position map. |
| 43 | `system_events` | `SystemEvent` | G | No platform health / mode stream. |

### 4.1 Missing names only (copy-paste)

```text
broker_connections
plan_group_mappings
mt5_account_snapshots
mt5_orders
mt5_symbols
mt5_xau_ticks
trader_feature_snapshots
trader_states
trader_risk_flags
model_versions
model_predictions
model_evaluations
shadow_fills
shadow_positions
shadow_performance
copy_allocations
risk_events
execution_venues
destination_symbols
fix_session_events
fix_orders
fix_execution_reports
destination_positions
source_destination_links
system_events
```

**25.**

### 4.2 Missing by layer

| Layer | Missing §45 tables | Count |
|---|---|---|
| Registry / source overlay | `broker_connections`, `plan_group_mappings` | 2 |
| Raw MT5 | `mt5_account_snapshots`, `mt5_orders`, `mt5_symbols`, `mt5_xau_ticks` | 4 |
| Scoring / state | `trader_feature_snapshots`, `trader_states`, `trader_risk_flags` | 3 |
| ML schema (no service) | `model_versions`, `model_predictions`, `model_evaluations` | 3 |
| Shadow | `shadow_fills`, `shadow_positions`, `shadow_performance` | 3 |
| Copy / risk | `copy_allocations`, `risk_events` | 2 |
| Destination / FIX | `execution_venues`, `destination_symbols`, `fix_session_events`, `fix_orders`, `fix_execution_reports`, `destination_positions`, `source_destination_links` | 7 |
| Ops | `system_events` | 1 |
| **Total** | | **25** |

Phase implication (architecture §67, honest): Phase 1 ingestion is already blocked on `mt5_orders` + `mt5_account_snapshots` + `mt5_symbols`. Phase 2 reconstruction can run in memory (`TradeReconstructor`) but cannot persist a complete raw layer. Phase 4–5 shadow/FIX cannot persist venue identity without `execution_venues` / `destination_symbols` / shadow fill+position. Phase 6 must not add XGBoost; it **may** add the three empty `model_*` types.

---

## 5. Entities that are **not** §45 tables (2)

These exist under `Domain\Entities` and are mapped by `TraderDbContext`. They do **not** count toward 18/43.

| Type | File | EF table today | Origin | Action |
|---|---|---|---|---|
| `ExecutionIntent` | `ExecutionIntent.cs` | `execution_intents` | §44 / §33 | **Keep.** Required before any FIX send. A20 #37. Not a §45 miss. |
| `KillSwitch` | `KillSwitch.cs` | `kill_switches` | **not** in §11 / §44 / §45 | **Do not treat as a §45 table.** A48 wants two independent controls plus `risk_events` / `audit_logs` / `system_events`. A single exclusive `KillSwitchMode` row is **UNSAFE** if used as the live latch. |

No other Entities type is extra. There is no `IngestionEvent`, no reconciliation-run type, no venue type hiding under another name.

---

## 6. Look-alikes that do **not** close a §45 gap

Do not credit these as Domain entities for the missing tables.

| Look-alike | Where it lives | Looks like | Why it does not count |
|---|---|---|---|
| `TraderState` enum | `Domain\Enums\TraderState.cs` | `trader_states` | Enum of tokens. No persist row, no `(broker_id, login)` identity. |
| `FeatureSnapshot` record | `Domain\Scoring\BaselineScorer.cs` | `trader_feature_snapshots` | Scorer DTO. Not under Entities. Not mapped. |
| `ShadowFill` record | `Domain\Shadow\ShadowCopyEngine.cs` | `shadow_fills` | Engine output. `ShadowOrderId` is `string`. Unused as persist. |
| `ShadowPosition` record | `Domain\Shadow\ShadowCopyEngine.cs` | `shadow_positions` | Engine output. `BrokerId` is `string` code. B01: unused even in-process. |
| `DestinationQuote` record | `Domain\Risk\RiskEngine.cs` | `destination_quotes` | In-memory quote for risk. Persist type is `DestinationQuoteSnapshot` (already counted PRESENT). |
| `RiskDecision` record | `Domain\Risk\RiskEngine.cs` | `risk_decisions` | Engine result. Persist type is `RiskDecisionRecord` (already counted PRESENT). |
| `ReconstructedTradeResult` | `Domain\Reconstruction` | `reconstructed_trades` | Engine result. Persist type is `ReconstructedTrade` (already counted PRESENT). |
| `CanonicalInstrumentRef` | `Domain\Instruments` | `canonical_instruments` | In-memory `record(string Code)`. |
| `ReconciliationIssueType` enum | `Domain\Enums` | `execution_reconciliation_issues` | Enum only. Table is not even in §45 (it is §44). No entity. |

---

## 7. A20 extras (not §45) — still no Entities type

A20 union is **47** = 43 §45 + 4 extras. Entity status of the four:

| Table | Origin | Domain\Entities type | Status |
|---|---|---|---|
| `execution_intents` | §44, §33 | `ExecutionIntent` | **PRESENT** (extra vs §45) |
| `ingestion_events` | §11 | **none** | **MISSING** |
| `execution_reconciliation_runs` | §44, §42–43 | **none** | **MISSING** |
| `execution_reconciliation_issues` | §44, §43 | **none** | **MISSING** (`ReconciliationIssueType` enum only) |

If someone asks “Entities vs the A20 catalog,” the miss list is the 25 in §4 **plus** these three. This report’s contract is **§45 = 43**.

---

## 8. Full 43-row matrix

| # | §45 table | Domain\Entities type | Class |
|---|---|---|---|
| 1 | `brokers` | `Broker` | **EXISTS_NEEDS_REFACTOR** |
| 2 | `broker_connections` | — | **MISSING** |
| 3 | `mt5_groups` | `Mt5Group` | **EXISTS_NEEDS_REFACTOR** |
| 4 | `plan_group_mappings` | — | **MISSING** |
| 5 | `mt5_accounts` | `Mt5Account` | **EXISTS_NEEDS_REFACTOR** |
| 6 | `mt5_account_snapshots` | — | **MISSING** |
| 7 | `mt5_orders` | — | **MISSING** |
| 8 | `mt5_deals` | `Mt5Deal` | **EXISTS_NEEDS_REFACTOR** |
| 9 | `mt5_positions_current` | `Mt5Position` (wrong name) | **EXISTS_NEEDS_REFACTOR** |
| 10 | `mt5_symbols` | — | **MISSING** |
| 11 | `mt5_xau_ticks` | — | **MISSING** |
| 12 | `reconstructed_trades` | `ReconstructedTrade` | **EXISTS_NEEDS_REFACTOR** |
| 13 | `canonical_instruments` | `CanonicalInstrument` | **EXISTS_NEEDS_REFACTOR** |
| 14 | `source_symbol_mappings` | `SourceSymbolMapping` | **EXISTS_NEEDS_REFACTOR** |
| 15 | `trader_feature_snapshots` | — | **MISSING** |
| 16 | `trader_scores` | `TraderScore` | **EXISTS_NEEDS_REFACTOR** |
| 17 | `trader_score_history` | `TraderScoreHistory` | **EXISTS_NEEDS_REFACTOR** |
| 18 | `trader_states` | — (enum + column only) | **MISSING** |
| 19 | `trader_risk_flags` | — (bools on `TraderScore`) | **MISSING** |
| 20 | `model_versions` | — | **MISSING** |
| 21 | `model_predictions` | — | **MISSING** |
| 22 | `model_evaluations` | — | **MISSING** |
| 23 | `shadow_orders` | `ShadowOrder` | **EXISTS_NEEDS_REFACTOR** |
| 24 | `shadow_fills` | — | **MISSING** |
| 25 | `shadow_positions` | — | **MISSING** |
| 26 | `shadow_performance` | — | **MISSING** |
| 27 | `copy_intents` | `CopyIntent` | **EXISTS_NEEDS_REFACTOR** |
| 28 | `copy_allocations` | — | **MISSING** |
| 29 | `risk_decisions` | `RiskDecisionRecord` (wrong name) | **EXISTS_NEEDS_REFACTOR** |
| 30 | `risk_events` | — | **MISSING** |
| 31 | `execution_venues` | — | **MISSING** |
| 32 | `destination_symbols` | — | **MISSING** |
| 33 | `destination_quotes` | `DestinationQuoteSnapshot` | **EXISTS_NEEDS_REFACTOR** |
| 34 | `fix_sessions` | `FixSessionState` (wrong name) | **EXISTS_NEEDS_REFACTOR** |
| 35 | `fix_session_events` | — | **MISSING** |
| 36 | `fix_orders` | — | **MISSING** |
| 37 | `fix_execution_reports` | — | **MISSING** |
| 38 | `destination_positions` | — | **MISSING** |
| 39 | `source_destination_links` | — | **MISSING** |
| 40 | `sync_checkpoints` | `SyncCheckpoint` | **EXISTS_NEEDS_REFACTOR** |
| 41 | `outbox_events` | `OutboxEvent` | **EXISTS_NEEDS_REFACTOR** |
| 42 | `audit_logs` | `AuditLog` | **EXISTS_NEEDS_REFACTOR** |
| 43 | `system_events` | — | **MISSING** |

**18 PRESENT + 25 MISSING = 43.**

---

## 9. What this report does **not** claim

- It does **not** claim the 18 present types are production-ready. They are demo property bags.
- It does **not** re-score EF mappings. B03: 18/43 `ToTable` names, 0/43 A61 configurations, 0 migrations.
- It does **not** claim `TraderDbContext` is missing a type. B21: 20/20 DbSets resolve.
- It does **not** add or rename Domain types. Implementers add the 25 missing entities (A61 names) in a later coding wave, then map them. Domain stays attribute-free.
- It does **not** authorize creating `*.MUTATED*` or hand-written `.mq5`. Out of scope.

---

## 10. Implementer add-list (Domain only, later wave)

When a coding agent is assigned this gap, add **one persist type per missing §45 table** under `D:\Prop\src\Domain\Entities`, using the A61 name. Do not reuse engine DTOs by moving them:

```text
BrokerConnection.cs          → class BrokerConnection
PlanGroupMapping.cs          → class PlanGroupMapping
Mt5AccountSnapshot.cs        → class Mt5AccountSnapshot
Mt5Order.cs                  → class Mt5Order
Mt5Symbol.cs                 → class Mt5Symbol
Mt5XauTick.cs                → class Mt5XauTick
TraderFeatureSnapshot.cs     → class TraderFeatureSnapshot   // not Scoring.FeatureSnapshot
TraderStateRecord.cs         → class TraderStateRecord       // not enum TraderState
TraderRiskFlag.cs            → class TraderRiskFlag
ModelVersion.cs              → class ModelVersion
ModelPrediction.cs           → class ModelPrediction
ModelEvaluation.cs           → class ModelEvaluation
ShadowFill.cs                → class ShadowFill              // new Entities type; rename engine record if CS0104
ShadowPosition.cs            → class ShadowPosition          // same
ShadowPerformance.cs         → class ShadowPerformance
CopyAllocation.cs            → class CopyAllocation
RiskEvent.cs                 → class RiskEvent
ExecutionVenue.cs            → class ExecutionVenue
DestinationSymbol.cs         → class DestinationSymbol
FixSessionEvent.cs           → class FixSessionEvent
FixOrder.cs                  → class FixOrder
FixExecutionReport.cs        → class FixExecutionReport
DestinationPosition.cs       → class DestinationPosition
SourceDestinationLink.cs     → class SourceDestinationLink
SystemEvent.cs               → class SystemEvent
```

Rename collisions to resolve **before** adding Entities `ShadowFill` / `ShadowPosition` / `DestinationQuote`: engine records in `Domain.Shadow` / `Domain.Risk` will CS0104 the moment a consumer imports both namespaces. B01 §4.2 already flagged `DestinationQuote`.

Optional A20 extras (not required to close this §45 list): `IngestionEvent`, `ExecutionReconciliationRun`, `ExecutionReconciliationIssue`.

---

## 11. Method / evidence

1. Read architecture §45 (lines 1672–1731) and counted 43 names.
2. `list_dir` + SHA-256 of `D:\Prop\src\Domain\Entities` — 20 files.
3. `grep` `public sealed class` under Entities — 20 types. No extra persist types elsewhere in Domain.
4. Cross-checked A61 target names, A20 catalog, B01 type inventory, B03 infra matrix, B21 DbSet pairing.
5. Confirmed look-alikes (`FeatureSnapshot`, `ShadowFill`, `ShadowPosition`, `TraderState`) live outside `Domain\Entities`.

No product `.cs` / `.csproj` / migration was created or edited.

**Score: 18/43 §45 tables have a Domain entity (41.9%). 25 missing. 2 extra non-§45 entities. 0/43 field-complete.**
