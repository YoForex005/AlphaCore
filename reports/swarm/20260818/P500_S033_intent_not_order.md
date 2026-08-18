# P500_S033 — Intents are not venue orders

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S033_intent_not_order.md` |
| Agent | P500_S033 (read-only; product not edited) |
| Date | 2026-08-18 |
| Assigned | Read Domain Entities `CopyIntent.cs` `ExecutionIntent.cs` `ShadowOrder.cs` and who writes them. Intents are not venue orders. Profit requires a later guarded sender. Do not edit product. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| SUT | Domain entities + sole product writers + FIX/risk/outbox consumers |
| Law | Architecture §§4, 32–33, 63–64, 75; A23 persist-before-send; A24 shadow vs live; A41 outbox; A42 ClOrdID; A70 FSM |

---

## Verdict

**Confirmed. `CopyIntent`, `ExecutionIntent`, and `ShadowOrder` are ledger types. None of them is a cTrader / FIX venue order.**

A row in `copy_intents` or `shadow_orders` does not move capital. A row in `execution_intents` would still not move capital until a **later guarded sender** persists a unique `ClOrdId`, passes risk + ownership + reconcile + flag, and writes FIX `NewOrderSingle` (`35=D`). That sender **does not exist**.

| Type | Table | What it is | What it is not | Product writer |
|---|---|---|---|---|
| `CopyIntent` | `copy_intents` | “We noticed a source lifecycle we *might* copy” | Not tag 35=D, not OrderQty, not a fill | **One:** `EfTradingStore.PersistDemoShadowAsync` — `Status = "SHADOW_ONLY"` |
| `ExecutionIntent` | `execution_intents` | Persist-before-send slot (unique `ClOrdId`) | Not a sent order until `SentAt` + wire | **Zero.** Entity + EF map only |
| `ShadowOrder` | `shadow_orders` | Simulated dest fill for dashboard slippage | Not a venue fill, not dest P&L | **One:** same `PersistDemoShadowAsync` after `ShadowCopyEngine.SimulateEntry` |

**Profit from copy requires:** approved `ExecutionIntent` → guarded sender → venue fill → dest mark. Today the chain stops at **SHADOW_ONLY souvenir rows**. Dashboard `ShadowPnl` is Σ `SourceVsShadowSlippage`, not money.

**Risk to capital from these types: NONE (`SAFE_BY_ABSENCE`).** Do not greenwash that as “copy is working.”

---

## 0. Entities as measured (full read)

### 0.1 `D:\Prop\src\Domain\Entities\CopyIntent.cs`

```5:24:D:\Prop\src\Domain\Entities\CopyIntent.cs
public sealed class CopyIntent
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long SourceLogin { get; set; }
    public Guid? SourceTradeId { get; set; }
    public long SourcePositionId { get; set; }
    public string CanonicalSymbol { get; set; } = "XAUUSD";
    public CopyIntentAction Action { get; set; }
    public TradeDirection Direction { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ExpectedPrice { get; set; }
    public DateTimeOffset SourceEventTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? RiskDecisionId { get; set; }
    public Guid? ExecutionIntentId { get; set; }
}
```

This is a **source-side command**. Fields that would bind it to a live send (`RiskDecisionId`, `ExecutionIntentId`) are nullable and **never assigned** by the only writer. There is no dest account, no dest instrument id, no FIX tags, no venue order id.

`CopyIntentAction` (`D:\Prop\src\Domain\Enums\CopyIntentAction.cs`): `OpenExposure | IncreaseExposure | ReduceExposure | CloseExposure`. Vocabulary of **desired exposure change**, not of FIX `Side`/`OrdType`.

### 0.2 `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`

```5:21:D:\Prop\src\Domain\Entities\ExecutionIntent.cs
public sealed class ExecutionIntent
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public Guid RiskDecisionId { get; set; }
    public string DestinationSymbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal VolumeLots { get; set; }
    public string? ClOrdId { get; set; }
    public string? FixOrderId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? FilledAt { get; set; }
    public decimal? FillPrice { get; set; }
    public string? RejectReason { get; set; }
}
```

This is the **persist-before-send** record (§33). Even a fully populated row would still be **our** intent until:

1. `ClOrdId` assigned and unique-committed **before** any socket write.
2. A sender writes `35=D` and stamps `SentAt`.
3. Venue `ExecutionReport` fills `FixOrderId` / `FilledAt` / `FillPrice`.

None of those steps have a product caller. EF unique index is on `ClOrdId` (`TraderDbContext` 136–141). Unique index on an empty table is not a send path.

### 0.3 `D:\Prop\src\Domain\Entities\ShadowOrder.cs`

```5:17:D:\Prop\src\Domain\Entities\ShadowOrder.cs
public sealed class ShadowOrder
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public Guid BrokerId { get; set; }
    public long SourceLogin { get; set; }
    public TradeDirection Direction { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Spread { get; set; }
    public decimal SourceVsShadowSlippage { get; set; }
    public DateTimeOffset FilledAt { get; set; }
}
```

Name is the hazard. This is a **simulated dest print**, not an order. No `ClOrdId`, no dest account, no `OrdStatus`, no leaves/cum qty. `FilledAt` is `UtcNow` of the demo rebuild, not a venue TransactTime.

`ShadowCopyEngine` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`) returns in-memory `ShadowFill` / `ShadowPosition` records. It **never** constructs `ShadowOrder`. The store maps `SimulateEntry` → `new ShadowOrder`.

