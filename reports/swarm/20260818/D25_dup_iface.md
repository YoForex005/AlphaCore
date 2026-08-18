# D25 — Duplicate collector ports: `IBrokerConnector` vs `IMt5BrokerConnector`

| Field | Value |
|---|---|
| Agent | D25 (duplicate-interface recensus) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\D25_dup_iface.md` |
| Assigned | Compare `IBrokerConnector` vs `IMt5BrokerConnector`. Write this file. |
| Product source modified | **No.** This report is the only write under the assigned path. |
| Test source modified | **No.** |
| Workspace | `D:\Prop` |
| Precedence | Recensus of on-disk C# as of 2026-08-18. **Confirms B24** (hashes unchanged). Supersedes A55/A57/A59/A90 on “which connector is live.” Does **not** implement A58’s larger port. A58 remains the *target* shape. |

Classification (architecture §73.B): `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**Keep Application `IMt5BrokerConnector`. Delete Mt5 `IBrokerConnector` (+ `Mt5BrokerEvent`) without implementing it.**

They are not two viable alternatives. They are a **live Application port** and an **orphaned Mt5 draft** written ~93 seconds earlier. Keeping both is a third surface waiting to happen (A90: “Do not invent a third connector”).

| Surface | Path | Layer | Classification | Implementors | Product consumers |
|---|---|---|---|---:|---:|
| **`IMt5BrokerConnector` + `IBrokerRegistry` + `Mt5*Dto`** | `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Application port | **EXISTS_NEEDS_REFACTOR** vs A58 / §6 (no orders, no subscribe, no server time) | **1** (`FakeMt5BrokerConnector`) | **4** (DI, seeder, ingestion via registry, worker via ingestion) |
| **`IBrokerConnector` + `Mt5BrokerEvent`** | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | Adapter draft | **DEPRECATED** | **0** | **0** |

**Do not** dual-implement. **Do not** make `FakeMt5BrokerConnector` implement both. **Do not** rename `IMt5BrokerConnector` to `IBrokerConnector`. Architecture §6 names the port `IMt5BrokerConnector`. A58 §5 / A58 §13 / A79 §0 already say **delete** the Mt5 file.

Honest caveat: the **winner is incomplete** vs architecture §6 and vs A58. It is still the only port that is wired, DTO-shaped, and named correctly. Harvest two *ideas* from the loser later (`GetServerTimeAsync`, `SubscribeAsync`). Do not keep the loser “until harvest” — copy signatures onto Application first, then delete the file.

B24 measured the same two SHA-256 values. This recensus found **no drift**.

---

## 1. Method

Read-only. No edits under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`.

