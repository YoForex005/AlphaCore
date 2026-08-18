# A42 — ClOrdID generation, persist-before-send, and `EXECUTION_STATE_UNKNOWN`

**Artifact:** `D:\Prop\reports\swarm\20260818\A42_clordid_idempotency.md`  
**Date:** 2026-08-18  
**Agent:** A42  
**Status:** design only — **no product source modified**  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§32–35, 41–45, 57–63, 67–72, 75  
**Official RoE (fetched 2026-08-18):** https://help.ctrader.com/fix/specification/  
**Official FAQ:** https://help.ctrader.com/fix/faqs/  
**Siblings this note binds to / refines:** A05, A08, A20, A23, A24, A25, A27, A28, A32, A34  

**Current measured code state:** `src/Fix.CTrader` and `apps/fix-worker` are empty scaffolds. There is **no** `ClOrdId` type, **no** `ExecutionIntent`, **no** persist-before-send, **no** unknown-state recovery. This file is the implementation contract for those pieces. Do not treat anything below as already built.

---

## 0. One-sentence law

Allocate a unique `ClOrdID` and persist the execution row **before** any TRADE socket write; if the process cannot prove cServer never saw that write, the row is `EXECUTION_STATE_UNKNOWN` and **must not** be retried as `NewOrderSingle` (`35=D`) — not with the same `ClOrdID`, and not with a new one — until reconciliation proves the venue outcome.

That is architecture §33 + §34 + senior rule §72.8–10, plus the official RoE sentence that unique client order IDs are required for Order Status to work.

---

## 1. Authority (quoted, not paraphrased away)

### 1.1 Architecture §33 — Idempotent Order Submission

> Every destination order must have a unique client order ID.

Persist before sending:

```text
execution_intent_id
cl_ord_id
source_broker_id
source_login
source_trade_id
source_event_id
destination_account
canonical_symbol
side
requested_quantity
created_at
status
```

The execution service must distinguish:

```text
not sent
sent but acknowledgement unknown
accepted
partially filled
filled
rejected
cancelled
```

> Never simply retry a NewOrderSingle because the TCP connection broke.  
> First reconcile.

### 1.2 Architecture §34 — Unknown Execution State

Critical case:

```text
send order
   ↓
network disconnects
   ↓
did cServer receive it?
```

> Do NOT blindly send the order again.

Set:

```text
EXECUTION_STATE_UNKNOWN
```

Then use:

```text
OrderStatusRequest
OrderMassStatusRequest
ExecutionReports
Position reconciliation
```

to determine the real state.

> Only after reconciliation may the system decide whether another order is required.

### 1.3 Adjacent architecture that this design must not violate

| Pin | Rule |
|---|---|
| §32 | Never send FIX from an MT5 callback. Persist `CopyIntent`, then risk, then `ApprovedExecutionIntent`, then the FIX worker. |
| §35 | Source trade ↔ dest orders ↔ dest `PosMaintRptID` is many-to-many over the lifecycle. |
| §41 | `REAL_COPY_EXECUTION_ENABLED=false` by default. TRADE session up ≠ license to send `35=D`. |
| §42 | After TRADE logon: block new executions → mass status → positions → repair → only then `READY_FOR_EXECUTION`. |
| §62 TRADE | Do not queue an unlimited stale-entry backlog. Do not resend unknown orders blindly. |
| §63 | Copy intents expire (`expires_at`, `max_signal_age`). Unknown **already-sent** orders do **not** expire into a resend. |
| §70.4 / §70.6 | Unique ClOrdID rules proven. Unknown-state recovery proven. |
| §72.8–10 | Every execution request idempotent. Never blindly retry a possibly-sent order. Reconcile after FIX reconnect. |

### 1.4 Official cTrader RoE (binding for wire format)

Quoted from the Order Status Request section:

> For a correct interaction, it is very important to have unique client order identifiers (`ClOrdID`) for all orders.

| Message | Tag 11 role | Tag 41 role |
|---|---|---|
| New Order Single `35=D` | **Required.** “A unique identifier of the order allocated by the client.” | n/a |
| Execution Report `35=8` | **Optional.** Same wording. Official market-fill examples echo it; the official **mass-status** example **omits** it (see §8.3). | n/a (cancel success ER may echo `41`) |
| Order Status Request `35=H` | **Required.** This is the **order’s** ClOrdID being queried, **not** a new request id. | n/a |
| Order Cancel Request `35=F` | **Required.** “A unique ID of the **cancel request** allocated by the client.” | **Required.** Original order’s ClOrdID |
| Order Cancel/Replace `35=G` | **Required.** “A unique ID of the **amend request** allocated by the client.” | **Required.** Original order’s ClOrdID |
| Order Mass Status `35=AF` | not used | n/a — uniqueness is `MassStatusReqID` (584) |
| Request for Positions `35=AN` | not used | n/a — uniqueness is `PosReqID` (710) |

Official NOS examples use numeric-looking ids (`11=876316397`). Official cancel/replace examples use mixed alphanumeric (`11=jR8dBPcZEQa9`, `41=n9Tm8x1AavO5`). Alphanumeric is therefore legal. There is **no published max length**; stay ≤ 32 printable ASCII, charset `[0-9A-Z]`, no SOH, `=`, `|`, or space.

Official connectivity: “All sides of a FIX session should have sequence numbers reset on establishing a FIX session” and Logon `141=Y`. After reconnect, **FIX resend cannot recover the original `35=D`**. Persist-before-send + status/position query is the only recovery.

Official FAQ (A34): two simultaneous connections cause the server to **copy** every report to each connection. A second `35=8` is not a second fill.

Official market `OrdType=1` is processed as **IOC**. A filled (or expired) market order will often **not** remain in the working-order book that mass status returns. That is the hard unknown-state case (§8.4).

Official Execution Report table does **not** list `ExecID` (17). Do not design primary ER idempotency on a tag the RoE does not promise (refine A20 — §10.3).

