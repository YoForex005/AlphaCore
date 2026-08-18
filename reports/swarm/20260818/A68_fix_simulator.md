# A68 — Architecture §61: in-process FIX simulator

**Artifact:** `D:\Prop\reports\swarm\20260818\A68_fix_simulator.md`  
**Date:** 2026-08-18  
**Agent:** A68 (design only)  
**Authority:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §61 (lines 2298–2315), plus the session / ER / MD / unknown-state law in §§25–34, 41–44, 60, 62, 68–70  
**Binding siblings:** `A05_fix_ctrader_audit.md`, `A20_table_catalog.md`, `A25_fix_session_spec.md` §8, `A27_test_inventory.md` §7, `A31`–`A34`, `A36_ctrader_data_dictionary.md`, `A49_feature_flags.md`  
**Official RoE (re-read at implementation):** https://help.ctrader.com/fix/specification/  
**Product source:** **not modified**

This file is the implementer-binding design for the **in-process cServer stand-in**. It is not a coding task. It does not authorize a live `NewOrderSingle` against Pepperstone account `1369850`.

---

## 0. Verdict

Architecture §61 is one sentence and a seven-line checklist:

```text
Before using real NewOrderSingle:
  Build a FIX adapter test mode.
  parse recorded ExecutionReports
  replay MarketDataIncrementalRefresh
  simulate disconnects
  simulate duplicate ExecutionReports
  simulate partial fill
  simulate rejection
  simulate unknown-state disconnect
Do not use the real account as the first integration test.
```

**Measured state (2026-08-18):** there is **no in-process venue**. `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` is a **pipe-delimited string factory**. It cannot accept a `NewOrderSingle`, cannot drop a socket, cannot own a book, cannot replay a recorded tape through the execution state machine, and several of its generated messages disagree with the official RoE (see §2). `apps/fix-worker/Worker.cs` is still a 1 Hz heartbeat logger. There is no `tests/Fix` project.

Classification:

| Piece | Class |
|---|---|
| §61 adapter test mode | **MISSING** |
| `FixSimulationHarness` | **EXISTS_NEEDS_REFACTOR** — keep as a *fixture builder* after RoE corrections; do not treat as the venue |
| `FixMessageParser` | **EXISTS_NEEDS_REFACTOR** — checksum OK; last-wins `Dictionary` **cannot** parse MD repeating groups |
| `ExecutionOrderStateMachine` | **EXISTS** — usable for ER apply; does not own persistence or recovery |
| Production QuickFIX sessions | **MISSING** (package on disk is `QuickFix.Net 1.8.0`, not the pinned QuickFIXn 1.14.1 pair) |
| Live-account first test | **FORBIDDEN** |

**Done for §61** means: the same Application ports the live adapter will implement can be driven by an in-process venue that never opens a TCP connection to `*.c-trader.com`, and the seven checklist items have green tests on recorded / scripted tapes.

---

## 1. Purpose and non-goals

### 1.1 Purpose

Prove, without a Pepperstone socket:

1. QUOTE: Security List → numeric tag 55 → incremental MD → `destination_quotes` + stale-quote clock.
2. TRADE: persist-before-send → `35=D` → ExecutionReport lifecycle (new / partial / fill / reject / cancel).
3. Transport faults: disconnect after a possible send marks `ExecutionOrderStatus.ExecutionStateUnknown` / `SentAcknowledgementUnknown` and **does not** emit a second `35=D` for the same `ClOrdID`.
4. FAQ duplicate reports: a second identical `35=8` is persisted once and does not double-book qty.
5. Recovery: after unknown-state, only `35=H` / `35=AF` / `35=AN` may resolve the order; a replacement order (if any) is a **new** `ClOrdID`.

The simulator is the **first** integration test of the adapter. Live Logon is a later staging checklist (`A25` §3.6), not a unit default.

### 1.2 Non-goals

- A second FIX engine. Do **not** write a `TcpClient` + checksum acceptor. Official Spotware sample is a teaching aid (`A33`).
- A network loopback QuickFIX acceptor as the *primary* §61 venue. Optional later for dictionary/codec soak; not required to close §61.
- Live host/port, live password, or any DNS to `live-us-eqx-01.p.c-trader.com`.
- Pretending cTrader is an LP. Volume is **cTrader units** (RoE: max precision 0.01), not MT5 lots. Tag 55 is a **numeric instrument id**, not `"XAUUSD"`.
- Sharing one sequence counter between QUOTE and TRADE.
- Blind retry of `NewOrderSingle` on disconnect (`A25` §5.5).
- Mixing source MT5 ticks into destination quotes (`A45`). Simulator MD feeds `PriceSource.CTraderQuoteSession` only.
- Registering the simulator in `apps/fix-worker` composition.

### 1.3 Hard safety rules

1. `FixWorker` must refuse `VenueMode=InProcess` (that mode is tests only).
2. Tests must refuse `VenueMode=LiveQuickFix` unless an explicit, opt-in trait is set **and** the configured host is not a `*.c-trader.com` name. Default is InProcess.
3. Simulator Logon accepts **any** username/password. It must **never** be pointed at a live password store “to be realistic.”
4. `REAL_COPY_EXECUTION_ENABLED` still gates the **application** send function. The venue will accept `35=D` if the application sends it; the test of the flag is that the application does **not** send.
5. No fixture file may contain a real password, account cookie, or full production SenderCompID of account `1369850` with a live secret. Use `live.testbroker.1` / `553=1` / `554=unused`.
6. Simulator cannot resolve or connect outbound. Construction with a live host string is a test failure (`FixAdapterTestModeDoesNotHitVenueTests`).

---

## 2. What exists today (honest)

### 2.1 `FixSimulationHarness` — string factory, not a venue

Path: `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`

It builds checksummed `|` messages via `FixMessageParser.BuildFixMessage`. Useful as a **fixture writer**. Not a session, not a book, not a transport.

RoE defects that a later coding task must fix **before** treating its output as golden:

