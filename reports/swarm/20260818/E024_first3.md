# E024 — Canceled position excluded from first-3?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E024_first3.md` |
| Agent | E024 (canceled-position × first-3 only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:43+05:30 |
| Assigned | Canceled position excluded from first-3. Write this file. **Do not modify product source.** |
| Workspace | `D:\Prop` |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Law | Architecture v2 §15; A21 §6 / §7.7 / F17; A83 (13/14 not fills; dirty + exclude); A22 scoring universe |
| Product source modified | **No.** This report (plus catalog notes) is the only write. |
| Test source modified | **No** |
| Method | Full read of reconstructor / helpers / scorer / ingest / store / dashboard / unit fact. SHA-256 + `git status`. `dotnet test` cancel/first-3 filter. Reports-only Domain eval `E024First3Eval` (scratch; not product). |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `STALE_REPORT`.

This is a **remeasure** of the same Domain SHAs D72/D73 already hashed (`AEA3930B…` / `EF41E774…` / `232573BF…` / `CB223DDE…`). It answers one claim only: **does a canceled position occupy a first-3 slot?**

---

## 0. Verdict (binding — do not greenwash)

**Helper YES. Production score / persist / dashboard NO. Official in-place cancel NOT PROVEN.**

A `position_id` that carries `DealAction.BuyCanceled` (13) or `SellCanceled` (14) is **excluded** from:

- `CompletedXauUsdTrades`
- `CountCompletedXauUsdTrades`
- `IsEarlyScoreEligible`

because `Reconstruct` stamps `EligibleForFirstThree = false` on **every** reconstructed lifecycle of that id, and the helpers require `Completed && IsXauUsd && EligibleForFirstThree`.

That is **not** the same as “the product excludes canceled positions from first-3.”

| Surface | Excluded from first-3? | Class |
|---|---|---|
| Volume book (`IsTradingDeal`) | 13/14 never apply as fills | `EXISTS_AND_GOOD` |
| In-memory dirty scan + helper | **Yes** (extra-ticket 13/14 on that `position_id`) | `EXISTS_AND_GOOD` |
| Unit fact `Canceled_deal_on_a_position_excludes_it_from_first_three` | **Yes** — completed=3, helper=2, eligible=false | `EXISTS_AND_GOOD` |
| A21 F17 extra-ticket (5017 dirty / 5018 counts) | Helper **yes**; no `RECON_CANCELED_DEAL` / no stub | `EXISTS_NEEDS_REFACTOR` |
| `ReconstructionScoringService.RebuildTraderAsync` | **No** — scores `Completed && IsXauUsd` only | `UNSAFE` |
| Persist `ReconstructedTrade` | **No column** — flag dropped | `MISSING` |
| Dashboard `IsFirstThree` | **No** — first 3 completed XAUUSD by time, ignores dirty | `UNSAFE` |
| Demo shadow (`PersistDemoShadowAsync`) | **No** — receives the unfiltered completed-XAU list | `UNSAFE` |
| Official same-ticket 0→13 (Encoding A) | **Not proven**; first-write-wins can hide 13 | `UNSAFE` |
| A21 `first3_keys` / dirty stub / metric | **Missing** | `MISSING` |
| C31 C9 (`count=3, eligible=true`); A83 §0 “never dirties” | **False on this SHA** | `STALE_REPORT` |

**One-line:** extra-ticket cancel **poisons that `position_id` out of the first-3 helper**; scoring, persist, and the trader-detail highlight still treat the completed XAU book as a first-3 trade and can latch `SHADOW`.

Do **not** claim “first-3 is cancel-safe in production.” Do **not** quote C31/A83 as the live engine. Do **not** treat demo login 10001’s three canned XAU round-trips as a cancel proof (`FakeMt5BrokerConnector` has **zero** 13/14 rows).

---

## 1. What the law requires

Architecture v2 §15: first-3 = **three completed reconstructed XAUUSD position lifecycles**. Trade #3 emits `EARLY_SCORE_ELIGIBLE`, not `PROVEN_PROFITABLE`.

A21 §6 / §7.7 / F17:

```text
is_canceled := action ∈ {13, 14}
on canceled (XAUUSD book):
  emit RECON_CANCELED_DEAL
  dirty current (or last) lifecycle of that position_id
  do not invent an inverse fill
  first-3 ignores dirty
if t.dirty: return          # complete_lifecycle does not occupy a first-3 slot
```

A83 restates: never apply 13/14 volume; dirty + exclude beats a silent wrong first-3; official encoding is **in-place ticket mutation** (0→13 / 1→14); F17 is the **unsafe extra-ticket** cousin. Non-XAU cancel is invisible to first-3. A rebuild that retracts a close must drop the latch.

Prop conservatism: **false negative over a silent wrong first-3.**

---

## 2. Files read (current SHA-256)

| Path | Bytes | Lines | SHA-256 |
|---|---:|---:|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | 12768 | 310 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | 2042 | 40 | `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | 1171 | 26 | `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` |
| `D:\Prop\src\Domain\Enums\DealAction.cs` | 622 | 28 | `6E87BFB536D43A57B48D548A0718E3C8C2E4914CE3CD0577410E6CB61D5054F1` |
| `D:\Prop\src\Domain\Enums\DealReason.cs` | 1149 | 47 | `3A4D92122D72155ACA3C0D9174758A966741A8B6E830917E6463CD905659E593` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | 1430 | 34 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 8143 | 187 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4535 | 92 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 12097 | 310 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 182 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 104 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | 4895 | 117 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` | 2402 | 54 | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` |

`git status --short` on the SUT: `M` reconstructor / result / `NormalizedDeal` / `DealIngestionService`; untracked store, dashboard queries, unit tests. Same bytes D72/D73 already measured.

Eval (scratch, not product): `D:\Prop\reports\swarm\20260818\_tmp_e024_first3\`  
Stdout (UTF-8 TSV, 61 lines, 9361 B): SHA-256 `26BABB7F25334D65255B302CB9B45D2AB655466AA25E90B6D680B74D4CA2EADE`.

Reflection (`META`): `hasDirty=False`, `hasFailure=False`, `hasSeq=False`, `hasEligible=True`, `hasFirst3Keys=False`, `entityDirty=False`, `entityEligible=False`, `entityFailure=False`, `earlyN=3`, `isTradingBuyCanceled=False`, `isTradingSellCanceled=False`.

---

## 3. How exclusion is implemented

```34:76:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var dirtyPositions = scoped
            .Where(d => d.Action is DealAction.BuyCanceled or DealAction.SellCanceled)
            .Select(d => d.PositionId)
            .ToHashSet();
        // …
            if (dirtyPositions.Contains(group.Key))
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
        // …
            .Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)
