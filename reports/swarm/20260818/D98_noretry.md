# D98 — `MayRetryNewOrderSingle` is false after send

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D98_noretry.md` |
| Agent | D98 (no-retry-after-send close-read only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:46:11+05:30 |
| Workspace | `D:\Prop` |
| Assigned | `MayRetryNewOrderSingle` false after send. Write this file. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `TraderIntelligence.Domain.Execution.ExecutionOrderStateMachine.MayRetryNewOrderSingle` |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d98_noretry\` (`dotnet run`, Release; stdout saved) |
| Law | Architecture v2 **§33**, **§34**, **§41**, **§70.4–§70.6**, **§72.8–10**; A42 §5.3 / §8.2; A70 §10; A89 #69 / #82 / G12 (unknown → no NOS); A100 **G09** (unknown recovery). Not A100 G12 (that gate is stale-quote). |
| Prior (same FSM SHA, not copied as verdict) | B16 §3.1, D17 §5.4, D36 §3.1, A70 §10 |
| Method | Full re-read of the machine, enum, `ExecutionIntent`, unit fact, FIX worker. `Get-FileHash` SHA-256. `Select-String` of product `*.cs` (exclude `bin`/`obj`/`_tmp_*`) for `MayRetryNewOrderSingle` / `AfterSendAttempt` / `ExecutionOrderStateMachine`. Throwaway Domain eval printed the 8-status matrix. Re-ran `Unknown_ack_cannot_retry_new_order`. Nothing answered from memory. |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `UNSAFE`.

This file answers **one assigned question**. It is **not** an A70-complete FSM review (that is D17), **not** a claim that G09 is green, and **not** a proof that a future send path will refuse a second `35=D`.

---

## 0. Verdict

**Yes — after send, `MayRetryNewOrderSingle` is false.**

The only legal “after send” status the Domain machine emits is `SentAcknowledgementUnknown`. That value is **not** in the retry allow-list. Measured three independent ways this pass; all agree.

| Check | Result |
|---|---|
| Source: `AfterSendAttempt()` | always `SentAcknowledgementUnknown` (no arguments) |
| Source: `MayRetryNewOrderSingle` | `status is NotSent or Rejected` |
| Therefore `MayRetry(AfterSendAttempt())` | **false** (closed form; no other branch) |
| Eval `MAY_RETRY_AFTER_SEND` | **`False`** |
| Eval `RECON_AFTER_SEND` | **`True`** |
| Unit `Unknown_ack_cannot_retry_new_order` | **1/1 Passed** (0.3483 s) |

§73.B for **the helper on the after-send status**: **EXISTS_AND_GOOD** (this is the A70 §10 closed form for `sent_unknown`).  
§73.B for **no-blind-retry as a system**: **MISSING** — zero product callers, no persist-before-send arm, `ExecutionIntent.Status` is an unbound `string`, factory can mint a new tag 11 without asking the helper.

**One-line:** After a send attempt the machine parks `SentAcknowledgementUnknown` and will not authorize another `NewOrderSingle` from that status. That is necessary and **not sufficient**. Do **not** call this A100 G09 PASS.

| Question | Answer |
|---|---|
| Is `MayRetryNewOrderSingle` false after `AfterSendAttempt()`? | **Yes** |
| Is it false after disconnect-with-unknown-ack? | **Yes** in code (`ExecutionStateUnknown`); **not** locked by the disconnect unit fact |
| Can `NotSent` still retry? | **Yes** — that is the first-send license |
| Can `Rejected` still retry? | **Yes** at the helper — **new 11 only** (name does not say so) |
| Does any worker call the helper? | **No** |
| Can the tree emit a live `35=D` today? | **No** (flag default false + no builder) — vacuous safety, not this proof |
| Product source edited this pass | **0** |

---

## 1. Assigned question, restated against law

Architecture §33:

> Never simply retry a NewOrderSingle because the TCP connection broke. First reconcile.

Architecture §34:

> Do NOT blindly send the order again. Set `EXECUTION_STATE_UNKNOWN`. Then use OrderStatusRequest / OrderMassStatusRequest / ExecutionReports / Position reconciliation. Only after reconciliation may the system decide whether another order is required.

A70 §10 / A42 interpretation used here:

```text
catch (IOException) { SendNewOrderSingle(sameClOrdId); }   // ILLEGAL
catch (IOException) { SendNewOrderSingle(newClOrdId); }    // ILLEGAL
```

The Domain stand-in for “may I fire `35=D`?” is `MayRetryNewOrderSingle`. “After send” in this tree means `AfterSendAttempt()` → `SentAcknowledgementUnknown` (in-session wait). Disconnect after a possible write is a **different** helper (`AfterDisconnectWithUnknownAck` → `ExecutionStateUnknown`). Both must refuse NOS. This ticket asks about the **after-send** predicate.

---

## 2. Files measured (this pass)

| Path | Bytes | Newlines | SHA-256 | LastWriteUtc | Role |
|---|---:|---:|---|---|---|
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | 2177 | 56 | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` | 2026-08-18T07:38:10Z | SUT — constants + predicates + `Apply` |
| `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` | 260 | 13 | `801D89D0A5D0E73F76EC195776C5A4D2BA3A09630F13A148C1B9C0AF27D9E7AF` | 2026-08-18T07:36:08Z | 8-value vocabulary |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | 783 | 21 | `56DC9ED8E4DAC442A66620386864F919B34F851FF22974CA2FBC23B0A5CC3617` | 2026-08-18T08:07:43Z | persist row; **string** `Status` |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | 2144 | 62 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | 2026-08-18T07:47:42Z | only consumer |
| `D:\Prop\apps\fix-worker\Worker.cs` | 2093 | 51 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | 2026-08-18T08:04:48Z | 15 s heartbeat; refuses NOS in a log line |

