# A44 — CanonicalInstrument XAUUSD mappings

**Artifact:** `D:\Prop\reports\swarm\20260818\A44_symbol_normalization.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§16, 30  
**Supporting sections:** §6, §10, §14–15, §31, §38, §44–45, §52, §60, §67 Phase 2, §69.4 / §69.10, §72.13–14  
**Supporting swarm notes:** `A01_domain_audit.md`, `A09_unit_tests_audit.md`, `A23_risk_engine_spec.md`, `A27_test_inventory.md`, `A28_phases_gates.md`, `A32_ctrader_fix_specification.md`, `A34_ctrader_fix_faq.md`  
**Date:** 2026-08-18  
**Status:** design only — **product source not modified**  
**Scope:** map source strings `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `GOLD` and numeric cTrader instrument IDs onto one `CanonicalInstrument` (`XAUUSD`). Architecture-listed sibling `XAUUSD.a` is included so §16 is complete.

---

## 1. Binding rules (do not weaken)

From architecture §16:

```text
CanonicalInstrument
  XAUUSD

broker/source symbol → canonical XAUUSD
cTrader instrument ID → canonical XAUUSD

Never assume FIX tag 55 is the string "XAUUSD".
Persist this mapping.
```

From architecture §30 (TRADE session, after logon):

```text
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

From architecture §72.13 / A34: **discover** cTrader IDs; **do not guess**; **do not hardcode** an ID from another cTrader account, broker, environment, or the RoE sample book.

From official cTrader RoE (quoted in A32 / A34): tag `55` (`Symbol`) is a **Spotware numeric instrument identifier** (`Long` / `Integer`). The human ticker is custom tag `1007` (`SymbolName`). Official reject text: `Expected numeric symbolId, but got CS8260`. Official sample: `55=1` is `EURUSD`, `55=39` is `NZDCHF` — IDs are not a universal majors table.

---

## 2. Honest current state (measured 2026-08-18)

| Item | Path / evidence | Status |
|---|---|---|
| `CanonicalInstrument` thin record | `D:\Prop\src\Domain\Entities\CanonicalInstrument.cs` (`Id`, `Symbol`, `Description`) | scaffold only |
| `SourceSymbolMapping` thin record | `D:\Prop\src\Domain\Entities\SourceSymbolMapping.cs` (`Id`, `BrokerId`, `SourceSymbol`, `CanonicalInstrumentId`) | scaffold only |
| `DestinationSymbol` / `DestinationSymbolMapping` | none under `src/` | **MISSING** |
| `CanonicalSymbol` / `DestinationInstrumentId` value objects | none | **MISSING** |
| `ISymbolNormalizer` / `CanonicalInstrumentMapper` | none in Domain or Application | **MISSING** |
| SecurityList discovery | none in `src/Fix.CTrader` | **MISSING** |
| Tables `canonical_instruments`, `source_symbol_mappings`, `destination_symbols` | no EF / SQL / migrations | **MISSING** |
| `XauCanonicalMappingTests` | `A09` / `A27` inventory | **MISSING** |
| Hardcoded Pepperstone XAU ID in product source | grep of `src/` | none found (correct; keep it that way) |

`ReconstructedTrade` already has both `CanonicalSymbol` and `SourceSymbol` string fields. That is the right split. Mapping must fill `CanonicalSymbol`; reconstruction must not invent it from a string contains-`XAUUSD` check.

This note designs the mapping. It does **not** implement it.

---

## 3. Identity model

Three identities. Never collapse them.

| Identity | Lives on | Example | Equality |
|---|---|---|---|
| `CanonicalInstrument` / `CanonicalSymbol` | platform | `XAUUSD` | exact `XAUUSD` |
| `SourceSymbol` | `(BrokerId, raw MT5 symbol)` | `XAUUSDm` on Achiever | broker + exact raw string (lookup uses a derived key) |
| `DestinationInstrumentId` | `(ExecutionVenueId, Spotware long)` | `55=184467…` on Pepperstone account N | venue + numeric ID |

Aliases (`XAUUSD.`, `XAUUSDm`, `GOLD`, `XAUUSD.a`) live **only** in `SourceSymbolMapping` (and, during destination discovery, as matchers against tag `1007`). They must never become the canonical symbol, never be written to FIX tag `55`, and never be treated as a second instrument for first-3-trade counting or XAU exposure.

cTrader numeric IDs live **only** in `DestinationSymbol`. They must never be stored as `"XAUUSD"` and must never be copied from another venue row.

### 3.1 Seed canonical instrument

One row. One symbol. No alias column.

| Field | Value |
|---|---|
| `symbol` | `XAUUSD` (invariant, uppercase, no suffix) |
| `description` | `Gold vs US Dollar (canonical)` |
| `id` | stable well-known GUID chosen at first migration (constant in Domain, e.g. `CanonicalInstrument.XauUsdId`) |

v1 seeds **only** `XAUUSD`. `XAGUSD`, `XAUEUR`, `BTCUSD` are out of scope. An unknown source symbol does **not** create a new canonical instrument.

### 3.2 Recommended types (names locked to A01 / A27)

| Type | Layer | Kind | Responsibility |
|---|---|---|---|
| `CanonicalSymbol` | Domain | value object | wraps `"XAUUSD"`; factory `CanonicalSymbol.XauUsd`; rejects empty / whitespace / lowercase / aliases |
| `SourceSymbol` | Domain | value object | raw broker string + `LookupKey` |
| `DestinationInstrumentId` | Domain | value object | `long` ≥ 1; parse from FIX `55`; reject non-numeric |
| `CanonicalInstrument` | Domain | entity | seed row |
| `SourceSymbolMapping` | Domain | entity | `(BrokerId, lookup key) → CanonicalInstrumentId` |
| `DestinationSymbol` | Domain | entity | `(VenueId, instrument id) → CanonicalInstrumentId` + `1007` + `1008` |
| `ISymbolNormalizer` | Domain port | interface | resolve source / dest → `CanonicalSymbol` or explicit miss |
| `CanonicalInstrumentMapper` | Application | service | implements the port using persisted mappings + seed catalog |
| `IDestinationInstrumentDiscovery` | Application port | interface | SecurityList persist/refresh |

Keep existing entity files where they already sit (`Domain\Entities\`). Do not relocate them in the first implementation change. Add `DestinationSymbol` next to them. Value objects belong under `Domain\ValueObjects\` when implemented.

---

## 4. Lookup-key normalization (source strings and tag `1007` names)

Used **only** to find a mapping row. The original broker / venue string is still persisted.

```text
NormalizeLookupKey(raw):
  if raw is null or whitespace → empty key (result = UNMAPPED)
  trim Unicode whitespace
  ToUpperInvariant()
  do NOT strip trailing '.'
  do NOT strip 'M', '.A', '#', suffixes, prefixes
  do NOT remove digits
  do NOT apply Unicode compatibility folding beyond invariant upper
