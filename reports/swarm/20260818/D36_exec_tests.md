# D36 — `ExecutionAndSizingTests` review (FSM + qty + ClOrdID + expiry)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D36_exec_tests.md` |
| Agent | D36 (execution/sizing unit-test review) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:36:50+05:30 (`dotnet test` filter) |
| Assigned | Read `ExecutionAndSizingTests.cs`. Write this file. Do not modify product source. |
| Product source edited | **No** |
| Test source edited | **No** |
| Subject | `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` |
| Law | Architecture v2 §33–§34, §36, §38, §60, §63, §70.4–§70.6; A09 / A27 / A42 / A43 / A70 §14 / A89 #47 #55 #67 #69 #80–#82 |
| Prior (same SHA, not copied as verdict) | B08 inventory, B16 §5, C17 §4 items 8/15–17, D18 §6 (qty smoke only) |
| Method | Full re-read of the 62-line class. Mapped every assertion to SUT branches. Re-hashed SUTs. Re-ran the class. Grep of product callers. Nothing answered from memory. |

Classification vs a required contract: `LOCKED` / `SMOKE` / `MISNAMED` / `MISSING`. A green `dotnet test` is not coverage.

---

## 0. Verdict

**INSUFFICIENT. Six passing smokes in one mixed file. 0 of the A89 dedicated execution classes exist. 0 of 4 §60 bullets this file is used to “cover” are COVERED.**

| Gate | Required | This file |
|---|---|---|
| §60 #8 source/destination quantity conversion | never passthrough lots as OrderQty | **SMOKE** of last-stage floor only; `0.10 → 0.10` |
| §60 #15 copy-intent idempotency | same source event → one intent | **MISSING** (this fact is expiry, not idempotency) |
| §60 #16 ClOrdID generation | unique, persist-before-send, stable `From(id)` | **SMOKE** of prefix + seq uniqueness |
| §60 #17 ExecutionReport state transitions | full 39/150 matrix + sticky terminals + no backward | **SMOKE** of 3 of ~20 A70 §14 facts |
| A70 §14 unit list | 13 named facts | **2 locked, 1 misnamed, 10 missing** |
| A89 #67 / #69 / #80 / #81 / #82 | dedicated classes | **MISSING** (this informal stand-in only) |
| A100 G09 / G10 / G12 | unknown recovery + sizing verified + no-blind-retry | **not proven** by 6/6 green |

**One-line:** 6/6 pass because they never ask the numbers, edges, or second `Apply` that would fail today’s SUTs — and they never ask the §34 fact `MayRetry(ExecutionStateUnknown)==false`.

Do **not** claim “execution/sizing unit tests cover Architecture §60.” Do **not** treat A89 `ClOrdIdGenerationTests` / `UnknownExecutionNoBlindRetryTests` / `ExecutionReportStateTransitionTests` / `ExecutionOrderTerminalStickyTests` / `ExecutionOrderRetryPolicyTests` as present. Those names are a backlog. The only on-disk lock is this file.

**Stale predecessor to correct:** B16 §5 says `ClOrdId_is_deterministic_and_unique_per_sequence` asserts “same args stable.” It does **not**. It never calls `Next` twice with the same `(intent, now, sequence)`. The method name lies.

---

## 1. What was actually read and measured

### 1.1 Test file

| | |
|---|---|
| Path | `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` |
| Bytes | 2144 |
| Lines | 62 |
| SHA-256 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` |
| LastWriteUtc | 2026-08-18T07:47:42Z |
| Namespace | `TraderIntelligence.Tests.Unit` (flat; not `Execution/` / `Fsm/` / `Sizing/`) |
| Framework | xUnit `[Fact]` + FluentAssertions. No `[Theory]`. No `[Trait]`. No skip. |

Unchanged vs B08 / C17 recorded hash.

### 1.2 SUTs (product not edited)

| Path | Bytes | Lines | SHA-256 | Role |
|---|---:|---:|---|---|
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | 2177 | 56 | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` | status mapper + retry/recon predicates |
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | 1041 | 31 | `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` | last-stage floor (not §38 converter) |
| `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs` | 658 | 17 | `CCC5506F2661C9D23EEA83070C1BFE5585158508718ECF1781C7C298E013D0A1` | tag-11 mint (`TI` + clock + seq + compact) |
| `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` | 246 | 6 | `76B82E4F0C6F6B43988D5E50EE5E5D229CC451C7E8267AD6DF56271790531D38` | `now - source > max` |
| `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` | — | 13 | `801D89D0A5D0E73F76EC195776C5A4D2BA3A09630F13A148C1B9C0AF27D9E7AF` | 8-value enum |

