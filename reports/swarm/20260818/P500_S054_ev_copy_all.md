# P500_S054 — EV of copy-all / copy-SHADOW-demo-uncosted / send-now

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S054_ev_copy_all.md` |
| Agent | P500_S054 (selection EV; read-only) |
| Date | 2026-08-18 |
| Assigned | Using the live integers in the parent prompt, write this file. **EV(copy-all) is negative.** **EV(copy-SHADOW-demo-uncosted) is unknown after spread.** **EV(send-now) is negative** because there are no dest quotes, no dest recon, and no dest sizing. **Do not edit product.** |
| Product source edited | **No** |
| Test source edited | **No** |
| Live book | Caller-measured **2026-08-18** API (not re-probed here) |
| Method | Arithmetic on the given buckets + close-read of scorer / persist-shadow / overview / qty / quote path. Product `*.cs` **not** modified. |
| Adjacent (read, not edited) | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`; `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`; `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`PersistDemoShadowAsync`); `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`; `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs`; `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`; siblings S001, S004, S006, S007, S008, S009, S010, S022, S029 |
| Secrets printed | **None** |

**One-line:** Copy-all of the scored XAU book is **−EV** on the measured tape (−154,425, driven by `RISK_BLOCKED` −241,579). Copying the SHADOW island (+78,276, **all demo**) is **not** a dest-profit number — after-spread EV is **unmeasured**. Sending live `35=D` now is **−EV** because dest quotes, dest recon, and dest sizing are absent.

---

## 0. Verdict (binding)

| Policy | Expected value | Why |
|---|---|---|
| **Copy-all scored XAU** | **Negative** | Naive 1:1 of the live book is **−154,425**. That redness **is** the `RISK_BLOCKED` left tail (**−241,579**). |
| **Copy-SHADOW, demo, uncosted** | **Unknown after spread** | Source Σ **+78,276** is **not** dest $ after Pepperstone bid/ask. All SHADOW is demo. No live dest quote tape. After-cost EV is **not** the source island. |
| **Send now** (`REAL_COPY` / `35=D`) | **Negative** | No dest quotes on the persist path, no dest recon, no wired lot→tag-38 sizing. A send without those three is a random (or oversized) bet, not a copy. Dest PnL is the constructor literal **0**. |
| Product changed? | **No** | Report only. |

```text
EV(copy-all scored XAU)                 = NEGATIVE     (−154,425 source book)
EV(copy-SHADOW demo, uncosted)          = UNKNOWN      after dest spread / costs
EV(send-now / REAL_COPY / 35=D)         = NEGATIVE     (no quotes, no dest recon, no sizing)
destinationRealPnl (live overview)      = 0            (literal, not a trade result)
SHADOW population                       = 100% demo    (no funded / no Starwave seat)
PRODUCT_EDITED                          = NO
```

Do **not** treat SHADOW +78,276 as a profit forecast. Do **not** treat dest PnL 0 as break-even trading. Do **not** enable `REAL_COPY` to “unlock” the green island.

---

## 1. Live measured book (2026-08-18, given)

Caller snapshot. This agent did **not** re-hit `:5000`.

| Bucket | Dollars | Grain | Copy meaning |
|---|---:|---|---|
| XAU scored book | **−154,425** | API `netSourcePnl` on scored XAU logins | Copy-all ≈ this number (uncosted, 1:1, same lots — already −EV before costs) |
| `SHADOW` | **+78,276** | Same `netSourcePnl` on `CurrentState == SHADOW` | Source island only. **All demo.** Not dest after spread. |
| `RISK_BLOCKED` | **−241,579** | Same field on blocked logins | Left tail. Copy-all copies this. |
| Destination real PnL | **0** | `OverviewDto.DestinationRealPnl` | Constructor literal in `GetOverviewAsync`. Never a venue fill. |

Arithmetic on the three named source buckets:

```text
book − SHADOW − RISK_BLOCKED
  = −154,425 − (+78,276) − (−241,579)
  = −154,425 − 78,276 + 241,579
  = +8,878

book excluding RISK_BLOCKED
  = −154,425 − (−241,579)
  = +87,154

SHADOW as a share of the non-blocked island
  = 78,276 / 87,154
  ≈ 89.8%
