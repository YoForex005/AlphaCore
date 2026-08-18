# D17 — ExecutionOrderStateMachine + ClOrdIdFactory (measured close-read)

| Field | Value |
|---|---|
| Agent | D17 (execution FSM / ClOrdID close-read only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\D17_exec_fsm.md` |
| Workspace | `D:\Prop` |
| Assigned | Read `ExecutionOrderStateMachine` and `ClOrdIdFactory`. Write this report. **Do not modify product source.** |
| Product source modified | **No.** This report is the only write. |
| Law | Architecture v2 **§33**, **§34**, **§41**, **§70.4–§70.6**, **§72.8–10** |
| Binding siblings | A42 (persist-before-send + unique `ClOrdID`), A70 (destination-order graph + duplicate ER), A25/A47 (recovery), A100 G09, B16 (prior no-blind-retry review) |
| Classification | `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` (architecture §73.B) |

This file re-reads the two Domain types from disk. It does **not** inherit B16’s verdict as a fact; every cell below was re-evaluated against the 2026-08-18 bytes. Where D17 and B16 agree, that is independent confirmation. Where they would disagree, this file wins for the current SHA.

---

## 0. Verdict

**The two types are a mapper + two predicates + a string mint. They are not an execution engine.**

| Type | Class | One-line |
|---|---|---|
| `ExecutionOrderStateMachine` | **EXISTS_NEEDS_REFACTOR** | Correct closed form for *no NOS from the two unknown statuses*. `Apply` is not a legal-transition table. Qty / ExecID / fingerprint unused. |
| `ClOrdIdFactory` | **EXISTS_NEEDS_REFACTOR** | Clock + `sequence:D4` + truncated intent prefix. **Not** A42 `From(execution_intent_id)`. Sequence suffix is the **overruled** A05 shape. |
| `ExecutionOrderStatus` | **EXISTS_AND_GOOD** (vocabulary) | Eight values match §33 seven + §34 overlay. Graph is **not** enforced by the enum. |
| `ExecutionReportInput` | **EXISTS_NEEDS_REFACTOR** | Carries `LastQty`/`CumQty`/`LeavesQty`/`ClOrdId`/`VenueOrderId`/`Text` and `Apply` never reads them. |
| Persist-before-send / T3 arm | **MISSING** | `ExecutionIntent` has no `send_armed_at` / `sent_at`. |
| Production caller | **MISSING** | No Application, FIX worker, or connector invokes either type. |
| Live `35=D` | **OFF by absence** | Worker logs a refusal. That is **vacuous safety**, not a proof the FSM will hold. |

**One-line law check (§34):** *“Do NOT blindly send the order again.”*

- `MayRetryNewOrderSingle(SentAcknowledgementUnknown)` = **false**. Measured.
- `MayRetryNewOrderSingle(ExecutionStateUnknown)` = **false**. Measured in code; **not** asserted by the disconnect unit fact.
- The factory will mint a **new** tag 11 on `sequence++` or a clock tick. The helper never sees that call.
- Crash after a future write that left the row `NotSent` still yields `MayRetry=true`. That is the classic §34 bug, and it is **not** closed by these types.

Do **not** treat this as A70-complete, A42-complete, §70.4 unique-ClOrdID proven, or A100 G09 PASS.

---

## 1. Method (read-only)

Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/`) were **not** edited.

| Step | What |
|---|---|
| 1 | Full-read `ExecutionOrderStateMachine.cs`, `ClOrdIdFactory.cs`, `ExecutionOrderStatus.cs`, `ExecutionIntent.cs`. |
| 2 | Full-read `ExecutionAndSizingTests.cs` (only consumer). |
| 3 | Read `TraderDbContext` `execution_intents` map, `Worker.cs`, `CTraderFixOptions`, `FixSessionOwnership`, `FixSimulationHarness` ER builders. |
| 4 | `grep` product `*.cs` for `ClOrdIdFactory`, `ExecutionOrderStateMachine`, `MayRetryNewOrderSingle`, `AfterSendAttempt`, `AfterDisconnectWithUnknownAck`, `NewOrderSingle`, `35=D`. |
| 5 | SHA-256 the SUT files. Compare hashes to B16. |
| 6 | Evaluate `Apply` / `MayRetry` / `RequiresReconciliation` for every `ExecutionOrderStatus` and every `MapOrdStatus` key. |

---

## 2. Files measured