| Source | What was measured |
|---|---|
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | 69 physical / 62 non-blank, 1858 B, SHA-256 `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132`, written 2026-08-18 13:09:51 |
| `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | 45 physical / 36 non-blank, 1557 B, SHA-256 `6B7AA65F293AF43A548D09BC92332286A5123EDB03DCCD498C2217490CCBC5BC`, written 2026-08-18 13:08:18 |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 170 physical, 7049 B, SHA-256 `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` — implements **`IMt5BrokerConnector` only** |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4277 B, SHA-256 `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` — `IBrokerRegistry` → `IMt5BrokerConnector` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | two `IMt5BrokerConnector` singletons + `IBrokerRegistry` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | `new BrokerRegistry(new IMt5BrokerConnector[] { … })` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | maps `Mt5*Dto` → Domain entities; never sees `IBrokerConnector` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | `DealIngestionService.SyncBrokerAsync` only (no connector type) |
| Domain entities | `Mt5Group` / `Mt5Account` / `Mt5Deal` / `Mt5Position` |
| Project refs | Application → Domain only; Mt5 → Domain + Application |
| Architecture §6 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 338–355 |
| Prior swarm | A04, A55, A57, A58, A59, A79, A90, B02, B04, B24, C10, C35, C54 |

Product C# grep (`D:\Prop\src` + `apps` + `tests`, exclude `bin/` `obj/`):

| Pattern | Files | Lines | Where |
|---|---:|---:|---|
| `IBrokerConnector` | **1** | **1** | declaration only |
| `: IBrokerConnector` | **0** | **0** | no implementor |
| `Mt5BrokerEvent` | **1** | **2** | same file (record + `SubscribeEventsAsync` return) |
| `SubscribeEventsAsync` | **1** | **1** | same file |
| `GetServerTimeAsync` | **1** | **1** | same file |
| `IMt5BrokerConnector` | **4** | **12** | contracts, Fake, DI, seeder |
| `IBrokerRegistry` | **4** | **5** | contracts, Fake/`BrokerRegistry`, DI, `DealIngestionService` |

`tests/Unit` and `tests/Integration`: **zero** hits for either interface name.

---

## 2. The two types, quoted

### 2.1 Application port (live) — `TraderIntelligence.Application.Contracts`

Lives next to the DTOs ingestion already persists. Paired with `IBrokerRegistry`.

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

Eight members. DTOs (`Mt5GroupDto`, `Mt5AccountDto`, `Mt5DealDto`, `Mt5PositionDto`) are **immutable records**. They have no EF `Id`, no `BrokerId`, no `IngestedAt`. That is the correct collector boundary: the store stamps catalog identity on upsert.

### 2.2 Mt5 draft (dead) — `TraderIntelligence.Mt5.Connectors`

Written 13:08:18; Application port 13:09:51. Namespace is the **adapter** project. Return types are **Domain persistence entities**. There is no `BrokerCode`. There is no registry pairing.

```5:44:D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs
public interface IBrokerConnector
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }

    Task<IReadOnlyList<Mt5Group>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Mt5Account>> GetAccountsAsync(Mt5Group group, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Mt5Deal>> GetDealsAsync(
        ulong login,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Mt5Position>> GetPositionsAsync(
        ulong login,
        CancellationToken cancellationToken = default);

    Task<DateTimeOffset> GetServerTimeAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<Mt5BrokerEvent> SubscribeEventsAsync(CancellationToken cancellationToken = default);
}

public sealed record Mt5BrokerEvent(
    Guid BrokerId,
    ulong Login,
    ulong? DealTicket,
    Mt5Deal? Deal,
    ulong? PositionTicket,
    Mt5Position? Position,
    DateTimeOffset EventTimeUtc,
    string EventType);
```

Nine members + one nested record. Comment on the record claims “SSE or future native binding.” Nothing produces the record.

---

## 3. Member-by-member comparison

| Concern | `IMt5BrokerConnector` (Application) | `IBrokerConnector` (Mt5) | Winner |
|---|---|---|---|
| Name vs architecture §6 | **`IMt5BrokerConnector`** — exact §6 identifier | `IBrokerConnector` — generic, not in §6 | Application |
| Layer | Application Contracts (port) | Mt5 Connectors (adapter) | Application |
| Identity for registry | `string BrokerCode` | **none** | Application |
| Catalog `Guid` | none (store resolves via `ITradingStore.ResolveBrokerIdAsync`) | none on interface; event carries `Guid BrokerId` | tie / later A58 `CatalogId` |
| Connect / Disconnect | `Task` + required `CancellationToken ct` | `Task` + optional `cancellationToken = default` | Application (callers already pass `ct`) |
| Connected probe | `Task<bool> IsConnectedAsync` | `bool IsConnected` sync property | **split** — keep one, not both; A58 wants property + `State` |
| Groups | `IReadOnlyList<Mt5GroupDto>` | `IReadOnlyList<Mt5Group>` **entity** | Application |
| Accounts filter | `string? group` — `null` = all (ingestion uses this) | **requires** `Mt5Group` entity | Application |
| Deals window | `long login`, `[from,to]` inclusive in fake | `ulong login`, same window args | Application (matches Domain `long Login`) |
| Positions | `long login` → `Mt5PositionDto` | `ulong login` → `Mt5Position` entity | Application |
| Orders | **missing** | **missing** | neither (§6 / A58 require it later) |
| Server time | **missing** | `GetServerTimeAsync` → `DateTimeOffset` | harvest into Application |
| Live events | **missing** | `SubscribeEventsAsync` → `Mt5BrokerEvent` | harvest **shape**, not this type |
| Symbols / ticks | missing | missing | neither (A58 later) |
| LastError / pump flag | missing | missing | neither (A58 later) |
| Return model | DTO records, no persistence keys | EF entities with `Id`/`BrokerId`/`IngestedAt` | Application |
| Login width | `long` (signed, matches `Mt5Account.Login`, demo logins `10001`…) | `ulong` (C++ `IMT5Client` / Manager) | Application **now**; A58 may widen later in **one** place |
| Default parameters | none | yes | Application (explicit tokens) |
| Implementors | `FakeMt5BrokerConnector` | **none** | Application |
| DI registrations | 2 singletons + registry | 0 | Application |
| Tests locking the type | 0 | 0 | neither |

Shared *names* that are **not** the same members: `ConnectAsync`, `DisconnectAsync`, `GetGroupsAsync`, `GetAccountsAsync`, `GetDealsAsync`, `GetPositionsAsync`. Return types, login width, and the accounts argument all differ. A class cannot implement both without adapters.

### 3.1 Why returning Domain entities is a hard fail

`Mt5Group` / `Mt5Account` / `Mt5Deal` / `Mt5Position` are EF rows. Persistence-only fields the draft would have to invent or leave empty:

| Entity | Persistence / policy fields **not** on the matching DTO |
|---|---|
| `Mt5Group` | `Id`, `BrokerId`, `EnabledForAnalysis`, `PlanMapping`, `LastDiscoveredAt`, `LastSyncedAt` |
| `Mt5Account` | `Id`, `BrokerId`, `RegistrationAt`, `LastAccessAt`, `LastSyncedAt` |
| `Mt5Deal` | `Id`, `BrokerId`, `IngestedAt`; entity clock is `DealTime`, DTO clock is `Time` |
| `Mt5Position` | `Id`, `BrokerId`, `Swap`, `TimeUpdate` |

A connector that returns those types either:

1. invents `Id`/`BrokerId` inside the adapter (leaks catalog into I/O), or
2. returns half-empty entities that look persisted.

`ITradingStore` already maps DTO → entity (`EfTradingStore.UpsertGroupAsync` / `UpsertAccountAsync` / `UpsertDealAsync` / `ReplacePositionsAsync`). `IBrokerConnector` would force a second mapper **or** skip the DTO and write entities from the adapter — both break the port.

`Mt5BrokerEvent` embeds `Mt5Deal?` / `Mt5Position?` (entities) plus `Guid BrokerId` and `ulong` tickets. Domain deal tickets are `long`. That event is not reusable.

### 3.2 `GetAccountsAsync` shapes are incompatible

| Call | Application | Mt5 draft |
|---|---|---|
| Ingestion today | `GetAccountsAsync(null, ct)` — census all logins | **cannot compile** — needs a `Mt5Group` instance |
| Filter by group | `string?` group name | pass a hydrated entity |

Architecture §7 / A39 / A40: enumerate **all** Manager-visible groups; plan map is **not** a fetch filter. The Application `null` group argument matches that. The draft’s required `Mt5Group` pushes filtering into the wrong place and cannot do a full-account census without first materializing every group entity.

`FakeMt5BrokerConnector.GetAccountsAsync` already treats null/whitespace as “all” (C10: not plan-filtered). That is the correct §7/§9 shape.

### 3.3 `IsConnected` vs `IsConnectedAsync`

The fake implements the async method as `Task.FromResult(_connected)`. No product caller reads it (`DealIngestionService` connects and proceeds; dashboard forges `Connected = true`). Either shape is unused **today**. A58 specifies a **sync** `IsConnected` plus `State`. When the port is extended, pick **one**. Do not add the Mt5 property alongside the Application method.

### 3.4 Dual-implement compile proof (not executed; types do not match)

If `FakeMt5BrokerConnector : IMt5BrokerConnector, IBrokerConnector` were attempted:

| `IBrokerConnector` member | Why the Fake cannot satisfy it |
|---|---|
| `GetGroupsAsync` → `Mt5Group` | Fake returns `Mt5GroupDto` |
| `GetAccountsAsync(Mt5Group, …)` | Fake takes `string?` |
| `GetDealsAsync(ulong, …)` | Fake takes `long` |
| `GetPositionsAsync(ulong, …)` | Fake takes `long` |
| `bool IsConnected` | Fake has `IsConnectedAsync` |
| `GetServerTimeAsync` | **absent** |
| `SubscribeEventsAsync` | **absent** |
| `BrokerCode` | extra on Application; not a problem for the draft, but the draft has **no** identity for `BrokerRegistry` |

That is the exact “two mostly identical connector codebases” §6 forbids.

---

## 4. Who actually uses what (measured)

### 4.1 `IMt5BrokerConnector` call graph

```
apps/mt5-worker/Worker.cs
        │  SyncBrokerAsync(Achiever | StarwaveFx) every 30s
        ▼
Application/Ingestion/DealIngestionService
        │  _registry.Get(brokerCode)
        ▼
Application/Contracts/IBrokerRegistry
        │
        ▼
Mt5/Connectors/BrokerRegistry  ──► IMt5BrokerConnector
        ▲
        │  AddSingleton ×2
Infrastructure/DependencyInjection
        │  DemoBrokerFactory.CreateDefault()
        ▼
FakeMt5BrokerConnector("ACHIEVER" | "STARWAVEFX")

DemoSeeder (same factory + new BrokerRegistry + DealIngestionService)
EfTradingStore (Mt5*Dto only — never sees IBrokerConnector)
```

`DealIngestionService.SyncBrokerAsync` members used:

1. `ConnectAsync`
2. `GetGroupsAsync`
3. `GetAccountsAsync(null, …)`
4. `GetDealsAsync(login, from, to)`
5. `GetPositionsAsync(login)`

Not used: `DisconnectAsync`, `IsConnectedAsync`, `BrokerCode` (registry already keyed it).

Worker never names either connector type.

### 4.2 `IBrokerConnector` call graph

```
src/Mt5/Connectors/IBrokerConnector.cs
        │
        └── (no implementor, no using, no DI, no test)
```

Grep hits under `D:\Prop\src` + `apps` + `tests` for the type name: **the interface declaration only**.

`FakeMt5BrokerConnector : IMt5BrokerConnector` — **not** `IBrokerConnector`.

### 4.3 Tests

`tests/Unit` and `tests/Integration`: **zero** references to either interface. No test locks `IBrokerConnector` in. Deleting it cannot fail a connector test because none exist (C17: 0/17 §60 unit areas COVERED).

---

## 5. Layering and project references

```
Domain          entities, BrokerCodes, reconstruction
    ▲
Application     IMt5BrokerConnector, DTOs, IBrokerRegistry, DealIngestionService
    ▲
Mt5             FakeMt5BrokerConnector, BrokerRegistry, IBrokerConnector (orphan)
    ▲
Infrastructure  DI + DemoSeeder + EfTradingStore
    ▲
apps/mt5-worker Worker
```

`TraderIntelligence.Application.csproj` references **Domain only**. Correct: Application cannot see Mt5.

`TraderIntelligence.Mt5.csproj` references Application + Domain. Correct for an adapter. **Incorrect** that the adapter also *declares* a second port.

If Application services ever took `IBrokerConnector`, Application would need a project reference to Mt5 → cycle (`Mt5` already references `Application`). That alone disqualifies the Mt5 interface as the product port.

Architecture §6 + A54: **ports live in Application (or Domain). Implementations live in Mt5.** A58 file target: `src/Application/Abstractions/Brokers/IMt5BrokerConnector.cs`. Current file `Contracts/Mt5Contracts.cs` is the right *layer*, wrong *folder name* — rename later, do not move the port into Mt5.

C35: Infrastructure → Mt5 is a composition leak acceptable for the demo slice. The orphan file does **not** justify that project reference.

---

## 6. Alignment with architecture §6 and A58

§6 sketch (quoted):

```csharp
public interface IMt5BrokerConnector
{
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<IReadOnlyCollection<Mt5Group>> GetGroupsAsync(...);
    Task<IReadOnlyCollection<Mt5Account>> GetAccountsAsync(...);
    Task<IReadOnlyCollection<Mt5Deal>> GetDealsAsync(...);
    Task<IReadOnlyCollection<Mt5Order>> GetOrdersAsync(...);
    Task<IReadOnlyCollection<Mt5Position>> GetPositionsAsync(...);
    IAsyncEnumerable<Mt5Event> SubscribeAsync(CancellationToken ct);
}
```

“The exact interface may be adjusted to the actual SDK.”
“Do not build two mostly identical connector codebases.”

| §6 member | Application now | Mt5 draft now |
|---|---|---|
| Type name `IMt5BrokerConnector` | **yes** | no |
| Connect / Disconnect | yes | yes |
| GetGroups / GetAccounts / GetDeals / GetPositions | yes (DTO, not entity) | yes (entity — wrong model) |
| GetOrdersAsync | **no** | **no** |
| SubscribeAsync | **no** | `SubscribeEventsAsync` (wrong name + entity payload) |
| Broker registry | **`IBrokerRegistry` exists and is used** | none |

A58 target is **larger** (server time, symbols, ticks, `BrokerCode` value type, `CatalogId`, `GetGroupLoginsAsync`, fail-closed deal paging, `LastError`, pump flag). Neither on-disk interface is A58-complete. That is **not** a reason to keep the draft. A58 §13: **Delete `IBrokerConnector`.** Application owns the port.

A58 completeness of the **winner** (honest, not greenwashed):

| A58 member | On `IMt5BrokerConnector` now |
|---|---|
| `BrokerCode Code` / `Guid CatalogId` / `DisplayName` | `string BrokerCode` only |
| `State` / `IsConnected` / `LastError` / `PumpEventsAvailable` | `IsConnectedAsync` only |
| `ConnectAsync` / `DisconnectAsync` | yes |
| `GetGroupsAsync` → `Mt5GroupInfo` | yes, but `Mt5GroupDto` |
| `GetGroupLoginsAsync` / `GetUserAsync` / `GetAccountAsync` | **no** |
| `GetAccountsAsync()` compose-all | `GetAccountsAsync(string? group)` — close enough for demo census |
| `GetDealsAsync(ulong, fromInclusive, toExclusive)` fail-closed | `long` + inclusive window; fake filters in memory |
| `GetOrdersAsync` | **no** |
| `GetPositionsAsync` | yes (`long`) |
| `GetSymbolsAsync` / `GetTickLastAsync` | **no** |
| `GetServerTimeAsync` | **no** (draft has a cousin) |
| `SubscribeAsync` / `TrySubscribeTicksAsync` | **no** (draft has a cousin) |

Prior reports vs this census:

| Report | Claim vs this census |
|---|---|
| A58 | Delete Mt5 `IBrokerConnector`; Application owns `IMt5BrokerConnector` — **still correct** |
| A79 | Draft is DEPRECATED; fake must **not** implement it — **still correct**; current fake complies |
| B24 | Same two hashes, same winner — **confirmed, no drift** |
| B04 / B02 | unused dual interface, zero implementers — **still correct** |
| C10 | Fake does not implement the draft — **still correct** |
| C35 / C54 | orphan file still on disk — **still correct** |
| A55 | listed `IBrokerConnector` as Class1 replacement — **stale**; file is leftover, not a live port |
| A57 | “keep or merge; do not keep two unused” — **partially stale**; Application port is now used |
| A59 | treated `IBrokerConnector` as “the C# port” — **superseded**; ingestion never bound it |
| A90 | “EXISTS (older surface). Do not invent a third” — **correct warning**; this report picks the older one for deletion |

---

## 7. Recommendation (binding for later implementation)

### 7.1 Keep

**`TraderIntelligence.Application.Contracts.IMt5BrokerConnector`** as the **only** collector port until an authorized pass moves it to `Application/Abstractions/Brokers/` (A58 path) **in the same change that updates all implementors**.

Keep with it:

- `Mt5GroupDto` / `Mt5AccountDto` / `Mt5DealDto` / `Mt5PositionDto`
- `IBrokerRegistry`
- `FakeMt5BrokerConnector` + `BrokerRegistry` + `DemoBrokerFactory` in Mt5
- DI registrations in `DependencyInjection.cs`

### 7.2 Delete (when a source-edit pass is authorized — **not this report**)

`D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` entirely (`IBrokerConnector` + `Mt5BrokerEvent`).

Zero compile break: nothing implements or consumes it.

Adjacent leftover, **not** this compare: `Mt5BrokerOptions` is also unused (A58 §13: replace; C54: not bound). Do not fold it into this delete unless the same authorized pass is already touching Mt5 configuration.

### 7.3 Harvest into `IMt5BrokerConnector` (same edit pass as delete, or immediately after)

Copy **ideas**, not types:

| From draft | Land on Application as | Notes |
|---|---|---|
| `GetServerTimeAsync` | `Task<DateTimeOffset> GetServerTimeAsync(CancellationToken ct)` (or A58 `Mt5ServerTime`) | Must not silently use host clock (A04 / A59). Fake can return a seeded offset. |
| `SubscribeEventsAsync` | `IAsyncEnumerable<…> SubscribeAsync(CancellationToken ct)` | §6 name. Payload = **new DTO** (`Mt5SourceEvent` / deal+position+user+order), **not** `Mt5BrokerEvent`, **not** EF entities. Deals stay polled (A12 / A79: no `PUMP_MODE_DEALS`). |
| `IsConnected` property | only if `IsConnectedAsync` is removed | Do not ship both. |

### 7.4 Do **not** harvest

| Draft bit | Why leave it |
|---|---|
| Interface name `IBrokerConnector` | §6 / A58 name is `IMt5BrokerConnector` |
| Return of `Mt5Group` / `Mt5Account` / `Mt5Deal` / `Mt5Position` | persistence types |
| `GetAccountsAsync(Mt5Group)` | breaks all-account census; ingestion uses `null` |
| `ulong login` / `ulong` tickets on the event | Domain + DTOs + worker are `long` today; widen **once** in A58, everywhere |
| `Mt5BrokerEvent` | incomplete (deal+position only); A79 forbids emitting it |
| Default `cancellationToken = default` | current callers pass tokens; A58 does not require defaults |

### 7.5 Forbidden next steps

1. A third interface (`IMt5Client`, `ISourceBroker`, `IMT5Manager` in C#).
2. `FakeMt5BrokerConnector : IMt5BrokerConnector, IBrokerConnector`.
3. Application taking a dependency on `TraderIntelligence.Mt5.Connectors`.
4. Production `Mt5ManagerBrokerConnector` implementing the Mt5 draft.
5. Moving `IMt5BrokerConnector` into the Mt5 project “because connectors live there.”
6. Hand-writing MQ5 or touching Manager DLL from this cleanup.

---

## 8. Completeness of the winner (do not greenwash)

`IMt5BrokerConnector` is the right **name and layer**. It is **not** Phase-1 complete. Deleting the draft does **not** connect Achiever or StarwaveFX (C42 / C54).

| Gap | Severity | Blocked by keeping the draft? |
|---|---|---|
| No `GetOrdersAsync` | Phase 1 reconcile / A58 | No — draft also lacks it |
| No `SubscribeAsync` | live ingest | No — harvest the idea after delete |
| No `GetServerTimeAsync` | checkpoint windows (A59) | No — one method to add |
| `string BrokerCode` vs A58 `BrokerCode` + `CatalogId` | identity hardening | No |
| `long` vs Manager `ulong` | overflow only if login > `long.MaxValue` (not in demo 10001/99001) | No — do not fork types |
| Fake is the only impl | no real Manager / HTTP collector | No — impl belongs in Mt5 against Application port |
| `Mt5BrokerOptions` still invents `RemoteUrl` / `ApiKey` / pool 25 | config law §56 | Separate from this dup (A58) |
| No connector unit tests | quality | Tests should fake `IMt5BrokerConnector` |
| Dashboard `Connected = true` never calls `IsConnectedAsync` | honesty | No |

Deleting the draft **reduces** risk of a second impl drifting. It does not flip §69 / §68.

---

## 9. Why not the other way (reject `IBrokerConnector` as the survivor)

If we deleted `IMt5BrokerConnector` and standardized on the Mt5 draft we would have to:

1. Move the interface into Application anyway (or create an Application→Mt5 cycle).
2. Rewrite `FakeMt5BrokerConnector`, `BrokerRegistry`, DI, seeder, `DealIngestionService`, `ITradingStore`, `EfTradingStore` off DTOs onto entities (or add a mapper the current DTO path already is).
3. Change `GetAccountsAsync(null)` to a group-entity loop.
4. Change every `long login` in Domain / worker / scores to `ulong`, or cast at every call.
5. Rename away from the architecture identifier.

That is a larger, worse change than deleting 45 unused lines.

---

## 10. Suggested delete checklist (future authorized edit — not done here)

1. Confirm grep: `IBrokerConnector` / `Mt5BrokerEvent` / `SubscribeEventsAsync` still zero consumers under `src` / `apps` / `tests`.
2. Delete `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`.
3. Optionally add `GetServerTimeAsync` + `SubscribeAsync` to `IMt5BrokerConnector` **in the same PR** if a live-ingest caller is landing; otherwise delete first (YAGNI until A59/A07 jobs exist).
4. Extend `FakeMt5BrokerConnector` only if those members are added.
5. Do not add a test that references the deleted type.

---

## 11. File inventory (this comparison)

| Path | SHA-256 | Physical lines | Disposition |
|---|---|---:|---|
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` | 69 | **KEEP** (port + DTOs + registry) |
| `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | `6B7AA65F293AF43A548D09BC92332286A5123EDB03DCCD498C2217490CCBC5BC` | 45 | **DELETE** |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 170 | **KEEP** (already on the winner) |

Hashes match B24. No product drift between B24 and D25.

---

## 12. One-line recommendation

**Single collector port = Application `IMt5BrokerConnector`. Treat `src/Mt5/Connectors/IBrokerConnector.cs` as dead code; delete it; do not implement it.**
