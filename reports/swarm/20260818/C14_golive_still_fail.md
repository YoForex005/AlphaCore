# C14 — Architecture §68 go-live gates: all still FAIL for live

| Field | Value |
|---|---|
| Agent | C14 (senior engineer, go-live re-measure only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (post B-wave / mid C-wave; after Domain unit tests and 15 React pages landed) |
| Artifact | `D:\Prop\reports\swarm\20260818\C14_golive_still_fail.md` |
| Assigned | Architecture §68 all should still be FAIL for **live**. Write this report. Do not modify product source. |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§68 Go-Live Gates** (lines 2605–2628) |
| Supporting | §67 Phase 8, §69 first useful version, §70 live FIX acceptance (lines 2658–2676), §72 rules 4–18 |
| Predecessor checklist | `A100_golive_gates.md` (wave-1: **0 PASS / 19 FAIL**) |
| Sibling bars | `A57` §69, `A101` §70, `A28` phase map, `B04`/`B05`/`B07`/`B08`/`B13`/`B18`, `C08` web census |
| Product source modified | **No.** This report is the only write. |

**Law (verbatim §68):** Do not enable real copying until **all** of these are true.

**Scoreboard (re-measured 2026-08-18):** **0 PASS / 19 FAIL for live.** Same integer as A100. Later lab tests and UI files **do not** change the live bar.

Live `NewOrderSingle` stays **OFF**. One FAIL blocks enablement. This file does not implement anything and does not flip any checkbox.

---

## 0. Verdict

**All 19 Architecture §68 gates remain FAIL for live.**

A100 measured **0 / 19** before most Domain tests and the React page files existed. That integer is still correct **as a live-copy license**. What changed is lab surface, not venue proof.

| Bar | A100 (wave 1) | C14 (this file) |
|---|---|---|
| §68 live-copy license | **0 / 19 PASS** | **0 / 19 PASS** |
| Checkboxes that may be `[x]` for live | none | **none** |
| `REAL_COPY_EXECUTION_ENABLED` | default false; unread as a send gate | still false; still no send function |
| Live `35=D` possible if process starts | No (absence) | No (absence) |
| Safe to enable real orders? | **No** | **No** |

**Do not greenwash these later facts into PASS:**

1. `dotnet test` Unit **61 passed / 22 skipped / 0 failed** and Integration **3 passed**. Those are in-memory Domain + FakeMt5 + `Assert.True(true)` slices. They are not live Achiever / StarwaveFX / Pepperstone proof.
2. `apps/web/src/pages/` now has **15** files (C08). Router import match is not venue health.
3. `TradeReconstructor`, `RiskEngine`, `QuantityNormalizer`, `SymbolNormalizer`, `ShadowCopyEngine`, unique EF indexes, and `CopyIntent.IdempotencyKey` **exist**. Unused or demo-only methods cannot become a live gate.
4. Fake connectors, in-memory DB, stamped `LoggedOn`, hardcoded `Connected = true`, and `/api/health` `{ healthy: true, details: "demo connector" }` are **anti-evidence**.

Vacuous / demo law (binding, copied from A100): Fake connector, in-memory DB, unused method, skipped test, or seeded rows **cannot** become PASS. A green `dotnet test` is not coverage of live.

```text
REAL_COPY_EXECUTION_ENABLED = false     -- stays false in every committed config that sets it
CTrader:RealCopyExecutionEnabled        -- worker default false; fix-worker appsettings has no CTrader block
Live 35=D to *.c-trader.com             -- FORBIDDEN until 19/19 §68 + 14/14 §70 + explicit prod review
```

---

## 1. Method

Read, hashed, grepped, and tested. Did not edit product source.

| Source | Path / action |
|---|---|
| Architecture §68 / §70 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2605–2676 |
| A100 / A101 / A28 / A57 | original scorecards |
| B04 / B05 / B07 / B08 / B13 / B18 | later layer reviews |
| C08 | 15-page React census |
| Product | `src/`, `apps/`, `tests/` (no `tests/Fix`, no `tests/Replay`) |
| Measured tests | `dotnet test` Unit + Integration, `--nologo --verbosity minimal` |
| SHA-256 | PowerShell `Get-FileHash` on the files in §2 |

Commands:

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity minimal
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj --nologo --verbosity minimal
```

| Project | Total | Passed | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|---:|
| `TraderIntelligence.Tests.Unit` | **83** | **61** | **0** | **22** | **0** |
| `TraderIntelligence.Tests.Integration` | **3** | **3** | **0** | **0** | **0** |

The Integration green run includes `PlaceholderRemoved.Integration_project_loads` → `Assert.True(true)` (`tests/Integration/UnitTest1.cs`). That is **not** a §68 test.

B08’s earlier “29 tests / 1 fail” snapshot is **stale on counts**. The live verdict is not.

---

## 2. Evidence files (SHA-256 at measure)

| SHA-256 | Path |
|---|---|
| `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | `D:\Prop\apps\fix-worker\Worker.cs` |
| `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | `D:\Prop\apps\mt5-worker\Worker.cs` |
| `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` |
| `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | `D:\Prop\apps\api\Program.cs` |
| `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` |

---

## 3. Working checklist (copy of §68) — live status

```text
[ ] FAIL  G01  MT5 historical/live ingestion is stable
[ ] FAIL  G02  duplicate event handling is proven
[ ] FAIL  G03  trade reconstruction tests pass
[ ] FAIL  G04  XAU symbol mappings are verified
[ ] FAIL  G05  quote session stable
[ ] FAIL  G06  trade session stable
[ ] FAIL  G07  cTrader reconciliation works after restart
[ ] FAIL  G08  copy intents are idempotent
[ ] FAIL  G09  unknown execution state recovery works
[ ] FAIL  G10  position sizing conversion is verified
[ ] FAIL  G11  risk engine unit/integration tests pass
[ ] FAIL  G12  stale quote rejection works
[ ] FAIL  G13  stale signal rejection works
[ ] FAIL  G14  shadow copy has sufficient sample
[ ] FAIL  G15  destination costs / slippage measured
[ ] FAIL  G16  kill switch tested
[ ] FAIL  G17  secrets removed from repo / logs
[ ] FAIL  G18  dashboard exposes venue health / risk
[ ] FAIL  G19  manual review completed
```

**Enable live copy only when:** `19/19 PASS` **and** `14/14` §70 **and** explicit production flag reviewed. Default remains OFF.

---

## 4. What changed since A100 (and why none of it is live PASS)

| Surface | A100 said | Now | Live? |
|---|---|---|---|
| Unit `[Fact]` / `[Theory]` | **zero** sources | 83 tests; 61 pass; 22 skip (almost all A43 conversion) | **No** — in-process Domain / FakeMt5 |
| Integration | empty / placeholder | `SeedingAndStoreTests` + `Assert.True(true)` | **No** — EF InMemory + canned deals |
| Reconstruction | no test class | `TradeReconstructionTests` 5 facts | **No** — synthetic `NormalizedDeal`, not venue history |
| Risk | zero tests; `Evaluate` unused | 5 facts; still **not** registered in DI; still no send hook | **No** |
| Sizing | no conversion tests | `QuantityNormalizer` step tests + **21 skipped** `Never_passthrough_MT5_lots` family | **No** |
| React pages | A62 empty `pages/` | 15 files imported (C08) | **No** — pages consume demo/stamped APIs |
| FIX package | `QuickFix.Net 1.8.0` (A05/A101/B05) | `TraderIntelligence.Fix.CTrader.csproj` has **no** QuickFIX NuGet at all | **Worse for sessions** — still no initiator |
| MT5 transport | two fakes | still `DemoBrokerFactory.CreateDefault()` only | **No** |
| Migrations | none | still `EnsureCreated` in api + both workers | **No** |
| `tests/Fix` | absent | still absent | **No** |

A100’s “zero tests” sentence is **stale**. A100’s **0 / 19 live** integer is **not**.

---

## 5. Per-gate live re-measure

### G01 — MT5 historical/live ingestion is stable — **FAIL (live)**

| | |
|---|---|
| §68 text | `MT5 historical/live ingestion is stable` |
| Status | **FAIL** |
| Earliest phase | 1 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `AddTraderIntelligence` always registers two fakes (`DemoBrokerFactory.CreateDefault()` in `src/Infrastructure/DependencyInjection.cs`). There is **no** other `IMt5BrokerConnector` implementer (B04).
- `FakeMt5BrokerConnector.ConnectAsync` sets a bool. Four canned logins (`10001`, `10002`, `10003`, `99001`). About **18** canned XAU round-trip deals.
- `apps/mt5-worker/Worker.cs` syncs those fakes every 30 s, then rebuilds only the four demo logins. Comment: “Execution copy is not performed here.”
- Missing / empty `ConnectionStrings:TraderIntelligence` → **in-memory** EF.
- Hosts call `EnsureCreated`. **Zero** EF `Migrations/` folders.
- C++ `mt5-sdk` is not the C# collector. No HTTP `/mt5/*` client. No Manager P/Invoke from C#.
- Dashboard `GetBrokersAsync` hard-codes `Connected = true` and `LastEventAt = UtcNow`. It never calls `IsConnectedAsync`.

**PASS when:** Achiever **and** StarwaveFX stay connected across reconnect; all groups/accounts discovered; history backfill restart-safe; live deals persist before async work; measured ~5k path; not the fake factory.

---

### G02 — Duplicate event handling is proven — **FAIL (live)**

| | |
|---|---|
| §68 text | `duplicate event handling is proven` |
| Status | **FAIL** |
| Earliest phase | 1 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- Unique index `(BrokerId, DealTicket)` on `mt5_deals` (`TraderDbContext`).
- `EfTradingStore.UpsertDealAsync` returns `false` if the ticket exists (select-then-insert).
- `SeedingAndStoreTests.Deal_upsert_is_idempotent` proves the **application check** on EF InMemory. InMemory does **not** enforce unique indexes. No concurrent insert. No PostgreSQL. No FIX ER replay. No restart-vs-broker-history compare.

A unique fluent index plus an in-memory upsert is not “proven” on live feeds.

**PASS when:** Replaying the same deal/event stream (and FIX ER later) does not create duplicate rows; tested in CI against PostgreSQL; source ledger matches broker history after restart.

---

### G03 — Trade reconstruction tests pass — **FAIL (live)**

| | |
|---|---|
| §68 text | `trade reconstruction tests pass` |
| Status | **FAIL** |
| Earliest phase | 2 |
| Checkbox | `[ ]` |

**Why this is the easiest false-green:** five `TradeReconstructionTests` facts now pass (`Reconstructs_simple_round_trip`, scale-in, reverse `InOut`, first-three XAU, ignore balance). Demo seed reconstructs canned XAU and scores login `10001` at 3 completed trades.

**Why it is still FAIL for live:**

- Inputs are hand-built `NormalizedDeal` records (`SourceSymbol = "XAUUSDm"`), not Manager history.
- Integration seed uses **FakeMt5** deals, not Achiever/Starwave tickets.
- A21 / A27 reconstruction fixture set is **not** on disk (completed XAU, first-3, reversals, volume-unit matrix against recorded venue files).
- Worker only rebuilds four demo logins. No live deal subscription.
- Demo seeder success is not a test of production reconstruction.

**PASS when:** A27 reconstruction fixtures pass in CI on recorded/live-shaped deals (completed XAU, first-3, reversals, volume units). Demo seeder success is not a test.

---

### G04 — XAU symbol mappings are verified — **FAIL (live)**

| | |
|---|---|
| §68 text | `XAU symbol mappings are verified` |
| Status | **FAIL** |
| Earliest phase | 2 / 4 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `SymbolNormalizer` ships hardcoded aliases (`XAUUSD`, `GOLD`, …). `SymbolNormalizerTests` maps five strings and asserts `TryMapVenueInstrumentId("123456")` is false until registered.
- Fake deals use the literal `"XAUUSD"`. Seeder `DestinationQuotes.VenueInstrumentId = null`.
- No persisted mapping verified against live Achiever/Starwave symbols.
- No cTrader Security List. `FixSimulationHarness` still builds strings; tag 55 is not a discovered numeric instrument id (§72.13).

**PASS when:** Source aliases confirmed on both MT5 brokers; Pepperstone instrument ID **discovered** and stored; tests reject guessed `55=XAUUSD`.

---

### G05 — Quote session stable — **FAIL (live)**

| | |
|---|---|
| §68 text | `quote session stable` |
| Status | **FAIL** |
| Earliest phase | 4 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `apps/fix-worker/Worker.cs` every 15 s sets QUOTE `LastInboundAt = UtcNow` and `Status = ReadyForMarketData`. **No socket.**
- `TraderIntelligence.Fix.CTrader.csproj` now has **zero** QuickFIX packages (not even the wrong `QuickFix.Net 1.8.0` pin B05 recorded). No initiator, SSL, dictionary, or MD handler.
- Parser + `FixSimulationHarness` only. `CTraderQuoteSession` **absent** (B05).
- `CTraderFixOptions.Host` defaults to live `live-us-eqx-01.p.c-trader.com`. Options are **not** bound by the worker (`apps/fix-worker/appsettings.json` is logging only).
- `DemoSeeder` plants QUOTE `ReadyForMarketData` against that live host.

**PASS when:** Independent SSL QUOTE session stays logged on across reconnect; heartbeats real; dashboard age from last **venue** quote, not a worker stamp.

---

### G06 — Trade session stable — **FAIL (live)**

| | |
|---|---|
| §68 text | `trade session stable` |
| Status | **FAIL** |
| Earliest phase | 7 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- Same worker stamps TRADE `Status = LoggedOn` whether or not `CTrader:RealCopyExecutionEnabled` is true (`real ? LoggedOn : LoggedOn`).
- No TRADE SSL initiator. `CTraderTradeSession` **absent**.
- NewOrderSingle correctly **not sent**, but the session is not stable — it is **not connected**.

**PASS when:** Independent SSL TRADE logon is stable; seq files persist; disconnect/reconnect proven. Send remains flagged off until G01–G19 + §70.

---

### G07 — cTrader reconciliation works after restart — **FAIL (live)**

| | |
|---|---|
| §68 text | `cTrader reconciliation works after restart` |
| Status | **FAIL** |
| Earliest phase | 7 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- Product `*.cs` contains **zero** `OrderMassStatus`, `RequestForPositions`, `destination_positions`, `GuardedNewOrder`, `fix_execution_reports`.
- No `StartupReconciliationCoordinator` / `ReconciliationGate` (A47: still MISSING).
- `GET /api/reconciliation/status` returns **now + zeros** (`unknownPositions = 0`, `mismatches = 0`) with no store read.
- `ReconciliationPage` JSON-dumps that stub.

**PASS when:** After process restart, mass-status + positions reconcile to `destination_positions`; inconsistent book **blocks** `READY_FOR_EXECUTION`; integration test exists.

---

### G08 — Copy intents are idempotent — **FAIL (live)**

| | |
|---|---|
| §68 text | `copy intents are idempotent` |
| Status | **FAIL** |
| Earliest phase | 5 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `CopyIntent.IdempotencyKey` + unique index exist.
- **No writer** creates intents from source deals. Grep of product `*.cs` finds no `new CopyIntent` / `CopyIntents.Add`. `ShadowCopyEngine` is unused outside its file.
- No persist-before-send. No A42 retry/crash test.

**PASS when:** Same source event cannot insert a second intent or fire a second order; persist-before-send; unique key proven under retry/crash.

---

### G09 — Unknown execution state recovery works — **FAIL (live)**

| | |
|---|---|
| §68 text | `unknown execution state recovery works` |
| Status | **FAIL** |
| Earliest phase | 7 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `ExecutionOrderStateMachine.AfterDisconnectWithUnknownAck()` → `ExecutionStateUnknown`; `MayRetryNewOrderSingle` only for `NotSent`/`Rejected`.
- No recovery service, no OrderStatus / MassStatus path, no test that drives a disconnect-after-submit.
- `ExecutionAndSizingTests` can call the static helper. That is not recovery.

**PASS when:** After send+disconnect the intent is `EXECUTION_STATE_UNKNOWN`; **no** blind `NewOrderSingle` retry; recover via status/ER/positions; tested.

---

### G10 — Position sizing conversion is verified — **FAIL (live)**

| | |
|---|---|
| §68 text | `position sizing conversion is verified` |
| Status | **FAIL** |
| Earliest phase | 5 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `QuantityNormalizer` is `sourceLots * allocationFactor` stepped to dest min/max. **Passthrough-shaped** when allocation = 1 (`0.10` lots → `0.10` “qty” on a 1 oz spec).
- `SourceDestinationQuantityConversionTests.Never_passthrough_MT5_lots` and the A43 E01–E39 family are **`[Fact(Skip = …)]`**. Measured skip count: **21** conversion + **1** max-re-floor = **22**.
- The **passing** conversion facts lock the passthrough (`Should().Be(0.10m)` / `Should().NotBe(10.00m)`). That is a measured G7 defect, not a converter.
- No dest contract-size / unit spec from Security List. `QuantityNormalizer` is unused by `ShadowCopyEngine` and `RiskEngine` (test comment on disk).

**PASS when:** Known source-lot → dest-qty fixtures pass (not skip); `Never_passthrough_MT5_lots` fails any `requested_quantity = source_lots` shortcut; dest spec comes from the venue.

---

### G11 — Risk engine unit / integration tests pass — **FAIL (live)**

| | |
|---|---|
| §68 text | `risk engine unit/integration tests pass` |
| Status | **FAIL** |
| Earliest phase | 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `RiskEngine.Evaluate` exists. Five unit facts: stale quote, flag false, stop-new vs close, unreconciled, stale signal.
- `grep Evaluate(` under product `*.cs` hits **only the definition**. `AddTraderIntelligence` does **not** register `RiskEngine`. B13: dead path.
- No integration suite. No spy that `SubmitNewOrderSingle` count = 0. No A23 hard-limit matrix.
- `AllowFixSend` is a return field nobody consults on a send function, because **there is no send function**.

Five green facts on a method nobody calls are not “unit/integration tests pass” for live.

**PASS when:** A23/A27 unit + integration suite passes: each hard limit, quote/signal stale, reduce vs open, kill switch, recon block, **zero** FIX outbound on reject, hook proven on the real worker pipeline (InProcess venue).

---

### G12 — Stale quote rejection works — **FAIL (live)**

| | |
|---|---|
| §68 text | `stale quote rejection works` |
| Status | **FAIL** |
| Earliest phase | 4 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `RiskLimits.MaxQuoteAge = 3s` vs `CTraderFixOptions.MaxQuoteAgeMs = 5000` — two unbound defaults (A72 / B13).
- Unit test constructs a 30 s-old `DestinationQuote` and gets `QUOTE_STALE`. Nothing on Application / workers calls `Evaluate`.
- No destination quote feed. FIX worker health is a timestamp stamp, not quote age.
- Seeded dest quote has `VenueInstrumentId = null`.

**PASS when:** `quote_age > configured_max` rejects OPEN/INCREASE on **live and shadow**; config not compile constants; `QuoteFreshnessGuardTests` pass; logged-on ≠ fresh.

---

### G13 — Stale signal rejection works — **FAIL (live)**

| | |
|---|---|
| §68 text | `stale signal rejection works` |
| Status | **FAIL** |
| Earliest phase | 5 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `RiskEngine` emits `SIGNAL_STALE`; `CopyIntentExpiry.IsExpired` exists. Neither is on a send/shadow path. One unit fact feeds `SourceEventTime` 5 minutes old.
- No `CopyIntent` writer. Reduce/close vs open-more (§72.17–18) is not on a pipeline.

**PASS when:** Expired `CopyIntent` cannot open more; tested on the pipeline; reduce/close not treated as open-more.

---

### G14 — Shadow copy has sufficient sample — **FAIL (live)**

| | |
|---|---|
| §68 text | `shadow copy has sufficient sample` |
| Status | **FAIL** |
| Earliest phase | 5 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `ShadowCopyEngine` simulates fills in memory; **no product call sites** (B18).
- Overview “shadow P&L” is `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries`) — table is empty in the demo path unless something else writes it. Four demo logins are not a go-live sample (A24).
- Engine will still fill a crossed / stale book; no fail-closed quote guards (B18).

