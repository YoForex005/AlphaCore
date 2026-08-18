# D47 — Is `CopyIntent` created after score `SHADOW`?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D47_copyintent.md` |
| Agent | D47 (CopyIntent vs score SHADOW order) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:38:00+05:30 (read-only; product source **not** edited) |
| Assigned question | **Is CopyIntent created after score SHADOW?** |
| Product source edited | **No** |
| SUT | `ReconstructionScoringService.RebuildTraderAsync` → `EfTradingStore.UpsertScoreAsync` → `EfTradingStore.PersistDemoShadowAsync` |
| Law | Architecture §§15, 23–24, 32, 36, 63–64, 75; A22 I5; A24 §§3–5; A41 §5; C59 (stale on writers) |
| Method | Full read of ingest/score/store/entity. Repo `grep` for `new CopyIntent` / `CopyIntents.Add` / `PersistDemoShadow`. `dotnet build` of Infrastructure. File hashes. Prefer false negatives over fake PASS. |

**Assigned answer:** **Yes — by control flow.** The only product writer of `CopyIntent` runs **after** the score is persisted, and it constructs rows **only when the just-computed state is `TraderState.SHADOW`.**

**One-line:** `RebuildTraderAsync` does `Score → UpsertScoreAsync (CurrentState = SuggestedState) → PersistDemoShadowAsync(SuggestedState)`; that method `return`s without `new CopyIntent` unless `state == SHADOW` (and a dest-quote row exists). This is a **demo backfill of OPEN intents on completed XAU trades**, not the A24 per-event factory. The tree is **mid-refactor and does not compile** as measured, so the path is designed but **not a runnable binary**.

---

## 0. Verdict

| Check | Result |
|---|---|
| Is there a product `CopyIntent` writer? | **Yes.** `EfTradingStore.PersistDemoShadowAsync` (`new CopyIntent` + `CopyIntents.Add`). **Only** writer. |
| Does that writer run **after** scoring? | **Yes.** Caller is `ReconstructionScoringService.RebuildTraderAsync` line 104, **after** `UpsertScoreAsync` (lines 88–102) has already `SaveChanges`. |
| Is creation gated on score `SHADOW`? | **Yes.** `if (state != TraderState.SHADOW) { SaveChanges; return; }` at store lines 267–271. |
| Which `state` is used? | `score.SuggestedState` from `BaselineScorer.Score` — **not** a re-read of `trader_scores`. Same token just written as `CurrentState`. |
| Non-SHADOW (`WATCH` / `EARLY_SCORE` / `RISK_BLOCKED` / `INSUFFICIENT_DATA` / `LIVE*`)? | **No** `CopyIntent`. Outbox `ScoreUpdate` only. |
| Also required besides SHADOW? | A `destination_quotes` row (`OrderByDescending(ReceivedAt).FirstOrDefault`). Missing quote → outbox only, **0** intents. |
| A24 incremental OPEN/INCREASE/REDUCE/CLOSE from source events? | **No.** Historical **completed** trades → `OpenExposure` only. |
| `RiskEngine` consulted? | **No.** |
| Outbox `ShadowCopyIntent`? | **No.** Writer stamps `OutboxEventType.ScoreUpdate`. |
| Tests lock this order? | **No.** Seed test does not assert `CopyIntents`. Zero `PersistDemoShadow` / `SHADOW_ONLY` facts. |
| Compiles / runs as measured? | **No.** `dotnet build` Infrastructure **FAIL** (see §7). Entity rewrite (13:37) dropped the fields the writer (13:35) assigns. |
| C59 “zero writers” still true? | **Stale.** Writer landed in `EfTradingStore` after C59. |
| Product source changed by D47 | **No.** |

**Do not claim** “shadow copy is wired to A24.” **Do not claim** “intents exist in a running demo.” **Do not claim** C59 is current. **Do not claim** LIVE traders also mint intents (gate is **exactly** `SHADOW`, which is stricter than A24’s `{SHADOW, LIVE_CANDIDATE, LIVE}` — and `FromBaseline` cannot emit LIVE anyway; D12).

---

## 1. Binding order (what “after score SHADOW” means)