---

## 1. Who writes them (product `*.cs` census)

Product scope: `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests` (ignore `_tmp_*` harnesses).

| Pattern | Product hits that construct / persist |
|---|---|
| `new CopyIntent` | **1** — `EfTradingStore.PersistDemoShadowAsync` L295 |
| `CopyIntents.Add` | **1** — same method L310 |
| `new ShadowOrder` / `ShadowOrders.Add` | **1** — same method L321 |
| `new ExecutionIntent` / `ExecutionIntents.Add` | **0** |
| `GuardedNewOrderSingle` | **0** |
| `35=D` / `(35, "D")` | **0** |
| `RiskDecisions.Add` / `new RiskDecisionRecord` | **0** (engine is unit-test only) |

### 1.1 Sole writer — `EfTradingStore.PersistDemoShadowAsync`

`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` L251–337.

Control flow:

```text
always: OutboxEvent Type=ScoreUpdate
if state != TraderState.SHADOW → SaveChanges; return
if no DestinationQuotes row → SaveChanges; return
foreach completed XAU trade (idempotency key shadow:{broker}:{login}:{positionId}):
    new CopyIntent { Status = "SHADOW_ONLY", Action = OpenExposure, ExpiresAt = OpenedAt+15s }
    ShadowCopyEngine.SimulateEntry(..., delay 80ms)
    new ShadowOrder { prices from simulated fill }
SaveChanges
```

What this writer **does not** do:

- Call `RiskEngine.Evaluate`
- Persist `RiskDecisionRecord`
- Persist `ExecutionIntent`
- Set `CopyIntent.RiskDecisionId` / `ExecutionIntentId` / `SourceTradeId` / `SourcePositionId` / `Direction`
- Check `CopyIntentExpiry.IsExpired`
- Convert source lots via `QuantityNormalizer` (echoes `MaxVolumeLots`)
- Emit outbox `ShadowCopyIntent` or `RiskCheckRequest` (uses `ScoreUpdate`)
- Touch FIX

`ExpiresAt = trade.OpenedAt.AddSeconds(15)` on a historical demo tape is already stale vs `UtcNow`. Even if a sender existed, these rows would fail a lawful age gate.

### 1.2 Who calls the writer

| Caller | Path | When |
|---|---|---|
| `ReconstructionScoringService.RebuildTraderAsync` | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L144 | After `UpsertScoreAsync` |
| `DemoSeeder.SeedAsync` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L137 | First empty-broker boot |
| `apps/mt5-worker/Worker.cs` | L34 | Periodic rebuild of demo logins |
| `apps/api/Program.cs` | L135 | API-triggered rebuild |
| `LiveIngestHostedService` | `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` L113 | Live ingest rebuild |

None of these is a FIX worker. None reads `ExecutionIntents`.

### 1.3 Who does **not** write them

