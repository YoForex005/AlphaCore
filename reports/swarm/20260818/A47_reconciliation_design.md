# A47 — cTrader startup and periodic reconciliation design

**Status:** binding implementation spec (pre-code).  
**Date:** 2026-08-18  
**Agent:** Grok Build subagent A47  
**Product source modified:** **none** (this file only).  
**Architecture:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§28, 33–35, 41–44, 52, 54, 57–58, 61–63, 67 Phase 7, 68, 70.  
**Official RoE (re-read at implementation time):** https://help.ctrader.com/fix/specification/  
**Official FAQ:** https://help.ctrader.com/fix/faqs/  
**Sibling swarm specs this document must not contradict:**

| File | What A47 inherits |
|---|---|
| `A05_fix_ctrader_audit.md` | Adapter is empty; §42/§43 types listed as MISSING |
| `A20_table_catalog.md` | `execution_reconciliation_*`, `fix_orders`, `destination_positions` keys |
| `A23_risk_engine_spec.md` | `RECONCILIATION_BLOCK` / `EXECUTION_STATE_UNKNOWN` evaluation order |
| `A25_fix_session_spec.md` | TRADE state `RECONCILING → READY_FOR_EXECUTION \| BLOCKED_INCONSISTENT`; unknown-state recovery |
| `A26_dashboard_api_spec.md` | §54 JSON, issue types, ack / run endpoints |
| `A27_test_inventory.md` | Reconcile + harness test names |
| `A28_phases_gates.md` | Phase 7: TRADE **read/recon only**; `NewOrderSingle` still off |
| `A30_implementation_sequence.md` | First useful dashboard may show cTrader recon as `never` / N/A |
| `A32` / `A34` | Header mapping; duplicate reports if two TRADE sockets |

This is the detailed design for **architecture §§42–43 and 54**. A25 §7.4–7.5 is the one-page sketch; this file is the algorithm, FIX contracts, repair policy, gate, schema, dashboard, and tests.

---

## 0. Honesty pin (measured 2026-08-18)

| Item | Measured state |
|---|---|
| `src/Fix.CTrader` TRADE adapter | Scaffold / empty — **no** `35=AF` / `35=AN` |
| `StartupReconciliationCoordinator` | **MISSING** |
| `PeriodicReconciliationCoordinator` | **MISSING** |
| `ReconciliationGate` / `READY_FOR_EXECUTION` | **MISSING** |
| `execution_reconciliation_runs` / `_issues` | **MISSING** (catalogued in A20, not migrated) |
| `fix_orders` / `destination_positions` / `fix_execution_reports` | **MISSING** |
| `apps/fix-worker` | Template heartbeat; no flags, no lease, no recon |
| Live `NewOrderSingle` | **OFF by absence** and by architecture default `REAL_COPY_EXECUTION_ENABLED=false` |

Do **not** claim cTrader reconciliation works. Phase 7 is not started (`A30` stops before it). This document is how to implement it later.

---

## 1. Binding law

Quoted architecture §42 (startup):

```text
Login successful
    ↓
block new executions
    ↓
OrderMassStatusRequest
    ↓
RequestForPositions
    ↓
consume Execution/Position reports
    ↓
compare with internal DB
    ↓
repair/update state
    ↓
only if reconciled:
READY_FOR_EXECUTION
```

Quoted: **“Never assume the database is correct after restart.”**

Quoted architecture §43 (periodic): compare internal open orders + destination positions vs cServer; raise alerts for:

```text
unknown external position
missing internal position
quantity mismatch
side mismatch
orphan execution report
unexpected fill
```

Quoted architecture §54 (dashboard): last successful MT5 recon, last successful cTrader recon, the six issue families plus **unresolved execution states**. **“Nothing unresolved should be silently ignored.”**

Quoted architecture §28 / §32 / §34:

- New TRADE owner: establish session → reconcile → **only then** accept intents.
- Never send FIX from an MT5 callback.
- Disconnect after send → `EXECUTION_STATE_UNKNOWN` → OrderStatus / MassStatus / positions → **then** decide if another order is required. **Never** blind-retry `NewOrderSingle`.

Quoted architecture §41: TRADE session + mass-status / position **reads** are allowed while `REAL_COPY_EXECUTION_ENABLED=false`. Phase 7 is **read + reconcile**. Phase 8 send is a later, separately gated increment.

### 1.1 Authority model (do not invert)

| Question | Authority | Store |
|---|---|---|
| What is actually open on Pepperstone / cServer **right now**? | **Venue** (TRADE snapshot: `35=8` with `150=I` + `35=AP`) | Transient snapshot rows on the run |
| What did **we** intend / send / apply? | **Postgres** | `execution_intents`, `fix_orders`, `fix_execution_reports`, `destination_positions` |
| May we send a new `35=D`? | Conjunction of flags + lease + **this gate** + risk (A23, A25 §6.3) | `fix_sessions.ready_for_execution` derived from latest successful run |
| Is Redis source of truth for the book? | **No** | Lease / fence only (A05, A20) |

During compare, **venue wins on observed qty/side/id**. Postgres is updated only by the **repair policy** in §8. After a **successful** run, Postgres is again the execution-state authority **until** the next snapshot or an unsolicited `35=8`.

### 1.2 What this design does **not** do

- Send `NewOrderSingle` to “fix” a mismatch.
- Auto-flatten an unknown external position.
- Auto-cancel a venue working order we did not place (except a later, separately authorized `EMERGENCY_FLATTEN`).
- Treat Logon as `READY_FOR_EXECUTION`.
- Treat an empty incomplete snapshot (timeout, no `35=j`, no `728=2`) as “flat book”.
- Use the QUOTE session for orders/positions.
- Use the generic FIX 4.4 dictionary without cTrader tags `721`, `584`, `911`, `710`, `727`, `728`, `702`, `704`, `705`, `730`, `1000–1006`.
- Open a second TRADE socket to “get a second opinion” (official FAQ: every report is **duplicated** to each active connection).

---

## 2. Official FIX contracts (cTrader RoE, fetched 2026-08-18)

All of these messages ride the **TRADE** session (`57=TRADE` outbound). Persist request IDs **before** the socket write (same family as `ClOrdID`, A05 / A25).

### 2.1 Order Mass Status Request — `MsgType(35)=AF`