---

## 2. What is **not** implemented (honest)

| Needed type | Layer | Disk today |
|---|---|---|
| `ClOrdId` / `OrigClOrdId` | Domain | **MISSING** |
| `ExecutionState` | Domain | **MISSING** (A01 already listed it) |
| `ExecutionIntent` | Domain | **MISSING** |
| `ClOrdIdGenerator` | Application | **MISSING** |
| `IExecutionIntentStore` | Application | **MISSING** |
| persist-before-send in FIX worker | Application / Fix.CTrader | **MISSING** — worker is a 1 Hz log loop |
| `UnknownExecutionRecoveryService` | Application | **MISSING** |
| `execution_intents` / `fix_orders` tables | Infrastructure | **MISSING** (catalogued in A20 only) |

`apps/fix-worker/Worker.cs` is a `BackgroundService` that logs the clock. It must not be wired to a live TRADE port until this design, A25 session ownership, A23 risk, and the §61 harness exist.

---

## 3. Identity layers (do not collapse)

Four different identifiers. Mixing any two is how double-sends happen.

```text
source event
    ↓  unique (source_broker_id, source_login, source_event_id, exposure_class)
CopyIntent.copy_intent_id
    ↓  risk (A23) — one durable risk_decision per evaluation
risk_decision_id
    ↓  persist-before-send
ExecutionIntent.execution_intent_id   ← our PK (uuid)
ExecutionIntent.cl_ord_id             ← FIX tag 11, UNIQUE forever
    ↓  cServer, only after an ER
fix_orders.dest_order_id              ← FIX tag 37 OrderID
destination_positions.destination_position_id  ← FIX tag 721 PosMaintRptID
```

| Identity | Allocated by | Reuse? |
|---|---|---|
| `copy_intent_id` | us, when the source event becomes a candidate | Never. Same source event + exposure class upserts the **same** row. |
| `risk_decision_id` | us, one per evaluation | Never. A replacement order requires a **new** risk evaluation (A20 `execution_intents_risk_uk`, A23). |
| `execution_intent_id` | us, when risk approves / reduce-size | Never. 1:1 with `cl_ord_id`. |
| `cl_ord_id` | us, **deterministic from** `execution_intent_id`, **before** send | **Never**, including after terminal. Cancel/replace get a **new** intent + new `cl_ord_id`. Status request **reuses** the order’s `cl_ord_id` in tag 11 (it is a query, not an allocation). |
| `dest_order_id` (37) | cServer | Venue-assigned. Partial unique when not null (A20). |
| `destination_position_id` (721) | cServer | Venue-assigned. Hedged accounts may receive it on the first ER. |

**Copy-intent idempotency** (A27 `CopyIntentIdempotencyGuard`) is the **upstream** guard: one live copy intent per source event.  
**ClOrdID idempotency** is the **downstream** guard: one destination order attempt per approved risk decision, recoverable after crash.

They are not substitutes. A second `CopyIntent` for the same source event is a product bug. A second `35=D` for a possibly-sent `cl_ord_id` is a **live-account** bug.

---

## 4. ClOrdID generation

### 4.1 Requirements (testable)

`ClOrdIdGenerator` / `IClOrdIdFactory` (A05, A27) must prove:

1. **Unique per destination order request.** Two persisted intents never share `cl_ord_id`. Enforced by `execution_intents_clord_uk` (A20) **and** by the generator.
2. **Stable regeneration.** `From(execution_intent_id)` is a **pure function**. Replay, worker restart, or “generate again before send” yields the **same** string. This is how persist-before-send stays idempotent.
3. **Never reused after send.** No attempt-counter suffix on the same intent. A replacement is a **new** `execution_intent_id` → new `cl_ord_id`.
4. **FIX-safe.** Charset `0-9A-Z` (Crockford Base32, uppercase). Length 26 for live order ids. No SOH, `=`, `|`, space, or lowercase (avoid case-fold collisions).
5. **Not a sequence.** Do not use `MAX(cl_ord_id)+1`, time-only stamps, or QuickFIX `MsgSeqNum`. Sequences reset on every cTrader Logon (`141=Y`) and collide across process restarts.
6. **Not the source ticket.** MT5 tickets are per-broker and would leak source identity onto the venue. Destination id is ours.
7. **Same family as shadow.** A24: shadow uses the same generator with a reserved prefix so live and shadow never collide if someone loads both into one uniqueness test.

### 4.2 Algorithm (binding)

```text
execution_intent_id  : UUID v7 (time-ordered; PostgreSQL uuid)
cl_ord_id            : CrockfordBase32( execution_intent_id_16_bytes )
                       → exactly 26 characters, alphabet
                         0123456789ABCDEFGHJKMNPQRSTVWXYZ
                         (no I, L, O, U)
```

128 bits / 5 = 26 characters with 2 pad bits = 0. Collision resistance is that of the UUID. The DB unique constraint is the last line of defence, not the first.

```text
ClOrdIdGenerator.From(ExecutionIntentId id) -> ClOrdId     // pure
ClOrdIdGenerator.NewIntentId()              -> ExecutionIntentId  // uuid v7
```

Do **not** put an attempt, hostname, or sequence into `cl_ord_id`. Those belong in `fix_session_events` / audit.

### 4.3 Namespaces (other client ids)

These are **not** `ClOrdID` and **must not** be inserted into `execution_intents.cl_ord_id`.

