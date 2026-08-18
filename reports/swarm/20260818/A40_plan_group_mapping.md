# A40 — `plan_group_mappings` design (optional labels, never the fetch filter)

**Artifact:** `D:\Prop\reports\swarm\20260818\A40_plan_group_mapping.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §9  
**Supporting sections:** §1.9, §6, §7, §8, §10, §11, §45, §49, §56, §60, §67 Phase 1, §69.2, §72.3  
**Date:** 2026-08-18  
**Status:** specification only — **no product source modified**  
**Scope:** table + Domain / Application / Infrastructure / API / worker contracts so plan mappings cannot become the MT5 group-fetch filter

---

## 1. Binding law (architecture §9)

Preserve the existing env catalog:

```env
MT5_GROUP_2STEP_DEMO=demo\yo-2step
MT5_GROUP_1STEP_DEMO=demo\yo-1step

MT5_GROUP_2STEP_REAL=contest\yo-2step
MT5_GROUP_1STEP_REAL=contest\yo-1step

MT5_GROUP_INSTANT_REAL=contest\yo-instant

MT5_GROUP_CORE_DEMO=demo\yo-2step
MT5_GROUP_CORE_REAL=contest\yo-2step

MT5_GROUP_PASSFIRST_DEMO=demo\yo-payp
MT5_GROUP_PASSFIRST_REAL=contest\yo-payp
```

**These mappings must not determine which MT5 groups are fetched.**

Correct (§9, §7, §1.9, §69.2):

```text
MT5 Manager API → discover all groups
                         ↓
                   optional plan mapping
```

Incorrect (forbidden):

```text
Known plan mappings → only sync these groups
```

Adjacent §7 law that the same code path must obey:

```text
Connect
  ↓
Enumerate groups          ← IMt5BrokerConnector.GetGroupsAsync / SDK GetGroupDetails
  ↓
Upsert groups             ← mt5_groups only
  ↓
Enumerate accounts        ← every discovered group, not mapped subset
  ↓
Associate accounts with broker + group
  ↓