Official: *“requests the status for orders matching the criteria specified within the request. The answer will be returned as a number of Execution Report messages (one for each order), or as a Business Message Reject message if no orders are found.”*

| Tag | Field | Req | Official value / comment |
|---|---|---|---|
| 584 | `MassStatusReqID` | Yes | Unique ID assigned by the client |
| 585 | `MassStatusReqType` | Yes | **`7` = Status for all orders. Only `7` is currently supported.** |
| 225 | `IssueDate` | No | *“If set, the response will contain only orders created **before** this date.”* |

**This design:** startup and periodic **must omit tag 225**. Setting it would silently drop newer working orders and produce a false-flat book.

Official request example (RoE):

```text
8=FIX.4.4|9=117|35=AF|34=3|49=live.theBroker.12345|52=20170404-07:20:55.325|56=CSERVER|57=TRADE|225=20170404-07:20:44.582|584=mZzEY|585=7|10=065|
```

Official response is **`35=8`**, not a dedicated mass-status body. Relevant ER tags for recon:

| Tag | Field | Role in recon |
|---|---|---|
| 150 | `ExecType` | Mass-status answers use **`I` = Order Status**. **Not a fill.** |
| 39 | `OrdStatus` | `0` New, `1` Partial, `2` Filled, `8` Rejected, `4` Cancelled, `C` Expired |
| 11 | `ClOrdID` | Match to `execution_intents` / `fix_orders` |
| 37 | `OrderID` | cServer order id |
| 584 | `MassStatusReqID` | Correlate to **this** run |
| 911 | `TotNumReports` | Expected count of `35=8` for this `584` |
| 54 / 55 / 38 / 14 / 151 / 721 | side, symbol, qty, cum, leaves, position | Compare fields |
| 17 | `ExecID` | **Not present** on the official mass-status example |

Official mass-status ER example (working sell limit; `911=1`):

```text
8=FIX.4.4|9=199|35=8|34=13|49=CSERVER|50=TRADE|52=20170404-07:20:55.333|56=live.theBroker.12345|14=0|37=635|38=100000|39=0|40=2|44=1.35265|54=2|55=1|59=1|60=20170404-07:20:44.582|150=I|151=100000|584=mZzEY|721=617|911=1|10=152|
```

**Empty working book:** official completion is **`35=j` Business Message Reject**, not “zero ExecutionReports and silence”. `379 BusinessRejectRefID` should echo the request business id. `380=0` (Other) is the only documented reason code.

**Working vs historical:** RoE says type `7` is “all orders”. The published example is a working limit (`39=0`, `151=OrderQty`). Treat the snapshot as:

- **Venue working set** = reports with `39∈{0,1}` (leaves still live).
- Terminal `39∈{2,4,8,C}` update matching internal rows but are **not** open orders.
- If a future RoE build dumps large historical sets, still filter the compare to working + any `ClOrdID` we have non-terminal.

### 2.2 Order Status Request — `MsgType(35)=H` (unknown-state only)

Not used for the full-book startup/periodic snapshot. Used **before** mass-status when a **single** `ClOrdID` is `EXECUTION_STATE_UNKNOWN` (A25 §5.4). Requires unique `ClOrdID` (official Order Status text).

### 2.3 Request for Positions — `MsgType(35)=AN`

| Tag | Field | Req | Official comment |
|---|---|---|---|
| 710 | `PosReqID` | Yes | Unique request ID set by the client |
| 721 | `PosMaintRptID` | No | *“A position ID to request. **If not set, all open positions will be returned.**”* |

**This design:** omit `721` on startup/periodic so the snapshot is the **full open book**. Single-`721` requests are allowed later for targeted unknown-state checks.

Official request example:

```text
8=FIX.4.4|9=100|35=AN|49=live.theBroker.12345|56=CSERVER|34=99|52=20170117-10:09:54|50=any_string|57=TRADE|710=876316401|10=103|
```

### 2.4 Position Report — `MsgType(35)=AP`

RoE does **not** list `RequestForPositionsAck` (`35=AO`). Completeness is carried **on each `35=AP`**.

| Tag | Field | Req | Official / observed use |
|---|---|---|---|
| 710 | `PosReqID` | Yes | Echo of our request |
| 721 | `PosMaintRptID` | No | Position id; *not set if `728` is not `VALID_REQUEST`* |
| 727 | `TotalNumPosReports` | Yes | Count of reports when `728=VALID_REQUEST`, else **`0`** |
| 728 | `PosReqResult` | Yes | Official table: **`0` or `2`** |
| 55 | `Symbol` | No | Spotware **numeric** instrument id; omitted if not valid request |
| 702 | `NoPositions` | No | `1` when valid request; omitted otherwise |
| 704 | `LongQty` | No | Open volume if **buy**; `0` if sell; omitted if not valid |
| 705 | `ShortQty` | No | Open volume if **sell**; `0` if buy; omitted if not valid |
| 730 | `SettlPrice` | No | Average price of opened volume |
| 1000–1006 | TP/SL suite | No | Informational; persist on `destination_positions`, not a compare key |

Official example (one short position, `727=1`, `728=0`, `704=0`, `705=30000`, `721=101`):

```text
8=FIX.4.4|9=163|35=AP|34=98|49=CSERVER|50=TRADE|52=20170117-10:09:54.076|56=live.theBroker.12345|57=any_string|55=1|710=876316401|721=101|727=1|728=0|730=1.06671|702=1|704=0|705=30000|10=182|
```

**Interpretation of `728` (label inference):** RoE comments use the name `VALID_REQUEST` for `728=0`. Combined with “otherwise `727=0`” and omitted `721`/`55`/`704`/`705`, **`728=2` is the empty / no-matching-positions completion**. Standard FIX 4.4 `PosReqResult=2` is “No positions found that match criteria”. If an implementation-time RoE build documents a different pair, pin the new values in `fix_session_events` and fail closed on any other `728`.

**Unsolicited `35=AP`:** not in the official catalog as a push stream. Periodic `35=AN` is mandatory. Do not wait for positions to “show up”.

### 2.5 Business Message Reject — `MsgType(35)=j`

| Tag | Field | Role |
|---|---|---|
| 379 | `BusinessRejectRefID` | Must equal our `MassStatusReqID` or `PosReqID` |
| 380 | `BusinessRejectReason` | Documented `0` = Other |
| 372 | `RefMsgType` | `AF` or `AN` |
| 58 | `Text` | Persist; do not parse as a fill |

