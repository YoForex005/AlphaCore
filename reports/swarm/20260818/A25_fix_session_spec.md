# A25 — cTrader FIX Session Specification

**Status:** binding implementation spec (pre-code)  
**Date:** 2026-08-18  
**Scope:** architecture §§25–34, 41–43 (plus the safety-adjacent rules those sections require)  
**Product source:** do not implement from this file until a later coding task. This document is specification only.  
**Primary architecture:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Official RoE (verify before first Logon):**  
https://help.ctrader.com/fix/specification/  
https://help.ctrader.com/fix/sending-and-receiving-messages/  
https://help.ctrader.com/fix/communication-model/  
https://help.ctrader.com/fix/faqs/

This spec exists so the FIX adapter is not “open a socket and send a market order.”  
It specifies **two independent sessions**, the **header-mapping warning**, **single-active ownership**, **unknown execution state**, and **feature flags**. Those five items are the live-account safety boundary.

---

## 0. Verdict

Pepperstone / cServer FIX 4.4 is the **external execution venue**, not an LP. Real approved copy trades route to account `1369850` through **two TLS FIX 4.4 sessions** (QUOTE + TRADE).

The current repo is **not ready** to open those sessions:

| Location | Measured state |
|---|---|
| `D:\Prop\src\Fix.CTrader\` | empty stub (`Class1`) |
| `D:\Prop\apps\fix-worker\` | generic `BackgroundService` heartbeat; no FIX, no flags, no lease |
| `D:\Prop\apps\fix-worker\appsettings.json` | logging only |

Until Logon is proven for **both** sessions in diagnostics, `REAL_COPY_EXECUTION_ENABLED` stays `false`.

Do **not** write a raw `TcpClient` FIX engine. Prefer QuickFIX/n with a cTrader-specific data dictionary and session config. Official Spotware sample code is a teaching aid, not a production engine (it uses one sequence counter across send helpers, blocking reads, and no reconnect/ownership).

---

## 1. Non-goals (this spec)

- Sending `NewOrderSingle` from an MT5 callback.
- Active-active TRADE sessions / cross-region FIX.
- Kafka, K8s, ClickHouse, or a custom FIX codec.
- Hardcoding a Pepperstone XAUUSD instrument ID.
- Blind lot → `OrderQty` conversion.
- Treating the broker form’s field labels as FIX tag numbers.

---

## 2. Two independent sessions

### 2.1 Why two objects

cTrader price quotation and trade messaging use **separate TCP/TLS connections and separate ports**. Architecture §1 / §27: QUOTE and TRADE are **not** one multiplexed session with a qualifier flag. They are two FIX sessions.

Maintain two independent session objects:

```text
CTraderQuoteSession
CTraderTradeSession
```

Never share:

```text
socket / SslStream
FIX session ID
message sequence counters (in + out)
heartbeat / TestRequest clock
last inbound timestamp
last outbound timestamp
reconnect / backoff state
metrics series
log scope
QuickFIX/n SessionSettings / store / log factory
```

Do **not** share one sequence counter between QUOTE and TRADE.

### 2.2 Production endpoints (architecture §25)

```text
Host: live-us-eqx-01.p.c-trader.com

QUOTE SSL  = 5211     QUOTE plain = 5201
TRADE SSL  = 5212     TRADE plain = 5202

SenderCompID (both) = live.pepperstone.1369850
TargetCompID (issued form) = cServer
Session qualifier   = QUOTE | TRADE
Account / Username  = 1369850
Password            = <SECRET: account 1369850>
```

**Production transport default is TLS.** Plain-text ports exist only for local diagnostics and must not be the production default (`CTRADER_FIX_USE_SSL=true`).

### 2.3 Independent state machine (each session)

```text
DISABLED
  → CONNECTING
  → LOGGING_ON
  → LOGGED_ON
  → (QUOTE) SUBSCRIBING / QUOTING
  → (TRADE) RECONCILING → READY_FOR_EXECUTION | BLOCKED_INCONSISTENT
  → LOGGING_OUT
  → DISCONNECTED
  → RECONNECT_BACKOFF
  → FAILED
