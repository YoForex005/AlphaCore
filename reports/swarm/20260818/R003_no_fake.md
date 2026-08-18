# R003 — Plan: refuse `FakeMt5BrokerConnector` when `USE_REAL_MT5=true`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\R003_no_fake.md` |
| Agent | R003 (composition / Fake-refuse plan) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass; hashes below) |
| Assigned | Read `DemoSeeder` and DI. Plan how to refuse Fake connector when `USE_REAL_MT5=true`. Write this file. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| Config / `.env*` / `appsettings` edited | **No** |
| Secret values printed | **None.** Password slots classified by name + class + length only. |
| Binding siblings | C05 / D23 (DI), D22 / E008 (seeder), D24 / C10 / C42 (Fake), A58 (registry/factory), A79 (test-only Fake), A49 / D69 (flag law), A75 / D61 / E001 / E011 (env), A77 (health), A100 G01 |
| Method | Full read of `DemoSeeder.cs` + `DependencyInjection.cs`. Cross-read Fake, contracts, ingestion, three host `Program.cs`, mt5-worker `Worker.cs`, `Mt5BrokerOptions`, `SeedingAndStoreTests`, dashboard `GetBrokersAsync`, API health, architecture §56. `Get-FileHash SHA256`. Classify gitignored `.env` keys without reprinting secret values. Grep product `*.cs` for `USE_REAL_MT5` / `: IMt5BrokerConnector` / `DemoBrokerFactory`. Nothing answered from memory. |

**Honesty rule:** `USE_REAL_MT5=true` in a gitignored file is **not** a Manager session. Registering `FakeMt5BrokerConnector` while that flag is on is a **silent lie**. This file is a **plan**. It does **not** implement the gate. It does **not** add a live connector. It does **not** claim A100 G01.

---

## 0. Verdict (binding)

**Today the flag is unread. Fake is always the production connector. `USE_REAL_MT5=true` does nothing.**

| Surface | Measured now |
|---|---|
| Product C# token `USE_REAL_MT5` | **0 hits** under `src/`, `apps/`, `tests/` |
| Architecture §56 catalog | **Does not name** `USE_REAL_MT5` |
| `AddTraderIntelligence` | **Always** `DemoBrokerFactory.CreateDefault()` → two `FakeMt5BrokerConnector` singletons |
| `DemoSeeder.SeedAsync` | **Always** a **second** `CreateDefault()` + year-window `SyncBrokerAsync` |
| `IMt5BrokerConnector` implementors | **1:** `FakeMt5BrokerConnector` |
| Live `Mt5ManagerBrokerConnector` / HTTP adapter | **MISSING** |
| Hosts load `D:\Prop\.env`? | **No** (zero `DotNetEnv` / `AddUserSecrets`) |
| Process / User / Machine `USE_REAL_MT5` | **ABSENT** |
| Gitignored `.env` line | `USE_REAL_MT5=true` **and** `USE_DEMO_DATA=false` (file unread by hosts) |

**Recommended later increment (not applied):** treat `USE_REAL_MT5=true` as a **fail-closed composition law**:

1. **Do not construct** `FakeMt5BrokerConnector` / `DemoBrokerFactory.CreateDefault()`.
2. **Do not ingest** the canned 18-deal tape through `DemoSeeder`.
3. **Do not start** the host if the only available implementor is Fake, or if any registered `IMt5BrokerConnector` **is** Fake.
4. **Do not** treat the flag as a copy/send license (`REAL_COPY_EXECUTION_ENABLED` stays a different floor).
5. Tests keep Fake **only** on an explicit demo path (`USE_REAL_MT5` false / unset).

Until a real connector exists, the honest outcome of `USE_REAL_MT5=true` is **`InvalidOperationException` at host build** — not a renamed Fake, not InMemory deals painted as Achiever.

**Overall class of current composition:** `UNSAFE` as a default once an operator writes `USE_REAL_MT5=true`. **MISSING** as a gate. Fake itself remains `EXISTS` / demo-only (D24).

