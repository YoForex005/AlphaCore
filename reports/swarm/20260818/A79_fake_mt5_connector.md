# A79 — `InMemoryMt5BrokerConnector` (test double)

**Artifact:** `D:\Prop\reports\swarm\20260818\A79_fake_mt5_connector.md`  
**Date:** 2026-08-18  
**Agent:** A79  
**Status:** specification only — **no product source modified**  
**Scope:** in-process fake for **groups, accounts, deals, events**, including a **measured 5,000-account sync simulation**.  
**Does not implement:** production `Mt5ManagerBrokerConnector`, native Manager DLL, HTTP collector, dealer/provisioning verbs.

**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§6–12, §56, §60, §62, §69.1–3  
**Sibling law (do not contradict):**

| Doc | Binding for this fake |
|---|---|
| A58 | `IMt5BrokerConnector` + registry; **fakes implement that port**; one job loop over `registry.All` |
| A04 / A12 | `GetDeals` is complete `[from,to]` or **fail**; no invented trades; no `PUMP_MODE_DEALS` |
| A13 / A37 / A38 | native volume = `lots * 10_000`; `DealAction` / `DealEntry` numeric values |
| A39 / A40 | discovery = **all** seeded groups; plan map is **not** a fetch filter |
| A07 / A57 | Phase 1 jobs: connect → groups → accounts (~5k) → backfill → live poll + events → checkpoints |
| A10 | `FakeMt5HistorySource` is **absorbed** by this type (do not keep a second history fake) |
| A18 | hermetic: **no sockets**, no Manager DLL, no Postgres inside the connector |
| A21 | optional XAU reconstruction fixtures ride on seeded deals; fake does not reconstruct |
| A27 | unlocks `Mt5BackfillRestartTests`, `Mt5LiveIngestIdempotencyTests`, `DualBrokerIsolationTests` |
| A53 / §62 | disconnect → do not invent deals; empty success ≠ failure |

---

## 0. Verdict (measured now)

| Surface | Path | Classification |
|---|---|---|
| Application port in tree | `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` `IMt5BrokerConnector` + `IBrokerRegistry` | **EXISTS_NEEDS_REFACTOR** vs A58 (no events, no orders, no server time, `long` logins, `string BrokerCode`) |
| Ingestion SUT | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | **EXISTS** — `Connect → GetGroups → GetAccounts(null) → per-login GetDeals + GetPositions` |
| Draft Mt5 port | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` + `Mt5BrokerEvent` | **DEPRECATED** name (A58 §13: delete; Application owns the port) |
| Production connector | `Mt5ManagerBrokerConnector` | **MISSING** |
| **This fake** | `InMemoryMt5BrokerConnector` | **MISSING** — specified here |
| Unit / Integration facts that use a broker fake | `tests/Unit`, `tests/Integration` | **0** (placeholders only; A09 / A10) |

`DealIngestionService` cannot be tested against Achiever/StarwaveFX. The first integration proof of §69 item 3 (**synchronize ~5,000 accounts**) must run against **this fake**, not a live Manager.

---

## 1. Binding laws

1. **Test-only.** The type lives under `tests/`. It is **not** registered in `apps/mt5-worker` except behind an explicit test host. It never opens a socket, never reads `MT5_PASSWORD`, never loads `MT5APIManager64.dll`.
2. **One class, N instances.** Achiever and StarwaveFX are two `InMemoryMt5BrokerConnector` objects in one `InMemoryMt5BrokerRegistry`. No `InMemoryAchieverConnector` fork (A58 §2).
3. **Implements the Application collector port**, not C++ `IMT5Client`. C# tests must not take a C++ ABI dependency.
4. **Read / subscribe only.** No `CreateUser`, `DealerBalance`, `SendTrade`, `CacheExecutedDeal` (A04, A58 §5.3). Mutating the in-memory book is a **test seed API**, not a collector verb.
5. **Do not invent trades.** A disconnected or failed `GetDeals` throws. An empty successful window means “broker has no deals in range,” not “history unavailable.”
6. **Plan map is not a filter.** Seeded groups include paths **outside** `MT5_GROUP_*`. `GetGroupsAsync` returns all of them.
7. **Compound identity.** Tickets and logins are unique **per instance** (`BrokerCode` / `CatalogId`). The same numeric login and deal ticket **must** be seedable on both brokers.
8. **Deals are polled.** `SubscribeAsync` is **not** the live-deal path. There is no `PUMP_MODE_DEALS`. `DealAdd` events fire only when a test **explicitly publishes** them (A07, A12, A58 §5.4).
9. **Volume is native `ulong`.** `1.00` lot = `10_000`. Never `/ 100`. Never convert to destination `OrderQty` (A13, A38, `VolumeConverter.Manager`).
10. **5,000-account census is a first-class seed**, not “add a loop in a test later.” §69.3 / A57 item 3 / A27 integration row 3.

---

## 2. Why this fake exists

Production `IMt5BrokerConnector` talks to a Manager session (A58 `Mt5ManagerBrokerConnector`). CI and Application unit tests **must not**.

| Consumer | What it needs from the fake |
|---|---|
| `DealIngestionService.SyncBrokerAsync` | groups + all accounts + deals window + positions |
| `DiscoverGroupsService` (A30 / A58) | `GetGroupsAsync` unfiltered |
| `SynchronizeAccountsService` (A57 §3) | 5,000 logins, group association, restart-safe iteration |
| `HistoricalBackfillService` | paged `GetDeals` + fail-closed incomplete history |
| `LiveIngestionService` | `SubscribeAsync` user/order/position events **plus** deal poll |
| `DualBrokerIsolationTests` | two instances, colliding numeric tickets |
| Reconstruction / score unit tests | deterministic XAU deal tapes **without** going through EF |

The fake is the **broker**. Persistence, outbox, checkpoints, reconstruction stay real SUTs (or their own fakes). Do not put `TraderDbContext` inside this class.

---

## 3. Placement (when implementation is authorized)

**Do not create these files in this A79 pass.** Product source stays untouched.

```text
tests/Shared/Fakes/InMemoryMt5BrokerConnector.cs
tests/Shared/Fakes/InMemoryMt5BrokerRegistry.cs
tests/Shared/Fakes/InMemoryMt5BrokerConnectorFactory.cs
tests/Shared/Fakes/InMemoryMt5SeedCatalog.cs
tests/Shared/Fakes/InMemoryMt5FaultSchedule.cs
tests/Shared/Fakes/InMemoryMt5CallLog.cs
tests/Shared/Fakes/Mt5SourceEvent.cs          # if Application models are not yet extracted
tests/Shared/Fakes/Adapters/ApplicationContractAdapter.cs

