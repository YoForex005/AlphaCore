# D101 — Untested reconstruction edges: OUT_BY, zero volume, mixed broker

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D101_recon_edges.md` |
| Agent | D101 (recon edge inventory) |
| Date | 2026-08-18 |
| Assigned | List untested recon edges: **OUT_BY**, **zero volume**, **mixed broker**. Write this file. Do **not** modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| Law | A21 §§1.5–1.6 / 4.4 / 7.5 / F09 / F23; A37 `ENTRY_OUT_BY=3`; A38 / `VolumeConverter`; A83 (zero on cancel ≠ `RECON_ZERO_VOLUME`); A89 #6 / #13 / #19; architecture §§10 / 14 / 15 |
| Method | Re-read live reconstructor + all product tests. Hashed files. Ran `dotnet test` on the recon-adjacent unit classes. Grepped `DealEntry.OutBy`, `VolumeNative = 0`, `STARWAVEFX` under `D:\Prop\tests`. Cross-checked A21 F09/F23, B34, C31, D11, D33, D72, D73. Nothing answered from memory. |

---

## 0. Verdict

**All three assigned families are untested in product xUnit / Integration.** A green run of the reconstruction class does **not** lock close-by, tradeable zero volume, or broker isolation.

| Family | Product facts that construct the edge | A21 fixture | A89 class on disk | Engine today | First-3 risk if untested |
|---|---|---|---|---|---|
| **OUT_BY** (`DealEntry.OutBy = 3`) | **0** | F09 **missing** | `#6 OutByReconstructionTests` **absent** | Same `ApplyOut` as `Out`. No `position_by_id`. | Same-ms pair order + overclose clip unguarded |
| **Zero volume** (tradeable `VolumeNative == 0`) | **0** | `RECON_ZERO_VOLUME` **missing** | `#19 ReconstructionZeroAndBadVolumeTests` **absent** | `ToLots(0)==0` → silent `continue`. Book stays **eligible**. | **Z4 / Z8 first-3 poison** (harness FAIL vs spec) |
| **Mixed broker** (ACHIEVER + STARWAVEFX on one tape) | **0** | F23 **missing** | `#13 ReconstructionBrokerIsolationTests` **absent** | Pre-filter `OrdinalIgnoreCase` **before** `GroupBy(PositionId)` | Isolation holds in C31 harness; a filter regression stays green |

Canceled deals are **no longer** in this “untested trio.” The 6th unit fact + `EligibleForFirstThree` dirty scan exist (D73 / D11). C31 §4 C9 and D33 “5 facts / no 13/14” are **stale** on cancel only. They remain accurate on OUT_BY, zero volume, and mixed broker.

Do **not** claim “F09 / F23 / zero-volume are covered.” Do **not** treat D11/C31 throwaway harnesses as product tests.

---

## 1. Measured surface (this SHA)

### 1.1 Files

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | 12768 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | 1171 | `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | 2042 | `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` |
| `D:\Prop\src\Domain\Enums\DealEntry.cs` | 239 | `C0A217FC3C44B1DEB2CB50F705C3C7D03103760D61B01C3FEBAB6FCC74A49E08` |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | 180 | `CF4165CE7A317B0282B9149B078E5D1E630F72524190AB20E0952BECBBAE1182` |
| `D:\Prop\src\Domain\Volume\VolumeConverter.cs` | 1318 | `C6C5E3FD26343532EF047F46D7728A5FED7027B82312A225B9CC3AA881EAC0A2` |
| `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | 836 | `C81AEE8F15DA0EB1449DA3549A0FDD809D8C1607B9964F908830DD8F371F5487` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | 1430 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | 1858 | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4535 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | 4895 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| `D:\Prop\tests\Unit\DealReasonTests.cs` | 1333 | `2B660B79B2D9BF812F637AF5200894FBA74536E7AB28439F8187CA956BAEB0E9` |
| `D:\Prop\tests\Unit\VolumeConverterTests.cs` | 791 | `DD04782A06319BB978C2E908C5C1FDEB6EBDB85E8525399FCBABBCE5CA94BFE5` |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | 3119 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` |

`tests/Unit/Reconstruction/` is **absent**. Grep of `D:\Prop\tests` for `OutBy` / `OUT_BY`: **0 hits**. Grep for tradeable `VolumeNative = 0`: **0 hits** (only `DealAction.Balance`). Grep for `STARWAVEFX` under tests: **0** (integration uses `BrokerCodes.Achiever` only).

### 1.2 This-pass test run

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter "FullyQualifiedName~TradeReconstructionTests|FullyQualifiedName~DealReasonTests|FullyQualifiedName~VolumeConverterTests"
  --nologo

Passed!  Failed: 0, Passed: 11, Skipped: 0, Total: 11
```

