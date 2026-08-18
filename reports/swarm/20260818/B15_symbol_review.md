# B15 — SymbolNormalizer review (aliases / venue IDs)

**Artifact:** `D:\Prop\reports\swarm\20260818\B15_symbol_review.md`  
**Subject:** `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs`  
**Check:** aliases and venue IDs are **never hardcoded**  
**Binding design:** architecture v2 §16 / §30 / §72.13; `A44_symbol_normalization.md`; `A86_instrument_discovery.md`; `A01` invariant (aliases live only in `SourceSymbolMapping`)  
**Tests read:** `D:\Prop\tests\Unit\SymbolNormalizerTests.cs`, `TradeReconstructionTests.cs`  
**Callers read:** `TradeReconstructor.cs`, `ReconstructedTradeResult.cs`, `DealIngestionService.cs` / `ReconstructionScoringService`, `TraderDbContext.cs` (`source_symbol_mappings`), `DemoSeeder.cs`  
**Date:** 2026-08-18  
**Status:** review only — **product source not modified**  
**Product files changed by this agent:** **0**

---

## Verdict

| Check | Result | Honest one-liner |
|---|---|---|
| Venue instrument IDs never hardcoded in `SymbolNormalizer` | **PASS** | `_venueIdToCanonical` starts empty; no numeric / Pepperstone / RoE IDs in this file |
| Source aliases never hardcoded | **FAIL** | compiled-in `DefaultXauAliases` (12 strings) plus a `StartsWith("XAUUSD")` / `Equals("GOLD")` heuristic |
| Standing check (both must pass) | **FAIL** | aliases are hardcoded; venue IDs are not |
| A44 fail-closed / persist-then-map | **FAIL** | mapper never reads `SourceSymbolMapping`; unknown `XAUUSD*` suffixes silently become canonical |
| Wired to SecurityList / destination table | **FAIL** | `RegisterVenueInstrument` is the only write path; no discovery, no persist |

Do **not** claim Phase 2 “XAU symbol mappings verified” (`A100` G04). Do **not** claim “aliases and venue IDs never hardcoded.” Venue IDs are clean **in this class**. Aliases are not.

---

## 1. What the file actually is

`CanonicalInstrumentRef` is a one-code wrapper. `XauUsd = new("XAUUSD")` is the **canonical identity**, not an alias. That constant is allowed.

`SymbolNormalizer` is a sealed in-memory dictionary pair:

| Dictionary | Populated how | Default contents |
|---|---|---|
| `_sourceToCanonical` | ctor copies `DefaultXauAliases` → `"XAUUSD"`, then overlays `extraSourceMappings` | **12 compiled aliases** |
| `_venueIdToCanonical` | ctor copies `venueIdMappings` if any; later `RegisterVenueInstrument` | **empty** |

Public API:

- `TryMapSource(string sourceSymbol, out string canonical)` — no `BrokerId`
- `IsXauUsd(string sourceSymbol)`
- `TryMapVenueInstrumentId(string venueInstrumentId, out string canonical)` — exact dict lookup, no inference
- `RegisterVenueInstrument(string venueInstrumentId, string canonical)` — reject whitespace id only

There is no `ISymbolNormalizer` port. There is no load from `source_symbol_mappings` / `destination_symbols`. `extraSourceMappings` exists as an override hatch and is **unused** by any production caller (grep of `*.cs`: only the ctor parameter).

---

## 2. Binding rules this review applied

From architecture §16 (v2 lines 699–731) and A44 §1 / §13:

```text
broker/source symbol → canonical XAUUSD
cTrader instrument ID → canonical XAUUSD
Never assume FIX tag 55 is the string "XAUUSD".
Persist this mapping.
```

A44 / A01 lock:

- Listed source aliases (`XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`) are **seed rows per broker**, not a global compiled set.
- Extra suffixes (`GOLD.`, `XAUUSD.pro`, `XAUUSDpro`, `XAUUSD.i`, …) must **not** auto-map.
- No `StartsWith("XAU")` / `Contains("XAU")` / silent default.
- Lookup key = trim + upper; **do not** strip `.` or suffixes.
- cTrader numeric IDs: **discover**, persist per venue, **never hardcode**, **never guess**.
- `TryMapVenueInstrumentId` must fail until a registered / persisted row exists.

---

## 3. Aliases — hardcoded (FAIL)