| Method | Defect vs official RoE (`A32`) |
|---|---|
| ER builders | Tag 55 default `"XAUUSD"` — cServer expects a **numeric** Spotware id. Official examples use `55=1` (EURUSD **on that sample broker**, not XAU). |
| ER builders | Missing `14 CumQty`, `151 LeavesQty`, `54 Side`, `38 OrderQty`, `721 PosMaintRptID`. Official New ER has `14=0\|151=10000\|721=101`. |
| ER builders | No `17 ExecID`. Official ER table also omits 17 — persist key must not assume it is always present (see §7.2). |
| Inbound header | Uses client-side `49=SENDER`, `57=TRADE`. Official **server** ER is `49=CSERVER\|50=TRADE\|56=<client>\|57=<echo of client 50>`. |
| `SimulateMarketDataSnapshot` | `35=X` is Incremental; Snapshot is `35=W`. Uses invented tags `1320`/`1321` instead of group `268/269/270`. |
| `SimulateLogonFail` | `35=3` + `371=reason`. Official failed Logon is **Logout `35=5`** with `58=InternalError: RET_INVALID_DATA`. |
| `SimulateDisconnect` | Emits a Heartbeat with `1128=text`. Disconnect is a **transport event**, not a FIX message. |
| `SimulateDuplicateExecutionReport` | Identity function. Does not assign or preserve an idempotency key. |
| `SimulateExecutionReport_UnknownState` | Uses `150=I` (Order Status). Unknown state is **absence of a terminal ER after a possible send**, not an ExecType. |

Keep the class. Correct the tags. Stop calling it “the simulator.”

### 2.2 `FixMessageParser` — checksum only, last-wins

Path: `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`

- Accepts `|` or can be fed SOH-normalized text.
- Validates tag 10 (ASCII sum mod 256, 3 digits).
- Builds 8 / 9 / 35 / body / 10 correctly enough for single-value tags.
- **`Parse` returns `IReadOnlyDictionary<int,string>`.** Repeating groups (`268` MD entries, `146` Security List symbols, two `269` on a snapshot) **collapse**. Official spot snapshot `268=2|269=0|270=1.06625|269=1|270=1.0663` would keep only the last `269`/`270`.

§61 MD replay **cannot** use this dictionary as the decode target. See §6.

### 2.3 Domain pieces the venue must drive (do not fork)

| Type | Path | Role for the simulator |
|---|---|---|
| `ExecutionOrderStatus` | `src/Domain/Enums/ExecutionOrderStatus.cs` | `NotSent` … `ExecutionStateUnknown` |
| `ExecutionReportInput` + `ExecutionOrderStateMachine` | `src/Domain/Execution/ExecutionOrderStateMachine.cs` | Pure apply. `MayRetryNewOrderSingle` only for `NotSent`/`Rejected`. `RequiresReconciliation` for sent-unknown + unknown. |
| `ClOrdIdFactory` | `src/Domain/Execution/ClOrdIdFactory.cs` | Persist-before-send id shape |
| `DestinationQuote` (risk record) | `src/Domain/Risk/RiskEngine.cs` | Quote cache output |
| `DestinationQuoteSnapshot` | `src/Domain/Entities/DestinationQuote.cs` | Persist shape (latest only, `A20`) |
| `FixSessionState` / `FixSessionStatus` / `FixSessionQualifier` | Domain | Two independent sessions |
| `CTraderFixOptions` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | Header + flags; add `VenueMode` later |
| `FixSessionOwnership` | `src/Fix.CTrader/Services/FixSessionOwnership.cs` | In-memory fence for harness tests |
| `RiskEngine` | Domain | Stale quote / stop / `AllowFixSend` |
| `ShadowCopyEngine` | Domain | Consumes destination quotes from MD replay |
| `CopyIntentExpiry` | Domain | TRADE-down must not fire expired intents (`§62`) |

`ExecutionOrderStateMachine.MapOrdStatus` already maps unknown `OrdStatus`/`ExecType` (including `I` and `C`) to `ExecutionStateUnknown` except `C` is not listed and falls through to unknown. Official `39=C` Expired should become an explicit status when Domain grows `Expired`; until then the simulator may emit `39=C` and tests must assert **current** machine behaviour (unknown), not invent a status.

### 2.4 Tables the harness persists into (when Infrastructure exists)

From `A20` (not all EF types exist yet):

```text
destination_symbols          -- 55 / 1007 / 1008 from recorded 35=y
destination_quotes           -- latest bid/ask per (venue, instrument)
fix_sessions                 -- independent QUOTE + TRADE seq
fix_session_events           -- logon, disconnect, reject
execution_intents            -- unique cl_ord_id; persist before send
fix_orders                   -- venue OrderID 37
fix_execution_reports        -- append; dedupe (see §7.2)
destination_positions        -- 721
execution_reconciliation_*   -- mass-status + positions after unknown/restart
```

The in-process venue **is not** the database. It is the **other side of the socket**. Tests use an in-memory or Testcontainers Postgres the same way production services will. Do not hide application persistence inside the simulator.

---

## 3. Shape: port + in-process venue (not a second engine)

```text
                    ┌─────────────────────────────────────┐
   tests/Fix        │  Scenario script (clock + faults)   │
                    └─────────────────┬───────────────────┘
                                      │
                    ┌─────────────────▼───────────────────┐
                    │  Application services (real code)   │
                    │  persist-before-send, SM, recon,    │
                    │  quote cache, risk, flags, lease    │
                    └─────────────────┬───────────────────┘
                                      │ ICTraderFixVenue
                 ┌────────────────────┴────────────────────┐
                 │                                         │
     ┌───────────▼──────────┐                 ┌────────────▼────────────┐
     │ CTraderFixSimulator  │   later only    │ QuickFixCTraderVenue    │
     │ (in-process, §61)    │                 │ (TLS initiator, Phase 4+)│
     │ no TcpClient         │                 │ never in §61 default    │
     └───────────┬──────────┘                 └─────────────────────────┘
                 │
     two logical sessions: QUOTE + TRADE
     independent MsgSeqNum, HB clock, book, subscriptions
```

### 3.1 Application port (to be added in a coding task)

Name: `TraderIntelligence.Application.Ports.Fix.ICTraderFixVenue`

The worker and tests depend on this port. They do **not** depend on QuickFIX types.

