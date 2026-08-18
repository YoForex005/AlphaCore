# P500_S040 — `MaxPositionQuantity = 5` is a gold blow-up cap, not a working dest cap

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S040_five_lot_cap.md` |
| Agent | P500 S040 (five-lot gold dest cap) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **None** |
| Live FIX send | **None** (`NewOrderSingleImplemented = false`; `AllowFixSend` forced false in copy persist) |
| Status | **POLICY / DEFAULT-VALUE FINDING** — do not treat 5.0 XAU lots as a retail working size |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest)

`RiskLimits.MaxPositionQuantity` default is **`5m`**. On XAUUSD that is **five standard lots of gold**.

```text
5.00 dest lots × 100 oz/lot  =  500 oz
$1 / oz move                 =  $500 P&L
$4 / oz move                 =  $2,000  = entire MaxDailyExecutionLoss default
$6 / oz move                 =  $3,000  = entire MaxPortfolioDrawdown default
```

That is **not** a first-money working cap on a retail Pepperstone / cTrader book. It is a **blow-up ceiling**: a single allowed ticket can spend the whole dollar-loss catalog in a routine gold swing.

**Lower-loss dest policy until proven (shadow + dest-net after costs):**

```text
gold dest working cap  ∈  [0.05, 0.10] lots     # Lots convention, 100 oz/lot
                       =  [5, 10] oz
```

`5.0` may remain as a **hard abort** far above the working cap (or, better, be lowered). It must **not** be the size the copy path is allowed to request on OPEN/INCREASE.

This report does **not** change product. Live send is still off. The finding is: **if** this default is ever the last qty gate before `35=D`, the first legal gold ticket can vaporize a retail dest account.

Related (already on disk, not re-decided here): `P500_S026_tiny_allocation.md` (working cap `0.05` lots / 5 oz + `allocationFactor ∈ [0.01, 0.05]`). This slot is the **anti-5.0** pin. S026 is the **what-to-use-instead** pin.

---

## 1. What the code actually allows

### 1.1 Engine default

File: `D:\Prop\src\Domain\Risk\RiskEngine.cs`

```text
public decimal MaxXauGrossExposure { get; init; } = 20m;   // line 10
public decimal MaxXauNetExposure   { get; init; } = 10m;   // line 11
public decimal MaxPositionQuantity { get; init; } = 5m;    // line 12
public int     MaxOpenPositions    { get; init; } = 20;    // line 13
```

Predicate (lines 129–130):

```text
if (request.RequestedQuantity > _limits.MaxPositionQuantity && IsIncreasing(request.Action))
    return Reject(..., "MAX_POSITION_QUANTITY");