Sync history
```

`demo\Maxmaster` is not the only Achiever group. `demo\yo-2step` is not the only StarwaveFX group. Dynamic discovery of **all groups accessible to the Manager login** is the source of `mt5_groups`. `plan_group_mappings` is a **label overlay**.

§49: the Groups page displays **every dynamically discovered group**. “Plan mapping” is a column, not the row source.

§45: `mt5_groups` and `plan_group_mappings` are **sibling** tables. They are not the same relation.

---

## 2. Current measured state (honest)

| Surface | Path | Classification |
|---|---|---|
| Architecture catalog | §9 env block + §45 name `plan_group_mappings` | specified, not implemented |
| C# Domain entity | no `PlanGroupMapping` under `D:\Prop\src` (`grep PlanGroupMapping` = 0) | **MISSING** |
| C# `Mt5Group` | `D:\Prop\src\Domain\Entities\Mt5Group.cs` | **EXISTS** — no plan field (correct shape) |
| C# Application ports | `D:\Prop\src\Application\Class1.cs` only | **MISSING** |
| C# Infrastructure / migrations | `D:\Prop\src\Infrastructure\Class1.cs` only; no `.sql`, no `DbContext` (A03) | **MISSING** |
| C# `IMt5BrokerConnector.GetGroupsAsync` | architecture §6 sketch only; `src\Mt5\Class1.cs` empty | **MISSING** |
| API `GET /api/v1/mt5/groups` | designed in A06; not in product | **MISSING** |
| C++ `GetAllGroups` / `GetGroupDetails` | `mt5_manager.cpp` walks `GroupTotal`/`GroupNext`; **does not read** `MT5_GROUP_*` | **EXISTS_AND_GOOD** for discovery |
| C++ group probe | `mt5-sdk\tests\mt5_group_probe.cpp` — local `GetAllGroups`, ignores mapping env | **EXISTS_AND_GOOD** |
| C++ `AppConfig` mapping fields | `mt5-sdk\config\app_config.{h,cpp}` + `.env.example` | **EXISTS** as optional config |
| C++ `MT5AccountHelper::getMt5Group` | `mt5-sdk\src\services\mt5_account_helper.cpp` | **provisioning lookup**, not fetch. **Do not port into the C# discovery worker.** Compile-time defaults (`Flexy\yo-*`) **diverge** from §9 (`contest\yo-*`). Seed the C# table from **§9**, not from those defaults. |

C++ discovery is already the correct pattern. The failure mode this design prevents is a **future C# worker** that treats `MT5_GROUP_*` / `plan_group_mappings` as a whitelist.

---

## 3. What a mapping is, and what it is not

A row in `plan_group_mappings` is an **optional human/operator label**:

> “On broker B, product plan P in environment E is *called* MT5 group path G.”

It is **not**:

- a permission to fetch that group
- a deny-list for unmapped groups
- a default for `enabled_for_analysis`
- a creator of `mt5_groups` rows
- a deleter of `mt5_groups` rows
- an account-enumeration mask
- a history-sync filter
- a scoring / copy-candidate filter

`enabled_for_analysis` lives on **`mt5_groups`** and is an audited operator PATCH (A06). Mapping presence must not imply analysis enablement. Analysis enablement must not require a mapping.

`MT5_DEFAULT_GROUP` (`demo\Maxmaster` on Achiever, `demo\default` in the SDK example) is also **not** a fetch filter. It is a provisioning landing group, if a writer ever creates accounts. Discovery still enumerates everything the Manager login can see.

---

## 4. Cardinality and seed catalog

### 4.1 Cardinality

| Relation | Rule | Evidence |
|---|---|---|
| Plan+env → group path | **many-to-one** | §9: `CORE_DEMO` and `2STEP_DEMO` both = `demo\yo-2step`; `CORE_REAL` and `2STEP_REAL` both = `contest\yo-2step` |
| Group path → plans | **one-to-many** labels | same |
| Plan+env → paths | **one** path per `(broker_id, plan_key, environment)` | each env key is a single string |
| INSTANT | **real only** | §9 has `MT5_GROUP_INSTANT_REAL` and no `*_INSTANT_DEMO` |
| Brokers | mappings are **per `broker_id`** | §10: every source table carries `broker_id`. Achiever default group is `demo\Maxmaster`; the `yo-*` catalog is a different tree. **Do not seed Achiever with StarwaveFX/YoPips paths.** |

Unique key is `(broker_id, plan_key, environment)`, **not** `(broker_id, group_path)`.

### 4.2 Closed seed keys (env → row)

| Env key | `plan_key` | `environment` | §9 `group_path` |
|---|---|---|---|
| `MT5_GROUP_2STEP_DEMO` | `2STEP` | `demo` | `demo\yo-2step` |
| `MT5_GROUP_1STEP_DEMO` | `1STEP` | `demo` | `demo\yo-1step` |
| `MT5_GROUP_2STEP_REAL` | `2STEP` | `real` | `contest\yo-2step` |
| `MT5_GROUP_1STEP_REAL` | `1STEP` | `real` | `contest\yo-1step` |
| `MT5_GROUP_INSTANT_REAL` | `INSTANT` | `real` | `contest\yo-instant` |
| `MT5_GROUP_CORE_DEMO` | `CORE` | `demo` | `demo\yo-2step` |
| `MT5_GROUP_CORE_REAL` | `CORE` | `real` | `contest\yo-2step` |
| `MT5_GROUP_PASSFIRST_DEMO` | `PASSFIRST` | `demo` | `demo\yo-payp` |
| `MT5_GROUP_PASSFIRST_REAL` | `PASSFIRST` | `real` | `contest\yo-payp` |

`plan_key` is stored as text so a later product tier is an **INSERT**, not a migration. Seed code knows the nine keys above. No `CHECK` that freezes the set.

C++ `getMt5Group` aliases (`yo_pips_2_step` / `yp_summit` → 2STEP, `yo_pips_1_step` / `yp_edge` → 1STEP, `yo_pips_instant` / `yp_instant` → INSTANT, `yp_core` / `yo_pips_core` → CORE, `yp_passfirst` → PASSFIRST) are a **provisioning** vocabulary. If a future writer needs them, store them as `plan_aliases` (optional jsonb or a small side table). They must not appear on `IMt5BrokerConnector`.

### 4.3 Per-broker seed (do not apply §9 globally)

The §9 block is **unprefixed**. §7 / §8 are two brokers. Binding seed rule:

1. Table is always keyed by `broker_id`.
2. Global `MT5_GROUP_*` env (or `PlanGroupMappings` config section) seeds **only** the broker flagged `SeedPlanGroupMappings=true` (expected: StarwaveFX / the YoPips-style tree).
3. Achiever starts with **zero** mapping rows unless an operator adds them.
4. Prefixed override is allowed later (`MT5_STARWAVEFX_GROUP_2STEP_DEMO=...`) and wins for that broker.
5. Blank env value → **skip that seed row**. Do not insert an empty `group_path`. Do not invent `MT5_DEFAULT_GROUP` as a stand-in.
6. After first successful seed, the **table is the source of truth**. Env is import-once (or explicit re-import). Discovery never re-reads env.

---

## 5. Table design

PostgreSQL. Versioned EF Core migration only (§72.3). No hand-edited production schema.

### 5.1 `mt5_groups` (companion — discovery truth)

Needed so mappings can join without becoming the row source. Aligns with `Domain.Entities.Mt5Group`:

```sql
CREATE TABLE mt5_groups (
    id                      uuid PRIMARY KEY,
    broker_id               uuid NOT NULL REFERENCES brokers (id),
    name                    text NOT NULL,              -- exact Manager group path, e.g. demo\yo-2step
    currency                text NOT NULL DEFAULT '',
    currency_digits         integer NOT NULL DEFAULT 2,
    company                 text NOT NULL DEFAULT '',
    margin_call             numeric(18, 6) NOT NULL DEFAULT 0,
    margin_stop_out         numeric(18, 6) NOT NULL DEFAULT 0,
    connections_allowed     boolean NOT NULL DEFAULT false,
    is_enabled_for_analysis boolean NOT NULL DEFAULT false,  -- operator flag; NOT derived from mappings
    is_present              boolean NOT NULL DEFAULT true,   -- false if last discover pass did not see it
    last_discovered_at      timestamptz NULL,
    last_synced_at          timestamptz NULL,
    created_at              timestamptz NOT NULL,
    updated_at              timestamptz NOT NULL,
    CONSTRAINT uq_mt5_groups_broker_name UNIQUE (broker_id, name)
);

