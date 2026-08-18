# P500_BOOK_6 — Copying prop-challenge demo accounts is adverse selection

| Field | Value |
|---|---|
| Slot | **6** |
| Date | 2026-08-18 |
| Agent | P500_BOOK_6 (senior quant/engineer; challenge-book selection only) |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_6.md` |
| Assigned | Copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Measured evidence for **higher profit / lower loss**. Honesty: wanting profit does not create an edge. Copying all **8463** logins would copy `RISK_BLOCKED` losses. |
| Product source modified | **No.** Report only. |
| Live `35=D` / NewOrderSingle | **Not sent. Not implemented on the copy hop.** |
| `REAL_COPY` flipped | **No.** |
| Secrets printed | **None.** |
| This-slot live HTTP | **Not re-probed.** `GET http://127.0.0.1:5000/api/overview` and `/api/traders` are SSRF-blocked from this worker. Numbers below are **on-disk measured pins**, not a new Manager attach. |

**Honesty rule:** wanting higher profit and lower loss does not create an edge. Ranking 8463 challenge logins by source dollars or `earlyScore` is selection on a hurdle, not a Pepperstone expectancy. Copy-all is how you import the blow-up tail.

---

## 0. Verdict

**ADVERSE_SELECTION_CONFIRMED.**

The Manager-visible book is a **prop-challenge factory**, not a funded XAU desk.