| Component | Touches intents? |
|---|---|
| `TradeReconstructor` | No. Returns snapshots only (C59 still true on **events**). |
| `BaselineScorer` | No. Emits `TraderState` only. |
| `RiskEngine` | No persist. Tests invent `CopyIntentId = "c1"`. |
| `CopyIntentExpiry` / `ClOrdIdFactory` / `ExecutionOrderStateMachine` | Pure helpers; **0** product callers that persist. |
| `apps/fix-worker/Worker.cs` | Heartbeats `fix_sessions` to `Disconnected`; **never** reads `CopyIntents` / `ExecutionIntents`. |
| `CTraderFixSession` | Logon `35=A` only. No `35=D`. |
| `FixSessionOwnership.ExecutionIntentsAllowed` | In-memory fence; unused by worker. |
| Dashboard `EfDashboardQueries` | Sums `ShadowOrders.SourceVsShadowSlippage` only. Does not list intents. |
| Web `/shadow` | Static copy; no `GET` portfolio of intents. |

---

## 2. Spec pipeline vs product pipeline

Architecture §32 / §75 / A23 / A24 (legal live path):

```text
source event
  → persist CopyIntent          (command; not an order)
  → RiskEngine.Evaluate         (may REJECT / REDUCE)
  → persist RiskDecision
  → persist ExecutionIntent     (still not an order; ClOrdId unique, Status=not_sent)
  → guarded sender (ownership + reconciled + flag + AllowFixSend + MayRetry)
  → NewOrderSingle 35=D         ← THIS is the venue order
  → ExecutionReport             ← THIS is the venue fill
  → dest position / P&L         ← THIS is profit or loss
```

Product today:

```text
deals → reconstruct snapshot → score
  → if SHADOW + dest quote:
        CopyIntent SHADOW_ONLY
        ShadowOrder (simulated fill)
        outbox ScoreUpdate
  → STOP

ExecutionIntent: never
RiskDecisionRecord: never
35=D: never
dest P&L: 0 (dashboard hard-codes live P&L fields to 0)
```

`LiveRuntimeStatus.Snapshot` is honest when the flag is false:

> “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.”

DI forces that flag: `RealCopyEnabled = false` (`DependencyInjection.cs` L40–41) with the comment “Live NewOrderSingle is not implemented.”

`RiskEngine` itself documents the split (`RiskEngine.cs` L90–93): if `RealExecutionEnabled == false` it still *could* evaluate, but “never allows FIX send” except the later `allowSend` conjunction (flag ∧ kill-switch None ∧ reconciled ∧ venue healthy). **No caller reaches that line from a persisted intent.**

---

## 3. Why profit cannot come from these rows

| Claim someone might make | Measured |
|---|---|
| “We have CopyIntents, so we are copying” | Those rows are `SHADOW_ONLY` backfill of **completed** historical OPENs. No dest account. |
| “ShadowOrder.FilledAt means we filled” | Simulated mid-book touch + 80 ms model. No venue. |
| “ShadowPnl on overview is profit” | `Sum(SourceVsShadowSlippage)` — a slip statistic, not cash. Live dest P&L columns are **0**. |
| “ExecutionIntent exists so persist-before-send is done” | Type + unique index. **0 inserts.** |
| “ClOrdIdFactory / MayRetryNewOrderSingle protect us” | Unit-tested math. Zero send arm. After a hypothetical send, `MayRetry` is false — still no sender. |
| “Turn on REAL_COPY_EXECUTION_ENABLED and we profit” | Fix worker **refuses** even if config is true (L45–46). No `35=D` codec. DI hard-wires false. |
| “A later sender can just dump CopyIntent to FIX” | Illegal. Missing risk, qty conversion (§38), dest instrument, expiry, ownership, reconcile, persist-before-send. Demo `ExpiresAt` already stale. |

**Profit requires a later guarded sender.** Minimum legal sender (not built):

