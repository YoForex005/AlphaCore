# W500_SLICE_79 — Program.cs env load vs CreateBuilder

- **slot:** 79
- **file:** `D:/Prop/apps/api/Program.cs`
- **angle:** env file not loaded before `WebApplication.CreateBuilder`
- **read:** full file (156/156 lines) via `read_file`; `grep` on `D:/Prop/apps/api` and `Program.cs` for `CreateBuilder|DotNetEnv|AddEnvironmentVariables|\.env|Load\(|LoadEnv|EnvFile`
- **supporting read:** `D:/Prop/src/Mt5/Env/EnvFile.cs` (42 lines) to confirm `FindAndLoad` writes process env before return
- **verdict:** **PASS**

## File

`D:/Prop/apps/api/Program.cs` is the ASP.NET Core host entry for Trader Intelligence API. It is the only place this angle can succeed or fail: dotenv must be applied to the **process environment** before `WebApplication.CreateBuilder(args)` so the default host configuration sources (and any later `AddEnvironmentVariables`) see file-backed keys.

## Angle

Assigned defect: a `.env` file is **not** loaded before `WebApplication.CreateBuilder`, so `IConfiguration` / `IHostEnvironment` / DI would miss keys that exist only on disk (`REAL_COPY_*`, MT5/FIX endpoints, risk flags). That would be a FAIL if `CreateBuilder` ran first (or if no loader ran at all).

Observed order on disk is the opposite of the defect.

## Evidence quotes

Usings include the lab dotenv helper. The first executable statement is `EnvFile.FindAndLoad()`, then `CreateBuilder`, then an explicit env-var configuration source:

```7:14:D:/Prop/apps/api/Program.cs
using TraderIntelligence.Mt5.Env;

var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`FindAndLoad` locates the first existing candidate (cwd / parents / `D:\Prop\.env`) and calls `Load`, which writes each `KEY=value` line into the **process** environment **before** returning the path. A missing file returns `null` and does not throw:

```5:20:D:/Prop/src/Mt5/Env/EnvFile.cs
    public static string? FindAndLoad()
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(cwd, ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", ".env")),
            @"D:\Prop\.env"
        };
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
            return null;
        Load(path);
        return path;
    }
```

```33:38:D:/Prop/src/Mt5/Env/EnvFile.cs
            var i = line.IndexOf('=');
            var key = line[..i].Trim();
            var value = line[(i + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];
            Environment.SetEnvironmentVariable(key, value);
```

Health surfaces whether a file was found. It does not print key names or values:

```30:30:D:/Prop/apps/api/Program.cs
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow, envLoaded = loadedEnv is not null }));
```

```55:56:D:/Prop/apps/api/Program.cs
        realCopyEnabled = runtime.RealCopyEnabled,
        envFile = loadedEnv is null ? "missing" : "loaded"
```

Grep on `D:/Prop/apps/api` for this angle:

| pattern | hits |
|---|---|
| `EnvFile.FindAndLoad()` | line 9 — **before** builder |
| `WebApplication.CreateBuilder` | line 11 — **after** load |
| `AddEnvironmentVariables` | line 12 — after builder, process env already populated |
| `DotNetEnv` / `LoadEnv` | none |

This file does **not** contain:

- `CreateBuilder` before any dotenv load
- a later-only `EnvFile.Load` after `builder.Build()` / `app.Run()`
- hardcoded credentials, proxy auth, or FIX passwords (none printed here)

## No-loss implication

The assigned defect is absent: process env is populated from `.env` (when a candidate exists) **before** `WebApplication.CreateBuilder`. Host configuration therefore sees file-backed flags and connection keys at the same time as other environment sources. `AddEnvironmentVariables()` after the builder is a second, later layer — not a substitute for the pre-builder load.

This host itself has no live send path. Mapped routes are health, dashboard reads, and `/api/ops/resync` (catalog + deal ingest + reconstruction scoring). Settings expose `REAL_COPY_EXECUTION_ENABLED` from `LiveRuntimeStatus` and hard-code `FEATURE_COPY_TRADING_ENABLED = false`. No `NewOrderSingle`, `OrderSend`, or flatten is constructed here.

No-loss: a missing `.env` is reported as `envFile: "missing"` / `envLoaded: false` and leaves process env unchanged — it does not invent size or place orders. A present `.env` is applied before the builder, so feature flags and risk-related keys are not silently dropped due to load order. Capital cannot be lost *by this ordering bug* because the ordering bug is not present in `Program.cs`.

Empty-PASS justification: not used. The assigned file was fully read (156 lines); the angle was evaluated against the actual call order (load → CreateBuilder → AddEnvironmentVariables), which is correct.