### 2.6 Session / identity traps that break recon

1. **Tag 55 is not `XAUUSD`.** It is the Spotware long id from Security List (`1007` is the name). Compare orders/positions by **instrument id**, then map to `canonical_symbol` via `destination_symbols` (A34).
2. **Hedge vs net.** `721` may be set on `NewOrderSingle` **only for hedged accounts** (RoE). Compare **per `PosMaintRptID`**, never by netting all XAU into one row.
3. **Duplicate reports** if two TRADE connections are up (FAQ). Idempotency keys: see §6.4.
4. **`ResetSeqNumFlag=Y`** on Logon: the FIX store is **not** a durable order log. Postgres is.
5. Header case: do not rewrite `cServer` ↔ `CSERVER` (A25 / A32). Recon must still accept inbound `50=TRADE` as the server-side session qualifier.

---

## 3. Components and ownership

Host: `apps/fix-worker` (lease owner). Domain owns values and the comparer. Application owns coordinators and the gate. `Fix.CTrader` only builds/sends `35=AF`/`35=AN`/`35=H` and parses `35=8`/`35=AP`/`35=j`. Infrastructure persists runs/issues/orders/positions.

### 3.1 Types to create (none exist today)

| Type | Layer | Responsibility |
|---|---|---|
| `MassStatusReqId` | Domain | Opaque unique id; persist-before-send |
| `PosReqId` | Domain | Same family |
| `ReconciliationRunType` | Domain enum | `STARTUP`, `PERIODIC`, `POST_DISCONNECT`, `LEADERSHIP`, `UNKNOWN_RECOVERY`, `MANUAL` |
| `ReconciliationRunStatus` | Domain enum | `IN_PROGRESS`, `SUCCESS`, `DEGRADED`, `FAILED`, `CANCELLED` |
| `ReconciliationIssueType` | Domain enum | §5.1 — matches A26 names |
| `ReconciliationIssueStatus` | Domain enum | `OPEN`, `ACKNOWLEDGED`, `RESOLVED`, `WONT_FIX_AUDITED`, `ACCEPTED_EXTERNAL` |
| `ReadyForExecutionState` | Domain enum | `BLOCKED_PENDING_RECON`, `READY_FOR_EXECUTION`, `BLOCKED_INCONSISTENT`, `BLOCKED_STALE`, `BLOCKED_NO_SESSION` |
| `VenueOrderSnapshot` | Domain | One working/terminal ER attributed to a `MassStatusReqId` |
| `VenuePositionSnapshot` | Domain | One `35=AP` attributed to a `PosReqId` |
| `ReconciliationComparer` | Domain | Pure: `(internalBook, venueBook) → issues + safeRepairs` |
| `ReconciliationRepairPolicy` | Domain | Which repairs are legal (table §8) |
| `ExecutionReconciliationRun` | Domain entity | A20 run row |
| `ExecutionReconciliationIssue` | Domain entity | A20 issue row |
| `IFixTradeReadGateway` | Application port | `RequestMassStatus`, `RequestPositions`, `RequestOrderStatus` — **no** `SubmitNew` on this port |
| `IReconciliationRunStore` | Application port | Insert run, attach snapshots, upsert issues, complete run |
| `IDestinationBookStore` | Application port | Read/write `fix_orders`, `destination_positions`, `fix_execution_reports`, `execution_intents` |
| `StartupReconciliationCoordinator` | Application | §4 sequence; called from TRADE `OnLogon` and leadership acquire |
| `PeriodicReconciliationCoordinator` | Application | Timer + stale watchdog |
| `UnknownExecutionRecoveryService` | Application | A25 §5.4; reuses the same snapshots |
| `ReconciliationGate` | Application | Single query: may we send `35=D`? |
| `TradeSessionLeadershipGate` | Application | New owner **must** run startup recon before accepting intents (A05 / A25) |
| `CTraderMassStatusClient` / `CTraderPositionClient` | Fix.CTrader | QuickFIX send + correlate |

Layering: `Fix.CTrader` implements ports and depends on **Domain**, not Application (A05 layering defect). `FixWorker` composes.

### 3.2 One recon at a time

Per `venue_id` + destination account: a **single** in-process mutex **and** a Postgres advisory lock / `IN_PROGRESS` row.

- A new TRADE Logon **cancels** an in-flight `PERIODIC` (`status=CANCELLED`) and starts `STARTUP` / `POST_DISCONNECT`.
- `MANUAL` waits or is rejected `409` if `IN_PROGRESS`.
- Never send a second `35=AF`/`35=AN` while the first snapshot is open.

---

## 4. Startup reconciliation (§42)

### 4.1 Triggers (always block first)

| Trigger | `run_type` |
|---|---|
| TRADE `35=A` Logon success | `STARTUP` (cold process) or `POST_DISCONNECT` (session was already up once this process) |
| TRADE lease acquired / fencing token incremented | `LEADERSHIP` |
| Process start with TRADE already logged on (should not happen; if it does, treat as `STARTUP`) | `STARTUP` |

On **any** trigger:

1. Set gate → `BLOCKED_PENDING_RECON` (persist on `fix_sessions` + `system_events`).
2. Refuse `NewOrderSingle` even if `REAL_COPY_EXECUTION_ENABLED=true`.
3. Unsolicited live `35=8` (`150≠I`) still persist (append-only) but **do not** flip the gate by themselves.

### 4.2 Sequence

```text
TRADE logged on AND lease owned
        ↓
INSERT execution_reconciliation_runs
    status=IN_PROGRESS
    blocked_new_execution=true
    mass_status_req_id = new MassStatusReqId   -- persist first
    pos_req_id         = new PosReqId          -- persist first
        ↓
Send 35=AF  585=7  584=mass_status_req_id   (no 225)
        ↓
Collect 35=8 where 584 matches
    OR 35=j where 379 matches
    until complete (§6) or timeout → FAILED
        ↓
Send 35=AN  710=pos_req_id                 (no 721)
        ↓
Collect 35=AP where 710 matches
    until complete (§6) or timeout → FAILED
        ↓
Build venue working-order set + venue open-position set
Load internal book (same venue_id + destination_account)
        ↓
Comparer → issues + safe repairs
        ↓
Apply ONLY safe repairs in one Postgres transaction
Upsert issues (fingerprint)
        ↓
if no execution-impacting OPEN issues
   AND no unresolved execution_intents
   AND both snapshots complete:
        status=SUCCESS
        READY_FOR_EXECUTION = true
else if snapshots complete:
        status=DEGRADED
        READY_FOR_EXECUTION = false
        BLOCKED_INCONSISTENT
else:
        status=FAILED
        READY_FOR_EXECUTION = false
```

