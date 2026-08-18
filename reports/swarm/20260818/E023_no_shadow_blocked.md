# E023 — `RISK_BLOCKED` must not create shadow

| Field | Value |
|---|---|
| Agent | E023 (`RISK_BLOCKED` × new-shadow OPEN) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:20:40Z (2026-08-18T13:50:40+05:30) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Assigned | `RISK_BLOCKED` must not create shadow. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E023_no_shadow_blocked.md` |
| Eval (not product) | `D:\Prop\reports\swarm\20260818\_tmp_e023_noshadow\` (`stdout.txt` `VERDICT=RISK_BLOCKED_CREATES_ZERO_SHADOW_ON_DEMO_AND_DIRECT`) |
| Product source modified | **No.** This report + the throwaway eval tree are the only writes. |
| Test source modified | **No.** |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Binding law | Architecture §22–24 / §39; `A24` §5 / §7.6 `TRADER_NOT_SHADOW_ELIGIBLE`; `A69` §4.8 / §9.1; `A22` R3; `A23` `PAUSE_TRADER` / `TRADER_RISK_BLOCKED` |
| Siblings | D16 (engine), D48 (six demo rows), D12 / C02 (scorer Case B), B18, A24, A69 |
| Method | Full read of persist / rebuild / scorer / engine / seeder / Fake tape / tests / A24+A69 eligibility. SHA-256 of the 14 files in §1. Product `*.cs` grep for `new ShadowOrder` / `ShadowOrders.Add` / `PersistDemoShadowAsync` / `TRADER_NOT_SHADOW` / `TRADER_RISK_BLOCKED`. InMemory `DemoSeeder.SeedAsync` + direct `PersistDemoShadowAsync` per state token. Product source **not** edited. |

**Honesty rule:** zero `shadow_orders` for a `RISK_BLOCKED` login is **not** Architecture §24 / A24 shadow copy. A `state != SHADOW` early-return is **not** `TRADER_NOT_SHADOW_ELIGIBLE`. A `ScoreUpdate` outbox row is **not** a shadow order. A winning-martingale `WATCH` is **not** this claim.

---

## 0. Verdict (binding — do not greenwash)

**HOLD on the assigned claim for the only writer that exists. `RISK_BLOCKED` does not create demo `shadow_orders` or `copy_intents`. Measured: login 10002 is `RISK_BLOCKED`, `shadow_orders=0`, `copy_intents=0`. Direct `PersistDemoShadowAsync(..., RISK_BLOCKED, 3 completed XAU, dest quote present)` writes `Δorders=0`, `Δintents=0`.**

This is a **token gate** on the demo persist helper (`state != TraderState.SHADOW` → save outbox, return). It is **not** A24 eligibility, **not** a risk-engine reject, and **not** test-locked.

| Question | Measured answer |
|---|---|
| Must `RISK_BLOCKED` open new shadow? | **No.** A69 §4.8 / §9.1: new shadow OPEN/INCR = **no**. A24 §5: only `SHADOW` / `LIVE_CANDIDATE` / `LIVE` generate shadow. |
| Does the demo path create shadow for 10002? | **No. 0 / 0.** State is `RISK_BLOCKED`. |
| Does `PersistDemoShadowAsync` write orders when `state == RISK_BLOCKED`? | **No.** Early-return after one `ScoreUpdate` outbox. |
| Is `ShadowCopyEngine` state-aware? | **No.** Calculator. No `TraderState` parameter. |
| Does `RiskEngine` reject `TRADER_NOT_SHADOW_ELIGIBLE` / `TRADER_RISK_BLOCKED`? | **No.** Request has **no** `trader_state`. Codes **absent**. Engine is **not called** on this path. |
| Who is the only `ShadowOrders.Add`? | `EfTradingStore.PersistDemoShadowAsync` L321. |
| If a caller **lies** `state=SHADOW` on the 10002 tape? | **Writes 3** `shadow_orders` + 3 `SHADOW_ONLY` intents. Gate trusts the token. |
| Do `WATCH` / `PAUSED` / `DISQUALIFIED` / `LIVE` / `LIVE_CANDIDATE` create demo shadow? | **No** (same `!= SHADOW` gate). A24 would allow `LIVE_CANDIDATE` / `LIVE`; demo persist is **stricter** and **wrong-shaped**. |
| Integration lock of “10002 has 0 shadow rows”? | **Missing.** `SeedingAndStoreTests` locks state only. |
| Is this §24 / A24 / §69.11? | **No.** Entry-only, invented dest quote (`VenueInstrumentId=null`), `RiskEngine` unused, intents already expired. |

Classification:

| Slice | Class |
|---|---|
| Law: `RISK_BLOCKED` must not **open** new shadow | **BINDING** (A24 / A69) |
| Demo persist vs `RISK_BLOCKED` | **HOLD** (measured 0 rows) |
| Direct persist vs `RISK_BLOCKED` | **HOLD** (measured Δ 0) |
| A24 `TRADER_NOT_SHADOW_ELIGIBLE` | **MISSING** |
| Risk-engine trader-state gate | **MISSING** |
| Test lock of zero shadow rows | **MISSING** |
| Caller-supplied state trust | **HOLE** (lie `SHADOW` → 3 rows) |
| Scorer always emitting `RISK_BLOCKED` on severe books | **FAIL** (winning martingale → `WATCH`; see D12 / C02). Those books do **not** hit this persist gate as `RISK_BLOCKED`. |
| Architecture §24 book | **MISSING** |
| Product source edited by E023 | **No** |

Do **not** tick A24 / A100 G14 / §69.11 from this file. Do **not** treat “10002 has no shadow rows” as “shadow eligibility is implemented.” Do **not** treat D16 “zero callers” as current — `PersistDemoShadowAsync` constructs the engine (D48).

---

## 1. Files read (no product edits)

| Path | Bytes | Physical lines | LastWriteTimeUtc | SHA-256 |
|---|---:|---:|---|---|
| `src/Infrastructure/Persistence/EfTradingStore.cs` | 12097 | 338 | 2026-08-18T08:05:59Z | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| `src/Application/Ingestion/DealIngestionService.cs` | 4535 | 106 | 2026-08-18T08:05:29Z | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` |
| `src/Domain/Scoring/BaselineScorer.cs` | 8143 | 212 | 2026-08-18T07:38:10Z | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| `src/Domain/Shadow/ShadowCopyEngine.cs` | 3249 | 91 | 2026-08-18T07:38:10Z | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` |
| `src/Domain/Risk/RiskEngine.cs` | 8567 | 189 | 2026-08-18T07:38:10Z | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| `src/Domain/Enums/TraderState.cs` | 264 | 14 | 2026-08-18T07:33:45Z | `E509C59F1597EE0F424A9F9408D0B27B2C7063D724B3D0A63880E2558B930D68` |
| `src/Domain/Entities/ShadowOrder.cs` | 556 | 17 | 2026-08-18T07:39:03Z | `8EF2D2372CFC01A27CBCA4A1855A322B54A4439FCB6B11AA3A5404FD0D1F8B86` |
| `src/Domain/Entities/CopyIntent.cs` | 951 | 22 | 2026-08-18T08:10:10Z | `C9AE3FF95058B72FC00A4DDBCBF2DFD68B7637D00D321244C376E2A1D6D9148B` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 5082 | 140 | 2026-08-18T08:04:59Z | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | 7049 | 170 | 2026-08-18T07:43:42Z | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | 8708 | 200 | 2026-08-18T08:05:15Z | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `tests/Integration/SeedingAndStoreTests.cs` | 3119 | 63 | 2026-08-18T07:47:42Z | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` |
| `tests/Unit/BaselineScorerTests.cs` | 2414 | 74 | 2026-08-18T07:47:42Z | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` |
| `tests/Unit/RiskEngineTests.cs` | 2909 | 87 | 2026-08-18T07:47:42Z | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` |

