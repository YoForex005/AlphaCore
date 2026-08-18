# D04 — C# `src/Mt5` census (`TraderIntelligence.Mt5`)

| Field | Value |
|---|---|
| Agent | D04 (senior engineer, inventory only) |
| Date | 2026-08-18 |
| Assigned | Inventory `D:\Prop\src\Mt5`. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D04_mt5_census.md` |
| Product source modified | **No.** This report is the only write. |
| Tree | `D:\Prop\src\Mt5` |
| Measured | 2026-08-18 (file UTC stamps 07:24–07:43; hashes below) |
| Precedence | Supersedes A04 / A07 **file inventory** (`Class1.cs`, “0 types”). Complements, does not replace, B04 (gap), B24 (port dup), C10 (plan-filter), C42 (no live MT5). |

---

## 0. Verdict (measured, not aspirational)

`TraderIntelligence.Mt5` is a **net8.0 class library** with **4 production `.cs` files + 1 csproj**. `Class1.cs` is **gone**. There is **no** HTTP adapter, **no** Manager P/Invoke, **no** native package, **no** `Http/` / `Registry/` / `DependencyInjection.cs` under this folder.

What ships:

| Surface | Classification | Evidence |
|---|---|---|
| `FakeMt5BrokerConnector` | **EXISTS — demo only.** Sole `IMt5BrokerConnector` implementor. | `Connectors\FakeMt5BrokerConnector.cs` |
| `BrokerRegistry` + `DemoBrokerFactory` | **EXISTS** in the **same file** as the Fake. | lines 70–170 |
| `IBrokerConnector` + `Mt5BrokerEvent` | **DEAD.** Zero implementors, zero consumers. | `Connectors\IBrokerConnector.cs` |
| `Mt5BrokerOptions` | **DEAD / UNSAFE-shape.** Never bound. Defaults `Mode=remote`, required `RemoteUrl`, invents `ApiKey`. | `Configuration\Mt5BrokerOptions.cs` |
| `DeterministicGuid` | **EXISTS unused.** SHA-256 → Guid. Zero call sites. | `Utils\DeterministicGuid.cs` |
| Real MT5 transport | **ABSENT** | 0 `HttpClient`, 0 `DllImport`, 0 `/mt5/*` literals in `D:\Prop\src\**\*.cs` |
| Live Achiever / StarwaveFX | **NOT PROVEN** | C42; Fake `ConnectAsync` flips a bool |

Honest one-liner: **C# `src/Mt5` is a canned in-memory fixture (2 broker codes, 4 groups, 4 logins, 18 XAUUSD deals, 0 positions). It cannot talk to MT5.**

---

## 1. Method

1. Recurse `D:\Prop\src\Mt5` (production + `bin/` + `obj/`).
2. SHA-256 + exact line count of every production file (exclude `bin/`, `obj/`).
3. Read every production `.cs` and the csproj in full.
4. Grep product C# for implementors, consumers, `HttpClient`, `DllImport`, `PInvoke`, `NativeLibrary`, `X-API-Key`, `/mt5/`.
5. Trace wiring: Application contracts, `DealIngestionService`, Infrastructure DI + `DemoSeeder`, `apps/mt5-worker`, dashboard, tests.
6. Count the canned fixture (groups / accounts / deals / tickets / volumes).
7. Compare on-disk tree to A30 planned `src/Mt5/Http/*` and A58 collector port.
8. **Did not** edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

---

## 2. On-disk tree

### 2.1 Production (hand-authored)

```text
D:\Prop\src\Mt5\
  TraderIntelligence.Mt5.csproj
  Configuration\
    Mt5BrokerOptions.cs
  Connectors\
    FakeMt5BrokerConnector.cs     # Fake + BrokerRegistry + DemoBrokerFactory
    IBrokerConnector.cs           # unused dual port + Mt5BrokerEvent
  Utils\
    DeterministicGuid.cs
```

**No** `Http\`, `Registry\`, `Native\`, `DependencyInjection.cs`, `README`, `Class1.cs`.

Top-level directories: `bin`, `Configuration`, `Connectors`, `obj`, `Utils`.

### 2.2 Production file metrics

| Path | Lines | Bytes | SHA-256 | LastWriteTimeUtc |
|---|---:|---:|---|---|
| `TraderIntelligence.Mt5.csproj` | 14 | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | 2026-08-18T07:24:51.466Z |
| `Configuration\Mt5BrokerOptions.cs` | 51 | 1609 | `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7` | 2026-08-18T07:38:04.054Z |
| `Connectors\IBrokerConnector.cs` | 45 | 1557 | `6B7AA65F293AF43A548D09BC92332286A5123EDB03DCCD498C2217490CCBC5BC` | 2026-08-18T07:38:18.725Z |
| `Connectors\FakeMt5BrokerConnector.cs` | 170 | 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 2026-08-18T07:43:42.841Z |
| `Utils\DeterministicGuid.cs` | 22 | 709 | `A1F44B7EE85DDA7C4A73C81DDAB3D5339D778C8FB20ECCD3D46BE64BC4B72A6D` | 2026-08-18T07:38:36.012Z |
| **Total production** | **302** | **11343** | | |

Hashes for Fake / `IBrokerConnector` / `Mt5BrokerOptions` match B24 / C10 / C42. The tree did not grow a live connector between those reviews and this census.

### 2.3 Build artifacts (not product source; inventory only)

| Output | Size | Notes |
|---|---:|---|
| `bin\Debug\net8.0\TraderIntelligence.Mt5.dll` | 22016 | Debug |
| `bin\Release\net8.0\TraderIntelligence.Mt5.dll` | 21504 | Release |
| `bin\*\TraderIntelligence.Mt5.deps.json` | 1977 | Project refs Domain + Application; transitive FluentValidation 11.9.2 |
| `bin\*\TraderIntelligence.{Application,Domain}.{dll,pdb}` | — | copied project refs |
| `obj\Release\net8.0\TraderIntelligence.Mt5.AssemblyInfo.cs` | — | `1.0.0.0`; InformationalVersion `1.0.0+398a14200ec65714c4077eed55c46808382ca1e3` |
| `obj\Release\net8.0\TraderIntelligence.Mt5.GeneratedMSBuildEditorConfig.editorconfig` | — | `TargetFramework=net8.0`; `RootNamespace=TraderIntelligence.Mt5`; platforms Linux,macOS,Windows |

`bin/` / `obj/` are generated. Do not treat them as adapter source.

---

## 3. Project, solution, dependencies

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

| Property | Measured |
|---|---|
| SDK | `Microsoft.NET.Sdk` (class library, not Worker/Web) |
| TFM | `net8.0` (portable; no `RuntimeIdentifier`, no `win-x64`) |
| PackageReference | **none** (FluentValidation arrives only via Application) |
| `AllowUnsafeBlocks` | **absent** |
| Native / content items | **absent** |
| `Microsoft.Extensions.Http` / `System.Net.Http.Json` | **absent** (A30 I2 asked for them; not added) |
| Implicit usings | enabled (generated `GlobalUsings.g.cs` includes `System.Net.Http` — **unused**) |
| Nullable | enable |
| Directory.Build.props | repo-wide `LangVersion=latest`, `Deterministic=true`, `TreatWarningsAsErrors=false` |
| Solution | `Mt5TraderIntelligence.sln` project `{CCD4D49A-9F3E-4795-AA56-CFBF87526E94}` |

**Direct project consumers of this csproj:**

| Project | Reference |
|---|---|
| `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | yes (DI + seeder) |
| `apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | yes (also via Infrastructure) |
| `tests\Integration\TraderIntelligence.Tests.Integration.csproj` | yes |
| `tests\Unit\TraderIntelligence.Tests.Unit.csproj` | **no** |
| `apps\api\TraderIntelligence.Api.csproj` | no (transitive via Infrastructure) |
| `src\Application` | **no** (correct: Application owns the port; Mt5 implements it) |

Layering (measured):

```text
Domain  ←  Application (IMt5BrokerConnector + DTOs)
                ↑
              Mt5 (Fake implements Application port;
                   Domain ref is only needed by dead IBrokerConnector)
                ↑
         Infrastructure (registers Fake)
                ↑
         apps/mt5-worker, apps/api
```

C++ `D:\Prop\mt5-sdk` is a **separate nested tree** (C20: preserved, not wired). This project does not compile, P/Invoke, or `#include` it.

---

## 4. Public type census (7 types)

| Type | Namespace | Kind | File | Role |
|---|---|---|---|---|
| `FakeMt5BrokerConnector` | `TraderIntelligence.Mt5.Connectors` | sealed class | Fake file | Sole `IMt5BrokerConnector` |
| `BrokerRegistry` | same | sealed class | Fake file | Sole `IBrokerRegistry` |
| `DemoBrokerFactory` | same | static class | Fake file | Two canned instances |
| `IBrokerConnector` | same | interface | `IBrokerConnector.cs` | **DEAD** dual port |
| `Mt5BrokerEvent` | same | sealed record | `IBrokerConnector.cs` | **DEAD** event DTO |
| `Mt5BrokerOptions` | `TraderIntelligence.Mt5.Configuration` | sealed class | options file | **DEAD** options sketch |
| `DeterministicGuid` | `TraderIntelligence.Mt5.Utils` | static class | utils file | **UNUSED** helper |

There is **no** type in root namespace `TraderIntelligence.Mt5`. `Class1` is gone.

No enums, no exceptions (`Mt5BrokerUnavailableException` etc. do not exist), no `IOptions` binder, no factory interface.

---

## 5. Dual collector ports (only one is live)

### 5.1 Live Application port (not in this folder)

`D:\Prop\src\Application\Contracts\Mt5Contracts.cs` — SHA-256 `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132`.

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

DTOs in the same file (not owned by `src/Mt5`):

| Record | Fields |
|---|---|
| `Mt5GroupDto` | Name, Currency, CurrencyDigits, Company, MarginCall, MarginStopOut, ConnectionsAllowed |
| `Mt5AccountDto` | Login (`long`), GroupName, Leverage, Balance, Equity, Margin, MarginFree, Profit |
| `Mt5DealDto` | DealTicket, Login, OrderTicket, PositionId, Symbol, Action, Entry, VolumeNative (`ulong`), Price, Profit, Commission, Swap, Time, Comment |
| `Mt5PositionDto` | PositionTicket, Login, Symbol, Direction, VolumeNative, PriceOpen/Current/Sl/Tp, Profit, TimeCreate — **no Swap, no TimeUpdate, no Comment** |

Missing vs architecture §6 / A58 (census of **absences** on the used port): `CatalogId`, `DisplayName`, `LastError`, `PumpEventsAvailable`, `GetGroupLogins`, `GetUser`, `GetAccount` (separate), `GetOrders`, `GetSymbols`, `GetTickLast`, `GetServerTime`, `Subscribe`, ticks, fail-closed deals, `TryGet` on registry.

Login/ticket types are `long`, not SDK `uint64_t`. Volume is native `ulong` (correct family). Deal carries `PositionId` (better than C++ HTTP JSON, which drops `position`).

### 5.2 Dead adapter-layer port (in this folder)

```5:44:D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs
public interface IBrokerConnector
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    Task<IReadOnlyList<Mt5Group>> GetGroupsAsync(...);
    Task<IReadOnlyList<Mt5Account>> GetAccountsAsync(Mt5Group group, ...);
    Task<IReadOnlyList<Mt5Deal>> GetDealsAsync(ulong login, ...);
    Task<IReadOnlyList<Mt5Position>> GetPositionsAsync(ulong login, ...);
    Task<DateTimeOffset> GetServerTimeAsync(...);
    IAsyncEnumerable<Mt5BrokerEvent> SubscribeEventsAsync(...);
}
```

Returns **Domain EF entities**. Adds server-time + a deal/position-only event record. Grep `: IBrokerConnector` / `Mt5BrokerEvent` under `D:\Prop\src`, `apps`, `tests` = **this file only**. B24: **delete; do not implement.** Harvest later: `GetServerTimeAsync`, `SubscribeAsync` — copy signatures onto Application first.

---

## 6. `FakeMt5BrokerConnector` — measured behaviour

```6:68:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
public sealed class FakeMt5BrokerConnector : IMt5BrokerConnector
{
    private readonly List<Mt5GroupDto> _groups;
    private readonly List<Mt5AccountDto> _accounts;
    private readonly List<Mt5DealDto> _deals;
    private readonly List<Mt5PositionDto> _positions;
    private bool _connected;
    ...
    public Task ConnectAsync(CancellationToken ct) { _connected = true; return Task.CompletedTask; }
    public Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Mt5GroupDto>>(_groups);
    public void AddDeal(Mt5DealDto deal) => _deals.Add(deal);
}
```

| Member | Measured |
|---|---|
| `BrokerCode` | constructor string; default fixture uses `"ACHIEVER"` / `"STARWAVEFX"` |
| `ConnectAsync` | sets `_connected = true`; ignores `ct`; no I/O |
| `DisconnectAsync` | sets `_connected = false`; ignores `ct` |
| `IsConnectedAsync` | returns `_connected`; starts **false** |
| `GetGroupsAsync` | returns **live** `_groups` (caller can mutate). **No** plan-map filter (C10 **PASS**) |
| `GetAccountsAsync(group)` | null/whitespace → live `_accounts`; else **copy** filtered by exact `GroupName` |
| `GetDealsAsync` | **copy** of deals with `Login` match and inclusive `[from, to]` on `Time` |
| `GetPositionsAsync` | **copy** filtered by login |
| `AddDeal` | append; no ticket uniqueness; unused in product |
| Cancellation | ignored on every method |
| Thread safety | raw `List<T>` — none |
| Incomplete history | cannot express; always success |
| Orders / subscribe / server time / ticks | absent |
| Disconnected queries | still return seed (A58/A79 want throw) |

`GetGroupsAsync` is not plan-filtered. That is the **correct** §7/§9 shape for a fake. It is **not** a complete Manager-visible catalog (only 4 hard-coded names).

---

## 7. `BrokerRegistry` + `DemoBrokerFactory`

### 7.1 Registry (same file, lines 70–87)

- `Dictionary<string, IMt5BrokerConnector>` keyed by `BrokerCode`, `StringComparer.OrdinalIgnoreCase`.
- `Get` throws `KeyNotFoundException` on miss (fail-closed lookup).
- `All()` copies values.
- Duplicate codes at construction throw (`ToDictionary`).
- No `TryGet`, no health `Snapshot`, no §56 slot binder.
- Lives next to the test double (A58 wanted `src/Mt5/Registry/`).

### 7.2 Factory constants

```91:93:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public const decimal VolumeScale = 10_000m;
    public static ulong Lots(decimal lots) => (ulong)decimal.Round(lots * VolumeScale, 0, MidpointRounding.AwayFromZero);
```

Matches `VolumeConverter.ManagerVolumeScale` and official `MTAPI_VOLUME_DIV`. **Does not** repeat the `mt5_types.h` “hundredths” comment bug (A81). `Lots(0.10) = 1000`, `Lots(0.05) = 500`, `Lots(0.20) = 2000`, `Lots(0.40) = 4000`.

Clock origin: `2026-06-01T08:00:00+00:00`. Close = open + 45 minutes. StarwaveFX deals start `t0 + 1 day`.

---

## 8. Default fixture census (`CreateDefault`)

| Broker | Groups | Accounts | Deals | Positions | Symbols |
|---|---:|---:|---:|---:|---|
| `ACHIEVER` | 3 | 3 | 12 | 0 | `XAUUSD` only |
| `STARWAVEFX` | 1 | 1 | 6 | 0 | `XAUUSD` only |
| **Total** | **4** | **4** | **18** | **0** | 1 |

Codes match `Domain.Brokers.BrokerCodes` (**uppercase**), not A58 lowercase `achiever` / `starwavefx`. Registry lookup is case-insensitive, so both resolve.

### 8.1 Groups

| Broker | Name | Currency | Digits | Company | MarginCall | StopOut | ConnectionsAllowed |
|---|---|---|---:|---|---:|---:|---|
| ACHIEVER | `demo\Maxmaster` | USD | 2 | Achiever | 100 | 50 | true |
| ACHIEVER | `demo\yo-2step` | USD | 2 | Achiever | 100 | 50 | true |
| ACHIEVER | `contest\yo-2step` | USD | 2 | Achiever | 100 | 50 | true |
| STARWAVEFX | `real\standard` | USD | 2 | StarwaveFX | 80 | 50 | true |

`demo\Maxmaster` is present and **not exclusive** (good vs §7). Catalog is **not** A79’s required unmapped extras (`demo\standard`, `real\vip`, Starwave `demo\default`, …).

### 8.2 Accounts

| Login | Broker | Group | Leverage | Balance | Equity | Margin | MarginFree | Profit | Deals |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|
| 10001 | ACHIEVER | `demo\Maxmaster` | 100 | 10_000 | 10_240 | 200 | 9_800 | +240 | 6 (3 round-trips) |
| 10002 | ACHIEVER | `demo\yo-2step` | 100 | 5_000 | 4_820 | 150 | 4_670 | −180 | 6 (martingale 0.10→0.20→0.40) |
| 10003 | ACHIEVER | `contest\yo-2step` | 200 | 25_000 | 25_000 | 0 | 25_000 | 0 | **0** (empty-success login) |
| 99001 | STARWAVEFX | `real\standard` | 100 | 8_000 | 8_110 | 80 | 7_920 | +110 | 6 |

No `GetUser` fields (name/email/rights/reg). No credit / floating / storage / margin_level.

### 8.3 Deals (exact)

Helper: `ticket_in = 10_000 + seq`, `ticket_out = 10_500 + seq`, `order_in = 20_000 + seq`, `order_out = 20_500 + seq`. IN profit = 0, IN swap = 0, IN comment `"open"`. OUT comment `"close"`. Commission split half/half.

`t0 = 2026-06-01T08:00:00Z`.

| seq | Broker | Login | Pos | Side | Open (UTC) | Close (UTC) | Lots | Vol native | Entry | Exit | OUT profit | Comm total | OUT swap | Tickets IN/OUT |
|---:|---|---:|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| 1 | ACHIEVER | 10001 | 501 | long | 08:00 | 08:45 | 0.10 | 1000 | 2320.10 | 2335.40 | +153 | −1.2 | −0.4 | 10001 / 10501 |
| 2 | ACHIEVER | 10001 | 502 | **short** | 11:00 | 11:45 | 0.10 | 1000 | 2338.00 | 2329.20 | −88 | −1.1 | −0.3 | 10002 / 10502 |
| 3 | ACHIEVER | 10001 | 503 | long | 14:00 | 14:45 | 0.10 | 1000 | 2325.50 | 2341.80 | +163 | −1.2 | −0.2 | 10003 / 10503 |
| 11 | ACHIEVER | 10002 | 601 | long | 08:00 | 08:45 | 0.10 | 1000 | 2320 | 2300 | −200 | −1 | 0 | 10011 / 10511 |
| 12 | ACHIEVER | 10002 | 602 | long | 10:00 | 10:45 | 0.20 | 2000 | 2300 | 2275 | −500 | −2 | 0 | 10012 / 10512 |
| 13 | ACHIEVER | 10002 | 603 | long | 12:00 | 12:45 | 0.40 | 4000 | 2275 | 2240 | −1400 | −4 | 0 | 10013 / 10513 |
| 21 | STARWAVEFX | 99001 | 701 | long | 2026-06-02 08:00 | 08:45 | 0.05 | 500 | 2340 | 2348 | +40 | −0.6 | 0 | 10021 / 10521 |
| 22 | STARWAVEFX | 99001 | 702 | long | 10:00 | 10:45 | 0.05 | 500 | 2348 | 2356 | +40 | −0.6 | 0 | 10022 / 10522 |
| 23 | STARWAVEFX | 99001 | 703 | long | 12:00 | 12:45 | 0.05 | 500 | 2356 | 2362 | +30 | −0.6 | 0 | 10023 / 10523 |

All `DealAction.Buy/Sell` + `DealEntry.In/Out`. **No** balance / credit / commission / dividend / canceled deals. Comments are literals `"open"` / `"close"`, not broker text.

### 8.4 Positions / orders / events / symbols / ticks

| Book | Default fixture |
|---|---|
| Positions | **empty** (`CreateDefault` never passes `positions:`) |
| Orders | type does not exist on the used port |
| Events | no subscribe on the used port |
| Symbols | implicit `XAUUSD` on deals only; no `GetSymbols` |
| Ticks | none |

---

## 9. Dead / unused types in this folder

### 9.1 `Mt5BrokerOptions` (51 lines)

Properties: `BrokerId`, `DisplayName`, `Server`, `Port`, `Login` (`ulong`), `Password`, `ServerName`, `Mode` (default **`"remote"`**), `PoolSize` (default **25**), proxy block, `RemoteUrl` (`[Required]`), `ApiKey`, `EgressIp`.

Nothing binds `IOptions<Mt5BrokerOptions>`. Worker `appsettings.json` is logging-only.

Conflicts if this type were ever bound as-is:

| Field | This type | Architecture / seeder |
|---|---|---|
| `Mode` default | `remote` | §56 / seeder `local` |
| `PoolSize` default | 25 | Achiever **8** / StarwaveFX **4** |
| `RemoteUrl` | required | §56 has **no** `MT5_REMOTE_URL` |
| `ApiKey` | invented | §56 has **no** `MT5_API_KEY` |
| Passwords on POCO | present | never log; today unused so not leaking |

**Do not promote this type.**

### 9.2 `DeterministicGuid.FromString`

SHA-256 of UTF-8 input, first 16 bytes → `Guid`. Intended for stable `brokers.id` (`broker:achiever`). **Zero call sites.** Seeder uses hand-picked `aaaaaaaa-…aaa1` / `aaa2` instead.

---

## 10. Consumers (outside this folder)

| Consumer | How it uses `src/Mt5` |
|---|---|
| `Infrastructure\DependencyInjection.cs` | `DemoBrokerFactory.CreateDefault()` → two `AddSingleton<IMt5BrokerConnector>` + `BrokerRegistry` |
| `Infrastructure\Seeding\DemoSeeder.cs` | **Second** `CreateDefault()` + **new** `BrokerRegistry`; ingest window `2026-01-01` … `2026-12-31` |
| `Application\Ingestion\DealIngestionService.cs` | `registry.Get(code)` → Connect → groups → `GetAccounts(null)` → per-login deals + positions. Does **not** iterate `All()` |
| `apps\mt5-worker\Worker.cs` | every 30 s: `SyncBrokerAsync(ACHIEVER)` then `STARWAVEFX` with `from = UtcNow-30d`, `to = UtcNow+1m`; then scores hard-coded logins `10001,10002,10003,99001` (`login >= 99000` broker switch) |
| `apps\mt5-worker\Program.cs` | `AddTraderIntelligence` + `EnsureCreated` + `DemoSeeder` |
| `EfDashboardQueries.GetBrokersAsync` | **does not** call the connector; `Connected = true` literal |
| `EfTradingStore` | maps `Mt5*Dto` → Domain entities; stamps `BrokerId` at persist |

Two independent `CreateDefault()` graphs (DI singleton vs seeder). `AddDeal` on the DI instance cannot change already-persisted seed rows.

**Worker window vs fixture (2026-08-18):** `from ≈ 2026-07-19`. Demo deals live in **2026-06-01/02** → **outside the live poll window**. After seed, the 30 s loop’s `GetDealsAsync` returns **empty**. Positions stay empty. Measured Fake+clock mismatch, not a live feed.

---

## 11. Tests

| Project | Refs `TraderIntelligence.Mt5`? | Facts that name Fake / registry / options |
|---|---|---|
| `tests\Unit` | **No** | **0.** `VolumeConverterTests` covers Domain scale 10_000, **not** `DemoBrokerFactory.Lots` |
| `tests\Integration` | **Yes** | `SeedingAndStoreTests` calls `DemoSeeder` (indirect Fake). Asserts 2 brokers, groups > 2, deals > 0, scores for 10001/10002. **No** isolated Fake / registry / window / encapsulation tests. `UnitTest1.cs` leftover. |

A58/A79 unit list (`InMemoryMt5BrokerConnectorTests`, 5k census, dual-broker isolation, fail-closed deals) is **unwritten**. The Fake is **not** the A79 `InMemoryMt5BrokerConnector` (that type is specified to live under `tests/` and is **MISSING**).

B04’s claim “Integration csproj does not reference Mt5” is **stale**. Integration **does** reference it as of this census.

---

## 12. Absences vs planned tree (A30 I2 / A58)

| Planned path | Status |
|---|---|
| `src/Mt5/Http/Mt5CollectorClient.cs` | **MISSING** |
| `src/Mt5/Http/Mt5CollectorOptions.cs` | **MISSING** |
| `src/Mt5/Http/Mt5Json.cs` | **MISSING** |
| `src/Mt5/Http/Mt5SseReader.cs` | **MISSING** |
| `src/Mt5/Registry/Mt5BrokerRegistry.cs` | **MISSING** (logic stuffed in Fake file) |
| `src/Mt5/Registry/BrokerOptions.cs` | **MISSING** |
| `src/Mt5/DependencyInjection.cs` | **MISSING** |
| `src/Mt5/Connectors/Mt5ManagerBrokerConnector.cs` | **MISSING** |
| `src/Mt5/Configuration/Mt5BrokerSlotBinder.cs` | **MISSING** |
| `InMemoryMt5BrokerConnector` under `tests/` (A79) | **MISSING** |

Grep `D:\Prop\src\**\*.cs`: `HttpClient` / `IHttpClientFactory` / `DllImport` / `PInvoke` / `NativeLibrary` / `X-API-Key` / `/mt5/` = **0**.

Dealer / provisioning verbs (`CreateUser`, `Withdraw`, `SendTrade`, …) are **correctly absent** from both ports and the Fake. Keep them off.

---

## 13. Coverage vs architecture §6 (used port)

| §6 method | C# now |
|---|---|
| `ConnectAsync` / `DisconnectAsync` | **FAKE** bool |
| `GetGroupsAsync` | **FAKE** 4 canned groups; not plan-filtered |
| `GetAccountsAsync` | **FAKE** 4 canned accounts |
| `GetDealsAsync` | **FAKE** 18 canned deals; no paging; no fail-closed |
| `GetOrdersAsync` | **ABSENT** |
| `GetPositionsAsync` | **FAKE** always empty in default fixture |
| `SubscribeAsync` | **ABSENT** on used port; **DEAD** cousin on `IBrokerConnector` |

Real I/O for §6: **0 / 7**.

---

## 14. Numeric snapshot

| Metric | Count |
|---|---:|
| Production `.cs` files | 4 |
| Production `.csproj` | 1 |
| Production C# + csproj lines | 302 |
| Public types | 7 |
| `IMt5BrokerConnector` implementors | 1 (Fake) |
| `IBrokerConnector` implementors | 0 |
| HTTP adapter files | 0 |
| Manager / P/Invoke files | 0 |
| PackageReference on this csproj | 0 |
| Canned brokers / groups / accounts / deals / positions | 2 / 4 / 4 / 18 / 0 |
| Unit tests targeting this project | 0 |
| Integration facts that construct Fake directly | 0 (seeder only) |
| `HttpClient` in `src/` | 0 |
| Live Manager sessions proven | 0 |

---

## 15. Stale sibling inventories (do not quote as current)

| Report | What it said | Now |
|---|---|---|
| A04, A07 | `Class1.cs`; `IMt5BrokerConnector` MISSING; worker 1 s log loop | Class1 **gone**; port **exists** in Application; worker **does** poll fakes |
| A09 / B04 tests note | Integration does not ref Mt5; 0 test classes | Integration **refs Mt5**; `SeedingAndStoreTests` exists |
| A04 coverage 0/41 | C# implements 0 of `IMT5Client` | Still **0 real I/O**; Fake covers a **narrow DTO subset** only |

Use **this file** for the `src/Mt5` file/type/fixture census. Use **B04** for the gap narrative. Use **C42** for the live-connect honesty pin.

---

## 16. Risks if this census is misread

1. Green `dotnet build` of `TraderIntelligence.Mt5` ≠ Manager up.
2. Dashboard `Connected = true` is hardcoded (C42).
3. Worker “syncs ACHIEVER/STARWAVEFX every 30 s” is a **fake poll**; June deals miss the 30-day window.
4. Promoting `Mt5BrokerOptions` ships `Mode=remote`, pool 25, required `RemoteUrl`.
5. Implementing `IBrokerConnector` freezes the wrong port (B24).
6. Treating 4 logins / 18 deals as a 5,000-account census (A79 / §69.3 still FAIL).
7. Copying C++ dealer verbs onto the collector “to match `IMT5Client`”.
8. Two `CreateDefault()` graphs (DI vs seeder) look like one book of truth.

---

## 17. Cross-links

| Need | Where |
|---|---|
| C++ `IMT5Client` / OUT verbs | A04 (stale C#), A12 |
| REST/SSE literals | A16 |
| Planned `src/Mt5/Http/*` | A30 I2 |
| Registry + §56 | A58, A75 |
| Group discovery not plan-filtered | A39, A40, **C10 PASS on Fake** |
| Volume 10_000 | A81, `VolumeConverter`, `DemoBrokerFactory.VolumeScale` |
| Fake-as-harness spec | A79 (type still MISSING under `tests/`) |
| Port duplication | **B24** — keep Application port; delete `IBrokerConnector` |
| Gap narrative | **B04** |
| Live connect honesty | **C42**, A100 G01 FAIL, A105 (no Manager DLL load) |
| SDK preserved, not wired | **C20** |
| Worker jobs | A07 (stale template), B07 / C07, `apps/mt5-worker/Worker.cs` |

---

## 18. One-page status

```text
Project                      TraderIntelligence.Mt5  net8.0  sln {CCD4D49A-…}
Production files             4 .cs + csproj   (Class1 GONE)
FakeMt5BrokerConnector       EXISTS (demo; 18 deals; 0 positions; no events)
BrokerRegistry               EXISTS (string dict in Fake file)
DemoBrokerFactory            EXISTS (ACHIEVER + STARWAVEFX)
IBrokerConnector             DEAD (delete; 0 implementors)
Mt5BrokerOptions             DEAD / UNSAFE defaults (do not bind)
DeterministicGuid            EXISTS unused (keep)
HTTP / Manager adapters      MISSING
Unit tests of this project   0
Live MT5                     NOT PROVEN
Product source               not modified
```
