# D73 — Does `IsTradingDeal` exclude canceled?

| Field | Value |
|---|---|
| Agent | D73 (senior engineer, canceled-deal predicate only) |
| Date | 2026-08-18 |
| Assigned | `IsTradingDeal` excludes canceled? Write this file. Do **not** modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D73_canceled.md` |
| Workspace | `D:\Prop` |
| Law | A21 §6 (`is_tradeable` / `is_canceled`); A83 (13/14 are not fills); A37 (`EnDealAction`); SDK `MT5APIDeal.h` |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No** |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `STALE_REPORT`.

---

## 0. Verdict

**Yes. `NormalizedDeal.IsTradingDeal` excludes canceled deals.**

The predicate is an exact match on `DealAction.Buy` (0) or `DealAction.Sell` (1), then a reason allow-list. `BuyCanceled` (13) and `SellCanceled` (14) fail the action clause, so the property is **false** for every canceled row, regardless of `Entry`, `Reason`, volume, or profit.

That is the **volume-book** gate. It is **not** the only canceled path:

| Path | Sees 13/14? | Effect |
|---|---|---|
| `NormalizedDeal.IsTradingDeal` | **No** (false) | Canceled rows never apply as IN/OUT/INOUT fills |
| `TradeReconstructor` dirty scan | **Yes** (`Action is BuyCanceled or SellCanceled`) | Same `position_id` lifecycles get `EligibleForFirstThree = false` |
| `CompletedXauUsdTrades` / `IsEarlyScoreEligible` | Indirect | Requires `Completed && IsXauUsd && EligibleForFirstThree` |
| `ReconstructionScoringService.RebuildTraderAsync` | **No** | Scores `Completed && IsXauUsd` only — **ignores** the first-3 flag |
| Persist `ReconstructedTrade` | **No** | Entity has no `EligibleForFirstThree` / `Dirty` |

So: canceled is excluded from **trading**, not from the **pipeline**. A83’s “silent drop before dirty” and C31’s C9 “count=3, eligible=true” are **stale** against the current SHA.

| Surface | Class |
|---|---|
| `IsTradingDeal` vs 13/14 | `EXISTS_AND_GOOD` (A21 `is_tradeable`) |
| Separate cancel dirty scan | `EXISTS_AND_GOOD` for extra-ticket first-3 (F17 cousin) |
| Official in-place mutation (same ticket 0→13) | `UNSAFE` if ingest is first-write-wins — cancel never reaches either path |
| `RECON_CANCELED_DEAL` / persist `Dirty` | `MISSING` |
| Scoring / store consume first-3 flag | `MISSING` |
| A83 §0 / §9, C31 C9, D33 “5 facts / no 13/14” | `STALE_REPORT` |

**One-line:** `IsTradingDeal` is false for canceled; the reconstructor still looks at 13/14 to exclude that `position_id` from first-3; scoring and persist do not.

---

## 1. What was read (no product edits)

| Path | Role | Bytes / lines / SHA-256 |
|---|---|---|
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | Predicate | **1171** / **29** / `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | Apply + dirty | **12768** / **347** / `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | `EligibleForFirstThree` default **true** | **2042** / **43** / `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` |
| `D:\Prop\src\Domain\Enums\DealAction.cs` | 13/14 mirror SDK | **622** / **29** / `6E87BFB536D43A57B48D548A0718E3C8C2E4914CE3CD0577410E6CB61D5054F1` |
| `D:\Prop\src\Domain\Enums\DealReason.cs` | Second conjunct | **1149** / **50** / `3A4D92122D72155ACA3C0D9174758A966741A8B6E830917E6463CD905659E593` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | Persist row; no eligibility | **1430** / **36** (no `Eligible*` / `Dirty`) |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Score filter | `RebuildTraderAsync` uses `Completed && IsXauUsd` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | First-write-wins + load | `UpsertDealAsync` returns false if ticket exists; `LoadDealsAsync` maps `Action`, leaves `Reason` null |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | Extra-ticket cancel fact | **4895** / **131** / `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| `D:\Prop\tests\Unit\DealReasonTests.cs` | `IsTradingDeal` false on Rollover Buy | **1333** / **44** / `2B660B79B2D9BF812F637AF5200894FBA74536E7AB28439F8187CA956BAEB0E9` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` | Official 13/14 | `DEAL_BUY_CANCELED=13`, `DEAL_SELL_CANCELED=14` |
| `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` | Binding F17 | SHA-256 `B150AC5069E93D615FE12AC5C8873E2ECFECD563E5F1E841D4C216A02F69C068` |
| `D:\Prop\reports\swarm\20260818\A83_canceled_deals.md` | Binding cancel law | SHA-256 `D59D986E30F1D7058EFF03B2FB4AE9558F558E05A93E655D5DA5B6F478F2127E` — **tree snapshot stale** |

Grep of `IsTradingDeal` under `D:\Prop\src`: **2** hits (`NormalizedDeal` definition, `TradeReconstructor` filter). No other predicate.

Grep of `BuyCanceled` / `SellCanceled` under `D:\Prop\src`: enum + reconstructor dirty scan only.

---

## 2. The predicate (measured)

```26:28:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public bool IsTradingDeal =>
        Action is DealAction.Buy or DealAction.Sell
        && DealReasons.CountsAsTraderActivity(Reason);