**PASS when:** Selected traders shadow-copied on **destination** quotes; sample size and window agreed and stored; source-vs-shadow analysis exists before any live copy.

---

### G15 — Destination costs / slippage measured — **FAIL (live)**

| | |
|---|---|
| §68 text | `destination costs / slippage measured` |
| Status | **FAIL** |
| Earliest phase | 5 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- Engine can compute `SourceVsShadowSlippage` if given a quote. No live/recorded Pepperstone tape. No cost model from dest fills. Seeded bid/ask is a demo snapshot, not a measured book.

**PASS when:** Slippage/spread/commission measured from destination quotes (and later dest fills); numbers drive `shadow_performance`, not source-broker P&L.

---

### G16 — Kill switch tested — **FAIL (live)**

| | |
|---|---|
| §68 text | `kill switch tested` |
| Status | **FAIL** |
| Earliest phase | 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `KillSwitch` entity + exclusive `KillSwitchMode`. Seeder inserts `Mode = None`.
- Unit test: `StopNewExecution` → `GlobalStop` on open, `Approve` on close (FIX still `AllowFixSend = false` because flag is false).
- No command API under `apps/` (no `KillSwitch` / `stop-new` in API or web). `Evaluate` unused. Exclusive enum still violates A48 two-control law (B13).
- Risk DTO daily P&L / DD / XAU exposure are **zeros**.