**Order of AF then AN is binding** (architecture §42). Do not parallelize the two requests: a fill between them is handled by §6.5 (AN is the later clock).

### 4.3 Leadership change

Loser: drop gate, stop the periodic timer, do **not** send, do **not** “finish” a run after fence loss (mark `FAILED` / `CANCELLED` with reason `LEADERSHIP_LOST`).

Winner: Logon (if needed) → `LEADERSHIP` run → same as §4.2. Accepting copy intents before `SUCCESS` is a P0 defect (duplicate TRADE reports + double send).

---

## 5. Periodic / daily reconciliation (§43)

### 5.1 Cadence (architecture does not pin numbers — these are defaults, must be configurable and measured)

```env
CTRADER_RECON_PERIODIC_INTERVAL_SEC=60
CTRADER_RECON_DAILY_UTC=00:05
CTRADER_RECON_SNAPSHOT_TIMEOUT_SEC=30
CTRADER_RECON_IDLE_COMPLETE_SEC=2
CTRADER_RECON_STALE_AFTER_SEC=180
CTRADER_RECON_QTY_ABS_TOLERANCE=0
```

| Clock | `run_type` | Notes |
|---|---|---|
| Every `PERIODIC_INTERVAL_SEC` after `READY` or after a degraded run | `PERIODIC` | Skip if `IN_PROGRESS` |
| First tick after `DAILY_UTC` | `PERIODIC` (tag `is_daily=true`) | Same algorithm; extra audit log; still not a flatten |
| Watchdog | — | If `now - last_successful_ctrader_run > STALE_AFTER_SEC` → drop READY to `BLOCKED_STALE` even with no new issues |
| `POST /api/v1/reconciliation/run` | `MANUAL` | Same algorithm; does **not** invent fills (A26) |

Periodic uses the **same** AF → AN → compare → repair path as startup. Difference:

- Gate is **already** `READY` or `BLOCKED_*`.
- A clean `SUCCESS` may **promote** `BLOCKED_INCONSISTENT` / `BLOCKED_STALE` back to `READY` (only this path, not an ACK).
- Any **execution-impacting** new issue **immediately** drops READY (`§70.14`).

### 5.2 Execution-impacting vs display-only

All A26 / §43 types are **execution-impacting** unless the issue is `ACCEPTED_EXTERNAL` for a **specific** `destination_position_id` (audited SuperAdmin). ACK is **not** a safety bypass.

| `issue_type` | Blocks READY? |
|---|---|
| `UNKNOWN_EXTERNAL_POSITION` | Yes (unless that `721` is `ACCEPTED_EXTERNAL`) |
| `MISSING_INTERNAL_POSITION` | Yes |
| `ORDER_MISMATCH` | Yes |
| `QUANTITY_MISMATCH` | Yes |
| `SIDE_MISMATCH` | Yes |
| `ORPHAN_FILL` | Yes |
| `ORPHAN_EXECUTION_REPORT` | Yes |
| `UNEXPECTED_FILL` | Yes |
| `UNRESOLVED_EXECUTION_STATE` | Yes |
| Snapshot `FAILED` / incomplete | Yes (`BLOCKED_PENDING_RECON` or stay blocked) |

`WONT_FIX_AUDITED` keeps the row visible and **still blocks** unless the type is `ACCEPTED_EXTERNAL`. This is how §54 “nothing silently ignored” is implemented.

---

## 6. Snapshot completeness (fail closed)

### 6.1 Mass-status complete when **either**

**A. Non-empty / working-or-terminal stream**

1. At least one `35=8` with `584 = our MassStatusReqId`.
2. Every such ER carries the same `911 = N` (if a later ER disagrees, **FAIL** the run).
3. Distinct venue orders collected (`37` if present, else `11`) **= N**.
4. Optional quiet period `IDLE_COMPLETE_SEC` with no further matching ER.

**B. Empty book**

1. Zero `35=8` with our `584`.
2. One `35=j` with `379 = our MassStatusReqId` and `372` absent or `AF`.
3. Then and only then: venue working set = **∅**.

**C. Otherwise after `SNAPSHOT_TIMEOUT_SEC`:** `FAILED`. Do **not** treat silence as empty (official FAQ: invalid FIX is **silently ignored**).

If both some `35=8` **and** a `35=j` share the same id → **FAIL** (incoherent).

### 6.2 Positions complete when **either**

**A. `728=0` (VALID_REQUEST)**

1. `727 = N` (`N ≥ 1`).
2. Count of `35=AP` with our `710` and `728=0` **= N**.
3. Each has `721` set. Duplicate `721` in one snapshot → **FAIL**.

**B. Empty book**

1. Exactly one `35=AP` with our `710`, `728=2`, `727=0`, `721` omitted.
2. Venue open-position set = **∅**.

**C. `35=j` for `AN`:** **FAIL** unless a future RoE documents empty-book-via-reject for positions the same way it does for mass-status. Do not guess.

**D. Timeout / `728` not in `{0,2}`:** `FAILED`.

### 6.3 Persist-before-send request ids

Same crash rule as `ClOrdID` (architecture §33):

```text
INSERT run (ids, IN_PROGRESS)
COMMIT
Send 35=AF / 35=AN
```

Crash after send: on next Logon a **new** run with **new** ids is started. Inbound late reports for the old ids are stored as orphans attributed to the old run (`ORPHAN_EXECUTION_REPORT` if they look like fills; status `150=I` on a cancelled run is ignored).

Id format (suggested): `R{runType[0]}{ulid}` so logs are greppable. Must be unique per destination account for the life of the account (RoE: “unique ID … assigned by the client”).

### 6.4 Dedup of inbound reports