tests/Unit/Mt5/InMemoryMt5BrokerConnectorTests.cs
tests/Unit/Mt5/InMemoryFiveThousandAccountSeedTests.cs
tests/Integration/Fakes/                         # project-ref Shared; do NOT duplicate the type
tests/Integration/Mt5/Mt5BackfillRestartTests.cs # SUT = backfill + this fake (A27)
tests/Integration/Mt5/DualBrokerIsolationTests.cs
```

`tests/Unit` today does **not** reference `src/Mt5`. Keep it that way: the fake depends on **Application + Domain only**.

`tests/Integration` does not reference `src/Mt5` today. The 5k sync test talks to Application services + this fake + (later) Testcontainers Postgres. Adding `src/Mt5` is **not** required for the fake.

Absorb A10’s proposed `FakeMt5HistorySource`. One book of deals is enough.

---

## 4. Interface the fake must satisfy

### 4.1 Target port (A58 — binding)

When Application ports are moved to `src/Application/Abstractions/Brokers/` as A58 specifies, the fake implements **that** `IMt5BrokerConnector` 1:1:

```csharp
public interface IMt5BrokerConnector
{
    BrokerCode Code { get; }
    Guid CatalogId { get; }
    string DisplayName { get; }

    Mt5BrokerConnectionState State { get; }
    bool IsConnected { get; }
    string LastError { get; }
    bool PumpEventsAvailable { get; }

    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);

    Task<IReadOnlyList<Mt5GroupInfo>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<ulong>> GetGroupLoginsAsync(string groupName, CancellationToken ct);

    Task<Mt5UserInfo?> GetUserAsync(ulong login, CancellationToken ct);
    Task<Mt5AccountSnapshotInfo?> GetAccountAsync(ulong login, CancellationToken ct);
    Task<IReadOnlyList<Mt5AccountInfo>> GetAccountsAsync(CancellationToken ct);

    Task<IReadOnlyList<Mt5DealInfo>> GetDealsAsync(
        ulong login, DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken ct);

    Task<IReadOnlyList<Mt5OrderInfo>> GetOrdersAsync(ulong login, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionInfo>> GetPositionsAsync(ulong login, CancellationToken ct);

    Task<IReadOnlyList<Mt5SymbolInfo>> GetSymbolsAsync(CancellationToken ct);
    Task<Mt5TickInfo?> GetTickLastAsync(string sourceSymbol, CancellationToken ct);

    Task<Mt5ServerTime> GetServerTimeAsync(CancellationToken ct);

    IAsyncEnumerable<Mt5SourceEvent> SubscribeAsync(CancellationToken ct);

    ValueTask<bool> TrySubscribeTicksAsync(IMt5TickSink sink, CancellationToken ct);
}
```

DTO field lists: A58 §5.1 (mirror `mt5_types.h`). Volume stays `ulong`. Every DTO is stamped with `Code` + `CatalogId` at the instance boundary.

### 4.2 Current tree adapter (until A58 lands)

Today `DealIngestionService` binds `TraderIntelligence.Application.Contracts.IMt5BrokerConnector`:

```53:63:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
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

The fake **core** is A58-shaped. A thin `AsApplicationContract()` wrapper (or explicit dual implementation) maps:

| Current `Mt5Contracts` | Fake core |
|---|---|
| `BrokerCode` (`ACHIEVER` / `STARWAVEFX` in `Domain.Brokers.BrokerCodes`) | `Code` stored lowercase `achiever` / `starwavefx` (A58); compare ordinal-ignore-case |
| `IsConnectedAsync` | `IsConnected` |
| `GetGroupsAsync` → `Mt5GroupDto` | `Mt5GroupInfo` (same fields as `GroupDetail`) |
| `GetAccountsAsync(group)` | if `group` is null/empty → all accounts; else `GetGroupLogins` + user/snapshot compose |
| `GetDealsAsync(login, from, to)` | `login` cast `checked` to `ulong`; **`to` treated as inclusive** (see §7.3) |
| `GetPositionsAsync` | current book for that login |

`IBrokerRegistry` today:

```65:69:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
public interface IBrokerRegistry
{
    IMt5BrokerConnector Get(string brokerCode);
    IReadOnlyList<IMt5BrokerConnector> All();
}
```

`InMemoryMt5BrokerRegistry` implements this **and** A58 `IMt5BrokerRegistry` once that type exists. `Get` is case-insensitive. Unknown code throws `KeyNotFoundException` (fail closed — no silent empty connector).

Do **not** also implement `TraderIntelligence.Mt5.Connectors.IBrokerConnector`. A58 deletes it. Dual-implementing a deprecated surface freezes the wrong shape.

### 4.3 Factory

```csharp
public interface IMt5BrokerConnectorFactory
{
    IMt5BrokerConnector Create(Mt5BrokerConnectionOptions options);
}
```

`InMemoryMt5BrokerConnectorFactory` ignores host/password/proxy (must **not** log them if present) and returns a new empty connected-or-disconnected instance keyed by `options.Code`. Tests normally `new InMemoryMt5BrokerConnector(...)` and skip the factory.

---

## 5. Connection, clock, identity

### 5.1 Construction

```csharp
public sealed class InMemoryMt5BrokerConnector : IMt5BrokerConnector
{
    public InMemoryMt5BrokerConnector(
        string brokerCode,
        Guid? catalogId = null,
        string? displayName = null,
        IClock? clock = null,
        InMemoryMt5Options? options = null);
}
```

| Field | Rule |
|---|---|
| `Code` | normalize to lowercase; accept `ACHIEVER` / `achiever` |
| `CatalogId` | default `DeterministicGuid.FromString("broker:" + Code)` — same helper as A58 §3 |
| `DisplayName` | default `Achiever` / `StarwaveFX` from architecture names |
| `State` | `Disconnected` until `ConnectAsync` |
| `PumpEventsAvailable` | default `true`; tests may set `false` to simulate no-pump fallback (A07) |
| `LastError` | `""` when healthy; never contains password / proxy material |

Preset catalog ids (must match production seed):

```text
broker:achiever    → DeterministicGuid.FromString("broker:achiever")
broker:starwavefx  → DeterministicGuid.FromString("broker:starwavefx")
```

### 5.2 Connect / disconnect

| Call | Behavior |
|---|---|
| `ConnectAsync` | `IsConnected=true`, `State=Connected` (or `Degraded` if `PumpEventsAvailable=false`), reset event channel, increment `ConnectCount` |
| `DisconnectAsync` | `IsConnected=false`, complete all `SubscribeAsync` enumerables, increment `DisconnectCount` |
| Any query while disconnected | throw `Mt5BrokerUnavailableException` — **not** empty lists (A58 §5.2, §62) |
| `ConnectAsync` while already connected | no-op success (idempotent) |
| `ct` cancelled | throw `OperationCanceledException`; do not flip connected |

Optional: `options.FailNextConnect = true` → throw, leave disconnected, set `LastError` to a non-secret reason (`"simulated connect failure"`).

### 5.3 Clock / server time

Inject `IClock` (A30). Default is a **frozen** `DeterministicClock` at:

```text
2026-01-15T12:00:00.000Z
```

`GetServerTimeAsync`:

- Success: `UtcTimestamp = clock.UtcNow`, `UsedHostFallback = false`.
- `options.ForceHostTimeFallback = true`: still return a timestamp but `UsedHostFallback = true`. Callers **must not** persist that as a checkpoint (A18 / A58). The fake itself never writes checkpoints.

Deal/event times are **absolute `DateTimeOffset`** supplied by the seed. The clock is only for “now” and server-time queries.

### 5.4 Call log (test observer)

Thread-safe counters, resettable:

```text
ConnectCount, DisconnectCount
GetGroupsCount
GetAccountsCount, GetGroupLoginsCount
GetUserCount, GetAccountCount
GetDealsCount, GetDealsByLogin (ConcurrentDictionary)
GetOrdersCount, GetPositionsCount
GetSymbolsCount, GetTickLastCount, GetServerTimeCount
SubscribeCount, EventsPublished, EventsConsumed
```

Used to prove the SUT did **not** open one Manager session per login (5,000 `GetDeals` on **one** instance is correct; 5,000 connector constructions is not).

---

## 6. Groups

### 6.1 Store

Keyed by **exact Manager path** (`demo\Maxmaster`). Comparison is ordinal (backslash preserved). No `group_id bigint` — Infrastructure’s phantom `Mt5Groups.group_id` is a mapping defect (A57); the fake must not invent numeric group ids.

