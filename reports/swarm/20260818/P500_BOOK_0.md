# P500_BOOK_0 — Quality 95.50 can sit on negative `netSourcePnl`

| Field | Value |
|---|---|
| Slot | **0** |
| Topic | Recalculate how `EarlyQualityScore = 95.50` can coexist with **negative** dashboard `netSourcePnl`. Quote the formula. Measured evidence for **higher profit / lower loss**. |
| SUT | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines; prior same-content SHA-256 `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34`, 8143 B, write 2026-08-18 13:08:10 — D12) |
| Adjacent | `EfDashboardQueries.GetTradersAsync`, `ReconstructionScoringService.RebuildTraderAsync`, `TraderRowDto`, Fake seed tape, `P500_PROFIT_SYNTHESIS.md`, `LIVE_GROUPS_AND_TRADERS.json` |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Live `35=D` sent | **No.** This slot did not enable `REAL_COPY`. |
| Secrets printed | **None.** |
| This-slot `GET 127.0.0.1:5000` | **Blocked** (worker SSRF filter). Live dollars below are **same-wave measured pins**, independently re-checked against on-disk catalog balances. |

**Honesty (binding):** wanting profit does not create an edge. `95.50` is a **shape** label on completed **XAU** tickets. It is not expectancy, not destination PnL, and not a license to copy. Copying all **8463** logins would copy the **`RISK_BLOCKED`** left tail (same-wave pin **−$241,580**) plus thousands of `INSUFFICIENT_DATA` books. Destination real PnL is **$0** because nothing sends `35=D`.

---

## 0. Verdict

| Claim | Measured answer |
|---|---|
| Can quality **95.50** coexist with **negative** `netSourcePnl`? | **Yes — because they are different columns.** |
| Can quality **95.50** coexist with **negative XAU** `features.NetPnl`? | **No. Algebra forbids it.** `95.50` requires XAU `NetPnl > 0` and `ProfitFactor >= 1.8`. |
| Is `95.50` a profit gate? | **No.** `NetPnl > 0` is a binary **+15**, not a dollar floor and not a veto on `SHADOW`. |
| Does copying the 8463-login catalog raise profit? | **No.** Scored XAU book pin **−$154,425**. `RISK_BLOCKED` pin **−$241,580**. Dest PnL **$0**. |
| Risk to Pepperstone capital this slot | **NONE** (`SAFE_BY_ABSENCE` + no send). |

---

## 1. Quoted formula (from the file that was read)

`ComputeFeatures` builds **XAU-only** net and PF:

```66:114:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var net = trades.Sum(t => t.NetRealizedPnl);
        var wins = trades.Where(t => t.NetRealizedPnl > 0).Select(t => t.NetRealizedPnl).ToList();
        var losses = trades.Where(t => t.NetRealizedPnl < 0).Select(t => -t.NetRealizedPnl).ToList();
        var grossProfit = wins.Sum();
        var grossLoss = losses.Sum();
        // ...
            NetPnl = net,
            GrossProfit = grossProfit,
            GrossLoss = grossLoss,
            ProfitFactor = grossLoss <= 0 ? (grossProfit > 0 ? 99m : 0m) : decimal.Round(grossProfit / grossLoss, 4),
```

`trades` is already `completedXau.Where(t => t.Completed && t.IsXauUsd)`. Non-XAU never enters `features.NetPnl`.

Quality, risk, behavior, and state:

```134:206:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var risk = 0m;
        if (features.Martingale) risk += 35;
        if (features.AveragingDown) risk += 20;
        if (features.LotEscalation) risk += 15;
        if (features.LotCv > 0.5m) risk += 10;
        if (features.SlUseRate < 0.3m) risk += 10;
        if (features.MaxDrawdown > 0 && features.GrossProfit > 0 && features.MaxDrawdown > features.GrossProfit)
            risk += 10;
        risk = Math.Min(100m, risk);

        var behavior = 100m;
        if (features.Martingale) behavior -= 30;
        if (features.AveragingDown) behavior -= 15;
        if (features.LotCv > 0.4m) behavior -= 10;
        if (features.SlUseRate < 0.5m) behavior -= 10;
        if (features.LossSizeCv > 0.8m) behavior -= 10;
        behavior = Math.Clamp(behavior, 0m, 100m);

        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
        if (features.ProfitFactor >= 1.8m) quality += 5;
        quality += behavior * 0.2m;
        quality -= risk * 0.25m;
        if (features.CompletedXauTrades < EarlyScoreTradeCount)
            quality = Math.Min(quality, 40m);
        quality = Math.Clamp(decimal.Round(quality, 2), 0m, 100m);
        // ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

Canonical write-up of the same lines:

```text
quality = 50
        + 15  if XAU NetPnl > 0          // binary, not |dollars|
        + 10  if ProfitFactor >= 1.2
        +  5  if ProfitFactor >= 1.8
        + 0.20 * behavior
        - 0.25 * risk
        then min(., 40) if N < 3
        then clamp(round(., 2), 0, 100)

SHADOW  if N >= 3 and quality >= 70 and risk < 40
          and not (risk >= 80 or (martingale and DD > 0 and XAU NetPnl < 0))
```

Dashboard `netSourcePnl` is **not** `features.NetPnl`. It is a second sum:

```90:118:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var pnls = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.Completed)
            .GroupBy(t => new { t.BrokerId, t.Login })
            .Select(g => new { g.Key.BrokerId, g.Key.Login, Pnl = g.Sum(x => x.NetRealizedPnl) })
            .ToListAsync(ct);
        // ...
                pnl,                          // NetSourcePnl — ALL completed symbols
                s?.EarlyQualityScore ?? 0,    // EarlyScore — XAU-only quality
```

`GetTradersAsync` walks **every** `Mt5Accounts` row (the 8460/8463 catalog), left-joins score, and sorts by `EarlyScore` descending. No `pnl > 0` predicate. No `RISK_BLOCKED` drop.

Rebuild path scores **XAU only**, then persists whatever state the stub emitted:

```126:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore { /* EarlyQualityScore = score.EarlyQualityScore,
            CurrentState = score.SuggestedState */ }, ct);
        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

---

## 2. Recalculation: how **95.50** is produced

Let `I_net = 1` iff XAU `NetPnl > 0`, `I_12 = 1` iff `PF >= 1.2`, `I_18 = 1` iff `PF >= 1.8` (implies `I_12`).

```text
quality = 50 + 15 I_net + 10 I_12 + 5 I_18 + 0.20 b − 0.25 r
```

### 2.1 The 95.50 identity

Assume the full bonus stack (`I_net = I_12 = I_18 = 1`):

```text
80 + 0.20 b − 0.25 r = 95.50
0.20 b − 0.25 r      = 15.50
4b − 5r              = 310
r                    = (4b − 310) / 5
```

On the **actual increment lattice** (`b` is 100 minus {30,15,10,10,10}; `r` is a sum of {35,20,15,10,10,10}):

| b | (4b−310)/5 | Reachable r? |
|---:|---:|---|
| 100 | 18 | no |
| 90 | **10** | **yes** (exactly one +10 risk flag) |
| 85 | 6 | no |
| 80 | 2 | no |
| 70 | −6 | no |

**The only lattice point that prints 95.50 is `behavior = 90`, `risk = 10`.**

That pair is the “one mild ding” book:

- `risk = 10` ⇒ exactly one of `{LotCv > 0.5, SlUseRate < 0.3, MaxDrawdown > GrossProfit}`. Not martingale, not averaging, not lot-escalation.
- `behavior = 90` ⇒ exactly one of `{LotCv > 0.4, SlUseRate < 0.5, LossSizeCv > 0.8}`.

The **dominant live/demo path** is unused stops:

```text
SlUseRate = 0
  → risk += 10, behavior -= 10
  → risk = 10, behavior = 90
NetPnl > 0, PF stub 99 (no XAU losses) or PF >= 1.8 (small XAU losses vs larger XAU wins)
quality = 50 + 15 + 10 + 5 + 90×0.20 − 10×0.25
        = 80 + 18 − 2.50
        = 95.50
N >= 3, quality >= 70, risk 10 < 40  →  SHADOW
```

`AverageHoldSeconds` is computed and **never added**. A 90-second gold scalp and a 4-hour swing print the same 95.50.

### 2.2 95.50 is impossible on a losing XAU book

If `I_net = 0` then `NetPnl <= 0` ⇒ `GP <= GL` ⇒ `PF <= 1` (or `PF = 0` when both are 0). Then `I_12 = I_18 = 0` as well.

```text
quality_max(I_net=0) = 50 + 0.20×100 − 0.25×0 = 70.00
```

70.00 is the **clean-loser with SL** SHADOW hole (still not a profit). It is **25.50 points below 95.50**.

If `I_net = 1` but `PF < 1.2`:

```text
quality_max = 50 + 15 + 20 = 85.00   // still not 95.50
```

If `I_net = 1` and `1.2 <= PF < 1.8` (only +10 PF):

```text
quality_max = 50 + 15 + 10 + 20 = 95.00   // 95.00, not 95.50
```

**Therefore 95.50 requires XAU `NetPnl > 0` and `ProfitFactor >= 1.8`.** A negative *XAU* book cannot print 95.50. A negative *dashboard* book can.

### 2.3 Demo tape (this repo, closed-form)

`FakeMt5BrokerConnector` 10001 (no SL on the DTO). Net = `Gross + Commission + Swap` (`TradeReconstructor` L332):

| Pos | Gross | Comm | Swap | Net |
|---:|---:|---:|---:|---:|
| 501 | +153 | −1.2 | −0.4 | **+151.40** |
| 502 | −88 | −1.1 | −0.3 | **−89.40** |
| 503 | +163 | −1.2 | −0.2 | **+161.60** |
| **Σ** | | | | **+223.60** |

```text
GP = 151.40 + 161.60 = 313.00
GL = 89.40
PF = 313.00 / 89.40 = 3.5011 >= 1.8
flat 0.10 lots, no martingale, SlUseRate = 0
quality = 95.50, state = SHADOW
```

Here **both** columns are green: `earlyScore = 95.50` and `netSourcePnl = +223.60` (E031 live Fake GET). That is the **easy** case. It does not prove 95.50 means “this login is a winner.”

99001 (three tiny XAU wins, PF stub 99, no SL): quality **95.50**, `netSourcePnl = +108.20`. Same shape, smaller dollars. The +15 bonus does not care.

10002 (0.10→0.20→0.40, all losses): `netSourcePnl = −2107`, quality **42.50**, `RISK_BLOCKED` via `martingale ∧ DD > 0 ∧ NetPnl < 0`. This is the shape you copy if you copy the catalog.

---

## 3. How 95.50 coexists with **negative** `netSourcePnl`

Two independent numbers ride the same `TraderRowDto`:

| Field | Source | Universe |
|---|---|---|
| `EarlyScore` (95.50) | `TraderScore.EarlyQualityScore` ← `BaselineScorer` | completed **XAUUSD** only |
| `NetSourcePnl` | Σ `ReconstructedTrades.NetRealizedPnl` where `Completed` | **every** reconstructed symbol |

A login can therefore print:

```text
XAU first-N:  small net > 0, PF >= 1.8, no SL  →  quality 95.50, SHADOW
other symbols / later non-XAU:  large losses     →  dashboard netSourcePnl < 0
```

`FromBaseline` never reads the dashboard column. `SHADOW` does not require `features.NetPnl > 0` either — that bit only adds +15. The state test is `quality >= 70 && risk < 40`.

### 3.1 Same-wave live rows (P500 pin, not re-GETed this worker)

From `P500_PROFIT_SYNTHESIS.md` §2.3–2.4 (API mid-scoring, Achiever only):

