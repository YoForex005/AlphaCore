# C59 — Reconstruction does not emit `CopyIntent`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C59_copyintent_gap.md` |
| Agent | C59 (reconstruction → copy-intent emit gap) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Question | Does reconstruction emit `CopyIntent` (persist + outbox) from reconstructed source events? |
| SUT | `TradeReconstructor`, `ReconstructionScoringService`, `EfTradingStore.ReplaceReconstructedAsync`, `CopyIntent` entity |
| Law | Architecture v2 §§13–15, §23–24, §32, §35–36, §63–64, §75; A21 §4.3 / §7; A23 §3.1; A24 §§3–5; A41 §5; A20 `copy_intents` card |
| Method | Full read of reconstruction + ingest persist + CopyIntent/Risk/Shadow/Outbox types. `grep` of product `*.cs` for writers. No implementation. Prefer false negatives over fake PASS. |

---

## 0. Verdict

**Confirmed. Reconstruction does not emit `CopyIntent`.**

The live rebuild path is:

```text
deals → TradeReconstructor.Reconstruct → ReplaceReconstructedAsync
                                      → BaselineScorer.Score → UpsertScoreAsync
                                      → STOP
```

It never:

- emits A21 §4.3 lifecycle events (`XAU_LIFECYCLE_OPENED` / `INCREASED` / `REDUCED` / `COMPLETED`)
- classifies §64 `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` / `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE`
- constructs or persists `CopyIntent`
- writes `outbox_events` (`ShadowCopyIntent`, `TradeCompleted`, `RiskCheckRequest`)
- calls `RiskEngine` or `ShadowCopyEngine`

`grep` of product `*.cs` for `new CopyIntent`, `CopyIntents.Add`, `ICopyIntent*`, `CopyIntentFactory`, `CreateCopyIntent` returns **zero writers**. The only `CopyIntents` hit is the unused `DbSet` on `TraderDbContext`.

This is **not** a one-line persist miss. Reconstruction today is a **lifecycle snapshot function**. `CopyIntent` is a **per-source-event** command. The snapshot cannot recover the four action classes, the source event id, or a stable `source_trade_id`. Wiring `new CopyIntent` onto `RebuildTraderAsync` as it exists would either no-op or violate §63 (blind catch-up of the entire history on every 30 s rebuild).

| Layer | Required (A21 / §32 / A24) | On disk | Class |
|---|---|---|---|
| Lifecycle events from the book | A21 §4.3 `XAU_LIFECYCLE_*` | **Absent** (`ReconstructedTradeResult` only) | **MISSING** |
| Copy-candidate gate | §32 `Copy candidate?`; A30 `ICopyCandidateEvaluator` | **Absent** | **MISSING** |
| `CopyIntent` factory + persist | §32 persist-before-risk; A23 §3.1 fields | Entity + EF table map only | **EXISTS_NEEDS_REFACTOR** (type); **MISSING** (flow) |
| Outbox `shadow-copy-intent` | §13, A41 | Enum member only; **0** inserts | **MISSING** |
| Incremental vs full rebuild | Live events only; no historical OPEN flood | `ReplaceReconstructedAsync` wipes + reinserts every cycle | **MISSING** |
| Stable `source_trade_id` | A20 FK `copy_intents.source_trade_id → reconstructed_trades.id` | Persist assigns `Guid.NewGuid()` every rebuild | **UNSAFE** if intents were added on top |
| Unit / integration emit tests | A09 #15, A27 factory + idempotency | Expiry helper + hand-built `RiskEvaluationRequest` only | **MISSING** |

**One-line:** vocabulary exists; the reconstruction pipeline stops at trades + scores; the copy path is dark.

Do **not** claim “shadow/live copy is gated by reconstruction.” There is no intent to gate.

---

## 1. Binding contract (what “emit” means)

Architecture §75 / §32 / A24 §3:

```text
Trade reconstruction
      ↓
Shadow-eligible reconstructed event?
      ↓
Classify action_class  (OPEN | INCREASE | REDUCE | CLOSE)
      ↓
Create CopyIntent  (expires_at, max_signal_age, action_class)
      ↓
Persist CopyIntent
      ↓
outbox shadow-copy-intent  /  risk-check-request
```

