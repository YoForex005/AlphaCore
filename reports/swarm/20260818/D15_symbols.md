# D15 — `SymbolNormalizer` re-measure (aliases, venue IDs, persist gap)

**Artifact:** `D:\Prop\reports\swarm\20260818\D15_symbols.md`  
**Subject:** `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` (83 lines; sole file under `Domain\Instruments\`)  
**Wave:** D15 — D-band re-read of the live mapper after A44 (design) and B15 (review)  
**Date:** 2026-08-18  
**Status:** review only — **product source not modified**  
**Product files changed by this agent:** **0**

**Binding design:** architecture v2 §16 / §30 / §72.13; `A44_symbol_normalization.md`; `A86_instrument_discovery.md`; `A01` invariant (aliases live only in `SourceSymbolMapping`)  
**Prior review:** `B15_symbol_review.md` (still accurate; this file re-measures the same SUT)  
**Go-live gate:** `A100` G04 — `XAU symbol mappings are verified` remains **FAIL**  
**Tests read:** `D:\Prop\tests\Unit\SymbolNormalizerTests.cs`, `TradeReconstructionTests.cs`  
**Callers read:** `TradeReconstructor.cs`, `ReconstructedTradeResult.cs`, `DealIngestionService.cs` / `ReconstructionScoringService`, `TraderDbContext.cs`, `DemoSeeder.cs`, `DependencyInjection.cs`, `RiskEngine.cs`, `FixSimulationHarness.cs`

---

## Verdict

| Check | Result | Honest one-liner |
|---|---|---|
| File exists and is the only Domain instrument mapper | **PASS** | `CanonicalInstrumentRef` + `SymbolNormalizer` in one file |
| Source aliases never hardcoded | **FAIL** | compiled-in `DefaultXauAliases` (12 tokens, 11 unique ignore-case keys) |
| Fail-closed on unknown `XAUUSD*` / `GOLD*` suffixes | **FAIL** | `compact.StartsWith("XAUUSD")` plus compact-`GOLD` heuristic |
| Per-broker `(BrokerId, key)` lookup | **FAIL** | `TryMapSource(string)` only; no broker argument |
| Persist-then-map (`source_symbol_mappings`) | **FAIL** | EF `DbSet` exists; mapper never reads it; seeder inserts **0** mapping rows |
| Venue instrument IDs never hardcoded in this class | **PASS** | `_venueIdToCanonical` starts empty; no numeric / Pepperstone / RoE IDs |
| Wired to SecurityList / `destination_symbols` | **FAIL** | no `DestinationSymbol` entity; `RegisterVenueInstrument` unused in production |
| DI registers a shared catalog | **FAIL** | `TradeReconstructor` is singleton and default-constructs `new SymbolNormalizer()` |
| A100 G04 / Phase 2 “mappings verified” | **FAIL** | same as B15; nothing closed this gap |

Do **not** claim Phase 2 “XAU symbol mappings verified.” Do **not** claim “aliases and venue IDs never hardcoded.” Venue IDs are clean **in this class**. Source aliases and the prefix heuristic are not.

B15 is **not stale**. Line numbers, HashSet contents, heuristic, and caller sites match the file on disk today.

---

## 1. What the file actually is

`D:\Prop\src\Domain\Instruments\` contains **one** C# file. Two types:

### 1.1 `CanonicalInstrumentRef` (allowed identity)

```1:8:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
namespace TraderIntelligence.Domain.Instruments;

public sealed record CanonicalInstrumentRef(string Code)
{
    public static CanonicalInstrumentRef XauUsd { get; } = new("XAUUSD");

    public override string ToString() => Code;
}
```

`XauUsd = new("XAUUSD")` is the **canonical identity**, not an alias. A44 §3.1 allows one seed instrument. This constant is legal. It is **not** a substitute for persisted `CanonicalInstrument` rows (those live in `Domain\Entities\CanonicalInstrument.cs` as a thin `Id`/`Code`/`Description` record and are unused by this mapper).

### 1.2 `SymbolNormalizer` (in-memory dictionary pair)

Sealed class. No interface. No `BrokerId`. No store. Two dictionaries, both `StringComparer.OrdinalIgnoreCase`:

| Dictionary | Populated how | Default contents |
|---|---|---|
| `_sourceToCanonical` | ctor copies `DefaultXauAliases` → `"XAUUSD"`, then overlays `extraSourceMappings` | **12 compiled aliases** |
| `_venueIdToCanonical` | ctor copies `venueIdMappings` if any; later `RegisterVenueInstrument` | **empty** |

Public API (complete):

| Member | Signature | What it actually does |
|---|---|---|
| ctor | `(IEnumerable<KeyValuePair<string,string>>? extraSourceMappings = null, IEnumerable<KeyValuePair<string,string>>? venueIdMappings = null)` | always installs the compiled XAU catalog first; extras overwrite keys; venue dict stays empty unless injected |
| `TryMapSource` | `(string sourceSymbol, out string canonical)` | trim → exact dict → compact (strip `.` and space) dict → `StartsWith("XAUUSD")` / compact `GOLD` heuristic |
| `IsXauUsd` | `(string sourceSymbol)` | `TryMapSource` && canonical equals `"XAUUSD"` ignore-case |
| `TryMapVenueInstrumentId` | `(string venueInstrumentId, out string canonical)` | exact dict lookup; **no** trim, **no** inference |
| `RegisterVenueInstrument` | `(string venueInstrumentId, string canonical)` | reject whitespace id; overwrite-or-insert |

There is no `ISymbolNormalizer` port. There is no `CanonicalInstrumentMapper`. There is no load from `source_symbol_mappings` or `destination_symbols`. `extraSourceMappings` / `venueIdMappings` are unused by any production caller (grep of `*.cs` under `D:\Prop\src`: only the ctor parameters).

There is **no** empty-catalog constructor. `new SymbolNormalizer()` always ships the compiled alias set.

---

## 2. Binding rules this review applied

From architecture §16 (quoted in A44 §1):

```text
CanonicalInstrument
  XAUUSD

broker/source symbol → canonical XAUUSD
cTrader instrument ID → canonical XAUUSD

Never assume FIX tag 55 is the string "XAUUSD".
Persist this mapping.
```

A44 / A01 locks still in force:

- Listed source aliases (`XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`) are **seed rows per broker**, not a global compiled set.
- Extra suffixes (`GOLD.`, `XAUUSD.pro`, `XAUUSDpro`, `XAUUSD.i`, …) must **not** auto-map.
- No `StartsWith("XAU")` / `Contains("XAU")` / silent default.
- Lookup key = trim + `ToUpperInvariant()`; **do not** strip `.` or suffixes.
- cTrader numeric IDs: **discover**, persist per venue, **never hardcode**, **never guess**.
- `TryMapVenueInstrumentId` must fail until a registered / persisted row exists.
- Unmapped source must **not** be written as `CanonicalSymbol`.

---

## 3. Source aliases — hardcoded (FAIL)

### 3.1 Compiled catalog

```12:16:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
    private static readonly HashSet<string> DefaultXauAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "XAUUSD", "XAUUSD.", "XAUUSDM", "XAUUSD.A", "XAUUSD.I", "XAUUSD.S",
        "XAUUSD.PRO", "XAUUSDPRO", "GOLD", "GOLD.", "GOLD.A", "XAUUSDpro"
    };