```

Rules:

1. A session may be `DISABLED` by its own feature flag without affecting the other session’s TCP connection.
2. `CTraderTradeSession.READY_FOR_EXECUTION` is **not** implied by Logon. It requires §13 startup reconciliation **and** flags in §15.
3. QUOTE failure must not tear down TRADE (and vice versa). Failure **does** change risk/execution policy (§16).
4. Sequence files / stores are per session ID. A reset on QUOTE must not reset TRADE.

### 2.4 Session identity (QuickFIX/n)

QuickFIX session key is typically:

```text
BeginString / SenderCompID / TargetCompID [ / SessionQualifier ]
```

For cTrader, the **session qualifier is `TargetSubID` (tag 57)**, values `QUOTE` and `TRADE`. Configure two SessionSettings blocks. Map the QuickFIX `SessionQualifier` (or equivalent custom header field) onto **tag 57**, not onto tag 50, unless a proven broker-issued form says otherwise (see §3).

Recommended logical IDs (persist on `fix_sessions`):

| session_key | role | port (TLS) | qualifier |
|---|---|---|---|
| `pepperstone-1369850-QUOTE` | market data + security list | 5211 | `QUOTE` |
| `pepperstone-1369850-TRADE` | orders, status, positions | 5212 | `TRADE` |

### 2.5 Official session behaviour that the adapter must honour

From the current cTrader RoE / FAQ (re-check at implementation time):

| Rule | Consequence |
|---|---|
| Sequence numbers reset on establishing a session (`ResetSeqNumFlag=Y`, tag 141) | Do not assume persisted seq continuity across Logon unless a later RoE says otherwise. Still persist **application** order/position state in PostgreSQL. |
| Heartbeat interval default 30s (`HeartBtInt` 108) | Configurable. `0` means no heartbeat required. |
| Quote feed may omit heartbeat while quotes stream | Do **not** treat missing Heartbeat on QUOTE as dead if incremental quotes are fresh. Use **quote age** as liveness. |
| Multiple simultaneous API connections duplicate reports | Two production owners of TRADE (or two TRADE sockets) will double ExecutionReports. Ownership is mandatory (§4). |
| Invalid Logon → Logout with `Text` (58) | Persist the text. Do not retry with mutated headers in a tight loop. |
| `Username` (553) = numeric trader login; `SenderCompID` (49) = `env.brokerUid.login` | `553=1369850`, `49=live.pepperstone.1369850`. Do not put the password anywhere except 554 and the secret store. |

### 2.6 What each session is allowed to send

| Message | QUOTE | TRADE |
|---|---|---|
| Logon / Logout / Heartbeat / TestRequest / Resend / Reject / SequenceReset | yes | yes |
| SecurityListRequest / SecurityList | yes (preferred for discovery; TRADE may also) | yes |
| MarketDataRequest / Snapshot / IncrementalRefresh | **yes** | no |
| NewOrderSingle / Cancel / CancelReplace | **no** | yes, and only if §15 allows |
| ExecutionReport (inbound) | no | yes |
| OrderStatusRequest / OrderMassStatusRequest | no | yes |
| RequestForPositions / PositionReport | no | yes |
| BusinessMessageReject (inbound) | yes | yes |

A MarketDataRequest on TRADE, or a NewOrderSingle on QUOTE, is a defect.

### 2.7 Capabilities that must exist before “FIX is done” (architecture §29)

Minimum workflows — not “send market order”:

```text
Logon, Logout, Heartbeat, TestRequest
ResendRequest / sequence handling
Reject, BusinessMessageReject

SecurityListRequest / SecurityList

MarketDataRequest
MarketDataSnapshot
MarketDataIncrementalRefresh

NewOrderSingle
ExecutionReport
OrderStatusRequest
OrderMassStatusRequest

RequestForPositions
PositionReport

