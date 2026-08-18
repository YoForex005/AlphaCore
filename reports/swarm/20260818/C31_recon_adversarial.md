# C31 — Adversarial reconstruction: zero volume, canceled deals, mixed brokers

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C31_recon_adversarial.md` |
| Agent | C31 (recon adversarial) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Question | Do `TradeReconstructionTests` + `TradeReconstructor` hide a failing case on **zero volume**, **canceled deals**, or **mixed brokers**? |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (334 lines, 12 307 bytes, SHA-256 `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD`) |
| Tests read | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` (112 lines, 3 939 bytes, SHA-256 `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2`) |
| Adjacent (read, not edited) | `NormalizedDeal.cs`, `ReconstructedTradeResult.cs`, `DealAction.cs`, `DealEntry.cs`, `VolumeConverter.cs`, `SymbolNormalizer.cs`, `BrokerCodes.cs`, `DealIngestionService.cs`, `EfTradingStore.cs` |
| Law | A21 §§4.4 / 6 / F17 / F23; A83 (13/14 are not fills); architecture §§10 / 14 / 15 |
| Measurement | `dotnet test tests/Unit --filter FullyQualifiedName~TradeReconstructionTests` → **5/5 passed**. Throwaway harness `reports/swarm/20260818/_tmp_c31_recon` called the compiled `TradeReconstructor` (Release) on the tapes below. Reflection: `ReconstructedTradeResult` has **no** `Dirty` / `FailureCode`. |
| Method | Read the two assigned files in full. Built tapes the five facts never construct. Compared measured rows to A21/A83. Prefer a false negative over a fake PASS. |

---

## 0. Verdict

**Yes. Two first-3-poisoning failing cases are live in the engine today. Mixed-broker isolation is not one of them.**

| Hunt | Failing case found? | What the engine actually does | First-3 effect |
|---|---|---|---|
| **Zero volume** | **YES (hard)** | `ToLots(0) == 0` → `continue`. No `RECON_ZERO_VOLUME`. A later real OUT still **completes the book clean**. | A book that saw a zero-volume tradeable deal is **eligible**. Fixture **Z8**: `CountCompletedXauUsdTrades=3`, `IsEarlyScoreEligible=true`. |
| **Canceled deals** | **YES (hard)** | `IsTradingDeal` is false for 13/14, so `.Where(IsTradingDeal)` **drops** them before apply. No inverse (good). No dirty (bad). A later real OUT of the same `position_id` **completes a clean XAU trade**. | Fixture **C9**: extra-ticket `BUY_CANCELED` on trade #2 + two other round-trips → **count=3, eligible=true**. A21 F17 / A83: that lifecycle is dirty and **must not** occupy a first-3 slot. |
| **Mixed brokers** | **No book-merge FAIL** | `OrdinalIgnoreCase` broker filter + login filter run **before** `GroupBy(PositionId)`. F23-shaped tape (same login, same `position_id`, same tickets, two product codes) reconstructs as two isolated completes. | ACH 2 completes do **not** latch just because SWX already has 3 on the same login. |

`TradeReconstructionTests` cannot see any of this. All five facts use `BrokerId="ACHIEVER"`, login `1`, `VolumeNative=1000` or `2000`, and never construct `BuyCanceled` / `SellCanceled`. **5/5 green is not coverage of the three assigned edges.**

Do **not** claim reconstruction is safe on manager-cancel rows, on `Volume()==0`, or that “tests cover F17 / F23.” Mixed-broker **filter** works; it is still **untested** in the unit class.

---

## 1. What the tests lock (and do not)

`TradeReconstructionTests` is five happy-path smokes plus a helper that cannot express the assigned edges:

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
            Action = action,
            Entry = entry,
            VolumeNative = volume,
            Price = price,
            Profit = profit,
            Commission = 0,
            Swap = 0,
            Time = DateTimeOffset.UnixEpoch.AddMinutes(t)
        };
