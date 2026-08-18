# D37 — `SeedingAndStoreTests` integration recensus (InMemory smoke ≠ §60)

| Field | Value |
|---|---|
| Agent | D37 (senior engineer, integration-test recensus only) |
| Date | 2026-08-18 13:38 +05:30 |
| Assigned | Read `SeedingAndStoreTests.cs`. Write this report. Do not modify product source. |
| Primary file | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |
| Bytes / physical lines / non-blank | **3119** / **63** / **58** |
| SHA-256 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` |
| LastWriteTime | 2026-08-18 13:17:42 +05:30 |
| Product source modified | **No.** This report is the only product-tree write. |
| Test source modified | **No.** |
| HEAD | `398a142` (`docs: add PNG fallback and update README; add conversion script`) |
| Git of the test file | **untracked** (`?? tests/Integration/SeedingAndStoreTests.cs`) |

**Path note:** the file lives under repo root `D:\Prop\tests\Integration\`, **not** under `D:\Prop\src\`. There is no copy in `src\`.

Companions (do not treat as current measured SUT unless noted):

| File | Relation to this recensus |
|---|---|
| `C16_seed_test_review.md` | Same test SHA. **SUT has drifted.** C16 is still correct about assertion weakness and InMemory ≠ §60. |
| `D20_store.md` | Store hash stale — `EfTradingStore` now includes `PersistDemoShadowAsync` (12097 B / SHA `DC03BBE6…555C36`). |
| `D22_seeder.md` | **Stale on FIX status.** Seeder now writes `Disconnected` + `LastError`, not `LoggedOn` / `ReadyForMarketData`. |
| `A10_integration_tests_audit.md` | Stale empty-suite snapshot (1 empty fact). |
| `A90_integration_class_list.md` | Binding seam law: InMemory may host, **never** counts as §60. Do not call `DemoSeeder` from this suite. |
| `A27_test_inventory.md` | Named §60 integration classes. This file is **none** of them. |
| `A78_deal_idempotency.md` | Persist-time law. Fact 2 is a bool adapter smoke, not `ON CONFLICT`. |
| `B08_tests_gap.md` | Earlier Integration 3/3 including placeholder. |

**Honesty rule:** a green `--no-build` run of a stale `Integration.dll` is not a green compile of the current worktree. `AnyAsync` + `Add` on EF InMemory is not PostgreSQL unique-index proof. `PersistDemoShadowAsync` writing four `outbox_events` is not §60 outbox processing.

---

## 0. Verdict

**PARTIAL InMemory orchestration smoke. Test file unchanged since C16. Does not count as Architecture §60 integration. Current worktree cannot rebuild the suite.**

The class is a real xUnit surface (not `Assert.True(true)`). Two facts exercise:

```text
DemoSeeder.SeedAsync
  → DealIngestionService.SyncBrokerAsync (FakeMt5BrokerConnector)
  → EfTradingStore upserts
  → ReconstructionScoringService.RebuildTraderAsync
  → TradeReconstructor + BaselineScorer
  → EfTradingStore.PersistDemoShadowAsync   ← new since C16; unasserted
