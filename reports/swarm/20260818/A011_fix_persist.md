# A011 — FIX persist: does `CTraderFixLogonHostedService` actually find existing `FixSessionState` rows?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A011_fix_persist.md` |
| Agent | A011 (FIX persist / `TraderDbContext` reflection) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned | Read `CTraderFixLogonHostedService.cs` and `FixSessionState.cs`. Does persist actually find existing rows? Reflection over all types to find `TraderDbContext` is fragile. No `35=D`. No secrets. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secret values printed | **None.** Password slots named only (`CTRADER_FIX_PASSWORD`). Tag 554 is not quoted. |

**Honesty rule:** a `SaveChangesAsync` after a loop that `continue`s on `row is null` is **not** an upsert. Finding `TraderDbContext` by **unqualified type name** across `AppDomain.CurrentDomain.GetAssemblies()` is **not** DI. A log line that says `NewOrderSingle still disabled` is **not** a `35=D` builder. Seeded `fix_sessions` rows in the **same process** InMemory database are the only measured reason persist can succeed.

Classification vocabulary: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `SAFE_BY_ABSENCE`.

---

## 0. Verdict (binding)

**Persist is update-only. It can find the two seeded rows when they already exist in the scoped `TraderDbContext`. It never inserts. Reflection is the wrong way to reach that context. No `35=D`.**

| Assigned question | Measured answer |
|---|---|
| Does persist actually find existing rows? | **Conditionally yes.** `FirstOrDefaultAsync(s => s.Qualifier == result.Qualifier)` will hit the unique `Qualifier` index **if** `DemoSeeder` (or a prior Postgres row) already inserted Quote/Trade. If the lookup returns null, persist **skips** that qualifier. There is no `Add`. |
| Is the reflection lookup of `TraderDbContext` sound? | **No. Fragile.** `GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == "TraderDbContext")` is name-only, not namespace-qualified, and `GetTypes()` can throw `ReflectionTypeLoadException` on any loaded assembly. That exception is swallowed; persist never runs. |
| Does persist send `35=D`? | **No.** Session builder emits `(35, "A")` only. Persist writes columns. **`SAFE_BY_ABSENCE`.** |
| Secrets in this report? | **None.** |

| Slice | Class |
|---|---|
| `PersistAsync` lookup by `Qualifier` | `EXISTS_NEEDS_REFACTOR` — correct predicate, **update-only** |
| Insert-if-missing / upsert | **`MISSING`** |
| Typed `TraderDbContext` injection | **`MISSING`** (cycle avoided by reflection instead of an Application-layer store) |
| `GetAssemblies` / `GetTypes` / `Name == "TraderDbContext"` | **`UNSAFE`** as a resolution strategy (silent fail, name collision, load-order) |
| Seeded rows (`DemoSeeder` Quote+Trade) | `EXISTS_AND_GOOD` as the **only** rows persist can update on first start |
| `BrokerCatalogSeed.EnsureAsync` FIX rows | **Dead** — type exists, **0 callers** |
| `35=D` / NewOrderSingle send | **`SAFE_BY_ABSENCE`** |
| Persist as proof of live session | **Not proven.** Worker loop restamps `Disconnected` every 15 s. |

One-liner:

```text
PERSIST = UPDATE-ONLY BY Qualifier
FINDS DemoSeeder ROWS WHEN PRESENT; NEVER INSERTS
REFLECTION GetTypes() Name=="TraderDbContext" IS FRAGILE
NO 35=D
```

---

## 1. What was read (product only)

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | logon host + persist |
| `D:\Prop\src\Domain\Entities\FixSessionState.cs` | row shape; **no password column** |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | TLS + `35=A` only |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | `DbSet<FixSessionState>` → table `fix_sessions`; unique `Qualifier` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | registers hosted service + `AddDbContext<TraderDbContext>` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | now **does** `ProjectReference` Fix.CTrader |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | Domain + Application only — **no** Infrastructure |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | inserts the two rows persist expects |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | unused alternate insert |
| `D:\Prop\apps\api\Program.cs` | `EnsureCreated` + `DemoSeeder` then `app.Run` |
| `D:\Prop\apps\fix-worker\Program.cs` | same seed, then `host.Run` |
| `D:\Prop\apps\mt5-worker\Program.cs` | same — **also** starts the FIX logon host |
| `D:\Prop\apps\fix-worker\Worker.cs` | 15 s loop **overwrites** the same two rows to `Disconnected` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dashboard reads the same set |
| `D:\Prop\src\Domain\Enums\FixSessionQualifier.cs` | `Quote=0`, `Trade=1` |
| `D:\Prop\src\Domain\Enums\FixSessionStatus.cs` | `Disconnected` … `Error` |

