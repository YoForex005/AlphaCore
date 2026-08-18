# A32 — cTrader FIX specification extract

**Source (official):** https://help.ctrader.com/fix/specification/  
**Document title:** cTrader FIX engine (Rules of Engagement)  
**FIX version (quoted):** “cTrader supports FIX version 4.4.”  
**Fetched:** 2026-08-18  
**Scope of this note:** header Comp/Sub IDs, Logon, NewOrderSingle, ExecutionReport, SecurityList, Market Data. Official field usage is quoted from the page. Product source was not modified.

---

## Session-qualifier mapping warning (binding)

Official header comments treat **`TargetSubID` (57) as the session qualifier** and **`SenderSubID` (50) as an originator id with a hard exception on the quote session**. Do not collapse them into one field.

Quoted official usage:

| Tag | Field | Required | Official value / comment |
| --- | --- | --- | --- |
| 57 | `TargetSubID` | **Yes** | “An additional **session qualifier**. Possible values are `QUOTE` and `TRADE`.” |
| 50 | `SenderSubID` | **No** | “The assigned value used to identify a **specific message originator**. **Must be set to `QUOTE` if `TargetSubID=QUOTE`.**” |

### Mapping rules implied by the official table + official examples

1. **Two sessions, two qualifiers.** Application catalog is split: Market Data Request / Snapshot / Incremental belong on a `QUOTE` session; New Order Single, Execution Report, Security List Request/List belong on a `TRADE` session. Official examples follow that split (`57=QUOTE` + `50=QUOTE` on MD; `57=TRADE` on orders / security list).
2. **Do not map `SenderSubID` as the session qualifier on TRADE.** On TRADE logon the official request uses `57=TRADE|50=any_string`. `50` is free-form originator text, not `TRADE`.
3. **On QUOTE, `SenderSubID` is not free-form.** If `TargetSubID=QUOTE`, official comment: “Must be set to `QUOTE`”. Official MD request: `50=QUOTE` (tag 57 omitted on some older MD examples; later reject examples send both `50=Quote` and `57=QUOTE`).
4. **Server Comp/Sub IDs are swapped, not mirrored 1:1.** Official successful Logon response:
   - client sent `49=live.theBroker.12345`, `56=CSERVER`, `57=TRADE`, `50=any_string`
   - server replies `49=CSERVER`, `56=live.theBroker.12345`, `50=TRADE`, `57=any_string`  
   So inbound `SenderSubID` carries the **session qualifier** (`TRADE`/`QUOTE`) and inbound `TargetSubID` echoes the client originator string. Treating inbound `50`/`57` with the same client-side meaning will mis-classify the session.
5. **Header table values are uppercase `QUOTE` / `TRADE`.** Later official examples sometimes echo mixed case (`50=Quote`, `57=Trade`). Implementers should send the table values (`QUOTE`, `TRADE`) and not assume case-insensitive equality unless the engine is proven to accept both.
6. **`SenderCompID` is not a free CompID.** Official format is `<Environment>.<BrokerUID>.<Trader Login>` (example `live.theBroker.12345`). Logon `Username` (553) is the numeric login only; do not put the dotted triple in 553 or the bare login in 49.

---

## Standard header (client → cTrader)

