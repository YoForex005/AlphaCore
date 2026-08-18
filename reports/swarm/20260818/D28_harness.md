# D28 — `FixSimulationHarness` review: **FLAG `55=123456`**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D28_harness.md` |
| Agent | D28 (FIX simulation harness, read-only) |
| Date | 2026-08-18 |
| Assigned | Read `FixSimulationHarness.cs`. **Flag `123456`.** Write this file. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT | `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` |
| SHA-256 | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` |
| Size | 8970 bytes, **205** lines, LF |
| Last write | 2026-08-18 13:18:52 |
| Law | Architecture v2 **§16**, **§30**, **§61**, **§69.10**, **§72.13**; official RoE tag 55; A32 / A34 / A44 / A68 / A86 |
| Classification vocabulary | §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` |

This file is review only. It does **not** authorize a live `NewOrderSingle`. It does **not** treat `123456` as Pepperstone XAU.

---

## 0. Verdict

**FLAG `123456`.**

`FixSimulationHarness.SimulateSecurityList` hardcodes FIX tag **`55="123456"`** and comments it as the **XAUUSD instrument numeric ID**. That is a **guessed / dummy Spotware id** living in the **product** adapter assembly. Architecture §16 / §30 / §72.13 and A86 forbid treating any remembered, sample, or invented number as the venue instrument ID.

Complementary **FLAG**: every ExecutionReport builder defaults tag 55 to the **ticker string** `"XAUUSD"`. Official cServer rejects non-numeric symbol ids (`Expected numeric symbolId, but got …`). The same class therefore emits **two illegal forms** of tag 55:

| Path | Tag 55 value | Type | Law broken |
|---|---|---|---|
| `SimulateSecurityList` | `"123456"` | invented numeric | §30 / §72.13 “do not hardcode / do not guess” |
| `SimulateExecutionReport_*` (7 methods) | `"XAUUSD"` (default) | ticker string | §16 “never assume tag 55 is `"XAUUSD"`” |

The class itself is **EXISTS_NEEDS_REFACTOR**: a checksummed `|` **string factory**, not Architecture §61 adapter test mode. Zero tests call it. `tests/Fix` does not exist. Do **not** treat this file as the simulator. Do **not** copy `123456` into `destination_symbols`, `appsettings`, seed, or a live `35=D` / `35=V`.

| Check | Result |
|---|---|
| Assigned flag `123456` | **FLAG** — hardcoded in product source at L141 |
| Is `123456` a discovered Pepperstone XAU id? | **No evidence.** Dummy placeholder. |
| Is `123456` an official RoE sample id? | **No.** RoE samples use `55=1` (EURUSD on **that** sample book) and `55=39` (NZDCHF). |
| Has the mapper pre-registered `123456`? | **No.** `SymbolNormalizer` venue dict starts empty (B15). |
| Does DemoSeeder persist `123456`? | **No.** `VenueInstrumentId = null`. |
| Does any test drive the harness? | **No.** Grep of `D:\Prop\tests` for `FixSimulationHarness` / `SimulateSecurityList` / `SimulateLogon` → **0**. |
| §61 seven capabilities | **0 / 7** implemented as a venue |
| §69.10 Discover Pepperstone XAU id | **not accepted** |
| Product source changed by this agent | **0 files** |

---

## 1. Direct answers

```text
Q: Where is 123456?
A: Exactly once in product C# under src/:

     D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs:141
         (55, "123456"), // XAUUSD instrument numeric ID (as string)

   Same method also stamps (1007, "XAUUSD") at L142.

Q: Is that legal?
A: Legal only as an *opaque fixture token inside a test that never leaves the
   test process*, and only if the mapper starts unmapped and production never
   copies the number.

   It is **illegal** as:
     - a claimed XAUUSD instrument ID (the comment does exactly that)
     - a production / seed / config default
     - a substitute for SecurityList discovery (§30 / §69.10)
     - a value sent on a live 35=D / 35=V / 35=x

   The literal lives in the *product* project (TraderIntelligence.Fix.CTrader),
   not under tests/. That is a leak surface.

Q: What should a later coding task do?
A: Keep a fixture id if tests need one, but:
     1. Stop commenting it as "the XAUUSD instrument numeric ID".
     2. Make the id a parameter (no compiled default that looks real).
     3. Never persist it from the factory into destination_symbols.
     4. Prove TryMapVenueInstrumentId("123456") is false until Register.
     5. Discover the real Pepperstone id via 35=x / 35=y and persist that.
     6. Default ER tag 55 to a *numeric* fixture, never "XAUUSD".

Q: Is FixSimulationHarness the §61 simulator?
A: No. It cannot accept NewOrderSingle, cannot drop a socket, cannot own a
   book, cannot replay a tape. It returns strings. See §4.
```

