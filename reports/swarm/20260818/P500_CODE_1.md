# P500_CODE_1 — RiskEngine vs copy-every-scored-XAU (RISK_BLOCKED book)

| Field | Value |
|---|---|
| Slot | **1** |
| Agent | P500_CODE_1 (senior trading-systems, this file only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| File | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| Angle | Would copying every scored XAU trader lose money given `RISK_BLOCKED` dominate the book? |
| Product source modified | **No.** This report is the only write. |
| Method | Full `read_file` of `RiskEngine.cs` (189 lines). Grep of `src/Domain/Risk` and `src/Domain` for `RISK_BLOCKED` / `TraderState` / `AllowFixSend` / `Evaluate(`. Read `BaselineScorer.FromBaseline`, `TraderState`, A71 G20, A23 `TRADER_RISK_BLOCKED`. **No** FIX send. **No** `NewOrderSingle`. No secrets printed. |
| Measured live (caller) | 8463 accounts; Achiever scoring; Starwave deals-done scored **0**; SHADOW all demo; `destinationRealPnl` **0**; FIX **LoggedOn**; `REAL_COPY` **false** |

Classification: **YES_WOULD_LOSE** as a live copy-all-scored policy. **SAFE_BY_ABSENCE** on Pepperstone dest today. Not a go-live PASS. Empty PASS is refused — the file was read; the strategy answer is not empty.

---

## Angle

Would **copying every scored XAU trader** lose money, given that `RISK_BLOCKED` dominates the scored book?

“Scored” here means a `TraderScore` / `BaselineScore` exists (Achiever scoring is live; Starwave deals-done scored **0** so those logins are not an XAU tape). `RISK_BLOCKED` is a **scoring state**, not a `RiskEngine` outcome.

---

## Verdict

**YES — if that policy were live, it would be expected to lose money.** `RISK_BLOCKED` dominance is a **warning**, not a filter this engine applies.

`RiskEngine.Evaluate` never sees `TraderState`. There is no request field for `RISK_BLOCKED` / `LIVE` / `SHADOW`. Spec reason `TRADER_RISK_BLOCKED` (A23 §4.3, A71 G20) is **not emitted**. Copy-every-scored therefore includes the modal scored class: martingale + drawdown + **negative NET** (or `risk >= 80`). Those are the books `FromBaseline` labels `RISK_BLOCKED`. Copying them is copying a losing, size-up-after-loss XAU cluster onto one dest gold book.

Martingale is blocked only if the **caller** sets `MartingaleFlag=true`. Caps (`MaxXauGross=20`, `MaxXauNet=10`, `MaxOpenPositions=20`) are last-ditch size brakes on caller-supplied snapshots, not a quality screen. `MAX_XAU_NET` is labeled `ReduceSize` but goes through `Reject()` → `ApprovedQuantity=0` (hard stop, not a clip). Side-blind `abs(net)+qty` treats a hedging add as more net. Loss/DD checks apply to **every** action, so a red dest day **cannot exit**.

**Current dest is not losing.** `AllowFixSend` requires `RealExecutionEnabled` (live `REAL_COPY false`). Product `Evaluate(` callers = this definition + five unit facts only. SHADOW is all-demo. `destinationRealPnl` is **0**. FIX LoggedOn is session, not send. Starwave scored **0** deals-done — those ~2k logins are not “scored XAU” yet.

Do **not** flip `REAL_COPY` and copy every Achiever score. That is the losing path this file fails to stop by state.

---

## Evidence quotes (file read 2026-08-18, 189 lines)

Identity-blind request: **no** `TraderState`, **no** `RISK_BLOCKED`, **no** `CompletedXauTrades`. `BrokerId` / `SourceLogin` are never read.

```32:56:D:\Prop\src\Domain\Risk\RiskEngine.cs
public sealed record RiskEvaluationRequest
{
    public required string CopyIntentId { get; init; }
    public required string BrokerId { get; init; }
    public required long SourceLogin { get; init; }
    public required CopyIntentAction Action { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal ExpectedPrice { get; init; }
    public required DateTimeOffset SourceEventTime { get; init; }
    public required DateTimeOffset DecisionTime { get; init; }
    public required DestinationQuote? Quote { get; init; }
    public required bool VenueHealthy { get; init; }
    public required bool RealExecutionEnabled { get; init; }
    public required bool Reconciled { get; init; }
    public required KillSwitchMode KillSwitch { get; init; }
    public required decimal TraderRealizedLoss { get; init; }
    public required decimal DailyExecutionPnl { get; init; }
    public required decimal PortfolioDrawdown { get; init; }
    public required decimal CurrentGrossXau { get; init; }
    public required decimal CurrentNetXau { get; init; }
    public required int OpenPositions { get; init; }
    public required decimal MarginUsage { get; init; }
    public required bool MartingaleFlag { get; init; }
    public required bool AbnormalSizing { get; init; }
}
```

XAU book caps are small vs 8463 source logins. Net check is side-blind and “reduce” is qty **0**:

```10:12:D:\Prop\src\Domain\Risk\RiskEngine.cs
    public decimal MaxXauGrossExposure { get; init; } = 20m;
    public decimal MaxXauNetExposure { get; init; } = 10m;
    public decimal MaxPositionQuantity { get; init; } = 5m;
```

```132:145:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.CurrentGrossXau + request.RequestedQuantity > _limits.MaxXauGrossExposure && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "MAX_XAU_GROSS");

        if (Math.Abs(request.CurrentNetXau) + request.RequestedQuantity > _limits.MaxXauNetExposure && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.ReduceSize, "MAX_XAU_NET");

        if (request.MarginUsage > _limits.MaxMarginUsage && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "MAX_MARGIN_USAGE");

        if (_limits.BlockMartingale && request.MartingaleFlag && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MARTINGALE_BLOCK");

        if (_limits.BlockAbnormalSizing && request.AbnormalSizing && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "ABNORMAL_SIZING_BLOCK");
```

```180:188:D:\Prop\src\Domain\Risk\RiskEngine.cs
    private static RiskDecision Reject(RiskEvaluationRequest request, RiskDecisionOutcome outcome, string reason) =>
        new()
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = outcome,
            ApprovedQuantity = 0,
            Reason = reason,
            AllowFixSend = false
        };
```

A copy-all-scored open that is not flagged martingale and is inside caps **APPROVE**s requested qty:

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        // ...
        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
```

Shadow/live flag is an empty `if` — send still requires the flag:

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

Red-day **freezes exits** (copy-all dest that starts losing cannot close through this engine):

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

What `RISK_BLOCKED` actually means (adjacent scorer; **not** consulted by this engine):

```194:195:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
```

Law this file violates if copy-all is attempted:

| Spec | Measured in `Evaluate` |
|---|---|
| A71 G20: `RISK_BLOCKED` blocks **new** copy | **MISSING** — no state field |
| A23 `TRADER_RISK_BLOCKED` | **never emitted** |
| A69: `RISK_BLOCKED` live NOS = **no** | Engine can `APPROVE` + `AllowFixSend=true` if caller sets `RealExecutionEnabled` and does not set `MartingaleFlag` |
| G21–G23: loss/DD must **not** trap dest exits | **FAIL** — lines 117–124 apply to close |
| G27: `MAX_XAU_NET` may `REDUCE_SIZE` to a positive stepped qty | **FAIL** — `ReduceSize` + qty `0` |

Grep:

| Probe | In `RiskEngine.cs` |
|---|---|
| `RISK_BLOCKED` / `TraderState` / `TRADER_RISK_BLOCKED` | **0** |
| `35=D` / `NewOrderSingle` | **0** |
| `Evaluate(` product callers | definition + `tests/Unit/RiskEngineTests.cs` only |

---

## Profit implication

**Copy-every-scored-XAU is not a profit path.** The scored class that is **blocked** is defined as a losing martingale (or risk ≥ 80). A book dominated by that class is a **negative-expectancy** source tape. Putting every Achiever score onto one Pepperstone/cTrader XAU dest would:

1. Align dest with correlated gold (one symbol; no Phase-2 concentration cap in this engine).
2. Include the `RISK_BLOCKED` majority as if they were `SHADOW` quality.
3. Size up after source losses whenever the caller forgets `MartingaleFlag`.
4. Hit 10 net / 20 gross lots of the **same** metal, then hard-zero further adds — leftover dest stays long/short the losing cluster.
5. On the first −500 / −2_000 / 3_000 dest day, **refuse to close**.

Starwave deals-done scored **0** adds **no** XAU edge (and no scored tape). Achiever scoring of thousands of logins is observation. FIX LoggedOn does not monetize scores. With `REAL_COPY false` and `destinationRealPnl` **0**, there is **no** live dest profit and **no** live dest loss from this file. Do not treat “8463 scored accounts” as 8463 copy candidates.

---

## Lower-loss implication

**Lower dest loss = do not copy every score. Filter `RISK_BLOCKED` (and `INSUFFICIENT_DATA` / `EARLY_SCORE`) before any intent.** Keep `REAL_COPY` false until a state-aware gate exists.

What this file already does that limits loss **if** a naive copy-all were wired with honest book snapshots:

- `AllowFixSend=false` when `RealExecutionEnabled=false` (today’s measured floor).
- `MARTINGALE_BLOCK` / `ABNORMAL_SIZING_BLOCK` if the caller actually sets the flags.
- Gross 20 / net 10 / 20 slots / 5 lots per ticket — a size ceiling, not a quality filter.
- `Reject` always `AllowFixSend=false`.

What this file does that **worsens** dest loss once a copy-all book exists:

- Ignores `RISK_BLOCKED` (the dominance signal).
- Freezes reduce/close at trader-loss / daily-loss / portfolio-DD.
- `MAX_XAU_NET` “reduce” is a zero-qty reject (no clip to remaining cap).
- No dest identity — cannot flatten what it approved.

Today’s dest loss floor is **absence of send**, not this gate. That matches measured `destinationRealPnl=0` and SHADOW-all-demo. Do **not** enable live copy of the scored book to “use” this engine.

---

## Binding one-liner

`RiskEngine` does not read `RISK_BLOCKED`. Copy-every-scored-XAU would copy martingale losers and is expected to lose if live. Dest capital is unharmed only because `REAL_COPY` is false and `Evaluate` is unwired.