```

Ctor always installs them (lines 25–27). `TradeReconstructor` default-constructs this type (`TradeReconstructor.cs` line 21), so every reconstruction path ships this catalog.

| # | Compiled token | Ignore-case unique? | In §16 / A44 §5.1 seed? | A44 §5.2 “must not auto-seed”? |
|---|---|---|---|---|
| 1 | `XAUUSD` | yes | yes | — |
| 2 | `XAUUSD.` | yes | yes | — |
| 3 | `XAUUSDM` | yes | yes (`XAUUSDm`) | — |
| 4 | `XAUUSD.A` | yes | yes | — |
| 5 | `XAUUSD.I` | yes | **no** | extra suffix (islamic-style) |
| 6 | `XAUUSD.S` | yes | **no** | extra suffix |
| 7 | `XAUUSD.PRO` | yes | **no** | explicit forbid (`XAUUSD.pro`) |
| 8 | `XAUUSDPRO` | yes | **no** | explicit forbid (`XAUUSDpro`) |
| 9 | `GOLD` | yes | yes | — |
| 10 | `GOLD.` | yes | **no** | explicit forbid |
| 11 | `GOLD.A` | yes | **no** | not listed; `GOLD.` family |
| 12 | `XAUUSDpro` | **no** — same key as #8 | **no** | same as #8 |

Compiled set: **12 tokens, 11 unique ignore-case keys**. Six of those keys are A44-forbidden or unlisted. `XAUUSDpro` is a dead duplicate of `XAUUSDPRO`.

`extraSourceMappings` can **overwrite** a key after the defaults (lines 29–33). It cannot turn the defaults off.

### 3.2 Second hardcoded path: compact + prefix heuristic

```43:68:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
    public bool TryMapSource(string sourceSymbol, out string canonical)
    {
        if (string.IsNullOrWhiteSpace(sourceSymbol))
        {
            canonical = string.Empty;
            return false;
        }

        var key = sourceSymbol.Trim();
        if (_sourceToCanonical.TryGetValue(key, out canonical!))
            return true;

        var compact = key.Replace(".", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        if (_sourceToCanonical.TryGetValue(compact, out canonical!))
            return true;

        if (compact.StartsWith("XAUUSD", StringComparison.OrdinalIgnoreCase)
            || compact.Equals("GOLD", StringComparison.OrdinalIgnoreCase))
        {
            canonical = CanonicalInstrumentRef.XauUsd.Code;
            return true;
        }

        canonical = string.Empty;
        return false;
    }
```

This is the A44 §13 anti-pattern (`if (symbol.Contains("XAU") || symbol == "GOLD")`) in slightly narrower form. It is **not** `Contains("XAU")` (so `XAUEUR` / `XAGUSD` correctly miss), but it **is** a silent prefix map of every compact `XAUUSD*`.

Lookup-key policy vs A44 §4:

| A44 `NormalizeLookupKey` | Implemented today |
|---|---|
| trim Unicode whitespace | `Trim()` only (no Unicode-category trim beyond BCL `Trim`) |
| `ToUpperInvariant()` | **no** — relies on `OrdinalIgnoreCase` dict comparer |
| do **not** strip trailing `.` | **strips every `.`** into `compact` |
| do **not** strip suffixes / spaces | **strips spaces** into `compact` |
| empty / whitespace → UNMAPPED | empty check returns false (correct) |

Dot-stripping collapses `XAUUSD.` → `XAUUSD` on the compact path. A44 §4: the trailing dot is part of the persist key. Collapsing is acceptable only as a *lookup convenience after a stored row exists*. Here it is a global rewrite plus a prefix match, so unknown dotted suffixes never fail closed.

### 3.3 Measured resolve matrix (logic review of the source; not a live broker probe)

| Raw | After trim / compact | Result today | A44 required |
|---|---|---|---|
| `XAUUSD` / `xauusd` / ` XAUUSD ` | exact / ignore-case | `XAUUSD` | mapped (seed) |
| `XAUUSD.` | exact default key | `XAUUSD` | mapped (seed; key must stay distinct) |
| `XAUUSDm` / `XAUUSDM` | exact / compact `XAUUSDM` | `XAUUSD` | mapped (seed) |
| `XAUUSD.a` / `XAUUSD.A` | exact `XAUUSD.A` | `XAUUSD` | mapped (seed) |
| `GOLD` / `gold` | exact | `XAUUSD` | mapped (seed) |
| `GOLD.` | exact default **or** compact `GOLD` | `XAUUSD` | **UNMAPPED** |
| `GOLD.A` | exact default | `XAUUSD` | **UNMAPPED** |
| `XAUUSD.pro` / `XAUUSDPRO` / `XAUUSDpro` | default or compact or prefix | `XAUUSD` | **UNMAPPED** |
| `XAUUSD.i` / `XAUUSD.s` | default or prefix | `XAUUSD` | **UNMAPPED** |
| `XAUUSD.c` / `XAUUSD#` / `XAUUSDecn` / `XAUUSDmicro` | not in set; `compact.StartsWith("XAUUSD")` | **silent `XAUUSD`** | **UNMAPPED** |
| `XAU USD` (internal space) | compact `XAUUSD` | `XAUUSD` | UNMAPPED (not a listed key) |
| `XAUEUR` / `XAUGBP` / `XAUJPY` | compact does not start with `XAUUSD` | unmapped | unmapped (correct) |
| `XAGUSD` / `SILVER` / `EURUSD` | no | unmapped | unmapped (correct) |
| `GOLDM` / `GOLDUSD` / `USDGOLD` | no (compact ≠ `GOLD`, no prefix) | unmapped | unmapped (correct) |
| `""` / whitespace / null-if-passed-as-empty | empty check | false / `""` | unmapped (correct) |

Correct misses (`XAUEUR`, `XAGUSD`, `EURUSD`, empty) must not be used to greenwash the prefix path. The dangerous cases are `XAUUSD.c`, `XAUUSDecn`, `XAUUSDmicro`, `XAUUSD.pro`, `GOLD.`.

### 3.4 Not per-broker

`TryMapSource` takes only the raw string. Achiever `GOLD` and StarwaveFX `GOLD` cannot be enabled independently. A44 `SourceSymbolMappingTests` (“Achiever `GOLD` does not map StarwaveFX `GOLD` unless that broker has a row”) cannot pass against this type.

`SourceSymbolMapping` (`D:\Prop\src\Domain\Entities\SourceSymbolMapping.cs`) is still the thin scaffold:

```3:9:D:\Prop\src\Domain\Entities\SourceSymbolMapping.cs
public sealed class SourceSymbolMapping
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public string SourceSymbol { get; set; } = string.Empty;
    public Guid CanonicalInstrumentId { get; set; }
}
```

Missing vs A44 §7: `source_symbol_key`, `is_enabled`, `mapping_origin`, timestamps.

EF maps it (`TraderDbContext.cs` lines 87–92: unique `(BrokerId, SourceSymbol)`). Grep of product `*.cs`: the `DbSet` is the **only** production reference. No insert, no query, no join into the mapper.

`DemoSeeder` inserts one `CanonicalInstrument` (`Code = "XAUUSD"`, fixed GUID `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1`) and **zero** `SourceSymbolMapping` rows. Persist-then-map is not implemented.

---

## 4. Downstream effect (reconstruction / scoring / first-3)

`TradeReconstructor` is registered as a **singleton** (`DependencyInjection.cs` line 38) with the parameterless ctor. That constructs `new SymbolNormalizer()` once for the process.

`OpenTrade.Start` (`TradeReconstructor.cs` lines 207–213):

```207:214:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            symbols.TryMapSource(deal.SourceSymbol, out var canonical);
            var trade = new OpenTrade
            {
                BrokerId = brokerId,
                Login = login,
                PositionId = positionId,
                CanonicalSymbol = string.IsNullOrEmpty(canonical) ? deal.SourceSymbol : canonical,
                SourceSymbol = deal.SourceSymbol,
```

Two defects stacked:

1. Hardcoded / heuristic map decides `CanonicalSymbol` for first-3 / `IsXauUsd` / scoring.
2. On a true miss, **raw source is copied into `CanonicalSymbol`**. A44: no pass-through of raw as canonical.

`ReconstructedTradeResult.IsXauUsd` (`ReconstructedTradeResult.cs` lines 40–41) compares `CanonicalSymbol` to `"XAUUSD"` only. So:

| Source print | Today | If mapper were fail-closed |
|---|---|---|
| `XAUUSDm` | counts as XAU (heuristic / catalog) | would count **only if** that broker has a seed row |
| `XAUUSD.c` | counts as XAU (prefix) | would **not** count |
| `GOLD.` | counts as XAU (catalog / compact GOLD) | would **not** count |
| `EURUSD` | `CanonicalSymbol = "EURUSD"`; `IsXauUsd = false` | same (correct non-count) |

`ReconstructionScoringService.RebuildTraderAsync` filters `t.Completed && t.IsXauUsd` then scores. `BaselineScorer.ComputeFeatures` filters the same again. `TradeReconstructor.CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` (threshold 3) all ride this filter. A silent prefix map therefore **inflates first-3 eligibility**.

`TradeReconstructionTests` hard-codes `SourceSymbol = "XAUUSDm"` on every fixture deal and asserts `IsXauUsd`. That locks the compiled/heuristic path, not persist-then-map.

Fake MT5 deals always emit `"XAUUSD"` (`FakeMt5BrokerConnector.cs` lines 167–168). Demo ingest never exercises `GOLD` / dotted suffixes / unknown `XAUUSD*`.

---

## 5. Venue IDs — not hardcoded (PASS in this class)

### 5.1 Measured source

```35:41:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
        _venueIdToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (venueIdMappings is not null)
        {
            foreach (var pair in venueIdMappings)
                _venueIdToCanonical[pair.Key] = pair.Value;
        }
```

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

Grep of `SymbolNormalizer.cs` for `\d{3,}`: **no matches**. No `55=1`, no RoE `39`, no Pepperstone long, no `123456`.

Default `TryMapVenueInstrumentId("123456", …)` is **false** until `RegisterVenueInstrument` or ctor injection. That matches A44 §6.2 / A86: “refuses to infer a canonical from the number itself.”

No production caller passes `venueIdMappings` or calls `RegisterVenueInstrument` (only `SymbolNormalizerTests.Does_not_guess_venue_instrument_ids`). Venue map stays empty in reconstruction / workers. Correct fail-closed **shape**. Not discovery.

### 5.2 Gaps that do **not** violate “never hardcode,” but will fail later gates

| Gap | Evidence | Why it matters |
|---|---|---|
| Tag `55` string `"XAUUSD"` is not rejected as a venue token | `TryMapVenueInstrumentId` is a raw dict; A44: `"XAUUSD"` as `55` must not map | Today it misses (good) unless someone `Register`s it (possible) |
| No numeric parse (`long ≥ 1`) | `RegisterVenueInstrument` accepts any non-whitespace string | Can store a ticker as an “id” |
| Empty / whitespace venue id | `TryMap` does not trim; returns false (no throw) | Fine for fail-closed; inconsistent with `Register` throw |
| No `ExecutionVenueId` in the key | one global dict | Demo vs live, two Pepperstone logins would collide if both registered |
| No persist / stale / ambiguous | no `DestinationSymbol` entity (A86 / B33 still true) | Phase 4 / §69.10 still closed |
| No SecurityList handler | `src/Fix.CTrader` has parser + ownership + sim harness only | cannot discover |
| Fixture id lives in the **test harness**, not here | `FixSimulationHarness.SimulateSecurityList` tag `55="123456"` | Legal as a test fixture; **must not** be copied into this mapper or `appsettings` |

`DemoSeeder` sets `DestinationQuoteSnapshot.VenueInstrumentId = null` (correct: do not invent an id). `DestinationQuoteSnapshot.CanonicalSymbol` and `CopyIntent` / `ExecutionIntent` default to `"XAUUSD"` on the entity — that is a default **canonical** string, not a venue id, and it is a silent default A44 forbids on the *mapper* (entities defaulting the identity is a separate smell).

`RiskEngine.DestinationQuote` carries `CanonicalSymbol` + `VenueInstrumentId` but **does not check** that the quote’s canonical is `XAUUSD` or that `VenueInstrumentId` is a mapped numeric. Exposure gates use caller-supplied `CurrentGrossXau` / `CurrentNetXau` numbers. Missing map is not `INTENT_INCOMPLETE` / `QUOTE_UNAVAILABLE` here.

### 5.3 Related (out of `SymbolNormalizer`, recorded so it is not forgotten)

`FixSimulationHarness` ExecutionReport helpers default `symbol = "XAUUSD"` (`FixSimulationHarness.cs` line 43 and siblings). That is the §16 / A34 anti-pattern for tag 55 (Spotware numeric id). Not a hardcode **inside** `SymbolNormalizer`. Still illegal if that string is ever sent on a live `35=D` / `35=V`.

---

## 6. Persist / DI / discovery inventory (measured)

| Item | Path | Status |
|---|---|---|
| `CanonicalInstrument` entity | `Domain\Entities\CanonicalInstrument.cs` | thin scaffold (`Id`, `Code`, `Description`) |
| `SourceSymbolMapping` entity | `Domain\Entities\SourceSymbolMapping.cs` | thin scaffold; no key / enabled / origin |
| `DestinationSymbol` entity | none under `src/` | **MISSING** |
| `CanonicalSymbol` / `SourceSymbol` / `DestinationInstrumentId` value objects | none | **MISSING** |
| `ISymbolNormalizer` port | none | **MISSING** |
| `CanonicalInstrumentMapper` | none | **MISSING** |
| `IDestinationInstrumentDiscovery` / SecurityList handler | none in `src/Fix.CTrader` | **MISSING** |
| EF `canonical_instruments` | `TraderDbContext` 80–85 | table mapped; unique `Code` |
| EF `source_symbol_mappings` | `TraderDbContext` 87–92 | table mapped; unique `(BrokerId, SourceSymbol)`; **never written** |
| EF `destination_symbols` | — | **MISSING** |
| Seed canonical row | `DemoSeeder` 61–66 | one `XAUUSD` row |
| Seed alias rows | `DemoSeeder` | **0** |
| DI registration of `SymbolNormalizer` | `DependencyInjection.cs` | **none** — reconstructed via `TradeReconstructor` default ctor |
| Production `RegisterVenueInstrument` callers | grep | **0** |
| Production `extraSourceMappings` / `venueIdMappings` | grep | **0** |

---

## 7. Tests vs the standing check

`D:\Prop\tests\Unit\SymbolNormalizerTests.cs` (32 lines, 2 methods):

| Test | What it proves | Standing-check coverage |
|---|---|---|
| `Maps_known_aliases_to_XAUUSD` (theory, 5 cases) | `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD` → `XAUUSD` on a **default** ctor | Documents the hardcoded catalog; does **not** prove rows came from persist |
| `Does_not_guess_venue_instrument_ids` | `"123456"` false until `RegisterVenueInstrument` | **Good** for venue IDs; fixture only |

Missing (A44 §11 / A89 #86–88 / A27 `Mapping.*`):

- Unknown / extra suffix **must not** become XAU: `EURUSD`, `XAUEUR`, `GOLD.`, `XAUUSD.pro`, `XAUUSD.c`, `XAUUSDecn`, empty
- Lookup-key policy: persist key `XAUUSD.` stays distinct; compact-strip is a **documented deviation**, not an untested accident
- Per-broker isolation
- `TryMapVenueInstrumentId("XAUUSD")` stays false (ticker is not an id)
- Empty venue id on `Register` throws
- No compile-time Pepperstone constant in product source (exists as a review grep today; no automated test)
- Dedicated classes `Mapping.XauCanonicalMappingTests`, `Mapping.SourceSymbolMappingTests`, `Mapping.DestinationInstrumentMappingTests`, `Harness.SecurityListXauDiscoveryTests` — **none exist**

`TradeReconstructionTests` uses `SourceSymbol = "XAUUSDm"` and asserts `IsXauUsd` — depends on the compiled alias / heuristic.

No unit test constructs `SymbolNormalizer(extraSourceMappings: …)` or injects a mapper into `TradeReconstructor`.

---

## 8. Finding list (reviewer, no rubber-stamp)

| ID | Sev | Finding | Evidence |
|---|---|---|---|
| D15-01 | **P0** | Source aliases are hardcoded in `DefaultXauAliases` | `SymbolNormalizer.cs` 12–16, 25–27 |
| D15-02 | **P0** | Prefix heuristic silently maps any compact `XAUUSD*` and compact `GOLD` | lines 55–64; A44 §5.2 / §13 forbid |
| D15-03 | **P0** | Catalog is global, not `(BrokerId, key)` | `TryMapSource(string)` only; A01 / A44 |
| D15-04 | **P0** | Compiled set includes A44-forbidden tokens (`GOLD.`, `XAUUSD.PRO`, `XAUUSDPRO`, `XAUUSD.I`, `XAUUSD.S`, `GOLD.A`) | same HashSet |
| D15-05 | **P0** | Persist tables unused; seeder does not insert mappings | `SourceSymbolMappings` DbSet only; `DemoSeeder` canonical row only |
| D15-06 | **P1** | Dot/space strip rewrites lookup keys | line 55; A44 §4 |
| D15-07 | **P1** | Unmapped source is written as `CanonicalSymbol` | `TradeReconstructor` 207–213 |
| D15-08 | **P1** | Unit tests lock the hardcoded happy path; no fail-closed cases | `SymbolNormalizerTests` |
| D15-09 | **P2** | `XAUUSDpro` duplicate of `XAUUSDPRO` under ignore-case | HashSet line 15 |
| D15-10 | — | Venue IDs are **not** hardcoded in this class | empty dict; no numeric literals |
| D15-11 | **P2** | Venue map is in-memory, unscoped, unvalidated numeric | lines 74–81; blocks Phase 4 even though hardcode check passes |
| D15-12 | info | Canonical code `"XAUUSD"` on `CanonicalInstrumentRef` is the identity, not an alias | allowed |
| D15-13 | **P1** | No `ISymbolNormalizer`; reconstructor singleton default-constructs the mapper | `DependencyInjection.cs` 38; `TradeReconstructor` 18–21 |
| D15-14 | **P2** | Entity defaults (`CopyIntent` / `ExecutionIntent` / `DestinationQuoteSnapshot`) hard-default `CanonicalSymbol = "XAUUSD"` | silent identity if a caller forgets to map |
| D15-15 | **P2** | `RiskEngine` does not require mapped canonical + numeric venue id | `RiskEngine.cs` quote checks are age/spread/mid only |
| D15-16 | info | B15 findings D15-01…12 are **unchanged**; this wave adds D15-13…15 from DI/risk/entity re-read | no product edit since B15 |

---

## 9. What “never hardcoded” would look like (not implemented here)

This note does not implement. When a coding task is authorized, A44 §8 / §14 already named the shape:

1. Empty in-memory maps at construction.
2. Seed **rows** for Achiever + StarwaveFX only: `XAUUSD`, `XAUUSD.`, `XAUUSDM`, `GOLD`, `XAUUSD.A`. Operator-disable per broker.
3. `TryMapSource(brokerId, raw)` → persist lookup; miss = explicit unmapped; **no** `StartsWith`.
4. Venue: `TryMapVenueInstrumentId(venueId, numericId)` against persisted `destination_symbols` after SecurityList; **zero** compile-time longs.
5. Reconstruction writes `CanonicalSymbol` only on a mapped hit; otherwise leave empty / exclude from XAU filters.
6. Tests: current five aliases **given seed rows**; unknown suffixes false; `"123456"` / `"XAUUSD"` as venue tokens false until register/discover.

Until then, `A100` G04 stays **FAIL**.

---

## 10. Honesty metrics

| Metric | Value |
|---|---|
| File reviewed | `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` (83 lines) |
| Hardcoded source alias tokens | **12** (11 unique ignore-case keys) |
| Extra vs §16 catalog | **6** forbidden / unlisted (`I`, `S`, `PRO`, `PRO` compact, `GOLD.`, `GOLD.A`) |
| Prefix / GOLD heuristic | **present** (lines 59–64) |
| Hardcoded venue / Pepperstone / RoE instrument IDs in this file | **0** |
| Production callers of `RegisterVenueInstrument` / `venueIdMappings` | **0** |
| Production load of `source_symbol_mappings` into the mapper | **0** |
| Seeded `SourceSymbolMapping` rows in `DemoSeeder` | **0** |
| `DestinationSymbol` entity | **0** |
| `ISymbolNormalizer` / mapper Application service | **0** |
| Unit tests asserting fail-closed unknown aliases | **0** |
| Unit tests asserting venue id is not guessed | **1** (fixture `123456`) |
| Unit tests asserting ticker `"XAUUSD"` is not a venue id | **0** |
| B15 stale? | **no** — SUT text unchanged |
| Product source modified | **0** |
| Live Achiever / Starwave / Pepperstone symbol lists probed this wave | **no** |

**Aliases never hardcoded:** false.  
**Venue IDs never hardcoded (in `SymbolNormalizer`):** true.  
**Persist-then-map:** false.  
**A100 G04:** **FAIL**.  
**Standing check (both alias + venue hardcode must pass):** **FAIL**.
