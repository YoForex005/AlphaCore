# A34 — cTrader FIX FAQs: multiple connections, duplicate reports, instrument IDs vs tag 55

**Primary source:** https://help.ctrader.com/fix/faqs/  
**Supporting official source (instrument IDs / tag 55 are not on the FAQ page):** https://help.ctrader.com/fix/specification/  
**Fetched:** 2026-08-18  
**Agent:** A34  
**Scope:** Official cTrader FIX documentation extract only. No product source was modified.

---

## How to use this document

- The live FAQ page is short: **four questions**. Only one of the requested topics is answered there.
- **Multiple connections / duplicate reports** is FAQ item 1. Quoted below from the page.
- **Instrument IDs vs tag 55** is **not** a FAQ entry. The official answer lives in the Rules of Engagement (RoE) at `/fix/specification/`, mainly Market Data, New Order Single, Execution Report, and Security List. That material is included here so this note covers the requested trio without inventing a FAQ.
- Do not treat human symbol names (`EURUSD`) as tag 55. Do not hardcode numeric IDs across brokers or environments.

---

## 1. What the FAQ page actually contains

Page title: **FAQs - cTrader FIX API**. Four headings, no mention of symbols, Security List, or tag 55.

| # | Heading | Official answer (paraphrase only where marked) |
|---|---------|------------------------------------------------|
| 1 | Why are FIX API reports received from the server duplicated? | **Verbatim:** FIX API reports will be duplicated if you have multiple connections to the API open simultaneously. The server will send a copy of the FIX response to each active connection. |
| 2 | Why is there no heartbeat response for the quote feed? | **Verbatim:** This is expected behaviour. When quotes are streaming, it negates the need for the heartbeat to be sent. |
| 3 | Is it possible to receive non-aggregated depth of market? | No. Tag 266 is not in the RoE. Spotware receives aggregated prices from brokers; non-aggregated book would require upstream change. Brokers are unlikely to disclose liquidity sources. |
| 4 | I sent a FIX message but I receive no response. Why? | Invalid FIX is silently ignored. Check with a FIX parser. Other listed causes: bad credentials (host, port, trading account number, password, SenderCompID, TargetCompID, SenderSubID); wrong checksum; DateTime not UTC; missing tags; tags in the wrong order. |

Support channels listed on the same page: Discord, archived Community forum, `support@ctrader.com`.

---

## 2. Multiple connections and duplicate reports (FAQ, primary)

### Official statement

From https://help.ctrader.com/fix/faqs/#why-are-fix-api-reports-received-from-the-server-duplicated :

> FIX API reports will be duplicated if you have multiple connections to the API open simultaneously. The server will send a copy of the FIX response to each active connection.

That is the entire official FAQ answer. There is no extra wording about Execution Report vs Position Report vs Market Data, no mention of sequence numbers, and no “dedupe by ClOrdID” recipe.

### What this implies (derived from the official sentence + RoE session model)

1. **Fan-out, not retry.** Duplicates are copies of the same server response sent independently to each **active** connection. This is not the same as a FIX resend (`35=2`) or a client-side retry of `NewOrderSingle`.
2. **Any application report can appear twice.** The FAQ says “FIX API reports” and “the FIX response”. In the RoE, application messages from the server include Execution Report (`35=8`), Position Report, Order Cancel Reject, Business Message Reject, Security List, Market Data Snapshot/Incremental. If two TRADE (or two QUOTE) sessions are up for the same party, each should expect its own copy.
3. **Intended two-session design is not the same as two TRADE sockets.** RoE standard header requires `TargetSubID` (tag 57) = `QUOTE` or `TRADE`. Those are two **session types**. One QUOTE + one TRADE is the documented split. Two simultaneous TRADE (or two QUOTE) sockets for the same account is what the FAQ is warning about.
4. **Do not treat a second copy as a second fill.** If two TRADE connections are open, two `35=8` messages with the same `ClOrdID` / `OrderID` / `ExecType=F` are expected by the FAQ model. Downstream must key idempotency on server IDs (`OrderID` tag 37, plus exec identity if present), not “I received another Execution Report so there was another trade”.
5. **Reconnect / leftover sessions count.** An abandoned client that never sent Logout, a second process, a dashboard plus an EA, or a test harness left running, all count as “multiple connections … open simultaneously”. The FAQ does not define idle timeout.
6. **QUOTE-side heartbeat gap is a separate FAQ.** No heartbeat on the quote feed while quotes stream is **expected**. Absence of `35=0` on QUOTE is not evidence that the session is dead, and is not a reason to open a second QUOTE connection.

### Related official no-response FAQ (same page)

