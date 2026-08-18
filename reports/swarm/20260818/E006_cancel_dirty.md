# E006 — `TradeReconstructor` dirty canceled positions

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E006_cancel_dirty.md` |
| Agent | E006 (cancel-dirty close-read) |
| Date | 2026-08-18 |
| Assigned | Read `TradeReconstructor` dirty canceled positions. Write this file. Do **not** modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (347 lines, 12 768 B, SHA-256 `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B`) |
| Law | A21 §4.4 / §6 / F17; A83 encodings A/B/C; A37 `EnDealAction` 13/14 |
| Method | Read reconstructor + result + `NormalizedDeal` + ingest/store/score/dashboard. Hashed current files. `dotnet test --filter FullyQualifiedName~TradeReconstructionTests` → **6/6 passed**. Reports-only eval `D:\Prop\reports\swarm\20260818\_tmp_e006_cancel\` (Release, Domain reference only) ran F17 / F17b–g / netting / score-leak tapes. |
| Eval stdout | `D:\Prop\reports\swarm\20260818\_tmp_e006_cancel\stdout.txt` |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `STALE_REPORT`.

---

## 0. Verdict

**There is a cancel-dirty path. It is not A21 dirty.**

`TradeReconstructor.Reconstruct` builds a `HashSet` of `PositionId`s that have any `DealAction.BuyCanceled` (13) or `SellCanceled` (14) on the scoped `(broker, login)` list. Those ids are still reconstructed from **Buy/Sell only**. After the book is built, **every** emitted lifecycle on that id is rewritten `EligibleForFirstThree = false`.

That is **position-wide first-3 taint**, not A21’s `dirty` bit on `(position_id, lifecycle_seq)` plus `RECON_CANCELED_DEAL`.

| Surface | Sees 13/14? | Effect | Class |
|---|---|---|---|
| `NormalizedDeal.IsTradingDeal` | **No** (false) | 13/14 never apply as IN/OUT/INOUT. **No inverse fill.** | `EXISTS_AND_GOOD` |
| `dirtyPositions` scan | **Yes** (`Action is BuyCanceled or SellCanceled`) | All rows of that `PositionId` → `EligibleForFirstThree=false` | `EXISTS_AND_GOOD` for extra-ticket first-3 helpers |
| `CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` | Indirect | Requires `Completed && IsXauUsd && EligibleForFirstThree` | `EXISTS_AND_GOOD` |
| `ReconstructedTradeResult.Dirty` / `FailureCode` | — | **No such properties** (`META hasDirty=False hasFailure=False`) | `MISSING` |
| `RECON_CANCELED_DEAL` metric / event | — | **Zero** hits under `D:\Prop\src` | `MISSING` |
| Canceled-only stub (F17b) | Scan yes, group no | Empty reconstruct. No dirty stub. | `MISSING` |
| Persist `ReconstructedTrade` | **No** | Entity has no `Eligible*` / `Dirty`. `ReplaceReconstructedAsync` drops the flag. | `MISSING` |
| `ReconstructionScoringService.RebuildTraderAsync` | **No** | Scores `Completed && IsXauUsd` only. **M5: helper 2/false, score 3/true, state=SHADOW.** | `UNSAFE` |
| Dashboard `isFirstThree` | **No** | First 3 `Completed && CanonicalSymbol=="XAUUSD"`. **DASH highlighted=3 including dirty pos 3.** | `UNSAFE` |
| Demo shadow | **No** | `PersistDemoShadowAsync` walks the same unfiltered completed-XAU list. | `UNSAFE` |
| Official in-place 0→13 (Encoding A) | Only if ingest stored 13 | `UpsertDealAsync` first-write-wins. Stale `Buy` stays tradeable. | `UNSAFE` |
| A83 §0 / §9, C31 C9, B11 H1 “never dirties”, D33 “no 13/14” | — | False on SHA `AEA3930B…` / test SHA `CB223DDE…` | `STALE_REPORT` |

**One-line:** canceled rows skip the volume book and taint that `position_id` out of the **in-memory** first-3 helpers; they do **not** set `Dirty`, do **not** emit `RECON_CANCELED_DEAL`, and production score / persist / dashboard / shadow still treat a flattened tainted XAU book as a real first-3 trade.

Do **not** claim “canceled positions are dirty.” Do **not** claim “first-3 is cancel-safe.” Do **not** quote A83 as the live product map.

---

## 1. Files read (current SHAs) — product not edited

| Path | Bytes | Lines | SHA-256 |
|---|---:|---:|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | 12 768 | 347 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | 2 042 | 43 | `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | 1 171 | 29 | `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` |
| `D:\Prop\src\Domain\Enums\DealAction.cs` | 622 | 29 | `6E87BFB536D43A57B48D548A0718E3C8C2E4914CE3CD0577410E6CB61D5054F1` |
| `D:\Prop\src\Domain\Enums\DealReason.cs` | 1 149 | 50 | `3A4D92122D72155ACA3C0D9174758A966741A8B6E830917E6463CD905659E593` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | 1 430 | 36 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` |
| `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | 836 | 24 | `C81AEE8F15DA0EB1449DA3549A0FDD809D8C1607B9964F908830DD8F371F5487` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4 535 | 106 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 12 097 | 338 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8 708 | 205 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 8 143 | 212 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | 4 895 | 131 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` | — | — | `B150AC5069E93D615FE12AC5C8873E2ECFECD563E5F1E841D4C216A02F69C068` |
| `D:\Prop\reports\swarm\20260818\A83_canceled_deals.md` | — | — | `D59D986E30F1D7058EFF03B2FB4AE9558F558E05A93E655D5DA5B6F478F2127E` |
| `D:\Prop\reports\swarm\20260818\D73_canceled.md` | — | — | `7AC73C7EC22B7FC7ED7B881C934333AA1DB34D283C198F2823D8DE5FEE3304FB` |

`META` (eval reflection): `hasDirty=False`, `hasFailure=False`, `hasSeq=False`, `hasEligible=True`, `hasFirst3Keys=False`, `entityDirty=False`, `entityEligible=False`, `entityFailure=False`, `earlyN=3`, `isTradingBuyCanceled=False`, `isTradingSellCanceled=False`.

Grep of `RECON_CANCELED` / `canceled_deals_total` / `reconstruction_failures` under `D:\Prop\src`: **0**.

Grep of `BuyCanceled` / `SellCanceled` under `D:\Prop\src`: enum + reconstructor dirty scan only.

---

## 2. What A21 / A83 require

A21 §6 third class + step 4:

```
is_canceled := action ∈ {13, 14}
→ RECON_CANCELED_DEAL
→ dirty current (or last) lifecycle of that position_id
→ do not invent an inverse fill
→ first-3 ignores dirty
```

A83 restates official MetaQuotes: the canceled row **is** the original ticket with action rewritten 0→13 / 1→14 and P/L zeroed. Clawback is a **separate** balance deal. Volume on 13/14 is forensic, not apply input.

Dirty scope (A83 §5.3):

| Scope | Rule |
|---|---|
| Key | `(broker_id, login, position_id, lifecycle_seq)` current if open, else last seq |
| Cancel-only id | Persist a **dirty stub** (not completed, not first-3). Do not invent direction from 13/14 |
| Other `position_id`s | Untouched |
| Non-XAUUSD cancel | `skipped_non_xau++`, **no** XAU dirty |
| Subsequent tradeable on a dirty open | Stay dirty; still apply if volume rules allow |
| First-3 | `completed ∧ ¬dirty ∧ XAUUSD` |

Two encodings:

- **A (official):** latest-per-ticket is 13/14. Surviving 0/1 tickets are the book. Skip + dirty.
- **B (A21 F17 extra ticket):** original BUY/SELL **and** a later 13/14. Remaining may be wrong. Dirty is mandatory so first-3 does not trust it.

---

## 3. What the engine actually does

```24:76:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public IReadOnlyList<ReconstructedTradeResult> Reconstruct(...)
    {
        var scoped = deals
            .Where(d => string.Equals(d.BrokerId, brokerId, StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Login == login)
            .ToList();

        var dirtyPositions = scoped
            .Where(d => d.Action is DealAction.BuyCanceled or DealAction.SellCanceled)
            .Select(d => d.PositionId)
            .ToHashSet();

        var trading = scoped
            .Where(d => d.IsTradingDeal)
            .OrderBy(d => d.Time)
            .ThenBy(d => d.DealTicket)
            .ToList();
        // GroupBy PositionId → ReconstructPosition (IN/OUT/INOUT only)
            if (dirtyPositions.Contains(group.Key))
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
        // ...
    }

    public IReadOnlyList<ReconstructedTradeResult> CompletedXauUsdTrades(...)
    {
        return Reconstruct(...)
            .Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)
            ...
    }
