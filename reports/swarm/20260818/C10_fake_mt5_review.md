# C10 — `FakeMt5BrokerConnector`: is group discovery plan-filtered?

| Field | Value |
|---|---|
| Agent | C10 (senior engineer, Fake connector only) |
| Date | 2026-08-18 |
| Assigned | Read `FakeMt5BrokerConnector.cs`. Group discovery not plan-filtered? Write this report. Do not modify product source. |
| Primary file | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` |
| Worktree SHA-256 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| Size | 170 lines (Fake + `BrokerRegistry` + `DemoBrokerFactory` in one file) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §7, §9 |
| Siblings | A39 (Manager `GetAllGroups`), A40 (`plan_group_mappings` is never the fetch filter), A79 (in-memory fake spec), B04 (`src/Mt5` gap), B24 (port dup), B32 (ingestion caller) |
| Product source modified | **No.** This report is the only write. |

---

## 0. Verdict

**YES. Group discovery is not plan-filtered.** That is the **correct** §7 / §9 behavior, not a missing feature.

`FakeMt5BrokerConnector.GetGroupsAsync` returns the in-memory `_groups` list as seeded. It does not read `MT5_GROUP_*`, `PlanMapping`, `EnabledForAnalysis`, `MT5_DEFAULT_GROUP`, or any allow-list. The constructor cannot even be given a plan catalog: the only group input is `IEnumerable<Mt5GroupDto>?`.

Classification of the assigned check: **PASS** on the Fake as written.

This is **not** a claim that:

- a production Manager / HTTP connector exists (it does **not** — this Fake is the only `IMt5BrokerConnector` implementor);
- the seed catalog is a complete Manager-visible set (it is **4** hard-coded names);
- A40 `plan_group_mappings` overlay exists (it does **not**);
- A79 `InMemoryGroupDiscoveryTests` exist (they do **not**);
- disconnected queries fail-closed (they return the seed — adjacent A58/A79 gap, not a plan filter).

The measured fact is narrower and true: **`GetGroupsAsync` on this type is not a plan-map intersection.**

Complementary, not duplicate: B32 already measured `DealIngestionService` (the caller). This file measures the **broker double** that actually owns the group list.

---

## 1. Binding law (quoted)

Architecture §7 (`demo\Maxmaster` is not exclusive):

> The system must dynamically enumerate **all groups accessible to the Manager login**.

Architecture §9 (plan map is a label, not a fetch list):

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

`docs/architecture.md` restates the same pin: “Plan-group mappings are labels, not fetch filters.”

A40 / A79 add the Fake-specific corollary: seed **must** include paths **outside** `MT5_GROUP_*`, and `GetGroupsAsync` must not drop them because a mapping table is empty or a default group hint is planted.

---

## 2. Method

1. Read `FakeMt5BrokerConnector.cs` in full (Fake, `BrokerRegistry`, `DemoBrokerFactory`).
2. Trace `GetGroupsAsync` / `GetAccountsAsync` through Application `IMt5BrokerConnector`, ingestion, EF upsert, dashboard, seeder, DI, worker.
3. Grep `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` for `MT5_GROUP`, `PlanMapping`, `plan_group`, `onlyGroups`, `allowedGroups`, `mappedOnly`, `DefaultGroup` — **0 hits**.
4. Grep product C# for `PlanMapping`, `EnabledForAnalysis`, `GetGroupsAsync`, `MT5_GROUP`.
5. Compare seed names against the nine §9 env paths and A79 required catalogs.
6. **Did not** edit any file under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

---

## 3. `GetGroupsAsync` — identity return, no filter

```14:53:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public FakeMt5BrokerConnector(
        string brokerCode,
        IEnumerable<Mt5GroupDto>? groups = null,
        IEnumerable<Mt5AccountDto>? accounts = null,
        IEnumerable<Mt5DealDto>? deals = null,
        IEnumerable<Mt5PositionDto>? positions = null)
    {
        BrokerCode = brokerCode;
        _groups = groups?.ToList() ?? new List<Mt5GroupDto>();
        _accounts = accounts?.ToList() ?? new List<Mt5AccountDto>();
        _deals = deals?.ToList() ?? new List<Mt5DealDto>();
        _positions = positions?.ToList() ?? new List<Mt5PositionDto>();
    }
    ...
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

Measured:

