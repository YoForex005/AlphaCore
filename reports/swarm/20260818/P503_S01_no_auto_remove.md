# P503_S01 — admitted trader later martingale/negative: skip new opens only; dest copies not flattened

**SUPERSEDED same day.** `CopyRosterEngine` + `TickRosterAsync` now **remove** and emit dest `FLATTEN_LOSS_CUT` closes. Pin: `P503_AUTO_ROSTER.md`. This file is the **pre-fix** hole.

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P503_S01_no_auto_remove.md` |
| Slot | **P503_S01** |
| Date | 2026-08-18 |
| Product source edited | **No** (report only) |
| Assigned reads | `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` (exists at assigned Domain path) |
| Assigned `CopyTradingService.cs` | **Not** under Domain. Actual path: `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` |
| Adjacent reads | `CopyTradingHostedService.cs`, `RiskEngine.cs`, `XauUsdOneToOneCopyPolicyTests.cs`, `KillSwitchMode.cs`, `docs/risk.md` |
| Claim under test | A trader who is **admitted** and later goes **martingale** or **negative XAU book** is only skipped for **NEW opens**. Existing dest copies are **not** flattened / auto-removed. |
| Verdict | **CONFIRMED.** Eligibility failure is a `continue` / `Reject` on the next Evaluate. There is **no** dest flatten, dest position table, or auto-remove on the product copy hop. |
| Tests wrong? | **No.** Tests assert the *skip* (`Martingale_trader_blocked`, `Negative_xau_pnl_blocked`). They do **not** assert dest flatten. Do not change product or tests. |
| Secrets | None. |

Empty PASS is forbidden. Both assigned types were opened in full.

---

## 0. Verdict in one paragraph

Today’s copy hop **re-checks** `IsTraderEligible` on every 20 s tick. If a previously admitted SHADOW/LIVE name later has `Martingale` / `AveragingDown` / `LotEscalation` or `XauNetPnl <= 0`, the service **`continue`s the whole trader**. That drops **new** `OpenExposure` intents. It does **not** emit `CloseExposure` against existing dest copies, does **not** call a flatten sender, and does **not** delete prior `CopyIntent` / `ShadowOrder` rows. `RiskEngine` martingale and `EmergencyFlatten` are also **open-family only** (`IsIncreasing`). Architecture / `docs/risk.md` describe dest flatten as a separate permission; product **does not implement** that path.

**CONFIRMED: no auto-remove of dest copies when an admitted trader later fails the size-pattern or XAU-PnL gate.**

---

## 1. Admission vs later failure (policy)

Eligibility is **recomputed from the current snapshot**. There is no “once admitted, stay copied” flag. The same function both admits and later ejects.

```73:115:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
    public bool IsTraderEligible(CopyTraderSnapshot trader, out string reason)
    {
        if (trader.State is TraderState.RISK_BLOCKED or TraderState.DISQUALIFIED or TraderState.PAUSED)
        {
            reason = "TRADER_BLOCKED_" + trader.State;
            return false;
        }
        // ...
        if (trader.Martingale || trader.AveragingDown || trader.LotEscalation)
        {
            reason = "TRADER_SIZE_PATTERN_BLOCK";
            return false;
        }
        // ...
        if (trader.XauNetPnl <= 0)
        {
            reason = "XAU_BOOK_NOT_PROFITABLE";
            return false;
        }
        // ...
        reason = "TRADER_ELIGIBLE";
        return true;
    }
```

`Evaluate` applies that gate to **every** signal, including close:

```117:120:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
    public CopyInstruction Evaluate(CopyTraderSnapshot trader, CopySignal signal)
    {
        if (!IsTraderEligible(trader, out var traderReason))
            return Reject(traderReason);
```

So a later-martingale / later-negative snapshot cannot produce an accepted instruction of **any** action through this policy. That is a **reject**, not a dest flatten.

Unit tests only cover the reject, not dest unwind:

```75:86:D:\Prop\tests\Unit\XauUsdOneToOneCopyPolicyTests.cs
    public void Martingale_trader_blocked()
    {
        _p.IsTraderEligible(GoodTrader() with { Martingale = true }, out var reason).Should().BeFalse();
        reason.Should().Be("TRADER_SIZE_PATTERN_BLOCK");
    }

    public void Negative_xau_pnl_blocked()
    {
        _p.IsTraderEligible(GoodTrader() with { XauNetPnl = -10m }, out var reason).Should().BeFalse();
        reason.Should().Be("XAU_BOOK_NOT_PROFITABLE");
    }
```

---

## 2. Service hop: skip trader, do not flatten dest

`GenerateShadowIntentsAsync` is the only production consumer. Snapshot is rebuilt from **current** `TraderScore` flags + completed XAU `NetRealizedPnl`. Then:

```110:127:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
            var snapshot = new CopyTraderSnapshot
            {
                State = score.CurrentState,
                CompletedXauTrades = score.CompletedXauTrades,
                XauNetPnl = xau.Where(t => t.Completed).Sum(t => t.NetRealizedPnl),
                Martingale = score.Martingale,
                AveragingDown = score.AveragingDown,
                LotEscalation = score.LotEscalation,
                GroupName = account?.GroupName
            };
            if (!_policy.IsTraderEligible(snapshot, out _))
                continue;

            foreach (var trade in xau.Where(t => !t.Completed))
            {
                var key = $"copy:{score.BrokerId}:{score.Login}:{trade.PositionId}:open";
                if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == key, ct))
                    continue;