Fields = C++ `GroupDetail` / `Mt5GroupDto`:

| Field | Default in seed |
|---|---|
| `Name` | path |
| `Currency` | `USD` |
| `CurrencyDigits` | `2` |
| `Company` | broker display name |
| `MarginCall` | `100` |
| `MarginStopOut` | `50` |
| `ConnectionsAllowed` | `true` |

### 6.2 API

- `AddGroup(...)` / `AddGroups(...)` — seed only; last write wins on same name.
- `RemoveGroup(name)` — drops the group **and** does **not** delete accounts unless `CascadeRemoveAccounts=true` (default false: accounts keep the old `GroupName` so tests can assert stale association).
- `GetGroupsAsync` — snapshot of **all** groups, stable order: ordinal by name.
- **Forbidden:** reading `DefaultGroupHint`, `MT5_GROUP_*`, `PlanGroupMapping`, or `EnabledForAnalysis` to drop rows.

### 6.3 Required catalogs (so §9 cannot accidentally become the universe)

**Achiever** (`InMemoryMt5SeedCatalog.AchieverGroups`):

| Name | Why it must be present |
|---|---|
| `demo\Maxmaster` | §7 default — **not exclusive** |
| `demo\standard` | unmapped extra |
| `real\vip` | unmapped extra |
| `contest\internal` | unmapped extra |

**StarwaveFX** (`InMemoryMt5SeedCatalog.StarwaveGroups`):

| Name | Role |
|---|---|
| `demo\yo-2step` | §9 mapped |
| `demo\yo-1step` | §9 mapped |
| `contest\yo-2step` | §9 mapped |
| `contest\yo-1step` | §9 mapped |
| `contest\yo-instant` | §9 mapped |
| `demo\yo-payp` | §9 mapped |
| `contest\yo-payp` | §9 mapped |
| `demo\default` | **unmapped** — must still be discovered |
| `contest\other` | **unmapped** — must still be discovered |

`SeedStandardGroups()` loads the catalog for `Code`. Tests that plant only `demo\yo-2step` are **invalid** for discovery proofs.

---

## 7. Accounts (including the 5,000-account census)

### 7.1 Store

Keyed by `ulong Login`. Fields compose A58 `Mt5UserInfo` + `Mt5AccountSnapshotInfo` + group name (`Mt5AccountInfo` / current `Mt5AccountDto`).

| Field | Seed default |
|---|---|
| `Login` | assigned |
| `GroupName` | required |
| `Leverage` | `100` |
| `Balance` / `Equity` | `10_000` |
| `Margin` / `MarginFree` / `Profit` | `0` / `10_000` / `0` |
| `Rights` | `0x0001` (`USER_RIGHT_ENABLED`) |
| `RegistrationAt` | `clock.UtcNow - 365d` |
| `LastAccessAt` | `clock.UtcNow` |

Unknown login on `GetUserAsync` / `GetAccountAsync` → `null` (authoritative miss).  
`GetAccountsAsync` never includes unknown logins.

`GetGroupLoginsAsync(group)`:

- unknown group → **empty success** (Manager-visible group list is `GetGroupsAsync`; a typo in a test is not “broker down”).
- known group → all logins in that group, **ascending**.

### 7.2 Seed helpers

```csharp
void AddAccount(Mt5AccountInfo account);
void AddAccounts(IEnumerable<Mt5AccountInfo> accounts);
void MoveAccount(ulong login, string newGroup);
void RemoveAccount(ulong login);          // also drops that login's deals/orders/positions
```

`AddAccount` to a group that does not exist **auto-creates a minimal group** only if `options.AutoCreateMissingGroups=true` (default **false**). Default fail: throw. Discovery tests must seed groups first.

### 7.3 `SeedFiveThousandAccounts` — binding 5k simulation

This is the method §69.3 tests call. Signature:

```csharp
public InMemoryCensusSeedResult SeedFiveThousandAccounts(InMemoryCensusOptions? options = null);
```

Defaults (`InMemoryCensusOptions`):

| Option | Default | Rule |
|---|---|---|
| `AccountCount` | **5000** | must be `>= 5000` for the §69.3 fact; smaller counts allowed only when the test name does **not** claim 5k |
| `BaseLogin` | `100_001` | avoids manager logins `2027` / `9904` |
| `IncludeStandardGroups` | `true` | loads §6.3 catalog first |
| `Skew` | `LargestTwoTake70Percent` | realistic group sizes |
| `BalanceMode` | `Constant_10000` | or `DeterministicFromLogin` (`10000 + (login % 97)`) |
| `SeedSparseXauTraders` | `0` | see §8.4 |
| `SameNumericLoginsAs` | `null` | if set to another connector, reuse **that** instance’s login numbers (isolation tests) |

**Skew algorithm (`LargestTwoTake70Percent`):**

1. Ensure standard groups exist (count `G >= 2`).
2. Sort group names ordinal.
3. `n0 = (int)(0.40 * AccountCount)`, `n1 = (int)(0.30 * AccountCount)`, remainder spread round-robin across the other groups (at least 1 each if remainder allows).
4. Login `BaseLogin + i` assigned in that order (stable, repeatable).

