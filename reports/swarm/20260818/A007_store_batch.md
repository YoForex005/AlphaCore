# A007 — Store batch: per-deal `SaveChanges`, uniques, dummy shadow, ALL-ingest gaps

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A007_store_batch.md` |
| Agent | A007 (senior engineer, store batch / scale / unique / shadow naming) |
| Date | 2026-08-18 |
| Assigned | Read `EfTradingStore.cs` and `TraderDbContext.cs`. Is every deal a separate `SaveChanges` (will not scale to all manager deals)? Unique indexes? Dummy `PersistDemoShadowAsync` naming vs real shadow? Gaps to ingest ALL groups/accounts/deals. No secrets. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| SUT | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (342 lines, 9 `SaveChangesAsync` sites) |
| Schema | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` (174 lines, fluent maps in-context) |
| Caller | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`SyncBrokerAsync` + `RebuildTraderAsync`) |
| Connector | `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (`IMt5BulkDealReader`) |
| Binding | Architecture v2 §§3, 10–13, 24, 59, 69.3; A24; A41; A59; A61; A78; A98 |
| Honesty | Sequential `AnyAsync` + `Add` + `SaveChanges` is not `INSERT … ON CONFLICT`. A unique fluent index is not a migration applied to Postgres. `PersistDemoShadowAsync` is not §24. Key names only — no passwords, no connection strings, no proxy auth. |

**Assigned answers (one line each):**

1. **Yes — every new deal is its own `SaveChangesAsync`.** `UpsertDealAsync` inserts one `Mt5Deal` then commits. `SyncBrokerAsync` awaits that inside `foreach (var deal in deals)`. That will **not** scale to a full Manager census (~5k accounts / 10⁵–10⁶ deals).
2. **Unique indexes exist in fluent config for the raw identity keys.** `(BrokerId, DealTicket)`, `(BrokerId, Login)`, `(BrokerId, Name)` are unique. Several ingest-critical tables are **not** unique (`reconstructed_trades`, `shadow_orders`). Check-then-insert is still TOCTOU.
3. **The name is honest: it is a demo backfill, not real shadow.** Production `RebuildTraderAsync` still calls it. It always writes a `ScoreUpdate` outbox; `copy_intents` + `shadow_orders` only when `state == SHADOW` and a `destination_quotes` row exists. Not A24 / not live send.
4. **ALL-ingest is not implemented.** No batch, no checkpoint, time-windowed deals, positions capped at 200 logins on the ingest path, worker scores 4 hard-coded logins, deal `Reason` dropped, group/account update paths incomplete.

---

## 0. Verdict

| Question | Measured | Class |
|---|---|---|
| One `SaveChanges` per deal? | **Yes.** `EfTradingStore.UpsertDealAsync` L112. No `AddRange`, no `BeginTransaction`, no `ExecuteSql` / `COPY`. | **UNSAFE** at Manager scale |
| One `SaveChanges` per group / account / position-replace / score / shadow? | **Yes.** Every mutating method ends with its own commit. | **UNSAFE** as a unit of work |
| Will this ingest all manager deals? | **No.** Per-row TX + extra `AnyAsync` SELECT + 30/90-day window + no paging + no `sync_checkpoints` write. | **FAIL** vs §69.3 |
| Unique indexes on identity? | **Yes in `OnModelCreating`:** deals `(BrokerId, DealTicket)`, accounts `(BrokerId, Login)`, groups `(BrokerId, Name)`. | `EXISTS_AND_GOOD` as model; **unproven** as applied PG |
| Unique on reconstructed / shadow / outbox? | **No.** Recon index is non-unique. `shadow_orders` PK only. Outbox unique **absent**. | **GAP** |
| Idempotent SQL? | **No.** `0` `ON CONFLICT` in the store. First-write-wins via `AnyAsync` then `Add`. | `EXISTS_NEEDS_REFACTOR` |
| `PersistDemoShadowAsync` = real shadow? | **No.** Name + policy = demo OPEN replay of **completed** XAU vs latest dest quote. Engine type is real (`ShadowCopyEngine`); pipeline is not. | **DEMO** |
| Gaps to ALL groups / accounts / deals? | Listed in §5. Connector *can* walk all groups; store / worker / DTO / window / cap cannot finish the job. | **FAIL** |
| Secrets in this report | **None.** Env **key names** only (`MT5_PASSWORD`, `DATABASE_URL`, …). | — |

```text
per-deal SaveChanges          YES
batch upsert / COPY           MISSING
unique (broker, ticket)       YES (fluent)
ON CONFLICT DO NOTHING        MISSING
sync_checkpoints writer       MISSING (DbSet exists)
PersistDemoShadow = A24       NO (demo name + completed-trade backfill)
ALL manager deals             NO
```

Do **not** claim “idempotency proven.” Do **not** claim “shadow is live.” Do **not** claim “5k accounts ingestable.” Do **not** paste connection strings or Manager passwords.

Companions (do not copy as current hashes — this store is **342** lines; D20 was 250, D95/E007 were ~310–338): `D20_store.md`, `D95_scale.md`, `E007_shadow.md`, `D45_outbox.md`, `D46_checkpoint.md`, `C58_outbox_dispatcher.md`, `A78_deal_idempotency.md`, `A24_shadow_copy_spec.md`, `A61_efcore_schema.md`.

---

## 1. What was read (no product edits)

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Entire file. 10 public methods. **9** `SaveChangesAsync`. |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | Entire `OnModelCreating`. 20 `DbSet`s. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `ITradingStore` + `SyncBrokerAsync` + `RebuildTraderAsync` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | DTO field sets; `IMt5BulkDealReader` |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Manager walk: `GroupTotal` / `UserGetByGroup` / `DealRequestByGroup` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Demo 4 groups / 4 accounts / 18 deals; **not** `IMt5BulkDealReader` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | One-shot −90d ingest; scores **all** `ListLoginsAsync` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 30 s loop; −30d; scores **only** `{10001,10002,10003,99001}` |
| `D:\Prop\src\Domain\Entities\Mt5{Deal,Group,Account,Position}.cs` | Persist shapes |
| `D:\Prop\src\Domain\Entities\{CopyIntent,ShadowOrder,OutboxEvent,SyncCheckpoint}.cs` | Shadow / unused checkpoint |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Real fill math; 80 ms delay below 250 ms overlay |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | `Reason` exists; store never maps it |
| `D:\Prop\src\Domain\Enums\DealReason.cs` | `CountsAsTraderActivity(null)` returns **true** (fail-open) |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Store scoped; live connectors required |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two native connectors (key **names** only) |

Grep of `EfTradingStore.cs` (this read):

| Token | Count |
|---|---|
| `SaveChangesAsync` | **9** |
| `BeginTransaction` / `Database.BeginTransaction` | **0** |
| `AddRange` (deals) | **0** (only implicit via loop `Add`) |
| `ON CONFLICT` / `ExecuteSql` / `Copy` / `NpgsqlBinaryImporter` | **0** |
| `SyncCheckpoint` | **0** |
| `ingestion_events` | **0** |
| `payload_hash` | **0** |
| `PersistDemoShadowAsync` | **1** method |
| `new ShadowCopyEngine()` | **1** (constructed inside persist; not DI) |

---

## 2. Every deal is a separate `SaveChanges` — will not scale

### 2.1 Store

```85:113:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task<bool> UpsertDealAsync(Guid brokerId, Mt5DealDto deal, DateTimeOffset now, CancellationToken ct)
    {
        var exists = await _db.Mt5Deals.AnyAsync(
            d => d.BrokerId == brokerId && d.DealTicket == deal.DealTicket, ct);
        if (exists)
            return false;

        _db.Mt5Deals.Add(new Mt5Deal { /* one row */ });
        await _db.SaveChangesAsync(ct);
        return true;
    }
