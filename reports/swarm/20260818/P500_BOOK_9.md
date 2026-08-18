# P500_BOOK_9 — Starwave scored 0 after 91,966 deals; do not size from Achiever-only scores

| Field | Value |
|---|---|
| Slot | **9** (book / selection EV) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_9.md` |
| Agent | P500_BOOK_9 (senior quant; incomplete two-broker book) |
| Assigned | Starwave scored **0** while `dealsInserted=91966`. Book is incomplete. Do not size from Achiever-only scores. Wanting profit does not create an edge. Copying all **8463** logins would copy `RISK_BLOCKED` losses. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Live `35=D` sent | **No.** This slot did not enable `REAL_COPY` and did not build or send NewOrderSingle. |
| Secrets printed | **None.** No passwords, no proxy auth, no FIX secrets. |
| Live HTTP this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` were **not re-hit** (localhost fetch blocked from this worker). Numbers below are the **on-disk live remasure** in `P500_PROFIT_SYNTHESIS.md` + `P500_S007` / `P500_S012` + Manager census JSON. |

**One-line:** STARWAVEFX has a **real deal tape** (`dealsInserted=91966`, phase `deals-done`) and **zero scores**. The dashboard leaderboard is **Achiever-only, mid-scoring, 100% demo SHADOW**. Sizing Pepperstone from that board is selection bias, not an edge. Copy-all **8463** would also lift the measured `RISK_BLOCKED` left tail (**−$241,580**). Wanting profit does not create an edge.

---

## 0. Verdict (binding)

| Claim | Result |
|---|---|
| Is the live book complete? | **No.** Starwave `scored=0` after **91,966** inserted deals. |
| May we size dest lots from current SHADOW / `earlyScore`? | **No.** Those ranks are Achiever-only (and demo). |
| Does `scored=0` mean Starwave has no deals? | **No.** `dealsInserted=91966` and phase `deals-done` prove the tape is in the store. Scoring has not started on that broker. |
| Is copy-all of 8463 +EV? | **No.** Scored XAU already **−$154,425**. The blocked tail is **−$241,580**. Unscored Starwave is an **unknown** extra tail. |
| Does wanting higher profit create an edge? | **No.** Desire is not expectancy. |
| Product send today? | **None.** `CTraderFixSession` outbound is `35=A` only. `NewOrderSingleImplemented=false`. Persist `AllowFixSend=false`. Dest real PnL is the overview literal **0**. |
| Product edited? | **No.** |

```text
STARWAVEFX.phase            = deals-done
STARWAVEFX.dealsInserted    = 91966
STARWAVEFX.scored           = 0
ACHIEVER.phase              = scoring
ACHIEVER.dealsInserted      = 260352
LIVE_XAU_SCORED             = 197  (Achiever only, climbing)
SHADOW                      = 70   (100% demo; 0 Starwave)
RISK_BLOCKED                = 29   (all martingale=true; Σ −241,580)
SCORED_XAU_BOOK             = −154,425
DESTINATION_REAL_PNL        = 0    (constructor literal)
COPY_ALL_8463               = copy the blocked tail + the unscored catalog
SIZE_FROM_ACHIEVER_ONLY     = FORBIDDEN
PRODUCT_EDITED              = NO
35=D                        = NOT SENT
```

Risk to live capital from this note: **NONE** (`SAFE_BY_ABSENCE`). Residual risk is **operator ranking**: treating an incomplete Achiever-demo leaderboard as a size book.

---

## 1. Honesty pin

Wanting higher profit and lower loss does **not** mint a filter. A TLS Logon (`35=A`) is not a fill. A SHADOW count of 70 is not dest expectancy. A Starwave deal counter of 91,966 is not a score. A catalog of 8,463 logins is not 8,463 copy candidates.

Copying **all 8463** logins onto one Pepperstone account would copy:

1. The measured Achiever `RISK_BLOCKED` losers (**−$241,580** source, all `martingale=true`).
2. The rest of the scored XAU book that is already net **−$154,425**.
3. ~8,284 `INSUFFICIENT_DATA` rows (including **every** Starwave login).
4. Demo-challenge pass-target tape (`demo\yo-2step` 6295 / 6512; Starwave `demo\FX2` 1905 / 1948).
5. House/LP residue (`Starwave\real\FX3\LP`, 2 logins) if the walk is flag-blind.

