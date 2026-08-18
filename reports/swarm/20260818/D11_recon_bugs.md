# D11 — Adversarial bugs in `TradeReconstructor`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D11_recon_bugs.md` |
| Agent | D11 (reconstructor adversarial) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Assigned | Read `TradeReconstructor.cs` adversarially. Write this report. Do not modify product source. |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (**347** lines, SHA-256 `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B`) |
| Result type | `ReconstructedTradeResult.cs` (SHA-256 `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA`) — has `EligibleForFirstThree`, **no** `Dirty` / `FailureCode` / `LifecycleSeq` |
| Input type | `NormalizedDeal.cs` — **no** `VolumeClosed`, `Fee`, `Reason`, `TimeMsc`, `PositionById` |
| Tests on disk | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` (6 `[Fact]`s, SHA-256 `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D`) |
| Law | `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` §§1–8, 10 (F01–F25); `D:\Prop\docs\trade-reconstruction.md` |
| Measurement | Throwaway harness `D:\Prop\reports\swarm\20260818\_tmp_d11_recon\` called the compiled Release `TradeReconstructor` (Manager scale 10_000). `dotnet test --filter FullyQualifiedName~TradeReconstructionTests` → **6/6 passed**. Those six facts do **not** cover the P0 holes below. |
| Method | Line-by-line walk of every `DealEntry` arm, `OpenTrade` mutator, first-3 filter, and identity formula. Numeric traces against A21. Prefer a false negative over a fake PASS. |

---

## 0. Verdict

`TradeReconstructor` is a **happy-path netting book**, not the A21 engine.

It will reconstruct a clean long/short round-trip, same-side scale-in VWAP, partial-then-flat, hedge-by-`position_id`, netting reuse after flatten, and (as of this SHA) **exclude every lifecycle on a `position_id` that ever saw `BuyCanceled`/`SellCanceled` from `CountCompletedXauUsdTrades`**. Averaging-down polarity matches A21 (long add-lower / short add-higher).

It will also, on any real reverse or any malformed entry, **invent volume, throw away a completed side, double-count INOUT money, clip an overclose into a clean flatten, or latch first-3 on a same-sign “close.”** There is still **no** `RECON_*` failure channel. `EligibleForFirstThree` is a cancel-only boolean. Production scoring (`ReconstructionScoringService.RebuildTraderAsync`) **does not read it**.

| # | Sev | One line | Measured |
|---|---|---|---|
| **B1** | **P0 money** | INOUT leftover `OpenTrade.Start` re-applies the reverse deal’s profit/commission/swap. | F04 leftover **net=8.95** (spec **0**). F05 Σ completed **26.85** (spec **17.90**). |
| **B2** | **P0 data loss** | Opposite `ENTRY_IN` calls `ApplyReverse` and **discards** the closed trade (`_ = closed`). | Long 1.00 + IN SELL 0.50 → **only** an open SHORT 0.50; long gone. |
| **B3** | **P0 inventory** | `ApplyReverse` always closes **all** `RemainingLots`, ignoring deal volume. | INOUT SELL 0.30 vs long 1.00 → **one completed long 1.00**, 0.70 vanished. |
| **B4** | **P0 overclose** | `CloseOut` does `Math.Min(lots, RemainingLots)`. Extra volume is dropped, book looks clean. | OUT SELL 1.50 vs long 1.00 → 1 completed, `WasPartialClose=false`, no leftover short. |
| **B5** | **P0 sign** | OUT / INOUT never check `DealAction` against open direction. Same-sign INOUT is a phantom complete. | Long 1.00 + INOUT BUY 1.50 → completed long + open long 0.50; first-3 **+1**. |
| **B6** | **P0 idempotency** | No seen-ticket set. Global sort turns replay `[101,102,101,102]` into scale-in + two OUTs. | 1 trade, `DealCount=4`, max=2.00, net **16.80** (F01 is 8.40 / `deal_count=2`). |

Do **not** claim reconstruction is A21-complete, “safe on reverse,” or that 6/6 green covers first-3 integrity.

---

## 1. What the file actually does (live SHA)

One book per `(brokerId, login, positionId)` after `Time, DealTicket` sort. Broker + login are filtered **first**. `BuyCanceled`/`SellCanceled` on the scoped list populate a `HashSet` of `PositionId`s; those groups are still reconstructed from BUY/SELL only, then every emitted row is rewritten `EligibleForFirstThree = false`.

```29:69:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var scoped = deals
            .Where(d => string.Equals(d.BrokerId, brokerId, StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Login == login)
            .ToList();

        var dirtyPositions = scoped
            .Where(d => d.Action is DealAction.BuyCanceled or DealAction.SellCanceled)
            .Select(d => d.PositionId)
            .ToHashSet();
        // ...
            if (dirtyPositions.Contains(group.Key))
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
        // ...
        return Reconstruct(brokerId, login, deals)
            .Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)
