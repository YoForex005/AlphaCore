# P500_S018 — Copying 70 SHADOW gold traders is one gold bet

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S018_correlation.md` |
| Agent | P500_S018 |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Law | Architecture §65; A60; A23 §6.4 / §10; A24 §9.2; A71; `RiskLimits` |
| Verdict | **70 SHADOW XAU books that fire the same side in the same minute are one thesis, not 70 edges.** v1 lower-loss substitute is already named: **cap destination net/gross XAU** (`RiskLimits.MaxXauNetExposure = 10`, `MaxXauGrossExposure = 20`) **and de-duplicate same-direction same-minute signals**. Full §65 cluster concentration is **Phase 2 after basic copy is stable** — do not implement now. |
| Remeasured | 2026-08-18 this pass |

## Profit implication

Copying 70 SHADOW gold names is **one gold bet × 70 tickets**. That multiplies dest spread/slippage, not edge. Per-trader `$500` loss still allows a cluster wipe. Lower loss = dest **net** cap (synthesis start **0.15–0.30**, not lab 10) + same-direction same-minute de-dup. Do not implement §65 clustering now.

**Remeasured hole:** `CopyTradingService.GenerateShadowIntentsAsync` always passes `CurrentGrossXau = 0`, `CurrentNetXau = 0`, `OpenPositions = 0`, `KillSwitch = None` (`CopyTradingService.cs` L173–179). The 10/20 caps **cannot bind**. 70 clones would all “approve” until other gates fire. `AllocationFactor = 0.05` still stacks.

---

## 0. Standing

A60 is binding: **do not implement clustering now.** This file does **not** reopen Engineering Phase 2 (§67 reconstruction), does **not** add `strategy_clusters`, and does **not** emit `CONCENTRATION_CAP`. It records why a 70-login SHADOW fan-out is still **one gold bet**, and which **already-specified** v1 controls actually cut loss.

Two different words (A60 §1.3):

| Word | Meaning | Status |
|---|---|---|
| `correlation_id` | Request / pipeline log id | v1 logging |
| Strategy correlation | “These N source logins are the same XAUUSD strategy” | §65 Phase 2 |

Never overload `correlation_id` as a cluster id.

---

## 1. Architecture §65 (verbatim)

Source: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §65.

```text
Do not copy 50 "different traders" if they are effectively the same XAUUSD strategy.

Track correlation by:

    direction
    entry time
    holding time
    return series
    session
    lot behavior

Add concentration caps.

Example:

    maximum allocation per correlated strategy cluster

This can be Phase 2 after basic copy execution is stable.
```

“50” is the architecture example. **70 SHADOW gold traders is the same failure mode, scaled.** The number of logins is not the number of bets.

§65 “Phase 2” ≠ Engineering Phase 2. A60 / A23 §10.1: concentration is **after** Engineering Phase 8 copy is stable (idempotent intents, stale quote/signal tested, sizing verified, kill switches tested, shadow sample measured, persist-before-send). Concentration is **not** a §68 go-live checkbox.

---

## 2. The one-gold-bet argument

v1 risk (A23 §6.4, A60 §2) can still approve N separately-selected “different” traders who:

- open XAUUSD **in the same direction**,
- **within the same minute** (often the same few seconds),
- hold for similar durations,
- print nearly identical return paths,
- trade the same session,
- size lots the same way.

On SHADOW that is still **one destination gold book**. `ShadowCopyEngine` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`) simulates **per-order** entry/exit/MTM. It does **not** roll 70 source logins into one thesis. A24 §9.2 *specifies* a shadow book with `gross_xau_qty` / `net_xau_qty` / `open_position_count`. That rollup is **not implemented** in the current engine class.

So:

```text
70 SHADOW gold traders × same side × same minute
    = 1 XAUUSD direction bet
    × 70 copies of the fill
    × 70 copies of the slippage
    × 70 copies of the news gap
```

If gold dumps, all 70 lose together. Leaderboard diversity is cosmetic. That is exactly A60 §2: book-level caps **bound the account** if they all fire together; they do **not** distinguish “70 independent edges” from “one signal with 70 logins.”

Per-trader `MaxLossPerTrader` also misses the cluster: 70 × a small per-login loss is still one large gold loss.

Scoring `suggested_allocation` is advisory only. Scoring must not become the cap (A60 §2, §39).

---

## 3. v1 substitute — cap net XAU (and keep gross)

A60 §2 v1 table (keep; do not remove when Phase 2 lands):