CREATE INDEX ix_mt5_groups_broker_present
    ON mt5_groups (broker_id, is_present);
```

Discovery upserts **every** name returned by the Manager API. Soft-absent (`is_present = false`) when a previously seen name disappears. **Never DELETE** a group because a mapping was removed or because the name is unmapped.

### 5.2 `plan_group_mappings` (labels only)

```sql
CREATE TABLE plan_group_mappings (
    id              uuid PRIMARY KEY,
    broker_id       uuid NOT NULL REFERENCES brokers (id),
    plan_key        text NOT NULL,          -- 2STEP | 1STEP | INSTANT | CORE | PASSFIRST | future
    environment     text NOT NULL,          -- demo | real
    group_path      text NOT NULL,          -- exact MT5 path; join key to mt5_groups.name
    display_name    text NULL,              -- optional UI string, e.g. "2-Step Demo"
    source          text NOT NULL,          -- seed_env | operator | import
    is_active       boolean NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL,
    updated_at      timestamptz NOT NULL,
    CONSTRAINT ck_plan_group_mappings_environment
        CHECK (environment IN ('demo', 'real')),
    CONSTRAINT ck_plan_group_mappings_source
        CHECK (source IN ('seed_env', 'operator', 'import')),
    CONSTRAINT ck_plan_group_mappings_plan_key
        CHECK (plan_key <> ''),
    CONSTRAINT ck_plan_group_mappings_group_path
        CHECK (group_path <> ''),
    CONSTRAINT uq_plan_group_mappings_broker_plan_env
        UNIQUE (broker_id, plan_key, environment)
);

CREATE INDEX ix_plan_group_mappings_broker_path
    ON plan_group_mappings (broker_id, group_path)
    WHERE is_active;

CREATE INDEX ix_plan_group_mappings_broker_active
    ON plan_group_mappings (broker_id)
    WHERE is_active;
```

**No foreign key to `mt5_groups`.** Reasons:

1. Seed may run before the first discover pass. A label that points at a not-yet-seen path is a dangling **label**, not a group to fetch.
2. Discovery must be able to insert/update `mt5_groups` without touching this table.
3. Deleting or soft-absenting a group must not cascade-delete labels.
4. A typo in `group_path` is visible on the Groups page as “mapped but not discovered” — it must **not** cause a `GroupRequest(name)` of only that path.

Join is always:

```sql
mt5_groups g
LEFT JOIN plan_group_mappings m
       ON m.broker_id = g.broker_id
      AND m.group_path = g.name
      AND m.is_active