| Kind | FIX tag | Format | Stored on |
|---|---|---|---|
| Live NOS / cancel / replace `ClOrdID` | 11 | 26-char Crockford(`execution_intent_id`) | `execution_intents.cl_ord_id` |
| Shadow simulated id | n/a | `SHDW` + 26-char Crockford | `shadow_orders.cl_ord_id` (A24 used `SHDW-`; **this note drops the hyphen** so the charset stays `[0-9A-Z]`. Implement one charset in both tables.) |
| Order Status Request | 11 | **Reuse** the order’s `cl_ord_id` | no new allocation |
| `MassStatusReqID` | 584 | `M` + 26-char Crockford(new uuid) | `execution_reconciliation_runs.mass_status_req_id` |
| `PosReqID` | 710 | `P` + 26-char Crockford(new uuid) | `execution_reconciliation_runs.pos_req_id` |
| `SecurityReqID` | 320 | `L` + 26-char Crockford(new uuid) | security-list request log |
| `MDReqID` | 262 | `Q` + 26-char Crockford(new uuid) | quote subscription state |

Cancel (`35=F`) and replace (`35=G`) allocate a **new** `execution_intent_id` of `request_kind = cancel | replace`, new `cl_ord_id`, and `orig_cl_ord_id` = parent’s `cl_ord_id` (tag 41). Prefer also sending venue `OrderID` (37) when known — RoE: “A preferable method to use” on replace.

### 4.4 Value object rules

```text
ClOrdId
  - immutable
  - constructed only via ClOrdIdGenerator or a trusted persist-reload
  - equality = ordinal string equality
  - ToFixTag11() = the 26-char value
  - reject empty, wrong length, wrong alphabet

OrigClOrdId
  - same type family as ClOrdId
  - must already exist as some execution_intents.cl_ord_id
```

Never concatenate `SenderCompID`, login, or symbol into the id.

### 4.5 What the generator must refuse

| Input | Result |
|---|---|
| Empty / default UUID | throw (fail closed) |
| Request to “mint a new ClOrdID for an existing intent” | return the existing deterministic value; do not mint |
| Request to reuse a **terminal** intent’s id for a new send | refuse; caller must create a new intent |
| Shadow id passed to a live send path | refuse |

---

## 5. Persist-before-send protocol

This is the only legal path from an approved risk decision to a TRADE write. Risk does not talk to the socket (A23 §4.2).

### 5.1 States (binding; refines A25)

Architecture §33 lists seven operational states. Architecture §34 names a **separate** post-loss state. A20 and A27 already list both. **A25 §5.2 collapsed `sent_ack_unknown` with `EXECUTION_STATE_UNKNOWN`.** That collapse is **rejected** here:

- Treating every in-flight IOC as venue-blocking unknown would freeze the book on every market order for the ack RTT.
- `sent_ack_unknown` is **normal** “we wrote, we are waiting for `35=8` on the **same** session.”
- `EXECUTION_STATE_UNKNOWN` is **abnormal** “we wrote or may have written, and we **lost the observer** (disconnect, crash, lease loss).”

| Status (persist) | Meaning | May send `35=D`? | How it leaves |
|---|---|---|---|
| `not_sent` | Row durable. `send_armed_at` is null. Process has **never** called the TRADE write API for this id. | **Yes, once**, if gates still pass | Arm (below) or abandon / expire **without** a write |
| `sent_ack_unknown` | Write armed and/or bytes handed to the socket. Session still the one that armed. No terminal ER yet. | **No** | ER on this session → accepted/partial/filled/rejected/cancelled/expired. Session loss / timeout / crash → `EXECUTION_STATE_UNKNOWN` |
| `accepted` | ER `OrdStatus=0` / `ExecType=0` (or status `I` with working New) | No (except cancel/replace as **new** intents) | later ERs |
| `partially_filled` | `OrdStatus=1` | No | fill / cancel remainder |
| `filled` | `OrdStatus=2` | No | terminal |
| `rejected` | `OrdStatus=8` / `ExecType=8` / business reject of **this** ClOrdID | No | terminal |
| `cancelled` | `OrdStatus=4` / `ExecType=4` | No | terminal |
| `expired` | `OrdStatus=C` / `ExecType=C` (IOC leftover) | No | terminal |
| `EXECUTION_STATE_UNKNOWN` | Observer lost after a possible send | **No** | **Only** §8 recovery |
| `not_on_venue` | Recovery proved venue never accepted this ClOrdID **and** positions did not move for it (A25 `not_on_venue`) | **No** (this row). A **new** intent may be created only under §9 | terminal for **this** ClOrdID |
| `abandoned` | `not_sent` cancelled by expiry / flag / kill **before** arm | No | terminal |

`not_sent` is the **only** state that may emit the **first** `NewOrderSingle` for that `cl_ord_id`.

### 5.2 Required persist columns (union of §33, A20, A23, A25)

```text
execution_intent_id          uuid PK
cl_ord_id                    text UNIQUE NOT NULL
copy_intent_id               uuid NOT NULL
risk_decision_id             uuid UNIQUE NOT NULL
request_kind                 nos | cancel | replace
orig_cl_ord_id               text NULL          -- tag 41 when cancel/replace
source_broker_id             uuid NOT NULL
source_login                 bigint NOT NULL
source_trade_id              uuid NOT NULL
source_event_id              text NOT NULL
destination_account          text NOT NULL
venue_id                     uuid NOT NULL
canonical_symbol             text NOT NULL
destination_symbol_id        text NOT NULL      -- Spotware numeric id, not "XAUUSD"
side                         buy | sell
requested_quantity           numeric NOT NULL   -- destination units
expected_destination_price   numeric NULL
exposure_class               open | increase | reduce | close
created_at                   timestamptz NOT NULL
status                       see §5.1
send_armed_at                timestamptz NULL   -- set in the arm transaction
sent_at                      timestamptz NULL   -- engine reported write completed
unknown_since                timestamptz NULL
unknown_reason               text NULL
fencing_token                bigint NOT NULL    -- A25 lease
fix_session_key              text NOT NULL
correlation_id               uuid NOT NULL
pre_send_book_fingerprint    text NOT NULL      -- §5.4
cserver_order_id             text NULL          -- tag 37 once known
destination_position_id      text NULL          -- tag 721 once known
superseded_by_intent_id      uuid NULL          -- if a replacement is later created
```

