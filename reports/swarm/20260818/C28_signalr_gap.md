# C28 — API SignalR package present; no hub mapped

| Field | Value |
|---|---|
| Agent | C28 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C28_signalr_gap.md` |
| Ask | Does the API have a SignalR package? Confirm **no hub is mapped**. Write this report. |
| Product source modified | **No.** Report / index / swarm-log only. |
| Method | Full read of `D:\Prop\apps\api\TraderIntelligence.Api.csproj` and `Program.cs`. Grep product `*.cs` / `*.csproj` for `AddSignalR`, `MapHub`, `IHubContext`, `: Hub`, `OpsHub`, `Microsoft.AspNetCore.SignalR`. Confirm no `Hubs/` / `Realtime/` folders. Hash API + web client files. Cross-read web stub, workers, restore graph, A26 §7 / A63 §6 / A97. **API process was not launched.** No negotiate / WebSocket probe. |
| Law | Architecture v2 §5 (React + ASP.NET Core + SignalR), A26 §7, A63 §6, A97 (binding hub contract). Hub is **recommended**, not a §69 first-useful gate. |
| Relates | A06 (stale host = weatherforecast-only), B06, C04 (current REST host, same hashes), A97 (SignalR events; §0 host note is stale), B10 / B30 (web `/hubs/dashboard`) |
| Supersedes | A06 / A97 §0 claim that `Program.cs` is still weatherforecast-only. **Does not** supersede A97 as the hub-event contract. |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Headline answers (measured)

| Question | Answer | Class | Evidence |
|---|---|---|---|
| Does `apps/api` reference a SignalR **NuGet** package? | **Yes.** Direct `PackageReference` `Microsoft.AspNetCore.SignalR.Common` **8.0.4**. Restored and copied to `bin`. | `EXISTS_NEEDS_REFACTOR` | §2 |
| Is that package a hub **host**? | **No.** Common is protocol / shared types. Host APIs (`AddSignalR`, `MapHub`, `Hub`) come from the Web SDK shared framework `Microsoft.AspNetCore.App`, which this project already has. Common is **not used** by any C# file. | wrong package for a host | §2.1 |
| Is any hub mapped? | **Confirmed: no.** Zero `AddSignalR`, zero `MapHub`, zero `UseWebSockets`, zero `Hub` subclass, no `Hubs/` folder. | `MISSING` | §3 |
| Binding path `/hubs/ops` (`OpsHub`)? | **MISSING** | `MISSING` | A26 §7, A63 §6, A97 §2.1 |
| Stub path `/hubs/dashboard` on the server? | **Not mapped** (client still targets it) | client `EXISTS_NEEDS_REFACTOR` | §4 |
| Workers host SignalR? | **No.** Generic `Host` workers; no `MapHub`. Correct per A97. | `EXISTS_AND_GOOD` (absence) | §5 |

Honest one-liner: **the API ships an unused `SignalR.Common` 8.0.4 reference and maps no hub. The React client still dials `/hubs/dashboard` and swallows the failure. Live tiles do not exist.**

Do not treat the package, the web stub, or A97 as an implemented hub.

---

## 1. Files hashed (this pass)

| Path | Bytes | SHA-256 | LastWriteUtc |
|---|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | `2026-08-18T07:52:04.8133238Z` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | `2026-08-18T07:25:15.5522783Z` |
| `D:\Prop\apps\api\appsettings.json` | 431 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` | `2026-08-18T07:45:01.3628245Z` |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | `2026-08-18T07:38:02.8638868Z` |
| `D:\Prop\apps\web\package.json` | 739 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` | `2026-08-18T07:36:29.6945552Z` |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | `148F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | `2026-08-18T07:50:38.3009629Z` |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | `2026-08-18T07:45:01.3618241Z` |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `2026-08-18T07:45:01.3638263Z` |
| `D:\Prop\Directory.Build.props` | 269 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` | `2026-08-18T07:35:12.1515604Z` |

`Program.cs` / `.csproj` hashes **match C04**. Host did not grow a hub between C04 and this pass.

`apps/api` non-`bin`/`obj` surface is still: `Program.cs`, the `.csproj`, two `appsettings*.json`, `Properties/launchSettings.json`, `TraderIntelligence.Api.http`. **No** `Hubs/`, **no** `Realtime/`, **no** second `.cs` file.

Directories confirmed absent: `D:\Prop\apps\api\Hubs`, `D:\Prop\apps\api\Realtime`, `D:\Prop\src\Application\Realtime`.

---

## 2. SignalR package — present

Exact csproj (`D:\Prop\apps\api\TraderIntelligence.Api.csproj`):

```xml
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
  …
