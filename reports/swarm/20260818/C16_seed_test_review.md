# C16 — `SeedingAndStoreTests` review (InMemory seed + deal upsert)

| Field | Value |
|---|---|
| Agent | C16 (senior engineer, seed/store integration-test review only) |
| Date | 2026-08-18 |
| Assigned | Read `tests/Integration/SeedingAndStoreTests.cs`. Write this report. Do not modify product source. |
| Primary file | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |
| Bytes / SHA-256 | **3119** / `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Companions | `A10_integration_tests_audit.md` (stale empty-suite snapshot), `A78_deal_idempotency.md`, `A79_fake_mt5_connector.md`, `A90_integration_class_list.md` (InMemory ≠ §60), `B08_tests_gap.md`, `B12_scoring_review.md`, `B32_ingestion_review.md`, `C09_cserver_fixed.md` |

**Path note:** the assigned relative path `tests/Integration/SeedingAndStoreTests.cs` is under the repo root `D:\Prop\`, **not** `D:\Prop\src\`. There is no copy under `src\`.

---

## 0. Verdict

**PARTIAL smoke. Green. Does not count as Architecture §60 integration.**

The class is a real xUnit surface (not `Assert.True(true)`). Both facts pass against **EF Core InMemory** + `DemoSeeder` + `FakeMt5BrokerConnector`. That proves the **happy-path orchestration** of:

`DemoSeeder` → `DealIngestionService.SyncBrokerAsync` → `EfTradingStore` upserts → `ReconstructionScoringService.RebuildTraderAsync` → `TradeReconstructor` + `BaselineScorer`.

It does **not** prove PostgreSQL migrations, unique-index idempotency, checkpoint resume, outbox, dual-broker ticket isolation, or the §61 “never hit the live account” gate.

| Gate | Classification | Why |
|---|---|---|
| Runnable facts | **2 / 2 PASS** | measured this review |
| Architecture §60 integration (8 areas) | **0 / 8** | InMemory; no migrations, checkpoints, outbox, FIX, reconcile |
| A90 “InMemory may host, never counts” | **violates labeling** | no `[Trait("Seam","InMemory")]` / `CountsAs60=false` |
| A90 “do not call `DemoSeeder` from this suite” | **violated** | fact 1 *is* `DemoSeeder.SeedAsync` |
| A78 unique `(broker_id, deal_ticket)` | **PARTIAL (app-level only)** | `AnyAsync` then `Add`; unique index never exercised |
| “10001 cannot be LIVE” | **vacuous** | `TraderStateMachine.FromBaseline` never returns `LIVE` |
| “10002 is RISK_BLOCKED” | **COVERED** | matches the demo martingale tape |
| Live FIX identifiers in fixture | **FAIL (safety)** | seed writes `live-us-eqx-01.p.c-trader.com` + `live.pepperstone.1369850`; fact 1 **asserts** `TargetCompId == "cServer"` |

**One-line:** useful developer smoke for the fake-broker seed path; **not** Phase-1 proof; `NotBe(LIVE)` and `Count() > 0` are too weak to lock behavior.

---

## 1. Measured run (this review)

Command (read-only; no product edit):

```text
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj
  --nologo --filter FullyQualifiedName~SeedingAndStoreTests --verbosity normal
```

Result (2026-08-18, net8.0, 0 warnings / 0 errors):

```text
Passed  SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores  [816 ms]
Passed  SeedingAndStoreTests.Deal_upsert_is_idempotent                           [20 ms]
Total tests: 2  Passed: 2  Failed: 0  Skipped: 0
Total time: 1.2758 Seconds
```

Sibling in the same assembly (not this class, still in the green project run if unfiltered):

```csharp
// D:\Prop\tests\Integration\UnitTest1.cs
public class PlaceholderRemoved
{
    [Fact]
    public void Integration_project_loads() => Assert.True(true);
}
```

Do **not** count `PlaceholderRemoved` toward this review. It is **FALSE_GREEN**.

Project seam (`TraderIntelligence.Tests.Integration.csproj`): `Microsoft.EntityFrameworkCore.InMemory` 8.0.4; **no** Testcontainers, **no** Respawn, **no** `[Trait]`. References Domain, Application, Infrastructure, Fix.CTrader, Mt5. Fix.CTrader is unused by this class.

---

## 2. File inventory

```1:63:D:\Prop\tests\Integration\SeedingAndStoreTests.cs
public class SeedingAndStoreTests
{
    [Fact]
    public async Task Demo_seed_discovers_groups_reconstructs_and_scores() { ... }