**Uniform alternative** (`Skew = RoundRobin`): `group = groups[i % G]`.

**Result:**

```csharp
public sealed record InMemoryCensusSeedResult(
    int AccountCount,
    int GroupCount,
    IReadOnlyDictionary<string, int> AccountsPerGroup,
    ulong FirstLogin,
    ulong LastLogin,
    TimeSpan SeedElapsed);
```

Invariants the 5k unit test must lock:

1. `AccountCount == 5000`.
2. `GetAccountsAsync()` after `ConnectAsync` returns **exactly 5000**, distinct logins.
3. `Σ AccountsPerGroup == 5000`.
4. Every account’s `GroupName` is in `GetGroupsAsync()`.
5. At least one account sits in an **unmapped** group (`demo\standard` / `demo\default` / `contest\other`).
6. `SeedElapsed` on a desktop CI agent is **< 200 ms** (no per-account `Task.Delay`).
7. A second `SeedFiveThousandAccounts` **replaces** the census (clear accounts + their deals) unless `options.Append=true`.

### 7.4 Dual-broker 5k

`InMemoryMt5SeedCatalog.SeedIsolatedPair()`:

```text
achiever   = new InMemoryMt5BrokerConnector("achiever")
starwavefx = new InMemoryMt5BrokerConnector("starwavefx")
achiever.SeedFiveThousandAccounts()
starwavefx.SeedFiveThousandAccounts(new() { SameNumericLoginsAs = achiever })
registry.Add(achiever); registry.Add(starwavefx)
```

Same logins `100_001…105_000` on **both** brokers. Persistence SUT must yield **10,000** `mt5_accounts` rows, unique on `(broker_id, login)`.

### 7.5 Sync simulation (what the **test** drives)

The connector does **not** upsert Postgres. The 5k **sync simulation** is:

```text
ConnectAsync
GetGroupsAsync                         → G groups (includes unmapped)
GetAccountsAsync()                     → 5000 accounts   OR
  foreach group: GetGroupLoginsAsync   → same 5000, no extras, no drops
foreach account:
    UpsertAccount                      → SUT
    GetDealsAsync(login, from, to)     → usually empty (census-only)
    GetPositionsAsync(login)           → usually empty
```

That is exactly `DealIngestionService.SyncBrokerAsync` today (`GetAccountsAsync(null)` then per-login deals/positions). The 5k integration fact may call that method **or** the future `SynchronizeAccountsService`.

**Budgets (census-only, no deals, no EF):**

| Step | Max wall time (CI) | Max extra allocations smell |
|---|---|---|
| Seed 5,000 | 200 ms | one list + dictionaries |
| `GetGroupsAsync` | 5 ms | snapshot copy |
| `GetAccountsAsync` (5k) | 50 ms | one new array of DTOs |
| 5,000 empty `GetDealsAsync` sequential | 500 ms | O(1) lookup per login |
| 5,000 empty `GetPositionsAsync` sequential | 500 ms | O(1) lookup |
| Full in-process sync loop above | **2 s** | no `Task.Delay` |

With EF / Testcontainers added, the **connector** portion of the same loop must still meet the 2 s budget; DB time is measured separately.

**Memory:** 5,000 accounts + 0 deals < **20 MB** retained on the connector (excluding test host). 5,000 accounts + 10,000 deals < **50 MB**.

**Concurrency:** all public methods are thread-safe (`lock` or concurrent dictionaries). The SUT may fan out `GetDeals` up to pool size 8 / 4. The fake does **not** enforce pool size unless `options.MaxConcurrentQueries` is set (default unlimited). Tests that prove “do not exceed pool” set `MaxConcurrentQueries=8` and `PerCallDelay=20ms`, then assert the SUT never trips `Mt5PoolExhaustedException`.

---

## 8. Deals

### 8.1 Store

Primary key **inside one instance**: `DealTicket` (`ulong` / `long` at the Application adapter). Duplicate `AddDeal` with the same ticket:

- default `DealCollision = Throw`
- `Replace` — overwrite (test-only correction)
- `Ignore` — keep first (mirrors ledger `ON CONFLICT DO NOTHING`)

Index: `(Login, Time, DealTicket)` so `GetDealsAsync` is a range scan, not a 5k×N filter.

Fields = `Mt5DealDto` / C++ `DealData` / A58 `Mt5DealInfo`:

| Field | Type / law |
|---|---|
| `DealTicket` | `> 0` |
| `Login` | must exist **or** `options.AllowOrphanDeals=true` (default false) |
| `OrderTicket` / `PositionId` | may be `0` on balance deals |
| `Symbol` | source string as-is (`XAUUSDm`, `GOLD`, `EURUSD`) — **no** canonicalization in the connector |
| `Action` | `DealAction` 0–20 (A37) |
| `Entry` | `DealEntry` 0–3 |
| `VolumeNative` | `ulong`; `10_000` = 1.00 lot |
| `Price`, `Profit`, `Commission`, `Swap` | `decimal`; swap = C++ `storage` |
| `Time` | `DateTimeOffset` (UTC) |
| `Comment` | optional |

