# D69 — `RealCopyExecutionEnabled` default is **`false`**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D69_flag.md` |
| Agent | D69 (flag default recensus, read-only of product) |
| Date | 2026-08-18 |
| Assigned | Find `RealCopyExecutionEnabled` default. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Primary type | `TraderIntelligence.Fix.CTrader.Configuration.CTraderFixOptions` |
| Primary file | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§41** (lines 1564–1590) and **§56** (line 2101) |
| Binding siblings | A49 (worker enforcement design), A25 §6, A75, A101, B13, D13, D26 (same options file), D32 (same worker) |
| Method | Read options POCO + every product `*.cs` / `appsettings*` / launchSettings / compose / `.env` hit for `RealCopyExecutionEnabled` / `REAL_COPY_EXECUTION_ENABLED`. SHA-256 via `Get-FileHash`. HEAD vs worktree via `git show` / `git hash-object`. Nothing answered from memory. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A hardcoded JSON/API `false` is a display floor, not a send gate. Absence of `NewOrderSingle` is **SAFE_BY_ABSENCE**, not proof that this property is wired.

---

## 0. Verdict (binding)

**Default is `false` (OFF).**

The property that owns the name is:

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

That is the **C# initializer**. It is the same on **HEAD** and on this **worktree** (the unstaged edit of this file is `TargetCompId` `CSERVER` → `cServer`, not this flag — D26). Architecture §41 / §56 name the env twin `REAL_COPY_EXECUTION_ENABLED=false`. Every other measured product site is also `false` or hardcodes `false`.

| Surface | Default | Bound to the POCO? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | **`false`** | **is** the POCO |
| Architecture §41 / §56 | **`false`** | design law |
| `apps/fix-worker` `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback **`false`** | **No** — different key, no `IOptions<CTraderFixOptions>` |
| `apps/fix-worker/appsettings*.json` | key **absent** | N/A (fallback applies) |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | **`false`** | **No** — different name |
| `apps/api` `MapGet("/api/settings")` | hardcoded **`false`** | **No** |
| `SettingsController` `LiveCopyEnabled` | `GetValue(..., false)` | **No** — Redis/settings alias |
| `EfDashboardQueries` `OverviewDto` / `RiskDashboardDto` / FIX `ExecutionEnabled` | literal **`false`** | **No** |
| `LiveCopyPage.tsx` | static copy “is false” | **No** |
| Local `.env` (gitignored) | `REAL_COPY_EXECUTION_ENABLED=false` | **No** — worker does not read this env name |
| Tracked `.env.example` (HEAD; **missing from worktree**) | `REAL_COPY_EXECUTION_ENABLED=false` | **No** |
| `docker-compose.yml` / launchSettings | key **absent** | N/A |
| `tests/` | **0** hits of `RealCopyExecutionEnabled` | fixture uses `RealExecutionEnabled = false` on the **risk request**, not this property |
| `RiskEngine` empty `if (RealExecutionEnabled == false …)` | caller bit, default in unit fixture **`false`** | **No** — different identifier |

**Classification of the default:** `EXISTS_AND_GOOD` (matches §41).  
**Classification of the gate:** `GATE_INCOMPLETE` / live send still **SAFE_BY_ABSENCE** (no 35=D builder; D32). Do not treat this filename as “Phase 8 is flag-gated.”

---

## 1. Files hashed (this pass)

| Bytes | Lines | SHA-256 | Path |
|---:|---:|---|---|
| 2344 | 80 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| 2093 | 51 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | `D:\Prop\apps\fix-worker\Worker.cs` |
| 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `D:\Prop\apps\api\Program.cs` |
| — | — | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | `D:\Prop\apps\api\appsettings.json` |
| — | — | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | `D:\Prop\apps\api\Controllers\SettingsController.cs` |
| — | — | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| — | — | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` |
| 3408 | — | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | `D:\Prop\.env` (local, gitignored) |

Git identity of the options file:

| Ref | Blob / note |
|---|---|
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| HEAD blob `CTraderFixOptions.cs` | `204f9d58a913022c31cdb4fa2eefef9d92916795` |
| Worktree blob | `f2cd089d29304a3e107dbc1e58957421a65296d6` (unstaged ` M`) |
| `RealCopyExecutionEnabled` on HEAD vs worktree | **both `= false`** |

`CTraderQuoteService` takes `CTraderFixOptions` and **never reads** `RealCopyExecutionEnabled` (only `Quote` + `MaxQuoteAgeMs`).

---

## 2. Law (architecture §41)

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

§41: connect, receive prices, request orders/positions, validate FIX — **without automatically placing new real orders**. `NewOrderSingle` requires `REAL_COPY_EXECUTION_ENABLED=true` **plus** runtime risk-engine healthy. §56 repeats the same `false` in the env sample.

Comment on the property matches that law: “Default OFF.”

---

