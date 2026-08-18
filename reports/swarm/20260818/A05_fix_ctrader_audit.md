# A05 — `src/Fix.CTrader` senior-engineer audit

**Date:** 2026-08-18  
**Auditor:** Grok Build subagent A05  
**Scope:** `D:\Prop\src\Fix.CTrader` vs architecture §§25–34, 41–44, 61  
**Product source modified:** none  
**Verdict:** **FAIL / MISSING.** Scaffold only. No FIX engine, no sessions, no types.

---

## 1. Executive verdict

`TraderIntelligence.Fix.CTrader` is an empty `net8.0` class-library stub. The only source file is a leftover `Class1`. There is **no QuickFIX/n package**, **no cTrader Rules-of-Engagement dictionary**, **no QUOTE session**, **no TRADE session**, **no SSL initiator**, **no ownership lease**, **no ClOrdID**, **no execution state machine**, and **no FIX simulator**.

Official need (architecture §1.7–1.8 + cTrader RoE + FAQ):

| Official / architecture rule | Measured state |
|---|---|
| Two **independent** QUOTE and TRADE FIX 4.4 sessions (separate TCP/SSL ports, separate sequence/heartbeat/reconnect state) | **MISSING** |
| Prefer **QuickFIX/n** with a cTrader-specific dictionary/config | **MISSING** — zero NuGet packages |
| **Do not** write a FIX engine from raw `TcpClient` | **No `TcpClient` in product source** (compliant only by absence; official Spotware sample is explicitly *not* an engine) |
| Persist-before-send unique `ClOrdID`; never blind-retry after disconnect | **MISSING** |
| Single-active TRADE ownership (duplicate reports if two connections) | **MISSING** |
| Simulator before any live `NewOrderSingle` | **MISSING** |

Classification (architecture §73.B):

| Component | Class |
|---|---|
| `src/Fix.CTrader` project in solution | **EXISTS_NEEDS_REFACTOR** (empty scaffold) |
| All §25–34 / 41–44 / 61 capabilities | **MISSING** |
| Raw `TcpClient` engine | **ABSENT** (good) — do not add one |
| Live-account safety | **UNSAFE if anyone wires this stub to production** — `FixWorker` is a 1 Hz heartbeat logger with no flags and no adapter |

**Measured implementation of the FIX adapter: 0%.** Do not claim QUOTE/TRADE connectivity, reconciliation, or execution readiness.

---

## 2. What exists on disk (evidence)

### 2.1 Tree

```
D:\Prop\src\Fix.CTrader\
  Class1.cs
  TraderIntelligence.Fix.CTrader.csproj
  obj\          (stale restore only; no successful build output)
```

No `bin\`, no `*.cfg`, no `*.xml` data dictionary, no `Sessions\`, no `QuickFix\`, no tests under `tests/Fix`.

### 2.2 Entire product source

`D:\Prop\src\Fix.CTrader\Class1.cs`:

```csharp
namespace TraderIntelligence.Fix.CTrader;

