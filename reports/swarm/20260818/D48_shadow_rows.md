# D48 — Are `ShadowOrders` created in the seeder?

| Field | Value |
|---|---|
| Agent | D48 (senior engineer; seeder → shadow-row measurement) |
| Date | 2026-08-18 |
| Assigned | Are `ShadowOrders` created in seeder? Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D48_shadow_rows.md` |
| Eval (not product) | `D:\Prop\reports\swarm\20260818\_tmp_d48_shadow\` (`stdout.txt` `VERDICT=YES_SIX_SHADOW_ROWS_VIA_REBUILD`) |
| Product source modified | **No.** This report + the throwaway eval tree are the only writes. |
| Method | Full read of `DemoSeeder`, `RebuildTraderAsync`, `PersistDemoShadowAsync`, `ShadowOrder`, `CopyIntent`, `BaselineScorer` / `TraderStateMachine`, `DemoBrokerFactory` tape, `ShadowCopyEngine.SimulateEntry`, `EfDashboardQueries` shadow sum. Grep product `*.cs` for `new ShadowOrder` / `HasData` / `ShadowOrders.Add`. InMemory `DemoSeeder.SeedAsync` count. Domain reconstruct+score+simulate cross-check. |

**Honesty rule:** a `shadow_orders` row written by the demo rebuild is **not** Architecture §24 shadow copy. Summing `SourceVsShadowSlippage` is **not** shadow P&L. Six entry-only fills against a seeder-invented 2399 book is **not** a destination tape.

---

## 0. Verdict

**YES — as a side-effect of the seeder’s rebuild, not as a direct `DemoSeeder` insert. Measured: 6 `shadow_orders` + 6 `SHADOW_ONLY` `copy_intents` after one empty-store `SeedAsync`.**

`DemoSeeder.cs` does **not** contain the token `ShadowOrders`. There is **no** EF `HasData`. There is **no** SQL seed script. The only product writer is:

```text
DemoSeeder.SeedAsync
  → DealIngestionService.SyncBrokerAsync (Fake tape)
  → ReconstructionScoringService.RebuildTraderAsync  × {10001,10002,10003,99001}
      → EfTradingStore.PersistDemoShadowAsync
          → if SuggestedState == SHADOW and a destination_quotes row exists
              → CopyIntents.Add (IdempotencyKey = shadow:{brokerId}:{login}:{positionId})
              → ShadowOrders.Add (one SimulateEntry per completed XAU trade)
