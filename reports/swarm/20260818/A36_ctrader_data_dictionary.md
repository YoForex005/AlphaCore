# A36 — Is a Generic FIX 4.4 Data Dictionary Sufficient for cTrader?

**Question:** Can QuickFIX/n (or any FIX engine) talk to cTrader/cServer using a stock FIX 4.4 dictionary, or do we need a cTrader-specific Rules-of-Engagement dictionary because of custom fields and instrument IDs?

**Verdict:** **No. A generic FIX 4.4 dictionary is not sufficient.** This is official Spotware policy, not a style preference. cTrader publishes its own QuickFIX dictionary (`FIX44-CSERVER.xml`), remaps `Symbol(55)` to a **broker-specific numeric instrument ID**, and defines **nine custom tags (1000–1008)** that do not exist in FIX 4.4. Using the generic dictionary will reject or mis-parse live Security List / Execution Report / Position Report traffic and will send orders that cServer refuses.

**Date:** 2026-08-18  
**Agent:** A36  
**Scope:** Research / planning only. No product source was modified.

**Architecture pin (already binding):** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §FIX Engine (lines 237–250) and §30 Instrument Discovery.

> Do not assume the generic FIX 4.4 dictionary is sufficient.  
> cTrader uses a defined subset and its own instrument identifiers/custom fields.

This report is the evidence file behind that pin.

---

## 1. Answer in one page

