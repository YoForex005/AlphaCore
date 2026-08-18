# P500_S045 — `RealCopyEnabled` is **not** forced false (env `true` sticks)

| Field | Value |
|---|---|
| Slot | P500_S045 |
| Date | 2026-08-18 |
| Assigned claim | `CTraderFixLogonHostedService` sets `RealCopyEnabled=false`; even env `true` is overwritten; this is the capital-preservation switch; **do not flip**; **do not edit product** |
| Measured now | Assigned overwrite **ABSENT** on disk. DI **binds** `REAL_COPY_EXECUTION_ENABLED`. Root `.env` is `true`. After boot the runtime bool can stay **true**. |
| Product source modified this slot | **No** |
| Live `35=D` / NewOrderSingle | **Still absent** (`SAFE_BY_ABSENCE` on the wire) |
| Secret values printed | **None** (env **key + boolean only**) |

## Verdict

**FAIL as a force-false capital-preservation switch. PASS as wire-safety by absence.**

The assigned story (“hosted service overwrites even env `true`”) is **not true of the files on disk at write time**. There is **zero** product assignment `_runtime.RealCopyEnabled = false`. `AddTraderIntelligence` now copies the env token onto the singleton. `D:\Prop\.env` has `REAL_COPY_EXECUTION_ENABLED=true`. Therefore **env `true` is not overwritten**.

This slot **did not** restore the pin and **did not** flip anything. Capital is still not at risk from a live Pepperstone ticket because **no `35=D` builder exists**, `CopyTradingService.NewOrderSingleImplemented = false`, and `RiskDecisionRecord.AllowFixSend` is written **`false`**. That is **not** the same as the assigned overwrite switch.

**Do not treat `/api/settings` `REAL_COPY_EXECUTION_ENABLED=true` as a send license. Do not add a sender. Do not flip the flag further. Do not edit product from this slot.**

## 1. `Program.cs` settings (API display, not a binder)

File: `D:\Prop\apps\api\Program.cs`

Boot loads `.env` then environment variables, then `AddTraderIntelligence`:

```9:14:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs`) walks CWD parents and always also tries `D:\Prop\.env`, then `Environment.SetEnvironmentVariable` for every `KEY=value`. So a root `.env` boolean **does** enter `IConfiguration`.

`/api/health` and `/api/settings` **read the runtime singleton**. They do **not** hardcode `false` anymore:

```54:76:D:\Prop\apps\api\Program.cs
        realCopyEnabled = runtime.RealCopyEnabled,
        envFile = loadedEnv is null ? "missing" : "loaded"
    });
});
// ...
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
```

| Endpoint | Field | Source | Can show `true` if env true? |
|---|---|---|---|
| `GET /api/health` | `realCopyEnabled` | `LiveRuntimeStatus.RealCopyEnabled` | **Yes** |
| `GET /api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED` | same singleton | **Yes** |
| `GET /api/settings` | `featureFlags.FEATURE_COPY_TRADING_ENABLED` | **literal `false`** | **No** |
| `GET /api/ingest/status` | `realCopyEnabled` + `copyNote` | `runtime.Snapshot()` | **Yes** |
| `GET /api/overview` | `RealCopyEnabled` | `EfDashboardQueries` → `_runtime.RealCopyEnabled` (L52) | **Yes** |
| `GET /api/risk` | `RealCopyEnabled` | same, L208 | **Yes** |

`FEATURE_COPY_TRADING_ENABLED` is a **display floor**. It is **not** wired to `LiveRuntimeStatus`. Root `.env` also has `FEATURE_COPY_TRADING_ENABLED=true`; the API **ignores** that token and still JSON-emits `false`.

Dead twin: `D:\Prop\apps\api\Controllers\SettingsController.cs` reads `FeatureFlags:LiveCopyEnabled` (appsettings default **false**) and can PUT Redis `settings:flags:live_copy`. API host has **no** `MapControllers`. That type **cannot** arm `LiveRuntimeStatus.RealCopyEnabled`.

Workers (`apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`) call `AddTraderIntelligence` but **do not** call `EnvFile.FindAndLoad`. They still inherit process env if the operator / compose already exported the key.

## 2. DI — env `true` **arms** the singleton

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–43:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

This is **not** a pin. Earlier swarm notes that cited a comment *“Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.”* plus `RealCopyEnabled = false` are **stale**. Current disk **honors** env.

On-disk env (boolean only; neighboring secret lines not copied):

| Key | File | Value |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | `D:\Prop\.env` L73 | **`true`** |
| `FEATURE_COPY_TRADING_ENABLED` | `D:\Prop\.env` L106 | **`true`** (API settings still emit `false`) |
| `FeatureFlags:LiveCopyEnabled` | `apps/api/appsettings.json` L46 | **`false`** (unmapped controller only) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` L35 | default **`false`**, **unbound** from `REAL_COPY_*` |

`string.Equals(..., "true", OrdinalIgnoreCase)` means `TRUE` / `True` also arm. Any other string (including empty / `1` / `yes`) stays **false**.

## 3. `CTraderFixLogonHostedService` — overwrite **gone**

File: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (112 lines).

**This session’s first `read_file` still had** `_runtime.RealCopyEnabled = false;` **immediately after stamping QUOTE/TRADE logon (then L68) and logged “NewOrderSingle still disabled.”** A concurrent edit removed that assignment before this report was written. **Current file:**

```60:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

