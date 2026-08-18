# B05 — Fix.CTrader gap: simulator + two session objects

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\B05_fix_gap.md` |
| Agent | B05 (Fix.CTrader audit + session/simulator spec) |
| Date | 2026-08-18 |
| Product source modified | **No** |
| Scope | `D:\Prop\src\Fix.CTrader` measured tree + implementer-binding spec for `CTraderQuoteSession`, `CTraderTradeSession`, and `CTraderFixSimulator` |
| Authority | Architecture v2 §§25–34, 41–44, 61, 68–70; official RoE https://help.ctrader.com/fix/specification/ |
| Binding siblings | A05 (stale), A08, A25, A27 §7, A32, A33, A34, A35, A36, A46, A49, A64, A68, A70, A72, A86 |
| Classification of `src/Fix.CTrader` | **EXISTS_NEEDS_REFACTOR** — four source files, zero sessions, zero venue |

This file is specification only. It does **not** authorize a live `NewOrderSingle` against Pepperstone account `1369850`. It does **not** implement types.

---

## 0. Verdict

Architecture §27 requires two **independent session objects**:

```text
CTraderQuoteSession
CTraderTradeSession
```

Architecture §61 requires an **in-process FIX adapter test mode** that can parse recorded ExecutionReports, replay incremental market data, and simulate disconnect / duplicate ER / partial / reject / unknown-state — **before** any socket to `*.c-trader.com`.

**Measured 2026-08-18:** neither session object exists. The class named `FixSimulationHarness` is a **pipe-delimited string factory**, not a venue. `apps/fix-worker` stamps `fix_sessions` rows as `ReadyForMarketData` / `LoggedOn` every 15 s **without a FIX connection**. Demo seed plants the same lie. The dashboard then reports QUOTE/TRADE “healthy.”

`A05_fix_ctrader_audit.md` is **stale**. It still describes `Class1.cs` and “zero types.” That file is gone. The project now has four `.cs` files and a **wrong** NuGet pin (`QuickFix.Net 1.8.0`). Do not treat A05’s “empty stub” as current, and do not treat the new files as sessions.

| Required object | On disk | Class |
|---|---|---|
| `CTraderQuoteSession` | **absent** | **MISSING** |
| `CTraderTradeSession` | **absent** | **MISSING** |
| `CTraderFixSimulator` (in-process venue, §61) | **absent** | **MISSING** |
| `ICTraderFixVenue` + `IFixSession` ports | **absent** | **MISSING** |
| `FixSimulationHarness` | `Testing/FixSimulationHarness.cs` | **EXISTS_NEEDS_REFACTOR** — fixture builder only |
| `FixMessageParser` | `Parsing/FixMessageParser.cs` | **EXISTS_NEEDS_REFACTOR** — last-wins `Dictionary`; cannot parse MD groups |
| `CTraderFixOptions` | `Configuration/CTraderFixOptions.cs` | **EXISTS_NEEDS_REFACTOR** — flags + two nested option bags; no binder, no `VenueMode` |
| `FixSessionOwnership` | `Services/FixSessionOwnership.cs` | **EXISTS_NEEDS_REFACTOR** — in-memory lock; not Redis+Postgres (A46) |
| QuickFIXn.Core 1.14.1 + QuickFIXn.FIX44 1.14.1 | **not referenced** | **MISSING** |
| `QuickFix.Net` 1.8.0 | csproj only; **zero** `using QuickFix` | **DEPRECATED / wrong package** |
| `FIX44-CSERVER.xml` / `quote.cfg` / `trade.cfg` | **absent** | **MISSING** |
| `tests/Fix` | **absent** | **MISSING** |

**Adapter completeness vs §§25–34, 41–44, 61: still ~0% of required behaviour.** Options + a checksum helper + a string factory are not a FIX adapter.

Live copy remains **safe by absence of a send path**, not by a gate. That is the correct *current* safety outcome. It is not an implemented control. The dashboard/seed “sessions are logged on” display is **UNSAFE if an operator trusts it**.

---

## 1. Measured tree (`D:\Prop\src\Fix.CTrader`)

Product source only (exclude `bin/`, `obj/`):

| Path | Bytes | Lines | SHA-256 | Role |
|---|---:|---:|---|---|
| `TraderIntelligence.Fix.CTrader.csproj` | 484 | 12 | `649D5E9B3D70DE1CEDA8AD3C19416A00F3EED8ACDA…` | net8.0 classlib; refs Domain + Application; `QuickFix.Net` 1.8.0 |
| `Configuration/CTraderFixOptions.cs` | 2344 | 55 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB36…` | Host, secret slots, Quote/Trade nested ports/headers, four flags |
| `Parsing/FixMessageParser.cs` | 6042 | 120 | `3E2C30C829673E045C0E7B21ABE7C371F09B57867C…` | Pipe parser/builder; tag 10 checksum; last-wins map |
| `Services/FixSessionOwnership.cs` | 4719 | 114 | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF…` | In-memory fencing lock + `ExecutionIntentsAllowed` |
| `Testing/FixSimulationHarness.cs` | 8970 | 185 | `103C2D1338FB2EBA718671BC189A1B15561CAB1A8D…` | Checksummed `|` message factory |

No `Sessions/`, no `Simulation/`, no `Spec/`, no `*.xml` dictionary, no `*.cfg`, no `IApplication`, no `SocketInitiator`.

Repo-wide `*.cs` search for `CTraderQuoteSession`, `CTraderTradeSession`, `CTraderFixSimulator`, `ICTraderFixVenue`, `using QuickFix`, `SocketInitiator`, `SessionSettings`: **zero product hits**. The only product token that names a quote session is the unused enum value `PriceSource.CTraderQuoteSession`.

### 1.1 Project / package

```xml
<PackageReference Include="QuickFix.Net" Version="1.8.0" />
```

Binding pin (`A35`): **`QuickFIXn.Core` 1.14.1 + `QuickFIXn.FIX44` 1.14.1**. `QuickFix.Net` 1.8.0 is an unofficial / obsolete id. Built `bin/Debug/net8.0/TraderIntelligence.Fix.CTrader.deps.json` lists **only** Domain + Application + FluentValidation — the 1.8.0 reference is unused at compile time because no type from it is referenced.

`Fix.CTrader` → `Application` is a **layering defect**. Ports must live in Application; the adapter implements them and should depend on Domain + QuickFIX only. Host (`apps/fix-worker`) composes Infrastructure + Fix.CTrader. Invert before adding engine types or Application will cycle.

### 1.2 `CTraderFixOptions` — keep, do not treat as sessions

Nested `QuoteFixOptions` / `TradeFixOptions` already split ports and headers (SSL 5211/5212, plain 5201/5202, `TargetSubId` `QUOTE`/`TRADE`). Flags exist on the type:

```text
QuoteEnabled = true
TradeSessionEnabled = true
RealCopyExecutionEnabled = false   -- correct default
UseSsl = true
HeartbeatIntervalSec = 30
MaxQuoteAgeMs = 5000
```

Gaps:

- No binder from env / `CTrader:*` in `apps/fix-worker` (worker reads only `CTrader:RealCopyExecutionEnabled`).
- `apps/api/appsettings.json` has a partial `CTrader` block (host, account, flags) that is **not** bound to this type.
- `TargetCompId` default `"cServer"` vs official RoE `"CSERVER"` — case must stay configurable; **no silent fold** (A25 §3, architecture §26).
- Hardcoded live SenderCompID `live.pepperstone.1369850` as a C# default is a footgun. Config default may document the issued form; tests must use `live.testbroker.1`.
- `MaxQuoteAgeMs = 5000` disagrees with `RiskLimits.MaxQuoteAge = 3s` (`A72`). One config key must win.
- No `VenueMode` (`InProcess` | `LiveQuickFix`).

### 1.3 `FixMessageParser` — codec seed, not MD decoder

- Accepts `|`; validates tag 10 (ASCII sum mod 256, 3 digits).
- Builder computes tags 9 and 10; places 8 then 35 then remaining tags **ascending**. Official RoE/FAQ require **RoE field order**. Do not use this builder for live outbound.
- `Parse` → `IReadOnlyDictionary<int,string>`: repeating groups collapse. Official spot snapshot `268=2|269=0|270=…|269=1|270=…` keeps only the last `269`/`270`. **§61 MD replay cannot use this dictionary.**
- Keep the class. Add `FixFieldList` (ordered, multi-value). Do not feed MD/SecurityList through `Dictionary`.

### 1.4 `FixSessionOwnership` — in-memory fence, not A46

Useful shape: `TryAcquire` / `Release` + monotonic `fencingToken` + `ExecutionIntentsAllowed = owned && reconciled`.

Defects vs A46:

- `InMemoryDistributedLockWithFencing` is process-local. Two `fix-worker` processes both acquire.
- No Redis key `ti:fix:lease:{session_key}`, no Postgres mint of the fence, no renew loop.
- Acquire does not fail closed when the lock provider is down.
- TRADE application send is not wired to this type at all.

Keep the interface idea. Production implementation is Infrastructure (`RedisTradeSessionLease` + Postgres fence). Do not put Redis in `Fix.CTrader`.

### 1.5 `FixSimulationHarness` — fixture writer, **not** the simulator

It builds checksummed `|` strings. It cannot accept a `NewOrderSingle`, cannot drop a socket, cannot own a book, cannot replay a tape through the execution state machine.

RoE defects (must fix **before** treating output as golden; details in A68 §2.1):

| Method | Defect |
|---|---|
| ER builders | Tag 55 default `"XAUUSD"` — cServer expects **numeric** Spotware id |
| ER builders | Missing 14/151/54/38/721; no 17 (17 may be absent — persist must not require it) |
| Inbound headers | Client-side `49=SENDER`, `57=TRADE`. Official **server** ER is `49=CSERVER\|50=TRADE\|56=<client>\|57=<echo of client 50>` |
| `SimulateMarketDataSnapshot` | `35=X` is Incremental; Snapshot is `35=W`. Invented tags 1320/1321 instead of group `268/269/270` |
| `SimulateLogonFail` | `35=3` + `371=reason`. Official failed Logon is **Logout `35=5`** + `58=…` |
| `SimulateDisconnect` | Heartbeat with `1128=text`. Disconnect is a **transport event**, not a FIX message |
| `SimulateExecutionReport_UnknownState` | Uses `150=I`. Unknown state is **absence of a terminal ER after a possible send** |
| `SimulateSecurityList` | Hardcodes `55=123456` — forbidden pattern for production mapping (`A86`) |

Keep the class as a **fixture builder** after tag corrections. Stop calling it the simulator.

### 1.6 Adjacent lies (not in `Fix.CTrader`, but they fake the two sessions)

`apps/fix-worker/Worker.cs` (every 15 s):

```csharp
quote.LastInboundAt = DateTimeOffset.UtcNow;
quote.Status = FixSessionStatus.ReadyForMarketData;
trade.LastInboundAt = DateTimeOffset.UtcNow;
trade.Status = FixSessionStatus.LoggedOn;   // both branches of `real` are LoggedOn
```

`DemoSeeder` inserts two `fix_sessions` rows already `ReadyForMarketData` / `LoggedOn` against `live-us-eqx-01.p.c-trader.com:5211/5212` with `SenderCompId=live.pepperstone.1369850`.

`EfDashboardQueries` treats those statuses as `QuoteHealthy` / `TradeHealthy`.

This is **display of a session that does not exist**. A later coding task must stop writing session status except from the two session objects (or the simulator driving the same persist path).

`apps/fix-worker` references `Fix.CTrader` and never uses a type from it.

`tests/Unit` and `tests/Integration` reference the project and have **zero** `*.cs` test sources. `tests/Fix` does not exist.

---

## 2. What “two session objects + simulator” means

One **port**, two **logical sessions**, two **implementations** of the venue:

```text
                    Application (persist-before-send, SM, recon, quote cache, flags, lease)
                                      │
                               ICTraderFixVenue
                 ┌────────────────────┴────────────────────┐
                 │                                         │
     ┌───────────▼──────────┐                 ┌────────────▼────────────┐
     │ CTraderFixSimulator  │                 │ QuickFixCTraderVenue    │
     │ (tests only, §61)    │                 │ (Phase 4+ live initiator)│
     └───────────┬──────────┘                 └────────────┬────────────┘
                 │                                         │
        SimQuoteSession                          CTraderQuoteSession
        SimTradeSession                          CTraderTradeSession
