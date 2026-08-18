# P500_S048 — No SecurityList on the wire means no legal tag 55

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S048_security_list.md` |
| Agent | P500_S048 (SecurityList / tag 55 legality) |
| Date | 2026-08-18 |
| Assigned | Read `CTraderQuoteService` and A86 if present. Pin: **no SecurityList on the wire ⇒ no legal tag 55**. Sending a guessed `"XAUUSD"` string would **reject** or hit the **wrong contract**. Do not edit product. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **No.** |
| Binding siblings | `A86_instrument_discovery.md`, `A32_ctrader_fix_specification.md`, `A34_ctrader_fix_faq.md`, `P500_S002_fix_no_send.md`, `P500_S008_no_quote_tape.md`, `D96_id.md` |
| Binding architecture | v2 **§16** (never assume tag 55 is `"XAUUSD"`), **§30** (Request → List → persist), **§31** (book keyed by symbol ID), **§69.10**, **§72.13** (do not guess) |
| Official RoE | https://help.ctrader.com/fix/specification/ (Security List Request `35=x` / Security List `35=y`); credentials page: IDs **differ across brokers** |

**Honesty rule:** a comment that says “discover instruments via SecurityList” is not a `35=x` on `*.c-trader.com`. An in-memory `_xauInstrumentId` that nobody sets from the socket is not a legal id. A harness `55=123456` is a **test token**, not Pepperstone gold. Do not tick §69.10 from this file.

---

## 0. Verdict (binding)

**CONFIRMED. There is no SecurityList on the wire. Therefore there is no legal FIX tag 55 for this venue.**

| Claim | Result | Class |
|---|---|---|
| Outbound application MsgType on the live socket | **`35=A` Logon only** (`CTraderFixSession.BuildLogon`) | `EXISTS_AND_BOUNDED` |
| Outbound `35=x` SecurityListRequest | **0 bytes** | `MISSING` |
| Inbound `35=y` SecurityList consumed from TLS | **never read as y** (one `ReadAsync` of the Logon reply, then dispose) | `MISSING` |
| Persisted `destination_symbols` / active XAU row | **no entity, no table write** | `MISSING` |
| Legal numeric tag 55 for `35=V` / `35=D` | **does not exist** | `UNMAPPED` |
| Guess `"XAUUSD"` into tag 55 | RoE reject: **Expected numeric symbolId** | **FORBID** |
| Guess a remembered / RoE / harness number | Official: **wrong instrument** (IDs differ across brokers) | **FORBID** |
| `CTraderQuoteService` on the wire | **zero callers**; tag-list only; `BuildSecurityListRequestTags` emits **`35=y`** (response type) | unused / wrong MsgType |
| §69.10 (discover Pepperstone XAU id) | **not accepted** | same as A86 §0 |

One-line:

```text
NO 35=x on the wire → NO 35=y book → NO legal 55.
Do not send 55=XAUUSD (reject). Do not send a guessed number (wrong contract).
Venue stays UNMAPPED until a measured full-book 35=y is persisted.
```

User wants quotes and (later) send. Both require tag 55. Tag 55 on this dialect is **not** the ticker. The only legal source is this venue’s `35=y` repeating group, field `55` (Spotware integer) matched via custom `1007` (name). That message has never been requested or stored. Fail closed.

---

## 1. What was actually read

| Path | Role | Used on the socket? |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | In-memory mapper: `OnSecurityListResponse` / `BuildSecurityListRequestTags` / `BuildMarketDataRequestTags` | **No.** Zero product callers (`grep` of `*.cs` hits only this file). |
| `D:\Prop\reports\swarm\20260818\A86_instrument_discovery.md` | Binding discovery law (never hardcode tag 55) | report only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **Only** TCP/TLS writer | Yes — **Logon only** |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | One-shot QUOTE then TRADE Logon; `RealCopyEnabled := false` | Yes — Logon host |
| `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` | Last-wins `Dictionary<int,string>` | unit tests only |
| `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` `SimulateSecurityList` | Fixture `55=123456` + `1007=XAUUSD` | **not** the venue |
| `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` | Venue map starts **empty**; `TryMapVenueInstrumentId` does not infer from the number | not discovery |
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | `VenueInstrumentId` nullable string | never filled from `35=y` |

Product source was not modified.

---

## 2. Why “no SecurityList on the wire” is measured, not rhetorical

`CTraderFixSession.TryLogonAsync` is the only live writer:

1. `TcpClient` + `SslStream` to host/port.
2. `BuildLogon` fields: `35=A`, `34`, `49`, `56`, `50`, `57`, `52`, `98=0`, `108=30`, `141=Y`, `553`, `554`.
3. One `WriteAsync` of that Logon.
4. One `ReadAsync` (4 KiB) to classify the Logon reply (`35=A` vs reject).
5. Dispose. **No heartbeat loop. No `35=x`. No second write.**

`CTraderFixLogonHostedService` calls that twice (QUOTE `:5211`, TRADE `:5212`) and persists `FixSessionState` status. It never constructs `CTraderQuoteService`. It never sends `559`. It never looks at tag `1007`.

Therefore: even a successful dual Logon leaves the process with **zero** instrument rows from cServer. Logged-on ≠ discovered.

---

## 3. `CTraderQuoteService` — what it does and what it does **not** do

File: `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` (133 lines). Class comment claims four QUOTE jobs: discover via SecurityList, identify XAU numeric id, subscribe MD, keep bid/ask and reject stale. Implementation is an **in-memory sketch**.

### 3.1 The only legal ingest path in this class

`OnSecurityListResponse(IEnumerable<IReadOnlyDictionary<int,string>>)`:

- Walks **already-split** instrument dictionaries (caller must have parsed the repeating group).
- Matches **only** `1007 == "XAUUSD"` (ordinal ignore-case). No A44 catalog (`XAUUSD.`, `XAUUSDM`, `GOLD`, …).
- Requires tag `55` present and `long.TryParse` (InvariantCulture integer). A ticker in 55 throws `FormatException`.
- Stores the first match in `_xauInstrumentId` and returns.
- Zero matches → `InvalidOperationException("SecurityList did not contain XAUUSD.")`.

That last throw is the correct **fail-closed** shape **if** a real `35=y` is fed in. Nothing in product feeds it. There is no session, no `35=y` handler, no `destination_symbols` upsert.

### 3.2 The “request builder” is the wrong MsgType

```111:114:D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs
    public IReadOnlyList<KeyValuePair<int, string>> BuildSecurityListRequestTags()
    {
        return new List<KeyValuePair<int, string>> { new(35, "y") };
    }