Unique constraints (A20, extended):

```text
execution_intents_clord_uk          (cl_ord_id)
execution_intents_risk_uk           (risk_decision_id)
execution_intents_live_copy_uq      (copy_intent_id)
  WHERE status IN (
    'not_sent','sent_ack_unknown','accepted',
    'partially_filled','EXECUTION_STATE_UNKNOWN'
  )
```

The partial unique means: one **live** attempt per copy intent. Terminal (`filled` / `rejected` / `cancelled` / `expired` / `not_on_venue` / `abandoned`) frees the slot so a **replacement** row can exist. `EXECUTION_STATE_UNKNOWN` **holds** the slot — that is the point.

`fix_orders` is the venue-facing projection of the same `cl_ord_id` (A20). Insert it in the **same** arm transaction so a crash cannot leave an intent without an order row.

### 5.3 Two commits, then a write (exact order)

```text
[T0] Risk APPROVE / REDUCE_SIZE
        ↓
[T1] BEGIN
        insert execution_intents
            status           = not_sent
            cl_ord_id        = From(execution_intent_id)
            send_armed_at    = NULL
            fencing_token    = current TRADE lease
        -- do NOT insert fix_orders yet (optional) or insert status=not_sent
     COMMIT
        ↓
     Intent is now crash-safe and **unsent**.
     Worker may die here. Restart: still not_sent → may proceed to T2.
        ↓
[T2] Re-check gates immediately before write (A23 §5, A25 §6.3):
        REAL_COPY_EXECUTION_ENABLED
        TRADE logged on + lease token still current
        READY_FOR_EXECUTION
        no unresolved EXECUTION_STATE_UNKNOWN on this venue
        copy_intent not expired
        kill switch
        database available
     If any fail: leave not_sent or mark abandoned. Do not write.
        ↓
[T3] BEGIN
        UPDATE execution_intents
           SET status        = sent_ack_unknown
             , send_armed_at = now()
             , pre_send_book_fingerprint = <snapshot>
         WHERE execution_intent_id = $1
           AND status = not_sent
           AND send_armed_at IS NULL
           AND fencing_token = $current
        -- 0 rows updated → another worker armed or fenced. STOP. Do not write.
        upsert fix_orders (cl_ord_id, status=sent_ack_unknown)
     COMMIT
        ↓
     From this instant the row is a **possibly-sent** order even if the
     process dies before the next line. Restart must treat it as
     EXECUTION_STATE_UNKNOWN if the session is gone, or wait for ER
     if this same session is still the owner and still logged on.
        ↓
[T4] Socket write of 35=D (QuickFIX Send)
        on local throw before any byte left the buffer:
            still sent_ack_unknown (TCP may have sent). Do NOT revert to not_sent.
            if session still up: stay sent_ack_unknown and wait for ER / timeout.
            if session dead: promote to EXECUTION_STATE_UNKNOWN.
        on Send() returning:
            set sent_at = now() (best-effort persist; missing sent_at ≠ not_sent)
        ↓
[T5] ExecutionReport(s) on this session
        persist fix_execution_reports (idempotent)
        advance status per §7
```

**Illegal:** generate `cl_ord_id` in memory → `Send` → persist. A crash between those two is an unrecoverable double-risk (A05).  
**Illegal:** persist `not_sent` → `Send` → persist `sent_ack_unknown`. A crash between send and the second persist looks like `not_sent` and will **resend**. That is the §34 bug.  
**Required:** **arm** (`sent_ack_unknown` + `send_armed_at`) **commits before** `Send`.

The accepted cost of T3-before-T4: a crash between COMMIT and `Send` produces a false `EXECUTION_STATE_UNKNOWN` for an order that never left the box. Recovery may later mark `not_on_venue` and, only then, allocate a **new** ClOrdID. Architecture prefers a missed copy over a double order (§72.9).

### 5.4 Pre-send book fingerprint

Market IOC + sequence reset makes “order not found” after reconnect **ambiguous** (filled-and-dropped vs never-received). The arm transaction **must** snapshot enough destination state to compare later:

```text
pre_send_book_fingerprint =
  hash(
    venue_id,
    destination_account,
    canonical_symbol,
    list of (destination_position_id, side, qty) for that symbol,
    dest gross, dest net
  )
```

Store the raw list in `execution_intent_book_snapshots` (child of the intent) if a hash-only compare is too weak for ops. Hash is the minimum.

Without this snapshot, recovery **cannot** legally emit `not_on_venue` for a market order. It must stay `EXECUTION_STATE_UNKNOWN` and block.

### 5.5 Who may transition `not_sent` → `sent_ack_unknown`

Only the FIX execution worker that **holds the current TRADE fencing token** (A25 §4). Risk, API, MT5 ingest, and the quote session have no write path to TRADE.

If the fencing token in the row ≠ current lease at T3, the update matches 0 rows. Drop the send.

---

## 6. Copy-intent idempotency (upstream of ClOrdID)

Not a second ClOrdID scheme. It is the reason we ever create an intent.

```text
Source MT5 event
    → Copy candidate?                         §32
    → persist CopyIntent                      unique (broker, login, event, exposure)
    → RiskEngine                              A23; persist risk_decision
    → persist ExecutionIntent not_sent        this note
    → FIX worker arm + send
```

| Replay | Required outcome |
|---|---|
| Same source event delivered twice | One `copy_intents` row. Second insert is a no-op / upsert. |
| Worker crashes after CopyIntent persist, before risk | Risk runs once; one `risk_decisions` row. |
| Worker crashes after ExecutionIntent `not_sent`, before T3 | Restart re-reads `not_sent`, re-checks gates, arms **same** `cl_ord_id`. |
| Worker crashes after T3, before/during T4 | `sent_ack_unknown` or `EXECUTION_STATE_UNKNOWN`. **No** second `35=D`. |
| Duplicate ExecutionReport (FAQ multi-connect or replay) | Second persist of `fix_execution_reports` is a no-op. Fills not double-booked. |