| v1 control | What it does | What it does not do |
|---|---|---|
| `max XAUUSD gross / net` | Caps destination book | Treats 70 clones as 70 traders |
| `max loss per selected trader` | Caps one `(broker_id, login)` | Misses the cluster |
| `max number of open positions` | Caps count, not thesis | A clone army still shares one thesis |
| martingale / abnormal sizing | Per-trader behavior | Does not group lookalikes |
| `suggested_allocation` | Advisory | Must not become the cap |

Architecture §39 names both book caps as hard limits. A23 §6.4:

| Limit | Intended action |
|---|---|
| `max XAUUSD gross exposure` | `REDUCE_SIZE` or `REJECT` (`MAX_XAU_GROSS`) |
| `max XAUUSD net exposure` | `REDUCE_SIZE` or `REJECT` (`MAX_XAU_NET`) |

**Net is the lower-loss knob for the 70-clone long (or short) pile.** Gross still matters when the book is flat-but-huge (10 long + 10 short is still concentrated gold gamma). A60 §7.1: a cluster that is flat net but 10+10 long/short is still concentrated.

### 3.1 What is on disk today

`D:\Prop\src\Domain\Risk\RiskEngine.cs` — lab defaults, **not** production law (A23 §6, A72, D13):

```text
MaxXauGrossExposure = 20
MaxXauNetExposure   = 10
```

Evaluate (increasing only):

```132:136:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.CurrentGrossXau + request.RequestedQuantity > _limits.MaxXauGrossExposure && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "MAX_XAU_GROSS");

        if (Math.Abs(request.CurrentNetXau) + request.RequestedQuantity > _limits.MaxXauNetExposure && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.ReduceSize, "MAX_XAU_NET");
```

REDUCE / CLOSE are **not** blocked by these caps (A71 G26/G27 — correct family). Concentration, when it later exists, also must not block REDUCE/CLOSE (A60 §3.2, §4.2).

Units: destination XAU quantity after sizing normalize/step (A43 / A60 §7.1). Do **not** treat `20` / `10` as “20 traders” or “10 dollars.” They are book ounces/qty on the execution (or shadow) account.

These numbers are **code defaults**. No product binder maps env / `appsettings` / `PATCH /api/v1/risk/limits` into `RiskLimits` (D13). A23 §6: do not freeze production ounces in source. Keep 10/20 as the **named lab floor** for the lower-loss story; measure before go-live.

### 3.2 Arithmetic of 70 clones

Assume each selected SHADOW login requests `q` destination XAU on the same side in one burst, and the caller actually accumulates `CurrentNetXau` / `CurrentGrossXau` across the book (today the engine only *reads* those fields — the caller must supply a real book).

| Per-login `q` | 70 × same-side stack | vs `MaxXauNet = 10` | vs `MaxXauGross = 20` |
|---|---|---|---|
| 0.10 | 7.0 | under net | under gross |
| 0.15 | 10.5 | **net binds** | under gross |
| 0.30 | 21.0 | net binds first | gross binds later |
| 1.00 | 70.0 | net binds after ~10 qty | gross after ~20 |

Without de-dup, the cap is the **only** binder. First intents approve; later ones should hit `MAX_XAU_NET` then `MAX_XAU_GROSS`. That bounds **ounces**, not **how many times we pay the same spread/slippage on the same print**.

If the caller passes `CurrentNetXau = 0` on every intent (identity-blind, no book rollup), **all 70 approve**. D13: `BrokerId` / `SourceLogin` unused; decisions are identity-blind. Shadow engine does not maintain the A24 book. **That is the live hole for “70 SHADOW = 70 fills.”**

`MaxOpenPositions = 20` (default) would reject opens 21–70 **if** `OpenPositions` is the real book count. That is a count cap, not a thesis cap, and it is untested against a 70-clone burst.

### 3.3 Measured defects on the v1 substitute (do not paper over)

| Defect | Evidence | Why it matters for 70 clones |
|---|---|---|
| `MAX_XAU_NET` returns `ReduceSize` **via `Reject()`** → `ApprovedQuantity = 0` | RiskEngine lines 135–136, 180–188; A71 §14; B13; D13 #16; E005 B16 `STUB_WRONG` | Spec wants a positive stepped remainder. Today it is a silent full reject labeled ReduceSize. Bound still “works” as a hard stop, but sizing-to-remaining-net is a lie. |
| Gross is hard `Reject`, no reduce-to-room | line 132–133; A23 §6.4 allows REDUCE_SIZE | First overshoot drops to 0 instead of filling remaining room. |
| Gross add is **side-blind** | `CurrentGrossXau + RequestedQuantity` | Correct for gross; net uses `\|net\| + qty` which also ignores whether this intent **reduces** net (a short against a long book should loosen net, not tighten it). |
| `MaxSlippage` never read | D13 | 70 clones still each pay modeled delay slip (`ShadowCopyEngine.DefaultLatencySlippagePoints = 0.05`). |
| No unit facts for rules 15–16 | `RiskEngineTests.cs` never sets `CurrentGrossXau` / `CurrentNetXau` off 0; C03 / D35 / E017 **Missing** | The lower-loss cap is unproven. |
| No DI / config bind | D13 | 10/20 exist only as `new RiskLimits()` defaults. |
| No cluster step | A23 §5 step 13 reserved; correctly unused | Expected. Do not fill it in this increment. |

