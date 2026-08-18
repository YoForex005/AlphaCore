# P500_BOOK_8 — Kill switch `MaxDailyExecutionLoss=2000` / `MaxLossPerTrader=500` are loss caps, not an edge

| Field | Value |
|---|---|
| Slot | **8** |
| Agent | P500_BOOK_8 (senior quant / trading-systems) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_8.md` |
| Topic | Kill-switch dollar tripwires `MaxDailyExecutionLoss = 2_000` and `MaxLossPerTrader = 500` are **loss caps**, not a profit edge |
| Product source modified | **No.** This report is the only product-adjacent write. |
| Test source modified | **No.** |
| Live `35=D` sent | **No.** No NewOrderSingle built. `REAL_COPY` not flipped. |
| Secrets printed | **None** |
| Live HTTP this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **not reachable from this agent** (SSRF block on loopback). Book integers are the P500 pin, not a re-probe. |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs`, `CopyTradingService.cs`, `BaselineScorer.TraderStateMachine`, `EfDashboardQueries.GetRiskAsync` / `GetOverviewAsync`, `SettingsController`, `docs/risk.md` |
| Binding | Architecture v2 §3 / §39 / §40 / §41; A23; A48; A71 G20–G23; P500_PROFIT_SYNTHESIS; siblings S003, S007, S019, S040, S054, S055 |
| Method | Full `read_file` of `RiskEngine.cs` (189 lines), copy-path `Evaluate` request, scorer `FromBaseline`, dashboard risk DTO, settings controller, unit tests. Grep of product `*.cs` for `MaxDailyExecutionLoss` / `MaxLossPerTrader` / `RISK_BLOCKED` / `8463`. No Manager attach. No `.env` dump. |

**Honesty rule:** wanting profit does not create an edge. A $500 / $2,000 tripwire is the **already-lost** number. Copying all **8,463** logins would copy the `RISK_BLOCKED` left tail. `SAFE_BY_ABSENCE` is why dest PnL is still **$0**.

---

## 0. Verdict (binding)

**LOSS_CAPS_NOT_EDGE.** `MaxLossPerTrader = 500` and `MaxDailyExecutionLoss = 2_000` are **after-the-fact dollar floors**. They do not select winners, do not reject `RISK_BLOCKED`, do not measure destination PnL on the live copy hop, and do not persist a kill-switch latch. They are **not** a trading edge.

| Claim | Measured class |
|---|---|
| These two numbers are a kill switch | **NO** — they are numeric predicates inside `Evaluate`. `KillSwitchMode` is a **separate exclusive enum**. Hitting `$2,000` emits reason `MAX_DAILY_EXECUTION_LOSS` / `GlobalStop` and does **not** write `KillSwitch.Mode = StopNewExecution`. |
| These two numbers are an edge | **NO** — edge is `E[dest PnL \| filter] > 0` after venue costs. A cap that fires when dest (or the caller-supplied field) is already `<= -$500` / `<= -$2,000` has **already spent** that money. |
| Copy-all 8,463 logins is profitable if the caps are on | **NO** — copy-all EV is the scored XAU book **−$154,425**, driven by `RISK_BLOCKED` **−$241,580**. Caps do not read `TraderState`. |
| Caps protect Pepperstone today | **NO** — dest is unharmed only because there is **no sender** (`NewOrderSingleImplemented=false`, persist `AllowFixSend=false`, `CanPromoteToLive => false`). |
| Caps are even **wired as dest daily / dest trader loss** | **NO** — copy hop hard-sets `DailyExecutionPnl = 0` and feeds `TraderRealizedLoss = min(0, source trade.NetRealizedPnl)`. The $2,000 dest daily line is **dead** on the product path. |
| Caps allow a losing dest to exit | **UNSAFE if live** — predicates apply to **every** action, including `CloseExposure`. A71 G21–G22 require **Approve** on reduce/close. Code `Reject`s. Red day **cannot exit**. |
| Working dest daily if send ever exists | **$200–$500 then `STOP_NEW_EXECUTION` only** (synthesis Stage D). Lab `$2,000` is too loose vs a legal 5-lot gold ticket. Never flatten source MT5. |