Adjacent (not asserted by this class): `ExecutionIntent`, `CopyIntent.IdempotencyKey` / `ExpiresAt`, `RiskEngine` `SIGNAL_STALE`, `QuantityNormalizerStepMinMaxTests`, `SourceDestinationQuantityConversionTests`.

### 1.3 Measured run (this wave)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity normal
  --filter FullyQualifiedName~ExecutionAndSizingTests
```

| | |
|---|---|
| Started | 2026-08-18 13:36:50 local |
| Total | **6** |
| Passed | **6** |
| Failed | **0** |
| Skipped | **0** |
| Duration | 0.3405 s |
| Exit | **0** |
| Warnings / errors | 0 |

Passed (order as printed):

```text
Copy_intent_expires
Unknown_ack_cannot_retry_new_order
Disconnect_after_send_is_unknown_state
Filled_report_is_terminal
Quantity_normalizer_steps_and_min
ClOrdId_is_deterministic_and_unique_per_sequence
```

A green class run is **not** G09 / G10 / G12.

### 1.4 Product callers (still zero)

`grep` of `MayRetryNewOrderSingle`, `AfterSendAttempt`, `AfterDisconnectWithUnknownAck`, `new ClOrdIdFactory`, `CopyIntentExpiry.IsExpired`, `QuantityNormalizer` over `D:\Prop\**\*.cs` excluding `bin`/`obj`:

| Symbol | Product `src/` | Tests |
|---|---|---|
| `ExecutionOrderStateMachine.*` | definition only | this file |
| `ClOrdIdFactory` | definition only | this file |
| `CopyIntentExpiry.IsExpired` | definition only (`RiskEngine` inlines its own age check) | this file |
| `QuantityNormalizer` | definition only | this file **and** `Sizing/` + `Normalization/` siblings |

`apps/fix-worker` still has no `35=D` send path. Helpers are vacuous-safe by absence, not by this suite.

---

## 2. File census — four domains dumped into one class

A89 §1: *“One public class per capability cluster (do not dump six domains into one file).”*

| Lines | Fact | SUT | A89 name it stands in for | Role |
|---:|---|---|---|---|
| 9–16 | `Unknown_ack_cannot_retry_new_order` | `ExecutionOrderStateMachine` | #69 + #82 | no-retry after send |
| 18–23 | `Disconnect_after_send_is_unknown_state` | same | #69 | park unknown (predicates **not** asserted) |
| 25–32 | `Filled_report_is_terminal` | `Apply` | #80 + #81 | map only; **not** sticky |
| 34–42 | `Quantity_normalizer_steps_and_min` | `QuantityNormalizer` | #47 (already has a dedicated file) | 3 last-stage numbers |
| 44–53 | `ClOrdId_is_deterministic_and_unique_per_sequence` | `ClOrdIdFactory` | #67 | prefix + seq 0 ≠ 1 |
| 55–61 | `Copy_intent_expires` | `CopyIntentExpiry` | #55 | 16s vs 5s vs 15s max |

No `[Theory]` over the 8-value enum. No fixture table. No `ExecutionReportInput` builder. `ClOrdId`, `VenueOrderId`, `LastQty`, `CumQty`, `LeavesQty`, `Text` on the one ER are never asserted (and `Apply` never reads the qty fields).

Sibling files that already own part of this surface (do not double-count as this class):

| Path | What it actually locks |
|---|---|
| `tests/Unit/Sizing/QuantityNormalizerStepMinMaxTests.cs` | last-stage floor / min / max / throws (A89 #47) |
| `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` | 4 passthrough locks + 21 skipped A43 facts |

This file’s qty fact is a **subset** of `QuantityNormalizerStepMinMaxTests`. It adds no new branch.

---

## 3. Fact-by-fact (what is locked vs what the name claims)

### 3.1 `Unknown_ack_cannot_retry_new_order` — **LOCKED** (narrow)

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

| Assert | SUT branch | Result |
|---|---|---|
| `AfterSendAttempt() == SentAcknowledgementUnknown` | constant return | locked |
| `MayRetry(sent) == false` | `status is NotSent or Rejected` | locked for **this** status |
| `RequiresReconciliation(sent) == true` | `SentAcknowledgementUnknown \| ExecutionStateUnknown` | locked for **this** status |

This is the only fact in the tree that binds “in-session wait after write → no second `35=D`.” Keep it. It is **not** the §34 disconnect fact.

Not asked:

- `MayRetry` / `RequiresReconciliation` on the other seven enum values
- that `AfterSendAttempt` ignores arguments (it has none — persist-before-send is untestable here)

### 3.2 `Disconnect_after_send_is_unknown_state` — **SMOKE / HOLE**

```18:23:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    [Fact]
    public void Disconnect_after_send_is_unknown_state()
    {
        ExecutionOrderStateMachine.AfterDisconnectWithUnknownAck()
            .Should().Be(ExecutionOrderStatus.ExecutionStateUnknown);
    }