```text
ICTraderFixVenue
  VenueKind Kind                          -- InProcess | LiveQuickFix
  IFixSession Quote { get; }
  IFixSession Trade { get; }

IFixSession
  FixSessionQualifier Qualifier
  FixSessionStatus Status
  int InboundSeq
  int OutboundSeq
  DateTimeOffset? LastInboundAt
  DateTimeOffset? LastOutboundAt
  Task ConnectAsync(ct)                   -- InProcess: mark Connecting → LogonSent
  Task LogonAsync(ct)
  Task LogoutAsync(ct)
  Task DisconnectAsync(reason, ct)        -- transport drop; no FIX body required
  IObservable<FixInboundMessage> Inbound

IFixQuoteClient : uses Quote session
  Task<SecurityListResult> RequestSecurityListAsync(req, ct)
  Task SubscribeMarketDataAsync(MarketDataRequest, ct)
  Task UnsubscribeMarketDataAsync(mdReqId, ct)

IFixTradeClient : uses Trade session
  Task SubmitNewOrderSingleAsync(NewOrderSingleCommand, ct)   -- write only; ER comes on Inbound
  Task RequestOrderStatusAsync(clOrdId, ct)                   -- 35=H
  Task RequestMassStatusAsync(massStatusReqId, ct)            -- 35=AF, type=7
  Task RequestPositionsAsync(posReqId, posMaintRptId?, ct)    -- 35=AN
  Task CancelAsync(origClOrdId, newClOrdId, ct)               -- 35=F
  Task ReplaceAsync(...)                                      -- 35=G
```

Rules on the port:

- `SubmitNewOrderSingleAsync` returning successfully means **the venue accepted the write into its inbound queue** (InProcess) or **the engine reported a socket write** (live). It does **not** mean accepted-by-cServer. Application then sits in `SentAcknowledgementUnknown` until an ER arrives.
- If the TRADE session is disconnected, `Submit*` throws `FixSessionNotReadyException`. Application maps that to unknown **only if** persist already marked `sent_at`; if persist of the outbound row failed, do not send (`A25` §5.3).
- QUOTE `SubmitNewOrderSingleAsync` is **not on the interface**. A test that reaches a TRADE method on the QUOTE session is a defect.
- Events are typed (`ExecutionReportMessage`, `MarketDataIncrementalMessage`, …), not raw QuickFIX `Message`.

### 3.2 Why in-process, not loopback TCP

| Option | Verdict |
|---|---|
| In-process venue behind `ICTraderFixVenue` | **Required.** Deterministic, no ports, no TLS certs, no race with TestRequest timers, can inject disconnect between persist and write. |
| QuickFIX initiator ↔ QuickFIX acceptor on `127.0.0.1` | Optional Phase-7 codec soak. Adds FileStore, SSL, and timing noise. Does **not** replace the in-process venue. |
| Spotware `TcpClient` sample as “fake cServer” | **Forbidden.** |

InProcess still speaks **FIX field semantics** (tags, ExecType, MD groups). It does not need to emit SOH on the hot path. Tests that assert codec fidelity parse recorded **files** through `FixMessageParser` / (later) QuickFIX + `FIX44-CSERVER.xml`, then feed the **typed** message into the venue inbound bus **or** into the application handler. Two layers:

```text
recorded .fix file
    → codec (parse + checksum + repeating groups)
    → typed inbound DTO
    → application handler   (ER applier, quote cache)

application outbound DTO
    → venue.Submit*(dto)
    → script decides reply / drop / reject
    → typed inbound DTO
    → same application handler
```

Do not force every unit test through a SOH round-trip. Do force **at least one** test per message type through a recorded official example (checksum + group parse).

### 3.3 Two sessions inside the venue

Mirror `A25` §2. The simulator holds:

```text
SimQuoteSession
SimTradeSession
```

Never share: sequence counters, last in/out timestamps, heartbeat/test clocks, MD subscription map, order book, reconnect count, log scope.

Message routing:

| Inbound to venue (client → sim) | Legal session | Otherwise |
|---|---|---|
| `35=A/5/0/1/2/3/4` | both | — |
| `35=x` SecurityListRequest | both (TRADE preferred in RoE examples) | — |
| `35=V` MarketDataRequest | **QUOTE only** | `35=Y` or session reject |
| `35=D/F/G/H/AF/AN` | **TRADE only** | session reject; application defect if it got this far |

`ResetSeqNumFlag=Y` on Logon: both sim sequences restart at 1 (`A33`). Application order/position state is **not** reset (Postgres is authority).

Failed Logon (optional script): reply `35=5` with `58=…`, do not stay LoggedOn.

Successful Logon reply **swaps** Comp/Sub IDs (`A32` cheat-sheet). Fixture builder must emit server-side headers.

### 3.4 Placement

```text
src/Application/Ports/Fix/           ICTraderFixVenue, IFixClock, inbound DTOs
src/Fix.CTrader/Parsing/             FixFieldList parser (repeating groups)
src/Fix.CTrader/Simulation/          CTraderFixSimulator + scripts  [tests reference; FixWorker does not register]
src/Fix.CTrader/Testing/             FixSimulationHarness (corrected fixture builder)
tests/Fix/                           TraderIntelligence.Tests.Fix
tests/Fix/Fixtures/                  recorded .fix tapes (no secrets)
tests/Fix/Harness/                   A27 §7.2 test classes
```

`TraderIntelligence.Fix.CTrader` may contain `Simulation/` so the venue can reuse the parser and options types. `apps/fix-worker` **must not** call `AddCTraderFixSimulator()`. InternalsVisibleTo the test project is acceptable.

`tests/Unit` may keep parser / state-machine tests. §61 lifecycle tests live in `tests/Fix`.

---

## 4. Clock, scripts, and determinism

### 4.1 `IFixClock`

```text
DateTimeOffset UtcNow { get; }
void Advance(TimeSpan delta);
```

All `52 SendingTime`, `60 TransactTime`, quote `ReceivedAt`, heartbeat liveness, and `CopyIntentExpiry` use this clock. Default in tests: start `2026-01-15T12:00:00.000Z` (a weekday; avoid inventing weekend-market behaviour).

Stale-quote tests: replay one snapshot, `Advance(MaxQuoteAge + 1ms)`, assert `RiskEngine` reason `QUOTE_STALE` and no `35=D`.

### 4.2 Scenario script

A script is an ordered list of **hooks** on venue events. It is data, not a second state machine for production.

```text
FixScenario
  Name
  Seed                         -- instrument book + optional open orders/positions
  QuoteFault                   -- None | DisconnectAfterN | SilentDrop
  TradeFault                   -- None | DisconnectAfterSubmit | DisconnectBeforeEr
                               -- | DuplicateNextEr | PartialThenFill | RejectNext
                               -- | NeverAcknowledge | DelayEr(n ticks)
  RecordedTapes[]              -- files to inject on Logon or on subscribe
```

Hooks (implement as a small strategy interface, not a YAML interpreter in v1):