| Check | Result |
|---|---|
| `GetGroupsAsync` signature | `CancellationToken` only. No `onlyThese` / `mappedOnly` / `IPlanGroupMappingStore`. |
| Body | `return _groups`. No `Where`, `Contains`, `Intersect`, `continue`. |
| Constructor deps | `brokerCode` + four optional enumerables. No `IConfiguration`, no env, no options. |
| `Mt5GroupDto` fields | `Name`, `Currency`, `CurrencyDigits`, `Company`, `MarginCall`, `MarginStopOut`, `ConnectionsAllowed`. **No** `PlanMapping` / `PlanKey` / `EnabledForAnalysis`. The Fake cannot mark “mapped?” because the DTO has nowhere to put it. |
| `GetAccountsAsync` filter | Optional **exact group path** (`a.GroupName == group`). Manager-style mask, **not** a plan overlay. Empty/null → all accounts. |
| File tokens `MT5_GROUP` / `PlanMapping` / `DefaultGroup` / `allowedGroups` | **Absent** (workspace grep on this file = 0). |

`BrokerRegistry` (same file, lines 70–87) is a case-insensitive `Dictionary<string, IMt5BrokerConnector>`. It does not hold or apply group names.

---

## 4. Seed catalog vs §9 — unmapped paths are present

`DemoBrokerFactory.CreateDefault()` (lines 95–127) is the only in-tree seed. DI and `DemoSeeder` both call it.

| Broker | Seeded group path | In §9 `MT5_GROUP_*`? | Still returned by `GetGroupsAsync`? |
|---|---|---|---|
| `ACHIEVER` | `demo\Maxmaster` | **No** (Achiever §7 default / provisioning landing group) | **Yes** |
| `ACHIEVER` | `demo\yo-2step` | Yes (`2STEP_DEMO` / `CORE_DEMO`) | Yes |
| `ACHIEVER` | `contest\yo-2step` | Yes (`2STEP_REAL` / `CORE_REAL`) | Yes |
| `STARWAVEFX` | `real\standard` | **No** | **Yes** |

If this Fake implemented the **forbidden** shape (`Known plan mappings → only these groups`), `demo\Maxmaster` and `real\standard` would be dropped and Starwave would emit the `yo-*` catalog. The opposite happens: **2 of 4** seed names are unmapped, and Starwave’s only group is unmapped.

Account book matches the same four paths (`10001` Maxmaster, `10002` `demo\yo-2step`, `10003` `contest\yo-2step`, `99001` `real\standard`). `GetAccountsAsync(null)` therefore also returns accounts that sit **outside** the plan map.

---

## 5. A40 forbidden needles vs this file

A40 §12: any of these on `GetGroupsAsync` is a spec fail.