---

## 2. Method (read-only)

Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/src/`) were **not** edited.

| Step | What |
|---|---|
| 1 | Read the 205-line SUT end-to-end |
| 2 | SHA-256 + byte/line count |
| 3 | Grep `src/` + `tests/` + `apps/` for `123456`, `FixSimulationHarness`, `SimulateSecurityList` |
| 4 | Cross-check parser, `SymbolNormalizer`, DemoSeeder, `TraderDbContext`, options, csproj |
| 5 | Apply architecture §16 / §30 / §61 / §69.10 / §72.13 and official RoE (A32/A34/A86) |

Adjacent reviews (A68, B05, B15, C13, C19, A86) independently named the same literal. This note **re-reads the file**; it does not copy their verdicts as evidence.

---

## 3. The flagged literal (primary finding)

```129:143:D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs
    public string SimulateSecurityList(string senderCompId = "SENDER", string senderSubId = "QUOTE", string targetCompId = "cServer", string targetSubId = "QUOTE")
    {
        // Extremely simplified. Only the tags our services look at:
        // 1007 = SymbolName, 55 = InstrumentID (numeric)
        // cTrader often returns instruments in a repeating group, but for unit tests we model both instrument fields as separate tags.
        return BuildStandardMessage(new[] {
            (8, "FIX.4.4"),
            (35, "y"), // SecurityList (simplified; actual MsgType for SecurityList is gateway-specific)
            (49, senderCompId),
            (56, targetCompId),
            (57, targetSubId),
            (50, senderSubId),
            (55, "123456"), // XAUUSD instrument numeric ID (as string)
            (1007, "XAUUSD")
        });
    }
```

### 3.1 Why the comment is the defect, not just the digits

A six-digit placeholder can be a **test token**. The comment asserts venue truth:

```text
XAUUSD instrument numeric ID
```

That sentence is **false** on this disk:

| Claim in comment | Measured |
|---|---|
| This is *the* XAUUSD id | No SecurityList client. No recorded `35=y`. No `destination_symbols` table. `CanonicalInstrument` has only `Id` / `Code` / `Description`. |
| It is numeric Spotware form | The *string* is digits. The *identity* is invented. Official IDs differ across brokers and environments (A34 / credentials page). |
| Tag 55 on `35=y` is a single field | Official Security List is repeating group `146` + `55` + `1007` + `1008`. This factory emits one pair and **omits `146`, `320`, `322`, `560`, `1008`**. |
| Services look at 1007 / 55 | No production service parses SecurityList. Grep of `src/` for `1007` → this file only. |

Wrong ID is not cosmetic. Official credentials text (A34 / A86): you may be **trading or receiving prices for the wrong symbols**.

### 3.2 What `123456` is not

| Candidate | Why it is not this |
|---|---|
| Official RoE `55=1` | Sample book **EURUSD**, not XAU. Copying `1` into an XAU test is a second FLAG (A68 §17.7). |
| Official RoE `55=39` | Sample book **NZDCHF**. |
| Pepperstone live XAU | Never discovered. §69.10 still open. |
| Demo seed | `DemoSeeder` writes `VenueInstrumentId = null` on the demo quote row. |
| `SymbolNormalizer` default | Venue dictionary is **empty** at construction. |

`SymbolNormalizerTests.Does_not_guess_venue_instrument_ids` uses `"123456"` **as a key it first proves is unmapped**:

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

That test is the **correct** use of the token (opaque, register-then-map). The harness comment is the **incorrect** use (asserted venue identity). A89 row 73 (`SecurityListDoesNotHardcodePepperstoneIdTests`) is the test that should pin this; the class file **does not exist**.

### 3.3 Leak surface

| Location | `123456` present? | Risk |
|---|---|---|
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` L141 | **YES — FLAG** | Product DLL ships the number + the lying comment |
| `tests/Unit/SymbolNormalizerTests.cs` | yes, as unmapped-then-register key | Acceptable fixture **if** never documented as Pepperstone XAU |
| `DemoSeeder` | **no** | Good |
| `CTraderFixOptions` | **no** | Good |
| `apps/*/appsettings*.json` | **no** | Good |
| `destination_symbols` / EF entity | table **MISSING** | Cannot persist yet; do not add this number when the table lands |
| `apps/fix-worker` | never constructs the harness (C19) | Safe by absence |

