# C34 — `apps/api/Program.cs` usings vs `ITradingStore`

| Field | Value |
|---|---|
| Agent | C34 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C34_api_usings.md` |
| Ask | Read `apps/api/Program.cs` for **missing usings** on `ITradingStore`. |
| Product source modified | **No.** Report only. |
| Primary SUT | `D:\Prop\apps\api\Program.cs` (not under `src\`; workspace root is `D:\Prop\src`) |
| Port definition | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`namespace TraderIntelligence.Application.Ingestion`) |
| Law | C# name resolution (using directives + implicit usings). Architecture §73 classification. |
| Relates | C04 (same `Program.cs` hash), C05 (seed resolve of `ITradingStore`), B06/A06 (stale host snapshots), B07 (workers FQN the same type) |

Classification (architecture §73): `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Headline answer (measured)

| Question | Answer |
|---|---|
| Is `using` for `ITradingStore` **missing** on the API host? | **No.** |
| Which directive brings it in? | `using TraderIntelligence.Application.Ingestion;` — **line 2** |
| Does line 90 `GetRequiredService<ITradingStore>()` compile as an unqualified name? | **Yes**, from that directive. The type is `public` in that namespace. |
| Would CS0246 (`The type or namespace name 'ITradingStore' could not be found`) fire on this file as written? | **No.** |
| Must anyone add another using for this identifier? | **No.** Do not add `using TraderIntelligence.Application.Contracts` for this type — `ITradingStore` is **not** in Contracts. |

Honest one-liner: **API `Program.cs` already has the only using `ITradingStore` needs. The workers are the files that omit that using and fully-qualify the type instead.**

---

## 1. Method

Read in full (product source, no edits):

- `D:\Prop\apps\api\Program.cs` (95 lines, 4658 bytes)
- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\obj\Debug\net8.0\TraderIntelligence.Api.GlobalUsings.g.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`, `D:\Prop\apps\fix-worker\Program.cs`, `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (port)
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` (confirm `ITradingStore` is **not** here)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`, `DependencyInjection.cs`, `Persistence\EfTradingStore.cs`
- `D:\Prop\Directory.Build.props` (`ImplicitUsings` on for the tree)

Grep: `ITradingStore`, `using`, `CS0246` under `D:\Prop\apps`, `D:\Prop\src`, `D:\Prop\reports\swarm\20260818`.

SHA-256 of every file cited in §2. Existing Debug API DLL timestamp checked (compile artifact only). **Did not** `dotnet build` this pass. **Did not** edit `src/`, `apps/`, `tests/`.

---