```

Volume is `VolumeConverter.Manager` (`native / 10_000`) **decimal lots**, flatten test `RemainingLots <= 1e-7`. Fees in `ToResult` are the literal `0m`. Identity is `"{BrokerId}:{Login}:{PositionId}:{OpenedAt.ToUnixTimeMilliseconds()}"`.

| Entry | Arm | Intended (A21) | Actual |
|---|---|---|---|
| `In` (0) | `ApplyIn` | Open or same-side scale-in; opposite IN → `RECON_IN_OPPOSITE_DIRECTION` dirty | Opposite IN = **synthetic reverse**; closed half **thrown away** |
| `Out` / `OutBy` (1/3) | `ApplyOut` → `CloseOut` | Opposite sign, `volume <= remaining`, else dirty | No sign check. Over-volume **clipped**. Flat OUT **skipped** |
| `InOut` (2) | `ApplyReverse` | Close `volume_closed ?? remaining`, require leftover > 0, money on closed only | Closes **all** remaining; leftover = `dealLots - remaining`; money applied **twice**; flat INOUT opens as IN |
| other (e.g. 255) | no `default` | `RECON_UNKNOWN_ENTRY` | Silent no-op |

`NormalizedDeal` has no `volume_closed_h`. The engine cannot implement F21 / A21 §7.6’s closed-half.

---

## 2. Critical bugs (must not ship first-3 / scoring on these paths)

### B1 — INOUT money is applied to **both** lifecycles (P0)

A21 §7.6 / F04: the INOUT deal’s profit / commission / storage / fee stay on the **closed** seq. The leftover `Start` must list the same ticket and **must not** `apply_money` again.

What the code does:

1. `ApplyReverse` → `CloseOut` → `ApplyCommon` adds money to the old book (correct).
2. Leftover `OpenTrade.Start` **also** does `GrossRealizedPnl += deal.Profit`, `Commission += deal.Commission`, `Swap += deal.Swap`. There is no “reversal open, skip money” path.

**Measured F04** (Manager native 10_000 / 15_000 = 1.00 / 1.50 lots; comm −0.70 then −1.05; profit 10 on the INOUT):

| Lifecycle | Spec net | Measured net | Tickets |
|---|---:|---:|---|
| seq1 LONG completed | **8.25** = 10 − 0.70 − 1.05 | **8.25** | 401, 402 |
| seq2 SHORT open | **0** | **8.95** = 10 − 1.05 | 402 |

Leftover size/direction/VWAP are correct (`0.50` short @ 2410). Only money is wrong.

**Measured F05** (then BUY OUT 0.50 @ 2390, profit 10, comm −0.35):

| Lifecycle | Spec net | Measured net |
|---|---:|---:|
| seq1 LONG | 8.25 | 8.25 |
| seq2 SHORT | **9.65** | **18.60** = (10+10) + (−1.05−0.35) |
| Σ completed | **17.90** | **26.85** |

`BaselineScorer` sums `NetRealizedPnl` across completed XAU. A trader who reverses **inflates profit factor**, can flip martingale (sign of net), and warps drawdown. Unit fact `Reverse_inout_closes_then_opens_opposite` asserts leftover **lots and direction only** — this bug is green today.

---

### B2 — Opposite `ENTRY_IN` synthesizes a reverse and **deletes** the prior lifecycle (P0)

A21 §7.4 / F19: opposite-side `ENTRY_IN` is `RECON_IN_OPPOSITE_DIRECTION`. Reverse is `ENTRY_INOUT` only.

```135:138:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var (closed, next) = ApplyReverse(open, deal, lots + open.RemainingLots, brokerId, login, positionId);
        _ = closed;
        return next ?? OpenTrade.Start(brokerId, login, positionId, deal, lots, direction, _symbols);
