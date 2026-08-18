# P500_S038 — ProfitFactor gaming: `GP/GL` with `PF=99` on a no-loss book

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S038_pf_gaming.md` |
| Agent | P500_S038 (read-only scoring honesty) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Secrets printed | **None** |
| SUT | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| Tests (do not lock skill) | `D:\Prop\tests\Unit\BaselineScorerTests.cs` |
| Spec contrast | `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` |
| Related | `B12_scoring_review.md`, `B35_score_fixtures.md`, `P500_S001_scorer_vs_negative_pnl.md`, `P500_S030_sl_rate.md` |

**Verdict:** `ProfitFactor` is **not a skill statistic**. It is `GP/GL` on completed XAU `NetRealizedPnl`, with a hard **99** when there are **zero losing trades**. A 3-trade lucky streak (or any N≥3 all-winner tape) hits **both** quality gates (`PF ≥ 1.2` and `PF ≥ 1.8`) and, if net > 0, the `+15` P&L boost. Combined with `N=3` eligibility, that book can print **100.00** (unit-test SL present) or the production cluster **95.50** (SL always missing). That is sample-size luck, not a measured edge. Higher-confidence profit must require **min trades (e.g. 20+) AND PF after costs AND a max-DD/GP bound**. Do not treat `PF=99` as evidence. Do not change product in this slot.

---

## 0. One-line answer

`PF = GL<=0 ? (GP>0 ? 99 : 0) : round(GP/GL, 4)`. Three winners, no loser → `PF=99` → `+15` quality from PF alone → plus `+15` if `NetPnl>0`. That is a lucky streak, not skill.

---

## 1. What the code actually computes

Source: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`.

### 1.1 Universe and sums

```66:70:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var net = trades.Sum(t => t.NetRealizedPnl);
        var wins = trades.Where(t => t.NetRealizedPnl > 0).Select(t => t.NetRealizedPnl).ToList();
        var losses = trades.Where(t => t.NetRealizedPnl < 0).Select(t => -t.NetRealizedPnl).ToList();
        var grossProfit = wins.Sum();
        var grossLoss = losses.Sum();
```

- Universe: completed XAU only, ordered by `ClosedAt`.
- `GP` = sum of **strictly positive** nets.
- `GL` = sum of **absolute strictly negative** nets.
- `Net == 0` (break-even) is dropped from both. A book of winners + scratches still has `GL = 0`.

### 1.2 The 99 sentinel

```114:114:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
            ProfitFactor = grossLoss <= 0 ? (grossProfit > 0 ? 99m : 0m) : decimal.Round(grossProfit / grossLoss, 4),
```

| GP | GL | Stored `ProfitFactor` |
|---|---|---|
| > 0 | 0 (no losing trade) | **99** |
| 0 | 0 (empty or all BE) | **0** |
| any | > 0 | `Round4(GP/GL)` (ToEven; **no cap**) |

A22 `baseline.v1` is the opposite shape: `GL>0 → min(GP/GL, 5)`; `GP>0` → **5**; else **1**. Stub vs spec: **99 vs 5**, all-BE **0 vs 1**, mixed book **uncapped vs 5**.

There is **no** `N` floor on this feature. One winner is already `PF=99`. Eligibility (`EarlyScoreTradeCount = 3`) is the only thing that stops a 1-trade or 2-trade book from becoming `SHADOW`; the feature itself is already maxed.

### 1.3 Quality treats 99 as “excellent process”

```152:160:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
        if (features.ProfitFactor >= 1.8m) quality += 5;
        quality += behavior * 0.2m;
        quality -= risk * 0.25m;
        if (features.CompletedXauTrades < EarlyScoreTradeCount)
            quality = Math.Min(quality, 40m);
        quality = Math.Clamp(decimal.Round(quality, 2), 0m, 100m);
```

Both PF steps fire at `PF=99`. They also fire at any real mixed-book PF ≥ 1.8. There is **no lerp, no diminishing return, no “untested loss process” haircut**. A22 explicitly scores `losses == 0` as **80 not 100** on the loss-consistency term (“untested loss process”). The stub’s `LossSizeCv` is **0** when `losses.Count < 2`, so a no-loss book looks *more* consistent than a book that actually survived a loss.

`N < 3` only **caps quality at 40**. At `N == 3` that cap is gone. `U(N)` from A22 (`U(3)=18`) is **not implemented**.

### 1.4 State consequence

