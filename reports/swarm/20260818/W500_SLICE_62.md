# W500_SLICE_62

- **slot:** 62
- **file:** `D:/Prop/apps/api/TraderIntelligence.Api.csproj`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (23 lines) via `read_file` after a mid-review rewrite (first pass was 21 lines, `net8.0`, no Mt5/Fix refs); grep on `apps/api` for `UserLogins|GetAccounts|ListLogins|/api/traders|MT5API|MetaQuotes`; followed compile items `Program.cs`, `EfDashboardQueries.GetTradersAsync`, `NativeMt5BrokerConnector` (`UserLogins` + `Factory.Initialize`), `project.assets.json` Mt5 node, `bin/Debug/net8.0` and empty `bin/Debug/net8.0-windows`
- **verdict:** FAIL

## Binding law (this angle)

Architecture v2 §7: after Manager Connect, **enumerate groups then enumerate accounts** (associate every login with broker + group). `demo\Maxmaster` is not the book.

Architecture v2 §9 / A39: plan `MT5_GROUP_*` mappings are labels, not the fetch filter. Account census is Manager `UserLogins` / `UserRequestArray` / `UserGetByGroup` over **all** Manager-visible groups.

A105 / §5: local Manager mode requires `MT5APIManager64.dll` + `MetaQuotes.MT5ManagerAPI64.dll` + `MetaQuotes.MT5CommonAPI64.dll` **beside the consuming exe**. `SMTManagerAPIFactory.Initialize(dllDir)` uses `AppContext.BaseDirectory`.

A005 / §73.B: `GET /api/traders` as “list every live Manager login” is **MISSING** if the driver set is `TraderScores` rather than Manager logins / `Mt5Accounts`.

## Evidence quotes

Assigned file is SDK Web MSBuild only. It names references and TFM. It does **not** declare the Manager runtime binaries, a login enumerator, or a “copy all UserLogins” target:

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

</Project>
```

This file does **not** contain:

- `UserLogins` / `UserRequestArray` / `GetAccountsAsync` / `GetTradersAsync`
- `<None>` / `<Content>` / `<Reference>` for `MT5APIManager64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, or `MetaQuotes.MT5CommonAPI64.dll`
- any `CopyToOutputDirectory` / post-build copy of vendor `mt5-sdk\...\Libs`
- a `Take`/`MaxLogins` cap (no fetch logic at all)

`net8.0-windows` + `x64` + a **direct** `TraderIntelligence.Mt5` `ProjectReference` is the right *shape* for a Windows Manager host. It is not a complete ALL-logins guarantee.

### 1. Manager factory cannot see DLLs this csproj does not ship

Live connect initializes the official factory from the API process base directory (`LiveMt5Registration` sets `NativeDllDirectory = AppContext.BaseDirectory`):

```69:75:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
            if (init != MTRetCode.MT_RET_OK && init != MTRetCode.MT_RET_ERR_DUPLICATE)
            {
                LastError = Describe(init, "Factory.Initialize");
                throw new InvalidOperationException(LastError);
            }
```

Account census (the ALL-logins walk) is behind that connect. Empty `UserRequestArray` falls back to `UserLogins`:

```223:232:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            var req = _manager.UserRequestArray(gname, users);
            if (req != MTRetCode.MT_RET_OK && req != MTRetCode.MT_RET_OK_NONE && req != MTRetCode.MT_RET_ERR_NOTFOUND)
                _manager.UserGetByGroup(gname, users);

            if (users.Total() == 0)
            {
                var loginRes = MTRetCode.MT_RET_OK;
                var logins = _manager.UserLogins(gname, out loginRes);
                if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                    _manager.UserRequestByLogins(logins, users);
            }
```

Measured API outputs (read-only listing; no rebuild in this slice):

