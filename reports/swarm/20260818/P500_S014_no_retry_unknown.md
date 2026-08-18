# P500_S014 — Persist ClOrdID; never retry `EXECUTION_STATE_UNKNOWN` as `35=D`

| Field | Value |
|---|---|
| Slot | **S014** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S014_no_retry_unknown.md` |
| Date | 2026-08-18 |
| Agent | P500_S014 (Domain Execution close-read; lower-loss / no double-fill) |
| Workspace | `D:\Prop` |
| Assigned | Read `src/Domain/Execution` (`ClOrdIdFactory`, `ExecutionOrderStateMachine`) and architecture persist-before-send. Write this file. **Do not edit product.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Thesis | Lower loss = persist ClOrdID **before** any TRADE write; **never** retry `EXECUTION_STATE_UNKNOWN` as `35=D`. This is **not** an edge. It is the double-fill gate — the fastest way to lose. |
| Law | Architecture v2 **§32–§34**, **§41–§42**, **§62–§63**, **§70.4–§70.6**, **§72.8–§72.10**; A42 T3 / §8.2; A70 §10; A100 G09 |
| Classification | architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` |
| Method | Full-read of the two SUTs + sibling Domain files + `ExecutionIntent` + enum + unit facts + `TraderDbContext` map + FIX worker. `grep` of product `*.cs` for `MayRetryNewOrderSingle`, `ClOrdIdFactory`, `new ExecutionIntent`, `35=D`. Architecture §33–§34 and §72.8–10 quoted from disk. Prior B16 / D17 / D98 used as siblings, **not** as this verdict. |

This slot answers one loss question. It is **not** an A70-complete FSM review, **not** a G09 PASS, and **not** a license to add a send path.

## Profit implication

Missing a copy after a crash is a missed trade. Retrying `EXECUTION_STATE_UNKNOWN` as `35=D` is **two live fills for one source event** — the fastest way to lose on XAUUSD. Architecture prefers the miss (§72.9). This is **not** an edge.

**Remeasured 2026-08-18:** `MayRetryNewOrderSingle` still allows only `NotSent` | `Rejected` (`ExecutionOrderStateMachine.cs` L35–36). `RequiresReconciliation` still true on both unknowns. `grep new ExecutionIntent` / `ExecutionIntents.Add` in `src/` = **0** (only `CountAsync(e => e.SentAt != null)` in `CopyTradingService`). `ClOrdIdFactory` still test-only. Persist-before-send is still **MISSING**. Copy pipeline does not mint tag 11.

---

## 0. Verdict

**The Domain helper already refuses a second `NewOrderSingle` from either unknown status. Persist-before-send is not implemented. That gap is the double-fill hole.**

| Layer | Score | One line |
|---|---|---|
| Thesis (law) | **BINDING** | Persist unique tag 11 **before** `Send`. `EXECUTION_STATE_UNKNOWN` **never** emits `35=D` (same 11 or a new one) until recon proves venue outcome. |
| `MayRetryNewOrderSingle(ExecutionStateUnknown)` | **EXISTS_AND_GOOD** (helper) | Closed form: only `NotSent` \| `Rejected`. Unknown is **false**. |
| `MayRetryNewOrderSingle(SentAcknowledgementUnknown)` | **EXISTS_AND_GOOD** (helper) | After-send park. Unit-locked. |
| `RequiresReconciliation` on both unknowns | **EXISTS_AND_GOOD** (helper) | true / true. |
| `ClOrdIdFactory` | **EXISTS_NEEDS_REFACTOR** | Clock + `sequence:D4` + truncated intent. **Not** A42 `From(execution_intent_id)`. Can mint a **second** tag 11 without asking the FSM. |
| Persist-before-send / T3 arm | **MISSING** | No `send_armed_at`. Crash after a future write can still look like first-send (`NotSent` / `"Pending"`). |
| Production caller | **MISSING** | Factory + FSM used only by unit tests. Zero `new ExecutionIntent`. |
| Live `35=D` | **OFF by absence** | No builder. Worker logs refusal. Vacuous safety, not a proof the rule will hold. |
| Double-fill as a system | **UNPROVEN** | Helper is necessary. Without T3 + unique-from-id + recon, a future send PR can still double-fill. |

