# P500_S024 — Persist-before-send outbox vs 35=D send path

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S024_outbox_gap.md` |
| Agent | P500_S024 (read-only gap; product not edited) |
| Date | 2026-08-18 |
| Assigned | Search `src` for outbox / ExecutionIntent / CopyIntent. Is there a persist-before-send outbox consumer that writes 35=D? If not, there is no safe send path. Do not edit product. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| Scope | Product C# under `D:\Prop\src` plus host workers/API that would have to consume the outbox. `mt5-sdk/vendor` ignored. |

---

## Verdict

**No. There is no persist-before-send outbox consumer that writes FIX NewOrderSingle (`35=D`). There is no safe send path.**

What exists is a **schema + souvenir writer**:

- `outbox_events` table mapping and `OutboxEvent` / `OutboxEventType` types.
- One insert site that always writes `OutboxEventType.ScoreUpdate` during demo shadow rebuild.
- `CopyIntent` rows with status `SHADOW_ONLY` (no live send).
- `ExecutionIntent` entity + unique `ClOrdId` index — **never inserted**.
- FIX TRADE path that can log on (`35=A`) and then **explicitly refuses** NewOrderSingle.

What does **not** exist (all required for a safe send):

1. Same-transaction persist of `CopyIntent` + approved `RiskDecision` + `ExecutionIntent` + outbox row **before** any socket write.
2. An outbox consumer (`IOutboxProcessor` / hosted drain) that claims unprocessed rows (`ProcessedAt IS NULL`, `FOR UPDATE SKIP LOCKED`).
3. A handler that, after durable commit, builds and writes `35=D`.
4. Any product C# that emits tag `35=D` at all.

**Safe send** here means: crash between persist and wire cannot duplicate or lose an order because the durable intent exists first, ClOrdID is assigned before send, retry is forbidden after `SentAcknowledgementUnknown`, and only one owner may drain. That pipeline is **absent**. Live 09:01Z restart: env can set `RealCopyEnabled=true` (`DependencyInjection` reads `REAL_COPY_EXECUTION_ENABLED`). That still does not create a safe path — `CopyTradingService.NewOrderSingleImplemented=false`, `AllowFixSend` is forced false, and no outbox consumer writes `35=D`.

---

## 1. Search census (product `src` + hosts)

| Query | Hits that matter | Meaning |
|---|---|---|
| `OutboxEvent` / `OutboxEvents` / `OutboxEventType` | Entity, enum, `DbSet`, one `Add` in `EfTradingStore` | Table + demo write only |
| `ProcessedAt` assignment | **0** (`ProcessedAt` only declared + indexed) | No consumer ack |
| `IOutbox*` / `IOutboxWriter` / `IOutboxProcessor` / `IOutboxHandler` / `TransactionalOutbox` | **0** in product `*.cs` | No ports |
| `SKIP LOCKED` / `pg_notify` | **0** | No claimer |
| `new ExecutionIntent` / `ExecutionIntents.Add` | **0** | Table unused |
| `CopyIntent` / `CopyIntents.Add` | Entity + demo insert in `PersistDemoShadowAsync` | Shadow-only |
| `ExecutionIntentId` | Property on `CopyIntent` only; never set | Link unused |
| `35=D` / `(35, "D")` / NewOrderSingle builder | **0** under `src` | No send codec |
| Hosted services that drain outbox | **0** | See §5 |

---

## 2. What the types actually are

### 2.1 Outbox (table, not a bus)

`D:\Prop\src\Domain\Entities\OutboxEvent.cs`

- `Id`, `Type`, `AggregateId`, `PayloadJson`, `OccurredAt`, `ProcessedAt?`, `Attempts`, `LastError?`, `CorrelationId?`
- Shape is **compatible** with a drain (unprocessed = `ProcessedAt == null`). Nobody sets `ProcessedAt`.

`D:\Prop\src\Domain\Enums\OutboxEventType.cs`

```
TradeCompleted = 0
ScoreUpdate = 1
ShadowCopyIntent = 2
RiskCheckRequest = 3
NotificationEvent = 4
```

There is **no** `NewOrderSingle` / `ExecutionSend` / `FixOutbound` type. Even a working drain of today's enum could not legally emit `35=D` without inventing a new kind (or overloading `ShadowCopyIntent`, which is not wired).

EF: `TraderDbContext.OutboxEvents` → table `outbox_events`, index on `ProcessedAt` only. No unique dedupe key, no `available_at`, no owner/lease columns.

### 2.2 CopyIntent (shadow souvenir)

`D:\Prop\src\Domain\Entities\CopyIntent.cs`

- Source identity, symbol, `Action`, direction, qty, expected price, timestamps, `Status`, unique `IdempotencyKey`.
- `RiskDecisionId?` and `ExecutionIntentId?` — **never assigned** in product code.
- Only writer: `EfTradingStore.PersistDemoShadowAsync` sets `Status = "SHADOW_ONLY"` and key `shadow:{brokerId}:{login}:{positionId}`.

`CopyIntentExpiry` is a static age check. `RiskEngine` evaluates a request DTO that **references** `CopyIntentId` as a string; it does not persist `CopyIntent` or `ExecutionIntent`.

### 2.3 ExecutionIntent (dead table)

`D:\Prop\src\Domain\Entities\ExecutionIntent.cs`

- `CopyIntentId`, `RiskDecisionId`, destination symbol, direction, `VolumeLots`, `ClOrdId?`, `FixOrderId?`, `Status`, `CreatedAt`, `SentAt?`, `FilledAt?`, `FillPrice?`, `RejectReason?`
- Unique index on `ClOrdId`.
- **Zero** `Add` / `new ExecutionIntent` under `src`, `apps`, `tests`.

`ClOrdIdFactory.Next(executionIntentId, now, sequence)` exists as a pure function. Nothing calls it on a live send path.

`ExecutionOrderStateMachine` encodes the safety rule `MayRetryNewOrderSingle` only for `NotSent` / `Rejected`. That is correct **if** a sender existed. No sender applies it.

`FixSessionOwnership.ExecutionIntentsAllowed => _hasOwnership && _reconciled` is an in-memory lock helper. Nothing gates a send on it because nothing sends.

---

## 3. The only outbox write is not persist-before-send

`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` `PersistDemoShadowAsync` (called from `ReconstructionScoringService.RebuildTraderAsync` **after** deals, reconstructed trades, and score have already been committed in **separate** `SaveChangesAsync` calls):

1. Always `OutboxEvents.Add` a **new Guid** `ScoreUpdate` with payload `{"state":"...","completed":N}`.
2. If state ≠ `SHADOW`, commit and return.
3. Else, for each completed XAU trade, insert `CopyIntent` (`SHADOW_ONLY`) + `ShadowOrder` from `ShadowCopyEngine.SimulateEntry`.
4. Commit.

This is **not** persist-before-send:

| Requirement | Measured |
|---|---|
| Same TX as raw MT5 deal | **Fail.** `UpsertDealAsync` / batch already committed. |
| Same TX as score | **Fail.** `UpsertScoreAsync` already committed. |
| Type that means “send this order” | **Fail.** Always `ScoreUpdate`, never `ShadowCopyIntent` / `RiskCheckRequest`. |
| `ExecutionIntent` + ClOrdID assigned before wire | **Fail.** No `ExecutionIntent` row. |
| Consumer marks `ProcessedAt` then sends `35=D` | **Fail.** No consumer. No `35=D`. |
| Idempotent | **Fail.** New `Id` every rebuild; worker can append forever. |

`ShadowCopyIntent` / `RiskCheckRequest` / `TradeCompleted` / `NotificationEvent` have **zero** insert sites.

---

## 4. There is no 35=D writer

Product FIX under `D:\Prop\src\Fix.CTrader`:

| File | What it writes on the wire |
|---|---|
| `Sessions/CTraderFixSession.cs` | Logon only: `(35, "A")` then read reply. Session disposed. |
| `Services/CTraderQuoteService.cs` | Builds `(35, "y")` SecurityList and `(35, "V")` MarketDataRequest (quote, not trade). |
| `Hosting/CTraderFixLogonHostedService.cs` | Calls `TryLogonAsync` for QUOTE+TRADE, then **forces** `_runtime.RealCopyEnabled = false`. Log: “NewOrderSingle still disabled”. |
| `Testing/FixSimulationHarness.cs` | Inbound fixtures: `A`, `3`, `0`, `y`, `X`, `8`. No outbound `D`. |
| `Parsing/FixMessageParser.cs` | Generic assemble/parse. No NewOrderSingle template. |

Grep of `D:\Prop\src` for `35=D`, `(35, "D")`, `MsgType=D`: **0 hits**.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false**. `DependencyInjection.AddTraderIntelligence` comments: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” and sets `RealCopyEnabled = false`.

---

## 5. Workers do not consume the outbox

Registered hosted services (`DependencyInjection.cs`):

- `LiveIngestHostedService` — MT5 ingest / rebuild, not outbox drain.
- `CTraderFixLogonHostedService` — one-shot logon, persist session rows.

`D:\Prop\apps\fix-worker\Worker.cs` loop:

- Reads `CTrader:RealCopyExecutionEnabled`.
- Stamps FIX session rows `Disconnected` / “NewOrderSingle remains off.”
- If the flag is true, **logs a warning and still does not send**.
- Does **not** query `OutboxEvents`, `CopyIntents`, or `ExecutionIntents`.

`D:\Prop\apps\mt5-worker\Worker.cs` — ingest loop, no outbox drain.

`D:\Prop\apps\api\Program.cs` `/api/health` returns **literal** `outboxBacklog = 0` (does not count `outbox_events`). `/api/reconciliation/status` note: “NewOrderSingle still off”.

No type named `OutboxConsumer`, `OutboxProcessor`, `OutboxDispatcher`, or `FixOrderSender` exists under `src` or `apps`.

---

## 6. Intended path vs implemented path

Docs (`D:\Prop\docs\ctrader-fix.md`) describe TRADE: NewOrderSingle `35=D` → ExecutionReport `35=8` → persist fill. Architecture swarm specs (A41 / C58 / D45) require: persist domain + outbox in **one** commit, then a background processor sends.

**Implemented path today:**

```
MT5 deal upsert (commit)
  → reconstruct (commit)
  → score (commit)
  → PersistDemoShadowAsync:
       ScoreUpdate outbox (always)
       CopyIntent SHADOW_ONLY + ShadowOrder (if SHADOW)
       commit
  → [dead letter] nobody reads outbox
  → FIX: 35=A logon only; RealCopyEnabled forced false
  → ExecutionIntent table empty
  → 35=D never built