```

The entire redness of the scored book is the blocked tail. Remove it and the same tape is **+87,154** of **source** dollars. That remainder is still **not** dest expectancy: it is mostly the SHADOW demo island plus ~+8,878 of other scored states (`WATCH` / `EARLY_SCORE` / residual; S007 previously split WATCH ≈ +8,177). This file does **not** re-split that residual.

Honesty on `NetSourcePnl` (same as S007): `EfDashboardQueries.GetTradersAsync` sums **all completed reconstructed trades** per `(BrokerId, Login)`. `BaselineScorer` / `RISK_BLOCKED` use **completed XAUUSD only**. Treat the dollars as **API `netSourcePnl` on scored XAU logins**, which may include non-XAU completed PnL on those same logins.

---

## 2. EV(copy-all) is negative

Copy-all means: every scored XAU login, any `TraderState`, source lots, no dest cost.

| If dest filled 1:1 at source PnL | Result |
|---|---|
| Copy the whole scored book | **−154,425** |
| Of which `RISK_BLOCKED` | **−241,579** |
| Of which `SHADOW` | **+78,276** |
| Residual scored | **+8,878** |

That is already **−EV before**:

- dest spread (S029: live `MaxAllowedSpread=2.0` is **USD/oz**, and there is **no** live bid/ask to measure the real width)
- latency / tag-38 reject
- lot explosion on names like ACHIEVER 303310 (S006: 0.01 → 2.0 lots on a SHADOW book)
- demo-challenge adverse selection (S004: SHADOW is 100% `demo\yo-*`)
- correlation (S018: N SHADOW gold books are **one** gold bet)

`TraderStateMachine.FromBaseline` already refuses the losing-martingale books a SHADOW seat:

```189:206:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        // ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

Copy-all **throws that veto away**. The measured loss of doing so is the blocked bucket. Lower-loss vs copy-all is mechanical on this tape: **never copy `RISK_BLOCKED`**. That is **not** a profit license for the remainder.

```text
copy-all  =  SHADOW(+78,276) + residual(+8,878) + BLOCKED(−241,579)
          =  −154,425
          =  NEGATIVE EV
```

---

## 3. EV(copy-SHADOW-demo-uncosted) is unknown after spread

Uncosted source Σ on the SHADOW bucket is **+78,276**. That number is **not** an EV.

| Why the island is not dest EV | Evidence |
|---|---|
| **All SHADOW is demo** | S004 / this prompt: challenge groups (`demo\yo-2step`, `demo\yo-payp`). Zero funded, zero contest, zero Starwave in the SHADOW seat. Challenge tape is selected to hit a profit target, then often martingales into the −241,579 tail. |
| **Source ≠ dest** | `netSourcePnl` is reconstructed MT5 source dollars. Dest is Pepperstone / cTrader XAU. Different spread, different contract, different reject grid. |
| **Scorer is not a profit filter** | S001: quality uses completed XAU only; dashboard PnL is all-symbol. A SHADOW name can be red on the row. Quality starts at 50 and can print 95.50 on a small XAU winner. |
| **No live dest quote tape** | `PersistDemoShadowAsync` loads `DestinationQuotes` newest-first and **returns without a `ShadowOrder` if null**. Live FIX DTO: bid/ask/age null (S008). Only `DemoSeeder` writes a fake 2399 book. |
| **`shadowPnl` is not dest $** | Overview sums `SourceVsShadowSlippage` (price units of entry slip). Empty set → 0. Seeded slip vs 2399 is not Pepperstone. `MarkToMarket` / `SimulateExit` have **zero product callers**. |
| **Spread is unmeasured** | `ShadowCopyEngine.SimulateEntry` records `Ask − Bid` **if** a quote row exists. Live persist path has no row. S029: the risk cap `2.0` is USD/oz and is **loose**, not a measured cost. |
| **Lot explosion sits inside SHADOW** | S006 login 303310 is SHADOW / +41,634 source / lots 0.01→2.0. Uncosted copy of that size is how dest dies even if the source island is green. |
| **One gold bet** | S018: copying the SHADOW cohort is one XAU thesis, not N independent edges. Correlation makes the +78,276 **non-diversified**. |

So:

```text
E[dest $ | copy every SHADOW, ignore costs]   ≠   +78,276
E[dest $ | copy every SHADOW, after spread]   =   UNKNOWN
```

Unknown is **not** “probably positive.” A demo challenge island with no dest tape and no after-cost shadow sample is **not** a trading signal. The honest profit path (already named in S007 / S010) is: **shadow expectancy after costs**, then a tiny allocation, still with `CanPromoteToLive = false`.

`PersistDemoShadowAsync` already refuses non-SHADOW states, then **also** refuses to model a fill without a quote:

```267:278:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var quoteRow = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);
        if (quoteRow is null)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
```

On the live path that second return is the common case. There is therefore **no** after-spread SHADOW sample to promote.

---

## 4. EV(send-now) is negative — no quotes / no recon / no sizing

Sending now means: flip `REAL_COPY` (or otherwise emit `35=D`) against this book, today.