Quoted: “Each administrative or application message is preceded by a standard header. Headers identify a message type, length, destination, sequence number, origination point and time.”  
Quoted: “All messages sent to cTrader should have a standard header with the following fields:”

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| 8 | `BeginString` | Yes | `FIX.4.4` | String | “Always unencrypted, must be the first field in a message.” |
| 9 | `BodyLength` | Yes | Any valid value | Integer | “Message body length. Always unencrypted, must be the second field in a message.” |
| 35 | `MsgType` | Yes | `A` (example in table) | String | “A message type. Always unencrypted, must be the third field in a message.” |
| **49** | **`SenderCompID`** | **Yes** | Any valid value | String | “An ID of the trading party in the following format: `<Environment>.<BrokerUID>.<Trader Login>`, where `Environment` is a determination of the server, like demo or live; `BrokerUID` is provided by cTrader and `Trader Login` is a numeric identifier of the trader account.” |
| **56** | **`TargetCompID`** | **Yes** | `CSERVER` | String | “A message target. The valid value is `CSERVER`.” |
| **57** | **`TargetSubID`** | **Yes** | `QUOTE` or `TRADE` | String | “An additional session qualifier. Possible values are `QUOTE` and `TRADE`.” |
| **50** | **`SenderSubID`** | **No** | Any valid value | String | “The assigned value used to identify a specific message originator. Must be set to `QUOTE` if `TargetSubID=QUOTE`.” |
| 34 | `MsgSeqNum` | Yes | `1` (example) | Integer | “A sequence number of the message.” |
| 52 | `SendingTime` | Yes | `20131129-15:40:08.155` (example) | UTCTimestamp | “Time of the message transmission always expressed in UTC (Universal Time Coordinated, also known as GMT).” |

Trailer (all messages): tag 10 `CheckSum`, required, “Always the last field in a message … trailing `<SOH>` as the end-of-message delimiter.”

---

## Logon (bidirectional) — `MsgType(35)=A`

Quoted: “A Logon message is sent from the client side application to begin a cTrader FIX session, and a response is sent by cTrader to the client side application. Once the logon is complete, quote and trade flows can proceed for the lifecycle of the session.”

Quoted: “If an invalid Logon message is received by cTrader (with invalid fields), cTrader sends a Logout message in response.”

Quoted connectivity rule: “All sides of a FIX session should have sequence numbers reset on establishing a FIX session. See the Logon message.”

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 98 | `EncryptMethod` | Yes | `0` | Integer | “Defines a message encryption scheme. Currently, only transport-level security is supported. The valid value is `0` = `NONE_OTHER` (encryption is not used).” |
| 108 | `HeartBtInt` | Yes | Any valid value | Integer | “A heartbeat interval in seconds. The value is set in the `config.properties` file (client side) as `SERVER.POLLING.INTERVAL`. The default interval value is 30 seconds. If `HeartBtInt` is set to `0`, no heartbeat message is required.” |
| 141 | `ResetSeqNumFlag` | No | `Y` | Boolean | “All sides of the FIX session should have the sequence numbers reset. The valid value is `Y` (reset).” |
| 553 | `Username` | No | Any valid value | String | “A numeric User ID. The user is linked to the `SenderCompID` value (the user’s organization, tag=49).” |
| 554 | `Password` | No | Any valid value | String | “A user password.” |
| | `Standard Trailer` | Yes | | | |

**Official note (quoted):** “The field `Username` (tag=553) must contain a numeric trader login value, whilst `SenderCompID` (tag=49) must contain an environment, `BrokerUID` and a trader login delimited by a dot (for example, `live.theBroker.12345`).”

**Official request example:**

```
8=FIX.4.4|9=126|35=A|49=live.theBroker.12345|56=CSERVER|34=1|52=20170117-08:03:04|57=TRADE|50=any_string|98=0|108=30|141=Y|553=12345|554=passw0rd!|10=131|
```

**Official response (success):**

```
8=FIX.4.4|9=106|35=A|34=1|49=CSERVER|50=TRADE|52=20170117-08:03:04.509|56=live.theBroker.12345|57=any_string|98=0|108=30|141=Y|10=066|
```

**Official response (failed):** Logout `35=5` with `58=InternalError: RET_INVALID_DATA` (not a Logon reject body).

Header mapping visible in the official pair: client `57=TRADE` / `50=any_string` → server `50=TRADE` / `57=any_string`; CompIDs swap `live.theBroker.12345` ↔ `CSERVER`.

---

## New Order Single — `MsgType(35)=D`

Direction (catalog): Client → cTrader. Official examples use `57=TRADE`.

