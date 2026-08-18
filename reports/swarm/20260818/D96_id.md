# D96 — Harness `123456` must not seed

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D96_id.md` |
| Agent | D96 (instrument-id seed barrier, read-only of product) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:00+05:30 |
| Assigned | **Harness `123456` must not seed.** Write this file. Do not modify product source. |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Test source modified | **No.** |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Binding law | Architecture v2 **§16**, **§30**, **§61**, **§69.10**, **§72.13**; official RoE tag 55; A32 / A34 / A44 / A68 / A86 / A89 #73 |
| Siblings | D28 (harness FLAG), D15 / B15 (mapper), D22 (seeder; **status narrative stale**), A86 (discovery), A57 item 10 |
| Classification vocabulary | §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` |

This is a **seed-barrier census**. It does **not** authorize a live `35=x` / `35=y` / `35=D`. It does **not** treat `123456` as Pepperstone XAU. It does **not** tick §69.10.

---

## 0. Verdict (binding)

**`123456` must not seed. Measured: it is not seeded. The harness still ships the digits.**

`FixSimulationHarness.SimulateSecurityList` hardcodes FIX tag **`55="123456"`** and comments it as the **XAUUSD instrument numeric ID**. Architecture §16 / §30 / §72.13 and A86 forbid treating any remembered, sample, or invented number as the venue instrument ID, and forbid copying a fixture id into persist / seed / options.

A later coding task that copies those six digits into `DemoSeeder`, `destination_quotes.venue_instrument_id`, a future `destination_symbols` row, `SymbolNormalizer` ctor defaults, `appsettings`, or `CTraderQuoteService` would convert a **test token** into a **forged discovery**. That is the defect this note pins.

| Surface | `123456` present? | Classification |
|---|---|---|
| `FixSimulationHarness.SimulateSecurityList` L141 | **YES — FLAG** (product assembly) | `UNSAFE` as a claimed XAU id; fixture-only if parameterized and never persisted |
| `DemoSeeder` dest quote | **No.** `VenueInstrumentId = null` | `EXISTS_AND_GOOD` on this field |
| `DemoSeeder` any other column | **No.** Zero `123456` tokens | seed barrier holds |
| `TraderDbContext` / `destination_symbols` | table **MISSING** | cannot persist yet; do **not** add this number when the table lands |
| `SymbolNormalizer` ctor venue dict | **empty** | `EXISTS_AND_GOOD` (no compiled venue id) |
| `CTraderFixOptions` | **no** instrument-id property | `EXISTS_AND_GOOD` |
| `apps/*/appsettings*.json` | **0** hits | `EXISTS_AND_GOOD` |
| `apps/web` | **0** hits; page prints `not discovered yet` when null | `EXISTS_AND_GOOD` |
| `tests/Unit/SymbolNormalizerTests` | yes, as **unmapped-then-register** key | legal fixture use |
| `CTraderQuoteService.OnSecurityListResponse` | would **accept** harness `55=123456` if fed that dictionary | leak path if wired; **not wired today** |
| §69.10 Discover Pepperstone XAU id | **not accepted** | `MISSING` |

**Do not** copy `123456` into seed. **Do not** treat the current `null` as “XAU mapped.” **Do not** tick A57 / A100 item 10 from the harness comment.

| Check | Result |
|---|---|
| Assigned rule `Harness 123456 must not seed` | **HOLDING** on persist/seed; **FLAG** remains in the harness source |
| Is `123456` a discovered Pepperstone XAU id? | **No evidence.** Dummy placeholder. |
| Does DemoSeeder persist `123456`? | **No.** `VenueInstrumentId = null`. |
| Does any options / JSON default it? | **No.** |
| Does the dashboard invent it? | **No.** `instrumentId` is the nullable quote column. |
| Product source changed by this agent | **0 files** |

---

## 1. Direct answers