OrderCancelRequest / OrderCancelReject
OrderCancelReplaceRequest
```

Phase gating: QUOTE + SecurityList + quotes first (architecture Phase 4). TRADE read/reconcile next (Phase 7). NewOrderSingle last (Phase 8), still behind `REAL_COPY_EXECUTION_ENABLED`.

---

## 3. Header mapping warning (architecture §26)

### 3.1 The defect this section exists to prevent

Do **not** blindly infer FIX tag placement from the human-readable credential form.

The issued connection details label the session qualifier as:

```text
SenderSubID = QUOTE / TRADE
```

The official cTrader FIX Rules of Engagement define session-related header fields including:

```text
SenderCompID   (49)
TargetCompID   (56)
TargetSubID    (57)   ← required session qualifier: QUOTE | TRADE
SenderSubID    (50)   ← optional originator; MUST be QUOTE if TargetSubID=QUOTE
```

Those are **not the same tag**. Mapping the form’s “SenderSubID = QUOTE/TRADE” onto tag 50 **and leaving tag 57 empty** is a likely Logon failure. Mapping it only onto 50 and also hardcoding 57 is acceptable only after a successful diagnostic Logon.

### 3.2 Official header (client → cServer), current RoE

| Tag | Name | Required | Official value / rule |
|---|---|---|---|
| 8 | BeginString | Y | `FIX.4.4` (first field) |
| 9 | BodyLength | Y | computed (second field) |
| 35 | MsgType | Y | third field |
| 49 | SenderCompID | Y | `<Environment>.<BrokerUID>.<Trader Login>` → `live.pepperstone.1369850` |
| 56 | TargetCompID | Y | RoE text says `CSERVER` |
| 57 | TargetSubID | Y | `QUOTE` or `TRADE` |
| 50 | SenderSubID | N | any string; **must be `QUOTE` when TargetSubID=`QUOTE`** |
| 34 | MsgSeqNum | Y | per session, starting at 1 after reset |
| 52 | SendingTime | Y | UTC |

Logon body:

| Tag | Name | Rule |
|---|---|---|
| 98 | EncryptMethod | `0` (transport TLS only; FIX-level encryption unused) |
| 108 | HeartBtInt | default 30 |
| 141 | ResetSeqNumFlag | `Y` on establish (RoE) |
| 553 | Username | numeric login `1369850` |
| 554 | Password | secret; never logged |

### 3.3 The case and label traps

| Trap | Why it is dangerous | Required behaviour |
|---|---|---|
| Form says `cServer`, RoE says `CSERVER` | Silent uppercasing can Logon-fail or Logon-succeed depending on acceptor | **Do not silently change case.** Persist the issued string. Make `TargetCompID` configurable. Prove Logon in diagnostics. If Logon fails on `cServer`, try `CSERVER` only as an **explicit, logged override** (`CTRADER_FIX_*_TARGET_COMP_ID`), never as a hidden mutate. |
| Form says `SenderSubID = QUOTE/TRADE` | Official qualifier is **TargetSubID (57)** | Both `SenderSubID` and `TargetSubID` are independently configurable per session. Default `TargetSubID` to the session qualifier. Default QUOTE `SenderSubID` to `QUOTE`. TRADE `SenderSubID` defaults to the broker-issued value, not a guessed `TRADE`. |
| Sample code uses `56=CSERVER` and `57=qualifier` and `50=_senderSubID` | Samples are dated (one article is frozen at RoE v2.9.1, 2017) | Follow **current** RoE + **this account’s issued form**. Do not hardcode an old sample. |
| `SenderCompID` format | RoE: `live.theBroker.12345`. Form: `live.pepperstone.1369850` | Use the issued CompID verbatim. |
| Password / username in logs or dashboard | FAQ and architecture §52 / §57 | Never log 554. Never show FIX password in the UI. Redact 553/554 at the sink. |

### 3.4 Configurable header fields (mandatory)

Every value below is **config**, not a constant in `Fix.CTrader`.

```env
CTRADER_FIX_HOST=live-us-eqx-01.p.c-trader.com
CTRADER_FIX_ACCOUNT_ID=1369850
CTRADER_FIX_PASSWORD=<SECRET>

CTRADER_FIX_USE_SSL=true
CTRADER_FIX_HEARTBT_INT=30
CTRADER_FIX_RESET_SEQ_NUM=true

# QUOTE
CTRADER_FIX_QUOTE_SSL_PORT=5211
CTRADER_FIX_QUOTE_PLAIN_PORT=5201
CTRADER_FIX_QUOTE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_QUOTE_SESSION_QUALIFIER=QUOTE
CTRADER_FIX_QUOTE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_QUOTE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>