| Class | Facts | Touches assigned families? |
|---|---:|---|
| `TradeReconstructionTests` | **6** | **No.** Entries used: `In` / `Out` / `InOut` only. Volumes `1000` / `2000` / `0` on **Balance**. Broker always `"ACHIEVER"`, login `1`. |
| `DealReasonTests` | 2 | **No.** Rollover BUY + reason allow-list. |
| `VolumeConverterTests` | 3 | **No.** `ToLots(1000)` / Extended 1.00. Never `ToLots(0)`. |
| Integration `SeedingAndStoreTests` | 2 | **No.** Seeds two broker **rows**; reconstructs demo tape. Does not feed overlapping tickets, `OutBy`, or tradeable vol=0. |

### 1.3 What the six recon facts *do* lock (so they are not this list)

| Fact | Broker | Entry set | Native vol | Why it is not an assigned edge |
|---|---|---|---|---|
| `Reconstructs_simple_round_trip` | ACHIEVER | In, **Out** | 1000 | Happy `ENTRY_OUT`, not `OUT_BY` |
| `Scale_in_and_partial_close` | ACHIEVER | In, Out | 1000 | Fused scale/partial |
| `Reverse_inout_closes_then_opens_opposite` | ACHIEVER | In, InOut | 1000 / 2000 | INOUT leftover |
| `First_three_completed_xau_unlocks_early_score` | ACHIEVER | In, Out | 1000 | Three *clean* books; positive latch only |
| `Canceled_deal_on_a_position_excludes_it_from_first_three` | ACHIEVER | In, Out + `BuyCanceled` | 1000 | **Cancel family** (tested). Not zero-vol, not OutBy, not mixed |
| `Ignores_balance_deals` | ACHIEVER | In + **Balance** | **0** | Zero is on `DealAction.Balance`. `IsTradingDeal` drops it **before** `ToLots`. **Not** `RECON_ZERO_VOLUME` |

Helper ceiling (`Deal(...)` L112–130): `BrokerId="ACHIEVER"`, `Login=1`, `SourceSymbol="XAUUSDm"`, `OrderTicket=DealTicket`, commission/swap 0, no `Reason`. Until facts stop using it, **F09 / F23 / Z\*** cannot be written in this class.

---

## 2. Engine branches the suite never enters

```87:107:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        foreach (var deal in deals)
        {
            var lots = _volume.ToLots(deal.VolumeNative);
            if (lots <= 0)
                continue;

            switch (deal.Entry)
            {
                case DealEntry.In:
                    ...
                case DealEntry.Out:
                case DealEntry.OutBy:
                    if (open is null)
                        continue;
                    if (ApplyOut(open, deal, lots, out var closed))
                    ...
```

```29:32:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var scoped = deals
            .Where(d => string.Equals(d.BrokerId, brokerId, StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Login == login)
            .ToList();
```

| Branch | Lines | Product test? |
|---|---|---|
| `lots <= 0` → `continue` | 90–91 | **No** (Balance never reaches here) |
| `DealEntry.OutBy` → `ApplyOut` | 98–107 | **No** |
| `Out`/`OutBy` while `open is null` | 100–101 | **No** |
| `CloseOut` clamp `Min(lots, Remaining)` | 268 | **No** (and applies to OutBy) |
| `CloseOut` `closeLots <= 0` still `ApplyCommon` | 269–272 | **No** |
| Broker filter vs a second `BrokerId` | 29–31 | **No** |
| Login filter vs a second login | 31 | **No** |
| `EligibleForFirstThree = false` for **zero-volume** | 34–51 | **No** — scan is cancel-only |
| `position_by_id` | — | **Impossible.** Field absent on `NormalizedDeal`, `Mt5Deal`, `Mt5DealDto`, `ReconstructedTradeResult` |