`AddDeal` does **not** publish `DealAdd` (no deal pump). Tests call `PublishDealAdd(deal)` if they need the event.

### 8.2 `GetDealsAsync` contract

**Complete history or fail** (A12 / A58):

```text
success + list   = every seeded deal with Time in the requested window
throw            = incomplete / disconnected / injected fault
```

Window:

| Surface | Interval |
|---|---|
| A58 `fromInclusive`, `toExclusive` | `from <= t < to` |
| Current `Mt5Contracts` `from`, `to` | `from <= t <= to` (inclusive). Adapter implements this so `DealIngestionService` does not drop the `to` tick. |

Sort: `Time` asc, then `DealTicket` asc.

`from > to` (or `from >= to` on the exclusive API) → empty success.

**Paging simulation:** internally the fake may split a login’s history into pages of `options.DealPageSize` (default **1000**, matching A16 HTTP `limit=1000`). `GetDealsAsync` **must still return the full merged window**. Tests inject `FailAfterDealPages = 1` to throw after the first page — callers must **not** treat the partial vector as complete and must **not** advance `sync_checkpoints`.

`GetRecentDeals` is **not** a public C# method. If a test needs the >40s history-lag hole: seed the deal **only** via `PublishDealAdd` and set `options.HistoryLag = true` so `GetDealsAsync` **omits** deals younger than `HistoryLag` until `PromoteRecentDealsToHistory()`. Default: seeded deals are immediately visible to `GetDealsAsync` (simpler backfill tests).

### 8.3 Balance / non-trading deals

Must be seedable (`Action = Balance/Credit/Commission/...`, empty symbol, `Entry = In`). Reconstruction ignores them (`NormalizedDeal.IsTradingDeal`). The fake must not drop them.

### 8.4 Sparse XAU tape (optional on the 5k census)

`SeedSparseXauTraders = N` (A57 item 4 / A21) attaches a **minimal** completed-XAU lifecycle to the first `N` logins:

```text
per trader k in 0..N-1, for trade t in 1..3:
  IN  Buy  XAUUSD  vol=10000  price=2400+t   time=T0 + 7d*(3k+t-1)
  OUT Sell XAUUSD  vol=10000  price=2401+t   time=that + 2h
```

Tickets: `1_000_000 + 1000*k + 10*t + {1=IN,2=OUT}`. Position id: `500_000 + 10*k + t`.

This is enough for “first 3 completed XAUUSD trades” **without** attaching 15,000 deals to the census. Default `N=0` so the 5k **account** test stays cheap.

Explicit reconstruction fixtures (partial, scale-in, reversal) belong in A21 golden files loaded via `AddDealsFromJson(path)` — not baked into the 5k helper.

### 8.5 Symbols / ticks

`AddSymbol(Mt5SymbolInfo)` / `SetTick(Mt5TickInfo)`.

`GetSymbolsAsync` — all seeded symbols (empty success if none).  
`GetTickLastAsync` — last tick or `null`.  
`TrySubscribeTicksAsync` — default returns `false` (fail-closed, A12). Tests may install an in-memory sink.

The fake **must not** fabricate XAU ticks to make MFE/MAE look complete (A21 §1.4, A45).

---

## 9. Events

### 9.1 Type set (C++ `MT5EventType` / A16 SSE map)

```text
PositionAdd, PositionUpdate, PositionDelete
DealAdd,    DealUpdate,    DealDelete
OrderAdd,   OrderUpdate,   OrderDelete
UserAdd,    UserUpdate,    UserDelete
```

C# name: `Mt5SourceEvent` (A58). Payload is one of deal/order/position/user. Include `EventTimeUtc` (clock or explicit) and `BrokerCode` / `CatalogId`.

Current `Mt5BrokerEvent` (`IBrokerConnector`) is **incomplete** (deal + position only). Do not emit that record from the fake.

### 9.2 Channel

- One `Channel<Mt5SourceEvent>` per connect generation (unbounded by default; `options.EventCapacity` to bound).
- `SubscribeAsync(ct)`: `ReadAllAsync` until disconnect, cancel, or complete.
- Multiple subscribers: **broadcast** (each event delivered to all live subscriptions). Implementation: fan-out list of channels. A single-consumer-only design is **rejected** — live ingest + a test probe must both see the stream.
- After `DisconnectAsync`, readers complete **cleanly** (not hang).
- `SubscribeAsync` while disconnected throws `Mt5BrokerUnavailableException`.

### 9.3 Publish API (test-only)

```csharp
void Publish(Mt5SourceEvent e);
void PublishUserAdd/Update/Delete(Mt5UserInfo user);
void PublishOrderAdd/Update/Delete(Mt5OrderInfo order);
void PublishPositionAdd/Update/Delete(Mt5PositionInfo position);
void PublishDealAdd/Update/Delete(Mt5DealInfo deal);   // does NOT imply GetDeals visibility unless also AddDeal
```

User publish **also** upserts the account book (so live account sync can ignore `GetAccounts` and apply events). Order/position publish upserts those books. Deal publish **does not** upsert history unless `options.DealEventsMutateHistory=true` (default false — matches “OnDealAdd likely silent / history lags”).