That is how you **lose** the dest account. It is not a profit plan.

---

## 2. Measured live book (mid-scoring, 2026-08-18)

Source of the API integers: `P500_PROFIT_SYNTHESIS.md` §1 (`GET /api/health`, `/api/overview`, `/api/ingest/status`, `/api/fix/sessions`, `/api/traders`). Cross-check: `P500_S007_blocked_left_tail.md`, `P500_S012_starwave_unscored.md`. Manager census (not the same grain as 8463): `LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json`.

### 2.1 Ingest / score phases

| Broker | Connect | Groups | Catalog accounts | Deals inserted | Phase | Scored |
|---|---|---:|---:|---:|---|---:|
| ACHIEVER | HTTP proxy | 8 | 6512 | **260352** | `scoring` | climbing through **225+** (XAU-scored names **197** at the synthesis cut) |
| STARWAVEFX | direct, no proxy | 10 | 1948 census / ~1951 API | **91966** | **`deals-done`** | **0** |
| **Catalog** | | **18** | **8460 census / 8463 API** | 352318 | incomplete | Achiever-only |

`8463 − 8460 = +3` is **unreconciled**. Census JSON is Achiever **6512** + Starwave **1948**. Synthesis wrote “Achiever 6512 + Starwave ~1951.” Do not greenwash the +3. Use **8463** as the later API catalog pin the parent named; use **8460** as the Manager dump. Neither number is a scored-XAU count.

Naive deal density (not a quality metric):

```text
Achiever  260352 / 6512 ≈ 40.0 deals / catalog login
Starwave   91966 / 1948 ≈ 47.2 deals / catalog login
```

Starwave is **not** an empty broker. It is an **unscored** broker with a **larger** deals-per-login average than Achiever on this 90-day window.

### 2.2 Achiever-only scored XAU (the only scored tape)

| Bucket | N | Source `netSourcePnl` sum | Copy meaning |
|---|---:|---:|---|
| `SHADOW` | 70 | **+$78,276** | 100% `demo\yo-*`. Not dest after spread. |
| `WATCH` | 79 | **+$8,178** | Same demo population. |
| `RISK_BLOCKED` | **29** | **−$241,580** | All `martingale=true`. Copy-all copies this. |
| All scored XAU | **197** | **−$154,425** | Copy-all EV of the **visible** tape, uncosted. |
| `LIVE` / `LIVE_CANDIDATE` | 0 / 0 | — | `CanPromoteToLive => false`. |
| `INSUFFICIENT_DATA` | **~8284** | mostly 0 / unknown | Includes **all** Starwave logins + unscored Achiever. |
| Destination real PnL | — | **$0** | Overview constructor literal. |
| Shadow PnL | — | **$0** | No dest quote tape. |

Arithmetic (same as S054 / S007):

```text
book − SHADOW − RISK_BLOCKED
  = −154,425 − (+78,276) − (−241,580)
  = +8,878          ← leftover WATCH / EARLY_SCORE, still source $

book excluding RISK_BLOCKED
  = −154,425 − (−241,580)
  = +87,154         ← still mostly demo SHADOW, still not dest EV

blocked tail vs SHADOW head
  = 241,580 / 78,276 ≈ 3.09× larger in dollars
```

The entire redness of the **visible** book is the blocked tail. That tail is **only the Achiever names already scored**. Starwave’s 91,966 deals have **not** been reconstructed or classified. The true two-broker left tail is **unmeasured**.

### 2.3 Group class (why Achiever-only SHADOW is the wrong universe)

Manager-visible (JSON sums; no passwords):

| Broker | Class | Accounts | Share |
|---|---|---:|---:|
| Achiever | `demo\yo-2step` | 6295 | 96.67% of 6512 |
| Achiever | other `demo\*` | 27 | 0.41% |
| Achiever | `contest\*` | 190 | 2.92% |
| Achiever | funded / `real\*` | **0** | 0% |
| Starwave | `demo\FX2\*` | 1905 | 97.79% of 1948 |
| Starwave | `cent\FX1\*` | 15 | 0.77% |
| Starwave | `real\FX3\*` incl. LP | **28** | 1.44% |
| Starwave | `real\FX3` minus `LP` | 26 | 1.33% |