| Hook | When | Effect |
|---|---|---|
| `OnLogon(session)` | after inbound `35=A` | default: reply `35=A` swapped headers; script may Logout |
| `OnMarketDataRequest` | inbound `35=V` | play recorded `35=W` then `35=X` tape, or `35=Y` |
| `OnNewOrderSingle` | inbound `35=D` | see §8–§11 |
| `OnOrderStatus` | inbound `35=H` | emit `150=I` from sim book, or nothing (still unknown) |
| `OnMassStatus` | inbound `35=AF` | one ER per live order + `911`; empty book may `35=j` (RoE) |
| `OnRequestForPositions` | inbound `35=AN` | `35=AP` from sim positions |
| `OnDisconnect(session)` | test calls `DisconnectAsync` | Status=Disconnected; no more inbound |

v1 scripts are C# types in `tests/Fix/Scripts/`. No DSL file format.

### 4.3 Determinism rules

- Sequence numbers start at 1 per session after Logon with `141=Y`.
- `OrderID` (37) and `PosMaintRptID` (721) are allocated from monotonic integers **per venue instance**, starting at `101` to match official examples.
- Prices and qty in generated ERs come from the command + script, not `DateTime.UtcNow` or `Random`.
- Re-running the same test against a fresh venue + fresh DB is bit-identical on persisted `cl_ord_id` **if** `ClOrdIdFactory` is given a frozen clock and sequence.

---

## 5. Capability 1 — parse recorded ExecutionReports

### 5.1 Fixture format

Directory: `tests/Fix/Fixtures/er/`

```text
er_official_new_market_buy.fix          -- official RoE New ER (A32)
er_official_fill_market_buy.fix         -- official RoE Fill ER (A32)
er_partial_then_fill_1.fix              -- 39=1, 150=F
er_partial_then_fill_2.fix              -- 39=2, 150=F
er_reject.fix                           -- 150=8, 39=8, 103=0, 58=…
er_canceled.fix                         -- 150=4, 39=4
er_order_status.fix                     -- 150=I
er_duplicate_copy.txt                   -- same bytes as fill (FAQ fan-out)
```

File rules:

- One FIX message per file, or a `.tape` with one message per line.
- Separator is `|` (SOH shown as pipe), matching `FixMessageParser`.
- Tag 10 must be present and valid. Tests fail on a bad checksum rather than “helpfully” ignoring it (`A34` FAQ: invalid FIX is silently dropped **by cServer**; our parser must **not** silently drop — it must fail the fixture load so we do not test against corrupt tape).
- No password tags. Redact `554` if a future capture ever includes Logon.
- Tag 55 in recorded ERs is numeric. Human name, if needed, is in a sidecar comment (`# symbol=XAUUSD instrument=41`) **above** the message, never in tag 55.

Official New + Fill (quoted from `A32`, do not “fix” these to add XAUUSD):

```text
8=FIX.4.4|9=197|35=8|34=77|49=CSERVER|50=TRADE|52=20170117-10:02:14.720|56=live.theBroker.12345|57=any_string|11=876316397|14=0|37=101|38=10000|39=0|40=1|54=1|55=1|59=3|60=20170117-10:02:14.591|150=0|151=10000|721=101|10=149|

8=FIX.4.4|9=206|35=8|34=78|49=CSERVER|50=TRADE|52=20170117-10:02:15.045|56=live.theBroker.12345|57=any_string|6=1.0674|11=876316397|14=10000|32=10000|37=101|38=10000|39=2|40=1|54=1|55=1|59=3|60=20170117-10:02:14.963|150=F|151=0|721=101|10=077|
```

These two files are the **codec golden**. Application tests that need XAU use a **separate** recorded-or-built ER with a documented test instrument id (see §6.3).

### 5.2 `RecordedFixMessageStore`

```text
RecordedFixMessageStore.Load(path) → IReadOnlyList<FixFieldList>
FixFieldList : IReadOnlyList<FixField>     -- preserves duplicates / group order
FixField(int Tag, string Value)
```

`FixFieldList.Get(tag)` = last value (QuickFIX-like).  
`FixFieldList.GetAll(tag)` = all.  
`FixFieldList.Groups(leadingTag)` used by MD / Security List decoders.

Checksum validation happens here, once.

### 5.3 `RecordedExecutionReportParser`

Maps a `FixFieldList` with `35=8` to `ExecutionReportMessage`:

| Field | Tags | Required in parser |
|---|---|---|
| ClOrdId | 11 | no (RoE: optional; mass-status may omit on some rows — if missing, recon must still key on 37) |
| VenueOrderId | 37 | yes |
| ExecType | 150 | yes |
| OrdStatus | 39 | yes |
| SymbolId | 55 | no (numeric string) |
| Side | 54 | no |
| LastQty | 32 | no |
| LastPx | 31 (and/or AvgPx 6 on official fill) | no — official fill uses `6` not `31` |
| CumQty | 14 | no |
| LeavesQty | 151 | no |
| OrderQty | 38 | no |
| TransactTime | 60 | no |
| SendingTime | 52 | no |
| Text | 58 | no |
| OrdRejReason | 103 | no |
| PosMaintRptId | 721 | no |
| MassStatusReqId | 584 | no |
| TotNumReports | 911 | no |
| ExecId | 17 | no — may be absent (see §7.2) |
| AvgPx | 6 | no |

**Do not invent tag 31 if only 6 is present.** The applier uses `LastPx ?? AvgPx` for fill price when `150=F`.

Feed: `parser.Parse(fields) → ExecutionReportInput` (existing Domain record) **plus** the extra fields Domain does not yet hold (`VenueOrderId` is already there; add persist DTO in Application).

### 5.4 What “parse recorded ERs” must prove (`A27` `RecordedExecutionReportParseTests`)

1. Official New ER: `150=0`, `39=0`, `14=0`, `151=10000`, `721=101`, checksum OK.
2. Official Fill ER: `150=F`, `39=2`, `14=10000`, `32=10000`, `6=1.0674`, `151=0`.
3. Apply New then Fill through `ExecutionOrderStateMachine`: `Accepted` → `Filled`. Terminal `Filled` does not move to a non-fill (`SM` already freezes Filled/Rejected/Cancelled).
4. Persist path (when table exists): one `fix_execution_reports` row per message; `fix_orders.dest_order_id=101`; `destination_positions` id `101`.
5. Corrupt checksum → fixture load throws; handler is never called.
6. Repeating-group-free ER still round-trips through `BuildFixMessage` with a matching checksum.