```

| Fact | Broker | Volume native | Canceled 13/14 | Other login | Other broker |
|---|---|---|---|---|---|
| `Reconstructs_simple_round_trip` | ACHIEVER | 1000 / 1000 | no | no | no |
| `Scale_in_and_partial_close` | ACHIEVER | 1000 × 4 | no | no | no |
| `Reverse_inout_closes_then_opens_opposite` | ACHIEVER | 1000 / 2000 | no | no | no |
| `First_three_completed_xau_unlocks_early_score` | ACHIEVER | 1000 × 6 | no | no | no |
| `Ignores_balance_deals` | ACHIEVER | 0 on **Balance** only | no | no | no |

`Ignores_balance_deals` is the only zero-native row, and it is `DealAction.Balance`, which `IsTradingDeal` already drops. That is **not** `RECON_ZERO_VOLUME` (A21: zero volume on a **tradeable** BUY/SELL).

Measured this review:

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~TradeReconstructionTests --nologo

Passed!  Failed: 0, Passed: 5, Skipped: 0, Total: 5
```

A21 matrix rows **F17** (cancel dirty) and **F23** (broker isolation) are **absent**. There is no `RECON_ZERO_VOLUME` fact.

---

## 2. Engine surfaces that decide the three hunts

```29:39:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var trading = deals
            .Where(d => d.IsTradingDeal)
            .Where(d => string.Equals(d.BrokerId, brokerId, StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Login == login)
            .OrderBy(d => d.Time)
            .ThenBy(d => d.DealTicket)
            .ToList();

        var results = new List<ReconstructedTradeResult>();
        foreach (var group in trading.GroupBy(d => d.PositionId))
            results.AddRange(ReconstructPosition(brokerId, login, group.Key, group.ToList()));
```

```74:78:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        foreach (var deal in deals)
        {
            var lots = _volume.ToLots(deal.VolumeNative);
            if (lots <= 0)
                continue;
```

```25:25:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public bool IsTradingDeal => Action is DealAction.Buy or DealAction.Sell;
```

```62:63:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public bool IsEarlyScoreEligible(string brokerId, long login, IReadOnlyList<NormalizedDeal> deals) =>
        CountCompletedXauUsdTrades(brokerId, login, deals) >= 3;
```

Consequences that the five facts never poke:

1. Actions `13` / `14` (`BuyCanceled` / `SellCanceled`) never reach `ReconstructPosition`. They cannot dirty a book because **there is no dirty channel** (`hasDirty=False`, `hasFailureCode=False` on the result type).
2. `VolumeNative == 0` is a silent `continue` after the deal **did** pass `IsTradingDeal`. Profit / commission / swap on that row vanish. The book is not marked dirty.
3. Broker isolation is a **pre-filter**, not a composite group key. Same `position_id` + same `login` on two codes stay separate **if and only if** `BrokerId` strings differ under `OrdinalIgnoreCase`.
4. First-3 is `completed && IsXauUsd` count `>= 3`. Anything that completes clean is a latch candidate. Dirty exclusion is impossible.

`VolumeConverter.Manager.ToLots(0) == 0`. `ToLots(1) == 0.0001` (sub-hundredth; A21 would be `RECON_VOLUME_NOT_QUANTIZED`, not zero — included because it is the adjacent “volume too small to be a real fill” hole).

---

## 3. Zero volume — failing case

A21 §4.4: tradeable `volume_h == 0` → `RECON_ZERO_VOLUME`, dirty that lifecycle, exclude from first-3. A83: `volume_h == 0` on a **canceled** row is **not** this code.

All volumes below are Manager native (`/ 10_000`). `1000` = 0.10 lot. Harness broker `ACHIEVER`, login `1`, symbol `XAUUSDm`.

### 3.1 Measured tape