## 2. Files hashed

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4277 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` |

`Program.cs` hash matches C04. Written 2026-08-18 13:22:04. `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.dll` and the matching `obj` copy are 32256 bytes, last write **13:22:26** (22 s later). That is an on-disk compile of this same host after the current usings landed. This pass did not re-run the compiler.

Same-day hash drift vs B06: B06 had `Program.cs` `13CF8003…` (4503 B). C04/C34 share `E914FA98…` (4658 B). The using block and the `ITradingStore` seed resolve are in the later file.

---

## 3. Where `ITradingStore` actually lives

One definition in the whole tree:

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

| Fact | Value |
|---|---|
| Namespace | `TraderIntelligence.Application.Ingestion` (file line 6) |
| Accessibility | `public` |
| File | same as `DealIngestionService` + `ReconstructionScoringService` |
| Also in `Application.Contracts`? | **No.** That file is DTOs + (elsewhere) connector ports. |
| Second type named `ITradingStore`? | **None** (grep of `D:\Prop\src` + `D:\Prop\apps`). No CS0104 risk. |
| Implementation | `TraderIntelligence.Infrastructure.Persistence.EfTradingStore : ITradingStore` |
| DI | `services.AddScoped<ITradingStore, EfTradingStore>()` in `AddTraderIntelligence` |

Required using for an **unqualified** `ITradingStore` in any other compilation unit:

```csharp
using TraderIntelligence.Application.Ingestion;
```

Equivalent that also works: `TraderIntelligence.Application.Ingestion.ITradingStore` (fully qualified; no using).

`using TraderIntelligence.Application.Contracts;` would **not** resolve it.

---

## 4. What `apps/api/Program.cs` actually imports

Entire using-directive block (lines 1–5):

```1:5:D:\Prop\apps\api\Program.cs
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;
```

Line 84 `using (var scope = app.Services.CreateScope())` is a **using statement**, not a directive. It does not affect name resolution.

### 4.1 Directive → types used in this file

| Line | Directive | Unqualified types it makes legal | Used? |
|---|---|---|---|
| 1 | `TraderIntelligence.Application.Dashboard` | `IDashboardQueries` (maps at 34, 54–62) | **Yes** |
| 2 | `TraderIntelligence.Application.Ingestion` | `DealIngestionService` (73), `ReconstructionScoringService` (73, 91), **`ITradingStore` (90)** | **Yes — this is the ITradingStore using** |
| 3 | `TraderIntelligence.Infrastructure` | `AddTraderIntelligence` (9) | **Yes** |
| 4 | `TraderIntelligence.Infrastructure.Persistence` | `TraderDbContext` (48, 63, 86) | **Yes** |
| 5 | `TraderIntelligence.Infrastructure.Seeding` | `DemoSeeder` (88) | **Yes** |

**Zero unused usings. Zero missing usings for `ITradingStore`.**

The same line-2 directive is also required for the `/api/ops/resync` parameters (`DealIngestionService`, `ReconstructionScoringService`). Removing it would break **three** identifiers, not one.

### 4.2 The seed resolve (the only `ITradingStore` token in the file)

```84:93:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>(),
        CancellationToken.None);
}
```

| Token | How it resolves |
|---|---|
| `CreateScope` / `GetRequiredService` | Implicit Web-SDK global using `Microsoft.Extensions.DependencyInjection` (`GlobalUsings.g.cs` line 7) |
| `TraderDbContext` | explicit using line 4 |
| `DemoSeeder` | explicit using line 5 |
| **`ITradingStore`** | **explicit using line 2** |
| `ReconstructionScoringService` | explicit using line 2 |
| `CancellationToken` | implicit `System.Threading` |
| `EnsureCreatedAsync` | extension on `DatabaseFacade`; reached via `db.Database` (EF package referenced transitively through Infrastructure). Not a missing type name in this file. |

`ITradingStore` appears **once** in this compilation unit (line 90). It is unqualified. The matching using is present.

### 4.3 Implicit / generated usings (do not hide `ITradingStore`)

`TraderIntelligence.Api.csproj`: `ImplicitUsings` enable (also set in `D:\Prop\Directory.Build.props`). Generated file:

`D:\Prop\apps\api\obj\Debug\net8.0\TraderIntelligence.Api.GlobalUsings.g.cs`

Framework only: `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`, `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Net.Http`, `System.Net.Http.Json`, `System.Threading`, `System.Threading.Tasks`.

**No** `TraderIntelligence.*` global using. Product types are not pulled in by implicit usings. `ITradingStore` depends on the handwritten line-2 directive (or an FQN). That handwritten directive exists.

Project refs on the API csproj (`Domain`, `Application`, `Infrastructure`) make the type **referenceable**. They do not import namespaces.

---

## 5. Full identifier map (every product type in `Program.cs`)

| Identifier | Lines | Namespace | Resolution |
|---|---|---|---|
| `AddTraderIntelligence` | 9 | `TraderIntelligence.Infrastructure` | using L3 |
| `JsonStringEnumConverter` | 12 | `System.Text.Json.Serialization` | **FQN** (no using) |
| `IDashboardQueries` | 34, 54–62 | `TraderIntelligence.Application.Dashboard` | using L1 |
| `TraderDbContext` | 48, 63, 86 | `TraderIntelligence.Infrastructure.Persistence` | using L4 |
| `EntityFrameworkQueryableExtensions` | 50, 68 | `Microsoft.EntityFrameworkCore` | **FQN** (no using) |
| `DealIngestionService` | 73 | `TraderIntelligence.Application.Ingestion` | using L2 |
| `ReconstructionScoringService` | 73, 91 | `TraderIntelligence.Application.Ingestion` | using L2 |
| **`ITradingStore`** | **90** | **`TraderIntelligence.Application.Ingestion`** | **using L2** |
| `DemoSeeder` | 88 | `TraderIntelligence.Infrastructure.Seeding` | using L5 |

Framework types (`WebApplication`, `Results`, `DateTimeOffset`, `Dictionary`, `CancellationToken`, LINQ) come from implicit usings. Not in scope for this ask.

---

## 6. Adjacent “missing usings” that are **not** `ITradingStore`

These are convenience gaps. They do **not** produce CS0246 because the file fully qualifies the names. Do not mis-report them as the assigned defect.

| Missing convenience using | Workaround in file | Severity for this ask |
|---|---|---|
| `using Microsoft.EntityFrameworkCore;` | `Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync` / `ToListAsync` (lines 50, 68–69) | Style only. `AsQueryable` / `Where` / `OrderByDescending` / `Take` still work via implicit `System.Linq`. |
| `using System.Text.Json.Serialization;` | `new System.Text.Json.Serialization.JsonStringEnumConverter()` (line 12) | Style only. |

No missing using for:

- `IDashboardQueries` (line 1 present)
- `DealIngestionService` / `ReconstructionScoringService` (line 2 present)
- `TraderDbContext` / `DemoSeeder` / `AddTraderIntelligence` (lines 3–5 present)

A06’s claim that API project refs are “never used in `Program.cs`” is **stale**. This host uses Application + Infrastructure types at compile time.

---

## 7. Contrast: workers **omit** the using and FQN the type

`apps/mt5-worker/Program.cs` and `apps/fix-worker/Program.cs` do **not** have `using TraderIntelligence.Application.Ingestion;`. Seed looks like:

```15:19:D:\Prop\apps\mt5-worker\Program.cs
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ReconstructionScoringService>(),
        CancellationToken.None);