---

## 1. File identity (this pass)

| Path | Bytes | Lines | SHA-256 | Role |
|---|---:|---:|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 5082 | 140 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | second Fake graph + catalog seed |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | 44 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | always registers Fake |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | 170 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | only implementor + factory + registry |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | 1858 | 70 | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` | port |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4535 | 106 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | walks whatever registry returns |
| `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | 1609 | 51 | `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7` | unused by DI |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | seed + health admits Fake |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | 22 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | same seed |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | 22 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | same seed |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 1882 | 45 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 30 s Fake ingest |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | 3119 | — | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | calls seeder; **bypasses DI** |

Seeder SHA matches E008 (`A6416491…`, `Disconnected` FIX rows). DI / Fake SHAs match D23 / D24 / C42. Ingestion SHA moved vs C05 (`87B74E71…` → `2637D97B…`) because `PersistDemoShadowAsync` is now on the scoring path — **not** because Fake was gated.

Gitignored operator file (classified, not loaded by hosts):

| Path | Bytes | Lines | SHA-256 | Tracked |
|---|---:|---:|---|---|
| `D:\Prop\.env` | 3484 | 118 | `A4EF94B990EE389C7E7900B599A60AE10E0C16E96E4B5DA612302759958982D7` | **No** (`.gitignore:28:.env`) |

This hash is **not** E001/D61’s `56C81786…` (3408 B / 115 lines). The file grew by the two flags at the bottom. HEAD `.env.example` does **not** contain `USE_REAL_MT5` / `USE_DEMO_DATA` (grep of `git show HEAD:.env.example` empty). Architecture §56 also omits both names.

Password-named keys in that ignored file (values discarded): three slots classify as **NON_PLACEHOLDER** (`MT5_PASSWORD` len 8, `ACHIEVER_PROXY_PASSWORD` len 15, `MT5_STARWAVEFX_PASSWORD` len 11). `CTRADER_FIX_PASSWORD` and `MT5_PASSWORD_ENCRYPTION_KEY` remain placeholder tokens. **This is not a live Manager proof.** Hosts do not read the file. Process / User / Machine names for `USE_REAL_MT5`, `USE_DEMO_DATA`, `MT5_PASSWORD`, and `MT5_STARWAVEFX_PASSWORD` are **ABSENT**. Do not copy those values into appsettings, reports, or logs.

---

## 2. What DemoSeeder actually does

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` is a **static** method, not an MS.DI service (C05). Signature:

```16:23:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
    public static async Task SeedAsync(
        TraderDbContext db,
        ITradingStore store,
        ReconstructionScoringService scoring,
        CancellationToken ct)
    {
        if (await db.Brokers.AnyAsync(ct))
            return;
```

It does **not** take `IConfiguration`, `IBrokerRegistry`, `DealIngestionService`, or a mode enum. It cannot see `USE_REAL_MT5` today.

Sequence on first empty `brokers` table:

| Step | What it writes / calls | Fake? |
|---|---|---|
| 1 | Two `Broker` rows (`ACHIEVER` / `STARWAVEFX`) with live-shaped `Server` / `Port` / `ManagerLogin` | Catalog paint. Fake never reads these fields. |
| 2 | One `CanonicalInstrument` `XAUUSD` | Catalog |
| 3 | Two `FixSessionState` rows, **`Disconnected`** (E008; D22 `LoggedOn` is **stale**) | Not MT5 Fake, still demo theatre |
| 4 | One dest quote `2399.45` / `2399.85`, `VenueInstrumentId = null` | Forged book |
| 5 | Default `KillSwitch` | Catalog |
| 6 | `SaveChangesAsync` | Persist catalog |
| 7 | `DemoBrokerFactory.CreateDefault()` | **Constructs Fake × 2** |
| 8 | `new BrokerRegistry(...)` + `new DealIngestionService(...)` | **Bypasses container** |
| 9 | `SyncBrokerAsync` both codes, `2026-01-01` … `2026-12-31` | Ingests **18 canned deals** / 4 logins |
| 10 | `RebuildTraderAsync` for `10001,10002,10003,99001` | Scores the Fake tape; side-effect shadow rows (D48) |

The refuse point that matters is **step 7**. If step 7 runs under `USE_REAL_MT5=true`, the database is polluted with demo tickets `10501…` / logins `10001…` that later look like Achiever history.

Guard `Brokers.Any()` is **not** a Fake refuse. On a shared Postgres, the first host to win the race seeds Fake forever; later hosts return. Flipping the flag after first boot **does not un-seed** the tape.

Hosts that always call this method after `EnsureCreatedAsync`:

- `apps/api/Program.cs` L84–93
- `apps/mt5-worker/Program.cs` L11–19
- `apps/fix-worker/Program.cs` L11–19

`tests/Integration/SeedingAndStoreTests` calls it with a hand-built InMemory context and **never** goes through `AddTraderIntelligence`. A DI-only gate would **not** stop that test (correct: tests must stay on Fake). A seeder that reads ambient process env would **break** that test if a developer exported `USE_REAL_MT5=true`.

---

## 3. What DI actually registers

```17:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
    public static IServiceCollection AddTraderIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
        }
        else
        {
            services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
        }

        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
        // store, queries, reconstructor, scorer, ingestion, scoring …
        return services;
    }
