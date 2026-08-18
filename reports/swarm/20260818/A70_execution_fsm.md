# A70 — Destination-order FSM and duplicate ExecutionReport handling

**Artifact:** `D:\Prop\reports\swarm\20260818\A70_execution_fsm.md`  
**Date:** 2026-08-18  
**Agent:** A70 (execution FSM spec only)  
**Status:** binding implementation spec — **no product source modified**  
**Scope:** one destination `ClOrdID` on Pepperstone / cServer FIX 4.4 TRADE. Not source MT5, not shadow, not session TCP state.

**Authority**

| Doc | What it locks |
|---|---|
| Architecture v2 §32–§35, §42–§44, §61, §68, §70, §72.8–§72.10 | persist-before-send, 7 order states, unknown recovery, live-exec gates |
| `A20_table_catalog.md` | `execution_intents.status`, `fix_execution_reports` uniqueness |
| `A25_fix_session_spec.md` §5 | unknown overlay, recon-only exit, illegal retry |
| `A32_ctrader_fix_specification.md` | tags 11 / 37 / 39 / 150 / 14 / 32 / 151 / 721 |
| `A34_ctrader_fix_faq.md` | two TRADE sockets → **copied** reports, not second fills |
| `A23` / `A48` / `A49` | risk, kill switch, `REAL_COPY_EXECUTION_ENABLED` (send gates, not FSM edges) |

**Measured Domain (2026-08-18) — do not pretend this spec is already applied**

| Type | Path | Class |
|---|---|---|
| Status enum (8 values) | `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` | EXISTS — names match; **graph not enforced** |
| Pure mapper | `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | EXISTS_NEEDS_REFACTOR — see §12 |
| Intent row | `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | EXISTS_NEEDS_REFACTOR — missing qty/venue/exec identity |
| ClOrdID factory | `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs` | EXISTS — uniqueness family only |
| `IExecutionReportApplier` / `fix_execution_reports` / tests | — | **MISSING** |

This file is the contract those types must implement. Chat is not the store.

---

## 0. Verdict

A destination order is **one `cl_ord_id`**, persisted **before** the first socket write of `NewOrderSingle` (35=D). Its application status is exactly this closed set:

```text
not_sent
sent_unknown
accepted
partial
filled
rejected
cancelled
```

plus the §34 recovery overlay:

```text
execution_state_unknown
```

`sent_unknown` means “write happened or may have happened; no usable venue `OrdStatus` yet.”  
`execution_state_unknown` means “after a break we cannot prove venue outcome.”  
Both **require reconciliation**. Neither may emit another 35=D on the **same** `cl_ord_id`.

Duplicate ExecutionReports (`35=8`) are **expected** (cTrader FAQ: every active TRADE connection gets a copy; FIX resend and `150=I` status snapshots also restate). Dedup is **not** “if I have seen this `ClOrdID`, ignore.” Dedup is:

1. persist the inbound ER,
2. classify it (`NEW_TRADE` / `STATUS_SNAPSHOT` / `WIRE_DUPLICATE` / `SEMANTIC_DUPLICATE` / `ORPHAN` / `CONFLICT`),
3. apply **fills only for a new trade identity**,
4. never leave a terminal state because a copy arrived.

§70.5 is failed until a second identical fill cannot change `cum_qty` or open a second destination position.

---

## 1. Non-goals

- Session FSM (`CONNECTING` / `LOGGED_ON` / `READY_FOR_EXECUTION`) — A25.
- Risk approve/reject of a *new* intent — A23. Risk does not rewrite this graph.
- Shadow orders — A24. Shadow has no venue ER.
- Quantity conversion (lots ↔ `OrderQty`) — A38 / A43. This FSM stores **destination** qty only.
- Two-phase commit with MT5. Source deals never drive `Apply`.
- Inventing `ExecID` when cServer omits tag 17. Use the fingerprint in §8.

---

## 2. Identity (one order)