```

and a sequential same-context deal replay.

| Gate | Classification | Why |
|---|---|---|
| Test file vs C16 | **UNCHANGED** | identical SHA-256 `2BB1EE24…2EFD` |
| Fresh `dotnet test` (rebuild) | **RED** | `TradeReconstructor.cs(50,41): error CS8858` — `with` on a **class** `ReconstructedTradeResult`. Unrelated to this test file. |
| Last compiled binaries `--no-build` | **3 / 3 PASS** | 2 class facts + `PlaceholderRemoved` (`Assert.True(true)`) |
| Architecture §60 integration (8 areas) | **0 / 8** | InMemory; no migrations, checkpoints, outbox processor, FIX, reconcile |
| A90 “InMemory may host, never counts” | **violates labeling** | no `[Trait("Seam","InMemory")]` / `CountsAs60=false` |
| A90 “do not call `DemoSeeder` from this suite” | **violated** | fact 1 *is* `DemoSeeder.SeedAsync` |
| A78 unique `(broker_id, deal_ticket)` | **PARTIAL (app-level only)** | `AnyAsync` then `Add`; unique index never exercised |
| “10001 cannot be LIVE” | **VACUOUS** | `TraderStateMachine.FromBaseline` never returns `LIVE` |
| “10002 is RISK_BLOCKED” | **COVERED** | measured `RISK_BLOCKED` (martingale ∧ MaxDD>0 ∧ NetPnl<0) |
| Seeder FIX honesty (`Disconnected`) | **UNASSERTED** | seeder was fixed after C16/D22; this class still only locks `cServer` + count 2 |
| Live identifiers in fixture | **FAIL (safety)** | seed still writes `live-us-eqx-01.p.c-trader.com` + `live.pepperstone.1369850` |
| Demo shadow / outbox side-effects | **UNASSERTED** | measured 4 outbox + 6 copy intents + 6 shadow orders after seed |

**One-line:** useful developer smoke for the fake-broker seed path; **not** Phase-1 proof; `NotBe(LIVE)` and `Count() > 0` are too weak to lock the tape this class already runs.

---

## 1. What was read / measured (no product edits)

| Path | Role | Measured |
|---|---|---|
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | subject | 3119 B, SHA-256 `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` |
| `D:\Prop\tests\Integration\UnitTest1.cs` | sibling placeholder | 162 B, SHA-256 `49671A3C7C367ED87C7711E2204865AA2ABB8A7A5783AD785CD66A1F6DA7F4D6` |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | seam | 1328 B, SHA-256 `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | fact-1 SUT | 5082 B / 140 physical / SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | store | 12097 B / SHA-256 `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | ingest + rebuild | SHA-256 `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | factory tape | SHA-256 `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | fluent uniques | SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (same as D19) |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `FromBaseline` | never emits `LIVE` / `LIVE_CANDIDATE` |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | current disk | `with { EligibleForFirstThree = false }` at line 50 — **does not compile** |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | current disk | `public sealed class` (not a record) + `EligibleForFirstThree` |
| Architecture §60 / §61 | law | 8 integration areas + 7 harness capabilities |
| Throwaway eval | measured tape | `D:\Prop\reports\swarm\20260818\_tmp_d37_eval\` (references last built DLLs; not product) |

Grep of the test file: **0** `[Trait]`, **0** `[Collection]`, **0** Testcontainers, **0** `UseNpgsql`, **0** `Migrate`, **0** checkpoint / outbox / shadow / `Disconnected` assertions.

---

## 2. Measured runs (this review)

### 2.1 Fresh rebuild — FAIL (Domain CS8858)

Command (read-only; no product edit):

```text
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj --nologo --verbosity normal
```

Result (2026-08-18 13:37:18 +05:30, net8.0):

```text
D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs(50,41): error CS8858:
  The receiver type 'ReconstructedTradeResult' is not a valid record type and is not a struct type.

0 Warning(s)
1 Error(s)
Time Elapsed 00:00:01.10
```

Cause (current worktree, **not** this test file):

```50:50:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
                rows = rows.Select(r => r with { EligibleForFirstThree = false }).ToList();
```

```6:6:D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs
public sealed class ReconstructedTradeResult
```

`TradeReconstructor.cs` LastWriteTime **13:37:18**; `ReconstructedTradeResult.cs` **13:37:32** — mid-wave edit after the last successful Integration compile. This agent did **not** introduce or fix it.

**Do not cite a green Integration rebuild from this recensus.** The suite is currently unbuildable from source.

### 2.2 Last compiled binaries — 3 / 3 PASS (stale Domain.dll)

Last successful compile timestamps:

| Artifact | LastWriteTime |
|---|---|
| `tests/Integration/bin/Debug/net8.0/TraderIntelligence.Domain.dll` | 13:34:53 |
| `DemoSeeder.cs` / `DealIngestionService.cs` / `EfTradingStore.cs` | 13:34:59 / 13:35:29 / 13:35:59 |
| `TraderIntelligence.Tests.Integration.dll` | 13:36:21 |
| `TradeReconstructor.cs` (`with` break) | 13:37:18 |

So `--no-build` exercises **current seeder + current store + current ingestion** against a Domain.dll from **13:34:53** (before the `with` expression).

```text
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj --nologo --no-build --verbosity normal
```

```text
Passed  PlaceholderRemoved.Integration_project_loads                              [2 ms]
Passed  SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores   [572 ms]
Passed  SeedingAndStoreTests.Deal_upsert_is_idempotent                            [18 ms]
Total tests: 3  Passed: 3  Failed: 0  Skipped: 0
Total time: 0.9573 Seconds
```

Do **not** count `PlaceholderRemoved` toward this class. It is **FALSE_GREEN**:

```1:6:D:\Prop\tests\Integration\UnitTest1.cs
namespace TraderIntelligence.Tests.Integration;

