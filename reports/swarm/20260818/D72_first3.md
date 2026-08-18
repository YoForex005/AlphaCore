# D72 — Is first-3 reconstructed completed XAU only?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D72_first3.md` |
| Agent | D72 (first-3 XAU-only audit) |
| Date | 2026-08-18 |
| Assigned | Answer: is first-3 reconstructed **completed XAU only**? Write this file. Do **not** modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| Method | Read architecture §15, A21 §5 / §7.7 / F06, live Domain/Application/Infrastructure/tests, Fake MT5 tape, dashboard highlight. Ran a reports-only eval against `TraderIntelligence.Domain` (no product edit). Hashed current files. |
| Law | Architecture v2 §15–16; `A21_reconstruction_spec.md`; A57 item 6; C13 item 6 |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d72_first3\` (scratch; not product) |
| Eval stdout | `D:\Prop\reports\swarm\20260818\_tmp_d72_first3\stdout.txt` |

---

## 0. Verdict

**Counting rule: YES. Reconstruction increment: NO (not complete). Engine: NOT XAU-only.**

“First 3 trades” is **defined** as **three completed reconstructed XAUUSD position lifecycles**. Non-XAU, still-open, and leftover-after-partial books **must not** occupy a first-3 slot (architecture §15; A21 F06).

The **in-memory helper** implements that filter:

```60:76:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public IReadOnlyList<ReconstructedTradeResult> CompletedXauUsdTrades(...)
    {
        return Reconstruct(...)
            .Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)
            ...
    }
    public bool IsEarlyScoreEligible(...) =>
        CountCompletedXauUsdTrades(...) >= 3;
```

**Measured** (`M3_2XAU_1EUR`): 2 completed XAUUSD + 1 completed EURUSD → `count=2`, `eligible=false`. EURUSD does **not** fill the third slot.

That is **not** the same as:

| Claim | True? |
|---|---|
| Reconstruction **drops** non-XAU deals (A21 §5) | **No.** `Reconstruct` still emits EURUSD / XAGUSD books (`M1_BOOK` pos 3–4). |
| First-3 increment is **done** (durable keys, A21 F06, §69.6) | **No.** Helpers + demo tape only. C13 item 6 = **DEMO**, not accepted. |
| Production score uses the helper | **No.** `RebuildTraderAsync` scores `Completed && IsXauUsd` and **ignores** `EligibleForFirstThree` (`M5_CANCEL`: helper 2 / score 3). |
| Dashboard `isFirstThree` is persisted | **No.** Recomputed as first 3 `Completed && CanonicalSymbol == "XAUUSD"`; no dirty bit. |
| Helper count is capped at 3 | **No.** `CountCompletedXauUsdTrades` is **all** eligible completed XAU (`M4_FOUR_XAU` = 4). Dashboard highlight caps at 3. |

**One-line for operators:** first-3 **means** completed reconstructed XAU only; the helper **counts** that way; the engine **still reconstructs every symbol**; the product **has not finished** first-3 (no `first3_keys`, score/dashboard can leak dirty XAU).

Do **not** claim “first-3 reconstruction is complete.” Do **not** claim “reconstruction is XAU-only.”

---

## 1. What the law requires

Architecture v2 §15 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L667–695):

```text
3 completed reconstructed XAUUSD position lifecycles
```

Do **not** count: order place, deal fill, partial close, SL/TP modify. Trade #3 emits `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`.

A21 §1.2 / §5 / §7.7 / F06 add:

- Non-XAUUSD lifecycle → **no** first-3 slot (`skipped_non_xau`; deals **dropped** before the book).
- Still-open / dirty / failed lifecycle → **no** first-3 slot.
- Aliases `XAUUSD.`, `XAUUSDm`, `GOLD` map to canonical `XAUUSD` and **do** count.
- `first3_keys` holds **only the first three** completed clean XAU keys. `completed_count` may be 4+.
- Latch identity is apply order `(closed_at_msc, ticket)`, not a later re-sort by `position_id`.

`docs/trade-reconstruction.md` §“First-3-Trade Semantics” is **weaker and wrong** vs §15: it says “first 3 completed trades on an account” without XAU-only. Ignore that paragraph when it conflicts with architecture / A21.

---

## 2. Files read (current SHAs)

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | 12768 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | 2042 | `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | 1171 | `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 8143 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4535 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 12097 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | 1430 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` |
| `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` | 3116 | `808CBA1F9C9F1FFF1647C0FDC9BD896BA1ECEBB463D22F971D0B4DDF6E687458` |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | 4895 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| `D:\Prop\docs\trade-reconstruction.md` | 2293 | `500B4FF1C538EAFDEBBCAF189DA24DD4BF0E41A285E64ED68C86BC7C7E2008A1` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | — | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |

`ReconstructedTradeResult` has `EligibleForFirstThree` (default true) and computed `IsXauUsd` (`CanonicalSymbol == "XAUUSD"`). **No** `Dirty`, **no** `LifecycleSeq`, **no** `first3_keys`. Entity `ReconstructedTrade` has **none** of those (eval `META entityEligible=False entityFirst3OrDirty=False`).

