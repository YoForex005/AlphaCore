# E007 — `PersistDemoShadowAsync`: SHADOW only?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E007_shadow.md` |
| Agent | E007 (SHADOW-only pin of `PersistDemoShadowAsync`) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:36+05:30 |
| Assigned | Read `PersistDemoShadowAsync`. SHADOW only? Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write from E007. |
| Test source modified | **No.** |
| SUT | `TraderIntelligence.Infrastructure.Persistence.EfTradingStore.PersistDemoShadowAsync` |
| SUT file | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` lines **251–337** |
| SUT SHA-256 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` |
| SUT size / physical lines / mtime | 12097 B / 338 / 2026-08-18 08:05:59Z |
| Git blob (`git hash-object`) | `543c143241402cb986cc8783db2e4ffdea64ec3a` (**untracked** `??`) |
| Only product caller | `ReconstructionScoringService.RebuildTraderAsync` (`DealIngestionService.cs` L104) |
| Binding law | Architecture §§1.4, 15, 18–24, 39, 41, 63–64, 68–70; A22; A24; A41; A69 |
| Same-SHA predecessors (not copied as this verdict) | D45 (outbox always), D47 (CopyIntent after SHADOW score), D48 (6 shadow rows), D97 (no LIVE) |
| Method | Full read of store method + port + caller + entities + scorer machine + engine + seeder + both workers + API resync. Product `*.cs` writer census. File hashes. `dotnet build` Infrastructure. Prefer false negatives over fake PASS. |

**Assigned answer: YES for `copy_intents` / `shadow_orders`. NO for the method as a whole. NEVER LIVE.**

**One-line:** `PersistDemoShadowAsync` always writes a `ScoreUpdate` outbox row for **any** `TraderState`; it `return`s without `new CopyIntent` / `new ShadowOrder` unless `state == TraderState.SHADOW` (and a `destination_quotes` row exists); `Status` is the literal `"SHADOW_ONLY"`; there is no `ExecutionIntent`, no FIX send, no `RiskEngine` consult.

---

## 0. Verdict (binding)

| Question | Result | Class |
|---|---|---|
| Do `CopyIntent` + `ShadowOrder` persist only when `state == SHADOW`? | **Yes.** Hard `if (state != TraderState.SHADOW) { SaveChanges; return; }` at L267–271 | `EXISTS_AND_GOOD` as a demo gate |
| Is `CopyIntent.Status` always `"SHADOW_ONLY"` on this path? | **Yes.** Literal at L307. Only `SHADOW_ONLY` token in product `*.cs` | demo tag, not an enum |
| Does the method run only for SHADOW traders? | **No.** `RebuildTraderAsync` always calls it with `score.SuggestedState` | invocation is **not** SHADOW-only |
| Does it write anything for non-SHADOW states? | **Yes.** `OutboxEventType.ScoreUpdate` (int `1`) for every rebuild | outbox is **not** SHADOW-only |
| Can it persist `TraderState.LIVE` / `LIVE_CANDIDATE` rows here? | **No.** Those tokens never appear in the method. LIVE fails the `== SHADOW` gate | SAFE_BY_GATE |
| Does it create live execution (`ExecutionIntent`, `35=D`, `NewOrderSingle`)? | **No.** Zero `new ExecutionIntent` / `ExecutionIntents.Add` in `src/` + `apps/` | SAFE_BY_ABSENCE |
| Does it consult `RiskEngine` / `CanPromoteToLive` / `CopyIntentExpiry`? | **No** | demo backfill |
| Is this Architecture §24 / A24 shadow copy? | **No.** Completed-trade OPEN replay vs a seeder-invented book | `DEMO`, not Phase 5 |
| Tests lock the SHADOW gate? | **No.** Zero `PersistDemoShadow` / `SHADOW_ONLY` / `CopyIntents` facts | `UNLOCKED` |
| Product source edited by E007 | **No** | report only |

```text
SHADOW-only (copy + shadow rows)   YES   -- if (state != TraderState.SHADOW) return
SHADOW-only (method / outbox)      NO    -- ScoreUpdate always
LIVE persist / live send           NO    -- never written, never sent
A24 / §24 book                     NO    -- demo backfill
measured                           2026-08-18T13:48:36+05:30
store SHA                          DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36
Infrastructure build               GREEN 0 warning / 0 error (this pass)
```