```

```26:28:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public bool IsTradingDeal =>
        Action is DealAction.Buy or DealAction.Sell
        && DealReasons.CountsAsTraderActivity(Reason);
```

`EligibleForFirstThree` defaults **true** (`ReconstructedTradeResult` L38). Only the cancel scan flips it. There is no `Dirty` bool.

Implications:

1. **13/14 are a third class.** They fail `IsTradingDeal`, so they never enter `ReconstructPosition`. They **are** collected into `dirtyPositions`. Balance is dropped and does **not** dirty.
2. **Dirty key is `position_id` for the whole group**, not A21’s current/last `lifecycle_seq`. Netting reuse of the same id + any cancel marks **every** row on that id ineligible (eval `NETTING`: two completes, `helperN=0`).
3. **Canceled-only tape** (`dirtyPositions` nonempty, `trading` empty): no group, no dirty stub (`F17B` `n=0`). A21 wants a persisted dirty stub.
4. **Scope is the reconstruct call**, which already filters `BrokerId` + `Login`. A cancel on another login or broker does not taint (`OTHER_LOGIN` / `OTHER_BROKER` `helperN=3`). A cancel on `position_id=0` does not taint 60/61/62 (`POS0` `helperN=3`).
5. **Reason on the cancel row is ignored** by the dirty scan. `BuyCanceled` + `CorporateAction` still dirties (`REASON` `helperN=0`).
6. **No inverse.** Extra-ticket `BuyCanceled` volume 0.20 next to a 0.10 IN leaves remaining 0.10 (`NO_INVERSE`). `DealCount` stays 1 (tickets `[1]`) — 13 never recorded as a fill.

This is Encoding B (surviving 0/1 **plus** a later 13/14, possibly a new ticket). Official Encoding A (same ticket rewritten 0→13) depends on ingest presenting Action=13.

---

## 4. Unit lock (measured)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter "FullyQualifiedName~Canceled_deal|FullyQualifiedName~First_three_completed|FullyQualifiedName~Ignores_balance|FullyQualifiedName~Rollover_is_not|FullyQualifiedName~Client_buy"
  --nologo

Passed!  Failed: 0, Passed: 5, Skipped: 0
```