    [Fact]
    public async Task Deal_upsert_is_idempotent() { ... }
}
```

| Item | Value |
|---|---|
| Namespace | `TraderIntelligence.Tests.Integration` (flat; A90 wanted `InMemory/`) |
| Facts | 2 |
| Theories | 0 |
| Traits / collections / fixtures | **none** |
| SUT construction | `new TraderDbContext(UseInMemoryDatabase(Guid.NewGuid()))` per fact |
| Isolation | unique InMemory name per fact — **good** |
| Disposal | `await using var db` — **good** |
| Clock | `DateTimeOffset.UtcNow` in the upsert fact (not asserted) |

Each fact builds `EfTradingStore` directly. Fact 1 also builds `ReconstructionScoringService(store, new TradeReconstructor(), new BaselineScorer())` and calls `DemoSeeder.SeedAsync`. Fact 2 never touches the seeder.

---

## 3. Fact 1 — `Demo_seed_discovers_groups_reconstructs_and_scores`

### 3.1 What the seeder actually writes

`DemoSeeder.SeedAsync` (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`):

1. **Early-return** if `db.Brokers.AnyAsync` — **untested**.
2. Inserts two hardcoded brokers (`aaaaaaaa-…aaa1` Achiever, `…aaa2` StarwaveFX) including **live lab IPs** (`57.128.141.65`, `84.201.6.142`) and manager logins `2027` / `9904`.
3. Inserts one `CanonicalInstrument` `XAUUSD`, two `FixSessionState` rows (QUOTE + TRADE) with host `live-us-eqx-01.p.c-trader.com`, `SenderCompId = live.pepperstone.1369850`, `TargetCompId = cServer`, one `DestinationQuoteSnapshot`, one `KillSwitch`.
4. `DemoBrokerFactory.CreateDefault()` → `BrokerRegistry` → `DealIngestionService.SyncBrokerAsync` for both codes, window **2026-01-01 … 2026-12-31 00:00:00Z**.
5. `RebuildTraderAsync` for logins `10001`, `10002`, `10003` (Achiever) and `99001` (StarwaveFX, `login >= 99000`).

`DemoBrokerFactory` tape (`D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`):

| Broker | Groups | Accounts | Closed XAU round-trips | Deals | Positions |
|---|---:|---:|---:|---:|---:|
| ACHIEVER | 3 (`demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`) | 10001, 10002, 10003 | 3 + 3 + 0 | 12 | 0 |
| STARWAVEFX | 1 (`real\standard`) | 99001 | 3 | 6 | 0 |
| **Total** | **4** | **4** | **9** | **18** | **0** |

Volume scale is `10_000` (`DemoBrokerFactory.Lots` == `VolumeConverter.Manager`). Deal times start `2026-06-01T08:00:00Z`, all inside the seeder window. `GetDealsAsync` filter is inclusive `from`/`to`.

Login 10002 is a 0.10 → 0.20 → 0.40 losing tape (classic martingale / lot-escalation). Login 10001 is three 0.10-lot mixed PnL trades. Login 10003 has **zero** deals.

### 3.2 Assertions vs expected (measured from product, not from the test)

| # | Assertion (as written) | Expected from current SUT + factory | Strength |
|---|---|---|---|
| 1 | `db.Brokers.Should().HaveCount(2)` | Achiever + StarwaveFX | **OK** (does not lock codes / ids) |
| 2 | `db.Mt5Groups.Count() > 2` | **4** | **WEAK** — 3 groups would still pass; names / `contest\yo-2step` unasserted |
| 3 | `db.Mt5Deals.Count() > 0` | **18** | **WEAK** — a single leftover deal passes |
| 4 | `ReconstructedTrades.Any(t => t.Completed && t.CanonicalSymbol == "XAUUSD")` | 9 completed XAU rows | **WEAK** — does not lock count, login, VWAP, PnL, volume |
| 5 | `TraderScores.Single(s => s.Login == 10001).CompletedXauTrades == 3` | 3 | **OK** (login-only `Single`; dual-broker same login would throw) |
| 6 | `…10001.CurrentState.Should().NotBe(TraderState.LIVE)` | actual state is **`SHADOW`** (see §3.3) | **VACUOUS** — `FromBaseline` never emits `LIVE` / `LIVE_CANDIDATE` (`B12`) |
| 7 | `…10002.CurrentState == RISK_BLOCKED` | `RISK_BLOCKED` via martingale ∧ MaxDD>0 ∧ NetPnl<0 | **STRONG** (best assertion in the class) |
| 8 | `FixSessionStates.Should().HaveCount(2)` | Quote + Trade | **OK** (does not lock `Qualifier`) |
| 9 | `TargetCompId.Distinct().Should().Equal("cServer")` | both rows `cServer` | **locks issued-form case** (good vs `CSERVER`) **and** cements demo FIX rows as expected data |