```

Cost of **one new deal** on Postgres:

1. `SELECT` (`AnyAsync` on unique `(BrokerId, DealTicket)`).
2. change-tracker `Add`.
3. implicit transaction `BEGIN` → `INSERT` → `COMMIT`.
4. return to the ingest loop.

A duplicate still pays the `SELECT` then returns `false` (no second insert). There is no batch skip.

### 2.2 Caller

```49:71:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                foreach (var deal in deals)
                {
                    if (await _store.UpsertDealAsync(brokerId, deal, now, ct))
                        insertedDeals++;
                }
            }
        }
        else
        {
            foreach (var account in accounts)
            {
                var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
                foreach (var deal in deals)
                {
                    if (await _store.UpsertDealAsync(brokerId, deal, now, ct))
                        insertedDeals++;
                }
            }
        }
```

Native connector **is** `IMt5BulkDealReader`, so live ingest takes the group path. That only changes **how deals are fetched** (`DealRequestByGroup` vs per-login `DealRequest`). Persistence is still **one commit per ticket**.

`NativeMt5BrokerConnector.GetGroupDealsCore` materializes the **entire** `CIMTDealArray` into a `List<Mt5DealDto>` before any persist. A busy group over a 30–90 day window can be tens of thousands of rows in one managed list, then N round-trips to write them.

### 2.3 Commit map (every public write)

| Method | `SaveChangesAsync` | Granularity | Same-TX companions |
|---|---|---|---|
| `UpsertGroupAsync` | L50 | 1 group | none |
| `UpsertAccountAsync` | L82 | 1 account | none |
| `UpsertDealAsync` | L112 | **1 deal** | none (no outbox, no checkpoint) |
| `ReplacePositionsAsync` | L141 | 1 login wipe+insert | none |
| `ReplaceReconstructedAsync` | L212 | 1 login wipe+insert | none |
| `UpsertScoreAsync` | L248 | 1 current score + **1 new history row** | none |
| `PersistDemoShadowAsync` | L269 / L276 / L336 | 1 outbox ± N intents/orders | outbox **not** with raw deal |

`ITradingStore` has **no** `SaveChanges` / unit-of-work / `AddDealNoSave` method. The application layer cannot batch even if it wanted to.

### 2.4 Scale arithmetic (order of magnitude, not a bench)

Assume a Manager book in the architecture envelope (§3 / §69.3: ~5,000 accounts).

| Step in `SyncBrokerAsync` | Commits |
|---|---|
| Groups (say 80) | 80 |
| Accounts (5,000) | 5,000 |
| Deals (say 100 closed / account / 90 d ≈ 500,000) | **500,000** |
| Positions (`Take(200)` only) | 200 |
| **Ingest subtotal** | **~505,280 TX** |
| Then `RebuildTraderAsync` per login (live host walks **all** logins): recon replace + score + shadow | **×3 commits × 5,000 = 15,000** more |

Plus one `AnyAsync`/`SingleOrDefaultAsync` per write. Plus full `LoadDealsAsync` of every deal for that login on rebuild (unpaged).

This is **not** a 5k-capable writer. A78 / A61 required `INSERT … ON CONFLICT (broker_id, deal_ticket) DO NOTHING` in **one** transaction with the outbox / checkpoint. The store does the opposite.

EF InMemory in tests hides the cost and does **not** enforce the unique index the same way Postgres `23505` does. `SeedingAndStoreTests` is not a 5k proof.

### 2.5 Other write methods that also will not scale

- **`ReplacePositionsAsync`:** `RemoveRange` of all current rows for the login (LINQ `Where` — tracked), then N `Add`, one commit. Concurrent two workers → unique `(BrokerId, PositionTicket)` race.
- **`ReplaceReconstructedAsync`:** same wipe+insert; **no unique** on `(BrokerId, Login, PositionId)` → two rebuilds racing **duplicate** rows.
- **`UpsertScoreAsync`:** last-write-wins current row; **always appends** `trader_score_history`. Worker every 30 s × 4 logins = unbounded history even on the demo set.
- **`ListLoginsAsync`:** `ContinueWith` on the same `Task` — not a batch bug, but loads every login Guid-free long into memory.

---

## 3. Unique indexes (`TraderDbContext.OnModelCreating`)

Fluent map is the **only** schema source (`Persistence\Configurations\` is empty). There is no evidence in this pass that a Postgres migration has been applied; uniques below are **model intent**.

### 3.1 Unique (identity / idempotency relevant)

| Table | Index | Used by store as |
|---|---|---|
| `brokers` | `Code` unique | `ResolveBrokerIdAsync` `SingleAsync` |
| `mt5_groups` | `(BrokerId, Name)` unique | group upsert lookup |
| `mt5_accounts` | `(BrokerId, Login)` unique | account upsert lookup |
| `mt5_deals` | `(BrokerId, DealTicket)` unique | deal existence + A78 identity |
| `mt5_positions_current` | `(BrokerId, PositionTicket)` unique | replace can 23505 under concurrency |
| `canonical_instruments` | `Code` unique | not written by this store |
| `source_symbol_mappings` | `(BrokerId, SourceSymbol)` unique | not written by this store |
| `trader_scores` | `(BrokerId, Login)` unique | score upsert |
| `sync_checkpoints` | `(BrokerId, Login, Stream)` unique | **never written** |
| `copy_intents` | `IdempotencyKey` unique | demo shadow skip key `shadow:{brokerId}:{login}:{positionId}` |
| `execution_intents` | `ClOrdId` unique | **never written** here |
| `fix_sessions` | `Qualifier` unique | not this store |

### 3.2 Non-unique / missing (ingest gaps)

| Table | Index | Problem |
|---|---|---|
| `mt5_deals` | `(BrokerId, Login, DealTime)` **non-unique** | Good read helper; not a substitute for ticket unique |
| `reconstructed_trades` | `(BrokerId, Login, PositionId, OpenedAt)` **non-unique** | Wipe-replace is the only de-dupe. Race → dupes. New `Guid` every rebuild |
| `trader_score_history` | `(BrokerId, Login, RecordedAt)` **non-unique** | Append-only forever |
| `outbox_events` | `ProcessedAt` **non-unique** | No unique on `(Type, AggregateId)` → ScoreUpdate spam every rebuild |
| `shadow_orders` | **PK only** | De-dupe is via `CopyIntents.IdempotencyKey`, not the fill row |
| `destination_quotes` | **PK only** | Shadow picks `OrderByDescending(ReceivedAt).FirstOrDefault` — any symbol, any venue |
| `risk_decisions` | `CopyIntentId` non-unique | unused on this path |
| `audit_logs` / `kill_switches` | PK only | unused on this path |

### 3.3 What unique does **not** give you

- **Not** `ON CONFLICT DO NOTHING`. Concurrent `AnyAsync == false` × 2 writers → first commit wins, second **throws** `23505` on Postgres (ingest cycle `catch` logs and retries the whole broker). InMemory may insert **two** rows (D20).
- **Not** payload-hash first-write-wins. A later Manager mutation of the same ticket (cancel / correction) is **silently ignored**. A83 still applies.
- **Not** applied-migration proof. `AddDbContext` uses InMemory when the connection string is missing or contains the placeholder token `<SECRET>` (`DependencyInjection.cs`). Uniques are then theater.

---

## 4. `PersistDemoShadowAsync`: dummy name vs real shadow

### 4.1 Naming

The method is **literally** `PersistDemoShadowAsync` on `ITradingStore` (port L17) and `EfTradingStore` (L251–337). That name is the correct honesty label.

It is **not** a dead demo hook. The only product caller is production scoring:

```125:125:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

