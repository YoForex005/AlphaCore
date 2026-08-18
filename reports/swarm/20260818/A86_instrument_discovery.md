# A86 — Instrument discovery: SecurityList request flow (never hardcode tag 55)

| Field | Value |
|---|---|
| Agent | A86 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A86_instrument_discovery.md` |
| Product source modified | **No** |
| Binding architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§16** (XAUUSD symbol normalization) and **§30** (instrument discovery on cTrader) |
| Supporting architecture | §§1.8, 26–32, 44–45, 52, 60, 69.10, 72.13 |
| Official FIX 4.4 | OnixS / FPL `SecurityListRequest` `35=x`, `SecurityList` `35=y`, tags 55 / 146 / 320 / 322 / 559 / 560 / 393 / 893 |
| Official cTrader RoE | https://help.ctrader.com/fix/specification/ (Security List Request / Security List) |
| Official cTrader companions | https://help.ctrader.com/fix/communication-model/ · https://help.ctrader.com/fix/getting-credentials/ · https://help.ctrader.com/fix/faqs/ · https://help.ctrader.com/fix/FIX44-CSERVER.xml |
| Sibling swarm notes | A05, A20, A25, A27, A28, A30, A32, A34, A36, A44, A57 |

**One-line law:** never put a ticker, a RoE sample ID, another broker’s ID, or a remembered Pepperstone ID into FIX tag `55`. Discover the venue’s numeric instrument ID with `35=x` / `35=y`, persist it, then reuse only that persisted value.

---

## 0. Verdict

cTrader overloads official FIX 4.4 `Symbol(55)` from a **human ticker** into a **Spotware numeric instrument ID**. Architecture §16 and §30 already forbid treating `"XAUUSD"` as tag 55 and forbid hardcoding an ID from another account or broker. Official Spotware text independently forbids the same thing: IDs **differ across brokers**; a wrong ID quotes or trades the **wrong instrument**.

**Measured product state (2026-08-18):** discovery is **MISSING**. There is no `SecurityListRequest` builder, no repeating-group parser, no `destination_symbols` entity/table, no persist-from-`35=y` job. The only SecurityList artifact in product source is a test harness that **hardcodes `55=123456`** — that pattern is forbidden in production and must not leak into mapping seed data.

§69.10 (discover Pepperstone XAUUSD instrument ID) is **not accepted**.

---

## 1. Architecture pins (verbatim intent)

### 1.1 §16 — XAUUSD symbol normalization

Source brokers may expose `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`. The execution venue may use a **numeric cTrader instrument ID**.

Canonical identity is one instrument:

```text
CanonicalInstrument
  XAUUSD
```

Two mapping directions, never collapsed:

```text
broker/source symbol     → canonical XAUUSD
cTrader instrument ID    → canonical XAUUSD
```

Binding sentences from §16:

- Never assume FIX tag 55 is the string `"XAUUSD"`.
- For cTrader, retrieve the Security List and map the returned symbol/instrument ID to the canonical symbol.
- Persist this mapping.

Source-side aliasing (`GOLD`, `XAUUSDm`, …) is **A44**. This note owns the **destination** half: SecurityList request → persist numeric `55`.

### 1.2 §30 — Instrument discovery on cTrader

On startup:

```text
TRADE/QUOTE session active
        ↓
Security List Request
        ↓
Security List response
        ↓
find XAUUSD instrument
        ↓
persist:
    cTrader instrument ID
    symbol name
    precision/digits