| ID | Tape | Spec | Measured | Vs spec |
|---|---|---|---|---|
| Z1 | BUY IN vol=0 only | dirty stub; 0 completed | `EMPTY` (silent skip) | **FAIL** (no `RECON_ZERO_VOLUME`) |
| Z2 | IN vol=0 then OUT 1000 | dirty; then `RECON_OUT_FLAT` | `EMPTY` (OUT on null open skipped) | **FAIL** dirty; no phantom close |
| Z3 | IN 1000 then OUT vol=0 | open rem=0.10 + dirty | 1 open long rem=0.10, tickets=`[1]`, **clean** | remaining **PASS**; dirty **FAIL** |
| **Z4** | IN 1000; OUT vol=0 profit=99; OUT 1000 profit=20 | dirty; 99 not a fill; first-3 exclude | **1 completed**, net=**20**, tickets=`[1,3]`, `DealCount=2`, `count=1` | **HARD FAIL** |
| Z5 | IN 1000; IN vol=0 @ 2290; OUT 1000 | dirty; do not pretend the mid IN never happened | 1 completed, `WasScaledIn=false`, `EntryVwap=2300`, tickets=`[1,3]` | **FAIL** |
| Z6 | IN 0 + OUT 0 profit=5 | two zero failures; profit 5 is not a trade | `EMPTY` (5 vanished) | **FAIL** dirty |
| Z7 | IN/OUT native **1** (0.0001 lot) | `RECON_VOLUME_NOT_QUANTIZED`; not first-3 | **1 completed**, lots=0.0001, `count=1` | **FAIL** (adjacent) |
| **Z8** | two clean XAU round-trips + third IN, **zero OUT**, real OUT | trade #3 dirty; `count=2`; eligible **false** | **count=3, eligible=true**; #3 tickets=`[14,16]` | **HARD FAIL** |

### 3.2 The case that must go in a future fact (Z4 / Z8)

Z4 numbers, copied from the harness:

```text
IN  BUY  ticket=1  pos=13  vol=1000  px=2300  profit=0
OUT SELL ticket=2  pos=13  vol=0     px=2310  profit=99
OUT SELL ticket=3  pos=13  vol=1000  px=2320  profit=20
```

Measured `ReconstructedTradeResult`:

| Field | Value |
|---|---|
| Count | 1 |
| `Completed` | **true** |
| `IsXauUsd` | true |
| `NetRealizedPnl` | **20** (99 dropped with the zero row) |
| `DealTickets` | **`[1, 3]`** — ticket 2 is invisible |
| `CountCompletedXauUsdTrades` | **1** |

Z8 puts that pattern on the **third** lifecycle. The existing first-3 fact (`First_three_completed_xau_unlocks_early_score`) would still pass: it never inserts a zero-volume OUT. The engine latches `EARLY_SCORE` on a book A21 says is dirty.

Why: `lots <= 0` `continue`s **before** `ApplyOut`. The open long is untouched. The next real OUT is a normal flatten. `WasPartialClose` stays false (the zero row never entered `CloseOut`). First-3 sees a textbook completed XAUUSD trade.

`Ignores_balance_deals` does not protect this path. Balance never reaches `ToLots`.

### 3.3 What is *not* broken about zero volume

A lone zero OUT does **not** flatten the book (`Z3` remaining 0.10). The engine does not treat `0 / 10_000` as a fill. The defect is **missing failure + missing dirty**, which becomes a **false first-3 positive** as soon as a later real close exists.

---

## 4. Canceled deals — failing case

A83 / A21 F17 binding, restated only as far as this measurement needs:

- `13` / `14` are **not** BUY/SELL. Never apply volume. Never invent an inverse.
- On an XAUUSD book: emit `RECON_CANCELED_DEAL`, mark current/last lifecycle **dirty**, **exclude** from first-3.
- Extra-ticket encoding (F17: ticket 961 BUY + ticket 962 `BUY_CANCELED`) must **not** flatten 5017 by inversion. If 5017 later flats on a surviving 0/1 leg, it is still dirty.

`DealAction.BuyCanceled = 13`, `SellCanceled = 14` already match the SDK. `IsTradingDeal` is **correct for the volume book** and **wrong as the only pipeline gate**.

### 4.1 Measured tape