`EligibleForFirstThree` exists (default `true`). The only writer is the canceled-`PositionId` rewrite. Zero-volume and mixed-broker dirtiness have **no channel**.

Production score (`ReconstructionScoringService.RebuildTraderAsync`) uses `Completed && IsXauUsd` and **ignores** `EligibleForFirstThree`. Even if a future zero-vol fact flipped the flag, scoring would still latch unless that service is also tested. That is adjacent, not a substitute for the three families.

---

## 3. Family A — `ENTRY_OUT_BY` (untested edges)

A21 §7.5: `ENTRY_OUT_BY` is identical for **this** `position_id`. Store `position_by_id` for audit. Counterparty is a **separate** row on the other `position_id`. Do **not** synthesize an opposite deal.

`DealEntry.OutBy = 3` exists. Fake MT5 `ClosedRoundTrip` emits only `DealEntry.In` / `DealEntry.Out`. Docs mention OUT_BY (`docs/trade-reconstruction.md` table). Tests never construct it.

D11 harness `B34_F09` measured **money PASS** (nets 3.30 / 0.30, two ids). That is a **reports-only** eval, not an xUnit lock. Close-time tie-break is **not** ticket. No `position_by`.

### 3.1 Inventory (each row = one missing product fact)

| ID | Untested edge | Spec / expected | Live SUT (unasserted) |
|---|---|---|---|
| **OB-01** | `DealEntry.OutBy` constructed at all | A89 #6 first method | Never |
| **OB-02** | **A21 F09** hedge pair: pos 6001 LONG + 6002 SHORT; tickets 803/804 `entry=3` same `time_msc` | Two completed trades, keys `…/6001/1` + `…/6002/1`; nets **3.30** / **0.30**; `completed_count=2`; **not** one scale-in | ApplyOut per group. Sort `(Time, DealTicket)` → 803 then 804 |
| **OB-03** | B34 twin of partial flatten: same numbers as F03/F04 OUTs but `Entry=OutBy` | Same remaining / VWAP / flags as `Out` | Same path; **0** facts |
| **OB-04** | Partial OUT_BY leaves rem > 0 | `WasPartialClose=true`, `Completed=false`, first-3 count **0** | `CloseOut` would set the flag if lots < rem |
| **OB-05** | Full flatten OUT_BY | `Completed=true`, `ClosedAt` set, `ExitVwap`, rem=0 | Same as Out |
| **OB-06** | Short closed by **BUY** OUT_BY (F09 6002) | Direction stays SHORT; action BUY is the hedge close | No sign check (`RECON_OUT_SAME_DIRECTION` missing) |
| **OB-07** | Long closed by **SELL** OUT_BY (F09 6001) | Direction stays LONG | Untested |
| **OB-08** | Do **not** invent the counterparty book | Only listed `position_id`s emit rows | Engine groups by listed ids only — unasserted |
| **OB-09** | Single-sided OUT_BY (counterparty missing from tape) | This book still completes independently | Untested |
| **OB-10** | Persist / audit `position_by_id` | A21 DealIn + trade audit field | **MISSING** on every persist/DTO/result type. Grep `position_by` under `src/` = **0** |
| **OB-11** | Same-ms OUT_BY pair first-3 **identity** | A21 apply order `(time_msc, completing ticket)` | `CompletedXauUsdTrades` sorts `ClosedAt, OpenedAt` — **no** `ThenBy` ticket (D11 H4) |
| **OB-12** | OUT_BY on a **flat** book | `RECON_OUT_FLAT`, dirty | `if (open is null) continue;` silent |
| **OB-13** | OUT_BY **overclose** (vol > remaining) | `RECON_OUT_OVERCLOSE` (F18 cousin) | `Math.Min` clip; clean complete; **no leftover reverse** (B11 C4 / D11 B4: “same clip on OUT_BY”) |
| **OB-14** | Same-sign OUT_BY (BUY OUT_BY on a long) | `RECON_OUT_SAME_DIRECTION` | Applied as a close anyway; exit VWAP written at the buy |
| **OB-15** | OUT_BY after scale-in | One trade; `WasScaledIn`; exit VWAP from OutBy legs | Untested |
| **OB-16** | Two sequential partial OUT_BYs then flat | One lifecycle; `WasPartialClose`; deal_count includes both OutBy tickets | Untested |
| **OB-17** | Mix `Out` then `OutBy` on the same `position_id` | Still one book | Untested |
| **OB-18** | INOUT leftover then OUT_BY flatten of the new side | Seq2 completes via OutBy | Untested |
| **OB-19** | F09 money + commission on **both** hedge legs | Long net 3.30 = 4.00 + (−0.35×2); short 0.30 | Harness PASS; **no** unit assert |
| **OB-20** | `DealTickets` includes the OutBy ticket | F09: `[801,803]` and `[802,804]` | Untested |
| **OB-21** | Two hedge completes do **not** latch first-3 | count=2, `IsEarlyScoreEligible=false` | First-3 fact never uses OutBy |
| **OB-22** | Hedge vs netting: two `position_id`s ≠ one scale-in | A89 #7; A21 §1.6 | First-3 uses three ids but never OutBy / never asserts “not merged” |
| **OB-23** | Close-by then **reuse** same `position_id` (new IN) | New `Id` / seq (F11 cousin) | No `lifecycle_seq`; Id is `OpenedAt` ms |
| **OB-24** | Volume mismatch between hedge sides (0.50 vs 0.30) | Each book independent; no cross-net | Untested |
| **OB-25** | `XAU_LIFECYCLE_REDUCED` / `COMPLETED` on OutBy | A21 §4.3 | Events **not emitted** |
| **OB-26** | Fake / demo tape never emits `entry=3` | Manager close-by path | `FakeMt5BrokerConnector.ClosedRoundTrip` is In+Out only |
| **OB-27** | Ingest cannot carry `position_by` even if tests wanted it | A21 DealIn | `Mt5DealDto` has no field; `LoadDealsAsync` cannot map it |