No `.env` values, no user-secrets, no password bytes.

---

## 2. Persist as written

```56:95:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        try
        {
            using var scope = _scopes.CreateScope();
            var dbType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "TraderDbContext");
            if (dbType is null)
                return;
            var db = scope.ServiceProvider.GetService(dbType);
            if (db is null)
                return;
            await PersistAsync(db, quote, trade, host, stoppingToken);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not persist FIX session rows");
        }
    }

    private static async Task PersistAsync(object db, CTraderFixSessionResult quote, CTraderFixSessionResult trade, string host, CancellationToken ct)
    {
        if (db is not DbContext ctx)
            return;
        var set = ctx.Set<TraderIntelligence.Domain.Entities.FixSessionState>();
        foreach (var result in new[] { quote, trade })
        {
            var row = await set.FirstOrDefaultAsync(s => s.Qualifier == result.Qualifier, ct);
            if (row is null)
                continue;
            row.Host = host;
            row.Port = result.Qualifier == FixSessionQualifier.Quote ? 5211 : 5212;
            row.Status = result.LoggedOn ? FixSessionStatus.LoggedOn : FixSessionStatus.Error;
            row.LastError = result.LastError;
            row.LastInboundAt = DateTimeOffset.UtcNow;
            row.LastOutboundAt = DateTimeOffset.UtcNow;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await ctx.SaveChangesAsync(ct);
    }
```

### 2.1 What the lookup keys on

`FixSessionState` identity is `Guid Id`. Session identity for persist is **`Qualifier`** (`Quote` / `Trade`). That matches the EF unique index:

```155:160:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
        modelBuilder.Entity<FixSessionState>(e =>
        {
            e.ToTable("fix_sessions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Qualifier).IsUnique();
        });
```

`FirstOrDefaultAsync` on a unique `Qualifier` is the right predicate **for an update**. It is not a find-or-create.

### 2.2 What is written vs what the entity can hold

| Column | Persist writes? | Note |
|---|---|---|
| `Id` | No | seed Guid kept |
| `Qualifier` | No (filter only) | |
| `Status` | Yes | `LoggedOn` or **`Error` only** — catch-path `Disconnected` from `CTraderFixSessionResult` is **collapsed to Error** |
| `Host` | Yes | from config / default hostname (identifier, not a secret) |
| `Port` | Yes | hardcoded 5211 / 5212 |
| `SenderCompId` / `TargetCompId` / SubIDs | **No** | seed values remain |
| `InboundSeq` / `OutboundSeq` | **No** | session uses local `seq = 1`; never stored |
| `LastInboundAt` / `LastOutboundAt` / `UpdatedAt` | Yes | `UtcNow` even if logon failed |
| `ReconnectCount` | **No** | |
| `LastError` | Yes | exception **type + message** (no password field on the entity) |
| `OwnerHeld` / `OwnerInstance` | **No** | ownership not persisted |

Entity has **no password property**. Persist cannot leak tag 554 into the table.

---

## 3. Does persist actually find existing rows?

### 3.1 Happy path (same process, first seed)

Hosts that call `AddTraderIntelligence` also seed **before** `Run`:

1. `EnsureCreatedAsync`
2. `DemoSeeder.SeedAsync` — if `!Brokers.Any()`, inserts **two** `FixSessionState` rows (`Quote` id `cccccccc-…ccc1`, `Trade` id `cccccccc-…ccc2`)
3. Hosted service `ExecuteAsync` (after the password gate + `TryLogonAsync`)
4. New scope → `ctx.Set<FixSessionState>()` → `FirstOrDefaultAsync` by `Qualifier`

On that path, **yes: both rows are found and updated.** The InMemory database name is the shared `"trader-intelligence-live"` string, so seed and persist see the same store **inside one process**.

`DemoSeeder` insert (the rows persist is designed to hit):

```68:103:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.FixSessionStates.AddRange(
            new FixSessionState
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                Qualifier = FixSessionQualifier.Quote,
                // ...
            },
            new FixSessionState
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                Qualifier = FixSessionQualifier.Trade,
                // ...
            });
```

