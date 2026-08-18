# D88 — `Broker.Code` vs Guid `BrokerId` consistency

| Field | Value |
|---|---|
| Agent | D88 (identity: catalog code vs persist Guid) |
| Date | 2026-08-18 |
| Assigned | Broker.Code vs Guid BrokerId consistency. Write this report. Do not modify product source. |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Method | Full read of catalog, persist, ingest, reconstruct, dashboard, seed, connectors, API, web pages, unit/integration tests. SHA-256 of 17 identity-bearing files. PowerShell SHA-256 → first-16-bytes `Guid` for `DeterministicGuid.FromString` inputs. Grep of `BrokerId` / `BrokerCode` / `Broker.Code` under `src`, `apps`, `tests`. |
| Binding law | Architecture v2 §10 (compound identity always includes `broker_id`); A20 (`brokers.id` **defines** `broker_id`; `brokers.code` UNIQUE); A58 §3 (target: lowercase code + `DeterministicGuid("broker:{code}")`); A57 hazard #1 (Guid vs string `broker_id`) |
| Siblings | `A20` (table catalog), `A57` (FUV identity hazard), `A58` (registry + CatalogId), `D19`/`D20`/`D21` (schema / store / queries), `D22` (seed Guids), `D25` (connector Guid vs code) |

**Honesty rule:** a working demo path that *happens* to pass the same uppercase literals everywhere is **not** “identity proven.” Homonymous `BrokerId` (Guid in persist, string **code** in domain) is a footgun until names, case, and FKs match. A58 CatalogIds were **not** the IDs that got seeded.

---

## 0. Verdict

**PARTIAL. The live ingest → reconstruct → dashboard path is internally consistent *if and only if* every caller uses `BrokerCodes` (`ACHIEVER` / `STARWAVEFX`) and lets `ResolveBrokerIdAsync` stamp persist rows.** The **names** are not consistent. The **specs** are not consistent. The **IDs** are not the ones A58 required.

| Question | Measured answer |
|---|---|
| Does persist `BrokerId` mean `brokers.id` (uuid)? | **Yes** on 11 child entities |
| Does domain `BrokerId` mean `brokers.id`? | **No.** It is the **code string** (`"ACHIEVER"`) |
| Is there a single translation function? | **Yes, one:** `ITradingStore.ResolveBrokerIdAsync(code) → Guid` plus `LoadDealsAsync` rewriting `NormalizedDeal.BrokerId = brokerCode` |
| Are seed Guids = `DeterministicGuid("broker:achiever")`? | **No.** Seed uses `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1/2`. `DeterministicGuid` has **zero callers** |
| Are codes lowercase as A58? | **No.** Product + A20 examples are `ACHIEVER` / `STARWAVEFX` |
| Is there an EF FK `*.BrokerId → brokers.id`? | **No** (`HasForeignKey` = 0) |
| Can a Guid/code mixup silently drop all trades? | **Yes.** `TradeReconstructor` filters `d.BrokerId == brokerId` (string). Wrong kind → empty book, no throw |
| Case-safe? | **No.** Resolve + detail-trade lookup are **ordinal/SQL exact**. Registry + recon + trader list are **ignore-case** |
| Frontend `types/index.ts` `brokerId` | **Stale.** Live pages use DTO `code` / `broker` (the **code**). Route param is *named* `brokerId` but carries the code |
| Safe to treat identity as closed? | **No** |

Classification vocabulary: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

| Slice | Class |
|---|---|
| `Broker.Id` (Guid PK) + `Broker.Code` (unique string) | `EXISTS_AND_GOOD` as a catalog shape |
| Persist children `Guid BrokerId` | `EXISTS_AND_GOOD` as §10 *type* |
| Domain `string BrokerId` on recon/risk/shadow | `EXISTS_NEEDS_REFACTOR` — **misnamed code** |
| `ResolveBrokerIdAsync` + `LoadDealsAsync` remap | `EXISTS_AND_GOOD` for the **current** happy path |
| Seed catalog Guids | `EXISTS_NEEDS_REFACTOR` vs A58 |
| `DeterministicGuid` | `EXISTS` and **unused** |
| EF FK to `brokers.id` | `MISSING` |
| Case / normalize policy | `UNSAFE` (mixed comparers) |
| `/api/trades?broker=` | `UNSAFE` — query param **ignored**; login-only |
| A58 CatalogId + lowercase code | `MISSING` in product |

