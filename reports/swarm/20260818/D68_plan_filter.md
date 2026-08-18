# D68 — Does ingestion filter by plan groups?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D68_plan_filter.md` |
| Agent | D68 (senior engineer, ingestion × plan-group filter only) |
| Date | 2026-08-18 |
| Assigned | Does ingestion filter by plan groups? Write this file. Do not modify product source. |
| Primary SUT | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| Callers | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`, `D:\Prop\apps\mt5-worker\Worker.cs` |
| Connector | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (`IMt5BrokerConnector`) |
| Store | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| Binding law | Architecture v2 §7 (enumerate **all** manager-visible groups) + §9 (plan map is a **label**, never the fetch list) |
| Siblings | A39, A40, A59, A79, B32, C10, D24, D31 |
| Product source modified | **No.** This report is the only write. |

---

## 0. Verdict (measured)

**No. Ingestion does not filter by plan groups.**

That is the **required** §7 / §9 shape, not a missing feature.

`DealIngestionService.SyncBrokerAsync` discovers every group the connector returns, upserts every name, then enumerates **all** accounts with `GetAccountsAsync(null, ct)`. It never reads `PlanMapping`, `EnabledForAnalysis`, `MT5_GROUP_*`, or a `plan_group_mappings` table. Those C# identifiers are **absent** from product source (`grep PlanGroupMapping|plan_group_mappings|MT5_GROUP_` on `*.cs` = **0**).

Classification of the assigned question: **PASS** on the C# collector as written (filter is absent, as required).

This is **not** a claim that:

- a live Manager / HTTP collector exists (it does **not** — only `FakeMt5BrokerConnector`);
- `plan_group_mappings` is implemented (it is **not**);
- negative tests `PlanGroupMappingIsNotFetchFilterTests` exist (they do **not**);
- Phase 1 §67 “all groups discovered” is proven against Achiever / StarwaveFX (it is **not** — Fake catalog is 4 hard-coded names);
- scoring rebuilds every ingested login (it does **not** — worker hard-codes four logins).

The measured fact is narrower and true: **plan groups are not a fetch filter on any ingestion write path.**

---

## 1. Binding law (quoted, not restated as invention)

Architecture §7 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`):

> `demo\Maxmaster` is not the only group.
>
> The system must dynamically enumerate **all groups accessible to the Manager login**.

Startup/resync shape (§7):

```text
Connect
  ↓
Enumerate groups
  ↓
Upsert groups
  ↓
Enumerate accounts
  ↓
Associate accounts with broker + group
  ↓
Sync history
```

Architecture §9:

> But these mappings must not determine which MT5 groups are fetched.

Correct:

```text
MT5 Manager API → discover all groups
                         ↓
                   optional plan mapping
```

Incorrect (forbidden):

```text
Known plan mappings → only sync these groups
```

`D:\Prop\docs\architecture.md` restates the same pin: “Plan-group mappings are labels, not fetch filters.”

The Groups UI copy matches: `apps\web\src\pages\GroupsPage.tsx` line 8 — “Plan mappings are labels only — they do not filter ingestion.”

---

## 2. Method

1. Read `DealIngestionService.cs` in full (`ITradingStore`, `DealIngestionService`, `ReconstructionScoringService`).
2. Trace `GetGroupsAsync` / `GetAccountsAsync` / `GetDealsAsync` / `UpsertGroupAsync` through Application contracts, Fake connector, EF store, dashboard, seeder, DI, worker.
3. Grep product C# for `PlanMapping`, `EnabledForAnalysis`, `PlanGroupMapping`, `plan_group_mappings`, `MT5_GROUP_`, `mappedOnly`, `getMt5Group`.
4. Read C++ `MT5Manager::GetAllGroups` and `MT5AccountHelper::getMt5Group` to confirm they are **not** on the C# collector.
5. Compare Fake seed names against the nine §9 `MT5_GROUP_*` paths.
6. SHA-256 the files below.
7. **Did not** edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

---

## 3. File hashes (this snapshot)