```text
ALLOW:  keep REAL_COPY / 35=D off; treat $500 / $2000 as labels, not edge;
        if a sender ever exists: dest daily $200–500 → STOP_NEW only;
        never copy RISK_BLOCKED; never copy all 8463.
FORBID: flip REAL_COPY because “we have a $2000 kill switch”;
        treat MaxLossPerTrader as a quality filter;
        auto-flatten dest or source on these thresholds;
        freeze dest exits once the cap is hit.
```

**One-line:** loss caps bound how much you have **already donated**. They do not create expectancy. Copying 8,463 challenge logins copies the `RISK_BLOCKED` −$241k tail onto one Pepperstone login.

---

## 1. What the code actually is (measured)

### 1.1 Defaults live only on `RiskLimits`

```5:22:D:\Prop\src\Domain\Risk\RiskEngine.cs
public sealed class RiskLimits
{
    public decimal MaxLossPerTrader { get; init; } = 500m;
    public decimal MaxDailyExecutionLoss { get; init; } = 2_000m;
    public decimal MaxPortfolioDrawdown { get; init; } = 3_000m;
    public decimal MaxXauGrossExposure { get; init; } = 20m;
    public decimal MaxXauNetExposure { get; init; } = 10m;
    public decimal MaxPositionQuantity { get; init; } = 5m;
    public int MaxOpenPositions { get; init; } = 20;
    public decimal MaxAllowedSpread { get; init; } = 2.0m;
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
    public decimal MaxPriceMove { get; init; } = 3.0m;
    public decimal MaxSlippage { get; init; } = 1.5m;
    public decimal MaxMarginUsage { get; init; } = 0.70m;
    public bool BlockMartingale { get; init; } = true;
    public bool BlockAbnormalSizing { get; init; } = true;
}
```

Architecture §39 names the limits (`max loss per selected trader`, `max daily execution-account loss`). It does **not** assign 500 / 2,000. Those are constructor defaults. `new RiskEngine()` / `CopyTradingService` `private readonly RiskEngine _risk = new();` use them. **No binder** from `appsettings.json` `RiskEngine:*`.

### 1.2 Tripwires fire after the money is gone, on every action

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

Facts measured in this file:

| Fact | Implication |
|---|---|
| Comparison is `<= -cap` | The field must **already be** −500 / −2,000. The rejected ticket is not the first dollar lost; it is the dollar **after** the floor. |
| No `IsIncreasing` guard | Unlike quote/spread/qty/martingale checks, these three apply to **OPEN, INCREASE, REDUCE, and CLOSE**. |
| `Reject()` always `ApprovedQuantity = 0`, `AllowFixSend = false` | Close of a mapped dest is **blocked** once the cap is hit. Dest is stuck. |
| Outcome `PauseTrader` / `GlobalStop` | DTO labels only. Nothing persists a trader pause or `KillSwitchMode.StopNewExecution`. |
| `RiskEvaluationRequest` has **no** `TraderState` | `RISK_BLOCKED` cannot trip these lines. |

`Reject` implementation (qty 0, no send):

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

A71 G21 / G22 (binding policy, **not** implemented):

| Gate | OPEN / INCREASE | REDUCE / CLOSE |
|---|---|---|
| G21 `MAX_LOSS_PER_TRADER` | Block + `PAUSE_TRADER` | **Approve** — do not trap a losing dest |
| G22 `MAX_DAILY_EXECUTION_LOSS` | Block + `GLOBAL_STOP` (stop-new **only**) | **Approve** — flatten remains a human action |

Code **fails G21–G22** on the close family. That is the opposite of “kill switch protects capital.” After the dest is red, the engine **forbids the exit**.

### 1.3 These caps are not the kill switch

Kill-switch law (§40 / A48) is two **independent** dest controls: `STOP_NEW_EXECUTION` (leave dest book) and `EMERGENCY_FLATTEN` (dest-only, SuperAdmin + confirm). Product stores one exclusive enum:

```1:8:D:\Prop\src\Domain\Enums\KillSwitchMode.cs
namespace TraderIntelligence.Domain.Enums;

public enum KillSwitchMode
{
    None = 0,
    StopNewExecution = 1,
    EmergencyFlatten = 2
}
```