Honesty: **the 10/20 caps are the right v1 law and they are only half-wired.** They do not yet make a 70-clone burst safe.

---

## 4. Second lower-loss control — de-duplicate same-direction same-minute signals

This is **not** Phase 2 clustering. It is a **cheap v1 proxy of two of the six §65 axes**:

| §65 axis | v1 de-dup proxy | Full Phase 2 (A60 §5) |
|---|---|---|
| **direction** | same Buy/Sell (or same `CopyIntent` side) | `long_frac` / `short_frac` / flip rate over history |
| **entry time** | same UTC minute (or `ENTRY_SYNC_WINDOW`) | pairwise fraction of entries inside the window |
| holding time | **out of scope for v1 de-dup** | median/p10/p90/CV |
| return series | **out of scope** | Pearson + sign agreement (offline) |
| session | **out of scope** | 4-bucket histogram |
| lot behavior | **out of scope** | lot CV, martingale rate |

A60 §5.2 already named the window:

```text
ENTRY_SYNC_WINDOW  — provisional discussion default:
    5–30 seconds   “same signal”
    1–5 minutes    “same headline”
```

**Do not freeze a production number in product source now** (A60). For this report’s operator story, **same UTC minute** is the honest, reviewable bucket: if 70 SHADOW gold logins open long between `12:04:00Z` and `12:04:59Z`, that is one signal.

### 4.1 Recommended de-dup contract (design only — not implemented)

Identity of a burst key:

```text
dedup_key = (
    canonical_symbol,          // XAUUSD
    direction,                 // Long | Short
    utc_minute                 // floor(source_event_time, 1 minute)
)
```

Policy when N>1 intents share `dedup_key` on the SHADOW (or later LIVE) book:

| Option | Behavior | Use |
|---|---|---|
| **A. First-wins** | Approve at most one OPEN/INCREASE per key; rest `REJECT` with a reserved reason (suggested later: `SIGNAL_DEDUP_SAME_MINUTE`, **not** `CONCENTRATION_CAP`) | Lowest loss, simplest |
| **B. Cap-to-one-budget** | Sum of approved qty on that key ≤ `min(MaxXauNet remaining, per-burst cap)` | Keeps size, still one bet |
| **C. Collapse to best login** | Keep the highest-score / already-selected primary; drop clones | Needs a deterministic rank; scoring still must not send |

**Prefer A or B.** Do not invent a second risk engine. Seat: **inside** `RiskEngine` or immediately before it, same persist-before-send, same `risk_decisions`. A60 §3.2: no sidecar *after* `ApprovedExecutionIntent`.

Do **not** de-dup REDUCE/CLOSE. Closing 70 mapped dest legs is risk reduction (A71, §64).

Do **not** use destination cTrader quotes as the sync clock. Use `source_event_time` (A60 §5.2). Stale-signal rejection (`MaxSourceSignalAge = 15s`) stays independent.

Do **not** treat `login` as globally unique. Key may later include nothing from login (that is the point) but membership stays `(broker_id, login)`.

### 4.2 Why de-dup + net cap together

```text
de-dup same-direction same-minute
    → at most one (or one budget) copy of the print
    → you do not pay 70 spreads on one tick

MaxXauNet = 10  (lab)
    → even a unique (non-clone) pile of gold cannot exceed 10 dest qty net

MaxXauGross = 20  (lab)
    → long+short two-way still cannot exceed 20 dest qty gross
```

Either control alone is insufficient:

- Caps only: 70 clones still each try to fill until ounces run out — many tickets, same thesis, more slippage/partial fills.
- De-dup only: one “unique” trader can still size past a sane gold book.
- Neither: 70 × q is an unbounded gold pile (and today’s caller can pass zero current exposure).

---

## 5. What must stay Phase 2 (do not build in this increment)

A60 §3.1 / V1-1…V1-11 remain in force:

```text
[DO NOT] Create strategy_clusters / membership tables
[DO NOT] Write a correlator job or pairwise similarity service
[DO NOT] Call clustering from RiskEngine
[DO NOT] Reject live or shadow copy for missing cluster_id
[DO NOT] Add CONCENTRATION_CAP to any v1 test as an expected production reason
[DO NOT] Add a dashboard “Clusters” nav item
[DO NOT] Train XGBoost / LLM / embeddings to invent clusters
[DO NOT] Put Kafka / a mesh / ClickHouse under this feature
[DO NOT] Modify product source to leave a stub that compiles against nothing
```

