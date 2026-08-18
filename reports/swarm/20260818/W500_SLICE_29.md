# W500_SLICE_29

- **slot:** 29
- **file:** `D:/Prop/apps/api/Program.cs`
- **angle:** env file not loaded before `WebApplication.CreateBuilder`
- **read:** full file (96 lines) via `read_file`; grep on `D:\Prop\apps\api` for `EnvFile|DotNetEnv|\.env` returned no matches; grep on `D:\Prop\apps` `*.cs` for `CreateBuilder|CreateApplicationBuilder|EnvFile` shows only the three host builders, none calling `EnvFile`
- **verdict:** FAIL

## Evidence quotes

Assigned file was read in full. The first executable statement is `WebApplication.CreateBuilder(args)`. Nothing before that line (and nothing after it) loads a dotenv / `.env` file into the process environment.

```1:19:D:/Prop/apps/api/Program.cs
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();
```

Observed absences in this file (and under `D:\Prop\apps\api`):

- no `EnvFile.Load(...)` (loader exists at `D:\Prop\src\Mt5\Env\EnvFile.cs` but has **zero callers** in product C#)
- no `DotNetEnv` / `dotenv` package or type
- no `File.ReadAllLines` / `SetEnvironmentVariable` of a `.env` path
- no `AddEnvironmentVariables` after a file load (CreateBuilder’s stock process-env provider is not a file loader)
- API csproj references Domain / Application / Infrastructure only — **not** `src\Mt5`, so this host cannot even compile a call to `TraderIntelligence.Mt5.Env.EnvFile` without a new project reference

Sibling hosts match the same order defect (`Host.CreateApplicationBuilder` is line 6 in both workers; no dotenv first):

- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`

`WebApplication.CreateBuilder` still binds `appsettings*.json`, Development user-secrets (if present), **already-set** process environment variables, and command-line args. It does **not** open repo-root `.env` or `apps/api/.env`. A file sitting at `D:\Prop\.env` is therefore invisible to this entrypoint.

## No-loss implication

This FAIL is a **configuration-order gap**, not a live send path. `Program.cs` maps dashboard/health GETs, a demo `/api/ops/resync`, and `DemoSeeder` — it does not emit FIX `NewOrderSingle`, Manager `OrderSend`, or any other capital-reducing call.

Because `.env` is **not** loaded before (or after) `CreateBuilder`:

- a filled operator `.env` cannot inject `MT5_PASSWORD`, `CTRADER_FIX_PASSWORD`, or `REAL_COPY_EXECUTION_ENABLED` into this process via dotenv
- README “secrets stay in `.env`” is **not wired** for the API host; live copy cannot be armed by dropping a file next to the repo
- fail-closed for dotenv: unread live flags/passwords do not reach `IConfiguration`

Capital is not at risk from this omission. The opposite is true: unread `.env` cannot silently start live copy. Process-level env vars already present before launch would still be seen by CreateBuilder; that path is outside this file.

Empty-PASS was **not** used: the assigned file was fully read; the angle is **present**.