### 3.2 Paths where persist finds **zero** rows (and does nothing)

| Condition | What happens |
|---|---|
| `row is null` | `continue` — **no insert** |
| `DemoSeeder` early-return (`Brokers.Any()` true) **and** `fix_sessions` empty | persist no-ops both qualifiers, still calls `SaveChangesAsync` (0 changes) |
| `BrokerCatalogSeed.EnsureAsync` never called | its FIX insert is dead code; cannot save persist |
| Fresh Postgres / empty `fix_sessions` | same skip |
| Password gate fires first | **persist never reached** (see §5) |
| `dbType is null` / `GetService` null / not `DbContext` / `GetTypes` throws | silent `return` or warning; **no write** |

**Persist does not create the rows it depends on.** Finding existing rows is entirely a seed/ops accident.

### 3.3 Independent evidence that the same predicate works

`apps/fix-worker/Worker.cs` uses the **typed** store and the same `Qualifier` filter:

```28:42:D:\Prop\apps\fix-worker\Worker.cs
            var quote = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Quote);
            // ...
            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
```

Dashboard `EfDashboardQueries.GetFixSessionsAsync` / overview also `SingleOrDefaultAsync` / `OrderBy` the same `DbSet`. Those readers **do** find the seeded pair on a demo host. Persist uses an equivalent query through `Set<T>()` rather than `TraderDbContext.FixSessionStates`. When the context instance is the real one, the SQL/InMemory query is the same.

That is **not** proof the reflection path obtained that instance.

### 3.4 Persist vs Worker clobber

Even when persist finds rows and writes `LoggedOn` / `Error`, `Worker` restamps both rows to `FixSessionStatus.Disconnected` every **15 seconds** in the fix-worker process. Dashboard readers after that interval see seed-like disconnected state, not the persist result.

API and mt5-worker do **not** run that 15 s stamp, but they **do** register `CTraderFixLogonHostedService` via `AddTraderIntelligence`. Three processes can each open TLS logon if the password env name is populated. Still no `35=D`.

---

## 4. Why reflection is fragile (and unnecessary)

Fix.CTrader **cannot** take a project reference to Infrastructure: Infrastructure already references Fix.CTrader (`TraderIntelligence.Infrastructure.csproj` line 7). Cycle. Persist therefore talks to EF as `object` / `DbContext`.

What they wrote instead:

```csharp
AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .FirstOrDefault(t => t.Name == "TraderDbContext");
```

### 4.1 Failure modes

| Failure | Effect |
|---|---|
| `Assembly.GetTypes()` throws `ReflectionTypeLoadException` (mixed/native/unloadable types) | entire `SelectMany` throws → catch → **persist skipped** |
| `GetAssemblies()` is **already-loaded only** | if Infrastructure has not been loaded yet, type is null → silent return. Unlikely after `AddTraderIntelligence`, but true in a test host that only new's the service. |
| Match is **`t.Name == "TraderDbContext"`** — no namespace, no assembly | first loaded type with that short name wins. A test double / another assembly with the same name binds wrongly or not at all. |
| Correct type found, `GetService(dbType)` null | silent return (e.g. scope without `AddDbContext`) |
| Resolved object is not `DbContext` | `PersistAsync` returns |
| Walks **every** type in **every** loaded assembly (runtime + NuGet) | cost + exception surface; not a DI lookup |

### 4.2 What should replace it (do not implement in this task)

Define a store on the **Application** side (already referenced by both projects), implement it on `TraderDbContext` in Infrastructure, inject that into `CTraderFixLogonHostedService`.

```text
Application: IFixSessionStateStore { GetByQualifier; UpdateAfterLogon; }  // upsert optional
Infrastructure: EfFixSessionStateStore(TraderDbContext)
Fix.CTrader: inject IFixSessionStateStore — no GetTypes, no object db
```

`Type.GetType("TraderIntelligence.Infrastructure.Persistence.TraderDbContext, TraderIntelligence.Infrastructure")` would be less wrong than `GetTypes()` and is still a hack. Do not keep it.

Infrastructure already has a compile-time reference to the hosted service type. Reflection is not buying a layering win; it is hiding the missing port.

---

## 5. Persist is behind the password gate (and behind logon)