```

Rules:

1. Worker and tests depend on `ICTraderFixVenue`, **not** on QuickFIX types and **not** on `FixSimulationHarness` strings.
2. QUOTE and TRADE never share: TCP/SslStream, `SessionID`, MsgSeqNum in/out, heartbeat/TestRequest clock, last in/out timestamps, reconnect/backoff, FileStore/FileLog path, metrics series, log scope, MD subscription map, order book.
3. Simulator sessions are in-process stand-ins of the same two objects. They speak FIX **field semantics** (tags, ExecType, MD groups) without SOH on the hot path.
4. `apps/fix-worker` **must not** register `CTraderFixSimulator`. Construction with `VenueMode=InProcess` against a `*.c-trader.com` host is a test failure.
5. Do **not** write a `TcpClient` + checksum engine. Official Spotware sample is a teaching aid (`A33`). Do **not** use loopback QuickFIX acceptor as the primary §61 venue.

---

## 3. Shared port (Application — **MISSING**, add in a coding task)

Placement: `src/Application/Ports/Fix/`

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
  DateTimeOffset? LastHeartbeatAt         -- may stay null on QUOTE while quotes stream
  string? LastError
  Task ConnectAsync(ct)
  Task LogonAsync(ct)
  Task LogoutAsync(ct)
  Task DisconnectAsync(reason, ct)        -- transport drop; no FIX body
  IObservable<FixInboundMessage> Inbound

IFixQuoteClient                           -- uses Quote only
  Task<SecurityListResult> RequestSecurityListAsync(req, ct)
  Task SubscribeMarketDataAsync(MarketDataRequest, ct)
  Task UnsubscribeMarketDataAsync(mdReqId, ct)

IFixTradeClient                           -- uses Trade only
  Task SubmitNewOrderSingleAsync(NewOrderSingleCommand, ct)
  Task RequestOrderStatusAsync(clOrdId, ct)          -- 35=H
  Task RequestMassStatusAsync(massStatusReqId, ct)   -- 35=AF, type=7
  Task RequestPositionsAsync(posReqId, posMaintRptId?, ct)  -- 35=AN
  Task CancelAsync(origClOrdId, newClOrdId, ct)      -- 35=F
  Task ReplaceAsync(...)                             -- 35=G
```