```text
Q: Must harness 123456 seed?
A: No. Never. Not into DemoSeeder, destination_quotes,
   destination_symbols (when it exists), SymbolNormalizer
   defaults, CTraderFixOptions, appsettings, migrations,
   or a live 35=V / 35=D.

Q: Does it seed today?
A: No. Grep of src/Infrastructure, apps/, DemoSeeder:
   0 hits for 123456. The only product C# literal is
   FixSimulationHarness.cs:141. The only test use is
   SymbolNormalizerTests.Does_not_guess_venue_instrument_ids,
   which first asserts the id is unmapped.

Q: Is null on the demo quote a discovered id?
A: No. It is the honest empty. The FIX page therefore
   prints "not discovered yet". That is correct.

Q: What would a later coding task do wrong?
A: Wire SimulateSecurityList → OnSecurityListResponse →
   DestinationQuoteSnapshot.VenueInstrumentId = "123456"
   and call that "SecurityList discovery." That would
   greenwash §69.10 with a guessed id. Official wrong-id
   failure mode: you quote or trade the wrong symbol.

Q: Is this D28 again?
A: D28 flags the literal in the factory. D96 pins the
   seed/persist barrier: the digits must not leave the
   test process. D22's "LoggedOn" narrative is stale
   (seeder now writes Disconnected); the dest-quote
   VenueInstrumentId=null fact is not stale.
```

---

## 2. Method (read-only)

Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/src/`) were **not** edited.

| Step | What |
|---|---|
| 1 | Full read of `FixSimulationHarness.SimulateSecurityList` + ER defaults |
| 2 | Full read of `DemoSeeder` dest-quote + FIX rows (current worktree; untracked file) |
| 3 | SHA-256 + bytes + physical lines via `Get-FileHash` / `Get-Item` |
| 4 | Grep `123456` under `src/`, `tests/`, `apps/`, `docs/` |
| 5 | Grep `VenueInstrumentId` / `destination_symbols` / `RegisterVenueInstrument` / `FixSimulationHarness` |
| 6 | Cross-read `SymbolNormalizer`, `TraderDbContext`, `CTraderFixOptions`, `CTraderQuoteService`, `EfDashboardQueries.GetFixSessionsAsync`, `FixSessionsPage.tsx`, `SeedingAndStoreTests` |
| 7 | Apply architecture §16 / §30 / §69.10 / §72.13 and A86 §8–11 |

Nothing answered from memory. D28 / A86 named the same literal; this note **re-measures seed and persist**, it does not copy their verdicts as evidence.

---

## 3. Files hashed (this pass)

| Bytes | Physical lines | SHA-256 | Path | Git |
|---:|---:|---|---|---|
| 8970 | 205 | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` | **`M`** vs HEAD |
| 5082 | 140 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | **`??` untracked** |
| 3116 | 83 | `808CBA1F9C9F1FFF1647C0FDC9BD896BA1ECEBB463D22F971D0B4DDF6E687458` | `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` | clean vs this question |
| 421 | 12 | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | — |
| 5951 | 174 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | unchanged vs D19/D51 |
| 896 | 31 | `EB26D062B1574F218D60D16578B8243411C5996FA43EE7CD616485932CCEFF33` | `D:\Prop\tests\Unit\SymbolNormalizerTests.cs` | — |
| 3119 | 63 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | — |
| — | 133 | `7D2FDE1D33B47D619EA8BB0EC5F943BC21D8D97B46BEA269D70D46A20859B44A` | `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | — |
| — | — | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | — |
| 1312 | 26 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` | `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx` | same as D79 |

D22 hashed the seeder as `139D8F87…0BEF` / 138 lines / `LoggedOn`. That file is gone. Current seeder is a **new untracked** blob (`A6416491…`) that writes `Disconnected` and still `VenueInstrumentId = null`. Use this hash, not D22, for the seed-status story. The **id** story did not change.

---

## 4. The harness literal (source of the digits)

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

### 4.1 Why the comment is the seed risk

A six-digit placeholder can be a **test token**. The comment asserts venue truth:

```text
XAUUSD instrument numeric ID
```

That sentence is **false** on this disk:

| Claim in comment | Measured |
|---|---|
| This is *the* XAUUSD id | No live `35=x`. No recorded `35=y`. No `destination_symbols`. `CanonicalInstrument` is `Id` / `Code` / `Description` only. |
| It is numeric Spotware form | The *string* is digits. The *identity* is invented. Official IDs differ across brokers and environments (A34). |
| Tag 55 on `35=y` is a single field | Official Security List is repeating group `146` + `55` + `1007` + `1008`. This factory emits one pair and omits `146`, `320`, `322`, `560`, `1008`. |

Wrong ID is not cosmetic. Official credentials text (A34 / A86): you may be **trading or receiving prices for the wrong symbols**.

### 4.2 What `123456` is not

| Candidate | Why it is not this |
|---|---|
| Official RoE `55=1` | Sample book **EURUSD**, not XAU. Copying `1` into an XAU seed is a second FLAG. |
| Official RoE `55=39` | Sample book **NZDCHF**. |
| Pepperstone live XAU | Never discovered. §69.10 still open. |
| Demo seed | `VenueInstrumentId = null` on the demo quote row. |
| `SymbolNormalizer` default | Venue dictionary is **empty** at construction. |
| Options / env | `CTraderFixOptions` has no instrument-id field. |

A86 already classified this line: *legal as a fixture id inside a test, illegal as a production default or seed.* D96 tightens the second half: **do not persist it**.

---

## 5. Seed census (must stay empty)

Current dest-quote insert, complete:

```105:113:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.DestinationQuotes.Add(new DestinationQuoteSnapshot
        {
            Id = Guid.NewGuid(),
            CanonicalSymbol = "XAUUSD",
            VenueInstrumentId = null,
            Bid = 2399.45m,
            Ask = 2399.85m,
            ReceivedAt = now
        });
```

| Seeded field | Value | Honest? |
|---|---|---|
| `CanonicalSymbol` | `"XAUUSD"` | Allowed catalog name (A44 / A86 §8.1). Not a FIX tag 55. |
| **`VenueInstrumentId`** | **`null`** | **Yes — this is the barrier.** |
| `Bid` / `Ask` | `2399.45` / `2399.85` | Invented book (out of scope; do not “fix” by writing `123456`) |
| `CanonicalInstrument.Code` | `"XAUUSD"` | Name seed. A86: *Do not seed a guessed instrument ID.* |
| FIX `Status` | `Disconnected` (both) | Honest vs D22; unrelated to tag 55 |
| `SourceSymbolMappings` | **0 rows** | mapper still uses compiled aliases (D15) |

`DemoSeeder` has **no** `using` for `TraderIntelligence.Fix.CTrader`. It never constructs `FixSimulationHarness`. It never calls `RegisterVenueInstrument`. It never writes `123456`.

Integration test `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` asserts broker/group/deal/score/CompID facts. It does **not** assert `VenueInstrumentId`. A later seeder that writes `"123456"` would still pass Fact 1. That is a **missing lock**, not a current leak.

### 5.1 Grep of `123456` (this pass)

| Tree | Hits | Paths |
|---|---:|---|
| `D:\Prop\src` | **1** | `Fix.CTrader\Testing\FixSimulationHarness.cs:141` |
| `D:\Prop\src\Infrastructure` | **0** | — |
| `D:\Prop\apps` | **0** | — |
| `D:\Prop\docs` | **0** | — |
| `D:\Prop\tests` | **3** | `tests\Unit\SymbolNormalizerTests.cs` L26–28 |

Product persist/seed/config: **clean**. The only compiled default is the harness factory.

---

## 6. Persist surface (cannot store a discovered id yet)

`TraderDbContext` (SHA `AFB195AC…`, unchanged vs D19/D51) has **18** `DbSet`s. There is **no** `DestinationSymbol` / `destination_symbols`. The only column that could hold a venue id today is:

```3:12:D:\Prop\src\Domain\Entities\DestinationQuote.cs
public sealed class DestinationQuoteSnapshot
{
    public Guid Id { get; set; }
    public string CanonicalSymbol { get; set; } = "XAUUSD";
    public string? VenueInstrumentId { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? VenueTimestamp { get; set; }
}
```

Dashboard path (`EfDashboardQueries.GetFixSessionsAsync` L179) forwards `quote?.VenueInstrumentId` as `FixSessionDto` instrument id. After seed that is **`null`**. The page:

```19:19:D:\Prop\apps\web\src\pages\FixSessionsPage.tsx
            <div>Instrument ID: {s.instrumentId ?? 'not discovered yet'}</div>
```

prints **`not discovered yet`**. That string is the correct operator view. Seeding `123456` would paint a **fake discovered id** on `/fix` without a socket.

When A20 / A30 increment 0011 lands `destination_symbols`:

- `instrument_id` = tag 55 from a **recorded or live** `35=y`, `bigint > 0`
- UNIQUE `(execution_venue_id, instrument_id)`
- Demo and live are **different venues** (A86 §8.2)
- **Never** copy the harness token into that unique key

---

## 7. Mapper and tests: the legal use of the token

`SymbolNormalizer` venue dict starts empty. No numeric default:

```35:40:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
        _venueIdToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (venueIdMappings is not null)
        {
            foreach (var pair in venueIdMappings)
                _venueIdToCanonical[pair.Key] = pair.Value;
        }
```

`TryMapVenueInstrumentId` is exact lookup, no inference. Production callers do **not** pass `venueIdMappings` and do **not** call `RegisterVenueInstrument` (D15).

The unit test is the **correct** use:

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

That test proves the mapper starts unmapped. It does **not** prove `123456` is XAU on Pepperstone. A89 row 73 (`SecurityListDoesNotHardcodePepperstoneIdTests`) is the test that should pin the harness; the class file **does not exist**. `tests/Fix` does not exist. Zero tests construct `FixSimulationHarness`.

---

## 8. Leak path if a later task “just wires it”

`CTraderQuoteService.OnSecurityListResponse` (SHA `7D2FDE1D…`) walks dictionaries for `1007=="XAUUSD"` then parses tag 55 as `long` and stores it:

```50:62:D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs
        foreach (var entry in instrumentEntries)
        {
            if (!entry.TryGetValue(1007, out var symbolName))
                continue;
            if (!string.Equals(symbolName, "XAUUSD", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!entry.TryGetValue(55, out var instrumentIdRaw))
                throw new FormatException("SecurityList entry missing tag 55 (InstrumentID) for XAUUSD.");
            if (!long.TryParse(instrumentIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var instrumentId))
                throw new FormatException($"SecurityList tag 55 (InstrumentID) is not numeric: '{instrumentIdRaw}'.");

            _xauInstrumentId = instrumentId;
            return;
        }
```

A harness `35=y` with `55=123456` + `1007=XAUUSD` would make `IsInstrumentResolved == true` and `XauInstrumentId == 123456`. `BuildMarketDataRequestTags` would then emit **`55=123456`** on a `35=V`. That is the exact production send A86 forbids.

Today: **no host constructs the harness, no host feeds this method, no host persists `_xauInstrumentId`.** `BuildSecurityListRequestTags` itself is also wrong (`35=y` instead of official request `35=x`) — adjacent, not the assigned seed question.

`CTraderFixOptions` has **no** `XauInstrumentId` / `VenueInstrumentId` property. Keep it that way. An options default of `123456` would be a seed by another name.

---

## 9. Binding rules this review applied

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

From A86 §8.1:

```text
A30 Increment 7: seed execution_venues with Pepperstone/cServer.
Do not seed a guessed instrument ID.
canonical_instruments seeds one row XAUUSD. That is a name, not a FIX id.
```

From A86 §10 (fixture exception, not a seed license):

```text
Harness fixtures may use any numeric id (41, 999001, even 123456)
provided:
  - the id appears only in the recorded/simulated 35=y
  - production config / seed / options have no default instrument id
```

From A57 item 10 / A89 #73: fixture ID must come from a recording (or stay an opaque unmapped token). Shipping `123456` as production would violate §72.13.

---

## 10. What a later coding task must not do

