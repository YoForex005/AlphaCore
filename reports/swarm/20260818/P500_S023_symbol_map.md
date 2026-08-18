# P500_S023 — Symbol map: wrong cTrader tag 55 is the wrong market (instant loss)

| Field | Value |
|---|---|
| Slot | **P500_S023** |
| Date | 2026-08-18 |
| Agent | P500_S023 (symbol / venue-id map) |
| Product source modified | **No.** Report only. |
| Subjects | `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` (83 lines) · `D:\Prop\docs\xauusd-normalization.md` (7 lines) |
| Supporting reads | `CTraderQuoteService.cs`, `CTraderFixSession.cs`, `CTraderFixLogonHostedService.cs`, `TradeReconstructor.cs`, `SourceSymbolMapping.cs`, `CanonicalInstrument.cs`, `DestinationQuote.cs`, `ExecutionIntent.cs`, `DemoSeeder.cs`, `DependencyInjection.cs`, `SymbolNormalizerTests.cs`, `docs/ctrader-fix.md`, architecture v2 §16 / §30, A32 / A34 / A44 / A86 / B15 / D15 |
| Angle | Wrong symbol id on cTrader (FIX tag **55** is a **numeric instrument id**, not a ticker) would send the **wrong market** — instant loss. **SecurityList is required before any send.** |

---

## Verdict

| Check | Result | Honest one-liner |
|---|---|---|
| Tag 55 treated as ticker `"XAUUSD"` in `SymbolNormalizer` | **PASS (this class)** | Venue dict starts empty; `TryMapVenueInstrumentId` never infers a ticker from a number |
| Tag 55 hardcoded from RoE / another account / harness `123456` in this class | **PASS (this class)** | No numeric IDs in `SymbolNormalizer.cs` |
| Docs pin “discover, never hardcode from another account” | **PASS (doc)** | `docs/xauusd-normalization.md` is short but correct |
| SecurityList required before any send (product path) | **FAIL** | Logon hosted service never sends `35=x`; quote service is not in DI; no send-gate keyed on a persisted venue id |
| Persisted `destination_symbols` / `(venue, instrument_id)` | **FAIL** | Table and entity **missing**. Seed quote `VenueInstrumentId = null` |
| Source aliases fail-closed / per-broker persist | **FAIL** | 12 compiled aliases + `StartsWith("XAUUSD")` heuristic; mapper never reads `source_symbol_mappings` |
| Wired: SecurityList → `RegisterVenueInstrument` → NOS tag 55 | **FAIL** | `RegisterVenueInstrument` unused in `src/` production. No `NewOrderSingle` builder. |
| A100 G04 “XAU symbol mappings verified” | **FAIL** | Same gap as B15 / D15. Do not claim verified. |
| Instant-loss if a send were armed with a guessed/wrong 55 | **TRUE** | Official RoE: `55=1` is EURUSD in the sample book. Wrong id = wrong instrument = real capital on the wrong market. |

**Capital at risk from this map today:** **NO** — only because live `35=D` is still off (`CTraderFixOptions.RealCopyExecutionEnabled` default false; hosted service logs “NewOrderSingle still disabled”; DI pins `RealCopyEnabled = false`). That is **SAFE_BY_ABSENCE**, not a verified mapping.

**Do not** put `"XAUUSD"` in tag 55. **Do not** reuse Pepperstone / RoE / harness IDs. **Do not** send until SecurityList has resolved **this account’s** numeric id and that id is the only value written to 55.

---

## 1. Binding law (why this is an instant-loss gate)

### 1.1 Architecture §16 (canonical vs venue)

```text
broker/source symbol     → canonical XAUUSD
cTrader instrument ID    → canonical XAUUSD

Never assume FIX tag 55 is the string "XAUUSD".
For cTrader, retrieve the Security List and map the returned
symbol/instrument ID to the canonical symbol.
Persist this mapping.
```

Source aliases listed in §16: `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`.

### 1.2 Architecture §30 (startup, before trade)

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

Binding sentence: **Do not hardcode an instrument ID from another cTrader account or broker.**

### 1.3 Official cTrader RoE (A32 / A34)