When basic copy is stable, concentration is **one layer inside** the existing engine (A23 step 13, flag default **off**):

```text
CopyIntent
    → RiskEngine steps 1–12          (v1, including XAU gross/net)
    → step 13 Concentration          (Phase 2, flag-gated)
    → persist risk_decision
    → ApprovedExecutionIntent
```

Offline correlator builds membership from reconstructed XAU lifecycles (six axes). Risk only *reads* a versioned snapshot. Pairwise work is **not** on the FIX/MT5 hot path.

Young / first-3 traders stay singletons (A60 §6.5). Cluster membership is **not** a promotion signal to LIVE.

---

## 6. Shadow vs live

| Path | Today | Lower-loss rule |
|---|---|---|
| SHADOW | Observe-only; `RealExecutionEnabled` default false; `AllowFixSend` false unless real flag + kill none + reconciled + healthy | **Still apply net/gross + de-dup on the shadow book.** Otherwise shadow P&L is a 70× gold fantasy and will greenwash promotion. |
| LIVE | `REAL_COPY_EXECUTION_ENABLED` remains explicit and default false (A49) | Same engine. Do not learn cluster thresholds on live. |
| Phase 2 later | `CONCENTRATION_CAPS_SHADOW_ENABLED` may turn on first (A60 §7.3) | Measure on shadow; live flag stays off until measured. |

Copying 70 SHADOW traders without a shadow **book** is not “safe because paper.” It is how a false diversification story gets into a LIVE flag review.

---

## 7. Honesty scorecard (measured)

| Claim | State |
|---|---|
| §65 / A60 exist and forbid 50 lookalikes | **Yes.** On disk. |
| Full correlation / concentration implemented | **No.** Phase 2 hooks only. Correct. |
| `RiskLimits.MaxXauNetExposure = 10` / `MaxXauGrossExposure = 20` | **Yes**, lab defaults in `RiskEngine.cs`. |
| Those caps bound a 70-clone burst in production | **No.** Caller-supplied book; no tests; net ReduceSize is qty 0; no config bind. |
| Same-direction same-minute de-dup | **Missing.** A60 named `ENTRY_SYNC_WINDOW`; no product type. |
| `ShadowCopyEngine` aggregates 70 logins | **No.** Per-fill only. |
| 70 SHADOW gold traders = 70 independent bets | **False.** One gold bet. |
| Product source changed by this agent | **No.** |

---

## 8. Later work (when scheduled — not this file)

Order that actually lowers loss, cheapest first:

1. **Wire a real shadow/live XAU book into `CurrentNetXau` / `CurrentGrossXau` / `OpenPositions`** so 10/20 can bind. Add the missing unit facts (E005 B15/B16).
2. **Fix `MAX_XAU_NET`**: `ReduceSize` must return a positive stepped remainder or `REJECT` + `SIZE_BELOW_MIN`, not `ApprovedQuantity = 0` under a ReduceSize label. Consider side-aware net (a hedge should not look like more net).
3. **Same-direction same-minute de-dup** (this report §4.1), persist reason, do not reuse `CONCENTRATION_CAP`.
4. Bind 10/20 (and any de-dup window) through the audited limits document — do not leave silent code constants as production law.
5. Only after Phase 8 + §68 + §70: A60 correlator + step 13 cluster caps, default off, shadow first.

Until (1)–(3) exist, selecting 70 SHADOW gold traders is **one uncapped-looking gold pile with a cosmetic login count.**

---

## 9. Sources

| Path | Role |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §39, §64, §65 | Hard limits; exposure families; copy correlation |
| `D:\Prop\reports\swarm\20260818\A60_correlation_phase2.md` | Phase 2 hooks; v1 substitute; `ENTRY_SYNC_WINDOW` |
| `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` §6.4, §10 | Book caps; concentration reserved |
| `D:\Prop\reports\swarm\20260818\A24_shadow_copy_spec.md` §9.2 | Shadow gross/net book (spec) |
| `D:\Prop\reports\swarm\20260818\A71_exposure_policy.md` | OPEN vs CLOSE; G26/G27; net ReduceSize defect |
| `D:\Prop\reports\swarm\20260818\D13_risk_review.md` | Measured Evaluate order |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `MaxXauNetExposure=10`, `MaxXauGrossExposure=20` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Per-order shadow only |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | No net/gross burst facts |

**End of P500_S018.**