| Claim | Measured result |
|---|---|
| Achiever is a challenge book | **100%.** 6512 / 6512 logins sit in `demo\yo-*` or `contest\yo-*`. **0** Achiever `real\` / funded groups. |
| Dominant group | `demo\yo-2step` **6295 / 6512 = 96.67%** |
| Combined demo+challenge share of 8460 | **8417 / 8460 = 99.49%** (Achiever 6512 + Starwave demo 1905) |
| Funded-ish pool | Starwave `real\FX3\*` **28 / 8460 = 0.33%**. Starwave scored **0** on the P500 pin. |
| Scored XAU book EV | **−$154,425** (197 names, mid-scoring) |
| `RISK_BLOCKED` tail | **29** names, **all `martingale=true`**, **−$241,580** |
| SHADOW head | **70** names, **+$78,276**, **100% demo** (`demo\yo-2step` + `demo\yo-payp`) |
| Destination real PnL | **$0** (overview constructor literal; no dest fills) |
| Copy-all 8463 includes blocked losses | **Yes**, if the catalog is the copy set. `GetTradersAsync` walks **every** `Mt5Accounts` row. Naive copy-all therefore copies the −$241k martingale bucket. |
| Wanting profit = edge | **No.** Quality awards **+15** for `NetPnl > 0`. That is the **pass-target** bonus. |

**Higher dest profit = do not copy the challenge universe.** **Lower dest loss = do not copy `RISK_BLOCKED`, do not copy-all 8463, do not send `35=D`.**

Risk to live dest capital from this slot: **NONE** (`SAFE_BY_ABSENCE` on the wire). Residual risk is **operator ranking**: treating 8463 / SHADOW / +$78k as a send list.

---

## 1. Measured universe (do not greenwash 8463 vs 8460)

### 1.1 Two census pins

| Pin | When | Accounts | Source |
|---|---|---:|---|
| Manager probe | 2026-08-18T08:42:16.8519545Z | **8460** = 6512 + 1948 | `LIVE_GROUPS_AND_TRADERS.json`, `LIVE_MANAGER_FETCH_MEASURED.md`, `CREDENTIALS_AND_COPY_STATUS.md` |
| P500 live API | same day, mid-scoring | **8463** | `P500_PROFIT_SYNTHESIS.md` (`GET /api/overview` `TotalAccounts`); synthesis writes “Achiever 6512 + Starwave ~1951” |

**+3 is unreconciled.** This slot did not re-attach. Use **8463** as the P500 dashboard catalog size the assigned brief names; use **8460** as the hashed Manager dump. The selection argument does not depend on the three-login delta.

`OverviewDto.TotalAccounts` is `Mt5Accounts.CountAsync` (`EfDashboardQueries.GetOverviewAsync`). Ingest writes that table with `GetAccountsAsync(null)` — **every** manager-visible login, not a profitable subset (`DealIngestionService.SyncCatalogAsync`).

### 1.2 Achiever groups (08:42Z dump)

These are **all** groups this Achiever manager can see.

| Group | §9 plan token | Accounts | Share of 6512 | Class |
|---|---|---:|---:|---|
| `demo\yo-2step` | `MT5_GROUP_2STEP_DEMO` / `CORE_DEMO` | **6295** | **96.67%** | challenge **DEMO** |
| `demo\yo-payp` | `PASSFIRST_DEMO` | 23 | 0.35% | challenge DEMO (pass-first) |
| `demo\yo-1step` | `1STEP_DEMO` | 4 | 0.06% | challenge DEMO |
| `demo\yo-instant` | — | 0 | 0% | empty DEMO |
| `contest\yo-2step` | `MT5_GROUP_2STEP_REAL` / `CORE_REAL` | 179 | 2.75% | **contest**, not dest-funded |
| `contest\yo-payp` | `PASSFIRST_REAL` | 5 | 0.08% | contest |
| `contest\yo-instant` | `INSTANT_REAL` | 4 | 0.06% | contest |
| `contest\yo-1step` | `1STEP_REAL` | 2 | 0.03% | contest |
| **demo** | | **6322** | **97.08%** | |
| **contest** | | **190** | **2.92%** | |
| **funded / `real\`** | | **0** | **0%** | |

Architecture §9 labels `contest\yo-*` as `*_REAL`. That token is a **plan overlay**, not going-concern capital (`A40_plan_group_mapping.md`). Sample contest balances in the dump sit at eval notionals (~1k or ~6k), not a scaled live book.

### 1.3 Starwave groups (same dump)

| Prefix | Accounts | Share of 1948 | Class |
|---|---:|---:|---|
| `Starwave\demo\FX2\*` (170+1735) | **1905** | **97.79%** | demo |
| `Starwave\cent\FX1\*` (11+4) | 15 | 0.77% | cent, not prop-funded |
| `Starwave\real\FX3\*` (22+4+2; empty grp2/3/5) | **28** | **1.44%** | only visible real-ish pool |

P500 pin: Starwave **deals-done, scored = 0**. Those 28 real names are **not** a scored XAU tape today. Do not invent scores.

### 1.4 Combined (8460)

| Class | N | Share |
|---|---:|---:|
| Challenge / demo (Achiever all + Starwave demo) | **8417** | **99.49%** |
| Cent | 15 | 0.18% |
| Real-ish | 28 | 0.33% |

A pooled `ORDER BY earlyScore DESC` over `/api/traders` is a **demo-challenge rank**. `GetTradersAsync` iterates **all** accounts and sorts by `EarlyScore` descending. There is no group-class filter on the grid.

---

## 2. Why this is adverse selection (not “more traders = more profit”)

### 2.1 What a challenge login is for

`demo\yo-2step` / `demo\yo-1step` / `demo\yo-instant` / `demo\yo-payp` exist to **hit a profit target** and stay inside daily / max drawdown. After pass, fail, or reset, the same population often **size-up after losses**. That is the product, not a bug in the tape.

| Path | Objective | After fail | Dest implication |
|---|---|---|---|
| `demo\yo-*` | Pass phase profit + DD rules | New login / rebuy eval | Virtual cash; reset option |
| `contest\yo-*` | Same hurdle, contest/leaderboard | Same class | Lottery + reset; §9 `*_REAL` is a **label** |
| Later funded / `real\` | Stay solvent on going-concern rules | Real loss, no eval reset | Closer to Pepperstone economics |

`demo\yo-payp` is explicitly **pass-first** in the §9 catalog (`MT5_GROUP_PASSFIRST_DEMO`). Pass-first is the cleanest statement of the selection: **survive the hurdle, then the book is disposable.**

### 2.2 The scorer pays the hurdle

`BaselineScorer.Score` (`src/Domain/Scoring/BaselineScorer.cs`):

```text
quality = 50
        + 15 if XAU net PnL > 0
        + 10 if PF >= 1.2
        +  5 if PF >= 1.8
        + 0.20 * behavior
        - 0.25 * risk