```

C# `is` pattern is **or of two exact enum members**. There is no range, no “market-like”, no `>= Buy && <= Sell`. Canceled is not a subtype of Buy/Sell.

```21:22:D:\Prop\src\Domain\Enums\DealAction.cs
    BuyCanceled = 13,
    SellCanceled = 14,
```

SDK (`IMTDeal::EnDealAction`): `DEAL_BUY_CANCELED = 13` “canceled buy deal”; `DEAL_SELL_CANCELED = 14` “canceled sell deal”. C# values match.

`DealReasons.CountsAsTraderActivity`:

- `reason is null` → **true** (reload from store always looks like trader activity; D44).
- Allow: Client, Expert, Dealer, SL, TP, StopOut, ExternalClient, Gateway, Signal, Mobile, Web.
- Deny: Rollover, VariationMargin, Settlement, Transfer, Sync, ExternalService, Migration, Split, CorporateAction.

The reason conjunct is **unreachable** for 13/14: `false && _` is still false.

A83 quotes the **old** one-line form `Action is Buy or Sell` without reason. Current SHA has **both** conjuncts. The canceled answer does not change.

---

## 3. Action truth table (`Reason = null`)

`IsTradingDeal` for every `DealAction` on the enum, reason omitted (store-shaped):

| Action | Value | `IsTradingDeal` |
|---|---:|---|
| `Buy` | 0 | **true** |
| `Sell` | 1 | **true** |
| `Balance` | 2 | false |
| `Credit` | 3 | false |
| `Charge` | 4 | false |
| `Correction` | 5 | false |
| `Bonus` | 6 | false |
| `Commission` / Daily / Monthly | 7–9 | false |
| `AgentDaily` / `AgentMonthly` | 10–11 | false |
| `InterestRate` | 12 | false |
| **`BuyCanceled`** | **13** | **false** |
| **`SellCanceled`** | **14** | **false** |
| `Dividend` / `DividendFranked` | 15–16 | false |
| `Tax` | 17 | false |
| `Agent` | 18 | false |
| `StopOutCompensation` / `…Credit` | 19–20 | false |

Buy/Sell + `Reason=Rollover` → false (`DealReasonTests.Rollover_is_not_a_trader_lifecycle_deal`). That is **not** a cancel.

What this is **not**:

| Lookalike | On `NormalizedDeal`? | `IsTradingDeal`? |
|---|---|---|
| `ExecutionOrderStatus.Cancelled` | No | n/a (FIX/order FSM) |
| FIX `ExecType=4` / `OrdStatus=4` | No | n/a (`FixSimulationHarness`) |
| `IMTOrder::ORDER_STATE_CANCELED` | No | n/a (no deal) |
| `DealReason` “canceled” | **No such member** | — |

Canceled, for this predicate, means **deal action 13/14 only**.

---

## 4. How the reconstructor uses both paths

```34:66:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var dirtyPositions = scoped
            .Where(d => d.Action is DealAction.BuyCanceled or DealAction.SellCanceled)
            .Select(d => d.PositionId)
            .ToHashSet();

        var trading = scoped
            .Where(d => d.IsTradingDeal)
            .OrderBy(d => d.Time)
            .ThenBy(d => d.DealTicket)
            .ToList();
        // …
            if (dirtyPositions.Contains(group.Key))
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
        // …
            .Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)
```

Implications:

1. **Volume book never applies 13/14.** Matches A21 `is_tradeable` and A83 “never invert / never apply volume.” Entry on a canceled row (`IN`/`OUT`) is ignored because the row never reaches `ReconstructPosition`.
2. **Canceled is a third class in the engine**, not silent balance-like. Balance is dropped by `IsTradingDeal` and is **not** in `dirtyPositions`.
3. **Dirty key is `position_id` for the whole group**, not A21’s current/last `lifecycle_seq`. Netting reuse of the same id + any cancel on that id marks **every** reconstructed row on that id ineligible.
4. **Canceled-only tape** (`dirtyPositions` nonempty, `trading` empty for that id): no group is emitted. No dirty stub. `RECON_CANCELED_DEAL` is not a type in Domain.
5. **Default eligibility is true.** Only the cancel scan flips it. There is no `Dirty` bool on `ReconstructedTradeResult` (name is `EligibleForFirstThree`).

This is Encoding B (extra ticket: original BUY/SELL **and** a later 13/14, possibly a new ticket). Official Encoding A (same ticket rewritten 0→13) depends on ingest seeing the new action.

---

## 5. Tests (measured this review)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter "FullyQualifiedName~Canceled_deal|FullyQualifiedName~Ignores_balance|FullyQualifiedName~Rollover_is_not|FullyQualifiedName~Client_buy"
  --nologo
```