Architecture §75 / §32 / A24 §3 (legal production path):

```text
reconstruct → score / trader_state
      ↓
shadow-eligible?   (A24: SHADOW | LIVE_CANDIDATE | LIVE)
      ↓
classify action_class → Create CopyIntent → Persist
      ↓
RiskEngine / shadow policy
```

A22 I5 / §23: trade #3 + high score → **SHADOW**, never LIVE. Copy must not run **before** that state exists, and must not run for `INSUFFICIENT_DATA` / `WATCH` / `RISK_BLOCKED` (C59 §5: “emit must run **after** state”).

So the assigned question is two conjuncts:

1. **Temporal:** persist score first, then persist intent.
2. **Predicate:** intent only if that score is `SHADOW`.

Both are true in the current caller/callee pair. Completeness vs A24 is a different question (FAIL).

---

## 2. Measured call chain

### 2.1 Application — score, persist score, then shadow

```79:105:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
    }
```

Order, same method, no branch between them:

| Step | What is committed | Clock |
|---|---|---|
| 1 | `reconstructed_trades` wipe/replace | own `SaveChanges` |
| 2 | `BaselineScorer.Score` in-memory | no IO |
| 3 | `trader_scores` upsert + **new** `trader_score_history` row | `UpsertScoreAsync` `SaveChanges` |
| 4 | outbox + optional `copy_intents` + `shadow_orders` | **second** `SaveChanges` in `PersistDemoShadowAsync` |

Score and intents are **not** one transaction. If step 4 throws, `CurrentState = SHADOW` is already durable and intents are absent.

`ITradingStore` now includes `PersistDemoShadowAsync` (line 17). Only this service calls it. `DealIngestionService.SyncBrokerAsync` still does **not** create intents.

Callers of `RebuildTraderAsync`:

| Caller | When |
|---|---|
| `DemoSeeder.SeedAsync` | first empty-broker boot; logins `10001, 10002, 10003, 99001` |
| `apps/mt5-worker/Worker.cs` | every **30 s**, same four logins |

### 2.2 Store — SHADOW gate then `new CopyIntent`

```251:337:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task PersistDemoShadowAsync(
        Guid brokerId,
        long login,
        TraderState state,
        IReadOnlyList<ReconstructedTradeResult> completedXau,
        CancellationToken ct)
    {
        _db.OutboxEvents.Add(new OutboxEvent { /* Type = ScoreUpdate, ... */ });

        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var quoteRow = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);
        if (quoteRow is null)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var engine = new ShadowCopyEngine();
        // map snapshot → Risk.DestinationQuote
        foreach (var trade in completedXau.Where(t => t.Completed).OrderBy(t => t.ClosedAt))
        {
            var key = $"shadow:{brokerId}:{login}:{trade.PositionId}";
            if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == key, ct))
                continue;

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
            // SimulateEntry(..., modeledDelay: 80ms) → ShadowOrder
        }
        await _db.SaveChangesAsync(ct);
    }
```

Gate table:

| Input | CopyIntent created? |
|---|---|
| `state == SHADOW` and ≥1 dest quote and ≥1 completed trade and new idempotency key | **Yes** (`OpenExposure`, `Status = "SHADOW_ONLY"`) |
| `state != SHADOW` | **No** (early return after ScoreUpdate outbox) |
| `SHADOW` but `DestinationQuotes` empty | **No** |
| `SHADOW` + quote, trade already keyed `shadow:{broker}:{login}:{positionId}` | **No** (skip) |
| Open (uncompleted) trade | **No** (`completedXau` already filtered; loop also `Where(Completed)`) |
| INCREASE / REDUCE / CLOSE of the same lifecycle | **Never minted** — one OPEN per `PositionId` |

`completedXau` is already `Completed && IsXauUsd` from the caller. The loop does not re-check `IsXauUsd`.

### 2.3 What “score SHADOW” is

`BaselineScorer` / `TraderStateMachine.FromBaseline` (D12, re-verified same SHA):

```text
N==0 or N<3 (and not blocked) → INSUFFICIENT_DATA
risk>=80 or (Martingale ∧ DD>0 ∧ NET<0) → RISK_BLOCKED
N>=3 ∧ quality>=70 ∧ risk<40 → SHADOW
N>=3 ∧ quality>=55 → WATCH
else → EARLY_SCORE
```

