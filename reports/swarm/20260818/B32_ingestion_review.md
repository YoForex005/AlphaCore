# B32 — `DealIngestionService` group discovery is not filtered by plan mapping

| Field | Value |
|---|---|
| Agent | B32 (senior engineer, ingestion path only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\B32_ingestion_review.md` |
| Assigned question | Read `DealIngestionService.cs`. Check that **group discovery is not filtered by plan mapping**. |
| Primary SUT | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| Binding law | Architecture v2 §9: Manager API discovers **all** groups; plan mapping is an optional label **after** discovery. Incorrect: `Known plan mappings → only sync these groups`. |
| Siblings | A39 (Manager `GetAllGroups` walk), A40 (`plan_group_mappings` is never the fetch filter), A04 / A07 / A79 |
| Product source modified | **None.** This report is the only write. |

---

## 0. Verdict (honest, measured)

**Confirmed: C# group discovery on the live ingestion path is not filtered by plan mapping.**

`DealIngestionService.SyncBrokerAsync` calls `IMt5BrokerConnector.GetGroupsAsync(ct)` with **zero** name / plan / allow-list arguments, then upserts **every** returned `Mt5GroupDto`. It does not inject a mapping store, does not read `MT5_GROUP_*`, does not skip unmapped names, and then enumerates accounts with `GetAccountsAsync(null, ct)` (all accounts, not a mapped-path list).

Classification of the assigned check: **PASS** on the C# collector as written.

This is **not** a claim that:

- a production Manager connector exists (it does **not** — only `FakeMt5BrokerConnector`);
- `plan_group_mappings` is implemented as the A40 sibling table (it is **not**);
- negative unit tests `PlanGroupMappingIsNotFetchFilterTests` exist (they do **not**);
- Phase 1 §12 backfill / checkpoints / live events are done (they are **not**).

The measured fact is narrower and true: **the ingestion service does not use plan mapping as a fetch filter.**

---

## 1. Assigned check — how it was measured

1. Read `DealIngestionService.cs` in full (including `ITradingStore` and `ReconstructionScoringService` in the same file).
2. Trace `GetGroupsAsync` / `GetAccountsAsync` / `UpsertGroupAsync` through Application contracts, Fake connector, EF store, dashboard, seeder, worker, DI.
3. Grep product C# for `PlanMapping`, `PlanGroupMapping`, `plan_group_mappings`, `MT5_GROUP_`, `GetGroupsAsync`, `EnabledForAnalysis`.
4. Compare against architecture §9 and A40 forbidden needles (A40 §12).
5. **Did not** edit any file under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

---

## 2. `SyncBrokerAsync` — discovery is the full connector list

File: `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`

```31:58:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        foreach (var group in groups)
            await _store.UpsertGroupAsync(brokerId, group, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        foreach (var account in accounts)
        {
            await _store.UpsertAccountAsync(brokerId, account, now, ct);
            var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
            foreach (var deal in deals)
            {
                if (await _store.UpsertDealAsync(brokerId, deal, now, ct))
                    insertedDeals++;
            }

            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }

        return insertedDeals;
    }
```

Measured sequence (matches architecture §7 / A40 §1 **shape**, not completeness of Phase 1):

```text
Connect
  ↓
GetGroupsAsync(ct)                 ← no plan / name / allow-list argument
  ↓
foreach group → UpsertGroupAsync   ← no continue / Contains / mapping check
  ↓
GetAccountsAsync(null, ct)         ← null = all accounts, not mapped subset
  ↓
per account: upsert + deals + positions
```

There is **no** `if`, `Where`, `Contains`, `continue`, or `break` between `GetGroupsAsync` and `UpsertGroupAsync`. Every DTO the connector returns is persisted.

Account enumeration is **not** `foreach mapping.GroupPath`. The only optional filter on `GetAccountsAsync` is a **group path string**. Ingestion passes `null`.

---

## 3. Constructor isolation — no mapping dependency

```20:29:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
public sealed class DealIngestionService
{
    private readonly IBrokerRegistry _registry;
    private readonly ITradingStore _store;

    public DealIngestionService(IBrokerRegistry registry, ITradingStore store)
    {
        _registry = registry;
        _store = store;
    }
```

A40 §7.1 compile-time rule: discovery must **not** take `IPlanGroupMappingStore`, `AppConfig` mapping fields, or `IOptions<PlanGroupMappingOptions>`.

Measured:

| Dependency | Present? |
|---|---|
| `IBrokerRegistry` | yes |
| `ITradingStore` | yes |
| `IPlanGroupMappingStore` | **no** (type does not exist in C#) |
| `PlanGroupMappingOptions` | **no** |
| `MT5_GROUP_*` / `IConfiguration` | **no** |
| `MT5AccountHelper` / C++ `getMt5Group` | **no** |

`ITradingStore` itself has no mapping method. Its group write is `UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, …)` — one discovered DTO at a time.

`ReconstructionScoringService` (same file, lines 62–103) rebuilds trades/scores from already-stored deals. It never lists groups and never reads plan mapping.

---

## 4. Port shapes — `GetGroupsAsync` cannot be asked for “mapped only”

Application port (`D:\Prop\src\Application\Contracts\Mt5Contracts.cs`):

```5:12:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
public sealed record Mt5GroupDto(
    string Name,
    string? Currency,
    int CurrencyDigits,
    string? Company,
    decimal? MarginCall,
    decimal? MarginStopOut,
    bool ConnectionsAllowed);
```

```53:62:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
public interface IMt5BrokerConnector
{
    string BrokerCode { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<bool> IsConnectedAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct);
    Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct);
}
```

Facts:

- `Mt5GroupDto` has **no** `PlanMapping` / `PlanKey` / `EnabledForAnalysis` field. The connector cannot return a “mapped?” flag because the type has nowhere to put it.
- `GetGroupsAsync` takes **only** `CancellationToken`. No `onlyThese`, `allowedGroups`, `mappedOnly`.
- `GetAccountsAsync` may filter by **exact group path** when the string is non-empty. That is a Manager-style mask, **not** a plan overlay. Ingestion never supplies a path.

Unused / stale sibling `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` declares `GetGroupsAsync(CancellationToken)` and `GetAccountsAsync(Mt5Group group, …)`. **`DealIngestionService` does not use this interface.** `IBrokerRegistry.Get` returns `IMt5BrokerConnector`. The unused `IBrokerConnector` is a leftover (A58 / A79: delete later). It is still mapping-blind (`GetGroupsAsync` has no plan argument).

---

## 5. Store upsert — every name is written; mapping is not consulted

File: `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`

```22:51:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Mt5Groups.SingleOrDefaultAsync(
            g => g.BrokerId == brokerId && g.Name == group.Name, ct);
        if (existing is null)
        {
            _db.Mt5Groups.Add(new Mt5Group
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                Name = group.Name,
                Currency = group.Currency,
                CurrencyDigits = group.CurrencyDigits,
                Company = group.Company,
                MarginCall = group.MarginCall,
                MarginStopOut = group.MarginStopOut,
                ConnectionsAllowed = group.ConnectionsAllowed,
                EnabledForAnalysis = true,
                LastDiscoveredAt = now,
                LastSyncedAt = now
            });
        }
        else
        {
            existing.Currency = group.Currency;
            existing.LastSyncedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
```

| Check | Result |
|---|---|
| Skip when `PlanMapping` is null? | **No.** Field is never read. |
| Skip when name ∉ `MT5_GROUP_*`? | **No.** Env is not referenced. |
| Insert only mapped names? | **No.** Insert key is `(BrokerId, Name)` from the DTO. |
| `EnabledForAnalysis` derived from mapping presence? | **No.** Hard-coded `true` on insert. (A40 wanted default `false` as an **operator** flag — a separate design gap, **not** a fetch filter.) |
| Write `PlanMapping`? | **No.** Column stays null unless something else sets it. Nothing in ingestion does. |
| `JOIN plan_group_mappings`? | **No.** Table / `DbSet` does not exist. |
| Delete unmapped groups? | **No.** Update path only touches `Currency` + `LastSyncedAt`. |

`TraderDbContext` maps `Mt5Group` → `mt5_groups` with unique `(BrokerId, Name)`. There is **no** `DbSet<PlanGroupMapping>` and **no** `plan_group_mappings` table.

---

## 6. Fake connector — returns the seeded catalog, including **unmapped** paths

`D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`:

```44:53:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Mt5GroupDto>>(_groups);

    public Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct)
    {
        var rows = string.IsNullOrWhiteSpace(group)
            ? _accounts
            : _accounts.Where(a => a.GroupName == group).ToList();
        return Task.FromResult<IReadOnlyList<Mt5AccountDto>>(rows);
    }