| Message | Dedupe key | Why |
|---|---|---|
| Live fill `35=8` `150=F` | `(venue_id, exec_id)` when tag 17 present | A20; §70.5 |
| Mass-status `35=8` `150=I` | `(venue_id, mass_status_req_id, dest_order_id)` | Official example **has no ExecID** |
| `35=AP` | `(venue_id, pos_req_id, pos_maint_rpt_id)` or `(venue_id, pos_req_id, 'EMPTY')` if `728=2` | Completeness set |
| FAQ duplicate socket copies | same keys | Second copy is a no-op |

If tag 17 is absent on a **Trade** ER, synthesize `exec_id = TRADE:{cl_ord_id}:{transact_time}:{last_qty}:{last_px}` and still persist; raise `ORPHAN_EXECUTION_REPORT` if we cannot prove uniqueness. Do **not** leave a nullable hole in `fix_execution_reports_exec_uk` (A20 gap — this spec fills it).

`150=I` must **never** increment applied fill qty.

### 6.5 Race: fill between AF and AN

AN is later, so the position book is the newer of the two.

| AF said | AN said | Extra unsolicited `150=F` in the window | Action |
|---|---|---|---|
| Working order O | Position P includes O’s `721` | Fill for O | Apply fill (idempotent); drop O from working set if `39` became `2` |
| Working order O | No P for O’s `721` | none | Possible IOC remainder or close; if internal still open → issue, do not invent a close fill |
| No O | P qty > internal | Fill missing | `UNEXPECTED_FILL` |
| No O | P qty == internal | — | OK |

After both snapshots, fold in any unsolicited `35=8` with `TransactTime` ∈ `[af_send, an_complete]` **before** compare.

---

## 7. Compare algorithm (internal DB vs venue)

### 7.1 Internal sets (Postgres)

**Account scope:** `(venue_id, destination_account)` — Pepperstone login `1369850` in this lab. Never mix source `broker_id` into the match key (A20).

```text
internal_working_orders =
    fix_orders
    where venue_id = V
      and destination_account = A
      and status in (accepted, partially_filled)
      and leaves_qty > 0

internal_unknown_orders =
    execution_intents
    where destination_account = A
      and status in (sent_ack_unknown, EXECUTION_STATE_UNKNOWN)

internal_open_positions =
    destination_positions
    where venue_id = V
      and destination_account = A
      and is_open = true
      and destination_position_id not in accepted_external_set
```

`not_sent` intents are **not** in the venue compare (we have not claimed they exist on cServer). They also must not be sent until READY.

### 7.2 Venue sets (this run only)

```text
venue_working_orders =
    snapshot 35=8 584=this_run
    where ord_status in (0, 1)

venue_open_positions =
    snapshot 35=AP 710=this_run
    where pos_req_result = 0
      and (long_qty + short_qty) > 0
```

### 7.3 Order match key (in order)

1. `ClOrdID` (11) if non-empty **and** present on an internal row.
2. Else `OrderID` (37) if we already stored `dest_order_id`.
3. Else unmatched.

cTrader **UI / other-API** working orders will have a cServer `37` and usually **no** `11` we issued → `ORDER_MISMATCH` (unknown external order). Same treatment as an unknown position: block READY.

### 7.4 Position match key

`PosMaintRptID` (721) = `destination_positions.destination_position_id` (text, not integer — RoE type is String).

Side from the snapshot:

```text
if long_qty > 0 and short_qty == 0 → BUY
if short_qty > 0 and long_qty == 0 → SELL
if both > 0                         → SIDE_MISMATCH (netting blob on a hedge id)
if both == 0                        → ignore (should not appear in open set)
```

Quantity = `long_qty` or `short_qty` (the non-zero one). Compare in **venue native units** with `CTRADER_RECON_QTY_ABS_TOLERANCE` (default **0**). Do **not** convert to MT5 lots inside the comparer. Lot conversion is a sizing concern (A38 / §38), not a book-equality concern.

Symbol: snapshot `55` must equal `destination_symbols.instrument_id` for the row’s `canonical_symbol`. Unknown `55` → `ORDER_MISMATCH` / position issue with note `UNMAPPED_INSTRUMENT` (still blocking; do not guess XAU).

### 7.5 Issue emission

Comparer is **pure**. It does not write. For each finding it emits a fingerprint (A20):

```text
issue_fingerprint = sha256(
    issue_type | venue_id | dest_order_id | destination_position_id | cl_ord_id
)
```

Same break inside one run upserts. Across runs, a still-present break opens a **new** issue row pointing at the new `run_id`; the previous row is marked `RESOLVED` with `resolved_by_run_id` if the fingerprint **disappears**. Humans do not “resolve” by deleting.

| Finding | `issue_type` | Payload |
|---|---|---|
| Venue working order, no internal match | `ORDER_MISMATCH` | venue `37`/`11`/`54`/`151` |
| Internal working order, no venue match | `ORDER_MISMATCH` | our `cl_ord_id` |
| Matched order, `54` ≠ internal side | `SIDE_MISMATCH` | both sides |
| Matched order, `151`/`38` ≠ internal leaves/qty | `QUANTITY_MISMATCH` | both qtys |
| Venue `721` not in internal open (and not accepted-external) | `UNKNOWN_EXTERNAL_POSITION` | `721`, side, qty, `55` |
| Internal open `721` absent from venue | `MISSING_INTERNAL_POSITION` | our row |
| Matched position, side differs | `SIDE_MISMATCH` | |
| Matched position, qty differs beyond tolerance | `QUANTITY_MISMATCH` | |
| `35=8` `150=F` with no `cl_ord_id` / no intent | `ORPHAN_FILL` | |
| `35=8` we cannot attribute (bad `584`, unknown `11`+`37`) and not `150=I` of this run | `ORPHAN_EXECUTION_REPORT` | |
| Venue position qty > sum of applied fills we have for that `721`, or internal qty jumped with no Trade ER | `UNEXPECTED_FILL` | |
| `internal_unknown_orders` still unknown after snapshot (see §9) | `UNRESOLVED_EXECUTION_STATE` | `cl_ord_id` |

`ORPHAN_FILL` and `ORPHAN_EXECUTION_REPORT` are both required: A26 lists both; architecture §43 names “orphan execution report” and “unexpected fill”.

### 7.6 Clean book

Empty venue + empty internal + no unknown intents + no orphan fills in the lookback window (this run’s inbound tape) → **zero issues** → `SUCCESS`.