```

`_ = closed` is an explicit throw-away. `ReconstructPosition` never sees that completed lifecycle.

**Measured F19-shape** (long 1.00, then IN SELL 0.50):

| Spec | Measured |
|---|---|
| Dirty open LONG rem=1.00, `completed_count=0` | **One** open SHORT rem=**0.50**, tickets=`[982]` |
| Prior LONG still on the book (dirty) | Prior LONG **absent** |
| No new lifecycle | Synthetic short carries the discarded close’s profit (`gross=10`) |

`dealLots = lots + RemainingLots` makes leftover **always equal to the opposite IN volume** (`leftover = (lots + rem) - rem = lots`). A 1.50 opposite IN against 1.00 remaining becomes leftover **1.50**, not a 0.50 net reverse (harness `B24_OPP_IN_1p5`: open short **1.50**, long gone).

First-3 **undercounts** a real closed long. If the leftover later flats, only the synthetic short counts — wrong side, wrong size, wrong PnL.

---

### B3 — INOUT always closes **all** remaining, even when deal volume is smaller (P0)

```168:174:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var closeLots = open.RemainingLots;
        open.CloseOut(deal, closeLots);
        var closed = open.ToResult(completed: true);

        var leftover = dealLots - closeLots;
        if (leftover <= FlatEpsilon)
            return (closed, null);