A86 already classified this exact line: *legal as a fixture id inside a test, illegal as a production default or seed.* D28 tightens that: the literal is **not inside a test**. It is inside `src/`.

---

## 4. What the class actually is

Path: `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`  
Namespace: `TraderIntelligence.Fix.CTrader.Testing`  
Type: `public sealed class FixSimulationHarness`

It holds a private `FixMessageParser` and returns pipe-delimited strings via `BuildFixMessage`. Useful later as a **fixture writer** after RoE corrections. Not a session, not a book, not a transport.

### 4.1 Public surface (13 methods)

| Method | MsgType emitted | Tag 55 | Notes |
|---|---|---|---|
| `SimulateLogonSuccess` | `35=A` | none | Client-shaped header; `141=Y`; no `553`/`554` (good — no password in fixtures) |
| `SimulateLogonFail` | `35=3` | none | **Wrong.** Official failed Logon is Logout `35=5` + `58=…`. Tag `371` is RefTagID, not Text. |
| `SimulateExecutionReport_New` | `35=8` | default `"XAUUSD"` | `150` from `execTransType` default `"0"`; `39=0` |
| `SimulateExecutionReport_Fill` | `35=8` | default `"XAUUSD"` | `150=F`, `39=2`; optional 32/31 |
| `SimulateExecutionReport_PartialFill` | `35=8` | default `"XAUUSD"` | `150=F`, `39=1` |
| `SimulateExecutionReport_Canceled` | `35=8` | default `"XAUUSD"` | `150=4`, `39=4` |
| `SimulateExecutionReport_Rejected` | `35=8` | default `"XAUUSD"` | `150=8`, `39=8`; text → tag 58 |
| `SimulateExecutionReport_Expired` | `35=8` | default `"XAUUSD"` | `150=C`, `39=C` |
| `SimulateExecutionReport_UnknownState` | `35=8` | default `"XAUUSD"` | `150=I`, `39=0` — **misnamed** (see §5.3) |
| `SimulateDuplicateExecutionReport` | n/a | n/a | Identity: returns the input string |
| `SimulateDisconnect` | `35=0` | none | Heartbeat + invented `1128=text`. Disconnect is a **transport** event. |
| `SimulateSecurityList` | `35=y` | **`"123456"` FLAG** | See §3 |
| `SimulateMarketDataSnapshot` | `35=X` | caller `symbolIdNumeric` | Name says Snapshot (`35=W`); body is Incremental letter. Invented tags 1320/1321. |

Private helpers: `BuildStandardMessage`, `BuildStandardMessageWithExecReport`.

### 4.2 ExecutionReport body actually written

```182:202:D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs
        var tags = new List<(int tag, string value)>
        {
            (8, "FIX.4.4"),
            (35, "8"), // ExecutionReport
            (49, senderCompId),
            (56, "cServer"),
            (57, "TRADE"),
            (50, senderSubId),
            (11, clOrdId), // ClOrdID
            (37, orderId), // OrderID
            (55, symbol),
            (150, execType),
            (39, ordStatus),
            (60, DateTimeOffset.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff")),
        };

        if (lastQty != 0m) tags.Add((32, lastQty.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (lastPx != 0m) tags.Add((31, lastPx.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (!string.IsNullOrEmpty(text)) tags.Add((58, text));
```