`RebuildTraderAsync` always calls it after `UpsertScoreAsync` (already committed). Two transactions: score, then shadow/outbox.

### 4.2 What it actually writes

| Exit | Condition | Written in that `SaveChanges` |
|---|---|---|
| L267–270 | `state != TraderState.SHADOW` | `OutboxEvent` `ScoreUpdate` only |
| L273–277 | no `destination_quotes` row | same outbox only |
| L289–336 | SHADOW + quote | outbox + `CopyIntent` (`Status = "SHADOW_ONLY"`) + `ShadowOrder` per completed XAU position not already keyed |

Always:

- `Type = OutboxEventType.ScoreUpdate`
- `PayloadJson` interpolated string (state + completed count) — **not** a versioned contract
- **No** `ProcessedAt` consumer in this store (D45 / C58: nothing drains)

SHADOW branch:

- Idempotency `shadow:{brokerId}:{login}:{trade.PositionId}` via `CopyIntents.AnyAsync` **per trade** (N extra SELECTs).
- `ExpiresAt = trade.OpenedAt.AddSeconds(15)` — already expired vs `UtcNow` for any historical trade.
- `new ShadowCopyEngine()` (not DI). `SimulateEntry` with **80 ms** modeled delay. Engine only applies the 0.05 point overlay when delay **> 250 ms**, so the 80 ms path is dest touch ± 0 overlay.
- Latest dest quote **without** symbol filter — XAU source can be marked off a quote for any `CanonicalSymbol`.
- Does **not** set `CopyIntent.Direction`, `SourcePositionId`, `SourceTradeId` (entity has them; initializer omits them → `Direction` default 0, position 0).
- **No** `RiskEngine`, **no** `CopyIntentExpiry` check, **no** `ExecutionIntent`, **no** FIX `35=D`.