```

Driving table for any list/sync is **`mt5_groups`** (or the live Manager result). Never `FROM plan_group_mappings` as the group set.

### 5.3 What is deliberately absent

| Anti-column / anti-constraint | Why absent |
|---|---|
| `UNIQUE (broker_id, group_path)` | CORE and 2STEP share a path |
| `mt5_group_id uuid NOT NULL REFERENCES mt5_groups` | would force creating groups from seed |
| `should_sync` / `fetch_priority` / `include_in_discovery` | would become a fetch filter |
| `is_enabled_for_analysis` on this table | that flag belongs on `mt5_groups` |
| Trigger that INSERTs into `mt5_groups` on mapping write | mapping would create groups |
| Trigger that DELETEs `mt5_groups` on mapping delete | mapping would own groups |

---

## 6. Domain types

Place next to the existing entity (`D:\Prop\src\Domain\Entities\PlanGroupMapping.cs`) unless the A01 Wave 1 reorg to `Domain\Mt5\` lands first. Do **not** add a plan field to `Mt5Group`.

```csharp
namespace TraderIntelligence.Domain.Enums;

public enum PlanEnvironment
{
    Demo = 1,
    Real = 2
}

public enum PlanMappingSource
{
    SeedEnv = 1,
    Operator = 2,
    Import = 3
}

public static class PlanKeys
{
    public const string TwoStep = "2STEP";
    public const string OneStep = "1STEP";
    public const string Instant = "INSTANT";
    public const string Core = "CORE";
    public const string PassFirst = "PASSFIRST";
}
```

```csharp
namespace TraderIntelligence.Domain.Entities;