Worktree: `DealIngestionService.cs` modified vs HEAD; `EfTradingStore.cs` untracked. Hashes match D48’s persist / rebuild pair.

Product `*.cs` grep (this pass):

| Pattern | Hits |
|---|---|
| `new ShadowOrder` / `ShadowOrders.Add` | **1** — `EfTradingStore.cs` L321 |
| `PersistDemoShadowAsync` | interface L17 + `RebuildTraderAsync` L104 + store L251 |
| `TRADER_NOT_SHADOW_ELIGIBLE` | **0** in product / tests |
| `TRADER_RISK_BLOCKED` | **0** in product / tests (spec-only) |
| `ShadowOrders` in `DemoSeeder.cs` | **0** |

---

## 2. Law — new shadow OPEN is forbidden

`TraderState.RISK_BLOCKED = 7`.

A69 §4.8:

| | |
|---|---|
| Meaning | Severe flags or explicit manual/risk block. |
| Shadow new opens | **no** |
| Live NOS | **no** — `TRADER_RISK_BLOCKED` / `SEVERE_RISK_FLAG` |

A69 §9.1 eligibility:

| State | New shadow OPEN/INCR |
|---|---|
| `SHADOW` | yes |
| `LIVE_CANDIDATE` | yes |
| `LIVE` | only if parallel shadow |
| **`RISK_BLOCKED`** | **no** |
| `PAUSED` / `DISQUALIFIED` / `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` | no |