| Path | Bytes | Lines | SHA-256 | LastWrite (local) |
|---|---:|---:|---|---|
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | 2177 | 56 | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` | 2026-08-18 13:08:10 |
| `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs` | 658 | 17 | `CCC5506F2661C9D23EEA83070C1BFE5585158508718ECF1781C7C298E013D0A1` | 2026-08-18 13:08:10 |
| `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` | 260 | 13 | `801D89D0A5D0E73F76EC195776C5A4D2BA3A09630F13A148C1B9C0AF27D9E7AF` | 2026-08-18 13:06:08 |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | 756 | 20 | `4E5EEDFAAED61B56573C5F1AC7D49F9C8424F27D5A4CEE023F756FA71F22D6B4` | 2026-08-18 13:09:03 |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | 2144 | 62 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | 2026-08-18 13:17:42 |

Hashes match B16. The two SUTs have **not** moved since that review.

`grep` of the five identifiers over `D:\Prop\**\*.cs`:

| Identifier | Hits |
|---|---|
| `ExecutionOrderStateMachine` | definition + `tests\Unit\ExecutionAndSizingTests.cs` (3 facts) |
| `ClOrdIdFactory` | definition + the same test file (1 fact) |
| `MayRetryNewOrderSingle` | definition + `Unknown_ack_cannot_retry_new_order` |
| `AfterSendAttempt` / `AfterDisconnectWithUnknownAck` | definitions + the two named facts |
| `NewOrderSingle` / `35=D` | comments / log strings in `apps\fix-worker\Worker.cs` and `CTraderFixOptions` XML doc |

**No DI registration.** `Infrastructure\DependencyInjection.cs` registers reconstructor, scorer, ingest, dashboard — not the factory and not the machine.

---

## 3. `ExecutionOrderStatus` (the vocabulary)

```csharp
public enum ExecutionOrderStatus
{
    NotSent = 0,
    SentAcknowledgementUnknown = 1,
    Accepted = 2,
    PartiallyFilled = 3,
    Filled = 4,
    Rejected = 5,
    Cancelled = 6,
    ExecutionStateUnknown = 7
}
```

| # | Enum | Architecture §33/§34 name | Terminal? |
|---|---|---|---|
| 0 | `NotSent` | not sent | no |
| 1 | `SentAcknowledgementUnknown` | sent but acknowledgement unknown | no |
| 2 | `Accepted` | accepted | no |
| 3 | `PartiallyFilled` | partially filled | no |
| 4 | `Filled` | filled | **yes** |
| 5 | `Rejected` | rejected | **yes** |
| 6 | `Cancelled` | cancelled | **yes** |
| 7 | `ExecutionStateUnknown` | `EXECUTION_STATE_UNKNOWN` | parking overlay |

A42/A70 also name `expired`, `not_on_venue`, `abandoned`. **Those members do not exist.** Expired IOC leftover therefore cannot be expressed as a terminal cancel-equivalent; see §5.3.

The enum does not carry quantities, venue ids, or a “legal next” set. Persistence stores it as the default EF integer on `execution_intents.status`.

---

## 4. `ExecutionReportInput` (same file, unused payload)

```csharp
public sealed record ExecutionReportInput(
    string ClOrdId,
    string? VenueOrderId,
    string ExecType,
    string OrdStatus,
    decimal? LastQty,
    decimal? CumQty,
    decimal? LeavesQty,
    string? Text);
```

| Field | Used by `Apply`? |
|---|---|
| `ClOrdId` | **no** — Apply is not keyed; a report for a different 11 still moves status |
| `VenueOrderId` | **no** — tag 37 never adopted |
| `ExecType` | **only if** `OrdStatus` is null/whitespace |
| `OrdStatus` | **yes** — preferred map key |
| `LastQty` / `CumQty` / `LeavesQty` | **no** — cannot detect double-book or `14+151≠38` |
| `Text` | **no** — cannot parse `ORDER_NOT_FOUND` |

Missing vs A70 `ExecutionReportInput`: `ExecId`, `LastPx`, `OrderQty`, `PosMaintRptId`, `TransactTime`, `MsgSeqNum`, `SessionKey`, `VenueId`, fingerprint inputs.

That is why §70.5 (duplicate `35=8` ≠ second fill) **cannot** be implemented inside this type.

---

## 5. `ExecutionOrderStateMachine` surface

Static class. No fields. No I/O. Full public API:

```text
AfterSendAttempt()                              → SentAcknowledgementUnknown
AfterDisconnectWithUnknownAck()                 → ExecutionStateUnknown
Apply(current, report)                          → mapped OrdStatus/ExecType, three absorb rules
MayRetryNewOrderSingle(status)                  → status is NotSent or Rejected
RequiresReconciliation(status)                  → SentAcknowledgementUnknown or ExecutionStateUnknown
```

Private:

```text
MapOrdStatus(ordStatus, execType)
    key = blank(ordStatus) ? execType : ordStatus
    key.ToUpperInvariant() switch { … }
