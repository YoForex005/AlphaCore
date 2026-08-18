# E010 — `DealReason` enum, `CountsAsTraderActivity`, and tests

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E010_reason.md` |
| Agent | E010 (senior engineer, `DealReason` + unit tests only) |
| Date | 2026-08-18 |
| Assigned | Read `DealReason.cs` and tests. Write this file. Do **not** modify product source. |
| Primary SUT | `D:\Prop\src\Domain\Enums\DealReason.cs` |
| Consumer | `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.IsTradingDeal` → `TradeReconstructor` |
| Tests | `D:\Prop\tests\Unit\DealReasonTests.cs` (only dedicated class) |
| Law | A82 (`EnDealReason` REAL_TRADING / SERVICE_*); A21 (`is_tradeable` then reason; `close_reason`); A37 (companion action/entry); SDK `IMTDeal::EnDealReason` |
| Companions | D44 (persist gap — still current); D73 (`IsTradingDeal` vs canceled); D33 / C01 (recon tests); D74 (HTTP never serializes this enum) |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Method | Full read of `DealReason.cs`, `NormalizedDeal.cs`, `DealReasonTests.cs`, `TradeReconstructionTests` helper, reconstructor filter, DTO/entity/store path, SDK header 54–80. SHA-256 of those files. `dotnet test` filter `~DealReasonTests`. Token grep of `DealReason` / `CountsAsTraderActivity` under `src/` and `tests/`. |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `STALE_REPORT` / `PARTIAL`.

---

## 0. Headline answer (measured)

**The vocabulary exists and matches `IMTDeal::EnDealReason` 0–19. The allow-list matches A82 `REAL_TRADING`. The tests are two green smokes and they lock the wrong null policy.**

| Surface | Class | One line |
|---|---|---|
| `enum DealReason : uint` members 0–19 | `EXISTS_AND_GOOD` | Same numbers as C++ `DEAL_REASON_CLIENT` … `CORPORATE_ACTION`. Pascal names; `SL`/`TP`/`SO`/`VMARGIN` remapped. |
| Border aliases `FIRST` / `LAST` | `MISSING` | Not stored on deals; not required. |
| `DealReasons.CountsAsTraderActivity` allow-list | `EXISTS_AND_GOOD` | Exactly A82 `{0,1,2,3,4,5,7,9,10,16,17}`. |
| `CountsAsTraderActivity(null)` | `UNSAFE` | Returns **true**. A82: absent = UNKNOWN ≠ trading. **Locked by the passing test.** |
| Out-of-range `(DealReason)20` | `EXISTS_AND_GOOD` (implicit) | Pattern match fails → **false**. No dedicated test. |
| `NormalizedDeal.IsTradingDeal` | `EXISTS_NEEDS_REFACTOR` | Action ∧ reason. Null reason reduces to action-only. |
| A82 money-fold / structural dirty-book | `MISSING` | Non-trading reason is **dropped**. No `apply_money`, no remaining update, no `was_service_close`. |
| Persist / DTO / Fake / C++ extract | `MISSING` | D44 still holds. Store-backed reconstruct always sees `Reason == null`. |
| `DealReasonTests` | `PARTIAL` | **2 / 2 green.** Covers 3 of 20 members + null. Does not encode A82 E1–E5. |
| HTTP wire | n/a | D74: this enum is **not** on any live map. |

**One-line:** domain can classify a hand-built `NormalizedDeal` the A82 way **except** missing reason is treated as a trader; production never supplies a reason, and the only tests bless that hole.

---

## 1. Files hashed (inputs; no product edits)

| Path | Bytes | Lines | SHA-256 |
|---|---:|---:|---|
| `D:\Prop\src\Domain\Enums\DealReason.cs` | 1149 | 50 | `3A4D92122D72155ACA3C0D9174758A966741A8B6E830917E6463CD905659E593` |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | 1171 | 29 | `232573BF65444A7A12A0B320F923CEA3D8DA1B5333E0DD2F0A8E4AFC2FD1801E` |
| `D:\Prop\tests\Unit\DealReasonTests.cs` | 1333 | 44 | `2B660B79B2D9BF812F637AF5200894FBA74536E7AB28439F8187CA956BAEB0E9` |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | 4895 | 131 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | 12768 | 347 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` |
| `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | 836 | 24 | `C81AEE8F15DA0EB1449DA3549A0FDD809D8C1607B9964F908830DD8F371F5487` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | 1858 | 69 | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 12097 | 338 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4535 | 106 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\reports\swarm\20260818\A82_deal_reasons.md` | 27727 | 509 | `F0F03DF134996B99F6E446B6DA69EE286B6E9FF38020E6C2F66EDD2311260619` |
| `D:\Prop\reports\swarm\20260818\D44_reason_gap.md` | 16576 | 252 | `FD09BB8304C24C608B98E20371A924B7A44B87DD162DEFF63D8BFCDADDEAA6D8` |