### 3.1 Compiled catalog

```12:16:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
    private static readonly HashSet<string> DefaultXauAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "XAUUSD", "XAUUSD.", "XAUUSDM", "XAUUSD.A", "XAUUSD.I", "XAUUSD.S",
        "XAUUSD.PRO", "XAUUSDPRO", "GOLD", "GOLD.", "GOLD.A", "XAUUSDpro"
    };
```

Ctor always installs them (lines 25–27). `new SymbolNormalizer()` — used by `TradeReconstructor` when no mapper is injected — therefore **always** ships this catalog.

| # | Compiled token | In §16 / A44 §5.1 seed? | A44 §5.2 “must not auto-seed”? |
|---|---|---|---|
| 1 | `XAUUSD` | yes | — |
| 2 | `XAUUSD.` | yes | — |
| 3 | `XAUUSDM` | yes (`XAUUSDm`) | — |
| 4 | `XAUUSD.A` | yes | — |
| 5 | `XAUUSD.I` | **no** | extra suffix (islamic-style) |
| 6 | `XAUUSD.S` | **no** | extra suffix |
| 7 | `XAUUSD.PRO` | **no** | explicit forbid (`XAUUSD.pro`) |
| 8 | `XAUUSDPRO` | **no** | explicit forbid (`XAUUSDpro`) |
| 9 | `GOLD` | yes | — |
| 10 | `GOLD.` | **no** | explicit forbid |
| 11 | `GOLD.A` | **no** | not listed; `GOLD.` family |
| 12 | `XAUUSDpro` | **no** | same key as #8 under `OrdinalIgnoreCase` (dead duplicate) |

So the compiled set is **wider** than the architecture catalog and includes tokens A44 named as fail-closed.

`extraSourceMappings` can **overwrite** a key after the defaults (lines 29–33). It cannot turn the defaults off. There is no “empty catalog” constructor.

### 3.2 Second hardcoded path: prefix / exact-GOLD heuristic

```51:64:D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs
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
```

This is the A44 §13 anti-pattern (`if (symbol.Contains("XAU") || symbol == "GOLD")`) in slightly narrower form.

Measured consequences (logic review; not executed here):

| Raw | After trim / compact | Result today | A44 required |
|---|---|---|---|
| `XAUUSD` / `xauusd` / ` XAUUSD ` | exact / compact | `XAUUSD` | mapped (seed) |
| `XAUUSD.` | exact key in defaults | `XAUUSD` | mapped (seed; key must stay distinct) |
| `XAUUSDm` | exact / compact `XAUUSDM` | `XAUUSD` | mapped (seed) |
| `GOLD` | exact | `XAUUSD` | mapped (seed) |
| `XAUUSD.a` | exact `XAUUSD.A` | `XAUUSD` | mapped (seed) |
| `GOLD.` | exact default **or** compact `GOLD` | `XAUUSD` | **UNMAPPED** |
| `GOLD.A` | exact default | `XAUUSD` | **UNMAPPED** |
| `XAUUSD.pro` / `XAUUSDPRO` | default or compact or prefix | `XAUUSD` | **UNMAPPED** |
| `XAUUSD.i` / `XAUUSD.s` | default or prefix | `XAUUSD` | **UNMAPPED** |
| `XAUUSD.c` / `XAUUSD#` / `XAUUSDecn` / `XAUUSDmicro` | not in set; `compact.StartsWith("XAUUSD")` | **silent `XAUUSD`** | **UNMAPPED** |
| `XAUEUR` / `XAUGBP` | compact does not start with `XAUUSD` | unmapped | unmapped (correct) |
| `XAGUSD` / `SILVER` / `EURUSD` | no | unmapped | unmapped (correct) |
| `""` / whitespace | empty check | false / `""` | unmapped (correct) |

Dot-stripping also **collapses** `XAUUSD.` → `XAUUSD` on the compact path. A44 §4: the trailing dot is part of the persist key. Collapsing is acceptable only as a *lookup convenience after a stored row exists*; here it is a global rewrite plus a prefix match, so unknown dotted suffixes never fail closed.

### 3.3 Not per-broker

`TryMapSource` takes only the raw string. Achiever `GOLD` and StarwaveFX `GOLD` cannot be enabled independently. A44 `SourceSymbolMappingTests` (“Achiever `GOLD` does not map StarwaveFX `GOLD` unless that broker has a row”) cannot pass against this type.