Never from an MT5 callback. Never a FIX `NewOrderSingle` from this path.

A21 §4.3 (reconstruction’s own event vocabulary — “for outbox later; this spec only defines them”):

| A21 event | When | §64 / `CopyIntentAction` |
|---|---|---|
| `XAU_LIFECYCLE_OPENED` | first IN / INOUT leftover of a new seq | `OpenExposure` |
| `XAU_LIFECYCLE_INCREASED` | scale-in | `IncreaseExposure` |
| `XAU_LIFECYCLE_REDUCED` | OUT / OUT_BY, remaining > 0 | `ReduceExposure` |
| `XAU_LIFECYCLE_COMPLETED` | remaining hits 0 | `CloseExposure` |
| `EARLY_SCORE_ELIGIBLE` | 3rd clean XAUUSD completion | **not** a copy intent |

A21 line 278: *“These map later to §64 … Reconstruction itself does not copy.”*

So the legal split is:

1. **`TradeReconstructor`** emits (or yields) per-deal lifecycle events + dirty flags.
2. **Application** (`ICopyCandidateEvaluator` + `ICopyIntentFactory`, A30 Increment 8) maps eligible events → persist `copy_intents` + outbox.

Neither half exists. The reconstructor does not emit events. Application has no factory, no store port, no handler.

A21 non-scope (“destination copy”) does **not** excuse missing §4.3 events. Without those events there is nothing lawful to copy.

---

## 2. What reconstruction actually emits today

### 2.1 Pure function — trades only

```24:45:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public IReadOnlyList<ReconstructedTradeResult> Reconstruct(
        string brokerId,
        long login,
        IReadOnlyList<NormalizedDeal> deals)
    {
        ...
        var results = new List<ReconstructedTradeResult>();
        foreach (var group in trading.GroupBy(d => d.PositionId))
            results.AddRange(ReconstructPosition(brokerId, login, group.Key, group.ToList()));

        return results
            .OrderBy(t => t.OpenedAt)
            .ThenBy(t => t.PositionId)
            .ToList();
    }
```

Return type is `IReadOnlyList<ReconstructedTradeResult>`. There is no event list, no `CopyIntent`, no outbox DTO, no `action_class`.

`CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` are **count/filter** helpers on that list. `IsEarlyScoreEligible` is `count >= 3`. It does **not** emit `EARLY_SCORE_ELIGIBLE` as an event (A21 §4.3 / §15).

### 2.2 Application rebuild — trades + score, then stop

```78:102:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore { ... CurrentState = score.SuggestedState ... }, ct);
    }
```

`ITradingStore` has **no** `InsertCopyIntent` / `EnqueueOutbox` method:

```8:18:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
public interface ITradingStore
{
    Task UpsertGroupAsync(...);
    Task UpsertAccountAsync(...);
    Task<bool> UpsertDealAsync(...);
    Task ReplacePositionsAsync(...);
    Task<IReadOnlyList<NormalizedDeal>> LoadDealsAsync(...);
    Task ReplaceReconstructedAsync(...);
    Task UpsertScoreAsync(...);
    Task<Guid> ResolveBrokerIdAsync(...);
}
```

`DealIngestionService.SyncBrokerAsync` writes groups / accounts / deals / positions only. It does **not** reconstruct and does **not** create intents. The mt5-worker calls ingest, then `RebuildTraderAsync` for a **hard-coded** login set `{10001, 10002, 10003, 99001}` — still no intent.

DI registers reconstructor + scoring only (`DependencyInjection.cs` 36–41). No `ICopyIntentFactory`, no outbox processor, no `CopyIntentFromTradeJob` (A30).

### 2.3 Persist drops the only identity that could link an intent