Current SHADOW groups (S004 / synthesis): **49+ `demo\yo-2step` + 6 `demo\yo-payp`**. **0** contest. **0** Starwave. **0** funded-real.

---

## 3. Why Starwave can sit at `scored=0` with 91,966 deals

This is a **pipeline queue**, not a “Starwave has no XAU” fact.

### 3.1 Three sequential waves, Achiever first

`LiveMt5Registration.CreateConnectors` returns `{ ACHIEVER, STARWAVEFX }` (`LiveMt5Registration.cs` L47–49). `BrokerRegistry.All()` is dictionary insertion order (net8 preserves it). `LiveIngestHostedService.ExecuteAsync` then runs **three** `foreach (var connector in connectors)` loops:

1. **Catalog** both brokers (`phase=catalog` → `catalog-done`).
2. **Deals** both brokers, 90-day window (`phase=deals` → **`deals-done`**, writes `DealsInserted`).
3. **Score** both brokers (`phase=scoring` → `done`).

Starwave finished loop 2 (`dealsInserted=91966`, `phase=deals-done`). Loop 3 is still on Achiever. `BrokerLiveStatus.Scored` is **only assigned inside loop 3** (every 25 logins, then the final count). Until Starwave enters that loop, `Scored` stays the `int` default **0** and `Phase` stays **`deals-done`**.

```98:125:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
            foreach (var connector in connectors)
            {
                var st = _runtime.Broker(connector.BrokerCode);
                if (!st.Connected)
                    continue;
                try
                {
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    // scored++ per login; st.Scored written every 25
                    st.Phase = "done";
                }
                ...
            }
```

`ListLoginsWithDealsAsync` is `Mt5Deals.Where(BrokerId).Select(Login).Distinct()` (`EfTradingStore.cs` L343–345). After 91,966 Starwave upserts that set is **non-empty**. `scored=0` is **not** “zero deal logins.” Sibling notes that treat it as an empty tape are **wrong for this snapshot**.

### 3.2 What scoring would do (and has not done)

`ReconstructionScoringService.RebuildTraderAsync` reconstructs **that login’s** deals, scores **completed XAU only**, writes `TraderScore`, and only then `PersistDemoShadowAsync` (SHADOW-only, and only if a dest quote row exists — live dest quotes are empty, so shadow fills stay 0).

Until that runs for Starwave:

- `/api/traders?broker=STARWAVEFX` left-joins **no** scores.
- `GetTradersAsync` paints those rows `INSUFFICIENT_DATA`, `earlyScore=0`, `completedXauTrades=0` (`EfDashboardQueries.cs` L103–119).
- `GetTradersAsync` then `OrderByDescending(t => t.EarlyScore)` (L128). Achiever SHADOW **95.50** names float to the top. Starwave is a **zero-score slab** at the bottom.

That sort is how an operator “sizes from the leaderboard” without noticing the book is half-missing.

### 3.3 Completing the queue is necessary, not sufficient

When Starwave `scored` leaves 0:

- You get a **second** scored tape, still ~97.8% `demo\FX2`.
- `real\FX3` is **28** logins, 13 of them equity 0, 2 of them `LP` (S037). That is not a funded book.
- Scoring demo FX2 is the **same** adverse-selection trap as scoring Achiever `demo\yo-2step` (S004).
- `RISK_BLOCKED` will appear on Starwave martingale losers the same way (`FromBaseline`: `risk >= 80` **or** `(Martingale && MaxDrawdown > 0 && NetPnl < 0)`).

Wait for scores. **Then still filter.** Do not treat “Starwave scored > 0” as a go-live.

---

## 4. Why Achiever-only scores must not set dest size

### 4.1 The scorer is not a profit filter

`BaselineScorer.Score` uses **completed XAU** features only:

```text
quality = 50
        + 15 if XAU net > 0
        + 10 if PF >= 1.2
        +  5 if PF >= 1.8
        + 0.20 * behavior
        - 0.25 * risk
SHADOW iff quality >= 70 AND risk < 40 AND completed XAU >= 3
```

Dashboard `netSourcePnl` sums **all completed reconstructed trades** (every symbol). Measured collision (S001 / synthesis):

| Login | Group | `earlyScore` | State | Dashboard net |
|---|---|---:|---|---:|
| 302252 | `demo\yo-2step` | 95.50 | SHADOW | **−68.46** |
| 303174 | `demo\yo-2step` | 95.50 | SHADOW | **−29.38** |
| 303310 | `demo\yo-2step` | (top SHADOW) | SHADOW | **+41,634** with `lotEscalation` and max **2.0** lots |

A 95.50 on a red dashboard name is **not** dest size. A +41k demo lot-explosion is **not** dest size.

`AverageHoldSeconds` is computed and **unused**. MFE/MAE is `Unavailable`. `CanPromoteToLive` is hard-`false` (`BaselineScorer.cs` L211). Trade #3 cannot become LIVE.

### 4.2 Copy-all 8463 copies the blocked tail

Product **does not** copy-all today:

- `PersistDemoShadowAsync` returns without a `CopyIntent` unless `state == SHADOW` (`EfTradingStore.cs` L267–271).
- `CopyTradingService.GenerateShadowIntentsAsync` allow-lists `{SHADOW, LIVE_CANDIDATE, LIVE}` only. `RISK_BLOCKED` is not copyable.
- Persist forces `AllowFixSend=false`. `NewOrderSingleImplemented=false`. `VenueReconciled=false`.
- `CTraderFixSession` has **0** `35=D` / `NewOrderSingle` builders. Hosted logon is `35=A` then dispose.

The **operator** error this slot forbids is: “we have 8463 logins, spray them.” That spray includes the 29 blocked names (**−$241,580** source) **and** every Starwave login that has not even been classified. The measured scored-XAU EV is already **−$154,425** **before** Pepperstone spread, 15 s signal-age death, and any lot conversion.

```text
EV(copy-all 8463, 1:1, uncosted)
  ≤ EV(copy scored XAU) + EV(copy unscored remainder)
  ≤ −154,425            + unknown Starwave left tail
  < 0
```

Unknown is not “maybe the missing 1948 save us.” Starwave is 97.8% the same demo-challenge shape. Completing scores is how you **measure** that tail, not how you assume it is green.

### 4.3 Allocation from an Achiever-only rank is still wrong even at 5%

`CopyTradingService.AllocationFactor = 0.05m`. That haircut does **not** repair selection. Five percent of a lot-explosion, five percent of a martingale, and five percent of a 163 s gold scalp are still the **wrong names**. Size is a second-stage control. **Who** you copy is first-stage. First-stage is incomplete.

---

## 5. Higher profit / lower loss (measured, not wished)

### 5.1 Higher dest profit (when a sender exists — it does not today)