| Name | FIX | Persistence | Rule |
|---|---|---|---|
| `execution_intent_id` | — | PK of `execution_intents` | One approved `risk_decision_id` → at most one **live** send row |
| `cl_ord_id` | 11 | `UNIQUE` on `execution_intents` and `fix_orders` | Allocated **before** send. Never reused after persist. Cancel/replace allocate a **new** 11 and set 41=`OrigClOrdID` |
| `dest_order_id` | 37 `OrderID` | `UNIQUE (venue_id, dest_order_id) WHERE NOT NULL` | cServer order id. Adopt on first ER that carries 37 |
| `destination_position_id` | 721 `PosMaintRptID` | dest position key | Hedge account position. Do not invent |
| `exec_id` | 17 `ExecID` | `UNIQUE (venue_id, exec_id) WHERE exec_id IS NOT NULL` | **If present.** Official RoE ER table / published examples **omit** 17 — never require it |
| `exec_fingerprint` | derived | `UNIQUE (venue_id, exec_fingerprint)` | Always computed. Dedup when 17 is absent |
| `orig_cl_ord_id` | 41 | on cancel/replace child | Points at the superseded `cl_ord_id` |

Client key: `(destination_account, cl_ord_id)`.  
Venue key: `(venue_id, dest_order_id)` once known.

Never generate `cl_ord_id` in memory and send before the intent row commits. Crash between send and persist is how unknown **duplicates** are born.

---

## 3. State catalog

Wire / DB strings are lowercase with underscores. Enum names are the existing `ExecutionOrderStatus` members.

### 3.1 The seven destination-order states (architecture §33)

| # | DB / log | Enum | Meaning | Terminal? | May send 35=D on **this** `cl_ord_id`? |
|---|---|---|---|---|---|
| 1 | `not_sent` | `NotSent` | Row committed. Socket write of 35=D has **not** been attempted. `sent_at` is null | no | **yes — only this state** |
| 2 | `sent_unknown` | `SentAcknowledgementUnknown` | Write returned success **or** we cannot prove the write did not happen. No mapped `OrdStatus` yet | no | **no** |
| 3 | `accepted` | `Accepted` | Venue working order. `39=0` New or `39=A` Pending New. `cum_qty = 0` | no | no |
| 4 | `partial` | `PartiallyFilled` | `39=1`. `0 < cum_qty < order_qty`, `leaves_qty > 0` | no | no |
| 5 | `filled` | `Filled` | `39=2`. `leaves_qty = 0`, `cum_qty = order_qty` | **yes** | no |
| 6 | `rejected` | `Rejected` | `39=8`. Venue refused **this** `cl_ord_id`. `cum_qty` must be 0 | **yes** | no (new ClOrdID only — §10) |
| 7 | `cancelled` | `Cancelled` | `39=4` (or `39=C` Expired, folded here). Remaining `leaves_qty` will not fill | **yes** | no |

User-facing aliases: **not sent**, **sent unknown**, **accepted**, **partial**, **filled**, **rejected**, **cancelled**.

### 3.2 Recovery overlay (architecture §34)

| DB / log | Enum | Meaning |
|---|---|---|
| `execution_state_unknown` | `ExecutionStateUnknown` | After disconnect, process death, leadership loss, or unmapped `OrdStatus`/`ExecType`, the venue outcome of **this** `cl_ord_id` is unprovable |

This is **not** a ninth business state like “partial.” It is the durable parking state for “do not guess.” A25 §5.2 wrote `sent_ack_unknown` as a synonym of `EXECUTION_STATE_UNKNOWN`. **This spec splits them:**

| | `sent_unknown` | `execution_state_unknown` |
|---|---|---|
| Typical cause | `AfterSendAttempt()` — write path returned | `AfterDisconnectWithUnknownAck()` or unmapped ER |
| May still receive the first 35=8 on the **same** TCP session | yes | only after reconnect + status query |
| `RequiresReconciliation` | **true** | **true** |
| Blind 35=D retry | **illegal** | **illegal** |
| Legal exit | inbound ER that maps (§6) **or** recon adopt (§9) | recon adopt / inbound ER after TRADE is logged on |

If persist of `sent_at` is itself uncertain (crash between persist-sent and write, or write success with persist fail), treat as **`execution_state_unknown`**, never as `not_sent`. Prefer false unknown over a second order.

### 3.3 What is *not* a destination-order state

| Event | Handling |
|---|---|
| Session `35=3` Reject after 35=D | **Not** `rejected`. Order may still exist. → `execution_state_unknown` + recon |
| `35=j` Business Message Reject | Same as session reject unless Text **proves** the NOS was not processed (still recon if unsure) |
| `35=9` Order Cancel Reject | Cancel **request** failed. Working order stays `accepted` / `partial` |
| `150=5` Replace | Child `cl_ord_id` (new row). Parent becomes `cancelled` (or stays working until 39=4). Do not mutate parent 11 |
| `150=C` / `39=C` Expired | Persist raw `C`. FSM status = `cancelled` (leaves will not fill) |
| `150=I` Order Status | Snapshot. May move status. **Never** a new fill (§8.4) |
| Kill switch / flag off | Blocks **new** `not_sent` → send. Does not rewrite existing status (A48 / A49) |