Quoted: “A New Order Single message has the following format.”

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 11 | `ClOrdID` | Yes | Any valid value | String | “A unique identifier of the order allocated by the client.” |
| 55 | `Symbol` | Yes | Any valid value | Long | “Instrument identifiers are provided by Spotware.” |
| 54 | `Side` | Yes | `1` or `2` | Integer | “`1` = Buy    `2` = Sell” |
| 60 | `TransactTime` | Yes | Any valid value | Timestamp | “Request time generated by the client.” |
| 38 | `OrderQty` | Yes | Any valid value | Qty | “The number of shares ordered. … A maximum precision is 0.01.” |
| 40 | `OrdType` | Yes | `1`, `2` or `3` | Char | “`1` = Market, the order will be processed by the Immediate or Cancel (IOC) scheme (`TimeInForce`, tag=59).    `2` = Limit, the order will be processed by the Good Till Cancel (GTC) scheme (`TimeInForce`, tag=59).    `3` = Stop, the order will be processed by the Good Till Cancel (GTC) scheme (`TimeInForce`, tag=59).” |
| 44 | `Price` | No | Any valid value | Price | “The worst client price that the client will accept. Required only when `OrdType` (tag=40) = `2`, in which case the order will not fill unless this price can be met.” |
| 99 | `StopPx` | No | Any valid value | Price | “A price that triggers the stop order. Required only when `OrdType` (tag=40) = `3`, in which case the order will not fill unless this price can be met.” |
| 59 | `TimeInForce` | No | `1`, `3` or `6` | String | “**Deprecated, this value will be ignored.** `TimeInForce` will be detected automatically depending on `OrdType` (tag=40) and `ExpireTime` (tag=126):    `1` = Good Till Cancel (GTC), will be used only for limit and stop orders (`OrdType`, tag=40) only if `ExpireTime` (tag=126) is not defined.    `3` = Immediate or Cancel (IOC), will be used only for market orders (`OrdType`, tag=40).    `6` = Good Till Date (GTD), will be used only for limit and stop orders (`OrdType`, tag=40) only if `ExpireTime` (tag=126) is defined.” |
| 126 | `ExpireTime` | No | `20140215-07:24:55` | Timestamp | “Expire time in the \"YYYYMMDD-HH:MM:SS\" format. If assigned, the order will be processed by the GTD scheme (`TimeInForce`: GTD).” |
| 721 | `PosMaintRptID` | No | Any valid value | String | “A position ID where this order should be placed. If not set, a new position will be created and its ID will be returned in the Execution Report message. It can be specified only for hedged accounts.” |
| 494 | `Designation` | No | Any valid value | String | “A custom order label.” |
| | `Standard Trailer` | Yes | | | |

**Official market-order request (new position) — TRADE session:**

```
8=FIX.4.4|9=143|35=D|49=live.theBroker.12345|56=CSERVER|34=77|52=20170117-10:02:14|50=any_string|57=TRADE|11=876316397|55=1|54=1|60=20170117-10:02:14|40=1|38=10000|10=010|
```

Responses are Execution Reports (`35=8`) on the same TRADE session (`50=TRADE` from CSERVER).

OrdType → implied TIF (official, TIF tag ignored): Market → IOC (`59=3` echoed on ER); Limit/Stop → GTC (`59=1`) unless `ExpireTime` set → GTD (`59=6`).

---

## Execution Report — `MsgType(35)=8`

Direction (catalog): Client ← cTrader.