# TRADE
CTRADER_FIX_TRADE_SSL_PORT=5212
CTRADER_FIX_TRADE_PLAIN_PORT=5202
CTRADER_FIX_TRADE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_SESSION_QUALIFIER=TRADE
CTRADER_FIX_TRADE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_TRADE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>
```

Engineer must populate `SenderSubID` / `TargetSubID` from the **current** broker-issued FIX form **and** the current RoE. If the form only prints one “session qualifier” field, write that value into `*_SESSION_QUALIFIER` **and** into `*_TARGET_SUB_ID`, then set `*_SENDER_SUB_ID` as:

- QUOTE: `QUOTE` (RoE: required when TargetSubID=QUOTE)
- TRADE: issued value if present, else a stable configured string (official examples use `any_string`; do not invent a second semantic)

### 3.5 Header construction rules

1. Preserve exact broker-issued credentials (including case).
2. Make **both** `SenderSubID` and `TargetSubID` configurable.
3. Follow the current official cTrader Rules of Engagement.
4. Never silently change case (`cServer` → `CSERVER`) unless the issued configuration/spec requires it.
5. Prove successful Logon for **both** sessions in staging/diagnostics before enabling execution.
6. Do not hardcode assumptions from an old sample.
7. Tag order and required tags must match current RoE (FAQ: missing tags, wrong order, bad checksum, non-UTC time → **no response**).
8. `BeginString` / `BodyLength` / `MsgType` stay in FIX-required positions; the engine (QuickFIX/n), not application code, should compute 9 and 10.

### 3.6 Diagnostic Logon gate (required before any application message)

A “header mapping proven” record must exist (file or `fix_session_events`) containing, for **each** session:

```text
timestamp (UTC)
host, port, TLS yes/no
SenderCompID, TargetCompID (as sent)
SenderSubID, TargetSubID (as sent)
Username (numeric only; no password)
ResetSeqNumFlag, HeartBtInt
outbound Logon checksum-valid? (yes/no)
inbound MsgType (A or 5)
inbound Text (58) if Logout
result: LOGON_OK | LOGON_REJECTED | NO_RESPONSE | TRANSPORT_FAIL
```

Only `LOGON_OK` on **both** sessions unlocks Phase 4+ application messages. A QUOTE-only success does **not** unlock TRADE application messages.

---

## 4. FIX session ownership (architecture §28)

### 4.1 Invariant

For one cTrader trading account, **do not allow two production instances to simultaneously own the same TRADE session.**

Official FAQ: multiple simultaneous FIX connections cause the server to send a **copy of each report to every active connection**. Duplicate ExecutionReports plus two senders = duplicate orders.

QUOTE is also a single-owner session in production (duplicate quotes are cheaper than duplicate fills, but still corrupt freshness and metrics). Enforce the same lease for QUOTE unless a later measurement justifies a read-only replica — and a replica is **out of scope**.

### 4.2 Authority

```text
PostgreSQL is the authority for execution state.
The FIX socket is not the authority.
Process memory is not the authority.
```

If the process dies holding a TCP session, cServer may still have the order. The next owner **must** reconcile from cServer and the DB, not from the previous process’s memory.

### 4.3 Mechanism (choose one; implement the first that the repo already has)

Allowed (architecture §28):

1. Deployment singleton (one `fix-worker` replica) **plus** a DB lease so a second replica cannot silently start.
2. PostgreSQL advisory lock.
3. Redis lease **with fencing token**.
4. Leader election that still writes the lease to the DB.

**Recommended for this repo (Postgres-first, no Redis required):** table-backed lease + fencing token.

```text
fix_session_leases
  venue_id
  session_key          -- pepperstone-1369850-TRADE
  owner_instance_id    -- hostname + pid + boot uuid
  fencing_token        -- monotonic bigint (DB sequence)
  leased_until
  last_renew_at
  state                -- ACQUIRING | OWNED | RELEASING | FENCED
```

Rules:

- Acquire with `UPDATE … WHERE leased_until < now() OR owner = me` returning a **new** fencing token.
- Renew at ≤ ⅓ of lease TTL.
- Every outbound TRADE application message (`D`, `F`, `G`, `H`, `AF`, `AN`, …) carries the fencing token into the persist-before-send row.
- If a write’s token ≠ current lease token, **drop the send** (fenced). Do not send “anyway.”
- On shutdown: Logout, then release lease. If Logout fails, still release lease; the next owner reconciles.
- Losing the lease mid-flight: mark in-flight intents `EXECUTION_STATE_UNKNOWN` (§5), do **not** reconnect from the loser.

### 4.4 Leadership change (mandatory order)

```text
new instance
    ↓
acquire lease (fencing token N+1)
    ↓
establish FIX TRADE session (TLS + Logon)
    ↓
block new executions
    ↓
reconcile orders + positions (§13)
    ↓
only if reconciled: READY_FOR_EXECUTION
    ↓