If a message is not valid FIX, **the server will not respond**. Checklist from the FAQ:

- Credentials: host, port, trading account number, password, `SenderCompID`, `TargetCompID`, `SenderSubID`
- Checksum
- DateTime must be UTC
- Required tags present (see RoE)
- Tags in RoE order

That silent-drop behaviour is independent of duplication. A bad logon on a second connection does not “steal” reports from the first; a **successful** second connection **does** receive copies.

---

## 3. Instrument IDs vs tag 55 (not on the FAQ page)

**Finding:** https://help.ctrader.com/fix/faqs/ has **zero** text about instrument IDs, symbol names, Security List, tag 55, tag 48, or tag 1007.

The official mapping is in the RoE: https://help.ctrader.com/fix/specification/

### 3.1 Tag 55 is not a ticker string

On cTrader FIX, tag 55 (`Symbol`) is a **numeric instrument identifier provided by Spotware**, not the human name (`EURUSD`).

RoE wording, repeated on Market Data Request, Market Data Snapshot, Incremental Refresh, New Order Single, Execution Report, and Security List:

> Instrument identifiers are provided by Spotware.

RoE types:

| Message | Tag 55 required? | Declared FIX format | Comment in RoE |
|---------|------------------|---------------------|----------------|
| Market Data Request (`35=V`) | Yes | Long | Instrument identifiers are provided by Spotware. |
| Market Data Snapshot (`35=W`) | Yes | Long | Same (typo on page: “identificators”). |
| Market Data Incremental (`35=X`) | Yes | Long | Same. |
| New Order Single (`35=D`) | Yes | Long | Same. |
| Execution Report (`35=8`) | No | Long | Same. |
| Security List Request (`35=x`) | No | Integer | “An ID for resolving the symbol name.” |
| Security List (`35=y`) | No | Integer | Instrument identifiers are provided by Spotware. |

Do not send `55=EURUSD`. The published Market Data Request example uses `55=1`. The published New Order Single examples also use `55=1`.

Standard FIX 4.4 uses tag 55 as a string symbol and often uses tag 48 (`SecurityID`) + tag 22 (`SecurityIDSource`) for numeric IDs. **cTrader overloads tag 55 as the Spotware numeric instrument ID.** The FAQ and RoE do not document tag 48 / tag 22 for this API.

### 3.2 Human name is tag 1007 (`SymbolName`), not tag 55

Security List (`35=y`) repeating group:

| Tag | Field | Required | Format | Official comment |
|-----|-------|----------|--------|------------------|
| 146 | `NoRelatedSym` | No | Integer | Number of repeating symbols (instruments). |
| 55 | `Symbol` | No | Integer | Instrument identifiers are provided by Spotware. |
| 1007 | `SymbolName` | No | String | A symbol name. |
| 1008 | `SymbolDigits` | No | Integer | Symbol digits. Possible values from `0` to `5`. |

Security List Request (`35=x`):

| Tag | Field | Required | Official comment |
|-----|-------|----------|------------------|
| 320 | `SecurityReqID` | Yes | Unique ID of the Security Definition Request. |
| 559 | `SecurityListRequestType` | Yes | Supported only `0` = Symbol (tag=55). |
| 55 | `Symbol` | No | An ID for resolving the symbol name. |

So:

- **Discover** IDs with `35=x` (`559=0`) on the TRADE session, then parse `35=y`.
- **Order / quote** using the numeric `55` from that list.
- **Display / map to MT5** using `1007` (`EURUSD`, …), never by assuming `55=1` is always EURUSD on every broker.

### 3.3 Official example: ID ≠ name, and IDs are not a dense 1…N of majors

RoE Security List response (abbreviated from the published example):

```
55=1|1007=EURUSD|1008=5
55=2|1007=GBPUSD|1008=5
55=3|1007=EURJPY|1008=3
…
55=17|1007=EURCAD|1008=4
55=10001|1007=USDCFDSAX|1008=5
55=18|1007=AUDCAD|1008=4
55=10002|1007=CD3295|1008=5
…
55=39 → NZDCHF (single-instrument request example)
```

Observations that follow only from that official sample (do not generalise IDs beyond “must discover”):

- `55=1` happens to be `EURUSD` **in this example**, with 5 digits.
- IDs jump (`17` then `10001`). Broker-specific / synthetic names appear (`USDCFDSAX`, `CD3295`, `CS6407_01_EURUSD`).
- A targeted request `55=39` returned `1007=NZDCHF`. Tag 55 on the request resolves a **name**, it is not the name itself.
- `1008` is price decimal digits, not lot size and not a substitute for ID.