`LIVE` / `LIVE_CANDIDATE` are unreachable. `AfterHighEarlyScore() => SHADOW` is unused.

Demo Fake tape (if the path compiled and a dest quote is seeded):

| Login | Book | Stub state | Intents if path ran |
|---|---|---|---|
| 10001 | 3 modest XAU round-trips | **SHADOW** | **3** OPEN (`PositionId` 501, 502, 503) |
| 10002 | 2×/2× losers | **RISK_BLOCKED** | **0** |
| 10003 | no deals | **INSUFFICIENT_DATA** | **0** |
| 99001 | 3 small winners | **SHADOW** | **3** OPEN (701, 702, 703) |

Seeder **does** insert one invented dest quote (`Bid=2399.45`, `Ask=2399.85`, `VenueInstrumentId=null`) **before** `RebuildTraderAsync`, so the quote gate would pass on first boot. Worker cycles after that would no-op on the unique `IdempotencyKey` **if** that index existed and the insert succeeded.

`SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` asserts 10001 `NotBe(LIVE)` and 10002 `RISK_BLOCKED`. It does **not** assert `CopyIntents` count 6 / 0. The order is **unpinned**.

---

## 3. Grep — only one constructor

Product `*.cs` (`D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`), this pass:

| Pattern | Hits that construct / persist | Notes |
|---|---|---|
| `new CopyIntent` | **1** | `EfTradingStore.PersistDemoShadowAsync` L295 |
| `CopyIntents.Add` | **1** | same method L310 |
| `PersistDemoShadowAsync` | port + store + **one** caller | `RebuildTraderAsync` L104 |
| `ICopyIntentFactory` / `CreateCopyIntent` | **0** | A30 Increment 8 still missing |
| `Status = "SHADOW_ONLY"` | **1** | this writer |
| `OutboxEventType.ShadowCopyIntent` | enum member only | writer uses **`ScoreUpdate`** |
| `RiskEngine.Evaluate` from this path | **0** | tests still hand-build `"c1"` |
| Tests mentioning `PersistDemoShadow` / `SHADOW_ONLY` / `CopyIntents` | **0** | |

FIX worker does not read `CopyIntents`. Dashboard does not list them. Live `NewOrderSingle` remains off (SAFE_BY_ABSENCE).

---

## 4. What is created (when the writer’s object initializer is valid)

Writer-assigned fields (store L295–309) vs A24 §5 / A23 §3.1:

| Field the writer sets | Spec | Honest note |
|---|---|---|
| `Id` new Guid | `copy_intent_id` | ok |
| `BrokerId` / `SourceLogin` | source broker / login | ok |
| `CanonicalSymbol` from trade | XAUUSD expected | ok if recon mapped |
| `Action = OpenExposure` | one of four classes | **only** this class |
| `RequestedQuantity = MaxVolumeLots` | dest qty after §38 | **source lots echoed** (A24 §9 FAIL) |
| `ExpectedPrice = EntryVwap` | source price informational | ok-ish |
| `SourceEventTime = OpenedAt` | event time | uses **open**, not close, of a **completed** trade |
| `CreatedAt = UtcNow` | collector/decision | mixed |
| `ExpiresAt = OpenedAt + 15s` | mandatory expiry | **already expired** for June 2026 demo tape vs `UtcNow` 2026-08-18. `CopyIntentExpiry.IsExpired` is **not** called. |
| `Status = "SHADOW_ONLY"` | status machine | ad-hoc string, not A26 `SHADOWED` / `PENDING` |
| `IdempotencyKey = shadow:{broker}:{login}:{positionId}` | UK | one OPEN per position; **no** `source_event_id` / action in the key |

Not set by the writer (and required by A24): `source_trade_id`, `source_event_id`, `source_position_id`, `source_side`, `max_signal_age` column, `trader_state` on the row, `correlation_id`, dest instrument, linked shadow position, INCREASE/REDUCE/CLOSE.