---

## 3. Surface-by-surface answer

### 3.1 Contract — completed XAU only

**YES.**

| Event | Counts toward first-3? |
|---|---|
| Completed reconstructed XAUUSD / mapped alias (`XAUUSDm`, `XAUUSD.`, `GOLD`) | Yes |
| Completed EURUSD / XAGUSD / XAUEUR / SILVER | No |
| Open XAU lifecycle | No |
| Partial close with remaining > 0 | No (same lifecycle; increment only when flat) |
| Balance / credit | No |
| Dirty / canceled-tainted XAU | No (A21; helper honors `EligibleForFirstThree`) |

### 3.2 `TradeReconstructor` helper — completed XAU only

**YES**, for `CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible`.

`IsXauUsd` is canonical-code equality, so aliases that `SymbolNormalizer` maps to `XAUUSD` count. Unmapped symbols keep the raw source as `CanonicalSymbol` and `IsXauUsd=false`.

Cancel taint: any `BuyCanceled`/`SellCanceled` on a `position_id` sets **every** lifecycle of that id `EligibleForFirstThree=false` (position-wide, not current-seq only). Helper then excludes them.

### 3.3 `Reconstruct` — **not** XAU-only

**NO.** A21 step 3 drops non-XAU before opening a book. Live code groups **all** trading deals by `position_id`. Non-XAU completed books are persisted by `ReplaceReconstructedAsync` unchanged.

`M1_MIXED`: `recon=7`, `completed=5`, of which only 3 are XAU.

### 3.4 Scoring persist — XAU completed, **not** first-3 / not dirty-safe

```86:96:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        ...
            CompletedXauTrades = score.Features.CompletedXauTrades,
```

`BaselineScorer` re-filters `Completed && IsXauUsd` and sets `EarlyScoreEligible = CompletedXauTrades >= 3`. It never sees `EligibleForFirstThree`.

| Tape | Helper count | Helper eligible | Score `CompletedXauTrades` | Score eligible |
|---|---:|---|---:|---|
| 2 XAU + 1 EUR (`M3`) | 2 | false | 2 (if passed filtered list) / 2 (scorer re-filters) | false |
| 4 XAU (`M4`) | **4** | true | **4** (all XAU, not first 3) | true |
| 2 clean XAU + 1 canceled-tainted XAU (`M5`) | **2** | **false** | **3** | **true** |

So production **can latch EARLY_SCORE on a dirty XAU book**. First-3 **identity** (`first3_keys`) is never written.

### 3.5 Dashboard highlight — completed XAUUSD, first 3 by time

```140:157:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var firstThree = 0;
        var highlights = trades.Select(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && firstThree < 3;
            if (first)
                firstThree++;
            ...
```

**YES** for symbol + completed + cap-3. **NO** for dirty exclusion (column does not exist). Order is `ClosedAt ?? OpenedAt`, not A21 completing-ticket apply order.

Leaderboard `NetSourcePnl` (`D21` Q3) sums **all** completed reconstructed trades — **not** XAU-only. That is PnL leakage, not first-3 leakage.

React footer (`TraderDetailPage.tsx` L44) correctly states “First 3 completed XAUUSD trades.” The UI does not compute first-3 itself.

### 3.6 Demo / Fake MT5 — XAU-only **by tape**, not by proof

`FakeMt5BrokerConnector.ClosedRoundTrip` hardcodes `Symbol = "XAUUSD"`. Achiever 10001 has **3** closed XAU round-trips; 10002 has 3 (martingale); Starwave 99001 has 3. **Zero** EUR/XAG/GOLD/open/partial/cancel rows.

`SeedingAndStoreTests` asserts `CompletedXauTrades == 3` for login 10001 and `Any(completed && CanonicalSymbol == "XAUUSD")`. That does **not** prove non-XAU exclusion.

### 3.7 Tests — first-3 is not locked as XAU-only

`TradeReconstructionTests` (6 facts, SHA `CB223DDE…`):

| Fact | XAU-only exclusion? |
|---|---|
| `First_three_completed_xau_unlocks_early_score` | Positive latch on 3 `XAUUSDm` only. No EUR spacer. |
| `Canceled_deal_on_a_position_excludes_it_from_first_three` | Dirty XAU excluded. Still all `XAUUSDm`. |
| `Ignores_balance_deals` | Balance empty, not mixed-symbol. |
| Helper `Deal(...)` | **Always** `SourceSymbol = "XAUUSDm"`. |

`FirstThreeCompletedXauTradesTests` (A27 / A89) is **absent**. A21 F06 is **not** encoded. D33/C01 remain valid: first-3 tests are smokes.

---

## 4. Measured eval (2026-08-18, Domain Release)

Reports-only project `D72First3Eval` referenced `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj`. Product source not touched.

### 4.1 Mixed book (`M1`)