**One-line:** persist Guid + API code is the right *split*; calling both `BrokerId` and seeding neither the A58 Guid nor a FK is the defect.

---

## 1. Two identifiers (do not collapse)

Architecture §10: never assume login or ticket is globally unique. Every source row must carry `broker_id`. A20: **`brokers.id` is that uuid.** `brokers.code` is the **human / config / URL** key.

| Kind | Canonical field | Runtime type | Example (product, 2026-08-18) | Example (A58 target) |
|---|---|---|---|---|
| Catalog PK | `Broker.Id` / persist `BrokerId` | `Guid` | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1` | `DeterministicGuid("broker:achiever")` = `076c2345-8a1d-e602-d030-c6fea7abf730` |
| Catalog code | `Broker.Code` / `BrokerCodes.*` | `string` (max 32, unique) | `ACHIEVER` | `achiever` |
| Compound account | `(BrokerId, Login)` | Guid + long | `(…aaa1, 10001)` | `(076c…f730, 10001)` |

These are **not interchangeable**. `Guid.Parse("ACHIEVER")` throws. `string.Equals(deal.BrokerId, broker.Id.ToString())` is false when `deal.BrokerId` is the code.

---

## 2. Inventory (measured)

### 2.1 Catalog

```3:8:D:\Prop\src\Domain\Entities\Broker.cs
public sealed class Broker
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
```

```3:7:D:\Prop\src\Domain\Brokers\BrokerCodes.cs
public static class BrokerCodes
{
    public const string Achiever = "ACHIEVER";
    public const string StarwaveFx = "STARWAVEFX";
}
```

`TraderDbContext`: table `brokers`, PK `Id`, **unique** `Code`, `HasMaxLength(32)`, required. No snake_case column rename (`Code` stays `Code` unless the provider quotes).

### 2.2 Persist children — `Guid BrokerId` (11)

| Entity | File | Used as natural-key part? |
|---|---|---|
| `Mt5Group` | `Domain\Entities\Mt5Group.cs` | unique `(BrokerId, Name)` |
| `Mt5Account` | `Mt5Account.cs` | unique `(BrokerId, Login)` |
| `Mt5Deal` | `Mt5Deal.cs` | unique `(BrokerId, DealTicket)` |
| `Mt5Position` | `Mt5Position.cs` | unique `(BrokerId, PositionTicket)` |
| `ReconstructedTrade` | `ReconstructedTrade.cs` | **non-unique** `(BrokerId, Login, PositionId, OpenedAt)` |
| `TraderScore` | `TraderScore.cs` | unique `(BrokerId, Login)` |
| `TraderScoreHistory` | `TraderScoreHistory.cs` | non-unique `(BrokerId, Login, RecordedAt)` |
| `CopyIntent` | `CopyIntent.cs` | not indexed by broker |
| `ShadowOrder` | `ShadowOrder.cs` | not indexed by broker |
| `SourceSymbolMapping` | `SourceSymbolMapping.cs` | unique `(BrokerId, SourceSymbol)` — **never written** |
| `SyncCheckpoint` | `SyncCheckpoint.cs` | unique `(BrokerId, Login, Stream)` — **never written** |

**No** `HasOne` / `HasForeignKey` / navigation to `Broker`. A20 §FK list is **not implemented**. An orphan Guid is a legal insert.

Entities that **do not** carry `BrokerId`: `Broker` itself (`Id`), `CanonicalInstrument`, `RiskDecisionRecord`, `ExecutionIntent`, `OutboxEvent` (string `AggregateId`), `DestinationQuoteSnapshot`, `FixSessionState`, `AuditLog`, `KillSwitch`.

### 2.3 Domain / application — `string BrokerId` that is actually the **code**

| Type | Property / param | Compared how | Consumed? |
|---|---|---|---|
| `NormalizedDeal` | `string BrokerId` | recon filter | Yes |
| `ReconstructedTradeResult` | `string BrokerId` | copied onto result; **not persisted** | Written, then discarded by store |
| `TradeReconstructor.Reconstruct` | `string brokerId` | `OrdinalIgnoreCase` vs deal | Yes — **empty result if mismatch** |
| `OpenTrade` (private) | `string BrokerId` | baked into `Id` | Yes |
| `RiskEvaluationRequest` | `string BrokerId` | — | **Never read** by `RiskEngine.Evaluate` |
| `ShadowPosition` | `string BrokerId` | — | **Never written** by `SimulateEntry` / `SimulateExit` |

Honestly named **code** surfaces (do **not** call these Guid):

| Surface | Name | Notes |
|---|---|---|
| `IMt5BrokerConnector.BrokerCode` | code | Fake + registry key (`OrdinalIgnoreCase`) |
| `ITradingStore.ResolveBrokerIdAsync(string brokerCode)` | code → Guid | exact `b.Code == brokerCode` |
| `DealIngestionService.SyncBrokerAsync(string brokerCode)` | code | |
| `ReconstructionScoringService.RebuildTraderAsync(string brokerCode)` | code | resolves Guid, reconstructs with **code** |
| `LoadDealsAsync(Guid brokerId, string brokerCode, …)` | **both** | filter Guid; emit code |
| Dashboard `BrokerStatusDto.Code`, `GroupRowDto.Broker`, `TraderRowDto.Broker` | code | |
| `IBrokerRegistry.Get(string brokerCode)` | code | ignore-case |

### 2.4 Draft / unused Guid-named options

| Surface | Type | Callers |
|---|---|---|
| `Mt5BrokerOptions.BrokerId` | `Guid` `[Required]` | **0** — DI never binds this type. No `Code` field |
| `Mt5BrokerEvent.BrokerId` | `Guid` | **0** — `IBrokerConnector` has **0** implementors (`D25`) |
| `DeterministicGuid.FromString` | Guid factory | **0** product callers |

A58 wanted `CatalogId` **on the connector**. Product connector has only `string BrokerCode`. Catalog Guid is resolved later from Postgres/InMemory.

---

## 3. The one working translation (happy path)

```16:20:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task<Guid> ResolveBrokerIdAsync(string brokerCode, CancellationToken ct)
    {
        var broker = await _db.Brokers.SingleAsync(b => b.Code == brokerCode, ct);
        return broker.Id;
    }
