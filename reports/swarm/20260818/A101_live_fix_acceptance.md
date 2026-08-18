# A101 — Architecture §70 live FIX acceptance (14 items, all FAIL)

| Field | Value |
|---|---|
| Agent | A101 (acceptance / design only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md` |
| Product source edited | **No** |
| Authority | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§70** (lines 2658–2676) |
| Binding siblings | `A20`, `A23`, `A25`, `A27` §7–8.2, `A28`, `A32`–`A36`, `A42`, `A46`–`A49`, `A61`, `A68`, `A70_execution_fsm.md` |
| Official RoE | https://help.ctrader.com/fix/specification/ |
| Official FAQ | https://help.ctrader.com/fix/faqs/ |
| Live account | Pepperstone / cServer `1369850` — **not** a test fixture |

**This file does not implement anything.** It is the measured scorecard for architecture §70 and the code contract that will turn each box green **without enabling real orders**.

---

## 0. Verdict

**Score: 0 / 14. Every §70 item is FAIL.**

Live copy is **safe by absence** (no `35=D` builder, no QuickFIX initiator, no TRADE socket). That is **not** a §70 pass. Several paths already **lie** about venue health (`DemoSeeder` + `fix-worker` stamp `LoggedOn` / `ReadyForMarketData` with no FIX engine). Treat those as **anti-evidence**.

```text
REAL_COPY_EXECUTION_ENABLED = false     -- stays false in every committed config
VenueMode default for tests            = InProcess
Live 35=D to *.c-trader.com            = FORBIDDEN until §68 + 14/14 + explicit prod review
```

**Definition of “satisfy without enabling real orders”** (binding for this file):

1. Committed `appsettings*`, `.env.example`, Docker, and launchSettings keep `REAL_COPY_EXECUTION_ENABLED=false` / `CTrader:RealCopyExecutionEnabled=false`.
2. The **only** function that may emit `MsgType=D` (`GuardedNewOrderSingle`) refuses unless the full conjunction in §16 is true. Tests that need a send flip the flag **in memory** and point `ICTraderFixVenue.Kind` at **`InProcess`**.
3. A live Pepperstone socket, if ever opened in staging, is **TRADE read + diagnostic Logon only** (`35=A/0/1/5`, optional `35=x`, `35=H/AF/AN`). No `35=D/F/G` on `live-us-eqx-01.p.c-trader.com`.
4. `apps/fix-worker` must **not** register `CTraderFixSimulator`. The in-process venue is tests only (`A68`).
5. Dashboard / seeder / worker must not report `LoggedOn` / `TradeHealthy=true` from a clock tick.

Until every box in §1 is green **and** the anti-greenwash checks in §17 pass, do **not** set the copy flag in any environment.

---

## 1. Scorecard (architecture §70 verbatim)

Quoted from architecture §70:

```text
1. TRADE FIX Logon is stable.
2. ExecutionReports are persisted correctly.
3. Position reports reconcile after restart.
4. Unique ClOrdID rules are proven.
5. Duplicate report handling is proven.
6. Unknown-state recovery is proven.
7. Partial fills are supported.
8. Order rejects are supported.
9. Cancel/replace is supported where required.
10. Destination position mapping is correct.
11. Risk-engine rejection happens before FIX send.
12. Real execution is feature flagged.
13. Global stop-new-orders works.
14. Reconciliation blocks execution while inconsistent.
```

| # | Item | Status | Classification | Proof lane (no live `35=D`) |
|---|---|---|---|---|
| 1 | TRADE FIX Logon is stable | **FAIL** | MISSING + **FAKE health** | InProcess Logon + optional diagnostic live Logon-only |
| 2 | ExecutionReports persisted correctly | **FAIL** | MISSING persist/applier | Recorded official `35=8` + InProcess book |
| 3 | Position reports reconcile after restart | **FAIL** | MISSING `35=AF/AN` + tables | Simulator restart + later TRADE **read** |
| 4 | Unique ClOrdID rules proven | **FAIL** | Factory only; no send path | Persist-before-send vs InProcess |
| 5 | Duplicate report handling proven | **FAIL** | Harness is identity fn | Same ER twice on InProcess TRADE |
| 6 | Unknown-state recovery proven | **FAIL** | SM helper only; `150=I` misused | Disconnect-after-submit script; `35=H/AF/AN` |
| 7 | Partial fills supported | **FAIL** | Enum maps `39=1`; no qty book | `PartialThenFill` script |
| 8 | Order rejects supported | **FAIL** | Enum maps `39=8`; no classes | ER / session / business reject scripts |
| 9 | Cancel / replace where required | **FAIL** | No `35=F/G/9` | InProcess cancel/replace; live send still off |
| 10 | Destination position mapping correct | **FAIL** | No `721` / link table | Simulator hedge book + links |
| 11 | Risk rejection before FIX send | **FAIL** | Pure `RiskEngine`; **not on send path** | Spy: `Submit` count = 0 |
| 12 | Real execution feature-flagged | **FAIL** | Option default false; **unread gate** | Flag false → `Submit` count = 0 with TRADE LoggedOn |
| 13 | Global stop-new-orders works | **FAIL** | Entity + risk branch; no API / send hook | Kill switch → `Submit` count = 0 |
| 14 | Recon blocks while inconsistent | **FAIL** | `!Reconciled` in risk; no gate | Mismatch seed → no NOS |

**Overall: FAIL.** Safe-to-start-the-process today ≠ §70 pass.

---

## 2. Measured tree (2026-08-18) — what exists vs what §70 needs

Honesty pin. Later coding tasks must not “tick” an item because a type name exists.

| Piece | Path | Class vs §70 |
|---|---|---|
| Architecture list | §70 | Binding 14 items |
| Fix worker | `D:\Prop\apps\fix-worker\Worker.cs` | 15 s loop: stamps `LastInboundAt`, forces TRADE `LoggedOn`. Reads `CTrader:RealCopyExecutionEnabled`. **No socket.** |
| Fix worker host | `D:\Prop\apps\fix-worker\Program.cs` | `AddTraderIntelligence` + `EnsureCreated` + `DemoSeeder`. No FIX DI. |
| Worker config | `apps/fix-worker/appsettings*.json` | Logging only. **No** `CTRADER_FIX_*`, **no** `REAL_COPY_EXECUTION_ENABLED`. |
| Options | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | Host/ports/CompIDs + `RealCopyExecutionEnabled=false`. **Not bound** by worker. Default host is the **live** Pepperstone name. |
| Package | `Fix.CTrader.csproj` | `QuickFix.Net 1.8.0` — **wrong pin** (`A35`: QuickFIXn.Core + QuickFIXn.FIX44 **1.14.1**). |
| Parser | `src/Fix.CTrader/Parsing/FixMessageParser.cs` | Checksum OK. `Dictionary` last-wins. **Cannot** parse MD / Security List groups. |
| String factory | `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | Not a venue. Tag 55 default `"XAUUSD"`. Missing 14/151/721/17. Fake disconnect Heartbeat. `150=I` mislabeled “unknown”. |
| In-memory fence | `src/Fix.CTrader/Services/FixSessionOwnership.cs` | Dev lock. **Not** Redis+Postgres (`A46`). Unused by worker. |
| SM | `src/Domain/Execution/ExecutionOrderStateMachine.cs` | Maps 39. Allows `PartiallyFilled` → `Accepted` (illegal, `A70` §5.2). No persist. |
| ClOrdID | `src/Domain/Execution/ClOrdIdFactory.cs` | String shape only. |
| Intent entity | `src/Domain/Entities/ExecutionIntent.cs` | Id + ClOrdId + status. **No** `sent_at`, fence, 37, 721, qty book. |
| Risk | `src/Domain/Risk/RiskEngine.cs` | `AllowFixSend` conjunction exists. **Nobody calls a send function.** |
| Kill switch | `KillSwitch` + `KillSwitchMode` | One exclusive `Mode` (violates `A48` two-control law). Not on send path. |
| Session row | `FixSessionState` / `fix_sessions` | Seeded **as if live**. |
| EF | `TraderDbContext` | Has `execution_intents`, `fix_sessions`, `kill_switches`, `destination_quotes`. **Missing** `fix_orders`, `fix_execution_reports`, `destination_positions`, `source_destination_links`, `execution_reconciliation_*`, `fix_session_leases`. |
| Dashboard | `EfDashboardQueries` | `TradeHealthy` / `QuoteHealthy` = enum in `{LoggedOn, Reconciling, Ready*}` — **greenwashes seeder**. |
| Seeder | `DemoSeeder.cs` | TRADE `LoggedOn`, QUOTE `ReadyForMarketData`, fake XAU quote, `VenueInstrumentId=null`. |
| Application ports | `src/Application/` | Ingest + dashboard only. **No** `ICTraderFixVenue`, no guarded send, no recon. |
| Tests | `tests/Unit`, `tests/Integration` | **No `*.cs` test classes.** `tests/Fix` **does not exist.** |
| Simulator venue | `A68` design | **MISSING** in product. |

`grep` of product `*.cs` for `NewOrderSingle`, `35=D`, `OrderMassStatus`, `RequestForPositions`, `READY_FOR_EXECUTION` as a **runtime gate**: no send/recon implementation. `MayRetryNewOrderSingle` is a pure helper.

---

## 3. Shared code every item depends on (build once; do not flip the flag)

These types are the seam. Item-specific code in §§4–17 assumes they exist.

### 3.1 Port (Application — production and tests)

```text
src/Application/Ports/Fix/ICTraderFixVenue.cs
src/Application/Ports/Fix/IFixSession.cs
src/Application/Ports/Fix/IFixQuoteClient.cs
src/Application/Ports/Fix/IFixTradeClient.cs
src/Application/Ports/Fix/IFixClock.cs
src/Application/Ports/Fix/FixInboundMessages.cs
```

Contract (`A68` §3.1, do not invent a second port):

- `VenueKind` = `InProcess` | `LiveQuickFix`.
- Two sessions, independent seq / HB / last in-out.
- TRADE-only: `SubmitNewOrderSingleAsync`, `RequestOrderStatusAsync` (`35=H`), `RequestMassStatusAsync` (`35=AF`), `RequestPositionsAsync` (`35=AN`), `CancelAsync` (`35=F`), `ReplaceAsync` (`35=G`).
- `Submit*` returning = write accepted by venue/engine, **not** accepted-by-cServer. Application moves to `SentAcknowledgementUnknown`.
- QUOTE must not expose `SubmitNewOrderSingleAsync`.

### 3.2 The only send function

```text
src/Application/Execution/GuardedNewOrderSingle.cs
```

Single choke point. Worker, flatten, tests — **no other** `35=D` builder. Re-check immediately before `Trade.SubmitNewOrderSingleAsync`:

```text
venue.Kind is InProcess
  OR (venue.Kind is LiveQuickFix AND REAL_COPY_EXECUTION_ENABLED
      AND CTRADER_FIX_ENABLED AND CTRADER_FIX_TRADE_SESSION_ENABLED
      AND NOT CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY)

AND TRADE.Status == ReadyForExecution
AND lease owned AND fencing token == persist token
AND RiskDecision.AllowFixSend == true
AND KillSwitch.StopNewExecution == false
AND ReconciliationGate.Ready == true
AND intent persisted, cl_ord_id unique, status == NotSent, not expired
AND (if price required) quote age <= MaxQuoteAge and instrument mapped
AND tag 55 is numeric (reject ticker strings before encode)
```

If `venue.Kind == LiveQuickFix` and `REAL_COPY_EXECUTION_ENABLED == false`: **throw / refuse**, persist `risk_decisions.Reason=REAL_COPY_DISABLED`, `Submit` count stays 0.

For §70 proof, **never** satisfy the live-and-flag-true branch. Tests set `Kind=InProcess` and may set the flag true **only** in that process.

### 3.3 In-process venue (tests)

```text
src/Fix.CTrader/Simulation/CTraderFixSimulator.cs
src/Fix.CTrader/Parsing/FixFieldList.cs          -- repeating groups
src/Fix.CTrader/Testing/FixSimulationHarness.cs  -- corrected fixture builder only
tests/Fix/TraderIntelligence.Tests.Fix.csproj
```

Replace `QuickFix.Net 1.8.0` with `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** (`A35`). Simulator must **not** reference the old package.

`FixWorker` composition: `AddCTraderFixSessions()` only. `AddCTraderFixSimulator()` is `InternalsVisibleTo` tests.

### 3.4 Tables the 14 items write (`A20` / `A61`)

Must exist as **versioned EF migrations** (not `EnsureCreated` in production):

```text
execution_venues
fix_sessions, fix_session_events, fix_session_leases
destination_symbols, destination_quotes
copy_intents, risk_decisions, execution_intents
fix_orders, fix_execution_reports, destination_positions
source_destination_links
execution_reconciliation_runs, execution_reconciliation_issues
kill_switches, audit_logs
```

Extend `ExecutionIntent` with: `sent_at`, `fencing_token`, `fix_session_key`, `dest_order_id`, `pos_maint_rpt_id`, `order_qty`, `cum_qty`, `leaves_qty`. Unique `cl_ord_id` stays.

### 3.5 Flags binder (`A49`)

```text
src/Application/Flags/RealExecutionFeatureFlags.cs
src/Fix.CTrader/Configuration/CTraderFixOptions.cs   -- add VenueMode, CtraderFixEnabled, DiagnosticLogonOnly
apps/fix-worker binds env CTRADER_FIX_* + REAL_COPY_EXECUTION_ENABLED
```

Defaults (`A25` §6.1): sessions may start; **copy flag false**.

---

## 4. Item 1 — TRADE FIX Logon is stable — **FAIL**

### 4.1 What “stable” means

Independent TRADE session (`57=TRADE`) reaches `LoggedOn` and **stays** there across Heartbeat / TestRequest / a clean reconnect with `141=Y`, without sharing seq files with QUOTE, without a second owner, and without the process pretending Logon from a seeder.

Official client Logon (RoE): `35=A`, `49=<env.broker.login>`, `56=CSERVER` (configurable; **no silent case fold** of issued `cServer`), `57=TRADE`, `50=` configured, `98=0`, `108=30`, `141=Y`, `553=` numeric login, `554=` secret.

Stable ≠ `READY_FOR_EXECUTION`. Logon is necessary, not sufficient (`A25` §2.3).

### 4.2 Why FAIL

| Evidence | Why it fails the item |
|---|---|
| No `CTraderTradeSession` / no `IInitiator` | Nothing speaks FIX |
| Wrong NuGet (`QuickFix.Net 1.8.0`) | Not the pinned engine |
| `Worker` sets `trade.Status = LoggedOn` every 15 s | **Fake Logon** |
| `DemoSeeder` inserts TRADE `LoggedOn` + live host/port | Dashboard `TradeHealthy=true` with zero TCP |
| `CTraderFixOptions.Host` defaults to live hostname | Dangerous if someone later news up an initiator |
| No `fix_session_events` Logon proof (`A25` §3.6) | Cannot audit headers / `58` Logout text |
| No lease | Two workers after a naive deploy would dual-Logon (FAQ: duplicate reports) |

### 4.3 Code that satisfies (no real orders)

```text
src/Fix.CTrader/Sessions/CTraderTradeSession.cs
src/Fix.CTrader/Sessions/CTraderQuoteSession.cs      -- isolation proof
src/Fix.CTrader/Sessions/CTraderFixSessionSettingsFactory.cs
src/Fix.CTrader/Sessions/CTraderFixApplication.cs    -- QuickFIX IApplication
src/Fix.CTrader/Headers/CTraderHeaderOptions.cs      -- 49/50/56/57 configurable
src/Infrastructure/Persistence/FixSessionEventStore.cs
src/Fix.CTrader/Services/FixSessionOwnership.cs      -- replace in-memory with A46 Redis+PG fence
tests/Fix/Harness/DisconnectDuringHeartbeatTests.cs
tests/Integration/Fix/QuickFixnSessionConfigurationTests.cs
tests/Fix/Harness/FixAdapterTestModeDoesNotHitVenueTests.cs
```

Behaviour:

1. **InProcess:** `ConnectAsync` + `LogonAsync` → sim replies `35=A` with **swapped** Comp/Sub IDs (`49=CSERVER`, `50=TRADE`, `56=` client). Persist `fix_session_events` `LOGON_OK`. Independent inbound/outbound seq start at 1 after `141=Y`.
2. **Isolation:** drop QUOTE; TRADE seq and `LoggedOn` unchanged (`A25` §2.3).
3. **Reconnect:** new Logon does **not** flush a send queue (`A25` §5.6).
4. **Optional staging (still no orders):** `CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=true`, `REAL_COPY_EXECUTION_ENABLED=false`, `VenueMode=LiveQuickFix`. TLS 5212 only. After `LOGON_OK`, send Heartbeat/TestRequest, Logout. Persist §3.6 record. **Refuse** `35=D/F/G/V`.
5. **Construction guard:** `VenueMode=InProcess` + host matching `*.c-trader.com` → test fail. Worker with `DiagnosticLogonOnly=false` and copy flag false still must not send.

### 4.4 Pass criteria

- [ ] Two session objects; two stores; two ports in settings (5211 / 5212 SSL default).
- [ ] TRADE `LOGON_OK` recorded with exact 49/50/56/57 as sent (case preserved).
- [ ] QUOTE down ≠ TRADE down.
- [ ] Worker no longer writes `LoggedOn` without `IFixSession.Status`.
- [ ] Seeder TRADE status is `Disconnected` (or omitted), never `LoggedOn`.
- [ ] Dashboard `LoggedOn` comes from session object, not from `LastInboundAt = UtcNow`.
- [ ] `GuardedNewOrderSingle` not invoked. Live `35=D` count = 0.

**Will not pass this item:** “we inserted a `fix_sessions` row.”

---

## 5. Item 2 — ExecutionReports are persisted correctly — **FAIL**

### 5.1 What “persisted correctly” means

Every inbound `35=8` is appended to `fix_execution_reports`, advances `fix_orders` **once** per new trade identity, and updates `execution_intents.status` through the `A70` graph. Official New+Fill pair (`A32` / `A68` §5) is the codec golden.

Required fields on the persist DTO (`A68` §5.3): 11, 37, 150, 39, 55 (numeric), 54, 32, 31 and/or 6, 14, 151, 38, 60, 52, 58, 103, 721, 584, 911, 17 (optional).

Dedup key (`A68` §7.2 / `A70` §8): tag 17 if present, else `syn:` + sha256 of `37|150|39|14|32|(31|6)|60|11`.

### 5.2 Why FAIL

- No `FixExecutionReport` entity / no `fix_execution_reports` table.
- No `IExecutionReportApplier`.
- `FixMessageParser` cannot retain duplicate tags (not needed for ER groups, but harness ERs omit 14/151/721).
- Harness ER headers are client-side (`49=SENDER`); official server is `49=CSERVER|50=TRADE`.
- SM does not persist and does not lock qty.

### 5.3 Code that satisfies (no real orders)

```text
src/Application/Execution/ExecutionReportMessage.cs
src/Application/Execution/IExecutionReportApplier.cs
src/Application/Execution/ExecutionReportApplier.cs
src/Domain/Execution/ExecutionOrderStateMachine.cs   -- fix illegal reverse (A70 §5.2)
src/Domain/Entities/FixOrder.cs
src/Domain/Entities/FixExecutionReport.cs
src/Infrastructure/Persistence/Configurations/FixExecutionReportConfiguration.cs
src/Fix.CTrader/Parsing/RecordedExecutionReportParser.cs
tests/Fix/Fixtures/er/er_official_new_market_buy.fix
tests/Fix/Fixtures/er/er_official_fill_market_buy.fix
tests/Fix/Harness/RecordedExecutionReportParseTests.cs
tests/Integration/Fix/ExecutionReportHandlingTests.cs
```

Drive: load official `|` files → `FixFieldList` + checksum → typed ER → applier → EF InMemory / Testcontainers. **No** live TRADE. InProcess may also emit the same New+Fill after a simulated `35=D` (flag true **only** in that test, `Kind=InProcess`).

### 5.4 Pass criteria

- [ ] Official New: `150=0`, `39=0`, `14=0`, `151=10000`, `721=101` persisted; intent `Accepted`; `fix_orders.dest_order_id=101`.
- [ ] Official Fill: `150=F`, `39=2`, `14=10000`, `32=10000`, `6=1.0674`, `151=0`; intent `Filled`; dest position 721 adopted.
- [ ] Bad checksum → fixture load throws; applier never called.
- [ ] `150=I` restatement persists a row but does **not** add qty (`A70` §8).
- [ ] Terminal `Filled` + later non-fill ER stays `Filled` (row still stored).
- [ ] Live `35=D` count = 0 outside InProcess tests.

---

## 6. Item 3 — Position reports reconcile after restart — **FAIL**

### 6.1 What it means

Architecture §42: TRADE Logon → **block new executions** → `35=AF` (MassStatusReqType=7) → `35=AN` → consume `35=8` / `35=AP` → compare DB → repair → only then `READY_FOR_EXECUTION`.

Never assume Postgres is the venue book after process death (`A47`).

### 6.2 Why FAIL

- No `RequestMassStatusAsync` / `RequestPositionsAsync`.
- No `destination_positions`, no recon run/issue tables.
- Worker never enters `Reconciling`.
- Seeder TRADE is `LoggedOn` with **zero** AF/AN.
- `RiskEngine` has a `Reconciled` bit that nothing sets from a run.

### 6.3 Code that satisfies (no real orders)

```text
src/Application/Reconciliation/StartupReconciliationCoordinator.cs
src/Application/Reconciliation/PeriodicReconciliationCoordinator.cs
src/Application/Reconciliation/ReconciliationGate.cs
src/Application/Reconciliation/VenueSnapshot.cs
src/Domain/Entities/DestinationPosition.cs
src/Domain/Entities/ExecutionReconciliationRun.cs
src/Domain/Entities/ExecutionReconciliationIssue.cs
src/Fix.CTrader/Simulation/Scripts/SeededBookRestartScript.cs
tests/Fix/Harness/StartupReconciliationAfterSimulatedRestartTests.cs
tests/Integration/Reconcile/PositionReconciliationTests.cs
```

**InProcess proof (required first):**

1. Seed sim book with working order + position (`37=101`, `721=101`, qty Q).
2. Persist matching `fix_orders` / `destination_positions` (as if previous process).
3. New venue instance **same seed**, new worker, acquire lease, TRADE Logon.
4. Gate stays **not ready**. Persist `35=AF` / `35=AN` request ids **before** send (`A47` §6.3).
5. Consume `150=I` ERs + `35=AP`. Compare (`A47` §7).
6. Match → `READY_FOR_EXECUTION`. Mismatch → `BLOCKED_INCONSISTENT` (item 14).
7. Empty book: `35=j` (AF) and `35=AP` `728=2` — do **not** treat timeout as flat.

**Later staging (still no orders):** live TRADE read with copy flag false. Persist real AF/AN. Do not send `35=D`.

### 6.4 Pass criteria

- [ ] After simulated restart, no `GuardedNewOrderSingle` until run `status=READY`.
- [ ] Run row + issues persisted; dashboard can show last cTrader recon (`A26` / §54).
- [ ] Unsolicited fill between AF and AN applied once then compared (`A47` §6.5).
- [ ] `150=I` does not double qty.
- [ ] Live `35=D` = 0.

---

## 7. Item 4 — Unique ClOrdID rules are proven — **FAIL**

### 7.1 What it means

Architecture §33 / RoE: unique client order ids for all orders (status query is by tag 11). Persist `cl_ord_id` **before** socket write. Never reuse after persist. Cancel/replace allocate a **new** 11; tag 41 = original. Replacement after `not_on_venue` is a **new** row (`A42`, `A70`).

Charset: ≤ 32 printable ASCII `[0-9A-Z]` (`A42`). Factory today emits `TI{yyyyMMddHHmmss}{seq}{intent16}` — keep if it stays unique and charset-legal (hex of a Guid is fine; do not introduce `-` after compact).

### 7.2 Why FAIL

- `ClOrdIdFactory` has **no tests**.
- Unique index on `execution_intents.cl_ord_id` exists in fluent API; **no migration**, no persist-before-send service.
- `MayRetryNewOrderSingle(Rejected)==true` does not allocate a new id.
- No collision test under parallel intents / reconnect.

### 7.3 Code that satisfies (no real orders)

```text
src/Application/Execution/PersistBeforeSendService.cs
src/Application/Execution/IExecutionIntentStore.cs
src/Domain/Execution/ClOrdIdFactory.cs              -- keep; freeze clock+seq in tests
tests/Unit/Execution/ClOrdIdGenerationTests.cs
tests/Fix/Harness/UniqueClOrdIdUnderRetryTests.cs
```

Proof on InProcess:

1. Two intents → two distinct 11; unique constraint holds.
2. Persist row `NotSent` **then** `Submit`. Crash between persist and submit → status unknown or still `NotSent` only if write proven absent (`A25` §5.3 fail-safe = unknown).
3. Rejected intent: second attempt = **new** 11; old row stays `Rejected`.
4. Unknown recovery `not_on_venue`: new 11; old row never rewritten to `NotSent`.
5. Same 11 never appears on a second `SubmitNewOrderSingleAsync`.

### 7.4 Pass criteria

- [ ] `UNIQUE (cl_ord_id)` on intents and `fix_orders`.
- [ ] Factory deterministic given (intentId, clock, seq).
- [ ] Outbound InProcess `35=D` tag 11 matches persisted row.
- [ ] Retry/replace 11 ≠ original 11.
- [ ] Live `35=D` = 0.

---

## 8. Item 5 — Duplicate report handling is proven — **FAIL**

### 8.1 What it means

Official FAQ: multiple TRADE connections cause the server to **copy** each report to every connection. Duplicate `35=8` is **not** a second fill (`A34`, `A70` §8).

Classify: `NEW_TRADE` / `STATUS_SNAPSHOT` (`150=I`) / `WIRE_DUPLICATE` / `SEMANTIC_DUPLICATE` / `ORPHAN` / `CONFLICT`. Apply LastQty only on `NEW_TRADE`. Unique `(venue_id, exec_id)` or fingerprint.

### 8.2 Why FAIL

- `SimulateDuplicateExecutionReport` returns the same string; nothing consumes it.
- No `exec_id` / fingerprint unique key.
- No metric `fix_execution_reports_duplicate_total`.

### 8.3 Code that satisfies (no real orders)

```text
src/Application/Execution/ExecutionReportClassifier.cs
-- unique index fix_execution_reports_exec_uk (venue_id, exec_id)
tests/Fix/Harness/DuplicateExecutionReportTests.cs
```

InProcess script `DuplicateNextEr`: emit identical Fill twice (two inbound seq, same body). Also: `PossDup=Y` / second process replay of the same handler.

### 8.4 Pass criteria

- [ ] Two identical fills → one persist row (or second `ON CONFLICT DO NOTHING`), `cum_qty` once, one `721`, status `Filled`.
- [ ] New then New (same 37/150/39/14) → still `Accepted`, one applied ack.
- [ ] Partial then fill (different 14/32/39) is **not** a duplicate.
- [ ] Metric increments on the second.
- [ ] Dual-owner is a **lease** test (`A46`), not an excuse to double-book.
- [ ] Live `35=D` = 0.

---

## 9. Item 6 — Unknown-state recovery is proven — **FAIL**

### 9.1 What it means

Architecture §34: persist → send `35=D` → disconnect / no ER → **did cServer see it?** Do **not** resend same or new 11 until `35=H` / `35=AF` / `35=AN` decide adopt vs `not_on_venue` vs `BLOCKED_INCONSISTENT`.

`150=I` is the **recovery response**, not the fault (`A68` §11). Current `SimulateExecutionReport_UnknownState` is **wrong**.

### 9.2 Why FAIL

- `AfterDisconnectWithUnknownAck()` exists; nothing calls it from transport.
- No recovery service.
- No `35=H/AF/AN` client.
- Harness disconnect is a Heartbeat with tag 1128.

### 9.3 Code that satisfies (no real orders)

```text
src/Application/Execution/UnknownExecutionRecoveryService.cs
src/Fix.CTrader/Simulation/Scripts/UnknownStateDisconnectSimulator.cs
tests/Unit/Execution/UnknownExecutionStateTests.cs
tests/Integration/Reconcile/UnknownExecutionRecoveryTests.cs
tests/Fix/Harness/UnknownStateRecoveryTests.cs
tests/Fix/Harness/DisconnectAfterNewOrderSingleTests.cs
```

InProcess scripts (`A68` §11):

| Seed | Sim book after drop | Legal outcome |
|---|---|---|
| `VenueAcceptedButSilent` | order exists (maybe filled) | `35=H`/`AF` adopt; **no** second 11 |
| `VenueNeverSaw` | empty | mass-status empty/`35=j` + positions unchanged → `not_on_venue`; **new** 11 only after that |

Illegal (assert): `catch (Disconnect) { Submit(same or new 11); }` before recon.

### 9.4 Pass criteria

- [ ] After `DisconnectAfterSubmit`: status unknown; outbound `35=D` count = 1; `MayRetryNewOrderSingle==false`; `RequiresReconciliation==true`.
- [ ] Adopt-filled: dest position appears; no second ClOrdID.
- [ ] `not_on_venue`: old row not rewritten to `NotSent`.
- [ ] Mismatch → item 14 path; no NOS.
- [ ] Startup without completed AF+AN stays off `READY_FOR_EXECUTION`.
- [ ] Live `35=D` = 0.

---

## 10. Item 7 — Partial fills are supported — **FAIL**

### 10.1 What it means

`39=1` / `150=F` then `39=2`. Persist `14`/`151`/`32`. One `721`. Qty in **cTrader units** (max precision 0.01). Do not accumulate in two places with different rules (`A68` §8): simulator emits consistent tags; applier trusts venue numbers; `14 != previous 14 + 32` → recon issue, no silent clamp.

IOC remainder may cancel leaves (`39=4` after partial).

### 10.2 Why FAIL

- SM maps `39=1` but allows reverse to `Accepted`.
- Harness partial omits 14/151/38/54/721; default qty `0.5` looks like lots.
- No dest position qty update.

### 10.3 Code that satisfies (no real orders)

```text
-- ExecutionReportApplier qty path (A70 §7)
src/Fix.CTrader/Simulation/Scripts/PartialThenFillScript.cs
tests/Fix/Harness/PartialFillLifecycleTests.cs
-- fix ExecutionOrderStateMachine: partial ↛ accepted
```

Script (`A68` §8.1): `Q=10000`, `Q1+Q2=Q`, three ERs. Application `QuantityNormalizer` runs **before** `Submit`, not inside the sim.

### 10.4 Pass criteria

- [ ] After ER2: `PartiallyFilled`, dest qty = Q1, leaves = Q−Q1.
- [ ] After ER3: `Filled`, dest qty = Q, leaves = 0, one `721`.
- [ ] Overflow third fill → issue, status stays `Filled`, qty not increased.
- [ ] `partial` + `39=0` stays `partial` (SM fix).
- [ ] Live `35=D` = 0.

---

## 11. Item 8 — Order rejects are supported — **FAIL**

### 11.1 What it means

Three reject classes (`A68` §9) — do not collapse:

| Class | Wire | Application |
|---|---|---|
| Execution reject | `35=8` `150=8` `39=8` | Terminal `Rejected`. New attempt = new 11. |
| Session reject | `35=3` | If this was the NOS write → **unknown**, not auto-`Rejected`. |
| Business reject | `35=j` | Reject-of-send only if `RefSeqNum` matches outbound NOS **and** no ER; else unknown. |

Failed Logon is `35=5`, not `35=3`. MD reject `35=Y` is quote-path only.

### 11.2 Why FAIL

- Harness reject omits 103/14/151.
- No session/business reject handlers.
- Risk reject-before-send is unwired (item 11).

### 11.3 Code that satisfies (no real orders)

```text
src/Application/Execution/RejectClassifier.cs
src/Fix.CTrader/Simulation/Scripts/OrderRejectSimulator.cs
tests/Fix/Harness/OrderRejectLifecycleTests.cs
tests/Fix/Harness/RiskRejectionBeforeFixSendTests.cs   -- also item 11
```

Default ER reject: `103=0`, `58=INSUFFICIENT_MARGIN`, `14=0`, `151=0`, no 721. Also: `55=XAUUSD` → reject with official “Expected numeric symbolId” text.

### 11.4 Pass criteria

- [ ] ER reject terminal; late fill ignored (frozen).
- [ ] Next attempt new ClOrdID.
- [ ] Session reject after write → `RequiresReconciliation`; no second `35=D`.
- [ ] `REAL_COPY_EXECUTION_ENABLED=false` → `Submit` = 0 even if LoggedOn.
- [ ] Live `35=D` = 0.

---

## 12. Item 9 — Cancel / replace is supported where required — **FAIL**

### 12.1 What “where required” means

Not “build a full OMS.” Required before live copy:

- `35=F` OrderCancelRequest (tag 11 = **cancel id**, tag 41 = orig) and inbound `35=8` `150=4` / `35=9` CancelReject.
- `35=G` CancelReplace for qty/price amend when policy needs it (limit rest); market IOC usually has nothing to replace.
- Kill-switch flatten later uses **close** `35=D` or cancel of working copy orders — still persist-before-send, still unknown-state. **Not** enabled on live in this acceptance pass.

`A27` allows `CancelReplaceInTestModeTests` to Skip until Phase 8 **only** if this item stays FAIL. Skipping is not a pass.

### 12.2 Why FAIL

- No F/G/9 codec, no orig-id link on `ExecutionIntent`.
- SM maps `39=4` only.

### 12.3 Code that satisfies (no real orders)

```text
src/Application/Execution/CancelReplaceService.cs
src/Fix.CTrader/Simulation/SimOrderBook.cs          -- rest a limit; accept F/G
tests/Fix/Harness/CancelReplaceInTestModeTests.cs
```

InProcess:

1. Seed a resting limit (`40=2`) after New ER.
2. `CancelAsync(orig, newClOrdId)` → ER `150=4` `39=4`; orig terminal `Cancelled`; dest leaves 0; `721` unchanged if already flat.
3. `ReplaceAsync` → child 11, 41=orig; book qty updates; orig no longer working.
4. Cancel unknown 11 → `35=9`; intent not invented.
5. `GuardedNewOrderSingle` still refuses live. Cancel on **LiveQuickFix** is also refused while copy flag is false **unless** a later flatten permission exists — **v1: refuse live F/G too**. Prove F/G only on InProcess.

### 12.4 Pass criteria

- [ ] InProcess cancel/replace green.
- [ ] New 11 for F and for G; 41 set.
- [ ] CancelReject does not mark `Cancelled` as if it worked.
- [ ] Live `35=D/F/G` = 0.

---

## 13. Item 10 — Destination position mapping is correct — **FAIL**

### 13.1 What it means

Architecture §35:

```text
source reconstructed trade
        ↓
destination execution orders
        ↓
destination cTrader position ID(s)     -- tag 721 PosMaintRptID
```

Support entry, scale-in, partial close, full close, reversal. **Do not** assume one source event = one dest order forever.

Hedge: inbound 721 attaches to existing position; else allocate (`A68` §12).

### 13.2 Why FAIL

- No `DestinationPosition`, no `SourceDestinationLink`.
- Quote snapshot has `VenueInstrumentId=null`.
- No 721 on harness ERs.

### 13.3 Code that satisfies (no real orders)

```text
src/Application/Execution/DestinationPositionMapper.cs
src/Domain/Entities/DestinationPosition.cs
src/Domain/Entities/SourceDestinationLink.cs
tests/Unit/Execution/DestinationPositionMappingTests.cs
tests/Fix/Harness/PartialFillLifecycleTests.cs      -- one 721 across partials
```

InProcess book allocates monotonic 721 from 101. Mapper writes `link_role` `ENTRY` / `SCALE_IN` / `PARTIAL_CLOSE` / `CLOSE` / `REVERSAL` (`A20`). Scale-in two `35=D` (InProcess only) → two intents / two 11 / **same** 721 if attach-to-position. Reversal = CLOSE then OPEN (two intents), two 721 or flatten+new per hedge rules — assert explicitly, do not guess.

### 13.4 Pass criteria

- [ ] UNIQUE `(venue_id, destination_account, destination_position_id)`.
- [ ] Partial path: one 721, qty = cum.
- [ ] Close: `is_open=false`; link `CLOSE`.
- [ ] Unknown 721 on inbound ER → issue, do not invent source trade.
- [ ] Live `35=D` = 0.

---

## 14. Item 11 — Risk-engine rejection happens before FIX send — **FAIL**

### 14.1 What it means

Architecture §70.11 / §37 / §39: RiskEngine is the last authority before send. Reject / pause / global stop / stale quote / stale signal / spread / price-move / limits ⇒ **builder never runs**. Scoring/ML cannot override (`A23`).

Existing `RiskEngine.Evaluate` already returns `AllowFixSend=false` on those reasons. That is **not** a pass until a send function consults it.

### 14.2 Why FAIL

- No `GuardedNewOrderSingle`.
- No persist of `risk_decisions` on the copy path (`RiskDecisionRecord` table exists; nothing writes it from a live pipeline).
- Worker never sees a `CopyIntent`.

### 14.3 Code that satisfies (no real orders)

```text
src/Application/Risk/RiskEvaluationService.cs     -- persist RiskDecisionRecord
src/Application/Execution/GuardedNewOrderSingle.cs
tests/Unit/Risk/RiskEngineHardLimitTests.cs
tests/Fix/Harness/RiskRejectionBeforeFixSendTests.cs
tests/Fix/Harness/QuoteUnavailableBlocksNewCopyTests.cs
```

Each case: build an approved-looking intent, force one reject input (`QUOTE_STALE`, `QUOTE_MISSING`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `SIGNAL_STALE`, max loss, martingale, `VENUE_UNHEALTHY`, `!RealExecutionEnabled`), run the **real** worker pipeline against InProcess.

Pass = `IFixTradeClient.SubmitNewOrderSingleAsync` **invocation count = 0** (Moq / spy). Persist `AllowFixSend=false` + reason.

Also: `AllowFixSend=true` in an InProcess test **does** call Submit — that proves the hook is on the path, not that live send is on.

### 14.4 Pass criteria

- [ ] Every hard reject in `A23` has a spy test with Submit = 0.
- [ ] Approve + InProcess + flag true in memory → Submit = 1 (control).
- [ ] Approve + LiveQuickFix + flag false → Submit = 0 (item 12).
- [ ] Live `35=D` = 0.

---

## 15. Item 12 — Real execution is feature flagged — **FAIL**

### 15.1 What it means

Architecture §41 / §70.12:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

Connect, quote, status, positions — **without** new real exposure. TRADE up ≠ license to send. Runtime cannot turn the copy flag **on** if config is false (`A25` §6.4). Dashboard must not show a button that bypasses config.

### 15.2 Why FAIL

| Evidence | Why |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` default false | Unbound; worker reads a **different** key `CTrader:RealCopyExecutionEnabled` |
| Worker logs the flag | Then still marks TRADE `LoggedOn` and does not implement a refuse-on-`35=D` because there is no `35=D` |
| No `CTRADER_FIX_ENABLED` | Sessions cannot be disabled independently |
| Vacuous “no send” | Absence, not a gate (`A08`, `A49`) |

### 15.3 Code that satisfies (no real orders)

```text
src/Application/Flags/RealExecutionFeatureFlags.cs
apps/fix-worker/Configuration/FixWorkerOptionsBinder.cs
apps/fix-worker/appsettings.json          -- RealCopyExecutionEnabled: false
tests/Unit/Risk/RealExecutionFeatureFlagTests.cs
tests/Integration/Flags/RealExecutionDisabledIntegrationTests.cs
```

Binder maps **both** env names (`REAL_COPY_EXECUTION_ENABLED` and `CTrader__RealCopyExecutionEnabled`) to one immutable options object. Config `false` wins over any dashboard “enable” attempt (write rejected; audit).

Composition: if flag false, `GuardedNewOrderSingle` short-circuits **before** encode. TRADE may still `Logon` + AF/AN.

InProcess test: TRADE `ReadyForExecution`, risk approve, **flag false** → Submit = 0. Same test flag true + InProcess → Submit = 1.

### 15.4 Pass criteria

- [ ] Every committed config default is false.
- [ ] Env `REAL_COPY_EXECUTION_ENABLED=true` in a test host with `VenueMode=LiveQuickFix` still refuses if host is `*.c-trader.com` (defence).
- [ ] Dashboard `RealCopyEnabled` reads the binder, not seeder.
- [ ] Worker log line remains; **plus** a structured refuse metric `fix_nos_refused_total{reason=flag}`.
- [ ] Live `35=D` = 0.

**Will not pass this item:** “default is false on a POCO nobody reads.”

---

## 16. Item 13 — Global stop-new-orders works — **FAIL**

### 16.1 What it means

Architecture §40 / §70.13: `STOP_NEW_EXECUTION` blocks **new** copy exposure (`OPEN` / `INCREASE`). Existing dest positions **untouched**. Not `EMERGENCY_FLATTEN`. Not the copy feature flag.

`A48`: do not persist a single exclusive `KillSwitchMode` as the only state. Two independent controls + audit.

### 16.2 Why FAIL

- `RiskEngine` checks `KillSwitchMode.StopNewExecution` — unwired.
- `kill_switches` has one row `Mode=None` from seeder.
- No API / RBAC (`A06` / `A51`) to set it.
- Exclusive enum is the `A48` defect.

### 16.3 Code that satisfies (no real orders)

```text
src/Application/Risk/KillSwitchState.cs          -- StopNewExecution bool + Flatten separately
src/Application/Risk/IKillSwitchStore.cs
src/Infrastructure/Persistence/EfKillSwitchStore.cs
-- API later: POST /risk/stop-new  (A26); v1 may set via store in tests only
tests/Unit/Risk/KillSwitchStopNewExecutionTests.cs
tests/Fix/Harness/GlobalStopNewOrdersTests.cs
```

Proof: InProcess, flag true in memory, risk would otherwise approve, `StopNewExecution=true` → Submit = 0, reason `STOP_NEW_EXECUTION`, dest positions unchanged, no `35=F/G`. `EmergencyFlatten` must **not** flip on as a side effect.

Do **not** implement live flatten to pass this item.

### 16.4 Pass criteria

- [ ] Stop-new blocks OPEN/INCREASE only.
- [ ] REDUCE/CLOSE policy remains as `A23` (still no live send).
- [ ] Audit log row on change (`set_by`, `reason`, timestamp).
- [ ] Submit = 0 while stop is on.
- [ ] Live `35=D` = 0.

---

## 17. Item 14 — Reconciliation blocks execution while inconsistent — **FAIL**

### 17.1 What it means

Architecture §42–§43 / §70.14: if internal book ≠ cServer (unknown external, missing internal, qty/side mismatch, orphan ER, unexpected fill, unresolved unknown), `READY_FOR_EXECUTION` is false and `GuardedNewOrderSingle` refuses. Nothing unresolved is silently ignored (`A47`, §54).

`RiskEngine` `!Reconciled` → `VENUE_NOT_RECONCILED` is the evaluation branch. The **gate** must be derived from the latest `execution_reconciliation_runs` row, not a bool the worker sets to true.

### 17.2 Why FAIL

- No recon run.
- Seeder + worker imply TRADE healthy without a run.
- `RiskEvaluationRequest.Reconciled` has no producer.

### 17.3 Code that satisfies (no real orders)

```text
src/Application/Reconciliation/ReconciliationGate.cs
src/Fix.CTrader/Simulation/Scripts/MismatchBookScript.cs
tests/Fix/Harness/ReconciliationBlocksExecutionWhileInconsistentTests.cs
tests/Integration/Reconcile/StartupReconciliationGateTests.cs
```

Seeds (`A47` / `A68` §11.4):

1. Sim has position DB does not → `UnknownExternalPosition` → gate false → Submit = 0.
2. DB has open dest position sim does not → `MissingInternalPosition` → same.
3. Qty/side mismatch → same.
4. Incomplete snapshot (timeout, no `35=j`, no `728=2`) → **not** treated as flat; gate false.
5. After repair + successful run → gate true (InProcess only may then Submit).

### 17.4 Pass criteria

- [ ] `READY_FOR_EXECUTION` false unless last **successful** run for that `(venue, account)` is current (TTL / “since last TRADE Logon”).
- [ ] Every `ReconciliationIssueType` has at least one seed → Submit = 0.
- [ ] Logon without AF+AN complete → gate false (item 3 + 6 + 14).
- [ ] Live `35=D` = 0.

---

## 18. Conjunction required to send (do not satisfy on live)

Copied from `A25` §6.3 so this file is self-contained. **§70 can be 14/14 with this conjunction still false on LiveQuickFix.**

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED          -- default false; InProcess tests may flip
TRADE == READY_FOR_EXECUTION
lease owned + fencing token current
risk AllowFixSend
STOP_NEW_EXECUTION == false
QUOTE usable if the order needs a price
intent + cl_ord_id persisted, status NotSent, not expired
venue.Kind == InProcess
  OR (explicit prod review + §68 19/19 — OUT OF SCOPE for A101)
```

`EMERGENCY_FLATTEN` is **not** a §70 item and is **not** required to pass this list. Do not send flatten `35=D` to prove stop-new.

---

## 19. Anti-greenwash (must fix or items 1 / 12 / 14 stay FAIL)

These current behaviours would let a dashboard or operator believe §70 is done:

| Location | Defect | Required change |
|---|---|---|
| `DemoSeeder` TRADE `LoggedOn` / QUOTE `ReadyForMarketData` | Fake venue | Seed `Disconnected`; quote row optional and **stale** or absent |
| `DemoSeeder` dest quote with `VenueInstrumentId=null` | Fake book | Do not seed a “live” quote; or mark `PriceSource` test-only |
| `Worker` stamps `LastInboundAt` + `LoggedOn` | Fake heartbeat | Only session object updates timestamps |
| `EfDashboardQueries` `TradeHealthy` / `QuoteHealthy` from enum | Believes seeder | Healthy = session object connected **and** (QUOTE: quote age) **and** (TRADE: last recon run) |
| `FixSessionDto.ExecutionEnabled` hardcoded `false` | Accidental honesty | Keep false until binder says otherwise; do not infer from `LoggedOn` |
| `EnsureCreated` in worker | Hides missing migrations | Versioned migrations (`A61`); tests use Testcontainers / InMemory |

A §70 item is **not** green if the only evidence is a DB enum.

---

## 20. Test map (closes `A27` §8.2)

Project **`D:\Prop\tests\Fix`** does not exist. Creating it is part of satisfying §70, not optional polish.

| # | A27 class | Satisfies item |
|---|---|---|
| 1 | `QuickFixnSessionConfigurationTests`, `DisconnectDuringHeartbeatTests`, `FixAdapterTestModeDoesNotHitVenueTests` | 1 |
| 2 | `RecordedExecutionReportParseTests`, `ExecutionReportHandlingTests` | 2 |
| 3 | `StartupReconciliationAfterSimulatedRestartTests`, `PositionReconciliationTests` | 3 |
| 4 | `ClOrdIdGenerationTests`, `UniqueClOrdIdUnderRetryTests` | 4 |
| 5 | `DuplicateExecutionReportTests` | 5 |
| 6 | `UnknownExecutionStateTests`, `UnknownExecutionRecoveryTests`, `DisconnectAfterNewOrderSingleTests` | 6 |
| 7 | `PartialFillLifecycleTests` | 7 |
| 8 | `OrderRejectLifecycleTests` | 8 |
| 9 | `CancelReplaceInTestModeTests` | 9 |
| 10 | `DestinationPositionMappingTests` | 10 |
| 11 | `RiskRejectionBeforeFixSendTests`, `RiskEngineHardLimitTests` | 11 |
| 12 | `RealExecutionFeatureFlagTests`, `RealExecutionDisabledIntegrationTests` | 12 |
| 13 | `KillSwitchStopNewExecutionTests`, `GlobalStopNewOrdersTests` | 13 |
| 14 | `ReconciliationBlocksExecutionWhileInconsistentTests`, `StartupReconciliationGateTests` | 14 |

Shared: `QuoteUnavailableBlocksNewCopyTests`, `TradeUnavailableDoesNotQueueUnlimitedBacklogTests` (support 11–14 / §62).

---

## 21. Implementation order (coding tasks; still no live orders)

Matches `A25` §12 / `A28` / `A68` §15 / `A30` without Phase 8 enablement:

1. **Codec** — `FixFieldList`, checksum, official ER/MD/y goldens. Fix harness tags (numeric 55, 14/151/721, server headers). **Unlocks 2, 5, 7, 8.**
2. **Ports + InProcess venue** — Logon two sessions, MD, NOS New+Fill, disconnect as transport. `tests/Fix` + “does not hit venue.” **Unlocks 1 (sim), 4, 6, 9.**
3. **Flags + `GuardedNewOrderSingle` + risk persist.** Default copy false. **Unlocks 11, 12, 13** (spy tests).
4. **EF tables** — intents extras, `fix_orders`, ERs, dest positions, links, recon, leases. Migrations. **Unlocks 2 persist, 3, 10, 14.**
5. **Applier + SM fix** (`A70`). Dup fingerprint. **Hardens 2, 5, 7, 8.**
6. **Startup/periodic recon + gate.** **Unlocks 3, 6 recovery, 14.**
7. **Ownership** (`A46`) — second instance cannot Logon TRADE. Required before any live diagnostic TRADE.
8. **Optional diagnostic live Logon-only** (item 1 soak). Still no `35=D`.
9. **Optional live TRADE read** (item 3 soak on 1369850). Copy flag false.
10. **Stop.** Do not set `REAL_COPY_EXECUTION_ENABLED=true`. §68 is a **separate** 19-item list (`A28`).

Do not wait for QuickFIX to start steps 1–6. Do not write a `TcpClient` engine (`A05`, `A35`).

---

## 22. What this file does **not** authorize

- `REAL_COPY_EXECUTION_ENABLED=true` in any committed file or operator runbook.
- Live `NewOrderSingle` / cancel / replace / flatten against account `1369850`.
- Treating `FixSimulationHarness` as §61 done (`A68` §16 still all open).
- Treating worker `LoggedOn` stamps as item 1.
- Hardcoding Pepperstone XAU tag 55 (use test id `41` in sim; live discovery later, `A68` §6.3).
- Blind retry on TCP drop.
- Registering the simulator in `apps/fix-worker`.
- Claiming §70 14/14 because Domain enums exist.

---

## 23. Residual risks (do not paper over)

1. **Fake health is worse than a blank dashboard.** Item 1 cannot pass until seeder/worker/dashboard are honest.
2. **No ExecID in official ERs.** Dedup must work without tag 17 (`A68` §7.2, `A70` §8). Confirm on first **recorded** capture; do not need a live order to capture a status ER if TRADE read is later enabled.
3. **Header case `cServer` vs `CSERVER`.** Item 1 diagnostic Logon is the only legal way to resolve it. Do not silently mutate.
4. **Package mismatch.** Shipping sessions on `QuickFix.Net 1.8.0` is a new defect.
5. **`KillSwitchMode` exclusive enum** conflicts with `A48`. Item 13 pass requires two independent bits even if the table is migrated in place.
6. **Item 9 vs Phase 8.** Prove F/G on InProcess now so §70.9 can go green without live send. Skip ≠ pass.
7. **Live TRADE read still duplicates reports** if two owners. Item 1 + `A46` before any live 5212.

---

## 24. One-page operator view

```text
§70 live FIX acceptance                         2026-08-18
=========================================================
 1. TRADE FIX Logon stable                      FAIL
 2. ExecutionReports persisted                  FAIL
 3. Position reports reconcile after restart    FAIL
 4. Unique ClOrdID rules proven                 FAIL
 5. Duplicate report handling proven            FAIL
 6. Unknown-state recovery proven               FAIL
 7. Partial fills supported                     FAIL
 8. Order rejects supported                     FAIL
 9. Cancel/replace supported where required     FAIL
10. Destination position mapping correct        FAIL
11. Risk rejection before FIX send              FAIL
12. Real execution feature-flagged              FAIL
13. Global stop-new-orders works                FAIL
14. Recon blocks execution while inconsistent   FAIL
---------------------------------------------------------
Score                                           0 / 14
REAL_COPY_EXECUTION_ENABLED                     false (policy; not a coded gate)
Live 35=D possible if process starts now?       No (absence)
Safe to enable real orders?                     No
Next proof venue                                CTraderFixSimulator (InProcess)
Pepperstone 1369850 as first test?              Forbidden (§61)
=========================================================
```

When a later coding wave ticks a box, update **this file** (or a dated successor) with the test class name, command, timestamp, and SHA-256 of the test assembly. Do not tick from chat.

---

## 25. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §33–35, §40–43, §61, §68, **§70**, §72.8–12
- `D:\Prop\apps\fix-worker\Worker.cs`, `Program.cs`, `appsettings.json`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`, `FixSessionState.cs`, `KillSwitch.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\reports\swarm\20260818\A25_fix_session_spec.md` §8–9
- `D:\Prop\reports\swarm\20260818\A27_test_inventory.md` §7–8.2
- `D:\Prop\reports\swarm\20260818\A28_phases_gates.md` §70 extract
- `D:\Prop\reports\swarm\20260818\A42_clordid_idempotency.md`
- `D:\Prop\reports\swarm\20260818\A46_session_ownership.md`
- `D:\Prop\reports\swarm\20260818\A47_reconciliation_design.md`
- `D:\Prop\reports\swarm\20260818\A48_kill_switch.md`
- `D:\Prop\reports\swarm\20260818\A49_feature_flags.md`
- `D:\Prop\reports\swarm\20260818\A68_fix_simulator.md`
- `D:\Prop\reports\swarm\20260818\A70_execution_fsm.md`

---

*End of A101. Product source was not modified. Real orders remain disabled.*
