# B04 — C# `src/Mt5` gap: `FakeMt5BrokerConnector` + registry + HTTP adapter

**Date:** 2026-08-18  
**Agent:** B04 (senior engineer, read-only of product source)  
**Artifact:** `D:\Prop\reports\swarm\20260818\B04_mt5_gap.md`  
**Product source modified:** **none**  
**Scope:** measured audit of `D:\Prop\src\Mt5` with focus on `FakeMt5BrokerConnector`, `BrokerRegistry` / `IBrokerRegistry`, and every HTTP adapter file that architecture + A16/A30 require.

Supersedes A04 / A57 / A58 **C# file inventory** (those reports described `Class1.cs` or “zero implementations”). This file is the current measured state of the C# MT5 adapter, not a restatement of the C++ SDK.

---

## 0. Verdict (measured, not aspirational)

| Surface | Measured state | Class |
|---|---|---|
| `TraderIntelligence.Mt5` production `.cs` | **4 files** + csproj. `Class1.cs` is **gone**. | EXISTS |
| `IMt5BrokerConnector` | Thin Application port in `Mt5Contracts.cs`. **Not** the §6 / A58 collector surface. | EXISTS_NEEDS_REFACTOR |
| `FakeMt5BrokerConnector` | In-memory test double. **Only** `IMt5BrokerConnector` implementation in the tree. | EXISTS — demo only |
| `BrokerRegistry` | String-keyed dictionary in the **same file** as the fake. | EXISTS_NEEDS_REFACTOR |
| `IBrokerConnector` | Second, unused interface over **persistence entities**. Zero implementers. | DEPRECATED / dead |
| `Mt5BrokerOptions` | Unused options sketch. Defaults `Mode=remote`, `PoolSize=25`, `[Required] RemoteUrl`, invents `ApiKey`. | EXISTS_NEEDS_REFACTOR / UNSAFE-shape |
| `DeterministicGuid` | Present, unused by Fake / registry / seeder. | EXISTS_AND_GOOD (keep) |
| C# HTTP adapter (`Mt5CollectorClient`, SSE reader, JSON map) | **Zero files. Zero `HttpClient`. Zero `/mt5/*` literals in `*.cs`.** | **MISSING** |
| Local Manager adapter (`Mt5ManagerBrokerConnector`, P/Invoke, pool, watchdog) | **Zero files.** | **MISSING** |
| Slot binder (`Mt5BrokerSlotBinder` / §56 keys) | **Zero files.** DI always wires `DemoBrokerFactory.CreateDefault()`. | **MISSING** |
| Tests of Fake / registry / HTTP | Unit + Integration csproj do **not** reference `TraderIntelligence.Mt5`. **0 test classes.** | **MISSING** |

**Phase 1 (“Achiever connected / StarwaveFX connected / all groups discovered / history backfilled / live deals persisted”) is FAIL** if “connected” means a real Manager or the A16 HTTP bridge. What exists is a **seeded in-memory fixture** that the worker polls every 30 s.

Dashboard “connected” is a lie: `EfDashboardQueries.GetBrokersAsync` hard-codes `Connected = true` and `LastEventAt = DateTimeOffset.UtcNow`. It never calls `IMt5BrokerConnector.IsConnectedAsync`.

Honest metric: **C# can demo-ingest 18 canned XAUUSD deals across 2 fake brokers. C# cannot talk to MT5.**

---

## 1. Evidence inventory (read-only)

| Path | What was inspected |
|---|---|
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Entire file: Fake + `BrokerRegistry` + `DemoBrokerFactory` (170 lines). |
| `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | Entire unused dual interface + `Mt5BrokerEvent`. |
| `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | Entire unused options type. |
| `D:\Prop\src\Mt5\Utils\DeterministicGuid.cs` | Entire SHA-256 GUID helper. |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | net8.0; refs Domain + Application; **no** `Http`, `AllowUnsafeBlocks`, native packages. |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | DTOs + `IMt5BrokerConnector` + `IBrokerRegistry`. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Only production consumer of the registry. |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Always registers two Fakes. |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Builds a **second** independent Fake pair to seed. |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Brokers page health is fabricated. |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `"ACHIEVER"` / `"STARWAVEFX"` (uppercase). |
| `D:\Prop\src\Domain\Volume\VolumeConverter.cs` | Official scale `10_000` (classic `Volume()`). |
| `D:\Prop\apps\mt5-worker\{Program.cs,Worker.cs,appsettings.json}` | Seed + 30 s poll of last 30 days; no MT5 config. |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | No Mt5 project reference; no `*.cs` tests on disk. |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | No Mt5 project reference. |
| `D:\Prop\mt5-sdk\src\core\imt5_client.h` | Real transport-agnostic contract. |
| `D:\Prop\mt5-sdk\src\core\mt5_types.h` | DTO + JSON serde (**`DealData.position` omitted**). |
| Architecture §§6–8, §12, §56 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Sibling specs | A04, A12, A16, A30, A39, A40, A58, A59, A81 |