| ID | Tape | Spec | Measured | Vs spec |
|---|---|---|---|---|
| C0 | `IsTradingDeal(BuyCanceled)` | false | **false** | **PASS** (volume skip) |
| C1 F17 | 961 BUY IN 5017; 962 action=13; 963–964 clean 5018 | 5017 dirty open; 5018 counts; `completed_count=1` | 5017 **clean** open rem=0.10 tickets=`[961]`; 5018 complete; `count=1` | count **PASS**; dirty **FAIL** |
| **C2** | F17 + SELL OUT 965 on 5017 | 5017 completed+**dirty**; `count=0` | **1 completed CLEAN**, tickets=`[961,965]`, `count=1` | **HARD FAIL** |
| C3 F17b | only ticket 970 action=13 | 0 trades + dirty stub | `EMPTY` | no inverse **PASS**; stub **FAIL** |
| C4 F17c | 971 BUY 1000; 972 action=13 vol=2000; 973 OUT 1000 | remaining +0.10→0; **dirty**; `count=0` | remaining path **correct** (no overclose); `count=1` completed | remaining **PASS**; first-3 **FAIL** |
| C5 F17d | 981 BUY; 982 `SellCanceled` OUT | stay open rem=0.10; dirty; `count=0` | open rem=0.10, `count=0`, **clean** | remaining **PASS**; dirty **FAIL** |
| C6 F17e | F17b + BALANCE −1 | clawback not on the book | `EMPTY` | **PASS** (no phantom net=−1) |
| C7 F17g | 3 completes, then third OUT rewritten to `SellCanceled` | recount 3→2; eligible true→false | `before=3/True after=2/False` | **PASS** (eligible is a recount, not a sticky latch) |
| C8 | 961 BUY + 962 action=13, no later OUT | do not invert; rem=+0.10 | 1 open long rem=0.10 | **PASS** (no inverse) |
| **C9** | clean #1; #2 = IN + `BuyCanceled` + OUT; clean #3 | #2 dirty; `count=2`; eligible **false** | **count=3, eligible=true**; #2 tickets=`[3,5]` | **HARD FAIL** |

### 4.2 The case that must go in a future fact (C2 / C9)

C2 (Encoding B + later flatten) — the smallest first-3 poison:

```text
961  BUY          IN   pos=5017  vol=1000  px=2400
962  BUY_CANCELED IN   pos=5017  vol=1000  px=2400
965  SELL         OUT  pos=5017  vol=1000  px=2410  profit=10
```

Measured:

```text
n=1  pos=5017  Completed=true  xau=true  net=10
tickets=[961,965]   DealCount=2
CountCompletedXauUsdTrades=1
```

962 is gone. The lifecycle looks like F01. `IsEarlyScoreEligible` will count it.

C9 scales that to a latch:

```text
pos 1: IN/OUT clean
pos 2: IN + BUY_CANCELED + OUT     ← A83 F17c shape
pos 3: IN/OUT clean
```

Measured: **three** completed XAUUSD trades, `eligible=true`. Spec: two clean + one dirty, eligible **false**.

### 4.3 What is *not* broken about cancels

The engine **does not** treat 13 as BUY or 14 as SELL. It **does not** invent an inverse fill (C8 rem stays +0.10; C3 does not open a short). A83’s “never invert” rule holds. Official in-place cancel of a **close** (C5 / C7) leaves the book open and **does** retract a recount latch.

The product bug vs A21/A83 is the **missing third class**: canceled is implemented as silent `is_balance_like`. First-3 on any account that later flats a cancel-tainted `position_id` is optimistic.

Adjacent ingest (not re-executed here, already in A83): `EfTradingStore.UpsertDealAsync` is first-write-wins. A same-ticket `BUY` → `BUY_CANCELED` mutation never reaches this function. Encoding A is invisible in production even if the reconstructor were fixed.

---

## 5. Mixed brokers — isolation holds; no failing merge

A21 §1.5 / F23: tickets are not globally unique. Key is `(broker_id, login, position_id, lifecycle_seq)`. Same login `1001`, same `position_id` `5001`, two brokers → **two** trades, two `First3State`s.

Product codes (`BrokerCodes`): `ACHIEVER`, `STARWAVEFX`. A21 F23 writes `ACH` / `SWX`. Tests hard-code `ACHIEVER` only.