Quoted: “An Execution Report message for an accepted order has the following format.”

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 37 | `OrderID` | Yes | Any valid value | String | “A cTrader order ID.” |
| 11 | `ClOrdID` | No | Any valid value | String | “A unique identifier of the order allocated by the client.” |
| 911 | `TotNumReports` | No | Any valid value | Integer | “The total number of reports returned in response to the Order Mass Status Request message.” |
| 150 | `ExecType` | Yes | Any valid value | Char | “`0` = New    `4` = Canceled    `5` = Replace    `8` = Rejected    `C` = Expired    `F` = Trade    `I` = Order Status” |
| 39 | `OrdStatus` | Yes | Any valid value | Char | “`0` = New    `1` = Partially filled    `2` = Filled    `8` = Rejected    `4` = Cancelled (when the order is partially filled, `Canceled` is returned signifying (tag=151), `LeavesQty` is cancelled and will not be subsequently filled).    `C` = Expired” |
| 55 | `Symbol` | No | Any valid value | Long | “Instrument identifiers are provided by Spotware.” |
| 54 | `Side` | No | `1` or `2` | Integer | “`1` = Buy    `2` = Sell” |
| 60 | `TransactTime` | No | Any valid value | Timestamp | “Execution time of a transaction represented by the Execution Report message (in UTC).” |
| 6 | `AvgPx` | No | Any valid value | Integer | “A price at which the deal was filled. For an IOC or GTD order, this is the Volume Weighted Average Price (VWAP) of the filled order.” |
| 38 | `OrderQty` | No | Any valid value | Qty | “This represents the number of shares for equities or based on normal convention the number of contracts for options, futures, convertible bonds, etc.” |
| 151 | `LeavesQty` | No | Any valid value | Qty | “The number of orders still to be filled. Possible values are between `0` (fully filled) and `OrderQty` (partially filled).” |
| 14 | `CumQty` | No | Any valid value | Qty | “The total number of orders which have been filled.” |
| 32 | `LastQty` | No | Any valid value | Qty | “The bought/sold quantity of orders which have been filled on this (last) fill.” |
| 40 | `OrdType` | No | `1` or `2` | Char | “`1` = Market    `2` = Limit” |
| 44 | `Price` | No | Any valid value | Price | “If supplied in a New Order Single message, it is echoed back in this Execution Report message.” |
| 99 | `StopPx` | No | Any valid value | Price | “If supplied in a New Order Single message, it is echoed back in this Execution Report message.” |
| 59 | `TimeInForce` | No | `1`, `3` or `6` | String | “`1` = Good Till Cancel (GTC)    `3` = Immediate or Cancel (IOC)    `6` = Good Till Date (GTD)” |
| 126 | `ExpireTime` | No | `20140215-07:24:55` | Timestamp | “If supplied in a New Order Single message, it is echoed back in this Execution Report message.” |
| 58 | `Text` | No | Any valid value | String | “Where possible, a message will explain the Execution Report.” |
| 103 | `OrdRejReason` | No | `0` | Integer | “`0` = `OrdRejReason.BROKER_EXCHANGE_OPTION`” |
| 721 | `PosMaintRptID` | No | Any valid value | String | “A position ID.” |
| 494 | `Designation` | No | Any valid value | String | “A custom order label of the client.” |
| 584 | `MassStatusReqID` | No | Any valid value | String | “A unique ID of the mass status request as assigned by the client.” |
| 1000 | `AbsoluteTP` | No | Any valid value | Price | “An absolute price at which the take profit will be triggered.” |
| 1001 | `RelativeTP` | No | Any valid value | Price | “A distance in pips from the entry price at which the take profit will be triggered.” |
| 1002 | `AbsoluteSL` | No | Any valid value | Price | “An absolute price at which the stop loss will be triggered.” |
| 1003 | `RelativeSL` | No | Any valid value | Price | “A distance in pips from the entry price at which the stop loss will be triggered.” |
| 1004 | `TrailingSL` | No | `N` or `Y` | Boolean | “Indicates if the stop loss is trailing.    `N` = Stop loss is not trailing.    `Y` = Stop loss is trailing.” |
| 1005 | `TriggerMethodSL` | No | Any valid value | Integer | “`1` = Stop loss will be triggered by the trade side.    `2` = Stop loss will be triggered by the opposite side (ask for buy positions and by bid for sell positions).    `3` = Stop loss will be triggered after two consecutive ticks according to the trade side.    `4` = Stop loss will be triggered after two consecutive ticks according to the opposite side (the second ask tick for buy positions and the second bid tick for sell positions).” |
| 1006 | `GuaranteedSL` | No | `N` or `Y` | Boolean | “Indicates if the stop loss is guaranteed.    `N` = Stop loss is not guaranteed.    `Y` = Stop loss is guaranteed.” |
| | `Standard Trailer` | Yes | | | |