public class PlaceholderRemoved
{
    [Fact]
    public void Integration_project_loads() => Assert.True(true);
}
```

Class-only score on stale binaries: **2 / 2 PASS**.

### 2.3 Seed gold-file dump (stale binaries, same SUT as §2.2)

Throwaway `D:\Prop\reports\swarm\20260818\_tmp_d37_eval\` referenced the Integration `bin\Debug` DLLs (no Domain rebuild). Full dump: `_tmp_d37_eval\stdout.txt`.

| Table / metric | Measured after `SeedAsync` | Asserted by this class? |
|---|---:|---|
| `brokers` | **2** | count only (`HaveCount(2)`) |
| `mt5_groups` | **4** | `> 2` only |
| `mt5_accounts` | **4** | **no** |
| `mt5_deals` | **18** | `> 0` only |
| `mt5_positions_current` | **0** | **no** |
| `reconstructed_trades` | **9** (all completed XAU) | `Any(Completed && XAUUSD)` |
| `trader_scores` | **4** | 10001 XAU count + `NotBe(LIVE)`; 10002 `RISK_BLOCKED` |
| `trader_score_history` | **4** | **no** |
| `fix_sessions` | **2** | count + `TargetCompId == cServer` |
| `destination_quotes` | **1** | **no** |
| `kill_switches` | **1** | **no** |
| `canonical_instruments` | **1** | **no** |
| `outbox_events` | **4** (`ScoreUpdate`, one per rebuilt login) | **no** |
| `copy_intents` | **6** (`SHADOW_ONLY`, 10001×3 + 99001×3) | **no** |
| `shadow_orders` | **6** | **no** |
| `sync_checkpoints` | **0** | **no** |
| `source_symbol_mappings` | **0** | **no** |
| `audit_logs` / `risk_decisions` / `execution_intents` | **0** | **no** |
| Second `SeedAsync` | deals 18, scores 4, outbox 4 (early-return) | **no** |

Per-login scores (measured):

| Login | Broker | State | CompletedXau | Martingale | AveragingDown | LotEscalation | Risk | Quality |
|---:|---|---|---:|---|---|---|---:|---:|
| 10001 | ACHIEVER | **SHADOW** | 3 | false | false | false | 10 | 95.50 |
| 10002 | ACHIEVER | **RISK_BLOCKED** | 3 | true | false | true | 70 | 42.50 |
| 10003 | ACHIEVER | **INSUFFICIENT_DATA** | 0 | false | false | false | 10 | 40 |
| 99001 | STARWAVEFX | **SHADOW** | 3 | false | false | false | 10 | 95.50 |

FIX rows (measured):

| Qualifier | Status | Host | Port | SenderCompId | TargetCompId | LastError |
|---|---|---|---:|---|---|---|
| Quote | **Disconnected** | `live-us-eqx-01.p.c-trader.com` | 5211 | `live.pepperstone.1369850` | `cServer` | `No live QUOTE socket. Demo seed only.` |
| Trade | **Disconnected** | `live-us-eqx-01.p.c-trader.com` | 5212 | `live.pepperstone.1369850` | `cServer` | `No live TRADE socket. NewOrderSingle off.` |

D22’s “FORGED `LoggedOn` / `ReadyForMarketData`” is **no longer true of the seeder**. The honesty fix landed in `DemoSeeder.cs` (SHA `A6416491…`). This test class **did not move** and therefore **does not lock** `Status == Disconnected`. A regression that re-forges `LoggedOn` stays green.

---

## 3. File inventory

```13:63:D:\Prop\tests\Integration\SeedingAndStoreTests.cs
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
| Facts | **2** |
| Theories | 0 |
| Traits / collections / fixtures | **none** |
| SUT construction | `new TraderDbContext(UseInMemoryDatabase(Guid.NewGuid()))` per fact |
| Isolation | unique InMemory name per fact — **good** |
| Disposal | `await using var db` — **good** |
| Clock | `DateTimeOffset.UtcNow` in the upsert fact (not asserted) |

Project seam (`TraderIntelligence.Tests.Integration.csproj`):