Tape: 2 completed XAU aliases + 1 EURUSD + 1 XAGUSD + 1 GOLD + 1 open XAU + 1 partial XAU.

| Metric | Value | Law |
|---|---|---|
| `Reconstruct` rows | 7 | A21 would emit **3 completed XAU + 1 open + 1 partial** and skip EUR/XAG |
| Completed any symbol | 5 | EUR + XAG extra |
| `Completed && IsXauUsd` | 3 | pos 1 (`XAUUSDm`), 2 (`XAUUSD.`), 5 (`GOLD`) |
| `CountCompletedXauUsdTrades` | **3** | GOLD is XAU; EUR/XAG/open/partial out |
| `IsEarlyScoreEligible` | **true** | third slot is GOLD, not EUR |
| Dashboard first-3 positions | `[1,2,5]` | same three |

### 4.2 Alias / unmapped matrix (`M2`)

| Source | Maps to XAUUSD? | First-3 count on a lone round-trip |
|---|---|---:|
| `XAUUSD` / `XAUUSD.` / `XAUUSDm` / `XAUUSD.a` / `GOLD` | Yes (catalog) | 1 |
| `GOLD.` / `XAUUSDFUT` / `XAUUSD.c` | **Yes (over-map)** | **1 — false XAU first-3** |
| `XAUEUR` / `XAGUSD` / `EURUSD` / `SILVER` | No | 0 |

A21 F24 allow-list would **unmap** `XAUUSDFUT` / `XAUUSD.c`. Prefix `StartsWith("XAUUSD")` plus compact `GOLD` is why they leak. D15 already flagged this; D72 confirms it **increments the first-3 helper**.

### 4.3 Other cases

| Id | Tape | Helper count | Eligible | Notes |
|---|---|---:|---|---|
| `M3` | 2 XAU + 1 EUR | 2 | false | **Core YES** |
| `M4` | 4 XAU | 4 | true | Count is not “first 3”; dashboard highlight = 3 |
| `M5` | 2 clean + cancel-tainted | 2 | false | Score N=3, score eligible=**true** (production leak) |
| `M6` | Rollover pair + Client IN + Settlement OUT | 0 | — | Reasons skip trading apply; leftover open |
| `M7` | Balance + credit + open + partial | 0 | false | Noise does not increment |
| `M8` | XAU IN + XAG OUT same `position_id` | **1** | — | Canonical taken from **first** deal → fake XAU complete |
| `M9` | `GOLD.` maps; `XAUEUR` does not | — | — | compact-GOLD over-map |

`META`: `hasDirty=False`, `hasSeq=False`, `hasFirst3Keys=False`, `hasEligible=True`, `earlyN=3`.

---

## 5. Is the first-3 increment **completed**?

**No.**

| Gate | Status |
|---|---|
| Architecture §15 definition | Written |
| A21 F06 fixture | Written, **not** encoded in tests |
| In-memory XAU completed filter | **Exists** |
| Durable `first3_keys` / `early_score_deal_ticket` | **Missing** |
| Persist `EligibleForFirstThree` / `Dirty` / `isFirstThree` | **Missing** |
| Score job uses helper | **No** |
| Outbox `EARLY_SCORE_ELIGIBLE` on trade #3 only | ScoreUpdate payload is `state` + raw completed count; not the A21 event |
| `FirstThreeCompletedXauTradesTests` | **Absent** |
| A57 item 6 | **PARTIAL helpers / not durable** |
| C13 / D41 §69.6 | **DEMO**, not accepted |

Demo login 10001 showing `CompletedXauTrades=3` is **three canned XAUUSD round-trips**, not a mixed-symbol proof.

---

## 6. Gaps that can still poison first-3

Even with “XAU only” on the helper:

1. **Over-map** (`XAUUSDFUT`, `XAUUSD.c`, `GOLD.`) counts as XAU.
2. **Mixed-symbol position** (XAU IN, XAG OUT) stays one completed XAU book (`M8`).
3. **Cancel taint** excluded in helper, **included** in `ReconstructionScoringService` and dashboard highlight (`M5`).
4. **Same-sign INOUT phantom complete** still counts (D11 `B18`: helper 3 / eligible true). Not re-run here; still open.
5. **No first-3 identity** — trade #4 is in `CompletedXauTrades` and in the scorer feature window.
6. **`docs/trade-reconstruction.md`** omits XAU-only (doc drift).

---

## 7. Done when (not done now)

- A21 F06 encoded bit-for-bit: EUR skip, GOLD partial then complete as #3, 4th XAU not in `first3_keys`.
- `Reconstruct` either drops non-XAU or first-3 / persist / score **all** use `Completed && IsXauUsd && EligibleForFirstThree`.
- `first3_keys` persisted; dashboard reads them (does not re-derive without dirty).
- Over-map suffixes fail closed (unmapped ≠ XAU).
- Score latch uses the helper, not an unfiltered completed-XAU list.

Until then: **first-3 is specified and helper-counted as completed XAU only; reconstruction is not XAU-only; the increment is not complete.**