only then accept new execution intents
```

Never: Logon → immediately drain `execution_intents` → NewOrderSingle.

### 4.5 What ownership does **not** allow

- Two `fix-worker` processes with `CTRADER_FIX_TRADE_SESSION_ENABLED=true` on the same account.
- A developer laptop TRADE session against the live account while production holds it.
- Sharing the live TRADE password with a second environment “just to watch.”
- Treating QUOTE ownership as a substitute for TRADE ownership.

Diagnostics against the live account require an explicit, single-operator run with production TRADE **disabled** (`CTRADER_FIX_TRADE_SESSION_ENABLED=false`) unless production is stopped and the lease is held by the diagnostic process.

---

## 5. Unknown execution state (architecture §33–§34)

### 5.1 The critical case

```text
persist intent + cl_ord_id
   ↓
send NewOrderSingle
   ↓
network disconnects / process dies / no ExecutionReport
   ↓
did cServer receive it?
```

**Do NOT blindly send the order again.**  
**Do NOT increment a retry counter and emit a new ClOrdID for the same intent unless reconciliation proved the first ClOrdID never existed on the venue.**

Set:

```text
EXECUTION_STATE_UNKNOWN
```

### 5.2 Persist-before-send (architecture §33)

Every destination order has a **unique** `cl_ord_id` allocated **before** the socket write. Persist at least:

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
fencing_token
fix_session_key
sent_at                  -- null until the engine reports "written to socket"
last_venue_update_at
```

The execution service must distinguish:

```text
not_sent
sent_ack_unknown          -- EXECUTION_STATE_UNKNOWN
accepted
partially_filled
filled
rejected
cancelled
```

`not_sent` is the only state that may transition to a first `NewOrderSingle`.  
`sent_ack_unknown` may **only** leave via reconciliation (§5.4), never via “retry send.”

### 5.3 When to enter `EXECUTION_STATE_UNKNOWN`

Enter (or stay in) unknown if **any** of:

1. Socket write of `NewOrderSingle` / `OrderCancelRequest` / `OrderCancelReplaceRequest` returned success or indeterminate, and no terminal ExecutionReport arrived before disconnect.
2. Disconnect after write, before Logon of the next session completes.
3. Process crash after persist of `sent_at` (or after a write whose persist of `sent_at` is itself uncertain — treat as sent).
4. Leadership loss after a send.
5. Resend/gap-fill leaves the order’s application state ambiguous.
6. Duplicate or out-of-order ExecutionReports that do not match the persisted `cl_ord_id` uniquely.

If persist of the outbound row **failed**, do **not** send. Fail closed.

If persist succeeded but the process cannot prove the write did **not** happen (crash between persist and write, or persist of `sent_at` missing), treat as **unknown**, not as `not_sent`.

### 5.4 Recovery (only legal path out of unknown)

```text
EXECUTION_STATE_UNKNOWN
        ↓
block additional NewOrderSingle for this intent
        ↓
ensure TRADE session logged on + lease owned
        ↓
OrderStatusRequest (35=H) by ClOrdID
        ↓
if still unknown or no order:
    OrderMassStatusRequest (35=AF, MassStatusReqType=7)
        ↓
consume ExecutionReport(s)
        ↓
RequestForPositions (35=AN)
        ↓
compare with destination_positions + fix_orders
        ↓
decide:
    venue has this ClOrdID     → adopt venue state (accepted/partial/filled/rejected/cancelled)
    venue has no such ClOrdID
      AND positions unchanged
      AND mass-status complete → mark not_on_venue; only then may a NEW cl_ord_id be allocated
    mismatch                   → BLOCKED_INCONSISTENT; human + risk; no auto-resend
```

cTrader RoE notes:

- `OrderStatusRequest` requires unique `ClOrdID` (tag 11). That is why ClOrdID uniqueness is a safety property, not a style rule.
- `OrderMassStatusRequest` currently supports `MassStatusReqType=7` (all orders). Empty book may return `BusinessMessageReject`.
- `RequestForPositions` without `PosMaintRptID` (721) returns all open positions.

Only after reconciliation may the system decide whether **another** order is required. A replacement order is a **new** row with a **new** `cl_ord_id`, linked to the same `execution_intent_id` only if policy allows (normally: original proven absent).

### 5.5 What “never simply retry because TCP broke” means in code

Illegal:

```text
catch (IOException) { SendNewOrderSingle(sameClOrdId); }
catch (IOException) { SendNewOrderSingle(newClOrdId); }
```

Legal:

```text
on transport fail after possible send:
    status = sent_ack_unknown
    increment metric fix_unknown_execution_states
    schedule reconcile; do not send
```