| Path | Bytes | SHA-256 |
|---|---:|---|
| `src\Application\Ingestion\DealIngestionService.cs` | 4535 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `src\Application\Contracts\Mt5Contracts.cs` | 1858 | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` |
| `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `src\Mt5\Connectors\IBrokerConnector.cs` | — | `6B7AA65F293AF43A548D09BC92332286A5123EDB03DCCD498C2217490CCBC5BC` |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | 12097 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `src\Infrastructure\Persistence\TraderDbContext.cs` | — | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | — | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `src\Infrastructure\DependencyInjection.cs` | — | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `src\Domain\Entities\Mt5Group.cs` | 693 | `05C07CA07C35FCE9D7A5E06B5BF302997E0C092E7E606B5511F43FE2B9623DB3` |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | — | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `apps\mt5-worker\Worker.cs` | — | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` |
| `apps\web\src\pages\GroupsPage.tsx` | — | `4F7874826403712D8AB6A0C5C85E9FD95D7C18F5E02B600E0C5384F387C91E65` |
| `mt5-sdk\src\core\mt5_manager.cpp` | — | `C25AD8CA9ACFBC5B64AB101C5BCDFCD1CF3CA6FE362BFCD2FC84EDC2EA2AFA98` |
| `mt5-sdk\src\services\mt5_account_helper.cpp` | — | `C491AF955EEE6FD08B7228884485614794D1C820AFCCB2C165A164941610F9A8` |

Fake SHA matches C10 / D24. Ingestion SHA is the current collector (B32 read the same control flow).

---

## 4. `SyncBrokerAsync` — every returned group and every account

File: `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`

```32:59:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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

Measured sequence:

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

Account enumeration is **not** `foreach mapping.GroupPath`. The only optional filter on `GetAccountsAsync` is an **exact group path string**. Ingestion always passes `null`.

Deal fetch is keyed by `account.Login` + `[from, to]`. That is a **time window**, not a plan-group whitelist.

Two callers, same unfiltered method:

| Caller | Window | Brokers |
|---|---|---|
| `DemoSeeder.SeedAsync` | `2026-01-01` … `2026-12-31` | `ACHIEVER` then `STARWAVEFX` |
| `apps\mt5-worker\Worker.cs` | `UtcNow-30d` … `UtcNow+1m` | same two codes |

Neither caller passes a group list or plan key.

---

## 5. Constructor isolation — no mapping dependency