**Not asserted at all (but the seeder writes / scoring runs them):**

| Surface | Expected | Why it matters |
|---|---|---|
| Group names | 3 Achiever + 1 Starwave, including `contest\yo-2step` | B32 / A40: plan mapping is **not** a fetch filter — this tape is the natural lock and the test never names the groups |
| Accounts | 4 (`10001/10002/10003/99001`) | ingestion account loop |
| Deal count | 18 | regression of factory tape |
| Positions | 0 | factory has no open book; `ReplacePositionsAsync` is a no-op delete+insert of empty |
| Login 10003 score | `CompletedXauTrades=0`, `INSUFFICIENT_DATA` | empty contest account is the “no deals” path |
| Login 99001 score | 3 completed XAU, `SHADOW` (same shape as 10001) | only Starwave reconstruction path |
| 10001 exact state | `SHADOW` | `NotBe(LIVE)` will not catch a drift to `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` |
| 10001 / 10002 flags | 10001 `Martingale=false`; 10002 `Martingale=true`, `LotEscalation=true` | scoring features |
| `TraderScoreHistory` | 4 rows (one per rebuild) | `UpsertScoreAsync` always appends history |
| Canonical instrument / kill switch / dest quote | 1 / 1 / 1 | seeder side-effects |
| `OutboxEvents` / `SyncCheckpoints` | **0** | Phase-1 holes; a future writer should fail this fact if someone claims “seed is backfill” |
| Broker **codes** | `ACHIEVER`, `STARWAVEFX` | identity law |
| Second `SeedAsync` | no-op (early-return) | seeder idempotency is untested; API startup + `/api/ops/resync` rely on it |
| `SyncBrokerAsync` return value | 12 + 6 inserted | seeder **discards** the insert counts |

### 3.3 Why 10001 is `SHADOW` (and why `NotBe(LIVE)` is not a gate)

From the factory tape + `TradeReconstructor.ToResult` (`Net = Gross + Commission + Swap + 0 fees`):

| Login 10001 trade | Net (approx) | Lots |
|---|---:|---:|
| 501 long | 153 − 1.2 − 0.4 = **151.4** | 0.10 |
| 502 short | −88 − 1.1 − 0.3 = **−89.4** | 0.10 |
| 503 long | 163 − 1.2 − 0.2 = **161.6** | 0.10 |

`BaselineScorer`: N=3 eligible; no martingale / averaging / lot-escalation; `SlUseRate=0` → risk **10**; Net>0 and high PF → quality ≳ 70; `risk < 40` → `TraderState.SHADOW`.

`TraderStateMachine.FromBaseline` (`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` 189–207) can return only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. `CanPromoteToLive` is hard-`false`. So `NotBe(LIVE)` cannot fail unless someone later adds a LIVE path **and** the seed tape qualifies. That is the same vacuous gate `B12` already called out. Unit `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` is the place that should lock SHADOW; this integration fact should pin **`== SHADOW`** if it wants to lock the demo book.

### 3.4 Reconstruction edges this fact cannot see

The factory only emits simple In/Out round-trips (`ClosedRoundTrip`). No scale-in, no partial close, no reversal, no `DealEntry.InOut`, no balance/credit deals, no open leftover, no non-XAU symbol, no alias (`XAUUSDm` / `GOLD`). A green seed run is **not** a reconstruction suite. Averaging-down polarity (fixed in current `OpenTrade.ScaleIn` to long-add-below-VWAP) is **untested here**.

### 3.5 Live identifiers (safety)

Fact 1 does not open a socket. It **does** persist and then treat as expected:

- FIX host `live-us-eqx-01.p.c-trader.com` ports 5211 / 5212
- `SenderCompId = live.pepperstone.1369850`
- `TargetCompId = cServer` (**asserted**)

Architecture §61: do not use the real account as the first integration test. A90 §1.5: do not call `DemoSeeder` from this suite; seed **synthetic** hosts (`test.invalid`) and manager login `0`. Current fact 1 **cements the live-looking rows**. That is not a live send, but it is a live-identifier fixture. `C09` already notes the `cServer` assert is consistent with issued-form case — keep the case lock, drop the live host/sender from anything this suite depends on.

API `Program.cs` calls the same seeder on startup (`EnsureCreated` + `SeedAsync`) and `/api/ops/resync` repeats the four-login rebuild. This test is the only automated lock of that path, which is why it is still worth keeping — as a **labeled InMemory smoke**, not as §60.

---

## 4. Fact 2 — `Deal_upsert_is_idempotent`

### 4.1 What it does

1. New InMemory `TraderDbContext`.
2. Inserts `Broker` id `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1`, code `BrokerCodes.Achiever`.
3. Builds `Mt5DealDto(ticket=1, login=1, order=1, pos=1, "XAUUSD", Buy, In, vol=1000, price=1, zeros, Time=UtcNow)`.
4. `ResolveBrokerIdAsync("ACHIEVER")`.
5. First `UpsertDealAsync` → `true`; second same DTO → `false`; `db.Mt5Deals.Should().HaveCount(1)`.

### 4.2 What `EfTradingStore.UpsertDealAsync` actually is

```85:114:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task<bool> UpsertDealAsync(...)
    {
        var exists = await _db.Mt5Deals.AnyAsync(
            d => d.BrokerId == brokerId && d.DealTicket == deal.DealTicket, ct);
        if (exists)
            return false;
        _db.Mt5Deals.Add(...);
        await _db.SaveChangesAsync(ct);
        return true;
    }
```

Fluent model unique: `(BrokerId, DealTicket)` (`TraderDbContext` lines 58–63). **InMemory does not enforce that unique index.**

A78 required law: `INSERT … ON CONFLICT (broker_id, deal_ticket) DO NOTHING`, first payload wins, four-way outcome, `mt5_duplicate_deals_total`. Current port is a **bool** (`true` = inserted). The test matches the bool adapter and **stops there**.

### 4.3 Covered vs missing

| Case | Status |
|---|---|
| Same `(broker, ticket)` second call returns `false` | **COVERED** |
| Row count stays 1 on exact replay | **COVERED** (single-threaded) |
| Unknown broker `ResolveBrokerIdAsync` throws `InvalidOperationException` | **MISSING** |
| Same ticket, **different** broker → two rows (§10) | **MISSING** (identity law) |
| Same ticket, different **login** on same broker | **MISSING** — store keys ticket only; second login would be dropped. That may be correct (ticket unique per broker) but it is unstated |
| Second delivery with **different price/profit** (first-write-wins, no overwrite) | **MISSING** — both calls use the identical DTO |
| Concurrent double-insert (TOCTOU: two `AnyAsync==false`) | **MISSING** — InMemory would likely accept two rows; Postgres unique would throw. This is the whole reason A90 forbids InMemory as proof |
| `ON CONFLICT DO NOTHING` vs catch-unique | **MISSING** — no SQL |
| Group / account upsert idempotency | **MISSING** — `UpsertGroupAsync` / `UpsertAccountAsync` update-in-place, never tested |
| Position replace is full replace | **MISSING** |
| Load order `DealTime, DealTicket` | **MISSING** |
| Score upsert updates in place + appends history | **MISSING** |
| `ingestion_events` / metrics | **MISSING** (product types absent) |

`VolumeNative = 1000` is `0.10` lots at Manager scale. The fact never converts or reconstructs, so it does not lock the scale.

Broker id reused from the seeder constant is fine (separate InMemory database).

---

## 5. What this class is **not** (do not greenwash)

| §60 / A90 class | This file |
|---|---|
| `PostgresMigrationTests` | No |
| `UniqueIndexContractTests` | No (InMemory) |
| `Mt5BackfillRestartTests` | No checkpoint read/write; full-window pull only |
| `Mt5DualBrokerIsolationTests` | Seed hits two brokers; no colliding ticket assert |
| `Mt5GroupDiscoveryIdempotencyTests` | Groups `> 2` only |
| `OutboxProcessingTests` | Store never writes outbox |
| `InMemoryDealIngestionOrchestrationTests` (A90 §8, labeled) | This is the closest thing, **unlabeled** |