```

Binding sentence from §30:

> Do not hardcode an instrument ID from another cTrader account or broker.

Related law:

| Pin | Text |
|---|---|
| §29 | SecurityListRequest / SecurityList are part of the **minimum** FIX surface. “Send market order” is not a complete adapter. |
| §31 | QUOTE book is keyed by **symbol ID**, not by the ticker string. |
| §44 / A20 | Persist in `destination_symbols` unique `(venue_id, instrument_id)`. |
| §69.10 | First useful version requires discovering the Pepperstone XAUUSD instrument ID. |
| §72.13 | Discover cTrader symbols/instrument IDs; **do not guess**. |
| A28 rule 13 / `[DO NOT]` | Guessed cTrader symbol / instrument IDs (tag 55). |

---

## 2. Official FIX 4.4 vs cTrader RoE (do not implement generic 4.4)

### 2.1 Generic FIX 4.4 (FPL / OnixS)

`SecurityListRequest` (`MsgType=x`) “return a list of securities from the counterparty that match criteria provided on the request.”

Required body:

| Tag | Name | FIX 4.4 | Meaning |
|---|---|---|---|
| 320 | `SecurityReqID` | **Y** | Unique ID of this security request (same family as Security Definition Request). |
| 559 | `SecurityListRequestType` | **Y** | Criteria of the request. |

Official `559` values (FIX 4.4):

| 559 | Meaning |
|---|---|
| 0 | `Symbol(55)` |
| 1 | `SecurityType(167)` and/or `CFICode(461)` |
| 2 | `Product(460)` |
| 3 | `TradingSessionID(336)` |
| **4** | **All Securities** |

Optional on generic 4.4 request (not used by cTrader RoE): full `<Instrument>` block, `InstrumentExtension`, `FinancingDetails`, underlyings, legs, `Currency(15)`, `Text(58)`, `TradingSessionID(336)`, `SubscriptionRequestType(263)`.

`SecurityList` (`MsgType=y`) returns the match set. Required:

| Tag | Name | FIX 4.4 |
|---|---|---|
| 320 | `SecurityReqID` | **Y** — echoes the request |
| 322 | `SecurityResponseID` | **Y** — unique response id |
| 560 | `SecurityRequestResult` | **Y** |

Official `560` values (FIX 4.4; **identical** to cTrader RoE):

| 560 | Meaning |
|---|---|
| 0 | Valid request |
| 1 | Invalid or unsupported request |
| 2 | No instruments found that match selection criteria |
| 3 | Not authorized to retrieve instrument data |
| 4 | Instrument data temporarily unavailable |
| 5 | Request for instrument data not supported |

Repeating instruments: `NoRelatedSym(146)` then one `<Instrument>` block per symbol. Generic Instrument’s human ticker is `Symbol(55)` type **String** — “common, human understood representation.” Numeric / CUSIP / ISIN identity is supposed to live in `SecurityID(48)` + `SecurityIDSource(22)`. Fragmentation uses `TotNoRelatedSym(393)` + `LastFragment(893)`.

**If you implement generic 4.4 literally you will do the wrong thing on cTrader:**

1. Send `559=4` (“All Securities”) — **unsupported** on cServer.
2. Send `55=XAUUSD` as the ticker — cServer rejects (`Expected numeric symbolId`).
3. Expect `48`/`22` — **absent** from `FIX44-CSERVER.xml`.
4. Expect `1007`/`1008` as unknown UDFs — they are **custom tags < 5000**; a stock dictionary drops or session-rejects them (A36).

### 2.2 cTrader RoE Security List Request (`35=x`)

Official table (https://help.ctrader.com/fix/specification/ , fetched 2026-08-18):

| Tag | Field | Required | Format | Official comment |
|---|---|---|---|---|
| | Standard Header | Yes | | `57=TRADE` in every published example |
| 320 | `SecurityReqID` | **Yes** | String | “A unique ID of the Security Definition Request.” |
| 559 | `SecurityListRequestType` | **Yes** | Integer | **Supported only `0` = Symbol (tag=55).** |
| 55 | `Symbol` | No | **Integer** | “An ID for resolving the symbol name.” |
| | Standard Trailer | Yes | | |

Official **targeted** request (resolves a **known** id → name):

```
8=FIX.4.4|9=107|35=x|34=3|49=live.theBroker.12345|50=Trade|52=20180427-12:24:27.106|56=CSERVER|57=TRADE|55=39|320=ILCea0JkdQEm|559=0|10=248|
```

That request already **knows** `55=39`. It cannot discover XAUUSD the first time.

**Full-book request (required for first discovery):** same message, **`55` omitted**, `559=0`. Official full response uses `146=143`. A34: omitting 55 is how the published full list is obtained.

Do **not** send `559=4`. Do **not** send `559=1/2/3`. Do **not** put a ticker in 55 “because 559=0 means Symbol.” On this dialect, `559=0` is the **only** legal type, and tag 55 is an **optional numeric filter**, not a name.

### 2.3 cTrader RoE Security List (`35=y`)

| Tag | Field | Required | Format | Official comment |
|---|---|---|---|---|
| 320 | `SecurityReqID` | Yes | String | Echo of the request |
| 322 | `SecurityResponseID` | Yes | String | Unique response id (published examples: `responce:<SecurityReqID>` — typo is the server’s) |
| 560 | `SecurityRequestResult` | Yes | Integer | 0–5, same enum as FIX 4.4 |
| 146 | `NoRelatedSym` | No | Integer | Number of repeating instruments |
| 55 | `Symbol` | No | **Integer** | “Instrument identifiers are provided by Spotware.” |
| **1007** | **`SymbolName`** | No | String | “A symbol name.” **Not in FIX 4.4.** |
| **1008** | **`SymbolDigits`** | No | Integer | “Symbol digits. Possible values from `0` to `5`.” **Not in FIX 4.4.** |

Official single-symbol response:

```
8=FIX.4.4|9=158|35=y|34=3|49=CSERVER|50=TRADE|52=20180427-12:24:27.107|56=live.theBroker.12345|57=Trade|320=ILCea0JkdQEm|322=responce:ILCea0JkdQEm|560=0|146=1|55=39|1007=NZDCHF|1008=4|10=088|
```

Official full-book excerpt (`146=143`):

```
55=1|1007=EURUSD|1008=5
55=2|1007=GBPUSD|1008=5
…
55=17|1007=EURCAD|1008=4
55=10001|1007=USDCFDSAX|1008=5
55=18|1007=AUDCAD|1008=4
55=10002|1007=CD3295|1008=5
…
55=39|1007=NZDCHF     (from the targeted example)
```

Observations that follow **only** from the official sample (do not generalise IDs):

- `55=1` is EURUSD **in this sample book only**. It is not a universal majors table and it is **not** XAUUSD.
- IDs jump (`17` → `10001`). Synthetics appear (`CS6407_01_EURUSD`, `CD3295`).
- Human name is **always** `1007`. Tag 55 never carries `"EURUSD"`.
- `1008` is price decimal digits, not contract size and not an ID.

### 2.4 Why tag 55 is not a ticker (official, three places)

1. **RoE field comments** on Market Data Request / Snapshot / Incremental, New Order Single, Execution Report, and Security List: “Instrument identifiers are provided by Spotware.” Type is **Long** on trading/MD, **Integer** on Security List. Same identifier; inconsistent declared type. Persist as `bigint` ≥ 1.
2. **Communication model** (verbatim intent): if `Symbol(55)` is used toward cTrader/cServer, you must specify the **FIX symbol ID**. That value **can be different across brokers**. Manual lookup: that broker’s cTrader Symbol information window.
3. **Get credentials** (verbatim): “FIX symbol ID **may differ across brokers** so it is important to check before connecting to a new broker using FIX API, otherwise you may be trading or receiving prices for the **wrong symbols**.”
4. **RoE reject example** (Market Data Request Reject `35=Y`): `58=INVALID_REQUEST: Expected numeric symbolId, but got CS8260` / `281=0` (unknown symbol).

Generic FIX 4.4 OnixS `Symbol(55)`: type **String**, “ticker symbol. Common, human understood representation.” That definition is **wrong for this venue**.

cTrader does **not** define tag `48` / `22`. There is no alternate ID path. Discovery is Security List or a manual ASP “FIX symbol ID” (operator override, still verified against `35=y`).

---

## 3. Why hardcoding is forbidden (not style)

| Hardcoded source | Why it fails |
|---|---|
| `"XAUUSD"` / `"GOLD"` in tag 55 | Official reject: expected **numeric** symbolId. |
| RoE sample `55=1` | That sample’s `1` is **EURUSD**. Using it on Pepperstone would quote/trade EURUSD or reject. |
| RoE sample `55=39` | That sample’s `39` is **NZDCHF**. |
| Another cTrader broker’s XAU id | Official: IDs differ across brokers. Wrong book. |
| Another Pepperstone **account** or **environment** (demo vs live) | Architecture §30: do not hardcode an ID from another account. Treat demo/live as different venues. |
| Yesterday’s remembered Pepperstone id, never refreshed | Books change. Refresh on session up. Stale id → wrong instrument or `35=Y`. |
| Community “GBPUSD is 2” tables | Sample-book folklore. Not a contract. |
| `FixSimulationHarness` `55=123456` | Legal as a **fixture id inside a test**, illegal as a production default or seed. |

Wrong ID is not a mapping cosmetic. Official credentials page: you may be **trading or receiving prices for the wrong symbols**. That is a live-account safety defect.

---

## 4. SecurityList request flow (binding)

Three identities stay distinct (A44 §3). This flow only produces the third.

| Identity | Example | Never write to |
|---|---|---|
| `CanonicalInstrument` | `XAUUSD` | FIX tag 55 |
| Source MT5 symbol | `XAUUSDm` | FIX tag 55 |
| Destination instrument ID | Spotware `long` from this venue’s `35=y` | a different venue row |

### 4.1 Preconditions

1. The chosen FIX session is `LOGGED_ON` (architecture §30: “TRADE/QUOTE session active”).
2. Feature flags allow application messages on that session (`CTRADER_FIX_QUOTE_ENABLED` / TRADE equivalent). Diagnostic-logon-only may run SecurityList then stop (A25 §6.6).
3. `REAL_COPY_EXECUTION_ENABLED` stays **false**. Discovery does not send `35=D`.
4. Data dictionary is **`FIX44-CSERVER.xml`**, not stock FIX 4.4 (A36). Otherwise `1007`/`1008` are lost.
5. `SecurityReqID` is unique in the same family as `ClOrdID` / `MDReqID` (A05). Persist the outbound request **before** send (same crash rule as orders: know what you asked).

### 4.2 Session placement (honest conflict — prove on Pepperstone)

| Source | What it says |
|---|---|
| Official RoE examples | Every published `35=x` / `35=y` is on **TRADE** (`57=TRADE` outbound, `50=TRADE` inbound). |
| Architecture §30 | “TRADE/QUOTE session active” — either session may be the one that is up. |
| A25 §2.6 | SecurityList **allowed on both**; QUOTE “preferred for discovery” because Phase 4 is QUOTE-first. |
| A30 Increment 7 | First useful version is **QUOTE only**; includes `SecurityListClient` and forbids creating `CTraderTradeSession`. |
| A32 catalog | Security List Request/List listed on **TRADE**. |
| A44 §6.3 | Tightened to **TRADE only**; “Do not discover on QUOTE.” |
| A36 residual unknown | Whether Security List is legal on QUOTE is **unproven**. Official examples are TRADE. |

**A86 rule (do not silently pick a side in code until measured):**

1. **Contract to implement first:** send `35=x` on **TRADE** when TRADE is logged on. That is the only officially exemplified path.
2. **Phase-4 / I7 constraint:** first useful version currently has no TRADE session. If QUOTE-only discovery is required to hit §69.10 before Phase 7, treat QUOTE `35=x` as a **measured experiment**: one diagnostic Logon, one request, record `560` / reject / silence. Do not assume success from A25’s “preferred.”
3. If QUOTE `35=x` is rejected, unsupported (`560=1/5`), or silently dropped (FAQ #4), **do not invent an ID**. Either enable a **read-only** TRADE session for SecurityList (still `REAL_COPY_EXECUTION_ENABLED=false`, no `35=D`) or fail venue health `XAU mapped? = false`.
4. Persist **once** into `destination_symbols`. Both sessions consume the same row. Never keep a QUOTE-local id and a TRADE-local id.

Do not send `35=V` or `35=D` until an active, non-stale, non-ambiguous XAU row exists for **this** venue.

### 4.3 Happy path (first discovery — full book)

```text
session LOGGED_ON
        ↓