```

Measured facts that the refuse plan must not forget:

| Fact | Consequence |
|---|---|
| `CreateDefault()` runs **at registration**, not resolve | Fake objects exist before `Build()`. A resolve-time check is too late to “not construct” them. |
| No `IHostEnvironment` parameter | Cannot branch on `IsProduction()` without adding a package or passing a bool from hosts. `IConfiguration` is already there — **use it**. |
| No `IOptions<Mt5BrokerOptions>` bind | Slot binder (A58) is **MISSING**. `USE_REAL_MT5=true` cannot magically build a Manager connector. |
| InMemory fallback is a **separate** lie | Empty `ConnectionStrings:TraderIntelligence` → InMemory. Real-MT5 + InMemory is still a demo store. Gate them independently (D23 `TI_ALLOW_INMEMORY`). |
| Split graph (C05 §6) | Seeder `new`s a second Fake pair. Gating only DI leaves seed on Fake. Gating only seeder leaves the worker loop on Fake. **Both** must refuse. |
| `GetRequiredService<IMt5BrokerConnector>()` (singular) | Last registration wins (`STARWAVEFX`). Nobody does this today. Refuse must cover `GetServices` / registry `All()`. |

`BrokerRegistry` (`FakeMt5BrokerConnector.cs` L70–87) accepts **any** `IMt5BrokerConnector`. It has no type check. A future “real” factory that accidentally still returns Fake would pass.

---

## 4. Downstream consumers (why a single `if` is not enough)

```text
AddTraderIntelligence
        │  always Fake × 2
        ▼
IBrokerRegistry ─────────────────────────────┐
        │                                    │
        ▼                                    ▼
DealIngestionService                  (unused at seed)
        │                                    │
        │                         DemoSeeder CreateDefault() #2
        │                                    │
        ▼                                    ▼
mt5-worker 30 s loop              first-boot year-window ingest
POST /api/ops/resync              RebuildTraderAsync × 4
        │
        ▼
EfDashboardQueries.GetBrokersAsync
        Connected = literal true          ← never IsConnectedAsync
apps/api /api/health
        healthy = true, details admit Fake