`Evaluate` reads that enum **separately** (lines 78–82) and then, on any **approve**, requires `KillSwitch == None` before `AllowFixSend`. Dollar caps never write the entity. Seed is fail-open `Mode = None` (`BrokerCatalogSeed` L67–73).

Copy hop **forces** the latch off:

```173:176:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    KillSwitch = KillSwitchMode.None,
                    TraderRealizedLoss = Math.Min(0m, trade.NetRealizedPnl),
                    DailyExecutionPnl = 0,
                    PortfolioDrawdown = 0,
```

So on the only product caller of `Evaluate`:

| Input | Value on copy hop | Can `$500` / `$2,000` fire? |
|---|---|---|
| `KillSwitch` | **always `None`** | Latch never consulted as ON |
| `DailyExecutionPnl` | **always `0`** | `0 <= -2000` is false. **`MAX_DAILY_EXECUTION_LOSS` cannot fire.** |
| `PortfolioDrawdown` | **always `0`** | `MAX_PORTFOLIO_DRAWDOWN` cannot fire |
| `TraderRealizedLoss` | `min(0,` **this source trade** `.NetRealizedPnl)` | Fires only if **that one source ticket** lost ≥ $500. Not dest-trader cumulative. Not dest-day. |
| `CurrentGrossXau` / `Net` / `OpenPositions` / `MarginUsage` | **0** | Book caps also blind |

`AllowFixSend` is then **overwritten false** on persist (`RiskDecisionRecord.AllowFixSend = false`, L192). `NewOrderSingleImplemented = false`. No `35=D` builder on the copy hop.

Dashboard risk tile is the same lie in the other direction — it cannot show dest daily PnL at all:

```198:208:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<RiskDashboardDto> GetRiskAsync(CancellationToken ct)
    {
        var ks = await _db.KillSwitches.OrderByDescending(k => k.UpdatedAt).FirstOrDefaultAsync(ct);
        // ...
        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), _runtime.RealCopyEnabled, rejects);
    }
```

`DailyPnl`, `Drawdown`, `XauLong`, `XauShort`, `XauNet` are constructor **zeros**. Overview `destinationRealPnl` is likewise a literal `0` (`GetOverviewAsync` L44).

### 1.4 Operator-facing numbers are a different, unbound catalog

| Surface | What it shows | Bound to `RiskLimits`? |
|---|---|---|
| `RiskLimits` defaults | **$500** / **$2,000** / **$3,000** | **This is the engine** |
| `apps/api/appsettings.json` `RiskEngine` | `MaxDailyDrawdownPct=5.0`, `MaxPositionSize=10.0`, `KillSwitchEnabled=true`, `KillSwitchOn=false` | **No** |
| `SettingsController` GET | Same 5% / 10 lots / kill bool | Redis write only; `RiskEngine` does not read Redis |
| `docs/risk.md` | 5% daily / 10% total + **auto flatten** on total-loss | **Stale brochure.** Auto-flatten is unauthorized (A48 / S019). Do not implement. |

An operator who “turns on the kill switch” in settings does **not** arm `$2,000`. An operator who believes `$2,000` is armed on dest daily is reading a field the copy hop **zeros**.

### 1.5 Tests do not pin the caps

`tests/Unit/RiskEngineTests.cs` (5 facts): stale quote, `RealExecutionEnabled=false` never sends, stop-new blocks open not close, unreconciled, stale signal. `TraderRealizedLoss` and `DailyExecutionPnl` in the fixture are **0**. There is **no** fact for `MAX_LOSS_PER_TRADER` or `MAX_DAILY_EXECUTION_LOSS` (C03 / A89 listed them as expected; they are **missing**). The “kill-switch dollars” are untested.

---

## 2. Why a loss cap is not an edge

Edge, for this desk (architecture §3): **future destination-net PnL inside risk limits**, after Pepperstone spread / commission / slippage.

A rule of the form

```text
if already_lost >= K then stop
```

has these properties:

