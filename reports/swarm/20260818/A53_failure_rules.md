# A53 — Failure Rules and No Blind Catch-Up

**Artifact:** `D:\Prop\reports\swarm\20260818\A53_failure_rules.md`  
**Date:** 2026-08-18  
**Agent:** A53  
**Status:** Binding implementation spec. **No product source was modified.**  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Primary sections:** **§62 Failure Rules**, **§63 No Blind Catch-Up Copying**  
**Supporting sections:** §4, §12, §17, §18, §24, §27–§28, §31–§43, §53–§54, §57–§58, §60–§61, §64, §68, §70, §72.7–§72.11, §72.15, §72.17–§72.18  
**Sibling specs (do not contradict):** `A23_risk_engine_spec.md`, `A24_shadow_copy_spec.md`, `A25_fix_session_spec.md`, `A07_mt5_worker_audit.md`, `A20_table_catalog.md`, `A27_test_inventory.md`

This document is the **single failure-mode matrix** for live and shadow copy. It restates architecture §62–§63 as implementable rules. When this spec and a sibling differ on a failure action, **§62–§63 win**; siblings own sizing, FIX codecs, and shadow fill math.

---

## How to use this document

- **Fail closed** on missing, stale, or inconsistent required state. A missing health bit is **down**, not “unknown-but-ok”.
- **Do not invent** source trades, destination quotes, fills, or books.
- **Do not blindly retry** a possibly-sent order (§33, §34, §72.9). That is not catch-up; it is unknown-state recovery.
- **Do not blindly catch up** a backlog of entries after reconnect, restart, leadership change, or flag flip (§63, §72.17).
- **OPEN/INCREASE** and **REDUCE/CLOSE** have **separate** policies (§63 last line, §64, §72.18).
- Thresholds (`max_quote_age`, `max_signal_age`, `max_source_staleness`, `expires_at` slack) are **configuration**, not code constants. Do not hardcode production milliseconds here.
- First useful version (§69) does **not** need ML. “ML unused by design” is not “ML down”.

---

# 1. Architecture law (verbatim)

## 1.1 §62 Failure Rules

### MT5 unavailable

```text
Do not invent source trades.
Continue retrying.
Expose stale-source status.
Do not open new copied positions from stale source data.
```

### ML unavailable

```text
Continue ingestion/reconstruction.
Do not promote new traders to live based on missing scoring.
Existing hard risk limits remain active.
```

### QUOTE FIX unavailable

```text
Do not create new live copy trades requiring fresh pricing.
```

### TRADE FIX unavailable

```text
Do not queue an unlimited backlog of stale entries.
Mark new intents appropriately.
Do not resend unknown orders blindly.
```

### Database unavailable

```text
Execution service should fail closed for new orders.
Do not run critical real execution solely from volatile memory.
```

## 1.2 §63 No Blind Catch-Up Copying

```text
If FIX is disconnected for 3 minutes while source traders opened 20 trades:

Do NOT reconnect and blindly execute all 20 old entries.

Each CopyIntent must have:

expires_at
max_signal_age

Stale entries expire.

Closing/reducing risk may have separate policy from opening new exposure.
```

The “3 minutes / 20 trades” story is **normative**, not illustrative folklore. A reconnect, restart, leadership win, or `REAL_COPY_EXECUTION_ENABLED` false→true **must not** flush a send queue.

---

# 2. What “down” means

A subsystem is **down** when **any** row in its table is true. Operators see the **primary** reason; risk sees a boolean + reason code.

## 2.1 MT5 down (per `broker_id`)

| Detector | Down when |
|---|---|
| Transport | Manager / HTTP sidecar `IsConnected = false` |
| Heartbeat | Last successful `Ping` / pump tick older than `max_mt5_heartbeat_age` |
| Event freshness | `now − last_persisted_source_event_time > max_source_staleness` |
| Checkpoint | Backfill/live `GetDeals` returned false, or checkpoint not advanced after a failed page |
| Pump fallback | Connect fell back to **no-pump** **and** deal-poll lag exceeds `max_source_staleness` (degraded = stale for **copy**, not an excuse to invent) |
| Process | `mt5-worker` heartbeat missing |

Brokers are **independent**. Achiever down does **not** stale StarwaveFX. Never invent deals for the dead broker to “keep the book aligned”.

`mt5_connected{broker_id}` and a durable `source_stale_since` timestamp are mandatory (§58, A07). Dashboard: `GET /api/v1/system/health` stale-source flags (A06).

## 2.2 ML down (global scorer, not the risk engine)

| Detector | Down when |
|---|---|
| Process | Scorer / model host heartbeat missing |
| Model | No loaded model version, or `promote` requested with empty score snapshot |
| Inference | Timeout / exception / `score_failures` without a durable replacement score |
| Freshness (promotion only) | Last **durable** score age `> max_score_age` **and** the action is a **promotion** |

Not down:

- Phase 0–5 / first useful version running **deterministic baseline only** (§18, §69). Label that `ML_NOT_IN_USE`.
- Risk evaluation. The risk engine **does not call ML** (A23 §3, §72.15). A down scorer cannot freeze hard limits.