```

| Question | Measured answer |
|---|---|
| Does `DemoSeeder` call `ShadowOrders.Add`? | **No.** File grep: `SEEDER_TEXT_CONTAINS_ShadowOrders=False`. |
| Does a first-run `SeedAsync` persist `shadow_orders`? | **Yes. 6 rows.** |
| Who actually `Add`s them? | `EfTradingStore.PersistDemoShadowAsync` L321. **Only** `new ShadowOrder` in product `*.cs`. |
| Which logins get rows? | **10001** (3) and **99001** (3). Both `TraderState.SHADOW`. |
| 10002 / 10003? | **Zero** shadow rows. `RISK_BLOCKED` / `INSUFFICIENT_DATA`. Outbox `ScoreUpdate` only. |
| Second `SeedAsync`? | Early-return on `Brokers.Any`. Counts stay 6 / 6 / 4. |
| Second `RebuildTraderAsync`? | Shadow + intent counts stay 6. Outbox grows **4 → 8** (not idempotent). |
| Is this §24 / A24? | **No.** Entry-only, Fake dest quote, `SimulateExit` never called, `RiskEngine` unused, intents already expired. |
| Integration test lock? | **None.** `SeedingAndStoreTests` does not count `ShadowOrders`. |

Classification:

| Slice | Class |
|---|---|
| Direct seeder insert | **ABSENT** |
| Seeder orchestration → persist | **EXISTS** (demo side-effect) |
| Row count after seed | **6** (measured InMemory) |
| Architecture §24 book | **MISSING** (thin fill-shaped rows; no fills/positions/performance tables) |
| Dashboard `ShadowPnl` after seed | **248.20** = Σ slippage, **not** P&L |
| Product source edited by D48 | **No** |

Do **not** treat “seed created 6 shadow rows” as go-live G14 / §69.11 / A24 §19. Do **not** treat D16 “no callers” / D22 “not written: ShadowOrders” / C59 “zero CopyIntent writers” as current — those snapshots predate `PersistDemoShadowAsync`.

---

## 1. What was read (no product edits)

| Path | SHA-256 | Bytes | Role |
|---|---|---:|---|
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 5082 | Catalog + Fake ingest + 4 rebuilds. **140** lines. |
| `src\Application\Ingestion\DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 4535 | `ITradingStore.PersistDemoShadowAsync` + `RebuildTraderAsync` L104. **106** lines. |
| `src\Infrastructure\Persistence\EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 12097 | **Only** `ShadowOrders.Add`. **338** lines. |
| `src\Domain\Entities\ShadowOrder.cs` | `8EF2D2372CFC01A27CBCA4A1855A322B54A4439FCB6B11AA3A5404FD0D1F8B86` | 556 | Persist shape (fill-ish). |
| `src\Domain\Entities\CopyIntent.cs` | `C9AE3FF95058B72FC00A4DDBCBF2DFD68B7637D00D321244C376E2A1D6D9148B` | 951 | `IdempotencyKey` / `Status` / `ExpiresAt`. |
| `src\Domain\Entities\DestinationQuote.cs` | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | 421 | `DestinationQuoteSnapshot` (filename ≠ type). |
| `src\Domain\Entities\OutboxEvent.cs` | `78108643D4C8E25DBEA767C30145366B3337C59D6E39EA3F613B480CDE6649A8` | 546 | `Type` / `AggregateId` / `PayloadJson` / `OccurredAt`. |
| `src\Domain\Shadow\ShadowCopyEngine.cs` | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | 3249 | `SimulateEntry` (80 ms delay, no 0.05 overlay). |
| `src\Domain\Scoring\BaselineScorer.cs` | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 8143 | `quality>=70 && risk<40` → `SHADOW`. |
| `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 7049 | Demo tape (9 closed XAU round-trips). |
| `src\Infrastructure\Persistence\TraderDbContext.cs` | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 5951 | `shadow_orders` PK-only. |
| `src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 8708 | `Sum(SourceVsShadowSlippage)`. |
| `tests\Integration\SeedingAndStoreTests.cs` | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | 3119 | No shadow-row assertion. |

Grep of product `*.cs`:

| Pattern | Hits |
|---|---|
| `new ShadowOrder` / `ShadowOrders.Add` | **1** — `EfTradingStore.cs` L321 |
| `HasData` | **0** |
| `ShadowOrders` in `DemoSeeder.cs` | **0** |
| `PersistDemoShadowAsync` | interface L17 + `RebuildTraderAsync` L104 + store L251 |

---

## 2. Seeder does not insert; seeder does trigger

`DemoSeeder.SeedAsync` writes, then rebuilds:

```105:138:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.DestinationQuotes.Add(new DestinationQuoteSnapshot
        {
            Id = Guid.NewGuid(),
            CanonicalSymbol = "XAUUSD",
            VenueInstrumentId = null,
            Bid = 2399.45m,
            Ask = 2399.85m,
            ReceivedAt = now
        });
        // ...
        await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, ct);
        await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, ct);

        foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        {
            var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
            await scoring.RebuildTraderAsync(code, login, ct);
        }
```

Catalog written **before** ingest (one `SaveChangesAsync`): 2 brokers, 1 `XAUUSD` instrument, 2 FIX rows (`Disconnected` + `LastError` “no live socket”), **1 invented dest quote**, 1 kill-switch `None`.

The dest quote is the **enabler**. `PersistDemoShadowAsync` returns after the outbox insert if `DestinationQuotes` is empty. The seeder plants a usable-looking book (`VenueInstrumentId = null`, bid/ask 2399.45/2399.85, `ReceivedAt = UtcNow`) so the later rebuild **will** price.

Rebuild hook:

```104:104:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

Gate + writer:

```267:333:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
        // latest destination_quotes → Risk.DestinationQuote
        // foreach completed XAU: CopyIntent SHADOW_ONLY + SimulateEntry(80ms) + ShadowOrders.Add
```

Every rebuild, including blocked/empty traders, still inserts one `OutboxEvent` `ScoreUpdate`. Shadow rows are **state-gated**.

---

## 3. Why 6 rows (not 9 completed XAU)

`TraderStateMachine.FromBaseline` (`BaselineScorer.cs` L189–207):

| Login | N completed XAU | State | Shadow rows |
|---|---:|---|---:|
| 10001 Achiever | 3 | **SHADOW** (q=95.50, r=10, b=90) | **3** |
| 10002 Achiever | 3 | **RISK_BLOCKED** (martingale ∧ DD>0 ∧ net<0; r=70) | **0** |
| 10003 Achiever | 0 | **INSUFFICIENT_DATA** | **0** |
| 99001 Starwave | 3 | **SHADOW** (q=95.50, r=10, b=90) | **3** |