```

| Raw (as ingested) | Lookup key | Seed-mapped to XAUUSD? |
|---|---|---|
| `XAUUSD` | `XAUUSD` | **yes** |
| `xauusd` | `XAUUSD` | yes (same key) |
| ` XAUUSD ` | `XAUUSD` | yes |
| `XAUUSD.` | `XAUUSD.` | **yes** (distinct key) |
| `XAUUSDm` | `XAUUSDM` | **yes** |
| `XAUUSDM` | `XAUUSDM` | yes (same key) |
| `GOLD` | `GOLD` | **yes** |
| `gold` | `GOLD` | yes |
| `XAUUSD.a` | `XAUUSD.A` | **yes** (architecture §16 sibling) |
| `XAUUSD.A` | `XAUUSD.A` | yes |

`SourceSymbolMapping` stores both:

- `source_symbol` — exact string from MT5 (preserve broker case/suffix for tick subscribe and audit)
- `source_symbol_key` — `NormalizeLookupKey(source_symbol)`
- unique `(broker_id, source_symbol_key)`

If the same broker later emits both `XAUUSDm` and `XAUUSDM`, they share one mapping row. If it emits `XAUUSD` and `XAUUSD.`, they are two rows, both pointing at the same canonical.

---

## 5. Source catalog (the requested aliases)

These are **seed rows**, not implicit global heuristics. Each enabled source broker (Achiever, StarwaveFX, later brokers) gets the same catalog copied at broker-register time. An operator may disable or add rows per broker. A seed is not a license to substring-match.

### 5.1 Required seed keys → `CanonicalInstrument.XAUUSD`

| # | Architecture / request | Lookup key | Typical meaning | Canonical | Counts toward first 3 XAU trades? | Notes |
|---|---|---|---|---|---|---|
| 1 | `XAUUSD` | `XAUUSD` | standard gold/USD | `XAUUSD` | yes | identity alias |
| 2 | `XAUUSD.` | `XAUUSD.` | trailing-dot suffix used by some MT5 books | `XAUUSD` | yes | the dot is part of the key |
| 3 | `XAUUSDm` | `XAUUSDM` | micro-lot gold/USD | `XAUUSD` | yes | **same instrument, different contract size** |
| 4 | `GOLD` | `GOLD` | broker ticker for gold/USD | `XAUUSD` | yes | exact `GOLD` only |
| 5 | `XAUUSD.a` (§16) | `XAUUSD.A` | letter suffix book | `XAUUSD` | yes | include so §16 is complete |

Identity mapping is **not** quantity mapping. `XAUUSDm` vs `XAUUSD` share `CanonicalSymbol` so scoring, first-3, and XAU exposure see one book. Lot → notional → destination `OrderQty` uses `mt5_symbols` contract size / `destination_symbols` step (architecture §38). Never treat 1.00 `XAUUSDm` lots as 1.00 destination units.

### 5.2 Must **not** auto-seed (fail closed until an operator row exists)

| Raw / key | Why it is not automatic |
|---|---|
| `XAUEUR`, `XAUGBP`, `XAUAUD`, `XAUJPY` | different quote currency |
| `XAGUSD`, `SILVER` | not gold |
| `GOLD.` , `GOLDM`, `GOLD.m`, `GOLDmicro`, `GOLDUSD`, `USDGOLD` | not the listed exact alias |
| `XAUUSD.pro`, `XAUUSD#`, `XAUUSD+`, `XAUUSD.c`, `XAUUSD.m` | extra suffix; add a row only after broker confirmation |
| `XAUUSDmicro`, `XAUUSD.micro` | not `XAUUSDm` |
| `XAUUSDpro`, `XAUUSDecn` | possibly a second gold contract |
| `CS6407_01_XAUUSD` (RoE-style synthetic) | destination discovery may see this on `1007`; never a source seed |
| any string that merely **contains** `XAUUSD` or `GOLD` | substring matching is forbidden |