### 4.3 Real shadow (A24 / §24) vs this method

| A24 / §24 expectation | This method |
|---|---|
| Per new source **event** (open/scale/close) | Replays **completed** reconstructed XAU on every rebuild |
| Fresh dest book, age + reject | Single latest snapshot, no reject, no age gate |
| Risk decision persisted | None |
| Shadow mark-to-market / exit | Entry simulate only |
| LIVE never from this path | Gate is `== SHADOW`; LIVE does not create rows here (good) |
| Same TX as score / outbox processor | Second `SaveChanges` after score; outbox undrained |
| Name / API of a copy engine service | `PersistDemoShadowAsync` + ad-hoc `new ShadowCopyEngine()` |

**Verdict:** the **identifier is dummy**; the **call site is production**. Treat rows as a seeder/rebuild souvenir, not as a measured shadow book. Dashboard sums of `SourceVsShadowSlippage` are not P&L.

---

## 5. Gaps to ingest ALL groups / accounts / deals

Native `GetGroupsCore` / `GetAccountsCore(null)` **do** walk `GroupTotal` and `UserGetByGroup` for every group. The store and workers then throw most of that census away or choke on it.

### 5.1 Persistence / transaction

| Gap | Evidence | Effect on ALL |
|---|---|---|
| Per-row `SaveChanges` | §2 | Hours of commits; connection burn; worker 30 s overlap |
| No `BeginTransaction` spanning group+accounts+deals | 0 hits | Partial census on crash |
| No `sync_checkpoints` writer | `DbSet` + unique exist; store never touches | Every cycle re-requests the same window |
| No `ingestion_events` / payload hash | 0 hits | Cannot audit first-write vs conflict |
| No outbox with raw deal | Outbox only in demo shadow as `ScoreUpdate` | §12 same-TX **MISSING** |
| Deal first-write-wins, no update | `AnyAsync` → return false | Manager corrections / cancels dropped |
| TOCTOU vs unique | check then insert | Dual host (`LiveIngestHostedService` + `mt5-worker`) can 23505 |