**One-line:** Missing a copy after a crash is a missed trade. Retrying `EXECUTION_STATE_UNKNOWN` as `35=D` is **two live fills for one source event** — the fastest way to lose on XAUUSD. Architecture prefers the miss (§72.9).

Do **not** call this A100 G09 PASS. Do **not** treat “no send path today” as “no-retry proven.”

---

## 1. Why this is not an edge

A reconnect after `Send` looks like an operations glitch. It is the **main** live-copy failure mode:

```text
T4  Session.Send(35=D  11=<cl_ord_id>  40=1 IOC)
        TCP dies / process crash / lease lost
        no 35=8
        did cServer take it?
```

cTrader market `OrdType=1` is **IOC**. A fill often **vanishes** from the working book. Official Logon is `141=Y` — FIX resend **will not** replay the original `35=D`. The only memory of “we may have already ordered” is **our row**.

| Naive recovery | What the venue can do | P&L |
|---|---|---|
| `catch { Send(same 11); }` | cServer may reject **or** treat as a new order (RoE: unique ClOrdID required; reuse is undefined / reject / **second order**) | 0×, 1×, or 2× size |
| `catch { mint new 11; Send; }` | If the first write landed, this is a **second** market order | **2× size, same side** |
| Drain `Status != Filled` on next TRADE logon | `"Pending"` / `NotSent` after a successful write is a **ghost first-send** | **2×** |

On gold, two IOC market tickets on the same signal is not “retry overhead.” It is **double inventory** at two prints. That is how a copy account dies in one reconnect.

Architecture §34 (disk, not paraphrase):

> Do NOT blindly send the order again.
> Set `EXECUTION_STATE_UNKNOWN`.
> Then use OrderStatusRequest / OrderMassStatusRequest / ExecutionReports / Position reconciliation.
> Only after reconciliation may the system decide whether another order is required.

Architecture §33:

> Persist before sending [cl_ord_id + intent fields].
> Never simply retry a NewOrderSingle because the TCP connection broke. First reconcile.

Architecture §72.8–10:

> Every execution request must be idempotent.
> Never blindly retry a possibly-sent order.
> Reconcile after FIX reconnect.

A42 §8.2 illegal forms (binding for this slot):

```text
catch (IOException) { SendNewOrderSingle(sameClOrdId); }   // ILLEGAL
catch (IOException) { SendNewOrderSingle(newClOrdId); }    // ILLEGAL
on reconnect: drain not_sent AND sent_ack_unknown as a send queue  // ILLEGAL
on CopyIntent expiry: resend the unknown order                     // ILLEGAL
```

**Lower loss** here is not “tighter stops.” It is **never paying for the same source event twice**.

---

## 2. Files measured (this pass)

| Path | Lines | Role |
|---|---:|---|
| `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs` | 17 | Tag-11 mint |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | 56 | Status mapper + retry/recon predicates |
| `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` | 6 | Signal-age helper (not a send gate) |
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | dest-grid only | Not a ClOrdID / retry type |
| `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` | 13 | Eight-value vocabulary |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | 21 | Persist row; **string** `Status = "Pending"` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` L136–141 | `execution_intents`; `HasIndex(ClOrdId).IsUnique()` | Last-line uniqueness, not T3 |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | 62 | Only product consumer of factory + FSM |
| `D:\Prop\apps\fix-worker\Worker.cs` | 51 | 15 s heartbeat; log refusal only |

Architecture quotes: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§33–34 (L1302–1372), §70.4–6 (L2666–2668), §72.8–10 (L2710–2712).

Siblings (same SUTs, independent of this verdict): A42, A70, B16, D17, D98, A100 G09.

FSM + factory bytes match the B16 / D17 / D98 close-reads (same public surface). **`ExecutionIntent` does not match D17** — current row is the string-status shape recorded by D98 (`Status`, `SentAt`, `FilledAt`, `VolumeLots`; no `ExecutionOrderStatus` column, no `send_armed_at`).

---

## 3. Persist-before-send (the only legal write order)

A42 T1 → T3 → T4, restated against **loss**:

```text
[T1] COMMIT execution_intents
        cl_ord_id     = From(execution_intent_id)     -- unique, stable
        status        = not_sent
        send_armed_at = NULL
     Crash here is safe: never on the wire.

