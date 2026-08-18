# A33 — cTrader FIX send/receive: sequence, resend, heartbeat, disconnect

**Assigned URL:** https://help.ctrader.com/fix/sending-and-receiving-messages/  
**Title:** Send and receive messages — cTrader FIX API  
**Fetched:** 2026-08-18  
**Agent:** A33  
**Product source:** not modified  
**Article self-date:** “up to date as of 03/02/2017”, written against **cTrader FIX engine Rules of Engagement v2.9.1**

This page is a **constructor + TCP sample tutorial**, not a session-protocol spec. It states explicitly that “additional functionality is required” to “establish and maintain proper communication with the server and proper handling of the responses”, and that those subjects were skipped. Sequence-gap recovery, resend orchestration, heartbeat timers, and disconnect/reconnect are therefore **not fully specified on the assigned page**.

Where the assigned page is silent, this report quotes the official siblings that the article itself points at (RoE / communication model / FAQs / sample constructors). Those complements are labelled **[RoE]**, **[CM]**, **[FAQ]**, **[SAMPLE]**. Nothing below is inferred from FIX 4.4 generally unless marked **[FIX-44 default, not cTrader-stated]**.

Complementary official pages used only to fill the four asked topics:

| Label | URL |
|---|---|
| RoE | https://help.ctrader.com/fix/specification/ |
| CM | https://help.ctrader.com/fix/communication-model/ |
| FAQ | https://help.ctrader.com/fix/faqs/ |
| SAMPLE | https://github.com/spotware/FIX-API-Sample (`MessageConstructor.cs`) |

Do **not** confuse this with cTrader **Open API** (Protobuf). Open API’s “heartbeat every 10 seconds or disconnect” is a different protocol and is **not** a FIX rule.

---

## 1. What the assigned page actually covers

Communication is four steps: construct → transmit → receive → parse. The article covers the first three; parsing is deferred.

A FIX message is `Tag=Value` pairs joined by SOH (`\u0001`; shown as `|` in examples). Protocol is **FIX.4.4**. Target is **cServer**.

Two **independent TCP sessions / ports**:

- `_priceClient` / `_priceStream` — quotations (`QUOTE`)
- `_tradeClient` / `_tradeStream` — trades (`TRADE`)

`TargetSubID` (tag 57) is `QUOTE` or `TRADE`. These are separate sequence spaces in practice (two sockets, two `Send*Message` paths). The sample initialises one `MessageConstructor` shared by both.

The sample is “by no means a bullet-proof application” and “by no means a full FIX engine”.

---

## 2. Sequence handling

### 2.1 On the assigned page

Header field **`MsgSeqNum` (tag 34)**:

> “this is the sequence number of the message. It needs to be **increased for each message sent in the same session**.”

Logon body may include **`ResetSeqNumFlag` (tag 141)**:

> “All sides of FIX session should have sequence numbers reset. Valid value is `"Y"` = Yes(reset).”

Sample Logon signature: `LogonMessage(qualifier, messageSequenceNumber, heartBeatSeconds, resetSeqNum)`. If `resetSeqNum` is true, the body appends `141=Y|`.

Example Logon request on the page:

```
8=FIX.4.4|9=126|35=A|49=theBroker.12345|56=CSERVER|34=1|52=20170117- 08:03:04|57=TRADE|50=any_string|98=0|108=30|141=Y|553=12345|554=passw0rd!|10=131|
```

Example Logon reply:

```
8=FIX.4.4|9=106|35=A|34=1|49=CSERVER|50=TRADE|52=20170117- 08:03:04.509|56=theBroker.12345|57=any_string|98=0|108=30|141=Y|10=066|
```

Both sides start at **`34=1`** when `141=Y`.

`SendMessage()` increments `_messageSequenceNumber++` **after** write + (optional) read, **unconditionally**, even if no reply arrived:

```
stream.Write(...)
while (!stream.DataAvailable && i < 100) { Thread.Sleep(100); i++; }
if (stream.DataAvailable) stream.Read(buffer, 0, 1024);
_messageSequenceNumber++;
```

Implications of the sample (not RoE rules, but what the official sample does):

- Sequence is **outbound-only** and **single counter**. There is no inbound expected-seq tracker.
- Increment happens even on **timeout / empty read**. That desynchronises the client if the write never left, or if the server did not accept the message.
- There is **no persist** of last sent / last received seq across process restart.
- There is **no PossDupFlag (43)**, **no PossResend (97)**, **no inbound gap detect**.
- QUOTE and TRADE share one constructor but two streams; the snippet shows **one** `_messageSequenceNumber`. A correct engine must keep **independent** seq state per session/port.

