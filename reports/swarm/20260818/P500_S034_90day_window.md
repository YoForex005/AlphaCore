# P500_S034 — 90-day deal window biases scores

| Field | Value |
|---|---|
| Slot | **S034** |
| Agent | P500_S034 (read-only ingest/score window) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Files | `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`; `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| Adjacent | `ReconstructionScoringService` (same file as ingest), `EfTradingStore.LoadDealsAsync`, `BaselineScorer`, `NativeMt5BrokerConnector.Windows`, `apps/api/Program.cs` `/api/ops/resync`, `apps/mt5-worker/Worker.cs` |
| Angle | A **90-day** deal pull can **miss older skill** or include **only a recent challenge burst**. That tape is what `BaselineScorer` ranks. |
| Product source modified | **No.** This report is the only write. |
| Method | Full `read_file` of both assigned files. `grep` for `AddDays(-90)`, `from, to`, `LoadDealsAsync`, `GetGroupDealsAsync`. Adjacent reads of store load (no date filter), scorer (all completed XAU in loaded set), 14-day Manager chunks, API resync (same 90d), mt5-worker (**30d**). No product edit. No live Manager attach in this slot. |
| Capital | **`SAFE_BY_ABSENCE`** — neither file builds `35=D` / `NewOrderSingle`. Bias is **ranking / SHADOW eligibility**, not dest send. |

Classification: **CONFIRMED_SCORE_BIAS**. Residual: store is **append-only** on deals, so a later wider window would accumulate; a **fresh DB + this host** is last-90-days-only forever. Not a go-live PASS.

---

## Verdict

**The live host hard-codes `UtcNow.AddDays(-90)` → `UtcNow.AddMinutes(1)` once, then `SyncBrokerAsync` forwards that pair unchanged. Scoring loads every persisted deal for the login (no second window) and scores every completed XAU trade in that set. On a cold store, “every deal” = last 90 host-UTC days. That is not account-lifetime skill and not “first 3 trades on the account.”**

Two opposite errors, both score-moving:

1. **Miss older skill.** Veteran / prior-challenge tape older than 90 days never lands. Martingale, SL use, PF, and the true first-3 are invisible. A quiet last quarter → `INSUFFICIENT_DATA` (0 XAU in window) even if the book is old and dirty. A clean last quarter after old size-ups → `SHADOW`-shaped scores.
2. **Include only a recent challenge burst.** A 30-day 2-step / instant challenge inside the 90-day box dominates N, PF, frequency, sequential lot flags. Burst pass/fail algorithms look like “skill.” `EarlyScoreTradeCount = 3` then fires on the **first three closes in the window**, not lifetime trade #1–#3.

`DealIngestionService` does **not** own the 90-day constant. It is a dumb `[from, to]` pump. The bias is **who calls it**.

---

## Measured window owners

| Caller | `from` | `to` | Who is scored |
|---|---|---|---|
| `LiveIngestHostedService` L37–38, L83 | `UtcNow.AddDays(-90)` | `UtcNow.AddMinutes(1)` | `ListLoginsWithDealsAsync` (logins that have **any** persisted deal) |
| `POST /api/ops/resync` (`apps/api/Program.cs` L118–119) | **same 90d** | **same +1m** | `ListLoginsAsync` (all catalog logins, including empty) |
| `apps/mt5-worker/Worker.cs` L27–28 | `UtcNow.AddDays(-30)` | `UtcNow.AddMinutes(1)` | hard-coded `{10001,10002,10003,99001}` |
| `DemoSeeder` | `2026-01-01` → `2026-12-31` | calendar year | four demo logins |
| C++ `resolveMt5TimeWindow` | `to - lookbackSeconds` (tests use **365d**); `to` = MT5 server time or host fallback | not used by C# host | n/a |

The C# live path does **not** use `mt5_time_window`. `from`/`to` are **host UTC**, not validated MT5 server time. A server clock hours ahead/behind shifts which deals fall inside the 90-day box.

Window is captured **once** at `ExecuteAsync` start (after the 2s delay). A multi-hour group walk still uses that frozen `[from, to]`. Fine for completeness of that snapshot; it does not walk forward.

---

## Evidence quotes

### 1. Host — the only 90-day constant on the live loop

```37:38:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);
```

```81:83:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    st.Phase = "deals";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    var deals = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
```

No config key. No `lookbackSeconds`. No per-broker override. No checkpoint / watermark (`SyncCheckpoint` unused here). Catalog is unwindowed; deals are not.

### 2. Ingest — forwards the pair; does not widen, shrink, or page beyond the connector

```54:80:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var catalog = await SyncCatalogAsync(brokerCode, ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;
        var groups = await connector.GetGroupsAsync(ct);
        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;

        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
        else
        {
            foreach (var account in accounts)
            {
                var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
```

Native live path is `IMt5BulkDealReader` → `DealRequestByGroup` per **returned group name** over that same `[from, to]`. A group missing from catalog discovery never contributes deals (separate slice). This slot: even a complete group list is still **90 host days**.

Connector chunks **14 days** inside the caller window (`NativeMt5BrokerConnector.Windows`). That is a Manager timeout/size mitigation, **not** a longer history:

```355:365:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Windows(DateTimeOffset from, DateTimeOffset to)
    {
        var cursor = from;
        while (cursor < to)
        {
            var end = cursor.AddDays(14);
            if (end > to)
                end = to;
            yield return (cursor, end);
            cursor = end;
        }
    }
```

~7 chunks × N groups. Still 90 days total. Unpaged `CIMTDealArray` per chunk (truncation risk is orthogonal; see W500_RESEARCH_20).

### 3. Score path — all persisted deals, no date filter

Host then:

```105:113:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    // ...
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
```

```119:127:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
```

```144:150:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task<IReadOnlyList<NormalizedDeal>> LoadDealsAsync(Guid brokerId, string brokerCode, long login, CancellationToken ct)
    {
        var rows = await _db.Mt5Deals
            .Where(d => d.BrokerId == brokerId && d.Login == login)
            .OrderBy(d => d.DealTime)
            .ThenBy(d => d.DealTicket)
```

`ListLoginsWithDealsAsync` = `Mt5Deals` distinct login for the broker. No `DealTime >= from`. Upsert is **insert-if-missing** (ticket key); it does **not** delete deals older than the window. Implications:

- **Cold Postgres + this host:** store = last 90 days. Scores = last 90 days.
- **Warm store that once ingested a wider range:** scores include leftover older tickets **plus** the new 90-day pull. Two environments, two “truths,” same binary.
- Logins whose last deal is **>90 days ago** are **never inserted** on a cold run → never appear in `ListLoginsWithDealsAsync` → **never scored**. They do not even get `INSUFFICIENT_DATA`; they are absent from the scored set.

`ReplaceReconstructedAsync` **wipes** that login’s reconstructed trades and rewrites from the loaded deal set. The reconstructed table therefore tracks the **current store**, not a lifetime tape.

### 4. Scorer — every completed XAU in the loaded set; “first 3” is N≥3, not lifetime first 3

```40:44:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public const int EarlyScoreTradeCount = 3;
    // ...
        var trades = completedXau.Where(t => t.Completed && t.IsXauUsd).OrderBy(t => t.ClosedAt).ToList();
```

```129:161:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var eligible = features.CompletedXauTrades >= EarlyScoreTradeCount;
        // risk += martingale 35 / averaging 20 / lot-escalation 15 / lotCv / slUse / DD>GP
        // quality += NetPnl>0 ? 15 : 0; PF gates; behavior*0.2; −risk*0.25
        if (features.CompletedXauTrades < EarlyScoreTradeCount)
            quality = Math.Min(quality, 40m);
```

`TraderStateMachine.FromBaseline`: 0 XAU → `INSUFFICIENT_DATA`; risk≥80 or (martingale ∧ DD>0 ∧ net<0) → `RISK_BLOCKED`; else N<3 → `INSUFFICIENT_DATA`; quality≥70 ∧ risk<40 → `SHADOW`.

Docs (`docs/trade-reconstruction.md` L41–47) say prop eval tracks the **first 3 completed trades on an account**. The implementation tracks the **first 3 completed XAU in whatever deals are in the table**. After a 90-day ingest, that is the first 3 **closes inside the window**.

`TradeFrequencyPerDay` uses span between first and last **windowed** close, floored at 1 day. A 48-hour challenge burst of 20 closes → ~10/day. A 2-year book of 20 closes would have been ~0.03/day. Frequency is not persisted as a named column but it is a feature; sequential flags (`Martingale`, `LotEscalation`) only compare **adjacent trades in this ordered window**.

---

## Bias matrix (score-moving)

| Scenario | What the 90-day box contains | Score effect |
|---|---|---|
| **Old skill, quiet 90d** | 0–2 XAU closes | `INSUFFICIENT_DATA` or quality capped at 40. Login may be **unscored** if 0 deals. Veteran disappears from leaderboard. |
| **Old martingale, clean 90d** | Clean N≥3, no size-up after loss | `Martingale=false`, risk low, `NetPnl>0` can add +15. False `SHADOW` / `WATCH`. |
| **Old clean book, recent challenge burst** | High-frequency challenge tape only | Burst PF/DD/lot-step dominate. Pass-algo martingale → `RISK_BLOCKED` (false block of a long-run good book) **or** lucky +$100 burst → +15 quality / `SHADOW` (false promote). |
| **Challenge-only account (typical 30d 2-step)** | Entire challenge + maybe prior fail leftover if <90d | Scores **are** the challenge. That matches prop-firm “this attempt” **only if** the attempt started inside 90d **and** no prior attempt leftovers remain. A retry on day 80 of a previous fail **mixes two attempts**. |
| **Position opened day −91, closed day −89** | OUT (and maybe INOUT) without full IN | Reconstruction can mark incomplete / wrong VWAP / orphan OUT. Features skip incomplete. N undercount; SL/hold wrong. |
| **IN in window, still open** | Open position not in `completedXau` | Current risk not in score. Burst in progress under-counted until close. |
| **Non-XAU skill** | Filtered by `IsXauUsd` after load | Window is deal-time, not symbol. Irrelevant to XAU score except it still **inserts** those deals and makes the login scorable with N=0 XAU → `INSUFFICIENT_DATA`. |
| **mt5-worker also running** | Extra **30-day** pull on four demo logins | Does not widen live 90d. Can race upserts. Live host remains the census window. |

Honesty vs architecture: three completed XAU trades are **explicitly not skill** (`docs/scoring.md`; P500_CODE_18). The 90-day window makes it easier to **hit N=3 on a burst** and never see the years that would falsify the burst.

---

## What this is not

- **Not a dest-edge or send path.** `LiveIngestHostedService` / `DealIngestionService` have **0** `35=D` / `NewOrderSingle`. Bias can change `SuggestedState` toward `SHADOW`; `CanPromoteToLive` is still `false`.
- **Not a measured live completeness number.** This slot did not attach to Achiever/Starwave or count tickets older than 90d vs inside 90d. Completeness of `DealRequestByGroup` inside each 14-day chunk is **unproven** (prior research).
- **Not “scores always equal last 90 days.”** Only true when the deal table has no older rows. Append-only upsert means a one-time 365-day backfill (C++ lookback style) would permanently enlarge the score tape until someone deletes.
- **Not server-time aligned.** Host UTC vs MT5 server TZ can drop or include a day at each edge.

---

## Cross-checks

| Item | Result |
|---|---|
| `DealIngestionService` default window | **None.** Signature requires `from, to`. |
| Config / env for lookback | **0** hits on the two assigned files. |
| `SyncCheckpoint` used by host or ingest | **No.** |
| Load-time filter `DealTime >=` | **No.** |
| Scorer recency / half-life / first-3-of-account | **No.** All completed XAU equally weighted, ordered by `ClosedAt`. |
| API resync | Same **90d** (`Program.cs` L118). |
| Worker | **30d** — stricter miss of older skill. |
| C++ helper | 365d lookback in tests; unused by C# live host. |
| Product edited | **No.** |

---

## Residual / recommended (not implemented)

If a later wave is allowed to touch product (this slot is not):

1. Separate **ingest lookback** (ops/timeout) from **score universe** (lifetime first-3 vs rolling 90d vs this-challenge-only). Document which one `EarlyQualityScore` means.
2. Persist `window_from` / `window_to` / `deal_min_time` / `deal_max_time` on `TraderScore` so a 90-day score cannot be read as career skill.
3. Use MT5 server time (`resolveMt5TimeWindow`) instead of host UTC.
4. For prop groups: window = **this challenge start → now**, not rolling 90d (avoids mixing failed attempts).
5. Do not treat N=3 inside a burst as `SHADOW` without dest costs (already a standing honesty item).

---

## Paths

| Role | Absolute path |
|---|---|
| Host (90d constant) | `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` |
| Ingest (forwards window) | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| Score rebuild | same ingest file, `ReconstructionScoringService` |
| Load all deals | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| Scorer | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| 14d Manager chunks | `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` |
| Manual resync 90d | `D:\Prop\apps\api\Program.cs` |
| Worker 30d | `D:\Prop\apps\mt5-worker\Worker.cs` |
| This report | `D:\Prop\reports\swarm\20260818\P500_S034_90day_window.md` |