---

## 4. Persist-before-send (only legal birth)

```text
risk_decision AllowFixSend
        ↓
allocate cl_ord_id          -- ClOrdIdFactory; unique; never reuse
        ↓
INSERT execution_intents    -- status = not_sent, sent_at NULL
INSERT fix_orders           -- same cl_ord_id, dest_order_id NULL
COMMIT
        ↓
re-check send conjunction   -- A25 §6.3 / A49 (flags, lease, READY, risk, not expired)
        ↓
if still not_sent:
    write 35=D
    AfterSendAttempt() → sent_unknown
    persist sent_at, fencing_token, fix_session_key
else:
    do not write
```

If the INSERT fails, **do not send**.  
If the INSERT commits and the process dies before `sent_at`, restart treats the row as **`execution_state_unknown`** (cannot prove the write did not sneak out).  
`not_sent` is the **only** state that may transition to a first `NewOrderSingle`.

Send conjunction (repeated from A25; FSM does not weaken it):

```text
REAL_COPY_EXECUTION_ENABLED = true
TRADE = READY_FOR_EXECUTION
lease owned + fence current
STOP_NEW_EXECUTION = false          -- for OPEN/INCREASE
status = not_sent
intent not expired
cl_ord_id persisted
```

---

## 5. Legal transition graph

Edges are the **only** legal status writes. Anything else is a `CONFLICT` → recon issue, status unchanged.

```text
                    persist
                       │
                       ▼
                   not_sent
                    /     \
         AfterSend     (never sent;
         Attempt        expire / risk
            │           retract — stay
            ▼           not_sent, no 35=D)
      sent_unknown ──────────────────────────────┐
       /    |    \                               │
      /     |     \  disconnect / crash /        │
     /      |      \ leadership loss             │
    ▼       ▼       ▼                            │
accepted  partial  execution_state_unknown ◄─────┘
    │         │              │
    │         │              │  35=H / 35=AF / 35=AN
    │         │              │  + ER / PositionReport
    │         │              ▼
    │         │         adopt venue
    │         │         (accepted|partial|filled|
    │         │          rejected|cancelled)
    │         │         or not_on_venue → see §9
    ▼         ▼
  partial   filled
    │
    ├──► filled          (39=2, leaves=0)
    └──► cancelled       (39=4/C; remaining leaves die)

accepted ──► filled      (full fill; Accepted may be skipped)
accepted ──► cancelled
accepted ──► rejected    only if cum_qty = 0

sent_unknown ──► accepted | partial | filled | rejected | cancelled
not_sent ──► (ER with matching cl_ord_id) treat as sent_unknown then apply
             (orphan ER: no row → OrphanExecutionReport; do not create an order)
```

### 5.1 Transition table

`S` = stay (idempotent restatement). `—` = illegal (do not apply; raise issue).

| from \ to | not_sent | sent_unknown | accepted | partial | filled | rejected | cancelled | exec_unknown |
|---|---|---|---|---|---|---|---|---|
| **not_sent** | S | send attempt | ER | ER | ER | ER | ER | crash/uncertain write |
| **sent_unknown** | — | S | ER 39=0/A | ER 39=1 | ER 39=2 | ER 39=8 | ER 39=4/C | disconnect |
| **accepted** | — | — | S | ER 39=1 | ER 39=2 | ER 39=8 if cum=0 | ER 39=4/C | disconnect while working |
| **partial** | — | — | — | S | ER 39=2 | — | ER 39=4/C | disconnect while working |
| **filled** | — | — | — | — | S | — | — | — |
| **rejected** | — | — | — | — | — | S | — | — |
| **cancelled** | — | — | — | — | — | — | S | — |
| **exec_unknown** | —* | — | recon/ER | recon/ER | recon/ER | recon/ER | recon/ER | S |

\* `exec_unknown` → `not_sent` is **illegal**. Proven-absent venue result is `not_on_venue` on the **old** row (terminal for that ClOrdID) and a **new** row if policy allocates a new 11 (§9).

### 5.2 Absorbing / monotonic rules