```172:209:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task ReplaceReconstructedAsync(...)
    {
        var existing = _db.ReconstructedTrades.Where(t => t.BrokerId == brokerId && t.Login == login);
        _db.ReconstructedTrades.RemoveRange(existing);
        foreach (var t in trades)
        {
            _db.ReconstructedTrades.Add(new ReconstructedTrade
            {
                Id = Guid.NewGuid(),   // A21 key discarded
                ...
                // RemainingVolumeLots, DealTickets, string result.Id NOT stored
            });
        }
        await _db.SaveChangesAsync(ct);
    }
```

`ReconstructedTradeResult.Id` is `$"{BrokerId}:{Login}:{PositionId}:{OpenedAt.ToUnixTimeMilliseconds()}"`. The store throws it away and mints a new Guid every 30 s cycle.

`CopyIntent.SourceTradeId` is `Guid?`. A20 requires `copy_intents.source_trade_id → reconstructed_trades.id`. After every rebuild the FK target is a **new** row. Any later emit that stored last cycle’s Guid would dangle.

Also dropped (needed to classify or size an intent from the snapshot): `RemainingVolumeLots`, `DealTickets`, lifecycle seq, dirty bit.

### 2.4 Snapshot ≠ events (why you cannot infer intents after the fact)

One completed scaled-and-partially-closed lifecycle is **one** `ReconstructedTradeResult` with flags:

| Source deals (example, unit test `Scale_in_and_partial_close`) | Required intents | What the result contains |
|---|---|---|
| IN 0.10 @ 2300 | `OPEN_EXPOSURE` 0.10 | — |
| IN 0.10 @ 2290 | `INCREASE_EXPOSURE` 0.10 | `WasScaledIn=true`, `WasAveragedDown=true` |
| OUT 0.10 @ 2310 | `REDUCE_EXPOSURE` 0.10 | `WasPartialClose=true` |
| OUT 0.10 @ 2320 | `CLOSE_EXPOSURE` 0.10 | `Completed=true`, one row |

Flags are boolean ORs. There is no per-deal qty, no per-deal time, no `source_event_id`. A consumer of `reconstructed_trades` **cannot** rebuild the four intents.

Reverse (`ENTRY_INOUT`) is two trade results (completed prior + open leftover) and still **zero** intents. A24 §4.1 / A23 §2 require **two** intents in order: `CLOSE_EXPOSURE` then `OPEN_EXPOSURE`. The leftover `OpenTrade.Start` also re-applies INOUT money (B11 C1) — even the snapshot is wrong for the new side.

Open-only books (`Completed=false`) are returned by `Reconstruct` but `RemainingVolumeLots` is not persisted. Dashboard / later jobs cannot see that an OPEN should still be live.

---

## 3. Grep evidence — no writer anywhere

Product `*.cs` (workspace `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`), 2026-08-18:

| Pattern | Hits that write / construct | What actually exists |
|---|---|---|
| `new CopyIntent` | **0** | — |
| `CopyIntents.Add` | **0** | — |
| `ICopyIntentFactory` / `ICopyIntentStore` / `ICopyCandidateEvaluator` | **0** | Named only in A30, not in source |
| `CopyIntentFactory` / `CreateCopyIntent` | **0** | — |
| `OutboxEvents.Add` / `new OutboxEvent` | **0** | `OutboxEvent` type + `DbSet` only |
| `ShadowCopyIntent` | 1 | `OutboxEventType.ShadowCopyIntent = 2` enum member |
| `CopyIntents` | 1 | `TraderDbContext` `DbSet` + `ToTable("copy_intents")` |
| `RiskEngine.Evaluate` callers | tests only | Hand-built `RiskEvaluationRequest` |
| `ShadowCopyEngine` callers | **0** outside its file | SimulateEntry/Exit unused |

`DemoSeeder` seeds brokers, XAU instrument, FIX session rows, one destination quote, kill switch, then ingest + `RebuildTraderAsync`. It does **not** insert `CopyIntent`. Seeded SHADOW traders (C16: login 10001) still produce **zero** intents.

`apps/mt5-worker/Worker.cs` ingest + score only.  
`apps/fix-worker/Worker.cs` stamps `fix_sessions` heartbeats; never reads `CopyIntents` / `ExecutionIntents`.

