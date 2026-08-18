# P500_BOOK_3 — TradeReconstructor vs 303274-style same-second 0.05 overlapping entries

| Field | Value |
|---|---|
| Slot | **3** (P500 book band) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_3.md` |
| Assigned | Read `TradeReconstructor` and 303274-style overlapping **0.05** lot **same-second** entries. Is grid flagged? Measured evidence for **higher profit / lower loss**. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** |
| `REAL_COPY` flipped | **No.** |
| `35=D` sent | **No.** |
| Method | Full read of `TradeReconstructor.cs` (347 lines), `ReconstructedTradeResult.cs`, `NormalizedDeal.cs`, `BaselineScorer.cs`, `TraderStateMachine`, `RiskEngine.cs`, `CopyTradingService.cs`, `QuantityNormalizer.cs`, `DealIngestionService.RebuildTraderAsync`, `EfDashboardQueries.GetOverviewAsync`, `TradeReconstructionTests.cs`, A21 §1.6 / §4.1 / §8, `docs/trade-reconstruction.md`, `docs/scoring.md`, catalog pin of login **303274**. Grep of `src/` for `WasGrid` / `GridFlag` / `same-second` / `overlapping` = **0**. Loopback `GET /api/overview` and `/api/traders` **not re-probed** this slot (agent fetch of `127.0.0.1` blocked). No Manager re-attach. No deal-tape replay. |
| Binding law | A21 reconstruction identity (hedge ticket ≠ scale-in). A22 `FLAG_AVERAGING_DOWN` (unimplemented). Architecture §14 / §15 / §35 / §60. Honesty: **wanting profit does not create an edge.** |

**Honesty (tape vs code):** Catalog confirms login **303274** exists. This tree has **no** persisted reconstructed-trade dump and **no** deal export for that login. The 102-trade / +1,228 / first-3 **−0.35, −55.30, +25.90** / later **SHADOW 93.50** numbers are **prior-session assigned facts** (`P500_PROFIT_SYNTHESIS.md`, `P500_S020`, `P500_S027`, `P500_S046`). Code path is measured here. The tape is **not** re-pulled. If a later ledger disagrees on dollars, **replace the dollars**; the identity hole (distinct `PositionId`s never call `ScaleIn`) does not move.

**One-line:** Grid is **not flagged**. Same-second overlapping 0.05 lots on distinct hedge `PositionId`s reconstruct as N independent flat-lot XAU trades (`WasScaledIn=false`, `WasAveragedDown=false`). Scorer martingale / averaging stay **false**. A recovered challenge grid can sit in **SHADOW**. Copying **all 8463** logins would also copy the `RISK_BLOCKED` left tail. Dest is unhurt today only because send is absent.

---

## 0. Verdict

| Question | Measured answer |
|---|---|
| Is grid flagged? | **NO.** There is no `WasGrid`, `GridFlag`, same-second cluster bit, or cross-`PositionId` detector in `src/`. |
| Does `TradeReconstructor` see 303274-style spray as one book? | **NO.** `GroupBy(d => d.PositionId)` (L45). A21 §1.6 forbids merging hedge tickets. That is **correct reconstruction identity** and a **wrong tail screen**. |
| Does averaging latch? | **Only** same-`PositionId` same-side `ENTRY_IN` at a **strictly worse** pre-fill VWAP (`ScaleIn` L251–255). Parallel 0.05s never enter `ScaleIn`. |
| Does martingale latch? | **Not in this class.** Scorer only: next **completed** trade `MaxVolumeLots > 1.25×` prior **and** prior `NetRealizedPnl < 0`. Same-size 0.05 never trips. Same-second opens are not “after a close.” |
| Can 303274-class land SHADOW? | **YES** if later expanding XAU NET>0, PF high, risk < 40. Assigned later state **SHADOW / 93.50**. |
| Does copy-all 8463 help profit? | **NO.** It copies `RISK_BLOCKED` losses (synthesis: **29** names, **−$241,580**) plus uncopyable scalps/grids. Scored XAU book was **net −$154,425**. Wanting profit is not an edge. |

**Slot 3 FAIL as a copy screen. PASS as reconstruction identity (A21 §1.6).** Empty PASS refused — the SUT was read in full.

Higher profit / lower loss (policy, **not** a product patch this slot):

1. Do **not** copy 303274-class same-second / same-minute multi-ticket same-symbol same-side gold.
2. Do **not** copy `RISK_BLOCKED`, `INSUFFICIENT_DATA`, or “all 8463.”
3. Keep `35=D` off. `destinationRealPnl` is a constructor **0** (`EfDashboardQueries.cs` L44). That is uncomputed dest, not a proven flat book.

---

## 1. What `TradeReconstructor` actually does

File: `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` — **347** lines this pass (prior swarm pin 12 768 B, SHA `AEA3930B…`; **not re-hashed** here).

Public surface:

| Member | What it gates |
|---|---|
| `Reconstruct` | Scope broker+login → drop cancel-dirty from first-3 → **group by `PositionId`** → per-position lifecycle |
| `CompletedXauUsdTrades` | `Completed && IsXauUsd && EligibleForFirstThree` — **no** grid / avg / lot-ratio predicate |
| `IsEarlyScoreEligible` | count ≥ 3. A spray of three 0.05 tickets **unlocks** scoring |

Grep on this file / `src/Domain/Reconstruction`:

| Probe | Hits |
|---|---:|
| `WasGrid` / `Grid` / `grid` | **0** |
| `Martingale` | **0** |
| `WasAveragedDown` | **3** (field, latch, emit) |
| `WasScaledIn` | **3** (field, latch, emit) |
| `Filter` / `Block` / `Reject` | **0** |
| `GroupBy(d => d.PositionId)` | **1** (L45) |

Grouping (the hole):

```45:52:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        foreach (var group in trading.GroupBy(d => d.PositionId))
        {
            var rows = ReconstructPosition(brokerId, login, group.Key, group.ToList());
            if (dirtyPositions.Contains(group.Key))
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
            results.AddRange(rows);
        }