allocate SecurityReqID (opaque, unique, persist request row: NotSent)
        ↓
send 35=x
     320 = SecurityReqID
     559 = 0
     55  = OMITTED          ← full book; required the first time
        ↓
await 35=y with 320 = same SecurityReqID
        ↓
if 560 ≠ 0 → DISCOVERY_FAILED (keep previous row; do not guess)
        ↓
if 560 = 0:
     parse 146 repeating groups { 55, 1007, 1008 }
     reject any 55 that is not an integer ≥ 1
     reject any 55 that equals a ticker string
        ↓
match NormalizeLookupKey(1007) against destination name catalog
     (A44 §5.1 / §6.4: XAUUSD, XAUUSD., XAUUSDM, XAUUSD.A, GOLD)
        ↓
preference:
     exact XAUUSD
     else XAUUSD.
     else XAUUSDM
     else XAUUSD.A
     else GOLD
     else >1 distinct 55 → AMBIGUOUS (persist candidates, activate none)
     else 0 matches → UNMAPPED
        ↓
upsert destination_symbols
     instrument_id  = 55
     symbol_name    = 1007 as received
     symbol_digits  = 1008 (0–5)
     venue_id       = this execution venue (Pepperstone / env / account)
     canonical      = XAUUSD only if uniquely preferred
        ↓
