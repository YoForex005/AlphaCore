# D02 — Application layer census (`src/Application`)

| Field | Value |
|---|---|
| Agent | D02 (read-only census) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\src\Application` |
| Product source modified | **No.** This report is the only write. |
| Adjacent audits (do not replace) | A02 is **stale** (`Class1` era). B02 is the architecture-section **audit**. This file is the **inventory**: files, types, members, implementers, callers, tests, absences. |
| Method | Enumerate product files (exclude `bin/` / `obj/` as design). SHA-256 + sizes via `Get-FileHash`. `rg` for types, implementers, callers, `FluentValidation`. Cross-read Infrastructure / Mt5 / hosts / tests. Compile artifacts cited as evidence only. |

Classification vocabulary (architecture §73.B) is used only in the absence catalog. Counts below are **measured**, not completeness percentages.

---

## 1. What is on disk

### 1.1 Product files (complete)

Hand-authored product files under `D:\Prop\src\Application`, excluding `bin/` and `obj/`:

| Path | Bytes | Lines | Non-blank | Last write (local) | SHA-256 |
|---|---:|---:|---:|---|---|
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | 433 | 17 | 13 | 2026-08-18 12:55:09 | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | 1,858 | 69 | 62 | 2026-08-18 13:09:51 | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 2,577 | 97 | 89 | 2026-08-18 13:09:51 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4,277 | 103 | 90 | 2026-08-18 13:09:51 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` |

**Totals (product):** 4 files, **9,145 bytes**, **286 lines**, **254 non-blank**.

`Class1.cs` is **gone**. No other product `.cs` / `.json` / `.resx` / `.md` under Application.

### 1.2 Folders that exist vs do not

Present:

```text
D:\Prop\src\Application\
  TraderIntelligence.Application.csproj
  Contracts\
    Mt5Contracts.cs
  Dashboard\
    DashboardModels.cs
  Ingestion\
    DealIngestionService.cs
  bin\          (compile output; not design)
  obj\          (MSBuild; not design)
```

**Not present** (no files, no empty stubs):

`Abstractions/`, `Ports/`, `Interfaces/`, `Validators/`, `DTOs/`, `Features/`, `UseCases/`, `Services/` (beyond the two classes in `Ingestion/`), `Brokers/`, `Copy/`, `Risk/`, `Scoring/`, `Shadow/`, `Execution/`, `Outbox/`, `Checkpoints/`, `Reconciliation/`, `Events/`, `Orders/`.

### 1.3 Solution membership

| Item | Value |
|---|---|
| Project name | `TraderIntelligence.Application` |
| sln path | `src\Application\TraderIntelligence.Application.csproj` |
| sln GUID | `{8A0BB7FD-D1CC-46B3-9C0C-6A2408866F36}` |
| sln parent folder | `src` (`Mt5TraderIntelligence.sln` line 16) |
| SDK | `Microsoft.NET.Sdk` (class library) |
| TFM | `net8.0` |
| Nullable / implicit usings | on (csproj + `Directory.Build.props`) |
| Assembly version (generated) | `1.0.0.0` |
| InformationalVersion (Release `obj`) | `1.0.0+398a14200ec65714c4077eed55c46808382ca1e3` |

---

## 2. Project graph

`TraderIntelligence.Application.csproj` in full:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.9.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

### 2.1 Outbound references (Application →)

| Kind | Target | Used in product `.cs`? |
|---|---|---|
| Project | `..\Domain\TraderIntelligence.Domain.csproj` | **Yes** (`Domain.Enums`, `Domain.Entities.TraderScore`, `Domain.Reconstruction`, `Domain.Scoring.BaselineScorer`) |
| Package | `FluentValidation` 11.9.2 | **No.** Zero `using FluentValidation`, `IValidator`, or `AbstractValidator` in any `D:\Prop\src` / `D:\Prop\apps` / `D:\Prop\tests` `.cs` file. |

No project references to Infrastructure, Mt5, Fix.CTrader, or apps. Layering arrow is **Application → Domain only**.

### 2.2 Inbound project references (who depends on Application)

