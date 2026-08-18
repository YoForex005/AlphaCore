# C52 — Expected unit tests after avg-down polarity fix

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C52_expected_tests.md` |
| Agent | C52 (expected-tests review only) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Test source edited | **No** |
| `dotnet test` this pass | **Not run** (assigned: review only) |
| Question | If Unit was **28/29** before the avg-down fix, what is the expected count after? |
| Answer | **29 passed / 0 failed / 0 skipped** on the **B08 closed set of 29** executed cases |
| Law | Architecture v2 §60 averaging-down; A21 §7.4 / F07; B08 measured 28/29; current `OpenTrade.ScaleIn` |

This file is a **static prediction**, not a measured `dotnet test` log. Do not cite it as a green CI run.

---

## 0. Verdict

**Expect 29/29 on the B08-era Unit census after the avg-down polarity fix.**

| Frame | Total | Expected passed | Expected failed | Expected skipped | Exit |
|---|---:|---:|---:|---:|---:|
| B08 Unit, **before** polarity fix (measured by B08) | **29** | **28** | **1** | 0 | 1 |
| Same 29 cases, **after** polarity fix (this review) | **29** | **29** | **0** | 0 | **0** |
| `tests/Integration` (unchanged by this fix) | **3** | **3** | 0 | 0 | 0 |

The single red case in the 29 was `TradeReconstructionTests.Scale_in_and_partial_close` asserting `WasAveragedDown == true` on a long add **below** prior VWAP. B08 recorded the SUT comparison inverted (`deal.Price > EntryVwap` for long). The working-tree SUT now uses A21 polarity (`deal.Price < EntryVwap` for long). That is the only assertion in the 29 that the fix can flip, and it flips **false → true**, which is what the fact already expects.

**29/29 is not Architecture §60 coverage.** It is “the one inverted-predicate fact no longer disagrees with the fixture.”

**29/29 is not the current `TraderIntelligence.Tests.Unit.csproj` total.** Extra files landed after B08 (`Normalization/`, `Sizing/`). A blind `dotnet test` on that csproj will **not** print `Total tests: 29`. See §8.

---

## 1. Method (review only)

Read, do not execute:

| Source | Path | Role |
|---|---|---|
| Measured 28/29 | `D:\Prop\reports\swarm\20260818\B08_tests_gap.md` §0, §3.1–3.2 | Authoritative pre-fix census |
| Current SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` `OpenTrade.ScaleIn` | Post-fix polarity |
| Spec | A21 §7.4 lines 468–469; `docs/trade-reconstruction.md` | LONG add below prior VWAP |
| The red fact | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` lines 33–49 | Only `WasAveragedDown` consumer in the 29 |
| Other 28 facts | 6 remaining Unit files that existed at B08 | Confirm they do not assert avg-down polarity |
| Later reviews | C01 (recon 5/5), B11 (predicate PASS), C17 (B08 FAIL stale) | Supporting, not this agent’s run |

Assigned constraint: **do not run tests. Do not modify product source.**

---

## 2. Before: B08 measured 28/29

B08 (`2026-08-18T13:19:49+05:30`):

```text
Failed  TradeReconstructionTests.Scale_in_and_partial_close
  Expected trade.WasAveragedDown to be true, but found False.
  at TradeReconstructionTests.cs:46

Total tests: 29
     Passed: 28
     Failed: 1