## 2.3 QUOTE down (destination `CTraderQuoteSession`)

| Detector | Down when |
|---|---|
| Flag | `CTRADER_FIX_ENABLED=false` or `CTRADER_FIX_QUOTE_ENABLED=false` |
| Session | Status ∉ {`LoggedOn`, `ReadyForMarketData`} (`FixSessionStatus`, §27) |
| Heartbeat | Last inbound older than session heartbeat tolerance |
| Book | No quote row for the mapped destination instrument |
| Age | `quote_age > max_quote_age` **or** `venue_quote_age > max_quote_age` (either trips OPEN) |

Stale quote **is** QUOTE-unavailable for any path that **requires fresh pricing** (§31, §37, §62). Do not invent a mid from source MT5 last-deal.

## 2.4 TRADE down (destination `CTraderTradeSession`)

| Detector | Down when |
|---|---|
| Flag | `CTRADER_FIX_ENABLED=false` or `CTRADER_FIX_TRADE_SESSION_ENABLED=false` |
| Session | Status ≠ `ReadyForExecution` — includes `Disconnected`, `Connecting`, `LogonSent`, `LoggedOn`, `Reconciling`, `LogoutSent`, `Error` |
| Lease | This process does not own the TRADE lease / fencing token (§28) |
| Reconcile | Startup or periodic reconcile incomplete or inconsistent (§42, §43, §70.14) |
| Heartbeat | Last inbound older than session heartbeat tolerance |
| Unknowns | Any unresolved `EXECUTION_STATE_UNKNOWN` (treat as **not ready**; do not send another NOS to “fix” it) |

`LoggedOn` without reconcile is **down for send**. Independent of QUOTE (§27, §72.11).

## 2.5 DB down (PostgreSQL is the authority)

| Detector | Down when |
|---|---|
| Connectivity | Cannot open / ping the execution database |
| Write | Persist of `copy_intents` / `risk_decisions` / `execution_intents` / `fix_orders` / ER / positions fails to **commit** |
| Read | Required snapshot for risk (book, flags, mapping, quote row if stored) cannot be loaded |

**Redis is not the database.** Redis up + Postgres down = **DB down**. In-memory books, FIX caches, and worker dictionaries are **volatile** and are not a license to send (§62 last paragraph, §28, A03).

If persist-before-send cannot complete, the order **was not sent**. There is no “optimistic NOS”.

---

# 3. Master matrix (binding)

Legend:

| Token | Meaning |
|---|---|
| **STOP** | Do not do this. Persist a decision / health flag. |
| **RETRY** | Keep trying the dependency. No invention. |
| **CONTINUE** | Allowed if other gates pass. |
| **EXPIRE** | Mark intent terminal; do not queue. |
| **RECONCILE** | Recover venue truth; do not send. |
| **SEPARATE** | REDUCE/CLOSE policy — see §6. |

## 3.1 What each outage does

| Path | MT5 down | ML down | QUOTE down | TRADE down | DB down |
|---|---|---|---|---|---|
| Invent source trades / ticks | **STOP** | CONTINUE (not ML’s job) | CONTINUE | CONTINUE | **STOP** (cannot persist; do not hallucinate) |
| Retry collector / session | **RETRY** | n/a | **RETRY** QUOTE | **RETRY** TRADE | **RETRY** DB; no send |
| Expose health | `SOURCE_STALE` | `ML_UNAVAILABLE` | `QUOTE_FIX_UNAVAILABLE` | `TRADE_FIX_UNAVAILABLE` | `DATABASE_UNAVAILABLE` |
| Persist raw MT5 + outbox | **STOP** new facts; **RETRY** | **CONTINUE** | **CONTINUE** | **CONTINUE** | **STOP** (fail closed) |
| Reconstruct logical trades | Only from **already persisted** raw | **CONTINUE** | **CONTINUE** | **CONTINUE** | **STOP** |
| Deterministic score (non-ML) | From last persisted trades only | **CONTINUE** | **CONTINUE** | **CONTINUE** | **STOP** |
| ML inference / rescoring | Skip live-tape features if source stale | **STOP** | CONTINUE (ML ≠ quote) | CONTINUE | **STOP** |
| Promote to `LIVE` / `LIVE_CANDIDATE` | **STOP** if that trader’s source is stale | **STOP** | **STOP** (no fresh dest evidence) | **STOP** (cannot prove venue) | **STOP** |
| Existing `LIVE` stay `LIVE` | Yes; **no new OPEN** from stale source | Yes; last durable score; **hard limits stay on** | Yes; **no new priced OPEN** | Yes; **no NOS** | Yes in DB once DB returns; **no send while down** |
| Shadow OPEN / INCREASE | **STOP** (no invented events) | CONTINUE if last eligibility durable | **STOP** | CONTINUE (shadow is QUOTE-only) | **STOP** |
| Shadow REDUCE / CLOSE | **SEPARATE** — no invented close event; existing marks may continue on dest quotes | CONTINUE | **SEPARATE** (last-quote waterfall / UNPRICED; A24 §8) | CONTINUE | **STOP** |
| Live OPEN / INCREASE | **STOP** `SOURCE_STALE` | CONTINUE through risk (ML not in hot path) | **STOP** `QUOTE_*` | **STOP** `TRADE_FIX_UNAVAILABLE`; **EXPIRE** stale; **no unbounded queue** | **STOP** fail closed |
| Live REDUCE / CLOSE | **SEPARATE** — only if a **persisted source close** exists and dest mapping is known | CONTINUE | **SEPARATE** — may proceed without *entry* freshness if TRADE ready + mapping known | **STOP** send if TRADE down; persist intent; do not unbounded-queue | **STOP** |
| New `NewOrderSingle` | **STOP** | not gated by ML | **STOP** if price required | **STOP** | **STOP** |
| Blind resend after TCP drop | n/a | n/a | n/a | **STOP** → `EXECUTION_STATE_UNKNOWN` | n/a (cannot know persist) |
| Blind catch-up of N entries on reconnect | **STOP** | **STOP** (no promo backlog fire) | **STOP** stale shadow/live opens | **STOP** (the §63 20-trade case) | **STOP** |
| Hard risk limits | **ACTIVE** | **ACTIVE** (explicit §62) | **ACTIVE** | **ACTIVE** | **ACTIVE** once DB readable; no bypass via memory |
| Kill switch `STOP_NEW_EXECUTION` | Still honored | Still honored | Still honored | Still honored | Cannot mutate; treat as **stop-new** if last durable flag is stop **or** flag unreadable |
| `EMERGENCY_FLATTEN` | Does not invent source | Allowed if TRADE+DB+lease | **SEPARATE** (flatten is CLOSE) | **STOP** until TRADE ready | **STOP** |