Port law:

- `SubmitNewOrderSingleAsync` success = **write accepted** (sim inbound queue or live socket write). It is **not** cServer accept. Application then sits in `SentAcknowledgementUnknown` until an ER arrives (`ExecutionOrderStateMachine.AfterSendAttempt`).
- If TRADE is disconnected, `Submit*` throws `FixSessionNotReadyException`. Map to `ExecutionStateUnknown` **only if** persist already marked `sent_at`; if persist of the outbound row failed, do not send (A25 §5.3).
- `IFixQuoteClient` has **no** `SubmitNew`. A test that reaches a TRADE method on the QUOTE session is a defect.
- Events are typed (`ExecutionReportMessage`, `MarketDataIncrementalMessage`, `SecurityListMessage`, `PositionReportMessage`, `SessionDisconnected`, …), not QuickFIX `Message`.
- `IFixClock` (`UtcNow` + `Advance`) is injected. All 52/60, quote `ReceivedAt`, heartbeat liveness, `CopyIntentExpiry` use it.

Existing Domain types the sessions must **drive**, not fork:

| Type | Path | Use |
|---|---|---|
| `FixSessionQualifier` | `Domain/Enums/FixSessionQualifier.cs` | `Quote` / `Trade` |
| `FixSessionStatus` | `Domain/Enums/FixSessionStatus.cs` | persist + dashboard; see §4.3 mapping |
| `FixSessionState` | `Domain/Entities/FixSessionState.cs` | `fix_sessions` row (unique on Qualifier) |
| `ExecutionOrderStatus` + SM | Domain Execution | ER apply; `MayRetryNewOrderSingle` only NotSent/Rejected |
| `ClOrdIdFactory` | Domain Execution | persist-before-send id shape |
| `DestinationQuoteSnapshot` | Domain Entities | latest quote persist |
| `RiskEngine.DestinationQuote` | Domain Risk | cache output into Evaluate |
| `CTraderFixOptions` | Fix.CTrader Configuration | headers + flags; add `VenueMode` |