---

## 4. Type inventory vs emit (honest: types ≠ flow)

### 4.1 `CopyIntent` entity — thinner than A23 / A24

```5:20:D:\Prop\src\Domain\Entities\CopyIntent.cs
public sealed class CopyIntent
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long SourceLogin { get; set; }
    public Guid? SourceTradeId { get; set; }
    public string CanonicalSymbol { get; set; } = "XAUUSD";
    public CopyIntentAction Action { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ExpectedPrice { get; set; }
    public DateTimeOffset SourceEventTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string IdempotencyKey { get; set; } = string.Empty;
}
```

EF: PK `Id`, unique `IdempotencyKey` only (`TraderDbContext` 122–127). No FK to `reconstructed_trades`. No `MaxSignalAge` column.

| A23 §3.1 / A24 §5 field | On entity? | Notes |
|---|---|---|
| `copy_intent_id` | `Id` | ok if written |
| `source_broker_id` | `BrokerId` | Guid, not the reconstructor’s string `brokerCode` |
| `source_login` | `SourceLogin` | ok |
| `source_trade_id` | `SourceTradeId` | Guid? — unstable across rebuild (see §2.3) |
| `source_event_id` | **No** | Cannot idempotent on deal ticket |
| `source_position_id` | **No** | |
| `correlation_id` | **No** | §57 |
| `canonical_symbol` | yes | default `"XAUUSD"`; no dest instrument id |
| `action_class` | `Action` | enum matches §64 names |
| `source_side` / `position_side` | **No** | only dest-ish qty/price |
| `source_event_volume` | collapsed into `RequestedQuantity` | A23: raw source ≠ dest qty (§38) |
| `source_price` | `ExpectedPrice` | naming implies dest; used as source in RiskEngine |
| `source_event_time` | yes | |
| `collector_receive_time` | **No** | |
| `decision_time` | **No** | |
| `expires_at` | yes | |
| `max_signal_age` | **No** | helper `CopyIntentExpiry.IsExpired(source, now, span)` exists; span is not stored |
| `trader_state` | **No** | score has `CurrentState`; never copied onto intent |
| `destination_account` | **No** | lives on `ExecutionIntent` only |
| `linked_destination_position_id` | **No** | required for REDUCE/CLOSE (A23) |
| `suggested_allocation` / `confidence` | **No** | |
| Natural UK `(broker, login, trade, event, action)` | **No** | opaque `IdempotencyKey` string (C06) |

`CopyIntentAction` itself is **EXISTS_AND_GOOD** as vocabulary (`OpenExposure=0` … `CloseExposure=3`).

`CopyIntentExpiry` is a two-line static (`now - sourceEventTime > maxSignalAge`). `RiskEngine` does **not** call it (C03). Nothing sets `ExpiresAt` because nothing constructs the entity.

### 4.2 Downstream types wait for an id that is never minted

| Consumer | Needs | Wired? |
|---|---|---|
| `RiskEvaluationRequest.CopyIntentId` | string id + `CopyIntentAction` | Unit tests invent `"c1"` |
| `RiskDecisionRecord.CopyIntentId` | Guid | `DbSet` only; no insert |
| `ExecutionIntent.CopyIntentId` | Guid | no factory, no persist-before-send |
| `ShadowOrder.CopyIntentId` | Guid | `ShadowCopyEngine` never writes `ShadowOrder` |
| `OutboxEventType.ShadowCopyIntent` | payload with intent id | no writer, no processor |
| Dashboard | rejected/pending intents (A26 / C13) | `EfDashboardQueries` never touches `CopyIntents` |

`RiskEngine.Evaluate` is a real function (stale quote / signal / spread / kill switch / reduce-vs-open). It is unreachable from reconstruction. `AllowFixSend` stays academic.

### 4.3 Identity mismatch if someone did emit from `ReconstructedTradeResult`

