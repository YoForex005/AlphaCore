# P500_CODE_0 — BaselineScorer cannot emit live 35=D

| Field | Value |
|---|---|
| Slot | **0** |
| File | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| Angle | Does this file allow live `35=D` send that would put Pepperstone capital at risk? |
| Verdict | **PASS — NO.** File was read in full (212 lines). It scores reconstructed XAU trades and suggests `TraderState`. It does not build, serialize, or send FIX. It cannot place Pepperstone capital. |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Method | Full `read_file` of `BaselineScorer.cs`. Grep of this file for `35=D` / `NewOrderSingle` / `REAL_COPY` / `LoggedOn` / `destinationRealPnl`. Grep of `src` for `CanPromoteToLive`. Confirm `TraderState` enum tokens exist but are unreachable from this unit. |

**Honesty:** PASS is **SAFE_BY_ABSENCE** plus a hard-false promotion pin. This unit is not a send gate. FIX LoggedOn elsewhere does not become a send because of this file.

Measured live context (given, not re-measured here): 8463 accounts; Achiever scoring; Starwave deals-done scored 0; SHADOW all demo; `destinationRealPnl` 0; FIX LoggedOn; `REAL_COPY` false.

---

## Angle

Does `BaselineScorer.cs` allow a live FIX NewOrderSingle (`35=D`) that would put Pepperstone capital at risk?

---

## Verdict

**PASS — does not allow live 35=D send.**

Reachable work in this compilation unit:

1. Aggregate completed XAU features (`ComputeFeatures`).
2. Compute risk / behavior / early-quality numbers (`Score`).
3. Map those numbers to `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` (`TraderStateMachine.FromBaseline`).
4. Pin `AfterHighEarlyScore()` to `SHADOW`.
5. Pin `CanPromoteToLive(_) => false`.

No socket, QuickFIX session, ClOrdID, MsgType, quantity, symbol send, or destination account write exists in this file. `TraderState.LIVE` and `TraderState.LIVE_CANDIDATE` exist on the enum in another file; this unit never returns them.

---

## Evidence quotes

Empty-book feature floor (no orders, no PnL side effects):

```46:63:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
            return new FeatureSnapshot
            {
                CompletedXauTrades = 0,
                NetPnl = 0,
                GrossProfit = 0,
                GrossLoss = 0,
                ProfitFactor = 0,
                LotCv = 0,
                LossSizeCv = 0,
                Martingale = false,
                AveragingDown = false,
                LotEscalation = false,
                AverageHoldSeconds = 0,
                SlUseRate = 0,
                MaxDrawdown = 0,
                TradeFrequencyPerDay = 0
            };
```

Score output is a record of numbers + a suggested state — not a send:

```161:171:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var state = TraderStateMachine.FromBaseline(eligible, quality, risk, features);
        return new BaselineScore
        {
            Features = features,
            RiskScore = decimal.Round(risk, 2),
            BehaviorScore = decimal.Round(behavior, 2),
            EarlyQualityScore = quality,
            SuggestedState = state,
            EarlyScoreEligible = eligible
        };
```

Highest “good book” state is SHADOW, not LIVE:

```189:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;

        if (!earlyEligible)
            return TraderState.INSUFFICIENT_DATA;

        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;

        if (quality >= 55)
            return TraderState.WATCH;

        return TraderState.EARLY_SCORE;
    }

    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

Grep on this file: `35=D` = 0; `NewOrderSingle` = 0; `REAL_COPY` = 0; `LoggedOn` = 0; `destinationRealPnl` = 0. Only SHADOW token matches.

`TraderState` (other file) lists `LIVE_CANDIDATE = 4` and `LIVE = 5`. Those tokens are **not** in any `return` of this file.

---

## Profit implication

**None from this file.** It cannot open Pepperstone BUY/SELL, cannot copy Achiever/Starwave into REAL, and cannot turn FIX LoggedOn into a TRADE `35=D`. Destination realized PnL stays 0 unless some *other* process sends. With `REAL_COPY` false and SHADOW all-demo, scoring 8463 accounts (including Starwave at 0 deals-done) is observation only. Do not treat a high `EarlyQualityScore` as a profit path.

---

## Lower-loss implication

**Protects capital by never arming live.** Losing martingale/averaging books can be tagged `RISK_BLOCKED` / lower quality, but that is a state label, not a flatten or hedge. `CanPromoteToLive => false` blocks a scoring-driven live promotion even if quality is 100. That is a **vacuous lock** (live branch never written), not a tested refuse-path on a live socket. It still means **this file cannot lose Pepperstone money**. Loss reduction vs live copy remains “do not send,” not “size down.”

---

## Binding one-liner

`BaselineScorer.cs` is offline XAU scoring. No `35=D`. `CanPromoteToLive` is hard-false. Pepperstone capital is not at risk from this file.
