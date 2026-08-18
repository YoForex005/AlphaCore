# D33 — `TradeReconstructionTests` coverage gaps

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D33_recon_tests.md` |
| Agent | D33 (recon unit-test coverage) |
| Date | 2026-08-18 |
| Assigned | Read `TradeReconstructionTests.cs`. Coverage gaps. Write this file. Do **not** modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| Subject | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| Law | Architecture v2 §§14–17, §35, §60; `D:\Prop\docs\trade-reconstruction.md`; A21 F01–F25; A89 §5.1 (22 recon classes) |
| Method | Re-read the class and helper in full. Mapped every assertion to SUT branches and A21 fixtures. Hashed files. Ran `dotnet test`. Listed sibling tests on disk. Nothing answered from memory. |

---

## 0. Verdict

**FAIL / INSUFFICIENT.** Five passing smokes. Not a reconstruction contract.

`TradeReconstructionTests` is **one** flat class, **five** `[Fact]`s, **~23** assertions. Scale-in, partial close, and averaging-down are fused into a single fact. Reverse and first-3 lock flags/counts only. **0 / 25** A21 fixtures are encoded bit-for-bit. **21 / 22** A89 reconstruction classes are **absent** from disk (A89’s “EXISTS” column is stale). A green run of this class does **not** prove §14 / §15 / §35 / §60.

| Metric | Measured |
|---|---|
| File | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` |
| Bytes / SHA-256 | **3939** / `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2` |
| `[Fact]`s | **5** |
| `[Theory]`s | **0** |
| Isolated scale-in / partial / reverse / first-3 classes | **0 / 4** |
| A21 F01–F25 encoded | **0 / 25** |
| A89 recon classes on disk | **1 / 22** (`TradeReconstructionTests` only) |
| `tests/Unit/Reconstruction/` | **absent** |
| This-class run | **5 passed / 5** (2026-08-18 13:37) |
| Adjacent `DealReasonTests` | **2 passed / 2** — **not** this class; does not close A21 F17 / F14 mixed |
| Sufficient for §60 reconstruction cluster? | **No** |

Do **not** claim “unit tests cover scale-in / partial / reverse / first-3.” They name those words. They do not lock the numbers or the exclusions.

C01 (sufficiency) and C31 (zero-volume / cancel / broker) remain valid on this SHA. This file is the **coverage-gap inventory** against the live SUT, A21 F01–F25, A89, and §14 field list.

---

## 1. What was actually read

### 1.1 Test class (entire)

`D:\Prop\tests\Unit\TradeReconstructionTests.cs` — 112 lines, namespace `TraderIntelligence.Tests.Unit` (not `…Unit.Reconstruction`).

| Lines | Fact | What it feeds | What it asserts |
|---:|---|---|---|
| 13–30 | `Reconstructs_simple_round_trip` | 1 long IN + 1 OUT, `position_id=10`, 0.10 / 0.10 @ 2320→2330, profit 100 | 1 trade; `Completed`; `IsXauUsd`; `Long`; `InitialVolumeLots=0.10`; `Net=100`; VWAP 2320 / 2330 |
| 33–49 | `Scale_in_and_partial_close` | 2 IN + 2 OUT on `position_id=20`; second IN worse (2290 < 2300) | **Fused:** `WasScaledIn`, `WasPartialClose`, `WasAveragedDown`, `Completed`, `MaxVolumeLots=0.20` |
| 51–67 | `Reverse_inout_closes_then_opens_opposite` | Long 0.10 + `InOut` sell 0.20 on `position_id=30` | Count 2; #1 complete Long; #2 open Short; leftover **0.10** only |
| 69–81 | `First_three_completed_xau_unlocks_early_score` | 3 independent XAUUSDm round-trips, positions 100/101/102 | `CountCompletedXauUsdTrades==3`; `IsEarlyScoreEligible==true` |
| 84–91 | `Ignores_balance_deals` | One `DealAction.Balance`, native 0, price 0 | `Reconstruct` empty |
| 93–111 | `Deal(...)` helper | Always `ACHIEVER` / login `1` / `XAUUSDm` / commission 0 / swap 0 / no SL/TP / no `Reason` / `OrderTicket=DealTicket` | — |