SHADOW if quality >= 70 && risk < 40 && completed XAU >= 3
```

The **+15 for `NetPnl > 0`** is exactly “did you print a positive residual.” On a challenge book that is **pass-target selection**, not dest-net skill. `CanPromoteToLive` is hard-`false`. Trade #3 can be `SHADOW`. It cannot be `LIVE`.

Dashboard `netSourcePnl` sums **all completed reconstructed trades** (no XAU filter). Score uses **completed XAU only**. Live paint (`P500_S001`):

| Login | Group | State | earlyScore | netSourcePnl |
|---|---|---|---:|---:|
| ACHIEVER 302252 | `demo\yo-2step` | SHADOW | 95.50 | **−68.46** |
| ACHIEVER 303174 | `demo\yo-2step` | SHADOW | 95.50 | **−29.38** |

SHADOW is **not** a profit filter.

### 2.3 The same population then blows

Unit fixture is the challenge archetype (`FakeMt5BrokerConnector` login **10002**, `demo\yo-2step`):

| Ticket | Lots | PnL |
|---|---:|---:|
| 601 | 0.10 | −200 |
| 602 | 0.20 | −500 |
| 603 | 0.40 | −1400 |

`BaselineScorerTests.Martingale_after_losses_is_risk_blocked` maps that tape to **`RISK_BLOCKED`**.

`TraderStateMachine.FromBaseline`:

```194:195:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
```

The left tail is **defined** as losing martingale (or risk ≥ 80). Copying it is copying the blow.

### 2.4 SHADOW “winners” are still challenge tape

P500 live SHADOW examples (all demo):

| Login | Group | XAU n | Source $ | Why it is not dest edge |
|---|---|---:|---:|---|
| 303310 | `demo\yo-2step` | 22 | +41,634 | `lotEscalation=true`, lots **0.01 → 2.0**, one ticket +13,692 (`P500_S006`) |
| 322947 | `demo\yo-payp` | 194 | +4,950 | avg hold **~163s**; gold scalps die in dest spread + 15s `MaxSourceSignalAge` |
| 303274 | `demo\yo-2step` | 102 | +1,228 | same-second 0.05 grid; first 3 XAU **−0.35, −55.30, +25.90** |
| 302252 | `demo\yo-2step` | 11 | **−68.46** | still SHADOW 95.50 |

`P500_S004`: measured SHADOW groups this pass were **only** `demo\yo-2step` and `demo\yo-payp`. **0** contest, **0** Starwave, **0** real.

---

## 3. Scored-book dollars (the copy-all EV)

P500 mid-scoring pin (`P500_PROFIT_SYNTHESIS.md` / `P500_S007`; Achiever scoring; Starwave scored 0):

| Bucket | N | Source PnL sum |
|---|---:|---:|
| SHADOW | 70 | +78,276 |
| WATCH | 79 | +8,178 |
| `RISK_BLOCKED` | **29** (all `martingale=true`) | **−241,580** |
| All scored XAU | 197 | **−154,425** |
| `LIVE` / `LIVE_CANDIDATE` | 0 / 0 | — |
| `INSUFFICIENT_DATA` (catalog remainder) | **~8284** of 8463 | not an XAU edge |
| Destination real PnL | — | **$0** |
| Shadow PnL | — | **$0** (no dest quote tape) |

The blocked tail is **larger than the SHADOW head** (−241k vs +78k). Copy-all EV of the scored XAU book is already **negative six figures at source**, before Pepperstone spread, commission, delay, and 1:1 lot risk.

`OverviewDto.DestinationRealPnl` is a constructor **0** (`EfDashboardQueries` L44). That is **uncomputed dest book**, not “challenge residual after costs = 0.”

---

## 4. Copying all 8463 logins copies `RISK_BLOCKED` losses

### 4.1 The catalog is not a copy list — unless you treat it as one

| Surface | What it does | Filter on `RISK_BLOCKED` / `demo\`? |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` | `GetAccountsAsync(null)` → upsert **all** | **No.** Plan maps are labels (`D68`). |
| `EfDashboardQueries.GetTradersAsync` | `foreach (var account in accounts)` | **No.** Unscored rows paint `INSUFFICIENT_DATA`. Sort = `EarlyScore` desc. |
| Naive “copy every `/api/traders` login” | would include 29 `RISK_BLOCKED` + ~8284 empty + 70 demo SHADOW | **Would copy the −$241,580 tail.** |
| `CopyTradingService.GenerateShadowIntentsAsync` | starts from `{SHADOW, LIVE_CANDIDATE, LIVE}` only | Does **not** start from `RISK_BLOCKED`. Then calls policy. |
| `XauUsdOneToOneCopyPolicy.IsTraderEligible` | reject `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED` | **Yes** — reason `TRADER_BLOCKED_RISK_BLOCKED`. |