```83:99:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    public void Canceled_deal_on_a_position_excludes_it_from_first_three()
    {
        // pos 10: Buy IN + Sell OUT + extra-ticket BuyCanceled
        // pos 20, 30: clean XAUUSDm round-trips
        _r.Reconstruct(...).Count(t => t.Completed).Should().Be(3);
        _r.CountCompletedXauUsdTrades(...).Should().Be(2);
        _r.IsEarlyScoreEligible(...).Should().BeFalse();
    }
```

Eval replica `UNIT` matches the fact bit-for-bit: `comp=3`, `helperN=2`, `helperElig=False`, pos 10 `elig=False`, tickets `[1,2]` (cancel ticket 3 never applied).

Not locked by that fact:

- `SellCanceled` (only `BuyCanceled` is constructed; eval `SELL_CXL` does exclude)
- Official in-place (latest row is 13, no surviving 0/1)
- `EligibleForFirstThree` asserted on the row itself
- A21 F17 tickets 961–964 / `RECON_CANCELED_DEAL`
- Scoring / dashboard / persist

`FirstThreeCompletedXauTradesTests` (A27 / A89) is still **absent**.

---

## 5. Measured eval (Domain Release, 2026-08-18)

Reports-only project referenced `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj`. Product source not touched.

| Id | Tape | helperN | helperElig | scoreN | scoreElig | state | First-3 helper? |
|---|---|---:|---|---:|---|---|---|
| `UNIT` | extra-ticket `BuyCanceled` on completed pos 10 + 2 clean | **2** | **false** | **3** | **true** | **SHADOW** | excluded |
| `SELL_CXL` | extra-ticket `SellCanceled` OUT on pos 10 + 2 clean | **2** | **false** | **3** | **true** | **SHADOW** | excluded |
| `M5` | 2 clean + 1 complete then `BuyCanceled` | **2** | **false** | **3** | **true** | **SHADOW** | excluded |
| `F17` | 961 BUY + 962 `BuyCanceled` on 5017; 5018 clean complete | **1** | false | 1 | false | INSUFFICIENT_DATA | 5017 out; 5018 in |
| `F17_FLAT` | F17 + later real OUT on 5017 | **0** | false | **1** | false | INSUFFICIENT_DATA | complete+dirty, helper 0 |
| `F17B` | official in-place: only `BuyCanceled` | 0 | false | 0 | false | INSUFFICIENT_DATA | empty (no stub) |
| `F17C` | surviving IN + canceled scale-in + OUT | **0** | false | 1 | false | INSUFFICIENT_DATA | remaining 0.10, not inverted |
| `F17D` | IN + `SellCanceled` OUT | 0 | false | 0 | false | INSUFFICIENT_DATA | stays **open**, dirty |
| `F17E` | canceled-only + balance clawback | 0 | false | 0 | false | INSUFFICIENT_DATA | no book |
| `F17F` | EURUSD cancel + clean XAU | **1** | false | 1 | false | INSUFFICIENT_DATA | non-XAU cancel does **not** dirty XAU |
| `F17G_BEFORE` | 3 clean | **3** | **true** | 3 | true | SHADOW | latch |
| `F17G_AFTER` | trade #3 close rewritten to `SellCanceled` | **2** | **false** | 2 | false | INSUFFICIENT_DATA | latch **retracts** in-memory |
| `NETTING` | complete, cancel, new lifecycle same pos | **0** | false | 2 | false | INSUFFICIENT_DATA | **both** seqs ineligible |
| `POS0` | cancel on pos 0 + 3 other XAU | **3** | true | 3 | true | SHADOW | isolated |
| `OTHER_LOGIN` | cancel login 2 / reconstruct login 1 | **3** | true | 3 | true | SHADOW | isolated |
| `OTHER_BROKER` | cancel `STARWAVE` / reconstruct `ACHIEVER` | **3** | true | 3 | true | SHADOW | isolated |
| `REASON` | `BuyCanceled` + `CorporateAction` | **0** | false | 1 | false | INSUFFICIENT_DATA | still dirty |
| `EMPTY_SYM` | XAU IN + empty-symbol cancel same pos | 0 | false | 0 | false | INSUFFICIENT_DATA | still dirty (key is pos, not symbol) |
| `NO_INVERSE` | 0.10 IN + 0.20 `BuyCanceled` | 0 | false | 0 | false | INSUFFICIENT_DATA | rem=0.10, not −0.10 |
| `DASH` | M5 tape, dashboard rule | highlight **3** (pos 3 `elig=False` still `first=True`) | leak |