Hashes of `DealReason.cs`, `NormalizedDeal.cs`, and `DealReasonTests.cs` **match** D44 / D73 / D74. This is the same tree those reports measured.

SDK quote source: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` lines 54–80 (`EnDealReason`, `DEAL_REASON_LAST = CORPORATE_ACTION = 19`).

---

## 2. The type (entire)

```1:50:D:\Prop\src\Domain\Enums\DealReason.cs
namespace TraderIntelligence.Domain.Enums;

/// <summary>
/// Mirrors IMTDeal::EnDealReason. Reconstruction treats only a subset as trader activity.
/// </summary>
public enum DealReason : uint
{
    Client = 0,
    Expert = 1,
    Dealer = 2,
    StopLoss = 3,
    TakeProfit = 4,
    StopOut = 5,
    Rollover = 6,
    ExternalClient = 7,
    VariationMargin = 8,
    Gateway = 9,
    Signal = 10,
    Settlement = 11,
    Transfer = 12,
    Sync = 13,
    ExternalService = 14,
    Migration = 15,
    Mobile = 16,
    Web = 17,
    Split = 18,
    CorporateAction = 19
}

public static class DealReasons
{
    public static bool CountsAsTraderActivity(DealReason? reason)
    {
        if (reason is null)
            return true;

        return reason.Value is
            DealReason.Client or
            DealReason.Expert or
            DealReason.Dealer or
            DealReason.StopLoss or
            DealReason.TakeProfit or
            DealReason.StopOut or
            DealReason.ExternalClient or
            DealReason.Gateway or
            DealReason.Signal or
            DealReason.Mobile or
            DealReason.Web;
    }
}
```

No `[Flags]`. Underlying type `uint` matches `uint32_t Reason()`. No `[EnumMember]` / `[JsonStringEnumMemberName]`. No `First`/`Last` sentinels.

Consumer (the only production call of the helper):

```24:28:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public DealReason? Reason { get; init; }

    public bool IsTradingDeal =>
        Action is DealAction.Buy or DealAction.Sell
        && DealReasons.CountsAsTraderActivity(Reason);
```

`TradeReconstructor.Reconstruct` then:

```39:43:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        var trading = scoped
            .Where(d => d.IsTradingDeal)
            .OrderBy(d => d.Time)
            .ThenBy(d => d.DealTicket)
            .ToList();