**Official New→Fill pair (market buy, new position):**

```
8=FIX.4.4|9=197|35=8|34=77|49=CSERVER|50=TRADE|52=20170117-10:02:14.720|56=live.theBroker.12345|57=any_string|11=876316397|14=0|37=101|38=10000|39=0|40=1|54=1|55=1|59=3|60=20170117-10:02:14.591|150=0|151=10000|721=101|10=149|
```

```
8=FIX.4.4|9=206|35=8|34=78|49=CSERVER|50=TRADE|52=20170117-10:02:15.045|56=live.theBroker.12345|57=any_string|6=1.0674|11=876316397|14=10000|32=10000|37=101|38=10000|39=2|40=1|54=1|55=1|59=3|60=20170117-10:02:14.963|150=F|151=0|721=101|10=077|
```

Note: ER table lists `OrdType` values as Market/Limit only; official stop-order ER still echoes `40=3` and `99=…`.

---

## Security List Request — `MsgType(35)=x` / Security List — `MsgType(35)=y`

Catalog: Security List Request Client → cTrader; Security List Client ← cTrader. Official examples use TRADE (`57=TRADE` outbound, `50=TRADE` inbound).

### Security List Request (`35=x`)

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 320 | `SecurityReqID` | Yes | Any valid value | String | “A unique ID of the Security Definition Request.” |
| 559 | `SecurityListRequestType` | Yes | `0` | Integer | “The type of a Security List Request being made. Supported only `0` = `Symbol` (tag=55).” |
| 55 | `Symbol` | No | Any valid value | Integer | “An ID for resolving the symbol name.” |
| | `Standard Trailer` | Yes | | | |

**Official request:**

```
8=FIX.4.4|9=107|35=x|34=3|49=live.theBroker.12345|50=Trade|52=20180427-12:24:27.106|56=CSERVER|57=TRADE|55=39|320=ILCea0JkdQEm|559=0|10=248|
```

### Security List (`35=y`)

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 320 | `SecurityReqID` | Yes | Any valid value | String | “A unique ID of the Security Definition Request.” |
| 322 | `SecurityResponseID` | Yes | Any valid value | String | “A unique ID of the Security List response.” |
| 560 | `SecurityRequestResult` | Yes | `0` | Integer | “Results returned to the Security Request message. The valid values are:    `0` = Valid request.    `1` = Invalid or unsupported request.    `2` = No instruments that match the selection criteria are found.    `3` = Not authorised to retrieve instrument data.    `4` = Instrument data temporarily unavailable.    `5` = Request for instrument data not supported.” |
| 146 | `NoRelatedSym` | No | Any valid value | Integer | “Specifies the number of repeating symbols (instruments).” |
| 55 | `Symbol` | No | Any valid value | Integer | “Instrument identifiers are provided by Spotware.” |
| 1007 | `SymbolName` | No | Any valid value | String | “A symbol name.” |
| 1008 | `SymbolDigits` | No | Any valid value | Integer | “Symbol digits. Possible values from `0` to `5`.” |
| | `Standard Trailer` | Yes | | | |

**Official single-symbol response:**

```
8=FIX.4.4|9=158|35=y|34=3|49=CSERVER|50=TRADE|52=20180427-12:24:27.107|56=live.theBroker.12345|57=Trade|320=ILCea0JkdQEm|322=responce:ILCea0JkdQEm|560=0|146=1|55=39|1007=NZDCHF|1008=4|10=088|
```

Repeating group is `146` then per-instrument `55` / `1007` / `1008`. Full-book example uses `146=143` and the same triplet per symbol (`55=1|1007=EURUSD|1008=5|…`). `Symbol` is a Spotware numeric id, not the ticker; ticker is custom tag `1007`.