1. **`filled`, `rejected`, `cancelled` absorb.** A later ER that maps to any other status is ignored for status (current `Apply` already locks `Filled` / `Rejected` / `Cancelled`). Still persist the ER.
2. **No backward motion.** `partial` ↛ `accepted`. `filled` ↛ `partial`. `rejected`/`cancelled` ↛ working.
3. **Skip-ahead is legal.** IOC market orders often emit `39=0` then `39=2`, or only `39=2`. `sent_unknown` → `filled` is legal.
4. **`partial` → `rejected` is illegal.** A working partial that the venue kills is `cancelled` (`39=4`, leaves cancelled — RoE). If cServer sends `39=8` with `cum_qty > 0`, persist raw, raise `UnresolvedExecutionState`, **do not** zero the fills.
5. **`accepted` → `rejected`** only with `cum_qty = 0`.
6. **Quantity is monotonic non-decreasing** on `cum_qty` for a given `cl_ord_id`. A snapshot with smaller `cum_qty` is `CONFLICT`.

Current `ExecutionOrderStateMachine.Apply` **violates (2)**: any non-terminal current status is replaced by the mapped status, so `PartiallyFilled` + `39=0` becomes `Accepted`. That is a spec defect, not a feature. Fix the applier; do not document the hole as allowed.

---

## 6. Mapping inbound `35=8` → proposed status

Prefer **`OrdStatus` (39)** as the state bit. `ExecType` (150) is the *reason this message exists*.

```text
proposed = MapOrdStatus(tag 39)
if tag 39 missing/blank:
    proposed = MapExecType(tag 150)   -- last resort
```

| Tag 39 (OrdStatus) | Proposed FSM |
|---|---|
| `0` / `NEW` | `accepted` |
| `A` / `PENDING_NEW` | `accepted` |
| `1` / `PARTIAL` / `PARTIALLY FILLED` | `partial` |
| `2` / `FILL` / `FILLED` | `filled` |
| `4` / `CANCELED` / `CANCELLED` | `cancelled` |
| `C` / `EXPIRED` | `cancelled` (raw `C` stored) |
| `8` / `REJECTED` / `REJECT` | `rejected` |
| other / empty after fallback | **do not guess** → stay or enter `execution_state_unknown` |

| Tag 150 (ExecType) | Role |
|---|---|
| `0` New | ack; status from 39 |
| `F` Trade | **may** apply `LastQty` if identity is new (§8) |
| `4` Canceled | status `cancelled` |
| `8` Rejected | status `rejected` if graph allows |
| `C` Expired | status `cancelled` |
| `5` Replace | child order / orig link; not a fill |
| `I` Order Status | **snapshot only** — never `NEW_TRADE` |
| other | unmapped; recon if we cannot keep a legal status |

Compare-and-set: `new_status = LegalTransition(current, proposed)`. If illegal → persist ER, increment `fix_er_illegal_transition_total`, open `UnresolvedExecutionState`, **keep `current`**.

---

## 7. Quantity on the order (not the FSM bit)

Persisted on `fix_orders` / last ER, not inside the enum.

| Field | Tag | Rule |
|---|---|---|
| `order_qty` | 38 | Immutable copy of `execution_intents.requested_quantity` (already normalized). Never change because an ER echoes a different 38 — that is `QuantityMismatch` |
| `last_qty` | 32 | This message’s trade size. Apply to book **only** on `NEW_TRADE` |
| `last_px` | 31 | This trade’s price. VWAP/`AvgPx` (6) is informational |
| `cum_qty` | 14 | Venue cumulative. **Source of truth** when present |
| `leaves_qty` | 151 | Venue remaining. RoE: 0 = fully filled, `OrderQty` = nothing filled |
| `avg_px` | 6 | Persist; do not drive state |

Invariants (when 14 and 151 both present):

```text
cum_qty >= 0
leaves_qty >= 0
cum_qty + leaves_qty == order_qty     -- else QuantityMismatch + recon
cum_qty == order_qty  ↔  status may be filled
leaves_qty == 0       ↔  status is filled or cancelled-after-full
0 < cum_qty < order_qty  ↔  status is partial or cancelled-partial
```

Apply order for a `NEW_TRADE`:

1. If `14` present: `cum_qty' = max(current_cum, inbound_14)`. If inbound_14 < current_cum → `CONFLICT`, do not apply.
2. If `14` absent: `cum_qty' = current_cum + last_qty` (only because identity is new).
3. If `cum_qty' > order_qty` → `CONFLICT` / `UnexpectedFill`. Do not book the overflow.
4. Recompute `leaves_qty` from tag 151 if present, else `order_qty - cum_qty'`.
5. Then map status from 39, not from “we added LastQty.”