---

## 4. Two session objects (architecture §27)

### 4.1 Identity

| Object | Logical key | TLS port | Plain (not prod default) | Tag 57 | Tag 50 default |
|---|---|---|---|---|---|
| `CTraderQuoteSession` | `pepperstone-1369850-QUOTE` | 5211 | 5201 | `QUOTE` | **must be `QUOTE`** when 57=`QUOTE` |
| `CTraderTradeSession` | `pepperstone-1369850-TRADE` | 5212 | 5202 | `TRADE` | broker-issued / `any_string`; **not** a second qualifier |

Host (issued): `live-us-eqx-01.p.c-trader.com`.  
SenderCompID (issued): `live.pepperstone.1369850`.  
TargetCompID: **configurable** (`cServer` on the issued form, `CSERVER` in RoE). Never mutate case.  
Username 553 = numeric `1369850`. Password 554 = secret. Never log 554.

QuickFIX session key is `BeginString / SenderCompID / TargetCompID [ / SessionQualifier ]`. Map QuickFIX `SessionQualifier` onto **tag 57**, not tag 50, unless a proven diagnostic Logon says otherwise.

Two `SessionSettings` blocks. Two `FileStorePath`s. `ResetOnLogon=Y` (RoE: seq reset on establish). `SSLEnable=Y` production. Distinct log scopes `fix.quote` / `fix.trade`.

### 4.2 Placement (coding task, not this file)

```text
src/Application/Ports/Fix/                 ICTraderFixVenue, IFixSession, IFixQuoteClient, IFixTradeClient, IFixClock, DTOs
src/Fix.CTrader/Sessions/CTraderQuoteSession.cs
src/Fix.CTrader/Sessions/CTraderTradeSession.cs
src/Fix.CTrader/Sessions/CTraderSessionRuntime.cs   -- shared seq/HB/metrics struct; NOT a shared counter
src/Fix.CTrader/QuickFix/CTraderFixSettingsFactory.cs
src/Fix.CTrader/QuickFix/QuickFixCTraderVenue.cs    -- live ICTraderFixVenue; two IApplication or one keyed by SessionID
src/Fix.CTrader/Spec/FIX44-CSERVER.xml              -- official DD; hash + fetch date (A36)
src/Fix.CTrader/Spec/quote.cfg
src/Fix.CTrader/Spec/trade.cfg
src/Fix.CTrader/Simulation/CTraderFixSimulator.cs
src/Fix.CTrader/Simulation/SimQuoteSession.cs
src/Fix.CTrader/Simulation/SimTradeSession.cs
src/Fix.CTrader/Testing/FixSimulationHarness.cs     -- corrected fixture builder
src/Fix.CTrader/Parsing/FixFieldList.cs             -- repeating groups
apps/fix-worker/                                   hosts live venue only; lease + flags; no business logic
tests/Fix/                                         §61 suite (A27 §7)
```

A30 Increment 7 said “do not create `CTraderTradeSession.cs` in v1.” That is a **phase gate on live TRADE sockets**, not a license to omit the type. The **type and the simulator TRADE session must exist** so §61 can run. `CTRADER_FIX_TRADE_SESSION_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` keep the live socket and 35=D off.

### 4.3 Session FSM (each object)

Map to existing `FixSessionStatus` (do not invent a second enum):

| Runtime phase | `FixSessionStatus` | Notes |
|---|---|---|
| Feature flag off | `Disconnected` + `LastError=DISABLED` | No socket |
| Connecting TCP/TLS | `Connecting` | |
| Logon sent | `LogonSent` | outbound 35=A |
| Logon OK | `LoggedOn` | TRADE **stays here** until recon starts |
| QUOTE SecurityList + MD subscribe | `ReadyForMarketData` | only after 35=y persisted **and** 35=V accepted |
| TRADE startup recon in flight | `Reconciling` | block new 35=D |
| TRADE recon clean + flags | `ReadyForExecution` | **not** implied by Logon |
| Logout sent | `LogoutSent` | |
| Transport down | `Disconnected` | QUOTE drop does **not** drop TRADE |
| Unrecoverable / fenced / inconsistent | `Error` | persist `LastError`; TRADE may be `Error` while QUOTE quotes |

Independent rules (architecture §27, A25 §2.3):

1. A session may be DISABLED by its own flag without affecting the other TCP connection.
2. `ReadyForExecution` requires: lease owned, TRADE LoggedOn, startup recon clean (`35=AF` + `35=AN`), `REAL_COPY_EXECUTION_ENABLED` is **not** sufficient alone (A25 §6.3 conjunction).
3. QUOTE failure must not tear down TRADE (and vice versa). Failure **does** change risk: stale/missing quote rejects OPEN/INCREASE; TRADE down does not queue unbounded NOS (`CopyIntentExpiry`).
4. Sequence files are per session. A reset on QUOTE must not reset TRADE.
5. QUOTE liveness = **quote age**, not Heartbeat. FAQ: quotes may omit 35=0 while streaming (A34).
6. Invalid Logon → inbound `35=5` + Text 58. Persist the text. Do not retry with mutated headers in a tight loop.