Do **not** claim “the persist path is SHADOW-only” without the outbox caveat. Do **not** claim “SHADOW_ONLY status is a live-send interlock” — nothing reads that string. Do **not** claim A24 / G14 / §69.11. Do **not** treat D16 “zero callers”, C59 “zero CopyIntent writers”, or D47 “Infrastructure RED” as current.

---

## 1. File identity (measured this pass)

| Path | SHA-256 | Bytes | Physical | LastWriteUtc |
|---|---|---:|---:|---|
| `src\Infrastructure\Persistence\EfTradingStore.cs` | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 12097 | 338 | 2026-08-18 08:05:59Z |
| `src\Application\Ingestion\DealIngestionService.cs` | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 4535 | 106 | 2026-08-18 08:05:29Z |
| `src\Domain\Enums\TraderState.cs` | `E509C59F1597EE0F424A9F9408D0B27B2C7063D724B3D0A63880E2558B930D68` | 264 | 15 | 2026-08-18 07:33:45Z |
| `src\Domain\Entities\CopyIntent.cs` | `C9AE3FF95058B72FC00A4DDBCBF2DFD68B7637D00D321244C376E2A1D6D9148B` | 951 | 24 | 2026-08-18 08:10:10Z |
| `src\Domain\Entities\ShadowOrder.cs` | `8EF2D2372CFC01A27CBCA4A1855A322B54A4439FCB6B11AA3A5404FD0D1F8B86` | 556 | 17 | 2026-08-18 07:39:03Z |
| `src\Domain\Entities\OutboxEvent.cs` | `78108643D4C8E25DBEA767C30145366B3337C59D6E39EA3F613B480CDE6649A8` | 546 | 16 | 2026-08-18 08:10:10Z |
| `src\Domain\Enums\OutboxEventType.cs` | `163ED842EE9AF0C94EA912A91845F31C8644F2A1A373A67C77E7FA16154BAADA` | 211 | 11 | 2026-08-18 07:34:07Z |
| `src\Domain\Shadow\ShadowCopyEngine.cs` | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | 3249 | 93 | 2026-08-18 07:38:10Z |
| `src\Domain\Scoring\BaselineScorer.cs` | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 8143 | 212 | 2026-08-18 07:38:10Z |
| `src\Infrastructure\Seeding\DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 5082 | 140 | 2026-08-18 08:04:59Z |
| `src\Domain\Entities\ExecutionIntent.cs` | `56DC9ED8E4DAC442A66620386864F919B34F851FF22974CA2FBC23B0A5CC3617` | 783 | 21 | 2026-08-18 08:07:43Z |
| `src\Domain\Risk\RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | 8567 | 176 | 2026-08-18 07:38:10Z |
| `src\Domain\Execution\CopyIntentExpiry.cs` | `76B82E4F0C6F6B43988D5E50EE5E5D229CC451C7E8267AD6DF56271790531D38` | 246 | 7 | 2026-08-18 07:38:10Z |
| `src\Domain\Entities\DestinationQuote.cs` | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | 421 | 13 | 2026-08-18 08:09:32Z |
| `apps\mt5-worker\Worker.cs` | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 1882 | 45 | 2026-08-18 07:45:01Z |
| `apps\api\Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 4731 | 95 | 2026-08-18 08:05:15Z |
| `tests\Integration\SeedingAndStoreTests.cs` | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | 3119 | 74 | 2026-08-18 07:47:42Z |

Git (this pass):

| Path | Worktree |
|---|---|
| `EfTradingStore.cs` | **untracked** (`??`); blob `543c1432…` |
| `DealIngestionService.cs` | **modified** (` M`); blob `71b2c922…` |
| `CopyIntent.cs` | **modified** (` M`); blob `81fd63b2…` |
| `ShadowOrder.cs` / `BaselineScorer.cs` / engine | clean vs the same SHAs D12/D48 already pinned |

Store SHA matches D45/D47/D48/D97. This file is a **reconfirm + SHADOW-only split**, not a new implementation.

---

## 2. The method (verbatim control flow)

```251:337:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task PersistDemoShadowAsync(
        Guid brokerId,
        long login,
        TraderState state,
        IReadOnlyList<ReconstructedTradeResult> completedXau,
        CancellationToken ct)
    {
        _db.OutboxEvents.Add(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            Type = OutboxEventType.ScoreUpdate,
            AggregateId = $"{brokerId}:{login}",
            PayloadJson = $"{{\"state\":\"{state}\",\"completed\":{completedXau.Count}}}",
            OccurredAt = DateTimeOffset.UtcNow
        });

        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var quoteRow = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);
        if (quoteRow is null)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var engine = new TraderIntelligence.Domain.Shadow.ShadowCopyEngine();
        // ...
        foreach (var trade in completedXau.Where(t => t.Completed).OrderBy(t => t.ClosedAt))
        {
            var key = $"shadow:{brokerId}:{login}:{trade.PositionId}";
            if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == key, ct))
                continue;

            var intent = new CopyIntent
            {
                // ...
                Action = CopyIntentAction.OpenExposure,
                Status = "SHADOW_ONLY",
                IdempotencyKey = key
            };
            _db.CopyIntents.Add(intent);

            var fill = engine.SimulateEntry(/* 80 ms delay */);
            _db.ShadowOrders.Add(new ShadowOrder { /* fill fields */ });
        }

        await _db.SaveChangesAsync(ct);
    }