### 9.4 What live ingest must still poll

Even with a rich event stream, A58 §5.4 remains:

```text
SubscribeAsync          → users / orders / positions
GetDealsAsync poll      → live deals
Periodic reconciliation → third leg
```

A test named `Live_deals_arrive_only_via_Subscribe` is a **FAIL** against this spec.

### 9.5 Pump off

`PumpEventsAvailable=false`:

- `SubscribeAsync` yields **no** events (complete immediately **or** never publish — pick **complete immediately** so tests do not hang).
- `Get*` request APIs still work.
- Health = degraded. `LastError` may be `"pump unavailable (simulated)"`.

---

## 10. Positions and orders

Separate books, keyed by ticket per login.

`GetPositionsAsync(login)` — current open positions (not history).  
`GetOrdersAsync(login)` — working + recent orders the test seeded (Manager `GetOrders` is “current,” not deal history).

`ReplacePositions(login, list)` — test helper for `DealIngestionService`’s `ReplacePositionsAsync` input.

Default: 5k census seeds **zero** positions and **zero** orders.

---

## 11. Fault schedule

`InMemoryMt5FaultSchedule` (attached to the connector):

| Fault | Effect |
|---|---|
| `DisconnectAfterQueries = N` | Nth query throws unavailable; `IsConnected=false` |
| `FailNextGetDeals` | next `GetDealsAsync` throws `Mt5HistoryIncompleteException` |
| `FailGetDealsForLogin` | that login only |
| `FailAfterDealPages` | throw mid-merge; **no** list returned |
| `FailGetGroups` | throw; SUT must not persist “zero groups” |
| `EmptyGroupsButConnected` | **forbidden as a silent default**; only if test calls `ClearGroups()` while connected (authoritative empty) |
| `PerCallDelay` | `Task.Delay` for pool / timeout tests |
| `MaxConcurrentQueries` | excess throws `Mt5PoolExhaustedException` |
| `CancelHonored` | every `await` observes `ct` |

Incomplete history **never** returns a prefix list. Prefer exceptions over `false` because the current C# port has no `bool` (A58: “Failure ≠ empty list”).

---

## 12. Dual-broker registry

```csharp
public sealed class InMemoryMt5BrokerRegistry : IBrokerRegistry /* + A58 IMt5BrokerRegistry */
{
    public void Add(InMemoryMt5BrokerConnector connector);
    public IMt5BrokerConnector Get(string brokerCode);
    public IReadOnlyList<IMt5BrokerConnector> All();
}
```

Rules:

- `All()` order: `achiever`, then `starwavefx`, then others by code.
- Jobs iterate `All()` — **no** `if (Achiever)` in SUTs (A58).
- Killing / failing one instance must not change the other’s `IsConnected`.
- `Snapshot()` (A58 health) returns non-secret fields only. Assert JSON/text contains no `password`, `ProxyPassword`, `ApiKey`.

---

## 13. Mapping to `DealIngestionService` (today)

```31:58:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
{
    var connector = _registry.Get(brokerCode);
    await connector.ConnectAsync(ct);
    ...
    var groups = await connector.GetGroupsAsync(ct);
    ...
    var accounts = await connector.GetAccountsAsync(null, ct);
    foreach (var account in accounts)
    {
        await _store.UpsertAccountAsync(...);
        var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
        ...
        var positions = await connector.GetPositionsAsync(account.Login, ct);
        await _store.ReplacePositionsAsync(...);
    }
    return insertedDeals;
}
```

5k implications of **this** loop (honest):

- It issues **1 + 5000 + 5000** connector calls after connect (groups + accounts is one shot, then 5k deals + 5k positions).
- That is acceptable **against the fake** and is the measurement we want before anyone “optimizes” by filtering to `MT5_GROUP_*`.
- It is **not** acceptable against a live Manager without a request pool (A04). The fake’s `MaxConcurrentQueries` is how we prove a future worker batches to pool 8 / 4.
- `GetAccountsAsync(null)` must not require the caller to pass a plan group.

A unit test `SyncBrokerAsync_FiveThousandAccounts_UpsertsEachLoginOnce` with an in-memory `ITradingStore` is the **minimum** Application proof of §69.3. Integration then repeats it against Postgres.

---

## 14. Tests this fake must pass (on the fake itself)

Namespace: `TraderIntelligence.Tests.Unit.Mt5`.