---

## 6. Capability 2 — replay MarketDataIncrementalRefresh

### 6.1 Official MD shape (`A32`)

Subscribe (`35=V`) on **QUOTE** only:

```text
263=1  Snapshot+updates
264=1  Spot   (or 0 depth)
265=1  Incremental only
267=2  269=0 and 269=1
146=1  55=<numeric>
```

Snapshot (`35=W`) then incrementals (`35=X`).

Incremental group (per entry):

| Tag | Meaning | Notes |
|---|---|---|
| 279 | MDUpdateAction | `0` New, `2` Delete. **No Change (`1`) in RoE.** |
| 269 | MDEntryType | `0` Bid, `1` Offer |
| 278 | MDEntryID | required |
| 55 | Symbol | numeric, required |
| 270 | Px | required when 279=0 |
| 271 | Size | required when 279=0 (depth); may be absent on spot |

Spot snapshot official example has **no 262 on the snippet in A32 for W** wait — table says 262 required. The worked official spot snapshot in A32 **omits 262**. Parser: 262 optional on `W`/`X` if missing; subscribe correlation uses last `MDReqID` on that session.

`FixSimulationHarness.SimulateMarketDataSnapshot` is **not** a legal incremental. Replace it with:

- `BuildMarketDataSnapshotFullRefresh` → `35=W`, group `268/269/270`
- `BuildMarketDataIncrementalRefresh` → `35=X`, group `268/279/269/278/55/270/271`

### 6.2 Quote cache behaviour

`MarketDataIncrementalRefreshReplayer` is **not** a separate production service. It is the test driver that:

1. Logs the QUOTE session on.
2. Optionally injects a recorded Security List (XAU name → id).
3. Sends `35=V` for that id.
4. Plays `35=W` + `35=X` from `tests/Fix/Fixtures/md/`.
5. Pushes typed updates into the same `IDestinationQuoteCache` the worker will use.

Cache contract (latest-only, `A20`):

```text
destination_quotes unique (venue_id, instrument_id)
bid, ask, quote_received_at = clock.UtcNow (our receive)
venue_timestamp = 52 or 60 if present, else null
spread = ask - bid
```

Book rules for incrementals:

- Spot (`264=1`): treat each New bid/offer as **replace** the side. Delete (`279=2`) clears that side → quote becomes incomplete → risk sees `QUOTE_MISSING` / unhealthy, not a stale last print.
- Depth (`264=0`): maintain entries by `278`. Best bid = max px of bid entries; best ask = min px of offer entries. v1 tests may use spot only; depth is a later fixture.
- Unknown `55` (not in `destination_symbols`): do not upsert a quote; increment `fix_md_unknown_symbol_total`. Do not invent XAU.

`quote_age = now - quote_received_at`. QUOTE liveness is **quote age**, not Heartbeat (`A34` FAQ 2). Replayer must not emit Heartbeats unless a test asks for them.

### 6.3 Test instrument (do not hardcode Pepperstone XAU id)

Until a captured Pepperstone `35=y` exists, use a **test-only** triple:

```text
venue = inprocess-test
55 = 41
1007 = XAUUSD
1008 = 2
```

`41` is **not** a live Pepperstone id. Document that in the fixture header. Production discovery (`A25` §7.1) overwrites this when a real Security List is persisted. Tests that claim “Pepperstone XAU discovery” must use a recorded **Pepperstone** list with secrets stripped; until that capture exists, mark that test `Skip` with reason, do **not** pretend `41` is Pepperstone.

### 6.4 What MD replay must prove (`MarketDataIncrementalRefreshReplayTests`)

1. After snapshot bid=2399.50 / ask=2400.50, cache matches; spread=1.00; age=0.
2. Incremental New bid 2399.80 updates bid only.
3. `clock.Advance(MaxQuoteAge + 1ms)` → `RiskEngine` `QUOTE_STALE` → `AllowFixSend=false`.
4. QUOTE disconnect mid-tape: cache freezes at last receive time; age grows; no new live copy (`§62`).
5. `35=V` on TRADE session is rejected; cache unchanged.
6. Repeating-group parse of official `35=W` example (`268=2|269=0|270=1.06625|269=1|270=1.0663`) yields two entries, not one.
7. Shadow entry (`ShadowCopyEngine.SimulateEntry`) prices from the **replayed** quote, not from the source deal (`A45` / `A24`).

---

## 7. Capability 3 — duplicate ExecutionReports

### 7.1 What a duplicate is (FAQ, not a retry)

`A34` official FAQ:

> FIX API reports will be duplicated if you have multiple connections to the API open simultaneously. The server will send a copy of the FIX response to each active connection.

That is **fan-out**, not `35=2` resend, and not a second fill. The harness must be able to deliver **the same application body twice** on one TRADE session (simulating what a buggy dual-owner would see on one process, or what recon-after-dual-connect might ingest).

Two test modes:

| Mode | How | Why |
|---|---|---|
| `DuplicateNextEr` script | Venue emits the same typed ER twice (same fields, two inbound seq numbers) | Application idempotency |
| Dual-session script | Two `ICTraderFixVenue` TRADE clients, one sim fan-out | Ownership / lease tests (`A25` §4); **not** required to close the ER applier test |

Do **not** model the duplicate as a second `LastQty` with a new identity.

### 7.2 Idempotency key (ExecID may be missing)

`A20` wants `UNIQUE (venue_id, exec_id)`. Official cTrader ER table and official examples **do not include tag 17 ExecID**.

Binding persist rule:

```text
if tag 17 present and non-empty:
    exec_id = tag 17
else:
    exec_id = "syn:" + sha256hex(
        37 + "|" + 150 + "|" + 39 + "|" + 14 + "|" + 32 + "|" + (31|6) + "|" + 60 + "|" + 11
    ).substring(0, 32)
```

Prefix `syn:` makes synthetic keys visible in the dashboard and forbids colliding with a future real 17.

Duplicate = same `(venue_id, exec_id)`. Second insert is a no-op (or `ON CONFLICT DO NOTHING`). State machine is applied **once**. `CumQty` / destination position qty must not double.

If two ERs share ClOrdID but differ in `14`/`32`/`39`, they are **not** duplicates (partial then fill).

PossDup / resend (`43=Y`) with the same key is also a no-op.

### 7.3 What dup tests must prove (`DuplicateExecutionReportTests`)