This is the **expected Phase 7** steady state while `REAL_COPY_EXECUTION_ENABLED=false` and nobody is trading the account by hand. A manual cTrader ticket on `1369850` **must** surface as `UNKNOWN_EXTERNAL_POSITION` / `ORDER_MISMATCH`. That is a feature.

---

## 8. Repair policy (what “repair/update state” means)

Architecture §42 says repair/update **then** READY only if reconciled. Repair is **not** “make the issue go away by overwriting.”

### 8.1 Safe (automatic, same transaction as run complete)

| Situation | Repair |
|---|---|
| Matched by `ClOrdID`; we lack `dest_order_id` / `721` | Copy `37` / `721` onto `fix_orders` / link |
| Matched; venue `150=I` leaves/cum/ord_status more specific | Update `fix_orders` leaves/cum/status; **do not** insert a fill |
| Matched position; we lack avg price / SL/TP | Update informational columns only |
| Venue `721` already referenced on **our** applied Trade ERs but `destination_positions` row missing | Insert open position from snapshot (we created it) |
| Internal open `721` absent **and** we have a terminal close/fill covering remaining qty | Set `is_open=false`, `closed_at=now`, `close_reason=VENUE_ABSENT_AFTER_FILLS` |
| Unknown intent: venue has this `ClOrdID` | Adopt venue state (A25 §5.4); clear unknown |
| Unknown intent: venue has no `ClOrdID` **and** position set equals pre-send expectation (no new `721`, no qty up) | Mark intent `not_on_venue` (terminal); **not** a resend |

### 8.2 Forbidden (never automatic)

| Situation | What humans / later phases may do |
|---|---|
| Unknown external position / order | SuperAdmin `ACCEPTED_EXTERNAL` **or** flatten under `EMERGENCY_FLATTEN` (not this increment) |
| Missing internal position we never filled | Investigate; do **not** delete the row without audit |
| Qty/side mismatch without a matching Trade ER | Issue only; do **not** overwrite qty to “match the venue” without an applied fill (that would hide `UNEXPECTED_FILL`) |
| `not_on_venue` | New `cl_ord_id` only after READY + risk + flags (Phase 8) |
| Any mismatch | `NewOrderSingle` / cancel / replace |

**Qty overwrite rule:** the only legal qty change on `destination_positions` is **sum of applied `150=F` LastQty** (signed by side) or an explicit audited close. Snapshot qty is used to **detect** drift, not to **set** the book.

### 8.3 Audit

Every applied repair writes `audit_logs` / `system_events` with `correlation_id`, `run_id`, `cl_ord_id`, `cserver_order_id`, `destination_position_id` (architecture §57). Never log `554` / passwords.

---

## 9. Unknown execution state (uses the same snapshots)

A25 §5.4 remains the recovery path. A47 adds the **book-level** contract so recovery cannot send.

```text
EXECUTION_STATE_UNKNOWN
    ↓
block additional 35=D for this intent (and globally if gate is not READY)
    ↓
35=H by ClOrdID
    ↓
if still unknown:
    this run’s 35=AF (or a dedicated UNKNOWN_RECOVERY run)
    ↓
this run’s 35=AN
    ↓
decide:
    venue has ClOrdID     → adopt; issue cleared
    venue has no ClOrdID
      AND no unexpected position/qty change
                          → not_on_venue
    anything else         → UNRESOLVED_EXECUTION_STATE; BLOCKED_INCONSISTENT
```

A dedicated `UNKNOWN_RECOVERY` run may run `35=H` first; it still **must not** skip AN. Replacement orders (new `cl_ord_id`) are **Phase 8** and require `SUCCESS` + flags.

---

## 10. `ReconciliationGate` (architecture §70.14)

Single function consulted by the send path and by A23 step 4:

```text
MaySendNewOrderSingle(venue, account) → Allow | Deny(reason)
```

