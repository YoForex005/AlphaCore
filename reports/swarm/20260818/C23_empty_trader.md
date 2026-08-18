# C23 — Demo login 10003 (zero deals) scores `INSUFFICIENT_DATA`

| Field | Value |
|---|---|
| Agent | C23 (senior engineer, empty-trader score confirm only) |
| Date | 2026-08-18 |
| Assigned | Demo seeder has account 10003 with no deals. Confirm scoring `INSUFFICIENT_DATA`. Write this report. Do not modify product source. |
| Product source modified | **No.** Report + throwaway eval under `_tmp_c23_empty/` only. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Eval stdout | `D:\Prop\reports\swarm\20260818\_tmp_c23_empty\stdout.txt` |
| Eval command | `dotnet run -c Release --project D:\Prop\reports\swarm\20260818\_tmp_c23_empty\C23EmptyEval.csproj` |
| Eval exit | **0** |
| Eval verdict | **`VERDICT=PASS_INSUFFICIENT_DATA`** |

---

## 0. Verdict

**Confirmed.** Demo Achiever login **10003** is a real seeded account (`contest\yo-2step`, balance/equity 25 000, profit 0) with **zero deals and zero positions**. Reconstruction emits **0** trades. `BaselineScorer` + `TraderStateMachine.FromBaseline` return **`TraderState.INSUFFICIENT_DATA`**. `DemoSeeder` → `ReconstructionScoringService.RebuildTraderAsync("ACHIEVER", 10003)` persists that state (`N=0`) on `trader_scores` and `trader_score_history`.

This is empty-success, not a connector failure. `GetDealsAsync(10003)` returns `[]`. The scorer does **not** invent a book.

| Surface | Measured | Required |
|---|---|---|
| Fake account 10003 exists | **Yes** (`contest\yo-2step`) | empty-success fixture |
| Deals / positions for 10003 | **0 / 0** | no invented tape |
| Reconstructed trades | **0** | `N=0` |
| `EarlyScoreEligible` | **false** | `N < 3` |
| Suggested / persisted state | **`INSUFFICIENT_DATA`** | A22 R4 / A69 TS2 |
| `CanPromoteToLive` | **false** | live still impossible |
| Official score (spec) | **none** | A22 §9.1 / Case D |
| Persisted quality (impl) | **40.00** (capped) | leak vs spec — see §5 |

Do **not** treat the quality number 40 as an official first-three score. The **state** is correct; the **publish** path still writes and ranks the cap.

---

## 1. Binding law (quoted)

A22 §9.1:

> `INSUFFICIENT_DATA` — `N < 3`. No official score.

A22 §9.3 R4:

> `if N < 3: INSUFFICIENT_DATA`

A22 §12.4 Case D (two trades; `N=0` is the stricter empty case):

> official scores not published to leaderboard  
> state `INSUFFICIENT_DATA`  
> no `EARLY_SCORE_ELIGIBLE`

A69 TS2: `N < 3 ∧ ¬(R0–R3) → INSUFFICIENT_DATA`.  
A92 L7: for `N < 3`, leaderboard score fields must be **`null`**, not `0` / not a fake 40.

Domain pin: `BaselineScorer.EarlyScoreTradeCount = 3`.  
`TraderState.INSUFFICIENT_DATA = 0`.

---

## 2. Seeded fixture (no deals)

`D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` `DemoBrokerFactory.CreateDefault()`:

```csharp
new Mt5AccountDto(10001, @"demo\Maxmaster", 100, 10_000, 10_240, 200, 9_800, 240),
new Mt5AccountDto(10002, @"demo\yo-2step", 100, 5_000, 4_820, 150, 4_670, -180),
new Mt5AccountDto(10003, @"contest\yo-2step", 200, 25_000, 25_000, 0, 25_000, 0)
```

`BuildAchieverDeals` emits round-trips **only** for 10001 (positions 501–503) and 10002 (601–603). **No `ClosedRoundTrip(..., 10003, ...)`.** `BuildStarwaveDeals` is login 99001 only.