Leftover REDUCE/CLOSE of an **already-open** shadow book remains allowed after demotion. That is **not** “create shadow.” Demo 10002 has no leftover book.

A24 §5 CopyIntent field `trader_state`:

> Only `SHADOW` / `LIVE_CANDIDATE` / `LIVE` traders generate shadow (config)

A24 §7.6 OPEN reject set includes `TRADER_NOT_SHADOW_ELIGIBLE`. That token is **not implemented**.

A22 R3 / `TraderStateMachine.FromBaseline`: `risk >= 80` **or** `(Martingale ∧ MaxDrawdown > 0 ∧ NetPnl < 0)` → `RISK_BLOCKED`. Dollars do not buy `SHADOW`.

---

## 3. The only writer — token gate, then simulate

`ReconstructionScoringService.RebuildTraderAsync` always calls persist with the **scorer’s** `SuggestedState`:

```104:104:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

`PersistDemoShadowAsync` writes a `ScoreUpdate` outbox for **every** login, then:

```267:271:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
```

Only after that check does it load the latest `destination_quotes` row, construct `ShadowCopyEngine`, and `Add` a `CopyIntent` (`Status = SHADOW_ONLY`, `Action = OpenExposure`) plus a `ShadowOrder` per completed trade.

Consequences for this claim:

1. **`RISK_BLOCKED` never reaches `ShadowOrders.Add`.** Measured.
2. The comparison is **exact `SHADOW`**, not an A24 eligibility set. `LIVE` / `LIVE_CANDIDATE` are also blocked (demo-stricter, A24-wrong).
3. The method does **not** re-score. It trusts `state`.
4. `RiskEngine.Evaluate` is **never** invoked. Martingale-on-intent (`MARTINGALE_BLOCK` / `PauseTrader`) does not run here.
5. `ShadowCopyEngine` cannot refuse a blocked trader; it never sees the state.

Seeder does not insert `ShadowOrders`. It rebuilds `{10001, 10002, 10003, 99001}` after planting one invented dest quote (bid 2399.45 / ask 2399.85, `VenueInstrumentId = null`). That quote is the **enabler for SHADOW logins only**.

---

## 4. Measured demo book (InMemory `SeedAsync`)

Eval: `dotnet run` of `_tmp_e023_noshadow` against current hashes. Same counts as D48.

Fake tape for 10002 (`DemoBrokerFactory.BuildAchieverDeals`):

| Position | Lots | Source P&L (deal profit + commission/swap) |
|---:|---:|---|
| 601 | 0.10 | −200 |
| 602 | 0.20 | −500 |
| 603 | 0.40 | −1400 |

Size-up after losses → `Martingale=true`. NET < 0, DD > 0 → `FromBaseline` second clause → `RISK_BLOCKED`. Risk score is **70** (not ≥ 80): 35 martingale + 15 lot-escalation + 10 lot-CV + 10 no-SL.

| Login | State | N | Risk | Quality | Martingale | `shadow_orders` | `copy_intents` | outbox |
|---:|---|---:|---:|---:|---|---:|---:|---:|
| 10001 | `SHADOW` | 3 | 10 | 95.50 | false | **3** | 3 | 1 |
| **10002** | **`RISK_BLOCKED`** | 3 | 70 | 42.50 | **true** | **0** | **0** | 1 |
| 10003 | `INSUFFICIENT_DATA` | 0 | 10 | 40 | false | 0 | 0 | 1 |
| 99001 | `SHADOW` | 3 | 10 | 95.50 | false | **3** | 3 | 1 |
| **Total** | | | | | | **6** | **6** | **4** |

10002 outbox payload: `{"state":"RISK_BLOCKED","completed":3}`. That is a **score event**, not a shadow.

Dashboard `shadowPnl` after seed is Σ `SourceVsShadowSlippage` = **248.20**, entirely from 10001+99001. 10002 contributes **0**. Overview `riskBlocked` count = 1.

---

## 5. Direct persist (quote present, completed XAU present)

After the seed, same store, same dest quote:

| Call | `Δ shadow_orders` | `Δ copy_intents` | `Δ outbox` |
|---|---:|---:|---:|
| `PersistDemoShadowAsync(10002, RISK_BLOCKED, losing-martingale×3)` | **0** | **0** | **1** |
| `PersistDemoShadowAsync(10002, SHADOW, same tape)` — **caller lie** | **3** | **3** | 1 |
| `WATCH` / `PAUSED` / `DISQUALIFIED` / `INSUFFICIENT_DATA` / `EARLY_SCORE` / `LIVE` / `LIVE_CANDIDATE` | **0** | — | 1 each |

Lie-path orders: qty `0.40, 0.20, 0.10` for login 10002. Proof that **behavior is not re-checked**. A future bug that stamps `SHADOW` on a blocked book **will** create shadow.

---

## 6. Adjacent scorer hole (not this claim, do not confuse)

Domain-only `BaselineScorer.Score` this pass:

| Tape | State | Quality | Risk | NET |
|---|---|---:|---:|---:|
| Losing 0.10→0.20→0.40 (10002-shaped) | **`RISK_BLOCKED`** | 42.50 | 70 | −2100 |
| Winning 0.10→0.20→0.40 (A22 Case B) | **`WATCH`** | 72.50 | 70 | +1700 |
| Mild 1.30× after a loss, SL unused | **`WATCH`** | 80.75 | 45 | +120 |

Winning martingale is **not** `RISK_BLOCKED` (NET≥0 skips the second clause; risk 70 < 80). It is also **not** `SHADOW` (risk not `< 40`), so **this persist helper still writes 0 rows**. D12’s milder book (risk < 40) **would** land `SHADOW` and **would** create demo shadow. That is a **scorer** defect, not a persist-gate miss on the `RISK_BLOCKED` token.

`RiskEngine` L141–142: `BlockMartingale && MartingaleFlag && IsIncreasing` → `PauseTrader` / `MARTINGALE_BLOCK`. Unused on the shadow persist path. Request has no `TraderState` field.

---

## 7. Tests do not lock the assigned claim

| Test | What it locks | Shadow-row assertion |
|---|---|---|
| `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` | 10002 `CurrentState == RISK_BLOCKED` | **None.** Would still pass if 10002 had 3 `shadow_orders`. |
| `BaselineScorerTests.Martingale_after_losses_is_risk_blocked` | losing 0.10/0.20/0.40 → `RISK_BLOCKED` | **None** (no store). |
| `RiskEngineTests` | quote / kill / REAL_COPY / reconcile | **No** trader state. **No** `TRADER_RISK_BLOCKED`. |
| Any `ShadowCopy*` / `PersistDemoShadow*` fact | — | **Missing.** |

The assigned invariant is therefore **measured in this eval** and **not a build breaker**.

---

## 8. What this is not

- Not A24 destination-QUOTE shadow (D16 / D48).
- Not leftover REDUCE/CLOSE after demotion (A69 §9.1). 10002 never had a shadow book.
- Not live `NewOrderSingle` (SAFE_BY_ABSENCE; FIX still `Disconnected`).
- Not proof that every severe book is `RISK_BLOCKED`.
- Not a reason to greenwash §69 item 11 / G14.

---

## 9. What a later increment must do (not done here)

Product source was **not** edited. If a later agent implements the law, the minimum lock is:

1. Persist / policy: `new shadow OPEN/INCR` only when current state ∈ `{SHADOW, LIVE_CANDIDATE, LIVE}` **and** (for `LIVE`) `SHADOW_PARALLEL_TO_LIVE`. `RISK_BLOCKED` → reject `TRADER_NOT_SHADOW_ELIGIBLE` / `TRADER_RISK_BLOCKED`, **no** `ShadowOrder` / `CopyIntent`.
2. Do not trust a caller-supplied state that disagrees with persisted `TraderScores.CurrentState`.
3. Integration: after `SeedAsync`, `db.ShadowOrders.Count(o => o.SourceLogin == 10002) == 0` and same for `CopyIntents`.
4. Unit: `PersistDemoShadowAsync(..., RISK_BLOCKED, ...)` Δorders = 0 even with a dest quote; lying `SHADOW` against a persisted `RISK_BLOCKED` score must **not** write (today it does).
5. Case B still belongs to the scorer (D12), not this persist helper.

---

## 10. Eval pin

```text
Path:    D:\Prop\reports\swarm\20260818\_tmp_e023_noshadow\
Command: dotnet run --project E023NoShadowEval.csproj -c Release
Stdout:  D:\Prop\reports\swarm\20260818\_tmp_e023_noshadow\stdout.txt
VERDICT=RISK_BLOCKED_CREATES_ZERO_SHADOW_ON_DEMO_AND_DIRECT
CLAIM_10002_IS_RISK_BLOCKED=True
CLAIM_10002_SHADOW_ZERO=True
CLAIM_10002_INTENT_ZERO=True
DIRECT_RISK_BLOCKED_DELTA_ORDERS=0
DIRECT_LIE_SHADOW_DELTA_ORDERS=3
```

Throwaway compile tree only. Not product. Not a unit test.

---

**One-liner:** `RISK_BLOCKED` must not create shadow (A24/A69). The only writer obeys that for the **token** — demo 10002 and a direct `RISK_BLOCKED` persist write **zero** `shadow_orders`. The gate is `!= SHADOW`, untested, unused by `RiskEngine`, and bypassable by lying `SHADOW`. Product source was not modified.