```

That is a **valid** alternative, not a missing type. Same namespace, same public interface.

| Host | `using …Ingestion`? | How `ITradingStore` is written |
|---|---|---|
| `apps/api/Program.cs` | **Yes** (L2) | Unqualified `ITradingStore` |
| `apps/mt5-worker/Program.cs` | **No** | FQN |
| `apps/fix-worker/Program.cs` | **No** | FQN |
| `apps/mt5-worker/Worker.cs` | **Yes** (L1) | Does not name `ITradingStore` (resolves `DealIngestionService` / `ReconstructionScoringService`) |
| `DemoSeeder.cs` | **Yes** (L3) | Parameter type `ITradingStore` |
| `EfTradingStore.cs` | **Yes** (L3) | `: ITradingStore` |
| `DependencyInjection.cs` | **Yes** (L6) | `AddScoped<ITradingStore, EfTradingStore>()` |

If the swarm prompt was triggered by seeing the worker FQNs and assuming the API was the same: **it is not.** The API is the host that already added the using.

Style note (not a defect, not applied): the three hosts could share one shape — either all `using …Ingestion` + short names, or all FQN. Today API = short, workers = FQN. Both compile.

---

## 8. Classification

| Item | Class |
|---|---|
| `using TraderIntelligence.Application.Ingestion` on API `Program.cs` | **EXISTS_AND_GOOD** |
| Unqualified `ITradingStore` at seed (line 90) | **EXISTS_AND_GOOD** |
| Extra / wrong using (`Application.Contracts`) for this type | **GONE** (correctly absent) |
| Second `ITradingStore` type | **GONE** |
| Worker `Program.cs` using for `ITradingStore` | **MISSING** (compensated by FQN — not this SUT) |
| `using Microsoft.EntityFrameworkCore` on API host | **MISSING** (FQN workaround; out of ask) |
| CS0246 on `ITradingStore` in API host | **ABSENT** |

**Assigned check: PASS.** There is no missing `ITradingStore` using to add.

### Recommended fix (not applied — product source frozen)

None for the asked identifier.

If a later editor wants consistency only:

1. Leave API as-is, **or**
2. Add `using TraderIntelligence.Application.Ingestion;` to both worker `Program.cs` files and shorten the two FQNs (optional hygiene).

Do **not** add a dummy `using ITradingStore = …` alias. Do **not** move the interface into `Application.Contracts` as a “using fix.”

---

## 9. What this review does **not** claim

- Did not re-run `dotnet build`. The existing Debug API DLL timestamp is consistent with a successful compile of this hash; it is not a fresh measurement from this agent.
- Did not declare the API host complete vs A63 `/api/v1`, auth, sanitizer, or SignalR (C04).
- Did not re-open C05’s split-composition finding (seeder `new`s a second connector graph). That is not a using bug.
- Did not treat A06 “project refs unused” as current.

---

## 10. Bottom line

| Ask | Measured answer |
|---|---|
| Missing usings for `ITradingStore` in `apps/api/Program.cs`? | **No.** Line 2 `using TraderIntelligence.Application.Ingestion;` is present and is the correct namespace. Line 90 `GetRequiredService<ITradingStore>()` is legal. |
| Anything to add? | **Nothing** for this type. |

Product source was not modified.