If Achiever lists both `XAUUSD` and `GOLD` as **distinct** contracts, both seed-map to the same canonical (same economic metal/USD). Exposure and sizing still use each symbol’s contract metadata from `mt5_symbols`. If a broker later proves `GOLD` is a different product (e.g. gram vs ounce with no reliable contract size), disable that broker’s `GOLD` row — do not invent a second canonical in v1.

### 5.3 Source resolve algorithm

```text
TryMapSource(brokerId, rawSymbol) → Result<CanonicalSymbol, Reason>

1. key = NormalizeLookupKey(rawSymbol)
   if key empty → UNMAPPED (SOURCE_SYMBOL_EMPTY)
2. load SourceSymbolMapping where broker_id = brokerId
                                 and source_symbol_key = key
                                 and is_enabled
   if found → CanonicalSymbol for that CanonicalInstrumentId
              (v1: must be XAUUSD; unknown canonical id → CANONICAL_UNKNOWN)
3. else → UNMAPPED (SOURCE_SYMBOL_UNMAPPED)
   never fall through to "looks like gold"
```

No in-memory regex. No `StartsWith("XAU")`. No default `XAUUSD`.

Ingest path: persist the **raw** deal/position/tick symbol unchanged. Mapping is applied when building `ReconstructedTrade.CanonicalSymbol`, when deciding whether a tick belongs on the XAU book, and when a copy intent is created.

Tick subscribe (A17): subscribe the **union of enabled source aliases for that broker**, not the string `"XAUUSD"` only. Otherwise `XAUUSDm` / `GOLD` open positions have no source ticks.

---

## 6. Destination catalog (numeric cTrader instrument IDs)

There is **no seed numeric ID**. Pepperstone XAUUSD is discovered per execution venue / account / environment.

### 6.1 What a destination row is

`destination_symbols` (architecture §44 / §45) — one active XAUUSD row per venue:

| Column | Source | Notes |
|---|---|---|
| `execution_venue_id` | `execution_venues` | Pepperstone / cServer account, not a source `BrokerId` |
| `instrument_id` | SecurityList tag `55` | `bigint`, Spotware id |
| `symbol_name` | tag `1007` | display + discovery matcher |
| `symbol_digits` | tag `1008` | RoE: 0–5; price precision, **not** lot size |
| `canonical_instrument_id` | mapper | `XAUUSD` |
| `mapping_status` | discovery | `DISCOVERED` / `CONFIRMED` / `STALE` / `AMBIGUOUS` / `UNMAPPED` |
| `discovered_at` / `last_seen_at` | clock | |
| `is_active` | | at most one active XAUUSD per venue |

Unique: `(execution_venue_id, instrument_id)`.  
Partial unique: one `is_active = true` row per `(execution_venue_id, canonical_instrument_id)`.

### 6.2 Parse rules for tag `55`

| Input | Result |
|---|---|
| integer / long token, value ≥ 1 | `DestinationInstrumentId` |
| `"XAUUSD"`, `"GOLD"`, `"EURUSD"` | **reject** `FIX_SYMBOL_NOT_NUMERIC` — never coerce to canonical |
| `0`, negative, empty, decimal | reject |
| RoE sample `1` (EURUSD in the published book) | valid **id type**, but **not** XAUUSD unless this venue’s `1007` says so |

`TryMapDestinationInstrumentId(venueId, id)` looks up the persisted row. It does **not** interpret the number. Unknown id → `DESTINATION_INSTRUMENT_UNMAPPED`. Stale row → `DESTINATION_INSTRUMENT_STALE`. Neither may be used on `35=D` or `35=V`.

### 6.3 SecurityList discovery (§30) — TRADE session only

A32: SecurityList Request/List are TRADE (`57=TRADE` outbound). Do not discover on QUOTE.

```text
TRADE logon complete
        ↓
35=x SecurityListRequest
     320 = new SecurityReqID
     559 = 0          (only supported type)
     55  = omitted    (full book; A34: omitting 55 is how 146=143 is obtained)
        ↓
35=y SecurityList
     560 must be 0 (valid)
     repeating 146 × { 55, 1007, 1008 }
        ↓
match each 1007 through NormalizeLookupKey + destination name catalog
        ↓
persist / refresh destination_symbols
        ↓
QUOTE may subscribe 35=V using the persisted numeric 55
TRADE may send 35=D using the same numeric 55
```

Targeted `35=x` with `55=<id>` only **resolves a name for an already-known id**. It cannot find XAUUSD the first time. First useful version (§69.10) must request the full list.

If `560 ≠ 0` (invalid / none / unauthorised / unavailable / unsupported): leave previous row in place, mark discovery failed, **do not** guess. Venue health: `XAUUSD mapped? = false` until a successful refresh confirms an active row.

### 6.4 Matching `1007` to canonical XAUUSD

Apply the **same lookup keys** as §5.1 against `NormalizeLookupKey(1007)`:

```text
XAUUSD | XAUUSD. | XAUUSDM | GOLD | XAUUSD.A
```

Preference when several instruments in **one** SecurityList match:

1. Exact key `XAUUSD` wins.
2. Else exact `XAUUSD.`
3. Else exact `XAUUSDM`
4. Else exact `XAUUSD.A`
5. Else exact `GOLD`
6. Else if still more than one distinct `55` → `AMBIGUOUS` (persist candidates, activate none)
7. Else if zero matches → `UNMAPPED`

Rationale: the destination book we copy onto should be the venue’s primary XAUUSD contract. `GOLD` is a fallback name, not a second live instrument. Two active gold contracts on the destination is an operator problem, not an auto-pick.

Do **not** treat RoE synthetics (`CS6407_01_EURUSD` style) as XAUUSD unless `1007` normalizes to a catalog key. Do **not** treat `contains("XAU")` as a match.

### 6.5 Refresh / stale rules

On every successful TRADE SecurityList:

| Previous active row | This list | Action |
|---|---|---|
| none | one preferred match | insert `DISCOVERED`, set active |
| same `instrument_id`, same `1007` | present | bump `last_seen_at`; keep active |
| same `instrument_id`, `1007` changed to another catalog key | present | update name/digits; keep id; audit |
| same `instrument_id`, `1007` no longer a catalog key | present | `STALE`; clear active; **block** new XAU execution |
| different `instrument_id` now preferred | old id absent or demoted | deactivate old (`STALE`); activate new `DISCOVERED`; audit. Open destination positions still keyed by old id must reconcile — do not rewrite live `fix_orders.symbol` |
| previous id absent, no new match | | `STALE`; `XAUUSD mapped? = false` |

