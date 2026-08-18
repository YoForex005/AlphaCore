# W500_SLICE_87

- **slot:** 87
- **file:** `D:/Prop/apps/api/TraderIntelligence.Api.csproj`
- **angle:** positions limited to first 200 accounts
- **read:** full file (25 lines) via `read_file`; grep on this file for `200|position|account|Take` returned **no matches**; same-project grep of `D:/Prop/apps/api` for `Take(200)` / `GetPositions` / `positions`; workspace grep for `Take(200)|accounts.Take` under `D:/Prop/src` (0 hits)
- **verdict:** PASS

## Evidence quotes

`TraderIntelligence.Api.csproj` is a 25-line SDK web project file. It lists project references, NuGet packages, and compile properties only. It does not compile C#, does not enumerate broker logins, and does not snapshot positions.

```1:24:D:/Prop/apps/api/TraderIntelligence.Api.csproj
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Mt5\TraderIntelligence.Mt5.csproj" />
    <ProjectReference Include="..\..\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="8.0.4" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

This file does not contain:

- `Take(200)` / `Take(` / `Skip(` / page size / `limit`
- any numeric `200` literal
- `position`, `account`, `GetPositionsAsync`, `ReplacePositionsAsync`
- MSBuild `DefineConstants` or items that could encode a first-N-accounts policy

The only `200` in the same API project is **not** a positions-by-account cap. `GET /api/trades` pages reconstructed trade rows by `OpenedAt` (read-only explorer), independent of account rank:

```101:108:D:/Prop/apps/api/Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
});
```

That is a 200-row trade list, not “first 200 accounts get position snapshots.”

Current ingestion no longer applies `accounts.Take(200)`. Per-login position replace walks the full `accounts` list (or bulk `*` when the connector implements `IMt5BulkPositionReader`):

```81:92:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkPositionReader posBulk)
        {
            var positions = await posBulk.GetGroupPositionsAsync("*", ct);
            await _store.ReplaceBrokerPositionsAsync(brokerId, positions, ct);
        }
        else
        {
            foreach (var account in accounts)
            {
                var positions = await connector.GetPositionsAsync(account.Login, ct);
                await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
            }
        }
```

`D:/Prop/src` grep for `Take(200)|accounts.Take` returned **zero** hits. Older swarm notes (`A005`, `A007`, `W500_SLICE_17/27`) quoted a historical `foreach (var account in accounts.Take(200))` that is **not present** in the file on disk now.

API `/api/ops/resync` also scores **every** login from `store.ListLoginsAsync` with no `Take(200)`:

```131:137:D:/Prop/apps/api/Program.cs
        var logins = await store.ListLoginsAsync(brokerId, ct);
        var scored = 0;
        foreach (var login in logins)
        {
            await scoring.RebuildTraderAsync(code, login, ct);
            scored++;
        }
```

Slot 87 asked whether **this csproj** limits positions to the first 200 accounts. It does not.

## No-loss implication

An MSBuild project file cannot drop, truncate, or skip an open-position book. It cannot send orders, size positions, or leave accounts 201+ with a stale `mt5_positions_current` row, because it never reads or writes positions. Same-project `GET /api/trades` `Take(200)` is explorer pagination of reconstructed trades and cannot hide live exposure or change order routing. Current `DealIngestionService` iterates all accounts (or bulk `*`). Slot 87 therefore has **no first-200-accounts position cap** and **no capital-loss / hidden-book path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (25/25 lines); the angle (positions limited to first 200 accounts) is absent from the csproj by construction, not by skipped review.
