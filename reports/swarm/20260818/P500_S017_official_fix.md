# P500_S017 — Official cTrader FIX (Help Centre + sibling pages)

| Field | Value |
|---|---|
| Slot | **P500_S017** |
| Date | 2026-08-18 |
| Angle | Official Spotware/cTrader FIX facts: FIX 4.4, QUOTE vs TRADE, TLS ports, TargetCompID, NewOrderSingle on TRADE. This repo has **no** NewOrderSingle sender. Trade-copier use case is listed, then downgraded. |
| Product source edited | **No** |
| Secrets printed | **No** (no password, no live FIX password value, official sample password not copied) |
| Method | `web_search` + `web_fetch`/`open_page` of https://help.ctrader.com/fix/ and every English sibling page on the Help sitemap; official screenshot `getting-fix-api-0.png`; official Spotware C# sample on GitHub; grep of `D:\Prop\src` and `D:\Prop\apps` for `35=D` / send path. |

**Verdict.** Official cTrader FIX is **FIX 4.4**, two sessions (**QUOTE** / **TRADE**), **TargetCompID** valid value **`CSERVER`** (UI form prints **`cServer`**), TLS ports on the official credentials form **5211 (QUOTE/price SSL)** and **5212 (TRADE SSL)**. Official RoE defines **New Order Single `MsgType(35)=D`** as a client→cTrader application message; every official `35=D` example uses **`57=TRADE`**. **This repository does not send `35=D`.** Product C# has **zero** `35=D` literals. The only live socket write is Logon `35=A`. Spotware lists trade copiers as a FIX application, then says **other Spotware APIs are more suitable**.

---

## 1. Official English page set (sitemap 2026-08-17)

https://help.ctrader.com/sitemap.xml lists these English FIX URLs (other locales are translations of the same eight):

| Page | URL | Fetched this slot |
|---|---|---|
| Getting started | https://help.ctrader.com/fix/ | Yes |
| Benefits | https://help.ctrader.com/fix/benefits/ | Yes |
| Limitations | https://help.ctrader.com/fix/limitations/ | Yes |
| Get credentials | https://help.ctrader.com/fix/getting-credentials/ | Yes |
| Credentials screenshot | https://help.ctrader.com/fix/img/getting-fix-api-0.png | Yes (downloaded; passwords already `****` on the image) |
| Communication model | https://help.ctrader.com/fix/communication-model/ | Yes |
| Send and receive messages | https://help.ctrader.com/fix/sending-and-receiving-messages/ | Yes |
| Specification (Rules of Engagement) | https://help.ctrader.com/fix/specification/ | Yes |
| FAQs | https://help.ctrader.com/fix/faqs/ | Yes |

Vendor pages linked from Help / marketing (not extra RoE):

| Resource | URL |
|---|---|
| Spotware “FIX API for trading” | https://www.spotware.com/ctrader/dev-resources/fix-api-for-trading/ |
| Official C# sample (linked from send/receive) | https://github.com/spotware/FIX-API-Sample |
| Sample form (current master) | https://raw.githubusercontent.com/spotware/FIX-API-Sample/master/FIX%20API%20Sample.cs |
| Official Python client | https://github.com/spotware/cTraderFixPy |
| Open API (limitations page “another API”) | https://openapi.ctrader.com/ |
| FIX 4.4 standard (linked from RoE) | https://www.fixtrading.org/standards/fix-4-4/ |

Nav labels “Dictionary” / “SDKs” on the Help FIX section are **not** on the sitemap. Do not treat `https://help.ctrader.com/fix/dictionary/` or `.../sdks/` as live official pages.

---

## 2. Official fact: FIX 4.4

Quoted from https://help.ctrader.com/fix/ :

> “cTrader supports FIX version 4.4.”

Same sentence on https://help.ctrader.com/fix/specification/ (FIX version section). RoE points at the FIX Trading Community 4.4 standard.