`expires_at` / `max_signal_age` apply to **creating or sending** new exposure. They do **not** authorize abandoning an unknown order.

---

## 7. ExecutionReport transitions (same session)

Legal edges for `IExecutionReportApplier` (A05). Duplicate ERs are no-ops.

```text
not_sent ──(T3 arm)──► sent_ack_unknown
                            │
                            ├─ 150=0 / 39=0 ──────────────► accepted
                            ├─ 150=F / 39=1 ──────────────► partially_filled
                            ├─ 150=F / 39=2 ──────────────► filled
                            ├─ 150=8 / 39=8 ──────────────► rejected
                            ├─ 150=4 / 39=4 ──────────────► cancelled
                            ├─ 150=C / 39=C ──────────────► expired
                            ├─ 150=5 (replace ack) ───────► accepted (qty/px from ER)
                            ├─ 150=I (status) ────────────► map 39 the same way
                            └─ session/lease/timeout ─────► EXECUTION_STATE_UNKNOWN

accepted ──► partially_filled ──► filled
accepted ──► filled
accepted ──► cancelled | expired | rejected
partially_filled ──► filled | cancelled (remainder dead, 39=4)

EXECUTION_STATE_UNKNOWN ──(§8 only)──► accepted | partial | filled
                                       | rejected | cancelled | expired
                                       | not_on_venue
                                       | stay unknown (still blocked)
```

Forbidden:

- `EXECUTION_STATE_UNKNOWN` → `not_sent`
- `sent_ack_unknown` → `not_sent`
- any state → `not_sent`
- `filled` / `rejected` / `cancelled` → any live send of the **same** `cl_ord_id`
- applying an ER whose `ClOrdID` (11) does not match a persisted row **as a fill of some other intent** (orphan ER → reconciliation issue, A20/A43)

If ER arrives **without** tag 11 (mass-status example): do **not** guess. Park the raw row keyed by `OrderID` (37) + `MassStatusReqID` (584) and let recovery bind it (§8.3).

IOC market path we expect on a healthy session (official example `11=876316397`):

```text
sent_ack_unknown → accepted (39=0, 150=0, 151=qty) → filled (39=2, 150=F, 151=0)
```

Timeouts on `sent_ack_unknown` while the session is **still up**:

| OrdType | Suggested first action (config, not code constants) | Then |
|---|---|---|
| Market IOC | short ack timeout (measured; start ~2–5 s) | `35=H` with **same** ClOrdID. Do **not** promote to venue-unknown yet if the session is healthy. |
| Limit / Stop GTC | longer working-order timeout | `35=H`; stay `accepted`/`sent_ack_unknown` if still working |

If `35=H` itself gets no answer and the session drops → `EXECUTION_STATE_UNKNOWN`.

---

## 8. Disconnect after send = `EXECUTION_STATE_UNKNOWN`

### 8.1 Entry conditions (any one)

Promote `sent_ack_unknown` (and any `accepted` / `partially_filled` that lost the session **before** a terminal ER, if you cannot prove the book) to `EXECUTION_STATE_UNKNOWN` when:

1. TRADE socket dies after T3 (arm) or T4 (write), before a **terminal** ER.
2. Process crashes after `send_armed_at` is set. On boot, if the session that armed is not this process’s current lease+logon, treat as unknown. (A25: crash between persist and write is unknown, not `not_sent`.)
3. TRADE lease lost after arm (A25 §4.3). The loser **must not** reconnect or send.
4. Logon of the **next** session (`141=Y` sequence reset) while this ClOrdID is still non-terminal.
5. `35=D` / `F` / `G` write return value is success **or indeterminate**.
6. An ER cannot be bound uniquely to the persisted `cl_ord_id` (duplicate/out-of-order without a stable key) — stay or enter unknown; do not invent a fill.

Set `unknown_since`, `unknown_reason`, increment `fix_unknown_execution_states` (§58).  
Risk input `unresolved_execution_states` becomes true (A23 §3.6).  
`READY_FOR_EXECUTION` is **cleared**. New `OPEN`/`INCREASE` are rejected `EXECUTION_STATE_UNKNOWN` / `RECONCILIATION_BLOCK` (A23 §5.4). Reduce/close of **other**, **known** mapped positions may still be allowed by kill-switch policy; they still use persist-before-send and **new** ClOrdIDs.

### 8.2 What is illegal in this state

```text
catch (IOException) { SendNewOrderSingle(sameClOrdId); }   // ILLEGAL
catch (IOException) { SendNewOrderSingle(newClOrdId); }    // ILLEGAL
on reconnect: drain not_sent AND sent_ack_unknown as a send queue  // ILLEGAL
on CopyIntent expiry: resend the unknown order                     // ILLEGAL
```

`not_sent` rows (never armed) are **not** unknown. They may still be sent after `READY_FOR_EXECUTION` returns, subject to expiry. They are not a backlog to flush blindly after a 3-minute outage (§63).

### 8.3 Recovery protocol (only legal exit)

Aligns with A25 §5.4 and architecture §34 / §42, with two RoE-driven corrections.

```text
any EXECUTION_STATE_UNKNOWN
        ↓
block additional 35=D for this intent AND for new exposure on the venue
        ↓
TRADE logged on + lease owned (new owner follows A25 §4.4)
        ↓
1. OrderStatusRequest (35=H)
      tag 11 = the unknown cl_ord_id
      tag 54 = side if we have it (optional in RoE)
        ↓
2. If H does not resolve (no ER, or business reject):
      OrderMassStatusRequest (35=AF)
      MassStatusReqType (585) = 7          -- only official value
      MassStatusReqID (584)   = new M+crockford
        ↓
3. Consume 35=8 (including ExecType=I) and persist raw
        ↓
4. RequestForPositions (35=AN)
      PosReqID (710) = new P+crockford
      no 721 → all open positions
        ↓
5. Compare:
      fix_orders + execution_intents
      destination_positions
      pre_send_book_fingerprint
        ↓
6. Decide per §8.5
```