| Login | Group | Completed XAU | Early score | State | Dashboard `netSourcePnl` |
|---:|---|---:|---:|---|---:|
| 302252 | `demo\yo-2step` | 11 | **95.50** | SHADOW | **−68.46** |
| 303174 | `demo\yo-2step` | (scored) | **95.50** | SHADOW | **−29.38** |

Those two rows are the existence proof: **95.50 ∧ `netSourcePnl < 0` is a legal served book.**

### 3.2 Independent catalog check (this slot, on disk)

`LIVE_GROUPS_AND_TRADERS.json` (probe `2026-08-18T08:42:16Z`; passwords not present):

| Login | Group | Balance | Equity | `1000 − balance` |
|---:|---|---:|---:|---:|
| 302252 | `demo\yo-2step` | **931.54** | 931.54 | **68.46** |
| 303174 | `demo\yo-2step` | **970.62** | 970.62 | **29.38** |

The painted negatives are the **challenge-account drawdown from a $1,000 start**, all symbols, all costs. They match the dashboard column to the cent. They do **not** match the XAU-only `features.NetPnl` that printed 95.50. Both logins are **demo 2-step**, not funded live.

### 3.3 What 95.50 is *not* saying about 302252

- Not “XAU lost $68.” If XAU had lost, quality would be ≤ 70, not 95.50.
- Not “this account has positive expectancy after Pepperstone spread.” Hold-time unused; MFE/MAE `Unavailable`; dest quotes null.
- Not “copy me.” `CanPromoteToLive => false`. `CopyTradingService.NewOrderSingleImplemented = false`. Persist shadows **every** completed XAU ticket for a SHADOW login, including losers inside a net-positive XAU slice.

---

## 4. Book-level evidence: copy-all is a loss

Same-wave `GET /api/overview` + `/api/traders` pin (`P500_PROFIT_SYNTHESIS.md` §1). This worker could not re-hit `:5000`.

| Metric | Pin |
|---|---|
| Accounts on the API | **8463** (Achiever 6512 + Starwave ~1951) |
| Earlier Manager probe (08:42Z JSON) | **8460** = 6512 + 1948 (18 groups). Drift of **3** logins — do not paper over it. |
| Achiever `demo\yo-2step` | **6295 / 6512** |
| SHADOW | 70, **100% demo** (`yo-2step` / `yo-payp`) |
| WATCH | 79 |
| RISK_BLOCKED | **29**, all `martingale=true` |
| LIVE / LIVE_CANDIDATE | **0 / 0** |
| INSUFFICIENT_DATA | ~8284 |
| SHADOW Σ source PnL | **+$78,276** |
| WATCH Σ source PnL | **+$8,178** |
| **RISK_BLOCKED Σ source PnL** | **−$241,580** |
| **All scored XAU source PnL** | **−$154,425** |
| `destinationRealPnl` | **$0** (overview hard-codes dest = 0) |
| Shadow PnL | **$0** (no standing quote tape) |
| `realCopyEnabled` (synthesis) | **false** at that capture |

Copy-all EV **is** the scored XAU book: **negative six figures**. The blocked tail is larger than the SHADOW head. Sorting by 95.50 and spraying `35=D` would still pull:

1. Negative-dashboard SHADOW names (302252, 303174).
2. Lot-escalation “winners” (303310, +41,634 at **2.0** lots — dest ruin if size is copied).
3. Sub-3-minute scalps (322947, hold ~163s) that die in spread + 15s signal-age.
4. Every `RISK_BLOCKED` martingale if the operator copies **logins** instead of `state==SHADOW`.
5. ~8284 `INSUFFICIENT_DATA` books with no XAU sample.

`GenerateShadowIntentsAsync` only iterates `{SHADOW, LIVE_CANDIDATE, LIVE}`. That **store-gate** is what keeps `RISK_BLOCKED` off the shadow hopper today. Copying “all 8463 logins” **bypasses** that gate. That is the honesty line in the assignment.

---