### 5.1 F23-shaped tape (product codes, Manager native)

| broker | ticket | login | pos | action/entry | native | price | profit |
|---|---:|---:|---:|---|---:|---:|---:|
| ACHIEVER | 101 | 1001 | 5001 | BUY IN | 10000 (1.00) | 2400 | 0 |
| ACHIEVER | 102 | 1001 | 5001 | SELL OUT | 10000 | 2410 | 10 |
| STARWAVEFX | 101 | 1001 | 5001 | BUY IN | 5000 (0.50) | 2390 | 0 |
| STARWAVEFX | 102 | 1001 | 5001 | SELL OUT | 5000 | 2395 | 2.5 |

`Reconstruct("ACHIEVER", 1001, mixed)` → **1** complete, lots=1.00, net=10, tickets=`[101,102]`, `BrokerId=ACHIEVER`.

`Reconstruct("STARWAVEFX", 1001, mixed)` → **1** complete, lots=0.50, net=2.5, tickets=`[101,102]`, `BrokerId=STARWAVEFX`.

Ids: `ACHIEVER:1001:5001:60000` vs `STARWAVEFX:1001:5001:60000` — no collision.

### 5.2 Other mixed tapes (all measured)

| ID | What was tried | Result | Fail? |
|---|---|---|---|
| M2 | ACH 2 completes + SWX 3 completes, **same login, overlapping position ids**, one list | ACH `count=2` eligible=false; SWX `count=3` eligible=true | **No leak** |
| M3 | deals `ACHIEVER`, call `Reconstruct("achiever", …)` | 1 complete; result `BrokerId="achiever"` | Filter **PASS**; Id casing follows the **argument**, not the deal |
| M4 | deals `ACH`, call `Reconstruct("ACHIEVER", …)` | **EMPTY**; `Reconstruct("ACH", …)` works | Not an alias. Porting F23 with `BrokerCodes.Achiever` against fixture `ACH` yields **zero trades** |
| M5 | deals `"ACHIEVER "` (trailing space), call `"ACHIEVER"` | **EMPTY** | Not trimmed. Silent venue drop, not a merge |
| M6 | same broker, same `position_id`, logins 1 and 2 | isolated lots 0.10 vs 0.20 | **No leak** |
| M7 | same ticket numbers on two brokers | both books `DealCount=2` | Cross-broker tickets are **not** treated as dups (also: engine has **no** in-broker dedupe — F16, out of scope) |
| M8 | both venues **mislabeled** `ACHIEVER` on one `position_id` | 2 completes (netting reuse after flatten) | Correct given the strings; ingest poison, not a filter bug |
| M9 | one `Reconstruct("ACHIEVER")` on a mixed list | SWX lifecycle omitted | API contract: caller must iterate brokers (`ReconstructionScoringService` does) |
| M10 | same `OpenedAt` ms / login / pos, two brokers | distinct Ids | **No collision** |

### 5.3 Why this hunt did not yield a merge FAIL

The filter is applied **before** `GroupBy(PositionId)`. Two venues only share a book if their `BrokerId` strings are `OrdinalIgnoreCase`-equal. Product codes `ACHIEVER` and `STARWAVEFX` do not collide.

What mixed-broker work still owes (not a measured merge bug):

- **Zero unit facts.** F23 is not in `TradeReconstructionTests`.
- **No `lifecycle_seq`.** F23’s key `(broker, login, pos, seq)` is approximated by `"{BrokerId}:{Login}:{PositionId}:{OpenedAtMs}"`.
- **Caller casing becomes identity.** `Reconstruct("achiever")` vs `Reconstruct("ACHIEVER")` on the same deals produce different `Id` / `BrokerId` stamps (`M3`).
- **A21 short codes are not product codes.** `ACH` ≠ `ACHIEVER` (`M4`). That is a fixture-port trap, not an isolation leak.
- **No trim.** `"ACHIEVER "` is another venue (`M5`).

None of those merge STARWAVEFX volume into an ACHIEVER first-3 count when the labels are the two product constants.

---