```

Facts:

| Fact | Implication |
|---|---|
| Comparison is **strict `>`** | **`RequestedQuantity = 5.00` is APPROVED.** The cap is “more than 5”, not “at most 5 − ε”. |
| Increasing only (`OpenExposure` / `IncreaseExposure`) | A 5-lot open is legal. Reduce/close ignore this cap (correct for flatten). |
| Reject is **hard zero**, not `ReduceSize` | Overshoot (`5.01`) becomes qty `0`, not clip to 5. Architecture A23 §6.4 allows REDUCE_SIZE; code does not. |
| Units **unnamed** | Field is `decimal`. No `lots` / `oz` / dest-convention enum. Lab tests treat it as the same unit as `RequestedQuantity` (source-lot-shaped today). |
| Default `RiskEngine` | `new RiskEngine()` / `new RiskLimits()` — **no binder** from `appsettings` `RiskEngine:*`. |

Money defaults sitting next to it (same object):

| Limit | Default | How many $1 XAU moves at **5.00 lots** (500 oz) |
|---|---:|---|
| `MaxLossPerTrader` | **500** | **1** |
| `MaxDailyExecutionLoss` | **2,000** | **4** |
| `MaxPortfolioDrawdown` | **3,000** | **6** |

XAUUSD routinely prints **$10–$30** in a session and **$50–$100** on news. The dollar caps and the 5-lot qty cap **cannot both be honest**. One of them is decoration.

### 1.2 Copy path bakes the same 5 into dest max

File: `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`

```text
AllocationFactor = 0.05m
GoldSpec         = InstrumentQuantitySpec(Min=0.01, Max=5, Step=0.01, Precision=2)
qty              = QuantityNormalizer.Normalize(trade.MaxVolumeLots, 0.05, GoldSpec)
```

`QuantityNormalizer` **clips up to dest `MaxQuantity`**. Source `100` lots × `0.05` = `5.00` dest lots — **legal**. Source `200` lots × `0.05` = `10` raw → **clipped to `5.00`**, still legal.

Then `RiskEngine.Evaluate` sees `RequestedQuantity = 5.00`, which is **not** `> 5`, so **`MAX_POSITION_QUANTITY` does not fire**.

Two independent “5”s agree to let a **500 oz** ticket through:

| Knob | Value | Role |
|---|---:|---|
| `RiskLimits.MaxPositionQuantity` | **5** | risk reject (strict `>`) |
| `GoldSpec.MaxQuantity` | **5** | dest grid clip |
| `AllocationFactor` | **0.05** | 5% of **source lots**, not a dest-lot cap |

S026 already said: applying `0.05` to raw MT5 lots is still the §38 crime, just smaller. This slot adds: **even after that 5%**, dest max/`MaxPositionQuantity` will **re-inflate** large source books back to **5 dest lots**.

### 1.3 Other “position size” numbers that are not this field

| Source | Number | Bound to `RiskLimits`? |
|---|---:|---|
| `apps/api/appsettings.json` `RiskEngine:MaxPositionSize` | **10.0** | **No** |
| `SettingsController` GET default | **10.0** | Writes Redis `settings:risk:max_position_size` only |
| `docs/risk.md` | **50 lots** | **No** (stale marketing; P500_S003) |
| Unit fixture `RiskEngineTests.Base` | **0.10** requested | Interior point; **does not pin** the 5 default |
| Architecture §39 | name only (`max position quantity`) | No numeric law |

If an operator reads the settings API they see **10**. If they read `RiskLimits` they see **5**. If they read `docs/risk.md` they see **50**. All three are **blow-up** on retail gold. None is `0.05–0.10`.

### 1.4 Book-level amplifiers

`MaxPositionQuantity` is **per request**, not book.

| Combo that still passes qty + gross (increasing, empty book then stacked) | Dest lots | Ounces | $ / $1 move |
|---|---:|---:|---:|
| One ticket at the cap | 5 | 500 | **500** |
| Four × 5-lot tickets (`MaxXauGrossExposure = 20`) | 20 | 2,000 | **2,000** |
| Twenty 1-lot tickets (`MaxOpenPositions = 20`, gross clips at 20) | 20 | 2,000 | **2,000** |

`CurrentGrossXau` / `CurrentNetXau` in `CopyTradingService.GenerateShadowIntentsAsync` are **hardcoded `0`**. Even the 20-lot gross gate is **not live-evaluated** on that path. The only numeric qty backstop that can fire on a single ticket is `MaxPositionQuantity` / dest max — both **5**.

`MAX_XAU_NET` claims `ReduceSize` and still sets `ApprovedQuantity = 0` (P500_S003). No stepped residual.

---

## 2. Why 5 lots of gold is enormous on retail Pepperstone

Convention used below (same as A43 / P500_S026 / P500_S029; **confirm on dest SecurityList before any send**):

```text
1.00 standard lot XAUUSD  ≈  100 troy oz
P&L USD                   =  ΔUSD/oz × ounces
```

Do **not** assume `100` at send time (A43 §1.1: mini/`10`, cent, suffix). For **this policy note**, 100 oz/lot is the **standard retail CFD** that makes 5 lots look as small as the identifier “5” suggests. Mini (`10` oz/lot) would make 5 lots = 50 oz — still 5–10× the proposed working cap.

### 2.1 Notional and tick risk (lab fixture mid `$2400` from `RiskEngineTests`)

| Dest size | Ounces | Notional @ $2400 | $ per $1/oz | $ per $10 | $ per $30 (session) | $ per $80 (news) |
|---|---:|---:|---:|---:|---:|---:|
| **0.05 lot** (working floor of band) | 5 | $12,000 | **$5** | $50 | $150 | $400 |
| **0.10 lot** (working ceiling until proven) | 10 | $24,000 | **$10** | $100 | $300 | $800 |
| 0.10 lot (unit-test fixture qty) | 10 | $24,000 | $10 | $100 | $300 | $800 |
| 1.00 lot | 100 | $240,000 | $100 | $1,000 | $3,000 | $8,000 |
| **5.00 lots (current default / dest max)** | **500** | **$1,200,000** | **$500** | **$5,000** | **$15,000** | **$40,000** |
| 20 lots (`MaxXauGross`) | 2,000 | $4,800,000 | $2,000 | $20,000 | $60,000 | $160,000 |

P&L **per dollar** does not depend on spot. Raising gold to $3,300 only raises **notional / margin**, not the $500-per-dollar at 5 lots.

### 2.2 Versus the engine’s own money caps

| Event at **5.00 lots** | P&L | Which default it spends |
|---|---:|---|
| **$1** adverse (noise) | −$500 | **all** of `MaxLossPerTrader` |
| **$4** adverse (quiet hour) | −$2,000 | **all** of `MaxDailyExecutionLoss` |
| **$6** adverse | −$3,000 | **all** of `MaxPortfolioDrawdown` |
| **$10** adverse (ordinary session) | −$5,000 | **past every money cap** — qty gate already allowed the ticket |

A cap that lets you open a size whose **first dollar** of adverse move is the entire per-trader loss budget is not a risk limit. It is a **label**.

At **0.05 lot**, the same table:

| Event | P&L | vs $500 / $2,000 / $3,000 |
|---|---:|---|
| $1 | −$5 | 1% of trader cap |
| $10 | −$50 | 10% of trader cap |
| $30 | −$150 | 30% of trader cap |
| $80 | −$400 | still inside $500 |
| $100 | −$500 | **equals** trader cap — news-sized, not noise-sized |

At **0.10 lot**, $50 adverse = $500 (trader cap). That is the **top** of “until proven”, not a place to start.

### 2.3 Margin (order-of-magnitude, not a Pepperstone measurement)

Retail gold leverage is often **1:20–1:100** (broker/product specific; **not** measured on dest 1369850 in this slot).

| Size | Notional @ $2400 | Margin @ 1:100 | Margin @ 1:20 |
|---|---:|---:|---:|
| 0.05 | $12k | ~$120 | ~$600 |
| 0.10 | $24k | ~$240 | ~$1,200 |
| **5.00** | **$1.2M** | **~$12,000** | **~$60,000** |

A typical small retail / first-money Pepperstone book is **not** sized to warehouse **$1.2M** gold notional. Five lots is **institutional-ticket** risk wearing a **single-digit** name.

`MaxMarginUsage = 0.70` cannot save this: `CopyTradingService` passes `MarginUsage = 0`. The 70% gate is dead on that path.

### 2.4 Spread tax at the current loose spread cap

P500_S029: `MaxAllowedSpread = 2.0` is **USD/oz**. Entry half-spread cost ≈ width × ounces.

| Dest size | Cost at 0.20 wide | Cost at 2.00 (the cap) |
|---|---:|---:|
| 0.05 lot (5 oz) | $1 | $10 |
| 0.10 lot (10 oz) | $2 | $20 |
| **5.00 lots (500 oz)** | **$100** | **$1,000** |

The engine will **approve** a 5-lot open into a **$2.00** book. That is a **$1,000** instant haircut — **2×** `MaxLossPerTrader` — before the trade even moves. Working cap 0.05–0.10 makes the same haircut **$10–$20**.

### 2.5 Source-lot passthrough still lurks

`EfTradingStore` demo-shadow still writes:

```text
RequestedQuantity = trade.MaxVolumeLots
```

That path **does not** go through `AllocationFactor` / `GoldSpec`. Achiever challenge books print **1–10+ lots** of gold while hunting a pass (P500_S004). A 5-lot **source** ticket, if ever identity-mapped to dest, is already at the blow-up cap. A 6-lot source ticket would reject (`>` 5) — so the engine’s idea of “too big” starts **after** retail catastrophe.

`QuantityNormalizer` at `allocationFactor = 1` still passthroughs `0.10 → 0.10` (A43 / G7 FAIL). The 5 dest-max is the only clip.

---

## 3. 5.0 vs 0.05–0.10 — same digit family, 50–100× risk

```text
5.00 / 0.05  =  100×
5.00 / 0.10  =   50×
```

| | Blow-up default | Working band (this report + S026) |
|---|---|---|
| Dest lots (Lots, 100 oz) | **5.00** | **0.05–0.10** |
| Ounces | 500 | 5–10 |
| $ / $1 | $500 | $5–$10 |
| Fits `$500` trader cap | **No** (1 tick of $1 spends it) | **Yes** until ~$50–$100 move |
| Role | abort / “never this big on retail gold” | first-money dest size until ≥30 shadow days dest-net+ (S026) |
| After proof | still too big for small retail unless equity / margin measured | may lift **slowly**; do not jump to 5.0 |

**5.0 is a blow-up cap, not a working cap.** Naming it `MaxPositionQuantity` does not make it a size you should ever **request**.

S026 already required persisting **both**:

```text
gold_dest_lot_cap = 0.05          # Lots-convention
gold_dest_cap_oz  = 5             # canonical
```

This slot extends the **until-proven** band to **`0.10` lots / 10 oz** as a **ceiling**, not a start. Start at **0.05**. Lift toward **0.10** only after measured dest-net. Never treat **5.0** as the next step.

CLOSE / REDUCE still ignore allocation and this working cap (A43 §4.7 / A71): flatten the **mapped dest remainder**, do not send `sourceLots * 0.05`.

---

## 4. What would actually lower loss (policy only — no product edit)

Ordered. None of this is implemented as a bound options object.

1. **Working dest cap `0.05` lots (5 oz) on XAU OPEN/INCREASE.** Reject or `ReduceSize`+re-floor if the converted dest qty exceeds it.
2. **Until-proven ceiling `0.10` lots (10 oz).** Config must refuse values above `0.10` until an audited lift (shadow-day gate, dest-net after costs).
3. **Keep `5.0` out of the working set.** If a hard abort remains, put it at `0.10` (or `0.20` after proof), not at `5`. A 5-lot abort that never fires on a 5-lot request is theater.
4. **Bind one number.** Delete the silent triad (`RiskLimits` 5 / `GoldSpec.Max` 5 / settings `MaxPositionSize` 10 / docs 50). Operator-visible value must be the working cap.
5. **Compare in ounces.** `RequestedQuantity` after §38 conversion, not source lots. Unverified dest convention → no send (A43).
6. **Evaluate live book.** Stop passing `CurrentGrossXau = 0`. `MaxXauGross = 20` lots is the same class of blow-up (2,000 oz). First-money gross/net should be **ounces in the 5–20 oz band**, not 10–20 **lots**.
7. **Do not raise `allocationFactor` to 1** while dest max is 5. That is how a 100-lot source challenge becomes a 5-lot dest death ticket.

Architecture A23 already allows `REDUCE_SIZE` on `max position quantity`. The working cap should **clip then re-quantize**, not only hard-reject, so a 0.20 converted ticket becomes 0.05–0.10 instead of a silent skip that invites a later “just raise the cap” hack.

---

## 5. Honesty / non-claims

| Claim | Truth |
|---|---|
| We blew 5 lots on Pepperstone | **No.** `NewOrderSingleImplemented = false`. Persist forces `AllowFixSend = false`. Dest real P&L **0 by absence**. |
| `5m` is proven ounces | **No.** Unit is unnamed; copy path treats it as dest-grid lots. |
| `GoldSpec` 0.01/5/0.01 is measured on Pepperstone | **No.** Hardcoded lab spec. Guessing dest min/max is the A43 crime. |
| Dollar table is live tape | **No.** Uses test fixture `$2400` and 100 oz/lot. Recalculate after SecurityList + QUOTE. |
| Money caps save you if qty is 5 | **No**, if those counters are stale/zero (they are on the generate path) **or** if the first print after send is a $10 spike. |
| This report changed defaults | **No.** Product not edited. |

`SAFE_BY_ABSENCE` today. The default is still a **loaded gun** the moment someone wires `35=D` and leaves `MaxPositionQuantity = 5`.

---

## 6. Evidence (read only)

| Path | What was used |
|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | Defaults L10–13; predicate L129–130; money caps L7–9; `Reject` zeros qty |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `AllocationFactor=0.05`, `GoldSpec` max **5**, `new RiskEngine()`, book fields hardcoded 0 |
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | Clips to dest `MaxQuantity` (5) |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Demo shadow `RequestedQuantity = MaxVolumeLots` (no alloc) |
| `D:\Prop\apps\api\appsettings.json` | `MaxPositionSize: 10.0` unbound |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Redis paint, not `RiskLimits` |
| `D:\Prop\docs\risk.md` | Stale **50 lots** |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | Fixture qty **0.10**, price **2400**; **zero** `MAX_POSITION_QUANTITY` facts |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §39 | Named limit, no number |
| `D:\Prop\reports\swarm\20260818\P500_S003_risk_loss_caps.md` | Reject catalog; qty cap untested / unwired to FIX |
| `D:\Prop\reports\swarm\20260818\P500_S026_tiny_allocation.md` | Working cap **0.05 lots / 5 oz** |
| `D:\Prop\reports\swarm\20260818\P500_S029_spread_units.md` | $ / oz × ounces; 100 oz/lot convention |
| `D:\Prop\reports\swarm\20260818\A43_position_sizing.md` | Never identity-map lots; ounces first |

`grep` `MaxPositionQuantity` under `D:\Prop\src`: **declaration + one compare**. No test. No options bind.

---

## 7. One-line pin

```text
RiskLimits.MaxPositionQuantity = 5  (and GoldSpec.MaxQuantity = 5)
    = 500 oz gold
    = $500 per $1
    = blow-up on retail Pepperstone

Working dest cap until proven: 0.05–0.10 lot (5–10 oz).
Start 0.05. Never treat 5.0 as a size to copy.
Product not edited this slot.
```