1. Drain only `ExecutionIntent` rows in `not_sent` with durable unique `ClOrdId` (A42).
2. Re-check `CopyIntentExpiry`, kill switch, `FixSessionOwnership.ExecutionIntentsAllowed`, TRADE `READY_FOR_EXECUTION`, `RiskDecision.AllowFixSend`, `REAL_COPY_EXECUTION_ENABLED`.
3. Persist `SentAt` / `SentAcknowledgementUnknown` **before** `WriteAsync`.
4. Encode `35=D` with dest `OrderQty` (converted lots), dest instrument id, fencing token.
5. Never retry after send (`MayRetryNewOrderSingle` false on unknown ack).
6. Apply ER → dest position → reconcile.

Until that exists, intents are **paper**.

---

## 4. Field-level: who sets what

### CopyIntent (writer = PersistDemoShadowAsync only)

| Field | Set? | Value |
|---|---|---|
| `Id` | Yes | `Guid.NewGuid()` |
| `BrokerId` / `SourceLogin` | Yes | from rebuild |
| `SourceTradeId` | **No** | stays null |
| `SourcePositionId` | **No** | stays 0 (key uses `trade.PositionId` only in idempotency string) |
| `CanonicalSymbol` | Yes | from reconstructed trade |
| `Action` | Yes | **always** `OpenExposure` |
| `Direction` | **No** | default 0 |
| `RequestedQuantity` | Yes | source `MaxVolumeLots` (not dest step) |
| `ExpectedPrice` | Yes | `EntryVwap` |
| `SourceEventTime` | Yes | `OpenedAt` of a **completed** trade |
| `CreatedAt` | Yes | `UtcNow` |
| `ExpiresAt` | Yes | `OpenedAt + 15s` (typically already expired) |
| `Status` | Yes | `"SHADOW_ONLY"` (not A26 `PENDING`/`SHADOWED`) |
| `IdempotencyKey` | Yes | `shadow:{brokerId}:{login}:{positionId}` |
| `RiskDecisionId` | **No** | |
| `ExecutionIntentId` | **No** | |

### ExecutionIntent

Every field: **unset**. No writer.

### ShadowOrder (same method, after `SimulateEntry`)

| Field | Source |
|---|---|
| `CopyIntentId` | just-minted intent `Id` |
| `BrokerId` / `SourceLogin` | rebuild |
| `Direction` | `trade.Direction` (here, not on CopyIntent) |
| `Quantity` / `Price` / `Spread` / `SourceVsShadowSlippage` / `FilledAt` | `ShadowFill` |

---

## 5. Adjacent types that are also not orders

| Type | Role | Writer |
|---|---|---|
| `RiskDecision` (in-memory record) | Engine output DTO | tests only |
| `RiskDecisionRecord` | Persist of that DTO | **0** |
| `OutboxEvent` `ScoreUpdate` | “score changed” souvenir | `PersistDemoShadowAsync` |
| `OutboxEventType.ShadowCopyIntent` | Enum member 2 | **0 inserts** |
| `ClOrdIdFactory.Next` | Would mint tag 11 from execution-intent id | tests only |
| `ExecutionOrderStateMachine` | Status math after a send that never happens | tests only |

---

## 6. Honesty box

| Forbidden claim | Truth |
|---|---|
| “EX5 / copy is live” | N/A to this repo; live FIX send is off. |
| “Shadow orders are venue orders” | They are simulated rows. |
| “CopyIntent is the order we send” | It is a candidate command. Live send needs ExecutionIntent + guarded 35=D. |
| “We just need to wire FIX to CopyIntent” | That would skip risk, persist-before-send, qty conversion, expiry, ownership. **Illegal.** |
| “Dashboard profit proves the sender” | Dashboard slip sum ≠ dest cash. Live P&L = 0. |
| Product edited for this report | **No.** |

---

## 7. Files cited

- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`
- `D:\Prop\src\Domain\Entities\ShadowOrder.cs`
- `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs`
- `D:\Prop\src\Domain\Enums\CopyIntentAction.cs`
- `D:\Prop\src\Domain\Enums\OutboxEventType.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs`
- `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`PersistDemoShadowAsync`)
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`ReconstructionScoringService`)
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- Prior: `C59_copyintent_gap.md` (stale on “zero writers”), `D47_copyintent.md`, `P500_S024_outbox_gap.md`, `A003_fix_noloss.md`

---

*End of P500_S033. Product source was not modified. Intents and shadow rows are not venue orders. Profit requires a later guarded sender that does not exist.*