```

### 5.1 The two constants ignore `current`

`AfterSendAttempt()` and `AfterDisconnectWithUnknownAck()` take **no** current status. They always return the same enum value.

| Caller mistake | Effect |
|---|---|
| `status = AfterDisconnect…()` on a row that is already `Filled` | Overwrites a terminal fill to `ExecutionStateUnknown` |
| `status = AfterSendAttempt()` on a row that is already `SentAcknowledgementUnknown` | Harmless restatement |
| `status = AfterSendAttempt()` on `NotSent` without a T3 arm | Looks correct **and** is the crash-window lie if persist of `SentAcknowledgementUnknown` happens *after* the socket write |

A70: uncertain write → unknown, **never** back to `NotSent`. These helpers cannot enforce that; they only emit the destination status. The worker must not assign them onto terminal rows.

### 5.2 `MapOrdStatus` — measured keys

Prefers tag 39. Falls back to tag 150 only when 39 is null/whitespace. Upper-invariant:

| Key (after upper) | Result |
|---|---|
| `0` / `NEW` | `Accepted` |
| `1` / `PARTIAL` / `PARTIALLY FILLED` | `PartiallyFilled` |
| `2` / `FILL` / `FILLED` | `Filled` |
| `4` / `CANCELED` / `CANCELLED` | `Cancelled` |
| `8` / `REJECTED` / `REJECT` | `Rejected` |
| `A` / `PENDING_NEW` | `Accepted` |
| **anything else** | `ExecutionStateUnknown` |

Official FIX / RoE values that **fall through to unknown**:

| Inbound | Why it matters |
|---|---|
| `C` / `EXPIRED` (`39=C`, `150=C`) | A70 / RoE: IOC leftover is **cancelled** (terminal, `MayRetry=false`). Today it is **unknown** → `RequiresReconciliation=true`. Conservative vs double-send; noisy vs book. Harness `SimulateExecutionReport_Expired` emits exactly this pair. |
| `F` as the **only** key (`150=F`, 39 blank) | Official Trade exec type. Mapper wants `FILL`/`FILLED`/`2`, **not** `F`. A trade ER that omitted 39 parks as unknown instead of filled. Healthy cTrader fills send both `150=F` and `39=1|2`, so the common path is saved by the 39 preference. |
| `I` as the only key (`150=I`, 39 blank) | Status snapshot. Parks unknown. Correct “do not guess.” |
| `I` **with** `39=0` | Maps to **`Accepted`**. Harness `SimulateExecutionReport_UnknownState` builds `150=I` + `39=0` and comments “unknown state.” **The comment is wrong relative to `Apply`.** That pair is a working New snapshot. |
| `5` / `REPLACED` | Replace ack. A70: child order. Today unknown. |
| `3` / `6` / `9` (DoneForDay / PendingCancel / Suspended) | unknown |
| `35=3` session Reject / `35=j` business reject stuffed in as `REJECTED` | Would map to `Rejected` → `MayRetry=true`. A70: session reject after a possible write is **unknown**, not venue-rejected. **This is a future blind-retry license** if a parser is sloppy. |

`ToUpperInvariant` makes `new`/`New`/`NEW` equivalent. Numeric codes are strings (`"0"`), not integers. A leading space on 39 is **not** trimmed (`IsNullOrWhiteSpace` is all-or-nothing) — `" 0"` is unknown.

### 5.3 `Apply` — three absorb rules, then overwrite

```csharp
var status = MapOrdStatus(report.OrdStatus, report.ExecType);
if (current == Filled && status != Filled) return current;
if (current == Rejected || current == Cancelled) return current;
return status;
```

There is **no** `LegalTransition(current, proposed)` table. Non-terminal current is **replaced** by the mapped value, including backward and skip-ahead.

Measured `Apply(current, mapped)` (mapped = MapOrdStatus result):

| current \ mapped | Accepted | Partial | Filled | Rejected | Cancelled | Unknown |
|---|---|---|---|---|---|---|
| `NotSent` | Accepted | Partial | Filled | Rejected | Cancelled | Unknown |
| `SentAcknowledgementUnknown` | Accepted | Partial | Filled | Rejected | Cancelled | Unknown |
| `Accepted` | Accepted | Partial | Filled | Rejected | Cancelled | Unknown |
| `PartiallyFilled` | **Accepted** | Partial | Filled | Rejected | Cancelled | Unknown |
| `Filled` | **Filled** | **Filled** | Filled | **Filled** | **Filled** | **Filled** |
| `Rejected` | **Rejected** | **Rejected** | **Rejected** | Rejected | **Rejected** | **Rejected** |
| `Cancelled` | **Cancelled** | **Cancelled** | **Cancelled** | **Cancelled** | Cancelled | **Cancelled** |
| `ExecutionStateUnknown` | Accepted | Partial | Filled | Rejected | Cancelled | Unknown |

A70 §5.1 / §5.2 verdict on the same cells:

| Cell | A70 | Code |
|---|---|---|
| `PartiallyFilled` + `39=0` → `Accepted` | **illegal** (no backward) | **allowed** |
| `PartiallyFilled` + `39=8` → `Rejected` | **illegal** if `cum_qty>0` (do not zero fills) | **allowed**, and qty is not even consulted |
| `Accepted` + `39=8` | legal only if `cum_qty=0` | always allowed |
| `Filled` + non-fill | absorb (keep Filled) | **PASS** |
| `Rejected` / `Cancelled` + anything, including a late `39=2` | absorb status; persist ER as `UnexpectedFill` | status absorb **PASS**; ER persist / issue **MISSING** |
| `ExecutionStateUnknown` → working/terminal via ER | legal after TRADE is up (A70 §9) | allowed with no recon gate |
| `ExecutionStateUnknown` → `NotSent` | **illegal** | mapper never emits `NotSent`, so this edge cannot happen through `Apply` |
| `sent_unknown` → `NotSent` | **illegal** | same — mapper cannot emit `NotSent` |
| skip-ahead `sent_unknown` → `Filled` | legal (IOC) | **PASS** |
| `39=C` | `Cancelled` | **Unknown** (see §5.2) |

`Filled` absorb is **asymmetric**: only non-`Filled` proposals are dropped. A restated `39=2` returns `Filled` (fine). `Rejected`/`Cancelled` absorb **including** a later real fill. Prefer that over re-opening the id for send; the missing half is an `UnexpectedFill` issue row (`ReconciliationIssueType.UnexpectedFill` exists as an enum and is unused here).

### 5.4 Retry and recon predicates (all eight statuses)

Evaluated against the enum, not against tests.

| `ExecutionOrderStatus` | `MayRetryNewOrderSingle` | `RequiresReconciliation` |
|---|---|---|
| `NotSent` | **true** | false |
| `SentAcknowledgementUnknown` | **false** | **true** |
| `Accepted` | false | false |
| `PartiallyFilled` | false | false |
| `Filled` | false | false |
| `Rejected` | **true** | false |
| `Cancelled` | false | false |
| `ExecutionStateUnknown` | **false** | **true** |

This **is** the A70 §10 closed form for the helper:

```text
MayRetryNewOrderSingle  = NotSent | Rejected
RequiresReconciliation  = SentAcknowledgementUnknown | ExecutionStateUnknown
```

Interpretation that the helper **does not encode**:

| Status | Retry `35=D` on **this** tag 11 | New tag 11 |
|---|---|---|
| `NotSent` | yes — **first** send only | no — already has 11 |
| `SentAcknowledgementUnknown` | **never** | only after recon `not_on_venue` |
| `Accepted` / `PartiallyFilled` | never | never while working |
| `Filled` | never | never |
| `Cancelled` | never | new risk decision only |
| `Rejected` | **never** (11 is burned) | **yes**, new row + new 11, if risk re-approves |
| `ExecutionStateUnknown` | **never** | only after recon `not_on_venue` |

`MayRetry(Rejected)==true` is therefore a **footgun** if a future worker reads it as “resend this 11.” It is legal **only** as “allocate a new intent / new 11.” The name does not say so. A42: replacement is a new `execution_intent_id`, never `sequence++` on the burned id.

Working `Accepted`/`PartiallyFilled` after a **session** drop also need recon (A25 / A47). They are **not** this helper’s job: `RequiresReconciliation` is false for them. A TRADE-startup gate must cover that; it does not exist yet.

### 5.5 What the machine does **not** do

| Missing | Effect |
|---|---|
| Legal-transition table | Illegal ER flips status (partial → accepted). Does not by itself emit `35=D`. |
| Qty monotonicity | Duplicate FAQ `35=8` can be applied as a new partial by a future booker that trusts `LastQty`. |
| Exec identity / fingerprint | Cannot classify `NEW_TRADE` vs `WIRE_DUPLICATE` vs `STATUS_SNAPSHOT`. |
| `not_on_venue` / `abandoned` / `expired` | Replacement-after-absent cannot be expressed. |
| Arm / `send_armed_at` | Crash between persist `NotSent` and socket write still looks retryable. |
| Session-reject path | `35=3` / unsure `35=j` not mapped. Mis-map to `Rejected` licenses retry. |
| Recovery API | No `35=H` / `35=AF` / `35=AN`. Unknown is a parking bit only. |
| ClOrdID match | Apply will advance an order from a report that names a different 11. |

---

## 6. `ClOrdIdFactory` — uniqueness sketch, not an idempotency lock

Full implementation (17 lines):

```csharp
public sealed class ClOrdIdFactory
{
    public string Next(string executionIntentId, DateTimeOffset now, int sequence)
    {
        if (string.IsNullOrWhiteSpace(executionIntentId))
            throw new ArgumentException("Execution intent id is required.", nameof(executionIntentId));
        if (sequence < 0)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        var compact = executionIntentId.Replace("-", "", StringComparison.Ordinal);
        if (compact.Length > 16)
            compact = compact[..16];
        return $"TI{now:yyyyMMddHHmmss}{sequence:D4}{compact}";
    }
}
```

Instance class, **no state**, not in DI. Could be a static pure function. There is no `From(id)` overload.

### 6.1 Grammar

```text
"TI" + yyyyMMddHHmmss + sequence:D4 + compact
compact = executionIntentId with '-' stripped, then first 16 chars (or all if shorter)
```

| Part | Width | Source |
|---|---|---|
| `TI` | 2 | literal prefix |
| timestamp | 14 | `DateTimeOffset` **calendar** components of `now` (not forced UTC) |
| sequence | 4 **minimum** | `sequence:D4` — values ≥ 10000 emit **5+** digits |
| compact | 1–16 | truncated identity |

Total length = `20 + len(compact)` = **21–36** for `sequence ∈ [0,9999]`. Not the A42 26-char Crockford.

Worked examples (computed from the formula; the unit test only locks the prefix of the first):

| Inputs | Output |
|---|---|
| `"intent-1"`, `2026-08-18T12:00:00Z`, `0` | `TI202608181200000000intent1` |
| `"intent-1"`, `2026-08-18T12:00:00Z`, `1` | `TI202608181200000001intent1` |
| `"0191f0a2-9c3e-7d11-8a44-0123456789ab"`, same `now`, `0` | `TI2026081812000000000191f0a29c3e7d11` |
| `Guid.Empty.ToString()`, same `now`, `0` | `TI2026081812000000000000000000000000` (accepted) |
| `"intent-1"`, `2026-08-18T12:00:00+05:30`, `0` | `TI202608181200000000intent1` (offset clock, not UTC) |
| `"intent-1"`, same instant, `10000` | `TI2026081812000010000intent1` (width break) |

### 6.2 Validation that exists / does not

| Input | Result |
|---|---|
| null / `""` / whitespace `executionIntentId` | `ArgumentException` |
| `sequence < 0` | `ArgumentOutOfRangeException` |
| `Guid.Empty` string | **accepted** |
| lowercase | **accepted** (`intent1`, hex guid) |
| SOH / `=` / `\|` / space inside the id | **accepted** if the caller passed them (only `-` is stripped) |
| “mint again for an existing intent” | **new string** if `now` or `sequence` differ |

### 6.3 Score vs A42 §4 (binding generator contract)

| A42 requirement | Measured |
|---|---|
| Deterministic `From(execution_intent_id)` | **FAIL** — `now` + `sequence` are inputs |
| Replay / crash-before-send yields the **same** 11 | **FAIL** unless the caller freezes both extras |
| No attempt / hostname / sequence in the id | **FAIL** — `sequence:D4` is an attempt suffix. A05 “ULID + attempt” is **overruled** by A42 §17 |
| Charset `[0-9A-Z]`, no lowercase | **FAIL** — test id is lowercase |
| Length 26 Crockford of the 16-byte UUID | **FAIL** — 21–36, `TI` + time + seq + 16-hex prefix |
| Refuse default UUID | **FAIL** — empty throws; `00000000-…` accepted |
| Shadow prefix `SHDW` disjoint from live | **MISSING** — `ShadowOrder` has **no** `ClOrdId` column |
| Do not use time as the uniqueness source | **FAIL** — second-granularity stamp |
| Unique in DB | **PARTIAL** — `execution_intents.cl_ord_id` UNIQUE exists (last line of defence) |

### 6.4 How the factory can still enable a blind retry

The factory cannot see status. These calls are all legal at this API:

```text
Next(intent, t0, 0)     →  TI…0000{prefix}     // first persist
// TCP dies after write
Next(intent, t0, 1)     →  TI…0001{prefix}     // NEW 11, same intent  — A42 ILLEGAL
Next(intent, t0+1s, 0)  →  TI…0000{prefix}     // NEW 11 if the second ticked — A42 ILLEGAL
```

`MayRetry==false` on unknown **does not bind the factory**. A worker that “retries with a fresh ClOrdID because the first one is in-flight” will compile. The UNIQUE index then either:

1. accepts the new 11 (double-send risk on the venue), or
2. rejects a collision and a retry loop increments `sequence` (same illegal path).

### 6.5 Truncation collision (UUID v7)

If `executionIntentId` is `Guid.ToString()` (32 hex after hyphen strip), only the **first 16 hex chars (64 bits)** enter the id.

UUID v7 layout in those 16 hex: 48-bit unix-ms + 4-bit version + **12 bits** of randomness **per millisecond**.

Two intents created in the same millisecond can share that prefix. Combined with the same `now` second and the same `sequence`, `Next` returns the **same tag 11**. The UNIQUE index fail-closes the insert (good) or a helper bumps `sequence` (illegal new 11).

Crockford-26 of the full 16 bytes does not have this hole.

`now:yyyyMMddHHmmss` uses the `DateTimeOffset`’s **displayed** clock, not `.UtcDateTime`. Two callers that pass the same instant with different offsets mint **different** ids; two callers that pass different instants that share a local second mint the **same** stamp. A42 forbids time as the uniqueness source for this reason.

---

## 7. Persistence around the FSM (not in the two types, required to judge them)

`ExecutionIntent` today:

```text
Id, CopyIntentId, RiskDecisionId, ClOrdId, BrokerId, SourceLogin,
SourceTradeId?, DestinationAccount, CanonicalSymbol, Side,
RequestedQuantity, Status, CreatedAt
```

`TraderDbContext`:

```text
ToTable("execution_intents")
HasKey(Id)
HasIndex(ClOrdId).IsUnique()
```

Present vs A42 T1/T3:

| Column / constraint | Disk |
|---|---|
| `cl_ord_id` UNIQUE | **yes** |
| `risk_decision_id` UNIQUE | **no** (plain Guid, no index) |
| live-copy partial unique on `copy_intent_id` | **no** |
| `send_armed_at` / `sent_at` / `unknown_since` | **no** |
| `request_kind` / `orig_cl_ord_id` | **no** |
| `fencing_token` / `fix_session_key` | **no** |
| `pre_send_book_fingerprint` | **no** |
| `cserver_order_id` / `destination_position_id` | **no** |
| `fix_orders` / `fix_execution_reports` | **no entities, no DbSets** |
| `source_event_id` | **no** (CopyIntent has `IdempotencyKey` instead) |

Without T3 (`status=sent_ack_unknown` + `send_armed_at` **committed before** `Session.Send`), a crash after a successful write still looks like `NotSent`. `MayRetry(NotSent)` is **true**. That is exactly the illegal resend A42 §5.3 names. **These two Domain types cannot close that hole.**

`CopyIntent.ExpiresAt` + `CopyIntentExpiry.IsExpired` exist. A42: expiry applies to **unsent** rows only. Nothing stops a future worker from treating an unknown row as expired-and-replaceable.

---

## 8. Tests that actually lock (not A89’s target list)

Only file: `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs`.

| Fact | Asserts | FSM / factory coverage |
|---|---|---|
| `Unknown_ack_cannot_retry_new_order` | `AfterSendAttempt()==SentAcknowledgementUnknown`; `MayRetry==false`; `RequiresReconciliation==true` | **Yes** — in-session wait |
| `Disconnect_after_send_is_unknown_state` | `AfterDisconnectWithUnknownAck()==ExecutionStateUnknown` | **Partial** — does **not** assert `MayRetry==false` or `RequiresReconciliation==true` |
| `Filled_report_is_terminal` | `Accepted` + `ExecType=FILL` + `OrdStatus=2` → `Filled` | none of retry; does **not** assert a later non-fill stays Filled |
| `ClOrdId_is_deterministic_and_unique_per_sequence` | seq 0 ≠ seq 1; prefix `TI20260818120000` | locks the **dangerous** sequence-suffix behavior; does **not** assert same-args stability (`a == Next(...,0)` again) |

**Not tested (A70 §14 / A89 #67 #69 #80–#82):**

```text
MayRetry(ExecutionStateUnknown) == false          -- THE §34 fact
MayRetry(Accepted|Partial|Filled|Cancelled) == false
MayRetry(NotSent) == true
MayRetry(Rejected) == true  AND "same 11 never"
RequiresReconciliation only on the two unknowns
Partial + 39=0 stays Partial                      -- would FAIL today
39=C → Cancelled                                  -- would FAIL today (maps Unknown)
150=F with blank 39                               -- would FAIL (maps Unknown)
150=I + 39=0 is Accepted, not unknown
Filled + later 39=1 stays Filled
Rejected/Cancelled absorb a later fill
empty intent / sequence<0 throw
Guid.Empty refused
same (intent, now, sequence) is stable            -- true in code, not asserted
From(id) independent of clock                     -- would FAIL
disconnect harness: outbound 35=D count stays 1   -- no send path, no harness fact
```

A89 marks `UnknownExecutionNoBlindRetryTests`, `ExecutionOrderRetryPolicyTests`, `ClOrdIdGenerationTests`, `ExecutionReportStateTransitionTests` as **EXISTS**. On disk they are **MISSING**. A89 is a target list. D17 does not inherit those PASSes.

The ClOrdID fact’s name (`…deterministic_and_unique_per_sequence`) **freezes the wrong contract**: uniqueness-by-sequence is what A42 forbids.

---

## 9. Call graph (who could still blind-retry?)

```text
RiskDecisionRecord.AllowFixSend
    → (no IApprovedExecutionIntentService)
    → ExecutionIntent row          -- type exists; no persist-before-send worker
    → ClOrdIdFactory.Next          -- unit tests only
    → ExecutionOrderStateMachine   -- unit tests only
    → FIX 35=D                     -- NO builder, NO QuickFIX, NO send