**Correction vs a naive “match mass status on ClOrdID”:** the official mass-status ER example **does not include tag 11**:

```text
8=FIX.4.4|9=199|35=8|…|14=0|37=635|38=100000|39=0|40=2|44=1.35265|54=2|55=1|59=1|
60=20170404-07:20:44.582|150=I|151=100000|584=mZzEY|721=617|911=1|10=152|
```

So:

- `35=H` is the **primary** ClOrdID probe. Official H response **does** echo `11`.
- Mass status is a **book dump**. Bind those ERs by `OrderID` (37) + `PosMaintRptID` (721) + side/qty/time **after** H (or a later ER) has associated 37↔11.
- Empty book may be `BusinessMessageReject` (`35=j`) rather than zero ERs (RoE). That is **not** by itself `not_on_venue` for an IOC.

**Correction vs A20 `fix_execution_reports_exec_uk (venue_id, exec_id)`:** RoE ER table has **no** tag 17. Dedup fingerprint until proven otherwise:

```text
(venue_id,
 cl_ord_id,                  -- nullable
 dest_order_id,              -- tag 37
 exec_type,                  -- 150
 ord_status,                 -- 39
 cum_qty, last_qty, leaves_qty,
 transact_time,
 mass_status_req_id)         -- 584 when present
```

If a live ER includes 17, store it opportunistically and unique **partially** `WHERE exec_id IS NOT NULL`. Do not fail ingest when 17 is absent.

### 8.4 Market IOC — the hard case

After disconnect, a market order that cServer **did** take is typically **gone** from the working book (IOC fill or expire). Then:

| Observation | Interpretation | Next status |
|---|---|---|
| `35=H` returns working/partial/filled ER with our `11` | venue has it | adopt 39/150 |
| `35=H` / cancel-style text `ORDER_NOT_FOUND:Order with clientOrderId=… not found` **and** destination qty on that symbol/side increased by `requested_quantity` vs pre-send snapshot | filled, then dropped from book | `filled` (bind 721 if present on positions) |
| `ORDER_NOT_FOUND` **and** a new unmatched `destination_position_id` with matching side/qty appeared | filled into a new hedge position | `filled` + create link (§35) |
| `ORDER_NOT_FOUND` **and** fingerprint **unchanged** **and** mass status complete **and** no unmatched fill-sized position | never accepted (or rejected and dropped) | `not_on_venue` |
| `ORDER_NOT_FOUND` **and** fingerprint moved by a **different** qty/side, or snapshot missing | cannot tell | **stay** `EXECUTION_STATE_UNKNOWN`, human + `execution_reconciliation_issues` |
| `35=j` `BusinessRejectRefID` = our ClOrdID, text reject | rejected | `rejected` |
| Positions moved, no way to attribute to this ClOrdID vs another unknown | conflict | stay unknown, `BLOCKED_INCONSISTENT` |

Limit/stop GTC is easier: mass status / H should still list working orders with 37, 39=0, 151>0. Official H example echoes `11=876316400`.

### 8.5 Decision table (after §8.3 completes)

| Venue | Book vs snapshot | Intent becomes | New `35=D`? |
|---|---|---|---|
| Has this ClOrdID (via H or bound 37) | n/a | adopt venue 39/150 | **No** |
| No ClOrdID, positions unchanged, mass-status complete, snapshot present | match | `not_on_venue` | **Only** via §9 |
| No ClOrdID, positions changed, attributable | treat as fill/partial | `filled` / `partially_filled` | **No** |
| No ClOrdID, positions changed, **not** attributable | stay unknown | `EXECUTION_STATE_UNKNOWN` | **No** |
| Recon incomplete / TRADE down | stay unknown | `EXECUTION_STATE_UNKNOWN` | **No** |

A25’s phrase “linked to the same `execution_intent_id`” for a replacement is **rejected**. A20: replacement is a **new row** and new `cl_ord_id`. The old row stays as forensic truth (`not_on_venue` / `rejected`). Link via `copy_intent_id` + `superseded_by_intent_id`.

---

## 9. When a **replacement** order is allowed

All of the following:

1. Original intent is **`not_on_venue`** (not merely unknown).
2. `READY_FOR_EXECUTION` is true (recon clean, no **other** unknowns).
3. A **new** `risk_decision` is persisted. The old approval is dead (A20 unique on `risk_decision_id`; A23: re-check stale quote / stale signal).
4. The `CopyIntent` is still unexpired. If `expires_at` passed, **stop**. That is the §63 “20 trades during a 3-minute outage” rule.
5. New `execution_intent_id`, new `cl_ord_id` = `From(new id)`.
6. `request_kind = nos`. `orig_cl_ord_id` may point at the failed id for audit, but tag 41 is **not** sent on `35=D`.
7. `REAL_COPY_EXECUTION_ENABLED` and all A25 send conjunctions.

Replace (`35=G`) is **not** this path. `35=G` amends a **known working** order and still allocates a new ClOrdID + OrigClOrdID.

---

## 10. Cancel / replace identity

| Action | Persist first | Tag 11 | Tag 41 | Tag 37 |
|---|---|---|---|---|
| Cancel working order | new intent `request_kind=cancel`, status `not_sent` | **new** ClOrdID | original ClOrdID | send if known |
| Replace qty/px | new intent `request_kind=replace` | **new** ClOrdID | original ClOrdID | “preferable” if known |
| Status | no new intent | **original** ClOrdID | n/a | n/a |

Same T3-before-T4 arm protocol. Disconnect after cancel/replace write → `EXECUTION_STATE_UNKNOWN` on the **cancel/replace intent**, and the **parent** stays at last known 39 until H/mass-status resolves both.