| Field | Reconstructor | `CopyIntent` |
|---|---|---|
| Broker | `string` code (`"ACHIEVER"`) | `Guid BrokerId` |
| Trade id | string `Broker:Login:Position:unixMs` | `Guid? SourceTradeId` |
| Event id | deal ticket on result (not persisted) | **missing column** |
| Qty | decimal lots | `RequestedQuantity` (units unspecified) |

There is no adapter. A30’s `CopyIntentFactory` / `CreateCopyIntentHandler` were never added.

---

## 5. Why a naïve hook on `RebuildTraderAsync` is illegal

`RebuildTraderAsync` is a **full-history replay** of every stored deal for that login, then a **wipe/replace** of `reconstructed_trades`. The worker does this every 30 seconds.

If emit were added as “for each completed XAU trade, insert OPEN+CLOSE”:

| Hazard | Spec | What would happen |
|---|---|---|
| Blind catch-up | §63, A24 §7.5 / §8.4 | Every historical open on 10001/10002 would mint new OPEN intents on first boot (and again if idempotency is only the Guid PK) |
| Duplicate on rebuild | A20 UK, A42, §68 G08 | Without `(broker, login, source_event_id, action)` the unique `IdempotencyKey` is whoever sets it; empty string would unique-crash the second row |
| OPEN of trades that already closed in the same rebuild | A24 §8.4 | Must suppress open+close in one gap when no prior shadow position exists |
| Trader not copy-eligible | §23, A24 §5 `trader_state` | 10003 is `INSUFFICIENT_DATA` (C23). 10001 is `SHADOW` only after scoring — emit must run **after** state, and must not emit for WATCH / INSUFFICIENT_DATA |
| Dirty / reverse-money bugs | A21 §4.4, B11 C1–C5 | Dirty lifecycles must not become copy intents. Current engine has no dirty bit and invents reverses |
| Dest qty = source lots | §38 | `RequestedQuantity = InitialVolumeLots` would be the forbidden `0.10 == 0.10` |
| Callback coupling | §32, §72.6 | Ingest callback/poller must not construct intents; only post-recon, post-commit |

A41 §2: *“Do not emit shadow/risk outbox rows for historical backfill.”*  
A41 §5: reconstruction TX may enqueue `trade-completed`; the **handler** (or factory in the same TX as a *new* lifecycle event) persists `copy_intents`. Backfill reconstructs; it does not enqueue stale copy work.

So emit requires **incremental** reconstruction (new deals / new lifecycle events since last checkpoint), not the current replace-all.

`SyncCheckpoint` exists as a type. Ingest does not advance a reconstruction-event cursor. There is no “deals applied through ticket T” watermark for copy.

---

## 6. Required event → intent map (guidance only — not implemented here)

When someone implements later, do **not** put `new CopyIntent` inside `TradeReconstructor`. Keep the book pure. Yield events; Application persists.

| A21 emit site (spec, not code) | Action | Qty | `source_event_id` | Notes |
|---|---|---|---|---|
| `open_lifecycle` / leftover of INOUT | `OpenExposure` | open lots / leftover lots | opening deal ticket | Skip if `!IsXauUsd` or trader state not in {SHADOW, LIVE_CANDIDATE, LIVE} (config) |
| scale-in `ENTRY_IN` | `IncreaseExposure` | add lots | that deal ticket | Same `source_trade_id` as the open lifecycle |
| `ENTRY_OUT` remaining ≠ 0 | `ReduceExposure` | closed lots | that deal ticket | Never flip; clip to remaining |
| `complete_lifecycle` | `CloseExposure` | last remaining | completing deal ticket | Same trade id; CLOSE policy (A24 §8) |
| `ENTRY_INOUT` | **two** intents, in order | close remaining; then open leftover | same ticket, two actions | Unique key includes `action`, so both may exist |
| Dirty / fail codes | **no intent** | — | — | Persist dirty trade only |
| Balance / credit / cancel | **no intent** | — | — | |
| Historical backfill / `now > expires_at` | expire or do not enqueue OPEN | — | — | CLOSE of an **existing** shadow position may still drain (A24 §8.4) |
| `EARLY_SCORE_ELIGIBLE` | outbox score path, not copy | — | — | |