1. **Do not** set `DemoSeeder` `VenueInstrumentId = "123456"` (or `41`, or RoE `1` / `39`).
2. **Do not** insert `destination_symbols.instrument_id = 123456` in a migration or seeder.
3. **Do not** pass `venueIdMappings: { "123456" → "XAUUSD" }` into the production `SymbolNormalizer` ctor.
4. **Do not** add `CTraderFixOptions.XauInstrumentId = 123456` / env `CTRADER_XAU_INSTRUMENT_ID=123456` as a “dev default.”
5. **Do not** parse `SimulateSecurityList()` in `apps/fix-worker` or `DemoSeeder` and persist tag 55.
6. **Do not** write a test named `SecurityListDiscoversXau` that asserts `55=="123456"` is the Pepperstone id (A89 #65 as written would greenwash this FLAG). Assert: tag 55 is **numeric**, tag 1007 is the ticker, mapper is **unmapped** until Register, seed column stays **null**.
7. **Do not** treat official `55=1` as XAUUSD.
8. **Do not** tick §69.10 / A100 G04 / A57 item 10 because a string factory exists.

Allowed later (separate coding task, not this agent):

- Parameterize the harness id (no compiled default that looks real).
- Stop commenting it as “the XAUUSD instrument numeric ID.”
- Discover the real Pepperstone id via `35=x` / `35=y` and persist **that**.
- Lock `SeedingAndStoreTests` with `VenueInstrumentId.Should().BeNull()` until discovery exists.

---

## 11. Honesty box

| Claim someone might make | Honest state 2026-08-18 |
|---|---|
| “We seeded the cTrader XAU instrument id” | **No.** Quote row is `null`. Page says `not discovered yet`. |
| “123456 is the Pepperstone XAU id” | **False.** Invented harness comment. |
| “123456 is safe because it is only a test double” | It lives in the **product** assembly (`TraderIntelligence.Fix.CTrader`) and is labeled as the real id. It is **not** in seed. Keep it that way. |
| “§69.10 done” | **No.** Discovery is MISSING. |
| “Wiring the harness to the seeder would finish discovery” | That would **violate** this note and §72.13. |
| “D22 says LoggedOn so seed is already a lie; adding 123456 is consistent” | D22 status story is **stale**. Current seeder writes `Disconnected`. Do not add a second lie on the id column. |
| Product source edited by D96 | **No.** |

---

## 12. One-page operator view

```text
D96  Harness 123456 must not seed                       2026-08-18
================================================================
Harness  FixSimulationHarness.cs L141  55="123456"      FLAG
Comment  "XAUUSD instrument numeric ID"                 FALSE
Seeder   VenueInstrumentId = null                       HOLDING
DbSet    destination_symbols                            MISSING
Mapper   venue dict empty at ctor                       HOLDING
Options  no instrument-id field                         HOLDING
apps/    0 hits for 123456                              HOLDING
UI       Instrument ID: not discovered yet              HONEST
QuoteSvc would accept harness 123456 if wired           LEAK PATH
§69.10   Pepperstone XAU id discovered                  NO
Product source edited by D96                            NO
================================================================
```

---

## 13. Sources (absolute)

- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` (SHA-256 `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616`)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`)
- `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` (SHA-256 `808CBA1F9C9F1FFF1647C0FDC9BD896BA1ECEBB463D22F971D0B4DDF6E687458`)
- `D:\Prop\src\Domain\Entities\DestinationQuote.cs` (SHA-256 `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726`)
- `D:\Prop\src\Domain\Entities\CanonicalInstrument.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` (SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB`)
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` (SHA-256 `7D2FDE1D33B47D619EA8BB0EC5F943BC21D8D97B46BEA269D70D46A20859B44A`)
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\tests\Unit\SymbolNormalizerTests.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx`
- `D:\Prop\docs\ctrader-fix.md` (SecurityList section; discover, do not invent)
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§16, 30, 61, 69.10, 72.13
- Official RoE: https://help.ctrader.com/fix/specification/
- Sibling reviews (cross-check only): `D28_harness.md`, `D15_symbols.md`, `D22_seeder.md` (status stale), `A86_instrument_discovery.md`, `A44_symbol_normalization.md`, `A57_first_useful_version.md`, `B15_symbol_review.md`

---

*End of D96. Product source was not modified. Harness `55=123456` must not seed; measured persist/seed/options are still empty.*