```text
Passed!  Failed: 0, Passed: 4, Skipped: 0
```

| Fact | What it locks about canceled |
|---|---|
| `Canceled_deal_on_a_position_excludes_it_from_first_three` | Extra-ticket `BuyCanceled` on position 10 + two clean XAU round-trips → `Reconstruct` completed **3**, `CountCompletedXauUsdTrades` **2**, `IsEarlyScoreEligible` **false** |
| `Ignores_balance_deals` | Balance is not a book (same action-filter family, **not** 13/14) |
| `Rollover_is_not_a_trader_lifecycle_deal` | `IsTradingDeal` false via **reason**, on a **Buy** |
| `Client_buy_still_counts` | Reason allow-list only |

There is **no** isolated `IsTradingDeal` matrix for 13/14 (`NormalizedDealContractTests` / A89 #20 still absent). The cancel fact never reads `deal.IsTradingDeal` on ticket 3; it proves the **pipeline** (skip fill + dirty first-3). From the predicate, ticket 3 is not in `trading` and **is** in `dirtyPositions`.

Not locked:

- `SellCanceled` (only `BuyCanceled` is constructed)
- Official in-place (latest row is 13, no surviving 0/1)
- Cancel of a close (`SELL_CANCELED` + `ENTRY_OUT`) leaving the book open
- Non-XAU cancel (helper hard-codes `XAUUSDm`)
- `EligibleForFirstThree` on the reconstructed row itself (fact uses count helpers)
- A21 F17 tickets 961–964 / `RECON_CANCELED_DEAL`

---

## 6. A21 / A83 vs this tree

A21 §6:

```
is_tradeable := action ∈ {0, 1}
is_canceled  := action ∈ {13, 14}
is_balance_like := not tradeable and not canceled
```

`IsTradingDeal` **is** `is_tradeable` ∧ reason-allow. It is **correct** that canceled is not tradeable.

A21 step 4 still requires: see the cancel, emit `RECON_CANCELED_DEAL`, dirty, do not invert. Current engine: see cancel (separate scan), dirty first-3 flag, do not invert. Missing: failure code, metric, persist dirty, canceled-only stub.

A83 §0 / §9 claimed (at write time):

- “`IsTradingDeal` drops 13/14 silently; `TradeReconstructor` never dirties”
- “tests: no F17 / no cancel cases”
- quoted predicate without `DealReasons`

**Those sentences are false on SHA `AEA3930B…` / test SHA `CB223DDE…`.** Keep A83 for official in-place semantics, ingest rebuild, and “never invert.” Do not quote A83 as the live product map.

C31 C9 (`count=3, eligible=true` HARD FAIL) is the same staleness: the unit fact now expects **2** and **false**.

D33 “5 facts / helper cannot construct 13/14” is stale: the class is **6** facts; cancel is constructed.

---

## 7. Where exclusion can fail without changing the predicate

`IsTradingDeal` can only exclude a cancel it is shown.

| Hole | Evidence | Result |
|---|---|---|
| First-write-wins ingest | `UpsertDealAsync`: if `(BrokerId, DealTicket)` exists → return false, **do not update `Action`** | Official 0→13 mutation stays `Buy`. `IsTradingDeal` stays **true**. Dirty scan never sees 13. |
| `LoadDealsAsync` drops `Reason` | Mapped fields omit `Reason` (stays null) | Reason filter dead on store rebuild (D44). **Does not** resurrect 13/14. |
| Scoring ignores first-3 flag | `RebuildTraderAsync`: `trades.Where(t => t.Completed && t.IsXauUsd)` | Extra-ticket cancel still **scores** the tainted complete |
| Persist drops the flag | `ReplaceReconstructedAsync` copies no `EligibleForFirstThree` | Dashboard/reload cannot honor first-3 dirty |
| Canceled-only position | No trading group | Empty reconstruct; no stub / no failure code |

The predicate itself is not the ingest hole. Asking “does `IsTradingDeal` exclude canceled?” on a **normalized row that already has Action=13/14** is yes. Asking whether **production history** always presents that row is **no** until upsert is latest-revision.

---

## 8. Disposition

| Question | Answer |
|---|---|
| Does `IsTradingDeal` exclude canceled? | **Yes** — false for `BuyCanceled` / `SellCanceled` |
| Does it exclude them as fills? | **Yes** — they never enter `trading` |
| Does the pipeline drop them entirely? | **No** — dirty scan uses `Action` 13/14 directly |
| First-3 on extra-ticket cancel? | **Excluded** in `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` (unit fact 4/4 green) |
| Official same-ticket cancel? | **Not proven**; first-write-wins can hide it |
| A21 F17 bit-for-bit? | **No** |
| Product source changed? | **No** |

**Yes, `IsTradingDeal` excludes canceled.** Treat A83/C31/D33 product snapshots as stale on this SHA. Do not weaken the predicate to include 13/14; do not assume scoring or persist honor the dirty flag.

**Product source was not modified.** This file is the only write from D73.