### 5.2 Window, paging, Manager API

| Gap | Evidence | Effect on ALL |
|---|---|---|
| Deals time-boxed | Live host **−90 d**; worker **−30 d**; `to = UtcNow+1m` | Older tickets never requested |
| No ticket watermark | `SyncCheckpoint.LastTicket` unused | Cannot resume `DealRequest` |
| Group deal array fully buffered | `GetGroupDealsCore` → `ReadDeals` entire array | Memory + Manager limit (`DealRequestByGroup` is not paged here) |
| No per-group enable filter | Ingest upserts **every** `GetGroupsAsync` name | Contest / covering / manager / test groups included |
| `EnabledForAnalysis` | Insert hard-codes `true`; **never read** by ingest | Cannot exclude a group without code change |
| `PlanMapping` | Entity column; store never sets | Plan filter (A40 / D68) unused on write |
| Positions `Take(200)` | `SyncBrokerAsync` L74 | Accounts 201+ keep **stale or empty** `mt5_positions_current` |
| Worker scores 4 logins | `Worker.cs` `{10001,10002,10003,99001}` | Even a full deal ingest would score four traders |
| Live host scores all logins | `ListLoginsAsync` then `RebuildTraderAsync` | After a real census this is 5k × (load all deals + wipe recon + score + demo shadow) — also will not finish |
| Fake path | Fake is **not** `IMt5BulkDealReader`; 4 accounts / 18 deals | Tests never exercise the bulk group loop |

### 5.3 Field drop — “all deals” is not “full deal”

`Mt5DealDto` / `ReadDeals` / `UpsertDealAsync` / `LoadDealsAsync` omit fields reconstruction already has hooks for:

| Field | On DTO? | On `Mt5Deal`? | On `NormalizedDeal`? | After load |
|---|---|---|---|---|
| `DealReason` | **No** | **No** | Yes (`Reason`) | **Always null** |
| SL / TP | **No** | **No** | Yes | always null |
| Magic / expert | **No** | **No** | **No** | lost |
| Digits / contract / tick | **No** | **No** | **No** | volume/price semantics incomplete |
| Position `Swap` | DTO **no** | Entity **yes** | n/a | always 0 on insert |

`NormalizedDeal.IsTradingDeal` is `Buy|Sell` **and** `DealReasons.CountsAsTraderActivity(Reason)`. `CountsAsTraderActivity(null)` is **true** (`DealReason.cs` L34–35) — missing reason **fail-opens**, so reconstruction still treats loaded deals as trader activity. Rollover / settlement / sync / transfer **cannot** be excluded once Reason was dropped at the Manager read. `TradeReconstructor` filters `.Where(d => d.IsTradingDeal)`.