```

`continue` is the **entire trader** — not “skip this open but flatten dest.” Effects:

| After later martingale / XAU book ≤ 0 | Happens? |
|---|---|
| New open intents for still-open source tickets | **No** (`continue` before the open loop) |
| Duplicate open for a ticket that already has `…:open` | **No** (idempotency; also skipped) |
| Auto `CloseExposure` of dest copies because eligibility died | **No** — close loop is **after** the same `continue` |
| Delete / cancel prior `CopyIntent` / `ShadowOrder` | **No** writer |
| FIX dest flatten / `35=D` close | **No** (`NewOrderSingleImplemented = false`) |

The close loop only fires when the **source** reconstructed trade is completed **and** the trader is **still** eligible **and** an open idempotency key already exists:

```253:274:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
            foreach (var trade in xau.Where(t => t.Completed && t.ClosedAt.HasValue))
            {
                var openKey = $"copy:{score.BrokerId}:{score.Login}:{trade.PositionId}:open";
                var closeKey = $"copy:{score.BrokerId}:{score.Login}:{trade.PositionId}:close";
                if (!await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == openKey, ct))
                    continue;
                if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == closeKey, ct))
                    continue;

                var close = _policy.Evaluate(snapshot, new CopySignal
                {
                    // ...
                    Action = CopyIntentAction.CloseExposure,
                    // ...
                    SourceStillOpen = false
                });
                if (!close.Accept)
                    continue;
```

That is **source-close copy**, not **eligibility-loss flatten**. If the trader is already ineligible, this block is never reached. Residual dest / shadow exposure is left as-is.

Hosted poll (20 s) only calls `GenerateShadowIntentsAsync`. No flatten tick:

```26:31:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                if (n > 0)
                    _log.LogInformation("Copy pipeline created {Count} SHADOW intents. Live NewOrderSingle still blocked.", n);
```

Grep of `D:\Prop\src` for `DestinationPosition`, dest-position auto-remove, or eligibility flatten: **0** product types. Flatten helpers exist only on the **demo CLI** (`CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix`), off the copy hop.

---

## 3. Risk engine also does not dest-flatten on martingale

Even if a later-martingale open slipped past the policy (it cannot today: service `continue`s first), `RiskEngine` only **rejects new increasing** actions:

```141:145:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (_limits.BlockMartingale && request.MartingaleFlag && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MARTINGALE_BLOCK");

        if (_limits.BlockAbnormalSizing && request.AbnormalSizing && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "ABNORMAL_SIZING_BLOCK");
```

`IsIncreasing` is `OpenExposure` / `IncreaseExposure` only. Kill-switch “flatten” is the same shape: **block new**, do not send dest closes.

```78:82:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");
```

Copy hop hard-codes `KillSwitch = KillSwitchMode.None` and persist `AllowFixSend = false`. Architecture law (A71 / v2 §40): *`STOP_NEW_EXECUTION` blocks new copy; leaves dest book untouched. Flatten is `CLOSE_EXPOSURE` under a separate permission.* Product `EmergencyFlatten` enum exists; dest flatten **run** does not.

`docs/risk.md` describes operator Emergency Flatten as “closes all open positions immediately at market.” That document is **not** wired to `CopyTradingService`.

---

## 4. Confirmation matrix

| Behavior | Evidence | Result |
|---|---|---|
| Later martingale / averaging / lot-escalation blocks **new** copy | `IsTraderEligible` → `TRADER_SIZE_PATTERN_BLOCK`; service `continue` | **Yes** |
| Later XAU book ≤ 0 blocks **new** copy | `XauNetPnl <= 0` → `XAU_BOOK_NOT_PROFITABLE`; same `continue` | **Yes** |
| Existing dest copies flattened on that transition | No dest position entity; no flatten intent; close loop only on **source** `Completed` while still eligible | **No** |
| Prior open intents / shadow fills removed | Open loop only **inserts** if idempotency key missing; no delete | **No** |
| Source-close still copied after ineligibility | Close loop sits **after** eligibility `continue`; `Evaluate` would also `Reject` | **No** (orphan dest if a live send ever existed) |
| Risk martingale / EmergencyFlatten dest-unwind | Both `IsIncreasing` only; reason `EMERGENCY_FLATTEN_BLOCKS_NEW` | **No** |
| Live dest capital today | `NewOrderSingleImplemented = false`; persist `AllowFixSend = false` | Dest PnL **$0** by `SAFE_BY_ABSENCE` |

---

## 5. Honesty / residual

- **Confirmed claim is about policy + hop shape**, not a measured live dest book. Live send is unimplemented; residual dest risk if `35=D` were later armed would be **orphaned copies** after an admitted name goes martingale/negative.
- Eligibility skip is **stricter** than “skip new opens”: it also **stops source-close copy**. That is the opposite of dest flatten and worse than leave-and-mirror-exits.
- State `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED` takes the same `continue` path — also no dest flatten.
- Scorer can flip `Martingale` after admission (`BaselineScorer` + persist `EfTradingStore`); the copy hop will then skip. Admission is not sticky. Dest inventory is sticky by omission.

**DONE.** Product not edited.