```

Grep of `DealReason` / `CountsAsTraderActivity` under `D:\Prop\src`: **only** `DealReason.cs` and `NormalizedDeal.cs`. Grep under `D:\Prop\tests`: **only** `DealReasonTests.cs`. Integration, Fake, API, workers: **0** hits.

---

## 3. C# ↔ official `EnDealReason`

Quoted from `MT5APIDeal.h` 56–80. Numeric identity is 1:1 with the C++ header (canonical). PHP / C# WebAPI samples still stop at `SPLIT=18` (A82 §4) — this product correctly includes `CorporateAction=19`.

| Value | C++ `IMTDeal` | C# member | A82 bucket | `CountsAsTraderActivity` |
|---:|---|---|---|---|
| 0 | `DEAL_REASON_CLIENT` | `Client` | REAL_TRADING | **true** |
| 1 | `DEAL_REASON_EXPERT` | `Expert` | REAL_TRADING | **true** |
| 2 | `DEAL_REASON_DEALER` | `Dealer` | REAL_TRADING | **true** |
| 3 | `DEAL_REASON_SL` | `StopLoss` | REAL_TRADING | **true** |
| 4 | `DEAL_REASON_TP` | `TakeProfit` | REAL_TRADING | **true** |
| 5 | `DEAL_REASON_SO` | `StopOut` | REAL_TRADING | **true** |
| 6 | `DEAL_REASON_ROLLOVER` | `Rollover` | SERVICE_MONEY | **false** |
| 7 | `DEAL_REASON_EXTERNAL_CLIENT` | `ExternalClient` | REAL_TRADING | **true** |
| 8 | `DEAL_REASON_VMARGIN` | `VariationMargin` | SERVICE_MONEY | **false** |
| 9 | `DEAL_REASON_GATEWAY` | `Gateway` | REAL_TRADING | **true** |
| 10 | `DEAL_REASON_SIGNAL` | `Signal` | REAL_TRADING | **true** |
| 11 | `DEAL_REASON_SETTLEMENT` | `Settlement` | SERVICE_STRUCTURAL | **false** |
| 12 | `DEAL_REASON_TRANSFER` | `Transfer` | SERVICE_STRUCTURAL | **false** |
| 13 | `DEAL_REASON_SYNC` | `Sync` | SERVICE_STRUCTURAL | **false** |
| 14 | `DEAL_REASON_EXTERNAL_SERVICE` | `ExternalService` | SERVICE_STRUCTURAL | **false** |
| 15 | `DEAL_REASON_MIGRATION` | `Migration` | SERVICE_STRUCTURAL | **false** |
| 16 | `DEAL_REASON_MOBILE` | `Mobile` | REAL_TRADING | **true** |
| 17 | `DEAL_REASON_WEB` | `Web` | REAL_TRADING | **true** |
| 18 | `DEAL_REASON_SPLIT` | `Split` | SERVICE_STRUCTURAL | **false** |
| 19 | `DEAL_REASON_CORPORATE_ACTION` | `CorporateAction` | SERVICE_STRUCTURAL | **false** |
| — | *(field omitted)* | `null` | UNKNOWN | **true** ← A82 wants **false** |
| 20+ | `> DEAL_REASON_LAST` | `(DealReason)n` | UNKNOWN | **false** |

Name remaps (same integers; do not treat identifier text as the SDK token):

| SDK token | C# identifier | Why it matters |
|---|---|---|
| `SL` | `StopLoss` | JSON / `ToString()` would be `"StopLoss"`, not `"SL"`. Not on HTTP today (D74). |
| `TP` | `TakeProfit` | Same. |
| `SO` | `StopOut` | Same. Not `"SO"`, not `"Stop-Out"`. |
| `VMARGIN` | `VariationMargin` | Same. |

Action is still the first gate (A82 §6, A21 §6). `DealAction.Balance` + `Reason = Client` is **not** a trading deal (`IsTradingDeal` false). MQL5 stamps balance rows `CLIENT`; the action clause is what saves us. There is **no** test that locks that conjunction.

---

## 4. What the helper does **not** do (A82 §7.2–7.3)

`CountsAsTraderActivity` is a **boolean allow-list**. It does not distinguish `SERVICE_MONEY` from `SERVICE_STRUCTURAL`. The reconstructor does not branch on reason except through `.Where(d => d.IsTradingDeal)`.

| A82 rule | Current code |
|---|---|
| Filter action first, then reason | **Yes** (`IsTradingDeal`) |
| REAL_TRADING → apply book | **Yes**, if `Reason` is set to an allow-listed value |
| `ROLLOVER` / `VMARGIN` → money-only; do not touch remaining / VWAP | **No.** Deal is dropped. Swap/profit on that ticket never fold into the open lifecycle. |
| Structural (settlement / transfer / sync / migration / split / corporate) → dirty book, maybe move remaining, `was_service_close` | **No.** Dropped. Remaining can desync. No `Dirty` / `WasServiceClose` on `ReconstructedTradeResult`. |
| Completing REAL_TRADING sets `close_reason` | **No.** Result type has no `CloseReason`. |
| Missing reason → UNKNOWN, do not coerce to `CLIENT` | **Violated.** `null` → trading. |

So even a perfectly filled `NormalizedDeal.Reason` only implements A82’s **binary** “count vs skip,” not the book-keeping half.

---

## 5. Tests (measured)

### 5.1 Run (this agent, 2026-08-18)

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter "FullyQualifiedName~DealReasonTests"
  --nologo --verbosity minimal
```

| Project | Total | Passed | Failed | Skipped | Duration | Exit |
|---|---:|---:|---:|---:|---|---:|
| `TraderIntelligence.Tests.Unit` (filter `DealReasonTests`) | **2** | **2** | **0** | **0** | 3 ms | **0** |

Green is not coverage.

### 5.2 What the two facts actually lock

Class: `TraderIntelligence.Tests.Unit.DealReasonTests`. **0** `[Theory]`. **0** InlineData. No `tests/Unit/Reconstruction/` folder.