```

Same-side add is **only** reachable inside one group:

```123:133:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        if (open is null)
            return OpenTrade.Start(brokerId, login, positionId, deal, lots, direction, _symbols);

        if (open.Direction == direction)
        {
            open.ScaleIn(deal, lots);
            return open;
        }
```

Averaging latch (netting only; strict inequality; **before** VWAP update):

```248:264:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public void ScaleIn(NormalizedDeal deal, decimal lots)
        {
            // Averaging down: add to a long after price fell, or to a short after price rose.
            var worse = Direction == TradeDirection.Long
                ? deal.Price < EntryVwap
                : deal.Price > EntryVwap;
            if (worse)
                WasAveragedDown = true;

            WasScaledIn = true;
            RemainingLots += lots;
            if (RemainingLots > MaxVolumeLots)
                MaxVolumeLots = RemainingLots;
```

What is **not** encoded:

| Missing | Effect on 303274-class |
|---|---|
| Cross-`PositionId` window | Same-second 0.05 / 0.05 / 0.05 = three `Start()`s |
| Same-price add (`==` VWAP) | Even **netting** same-price grid is not averaging |
| Add count / lot sum / adverse $ | One tick below VWAP ≡ a $80/oz ladder |
| Unrealized loss on a **sibling** ticket | Invisible |
| Hold time | Computed later; unused by score |
| Grid / cluster / concentration | **No field on `ReconstructedTradeResult`** |

Result flags (`ReconstructedTradeResult.cs` L34–36): `WasScaledIn`, `WasPartialClose`, `WasAveragedDown`. No fourth bit.

A21 §1.6 is explicit and implemented:

> Hedging mode: each `position_id` is its own lifecycle. Three separate BUY tickets are **three** trades, not one scale-in.

A21 §8.1: the algorithm **refuses** to “Merge multiple hedge `position_id`s into one scaled trade.” `docs/trade-reconstruction.md` groups by `position_ticket` and never mentions averaging or grid.

That identity is **correct for a book**. It is **incorrect as a copy filter**. Prop / Achiever demo MT5 gold is almost always **hedging**. An EA that fires 0.05 + 0.05 + 0.05 in one second produces N tickets, N reconstructed trades, N first-3 candidates, **zero** averaging bits.

---

## 2. Worked 303274-shaped reconstruction (code path, not a re-pulled tape)

Volume law: `VolumeConverter.Manager` scale **10_000**. Native `500` = **0.05** lot.

Fixture the product **would** emit (same UTC second, three hedge tickets, worse prices — the assigned overlapping-0.05 class):

| Deal | PositionId | Entry | Side | Native | Lots | Price | Time |
|---|---:|---|---|---:|---:|---:|---|
| 1 | 9001 | IN | Buy | 500 | 0.05 | 2340.00 | T |
| 2 | 9002 | IN | Buy | 500 | 0.05 | 2339.20 | T (same second) |
| 3 | 9003 | IN | Buy | 500 | 0.05 | 2338.40 | T (same second) |
| 4–6 | 9001–9003 | OUT | Sell | 500 | 0.05 | various | T+60…180 s |

Because grouping key is `PositionId`:

| Reconstructed row | `MaxVolumeLots` | `WasScaledIn` | `WasAveragedDown` | `DealCount` |
|---|---:|---|---|---:|
| 9001 | 0.05 | **false** | **false** | 2 |
| 9002 | 0.05 | **false** | **false** | 2 |
| 9003 | 0.05 | **false** | **false** | 2 |

If those three were **one** netting `PositionId`, `ScaleIn` would fire twice, `WasScaledIn=true`, and prices 2339.20 / 2338.40 vs VWAP would set `WasAveragedDown=true`. Hedging never takes that branch.

Unit tests **lock the netting path only**. `TradeReconstructionTests.Deal(...)` forces the same `position` on every deal of a case. Facts: simple RT, scale-in+partial (same pos 20 @ 2300 then 2290 → `WasAveragedDown` true), reverse INOUT, first-3 unlock, cancel dirt, ignore balance. **Zero** hedge-grid / same-second / distinct-`PositionId` facts. Grep of `tests/` for `WasGrid` / `same-second` / `overlapping` = **0**.

---

## 3. Live pin: login 303274

Catalog (`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`, probe `2026-08-18T08:42:16Z`):

| Field | Value |
|---|---|
| login | **303274** |
| broker (file) | ACHIEVER group list |
| group | `demo\yo-2step` |
| leverage | 100 |
| balance / equity | **16228.24 / 16228.24** |

Assigned tape (not re-pulled; cite `P500_PROFIT_SYNTHESIS` §2.4 / `P500_S020` / `P500_S005`):

| Field | Value |
|---|---|
| Completed XAU | **102** |
| Dashboard / source PnL | **+1,228** |
| Pattern | **overlapping 0.05 lot, same-second multi-ticket** |
| Hold class | **1–3 min** scalps; winners **$0.35–$0.85** |
| First 3 XAU NET | **−0.35, −55.30, +25.90** → **−29.75** |
| First-3 PF | 25.90 / 55.65 ≈ **0.465** (not skill) |
| Later state / quality | **SHADOW / 93.50** |
| Averaging / martingale (assigned) | **false / false** — scorer did not treat the grid as averaging |

Why the code **cannot** flag that pattern:

1. Same-second multi-ticket ⇒ distinct hedge `PositionId`s (ticket ≈ position on retail/prop MT5).
2. `GroupBy(PositionId)` never merges them.
3. Each IN is `open is null` → `Start`, not `ScaleIn`.
4. `WasAveragedDown` stays default **false** on all 102 rows.
5. `BaselineScorer.ComputeFeatures` → `AveragingDown = trades.Any(...)` → **false**.
6. Martingale loop: `0.05 > 0.05 × 1.25` is **false**. Same-second opens are not sequenced after a **closed** loser.
7. Lot escalation: `0.05 > 0.05 × 1.5` is **false**.
8. Risk from averaging / martingale / escalation = **0**. Standing SL-miss penalty is at most **+10**. `SHADOW` needs `risk < 40` and `quality ≥ 70`. **93.50 is reachable** on the expanding book (NET>0, high PF) even though first-3 is red.

First-3 is a **count latch**, not a profit license (`IsEarlyScoreEligible` L75–76). Production score window is **all** `Completed && IsXauUsd` (`RebuildTraderAsync` L126–127), not the first three dollars. That is how −29.75 then later SHADOW 93.50 can coexist.

`AverageHoldSeconds` is computed (`BaselineScorer.cs` L120) and **never read** by `Score()` or `FromBaseline`. 1–3 minute gold is invisible to state.

---

## 4. Downstream still does not treat it as a grid

### 4.1 Scorer

```86:94:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var martingale = false;
        var lotEscalation = false;
        for (var i = 1; i < trades.Count; i++)
        {
            if (trades[i - 1].NetRealizedPnl < 0 && trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.25m)
                martingale = true;
            if (trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.5m)
                lotEscalation = true;
        }
```

```117:118:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
            Martingale = martingale,
            AveragingDown = trades.Any(t => t.WasAveragedDown),
```

```134:137:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var risk = 0m;
        if (features.Martingale) risk += 35;
        if (features.AveragingDown) risk += 20;
        if (features.LotEscalation) risk += 15;
```

```194:205:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

| Book | Flags | Typical state |
|---|---|---|
| Hedging 0.05×N same second, later green | avg **false**, mart **false**, risk ~0–10 | **SHADOW** (303274 class) |
| Netting add-lower on one ticket, later green | avg **true**, risk **+20** | **still SHADOW** (20 < 40) |
| Closed loss then next ticket >1.25×, NET<0, DD>0 | mart **true** | **RISK_BLOCKED** |
| A22 `FLAG_AVERAGING_DOWN` (floor 75; severe at N≤3) | **not implemented** | grep `FLAG_AVERAGING` in `src/Domain` = **0** |

A recovered grid is the **dangerous** one for dest: it looks like high win-rate, high PF XAU. The next one-way gold trend is the left tail. Reconstruction will faithfully emit that blow-up **after** it has already happened on the source.

`docs/scoring.md`: “Martingale / large sequential size-up after losses ⇒ `RISK_BLOCKED`.” Same-second flat 0.05 is **neither**.

### 4.2 Risk engine

`RiskEvaluationRequest` has `MartingaleFlag` and `AbnormalSizing`. **No** averaging. **No** grid. **No** `TraderState`. `Evaluate` never emits `TRADER_RISK_BLOCKED`.

`CopyTradingService` passes `MartingaleFlag = score.Martingale` and `AbnormalSizing = score.LotEscalation`. For 303274-class both are **false**, so `MARTINGALE_BLOCK` / `ABNORMAL_SIZING_BLOCK` do **not** fire.

### 4.3 Copy hop (still no send)

```95:96:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
        var copyable = new[] { TraderState.SHADOW, TraderState.LIVE_CANDIDATE, TraderState.LIVE };
        var scores = await _db.TraderScores.Where(s => copyable.Contains(s.CurrentState)).ToListAsync(ct);
```

303274-class **is** copyable under this set (SHADOW). `RISK_BLOCKED` is **excluded** here — that is **not** the same as a copy-all-8463 policy.

Each reconstructed 0.05 ticket is sized:

```text
AllocationFactor = 0.05
GoldSpec        = min 0.01 / max 5 / step 0.01
raw             = 0.05 × 0.05 = 0.0025
Truncate(0.0025 / 0.01) = 0  →  QuantityNormalizer returns 0  →  intent skipped
```

That is a **min-lot floor accident**, not a grid flag. Raise allocation toward 1:1 (or merge the basket into `MaxVolumeLots`) and dest sprays **N × 0.05** into the same second, all lifting the same ask.

Persist always writes `AllowFixSend = false`. `NewOrderSingleImplemented = false`. `VenueReconciled = false`. `CanPromoteToLive => false`. This slot did not enable any of those.

### 4.4 Dest economics (why “copy the SHADOW grid” is not profit)

Contract: 1 lot = 100 oz. On **0.05** lot, $1/oz = **$5.00**.

Assigned 303274 winners **$0.35–$0.85** ⇒ captured move **7–17 cents**. Dest taker round-trip at a **0.30** gold spread = **$1.50** on 0.05 lot — already larger than the median assigned winner — **before** 1–15 s ingest lag, **before** `SIGNAL_STALE` (15 s) drops late winners and still allows unguarded CLOSE. Illustrative dest math is in `P500_S005` §6 (dest **−$1.40** vs source **+$0.85** on a 17-cent ticket). A72: dest p50/p95 spread is **not** measured on this venue; even a tight **0.18** already kills the $0.35 winner.

Same-second spray makes it worse: N dest `35=D` after one delay, one gold bet, clustered gross.

---

## 5. Copy-all 8463 is the opposite of an edge

Catalog pin (`LIVE_GROUPS_AND_TRADERS.json`): Achiever **8 groups / 6512** + Starwave **10 groups / 1948** = **18 / 8460**. Synthesis / parent live API used **8463** (unreconciled +3; do not greenwash). User brief: **8463**. None of those counts is “8463 copy candidates.”

Synthesis live book (mid-scoring, **not re-GETted** this slot):

| Bucket | Count | Source XAU / dashboard $ |
|---|---:|---:|
| Accounts | 8463 | — |
| SHADOW | 70 | **+$78,276** (all **demo**) |
| WATCH | 79 | +$8,178 |
| `RISK_BLOCKED` | **29** (all `martingale=true`) | **−$241,580** |
| Scored XAU book | — | **−$154,425** |
| `LIVE` / `LIVE_CANDIDATE` | 0 / 0 | — |
| Dest real P&L | — | **$0** (code literal) |
| Shadow P&L | — | **$0** (no quote tape) / or demo slippage vs invented 2399 |

`GetOverviewAsync` writes `DestinationRealPnl`, `XauGross`, `XauNet` as constructor **0, 0, 0**.

```text
copy-all EV  ≈  scored XAU book  ≈  −$154k at source
             +  dest spread/lag on scalps and grids
             +  the −$241k RISK_BLOCKED tail if those names are included
```

`CopyTradingService` today does **not** iterate `RISK_BLOCKED`. A policy of “copy every login in the 8463 census” **would**. That is the honesty line: **copying all 8463 logins would copy `RISK_BLOCKED` losses.** The blocked tail is larger than the SHADOW head. SHADOW itself is 100% `demo\yo-2step` / `demo\yo-payp` — challenge books built to **pass a profit target**, then many martingale the rest.

Wanting higher dest profit does not make 303274’s +1,228 (or 70 SHADOW names, or 8463 rows) a Pepperstone edge. Source $ on a demo 2-step is not destination expectancy after venue costs.

---

## 6. What higher profit / lower loss actually is (still no send)

Do **not** implement in this slot. Named so the hole is not re-discovered as a feature.

| Gate | Why |
|---|---|
| Reject same-second / same-minute multi-ticket same-symbol same-side as **grid / concentration** | 303274 hole; A21 identity stays; **score/risk** must not treat “different `PositionId`” as “not averaging” |
| Reject `RISK_BLOCKED` **and** do not copy-all 8463 | Left tail −$241k |
| Median hold **≥ 15 min** (use the unused `AverageHoldSeconds`, preferably **median**) | 1–3 min gold dies in dest RT |
| Completed XAU **≥ 20**, XAU-only NET>0 after dest haircut | First-3 −29.75 is not skill; 93.50 is expanding luck + recovered grid |
| Dest qty after allocation **≥ min and ≤ 0.05**; do not 1:1 a spray | 5% of 0.05 already floors to 0 — do not “fix” that by raising size |
| Keep `REAL_COPY` false / `35=D` absent until shadow-after-costs on a **standing QUOTE** tape is green | Dest P&L is still the literal 0 |

`CanPromoteToLive => false` is the capital brake. It is **not** a grid detector.

---

## 7. Non-claims

- This slot did **not** replay 303274 deals from Manager or Postgres. No reconstructed-trade JSON for that login exists in-tree.
- `GET http://127.0.0.1:5000/api/overview` and `/api/traders` were **not** re-measured (loopback blocked). Live counts above are **prior pins**.
- SHA-256 of `TradeReconstructor.cs` was **not** recomputed this pass. Line count **347** matches the prior 347-line / 12 768 B pin.
- Dest spread dollars in §4.4 are **illustrative** (A72). The inequality (dest RT ≫ 7–17 cent capture) is the claim.
- Product source, flags, and FIX were not touched. No `35=D`. No secrets.

---

## 8. Files read (absolute)

| Path | Use |
|---|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | GroupBy PositionId; ScaleIn latch; no grid bit |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | flags: scaled / partial / averaged only |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | `PositionId` is the identity |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 1.25× martingale; Any(averaged); SHADOW / RISK_BLOCKED |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | no averaging / no grid / no TraderState |
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | 0.05 × 0.05 → 0 dest qty |
| `D:\Prop\src\Domain\Volume\VolumeConverter.cs` | native 500 = 0.05 lot |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | Martingale / AveragingDown / LotEscalation — no Grid |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | expanding-window score; copies flags |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | SHADOW set includes 303274-class; AllowFixSend forced false |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `destinationRealPnl` literal 0 |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | netting-only; no hedge-grid fact |
| `D:\Prop\docs\trade-reconstruction.md` | group by position_ticket; no averaging |
| `D:\Prop\docs\scoring.md` | RISK_BLOCKED = sequential size-up after losses |
| `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` | §1.6 hedge ≠ scale-in; §4.1 avg predicate; §8 no merge |
| `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` | unimplemented `FLAG_AVERAGING_DOWN` floor 75 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 303274 card; census 6512+1948=8460 |
| `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` | 303274 + copy-all EV |
| `D:\Prop\reports\swarm\20260818\P500_S005_gold_scalp_uncopyable.md` | dest RT vs $0.35–$0.85 |
| `D:\Prop\reports\swarm\20260818\P500_S020_first3_not_skill.md` | first-3 −29.75 vs SHADOW 93.50 |
| `D:\Prop\reports\swarm\20260818\P500_S027_martingale_holes.md` | same-second ≠ 1.25× |
| `D:\Prop\reports\swarm\20260818\P500_S046_averaging_flag.md` | WasAveragedDown never set on hedge grid |
| `D:\Prop\reports\swarm\20260818\P500_CODE_103.md` | reconstructor has no filter |

---

## 9. Bottom line

```text
TradeReconstructor  := GroupBy(PositionId). ScaleIn only inside one ticket.
WasAveragedDown     := same PositionId AND same-side IN AND price strictly worse than VWAP
WasGrid             := DOES NOT EXIST
303274-class        := same-second overlapping 0.05 hedge tickets
                    := N independent trades, flags false, martingale false
                    := SHADOW 93.50 is reachable on the recovered expanding book
Copy-all 8463       := includes RISK_BLOCKED (−$241k pin) + this grid class
Wanting profit      := not an edge
Dest today          := destinationRealPnl literal 0; 35=D absent; SAFE_BY_ABSENCE
Lower loss          := do not copy the spray; do not copy RISK_BLOCKED; do not copy-all
Higher profit       := tiny filtered subset after dest costs — not more logins
```

Product source was not edited. `REAL_COPY` was not enabled. No `35=D` was built or sent.