---

## Market Data

Catalog:

- Market Data Request — Client → cTrader (`35=V`)
- Market Data Snapshot/Full Refresh — Client ← cTrader (`35=W`)
- Market Data Incremental Refresh — Client ← cTrader (`35=X`)
- Market Data Request Reject — (`35=Y`) documented under application messages (not in the short catalog list)

Official examples are on the **QUOTE** session (`50=QUOTE` outbound; `49=CSERVER|50=QUOTE` inbound). Logout text: “Before terminating the session, cTrader will cancel all prices that are still actively streaming out to the requesting party.”

### Market Data Request — `MsgType(35)=V`

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 262 | `MDReqID` | Yes | Any valid value | String | “A unique quote request ID. A new ID for a new subscription, the same ID as used before for a subscription removal.” |
| 263 | `SubscriptionRequestType` | Yes | `1` or `2` | Char | “`1` = Snapshot plus updates (subscribe).    `2` = Disable previous snapshot plus update request (unsubscribe).” |
| 264 | `MarketDepth` | Yes | `0` or `1` | Integer | “A full book will be provided.    `0` = Depth subscription    `1` = Spot subscription” |
| 265 | `MDUpdateType` | Yes | Any valid value | Integer | “Only the Incremental Refresh is supported.” |
| 267 | `NoMDEntryTypes` | Yes | `2` | Integer | “Always set to `2` (both bid and ask will be sent).” |
| 269 | `MDEntryType` | Yes | `0` or `1` | Char | “This repeating group contains a list of all types of the Market Data Entries the requester wants to receive.    `0` = Bid    `1` = Offer” |
| 146 | `NoRelatedSym` | Yes | Any valid value | Integer | “The number of symbols requested.” |
| 55 | `Symbol` | Yes | Any valid value | Long | “Instrument identifiers are provided by Spotware.” |
| | `Standard Trailer` | Yes | | | |

**Official spot subscribe (`264=1`) — QUOTE originator:**

```
8=FIX.4.4|9=131|35=V|49=live.theBroker.12345|56=CSERVER|34=3|52=20170117-10:26:54|50=QUOTE|262=876316403|263=1|264=1|265=1|146=1|55=1|267=2|269=0|269=1|10=094|
```

**Official depth subscribe (`264=0`):**

```
8=FIX.4.4|9=131|35=V|49=live.theBroker.12345|56=CSERVER|34=2|52=20170117-11:13:44|50=QUOTE|262=876316411|263=1|264=0|265=1|146=1|55=1|267=2|269=0|269=1|10=087|
```

### Market Data Snapshot/Full Refresh — `MsgType(35)=W`

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 262 | `MDReqID` | Yes | Any valid value | String | “The ID of the market data request previously sent.” |
| 55 | `Symbol` | Yes | Any valid value | Long | “Instrument identificators are provided by Spotware.” |
| 268 | `NoMDEntries` | Yes | Any valid value | Integer | “The number of entries following.” |
| 269 | `MDEntryType` | No | `0` or `1` | Char | “The valid values are:    `0` = Bid    `1` = Offer    Required only when `NoMDEntries` (tag=268) > `0`.” |
| 299 | `QuoteEntryID` | No | Any valid value | String | “A unique identification of the quote as a part of `QuoteSet`.” |
| 270 | `MDEntryPx` | No | `1.2345` | Price | “A price of the Market Data Entry. Required only when `NoMDEntries` (tag=268) > `0`.” |
| 271 | `MDEntrySize` | No | `500000` | Volume | “Volume of the Market Data Entry. Required only when `NoMDEntries` (tag=268) > `0`.” |
| 278 | `MDEntryID` | No | Any valid value | String | “A unique Market Data Entry identifier.” |
| | `Standard Trailer` | Yes | | | |

**Official spot snapshot (bid+offer, no size):**