| Fact | Constructs | Asserts | Does **not** assert |
|---|---|---|---|
| `Rollover_is_not_a_trader_lifecycle_deal` | One `NormalizedDeal`: Buy / In / `XAUUSD` / native 1000 / swap 1 / `Reason = Rollover` | `IsTradingDeal == false`; `Reconstruct(...)` empty | OUT+IN roll pair (A82 E3); money fold; remaining through the roll; first-3 slot **not** burned; `VariationMargin` |
| `Client_buy_still_counts` | **No deal.** Direct helper calls only | `CountsAsTraderActivity(Client)==true`; `Migration==false`; `null==true` | Any reconstruct of a Client fill; SL/TP/SO; Expert; the other 16 members; action-gate with Client on a Balance row |

The second fact’s name is false advertising: it never buys.

The first fact is a **single isolated IN**. A82’s rollover foot-gun is a paired OUT+IN on an already-open book. That pair is **not** in this class and **not** in `TradeReconstructionTests`.

### 5.3 Coverage matrix (20 members + null + unknown)

| Input | Expected (A82) | Code | Test lock |
|---|---|---|---|
| `Client` | trading | true | **yes** (helper only) |
| `Expert` | trading | true | **no** |
| `Dealer` | trading | true | **no** |
| `StopLoss` | trading (must count close) | true | **no** |
| `TakeProfit` | trading | true | **no** |
| `StopOut` | trading (scoring wants this) | true | **no** |
| `Rollover` | ignore (money) | false | **yes** (IN-only reconstruct empty) |
| `ExternalClient` | trading (FIX/API source) | true | **no** |
| `VariationMargin` | ignore (money) | false | **no** |
| `Gateway` | trading | true | **no** |
| `Signal` | trading | true | **no** |
| `Settlement` | ignore (structural) | false | **no** |
| `Transfer` | ignore (structural) | false | **no** |
| `Sync` | ignore (structural) | false | **no** |
| `ExternalService` | ignore (structural) | false | **no** |
| `Migration` | ignore (structural) | false | **yes** (helper only; no E4 dump) |
| `Mobile` | trading | true | **no** |
| `Web` | trading | true | **no** |
| `Split` | ignore (structural) | false | **no** (A82 E5 untested) |
| `CorporateAction` | ignore (structural) | false | **no** |
| `null` | UNKNOWN → **not** trading | **true** | **yes — locks the A82 violation** |
| `(DealReason)20` / `99` | UNKNOWN → not trading | false | **no** |
| `BuyCanceled` + `Client` | not a fill (D73) | `IsTradingDeal` false via action | **no** in this class |
| `Balance` + `Client` | skip (action gate) | `IsTradingDeal` false | **no** in this class (`Ignores_balance_deals` omits `Reason`) |

**3 / 20 members** have any assertion. **0 / 5** A82 worked examples (E1 SL, E2 SO, E3 rollover pair, E4 migration dump, E5 split) are encoded.

### 5.4 Adjacent tests make the hole worse

`TradeReconstructionTests.Deal(...)` never sets `Reason`. Init default is `null`. `CountsAsTraderActivity(null)` is true, so every recon smoke (round-trip, scale-in, reverse, first-3, cancel extra-ticket, balance) **depends on the unsafe null policy**. If someone later implements A82 UNKNOWN (`null → false`), **those six facts go red** even though they never intended to test reason.

Integration `SeedingAndStoreTests` / Fake `ClosedRoundTrip` build `Mt5DealDto` (14-arg; no reason). They cannot name the field.

---

## 6. Persist path (unchanged from D44)

```text
IMt5BrokerConnector.GetDealsAsync
        │  Mt5DealDto  — no Reason
        ▼
DealIngestionService.SyncBrokerAsync
        ▼
EfTradingStore.UpsertDealAsync → Mt5Deal  — no Reason column
        ▼
LoadDealsAsync → NormalizedDeal { … Comment }  // Reason omitted → null
        ▼
IsTradingDeal: null ⇒ true ⇒ BUY/SELL only
        ▼
ReconstructedTrade  — no close_reason / was_service_close
```

Until ingest persists a nullable `uint` and **does not** backfill `Client`, the allow-list is dead on every store-backed reconstruct. Unit tests that hand-set `Reason = Rollover` do not describe production.

A21 still documents `DealIn.reason` default **0** (`CLIENT`). That default remains `UNSAFE` (A82 §0: do not coerce missing → CLIENT).

---

## 7. Production effect if reason **were** present