System constructors listed for sequence control: `ResendMessage()`, `RejectMessage()`, `SequenceResetMessage()`. No call-site logic is shown.

### 2.2 Complementary official rules **[RoE] [CM]**

**[CM]** Session definition:

- Session = initiator (client) ↔ acceptor (cServer).
- “A session can include **multiple physical connections** and is maintained using sequence numbers.”
- “Every new message within a single session receives a unique sequence number **starting from 1**.”
- “Both parties rely on sequence numbers to maintain orderly communication.”
- “Missing messages are re-transmitted with a **bilateral agreement** between both parties.”

Typical flow **[CM]**: (1) client Logon → (2) application exchange → (3) Logout.

**[RoE] Connectivity — Sequence number reset:**

> “All sides of a FIX session should have sequence numbers reset on establishing a FIX session. See the Logon message.”

**[RoE] Logon `141 ResetSeqNumFlag`:** optional, value `Y`. Same wording as the assigned page.

**[RoE] Header `34 MsgSeqNum`:** required, example value `1`.

**[RoE] Sequence Reset (`35=4`):**

> “An inbound/outbound message should **not** be used at an application level. A Sequence Reset message can **only increase** a sequence number.”

| Tag | Name | Req | Notes |
|---|---|---|---|
| 123 | `GapFillFlag` | No | `Yes`/`No`. Indicates the Sequence Reset is replacing admin/app messages that will **not** be resent. |
| 36 | `NewSeqNo` | Yes | New sequence number (example `1` in the table — that is a field example, not a rule that reset always goes to 1). |

**[RoE] Reject (`35=3`):** session-level rule violation.

> “Refused messages must be **recorded** and an **increment must be applied to the incoming sequence number**.”

So a session Reject **consumes** the inbound seq (do not stall waiting to reprocess the bad message). Record it. Increment inbound expected seq.

Reject fields relevant to sequence: `45 RefSeqNum` (required), plus optional `58 Text`, `371 RefTagID`, `372 RefMsgType`, `373 SessionRejectReason` (0–17 coded causes including CompID, SendingTime accuracy, invalid MsgType, tag order).

**[RoE] Failed Logon** does **not** stay in session: cTrader replies with **Logout** (`35=5`) and `58=InternalError: RET_INVALID_DATA` (example). That is not a Sequence Reset.

### 2.3 Sample constructor gaps **[SAMPLE]**

`SequenceResetMessage(qualifier, messageSequenceNumber, rejectSequenceNumber)` only emits `36=<newSeq>|`. It does **not** emit `123 GapFillFlag`. Parameter is misnamed `rejectSequenceNumber`.

`RejectMessage` appends `45=` to a body then calls `ConstructHeader(..., string.Empty)`, so **BodyLength excludes the body**. Do not copy this.

### 2.4 What is **not** stated

None of the official FIX pages fetched here specify:

- exact inbound gap policy (always Resend vs always Sequence Reset / GapFill)
- whether cTrader stores a retransmission cache, and for how long / how many messages
- PossDup handling
- what happens if client Logons **without** `141=Y` after a crash (RoE says sides “should” reset on establish; it does not document resume-from-last-seq)
- sequence persistence across TCP reconnects inside one logical session (**[CM]** allows multiple physical connections per session, but gives no resume algorithm)

Treat **reset-on-Logon (`141=Y`)** as the cTrader-documented establish path.

---

## 3. Resend

### 3.1 On the assigned page

Listed only as a system-message constructor:

- Resend request: `MessageConstructor.ResendMessage()`
- Sequence reset: `MessageConstructor.SequenceResetMessage()`

No trigger conditions, no range rules, no “who initiates”, no interaction with application messages. The intro says this extra handling was skipped.

### 3.2 Complementary official rules **[RoE] [CM] [SAMPLE]**

**[CM]** Resend request: “request to **retransmit certain application messages**” (client ↔ cTrader).

**[CM]** Sequence reset: “in case of communication problems missing messages are **recovered** or the sequence is **reset to ignore** the missing messages.”

**[RoE] Resend Request (`35=2`):** inbound/outbound; used “typically when a **gap is detected** in the sequence numbering.”

| Tag | Name | Req | Meaning |
|---|---|---|---|
| 7 | `BeginSeqNo` | **Yes** | First seq in the range to resent |
| 16 | `EndSeqNo` | **Yes** | Last seq in the range to resent |