FSM + enum + unit-test hashes match B16 / D17 / D36. **`ExecutionIntent` does not.** D17 recorded `4E5EEDFA…` (20 lines, enum-shaped columns). Current bytes are a different entity (`Status: string = "Pending"`, `SentAt`, `FilledAt`, `VolumeLots`, no `ExecutionOrderStatus` column). Use this file, not D17 §7, for the persist row.

Eval tree (not product):

| Path | Role |
|---|---|
| `D:\Prop\reports\swarm\20260818\_tmp_d98_noretry\D98NoretryEval.csproj` | references Domain only |
| `D:\Prop\reports\swarm\20260818\_tmp_d98_noretry\Program.cs` | prints after-send + 8×2 matrix |
| `D:\Prop\reports\swarm\20260818\_tmp_d98_noretry\stdout.txt` | this-run capture |

---

## 3. The predicate (verbatim)

```17:40:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static ExecutionOrderStatus AfterSendAttempt() =>
        ExecutionOrderStatus.SentAcknowledgementUnknown;

    public static ExecutionOrderStatus AfterDisconnectWithUnknownAck() =>
        ExecutionOrderStatus.ExecutionStateUnknown;

    public static ExecutionOrderStatus Apply(ExecutionOrderStatus current, ExecutionReportInput report)
    {
        // … map + three absorb rules; never emits NotSent …
    }

    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;

    public static bool RequiresReconciliation(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.SentAcknowledgementUnknown
            or ExecutionOrderStatus.ExecutionStateUnknown;
```

There is no third clause. There is no “unless the socket might have dropped.” There is no ClOrdID argument. After send is therefore **false** solely because `SentAcknowledgementUnknown` is neither `NotSent` nor `Rejected`.

`AfterSendAttempt()` takes **no** current status. It cannot refuse to overwrite a `Filled` row. That is a worker contract, not a hole in this boolean.

---

## 4. Measured after-send result

Command:

```text
dotnet run --project D:\Prop\reports\swarm\20260818\_tmp_d98_noretry\D98NoretryEval.csproj -c Release --nologo
```

Stdout (this pass):

```text
AFTER_SEND=SentAcknowledgementUnknown
MAY_RETRY_AFTER_SEND=False
RECON_AFTER_SEND=True
AFTER_DISCONNECT=ExecutionStateUnknown
MAY_RETRY_AFTER_DISCONNECT=False
RECON_AFTER_DISCONNECT=True
MAY_RETRY_NOTSENT=True
MAY_RETRY_REJECTED=True
```

That is the assigned fact, executed against the compiled Domain assembly, not inferred from a comment.

---

## 5. Full eight-status matrix (same eval)

| # | `ExecutionOrderStatus` | `MayRetryNewOrderSingle` | `RequiresReconciliation` | After-send meaning |
|---:|---|---|---|---|
| 0 | `NotSent` | **true** | false | **Before** send. First `35=D` only. |
| 1 | `SentAcknowledgementUnknown` | **false** | **true** | **After send.** Assigned fact. |
| 2 | `Accepted` | false | false | Working. No NOS. |
| 3 | `PartiallyFilled` | false | false | Working. No NOS. |
| 4 | `Filled` | false | false | Terminal. No NOS. |
| 5 | `Rejected` | **true** | false | **Not** “after send.” New row + **new** 11 only. Same 11 is burned. |
| 6 | `Cancelled` | false | false | Terminal. No NOS. |
| 7 | `ExecutionStateUnknown` | **false** | **true** | After disconnect / unreadable venue. No NOS. |