1. **It does not choose who to copy.** Selection happens (or fails to happen) **before** dest PnL exists. `FromBaseline` labels `RISK_BLOCKED`; `Evaluate` never reads that label.
2. **On the days it binds, dest PnL is already ≤ −K.** Expected contribution of those days is **≤ −K**, plus any further hole if exits are frozen (this engine) or if flatten prints the worst bid (docs/risk.md — do not code).
3. **On the days it does not bind, it does nothing.** Winners and losers that have not yet printed −K pass identically.
4. **K is a budget, not a forecast.** Raising K to “give the system room to make profit” **raises** max dest death. Lowering K lowers death and still does not create +EV.

Wanting the Pepperstone account to be profitable does not change (1)–(4). LoggedOn (`35=A`) does not change them. 8,463 source logins do not change them.

Numeric illustration at the **legal** dest size (`MaxPositionQuantity = 5` is **approved**, strict `>`; GoldSpec max is also 5; S040):

```text
1.00 lot XAU  ≈  100 oz     (lab / S040 convention; confirm on dest SecurityList before any send)
5.00 lots     =  500 oz
$1 / oz       =  $500   =  entire MaxLossPerTrader
$4 / oz       =  $2,000 =  entire MaxDailyExecutionLoss
$6 / oz       =  $3,000 =  entire MaxPortfolioDrawdown
```

XAUUSD routinely prints **$4** in a quiet hour and **$10–$30** in a session. One legal ticket can **spend the entire “kill switch”** before the predicate is allowed to fire. After it fires, close is rejected. That is a **blow-up ceiling**, not a working policy and not an edge.

Synthesis Stage D (still no send today): dest daily **$200–$500** then `STOP_NEW_EXECUTION` only; dest working cap **0.05** lot; dest net **0.15–0.30**. Lab `$2,000` next to a 5-lot qty cap is internally inconsistent.

---

## 3. Copying all 8,463 logins copies `RISK_BLOCKED` losses

### 3.1 Live book (P500 pin; this slot did not re-hit `:5000`)

Source: `P500_PROFIT_SYNTHESIS.md` §1 / `P500_S007_blocked_left_tail.md` (mid-scoring, Achiever). Prior manager census was **18 groups / 8,460** traders (Achiever 8/6512 + Starwave 10/1948). P500 live API counted **8,463** accounts (Achiever 6512 + Starwave ~1951). The +3 is **unreconciled**. Neither number is an edge.

| Metric | Value |
|---|---|
| Accounts | **8463** (P500 API) / **8460** (manager re-sum) |
| XAU traders with a score | **197** (Achiever only; climbing) |
| Traders with ≥3 completed XAU | **178** |
| `SHADOW` | **70**, source Σ **+$78,276**, **100% demo** |
| `WATCH` | **79**, source Σ **+$8,178** |
| `RISK_BLOCKED` | **29**, all `martingale=true`, source Σ **−$241,580** |
| All scored XAU source PnL | **−$154,425** |
| `LIVE` / `LIVE_CANDIDATE` | **0 / 0** |
| `INSUFFICIENT_DATA` | **~8284** |
| Starwave scored | **0** (phase `deals-done`) |
| Destination real PnL | **$0** (constructor literal) |
| Shadow PnL | **$0** (no quote tape) |

S054 arithmetic: remove `RISK_BLOCKED` and the same tape is **+$87,154 of source dollars** — still **not** dest expectancy (demo SHADOW + WATCH, uncosted). **The entire redness of the scored book is the blocked tail.**

### 3.2 What `RISK_BLOCKED` actually means

```194:195:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
```