`F17` matches the A21 first-3 *count* (only 5018). It does **not** emit `RECON_CANCELED_DEAL`, does **not** persist dirty, and 5017 remains an ordinary open row with `elig=false` only in memory.

---

## 6. Where the product still counts the canceled position

### 6.1 Scoring persist

```86:87:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
```

`BaselineScorer` re-filters `Completed && IsXauUsd` and sets `EarlyScoreEligible = CompletedXauTrades >= 3`. It never reads `EligibleForFirstThree`.

On `UNIT` / `M5` / `SELL_CXL`: helper says **not** early-score; production writes `CompletedXauTrades=3`, `CurrentState=SHADOW`.

`PersistDemoShadowAsync` is then called with that **same unfiltered** `completedXau` list. If state is `SHADOW`, every completed XAU book — including the cancel-tainted one — can become a `CopyIntent` / `ShadowOrder`. First-3 conservatism dies at the Application boundary.

### 6.2 Persist drops the flag

`ReplaceReconstructedAsync` copies `Completed`, volumes, PnL, scale/partial/avg flags. It does **not** copy `EligibleForFirstThree`. Entity census has no `Dirty` / `Eligible*` / `IsFirstThree` / `RemainingVolumeLots`. A store rebuild cannot honor first-3 dirty.

### 6.3 Dashboard highlight

```140:156:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var firstThree = 0;
        var highlights = trades.Select(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && firstThree < 3;
            if (first)
                firstThree++;
```

Eval `DASH`: pos 3 `elig=False` still `first=True`; `highlighted=3`.

React (`TraderDetailPage.tsx` L39) only prints `t.isFirstThree`. The footer text (“First 3 completed XAUUSD trades”) is correct as law and **wrong as implemented** once a cancel exists.

### 6.4 Official in-place cancel never reaches the helper

```85:90:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (exists)
            return false;
```

`UpsertDealAsync` is first-write-wins on `(BrokerId, DealTicket)`. A live `OnDealUpdate` that rewrites ticket T from `Buy` to `BuyCanceled` is dropped. `LoadDealsAsync` then feeds Action=0 into reconstruct: `IsTradingDeal` stays **true**, dirty scan never sees 13, first-3 **includes** the voided fill.

`LoadDealsAsync` also omits `Reason` (stays null). That does **not** resurrect 13/14; it only kills the reason allow-list on store rebuild (D44).

Until upsert is latest-revision, **production history can present a canceled position as a clean first-3 trade.**

---

## 7. A21 / A83 / C31 vs this tree

| Claim in older reports | This SHA |
|---|---|
| C31 C9: extra-ticket cancel → `count=3, eligible=true` HARD FAIL | **Stale.** Helper `count=2, eligible=false`. Score still 3/true. |
| A83 §0: “`IsTradingDeal` drops 13/14 silently; `TradeReconstructor` never dirties” | **Stale.** Separate dirty scan exists. Keep A83 for official in-place + never-invert. |
| D33: 5 facts / helper cannot construct 13/14 | **Stale.** Class is 6 facts; cancel is constructed. |
| D72 M5 / D73 unit fact | **Still true.** Same SHAs. E024 independently reproduced. |
| A21 F17 bit-for-bit | **No.** Helper exclusion yes; failure code, metric, persist dirty, canceled-only stub, `first3_keys` missing. Dirty scope is position-wide, not current seq. |

---

## 8. Done when (not done now)

- `RebuildTraderAsync` (and shadow) pass `Completed && IsXauUsd && EligibleForFirstThree` (or persisted dirty).
- `ReconstructedTrade` stores `EligibleForFirstThree` / `Dirty`; dashboard **reads** it (does not re-derive from completed XAU only).
- `UpsertDealAsync` keeps latest Action so official 0→13 is visible; cancel triggers full rebuild of `(broker, login)`.
- A21 F17 encoded: 962 → `RECON_CANCELED_DEAL`; 5017 dirty not first-3; 5018 counts; canceled-only stub; `SellCanceled` close stays open.
- Official in-place fixture (latest row is 13, no surviving 0/1) + extra-ticket fixture both green.
- `FirstThreeCompletedXauTradesTests` locks the matrix; C31 C9 / A83 §0 marked stale in those files or superseded.

Until then: **a canceled position is excluded from the first-3 helper and from the unit fact; it is not excluded from production first-3 identity, score latch, dashboard highlight, or shadow demo.**

**Product source was not modified.** This file is the only product-adjacent write from E024.