Missing vs official New ER (A32 / A68): `14 CumQty`, `151 LeavesQty`, `54 Side`, `38 OrderQty`, `721 PosMaintRptID`. Tag `17 ExecID` is also absent (official table often omits 17 — persist must not require it). LastQty/LastPx omitted when `0m`, so New/Cancel/Reject/Expired/Unknown builders emit **no** 32/31.

Header direction is **client-shaped** (`49=SENDER`, `57=TRADE`). Official **server** ER is `49=CSERVER|50=TRADE|56=<client>|57=<echo of client 50>`.

`DateTimeOffset.UtcNow` in tag 60 makes two “identical” New ERs **non-byte-identical**. `SimulateDuplicateExecutionReport` cannot be the FAQ-duplicate proof unless the caller freezes the string first.

---

## 5. Secondary flags (same file; not the assigned literal)

These do not dilute FLAG-123456. They stop the next implementer from treating harness output as golden.

### 5.1 FLAG — ER tag 55 defaults to ticker `"XAUUSD"`

Seven methods:

```43:43:D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs
    public string SimulateExecutionReport_New(string clOrdId, string orderId, string symbol = "XAUUSD", ...
```

Same default on Fill / PartialFill / Canceled / Rejected / Expired / UnknownState.

Architecture §16 (verbatim intent): *Never assume FIX tag 55 is the string `"XAUUSD"`.*  
Official reject: *Expected numeric symbolId, but got CS8260.*

`SimulateMarketDataSnapshot` is the only application-message builder that already takes a numeric id. That is the right shape; SecurityList and ERs should match it.

### 5.2 FLAG — `SimulateMarketDataSnapshot` is not a snapshot and not a book

```146:162:D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs
    public string SimulateMarketDataSnapshot(...)
    {
        ...
            (35, "X"), // MarketDataSnapshotFullRefresh (simplified)
            ...
            (55, symbolIdNumeric),
            (1320, bid), // Custom-ish: we'll reuse 1320 for tests as "Bid"
            (1321, ask), // 1321 for tests as "Ask"
```

| Claim | Official |
|---|---|
| Comment: Snapshot Full Refresh | Snapshot is **`35=W`**. `35=X` is Incremental Refresh. |
| Tags 1320 / 1321 | Not RoE bid/ask. Official book is group `268` / `269` / `270` (and 271). |
| Parser can read both sides | `FixMessageParser.Parse` → last-wins `Dictionary<int,string>`. Two `269`/`270` **collapse**. §61 MD replay **cannot** use this dictionary (B28 / A68). |

### 5.3 FLAG — `SimulateExecutionReport_UnknownState` is not unknown-state

Unknown-state (architecture §34 / §61 / A42) is: **NOS may have left the process, no terminal ER, socket dead.** It is a **transport** fault, not an ExecType.

