# D71 — Is `CopyIntentExpiry` used?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D71_expire.md` |
| Agent | D71 (CopyIntentExpiry usage census, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:41:51+05:30 |
| Assigned | **CopyIntentExpiry used?** Write this file. Do not modify product source. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| SUT | `TraderIntelligence.Domain.Execution.CopyIntentExpiry` |
| Law | Architecture v2 **§36** (copy timing / stale reject) and **§63** (`expires_at` + `max_signal_age`; no blind catch-up). Specs: A23, A24 §6, A53, A71, A73 §4.1. |
| Method | Full read of the helper, `CopyIntent`, `RiskEngine`, `EfTradingStore.PersistDemoShadowAsync`, `ReconstructionScoringService`, both workers, API `/api/settings`, dashboard, shadow engine. `Select-String` of `src/`, `apps/`, `tests/` `*.cs` (exclude `bin`/`obj`) for `CopyIntentExpiry`, `.IsExpired(`, `using TraderIntelligence.Domain.Execution`, `ExpiresAt`. SHA-256 via `Get-FileHash`. Prefer false negatives over fake PASS. |

**Assigned answer:** **No — not on any product path.** The type is compiled into Domain and is called only from one unit fact (`ExecutionAndSizingTests.Copy_intent_expires`, two assertions). `RiskEngine` inlines an equivalent `>` age check. The only `CopyIntent` writer stamps `ExpiresAt = OpenedAt + 15s` by arithmetic and never invokes the helper. Nothing later reads `ExpiresAt` to expire a row.

**One-line:** `CopyIntentExpiry` is a two-line public predicate, unit-smoked, **dead at runtime**.

---

## 0. Verdict (honest)

| Slice | Measured | Class |
|---|---|---|
| Type on disk | `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` — static class, one method | `EXISTS_NEEDS_REFACTOR` |
| Product callers (`src/`, `apps/`) | **0** | unused |
| `using TraderIntelligence.Domain.Execution` in product | **0** (tests only) | unused |
| Test callers | **1** fact, **2** rows | smoke only |
| Dedicated `CopyIntentExpiryTests` / `StaleCopyIntentExpiryTests` | **absent** | `MISSING` vs A27 / A89 #55 |
| `RiskEngine.Evaluate` | inlines `DecisionTime - SourceEventTime > MaxSourceSignalAge` | **does not call** helper |
| `PersistDemoShadowAsync` | writes `CopyIntent.ExpiresAt` without the helper | **does not call** helper |
| `ExpiresAt` readers in product | **0** (property + one assignment) | column is write-only |
| `expires_at` **or** `max_signal_age` (A73 / §63) | helper implements **age only** (`now - source > max`) | incomplete vs law |
| OPEN vs CLOSE family ages (A71 / §64) | one `TimeSpan` for all actions | incomplete |
| FIX worker re-check before NOS | worker never reads intents | unused |
| Dashboard / API / web | no reference | unused |

**Do not claim** “stale-signal rejection is gated by `CopyIntentExpiry`.”  
**Do not claim** the helper is unused in the repo — tests call it.  
**Do not claim** C59 “nothing sets `ExpiresAt`” — that is **stale**; the store now assigns it.  
**Do not claim** “expiry is wired because `ExpiresAt` is populated.” A timestamp that is never compared is decoration.

---

## 1. Files hashed (this pass)

| Bytes | Lines | SHA-256 | Path |
|---:|---:|---|---|
| 246 | 7 | `76B82E4F0C6F6B43988D5E50EE5E5D229CC451C7E8267AD6DF56271790531D38` | `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` |
| 951 | 24 | `C9AE3FF95058B72FC00A4DDBCBF2DFD68B7637D00D321244C376E2A1D6D9148B` | `D:\Prop\src\Domain\Entities\CopyIntent.cs` |
| 8567 | 189 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| 12097 | 338 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| 4535 | 106 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| 3249 | 91 | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` |
| 5951 | 174 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 2144 | 62 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` |
| 2909 | 87 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | `D:\Prop\tests\Unit\RiskEngineTests.cs` |
| 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `D:\Prop\apps\api\Program.cs` |

Helper SHA matches D36 (unchanged since 2026-08-18T07:38:10Z). `CopyIntentExpiry.cs` LastWriteUtc = `2026-08-18T07:38:10.3123032Z`.

Product files named `*Expir*`: **only** this helper. No `CopyIntentExpiryPolicy`, no `ICopyIntentExpiry`, no Application port.

---

## 2. What the type actually is

```1:6:D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs
namespace TraderIntelligence.Domain.Execution;

public static class CopyIntentExpiry
{
    public static bool IsExpired(DateTimeOffset sourceEventTime, DateTimeOffset now, TimeSpan maxSignalAge) =>
        now - sourceEventTime > maxSignalAge;
}
```

| Aspect | Measured |
|---|---|
| Kind | `public static class` — no instance, no DI |
| Members | one expression-bodied `bool IsExpired(...)` |
| Inputs | `sourceEventTime`, `now`, `maxSignalAge` |
| **Not** inputs | `expiresAt`, `CopyIntent`, `CopyIntentAction`, `IUtcClock` |
| Predicate | `now - sourceEventTime > maxSignalAge` (strict `>`; age **==** max is **not** expired) |
| Side effects | none |
| Usings | none (BCL only) |

It does **not** look at `CopyIntent.ExpiresAt`. An operator (or the store) can set a short `expires_at` and this helper will ignore it. That is the A73 §4.1 / §63 hole, still open.

A73 required shape (not implemented):

```text
IsExpired(sourceEventTime, expiresAt, now, maxSignalAge) =>
    now >= expiresAt || now - sourceEventTime > maxSignalAge
```

---

## 3. Grep evidence — every C# hit

`Select-String` over `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` `*.cs`, excluding `bin`/`obj`, patterns `CopyIntentExpiry` and `.IsExpired(`:

| Path | Line | Role |
|---|---:|---|
| `src\Domain\Execution\CopyIntentExpiry.cs` | 3 | definition (`public static class`) |
| `src\Domain\Execution\CopyIntentExpiry.cs` | 5 | definition (`IsExpired`) |
| `tests\Unit\ExecutionAndSizingTests.cs` | 59 | test: 16 s vs 15 s → `true` |
| `tests\Unit\ExecutionAndSizingTests.cs` | 60 | test: 5 s vs 15 s → `false` |

**Zero** hits in:

- `Application/` (including `DealIngestionService` / `ReconstructionScoringService`)
- `Infrastructure/` (including `EfTradingStore`, `TraderDbContext`, `EfDashboardQueries`, `DemoSeeder`, DI)
- `Fix.CTrader/`
- `Mt5/`
- `apps/api`, `apps/mt5-worker`, `apps/fix-worker`
- `apps/web` (`*.ts` / `*.tsx`: only the word “CopyIntent” in static Shadow page copy)
- `tests/Integration/`
- `docs/`

`using TraderIntelligence.Domain.Execution` appears only in three **test** files:

| File | Uses `CopyIntentExpiry`? |
|---|---|
| `tests\Unit\ExecutionAndSizingTests.cs` | **yes** (`IsExpired`) |
| `tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | no (qty only) |
| `tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | no (qty only) |

No product assembly imports the `Execution` namespace. The helper cannot be reached without a new `using` or a fully-qualified name that does not exist.

---

## 4. Parallel clocks that are **not** this type

Three nearby 15-second numbers exist. None of them call `CopyIntentExpiry`.

### 4.1 `RiskEngine` — inlined `SIGNAL_STALE`

```16:16:D:\Prop\src\Domain\Risk\RiskEngine.cs
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
```

```113:115:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var signalAge = request.DecisionTime - request.SourceEventTime;
        if (signalAge > _limits.MaxSourceSignalAge && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "SIGNAL_STALE");
```

Same `>` semantics as the helper. Differences that prove it is **not** a call-through:

| | `CopyIntentExpiry.IsExpired` | `RiskEngine` |
|---|---|---|
| Clock | caller-supplied `now` | `request.DecisionTime` |
| Cap | caller-supplied `TimeSpan` | `RiskLimits.MaxSourceSignalAge` (code default 15 s) |
| `expires_at` | ignored (no param) | ignored (`RiskEvaluationRequest` has no `ExpiresAt`) |
| Action family | none | only `OpenExposure` / `IncreaseExposure` (`IsIncreasing`) |
| Reason code | `bool` only | `"SIGNAL_STALE"` |
| REDUCE/CLOSE | would expire if caller asked | **never** stale-rejected |

`RiskEngineTests.Stale_signal_rejected` feeds `SourceEventTime = DecisionTime - 5min` and asserts `Reason == SIGNAL_STALE`. That fact does **not** mention `CopyIntentExpiry`. C03 / D35 already recorded this split; it is still true at this hash.

`Evaluate` is itself only called from unit tests. There is still no product `RiskEngine.Evaluate` site. So even the **inline** stale check is not on a send/shadow path.

### 4.2 `PersistDemoShadowAsync` — writes `ExpiresAt`, never evaluates it

```295:310:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
            var intent = new CopyIntent
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                SourceLogin = login,
                CanonicalSymbol = trade.CanonicalSymbol,
                Action = CopyIntentAction.OpenExposure,
                RequestedQuantity = trade.MaxVolumeLots,
                ExpectedPrice = trade.EntryVwap,
                SourceEventTime = trade.OpenedAt,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = trade.OpenedAt.AddSeconds(15),
                Status = "SHADOW_ONLY",
                IdempotencyKey = key
            };
            _db.CopyIntents.Add(intent);
```

| Fact | Measured |
|---|---|
| Uses `CopyIntentExpiry`? | **No.** `AddSeconds(15)` is a literal. |
| Uses `RiskLimits.MaxSourceSignalAge`? | **No.** |
| Uses API `maxSignalAgeSeconds`? | **No.** |
| Skips row if already expired (`now > OpenedAt+15s`)? | **No.** Historical completed trades from a 30-day ingest window still mint `OpenExposure`. Their `ExpiresAt` is in the **past** at insert time. |
| Later `if (CopyIntentExpiry.IsExpired(...))` / `ExpiresAt` filter? | **No.** Loop goes straight to `ShadowCopyEngine.SimulateEntry`. |

This is the §63 anti-pattern (backfill of stale opens) with an expiry column that would have rejected them **if anyone read it**.

`ExpiresAt` product hits (same `Select-String`, exclude `bin`/`obj`):

| Path | Line | Kind |
|---|---:|---|
| `Domain\Entities\CopyIntent.cs` | 19 | property |
| `Infrastructure\Persistence\EfTradingStore.cs` | 306 | **assignment only** |
| `Fix.CTrader\Services\FixSessionOwnership.cs` | 41, 54 | **unrelated** lock TTL (`expiresAt` camelCase on an in-memory tuple) |

EF maps `copy_intents` PK + unique `IdempotencyKey` only (`TraderDbContext` 122–127). No index on `ExpiresAt`. No query `Where(c => c.ExpiresAt > now)`.

### 4.3 API settings — display number, not a binder

```42:46:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    ...
```

Literal JSON. Not injected into `RiskLimits`, not passed to `CopyIntentExpiry`, not used by the store’s `AddSeconds(15)`.

`docs/risk.md` “Maximum delay: 2000ms” is a **third** number, also unbound.

---

## 5. Call graph (what actually runs)

```text
mt5-worker /api/ops/resync
      → ReconstructionScoringService.RebuildTraderAsync
            → TradeReconstructor
            → UpsertScoreAsync
            → PersistDemoShadowAsync          // may new CopyIntent
                  → ExpiresAt = OpenedAt+15s  // no helper
                  → ShadowCopyEngine.SimulateEntry
                  // no IsExpired, no RiskEngine

fix-worker
      → stamps FixSessionStates Disconnected
      → does not query CopyIntents
      → does not call CopyIntentExpiry
      → never sends 35=D

RiskEngine.Evaluate
      → unit tests only
      → inlined SIGNAL_STALE
      → no CopyIntentExpiry

CopyIntentExpiry.IsExpired
      → ExecutionAndSizingTests.Copy_intent_expires only
```

`EfDashboardQueries` never touches `CopyIntents`. Web Shadow page is static copy. No SignalR, no outbox drain that would re-check age.

---

## 6. Tests — smoke, not §63

```55:61:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    [Fact]
    public void Copy_intent_expires()
    {
        var t = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        CopyIntentExpiry.IsExpired(t, t.AddSeconds(16), TimeSpan.FromSeconds(15)).Should().BeTrue();
        CopyIntentExpiry.IsExpired(t, t.AddSeconds(5), TimeSpan.FromSeconds(15)).Should().BeFalse();
    }
```

| Locked | Not locked |
|---|---|
| 16 s > 15 s → true | age **==** 15 s (boundary; `>` vs `>=`) |
| 5 s > 15 s → false | `now >= expiresAt` independent of age |
| | OPEN vs CLOSE family spans |
| | `CopyIntent` row / `ExpiresAt` column |
| | 20 intents after a 3-minute TRADE gap → 0 NOS (A53 / A27) |
| | persist-then-expire on the demo writer |
| | `INTENT_EXPIRED` vs `SIGNAL_STALE` |

A01 names `tests/Unit/Domain/CopyIntentExpiryTests.cs`. A27 / A89 #55 name `Risk.StaleCopyIntentExpiryTests`. **Neither file exists.** D36 already classified `Copy_intent_expires` as a 2-row smoke standing in for #55. Still true.

---

## 7. Spec vs type (why “exists” ≠ “used”)

Architecture §63 (verbatim requirement on each `CopyIntent`):

```text
expires_at
max_signal_age
```

| Required | On disk | Evaluated by `CopyIntentExpiry`? |
|---|---|---|
| `expires_at` | `CopyIntent.ExpiresAt` (written by demo store) | **No** — no parameter |
| `max_signal_age` | **no column**; helper takes a `TimeSpan` arg | only if a caller supplied it — **no product caller** |
| Re-check at FIX send | A73 §4.2 | **No** send path |
| CLOSE longer than OPEN | §63 last sentence / A71 | **No** — one span |
| No 20-intent catch-up | §63 worked example | Demo writer **does** backfill completed history |

`docs/risk.md` 2000 ms max delay contradicts both the 15 s helper/engine default and the 15 s `ExpiresAt` stamp. None of the three numbers is a single bound policy object.

---

## 8. Classification vs earlier swarm notes

| Prior | Claim | Still true at this hash? |
|---|---|---|
| B01 | `IsExpired` never called; RiskEngine inlines | **Product: yes.** Tests now call it (B01 predated or ignored the fact). |
| B13 / C03 / D13 / D35 | Engine does not call helper | **Yes.** |
| A73 T05 | `EXISTS_NEEDS_REFACTOR` | **Yes** — signature still age-only. |
| A73 L39 | `tests/Unit` has no `.cs` files | **Stale.** Tests exist; helper smoke is in `ExecutionAndSizingTests`. |
| C33 | helper never called | **Stale if counting tests**; **true** for product. |
| C59 L281 | nothing sets `ExpiresAt` because nothing constructs the entity | **Stale.** D47 / this pass: `PersistDemoShadowAsync` constructs and sets `ExpiresAt`. Still does not call the helper. |
| D16 | helper unused by `ShadowCopyEngine` | **Yes** (engine still has no expiry argument). Store now *calls* `SimulateEntry` but still not `IsExpired`. |
| D36 | product callers of `IsExpired` = definition only | **Yes.** |
| D47 | `CopyIntent` **is** created after SHADOW | Orthogonal. Creation ≠ expiry use. |

---

## 9. Honesty box

| Claim someone might make | Measured |
|---|---|
| “`CopyIntentExpiry` is used” | **Only by** `ExecutionAndSizingTests.Copy_intent_expires`. **No** product caller. |
| “Risk uses the expiry helper” | **No.** `RiskEngine` duplicates the `>` check and never references the type. |
| “Shadow path expires stale intents” | **No.** Writer sets `ExpiresAt` then fills shadow regardless of age. Historical `OpenedAt+15s` is typically already past. |
| “§63 is implemented because the helper exists” | **No.** Missing `expires_at` clause, missing stored `max_signal_age`, missing send-time re-check, missing catch-up test. Demo writer is the catch-up. |
| “Dead code can be deleted safely” | Public Domain API; tests would fail. Deleting is a product change — **not done here**. |
| Product source modified for this report | **No.** |

---

## 10. What “used” would look like (acceptance, not a task for this agent)

`CopyIntentExpiry` is **used** only when all of the following are measured:

1. Product path (risk evaluate **and** FIX/shadow send) calls `IsExpired` (or a successor that takes **both** `expiresAt` and `maxSignalAge`).
2. `CopyIntent` persists `max_signal_age` (family-aware) **and** `expires_at`; helper reads both.
3. Demo / incremental emit **skips or marks expired** OPENs instead of simulating fills on past `ExpiresAt`.
4. REDUCE/CLOSE use a longer span; 3-minute TRADE gap + 20 stale OPENs → 0 `NewOrderSingle` (A53).
5. Named tests (`CopyIntentExpiryBothClauses` + `StaleCopyIntentExpiryTests`) lock `>` vs `>=` and both clauses.

Until then the honest label is: **defined, unit-smoked, unused by the product.**

---

## 11. Files cited

- `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs`
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Enums\CopyIntentAction.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\docs\risk.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §36, §63, §64
- `D:\Prop\reports\swarm\20260818\A73_copy_latency.md` §4.1
- `D:\Prop\reports\swarm\20260818\C59_copyintent_gap.md` (stale on `ExpiresAt` writer)
- `D:\Prop\reports\swarm\20260818\D36_exec_tests.md`
- `D:\Prop\reports\swarm\20260818\D47_copyintent.md` (writer exists; not this helper)

---

*End of D71. `CopyIntentExpiry` is not used by product code.*