```

Facts that follow from the text, not from a comment:

1. The first statement is **unconditional** `OutboxEvents.Add`. There is no `if (state == SHADOW)` around it.
2. The only SHADOW gate is L267. Every other `TraderState` (`INSUFFICIENT_DATA`, `EARLY_SCORE`, `WATCH`, `LIVE_CANDIDATE`, `LIVE`, `PAUSED`, `RISK_BLOCKED`, `DISQUALIFIED`) takes the early return after committing the outbox row.
3. Passing `LIVE` would **not** write copy/shadow rows. The method does not special-case LIVE; LIVE is just “not SHADOW.”
4. A second gate (`quoteRow is null`) can skip copy/shadow even for SHADOW. Demo seed plants one invented `destination_quotes` row first, so the gate passes on first boot.
5. `Status = "SHADOW_ONLY"` is a string, not `TraderState`, not an enum. Default entity status is `"Pending"` (`CopyIntent.cs` L20); this path overwrites it.
6. `Action` is hardcoded `OpenExposure`. No `Increase` / `Reduce` / `Close`. `SimulateExit` is never called from product code.
7. Idempotency is `AnyAsync` on `IdempotencyKey = shadow:{brokerId}:{login}:{positionId}`. EF also declares a unique index on that column (`TraderDbContext` L126). Outbox has **no** equivalent key — new `Guid` every call.
8. `CopyIntent.Direction` and `SourcePositionId` are **not assigned**. Defaults are `Long` (0) and `0`. `ShadowOrder.Direction` **is** copied from `trade.Direction`. Login 10001 position 502 is a short on the Fake tape; the intent row would lie about direction.
9. `ExpiresAt = trade.OpenedAt.AddSeconds(15)`. Demo opens are 2026-06-01/02. Vs `UtcNow` the intents are already expired. `CopyIntentExpiry.IsExpired` is never called.
10. Modeled delay is `TimeSpan.FromMilliseconds(80)`. Engine only overlays `DefaultLatencySlippagePoints` (0.05) when delay **> 250 ms**. Overlay does not apply.
11. `new ShadowCopyEngine()` is constructed in the store. The engine is **not** in DI.
12. `RiskDecisionId` / `ExecutionIntentId` stay null.

---

## 3. Split answer to “SHADOW only?”

### 3.1 YES — row writers for copy + shadow

| Writer | Product sites (`src/` + `apps/`, exclude `bin`/`obj`) | Gate |
|---|---|---|
| `new CopyIntent` / `CopyIntents.Add` | **1** — store L295 / L310 | `state == SHADOW` ∧ dest quote ∧ new key |
| `new ShadowOrder` / `ShadowOrders.Add` | **1** — store L321 | same |
| `"SHADOW_ONLY"` | **1** — store L307 | same |

`ITradingStore` has **one** implementation: `EfTradingStore`. There is no second persist path.

### 3.2 NO — the method is a score-rebuild side-effect

Caller (only product call of the port):

```86:104:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            // ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

`RebuildTraderAsync` always ends here. It does **not** `if (SuggestedState == SHADOW)` before the call.

`DealIngestionService.SyncBrokerAsync` does **not** call persist. Ingest is groups/accounts/deals/positions only.

Indirect callers of `RebuildTraderAsync` (therefore of persist):