## 3. Every measured default site

### 3.1 The property (canonical)

`public bool RealCopyExecutionEnabled { get; set; } = false;`

No `[DefaultValue]`, no constructor override, no static factory that sets `true`. `new CTraderFixOptions()` yields `false`.

Sibling flags on the **same** type (for contrast, not this ask):

| Property | Compile default |
|---|---|
| `UseSsl` | `true` |
| `QuoteEnabled` | `true` |
| `TradeSessionEnabled` | `true` |
| **`RealCopyExecutionEnabled`** | **`false`** |

That matrix is the §41 shape: sessions may be on; send stays off.

### 3.2 Fix-worker read (not the POCO)

```21:22:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

- Missing / unparseable / absent section → **`false`**.
- Key is **`CTrader:RealCopyExecutionEnabled`**, so env alias is `CTrader__RealCopyExecutionEnabled`.
- Architecture flat name `REAL_COPY_EXECUTION_ENABLED` is **not** this key. Local `.env` line 73 does **not** reach this `GetValue`.
- `apps/fix-worker/appsettings.json` and `appsettings.Development.json` are logging-only. `Properties/launchSettings.json` has no `CTrader*` / `REAL_COPY*` env.
- If `real` is true, the loop only **logs a warning**. Status is still `Disconnected`. No socket. No 35=D (D32).

### 3.3 API / dashboard (display floors)

| Site | What it does |
|---|---|
| `Program.cs` L42–47 `/api/settings` | `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = false` — **not** read from config |
| `SettingsController` | `FeatureFlags:LiveCopyEnabled` default **false**; PUT writes Redis `settings:flags:live_copy`. **Not** `RealCopyExecutionEnabled` |
| `appsettings.json` | `"FeatureFlags": { "LiveCopyEnabled": false, ... }` — no `CTrader` / `RealCopyExecutionEnabled` key. `CTraderFix` block has hosts/ports only |
| `EfDashboardQueries` L42, L183, L196 | `OverviewDto.RealCopyEnabled = false`; FIX DTO `ExecutionEnabled = false`; `RiskDashboardDto.RealCopyEnabled = false` |
| `LiveCopyPage.tsx` | Static sentence: flag is false |

B13 / A101 claims that API `appsettings.json` contains `CTrader:RealCopyExecutionEnabled: false` are **stale** on this hash (`69D41CAD…`). That key is **not** in the file.

### 3.4 Env / compose / docs

| Site | Measured |
|---|---|
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=false` (gitignored; `.gitignore` has `.env` + `.env.*` with `!.env.example`) |
| `D:\Prop\.env.example` | **Missing on disk.** HEAD blob contains `REAL_COPY_EXECUTION_ENABLED=false` |
| `docker-compose.yml` | **0** flag keys (api service sets `ASPNETCORE_ENVIRONMENT` only) |
| `README.md` Safety | “Real NewOrderSingle is **off** (`REAL_COPY_EXECUTION_ENABLED=false`).” |
| `docs/architecture.md` | same default |
| `docs/deployment.md` | operator step: keep false until FIX verified |
| `docs/ctrader-fix.md` | false disables live execution |

### 3.5 Risk request bit (not the options property)

`RiskEvaluationRequest.RealExecutionEnabled` is a **required caller field**, not `CTraderFixOptions`. Unit fixture `RiskEngineTests.Base` sets `RealExecutionEnabled = false`. `Evaluate` L90–93 is an empty `if` when the bit is false (does not reject). `AllowFixSend` L147–150 is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Domain grep for `RealCopyExecutionEnabled`: **0**. See D13.

`tests/` grep for `RealCopyExecutionEnabled`: **0**. A89’s `CTraderFixOptionsSafetyDefaultsTests` / `RealExecutionFeatureFlagTests` are **MISSING** as files.

---

## 4. What this default does **not** mean

1. It does **not** bind `REAL_COPY_EXECUTION_ENABLED` onto `CTraderFixOptions` (A49 §3.5 gap still holds).
2. It does **not** make `new CTraderFixOptions()` the worker’s runtime object (`Configure<CTraderFixOptions>` is absent).
3. It does **not** block 35=D by conjunction — there is no send function to block (D05 / D32).
4. It is **not** `FeatureFlags:LiveCopyEnabled`. A SuperAdmin PUT on that Redis key cannot flip `RealCopyExecutionEnabled`.
5. Dashboard `RealCopyEnabled: false` is a **literal**, not a read of this property.
6. `TradeSessionEnabled = true` is **not** a send license.

---

## 5. One-liner

`RealCopyExecutionEnabled` defaults to **`false`** (`CTraderFixOptions` L35; same on HEAD). Architecture, `.env`, worker `GetValue` fallback, API literals, and dashboard DTOs all stay off. The default is correct. The send gate is still absence, not this bool.

*End of D69. Product source was not modified.*