`TraderStateMachine.FromBaseline`: `N≥3`, `quality≥70`, `risk<40` → **SHADOW**. `CanPromoteToLive => false` (vacuous LIVE lock). The gaming therefore pollutes **EarlyScore / leaderboard / SHADOW**, not live copy. That is still operationally dangerous: operators read 95–100 as “this trader is good.”

---

## 2. Walkthrough — 3-trade lucky streak (no loss)

This is the unit-test tape and the B35 FX-02 shape.

| n | Net | Lots | Result |
|---|---|---|---|
| 1 | +80 | 0.10 | win |
| 2 | +70 | 0.10 | win |
| 3 | +90 | 0.10 | win |

`BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` (`Closed(1,80), Closed(2,70), Closed(3,90)`, `InitialSl=2290`, commission/swap/fees **0**).

| Feature | Value | Why |
|---|---|---|
| `N` | 3 | eligible |
| `GP` | 240 | all nets > 0 |
| `GL` | 0 | no `Net < 0` |
| `ProfitFactor` | **99** | sentinel |
| `NetPnl` | 240 > 0 | +15 quality |
| `LotCv` | 0 | `n` lots identical; CV `<2` → 0 anyway |
| `LossSizeCv` | 0 | `<2` losses |
| `Martingale` / `AveragingDown` / `LotEscalation` | false | no size-up after loss |
| `MaxDrawdown` | 0 | equity never leaves peak |
| `SlUseRate` (test) | 1 | fixture `InitialSl=2290` |
| `SlUseRate` (production ingest) | **0** | SL never populated (`P500_S030`) |

### 2.1 Quality with test SL (what the unit test implies)

```
risk     = 0
behavior = 100
quality  = 50 + 15 + 10 + 5 + 0.20*100 − 0.25*0 = 100.00
state    = SHADOW
```

PF contribution of the lucky streak: **+15 of 100**. Combined with NET sign: **+30 of 100** from “I have not lost yet.”

### 2.2 Quality with production SL hole (95.50 cluster)

```
risk     = 10     (SlUseRate < 0.3)
behavior = 90     (SlUseRate < 0.5)
quality  = 50 + 15 + 10 + 5 + 18 − 2.50 = 95.50
state    = SHADOW
```

The 95.50 number operators see on clean 3-winner demo books is **this identity**, not a measured PF of 1.8× after costs. `PF=99` and `PF=1.8` produce the **same** quality increment.

### 2.3 Same boost for a cheaper cheat

Replace +80/+70/+90 with **+$0.01 × 3**. Still `GP>0`, `GL=0`, `PF=99`, `NetPnl>0`. Quality is **identical**. Dollar magnitude is not an input except the sign of NET and the unused DD>GP risk term (which cannot fire when equity is monotone up).

A 3-scratch + 1-tick-winner tape also works if any net is `> 0` and none is `< 0`.

---

## 3. Why this is not skill

| Failure mode | What the stub does | Why it is luck / gaming |
|---|---|---|
| Sample size 3 | Official at `N=3`; no `U(N)` | Binomial: three independent 55% winners is common. PF is undefined / infinite; stub maps that to **99**. |
| No loss observed | `GL=0` → 99; `LossSizeCv=0` | Loss process is **untested**. A22 scores that 80, not 100. Stub scores it as perfect. |
| Step gates | `≥1.2` then `≥1.8` | Once you have no loser, both fire. `PF=1.81` and `PF=99` are equal for quality. |
| Cap 99 vs 5 | 99 stored, used raw | Leaderboard / API can display 99 as if it were a real ratio. A22 `PF_CAP=5`. |
| No cost haircut in the *ratio policy* | Uses `NetRealizedPnl` | Correct **if** commission/swap/fees are on the trade. Demo seed + unit fixtures set them to **0**. Gross deal profit then *is* net. A +$1 winner with −$7 round-trip cost is a **loss** only when costs are ingested. They often are not. |
| No DD/GP bound on PF | DD>GP is **+10 risk only**, and only if `DD>0` **and** `GP>0` | All-winner tape has `MaxDrawdown=0`. Bound never fires. A later −$1 after +$1000 still yields PF≈1000 (uncapped). |
| Break-evens ignored | `Net==0` out of GP and GL | Scratch / commission-washed zeros do not create a denominator. |
| Reverse / dirty reconstruction | PF sums reconstructed nets | Known inflate path (`B11` / `D11`): reverse INOUT can double-count or warp nets; unit tests do not assert PF. |
| Expanding window | Full history, no FIRST3 freeze | Trader can stop after three winners and keep `PF=99` forever. No decay. |
| Tests lock the game | `Three_disciplined_winners_go_to_shadow_not_live` | Asserts **SHADOW**, not PF honesty. `ProfitFactorAndNetPnlTests` **does not exist** (`A89` / `C02` / `E019`). Mixed-book `GP/GL` is **untested**. |