| Missing prerequisite | Measured state | EV effect of sending anyway |
|---|---|---|
| **Dest quotes** | `CTraderQuoteService` is in-memory only; never writes `destination_quotes`. Live FIX logon does not subscribe `35=V`. Persist-shadow sees `quoteRow == null`. Overview `destinationRealPnl` is the literal **0** in `GetOverviewAsync` (the `0` after `shadowPnl`). | Market orders with no bid/ask, no age, no spread cap that can fire. That is a **blind** send. Blind gold size has **negative** expectancy vs a measured book (you pay the offer you cannot see, you cannot refuse a $2+ print, you cannot mark). |
| **Dest recon** | Architecture §68 G07 / §70 dest-position recon after restart: **FAIL** (S022: §68 **0/19**, §70 **0/14**). `FixSessionStatus.Reconciling` exists as an enum and a dashboard “healthy” OR-clause, not as a working dest ledger vs venue. Source `TradeReconstructor` is wired to ingest; **destination** fill/position recon is not. | Without dest recon you cannot know open dest XAU, cannot flatten, cannot detect a partial/unknown fill. First send can double, orphan, or leave exposure the risk book never saw. That is **−EV** vs “do not send.” |
| **Dest sizing** | `QuantityNormalizer` is a last-stage dest grid (`sourceLots × allocationFactor`, floor, min→0). **Zero** product callers. No ounces converter (S009 / A43). `PersistDemoShadowAsync` copies `trade.MaxVolumeLots` straight into `RequestedQuantity`. Tag `38` / `OrderQty` construction in product `*.cs` is **absent** (SAFE_BY_ABSENCE on the wire, **−EV if a sender is bolted on**). | 1:1 lots on demo 2.0-lot explosions, or `38=0.10` on a BaseUnits book (100× too small / 100× too big depending on convention). Either dust-rejects or account death. Both are **−EV** vs not sending. |

Overview dest PnL is not computed from fills:

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
            shadowPnl,
            0,
            0,
            0,
            // ...
            _runtime.RealCopyEnabled);
```

`0` here is **absence of a dest ledger**, consistent with no quotes, no dest recon, no send. It is **not** evidence that a first live clip would print 0.

S022 remains binding: `REAL_COPY_EXECUTION_ENABLED` stays **false**. Enabling it to chase the +78,276 island would convert an **unknown after-spread** source number into a **known-negative** operational bet (blind quotes, no dest recon, unsized lots) on **demo-selected** gold.

```text
send-now  =  copy a demo SHADOW island
          ×  no dest bid/ask
          ×  no dest position recon
          ×  source lots as dest qty
          =  NEGATIVE EV
          (and illegal under §68 / §70)
```

Capital at risk **today** from this process: **NONE** (`SAFE_BY_ABSENCE` — no `35=D` builder). Residual operator risk: reading +78,276 or dest PnL 0 as a license to send.

---

## 5. What would have to be true before any of these EVs can flip

Not a build list. A measurement bar. This slot does **not** implement any of it.

| Bar | Copy-all | Copy-SHADOW uncosted | Send-now |
|---|---|---|---|
| Drop `RISK_BLOCKED` / `martingale=true` | Required just to stop the −241,579 tail | Already true of the SHADOW token on **losing** martingales; winning martingales can still sit in SHADOW (C32 / S007) | Required |
| Live dest quote persist + age | Does not save copy-all | Required to **measure** after-spread EV | Required |
| Shadow MTM / exit on **that** tape | N/A (still −EV) | Required; `MarkToMarket` must actually run | Required before size > 0 |
| Dest recon after restart | N/A | Needed before treating shadow as a book | Required (§68 G07) |
| Ounce path + tiny `allocationFactor` + dest cap | N/A | Required before the island can be sized | Required (S006 / S009 / S026) |
| Funded / out-of-sample, not demo challenge | N/A | Required before the +78,276 can be believed | Required (S004) |
| `CanPromoteToLive` stays false until dest sample exists | — | — | Binding (S011) |

Until those bars are **measured**, the three answers stay:

1. **Copy-all: negative.**
2. **Copy-SHADOW-demo-uncosted: unknown after spread.**
3. **Send-now: negative.**

---

## 6. Honesty / non-claims

- This file did **not** re-query the API. Integers are the parent prompt: XAU book **−154,425**, SHADOW **+78,276**, BLOCKED **−241,579**, dest PnL **0**, SHADOW **all demo**.
- This file did **not** compute a dollar after-spread EV for the SHADOW island. That computation is **impossible** without a dest quote tape.
- This file does **not** claim A22 Case B is implemented.
- This file does **not** claim dest PnL 0 is a trading result.
- This file does **not** recommend a live clip “to measure EV.” Measuring dest EV is **shadow after costs**, not `35=D`.
- Product source was **not** modified.

---

*End P500_S054. Product source was not modified. Copy-all is −EV. SHADOW-demo uncosted is unknown after spread. Send-now is −EV (no quotes / no dest recon / no sizing).*