| Project | Path | Uses Application **types** in `.cs`? |
|---|---|---|
| `TraderIntelligence.Infrastructure` | `D:\Prop\src\Infrastructure` | **Yes** — implements `ITradingStore`, `IDashboardQueries`; DI + seeder call use-cases |
| `TraderIntelligence.Mt5` | `D:\Prop\src\Mt5` | **Yes** — `FakeMt5BrokerConnector` : `IMt5BrokerConnector`; `BrokerRegistry` : `IBrokerRegistry` |
| `TraderIntelligence.Fix.CTrader` | `D:\Prop\src\Fix.CTrader` | **ProjectReference only.** Zero `using TraderIntelligence.Application*` in that tree |
| `TraderIntelligence.Api` | `D:\Prop\apps\api` | **Yes** — maps `IDashboardQueries`; `/api/ops/resync` calls ingest + scoring |
| `TraderIntelligence.Mt5Worker` | `D:\Prop\apps\mt5-worker` | **Yes** — 30s poll of `DealIngestionService` + `ReconstructionScoringService` |
| `TraderIntelligence.FixWorker` | `D:\Prop\apps\fix-worker` | **Seed only** — `Program.cs` resolves `ITradingStore` + `ReconstructionScoringService` for `DemoSeeder`. `Worker.cs` does not use Application types |
| `TraderIntelligence.Tests.Unit` | `D:\Prop\tests\Unit` | **ProjectReference only.** Zero Application type names in unit test `.cs` |
| `TraderIntelligence.Tests.Integration` | `D:\Prop\tests\Integration` | **Yes** — `SeedingAndStoreTests` constructs `ReconstructionScoringService` and `Mt5DealDto` |

---

## 3. Type census (16 public types)

All types are `public`. There are no `internal` types, no nested types, no enums, no attributes, no extension methods, no static helpers.

| # | Namespace | Type | Kind | File |
|---:|---|---|---|---|
| 1 | `TraderIntelligence.Application.Contracts` | `Mt5GroupDto` | sealed record | `Contracts\Mt5Contracts.cs` |
| 2 | | `Mt5AccountDto` | sealed record | same |
| 3 | | `Mt5DealDto` | sealed record | same |
| 4 | | `Mt5PositionDto` | sealed record | same |
| 5 | | `IMt5BrokerConnector` | interface | same |
| 6 | | `IBrokerRegistry` | interface | same |
| 7 | `TraderIntelligence.Application.Dashboard` | `OverviewDto` | sealed record | `Dashboard\DashboardModels.cs` |
| 8 | | `BrokerStatusDto` | sealed record | same |
| 9 | | `GroupRowDto` | sealed record | same |
| 10 | | `TraderRowDto` | sealed record | same |
| 11 | | `FixSessionDto` | sealed record | same |
| 12 | | `RiskDashboardDto` | sealed record | same |
| 13 | | `IDashboardQueries` | interface | same |
| 14 | `TraderIntelligence.Application.Ingestion` | `ITradingStore` | interface | `Ingestion\DealIngestionService.cs` |
| 15 | | `DealIngestionService` | sealed class | same |
| 16 | | `ReconstructionScoringService` | sealed class | same |

**Buckets:** 10 records, 4 interfaces, 2 classes.

No `Mt5OrderDto`, `Mt5EventDto`, `CopyIntentDto`, `ExecutionIntentDto`, `RiskDecisionDto`, `OutboxMessage`, `SyncCheckpointDto`, `ScoreUpdateRequest`, or validator types.

---

## 4. Port census (interfaces)

### 4.1 `IMt5BrokerConnector` — collector port (§6)

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

| Member | Present | vs architecture §6 sketch |
|---|---|---|
| `BrokerCode` | yes (extra) | not on sketch; used as registry key |
| `ConnectAsync` | yes | sketch |
| `DisconnectAsync` | yes | sketch |
| `IsConnectedAsync` | yes (extra) | sketch has no connected query |
| `GetGroupsAsync` | yes | sketch (`IReadOnlyCollection<Mt5Group>`) |
| `GetAccountsAsync(string? group)` | yes | sketch (`GetAccountsAsync(...)`) |
| `GetDealsAsync(login, from, to)` | yes | sketch |
| `GetPositionsAsync(login)` | yes | sketch |
| `GetOrdersAsync` | **no** | named on sketch |
| `SubscribeAsync` / event stream | **no** | sketch `IAsyncEnumerable<Mt5Event>` |