| Scenario | With current helper + drop filter | A82 want |
|---|---|---|
| Desktop / EA / mobile / web / SL / TP / SO fill | Applied | Applied + `close_reason` on complete |
| Overnight roll OUT+IN (`Rollover`) | Both legs dropped. If a Client IN is already open, remaining stays; roll swap is **lost**. If the book was empty, nothing opens. **No fake complete** (better than reason-blind A21). | Money fold into open book; one lifecycle |
| Migration IN dump | Dropped. Open exposure invisible. Later Client OUT has nothing to close (or opens a short). | Dirty open, excluded from first-3 |
| Split IN | Dropped. `remaining` ≠ broker lots. Scale-in flags stay false (good) but book is wrong. | Dirty remaining; VWAP not a scale-in |
| Missing ingest (`null`) | **Treated as trading** | UNKNOWN / structural |

On today’s ingest, every row is the last row of that table.

---

## 8. Stale / current companions

| Report | Claim | vs this SHA |
|---|---|---|
| A82 §6 / §10 | `NormalizedDeal` has no reason; no `DealReason` enum; `IsTradingDeal` is action-only | **STALE** on domain types. **Current** on persist + C++ extract. |
| D44 | Reason not persisted; null counts as trading; tests in-memory only | **CURRENT** (same hashes). |
| D73 | `IsTradingDeal` = Buy/Sell ∧ `CountsAsTraderActivity` | **CURRENT.** Canceled still fails the action clause. |
| D74 | `DealReason` not on any HTTP map | **CURRENT.** |
| D33 | `DealReasonTests` 2/2; recon helper has no `Reason` | **CURRENT** (recon class later gained a 6th cancel fact; helper still omits `Reason`). |
| A21 `reason` default 0 | Spec still says optional default CLIENT | **UNSAFE** as a persist default. Not implemented on `Mt5Deal`. |
| B26 “do not invent Reason on Mt5Deal” | Entity still has no column | **CURRENT** for persist. Domain enum **does** exist. |

---

## 9. Findings

1. **`DealReason` 0–19 is a correct mirror of `IMTDeal::EnDealReason`.** `CorporateAction=19` is present (WebAPI samples omit it). Identifier remaps (`StopLoss`/`TakeProfit`/`StopOut`/`VariationMargin`) are cosmetic unless this enum ever hits JSON.
2. **`CountsAsTraderActivity`’s non-null allow-list is exactly A82 `REAL_TRADING`.** Eleven values in, nine out. Out-of-range casts fall through to false.
3. **`null` is treated as trading.** That is the opposite of A82 UNKNOWN and of “do not default missing to CLIENT.” `Client_buy_still_counts` **asserts** `CountsAsTraderActivity(null) == true`, so a correct A82 fix will fail this test on purpose.
4. **Tests are insufficient.** Two facts, three members, no Theory, no E1–E5, no action∧reason conjunction, no SL/TP/SO must-count, no ExternalClient/Gateway (the source-side reasons this product will actually see on FIX/API traders).
5. **Skip ≠ A82 apply.** Rollover/split/migration are dropped, not money-folded or dirty-booked. `ReconstructedTradeResult` still has no `CloseReason` / `WasServiceClose` / `Dirty`.
6. **Production never sets `Reason`.** DTO, entity, store, Fake, C++ `extractDeal` omit it (D44). Store-backed reconstruct is still reason-blind. A green `DealReasonTests` run does not protect first-3 from a live rollover.
7. **`TradeReconstructionTests` is coupled to the unsafe default.** Helper omits `Reason`. Changing null to UNKNOWN will fail reconstruction smokes that never mentioned reason.

**Product source was not modified.** This file is the only write from E010.

---

## 10. Verdict

| Question | Answer |
|---|---|
| Does `DealReason.cs` match the SDK numbers? | **Yes.** 0–19 inclusive. |
| Does the allow-list match A82 real-trading? | **Yes**, for non-null values. |
| Is missing reason handled as A82 UNKNOWN? | **No.** `null → true`. |
| Do the tests prove the policy? | **No.** They prove Client / Migration / Rollover-IN and they **freeze the null bug**. |
| Is reason used on the persist/reconstruct path? | **No.** Field stays null; filter is dead. |
| Product source changed by this agent? | **No.** |

Do **not** treat 2/2 green as an A82 close. Do **not** backfill `Client` on ingest. Any follow-up that flips `CountsAsTraderActivity(null)` to false must also update `DealReasonTests` and every `NormalizedDeal` helper that relies on the default.