```

Parks the overlay. **Does not** assert:

```text
MayRetryNewOrderSingle(ExecutionStateUnknown) == false     -- THE §34 / G12 fact
RequiresReconciliation(ExecutionStateUnknown) == true
```

SUT already returns those values (B16 matrix). The test does not lock them. A future edit that sets `MayRetry(ExecutionStateUnknown)=true` keeps this fact green.

B16 called this **Partial**. Confirmed.

### 3.3 `Filled_report_is_terminal` — **MISNAMED**

```25:32:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    [Fact]
    public void Filled_report_is_terminal()
    {
        var status = ExecutionOrderStateMachine.Apply(
            ExecutionOrderStatus.Accepted,
            new ExecutionReportInput("c1", "v1", "FILL", "2", 0.1m, 0.1m, 0, null));
        status.Should().Be(ExecutionOrderStatus.Filled);
    }
```

What it actually proves: `Apply(Accepted, OrdStatus="2") == Filled`.

What the name claims: Filled is absorbing.

| Input field | Used by `Apply`? | Asserted? |
|---|---|---|
| `ClOrdId = "c1"` | no | no |
| `VenueOrderId = "v1"` | no | no |
| `ExecType = "FILL"` | **no** — `OrdStatus` is non-blank, so key = `"2"` | no |
| `OrdStatus = "2"` | yes → `Filled` | via result only |
| `LastQty = 0.1` | no (`Apply` never reads qty) | no |
| `CumQty = 0.1` | no | no |
| `LeavesQty = 0` | no | no |
| `Text = null` | no | no |

`MapOrdStatus` prefers tag 39. `"FILL"` is dead on this call. A second `Apply(Filled, 39=1)` is **never issued**, so A70 §5.2 stickiness is unlocked. A70 also requires `Filled` lock to include qty (`cum == order`); `Apply` has no `order_qty`.

Missing map keys on this class (all compiled in `MapOrdStatus`):

| Key | Proposed | Fact? |
|---|---|---|
| `"0"` / `"NEW"` | Accepted | **no** |
| `"1"` / `"PARTIAL"` / `"PARTIALLY FILLED"` | PartiallyFilled | **no** |
| `"2"` / `"FILL"` / `"FILLED"` | Filled | only `"2"` from Accepted |
| `"4"` / `"CANCELED"` / `"CANCELLED"` | Cancelled | **no** |
| `"8"` / `"REJECTED"` / `"REJECT"` | Rejected | **no** |
| `"A"` / `"PENDING_NEW"` | Accepted | **no** |
| `"C"` / `"EXPIRED"` | **ExecutionStateUnknown** today (A70 wants Cancelled) | **no** |
| `"I"` (150=I status snapshot) | **ExecutionStateUnknown** if used as key | **no** |
| garbage / empty both | ExecutionStateUnknown | **no** |

A70 §5.2 backward-edge defect (`PartiallyFilled` + `39=0` → `Accepted`) is **untested**. A characterization fact would go red if someone later tightens `Apply`. Today a green run cannot see the hole.

### 3.4 `Quantity_normalizer_steps_and_min` — **SMOKE** (superseded)

```34:42:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    [Fact]
    public void Quantity_normalizer_steps_and_min()
    {
        var n = new QuantityNormalizer();
        var spec = new InstrumentQuantitySpec(0.01m, 5m, 0.01m, 2);
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
    }