**Implementer (one):** `TraderIntelligence.Mt5.Connectors.FakeMt5BrokerConnector`. No live Manager / HTTP implementer of this interface.

`DealIngestionService.SyncBrokerAsync` calls `ConnectAsync` and **never** `DisconnectAsync` or `IsConnectedAsync`.

### 4.2 `IBrokerRegistry`

```65:69:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
public interface IBrokerRegistry
{
    IMt5BrokerConnector Get(string brokerCode);
    IReadOnlyList<IMt5BrokerConnector> All();
}
```

**Implementer (one):** `TraderIntelligence.Mt5.Connectors.BrokerRegistry` — `Dictionary<string, IMt5BrokerConnector>` keyed ordinal-ignore-case; `Get` throws `KeyNotFoundException`.

`All()` is **never called** from Application. DI registers two `FakeMt5BrokerConnector` singletons (`ACHIEVER`, `STARWAVEFX`) then builds the registry from `GetServices<IMt5BrokerConnector>()`.

### 4.3 `ITradingStore` — persistence façade

```8:18:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
public interface ITradingStore
{
    Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct);
    Task UpsertAccountAsync(Guid brokerId, Mt5AccountDto account, DateTimeOffset now, CancellationToken ct);
    Task<bool> UpsertDealAsync(Guid brokerId, Mt5DealDto deal, DateTimeOffset now, CancellationToken ct);
    Task ReplacePositionsAsync(Guid brokerId, long login, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct);
    Task<IReadOnlyList<NormalizedDeal>> LoadDealsAsync(Guid brokerId, string brokerCode, long login, CancellationToken ct);
    Task ReplaceReconstructedAsync(Guid brokerId, long login, IReadOnlyList<ReconstructedTradeResult> trades, CancellationToken ct);
    Task UpsertScoreAsync(TraderScore score, CancellationToken ct);
    Task<Guid> ResolveBrokerIdAsync(string brokerCode, CancellationToken ct);
}
```

Eight members, three concerns on one port:

| Concern | Members |
|---|---|
| Raw ingest | `UpsertGroupAsync`, `UpsertAccountAsync`, `UpsertDealAsync`, `ReplacePositionsAsync` |
| Identity | `ResolveBrokerIdAsync` |
| Reconstruction + score writes | `LoadDealsAsync`, `ReplaceReconstructedAsync`, `UpsertScoreAsync` |

**Not on the port:** checkpoints, outbox, orders, ingestion_events, copy intents, execution intents, risk decisions, kill switch writes, unit-of-work / `SaveChanges` batching.

**Implementer (one):** `TraderIntelligence.Infrastructure.Persistence.EfTradingStore`.

Observed implementer semantics (Infrastructure, not Application, but they define what the port *does* today):

| Method | Implementer behavior |
|---|---|
| `ResolveBrokerIdAsync` | `Brokers.SingleAsync(Code == brokerCode)` — throws if missing |
| `UpsertGroupAsync` | insert all DTO fields; update only `Currency` + `LastSyncedAt` |
| `UpsertAccountAsync` | insert all DTO fields; update `GroupName`, `Balance`, `Equity`, `LastSyncedAt` only (not leverage/margin/profit) |
| `UpsertDealAsync` | insert-if-absent on `(BrokerId, DealTicket)`; returns `false` on duplicate; **no field update** |
| `ReplacePositionsAsync` | delete all positions for `(brokerId, login)` then insert |
| `LoadDealsAsync` | all deals for login ordered by time/ticket → `NormalizedDeal` (`BrokerId` string is **brokerCode**, not Guid; SL/TP left null) |
| `ReplaceReconstructedAsync` | wipe-and-replace reconstructed trades for login |
| `UpsertScoreAsync` | upsert `TraderScores` by `(BrokerId, Login)` and always append `TraderScoreHistory` |

Each method `SaveChangesAsync` independently.

### 4.4 `IDashboardQueries` — read models (§§46–53 shape, not engine)