On cTrader FIX, tag **55 (`Symbol`) is a Spotware numeric instrument identifier**, not the human ticker.

| Tag | Name | Meaning on cTrader |
|---|---|---|
| **55** | `Symbol` | **Numeric instrument id** (`Long`). This is what MarketDataRequest and NewOrderSingle must carry. |
| **1007** | `SymbolName` | Human ticker (`XAUUSD`, `EURUSD`, …). Match here. Never send this as 55. |
| **1008** | digits / precision | Persist with the id. |

Official SecurityList example (A32):

```text
35=y|…|146=1|55=39|1007=NZDCHF|1008=4|
```

Full-book sample uses `55=1|1007=EURUSD`. Therefore:

- Sending `55=XAUUSD` is invalid (official reject family: *Expected numeric symbolId*).
- Sending `55=1` because “1 looks like a default / RoE sample” would **buy/sell EURUSD** while the book, risk, and hedge thesis are XAUUSD.
- Sending another account’s gold id (ids **differ across brokers and environments**) would hit whatever instrument that integer is **on this venue**.

That is not slippage. That is **the wrong market**. Instant, unbounded relative to the intended gold hedge.

### 1.4 Docs pin (read in full)

`D:\Prop\docs\xauusd-normalization.md` (entire file):

```1:7:D:\Prop\docs\xauusd-normalization.md
# XAUUSD normalization

Canonical code: `XAUUSD`.

Source aliases include `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `GOLD`, and dotted/pro suffixes.

cTrader instrument IDs are **discovered** via Security List and registered. They are never hardcoded from another account.
```

Correct law. Incomplete: no request/response tags, no persist table, no “block send until resolved”, no warning that 55 is numeric. Still: **never hardcode from another account.**

---

## 2. What `SymbolNormalizer` actually does

File: `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs`. Sole type under `Domain\Instruments\` besides `CanonicalInstrumentRef`.

### 2.1 Two dictionaries — do not collapse them

| Dictionary | Default | Inference |
|---|---|---|
| `_sourceToCanonical` | **12 compiled aliases** → `"XAUUSD"` | trim → exact → strip `.`/space → `StartsWith("XAUUSD")` or compact `GOLD` |
| `_venueIdToCanonical` | **empty** | **none**. Exact key only. |

```12:41:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
    private static readonly HashSet<string> DefaultXauAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "XAUUSD", "XAUUSD.", "XAUUSDM", "XAUUSD.A", "XAUUSD.I", "XAUUSD.S",
        "XAUUSD.PRO", "XAUUSDPRO", "GOLD", "GOLD.", "GOLD.A", "XAUUSDpro"
    };
    // …
        _venueIdToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (venueIdMappings is not null)
        {
            foreach (var pair in venueIdMappings)
                _venueIdToCanonical[pair.Key] = pair.Value;
        }
```

### 2.2 Venue API (the send-critical half)

```74:82:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
    public bool TryMapVenueInstrumentId(string venueInstrumentId, out string canonical) =>
        _venueIdToCanonical.TryGetValue(venueInstrumentId, out canonical!);

    public void RegisterVenueInstrument(string venueInstrumentId, string canonical)
    {
        if (string.IsNullOrWhiteSpace(venueInstrumentId))
            throw new ArgumentException("Venue instrument id is required.", nameof(venueInstrumentId));
        _venueIdToCanonical[venueInstrumentId] = canonical;
    }
```

Honest positives:

- Empty venue map on `new SymbolNormalizer()`.
- No trim / prefix / numeric-guess on venue ids.
- Whitespace id rejected on register.
- Unit test asserts the fail-closed shape:

```23:30:D:\Prop\tests\Unit\SymbolNormalizerTests.cs
    [Fact]
    public void Does_not_guess_venue_instrument_ids()
    {
        _n.TryMapVenueInstrumentId("123456", out _).Should().BeFalse();
        _n.RegisterVenueInstrument("123456", "XAUUSD");
        _n.TryMapVenueInstrumentId("123456", out var c).Should().BeTrue();
        c.Should().Be("XAUUSD");
    }