Official cancel success ER: `11=<cancel id>`, `41=<orig>`, `150=4`, `39=4`.  
Official cancel miss: `35=j` text `ORDER_NOT_FOUND:Order with clientOrderId=<orig> not found.` Treat as recon input, not as a license to send `35=D`.

---

## 11. Duplicate reports and single TRADE owner

FAQ (verbatim, A34):

> FIX API reports will be duplicated if you have multiple connections to the API open simultaneously. The server will send a copy of the FIX response to each active connection.

Implications for this design:

1. **One production TRADE owner** per destination account (A25 lease). Two TRADE sockets make every fill look like two fills if the handler is naive.
2. Inbound idempotency is **not** “ClOrdID seen once.” It is the ER fingerprint in §8.3. The same `ClOrdID` will legitimately appear on New, Trade, Status, and the FAQ copy.
3. Do not open a second TRADE “just to query status.” Query on the owner session.
4. QUOTE + TRADE is the documented split, not two TRADE connections.

---

## 12. QuickFIX/n integration points (no engine from `TcpClient`)

A05 / official send-recv article: the Spotware `TcpClient` sample is **not** an engine. Use QuickFIX/n with a cTrader RoE dictionary (A25, A32).

| Callback / hook | Persist-before-send duty |
|---|---|
| Application code **before** `Session.Send(newOrderSingle)` | T2 gates + T3 arm must have **committed** |
| `ToApp` | may stamp `sent_at`; must not be the first persist of the id |
| `FromApp` `35=8` | persist raw ER, apply §7, never send |
| `OnLogout` / socket fail | promote in-flight armed intents to `EXECUTION_STATE_UNKNOWN` |
| `OnLogon` | **do not** send. Run §42 / A25 §4.4 recon. `141=Y` means the previous `35=D` will **not** be resent by the session layer — our DB is the memory. |
| FileStore / SqlStore | session sequence only. **Not** the order source of truth. Reset with the session. |

Never configure QuickFIX to retransmit application `35=D` after a new Logon as a substitute for this design.

---

## 13. Schema contract (delta on A20)

Keep A20 table names. Add / clarify:

| Object | Change vs A20 |
|---|---|
| `execution_intents.status` | Enum includes `expired`, `not_on_venue`, `abandoned`, and **distinct** `sent_ack_unknown` vs `EXECUTION_STATE_UNKNOWN` |
| `execution_intents.send_armed_at` | **Required** for the T3 protocol |
| `execution_intents.pre_send_book_fingerprint` | **Required** for IOC unknown recovery |
| `execution_intent_book_snapshots` | Optional child; raw dest positions at arm |
| `execution_intents.request_kind` / `orig_cl_ord_id` | Cancel/replace |
| `execution_intents_live_copy_uq` | Partial unique on `copy_intent_id` for live statuses |
| `fix_execution_reports` unique | Do **not** require `exec_id`. Use fingerprint unique; partial unique on `exec_id` if present |
| `client_request_ids` (optional) | Ledger of 584/710/320/262 allocations so operators do not reuse them as ClOrdIDs |

`fix_orders.cl_ord_id` UNIQUE and FK to `execution_intents.cl_ord_id` remains.

---

## 14. Types to add later (do not add in this task)

When a coding task is opened, create these — **not now**:

```text
src/Domain/Identifiers/ClOrdId.cs
src/Domain/Identifiers/OrigClOrdId.cs
src/Domain/Identifiers/ExecutionIntentId.cs
src/Domain/Enums/ExecutionState.cs
src/Domain/Enums/ExecutionRequestKind.cs
src/Domain/Execution/ExecutionIntent.cs
src/Domain/Execution/FixOrder.cs
src/Domain/Execution/FixExecutionReport.cs

src/Application/Execution/ClOrdIdGenerator.cs
src/Application/Execution/IExecutionIntentStore.cs
src/Application/Execution/FixOrderPersistBeforeSend.cs
src/Application/Execution/ExecutionReportStateMachine.cs
src/Application/Execution/UnknownExecutionRecoveryService.cs
src/Application/Execution/ReconciliationGate.cs

src/Execution/   (architecture §66; currently absent — A01/A25)

tests/Unit/Execution/ClOrdIdGenerationTests.cs
tests/Unit/Execution/UnknownExecutionStateTests.cs
tests/Unit/Execution/ExecutionReportStateTransitionTests.cs
tests/Fix/Harness/DisconnectAfterNewOrderSingleTests.cs
tests/Fix/Harness/UnknownStateRecoveryTests.cs
tests/Fix/Harness/UniqueClOrdIdUnderRetryTests.cs
```

This A42 task **must not** create those files.

---

## 15. Tests that lock this design (from A27 / §60–61 / §70)

| Class | Must prove |
|---|---|
| `Execution.ClOrdIdGenerationTests` | Unique; `From(id)` stable; alphabet/length; live vs `SHDW` disjoint; refuse default UUID |
| `Harness.UniqueClOrdIdUnderRetryTests` | Reconnect / retry never reuses a sent id; replacement only after `not_on_venue` |
| `Execution.UnknownExecutionStateTests` | Disconnect after send → `EXECUTION_STATE_UNKNOWN`; **no** blind `35=D` |
| `Harness.DisconnectAfterNewOrderSingleTests` | Same; simulator drops after `35=D` |
| `Harness.UnknownStateRecoveryTests` | H → AF → AN; adopt or `not_on_venue`; new ClOrdID only then |
| `Execution.CopyIntentIdempotencyTests` | Same source event ≠ second live intent |
| `Execution.ExecutionReportStateTransitionTests` | Graph in §7; duplicate ER no double fill |
| `Harness.DuplicateExecutionReportTests` | FAQ copy ≠ second fill |
| `Harness.OrderRejectLifecycleTests` | Reject terminal; no duplicate ClOrdID retry |
| `Reconcile.UnknownExecutionRecoveryTests` | Integration: status/positions; replacement only after prove-absent |
| `Risk.RiskEngineHardLimitTests` | Unresolved unknown → no new open (A23) |

