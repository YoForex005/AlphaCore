# A10 — Integration tests audit (`tests/Integration` vs Architecture §60 + §61)

**Date:** 2026-08-18  
**Auditor:** senior engineer (swarm A10)  
**Scope:** `D:\Prop\tests\Integration` vs Architecture §60 required **integration** tests and Architecture §61 **FIX simulation / test harness**  
**Sources:**
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`
  - §60 “Testing Strategy” → Integration tests (lines 2259–2273)
  - §60 Replay (lines 2275–2293) — adjacent, not claimed as Integration
  - §61 “FIX Simulation / Test Harness” (lines 2297–2315)
  - §12 backfill / live / outbox (lines 538–571)
  - §13 transactional outbox (lines 575–591)
  - §33–§35 idempotency / unknown state / position mapping (lines 1302–1398)
  - §41 feature flags (lines 1564–1588)
  - §42–§43 reconciliation (lines 1594–1641)
  - §44–§45 execution / core tables (lines 1645–1731)
  - §66 suggested `/tests/Integration`, `/tests/Fix`, `/tests/Replay` (lines 2451–2456)
  - §67 phases 1 / 4 / 7 / 8 (lines 2493–2601)
  - §68 go-live gates (lines 2605–2628)
  - §70 live FIX acceptance (lines 2658–2677)
- `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj`
- `D:\Prop\tests\Integration\UnitTest1.cs`
- `D:\Prop\tests\Unit\` (sibling scaffold only; see A09)
- `D:\Prop\Mt5TraderIntelligence.sln`
- Product snapshot under `D:\Prop\src\`, `D:\Prop\apps\` at audit time

**Product source:** not modified by this audit.

**Companion:** `D:\Prop\reports\swarm\20260818\A09_unit_tests_audit.md` maps the 17 §60 **unit** areas. This file maps the 8 §60 **integration** areas + 7 §61 harness capabilities. Do not double-count.

---

## Verdict

**FAIL.**

| Gate | Required | Implemented | Evidence |
|---|---:|---:|---|
| §60 integration areas | 8 | **0** | no class, fixture, or assertion matches any required item |
| §61 FIX harness capabilities | 7 | **0** | no adapter test mode, no recorded FIX fixtures, no simulator |
| §61 “do not use the real account as the first integration test” | 1 hard rule | **unenforceable** | no harness, no venue-block, no live-skip trait |
| Runnable xUnit facts | — | 1 | empty `UnitTest1.Test1` — **false green** |

`tests/Integration` is the Visual Studio xUnit template plus three extra package/project references. It is not an integration suite.

A passing `dotnet test` on this project is **not** evidence of PostgreSQL, MT5, outbox, QuickFIX, ExecutionReport, reconciliation, or unknown-state recovery. Treating it as such would greenwash §68 / §70 live-execution gates.

---

## Measured run (this audit)

Command (read-only):

```text
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj --nologo --verbosity minimal
```

Result (2026-08-18, net8.0):

```text
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: < 1 ms
  TraderIntelligence.Tests.Integration.dll