```

Honest negatives (send path):

- `RegisterVenueInstrument` is **never called** from production `src/` (only the test + this class).
- No caller injects `venueIdMappings`.
- No `BrokerId` / `VenueId` on the lookup. One process-global dict if anyone ever registers.
- Reverse map (canonical → tag 55) **does not exist**. The class cannot produce the integer a `35=D` must carry.
- `TradeReconstructor` default-constructs `new SymbolNormalizer()` and only calls `TryMapSource` (source half). Reconstruction never registers a venue id.

### 2.3 Source half — not a send, still a wrong-market feeder

```43:68:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
    public bool TryMapSource(string sourceSymbol, out string canonical)
    {
        // trim → dict → compact (strip '.' and space) → StartsWith("XAUUSD") / compact GOLD
```

`TradeReconstructor.OpenTrade.Start` then **does not fail closed**:

```220:226:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            symbols.TryMapSource(deal.SourceSymbol, out var canonical);
            var trade = new OpenTrade
            {
                // …
                CanonicalSymbol = string.IsNullOrEmpty(canonical) ? deal.SourceSymbol : canonical,
```

Unmapped source is copied through as “canonical.” Mapped-by-heuristic (`XAUEUR` starts with `XAUUSD`? No — but `XAUUSD.micro`, `XAUUSDcheck`, `XAUUSDJPY` **do**) become `XAUUSD`. If a later send used that canonical to pick “the gold instrument,” a non-XAU book could be hedged as gold — or a gold book dropped.

Compiled aliases include A44-forbidden extras: `XAUUSD.I`, `XAUUSD.S`, `XAUUSD.PRO`, `XAUUSDPRO`, `GOLD.`, `GOLD.A`. `XAUUSDpro` is a case-duplicate of `XAUUSDPRO`.

`source_symbol_mappings` exists as EF (`TraderDbContext` → table `source_symbol_mappings`). **Zero seed rows.** Mapper never reads it. `CanonicalInstrument` seed is one `XAUUSD` row only.

---

## 3. SecurityList in product — shape exists, not on the wire

### 3.1 `CTraderQuoteService` (in-memory only)

`D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` is the only production type that mentions SecurityList + tag 55 as instrument id.

Intended contract (comments):

1. Discover via SecurityList.
2. Identify numeric XAUUSD id (tag 55) by matching tag **1007** = `XAUUSD`.
3. Subscribe MD using that numeric 55.
4. Reject stale quotes.

Measured behavior:

| Member | What it does | Send-safe? |
|---|---|---|
| `_xauInstrumentId` | `long?`, starts null | yes (unresolved) |
| `IsInstrumentResolved` | `_xauInstrumentId.HasValue` | correct gate shape |
| `XauInstrumentId` | throws if unresolved | fail-closed getter |
| `OnSecurityListResponse` | first entry with `1007==XAUUSD` (ignore-case); require numeric 55 | **does not guess**; **does not persist**; **exact ticker only** (misses venue names that are not the string `XAUUSD`) |
| `BuildSecurityListRequestTags` | returns **`(35, "y")` only** | **WRONG MsgType.** Request is `35=x`. `y` is the **response**. Missing required `320` (`SecurityReqID`) and `559` (`SecurityListRequestType`). Official cTrader `559` supported value is `0` = Symbol (A32). Architecture/docs also mention type 0 = all securities — confirm against RoE before coding. |
| `BuildMarketDataRequestTags` | `35=V`, `55=<resolved long>`, `263=1`, `264=1` | correct **shape** (numeric 55) **if** id was discovered on **this** session |

`OnSecurityListResponse` will throw if XAUUSD is present without 55, if 55 is non-numeric, or if no `1007=XAUUSD`. It will **not** fall back to `"XAUUSD"` as tag 55. That part is correct.

It will also **not**:

- Parse repeating group `146`.
- Persist id / name / digits (`1008`).
- Call `SymbolNormalizer.RegisterVenueInstrument`.
- Handle fragmented SecurityList (`893`).
- Match aliases on 1007 (`GOLD`, `XAUUSD.`).
- Bind the id to `(venue, account, environment)`.

### 3.2 Session / host — SecurityList is never sent

`CTraderFixSession.TryLogonAsync` builds **only** Logon (`35=A`). It returns after the first reply. No `35=x`, no keep-alive, no application loop.

`CTraderFixLogonHostedService` calls that logon twice (QUOTE 5211, TRADE 5212), writes `LiveRuntimeStatus`, persists session **status** rows. It does **not** construct `CTraderQuoteService`. DI (`DependencyInjection.cs`) does **not** register `CTraderQuoteService`.

So: even the (buggy) request builder never runs. `IsInstrumentResolved` is never true in a live process. Seeded `DestinationQuoteSnapshot.VenueInstrumentId` is **null**.

### 3.3 Harness `55=123456` — do not seed

`FixSimulationHarness.SimulateSecurityList` hardcodes `(55, "123456")` + `(1007, "XAUUSD")` for tests. D96 already flags this: **must not leak into mapping seed**. `123456` is not a Pepperstone instrument id.

---

## 4. Instant-loss scenarios (if send were armed)

These are **not** happening now because NOS is off. They are the reason the send gate must stay closed until discovery + persist land.

| # | Bad 55 value | What cServer does | P&L effect |
|---|---|---|---|
| 1 | `"XAUUSD"` (ticker in 55) | Reject / silent drop (FAQ: invalid FIX may get no response) | Missed hedge **or** unknown state if a retry is added. Unknown + retry = double. |
| 2 | RoE sample `1` | **EURUSD** in the official full book | Gold thesis, FX fill. Instant wrong-market loss. |
| 3 | RoE sample `39` | **NZDCHF** | Same. |
| 4 | Another broker’s gold id | Whatever that integer is **here** | Wrong instrument; size/digits/contract may also be wrong → quantity blow-up on top. |
| 5 | Demo / previous-account id reused after broker migrate | Same as 4 | IDs are **not** portable. Docs already forbid this. |
| 6 | Harness `123456` copied to seed | Unknown or some live instrument | Roulette. |
| 7 | Empty / omitted 55 | Reject | No hedge; source already filled. |
| 8 | Send before SecurityList | No resolved id; any fallback is a guess | **Forbidden.** |
| 9 | MD subscribed to id A, NOS sent with id B | Quotes validate gold; fill is not gold | Pre-trade checks lie. |
| 10 | Source heuristic maps non-gold → canonical XAUUSD, then resolved gold id is sent | Fills gold against a non-gold source | Unhedged source + naked dest. |

Quantity is a second explosion: min lot / digits / contract size come from the **same** SecurityList row (`1008` and volume fields). Wrong id ⇒ wrong spec ⇒ `QuantityNormalizer` can emit a legal-looking size that is enormous on the actual instrument.

---

## 5. Required send gate (SecurityList before any send)

No `35=D`, no `35=V` that can be mistaken for live, no dest quote used for risk, until all of the following are true **for this process / this account**:

```text
QUOTE (and TRADE, per RoE session split) LoggedOn
        ↓
send SecurityListRequest  35=x
      required: 320=unique SecurityReqID
                559=official supported type (RoE: 0 = Symbol)
      never 35=y on the request
        ↓
parse SecurityList  35=y
      repeating 146: each 55 (numeric) + 1007 (name) + 1008 (digits) + volume spec
        ↓
select the row whose 1007 matches the persisted venue name for canonical XAUUSD
      (do not assume the string is always "XAUUSD")
        ↓
persist destination_symbols unique (venue_id, instrument_id)
      fields: instrument_id, symbol_name, digits, contract/volume, discovered_at, account
        ↓
RegisterVenueInstrument(instrument_id, "XAUUSD")  — in-memory cache of the persisted row
        ↓
only then:
      MD  55=<that id>
      NOS 55=<that id>
      ExecutionIntent.DestinationSymbol = that numeric id (not "XAUUSD")
```

Fail-closed rules:

1. `IsInstrumentResolved == false` ⇒ `AllowFixSend = false`. No fallback ticker. No last-known-from-another-env.
2. If 55 on an inbound ER / MD does not map via `TryMapVenueInstrumentId` ⇒ do not treat as XAU exposure.
3. If persist is missing, in-memory register dies on process restart — **re-discover on every logon**, then persist again; never bake the integer into config.
4. `REAL_COPY_EXECUTION_ENABLED` must stay false until (1)–(3) are tested against the live SecurityList for Pepperstone account `1369850` and the resolved id is recorded in an ops note (not hardcoded in source).

`docs/ctrader-fix.md` already lists this order (SecurityListRequest type 0 → SecurityList → local map keyed by SecurityID → then MD / trade). Product does not implement it.

---

## 6. Identity model (do not collapse)

| Identity | Example | Where it lives | Allowed on FIX 55? |
|---|---|---|---|
| Canonical | `XAUUSD` | `CanonicalInstrument.Code` / `CanonicalInstrumentRef.XauUsd` | **Never** |
| Source symbol | `XAUUSDm`, `GOLD`, `XAUUSD.` | `(BrokerId, SourceSymbol)` → `source_symbol_mappings` | **Never** |
| Venue instrument id | `55=<long>` for **this** cTrader account | `destination_symbols` (missing) + `RegisterVenueInstrument` | **Only this** |

`ExecutionIntent.DestinationSymbol` is a free `string` defaulting to `""`. If a future sender copies canonical `XAUUSD` into that field and then into tag 55, that is the ticker bug.

`RiskEngine.DestinationQuote.VenueInstrumentId` is `string?`. Seeded null. A risk ALLOW with a null venue id must not authorize a send.

---

## 7. Measured wiring (2026-08-18)

| Path | Status |
|---|---|
| `SymbolNormalizer` constructed | Yes — `TradeReconstructor` singleton, default ctor |
| `TryMapSource` used | Yes — reconstruction only |
| `TryMapVenueInstrumentId` / `RegisterVenueInstrument` used in production | **No** |
| `source_symbol_mappings` rows | **0** |
| `destination_symbols` | **Does not exist** |
| `CTraderQuoteService` in DI | **No** |
| SecurityListRequest on QUOTE/TRADE after logon | **No** |
| `NewOrderSingle` builder / send | **No** (`RealCopyExecutionEnabled` default false) |
| Unit tests for venue fail-closed | **Yes** (one fact; uses `123456` as a **negative** then register) |
| Unit tests for `CTraderQuoteService` SecurityList | **None** matching `*Tests*.cs` |
| Docs `xauusd-normalization.md` | Exists; 7 lines; law correct; no tag table |

---

## 8. What a later increment must change (not done here)

Product was **not** edited by this agent. When a coder wave is allowed:

1. Fix `BuildSecurityListRequestTags` to `35=x` + `320` + `559`. Do not ship `35=y` as a request.
2. After logon, send that request on the RoE-correct session, parse `146` groups, persist, register.
3. Send gate: unresolved id ⇒ no MD subscribe that can be used for risk, and no `35=D`.
4. Stop compiling source aliases into `SymbolNormalizer`; load per-broker `source_symbol_mappings`; delete the `StartsWith("XAUUSD")` heuristic; fail closed in `OpenTrade.Start` instead of copying raw source into `CanonicalSymbol`.
5. Never write harness `123456`, RoE `1`/`39`, or a remembered Pepperstone id into seed or config.
6. Expand `docs/xauusd-normalization.md` with the tag-55 numeric rule, the persist schema, and the send-gate checklist.

---

## 9. Honesty close

- Venue IDs are **not** hardcoded inside `SymbolNormalizer`. That is the only clean half.
- Source aliases **are** hardcoded. Persist table is unused.
- Docs state the right discovery rule and are otherwise a stub.
- A quote-service stub knows 55 is numeric and 1007 is the name, but it is unwired, does not persist, and its “request” builder emits the **response** MsgType.
- Live send is off. Therefore this map cannot yet place Pepperstone capital. It also cannot yet **prevent** a wrong-market send if someone arms NOS without finishing SecurityList.

**Gate for any future `35=D`:** SecurityList resolved + persisted + registered + tag 55 is that numeric id and nothing else. Until then, keep NewOrderSingle disabled.

**Product files changed by this agent:** **0**.
)