### 4.4 `CTraderQuoteSession` — allowed messages

Outbound (client → cServer):

```text
A Logon, 5 Logout, 0 Heartbeat, 1 TestRequest
2 ResendRequest, 3 Reject, 4 SequenceReset
x SecurityListRequest
V MarketDataRequest
```

Inbound (cServer → client):

```text
A, 5, 0, 1, 2, 3, 4, j BusinessMessageReject
y SecurityList
W MarketDataSnapshotFullRefresh
X MarketDataIncrementalRefresh
Y MarketDataRequestReject
```

**Forbidden on QUOTE:** `35=D/F/G/H/AF/AN`. Implementation: those methods do not exist on `IFixQuoteClient`. If a mis-wired live initiator would send them, throw before the engine write.

On Logon OK:

```text
SecurityListRequest (35=x, 559=0)
  → persist destination_symbols (55 numeric, 1007 name, 1008 digits)
  → find canonical XAUUSD by name 1007, never by guessing 55
  → MarketDataRequest 263=1 264=1 265=1 267=2 (269=0 and 269=1) 146=1 55=<discovered id>
  → upsert destination_quotes (bid/ask, ReceivedAt=clock, VenueTimestamp=52/60)
```

Do **not** hardcode Pepperstone XAU id. Simulator seed may use test-only `55=41` / `1007=XAUUSD` / `1008=2` documented as **not** a live id (`A68` §6.3, `A86`).

Stale quote: `quote_age = now - ReceivedAt`. If `quote_age > MaxQuoteAge` (single bound config), `RiskEngine` reason `QUOTE_STALE`, `AllowFixSend=false`. QUOTE “LoggedOn” is not a substitute (`A72`).

On QUOTE disconnect: mark quotes unusable immediately (do not keep last bid as fresh). Age grows from last receive. Clear MD subscriptions (treat hard drop like Logout).

### 4.5 `CTraderTradeSession` — allowed messages

Outbound:

```text
A, 5, 0, 1, 2, 3, 4
x SecurityListRequest          -- allowed; prefer QUOTE for discovery
D NewOrderSingle               -- only if send conjunction true
F OrderCancelRequest
G OrderCancelReplaceRequest
H OrderStatusRequest
AF OrderMassStatusRequest      -- MassStatusReqType=7
AN RequestForPositions
```

Inbound:

```text
A, 5, 0, 1, 2, 3, 4, j
8 ExecutionReport
9 OrderCancelReject
y SecurityList
AP PositionReport
```

**Forbidden on TRADE:** `35=V` MarketDataRequest. Simulator / live session reject; cache unchanged.

Leadership (A25 §4.4, A46):

```text
acquire lease (new fencing token)
  → construct CTraderTradeSession / initiator
  → TLS + Logon
  → Status = Reconciling; block new executions
  → 35=AF then 35=AN
  → compare DB; persist recon run/issues
  → only if clean: ReadyForExecution
  → only then accept new execution intents
```

Never: Logon → drain `execution_intents` → 35=D.

Disconnect after a possible send: `AfterDisconnectWithUnknownAck` → `ExecutionStateUnknown`. **No second 35=D** (same or new ClOrdID) until recon (`A25` §5, `A70`). Reconnect Logon **does not flush a send queue**.

### 4.6 Header construction (both objects)

Configurable per session (A25 §3.4). Engine (QuickFIX), not application code, computes tags 9 and 10.

Logon body: `98=0`, `108=<HeartBtInt>`, `141=Y`, `553=<numeric login>`, `554=<password>`.

Successful Logon **swaps** Comp/Sub IDs (official pair, A32). Inbound `50` on a server message carries the **session qualifier**; inbound `57` echoes the client originator. Do not classify inbound sessions with the client-side 50/57 meaning.

Diagnostic Logon gate (A25 §3.6) must be recorded on `fix_session_events` for **each** object before any application message in a live environment. Simulator Logon does not satisfy the live gate.

### 4.7 Persist + metrics (both objects)

Each object owns writes to its `fix_sessions` row (`FixSessionState`, unique Qualifier). Fields: host, port, Comp/Sub IDs as **sent**, inbound/outbound seq, last in/out, reconnect count, last error, `OwnerHeld` / `OwnerInstance` for TRADE.

Do **not** let `Worker.cs` or `DemoSeeder` invent `LoggedOn` / `ReadyForMarketData`. Seed status = `Disconnected`. Dashboard `FixSessionDto` reads these rows; `ExecutionEnabled` is the send conjunction, default false.

Per-session metrics (architecture §58):

```text
fix_quote_connected / fix_trade_connected
fix_logon_failures / fix_reconnects
fix_inbound_messages_total / fix_outbound_messages_total
fix_rejects_total / fix_business_rejects_total
fix_execution_reports_total / fix_execution_reports_duplicate_total
fix_unknown_execution_states
fix_lease_held / fix_lease_lost / fix_fenced_sends_total
```

Never log tag 554. Redact 553/554 at the sink (`A76`).

### 4.8 Package + dictionary (live objects only)

Replace `QuickFix.Net` 1.8.0 with:

```xml
<PackageReference Include="QuickFIXn.Core" Version="1.14.1" />
<PackageReference Include="QuickFIXn.FIX44" Version="1.14.1" />
```