| Caller | When |
|---|---|
| `DemoSeeder.SeedAsync` L134–138 | first empty-store boot; logins `{10001,10002,10003,99001}` |
| `apps/mt5-worker/Worker.cs` L31–35 | every **30 s** |
| `apps/api/Program.cs` `/api/ops/resync` L79–80 | on demand |
| Integration test | via seeder |

FIX worker does **not** call persist. It only stamps `FixSessionStatus.Disconnected`.

### 3.3 NO — outbox is not SHADOW-only

| Writer | Product sites | Gate |
|---|---|---|
| `new OutboxEvent` / `OutboxEvents.Add` | **1** — store L258 | **none** (every rebuild) |
| `OutboxEventType.ShadowCopyIntent` | **0** | enum exists; unused |
| Outbox drain / `ProcessedAt` setter | **0** | pending forever |

Payload interpolates the incoming `state` as a string (`"SHADOW"`, `"RISK_BLOCKED"`, …). That is a score notification, not a shadow-copy command.

### 3.4 YES — never LIVE execution

| Token / writer | Product sites | Role |
|---|---|---|
| `new ExecutionIntent` / `ExecutionIntents.Add` | **0** | table exists; no writer |
| `TraderState.LIVE` assignment | **0** in `src/` + `apps/` | dashboard **counts** only (`EfDashboardQueries` L32–33) |
| `CanPromoteToLive` | definition + **1** unit fact | `=> false`; persist does not call it |
| `RiskEngine.Evaluate` from persist | **0** | engine unused on this path |
| `NewOrderSingle` / `35=D` from persist | **0** | FIX worker still refuses send |
| `RealCopyEnabled` overview field | literal `false` in `GetOverviewAsync` last ctor arg | not read by persist |