| Claim | Evidence | Binding implication |
|---|---|---|
| cTrader is FIX 4.4 **by version string only** | Official RoE: “cTrader supports FIX version 4.4” | `BeginString=FIX.4.4` is required. That does **not** mean the FPL/OnixS/QuickFIX stock `FIX44.xml` is the contract. |
| Spotware ships a **cServer-specific** QuickFIX dictionary | [help.ctrader.com/fix/FIX44-CSERVER.xml](https://help.ctrader.com/fix/FIX44-CSERVER.xml) | Pin this file (or a versioned copy) as `DataDictionary`. Do not use `spec/FIX44.xml` from QuickFIX/n. |
| Official staff told QuickFIX users to use **their** dictionary | Panagiotis Charalampous, community thread [22265](https://community.ctrader.com/forum/fix-api/22265/) (2019-11-08): “make sure you are using our dictionary with your quickfix engine” and linked `FIX44-CSERVER.xml` | Community confirmation that generic DD is the failure mode. |
| `Symbol(55)` is **not** a ticker | RoE types it `Long`; “Instrument identifiers are provided by Spotware.” Credentials page: “FIX symbol ID may differ across brokers.” Official reject: `Expected numeric symbolId, but got CS8260`. | Never send `55=XAUUSD`. Discover via Security List (`55` + custom `1007` name + `1008` digits). Persist per broker/account. |
| Nine custom tags 1000–1008 | Official RoE Execution Report / Position Report / Security List + `FIX44-CSERVER.xml` `<fields>` | Generic FIX 4.4 has **no** tags 1000–1008. They also **collide** with later FIX versions (e.g. FIX 5.0 SP2 `1007=SideReasonCd`). |
| Message **subset and required-field lists differ** | `FIX44-CSERVER.xml` NewOrderSingle has 12 body fields; generic FIX 4.4 NOS is a large Instrument + OrderQtyData + Parties message. Request-for-Positions is two fields. | Generic DD will fail outgoing validation (missing “standard” requireds) **or** accept extra fields cServer will reject / ignore / drop with no reply (FAQ). |
| `ValidateUserDefinedFields=N` is **not** a workaround | QuickFIX treats UDF as tag **≥ 5000**. cTrader customs are **1000–1008**. | `AllowUnknownMsgFields=Y` would only *not reject*; it would not give typed `SymbolName` / TP-SL fields. Wrong and unsafe. |

**Do this:** vendor and pin `FIX44-CSERVER.xml` (hash + fetch date), configure QuickFIX/n `DataDictionary=` to that file on **both** QUOTE and TRADE sessions, implement Security List → instrument map, never hardcode tag 55.

---

## 2. Official sources (primary)

| Doc | URL | What it settles |
|---|---|---|
| Rules of Engagement / Specification | https://help.ctrader.com/fix/specification/ | Full message/field contract. Tag 55 = Spotware instrument ID (`Long`). Custom 1000–1008. `721` as **position ID** on NOS/ER/AN/AP. |
| Communication model | https://help.ctrader.com/fix/communication-model/ | Tag 55 must be the **FIX symbol ID**; “that value can be different across brokers”; look it up in that broker’s cTrader Symbol info window. |
| Get credentials | https://help.ctrader.com/fix/getting-credentials/ | How to read FIX symbol ID from ASP → Symbol info. Explicit warning: IDs differ across brokers; wrong ID = trade/quote the **wrong** instrument. |
| FAQs | https://help.ctrader.com/fix/faqs/ | Invalid messages get **no response**. Tag order must match RoE. Tag 266 (non-aggregated book) is **not** in RoE. Quote session heartbeats are omitted while quotes stream. |
| Limitations | https://help.ctrader.com/fix/limitations/ | RoE is a **closed, non-extensible** operation set. No balance/leverage/margin. No historical MD. Use Open API alongside FIX for account data. |
| Send/receive (C# sample) | https://help.ctrader.com/fix/sending-and-receiving-messages/ | Always follow latest RoE. Separate QUOTE and TRADE sockets. Header: `TargetCompID=CSERVER`, `TargetSubID=QUOTE\|TRADE`. |
| Official QuickFIX dictionary | https://help.ctrader.com/fix/FIX44-CSERVER.xml | The actual DD to load. Comment `<!-- CServer Specifics -->` on `SymbolName` / `SymbolDigits`. |
| FIX API landing | https://help.ctrader.com/fix/ | “cTrader supports FIX version 4.4” + pointer to FPL 4.4 **and** the cTrader RoE. |
| Spotware product page | https://www.spotware.com/ctrader/dev-resources/fix-api-for-trading/ | Points at Help Centre spec + Discord. |
| Official Python client | https://github.com/spotware/cTraderFixPy | Builds NOS with `55`, `721`, `494`; SecurityListRequest with numeric `55`. No generic DD. |
| Official QuickFIX/n sample | https://github.com/spotware/quickfixnsamples.net | Community developer (amusleh) uses this with `DataDictionary=./FIX44-CSERVER.xml`. |

**FPL / generic contrast (not the cTrader contract):**

| Doc | URL | What generic 4.4 says |
|---|---|---|
| OnixS FIX 4.4 `Symbol(55)` | https://www.onixs.biz/fix-dictionary/4.4/tagnum_55.html | Type **String**. “Ticker symbol. Common, human understood representation.” `SecurityID(48)` is the place for an ID when no ticker exists. |
| OnixS FIX 4.4 New Order Single | https://www.onixs.biz/fix-dictionary/4.4/msgType_D_68.html | Requires Instrument + OrderQtyData components. Forex convention is `CCY1/CCY2` **in tag 55**. `Designation(494)` is “supplementary registration information,” not a cTrader order label. **No** `PosMaintRptID(721)` on NOS. |
| OnixS FIX 5.0 SP2 `1007` | https://www.onixs.biz/fix-dictionary/5.0.sp2/tagnum_1007.html | `SideReasonCd` — **name collision** with cTrader `SymbolName`. |
| QuickFIX/n config | https://quickfixengine.org/n/documentation/configuration.html | `ValidateUserDefinedFields` only covers tag ≥ 5000. `AllowUnknownMsgFields` is for unknown tags **< 5000**. |

---

## 3. Instrument IDs — the fatal generic-dictionary assumption

### 3.1 What generic FIX 4.4 thinks

Generic FIX 4.4:

- `Symbol(55)` = human ticker (`EURUSD`, `XAUUSD`, `GBP/USD`).
- Optional `SecurityID(48)` + `SecurityIDSource(22)` for CUSIP/ISIN/exchange ID.
- Security List returns those standard Instrument fields.

cTrader **does not define tag 48 at all** in `FIX44-CSERVER.xml`. There is no `SecurityID` / `SecurityIDSource` path.

### 3.2 What cTrader actually requires

Official communication model (verbatim intent):

> If the `Symbol(55)` tag is used in a FIX message to cTrader/cServer, you must specify the **FIX symbol ID**. That value can be different across brokers. You can find FIX Symbol ID from that broker’s cTrader application in the symbol information window.

Official credentials page:

> Each symbol has a unique ID, which is required to identify it in the FIX messages.  
> **FIX symbol ID may differ across brokers** so it is important to check before connecting to a new broker using FIX API, otherwise you may be trading or receiving prices for the **wrong symbols**.

Official RoE field comments (Market Data Request / Snapshot / Incremental / New Order Single / Execution Report):

| Tag | RoE name | RoE type | RoE comment |
|---|---|---|---|
| 55 | Symbol | **Long** (Security List: Integer) | “Instrument identifiers are provided by Spotware.” |

Worked official examples (RoE):

```
MarketDataRequest:  ...|146=1|55=1|267=2|269=0|269=1|...
NewOrderSingle:     ...|11=876316397|55=1|54=1|60=...|40=1|38=10000|...
SecurityListReq:    ...|55=39|320=ILCea0JkdQEm|559=0|...
SecurityList:       ...|146=1|55=39|1007=NZDCHF|1008=4|...
```

`55=1` in the sample book is **EURUSD** on that sample broker (`55=1|1007=EURUSD|1008=5` in the long Security List dump). **Do not treat `1` as EURUSD on Pepperstone or any other book.** The same dump has IDs like `10001`, `22312`, `21576` — they are not sequential tickers.

Official reject when someone sent a string “symbol”:

```
35=Y ... 58=INVALID_REQUEST: Expected numeric symbolId, but got CS8260|262=...|281=0
```

(`Market Data Request Reject`, RoE examples. `281=0` = Unknown symbol.)

### 3.3 How to resolve name ↔ ID (official + community)

1. **Security List Request (`35=x`)** with `559=0`. Optional `55` filters to one ID. Omit `55` to list the book.
2. **Security List (`35=y`)** repeating group `146=NoRelatedSym`:
   - `55` = numeric FIX symbol ID
   - `1007 SymbolName` = human name (`XAUUSD`, `EURUSD`, …)
   - `1008 SymbolDigits` = 0–5
3. Persist the triple **per broker / environment / account**. Architecture §30.
4. Manual fallback: cTrader ASP → Symbol info → **FIX symbol ID** (credentials page + [community 12218](https://community.ctrader.com/forum/fix-api/12218/), Spotware staff Panagiotis).

Community / integrator corroboration:

- SharpTrader FIX bridge: “symbols are recognized by **FIX Symbol ID, not by name**.” Unicum field must be the FIX ID from symbol spec.
- HFT Forex Copier docs: “FIX symbol IDs may differ across cTrader brokers — a mismatch can result in trades on the wrong instrument.”
- [community 41846](https://community.ctrader.com/forum/fix-api/41846/): title is exactly the type clash (“tag 55 … long but tag 55 is store string”). Working sample used `55=7` for GBPJPY on that broker, **not** `55=GBPJPY`.

### 3.4 Why a generic DD cannot save you here

Even if QuickFIX accepts `55=XAUUSD` as a legal String (generic type), **cServer will reject or route the wrong instrument**. The dictionary cannot invent the ID. Discovery + a mapping table is mandatory. Architecture A28 rule 13 / §30 already forbids hardcoding.

`FIX44-CSERVER.xml` still types `55` as `STRING` (QuickFIX wire type). That is compatible with sending `"41"` as text. It is **not** permission to send `"XAUUSD"`.

---

## 4. Custom fields (not in generic FIX 4.4)

From official RoE + `FIX44-CSERVER.xml` `<fields>`:

| Tag | cTrader name | Type | Where used | Generic FIX 4.4 | Later FIX collision |
|---|---|---|---|---|---|
| 1000 | AbsoluteTP | PRICE | ER, Position Report | **absent** | assigned in later versions |
| 1001 | RelativeTP | PRICE | ER | **absent** | assigned in later versions |
| 1001 | (same) | | distance in **pips** from entry | | |
| 1002 | AbsoluteSL | PRICE | ER, Position Report | **absent** | |
| 1003 | RelativeSL | PRICE | ER | **absent** | |
| 1004 | TrailingSL | BOOLEAN `Y/N` | ER, Position Report | **absent** | |
| 1005 | TriggerMethodSL | INT 1–4 | ER, Position Report | **absent** | |
| 1006 | GuaranteedSL | BOOLEAN `Y/N` | ER, Position Report | **absent** | |
| 1007 | **SymbolName** | STRING | Security List Instrument | **absent** | FIX 5.0 SP2 `1007=SideReasonCd` |
| 1008 | **SymbolDigits** | INT 0–5 | Security List Instrument | **absent** | assigned in later versions |

`FIX44-CSERVER.xml` Instrument component (verbatim structure):

```xml
<component name="Instrument">
    <field name="Symbol" required="Y" />
    <!-- CServer Specifics -->
    <field name="SymbolName" required="N" />
    <field name="SymbolDigits" required="N" />
</component>
```

That comment is Spotware admitting these are **not** FPL Instrument fields.

### 4.1 Why `ValidateUserDefinedFields=N` does not fix this

QuickFIX/n documentation:

- **User-defined fields** = tag **≥ 5000** (legacy 5000–9999; bilateral 20000–39999 per FPL).
- Tags **1000–1008 are below 5000**, so they are treated as **unknown standard fields**.
- Default `AllowUnknownMsgFields=N` → incoming Security List / ER / AP with 1000–1008 is session-rejected (`373=3` Undefined Tag, or dropped depending on engine).
- `AllowUnknownMsgFields=Y` → message accepted as untyped leftovers. Application code cannot `GetDecimal(Tags.AbsoluteSL)` / `GetString(SymbolName)` unless the DD defines them.
- Loading a **FIX 5.0+** dictionary is worse: 1007/1008 parse as the **wrong** official fields.

So the only correct engine configuration is: **cTrader DD that names 1000–1008**.

---

## 5. Standard tags with **cTrader-specific semantics or placement**

These exist in generic FIX 4.4 but are **not** used the generic way. A generic DD either forbids them on the message or teaches the wrong meaning.

| Tag | Generic FIX 4.4 | cTrader RoE |
|---|---|---|
| 55 Symbol | Ticker string | Numeric Spotware/broker instrument ID |
| 721 PosMaintRptID | Position **Maintenance Report** ID (pos-maint messages) | **cTrader position ID**. Optional on NOS to target an existing **hedged** position; echoed on ER; filter on Request-for-Positions; identity on Position Report. |
| 494 Designation | Supplementary registration / CIV info on NOS | **Custom order label** (client comment). On ER too. |
| 225 IssueDate | Issue date of the **instrument** | On `OrderMassStatusRequest (AF)`: “if set, response contains only orders created **before** this date.” Type in RoE examples is a timestamp, not a calendar issue date. |
| 59 TimeInForce | Honored (Day/IOC/GTC/GTD/…) | On NOS: **deprecated, ignored**. TIF is inferred from `OrdType` + presence of `ExpireTime(126)`. Market → IOC; Limit/Stop without 126 → GTC; with 126 → GTD. |
| 38 OrderQty | Shares / contracts | cTrader **units** (volume). Max precision 0.01. **Not** MT5 lots. Architecture: never convert lots blindly. |
| 49 SenderCompID | Firm ID | `<Environment>.<BrokerUID>.<TraderLogin>` e.g. `live.theBroker.12345` / `demo.pepperstone.3832372` |
| 56 TargetCompID | Counterparty firm | Must be `CSERVER` (docs; some live books accept `cServer`) |
| 57 TargetSubID | Sub-id | Required `QUOTE` or `TRADE` — **two sessions** |
| 50 SenderSubID | Originator | Must be `QUOTE` when `57=QUOTE` |
| 141 ResetSeqNumFlag | Optional | RoE: all sides **should** reset on establish. Typical `141=Y`. |
| 553 / 554 | Username / Password | Numeric trader login + **FIX** password (not the cTrader UI password). |
| 266 AggregatedBook | Standard MD field | **Not in RoE**. FAQ: non-aggregated DOM is impossible. |
| 585 MassStatusReqType | Many enums | Only `7` (all orders) supported. |
| 559 SecurityListRequestType | 0–4 | Only `0` (Symbol) supported. |

Generic New Order Single does **not** include `721` in the message definition. A strict generic DD will reject an outgoing hedge-to-existing-position order that sets `721=…`.

Generic Execution Report does **not** include `721`, `494`, or 1000–1006. Incoming fill/status messages that carry the position ID and SL/TP will fail validation.

Generic Request-for-Positions (`AN`) requires `PosReqType(724)` and typically Account / Parties. cTrader `AN` is only `710` + optional `721`. A generic DD will refuse to **send** the legal cTrader request.

Generic Position Report (`AP`) is a large message (Account, Currency, ClearingBusinessDate, …). cTrader `AP` is a short custom subset. Incoming reports will miss “required” generic fields and/or contain unknown 1000/1002/1004–1006.

---

## 6. Message subset vs generic FIX 4.4

cTrader application messages (RoE + `FIX44-CSERVER.xml`):

| MsgType | Name | Direction | In generic 4.4? | cTrader vs generic |
|---|---|---|---|---|
| V | Market Data Request | → | yes | Symbol **inside** `NoRelatedSym` group; `55` numeric; `267=2`; depth only 0 or 1 |
| W | MD Snapshot/Full Refresh | ← | yes | `55` numeric; no full Instrument block |
| X | MD Incremental Refresh | ← | yes | update 0/2 only |
| Y | MD Request Reject | ← | yes | `281=0` used for bad symbol ID |
| D | New Order Single | → | yes | **12 fields**. No HandlInst, no Parties, no SecurityID. Adds `721`, `494`. |
| 8 | Execution Report | ← | yes | Adds `721`, `494`, `584`, `911`, **1000–1006** |
| H | Order Status Request | → | yes | `11` + optional `54` only |
| AF | Order Mass Status Request | → | yes | `584`, `585=7`, remapped `225` |
| F / G / 9 | Cancel / Replace / Cancel Reject | ↔ | yes | Small field lists |
| j | Business Message Reject | ← | yes | `380=0` Other, text explains |
| AN | Request for Positions | → | yes | `710` + optional `721` only |
| AP | Position Report | ← | yes | Short; `55` still the numeric ID; TP/SL customs |
| x / y | Security List Request / List | ↔ | yes | Instrument = `55` + **1007** + **1008** |

Admin: 0, 1, A, 5, 2, 3, 4 — standard-shaped but header CompIDs/SubIDs are cTrader-specific.

Not supported (generic engines love these; cTrader does not): Order Cancel All, Trade Capture, Allocation, Quote, IOI, Security Definition (vs List), historical MD, account/collateral, tag 266.

Limitations page: the set **cannot be extended**. If it is not in the RoE, it does not exist.

---

## 7. Community evidence that generic / stock QuickFIX dictionaries fail

### 7.1 Official staff: use our dictionary

[Can't get market data request](https://community.ctrader.com/forum/fix-api/22265/) (2019-11-08).

User sent QuickFIX Python `fix.Symbol("2")` on `35=V`. cServer reply:

```
35=3 ... 58=Tag not defined for this message type, field=55|371=55|372=V|373=2
```

Trying `Symbol="EURUSD"` produced the **same** reject. Root cause was **field order / repeating-group layout** versus RoE (`55` must sit **inside** `146 NoRelatedSym`, after the MD-entry-type group), plus `146=0`.

Spotware staff **Panagiotis Charalampous**:

> Can you please also make sure you are using our dictionary with your quickfix engine?

He first posted a bad link, then corrected it to **`https://help.ctrader.com/fix/FIX44-CSERVER.xml`**. User: “I solved the problem.”

A 2021 follow-up still hit `373=2 field=55` until the cTrader dictionary **and** group order were both correct. Dictionary alone is necessary, not sufficient — the DD encodes the **group membership** of tag 55.

This is the cleanest public proof that:

1. Stock QuickFIX `FIX44.xml` does not match cServer’s MarketDataRequest layout / required groups.
2. Spotware’s supported path is `FIX44-CSERVER.xml`.

### 7.2 Tag 55 type confusion

[tag 55 give document msgType long but tag 55 is store string](https://community.ctrader.com/forum/fix-api/41846/) (2023-09-15). Working counter-example used `55=7` (numeric ID for GBPJPY on that book).

### 7.3 How to get the ID

[Symbol Id](https://community.ctrader.com/forum/fix-api/12218/) — Spotware staff: (1) Symbol Information → FIX Symbol ID; (2) Connect/Open API `symbolId`.

### 7.4 Production configs actually load the cServer DD

[FIX API crash…](https://community.ctrader.com/forum/fix-api/37414/) — QuickFIX/n users and Spotware’s own sample:

```
DataDictionary=./FIX44-CSERVER.xml
; also seen: DataDictionary=.\ctrader_FIX44.xml
```

Spotware community developer **amusleh** pointed at https://github.com/spotware/quickfixnsamples.net and subscribed using **numeric IDs** (`i.ToString()` in a 1..200 loop) — not tickers.

### 7.5 Third-party integrators

SharpTrader: map Unicum = FIX Symbol ID, **not** name.

HFT copier: IDs differ by broker; wrong ID = wrong instrument.

---

## 8. What breaks if we ship generic `FIX44.xml`

| Scenario | Generic DD behavior | Production impact |
|---|---|---|
| Subscribe MD with `55=XAUUSD` | Locally valid String | cServer `Y` / unknown symbol, or worse, **wrong** ID if some book happens to coerce |
| Subscribe MD with `55=41` but 55 **outside** `146` group | Engine may emit tag-55 at wrong place | cServer `35=3` `373=2` “Tag not defined for this message type, field=55” (seen live) |
| Security List inbound `1007`/`1008` | Undefined tag (<5000) | Session reject or dropped names/digits → **cannot build instrument map** |
| Execution Report inbound `721` + `1000–1006` | `721` often not legal on 35=8; 1000–1006 unknown | Reject fills / lose position ID / lose SL-TP on reconcile |
| Position Report inbound | Missing generic requireds + custom SL/TP | Reconcile broken |
| NOS to existing hedge pos `721=` | 721 not on generic NOS | Engine refuses send **or** strips field → **opens a new position** |
| NOS without HandlInst / extra Instrument fields | Generic may require more than RoE | Local reject, or cServer silent drop (FAQ: invalid FIX → **no response**) |
| Request-for-Positions | Missing generic `PosReqType` | Cannot reconcile positions |
| Load FIX 5.0 DD “to get 1000+ tags” | 1007 parsed as `SideReasonCd` | **Silent semantic corruption** of symbol names |

None of these are theoretical. The MD `373=2 field=55` and `Expected numeric symbolId` rejects are on the official pages / official forum.

---

## 9. Recommended dictionary + session policy (for Phase 4 / 7)

This is the implementation contract implied by the official docs. Still no product-source change from this agent.

### 9.1 Dictionary

1. Fetch `https://help.ctrader.com/fix/FIX44-CSERVER.xml`.
2. Vendor a **versioned copy** under the repo (e.g. `fix/dictionaries/FIX44-CSERVER.xml`) with SHA-256 and fetch timestamp in the same report/PR.
3. Re-fetch when Spotware revises RoE (Limitations/RoE both say the min set can change).
4. QuickFIX/n both sessions:

```
BeginString=FIX.4.4
UseDataDictionary=Y
DataDictionary=fix/dictionaries/FIX44-CSERVER.xml
ValidateUserDefinedFields=Y
AllowUnknownMsgFields=N
ValidateFieldsOutOfOrder=Y
ResetOnLogon=Y
```

Do **not** point at QuickFIX/n `spec/FIX44.xml`.  
Do **not** “relax validation to make generic DD work.” That hides 1007/1008 and group errors.

5. Keep a **diff note** against stock FIX44.xml in the same folder (custom fields, Instrument component, per-message field lists). Re-diff on every vendor bump.

### 9.2 Instrument map (Architecture §30)

On TRADE (and/or QUOTE) logon:

```
SecurityListRequest (35=x, 559=0)
  → SecurityList (35=y)
  → table:
       broker_uid
       environment          -- demo|live
       account_login
       fix_symbol_id        -- tag 55
       symbol_name          -- tag 1007
       symbol_digits        -- tag 1008
       retrieved_utc
```

Resolve canonical `XAUUSD` (and aliases `XAUUSD.a`, `GOLD`, …) **only** via this table. Fail closed if no unique match.

Never copy an ID from another broker, from the RoE sample book (`55=1` = EURUSD **in the sample only**), or from a previous Pepperstone account.

UI fallback: credentials-page FIX symbol ID, stored as an override with audit, still verified against Security List.

### 9.3 Application encoding rules the DD will not save you from

- Tag 55 always numeric string of the mapped ID.
- `38` is **units**, not lots.
- Hedged close/add: set `721` to the cTrader position ID from ER/AP.
- Do not send `59`; set `126` only when you want GTD.
- `49` / `56` / `57` / `50` / `553` / `554` per current broker FIX form + RoE (Architecture §26). Do not guess from labels.
- Two sessions, two sequence stores (Architecture §27 / A28 rule 11).
- Invalid messages may get **zero** reply (FAQ) — timeouts are not “no liquidity.”

### 9.4 What the dictionary does **not** contain

- Account balance, margin, leverage (Limitations → use Open API).
- Historical candles.
- Non-aggregated DOM (tag 266).
- A global XAUUSD ID.

---

## 10. Architecture cross-check

| Architecture statement | Research result |
|---|---|
| “Do not assume the generic FIX 4.4 dictionary is sufficient.” | **Confirmed.** Official DD + official staff + field collisions. |
| “cTrader uses a defined subset and its own instrument identifiers/custom fields.” | **Confirmed.** Closed RoE message set; tag 55 numeric; tags 1000–1008. |
| §30 Security List → persist ID / name / digits; do not hardcode | **Confirmed.** `55` + `1007` + `1008`. IDs differ by broker. |
| Two independent QUOTE / TRADE sessions | **Confirmed.** Credentials page + header `57`. |
| Prefer QuickFIX/n + cTrader RoE dictionary | **Confirmed.** Official sample + `FIX44-CSERVER.xml`. |

No architecture change required. This report is the citation pack for that already-written law.

---

## 11. Residual unknowns (honest)

| Item | Status |
|---|---|
| Whether `TargetCompID` must be exactly `CSERVER` vs `cServer` | Docs say `CSERVER`. Live community configs often use `cServer`. **Use the value on the broker-issued FIX form.** |
| Whether Security List is legal on QUOTE as well as TRADE | Official examples use `57=TRADE`. Confirm on the target Pepperstone book in Phase 4; do not assume. |
| Whether every broker’s ID space is stable across symbol-list changes | Officially “may differ across brokers.” **Within** a broker, treat IDs as stable until Security List says otherwise; refresh on reconnect. |
| RelativeTP/SL unit (“pips”) vs points vs price increment | RoE says “distance in pips.” Confirm against Pepperstone digits/`1008` before any SL/TP write. Out of scope for dictionary choice. |
| Hash of today’s `FIX44-CSERVER.xml` | Fetched 2026-08-18 from help.ctrader.com. Re-hash when vendoring into the product tree (not done by this agent — no product source edits). |

---

## 12. Bottom line

**Generic FIX 4.4 is the wrong contract.**

cTrader speaks FIX 4.4 framing (`8=FIX.4.4`) over a **cServer dialect**:

1. **Instrument identity** is a **per-broker numeric FIX symbol ID in tag 55**, not a ticker. Human names live in **custom tag 1007**. Digits in **custom tag 1008**. Official server text: `Expected numeric symbolId`.
2. **Custom tags 1000–1008** (TP/SL + symbol metadata) are absent from FIX 4.4 and collide with later official tags. They are **not** QuickFIX “user-defined fields” (≥5000), so relaxing UDF validation does not help.
3. **Message shapes** are a strict RoE subset with remapped fields (`721` = position ID, `494` = label, `225` = created-before filter, TIF ignored).
4. Spotware publishes and **tells QuickFIX users to load** [`FIX44-CSERVER.xml`](https://help.ctrader.com/fix/FIX44-CSERVER.xml).

Phase 4 (QUOTE) and Phase 7 (TRADE read) must vendor that dictionary and discover instruments via Security List. Guessing `55=XAUUSD` or copying an ID from another cTrader account is a go-live blocker (A28 rule 13 / `[DO NOT] Guessed cTrader symbol / instrument IDs`).

---

## 13. Source list (absolute / URL)

**Official**

- https://help.ctrader.com/fix/
- https://help.ctrader.com/fix/specification/
- https://help.ctrader.com/fix/communication-model/
- https://help.ctrader.com/fix/getting-credentials/
- https://help.ctrader.com/fix/faqs/
- https://help.ctrader.com/fix/limitations/
- https://help.ctrader.com/fix/sending-and-receiving-messages/
- https://help.ctrader.com/fix/FIX44-CSERVER.xml
- https://www.spotware.com/ctrader/dev-resources/fix-api-for-trading/
- https://github.com/spotware/cTraderFixPy
- https://github.com/spotware/cTraderFixPy/blob/main/ctrader_fix/messages.py
- https://github.com/spotware/quickfixnsamples.net
- https://www.fixtrading.org/standards/fix-4-4/

**Generic FIX contrast**

- https://www.onixs.biz/fix-dictionary/4.4/tagnum_55.html
- https://www.onixs.biz/fix-dictionary/4.4/msgType_D_68.html
- https://www.onixs.biz/fix-dictionary/5.0.sp2/tagnum_1007.html
- https://quickfixengine.org/n/documentation/configuration.html

**Community / integrators**

- https://community.ctrader.com/forum/fix-api/22265/
- https://community.ctrader.com/forum/fix-api/12218/
- https://community.ctrader.com/forum/fix-api/41846/
- https://community.ctrader.com/forum/fix-api/37414/
- https://sharptrader.arbitragesoftware.net/Adding_cTrader_Session
- https://hftforexcopier.com/ctrader-trade-copier/

**Local**

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§FIX Engine, 26–31, 67–73
- `D:\Prop\reports\swarm\20260818\A28_phases_gates.md` rule 13 / `[DO NOT]` guessed tag 55