| Needle | In `FakeMt5BrokerConnector` / `DemoBrokerFactory` / `BrokerRegistry`? |
|---|---|
| `GetGroupsAsync(.*mapping` / `onlyGroups` / `allowedGroups` | **Absent** |
| Read `MT5_GROUP_*` / `Environment.GetEnvironmentVariable` | **Absent** |
| `if (!mappings.Contains(group.Name)) continue;` | **Absent** |
| `EnabledForAnalysis = mappingExists` | **Absent** (field not on DTO) |
| `MT5_DEFAULT_GROUP` as the sole returned group | **Absent** (Achiever still emits Maxmaster **and** two `yo-*` paths) |
| Drive the list from `plan_group_mappings` | **Absent** (type / table do not exist in C#) |
| `IPlanGroupMappingStore` constructor arg | **Absent** |

---

## 6. Downstream does not re-filter (caller chain)

The Fake is wired as production DI, not a test-only double:

| Consumer | Path | Group behavior |
|---|---|---|
| DI | `D:\Prop\src\Infrastructure\DependencyInjection.cs` 31–34 | `DemoBrokerFactory.CreateDefault()` → two `IMt5BrokerConnector` singletons |
| Seeder | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` 124–130 | Second independent Fake pair → `DealIngestionService.SyncBrokerAsync` both codes |
| Ingestion | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` 38–40 | `GetGroupsAsync(ct)` then `foreach` upsert. **No** skip. (B32 PASS) |
| Store | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` 22–51 | Insert key `(BrokerId, Name)`. Never reads `PlanMapping`. Insert sets `EnabledForAnalysis = true`, leaves `PlanMapping` null. |
| Dashboard | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` 59–71 | `_db.Mt5Groups.ToListAsync` — all rows. Displays `g.PlanMapping` (null). |
| API | `D:\Prop\apps\api\Program.cs` 56 | `GET /api/groups` → all rows |
| UI | `D:\Prop\apps\web\src\pages\GroupsPage.tsx` 9 | Copy: “Plan mappings are labels only — they do not filter ingestion.” Renders `g.planMapping ?? '—'`. |
| Worker | `D:\Prop\apps\mt5-worker\Worker.cs` 29–30 | Same `SyncBrokerAsync` every 30 s |

`Mt5Group.PlanMapping` (`Domain\Entities\Mt5Group.cs` line 15) is a **display slot**. Nothing in this Fake, ingestion, or upsert writes it. A40 wanted a sibling `plan_group_mappings` table instead of a column on `mt5_groups`. Schema-shape deviation, **not** a fetch filter.

Integration `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` asserts `db.Mt5Groups.Count() > 2` after seed. Consistent with **4** upserted groups, not “only mapped yo-*” (2 names) and not the 9-key env catalog.

---

## 7. Adjacent gaps — do not confuse with a plan filter

These are real. They do **not** reverse the assigned verdict.

| Observation | Why it is not “filtered by plan mapping” |
|---|---|
| Catalog is 4 hard-coded names, not `GroupTotal`/`GroupNext` | Demo completeness vs a live Manager. Not an env whitelist. |
| Fake lives in `src/Mt5` and is registered as the worker’s broker | A79 wanted `tests/` only. Placement / production-wire risk. Still mapping-blind. |
| Achiever seed includes `demo\yo-2step` / `contest\yo-2step` | A40: do **not** seed Achiever with YoPips paths. Catalog-composition bug. Those names are **extra**, not a filter that drops Maxmaster. |
| Starwave seed omits every §9 `yo-*` path | Incomplete Starwave fixture. Unmapped `real\standard` is still returned. |
| A79 required extras (`demo\standard`, `real\vip`, `contest\internal`, `demo\default`, `contest\other`) are missing | Seed too small for a discovery **proof**. Still not a plan intersection. |
| `GetGroupsAsync` while disconnected still returns `_groups` | A58/A79: disconnected query should throw, not look like “zero/all groups.” Fail-open, not plan-filter. |
| `GetAccountsAsync(string? group)` *can* filter by path | Ingestion passes `null`. A future caller that iterates only mapped paths would be the bug. |
| Worker scoring hard-codes logins `10001, 10002, 10003, 99001` | Scoring subset. Discovery already upserted all four groups. |
| Unused `IBrokerConnector` (`src/Mt5/Connectors/IBrokerConnector.cs`) | Dead draft (B24: delete). Its `GetGroupsAsync` is also mapping-blind. Fake does **not** implement it. |
| No `InMemoryGroupDiscoveryTests` / `PlanGroupMappingIsNotFetchFilterTests` | Missing **regression lock**. Current proof is source inspection + `Count > 2`. |
| No production `Mt5ManagerBrokerConnector` | When written, it must compose C++ `GetAllGroups` (A39/A84, already mapping-blind) and must **not** substitute `MT5_GROUP_*`. |

Honest metric: **C# demo-discovers 4 canned groups across 2 Fake brokers, including 2 unmapped paths. C# cannot talk to MT5. C# does not use the plan map as a group fetch filter.**

---

## 8. Acceptance against the assigned question

| # | Question | Answer |
|---|---|---|
| 1 | Does `FakeMt5BrokerConnector.GetGroupsAsync` filter by plan mapping? | **No.** Identity return of `_groups`. |
| 2 | Does the method accept a mapping / allow-list argument? | **No.** `CancellationToken` only. |
| 3 | Are unmapped demo paths still in the default catalog? | **Yes** — `demo\Maxmaster`, `real\standard`. |
| 4 | Would a plan-map whitelist drop those two names? | **Yes** — they are not in §9. They are still returned. |
| 5 | Does `GetAccountsAsync` walk `MT5_GROUP_*`? | **No.** Null/empty = all accounts; else exact `GroupName`. |
| 6 | Does any token in this file mention plans? | **No** (grep = 0). |
| 7 | Is this the required §9 shape? | **Yes** — discover all (seeded) groups; mapping is not an input. |
| 8 | Product source changed by this agent? | **No.** |

---

## 9. Files read (not modified)

- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Domain\Entities\Mt5Group.cs`
- `D:\Prop\src\Domain\Entities\Mt5Account.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\web\src\pages\GroupsPage.tsx`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§7, 9
- Sibling reports A39, A40, A79, B04, B24, B32

**Written:** `D:\Prop\reports\swarm\20260818\C10_fake_mt5_review.md` (this file).  
**Product source modified:** none.