`DealIngestionService` group discovery is unfiltered (`GetGroupsAsync()` then `GetAccountsAsync(null)`) — B32 PASS on the product. This test could lock that by asserting `contest\yo-2step` is present; it does not.

---

## 6. Wiring / style defects (concrete)

1. **Wrong seam for the project name.** Integration + InMemory + `DemoSeeder` is an Application orchestration smoke. Either move to `tests/Unit` or keep here with A90 traits and a comment that it does not count.
2. **Weak numeric bounds** (`> 2`, `> 0`, `Any`). Gold-file the factory: 2 brokers, 4 groups, 4 accounts, 18 deals, 9 completed XAU, 4 scores.
3. **`NotBe(LIVE)`** does not lock 10001. Use `Be(TraderState.SHADOW)`.
4. **Login-only `Single`**. Should be `(BrokerId, Login)` once broker ids are resolved.
5. **No re-seed fact.** `if (await db.Brokers.AnyAsync) return;` is the production startup guard. Untested. A second `SeedAsync` on the same ctx must not duplicate deals (InMemory smoke) *and* a Postgres unique test must still exist separately.
6. **Seeder swallows insert counts.** Test cannot see `SyncBrokerAsync` return 12 / 6 without calling ingestion itself.
7. **Live FIX / manager identifiers** in the SUT the test treats as fixture.
8. **No cancellation / unknown-broker / date-window miss** cases. Window upper bound is `2026-12-31T00:00:00Z` (not end-of-day). Factory deals are in June so they pass; a 31-Dec deal after midnight would be dropped — untested.
9. **Unused Fix.CTrader project reference** on the test csproj (B08). A FIX compile break takes this class down for no reason.
10. **`UnitTest1.cs` still in the assembly.** Delete when the next real class lands.

---

## 7. Recommended next facts (do not add empty classes; product not edited)

Keep this file as labeled InMemory smoke. Then:

| Priority | Fact | Seam | Why |
|---|---|---|---|
| 1 | Tighten fact 1: exact 4 groups (name the contest path), 18 deals, 9 completed XAU, 10001 `SHADOW` + `Martingale==false`, 10002 flags, 10003 `INSUFFICIENT_DATA`, 99001 `CompletedXauTrades==3`, history count 4, outbox/checkpoints 0 | InMemory | Locks the tape this class already runs |
| 2 | `SeedAsync` second call is a no-op (broker count / deal count unchanged) | InMemory | Production startup |
| 3 | Same ticket, two brokers → two rows; same ticket replay → one row + `false` | **Postgres** | A78 / §10 |
| 4 | First-write-wins: second upsert with mutated price does not change the stored row | Postgres | A78 forbidden `DO UPDATE` |
| 5 | Concurrent upsert does not create two rows (unique index, not `AnyAsync`) | Postgres | TOCTOU |
| 6 | Stop calling `DemoSeeder` once a synthetic fixture exists (`test.invalid`, login 0) | both | §61 / A90 |

Do **not** invent `OutboxProcessingTests` or `Mt5BackfillRestartTests` until the store writes those tables.

---

## 8. Disposition

| Metric | Value |
|---|---|
| Facts in class | 2 |
| Measured this review | **2 passed**, 0 failed, 0 skipped |
| Assertions that lock real demo behavior | **2** (10001 completed-XAU count; 10002 `RISK_BLOCKED`) |
| Vacuous / weak assertions | 10001 `NotBe(LIVE)`; groups `> 2`; deals `> 0`; `Any` completed XAU |
| §60 integration areas proven | **0 / 8** |
| Unique-index proof | **None** (InMemory + `AnyAsync`) |
| Checkpoints / outbox / positions / 10003 / 99001 | **unasserted** |
| Live identifiers persisted by the SUT under test | **Yes** (FIX host + SenderCompId) |
| Product source changed by C16 | **No** |

**Do not treat the Integration green run as Phase-1 or §68 exit.** Treat this class as an unlabeled InMemory orchestration smoke that currently locks two useful demo outcomes and misses the identity/idempotency law it is named after.