```

A86 / RoE request is **`35=x`**, body **`320` + `559=0`**, tag **55 omitted** on first/full discovery. This helper emits **`35=y`** (the **response** type) and nothing else. Even if a later task wired it to `SslStream.WriteAsync`, cServer would not treat it as a SecurityListRequest. It is not a discovery client.

### 3.3 MD tags only after a resolved id

`BuildMarketDataRequestTags` throws if `_xauInstrumentId` is unset, then emits `35=V`, `55=<numeric>`, `263=1`, `264=1`. That is the **right type** for tag 55 (digits, not `"XAUUSD"`) — and it is **unreachable** from the socket because nothing calls `OnSecurityListResponse` with a live list. Also: tags `1320`/`1321` used in `TryAcceptMarketDataSnapshot` are **not** RoE MD (`269`/`270`). Harness-only.

### 3.4 Call graph

Product-tree `grep` for `CTraderQuoteService` / `OnSecurityListResponse` / `BuildSecurityListRequestTags`: **this file only**. Not registered in DI. Not referenced by `apps/fix-worker`. Unused.

---

## 4. A86 law (do not re-litigate)

A86 one-line (verbatim intent): **never** put a ticker, a RoE sample ID, another broker’s ID, or a remembered Pepperstone ID into FIX tag `55`. Discover the venue’s numeric instrument ID with `35=x` / `35=y`, persist it, then reuse only that persisted value.

cTrader **overloads** official FIX 4.4 `Symbol(55)` from a human ticker into a **Spotware numeric instrument ID**. The human name lives in **custom tag 1007**. Digits in **custom 1008**. Generic FIX 4.4 “all securities” `559=4` is **unsupported**. RoE supports **only** `559=0`, and tag 55 on the request is an **optional numeric filter**, not a name.

Official credentials / communication-model text (A86 §2.4): if you use the wrong id you may be **trading or receiving prices for the wrong symbols**. IDs **differ across brokers**. Demo vs live is a different venue (§30).

### 4.1 What a guessed `"XAUUSD"` does

| Outbound | If tag 55 = `"XAUUSD"` |
|---|---|
| `35=x` (targeted list) | Invalid. 55 must be Integer. Silent drop (FAQ #4) or `560=1` / session reject. |
| `35=V` MarketDataRequest | RoE `35=Y` example: `58=INVALID_REQUEST: Expected numeric symbolId, but got CS8260` / `281=0`. Same class of reject for a ticker. **No tape.** |
| `35=D` NewOrderSingle | Same type error, or (worse, if a future codec coerced it) a book that is not gold. |

There is **no** “ask for gold by name” request on this dialect. `559=0` + `55=XAUUSD` is **not** “symbol = XAUUSD”. It is “filter to instrument id `XAUUSD`”, which is not an id.

### 4.2 What a guessed **number** does (the worse failure)

| Guess | Official / A86 meaning |
|---|---|
| RoE sample `55=1` | That sample’s **EURUSD**. Quote/trade EURUSD on Pepperstone, or reject. |
| RoE sample `55=39` | That sample’s **NZDCHF**. |
| Harness `55=123456` | Test token (`FixSimulationHarness.SimulateSecurityList`). **Not** a live id. D96: do not copy into seeder / options / `destination_quotes`. |
| Another broker’s XAU id | Official: IDs differ. Wrong book. |
| Yesterday’s remembered Pepperstone id, never refreshed | Books change. Stale id → wrong instrument or `35=Y`. |
| Community “GBPUSD is 2” tables | Sample-book folklore. |

A **reject** is the lucky outcome. The unlucky outcome is a **filled** `35=8` on the wrong contract. That is a live-account safety defect, not a mapping cosmetic.

---

## 5. Parser cannot produce a legal book even if `35=y` arrived

`FixMessageParser.Parse` last-wins into `Dictionary<int,string>` (`tags[tag] = val`). Official full book repeats `55` / `1007` / `1008` **146 times** (RoE `146=143` example). A last-wins map keeps **only the last instrument**. A86 §6: that is a **codec defect**. `CTraderQuoteService.OnSecurityListResponse` is shaped for `IEnumerable<IReadOnlyDictionary<…>>` (one dict per instrument) — which **this parser does not emit**.

So the stack, if naively wired tomorrow, would still have **no legal tag 55**:

```text
socket 35=y (full book)
    → FixMessageParser (last-wins) → one leftover 55/1007
    → OnSecurityListResponse sees a single entry, maybe not XAU
    → throw "SecurityList did not contain XAUUSD"
    → _xauInstrumentId stays null
    → BuildMarketDataRequestTags throws
