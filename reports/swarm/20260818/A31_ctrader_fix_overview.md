# A31 — Official cTrader FIX API overview (QUOTE vs TRADE, TLS ports, messages)

| Field | Value |
|---|---|
| Agent | A31 (official FIX overview only) |
| Date | 2026-08-18 |
| Primary source | https://help.ctrader.com/fix/ |
| Product source edited | **No** |
| Method | Fetch official Help Centre pages listed in https://help.ctrader.com/sitemap.xml, plus official Spotware GitHub samples linked from Help / spotware.com. Nothing invented. |

**Honesty pin.** Official Rules of Engagement do **not** publish a standalone “TLS port table” as prose. Numeric SSL / plaintext ports appear on the official **Get credentials** screenshot and in the official Spotware C# sample. Host names are **not** a single global constant; they come from the cTrader FIX API credentials form.

---

## 1. Official source set (English)

Sitemap-confirmed Help Centre pages (2026-08-17 lastmod):

| Page | URL |
|---|---|
| Getting started | https://help.ctrader.com/fix/ |
| Benefits | https://help.ctrader.com/fix/benefits/ |
| Limitations | https://help.ctrader.com/fix/limitations/ |
| Get credentials | https://help.ctrader.com/fix/getting-credentials/ |
| Communication model | https://help.ctrader.com/fix/communication-model/ |
| Send and receive messages | https://help.ctrader.com/fix/sending-and-receiving-messages/ |
| Specification (Rules of Engagement) | https://help.ctrader.com/fix/specification/ |
| FAQs | https://help.ctrader.com/fix/faqs/ |

Nav on those pages also lists **Dictionary** and **SDKs**. Those paths 404 (`https://help.ctrader.com/fix/dictionary/`, `https://help.ctrader.com/fix/sdks/`) and are **absent** from the sitemap. Do not treat them as live official pages.

Official vendor pages linked from Help / marketing:

| Resource | URL |
|---|---|
| Spotware product page (points at Help + GitHub) | https://www.spotware.com/ctrader/dev-resources/fix-api-for-trading/ |
| Official C# sample (linked from Help) | https://github.com/spotware/FIX-API-Sample |
| Official Python package (Spotware) | https://github.com/spotware/cTraderFixPy |
| Official Python docs | https://spotware.github.io/cTraderFixPy/ |
| Credentials UI screenshot on Get credentials | https://help.ctrader.com/fix/img/getting-fix-api-0.png |

---

## 2. Protocol identity

Quoted from https://help.ctrader.com/fix/ :

> “cTrader supports FIX version 4.4.”

Same statement on https://help.ctrader.com/fix/specification/ :

> “cTrader supports FIX version 4.4.”