Constructor: `new TradeReconstructor(VolumeConverter.Manager)` — default `SymbolNormalizer`, scale **10_000**. Native `1000` → **0.10** lots. That conversion is implicit, not an Extended-scale or hundredths-integer test.

### 1.2 SUT + adjacent (read, not edited)

| Path | SHA-256 | Bytes | Role |
|---|---|---:|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD` | 12307 | Apply engine |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` | 1171 | Input; `IsTradingDeal` = Buy/Sell **and** `DealReasons.CountsAsTraderActivity` |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | `2248708669BC739A0EDE71318B764E7F34A07F906720FF36EE2762EB8FFABCDE` | 1980 | Output; **no** `Dirty`, **no** `lifecycle_seq`, **no** first-3 keys |
| `D:\Prop\src\Domain\Enums\DealReason.cs` | `3A4D92122D72155ACA3C0D9174758A966741A8B6E830917E6463CD905659E593` | 1149 | Reason allow-list |
| `D:\Prop\tests\Unit\DealReasonTests.cs` | `2B660B79B2D9BF812F637AF5200894FBA74536E7AB28439F8187CA956BAEB0E9` | 1333 | Adjacent 2 facts (Rollover skip + Client/Migration/null) |

`docs/trade-reconstruction.md` (11 lines) restates IN / OUT / INOUT / first-3 / Manager scale. It does not add fixtures.

### 1.3 Measured run (this review)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter "FullyQualifiedName~TradeReconstructionTests|FullyQualifiedName~DealReasonTests"
  --nologo
```

```text
Passed  DealReasonTests.Client_buy_still_counts
Passed  TradeReconstructionTests.Ignores_balance_deals
Passed  DealReasonTests.Rollover_is_not_a_trader_lifecycle_deal
Passed  TradeReconstructionTests.First_three_completed_xau_unlocks_early_score
Passed  TradeReconstructionTests.Scale_in_and_partial_close
Passed  TradeReconstructionTests.Reverse_inout_closes_then_opens_opposite
Passed  TradeReconstructionTests.Reconstructs_simple_round_trip

