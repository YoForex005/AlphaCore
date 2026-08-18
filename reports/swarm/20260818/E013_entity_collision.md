# E013 — `DestinationQuoteSnapshot` restore after entity overwrite

| Field | Value |
|---|---|
| Agent | E013 (senior engineer; entity name-collision only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:04+05:30 (2026-08-18T08:20:04Z) |
| Artifact | `D:\Prop\reports\swarm\20260818\E013_entity_collision.md` |
| Assigned | An agent overwrote the DestinationQuote entity. Confirm `DestinationQuoteSnapshot` restored. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Workspace | `D:\Prop` |
| Subject | `D:\Prop\src\Domain\Entities\DestinationQuote.cs` |
| Twin (must stay distinct) | `TraderIntelligence.Domain.Risk.DestinationQuote` in `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| Prior pins | B01 §4.2–4.3 (CS0104 warning); B18 (entity SHA); B21 §4.1 (filename ≠ type); D01 §1.2; D19; D96 §6 |
| Method | Read the entity file; SHA-256; git blob vs `HEAD`; blame; consumer grep; `dotnet build` Domain + Infrastructure. No edits under `src/`, `apps/`, `tests/`. |

This is a **read-only confirmation**. It does not rename `DestinationQuote.cs`, does not merge the persist type into the risk record, and does not delete the adjacent untracked `RiskDecision.cs`.

---

## 0. Verdict (binding — do not greenwash)

**YES. `DestinationQuoteSnapshot` is restored (present) and is byte-identical to `HEAD`.**

| Assigned claim | Measured result |
|---|---|
| Persist type `DestinationQuoteSnapshot` exists | **TRUE.** `public sealed class DestinationQuoteSnapshot` at `DestinationQuote.cs` L3 |
| File was overwritten to `class DestinationQuote` and left that way | **FALSE on disk now.** No `class DestinationQuote` / `record DestinationQuote` under `Domain\Entities` |
| Current bytes differ from the committed snapshot | **FALSE.** Working-tree blob = `HEAD` blob `ba2cebfd…` |
| Current bytes differ from B18’s pin | **FALSE.** SHA-256 still `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` |
| Risk DTO `DestinationQuote` survived | **TRUE.** Positional record in `RiskEngine.cs` L24–30; SHA-256 still `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| Consumers still compile against the snapshot | **TRUE.** Domain **0/0**, Infrastructure **0/0** (Release) |
| Git has an intermediate commit of the overwrite | **NO.** Only `6c41447` (Initial commit) touches this path. Blame is that commit on every line |
| Filename/type split is gone | **FALSE.** Stem is still `DestinationQuote`; type is still `DestinationQuoteSnapshot` |

**Honest one-liner:** the persist entity is `DestinationQuoteSnapshot` again (or still), matching `HEAD` and B18 exactly. The dangerous rename `Entities.DestinationQuote` is **not** on disk. Git cannot exhibit the overwritten bytes — there is no lost commit — but the 13:37–13:40 entity-edit window did rewrite this file’s timestamp, and a sibling agent in that same window **did** land a live homonym (`Entities.RiskDecision`). Treat Snapshot as **restored / present**; do not treat the filename as safe.

Do **not** claim the two quote types were unified. Do **not** claim `destination_quotes` is a complete A20 upsert. Do **not** claim this ticket cleaned `RiskDecision.cs`.

---

## 1. What was read (no product edits)

| Path | Role | Measured |
|---|---|---|
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | persist entity | **12** lines; **421** bytes; LF only; no BOM (first byte `0x6E` = `n` of `namespace`); LastWriteTime **2026-08-18 13:39:32 +05:30**; CreationTime 13:09:03; SHA-256 `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | in-memory quote DTO + engine | SHA-256 `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` (unchanged vs B18) |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | `DbSet` + `ToTable` | SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (unchanged vs D19) |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | constructs snapshot | SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | maps snapshot → Risk DTO | SHA-256 `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36`; FQCN `TraderIntelligence.Domain.Risk.DestinationQuote` at L281 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | latest quote for `/fix` | SHA-256 `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | prices off Risk DTO | `using Domain.Risk`; parameter type `DestinationQuote` |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | constructs Risk DTO | `new DestinationQuote("XAUUSD", …)` under `using Domain.Risk` |
| `D:\Prop\src\Domain\Entities\RiskDecision.cs` | **adjacent live collision** (not this ticket) | untracked; 409 B; LastWriteTime **13:37:39**; `public sealed class RiskDecision` |
| `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs` | the persist type DbContext actually uses | tracked; `DbSet<RiskDecisionRecord>` |
| `D:\Prop\src\Domain\Entities\DestinationQuoteSnapshot.cs` | would-be matching filename | **ABSENT** (`Test-Path` false) |

`git status --porcelain -- src/Domain/Entities/DestinationQuote.cs` is **empty**. Working tree matches `HEAD`.

---

## 2. On-disk type (the restore)

Exact file, 12 lines, LF, 421 bytes (includes `VenueInstrumentId`):

```1:12:D:\Prop\src\Domain\Entities\DestinationQuote.cs
namespace TraderIntelligence.Domain.Entities;

public sealed class DestinationQuoteSnapshot
{
    public Guid Id { get; set; }
    public string CanonicalSymbol { get; set; } = "XAUUSD";
    public string? VenueInstrumentId { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? VenueTimestamp { get; set; }
}
```

| Member | Kind | Persist role |
|---|---|---|
| `Id` | `Guid` | PK only (`TraderDbContext` L152: `HasKey(x => x.Id)`; no unique `(venue, instrument)`) |
| `CanonicalSymbol` | `string` default `"XAUUSD"` | canonical code, **not** a venue id (B15) |
| `VenueInstrumentId` | `string?` | only column that could hold tag 55; seeder writes `null` (D96) |
| `Bid` / `Ask` | `decimal` | dest book |
| `ReceivedAt` | `DateTimeOffset` | collector clock |
| `VenueTimestamp` | `DateTimeOffset?` | venue clock; unused by `ShadowCopyEngine` (B18) |

Declared types in this file: **one** (`DestinationQuoteSnapshot`). No second type. No `partial`. No `DestinationQuote` alias.

---

## 3. Identity vs `HEAD` and vs B18

| Check | Result |
|---|---|
| SHA-256 (this pass) | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` |
| SHA-256 quoted by B18 | **same** |
| `git hash-object` working tree | `ba2cebfdc9d323778f74fbff3e7574d79b0a2639` |
| `git rev-parse HEAD:src/Domain/Entities/DestinationQuote.cs` | `ba2cebfdc9d323778f74fbff3e7574d79b0a2639` (**equal**) |
| `git diff -- src/Domain/Entities/DestinationQuote.cs` | **empty** |
| `git log --all --` this path | `6c41447 2026-08-18 13:12:17 +0530 Initial commit` only |
| `git blame -l` | all 12 lines `^6c414477f632416031b851171d3354fe2a23259` (AutoCommit) |
| `git show HEAD:…` | identical 12-line snapshot class |
| Encoding | UTF-8, no BOM, LF (`HAS_CR=False`) |

**Restore proof that is measured:** whatever happened at LastWriteTime 13:39:32, the file **now** contains the original snapshot type and the **same blob** as the initial commit.

**Overwrite proof that is *not* measured:** git has no parent with `class DestinationQuote` in this file. The overwritten text is not in the object store, not in a stash (`git stash list` empty), and not in a leftover `*DestinationQuote*` path under `D:\Prop` outside this file. Confirmation is of the **restored end state**, not a recovered diff of the bad write.

---

## 4. Why an overwrite was the feared move (B01)

B01 already named this file as the only Domain stem/class split:

| File | Declared type | Why it is a trap |
|---|---|---|
| `Entities\DestinationQuote.cs` | `DestinationQuoteSnapshot` | Filename advertises `TraderIntelligence.Domain.Risk.DestinationQuote` |

B01: *“This is **not** a compiler error. It **will** become CS0104 the moment anyone adds `class DestinationQuote` under `Entities` while a consumer imports both `Entities` and `Risk`.”*

That is the exact overwrite this ticket is about: renaming the persist class to match the filename (or pasting the Risk record into the entity file). Either move would:

1. Break `DbSet<DestinationQuoteSnapshot>` / `new DestinationQuoteSnapshot` (**CS0246**) unless every consumer was edited in the same pass.
2. Or, if consumers were also renamed, create `Entities.DestinationQuote` + `Risk.DestinationQuote` and explode the first file that `using`s both namespaces (**CS0104**).

Neither failure is present after this pass. See §6.

---

## 5. The two types that must not collapse

### 5.1 Persist — `TraderIntelligence.Domain.Entities.DestinationQuoteSnapshot`

EF entity. Mutable class. Has `Id`. Mapped to `destination_quotes`. Filename `DestinationQuote.cs`.

### 5.2 Risk DTO — `TraderIntelligence.Domain.Risk.DestinationQuote`

```24:30:D:\Prop\src\Domain\Risk\RiskEngine.cs
public sealed record DestinationQuote(
    string CanonicalSymbol,
    string? VenueInstrumentId,
    decimal Bid,
    decimal Ask,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? VenueTimestamp);
```

Positional record. **No `Id`.** Used by `RiskEngine.Evaluate`, `ShadowCopyEngine.SimulateEntry/Exit/MarkToMarket`, and `RiskEngineTests`.

| Field | Snapshot entity | Risk record |
|---|---|---|
| `Id` | **yes** | **no** |
| `CanonicalSymbol` | yes | yes |
| `VenueInstrumentId` | yes (`string?`) | yes (`string?`) |
| `Bid` / `Ask` | yes | yes |
| `ReceivedAt` | yes | yes |
| `VenueTimestamp` | yes | yes |

They are **field-compatible minus `Id`**. They are **not** the same CLR type. `EfTradingStore` copies row → record by constructor (L281–287). That adapter is the evidence the split is intentional, not an accident to “fix” by overwrite.

`TraderDbContext` does **not** reference `Risk.DestinationQuote`. `RiskEngine` does **not** reference `DestinationQuoteSnapshot`.

---

## 6. Consumers (still compiled against Snapshot)

Grep of `class DestinationQuote` / `record DestinationQuote` / `DestinationQuoteSnapshot` under product `.cs`:

| Site | What it binds |
|---|---|
| `Entities\DestinationQuote.cs` L3 | `class DestinationQuoteSnapshot` |
| `Risk\RiskEngine.cs` L24 | `record DestinationQuote` |
| `TraderDbContext.cs` L28 | `DbSet<DestinationQuoteSnapshot> DestinationQuotes` |
| `TraderDbContext.cs` L149–153 | `Entity<DestinationQuoteSnapshot>` → table `destination_quotes`, PK `Id` |
| `DemoSeeder.cs` L105 | `new DestinationQuoteSnapshot { … VenueInstrumentId = null, Bid = 2399.45m, Ask = 2399.85m }` |
| `EfTradingStore.cs` L273 + L281 | read `DestinationQuotes`; `new Domain.Risk.DestinationQuote(...)` **FQCN** |
| `EfDashboardQueries.cs` L165 | latest `DestinationQuotes` by `ReceivedAt` |
| `ShadowCopyEngine.cs` | Risk DTO only (`using Domain.Risk`) |
| `RiskEngineTests.cs` L70 | `new DestinationQuote(...)` under `using Domain.Risk` |

No authored file under `src/`, `apps/`, `tests/` has **both**

```csharp
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Risk;
```

So CS0104 is still **latent**, not live. The FQCN in `EfTradingStore` is the correct pattern.

`Persistence\Configurations\` is still **empty**. There is no `DestinationQuotesConfiguration`. A72’s “missing config class” is unchanged and is **not** a restore failure.

### 6.1 Build (measured this pass)

| Project | Configuration | Errors | Warnings | Exit |
|---|---|---:|---:|---:|
| `src\Domain\TraderIntelligence.Domain.csproj` | Release | **0** | **0** | 0 |
| `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | Release | **0** | **0** | 0 |

If Snapshot had been left overwritten to a type name `DestinationQuote` without updating DbContext/Seeder, Infrastructure would have been **CS0246** on `DestinationQuoteSnapshot`. It is not.

Domain also compiled with the extra untracked `RiskDecision.cs` on disk (different namespace from `Risk.RiskDecision`). That is **not** a Snapshot failure; see §8.

---

## 7. Timeline around the suspected overwrite

| Time (+05:30) | Event |
|---|---|
| 13:08:41 | First batch of entity files (`Broker`, `Mt5*`, scores, …) |
| 13:09:03 | Second batch: `DestinationQuote.cs` **created**; also `RiskDecisionRecord`, `KillSwitch`, `ShadowOrder`, `AuditLog`, `SyncCheckpoint` |
| 13:12:17 | `6c41447` Initial commit — blob is already `DestinationQuoteSnapshot` |
| **13:37:39** | **Untracked** `RiskDecision.cs` appears (`class RiskDecision` in `Entities`) |
| 13:37:43 | `ExecutionIntent.cs` rewritten (working-tree dirty vs HEAD) |
| **13:39:32** | `DestinationQuote.cs` LastWriteTime (content = HEAD snapshot) |
| 13:40:10 | `CopyIntent.cs` (dirty), `OutboxEvent.cs`, `FixSessionState.cs` |

The 13:37–13:40 window is an entity-mutating agent pass. DestinationQuote’s timestamp moved **27 minutes** after commit while its blob did **not**. Two readings are both honest:

1. **Restore reading (assigned claim):** an agent wrote `class DestinationQuote` over this file and a later write put Snapshot back. Last write = restore. Git never saw the bad bytes.
2. **Touch reading:** an agent opened/saved the file without changing bytes (or a tool re-emitted the same 421 bytes).

This ticket cannot distinguish (1) from (2) without the overwritten text. It **can** distinguish the end state: Snapshot is what is on disk, and it matches `HEAD`.

`CopyIntent` / `ExecutionIntent` dirtiness and the new `RiskDecision.cs` are **independent** mutations in the same window. They are listed only as circumstantial evidence that agents were colliding entity names then. This report does not review those diffs.

---

## 8. Adjacent live collision (not restored here)

`D:\Prop\src\Domain\Entities\RiskDecision.cs` is **untracked** (`??`) and declares:

```5:13:D:\Prop\src\Domain\Entities\RiskDecision.cs
public sealed class RiskDecision
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public RiskDecisionOutcome Outcome { get; set; }
    public decimal? AdjustedVolumeLots { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}
```

That is the **same simple name** as `TraderIntelligence.Domain.Risk.RiskDecision` (the engine result record). The persist type DbContext actually uses is still `RiskDecisionRecord`. Shape also **differs** (`AdjustedVolumeLots` / nullable `Reason` vs `ApprovedQuantity` / `AllowFixSend`).

This is B01’s Risk pair (`RiskDecision` vs `RiskDecisionRecord`) made worse: Entities now has **both** `RiskDecision` and `RiskDecisionRecord`. Domain still compiles because the engine record lives in another namespace. The first consumer that `using`s both namespaces and writes `RiskDecision` will be **CS0104**.

**Out of scope for E013.** Do not delete or edit that file in this ticket. Do not treat Snapshot restore as a cleanup of all entity homonyms.

---

## 9. What restore does **not** mean

| Claim | Status |
|---|---|
| Filename should stay `DestinationQuote.cs` | Unchanged trap. Safer rename would be `DestinationQuoteSnapshot.cs` (B21 §6 explicitly deferred) |
| `destination_quotes` has `(venue_id, instrument_id)` uniqueness | **No.** PK `Id` only (D19 / C06). No `VenueId` |
| Quote feed writes this table from FIX | **No.** Seeder inserts one demo row. `CTraderQuoteService` keeps in-memory bid/ask only |
| Shadow fill is priced from the persisted row | **Partial.** Store maps row → Risk DTO when state is `SHADOW`; engine still does not validate stale/spread (B18) |
| Two quote types unified | **No.** Adapter remains FQCN constructor |
| §69 item (dest quote cache / shadow) is PASS | **No.** Type presence ≠ pipeline |
| Product source was edited to restore Snapshot | **Not by this agent.** Bytes already matched `HEAD` when measured |

---

## 10. Binding answers (repeat)

| Question | Answer |
|---|---|
| Is `DestinationQuoteSnapshot` restored? | **YES** — declared, hashed, blamed, compiled. |
| Is `Entities.DestinationQuote` on disk? | **NO.** |
| Does the file match `HEAD` / B18? | **YES** — blob `ba2cebfd…`, SHA-256 `E5CFED15…`. |
| Did this agent modify product source? | **NO.** |
| Is the filename/type split gone? | **NO.** |
| Is there a leftover overwritten copy? | **NO** (not in git, not on disk). |
| Sibling entity collision in the same window? | **YES** — untracked `Entities.RiskDecision` (separate ticket). |

**Classification:** persist quote type `EXISTS_NEEDS_REFACTOR` (same as B18/D19: present, underspecified, filename trap). Overwrite residual: **ABSENT**.