```
8=FIX.4.4|9=134|35=W|34=2|49=CSERVER|50=QUOTE|52=20170117-10:26:54.630|56=live.theBroker.12345|57=any_string|55=1|268=2|269=0|270=1.06625|269=1|270=1.0663|10=118|
```

Depth snapshot example includes `271` size and `278` entry ids; header `50=QUOTE`.

### Market Data Incremental Refresh — `MsgType(35)=X`

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 262 | `MDReqID` | Yes | Any valid value | String | “The ID of the market data request previously sent.” |
| 268 | `NoMDEntries` | Yes | Any valid value | Integer | “The number of entries following. This repeating group contains a list of all types of Market Data Entries the requester wants to receive.” |
| 279 | `MDUpdateAction` | Yes | `0` or `2` | Char | “A type of the Market Data update action. The valid values are:    `0` = New    `2` = Delete” |
| 269 | `MDEntryType` | No | `0` or `1` | Char | “The valid values are:    `0` = Bid    `1` = Offer” |
| 278 | `MDEntryID` | Yes | Any valid value | String | “An ID of the Market Data Entry.” |
| 55 | `Symbol` | Yes | Any valid value | Long | “Instrument identifiers are provided by Spotware.” |
| 270 | `MDEntryPx` | No | `1.2345` | Price | “Required only when `MDUpdateAction` (tag=279) = `0`.” |
| 271 | `MDEntrySize` | No | `10000` | Double | “Required only when `MDUpdateAction` (tag=279) = `0`.” |
| | `Standard Trailer` | Yes | | | |

Only Incremental Refresh is supported as `MDUpdateType` (tag 265). Update actions documented: New (`0`) and Delete (`2`) — no Change (`1`) in the official table.

### Market Data Request Reject — `MsgType(35)=Y`

| Tag | Field name | Required | Value | FIX format | Official comments (quoted) |
| --- | --- | --- | --- | --- | --- |
| | `Standard Header` | Yes | | | |
| 262 | `MDReqID` | Yes | Any valid value | String | “Must refer to `MDReqID` (tag=262) of the request.” |
| 281 | `MDReqRejReason` | No | Any valid value | Integer | “`0` = Unknown symbol    `4` = Unsupported `SubscriptionRequestType` (tag=263)    `5` = Unsupported `MarketDepth` (tag=264)” |
| | `Standard Trailer` | Yes | | | |

Official reject texts: `INVALID_REQUEST: Expected numeric symbolId, but got CS8260` (`281=0`); `INVALID_REQUEST: MarketDepth should be either 0 or 1` (`281=5`). Confirms `Symbol` (55) on MD must be the Spotware numeric id, same as Security List `55`.

---

## Comp/Sub ID cheat-sheet (official examples)

| Direction | Session | 49 SenderCompID | 56 TargetCompID | 50 SenderSubID | 57 TargetSubID |
| --- | --- | --- | --- | --- | --- |
| Client → cTrader | TRADE | `live.theBroker.12345` | `CSERVER` | `any_string` (or omitted on some later msgs) | `TRADE` (required qualifier) |
| cTrader → Client | TRADE | `CSERVER` | `live.theBroker.12345` | `TRADE` | `any_string` (echo of client 50) |
| Client → cTrader | QUOTE | `live.theBroker.12345` | `CSERVER` | **`QUOTE` (mandatory when 57=QUOTE)** | `QUOTE` (required qualifier; omitted on some older MD samples) |
| cTrader → Client | QUOTE | `CSERVER` | `live.theBroker.12345` | `QUOTE` | `any_string` / `Quote` |

**Warning restated:** map client outbound session with **tag 57**; map server inbound session with **tag 50**. Do not send `50=TRADE` as if it were the TRADE qualifier, and do not send a free-form `50` on a QUOTE session.

---

## Source

- https://help.ctrader.com/fix/specification/
- FIX 4.4 reference linked from that page: https://www.fixtrading.org/standards/fix-4-4/
- Product source under `D:\Prop\src` was not modified.