Total tests: 7
Passed: 7
```

Filter `~TradeReconstructionTests` alone is **5/5**. The extra 2 are **not** coverage of this class.

Sibling glob under `D:\Prop\tests\Unit`: only `TradeReconstructionTests.cs`. No `*Partial*`, `*Scale*`, `*Reversal*`, `*FirstThree*`, no `Reconstruction\` folder.

---

## 2. Assertion map (what is actually locked)

Dashes = unasserted. That is the gap.

| Field / rule | Simple | Scale+partial | Reverse | First-3 | Balance |
|---|---|---|---|---|---|
| Trade count | 1 | 1 | 2 | — | 0 |
| `Completed` | T | T | T then F | via count helpers | — |
| `Direction` | Long | — | Long then Short | — | — |
| `IsXauUsd` | T | — | — | implicit `XAUUSDm` | — |
| `CanonicalSymbol` / `SourceSymbol` | — | — | — | — | — |
| `BrokerId` / `Login` / `PositionId` | — | — | — | — | — |
| `Id` (`Broker:Login:Position:OpenedAtMs`) | — | — | — | — | — |
| `OpenedAt` / `ClosedAt` | — | — | — | — | — |
| `InitialVolumeLots` | 0.10 | — | — | — | — |
| `MaxVolumeLots` | — | 0.20 | — | — | — |
| `ClosedVolumeLots` | — | — | — | — | — |
| `RemainingVolumeLots` | — | — | 0.10 on #2 | — | — |
| `EntryVwap` / `ExitVwap` | 2320 / 2330 | — | — | — | — |
| `NetRealizedPnl` | 100 | — | — | — | — |
| Gross / commission / swap / fees | — | — | — | — | — |
| `WasScaledIn` | — | T | — | — | — |
| `WasPartialClose` | — | T | — | — | — |
| `WasAveragedDown` | — | T | — | — | — |
| False-flags on a clean full close | — | — | — | — | — |
| `DealCount` / `OrderCount` / tickets | — | — | — | — | — |
| `InitialSl/Tp` / `FinalSl/Tp` | — | — | — | — | — |
| `CountCompletedXauUsdTrades` | — | — | — | `== 3` | — |
| `IsEarlyScoreEligible` | — | — | — | `true` | — |
| Eligible **false** at N=0/1/2 | — | — | — | **NO** | — |
| N=4 latch / not `PROVEN_PROFITABLE` | — | — | — | **NO** | — |
| `CompletedXauUsdTrades` list / order / keys | — | — | — | **never called** | — |
| Non-XAU / open / partial excluded | — | — | — | **NO** | — |

`CompletedXauUsdTrades(...)` is a public SUT method. This class never invokes it. First-3 goes through the count/eligible wrappers only.

---

## 3. SUT branch coverage (engine vs this class)

### 3.1 Public surface

| Method | Called? | Locked? |
|---|---|---|
| `Reconstruct(broker, login, deals)` | Yes (4 facts) | Shape only |
| `CompletedXauUsdTrades` | **No** | Filter `Completed && IsXauUsd`, sort `(ClosedAt, OpenedAt)` untested |
| `CountCompletedXauUsdTrades` | Yes, once | Positive `== 3` only |
| `IsEarlyScoreEligible` | Yes, once | Positive `true` only (`>= 3`) |
| ctor `(volume, symbols)` custom | **No** | Extra mappings / Extended scale / injected normalizer never used |

### 3.2 Filter / sort / group (`Reconstruct` lines 29–44)

| Branch | Spec | This class |
|---|---|---|
| Drop `!IsTradingDeal` | A21 §6 | Balance-only empty. Not mixed. Not Credit/Commission/Bonus/Tax/Dividend/SO-comp matrix |
| `Reason` not trader activity | `DealReasons` + `NormalizedDeal` | **Never sets `Reason`.** Rollover is `DealReasonTests`, not here |
| Broker `OrdinalIgnoreCase` | §10 / F23 | Always `"ACHIEVER"`. No `STARWAVEFX`, no case fold, no mixed tape |
| Login filter | §10 | Always `1`. No other login |
| Sort `Time` then `DealTicket` | A21 §7.1 / F15 | Inputs already ordered. Shuffle / OUT-before-IN untested |
| `GroupBy(PositionId)` hedge vs netting | A21 §1.6 / F09 | First-3 uses 3 ids but never asserts 3 trades from `Reconstruct` |
| Emit open remainder `completed=false` | F13 / reverse leftover | Reverse leftover only. Open-only IN never counted |

### 3.3 `ReconstructPosition` apply

| Branch | Lines | This class |
|---|---|---|
| `lots <= 0` → `continue` | 77–78 | **No.** Balance uses action-filter, not zero-volume BUY |
| `DealEntry.In` → `ApplyIn` | 82–84 | Yes (open + same-side scale) |
| `DealEntry.Out` → `ApplyOut` | 85–94 | Yes (full + fused partial) |
| `DealEntry.OutBy` → same `ApplyOut` | 86 | **Never constructed** |
| `DealEntry.InOut` → `ApplyReverse` | 95–100 | Yes (leftover > 0) |
| `Out` while `open is null` → skip | 87–88 | **No** (F15 wrong-order / F18 cousin) |
| `ApplyOut` complete when `Remaining <= 1e-7` | 131–134 | Yes (simple + fused scale) |
| `ApplyOut` leave open (partial) | 137–138 | Only inside fused fact; no mid-book assert |
| Opposite `ENTRY_IN` → silent `ApplyReverse` | 122–125 | **No.** A21 F19 wants `RECON_IN_OPPOSITE_DIRECTION` |
| `ApplyReverse` on flat (`open is null`) | 150–152 | **No.** SUT **opens** a new book; spec F20/INOUT-flat fails |
| INOUT leftover `<= FlatEpsilon` | 160–161 | **No** (same-size INOUT) |
| INOUT leftover > 0 | 163 | Yes (shape only) |
| Trailing open appended | 104–105 | Reverse #2 only |

### 3.4 `OpenTrade` money / flags

| Branch | This class |
|---|---|
| `Start` maps `XAUUSDm` → `XAUUSD` | Implicit via `IsXauUsd` on simple fact only |
| `Start` unmapped symbol (`XAGUSD`) | **No** (F24) |
| `Start` extra-mapping override | **No** |
| `ScaleIn` long add **below** prior VWAP → `WasAveragedDown` | Yes (boolean only; no 2295 VWAP) |
| `ScaleIn` long add **above** prior VWAP → **not** avg-down | **No** (F02) |
| `ScaleIn` short add **above** VWAP → avg-down | **No** (F08) |
| `ScaleIn` short add **below** VWAP → not avg-down | **No** (F12 trade #2) |
| `CloseOut` clamp `Min(lots, Remaining)` | **No** (F18; SUT clamps, spec dirties) |
| `CloseOut` `closeLots <= 0` still `ApplyCommon` | **No** |
| Commission / swap accumulate | Helper hard-codes **0**. Never asserted |
| `Fees` hardcoded `0m` in `ToResult` | Never asserted |
| SL/TP initial + final | Helper omits both |
| `DealTickets` list / INOUT ticket on both sides | **No** |
| Distinct `OrderCount` (F10 same order two INs) | Helper sets `OrderTicket = DealTicket` always |

---

## 4. Helper cannot express (coverage ceiling)

```93:111:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    private static NormalizedDeal Deal(
        long ticket, long position, DealAction action, DealEntry entry, ulong volume, decimal price, decimal profit, int t) =>
        new()
        {
            BrokerId = "ACHIEVER",
            Login = 1,
            DealTicket = ticket,
            OrderTicket = ticket,
            PositionId = position,
            SourceSymbol = "XAUUSDm",
            ...
            Commission = 0,
            Swap = 0,
            Time = DateTimeOffset.UnixEpoch.AddMinutes(t)
        };