`quality >= 70 && risk < 40` after N≥3 and not blocked → `SHADOW`. 10001/99001 match C16/C23/D12. 10002 never enters the `foreach`. 10003 has an empty `completedXau` list even if it did.

Factory positions that become rows:

| Login | PositionId | Side | Lots | Entry VWAP | Fill px (taker) | Slippage |
|---|---:|---|---:|---:|---:|---:|
| 10001 | 501 | Long | 0.10 | 2320.10 | **2399.85** (ask) | **79.75** |
| 10001 | 502 | Short | 0.10 | 2338.00 | **2399.45** (bid) | **−61.45** |
| 10001 | 503 | Long | 0.10 | 2325.50 | **2399.85** | **74.35** |
| 99001 | 701 | Long | 0.05 | 2340 | **2399.85** | **59.85** |
| 99001 | 702 | Long | 0.05 | 2348 | **2399.85** | **51.85** |
| 99001 | 703 | Long | 0.05 | 2356 | **2399.85** | **43.85** |

`modeledDelay = 80 ms` is **not** `> 250 ms`, so `DefaultLatencySlippagePoints` (0.05) is **not** applied. Fill = dest touch. Spread persisted = **0.40**. Quantity = `MaxVolumeLots` (source lots echoed; `QuantityNormalizer` unused).

Σ slippage = **248.20**. That is also `EfDashboardQueries` `ShadowPnl` after seed.

CopyIntent keys (unique; rebuild skips if present):

```text
shadow:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1:10001:501
shadow:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1:10001:502
shadow:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1:10001:503
shadow:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2:99001:701
shadow:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2:99001:702
shadow:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2:99001:703
```

`ExpiresAt = OpenedAt + 15s` on June 2026 opens → **already expired** at seed time. Status is still `SHADOW_ONLY`. `CopyIntent.Direction` / `SourcePositionId` are **not set** by the writer (enum/long defaults).

`SimulateExit` is **never** called. There is no shadow close, no `shadow_fills`, no `shadow_positions`.

---

## 4. Measured run (this review)

Throwaway eval (reports only; not product):

```text
dotnet run --project D:\Prop\reports\swarm\20260818\_tmp_d48_shadow\D48ShadowEval.csproj -c Release
```

`stdout.txt` (abridged):

```text
EXPECTED_SHADOW_ORDERS=6
SEED1_SHADOW_ORDERS=6
SEED1_COPY_INTENTS=6
SEED1_OUTBOX=4
SEED1_DEST_QUOTES=1
SEED1_SCORES=10001:SHADOW:N=3;10002:RISK_BLOCKED:N=3;10003:INSUFFICIENT_DATA:N=0;99001:SHADOW:N=3
SEED1_SLIP_SUM=248.20
SEED2_EARLY_RETURN_SHADOW_ORDERS=6
SEED2_EARLY_RETURN_COPY_INTENTS=6
SEED2_EARLY_RETURN_OUTBOX=4
REBUILD_SHADOW_ORDERS=6
REBUILD_COPY_INTENTS=6
REBUILD_OUTBOX=8
DASH_SHADOW_COUNT=2 DASH_SHADOW_PNL=248.20 DASH_BLOCKED=1
SEEDER_TEXT_CONTAINS_ShadowOrders=False
VERDICT=YES_SIX_SHADOW_ROWS_VIA_REBUILD
```

Cross-check: C23 `SEED_SCORES` / `DASH_OVERVIEW shadow=2` is **trader-state count**, not `shadow_orders` count. Same Fake tape, same two SHADOW logins.

Store: EF Core **InMemory**. This is **not** a Postgres unique-index proof. `copy_intents.IdempotencyKey` unique is configured in fluent API; InMemory skip is application-level `AnyAsync`.

---

## 5. Who runs the seeder (who plants the 6 rows)

| Caller | When | Effect on empty store |
|---|---|---|
| `D:\Prop\apps\api\Program.cs` ~88 | API boot after `EnsureCreatedAsync` | First empty `brokers` → 6 shadow rows |
| `D:\Prop\apps\mt5-worker\Program.cs` ~15 | Worker boot | Same |
| `D:\Prop\apps\fix-worker\Program.cs` ~15 | Worker boot | Same (destination process writes source + shadow) |
| `SeedingAndStoreTests` fact 1 | Isolated InMemory | Creates the 6 rows; **does not assert them** |
| `/api/ops/resync` | Not a seeder | Rebuilds the four logins → `PersistDemoShadowAsync` again (keys skip; outbox +4) |