**[SAMPLE] `ResendMessage(qualifier, messageSequenceNumber, endSequenceNo)`** only appends **`16=<end>`**. It **omits required `7 BeginSeqNo`**. The XML-doc comment also mislabels `messageSequenceNumber` as “last record in range to be resent” — that argument is the **header** `34`, not the resend range start. **Do not ship this constructor as-is.**

Recovery pairing implied by RoE + CM (not a step-by-step on the assigned page):

1. Detect inbound gap (`received 34` > `expected 34`).
2. Send Resend Request `35=2` with `7=expected` and `16=received-1` (or a closed range).
3. Counterparty either **retransmits** the application messages, or sends **Sequence Reset** `35=4` with `36=NewSeqNo` and optionally `123=Y` (gap fill — skip the hole).
4. `35=4` may **only increase** seq. Never use Sequence Reset as an application-level command.

There is **no documented guarantee** that cTrader will replay Execution Reports / MD increments from a deep history. Plan a **post-reconnect application reconcile** (Order Mass Status / Request for Positions / MD resubscribe) rather than relying on resend as a full drop-recovery.

---

## 4. Heartbeat

### 4.1 On the assigned page

Logon body **`HeartBtInt` (tag 108)**:

> “Heartbeat interval in seconds. Value is set in the `config.properties` file (client side) as `SERVER.POLLING.INTERVAL`. **30 seconds is default**. If `HeartBtInt` is set to **0, no heartbeat message is required**.”

Sample Logon uses `108=30` in the worked example. The C# API takes `heartBeatSeconds` as a parameter (`108=` + value).

System constructors:

- Heartbeat: `MessageConstructor.HeartbeatMessage()`
- Test request: `MessageConstructor.TestRequestMessage()`

No timer, no “send heartbeat if idle for N seconds”, no Test Request timeout, no “drop the socket if heartbeat missed”. The sample `SendMessage` only waits **≤ 10 s** (`100 × 100 ms`) for **any** bytes after a write — that is a **request/response poll**, not a heartbeat engine.

### 4.2 Complementary official rules **[RoE] [CM] [FAQ] [SAMPLE]**

**[CM]** Heartbeat (client ↔ cTrader): “used to check communication link between two parties.”  
**[CM]** Test request (client ↔ cTrader): “used to test the health of the communication link.”

**[RoE] Heartbeat (`35=0`):**

> “Heartbeat messages are sent by **both** cTrader and the client application to confirm a live connection.”

> “The provider’s client application transmits a **recurring heartbeat at the interval** defined by `HeartBtInt` (tag=108) in a Logon message, **or as a response to a Test Request**.”

| Tag | Name | Req | Notes |
|---|---|---|---|
| 112 | `TestReqID` | No | **Required if** the heartbeat is a reply to a Test Request |

**[RoE] Test Request (`35=1`):**

> “It forces a heartbeat from the receiver. A response is sent as a Heartbeat containing `TestReqID`.”

| Tag | Name | Req | Notes |
|---|---|---|---|
| 112 | `TestReqID` | **Yes** | “Heartbeat message ID. `TestReqID` should be **incremental**.” |

**[RoE] Logon `108 HeartBtInt`:** required integer. Same 30 s default / `0` = no heartbeat text as the assigned page.

**[FAQ] “Why is there no heartbeat response for the quote feed?”**

> “This is **expected behaviour**. When quotes are streaming, it **negates the need** for the heartbeat to be sent.”

So on **QUOTE**, while Market Data is flowing, cTrader may **omit** Heartbeat `35=0`. Silence on QUOTE is **not** proof of death if increments (`35=X` / snapshots `35=W`) are arriving. On **TRADE**, there is no such FAQ exemption — idle TRADE still needs the negotiated heartbeat (unless `108=0`).

**[SAMPLE] `HeartbeatMessage`** builds a header-only `35=0` (no `112`). Fine for timer-driven heartbeats; **insufficient** as a Test Request reply (must echo `112`).

**[SAMPLE] `TestRequestMessage`** emits `112=<testRequestID>|` as required.

### 4.3 Behaviour to implement (grounded)

| Case | Required behaviour |
|---|---|
| Logon `108=N` with `N>0` | Client sends `35=0` at least every N seconds of **idle outbound** (and should expect inbound traffic — heartbeat or app — on a similar cadence, except QUOTE-while-streaming). |
| Logon `108=0` | No heartbeat required (cTrader-stated). Link liveness is then only TCP + application traffic. Risky on TRADE. |
| Receive `35=1` | Immediately reply `35=0` with the **same** `112`. |
| Send `35=1` | Expect `35=0` with matching `112`. Incremental `112`. |
| QUOTE + active MD | Missing `35=0` is expected **[FAQ]**. Watch last **any** inbound message time, not last heartbeat. |
| Default if unspecified | 30 seconds. |