This method emits `150=I` (Order Status) and `39=0` (New). `ExecutionOrderStateMachine.MapOrdStatus` prefers tag 39, so `39=0` maps to **`Accepted`**, not `ExecutionStateUnknown` (B16). The comment on L113 (“we'll treat this as unknown state in service”) is **false** against the machine that exists.

`150=I` is the **recovery response** to `35=H`, not the fault (A68 §11).

### 5.4 FLAG — `SimulateDisconnect` is a Heartbeat

```121:127:D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs
    public string SimulateDisconnect(string text = "Connection dropped")
        => BuildStandardMessage(
            new[] {
                (8, "FIX.4.4"),
                (35, "0"), // Heartbeat (used as placeholder)
                (1128, text)
            });
```

Disconnect is not a FIX message. Tag 1128 is ApplVerID, not a text field. A later `CTraderFixSimulator` must raise a transport event (and, after a possible send, `AfterDisconnectWithUnknownAck()` → `ExecutionStateUnknown`).

### 5.5 FLAG — `SimulateLogonFail` is a Session Reject

Official: invalid Logon → **Logout `35=5`** with `58=InternalError: RET_INVALID_DATA` (A32). This factory emits `35=3` and stuffs the reason into tag **371** (RefTagID).

### 5.6 FLAG — SecurityList is not a repeating group

Official `35=y` (A32 / A86): `320` / `322` / `560` / `146=N` then N × (`55`, `1007`, `1008`). This factory: one `55` + one `1007`. No digits (`1008`). No request echo. Comment even says MsgType is “gateway-specific”; official MsgType **is** `y`.

### 5.7 Note — `cServer` case (not a new FLAG)

Worktree defaults and tag 56 are `"cServer"` (issued form). Do not silently fold to `CSERVER` (architecture §26 / B27 / C09 / C21). Keep case as configured. Official RoE table prints `CSERVER`; this repo’s pin is **do not rewrite**.

---

## 6. Architecture §61 scoreboard (measured against this file)

Architecture §61:

```text
Before using real NewOrderSingle:
  Build a FIX adapter test mode.
  parse recorded ExecutionReports
  replay MarketDataIncrementalRefresh
  simulate disconnects
  simulate duplicate ExecutionReports
  simulate partial fill
  simulate rejection
  simulate unknown-state disconnect
Do not use the real account as the first integration test.
```

| §61 item | Harness method | Measured | Class |
|---|---|---|---|
| Adapter test mode (no `*.c-trader.com`) | none | No `VenueMode`, no in-process venue, no “does not hit venue” guard | **MISSING** |
| Parse recorded ERs | ER builders | Generates synthetic strings; no fixture store; no handler | **MISSING** |
| Replay `35=X` | `SimulateMarketDataSnapshot` | Wrong tags; parser cannot hold a two-sided book | **MISSING** |
| Simulate disconnects | `SimulateDisconnect` | Emits `35=0` | **UNSAFE** as a stand-in |
| Duplicate ERs | `SimulateDuplicateExecutionReport` | `return input;` — no persist key, no clock freeze | **EXISTS_NEEDS_REFACTOR** (identity only) |
| Partial fill | `SimulateExecutionReport_PartialFill` | One string, `39=1`; no lifecycle 1→2 | **EXISTS_NEEDS_REFACTOR** |
| Rejection | `SimulateExecutionReport_Rejected` | One string, `39=8` | **EXISTS_NEEDS_REFACTOR** |
| Unknown-state disconnect | `SimulateExecutionReport_UnknownState` | `150=I` + `39=0` → SM **Accepted** | **UNSAFE** (wrong semantics) |

**§61: 0 / 7 as a venue.** String builders for partial/reject are seeds, not the capability.

A68 §16 acceptance boxes that this file still fails (re-checked):

```text
[ ] Adapter test mode exists and cannot open *.c-trader.com
[ ] Recorded official New+Fill ERs parse
[ ] 35=X updates destination quote
[ ] Disconnect after 35=D → ExecutionStateUnknown
[ ] Duplicate 35=8 → one persist row
[ ] Partial 39=1 then 39=2
[ ] Reject 150=8 terminal
[ ] Unknown-state recovery via 35=H / AF / AN
[ ] Tag 55 in all generated application messages is numeric   ← FAIL (XAUUSD + 123456)
```

---

## 7. Adjacent measured facts (this pass)

| Piece | Path | Role vs harness |
|---|---|---|
| `FixMessageParser` | `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` | Only product caller of `BuildFixMessage` is this harness (B28). Last-wins `Dictionary`. Checksum ASCII sum mod 256, 3 digits. Builder sorts remaining tags **ascending** — not RoE order. |
| `CTraderFixOptions` | `Configuration\CTraderFixOptions.cs` | Live host default `live-us-eqx-01.p.c-trader.com`; SenderCompID default `live.pepperstone.1369850`; `RealCopyExecutionEnabled=false`. No `VenueMode`. |
| `FixSessionOwnership` | `Services\FixSessionOwnership.cs` | In-memory fence. Unused by harness. |
| `CTraderQuoteSession` / `CTraderTradeSession` / `CTraderFixSimulator` | **absent** | Still MISSING (B05). |
| `ICTraderFixVenue` | **absent** | Still MISSING. |
| `tests/Fix` | **absent** | A27 / A68 required project not created. |
| QuickFIXn 1.14.1 | **not referenced** | Current `TraderIntelligence.Fix.CTrader.csproj` has **zero** FIX packages (worktree; C19). |
| `destination_symbols` | **absent** | No entity, no `DbSet`. Discovery cannot persist. |
| `apps/fix-worker` | `D:\Prop\apps\fix-worker\Worker.cs` | Never `new FixSimulationHarness()`. Stamps `LoggedOn` / `ReadyForMarketData` without a socket. |

A89 rows 64–66, 73 list harness test classes as `EXISTS`. That column means **SUT / inventory name**, not a green test file. Measured: **zero** `*.cs` under `tests/` mention the harness.

---

## 8. Binding rules this review applied

From architecture §16:

```text
Never assume FIX tag 55 is the string "XAUUSD".
For cTrader, retrieve the Security List and map the returned
symbol/instrument ID to the canonical symbol.
Persist this mapping.
```

From architecture §30:

```text
Do not hardcode an instrument ID from another cTrader account or broker.
```

From architecture §72.13:

```text
Discover cTrader symbols/instrument IDs; do not guess.
```

From architecture §61: adapter test mode **before** real `NewOrderSingle`; do not use the real account as the first integration test.

From official RoE (A32 / A34):

- Tag 55 is a **Spotware numeric instrument id**, not a ticker.
- Human name is tag **1007** `SymbolName`.
- IDs **differ across brokers**.
- Official MD reject: expected **numeric** symbolId.

From A86: `FixSimulationHarness` `55=123456` is legal only as a **fixture id inside a test**, illegal as a production default or seed.

From `D:\Prop\docs\ctrader-fix.md` rule 4: *Do not hardcode instrument IDs. Discover via Security List.*

---

## 9. What a later coding task must not do

1. **Do not** copy `123456` into `destination_symbols`, DemoSeeder, `appsettings`, or `SymbolNormalizer` ctor defaults.
2. **Do not** write `FixSimulationHarnessLogonAndRejectTests` that asserts `55=="123456"` is “the XAU id” (A89 #65 as written would greenwash this FLAG). Assert: tag 55 is **numeric**, tag 1007 is the ticker, mapper is **unmapped** until Register (A89 #73).
3. **Do not** rename this class to `CTraderFixSimulator` without adding a venue (Logon, two sessions, book, NOS, transport drop).
4. **Do not** feed `35=X` / `35=y` through `IReadOnlyDictionary<int,string>`.
5. **Do not** enable `REAL_COPY_EXECUTION_ENABLED` because a string factory exists.
6. **Do not** register the harness in `apps/fix-worker`.
7. **Do not** treat official `55=1` as XAUUSD.

Keep the class. Parameterize the fixture id. Correct ER/MD/Logon/disconnect tags (A68 §15 step 2). Build `CTraderFixSimulator` as a **separate** type (A68 / B05).

---

## 10. Honesty box

| Claim someone might make | Honest state 2026-08-18 |
|---|---|
| “We have a FIX simulation harness” | We have a **pipe-delimited string factory** in `src/Fix.CTrader/Testing`. |
| “SecurityList returns the XAU instrument id” | It returns the **invented** string `123456` and a comment that calls that XAU. |
| “Tag 55 is numeric everywhere” | SecurityList: dummy numeric. ERs: ticker `"XAUUSD"`. |
| “§61 is underway” | 0/7 venue capabilities. 0 tests. No `tests/Fix`. |
| “§69.10 done” | **No.** Pepperstone XAU id is not discovered. |
| “123456 is safe because it is only a test double” | It lives in the **product** assembly and is labeled as the real id. |

---

## 11. Sources (absolute)

- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` (SUT; SHA-256 `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616`)
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Entities\CanonicalInstrument.cs`
- `D:\Prop\src\Domain\Entities\DestinationQuote.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\tests\Unit\SymbolNormalizerTests.cs`
- `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§16, 30, 61, 69.10, 72.13
- Official RoE: https://help.ctrader.com/fix/specification/
- Official FAQ / credentials / communication-model as extracted in A32 / A34 / A86
- Sibling reviews (cross-check only): `A68_fix_simulator.md`, `A86_instrument_discovery.md`, `B05_fix_gap.md`, `B15_symbol_review.md`, `B16_fix_fsm_review.md`, `C13_fuv_scorecard.md`, `C19_quickfix_not_wired.md`
