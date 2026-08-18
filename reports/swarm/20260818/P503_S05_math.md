# P503_S05 — CopyRosterEngine ConsecutiveLosses + DrawdownFromPeak math

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P503_S05_math.md` |
| Slot | **P503_S05** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned | Read `CopyRosterEngine` `ConsecutiveLosses` and `DrawdownFromPeak`. Quote the loops. Confirm **3-loss** and **40% peak DD**. Do not edit product. |
| Product source edited | **No.** This report is the only write. |
| Test source edited | **No.** |
| SUT | `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` |
| Tests (read only) | `D:\Prop\tests\Unit\CopyRosterEngineTests.cs` |
| Method | Full `read_file` of `CopyRosterEngine.cs` (138 lines). Adjacent: `CopyRosterEngineTests.cs` (129 lines). Grep `ConsecutiveLosses` / `DrawdownFromPeak` / `MaxConsecutiveXauLosses` / `MaxDrawdownVsPeak`. Nothing from memory. |

**Honesty rule:** Confirm the **defaults and the inequalities as written**. Do not claim the unit fixtures walk the same chronology the helpers assume. Product not modified.

---

## 0. Verdict (binding)

**CONFIRMED.** Default roster math is **3 consecutive closed XAU losses** and **40% drawdown vs peak closed-book equity**.

| Claim | Result | Evidence |
|---|---|---|
| Consecutive-loss cap is **3** | **Yes** | `RosterLimits.MaxConsecutiveXauLosses` default `= 3` (`CopyRosterEngine.cs` L15). |
| Peak-DD cap is **40%** | **Yes** | `RosterLimits.MaxDrawdownVsPeak` default `= 0.40m` (`CopyRosterEngine.cs` L16). |
| Streak counts trailing losses newest-last | **Yes** | `ConsecutiveLosses` reverse `for` (`L100–111`). `NetRealizedPnl < 0` increments; else `break`. |
| DD is max peak-to-trough / peak | **Yes** | `DrawdownFromPeak` forward `foreach` (`L113–128`). Gate is `dd.drawdown / dd.peak >= 0.40m` when `peak > 0` (`L70–72`). |
| Applied on roster only | **Yes** | Both gates require `alreadyOnRoster` (`L67`, `L71`). |
| Product edited | **No** | Report only. |

One-line:

```text
alreadyOnRoster && ConsecutiveLosses(xau) >= 3          → RemoveAndFlatten CONSECUTIVE_LOSSES_{n}
alreadyOnRoster && peak > 0 && maxDd/peak >= 0.40m      → RemoveAndFlatten DRAWDOWN_FROM_PEAK
```

---

## 1. Defaults (the two numbers)

```13:19:D:\Prop\src\Domain\Copy\CopyRosterEngine.cs
public sealed record RosterLimits
{
    public int MaxConsecutiveXauLosses { get; init; } = 3;
    public decimal MaxDrawdownVsPeak { get; init; } = 0.40m;
    public decimal MaxUnrealizedLossLotsUsd { get; init; } = 150m;
    public int MinCompletedXauToAdmit { get; init; } = XauUsdOneToOneCopyPolicy.MinCompletedXauTrades;
}
```

`CopyRosterEngine` ctor uses `limits ?? new RosterLimits()` (`L38–42`). Unit tests construct `new CopyRosterEngine()` with no override. The numbers in this report are therefore the **live defaults**.

---

## 2. Call site — sort first, then both loops

`Decide` filters completed XAU, **orders oldest → newest** (`ClosedAt ?? OpenedAt`), then feeds that list to both helpers. Newest is last. That matches `ConsecutiveLosses`’s parameter name `closedNewestLast`.

```56:72:D:\Prop\src\Domain\Copy\CopyRosterEngine.cs
        var xau = completedXau
            .Where(t => t.Completed && t.IsXauUsd)
            .OrderBy(t => t.ClosedAt ?? t.OpenedAt)
            .ToList();

        var net = xau.Sum(t => t.NetRealizedPnl);
        if (alreadyOnRoster && net <= 0)
            return Remove("XAU_BOOK_TURNED_NEGATIVE");

        var streak = ConsecutiveLosses(xau);
        if (alreadyOnRoster && streak >= _limits.MaxConsecutiveXauLosses)
            return Remove("CONSECUTIVE_LOSSES_" + streak);

        var dd = DrawdownFromPeak(xau);
        if (alreadyOnRoster && dd.peak > 0 && dd.drawdown / dd.peak >= _limits.MaxDrawdownVsPeak)
            return Remove("DRAWDOWN_FROM_PEAK");