`DemoSeeder.SeedAsync` (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`) syncs ACHIEVER + STARWAVEFX, then:

```csharp
foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
{
    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
    await scoring.RebuildTraderAsync(code, login, ct);
}
```

10003 is therefore scored on **Achiever** against an empty deal load.

SHA-256 at measure:

| File | SHA-256 |
|---|---|
| `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` |
| `src\Domain\Scoring\BaselineScorer.cs` | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| `src\Application\Ingestion\DealIngestionService.cs` | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` |
| `src\Domain\Enums\TraderState.cs` | `E509C59F1597EE0F424A9F9408D0B27B2C7063D724B3D0A63880E2558B930D68` |

---

## 3. Why the state is `INSUFFICIENT_DATA`

`TraderStateMachine.FromBaseline` (`BaselineScorer.cs` 189–198):

```csharp
if (features.CompletedXauTrades == 0)
    return TraderState.INSUFFICIENT_DATA;

if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
    return TraderState.RISK_BLOCKED;

if (!earlyEligible)
    return TraderState.INSUFFICIENT_DATA;
```

`N=0` short-circuits **before** risk / quality landings. Forced `FromBaseline(eligible=false, quality=99, risk=0, N=0)` still returned `INSUFFICIENT_DATA` (eval `STATE_MACHINE_N0_FORCED`).

`Score()` also sets `EarlyScoreEligible = CompletedXauTrades >= 3` → **false**.

Empty `ComputeFeatures` snapshot (no trades): all PnL/CV/flags 0, `SlUseRate=0`. That snapshot is **not** a score; it is the input the stub still walks (see §5).

---

## 4. Measured run (2026-08-18)

Throwaway eval (not product): `_tmp_c23_empty/`. In-memory `DemoSeeder` + live `BaselineScorer` / `TradeReconstructor` / `EfDashboardQueries`.

```text
ACCOUNT_FOUND=True
ACCOUNT group=contest\yo-2step leverage=200 balance=25000 equity=25000 margin=0 marginFree=25000 profit=0
DEALS_SEED_WINDOW=0
DEALS_UNBOUNDED=0
POSITIONS=0
DEALS_OTHER login=10001 n=6
DEALS_OTHER login=10002 n=6
DEALS_OTHER login=99001 n=6
ACHIEVER_DEAL_LOGINS=10001,10002
RECON trades=0 completedXau=0 earlyEligible=False
SCORE_EMPTY_ARRAY N=0 eligible=False state=INSUFFICIENT_DATA risk=10 behavior=90 quality=40
SCORE_RECON_10003 N=0 eligible=False state=INSUFFICIENT_DATA risk=10 behavior=90 quality=40
SEED_ACCOUNTS=10001,10002,10003,99001
SEED_DEALS_BY_LOGIN=10001=6;10002=6;99001=6
SEED_RECON_BY_LOGIN=10001=3;10002=3;99001=3
SEED_10003_DEALS=0
SEED_10003_RECON=0
SEED_10003_STATE=INSUFFICIENT_DATA
SEED_10003_N=0
SEED_10003_RISK=10
SEED_10003_BEHAVIOR=90
SEED_10003_QUALITY=40
SEED_10003_INSUFFICIENT=True
SEED_10003_HISTORY=1 state=INSUFFICIENT_DATA
DASH_OVERVIEW accounts=4 xauTraders=3 three=3 watch=0 shadow=2 blocked=1
DASH_10003 state=INSUFFICIENT_DATA early=40
DASH_INSUFFICIENT_LOGINS=10003
VERDICT=PASS_INSUFFICIENT_DATA
```

Peer contrast (same seed, not the assigned question): 10001/`99001` → `SHADOW` N=3; 10002 → `RISK_BLOCKED` N=3. Only 10003 is empty.

Control: synthetic `N=2` with SL also scored `INSUFFICIENT_DATA` (A22 Case D). `dotnet test` `BaselineScorerTests` — **3 passed / 0 failed** (`Two_trades_remain_insufficient` locks Case D, **not** `N=0`).

---

## 5. Honesty — state PASS, publish PARTIAL

### 5.1 Empty snapshot still walks SL penalties

`N=0` `ComputeFeatures` sets `SlUseRate=0`. `Score()` then:

| Term | Walk | Result |
|---|---|---|
| risk | `SlUseRate < 0.3` → +10 | **10** (not 0) |
| behavior | `SlUseRate < 0.5` → −10 | **90** (not 100) |
| quality | `50 + 90×0.2 − 10×0.25 = 65.50`, then `N<3` cap | **40.00** |

B12 (`B12_scoring_review.md` demo table) wrote `10003 | 0 / 100 / 40.00`. **Risk/behavior there are stale.** Measured persist is **10 / 90 / 40.00**. State column in B12 (`INSUFFICIENT_DATA`) is still correct.

### 5.2 Official-score leak (A22 / A92)

`ReconstructionScoringService.RebuildTraderAsync` **always** `UpsertScoreAsync`s `RiskScore`, `BehaviorScore`, `EarlyQualityScore`, `CurrentState`. There is no “omit official score when `N<3`” branch.

`EfDashboardQueries.GetTradersAsync` does **not** hide `INSUFFICIENT_DATA`. After seed, login 10003 is on the traders list with `EarlyScore=40`. Rank-1 is still 10001 (`95.50` / `SHADOW`) because sort is `OrderByDescending(EarlyScore)`.

Overview is cleaner: `xauTraders` counts `CompletedXauTrades > 0` → **3** (10003 excluded). `TradersWithThreeTrades` → **3**. Watch/Shadow/Blocked tiles do not count `INSUFFICIENT_DATA`.

A92 L7 / A22 Case D: **FAIL on publish**, **PASS on state**.

### 5.3 Tests do not lock 10003

| Test | What it locks | 10003 / `N=0` |
|---|---|---|
| `BaselineScorerTests.Two_trades_remain_insufficient` | `N=2` → `INSUFFICIENT_DATA` | no empty-list fact |
| `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` | 10001 `N=3` not LIVE; 10002 `RISK_BLOCKED` | **no** `Login==10003` assert; does not even require a 10003 score row |

Empty-trader confirmation today is this report + `_tmp_c23_empty`, not a product test.

---

## 6. What this does **not** claim

- Not a live Achiever Manager proof. Fixture is `FakeMt5BrokerConnector` / `DemoBrokerFactory` only.
- Not A22 formula parity. Quality 40 is a stub walk + cap, not “no official score.”
- Not a leaderboard-compliance PASS (A92 L7 still open).
- Not a claim that 10002 risk/quality in B12 still match (they do not; out of C23 scope).
- Product source was **not** edited. No new unit/integration fact was added under `tests/`.

---

## 7. Files read (absolute)

- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md`
- `D:\Prop\reports\swarm\20260818\B12_scoring_review.md`

Scratch (not product): `D:\Prop\reports\swarm\20260818\_tmp_c23_empty\`

---

*End of C23. Product source was not modified. State for empty demo login 10003 is `INSUFFICIENT_DATA`.*