QUOTE may subscribe 35=V using persisted numeric 55
TRADE may later send 35=D using the same numeric 55
dashboard: XAU mapped? = true; Instrument ID = persisted 55
```

Targeted `35=x` with `55=<already-known id>` is a **name refresh**, not first discovery. First useful version **must** omit 55.

### 4.4 Failure path (fail closed)

| Observation | Action |
|---|---|
| Timeout / no `35=y` | FAQ #4: invalid FIX is silently ignored. Do not invent an ID. Retry once after validating codec/header; then `DISCOVERY_FAILED`. |
| `560=1` invalid/unsupported | Likely `559≠0` or illegal 55. Fix the request. Do not fall back to a canned id. |
| `560=2` no instruments match | Targeted 55 unknown on this book. Full-book retry if the request had 55; else UNMAPPED. |
| `560=3` not authorised | Venue health fail. Operator / credentials. |
| `560=4` temporarily unavailable | Backoff. Keep previous row if any; mark stale-check pending. |
| `560=5` request not supported | Session/role problem (e.g. QUOTE may not serve list). Do not guess. |
| `35=j` Business Message Reject | Persist 58. Fail closed. |
| `35=3` session Reject | Persist 371/58. Fail closed. |
| Parse error (missing 146, non-numeric 55, missing 1007 on all rows) | Fail closed. Dictionary or codec defect. |
| Multiple gold contracts, no unique preference winner | `AMBIGUOUS`. Activate **none**. Operator chooses; audit. |
| Previous active id absent from new list | Mark `STALE`; `XAU mapped? = false` until a new unique match is confirmed. |

Never “use last year’s Pepperstone id while discovery is down.” Stale-but-previously-confirmed may remain on the row for audit, but **must not** be written to new `35=V` / `35=D` until a successful refresh confirms it.

### 4.5 Refresh triggers

Run the full-book flow:

- every successful Logon of the discovery session
- venue / account / environment change (treat as a **new** `venue_id`; never copy ids across)
- operator “refresh instruments” action (audited)
- periodic (configurable; default once per Logon is enough for v1)

On every successful list, apply A44 §6.5 stale/id-change rules. Open destination positions stay keyed by the **old** id until recon; do not rewrite live `fix_orders.symbol`.

---

## 5. Message construction (outbound `35=x`)

Header is the same cTrader header as every other application message (A32). Discovery does not invent CompIDs.

| Tag | Value | Notes |
|---|---|---|
| 8 | `FIX.4.4` | |
| 9 | computed | |
| 35 | `x` | SecurityListRequest |
| 49 | `<Environment>.<BrokerUID>.<Login>` | e.g. `live.pepperstone.1369850` |
| 56 | issued `TargetCompID` | RoE table `CSERVER`; do not silently fold `cServer` (A25 §26) |
| 57 | `TRADE` on the exemplified path | QUOTE only if measured legal |
| 50 | originator; must be `QUOTE` if `57=QUOTE` | TRADE: free-form originator, not a second qualifier |
| 34 | next outbound seq | per session store |
| 52 | UTC | FAQ #4 |
| 320 | unique `SecurityReqID` | persist first |
| 559 | `0` | **only** supported type |
| 55 | **omit** on first/full discovery | set only to refresh a **known numeric** id |
| 10 | checksum | |

Do not send: `559=4`, `263` (security-status subscribe), `48`, `22`, `167`, `460`, `336`, ticker strings.

`SecurityReqID` uniqueness: same generator family as other client request ids. Do not reuse after persist. Correlate inbound `35=y.320` to the outstanding request; ignore or alert on unknown 320 (duplicate-connection fan-out still possible per FAQ #1 — dedupe on `322` + `320`).

---

## 6. Response parse (inbound `35=y`)

Generic `FixMessageParser` that last-wins a `Dictionary<int,string>` **cannot** parse a Security List. Official full book repeats tag `55` / `1007` / `1008` **146 times**. A last-wins map keeps only the last instrument. That is a codec defect, not a mapping defect.

Required parse behaviour:

1. Read header + `320` / `322` / `560`.
2. If `560 ≠ 0`, stop; no instrument loop.
3. Read `146`. If absent and `560=0`, treat as empty list (UNMAPPED), do not guess.
4. Walk the repeating group: each instrument is the triplet `55`, `1007`, `1008` in RoE order. Official dump is flat (`55=1|1007=EURUSD|1008=5|55=2|…`) with no nested delimiter.
5. Validate `55` as integer ≥ 1. A non-numeric `55` is a protocol violation — drop that row, alert, do not coerce.
6. Persist **all** instruments (or at least all XAU-catalog matches plus a raw dump for audit). v1 minimum: persist every row whose `1007` key is in the destination catalog, plus the selected active XAU row.
7. `1008` in `0…5`. Out-of-range digits → keep the id/name, mark quality flag, do not reject the whole list.
8. Official RoE does **not** document `393` / `893` fragmentation. If they appear, concatenate fragments until `LastFragment=Y` or fail closed. Do not map from a partial book.

Do not load a FIX 5.0+ dictionary: tag `1007` there is `SideReasonCd`, not `SymbolName`.

---

## 7. Matching `1007` → canonical XAUUSD

Reuse A44 lookup keys. Do not invent a second catalog.

```text
NormalizeLookupKey(1007): trim, ToUpperInvariant, do not strip suffixes
```

| `1007` as received | Key | Eligible XAU? |
|---|---|---|
| `XAUUSD` | `XAUUSD` | **yes** (preferred) |
| `XAUUSD.` | `XAUUSD.` | yes |
| `XAUUSDm` / `XAUUSDM` | `XAUUSDM` | yes |
| `XAUUSD.a` | `XAUUSD.A` | yes |
| `GOLD` | `GOLD` | yes (last preference) |
| `CS6407_01_XAUUSD` | `CS6407_01_XAUUSD` | **no** (not a catalog key) |
| `XAUEUR`, `XAGUSD`, `EURUSD` | as normalized | **no** |
| any string that merely contains `XAU` / `GOLD` | — | **no** |

Preference when several instruments in **one** list match: `XAUUSD` > `XAUUSD.` > `XAUUSDM` > `XAUUSD.A` > `GOLD`. Two distinct `55` at the same preference rank → `AMBIGUOUS`. Zero matches → `UNMAPPED`.

`GOLD` on the destination is a **name fallback**, not a second live copy target. Two active gold contracts is an operator problem.

---

## 8. Persistence

### 8.1 Do not seed an instrument ID

A30 Increment 7: seed `execution_venues` with Pepperstone/cServer. **Do not seed a guessed instrument ID.**

`canonical_instruments` seeds **one** row `XAUUSD`. That is a name, not a FIX id.

### 8.2 `destination_symbols` (A20 + A44)

| Column | Source |
|---|---|
| `execution_venue_id` | `execution_venues` (broker + env + account). **Not** a source `broker_id`. |
| `instrument_id` | tag 55, `bigint > 0` |
| `symbol_name` | tag 1007 as received |
| `symbol_name_key` | `NormalizeLookupKey(1007)` |
| `symbol_digits` | tag 1008, 0–5 |
| `canonical_instrument_id` | set only when uniquely preferred XAU |
| `mapping_status` | `DISCOVERED` / `CONFIRMED` / `STALE` / `AMBIGUOUS` / `UNMAPPED` / `DISCOVERY_FAILED` |
| `is_active` | at most one active XAUUSD per venue |
| `discovered_at` / `last_seen_at` | UTC |
| `security_req_id` / `security_response_id` | 320 / 322 of the list that last wrote the row |

UNIQUE `(execution_venue_id, instrument_id)`.  
Partial unique: one `is_active = true` per `(execution_venue_id, canonical_instrument_id)`.

Never copy venue A’s `instrument_id` onto venue B. Demo and live are different venues.

### 8.3 Outstanding request row (recommended)

`fix_security_list_requests` (or a `fix_session_events` subtype): `security_req_id`, session qualifier, sent_at, `560` if any, result, instrument_count. Needed to correlate and to prove we did not send `55=XAUUSD`.

### 8.4 What consumes the persisted id

| Consumer | Tag 55 / field |
|---|---|
| `35=V` Market Data Request | persisted numeric XAU id |
| inbound `35=W` / `35=X` | parse numeric; join this table |
| `35=D` NewOrderSingle | **same** persisted numeric id |
| inbound `35=8` | parse numeric; unknown id → recon alert, not “must be XAU” |
| Dashboard QUOTE card | `XAU mapped?`, instrument ID, digits |
| Risk / shadow quote snapshot | `VenueInstrumentId` must equal the active row |

CopyIntent / RiskDecision / ExecutionIntent store `canonical_symbol = XAUUSD` **and** `destination_instrument_id`. Missing the id is `QUOTE_UNAVAILABLE` / `INTENT_INCOMPLETE`, not a license to substitute `"XAUUSD"`.

---

## 9. Current measured state (do not greenwash)

| Item | Path / evidence | Status |
|---|---|---|
| Architecture §16 / §30 | v2 spec lines 699–731, 1207–1226 | written |
| Official RoE Security List | help.ctrader.com/fix/specification | written (A32) |
| `destination_symbols` entity / EF / SQL | none under `src/` | **MISSING** |
| `IDestinationInstrumentDiscovery` / `SecurityListClient` / `SecurityListSyncService` / `SecurityListJob` | none | **MISSING** |
| Repeating-group parser | `FixMessageParser` last-wins `Dictionary<int,string>` | **cannot parse `35=y`** |
| QuickFIX/n + `FIX44-CSERVER.xml` | not vendored as the live initiator path | **MISSING** (A36) |
| Seeded / hardcoded production XAU id | grep of `src/**/*.cs` | **none** (keep it that way) |
| Test harness hardcoded id | `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` `SimulateSecurityList` → `55=123456` | **fixture-only**; also defaults ER `symbol = "XAUUSD"` which is the **wrong type** for tag 55 |
| `SymbolNormalizer.RegisterVenueInstrument` | in-memory dict, empty unless caller injects | **not** discovery |
| `CanonicalInstrument` / `SourceSymbolMapping` | scaffold entities only | no dest mapping |
| §69.10 | A57 | **not accepted** |

`SymbolNormalizer.TryMapVenueInstrumentId` correctly refuses to infer a canonical from the number itself. That is the right shape. It is not a SecurityList client.

---

## 10. Implementation shape (when a later coding task is assigned)

Names already reserved by A05 / A27 / A30. Do not invent a parallel stack.

```text
src/Fix.CTrader/Messages/SecurityListClient.cs          # build 35=x; handle 35=y
src/Fix.CTrader/Mapping/DestinationSymbolMapper.cs      # 1007 → canonical
src/Application/Fix/SecurityListSyncService.cs          # persist upsert + status
apps/fix-worker/Hosting/SecurityListJob.cs              # on Logon + manual refresh
src/Domain/Venues/DestinationSymbol.cs
src/Infrastructure/Persistence/Configurations/DestinationSymbolConfiguration.cs
tests/Unit/Fix/SecurityListXauMappingTests.cs
tests/Integration/Fix/Fixtures/security_list_xau.fix    # recorded 35=y, no secrets
```

Harness fixtures may use **any** numeric id (`41`, `999001`, even `123456`) **provided**:

- the id appears only in the recorded/simulated `35=y`
- production config / seed / options have **no** default instrument id
- tests assert `Builds_SecurityListRequest_omits_tag55_on_full_book`
- tests assert `Rejects_hardcoded_tag55_XAUUSD_assumption`
- tests assert last-wins parse of two `55` tags **fails** the acceptance bar (group parser required)

Manual ASP “FIX symbol ID” is an **operator override** with audit. It still must match a subsequent `35=y` row or the venue stays unmapped for send.

---

## 11. Anti-patterns (review checklist)

1. **Do not** send `55=XAUUSD` on `35=x`, `35=V`, or `35=D`.
2. **Do not** send `559=4` because generic FIX 4.4 calls that “All Securities.”
3. **Do not** send `559=0` **and** `55=XAUUSD` “to ask for gold by name.” Tag 55 is an id filter; a name there is invalid.
4. **Do not** hardcode Pepperstone / RoE / community IDs in options, constants, or migrations.
5. **Do not** copy demo → live or broker A → broker B.
6. **Do not** subscribe market data until the persisted active row exists.
7. **Do not** parse `35=y` with a last-wins tag map.
8. **Do not** load generic `FIX44.xml` or a FIX 5 dictionary (1007 collision).
9. **Do not** treat `55=1` as EURUSD (or anything) on Pepperstone.
10. **Do not** substring-match `1007` for `XAU` / `GOLD`.
11. **Do not** activate two gold contracts.
12. **Do not** use discovery failure as a reason to open a second FIX connection (FAQ #1 duplicates reports).
13. **Do not** put the FIX password in SecurityList logs. Redact 554 everywhere; 320/55/1007/1008 are fine to log.
14. **Do not** implement this as an MT5 callback side effect (§32). Discovery is a FIX-session job.

---

## 12. Tests required (A10 / A27 / A30) — none exist yet

| Test | Asserts |
|---|---|
| `Builds_SecurityListRequest` | `35=x`, `559=0`, unique `320`, **no tag 55** on full book |
| `Builds_targeted_SecurityListRequest_only_with_numeric_55` | `55` integer ≥ 1; rejects `"XAUUSD"` |
| `Parses_official_single_symbol_y` | RoE `55=39` / `1007=NZDCHF` / `1008=4` / `560=0` |
| `Parses_official_full_book_repeating_group` | `146=143` excerpt; **not** last-wins |
| `Maps_1007_XAUUSD_to_canonical` | preferred key wins |
| `Ambiguous_two_gold_ids_activates_none` | |
| `Rejects_hardcoded_tag55_XAUUSD_assumption` | builder + mapper |
| `Does_not_seed_instrument_id` | venue seed has no `instrument_id` |
| `Discovery_failed_does_not_guess` | `560=4` / timeout → no new active row |
| `Harness.SecurityListXauDiscoveryTests` | replay recorded list → persist Pepperstone XAU id (**from the fixture**, not a constant used by production) |

Do **not** use the live Pepperstone account as the first integration test (architecture §61).

---

## 13. Source map

| Claim | Where it is official or binding |
|---|---|
| Never assume tag 55 is `"XAUUSD"`; persist Security List mapping | Architecture §16 |
| Startup flow Request → List → find XAU → persist id/name/digits | Architecture §30 |
| Do not hardcode an ID from another account or broker | Architecture §30 |
| Discover; do not guess | Architecture §72.13, A28 rule 13 |
| `35=x` / `35=y` exist in FIX 4.4; 320/559 required; 560 result enum | OnixS FIX 4.4 `msgType_x_120`, `msgType_y_121`, `tagNum_559`, `tagNum_560` |
| Generic `55` is a **ticker string** | OnixS FIX 4.4 `tagNum_55` |
| Generic “all securities” is `559=4` | OnixS `tagNum_559` |
| cTrader supports **only** `559=0` | RoE Security List Request |
| cTrader `55` is Spotware numeric id; name is `1007`; digits `1008` | RoE Security List |
| Official examples are TRADE; targeted `55=39` → `NZDCHF`; full book `146=143` | RoE examples |
| IDs differ across brokers; wrong ID = wrong symbol | Credentials page + communication model |
| `Expected numeric symbolId` | RoE `35=Y` example |
| Invalid FIX may get no response | FAQ #4 |
| Custom 1007/1008 require `FIX44-CSERVER.xml` | A36 + official dictionary |

---

## 14. Residual unknowns (honest)

| Item | Status |
|---|---|
| Is `35=x` accepted on the Pepperstone **QUOTE** session? | **Unproven.** Official examples are TRADE. Measure before baking QUOTE-only discovery into I7. |
| Does cServer fragment large books (`393`/`893`)? | Not in RoE. Official sample is one `35=y`. Handle if seen; do not require. |
| Is the XAU contract named `XAUUSD`, `GOLD`, or a suffix on Pepperstone? | Unknown until a recorded Security List exists. Matcher catalog is ready; do not guess the id. |
| Are instrument IDs stable across symbol-list edits on the **same** account? | Officially specified only as differing **across brokers**. Refresh on Logon; treat change as STALE/id-swap (A44 §6.5). |
| Tag 55 Long vs Integer | Same id; persist `bigint`. |

---

## 15. Bottom line

Architecture §16 says the venue identity is a **numeric cTrader instrument ID** mapped onto canonical `XAUUSD`, not the string `"XAUUSD"` in tag 55. Architecture §30 says the only legal way to obtain that id is **Security List Request → Security List → persist**. Official FIX 4.4 gives the message types and the `560` result enum; official cTrader RoE **rewrites** the payload: `559` only `0`, tag 55 is a Spotware integer, the ticker lives in custom `1007`, digits in custom `1008`, and IDs are **per broker**.

Therefore:

1. Build `35=x` with `559=0` and **omit** `55` for first discovery.
2. Parse `35=y` as a **repeating group**, not a last-wins map.
3. Activate at most one XAU row per venue from `1007` catalog match.
4. Every later `35=V` / `35=D` uses that persisted number.
5. If discovery fails, the venue is unmapped. **Never hardcode an instrument ID.**
)