```

`closeLots` is not `min(dealLots, remaining)` and not `volume_closed`. It is the full book.

| Deal | Remaining | Spec | Measured |
|---|---:|---|---|
| INOUT SELL 1.50 vs long 1.00 | 1.00 | close 1.00, open short 0.50 | same (happy path; money still B1) |
| INOUT SELL 1.00 vs long 1.00 | 1.00 | `RECON_INOUT_NO_NEW_VOLUME` (F20) | leftover 0 → **one completed clean** |
| INOUT SELL 0.30 vs long 1.00 | 1.00 | invalid / still long 0.70 | leftover −0.70 ≤ ε → **flatten 1.00**, 0.70 inventory gone (`B4_INOUT_UNDERVOL`) |

A feed that puts **new-side only** in `Volume` and closed size in `VolumeClosed` (Manager’s real INOUT encoding) **wipes the position**. `NormalizedDeal` cannot carry `VolumeClosed`, so F21 is unimplementable in this function alone.

---

### B4 — OUT over-close is clipped; the reverse that should have been INOUT is lost (P0)

```267:268:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public void CloseOut(NormalizedDeal deal, decimal lots)
        {
            var closeLots = Math.Min(lots, RemainingLots);
```

A21 F18: long 1.00 then OUT SELL 1.50 → `RECON_OUT_OVERCLOSE`. Measured (`B5_OUT_OVERCLOSE`): `min(1.50, 1.00) = 1.00`, remaining 0, `WasPartialClose=false`, **one completed long**, no leftover short. Same clip on `OUT_BY`.

---

### B5 — No sign check on OUT / INOUT; same-sign INOUT is a phantom first-3 trade (P0)

A21 §7.5–7.6: LONG remaining must be reduced by **SELL**; SHORT by **BUY**. Same-sign is `RECON_OUT_SAME_DIRECTION` / `RECON_INOUT_SAME_DIRECTION`.

Code derives the **new** INOUT direction from `deal.Action` and never compares it to `open.Direction`.

**Measured same-sign INOUT** (long 1.00, INOUT BUY 1.50) — `B6_SAMESIGN_INOUT`:

1. `newDirection = Long`
2. Close **all** 1.00 at the BUY price (a buy cannot close a long)
3. Leftover 0.50 → new **Long**
4. **One completed long** + one open long on the same `position_id`

**Measured first-3 poison** (`B18_FIRST3_PHANTOM`): two clean XAU round-trips + this same-sign INOUT on a third `position_id` → `CountCompletedXauUsdTrades=3`, `IsEarlyScoreEligible=true`. Spec: the third book is dirty, eligible **false**.

**Same-sign OUT** (`B15_SAMESIGN_OUT`): long 1.00, OUT BUY 0.40 → remaining 0.60, exit VWAP written at the **buy** price, `WasPartialClose=true`. Inventory and exit VWAP are both wrong; no dirty.

---

### B6 — Duplicate tickets are applied twice; sort makes F16 a doubled book (P0)

A21 F16: replay F01 as `[101,102,101,102]` → `duplicate_deals_total=2`, **one** trade, `deal_count=2`.

`Reconstruct` sorts `(Time, DealTicket)` **before** apply and has **no** seen-ticket set. Two copies of 101 @ t=1000 and two copies of 102 @ t=2000 apply as:

```text
101 IN  +1.00
101 IN  +1.00   → WasScaledIn, max=2.00
102 OUT −1.00   → WasPartialClose, rem=1.00
102 OUT −1.00   → complete
```

**Measured `B8_F16_DUP_REPLAY`:** 1 completed, `DealCount=4`, tickets=`[101,101,102,102]`, max=2.00, net=**16.80** (F01 net is **8.40**).

Ingest `UpsertDealAsync` is first-write-wins, so a clean DB replay may not hit this. `Reconstruct` is a pure function of the list the caller hands it. `LoadDealsAsync` does not distinct tickets. A duplicated IN **without** a duplicated OUT leaves the book **open** at 2× size (`B19_DUP_IN`: rem=1.00 after one OUT, `WasScaledIn=true`).

---

## 3. High — first-3, identity, failures A21 requires

### H1 — There is still no `RECON_*` / `Dirty` model

A21 §4.4 lists 18 failure codes. `ReconstructedTradeResult` gained `EligibleForFirstThree` (default `true`). That is **not** a failure code. Reflection on the result type: `hasDirty=False`, `hasFailure=False`, `hasSeq=False`.

| Spec code | Live behavior |
|---|---|
| `RECON_IN_OPPOSITE_DIRECTION` | B2: invent reverse, drop closed |
| `RECON_OUT_FLAT` | `if (open is null) continue;` (`B17_OUT_FLAT` = EMPTY) |
| `RECON_OUT_SAME_DIRECTION` | B5: apply anyway |
| `RECON_OUT_OVERCLOSE` | B4: clip |
| `RECON_INOUT_FLAT` | `ApplyReverse(null)` → `Start` as IN (`B16_INOUT_FLAT` = open short) |
| `RECON_INOUT_SAME_DIRECTION` | B5 |
| `RECON_INOUT_NO_NEW_VOLUME` | B3 / F20: treat as flatten |
| `RECON_INOUT_CLOSED_MISMATCH` | impossible (no `volume_closed`) |
| `RECON_ZERO_VOLUME` | `if (lots <= 0) continue;` — book stays **eligible** |
| `RECON_BAD_PRICE` | never checked; `price=0` → `EntryVwap=0` (`B13_PRICE0`) |
| `RECON_MISSING_POSITION_ID` | `GroupBy(PositionId)` opens a book on `0` (`B12_POS0` completes) |
| `RECON_CANCELED_DEAL` | **partially** handled — see H2 |
| `RECON_UNKNOWN_ENTRY` | fall through the `switch` (`B14_UNKNOWN_ENTRY`: open leftover, never flats) |
| `RECON_VOLUME_NOT_QUANTIZED` | native `1` = 0.0001 lot **completes** (`B20_SUB_LOT`) |

Canceled rows are correctly **not** inverted. Zero-volume OUT is correctly **not** a flatten (`lots<=0` never enters `CloseOut`). The defect is **missing failure + missing dirty**, which becomes a **false first-3 positive** as soon as a later real close exists.

**Measured zero-volume then real OUT (`B10_ZERO_VOL_OUT`):**

```text
IN  BUY  vol=10000 profit=0
OUT SELL vol=0     profit=99
OUT SELL vol=10000 profit=20
```

Result: **1 completed**, net=**20**, tickets=`[1,3]`, `EligibleForFirstThree` stays **true**. Ticket 2 and profit 99 vanished. A21: `RECON_ZERO_VOLUME`, dirty, exclude.

---

### H2 — Cancel exclusion is position-scoped, scoring-blind, and not a failure

Live change vs older reviews (C31 is **stale** on this one point): any `BuyCanceled`/`SellCanceled` on `(broker, login)` marks **every** reconstructed row of that `PositionId` `EligibleForFirstThree=false`. `CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` honor the flag.

**What that gets right**

- Extra-ticket `BUY_CANCELED` is not inverted (rem after 961+962 stays the IN size until a real OUT).
- `Canceled_deal_on_a_position_excludes_it_from_first_three` (6th unit fact) encodes: pos 10 IN/OUT then a **later** cancel on the same id + two other completes → `Reconstruct` completed **3**, `CountCompletedXauUsdTrades` **2**, eligible **false**. Measured harness `B39` (IN + cancel + OUT on the middle book + two cleans): `count=2`, `eligible=false`.
- The unit fact **would have failed** on the pre-`EligibleForFirstThree` SHA (`E20457B3…`). It is green on `AEA3930B…`.

**What is still wrong vs A21 F17**

| Requirement | Live |
|---|---|
| Emit `RECON_CANCELED_DEAL` | **No** |
| Mark lifecycle **dirty** | **No** `Dirty` bit; only a first-3 bool |
| Do not invent an inverse | **Yes** |
| Taint **current or last** lifecycle | Taints **all** seqs on that `position_id` (future netting reuse after a historical cancel can never enter first-3) |
| Persist dirty for audit | `ReplaceReconstructedAsync` drops `EligibleForFirstThree` (entity has no column) |
| Exclude from first-3 | `CountCompletedXauUsdTrades` **yes**; production score **no** |

`ReconstructionScoringService.RebuildTraderAsync`:

```82:87:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
```

That filter is `Completed && IsXauUsd` — **not** `EligibleForFirstThree`. `BaselineScorer.Score` then sets `EarlyScoreEligible = CompletedXauTrades >= 3` on the **unfiltered** list. A book that the new unit test refuses to count will still latch `EARLY_SCORE` and still contribute PnL / martingale / averaging-down in production.

The 6th fact does **not** lock F17’s extra-ticket-then-later-OUT shape as “completed+dirty.” `Reconstruct` still emits that row as `Completed=true` with default-looking money (`B11_CANCELED`: tickets=`[961,965]`, net=10, looks like F01).

---

### H3 — Identity is `OpenedAt` milliseconds; same-second reuse collides

```313:313:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
                Id = $"{BrokerId}:{Login}:{PositionId}:{OpenedAt.ToUnixTimeMilliseconds()}",
```

A21 key is `(broker_id, login, position_id, lifecycle_seq)`. MT5 `DealTime` is often **second** resolution. Harness `B9_SAME_MS_ID`: IN + OUT + IN on position 77, all `time_msc=1000` → **two** result rows, **one** Id `ACHIEVER:1:77:1000` (`uniqueIds=1`, `n=2`). Any dictionary keyed by `Id` drops a lifecycle. Persist uses `Guid.NewGuid()`, so the DB does not collide — it also **throws the A21 key away**.

Caller casing is part of the Id (`B28_CASE`): `Reconstruct("ACHIEVER")` vs `Reconstruct("achiever")` on the same deals → `ACHIEVER:1:203:1000` vs `achiever:1:203:1000`. Filter is `OrdinalIgnoreCase`; identity is not normalized.

---

### H4 — First-3 order is not apply order; latch is a count

- `Reconstruct` returns `OpenedAt, PositionId`.
- `CompletedXauUsdTrades` re-sorts `ClosedAt, OpenedAt` — **no** `ThenBy` completing ticket.
- A21 first-3 is apply order = `(time_msc, ticket)` of the completing deal (F06 / F09 same-ms OUT_BY pair).
- `IsEarlyScoreEligible` is `count >= 3`. Latch *existence* is OK when every counted XAU is clean. Latch *identity* (`first3_keys`, `early_score_deal_ticket`) **does not exist**.
- Trade #4 is not excluded from scoring (`BaselineScorer` uses the entire completed-XAU list the caller passes).

Same-ms closes (`B27_CLOSE_TIE`) happen to order by `OpenedAt` (pos 201 then 202), which matches ticket 11 before 21 on this tape — not a general guarantee.

---

### H5 — Volume is decimal lots + `1e-7`, not integer hundredths

A21 §2 / §8.7: remaining is `int64` hundredths; flatten is `remaining_h == 0`. Never IEEE lots.

With `VolumeConverter.Manager` and `decimal`, `k/10000` is exact, so **well-quantized Manager volumes will not drift**. The failure is contract, not float dust:

- Sub-0.01 lots complete (`B20_SUB_LOT`: native 1 → 0.0001 lot, `Completed=true`). Spec: `RECON_VOLUME_NOT_QUANTIZED`.
- There is no adapter that takes A21 fixture `volume_h` (1.00 lot = **100**). Feeding F01 `volume_h=100` as `VolumeNative` through Manager yields **0.01** lots, not 1.00. The six unit tests use Manager native `1000` = 0.10 lots — they are **not** A21 fixtures.
- `VolumeClosed` is absent on `Mt5Deal` / `Mt5DealDto` / `NormalizedDeal`.
- `Fees` are hardcoded `0` (`B25_FEES`: F01-shaped net **8.40** matches A21 only because fee=0).

---

### H6 — Symbol / book grouping is looser than A21

- `SymbolNormalizer.TryMapSource` is **not** keyed by `broker_id`. A21 mappings are `(broker_id, source_symbol)`.
- Any compact symbol starting with `XAUUSD` maps to XAUUSD. **`XAUUSDFUT` counts as first-3 XAU** (`B22_PERMISSIVE_XAU`, `countXau=1`). A21 F24 allow-list would UNMAP that suffix.
- Canonical is taken from the **first** deal of the lifecycle. Mixed symbols on one `position_id` (`B23_MIXED_SYMBOL_POS`: XAUUSDm IN + XAGUSD OUT) produce **one** completed XAU book, exit VWAP = 31. A21 books are per position; mixed-symbol is ingest poison, but the engine never dirties.
- Non-XAU books are still reconstructed (A21 drops them before the book). `CompletedXauUsdTrades` filters after. Harmless extra persist; dangerous if the normalizer over-maps (H6 bullet 2).
- F24 happy half **does** work: XAGUSD `IsXauUsd=false`, `XAUUSD.a` maps, `completedXau=1` (`B21_F24_SYMBOL`).

---

## 4. Medium / adjacent (still reconstruction)

| Id | Hole | Measured / path |
|---|---|---|
| M1 | `ToResult` never sets `EligibleForFirstThree`; only the post-pass `with` does | Cancel is the only writer |
| M2 | Persist drops `RemainingVolumeLots`, `DealTickets`, string `Id`, `EligibleForFirstThree` | `EfTradingStore.ReplaceReconstructedAsync`; entity `ReconstructedTrade` has no those columns |
| M3 | `LoadDealsAsync` never sets `StopLoss` / `TakeProfit` | Production `InitialSl`/`FinalSl` always null even though `ApplyCommon` would honor them |
| M4 | `SL=0` is `HasValue` | `B26_SL0`: `InitialSl=0`, `FinalSl=0` — not null. Scorer `SlUseRate` uses `> 0`, so 0 does not count as SL (accidentally OK) |
| M5 | INOUT `FinalSl` on the **closed** side is the new-side SL | `ApplyCommon` during `CloseOut` — A21 also `maybe_update_sl_tp` on the close path; economically odd |
| M6 | `Reconstruct(null)` | `ArgumentNullException` (`source`) — no domain error |
| M7 | `TradeReconstructor` is a DI **singleton** | Reconstruct itself is stateless per call (`OpenTrade` is local). `SymbolNormalizer` is mutable if a future caller hits `RegisterVenueInstrument` on a shared instance |
| M8 | Commission / daily-swap actions are dropped | Matches A21 F14 (`B42_F14`: tickets `[942,944]`, commission deal not folded) |
| M9 | Hedge `OUT_BY` ≡ `OUT` per `position_id` | F09 money **PASS** (`B34_F09`: nets 3.30 and 0.30). No `position_by` audit field |

---

## 5. What is *not* broken (keep these)

Measured PASS on this SHA (Manager-native deals, mapped symbols):

| Path | Evidence |
|---|---|
| Simple long IN + opposite OUT | `B30_F01` net=8.40, VWAP 2400/2410; unit `Reconstructs_simple_round_trip` |
| Scale-in VWAP, not avg-down on add-higher | `B31_F02` entry=`2403.333…`, max=1.5, `WasScaledIn=true`, `WasAveragedDown=false`; net=22.90 |
| Partial then remainder = **one** trade | `B38_F03` exit=2416, `WasPartialClose=true`, net=14.60 |
| Long add-lower / short add-higher avg-down | `Scale_in_and_partial_close`; `B35_F08_SHORT_AVG` `avg=True`; compare runs **before** VWAP update |
| INOUT leftover **lots / direction / ticket-on-both** | Unit reverse fact; `B37` tickets 402 on both rows |
| Netting reuse after flatten | `B32_F11` LONG then SHORT, distinct `OpenedAt` |
| Sort OUT-before-IN in the list | `B33_F15_SORT` applies 951 then 952 |
| Hedge two `position_id`s | `B34_F09` two completes, no merge |
| Balance / credit / commission skip | `Ignores_balance_deals`; `B42_F14` |
| Broker + login pre-filter | Same `position_id` on two codes stay isolated (C31 M1; not re-merged here) |
| Open-only does not count | `B41_OPEN_ONLY` completed=0 |
| Cancel is not inverted | B11 rem path; IsTradingDeal false for 13/14 |
| Cancel-tainted `position_id` excluded from **count API** | 6th unit fact; `B39` count=2 |

`WasScaledIn` includes two `ENTRY_IN` on one `order_ticket` (A21 F10). Do not “fix” historical entry VWAP to inventory VWAP — A21 `open_vol_sum_h` is not reduced on OUT.

---

## 6. A21 fixture scorecard (this SHA, Manager-native translation)

Interpretation: **PASS** = this function, given correctly scaled Manager-native deals and mapped symbols, would match the fixture’s trade count, sides, volumes, flags, and money. **PARTIAL** = shape OK, identity/events/money/dirty incomplete. **FAIL** = wrong trades or wrong money.

| Fx | Intent | Result | Why |
|---|---|---|---|
| F01 | Simple long | **PASS** | `B30_F01` / unit simple fact (different numbers). |
| F02 | Scale-in VWAP | **PASS** | `2403.333…` / max 1.50 / not avg-down. |
| F03 | Partial then rest | **PASS** | One trade, partial, exit 2416. |
| F04 | INOUT split + money on A | **FAIL** | Leftover size/dir OK; leftover **net=8.95 ≠ 0** (B1). |
| F05 | Close leftover | **FAIL** | Σ net **26.85 ≠ 17.90** (B1). |
| F06 | First-3 + aliases + partial GOLD | **PARTIAL** | Count / GOLD map likely OK. No `first3_keys`, no event on ticket 609, scorer uses all completed. |
| F07 | Avg-down long | **PASS** | Current `<` compare. |
| F08 | Avg-down short | **PASS** (untested in unit class) | Current `>` compare. `B35` `avg=True`. |
| F09 | Hedge OUT_BY pair | **PASS** | Two ids, money 3.30 / 0.30. Close-time tie-break is not ticket. |
| F10 | Same-order partial fills | **PASS** | Two IN → scaled; `OrderCount` distinct. |
| F11 | Netting reuse | **PASS** | `B32_F11`. Seq-less new Id. |
| F12 | First-3 includes a reverse | **FAIL** | Seq2 money wrong (B1); no first3 slice. |
| F13 | Open only | **PASS** | `B41`. |
| F14 | Balance skip | **PASS** | `B42`. |
| F15 | Sort OUT-before-IN | **PASS** | `B33`. |
| F16 | Duplicate tickets | **FAIL** | `B8` deal_count=4, doubled money. |
| F17 | Cancel dirty | **PARTIAL** | Count API excludes the `position_id`. No `RECON_CANCELED_DEAL`, no `Dirty`, scoring ignores the flag, taint is all-seqs. |
| F18 | OUT overclose | **FAIL** | B4 clip + clean complete. |
| F19 | Opposite IN | **FAIL** | B2. |
| F20 | INOUT flatten (no new) | **FAIL** | `B7_INOUT_EXACT` clean complete. |
| F21 | `volume_closed` ≠ remaining | **FAIL** | Field absent; inferred remaining. |
| F22 | `position_id=0` | **FAIL** | `B12_POS0` opens and completes. |
| F23 | Two brokers | **PASS** | Pre-filter (not re-run; unchanged). |
| F24 | Unmapped vs `XAUUSD.a` | **PARTIAL** | XAG skip + `.a` map **PASS**. Any `XAUUSD*` compact also maps (`B22`) — wider than A21. |
| F25 | Three simples → eligible | **PASS** | Unit `First_three_completed_xau_unlocks_early_score`. |

**Tally: 12 PASS / 3 PARTIAL / 10 FAIL** of 25.

Replay-stability of **flags and leftover lots** is likely. Replay-stability of **money on reverse** is stably **wrong**. F17 moved from FAIL (C31) to PARTIAL on this SHA; do not back-port C31’s “count=3 on cancel-tainted complete” without re-measuring.

A21 §11 unit matrix (27 rows): this repo has **6** reconstruction facts. Named A89 classes (`PositionReversalReconstructionTests`, …) are **not on disk**.

---

## 7. Why the existing suite stays green

| Fact | What it locks | What it cannot see |
|---|---|---|
| `Reconstructs_simple_round_trip` | 0.10 long, net=100, VWAP | F01 commissions/fees, F18–F22 |
| `Scale_in_and_partial_close` | flags + max=0.20 | F02 VWAP 12dp, F08 short, isolated partial |
| `Reverse_inout_closes_then_opens_opposite` | 2 rows, leftover **lots** 0.10 | **B1 money**, F20, same-sign, under-volume |
| `First_three_completed_xau_unlocks_early_score` | three *clean* round-trips → 3 / true | phantom INOUT, zero-vol, F06 noise |
| `Canceled_deal_on_a_position_excludes_it_from_first_three` | count API drops a **position_id** that has a 13/14 row | `Dirty`, `RECON_CANCELED_DEAL`, scoring path, lifecycle_seq, extra-ticket-then-OUT money |
| `Ignores_balance_deals` | `DealAction.Balance` empty | tradeable `volume=0`, 13/14 as failures |

Helper hard-codes `ACHIEVER` / login 1 / `XAUUSDm` / comm 0 / swap 0 / `OrderTicket = DealTicket`. 6/6 green is a smoke suite.

---

## 8. Worked traces (copy into tests later; do not implement here)

Volumes are **lots**. Native = lots × 10_000 under Manager.

| Id | Tape | Assert | Today |
|---|---|---|---|
| T1 | IN BUY 1.00 @ 2400 comm −0.70; INOUT SELL 1.50 @ 2410 profit 10 comm −1.05 | closed net=8.25; leftover rem=0.50 **gross=0 comm=0** | leftover **gross=10 comm=−1.05** |
| T2 | IN BUY 1.00; IN SELL 0.50 | dirty open long rem=1.00; no short | open short 0.50; long gone |
| T3 | IN BUY 1.00; INOUT SELL 0.30 | not a clean complete of 1.00 | completed long 1.00 |
| T4 | IN BUY 1.00; OUT SELL 1.50 | `RECON_OUT_OVERCLOSE`; not first-3 | completed long 1.00 |
| T5 | IN BUY 1.00; INOUT BUY 1.50 | dirty; still one long book | completed long + open long 0.50; count += 1 |
| T6 | IN BUY 1.00; INOUT SELL 1.00 | `RECON_INOUT_NO_NEW_VOLUME` | completed clean |
| T7 | Replay F01 ×2 | deal_count=2, net=8.40 | deal_count=4, net=16.80 |
| T8 | IN + zero OUT profit 99 + real OUT profit 20 | dirty; 99 not a fill | completed net=20, tickets omit the zero row |
| T9 | Same-ms IN/OUT/IN | distinct keys / seq 1 and 2 | **same** `Id` |
| T10 | Two clean + same-sign INOUT | eligible false | eligible **true** |

---

## 9. Honesty box

| Claim someone might make | Measured |
|---|---|
| “EX5-style 95% reconstruction” | **No.** 10/25 A21 fixtures fail; reverse money is systematically wrong. |
| “Reversal is implemented” | **Lots/direction leftover: yes. Money/dirty/`volume_closed`: no.** |
| “Averaging-down is inverted” | **Not on this SHA.** LONG `<` / SHORT `>` matches A21. |
| “Canceled deals poison first-3” | **Count API: no (flag false). Production `RebuildTraderAsync`: still yes.** |
| “Canceled deals are inverted into fake closes” | **No.** Inverse is not the bug. |
| “Zero volume flattens the book” | **No.** Zero OUT alone is skipped. The bug is silent skip + later clean complete. |
| “6 unit tests pass ⇒ recon is done” | **Those tests do not assert F04 money, F16, F18–F22, or scoring’s ignore of `EligibleForFirstThree`.** |
| “First 3 trades are A21 first-3” | **Count of `Completed && IsXauUsd && EligibleForFirstThree` ≥ 3.** No keys, no apply-order slice, no dirty codes. |
| Product source modified for this review | **No.** |

---

## 10. Files cited

- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (SHA-256 `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B`)
- `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` (SHA-256 `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA`)
- `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs`
- `D:\Prop\src\Domain\Enums\DealEntry.cs` / `DealAction.cs` (`BuyCanceled=13`, `SellCanceled=14`)
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs` (`Manager` scale 10_000)
- `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`RebuildTraderAsync` ignores `EligibleForFirstThree`)
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`LoadDealsAsync` / `ReplaceReconstructedAsync`)
- `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` (no remaining / tickets / seq / eligible)
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs` (SHA-256 `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D`)
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md`
- `D:\Prop\docs\trade-reconstruction.md`
- Measurement only (not product): `D:\Prop\reports\swarm\20260818\_tmp_d11_recon\` (`stdout.txt`)

**Product source was not modified.** This report is the assigned write.