Idempotency pin (A20 / A41):

```text
(source_broker_id, source_login, source_trade_id, source_event_id, action)
```

Opaque `IdempotencyKey` may store a canonical rendering of that tuple; it is not a substitute for the five-column UK.

`expires_at` / `max_signal_age` are family-specific (OPEN short, CLOSE long). Missing expiry is `INTENT_INCOMPLETE`, not “never expire.”

Preconditions the current snapshot cannot supply: `source_event_id`, `lifecycle_seq`, dirty, remaining after each deal, collector receive time, destination instrument id, linked dest/shadow position for CLOSE.

---

## 7. Adjacent holes that keep emit blocked even after a factory exists

These are not “also nice”; they block a correct emit.

| Hole | Evidence | Effect on CopyIntent |
|---|---|---|
| No A21 events from the book | `TradeReconstructor` returns trades only | Factory has no input |
| No `lifecycle_seq` | result `Id` is time-ms; entity has no seq | Reversal / netting reuse collide |
| No dirty model | B11 H1; 18 `RECON_*` codes unused | Bad books would copy |
| No `VolumeClosed` on `NormalizedDeal` | B11 C3 / F21 | INOUT qty wrong → wrong CLOSE+OPEN sizes |
| `ReplaceReconstructedAsync` new Guids | §2.3 | `SourceTradeId` cannot FK |
| SL/TP never loaded | `LoadDealsAsync` omits them | Informational only for copy; still a recon defect |
| Scoring uses **all** completed XAU, not first-3 | B11 H4 | State may be SHADOW for the wrong reason; still no intent |
| Worker login allow-list | `{10001,10002,10003,99001}` | Even a factory would only see demo logins |
| No outbox poller | A41 still design-only; 0 inserts | Persist without outbox strands shadow/risk |
| `ShadowCopyEngine` is a quote math helper | no persist, no intent | Shadow page stays empty (C13) |
| No `copy_allocations` | A20 table 29 absent as type | §38 sizing has nowhere to hang |
| `RequestedQuantity` vs dest step | `QuantityNormalizer` unused by engines (unit test says so) | OPEN would send source lots |

Reconstruction quality is independently **not** A21-complete (B11: 10/25 fixtures fail). Emitting copy from this book would copy **wrong** reverses and clipped over-closes. Fix the book (or at least refuse dirty) **before** turning on emit.

---

## 8. Tests — nothing asserts emit

| Existing fact | What it proves | CopyIntent? |
|---|---|---|
| `TradeReconstructionTests` (5 facts) | Happy-path lots / flags / first-3 **count** | **No** |
| `ExecutionAndSizingTests.Copy_intent_expires` | `CopyIntentExpiry.IsExpired` 16s vs 5s | Expiry helper only; no row |
| `RiskEngineTests` | `Evaluate` on a hand-built request `CopyIntentId="c1"` | Engine, not persist |
| A27 `CopyIntentFactoryTests` / `CopyIntentIdempotencyTests` | Required | **Files absent** |
| A27 `StaleCopyIntentExpiryTests` (20 intents after 3 min gap) | §63 | **Absent** |
| Integration `SeedingAndStoreTests` | seed has completed XAU | Does not assert `CopyIntents.Any() == false` either — gap is unpinned |

Named missing tests that would lock this gap (do not add in this change):

- `Reconstruction_does_not_emit_copy_intent_today` (characterization: after `RebuildTraderAsync`, `CopyIntents` count is 0) — honest pin
- Later: one intent per A21 event; INOUT → CLOSE then OPEN; same deal+action replay → one row; backfill → 0 OPEN; `INSUFFICIENT_DATA` → 0; dirty → 0; CLOSE not blocked by OPEN staleness

C17 already scored “copy-intent idempotency” as **MISSING**. This report is the reconstruction-side reason that score cannot turn green: there is no producer.

---

## 9. Classification vs earlier swarm notes

