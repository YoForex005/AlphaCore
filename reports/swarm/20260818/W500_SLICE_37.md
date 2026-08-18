# W500_SLICE_37

- **slot:** 37
- **file:** `D:/Prop/apps/api/TraderIntelligence.Api.csproj`
- **angle:** positions limited to first 200 accounts
- **read:** full file (21 lines) via `read_file`; grep on this file for `200|position|account|Take\(|limit` returned **no matches**
- **verdict:** PASS

## Evidence quotes

`TraderIntelligence.Api.csproj` is an SDK-style web project file only. It declares three project references, three NuGet packages, and three compile properties. It does not enumerate broker logins, does not call `GetPositionsAsync`, and does not apply `Take(200)` or any other account cap.

Full file (21/21 lines):

```1:21:D:/Prop/apps/api/TraderIntelligence.Api.csproj
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="8.0.4" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

This file does not contain:

- `Take(200)` / `Take(` / `Skip` / page size / `limit`
- `GetPositionsAsync` / `ReplacePositionsAsync` / `Mt5Position`
- any `foreach` over accounts or groups
- any numeric `200` literal
- any MSBuild item/property that could bound account count or position sync

The only nearby `200` in the API host is **not** an account-window on positions. `apps/api/Program.cs` caps **reconstructed trade rows** on `GET /api/trades` (newest first). That is a trade-list page cap, not a first-200-accounts position replace:

```63:70:D:/Prop/apps/api/Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
```

`/api/ops/resync` in the same host calls `DealIngestionService.SyncBrokerAsync` with no extra account window of its own:

```73:78:D:/Prop/apps/api/Program.cs
app.MapPost("/api/ops/resync", async (DealIngestionService ingestion, ReconstructionScoringService scoring, CancellationToken ct) =>
{
    var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    var to = DateTimeOffset.UtcNow;
    var a = await ingestion.SyncBrokerAsync("ACHIEVER", from, to, ct);
    var s = await ingestion.SyncBrokerAsync("STARWAVEFX", from, to, ct);
```

The first-200-accounts position cap lives **outside** this slice, in ingestion (not the API csproj):

```78:82:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        foreach (var account in accounts.Take(200))
        {
            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }
```

That `Take(200)` is out of this slice’s file. Slot 37 asked whether **this** csproj limits position sync to the first 200 accounts. It does not.

## No-loss implication

A `.csproj` cannot drop, truncate, or skip a position book. It only names compile-time references and TFM flags. It cannot send orders, cannot size positions, and cannot leave accounts 201+ with a stale local book — because it never reads or writes positions. The live completeness risk of `accounts.Take(200)` (missed position replace for logins beyond 200 if any consumer treats `mt5_positions_current` as the complete open-risk book) is owned by `DealIngestionService`, not by `TraderIntelligence.Api.csproj`. The API host’s own `Take(200)` is a reconstructed-trade explorer cap, not a live-position account window. Slot 37 therefore has **no position-cap capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (21/21 lines); the angle (positions limited to first 200 accounts) is absent by construction, not by skipped review.