Official header `BeginString` (tag 8) required value: `FIX.4.4`  
(https://help.ctrader.com/fix/specification/ — Standard header).

All messages start with `8=FIX.x.y` and end with checksum tag 10  
(https://help.ctrader.com/fix/communication-model/ ).

---

## 3. Session model — QUOTE vs TRADE

### 3.1 Two connection types (credentials)

Quoted from https://help.ctrader.com/fix/getting-credentials/ :

> “There are 2 types of connection, price connection and trade connection, and each type has its own separate set of credentials. Trading operations requests cannot be sent through the price connection's credentials and vice versa.”

The same page’s official screenshot (`getting-fix-api-0.png`) labels the two blocks **Price Connection** and **Trade Connection**, with:

| UI block | `SenderSubID` shown | Role |
|---|---|---|
| Price Connection | `QUOTE` | market-data / price session |
| Trade Connection | `TRADE` | trading session |

### 3.2 Session qualifier in the FIX header

Quoted from https://help.ctrader.com/fix/specification/ — Standard header, tag 57 `TargetSubID`:

> “An additional session qualifier. Possible values are `QUOTE` and `TRADE`.”

Same possible values on https://help.ctrader.com/fix/communication-model/ (message example table, tag 57).

Quoted official `SenderSubID` (tag 50) rule on the specification page:

> “The assigned value used to identify a specific message originator. Must be set to `QUOTE` if `TargetSubID=QUOTE`.”

Do **not** treat tag 50 as the session qualifier on TRADE. Official TRADE Logon example uses `57=TRADE` and `50=any_string`  
(https://help.ctrader.com/fix/specification/ — Logon).

On a successful Logon **response**, CompIDs and SubIDs are swapped. Official success example:

- Client sent `49=live.theBroker.12345`, `56=CSERVER`, `57=TRADE`, `50=any_string`
- Server replied `49=CSERVER`, `56=live.theBroker.12345`, `50=TRADE`, `57=any_string`

(https://help.ctrader.com/fix/specification/ )

### 3.3 Two TCP ports / two sockets

Quoted from https://help.ctrader.com/fix/sending-and-receiving-messages/ :

> “In our case we create two clients, since price quotation messages and trade messages are handled by different ports on the server.”

Official sample constructs `_priceClient` on `_pricePort` and `_tradeClient` on `_tradePort`.

### 3.4 Session lifecycle

Quoted from https://help.ctrader.com/fix/communication-model/ :

> “FIX API uses session-based communication. A session is defined as communication between two parties: the initiator (client), the party that initiates communication, and the acceptor (server), the party that receives the connection request from the initiator.”

> “The server validates client requests using the Logon message.”

> “Each session maintains bi-directional messages between the client and the cTrader server. A session can include multiple physical connections and is maintained using sequence numbers.”

Typical session flow (same page):

1. Client starts the session with a Logon message.
2. Client exchanges application messages with the server.
3. Session ends with a Logout message.

Quoted from https://help.ctrader.com/fix/specification/ — Connectivity:

> “All sides of a FIX session should have sequence numbers reset on establishing a FIX session. See the Logon message.”

Quoted Logon behaviour (same page):

> “A Logon message is sent from the client side application to begin a cTrader FIX session, and a response is sent by cTrader to the client side application. Once the logon is complete, quote and trade flows can proceed for the lifecycle of the session.”

> “If an invalid Logon message is received by cTrader (with invalid fields), cTrader sends a Logout message in response.”

Quoted Logout behaviour (same page):

> “Before terminating the session, cTrader will cancel all prices that are still actively streaming out to the requesting party.”

### 3.5 CompID / login identity (session binding)

Quoted from specification, tag 49 `SenderCompID`:

> “An ID of the trading party in the following format: `<Environment>.<BrokerUID>.<Trader Login>`, where `Environment` is a determination of the server, like demo or live; `BrokerUID` is provided by cTrader and `Trader Login` is a numeric identifier of the trader account.”

Quoted `TargetCompID` (tag 56):

> “A message target. The valid value is `CSERVER`.”

Quoted Logon note:

> “The field `Username` (tag=553) must contain a numeric trader login value, whilst `SenderCompID` (tag=49) must contain an environment, `BrokerUID` and a trader login delimited by a dot (for example, `live.theBroker.12345`).”

**Observed official UI vs RoE casing (do not collapse):**

| Field | Rules of Engagement | Official credentials screenshot | Official Python sample config |
|---|---|---|---|
| TargetCompID | `CSERVER` | `cServer` | `cServer` |
| SenderCompID example | `live.theBroker.12345` | `ctrader.4791386` | empty placeholder |

Sources: https://help.ctrader.com/fix/specification/ ; https://help.ctrader.com/fix/img/getting-fix-api-0.png ; https://raw.githubusercontent.com/spotware/cTraderFixPy/main/samples/ConsoleSample/config.json

### 3.6 Quote-session heartbeat exception

Quoted from https://help.ctrader.com/fix/faqs/ :

> “Why is there no heartbeat response for the quote feed?”  
> “This is expected behaviour. When quotes are streaming, it negates the need for the heartbeat to be sent.”

Default `HeartBtInt` (tag 108) is 30 seconds; `0` means no heartbeat required  
(https://help.ctrader.com/fix/specification/ — Logon).

---

## 4. TLS / transport / ports

### 4.1 What RoE says about encryption (not a port number)

Quoted from https://help.ctrader.com/fix/specification/ — Logon, tag 98 `EncryptMethod`:

> “Defines a message encryption scheme. Currently, only transport-level security is supported. The valid value is `0` = `NONE_OTHER` (encryption is not used).”

Same wording in the official send/receive sample comments  
(https://help.ctrader.com/fix/sending-and-receiving-messages/ ).

Meaning on the official page: **FIX-level message encryption is off**. Security, if any, is **transport-level** (outside the FIX body).

### 4.2 How to obtain host + port

Quoted from https://help.ctrader.com/fix/getting-credentials/ :

> “You can find the FIX API credentials directly in the cTrader settings. Select the Cog icon in the bottom left and select FIX API from the settings menu.”

Quoted from https://help.ctrader.com/fix/faqs/ (no-response checklist):

> “Check your host, port, trading account number, password, SenderCompID, TargetCompID and SenderSubID.”

Quoted from https://help.ctrader.com/fix/specification/ — Connectivity:

> “A connection to cTrader’s FIX engine is available via the Internet, a VPN tunnel or a cross-connect to our data centre facilities in the UK. Contact us for further details.”

RoE does **not** name a hostname or TCP port in that connectivity section.

### 4.3 Official numeric ports (credentials screenshot + official sample)

Official Get-credentials screenshot (https://help.ctrader.com/fix/img/getting-fix-api-0.png ) prints the port labels as:

| Session (UI) | Official port line on screenshot | Session qualifier on same screenshot |
|---|---|---|
| **Price Connection** | `Port: 5211 (SSL), 5201 (Plain text)` | `SenderSubID: QUOTE` |
| **Trade Connection** | `Port: 5212 (SSL), 5202 (Plain text)` | `SenderSubID: TRADE` |

Official Spotware C# sample currently checked into GitHub uses the **SSL** pair and wraps both sockets in `SslStream` + `AuthenticateAsClient`:

```csharp
private int _pricePort = 5211;
private int _tradePort = 5212;
// ...
_priceStreamSSL = new SslStream(_priceClient.GetStream(), false,
            new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
_priceStreamSSL.AuthenticateAsClient(_host);
_tradeStreamSSL = new SslStream(_tradeClient.GetStream(), false,
            new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
_tradeStreamSSL.AuthenticateAsClient(_host);
```

Source: https://raw.githubusercontent.com/spotware/FIX-API-Sample/master/FIX%20API%20Sample.cs  
Linked from https://help.ctrader.com/fix/sending-and-receiving-messages/ and https://www.spotware.com/ctrader/dev-resources/fix-api-for-trading/

Official Python client (Spotware) treats TLS as a boolean on the same host/port:

```python
endpoint = clientFromString(self._runningReactor, f"ssl:{host}:{port}" if ssl else f"tcp:{host}:{port}")
```

Source: https://raw.githubusercontent.com/spotware/cTraderFixPy/main/ctrader_fix/client.py  
Sample config placeholder (`SSL: false`, `Port: 0`) : https://raw.githubusercontent.com/spotware/cTraderFixPy/main/samples/ConsoleSample/config.json  
Docs comment: “you can use two separate config files for QUOTE and TRADE” — https://spotware.github.io/cTraderFixPy/

### 4.4 Dated Help sample (do not treat as current TLS law)

https://help.ctrader.com/fix/sending-and-receiving-messages/ (note at bottom):

> “This article is up to date as of 03/02/2017 and developed with consideration for cTrader FIX engine, Rules of Engagement v2.9.1.”

That article’s constructor uses **plain** `TcpClient(_host, _pricePort)` / `TcpClient(_host, _tradePort)` with no `SslStream`. That is older sample code on the same Help site. Current official GitHub sample uses SSL on 5211/5212.

### 4.5 What is **not** officially published as a global constant

| Claim | Official status |
|---|---|
| Every broker always uses 5211/5212/5201/5202 | **Not stated** in RoE prose. Screenshot + sample show those numbers; FAQ still says check **your** host/port. |
| A single public hostname | **Not stated.** Screenshot hosts are examples; community posts (not RoE) show `hNN.p.ctrader.com`. Use the credentials form. |
| TLS version / cipher suite | **Not stated** on Help FIX pages. |
| Certificate pinning / CA list | **Not stated.** Official C# sample validates via `SslPolicyErrors.None`. |

---

## 5. Supported messages

Specification disclaimer (quoted, https://help.ctrader.com/fix/specification/ ):

> “Note that this is a minimum set of messages required to support the necessary workflows. It is subject to change over time as both business needs and the FIX standards evolve.”

Same disclaimer on https://help.ctrader.com/fix/communication-model/ .

### 5.1 Official RoE catalog (specification “cTrader FIX engine”)

**System / session messages** (https://help.ctrader.com/fix/specification/ ):

| Message | Direction (official) | MsgType(35) |
|---|---|---|
| Heartbeat | Client ↔ cTrader | `0` |
| Test Request | Client ↔ cTrader | `1` |
| Logon | Client → cTrader (body also documents bidirectional) | `A` |
| Logout | Client → cTrader (body also documents response by cTrader) | `5` |
| Resend Request | Client ↔ cTrader | `2` |
| Reject | Client ↔ cTrader | `3` |
| Sequence Reset | Client ↔ cTrader | `4` |

Logon heading on the same page is “Logon (bidirectional) (MsgType(35)=A)”. Logout text says it is sent by the client **and** as a response by cTrader.

**Application messages** (same official list):

| Message | Direction (official) | MsgType(35) |
|---|---|---|
| Market Data Request | Client → cTrader | `V` |
| Market Data Snapshot/Full Refresh | Client ← cTrader | `W` |
| Market Data Incremental Refresh | Client ← cTrader | `X` |
| New Order Single | Client → cTrader | `D` |
| Order Status Request | Client → cTrader | `H` |
| Order Mass Status Request | Client → cTrader | `AF` |
| Execution Report | Client ← cTrader | `8` |
| Business Message Reject | Client ← cTrader | `j` |
| Request for Positions | Client → cTrader | `AN` |
| Position Report | Client ← cTrader | `AP` |
| Order Cancel Request | Client → cTrader | `F` |
| Order Cancel Reject | Client ← cTrader | `9` |
| Order Cancel/Replace Request | Client → cTrader | `G` |
| Security List Request | Client → cTrader | `x` |
| Security List | Client ← cTrader | `y` |

### 5.2 Additional message specified on the same RoE page (not in the top bullet list)

The specification later documents **Market Data Request Reject**, `MsgType(35)=Y`, with official examples on the QUOTE session (`50=QUOTE` / `57=QUOTE`).  
https://help.ctrader.com/fix/specification/#market-data-request-reject-msgtype35y

### 5.3 Communication-model list is shorter (older subset)

https://help.ctrader.com/fix/communication-model/ application list is only:

- Market data request
- Market data incremental refresh
- New order single
- Execution report
- Business message reject

That page’s system list also has a copy error: two consecutive bullets both say **“Logon (client → cTrader)”**, the second described as “normal termination of session” (that is Logout on the specification page). Prefer the specification catalog.

### 5.4 Official examples: which session carries which application message

Taken only from official request/response examples on https://help.ctrader.com/fix/specification/ :

| Session qualifier in official examples | Messages shown |
|---|---|
| `QUOTE` (`57=QUOTE` and/or `50=QUOTE`) | Market Data Request `V`, Snapshot `W`, Incremental `X`, Market Data Request Reject `Y` |
| `TRADE` (`57=TRADE`) | New Order Single `D`, Order Status Request `H`, Order Mass Status `AF`, Execution Report `8`, Request for Positions `AN`, Position Report `AP`, Order Cancel `F`, Order Cancel Reject `9`, Order Cancel/Replace `G`, Security List Request `x`, Security List `y`, Business Message Reject `j` (failed cancel / amend) |

This matches the credentials rule: trading operations do not go on the price connection, and vice versa  
(https://help.ctrader.com/fix/getting-credentials/ ).

### 5.5 Market-data behaviour that the official pages pin down

From specification Market Data Request:

- `SubscriptionRequestType` (263): `1` = snapshot plus updates (subscribe); `2` = unsubscribe.
- `MarketDepth` (264): `0` = depth subscription; `1` = spot subscription.
- `MDUpdateType` (265): “Only the Incremental Refresh is supported.”
- `NoMDEntryTypes` (267): always `2` (bid and ask).
- `MDEntryType` (269): `0` = Bid, `1` = Offer.
- `Symbol` (55): “Instrument identifiers are provided by Spotware” (numeric FIX symbol ID, not the human name).

Spotware staff on the official community (https://community.ctrader.com/forum/fix-api/24104/ ), clarifying RoE examples:

> “If you subscribe to top of the book values (264=1) you will not get sizes. Sizes are only returned for depth subscriptions.”

> “Our example messages show clearly that tag 271 is not returned for spot subscription.”

Tag 266 (aggregated book) is **not** in RoE. FAQ: “Tag 266 is not included in our Rules of Engagement.” Non-aggregated DOM is not supported  
(https://help.ctrader.com/fix/faqs/ ).

### 5.6 Order types officially listed on New Order Single

Quoted from specification `OrdType` (40):

- `1` = Market — processed as Immediate or Cancel (`TimeInForce` 59).
- `2` = Limit — processed as Good Till Cancel unless `ExpireTime` (126) is set (then GTD).
- `3` = Stop — same GTC/GTD rule as limit.

Quoted: `TimeInForce` (59) is **deprecated** and “this value will be ignored”; TIF is detected from `OrdType` + `ExpireTime`.

`PosMaintRptID` (721) may attach an order to an existing position; “It can be specified only for hedged accounts.”

### 5.7 What FIX officially does **not** do

Quoted from https://help.ctrader.com/fix/limitations/ :

> “FIX API serves two primary purposes: 1. Receive live market data 2. Perform trading operations”

> “with FIX API they will not be able to access cTrader account information such as current balance, leverage, margin and more.”

> “it does not support requests for historical market data.”

The same page points at Open API (https://openapi.ctrader.com/) for account information and extra functionality.

---

## 6. Message framing (needed to interpret the session)

Quoted from https://help.ctrader.com/fix/communication-model/ :

- Every message has **Header + Body + Footer**.
- Fields are `{tag}={value}` pairs.
- `TargetCompID` valid value inside cTrader FIX API: `CSERVER`.
- If `Symbol(55)` is used, “you must specify the FIX symbol ID. That value can be different across brokers.”

FIX symbol ID is shown in the symbol information window  
(https://help.ctrader.com/fix/getting-credentials/ ).

Delimiter in wire messages is SOH (`\u0001`); Help examples print `|` for readability  
(https://help.ctrader.com/fix/sending-and-receiving-messages/ ).

---

## 7. Duplicate connections

Quoted from https://help.ctrader.com/fix/faqs/ :

> “FIX API reports will be duplicated if you have multiple connections to the API open simultaneously. The server will send a copy of the FIX response to each active connection.”

---

## 8. Implementation checklist (official facts only)

1. Open **two** sessions with **two** credential sets: price/`QUOTE` and trade/`TRADE`  
   (https://help.ctrader.com/fix/getting-credentials/ ).
2. Take **host + port + password + CompIDs** from cTrader Settings → FIX API. Do not hard-code a hostname from RoE (there is none).
3. If using the ports printed on the official credentials screenshot: **5211 SSL / 5201 plaintext** (price), **5212 SSL / 5202 plaintext** (trade). Official current C# sample uses **5211/5212 + TLS**.
4. Send `35=A` Logon with `98=0`, `108` heartbeat, `141=Y` recommended, `553` = numeric login, `554` = FIX password, `49` = dotted CompID, `56=CSERVER`, `57=QUOTE` or `TRADE`. If `57=QUOTE`, set `50=QUOTE`.
5. Reset sequence numbers on session establish (RoE connectivity + tag 141).
6. Route market-data messages to the price socket; route orders/positions/security-list to the trade socket.
7. Do not expect account balance/leverage/margin or historical bars on FIX (limitations page).

---

## 9. Sources (URLs quoted above)

- https://help.ctrader.com/fix/
- https://help.ctrader.com/fix/benefits/
- https://help.ctrader.com/fix/limitations/
- https://help.ctrader.com/fix/getting-credentials/
- https://help.ctrader.com/fix/img/getting-fix-api-0.png
- https://help.ctrader.com/fix/communication-model/
- https://help.ctrader.com/fix/sending-and-receiving-messages/
- https://help.ctrader.com/fix/specification/
- https://help.ctrader.com/fix/faqs/
- https://www.spotware.com/ctrader/dev-resources/fix-api-for-trading/
- https://github.com/spotware/FIX-API-Sample
- https://raw.githubusercontent.com/spotware/FIX-API-Sample/master/FIX%20API%20Sample.cs
- https://github.com/spotware/cTraderFixPy
- https://spotware.github.io/cTraderFixPy/
- https://raw.githubusercontent.com/spotware/cTraderFixPy/main/ctrader_fix/client.py
- https://raw.githubusercontent.com/spotware/cTraderFixPy/main/samples/ConsoleSample/config.json
- https://community.ctrader.com/forum/fix-api/24104/ (Spotware staff clarification of tag 271 / depth vs spot; not RoE)
- https://www.fixtrading.org/standards/fix-4-4/ (FIX 4.4 standard, linked from official intro)

Product source under `D:\Prop\src` was not modified.