**PASS when:** `STOP_NEW_EXECUTION` proven (does not flatten); flatten is a distinct authorized path; audited; integration test; §70.13 also true.

---

### G17 — Secrets removed from repo / logs — **FAIL (live)**

| | |
|---|---|
| §68 text | `secrets removed from repo / logs` |
| Status | **FAIL** |
| Earliest phase | 0 (re-check always) |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- A19: **no live passwords** in `D:\Prop` (still good, still not sufficient).
- A76: **no** central `FixWireRedactor` / Serilog denylist. Call-site luck is not §57.
- `apps/api/appsettings.json` stores live targeting: host `live-us-eqx-01.p.c-trader.com`, `AccountId` `1369850`, empty `Password`.
- `DemoSeeder` persists Achiever `57.128.141.65` / manager `2027` and Starwave `84.201.6.142` / manager `9904`.
- Architecture markdown holds the same identifiers. Identifier policy is unsigned.

**PASS when:** Re-scan finds no committed secrets; FIX tags 553/554 and `Password=` redacted to `***`; dashboard never receives credentials (§72.4–5). Identifier policy signed.

---

### G18 — Dashboard exposes venue health / risk — **FAIL (live)**

| | |
|---|---|
| §68 text | `dashboard exposes venue health / risk` |
| Status | **FAIL** |
| Earliest phase | 3 / 4 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- C08: **15 / 15** `App.tsx` imports resolve. That is a router census, not health.
- `EfDashboardQueries.GetOverviewAsync`: `Mt5Healthy = brokers > 0`. `QuoteHealthy` / `TradeHealthy` read **stamped** session enums (`ReadyForMarketData` / `LoggedOn`).
- `GetBrokersAsync` hard-codes `Connected = true`.
- `GET /api/health` hard-codes Achiever `healthy = true` with `details = "demo connector"`, FIX QUOTE `healthy = true`, database `healthy = true`.
- `SystemHealthPage` / `ReconciliationPage` dump those JSON blobs. `FixSessionsPage` shows `connected` / `loggedOn` from the seeder/worker lie. `LiveCopyPage` is an amber stub (“stay empty until go-live gates pass”).
- Risk DTO numeric fields are zeros. `ExecutionEnabled` is a `false` literal on session DTO — accidental honesty, not a binder.