```

Until the helper grows (or facts stop using it), these cases **cannot** be written in this class:

| Parameter | Frozen value | Blocks |
|---|---|---|
| `BrokerId` | `"ACHIEVER"` | F23 mixed brokers; `STARWAVEFX`; case fold |
| `Login` | `1` | Cross-login isolation; F06 login 2001 |
| `SourceSymbol` | `"XAUUSDm"` | F06 EURUSD / GOLD / `XAUUSD.`; F24 XAG |
| `OrderTicket` | `= DealTicket` | F10 same-order partial fills (`order_count=2`) |
| `Commission` / `Swap` | `0` | F01 net 8.40; money split; `Fees` |
| `StopLoss` / `TakeProfit` | omitted | A89 #16 SL/TP propagation |
| `Reason` | omitted (`null` ⇒ counts) | F17 cancel; rollover-in-book; A82 matrix |
| `Comment` | omitted | unused by SUT; no gap |
| Volume native | 1000 or 2000 | Zero-volume BUY (F / C31 Z8); sub-lot; Extended scale |
| Entry | In / Out / InOut only | `OutBy` (F09) |
| Action | Buy / Sell / Balance | Credit, Commission*, Canceled 13/14, Dividend, Tax, SO-comp |
| Time | `UnixEpoch + t minutes`, already sorted | F15 out-of-order ingest |

---

## 5. Gap catalog (required proofs missing)

### 5.1 Architecture §60 reconstruction cluster

| # | §60 required | On-disk fact | Isolated? | Status |
|---:|---|---|---|---|
| 2 | trade reconstruction | `Reconstructs_simple_round_trip` + `Ignores_balance_deals` | Smoke | **PARTIAL** |
| 3 | partial close | bundled in `Scale_in_and_partial_close` | **No** | **PARTIAL** |
| 4 | scale-in | same fact | **No** | **PARTIAL** |
| 5 | full close | simple fact (no comm/swap/fees) | Collapsed | **PARTIAL** |
| 6 | position reversal | `Reverse_inout_…` count/dir/remaining | Smoke | **PARTIAL** |

§15 first-3 is **not** a §60 bullet by name; A21 + A89 treat it as P0. This class has a **positive latch only**.

### 5.2 A21 fixtures F01–F25

| ID | Topic | In `TradeReconstructionTests`? |
|---|---|---|
| F01 | Simple long; comm/swap; first-3=1; not eligible | Cousin only (different px/vol; no fees; no count=1) |
| F02 | Scale-in, **not** avg-down, VWAP 2403.33… | **Missing** |
| F03 | Partial-only; mid-book count 0; exit 2416 | **Missing** |
| F04 | INOUT leftover; money on A; ticket on both | Shape only; money/tickets **missing** |
| F05 | Reverse then close new side; 2 completed | **Missing** |
| F06 | First-3 + EURUSD skip + GOLD + 4th | **Missing** |
| F07 | Long avg-down VWAP 2390 / net −12.80 | Boolean only |
| F08 | Short avg-down | **Missing** |
| F09 | Hedge `OUT_BY` two books | **Missing** |
| F10 | Same-order partial fill = scale-in | **Missing** |
| F11 | Netting reuse of `position_id` (seq 2) | **Missing** |
| F12 | First-3 contains scale-in + reverse | **Missing** |
| F13 | Open-only count 0 | **Missing** |
| F14 | Balance/commission **mixed** with XAU | Balance-only empty; mixed **missing** |
| F15 | Out-of-order ingest sort | **Missing** |
| F16 | Duplicate ticket idempotent | **Missing** |
| F17 | Canceled dirties / excludes | **Missing** |
| F18 | OUT overclose dirty | **Missing** (SUT clamps) |
| F19 | Opposite IN dirty | **Missing** (SUT silent-reverses) |
| F20 | INOUT no new volume | **Missing** |
| F21 | `volume_closed_h` mismatch | **Missing** (field absent on `NormalizedDeal`) |
| F22 | `position_id == 0` | **Missing** |
| F23 | Multi-broker isolation | **Missing** |
| F24 | Unmapped XAG skip; `XAUUSD.a` counts | **Missing** |
| F25 | Minimum 3 identical XAU → eligible | Cousin of first-3 fact (no keys / no completing ticket) |

**0 / 25 bit-for-bit.** Replay-stability of F01–F12: **not** encoded. “No MFE/MAE fields”: `ReconstructedTradeResult` has none; **unasserted**.

### 5.3 A89 §5.1 reconstruction classes (22)

A89 marks these **EXISTS**. Disk check 2026-08-18: only `#1` exists, and it is the collapsed file, not the contract A89 describes.