`ExecuteAsync` order:

1. Read `CTRADER_FIX_PASSWORD`. If missing or the value contains the placeholder token `<SECRET>`, **log warning and `return`**. Persist does not run.
2. Build host / sender / target from config with compiled defaults (identifiers, not secrets).
3. `TryLogonAsync` QUOTE **5211**, then TRADE **5212** — each sends **`35=A`**.
4. Log `FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled)`.
5. Then the reflection persist block.

So:

- Local demo with no real password env → **persist never executes.** Seeded `Disconnected` rows stay until Worker overwrites the same text.
- Persist is not a startup health stamp. It is a **post-logon side effect**.

Password is passed into `BuildLogon` as tag **554**. This report does **not** quote that builder’s secret field values.

---

## 6. No `35=D`

`CTraderFixSession.BuildLogon` body tags start with `(35, "A")`. Other tags: 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. There is no `(35, "D")`, no `NewOrderSingle` encoder, no cancel/replace.

The hosted service mentions `NewOrderSingle` **only** in an information log (`still disabled`). Persist writes `Host` / `Port` / `Status` / timestamps / `LastError`.

Product C# still has **no** `35=D` builder (E034). This persist path does not add one.

**Live send if this host starts now:** Logon `35=A` **can** go out when the password slot is a real secret. **`35=D` cannot.** `SAFE_BY_ABSENCE` for NewOrderSingle. Logon is **not** proven by this file (no wire capture here).

---

## 7. Status mapping bug (when a row *is* found)

`CTraderFixSession` catch returns `Status = "Disconnected"` and `LoggedOn = false`. Persist ignores `result.Status` and sets:

```csharp
row.Status = result.LoggedOn ? FixSessionStatus.LoggedOn : FixSessionStatus.Error;
```

A refused TCP/TLS/timeout therefore becomes **`Error`**, not `Disconnected`. Worker later forces `Disconnected` anyway (fix-worker only).

---

## 8. Classification table

| Item | Class |
|---|---|
| Qualifier lookup against unique index | `EXISTS_AND_GOOD` as a query |
| Find **existing** DemoSeeder rows on first InMemory start | **Yes, when persist is reached** |
| Persist without prior seed | **No rows found; no insert** |
| Reflection type hunt | `UNSAFE` / `EXISTS_NEEDS_REFACTOR` |
| Application-layer session store | `MISSING` |
| `BrokerCatalogSeed` as persist’s safety net | `MISSING` (uncalled) |
| Seq / ownership / CompID persist | `MISSING` |
| `35=D` | `SAFE_BY_ABSENCE` |
| Password column / logged password | **Absent** (good) |

---

## 9. Authorized later work (not this task)

1. Replace `GetAssemblies`/`GetTypes` with `IFixSessionStateStore` (Application port, Infrastructure adapter).
2. Upsert on `Qualifier` so persist does not depend on `DemoSeeder`.
3. Map connect-fail → `Disconnected`; logon-reject → `Error`; success → `LoggedOn`. Do not stamp `LastInboundAt` on a failed read.
4. Persist seq + Comp/Sub IDs from the session result, not only host/port.
5. Stop `Worker` from clobbering live persist (or make Worker the only writer, not both).
6. Do **not** register live `TryLogonAsync` in API + mt5-worker unless that is intended. Three hosts × two ports is not single-owner TRADE (§28).
7. Do **not** add `35=D`. Keep the copy flag default false.

---

## 10. Direct answers

**Does persist actually find existing rows?**  
**Only if they already exist.** The query is `FirstOrDefaultAsync` on `Qualifier`. After a full `DemoSeeder` in the same InMemory/Postgres store, Quote and Trade are found and updated. If a row is missing, persist **skips** it — it never inserts. If the password placeholder gate trips, or `GetTypes()` fails, persist does not run at all.

**Is reflection over all types to find `TraderDbContext` fragile?**  
**Yes.** Name-only match, already-loaded assemblies only, `GetTypes()` can throw, failures are silent. Infrastructure already references Fix.CTrader; the missing piece is an Application store interface, not a domain-wide type scan.

**`35=D`?**  
**None** on this path. Session sends `35=A` only. Persist does not encode FIX.

**Secrets?**  
**None printed.** Entity has no password column. Config key name only: `CTRADER_FIX_PASSWORD`.

**Product source edited?**  
**No.**