Never copy venue A’s `instrument_id` onto venue B. Demo vs live are different venues.

### 6.6 What FIX messages consume

| Message | Tag 55 value |
|---|---|
| `35=V` / `35=W` / `35=X` (QUOTE) | persisted numeric XAUUSD id |
| `35=D` NewOrderSingle (TRADE) | same numeric id |
| `35=8` ExecutionReport inbound | parse numeric; join `destination_symbols`; unknown id → reconcile alert, not “must be XAUUSD” |
| Dashboard §52 “Instrument ID” | the persisted number |
| Dashboard §52 “XAUUSD mapped?” | `true` iff active non-stale, non-ambiguous row exists |

CopyIntent / RiskDecision / ExecutionIntent store `canonical_symbol = XAUUSD` **and** (when destination-bound) `destination_instrument_id`. Risk A23: `canonical_symbol` must be mapped; `symbol_id` on the quote snapshot is the cTrader id. Missing either is `INTENT_INCOMPLETE` / `QUOTE_UNAVAILABLE`.

---

## 7. Persistence (projection of this design onto §45)

No migrations in this change-set. Column contract for when Infrastructure implements:

### `canonical_instruments`

| Column | Type | Constraint |
|---|---|---|
| `id` | uuid | PK |
| `symbol` | text | unique, check `symbol = upper(symbol)` |
| `description` | text null | |
| `created_at` | timestamptz | |

Seed: one row `XAUUSD`.

### `source_symbol_mappings`

| Column | Type | Constraint |
|---|---|---|
| `id` | uuid | PK |
| `broker_id` | uuid | FK `brokers` |
| `source_symbol` | text | raw MT5 |
| `source_symbol_key` | text | `NormalizeLookupKey`; unique with `broker_id` |
| `canonical_instrument_id` | uuid | FK |
| `mapping_origin` | text | `SEED` / `OPERATOR` |
| `is_enabled` | bool | default true |
| `created_at` / `updated_at` | timestamptz | |

### `destination_symbols`

| Column | Type | Constraint |
|---|---|---|
| `id` | uuid | PK |
| `execution_venue_id` | uuid | FK `execution_venues` |
| `instrument_id` | bigint | tag 55; `> 0` |
| `symbol_name` | text | tag 1007 as received |
| `symbol_name_key` | text | normalized `1007` |
| `symbol_digits` | smallint | 0–5 |
| `canonical_instrument_id` | uuid | FK; null while `AMBIGUOUS`/`UNMAPPED` candidates |
| `mapping_status` | text | see §6.1 |
| `is_active` | bool | |
| `discovered_at` / `last_seen_at` | timestamptz | |

`mt5_symbols` (separate table) holds per-broker contract size / volume step for **source** sizing. It is not a substitute for these two mapping tables.

---

## 8. Application / FIX ports (not implemented)

```text
ISymbolNormalizer
  TryMapSource(BrokerId, SourceSymbol)
      → MappingResult<CanonicalSymbol>
  TryMapDestinationId(ExecutionVenueId, DestinationInstrumentId)
      → MappingResult<CanonicalSymbol>
  IsXauUsd(CanonicalSymbol) → bool

ISourceSymbolMappingCatalog
  GetEnabledAliases(BrokerId) → IReadOnlyList<SourceSymbol>
      // tick subscribe + seed materialization

IDestinationInstrumentDiscovery
  RequestFullSecurityList(TradeSession, SecurityReqID)
  ApplySecurityList(ExecutionVenueId, SecurityList)
      → DestinationDiscoveryResult
  GetActiveXau(ExecutionVenueId)
      → DestinationSymbol | none
```

`MappingResult` is explicit: `Mapped | Unmapped | Stale | Ambiguous | InvalidToken`. Callers must not `.GetValueOrDefault(XAUUSD)`.

`CanonicalInstrumentMapper` loads mappings from the store (cache allowed; Postgres is source of truth). Seed catalog is applied **only** by a broker-provisioning use case that inserts `SourceSymbolMapping` rows — not by the hot-path mapper inventing aliases.

`SecurityListHandler` (Fix.CTrader) parses `35=y` and calls `ApplySecurityList`. It never writes FIX. It never assumes `55=1` is anything.

---

## 9. Pipeline wiring (consumers of the map)