`Allow` only if **all** are true:

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED          -- Phase 7: this is false; gate still exists
STOP_NEW_EXECUTION = false
lease owned + fence current
TRADE session LOGGED_ON
latest terminal STARTUP|POST_DISCONNECT|LEADERSHIP run = SUCCESS
no OPEN execution-impacting issues for (venue, account)
no execution_intents in unknown states for that account
last SUCCESS completed_at >= now - STALE_AFTER_SEC
intent.status = not_sent
```

Deny reasons map to A23 / A26: `RECONCILIATION_BLOCK`, `EXECUTION_STATE_UNKNOWN`, `NOT_RECONCILED`, `PRECONDITION_FAILED` (HTTP 412).

Logon **without** a completed mass-status + positions stays off READY (A25 test #10).

---

## 11. Durable schema (extends A20; do not invent a second pair of tables)

### 11.1 `execution_reconciliation_runs`

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | `run_id` |
| `venue_id` | uuid NOT NULL | FK `execution_venues` |
| `destination_account` | text NOT NULL | cTrader login |
| `run_type` | text NOT NULL | CHECK `STARTUP\|PERIODIC\|POST_DISCONNECT\|LEADERSHIP\|UNKNOWN_RECOVERY\|MANUAL` |
| `is_daily` | bool NOT NULL DEFAULT false | |
| `status` | text NOT NULL | CHECK §3.1 |
| `blocked_new_execution` | bool NOT NULL | true while `IN_PROGRESS` and on any non-SUCCESS |
| `ready_for_execution` | bool NOT NULL | result **after** complete; only true on `SUCCESS` |
| `mass_status_req_id` | text NOT NULL | persist-before-send |
| `pos_req_id` | text NOT NULL | persist-before-send |
| `mass_status_expected` | int NULL | from `911` |
| `mass_status_received` | int NOT NULL DEFAULT 0 | |
| `mass_status_empty_reject` | bool NOT NULL DEFAULT false | `35=j` empty-book |
| `pos_expected` | int NULL | from `727` |
| `pos_received` | int NOT NULL DEFAULT 0 | |
| `pos_req_result` | text NULL | `0` / `2` |
| `started_at` | timestamptz NOT NULL | |
| `af_sent_at` / `an_sent_at` | timestamptz NULL | |
| `completed_at` | timestamptz NULL | |
| `fail_reason` | text NULL | `TIMEOUT`, `INCOMPLETE_AF`, `INCOMPLETE_AN`, `LEADERSHIP_LOST`, `SESSION_DOWN`, `INCOHERENT_SNAPSHOT` |
| `issue_count` | int NOT NULL DEFAULT 0 | |
| `correlation_id` | uuid NOT NULL | |
| `fencing_token` | text NULL | lease at start; abort if changed |

Indexes: `(venue_id, started_at DESC)`, `(run_type, status)`, unique `(venue_id, mass_status_req_id)`, unique `(venue_id, pos_req_id)`.

**No** unique on “one success per day”. Many runs.

### 11.2 `execution_reconciliation_issues`

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `run_id` | uuid NOT NULL | FK runs |
| `venue_id` | uuid NOT NULL | |
| `issue_type` | text NOT NULL | §7.5 |
| `issue_fingerprint` | text NOT NULL | |
| `status` | text NOT NULL | |
| `cl_ord_id` | text NULL | |
| `dest_order_id` | text NULL | tag 37 |
| `destination_position_id` | text NULL | tag 721 |
| `instrument_id` | bigint NULL | tag 55 |
| `internal_side` / `external_side` | text NULL | |
| `internal_quantity` / `external_quantity` | numeric NULL | venue units |
| `note` | text NULL | no secrets |
| `detected_at` | timestamptz NOT NULL | |
| `acknowledged_at` / `acknowledged_by` | timestamptz / text | |
| `resolved_at` / `resolved_by_run_id` | | |
| `accepted_external` | bool NOT NULL DEFAULT false | only with `ACCEPTED_EXTERNAL` |

UNIQUE `(run_id, issue_fingerprint)`. Partial index `(venue_id, issue_type)` WHERE `status IN ('OPEN','ACKNOWLEDGED')`.

### 11.3 Snapshot tables (recommended; not named in §44)

Keep venue tape durable so a FAILED run is diagnosable:

- `execution_reconciliation_order_snapshots` — one row per mass-status `35=8` (`run_id`, `dest_order_id`, `cl_ord_id`, `ord_status`, `leaves_qty`, `cum_qty`, `side`, `instrument_id`, `pos_id`, raw hash).
- `execution_reconciliation_position_snapshots` — one row per `35=AP`.

If a later migration prefers JSONB on the run row, fine for v1 **only if** raw FIX is also in `fix_session_events` / a message log. Do not drop the only copy of `911`/`727`.

### 11.4 Adjacent columns the comparer needs

On `fix_orders`: `leaves_qty`, `cum_qty`, `ord_status`, `dest_order_id`, `destination_position_id`, `side`, `instrument_id`.

On `destination_positions`: `destination_position_id`, `is_open`, `side`, `quantity`, `avg_price`, `instrument_id`, `canonical_symbol`.

On `execution_intents`: `status` including `not_on_venue` (A25) in addition to A20’s list.

On `fix_sessions` (TRADE row): `ready_for_execution`, `last_reconciliation_run_id`, `last_reconciliation_at`, `recon_state`.

### 11.5 MT5 half of §54

`Last successful MT5 reconciliation` is **not** a cTrader TRADE snapshot. It is the source ingest reconcile (`A30` I3 / architecture §12 `Periodic Reconciliation` of broker history vs `mt5_deals` / `mt5_positions_current`).

Dashboard reads:

```text
mt5.lastSuccessfulAt  = latest SUCCESS checkpoint/run for stream positions_reconcile / deals
cTrader.lastSuccessfulAt = latest execution_reconciliation_runs
                            where run_type in (STARTUP, PERIODIC, POST_DISCONNECT, LEADERSHIP, MANUAL)
                              and status = SUCCESS
                              and ready_for_execution = true
```

Until Phase 7 exists, API returns `cTrader.lastSuccessfulAt = null`, `status = "NEVER"`, `readyForExecution = false` (A06 / A30). That is **not** a silent ignore: `openIssueCounts` may be empty, and the UI must show **Never / N/A**, not “healthy”.

---

## 12. Dashboard (§54) and API (align A26)

### 12.1 What the page must show

```text
Last successful MT5 reconciliation
Last successful cTrader reconciliation