```

| Call | Result | What it is |
|---|---:|---|
| `0.10 × 1` | **0.10** | lots passthrough. A43 E01 / G7 wants **10.00** on a 100 oz BaseUnits book |
| `0.10 × 0.05` | **0** | below min after `0.005` floor. Not `SIZE_BELOW_MIN` |
| `0.333 × 1` | **0.33** | `Truncate` to step 0.01. Correct last-stage floor |

All three rows are already in `QuantityNormalizerStepMinMaxTests` (`Floors_to_step`, `Below_min_returns_zero`). This fact does **not** lock max, unaligned max (F1 / 5.09), allocation `> 1`, throws, or ounces.

Do not cite this method as §60 #8. D18 already scored the converter **MISSING**.

### 3.5 `ClOrdId_is_deterministic_and_unique_per_sequence` — **MISNAMED / SMOKE**

```44:53:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    [Fact]
    public void ClOrdId_is_deterministic_and_unique_per_sequence()
    {
        var f = new ClOrdIdFactory();
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var a = f.Next("intent-1", now, 0);
        var b = f.Next("intent-1", now, 1);
        a.Should().NotBe(b);
        a.Should().StartWith("TI20260818120000");
    }
```

Computed values (SUT, not asserted):

```text
compact("intent-1") = "intent1"
a = TI202608181200000000intent1
b = TI202608181200000001intent1
```

| Claim in the name | Asserted? | SUT truth |
|---|---|---|
| Deterministic (same args → same string) | **No** — `Next` is never called twice with `(intent-1, now, 0)` | would pass if asked (`Next` is pure) |
| Unique per sequence | **Yes** — seq 0 ≠ seq 1 | locked |
| Prefix `TI` + compact clock | **Yes** — `StartWith("TI20260818120000")` | locked (both `a` and `b` share it) |
| Full string / `D4` seq / hyphen strip | **No** | `intent-1` → `intent1` is implicit |
| Empty / whitespace intent throws | **No** | SUT throws `ArgumentException` |
| `sequence < 0` throws | **No** | SUT throws `ArgumentOutOfRangeException` |
| Truncate compact at 16 | **No** | `compact[..16]` untested |
| A42 `From(execution_intent_id)` 26-char Crockford, no seq | **No** | factory is `TI` + clock + **sequence suffix** — A42 **forbids** this |
| Charset `0-9A-Z` (no lowercase) | **No** | `"intent1"` is lowercase |
| Persist-before-send reuse of the **same** 11 | **No** | no persist |

`StartWith("TI20260818120000")` is a weak uniqueness lock: any id minted in that UTC second matches. The only uniqueness assert is `a != b` for two sequences.

A42 §4.2: *“Do not put an attempt, hostname, or sequence into `cl_ord_id`.”* This fact **locks the forbidden shape**. That is useful as a characterization of today’s factory. It is **not** a pass of §70.4.

B16 §5 “same args stable” is **stale**. Correct it from this file.

### 3.6 `Copy_intent_expires` — **SMOKE** (not §60 #15)

```55:61:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
    [Fact]
    public void Copy_intent_expires()
    {
        var t = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        CopyIntentExpiry.IsExpired(t, t.AddSeconds(16), TimeSpan.FromSeconds(15)).Should().BeTrue();
        CopyIntentExpiry.IsExpired(t, t.AddSeconds(5), TimeSpan.FromSeconds(15)).Should().BeFalse();
    }