1. Fill ER applied twice → one `fix_execution_reports` row, position qty = `LastQty` once, status `Filled`.
2. New ER then New ER (same 37/150/39/14) → still `Accepted`, one row.
3. Metric `fix_execution_reports_duplicate_total` increments on the second.
4. Dual TRADE owners is a **lease** test, not an excuse to double-fill.

---

## 8. Capability 4 — partial fill

### 8.1 Venue behaviour

On `35=D` with `OrdType=1` (market / IOC), default happy path is New + full fill (official pair). Partial script:

```text
OrderQty = Q
PartialThenFill:
  ER1: 150=0, 39=0, 14=0, 151=Q, 32 omitted          -- ack
  ER2: 150=F, 39=1, 32=Q1, 14=Q1, 151=Q-Q1, 31=px    -- partial
  ER3: 150=F, 39=2, 32=Q2, 14=Q,  151=0,    31=px    -- fill
  Q1 + Q2 = Q
```

IOC remainder: optional `PartialThenCancelLeaves` emits `150=4`, `39=4`, `151=0` after a partial (RoE: Cancelled on a partial means leaves will not fill).

Limit (`40=2`) may rest: New only, until the script fills or the test cancels.

Qty math is in **cTrader units**. Test orders use `38=10000` (official) or a documented unit size. `QuantityNormalizer` stays on the application side (lots → units) **before** `SubmitNew`.

### 8.2 State machine

Existing `Apply`:

```text
NotSent / SentAckUnknown + 39=0 → Accepted
Accepted + 39=1 → PartiallyFilled
PartiallyFilled + 39=2 → Filled
Filled + anything else → Filled (frozen)
```

`ExecutionReportInput` already has `LastQty` / `CumQty` / `LeavesQty`. Application persist must store them; the SM today only maps status. **Do not** put qty accumulation inside the simulator *and* the applier with different rules. Simulator emits consistent `14`/`151`/`32`; applier trusts venue numbers and alerts on `14 != previous 14 + 32` as a recon issue (`ReconciliationIssueType` already has `OrphanExecutionReport`; add `QtyInconsistent` when Domain grows it, or reuse an existing issue type — do not silently clamp).

### 8.3 What partial tests must prove (`PartialFillLifecycleTests`)

1. After ER2: status `PartiallyFilled`, dest position qty = Q1, `LeavesQty` = Q−Q1.
2. After ER3: `Filled`, dest qty = Q, leaves = 0, one `source_destination_links` row (or one position 721).
3. Copy mapping stays **one** destination position (`721` constant).
4. A third fill that would exceed `38` is **not** applied as extra qty; recon issue; status stays `Filled`.

---

## 9. Capability 5 — reject

Three reject classes. Do not collapse them.

| Class | MsgType | When | Application effect |
|---|---|---|---|
| Execution reject | `35=8` `150=8` `39=8` | Venue understood the order and refused it | Terminal `Rejected`. `MayRetryNewOrderSingle` is **true** only after policy says the reject is clean (no fill). New attempt = **new** ClOrdID. Same ClOrdID must never be resent. |
| Session reject | `35=3` | Tag/session rule (bad CompID, missing required, …) | Persist `fix_session_events`. If this was the NOS, treat as **unknown** (did cServer book it?). Do **not** map session reject to `Rejected` automatically. |
| Business reject | `35=j` | App-level refuse (e.g. empty mass-status) | If RefMsgType is `D`, treat as reject-of-send **only** when `RefSeqNum` matches the outbound NOS **and** no ER exists; else unknown. |
| MD reject | `35=Y` | Bad `55` string, bad depth | Quote path only. Official text: `Expected numeric symbolId, but got CS8260`. |

Failed Logon is **Logout `35=5`**, not `35=3` (`A32`).

`OrderRejectSimulator` default: after `35=D`, emit

```text
150=8  39=8  103=0  58=INSUFFICIENT_MARGIN  14=0  151=0
```

No position 721 (or 721 omitted). Intent status `Rejected`. Risk/audit row recorded. `AllowFixSend` on a retry is a **new** intent.

Also script: application sends `55=XAUUSD` → venue replies `35=8`/`35=j`/`35=Y` with the official numeric-id text. This locks the “never put the ticker in 55” rule.

### 9.1 What reject tests must prove (`OrderRejectLifecycleTests`)

1. ER reject is terminal; SM stays `Rejected` if a late fill arrives (frozen).
2. Same ClOrdID is not reused on a subsequent `SubmitNew`.
3. Session reject after write → `RequiresReconciliation` true; no automatic second `35=D`.
4. Risk reject **before** send (`AllowFixSend=false`) → venue `Submit` call count = 0 (`RiskRejectionBeforeFixSendTests`).
5. `REAL_COPY_EXECUTION_ENABLED=false` → venue `Submit` count = 0 even if TRADE is LoggedOn (`A49`).

---

## 10. Capability 6 — disconnect

Disconnect is a **transport event**.

```text
IFixSession.DisconnectAsync(reason)
```

Effects:

- Session `Status = Disconnected` (or `Error` if reason is fault).
- Inbound observable completes or emits a typed `SessionDisconnected` (not a Heartbeat).
- No further generated ER/MD until `ConnectAsync` + `LogonAsync` (`141=Y` → seq reset).
- QUOTE drop does **not** drop TRADE, and vice versa (`A25` §2.3).
- In-flight MD subscriptions are cleared (RoE: Logout cancels streaming prices; treat hard drop the same).
- Application: if a `35=D` write had returned (or `sent_at` is non-null), status → `SentAcknowledgementUnknown` / `ExecutionStateUnknown`. **No resend.**

Scripts:

| Script | Sequence |
|---|---|
| `DisconnectDuringHeartbeat` | Logon both; drop QUOTE; TRADE stays LoggedOn; quote age grows |
| `DisconnectAfterSubmit` | persist NOS → `Submit` returns → **no ER** → drop TRADE |
| `DisconnectMidErStream` | New ER delivered → drop before Fill |
| `DisconnectBeforeWrite` | persist `NotSent` → drop → `Submit` throws → stay `NotSent` (retry same row **only if** application can prove write did not happen; if unproven, promote to unknown — `A25` §5.3 fail-safe is unknown) |

`FixSimulationHarness.SimulateDisconnect` must not be used as the production-facing signal.

### 10.1 What disconnect tests must prove