Assigned truth, unsoftened: **copying all 8463 logins would copy `RISK_BLOCKED` losses.** The catalog walk has no state gate. The only gates live in the copy policy **if** that policy stays on the hop **and** the operator does not bypass it with “copy the grid.”

Unit lock: `XauUsdOneToOneCopyPolicyTests.Risk_blocked_state_rejected` — `GoodTrader() with { State = RISK_BLOCKED }` is ineligible.

### 4.2 What the current policy already refuses (keep this)

`src/Domain/Copy/XauUsdOneToOneCopyPolicy.cs` `IsTraderEligible`:

| Gate | Reason | Why it cuts dest loss |
|---|---|---|
| State ∈ {`RISK_BLOCKED`, `DISQUALIFIED`, `PAUSED`} | `TRADER_BLOCKED_*` | Drops the −$241k martingale bucket |
| State ∈ {`INSUFFICIENT_DATA`, `EARLY_SCORE`, `WATCH`} | `TRADER_NOT_SHADOW_YET` | Drops first-3 luck and the ~8284 empty catalog |
| `Martingale` / `AveragingDown` / `LotEscalation` | `TRADER_SIZE_PATTERN_BLOCK` | Drops 303310-class lot explosions even if still SHADOW |
| Completed XAU **< 20** | `NEED_MORE_XAU_HISTORY` | First-3 is not a book |
| XAU net **≤ 0** | `XAU_BOOK_NOT_PROFITABLE` | Drops 302252-class SHADOW-red names |
| Group starts with `demo\` or `contest\` | `DEMO_OR_CONTEST_GROUP` | Drops Achiever challenge paths |

Tests: `Demo_group_blocked` (`demo\yo-2step` → `DEMO_OR_CONTEST_GROUP`); `Negative_xau_pnl_blocked`; `First_three_trades_not_enough`; `Martingale_trader_blocked`.

On the **current** SHADOW set (100% Achiever `demo\`), **every** live SHADOW name fails `DEMO_OR_CONTEST_GROUP`. That is the correct loss floor for this book. It is **not** dest profit. It is **refusing to import the factory**.

### 4.3 Holes (do not claim the policy finishes the job)

1. **Starwave demo prefix.** Eligibility is `StartsWith("demo\\")` / `StartsWith("contest\\")`. Live Starwave demo is `Starwave\demo\FX2\grp2` (**1735** logins). That string does **not** start with `demo\`. If Starwave scoring later paints SHADOW on those 1735, the group gate **misses**. Use a contains / segment check (`\demo\` / `\contest\`), not a startswith on the Achiever spelling.
2. **`AllocationFactor = 1`** (1:1 lots, gold cap **5.0**). Policy comment says “copy next XAUUSD events 1:1.” That **raises** dest loss if a later eligible name is large. Profit path is still **0.01–0.05** allocation and a **0.05** dest lot cap until shadow-after-costs is green (`P500_S006`). 1:1 of 303310’s 2.0-lot tickets is how Pepperstone dies.
3. **`RiskEngine.Evaluate` is identity-blind.** It does not read `TraderState`. Spec token `TRADER_RISK_BLOCKED` is not emitted. If someone expands `copyable` to “every scored XAU” and forgets `MartingaleFlag`, Evaluate can `APPROVE`. Product persist still forces `AllowFixSend = false`.
4. **No `35=D`.** `NewOrderSingleImplemented = false`, `VenueReconciled = false`. Flag-armed runtime is **not** a fill. Do not enable `REAL_COPY` to “monetize” 8463 names.

---

## 5. Higher profit / lower loss (this slot’s operating list)

Do these in order. Skipping to “copy more logins” is the loss path.

### Lower loss (now)

1. Do **not** copy all 8463. The catalog is observation (`GetAccountsAsync(null)`).
2. Do **not** copy `RISK_BLOCKED`. That bucket is **−$241,580** and is **defined** as losing martingale / risk ≥ 80.
3. Do **not** copy `demo\` / `contest\` Achiever groups. They exist to pass a target then blow / reset.
4. Do **not** send `35=D`. Keep `CanPromoteToLive == false`. Dest PnL stays **$0** until a sender + recon + real quotes exist.
5. Do **not** flatten the MT5 source if dest later loses.
6. Tighten the group gate to catch `Starwave\demo\*` before Starwave scoring lands.
7. Do **not** ship 1:1 lots (`AllocationFactor = 1`, max 5) as the working dest size.

### Higher profit (only after the loss floor)

1. Universe = **funded / `real\`** names that survive the policy **and** dest-cost haircut — today that is **28** Starwave `real\FX3\*` logins, **unscored**. Finish Starwave scoring. Do not drown them in 6295 `demo\yo-2step` ranks.
2. Eligibility already in policy that is actually +EV-shaped: **≥ 20** completed XAU, **XAU net > 0**, no martingale / avg-down / lot-escalation, not blocked.
3. Add what the policy still lacks: median hold **≥ 15 minutes**, no same-second multi-ticket grid, dest qty after haircut **≤ 0.05** lot, gold-specific spread cap, 30+ days **destination** (not source) shadow expectancy > 0.
4. Quality must stop treating `NetPnl > 0` on a 3-trade challenge tape as skill. First-3 / 95.5 is source luck (`P500_CODE_102`).
5. Tiny live only after shadow-after-costs is green **and** §68/§70 gates are actually PASS. Not before.

### What higher profit is **not**

- Not copying more of the 8463.
- Not ranking SHADOW by +$41k / 95.5 / pass-first residual.
- Not flipping `REAL_COPY` because FIX is `LoggedOn`.
- Not ML (Phase 6; not built; must beat this baseline OOS).
- Not wanting it.

---

## 6. Evidence index (absolute paths)

| Path | What it measures |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 08:42Z Manager census: 8/6512 + 10/1948; group counts |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Same census, no passwords |
| `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` | 8463 API pin; −$154,425 / −$241,580 / SHADOW all demo; dest $0 |
| `D:\Prop\reports\swarm\20260818\P500_S004_demo_adverse_selection.md` | SHADOW 100% demo |
| `D:\Prop\reports\swarm\20260818\P500_S007_blocked_left_tail.md` | Blocked tail dominates |
| `D:\Prop\reports\swarm\20260818\P500_S036_contest_vs_demo.md` | 6295 vs 179; `contest` ≠ funded |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | +15 for `NetPnl > 0`; `RISK_BLOCKED`; `CanPromoteToLive => false` |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | `DEMO_OR_CONTEST_GROUP` + `TRADER_BLOCKED_RISK_BLOCKED` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Shadow hop; `AllowFixSend = false`; no `35=D` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | All-account grid; dest PnL literal 0 |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog = all logins |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 10002 `demo\yo-2step` martingale seed |
| `D:\Prop\tests\Unit\XauUsdOneToOneCopyPolicyTests.cs` | Demo + `RISK_BLOCKED` rejects |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | Martingale → `RISK_BLOCKED` |

---

## 7. What this slot did not do

- Did not `GET` `:5000/api/overview` or `/api/traders` (localhost blocked here).
- Did not re-attach Manager or re-sum live state counts on the 8463-row catalog.
- Did not enable `REAL_COPY`. Did not emit `35=D`. Did not print secrets.
- Did not edit product source.
- Did not claim Starwave real 28 names are +EV (they are **unscored**).
- Did not claim the copy policy is a complete dest-profit engine. It is a **challenge-import rejector** with a Starwave-demo prefix hole and a 1:1 lot residual.

---

## 8. One-line operating law

```text
8463 logins are a challenge factory, not a copy book.
Wanting profit is not an edge.
Copy-all copies RISK_BLOCKED (−$241k) into a scored XAU book that is already −$154k.
Higher profit = exclude demo/contest + blocked + first-3.
Lower loss = do not send.
```