```

Order of checks (after state / size-pattern / demo-group):

1. Whole-book `net <= 0` → `XAU_BOOK_TURNED_NEGATIVE`
2. Trailing streak `>= 3` → `CONSECUTIVE_LOSSES_{n}`
3. `maxDd / peak >= 0.40` with `peak > 0` → `DRAWDOWN_FROM_PEAK`

`>=` is inclusive: **exactly 3** losses fire; **exactly 40.00%** of peak fires.

---

## 3. Quoted loop — ConsecutiveLosses (3-loss)

```100:111:D:\Prop\src\Domain\Copy\CopyRosterEngine.cs
    public static int ConsecutiveLosses(IReadOnlyList<ReconstructedTradeResult> closedNewestLast)
    {
        var n = 0;
        for (var i = closedNewestLast.Count - 1; i >= 0; i--)
        {
            if (closedNewestLast[i].NetRealizedPnl < 0)
                n++;
            else
                break;
        }
        return n;
    }
```

### 3.1 Semantics

| Rule | As written |
|---|---|
| Walk | Reverse index: last element first (newest, after `Decide` sort). |
| Loss | `NetRealizedPnl < 0` only. |
| Stop | First non-loss (`>= 0`) `break`s. Older losses do not count. |
| Breakeven | `== 0` is **not** a loss; it **resets** the streak. |
| Empty list | `Count == 0` → loop never runs → `0`. |
| All losers | `n == Count`. |
| Gate | `streak >= 3` (default). 1 or 2 trailing losses do not remove. |

Worked examples (newest last):

| Closed PnL sequence (oldest → newest) | `n` | `n >= 3` |
|---|---:|:---:|
| `+10, +10, +10` | 0 | no |
| `+10, −1, −1` | 2 | no |
| `+10, −1, −1, −1` | **3** | **yes** |
| `−1, −1, −1, −1` | 4 | yes (`CONSECUTIVE_LOSSES_4`) |
| `−5, −5, −5, +1` | 0 | no (newest is a win) |
| `−5, −5, −5, 0` | 0 | no (breakeven breaks) |
| `+10, −1, +1, −1, −1` | 2 | no (win in the middle) |

**CONFIRMED: 3 consecutive newest-closed XAU losses remove an already-rostered trader.**

---

## 4. Quoted loop — DrawdownFromPeak (40%)

```113:128:D:\Prop\src\Domain\Copy\CopyRosterEngine.cs
    public static (decimal peak, decimal drawdown) DrawdownFromPeak(IReadOnlyList<ReconstructedTradeResult> closed)
    {
        var equity = 0m;
        var peak = 0m;
        var maxDd = 0m;
        foreach (var t in closed)
        {
            equity += t.NetRealizedPnl;
            if (equity > peak)
                peak = equity;
            var dd = peak - equity;
            if (dd > maxDd)
                maxDd = dd;
        }
        return (peak, maxDd);
    }
```

Gate again:

```70:72:D:\Prop\src\Domain\Copy\CopyRosterEngine.cs
        var dd = DrawdownFromPeak(xau);
        if (alreadyOnRoster && dd.peak > 0 && dd.drawdown / dd.peak >= _limits.MaxDrawdownVsPeak)
            return Remove("DRAWDOWN_FROM_PEAK");
```

### 4.1 Semantics

| Rule | As written |
|---|---|
| Curve | Closed-trade running sum of `NetRealizedPnl`, start `equity = 0`. |
| Peak | High-water **closed equity**, start `0`. Updated only when `equity > peak` (strict). |
| Drawdown | `peak - equity` after each trade; `maxDd` is the largest of those. |
| Ratio | `maxDd / peak` compared to `0.40m`. |
| `peak > 0` guard | If the book never prints a positive high-water, DD **cannot** fire (division skipped). A never-green book is supposed to die on `XAU_BOOK_TURNED_NEGATIVE` (`net <= 0`) instead. |
| Open PnL | **Not** in this helper. Open copy flatten is a different cap: `ShouldFlattenOpenCopy` at `−$150` (`L17`, `L97–98`). |

Worked example matching the intended “winners then crash” story (oldest → newest):

| Step | Trade PnL | equity | peak | dd | maxDd | maxDd/peak |
|---|---:|---:|---:|---:|---:|---:|
| 0 | — | 0 | 0 | 0 | 0 | n/a |
| 1–20 | +50 × 20 | 1000 | 1000 | 0 | 0 | 0 |
| 21 | −700 | 300 | 1000 | 700 | 700 | **0.70** |

`0.70 >= 0.40` and `peak > 0` → **Remove `DRAWDOWN_FROM_PEAK`**.

Boundary:

| peak | maxDd | ratio | Fire? |
|---:|---:|---:|:---:|
| 1000 | 399 | 0.399 | no |
| 1000 | 400 | **0.40** | **yes** (inclusive) |
| 1000 | 401 | 0.401 | yes |
| 0 | 100 | n/a | **no** (`peak > 0` false) |

**CONFIRMED: 40% drawdown from peak closed equity removes an already-rostered trader.**

---

## 5. Unit fixtures (read-only; chronology caveat)

`D:\Prop\tests\Unit\CopyRosterEngineTests.cs` names both gates.

### 5.1 `Three_consecutive_losses_remove` (L84–92)

```84:92:D:\Prop\tests\Unit\CopyRosterEngineTests.cs
    [Fact]
    public void Three_consecutive_losses_remove()
    {
        var trades = Enumerable.Range(1, 22).Select(i => Xau(i, i >= 20 ? -5 : 20)).ToList();
        var snap = Shadow(trades.Sum(t => t.NetRealizedPnl));
        var d = _e.Decide(snap, trades, true);
        d.Action.Should().Be(RosterAction.RemoveAndFlatten);
        d.Reason.Should().StartWith("CONSECUTIVE_LOSSES_");
    }