```

`FixSessionOwnership.ExecutionIntentsAllowed` is `ownership && reconciled`. **Nothing in the worker consults it.**

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false**. `apps\fix-worker\Worker.cs` (15 s heartbeat):

- stamps QUOTE `ReadyForMarketData` and TRADE `LoggedOn` with **no socket**
- logs a warning if the flag is on
- still does not send

`FixSimulationHarness` can emit New / Partial / Fill / Cancel / Reject / Expired / `150=I`+`39=0` / duplicate-by-identity. **No test wires those strings through `ExecutionOrderStateMachine.Apply`.**

**Current production behavior:** it is impossible to blindly retry NOS because it is impossible to send NOS. That is **fail-closed by absence** (A08, A49, A100, C19). It is **not** a demonstration that the FSM will hold once a send path exists.

The first send-path PR that does any of the following **breaks §34** even with today’s helper:

1. Persist `NotSent` → `Send` → persist `SentAcknowledgementUnknown` (crash window looks retryable).
2. `catch (IOException) { if (MayRetry) Send(same); }` — safe for unknown, **unsafe** if a `35=3` was mapped to `Rejected`.
3. `catch { Send(factory.Next(id, UtcNow, seq++)); }` — helper never consulted; new 11.
4. On TRADE logon, drain `execution_intents` where `Status != Filled`. `NotSent` + stale `Rejected` would both fire.
5. Assign `AfterDisconnectWithUnknownAck()` onto a `Filled`/`Cancelled` row and later treat unknown as “needs a replacement 11.”

---

## 10. Scorecard vs architecture and siblings

| Claim | Source | This pass |
|---|---|---|
| After send → `sent_unknown` | §33, A70 | **PASS** (`AfterSendAttempt`) |
| Disconnect → `EXECUTION_STATE_UNKNOWN` | §34 | **PASS** (`AfterDisconnectWithUnknownAck`) |
| No NOS retry from either unknown | §34, A42 §8.2, A70 §10 | **PASS** at the helper; **FAIL** as a system (no arm, no caller, factory can mint new 11) |
| Unique ClOrdID, never reuse | §33, §70.4 | **PARTIAL** — UNIQUE index; generator is time+seq, not `From(id)` |
| Persist-before-send / T3 arm | A42 §5.3 | **FAIL** — not in these types |
| Duplicate ER ≠ second fill | §70.5, A34 FAQ | **FAIL** — `Apply` has no identity |
| No backward status | A70 §5.2 | **FAIL** — `PartiallyFilled` + `39=0` → `Accepted` |
| `39=C` → cancelled | A70 / RoE | **FAIL** — maps unknown |
| Recovery H → AF → AN | §34, A47 | **FAIL** — missing |
| G09 go-live unknown recovery | A100 | remains **FAIL** |
| A89 G12 “lock disconnect → unknown, no retry” | A89 | **FAIL** — disconnect test does not lock `MayRetry` |
| Live send remains default false | §41 | **PASS** (flag + absence) |

Agreement with B16: same hashes, same helper closed form, same factory defects, same vacuous-safety call graph. D17 adds the full 8×6 `Apply` matrix, the `150=F` (bare) → unknown hole, the `DateTimeOffset` offset-clock stamp, and the `sequence≥10000` width break.

---

## 11. Honest residual risks (when a send path is added)

| ID | Risk | Sev | Mitigation that is **not** “edit these two files in this task” |
|---|---|---|---|
| R1 | Crash after write, row still `NotSent` → `MayRetry=true` | **P0** | A42 T3: commit `sent_unknown` + `send_armed_at` **before** `Send`. Never revert to `NotSent`. |
| R2 | Worker retries with `Next(..., sequence+1)` or a new clock | **P0** | Replace factory with `From(execution_intent_id)` only. Refuse a second 11 on a live row. |
| R3 | Session/business reject mapped to `Rejected` → `MayRetry=true` on a possibly live order | **P0** | Keep `35=3` / unsure `35=j` as `ExecutionStateUnknown`. Document `MayRetry(Rejected)` as “new row + new 11 only.” |
| R4 | UUID v7 16-hex prefix collision + seq 0 → duplicate 11 or forced seq bump | P1 | Stop truncating; Crockford-26 of the full UUID. |
| R5 | `39=C` parked as unknown freezes the live-copy slot | P2 | Map expired → `Cancelled` (A70). Conservative today; not a double-send. |
| R6 | Backward `Apply` + unused qty → double-book on FAQ copy `35=8` | P0 for §70.5, not for NOS count | A70 applier + `fix_execution_reports` unique fingerprint. |
| R7 | Tests do not lock `MayRetry(ExecutionStateUnknown)==false` | P0 test hole | Named theory over all 8 statuses before any live `35=D`. |
| R8 | Bare `150=F` (no 39) → unknown, not filled | P1 | Prefer 39; if only 150=`F` and `LastQty`/`CumQty` prove a trade, still recon — do not guess fill without 39. |
| R9 | `AfterDisconnect*` assigned onto a terminal row | P1 | Helper should take `current` and refuse to leave `Filled`/`Rejected`/`Cancelled`. |
| R10 | ClOrdID fact freezes sequence-suffix as “correct” | P1 | Replace the fact when the generator is rewritten; do not treat it as A42 PASS. |

---

## 12. What D17 is **not** claiming

- That EX5 / Quantum Queen work is in scope (it is not).
- That the FSM is A70-complete. It is a **mapper + two predicates**.
- That ClOrdIDs are production-ready. They are a uniqueness **sketch**.
- That G09 / §70.6 unknown-recovery is green.
- That adding states or rewriting `Apply` in this pass is required. **No product source was changed.**
- That “no send path” equals “no-blind-retry proven.” It equals “the bug cannot fire yet.”

---

## 13. Checklist (score only — later coding task)

```text
[x] AfterSendAttempt → SentAcknowledgementUnknown
[x] AfterDisconnectWithUnknownAck → ExecutionStateUnknown
[x] MayRetry false on SentAcknowledgementUnknown
[x] MayRetry false on ExecutionStateUnknown   (true in code; not asserted)
[x] RequiresReconciliation on both unknowns
[x] Filled absorb of a later non-fill          (true in code; weakly tested)
[x] Rejected/Cancelled absorb                  (true in code; untested)
[ ] MayRetry(Rejected) documented + implemented as new-11-only
[ ] ClOrdID = deterministic From(intent id), no clock, no sequence
[ ] Charset [0-9A-Z], length 26, refuse Guid.Empty
[ ] T3 arm committed before socket write
[ ] Crash after arm cannot look like NotSent
[ ] 35=3 / unsure 35=j cannot become Rejected
[ ] Apply refuses backward edges (partial ↛ accepted)
[ ] 39=C → Cancelled
[ ] Apply keyed by ClOrdId; qty + fingerprint used
[ ] Recovery H → AF → AN before any replacement 11
[ ] Unit fact: MayRetry false for every non-(NotSent|Rejected) status
[ ] Harness: DisconnectAfterSubmit outbound 35=D count == 1
[ ] REAL_COPY_EXECUTION_ENABLED remains default false
```

---

## 14. Sources

- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs`
- `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\RiskDecisionRecord.cs`
- `D:\Prop\src\Domain\Entities\ShadowOrder.cs`
- `D:\Prop\src\Domain\Enums\ReconciliationIssueType.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` (`execution_intents.cl_ord_id` UNIQUE)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§33–34
- `D:\Prop\reports\swarm\20260818\A42_clordid_idempotency.md`
- `D:\Prop\reports\swarm\20260818\A70_execution_fsm.md`
- `D:\Prop\reports\swarm\20260818\B16_fix_fsm_review.md`
- `D:\Prop\reports\swarm\20260818\A47_reconciliation_design.md`
- `D:\Prop\reports\swarm\20260818\A100_golive_gates.md` (G09 FAIL)

Product source under `D:\Prop\src` and `D:\Prop\apps` was **not** modified.

*End D17.*