```

Project at that snapshot: 7 source files under `D:\Prop\tests\Unit` (no `Normalization/`, no `Sizing/`).

| Class | Executed | B08 result |
|---|---:|---|
| `SmokeTests` | 1 | 1 pass |
| `TradeReconstructionTests` | 5 | **4 pass, 1 fail** |
| `BaselineScorerTests` | 3 | 3 pass |
| `RiskEngineTests` | 5 | 5 pass |
| `ExecutionAndSizingTests` | 6 | 6 pass |
| `SymbolNormalizerTests` | 6 (5 theory + 1 fact) | 6 pass |
| `VolumeConverterTests` | 3 | 3 pass |
| **Total** | **29** | **28 / 1 / 0** |

Arithmetic check: `1+5+3+5+6+6+3 = 29`.

---

## 3. The one red fact (test was right)

```33:49:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    public void Scale_in_and_partial_close()
    {
        var deals = new[]
        {
            Deal(1, 20, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 1),
            Deal(2, 20, DealAction.Buy, DealEntry.In, 1000, 2290m, 0, t: 2),
            Deal(3, 20, DealAction.Sell, DealEntry.Out, 1000, 2310m, 20, t: 3),
            Deal(4, 20, DealAction.Sell, DealEntry.Out, 1000, 2320m, 40, t: 4)
        };

        var trade = _r.Reconstruct("ACHIEVER", 1, deals).Should().ContainSingle().Subject;
        trade.WasScaledIn.Should().BeTrue();
        trade.WasPartialClose.Should().BeTrue();
        trade.WasAveragedDown.Should().BeTrue();
        trade.Completed.Should().BeTrue();
        trade.MaxVolumeLots.Should().Be(0.20m);
    }
```

Helper trap: second `Deal(...)` argument is **`PositionId`**, not volume. All four rows use `VolumeNative = 1000` → `0.10` lot under `VolumeConverter.Manager` (scale 10_000). Same `PositionId = 20`.

| Step | Deal | Effect |
|---|---|---|
| 1 | BUY IN 0.10 @ 2300 | `OpenTrade.Start`; `EntryVwap = 2300` |
| 2 | BUY IN 0.10 @ 2290 | same-side `ScaleIn`; long add **below** prior VWAP |
| 3 | SELL OUT 0.10 @ 2310 | `closeLots < remaining` → `WasPartialClose` |
| 4 | SELL OUT 0.10 @ 2320 | remaining ≤ ε → `Completed` |

A21 §7.4 (compare **before** updating VWAP):

```text
if t.direction == LONG  and deal.price < vwap_before: book.seen_avg_down = true
if t.direction == SHORT and deal.price > vwap_before: book.seen_avg_down = true
```

`2290 < 2300` on a long is avg-down. The fact’s `BeTrue()` is the spec. B08’s SUT had the inequality flipped, so the fact was a **true product defect**, not a bad expected value.

B08 SUT (quoted there; no longer on disk):

```csharp
var worse = Direction == TradeDirection.Long
    ? deal.Price > EntryVwap   // inverted
    : deal.Price < EntryVwap;
```

`2290 > 2300` is false → `WasAveragedDown` stayed false → line 46 failed. Assertions after that line were unobserved on the red run (`WasScaledIn`, `WasPartialClose`, `Completed`, `MaxVolumeLots == 0.20`).

---

## 4. After: current `ScaleIn` matches A21

Working tree (`TradeReconstructor.OpenTrade.ScaleIn`):

```235:250:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public void ScaleIn(NormalizedDeal deal, decimal lots)
        {
            // Averaging down: add to a long after price fell, or to a short after price rose.
            var worse = Direction == TradeDirection.Long
                ? deal.Price < EntryVwap
                : deal.Price > EntryVwap;
            if (worse)
                WasAveragedDown = true;

            WasScaledIn = true;
            RemainingLots += lots;
            if (RemainingLots > MaxVolumeLots)
                MaxVolumeLots = RemainingLots;
            _entryNotional += deal.Price * lots;
            _entryLots += lots;
            ApplyCommon(deal);
        }