[T2] Re-check gates (flag, lease, READY_FOR_EXECUTION,
     no unresolved EXECUTION_STATE_UNKNOWN, expiry, kill).

[T3] COMMIT
        status        = sent_ack_unknown
        send_armed_at = now()
     WHERE status = not_sent AND send_armed_at IS NULL
     0 rows → STOP. Do not write.
     From this COMMIT the row is a **possibly-sent** order
     even if the process dies before the next line.

[T4] Session.Send(35=D)
     local throw / TCP die / no ER:
        do NOT revert to not_sent
        session dead → EXECUTION_STATE_UNKNOWN
        MayRetry == false
        new 11 == illegal
```

**Illegal (the double-fill recipes):**

| Recipe | Why it doubles |
|---|---|
| Generate 11 in memory → `Send` → persist | Crash between Send and persist: restart mints **another** 11 |
| Persist `not_sent` → `Send` → persist `sent_unknown` | Crash between Send and second persist: row still looks first-send; `MayRetry(NotSent)==true` |
| Revert `sent_unknown` → `not_sent` on `IOException` | Same as above, **on purpose** |
| `sequence++` / new clock on unknown | New tag 11 while venue may already hold the first |

Accepted cost of T3-before-T4: a crash **after arm, before bytes leave** produces a **false** unknown. Recovery may later mark `not_on_venue` and only then allocate a **new** intent + new 11. Architecture prefers a **missed copy** over a **double order**. That is the lower-loss trade.

**Measured today:** T1 writer = **0**. T3 columns (`send_armed_at`, `unknown_since`, `pre_send_book_fingerprint`) = **absent**. `SentAt` exists on the entity and **nobody sets it**. Unique `ClOrdId` index exists and is unused (no inserts).

---

## 4. `ExecutionOrderStateMachine` — what the helper actually does

Static. No I/O. Full public API:

```17:40:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static ExecutionOrderStatus AfterSendAttempt() =>
        ExecutionOrderStatus.SentAcknowledgementUnknown;

    public static ExecutionOrderStatus AfterDisconnectWithUnknownAck() =>
        ExecutionOrderStatus.ExecutionStateUnknown;

    public static ExecutionOrderStatus Apply(ExecutionOrderStatus current, ExecutionReportInput report)
    { /* map + Filled / Rejected / Cancelled absorb */ }

    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;

    public static bool RequiresReconciliation(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.SentAcknowledgementUnknown
            or ExecutionOrderStatus.ExecutionStateUnknown;
```

`AfterDisconnectWithUnknownAck` **is** the §34 park. There is no third `MayRetry` clause. There is no “unless TCP might have dropped.”

### 4.1 Eight-status matrix (closed form, re-read)

| # | Status | `MayRetryNewOrderSingle` | `RequiresReconciliation` | Legal `35=D`? |
|---:|---|---|---|---|
| 0 | `NotSent` | **true** | false | **First send only**, after T3 arm |
| 1 | `SentAcknowledgementUnknown` | **false** | **true** | **Never** |
| 2 | `Accepted` | false | false | Never (cancel/replace = new intent) |
| 3 | `PartiallyFilled` | false | false | Never |
| 4 | `Filled` | false | false | **Never** (exposure exists) |
| 5 | `Rejected` | **true** | false | **New row + new 11 only.** Same 11 is burned |
| 6 | `Cancelled` | false | false | Never on this 11 |
| 7 | `ExecutionStateUnknown` | **false** | **true** | **Never** until recon → `not_on_venue` + new risk + new intent |

This is A70 §10. **`EXECUTION_STATE_UNKNOWN` cannot authorize `35=D`.** That is the assigned law, implemented as a boolean.

### 4.2 What the helper does **not** close (loss-relevant)

| Hole | Double-fill path if a send PR ignores it |
|---|---|
| No T3 | Crash after write leaves `NotSent` / `"Pending"` → `MayRetry=true` |
| `MayRetry(Rejected)==true` | Session reject (`35=3`) stuffed as `REJECTED` licenses a retry of a **possibly live** order |
| Factory not bound | `Next(id, UtcNow, seq++)` mints a new 11; helper never consulted |
| `AfterDisconnect*` takes no `current` | Overwrites `Filled` → unknown → later “replacement” 11 |
| `Apply` unused qty / ExecID | Duplicate FAQ `35=8` can be booked twice by a future fill handler (second **book**, not second NOS — still a P&L lie) |
| `39=C` → unknown | Conservative vs send (recon required). Not a double-send. Noisy vs book. |
| `ExecutionIntent.Status` is `string` `"Pending"` | Helper takes the enum. A worker that drains `Status != "Filled"` never sees this predicate |
| Recovery `35=H` / `35=AF` / `35=AN` | **MISSING.** Unknown is a parking bit with no legal exit to `not_on_venue` |

---

## 5. `ClOrdIdFactory` — uniqueness sketch, not an idempotency lock

```3:16:D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs
public sealed class ClOrdIdFactory
{
    public string Next(string executionIntentId, DateTimeOffset now, int sequence)
    {
        // ...
        return $"TI{now:yyyyMMddHHmmss}{sequence:D4}{compact}";
    }
}
```

`compact` = intent id with `-` stripped, then **first 16 chars**.

| A42 requirement | Measured |
|---|---|
| Deterministic `From(execution_intent_id)` | **FAIL** — `now` + `sequence` are inputs |
| Crash-before-send yields the **same** 11 | **FAIL** unless caller freezes both extras |
| No attempt / sequence in the id | **FAIL** — `sequence:D4` is an attempt suffix (A05 shape, **overruled** by A42) |
| Charset `[0-9A-Z]`, 26 Crockford | **FAIL** — lowercase test id; 21–36 chars; `TI` prefix |
| Refuse default UUID | **FAIL** — `Guid.Empty` string accepted |
| Time not the uniqueness source | **FAIL** — second-granularity stamp; offset clock, not forced UTC |
| Unique in DB | **PARTIAL** — `execution_intents.cl_ord_id` UNIQUE exists; never written |

Worked collision / second-send (legal at this API):

```text
Next("intent-1", t0, 0)     →  TI…0000intent1     // first persist
// TCP dies after write; row unknown or still "Pending"
Next("intent-1", t0, 1)     →  TI…0001intent1     // NEW 11, same intent  — A42 ILLEGAL
Next("intent-1", t0+1s, 0)  →  TI… (new second)   // NEW 11 if the clock ticked
```

`MayRetry==false` on unknown **does not bind the factory**. A worker that “retries with a fresh ClOrdID because the first one is in-flight” **compiles**. The UNIQUE index then either accepts the new 11 (**double-send on the venue**) or rejects and a loop increments `sequence` (same illegal path).

UUID v7 truncation: only 16 hex (64 bits) enter the id. Two intents in the same millisecond can share that prefix; same `now` second + `sequence=0` → **same tag 11**. The UNIQUE index fail-closes the insert (good) or a helper bumps `sequence` (illegal new 11).

---

## 6. Persist row vs the helper (type mismatch)

Current `ExecutionIntent` (full surface):

```text
Id, CopyIntentId, RiskDecisionId, DestinationSymbol, Direction,
VolumeLots, ClOrdId?, FixOrderId?, Status:string="Pending",
CreatedAt, SentAt?, FilledAt?, FillPrice?, RejectReason?
```

| Needed for T3 / §34 | Disk |
|---|---|
| `cl_ord_id` UNIQUE | **yes** (EF index; nullable property) |
| `status` as `ExecutionOrderStatus` | **no** — unbound string `"Pending"` |
| `send_armed_at` | **no** |
| `unknown_since` / `unknown_reason` | **no** |
| `pre_send_book_fingerprint` | **no** |
| `fencing_token` / `fix_session_key` | **no** |
| `request_kind` / `orig_cl_ord_id` | **no** |
| `fix_orders` / `fix_execution_reports` | **no entities** |
| Writer (`new ExecutionIntent` / `ExecutionIntents.Add`) | **0** in product `*.cs` |

A future worker that retries on `Status != "Filled"` will treat **every** `"Pending"` row as first-send. The FSM boolean never runs.

`CopyIntentExpiry.IsExpired` exists. A42: expiry applies to **unsent** rows only. Nothing stops treating an unknown row as expired-and-replaceable. That would be a **licensed** second `35=D`.

---

## 7. Tests that lock (and the hole that does not)

Only consumer: `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs`.

| Fact | What it locks | Loss relevance |
|---|---|---|
| `Unknown_ack_cannot_retry_new_order` | `AfterSendAttempt` → `SentAcknowledgementUnknown`; `MayRetry==false`; `RequiresReconciliation==true` | **Yes** — in-session wait cannot authorize NOS |
| `Disconnect_after_send_is_unknown_state` | `AfterDisconnect` → `ExecutionStateUnknown` only | **Partial** — does **not** assert `MayRetry==false` |
| `Filled_report_is_terminal` | `Accepted` + `39=2` → `Filled` | Does not lock absorb of a later non-fill |
| `ClOrdId_is_deterministic_and_unique_per_sequence` | seq 0 ≠ seq 1; prefix `TI20260818120000` | **Freezes the dangerous sequence-suffix contract** A42 forbids |

**THE §34 fact is not a named test:**

```text
MayRetryNewOrderSingle(ExecutionStateUnknown) == false
```

True in code (closed form). A one-line edit `status is NotSent or Rejected or ExecutionStateUnknown` would keep `Unknown_ack_cannot_retry_new_order` **green** and **re-license double-send after disconnect**.

A89 names `UnknownExecutionNoBlindRetryTests` / `ExecutionOrderRetryPolicyTests` / `ClOrdIdGenerationTests` as EXISTS. On disk they are **MISSING**. Do not inherit those PASSes.

No harness fact: `DisconnectAfterSubmit` outbound `35=D` count == 1. There is no send path to count.

---

## 8. Call graph (who could still double-fill?)

```text
RiskDecisionRecord.AllowFixSend
    → (no IApprovedExecutionIntentService)
    → ExecutionIntent row          -- type exists; 0 writers
    → ClOrdIdFactory.Next          -- unit tests only
    → ExecutionOrderStateMachine   -- unit tests only
    → FIX 35=D                     -- NO builder, NO QuickFIX Send
```

`grep` product `*.cs` (exclude `bin`/`obj`/`_tmp_*`):

| Identifier | Hits |
|---|---|
| `ExecutionOrderStateMachine` / `MayRetryNewOrderSingle` / `AfterDisconnectWithUnknownAck` | definition + `ExecutionAndSizingTests` |
| `ClOrdIdFactory` | definition + same test file |
| `new ExecutionIntent` / `ExecutionIntents.Add` | **0** |
| `35=D` / `(35, "D")` in `src/Fix.CTrader` | **0** (log/comment only elsewhere) |

`apps/fix-worker/Worker.cs`: stamps QUOTE/TRADE `Disconnected`; `LastError = "NewOrderSingle remains off."`; if `CTrader:RealCopyExecutionEnabled` is true it **still** does not send.

`DependencyInjection` pins `RealCopyEnabled = false` with comment: *“Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.”*

**Current production:** it is impossible to retry unknown as `35=D` because it is impossible to send `35=D`. That is **`SAFE_BY_ABSENCE`**. It is **not** a demonstration that the first send-path PR will persist-then-arm-then-refuse-unknown.

The first send-path PR that does any of the following **breaks the thesis** even with today’s helper:

1. Persist `NotSent`/`Pending` → `Send` → persist unknown (crash window looks retryable).
2. `catch (IOException) { if (MayRetry) Send(same); }` after mapping `35=3` to `Rejected`.
3. `catch { Send(factory.Next(id, UtcNow, seq++)); }` — helper never consulted; **new 11 = double inventory**.
4. On TRADE logon, drain `execution_intents` where `Status != "Filled"`.
5. Treat CopyIntent expiry as license to replace an **unknown** row.

---

## 9. Score vs law (this slot)

| Claim | Source | This pass |
|---|---|---|
| Unique ClOrdID persisted **before** send | §33, A42 T1/T3 | **FAIL** as a system. UNIQUE index exists. No writer. Factory is not `From(id)`. |
| After send → `sent_unknown` | §33, A70 | **PASS** (`AfterSendAttempt`) |
| Disconnect → `EXECUTION_STATE_UNKNOWN` | §34 | **PASS** (constant) |
| Never retry unknown as `35=D` (same or new 11) | §34, §72.9, A42 §8.2 | **PASS** at helper for same-status retry. **FAIL** as a system (factory new-11, no T3, no caller). |
| Recon H → AF → AN before any replacement | §34, §70.6, A47 | **FAIL** — missing |
| Unique ClOrdID rules proven | §70.4 | **FAIL** — sketch + unused UNIQUE |
| Duplicate ER ≠ second fill | §70.5 | **FAIL** — `Apply` has no identity |
| Unknown-state recovery proven | §70.6, A100 G09 | **FAIL** |
| Live send default false | §41 | **PASS** (flag + absence) |
| Prefer miss over double | §72.9 | **LAW**. Not encoded in persist. |

Agreement with B16 / D17 / D98 on helper closed form and vacuous-safety call graph. This slot adds the **loss framing**: unknown-retry is the double-fill path, not a reconnect edge.

---

## 10. What “lower loss” requires before any live `35=D`

Not a coding task in this slot. Checklist so a later PR cannot claim this report as a send license:

```text
[x] MayRetry(SentAcknowledgementUnknown) == false          (helper + unit)
[x] MayRetry(ExecutionStateUnknown) == false               (helper; NOT a named unit)
[x] RequiresReconciliation on both unknowns
[x] REAL_COPY default false; no 35=D builder
[ ] ClOrdID = From(execution_intent_id), no clock, no sequence
[ ] T1 persist unique 11 as not_sent
[ ] T3 arm (sent_unknown + send_armed_at) COMMITS before Send
[ ] Crash after T3 cannot look like NotSent / "Pending"
[ ] Never revert sent_unknown / unknown → not_sent
[ ] Never mint a second 11 on a live / unknown row
[ ] MayRetry(Rejected) documented as new-row + new-11 only
[ ] 35=3 / unsure 35=j cannot become Rejected
[ ] Named fact: MayRetry(ExecutionStateUnknown)==false
[ ] Harness: DisconnectAfterSubmit outbound 35=D count == 1
[ ] Recovery 35=H → 35=AF → 35=AN + book fingerprint
[ ] Replacement 35=D only after not_on_venue + new risk + new intent
[ ] Unresolved unknown clears READY_FOR_EXECUTION / blocks new opens
[ ] Duplicate 35=8 does not double-book LastQty
```

Until the unchecked boxes are measured PASS, **do not enable** `REAL_COPY_EXECUTION`. A missed copy is cheaper than a double fill.

---

## 11. Disposition

| Metric | Value |
|---|---|
| Product source changed | **No** |
| Assigned thesis | Persist ClOrdID; never retry `EXECUTION_STATE_UNKNOWN` as `35=D` |
| Thesis vs law | **Correct and binding.** Not an edge. Double fill is the fastest live-copy loss. |
| Helper | **EXISTS_AND_GOOD** for the two unknown booleans |
| Factory | **EXISTS_NEEDS_REFACTOR** (time+seq can mint a second 11) |
| Persist-before-send | **MISSING** |
| System no-retry | **UNPROVEN** / `SAFE_BY_ABSENCE` |
| G09 | remains **FAIL** |
| Capital at risk from this tree starting now | **NONE** from `35=D` (no builder). **NONE** from this report (no product edit). |

**Do not claim “no-blind-retry is proven.”** Claim: the Domain predicate refuses NOS from `EXECUTION_STATE_UNKNOWN`; persist-before-send is the other half of the same loss rule and is not built; a future `35=D` that skips T3 or mints a new 11 on unknown is how this account would lose fastest.

*End P500_S014.*