| Output | State vs Manager binaries |
|---|---|
| `D:\Prop\apps\api\bin\Debug\net8.0\` (only populated host) | Has `TraderIntelligence.Mt5.dll`. **Zero** `MT5APIManager64.dll` / `MetaQuotes.MT5*.dll`. `TraderIntelligence.Api.deps.json` lists Domain/Application/Infrastructure only (pre-rewrite graph). |
| `D:\Prop\apps\api\bin\Debug\net8.0-windows\` | **Empty** after the TFM change. No rebuilt host, no vendor Libs. |

`obj\project.assets.json` `TraderIntelligence.Mt5/1.0.0` is assembly-only (`bin/placeholder/TraderIntelligence.Mt5.dll`). No `contentFiles` for the three vendor Libs. Restore does not substitute for the missing csproj copy items.

Without those files next to `TraderIntelligence.Api.exe`, `Factory.Initialize` throws, `ConnectAsync` never completes, `GetAccountsAsync`/`UserLogins` never run, and the in-process `LiveIngestHostedService` (registered by `AddTraderIntelligence`) cannot upsert Manager logins. That is a **csproj-level** ALL-traders fetch failure: this project is the process that must load the Manager DLL.

Mt5.csproj *does* mark the trio `CopyToOutputDirectory=PreserveNewest`. Transitive flow into this Web host is **unverified** (windows output empty; last successful output has 0 files). This file does not pin the copy itself.

### 2. The host this csproj compiles does not list Manager logins

SDK Web compiles `Program.cs` as the executable surface. `GET /api/traders` is a DB score query, not Manager `UserLogins` and not `Mt5Accounts`:

```95:96:D:/Prop/apps/api/Program.cs
app.MapGet("/api/traders", (IDashboardQueries q, string? broker, string? state, CancellationToken ct) =>
    q.GetTradersAsync(broker, state, ct));
```

```74:90:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        // ...
        var mapped = new List<TraderRowDto>();
        foreach (var s in scores)
        {
            if (!brokers.TryGetValue(s.BrokerId, out var b))
                continue;
            var account = accounts.FirstOrDefault(a => a.BrokerId == s.BrokerId && a.Login == s.Login);
```

`Mt5Accounts` is join payload (`GroupName` only). Unscored Manager logins never become trader rows. There is no `GET` that returns `store.ListLoginsAsync` or live `UserLogins`.

`POST /api/ops/resync` now scores **every stored** login after `SyncCatalogAsync` + `SyncBrokerAsync` (no longer the four demo ids `10001/10002/10003/99001`). That is the right *list* **if** the connector actually returned the full Manager set. This csproj does not ensure the Manager DLL is present, and `/api/traders` still will not show an account that ingest upserted but scoring skipped/failed.

`GET /ready` counts `Mt5Accounts` (census size) but does not fetch or return logins.

`TraderIntelligence.Api.http` has `GET /api/traders` only — no resync, no login dump.

### 3. What is *not* a plan-map filter in this file

The csproj does not mention `MT5_GROUP_*`. It does not compile a group allow-list. Residual §9 risk is elsewhere (`NativeMt5BrokerConnector.GetGroupsCore` `GroupNext` continue-on-fail; one-shot `LiveIngestHostedService`). Those are not this XML.

Serilog / SignalR.Common package refs are unused for this angle (not a login cap).

## No-loss implication

This host is the process that `AddTraderIntelligence` uses for live Manager connect + catalog. If the three vendor DLLs are absent from `AppContext.BaseDirectory`, **no** Manager login is fetched. `/api/traders` then shows only leftover `TraderScores` (or empty). Operators see a healthy web process and a short/empty leaderboard while the live Achiever/Starwave books are invisible.

Even after a successful ingest, the compiled `/api/traders` path is score-driven: a Manager login that is in `Mt5Accounts` but not in `TraderScores` is omitted from the book used for ranking, promotion, and copy selection.

Capital-relevant effects are **omission**, not a send:

- Source logins the Manager can see never reach reconstruction / scoring / kill-switch / copy.
- Partial or failed fetch looks like “few traders” rather than “Manager census failed.”
- This `.csproj` cannot emit `NewOrderSingle`; it can starve the live source universe so copy/risk decide on a subset.

Residual: `net8.0-windows` + x64 + Mt5 project reference is necessary and now present. It is not sufficient without shipping `MT5APIManager64.dll` next to the API exe and exposing all Manager logins (not only scores) on the traders surface.