| # | Class | Pri | On disk | Gap vs “must prove” |
|---:|---|---|---|---|
| 1 | `TradeReconstructionTests` | P0 | **This file** | Does **not** lock `Order ≠ Deal ≠ Position ≠ Logical Trade` identity; no multi-deal same-side **without** the fused extras |
| 2 | `PartialCloseReconstructionTests` | P0 | **absent** | Partial ≠ trade #2; remaining after first OUT |
| 3 | `ScaleInReconstructionTests` | P0 | **absent** | Isolated second IN; VWAP; max volume |
| 4 | `FullCloseReconstructionTests` | P0 | **absent** | `ClosedAt`; net = gross+comm+swap+fees |
| 5 | `PositionReversalReconstructionTests` | P0 | **absent** | Money on closed side; tickets; F05 |
| 6 | `OutByReconstructionTests` | P1 | **absent** | `DealEntry.OutBy` never constructed |
| 7 | `HedgeVsNettingReconstructionTests` | P0 | **absent** | Distinct `position_id`s ≠ one scale-in |
| 8 | `FirstThreeCompletedXauTradesTests` | P0 | **absent** | Exclusions (open/partial/non-XAU/balance/N<3) |
| 9 | `EarlyScoreEligibleLatchTests` | P0 | **absent** | N=0/1/2 false; N=4 still eligible; no `PROVEN_PROFITABLE` |
| 10 | `NonTradeDealExclusionTests` | P0 | **absent** | Full `DealAction` 2–20 matrix. `DealReasonTests` covers **Rollover** + Client/Migration only |
| 11 | `CanceledDealHandlingTests` | P1 | **absent** | 13/14 skip vs dirty |
| 12 | `ReconstructionSortDeterminismTests` | P0 | **absent** | Shuffle → same `Id` / VWAP / flags |
| 13 | `ReconstructionBrokerIsolationTests` | P0 | **absent** | ACH vs SWX same ticket |
| 14 | `ReconstructionLifecycleReuseTests` | P1 | **absent** | Flat then new IN = new `Id` (F11) |
| 15 | `ReconstructionVwapAndPnlTests` | P0 | **absent** | Decimal Σ(px×lots)/Σ(lots); signed comm/swap |
| 16 | `ReconstructionSlTpPropagationTests` | P2 | **absent** | Initial from first; final from last carrier |
| 17 | `AveragingDownFlagReconstructionTests` | P0 | **absent** | Long below / long above / short invert |
| 18 | `OpenLifecycleNotCompletedTests` | P0 | **absent** | `ClosedAt=null`; omitted from completed-XAU |
| 19 | `ReconstructionZeroAndBadVolumeTests` | P1 | **absent** | `lots<=0` skip; `price<=0` no crash |
| 20 | `NormalizedDealContractTests` | P1 | **absent** | `IsTradingDeal` × every `DealAction` |
| 21 | `ReconstructedTradeResultXauFlagTests` | P0 | **absent** | `IsXauUsd` iff canonical `XAUUSD`; leftover `GOLD` is **not** XAU |
| 22 | `ReconstructionScoringServiceRebuildTests` | P1 | **absent** | Load → reconstruct → persist → score; no FIX |