```88:97:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public interface IDashboardQueries
{
    Task<OverviewDto> GetOverviewAsync(CancellationToken ct);
    Task<IReadOnlyList<BrokerStatusDto>> GetBrokersAsync(CancellationToken ct);
    Task<IReadOnlyList<GroupRowDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct);
    Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct);
    Task<IReadOnlyList<FixSessionDto>> GetFixSessionsAsync(CancellationToken ct);
    Task<RiskDashboardDto> GetRiskAsync(CancellationToken ct);
}
```

**Implementer (one):** `TraderIntelligence.Infrastructure.Dashboard.EfDashboardQueries`.

No `GetTradesAsync` — API `/api/trades` queries `TraderDbContext` directly and **bypasses** this port. No reconciliation / settings / health ports.

---

## 5. Use-case census (2 classes)

### 5.1 `DealIngestionService`

| Item | Value |
|---|---|
| Ctor | `(IBrokerRegistry registry, ITradingStore store)` |
| Public API | `Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)` |
| Return | count of **newly inserted** deals (`UpsertDealAsync` returned true) |
| Side effects | connect connector; resolve broker id; upsert every group; upsert every account (`GetAccountsAsync(null)`); per account fetch deals in `[from,to]` and positions (replace) |
| Not done | disconnect; checkpoints; normalize; validate; outbox; orders; subscribe; per-group account filter; transaction across accounts |

Callers of `SyncBrokerAsync`:

| Caller | Window | Brokers |
|---|---|---|
| `apps/mt5-worker/Worker.cs` | `UtcNow-30d` … `UtcNow+1m`, every 30s | `BrokerCodes.Achiever`, `BrokerCodes.StarwaveFx` |
| `apps/api/Program.cs` `POST /api/ops/resync` | `2026-01-01` … `UtcNow` | literal `"ACHIEVER"`, `"STARWAVEFX"` |
| `Infrastructure/Seeding/DemoSeeder.cs` | `2026-01-01` … `2026-12-31` | `BrokerCodes.Achiever`, `BrokerCodes.StarwaveFx` |

No unit test constructs `DealIngestionService`. Integration coverage is only via `DemoSeeder`.

### 5.2 `ReconstructionScoringService`

| Item | Value |
|---|---|
| Ctor | `(ITradingStore store, TradeReconstructor reconstructor, BaselineScorer scorer)` |
| Public API | `Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)` |
| Steps | resolve broker → load deals → `TradeReconstructor.Reconstruct` → replace reconstructed → score **completed XAU** trades only → `UpsertScoreAsync` new `TraderScore` (`Id = Guid.NewGuid()`, features + `CurrentState = score.SuggestedState`) |

It writes **RiskScore / BehaviorScore / EarlyQualityScore / flags / TraderState**, not the §39 triple (`candidate`, `confidence`, `suggested allocation`).

Hard-coded login list used by every host after ingest: `10001, 10002, 10003, 99001` (99001 → STARWAVEFX, others → ACHIEVER). Application itself does **not** enumerate accounts; hosts do.

Callers of `RebuildTraderAsync`: same three as ingest (worker, `/api/ops/resync`, `DemoSeeder`) plus integration test via seeder.

---

## 6. DTO field catalog

### 6.1 Collector DTOs (`Contracts`)

Domain enums used on the wire: `DealAction`, `DealEntry`, `TradeDirection`.

| DTO | Positional fields (order) |
|---|---|
| `Mt5GroupDto` (7) | `Name`, `Currency`, `CurrencyDigits`, `Company`, `MarginCall`, `MarginStopOut`, `ConnectionsAllowed` |
| `Mt5AccountDto` (8) | `Login`, `GroupName`, `Leverage`, `Balance`, `Equity`, `Margin`, `MarginFree`, `Profit` |
| `Mt5DealDto` (14) | `DealTicket`, `Login`, `OrderTicket`, `PositionId`, `Symbol`, `Action`, `Entry`, `VolumeNative`, `Price`, `Profit`, `Commission`, `Swap`, `Time`, `Comment` |
| `Mt5PositionDto` (12) | `PositionTicket`, `Login`, `Symbol`, `Direction`, `VolumeNative`, `PriceOpen`, `PriceCurrent`, `PriceSl`, `PriceTp`, `Profit`, `TimeCreate` |

Field mapping onto Domain entities (persistence, not Application):