`source_symbol_mappings` is mapped in EF (`TraderDbContext` lines 87–92: unique `(BrokerId, SourceSymbol)`) and **never written** (grep: only the `DbSet`). `DemoSeeder` inserts one `CanonicalInstrument` (`Code = "XAUUSD"`) and zero mapping rows. Persist-then-map is not implemented.

### 3.4 Downstream effect

`TradeReconstructor` default-constructs `new SymbolNormalizer()` (`TradeReconstructor.cs` line 21). `OpenTrade.Start` (`lines 207–213`):

```text
symbols.TryMapSource(deal.SourceSymbol, out var canonical);
CanonicalSymbol = string.IsNullOrEmpty(canonical) ? deal.SourceSymbol : canonical;
```

Two defects stacked:

1. Hardcoded / heuristic map decides first-3 / `IsXauUsd` / scoring (`ReconstructionScoringService` filters `t.Completed && t.IsXauUsd`).
2. On a true miss, **raw source is copied into `CanonicalSymbol`**. A44: no pass-through of raw as canonical.

`ReconstructedTradeResult.IsXauUsd` then compares `CanonicalSymbol` to `"XAUUSD"` only — so an unmapped `GOLD.` that the heuristic already remapped **counts**; a future fail-closed `GOLD.` that passed through as `GOLD.` would **not** count. Today the heuristic wins.

---

## 4. Venue IDs — not hardcoded (PASS in this class)

### 4.1 Measured source

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

No production caller passes `venueIdMappings` or calls `RegisterVenueInstrument` (only the unit test). Venue map stays empty in reconstruction / workers. Correct fail-closed **shape**. Not discovery.

### 4.2 Gaps that do **not** violate “never hardcode,” but will fail later gates

| Gap | Evidence | Why it matters |
|---|---|---|
| Tag `55` string `"XAUUSD"` is not rejected as a venue token | `TryMapVenueInstrumentId` is a raw dict; A44: `"XAUUSD"` as `55` must not map | Today it misses (good) unless someone `Register`s it (possible) |
| No numeric parse (`long ≥ 1`) | `RegisterVenueInstrument` accepts any non-whitespace string | Can store a ticker as an “id” |
| Empty / whitespace venue id | `TryMap` does not trim; returns false (no throw) | Fine for fail-closed; inconsistent with `Register` throw |
| No `ExecutionVenueId` in the key | one global dict | Demo vs live, two Pepperstone logins would collide if both registered |
| No persist / stale / ambiguous | no `DestinationSymbol` entity (A86 still true) | Phase 4 / §69.10 still closed |
| Fixture id lives in the **test harness**, not here | `FixSimulationHarness.SimulateSecurityList` tag `55="123456"` | Legal as a test fixture; **must not** be copied into this mapper or `appsettings` |

`DemoSeeder` sets `DestinationQuoteSnapshot.VenueInstrumentId = null` (correct: do not invent an id). `DestinationQuoteSnapshot.CanonicalSymbol` defaults to `"XAUUSD"` on the entity — that is a default **canonical** string, not a venue id.

### 4.3 Related (out of `SymbolNormalizer`, recorded so it is not forgotten)

`FixSimulationHarness` ExecutionReport helpers default `symbol = "XAUUSD"`. That is the §16 / A34 anti-pattern for tag 55 (Spotware numeric id). Not a hardcode **inside** `SymbolNormalizer`. Still illegal if that string is ever sent on a live `35=D` / `35=V`.

---

## 5. Tests vs the standing check

`D:\Prop\tests\Unit\SymbolNormalizerTests.cs` (measured):

| Test | What it proves | Standing-check coverage |
|---|---|---|
| `Maps_known_aliases_to_XAUUSD` | `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD` → `XAUUSD` on a **default** ctor | Documents the hardcoded catalog; does **not** prove rows came from persist |
| `Does_not_guess_venue_instrument_ids` | `"123456"` false until `RegisterVenueInstrument` | **Good** for venue IDs; fixture only |