```

```79:84:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
```

```152:154:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        return rows.Select(d => new NormalizedDeal
        {
            BrokerId = brokerCode,
```

```29:31:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var scoped = deals
            .Where(d => string.Equals(d.BrokerId, brokerId, StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Login == login)
```

Then persist writes **Guid** again (`ReplaceReconstructedAsync`, `TraderScore.BrokerId`, `CopyIntent.BrokerId`, `ShadowOrder.BrokerId`). `ReconstructedTradeResult.BrokerId` (the code) is **dropped**.

Dashboard maps Guid → code for the wire:

```66:66:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            var code = brokers.TryGetValue(g.BrokerId, out var b) ? b.Code : g.BrokerId.ToString();
```

```89:94:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            if (!brokers.TryGetValue(s.BrokerId, out var b))
                continue;
            // …
                b.Code,
```

```131:137:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var b = await _db.Brokers.AsNoTracking().SingleOrDefaultAsync(x => x.Code == broker, ct);
        if (b is null)
            return new TraderDetailDto(header, Array.Empty<TradeHighlightDto>());

        var trades = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.BrokerId == b.Id && t.Login == login)
```

**This is the correct persist/API split** when the strings match. It is **not** documented as a type (`BrokerCode` vs `BrokerCatalogId`). A new call site that passes `broker.Id.ToString()` into `Reconstruct` will score an **empty** trader.

---

## 4. Seeded catalog (the only writer of `brokers.id`)

`DemoSeeder` (SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`, 140 / 5082):

| `Code` | `Id` (literal) | DisplayName | Server |
|---|---|---|---|
| `ACHIEVER` | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1` | Achiever | `57.128.141.65` |
| `STARWAVEFX` | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2` | StarwaveFX | `84.201.6.142` |

Guard: `if (await db.Brokers.AnyAsync(ct)) return;` — first writer wins. Later A58-style upsert **cannot** run through this seeder.

Same two strings are hard-coded in:

| Caller | Literal |
|---|---|
| `DemoBrokerFactory` Fake constructors | `"ACHIEVER"`, `"STARWAVEFX"` |
| `apps/mt5-worker/Worker.cs` | `BrokerCodes.Achiever` / `StarwaveFx` |
| `apps/api/Program.cs` `/api/ops/resync` | `"ACHIEVER"` / `"STARWAVEFX"` |
| `apps/api/Program.cs` `/api/health` | name `"ACHIEVER"` (Fake, not live) |
| `apps/api/Program.cs` `/api/settings` `brokerConfigs[].id` | **code**, not Guid |
| Unit tests | `BrokerId = "ACHIEVER"` on `NormalizedDeal` / `RiskEvaluationRequest` |
| Integration `Deal_upsert_is_idempotent` | seed `…aaa1` + `BrokerCodes.Achiever` |

**Current literals agree with each other.** They do **not** agree with A58.

---

## 5. Findings (consistency defects)

### F1 — Homonym: `BrokerId` means two types

Same identifier, two CLRs:

```text
Guid   BrokerId  =  brokers.id          // persist, options, unused event
string BrokerId  =  brokers.code        // NormalizedDeal, recon, risk request, shadow position
```

A57 hazard #1 is **still open**. Application *does* map at the store boundary (`LoadDealsAsync` takes both), but the domain property was **not** renamed to `BrokerCode`. Reviewers and future adapters will pass the wrong one.

**Class:** `EXISTS_NEEDS_REFACTOR`

### F2 — A58 CatalogId ≠ seeded Id (`DeterministicGuid` dead)

Computed with the **same** algorithm as `Mt5\Utils\DeterministicGuid.cs` (SHA-256 UTF-8, first 16 bytes → `Guid`):

| Input | Guid |
|---|---|
| `broker:achiever` (A58) | `076c2345-8a1d-e602-d030-c6fea7abf730` |
| `broker:ACHIEVER` | `d2c5ed53-8269-c19c-dfaf-f03feafc0d92` |
| `broker:starwavefx` (A58) | `31e329d0-e030-026b-aa22-953cc4f3d239` |
| `broker:STARWAVEFX` | `3a54a135-bea3-05c4-3c69-4943c50ca395` |
| `ACHIEVER` | `184a87fe-efc6-6494-d269-45481b26979a` |
| Seed Achiever | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1` |
| Seed StarwaveFX | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2` |

**Zero overlap.** A future worker that “just implements A58” and upserts `brokers.id = DeterministicGuid("broker:achiever")` with code `achiever` creates a **second catalog row**. Existing `mt5_*` / scores / copy intents stay on `…aaa1` and become **orphans** (no FK to stop it; dashboard **hides** orphan scores — F6).

If they keep code `ACHIEVER` and change only the Guid: unique `Code` blocks the insert; children still point at `…aaa1`.

**Class:** `UNSAFE` for any CatalogId migration without a rewrite of every child row.

### F3 — Spec vs spec: A20 uppercase vs A58 lowercase

| Doc | Stored `brokers.code` |
|---|---|
| A20 §5.1 | examples `ACHIEVER`, `STARWAVEFX` |
| A58 §3 | **lowercase** `achiever`, `starwavefx` |
| Product `BrokerCodes` + seed + Fake + worker + `/api/ops/resync` | `ACHIEVER`, `STARWAVEFX` |

Product matches **A20 examples**, not A58. Implementing A58 “as written” without a normalize function **breaks** `ResolveBrokerIdAsync` (exact `==`).

**Class:** `MISSING` single normalize law.

### F4 — Mixed comparers (case)

| Site | Comparison | `"achiever"` vs DB `ACHIEVER` |
|---|---|---|
| `ResolveBrokerIdAsync` | `b.Code == brokerCode` (CLR / PG default = **case-sensitive**) | **throws** `InvalidOperationException` (no row) |
| `GetTraderDetailAsync` broker lookup | `x.Code == broker` | **null** → header kept, **trades = []** |
| `GetTradersAsync` filter | `OrdinalIgnoreCase` | **keeps** the row |
| `TradeReconstructor` | `OrdinalIgnoreCase` | **keeps** deals |
| `BrokerRegistry` | `OrdinalIgnoreCase` | **finds** Fake connector |
| Unique index `brokers.Code` | provider collation | PG: `ACHIEVER` and `achiever` are **two** rows |

Concrete footgun: `GetTraderAsync("achiever", 10001)` (ignore-case list) can return a header, then `GetTraderDetailAsync` returns that header with **zero highlights**. Looks like “trader has no trades.”

**Class:** `UNSAFE`

### F5 — `/api/trades` ignores `broker` (login-only; Guid on the wire)

```63:70:D:\Prop\apps\api\Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await … ToListAsync(query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
});
```

- Query param `broker` is **bound and unused**.
- Filter is `login` alone — violates §10 the moment two brokers share a login (A58 identity law: Achiever `9904` ≠ StarwaveFX `9904`).
- Response is the **entity**: `brokerId` JSON is the **Guid**, not the code. Trade explorer does not even render it (`TradeExplorerPage` columns: login/symbol/dir/…).
- Demo tape hides this: Achiever logins `10001–10003`, Starwave `99001`.

**Class:** `UNSAFE`

### F6 — Orphan Guid handling is inconsistent (no FK)

| Reader | Missing `brokers` row for child `BrokerId` |
|---|---|
| `GetGroupsAsync` | **Shows** `g.BrokerId.ToString()` as the “broker code” |
| `GetTradersAsync` | **Skips** the score (`continue`) |
| `GetTraderDetailAsync` | If code lookup fails: header (if list found it) + **empty** trades |
| Store writes | Accept any Guid |

Groups page can display `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1` next to a real `ACHIEVER` row if a group is stamped with an unknown Guid. Leaderboard silently shrinks.

**Class:** `UNSAFE`

### F7 — Two trade identities

| Layer | Trade id |
|---|---|
| Domain `ReconstructedTradeResult.Id` | `"{code}:{login}:{positionId}:{unixMs}"` e.g. `ACHIEVER:10001:501:…` |
| Persist `ReconstructedTrade.Id` | `Guid.NewGuid()` **every rebuild** |
| `CopyIntent.SourceTradeId` | `Guid?` — **never set** in `PersistDemoShadowAsync` |
| Shadow idempotency key | `$"shadow:{guid}:{login}:{positionId}"` (Guid, not code) |

`ShadowPosition.SourceTradeId` is `string`; persist `CopyIntent.SourceTradeId` is `Guid?`. You cannot assign the recon id into the persist column without a new parse scheme.

**Class:** `EXISTS_NEEDS_REFACTOR`

### F8 — Connector / options identity split (`D25` recensus)

| Port | Broker identity |
|---|---|
| Live `IMt5BrokerConnector` | `string BrokerCode` only |
| Dead `IBrokerConnector.Mt5BrokerEvent` | `Guid BrokerId` |
| Dead `Mt5BrokerOptions` | `Guid BrokerId`, **no** `Code`, requires `RemoteUrl` |

Fake `ClosedRoundTrip(string broker, …)` **never writes** `broker` onto `Mt5DealDto` (DTO has no broker field). Stamping happens only in the store after resolve. C++ `mt5-sdk` still has **no** `broker_id` (A04 / A57) — correct for native rows; C# **must** stamp at the boundary. Today only the Fake path exists.

**Class:** live port `EXISTS_NEEDS_REFACTOR` vs A58 (`Code` + `CatalogId`); draft Guid surfaces `DEPRECATED`.

### F9 — Web types vs live pages vs route name

| Layer | Field | Value kind |
|---|---|---|
| `apps/web/src/types/index.ts` `Broker.id` | unused by pages | implied uuid |
| `types/index.ts` `Trader.brokerId` / `Group.brokerId` | unused (`any` in pages) | implied uuid or code |
| `BrokersPage` | `b.code` / `b.displayName` | **code** (matches `BrokerStatusDto`) |
| `TradersPage` / `GroupsPage` | `t.broker` / `g.broker` | **code** |
| Route `/traders/:brokerId/:login` | param **named** `brokerId` | actually **code** (`t.broker`) |
| `/api/settings` `brokerConfigs[].id` | `"ACHIEVER"` | **code** |
| `/api/traders/{broker}/{login}` | path | **code** |

Live UI works because pages use `any` and camelCase DTO names (`code`, `broker`), not `types/index.ts`. The **name** `brokerId` on the route is a Guid lie.

**Class:** types file `EXISTS_NEEDS_REFACTOR`; pages `EXISTS_AND_GOOD` against current DTOs.

### F10 — Risk / shadow `BrokerId` is decorative

`RiskEvaluationRequest.BrokerId` is `required string`. Tests set `"ACHIEVER"`. `RiskEngine.Evaluate` **never reads it**. `ShadowPosition.BrokerId` is required on the record; `ShadowCopyEngine` never constructs a `ShadowPosition` in the persist path (`PersistDemoShadowAsync` writes `ShadowOrder` with **Guid**).

A later risk ledger keyed by this string will not join persist Guid rows without another remap.

**Class:** `EXISTS_NEEDS_REFACTOR`

### F11 — No test that the two kinds stay paired

| Test | Proves | Does not prove |
|---|---|---|
| `SeedingAndStoreTests.Demo_seed_…` | two `Broker` rows exist; scores exist | `TraderScore.BrokerId == brokers.Id` for that login’s code |
| `Deal_upsert_is_idempotent` | resolve `ACHIEVER` → insert deal | resolved Guid equals `…aaa1` |
| `TradeReconstructionTests` | recon with `"ACHIEVER"` on both args | Guid.ToString() / `"achiever"` / two brokers |
| `RiskEngineTests` | `"ACHIEVER"` on request | unused field |

**Missing:** `ResolveBrokerIdAsync("ACHIEVER") == …aaa1`; same ticket two brokers; `Reconstruct(guid.ToString(), dealsWithCode)` is empty (document the footgun); `GetTraderDetailAsync("achiever", 10001)` trade count; `/api/trades?broker=STARWAVEFX&login=10001` must not return Achiever rows.

**Class:** `MISSING`

---

## 6. End-to-end map (what a row actually stores)

```text
config / worker / URL / Fake.BrokerCode     "ACHIEVER"          (code)
        │
        ▼
BrokerRegistry.Get(code)  ──────── ignore-case ──► connector
        │
        ▼
ResolveBrokerIdAsync(code) ────── exact == ─────► Guid …aaa1
        │
        ├─► mt5_groups / mt5_accounts / mt5_deals / positions
        │     BrokerId = Guid
        │
        ├─► LoadDealsAsync(Guid, code)
        │     NormalizedDeal.BrokerId = "ACHIEVER"     ← rename in place
        │
        ├─► Reconstruct("ACHIEVER", login, deals)
        │     result.BrokerId = "ACHIEVER"
        │     result.Id       = "ACHIEVER:login:pos:ms"
        │
        └─► persist reconstructed_trades / trader_scores / copy_intents
              BrokerId = Guid …aaa1
              entity.Id = NewGuid()                    ← recon string id discarded

API list/detail: join Guid → Broker.Code → JSON "broker" / "code"
/api/trades:     raw entity JSON "brokerId": "aaaaaaaa-…aaa1"
```

The **only** place Guid and code travel together is `LoadDealsAsync`’s parameter list. Every other method takes one or the other.

---

## 7. Scorecard vs architecture / A58 / A20

| Requirement | Status |
|---|---|
| §10 every source table has `broker_id` | **Yes** on the 11 persist types that exist. Missing tables (`mt5_orders`, ticks, `ingestion_events`) are out of this file |
| §10 compound uniques include broker, not ticket alone | **Yes** on deals/accounts/groups/positions/scores. Recon unique **no** (`D20`) |
| A20 `brokers.id` is the uuid | **Yes** |
| A20 `brokers.code` UNIQUE | **Yes** (unnamed index) |
| A20 FK `*.broker_id → brokers.id` | **No** |
| A20 code examples uppercase | **Matches product** |
| A58 code lowercase | **No** |
| A58 `CatalogId = DeterministicGuid("broker:{code}")` | **No** — unused helper + different seed |
| A58 connector carries Code **and** CatalogId | **Code only** |
| A57 “map at Application boundary” | **Partial** — store remaps; domain still names it `BrokerId` |
| Single comparer / normalize | **No** |
| Login unique only with broker on every API | **`/api/trades` fails** |

**8 / 13** on this checklist, and the 8 are the sequential Fake demo.

---

## 8. Forbidden / do-not-claim

1. Do **not** claim “identity is Guid everywhere” or “identity is code everywhere.” It is **both**, poorly named.
2. Do **not** treat seed `aaaaaaaa-…aaa1` as A58 CatalogId. Measured values differ (F2).
3. Do **not** implement A58 lowercase codes without a migration + one normalize function. `ResolveBrokerIdAsync` will throw on first worker tick.
4. Do **not** persist `NormalizedDeal.BrokerId` into a `uuid` column (`Guid.Parse("ACHIEVER")` throws).
5. Do **not** unique `deal_ticket` / `login` without `broker_id`.
6. Do **not** tick “§10 proven” from `/api/trades` or Trade explorer (login-only, Guid on wire, no broker column).
7. Do **not** use `GetGroupsAsync` fallback `Guid.ToString()` as a public code.
8. Do **not** add a third key (`MT5_SERVER_NAME`, host:port, manager login) as PK. A58 already forbids server name as id.

---

## 9. What a later coding task should do (not this agent)

Minimum to close D88 without breaking the demo:

1. Introduce two types (or two names): `BrokerCode` (string, normalize `Trim().ToUpperInvariant()` **or** lock A20 uppercase) and `BrokerCatalogId` (`Guid`). Stop putting the code on a property named `BrokerId`.
2. Pick **one** catalog Guid law: keep seed `…aaa1/aaa2` **or** switch to `DeterministicGuid` **and** rewrite children in the same migration. Do not do half.
3. Resolve: `ResolveBrokerIdAsync` must use the same comparer as the registry (normalize first, then exact lookup on stored canonical form).
4. `GetTraderDetailAsync`: look up by normalized code; do **not** return a header with empty trades when the list filter matched.
5. Add EF `HasForeignKey` (or at least a check constraint) `BrokerId → brokers.Id` on the 11 children.
6. `/api/trades`: resolve `broker` → Guid, filter `(BrokerId, Login)`; never login alone; DTO should expose **code**, not raw Guid, unless the client is an admin dump.
7. Tests in F11.

Until (1)+(3)+(6), treat identity as **demo-consistent, not system-consistent**.

---

## 10. File hashes (this recensus)

| Path | SHA-256 | Lines / bytes |
|---|---|---|
| `src\Domain\Entities\Broker.cs` | `412FF86681DF6189C3673762C38B22622A471C1578B5555E85827AAE02DEF19D` | 20 / 778 |
| `src\Domain\Brokers\BrokerCodes.cs` | `CF4165CE7A317B0282B9149B078E5D1E630F72524190AB20E0952BECBBAE1182` | 7 / 180 |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 338 / 12097 |
| `src\Application\Ingestion\DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 106 / 4535 |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 140 / 5082 |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 205 / 8708 |
| `src\Infrastructure\Persistence\TraderDbContext.cs` | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | (unchanged vs D20 schema hash) |
| `src\Domain\Reconstruction\NormalizedDeal.cs` | `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` | |
| `src\Domain\Reconstruction\ReconstructedTradeResult.cs` | `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` | |
| `src\Domain\Reconstruction\TradeReconstructor.cs` | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` | |
| `src\Mt5\Utils\DeterministicGuid.cs` | `A1F44B7EE85DDA7C4A73C81DDAB3D5339D778C8FB20ECCD3D46BE64BC4B72A6D` | 22 / 709 |
| `src\Mt5\Configuration\Mt5BrokerOptions.cs` | `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7` | |
| `src\Mt5\Connectors\IBrokerConnector.cs` | `6B7AA65F293AF43A548D09BC92332286A5123EDB03DCCD498C2217490CCBC5BC` | |
| `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | |
| `src\Application\Contracts\Mt5Contracts.cs` | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` | |
| `apps\api\Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | |
| `apps\web\src\types\index.ts` | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | |

D20’s store hash (`05103CE5…`) is **stale** — `PersistDemoShadowAsync` landed; identity remap at `LoadDealsAsync` is unchanged in *shape*.

---

## 11. One-page operator view

```text
D88  Broker.Code vs Guid BrokerId                         2026-08-18
================================================================
Catalog     brokers.id uuid + brokers.code UNIQUE
Persist     11 children Guid BrokerId, ZERO FK
Domain      string BrokerId == CODE ("ACHIEVER")
Translate   ResolveBrokerIdAsync + LoadDeals remap
Seed Guid   aaaaaaaa-…aaa1 / …aaa2
A58 Guid    076c2345-…f730 / 31e329d0-…d239   ≠ seed
DeterministicGuid callers                     0
Code case   product/A20 UPPER; A58 lower
Resolve ==  case-sensitive; recon/registry ignore-case
Detail API  "achiever" → header + EMPTY trades
/api/trades broker= unused; login-only; Guid JSON
Orphans     groups show Guid string; traders skip
Happy path  Fake + BrokerCodes literals        WORKS
§10 proven  NO
Product edited                                NO
================================================================
```

---

## 12. Sources

- `D:\Prop\src\Domain\Entities\Broker.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Entities\Mt5{Group,Account,Deal,Position}.cs`
- `D:\Prop\src\Domain\Entities\{ReconstructedTrade,TraderScore,TraderScoreHistory,CopyIntent,ShadowOrder,SourceSymbolMapping,SyncCheckpoint}.cs`
- `D:\Prop\src\Domain\Reconstruction\{NormalizedDeal,ReconstructedTradeResult,TradeReconstructor}.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Infrastructure\Persistence\{EfTradingStore,TraderDbContext}.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Mt5\Utils\DeterministicGuid.cs`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Mt5\Connectors\{IBrokerConnector,FakeMt5BrokerConnector}.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\web\src\{types\index.ts,App.tsx,pages\*.tsx,api\hooks.ts}`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\tests\Unit\{TradeReconstructionTests,RiskEngineTests,BaselineScorerTests,DealReasonTests}.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §10
- `D:\Prop\reports\swarm\20260818\{A20_table_catalog,A57_first_useful_version,A58_broker_registry,D20_store,D21_queries,D22_seeder,D25_dup_iface}.md`

---

*End of D88. Product source was not modified. Persist Guid + API code is the intended split; naming, case, seed CatalogId, FKs, and `/api/trades` are not consistent.*