**A89 gap:** `#6 OutByReconstructionTests` and `#7 HedgeVsNettingReconstructionTests` are **not on disk**.

---

## 4. Family B — zero volume (untested edges)

A21 §4.4: tradeable `volume_h == 0` → `RECON_ZERO_VOLUME`, mark that lifecycle **dirty**, **exclude** from first-3. A83: `volume_h == 0` on a **canceled** row is **not** this code. A21 §3: `volume_h` is 0 only for non-trade / cancel.

Live: `VolumeConverter.Manager.ToLots(0) == 0` then `if (lots <= 0) continue;` **before** `ApplyIn` / `ApplyOut`. Profit / commission / swap / ticket on that row vanish. `EligibleForFirstThree` stays **true**. A later real OUT completes a **clean** XAU book.

`Ignores_balance_deals` does **not** cover this. Balance never reaches `ToLots`.

D11 `B10_ZERO_VOL_OUT` (re-measured on this SHA): IN 1.00 + OUT vol=0 profit=99 + OUT 1.00 profit=20 → **1 completed**, net=**20**, tickets=`[1,3]`, eligible **true**. Spec: dirty, 99 is not a fill, first-3 exclude.

C31 Z1–Z8 remain valid **engine** measurements. They are still **not** product tests. C31’s “5 facts / never construct vol=0 BUY/SELL” is still true (now 6 facts; the extra one is cancel, not zero).

### 4.1 Inventory