| Prior | Claim | Still true? |
|---|---|---|
| A29 E10 | CopyIntent type; **no** flow | **Yes** (type now has EF table map; still no flow) |
| A29 E11 | `CopyIntentAction` matches §64 | **Yes** |
| A21 §4.3 | Events defined for outbox later | **Still not implemented** |
| A30 Increment 8 | `CopyIntentFactory`, `CopyIntentFromTradeJob` | **Not on disk** |
| A41 | reconstruction TX enqueues `trade-completed` then handler persists intents | **0 outbox writes** |
| C07 | workers do not write `CopyIntent` | **Yes** |
| C14 G08 | no writer; idempotency FAIL | **Yes** — this file is the reconstruction-specific proof |
| B11 | recon is a netting book, not A21 | **Yes**; emit cannot ride that book as-is |

A29’s older “no reconstruction engine” snapshot is **stale**. The engine exists. The **emit** still does not.

---

## 10. Honesty box

| Claim someone might make | Measured |
|---|---|
| “Reconstruction emits CopyIntent” | **No.** Zero constructors, zero `DbSet` writes, zero outbox. |
| “We persist intents; we just don’t send FIX” | **No.** Table is empty by construction. Seeder does not insert. |
| “Shadow path is wired; live is off” | **No.** Shadow also requires a persisted intent (A24 §3). `ShadowCopyEngine` is unused. |
| “CopyIntent entity means Phase 5 started” | **Type + unique `IdempotencyKey` only.** G08 still FAIL. |
| “RebuildTraderAsync could add three lines and be done” | **False.** No events, unstable trade Guid, full-history replay = §63 violation. |
| “A21 says reconstruction does not copy, so this is out of scope” | Reconstruction **must** emit `XAU_LIFECYCLE_*`. Application maps them. **Both** are missing. |
| Product source modified for this report | **No.** |

---

## 11. What “done” would look like (acceptance, not a task list for this agent)

Emit is **PASS** only when all of the following are measured:

1. `TradeReconstructor` (or a sibling projector) yields A21 §4.3 events with `source_event_id`, `lifecycle_seq`, qty, side, dirty.
2. Incremental apply (checkpoint) — not wipe/replace of the whole login — feeds the factory.
3. `ICopyCandidateEvaluator` allows only configured states; XAUUSD only; dirty excluded.
4. Factory persists `CopyIntent` with `expires_at` **and** stored `max_signal_age` **before** risk/shadow.
5. Unique `(broker, login, source_trade_id, source_event_id, action)` proven under retry (A09 #15).
6. Same TX outbox `shadow-copy-intent` (and `trade-completed`); backfill does not enqueue OPEN.
7. INOUT → CLOSE then OPEN; rejected OPEN leaves flat (A24 §4.1).
8. `source_trade_id` is the A21 key (or a stable Guid derived from it), not `Guid.NewGuid()` per rebuild.
9. Characterization + factory + catch-up tests green. Live `NewOrderSingle` still off.

Until then: **reconstruction stops at `reconstructed_trades` + `trader_scores`.** The copy pipeline has no input.

---

## 12. Files cited

- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs`
- `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs`
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`
- `D:\Prop\src\Domain\Entities\ShadowOrder.cs`
- `D:\Prop\src\Domain\Entities\OutboxEvent.cs`
- `D:\Prop\src\Domain\Enums\CopyIntentAction.cs`
- `D:\Prop\src\Domain\Enums\OutboxEventType.cs`
- `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`ReconstructionScoringService`, `ITradingStore`)
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§13, 32, 63–64, 75
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` §4.3, §7
- `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` §3.1
- `D:\Prop\reports\swarm\20260818\A24_shadow_copy_spec.md` §§3–5
- `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` `copy_intents`
- `D:\Prop\reports\swarm\20260818\A41_outbox_design.md` §5
- `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md` Increment 8
- `D:\Prop\reports\swarm\20260818\B11_recon_review.md`
- `D:\Prop\reports\swarm\20260818\C14_golive_still_fail.md` G08

---

*End of C59. Reconstruction produces lifecycle snapshots and scores. It does not emit `CopyIntent`.*