```

| Check | Result |
|---|---|
| Compare uses **prior** `EntryVwap` (notional updated after the flag) | Yes |
| Long: worse iff `price < vwap` | Yes (A21 F07) |
| Short: worse iff `price > vwap` | Yes (A21 F08) — **untested** by the 29 |
| Equal price is not avg-down | Yes — **untested** by the 29 |
| Flag is sticky (or-assign) | Yes |
| `WasScaledIn` always set on same-side IN | Yes |

Static walk of the fixture against this SUT:

| Assertion | Expected | Why |
|---|---|---|
| single trade | 1 | one `position_id`, flat at end |
| `WasScaledIn` | true | second `ENTRY_IN` same side |
| `WasPartialClose` | true | first OUT 0.10 of 0.20 remaining |
| `WasAveragedDown` | **true** | `2290 < 2300` on long, **now** sets the flag |
| `Completed` | true | second OUT flattens |
| `MaxVolumeLots` | 0.20 | remaining peaked at 0.20 |

**This fact is expected PASS.** That is the 28 → 29 transition.

Unasserted values the same fixture already produces (do **not** count as extra passing tests):

```text
InitialVolumeLots   = 0.10
ClosedVolumeLots    = 0.20
RemainingVolumeLots = 0
EntryVwap           = (2300*0.10 + 2290*0.10) / 0.20 = 2295
ExitVwap            = (2310*0.10 + 2320*0.10) / 0.20 = 2315
NetRealizedPnl      = 20 + 40 = 60
Direction           = Long
```

---

## 5. Expected roster (the closed set of 29)

All rows are **expected**, not measured by C52.

### 5.1 `SmokeTests` (1) — still pass

| # | Case | Expected | Why the avg-down fix cannot break it |
|---:|---|---|---|
| 1 | `Domain_assembly_loads` | PASS | Loads `VolumeConverter` assembly only |

### 5.2 `TradeReconstructionTests` (5) — 4 unchanged + 1 flip to pass

| # | Case | B08 | After fix | Why |
|---:|---|---|---|---|
| 2 | `Reconstructs_simple_round_trip` | PASS | PASS | One IN/OUT; never calls `ScaleIn` |
| 3 | `Scale_in_and_partial_close` | **FAIL** | **PASS** | Polarity now matches fixture (§3–4) |
| 4 | `Reverse_inout_closes_then_opens_opposite` | PASS | PASS | `ApplyReverse`, not same-side `ScaleIn`; does not read `WasAveragedDown` |
| 5 | `First_three_completed_xau_unlocks_early_score` | PASS | PASS | Count/`IsEarlyScoreEligible` only |
| 6 | `Ignores_balance_deals` | PASS | PASS | `DealAction.Balance` filtered; empty list |

### 5.3 `BaselineScorerTests` (3) — still pass

Handmade `ReconstructedTradeResult`s hard-code `WasAveragedDown = false`. Scorer never calls `TradeReconstructor`.

| # | Case | Expected | Locked fields |
|---:|---|---|---|
| 7 | `Two_trades_remain_insufficient` | PASS | N=2 → `INSUFFICIENT_DATA`, `EarlyScoreEligible=false` |
| 8 | `Three_disciplined_winners_go_to_shadow_not_live` | PASS | N=3 winners + SL → `SHADOW`; `CanPromoteToLive==false` |
| 9 | `Martingale_after_losses_is_risk_blocked` | PASS | 0.10→0.20→0.40 after losses → `Martingale` + `RISK_BLOCKED` |

`FromBaseline` still has no `LIVE` path (`CanPromoteToLive` is unconditionally false). Unchanged by ScaleIn.

### 5.4 `RiskEngineTests` (5) — still pass

`RiskEngine.Evaluate` has no `WasAveragedDown` input.

| # | Case | Expected | Reason / outcome locked |
|---:|---|---|---|
| 10 | `Stale_quote_rejects_open` | PASS | `QUOTE_STALE` / `Reject` / no FIX |
| 11 | `Real_flag_false_never_allows_fix_send` | PASS | `Approve` + `AllowFixSend=false` |
| 12 | `Stop_new_execution_blocks_opens_not_closes` | PASS | open `GlobalStop`; close `Approve` (flag still off) |
| 13 | `Unreconciled_venue_blocks_new_exposure` | PASS | `VENUE_NOT_RECONCILED` |
| 14 | `Stale_signal_rejected` | PASS | `SIGNAL_STALE` (5 min > 15 s limit) |

### 5.5 `ExecutionAndSizingTests` (6) — still pass

| # | Case | Expected | Static check vs current SUT |
|---:|---|---|---|
| 15 | `Unknown_ack_cannot_retry_new_order` | PASS | `AfterSendAttempt` → unknown; `MayRetry=false`; `RequiresReconciliation=true` |
| 16 | `Disconnect_after_send_is_unknown_state` | PASS | `AfterDisconnectWithUnknownAck` → `ExecutionStateUnknown` |
| 17 | `Filled_report_is_terminal` | PASS | OrdStatus `"2"` / ExecType `"FILL"` → `Filled` |
| 18 | `Quantity_normalizer_steps_and_min` | PASS | `0.10×1=0.10`; `0.10×0.05=0.005→0`; `0.333→0.33` |
| 19 | `ClOrdId_is_deterministic_and_unique_per_sequence` | PASS | prefix `TI20260818120000`; seq 0 ≠ seq 1 |
| 20 | `Copy_intent_expires` | PASS | 16 s > 15 s true; 5 s false |

### 5.6 `SymbolNormalizerTests` (6) — still pass

| # | Case | Expected |
|---:|---|---|
| 21 | `Maps_known_aliases_to_XAUUSD("XAUUSD")` | PASS |
| 22 | `Maps_known_aliases_to_XAUUSD("XAUUSD.")` | PASS |
| 23 | `Maps_known_aliases_to_XAUUSD("XAUUSDm")` | PASS |
| 24 | `Maps_known_aliases_to_XAUUSD("XAUUSD.a")` | PASS |
| 25 | `Maps_known_aliases_to_XAUUSD("GOLD")` | PASS |
| 26 | `Does_not_guess_venue_instrument_ids` | PASS (false until `RegisterVenueInstrument`) |

All five aliases are in `DefaultXauAliases` (case-insensitive; `XAUUSDm` → `XAUUSDM`). Reconstruction’s `XAUUSDm` fixture is independent of avg-down.

### 5.7 `VolumeConverterTests` (3) — still pass

| # | Case | Expected |
|---:|---|---|
| 27 | `Manager_scale_maps_0_10_lots_to_1000_native` | PASS (`Scale=10000`, `ToNative(0.10)=1000`) |
| 28 | `Extended_scale_maps_one_lot_to_100_million` | PASS |
| 29 | `Hundredths_comment_is_not_the_default` | PASS (`Manager.Scale ≠ 100`) |

---

## 6. Why nothing else in the 29 goes red

The polarity change is confined to `OpenTrade.ScaleIn`’s `worse` boolean.

| Could it fail a different fact? | No, because |
|---|---|
| Simple round-trip / first-3 / balance | Never scale-in |
| Reverse InOut | Opposite-side remainder via `ApplyReverse` / `Start`; no same-side `ScaleIn` |
| Scorer | Hand-built trades; `WasAveragedDown=false` constant |
| Risk / FSM / ClOrdID / expiry / qty / symbols / volume | No call into `TradeReconstructor` |
| A later fact that required “long add below VWAP is **not** avg-down” | **Does not exist** in the 29 |

No `[Fact(Skip=…)]` exists in the B08 seven files. Expected skipped for this frame: **0**.

Compile coupling: Unit still references `Fix.CTrader`. B08 noted a prior `FixMessageParser` `char`/`string` break. C52 assumes the tree still **builds** (B11/C01/C17 already compiled it). A compile error would prevent 29/29; that is outside the avg-down predicate.

---

## 7. What 29/29 does **not** mean

| Claim | Reality after 29/29 |
|---|---|
| Architecture §60 unit (17 areas) | Still **0 COVERED**. Averaging-down stays **PARTIAL** (one long add-in-loss boolean). |
| A21 F07 (long avg-down **numbers**) | Only the boolean is locked. VWAP 2295 / net 60 unasserted. |
| A21 F08 (short add **above** VWAP) | **No fact** in the 29. Short branch is compiled, not tested. |
| A21 F02 (scale-in that is **not** avg-down) | **No fact**. A regression that sets `WasAveragedDown=true` on every scale-in still passes this suite. |
| Add-in-profit negative | **No fact**. |
| `FeatureSnapshot.AveragingDown` | Scorer tests force the flag false; they do not prove `Any(WasAveragedDown)`. |
| Partial / scale-in / full-close as isolated §60 areas | Still one fused smoke. |
| Integration §60 (8) | Still 0/8. Seed tests do not assert `WasAveragedDown`. |
| Go-live / first-useful | Unchanged (A100 / A57). |

C01 already classified these five reconstruction facts as **insufficient** even when 5/5 green. C52 agrees. Green here is “predicate no longer inverted,” not “reconstruction contract locked.”

---

## 8. Do not confuse 29/29 with the current Unit project

B08’s 29 is a **closed set**. The working tree now also contains:

| Extra file (not in the 29) | Role |
|---|---|
| `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | 4 passing passthrough facts + many `[Fact(Skip=…)]` / skipped theory (A43 converter missing) |
| `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | Last-stage min/step matrix + 1 skipped A43 E23 fact |

A later agent (C17) measured the **expanded** project at **83** executed cases (`60` pass / `1` fail / `22` skip). That fail was `Allocation_scales_before_step` expecting `0.10` for `0.10×0.10` (test arithmetic, not avg-down). The working-tree copy of that fact now expects `0.01m`. C52 **did not run** the expanded suite and **does not** predict 83/83 or 60/61.

| If you run… | Do not expect |
|---|---|
| `dotnet test tests\Unit\TraderIntelligence.Tests.Unit.csproj` (whole project, today) | `Total tests: 29` |
| `--filter FullyQualifiedName~TradeReconstructionTests` | 29/29 (that filter is **5** tests; expect 5/5) |
| The B08 seven files only (or an equivalent filter list in §10) | **29/29** after the polarity fix |

Integration stays **3** (`PlaceholderRemoved` + 2 seed/upsert). Avg-down polarity does not change those assertions.

---

## 9. Prior measured support (not this agent)

These are other agents’ runs, cited so a later runner knows the flip already happened on the recon class:

| Review | What they ran | Result | Bearing on 29/29 |
|---|---|---|---|
| B08 | Full Unit csproj as it then existed | 28 pass / 1 fail (`Scale_in_and_partial_close`) | Pre-fix baseline |
| C01 | `--filter …~TradeReconstructionTests` | **5/5** including `Scale_in_and_partial_close` | The flipped fact is green in isolation |
| B11 | Source review (no claim this is a new run) | `ScaleIn` polarity matches A21; fixture “passes” | Same predicate |
| C17 | Full Unit as then existed (83 cases) | Averaging FAIL marked **stale**; new red was qty-test math | Confirms avg-down is no longer the Unit blocker |

C52 adds **no** new `dotnet test` line. The 29/29 figure is the B08 census with that one fail removed.

---

## 10. How a later agent should verify (do not treat as done)

Closed-set filter (reproduces the 29, not the extra files):

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity normal --filter "FullyQualifiedName~SmokeTests|FullyQualifiedName~TradeReconstructionTests|FullyQualifiedName~BaselineScorerTests|FullyQualifiedName~RiskEngineTests|FullyQualifiedName~ExecutionAndSizingTests|FullyQualifiedName~SymbolNormalizerTests|FullyQualifiedName~VolumeConverterTests"
```

Expect:

```text
Total tests: 29
     Passed: 29
     Failed: 0
```

Whole-project run is a **different** experiment. Record its total/pass/fail/skip separately. Do not relabel it “29/29.”

---

## 11. Disposition

| Metric | Value |
|---|---|
| Pre-fix Unit (B08, measured) | **28 / 29** (1 fail) |
| Post-fix Unit on that same 29 (C52, review) | **Expect 29 / 29** |
| The case that flips | `TradeReconstructionTests.Scale_in_and_partial_close` |
| Flip cause | `ScaleIn` long compare `>` → `<` (A21) |
| Other 28 expected to stay green | Yes — none assert the inverted predicate |
| Integration expected | **3 / 3** (unchanged) |
| §60 averaging-down after 29/29 | **PARTIAL**, not COVERED |
| C52 ran `dotnet test` | **No** |
| Product / test source edited by C52 | **No** |

**One-line:** if the suite that printed 28/29 is the suite you still mean, the avg-down polarity fix is expected to print **29/29**; that is a smoke-gate repair, not a reconstruction or scoring contract.