Product-tree grep of `RealCopyEnabled` under `src/**/*.cs` (12 hits): **no assignment to `false`**. The hosted service **reads** the bool for a log field. It **does not write** it.

Password missing / contains `<SECRET>`: early `return` at L34–38. That path never touched the flag even when the overwrite existed. **Now both paths leave DI’s env-derived value in place.**

LoggedOn QUOTE/TRADE is **session proof only**. It is not a send license. Hosted service still only calls `CTraderFixSession.TryLogonAsync` (`35=A`). Persist updates existing `FixSessionState` rows (does not insert).

**Do not re-add a sender here. Do not flip `RealCopyEnabled` to invent a live path. This slot did not restore the missing pin because the standing instruction was “do not edit product.”**

## 4. `LiveRuntimeStatus` — honest when armed

`D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`:

```32:44:D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs
    public bool RealCopyEnabled { get; set; }
    // ...
        copyNote = RealCopyEnabled
            ? "REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."
            : "NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.",
```

Older reports that quote `copyNote` as *“LIVE SEND ARMED — unexpected”* when true are **stale**. Current true-note still admits **no ticket**. False-note is the old capital-safe sentence.

Property is a public setter. Any later hosted service can flip it. **None currently write `false`.**

## 5. Downstream consumers (display / shadow only)

| Consumer | Uses flag as | Effect if env true |
|---|---|---|
| `CopyTradingService.GetStatusAsync` | `RealCopyArmed` | Status JSON can say armed; `NewOrderSingleImplemented` const **false**; `VenueReconciled` const **false** |
| `CopyTradingService` risk request | `RealExecutionEnabled = _runtime.RealCopyEnabled` | Risk **may** see execution enabled; persist still `AllowFixSend = false` (L192) |
| `CopyTradingService.BuildBlockers` | adds `"REAL_COPY_EXECUTION_ENABLED is false"` only when flag false | That blocker **drops** if env true; **other blockers remain** (no sender, not reconciled, 0 LIVE, FIX logon) |
| `CopyTradingHostedService` | does **not** read the flag | Shadow intents every 20 s; log “Live NewOrderSingle still blocked.” |
| `EfDashboardQueries` overview / risk | last DTO field | UI can paint **ON** |
| `apps/fix-worker/Worker.cs` | **different** key `CTrader:RealCopyExecutionEnabled` default **false** | Log-only; if that nested key were true it still **refuses** send and stamps sessions `Disconnected` |

Live-send `if` inside `GenerateShadowIntentsAsync` L198 still requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. Two of those are **hardcoded false**. The branch only writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. It never builds FIX.

## 6. Precedence (measured)

```
D:\Prop\.env REAL_COPY_EXECUTION_ENABLED=true
        │
        ▼
EnvFile.FindAndLoad (API only) + AddEnvironmentVariables
        │
        ▼
AddTraderIntelligence
  LiveRuntimeStatus.RealCopyEnabled = (env == "true")   ← ARMS
        │
        ▼
CTraderFixLogonHostedService
  TryLogon 35=A QUOTE 5211 + TRADE 5212
  _runtime.RealCopyEnabled = false   ← NOT ON DISK
  log RealCopyArmed={current value}  ← PASSTHROUGH
        │
        ▼
GET /api/settings featureFlags.REAL_COPY_EXECUTION_ENABLED
GET /api/health realCopyEnabled
GET /api/ingest/status Snapshot()
overview / risk DTOs
        │
        ▼
Wire: still no NewOrderSingle / 35=D / tag 38
```

**Assigned sentence “even env true is overwritten” = FALSE on current disk.**

## 7. Capital / honesty

| Check | Result |
|---|---|
| Assigned overwrite switch present? | **No** |
| Env `true` overwritten? | **No** |
| API can advertise armed? | **Yes** (after `.env` load) |
| `35=D` / `NewOrderSingle` assembler? | **No** (`SAFE_BY_ABSENCE`) |
| `AllowFixSend` persisted? | **Always `false`** |
| `NewOrderSingleImplemented` | **const `false`** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default false, unbound |
| This slot edited product? | **No** |
| This slot flipped the flag? | **No** |
| Risk to live Pepperstone capital from a ticket this process can emit | **NONE** (absence, not the assigned pin) |

**Operator note (do not implement here):** restoring `_runtime.RealCopyEnabled = false` after logon (and on the password-skip path) would match the assigned capital-preservation switch. Re-hardcoding `false` in DI would ignore env. Either change is a **product edit** and was **out of scope**. Concurrent removal of the pin is a **real regression of the display/gate bool**, not of the missing sender.

## 8. Files read (this slot)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\Controllers\SettingsController.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `.env` keys `REAL_COPY_EXECUTION_ENABLED` / `FEATURE_COPY_TRADING_ENABLED` via grep (values not dumped beyond the booleans)

## 9. Binding instruction (repeat)

This is supposed to be the capital-preservation switch. **Do not flip it on. Do not add NewOrderSingle. Do not edit product from this slot.** Live copy stays off because the sender does not exist — not because env `true` is overwritten.