| ID | Untested edge | Spec | Live SUT (unasserted) |
|---|---|---|---|
| **ZV-01** | Lone tradeable BUY IN `VolumeNative=0` | Dirty stub; 0 completed; `RECON_ZERO_VOLUME` | `EMPTY` (silent skip) |
| **ZV-02** | IN vol=0 then real OUT | Dirty + `RECON_OUT_FLAT` on the OUT | `EMPTY` (OUT on null open skipped) |
| **ZV-03** | Real IN then OUT vol=0 | Open rem unchanged; dirty | 1 **open** long, tickets=`[IN]`, **clean** / eligible |
| **ZV-04** | Real IN; **zero OUT + profit**; real OUT | Dirty; profit on zero row is not a fill; exclude first-3 | **HARD FAIL vs spec:** 1 completed **clean**, net = last OUT only (D11 B10) |
| **ZV-05** | Real IN; mid **zero IN**; real OUT | Do not pretend the mid IN never happened; dirty; `WasScaledIn` policy | Mid IN dropped; `WasScaledIn=false`; VWAP from first IN only |
| **ZV-06** | IN 0 + OUT 0 + profit | Two zero failures; profit is not a trade | `EMPTY`; profit vanished |
| **ZV-07** | **Z8 first-3 poison:** two clean XAU + third IN / zero OUT / real OUT | Trade #3 dirty; `count=2`; eligible **false** | **count=3, eligible=true** (C31 Z8). Existing first-3 fact never inserts a zero row |
| **ZV-08** | Native **1** (0.0001 lot) | `RECON_VOLUME_NOT_QUANTIZED`; not first-3 | Completes as 0.0001 lot (D11 B20). Adjacent, still untested |
| **ZV-09** | Zero-volume **OUT_BY** | `RECON_ZERO_VOLUME` on that hedge book | `continue`; later real OutBy can flatten clean |
| **ZV-10** | Zero-volume **INOUT** | `RECON_ZERO_VOLUME` | `continue`; leftover never opens |
| **ZV-11** | Failure code `RECON_ZERO_VOLUME` | A21 §4.4 | **Not implemented.** Result has no `Dirty` / `FailureCode` |
| **ZV-12** | Zero-vol book `EligibleForFirstThree=false` | Dirty exclude | Flag writer is **cancel-only** |
| **ZV-13** | Profit / commission / swap on the zero row | Must not vanish if the row is recorded; must not apply if skipped-as-fill | Dropped with the `continue` |
| **ZV-14** | `DealTickets` omits the zero ticket | Audit must show the bad row **or** a failure | Tickets = surviving fills only |
| **ZV-15** | `WasPartialClose` from a zero OUT | Must not set (0 is not a reduce) | Never enters `CloseOut` — unasserted |
| **ZV-16** | `WasScaledIn` from a zero IN | Must not set | Never enters `ScaleIn` — unasserted |
| **ZV-17** | `VolumeConverter.ToLots(0)` in recon context | 0 → skip + dirty | Converter tests never call `ToLots(0)` |
| **ZV-18** | `CloseOut` `closeLots <= 0` still `ApplyCommon` | Only reachable if `lots>0` but remaining ≤ 0 | Untested dead-adjacent path |
| **ZV-19** | Tradeable `price <= 0` with vol > 0 | `RECON_BAD_PRICE` (A89 #19 sibling) | No price guard; VWAP poisoned (D11 B13) |
| **ZV-20** | Zero vol on **canceled** 13/14 | A83: **not** `RECON_ZERO_VOLUME` | Cancel scan dirties `position_id`; vol unused. Cancel fact uses vol=1000 |
| **ZV-21** | Scoring / dashboard latch on Z8 | First-3 must not include the dirty book | `RebuildTraderAsync` ignores eligibility anyway |
| **ZV-22** | A21 `volume_h=0` fed as Manager native 0 vs hundredths adapter | Adapter must convert once | No adapter; tests speak Manager native |
| **ZV-23** | Extended-scale zero | Same dirty rule | Recon always uses `VolumeConverter.Manager` in tests |
| **ZV-24** | Zero INOUT + later real INOUT / OUT | Dirty seq | Untested |

**A89 gap:** `#19 ReconstructionZeroAndBadVolumeTests` is **not on disk**. Spec stance in A89: assert **today’s skip** *and* that `RECON_ZERO_VOLUME` is missing (fail-closed / documented gap). Neither exists.

---

## 5. Family C — mixed broker (untested edges)

A21 §1.5 / F23: tickets are **not** globally unique. Key is `(broker_id, login, position_id, lifecycle_seq)`. Same login `1001`, same `position_id` `5001`, two brokers → **two** trades, two `First3State`s. Duplicate tickets across brokers are **not** duplicates.

Product codes: `BrokerCodes.Achiever = "ACHIEVER"`, `BrokerCodes.StarwaveFx = "STARWAVEFX"`. A21 F23 writes `ACH` / `SWX` — those strings are **not** aliases (C31 M4).

C31 M1–M10 / D11 M9 measured **isolation PASS** when labels are the two product constants. **Zero unit facts.** A filter regression (drop the broker `Where`, or group by `PositionId` only) would still pass every current test.

### 5.1 What demo / integration do **not** prove

`DemoSeeder` inserts both broker rows and `SyncBrokerAsync`s both codes. Fake tapes use **different logins** (10001 vs 99001), **different position ids** (501–503 vs 701–703), **different deal tickets** (`10000+seq` with seq 1–13 vs 21–23). Integration asserts `Brokers.HaveCount(2)` and Achiever login **10001** count 3. That is **not** F23.

`LoadDealsAsync` filters `d.BrokerId == brokerId` (GUID) then stamps `NormalizedDeal.BrokerId = brokerCode`. Isolation at persist is GUID; isolation at reconstruct is the **string** argument. Neither mixed-tape path is tested.

### 5.2 Inventory

| ID | Untested edge | Spec | Live SUT (unasserted) |
|---|---|---|---|
| **MB-01** | **A21 F23** bit-for-bit: ACH 1.00 @ 2400/2410 net 10 + SWX 0.50 @ 2390/2395 net 2.5; same login 1001, same pos 5001, same tickets 101/102 | Two trades; two First3States; tickets are not dups | Pre-filter. C31 M1 harness PASS |
| **MB-02** | `Reconstruct("ACHIEVER", login, mixed)` does not emit SWX rows | Caller iterates brokers (`RebuildTraderAsync` is per `brokerCode`) | SWX omitted. Untested |
| **MB-03** | `Reconstruct("STARWAVEFX", …)` on the same mixed list | Isolated 0.50 / net 2.5 | Untested |
| **MB-04** | First-3 **does not leak**: ACH 2 completes + SWX 3 completes, same login, overlapping pos ids | ACH eligible **false**; SWX eligible **true** | C31 M2 harness PASS. **0** facts |
| **MB-05** | Same ticket numbers on two brokers both have `DealCount=2` | Not treated as in-broker dups | Engine has **no** in-broker dedupe either (F16, adjacent) |
| **MB-06** | Result `Id` collision | `ACHIEVER:1001:5001:…` ≠ `STARWAVEFX:1001:5001:…` | Prefix is the argument string |
| **MB-07** | Case fold filter | `OrdinalIgnoreCase`: deals `ACHIEVER`, call `"achiever"` reconstructs | Filter PASS; **Id / BrokerId follow the argument**, not the deal (C31 M3) |
| **MB-08** | Caller casing as identity | Same deals → two Ids if caller casing differs | Untested; persist uses `Guid` so DB does not collide |
| **MB-09** | A21 short code `ACH` vs product `ACHIEVER` | Not an alias | `Reconstruct("ACHIEVER")` on `BrokerId="ACH"` → **EMPTY** (M4). Fixture-port trap |
| **MB-10** | Trailing / leading whitespace `"ACHIEVER "` | Not trimmed; silent venue drop, not a merge | C31 M5 EMPTY |
| **MB-11** | Same broker, same `position_id`, **two logins** | Isolated books | Login filter exists. Untested |
| **MB-12** | Both venues **mislabeled** `ACHIEVER` on one `position_id` | Ingest poison: netting reuse after flatten, **one** venue | Correct given the strings; untested |
| **MB-13** | `UpsertDealAsync` same ticket, **two** broker GUIDs | Both persist (`(broker_id, ticket)` identity) | Integration upsert test is **Achiever only** |
| **MB-14** | `LoadDealsAsync` cannot return the other venue | GUID filter | Untested mixed load |
| **MB-15** | `RebuildTraderAsync` / score isolation | SWX latch must not write Achiever `TraderScore` | Per-`brokerCode`. Integration never asserts SWX score vs ACH login collision |
| **MB-16** | `SymbolNormalizer` is **not** keyed by `broker_id` | A21 mappings are `(broker_id, source_symbol)`; GOLD may not be XAU on every venue | Extra mappings are global on the singleton reconstructor |
| **MB-17** | Broker-specific override: `GOLD` → XAU on ACH, unmapped on SWX | F24 / §16 persist mapping | Impossible today without a per-call normalizer. Untested |
| **MB-18** | Demo / Fake never overlap tickets or logins | F23 cannot be “covered by seed” | Measured: different login/pos/ticket spaces |
| **MB-19** | Result `BrokerId` is the **call argument**, not `deal.BrokerId` | Stamp must be the venue being rebuilt | Untested |
| **MB-20** | No `lifecycle_seq` — Id is `{Broker}:{Login}:{Position}:{OpenedAtMs}` | F23 key includes seq | Broker prefix is the only string isolation |
| **MB-21** | Mixed-list **shuffle** still isolates after sort | Sort is global on the scoped (already filtered) list | Untested |
| **MB-22** | Empty / null / unknown broker code | Domain error vs empty result | `Reconstruct(null)` → `ArgumentNullException` on `string.Equals` (D11 M6). Untested |
| **MB-23** | Dashboard first-3 recompute ignores broker if queried wrong | Persist `ReconstructedTrade.BrokerId` is GUID | Integration does not query SWX reconstructed rows |

**A89 gap:** `#13 ReconstructionBrokerIsolationTests` is **not on disk**. Required first methods (`Same_ticket_different_broker_is_not_merged`, `Wrong_login_is_ignored`) do not exist.

---

## 6. Cross-family (also untested)

These need two assigned families in one tape. None exist.

| ID | Tape | Why it matters |
|---|---|---|
| **X-01** | F09-shaped OUT_BY pair **copied on both brokers** (same tickets 803/804) | Close-by + F23. Must yield **four** books, not two |
| **X-02** | Zero-volume OUT_BY on one hedge side | ZV-09 × OB. That side dirty; counterparty may still complete |
| **X-03** | Zero-volume OUT on ACH third book + SWX three cleans, same login | Z8 must not let SWX latch ACH (and vice versa) |
| **X-04** | Cancel on ACH pos + zero-vol on SWX same pos id | Dirty channels must not cross the broker filter |
| **X-05** | Same-ms OutBy pair on ACH + same-ms Out pair on SWX | First-3 identity per venue |
| **X-06** | `"achiever"` call vs deals labeled `ACHIEVER` + `STARWAVEFX` | Case fold must not swallow SWX or split ACH identity |

---

## 7. A21 / A89 scoreboard (assigned families only)

| Contract | Status in product tests | Notes |
|---|---|---|
| F09 Close-by hedge | **Missing** | Harness money PASS (D11). No unit. No `position_by`. Tie-break not ticket |
| F18 OUT overclose | **Missing** | Same clip on OutBy (OB-13) |
| F23 Multi-broker isolation | **Missing** | Harness isolation PASS (C31). No unit |
| `RECON_ZERO_VOLUME` | **Missing** | Silent skip + eligible (ZV-04 / ZV-07 HARD vs spec) |
| `RECON_OUT_FLAT` (OutBy / Out) | **Missing** | Silent `continue` |
| `RECON_VOLUME_NOT_QUANTIZED` | **Missing** | Native 1 completes (ZV-08) |
| A89 #6 `OutByReconstructionTests` | **Absent** | — |
| A89 #7 `HedgeVsNettingReconstructionTests` | **Absent** | — |
| A89 #13 `ReconstructionBrokerIsolationTests` | **Absent** | — |
| A89 #19 `ReconstructionZeroAndBadVolumeTests` | **Absent** | — |

Replay-stability of F09 / F23: **not** encoded.

---

## 8. Suggested facts (do not implement here)

Minimum product tests that would close the assigned families. Prefer A21 ids as names. Assert the **full row** (volumes, VWAP to 12 dp, money, flags, tickets, `EligibleForFirstThree`, count/eligible) **and** document today’s missing `RECON_*` rather than rubber-stamping the skip.

| Pri | Suggested name | Locks |
|---|---|---|
| P0 | `F23_same_ticket_two_brokers_are_not_one_trade` | MB-01…MB-06 |
| P0 | `F23_first3_does_not_leak_across_venues` | MB-04 |
| P0 | `F09_out_by_two_hedge_books` | OB-02, OB-06, OB-07, OB-19, OB-20, OB-21 |
| P0 | `OutBy_partial_is_not_a_second_trade` | OB-03, OB-04 |
| P0 | `Zero_volume_out_then_real_out_is_dirty` | ZV-04 (assert current skip **and** that first-3 is wrongly eligible until the engine dirties) |
| P0 | `Z8_zero_volume_must_not_latch_early_score` | ZV-07 |
| P1 | `OutBy_on_flat_is_not_a_fill` | OB-12 |
| P1 | `OutBy_overclose_is_clipped_today` | OB-13 (lock current clamp + missing leftover) |
| P1 | `Zero_in_only_emits_no_clean_complete` | ZV-01 |
| P1 | `Broker_case_fold_does_not_merge_starwave` | MB-07, MB-10, X-06 |
| P1 | `F09_copied_on_both_brokers_is_four_books` | X-01 |
| P2 | `Position_by_id_round_trips` | OB-10 — **blocked** until the field exists |
| P2 | `ToLots_zero_in_reconstructor` | ZV-17 |

Until those exist, A100 “A27 reconstruction fixtures pass” and A57 reconstruct increment stay **unchecked** for these three families.

---

## 9. Stale reports (read with this file)

| Report | Still true here? | Stale bit |
|---|---|---|
| C31 (zero / cancel / mixed) | Zero + mixed **yes**. Cancel **no** | C9 eligible=true is stale; 6th fact + dirty scan exist |
| D33 (5 facts, 0/25 A21) | OUT_BY / F23 / zero **yes** | Fact count is now **6**; cancel fact landed |
| D09 census SHA of `TradeReconstructionTests` | **Stale** (`5D99BA22…` / 3939 B) | Live `CB223DDE…` / 4895 B |
| B11 H1 “canceled stays clean” | **Stale** | Eligible rewrite landed |
| D11 B10 / B34_F09 / M9 | **Engine** measurements still usable | Not product tests |
| D73 | Cancel predicate accurate | Does not close OB / ZV / MB |

---

## 10. Honesty box

| Claim | Measured |
|---|---|
| “OUT_BY is unit-tested” | **False.** 0 constructions. Enum exists; `case OutBy` is dead to the suite |
| “F09 is proven” | **Harness money PASS only.** No xUnit. No `position_by`. Tie-break untested |
| “Zero volume is tested via `Ignores_balance_deals`” | **False.** That is `DealAction.Balance` |
| “Zero volume is safe” | **False vs A21.** Z4/Z8: later real OUT completes a **clean** eligible XAU book |
| “Mixed broker is tested because seeder inserts STARWAVEFX” | **False.** Different login/pos/ticket spaces; integration never reconstructs a mixed list |
| “Mixed broker isolation is broken” | **Not measured as a merge bug** when codes are `ACHIEVER` / `STARWAVEFX`. It is **untested** |
| “11/11 green ⇒ these edges are safe” | Those 11 facts never construct OutBy, tradeable vol=0, or a second broker |
| Product source modified | **No** |

**FAIL / INSUFFICIENT for the assigned families.** Next increment is isolated xUnit rows for **F09**, **F23**, and **Z4/Z8** — not another fused smoke.

**Product source was not modified.** This report is the assigned write.