### 5.6 Interaction with reconnect

On TRADE reconnect / new Logon:

1. Do **not** flush a send queue.
2. Run §13 startup reconciliation (mass status + positions).
3. All in-flight or unknown orders are inputs to that reconcile, not send candidates.
4. Stale copy intents expire (`expires_at` / `max_signal_age` — architecture §63). Unknown **orders already sent** do not expire into a resend; they stay unknown until the venue answers.

---

## 6. Feature flags (architecture §41)

### 6.1 Defaults

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

This allows connecting, receiving prices, requesting orders/positions, and validating FIX connectivity **without** placing new real orders.

### 6.2 Flag matrix

| Flag | Off | On |
|---|---|---|
| `CTRADER_FIX_ENABLED` | Both sessions stay `DISABLED`. Worker may run health/lease code only. | Session objects may start. |
| `CTRADER_FIX_QUOTE_ENABLED` | No QUOTE socket. Destination quotes are unavailable. | QUOTE Logon + SecurityList + MD subscription allowed. |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | No TRADE socket. No status/position queries. | TRADE Logon + read/reconcile + cancel **only if other flags allow**. **Not** a license to send `NewOrderSingle`. |
| `REAL_COPY_EXECUTION_ENABLED` | **Hard block** on outbound `NewOrderSingle` (and any new exposure). Cancels / flatten follow kill-switch policy, not this flag. | Necessary but **not sufficient** to send. |

### 6.3 Conjunction required to send `NewOrderSingle`

All of the following must be true at the moment of send. Re-check immediately before the socket write.

```text
CTRADER_FIX_ENABLED = true
CTRADER_FIX_TRADE_SESSION_ENABLED = true
REAL_COPY_EXECUTION_ENABLED = true
TRADE session = READY_FOR_EXECUTION          -- Logon + reconcile passed
lease owned + fencing token current
risk engine healthy
STOP_NEW_EXECUTION = false                   -- architecture §40
global kill / venue pause = false
QUOTE usable if the order requires a fresh price
  (CTRADER_FIX_QUOTE_ENABLED
   AND quote_age <= configured_max_quote_age
   AND instrument mapped)
execution_intent persisted
cl_ord_id persisted
status = not_sent
intent not expired
```

If any check fails: do not send. Persist a risk/execution decision. Fail closed.

`REAL_COPY_EXECUTION_ENABLED=true` with a missing or stale quote is still a reject (`QUOTE_STALE` / venue unhealthy). Architecture §31, §37, §62.

### 6.4 Runtime vs config

- Config flags are the **floor**. A dashboard/kill-switch can turn execution **off** at runtime (`STOP_NEW_EXECUTION`) without a restart.
- Runtime must **not** turn `REAL_COPY_EXECUTION_ENABLED` on if the config value is `false` (defence in depth). Promoting to live is a config + audit event, not a button that bypasses config.
- Changing `REAL_COPY_EXECUTION_ENABLED` from false → true at runtime still requires `READY_FOR_EXECUTION`. Do not send a backlog that accumulated while the flag was off (architecture §63: no blind catch-up).

### 6.5 Kill switch is not a feature flag (architecture §40)

Do not conflate:

| Control | Effect |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | Master enable for **new** real copy `NewOrderSingle`. Default false. |
| `STOP_NEW_EXECUTION` | Operational halt of new copy orders; existing positions untouched. |
| `EMERGENCY_FLATTEN` | Attempts to close destination positions. Separate permission + confirmation. |

`EMERGENCY_FLATTEN` may send reducing orders even when `REAL_COPY_EXECUTION_ENABLED` is false, **only** if TRADE is logged on, lease is owned, and flatten is authorized. It still uses persist-before-send and unknown-state rules.

### 6.6 Recommended additional gates (same family; keep explicit)

```env
CTRADER_FIX_USE_SSL=true
CTRADER_FIX_ALLOW_PLAINTEXT=false
CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=false
MAX_QUOTE_AGE_MS=<measured, not guessed>
```

`CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=true` means: Logon, heartbeat, optional SecurityList, then stop. No MD subscription required, no TRADE application messages. Used to satisfy §3.6 without touching the book.

---

## 7. Supporting session behaviour required by §§25–34, 41–43

These are not optional “later phases” relative to a TRADE session that can send.

### 7.1 Instrument discovery (architecture §30)

On startup, after the chosen session is active:

```text
session LOGGED_ON
    ↓
SecurityListRequest (35=x, 559=0)
    ↓
SecurityList (35=y)
    ↓
find XAUUSD (tag 1007 name; tag 55 is the cServer instrument ID — integer)
    ↓
persist destination_symbols:
    cTrader instrument ID
    symbol name
    precision / digits (tag 1008)
```

Do **not** hardcode an instrument ID from another cTrader account or broker. Tag 55 on outbound orders is the **numeric FIX symbol ID**, not the text `XAUUSD`.

Discovery may run on QUOTE or TRADE; persist once; both sessions consume the same `destination_symbols` row.

### 7.2 Destination quote feed (architecture §31)

QUOTE session owns:

```text
latest quote
quote received timestamp (our clock)
venue timestamp if present
symbol ID
bid
ask
```

Uses: best destination bid/ask, quote freshness, shadow pricing, slippage reference, pre-trade price checks.

Risk **must** reject stale quotes:

```text
if quote_age > configured_max_quote_age:
    reject new copy order
```

Threshold is configurable **and measured**. Quote-session “connected” is not a substitute for `quote_age`.

### 7.3 Trade execution flow (architecture §32)

Correct production path:

```text
Source MT5 event
      ↓
Copy candidate?
      ↓
Create CopyIntent → persist
      ↓
RiskEngine evaluates → persist decision
      ↓
ApprovedExecutionIntent → persist
      ↓
FIX Execution Worker (this process, lease owner)
      ↓
NewOrderSingle
      ↓
ExecutionReport(s) → persist
      ↓
Update destination position
      ↓
Reconcile
```

**Never** send a FIX order directly from an MT5 event callback.

### 7.4 Startup reconciliation (architecture §42)

On TRADE Logon:

```text
Login successful
    ↓
block new executions
    ↓
OrderMassStatusRequest
    ↓
RequestForPositions
    ↓
consume Execution / Position reports
    ↓
compare with internal DB
    ↓
repair / update state
    ↓
only if reconciled:
READY_FOR_EXECUTION
```

Never assume the database is correct after restart.

### 7.5 Daily / periodic reconciliation (architecture §43)

Periodically compare:

```text
internal open orders
internal destination positions
vs
cServer order / position state
```

Raise alerts (and drop `READY_FOR_EXECUTION` if execution-impacting):

```text
unknown external position
missing internal position
quantity mismatch
side mismatch
orphan execution report
unexpected fill
```

Persist runs on `execution_reconciliation_runs` and issues on `execution_reconciliation_issues`.

### 7.6 Failure rules that bind the session layer (architecture §62)

| Condition | Session / execution behaviour |
|---|---|
| QUOTE FIX unavailable or quotes stale | Do not create new live copy trades that require fresh pricing. |
| TRADE FIX unavailable | Do not queue an unlimited backlog of stale entries. Mark new intents appropriately. Do not resend unknown orders blindly. |
| Database unavailable | Fail closed for new orders. Do not run critical real execution solely from memory. |
| Leadership lost | Loser does not send. Winner reconciles first. |

### 7.7 Durable tables the session layer needs (architecture §44)