```text
MT5 deal/position/tick (raw symbol)
        ↓
ISymbolNormalizer.TryMapSource
        ↓
unmapped → keep raw, exclude from XAU reconstruction / first-3 / XAU ticks
mapped   → ReconstructedTrade.CanonicalSymbol = XAUUSD
             ReconstructedTrade.SourceSymbol  = raw
        ↓
CopyIntent.canonical_symbol = XAUUSD
        ↓
RiskEngine (A23): require canonical XAUUSD + destination quote.symbol_id
        ↓
ExecutionIntent: canonical_symbol + destination_instrument_id
        ↓
FIX 35=D / 35=V tag 55 = destination_instrument_id  (numeric)
```

First-3 counter (`A27` `FirstThreeCompletedXauTradesTests`): count only `completed && CanonicalSymbol == XAUUSD`. A completed `EURUSD` lifecycle and an unmapped `GOLD.` lifecycle do not increment.

Risk XAU gross/net exposure: sum destination (or source-notional) quantities whose canonical is `XAUUSD`, regardless of whether the source print was `GOLD` or `XAUUSDm`.

---

## 10. Worked examples

### 10.1 Source

| Broker | Raw symbol | Result | Reason |
|---|---|---|---|
| Achiever | `XAUUSD` | `XAUUSD` | seed key |
| Achiever | `XAUUSD.` | `XAUUSD` | seed key |
| Achiever | `XAUUSDm` | `XAUUSD` | seed key `XAUUSDM` |
| Achiever | `GOLD` | `XAUUSD` | seed key |
| StarwaveFX | `gold` | `XAUUSD` | same key after normalize |
| Achiever | `XAUEUR` | UNMAPPED | different quote currency |
| Achiever | `GOLD.` | UNMAPPED | not exact `GOLD` |
| Achiever | `XAUUSD.pro` | UNMAPPED | extra suffix |
| Achiever | `XAUUSD.a` | `XAUUSD` | §16 sibling seed |
| *(any)* | `""` | UNMAPPED | empty |

### 10.2 Destination (illustrative IDs only — **not** Pepperstone production)

These numbers are **test fixtures**. They must never be copied into `appsettings` as “the” XAUUSD id.

| Venue | SecurityList fragment | Active map |
|---|---|---|
| Venue A | `55=41\|1007=XAUUSD\|1008=3` | `41 → XAUUSD` |
| Venue A | also `55=77\|1007=GOLD\|1008=2` | `GOLD` ignored; `41` wins preference |
| Venue B | only `55=9001\|1007=GOLD\|1008=3` | `9001 → XAUUSD` (fallback) |
| Venue C | `55=12\|1007=XAUUSD\|1008=3` and `55=13\|1007=XAUUSD.\|1008=3` | prefer `12` |
| Venue D | `55=1\|1007=EURUSD\|1008=5` (RoE sample shape) | XAU **unmapped** — `1` is not gold |
| any | inbound `35=D` built with `55=XAUUSD` | invalid; do not send |
| Venue A after refresh loses `41` | | row `STALE`; mapped? = false |

§69.10 “Discover the Pepperstone XAUUSD instrument ID” is satisfied only when a **measured** SecurityList from that account has been persisted. A stub replay (`A27` `SecurityListReplayStub`) is the test stand-in.

---

## 11. Tests required (lock this design)

From `A09` / `A27`. Still missing. When written they must prove:

### `Mapping.XauCanonicalMappingTests` → `CanonicalInstrumentMapper`

| Method | Must prove |
|---|---|
| `Maps_XAUUSD_variants_and_GOLD_to_canonical` | `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD` (any listed case) → `CanonicalInstrument` / `CanonicalSymbol.XauUsd` **given seed rows for that broker** |
| `Maps_cTrader_instrument_id` | fixture numeric id → `XAUUSD`; string `"XAUUSD"` as `55` does **not** map |
| `Unknown_symbol_does_not_silently_become_XAUUSD` | `EURUSD`, `XAUEUR`, `GOLD.`, `XAUUSD.pro`, empty → unmapped |

### `Mapping.SourceSymbolMappingTests` → `SourceSymbolMapping`

| Method | Must prove |
|---|---|
| per-broker isolation | Achiever `GOLD` mapped does not map StarwaveFX `GOLD` unless StarwaveFX has its own row |
| missing mapping is explicit fail | no pass-through of raw symbol as canonical |
| `XAUUSD` vs `XAUUSD.` | two keys, both canonical XAUUSD |