```

| Consumer | Today | Required when `USE_REAL_MT5=true` |
|---|---|---|
| `AddTraderIntelligence` L31–33 | Always Fake | **Throw** unless every registered connector is non-Fake |
| `DemoSeeder` L126–132 | Always Fake ingest | **Do not** `CreateDefault`. Catalog-only or skip seed |
| Three host `Program.cs` | Always `SeedAsync` | Skip demo seed / call catalog-only |
| `DealIngestionService.SyncBrokerAsync` | Trusts registry | Belt: throw if `connector is FakeMt5BrokerConnector` |
| `BrokerRegistry` ctor / `Get` | Any implementor | Belt: throw if use-real && type is Fake |
| mt5-worker loop | Hard-coded `10001…99001` | Must iterate `registry.All()` live logins (A58). Out of this flag’s minimum, but the hard-coded Fake logins must not be scored as live |
| `/api/health` L28 | `healthy = true` + Fake details | `healthy = false` or process already dead |
| `GetBrokersAsync` L53 | `Connected = true` | Must not paint Fake as connected |
| `SettingsController` PUT | Redis flags only | **Must not** be able to flip `USE_REAL_MT5` |
| `SeedingAndStoreTests` | Direct seeder | Keep Fake; pass explicit demo mode |

`USE_REAL_MT5=true` is **not** `REAL_COPY_EXECUTION_ENABLED`. Refusing Fake does not authorize `35=D`. D69 / A49 stay the send floor.

---

## 5. Flag contract (when someone is allowed to edit product code)

### 5.1 Names

| Name | Role | Default |
|---|---|---|
| `USE_REAL_MT5` | **Primary fail-closed gate.** Env / `IConfiguration` flat key. | **false** (absent / empty / unparsable) |
| `Mt5:UseRealMt5` | Optional appsettings alias (`Mt5__UseRealMt5`). | same parse |
| `USE_DEMO_DATA` | Sibling already written in ignored `.env` (`false`). Gates **seeder tape**, not transport. | **true** only when `USE_REAL_MT5` is false; else **false** |
| `TI_ALLOW_FAKE_MT5` | Escape hatch for Production+Fake laptop demos. | **false** |
| `MT5_USE_FAKE` (D23 note) | **Do not add.** Inverse synonym. Dual flags are a conflict source. | — |

Architecture §56 does not list `USE_REAL_MT5`. Adding it is a **product safety key**, same family as `TI_ALLOW_INMEMORY` (D23), not a venue identity key. It may appear as a literal `false` in a future `.env.example`. It must **never** default true in committed config.

`SettingsController` / Redis `settings:flags:*` **must not** bind this name. Same law as A49: a dashboard PUT cannot raise a safety floor.

### 5.2 Parse

Match A58 boolean law, one helper (do not scatter `bool.TryParse`):

```text
true  ←  "true" | "1" | "yes" | "on"     (OrdinalIgnoreCase, trimmed)
false ←  absent | "" | "false" | "0" | "no" | "off" | anything else
```

Resolution order inside `AddTraderIntelligence`:

1. `configuration["USE_REAL_MT5"]`
2. else `configuration["Mt5:UseRealMt5"]`
3. else **false**

`WebApplication.CreateBuilder` / `Host.CreateApplicationBuilder` already map process environment variables into `IConfiguration`. **No DotNetEnv is required** for the gate to work **if** the operator sets the process env (launchSettings, compose `environment:`, systemd, User env).

**Critical honesty:** writing `USE_REAL_MT5=true` only in gitignored `D:\Prop\.env` will **still do nothing** after the C# gate lands, because hosts do not load that file (E011). The gate plan does **not** include adding a dotenv loader. Operator checklist is §10.

### 5.3 Conjunction table

| `USE_REAL_MT5` | `USE_DEMO_DATA` | Real implementor exists | Password slots real (not `<SECRET>` / empty) | Host result |
|---|---|---|---|---|
| false / unset | true / unset | n/a | n/a | **Demo allowed.** Fake + `DemoSeeder` tape. Current behavior. |
| false / unset | false | n/a | n/a | Demo **transport** still Fake. Seeder **skips tape** (catalog-only or no-op). Optional later; not required for the assigned refuse. |
| **true** | true | * | * | **FAIL START.** Conflict. Real mode refuses demo tape. |
| **true** | false / unset | **no** | * | **FAIL START.** `USE_REAL_MT5=true` but Fake is the only type. This is **today’s honest outcome**. |
| **true** | false / unset | yes | placeholder / empty / `<SECRET>` | **FAIL START.** Slot not configured (A58). Do not connect with a token. |
| **true** | false / unset | yes | non-placeholder | Register **only** real connectors. No Fake. No `CreateDefault`. No year-window demo ingest. Live connect is still a **runtime** fact (`ConnectAsync` against Manager), not this flag. |

Production extra (recommended, not the assigned minimum):

```text
if (env.IsProduction() && !useReal && !tiAllowFakeMt5)
    throw;   // Production does not silently run Fake