| Package / reference | Present? | Counts as §60? |
|---|---|---|
| xUnit 2.5.3 + FluentAssertions 6.12.0 | yes | n/a |
| `Microsoft.EntityFrameworkCore.InMemory` 8.0.4 | **yes** | **No** (A90 §1.1) |
| Testcontainers / Testcontainers.PostgreSQL | **no** | required for the three DB bullets |
| Respawn | **no** | |
| Npgsql (direct test use) | **no** | |
| Domain / Application / Infrastructure / Mt5 | yes | |
| Fix.CTrader | **yes, unused** by this class | compile-couples Integration to FIX |

A90 wanted `[Trait("Category","Integration")] [Trait("Seam","InMemory")] [Trait("CountsAs60","false")]`. None present.

---

## 4. Fact 1 — `Demo_seed_discovers_groups_reconstructs_and_scores`

### 4.1 What the seeder actually writes (current disk)

`DemoSeeder.SeedAsync` (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`, SHA `A6416491…`):

1. **Early-return** if `db.Brokers.AnyAsync` — **untested by a dedicated fact**; eval confirms second call is a no-op.
2. Inserts two hardcoded brokers (`aaaaaaaa-…aaa1` Achiever, `…aaa2` StarwaveFX) including **live lab IPs** (`57.128.141.65`, `84.201.6.142`) and manager logins `2027` / `9904`.
3. Inserts one `CanonicalInstrument` `XAUUSD`.
4. Inserts two `FixSessionState` rows (QUOTE + TRADE) with host `live-us-eqx-01.p.c-trader.com`, `SenderCompId = live.pepperstone.1369850`, `TargetCompId = cServer`, **`Status = Disconnected`**, honest `LastError` strings, seq 1/1, timestamps = seed `now`.
5. One `DestinationQuoteSnapshot` (`2399.45` / `2399.85`, `VenueInstrumentId = null`).
6. One `KillSwitch` `Mode=None`.
7. `DemoBrokerFactory.CreateDefault()` → **new** `BrokerRegistry` → `DealIngestionService.SyncBrokerAsync` for both codes, window **2026-01-01 … 2026-12-31 00:00:00Z**.
8. `RebuildTraderAsync` for logins `10001`, `10002`, `10003` (Achiever) and `99001` (StarwaveFX, `login >= 99000`).
9. Each rebuild now calls `PersistDemoShadowAsync` (outbox always; copy+shadow only when state is `SHADOW` and a dest quote exists).

`DemoBrokerFactory` tape (`FakeMt5BrokerConnector.cs`):

| Broker | Groups | Accounts | Closed XAU round-trips | Deals | Positions |
|---|---:|---|---:|---:|---:|
| ACHIEVER | 3 (`demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`) | 10001, 10002, 10003 | 3 + 3 + 0 | 12 | 0 |
| STARWAVEFX | 1 (`real\standard`) | 99001 | 3 | 6 | 0 |
| **Total** | **4** | **4** | **9** | **18** | **0** |

Volume scale is `10_000` (`DemoBrokerFactory.Lots` == `VolumeConverter.Manager`). Deal times start `2026-06-01T08:00:00Z`, all inside the seeder window. `GetDealsAsync` filter is inclusive `from`/`to`.

Login 10002 is a 0.10 → 0.20 → 0.40 losing tape (martingale / lot-escalation). Login 10001 is three 0.10-lot mixed PnL trades. Login 10003 has **zero** deals. Login 99001 is three 0.05-lot winners.

### 4.2 Assertions vs measured SUT

| # | Assertion (as written) | Measured from current compiled SUT | Strength |
|---|---|---|---|
| 1 | `db.Brokers.Should().HaveCount(2)` | Achiever + StarwaveFX | **OK** (does not lock codes / ids / IPs) |
| 2 | `db.Mt5Groups.Count() > 2` | **4** | **WEAK** — 3 groups still pass; `contest\yo-2step` unasserted |
| 3 | `db.Mt5Deals.Count() > 0` | **18** | **WEAK** — a single leftover deal passes |
| 4 | `ReconstructedTrades.Any(t => t.Completed && t.CanonicalSymbol == "XAUUSD")` | **9** completed XAU | **WEAK** — does not lock count, login, VWAP, PnL |
| 5 | `TraderScores.Single(s => s.Login == 10001).CompletedXauTrades == 3` | 3 | **OK** (login-only `Single`; dual-broker same login would throw) |
| 6 | `…10001.CurrentState.Should().NotBe(TraderState.LIVE)` | actual **`SHADOW`** | **VACUOUS** — `FromBaseline` never emits `LIVE` / `LIVE_CANDIDATE` |
| 7 | `…10002.CurrentState == RISK_BLOCKED` | `RISK_BLOCKED` | **STRONG** (best assertion in the class) |
| 8 | `FixSessionStates.Should().HaveCount(2)` | Quote + Trade | **OK** (does not lock `Qualifier` or `Status`) |
| 9 | `TargetCompId.Distinct().Should().Equal("cServer")` | both rows `cServer` | **locks issued-form case** (good vs `CSERVER`) **and** cements demo FIX rows as expected data |

**Not asserted at all (but the seeder / rebuild now writes them):**

| Surface | Measured | Why it matters |
|---|---|---|
| Group names | 3 Achiever + 1 Starwave, including `contest\yo-2step` | B32 / A40: plan mapping is **not** a fetch filter |
| Accounts | 4 (`10001/10002/10003/99001`) | ingestion account loop |
| Deal count | 18 | factory-tape regression |
| Positions | 0 | factory has no open book |
| Login 10003 | `CompletedXauTrades=0`, `INSUFFICIENT_DATA` | empty contest account |
| Login 99001 | 3 completed XAU, `SHADOW` | only Starwave reconstruction path |
| 10001 exact state | `SHADOW` | `NotBe(LIVE)` will not catch drift to `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` |
| 10001 / 10002 flags | 10001 all false; 10002 `Martingale=true`, `LotEscalation=true` | scoring features |
| `TraderScoreHistory` | 4 | `UpsertScoreAsync` always appends |
| FIX `Status` | both **`Disconnected`** | honesty lock after the D22 seeder fix |
| FIX `LastError` | quote/trade “No live … socket” | honesty lock |
| Canonical / kill / dest quote | 1 / 1 / 1 | seeder side-effects |
| `OutboxEvents` | **4** `ScoreUpdate` | `PersistDemoShadowAsync` — **not** A41 transactional outbox |
| `CopyIntents` / `ShadowOrders` | **6 / 6** | demo shadow only for SHADOW logins |
| `SyncCheckpoints` | **0** | Phase-1 hole; a “seed is backfill” claim should fail here |
| Broker codes | `ACHIEVER`, `STARWAVEFX` | identity law |
| Second `SeedAsync` | no-op | production startup + `/api/ops/resync` rely on it |
| `SyncBrokerAsync` return | 12 + 6 inserted | seeder **discards** the insert counts |

### 4.3 Why 10001 is `SHADOW` (and why `NotBe(LIVE)` is not a gate)

Factory tape + reconstructor `Net = Gross + Commission + Swap + 0 fees`:

| Login 10001 trade | Net (approx) | Lots |
|---|---:|---:|
| 501 long | 153 − 1.2 − 0.4 = **151.4** | 0.10 |
| 502 short | −88 − 1.1 − 0.3 = **−89.4** | 0.10 |
| 503 long | 163 − 1.2 − 0.2 = **161.6** | 0.10 |

`BaselineScorer`: N=3 eligible; no martingale / averaging / lot-escalation; `SlUseRate=0` → risk **10**; Net>0 and high PF → quality **95.50**; `risk < 40` → `TraderState.SHADOW`. Matches eval.

`TraderStateMachine.FromBaseline` (`BaselineScorer.cs` 189–207) can return only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. `CanPromoteToLive` is hard-`false`. `LIVE` and `LIVE_CANDIDATE` exist on the enum (values 4 and 5) but this path never writes them. So `NotBe(LIVE)` cannot fail unless someone later adds a LIVE path **and** the seed tape qualifies.

Unit `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` is the place that should lock SHADOW. This integration fact should pin **`== SHADOW`** if it wants to lock the demo book.

10002: 0.10/0.20/0.40 losers → `Martingale=true`, `LotEscalation=true`, risk **70**, quality **42.50**, `RISK_BLOCKED`. That assertion is the only one that would catch a scorer/tape regression.

10003 empty: scorer still assigns `RiskScore=10` because `SlUseRate=0 < 0.3` on a zero-trade snapshot, then `FromBaseline` short-circuits on `CompletedXauTrades==0`. Unasserted.

### 4.4 Reconstruction edges this fact cannot see

The factory only emits simple In/Out round-trips (`ClosedRoundTrip`). No scale-in, no partial close, no reversal, no `DealEntry.InOut`, no balance/credit deals, no open leftover, no canceled deals, no non-XAU symbol, no alias (`XAUUSDm` / `GOLD`). A green seed run is **not** a reconstruction suite. Averaging-down polarity is **untested here**.

The in-flight `EligibleForFirstThree` work (canceled-deal dirty set) is also untested here — and currently **does not compile**.

### 4.5 Live identifiers (safety)

Fact 1 does not open a socket. It **does** persist and then treat as expected:

- FIX host `live-us-eqx-01.p.c-trader.com` ports 5211 / 5212
- `SenderCompId = live.pepperstone.1369850`
- `TargetCompId = cServer` (**asserted**)
- Achiever `57.128.141.65` manager `2027`; Starwave `84.201.6.142` manager `9904`

Architecture §61: do not use the real account as the first integration test. A90 §1.5: do not call `DemoSeeder` from this suite; seed **synthetic** hosts (`test.invalid`) and manager login `0`. Current fact 1 **cements the live-looking rows**. Status is now honestly `Disconnected`; identifiers are still live.

API / both workers still call the same seeder on empty-broker boot. This test is the only automated lock of that path — keep it as a **labeled InMemory smoke**, not as §60.

### 4.6 Demo shadow is not outbox processing

`ReconstructionScoringService.RebuildTraderAsync` now ends with `PersistDemoShadowAsync`. The store:

- always inserts an `OutboxEvent` `ScoreUpdate` with a hand-built JSON fragment;
- returns after that `SaveChanges` when state ≠ `SHADOW`;
- otherwise walks completed XAU trades, skips existing `shadow:{brokerId}:{login}:{positionId}` keys, writes `CopyIntent` `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry`.

Measured: 4 outbox, 6 intents, 6 shadow fills. **No** processor, **no** `SKIP LOCKED`, **no** same-TX as deal insert, **no** `DedupeKey` unique, `ExpiresAt = OpenedAt + 15s` (already expired vs seed clock on 2026-06-01 tapes). A green fact 1 is **not** `OutboxProcessingTests`.

---

## 5. Fact 2 — `Deal_upsert_is_idempotent`

### 5.1 What it does

1. New InMemory `TraderDbContext`.
2. Inserts `Broker` id `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1`, code `BrokerCodes.Achiever`.
3. Builds `Mt5DealDto(ticket=1, login=1, order=1, pos=1, "XAUUSD", Buy, In, vol=1000, price=1, zeros, Time=UtcNow)`.
4. `ResolveBrokerIdAsync("ACHIEVER")`.
5. First `UpsertDealAsync` → `true`; second same DTO → `false`; `db.Mt5Deals.Should().HaveCount(1)`.

### 5.2 What `EfTradingStore.UpsertDealAsync` actually is

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

### 5.3 Covered vs missing

| Case | Status |
|---|---|
| Same `(broker, ticket)` second call returns `false` | **COVERED** |
| Row count stays 1 on exact replay | **COVERED** (single-threaded, same context) |
| Unknown broker `ResolveBrokerIdAsync` throws | **MISSING** |
| Same ticket, **different** broker → two rows (§10) | **MISSING** |
| Same ticket, different **login** on same broker | **MISSING** — store keys ticket only |
| Second delivery with **different price/profit** (first-write-wins) | **MISSING** — both calls use the identical DTO |
| Concurrent double-insert (TOCTOU: two `AnyAsync==false`) | **MISSING** — InMemory would likely accept two rows; Postgres unique would throw |
| `ON CONFLICT DO NOTHING` vs catch-unique | **MISSING** — no SQL |
| Group / account upsert idempotency | **MISSING** |
| Position replace is full replace | **MISSING** |
| Load order `DealTime, DealTicket` | **MISSING** |
| Score upsert updates in place + appends history | **MISSING** |
| `PersistDemoShadowAsync` key skip | **MISSING** |
| `ingestion_events` / metrics | **MISSING** (product types absent) |

`VolumeNative = 1000` is `0.10` lots at Manager scale. The fact never converts or reconstructs.

Broker id reused from the seeder constant is fine (separate InMemory database).

---

## 6. What this class is **not** (do not greenwash)

Architecture §60 integration required list (verbatim):

```text
PostgreSQL migrations
MT5 backfill/restart
outbox processing
QuickFIX/n session configuration
FIX message parse/build
ExecutionReport handling
position reconciliation
unknown-execution recovery
```

| §60 / A27 / A90 class | This file |
|---|---|
| `Persistence.PostgreSqlMigrationTests` | No |
| `Persistence.CoreSchemaContractTests` | No |
| `UniqueIndexContractTests` | No (InMemory) |
| `Mt5.Mt5BackfillRestartTests` | No checkpoint read/write; full-window pull only |
| `Mt5.Mt5LiveIngestIdempotencyTests` | No |
| `Mt5.DualBrokerIsolationTests` | Seed hits two brokers; no colliding-ticket assert |
| `Mt5GroupDiscoveryIdempotencyTests` | Groups `> 2` only |
| `Outbox.OutboxProcessingTests` | Four demo `ScoreUpdate` rows; no processor |
| `Fix.QuickFixnSessionConfigurationTests` | No |
| `Fix.FixMessageParseBuildIntegrationTests` | No |
| `Fix.ExecutionReportHandlingTests` | No |
| `Reconcile.PositionReconciliationTests` | No |
| `Reconcile.UnknownExecutionRecoveryTests` | No |
| `InMemoryDealIngestionOrchestrationTests` (A90 §8, labeled) | This is the closest thing, **unlabeled** |
| §61 FIX harness (7 capabilities) | **0 / 7** |

`tests/Replay` and `tests/Fix` projects are still **absent**.

`DealIngestionService` group discovery is unfiltered (`GetGroupsAsync()` then `GetAccountsAsync(null)`) — B32 PASS on the product. This test could lock that by asserting `contest\yo-2step` is present; it does not.

---

## 7. Drift since C16 (same test SHA)

| Surface | C16 snapshot | D37 re-measure |
|---|---|---|
| Test SHA-256 | `2BB1EE24…2EFD` | **identical** |
| Seeder FIX status | `ReadyForMarketData` / `LoggedOn` (D22) | **`Disconnected` / `Disconnected`** + `LastError` |
| Seeder SHA | D22 `139D8F87…0BEF` | `A6416491…1FE20` |
| Store | 8 methods, no shadow persist (D20 `05103CE5…`) | 9 methods; `PersistDemoShadowAsync` present |
| Rebuild | score only | score + demo outbox/shadow |
| Outbox after seed | 0 (C16 expected) | **4** (unasserted) |
| Copy / shadow after seed | 0 | **6 / 6** (unasserted) |
| Fresh compile | C16 2/2 PASS | **CS8858** in Domain |
| Stale-bin run | 2/2 | 2/2 + placeholder |

C16’s assertion-strength table is still binding. C16’s “outbox/checkpoints = 0” gold-file is **stale for outbox**. D22’s forged-status finding is **stale for the seeder**, still **valid as a warning that this test never locked status**.

---

## 8. Wiring / style defects (concrete)

1. **Wrong seam for the project name.** Integration + InMemory + `DemoSeeder` is an Application orchestration smoke. Either move to `tests/Unit` or keep here with A90 traits and a comment that it does not count.
2. **Weak numeric bounds** (`> 2`, `> 0`, `Any`). Gold-file the factory (measured §2.3): 2 brokers, 4 groups, 4 accounts, 18 deals, 9 completed XAU, 4 scores.
3. **`NotBe(LIVE)`** does not lock 10001. Use `Be(TraderState.SHADOW)`.
4. **Login-only `Single`**. Should be `(BrokerId, Login)` once broker ids are resolved.
5. **No re-seed fact.** `if (await db.Brokers.AnyAsync) return;` is the production startup guard. Eval proves it; the class does not.
6. **Seeder swallows insert counts.** Test cannot see `SyncBrokerAsync` return 12 / 6 without calling ingestion itself.
7. **Live FIX / manager identifiers** in the SUT the test treats as fixture.
8. **Missed honesty lock.** After the seeder started writing `Disconnected`, this class should have asserted status + `LastError`. It still accepts `LoggedOn`.
9. **Missed shadow lock.** After `PersistDemoShadowAsync` landed, outbox/copy/shadow counts are free proof and unasserted.
10. **No cancellation / unknown-broker / date-window miss** cases. Window upper bound is `2026-12-31T00:00:00Z` (not end-of-day). Factory deals are in June so they pass.
11. **Unused Fix.CTrader project reference** on the test csproj. A FIX compile break takes this class down for no reason. Domain CS8858 already does.
12. **`UnitTest1.cs` still in the assembly.** Delete when the next real class lands.

---

## 9. Recommended next facts (do not add empty classes; product not edited)

Keep this file as labeled InMemory smoke. Then:

| Priority | Fact | Seam | Why |
|---|---|---|---|
| 0 | Restore Domain compile (`ReconstructedTradeResult` record **or** drop `with`) | product | Suite is currently unbuildable |
| 1 | Tighten fact 1: exact 4 groups (name the contest path), 18 deals, 9 completed XAU, 10001 `SHADOW` + flags, 10002 flags, 10003 `INSUFFICIENT_DATA`, 99001 `CompletedXauTrades==3`, history 4, FIX both `Disconnected`, outbox 4, copy 6, shadow 6, checkpoints 0 | InMemory | Locks the tape this class already runs |
| 2 | `SeedAsync` second call is a no-op (broker / deal / outbox counts unchanged) | InMemory | Production startup |
| 3 | Same ticket, two brokers → two rows; same ticket replay → one row + `false` | **Postgres** | A78 / §10 |
| 4 | First-write-wins: second upsert with mutated price does not change the stored row | Postgres | A78 forbidden `DO UPDATE` |
| 5 | Concurrent upsert does not create two rows (unique index, not `AnyAsync`) | Postgres | TOCTOU |
| 6 | Stop calling `DemoSeeder` once a synthetic fixture exists (`test.invalid`, login 0) | both | §61 / A90 |

Do **not** invent `OutboxProcessingTests` or `Mt5BackfillRestartTests` until a real processor / checkpoint writer exists. The four demo `ScoreUpdate` rows are not that processor.

---

## 10. Disposition

| Metric | Value |
|---|---|
| Facts in class | 2 |
| Fresh rebuild this review | **FAIL** (`CS8858`, Domain) |
| Stale-bin class facts | **2 passed**, 0 failed, 0 skipped |
| Assembly includes `PlaceholderRemoved` | yes — **FALSE_GREEN** |
| Assertions that lock real demo behavior | **2** (10001 completed-XAU count; 10002 `RISK_BLOCKED`) |
| Vacuous / weak assertions | 10001 `NotBe(LIVE)`; groups `> 2`; deals `> 0`; `Any` completed XAU |
| §60 integration areas proven | **0 / 8** |
| §61 harness capabilities proven | **0 / 7** |
| Unique-index proof | **None** (InMemory + `AnyAsync`) |
| Checkpoints | **0**, unasserted |
| Demo outbox / copy / shadow | **4 / 6 / 6**, unasserted |
| FIX status honesty | seeder **Disconnected**; test **does not lock it** |
| Live identifiers persisted by the SUT under test | **Yes** (FIX host + SenderCompId + lab IPs) |
| Test SHA vs C16 | **identical** |
| Product source changed by D37 | **No** |

**Do not treat the Integration `--no-build` green run as Phase-1 or §68 exit.** Treat this class as an unlabeled InMemory orchestration smoke that currently locks two useful demo outcomes, misses the identity/idempotency law it is named after, and cannot even rebuild until Domain compiles again.

---

## 11. Sources

- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` (SHA-256 `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD`)
- `D:\Prop\tests\Integration\UnitTest1.cs`
- `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §60–§61
- `D:\Prop\reports\swarm\20260818\C16_seed_test_review.md`
- `D:\Prop\reports\swarm\20260818\D20_store.md`
- `D:\Prop\reports\swarm\20260818\D22_seeder.md`
- `D:\Prop\reports\swarm\20260818\A90_integration_class_list.md`
- `D:\Prop\reports\swarm\20260818\A27_test_inventory.md`
- `D:\Prop\reports\swarm\20260818\A78_deal_idempotency.md`
- `D:\Prop\reports\swarm\20260818\A10_integration_tests_audit.md`
- `D:\Prop\reports\swarm\20260818\_tmp_d37_eval\stdout.txt`

---

*End of D37. Product source was not modified. `SeedingAndStoreTests` is an unlabeled InMemory smoke (2 facts). It is not Architecture §60 integration.*
