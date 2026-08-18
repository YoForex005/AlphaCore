# E014 — Broken `ReconstructedTradesConfiguration` is **deleted**

| Field | Value |
|---|---|
| Agent | E014 (senior engineer, bad EF config deletion confirm only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:49:37+05:30 (2026-08-18T08:19:37Z) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` / India Standard Time |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\E014_bad_config.md` |
| Assigned | Confirm broken `ReconstructedTradesConfiguration` deleted. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| `src/` / `apps/` / `tests/` / `mt5-sdk/` edited | **No.** |
| Git commit / restore / recreate of the file | **No.** |
| Law | Architecture v2 §§10, 45, 73.B; A61 EF contract; A20 catalog; A78 singular-entity law |
| Prior measure | B26 (HEAD plural configs), D47 (CS0246 while transient file existed), D51 §9 (file appeared ~13:37 and vanished ~13:39:33), D55 (stale list that still named the path) |
| Subject path | `D:\Prop\src\Infrastructure\Persistence\Configurations\ReconstructedTradesConfiguration.cs` |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

This is a **read-only confirmation**. It does not restore the file, invent `class ReconstructedTrades`, add `IEntityTypeConfiguration<T>`, or rewrite `TraderDbContext`.

---

## 0. Verdict (binding)

**CONFIRMED DELETED.** The broken `ReconstructedTradesConfiguration` is **not on disk**, **never existed in git**, and is **not compiled into** the current Infrastructure assembly.

| Claim | Measured now |
|---|---|
| `Configurations\ReconstructedTradesConfiguration.cs` exists | **No.** `Test-Path` = `False` at 13:48:29 and again at 13:49:37 |
| Any `*ReconstructedTradesConfiguration*` under `D:\Prop` (including `bin/` `obj/`) | **0 files** |
| Path ever tracked by git | **No.** `git ls-files --error-unmatch` → pathspec not known to git. `git log --all` empty. `git rev-list --all --objects` has **no** object with that name |
| HEAD `Configurations/` | **5** plural stubs only (`Brokers`, `Mt5Accounts`, `Mt5Deals`, `Mt5Groups`, `Mt5Positions`). **Not** this file |
| Working-tree `Configurations/` | Directory **exists**, **0** children. LastWrite **2026-08-18T13:39:33.3471179+05:30** (matches D51 vanish time) |
| Product `IEntityTypeConfiguration` / `ApplyConfiguration` / `class ReconstructedTrades` | **0 hits** under `D:\Prop\src` `*.cs` |
| Worktree `TraderDbContext` | `DbSet<ReconstructedTrade>` + **inline** `modelBuilder.Entity<ReconstructedTrade>` (lines 17, 73–78). **No** `ApplyConfiguration` |
| `dotnet build` Infrastructure Release | **exit 0.** 0 Warning(s). 0 Error(s). 0.78 s |
| A61 replacement `ReconstructedTradeConfiguration.cs` (singular) | Still **MISSING**. Deletion of the broken plural file is **not** the A61 map |

**Honest one-liner:** the CS0246 file D47 compiled against is gone; the compile is green **because the broken class is absent**, not because a correct `IEntityTypeConfiguration<ReconstructedTrade>` landed.

Do **not** `git checkout` anything to “put the config back.” HEAD never contained this file. Restoring the five **other** deleted HEAD configs would re-introduce a different CS0246 set (`Brokers`, `Mt5Deals`, …). Do **not** treat a green Infrastructure build as “EF configuration layer exists.”

---

## 1. Method (read-only)

1. `Test-Path` on the directory and on `ReconstructedTradesConfiguration.cs`.
2. `Get-ChildItem -Force` of `Configurations\` (hidden included).
3. Recursive `*ReconstructedTradesConfiguration*` under `D:\Prop` (no `bin`/`obj` exclusion on the second pass).
4. `git ls-files`, `git ls-tree HEAD`, `git status --short`, `git ls-files --deleted`, `git log --all`, `git rev-list --all --objects`, `git ls-files --error-unmatch` for that path.
5. `git show HEAD:src/Infrastructure/Persistence/TraderDbContext.cs` for the **name** `ReconstructedTradesConfiguration` (a constructor call, not a file).
6. Grep / `Select-String` on product `*.cs` for `IEntityTypeConfiguration`, `ApplyConfiguration`, `ApplyConfigurationsFromAssembly`, `ReconstructedTradesConfiguration`, `class ReconstructedTrades`.
7. SHA-256 + bytes + LastWrite of current `TraderDbContext.cs` and `ReconstructedTrade.cs`.
8. `dotnet build D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj -c Release` (measurement only; no source edit).
9. Cross-check D47 (compile fail), D51 §9 (transient file), B26 (HEAD never had this class file), D55 (stale inventory), A61 §8.12 / file list (singular replacement name).

No file under `src/`, `apps/`, `tests/`, or `mt5-sdk/` was written, deleted, or restored by this agent.

---

## 2. Disk now

| Path | Present | Notes |
|---|---|---|
| `D:\Prop\src\Infrastructure\Persistence\Configurations\` | **Yes** (empty directory) | Attributes=`Directory`. Length reported as 1 (NTFS directory). LastWrite=`2026-08-18T13:39:33.3471179+05:30` |
| `...\Configurations\ReconstructedTradesConfiguration.cs` | **No** | `Test-Path` false |
| Children (files) | **0** | `-Force` included |
| Children (subdirs) | **0** | |
| Any `*ReconstructedTradesConfiguration*` under `D:\Prop` | **0** | Includes `bin/`, `obj/`, `reports/` scratch trees |

Infrastructure product files on disk (exclude `bin/` `obj/`):

| Path | Bytes | LastWrite (+05:30) |
|---|---:|---|
| `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | 1035 | 13:15:01 |
| `src\Infrastructure\DependencyInjection.cs` | 1900 | 13:14:18 |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 13:35:15 |
| `src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | 13:12:48 |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | 12097 | 13:35:59 |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | 5082 | 13:34:59 |

**Zero** `*Configuration.cs` under `src` outside `bin/`/`obj/`. The empty `Configurations\` folder is a leftover directory, not a map.

---

## 3. Git now

`HEAD` = `398a14200ec65714c4077eed55c46808382ca1e3` (2026-08-18 13:24:21 +0530). Branch `main`, up to date with `origin/main`.

`git ls-files -- src/Infrastructure/Persistence/Configurations`:

```text
src/Infrastructure/Persistence/Configurations/BrokersConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5AccountsConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5DealsConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5GroupsConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5PositionsConfiguration.cs
```

Those five are **`D`** in the working tree (deleted on disk, still in HEAD). They are the B26 plural-type stubs (`IEntityTypeConfiguration<Brokers>` etc.). They are **not** `ReconstructedTradesConfiguration`.

`git ls-tree -r HEAD` for that folder matches the same five blobs (`3c750cef…`, `9882f5da…`, `7f4544d7…`, `6d5061c4…`, `d1adf889…`). **No sixth blob.**

```text
git ls-files --error-unmatch src/Infrastructure/Persistence/Configurations/ReconstructedTradesConfiguration.cs
→ error: pathspec '…ReconstructedTradesConfiguration.cs' did not match any file(s) known to git
```

`git log --all -- **/ReconstructedTradesConfiguration.cs` = **empty**. The path was never added, never committed, never deleted through git.

HEAD `TraderDbContext` (blob `6950c9373def993100f69e14210c02d016212883`) **names** the missing class:

```csharp
modelBuilder.ApplyConfiguration(new ReconstructedTradesConfiguration());
public DbSet<ReconstructedTrades> ReconstructedTrades => Set<ReconstructedTrades>();
```

That is a **constructor reference**, not a file. B26 already classified this as one of 15 `new XxxConfiguration()` types that were **never created**. HEAD therefore cannot compile; the worktree rewrite dropped every `ApplyConfiguration` call.

Working-tree porcelain on Persistence (unchanged by this agent):

```text
 D src/Infrastructure/Persistence/Configurations/BrokersConfiguration.cs
 D src/Infrastructure/Persistence/Configurations/Mt5AccountsConfiguration.cs
 D src/Infrastructure/Persistence/Configurations/Mt5DealsConfiguration.cs
 D src/Infrastructure/Persistence/Configurations/Mt5GroupsConfiguration.cs
 D src/Infrastructure/Persistence/Configurations/Mt5PositionsConfiguration.cs
 M src/Infrastructure/Persistence/TraderDbContext.cs
?? src/Infrastructure/Persistence/EfTradingStore.cs
```

No `?? …ReconstructedTradesConfiguration.cs`. No staged add. No ignored copy of that name.

---

## 4. What the broken file was (when it briefly existed)

D47 measured a compile **fail** at **2026-08-18T13:38:00+05:30**:

| File | Error |
|---|---|
| `Persistence\Configurations\ReconstructedTradesConfiguration.cs` | CS0246 `ReconstructedTrades` not found (**twice**) |
| `TraderDbContext.cs:28` | CS0246 `DestinationQuoteSnapshot` not found (separate mid-wave rewrite) |

D51 §9 recorded the same transient file and its disappearance:

| Item | D51 measurement |
|---|---|
| Path | `D:\Prop\src\Infrastructure\Persistence\Configurations\ReconstructedTradesConfiguration.cs` |
| Window | ~13:37:35 last write → **gone by ~13:39:33** |
| Size | **1772** bytes |
| SHA-256 | `E9581103DE593B4087AA24A63D6D1DD402E39292F4706957CBC994CC4589373B` |
| Type argument | `IEntityTypeConfiguration<ReconstructedTrades>` |
| Domain type that exists | `ReconstructedTrade` (singular) — `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` |
| Shape | `deal_ticket` unique; `volume` as `decimal` mapped `bigint`; `DateTime created_at` |
| Wired into `OnModelCreating`? | **No.** Worktree context has no `ApplyConfiguration` / `ApplyConfigurationsFromAssembly` |

That SHA-256 is a **file hash from D51**, not a git blob. `git rev-list --objects` does not contain it. This agent **did not** re-read those bytes — they are gone. The D51 description is enough to classify the file as **UNSAFE** / type-**MISSING** had it remained.

Why it was broken (even if someone restored those 1772 bytes):

1. **Wrong CLR name.** Domain has `public sealed class ReconstructedTrade`. There is **no** `class ReconstructedTrades`. SDK-style csproj compiles every `.cs` under the project, so the file is a CS0246 even if `OnModelCreating` never calls it.
2. **Wrong identity.** A reconstructed trade is a **position lifecycle**, not a deal. Unique `deal_ticket` is the A78 `mt5_deals` law, not A61 §8.12.
3. **Volume type fight.** `decimal` CLR + `bigint` store is the same defect as HEAD `Mt5DealsConfiguration`. Domain `ReconstructedTrade` uses `decimal` lots (`InitialVolumeLots` / `MaxVolumeLots` / `ClosedVolumeLots`). A61 wants a **native bigint** decision, not a CLR/store clash.
4. **`DateTime created_at`.** Domain uses `DateTimeOffset OpenedAt` / `ClosedAt`. A61 forbids the legacy `DateTime` + timestamp switch.
5. **Dead even if the type existed.** Worktree `TraderDbContext` last write is **13:12:48** — **before** the transient file — and maps inline. The class would not have been applied.

HEAD `BrokersConfiguration` / `Mt5DealsConfiguration` show the same plural + shadow-property pattern the transient file followed. Those five remain deleted in the worktree on purpose (B26: do not restore).

---

## 5. What maps `ReconstructedTrade` now

Worktree `TraderDbContext.cs`: **5951** bytes, **174** lines, SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (unchanged vs B21/D19). LastWrite `2026-08-18T13:12:48.0601582+05:30`.

```csharp
public DbSet<ReconstructedTrade> ReconstructedTrades => Set<ReconstructedTrade>();

modelBuilder.Entity<ReconstructedTrade>(e =>
{
    e.ToTable("reconstructed_trades");
    e.HasKey(x => x.Id);
    e.HasIndex(x => new { x.BrokerId, x.Login, x.PositionId, x.OpenedAt });
});
```

Domain `ReconstructedTrade.cs`: **1430** bytes, **36** lines, SHA-256 `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014`.

Consumers (`EfTradingStore.ReplaceReconstructedAsync`, `EfDashboardQueries`) use the `DbSet` property name `ReconstructedTrades` against type `ReconstructedTrade`. That property name is legal. The **type** `ReconstructedTrades` is what must stay missing.

This inline map is **not** the deleted file and **not** A61:

| Contract (A61 §8.12 / A20) | Worktree now |
|---|---|
| File `ReconstructedTradeConfiguration.cs` | **MISSING** |
| `IEntityTypeConfiguration<ReconstructedTrade>` | **MISSING** |
| UNIQUE `(broker_id, position_id)` named `reconstructed_trades_position_uk` | **No.** Non-unique index on `(BrokerId, Login, PositionId, OpenedAt)` |
| `HasDatabaseName` | **0** |
| `HasForeignKey` | **0** |
| snake_case convention | **0** (`EFCore.NamingConventions` not referenced) |
| `ApplyConfigurationsFromAssembly` | **0** |

Class of the **current** inline map: `EXISTS_NEEDS_REFACTOR` (compiles; identity is wrong vs A61). Class of the **deleted** plural file: `UNSAFE` + type `MISSING`. Class of the A61 split config: still `MISSING`.

---

## 6. Compile evidence (after deletion)

Command (this pass, no source writes):

```text
dotnet build D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj -c Release --nologo
```

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:00.78
build_exit=0
```

`TraderIntelligence.Infrastructure -> D:\Prop\src\Infrastructure\bin\Release\net8.0\TraderIntelligence.Infrastructure.dll`

D47’s CS0246 on this filename **cannot reproduce** because the file is gone. A second `Test-Path` immediately before that build was still `False`. `Configurations` child count = **0**.

Green compile **depends on the file staying deleted.** Re-adding `IEntityTypeConfiguration<ReconstructedTrades>` would restore CS0246 on every SDK-style build.

---

## 7. Stale reports (do not cite as current)

| Report | What it said | Status vs this measure |
|---|---|---|
| B26 (13:21) | `ReconstructedTradesConfiguration` **MISSING** from HEAD (one of 15 never-created classes). Worktree `Configurations/` empty | **Still true** for git. Transient file came **after** B26 |
| D47 (13:38) | Compile **FAIL** CS0246 on this exact path | **True at 13:38.** **Stale now** — file deleted |
| D51 §9 (~13:40, closed after 13:39:33) | Transient 1772-byte file vanished; second build green | **Holds.** Directory LastWrite still 13:39:33 |
| D55 (report stamp 13:40:55) | Listed `Persistence/Configurations/ReconstructedTradesConfiguration.cs` among Infrastructure product `.cs` | **Stale.** Directory mtime is 13:39:33; this pass finds **0** such files. D55’s Redis verdict does not depend on the filename |

Use **this file** for “is the broken config still there?” Use D51 only as the witness of the ~2-minute appearance.

---

## 8. What must not be done

1. **Do not** recreate `ReconstructedTradesConfiguration.cs` with `IEntityTypeConfiguration<ReconstructedTrades>`.
2. **Do not** add Domain type `ReconstructedTrades` to satisfy the old name. A61 / A78: entity names are singular (`ReconstructedTrade`).
3. **Do not** `git checkout -- src/Infrastructure/Persistence/Configurations`. That restores the **other** five broken plural stubs, not this file, and re-breaks the build.
4. **Do not** treat deletion as A61 progress. The required file name is `ReconstructedTradeConfiguration.cs` (`IEntityTypeConfiguration<ReconstructedTrade>`), applied via `ApplyConfigurationsFromAssembly`.
5. **Do not** unique `deal_ticket` on `reconstructed_trades`. That identity belongs on `mt5_deals`.
6. **Do not** map `volume` as `decimal` stored as `bigint`.
7. **Do not** count the empty `Configurations\` directory as a configuration layer.
8. This agent **did not** delete the file. Deletion is pre-existing worktree state (directory mtime 13:39:33). This report only confirms it.

---

## 9. Honesty / classification

| Item | Class | Evidence |
|---|---|---|
| On-disk `ReconstructedTradesConfiguration.cs` | **DELETED** / absent | §2 `Test-Path` false; 0 recursive hits |
| Git history of that path | **never existed** | §3 |
| HEAD `new ReconstructedTradesConfiguration()` | `UNSAFE` (committed context only) | HEAD blob `6950c937…`; worktree no longer calls it |
| Transient 13:37–13:39 file | `UNSAFE` + type `MISSING` | D47 CS0246; D51 SHA `E9581103…`; gone |
| Worktree empty `Configurations/` | `MISSING` vs A61 (43 files) | 0 children |
| Worktree inline `Entity<ReconstructedTrade>` | `EXISTS_NEEDS_REFACTOR` | §5 — table name ok; UNIQUE wrong |
| A61 `ReconstructedTradeConfiguration.cs` | `MISSING` | A61 file list line “ReconstructedTradeConfiguration.cs” |
| Infrastructure Release compile | **PASS** (0/0) | §6 — **because** the broken file is gone |
| §45 / A61 complete model | `MISSING` | D19 18/43 names; 0 split configs; 0 migrations |

**Assigned question:** is the broken `ReconstructedTradesConfiguration` deleted?

**Answer: Yes.** Confirmed on disk, in git, and by a clean Infrastructure Release build. The A61 singular replacement was **not** created. Product source was **not** modified by this agent.
)