**PASS when:** React shows real MT5/QUOTE/TRADE health, quote age, risk rejects, kill-switch mode, shadow book — from venue facts, not seed/heartbeat. No secrets in the browser.

---

### G19 — Manual review completed — **FAIL (live)**

| | |
|---|---|
| §68 text | `manual review completed` |
| Status | **FAIL** |
| Earliest phase | 8 sign-off |
| Checkbox | `[ ]` |

No signed review exists. Swarm audits (A01–A105, B-band, C08/C09, this C14) are **not** Phase 8 sign-off. C14 is a **FAIL confirmation**, not a go-live review.

**PASS when:** Named reviewer records G01–G18 PASS + §70 14/14 + production flag decision (default OFF if any box unchecked).

---

## 6. Gate-to-phase scoreboard (live)

| ID | Gate | Earliest phase | Must be true at go-live | Lab surface now | Live status |
|----|------|----------------|-------------------------|-----------------|-------------|
| G01 | MT5 historical/live ingestion stable | 1 | yes | FakeMt5 + 30 s loop | **FAIL** |
| G02 | Duplicate event handling proven | 1 | yes | InMemory upsert fact | **FAIL** |
| G03 | Trade reconstruction tests pass | 2 | yes | 5 synthetic facts | **FAIL** |
| G04 | XAU symbol mappings verified | 2 / 4 | yes | Hardcoded aliases | **FAIL** |
| G05 | Quote session stable | 4 | yes | 15 s stamp | **FAIL** |
| G06 | Trade session stable | 7 | yes | 15 s `LoggedOn` stamp | **FAIL** |
| G07 | cTrader reconciliation after restart | 7 | yes | Stub `/api/reconciliation/status` | **FAIL** |
| G08 | Copy intents idempotent | 5 / 8 | yes | Unique index; no writer | **FAIL** |
| G09 | Unknown execution state recovery | 7 / 8 | yes | SM helper only | **FAIL** |
| G10 | Position sizing conversion verified | 5 / 8 | yes | Passthrough + 22 skips | **FAIL** |
| G11 | Risk engine unit/integration tests | 8 | yes | 5 unused-method facts | **FAIL** |
| G12 | Stale quote rejection | 4 / 8 | yes | Pure Evaluate fact | **FAIL** |
| G13 | Stale signal rejection | 5 / 8 | yes | Pure Evaluate fact | **FAIL** |
| G14 | Shadow copy sufficient sample | 5 | yes | Unused engine | **FAIL** |
| G15 | Destination costs/slippage measured | 5 | yes | No dest tape | **FAIL** |
| G16 | Kill switch tested | 8 | yes | Entity + unused branch | **FAIL** |
| G17 | Secrets removed from repo/logs | 0, re-check always | yes | No passwords; IDs committed; no redactor | **FAIL** |
| G18 | Dashboard venue health/risk | 3 / 4 | yes | 15 pages on fake health | **FAIL** |
| G19 | Manual review completed | 8 sign-off | yes | None | **FAIL** |