Skill would survive: more independent trials, costs that actually hit the book, at least one loss (so the ratio is defined), and drawdown that is small **relative to** gross profit. None of those are required for the +15 PF boost.

---

## 4. “After costs” — current vs required

Reconstruction **does** form net as:

```
NetRealizedPnl = GrossRealizedPnl + Commission + Swap + fees
```

(`TradeReconstructor.cs` ~line 332. Commission is typically negative.)

So **if** ingest fills commission/swap/fees, PF is already on net. That is not the same as a **policy** of “PF after costs”:

| Reality | Effect on PF gaming |
|---|---|
| Unit tests: `Commission=0`, `Swap=0`, `Fees=0`, `Net=pnl` argument | PF=99 on any all-positive tape |
| B35 gold fixtures: same zeros | Gold files encode the game |
| Demo seeder deals: typically profit-only | Same |
| Live MT5: commission/swap exist on deals **if** the DTO path copies them | Mixed. `Mt5DealDto` has Commission + Swap; fees path is thinner. A winner that is net-negative after commission **should** enter GL — but a tiny net-positive after costs still yields PF=99 if no other trade lost. |
| No explicit “cost-adjusted PF” feature | Cannot require `PF_net` vs `PF_gross` or reject `cost/GP` above a bound |

**Required for a higher-profit claim:** compute PF on **net after commission+swap+fees**, refuse the 99 sentinel as a quality input, and drop (or heavily haircut) books where `|cost| / GP` is unknown or zero because costs were never ingested.

---

## 5. Max DD / GP bound — current vs required

Existing risk line:

```140:141:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (features.MaxDrawdown > 0 && features.GrossProfit > 0 && features.MaxDrawdown > features.GrossProfit)
            risk += 10;
```

Problems:

1. **+10 risk** does not touch `ProfitFactor` and does not remove the +15 quality PF boost.
2. `MaxDrawdown` is **completed-trade peak-to-trough on the same XAU window**, not intra-trade MAE / MTM (MFE/MAE is `Unavailable`).
3. Monotone-up lucky streak → `DD=0` → predicate false.
4. No `DD/GP` ratio stored; no reject if `DD/GP > 0.3` (example).
5. A22 wanted `max_dd_frac = max_dd / scale` plus a severe-DD **floor 75**, not a +10 add.

**Required for a higher-profit claim:** treat `max(DD)/GP` (and/or `|max(DD)| / |NET|`) as a **gate**, not a small additive. Example (research only, not implemented): if `N` is high enough to trust PF, still require `max_dd <= k * GP` (k ∈ 0.25–0.50) else withhold the PF quality term and do not advertise “high profit.”

---

## 6. Recommended policy (research; do not implement in this slot)

Higher displayed / actionable profit should be **conjunctive**. Any one missing → no “high PF” boost, no 99, no SHADOW-on-luck.

### 6.1 Minimum trades

- Do **not** feed PF into quality at `N < 20` (A22 `MIN_LIVE_TRADES = 20`; loader must reject `<= 3`).
- Keep `N=3` as **eligibility for a heavily penalized early score only**, with `U(3)` (or equivalent) still applied.
- Suggested split:
  - `N < 3`: insufficient (already).
  - `3 ≤ N < 20`: PF **omitted or capped at 1.0 for scoring**; display “n/a (n<20)” or the raw ratio **with** sample warning. No +10/+5.
  - `N ≥ 20`: PF may enter quality, after costs, after DD bound.

### 6.2 PF after costs

- Input = `NetRealizedPnl` **only if** at least one of commission/swap/fees is non-zero **or** an explicit “costs confirmed zero” flag exists.
- If every trade has `Commission=Swap=Fees=0`, treat cost quality as **unknown**; do not award PF boost.
- Cap the **feature** at 5 (A22 `PF_CAP`). Never store 99 as if it were a ratio. Suggested mapping when `GL==0`:
  - `N < 20` → PF feature **undefined** (not 99).
  - `N ≥ 20` and costs present → cap at 5, and still apply the untested-loss haircut (A22 loss-consistency 80).