It is a **scoring state**: high risk **or** (martingale ∧ drawdown ∧ **negative XAU net**). Copying it is copying a **losing, size-up-after-loss** XAU cluster. `CanPromoteToLive(_) => false`. Reachable set is `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no `LIVE`.

`RiskEngine.cs` grep this slot: `RISK_BLOCKED` = **0**, `TraderState` = **0**, `TRADER_RISK_BLOCKED` = **0**. A71 G20 (`RISK_BLOCKED` blocks **new** copy) is **MISSING**.

### 3.3 Copy-all vs current product path

| Policy | Includes `RISK_BLOCKED`? | Dest EV |
|---|---|---|
| Naive copy-all **8,463** logins | **Yes** — plus ~8,284 `INSUFFICIENT_DATA` and Starwave **unscored** | **−EV.** Blocked tail −$241k plus noise. Correlated gold on **one** dest login. |
| Copy every **scored** XAU (197) | **Yes** (29 blocked) | Source book **−$154,425** (S054) |
| Copy `SHADOW` only (product `copyable` set) | **No** (`CopyTradingService` L95: `SHADOW` / `LIVE_CANDIDATE` / `LIVE`) | Source +$78k **demo**; dest after spread **unknown**; no quote tape |
| Copy `RISK_BLOCKED` “because $500/$2000 will cut it off” | **Yes, until dest has already lost the cap** | Cap is the **loss**, not the filter |

Product today does **not** copy-all. `GenerateShadowIntentsAsync` filters to SHADOW/LIVE*/LIVE. LIVE is empty. SHADOW is all `demo\yo-2step` / `demo\yo-payp`. That is **not** dest profit (S004 / S054). It is also **not** a reason to spray the other 8,393 logins.

If someone “just copies everything we fetched,” they copy:

1. The 29 martingale losers (−$241,580 source).
2. Thousands of `INSUFFICIENT_DATA` first prints (trade #3 is luck; `CanPromoteToLive` is hard-false for a reason).
3. Starwave’s ~1,951 logins with **zero** scores.
4. One correlated XAU thesis onto one retail Pepperstone book (S018 / S055).

`$500` / `$2,000` would not stop (1)–(4) **as selection**. At 5-lot gold they would at best **mark the wreck** after a $4 move, then **refuse the close**.

---

## 4. Higher profit / lower loss (this slot)

Honesty again: these bullets are **filters and size**, not a promise. No send.

### Higher dest profit (only possible path)

1. **Do not copy all 8,463.** Catalog size is not a signal.
2. **Never copy `RISK_BLOCKED`.** That is the −$241k tail. Gate on `TraderState` **before** `Evaluate` (A71 G20). Do not wait for dest −$500.
3. Do not treat `SHADOW` +$78,276 or `earlyScore=95.5` as dest expectancy. All current SHADOW is demo. Dashboard `netSourcePnl` is **all symbols**; score is **XAU-only**.
4. Shadow **on a standing QUOTE tape** after a gold-specific cost haircut, hold-time floor, no lot-escalation, dest qty **≤ 0.05**. Thirty-plus green dest-shadow days **before** any `35=D`.
5. Keep `REAL_COPY_EXECUTION_ENABLED=false` and `CanPromoteToLive == false` until that tape exists. FIX `LoggedOn` is recon/quotes, not a fill.

### Lower dest loss (now, and if a sender is ever built)

1. **Now:** do not send. Dest PnL $0 is `SAFE_BY_ABSENCE`, not a working kill switch.
2. **Do not enable live copy of the scored book “because the $2,000 cap will save us.”** It will not. It is too loose, wrongly fed, and it freezes exits.
3. If a sender exists later: dest daily **$200–$500** → persist `STOP_NEW_EXECUTION` (not a one-shot `Reject` on the next tick). **Never** auto-flatten. **Never** flatten source MT5 (those 8,463 tickets are not ours).
4. `MAX_LOSS_PER_TRADER` / `MAX_DAILY_EXECUTION_LOSS` must **not** apply to dest reduce/close (G21–G22). Fix that **before** any live NOS.
5. Feed **destination** daily PnL and **destination** per-trader realized — not `0` and not a single source ticket.
6. Working gold size **0.05 lot**, not 5.00. At 0.05 lot a $4 move is **−$20**, so a $200–$500 daily latch is a real brake instead of a tombstone.

---

## 5. What this slot did **not** do

- Did not flip `REAL_COPY_EXECUTION_ENABLED`.
- Did not send or build `35=D` / NewOrderSingle.
- Did not flatten source or dest.
- Did not re-attach Manager; did not print secrets.
- Did not re-query `127.0.0.1:5000` (loopback blocked here). Book integers are the P500 pin (`P500_PROFIT_SYNTHESIS.md`, S007, S054).
- Did not treat dest PnL $0 as “flat and profitable.”

---

## 6. Binding one-liner

```text
$500 / $2,000 are loss caps, not an edge, not a kill-switch latch.
Copy-all 8463 = copy RISK_BLOCKED (−$241k) onto one dest.
Wanting profit does not create expectancy.
35=D stays OFF.
```