Then a `ShadowOrder` is inserted in the **same** `SaveChanges`, priced by `ShadowCopyEngine.SimulateEntry` with **80 ms** delay (below the engine’s 250 ms overlay). D16 still applies: dest touch, no reject, no post-delay re-read. This **does** make D16 F12 “zero product callers” **stale**.

---

## 5. Type drift — writer and entity no longer match

Measured hashes / clocks (this agent, `Get-FileHash SHA256`):

| Path | SHA-256 | Bytes | LastWriteTime |
|---|---|---|---|
| `Application\Ingestion\DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 4535 | 2026-08-18 **13:35:29** |
| `Infrastructure\Persistence\EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 12097 | 2026-08-18 **13:35:59** |
| `Domain\Entities\OutboxEvent.cs` | `0F5CDDF38EA37DEA27D30E7E6A33C9516EA7149C8B14D8F9D442F0267F4471C3` | 485 | 2026-08-18 **13:37:34** |
| `Domain\Entities\CopyIntent.cs` | `9BBDB6C1C342D7EE8D6E44FD8ED5FCA85209E0971B4BA7406EE7060EB334EDFB` | 680 | 2026-08-18 **13:37:37** |
| `Domain\Entities\DestinationQuote.cs` | `47EDF6BD0D5AA785CB48CB27A2FAE3AB8645B0EA8BC0CF35A4C52423962A1FE8` | 349 | 2026-08-18 **13:37:40** |
| `Domain\Scoring\BaselineScorer.cs` | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | — | unchanged vs D12 |
| `Infrastructure\Persistence\TraderDbContext.cs` | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | — | still maps `IdempotencyKey` + `DestinationQuoteSnapshot` |

On-disk `CopyIntent` **now** (13:37 rewrite):

```5:19:D:\Prop\src\Domain\Entities\CopyIntent.cs
public sealed class CopyIntent
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long SourceLogin { get; set; }
    public long SourcePositionId { get; set; }
    public string CanonicalSymbol { get; set; } = string.Empty;
    public CopyIntentAction Action { get; set; }
    public TradeDirection Direction { get; set; }
    public decimal VolumeLots { get; set; }
    public decimal SourcePrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? RiskDecisionId { get; set; }
    public Guid? ExecutionIntentId { get; set; }
}
```

Writer still assigns **removed** members: `RequestedQuantity`, `ExpectedPrice`, `SourceEventTime`, `ExpiresAt`, `Status`, `IdempotencyKey`. It never assigns the **new** members: `SourcePositionId`, `Direction`, `VolumeLots`, `SourcePrice`.

`TraderDbContext` still: `e.HasIndex(x => x.IdempotencyKey).IsUnique();` — property gone.

`OutboxEvent` now has `EventType` / `Payload` / `CreatedAt`. Writer sets `Type` / `AggregateId` / `PayloadJson` / `OccurredAt`.

`DestinationQuote.cs` is now `record DestinationQuote(..., long CTraderInstrumentId, ..., QuoteReceivedAt, ...)`. `TraderDbContext` / `DemoSeeder` still name `DestinationQuoteSnapshot` with `VenueInstrumentId` / `ReceivedAt`. Store reads `quoteRow.ReceivedAt` / `VenueInstrumentId`.

**Control-flow answer does not depend on those names compiling.** The SHADOW gate is still in source. A running process cannot currently execute it.

---

## 6. Compile measurement (Infrastructure)

```text
dotnet build D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj -c Release
exit 1
```

Errors reported this pass (0 warnings):

| File | Error |
|---|---|
| `Persistence\Configurations\ReconstructedTradesConfiguration.cs` | CS0246 `ReconstructedTrades` not found (twice) |
| `TraderDbContext.cs:28` | CS0246 `DestinationQuoteSnapshot` not found |

The compiler stopped at missing types. The `CopyIntent` / `OutboxEvent` initializer mismatches in `PersistDemoShadowAsync` are **also** CS0117-class defects; they will surface once the snapshot type exists again.

Classification of the **path**: `EXISTS_NEEDS_REFACTOR` as an order/gate sketch. **`UNSAFE` / non-runnable** as a shipping feature. Classification of A24 emit: still **MISSING** (no events, no four action classes, no factory).

---