### 6.3 Max DD / GP bound

- Compute `dd_over_gp = MaxDrawdown / max(GP, eps)` on the same window as PF.
- If `dd_over_gp > bound` (start at **0.50**, tighten toward **0.25** for “high profit” copy), **zero the PF quality term** and raise risk (A22 severe-DD floor 75 is the right *shape*).
- All-winner `DD=0` must **not** be treated as a perfect bound pass when `N < 20`. It is “no closed loss yet,” not “no drawdown risk.”

### 6.4 Quality formula shape (contrast only)

| Term | Stub today | Honest higher-profit bar |
|---|---|---|
| NET currency | +15 if `NET>0` (A22 **forbidden**) | omit |
| PF | +10/+5 raw, 99 allowed | 0 unless `N≥20` and costs known; then lerp with cap 5; weight ≤ 12% |
| Untested losses | rewarded | haircut |
| `U(N)` | 0 | `U(3)=18` … `U(20)=3` |
| DD/GP | +10 risk if DD>GP | gate on PF term |
| LIVE | hard-false (keep) | still false until shadow sample + human bits + `N≥20` |

Product stays unchanged this slot. When a later versioned scorer lands, it must be a **new** `score_version`, not a silent rewrite of `baseline_v0`.

---

## 7. Numeric comparison (same 3-winner tape)

Assume production SL hole (`risk=10`, `behavior=90`) so numbers match the live cluster.

| Policy | PF used | Quality | State |
|---|---|---|---|
| Stub now | 99 → +15 | **95.50** | SHADOW |
| Stub + delete PF steps only | 99 unused | 80.50 | SHADOW still (`≥70`) |
| Stub + delete PF and NET | 99 unused, no +15 | 65.50 | WATCH |
| A22-shaped (approx): raw `0.45*90 + 0.35*90 + 0.12*100 + 0.08*…` then `−U(3)=18`, P&L terms ≤20% | cap 5, not 99 | **≤ 82**, typically mid-70s | SHADOW at most |
| This report’s high-profit bar | PF withheld (`N=3<20`) | no PF boost | cannot claim high profit |

Even deleting PF steps leaves SHADOW via NET+behavior. The luck is **stacked**: PF 99, NET sign, no `U(N)`, DD=0, LossCv=0.

---

## 8. What is *not* claimed

- LIVE copy is **not** auto-opened by this bug (`CanPromoteToLive => false`).
- Mixed books with real GL do use `GP/GL` (uncapped). That path is **untested** (`ProfitFactorAndNetPnlTests` missing) but is not the 99 sentinel.
- Dashboard `netSourcePnl` can still be negative while XAU PF=99 (`P500_S001`). Two universes.
- This report does **not** change `BaselineScorer.cs`, tests, or fixtures.

---

## 9. Evidence index

| Claim | Evidence |
|---|---|
| PF formula / 99 sentinel | `BaselineScorer.cs:114` |
| GP/GL from net sign | `BaselineScorer.cs:66-70` |
| Quality +10/+5 on PF | `BaselineScorer.cs:154-155` |
| NET +15 | `BaselineScorer.cs:153` |
| N=3 eligibility | `EarlyScoreTradeCount = 3` line 40; cap only when `N<3` line 158-159 |
| 3 winners → SHADOW | `BaselineScorerTests.cs:21-26` |
| Costs on net when present | `TradeReconstructor.cs` Net = Gross+Commission+Swap+fees |
| Fixtures zero costs | `BaselineScorerTests.Closed`; `B35_score_fixtures.md` §4 |
| A22 PF_CAP=5, MIN_LIVE=20, U(3)=18, losses==0 → 80 | `A22_scoring_spec.md` §3 / §4.2 / §7 / §10 |
| 99 vs 5 already filed | `B12_scoring_review.md` §4.1 |
| No `ProfitFactorAndNetPnlTests` | `C02_score_tests_review.md`, `E019_score_cov.md` |
| Product edited this slot | **No** |

---

## 10. Close

`ProfitFactor = 99` on a no-loss 3-trade streak is a **sentinel for “undefined ratio,”** then reused as a **skill bonus**. It is not skill. Do not promote, sort, or copy on that number. When profit is the claim, require **N≥20**, **PF after real costs**, and a **max DD/GP bound**; cap PF at 5; haircut untested losses; keep LIVE locked. Product source was not modified.