## 3.2 Compound failures (AND, fail closed)

| Combination | Binding result |
|---|---|
| **Any + DB down** | DB wins. No new persist, no new NOS, no memory execution, no shadow orders. |
| MT5 + QUOTE | No new source facts **and** no priced OPEN. Existing dest positions untouched. |
| MT5 + TRADE | No new copied OPEN. Do not queue source events that arrive after reconnect as a batch. |
| QUOTE + TRADE | Venue dark. No live OPEN. No NOS. Shadow OPEN stopped. Unknown in-flight orders stay unknown until TRADE returns **and** reconcile. |
| ML + anything | ML never **unblocks** a down venue or a stale intent. ML never **bypasses** hard limits. |
| TRADE down + already-sent NOS | **§34**, not §63. Status `EXECUTION_STATE_UNKNOWN`. Recover via mass-status / positions. **Do not expire-into-resend.** |
| All five down | Health strip red. Collector retries when processes live. Zero copy. Zero promotion. Zero NOS. |

Partial MT5 (one of two brokers) is **not** a compound global outage.

---

# 4. Per-subsystem rules

## 4.1 MT5 unavailable

**Continue**

- Watchdog / connect retry (C++ `MT5Watchdog` backoff is library evidence only; C# worker must publish the same *policy*: retry, do not invent — A15, A07).
- Dashboard stale-source flag, `mt5_connected`, `mt5_reconnects`, `mt5_backfill_lag`.
- Destination QUOTE/TRADE sessions (they do not depend on MT5).
- Reconstruction / scoring / shadow **only** on **already durable** raw rows.
- Existing destination positions. Do not flatten because the source collector died.

**Stop**

- Synthesizing deals, positions, or “likely” XAU entries to keep copy aligned.
- Opening **new** copied or shadow positions from a stale broker’s tape.
- Advancing a backfill checkpoint on a failed / partial `GetDeals` (A07).
- Treating no-pump fallback as live copy-quality without a proven deal-poll freshness SLA.

**On return**

1. Reconnect. Backfill from checkpoint with overlap. Idempotent upsert.
2. Publish `source_stale = false` only after freshness ≤ `max_source_staleness`.
3. **Do not** replay the gap into `NewOrderSingle` / shadow OPEN. Gap events older than `expires_at` / `max_signal_age` **die**.
4. REDUCE/CLOSE for positions that **already** have a dest/shadow mapping may apply §6.

Reason codes: `SOURCE_STALE`. Metrics: `mt5_connected`, `mt5_reconnects`, `mt5_backfill_lag`, `mt5_outbox_backlog` (§58).

## 4.2 ML unavailable

**Continue**

- Ingestion, dedup, persist, outbox, reconstruction (§62, §12, §14).
- Deterministic baseline features/scores if that path is the configured authority (§18).
- Risk engine hard limits — **remain active** even if the last ML score is missing.
- Shadow and live **execution decisions** that do not require a **new** promotion (A23: engine does not call ML).

**Stop**

- `WATCH` / `SHADOW` → `LIVE_CANDIDATE` / `LIVE` because “the model will like this”.
- Automated model self-promotion (§71).
- Using a failed inference as score `0` or score `1`. Persist `ML_UNAVAILABLE`; leave last **durable** score in place.
- Demoting `LIVE` → `PAUSED` solely because the scorer process died. (Risk flags, max-loss, and kill switches still apply.)

**On return**

- Rescore from durable features. Do not retroactively open skipped entries.
- Promotion remains a gated, audited action (§23, §59), not a catch-up batch.

Reason codes: `ML_UNAVAILABLE` (blocks **promotion**, not hard limits). Metrics: `score_requests_total`, `score_failures_total`, `prediction_latency` (§58).

## 4.3 QUOTE FIX unavailable

**Continue**

- MT5 ingest / reconstruction.
- TRADE session reconnect + reconcile (independent sequence files — §27).
- Shadow **marks** of existing positions using last quote with quality `STALE_QUOTE` / `UNPRICED` (A24 §13). Zero is a lie; do not write `unrealized=0` when unpriced.

**Stop**

- New **live** copy that needs fresh pricing (§62, §31, §37).
- New **shadow OPEN/INCREASE** (A24 §1.3, §2.1).
- Substituting Achiever/Starwave last-deal or bars as the destination bid/ask (`PriceSource` must stay honest — §17).

**On return**

- Rebuild quote cache from the live stream. Reject OPEN until `quote_age ≤ max_quote_age`.
- **No** shadow/live OPEN catch-up of the disconnect window (§63, A24 §7.5).
- Pending REDUCE/CLOSE of **existing** shadow positions: process via A24 §8.4 (closes before surviving opens).

Reason codes: `QUOTE_FIX_UNAVAILABLE`, `QUOTE_UNAVAILABLE`, `QUOTE_STALE`, `SPREAD_TOO_WIDE` (spread is a sibling of freshness, not a session-down synonym).

Metrics: `fix_quote_connected`, `fix_reconnects`, `fix_logon_failures` (§58).

## 4.4 TRADE FIX unavailable

**Continue**

- QUOTE stream and shadow fills (shadow does not send TRADE).
- Creating **CopyIntents** from fresh source events **only if** they get `expires_at` / `max_signal_age` and are **not** dumped into an unbounded send queue.
- Marking those intents `TRADE_FIX_UNAVAILABLE` / letting them expire (§62, A23 §5.5).

**Stop**

- `NewOrderSingle`, cancel/replace except as part of an authorized path that **also** requires TRADE up.
- **Unlimited** backlog: no in-memory `ConcurrentQueue` of NOS, no “send these 20 when Logon returns”.
- Blind resend of `sent_ack_unknown` / `EXECUTION_STATE_UNKNOWN` (§34, A25 §5.5).

**On return (normative sequence)**

```text
TRADE Logon
    ↓
block new executions          -- still TRADE-down for send
    ↓
OrderMassStatusRequest
    ↓
RequestForPositions
    ↓
compare with DB
    ↓
READY_FOR_EXECUTION           -- only if reconciled
    ↓
expire OPEN/INCREASE where now >= expires_at
                         OR signal_age > max_signal_age
    ↓
re-evaluate ONLY still-fresh OPEN/INCREASE
    against a LIVE quote + full risk
    ↓
never flush a pre-disconnect send queue
```

This is §42 + §63 composed. A25 §5.6 is the session-layer twin.

Reason codes: `TRADE_FIX_UNAVAILABLE`, `RECONCILIATION_BLOCK`, `EXECUTION_STATE_UNKNOWN`, `INTENT_EXPIRED`, `SIGNAL_STALE`, `CATCH_UP_SUPPRESSED`.

Metrics: `fix_trade_connected`, `fix_unknown_execution_states`, `risk_rejections_total` (§58).

## 4.5 Database unavailable

**Continue**

- Process stay-alive, health `DATABASE_UNAVAILABLE`, FIX sockets **may** stay logged on so they do not flap — but they **must not send** application orders.
- Local retry of DB connectivity.

**Stop**

- New `copy_intents`, `risk_decisions`, `execution_intents`, shadow orders/fills.
- Any `NewOrderSingle` (cannot persist `cl_ord_id` / `not_sent` first — §33, §72.7).
- Serving risk books or `/positions` from Redis or FIX cache as if they were authoritative (A03).
- “Queue in memory and drain when Postgres returns” **for OPEN/INCREASE**. That **is** a stale backlog (§62 + §63). Those in-memory intents are discarded; only **new** source events after DB return may create intents, and they still expire.

**On return**

- Do not assume the DB matches the venue. TRADE must **re-reconcile** before `READY_FOR_EXECUTION` (§42: never assume the database is correct after restart; a DB outage is the same class of amnesia).
- Unknown in-flight (if a send raced a DB failure) stays unknown until venue reports. Prefer **false unknown** over a second NOS.

Reason codes: `DATABASE_UNAVAILABLE`.

---

# 5. No blind catch-up (normative)

## 5.1 Definition

**Blind catch-up** is any path that, after a gap, **replays a backlog** of source events or `CopyIntent`s into destination orders (live or shadow OPEN) **because the book “should look like the source history.”**

Forbidden triggers (non-exhaustive):

| Trigger | Forbidden action |
|---|---|
| TRADE reconnect after 3 minutes, 20 source opens | 20 × `NewOrderSingle` |
| QUOTE reconnect after a gap | 20 × shadow OPEN at now’s price |
| `REAL_COPY_EXECUTION_ENABLED` false → true | Drain intents accumulated while off (A25 §6.4) |
| Process restart / deploy | Flush `not_sent` OPEN older than expiry |
| Leadership / lease win (§28) | Winner sends predecessor’s queue |
| MT5 collector catch-up | Backfill is **source of truth only** (A07). It must not fan out copy NOS. |
| ML scorer return | Promote + open everything missed |
| Kill-switch lift | Fire the paused OPEN backlog without re-eval + expiry |
| Operator “sync book to sources” button | Not a legal v1 action |

## 5.2 Every `CopyIntent` carries expiry

Required fields (§63, §36, A20, A23 §3.1):

```text
source_event_time
collector_receive_time
decision_time
expires_at              -- absolute clock; hard
max_signal_age          -- duration; per-intent, may be tighter than global
```

Evaluation (A23 §6.2):

```text
signal_age = decision_time - source_event_time

if now >= expires_at:                    REJECT  INTENT_EXPIRED
if signal_age > intent.max_signal_age:   REJECT  SIGNAL_STALE
if signal_age > max_source_signal_age:   REJECT  SIGNAL_STALE   # global cap
```

The **stricter** bound wins. Measure the latency chain even on reject (§36).

`expires_at` is set at **intent creation**, not at reconnect. Extending expiry because FIX was down **is** catch-up and is forbidden.

## 5.3 What reconnect **may** do

For each persisted OPEN/INCREASE intent, **individually**:

1. If expired or over-age → terminal `INTENT_EXPIRED` / `SIGNAL_STALE` / `CATCH_UP_SUPPRESSED`. No send.
2. If still fresh → run the **full** risk path against a **live** quote and current book. Freshness is re-checked at send instant (A25 §6.3).
3. If source opened **and** fully closed in the gap **and** no dest/shadow position exists → drop both (`CATCH_UP_SUPPRESSED` + `NO_SHADOW_POSITION` / `MAPPING_MISSING`). Do not open-then-close to “match history” (A24 §8.4).

There is **no** batch send API.

## 5.4 Worked example (must be a test)

```text
t=0     TRADE healthy, REAL_COPY_EXECUTION_ENABLED=true
t=0     TRADE disconnects
t=0..180s   20 source XAU opens persist as CopyIntents
            each expires_at = source_event_time + max_signal_age  (e.g. << 180s)
t=180s  TRADE Logon
t=180s  block send; mass-status + positions; READY_FOR_EXECUTION
t=180s  20 intents: now >= expires_at → INTENT_EXPIRED
        NewOrderSingle count == 0
        fix_outbound NewOrderSingle delta == 0
```

If a 21st source open occurs at `t=181s` with `signal_age` under the cap and quote fresh: **that one** may be evaluated. It is not “the backlog.”

Same story for shadow OPEN with QUOTE down 3 minutes (A24 §7.5, A27 `Replay.NoBlindCatchUpReplayTests`).

## 5.5 Queues that are illegal

```text
List<CopyIntent> _pendingWhileTradeDown;   // drain on Logon
Channel<NewOrderSingle> _nosOutboxMemory;  // unbounded
"retry all status=not_sent on startup"     // without expiry filter
```

Legal durable states:

```text
copy_intents.status = REJECTED / EXPIRED / TRADE_FIX_UNAVAILABLE
execution_intents.status = not_sent   -- only if unexpired AND DB up;
                                        send path still re-checks everything
execution_intents.status = sent_ack_unknown / EXECUTION_STATE_UNKNOWN
                                        -- recover, never batch-resend
```

Cap in-flight `not_sent` OPEN rows with a configured bound if needed. Hitting the bound is `TRADE_FIX_UNAVAILABLE` / reject, **not** silent drop-and-later-replay.

---

# 6. REDUCE / CLOSE is not catch-up of entries

§63 last sentence and §64 are part of this spec.

| Class | Catch-up of **entries** | After a gap |
|---|---|---|
| `OPEN_EXPOSURE` | Forbidden | Expire / re-eval if still fresh |
| `INCREASE_EXPOSURE` | Forbidden | Same as OPEN |
| `REDUCE_EXPOSURE` | Not “entry catch-up” | **May** close a **known** dest/shadow slice even if the close signal is older than `max_signal_age` **open** |
| `CLOSE_EXPOSURE` | Not “entry catch-up” | Same; prefer processing closes **before** surviving opens |

REDUCE/CLOSE still require:

- Durable dest / shadow **mapping** (`linked_destination_position_id` / shadow position). No mapping → `MAPPING_MISSING` / `NO_SHADOW_POSITION`. **Do not open a position just to close it.**
- TRADE + lease + persist-before-send for **live** flatten/close.
- DB up.
- Identity / idempotency (`cl_ord_id` unique; no blind retry).

REDUCE/CLOSE must **not** be blocked by OPEN-only guards: `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `SIGNAL_STALE` at the **open** threshold, `STOP_NEW_EXECUTION` (A23 §5.3, A24 §8.2). `STOP_NEW_EXECUTION` leaves existing positions untouched (§40). `EMERGENCY_FLATTEN` is a separately permissioned CLOSE path.

QUOTE down + live CLOSE: do not invent a price; still require TRADE. Shadow CLOSE uses A24 waterfall (live touch → last quote → `UNPRICED` hold).

---

# 7. Unknown-state recovery is not catch-up

Do not implement §62 TRADE-down “do not resend unknown orders” as intent expiry.

| Situation | Law | Action |
|---|---|---|
| Intent **never sent** (`not_sent`), gap longer than expiry | §63 | Expire. No NOS. |
| NOS **possibly reached** cServer, TCP died | §34, §72.9 | `EXECUTION_STATE_UNKNOWN`. Mass-status / positions. **No** second NOS with same or new `cl_ord_id` until venue proves **absent**. |
| Venue proves absent + intent still fresh | §34 after reconcile | **New** `cl_ord_id`, new persist, full risk. Rare. |
| Venue proves absent + intent expired | §34 + §63 | Do **not** replace. |
| Duplicate ER | §60 / A25 | Idempotent. Not a resend. |

Illegal:

```text
catch (IOException) { SendNewOrderSingle(sameClOrdId); }
catch (IOException) { SendNewOrderSingle(newClOrdId); }
on Logon: foreach unknown in list Send(it);
```

Legal (A25 §5.5):

```text
on transport fail after possible send:
    status = sent_ack_unknown
    increment fix_unknown_execution_states
    schedule reconcile
    do not send
```

Any unresolved unknown ⇒ not `READY_FOR_EXECUTION` ⇒ `EXECUTION_STATE_UNKNOWN` / `RECONCILIATION_BLOCK` on **new** OPEN (A23 §5.4).

---

# 8. Evaluation order (live send)

Fail closed on the first blocker. Same order as A23 §5 so tests stay stable. **This document owns steps 1 and 5–6 and the catch-up filter.**

1. **DB available** — else `DATABASE_UNAVAILABLE`, no persist, no send.
2. Feature flag `REAL_COPY_EXECUTION_ENABLED` (live path only).
3. Kill switch: `STOP_NEW_EXECUTION` / flatten-in-progress block OPEN/INCREASE.
4. Reconcile + no unresolved unknowns → else `RECONCILIATION_BLOCK` / `EXECUTION_STATE_UNKNOWN`.
5. **QUOTE health** for priced OPEN/INCREASE.
6. **TRADE health** = `READY_FOR_EXECUTION` + lease. Else mark / expire; **no unbounded queue**.
7. **Source health** — `SOURCE_STALE` ⇒ no new copied OPEN from that broker.
8. **Expiry / signal age** (§5.2). This is the catch-up killer.
9. Quote age / spread / price-move (A23 §6.1–6.3).
10. Sizing + book limits (A23).
11. Persist `risk_decision`; if approved, persist `execution_intent` `not_sent`; only then socket write.

ML down is **not** a step here. Promotion is a **different** state machine.

---

# 9. Reason codes (closed set for this spec)

Stable strings for `risk_decisions.primary_reason`, metrics, dashboard. Align with A23 §4.3 / A24 §7.6 / §8.6.

| Code | Typical when | Blocks OPEN/INCREASE | Blocks REDUCE/CLOSE |
|---|---|---|---|
| `SOURCE_STALE` | MT5 / collector stale | Yes (that broker) | Only if **no** persisted close event |
| `ML_UNAVAILABLE` | Scorer/model down | No (not a send gate) | No |
| `QUOTE_FIX_UNAVAILABLE` | QUOTE session down | Yes | No (live still needs TRADE; shadow uses waterfall) |
| `QUOTE_UNAVAILABLE` | No instrument quote | Yes | Shadow: waterfall / UNPRICED |
| `QUOTE_STALE` | `quote_age` over max | Yes | No at OPEN threshold |
| `TRADE_FIX_UNAVAILABLE` | TRADE not ready | Yes | Yes for **live send** |
| `DATABASE_UNAVAILABLE` | Postgres down | Yes | Yes |
| `RECONCILIATION_BLOCK` | Not `READY_FOR_EXECUTION` | Yes | Yes for live send |
| `EXECUTION_STATE_UNKNOWN` | Unresolved sent order | Yes (new OPEN) | Do not send another NOS to “fix” |
| `SIGNAL_STALE` | `max_signal_age` | Yes | No at OPEN threshold |
| `INTENT_EXPIRED` | `expires_at` | Yes | Close may still run if mapping exists and policy allows |
| `CATCH_UP_SUPPRESSED` | Gap replay refused | Yes (terminal) | n/a (open never existed) |
| `MAPPING_MISSING` | No dest link | n/a | Yes |
| `NO_SHADOW_POSITION` | Shadow close, no book | n/a | Terminal, not an error if open never accepted |
| `REAL_EXECUTION_DISABLED` | Flag off | Yes | Flatten policy separate |
| `STOP_NEW_EXECUTION` | Kill switch | Yes | No |
| `INTENT_INCOMPLETE` | Missing identity / expiry fields | Yes | Yes |

An intent **without** `expires_at` and `max_signal_age` is `INTENT_INCOMPLETE`. Missing expiry is not “never expires.”

---

# 10. Health, persistence, logs, metrics

## 10.1 Durable health (not only gauges)

Publish (A20 `system_events`, A06 `/api/v1/system/health`):

```text
mt5[broker_id].connected
mt5[broker_id].source_stale
mt5[broker_id].source_stale_since
mt5[broker_id].last_event_time
ml.available
ml.not_in_use                 -- first useful version
quote.session_status
quote.last_inbound
trade.session_status          -- ReadyForExecution only when reconciled
trade.lease_owned
trade.unresolved_unknowns
db.available
catch_up.suppressed_total     -- increment on §63 drops
```

Dashboard Overview health strip (§47, A26) must show MT5 + QUOTE + TRADE + (implicit) API/DB. Stale-source must be visible; do not paint a green “copied” count while `SOURCE_STALE`.

## 10.2 Tables this spec requires (already named)

From A20 / §44–§45:

- `copy_intents` — UNIQUE `(source_broker_id, source_login, source_trade_id, source_event_id, action)`; carry `expires_at`, `max_signal_age`, timestamps
- `risk_decisions` / `risk_events`
- `execution_intents` + `fix_orders` + `fix_execution_reports`
- `destination_quotes`, `destination_positions`, `source_destination_links`
- `fix_sessions` / `fix_session_events` / leases
- `system_events`, `audit_logs`

No extra table is required to implement §62–§63.

## 10.3 Logging (§57)

Every reject / expiry / unknown / reconnect decision:

```text
correlation_id, broker_id, source_login, source_trade_id
copy_intent_id, risk_decision_id, execution_intent_id
cl_ord_id, cserver_order_id, destination_position_id
fix_session, fencing_token
primary_reason, exposure_class
signal_age, quote_age, source_stale, session_status
```

Never log FIX/MT5 passwords or proxy secrets.

## 10.4 Metrics (§58) owned by failure handling

| Metric | Role |
|---|---|
| `mt5_connected` | MT5 down detector |
| `mt5_reconnects` | retry, not catch-up |
| `mt5_backfill_lag` | stale-source input |
| `score_failures_total` | ML down |
| `fix_quote_connected` | QUOTE down |
| `fix_trade_connected` | TRADE down |
| `fix_unknown_execution_states` | §34, not §63 |
| `risk_rejections_total{reason}` | include all codes in §9 |
| `copy_intents_total{status}` | include `EXPIRED`, `CATCH_UP_SUPPRESSED` |

A test must be able to assert **zero** `fix_outbound_messages_total` of type NOS across the 20-intent example.

---

# 11. Tests (acceptance for this spec)

Must exist before live copy (A27 names are the intended classes). None of these exist as passing product tests today.

| Class | Must prove |
|---|---|
| `Risk.StaleCopyIntentExpiryTests` | `expires_at` / `max_signal_age`; **0 NOS** for 20 intents after 3-minute FIX gap |
| `Replay.NoBlindCatchUpReplayTests` | FIX-down gap in the tape does not fire expired intents on resume |
| `Harness.QuoteUnavailableBlocksNewCopyTests` | QUOTE down / stale → no new priced live/shadow OPEN |
| `Harness.TradeUnavailableDoesNotQueueUnlimitedBacklogTests` | TRADE down → mark/expire; queue depth bounded; Logon does not drain |
| `Execution.UnknownExecutionStateTests` | Disconnect after send → unknown; **no** blind NOS |
| `Harness.UnknownStateRecoveryTests` | Recovery via status/positions; replacement only with **new** `cl_ord_id` after proven absent |
| `Harness.DisconnectAfterNewOrderSingleTests` | Same as unknown; QUOTE vs TRADE independent |
| `Risk.QuoteFreshnessGuardTests` | `quote_age > max` rejects OPEN |
| `Risk.OpenVsCloseExposurePolicyTests` | CLOSE not blocked by OPEN stale/spread/move |
| `Risk.ScoringCannotBypassRiskTests` | ML/score cannot send through a §62 fail |
| `Risk.RealExecutionFeatureFlagTests` | Flag off → no NOS; flag on does not flush backlog |
| `Harness.StartupReconciliationAfterSimulatedRestartTests` | Restart → block → reconcile → ready or stay blocked |
| `Harness.ReconciliationBlocksExecutionWhileInconsistentTests` | Inconsistent book → no NOS |
| `Mt5` collector tests | Failed `GetDeals` does not invent rows or advance checkpoint |

Go-live boxes this spec owns (§68, §70):

```text
[ ] unknown execution state recovery works
[ ] stale quote rejection works
[ ] stale signal rejection works
[ ] reconciliation blocks execution while inconsistent
[ ] risk-engine rejection happens before FIX send
[ ] no 20-intent catch-up after a 3-minute TRADE/QUOTE gap
```

---

# 12. Implementation status (measured, not hoped)

Read-only check on 2026-08-18. **Do not treat Domain enums as a working fail-closed engine.**

| Piece | Path | Classification |
|---|---|---|
| Architecture law | `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §62–§63 | **EXISTS_AND_GOOD** |
| This spec | this file | **EXISTS_AND_GOOD** (planning) |
| Sibling risk/session/shadow | `A23`, `A24`, `A25` | Specs only |
| `KillSwitchMode`, `FixSessionStatus`, `CopyIntentAction`, `ExecutionOrderStatus`, `RiskDecisionOutcome`, `FeatureQuality`, `PriceSource` | `D:\Prop\src\Domain\Enums\` | **EXISTS** as enums; **no engine** |
| `CopyIntents` entity + expiry fields | Domain | **MISSING** (`CopyIntentAction` enum only). `TraderDbContext` **names** `CopyIntents` / `RiskDecisions` / `ExecutionIntents` / `DestinationQuotes` / `FixSessionStates` but those types and EF configurations are **not** in tree (only `BrokersConfiguration.cs`). |
| Reason codes `SOURCE_STALE`, `QUOTE_FIX_UNAVAILABLE`, … | product `*.cs` | **MISSING** (zero matches) |
| Application fail-closed services | `D:\Prop\src\Application\Class1.cs` | **MISSING** |
| Risk engine / expiry policy / health aggregator | C# | **MISSING** |
| FIX worker QUOTE/TRADE down gates | `apps/fix-worker` | **MISSING** (A08: QUOTE-down cannot fire) |
| MT5 worker stale-source publish | `apps/mt5-worker` | **MISSING** (A07). C++ `MT5Watchdog` retries a single manager; it is **not** wired to this product’s copy path. |
| Tests in §11 | `tests/` | **MISSING** (placeholders only) |

**Verdict:** Failure rules are **specified**, not implemented. Enabling `REAL_COPY_EXECUTION_ENABLED` in this tree would have **no** §62–§63 protection. That is a hard go-live fail, not a polish item.

---

# 13. Traceability

| Topic | Architecture | This spec | Sibling |
|---|---|---|---|
| Five outages | **§62** | §2–§4 | A23 §9 |
| No blind catch-up | **§63** | §5 | A23 §6.2, A24 §7.5, A25 §5.6 / §6.4 |
| Persist before send | §32–§33, §72.7–8 | §4.5, §8 | A23, A25 |
| Unknown after send | §34, §72.9 | §7 | A25 §5 |
| Reconcile before ready | §42–§43, §70.14, §72.10 | §4.4, §8 | A25 §7 |
| Independent QUOTE/TRADE | §27, §72.11 | §2.3–§2.4 | A25 |
| Single TRADE owner | §28 | §2.4, §5.1 | A25 |
| Quote freshness | §31, §37 | §2.3, §4.3 | A23 §6.1 |
| Signal timestamps | §36, §72.17 | §5.2 | A23 §6.2 |
| OPEN vs CLOSE | §64, §72.18 | §6 | A23 §2, A24 §8 |
| ML never bypasses risk | §39, §72.15 | §4.2 | A23 |
| Feature flags | §41 | §5.1 flag flip | A25 §6 |
| Kill switches | §40 | §3.1, §6 | A23 §8 |
| First version needs no ML | §69 | §2.2 `ML_NOT_IN_USE` | A28 |
| Metrics / logs | §57–§58 | §10 | A06, A26 |
| Tests / gates | §60–§61, §68, §70 | §11 | A27, A28 |

---

# 14. Explicit non-goals

- No product source changes in this task.
- No hardcoded production ages, spreads, or lot sizes.
- No Kafka / mesh / extra venue to “buffer catch-up” (§71). A durable outbox of **source facts** is not a send queue.
- No malware, cracks, or unofficial EX5 tools.
- No MQ5 emission.
- No Redis-as-authority for execution or shadow books.
- No concentration engine (§65 Phase 2).
- No conflating `STOP_NEW_EXECUTION` with `EMERGENCY_FLATTEN`.
- No treating MT5 **historical backfill** as copy catch-up.

---

# 15. Engineer checklist (implement later; not done here)

```text
[ ] CopyIntent persist requires expires_at + max_signal_age else INTENT_INCOMPLETE
[ ] Health snapshot: MT5 per broker, ML, QUOTE, TRADE, DB
[ ] Risk step 1 = DB; missing DB = no send
[ ] QUOTE down/stale blocks priced OPEN (live + shadow)
[ ] TRADE not ReadyForExecution = no NOS; intents marked, not queued unbounded
[ ] MT5 stale = SOURCE_STALE; collector retries; no invented deals
[ ] ML down = no promotion; ingestion continues; hard limits stay on
[ ] Reconnect / restart / lease win / flag-on = expire then individual re-eval
[ ] 20-intent / 3-minute test is red until NOS count == 0
[ ] Unknown send = EXECUTION_STATE_UNKNOWN, never catch-up resend
[ ] REDUCE/CLOSE mapped positions use separate policy
[ ] Dashboard shows stale-source and venue-down without greenwashing
```

**DONE condition for implementation (future work):** reviewer PASS + tests in §11 PASS + this checklist evidenced on disk. This artifact only specifies the rules.
)