A89 #23–25 (ingest / demo tape) are also absent from Unit. Integration `SeedingAndStoreTests` only asserts `Any(completed XAU)` and login `10001` count 3 — **not** A21 rows.

### 5.4 §14 output fields never locked

Architecture §14 field list vs this class:

| §14 field | Asserted? |
|---|---|
| `id` | **No** |
| `broker_id` / `login` / `position_id` | **No** |
| `canonical_symbol` / `source_symbol` | **No** (only `IsXauUsd` on simple) |
| `direction` | Simple + reverse only |
| `opened_at` / `closed_at` | **No** |
| `entry_vwap` / `exit_vwap` | Simple only |
| `initial_volume` | Simple only |
| `max_volume` | Fused scale fact only |
| `closed_volume` | **No** |
| `gross_realized_pnl` / `commission` / `swap` / `fees` | **No** |
| `net_realized_pnl` | Simple only (`100`, all fees 0) |
| `deal_count` / `order_count` | **No** |
| `initial_sl/tp` / `final_sl/tp` | **No** |
| `was_scaled_in` / `was_partial_close` / `was_averaged_down` | True on fused fact; **never false** |
| `completed` | Yes on 3 facts |

`lifecycle_seq`, `dirty`, `deal_tickets[]`, `First3State` are **not on the result type**. Tests cannot lock A21 keys even if they tried.

### 5.5 §15 exclusion table (first-3 definition)

| Event | Counts? | Tested in this class? |
|---|---|---|
| Order / pending | No | **No** |
| IN still open (F13) | No | **No** |
| Partial remaining > 0 (F03) | No | **No** |
| SL / TP modify | No (not a deal) | **No** |
| Balance / credit / commission (F14) | No | Balance-only empty; **not** mixed |
| Non-XAUUSD (F06 EURUSD / F24 XAG) | No | **No** — helper hardcodes `XAUUSDm` |
| Still-open reverse leftover | No | Reverse fact does not call `Count*` |
| Dirty / canceled (F17–F22) | No | SUT has **no** dirty; facts never feed 13/14 |
| Completed XAU #1 / #2 | eligible **false** | **No** |
| Completed XAU #3 | eligible **true** | **Yes** |
| Completed XAU #4 | still eligible; not `PROVEN_PROFITABLE` | **No** |
| GOLD / `XAUUSD.` map and count | Yes | Only `XAUUSDm` |
| Cross-broker / cross-login | Isolated counters | **No** |

If the SUT counted each `OUT` as a trade, the first-3 fixture would still show 3 (one OUT per lifecycle). The dangerous §15 failure mode is **invisible**.