### `Mapping.DestinationInstrumentMappingTests` → `DestinationSymbol`

| Method | Must prove |
|---|---|
| numeric id → canonical | after discovery apply |
| no hardcoded foreign-account id | mapper has zero compile-time Pepperstone constants |
| ambiguous list | `XAUUSD` + second equal-preference clash → no active row |
| stale id | deactivated id cannot be used for NOS / MD |

### `Harness.SecurityListXauDiscoveryTests`

Replay a recorded `35=y` (or `SecurityListReplayStub`). Persist id + `1007` + `1008`. Assert dashboard/query “XAUUSD mapped?” becomes true. Assert a second venue with a different id does not overwrite the first.

---

## 12. Phase gates this design unblocks

| Gate | Mapping obligation |
|---|---|
| Phase 2 — source symbol mappings (`A28`) | seed §5.1 per Achiever and StarwaveFX; unit tests green; verified against **real** broker symbol lists before claiming Phase 2 exit |
| §69.4 Capture XAUUSD trades correctly | reconstruction only after `TryMapSource` hits |
| §69.6 First 3 completed XAUUSD trades | filter on canonical, not raw |
| Phase 4 / §69.10 Pepperstone instrument ID | SecurityList persist; no guessed `55` |
| §52 QUOTE card | `XAUUSD mapped?`, Instrument ID, bid/ask |
| Live `35=D` | tag 55 = persisted long only |

Phase 2 exit does **not** require destination IDs. Destination discovery is Phase 4 / first-useful item 10. Do not block reconstruction on FIX.

---

## 13. Anti-patterns (reject in review)

| Anti-pattern | Violates |
|---|---|
| `if (symbol.Contains("XAU") \|\| symbol == "GOLD") canonical = "XAUUSD"` | §16 fail-closed |
| `fix[55] = "XAUUSD"` or parse tag 55 as a ticker | §16, RoE, A34 |
| Hardcode `instrument_id = 1` (RoE sample is EURUSD) or any blog/Pepperstone screenshot | §30, §72.13 |
| Share one destination id across demo/live or two logins | §30 |
| Treat `XAUUSDm` as a different canonical (drops micros from first-3 / exposure) | §16 catalog |
| Treat `XAUUSDm` lots as destination `OrderQty` | §38 / §72.14 |
| Auto-map `GOLD.` / `XAUEUR` | §5.2 |
| Create canonical rows from every unknown MT5 symbol | XAUUSD-first |
| Discover SecurityList on the QUOTE session | A32 session split |
| Send `35=x` with `55=XAUUSD` to “search by name” | RoE: `55` is an id; `559=0` is not a name search |
| Silent default when mapping table is empty | §16 persist + fail closed |
| Put alias list on `CanonicalInstrument` itself | A01 invariant |

---

## 14. Implementation sequence (when product work is authorized)

Not done by this note.

1. Value objects `CanonicalSymbol`, `SourceSymbol`, `DestinationInstrumentId`.
2. Extend `SourceSymbolMapping` with `source_symbol_key`, `is_enabled`, `mapping_origin`. Add `DestinationSymbol`.
3. `ISymbolNormalizer` + `CanonicalInstrumentMapper` + in-memory catalog for unit tests.
4. `XauCanonicalMappingTests` / `SourceSymbolMappingTests` / `DestinationInstrumentMappingTests`.
5. Infrastructure tables + seed `XAUUSD` + per-broker alias copy for Achiever and StarwaveFX.
6. Wire reconstruction + tick subscribe to the mapper.
7. `SecurityListHandler` + `IDestinationInstrumentDiscovery` on TRADE logon.
8. FixWorker: QUOTE subscribe and NOS read id only from `destination_symbols`.

---

## 15. Honesty metrics

| Metric | Value |
|---|---|
| Architecture aliases specified (§16) | `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD` |
| Aliases designed in this note | those five + numeric cTrader ids |
| Destination numeric IDs hardcoded | **0** (forbidden) |
| Product files changed by this agent | **0** |
| Mapper / discovery implemented | **no** |
| Tests implemented | **no** |
| Ready to claim “XAUUSD mapped” in production | **no** |

**Phase 2 mapping design:** specified. **Phase 2 mapping implementation:** not started.
)