```

There is **no** branch from `CopyIntent` → risk persist → `ExecutionIntent` → outbox → consumer → TRADE socket.

---

## 7. Why “just send from the callback” would also be unsafe

Even if someone added a `35=D` write on the ingest/FIX logon thread without this outbox:

- Crash after TCP write / before ack → unknown state; `MayRetryNewOrderSingle` would correctly refuse retry **only if** `SentAcknowledgementUnknown` was persisted first.
- Crash before persist / after send → ghost fill, no ClOrdID in DB, recon cannot match.
- Two API/worker instances → no Redis fencing used; `FixSessionOwnership` is unused in-memory.
- `CopyIntent` expiry / kill switch / `RealExecutionEnabled` in `RiskEngine` are not on a send path.

Hence: **no consumer that writes 35=D after persist ⇒ no safe send path.** The current process is capital-safe only because send is unimplemented, not because the outbox is working.

---

## 8. Gap list (do not implement in this task)

Product was not edited. Remaining work if a later increment is authorized:

1. Application ports: `IOutboxWriter` (same `DbContext` TX as domain write) + `IOutboxProcessor` + typed handlers. No Kafka.
2. Persist `CopyIntent` + `RiskDecisionRecord` + `ExecutionIntent` (ClOrdID assigned) + outbox row (`ShadowCopyIntent` or a new `FixNewOrderSingle` kind) in **one** `SaveChangesAsync`.
3. Hosted drain: `ProcessedAt IS NULL` claim (`SKIP LOCKED`), then build `35=D` on TRADE only if `ExecutionIntentsAllowed` && `RealCopyEnabled` && risk still green.
4. On send attempt: persist `SentAt` + `SentAcknowledgementUnknown` **before or atomically with** considering the message sent; never retry that ClOrdID.
5. Stop lying: `/api/health.outboxBacklog` must count unprocessed rows; do not hardcode `0`.
6. Until 1–4 exist, keep `RealCopyEnabled = false` (already forced).

---

## 9. One-liner

**`outbox_events` is a demo `ScoreUpdate` log; `CopyIntent` is SHADOW_ONLY; `ExecutionIntent` is unused; no consumer writes `35=D` — there is no safe send path.**