| DTO field | Domain entity field | Notes |
|---|---|---|
| `Mt5GroupDto.*` | `Mt5Group` same names | Entity extras: `Id`, `BrokerId`, `EnabledForAnalysis`, `PlanMapping`, `LastDiscoveredAt`, `LastSyncedAt` |
| `Mt5AccountDto.*` | `Mt5Account` same names | Entity extras: `Id`, `BrokerId`, `RegistrationAt`, `LastAccessAt`, `LastSyncedAt`. DTO has no registration/last-access |
| `Mt5DealDto.Time` | `Mt5Deal.DealTime` | Entity extras: `Id`, `BrokerId`, `IngestedAt`. No deal reason / volume-ext / SL-TP on either |
| `Mt5PositionDto.*` | `Mt5Position` same names | Entity extras: `Id`, `BrokerId`, `Swap`, `TimeUpdate`. DTO has no swap |

`VolumeNative` is `ulong` on DTO and entity (native MT5 units; fake factory uses scale **10 000**). Application does not convert lots.

### 6.2 Dashboard DTOs

| DTO | Fields (count) |
|---|---|
| `OverviewDto` | 17: `TotalAccounts`, `ConnectedBrokers`, `XauTraders`, `TradersWithThreeTrades`, `Watch`, `Shadow`, `LiveCandidates`, `Live`, `RiskBlocked`, `ShadowPnl`, `DestinationRealPnl`, `XauGross`, `XauNet`, `Mt5Healthy`, `QuoteHealthy`, `TradeHealthy`, `RealCopyEnabled` |
| `BrokerStatusDto` | 8: `Code`, `DisplayName`, `Server`, `ManagerLoginMasked`, `Connected`, `GroupCount`, `AccountCount`, `LastEventAt` |
| `GroupRowDto` | 7: `Broker`, `Group`, `Accounts`, `EnabledForAnalysis`, `PlanMapping`, `LastDiscovered`, `LastSynced` |
| `TraderRowDto` | 14: `Broker`, `Login`, `Group`, `CompletedXauTrades`, `NetSourcePnl`, `EarlyScore`, `MlProbability`, `RiskScore`, `Martingale`, `AveragingDown`, `LotEscalation`, `State`, `ShadowPnl`, `LastScored` |
| `FixSessionDto` | 17: `Qualifier`, `Host`, `Port`, `Connected`, `LoggedOn`, `Status`, `LastInbound`, `LastOutbound`, `InboundSeq`, `OutboundSeq`, `ReconnectCount`, `LastError`, `InstrumentId`, `Bid`, `Ask`, `QuoteAgeSeconds`, `ExecutionEnabled` |
| `RiskDashboardDto` | 8: `DailyPnl`, `Drawdown`, `XauLong`, `XauShort`, `XauNet`, `KillSwitch`, `RealCopyEnabled`, `RecentRejectReasons` |

Implementer fills several numerics with literals (`DestinationRealPnl`/`XauGross`/`XauNet` = 0; `RealCopyEnabled` = false; trader `ShadowPnl` = 0; risk PnL/exposure = 0; broker `Connected` = true). That is Infrastructure, recorded so the DTO is not mistaken for a live telemetry contract.

**React `apps/web/src/types/index.ts` is not 1:1** with these records (different names: `totalBrokers` vs `connectedBrokers`, `tradersByState` vs discrete counts, `fixHealthy` vs three bools, `RiskStatus.equity/balance/margin` absent from `RiskDashboardDto`). Census only: Application does not own the TS types.

---

## 7. Domain types Application actually references

| Domain type | Where Application uses it |
|---|---|
| `DealAction`, `DealEntry` | `Mt5DealDto` |
| `TradeDirection` | `Mt5PositionDto` |
| `TraderState` | `TraderRowDto.State` |
| `TraderScore` | `ITradingStore.UpsertScoreAsync`; constructed in `ReconstructionScoringService` |
| `NormalizedDeal` | `ITradingStore.LoadDealsAsync` return |
| `ReconstructedTradeResult` | `ITradingStore.ReplaceReconstructedAsync`; `trades.Where(t => t.Completed && t.IsXauUsd)` |
| `TradeReconstructor` | injected into `ReconstructionScoringService` |
| `BaselineScorer` | injected; `Score(completedXau)` |