`150=I` updates `cum_qty` / `leaves_qty` from the snapshot **if monotonic**; it never does step 2.

---

## 8. Duplicate ER handling (architecture §70.5, FAQ, §61)

### 8.1 Why duplicates exist

| Source | What arrives | Official / derived |
|---|---|---|
| Two TRADE TCP sessions | **Copy** of the same application report to each socket | A34 FAQ #1 verbatim |
| FIX `ResendRequest` (35=2) | Same `MsgSeqNum` replayed | session layer |
| `OrderStatusRequest` / mass status | `150=I` restating 39/14/151 | RoE ExecType `I` |
| Client crash + replay of persisted handler | same row processed twice | our pipeline |
| Test harness `SimulateDuplicateExecutionReport` | identical raw string twice | `FixSimulationHarness` |

FAQ does **not** say “dedupe by ClOrdID.” A second `35=8` on the same 11 can be a **new partial**, a **status snapshot**, or a **byte-for-byte copy**. Classification is mandatory.

Prevention: **one** production TRADE owner (A25 §4). The FSM must still be idempotent if that fails.

### 8.2 Persist first

Every inbound `35=8` (and 35=9 / 35=j that names a `cl_ord_id`) is appended to `fix_execution_reports` **before** `Apply`.

Minimum columns (A20 + this spec):

```text
id
venue_id
exec_id                  -- tag 17, nullable
exec_fingerprint         -- §8.3, NOT NULL
msg_seq_num              -- tag 34, nullable
session_key
cl_ord_id                -- tag 11
dest_order_id            -- tag 37
destination_position_id  -- tag 721
exec_type                -- tag 150
ord_status               -- tag 39
last_qty, last_px, cum_qty, leaves_qty, avg_px, order_qty
ord_rej_reason           -- tag 103
text                     -- tag 58
transact_time            -- tag 60
received_at
classification           -- NEW_TRADE | STATUS_SNAPSHOT | WIRE_DUPLICATE | SEMANTIC_DUPLICATE | ORPHAN | CONFLICT
applied                  -- bool: did this row change fix_orders?
```

Unique indexes:

```text
UNIQUE (venue_id, exec_id)          WHERE exec_id IS NOT NULL
UNIQUE (venue_id, exec_fingerprint)
-- optional exact-wire:
UNIQUE (session_key, msg_seq_num)   WHERE msg_seq_num IS NOT NULL
```

`INSERT … ON CONFLICT DO NOTHING` (or equivalent) returning “already present” **is** the duplicate detector. Do not SELECT-then-INSERT without a unique constraint.

### 8.3 Fingerprint (required because tag 17 may be absent)

Official RoE Execution Report field list and the published New→Fill pair **do not include `ExecID` (17)**. Architecture A20 still wants `(venue_id, exec_id)`. Both are true:

- If 17 is present and non-blank → store it; semantic key = `(venue_id, exec_id)`.
- Always compute:

```text
exec_fingerprint = SHA-256( UTF-8(
    venue_id            || '|' ||
    cl_ord_id           || '|' ||
    dest_order_id       || '|' ||
    exec_type           || '|' ||
    ord_status          || '|' ||
    last_qty            || '|' ||
    last_px             || '|' ||
    cum_qty             || '|' ||
    leaves_qty          || '|' ||
    transact_time
))
```

Use invariant decimal formatting (no thousands sep, no trailing junk). Missing optional fields are empty strings, not `"0"` (so a true `LastQty=0` differs from omitted).

If two **different** trades ever collide on this tuple, that is a venue/clock defect → `CONFLICT`, do not merge.

### 8.4 Classifier

Run **after** a successful insert. On unique-key conflict, classification is `WIRE_DUPLICATE` or `SEMANTIC_DUPLICATE` and `Apply` is a no-op.