---

## 6. Adjacent tests do **not** close these gaps

| Class | Relation | What it does **not** do |
|---|---|---|
| `DealReasonTests` (2 facts) | Calls `Reconstruct` once on a **Rollover** BUY | Not a `DealAction` 2–20 matrix; not mixed book; not first-3 poison; not F17 cancel |
| `BaselineScorerTests` | Builds **hand-made** `ReconstructedTradeResult`s | Never calls `TradeReconstructor` |
| `SymbolNormalizerTests` | Alias / venue register | Not reconstruct-then-map; not F24 |
| `VolumeConverterTests` | Scale 10_000 / Extended | Not reconstructor binding; not zero-volume apply |
| `SeedingAndStoreTests` (Integration) | Demo seed → `Any(completed XAU)` + login 10001 count 3 | Synthetic demo tape, not A21 |

`ReconstructionScoringService.RebuildTraderAsync` (`DealIngestionService.cs`) is the persist/score orchestrator. **Zero** unit facts target it.

---

## 7. SUT defects this suite cannot see

Not a request to edit product source. These explain why **5/5 green is not a reconstruction PASS**.

1. **INOUT money double-applied.** `CloseOut` adds `deal.Profit`; `OpenTrade.Start` adds it again. On the reverse fixture, new-side `NetRealizedPnl` is **−10**; A21 F04 requires **0**. The fact never reads PnL.

```155:163:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var closeLots = open.RemainingLots;
        open.CloseOut(deal, closeLots);
        var closed = open.ToResult(completed: true);
        var leftover = dealLots - closeLots;
        if (leftover <= FlatEpsilon)
            return (closed, null);
        return (closed, OpenTrade.Start(brokerId, login, positionId, deal, leftover, newDirection, _symbols));
```

2. **Opposite `ENTRY_IN` silent reverse** (lines 122–125) vs A21 F19 dirty. Discarded `closed` (`_ = closed`) can drop a completed lifecycle.

3. **Decimal lots + `FlatEpsilon`** vs A21 integer `volume_h` / `remaining_h == 0`.

4. **No `lifecycle_seq`.** Netting reuse (F11) cannot key `(broker, login, position, seq)`. `Id` is `{BrokerId}:{Login}:{PositionId}:{OpenedAtUnixMs}`.

5. **No dirty / failure codes.** Zero-volume BUY, cancel 13/14, OUT-flat, OUT-overclose, INOUT-flat, missing `position_id`: skip / clamp / invent. First-3 still eligible (C31 Z8 / C9).

6. **No `volume_closed_h`** on `NormalizedDeal`. Infer-always-from-remaining; F21 untestable.

7. **`Reconstruct` keeps non-XAU books.** A21 §5 drops them before the book. `CompletedXauUsdTrades` filters after the fact.

8. **`Fees` always `0m`.** Commission/swap never non-zero in the helper.

9. **No first-3 cursor** (keys, `early_score_at`, completing ticket, events). Eligible is `count >= 3` recount, not a latch.

10. **`Mt5Deal` persist shape has no `Reason`.** Even if tests set `NormalizedDeal.Reason`, ingest cannot feed it from the store today.

---

## 8. What the five facts *do* lock (so they are not worthless)

Keep them as a compile/smoke net. Do not promote them to the A21 gate.

| Fact | Real lock |
|---|---|
| `Reconstructs_simple_round_trip` | One IN/OUT same `position_id` → one completed long; Manager `1000→0.10`; `XAUUSDm` maps `IsXauUsd`; single-price VWAP; profit passthrough as net when fees are 0 |
| `Scale_in_and_partial_close` | Same-side second IN sets `WasScaledIn` + `WasAveragedDown` (long add-lower); two OUTs set `WasPartialClose`; `MaxVolumeLots` rises to 0.20; still one trade when finally flat |
| `Reverse_inout_closes_then_opens_opposite` | `InOut` sell larger than remaining → 2 results, opposite directions, leftover 0.10 open |
| `First_three_completed_xau_unlocks_early_score` | Three clean completed XAU lifecycles flip `>= 3` to true |
| `Ignores_balance_deals` | A lone `Balance` row is not a book |