A70 §10 closed form, re-executed:

```text
MayRetryNewOrderSingle  = NotSent | Rejected
RequiresReconciliation  = SentAcknowledgementUnknown | ExecutionStateUnknown
```

No unknown state can return true. That is the entire Domain “no blind retry” guarantee.

---

## 6. Unit lock (what is frozen vs what is not)

```9:16:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    [Fact]
    public void Unknown_ack_cannot_retry_new_order()
    {
        var sent = ExecutionOrderStateMachine.AfterSendAttempt();
        sent.Should().Be(ExecutionOrderStatus.SentAcknowledgementUnknown);
        ExecutionOrderStateMachine.MayRetryNewOrderSingle(sent).Should().BeFalse();
        ExecutionOrderStateMachine.RequiresReconciliation(sent).Should().BeTrue();
    }
```

This-run:

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~Unknown_ack_cannot_retry_new_order
```

| | |
|---|---|
| Started | 2026-08-18 13:45:49 local |
| Total | **1** |
| Passed | **1** (`Unknown_ack_cannot_retry_new_order`, 4 ms) |
| Failed | **0** |
| Exit | **0** |

This is the **only** on-disk fact that binds “after write → no second `35=D`.” Keep it. A89 #69 / #82 named classes still **do not exist**.

Not locked by any fact (code already returns these values; a future edit would stay green):

```text
MayRetryNewOrderSingle(ExecutionStateUnknown) == false     -- THE §34 disconnect fact
MayRetryNewOrderSingle(Accepted|PartiallyFilled|Filled|Cancelled) == false
MayRetryNewOrderSingle(NotSent) == true
MayRetryNewOrderSingle(Rejected) == true   AND "same 11 never"
RequiresReconciliation only on the two unknowns
outbound 35=D count == 1 after DisconnectAfterSubmit
```

`Disconnect_after_send_is_unknown_state` only asserts the parking enum. D36 called that a hole. Confirmed this pass: the eval shows `MAY_RETRY_AFTER_DISCONNECT=False`, the test does not.

---

## 7. Product callers (still zero)

`Select-String` of `MayRetryNewOrderSingle`, `AfterSendAttempt`, `AfterDisconnectWithUnknownAck`, `ExecutionOrderStateMachine` over `D:\Prop\**\*.cs` excluding `bin` / `obj` / `_tmp_*`:

| Location | Hits |
|---|---|
| `src\Domain\Execution\ExecutionOrderStateMachine.cs` | definitions |
| `tests\Unit\ExecutionAndSizingTests.cs` | the three FSM facts |

**No** Application service, FIX worker, MT5 worker, seeder, store, or dashboard query invokes the helper. `Infrastructure\DependencyInjection.cs` does not register the machine (it is static). `FixSessionOwnership.ExecutionIntentsAllowed` is a **different** gate (`ownership && reconciled`) and is also unused by the worker.

`apps\fix-worker\Worker.cs` (15 s loop):

- stamps QUOTE / TRADE `FixSessionState` to `Disconnected`
- log: `NewOrderSingle remains off.`
- if `CTrader:RealCopyExecutionEnabled` is true, **still** does not send; it only warns

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false**. There is no `35=D` builder, no QuickFIX session, no `Session.Send`.

`grep` of `new ExecutionIntent` / `ExecutionIntents.Add` in product `*.cs`: **0**. The type is mapped (`execution_intents`, unique `ClOrdId`) and never written. `CopyIntent.ExecutionIntentId` is nullable and unused.

**Current production behavior:** it is impossible to blindly retry NOS because it is impossible to send NOS. That is **fail-closed by absence**. It is **not** a demonstration that a future send path will call this helper.

---

## 8. Why “false after send” is not a system proof

These are **not** defects in the boolean. They are why G09 stays FAIL even though the assigned fact is true.

| ID | Hole | Why it survives a green helper |
|---|---|---|
| H1 | Persist `NotSent` → socket write → persist `sent_unknown` | Crash between write and the second persist leaves `NotSent`. `MayRetry(NotSent)==true`. That is the A42 §5.3 / §34 resend bug. `SentAt` exists on the row and **nobody sets it**. There is no `send_armed_at`. |
| H2 | `ExecutionIntent.Status` is `string` default `"Pending"` | Helper takes `ExecutionOrderStatus`. Nothing maps `"Pending"` / `"SHADOW_ONLY"` / `"SentAcknowledgementUnknown"` onto the enum. A worker that retries on `Status != "Filled"` never sees this predicate. |
| H3 | `ClOrdIdFactory.Next(..., sequence++)` | Mints a **new** tag 11. Helper is never consulted. A42 forbids a second 11 on a live row. |
| H4 | `MayRetry(Rejected)==true` | Legal **only** as “new intent + new 11.” The name reads as “resend NOS.” Session reject (`35=3`) stuffed into `Apply` as `REJECTED` would **license** a retry of a possibly-live order. |
| H5 | `AfterDisconnect*` assigned onto a terminal row | Helper has no `current` argument. Parks `ExecutionStateUnknown` even over `Filled`. Later replacement-11 logic could fire. |
| H6 | No `35=H` / `35=AF` / `35=AN` | Unknown is a parking bit. Recovery that could legally emit `not_on_venue` (and only then a new 11) is **MISSING**. |
| H7 | Tests do not lock the disconnect `MayRetry==false` | A one-line edit `status is NotSent or Rejected or ExecutionStateUnknown` would keep `Unknown_ack_cannot_retry_new_order` green. |

Do **not** treat A89 `UnknownExecutionNoBlindRetryTests` / `ExecutionOrderRetryPolicyTests` as present. Those names are a backlog.

---

## 9. Score vs the law this ticket is scored against

| Claim | Source | This pass |
|---|---|---|
| After send → `sent_unknown` | §33, A70 | **PASS** (`AfterSendAttempt`) |
| `MayRetry(sent_unknown) == false` | §34, A42 §8.2, A70 §10, **this ticket** | **PASS** (source + eval + unit) |
| `RequiresReconciliation(sent_unknown) == true` | A70 §10 | **PASS** |
| Disconnect → `EXECUTION_STATE_UNKNOWN` | §34 | **PASS** (constant); retry-false **untested** |
| `MayRetry(execution_unknown) == false` | §34 | **PASS** in code / eval; **FAIL** as a named fact |
| No NOS retry as a **system** | §34, G09 | **FAIL** (no arm, no caller, no recovery) |
| Unique ClOrdID, never reuse | §33, §70.4, A42 | **PARTIAL** — UNIQUE index; factory is time+seq |
| Persist-before-send / T3 arm | A42 §5.3 | **FAIL** |
| Live send remains default false | §41 | **PASS** (flag + absence) |
| A89 #69 / #82 dedicated classes | A89 | **MISSING** (one mixed-file fact only) |
| A100 G09 | A100 / C14 / D42 | remains **FAIL** |

Agreement with B16 / D17 / D36 on the helper closed form and the vacuous-safety call graph. D98 adds a **compiled** 8-status print (`_tmp_d98_noretry\stdout.txt`) and records that `ExecutionIntent` has moved since D17 (string status, `SentAt` unused).

---

## 10. What a later send-path PR must not do

The helper will not save these, even though `MayRetry` is already false after send:

1. Persist `NotSent` → `Send` → persist `SentAcknowledgementUnknown`.
2. `catch (IOException) { if (MayRetry) Send(same); }` after mapping `35=3` to `Rejected`.
3. `catch { Send(factory.Next(id, UtcNow, seq++)); }` — helper never consulted.
4. Drain `execution_intents` where `Status != "Filled"` on TRADE logon (`"Pending"` would fire).
5. Treat `MayRetry(Rejected)` as “resend this 11.”

Legal after-send path (not implemented; listed so this PASS is not misread as a license to send):

```text
T3  COMMIT status=sent_ack_unknown + send_armed_at   -- before any byte
T4  Session.Send(35=D)
    on any doubt: stay sent_unknown or promote ExecutionStateUnknown
    MayRetry == false
    RequiresReconciliation == true
    do not mint a second 11
    35=H / 35=AF / 35=AN before any replacement
```

---

## 11. Disposition

| Metric | Value |
|---|---|
| Product source changed | **No** |
| Test source changed | **No** |
| Assigned fact | `MayRetryNewOrderSingle` **false** after send |
| Helper class (§73.B) | **EXISTS_AND_GOOD** for that one boolean |
| System class | **MISSING** / vacuous-safe by absence |
| FSM SHA-256 | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` |
| Unit fact this-run | **1 / 0 / 0** (pass / fail / skip) |
| Eval | `MAY_RETRY_AFTER_SEND=False` |
| INDEX / SWARM_LOG rewritten | SWARM_LOG entry only; INDEX not rewritten (D-band still landing) |

**Do not claim “no-blind-retry is proven.”** Claim: after `AfterSendAttempt()` the Domain predicate returns false, the unit fact locks that, reconciliation is required, and nothing in product calls it yet.

*End D98.*