## 5. Higher profit / lower loss (what the formula actually implies)

Wanting 95.50 to mean “edge” does not make it an edge. The arithmetic says the opposite.

### Higher profit (only if send ever exists — it does not today)

| Do | Why the formula agrees |
|---|---|
| Split **XAU-only net** from dashboard `netSourcePnl` | 95.50 is XAU; −68.46 is the $1,000 challenge. Operators will copy losers if they trust the painted column. |
| Floor on **XAU** net **and** all-symbol net before any intent | `I_net` is +15, not a veto. Add the veto the stub omitted. |
| Raise N from 3 to ≫ 20 | 95.50 is reachable at N=3 on three lucky XAU tickets. First-3 is luck. |
| Use `AverageHoldSeconds` | Computed, unused. Scalps print 95.50. |
| Haircut PF for dest spread/commission | Source PF 3.5 on 0.10 lot gold is not Pepperstone PF. |
| Drop `demo\` / `contest\` | 6295 of 6512 Achiever logins are `demo\yo-2step`. Adverse selection. |
| Keep dest size ≤ 0.05 lot | 95.50 does not see max lot. 303310 is the counterexample. |

### Lower loss (now, and later)

| Do | Why |
|---|---|
| **Do not send.** Keep `REAL_COPY` off. Do not add `35=D`. | Dest PnL is 0. That is the only measured “profit.” |
| **Do not copy 8463 logins.** | You copy `RISK_BLOCKED` −$241k plus unknown books. |
| **Do not copy `RISK_BLOCKED`.** | Store already skips them for shadow. Keep that. It is a **shape** reject, not a PnL floor. |
| **Do not treat SHADOW 95.50 as a winner screen.** | 302252 / 303174. Also: clean XAU loser + SL ⇒ quality **70** ⇒ still SHADOW. |
| Do not copy unused-SL 95.50 books as if they were risk-controlled | The 95.50 lattice **is** the unused-SL path (`r=10,b=90`). |
| Do not flatten the MT5 source | Kill switch is dest-side only. |

**If send were armed on the current score:** expected destination PnL is **negative**. The source book is already net **−$154,425** before venue costs.

---

## 6. What this slot did not do

- Did not edit product source.
- Did not enable `REAL_COPY`.
- Did not build or send `35=D` / `NewOrderSingle`.
- Did not print passwords or FIX secrets.
- Did not re-GET `http://127.0.0.1:5000/api/overview` or `/api/traders` (SSRF block on this worker). Live aggregates cited from `P500_PROFIT_SYNTHESIS.md`. 302252/303174 **dollars** re-checked against `LIVE_GROUPS_AND_TRADERS.json` balances.
- Did not claim ML edge (Phase 6 closed; `mlProbability` is hard-null).

---

## 7. Slot close

- **slot:** 0
- **verdict:** `95.50` is XAU-shape, not profit. It **requires** XAU `NetPnl > 0` and `PF >= 1.8` and `(b,r)=(90,10)`. It **coexists** with negative `netSourcePnl` because the dashboard sums **all symbols**. Live demo rows 302252 (−68.46) and 303174 (−29.38) are the existence proof; catalog balances 931.54 / 970.62 independently match a $1,000 challenge drawdown.
- **evidence:** Full read of `BaselineScorer.cs` L152–160 / L194–204. Dashboard sum L90–94 vs scorer XAU filter L44. Fake 10001 closed-form +223.60 → 95.50. Catalog JSON 302252/303174. Same-wave book: 8463 logins, SHADOW all demo, RISK_BLOCKED −$241,580, scored XAU −$154,425, dest $0.
- **risk_to_capital:** **NONE** this process (`35=D` absent, this slot did not arm copy). **HIGH** if an operator copied all 8463 logins or treated 95.50 as a send list.
- **profit implication:** none from 95.50. Wanting profit is not an edge.
- **lower-loss implication:** do not copy the catalog; do not copy `RISK_BLOCKED`; do not send; split the two PnL columns before any live path.