```

`GetGroupsAsync` is the in-memory list as seeded. No intersection with a plan catalog.

`DemoBrokerFactory.CreateDefault()` (same file, lines 99–125) seeds:

| Broker | Group path | In architecture §9 `MT5_GROUP_*`? | Still discovered? |
|---|---|---|---|
| ACHIEVER | `demo\Maxmaster` | **No** (Achiever default / provisioning landing group) | **Yes** |
| ACHIEVER | `demo\yo-2step` | Yes (`2STEP_DEMO` / `CORE_DEMO`) | Yes |
| ACHIEVER | `contest\yo-2step` | Yes (`2STEP_REAL` / `CORE_REAL`) | Yes |
| STARWAVEFX | `real\standard` | **No** | **Yes** |

If plan mapping were the fetch filter, `demo\Maxmaster` and `real\standard` would be dropped. They are not. Demo seeder then calls `SyncBrokerAsync` for both brokers (`DemoSeeder.cs` 126–130). Integration `SeedingAndStoreTests` asserts `db.Mt5Groups.Count() > 2` after seed — consistent with **4** upserted groups, not the 9-key env catalog (and not “only mapped yo-*”).

DI (`D:\Prop\src\Infrastructure\DependencyInjection.cs` 31–40) registers those two fakes and `DealIngestionService`. The worker (`D:\Prop\apps\mt5-worker\Worker.cs` 25–30) calls the same `SyncBrokerAsync` for Achiever then StarwaveFX. Same unfiltered path.

Honesty about the fake: the catalog is a **hard-coded demo subset** (3 + 1 names), not a live `GroupTotal`/`GroupNext` walk. That limits **completeness vs a real Manager**, not the plan-mapping question. A future `Mt5ManagerBrokerConnector` must call C++ `GetAllGroups` / `GetGroupDetails` (A39 / A84 already mapping-blind) and must **not** substitute `MT5_GROUP_*`.

---

## 7. Plan mapping exists only as a **display** slot, never as a filter

| Surface | Path | Role | Used as fetch filter? |
|---|---|---|---|
| Entity column | `Domain\Entities\Mt5Group.PlanMapping` (`string?`) | Optional label on the group row | **No** — ingestion never reads or writes it |
| Dashboard DTO | `GroupRowDto.PlanMapping` | Column on Groups page | **No** — `GetGroupsAsync` is `_db.Mt5Groups.ToListAsync` (all rows) |
| Dashboard query | `EfDashboardQueries.GetGroupsAsync` | LEFT-style display of `g.PlanMapping` | **No** — no `Where(g => g.PlanMapping != null)` |
| UI | `apps\web\src\pages\GroupsPage.tsx` | Renders `g.planMapping ?? '—'`; copy: “Plan mappings are labels only — they do not filter ingestion.” | **No** — no `mappedOnly` query |
| `plan_group_mappings` table | specified A20 / A40 | **MISSING** in product | n/a |
| `PlanGroupMapping` type | specified A01 / A40 | **MISSING** (`grep PlanGroupMapping` on `*.cs` = 0) | n/a |
| C# `MT5_GROUP_*` | specified §9 seed | **MISSING** (`grep MT5_GROUP_` on `*.cs` = 0) | n/a |
| C++ `getMt5Group` | `mt5-sdk\src\services\mt5_account_helper.cpp` | **Provisioning write-path** only | **Not called** from C# ingestion |

A40 said: do **not** add a plan field to `Mt5Group`; keep a sibling table. The product instead has `Mt5Group.PlanMapping`. That is a **schema-shape deviation** from A40, **not** a discovery filter. The field is null after ingestion. Do not treat this as “mappings now own groups.”

Dashboard list (`EfDashboardQueries.cs` 59–71) iterates **every** `mt5_groups` row and counts accounts by `(BrokerId, GroupName)`. Unmapped groups remain rows with `PlanMapping == null`.

---

## 8. A40 forbidden needles vs this path

A40 §12: any of these in the discovery/upsert path is a spec fail.

| Needle | In `DealIngestionService` / `EfTradingStore.UpsertGroup*` / Fake `GetGroupsAsync`? |
|---|---|
| `GetGroupsAsync(.*mapping` / `onlyGroups` / `allowedGroups` | **Absent** |
| `plan_group_mappings` inside discovery or upsert | **Absent** (table does not exist) |
| `WHERE g.name IN (SELECT group_path FROM plan_group_mappings` | **Absent** |
| `JOIN plan_group_mappings` as driving table of sync | **Absent** |
| `if (!mappings.Contains(group.Name)) continue;` | **Absent** |
| `PlanGroupMappingOptions` / `MT5_GROUP_*` injected into connector or discovery | **Absent** |
| `EnabledForAnalysis = mappingExists` | **Absent** (`true` constant) |
| `INSERT INTO mt5_groups … SELECT … FROM plan_group_mappings` | **Absent** |
| `MT5_DEFAULT_GROUP` as the sole upserted group | **Absent** (Achiever fake still emits `demo\Maxmaster` **and** two `yo-*` paths) |
| Seeding Achiever exclusively with `demo\yo-*` | **Absent** (`demo\Maxmaster` is first) |

Allowed later uses of mappings (seed / admin / LEFT JOIN query) are **not implemented**. That is fine: empty overlay is a valid production state (A40 §8, Achiever).

---

## 9. Adjacent facts that must **not** be confused with a plan filter

These are real gaps. They do **not** reverse the assigned verdict.

| Observation | Why it is not “filtered by plan mapping” |
|---|---|
| No live C# Manager connector; worker syncs the **fake** catalog | Completeness vs Manager `GroupTotal` is unproven. Filter-by-plan is still absent. |
| Fake catalog is 4 names, not hundreds | Demo seed size. Not an env whitelist. |
| `GetAccountsAsync(string? group)` *can* filter by path | Ingestion passes `null`. A future caller that passes mapped paths only would be the bug — not present today. |
| Worker scoring loop hard-codes logins `10001, 10002, 10003, 99001` | Scoring subset. Groups/accounts/deals for those logins were already ingested without a plan check. |
| `UpsertGroupAsync` update path does not refresh `LastDiscoveredAt` / company / margins | Stale-column bug, not a mapping skip. |
| `EnabledForAnalysis = true` on insert (A40 wanted `false`) | Operator-flag default, independent of mappings. |
| `Mt5Group.PlanMapping` column exists contrary to A40 “no plan field on group” | Unused by discovery. Residual temptation for a later author — see §10. |
| No `PlanGroupMappingIsNotFetchFilterTests` (A40 §13) | Missing **proof** tests. Current integration only asserts `Count > 2`. |
| §12 checkpoints / outbox / live subscribe **MISSING** (A59) | Ingestion is a coarse poll, not Phase 1 complete. Still mapping-blind. |
| Dual interface `IBrokerConnector` vs `IMt5BrokerConnector` | Dead type; unused by this service. |
| C++ `MT5AccountHelper::getMt5Group` still maps plans → paths | Write-path for **new** accounts. Not on the C# collector. Do not port it into `GetGroupsAsync`. |

---

## 10. Residual risk (keep this PASS honest)

The failure mode A40 exists to prevent is a **future** worker that treats `MT5_GROUP_*` / `PlanMapping` as a whitelist.

Highest-risk future edits (do not do these):

1. `GetGroupsAsync` implemented as “return the nine env paths.”
2. `foreach (var group in groups) if (string.IsNullOrEmpty(group.PlanMapping)) continue;`
3. `GetAccountsAsync` called once per `plan_group_mappings.group_path` instead of `null` / every present `mt5_groups` row.
4. Groups page default `?mappedOnly=true`.
5. Copying C++ `Flexy\yo-*` defaults into C# discovery (contradicts §9 `contest\yo-*` **and** would still be a whitelist).

Until A40 §13 tests exist, treat the current PASS as **source-inspection**, not a regression lock.

Recommended later tests (do **not** implement in this task):

- Connector stub returns 12 names, mapping table empty → store receives 12.
- Nine seed mappings + connector returns 50 → store receives 50.
- `DealIngestionService` constructor has no mapping parameter (reflection).
- Seed includes `demo\Maxmaster` and `real\standard`; both persist with `PlanMapping == null`.

---

## 11. C++ boundary (context only; not the SUT)

Local `MT5Manager::GetAllGroups` / `GetGroupDetails` walk `GroupTotal` + `GroupNext` and do **not** read `MT5_GROUP_*` (A39, A84). `mt5_group_probe` is mapping-blind. C# has **zero** `GetAllGroups` symbols today (A84). When a production connector is written, it must compose those C++ walks. It must not invent a C# allow-list. B32 does not re-audit the SDK; it only notes the C# collector already matches the required **call shape** (`GetGroups` then upsert all).

---

## 12. Acceptance against the assigned question

| # | Question | Answer |
|---|---|---|
| 1 | Does `DealIngestionService` filter discovered groups by plan mapping? | **No.** |
| 2 | Does `GetGroupsAsync` accept a mapping / allow-list argument? | **No.** |
| 3 | Are unmapped demo paths (`demo\Maxmaster`, `real\standard`) still upserted? | **Yes** (fake catalog + foreach upsert). |
| 4 | Does account sync walk mappings instead of all accounts? | **No** — `GetAccountsAsync(null)`. |
| 5 | Is `PlanMapping` / `plan_group_mappings` an input to discovery? | **No.** Overlay table missing; entity field unused. |
| 6 | Product source changed by this agent? | **No.** |

**PASS** for “group discovery not filtered by plan mapping.”

Do not upgrade this to “Phase 1 group discovery done” or “≥95% Manager-visible coverage.” Those require a live `GetAllGroups` connector and count-vs-`GroupTotal` tests that do not exist.