Unknown external positions
Missing internal positions
Order mismatches
Quantity mismatches
Orphan fills
Unresolved execution states
```

Plus A26 extras that are the same family: `ORPHAN_EXECUTION_REPORT`, `UNEXPECTED_FILL`, `SIDE_MISMATCH`, and TRADE card “last reconciliation” (`A26` §6.11).

Empty issue list is allowed **only** when a run completed `SUCCESS` (or cTrader is `NEVER` and the banner says so). An empty list while TRADE is logged on and `lastSuccessfulAt` is null is a **bug** (missing run), not “all clear”.

### 12.2 Endpoints

| Method | Path | Role | Behaviour |
|---|---|---|---|
| `GET` | `/api/v1/reconciliation` | ReadOnly+ | A26 payload: mt5 + cTrader + `openIssueCounts` + newest open issues |
| `GET` | `/api/v1/reconciliation/runs` | ReadOnly+ | Paged runs (A06); include `runType`, `status`, `readyForExecution`, timestamps, `failReason`, `issueCount` |
| `GET` | `/api/v1/reconciliation/issues` | ReadOnly+ | Filter `type`, `status`; paging; **never drop OPEN** |
| `POST` | `/api/v1/reconciliation/issues/{issueId}/ack` | RiskManager+ | `OPEN → ACKNOWLEDGED`; does **not** set READY |
| `POST` | `/api/v1/reconciliation/issues/{issueId}/accept-external` | SuperAdmin | Only `UNKNOWN_EXTERNAL_POSITION`; sets `ACCEPTED_EXTERNAL`; still visible; next SUCCESS may READY if that `721` is excluded |
| `POST` | `/api/v1/reconciliation/run` | RiskManager+ | Enqueue `MANUAL`; 409 if `IN_PROGRESS`; does not invent fills |

Never return FIX passwords, `554`, or raw Logon (A26 §3).

While not READY, live-entry mutations return **412** `PRECONDITION_FAILED` (A26).

### 12.3 SignalR (A26)

`reconciliation.issue` on new/cleared issues; invalidate `['reconciliation']`. Do not poll secrets.

### 12.4 TRADE session card (§52)

`lastReconciliation: { at, status }` where `status` is `READY_FOR_EXECUTION` | `BLOCKED_INCONSISTENT` | `BLOCKED_PENDING_RECON` | `BLOCKED_STALE` | `NEVER`. `openOrders` / `openDestinationPositions` are **internal** counts after the last applied repair, with a subtitle if they disagree with the last venue snapshot (should be zero disagreement on SUCCESS).

---

## 13. Feature flags and Phase 7

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

| Flag off | Recon |
|---|---|
| `CTRADER_FIX_ENABLED` or TRADE session off | No AF/AN; gate `BLOCKED_NO_SESSION`; dashboard `NEVER` |
| `REAL_COPY_EXECUTION_ENABLED=false` | **Recon still runs.** This is Phase 7. |
| `CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=true` (A25) | **No** AF/AN; do not claim recon works |

Phase 7 exit (`A28` / §67 / §70.3 / §70.14):

```text
[ ] SSL TRADE session
[ ] OrderMassStatusRequest 585=7
[ ] RequestForPositions (all open)
[ ] ExecutionReport parser (including 150=I + 911 + 584)
[ ] PositionReport parser (710/721/727/728/704/705)
[ ] Reconciliation persist + gate
[ ] Position reports reconcile after simulated restart
[ ] Duplicate 35=8 / 35=AP idempotent
[ ] NewOrderSingle still compiled/flagged OFF
```

---

## 14. Metrics and logs

Architecture §58 has no recon series. Add (do not replace FIX counters):

```text
fix_recon_runs_total{run_type,status}
fix_recon_issues_open{issue_type}
fix_recon_snapshot_timeouts_total
fix_recon_ready                            # 0/1 gauge per venue
fix_recon_last_success_unixtime
fix_unknown_execution_states               # already in §58
```

Logs (structured, §57): `correlation_id`, `run_id`, `run_type`, `mass_status_req_id`, `pos_req_id`, `cl_ord_id`, `cserver_order_id`, `destination_position_id`, `fix_session=TRADE`, `fencing_token`, `ready_for_execution`. Redact passwords centrally.

---

## 15. Tests (must exist before any live AF/AN against Pepperstone)

Do **not** use account `1369850` as the first integration test (architecture §61). Use `OrderMassStatusReplayStub` + `PositionReportReplayStub` (`A27`).

| Test (A27 name) | Must prove |
|---|---|
| `Reconcile.StartupReconciliationGateTests` | After logon, send blocked until `SUCCESS` |
| `Reconcile.PositionReconciliationTests` | Unknown / missing / qty / side / orphan / unexpected fill |
| `Harness.StartupReconciliationAfterSimulatedRestartTests` | Restart → block → AF+AN tape → READY **or** stay blocked |
| `Harness.ReconciliationBlocksExecutionWhileInconsistentTests` | Dirty book → no `35=D` (§70.14) |
| `Reconcile.UnknownExecutionRecoveryTests` | `35=H` then AF/AN; adopt vs `not_on_venue`; no second `35=D` |
| `Harness.DuplicateExecutionReportTests` | Second identical `35=8` does not double-book |
| Empty-book AF | `35=j` + no ER → empty working set → SUCCESS if internal empty |
| Silent AF | timeout → `FAILED` → not READY |
| Empty-book AN | `728=2` `727=0` → empty positions |
| Incomplete AN | `727=2` but one `AP` → `FAILED` |
| `150=I` is not a fill | leaves update only |
| Manual cTrader position fixture | `UNKNOWN_EXTERNAL_POSITION` blocks READY |
| `ACK` does not READY | |
| `ACCEPTED_EXTERNAL` | that `721` excluded; other issues still block |
| Periodic stale watchdog | no SUCCESS for `STALE_AFTER_SEC` → `BLOCKED_STALE` |
| Leadership | winner reconciles before intents; loser cannot complete a run |
| Flag | TRADE read allowed; `NewOrderSingle` closed (`Flags.RealExecutionDisabledIntegrationTests`) |

Comparer unit tests take in-memory books — no QuickFIX required.

---

## 16. Suggested implementation order (coding task, not this file)

1. Domain enums + comparer + fingerprint + gate (pure). Tests first.
2. A20 tables + snapshot tables + migrations (`A03` still 0 migrations).
3. `IFixTradeReadGateway` + simulator stubs (`A27` §7.1).
4. `StartupReconciliationCoordinator` wired to TRADE `OnLogon` in FixWorker.
5. `PeriodicReconciliationCoordinator` timer.
6. API §12 + dashboard fields (cTrader `NEVER` until first run).
7. Unknown-state recovery reuse.
8. Only then consider Phase 8 send **behind** `REAL_COPY_EXECUTION_ENABLED`.

Do **not** modify product source from this task.

---

## 17. Open RoE risks (fail closed, do not paper over)

1. **Empty mass-status Text** is not specified — completeness is “`35=j` + zero matching ER”, not a string match.
2. **`728=2` meaning** is inferred from RoE comments (`VALID_REQUEST` vs omitted fields). Re-read the table at implementation; unknown `728` = `FAILED`.
3. **Whether type `7` includes historical orders** is not fully specified. Filter to working `39` for the open-order set.
4. **No official `35=AO`.** Completeness is `727`/`911` only.
5. **No ExecID on official `150=I`.** Do not force tag 17 uniqueness on status snapshots.
6. **Two TRADE sockets duplicate every report.** Lease bugs look like double fills; treat as P0.
7. **Same cTrader account used by a human** will always fail recon until `ACCEPTED_EXTERNAL` or a dedicated copy account. Prefer a dedicated account in ops.

---

## 18. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§28, 33–35, 41–44, 52, 54, 57–58, 61–63, 67–70
- https://help.ctrader.com/fix/specification/ — Order Mass Status Request, Execution Report (`584`/`911`/`150=I`), Request for Positions, Position Report, Business Message Reject
- https://help.ctrader.com/fix/faqs/ — duplicate reports; invalid FIX is silent
- `D:\Prop\reports\swarm\20260818\A05_fix_ctrader_audit.md`
- `D:\Prop\reports\swarm\20260818\A20_table_catalog.md`
- `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md`
- `D:\Prop\reports\swarm\20260818\A25_fix_session_spec.md`
- `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md`
- `D:\Prop\reports\swarm\20260818\A27_test_inventory.md`
- `D:\Prop\reports\swarm\20260818\A28_phases_gates.md`
- `D:\Prop\reports\swarm\20260818\A32_ctrader_fix_specification.md`
- `D:\Prop\reports\swarm\20260818\A34_ctrader_fix_faq.md`

**Product source under `D:\Prop\src` was not modified.**