Missing (A44 §11 / A89 #86–88, G8):

- Unknown / extra suffix **must not** become XAU: `EURUSD`, `XAUEUR`, `GOLD.`, `XAUUSD.pro`, `XAUUSD.c`, `XAUUSDecn`, empty
- Lookup-key policy: persist key `XAUUSD.` stays distinct; compact-strip is a **documented deviation**, not an untested accident
- Per-broker isolation
- `TryMapVenueInstrumentId("XAUUSD")` stays false (ticker is not an id)
- Empty venue id on `Register` throws
- No compile-time Pepperstone constant in product source (exists as a review grep today; no automated test)

`TradeReconstructionTests` uses `SourceSymbol = "XAUUSDm"` and asserts `IsXauUsd` — depends on the compiled alias / heuristic.

---

## 6. Finding list (reviewer, no rubber-stamp)

| ID | Sev | Finding | Evidence |
|---|---|---|---|
| B15-01 | **P0** | Source aliases are hardcoded in `DefaultXauAliases` | `SymbolNormalizer.cs` 12–16, 25–27 |
| B15-02 | **P0** | Prefix heuristic silently maps any compact `XAUUSD*` and compact `GOLD` | lines 55–64; A44 §5.2 / §13 forbid |
| B15-03 | **P0** | Catalog is global, not `(BrokerId, key)` | `TryMapSource(string)` only; A01 / A44 |
| B15-04 | **P0** | Compiled set includes A44-forbidden tokens (`GOLD.`, `XAUUSD.PRO`, `XAUUSDPRO`, `XAUUSD.I`, `XAUUSD.S`, `GOLD.A`) | same HashSet |
| B15-05 | **P0** | Persist tables unused; seeder does not insert mappings | `SourceSymbolMappings` DbSet only; `DemoSeeder` canonical row only |
| B15-06 | **P1** | Dot/space strip rewrites lookup keys | line 55; A44 §4 / A89 G8 |
| B15-07 | **P1** | Unmapped source is written as `CanonicalSymbol` | `TradeReconstructor` 207–213 |
| B15-08 | **P1** | Unit tests lock the hardcoded happy path; no fail-closed cases | `SymbolNormalizerTests` |
| B15-09 | **P2** | `XAUUSDpro` duplicate of `XAUUSDPRO` under ignore-case | HashSet line 15 |
| B15-10 | — | Venue IDs are **not** hardcoded in this class | empty dict; no numeric literals |
| B15-11 | **P2** | Venue map is in-memory, unscoped, unvalidated numeric | lines 74–81; blocks Phase 4 even though hardcode check passes |
| B15-12 | info | Canonical code `"XAUUSD"` on `CanonicalInstrumentRef` is the identity, not an alias | allowed |

---

## 7. What “never hardcoded” would look like (not implemented here)

This note does not implement. When a coding task is authorized, A44 §8 / §14 already named the shape:

1. Empty in-memory maps at construction.
2. Seed **rows** for Achiever + StarwaveFX only: `XAUUSD`, `XAUUSD.`, `XAUUSDM`, `GOLD`, `XAUUSD.A`. Operator-disable per broker.
3. `TryMapSource(brokerId, raw)` → persist lookup; miss = explicit unmapped; **no** `StartsWith`.
4. Venue: `TryMapVenueInstrumentId(venueId, numericId)` against persisted `destination_symbols` after SecurityList; **zero** compile-time longs.
5. Tests: current five aliases **given seed rows**; unknown suffixes false; `"123456"` / `"XAUUSD"` as venue tokens false until register/discover.

Until then, `A100` G04 stays **FAIL**.

---

## 8. Honesty metrics

| Metric | Value |
|---|---|
| File reviewed | `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` (83 lines) |
| Hardcoded source alias tokens | **12** (11 unique ignore-case keys) |
| Extra vs §16 catalog | **6** forbidden / unlisted (`I`, `S`, `PRO`, `PRO` compact, `GOLD.`, `GOLD.A`) |
| Prefix / GOLD heuristic | **present** (lines 59–64) |
| Hardcoded venue / Pepperstone / RoE instrument IDs in this file | **0** |
| Production callers of `RegisterVenueInstrument` / `venueIdMappings` | **0** |
| Production load of `source_symbol_mappings` into the mapper | **0** |
| Unit tests asserting fail-closed unknown aliases | **0** |
| Unit tests asserting venue id is not guessed | **1** (fixture `123456`) |
| Product source modified | **0** |

**Aliases never hardcoded:** false.  
**Venue IDs never hardcoded (in `SymbolNormalizer`):** true.  
**Standing check:** **FAIL**.