/// <summary>
/// Optional product-plan label for a discovered MT5 group path.
/// Not a fetch/sync filter. Absence is valid.
/// </summary>
public sealed record PlanGroupMapping(
    Guid Id,
    Guid BrokerId,
    string PlanKey,
    PlanEnvironment Environment,
    string GroupPath,
    string? DisplayName,
    PlanMappingSource Source,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

Invariants (constructor / factory):

- `BrokerId != default`
- `PlanKey` trimmed, non-empty, stored uppercase
- `GroupPath` trimmed, non-empty, **not** slash-normalized (`demo/yo-2step` ≠ `demo\yo-2step`). Persist the Manager/env backslash form.
- No call into any group-discovery port.

`Mt5Group` stays as it is today (no `PlanKey`). UI/API compose labels at read time.

---

## 7. Application ports — isolation is the design

### 7.1 Discovery has no mapping dependency

```csharp
public interface IMt5BrokerConnector
{
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);

    /// <summary>
    /// Every group the Manager login can see. No plan filter. No name allow-list.
    /// </summary>
    Task<IReadOnlyCollection<DiscoveredMt5Group>> GetGroupsAsync(CancellationToken ct);

    Task<IReadOnlyCollection<Mt5Account>> GetAccountsAsync(/* not a mapping list */, CancellationToken ct);
    // … deals / orders / positions / subscribe …
}

public sealed record DiscoveredMt5Group(
    string Name,
    string Currency,
    int CurrencyDigits,
    string Company,
    decimal MarginCall,
    decimal MarginStopOut,
    bool ConnectionsAllowed);

public interface IMt5GroupStore
{
    Task UpsertDiscoveredAsync(
        Guid brokerId,
        IReadOnlyList<DiscoveredMt5Group> groups,
        DateTimeOffset now,
        CancellationToken ct);

    Task<IReadOnlyList<Mt5Group>> ListByBrokerAsync(Guid brokerId, CancellationToken ct);
}

public interface IMt5GroupDiscoveryService
{
    Task<GroupDiscoveryResult> DiscoverAndUpsertAsync(Guid brokerId, CancellationToken ct);
}

public sealed record GroupDiscoveryResult(
    Guid BrokerId,
    int ManagerCount,
    int UpsertedCount,
    int SoftAbsentCount);
```

**Compile-time rule:** `Mt5GroupDiscoveryService` constructor takes `IMt5BrokerConnector` + `IMt5GroupStore` (+ time/clock). It must **not** take `IPlanGroupMappingStore`, `AppConfig` mapping fields, or `IOptions<PlanGroupMappingOptions>`.

Algorithm (only legal sequence):

```text
groups = await connector.GetGroupsAsync(ct)
         // MUST be the full Manager walk (GetGroupDetails / GetAllGroups)
upsert  = await groupStore.UpsertDiscoveredAsync(brokerId, groups, now, ct)
         // SQL touches mt5_groups only
return counts
```

`GetGroupsAsync` implementations (local Manager or HTTP `/mt5/groups`) take **zero** name filters. No `IReadOnlyCollection<string> onlyThese`. No `where mappings.Contains`.

Account enumeration after upsert walks **`mt5_groups` where broker_id = X AND is_present`**, or a single Manager `UserLogins` mask that means “all” if the SDK is proven to support it. It must **not** walk `plan_group_mappings.group_path`.

### 7.2 Mapping store is read/write for labels only

```csharp
public interface IPlanGroupMappingStore
{
    Task<IReadOnlyList<PlanGroupMapping>> ListByBrokerAsync(Guid brokerId, CancellationToken ct);
    Task<IReadOnlyList<PlanGroupMapping>> ListByGroupPathAsync(Guid brokerId, string groupPath, CancellationToken ct);
    Task UpsertAsync(PlanGroupMapping mapping, CancellationToken ct);
    Task DeactivateAsync(Guid id, DateTimeOffset now, CancellationToken ct);
}

public interface IPlanGroupMappingSeed
{
    /// <summary>
    /// Insert-if-absent from configuration. Never inserts mt5_groups.
    /// Never calls GetGroupsAsync.
    /// </summary>
    Task<int> SeedIfEmptyAsync(Guid brokerId, IReadOnlyList<PlanGroupSeed> seeds, CancellationToken ct);
}

public sealed record PlanGroupSeed(
    string PlanKey,
    PlanEnvironment Environment,
    string GroupPath,
    string? DisplayName);
```

`SeedIfEmptyAsync` writes `plan_group_mappings` only. Empty `GroupPath` skipped. Duplicate `(broker_id, plan_key, environment)` left untouched (seed does not clobber operator edits).

### 7.3 Read model for §49 (LEFT JOIN, groups drive)

```csharp
public sealed record Mt5GroupPageRow(
    Guid GroupId,
    Guid BrokerId,
    string BrokerDisplayName,
    string GroupName,
    int AccountCount,
    bool EnabledForAnalysis,
    IReadOnlyList<PlanLabel> PlanMappings,  // empty = unmapped; still a row
    DateTimeOffset? LastDiscoveredAt,
    DateTimeOffset? LastSyncedAt);

public sealed record PlanLabel(string PlanKey, PlanEnvironment Environment, string? DisplayName);

public interface IMt5GroupQuery
{
    Task<IReadOnlyList<Mt5GroupPageRow>> ListDiscoveredAsync(
        Guid? brokerId,
        CancellationToken ct);
}
```

SQL shape (binding):

```sql
SELECT g.id,
       g.broker_id,
       b.name                          AS broker_display_name,
       g.name                          AS group_name,
       (SELECT count(*) FROM mt5_accounts a
         WHERE a.broker_id = g.broker_id AND a.group_id = g.id) AS account_count,
       g.is_enabled_for_analysis,
       g.last_discovered_at,
       g.last_synced_at,
       coalesce(
           (SELECT json_agg(json_build_object(
                       'plan_key', m.plan_key,
                       'environment', m.environment,
                       'display_name', m.display_name)
                   ORDER BY m.plan_key, m.environment)
              FROM plan_group_mappings m
             WHERE m.broker_id = g.broker_id
               AND m.group_path = g.name
               AND m.is_active),
           '[]'::json)                 AS plan_mappings
  FROM mt5_groups g
  JOIN brokers b ON b.id = g.broker_id
 WHERE (@broker_id IS NULL OR g.broker_id = @broker_id)
 ORDER BY b.name, g.name;
```

Unmapped groups return `plan_mappings = []`. They **remain rows**.

Optional later filter `?planKey=2STEP` is a **display** predicate on the LEFT JOIN. It must not exist on the discovery worker.

---

## 8. Worker / connector wiring

`apps/mt5-worker` startup/resync (A07 §3.1) per broker:

```text
registry.Get(brokerId).ConnectAsync
  ↓
IMt5GroupDiscoveryService.DiscoverAndUpsertAsync   // no mapping store
  ↓
enumerate accounts for every present mt5_groups row
  ↓
associate accounts (broker_id + login + group_id)
  ↓
history backfill
```

`IPlanGroupMappingSeed.SeedIfEmptyAsync` may run **once** at host start, **in parallel or before** discovery, never as an input to discovery. Failure to seed must **not** skip discovery. Empty mapping table is a valid production state (Achiever).

C++ boundary (do not change in this task; contract for later):

- Keep `MT5Manager::GetAllGroups` / `GetGroupDetails` as full `GroupNext` walks.
- Keep `mt5_group_probe` mapping-blind.
- Do **not** add `AppConfig.mt5_group_*` into `GetAllGroups`.
- `MT5AccountHelper::getMt5Group` stays a **create-account** helper. Trader Intelligence Phase 1 does not create MT5 accounts. Do not call it from the C# worker.

HTTP remote client (`MT5HttpClient::GetAllGroups` → `GET /mt5/groups`) already returns the remote full list. Remote `GetGroupDetails` currently returns false (A16). Connector should prefer details when local; name-only remote still upserts **all names**, with empty currency/company until details exist. Still no mapping filter.

---

## 9. API (labels are CRUD; groups are discovered)

Align with A06 §4.5; add mapping admin without turning it into discovery.

| Method | Path | Role | Behavior |
|---|---|---|---|
| `GET` | `/api/v1/mt5/groups` | ReadOnly+ | `IMt5GroupQuery.ListDiscoveredAsync`. One row per `mt5_groups`. `planMappings` may be empty. |
| `PATCH` | `/api/v1/mt5/groups/{groupId}` | Analyst+ | `enabledForAnalysis` only. Audited. **Does not** write mappings. |
| `GET` | `/api/v1/mt5/plan-group-mappings?brokerId=` | ReadOnly+ | List labels. May include paths not yet in `mt5_groups` (dangling seed). |
| `PUT` | `/api/v1/mt5/plan-group-mappings` | Analyst+ | Upsert label. **Does not** call `GetGroupsAsync`. **Does not** insert `mt5_groups`. Audited. |
| `DELETE` | `/api/v1/mt5/plan-group-mappings/{id}` | Analyst+ | Soft-deactivate (`is_active=false`). **Does not** delete or hide the group. Audited. |

Forbidden API behaviors:

- `GET /mt5/groups?mappedOnly=true` as the default Groups page query.
- `POST /mt5/groups/sync` body `{ "paths": [ ...from mappings ] }`.
- Creating a group resource from a mapping PUT.

Dashboard §49 column “Plan mapping”: join `PlanLabel` list as `"2STEP/demo, CORE/demo"` or “—”. Never hide the row when the cell is “—”.

---

## 10. Configuration shape (preserve §9, isolate from fetch)

C# options (Infrastructure binding only; **not** injected into discovery):

```csharp
public sealed class PlanGroupMappingOptions
{
    public const string SectionName = "PlanGroupMappings";

    /// <summary>Broker id (or key) that receives the unprefixed MT5_GROUP_* seed. Empty = seed nobody.</summary>
    public string? SeedBrokerKey { get; set; }

    public string? TwoStepDemo { get; set; }
    public string? OneStepDemo { get; set; }
    public string? TwoStepReal { get; set; }
    public string? OneStepReal { get; set; }
    public string? InstantReal { get; set; }
    public string? CoreDemo { get; set; }
    public string? CoreReal { get; set; }
    public string? PassFirstDemo { get; set; }
    public string? PassFirstReal { get; set; }
}
```

Env names stay the §9 keys so existing `.env` files keep working. `SeedBrokerKey` must be explicit in appsettings (e.g. `starwavefx`). If unset, **zero rows** are seeded — safer than painting Achiever with `yo-*` paths.

Discovery options are a **different** class (`Mt5BrokerOptions`: server, login, pool, proxy). No mapping fields on that class.

---

## 11. Persistence notes (EF)

When implementation is authorized:

- `TraderIntelligenceDbContext` exposes `DbSet<Mt5Group>` and `DbSet<PlanGroupMapping>`.
- Separate `IEntityTypeConfiguration<>` classes.
- `PlanGroupMappingConfiguration` maps to `plan_group_mappings`, unique index on `(BrokerId, PlanKey, Environment)`, filter index on `(BrokerId, GroupPath) WHERE IsActive`.
- **No** navigation from `Mt5Group` that is required. Optional collection `IReadOnlyList<PlanGroupMapping>` on a **query DTO** only, not on the write entity used by upsert.
- Group upsert SQL / EF must not `Include` mappings and must not `SaveChanges` mappings.
- Migration name example: `AddPlanGroupMappings` (additive). Does not rewrite `mt5_groups` uniqueness.

---

## 12. Forbidden patterns (review / grep needles)

Any of the following in product code is a **spec fail**:

| Needle / smell | Why it is wrong |
|---|---|
| `GetGroupsAsync(.*mapping` / `onlyGroups` / `allowedGroups` | connector became a filter |
| `plan_group_mappings` inside `Mt5GroupDiscoveryService` or `IMt5GroupStore.Upsert*` | discovery coupled to labels |
| `WHERE g.name IN (SELECT group_path FROM plan_group_mappings` | mappings as fetch/sync set |
| `JOIN plan_group_mappings` as the **driving** table of a sync job | same |
| `if (!mappings.Contains(group.Name)) continue;` in a discover/account/history loop | same |
| `AppConfig.mt5_group_*` / `PlanGroupMappingOptions` injected into connector or discovery | env catalog as allow-list |
| `is_enabled_for_analysis = mappingExists` | analysis flag hijacked |
| `INSERT INTO mt5_groups … SELECT … FROM plan_group_mappings` | labels create groups |
| `ON DELETE CASCADE` from mappings to groups (or trigger delete) | labels own groups |
| Seeding Achiever with `demo\yo-*` because env is global | wrong broker tree |
| Using C++ `Flexy\yo-*` defaults as C# seed | contradicts §9 `contest\yo-*` |
| `MT5_DEFAULT_GROUP` as the sole upserted group | contradicts §7 |

Allowed references to `plan_group_mappings`:

- seed service
- mapping store
- `IMt5GroupQuery` LEFT JOIN / subquery
- mapping admin API
- future account-**create** helper (out of Phase 1 scope)

---

## 13. Tests (must exist before the worker is “done”)

Named to extend A27. Prove the negative: mappings do not shrink the discovered set.

### 13.1 Unit — `tests/Unit/Mt5/PlanGroupMappingIsNotFetchFilterTests.cs`

| Test | Must prove |
|---|---|
| `Discover_upserts_every_connector_group_when_mapping_table_empty` | stub connector returns 12 names; store receives 12; mapping store **not invoked** (strict mock) |
| `Discover_upserts_unmapped_groups_when_nine_seed_rows_exist` | 9 mappings, connector returns 50; store receives 50 |
| `Discovery_service_constructor_does_not_accept_mapping_store` | architectural: service type has no such parameter (reflection assertion is acceptable) |
| `Seed_does_not_call_connector_or_group_store` | seed writes mappings only |
| `Seed_skips_blank_paths` | empty env → no row |
| `Seed_does_not_overwrite_operator_row` | existing `source=operator` kept |
| `Core_and_two_step_share_path` | two active rows, one `group_path` |
| `Instant_has_no_required_demo_row` | seed of 9 keys creates 9 rows, not 10 |
| `Deactivate_mapping_does_not_touch_groups` | group store mock unused |
| `Default_group_is_not_a_fetch_filter` | Achiever `demo\Maxmaster` in options; connector still returns N; all N upserted |

### 13.2 Integration — `tests/Integration/Persistence/PlanGroupMappingSchemaTests.cs`

| Test | Must prove |
|---|---|
| Migration creates `plan_group_mappings` with unique `(broker_id, plan_key, environment)` | schema contract |
| Same `group_path` two `plan_key`s on one broker **succeeds** | CORE+2STEP |
| Same `plan_key`+`environment` two brokers **succeeds** | §10 isolation |
| Same `plan_key`+`environment` same broker **fails** | unique |
| Empty `group_path` **fails** check | no blank labels |
| `IMt5GroupQuery` on 50 groups + 9 mappings returns **50** rows; 7 distinct paths labeled (2STEP/CORE share 2 paths; 1STEP 2; INSTANT 1; PASSFIRST 2 → 7 labeled paths); 43 with `[]` | §49 “every discovered group” |
| DELETE/deactivate mapping leaves `mt5_groups` row count unchanged | ownership |
| Discover pass with 0 mapping rows still commits 50 `mt5_groups` | empty overlay |

### 13.3 Integration — dual broker

Achiever stub returns `{demo\Maxmaster, real\pro, …}` with **zero** mappings. StarwaveFX stub returns a larger set plus the §9 paths. After one worker cycle: Achiever group count = stub count; StarwaveFX group count = stub count; mapping rows exist only for the seed broker.

---

## 14. Intended files (do not create in this task)

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Entities\PlanGroupMapping.cs` | entity |
| `D:\Prop\src\Domain\Enums\PlanEnvironment.cs` | enum |
| `D:\Prop\src\Domain\Enums\PlanMappingSource.cs` | enum |
| `D:\Prop\src\Domain\Enums\PlanKeys.cs` | seed constants |
| `D:\Prop\src\Application\Contracts\Mt5\IMt5GroupDiscoveryService.cs` | discover port |
| `D:\Prop\src\Application\Contracts\Mt5\IMt5GroupStore.cs` | group write/read |
| `D:\Prop\src\Application\Contracts\Mt5\IPlanGroupMappingStore.cs` | label store |
| `D:\Prop\src\Application\Contracts\Mt5\IPlanGroupMappingSeed.cs` | env import |
| `D:\Prop\src\Application\Contracts\Mt5\IMt5GroupQuery.cs` | §49 read model |
| `D:\Prop\src\Application\Mt5\Mt5GroupDiscoveryService.cs` | **no mapping inject** |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\PlanGroupMappingConfiguration.cs` | EF |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\Mt5GroupConfiguration.cs` | EF |
| `D:\Prop\src\Infrastructure\Persistence\Migrations\*AddPlanGroupMappings*.cs` | versioned SQL |
| `D:\Prop\src\Infrastructure\Options\PlanGroupMappingOptions.cs` | seed bind only |
| `D:\Prop\apps\api\` groups + mapping endpoints | when API work is authorized |
| `D:\Prop\tests\Unit\Mt5\PlanGroupMappingIsNotFetchFilterTests.cs` | negative tests |
| `D:\Prop\tests\Integration\Persistence\PlanGroupMappingSchemaTests.cs` | schema + LEFT JOIN |

`Mt5Group.cs` already exists and must **not** grow a required plan field.

---

## 15. Acceptance (this spec is satisfied when)

1. `plan_group_mappings` exists as specified; `mt5_groups` is populated **only** from Manager discovery.
2. Connector `GetGroupsAsync` returns the full Manager set; its signature has no mapping/allow-list argument.
3. Discovery service has no compile-time dependency on the mapping store or mapping options.
4. Groups page / `GET /api/v1/mt5/groups` returns every discovered group; plan column may be empty.
5. Seeding the nine §9 rows does not change the discovered group count.
6. Removing a mapping does not remove a group.
7. Adding a mapping does not fetch that path from the Manager by itself.
8. Achiever can run with zero mapping rows and still persist every visible group (including more than `demo\Maxmaster`).
9. CORE and 2STEP may label the same `group_path`.
10. Grep of the discovery/upsert path for `plan_group_mappings` / `PlanGroupMapping` / `MT5_GROUP_` is empty.

Until those tests exist and pass, treat `plan_group_mappings` as **UNSPECIFIED IN CODE** even though this document is complete.

---

## 16. Risks

| Risk | Mitigation |
|---|---|
| Implementer “optimizes” sync to mapped groups only (9 paths vs hundreds) | Tests in §13; Phase 1 gate “all groups discovered” (§67 / §69.2) is count vs Manager `GroupTotal`, not vs mapping count |
| Global env seed applied to Achiever | `SeedBrokerKey` required; default seed nobody |
| C++ `Flexy\` defaults copied into C# | Seed table uses §9 `contest\` / `demo\yo-*` only |
| `GetGroupLogins` called only for mapped paths | Account walk uses `mt5_groups` present set |
| HTTP remote `GetGroupDetails` is false | Name-only full list still upserts; do not fall back to mapping paths |
| Operator wants “only analyze mapped groups” | Use `enabled_for_analysis`, not mapping presence |
| Future provisioning writer reuses mappings | Allowed as **write-target lookup**; still never a fetch filter |

---

## 17. Decision record

| Decision | Choice |
|---|---|
| Mappings vs groups | Separate tables; groups are discovered; mappings are labels |
| Join | `(broker_id, group_path)` LEFT JOIN; no required FK |
| Uniqueness | `(broker_id, plan_key, environment)` |
| Shared paths | Allowed (CORE = 2STEP in §9) |
| Env catalog | Preserved as optional one-shot seed for one configured broker |
| Fetch filter | **Never.** Manager API is the only group set |
| Analysis flag | On `mt5_groups`, independent |
| Product source in this task | **Unchanged** |