```

Do **not** invert this into “Production implies `USE_REAL_MT5=true` then invent a stub connector.” Missing real impl + Production = **fail start**.

### 5.4 What the flag is **not**

- Not a Manager Logon. G01 stays FAIL until a measured session exists (C42).
- Not `REAL_COPY_EXECUTION_ENABLED`. Send stays off / SAFE_BY_ABSENCE.
- Not `FeatureFlags:LiveCopyEnabled`.
- Not “Postgres is up.” InMemory vs Npgsql is a different key.
- Not permission to persist seeded IPs as proof of those hosts.
- Not a dashboard Connected bit.

---

## 6. Refuse design (three layers)

Layer 1 is sufficient if it is complete. Layers 2–3 exist because C05 already proved a **split graph**.

### 6.1 Layer 1 — do not construct Fake (composition)

**File:** `DependencyInjection.cs` (only place that should call `CreateDefault` today for the container).

Pseudocode (plan only — **not applied**):

```csharp
var useReal = Mt5Mode.ParseUseReal(configuration);

if (useReal)
{
    // FORBIDDEN:
    //   DemoBrokerFactory.CreateDefault()
    //   new FakeMt5BrokerConnector(...)
    //   AddSingleton<IMt5BrokerConnector>(anyFake)

    var reals = Mt5RealConnectorFactory.TryCreateAll(configuration);
    if (reals.Count == 0)
        throw new InvalidOperationException(
            "USE_REAL_MT5=true refuses FakeMt5BrokerConnector, and no real IMt5BrokerConnector is registered.");

    foreach (var c in reals)
    {
        if (c is FakeMt5BrokerConnector)
            throw new InvalidOperationException(
                "USE_REAL_MT5=true refuses FakeMt5BrokerConnector.");
        services.AddSingleton<IMt5BrokerConnector>(c);
    }
}
else
{
    var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
    services.AddSingleton<IMt5BrokerConnector>(achiever);
    services.AddSingleton<IMt5BrokerConnector>(starwave);
}

services.AddSingleton<IBrokerRegistry>(sp =>
{
    var all = sp.GetServices<IMt5BrokerConnector>().ToList();
    if (useReal && all.Any(c => c is FakeMt5BrokerConnector))
        throw new InvalidOperationException("USE_REAL_MT5=true refuses FakeMt5BrokerConnector in IBrokerRegistry.");
    return new BrokerRegistry(all);
});
```

Until `Mt5RealConnectorFactory` / `Mt5ManagerBrokerConnector` exists, `TryCreateAll` returns empty and **the throw is the feature**. Do **not** ship a `NotImplementedMt5BrokerConnector` that returns empty lists — that is Fake with a holier name.

`Mt5BrokerOptions` stays unused until A58 slot binder exists. `USE_REAL_MT5=true` does **not** require `RemoteUrl` (A58: that `[Required]` is wrong for local mode).

### 6.2 Layer 2 — seeder must not build a second Fake graph

**File:** `DemoSeeder.cs`.

Do **not** have the seeder read `Environment.GetEnvironmentVariable("USE_REAL_MT5")`. That couples tests to the developer’s shell.

Change the signature (when implementation is authorized):

```csharp
public static async Task SeedAsync(
    TraderDbContext db,
    ITradingStore store,
    ReconstructionScoringService scoring,
    CancellationToken ct,
    Mt5SeedMode mode = Mt5SeedMode.DemoFakeTape)