Load **`FIX44-CSERVER.xml`** (A36), not stock `FIX44.xml`. Custom tags 721, 1000–1008; tag 55 is Long. Simulator **must not** take a dependency on the old 1.8.0 package.

Two `IApplication` instances (or one class keyed by `SessionID`). Prefer two instances so a bug cannot share sequence state.

---

## 5. Simulator specification (`CTraderFixSimulator`)

This section is the implementer contract for §61. It aligns with A68 and names the **two session objects inside the venue**. A68 remains the long-form script catalog; this file is the gap + object spec.

### 5.1 Role

In-process cServer stand-in. Same `ICTraderFixVenue` the live adapter will implement. Never opens a TCP connection. First integration test of the adapter.

Proves, without Pepperstone:

1. QUOTE: Security List → numeric 55 → incremental MD → `destination_quotes` + stale-quote clock.
2. TRADE: persist-before-send → 35=D → ER lifecycle (new / partial / fill / reject / cancel).
3. Transport fault after write → `ExecutionStateUnknown`; outbound 35=D count stays 1.
4. Duplicate 35=8 → one persist row; qty not doubled (`A70` fingerprint; tag 17 may be absent).
5. Recovery only via 35=H / 35=AF / 35=AN; replacement order (if any) is a **new** ClOrdID.

### 5.2 Two sessions inside the venue

```text
CTraderFixSimulator
  SimQuoteSession     -- implements IFixSession + IFixQuoteClient surface
  SimTradeSession     -- implements IFixSession + IFixTradeClient surface
```

Never share seq, clocks, book, subscriptions, reconnect count, log scope.

Routing (inbound to venue = client → sim):

| MsgType | Legal session | Otherwise |
|---|---|---|
| A / 5 / 0 / 1 / 2 / 3 / 4 | both | — |
| x SecurityListRequest | both (TRADE preferred in official examples) | — |
| V MarketDataRequest | **QUOTE only** | 35=Y or session reject |
| D / F / G / H / AF / AN | **TRADE only** | session reject; application defect if it got this far |

`141=Y` on Logon: both sim sequences restart at 1. Application order/position state is **not** reset (Postgres is authority).

Successful Logon reply **swaps** Comp/Sub IDs. Failed Logon (optional script): `35=5` + `58=…`, do not stay LoggedOn.

`DisconnectAsync` is a transport event: `Status=Disconnected`, inbound completes or emits `SessionDisconnected`, no further ER/MD until Connect+Logon. QUOTE drop does not drop TRADE.

### 5.3 Minimum book

```text
SimInstrument   55 / 1007 / 1008
SimMdBook       bid/ask + entry ids + LastUpdate=clock
SimOrder        11, 37, 55, 54, 40, 38, 14, 151, 39, 721, LastPx, Accepted
SimPosition     721, 55, Side, Qty, AvgPx
```

Market 35=D default: create order + hedge position, New ER + Fill ER unless a fault script overrides. `PosMaintRptID` on inbound NOS attaches if present; else allocate new 721. Allocators start at `101` to match official examples.

Test instrument (until a stripped Pepperstone 35=y exists):

```text
venue = inprocess-test
55 = 41
1007 = XAUUSD
1008 = 2
```

`41` is **not** a live Pepperstone id. Tests that claim Pepperstone discovery stay `Skip` until a captured list exists.

### 5.4 Scripts (C# types in `tests/Fix/Scripts/`, not a DSL)

| Script | Session | Effect |
|---|---|---|
| default Logon | both | reply 35=A swapped headers |
| `OnMarketDataRequest` | QUOTE | play recorded 35=W then 35=X, or 35=Y |
| `PartialThenFill` | TRADE | 39=0 then 39=1 then 39=2; 14/151/32 consistent |
| `RejectNext` | TRADE | 150=8 39=8 103=0 58=…; no 721 |
| `DuplicateNextEr` | TRADE | same application body twice (two inbound seq) |
| `DisconnectAfterSubmit` | TRADE | Submit returns; zero ER; drop TRADE |
| `VenueAcceptedButSilent` | TRADE | book has the order; drop; 35=H/AF will adopt |
| `VenueNeverSaw` | TRADE | book empty; drop; mass-status empty/35=j; then new ClOrdID allowed |
| `DisconnectDuringHeartbeat` | QUOTE | drop QUOTE; TRADE stays LoggedOn; quote age grows |

`150=I` is a **recovery response**, not the unknown-state fault.

### 5.5 Codec path

```text
recorded .fix file
  → FixFieldList (checksum + repeating groups)
  → typed inbound DTO
  → same application handler the live session will call
```

Do not force every unit test through SOH. Force **at least one** test per message type through a recorded official example.

Official New + Fill (A32; do not rewrite 55 to XAUUSD):

```text
8=FIX.4.4|9=197|35=8|34=77|49=CSERVER|50=TRADE|52=20170117-10:02:14.720|56=live.theBroker.12345|57=any_string|11=876316397|14=0|37=101|38=10000|39=0|40=1|54=1|55=1|59=3|60=20170117-10:02:14.591|150=0|151=10000|721=101|10=149|

8=FIX.4.4|9=206|35=8|34=78|49=CSERVER|50=TRADE|52=20170117-10:02:15.045|56=live.theBroker.12345|57=any_string|6=1.0674|11=876316397|14=10000|32=10000|37=101|38=10000|39=2|40=1|54=1|55=1|59=3|60=20170117-10:02:14.963|150=F|151=0|721=101|10=077|
```

`55=1` on those fixtures is **EURUSD on the sample broker**. XAU tests use a separate tape with documented test id `41`.