Workspace grep over `D:\Prop\src\**\*.cs` for `HttpClient`, `IHttpClientFactory`, `text/event-stream`, `X-API-Key`, `DllImport`, `PInvoke`, `NativeLibrary`: **no matches**.

Grep for `Mt5CollectorClient` / `Mt5SseReader` / `src/Mt5/Http/`: hits **only** in `A30_implementation_sequence.md` (planned, not created).

---

## 2. File inventory of `src/Mt5` (as found)

```text
D:\Prop\src\Mt5\
  TraderIntelligence.Mt5.csproj
  Configuration\Mt5BrokerOptions.cs          UNUSED sketch
  Connectors\IBrokerConnector.cs             UNUSED dual interface
  Connectors\FakeMt5BrokerConnector.cs       Fake + BrokerRegistry + DemoBrokerFactory
  Utils\DeterministicGuid.cs                 UNUSED helper (keep)
```

**There is no `Http\`, no `Registry\`, no `DependencyInjection.cs` inside Mt5.**  
A30’s planned tree is entirely absent:

```text
src/Mt5/Http/Mt5CollectorClient.cs          MISSING
src/Mt5/Http/Mt5CollectorOptions.cs         MISSING
src/Mt5/Http/Mt5Json.cs                     MISSING
src/Mt5/Http/Mt5SseReader.cs                MISSING
src/Mt5/Registry/Mt5BrokerRegistry.cs       MISSING (logic stuffed into Fake file)
src/Mt5/Registry/BrokerOptions.cs           MISSING
src/Mt5/Connectors/Mt5ManagerBrokerConnector.cs   MISSING
src/Mt5/Configuration/Mt5BrokerSlotBinder.cs      MISSING
src/Mt5/DependencyInjection.cs              MISSING
```

csproj (no HTTP, no interop):

```1:14:D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

---

## 3. Dual contracts (do not ship both)

Two collector ports exist. Only one has an implementer. They disagree on names, types, and verbs.

### 3.1 Application port — what the Fake implements

`D:\Prop\src\Application\Contracts\Mt5Contracts.cs`

```53:69:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
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

public interface IBrokerRegistry
{
    IMt5BrokerConnector Get(string brokerCode);
    IReadOnlyList<IMt5BrokerConnector> All();
}
```

This is **narrower** than architecture §6 (no orders, no subscribe) and **narrower** than A58 (no `CatalogId`, `LastError`, `GetGroupLogins`, `GetUser`/`GetAccount`, `GetServerTime`, fail-closed deals, ticks).

DTOs (`Mt5GroupDto` / `Mt5AccountDto` / `Mt5DealDto` / `Mt5PositionDto`) have **no `BrokerId` / `BrokerCode`**. Identity is stamped later by `DealIngestionService` via `ITradingStore.ResolveBrokerIdAsync`. That works for the demo; it is not the A58 “stamp at the instance boundary” rule.

Login/ticket types are `long`, not SDK `uint64_t`. Volume is `ulong` native (correct family). Deal uses `Swap` not C++ `storage`.

### 3.2 Adapter-layer port — unused, wrong types

`D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` returns **Domain persistence entities** (`Mt5Group`, `Mt5Account`, `Mt5Deal`, `Mt5Position`) and adds server-time + SSE-shaped events:

```5:44:D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs
public interface IBrokerConnector
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }

    Task<IReadOnlyList<Mt5Group>> GetGroupsAsync(...);
    Task<IReadOnlyList<Mt5Account>> GetAccountsAsync(Mt5Group group, ...);
    Task<IReadOnlyList<Mt5Deal>> GetDealsAsync(ulong login, DateTimeOffset from, DateTimeOffset to, ...);
    Task<IReadOnlyList<Mt5Position>> GetPositionsAsync(ulong login, ...);
    Task<DateTimeOffset> GetServerTimeAsync(...);
    IAsyncEnumerable<Mt5BrokerEvent> SubscribeEventsAsync(...);
}
```

Gaps vs the Application port and vs §6:

| Topic | `IMt5BrokerConnector` (used) | `IBrokerConnector` (dead) | §6 / A58 |
|---|---|---|---|
| Implementer | Fake | **none** | one real class, N instances |
| Layer | Application DTOs | Domain **EF entities** | Application DTOs |
| Broker identity | `string BrokerCode` | none | `BrokerCode` + `Guid CatalogId` |
| Accounts | optional `string? group` | **requires** `Mt5Group` entity | compose logins + GetUser + GetAccount |
| Login type | `long` | `ulong` | `ulong` |
| Connected | `IsConnectedAsync` | `IsConnected` property | both + `LastError` + pump flag |
| Orders | **no** | **no** | **required** |
| Subscribe | **no** | `SubscribeEventsAsync` | `SubscribeAsync` |
| Server time | **no** | `GetServerTimeAsync` | **required** (no silent host clock) |
| Events | — | deal + position only | deal/order/position/**user** |
| GetOrders / symbols / ticks | no | no | A58 MUST |

A58 law: **delete `IBrokerConnector`**. Do not implement it. Application owns the port.

`Mt5BrokerEvent` comment claims “SSE or future native binding” but nothing produces the record.

---

## 4. `FakeMt5BrokerConnector` — what it actually does

```6:68:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
public sealed class FakeMt5BrokerConnector : IMt5BrokerConnector
{
    private readonly List<Mt5GroupDto> _groups;
    private readonly List<Mt5AccountDto> _accounts;
    private readonly List<Mt5DealDto> _deals;
    private readonly List<Mt5PositionDto> _positions;
    private bool _connected;
    ...
    public Task ConnectAsync(CancellationToken ct)
    {
        _connected = true;
        return Task.CompletedTask;
    }
    ...
    public Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Mt5GroupDto>>(_groups);
    ...
    public void AddDeal(Mt5DealDto deal) => _deals.Add(deal);
}
```

### 4.1 Behaviours that are real (and useful for tests)

- Two independent instances can coexist (`ACHIEVER` vs `STARWAVEFX`) — this is the **only** multi-broker proof in C#.
- `GetAccountsAsync(group)` filters by `GroupName` when `group` is non-blank; `null`/whitespace returns all. `DealIngestionService` always passes `null` — **does not** use `MT5_DEFAULT_GROUP` / `MT5_GROUP_*` as a fetch filter. That part of §9 is accidentally correct for the demo.
- `GetDealsAsync` filters `Login` + inclusive `[from, to]` on `Mt5DealDto.Time`.
- `GetPositionsAsync` filters by login.
- `AddDeal` lets a test inject a later deal without rebuilding the factory (unused in product).
- Volume helper `DemoBrokerFactory.Lots` uses **`10_000`** — same as `VolumeConverter.ManagerVolumeScale` and official `MTAPI_VOLUME_DIV`. **Does not** repeat the `mt5_types.h` “hundredths” comment bug (A81).

### 4.2 Behaviours that are fake / wrong for a collector

| Behaviour | Measured | Why it is a gap |
|---|---|---|
| `ConnectAsync` | always succeeds; ignores `ct` | No Manager DLL, no HTTP health, no proxy, no IP-block path. “Connected” is a bool flip. |
| `DisconnectAsync` | flips the bool | No SSE teardown, no pool return. |
| `IsConnectedAsync` | returns `_connected` | Never probes `/mt5/health`. Starts `false` until first `Connect`. |
| `LastError` / pump flag | **absent** | Cannot express no-pump / IP-block / degraded. |
| Cancellation | ignored on every method | A cancelled worker still “succeeds”. |
| Thread safety | raw `List<T>` | Worker + `AddDeal` would race. |
| Encapsulation | `GetGroupsAsync` / unfiltered `GetAccountsAsync` return the **live backing lists** | Callers can mutate the fixture. Filtered queries copy. |
| Incomplete history | always “success + list” | Violates `IMT5Client::GetDeals` complete-history contract (false ≠ empty). |
| `GetRecentDeals` merge | **absent** | Cannot simulate the >40 s DealRequest lag hole. |
| Orders | **absent** | §6 / A58 MUST. |
| Subscribe / SSE | **absent** | §12 live leg missing on the used interface. |
| Server time | **absent** | Checkpoints cannot use broker time. |
| Users vs accounts | single `Mt5AccountDto` | No `GetUser` (name/email/rights/reg) vs `GetAccount` (credit/floating/storage/margin_level). |
| Group logins | **absent** | Cannot compose accounts the SDK way. |
| Symbols / ticks | **absent** | No `mt5_symbol_metadata` / `mt5_ticks_xauusd` path. |
| Positions in default fixture | **empty** | `CreateDefault()` never passes `positions:`. Every `GetPositionsAsync` is `[]`. |
| `AddDeal` | no ticket uniqueness | Duplicate `(login, dealTicket)` is allowed. |
| Failures | none | Cannot test `Mt5BrokerUnavailableException`, page truncation, or empty-vs-unsupported. |

`ConnectAsync` is invoked on **every** `DealIngestionService.SyncBrokerAsync` cycle. The Fake never disconnects. There is no watchdog, no reconnect metric, no `PumpEventsAvailable`.

### 4.3 DTO field gaps vs C++ (`mt5_types.h`)

| C++ struct | C# DTO | Missing / renamed (collector-relevant) |
|---|---|---|
| `GroupDetail` | `Mt5GroupDto` | **Complete** for Phase 1 group upsert. |
| `UserData` | *(none)* | name, email, country/city/phone, registration, last_access, rights. |
| `AccountData` | `Mt5AccountDto` | credit, margin_level, floating, storage. Balance/equity/margin/profit present. |
| `DealData` | `Mt5DealDto` | `storage` → `Swap` (name drift). `position` **is** present (better than HTTP JSON). Time is `DateTimeOffset` not unix. No `broker_id`. |
| `PositionData` | `Mt5PositionDto` | storage/swap on DTO **missing** (entity has `Swap`; DTO does not). No comment, no `time_update`. Direction is `TradeDirection` not action 0/1. |
| `OrderData` | **none** | entire type missing. |
| `SymbolData` / `TickData` / `MT5Event` | **none** | |

`EfTradingStore.ReplacePositionsAsync` therefore always writes `Swap = 0` (property default) even if a future Fake grew swap — the DTO cannot carry it.

---

## 5. `DemoBrokerFactory` — canned universe

Hard-coded codes match `BrokerCodes` (**uppercase**), **not** A58’s lowercase `achiever` / `starwavefx`.

```4:7:D:\Prop\src\Domain\Brokers\BrokerCodes.cs
public static class BrokerCodes
{
    public const string Achiever = "ACHIEVER";
    public const string StarwaveFx = "STARWAVEFX";
}
```

### 5.1 Census (exact)

| Broker | Groups | Accounts | Deals | Positions | Symbols |
|---|---:|---:|---:|---:|---|
| `ACHIEVER` | 3 | 3 | 12 | 0 | `XAUUSD` only |
| `STARWAVEFX` | 1 | 1 | 6 | 0 | `XAUUSD` only |
| **Total** | **4** | **4** | **18** | **0** | 1 |

Achiever groups (good: **not** only `demo\Maxmaster`):

- `demo\Maxmaster`
- `demo\yo-2step`
- `contest\yo-2step`

Accounts:

| Login | Group | Balance / Equity / Profit | Deals |
|---:|---|---|---|
| 10001 | `demo\Maxmaster` | 10_000 / 10_240 / +240 | 3 closed round-trips (mix of win/loss, one short) |
| 10002 | `demo\yo-2step` | 5_000 / 4_820 / −180 | 3 closed losers, lots 0.10 → 0.20 → 0.40 (martingale-shaped) |
| 10003 | `contest\yo-2step` | 25_000 / 25_000 / 0 | **zero deals** (empty-success case) |
| 99001 | `real\standard` | 8_000 / 8_110 / +110 | 3 small winners @ 0.05 lot |

All deals are `DealAction.Buy/Sell` + `DealEntry.In/Out`. **No** balance/credit/commission/dividend deals. Comments are the literals `"open"` / `"close"`, not broker comments.

Clock origin: `2026-06-01T08:00:00+00:00`. Each close is `open + 45 minutes`. StarwaveFX deals are `t0+1 day`.

Volume examples (scale 10_000): `0.10 lot → 1_000`, `0.05 lot → 500`. Correct vs A81 / `VolumeConverter`.

### 5.2 Why the worker does not “live ingest” this fixture

`apps/mt5-worker/Worker.cs` each cycle:

```27:30:D:\Prop\apps\mt5-worker\Worker.cs
var from = DateTimeOffset.UtcNow.AddDays(-30);
var to = DateTimeOffset.UtcNow.AddMinutes(1);
await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
```

On **2026-08-18**, `from` ≈ **2026-07-19**. Demo deals live in **2026-06-01/02** → **outside the window**. After `DemoSeeder` (which uses `2026-01-01` … `2026-12-31`) the 30 s loop’s `GetDealsAsync` returns **empty**. Positions stay empty. This is a **measured** Fake+window mismatch, not a live MT5 feed.

Seeder and DI each call `CreateDefault()` **separately**. Two disconnected fixture graphs. `AddDeal` on the DI singleton cannot affect the already-persisted seed rows.

---

## 6. `BrokerRegistry` — exists, too small

```70:87:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
public sealed class BrokerRegistry : IBrokerRegistry
{
    private readonly Dictionary<string, IMt5BrokerConnector> _connectors;

    public BrokerRegistry(IEnumerable<IMt5BrokerConnector> connectors)
    {
        _connectors = connectors.ToDictionary(c => c.BrokerCode, StringComparer.OrdinalIgnoreCase);
    }

    public IMt5BrokerConnector Get(string brokerCode)
    {
        if (!_connectors.TryGetValue(brokerCode, out var connector))
            throw new KeyNotFoundException($"Unknown broker '{brokerCode}'.");
        return connector;
    }

    public IReadOnlyList<IMt5BrokerConnector> All() => _connectors.Values.ToList();
}
```

### 6.1 What works

- Case-insensitive lookup (`achiever` would resolve `ACHIEVER`).
- `All()` copies values (safe).
- Duplicate `BrokerCode` at construction throws (`ToDictionary`) — fail fast.
- Infrastructure registers **one class, two instances** — the §6 “do not fork Achiever/Starwave codebases” rule is respected **for the Fake**.

### 6.2 Gaps vs A58 `IMt5BrokerRegistry`

| A58 | Today | Gap |
|---|---|---|
| Port name `IMt5BrokerRegistry` in `Application/Abstractions/Brokers/` | `IBrokerRegistry` in `Mt5Contracts.cs` | rename / move |
| Key type `BrokerCode` | raw `string` | no normalize-on-type |
| `TryGet` | no | callers must catch `KeyNotFoundException` |
| `GetRequired(BrokerCode)` | `Get(string)` | same idea, weaker type |
| `Snapshot()` health (masked login, pool, pump, last error, **no secrets**) | **none** | dashboard invents connected=true |
| Built from §56 slot binder + factory | built from `DemoBrokerFactory` | no env, no Mode check |
| Empty registry → host fail | empty `All()` is legal; `Get` throws later | silent no-op possible |
| Lives in `src/Mt5/Registry/` | stuffed under `Connectors\FakeMt5BrokerConnector.cs` | test double + production registry mixed |

### 6.3 Consumers do not iterate the registry

`DealIngestionService.SyncBrokerAsync(string brokerCode, …)` is correctly **broker-agnostic**.

The worker is **not**:

```29:34:D:\Prop\apps\mt5-worker\Worker.cs
await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
{
    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
```

Adding a third Fake to DI would **not** be ingested or scored. That is the opposite of A58 §9 (“foreach `registry.All`”). The `login >= 99000` branch is a second forbidden broker switch.

`SyncBrokerAsync` never:

- reads `IsConnectedAsync` after connect,
- uses `SyncCheckpoint`,
- fail-closes on incomplete deals,
- writes outbox,
- subscribes,
- disconnects,
- walks `registry.All()`.

`ITradingStore` / `SyncCheckpoint` table exist; **no connector or ingestion path writes a checkpoint**.

---

## 7. HTTP adapter files — **absent**

### 7.1 Product C#

| Search | Result |
|---|---|
| `src/Mt5/Http/**` | directory does not exist |
| `new HttpClient` / `IHttpClientFactory` in `D:\Prop\src` | **0** |
| `/mt5/health`, `/mt5/events/stream`, `X-API-Key` in `*.cs` | **0** |
| JSON map for `DealData` / SSE `data:` lines | **0** |
| Timeout knobs (5 s REST, 2 s connect, SSE low-speed 60 s) | **0** |

The only C# type that *mentions* remote HTTP is the unused options sketch:

```24:44:D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs
    /// <summary>
    /// "local" or "remote" (HTTP bridge).
    /// </summary>
    [Required]
    public string Mode { get; set; } = "remote";

    public int PoolSize { get; set; } = 25;
    ...
    [Required]
    public string? RemoteUrl { get; set; } // e.g. http://localhost:8080

    public string? ApiKey { get; set; }
```

Nothing binds `IOptions<Mt5BrokerOptions>`. Worker `appsettings.json` is logging-only. If this type were bound as-is it would **require** `RemoteUrl` even for local Manager mode, default pool **25** (architecture Achiever **8** / StarwaveFX **4**), and default mode **remote** (architecture §56 `MT5_MODE=local`).

Invented keys vs §56 allow-list (A58 / A75): `RemoteUrl`, `ApiKey`, `ProxyType`, `EgressIp` (Achiever’s printed key is `ACHIEVER_EGRESS_IP`), unprefixed `ProxyLogin`/`ProxyPassword`. **Do not promote this type.**

Password and proxy password sit on the POCO. That is acceptable as a process secret holder **only if never logged / never written to `brokers`**. Today it is unused, so it is not leaking — but the shape is the same trap A19/A58 already flagged.

### 7.2 What A16 already froze on the wire (C++ `MT5HttpClient`)

Collector-relevant REST (must be **read**, not re-invented):

| Method | Path | C++ method | C# today |
|---|---|---|---|
| `GET` | `/mt5/health` | `IsConnected` (5 s cache, `success && connected`) | **MISSING** |
| `GET` | `/mt5/groups` | `GetAllGroups` | **MISSING** |
| `GET` | `/mt5/groups/count` | `GroupTotal` | **MISSING** |
| `GET` | `/mt5/groups/{name}/logins` | `GetGroupLogins` | **MISSING** |
| `GET` | `/mt5/users/{login}` | `GetUser` | **MISSING** |
| `GET` | `/mt5/users/logins?group=` | `GetUserLogins` | **MISSING** |
| `GET` | `/mt5/accounts/{login}` | `GetAccount` | **MISSING** |
| `GET` | `/mt5/accounts/{login}/positions` | `GetPositions` | **MISSING** |
| `GET` | `/mt5/accounts/{login}/deals?from=&to=` + cursor/page | `GetDeals` (max 10_000 GETs; partial → `false`) | **MISSING** |
| `GET` | `/mt5/symbols/count` / `/{pos}` / `/name/{name}` / `/{sym}/tick` | symbol + last tick | **MISSING** |
| `GET` | `/mt5/server/time` | `GetServerTime` | **MISSING** |
| `GET` | `/mt5/events/stream` | SSE → 12 event type strings | **MISSING** |

A16 **unsupported** on remote (C# must fail closed, not empty):

- `GetGroupDetails`
- `GetOrders`
- `GetRecentDeals` / `CacheExecutedDeal`
- `SubscribeTicks`
- `GetChart` / news-calendar

A30 additionally wants **new** collector routes (`GET /mt5/groups/details`, `GET /mt5/accounts/{login}/orders`) on a **read-only** `mt5-collector` — also **not built**. Until those exist, a C# HTTP client cannot satisfy A58 `GetOrdersAsync` / full `GetGroupsAsync` details in remote mode.

### 7.3 Dealer paths the C# collector must **never** grow

These exist on C++ `MT5HttpClient` because the SDK was extracted from YoPips admin. A30 says refuse (404/405). Fake correctly does **not** expose them. Keep them off `IMt5BrokerConnector`:

```text
POST   /mt5/users
DELETE /mt5/users/{login}
PUT    /mt5/users/{login}/password | /group | /leverage | /rights
POST   /mt5/users/{login}/check-password
POST   /mt5/accounts/{login}/balance | /deposit | /withdraw
POST   /mt5/dealer/order
```

### 7.4 HTTP-specific landmines a future `Mt5CollectorClient` must not copy blindly

Quoted from C++ serde (A04 / A16 already documented; still open):

```335:349:D:\Prop\mt5-sdk\src\core\mt5_types.h
inline void to_json(nlohmann::json& j, const DealData& d) {
    j = {{"ticket",d.ticket},{"login",d.login},{"order",d.order},{"symbol",d.symbol},
         {"action",d.action},{"entry",d.entry},{"volume",d.volume},{"price",d.price},
         {"profit",d.profit},{"commission",d.commission},{"storage",d.storage},
         {"time",d.time},{"comment",d.comment}};
}
inline void from_json(const nlohmann::json& j, DealData& d) {
    d.ticket = j.value("ticket",(uint64_t)0); d.login = j.value("login",(uint64_t)0);
    d.order = j.value("order",(uint64_t)0); d.symbol = j.value("symbol","");
    d.action = j.value("action",(uint32_t)0); d.entry = j.value("entry",(uint32_t)0);
    d.volume = j.value("volume",(uint64_t)0); d.price = j.value("price",0.0);
    d.profit = j.value("profit",0.0); d.commission = j.value("commission",0.0);
    d.storage = j.value("storage",0.0); d.time = j.value("time",(int64_t)0);
    d.comment = j.value("comment","");
}
```

There is no `"position"` key in either function. Local `extractDeal` fills `DealData.position`; remote JSON drops it.

- **`DealData.position` is dropped on the JSON path.** Reconstruction key is `broker_id + position_id`. C# HTTP adapter must read `position` **if present** and **fail closed** if history deals arrive without it (do not persist `position_id = 0` as authoritative).
- Group names contain `\`. C++ concatenates utf8(name) **without URL-encoding** (`/mt5/groups/` + name + `/logins`). A C# client must encode; the remote service must accept encoded names. Today neither C# nor a product collector exists to fix this.
- `has_more` without `next_cursor` is failure (A16 §2.1). Empty array + `success` is “no deals”, not “give up”.
- Auth header is `X-API-Key`. §56 has **no** `MT5_API_KEY` / `MT5_REMOTE_URL`. See §8.
- Default remote URL in C++ examples is **HTTP**, not HTTPS (A19). Do not bake `http://localhost:8080` from `Mt5BrokerOptions` into production.

SSE types that must be mapped (A16 §3):  
`PositionAdd/Update/Delete`, `DealAdd/Update/Delete`, `OrderAdd/Update/Delete`, `UserAdd/Update/Delete`.  
Unknown types dropped. No tick SSE. Prefix `data: ` (6 chars including space).

---

## 8. Design tension (do not paper over)

Three written laws disagree on **how** C# should reach MT5. B04 records the conflict; it does not pick a winner by inventing keys.

| Doc | Transport for `apps/mt5-worker` |
|---|---|
| Architecture §56 / A58 / A75 | `MT5_MODE=local`. **No** remote URL / API key in the allow-list. `Mode=remote` → **fail closed**. Do not bind `Mt5BrokerOptions.RemoteUrl`. |
| A16 / A04 §12 | Prefer **one** C++ Manager owner. C# speaks the **existing** `MT5HttpClient` REST+SSE contract. `src/Mt5` is the natural home of that client. |
| A30 I2 | Build a **new** read-only `apps/mt5-collector` + C# `Mt5CollectorClient` (`src/Mt5/Http/*`). |

**Measured product implements none of the three.** The running worker talks only to `FakeMt5BrokerConnector`.

Implementation constraint that is not in dispute:

1. **One** `IMt5BrokerConnector` class family (plus a Fake for tests). Not `Mt5.Achiever` / `Mt5.StarwaveFx`.
2. Two configured **instances** in a registry.
3. Collector is **read/subscribe**. No `SendTrade` / `Withdraw` / `CreateUser` on the port.
4. Until architecture **prints** remote keys in §56, a product host must not invent `MT5_REMOTE_URL`. A test/`remote` transport may exist behind an explicit factory used only in labs, or behind a collector process that still consumes §56 **Manager** keys on the C++ side.
5. Do not treat Fake as a third production transport.

---

## 9. Wiring (DI / seed / dashboard / worker)

```31:34:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

- No `IOptions`, no env, no `Mode` branch, no factory.
- In-memory EF is used when the connection string is missing or contains `<SECRET>`.
- `DemoSeeder` writes catalog rows with **hand-picked GUIDs** (`aaaaaaaa-…aaa1` / `aaa2`), **not** `DeterministicGuid.FromString("broker:achiever")`.
- Seeder `Broker.Mode = "local"` while `Mt5BrokerOptions` defaults `"remote"` — catalog and options sketch disagree.
- Seeder stores Achiever `57.128.141.65:443` login `2027` and StarwaveFX `84.201.6.142:443` login `9904` (architecture numbers) but the connector **never uses them**.
- Dashboard `ConnectedBrokers` = count of `Enabled` catalog rows; `Mt5Healthy = brokers > 0`; per-row `connected: true` always.

`DeterministicGuid` is the right tool for stable `brokers.id` without a `BROKER_ID` env key. **Zero call sites** outside its own file.

---

## 10. Coverage scorecard

Legend: **FAKE** = in-memory only; **ABSENT** = no type/method; **DEAD** = type exists, unused.

### 10.1 Architecture §6 sketch

| §6 method | C# now |
|---|---|
| `ConnectAsync` / `DisconnectAsync` | **FAKE** bool |
| `GetGroupsAsync` | **FAKE** 4 canned groups |
| `GetAccountsAsync` | **FAKE** 4 canned accounts (not composed from logins) |
| `GetDealsAsync` | **FAKE** 18 canned deals; no paging; no fail-closed |
| `GetOrdersAsync` | **ABSENT** |
| `GetPositionsAsync` | **FAKE** always empty in default fixture |
| `SubscribeAsync` | **ABSENT** on used port; **DEAD** on `IBrokerConnector` |

### 10.2 A58 collector port (adjusted to SDK)

| Member | C# now |
|---|---|
| `Code` / `CatalogId` / `DisplayName` | string code only |
| `State` / `LastError` / `PumpEventsAvailable` | **ABSENT** |
| `GetGroupLoginsAsync` / `GetUserAsync` / `GetAccountAsync` | **ABSENT** |
| `GetDealsAsync` fail-closed + merge recent | always success |
| `GetOrdersAsync` / `GetSymbolsAsync` / `GetTickLastAsync` | **ABSENT** |
| `GetServerTimeAsync` (`UsedHostFallback`) | **ABSENT** (dead cousin on `IBrokerConnector`) |
| `SubscribeAsync` / `TrySubscribeTicksAsync` | **ABSENT** |
| Slot binder §56 only | **ABSENT** |
| `Mt5ManagerBrokerConnector` | **ABSENT** |
| HTTP `Mt5CollectorClient` | **ABSENT** |

### 10.3 `IMT5Client` (41 methods) vs used C# port

Collector **MUST / COMPOSE** that the Fake does **not** cover:  
`GetLastError`, `GetEventQueue`, `GetUser`, `GetAccount` (as its own call), `GetUserLogins` / `GetGroupLogins`, `GetOrders`, `GetDeals` paging contract, `GetRecentDeals`, `SymbolTotal` / `GetSymbol*`, `GetTickLast`, `SubscribeTicks` (fail-closed), `GroupTotal` / `GetAllGroups` / `GetGroupDetails`, `GetServerTime`.

Collector **OUT** (correctly not on Fake):  
`CreateUser`, `DeleteUser`, `UpdateUser*`, `ChangePassword`, `CheckPassword`, `DealerBalance`, `Deposit`, `Withdraw`, `DealerSendOrder`, `SendTrade`, `CacheExecutedDeal`, news/calendar.

**Keep that OUT list.** Expanding the Fake “to match IMT5Client” would be a regression.

### 10.4 Numeric snapshot

| Metric | Count |
|---|---|
| Production C# files in `src/Mt5` | 4 |
| HTTP adapter files | **0** |
| Real MT5 transports in C# | **0** |
| `IMt5BrokerConnector` implementations | **1** (Fake) |
| Brokers the Fake can represent | 2 (demo) |
| Canned deals | 18 |
| Canned positions | 0 |
| §6 methods implemented for real I/O | **0 / 7** |
| A58 MUST members implemented for real I/O | **0** |
| Unit/integration tests referencing Mt5 | **0** |
| `HttpClient` usages under `src/` | **0** |

---

## 11. Tests (must exist before claiming the Fake is a harness)

`TraderIntelligence.Tests.Unit` references Domain, Application, Fix.CTrader — **not Mt5**.  
`TraderIntelligence.Tests.Integration` references Domain, Application, Infrastructure, Fix.CTrader — **not Mt5**.  
No `*.cs` tests are present under `D:\Prop\tests\Unit` or `D:\Prop\tests\Integration` in this tree.

A58 §14 tests that are **unwritten**:

| Test | Would prove | Today |
|---|---|---|
| Two fakes, one `DealIngestionService` | no duplicated business logic | seeder/worker hard-code two codes |
| Same login `9904` on both brokers → two rows | §10 | not covered (fixture logins do not collide) |
| `GetGroupsAsync` not filtered by `DefaultGroupHint` / `MT5_GROUP_*` | §9 | accidentally true; no assertion |
| Incomplete `GetDeals` → fail, checkpoint unchanged | A04 contract | Fake **cannot** express incomplete |
| Registry unknown code throws / `TryGet` false | lookup | untested |
| Snapshot JSON has no password | §55 | no snapshot type |
| Empty registry → host fail | no silent worker | DI always inserts two fakes |
| Backing-list encapsulation | groups list not mutated by caller | leak exists, no test |
| Volume `Lots(0.10) == 1000` | A81 | untested, but code is correct |
| HTTP mapping: `position` required | serde bug | no client |

The Fake **is** the right test double **once** `IMt5BrokerConnector` is widened and the Fake grows: incomplete-history flag, subscribe channel, server time, positions, orders, collision logins, and **copies** of lists.

---

## 12. Risks if this surface is mistaken for “MT5 done”

1. **Dashboard green ≠ Manager up.** `connected: true` is hardcoded. A release that stops at Fake+seed will look live.
2. **Worker 30-day window vs June 2026 fixture** hides the fact that the live poll path returns empty. People will “debug ingestion” when the clock is the bug.
3. **Promoting `Mt5BrokerOptions`** ships `Mode=remote`, `PoolSize=25`, required `RemoteUrl`, invented `ApiKey` — contradicts §56 and A58.
4. **Implementing `IBrokerConnector`** leaks EF entities into the adapter and keeps two ports forever.
5. **Copying C++ `MT5HttpClient` dealer verbs** onto the C# port (`Withdraw`, `SendTrade`) — Fake currently avoids this; do not “complete the interface” from `imt5_client.h`.
6. **Trusting HTTP JSON deals** without `position` — reconstruction and §10 identity break.
7. **Treating empty `GetPositions` / `GetOrders` as authoritative** when the method is unsupported (remote) or unseeded (Fake).
8. **Using `Subscribe` as the only deal path** if/when SSE is added — there is still no `PUMP_MODE_DEALS`.
9. **Uppercase `ACHIEVER` vs A58 `achiever`.** Persistence `brokers.code` is already uppercase via seeder. Pick one normalize rule (`ToLowerInvariant`) before a second writer appears.
10. **Hard-coded worker logins** (`10001…99001`) will miss every real Achiever login (~5,000). The Fake census is not a census.
11. **No checkpoint** + always-success `GetDeals` = silent truncation if a real adapter is dropped in behind the same service.
12. **Two `CreateDefault()` graphs** (DI vs seeder) — `AddDeal` on the singleton does not change seeded history.

---

## 13. What “done” looks like for *this* gap (not implemented here)

B04 is closed only when **all** of the following are measured on disk:

1. **Single Application port** (`IMt5BrokerConnector` as A58, or a recorded adjustment). `IBrokerConnector` deleted. DTOs carry `BrokerCode` + `CatalogId`. Persistence entities stay in Domain.
2. **Fake stays a Fake** — implements the full collector port, returns **copies**, can fail closed, can emit events, has positions/orders/server-time hooks, has a colliding-login fixture. Used by tests, **not** by production DI when §56 slots are configured.
3. **Registry** is `IMt5BrokerRegistry` with `TryGet` / `All` / `Snapshot`. Worker jobs loop `All`. No `if (login >= 99000)`.
4. **One real adapter class** (Manager local and/or HTTP — resolve §8 without inventing §56 keys) constructed by a factory from `Mt5BrokerConnectionOptions`. Two instances: Achiever + StarwaveFX.
5. **HTTP files exist only if remote is an authorized transport**, and then they are exactly A16 paths + A30 collector refusals: `Mt5CollectorClient`, JSON map that **does not drop `position`**, SSE reader, timeouts, `X-API-Key` never logged. Dealer routes **not** wrapped.
6. **`Mt5BrokerOptions` replaced** (no `RemoteUrl` required, no pool 25, no default remote). `DeterministicGuid` used for catalog ids.
7. **Ingestion** uses checkpoints + fail-closed deals + positions reconcile. Dashboard `Connected` comes from `IsConnected` / health, not `true`.
8. **Tests** listed in §11 are green. Unit project references Mt5.

Until then this gap stays **open**. Do not claim `TraderIntelligence.Mt5` “wraps the SDK” or “has an HTTP adapter”. It has a **demo Fake and a dictionary**.

---

## 14. Cross-links

| Need | Where |
|---|---|
| C++ `IMT5Client` map / OUT verbs | A04, A12 |
| REST/SSE literals + timeouts | A16 |
| Planned C# `src/Mt5/Http/*` tree | A30 I2 |
| Registry + §56 allow-list | A58, A75, architecture §56 |
| Group discovery / no plan filter | A39, A40, §9 |
| Volume scale 10_000 vs “hundredths” | A81 (`DemoBrokerFactory.VolumeScale` is the good one) |
| Checkpoints / live+backfill+reconcile | A59, architecture §12 |
| Worker job list | A07, A64 |
| Secrets / no password in catalog | A19, architecture §55 |
| Older “Class1 empty” snapshot | A04, A29 S01 — **stale**; use this file |

---

## 15. One-page status for the swarm index

```text
FakeMt5BrokerConnector     EXISTS (demo only; 18 deals; 0 positions; no events)
BrokerRegistry             EXISTS (string dict in Fake file; no health; no §56 binder)
IBrokerConnector           DEAD (delete)
Mt5BrokerOptions           DEAD / UNSAFE defaults (do not bind)
DeterministicGuid          EXISTS unused (keep)
HTTP adapter               MISSING (0 files, 0 HttpClient)
Manager adapter            MISSING
Phase 1 "brokers connected" FAIL (Fake bool + dashboard hard-coded true)
Product source             not modified
```