`if (await db.Brokers.AnyAsync(ct)) return;` — first process wins. Shared Postgres later: one seed, then workers only rebuild.

---

## 6. What these rows are **not**

| Claim | Truth |
|---|---|
| §24 dest-quote shadow book | **No.** Latest-only invented snapshot; `VenueInstrumentId=null`; no post-delay re-read; no OPEN reject. |
| Shadow P&L | **No.** Dashboard sums print-vs-fill **price difference** (248.20 on a 2399 book vs June 2320s entries). |
| Closed shadow positions | **No.** Entry only. |
| Live copy / `35=D` | **No.** `SHADOW_ONLY`. `RealCopyEnabled` stays false. |
| Idempotent scoring pipeline | **Partial.** Shadow keys skip. Outbox `ScoreUpdate` appends every rebuild. |
| Test-locked | **No.** C16 fact 1 still only `NotBe(LIVE)` on 10001. |
| EF `HasData` / SQL fixture | **No.** |

D16 F12 (“zero product callers of `ShadowCopyEngine`”) is **stale**. `PersistDemoShadowAsync` constructs the engine.

D22 §5.3 (“Not written: `ShadowOrders` / `CopyIntents` / `OutboxEvents`”) is **stale** on those three tables. D22 FIX `LoggedOn` snapshot is also stale: current seeder writes `Disconnected` + `LastError`.

C58 / C59 “nothing inserts outbox / CopyIntent” are **stale** on the demo rebuild path.

---

## 7. Residual risks

1. **Dashboard lie after first API boot.** Overview `Shadow=2` (true trader-state count) plus `ShadowPnl=248.20` (false P&L) looks like a working shadow book.
2. **Expired intents at birth.** `ExpiresAt` is source-open + 15 s on 2026-06-01/02. Any later expiry gate will see 6 already-dead intents.
3. **Catch-up of full history.** One OPEN per completed position on first SHADOW rebuild is the A24/C59 “blind catch-up” defect. Seed makes it the **default demo state**.
4. **Three hosts seed.** First empty store wins; fix-worker should not write `mt5_*` or `shadow_orders` (`B07`).
5. **Worktree churn.** Mid-review, `CopyIntent` / `DestinationQuote` / `OutboxEvent` briefly lost the fields this writer needs and Infrastructure failed CS0246 (`DestinationQuoteSnapshot`, `ReconstructedTrades`). Re-measured after restore. A later entity slimming will **silently stop** creating rows (or fail compile). Snapshot hashes above.

---

## 8. One-page operator view

```text
D48  ShadowOrders in seeder?                            2026-08-18
================================================================
DemoSeeder.cs SHA-256  A6416491…1FE20   140 lines
  contains "ShadowOrders"?              NO
  EF HasData / SQL seed?                NO
----------------------------------------------------------------
Path   SeedAsync → RebuildTraderAsync → PersistDemoShadowAsync
Gate   SuggestedState == SHADOW  AND  destination_quotes exists
----------------------------------------------------------------
10001 SHADOW N=3                         3 rows
10002 RISK_BLOCKED N=3                   0 rows
10003 INSUFFICIENT_DATA N=0              0 rows
99001 SHADOW N=3                         3 rows
MEASURED after SeedAsync                 6 shadow_orders
                                         6 copy_intents (SHADOW_ONLY)
                                         4 outbox ScoreUpdate
DASH ShadowPnl                           248.20  (slippage, not P&L)
Fill book                                seeder 2399.45 / 2399.85
SimulateExit                             NEVER
§24 / A24                                NO
Product source edited by D48             NO
================================================================
```

---

## 9. Sources

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Domain\Entities\ShadowOrder.cs`
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\DestinationQuote.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\_tmp_d48_shadow\stdout.txt`
- Siblings (partially stale): `D16_shadow.md`, `D22_seeder.md`, `C16_seed_test_review.md`, `C23_empty_trader.md`, `C59_copyintent_gap.md`, `A24_shadow_copy_spec.md`

---

*End of D48. Product source was not modified. Answer: yes — 6 `shadow_orders` rows are created as a rebuild side-effect of `DemoSeeder`, not by the seeder inserting them directly.*