**Not stated** on any fetched cTrader FIX page: how many missed intervals before the server drops the socket; whether Test Request is sent after 1× or 1.2× interval; whether `108` is echoed as a binding mutual interval (the success Logon example **does** echo `108=30`).

Do **not** apply Open API’s 10-second rule here.

---

## 5. Disconnect / logout / “no response”

### 5.1 On the assigned page

Disconnect is almost absent.

- Logout exists only as `MessageConstructor.LogoutMessage()`.
- Transport is raw `TcpClient` + `NetworkStream`. No TLS discussion on this page.
- `SendMessage` does not close the socket. No FIN/RST handling. No reconnect. No Logout-before-close.
- Read path: wait up to **10 seconds** for `DataAvailable`; if still nothing, return a **zeroed 1024-byte buffer decoded as ASCII** (looks like empty/NUL payload) and **still increment seq**.
- “You cannot show a raw FIX message to the user” — parse is out of scope.

The article warns that proper session maintenance was **intentionally omitted**.

### 5.2 Complementary official rules **[RoE] [CM] [FAQ]**

**Normal end [CM] [RoE]:**

1. Client sends Logout `35=5`.
2. cTrader responds Logout `35=5`.
3. Session ends.

**[RoE] Logout (`35=5`):**

> “A Logout message is sent from the client application to request a session end with cTrader and as a response by cTrader.”

> “A session logout occurs in response to a Market Participant sending a Logout message to cTrader. **Before terminating the session, cTrader will cancel all prices that are still actively streaming out to the requesting party.**”

> “If an **invalid Logon** is received (invalid fields), cTrader sends a Logout with error details in `Text` (tag=58).”

Logout `58 Text` is **optional**, “used only for cTrader-to-client messages as an invalid Logon message response.”

Examples **[RoE]:**

Request: `35=5` … `34=161` … (no body text)  
Response: `35=5` … `34=160` …

Failed Logon response: `35=5` … `58=InternalError: RET_INVALID_DATA`.

**[FAQ] Invalid / unparseable FIX:**

> “If your message is not a valid FIX message, the **server will not respond** to it.”

Other listed no-response causes: bad credentials (host, port, account, password, SenderCompID, TargetCompID, SenderSubID), **wrong checksum**, DateTime **not UTC**, missing tags, **wrong tag order**.

This is operationally a **silent drop**, not a Reject. Combined with the sample’s 10 s wait + seq++, this is how a client quietly desynchronises.

**[FAQ] Duplicate reports:** multiple simultaneous connections → server sends a copy of each FIX response to **each** active connection. Not a disconnect rule, but it means “open a second TRADE socket” is not a failover pattern; it **fans out** reports.

**[CM]** A session “can include multiple physical connections” and is “maintained using sequence numbers.” That is **not** documented as “reconnect and continue seq without Logon.” After a TCP drop, the documented establish path is still **Logon** (with `141=Y` per RoE “reset on establishing”).

### 5.3 What disconnect does **not** document

Fetched cTrader FIX pages do **not** state:

- idle TCP timeout
- heartbeat-miss disconnect threshold
- whether in-flight GTC/limit orders survive a TRADE socket drop (Logout cancels **streaming prices**, not working orders)
- whether unsolicited Execution Reports continue after QUOTE drop
- reconnect backoff, session reuse, or SSL drop behaviour (connectivity page mentions Internet / VPN / cross-connect; TLS is not specified on the assigned page)

After any drop: treat QUOTE MD subscriptions as **dead** (Logout explicitly cancels streaming prices; a RST is at least as destructive). Resubscribe Market Data. On TRADE, **reconcile** orders/positions; do not assume resend will replay the gap.

---

## 6. Transport notes from the assigned page (affect all four topics)

### 6.1 Message framing

Wire delimiter is **SOH (`\u0001`)**, not `|`. Sample builds with `|` then `Replace("|", "\u0001")`.

Header order in the sample:

1. `8=FIX.4.4`
2. `9=BodyLength` (length of everything after `9=…|` through end of body, **excluding** trailer `10`)
3. `35` MsgType
4. `49` SenderCompID
5. `56` TargetCompID (`CSERVER`)
6. `57` TargetSubID (`QUOTE` / `TRADE`)
7. `50` SenderSubID
8. `34` MsgSeqNum
9. `52` SendingTime UTC `yyyyMMdd-HH:mm:ss` (**no millis** in the sample header; server examples often have `.sss`)

Trailer: `10=` + checksum **mod 256**, three digits, last field.

