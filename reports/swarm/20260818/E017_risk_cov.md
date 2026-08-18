# E017 — `RiskEngineTests` fact list (5 green facts ≠ risk coverage)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E017_risk_cov.md` |
| Agent | E017 (risk unit-test fact inventory / coverage) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:49:20+05:30 (`DESKTOP-FQPFPKE` / `ADMIN`) |
| Assigned | List `RiskEngineTests` facts. Write this file. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| Tests read | `D:\Prop\tests\Unit\RiskEngineTests.cs` |
| Tests SHA-256 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` |
| Tests size | 2909 bytes / **87** raw lines / **78** non-blank / last write UTC 2026-08-18 07:47:42 |
| SUT read | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| SUT SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| SUT size | 8567 bytes / **189** raw lines / **156** non-blank / last write UTC 2026-08-18 07:38:10 |
| Adjacent read | `RiskDecisionOutcome.cs`, `CopyIntentAction.cs`, `KillSwitchMode.cs`, `ExecutionAndSizingTests.cs`, `DependencyInjection.cs`, `docs\risk.md` |
| Law | Architecture §31, §37, §39–§41, §60, §63–§64, §68, §70.11–13; `docs\risk.md`; A23 §5 / §11.3, A27 §4.5, A48, A49, A71, A72, A89 #50–59 |
| Prior (same 5-fact file) | C03 (missing-case catalog), C17 §6 (branch vs facts), C33 (flatten/close adversarial), D13 (engine recensus), D35 (test re-read + prior run), B36 (fixture design, not on disk) |
| Method | Full re-read of `RiskEngineTests.cs` and every `return` / reason string in `Evaluate`. SHA-256 both files. `dotnet test --filter FullyQualifiedName~RiskEngineTests`. Count `[Fact]`, `Should()`, actions, kill modes, outcomes, and asserted reason strings. Grep product `*.cs` for `Evaluate(` / `IRiskEngine` / `new RiskEngine`. Nothing answered from memory. |

**Assigned answer:** `RiskEngineTests` contains **exactly five** `[Fact]` methods and **zero** `[Theory]`. Measured `dotnet test` on this class is **5 passed / 0 failed / 0 skipped** (2026-08-18 13:49:20 +05:30, 0.3776 s, exit 0). That is a **smoke list**, not A23 / §60 / §64 / §68 coverage. The engine has **21** first-match reason paths; this file fully asserts **one** of them (`QUOTE_STALE` on `OpenExposure`). Product source was not modified.

---

## 0. Verdict

**FAIL as a risk-limits suite. PASS as five isolated smoke facts.**

| Check | Class | One-line |
|---|---|---|
| File exists | **YES** | `D:\Prop\tests\Unit\RiskEngineTests.cs` (not `tests/Unit/Risk/` — that folder is **absent**) |
| Facts on disk | **5** | listed in §2; no `[Theory]`, no `Skip`, no `InlineData` |
| Facts execute | **5/5 green** | Measured 13:49:20 +05:30; TRX `D:\Prop\reports\swarm\20260818\_tmp_e017\RiskEngineTests.trx` |
| `Should()` asserts | **10** | 3 + 2 + 3 + 1 + 1 across the five facts |
| A23 §11.3 isolation suite | **MISSING** | No one-reason-per-member theory |
| `AllowFixSend=true` | **UNTESTED** | Fixture pins `RealExecutionEnabled=false`; send-true conjunction never asserted |
| `IncreaseExposure` / `ReduceExposure` | **UNTESTED** | Only `OpenExposure` + one `CloseExposure` |
| `EmergencyFlatten` | **UNTESTED** | Kill coverage is `StopNewExecution` on Open (outcome only) + Close approve |
| `ReduceSize` / `PauseTrader` / `PauseVenue` | **UNTESTED** | Outcomes seen: `Approve`, `Reject`, `GlobalStop` |
| Custom `RiskLimits` | **UNTESTED** | Always `new RiskEngine()` |
| Integration / send-path hook | **ABSENT** | `Evaluate` is called only from this class. DI does not register `RiskEngine` |
| A89 #50–59 named classes | **ABSENT** | Phantom inventory; do not cite `EXISTS` as coverage |
| Product source changed by E017 | **NO** | Report only |

**Do not claim** “risk engine unit tests pass” as a §68 / A23 §11.3 box. Five facts pass. The **required** suite is absent.

**Do not claim** stale-quote / stale-signal / kill-switch “work.” Each is one interior Open (or Open+Close) case, and two of those facts check only `Reason`.

**Do not treat C03 / D35 as stale.** This re-read lands the same 5 facts / same SHA-256. E017 is the **fact list + live coverage matrix**, not a rewrite of those catalogs.

---

## 1. Measured identity

### 1.1 Files

| Path | Bytes | Raw lines | Non-blank | SHA-256 | LastWriteUtc |
|---|---:|---:|---:|---|---|
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 2909 | 87 | 78 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | 2026-08-18T07:47:42.9670731Z |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 8567 | 189 | 156 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | 2026-08-18T07:38:10.3112941Z |
| `D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs` | 206 | 11 | 10 | `A0753C0FAA97261E1E26717AB3E6465F30C9F2D9024A3FF3675B1377C7D26951` | 2026-08-18T07:34:04.4142550Z |
| `D:\Prop\src\Domain\Enums\CopyIntentAction.cs` | 182 | 9 | 8 | `94BA143D84459E2DB8C04E5E9199A4D548443A5C4BF99C015046E995E22C7AF6` | 2026-08-18T07:34:00.4506606Z |
| `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` | 140 | 8 | 7 | `528429B0DF8023E3DAB465BC6C8D1C025DCE651EA31E11A2E8FA68DDE8BFBC82` | 2026-08-18T07:36:08.5585637Z |

Same test SHA as D35 / D13 / C17. Same SUT SHA as D13 / D35. No byte drift since 07:47 UTC.

### 1.2 Live run (this agent)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~RiskEngineTests
  --nologo --verbosity normal
  --logger trx;LogFileName=D:\Prop\reports\swarm\20260818\_tmp_e017\RiskEngineTests.trx
```

| Item | Value |
|---|---|
| Host DLL | `D:\Prop\tests\Unit\bin\Debug\net8.0\TraderIntelligence.Tests.Unit.dll` |
| Adapter | xUnit.net VSTest Adapter v2.5.3.1+6b60a9e56a / .NET 8.0.30 |
| Computer | `DESKTOP-FQPFPKE` |
| TRX times | start `2026-08-18T13:49:20.3756511+05:30` / finish `2026-08-18T13:49:20.7670407+05:30` |
| Console | `Test Run Successful.` Total **5**, Passed **5**, Failed **0**, Skipped **0**, **0.3776 s** |
| TRX counters | total=5 executed=5 passed=5 failed=0 error=0 timeout=0 aborted=0 inconclusive=0 |
| Warnings / errors | **0 / 0** |
| `dotnet` exit | **0** |
| Product files written | **none** |

| Display name | Outcome | Duration |
|---|---|---|
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_quote_rejects_open` | Passed | 8.278 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Real_flag_false_never_allows_fix_send` | Passed | 0.322 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Stop_new_execution_blocks_opens_not_closes` | Passed | 0.144 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Unreconciled_venue_blocks_new_exposure` | Passed | 0.069 ms |
| `TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_signal_rejected` | Passed | 0.074 ms |

Green here means “these ten `Should()` still match the stub.” It does **not** mean A23 is implemented.

---

## 2. Complete fact list (the assignment)

One class, one field, five `[Fact]`s, one helper. **Zero** `[Theory]`. **Zero** `Skip`. **Zero** custom `RiskLimits`.

```7:9:D:\Prop\tests\Unit\RiskEngineTests.cs
public class RiskEngineTests
{
    private readonly RiskEngine _e = new();
```

`new RiskEngine()` → default `RiskLimits` only. Those numbers are **not pinned** by any fact.

### 2.0 Shared `Base()` fixture (every fact starts here)

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

| Field | Fixture value | Notes |
|---|---|---|
| `CopyIntentId` | `"c1"` | written; **never asserted** |
| `BrokerId` | `"ACHIEVER"` | unused by engine; **never asserted** |
| `SourceLogin` | `1` | unused by engine; **never asserted** |
| `Action` | `OpenExposure` | default; fact 3 close overrides |
| `RequestedQuantity` | `0.10` | interior; never on a cap; **never asserted** on the decision |
| `ExpectedPrice` | `2400` | mid of quote is exactly 2400 |
| `SourceEventTime` | `now - 1s` | 1 s ≪ default `MaxSourceSignalAge` 15 s |
| `DecisionTime` | `2026-08-18T12:00:00Z` | frozen |
| `Quote` | XAUUSD bid 2399.9 / ask 2400.1 / `ReceivedAt=now-200ms` / `VenueTimestamp=now` | spread 0.2 ≪ 2.0; age 200 ms ≪ 3 s; venue clock **unread** by engine |
| `VenueHealthy` | `true` | never flipped |
| `RealExecutionEnabled` | **`false`** | never flipped to `true` |
| `Reconciled` | `true` | fact 4 flips false |
| `KillSwitch` | `None` | fact 3 flips `StopNewExecution` |
| book / flags | zeros / false / margin 0.1 | never on a limit |

This is a **single interior point**. It never sits on a limit, never uses `Increase`/`Reduce`, never turns the real-copy flag on, never injects custom limits, never sets `Quote=null`, never sets `EmergencyFlatten`.

---

### Fact 1 — `Stale_quote_rejects_open` (strongest)

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
| FQN | `TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_quote_rejects_open` |
| Tweak | `Quote.ReceivedAt = DecisionTime - 30s` (10× default `MaxQuoteAge=3s`) |
| Action | `OpenExposure` (default) |
| Asserts (3) | `Outcome=Reject`, `Reason=QUOTE_STALE`, `AllowFixSend=false` |
| Measured | Passed, 8.278 ms |
| Proves | Far-past receive-age on **Open** hits the stale branch |
| Does not prove | `age == 3.000s` vs `age > 3s`; `Increase`; reduce/close exemption; `Quote=null` (`QUOTE_MISSING`); `VenueTimestamp` stale while receive fresh; custom `MaxQuoteAge`; `ApprovedQuantity=0`; `CopyIntentId` echo |

This is the **only** fact that asserts Outcome + Reason + AllowFixSend together. Still not a dual-clock / boundary lock (A72, A23 §6.1). Changing `MaxQuoteAge` 3 s → 10 s **keeps this green** (30 s still exceeds). Changing it to 60 s would fail.

---

### Fact 2 — `Real_flag_false_never_allows_fix_send` (necessary, not sufficient)

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
| FQN | `TraderIntelligence.Tests.Unit.RiskEngineTests.Real_flag_false_never_allows_fix_send` |
| Tweak | **none** (`RealExecutionEnabled=false` is the fixture default) |
| Action | `OpenExposure` |
| Asserts (2) | `Outcome=Approve`, `AllowFixSend=false` |
| Measured | Passed, 0.322 ms |
| Proves | Default fixture + flag false → shadow-shaped approve |
| Does not prove | `Reason=APPROVED`; `ApprovedQuantity=0.10`; flag **true** conjunction; Close/Reduce with flag false; worker NOS refuse; empty engine `if` at lines 90–93 |

`docs/risk.md` / A49: FIX send requires `REAL_COPY_EXECUTION_ENABLED=true` **and** `AllowFixSend`. This fact covers only the first half of that sentence, and only as a request bit, unbound from `CTraderFixOptions`.

A later change that hard-wires `AllowFixSend=false` **keeps this fact green**.

---

### Fact 3 — `Stop_new_execution_blocks_opens_not_closes` (worst lock-in)

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
| FQN | `TraderIntelligence.Tests.Unit.RiskEngineTests.Stop_new_execution_blocks_opens_not_closes` |
| Tweaks | (a) Open + `StopNewExecution`; (b) Close + `StopNewExecution` |
| Actions | `OpenExposure`, `CloseExposure` — the **only** Close in the file |
| Asserts (3) | Open `Outcome=GlobalStop` (**no reason**); Close `Outcome=Approve` + `AllowFixSend=false` |
| Measured | Passed, 0.144 ms |
| Proves | Stop-new blocks Open **outcome**; Close is not `GlobalStop` |
| Does not prove | Reason `STOP_NEW_EXECUTION`; `Increase`; `Reduce`; flatten distinctness; dest book unchanged; A48 send-under-stop-new |

The Close `AllowFixSend=false` assertion **locks the current send defect** (engine requires `KillSwitch == None` for every family). A48 / A71: approved CLOSE under stop-new **must** be allowed to send when the rest of the close conjunction holds. Implementing that spec **breaks this fact**.

Do not treat this as “kill switch tested” (§68 / A100 G16).

---

### Fact 4 — `Unreconciled_venue_blocks_new_exposure` (Reason only)

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
| FQN | `TraderIntelligence.Tests.Unit.RiskEngineTests.Unreconciled_venue_blocks_new_exposure` |
| Tweak | `Reconciled=false` |
| Action | `OpenExposure` |
| Asserts (1) | `Reason=VENUE_NOT_RECONCILED` **only** |
| Measured | Passed, 0.069 ms |
| Proves | Open + unreconciliation emits that string |
| Does not prove | `Outcome=Reject`; `AllowFixSend=false`; qty 0; `Increase`; Close of known dest still `RISK_REDUCTION`; unknown-on-this-id |

Would stay green if the engine returned `Approve` + `VENUE_NOT_RECONCILED`, or `GlobalStop` with that reason.

---

### Fact 5 — `Stale_signal_rejected` (Reason only)

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
| FQN | `TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_signal_rejected` |
| Tweak | `SourceEventTime = DecisionTime - 5min` (20× default 15 s) |
| Action | `OpenExposure` |
| Asserts (1) | `Reason=SIGNAL_STALE` **only** |
| Measured | Passed, 0.074 ms |
| Proves | 5 min ≫ 15 s on Open emits that string |
| Does not prove | Outcome / send / qty; `age==15s` vs `>15s`; `Increase`; Close clock; `expires_at`; 20-intent catch-up |

`CopyIntentExpiry.IsExpired` is tested in `ExecutionAndSizingTests.Copy_intent_expires` (16 s vs 5 s against a 15 s span). `RiskEngine` **does not call** `CopyIntentExpiry`. That fact is **not** `SIGNAL_STALE` coverage.

---

### 2.1 Assertion quality matrix

| # | Fact | Outcome | Reason | ApprovedQuantity | AllowFixSend | CopyIntentId | Action |
|---:|---|---|---|---|---|---|---|
| 1 | `Stale_quote_rejects_open` | yes (`Reject`) | yes (`QUOTE_STALE`) | **no** | yes (`false`) | **no** | Open |
| 2 | `Real_flag_false_never_allows_fix_send` | yes (`Approve`) | **no** | **no** | yes (`false`) | **no** | Open |
| 3a | `Stop_new_…` open | yes (`GlobalStop`) | **no** | **no** | **no** | **no** | Open |
| 3b | `Stop_new_…` close | yes (`Approve`) | **no** | **no** | yes (`false`) | **no** | Close |
| 4 | `Unreconciled_venue_blocks_new_exposure` | **no** | yes (`VENUE_NOT_RECONCILED`) | **no** | **no** | **no** | Open |
| 5 | `Stale_signal_rejected` | **no** | yes (`SIGNAL_STALE`) | **no** | **no** | **no** | Open |

| Metric | Count |
|---|---:|
| Full-tuple facts (Outcome+Reason+Qty+Send+Id) | **0** |
| Near-full (Outcome+Reason+Send) | **1** (fact 1) |
| Reason-only facts | **2** (facts 4, 5) |
| Outcome without Reason | **2** (fact 2; fact 3 open) |
| `AllowFixSend=true` expected | **0** |
| `ApprovedQuantity` asserted | **0** |
| `CopyIntentId` echoed | **0** |

---

## 3. Engine surface vs these five facts

`Evaluate` is a first-blocking-check chain. Every `return` is a required unit case (A23 §5, A89 #50).

| # | Predicate (current code) | Outcome | Reason | Increasing-only? | Covered by which fact? |
|---:|---|---|---|---|---|
| 1 | `KillSwitch==StopNewExecution && IsIncreasing` | `GlobalStop` | `STOP_NEW_EXECUTION` | yes | **Partial** — fact 3 open outcome only; reason unasserted; Increase missing |
| 2 | `KillSwitch==EmergencyFlatten && IsIncreasing` | `GlobalStop` | `EMERGENCY_FLATTEN_BLOCKS_NEW` | yes | **None** |
| 3 | `!Reconciled && IsIncreasing` | `Reject` | `VENUE_NOT_RECONCILED` | yes | **Partial** — fact 4 Reason only, Open only |
| 4 | `!VenueHealthy && IsIncreasing` | `PauseVenue` | `VENUE_UNHEALTHY` | yes | **None** |
| — | `RealExecutionEnabled==false && Action!=CloseExposure` | *(empty body)* | — | n/a | **None** (dead `if` at 90–93) |
| 5 | `Quote is null && IsIncreasing` | `Reject` | `QUOTE_MISSING` | yes | **None** |
| 6 | `DecisionTime-ReceivedAt > MaxQuoteAge && IsIncreasing` | `Reject` | `QUOTE_STALE` | yes | **Partial** — fact 1, 30 s Open only |
| 7 | `Ask-Bid > MaxAllowedSpread && IsIncreasing` | `Reject` | `SPREAD_TOO_WIDE` | yes | **None** |
| 8 | `\|mid-ExpectedPrice\| > MaxPriceMove && IsIncreasing` | `Reject` | `PRICE_MOVED_TOO_FAR` | yes | **None** |
| 9 | `DecisionTime-SourceEventTime > MaxSourceSignalAge && IsIncreasing` | `Reject` | `SIGNAL_STALE` | yes | **Partial** — fact 5, 5 min Open, Reason only |
| 10 | `TraderRealizedLoss <= -MaxLossPerTrader` | `PauseTrader` | `MAX_LOSS_PER_TRADER` | **no — all actions** | **None** |
| 11 | `DailyExecutionPnl <= -MaxDailyExecutionLoss` | `GlobalStop` | `MAX_DAILY_EXECUTION_LOSS` | **no — all actions** | **None** |
| 12 | `PortfolioDrawdown >= MaxPortfolioDrawdown` | `GlobalStop` | `MAX_PORTFOLIO_DRAWDOWN` | **no — all actions** | **None** |
| 13 | `OpenPositions >= MaxOpenPositions && IsIncreasing` | `Reject` | `MAX_OPEN_POSITIONS` | yes | **None** |
| 14 | `RequestedQuantity > MaxPositionQuantity && IsIncreasing` | `Reject` | `MAX_POSITION_QUANTITY` | yes | **None** |
| 15 | `CurrentGrossXau + RequestedQuantity > MaxXauGross && IsIncreasing` | `Reject` | `MAX_XAU_GROSS` | yes | **None** |
| 16 | `\|CurrentNetXau\| + RequestedQuantity > MaxXauNet && IsIncreasing` | `ReduceSize` | `MAX_XAU_NET` | yes | **None** (only `ReduceSize` path; helper still qty 0) |
| 17 | `MarginUsage > MaxMarginUsage && IsIncreasing` | `Reject` | `MAX_MARGIN_USAGE` | yes | **None** |
| 18 | `BlockMartingale && MartingaleFlag && IsIncreasing` | `PauseTrader` | `MARTINGALE_BLOCK` | yes | **None** |
| 19 | `BlockAbnormalSizing && AbnormalSizing && IsIncreasing` | `Reject` | `ABNORMAL_SIZING_BLOCK` | yes | **None** |
| 20 | `IsReducing` fall-through | `Approve` | `RISK_REDUCTION` | reduce/close | **Partial** — fact 3 close; Reduce never; reason unasserted |
| 21 | else (increasing fall-through) | `Approve` | `APPROVED` | open/increase | **Partial** — fact 2 Open + flag false; Increase never; reason unasserted |

`Reject(...)` always sets `ApprovedQuantity=0` and `AllowFixSend=false`. **No fact asserts that contract** except fact 1’s send bit.

`RiskLimits.MaxSlippage` is never read. `DestinationQuote.VenueTimestamp` is never read. No test documents either dead field.

### 3.1 Enum / flag coverage

| Dimension | Values | Touched by a fact |
|---|---|---|
| `CopyIntentAction` | Open, Increase, Reduce, Close | Open (facts 1–5), Close (fact 3). **Increase=0. Reduce=0.** |
| `KillSwitchMode` | None, StopNewExecution, EmergencyFlatten | None (implicit), StopNew (fact 3). **Flatten=0.** |
| `RiskDecisionOutcome` | Approve, ReduceSize, Reject, PauseTrader, PauseVenue, GlobalStop | Approve (2, 3b), Reject (1), GlobalStop (3a). **ReduceSize=0. PauseTrader=0. PauseVenue=0.** |
| `RealExecutionEnabled` | false / true | **false only** |
| `RiskEngine(RiskLimits)` | default / custom | **default only** |
| `AllowFixSend` | false / true | **false only** (facts 1, 2, 3b) |

### 3.2 `AllowFixSend` conjunction (engine 147–150)

```text
allowSend = RealExecutionEnabled
         && KillSwitch == None
         && Reconciled
         && VenueHealthy
```

| Case | Current code | Fact? |
|---|---|---|
| All four true, Open | `Approve` + `AllowFixSend=true` + `APPROVED` | **Missing** — highest-value happy path |
| All four true, Close | `Approve` + `AllowFixSend=true` + `RISK_REDUCTION` | **Missing** |
| Flag false, rest true, Open | `Approve` + `AllowFixSend=false` | Fact 2 |
| Flag true, `StopNewExecution`, Close | `Approve` + `AllowFixSend=false` (current); spec A48 **true** | Fact 3 encodes **current**, not spec |
| Flag true, flatten, Close | `Approve` + `AllowFixSend=false` | **Missing** |

Without a fact that sets `RealExecutionEnabled=true` and expects `AllowFixSend=true`, the send bit can be hard-wired `false` and facts 1–5 still pass (fact 1 already expects false on reject).

---

## 4. What these five facts do **not** cover (honest)

This is a **coverage remainder**, not a second C03. C03 still owns the M01–M31 / B01–B12 / O01–O13 catalog.

### 4.1 Reason strings with **zero** facts (15 of 21)

```text
EMERGENCY_FLATTEN_BLOCKS_NEW
VENUE_UNHEALTHY
QUOTE_MISSING
SPREAD_TOO_WIDE
PRICE_MOVED_TOO_FAR
MAX_LOSS_PER_TRADER
MAX_DAILY_EXECUTION_LOSS
MAX_PORTFOLIO_DRAWDOWN
MAX_OPEN_POSITIONS
MAX_POSITION_QUANTITY
MAX_XAU_GROSS
MAX_XAU_NET
MAX_MARGIN_USAGE
MARTINGALE_BLOCK
ABNORMAL_SIZING_BLOCK
```

Plus two approve strings that are **emitted but never asserted**: `STOP_NEW_EXECUTION`, `RISK_REDUCTION`, `APPROVED`. (Fact 3 open / fact 3 close / fact 2 respectively.)

### 4.2 A23 §11.3 required cases vs this file

| A23 §11.3 required | This file |
|---|---|
| each hard limit in isolation | **No** — 0 isolation theory |
| stale quote reject (`quote_age > max`) | **Partial** — 30 s Open only; operator `>` unpinned |
| stale signal reject and `expires_at` | **Partial** — Reason string only; no `expires_at` |
| no blind 20-intent catch-up after 3-minute FIX gap | **No** |
| `PRICE_MOVED_TOO_FAR` / `SPREAD_TOO_WIDE` | **No** |
| source lots ≠ destination qty | **Not this class** (and `QuantityNormalizer` is unused by `Evaluate`) |
| `REDUCE_SIZE` vs hard `REJECT` when below min | **No** (`MAX_XAU_NET` untested; qty 0 if hit) |
| `OPEN`/`INCREASE` stricter than `REDUCE`/`CLOSE` | **Partial** — one Close under stop-new; Increase/Reduce never |
| `STOP_NEW_EXECUTION` does not flatten | **Not proven** — flatten never sent |
| `EMERGENCY_FLATTEN` permission is distinct | **No** |
| reconciliation / unknown-state blocks send | **Partial** — Reason string; no unknown-on-this-id |
| risk rejection occurs with **zero** FIX outbound | **No** — no worker / harness / send probe |

### 4.3 §68 / A100 boxes these facts **cannot** tick

```text
[ ] risk engine unit/integration tests pass     -- 5 smoke facts ≠ the suite
[ ] stale quote rejection works                 -- one interior Open at 30 s
[ ] stale signal rejection works                -- Reason string at 5 min
[ ] kill switch tested                          -- Open outcome + Close send=false lock-in
[ ] risk-engine rejection happens before FIX send
[ ] global stop-new-orders works
[ ] reconciliation blocks execution while inconsistent
```

### 4.4 Phantom A89 / A27 inventory (do not cite as coverage)

A89 §5.4 marks these `EXISTS`. **Measured on disk 2026-08-18 13:49: `tests/Unit/Risk/` does not exist.** Only `RiskEngineTests.cs` is a `RiskEngine` caller.

| A89 # | Claimed class | Disk | What these 5 facts actually cover of its “must prove” |
|---:|---|---|---|
| 50 | `RiskEngineHardLimitTests` | **No** | 3 of ~19 reasons, none isolated with full asserts |
| 51 | `RiskEngineApproveReduceRejectTests` | **No** | Approve+Reject+GlobalStop seen; ReduceSize/PauseTrader/PauseVenue never; send-true never |
| 52 | `OpenVsCloseExposurePolicyTests` | **No** | One Close under stop-new |
| 53 | `QuoteFreshnessGuardTests` | **No** | One 30 s Open; no `==max` boundary; no missing quote |
| 54 | `PriceMoveAndSpreadGuardTests` | **No** | **Zero** |
| 55 | `StaleCopyIntentExpiryTests` | **No** (expiry helper tested elsewhere) | One 5 min Open reason |
| 56 | `KillSwitchStopNewExecutionTests` | **No** | One Open outcome + one Close approve |
| 57 | `KillSwitchEmergencyFlattenTests` | **No** | **Zero** |
| 58 | `RealExecutionFeatureFlagTests` | **No** | Flag false → no send; flag true untested; options unbound |
| 59 | `RiskEngineNetExposureReduceSizeTests` | **No** | **Zero** |

A27 §4.5 names the same cluster under `tests/Unit/Risk/`. Folder **does not exist**. File lives at `tests/Unit/RiskEngineTests.cs`.

**Do not cite A89 status `EXISTS` as evidence these cases are locked.**

---

## 5. Call sites (why green tests are not a gate)

`grep` of product `*.cs` for `Evaluate(` / `new RiskEngine` / `IRiskEngine`:

| Location | Hit |
|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs:76` | definition only |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | **only caller** (6 `Evaluate` invocations across 5 facts) |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | registers `TradeReconstructor`, `BaselineScorer`, ingestion — **not** `RiskEngine` |
| `apps/api`, `apps/fix-worker`, `apps/mt5-worker` | no `Evaluate` |
| `tests/Integration` | no `Evaluate` |

A green unit suite on a **dead path** cannot tick “risk-engine rejection happens before FIX send” (§70.11). There is no send function.

---

## 6. Adjacent files that are **not** this coverage

| Path | Why it does not count |
|---|---|
| `tests/Unit/ExecutionAndSizingTests.cs` `Copy_intent_expires` | `CopyIntentExpiry` helper; engine never calls it |
| `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` | Sizing converter; unused by `Evaluate` |
| `tests/Unit/Sizing/QuantityNormalizerStepMinMaxTests.cs` | Same |
| `tests/Integration/SeedingAndStoreTests.cs` | Store/seed; no `Evaluate` |
| A89 #50–59 class names | **Not on disk** |
| A27 §4.5 `tests/Unit/Risk/` | Folder **does not exist** |
| B36 fixture pack `RF-SQ-*` / `RF-SS-*` / `RF-KS-*` / `RF-RA-*` / `RF-RB-*` | Design only; no JSON / extra `[Fact]` landed |

`RiskEngineTests.cs` is the **only** `*Risk*.cs` under `D:\Prop\tests`.

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
| Apply loss/DD to Close (already true in SUT) | All five | B13-01 / D13 exit-freeze ships unchallenged |
| Change `MaxQuoteAge` 3s → 10s | Fact 1 still rejects at 30s | Threshold not pinned |
| Change `MaxQuoteAge` 3s → 60s | Fact 1 **would** fail | Only wide-margin defaults are accidentally pinned |
| Implement A48 “closes may send under stop-new” | Fact 3 **fails** | Worst lock-in: spec-correct fix is a red test |

---

## 8. Honest counts

| Metric | Count |
|---|---:|
| `[Fact]` in `RiskEngineTests` | **5** |
| `[Theory]` | **0** |
| `Should()` asserts | **10** |
| `Evaluate(` invocations in the file | **6** (fact 3 calls twice) |
| Engine first-match reason paths | **21** |
| Reasons with any string assert | **3** (`QUOTE_STALE`, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE`) |
| Reasons implied but unasserted as strings | **3** (`APPROVED`, `STOP_NEW_EXECUTION`, `RISK_REDUCTION`) |
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
| `tests/Unit/Risk/` | **does not exist** |
| Product source changed by E017 | **No** |

Approximate completeness vs A23 §11.3: **well under 20%** of required *cases*, even if every existing fact is counted at full credit (they are not).

---

## 9. Relation to prior reports

| File | Role vs E017 |
|---|---|
| **This file** | Fact-by-fact inventory + measured 13:49 run + coverage remainder |
| C03 | Same 5-fact file; missing-case catalog (M01–M31, B01–B12, O01–O13). **Still current.** E017 does not replace C03. |
| D35 | Prior re-read + 13:36 run. Same SHA. E017 re-ran and listed facts as the assigned deliverable. |
| C17 §6 | Branch-vs-fact table. Agrees: 5 facts, ~14 reasons with zero. |
| B13 / D13 | Engine **behavior** reviews. E017 is the **test** fact list, not a second engine review. |
| C33 | Flatten/close adversarial on the SUT. Confirms **zero** `EmergencyFlatten` facts here. |
| B36 | Binding fixture design for five go-live families. **Not implemented.** These 5 facts are a thin subset of `expect_stub` only. |
| A23 / A71 / A48 / A72 | Spec. Tests do not gold-file those matrices. |

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

`RiskEngineTests` (SHA-256 `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51`) is a **5-fact smoke class**:

1. `Stale_quote_rejects_open`
2. `Real_flag_false_never_allows_fix_send`
3. `Stop_new_execution_blocks_opens_not_closes`
4. `Unreconciled_venue_blocks_new_exposure`
5. `Stale_signal_rejected`

Measured run: **5 passed / 0 failed / 0 skipped**. The engine has **21** first-match reasons; this file fully asserts **one**. Missing cases are not polish: the untested branches include every book cap, both behavioral blocks, flatten, venue-health, missing quote, spread, price-move, `ReduceSize`, both unused actions, the only `AllowFixSend=true` path, and the A71 red-day exit matrix.

**Next increment (not this task):** keep C03’s replacement shape; do not grow this smoke file into a false complete suite. Add a full-tuple helper; isolation theory per reason; 4-action family matrix; one send-true fact; rewrite fact 3 so it cannot greenwash the stop-new send defect; label CURRENT_STUB vs SPEC for loss/DD-on-close. E017 does not implement those tests.

Product source was not modified.