`ReadDeals` never calls `d.Reason()` on `CIMTDeal` even though the native object has it. That is an ALL-deals **semantic** hole (A82 / A83 / D44 / D73), not a volume hole.

### 5.4 Group / account upsert incompleteness

**Group insert** copies currency, digits, company, margin, connections. **Group update** writes only `Currency` + `LastSyncedAt`. Digits / company / margin / connections **stale** after first see.

**Account insert** copies leverage, margin, profit. **Account update** writes `GroupName`, `Balance`, `Equity`, `LastSyncedAt` only. Leverage / Margin / MarginFree / Profit **stale**. `RegistrationAt` / `LastAccessAt` never set.

So “all accounts ingested” can still be a **partial snapshot**.

### 5.5 Dual writers / hosts

| Host | Brokers | Window | Score set |
|---|---|---|---|
| `LiveIngestHostedService` (Infrastructure DI) | `registry.All()` (Achiever + Starwave native) | −90 d, **once** at startup | every login in `mt5_accounts` |
| `apps/mt5-worker` `Worker` | hard-coded Achiever + Starwave | −30 d, **every 30 s** | four canned logins |

Both call the same per-deal store. Neither coordinates a lease. Both can run if both processes share a database.

### 5.6 Connector “all groups” is unfiltered on purpose — and unscoped

`GetAccountsAsync(null)` (ingest always passes `null`) enumerates **every** Manager group. That is the right primitive for ALL, but:

- no allow-list / deny-list;
- no `demo\` vs `real\` policy;
- contest group `contest\yo-2step` is first-class in the Fake and would be first-class live;
- `DealRequestByGroup` for a covering/manager group can be enormous.

ALL without policy is as dangerous as ALL without batch.

### 5.7 What would be required (not implemented — do not treat as done)

1. Port: `UpsertDealsBatchAsync` / raw SQL `COPY` or `INSERT … ON CONFLICT DO NOTHING` returning inserted tickets.
2. One transaction per **page** (group × time slice or ticket range), including `sync_checkpoints` + outbox.
3. Checkpoint `LastTimestamp` / `LastTicket` per `(BrokerId, Login|Group, Stream)`.
4. Persist `DealReason` (and cancel/correction policy).
5. Remove `Take(200)` or page positions.
6. Score loop = ingested logins with a budget, not `{10001…}`.
7. Stop calling `PersistDemoShadowAsync` from the live rebuild path, or replace it with an A24 event factory.
8. Applied Postgres migrations matching the fluent uniques.
9. Single ingest owner (lease / `fix_sessions`-style lock analog for MT5 pump).

---

## 6. Secrets

This report contains **no** passwords, connection strings, proxy auth, or Manager login numbers from config values.

Referenced **key names** only (from `LiveMt5Registration` / DI, not values):

- `ConnectionStrings:TraderIntelligence` / `DATABASE_URL`
- `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`
- `MT5_SERVER`, `MT5_PORT`, `MT5_LOGIN`
- `MT5_STARWAVEFX_SERVER`, `MT5_STARWAVEFX_PORT`, `MT5_STARWAVEFX_LOGIN`
- `ACHIEVER_PROXY_ENABLED`, `ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD`

`DependencyInjection` refuses to start without both password keys present and not equal to the `<SECRET>` placeholder. That is a fail-closed gate, not a credential.

---

## 7. Classification summary

| Item | Class |
|---|---|
| Per-deal `SaveChanges` | **UNSAFE** (will not scale to all Manager deals) |
| Fluent unique `(BrokerId, DealTicket)` | `EXISTS_AND_GOOD` as model |
| Applied PG unique + `ON CONFLICT` | **MISSING** / unproven |
| Unique reconstructed / shadow_orders | **MISSING** |
| `sync_checkpoints` usage | **MISSING** (schema only) |
| `PersistDemoShadowAsync` | **DEMO** name, **production** caller, **not** A24 |
| Ingest ALL groups (connector walk) | `EXISTS_AND_GOOD` as fetch primitive |
| Ingest ALL accounts (persist + positions + score) | **FAIL** (`Take(200)`, worker 4 logins, per-row TX) |
| Ingest ALL deals (history + fields + batch) | **FAIL** |

**One-line close:** The store is a correct **single-threaded demo upsert**. It is not a Manager-scale batch ingest, not a transactional outbox, and `PersistDemoShadowAsync` must not be counted as real shadow copy.