```

| `Mt5SeedMode` | Behavior |
|---|---|
| `DemoFakeTape` | Current body (CreateDefault + year ingest + score). Tests + demo hosts. |
| `CatalogOnly` | Brokers / instrument / FIX `Disconnected` / kill-switch. **No** `CreateDefault`. **No** `SyncBrokerAsync`. |
| `RefuseFake` | If `Brokers` empty: throw `InvalidOperationException("USE_REAL_MT5=true refuses DemoSeeder Fake tape.")`. If brokers already exist: return (do not top-up Fake). |

Hosts:

```csharp
var mode = Mt5Mode.ParseUseReal(builder.Configuration)
    ? Mt5SeedMode.RefuseFake
    : Mt5SeedMode.DemoFakeTape;
await DemoSeeder.SeedAsync(db, store, scoring, ct, mode);
```

Better (C05 recommended fix, still later): delete seeder’s `CreateDefault` / `new DealIngestionService` entirely. Demo tape ingest goes through the **container** `DealIngestionService`. Then Layer 1 is the only constructor. `RefuseFake` becomes “do not call seed” or “catalog only.”

Minimum for the assigned question: **`CreateDefault` must be unreachable when the host parsed `USE_REAL_MT5=true`.**

Do **not** keep seeding live-shaped `Server = 57.128.141.65` as if that were a connect. Catalog paint is a separate honesty bug (C42). Refuse-Fake can leave catalog rows for now; it must not write `mt5_deals` from the June 2026 tape.

### 6.3 Layer 3 — runtime type guard (belt)

Even after Layers 1–2, a future test host or a mistaken `AddSingleton<IMt5BrokerConnector>(new Fake…)` under the flag must die on first use.

| Site | Check |
|---|---|
| `BrokerRegistry` ctor | if use-real && any `is FakeMt5BrokerConnector` → throw |
| `BrokerRegistry.Get` / `All` | same |
| `DealIngestionService.SyncBrokerAsync` after `registry.Get` | if use-real && `connector is FakeMt5BrokerConnector` → throw **before** `ConnectAsync` |

Pass the bool via a tiny options type (`Mt5ModeOptions { bool UseRealMt5 }`) registered singleton from `AddTraderIntelligence`. Do **not** pass it through Domain. Do **not** put `USE_REAL_MT5` on `IMt5BrokerConnector` (the port stays broker-agnostic).

`ConnectAsync` on Fake flipping `_connected = true` must never run in this mode. The type check is the refuse; do not “fix” Fake to throw on connect when the flag is on — Fake must not be in the graph.

### 6.4 Honesty surfaces (same increment or immediately after)

These are not constructors, but they **re-create the lie** if left as-is:

| Site | Today | Under `USE_REAL_MT5=true` |
|---|---|---|
| `apps/api/Program.cs` L26–33 `/api/health` | `healthy = true`, details admit Fake | Process should already have failed start. If it did not, `healthy` **must be false**. |
| `EfDashboardQueries.GetBrokersAsync` | `Connected = true` literal | Must call `IsConnectedAsync` on a **non-Fake** connector, or `false`. |
| `OverviewDto` `brokers > 0` as “MT5 up” (L39) | Catalog non-empty ⇒ true | Catalog ≠ session. Do not use broker row count as G01. |
| Worker log “MT5 ingestion worker started” | Implies a collector | Log `USE_REAL_MT5` and connector **type name**. |

A77: `/ready` must **not** require live Manager (same reason FIX `/ready` must not require `REAL_COPY`). Fail-closed for Fake is a **start** failure, not a 503 flap every 30 s. Once a real connector exists, `/health` may report `mt5.connected=false` without killing the process (A53 stay-alive). That is later.

---

## 7. Recommended implementation sequence (not applied)

Do this as **one** authorized increment. Do not land Layer 1 without Layer 2 — the split graph would keep seeding Fake into a “real” host.

| Step | Change | Pass criterion |
|---|---|---|
| S0 | Add `Mt5Mode` parse helper + `Mt5ModeOptions` (Application or Infrastructure). Default `UseRealMt5=false`. | Unit: parse table in §5.2. No product behavior change yet if unused. |
| S1 | `AddTraderIntelligence`: branch in §6.1. `TryCreateAll` returns empty. | `USE_REAL_MT5=true` → `Build()` / first resolve **throws** with message containing `refuses Fake`. `unset` → current demo DAG. |
| S2 | `DemoSeeder` grows `Mt5SeedMode`. Hosts pass parsed mode. Tests pass `DemoFakeTape` explicitly. | Seeder under `RefuseFake` never calls `CreateDefault` (grep + test). `SeedingAndStoreTests` still green. |
| S3 | Registry / ingestion type guard. | Injecting Fake under the flag throws even if S1 is bypassed in a test `ServiceCollection`. |
| S4 | Health / `Connected` literals. | No `healthy=true` Fake path when flag true (vacuous if S1 throws). Demo path may keep the current honest details string. |
| S5 | Tests in §8. | All new facts green. Existing integration facts still use Fake. |
| S6 | Docs / `.env.example` later: `USE_REAL_MT5=false` literal. Never commit `true`. | Placeholder-only (A75). |
| S7 | **Only after** a real `IMt5BrokerConnector` exists: `TryCreateAll` returns it; A58 slot binder; G01 still measured separately. | Flag true + real passwords + `ConnectAsync` success is the **first** time “real MT5” may be claimed. |

Do **not** do S7 in the refuse increment. A gate that cannot yet open is the correct safety state.

Do **not** add DotNetEnv in this increment. Document process-env wiring instead.

Do **not** move Fake under `tests/` in this increment (A79). Placement cleanup is a different edit. The refuse is behavioral.

---

## 8. Tests that must exist before calling the gate DONE

None of these exist. `AddTraderIntelligence` is **untested** (D23 §8).

| Class / fact (suggested) | Arrange | Assert |
|---|---|---|
| `UseRealMt5ParseTests` | table of strings | §5.2 |
| `DiRefusesFakeWhenUseRealTrue` | `ServiceCollection` + config `USE_REAL_MT5=true` + `AddTraderIntelligence` | `Build()` or `GetRequiredService<IBrokerRegistry>()` throws `InvalidOperationException`; message mentions Fake; `GetServices<IMt5BrokerConnector>()` is empty **or** never reached |
| `DiAllowsFakeWhenUseRealUnset` | empty config | registry `All()` are `FakeMt5BrokerConnector`; codes `ACHIEVER` / `STARWAVEFX` |
| `DiConflictUseRealAndUseDemoData` | both true | throw (conflict) |
| `SeederRefusesFakeWhenModeRefuse` | InMemory db, `Mt5SeedMode.RefuseFake` | throw **before** any `Mt5Deals` row; `CreateDefault` not required if we assert deal count 0 + exception |
| `SeederDemoTapeStillWorks` | current `SeedingAndStoreTests` + explicit `DemoFakeTape` | existing asserts (2 brokers, 10001 has 3 XAU, 10002 `RISK_BLOCKED`) |
| `RegistryRejectsFakeUnderUseReal` | `new BrokerRegistry(new[] { new FakeMt5BrokerConnector("ACHIEVER") })` with options true | ctor or `Get` throws |
| `IngestionRefusesFakeUnderUseReal` | `DealIngestionService` + Fake registry + options true | `SyncBrokerAsync` throws; store deal count 0 |
| `SettingsCannotRaiseUseReal` | (later, if a settings key is added) | PUT ignored / 403 |

Integration tests **must not** set `USE_REAL_MT5=true` in the fixture environment and then call today’s `SeedAsync` — that is how CI goes red for the wrong reason. Pass the enum.

Unit project does not reference Infrastructure / Mt5 today (C17). DI tests belong in **Integration** (or a new `tests/Composition` project). Do not add an Infrastructure reference to Unit just for this gate.

---

## 9. Operator wiring (after the gate exists)

Because hosts already bind process env into `IConfiguration`:

| How the flag reaches C# | Works after S1? |
|---|---|
| `set USE_REAL_MT5=true` then `dotnet run` | **Yes** |
| `launchSettings.json` `environmentVariables.USE_REAL_MT5` | **Yes** |
| compose `environment: USE_REAL_MT5: "true"` | **Yes** (compose API service today sets only `ASPNETCORE_ENVIRONMENT`) |
| User / Machine env | **Yes** if the process inherits it |
| `appsettings.json` `"USE_REAL_MT5": true` or `"Mt5": { "UseRealMt5": true }` | **Yes** if written. **Do not commit `true`.** |
| Gitignored `D:\Prop\.env` line 117 | **No**, until something loads the file (out of scope) |

**Today (before S1):** every row above except the unread `.env` line is also a no-op, because C# never reads the name. Current boot is **always Fake**, including if an operator already exported the variable (they have not — process name **ABSENT** this pass).

If S1 lands while a developer has `USE_REAL_MT5=true` in User env, **every** `dotnet run` of api / workers will fail start. That is correct. Tests that call `AddTraderIntelligence` with the ambient configuration would also fail — another reason tests must pass an explicit `IConfiguration` instance, not `new ConfigurationBuilder().AddEnvironmentVariables()`.

---

## 10. What this plan does **not** authorize

- Implementing `Mt5ManagerBrokerConnector`, P/Invoke, or `MT5APIManager64.dll` load.
- Adding DotNetEnv / `AddUserSecrets`.
- Printing or relocating password material from `.env`.
- Enabling `REAL_COPY_EXECUTION_ENABLED` or any `35=D` path.
- Treating seeder IPs / manager logins as a live attach.
- Overwriting `SeedingAndStoreTests` to require live MT5.
- Renaming Fake to `InMemoryMt5BrokerConnector` and registering it under the real flag.
- Claiming G01 / §69 item 1 after the throw exists.
- Hand-writing MQ5 or touching `D:\Ex5 Decompile`.

Live copy remains **SAFE_BY_ABSENCE** (E002). Live Manager remains **NOT PROVEN** (C42). This plan only removes the path where an operator can write `USE_REAL_MT5=true` and still ingest `DemoBrokerFactory` tickets.

---

## 11. Answers to the assigned question

| Question | Answer |
|---|---|
| What does DemoSeeder do with Fake? | On first empty `brokers` table it `CreateDefault()`s two Fakes, `new`s a registry + `DealIngestionService`, and upserts the year-2026 canned tape, then scores four hard-coded logins. It ignores the container and ignores all flags. |
| What does DI do with Fake? | `AddTraderIntelligence` **always** registers those two Fakes as the only `IMt5BrokerConnector`s. No env branch. |
| Does `USE_REAL_MT5` exist in product C#? | **No.** |
| Does anything already refuse Fake? | **No.** |
| How should Fake be refused when `USE_REAL_MT5=true`? | Fail-closed at **registration** (do not construct), at **seed** (do not `CreateDefault` / do not ingest tape), and at **runtime** (type-check). If no real implementor exists, **throw at host start**. |
| What happens if that plan is applied today? | Every host with the flag actually bound **refuses to start**. That is the honest result. Demo without the flag is unchanged. |
| Product source changed by R003? | **No.** |

### One-liner

`USE_REAL_MT5=true` must mean “this process may not construct, register, seed, or ingest through `FakeMt5BrokerConnector`.” Today the name is unread, DI and `DemoSeeder` both call `DemoBrokerFactory.CreateDefault()`, and the only implementor is Fake — so the refuse is **MISSING**, and the silent-Fake boot is the lie to close.

*End of R003. Product source was not modified.*