**Count:** 19 gates. **0 PASS for live.** Zero skips. A single unchecked item blocks real copy.

---

## 7. Related bars (not substitutes)

| Bar | Score | Note |
|-----|-------|------|
| §69 first useful version (A57) | **0 / 12** (details stale; integer still 0) | Required before judging ML; still not a live-copy license |
| §70 live FIX acceptance (A101) | **0 / 14** | Required **in addition** to this list before Phase 8 send |
| §71 do-not-build | Kafka / K8s / ClickHouse / LLM / mesh | Correctly absent; do not add to pass these gates |

`CTrader:RealCopyExecutionEnabled` default **false** and “worker refuses NewOrderSingle” are **controls by absence**, not G01–G19 PASS.

Conjunction still required (A25 / A101; do not satisfy on live):

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED          -- default false
TRADE == READY_FOR_EXECUTION
lease owned + fencing token current
risk AllowFixSend
STOP_NEW_EXECUTION == false
QUOTE usable if the order needs a price
intent + cl_ord_id persisted, status NotSent, not expired
venue.Kind == InProcess
  OR (explicit prod review + §68 19/19)
```

Today almost every conjunct is unimplemented. The process cannot send `35=D`. That is **not** 19/19.

---

## 8. Anti-greenwash (would let an operator think §68 is done)

| Location | Defect | Why it stays FAIL |
|---|---|---|
| `DemoSeeder` TRADE `LoggedOn` / QUOTE `ReadyForMarketData` | Fake venue | Health from enum |
| `fix-worker` `LastInboundAt` + `LoggedOn` every 15 s | Fake heartbeat | No socket |
| `fix-worker` `real ? LoggedOn : LoggedOn` | Flag does not change status | Vacuous |
| `EfDashboardQueries` `Mt5Healthy` / `QuoteHealthy` / `TradeHealthy` | Believes seeder/worker | Not venue facts |
| `GetBrokersAsync` `Connected = true` | Hard-coded | Never `IsConnectedAsync` |
| `GET /api/health` demo `healthy: true` | Hard-coded | Operator lie |
| `GET /api/reconciliation/status` zeros | Hard-coded | No recon run |
| `TradeReconstructionTests` 5 greens | Synthetic deals | Not live history |
| `RiskEngineTests` 5 greens | Dead path | Not on send |
| `Deal_upsert_is_idempotent` | InMemory | Not Postgres / live replay |
| 61 passing unit tests | Domain lab | §68 is a live-copy block list |
| `LiveCopyPage` amber text | Honest stub | Does not pass G18 |
| `ExecutionEnabled: false` literal | Accidental honesty | Not a coded gate |

A §68 item is **not** green if the only evidence is a DB enum, a skipped test, or a method nobody calls.

---

## 9. Sign-off

```text
[ ] 19/19 §68 gates PASS
[ ] 14/14 §70 live FIX acceptance PASS
[ ] First useful version (§69) signed (A57)
[ ] Explicit production flag reviewed
[ ] Manual review name / date / evidence links recorded
[ ] Default remains OFF if any box is unchecked
```

**Current:** all boxes unchecked. **Real copy: DISABLED.**

When a later coding wave ticks a box, update **A100 or a dated successor** with the test class name, command, timestamp, and SHA-256 of the test assembly. Do not tick from chat. Do not tick from this C14 file — it is a **still-FAIL** pin.

---

## 10. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §67–§70, §72
- `D:\Prop\reports\swarm\20260818\A100_golive_gates.md`
- `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md`
- `D:\Prop\reports\swarm\20260818\A28_phases_gates.md`
- `D:\Prop\reports\swarm\20260818\A57_first_useful_version.md`
- `D:\Prop\reports\swarm\20260818\B04_mt5_gap.md`
- `D:\Prop\reports\swarm\20260818\B05_fix_gap.md`
- `D:\Prop\reports\swarm\20260818\B07_workers_gap.md`
- `D:\Prop\reports\swarm\20260818\B08_tests_gap.md`
- `D:\Prop\reports\swarm\20260818\B13_risk_review.md`
- `D:\Prop\reports\swarm\20260818\B18_shadow_review.md`
- `D:\Prop\reports\swarm\20260818\C08_web_pages_review.md`
- `D:\Prop\apps\fix-worker\Worker.cs`, `Program.cs`, `appsettings.json`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\api\Program.cs`, `appsettings.json`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`, `Seeding\DemoSeeder.cs`, `Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`, `Shadow\ShadowCopyEngine.cs`, `Execution\QuantityNormalizer.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\tests\Unit\*`, `D:\Prop\tests\Integration\*`

---

*End of C14. Product source was not modified. Architecture §68 remains **0 PASS / 19 FAIL for live**. Real orders remain disabled.*