Harness **before** any real `NewOrderSingle` (§61). Do not use account `1369850` as the first test.

---

## 16. Logging and metrics

§57 identifiers on every persist, send, ER, and recovery step:

```text
correlation_id
broker_id / source_login / source_trade_id
copy_intent_id
risk_decision_id
execution_intent_id
cl_ord_id
cserver_order_id
destination_position_id
fix_session
fencing_token
status
unknown_reason
```

Never log tag 554 (password).

§58:

```text
execution_orders_total
execution_fills_total
execution_rejections_total
fix_execution_reports_total
fix_unknown_execution_states          -- increment on enter, not on every poll
fix_business_rejects_total
```

---

## 17. Conflicts with sibling swarm notes (explicit)

| Sibling | Tension | This note’s ruling |
|---|---|---|
| A25 §5.2 | Equates `sent_ack_unknown` with `EXECUTION_STATE_UNKNOWN` | **Split.** In-session wait vs observer-lost. A20/A27 already split. A25 recovery text still applies to the **unknown** state. |
| A25 §5.4 | Replacement “linked to the same `execution_intent_id` if policy allows” | **New row**, new id (A20). Old row remains. |
| A20 | `fix_execution_reports` UNIQUE on `exec_id` | **Do not require** tag 17. Fingerprint unique; optional partial unique on 17. |
| A24 | Shadow prefix `SHDW-` | Same family; **drop hyphen** (`SHDW` + 26) so charset stays `[0-9A-Z]`. Update A24 when that spec is revised. |
| A05 | Factory “ULID / execIntentId + attempt” | ULID-like encoding of the intent UUID is correct. **No attempt suffix.** |
| A23 | `cl_ord_id` “generated here or by execution service before send” | Generated **when the execution intent is inserted** (`From(id)`), not in the socket layer. |

Architecture text wins over sibling wording when they disagree; this note records the resolution so implementers do not have to guess.

---

## 18. Phase placement

| Phase (A28 / §67) | ClOrdID / persist work |
|---|---|
| Phase 5 Shadow | Generator exists; shadow ids use `SHDW` prefix; no TRADE send |
| Phase 7 TRADE read | Generator + unique constraint exist **even while send is disabled** (§70.4 / A28). Recovery **designed**. Mass status / H / AN implemented as **read** paths. |
| Phase 8 Live send | T3-before-T4, state machine, unknown recovery **proven** on the §61 harness. Flag still default OFF. |

Do not implement live `35=D` in order to “try the generator.”

---

## 19. Worked example (disconnect after send)

```text
execution_intent_id = 0191f0a2-9c3e-7d11-8a44-0123456789ab
cl_ord_id           = Crockford(that uuid)          -- 26 chars, say 01J8H2K4...
status              = not_sent                      -- T1 commit

T3 commit           status=sent_ack_unknown
                    send_armed_at=2026-08-18T12:00:00Z
                    fingerprint=sha256(XAU long 0, short 0)

T4 Send 35=D 11=<cl_ord_id> 55=<pepperstone XAU id> 54=1 40=1 38=1000
TCP dies. No 35=8.

status              = EXECUTION_STATE_UNKNOWN
READY_FOR_EXECUTION = false
metric              fix_unknown_execution_states += 1

-- ILLEGAL: Send 35=D again with 11=<same>
-- ILLEGAL: mint new cl_ord_id and Send 35=D for the same copy_intent

Next TRADE Logon (141=Y). Seq back to 1. Original 35=D will not be resent.

35=H 11=<same cl_ord_id>
  case A: 35=8 11=<same> 39=2 150=F 14=1000 721=101
          → filled; link dest position 101; recon clean
  case B: 35=j ORDER_NOT_FOUND + positions still flat + AF complete
          → not_on_venue
          → if CopyIntent unexpired and risk re-approves:
               NEW execution_intent_id, NEW cl_ord_id, T1..T4 again
  case C: 35=j ORDER_NOT_FOUND + unexplained +0.5 lot
          → stay EXECUTION_STATE_UNKNOWN; issue row; human
```

---

## 20. Acceptance checklist (this design, not the empty repo)

```text
[ ] ClOrdID is unique, deterministic from execution_intent_id, 26-char Crockford
[ ] Persist T1 (not_sent) and T3 (arm sent_ack_unknown) happen before Send
[ ] Crash after T3 cannot look like not_sent
[ ] Disconnect after Send sets EXECUTION_STATE_UNKNOWN
[ ] No automatic 35=D retry of same or new ClOrdID from that state
[ ] Recovery is 35=H then 35=AF then 35=AN plus book fingerprint
[ ] Mass-status ERs without tag 11 are not mis-bound
[ ] Replacement is a new row + new ClOrdID only after not_on_venue + new risk
[ ] Cancel/replace allocate a new ClOrdID and set OrigClOrdID
[ ] Status request reuses the order ClOrdID
[ ] Unresolved unknown blocks READY_FOR_EXECUTION / new opens
[ ] Duplicate 35=8 does not double-book
[ ] REAL_COPY_EXECUTION_ENABLED default false
[ ] Unit + harness tests in §15 exist before any live NOS
```

Today every box is unchecked in **code**. This document is the contract those boxes will be scored against.

---

## 21. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§32–34, 41–45, 57–63, 67–72
- https://help.ctrader.com/fix/specification/ (RoE: unique ClOrdID, NOS, H, AF, ER, F, G, AN; `141=Y`)
- https://help.ctrader.com/fix/faqs/ (duplicate reports if multiple connections)
- https://help.ctrader.com/fix/sending-and-receiving-messages/
- Swarm: A05, A08, A20, A23, A24, A25, A27, A28, A32, A34

Product source under `D:\Prop\src` and `D:\Prop\apps` was **not** modified.