Therefore: **never hardcode tag 55.** Discover via Security List per environment/broker/account. That matches architecture rule 13 in `A28_phases_gates.md` (“Discover cTrader symbols/instrument IDs; do not guess”).

### 3.4 Security List request types

RoE: `SecurityListRequestType` (559) supports **only** `0` = Symbol (tag=55). There is no documented “all names as strings” request type. Omitting tag 55 on `35=x` is how the published full-list example is obtained (`146=143` instruments). Sending tag 55 filters to that ID.

`SecurityRequestResult` (560) on the response: `0` valid; `1` invalid/unsupported; `2` no instruments match; `3` not authorised; `4` temporarily unavailable; `5` request not supported.

---

## 4. Session facts from RoE that interact with the FAQ

These are not FAQ text. They are needed to apply FAQ #1 without opening extra connections “to fix” other symptoms.

| Topic | Official RoE fact |
|-------|-------------------|
| FIX version | 4.4 |
| TargetCompID (56) | Always `CSERVER` |
| SenderCompID (49) | `<Environment>.<BrokerUID>.<Trader Login>` e.g. `live.theBroker.12345` |
| Username (553) | Numeric trader login only; not the dotted CompID |
| TargetSubID (57) | `QUOTE` or `TRADE` |
| SenderSubID (50) | Must be `QUOTE` if `TargetSubID=QUOTE` |
| Sequence numbers | Reset on establishing a session; Logon `ResetSeqNumFlag` (141) = `Y` |
| Invalid Logon | Server replies Logout (`35=5`) with `Text` (58), not silence |
| Invalid application FIX | FAQ: no response |
| Quote heartbeat | FAQ: none while quotes stream (expected) |
| Tag 266 (aggregated book) | Not in RoE; non-aggregated depth is not available |

---

## 5. Practical acceptance notes (for later FIX work; not implemented here)

Use these as review checks. They are consequences of the official docs above, not new product requirements invented here.

1. **One TRADE TCP session and one QUOTE TCP session per trading identity**, unless a measured design says otherwise. A second TRADE/QUOTE process is a known source of duplicate reports per FAQ #1.
2. **Dedupe inbound application messages** by server identity (`OrderID` 37, `ClOrdID` 11, exec type 150, and Security List `SecurityResponseID` 322), because FAQ #1 says the server will copy the same response to every active connection.
3. **Never interpret a second `35=8` on a second socket as a second fill** without proving a distinct execution.
4. **Never send ticker strings in tag 55.** Populate tag 55 only from Security List (`35=y`) tag 55, keyed by `1007` / `1008`.
5. **Refresh the instrument map on connect** (and after broker/environment change). IDs in the RoE sample are not a universal table.
6. **Do not open a second QUOTE connection** because heartbeats stopped. FAQ #2 says that is expected while quotes stream.
7. **A silent send is not a duplicate-report problem.** FAQ #4: invalid FIX produces no response.

---

## 6. Source map

| Claim | Where it is official |
|-------|----------------------|
| Duplicate reports if multiple connections | FAQ Q1, `/fix/faqs/` |
| Server sends a copy to each active connection | FAQ Q1 (verbatim) |
| No quote heartbeat while quotes stream | FAQ Q2 |
| No non-aggregated depth; tag 266 absent | FAQ Q3 |
| Invalid FIX → no response; credential/checksum/UTC/tag-order checks | FAQ Q4 |
| Tag 55 = Spotware instrument identifier (numeric Long/Integer) | RoE application messages, `/fix/specification/` |
| Human symbol = tag 1007 `SymbolName` | RoE Security List |
| Digits = tag 1008 `SymbolDigits` (0–5) | RoE Security List |
| Discover via `35=x` / `35=y`; `559=0` only | RoE Security List Request |
| Example `55=1` ↔ `EURUSD` in the published list only | RoE Security List example |
| QUOTE vs TRADE session split | RoE standard header tags 57 / 50 |

---

## 7. Gaps (honest)

- The FAQ page does **not** define “connection” (TCP session vs CompID vs SenderSubID vs process).
- The FAQ page does **not** say whether QUOTE+TRADE together counts as “multiple connections”. RoE documents both as required session types; treat FAQ #1 as **duplicate sockets of the same role** unless Spotware support says otherwise.
- No official dedupe key, no official “max connections per account”, no official idle-disconnect time.
- RoE type for tag 55 is **Long** on trading/MD messages and **Integer** on Security List. Same identifier, inconsistent declared type.
- Instrument ID vs tag 55 is **not** a FAQ. Anything beyond RoE tables and examples is inference and is labelled as such above.