Standard header tag 8 `BeginString` required value: **`FIX.4.4`**. Always first field, always unencrypted  
(https://help.ctrader.com/fix/specification/ — Standard header).

Communication-model page: every message starts `8=FIX.x.y` and ends with checksum tag 10.

Send/receive sample header constructor hard-codes `8=FIX.4.4|`.

**This is not FIX 5.x, not FIXT 1.1, not a proprietary binary.** Session is classic FIX 4.4 tag=value over a socket.

---

## 3. Official fact: QUOTE vs TRADE (two connections, two credential sets)

### 3.1 Credentials form (price vs trade)

Quoted from https://help.ctrader.com/fix/getting-credentials/ :

> “There are 2 types of connection, price connection and trade connection, and each type has its own separate set of credentials. Trading operations requests cannot be sent through the price connection's credentials and vice versa.”

Official screenshot (`getting-fix-api-0.png`) labels the two blocks **Price Connection** and **Trade Connection**. On that official image (passwords already masked `****`; values below are **UI labels**, not this repo’s secrets):

| UI block | `SenderSubID` printed | Port line printed |
|---|---|---|
| Price Connection | `QUOTE` | `5211 (SSL), 5201 (Plain text)` |
| Trade Connection | `TRADE` | `5212 (SSL), 5202 (Plain text)` |

Both blocks print `TargetCompID: cServer`. Host names on the screenshot are **form examples**; RoE does not publish a global hostname. FAQ still says check **your** host and port.

### 3.2 Session qualifier in the FIX header (tag 57, not tag 50)

Quoted from specification standard header, tag 57 `TargetSubID`:

> “An additional session qualifier. Possible values are `QUOTE` and `TRADE`.”

Quoted tag 50 `SenderSubID`:

> “The assigned value used to identify a specific message originator. Must be set to `QUOTE` if `TargetSubID=QUOTE`.”

Do **not** treat tag 50 as the TRADE session qualifier. Official TRADE Logon request uses `57=TRADE` and `50=any_string`  
(https://help.ctrader.com/fix/specification/ — Logon).

Communication-model example table repeats the same two qualifier values on tag 57.

### 3.3 Two sockets / two ports

Quoted from https://help.ctrader.com/fix/sending-and-receiving-messages/ :

> “In our case we create two clients, since price quotation messages and trade messages are handled by different ports on the server.”

Official sample constructs `_priceClient` on `_pricePort` and `_tradeClient` on `_tradePort`. New Order Single buttons call `SendTradeMessage`. Market-data buttons call `SendPriceMessage`.

### 3.4 What belongs on which session (from official examples)

Taken only from request/response examples on https://help.ctrader.com/fix/specification/ :

| Qualifier in official examples | Messages shown |
|---|---|
| `QUOTE` (`57=QUOTE` and/or `50=QUOTE`) | Market Data Request `V`, Snapshot `W`, Incremental `X`, Market Data Request Reject `Y` |
| `TRADE` (`57=TRADE`) | New Order Single `D`, Order Status Request `H`, Order Mass Status `AF`, Execution Report `8`, Request for Positions `AN`, Position Report `AP`, Order Cancel `F`, Order Cancel Reject `9`, Order Cancel/Replace `G`, Security List Request `x`, Security List `y`, Business Message Reject `j` |

This matches the credentials rule: trading operations do not go on the price connection.

### 3.5 Session lifecycle (Logon)

Quoted from https://help.ctrader.com/fix/communication-model/ :

> “FIX API uses session-based communication.”  
> “The server validates client requests using the Logon message.”

Typical flow on that page: Logon → application messages → Logout.

Quoted from specification Logon (`MsgType(35)=A`):

> “A Logon message is sent from the client side application to begin a cTrader FIX session, and a response is sent by cTrader to the client side application. Once the logon is complete, quote and trade flows can proceed for the lifecycle of the session.”

> “If an invalid Logon message is received by cTrader (with invalid fields), cTrader sends a Logout message in response.”

Logon body (official required / noted fields):

| Tag | Field | Official note |
|---|---|---|
| 98 | `EncryptMethod` | Required `0` = `NONE_OTHER`. “Currently, only transport-level security is supported.” “encryption is not used” **at the FIX-message layer**. |
| 108 | `HeartBtInt` | Default 30 seconds. `0` = no heartbeat required. |
| 141 | `ResetSeqNumFlag` | Optional `Y`. Connectivity section: reset seq nums on session establish. |
| 553 | `Username` | Numeric trader login **only**. Not the dotted CompID. |
| 554 | `Password` | User password. **Not printed in this report.** |

Quoted Logon note:

> “The field `Username` (tag=553) must contain a numeric trader login value, whilst `SenderCompID` (tag=49) must contain an environment, `BrokerUID` and a trader login delimited by a dot (for example, `live.theBroker.12345`).”

Official successful Logon **response** swaps CompIDs: client `49=…` / `56=CSERVER` / `57=TRADE` / `50=any_string` → server `49=CSERVER` / `56=…` / `50=TRADE` / `57=any_string`.

FAQ: quote-feed heartbeats may be absent while quotes stream. That is documented as expected.

---

## 4. Official fact: TLS / ports

### 4.1 RoE does **not** publish a port table in prose

Specification “Connectivity”:

> “A connection to cTrader’s FIX engine is available via the Internet, a VPN tunnel or a cross-connect to our data centre facilities in the UK. Contact us for further details.”

No hostname, no TCP port in that paragraph.

FAQ no-response checklist: check host, port, trading account number, password, SenderCompID, TargetCompID, SenderSubID.

### 4.2 Official numeric ports (credentials screenshot + current official sample)

Official Get-credentials screenshot prints:

| Session | Official port line |
|---|---|
| Price / QUOTE | **5211 (SSL)**, 5201 (Plain text) |
| Trade / TRADE | **5212 (SSL)**, 5202 (Plain text) |

Current official C# sample (`FIX API Sample.cs`, linked from Help send/receive and from spotware.com) uses the **SSL pair** and wraps both sockets in `SslStream` + `AuthenticateAsClient`:

- `_pricePort = 5211`
- `_tradePort = 5212`

(Sample also hard-codes a demo host and a password. **Password is not copied here.** Treat sample host as an example, same as the screenshot.)

### 4.3 EncryptMethod vs TLS (do not confuse)

Tag 98 = `0` means **no FIX-body encryption**. Official comment: **transport-level** security is what they support. TLS is outside the FIX string.

Send/receive Help article (dated **03/02/2017**, RoE v2.9.1) still shows **plain** `TcpClient` with no `SslStream`. That article is older than the current GitHub sample. Current official sample = SSL on 5211/5212. Do not treat the 2017 constructor as current TLS law.

### 4.4 What is **not** official as a global constant

| Claim | Official status |
|---|---|
| Every broker always uses 5211/5212/5201/5202 | **Not stated** in RoE prose. Screenshot + sample show those numbers; FAQ still says check **your** port. |
| One public hostname for all brokers | **Not stated.** Screenshot hosts are examples. Use the credentials form. |
| TLS version / cipher suite / pinning | **Not stated** on Help FIX pages. Official C# sample accepts `SslPolicyErrors.None`. |

---

## 5. Official fact: TargetCompID

Quoted from specification tag 56 `TargetCompID`:

> “A message target. The valid value is `CSERVER`.”

Quoted from communication-model example table, tag 56:

> “Message target is CSERVER. It is the only valid value within cTrader FIX API.”

Quoted from send/receive article:

> “`TargetCompID(*)` – it is provided in the FIX API form of cTrader (usually it is cServer)”  
> “this is the target of our message. In our case, it will always be cServer.”

Sample header comment: `Valid value is "CSERVER"`. Sample field: `_targetCompID = "CSERVER"`.

Official credentials screenshot prints **`TargetCompID: cServer`**.

**Do not collapse the case.** RoE table = `CSERVER`. Official UI form + send/receive prose = `cServer`. This repo’s options default to `cServer` (observed; not a secret). Live acceptance of either spelling is a **measured-engine** question, not something RoE settles in one sentence.

SenderCompID (tag 49) official format: `<Environment>.<BrokerUID>.<Trader Login>` (example `live.theBroker.12345`). That is the trading party, not the target.

---

## 6. Official fact: NewOrderSingle exists on TRADE

### 6.1 Catalog

Specification application list includes:

> “New Order Single (Client → cTrader)”

Communication-model application list:

> “New order single (client → cTrader) – used to electronically submit the orders to a broker for execution.”

Send/receive application list includes `MessageConstructor.NewOrderSingleMessage()`.

Official sample UI: `btnNewOrderSingle_Click` / stop / limit all call  
`NewOrderSingleMessage(SessionQualifier.TRADE, …)` then **`SendTradeMessage`**.

### 6.2 Wire type and required body (RoE)

Heading on specification: **New Order Single (MsgType(35)=D)**.

Official required fields (plus standard header/trailer):

| Tag | Field | Required | Official values / comments |
|---|---|---|---|
| 11 | `ClOrdID` | Yes | Unique client order id |
| 55 | `Symbol` | Yes | Long. “Instrument identifiers are provided by Spotware.” (FIX symbol ID, not the human name) |
| 54 | `Side` | Yes | `1` Buy, `2` Sell |
| 60 | `TransactTime` | Yes | Client-generated UTC |
| 38 | `OrderQty` | Yes | Qty; max precision 0.01 |
| 40 | `OrdType` | Yes | `1` Market (IOC), `2` Limit (GTC unless ExpireTime), `3` Stop (same GTC/GTD rule) |
| 44 | `Price` | No | Required when `OrdType=2` |
| 99 | `StopPx` | No | Required when `OrdType=3` |
| 59 | `TimeInForce` | No | **Deprecated, ignored.** TIF inferred from OrdType + ExpireTime |
| 126 | `ExpireTime` | No | If set, GTD for limit/stop |
| 721 | `PosMaintRptID` | No | Existing position; hedged accounts only |
| 494 | `Designation` | No | Custom label |

Official `35=D` examples all carry **`57=TRADE`**. Market-to-new-position example (placeholders only):

`35=D|…|57=TRADE|11=…|55=1|54=1|60=…|40=1|38=10000`

Server replies with Execution Report `35=8` on the TRADE session (`50=TRADE` inbound).

### 6.3 Related TRADE messages (so copy is not “D only”)

RoE also documents on the trade side: Order Status `H`, Mass Status `AF`, Execution Report `8`, Positions `AN`/`AP`, Cancel `F`, Cancel Reject `9`, Cancel/Replace `G`, Security List `x`/`y`, Business Reject `j`.

A sender that only knew `35=D` would still be incomplete versus official RoE. This slot’s point is narrower: **official protocol has a TRADE NewOrderSingle; this repo does not implement a sender.**

---

## 7. Official fact: trade copiers are listed, then downgraded

Quoted in full from https://help.ctrader.com/fix/ — “Typical application methods” → **Trade copiers**:

> “Systems that will automatically replicate trades on multiple trading accounts across multiple brokers or on the accounts of traders are connected to the copier. **However, we believe other Spotware APIs are more suitable for this.**”

Same page applies the same “other APIs more suitable” caveat to **custom trading interfaces**.

Limitations page (https://help.ctrader.com/fix/limitations/ ):

> “FIX API serves two primary purposes: 1. Receive live market data 2. Perform trading operations”

> “with FIX API they will not be able to access cTrader account information such as current balance, leverage, margin and more.”

> “it does not support requests for historical market data.”

That page points at Open API (https://openapi.ctrader.com/) “to request market data, account information and additional functionality.”

**Honesty for this product.** Official docs do **not** forbid using FIX as a destination for copied orders. They **do** list copy as a use case and immediately say other Spotware APIs fit copy better. FIX cannot read balance/leverage/margin or history. A copy system that needs those facts must use another API (or the broker UI) alongside FIX. This is vendor guidance, not a license block.

---

## 8. This repo has **no** NewOrderSingle sender

Measured on 2026-08-18. Product source was **not** edited.

### 8.1 Grep

| Search | Path | Hits |
|---|---|---|
| `35=D` | `D:\Prop\src` | **0** |
| `35=D` | `D:\Prop\apps` | **0** |
| `NewOrderSingle` | product `.cs` | Comments, log strings, a default-false flag, worker refuse path, FSM helper name only |

There is no `NewOrderSingleMessage`, no `MsgType=D` builder, no `35=D` assemble, no ClOrdID write to a TRADE stream.

### 8.2 Only live socket write is Logon `35=A`

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines):

- Connects TLS (`SslStream`, TLS 1.2/1.3) to the given host/port.
- Builds **one** message: `BuildLogon` with `(35, "A")` plus 34/49/56/50/57/52/98=0/108=30/141=Y/553/554.
- **One** `WriteAsync`.
- Reads one reply; treats `35=A` as LoggedOn.
- Disposes sockets. No loop, no market-data subscribe, no order.

`CTraderFixLogonHostedService` calls that helper twice (QUOTE 5211, TRADE 5212), then sets `RealCopyEnabled = false` and logs “NewOrderSingle still disabled”.

### 8.3 Other Fix.CTrader files are not a sender

| File | What it does | Wire send? |
|---|---|---|
| `CTraderFixOptions.cs` | Defaults including `RealCopyExecutionEnabled = false`, SSL 5211/5212, `TargetCompId = "cServer"` | No |
| `CTraderQuoteService.cs` | In-memory tag **lists** (`35=V`, a `35=y` list). No `TcpClient` | No |
| `FixMessageParser.cs` | Parse/assemble helpers | No |
| `FixSimulationHarness.cs` | In-process fake replies for tests (`35=A`/`8`/`X`…). No network | No |
| `FixSessionOwnership.cs` | Ownership helper | No |

`apps/fix-worker/Worker.cs` stamps session rows **Disconnected** and “NewOrderSingle remains off.” If `CTrader:RealCopyExecutionEnabled` is true it **logs a warning and still refuses**.

`src/Infrastructure/DependencyInjection.cs` comment + assignment: live NewOrderSingle is not implemented; `RealCopyEnabled = false`.

Domain `MayRetryNewOrderSingle` is an FSM predicate, not a socket.

**SAFE_BY_ABSENCE.** Official protocol has `35=D` on TRADE. This tree cannot emit it. Logon ≠ send. A later commit could add a sender; this slot’s measurement is the current product C#.

---

## 9. CompID / qualifier mapping (official, easy to get wrong)

1. **Tag 57 is the session qualifier** (`QUOTE` / `TRADE`). Tag 50 is originator text, except **must be `QUOTE` when 57=QUOTE**.
2. **Do not put `TRADE` in tag 50 and call that the qualifier.** Official TRADE Logon uses `57=TRADE|50=any_string`.
3. **Tag 49** = dotted `<Environment>.<BrokerUID>.<Login>`. **Tag 553** = numeric login only.
4. **Tag 56** = `CSERVER` / UI `cServer`. Only valid target inside cTrader FIX.
5. **Two credential sets, two sockets.** Do not send `35=D` on the price port. Official examples never do.
6. **Symbol(55)** is the broker-specific numeric FIX symbol ID (symbol-info window). Wrong ID = wrong instrument. Credentials page warns IDs differ across brokers.
7. **Seq reset** on establish (connectivity + tag 141).
8. **Multiple connections duplicate reports** (FAQ).
9. **Checksum / UTC / tag order** must match RoE or the server may send nothing (FAQ).

---

## 10. What this slot does **not** claim

- That live QUOTE/TRADE Logon is proven in this process (not re-measured here).
- That `cServer` vs `CSERVER` is accepted by every gateway (RoE vs UI disagree on case).
- That 5211/5212 are the only ports any broker will ever print.
- That FIX is the vendor-preferred copy API (it is listed, then downgraded).
- That this repo is “ready to send” once a flag flips. The sender **does not exist**.
- Any password, any live FIX secret, any sample password from GitHub.

---

## 11. Sources

- https://help.ctrader.com/fix/
- https://help.ctrader.com/fix/benefits/
- https://help.ctrader.com/fix/limitations/
- https://help.ctrader.com/fix/getting-credentials/
- https://help.ctrader.com/fix/img/getting-fix-api-0.png
- https://help.ctrader.com/fix/communication-model/
- https://help.ctrader.com/fix/sending-and-receiving-messages/
- https://help.ctrader.com/fix/specification/
- https://help.ctrader.com/fix/faqs/
- https://help.ctrader.com/sitemap.xml
- https://www.spotware.com/ctrader/dev-resources/fix-api-for-trading/
- https://github.com/spotware/FIX-API-Sample
- https://raw.githubusercontent.com/spotware/FIX-API-Sample/master/FIX%20API%20Sample.cs
- Product read-only: `src/Fix.CTrader/Sessions/CTraderFixSession.cs`, `Hosting/CTraderFixLogonHostedService.cs`, `Configuration/CTraderFixOptions.cs`, `apps/fix-worker/Worker.cs`, `src/Infrastructure/DependencyInjection.cs`