```text
if no execution_intent / fix_orders for cl_ord_id:
    ORPHAN                          -- ReconciliationIssueType.OrphanExecutionReport
    do not create an order from the ER
    do not send 35=D

else if insert lost on (venue_id, exec_id):
    SEMANTIC_DUPLICATE              -- same ExecID
    do not apply LastQty
    do not change status (already applied)

else if insert lost on (venue_id, exec_fingerprint)
      or (session_key, msg_seq_num):
    WIRE_DUPLICATE                  -- FAQ copy or resend
    same as semantic: no qty, no status change

else if exec_type == 'I':
    STATUS_SNAPSHOT
    may LegalTransition(status) + monotonic cum/leaves
    LastQty is ignored even if present

else if exec_type == 'F' OR (ord_status in {1,2} AND last_qty > 0
                             AND exec_type not in {I,0,4,5,8,C}):
    NEW_TRADE
    apply §7 then §5

else:
    -- 150=0 New, 4 cancel, 8 reject, 5 replace, C expire, …
    non-trade ER
    LegalTransition only; LastQty ignored
```

A second `150=F` with a **new** fingerprint / ExecID on the same `cl_ord_id` is a **new partial** (or the completing fill). That is not a duplicate.

A second `150=F` with the **same** fingerprint / ExecID is a duplicate. `cum_qty` stays. Destination position qty stays.

### 8.5 What “idempotent Apply” means

```text
Apply(current, er) is a pure function of
    (order_row, fill_book, er, classification)

duplicate ⇒ (order_row, fill_book) unchanged
STATUS_SNAPSHOT ⇒ order_row may move forward; fill_book rows unchanged
NEW_TRADE ⇒ at most one new fill_book row keyed by exec identity
```

Never implement “add `LastQty` every time `39=1|2`.” That double-books FAQ copies and `150=I` restatements.

Fill book key: `(venue_id, exec_id)` or `(venue_id, exec_fingerprint)`.

### 8.6 Orphans, conflicts, unexpected fills

| Classification | `ReconciliationIssueType` | Effect |
|---|---|---|
| `ORPHAN` | `OrphanExecutionReport` | persist ER; no order |
| inbound `cum_qty` < stored | `QuantityMismatch` | no apply |
| inbound `cum_qty` > `order_qty` | `UnexpectedFill` | no overflow book |
| illegal graph edge | `UnresolvedExecutionState` | keep status |
| `NEW_TRADE` on `filled`/`rejected`/`cancelled` | `UnexpectedFill` | persist; no qty |
| side (54) ≠ intent side | `SideMismatch` | persist; no apply |

Nothing unresolved is silently ignored (A20 / §54).

### 8.7 Duplicate handling is not retry suppression

| Concern | Mechanism |
|---|---|
| Same 35=8 twice | §8 unique keys |
| Same 35=D twice | **never** send twice on one `cl_ord_id`; persist-before-send |
| Same source signal twice | `copy_intents.idempotency_key` (A23 / A24) — upstream of this FSM |
| Two TRADE owners | lease (A25) — still need §8 |

---

## 9. Recovery (only legal path out of unknown)

Owned in detail by A25 §5.4. FSM-level contract:

```text
sent_unknown | execution_state_unknown | accepted/partial + session dead
        ↓
block additional 35=D for this cl_ord_id
        ↓
TRADE logged on + lease owned
        ↓
35=H OrderStatusRequest by ClOrdID
        ↓
if still unknown: 35=AF MassStatusReqType=7
        ↓
consume 35=8 (classifier in §8)
        ↓
35=AN RequestForPositions
        ↓
decide:
  venue has this ClOrdID     → adopt 39/14/151 (legal edge only)
  venue has no such ClOrdID
    AND positions unchanged
    AND mass-status complete → mark this row not_on_venue (keep cl_ord_id;
                                status stays cancelled-equivalent / rejected-equivalent
                                for retry policy: MayRetry is false on this 11)
                                only THEN a NEW cl_ord_id may be allocated
  mismatch                   → BLOCKED_INCONSISTENT; no auto-resend
```

Illegal:

```text
catch (IOException) { SendNewOrderSingle(sameClOrdId); }
catch (IOException) { SendNewOrderSingle(newClOrdId); }
```

Legal:

```text
on transport fail after possible send:
    status = execution_state_unknown     -- or stay sent_unknown if write never left
    metric fix_unknown_execution_states++
    schedule reconcile
    do not send
```

Unknown **orders already sent** do not expire into a resend. Copy-intent `expires_at` applies to **unsent** `not_sent` rows only.

---

## 10. Retry and `MayRetryNewOrderSingle`

Current helper (keep the name, tighten the meaning):

```text
MayRetryNewOrderSingle(status) =
    status is NotSent or Rejected
```

Interpretation:

| Status | Retry 35=D on **same** 11 | New 11 for same risk/intent family |
|---|---|---|
| `not_sent` | yes (the first send) | no — already has 11 |
| `sent_unknown` | **never** | only after §9 `not_on_venue` |
| `accepted` / `partial` | never | never while working |
| `filled` | never | never (exposure exists) |
| `cancelled` | never | new risk decision only (new intent) |
| `rejected` | **never** (11 is burned) | **yes**, new row + new 11, if risk re-approves and reject is not “duplicate/exists” |
| `execution_state_unknown` | never | only after §9 |

`RequiresReconciliation(status)` stays:

```text
SentAcknowledgementUnknown | ExecutionStateUnknown
```

Working `accepted`/`partial` after a **session** drop also need recon, but they are not this helper’s job — the TRADE startup gate (A25 §13 / §42) already blocks `READY_FOR_EXECUTION` until mass status + positions match.

---

## 11. Applier API (to implement; not present)

Replace the current three-argument-poor `Apply` with a result object. Domain stays pure (no I/O).

```text
ExecutionReportInput
    ClOrdId, VenueOrderId, ExecId?, ExecType, OrdStatus,
    LastQty?, LastPx?, CumQty?, LeavesQty?, OrderQty?,
    PosMaintRptId?, Text?, TransactTime?, MsgSeqNum?,
    SessionKey, VenueId, RawFingerprintInputs

ExecutionApplyResult
    NextStatus
    Classification
    NewCumQty, NewLeavesQty
    BookFill           -- bool
    IssueType?         -- ReconciliationIssueType
    StatusChanged      -- bool
```

Pipeline in the Application worker (I/O lives here):

```text
parse 35=8
  → persist fix_execution_reports (unique)
  → if conflict: classify duplicate; return
  → load fix_orders by cl_ord_id
  → Domain applier
  → update fix_orders + destination_positions in the same transaction as the ER row
  → outbox (fill / terminal) only if BookFill or StatusChanged
```

`ExecutionReportInput` today has no `ExecId`, no fingerprint, no `OrderQty`. That is why qty double-count cannot be prevented in Domain yet.

---

## 12. Honest gap vs current `ExecutionOrderStateMachine`

`D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` (measured):

| Behavior | Code today | This spec |
|---|---|---|
| `AfterSendAttempt` | → `SentAcknowledgementUnknown` | same (`sent_unknown`) |
| `AfterDisconnectWithUnknownAck` | → `ExecutionStateUnknown` | same |
| Map 39/150 | 0/A accepted; 1 partial; 2 filled; 4 cancelled; 8 rejected; else unknown | same + `C` → `cancelled` |
| Terminal lock | Filled ignores non-Filled; Rejected/Cancelled ignore all | keep + `filled` lock includes qty |
| Backward edges | **allowed** (returns mapped status) | **forbidden** |
| Duplicate ER | **not considered** | §8 required |
| `150=I` vs `150=F` | ignored (39 wins) | 39 for status; 150 for fill-vs-snapshot |
| `MayRetry` | NotSent, Rejected | same, with “new 11 only” on Rejected |
| `RequiresReconciliation` | sent_unknown, exec_unknown | same |
| Expired / replace | unknown / unknown | cancelled / child order |
| Tests | none under `tests/` | §14 |

`ExecutionIntent` carries `Status` and `ClOrdId` but not `sent_at`, `dest_order_id`, `cum_qty`, `leaves_qty`, or last exec identity. DbContext names `ExecutionIntents` / `ExecutionIntentsConfiguration` that are **not** in `Persistence/Configurations/` — persistence of this FSM is not done.

Do not call the current mapper “§70.5 complete.”

---

## 13. Interaction with other machines

| Other machine | Relation |
|---|---|
| CopyIntent `Pending` / expired | Upstream. Expired intent ⇒ do not leave `not_sent` via send |
| RiskDecision `AllowFixSend` | Gate on first send only |
| Kill `STOP_NEW_EXECUTION` | Blocks `not_sent` → send for OPEN/INCREASE. Does not cancel working orders |
| Kill `EMERGENCY_FLATTEN` | New **close** intents (`not_sent` → send) under A48. Unknown flatten orders follow **this** FSM (no blind retry) |
| TRADE session `READY_FOR_EXECUTION` | Required to send and to exit unknown via recon |
| Destination position | Updated only from applied `NEW_TRADE` / snapshot adopt, keyed by 721 |
| Shadow | Independent. Never feed shadow fills into this graph |

---

## 14. Tests that must exist before live 35=D (A27 / §60 / §61 / §70)