**[RoE]** `SenderCompID` format is `<Environment>.<BrokerUID>.<Trader Login>` (e.g. `live.theBroker.12345`). The assigned-page example uses `theBroker.12345` without environment — treat RoE as authoritative.

**[RoE]** `Username` (553) = numeric trader login; `50 SenderSubID` “Must be set to `QUOTE` if `TargetSubID=QUOTE`.”

### 6.2 Send/receive sample behaviour

- ASCII encode, `NetworkStream.Write` the whole message.
- Block up to 10 s for `DataAvailable`.
- Single `Read` of **1024 bytes** — multi-message bursts and large MD Incremental Refresh **will fragment or concatenate**. Not a real FIX framer (need read-until-`10=xxx<SOH>`).
- One write → at most one read. Unsolicited Execution Reports / MD increments have **no reader loop**.
- Seq++ after the attempt.

### 6.3 Dual session

Price and trade are **different ports**. Heartbeat, sequence, resend, and logout are **per session**. Logging out TRADE must not be assumed to tear down QUOTE (and vice versa), except that Logout on a session cancels **that** session’s streaming prices.

---

## 7. System message map (assigned page + RoE)

| MsgType | Name | Direction | Role for the four topics |
|---|---|---|---|
| `A` | Logon | Client → cTrader, reply ← | Starts session. Sets `108` heartbeat and optional `141=Y` seq reset. Invalid → Logout, not Reject. |
| `0` | Heartbeat | ↔ | Liveness at `108` interval, or echo of Test Request (`112`). |
| `1` | Test Request | ↔ | Force a Heartbeat with incremental `112`. |
| `2` | Resend Request | ↔ | Gap fill request; `7`/`16` range. |
| `3` | Reject | ↔ | Session-level failure; **record + increment inbound seq**. |
| `4` | Sequence Reset | ↔ | Skip/advance seq only; not an app message; **only increase**. Optional `123` gap fill. |
| `5` | Logout | Client →, reply ← | Normal disconnect. Cancels streaming prices. Also used as failed-Logon response (`58`). |

---

## 8. Implementation implications (Prop / FIX.4.4 client)

These are consequences of the docs above, not product edits (none made).

1. **Two FIX engines**, not one: independent TCP, `34`, `108` timers, resend state, and Logout for QUOTE vs TRADE.
2. **Logon with `141=Y`** is the documented establish/reset. Do not invent seq-resume unless a later RoE says so.
3. **Do not copy** `ResendMessage` (missing `7`), `RejectMessage` (BodyLength bug), or `SendMessage` (seq++ on timeout, 1024-byte read, no unsolicited pump).
4. Heartbeat: implement a real idle timer from `108`; reply to `35=1`; **do not** declare QUOTE dead solely because `35=0` is absent while `35=W`/`35=X` flow **[FAQ]**.
5. `108=0` is legal and disables required heartbeats — avoid on TRADE unless a broker requires it.
6. Logout is the only documented graceful disconnect; expect prices on that session to be **cancelled**. After any drop, **resubscribe MD** and **reconcile** orders/positions; resend is not a full store-and-forward log.
7. Invalid bytes / bad checksum / bad UTC / wrong tag order → **silence**, not Reject **[FAQ]**. Detect via timer, not via a guaranteed `35=3`.
8. Second parallel connection duplicates reports **[FAQ]**; it is not HA.
9. Sample is 2017 / RoE v2.9.1. Current RoE (fetched 2026-08-18) is the binding field list; the tutorial can lag (SenderCompID environment prefix, extra app messages such as Order Mass Status / Security List).

---

## 9. Source fidelity / gaps

| Asked topic | Assigned page density | Completeness |
|---|---|---|
| Sequence | Header `34` increment; Logon `141=Y`; seq++ in `SendMessage` | Partial. No inbound tracker, no persist, no gap algorithm. |
| Resend | Constructor names only | Thin. Range tags and “only increase” live in RoE. Sample Resend is incomplete. |
| Heartbeat | `108` default 30 / 0 disables; Heartbeat + TestRequest constructors | Partial. Recurring send + TestReqID echo live in RoE. QUOTE exemption in FAQ. No drop threshold. |
| Disconnect | Logout constructor; TCP client; 10 s read wait | Thin. Logout cancels prices + invalid Logon → Logout live in RoE. Silent-invalid in FAQ. No idle timeout number. |

**Honesty:** the assigned URL alone is insufficient to implement a correct session. A client built only from that page will increment seq blindly, never heartbeat on a timer, never resend on a gap, and never Logout cleanly.
