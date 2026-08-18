# W500_VERIFY_16 — Adversarial live-path verify (slot 16)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **16** |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
| Secrets printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this slot | **No** (capability from source only; census not re-probed) |
| Overall verdict | **FAIL** |

**Law used:** set overall **FAIL** if any assigned claim cannot be proven from the live file. Wanting a green card does not create proof.

Assigned claims:

1. `DemoSeeder` is not the API startup path
2. Native connector can list all groups via `GroupRequestArray` or `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has no `35=D`
5. `REAL_COPY_EXECUTION` stays false

---

## Scorecard

| # | Claim | File-proven? | Verdict |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **Yes** | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | **Yes** (capability; this slot did not attach) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **Yes** (capability; this slot did not attach) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | **Yes** (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No** — opposite is on disk | **FAIL** |

**Overall: FAIL** because claim 5 is not true on the live path. Architecture/POCO *defaults* are still false; the running API **binds** lab `.env` `REAL_COPY_EXECUTION_ENABLED=true` onto `LiveRuntimeStatus.RealCopyEnabled` and does **not** re-pin it.

Risk to capital today: **NONE** (`SAFE_BY_ABSENCE`). Copy hop still has no `NewOrderSingle` builder. The FAIL is a **flag-pin** fail, not a live ticket.

---

## 1. PASS — `DemoSeeder` is not the API startup path

Live file: `D:\Prop\apps\api\Program.cs` (160 lines).

Startup after `app.Build()`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- `using TraderIntelligence.Infrastructure.Seeding;` exists solely for `BrokerCatalogSeed`.
- Tokens `DemoSeeder`, `FakeMt5`, `10001`, `10002` in this file: **0**.
- Tokens `DemoSeeder` under `D:\Prop\apps\`: **0**.
- Same seed on workers: `D:\Prop\apps\mt5-worker\Program.cs` L15 and `D:\Prop\apps\fix-worker\Program.cs` L15 are also `BrokerCatalogSeed.EnsureAsync` only.

`DemoSeeder` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder`) and is called from `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25. That is a test fixture, **not** API/worker startup.

DI (`D:\Prop\src\Infrastructure\DependencyInjection.cs` L36–48) fail-closes unless both manager passwords pass `IsSecret`, then registers `LiveMt5Registration.CreateConnectors` (Native ×2). No `FakeMt5BrokerConnector` on the host register path.

**Claim 1 proven from the file.**

---

## 2. PASS — Native can list all groups via `GroupRequestArray` or `GroupTotal`

Live file: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

`GetGroupsCore` (L144–186):

1. Primary: `_manager.GroupRequestArray("*", arr)` (L155). Mask `*` is the manager-visible enumerator.
2. If the request array is empty: fallback `_manager.GroupTotal()` + `GroupNext` (L174–179).

Ingest uses that walk, not a plan-name filter:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Hosted ingest (`LiveIngestHostedService` L56) calls `SyncCatalogAsync` for every registered connector (Achiever + Starwave). No `Take`/`Skip` on the group walk.

**Caveat (honest, not a FAIL of this claim):** this slot did **not** live-attach. Source proves the connector *can* enumerate via those two APIs. Completeness at runtime still depends on Connect + ACL. `GroupTotal` is the pump-cache fallback; the request path is `GroupRequestArray("*")`.

**Claim 2 proven from the file as capability.**

---

## 3. PASS — All traders via `UserRequestArray` / `UserLogins`

Same connector file, `GetAccountsCore` + `ReadAccountsForGroup`:

- `GetAccountsCore(null)` (L189–213) walks **every** group from `GetGroupsCore()`, then `ReadAccountsForGroup`.
- Primary: `_manager.UserRequestArray(gname, users)` (L223).
- Hard-fail only: `UserGetByGroup` (pump cache) (L225).
- If still empty: `_manager.UserLogins(gname, out loginRes)` then `UserRequestByLogins` (L230–232).

Ingest catalog path is `GetAccountsAsync(null)` — all groups, all users those APIs return. Manual `/api/ops/resync` (`Program.cs` L134) scores `store.ListLoginsAsync` (all persisted logins), not a 4-login demo set.

**Caveat:** this slot did not re-probe Achiever/Starwave. Prior measured census (not re-attached here): 8/6512 + 10/1948 = 18/8460.

**Claim 3 proven from the file as capability.**

---

## 4. PASS — `CTraderFixSession` has no `35=D`

Live file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines, read in full).

Outbound MsgType construction is **only**:

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
```

Measured in this file:

| Token | Hits |
|---|---:|
| `(35, "A")` | 1 (L96) |
| `WriteAsync` | 1 (L49, logon bytes) |
| `35=D` / `"D"` / `NewOrderSingle` | **0** |
| Tag 35 extract | inbound reply only (L55) |

Sockets are `using`/`await using` and disposed. Hosted caller is `CTraderFixLogonHostedService` (QUOTE 5211 + TRADE 5212 logon only). It logs `NewOrderSingle still unimplemented` (L69) and does **not** build an order.

**Sibling residual (does not refute this claim):** `CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. That is a **different type**, demo-gated (`demo-` host / `demo.` sender; refuses `live-*` / `live.` / account `1369850`), called from `tools/DemoFixTestTrade`, **not** from API/DI/copy. Claim 4 is specifically `CTraderFixSession`.

**Claim 4 proven from the assigned file.**

---

## 5. FAIL — `REAL_COPY_EXECUTION` does **not** stay false

This is the claim that cannot be proven. The live path **arms** the flag.

### 5.1 DI binds the env token (no hard-false pin)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`LiveRuntimeStatus.RealCopyEnabled` is a settable bool (default false only if the env key is missing or not `"true"`).

### 5.2 API loads `.env` then environment variables

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L14) includes `D:\Prop\.env` and `Environment.SetEnvironmentVariable` for every `KEY=value`.

### 5.3 Lab `.env` is already true

| Line | Token | Value |
|---|---|---|
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED` | **`true`** |
| `D:\Prop\.env` L106 | `FEATURE_COPY_TRADING_ENABLED` | **`true`** |

No other bytes from `.env` are quoted.

Therefore a host that reaches `AddTraderIntelligence` with this `.env` sets `runtime.RealCopyEnabled = true`.

### 5.4 Hosted FIX logon does **not** re-pin false

`CTraderFixLogonHostedService.ExecuteAsync` reads `_runtime.RealCopyEnabled` only to log `RealCopyArmed={Armed}` (L69–70). Zero assignments of `RealCopyEnabled = false`.

### 5.5 Settings API reports the runtime bool, not a literal false

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

Minimal-API `GET /api/settings` wins over unused `SettingsController` (that controller talks Redis and a different `LiveCopyEnabled` name; it is not the live settings surface).

### 5.6 What *is* still false (does not rescue claim 5)

| Surface | Value | Why it does not prove “stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | POCO is **not** registered/`Configure<>`’d in DI. Unused pin. |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **Different key.** Log-only. Stamps sessions `Disconnected`. Does not pin API runtime. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Blocks send; does not keep the **flag** false. |
| Persist `AllowFixSend` | hardcoded `false` (L211) | Blocks send; flag can still be true. |
| Committed `appsettings.json` `FeatureFlags.LiveCopyEnabled` | `false` | Different name; unused by `/api/settings` above. |
| Architecture docs / README | say the flag must be false | Policy, not the running binder. |

`CopyTradingService.GetStatusAsync` will report `RealCopyArmed: true` when the env is true, and will **omit** the blocker `"REAL_COPY_EXECUTION_ENABLED is false"` (L316–317). Other blockers (`NewOrderSingleImplemented`, `VenueReconciled`, 0 LIVE traders) still keep the hop paper-only.

**Claim 5 cannot be proven. It is false on this tree.** Slots that still say “DI/hosted/.env pin false” or `CREDENTIALS_AND_COPY_STATUS.md` “forced false” are **stale**.

---

## Copy hop (context; not a sixth assigned claim)

| Gate | Live file | Value |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | `const false` |
| `VenueReconciled` | L16 | `const false` |
| Persist `AllowFixSend` | L211 | `false` |
| LIVE send branch | L217 | requires LIVE + both consts + `decision.AllowFixSend` |
| `ExecutionIntent` writers | product `*.cs` this slot | not re-counted; persist path writes `SHADOW_ONLY` |
| Hosted outbound MsgType | `CTraderFixSession` | `A` only |

So capital is still **SAFE_BY_ABSENCE**. Residual: the **next** sender wired to `LiveRuntimeStatus.RealCopyEnabled` will see **armed** on this lab host.

---

## What this slot did not do

- No Manager Connect / no re-census.
- No GET against a running API (would need a live process; not assumed).
- No product edits.
- Did not invoke `tools/DemoFixTestTrade`.

---

## Verdict

**FAIL.** Claims 1–4 are file-proven. Claim 5 (`REAL_COPY_EXECUTION` stays false) is **not** file-proven: DI L41 binds `.env` L73 `=true`, logon does not re-pin, `/api/settings` echoes the runtime bool.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` — no `35=D` on `CTraderFixSession`; copy `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`).