```

`Xau(id, …)` sets `ClosedAt = UtcNow.AddDays(-id).AddHours(1)` (L34). **Larger `id` is older.** Losses are `id >= 20` → ids 20, 21, 22 = the **oldest** three.

After `Decide`’s `OrderBy(ClosedAt)` the list is oldest-first:

```text
[22=−5, 21=−5, 20=−5, 19=+20, …, 1=+20]   newest last = +20
```

`ConsecutiveLosses` then walks from the **end**, sees `+20`, `break`s, `n = 0`. Book net `19×20 + 3×(−5) = +365 > 0`. Peak DD after recovery is `15 / 365 ≈ 0.041 < 0.40`.

**Honesty:** the **engine default is still 3 trailing losses**. This fixture’s losses are at the **start** of the closed series, not the tail. As written, `Decide` should **not** emit `CONSECUTIVE_LOSSES_*`. That is a test-data chronology bug, not a default-number bug. Product not edited.

A fixture that would actually hit the 3-loss gate (newest last after sort) would lose on the **smallest** ids, e.g. `i <= 3 ? −5 : 20`.

### 5.2 `Peak_drawdown_removes` (L117–128)

```117:128:D:\Prop\tests\Unit\CopyRosterEngineTests.cs
    public void Peak_drawdown_removes()
    {
        var trades = new List<ReconstructedTradeResult>();
        for (var i = 1; i <= 20; i++)
            trades.Add(Xau(i, 50));
        trades.Add(Xau(21, -700));
        var snap = Shadow(trades.Sum(t => t.NetRealizedPnl), 21);
        var d = _e.Decide(snap, trades, true);
        d.Action.Should().Be(RosterAction.RemoveAndFlatten);
        d.Reason.Should().Be("DRAWDOWN_FROM_PEAK");
    }
```

Intended story: 20×+50 then −700 → peak 1000, dd 700, ratio 0.70.

Actual chronology (`id=21` is oldest): `−700` **first**, then +50×20.

| After | equity | peak | maxDd | ratio |
|---|---:|---:|---:|---:|
| first trade −700 | −700 | 0 | 700 | n/a (`peak==0`) |
| +50×20 | 300 | 300 | 700 | **700/300 ≈ 2.33** |

`peak > 0` becomes true on recovery, but `maxDd` still holds the **initial hole vs peak=0**. `2.33 >= 0.40` → still `DRAWDOWN_FROM_PEAK`.

So this fact can pass for a **different** reason than “20 winners then a 70% crash.” The 40% inequality is still the gate. Side effect of starting `peak = 0`: any later positive peak keeps the opening underwater stretch in `maxDd`.

---

## 6. What this slot does **not** claim

- That `CopyRosterEngine` is wired into the 20 s copy hopper (P503_S01/S02: product hop re-checks policy and `continue`s; dest flatten is a roster decision, not proven live-wired here).
- That open unrealized PnL is inside `DrawdownFromPeak` (it is not; `$150` cap is `ShouldFlattenOpenCopy`).
- That the 3-loss unit fixture matches newest-last chronology (it does not; see §5.1).
- Any product edit.

---

## 7. Confirmations (repeat)

1. **`MaxConsecutiveXauLosses = 3`.** Reverse loop over newest-last closed XAU. `NetRealizedPnl < 0` counts; first `>= 0` stops. `alreadyOnRoster && streak >= 3` → `CONSECUTIVE_LOSSES_{n}`.
2. **`MaxDrawdownVsPeak = 0.40m` (40%).** Forward running-sum loop. `peak` is closed-equity high water from 0. `drawdown` is max `peak − equity`. `alreadyOnRoster && peak > 0 && drawdown/peak >= 0.40` → `DRAWDOWN_FROM_PEAK`.

**CONFIRMED: 3-loss and 40% peak DD.**