**Domain types that exist and Application does not mention:** `RiskEngine`, `ShadowCopyEngine`, `CopyIntent`, `ExecutionIntent`, `RiskDecisionRecord`, `OutboxEvent`, `SyncCheckpoint`, `KillSwitch`, `ClOrdIdFactory`, `ExecutionOrderStateMachine`, `QuantityNormalizer`, `SymbolNormalizer`, `VolumeConverter`, `CopyIntentExpiry`, all other entities/enums (`OutboxEventType`, `KillSwitchMode`, `RiskDecisionOutcome`, …).

---

## 8. DI registration (Infrastructure, not Application)

`D:\Prop\src\Infrastructure\DependencyInjection.cs` `AddTraderIntelligence`:

| Service | Lifetime | Implementation |
|---|---|---|
| `IMt5BrokerConnector` ×2 | Singleton | `DemoBrokerFactory.CreateDefault()` → fake ACHIEVER + STARWAVEFX |
| `IBrokerRegistry` | Singleton | `new BrokerRegistry(GetServices<IMt5BrokerConnector>())` |
| `ITradingStore` | Scoped | `EfTradingStore` |
| `IDashboardQueries` | Scoped | `EfDashboardQueries` |
| `TradeReconstructor` | Singleton | Domain |
| `BaselineScorer` | Singleton | Domain |
| `DealIngestionService` | Scoped | self |
| `ReconstructionScoringService` | Scoped | self |

No `IValidator<>`, no `IRiskEngine`, no outbox, no checkpoint store, no live connector factory.

---

## 9. Host / API surface that binds to Application

| HTTP / host | Application type | Notes |
|---|---|---|
| `GET /api/overview` | `IDashboardQueries.GetOverviewAsync` | not `/api/v1/overview` (A91) |
| `GET /api/brokers` | `GetBrokersAsync` | |
| `GET /api/groups` | `GetGroupsAsync` | |
| `GET /api/traders` | `GetTradersAsync(broker, state)` | |
| `GET /api/traders/{broker}/{login}` | `GetTraderAsync` | returns same `TraderRowDto`, not a detail DTO |
| `GET /api/fix/sessions` | `GetFixSessionsAsync` | |
| `GET /api/risk` | `GetRiskAsync` | |
| `GET /api/risk/status` | `GetRiskAsync` | **same** method as `/api/risk` |
| `POST /api/ops/resync` | `DealIngestionService` + `ReconstructionScoringService` | |
| `GET /api/trades` | **none** | `TraderDbContext` bypass |
| `GET /api/health`, `/health`, `/ready`, `/api/reconciliation/status`, `/api/settings` | **none** | anonymous objects in `Program.cs` |
| mt5-worker loop | ingest + scoring | 30s; logins hard-coded |
| fix-worker loop | none | seed uses Application; worker does not |
| API / both workers startup | `ITradingStore` + `ReconstructionScoringService` | `DemoSeeder.SeedAsync` |

---

## 10. Tests that touch Application