```

```26:28:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public bool IsTradingDeal =>
        Action is DealAction.Buy or DealAction.Sell
        && DealReasons.CountsAsTraderActivity(Reason);
```

```38:38:D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs
    public bool EligibleForFirstThree { get; init; } = true;
```

Implications (measured, not guessed):

1. **Volume book never applies 13/14.** Entry on a canceled row is ignored because the row never reaches `ReconstructPosition`. Matches A21 `is_tradeable` and A83 “never invert.”
2. **Canceled is a third class in the engine**, not silent balance-like. Balance is not in `dirtyPositions`.
3. **Default eligibility is true.** Only this scan flips it. `ToResult` never writes the flag. There is no `Dirty` bool.
4. **Taint key is `position_id` for the whole group**, not current/last `lifecycle_seq`. Netting reuse after a historical cancel marks **every** later lifecycle ineligible (`NETTING` below).
5. **Scan is symbol-blind.** An EURUSD 13/14 on the same `position_id` as an XAU book would taint it. A21/A83: non-XAU cancel must not dirty XAU. Different ids (F17f) happen to be safe.
6. **Canceled-only tape:** `dirtyPositions` nonempty, no `IsTradingDeal` in that group → **no row**. No stub. No failure code.
7. **Broker/login scope works.** `OTHER_LOGIN` / `OTHER_BROKER` cancels never enter `scoped`. Three clean XAU stay eligible.
8. **Reason is ignored** on 13/14. `CorporateAction` on a `BuyCanceled` still dirties (`REASON`). The reason conjunct on `IsTradingDeal` is unreachable for 13/14.

`docs/trade-reconstruction.md` does not mention cancel taint at all.

---

## 4. Measured tapes (2026-08-18, Domain Release)

Reports-only project `E006CancelEval` referenced `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj`. Product source not touched. Volume = Manager native `1000` = 0.10 lots (A21 `volume_h=10`).

`helperN` = `CountCompletedXauUsdTrades`. `helperElig` = `IsEarlyScoreEligible`. `scoreN` / `scoreElig` = `BaselineScorer` on `Completed && IsXauUsd` (**ignores** `EligibleForFirstThree`), which is what `RebuildTraderAsync` does.

### 4.1 Extra-ticket / official / latch

| Id | Tape | n | completed | helperN | helperElig | scoreN | scoreElig | Notes |
|---|---|---:|---:|---:|---|---:|---|---|
| `UNIT` | Unit fact: pos 10 IN/OUT + later `BuyCanceled` + two clean XAU | 3 | 3 | **2** | **false** | **3** | **true** | Pos 10 `elig=false`, tickets=`[1,2]`. Score latch **SHADOW**. |
| `F17` | 961 BUY + 962 `BuyCanceled` on 5017; 963/964 complete 5018 | 2 | 1 | **1** | false | 1 | false | 5017 **open** rem=0.10, `elig=false`, tickets=`[961]` (962 not a fill). 5018 clean. Spec `completed_count=1`. |
| `F17_FLAT` | 961 + 962 + later real OUT 965 | 1 | 1 | **0** | false | **1** | false | Looks like F01: tickets=`[961,965]`, net=10, `elig=false`. Helper excludes; scorer still counts 1. |
| `F17B` | Latest row only = 970 `BuyCanceled` | **0** | 0 | 0 | false | 0 | false | **No stub.** Spec: dirty stub + `RECON_CANCELED_DEAL`. |
| `F17C` | BUY 0.10 + canceled extra 0.20 + OUT 0.10 | 1 | 1 | **0** | false | 1 | false | tickets=`[971,973]`, rem=0, **no overclose**. 972 volume not inverted. |
| `F17D` | BUY + `SellCanceled` OUT | 1 | 0 | 0 | false | 0 | false | Stays **open** rem=0.10, tickets=`[981]`. Close was not applied. |
| `F17E` | F17b + BALANCE −1.00 pos 0 | 0 | 0 | 0 | false | 0 | false | Balance skipped. No XAU book. Clawback not attached. |
| `F17F` | EURUSD `BuyCanceled` pos 9 + clean XAU pos 40 | 1 | 1 | 1 | false | 1 | false | EUR group empty. XAU `elig=true`. No XAU dirty. `helperElig=false` because count is 1, not 3. |
| `F17G_BEFORE` | 3 clean XAU | 3 | 3 | 3 | **true** | 3 | true | Latch exists in helper **and** score. |
| `F17G_AFTER` | Same tape; ticket 15 rewritten `SellCanceled` | 3 | 2 | **2** | **false** | 2 | false | Pos 102 reverts to open rem=0.10, `elig=false`. Helper **retracts**. Score also 2 only because the book is no longer completed. |
| `SELL_CXL` | Same as UNIT with `SellCanceled` | 3 | 3 | 2 | false | 3 | true | `SellCanceled` equals `BuyCanceled` on the scan. |
| `NETTING` | Complete pos 50, cancel, new lifecycle same id | 2 | 2 | **0** | false | 2 | false | **Both** seqs `elig=false`. A21 would taint last/current only; seq 1 could stay first-3. |
| `POS0` | `BuyCanceled` pos 0 + 3 clean XAU | 3 | 3 | 3 | **true** | 3 | true | Pos 0 has no trading group. Other books untouched. |
| `OTHER_LOGIN` | Cancel login 2, reconstruct login 1 | 3 | 3 | 3 | true | 3 | true | Scoped out. |
| `OTHER_BROKER` | Cancel `STARWAVE`, reconstruct `ACHIEVER` | 3 | 3 | 3 | true | 3 | true | Scoped out. |
| `REASON` | Completed + `BuyCanceled` reason=`CorporateAction` | 1 | 1 | 0 | false | 1 | false | Action scan ignores reason. |
| `EMPTY_SYM` | XAU IN + cancel with `SourceSymbol=""` | 1 | 0 | 0 | false | 0 | false | Still dirties by `position_id`. Better than A83 empty-symbol miss. |
| `M5` | 2 clean + 1 completed-then-canceled | 3 | 3 | **2** | **false** | **3** | **true** | **Production leak.** `state=SHADOW`. |
| `NO_INVERSE` | BUY 0.10 + `BuyCanceled` 0.20 | 1 | 0 | 0 | false | 0 | false | rem stays **0.10**. Did **not** subtract 0.20. |

### 4.2 Dashboard highlight (`DASH` = M5 order)

`EfDashboardQueries.GetTraderDetailAsync` does **not** read `EligibleForFirstThree` (column does not exist after persist). Simulated on in-memory rows with the same predicate the query uses:

| pos | Completed | EligibleForFirstThree | `first` highlight |
|---|---|---|---|
| 1 | true | true | **true** |
| 2 | true | true | **true** |
| 3 | true | **false** | **true** (dirty still highlighted) |

`highlighted=3`. Dirty completed XAU occupies a first-3 slot on the trader-detail page.

### 4.3 Persist census

`ReconstructedTrade` properties: `BrokerId, CanonicalSymbol, ClosedAt, ClosedVolumeLots, Commission, Completed, DealCount, Direction, EntryVwap, ExitVwap, Fees, FinalSl, FinalTp, GrossRealizedPnl, Id, InitialSl, InitialTp, InitialVolumeLots, Login, MaxVolumeLots, NetRealizedPnl, OpenedAt, OrderCount, PositionId, SourceSymbol, Swap, WasAveragedDown, WasPartialClose, WasScaledIn`.

**Absent vs result:** `EligibleForFirstThree`, `DealTickets`, `RemainingVolumeLots`, string `Id`, `IsXauUsd`. **Absent vs A21:** `Dirty`, `FailureCode`, `LifecycleSeq`.

`TraderDbContext` maps `reconstructed_trades` with index `(BrokerId, Login, PositionId, OpenedAt)` only. No dirty column.

---

## 5. Production consumers ignore the flag

```82:87:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
```

`BaselineScorer` re-filters `Completed && IsXauUsd` and sets `EarlyScoreEligible = CompletedXauTrades >= 3`. It never reads `EligibleForFirstThree`.

`PersistDemoShadowAsync` receives that same `completedXau` list. A dirty completed XAU on a `SHADOW` account can emit a `CopyIntent` keyed `shadow:{broker}:{login}:{positionId}`.

`ReplaceReconstructedAsync` copies no eligibility field. After a rebuild, **nobody** can recover the taint from EF.

`UpsertDealAsync`: if `(BrokerId, DealTicket)` exists → return false, **do not update `Action`**. Official Encoding A (`BUY` → `BUY_CANCELED` on the same ticket) **never reaches** the dirty scan. `IsTradingDeal` stays true on the stale fill. That is the highest-probability production miss.

`Mt5Deal` / `LoadDealsAsync` have no `Reason`. Irrelevant to 13/14 (action scan). Relevant to D44 on Buy/Sell.

---

## 6. Tests (measured this review)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~TradeReconstructionTests
  --nologo
→ Passed!  Failed: 0, Passed: 6, Skipped: 0
```