## 6. Scoreboard (this review only)

| Case | Area | Verdict |
|---|---|---|
| S0 simple round-trip (unit-test shape) | sanity | **PASS** (matches the 5/5 class) |
| Z1–Z2, Z3-dirty, Z5–Z7 | zero volume | **FAIL** (no `RECON_*`, no dirty) |
| Z3 remaining | zero volume | **PASS** (does not flatten on 0) |
| **Z4 / Z8** | zero volume | **HARD FAIL** (clean complete / early-score latch) |
| C0, C1-count, C3-volume, C4-remaining, C5-remaining, C6, C7, C8 | canceled | **PASS** (skip / no inverse / recount retract) |
| C1-dirty, C3-dirty, C5-dirty | canceled | **FAIL** (no dirty / no stub) |
| **C2 / C4-first3 / C9** | canceled | **HARD FAIL** (canceled-tainted complete is first-3 eligible) |
| M1 F23, M2, M6, M7, M10 | mixed brokers | **PASS** isolation |
| M3 / M4 / M5 | mixed brokers | **PASS** as filter behavior; **untested** convention/casing/whitespace traps |
| Unit class coverage of Z / C / M | tests | **0 facts** |

---

## 7. Why the existing suite stays green

| Would catch Z4 / Z8 / C2 / C9? | Why not |
|---|---|
| `Reconstructs_simple_round_trip` | No zero row, no 13/14 |
| `Scale_in_and_partial_close` | All four legs `VolumeNative=1000`, action BUY/SELL |
| `Reverse_inout_closes_then_opens_opposite` | Different bug family (INOUT money, already C01/B11) |
| `First_three_completed_xau_unlocks_early_score` | Three *clean* round-trips only; asserts `== 3` / `true`, never a negative |
| `Ignores_balance_deals` | `DealAction.Balance`, not tradeable zero and not 13/14 |

A regression that treats `VolumeNative==0` as “not a deal” and `BuyCanceled` as “not a deal” is exactly what the current filter already does. The facts agree with that implementation. They do not agree with A21.

---

## 8. Honesty box

| Claim | Measured |
|---|---|
| “Found a failing zero-volume case” | **Yes.** Z4/Z8: zero tradeable OUT is dropped; a later real OUT completes a **clean** XAU trade that latches first-3. |
| “Found a failing canceled-deal case” | **Yes.** C2/C9: extra-ticket `BUY_CANCELED` is dropped; a later OUT completes a **clean** XAU trade. F17 dirty-exclude does not exist. |
| “Found a failing mixed-broker merge” | **No.** F23-shaped tape and overlapping `position_id`s stay isolated when `BrokerId` strings differ. |
| “5 unit tests pass ⇒ these edges are safe” | **Those tests never construct vol=0 BUY/SELL, 13/14, or a second broker.** |
| “Canceled deals are inverted into fake closes” | **No.** Remaining after 961+962 is +0.10. Inverse is not the bug. |
| “Zero volume flattens the book” | **No.** Zero OUT alone leaves rem=0.10. The bug is silent skip + later clean complete. |
| “First-3 excludes dirty / canceled / zero-vol books” | **Cannot.** No `Dirty`. Eligible is `completed && IsXauUsd` count `>= 3`. |
| Product source modified | **No.** |

---

## 9. Files cited

- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (SHA-256 `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD`)
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs` (SHA-256 `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2`)
- `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs`
- `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs`
- `D:\Prop\src\Domain\Enums\DealAction.cs` (`BuyCanceled=13`, `SellCanceled=14`)
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` (`ACHIEVER`, `STARWAVEFX`)
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs` (`Manager` scale 10 000)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`RebuildTraderAsync` calls `Reconstruct(brokerCode, login, deals)`)
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` (F17, F23, `RECON_ZERO_VOLUME`, `RECON_CANCELED_DEAL`)
- `D:\Prop\reports\swarm\20260818\A83_canceled_deals.md`
- Measurement only (not product): `D:\Prop\reports\swarm\20260818\_tmp_c31_recon\`

**Product source was not modified.** This report is the assigned write.