```21:30:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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

| Dependency | Present? |
|---|---|
| `IBrokerRegistry` | yes |
| `ITradingStore` | yes |
| `IPlanGroupMappingStore` | **no** (type does not exist in C#) |
| `PlanGroupMappingOptions` | **no** |
| `IConfiguration` / `MT5_GROUP_*` | **no** |
| `MT5AccountHelper` / C++ `getMt5Group` | **no** (`grep getMt5Group` on `*.cs` = 0) |

`ITradingStore` has no mapping method. Group write is `UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, …)` — one discovered DTO at a time.

`ReconstructionScoringService` (same file) rebuilds trades/scores from already-stored deals by `(brokerId, login)`. It never lists groups and never reads plan mapping.

DI (`DependencyInjection.cs` 31–40) registers two Fake connectors + `DealIngestionService`. No mapping options bind.

---

## 6. Port shapes — `GetGroupsAsync` cannot be asked for “mapped only”

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

- `Mt5GroupDto` has **no** `PlanMapping` / `PlanKey` / `EnabledForAnalysis` field. The connector cannot return a “mapped?” flag.
- `GetGroupsAsync` takes **only** `CancellationToken`. No `onlyThese`, `allowedGroups`, `mappedOnly`.
- `GetAccountsAsync` may filter by **exact group path** when the string is non-empty. That is a Manager-style mask, **not** a plan overlay. Ingestion never supplies a path.

Unused sibling `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` declares `GetGroupsAsync(CancellationToken)` and `GetAccountsAsync(Mt5Group group, …)`. **`DealIngestionService` does not use this interface.** `IBrokerRegistry.Get` returns `IMt5BrokerConnector`. The leftover port is still mapping-blind.

---

## 7. Store upsert — every name is written; mapping is not consulted

`EfTradingStore.UpsertGroupAsync` (`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` 22–51):

| Check | Result |
|---|---|
| Skip when `PlanMapping` is null? | **No.** Field is never read. |
| Skip when name ∉ `MT5_GROUP_*`? | **No.** Env is not referenced. |
| Insert only mapped names? | **No.** Insert key is `(BrokerId, Name)` from the DTO. |
| `EnabledForAnalysis` derived from mapping presence? | **No.** Hard-coded `true` on insert. |
| Write `PlanMapping`? | **No.** Column stays null after ingestion. |
| `JOIN plan_group_mappings`? | **No.** Table / `DbSet` does not exist. |
| Delete unmapped groups? | **No.** Update path only touches `Currency` + `LastSyncedAt`. |

`TraderDbContext` exposes `DbSet<Mt5Group>` and **no** `DbSet<PlanGroupMapping>`.

`UpsertAccountAsync` keys on `(BrokerId, Login)` and stores `GroupName` as a string from the account DTO. No join to a plan table.

`UpsertDealAsync` keys on `(BrokerId, DealTicket)`. The only skip is **ticket already present** (idempotency), not plan membership.

---

## 8. Fake connector + demo catalog — unmapped paths still persist

`FakeMt5BrokerConnector.GetGroupsAsync` is an identity return of the in-memory list:

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

`GetDealsAsync` filters `Login` + time only.

`DemoBrokerFactory.CreateDefault()` seeds four group paths:

| Broker | Group path | In architecture §9 `MT5_GROUP_*`? | Still discovered + upserted? |
|---|---|---|---|
| ACHIEVER | `demo\Maxmaster` | **No** (default / landing group) | **Yes** |
| ACHIEVER | `demo\yo-2step` | Yes (`2STEP_DEMO` / `CORE_DEMO`) | Yes |
| ACHIEVER | `contest\yo-2step` | Yes (`2STEP_REAL` / `CORE_REAL`) | Yes |
| STARWAVEFX | `real\standard` | **No** | **Yes** |

If plan mapping were the fetch filter, `demo\Maxmaster` and `real\standard` would be dropped. They are not.

Accounts follow the same four paths (logins `10001`, `10002`, `10003`, `99001`). Ingestion pulls all four because `GetAccountsAsync(null)` does not restrict to `yo-*`.

Integration `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` asserts `db.Mt5Groups.Count() > 2` after seed — consistent with **4** upserted groups, not “only mapped yo-*” (2) and not the 9-key env catalog.

Honesty about the Fake: the catalog is a **hard-coded demo subset** (3 + 1 names), not a live `GroupTotal` / `GroupNext` walk. That limits **completeness vs a real Manager**, not the plan-mapping question.

---

## 9. What *does* filter — do not confuse these with plan groups

| Filter | Where | What it drops | Plan group? |
|---|---|---|---|
| Time window `[from, to]` | `GetDealsAsync` + worker `UtcNow-30d` | Deals outside the window. Worker clock 2026-08-18 drops the June 2026 Fake tape (D31). | **No** |
| Login | `GetDealsAsync` / `GetPositionsAsync` | Other accounts’ tickets | **No** |
| Optional group path | `GetAccountsAsync(string? group)` when non-empty | Accounts whose `GroupName` ≠ that path | **Not used** by ingestion (`null`) |
| Deal-ticket idempotency | `UpsertDealAsync` | Duplicate tickets | **No** |
| Scoring login list `{10001,10002,10003,99001}` | worker + seeder `RebuildTraderAsync` | Other logins are not rescored | **No** — post-ingest scoring subset |
| Manager ACL on a **future** live connect | C++ `GroupTotal` / `GroupNext` | Groups the manager login cannot see | Server ACL, **not** `MT5_GROUP_*` |

The scoring loop hard-code is a **coverage gap** (any extra ingested login is stored but not rebuilt). It is not a plan-group allow-list.

---

## 10. Plan mapping exists only as a display slot

| Surface | Path | Role | Used as fetch filter? |
|---|---|---|---|
| Entity column | `Domain\Entities\Mt5Group.PlanMapping` (`string?`) | Optional label on the group row | **No** — ingestion never reads or writes it |
| Operator flag | `Mt5Group.EnabledForAnalysis` (default `true`; store insert also `true`) | Dashboard “Analysis” column | **No** — ingestion never reads it |
| Dashboard DTO | `GroupRowDto.PlanMapping` | Groups page column | **No** — `GetGroupsAsync` is `_db.Mt5Groups.ToListAsync` (all rows) |
| Dashboard query | `EfDashboardQueries.GetGroupsAsync` | Displays `g.PlanMapping` | **No** — no `Where(g => g.PlanMapping != null)` |
| UI | `GroupsPage.tsx` | `g.planMapping ?? '—'` | **No** — no `mappedOnly` query |
| `plan_group_mappings` table | specified A20 / A40 | **MISSING** in product | n/a |
| `PlanGroupMapping` type | specified A01 / A40 | **MISSING** | n/a |
| C# `MT5_GROUP_*` | specified §9 seed | **MISSING** | n/a |
| C++ `getMt5Group` | `mt5-sdk\src\services\mt5_account_helper.cpp` | **Provisioning write-path** (plan type + phase → group name for **new** users) | **Not called** from C# |

A40 said: do **not** add a plan field to `Mt5Group`; keep a sibling table. The product instead has `Mt5Group.PlanMapping`. That is a **schema-shape deviation** from A40, **not** a discovery filter. After seed/ingest the column is null. Do not treat this as “mappings now own groups.”

---

## 11. C++ boundary (not on the C# collector)

`MT5Manager::GetAllGroups` (`mt5_manager.cpp` 962–982) walks `GroupTotal` + `GroupNext` and pushes every `grp->Group()` name. It does **not** read `MT5_GROUP_*` or `getMt5Group`. `MT5Session::GetAllGroups` is the same walk. `mt5_group_probe.cpp` prints that list and never intersects it with the env catalog.

`MT5HttpClient::GetAllGroups` GETs `/mt5/groups` and returns the JSON array as-is. No client-side plan intersect.

`MT5AccountHelper::getMt5Group(planType, phase)` **does** map plan keys onto group paths. That is how a **new / promoted account** is assigned a group. It is not an enumerator. C# ingestion does not call it. Do **not** port it into `GetGroupsAsync`.

C# has **zero** `GetAllGroups` symbols. When a production connector is written, it must compose those C++ walks and must **not** substitute `MT5_GROUP_*`.

---

## 12. A40 forbidden needles vs this path

A40 §12: any of these in the discovery/upsert path is a spec fail.

| Needle | In `DealIngestionService` / `UpsertGroup*` / Fake `GetGroupsAsync`? |
|---|---|
| `GetGroupsAsync(.*mapping` / `onlyGroups` / `allowedGroups` | **Absent** |
| `plan_group_mappings` inside discovery or upsert | **Absent** (table does not exist) |
| `WHERE g.name IN (SELECT group_path FROM plan_group_mappings` | **Absent** |
| `JOIN plan_group_mappings` as driving table of sync | **Absent** |
| `if (!mappings.Contains(group.Name)) continue;` | **Absent** |
| `PlanGroupMappingOptions` / `MT5_GROUP_*` injected into connector or discovery | **Absent** |
| `EnabledForAnalysis = mappingExists` | **Absent** (`true` constant) |
| `INSERT INTO mt5_groups … SELECT … FROM plan_group_mappings` | **Absent** |
| `MT5_DEFAULT_GROUP` as the sole upserted group | **Absent** (Fake still emits `demo\Maxmaster` **and** two `yo-*` paths) |
| Seeding Achiever exclusively with `demo\yo-*` | **Absent** (`demo\Maxmaster` is first) |

Allowed later uses of mappings (seed / admin / LEFT JOIN display) are **not implemented**. Empty overlay is a valid production state (A40 §8, Achiever).

---

## 13. Tests — no regression lock

| Test | What it proves | Plan-filter proof? |
|---|---|---|
| `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` | After seed, `Mt5Groups.Count() > 2` | Weak **consistent** with 4 names; does **not** assert `demo\Maxmaster` / `real\standard` / `PlanMapping == null` |
| `SeedingAndStoreTests.Deal_upsert_is_idempotent` | Duplicate ticket returns false | Unrelated |
| `PlanGroupMappingIsNotFetchFilterTests` (A40 §13) | Connector stub 12 names + empty mapping → store 12 | **MISSING** |
| `InMemoryGroupDiscoveryTests` (A79) | Fake catalog includes unmapped paths | **MISSING** |

Until those exist, treat this PASS as **source inspection**, not a CI lock.

---

## 14. Residual risk (keep this PASS honest)

The failure mode A40 exists to prevent is a **future** worker that treats `MT5_GROUP_*` / `PlanMapping` as a whitelist.

Highest-risk future edits (do not do these):

1. Implement `GetGroupsAsync` as “return the nine env paths.”
2. `foreach (var group in groups) if (string.IsNullOrEmpty(group.PlanMapping)) continue;`
3. Call `GetAccountsAsync` once per `plan_group_mappings.group_path` instead of `null`.
4. Groups page default `?mappedOnly=true`.
5. Copy C++ `Flexy\yo-*` defaults into C# discovery (contradicts §9 `contest\yo-*` **and** would still be a whitelist).
6. Skip `UpsertGroupAsync` when `EnabledForAnalysis == false`.

---

## 15. Direct answer

**Does ingestion filter by plan groups?**

**No.**

- Collector: `DealIngestionService.SyncBrokerAsync` upserts every `GetGroupsAsync` row and every `GetAccountsAsync(null)` account.
- Connector: Fake `GetGroupsAsync` returns the seeded list with no plan intersection; unmapped `demo\Maxmaster` and `real\standard` remain.
- Store: `UpsertGroupAsync` never reads or writes `PlanMapping`; `EnabledForAnalysis` is hard-coded `true`.
- Schema: no `plan_group_mappings` table; no C# `PlanGroupMapping` type; no `MT5_GROUP_*` bind.
- C++ plan map (`getMt5Group`) is a **provisioning** helper, not on this path.
- Dashboard / Groups page list **all** `mt5_groups` rows and treat plan as a label.

That absence is **correct** under architecture §7 / §9. Completeness of discovery against a live Manager is a **separate, still-open** gap (Fake-only transport).