| Fact | What it locks |
|---|---|
| `Canceled_deal_on_a_position_excludes_it_from_first_three` | Extra-ticket `BuyCanceled` on pos 10 + two clean → `Reconstruct` completed **3**, `CountCompletedXauUsdTrades` **2**, `IsEarlyScoreEligible` **false** |
| Other five facts | Happy-path book. No 13/14. |

Not locked:

- `SellCanceled` (eval `SELL_CXL` proves the scan; no fact)
- `EligibleForFirstThree` on the row itself (fact uses count helpers only)
- Scoring / persist / dashboard ignore
- F17 tickets 961–964 / F17b stub / F17c no-overclose / F17d stay-open
- `RECON_CANCELED_DEAL`
- Official same-ticket mutation
- Netting-reuse over-taint
- Non-XAU cancel on the **same** `position_id`

`CanceledDealHandlingTests` (A89 #11) is **absent**. A21 F17 is **not** encoded bit-for-bit.

---

## 7. A21 F17 scorecard (this SHA)

| Fixture | Spec | Live | Result |
|---|---|---|---|
| F17 extra-ticket | 5017 dirty excluded; 5018 counts; no inverse | 5017 open `elig=false`; 5018 counts; 962 not in tickets | **PARTIAL** (helper yes; no `Dirty` / no `RECON_CANCELED_DEAL`) |
| F17 later flatten | Still dirty, not first-3 | `F17_FLAT` helperN=0, row looks clean complete | **PARTIAL** |
| F17b in-place only | Dirty stub, 0 completed | **0 rows** | **FAIL** (silent empty) |
| F17c cancel scale-in | +10 → 0, dirty, no overclose | rem path correct, tickets `[971,973]`, helperN=0 | **PARTIAL** |
| F17d cancel close | Stay open, not completed | rem=0.10, `elig=false` | **PARTIAL** |
| F17e clawback | Balance skipped, not on book | Empty reconstruct | **PASS** (skip) / no stub |
| F17f non-XAU cancel | No XAU dirty | Different pos: XAU stays `elig=true` | **PASS** only because ids differ |
| F17g latch retract | eligible false after rebuild | helper 3→2, eligible false | **PASS** on helper when close is retracted; **FAIL** if the tainted book stays completed (`M5`/`UNIT`) |
| Never invert | Do not subtract canceled volume | `NO_INVERSE` rem=0.10 | **PASS** |
| Persist dirty | `reconstructed_trades.dirty` | Column missing | **FAIL** |
| Score / first-3 consume dirty | A22 must exclude | Score N includes dirty completes | **FAIL** |

**Tally: 2 PASS / 5 PARTIAL / 4 FAIL** on the cancel family. Not ≥95% F17 parity.

---

## 8. Where cancel-dirty can fail without changing the scan

| Hole | Evidence | Result |
|---|---|---|
| First-write-wins ingest | `UpsertDealAsync` L87–90 | Encoding A never presents Action=13. Book stays **clean** and tradeable. |
| Score filter | `RebuildTraderAsync` L86 | Dirty completed XAU latches `EARLY_SCORE` / `SHADOW` (`M5`, `UNIT`, `SELL_CXL`) |
| Persist drop | `ReplaceReconstructedAsync` L178–209 | Dashboard cannot honor taint |
| Dashboard predicate | `EfDashboardQueries` L140–156 | Dirty XAU highlighted as first-3 |
| Shadow | `PersistDemoShadowAsync` L289 | Dirty complete can open a shadow intent |
| Position-wide taint | `NETTING` | Historical cancel permanently bans that `position_id` from first-3 helpers (false negative) |
| No stub | `F17B` | Cancel-only evidence disappears from reconstruct output |
| Symbol-blind scan | Action-only `HashSet` | EUR 13/14 on a reused XAU `position_id` would taint (A83 forbids) |
| No failure channel | Domain has no `RECON_*` type | Ops cannot alert `canceled_deals_total` |

The predicate `IsTradingDeal` is **not** the ingest hole. Asking “does the reconstructor dirty canceled positions?” on a **normalized list that already contains Action=13/14** is: **it taints first-3 eligibility on that position_id**. Asking whether **production history** always presents that list is **no** until upsert is latest-revision.

---

## 9. Stale reports (do not quote as live)

| Report | Claim | Live SHA |
|---|---|---|
| A83 §0 / §9 | “`TradeReconstructor` never dirties”; “tests: no cancel cases” | Dirty scan + 6th fact exist |
| B11 H1 | `RECON_CANCELED_DEAL` → skip; book stays **clean** | Extra-ticket flatten is `elig=false` |
| C31 C9 | count=3, eligible=true HARD FAIL | Unit + eval: helper count=2, eligible=false |
| C01 / D33 | 5 facts / cannot construct 13/14 | 6 facts; `BuyCanceled` constructed |
| D37 | `with` on class **does not compile** | Result is a `record`; 6/6 green |
| A89 #11 PARTIAL text | “silently skipped — document dirty-lifecycle gap” | Skip-fill still true; first-3 taint now exists |

Keep A83 for official in-place semantics, ingest rebuild, and “never invert.” Keep D73 for the `IsTradingDeal` truth table. This file owns the **dirty-position** map.

D11 H2 / D72 M5 / D49 T5 remain valid: helper excludes; score and dashboard do not.

---

## 10. Honesty box

| Claim someone might make | Measured |
|---|---|
| “Canceled positions are dirty” | **No `Dirty` bit.** Only `EligibleForFirstThree=false` on that `position_id`. |
| “First-3 is cancel-safe” | **Helpers yes. Production score / dashboard / shadow no.** |
| “Canceled deals are inverted” | **No.** `NO_INVERSE` rem=0.10; F17c no overclose. |
| “F17 is implemented” | **PARTIAL.** Extra-ticket helper exclude + no inverse. No failure code, no stub, no persist, score leak. |
| “6 unit tests pass ⇒ cancel-dirty is done” | Those tests do not assert the score path, persist, F17b, or `SellCanceled`. |
| “In-place Manager cancel is handled” | **Not if ingest is first-write-wins.** |
| Product source modified | **No.** |

---

## 11. Disposition

| Question | Answer |
|---|---|
| Does `TradeReconstructor` see canceled deals? | **Yes** — separate `dirtyPositions` scan on Action 13/14 |
| Does it apply them as fills? | **No** |
| Does it invert volume? | **No** (measured) |
| Does it mark positions dirty? | **It clears `EligibleForFirstThree` on every lifecycle of that `position_id`.** It does **not** set `Dirty` or `RECON_CANCELED_DEAL`. |
| First-3 helpers on extra-ticket cancel? | **Excluded** (unit 6/6, eval `UNIT`/`M5`/`F17_FLAT`) |
| Official same-ticket cancel? | **Not proven**; upsert can hide it |
| Production score / dashboard / shadow? | **Include** dirty completed XAU |
| A21 F17 bit-for-bit? | **No** |
| Product source changed? | **No** |

**Done when (not now):** A21 F17–F17g encoded; persist `dirty` + `failure_code=RECON_CANCELED_DEAL`; score / dashboard / shadow consume `Completed && IsXauUsd && EligibleForFirstThree` (or `!Dirty`); upsert latest-revision + full rebuild on 13/14; cancel-only stub; taint limited to current/last `lifecycle_seq`; non-XAU cancel cannot taint XAU.

Until then: **cancel-dirty is a first-3 helper flag on `position_id`, not a reconstruction failure model.**

**Product source was not modified.** This file is the only product-facing write from E006. Scratch eval lives under `D:\Prop\reports\swarm\20260818\_tmp_e006_cancel\` and is not product source.