`TraderStateMachine.FromBaseline` reachable set (every `return`, L189–206): `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. **`LIVE` and `LIVE_CANDIDATE` are unreachable.** Today `SuggestedState` cannot be LIVE, so persist cannot be handed LIVE except by a future scorer change or a hand-written call. If that happens, this method still will **not** write copy/shadow (not SHADOW) and still will **not** write `ExecutionIntent`.

---

## 4. What a first seed actually writes (same SHA as D48)

`DemoSeeder` does not contain the token `ShadowOrders`. It plants one dest quote (`Bid=2399.45`, `Ask=2399.85`, `VenueInstrumentId=null`), ingests the Fake tape, then rebuilds four logins.

D48 eval (`D:\Prop\reports\swarm\20260818\_tmp_d48_shadow\stdout.txt`) on these **same** store/seeder/scorer/fake SHAs:

| Login | `SuggestedState` | Outbox | `CopyIntent` | `ShadowOrder` |
|---:|---|---:|---:|---:|
| 10001 | `SHADOW` (N=3, q=95.50, r=10) | 1 | **3** | **3** |
| 10002 | `RISK_BLOCKED` (losing martingale) | 1 | 0 | 0 |
| 10003 | `INSUFFICIENT_DATA` (N=0) | 1 | 0 | 0 |
| 99001 | `SHADOW` (N=3, q=95.50, r=10) | 1 | **3** | **3** |
| **Total** | | **4** | **6** | **6** |

All six intents: `status=SHADOW_ONLY`, `action=OpenExposure`, keys `shadow:{broker}:{login}:{501\|502\|503\|701\|702\|703}`.

Slippage sum `248.20` = dashboard `ShadowPnl`. That is **Σ `SourceVsShadowSlippage`**, not P&L. Ask−source on longs vs a 2399 book against June 2320–2356 entries.

Second `SeedAsync`: early-return on `Brokers.Any` — counts stay 6 / 6 / 4.

Second `RebuildTraderAsync`: copy+shadow stay 6 (key skip); outbox grows **4 → 8** (not idempotent). The 30 s mt5-worker loop is that Nth caller.

10002/10003 prove the SHADOW gate in data: persist **ran**, outbox **exists**, copy/shadow **absent**.

---

## 5. Why this is still not A24 / §24

| A24 / §24 requirement | This method |
|---|---|
| Per-source-event factory (open/increase vs reduce/close) | Full-history OPEN backfill of **completed** XAU trades |
| Destination QUOTE FIX as price authority | Latest `destination_quotes` row; seeder-invented; no venue id; no age check |
| Fail closed on missing quote (OPEN) | Returns after outbox; no shadow row (good) **and** no fail signal |
| No blind catch-up of stale opens (§63) | **Intentionally** backfills every completed trade on first SHADOW rebuild. Name `PersistDemoShadow` admits demo |
| `RiskEngine` before any modeled fill that could later promote | Unused |
| Persist order + fill + position + pnl tables | One fill-shaped `shadow_orders` row; `SimulateExit` unused; no `shadow_position` / `shadow_pnl` |
| `OutboxEventType.ShadowCopyIntent` | Unused; writer emits `ScoreUpdate` |
| Quote freshness / expiry evaluation | Writes `ExpiresAt`; never reads it |
| Quantity normalization (§38) | `RequestedQuantity = trade.MaxVolumeLots` (source lots) |
| Not a live send | **Holds.** No TRADE socket. Worker log: “Execution copy is not performed here.” |

Classification: **`DEMO` persist of SHADOW-tagged rows.** Not a send gate. Not a promotion gate. Not Phase 5.

---

## 6. Compile (this pass — D47 RED is stale)

```text
dotnet build D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj --nologo -v q
Build succeeded.  0 Warning(s)  0 Error(s)  00:00:00.91
EXIT=0
```

Measured 2026-08-18T13:47:42+05:30. `CopyIntent` / `OutboxEvent` initializers in the method match the current entity shapes. D47 “entity rewrite 13:37 vs writer 13:35 / Infrastructure RED” is **closed** for this SHA pair.

---

## 7. Tests (none lock the question)

| Pattern in `tests/**/*.cs` | Hits |
|---|---:|
| `PersistDemoShadow` | **0** |
| `SHADOW_ONLY` | **0** |
| `CopyIntents` / `ShadowOrders` count | **0** |
| `TraderState.LIVE` | 1 — seed test `10001.CurrentState.Should().NotBe(LIVE)` |
| `CanPromoteToLive` | 1 — unit fact on the `SHADOW` argument only |

`SeedingAndStoreTests` does not assert outbox / intent / shadow counts. A 10002 `RISK_BLOCKED` + zero-intent fact would lock the gate; it is not written.

---

## 8. Stale reports (do not cite as current)

| Report | Claim | Now |
|---|---|---|
| D16 / C45 | `ShadowCopyEngine` unused; zero product callers | **Stale.** Store constructs it at L280 |
| C59 | Zero `CopyIntent` writers | **Stale.** This method is the only writer |
| D22 | Seeder does not write Outbox/Copy/Shadow | **Stale** for first-run side-effect; still true that seeder text has no `ShadowOrders` token |
| D47 | Infrastructure does not compile | **Stale this pass** (GREEN) |
| D20 | Store has 8 methods, no shadow persist | **Stale.** 9 methods; this is the ninth |

D45 / D47 / D48 / D97 control-flow facts **hold** on the same store SHA. This file adds the explicit SHADOW-only split.

---

## 9. What E007 does **not** claim

- That `"SHADOW_ONLY"` is enforced anywhere except as a stored string.
- That LIVE promotion is gated by this method. Promotion is impossible today because `FromBaseline` cannot emit LIVE **and** `CanPromoteToLive => false` is unused on the persist path (D97).
- That outbox processing exists. Four pending `ScoreUpdate` rows after seed are not A41.
- That unique-index idempotency is proven on PostgreSQL. `AnyAsync` + InMemory is not that proof (D37).
- That `Direction` on `CopyIntent` is correct. It is not set.
- That §68 / §70 / A57 scores move. They do not.

---

## 10. Paths read (no product edits)

- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`PersistDemoShadowAsync`, `UpsertScoreAsync`)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`ITradingStore`, `RebuildTraderAsync`)
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Domain\Entities\CopyIntent.cs`
- `D:\Prop\src\Domain\Entities\ShadowOrder.cs`
- `D:\Prop\src\Domain\Entities\OutboxEvent.cs`
- `D:\Prop\src\Domain\Entities\ExecutionIntent.cs`
- `D:\Prop\src\Domain\Entities\DestinationQuote.cs`
- `D:\Prop\src\Domain\Enums\OutboxEventType.cs`
- `D:\Prop\src\Domain\Enums\CopyIntentAction.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (`FromBaseline`, `CanPromoteToLive`)
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\_tmp_d48_shadow\stdout.txt` (same-SHA seed measurement)

---

*End of E007. Product source was not modified. Answer: copy/shadow rows are SHADOW-only; the method and its outbox are not; LIVE is never written or sent.*