Unit (`Execution.ExecutionReportStateTransitionTests`):

```text
NotSent_AfterSendAttempt_is_sent_unknown
SentUnknown_39_0_is_accepted
SentUnknown_39_2_skips_to_filled
Accepted_39_1_is_partial
Partial_then_39_2_is_filled
Partial_then_39_0_stays_partial          -- backward forbidden
Filled_then_39_1_stays_filled
Rejected_and_Cancelled_absorb
Accepted_39_8_with_cum_0_is_rejected
Partial_39_8_does_not_clear_fills
39_C_maps_to_cancelled
150_I_does_not_book_LastQty
MayRetry_only_NotSent_or_Rejected
RequiresReconciliation_on_both_unknowns
```

Duplicate / qty:

```text
Duplicate_ExecID_does_not_double_fill
Duplicate_fingerprint_without_tag_17_does_not_double_fill
Second_150_F_new_fingerprint_adds_partial
FAQ_two_sockets_same_body_one_fill
Duplicate_is_persisted_and_classified
```

Harness (A10 names):

```text
DuplicateExecutionReportSimulationTests.Second_identical_ER_is_ignored_for_qty
PartialFillSimulationTests.Leaves_order_partially_filled_and_position_qty_matches_cumQty
RejectionSimulationTests.ER_39_8_sets_rejected_and_does_not_open_position
UnknownStateDisconnectSimulationTests.Disconnect_after_NOS_sets_execution_state_unknown
UnknownExecutionRecoveryTests.Does_not_resend_NOS_on_reconnect
```

§70.5 is **not** checked until the duplicate facts are green on the in-process simulator. Real account `1369850` is not the first test (architecture §61).

---

## 15. Metrics and logs (no secrets)

| Metric | When |
|---|---|
| `fix_orders_sent_unknown` | enter `sent_unknown` |
| `fix_unknown_execution_states` | enter `execution_state_unknown` |
| `fix_er_wire_duplicate_total` | `WIRE_DUPLICATE` |
| `fix_er_semantic_duplicate_total` | `SEMANTIC_DUPLICATE` |
| `fix_er_new_trade_total` | `NEW_TRADE` applied |
| `fix_er_orphan_total` | `ORPHAN` |
| `fix_er_illegal_transition_total` | graph reject |
| `fix_fills_booked_qty` | sum of applied `last_qty` |

Log `cl_ord_id`, `dest_order_id`, `exec_id`/`fingerprint`, `exec_type`, `ord_status`, `classification`, `from_status`, `to_status`. Never log password or raw Logon.

---

## 16. Implementation checklist (later coding task — not this file)

```text
[ ] Tighten Apply to the §5 table (no backward edges)
[ ] Map 39=C → cancelled
[ ] Extend ExecutionReportInput with ExecId, fingerprint inputs, Cum/Leaves/OrderQty
[ ] Persist fix_execution_reports with the two UNIQUE keys
[ ] Classifier §8.4 before any position update
[ ] Fill book keyed by exec identity
[ ] sent_at + AfterSendAttempt only after commit of not_sent
[ ] Uncertain write → execution_state_unknown, never not_sent
[ ] IExecutionReportApplier + UnknownExecutionRecovery (A25 §5.4)
[ ] Unit + harness facts in §14
[ ] REAL_COPY_EXECUTION_ENABLED remains false until §68 and §70 are evidenced
```

---

## 17. Source map

| Claim | Where |
|---|---|
| Distinguish not sent / sent-ack-unknown / accepted / partial / filled / rejected / cancelled | Architecture §33 |
| Never retry 35=D because TCP broke; `EXECUTION_STATE_UNKNOWN` then status/mass-status/positions | Architecture §34, A25 §5 |
| Duplicate report handling is a live-exec gate | Architecture §70.5 |
| Persist every ER; unique `(venue_id, exec_id)` | A20 `fix_execution_reports` |
| Reports duplicated if multiple API connections | cTrader FAQ (A34) |
| ExecType 0/4/5/8/C/F/I ; OrdStatus 0/1/2/8/4/C | A32 RoE table |
| Tag 17 not in official ER examples | A32 published New→Fill pair |
| Current mapper + enum | `ExecutionOrderStateMachine.cs`, `ExecutionOrderStatus.cs` |
| Simulator already has duplicate hook | `FixSimulationHarness.SimulateDuplicateExecutionReport` |

---

*End A70. Product source was not modified.*