```

That duration is the signature of an empty `[Fact]`. No database, no FIX socket, no worker host, no assertion.

Sibling `tests/Unit` is the same shape: 1 empty pass (`A09`). Combined C# automated proof of trading behavior: **0 facts**.

`D:\Prop\test-release.log` is empty (0 bytes). No CI workflow exists (`D:\Prop\.github` absent). No `docker-compose` exists. There is no pipeline that could distinguish this false green from a real suite.

---

## Current `tests/Integration` inventory

| Path | SHA-256 (this audit) | Role | Status |
|---|---|---|---|
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | `93B83D025007315F2979FA52A0B6E6CDC8AC0C554A019FBD0CFAE2D086712019` | net8.0 xUnit project | Present |
| `D:\Prop\tests\Integration\UnitTest1.cs` | `A5DB477AEF20B9B17B81FF543F79998B88ADCD3C43F47D41571C754CCA56E0F7` | `TraderIntelligence.Tests.Integration.UnitTest1.Test1()` empty body | Placeholder only |
| `D:\Prop\tests\Integration\bin\*`, `obj\*` | — | restore / compile artifacts from this audit run | Ignore |

**No other files exist under `tests/Integration`.** No `Fixtures/`, `Harness/`, `Migrations/`, `appsettings*.json`, `xunit.runner.json`, `FIX44*.xml`, recorded `.fix` messages, collection fixtures, or skip/trait helpers.

### Project references (today)

```text
..\..\src\Domain\TraderIntelligence.Domain.csproj
..\..\src\Application\TraderIntelligence.Application.csproj
..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
..\..\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj
```

**Not referenced:** `src\Mt5`, `apps\mt5-worker`, `apps\fix-worker`, `apps\api`.

MT5 backfill/restart (§60 item 2) cannot be compiled against `TraderIntelligence.Mt5` from this project until that reference is added. Worker hosts are not required as project references if the SUT is extracted into libraries; those libraries do not exist yet.

### Packages (today)

| Package | Version | Used by any test? | Fit for §60/§61? |
|---|---|---|---|
| `xunit` | 2.5.3 | yes (`[Fact]`) | runner only |
| `xunit.runner.visualstudio` | 2.5.3 | adapter | runner only |
| `Microsoft.NET.Test.Sdk` | 17.8.0 | host | runner only |
| `FluentAssertions` | 6.12.0 | **no** | fine later; unused |
| `coverlet.collector` | 6.0.0 | **no** | unused; no coverage settings |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.4 | **no** | **wrong** for “PostgreSQL migrations” |

**Missing packages (honest blockers, not installed):**

- `Testcontainers` / `Testcontainers.PostgreSQL` — real Postgres for migrations, constraints, `jsonb`, sequences, advisory locks
- `Respawn` (or equivalent) — isolate tests without rebuilding the container every fact
- `Microsoft.EntityFrameworkCore.Design` is on Infrastructure only; no test-time migrator helper
- `QuickFIX/n` / `QuickFIXn.FIX4.4` — **not referenced anywhere in the solution**, including `Fix.CTrader`
- `Microsoft.AspNetCore.Mvc.Testing` — not needed until API integration exists
- no FIX acceptor / in-process simulator package (harness should be first-party test mode, not a live venue)

### What the csproj *implies* vs what it *does*

Referencing Infrastructure + InMemory EF suggests someone planned “integration” as in-memory EF. Architecture §60 item 1 is **PostgreSQL migrations**. InMemory:

- does not run EF/Npgsql migrations
- does not enforce Postgres unique indexes, exclusion constraints, or `CITEXT`
- does not test `jsonb` outbox payloads
- does not test restart durability
- cannot stand in for `outbox_events` transactional commit with the same connection as raw MT5 rows (§12 / §13)

Keep InMemory out of this project. If an in-memory seam is needed, it belongs in `tests/Unit`.

---

## Product surface the suite would have to hit (snapshot)

Integration tests cannot invent behavior. At audit time:

| Assembly | Source that exists | SUT usable by Integration? |
|---|---|---|
| `TraderIntelligence.Domain` | Enums (`CopyIntentAction`, `DealAction`, `DealEntry`, `FixSessionQualifier`, `OutboxEventType`, `RiskDecisionOutcome`, `TradeDirection`, `TraderState`) + entity records (`Broker`, `CanonicalInstrument`, `Mt5Account`, `Mt5Deal`, `Mt5Group`, `Mt5Position`, `ReconstructedTrade`, `SourceSymbolMapping`, `TraderScore`) | Data shapes only. No repositories, no outbox, no FIX types, no checkpoints. |
| `TraderIntelligence.Application` | `Class1` stub + FluentValidation package | No |
| `TraderIntelligence.Infrastructure` | **no `.cs` files**; packages: EF Core Design 8.0.4, Npgsql.EFCore 8.0.4, StackExchange.Redis 2.8.0 | Empty. No `DbContext`, no `Migrations/`, no outbox processor, no Redis usage. |
| `TraderIntelligence.Fix.CTrader` | **no `.cs` files**; no QuickFIX package | Empty. No session, dictionary, test mode, or message codec. |
| `TraderIntelligence.Mt5` | `Class1` stub; **not referenced** by Integration | No |
| `TraderIntelligence.FixWorker` | template `BackgroundService` logging every 1s | No FIX loop |
| `TraderIntelligence.Mt5Worker` | same template | No backfill |
| `TraderIntelligence.Api` | weather-forecast template | Irrelevant |

Suggested §66 folders that do **not** exist: `tests/Replay`, `tests/Fix`, `tests/Risk`. Solution nests only Unit + Integration.

`mt5-sdk/tests/*.cpp` are a **different tree** (C++ SDK unit/probe binaries; `MT5SDK_BUILD_TESTS` defaults OFF). Do **not** count them toward this C# Integration audit. `mt5_ledger_store_test.cpp` validates SHA-256 / record shape in-process; it does not run PostgreSQL migrations for the .NET platform.

---

## Required list (Architecture §60, Integration tests)

Quoted from the architecture `Required:` block (lines 2263–2272):

1. PostgreSQL migrations  
2. MT5 backfill/restart  
3. outbox processing  
4. QuickFIX/n session configuration  
5. FIX message parse/build  
6. ExecutionReport handling  
7. position reconciliation  
8. unknown-execution recovery  

Architecture §60 also lists a **Replay** pipeline (historical MT5 events → replay → reconstruction → features → scores → shadow copy). That is **out of this mapping**. It belongs in a future `tests/Replay` project, not as a substitute for the 8 items above.

---

## Mapping: §60 required test → future test class

Namespace root: `TraderIntelligence.Tests.Integration`.  
Folder = last namespace segment.  
File = `{ClassName}.cs`.  
xUnit class suffix: `Tests`.  
Apply `[Trait("Category", "Integration")]` on every class. Postgres-backed classes share one `ICollectionFixture<PostgresFixture>` (Testcontainers). FIX classes share `ICollectionFixture<FixHarnessFixture>` and **must not** resolve `live-us-eqx-01.p.c-trader.com`.

| # | §60 required test | Future test class | Future path under `tests/Integration/` | Future SUT (architecture) | Arch refs | Current coverage |
|---|-------------------|-------------------|----------------------------------------|---------------------------|-----------|------------------|
| 1 | PostgreSQL migrations | `PostgresMigrationTests` | `Persistence/PostgresMigrationTests.cs` | `TraderIntelligenceDbContext` + versioned EF migrations applying §45 / §44 tables | §45, §67.1, §72.3, §60 | **Missing** |
| 2 | MT5 backfill/restart | `Mt5BackfillRestartTests` | `Ingestion/Mt5BackfillRestartTests.cs` | `HistoricalBackfill` + `SyncCheckpoint` (read checkpoint → fetch → normalize → idempotent upsert → persist checkpoint); crash mid-batch must resume, not duplicate | §12, §67.1, §60 | **Missing** |
| 3 | outbox processing | `OutboxProcessingTests` | `Outbox/OutboxProcessingTests.cs` | transactional outbox writer (same commit as raw MT5 persist) + `OutboxProcessor` for `OutboxEventType` (`TradeCompleted`, `ScoreUpdate`, `ShadowCopyIntent`, `RiskCheckRequest`, `NotificationEvent`) | §12, §13, §45 `outbox_events`, §60 | **Missing** |
| 4 | QuickFIX/n session configuration | `QuickFixSessionConfigurationTests` | `Fix/QuickFixSessionConfigurationTests.cs` | QuickFIX/n settings + **cTrader** data dictionary (not generic FIX 4.4); independent QUOTE vs TRADE session objects (`FixSessionQualifier`) | §5 FIX Engine, §26–§28, §60 | **Missing** |
| 5 | FIX message parse/build | `FixMessageParseBuildTests` | `Fix/FixMessageParseBuildTests.cs` | codec for Logon / Logout / Heartbeat / TestRequest / SecurityList / MarketData / NewOrderSingle / ExecutionReport / OrderStatus / RequestForPositions / PositionReport / Cancel / BusinessMessageReject | §29, §30, §60 | **Missing** |
| 6 | ExecutionReport handling | `ExecutionReportHandlingTests` | `Fix/ExecutionReportHandlingTests.cs` | persist `fix_execution_reports`; drive order state (`not sent` / `sent unknown` / `accepted` / `partially filled` / `filled` / `rejected` / `cancelled`) | §32–§33, §44, §60, §70.2 | **Missing** |
| 7 | position reconciliation | `PositionReconciliationTests` | `Reconciliation/PositionReconciliationTests.cs` | startup + periodic reconcile: `OrderMassStatusRequest` + `RequestForPositions` vs internal `destination_positions`; block `READY_FOR_EXECUTION` until consistent; alert unknown / missing / qty / side / orphan ER | §35, §42–§43, §60, §68, §70.3 | **Missing** |
| 8 | unknown-execution recovery | `UnknownExecutionRecoveryTests` | `Reconciliation/UnknownExecutionRecoveryTests.cs` | after send+disconnect: `EXECUTION_STATE_UNKNOWN`; **no** blind `NewOrderSingle` retry; recover via OrderStatus / MassStatus / ERs / positions; only then decide whether another order is required | §33–§34, §60, §68, §70.6 | **Missing** |

**Mapped classes: 8. Implemented classes: 0.**

Shared fixture (does not exist):

| Type | Path | Job |
|---|---|---|
| `PostgresFixture` | `tests/Integration/Fixtures/PostgresFixture.cs` | start Postgres 16 via Testcontainers; apply migrations once per collection; Respawn between tests |
| `FakeMt5HistorySource` | `tests/Integration/Fakes/FakeMt5HistorySource.cs` | deterministic deal pages for backfill/restart; **no** live Manager API |
| `OutboxHarness` | `tests/Integration/Fixtures/OutboxHarness.cs` | run processor against the same DbContext as the writer |

---

## Required list (Architecture §61, FIX simulation / test harness)

§61 is a **product capability** (“FIX adapter test mode”) plus tests that drive it. It is a hard gate: **“Before using real NewOrderSingle”** and **“Do not use the real account as the first integration test.”**

Quoted capabilities (lines 2303–2312):

1. parse recorded ExecutionReports  
2. replay MarketDataIncrementalRefresh  
3. simulate disconnects  
4. simulate duplicate ExecutionReports  
5. simulate partial fill  
6. simulate rejection  
7. simulate unknown-state disconnect  

Plus the implied adapter:

- `IFixAdapter` test mode (in-process acceptor / scripted session)  
- never open TLS to Pepperstone/cServer from CI or `dotnet test`  
- `REAL_COPY_EXECUTION_ENABLED` remains false (§41)

§66 suggests a sibling `tests/Fix` project. **Recommendation:** implement the harness **inside** `tests/Integration/FixHarness/` first (this project already references `Fix.CTrader` + Infrastructure). Extract `tests/Fix` only if the suite becomes slow or starts pulling Postgres into pure codec tests. Do not create an empty `tests/Fix` project as decoration.

Parse/build of **recorded** messages can be unit-speed; it still belongs in this Integration map because §61 defines it as the **first integration test** of the adapter, replacing the live account.

---

## Mapping: §61 capability → future harness + tests

Future product types (none exist; names are the contract):

| Type | Home | Job |
|---|---|---|
| `FixAdapterMode` (`Live` / `Test`) | `src/Fix.CTrader` | Test mode refuses real host/ports; Live mode requires explicit flags |
| `IFixSession` (QUOTE + TRADE) | `src/Fix.CTrader` | independent sequence / reconnect state |
| `RecordedFixMessageStore` | test assembly | load SOH-separated fixtures |
| `ScriptedFixAcceptor` | test assembly | play scripted inbound messages; capture outbound |
| `FixDisconnectInjector` | test assembly | drop the session after a named outbound (`NewOrderSingle`) |
| `DuplicateReportInjector` | test assembly | deliver the same ExecutionReport twice (cTrader FAQ / §70.5) |

| # | §61 capability | Future test class | Future path | Fixture | Current coverage |
|---|---|---|---|---|---|
| 1 | parse recorded ExecutionReports | `RecordedExecutionReportParseTests` | `FixHarness/RecordedExecutionReportParseTests.cs` | `Fixtures/Fix/execution_reports/*.fix` | **Missing** |
| 2 | replay MarketDataIncrementalRefresh | `MarketDataIncrementalRefreshReplayTests` | `FixHarness/MarketDataIncrementalRefreshReplayTests.cs` | `Fixtures/Fix/md_incremental/*.fix` | **Missing** |
| 3 | simulate disconnects | `FixDisconnectSimulationTests` | `FixHarness/FixDisconnectSimulationTests.cs` | script: Logon → drop | **Missing** |
| 4 | simulate duplicate ExecutionReports | `DuplicateExecutionReportSimulationTests` | `FixHarness/DuplicateExecutionReportSimulationTests.cs` | same `ExecID` twice | **Missing** |
| 5 | simulate partial fill | `PartialFillSimulationTests` | `FixHarness/PartialFillSimulationTests.cs` | ER `OrdStatus=1` then `2` | **Missing** |
| 6 | simulate rejection | `RejectionSimulationTests` | `FixHarness/RejectionSimulationTests.cs` | ER reject + `OrdRejReason` | **Missing** |
| 7 | simulate unknown-state disconnect | `UnknownStateDisconnectSimulationTests` | `FixHarness/UnknownStateDisconnectSimulationTests.cs` | NOS sent → drop before ER → `EXECUTION_STATE_UNKNOWN` | **Missing** |

**Hard-rule tests (not in the 7-bullet list, still mandatory):**

| Class | Path | Assertion |
|---|---|---|
| `FixAdapterTestModeSafetyTests` | `FixHarness/FixAdapterTestModeSafetyTests.cs` | Test mode does not open a socket to `CTRADER_FIX_HOST`; `NewOrderSingle` is not sent to cServer; missing test-mode config fails closed |
| `FixAdapterDoesNotUseLiveAccountAsFirstTest` | `FixHarness/FixAdapterDoesNotUseLiveAccountAsFirstTest.cs` | default `dotnet test` path never reads live password / account `1369850` from env or the architecture doc |

**Mapped harness test classes: 7 + 2 safety. Implemented: 0.**  
**Recorded fixtures on disk: 0.**

Do **not** commit the architecture’s live host, sender IDs, or account password into test `cfg` files. Use synthetic `SenderCompID=TEST.QUOTE` / `TEST.TRADE`, `TargetCompID=TEST.CSERVER`, loopback acceptor.

---

## Placeholder that must not count

```csharp
namespace TraderIntelligence.Tests.Integration;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }
}
```

This is the Visual Studio template. xUnit treats an empty fact as **Passed**. It is not a §60 or §61 test.

Delete `UnitTest1.cs` when the first real class lands. Do not rename it to “cover” migrations or FIX. Do not add `Assert.True(true)`.

---

## Suggested method names (not implemented)

Minimum `[Fact]` / `[Theory]` names so implementers do not collapse 8+7 areas into one file.

### §60 Integration

| Class | First methods to add |
|---|---|
| `PostgresMigrationTests` | `Empty_database_applies_all_migrations`; `Apply_is_idempotent_on_second_run`; `Required_tables_exist` (`brokers`, `mt5_deals`, `outbox_events`, `sync_checkpoints`, `fix_sessions`, `fix_orders`, `fix_execution_reports`, `destination_positions`, `copy_intents`, `execution_intents`); `Down_migration_is_not_required_but_up_is_versioned` |
| `Mt5BackfillRestartTests` | `Backfill_upserts_deals_by_broker_login_ticket`; `Killed_mid_page_resumes_from_checkpoint_without_duplicates`; `Second_full_backfill_does_not_increase_row_count`; `Live_event_after_backfill_is_deduped_against_history` |
| `OutboxProcessingTests` | `Raw_deal_and_outbox_row_commit_atomically`; `Processor_marks_published_and_is_idempotent`; `Crash_after_commit_before_mark_retries_once`; `Failed_handler_does_not_lose_the_event` |
| `QuickFixSessionConfigurationTests` | `Quote_and_trade_are_distinct_sessions`; `Generic_FIX44_dictionary_is_rejected_in_favor_of_cTrader_ROE`; `Session_qualifier_and_header_fields_are_explicit`; `Two_active_TRADE_sessions_for_same_account_are_refused` |
| `FixMessageParseBuildTests` | `Parses_recorded_Logon_and_Heartbeat`; `Builds_SecurityListRequest`; `Builds_MarketDataRequest_for_discovered_XAU_id`; `Roundtrips_NewOrderSingle_without_sending`; `Rejects_hardcoded_tag55_XAUUSD_assumption` |
| `ExecutionReportHandlingTests` | `New_then_partial_then_fill_persists_one_order`; `Reject_sets_rejected_and_does_not_open_position`; `Unknown_ExecID_is_stored_and_alerted`; `Duplicate_ExecID_does_not_double_fill` |
| `PositionReconciliationTests` | `Startup_blocks_READY_FOR_EXECUTION_until_mass_status_and_positions_match`; `Unknown_external_position_raises_issue`; `Missing_internal_position_raises_issue`; `Quantity_or_side_mismatch_blocks_new_orders`; `Reconciled_state_unblocks` |
| `UnknownExecutionRecoveryTests` | `Disconnect_after_NOS_sets_EXECUTION_STATE_UNKNOWN`; `Does_not_resend_NOS_on_reconnect`; `OrderStatus_found_transitions_to_accepted_or_filled`; `Neither_order_nor_position_found_allows_new_intent_only_after_reconcile` |

### §61 Harness

| Class | First methods to add |
|---|---|
| `RecordedExecutionReportParseTests` | `Parses_fill_from_fixture`; `Parses_reject_from_fixture`; `Unknown_custom_tag_does_not_throw` |
| `MarketDataIncrementalRefreshReplayTests` | `Replay_updates_bid_ask_and_received_timestamp`; `Stale_replayed_quote_is_rejected_by_risk_policy` |
| `FixDisconnectSimulationTests` | `Drop_after_logon_marks_session_unhealthy`; `Reconnect_in_test_mode_does_not_hit_live_host` |
| `DuplicateExecutionReportSimulationTests` | `Second_identical_ER_is_ignored_for_qty`; `Duplicate_is_audited` |
| `PartialFillSimulationTests` | `Leaves_order_partially_filled_and_position_qty_matches_cumQty` |
| `RejectionSimulationTests` | `Business_reject_and_order_reject_are_distinct_states` |
| `UnknownStateDisconnectSimulationTests` | `Injector_drops_after_outbound_NOS_before_ER`; `State_is_UNKNOWN_not_rejected` |
| `FixAdapterTestModeSafetyTests` | `Test_mode_cannot_be_silently_upgraded_to_Live`; `Live_host_config_in_test_run_fails_fast` |

---

## Wiring defects (concrete)

1. **False green.** One empty fact. Any CI that runs `dotnet test Mt5TraderIntelligence.sln` will report success.
2. **InMemory EF package in an Integration project whose first required test is PostgreSQL migrations.** Wrong seam. Remove when real fixtures land.
3. **No Testcontainers, no docker-compose, no connection-string convention.** There is nothing to migrate.
4. **No `DbContext` and no `Migrations/` folder** in Infrastructure (project is source-empty). Item 1 is blocked on product work, not on more test templates.
5. **No QuickFIX/n anywhere.** Items 4–6 and all of §61 are blocked on pinning a stable QuickFIX/n + cTrader dictionary (architecture §5 / §74).
6. **`Fix.CTrader` has zero source files.** Test mode cannot be written against `Class1` because even that stub is gone.
7. **Integration does not reference `TraderIntelligence.Mt5`.** Item 2 cannot target the MT5 assembly.
8. **No `[Trait]`, no collection fixtures, no skip for `LIVE_FIX=1`.** When someone later pastes architecture env vars into `appsettings`, tests have no safety rail.
9. **Solution is missing `tests/Replay`, `tests/Fix`, `tests/Risk`.** Acceptable for Phase 0 if Integration is real. It is not.
10. **No `.gitignore` at repo root.** `bin/` / `obj/` from this audit run (and secrets later) have no ignore policy in-tree.
11. **Architecture doc embeds live FIX identifiers** (`CTRADER_FIX_HOST=live-us-eqx-01.p.c-trader.com`, account `1369850`). Tests must never load those as defaults.

---

## Safety (binding)

Architecture §61: **Do not use the real account as the first integration test.**  
Architecture §41: `REAL_COPY_EXECUTION_ENABLED=false` by default.  
Architecture §8 / §70: no live `NewOrderSingle` until harness + reconciliation + unknown-state recovery are proven.

Until `FixAdapterMode.Test` exists and the safety facts above pass, **no engineer should point this project at Pepperstone/cServer**, including “just Logon”. Quote-session “health checks” against the live host are Phase 4 **manual/operator** work, not `tests/Integration`.

---

## What this audit is not

| Item | Why excluded |
|---|---|
| §60 unit (17 areas) | `A09_unit_tests_audit.md` |
| §60 Replay pipeline | future `tests/Replay`; do not fake it inside Integration |
| `mt5-sdk/tests/*.cpp` | C++ SDK; different process; not this csproj |
| Risk-engine unit tests | §60 unit item 14; Integration only if a later risk+Postgres test is added |
| Dashboard / API HTTP tests | not in §60 Integration list |
| Live broker connectivity | forbidden as first Integration test (§61) |

---

## Recommended implementation order (do not start tests before SUTs)

Align with §67. Do not write 15 empty test classes as a substitute for product.

| Order | Phase | Build first (product) | Then add tests |
|---|---|---|---|
| 1 | 1 | `DbContext` + versioned migrations for §45 core tables | `PostgresMigrationTests` |
| 2 | 1 | raw deal persist + `sync_checkpoints` + fake MT5 source | `Mt5BackfillRestartTests` |
| 3 | 1 | transactional `outbox_events` + processor | `OutboxProcessingTests` |
| 4 | 4 | QuickFIX/n pin + cTrader dictionary + QUOTE/TRADE settings **in Test mode** | `QuickFixSessionConfigurationTests`, `FixMessageParseBuildTests`, `FixAdapterTestModeSafetyTests` |
| 5 | 4 / 61 | recorded MD incremental fixtures + quote cache | `MarketDataIncrementalRefreshReplayTests` |
| 6 | 7 | TRADE read path: ER parser, mass status, positions — **NOS still disabled** | `ExecutionReportHandlingTests`, `PositionReconciliationTests`, recorded ER parse, duplicate / partial / reject simulators |
| 7 | 8 / 61 | unknown-state + disconnect injector **before** any live NOS | `UnknownExecutionRecoveryTests`, `UnknownStateDisconnectSimulationTests`, `FixDisconnectSimulationTests` |

A test class with no SUT is another `UnitTest1`. Prefer `[Fact(Skip = "SUT not implemented")]` only if CI must list the gap; better: do not add the class until the type exists.

---

## Gaps / blockers (honest)

1. **0/8 §60 integration areas implemented.**
2. **0/7 §61 harness capabilities implemented.** Zero recorded FIX fixtures.
3. **False green:** `Passed: 1` in `< 1 ms`.
4. **No SUTs** for persistence, outbox, MT5 backfill, or FIX. Domain entity records are not a substitute.
5. **Infrastructure and Fix.CTrader are source-empty** (csproj + packages / refs only).
6. **InMemory EF** is the wrong integration seam for Postgres.
7. **No Mt5 project reference** for backfill tests.
8. **No CI, no docker-compose, no Testcontainers, no QuickFIX/n.**
9. **Live-account safety is not encoded in tests.** The architecture file itself is currently a more complete FIX spec than the test project.
10. **§68 / §70 gates that this suite must eventually prove are all unchecked:** reconciliation after restart, unknown-state recovery, duplicate ER handling, partial fills, rejects, risk-engine integration.

---

## Disposition

| Metric | Value |
|---|---|
| §60 integration areas required | 8 |
| Future Integration test classes named | 8 |
| §61 harness capabilities required | 7 |
| Future harness test classes named | 7 + 2 safety |
| Classes present in `tests/Integration` | 0 real (only `UnitTest1`) |
| Assertions | 0 |
| Recorded FIX fixtures | 0 |
| Measured `dotnet test` | 1 passed, 0 failed, 0 skipped, `< 1 ms` |
| Coverage of §60 Integration | **0/8** |
| Coverage of §61 harness | **0/7** |
| Product source changed by A10 | No |

Implement the classes above when the corresponding SUTs exist. Until then this audit is the authoritative name map for Integration + FIX harness. Do not treat the current green test run as a go-live or Phase-1 exit signal.