```

SUT: `now - sourceEventTime > maxSignalAge` (strict `>`).

| Case | Result | Locked? |
|---|---|---|
| 16s > 15s | expired | yes |
| 5s > 15s | not expired | yes |
| **15s == 15s** | not expired | **no** |
| `CopyIntent.ExpiresAt` | unused by helper | **no** |
| OPEN vs CLOSE family ages (A71 / A73) | one `TimeSpan` | **no** |
| A53: 20 intents after 3-minute TRADE gap → 0 NOS | no send path | **no** |
| Same source event → one `IdempotencyKey` | different concern | **MISSING** |

`RiskEngine` recomputes `DecisionTime - SourceEventTime` itself and does not call this helper. A green expiry smoke does not prove `SIGNAL_STALE` on the risk path.

C17: *“`Copy_intent_expires` is expiry, not idempotency.”* Confirmed.

---

## 4. §60 / A70 / A89 score for **this file only**

### 4.1 Architecture §60 (the four bullets people hang on this class)

| # | Required | Facts here | Status |
|---:|---|---|---|
| 8 | source/destination quantity conversion | `Quantity_normalizer_steps_and_min` | **PARTIAL** (last-stage; converter absent). Dedicated siblings exist; still 0 COVERED. |
| 15 | copy-intent idempotency | `Copy_intent_expires` | **MISSING** — wrong contract |
| 16 | ClOrdID generation | `ClOrdId_is_deterministic_and_unique_per_sequence` | **PARTIAL** — prefix + seq; no stability, no persist-before-send, shape ≠ A42 |
| 17 | ExecutionReport state transitions | 3 FSM facts | **PARTIAL** — send-unknown locked; disconnect incomplete; “terminal” is a map |

### 4.2 A70 §14 unit list vs this file

| Required fact (A70 §14) | Here | Class |
|---|---|---|
| `NotSent_AfterSendAttempt_is_sent_unknown` | inside `Unknown_ack_…` | **LOCKED** |
| `SentUnknown_39_0_is_accepted` | — | **MISSING** |
| `SentUnknown_39_2_skips_to_filled` | — | **MISSING** |
| `Accepted_39_1_is_partial` | — | **MISSING** |
| `Partial_then_39_2_is_filled` | — | **MISSING** |
| `Partial_then_39_0_stays_partial` | — | **MISSING** (SUT currently **violates**) |
| `Filled_then_39_1_stays_filled` | name only; no second Apply | **MISSING** |
| `Rejected_and_Cancelled_absorb` | — | **MISSING** |
| `Accepted_39_8_with_cum_0_is_rejected` | — | **MISSING** |
| `Partial_39_8_does_not_clear_fills` | — | **MISSING** (qty not in `Apply`) |
| `39_C_maps_to_cancelled` | — | **MISSING** (SUT → unknown) |
| `150_I_does_not_book_LastQty` | — | **MISSING** (qty not booked at all) |
| `MayRetry_only_NotSent_or_Rejected` | only `sent_unknown → false` | **PARTIAL** |
| `RequiresReconciliation_on_both_unknowns` | only sent-unknown | **PARTIAL** |

Duplicate / qty A70 facts (`Duplicate_ExecID…`, FAQ two sockets) : **MISSING**. `ExecutionReportInput` has no `ExecId` / fingerprint.

### 4.3 A89 named classes (EXISTS = SUT exists, not the test class)

| # | Named class | On disk | Absorbed here? |
|---:|---|---|---|
| 47 | `QuantityNormalizerStepMinMaxTests` | **yes** (`Sizing/`) | 3 overlapping rows |
| 55 | `StaleCopyIntentExpiryTests` | **no** | 2-row smoke only |
| 67 | `ClOrdIdGenerationTests` | **no** | prefix + seq |
| 68 | `CopyIntentIdempotencyKeyTests` | **no** | **not even a smoke** |
| 69 | `UnknownExecutionNoBlindRetryTests` | **no** | 1.5 of 3 required asserts |
| 80 | `ExecutionReportStateTransitionTests` | **no** | one map (`39=2`) |
| 81 | `ExecutionOrderTerminalStickyTests` | **no** | **no** (misnamed fact) |
| 82 | `ExecutionOrderRetryPolicyTests` | **no** | sent-unknown only |

A89 “EXISTS” on those rows means the Domain type is present. Do not inherit PASS.

---

## 5. What this file gets right

- The one safety-critical Domain predicate people will call first is locked: **after send, do not retry NOS; reconcile.**
- Disconnect parks `ExecutionStateUnknown` (status constant only).
- Clock is an **input** to `ClOrdIdFactory.Next` (testable; good). Sequence uniqueness is at least asked.
- Expiry uses a frozen `DateTimeOffset` and a strict `>` that matches `RiskLimits` default 15 s.
- Qty smoke uses `decimal` and the same `(0.01, 5, 0.01, 2)` spec as the dedicated last-stage suite.
- No live I/O. No broker passwords. No `UnitTest1` placeholder.

That is a **starter smoke**, not a contract.

---

## 6. Defects this suite cannot see (SUT holes that stay green)

These are product facts, listed so a green D36 run is not misread as “FSM/sizing done.” Product was not edited.

| ID | SUT | Why 6/6 cannot catch it |
|---|---|---|
| F-SM1 | `Apply` allows backward `PartiallyFilled → Accepted` | no partial fact |
| F-SM2 | `39=C` → `ExecutionStateUnknown` not `Cancelled` | no `C` fact |
| F-SM3 | `Apply` ignores `LastQty`/`CumQty`/`LeavesQty` | ER qty never asserted |
| F-SM4 | `MayRetry(Rejected)==true` does not allocate a new 11 | Rejected untested; helper has no ClOrdID |
| F-ID1 | `ClOrdIdFactory` is time+seq, not A42 `From(uuid)` | fact **rewards** the seq suffix |
| F-ID2 | lowercase compact; length ≠ 26 | charset/length untested |
| F-Q1 | `Normalize` passthrough lots (G7) | fact **expects** `0.10` |
| F-Q2 | unaligned max returned raw (D18 F1) | max untested here |
| F-EX1 | helper ignores `CopyIntent.ExpiresAt` | only `maxSignalAge` asked |
| F-CALL | zero product callers | unit green ≠ send-path green |

---

## 7. What to add next (tests only; not this agent’s write)

Do **not** grow this mixed file. Split to A89 names under `tests/Unit/Execution/` and `tests/Unit/Fsm/`. Keep `Unknown_ack_cannot_retry_new_order` as the first fact of #69/#82 (move, do not weaken).

Minimum facts that would have to go red or skip-closed against **today’s** SUTs:

```text
#69 / #82
  MayRetry(ExecutionStateUnknown) == false
  RequiresReconciliation(ExecutionStateUnknown) == true
  MayRetry(NotSent) == true
  MayRetry(Rejected) == true          -- document: new 11 only
  MayRetry(Accepted|Partial|Filled|Cancelled) == false