</Project>
```

| Check | Result |
|---|---|
| Direct `PackageReference` | **Yes** — line 10, `Microsoft.AspNetCore.SignalR.Common` Version `8.0.4` |
| Other SignalR PackageReference in the solution | **None.** Grep of every product `*.csproj` found this one line only. |
| `Microsoft.AspNetCore.SignalR` (host package) | **Not referenced.** Not needed on a Web SDK project; host types live in `Microsoft.AspNetCore.App`. |
| Central package management | **Off.** `Directory.Build.props` has no `ManagePackageVersionsCentrally` and no SignalR pin. A102’s proposed CPM pin is **not applied**. |
| Restore graph | `obj\TraderIntelligence.Api.csproj.nuget.dgspec.json` lists `"Microsoft.AspNetCore.SignalR.Common": { "version": "[8.0.4, )" }`. `obj\project.assets.json` target `net8.0` includes `Microsoft.AspNetCore.SignalR.Common/8.0.4` (`lib/net8.0/Microsoft.AspNetCore.SignalR.Common.dll`) plus its deps `Microsoft.AspNetCore.Connections.Abstractions/8.0.4` and `Microsoft.Extensions.Options/8.0.2`. |
| Runtime copy | `apps/api/bin/Debug/net8.0/Microsoft.AspNetCore.SignalR.Common.dll` exists. `TraderIntelligence.Api.deps.json` lists the package as a **direct** dependency of `TraderIntelligence.Api/1.0.0` (fileVersion `8.0.424.17014`). |
| Used by product C# | **Zero** `using Microsoft.AspNetCore.SignalR` (or any SignalR type) under `apps/api`, `apps/*-worker`, `src/**`. Dead reference. |

### 2.1 Why Common is the wrong “we have SignalR” signal

| Package / framework | What it is | What this repo has |
|---|---|---|
| `Microsoft.NET.Sdk.Web` → `Microsoft.AspNetCore.App` | Shared framework. Contains `Hub`, `AddSignalR()`, `MapHub<T>()`. **This is the host.** | Already implied by the Web SDK. Unused for SignalR. |
| `Microsoft.AspNetCore.SignalR.Common` 8.0.4 | Shared protocol / connection primitives. **Not** a hub host. | **Referenced, unused.** |
| `Microsoft.AspNetCore.SignalR.Client` | .NET client. | **Not** on the API. Browser uses npm `@microsoft/signalr` `^8.0.0` (lockfile `8.0.29`). |
| `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` | Binary protocol. | **Not** referenced. A97 v1 forbids MessagePack anyway. |

A06 / A97 / B06 already called this out. This pass reconfirms on the current hashes: **package yes, host no.**

---

## 3. No hub mapped (confirmed)

`D:\Prop\apps\api\Program.cs` is a 95-line minimal API (same 4658-byte blob C04 hashed). Composition, in order:

1. `AddTraderIntelligence(builder.Configuration)` — EF / dashboard queries / ingestion DI. Not SignalR.
2. `ConfigureHttpJsonOptions` + `JsonStringEnumConverter`.
3. `AddEndpointsApiExplorer` + `AddSwaggerGen`.
4. CORS default: `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`.
5. `UseCors()`. Development: `UseSwagger()` only (no UI).
6. Fifteen anonymous HTTP maps (table below).
7. Startup scope: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`.
8. `app.Run()`.

**Token counts in this file (case-sensitive):**

| Token | Hits |
|---|---:|
| `AddSignalR` | **0** |
| `MapHub` | **0** |
| `UseSignalR` | **0** |
| `UseWebSockets` | **0** |
| `IHubContext` | **0** |
| `Hub` | **0** |
| `SignalR` | **0** |
| `MapGet` | **14** |
| `MapPost` | **1** |
| `MapControllers` | **0** |

Live maps (complete). None is a hub.

| # | Method | Path |
|---|---|---|
| 1 | `GET` | `/health` |
| 2 | `GET` | `/api/health` |
| 3 | `GET` | `/api/risk/status` |
| 4 | `GET` | `/api/reconciliation/status` |
| 5 | `GET` | `/api/settings` |
| 6 | `GET` | `/ready` |
| 7 | `GET` | `/api/overview` |
| 8 | `GET` | `/api/brokers` |
| 9 | `GET` | `/api/groups` |
| 10 | `GET` | `/api/traders` |
| 11 | `GET` | `/api/traders/{broker}/{login}` |
| 12 | `GET` | `/api/fix/sessions` |
| 13 | `GET` | `/api/risk` |
| 14 | `GET` | `/api/trades` |
| 15 | `POST` | `/api/ops/resync` |

Repo-wide product C# / csproj grep for `AddSignalR|MapHub|UseSignalR|IHubContext|HubConnectionContext|: Hub|class OpsHub` → **no matches**. The only product-source `hubs/` string is the **web client** URL in `signalr.ts` (wrong path; see §4).

Therefore a running API would expose **no** negotiate endpoint at `/hubs/ops` or `/hubs/dashboard`. Expected HTTP on those paths: **404** (unmapped). This was **not** probed live; the conclusion is from the route table as read.

### 3.1 Stale swarm notes (keep on disk)

| Note | What it said | This pass |
|---|---|---|
| A06 / A97 §0 | `Program.cs` is still weatherforecast-only | **Stale.** Weather route is gone (C04). Host is a demo REST BFF. SignalR conclusion is **unchanged**. |
| A63 “0 of those routes exist” | first-useful `/api/v1/**` + `/hubs/ops` | Unversioned `/api/*` GETs now exist; **`/hubs/ops` still 0**. |
| A102 “keep SignalR.Common 8.0.4” | CPM pin | Pin is a **plan**. Do not read it as “hub implemented.” Hosting still does not need Common. |

---

## 4. Web client — package yes, path wrong, no consumers

| Item | Measured |
|---|---|
| npm | `@microsoft/signalr` `^8.0.0` in `apps/web/package.json`; lockfile resolves `8.0.29` |
| File | `D:\Prop\apps\web\src\api\signalr.ts` (899 B) |
| URL | `` `${VITE_API_URL \|\| 'http://localhost:5000'}/hubs/dashboard` `` |
| Binding | A26 / A63 / A97 = **`/hubs/ops`**. Stub path is **forbidden** (A63 §6: “Do not implement `/hubs/dashboard`”). |
| Auth | None. No `accessTokenFactory`, no `Authorization` header. |
| Start | `DashboardLayout` `useEffect` → `startConnection()`. Failure is `console.warn` and swallowed. |
| `onEvent` | Defined. **Zero** importers besides its own file. No page invalidates TanStack Query from the hub. |

Until the API maps a hub **and** the client is retargeted, the shell connection is a guaranteed fail that is hidden from the UI.

---

## 5. Workers do not host SignalR (correct)

`apps/mt5-worker` and `apps/fix-worker` are `Host.CreateApplicationBuilder` generic hosts. They register `AddTraderIntelligence` + one `BackgroundService`. They are **not** Web hosts. Grep of both `Program.cs` / `Worker.cs` / csproj: no SignalR package, no `MapHub`.

A97 §2.1: workers publish to Redis / `system_events`; they **never** `MapHub`. Current absence matches the contract. Redis `ops:events` relay is still **MISSING** (A41 / A97) — out of this file’s ask, recorded so “no hub” is not confused with “workers should have one.”

---

## 6. Gap vs binding contract

| Contract item | Binding | Today | Class |
|---|---|---|---|
| Hub class `OpsHub : Hub` | A97 §2.1 | no type | `MISSING` |
| `builder.Services.AddSignalR()` | A30 I6 / A97 | not called | `MISSING` |
| `app.MapHub<OpsHub>("/hubs/ops")` | A26 §7 | not called | `MISSING` |
| JWT on negotiate | A51 / A97 | no auth on host at all (C04) | `MISSING` |
| JSON protocol, MessagePack off | A97 | N/A (no hub) | — |
| Events `ops.header`, `overview.updated`, `fix.session`, `quote.xauusd`, `risk.state`, `trader.score`, … | A26 §7 / A63 §6.1 / A97 §§5–8 | none | `MISSING` |
| Mutations over hub (`SendOrder`, flatten, `SetPassword`) | **Forbidden** | none exist | `EXISTS_AND_GOOD` (absence) |
| Payload sanitizer | A26 §3 / A76 | none | `MISSING` (REST is safe-by-absence, C04) |
| Redis `ops:events` subscriber in API | A41 / A97 | none | `MISSING` |
| Client `/hubs/ops` | A63 §6 | `/hubs/dashboard` | `EXISTS_NEEDS_REFACTOR` |
| `SignalR.Common` as proof of feature | — | dead PackageReference | `EXISTS_NEEDS_REFACTOR` |

§69 first-useful paint may poll REST (A06 §4.14, A63 §6, A97). The hub is **not** a go-live gate. It is still a real gap for live header / quote tiles.

---

## 7. Findings

| ID | Sev | Finding |
|---|---|---|
| C28-01 | **INFO** | API **has** a SignalR NuGet: `Microsoft.AspNetCore.SignalR.Common` 8.0.4. Restored. Copied to `bin`. |
| C28-02 | **PASS (gap confirm)** | **No hub is mapped.** No `AddSignalR`, no `MapHub`, no `Hub` subclass, no `/hubs/ops`, no `/hubs/dashboard`. |
| C28-03 | **MED** | Common is the **wrong** package to treat as a hub host. Web SDK already includes host types. The reference is unused. Do not add a second host package on top of the shared framework. |
| C28-04 | **MED** | Web stub dials `/hubs/dashboard` and swallows start failure. `onEvent` has no consumers. Retarget to `/hubs/ops` in the same coding wave that maps the hub (A97 checklist #8). |
| C28-05 | **INFO** | Workers correctly do **not** host SignalR. |
| C28-06 | **INFO** | A97 §0 “weatherforecast host” is stale (C04). SignalR measured state in A97 §0 is still correct. |

---

## 8. What this file does **not** authorize

- Implementing `OpsHub` from this report (coding task; follow A97, not this gap note).
- Adding `Microsoft.AspNetCore.SignalR` as a redundant PackageReference.
- Mapping `/hubs/dashboard`.
- Hosting a hub on either worker.
- Enabling MessagePack.
- Sending secrets / FIX passwords / `SenderSubId` on any future frame (A26 §3, C04).
- Claiming live tiles, negotiate 200, or “SignalR done” because Common is in the csproj.

---

## 9. Checklist (this pass)

- [x] API SignalR package identified (`SignalR.Common` 8.0.4).
- [x] Confirmed **no** hub mapped.
- [x] Confirmed no `Hub` type / `Hubs/` folder in product source.
- [x] Workers have no `MapHub`.
- [x] Web client path mismatch recorded.
- [x] Product source **not** modified.