1. Independent sessions: QUOTE down, TRADE seq unchanged.
2. Reconnect Logon does **not** flush a send queue (`A25` §5.6).
3. After `DisconnectAfterSubmit`: `RequiresReconciliation` true; `MayRetryNewOrderSingle` false; outbound `35=D` count stays 1.
4. TRADE down does not enqueue unbounded NOS (`TradeUnavailableDoesNotQueueUnlimitedBacklogTests`); intents expire via `CopyIntentExpiry`.

---

## 11. Capability 7 — unknown-state disconnect

This is §61’s last bullet and `A25` §5 / architecture §34. It is the reason the simulator exists.

### 11.1 Definition

```text
persist execution_intent + cl_ord_id     status=NotSent
send 35=D                                status=SentAcknowledgementUnknown
no terminal ER
transport dead or process crash
                                        status=ExecutionStateUnknown
```

`FixSimulationHarness.SimulateExecutionReport_UnknownState` (`150=I`) is **wrong**. `150=I` is the **recovery response**, not the fault.

### 11.2 Simulator support

`UnknownStateDisconnectSimulator`:

1. Accept `35=D` into the sim book **optionally** (script flag `VenueAcceptedButSilent` vs `VenueNeverSaw`).
2. Emit **zero** ERs.
3. Drop TRADE.

Two seeds for recovery:

| Seed | Sim book after drop | Legal application outcome |
|---|---|---|
| `VenueAcceptedButSilent` | order exists, maybe even filled internally | `35=H` / `35=AF` returns ER(s); adopt venue state; **no** second NOS |
| `VenueNeverSaw` | book empty, positions unchanged | mass-status empty / `35=j`; positions match DB; mark `not_on_venue`; **then** a **new** ClOrdID may be allocated |

The application **cannot** distinguish these without querying. That is the point of the recovery path.

### 11.3 Recovery sequence (application; venue only answers)

```text
EXECUTION_STATE_UNKNOWN
  → block additional 35=D for this intent
  → TRADE Logon + lease owned
  → 35=H by ClOrdID
  → if still unknown: 35=AF MassStatusReqType=7
  → consume 35=8 (150=I and/or 150=F/0/8/4)
  → 35=AN RequestForPositions
  → compare destination_positions + fix_orders
  → adopt | not_on_venue | BLOCKED_INCONSISTENT
```

Venue answers:

- `35=H` + known ClOrdID → one `150=I` echoing current `39`/`14`/`151`/`37`/`721`.
- `35=H` + unknown ClOrdID → RoE does not promise a shape; simulator emits **no** ER (application stays unknown) **or** a reject with text. Pick **no ER** as default so tests cannot cheat by mapping reject → `not_on_venue` without mass-status.
- `35=AF` → one ER per book order; `911=N`; `584` echoed. Empty book → `35=j`.
- `35=AN` → one `35=AP` per position; include `721`.

**Illegal in application (assert in tests):**

```text
catch (Disconnect) { SubmitNew(sameClOrdId); }
catch (Disconnect) { SubmitNew(newClOrdId); }   // before recon
```

### 11.4 What unknown-state tests must prove (`UnknownStateRecoveryTests`)

1. After silent drop: status unknown; `35=D` count = 1.
2. Seed `VenueAcceptedButSilent` + filled: recovery adopts `Filled`; dest position appears; no second ClOrdID.
3. Seed `VenueNeverSaw` + positions unchanged + mass-status complete: `not_on_venue`; a **new** intent/ClOrdID is allowed; old row stays unknown/not-on-venue, never rewritten as `NotSent`.
4. Mismatch (sim has a position DB does not): `BLOCKED_INCONSISTENT`; `READY_FOR_EXECUTION` false; no NOS (`ReconciliationBlocksExecutionWhileInconsistentTests`).
5. Startup: Logon without completed AF+AN stays off `READY_FOR_EXECUTION` (`A25` §8 item 10).

---

## 12. Simulator book (minimum state)

The venue keeps **just enough** to answer status/positions and to emit consistent ERs.

```text
SimInstrument
  SymbolId          -- tag 55 numeric string
  SymbolName        -- 1007
  Digits            -- 1008

SimMdBook
  InstrumentId
  Bid, Ask, BidId, AskId
  LastUpdate        -- clock

SimOrder
  ClOrdId           -- 11
  OrderId           -- 37
  SymbolId
  Side              -- 54
  OrdType           -- 40
  OrderQty          -- 38
  CumQty, LeavesQty
  OrdStatus         -- 39
  PosMaintRptId     -- 721
  LastPx
  Accepted          -- false if VenueNeverSaw

SimPosition
  PosMaintRptId     -- 721
  SymbolId, Side, Qty, AvgPx
```

Market `35=D` default: create order + position (hedge), New ER + Fill ER unless a fault script overrides.

`PosMaintRptID` on inbound NOS attaches to an existing position if present (hedge); else allocate new 721.

No need to model trailing SL / guaranteed SL (tags 1000–1006) in v1. Parser must **retain** those tags on recorded ERs so production code can persist them later; simulator-generated ERs may omit them.

---

## 13. Feature flags and composition

Simulator tests still run the **real** conjunction (`A25` §6.3 / `A49`):

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED          -- default false; tests that send flip it true *in memory*
TRADE = READY_FOR_EXECUTION
lease owned + fence current
risk healthy
STOP_NEW_EXECUTION = false
QUOTE usable if the order needs a price
intent persisted, cl_ord_id persisted, status = NotSent, not expired
```

`CTraderFixOptions` addition (coding task):

```text
VenueMode = InProcess | LiveQuickFix   -- tests set InProcess
InProcess does not use Host/Port/Password
```

`FixAdapterTestMode` helper:

```text
static ServiceProvider Create(FixScenario scenario)
  - IFixClock = FakeFixClock
  - ICTraderFixVenue = CTraderFixSimulator
  - in-memory lease
  - in-memory or EF InMemory / Testcontainers DB
  - REAL_COPY_EXECUTION_ENABLED as the test asks
  - assert venue.Kind == InProcess
  - assert no Dns / no Socket to c-trader.com (wrap IHostEnvironment / a Guard)
