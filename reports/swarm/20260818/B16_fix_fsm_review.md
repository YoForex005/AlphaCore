# B16 — ExecutionOrderStateMachine + ClOrdIdFactory: no-blind-retry review

| Field | Value |
|---|---|
| Agent | B16 (senior engineer, FSM / ClOrdID review only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Question | Does the current Domain FSM + ClOrdID factory **forbid a blind `NewOrderSingle` retry** after a possible send (architecture §33–§34)? |
| Authority | Architecture v2 §§32–34, 41–42, 62–63, 70.4–70.6, 72.8–10; `A42_clordid_idempotency.md`; `A70_execution_fsm.md`; `A25` §5; `A100` G09 |
| Method | `read_file` of the two SUTs + enum + `ExecutionIntent` + unit tests + FIX worker + ownership + harness; `grep` of `MayRetryNewOrderSingle` / `ClOrdIdFactory` / send paths under `D:\Prop` |

---

## 0. Verdict

**Domain helper: no-blind-retry on the two unknown states is implemented and is the correct closed form.**  
**End-to-end: not proven. Do not call this §34 / G09 PASS.**

| Layer | Blind `35=D` after possible send? | Score |
|---|---|---|
| `MayRetryNewOrderSingle(SentAcknowledgementUnknown)` | **false** | PASS |
| `MayRetryNewOrderSingle(ExecutionStateUnknown)` | **false** | PASS (untested as a named fact) |
| `AfterDisconnectWithUnknownAck()` | parks `ExecutionStateUnknown` | PASS |
| `RequiresReconciliation` on both unknowns | **true** | PASS |
| Same `cl_ord_id` cannot be reminted by the factory after persist | **not enforced** — `Next` takes `now` + `sequence` | FAIL vs A42 |
| Persist-before-send / arm-before-write | **absent** | FAIL (A42 T3) |
| Recovery (`35=H` / `35=AF` / `35=AN`) | **absent** | FAIL (A47 / G09) |
| Production caller of `MayRetry` | **none** — no `35=D` send path | VACUOUS SAFE |
| Live `NewOrderSingle` | still **OFF by absence** + `RealCopyExecutionEnabled` default false | control, not a proof |

**One-line law check (architecture §34):** *“Do NOT blindly send the order again.”*  
The static machine **will not authorize** a second NOS from `sent_unknown` or `execution_state_unknown`. That is necessary and **not sufficient**. A worker that ignores the helper, or that mints a **new** `ClOrdID` via `sequence++` / a new clock tick for the same risk row, can still double-send. Neither the factory nor any Application/FIX type stops that today.

Do **not** treat A89’s `UnknownExecutionNoBlindRetryTests` / `ExecutionOrderRetryPolicyTests` as green — those class names **do not exist**. The only lock is three facts inside `ExecutionAndSizingTests`.

---

## 1. Files measured (this pass)

| Path | Lines | SHA-256 | Role |
|---|---:|---|---|
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | 56 | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` | SUT — status mapper + retry/recon predicates |
| `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs` | 17 | `CCC5506F2661C9D23EEA83070C1BFE5585158508718ECF1781C7C298E013D0A1` | SUT — tag-11 string mint |
| `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` | 13 | `801D89D0A5D0E73F76EC195776C5A4D2BA3A09630F13A148C1B9C0AF27D9E7AF` | 8-value enum (7 business + unknown overlay) |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | 20 | `4E5EEDFAAED61B56573C5F1AC7D49F9C8424F27D5A4CEE023F756FA71F22D6B4` | persist row (thin) |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | — | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | only consumer of both SUTs |

`grep` of `MayRetryNewOrderSingle`, `AfterSendAttempt`, `AfterDisconnectWithUnknownAck`, `ClOrdIdFactory` over `*.cs` under `D:\Prop`:

| Location | Hits |
|---|---|
| `ExecutionOrderStateMachine.cs` | definitions |
| `ClOrdIdFactory.cs` | definition |
| `tests\Unit\ExecutionAndSizingTests.cs` | the only calls |
| `Infrastructure\Persistence\TraderDbContext.cs` | `HasIndex(x => x.ClOrdId).IsUnique()` on `execution_intents` |

**No Application service, no FIX worker, no connector calls either type.** `apps\fix-worker\Worker.cs` is a 15 s heartbeat that stamps `FixSessionState` and **refuses** NOS in a log line only.

---

## 2. Architecture pins this review scores against

Quoted §33:

> Never simply retry a NewOrderSingle because the TCP connection broke. First reconcile.

Quoted §34:

> Do NOT blindly send the order again. Set `EXECUTION_STATE_UNKNOWN`. Then use OrderStatusRequest / OrderMassStatusRequest / ExecutionReports / Position reconciliation. Only after reconciliation may the system decide whether another order is required.

A42 / A70 interpretation used here (binding for this review):

```text
catch (IOException) { SendNewOrderSingle(sameClOrdId); }   // ILLEGAL
catch (IOException) { SendNewOrderSingle(newClOrdId); }    // ILLEGAL
```

| Status | Retry `35=D` on **this** tag 11 | New tag 11 for same family |
|---|---|---|
| `NotSent` | yes — the **first** send only | no — already has 11 |
| `SentAcknowledgementUnknown` | **never** | only after recon `not_on_venue` |
| `Accepted` / `PartiallyFilled` | never | never while working |
| `Filled` | never | never |
| `Cancelled` | never | new risk decision only |
| `Rejected` | **never** (11 is burned) | **yes**, new row + new 11, if risk re-approves |
| `ExecutionStateUnknown` | **never** | only after recon `not_on_venue` |

`MayRetryNewOrderSingle` is allowed to stay `true` for `Rejected` **only if** the caller understands that as “allocate a new intent / new 11,” not “resend this 11.”

---

## 3. ExecutionOrderStateMachine — what the code actually does

Full surface (`D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`):

```csharp
AfterSendAttempt()                → SentAcknowledgementUnknown
AfterDisconnectWithUnknownAck()   → ExecutionStateUnknown
Apply(current, report)            → mapped OrdStatus/ExecType, with three absorb rules
MayRetryNewOrderSingle(status)    → status is NotSent or Rejected
RequiresReconciliation(status)    → SentAcknowledgementUnknown or ExecutionStateUnknown
```

### 3.1 Retry / recon matrix (evaluated against the enum, not against tests)

| `ExecutionOrderStatus` | `MayRetryNewOrderSingle` | `RequiresReconciliation` | Blind-retry risk |
|---|---|---|---|
| `NotSent` (0) | **true** | false | None — first send. Must still be gated by persist + flags. |
| `SentAcknowledgementUnknown` (1) | **false** | **true** | **Blocked** at this helper |
| `Accepted` (2) | false | false | Blocked. Session-drop recon is **not** this helper (A25 / A47). |
| `PartiallyFilled` (3) | false | false | Blocked |
| `Filled` (4) | false | false | Blocked |
| `Rejected` (5) | **true** | false | **Footgun** if caller resends the **same** 11. Legal only as a **new** 11. Helper does not say so. |
| `Cancelled` (6) | false | false | Blocked |
| `ExecutionStateUnknown` (7) | **false** | **true** | **Blocked** at this helper |

This is the A70 §10 closed form. **No unknown state can retry NOS through this predicate.** That is the entire “no blind retry” guarantee that exists in Domain.

### 3.2 `Apply` — status only, not a send gate

```csharp
var status = MapOrdStatus(report.OrdStatus, report.ExecType);
if (current == Filled && status != Filled) return current;
if (current == Rejected || current == Cancelled) return current;
return status;
```

`MapOrdStatus` prefers tag 39, else tag 150; `ToUpperInvariant()`:

| key | mapped |
|---|---|
| `0` / `NEW` | `Accepted` |
| `1` / `PARTIAL` / `PARTIALLY FILLED` | `PartiallyFilled` |
| `2` / `FILL` / `FILLED` | `Filled` |
| `4` / `CANCELED` / `CANCELLED` | `Cancelled` |
| `8` / `REJECTED` / `REJECT` | `Rejected` |
| `A` / `PENDING_NEW` | `Accepted` |
| anything else (including `C` / `EXPIRED` / `I` when 39 is `I`) | `ExecutionStateUnknown` |

Findings that are **not** blind-retry but **will** corrupt a later retry decision:

1. **Backward edges are allowed.** `PartiallyFilled` + `39=0` becomes `Accepted`. A70 §5.2 forbids this. Status can walk backwards; `MayRetry` stays false, so this is a bookkeeping bug, not a double-send by itself.
2. **`39=C` / `EXPIRED` maps to `ExecutionStateUnknown`.** A70 / RoE fold expired IOC leftover to `Cancelled` (terminal, `MayRetry=false`). Today an expired market leftover **looks like unknown** → `RequiresReconciliation=true` and **blocks** a replacement until recon. Conservative (good vs double-send), noisy (false unknown).
3. **`150=I` is ignored as a classifier.** If tag 39 is present, status follows 39. If a harness/status snapshot omits 39 and sends `150=I`, map key is `I` → `ExecutionStateUnknown`. `FixSimulationHarness.SimulateExecutionReport_UnknownState` does this (`150=I`, `39=0`) — that pair maps to `Accepted`, not unknown. The harness comment is wrong relative to `Apply`.
4. **No quantity, no ExecID, no fingerprint.** `ExecutionReportInput` has `LastQty` / `CumQty` / `LeavesQty` and **never reads them**. Duplicate `35=8` (FAQ two TRADE sockets) cannot be distinguished from a new partial. That is A70 §8 / §70.5 — a **double-book** risk, not a second `35=D`. Still a live-exec gate.
5. **Terminal absorb is correct for status.** `Filled` ignores a later non-fill; `Rejected`/`Cancelled` ignore everything. A late real fill after `Rejected` would be dropped (UnexpectedFill in A70) — prefer that over re-opening the id for send.

### 3.3 What the machine does **not** do

| Missing (A70 / A42) | Effect on blind retry |
|---|---|
| Legal-transition table | Illegal ER can still flip status (e.g. partial → accepted). Does not by itself emit `35=D`. |
| `not_on_venue` / `abandoned` / `expired` statuses | Replacement-after-absent cannot be expressed. A future worker that treats `MayRetry(Rejected)` as “resend” has no `not_on_venue` gate. |
| Arm / `send_armed_at` | Crash between persist `NotSent` and socket write still **looks like** `NotSent` → `MayRetry=true`. **This is the classic §34 bug** if a send path is added without T3-before-T4. |
| Session-reject (`35=3` / `35=j`) path | Not mapped. A70: session reject after write is **unknown**, not `Rejected`. If a future parser stuffs `35=3` into `Apply` as `REJECTED`, `MayRetry` becomes **true** — that would be a **blind retry license**. |
| Recovery API | No `35=H` / `AF` / `AN`. Unknown is a parking bit only. |

---

## 4. ClOrdIdFactory — uniqueness family, not an idempotency lock

Full implementation:

```csharp
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
```

### 4.1 Measured shape

```text
TI + yyyyMMddHHmmss + sequence:D4 + first 16 chars of (id with '-' stripped)
```

Example from the unit test (`intent-1`, `2026-08-18T12:00:00Z`, seq 0):

```text
TI202608181200000000intent1
```

| Property | Measured | A42 / RoE contract |
|---|---|---|
| Deterministic `From(execution_intent_id)` | **No** — clock + sequence are inputs | Required. Replay / crash-before-send must yield the **same** 11 |
| Attempt / sequence suffix | **Yes** (`sequence:D4`) | **Forbidden.** Replacement = new intent id, not `seq++` |
| Time in the id | **Yes** (second granularity) | Forbidden as the uniqueness source (seq reset / restart) |
| Charset `[0-9A-Z]` | **No** — test id is lowercase `intent1`; a `Guid.ToString()` is `0-9a-f` | Required. No SOH / `=` / `\|` / space / lowercase |
| Length 26 | **No** — `2+14+4+len(compact)` = 21–36 | 26 Crockford of the 16-byte UUID |
| Truncation | first **16** of compact | full 128-bit identity |
| Shadow prefix `SHDW` | none | live vs shadow must not collide |
| Refuse default / empty UUID | empty/whitespace throws; `Guid.Empty` string `"00000000-…"` is **accepted** | refuse default UUID |
| Unique in DB | `execution_intents.cl_ord_id` UNIQUE exists | last line of defence, not the generator |

### 4.2 Why the factory can still enable a blind retry

The factory is a **pure string builder**. It cannot see status. Two calls that should be illegal after a possible send are **legal at this API**:

```text
Next(intent, t0, 0)  →  TI…0000{prefix}     // first persist
// TCP dies after write
Next(intent, t0, 1)  →  TI…0001{prefix}     // NEW 11, same intent  — A42 ILLEGAL
Next(intent, t0+1s, 0) → TI…0000{prefix}    // NEW 11 if the second ticked — A42 ILLEGAL
```

A42: *“Do not put an attempt, hostname, or sequence into `cl_ord_id`.”*  
A05’s older wording (“ULID / execIntentId + attempt”) is **overruled** by A42 §17. This factory implements the **overruled** shape.

`MayRetry` being false on unknown does **not** bind the factory. A worker that “retries with a fresh ClOrdID because the first one is in-flight” will compile and will look unique to the DB index.

### 4.3 Truncation collision (UUID v7)

If `executionIntentId` is `Guid.ToString()` (32 hex after hyphen strip), only the first **16 hex chars** (64 bits) enter the id.

UUID v7: first 48 bits = unix ms, next 4 = version, next 12 = rand.  
First 16 hex = timestamp + version + **12 bits** of uniqueness **per millisecond**.

Two intents created in the same ms can share that prefix. Combined with the same `now` second and the same `sequence`, `Next` returns the **same tag 11**. The UNIQUE index would then reject the second insert (fail-closed — good) or a retry loop would mint `sequence+1` (new 11 — the illegal path).

Crockford-26 of the full 16 bytes does not have this hole.

### 4.4 Persist-before-send is not in these types

`ExecutionIntent` today:

```text
Id, CopyIntentId, RiskDecisionId, ClOrdId, BrokerId, SourceLogin,
SourceTradeId?, DestinationAccount, CanonicalSymbol, Side,
RequestedQuantity, Status, CreatedAt
```

Missing vs A42 T1/T3 (the protocol that makes “no blind retry” crash-safe):

```text
source_event_id, request_kind, orig_cl_ord_id,
send_armed_at, sent_at, unknown_since, unknown_reason,
fencing_token, fix_session_key, pre_send_book_fingerprint,
cserver_order_id, destination_position_id, superseded_by_intent_id
```

Without `send_armed_at` committed **before** `Session.Send`, a crash after a successful write still looks like `NotSent`. `MayRetry(NotSent)` is **true**. That is exactly the illegal resend A42 §5.3 names.

`TraderDbContext` unique index on `ClOrdId` is present. There is **no** unique `(risk_decision_id)`, **no** live-copy partial unique, **no** `fix_orders` / `fix_execution_reports` tables in the model.

---

## 5. Tests — what is actually locked

`D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` (only file):

| Fact | Asserts | Blind-retry coverage |
|---|---|---|
| `Unknown_ack_cannot_retry_new_order` | `AfterSendAttempt()==SentAcknowledgementUnknown`; `MayRetry==false`; `RequiresReconciliation==true` | **Yes** — in-session wait |
| `Disconnect_after_send_is_unknown_state` | `AfterDisconnectWithUnknownAck()==ExecutionStateUnknown` | **Partial** — does **not** assert `MayRetry==false` or `RequiresReconciliation==true` |
| `Filled_report_is_terminal` | `Accepted` + `FILL`/`2` → `Filled` | none |
| `ClOrdId_is_deterministic_and_unique_per_sequence` | same args stable; seq 0 ≠ seq 1; prefix `TI20260818120000` | locks the **dangerous** sequence-suffix behavior |

**Not tested (A70 §14 / A89 #69 #82):**

```text
MayRetry(ExecutionStateUnknown) == false          -- THE §34 fact
MayRetry(Accepted|Partial|Filled|Cancelled) == false
MayRetry(NotSent) == true
MayRetry(Rejected) == true  AND "same 11 never"
RequiresReconciliation only on the two unknowns
Partial + 39=0 stays Partial
39=C → cancelled (today fails)
150=I does not book LastQty
disconnect harness: outbound 35=D count stays 1
```

A89 marks `UnknownExecutionNoBlindRetryTests`, `ExecutionOrderRetryPolicyTests`, `ClOrdIdGenerationTests`, `ExecutionReportStateTransitionTests` as **EXISTS**. On disk they are **MISSING**. A89 is a target list, not a census. B16 does not inherit those PASSes.

---

## 6. Call-graph: who could still blind-retry?

```text
Risk / CopyIntent
    → (no IApprovedExecutionIntentService)
    → ExecutionIntent row   -- type exists; no persist-before-send worker
    → ClOrdIdFactory.Next   -- not called outside unit tests
    → ExecutionOrderStateMachine.MayRetry  -- not called outside unit tests
    → FIX 35=D              -- NO builder, NO QuickFIX send
```

`FixSessionOwnership.ExecutionIntentsAllowed` is `ownership && reconciled`, but **nothing in the worker consults it** before a send (there is no send).

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false**. Worker logs a warning if config flips it on and still does not send.

**Current production behavior:** it is impossible to blindly retry NOS because it is impossible to send NOS. That is **fail-closed by absence** (`A08`, `A49`, `A100`). It is **not** a demonstration that the FSM will hold once a send path exists.

The first send-path PR that does any of the following **breaks §34** even with today’s helper:

1. Persist `NotSent` → `Send` → persist `SentAcknowledgementUnknown` (crash window looks retryable).
2. `catch (IOException) { if (MayRetry) Send(same); }` — safe for unknown, **unsafe** if a `35=3` was mapped to `Rejected`.
3. `catch { Send(factory.Next(id, UtcNow, seq++)); }` — helper never consulted; new 11.
4. On TRADE logon, drain `execution_intents` where `Status != Filled` (A25: illegal). `NotSent` + stale `Rejected` would both fire.

---

## 7. Scorecard vs sibling specs

| Claim | Source | This pass |
|---|---|---|
| After send → `sent_unknown` | §33, A70 | **PASS** (`AfterSendAttempt`) |
| Disconnect → `EXECUTION_STATE_UNKNOWN` | §34 | **PASS** (`AfterDisconnectWithUnknownAck`) |
| No NOS retry from either unknown | §34, A42 §8.2, A70 §10 | **PASS** at helper; **FAIL** as a system (no arm, no caller, factory can mint new 11) |
| Unique ClOrdID, never reuse | §33, §70.4 | **PARTIAL** — UNIQUE index; generator is time+seq, not `From(id)` |
| Persist-before-send / T3 arm | A42 §5.3 | **FAIL** — not in these types |
| Duplicate ER ≠ second fill | §70.5, A34 FAQ | **FAIL** — `Apply` has no identity |
| Recovery H → AF → AN | §34, A47 | **FAIL** — missing |
| G09 go-live | A100 | remains **FAIL** |
| A89 G12 “lock disconnect → unknown, no retry” | A89 | **FAIL** — disconnect test does not lock `MayRetry` |

---

## 8. Honest residual risks (when a send path is added)

| ID | Risk | Severity | Mitigation that is **not** “edit these two files in this task” |
|---|---|---|---|
| R1 | Crash after write, row still `NotSent` → `MayRetry=true` | **P0** | A42 T3: commit `sent_unknown` + `send_armed_at` **before** `Send`. Never revert to `NotSent`. |
| R2 | Worker retries with `Next(..., sequence+1)` or a new clock | **P0** | Replace factory with `From(execution_intent_id)` only. Refuse a second 11 on a live row. |
| R3 | Session/business reject mapped to `Rejected` → `MayRetry=true` on a **possibly live** order | **P0** | Keep `35=3` / unsure `35=j` as `ExecutionStateUnknown`. Document `MayRetry(Rejected)` as “new row + new 11 only.” |
| R4 | UUID v7 prefix collision + seq 0 → duplicate 11 or forced seq bump | P1 | Stop truncating; Crockford-26 of full UUID. |
| R5 | `39=C` parked as unknown freezes the live-copy slot (A42 partial unique) | P2 | Map expired → `Cancelled` (A70). Conservative today; not a double-send. |
| R6 | Backward `Apply` + duplicate ER double-books qty | P0 for §70.5, not for NOS count | A70 applier + `fix_execution_reports` unique fingerprint. |
| R7 | Tests do not lock `MayRetry(ExecutionStateUnknown)==false` | P0 test hole | Named theory over all 8 statuses before any live 35=D. |

---

## 9. What B16 is **not** claiming

- That EX5 / Quantum Queen work is in scope (it is not).
- That the FSM is A70-complete. It is a **mapper + two predicates**.
- That ClOrdIDs are production-ready. They are a uniqueness **sketch**.
- That G09 / §70.6 unknown-recovery is green.
- That adding more states in this review pass is required. **No product source was changed.**

---

## 10. Checklist (score only — later coding task)

```text
[x] AfterSendAttempt → SentAcknowledgementUnknown
[x] AfterDisconnectWithUnknownAck → ExecutionStateUnknown
[x] MayRetry false on SentAcknowledgementUnknown
[x] MayRetry false on ExecutionStateUnknown   (true in code; not asserted)
[x] RequiresReconciliation on both unknowns
[ ] MayRetry(Rejected) documented + implemented as new-11-only
[ ] ClOrdID = deterministic From(intent id), no clock, no sequence
[ ] T3 arm committed before socket write
[ ] Crash after arm cannot look like NotSent
[ ] 35=3 / unsure 35=j cannot become Rejected
[ ] Recovery H → AF → AN before any replacement 11
[ ] Unit fact: MayRetry false for every non-(NotSent|Rejected) status
[ ] Harness: DisconnectAfterSubmit outbound 35=D count == 1
[ ] REAL_COPY_EXECUTION_ENABLED remains default false
```

---

## 11. Sources

- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs`
- `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`
- `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` (`execution_intents.cl_ord_id` UNIQUE)
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§33–34
- `D:\Prop\reports\swarm\20260818\A42_clordid_idempotency.md`
- `D:\Prop\reports\swarm\20260818\A70_execution_fsm.md`
- `D:\Prop\reports\swarm\20260818\A47_reconciliation_design.md`
- `D:\Prop\reports\swarm\20260818\A100_golive_gates.md` (G09 FAIL)

Product source under `D:\Prop\src` and `D:\Prop\apps` was **not** modified.

*End B16.*