```text
execution_venues
fix_sessions
fix_session_events
fix_session_leases          -- ownership (§4)

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

### 7.8 Logging and metrics

Every relevant event includes (architecture §57):

```text
correlation_id, broker_id, source_login, source_trade_id
copy_intent_id, risk_decision_id, execution_intent_id
cl_ord_id, cserver_order_id, destination_position_id
fix_session
fencing_token
```

Never log authentication tags containing passwords. Redact centrally.

FIX metrics (architecture §58), **per session** where applicable:

```text
fix_quote_connected
fix_trade_connected
fix_logon_failures
fix_reconnects
fix_inbound_messages_total
fix_outbound_messages_total
fix_rejects_total
fix_business_rejects_total
fix_execution_reports_total
fix_unknown_execution_states
```

Plus ownership: `fix_lease_held`, `fix_lease_lost`, `fix_fenced_sends_total`.

Dashboard (§52) shows **separate cards** for QUOTE and TRADE (host, SSL port, connected, logged on, sequences, last in/out, reconnects, heartbeat/test, errors). QUOTE card: XAU mapped, instrument ID, bid/ask, quote age, spread. TRADE card: execution enabled, open orders, open positions, last ER, last reconciliation. Never show the FIX password.

---

## 8. Test harness (architecture §61) — required before real NewOrderSingle

Do not use account `1369850` as the first integration test.

The adapter test mode must:

```text
parse recorded ExecutionReports
replay MarketDataIncrementalRefresh
simulate disconnects
simulate duplicate ExecutionReports
simulate partial fill
simulate rejection
simulate unknown-state disconnect
```

Unit tests that this spec considers acceptance-level:

1. QUOTE and TRADE sequence stores are distinct.
2. Header builder emits configured `TargetSubID` / `SenderSubID` without mutating `cServer` → `CSERVER`.
3. QUOTE SenderSubID is `QUOTE` when TargetSubID is `QUOTE`.
4. NewOrderSingle is refused on the QUOTE session.
5. NewOrderSingle is refused when `REAL_COPY_EXECUTION_ENABLED=false` even if TRADE is logged on.
6. Second instance cannot acquire the TRADE lease while the first holds it.
7. Fenced token cannot send.
8. Disconnect after send marks `sent_ack_unknown` and does not resend the same or a new ClOrdID.
9. Recovery: venue unknown ClOrdID + unchanged positions → `not_on_venue` → new ClOrdID allowed; venue has ClOrdID → adopt, no second send.
10. Startup: Logon without completed mass-status + positions stays off `READY_FOR_EXECUTION`.
11. Stale quote rejects a would-be send.
12. Password never appears in structured log output.

---

## 9. Acceptance (live TRADE send) — excerpt of architecture §70 that this spec owns

```text
[ ] TRADE FIX Logon is stable (and QUOTE Logon is stable)
[ ] Header mapping proven for both sessions (no silent case change)
[ ] Two session objects; independent sequence / heartbeat / metrics
[ ] Single-active TRADE ownership with fencing token
[ ] ExecutionReports persisted correctly
[ ] Position reports reconcile after restart
[ ] Unique ClOrdID rules proven
[ ] Duplicate report handling proven
[ ] Unknown-state recovery proven
[ ] Real execution is feature-flagged (default off)
[ ] Reconciliation blocks execution while inconsistent
[ ] Global STOP_NEW_EXECUTION works
[ ] Secrets absent from repo / logs / dashboard
```

Architecture §68 also requires unknown-state recovery and restart reconciliation before `REAL_COPY_EXECUTION_ENABLED=true` in production.

---

## 10. Suggested code placement (when a later task implements this)

Adapt; do not duplicate engines.

```text
src/Fix.CTrader/          session types, header/settings, message handlers, dictionary
src/Execution/            persist-before-send, state machine, unknown-state recovery
apps/fix-worker/          host, flags, lease loop, no business logic in Program.cs
tests/Fix/                replay + disconnect + flag + lease tests
```

`apps/fix-worker` remains a host. Domain rules stay out of `Worker.cs`.

---

## 11. Open risks (do not paper over)

1. **Header ambiguity is real.** Issued form vs RoE (`cServer`/`CSERVER`, SenderSubID vs TargetSubID) is not resolved until diagnostic Logon. Do not “pick one and ship.”
2. **RoE can change.** Re-read https://help.ctrader.com/fix/specification/ at implementation time; pin the RoE version in `fix_session_events`.
3. **Quote heartbeat absence** is expected while streaming. Liveness = quote age, not Heartbeat.
4. **Duplicate reports** if ownership fails. Treat lease bugs as P0, equal to a double-send.
5. **ResetSeqNumFlag=Y** on each Logon (current RoE) means the socket sequence is not a durable order log. PostgreSQL is.
6. Live account `1369850` is real money. Diagnostic Logon is allowed; diagnostic `NewOrderSingle` is not.

---

## 12. Implementation checklist (coding task, not this file)

1. Add QuickFIX/n + cTrader dictionary; two SessionSettings.
2. Wire configurable headers exactly as §3.4.
3. Implement diagnostic Logon-only mode; record §3.6 evidence for both sessions.
4. Implement `fix_session_leases` + fencing; refuse second TRADE owner.
5. QUOTE: SecurityList + XAU map + quotes + stale-quote metric.
6. TRADE: mass status + positions + startup reconcile; `READY_FOR_EXECUTION` gate.
7. Persist-before-send + unknown-state recovery; no retry-on-disconnect.
8. Feature flags default as §6.1; send path is a single guarded function.
9. Test harness §8 green on recorded data.
10. Only then consider `REAL_COPY_EXECUTION_ENABLED` in a non-prod proof environment.

---

*End of A25. Product source was not modified.*