ExecID may be missing. Persist unique key (`A68` §7.2 / `A70`): tag 17 if present, else `syn:` + sha256 of `37|150|39|14|32|(31|6)|60|11`.

### 5.6 Feature flags still apply

The venue will accept 35=D if the application sends it. Tests of `REAL_COPY_EXECUTION_ENABLED=false` assert **Submit count = 0**. Simulator Logon accepts any username/password and must **never** read the live password store.

`FixAdapterTestMode` composition:

```text
IFixClock = FakeFixClock (start 2026-01-15T12:00:00.000Z)
ICTraderFixVenue = CTraderFixSimulator
in-memory lease (existing FixSessionOwnership.InMemory… is OK for tests)
in-memory or Testcontainers DB
assert Kind == InProcess
assert no Dns/Socket to c-trader.com
```

### 5.7 Test project (closes A27 §7 — still **MISSING**)

`D:\Prop\tests\Fix\TraderIntelligence.Tests.Fix.csproj` does not exist.

Required classes (names locked by A27/A68):

```text
FixAdapterTestModeDoesNotHitVenueTests
RecordedExecutionReportParseTests
MarketDataIncrementalRefreshReplayTests
DisconnectDuringHeartbeatTests
DisconnectAfterNewOrderSingleTests
DuplicateExecutionReportTests
PartialFillLifecycleTests
OrderRejectLifecycleTests
UnknownStateRecoveryTests
QuoteAndTradeSessionIsolationTests      -- two objects; independent seq
SecurityListXauDiscoveryTests
StartupReconciliationAfterSimulatedRestartTests
ReconciliationBlocksExecutionWhileInconsistentTests
RiskRejectionBeforeFixSendTests
GlobalStopNewOrdersTests
UniqueClOrdIdUnderRetryTests
QuoteUnavailableBlocksNewCopyTests
TradeUnavailableDoesNotQueueUnlimitedBacklogTests
```

Plus unit (may live in `tests/Unit` once sources exist): header case (`cServer` not folded), QUOTE SenderSubID=`QUOTE` when 57=`QUOTE`, NOS refused on QUOTE, NOS refused when flag false, second instance cannot acquire TRADE lease, fenced token cannot send, password absent from logs.

---

## 6. Send conjunction (TRADE session + simulator must both honour)

All true immediately before a 35=D write (A25 §6.3 / A49):

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED            -- default false
TRADE Status == ReadyForExecution
lease owned + fencing token current
risk healthy (AllowFixSend)
STOP_NEW_EXECUTION == false
QUOTE usable if the order needs a price
  (QUOTE enabled AND quote_age <= MaxQuoteAge AND instrument mapped)