```

Do not “fix” that by stuffing `"XAUUSD"` into tag 55.

---

## 6. Persistence gap — nothing to reuse

A86 §8 requires `destination_symbols` unique `(execution_venue_id, instrument_id)`, `symbol_name` from `1007`, `symbol_digits` from `1008`, at most one active XAU per venue. **No** `DestinationSymbol` entity under `src/`. `DestinationQuoteSnapshot.VenueInstrumentId` is a nullable string with no SecurityList writer. `SymbolNormalizer._venueIdToCanonical` starts empty; `TryMapVenueInstrumentId` **refuses** to infer a canonical from the digits themselves (correct). `RegisterVenueInstrument` is a test injection point, not a `35=y` client.

Without a persisted row there is nothing for `35=V` or a future `35=D` to copy. Missing id is `QUOTE_UNAVAILABLE` / `INTENT_INCOMPLETE`, **not** a license to substitute the ticker.

---

## 7. Legal vs illegal tag 55 (review checklist)

| Source of the number / string | Legal on `35=V` / `35=D`? |
|---|---|
| This venue’s latest `35=y` row, `560=0`, `1007` uniquely preferred as XAU, persisted, refresh-confirmed | **Yes** — the only yes |
| Operator ASP “FIX symbol ID” that **matches** a subsequent `35=y` row (audited override) | Conditional yes (A86 §10) |
| `"XAUUSD"` / `"GOLD"` / source alias `XAUUSDm` | **No** — type reject or nonsense filter |
| RoE sample ids (`1`, `39`, …) | **No** — those names are EURUSD / NZDCHF in the **sample** book |
| `FixSimulationHarness` `123456` | **No** in production / seed / options |
| Empty / omitted on `35=V` or `35=D` | **No** — MD and NOS require the id |
| Stale previous row after a failed refresh | **No** for new send (A86 §4.4). Audit may keep the old number; do not write it. |

First-discovery request (`35=x`): **omit** tag 55. That omission is not “no id forever”; it is how the full book is obtained. The **response** is what creates the legal id.

---

## 8. Session placement (do not silently pick QUOTE)

A86 §4.2 conflict remains:

- Every published RoE `35=x` / `35=y` is on **TRADE**.
- Architecture Phase 4 / I7 is QUOTE-first; QUOTE `35=x` is **unproven**.
- If QUOTE list is rejected (`560=1/5`), dropped, or silent: **do not invent an id**. Either a read-only TRADE discovery session (`REAL_COPY_EXECUTION_ENABLED` still false, no `35=D`) or venue health `XAU mapped? = false`.

P500_S048 does **not** authorize a QUOTE-only discovery implementation. Measure first.

---

## 9. What must exist before any later coding task writes tag 55

Minimum conjunction (A86 §§4–8, 11). This agent does **not** implement it.

1. Logged-on session that has been **measured** to accept `35=x`.
2. Builder: `35=x`, unique `320`, `559=0`, **no tag 55** on full book. **Never** `559=4`. **Never** `55=XAUUSD`.
3. Repeating-group parser (not last-wins dict). Reject non-numeric 55 rows.
4. Match `NormalizeLookupKey(1007)` against the destination catalog; activate **at most one** XAU; `AMBIGUOUS` activates none.
5. Persist `destination_symbols` for **this** `venue_id` (broker + env + account). Do not seed an instrument id.
6. Only then may `35=V` carry that persisted number. `35=D` is a **later** gate (P500_S002: no sender today; do not add one from this note).

If any step fails: **UNMAPPED**. Fail closed.

---

## 10. Anti-patterns this slot exists to block

1. “We know it’s gold, just send `55=XAUUSD`.” → reject / invalid FIX.
2. “Use the RoE example `55=1` until Pepperstone answers.” → EURUSD in the sample book.
3. “Copy `123456` from the harness into appsettings.” → forged discovery (D96).
4. “Logon succeeded, so we can subscribe.” → Logon is not a book.
5. “`CTraderQuoteService` already discovers.” → unused; request MsgType is `y`.
6. “Parser returned a 55, ship it.” → last-wins of 143 instruments is not XAU.
7. “Discovery failed, keep last year’s id on new `35=V`.” → A86 forbids.
8. Tick §69.10 from a comment or this markdown.

---

## 11. Residual unknowns (honest)

| Item | Status |
|---|---|
| Pepperstone QUOTE accepts `35=x`? | **Unproven** (A86 §14). |
| Pepperstone XAU `1007` spelling (`XAUUSD` vs `GOLD` vs suffix) | Unknown until a recorded list exists. Matcher catalog is ready; **do not guess the id**. |
| Would `CTraderQuoteService`’s exact-`XAUUSD` match miss a `GOLD` contract? | Yes — narrower than A44. Irrelevant until a real `35=y` is parsed as a group. |

---

## 12. Bottom line

```text
WIRE TODAY:     35=A Logon (QUOTE + TRADE), dispose.
SECURITY LIST:  not sent, not received, not persisted.
LEGAL TAG 55:   none.
ILLEGAL:        55=XAUUSD          → reject (expected numeric symbolId)
ILLEGAL:        55=<guessed long>  → wrong contract or reject
CTraderQuoteService: unused tag lists; BuildSecurityListRequestTags is 35=y (wrong).
A86 still binding. §69.10 not accepted.
```

No SecurityList on the wire means no legal tag 55. Do not send a guessed XAUUSD string. Do not send a guessed number. Product was not edited.
)