#80 / #81
  map 0/1/2/4/8/A + unknown
  Apply(Filled, 39=1) stays Filled
  Apply(Rejected|Cancelled, anything) stays
  Partial + 39=0   -- today would FAIL (lock as characterization or skip until Apply is tightened)
  39=C             -- today unknown; skip until map matches A70

#67
  Next(same args) == Next(same args)     -- name already claims this
  empty / whitespace intent throws
  sequence < 0 throws
  compact hyphen-strip + 16-char clip
  characterization: current shape is TI+clock+D4+compact (A42 From(uuid) still MISSING)

#55
  age == max is not expired
  helper vs CopyIntent.ExpiresAt (two clauses)
  do not claim A53 backlog until a send path exists

#68
  still MISSING — write a pure key function test; this file must not be renamed to cover it
```

Do **not** un-skip A43 ounces facts from this class. That is D18 / `SourceDestinationQuantityConversionTests`.

---

## 8. Call graph (why 6/6 cannot prove live safety)

```text
CopyIntent.ExpiresAt / IdempotencyKey
        │  nobody writes these from reconstruction (C59)
        ▼
CopyIntentExpiry.IsExpired          ← this file, 2 rows
        │  RiskEngine does not call it
        ▼
RiskEngine.Evaluate                 ← RiskEngineTests (5 reasons)
        │
        ▼
ExecutionIntent + ClOrdIdFactory.Next   ← this file only
        │
        ▼
ExecutionOrderStateMachine.AfterSend / MayRetry / Apply
        │  this file only
        ▼
FIX 35=D / 35=H / 35=AF / 35=AN     ← ABSENT
```

G09 unknown-state recovery remains **FAIL**. G12 no-blind-retry remains **helper PASS / system unproven**. G10 sizing remains **FAIL**.

---

## 9. Disposition

| Metric | Value |
|---|---|
| Product source changed | **No** |
| Test source changed | **No** |
| Subject SHA-256 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` |
| Facts in class | **6** |
| This-run pass / fail / skip | **6 / 0 / 0** |
| §60 #8 / #15 / #16 / #17 | PARTIAL / **MISSING** / PARTIAL / PARTIAL |
| A70 §14 locked | **2** (send→unknown+no-retry; disconnect status constant) |
| A89 dedicated exec classes present | **0** of #67 #69 #80 #81 #82 (`#47` lives elsewhere) |
| Classification | informal stand-in: `EXISTS` as smoke; contract: **INSUFFICIENT** |
| INDEX / SWARM_LOG rewritten | **No** (this file is the assigned artifact) |

**Do not claim “ExecutionAndSizingTests cover execution and sizing.”** Claim: one no-retry-after-send lock, one disconnect park, one FILL map, three last-stage qty numbers already owned by `Sizing/`, a ClOrdID prefix that contradicts A42, and an expiry predicate that is not copy-intent idempotency.