## 7. Adjacent honesty (not the assigned question)

These must not be laundered into “copy after SHADOW is done.”

| Topic | Measured |
|---|---|
| Per-deal lifecycle events | Still absent (`TradeReconstructor` returns snapshots). C59 §2 still holds on **events**. |
| Blind catch-up | Writer **intentionally** backfills every completed trade on first SHADOW rebuild. A41/A24 forbid historical OPEN flood. Demo-only name (`PersistDemoShadow`) admits this. `ExpiresAt = OpenedAt+15s` is already stale. |
| Two transactions | Score can be SHADOW with 0 intents if shadow persist fails or quote missing. |
| Idempotency | App-level `AnyAsync(IdempotencyKey)` + unique index **on a property the entity no longer has**. TOCTOU same as D20 deals. |
| Qty | `MaxVolumeLots` → dest qty. `QuantityNormalizer` unused. |
| Risk | Not called. Kill switch / spread / stale quote do not block this mint. |
| LIVE gate | `state != SHADOW` would also block `LIVE_CANDIDATE` / `LIVE` if those ever appeared. A24 wants those states eligible. Vacuous today (D12). |
| D16 “no callers” | **Stale.** This store method constructs `ShadowCopyEngine`. Engine defects remain. |
| C59 “zero `new CopyIntent`” | **Stale** as of `EfTradingStore` 13:35:59. |
| D20 store line count 250 | **Stale.** File is now 338 lines / 12097 bytes. |

---

## 8. Prior swarm vs this file

| Prior | Claim | This measure |
|---|---|---|
| C59 | Reconstruction does not emit `CopyIntent`; 0 writers | **Writer exists** after score. Events still missing. |
| A24 §3 / A30 Inc 8 | Factory + candidate evaluator + persist-before-risk | **Still missing.** Demo hook is not that increment. |
| A22 I5 / D12 | High score → SHADOW, not LIVE | **Still true.** This hook **consumes** that token. |
| A41 | Same-TX `shadow-copy-intent` outbox | **ScoreUpdate** only; separate TX from score. |
| D16 F12 | Shadow engine unused | **Used** here (80 ms entry). |

---

## 9. Direct answer

**Is `CopyIntent` created after score `SHADOW`?**

**Yes.**

1. **After score:** `UpsertScoreAsync` commits `trader_scores.CurrentState = SuggestedState`, then `PersistDemoShadowAsync` runs.
2. **After / only if SHADOW:** that method creates `CopyIntent` rows if and only if the passed state is `TraderState.SHADOW` (plus dest-quote + new idempotency key).
3. **Not** after `WATCH` / `EARLY_SCORE` / `RISK_BLOCKED` / `INSUFFICIENT_DATA`.
4. **Not** the A24 production emit (no lifecycle events, OPEN-only backfill, no risk, expired `ExpiresAt`, source lots).
5. **Not proven in a running binary** at this timestamp: entity/DbContext/seeder types were rewritten ~90 s after the writer; Infrastructure build is RED.

Until a green build + a seed/integration fact (`10001` → N OPEN `SHADOW_ONLY`; `10002` → 0), treat the order as **source-readable design**, not a measured runtime property.

---

## 10. Files cited

- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`ReconstructionScoringService`, `ITradingStore`)
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`UpsertScoreAsync`, `PersistDemoShadowAsync`)
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\OutboxEvent.cs`
- `D:\Prop\src\Domain\Entities\ShadowOrder.cs`
- `D:\Prop\src\Domain\Entities\TraderScore.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Domain\Enums\CopyIntentAction.cs`
- `D:\Prop\src\Domain\Enums\OutboxEventType.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (`DemoBrokerFactory`)
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\C59_copyintent_gap.md` (stale on writers)
- `D:\Prop\reports\swarm\20260818\D12_scorer_review.md`
- `D:\Prop\reports\swarm\20260818\D16_shadow.md`
- `D:\Prop\reports\swarm\20260818\A24_shadow_copy_spec.md` §§3–5

---

*End of D47. Product source was not modified. Answer: yes — CopyIntent is created after, and only after, a SHADOW score, in `PersistDemoShadowAsync`. Not A24-complete. Not compiling as measured.*