public class Class1
{

}
```

### 2.3 Project file

`D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`:

- `net8.0`, nullable, implicit usings
- `ProjectReference` → `Domain`, `Application`
- **No `PackageReference` at all** — not `QuickFIXn.Core`, not `QuickFIXn.FIX44`
- No `Content` / `EmbeddedResource` for session settings or RoE dictionary

`obj\project.assets.json` restore snapshot is **stale and empty**: `"libraries": {}`, `"projectFileDependencyGroups": { "net8.0": [] }`, and the restore graph lists **no project references**. That does not match the current `.csproj`. The project has never been restored/built in this tree after the Domain/Application refs were added. Unit/integration restore graphs still point at `bin/placeholder/TraderIntelligence.Fix.CTrader.dll`.

### 2.4 Wiring (references only — no usage)

| Consumer | Reference | Uses adapter types? |
|---|---|---|
| `Mt5TraderIntelligence.sln` | project `{76085664-…}` | N/A |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | yes | **no** — `Worker` is the stock `BackgroundService` that logs `"Worker running at: {time}"` every 1 s |
| `tests/Unit` | yes | empty `Fact` stub |
| `tests/Integration` | yes | empty `Fact` stub |
| `apps/api` | **no** | — |

`apps/fix-worker` has **no** QuickFIX/n package, **no** cTrader env keys, **no** `CTRADER_*` / `REAL_COPY_EXECUTION_ENABLED` flags.

Repo-wide `*.cs` search for `QuickFix`, `QuickFIX`, `IApplication`, `SessionSettings`, `SocketInitiator`, `TcpClient`, `ClOrdID`, `ExecutionReport`, `CTrader` **outside the architecture markdown**: **zero hits**.

`Domain`, `Application`, `Infrastructure`, `Mt5` are the same empty `Class1` pattern. Infrastructure already references `StackExchange.Redis` and `Npgsql.EntityFrameworkCore.PostgreSQL` — the *packages* that would back a lease and §44 tables exist, the *types/tables* do not.

`docs/` is empty. There is no `docs/ctrader-fix.md`.

---

## 3. Official need (binding)

Sources checked 2026-08-18:

- Architecture: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§1, 5, 25–34, 41–44, 56, 61, 66–70, 74
- https://help.ctrader.com/fix/
- https://help.ctrader.com/fix/specification/ (cTrader FIX engine Rules of Engagement)
- https://help.ctrader.com/fix/sending-and-receiving-messages/
- https://help.ctrader.com/fix/faqs/
- QuickFIX/n: https://github.com/connamara/quickfixn — current packages **QuickFIXn.Core 1.14.1** + **QuickFIXn.FIX44 1.14.1** (v1.14 renamed `QuickFIXn.FIX4.4` → `QuickFIXn.FIX44`)

### 3.1 Two independent sessions (non-negotiable)

cTrader FIX 4.4 is **two connections**, not one multiplexed session.

Pepperstone / architecture §25:

| | QUOTE | TRADE |
|---|---|---|
| Host | `live-us-eqx-01.p.c-trader.com` | same |
| SSL (production default) | **5211** | **5212** |
| Plain (not production default) | 5201 | 5202 |
| SenderCompID | `live.pepperstone.1369850` | `live.pepperstone.1369850` |
| TargetCompID (broker form) | `cServer` | `cServer` |
| Session qualifier | `QUOTE` | `TRADE` |

Official RoE **standard header**:

| Tag | Field | Required | Official value |
|---|---|---|---|
| 8 | BeginString | Yes | `FIX.4.4` |
| 49 | SenderCompID | Yes | `<Environment>.<BrokerUID>.<Trader Login>` e.g. `live.pepperstone.1369850` |
| 56 | TargetCompID | Yes | Official table says **`CSERVER`** |
| 57 | TargetSubID | Yes | **`QUOTE` or `TRADE`** — this *is* the session split |
| 50 | SenderSubID | No | **Must be `QUOTE` if TargetSubID=QUOTE** |

Architecture §27 requires two independent objects:

```
CTraderQuoteSession
CTraderTradeSession
```

each with its own connection, MsgSeqNum, heartbeat, last in/out timestamps, reconnect, metrics, logs. **Do not share one sequence counter.**

Official connectivity: sequence numbers reset on session establish (`ResetSeqNumFlag=Y`, tag 141). EncryptMethod=0; transport security is TLS, not FIX encryption.

### 3.2 QuickFIX/n — not a home-grown TcpClient engine

Architecture §1.8 / §5:

> Do not write a FIX engine from raw TcpClient unless absolutely necessary. Prefer QuickFIX/n with a cTrader-specific Rules-of-Engagement dictionary/configuration. Do not assume the generic FIX 4.4 dictionary is sufficient.

Official send/receive article (Spotware C# sample, last noted 2017 / RoE v2.9.1):

- Teaching demo constructs strings, checksums, and **two `TcpClient`s** (price port + trade port).
- Explicit disclaimer: *“by no means a full FIX engine… If you would like to avoid building your own FIX engine, you might consider using one of the third-party FIX engines available.”*

**Implementation law for this repo:** host two QuickFIX/n `IInitiator` / `IApplication` instances (or one initiator with two `SessionID`s that must never share sequence files). Do **not** copy the Spotware `TcpClient` sample into `Fix.CTrader`.

Generic `FIX44.xml` is **insufficient**. cTrader custom tags that a stock dictionary will drop or reject:

| Tag | Name | Used on |
|---|---|---|
| 721 | PosMaintRptID | NewOrderSingle, ExecutionReport, PositionReport |
| 1000–1006 | AbsoluteTP/RelativeTP/AbsoluteSL/RelativeSL/TrailingSL/TriggerMethodSL/GuaranteedSL | ExecutionReport / PositionReport |
| 1007 | SymbolName | SecurityList |
| 1008 | SymbolDigits | SecurityList |

Tag 55 `Symbol` is a **Spotware numeric instrument ID** (`Long`), not `"XAUUSD"`. Official reject example: `Expected numeric symbolId, but got CS8260`.

### 3.3 Header-case trap (§26 + RoE)

| Source | TargetCompID |
|---|---|
| Official RoE table + every official example | `CSERVER` |
| Pepperstone form / architecture env sample §25, §56 | `cServer` |
| Official send/receive prose | “usually it is cServer” *and* sample uses `CSERVER` |

Architecture §26 is correct: **do not guess**. Make `SenderSubID` and `TargetSubID` **and** `TargetCompID` configurable. Never silently rewrite `cServer` ↔ `CSERVER`. Prove Logon on **both** sessions in diagnostics before enabling TRADE application messages.

Logon body (RoE): `98=0`, `108=<HeartBtInt>`, `141=Y`, `553=<numeric login 1369850>`, `554=<password>`. Username is the numeric trader login; SenderCompID is `live.pepperstone.1369850`.

FAQ: **QUOTE heartbeats may be omitted while quotes stream** — do not treat missing 35=0 on QUOTE as dead without quote-age.

FAQ: **multiple simultaneous API connections duplicate every report** to each connection. This is why §28 single-active TRADE ownership is a safety control, not a nicety.

### 3.4 Minimum message set (RoE = architecture §29)

System: Heartbeat, TestRequest, Logon, Logout, ResendRequest, Reject, SequenceReset.

Application: MarketDataRequest / Snapshot / IncrementalRefresh / **MarketDataRequestReject (35=Y, not listed in §29 — still required)**, SecurityListRequest / SecurityList, NewOrderSingle, ExecutionReport, OrderStatusRequest, OrderMassStatusRequest, RequestForPositions, PositionReport, OrderCancelRequest, OrderCancelReject, OrderCancelReplaceRequest, BusinessMessageReject.

Market orders are IOC (`OrdType=1`). Limit/Stop are GTC/GTD. `TimeInForce` on send is deprecated/ignored; cServer derives it. Attach-to-position uses tag 721 (hedge accounts only).

---

## 4. Section-by-section gap (§§25–34, 41–44, 61)

| § | Title | Required in `Fix.CTrader` / adjacent | Status |
|---|---|---|---|
| **25** | Venue: host, SSL 5211/5212, two SenderCompIDs/qualifiers, account 1369850 | Options + two SessionSettings | **MISSING** |
| **26** | Configurable SenderSubID/TargetSubID; no case mutation; staging Logon proof | Header mapping options | **MISSING** |
| **27** | `CTraderQuoteSession` + `CTraderTradeSession`, independent seq/HB/metrics | Two session objects | **MISSING** |
| **28** | Single-active TRADE ownership (Redis lease / fencing / advisory lock) | Lease types | **MISSING** |
| **29** | Full RoE message surface, not “send market order” | Message handlers | **MISSING** |
| **30** | SecurityList → persist instrument ID, name, digits; no hardcoded ID | Discovery service | **MISSING** |
| **31** | QUOTE book: bid/ask, venue+recv timestamps, symbol ID; stale-quote reject | Quote cache | **MISSING** |
| **32** | MT5 event ↛ FIX. Path is CopyIntent → Risk → ExecutionIntent → worker → 35=D | Worker + ports | **MISSING** |
| **33** | Persist `cl_ord_id` **before** send; distinguish not-sent / sent-unknown / accepted / partial / filled / rejected / cancelled | ClOrdID + persist | **MISSING** |
| **34** | Disconnect after send → `EXECUTION_STATE_UNKNOWN`; 35=H / 35=AF / positions; **no blind resend** | Unknown-state recovery | **MISSING** |
| **41** | `CTRADER_FIX_ENABLED`, `*_QUOTE_ENABLED`, `*_TRADE_SESSION_ENABLED`, `REAL_COPY_EXECUTION_ENABLED=false` | Feature flags | **MISSING** (and not in FixWorker appsettings) |
| **42** | On TRADE logon: block new exec → mass status → positions → repair DB → `READY_FOR_EXECUTION` | Startup reconciler | **MISSING** |
| **43** | Periodic order/position compare; alerts for unknown/missing/qty/side/orphan/unexpected fill | Periodic reconciler | **MISSING** |
| **44** | Durable tables listed below | No EF entities, no SQL | **MISSING** |
| **61** | Adapter test mode: recorded ER replay, MD replay, disconnect, dup ER, partial, reject, unknown-state | Simulator | **MISSING** |

§44 tables with **zero** code or migrations anywhere in the repo:

```
execution_venues
fix_sessions
fix_session_events
destination_symbols
destination_quotes
copy_intents
risk_decisions
execution_intents
fix_orders
fix_execution_reports
destination_positions
source_destination_links
execution_reconciliation_runs
execution_reconciliation_issues
```

Architecture phases: Phase 4 = QUOTE only; Phase 7 = TRADE read/recon with **NewOrderSingle still disabled**; Phase 8 = flagged live send. Current code is **before Phase 4**.

---

## 5. Missing types (required list)

These types do not exist. Names below are the minimum the adapter must grow. Domain owns values/state; Application owns ports; `Fix.CTrader` owns QuickFIX adapters; Infrastructure owns Redis/Postgres; `FixWorker` hosts.

### 5.1 Session config — **MISSING**

| Type | Layer | Why |
|---|---|---|
| `CTraderFixVenueOptions` | Application / hosting | Host, account id, password ref (not plaintext in repo), `UseSsl`, feature flags §41 |
| `CTraderFixSessionOptions` | Application | Per-session: SSL port, plain port, SenderCompID, TargetCompID, SenderSubID, TargetSubID, qualifier, HeartBtInt, FileStore/FileLog paths |
| `CTraderSessionQualifier` | Domain | `Quote` / `Trade` enum; maps to tag 57 |
| `CTraderFixHeaderMapping` | Domain | Explicit 49/50/56/57 values; **no silent case fold**; documents broker-form vs RoE `CSERVER` |
| `CTraderQuoteSession` | Fix.CTrader | Independent QuickFIX `IApplication` + `SessionID` for QUOTE :5211 |
| `CTraderTradeSession` | Fix.CTrader | Independent QuickFIX `IApplication` + `SessionID` for TRADE :5212 |
| `ICTraderFixSessionFactory` | Application port | Builds two initiators from options; never one shared sequence store |
| `CTraderFixSessionState` | Domain | Connected / logged-on, last inbound/outbound UTC, next seq in/out, reconnect count, last HB/TR, last error |
| `FIX44-cTrader.xml` (RoE dictionary) | Fix.CTrader content | Custom 721, 1000–1008, 55-as-long |
| `quote.cfg` / `trade.cfg` | Fix.CTrader content | QuickFIX `SessionSettings`; `SocketConnectHost/Port`, `SSLEnable`, `ResetOnLogon=Y`, distinct `FileStorePath` |
| Pinned packages | csproj | `QuickFIXn.Core` + `QuickFIXn.FIX44` (pin 1.14.1 or later 1.14.x). **Not** deprecated `QuickFIXn.FIX4.4` |

Absence of these types is why two independent sessions cannot be created.

### 5.2 Ownership lease — **MISSING**

| Type | Layer | Why |
|---|---|---|
| `ITradeSessionOwnershipLease` | Application port | Acquire / renew / release TRADE ownership |
| `TradeSessionLease` | Domain | `accountId`, `ownerInstanceId`, `fenceToken`, `expiresAt`, `sessionQualifier=TRADE` |
| `FenceToken` | Domain | Monotonic token; stale owner must not send 35=D after leadership change |
| `RedisTradeSessionLease` | Infrastructure | Redis lease + fencing (§5 / §28). Redis is **not** source of truth for orders |
| `PostgresAdvisoryLockTradeLease` | Infrastructure (alt) | Acceptable §28 alternative |
| `TradeSessionLeadershipGate` | Application | New owner: establish session → reconcile (§42) → only then accept intents |

Official FAQ: two open TRADE connections **duplicate every ExecutionReport**. Architecture §1.9 / §28: one production TRADE owner per account.

QUOTE may be multi-reader later; **TRADE must be singleton**. Lease the TRADE session, not the process.

### 5.3 ClOrdID — **MISSING**

| Type | Layer | Why |
|---|---|---|
| `ClOrdId` | Domain value object | Tag 11. Unique for **all** orders (official Order Status text). Opaque, persistable, comparable |
| `OrigClOrdId` | Domain | Tag 41 on cancel (35=F) and replace (35=G). New `ClOrdId` per request |
| `IClOrdIdFactory` | Application port | Deterministic unique IDs (e.g. ULID / `execIntentId` + attempt). Never reuse after persist |
| `ClientOrderKey` | Domain | `(destinationAccount, clOrdId)` unique constraint |
| `FixOrderPersistBeforeSend` | Application | Writes `execution_intent_id`, `cl_ord_id`, source ids, symbol, side, qty, `created_at`, `status=NotSent` **then** `Send(35=D)` |
| `MassStatusReqId` / `PosReqId` / `SecurityReqId` / `MdReqId` | Domain | Same uniqueness family for 35=AF / AN / x / V |

§33 + official RoE: unique client order IDs are required for Order Status to work. Cancel/replace allocate a **new** ClOrdID and reference OrigClOrdID.

Never generate ClOrdID in memory and send before the row exists. Crash between send and persist is how you get unknown duplicates.

### 5.4 Execution state machine — **MISSING**

| Type | Layer | Why |
|---|---|---|
| `ExecutionOrderState` | Domain | §33+§34: `NotSent`, `SentAckUnknown`, `Accepted`, `PartiallyFilled`, `Filled`, `Rejected`, `Cancelled`, `Expired`, `Replaced`, **`Unknown`** |
| `ExecutionOrder` | Domain | Intent id, ClOrdID, cServer `OrderID` (37), `PosMaintRptID` (721), qty/cum/leaves, last ER |
| `ExecutionReport` | Domain | Tags 150 ExecType, 39 OrdStatus, 32/14/151, 6 AvgPx, 103, 58, 584, 911 |
| `IExecutionReportApplier` | Application | Pure transition: `(state, ER) → state`. Idempotent on duplicate ER (FAQ) |
| `ExecutionOrderStateMachine` | Domain | Legal edges only. `Unknown` is absorbing until recon |
| `UnknownExecutionRecovery` | Application | After drop: 35=H by ClOrdID, then 35=AF (`MassStatusReqType=7`), then 35=AN; **never** second 35=D until resolved |
| `StartupReconciliationCoordinator` | Application | §42 sequence; blocks `READY_FOR_EXECUTION` |
| `PeriodicReconciliationCoordinator` | Application | §43 compare + issue rows |
| `ReadyForExecutionGate` | Application | Requires: lease owned, TRADE logged on, recon clean, `REAL_COPY_EXECUTION_ENABLED`, risk healthy, quote fresh |
| `IFixTradeGateway` | Application port | `SubmitNew`, `Status`, `MassStatus`, `Positions`, `Cancel`, `Replace` — **no MT5 callback path** |

Official ExecType (150): `0` New, `4` Canceled, `5` Replace, `8` Rejected, `C` Expired, `F` Trade, `I` Order Status.  
Official OrdStatus (39): `0` New, `1` Partial, `2` Filled, `8` Rejected, `4` Cancelled, `C` Expired.

§34 critical path: send → network dies → “did cServer get it?” → **`EXECUTION_STATE_UNKNOWN`** → reconcile → only then decide another order.

### 5.5 Simulator — **MISSING**

| Type | Layer | Why |
|---|---|---|
| `ICTraderFixVenue` | Application port | Production = QuickFIX sessions; test = simulator. Worker depends on the port |
| `CTraderFixSimulator` | tests / Fix.CTrader test host | In-process venue: logon, security list, quotes, orders, positions |
| `RecordedFixMessageStore` | tests | Parse recorded 35=8 / 35=X / 35=AP / 35=j / 35=9 |
| `FixDisconnectScript` | tests | Drop after 35=D to force `Unknown` |
| `DuplicateExecutionReportScript` | tests | Two identical 35=8 (FAQ duplicate-connection behaviour) |
| `PartialFillScript` | tests | 39=1 then 39=2 |
| `RejectScript` | tests | 150=8 / 35=j / 35=Y |
| `UnknownStateDisconnectScript` | tests | §61 + §34 recovery without a second 35=D |

Architecture §61: **Do not use the real Pepperstone account as the first integration test.** `tests/Fix` and `tests/Replay` folders do not exist. Unit/integration projects only have empty `UnitTest1`.

Required unit tests (§60) that cannot be written until the types exist: ClOrdID generation, ExecutionReport transitions, unknown-execution recovery, QuickFIX session configuration, FIX parse/build.

---

## 6. Additional missing types (same adapter, not in the five-name list)

Needed to satisfy §§29–32, 41–44 without pretending they are “later”:

- `DestinationInstrument` — Spotware id (55), `SymbolName` (1007), `SymbolDigits` (1008)
- `ISecurityListClient` / `CTraderInstrumentDiscovery`
- `DestinationQuote` — bid, ask, recv UTC, venue UTC, symbol id, age
- `IDestinationQuoteCache` + stale-quote policy
- `CopyIntent`, `RiskDecision`, `ApprovedExecutionIntent` (Application/Domain; FIX must not be invoked from MT5)
- `SourceDestinationLink` — reconstructed trade ↔ fix orders ↔ position 721
- `ExecutionVenue`, `FixSessionRecord`, `FixSessionEvent`
- `ReconciliationRun`, `ReconciliationIssue`
- Feature-flag type `CTraderFixFeatureFlags` matching §41 defaults (`REAL_COPY_EXECUTION_ENABLED=false`)

Layering defect already in the stub: `Fix.CTrader` references **Application**. The adapter should implement Application **ports** and depend on **Domain** (+ QuickFIX). Host (`FixWorker`) composes Infrastructure + Fix.CTrader. Invert before adding engine code or Application will drag the adapter into a cycle.

---

## 7. Safety / anti-patterns

1. **Do not port** https://github.com/spotware/FIX-API-Sample (`TcpClient` + string builder + checksum). Official docs say it is not an engine.
2. **Do not** share one QuickFIX `FileStorePath` / sequence series between QUOTE and TRADE.
3. **Do not** start two FixWorker replicas against TRADE without a lease — cServer will fan out duplicate 35=8.
4. **Do not** send 35=D from an MT5 callback (§32).
5. **Do not** retry 35=D because TCP dropped (§33–34).
6. **Do not** hardcode a Pepperstone XAU instrument id from another account (§30). Discover via 35=x / 35=y.
7. **Do not** put `XAUUSD` in tag 55. It must be the numeric id from SecurityList.
8. **Do not** enable `REAL_COPY_EXECUTION_ENABLED` until simulator + recon + ClOrdID uniqueness are proven (§68–70).
9. **Do not** commit `CTRADER_FIX_PASSWORD` or account 1369850 password. Architecture already lists it as `<SECRET>`.
10. **Do not** treat generic FIX 4.4 `DataDictionary` as complete — custom 1007/1008/721 will be lost.

---

## 8. Honest metrics

| Metric | Measured |
|---|---|
| Product `.cs` files in `Fix.CTrader` | **1** (`Class1`) |
| Types that do FIX work | **0** |
| QuickFIX/n package references | **0** |
| `TcpClient` FIX engine | **0** (correct absence) |
| Independent QUOTE/TRADE sessions | **0 / 2** |
| Session-config types | **0** |
| Ownership-lease types | **0** |
| ClOrdID types | **0** |
| Execution state-machine types | **0** |
| Simulator types | **0** |
| §44 tables / EF entities | **0** |
| Feature flags in FixWorker | **0** |
| FIX unit/integration/replay tests | **0** |
| Adapter completeness vs §§25–34, 41–44, 61 | **0%** |

---

## 9. What “done” for this project looks like (not implemented)

Pinned QuickFIX/n; RoE dictionary; two SessionSettings; `CTraderQuoteSession` + `CTraderTradeSession`; Redis/DB TRADE lease with fence; persist-before-send `ClOrdId`; state machine including `Unknown`; §42/§43 recon; §41 flags defaulting live send **off**; in-process simulator used by `tests/Fix` **before** any socket to `live-us-eqx-01.p.c-trader.com`.

Until then, `Fix.CTrader` is a namespace holder, not an execution adapter.

---

## 10. Sources

- `D:\Prop\src\Fix.CTrader\Class1.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Fix.CTrader\obj\project.assets.json`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§25–34, 41–44, 61
- https://help.ctrader.com/fix/
- https://help.ctrader.com/fix/specification/
- https://help.ctrader.com/fix/sending-and-receiving-messages/
- https://help.ctrader.com/fix/faqs/
- https://www.nuget.org/packages/QuickFIXn.Core/ (1.14.1)
- https://www.nuget.org/packages/QuickFIXn.FIX44/ (1.14.1)