Regression a **flag-always-true** bug on `WasPartialClose` would still be green (simple fact never asserts false). Regression that **counts deals** instead of lifecycles can stay green on the first-3 fixture. Regression that **double-counts INOUT PnL** is green today.

---

## 9. Minimum facts that would close the gaps (do not implement here)

Split the fused fact. Assert the full row (volumes, VWAPs to 12 dp, money, flags, tickets) **and** first-3 count/eligible. Prefer A21 fixture IDs as names.

| Priority | Missing fact (suggested name) | Fixture / rule |
|---|---|---|
| P0 | `F02_scale_in_not_averaged_down_vwap` | Isolated scale-in; `WasAveragedDown=false`; entry 2403.333333333333 |
| P0 | `F03_partial_is_not_a_second_trade` | Mid-book `Completed=false`, count 0; then one trade, exit 2416 |
| P0 | `Simple_full_close_flags_are_false` | `WasScaledIn/Partial/AvgDown == false` |
| P0 | `F04_inout_money_stays_on_closed_side` | New-side net **0**; ticket 402 on both |
| P0 | `F05_reverse_then_close_new_side` | 2 completed; eligible false |
| P0 | `F06_first3_skips_eurusd_partial_and_fourth` | Keys 10/11/12; GOLD maps; #4 excluded |
| P0 | `Eligible_false_at_zero_one_two` | N=0/1/2 |
| P0 | `F08_short_average_down` | Short add above VWAP |
| P0 | `F11_position_id_reuse_is_new_lifecycle` | Two `Id`s / seq after flatten |
| P0 | `F13_open_only_does_not_count` | count 0 |
| P0 | `F15_sorts_out_before_in` | Presentation `[OUT,IN]` still completes |
| P0 | `F23_same_ticket_two_brokers_are_not_one_trade` | ACH vs SWX |
| P1 | `F09_out_by_two_hedge_books` | `DealEntry.OutBy` |
| P1 | `F10_same_order_two_ins_is_scale_in` | `OrderCount=2` |
| P1 | `F14_mixed_balance_does_not_fold_commission_deal` | Mixed tape |
| P1 | `F16_duplicate_ticket_idempotent` | deal_count 2 |
| P1 | `F17_canceled_does_not_invent_fill` | 13/14; document dirty gap |
| P1 | `F18_overclose_clamped_today` | Assert current clamp **and** that first-3 is still wrongly eligible |
| P1 | `Zero_volume_buy_skipped` | C31 Z8 |
| P1 | `F24_xagusd_not_xau` | Unmapped skip |
| P2 | SL/TP initial/final | A89 #16 |
| P2 | `IsXauUsd` false when canonical leftover is `GOLD` | A89 #21 |

Until those exist, A100 “A27 reconstruction fixtures pass” and A57 reconstruct increment stay **unchecked**.

---

## 10. Disposition

| Question | Answer |
|---|---|
| Did I read `TradeReconstructionTests.cs`? | **Yes** (entire file; SHA `5D99BA22…`) |
| Are there coverage gaps? | **Yes — the class is almost all gap** |
| A21 bit-for-bit? | **0 / 25** |
| §60 recon cluster locked? | **No** (5 bullets, all PARTIAL / fused) |
| A89 recon classes present? | **1 / 22** |
| 5/5 green? | **Yes — smoke only** |
| Product source changed? | **No** |
| Test source changed? | **No** |

**FAIL / INSUFFICIENT.** Keep the five facts as a compile smoke. Do not treat them as the reconstruction gate. Next increment is A21 F01–F25 as isolated xUnit rows, especially **F03** (partial ≠ trade), **F04** (money split — live double-count), **F06** (first-3 exclusions), and **F02** (scale-in VWAP without avg-down).

Prior reports that remain accurate on this SHA: `C01_recon_tests_review.md`, `C31_recon_adversarial.md`, `B11_recon_review.md`. This file is the D-band re-measure + gap list; it does not supersede C31’s measured zero-volume / cancel first-3 poison.