```

---

## 14. Test project map (closes A27 §7)

Project: `D:\Prop\tests\Fix\TraderIntelligence.Tests.Fix.csproj` (does not exist yet).

| A27 class | Simulator piece | Must prove |
|---|---|---|
| `FixAdapterTestModeDoesNotHitVenueTests` | `VenueKind.InProcess` | No live host/port; construction with `*.c-trader.com` throws |
| `RecordedExecutionReportParseTests` | §5 | Official ERs parse; SM + persist |
| `MarketDataIncrementalRefreshReplayTests` | §6 | Cache + stale clock + group parse |
| `DisconnectDuringHeartbeatTests` | §10 | Independent sessions |
| `DisconnectAfterNewOrderSingleTests` | §10 + §11 | Unknown; no second NOS |
| `DuplicateExecutionReportTests` | §7 | Idempotent fill |
| `PartialFillLifecycleTests` | §8 | 39=1 then 39=2 |
| `OrderRejectLifecycleTests` | §9 | Terminal reject; new ClOrdID only |
| `UnknownStateRecoveryTests` | §11 | H / AF / AN; adopt vs not_on_venue |
| `CancelReplaceInTestModeTests` | book + `35=F/G/9` | v1 stub may Skip until Phase 8 |
| `SecurityListXauDiscoveryTests` | recorded `35=y` | Persist 55/1007/1008; no hardcoded live id |
| `StartupReconciliationAfterSimulatedRestartTests` | AF + AN after new venue instance with **same seed book** | Ready or blocked |
| `ReconciliationBlocksExecutionWhileInconsistentTests` | mismatch seed | No NOS |
| `RiskRejectionBeforeFixSendTests` | RiskEngine + venue spy | Submit count 0 |
| `GlobalStopNewOrdersTests` | `KillSwitchMode.StopNewExecution` | Submit count 0 |
| `UniqueClOrdIdUnderRetryTests` | factory + recon | Replacement ≠ original |
| `QuoteUnavailableBlocksNewCopyTests` | MD down / stale | No send |
| `TradeUnavailableDoesNotQueueUnlimitedBacklogTests` | TRADE down | Expiry, not infinite queue |

Parser/SM unit tests stay in `tests/Unit` (`A27` §60). They do not replace this project.

---

## 15. Implementation sequence (coding tasks; not this file)

Order matches `A25` §12 / `A28` / `A30` without enabling live send:

1. **Codec:** `FixFieldList` parser (keep checksum); repeating-group helpers; golden official `35=8` / `35=W` / `35=y`.
2. **Correct `FixSimulationHarness`** tags (numeric 55, real MD groups, server headers, remove fake disconnect message).
3. **Ports:** `ICTraderFixVenue` + inbound DTOs + `IFixClock` in Application (empty product impl).
4. **`CTraderFixSimulator`:** Logon/Logout, two sessions, Security List seed, MD subscribe + replay, NOS New+Fill.
5. **`tests/Fix`** project + “does not hit venue” guard.
6. **Scripts:** disconnect, dup ER, partial, reject, unknown-state + H/AF/AN.
7. **Wire** real persist-before-send + SM + quote cache **when those services exist**. Until they exist, tests may drive the SM and an in-memory cache directly — still through the port, not through harness strings alone.
8. **Only then** live QuickFIX QUOTE (Phase 4). TRADE read (Phase 7). Flagged send (Phase 8) after §68/§70.

Do not wait for QuickFIX to start the simulator. The port is the seam.

---

## 16. Acceptance (Architecture §61 + §70 subset this design owns)

```text
[ ] Adapter test mode exists and cannot open *.c-trader.com
[ ] Recorded official New+Fill ERs parse (checksum + fields)
[ ] Recorded / built 35=X updates destination quote; stale clock rejects send
[ ] Disconnect after 35=D → ExecutionStateUnknown; outbound 35=D count = 1
[ ] Duplicate 35=8 → one persist row; qty not doubled
[ ] Partial 39=1 then 39=2 → one 721; qty = OrderQty
[ ] Reject 150=8 is terminal; next attempt new ClOrdID
[ ] Unknown-state recovery via 35=H / 35=AF / 35=AN; no blind resend
[ ] REAL_COPY_EXECUTION_ENABLED=false never calls Submit
[ ] QUOTE vs TRADE sequences are independent
[ ] Tag 55 in all generated application messages is numeric
[ ] No secrets in fixtures
[ ] FixWorker does not register the simulator
```

Until every box is green, **do not** treat `FixSimulationHarness` as §61 done, and **do not** enable `REAL_COPY_EXECUTION_ENABLED` in any environment.

---

## 17. Open risks (do not paper over)

1. **No ExecID in official ERs.** Persist unique key must work without tag 17 (`§7.2`). Confirm on first live capture whether cServer actually sends 17; if it does, prefer it.
2. **Parser last-wins.** Shipping MD replay on the current `Dictionary` parser will silently drop one side of the book. Codec work is a prerequisite, not polish.
3. **`150=I` is not unknown.** Existing harness method name will mislead the next implementer until renamed.
4. **Package mismatch.** `QuickFix.Net 1.8.0` on `Fix.CTrader.csproj` is not the QuickFIXn 1.14.1 + `FIX44-CSERVER.xml` pin (`A05` / `A36`). Simulator must not take a dependency on that old package.
5. **Layering.** `Fix.CTrader` already references Application. Ports should live in Application; simulator implements them. Do not put `ICTraderFixVenue` inside the test project only — production would then invent a second port.
6. **Empty mass-status → `35=j`.** Recovery must not treat business reject as “venue never saw this ClOrdID” without also checking positions.
7. **Official examples use `55=1` = EURUSD on the sample broker.** Copy-pasting them into an XAU test without remapping will “prove” the wrong instrument.

---

## 18. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §61, §33–34, §62, §70
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\reports\swarm\20260818\A05_fix_ctrader_audit.md` §5.5
- `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` (`destination_quotes`, `fix_execution_reports`)
- `D:\Prop\reports\swarm\20260818\A25_fix_session_spec.md` §5, §8
- `D:\Prop\reports\swarm\20260818\A27_test_inventory.md` §7
- `D:\Prop\reports\swarm\20260818\A32_ctrader_fix_specification.md` (official ER / MD / headers)
- `D:\Prop\reports\swarm\20260818\A33_ctrader_fix_send_recv.md` (seq reset, disconnect ≠ app message)
- `D:\Prop\reports\swarm\20260818\A34_ctrader_fix_faq.md` (duplicate reports, quote heartbeat absence)
- `D:\Prop\reports\swarm\20260818\A36_ctrader_data_dictionary.md` (tag 55 numeric, custom 1007/1008/721)
- `D:\Prop\reports\swarm\20260818\A49_feature_flags.md`
- https://help.ctrader.com/fix/specification/
- https://help.ctrader.com/fix/faqs/

---

*End of A68. Product source was not modified.*