| Class | Facts (minimum) |
|---|---|
| `InMemoryMt5BrokerConnectorTests` | disconnected query throws; connect is idempotent; disconnect completes subscribe; DTO stamps `CatalogId` |
| `InMemoryGroupDiscoveryTests` | standard catalogs include unmapped paths; `GetGroupsAsync` count not reduced by planting `DefaultGroupHint` / `demo\yo-2step` only |
| `InMemoryAccountBookTests` | `GetAccounts(null)` vs per-group; unknown login → null; remove account drops deals |
| `InMemoryFiveThousandAccountSeedTests` | §7.3 invariants; second seed replaces; `SeedElapsed < 200ms`; dual-broker same logins stay isolated by `CatalogId` |
| `InMemoryDealWindowTests` | inclusive current-contract window; A58 half-open window; sort order; empty success vs throw; `FailAfterDealPages` returns nothing |
| `InMemoryVolumeAndEnumTests` | `VolumeNative=10000` round-trips; `DealAction.SoCompensation=19`; no `/100` helper on the fake |
| `InMemoryEventStreamTests` | broadcast to two subscribers; user event upserts book; `DealAdd` does **not** appear in `GetDeals` by default; pump-off subscribe completes empty |
| `InMemoryFaultTests` | `FailNextGetDeals`; cancel during delay; `MaxConcurrentQueries` |
| `InMemoryRegistryTests` | two codes; unknown code throws; `All()` iteration; one connector fail leaves the other up |

Integration (A27), **using** the fake, not re-implementing it:

| Class | Must prove |
|---|---|
| `Mt5.Mt5BackfillRestartTests` | 5k census + sparse deals; kill mid-loop (fault schedule); restart; no duplicate `(broker_id, login)` / `(broker_id, deal_ticket)` |
| `Mt5.Mt5LiveIngestIdempotencyTests` | live `PublishDealAdd` + later `AddDeal` history of same ticket → one row |
| `Mt5.DualBrokerIsolationTests` | login `1001` (or `100_001`) on both brokers → two rows |

Replay (A27) may load recorded JSON **into** this fake (`AddDealsFromJson`) instead of inventing a third source.

---

## 15. Forbidden (hard)

| Action | Why |
|---|---|
| Put the class in `src/Mt5` as the production connector | would ship a silent empty broker |
| Filter groups/accounts by `MT5_GROUP_*` or `demo\Maxmaster` | §7 / §9 / A40 |
| Return empty list when disconnected | §62 / A58 — looks like “no traders” |
| Return a partial deal page as success | checkpoint hole (A04) |
| Auto-emit `DealAdd` on `AddDeal` and treat subscribe as live deals | no `PUMP_MODE_DEALS` |
| `CreateUser` / `Deposit` / `SendTrade` on the fake **port** | collector is read-only; seed methods are not port members |
| Convert volume to lots or cTrader qty | A13 / A38 / §1.10 |
| Canonicalize `GOLD` → `XAUUSD` inside the connector | `SymbolNormalizer` is a later stage |
| Open HTTP/TCP or read live `.env` passwords | A18 hermetic split |
| Use EF InMemory **as** the broker | wrong seam (A10); this type is the broker |
| One global static book for both brokers | §10 identity clash |
| Sleep per account in `SeedFiveThousandAccounts` | 5k × delay kills CI and is not a Manager simulation |
| Claim “5k sync done” because the seed method exists | done = SUT upserted 5k rows with checkpoints (A57) |

---

## 16. Acceptance (this design)

The **spec** is done when this file is on disk. The **implementation** is done when:

```text
[ ] InMemoryMt5BrokerConnector exists under tests/ only
[ ] Implements Application IMt5BrokerConnector (current and/or A58)
[ ] SeedFiveThousandAccounts(5000) meets §7.3 invariants in < 200 ms
[ ] Dual instance + same numeric logins isolated by CatalogId
[ ] GetGroupsAsync returns unmapped groups
[ ] GetDeals fail-closed on injected incomplete page
[ ] SubscribeAsync broadcasts user/order/position; deals stay polled
[ ] DealIngestionService.SyncBrokerAsync against the fake + in-memory store
    upserts 5000 accounts (measured count)
[ ] No product source in src/ or apps/ was required to land the fake
[ ] No live Manager, no Pepperstone, no passwords in fixtures
```

Until the 5,000 **upsert** count is measured in a test output, §69.3 remains **FAIL**. Seeding 5,000 objects in a dictionary is not “accounts synchronized.”

---

## 17. Cross-links

| Need | Where |
|---|---|
| Port + registry | A58 |
| 5k as first-useful item | §69.3, A57 item 3, A27 §8.1, A28 |
| Group discovery | A39, A40, §7, §9 |
| Deal enums / volume | A37, A38, A13, `VolumeConverter` |
| Ingest loop | A07, §12, `DealIngestionService` |
| Failure / no invent | A53, §62 |
| Test class names | A27 |
| Absorb history fake | A10 `FakeMt5HistorySource` |

---

## 18. Files read (not modified)

- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Mt5\Utils\DeterministicGuid.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Entities\Mt5{Group,Account,Deal,Position}.cs`
- `D:\Prop\src\Domain\Enums\DealAction.cs`, `DealEntry.cs`
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs`
- `D:\Prop\mt5-sdk\src\core\imt5_client.h` (`GetDeals` complete-history comment)
- `D:\Prop\mt5-sdk\src\core\mt5_types.h` (`GroupDetail`, `DealData`, `MT5EventType`)
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§6–12, §69
- Sibling reports A07, A10, A12, A13, A18, A21, A27, A39, A40, A53, A57, A58

**Written:** `D:\Prop\reports\swarm\20260818\A79_fake_mt5_connector.md` (this file).  
**Product source modified:** none.