| Test | File | What it actually exercises |
|---|---|---|
| `Demo_seed_discovers_groups_reconstructs_and_scores` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | `ReconstructionScoringService` via `DemoSeeder` (which also new's `DealIngestionService`) |
| `Deal_upsert_is_idempotent` | same | `Mt5DealDto` + `EfTradingStore.UpsertDealAsync` only — **not** `DealIngestionService` |

**Zero** unit tests for `DealIngestionService`, `ReconstructionScoringService`, `IMt5BrokerConnector` contract, `IBrokerRegistry`, or `IDashboardQueries`.

---

## 11. Duplicate / unused surfaces (outside Application, impact on census)

| Item | Path | Relation to Application |
|---|---|---|
| Unused `IBrokerConnector` | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | Second collector shape: Domain entities, `SubscribeEventsAsync`, `GetServerTimeAsync`, `ulong` logins, **no** `GetOrders`. Zero implementers. B24: keep Application `IMt5BrokerConnector`. |
| `Fix.CTrader` ProjectReference | `TraderIntelligence.Fix.CTrader.csproj` | Dead reference; no Application usings |
| Unit test ProjectReference | `TraderIntelligence.Tests.Unit.csproj` | Dead reference; no Application usings |
| `FluentValidation` 11.9.2 | Application csproj | Restored into consumers; unused |

---

## 12. Absence catalog (measured “not in Application”)

These names do **not** appear in Application product C#:

`GetOrders`, `Subscribe`, `Mt5Order`, `Mt5Event`, `checkpoint`, `SyncCheckpoint`, `Outbox`, `IEventBus`, `CopyIntent`, `ExecutionIntent`, `IRiskEngine`, `RiskEngine`, `KillSwitch`, `IValidator`, `AbstractValidator`, `FluentValidation`, `ClOrdId`, `NewOrderSingle`, `ShadowCopy`, `IFixExecution`, `ingestion_events`.

Implied ports still missing (A02/B02 list; still true):

| ID | Needed type | Status in Application |
|---|---|---|
| B1f/g | `GetOrdersAsync`, `SubscribeAsync` | **MISSING** |
| O2 | `ISyncCheckpointStore` | **MISSING** |
| O3 | `ILiveIngestionService` | **MISSING** |
| O5 | `IIngestionReconciliationService` | **MISSING** |
| O6–O9 | outbox writer / processor / event-bus / typed payloads | **MISSING** |
| C1–C7 | copy candidate / CopyIntent / ExecutionIntent / FIX worker port | **MISSING** |
| R1–R5 | `IRiskEngine` + scoring→risk DTO + hard-limit ports | **MISSING** |
| S2 | outbox score-update handler | **MISSING** |

`FluentValidation` package: present, unused → **EXISTS_NEEDS_REFACTOR** (package only).

Nothing in Application is **UNSAFE** (no secrets, no FIX send, no P/Invoke, no connection strings).

---

## 13. Compile artifacts (evidence, not design)

| Config | DLL | PDB | deps.json | Last write |
|---|---:|---:|---:|---|
| Debug `net8.0` | 58,368 | 19,620 | 1,495 | 2026-08-18 13:24:35 |
| Release `net8.0` | 56,832 | 19,380 | 1,495 | 2026-08-18 13:26:33 |

Both configs also copy `TraderIntelligence.Domain.dll` beside the Application DLL. Generated global usings are the default SDK set (`System`, `Collections.Generic`, `IO`, `Linq`, `Net.Http`, `Threading`, `Threading.Tasks`). No extra usings.

---

## 14. Counts

| Item | Count |
|---:|---|
| Product files | **4** (1 csproj + 3 `.cs`) |
| Product folders with source | **3** (`Contracts`, `Dashboard`, `Ingestion`) |
| Public types | **16** |
| Interfaces / ports | **4** |
| Use-case classes | **2** |
| Public methods on use-cases | **2** (`SyncBrokerAsync`, `RebuildTraderAsync`) |
| Collector DTO fields | **41** (7+8+14+12) |
| Dashboard DTO fields | **71** (17+8+7+14+17+8) |
| Interface members | **25** (8+2+8+7) |
| Implementers of Application ports | **4** classes (all outside Application) |
| Hosts that call ingest/score | **3** (API resync, mt5-worker, DemoSeeder) |
| API endpoints bound to `IDashboardQueries` | **8** (including `/api/risk` and `/api/risk/status` sharing one method) |
| Application unit tests | **0** |
| Integration tests touching Application types | **2** (one via seeder, one DTO+store) |
| `FluentValidation` validators | **0** |
| `Class1` leftovers | **0** |
| Live `IMt5BrokerConnector` implementers | **0** (fake only) |

---

## 15. Honest summary

`TraderIntelligence.Application` is a **thin port + two use-cases + dashboard read models** library:

1. **§6 collector port** — `IMt5BrokerConnector` + four DTOs + `IBrokerRegistry`. Six of eight sketch members exist. Orders and subscribe do not.
2. **Poll ingest** — `DealIngestionService.SyncBrokerAsync` walks groups/accounts/deals/positions through `ITradingStore`. No checkpoint, live loop, validation, or outbox.
3. **Rebuild + baseline score** — `ReconstructionScoringService` couples reconstruction and scoring and is invoked in the same host cycle as ingest.
4. **Dashboard query port** — six DTOs + `IDashboardQueries`, consumed by the API. Not an execution or risk engine.

A02’s empty `Class1` project is obsolete. B02’s architecture FAIL against §§12/32/39 is consistent with this inventory: those sections have **no Application types**.

This census did not modify product source.