execution_intent persisted
cl_ord_id persisted
status == NotSent
intent not expired (CopyIntentExpiry)
```

If any check fails: do not call `Submit*`. Persist the decision. Fail closed.

`EMERGENCY_FLATTEN` may send reducing orders when `REAL_COPY_EXECUTION_ENABLED` is false, only if TRADE is logged on, lease owned, flatten authorized — still persist-before-send + unknown-state rules. Out of v1 simulator happy-path; do not block §61 on flatten.

---

## 7. Gap matrix (honest)

| Capability | Required | Measured | Class |
|---|---|---|---|
| Two independent session **types** | §27 | 0 / 2 | **MISSING** |
| Two independent sequence stores | §27 | 0 / 2 | **MISSING** |
| QuickFIX initiator + RoE dictionary | §1.8 / A35 / A36 | wrong unused package | **MISSING** |
| Configurable 49/50/56/57, no case fold | §26 | options fields only | **EXISTS_NEEDS_REFACTOR** |
| QUOTE SecurityList + MD → cache | §30–31 | harness fake tags; no cache writer | **MISSING** |
| TRADE recon AF+AN before ready | §42 | worker stamps LoggedOn | **MISSING** (and **misleading**) |
| Persist-before-send + unknown recovery | §33–34 | SM helpers exist; no adapter | Domain **EXISTS**; adapter **MISSING** |
| Single-active TRADE lease | §28 / A46 | in-memory only, unused | **EXISTS_NEEDS_REFACTOR** |
| In-process venue | §61 | string factory | **MISSING** |
| Repeating-group parser | §61 MD / A86 | last-wins Dictionary | **EXISTS_NEEDS_REFACTOR** |
| Feature-flag send gate | §41 / A49 | option default false; no send path | **VACUOUS / MISSING** |
| `tests/Fix` | §61 / A27 | folder absent | **MISSING** |
| Worker uses Fix.CTrader types | host | project ref unused; fakes health | **EXISTS_NEEDS_REFACTOR** |
| Dashboard session health | §52 | seeded + stamped lie | **UNSAFE if trusted** |

---

## 8. Implementation sequence (coding tasks; not this file)

Order matches A25 §12 / A30 / A68 §15 without enabling live send:

1. **Ports** in Application: `ICTraderFixVenue`, `IFixSession`, quote/trade clients, `IFixClock`, inbound DTOs.
2. **Codec:** `FixFieldList`; golden official 35=8 / 35=W / 35=y; keep checksum.
3. **Correct `FixSimulationHarness`** tags (numeric 55, real MD groups, server headers, remove fake disconnect message, rename UnknownState).
4. **`CTraderFixSimulator` + `SimQuoteSession` + `SimTradeSession`.** Logon/Logout, independent seq, Security List seed, MD subscribe + replay, NOS New+Fill, scripts in §5.4.
5. **`tests/Fix`** + “does not hit venue” guard + isolation test (QUOTE seq increment does not move TRADE).
6. **Stop the lie:** seeder status=`Disconnected`; worker must not stamp LoggedOn/ReadyForMarketData. Persist only from session objects.
7. **Replace package** with QuickFIXn 1.14.1 pair + pin `FIX44-CSERVER.xml`. Invert `Fix.CTrader` → Application dependency (ports already in Application).
8. **`CTraderQuoteSession` / `CTraderTradeSession`** live implementations behind `QuickFixCTraderVenue`. Diagnostic Logon-only first.
9. Wire persist-before-send + SM + quote cache + A46 lease when those services exist. Until then, tests may drive SM + in-memory cache **through the port**.
10. Phase 4 live QUOTE. Phase 7 TRADE read/recon. Phase 8 flagged send only after §68/§70 boxes that this adapter owns are green.

Do not wait for QuickFIX to start the simulator. The port is the seam.

---

## 9. Acceptance (this document owns)

```text
[ ] ICTraderFixVenue has Quote and Trade IFixSession properties
[ ] CTraderQuoteSession and CTraderTradeSession exist as types (live or sim)
[ ] SimQuoteSession / SimTradeSession do not share MsgSeqNum
[ ] NOS is not callable on the quote client
[ ] 35=V on TRADE is rejected; cache unchanged
[ ] Adapter test mode cannot open *.c-trader.com
[ ] Recorded official New+Fill ERs parse (checksum + fields)
[ ] 35=W repeating group yields two MD entries, not one
[ ] Disconnect after 35=D → ExecutionStateUnknown; outbound 35=D count = 1
[ ] Duplicate 35=8 → one persist row; qty not doubled
[ ] Partial 39=1 then 39=2 → one 721; qty = OrderQty
[ ] Reject 150=8 is terminal; next attempt new ClOrdID
[ ] Unknown-state recovery via 35=H / 35=AF / 35=AN; no blind resend
[ ] REAL_COPY_EXECUTION_ENABLED=false never calls Submit
[ ] Tag 55 in generated application messages is numeric
[ ] No secrets in fixtures; password never logged
[ ] FixWorker does not register the simulator
[ ] DemoSeeder / Worker do not mark sessions LoggedOn without a session object
[ ] QuickFix.Net 1.8.0 is not the production engine pin
```

Until every box is green, **do not** treat `FixSimulationHarness` as §61 done, **do not** treat `fix_sessions.Status` as connectivity, and **do not** enable `REAL_COPY_EXECUTION_ENABLED` in any environment.

---

## 10. Open risks (do not paper over)

1. **A05 is stale.** Implementers who start from A05 will recreate `Class1` history and miss the four files and the wrong package.
2. **Dashboard/seed health is a lie.** Operators can believe QUOTE/TRADE are up. Fix persist-from-session before any live Logon.
3. **Header case is unresolved** until diagnostic Logon (`cServer` vs `CSERVER`). Do not pick one in C# and ship.
4. **Parser last-wins** will silently one-side the book if MD replay ships on `Dictionary`.
5. **`150=I` named “UnknownState” in the harness** will mislead the next coder. Unknown is a transport/recovery condition.
6. **Package mismatch.** Simulator must not depend on `QuickFix.Net` 1.8.0.
7. **Layering.** Ports in Application; adapter implements. Do not put `ICTraderFixVenue` only in the test project.
8. **Two MaxQuoteAge defaults** (5 s options vs 3 s risk). Bind one key (`A72`).
9. **Official examples use `55=1` = EURUSD on the sample broker.** Copy-paste into an XAU test proves the wrong instrument.
10. Live account `1369850` is real money. Simulator first. Diagnostic Logon is allowed; diagnostic 35=D is not.

---

## 11. Sources

- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\bin\Debug\net8.0\TraderIntelligence.Fix.CTrader.deps.json`
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\src\Domain\Enums\FixSessionQualifier.cs`
- `D:\Prop\src\Domain\Enums\FixSessionStatus.cs`
- `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§26–28, 61
- `D:\Prop\reports\swarm\20260818\A05_fix_ctrader_audit.md` (stale snapshot)
- `D:\Prop\reports\swarm\20260818\A25_fix_session_spec.md`
- `D:\Prop\reports\swarm\20260818\A27_test_inventory.md` §7
- `D:\Prop\reports\swarm\20260818\A32_ctrader_fix_specification.md`
- `D:\Prop\reports\swarm\20260818\A35_quickfixn_packages.md`
- `D:\Prop\reports\swarm\20260818\A36_ctrader_data_dictionary.md`
- `D:\Prop\reports\swarm\20260818\A46_session_ownership.md`
- `D:\Prop\reports\swarm\20260818\A49_feature_flags.md`
- `D:\Prop\reports\swarm\20260818\A64_worker_pipelines.md` §9.3–9.4
- `D:\Prop\reports\swarm\20260818\A68_fix_simulator.md`
- `D:\Prop\reports\swarm\20260818\A70_execution_fsm.md`
- `D:\Prop\reports\swarm\20260818\A72_quote_guards.md`
- `D:\Prop\reports\swarm\20260818\A86_instrument_discovery.md`
- https://help.ctrader.com/fix/specification/
- https://help.ctrader.com/fix/faqs/
- https://help.ctrader.com/fix/FIX44-CSERVER.xml

---

*End of B05. Product source was not modified.*
