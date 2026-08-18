# D35 — `RiskEngineTests` re-read (5 green facts ≠ risk coverage)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D35_risk_tests.md` |
| Agent | D35 (risk unit-test re-read) |
| Date | 2026-08-18 |
| Assigned | Read `RiskEngineTests.cs`. Write this file. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| Tests read | `D:\Prop\tests\Unit\RiskEngineTests.cs` |
| Tests SHA-256 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` |
| Tests size | 2909 bytes / 86 lines / last write UTC 2026-08-18 07:47:42 |
| SUT read | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| SUT SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| SUT size | 8567 bytes / 189 lines / last write UTC 2026-08-18 07:38:10 |
| Adjacent read | `RiskDecisionOutcome.cs`, `CopyIntentAction.cs`, `KillSwitchMode.cs`, `RiskDecisionRecord.cs`, `CopyIntentExpiry.cs`, `ExecutionAndSizingTests.cs`, `DependencyInjection.cs`, `docs/risk.md` |
| Law | Architecture §31, §37, §39–§41, §60, §63–§64, §68, §70.11–13; `docs/risk.md`; A23, A27 §4.5, A48, A49, A71, A72, A89 #50–59 |
| Prior (same 5-fact file, not copied as verdict) | C03 (missing-case list), B13 (engine behavior), B36 (fixture design, not on disk), C33 (flatten/close adversarial) |
| Method | Full re-read of `RiskEngineTests.cs` and every `return` in `Evaluate`. Hash both files. Run `dotnet test --filter FullyQualifiedName~RiskEngineTests`. Count facts, asserted fields, actions, kill modes, outcomes, and reason strings. Grep product `*.cs` for `Evaluate(` / `IRiskEngine`. Nothing answered from memory. |

**Assigned answer:** `RiskEngineTests` is a **5-fact smoke class**. Measured `dotnet test` is **5 passed / 0 failed / 0 skipped**. That does **not** prove A23 / §60 / §64 / §68. The engine has **21** first-match reason paths; this file fully asserts **one** of them (`QUOTE_STALE` on Open). Product source was not modified.

---

## 0. Verdict

**FAIL as a risk-limits suite. PASS as five isolated smoke facts.**

| Check | Class | One-line |
|---|---|---|
| File exists | **YES** | `D:\Prop\tests\Unit\RiskEngineTests.cs` (not `tests/Unit/Risk/` — that folder is absent) |
| Facts execute | **5/5 green** | Measured 2026-08-18 13:36 local; 0.4022 s; exit 0 |
| A23 §11.3 isolation suite | **MISSING** | No theory; no one-reason-per-member |
| `AllowFixSend=true` | **UNTESTED** | Fixture pins `RealExecutionEnabled=false`; send-true conjunction never asserted |
| `IncreaseExposure` / `ReduceExposure` | **UNTESTED** | Only Open + one Close |
| `EmergencyFlatten` | **UNTESTED** | Kill coverage is StopNew on Open (outcome only) + Close approve |
| `ReduceSize` / `PauseTrader` / `PauseVenue` | **UNTESTED** | Outcomes seen: Approve, Reject, GlobalStop |
| Custom `RiskLimits` | **UNTESTED** | Always `new RiskEngine()` |
| Integration / send-path hook | **ABSENT** | `Evaluate` is called only from this class. DI does not register `RiskEngine` |
| A89 #50–59 named classes | **ABSENT** | Phantom inventory; do not cite `EXISTS` |
| Product source changed by D35 | **NO** | Report only |

**Do not claim** “risk engine unit tests pass” as a §68 box. Five facts pass. The **required** suite is absent.

**Do not claim** stale-quote / stale-signal / kill-switch “work.” Each is one interior Open (or Open+Close) case, and two of those facts check only `Reason`.

**Do not treat C03 as stale.** This re-read lands the same 5 facts / same SHA-sized file. D35 adds measured hashes + a live test run.

---

## 1. Measured run

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~RiskEngineTests
  --nologo --verbosity normal
```

| Item | Value |
|---|---|
| Host | `D:\Prop\tests\Unit\bin\Debug\net8.0\TraderIntelligence.Tests.Unit.dll` |
| Adapter | xUnit.net VSTest Adapter v2.5.3.1+6b60a9e56a / .NET 8.0.30 |
| Started | 2026-08-18 13:36:15 local |
| Result | **Test Run Successful.** Total **5**, Passed **5**, Failed **0**, Skipped **0**, 0.4022 s |
| Warnings / errors | **0 / 0** |
| Product files written | **none** |

| Display name | Result | Duration |
|---|---|---|
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_quote_rejects_open` | Passed | 53 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Real_flag_false_never_allows_fix_send` | Passed | < 1 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Stop_new_execution_blocks_opens_not_closes` | Passed | < 1 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Unreconciled_venue_blocks_new_exposure` | Passed | < 1 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_signal_rejected` | Passed | < 1 ms |

Green here means “these five assertions still match the stub.” It does **not** mean A23 is implemented.

---

## 2. What the test file actually is

One class, one field, five `[Fact]`s, one helper. **Zero** `[Theory]`. **Zero** `Skip`. **Zero** custom `RiskLimits`.

```7:9:D:\Prop\tests\Unit\RiskEngineTests.cs
public class RiskEngineTests
{
    private readonly RiskEngine _e = new();
```

`new RiskEngine()` → default `RiskLimits` only (`MaxQuoteAge=3s`, `MaxSourceSignalAge=15s`, `MaxAllowedSpread=2.0`, `MaxPriceMove=3.0`, `MaxLossPerTrader=500`, `MaxDailyExecutionLoss=2000`, `MaxPortfolioDrawdown=3000`, `MaxXauGross=20`, `MaxXauNet=10`, `MaxPositionQuantity=5`, `MaxOpenPositions=20`, `MaxMarginUsage=0.70`, both block flags true). Those numbers are **not pinned** by any fact.

### 2.1 `Base()` fixture (the only snapshot)

```57:86:D:\Prop\tests\Unit\RiskEngineTests.cs
    private static RiskEvaluationRequest Base(Func<RiskEvaluationRequest, RiskEvaluationRequest>? tweak = null)
    {
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var req = new RiskEvaluationRequest
        {
            CopyIntentId = "c1",
            BrokerId = "ACHIEVER",
            SourceLogin = 1,
            Action = CopyIntentAction.OpenExposure,
            RequestedQuantity = 0.10m,
            ExpectedPrice = 2400m,
            SourceEventTime = now.AddSeconds(-1),
            DecisionTime = now,
            Quote = new DestinationQuote("XAUUSD", "1", 2399.9m, 2400.1m, now.AddMilliseconds(-200), now),
            VenueHealthy = true,
            RealExecutionEnabled = false,
            Reconciled = true,
            KillSwitch = KillSwitchMode.None,
            TraderRealizedLoss = 0,
            DailyExecutionPnl = 0,
            PortfolioDrawdown = 0,
            CurrentGrossXau = 0,
            CurrentNetXau = 0,
            OpenPositions = 0,
            MarginUsage = 0.1m,
            MartingaleFlag = false,
            AbnormalSizing = false
        };
        return tweak is null ? req : tweak(req);
    }
```

Interior point. Never on a limit. Never `Increase`/`Reduce`. Never `RealExecutionEnabled=true`. Never `EmergencyFlatten`. Never `Quote=null`. Spread is 0.2 (well under 2.0). Mid is 2400.0 (exactly `ExpectedPrice`). Quote age 200 ms. Signal age 1 s.

`BrokerId` / `SourceLogin` / `CopyIntentId` are written and then **never asserted**. `VenueTimestamp` is set to `now` and **never read** by the engine.

---

## 3. Fact-by-fact

### 3.1 `Stale_quote_rejects_open` — strongest fact

```11:18:D:\Prop\tests\Unit\RiskEngineTests.cs
    [Fact]
    public void Stale_quote_rejects_open()
    {
        var d = _e.Evaluate(Base(q => q with { Quote = q.Quote! with { ReceivedAt = q.DecisionTime.AddSeconds(-30) } }));
        d.Outcome.Should().Be(RiskDecisionOutcome.Reject);
        d.Reason.Should().Be("QUOTE_STALE");
        d.AllowFixSend.Should().BeFalse();
    }
```

| | |
|---|---|
| Tweak | `ReceivedAt = DecisionTime - 30s` (10× default `MaxQuoteAge`) |
| Asserts | `Outcome=Reject`, `Reason=QUOTE_STALE`, `AllowFixSend=false` |
| Proves | Far-past receive-age on **Open** hits the stale branch |
| Does not prove | `age == 3.000s` vs `age > 3s`; Increase; reduce/close exemption; `Quote=null`; `VenueTimestamp` stale while receive fresh; custom `MaxQuoteAge`; `ApprovedQuantity=0`; `CopyIntentId` echo |

This is the **only** fact that asserts Outcome + Reason + AllowFixSend together. Still not a dual-clock / boundary lock (A72, A23 §6.1).

### 3.2 `Real_flag_false_never_allows_fix_send` — necessary, not sufficient

```20:26:D:\Prop\tests\Unit\RiskEngineTests.cs
    [Fact]
    public void Real_flag_false_never_allows_fix_send()
    {
        var d = _e.Evaluate(Base());
        d.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        d.AllowFixSend.Should().BeFalse();
    }
```

| | |
|---|---|
| Tweak | none (`RealExecutionEnabled=false` is the default) |
| Asserts | `Outcome=Approve`, `AllowFixSend=false` |
| Proves | Default fixture + flag false → shadow-shaped approve |
| Does not prove | `Reason=APPROVED`; `ApprovedQuantity=0.10`; flag **true** conjunction; Close/Reduce with flag false; worker NOS refuse; empty engine `if` at lines 90–93 |

`docs/risk.md`: FIX send requires `REAL_COPY_EXECUTION_ENABLED=true` **and** `AllowFixSend`. This fact covers only the first half of that sentence, and only as a request bit, unbound from `CTraderFixOptions`.

A later change that hard-wires `AllowFixSend=false` **keeps this fact green**.

### 3.3 `Stop_new_execution_blocks_opens_not_closes` — worst lock-in

```28:41:D:\Prop\tests\Unit\RiskEngineTests.cs
    [Fact]
    public void Stop_new_execution_blocks_opens_not_closes()
    {
        var open = _e.Evaluate(Base(q => q with { KillSwitch = KillSwitchMode.StopNewExecution }));
        open.Outcome.Should().Be(RiskDecisionOutcome.GlobalStop);

        var close = _e.Evaluate(Base(q => q with
        {
            Action = CopyIntentAction.CloseExposure,
            KillSwitch = KillSwitchMode.StopNewExecution
        }));
        close.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        close.AllowFixSend.Should().BeFalse();
    }
```

| | |
|---|---|
| Tweaks | Open + `StopNewExecution`; Close + `StopNewExecution` |
| Asserts | Open `Outcome=GlobalStop` (**no reason**); Close `Approve` + `AllowFixSend=false` |
| Proves | Stop-new blocks Open **outcome**; Close is not `GlobalStop` |
| Does not prove | Reason `STOP_NEW_EXECUTION`; Increase; Reduce; flatten distinctness; dest book unchanged |

The Close `AllowFixSend=false` assertion **locks the current send defect** (engine requires `KillSwitch == None` for every family). A48 / A71: approved CLOSE under stop-new **must** be allowed to send when the rest of the close conjunction holds. Implementing that spec **breaks this fact**.

Do not treat this as “kill switch tested” (§68 / A100 G16).

### 3.4 `Unreconciled_venue_blocks_new_exposure` — Reason only

```43:48:D:\Prop\tests\Unit\RiskEngineTests.cs
    [Fact]
    public void Unreconciled_venue_blocks_new_exposure()
    {
        var d = _e.Evaluate(Base(q => q with { Reconciled = false }));
        d.Reason.Should().Be("VENUE_NOT_RECONCILED");
    }
```

| | |
|---|---|
| Tweak | `Reconciled=false` (Open) |
| Asserts | `Reason` **only** |
| Proves | Open + unreconciliation emits that string |
| Does not prove | `Outcome=Reject`; `AllowFixSend=false`; qty 0; Increase; Close of known dest still `RISK_REDUCTION`; unknown-on-this-id |

Would stay green if the engine returned `Approve` + `VENUE_NOT_RECONCILED`, or `GlobalStop` with that reason.

### 3.5 `Stale_signal_rejected` — Reason only

```50:55:D:\Prop\tests\Unit\RiskEngineTests.cs
    [Fact]
    public void Stale_signal_rejected()
    {
        var d = _e.Evaluate(Base(q => q with { SourceEventTime = q.DecisionTime.AddMinutes(-5) }));
        d.Reason.Should().Be("SIGNAL_STALE");
    }
```

| | |
|---|---|
| Tweak | `SourceEventTime = DecisionTime - 5min` (20× default 15 s) |
| Asserts | `Reason` **only** |
| Proves | 5 min ≫ 15 s on Open emits that string |
| Does not prove | Outcome / send / qty; `age==15s` vs `>15s`; Increase; Close clock; `expires_at`; 20-intent catch-up |

`CopyIntentExpiry.IsExpired` is tested in `ExecutionAndSizingTests.Copy_intent_expires` (16 s vs 5 s against a 15 s span). `RiskEngine` **does not call** `CopyIntentExpiry`. That fact is not `SIGNAL_STALE` coverage.

### 3.6 Assertion quality matrix

| Fact | Outcome | Reason | ApprovedQuantity | AllowFixSend | CopyIntentId | Action |
|---|---|---|---|---|---|---|
| 1 stale quote | yes | yes | **no** | yes | **no** | Open |
| 2 real-flag false | yes | **no** | **no** | yes | **no** | Open |
| 3 open stop-new | yes | **no** | **no** | **no** | **no** | Open |
| 3 close stop-new | yes | **no** | **no** | yes (false) | **no** | Close |
| 4 unreconciliation | **no** | yes | **no** | **no** | **no** | Open |
| 5 stale signal | **no** | yes | **no** | **no** | **no** | Open |

Full-tuple facts: **0**. Near-full (Outcome+Reason+Send): **1**.

---

## 4. Engine surface vs tests

`Evaluate` is a first-blocking-check chain. Every `return` is a required unit case (A23 §5).

| # | Predicate (current code) | Outcome | Reason | Increasing-only? | Tested? |
|---:|---|---|---|---|---|
| 1 | `KillSwitch==StopNewExecution && IsIncreasing` | `GlobalStop` | `STOP_NEW_EXECUTION` | yes | **Partial** — Open outcome only; reason unasserted; Increase missing |
| 2 | `KillSwitch==EmergencyFlatten && IsIncreasing` | `GlobalStop` | `EMERGENCY_FLATTEN_BLOCKS_NEW` | yes | **Missing** |
| 3 | `!Reconciled && IsIncreasing` | `Reject` | `VENUE_NOT_RECONCILED` | yes | **Partial** — Reason only, Open only |
| 4 | `!VenueHealthy && IsIncreasing` | `PauseVenue` | `VENUE_UNHEALTHY` | yes | **Missing** |
| — | `RealExecutionEnabled==false && Action!=CloseExposure` | *(empty body)* | — | n/a | **Missing** (dead `if`) |
| 5 | `Quote is null && IsIncreasing` | `Reject` | `QUOTE_MISSING` | yes | **Missing** |
| 6 | `DecisionTime-ReceivedAt > MaxQuoteAge && IsIncreasing` | `Reject` | `QUOTE_STALE` | yes | **Partial** — 30 s Open only |
| 7 | `Ask-Bid > MaxAllowedSpread && IsIncreasing` | `Reject` | `SPREAD_TOO_WIDE` | yes | **Missing** |
| 8 | `\|mid-ExpectedPrice\| > MaxPriceMove && IsIncreasing` | `Reject` | `PRICE_MOVED_TOO_FAR` | yes | **Missing** |
| 9 | `DecisionTime-SourceEventTime > MaxSourceSignalAge && IsIncreasing` | `Reject` | `SIGNAL_STALE` | yes | **Partial** — 5 min Open, Reason only |
| 10 | `TraderRealizedLoss <= -MaxLossPerTrader` | `PauseTrader` | `MAX_LOSS_PER_TRADER` | **no — all actions** | **Missing** |
| 11 | `DailyExecutionPnl <= -MaxDailyExecutionLoss` | `GlobalStop` | `MAX_DAILY_EXECUTION_LOSS` | **no — all actions** | **Missing** |
| 12 | `PortfolioDrawdown >= MaxPortfolioDrawdown` | `GlobalStop` | `MAX_PORTFOLIO_DRAWDOWN` | **no — all actions** | **Missing** |
| 13 | `OpenPositions >= MaxOpenPositions && IsIncreasing` | `Reject` | `MAX_OPEN_POSITIONS` | yes | **Missing** |
| 14 | `RequestedQuantity > MaxPositionQuantity && IsIncreasing` | `Reject` | `MAX_POSITION_QUANTITY` | yes | **Missing** |
| 15 | `CurrentGrossXau + RequestedQuantity > MaxXauGross && IsIncreasing` | `Reject` | `MAX_XAU_GROSS` | yes | **Missing** |
| 16 | `\|CurrentNetXau\| + RequestedQuantity > MaxXauNet && IsIncreasing` | `ReduceSize` | `MAX_XAU_NET` | yes | **Missing** (only `ReduceSize` path) |
| 17 | `MarginUsage > MaxMarginUsage && IsIncreasing` | `Reject` | `MAX_MARGIN_USAGE` | yes | **Missing** |
| 18 | `BlockMartingale && MartingaleFlag && IsIncreasing` | `PauseTrader` | `MARTINGALE_BLOCK` | yes | **Missing** |
| 19 | `BlockAbnormalSizing && AbnormalSizing && IsIncreasing` | `Reject` | `ABNORMAL_SIZING_BLOCK` | yes | **Missing** |
| 20 | `IsReducing` fall-through | `Approve` | `RISK_REDUCTION` | reduce/close | **Partial** — Close under stop-new only; Reduce never; reason unasserted |
| 21 | else (increasing fall-through) | `Approve` | `APPROVED` | open/increase | **Partial** — Open + flag false; Increase never; reason unasserted |

`Reject(...)` always sets `ApprovedQuantity=0` and `AllowFixSend=false`. **No fact asserts that contract** except fact 1’s send bit.

`RiskLimits.MaxSlippage` is never read. `DestinationQuote.VenueTimestamp` is never read. No test documents either dead field.

### 4.1 Enum / flag coverage

| Dimension | Values | Touched by a fact |
|---|---|---|
| `CopyIntentAction` | Open, Increase, Reduce, Close | Open, Close. **Increase=0. Reduce=0.** |
| `KillSwitchMode` | None, StopNewExecution, EmergencyFlatten | None (implicit), StopNew. **Flatten=0.** |
| `RiskDecisionOutcome` | Approve, ReduceSize, Reject, PauseTrader, PauseVenue, GlobalStop | Approve, Reject, GlobalStop. **ReduceSize=0. PauseTrader=0. PauseVenue=0.** |
| `RealExecutionEnabled` | false / true | **false only** |
| `RiskEngine(RiskLimits)` | default / custom | **default only** |
| `AllowFixSend` | false / true | **false only** |

### 4.2 `AllowFixSend` conjunction (engine 147–150)

```text
allowSend = RealExecutionEnabled
         && KillSwitch == None
         && Reconciled
         && VenueHealthy
```

| Case | Current code | Tested? |
|---|---|---|
| All four true, Open | `Approve` + `AllowFixSend=true` + `APPROVED` | **Missing** — highest-value happy path |
| All four true, Close | `Approve` + `AllowFixSend=true` + `RISK_REDUCTION` | **Missing** |
| Flag false, rest true, Open | `Approve` + `AllowFixSend=false` | Fact 2 |
| Flag true, `StopNewExecution`, Close | `Approve` + `AllowFixSend=false` (current); spec A48 **true** | Fact 3 encodes **current**, not spec |
| Flag true, flatten, Close | `Approve` + `AllowFixSend=false` | **Missing** |

Without a fact that sets `RealExecutionEnabled=true` and expects `AllowFixSend=true`, the send bit can be hard-wired `false` and facts 1–5 still pass (fact 1 already expects false on reject).

---

## 5. Call sites (why green tests are not a gate)

`grep` of product `*.cs` for `Evaluate(` / `new RiskEngine` / `IRiskEngine`:

| Location | Hit |
|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | definition only |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | only caller |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | registers `TradeReconstructor`, `BaselineScorer`, ingestion — **not** `RiskEngine` |
| `apps/api`, `apps/fix-worker`, `apps/mt5-worker` | no `Evaluate` |
| `tests/Integration` | no `Evaluate` |

`RiskDecisionRecord` exists as an EF entity (`Outcome`, `ApprovedQuantity`, `Reason`, `AllowFixSend`). Nothing in this test class writes it. Nothing in Application maps `RiskDecision` → `RiskDecisionRecord`.

A green unit suite on a **dead path** cannot tick “risk-engine rejection happens before FIX send” (§70.11). There is no send function.

---

## 6. Adjacent files that are **not** this coverage

| Path | Why it does not count |
|---|---|
| `tests/Unit/ExecutionAndSizingTests.cs` `Copy_intent_expires` | `CopyIntentExpiry` helper; engine never calls it |
| `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` | Asserts `QuantityNormalizer` is unused by `RiskEngine` (that is a sizing fact, not a risk fact) |
| `tests/Integration/SeedingAndStoreTests.cs` | Store/seed; no `Evaluate` |
| A89 #50–59 class names | **Not on disk** (`RiskEngineHardLimitTests`, `OpenVsCloseExposurePolicyTests`, `QuoteFreshnessGuardTests`, `KillSwitchEmergencyFlattenTests`, `RiskEngineNetExposureReduceSizeTests`, …) |
| A27 §4.5 `tests/Unit/Risk/` | Folder **does not exist** |
| B36 fixture pack `RF-SQ-*` / `RF-SS-*` / `RF-KS-*` / `RF-RA-*` / `RF-RB-*` | Design only; no JSON / extra `[Fact]` landed |

---

## 7. What a later “keep tests green” change can hide

| Change | Facts that stay green | Why that is dangerous |
|---|---|---|
| Hard-wire `AllowFixSend=false` | 1, 2, 3, 4, 5 | Live conjunction never asserted |
| Drop `Reason` on recon / signal (keep any / empty) | 3 (no reason check); 4/5 fail only if the string changes | Weak |
| Treat Increase as reducing | All five | Increase never sent |
| Treat Reduce as increasing | All five | Reduce never sent |
| Delete flatten branch | All five | Flatten never sent |
| Return `ReduceSize` with qty 0 forever | All five | A71 G9 untested |
| Apply quote/spread/signal to Close | Fact 3 still passes (Close is not stale in that fact) | Family split unproven on market-quality guards |
| Apply loss/DD to Close (already true in SUT) | All five | B13-01 ships unchallenged |
| Change `MaxQuoteAge` 3s → 10s | Fact 1 still rejects at 30s | Threshold not pinned |
| Change `MaxQuoteAge` 3s → 60s | Fact 1 **would** fail | Only wide-margin defaults are accidentally pinned |
| Implement A48 “closes may send under stop-new” | Fact 3 **fails** | Worst lock-in: spec-correct fix is a red test |

---

## 8. Honest counts

| Metric | Count |
|---|---:|
| `[Fact]` in `RiskEngineTests` | **5** |
| `[Theory]` | **0** |
| Engine reason strings | **21** |
| Reasons with any assert | **3** (`QUOTE_STALE`, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE`). Implied `APPROVED` / `STOP_NEW_EXECUTION` / `RISK_REDUCTION` are **unasserted as strings**. |
| Reasons with Outcome+Reason+Send | **1** (`QUOTE_STALE`) |
| Actions exercised | **2 / 4** |
| Kill modes exercised | **2 / 3** |
| Outcomes exercised | **3 / 6** |
| `AllowFixSend=true` facts | **0** |
| Custom `RiskLimits` facts | **0** |
| `IncreaseExposure` facts | **0** |
| `ReduceExposure` facts | **0** |
| `EmergencyFlatten` facts | **0** |
| `ReduceSize` facts | **0** |
| `PauseTrader` / `PauseVenue` facts | **0** |
| Integration `Evaluate` calls | **0** |
| Product `Evaluate` call sites | **0** (dead path) |
| A89 risk classes on disk | **0 / 10** |
| Product source changed by D35 | **No** |

Approximate completeness vs A23 §11.3: **well under 20%** of required *cases*, even if every existing fact is counted at full credit (they are not).

---

## 9. Relation to prior reports

| File | Role vs D35 |
|---|---|
| C03 | Same 5-fact file; missing-case catalog (M01–M31, B01–B12, O01–O13). **Still current.** D35 does not replace C03. |
| B13 | Engine behavior review (kill / stale quote / REAL_COPY / family split). D35 is the **test** re-read, not a second B13. |
| B36 | Binding fixture design for the five go-live families. **Not implemented.** These 5 facts are a thin subset of `expect_stub` only. |
| C33 | Flatten/close adversarial on the SUT. Confirms **zero** `EmergencyFlatten` facts here. |
| A23 / A71 / A48 / A72 | Spec. Tests do not gold-file those matrices. |
| C17 | Unit vs §60: risk-limits area is PARTIAL at best. D35 agrees. |

---

## 10. Explicit non-claims

- Not “risk engine unit tests pass” as a go-live box. Five facts pass; the **suite required by §60 / §68 is absent**.
- Not “stale quote rejection works” beyond one interior Open case at 30 s.
- Not “stale signal rejection works” beyond a Reason string at 5 minutes.
- Not “kill switch tested.”
- Not “§64 Open vs Close policy tested.”
- Not “REAL_COPY gated.” Flag-false is touched; flag-true is not.
- Not a claim that A89 `EXISTS` rows are implemented.
- Not a claim that `RiskEngine` sits on a send path.
- No product source modified. No test source modified. No MQ5. No FIX send. No secrets.

---

## 11. Disposition

`RiskEngineTests` (SHA-256 `7B9523…2DF51`) is a **5-fact smoke file** over a **21-reason** first-match engine. Measured run: **5 passed**. Missing cases are not polish: the untested branches include every book cap, both behavioral blocks, flatten, venue-health, missing quote, spread, price-move, `ReduceSize`, both unused actions, the only `AllowFixSend=true` path, and the A71 red-day exit matrix.

**Next increment (not this task):** keep C03’s replacement shape; do not grow this smoke file into a false complete suite. Add a full-tuple helper; isolation theory per reason; 4-action family matrix; one send-true fact; rewrite fact 3 so it cannot greenwash B13-02; label CURRENT_STUB vs SPEC for B13-01/04. D35 does not implement those tests.

Product source was not modified.