| Action | Why it is the profit lever |
|---|---|
| **Do not size from this leaderboard** | Ranks are Achiever-demo `earlyScore`, not dest-net after costs. |
| **Wait until Starwave `scored` leaves 0** | 91,966 deals are invisible to the scorer. Any “top N” is a one-broker sample. |
| **Then drop `RISK_BLOCKED`** | Removes **−$241,580** of the *visible* book (3× the SHADOW head). |
| **Then drop `demo\` / `contest\` / `cent\` / `LP`** | Challenge pass-target + house flow is adverse selection (S004 / S036 / S037). |
| **Then dest-cost the residual on a standing QUOTE tape** | Source +$78,276 is not Pepperstone bid/ask. `shadowPnl=0` because quotes are empty. |
| **Keep size tiny and below source** | `MaxPositionQuantity=5` gold is a ruin cap, not a working cap (S055). |

None of those steps is “send more.” Sending more of an incomplete, demo, net-negative book **lowers** dest profit.

### 5.2 Lower dest loss (binding now)

| Action | Why it cuts loss |
|---|---|
| **Do not send `35=D`** | No dest fill ⇒ dest loss stays the literal 0. `SAFE_BY_ABSENCE`. |
| **Do not enable `REAL_COPY` as a profit switch** | Flag may be env-true; sender is still unimplemented. Arming is not a fill. |
| **Do not copy-all 8463** | That **is** copying `RISK_BLOCKED` + unknown Starwave. |
| **Do not invent Starwave scores** | Fail-closed on `INSUFFICIENT_DATA`. Missing score ≠ 0 risk. |
| **Do not treat SHADOW +78k as a budget** | 100% demo, uncosted, XAU-only quality vs all-symbol dashboard. |
| **Never retry unknown ClOrdID** | Adjacent S014; not this slot’s code path. |

Lower loss today is **absence of a sender**, not a proven filter. The filter work above is what keeps loss low **if** a sender is added later.

### 5.3 What this slot refuses to claim

- That Starwave FX3 is +EV (28 names, mostly dead / LP; **unscored**).
- That finishing the score loop will flip the book green.
- That 8463 ≈ 8460 so the extra 3 do not matter (unreconciled; report both).
- That `destinationRealPnl=0` is break-even trading (it is a literal).
- That FIX `LoggedOn` monetizes anything.

---

## 6. Capital / send status (this process)

| Gate | Measured |
|---|---|
| `CTraderFixSession` outbound MsgType | **`A` only** (grep of `src/Fix.CTrader` for `35=D` / `NewOrderSingle` in the session = 0 builders) |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` |
| Persist `AllowFixSend` | forced `false` |
| `CanPromoteToLive` | `=> false` |
| Overview `DestinationRealPnl` | literal `0` (`EfDashboardQueries.cs` after `shadowPnl`) |
| `REAL_COPY_EXECUTION_ENABLED` | DI-bound from env (may be true on the API host). **Not** a sender. This slot did not flip it. |
| This slot | no product edit, no `35=D`, no `REAL_COPY` enable |

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). The loss this note prevents is **future / operator**: sizing from Achiever-only scores or spraying 8463 logins, which **would** copy `RISK_BLOCKED` losses.

---

## 7. Evidence index (absolute paths)

| Path | What it proves |
|---|---|
| `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` | Live API remasure: 8463 accounts; Starwave **91966 / scored 0 / deals-done**; Achiever 260352 / scoring; SHADOW +78,276; blocked −241,580; book −154,425; dest 0 |
| `D:\Prop\reports\swarm\20260818\P500_S012_starwave_unscored.md` | Same ingest pin; sequential score; Achiever-demo SHADOW bias |
| `D:\Prop\reports\swarm\20260818\P500_S007_blocked_left_tail.md` | 70 / 79 / 29 split; all 29 blocked are martingale |
| `D:\Prop\reports\swarm\20260818\P500_S004_demo_adverse_selection.md` | SHADOW 100% demo; Starwave scored 0 |
| `D:\Prop\reports\swarm\20260818\P500_S037_starwave_real_tiny.md` | Starwave real FX3 = 28; demo FX2 = 1905 |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8/6512 + 10/1948 = 18/8460 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Starwave block `accounts: 1948`, `openPositions: 478` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog → deals → score; Achiever first; `Scored` only in loop 3 |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLoginsWithDealsAsync`; SHADOW-only persist |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Unscored → `INSUFFICIENT_DATA` / `earlyScore=0`; sort by `earlyScore` desc; dest PnL literal 0 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | SHADOW ≠ profit; `RISK_BLOCKED` rule; `CanPromoteToLive=false` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Connector order Achiever then Starwave; Starwave `ProxyEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Logon only; no `35=D` |

---

## 8. Profit implication (close)

**Higher profit** = do not bet the dest book on an Achiever-only, mid-scoring, demo SHADOW rank while 91,966 Starwave deals sit at `scored=0`. Complete the two-broker score, **then** drop `RISK_BLOCKED` / demo / contest / cent / LP, **then** mark the residual on real Pepperstone quotes at tiny size.

**Lower loss** = do not copy-all 8463 (that copies the −$241k blocked tail plus the unclassified Starwave slab); do not invent Starwave scores; do not send `35=D`.

Wanting those two outcomes does not create an edge. The incomplete book is a **measurement fail**, not a green light.
