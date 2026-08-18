# P500_CODE_9 — Program.cs: negative `netSourcePnl` SHADOW rows prove the scorer is not a profit gate

| Field | Value |
|---|---|
| Slot | **9** |
| Agent | P500_CODE_9 (senior trading-systems, this API host only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| File | `D:\Prop\apps\api\Program.cs` |
| Angle | Negative `netSourcePnl` SHADOW rows prove the scorer is not a profit gate |
| Product source modified | **No.** This report is the only write. |
| Method | Full `read_file` of `Program.cs` (156 lines). Grep of this file for `netSourcePnl` / `SHADOW` / `scored` / `REAL_COPY` / `NewOrderSingle`. Read the exact callees the host invokes: `ReconstructionScoringService.RebuildTraderAsync`, `BaselineScorer` / `TraderStateMachine.FromBaseline`, `EfTradingStore.PersistDemoShadowAsync` + `ListLoginsAsync`, `EfDashboardQueries.GetTradersAsync` / `GetOverviewAsync`, demo tape `FakeMt5BrokerConnector.BuildAchieverDeals`. **No** HTTP, **no** FIX socket, **no** `NewOrderSingle` construction. |
| Measured live (caller) | 8463 accounts; Achiever scoring; Starwave deals-done scored **0**; SHADOW all demo; `destinationRealPnl` **0**; FIX **LoggedOn**; `REAL_COPY` **false** |

Classification: **CONFIRMED** on the angle. Capital class for this leaf: `SAFE_BY_ABSENCE` (no send). Not a go-live PASS. Not a claim that live Achiever books were re-probed in this slot.

---

## Verdict

**CONFIRMED — the scorer is not a profit gate, and `Program.cs` does not add one.**

`Program.cs` was read in full. Empty PASS is not used. The host:

1. Exposes `GET /api/traders` with optional `broker` / `state` only — **no** `minNetSourcePnl`, **no** `pnl > 0` filter, **no** “SHADOW requires profit” conjunct.
2. Exposes `POST /api/ops/resync` that scores **every** `ListLoginsAsync` login on ACHIEVER + STARWAVEFX. The loop increments `scored` per login. It never inspects `NetRealizedPnl` / `netSourcePnl`.
3. Hands each login to `ReconstructionScoringService.RebuildTraderAsync`, which persists `SuggestedState` blindly and then `PersistDemoShadowAsync` if that state is `SHADOW`. Persist writes one `SHADOW_ONLY` intent + `ShadowOrder` per **completed** XAU trade, including losers.
4. In `BaselineScorer`, `NetPnl > 0` is only `quality += 15`. `FromBaseline` emits `SHADOW` on `quality >= 70 && risk < 40`. Negative net is **not** a veto unless it is conjoined with `Martingale && MaxDrawdown > 0`.
5. Algebra: N≥3, SL used, no flags, `NetPnl <= 0` ⇒ `quality = 50 + 20 = 70`, `risk = 0` ⇒ **SHADOW**. A losing first-three book is a legal SHADOW row. Demo login **10001** is SHADOW while trade **502** is a source loser (`profit = -88`) and still gets a shadow row (D48: 3 rows for 10001).

Caller-measured live 8463 / Achiever scoring / Starwave `deals-done` scored **0** does not install a profit gate. SHADOW remaining **all demo** and `destinationRealPnl` **0** mean those SHADOW rows are still Fake-book + dest-off, not Pepperstone fills.

---

## Evidence quotes

### 1. This file — traders list has no PnL gate

`D:\Prop\apps\api\Program.cs` maps traders as a passthrough. The only query knobs are `broker` and `state`:

```95:96:D:\Prop\apps\api\Program.cs
app.MapGet("/api/traders", (IDashboardQueries q, string? broker, string? state, CancellationToken ct) =>
    q.GetTradersAsync(broker, state, ct));
```

The query that fills `netSourcePnl` sums **all completed** reconstructed trades per login and attaches whatever `CurrentState` exists. Negative sum + `SHADOW` is a legal row:

```90:118:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var pnls = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.Completed)
            .GroupBy(t => new { t.BrokerId, t.Login })
            .Select(g => new { g.Key.BrokerId, g.Key.Login, Pnl = g.Sum(x => x.NetRealizedPnl) })
            .ToListAsync(ct);
        // ...
            mapped.Add(new TraderRowDto(
                b.Code,
                account.Login,
                account.GroupName,
                s?.CompletedXauTrades ?? 0,
                pnl,
                // ...
                s?.CurrentState ?? TraderState.INSUFFICIENT_DATA,
```

No `if (pnl <= 0) skip SHADOW`. No `minNetSourcePnl` (A92 listed that filter as **not** in §50 and “P&L remains a column + sort key only”).

### 2. This file — resync scores every login, not every profitable login

```111:143:D:\Prop\apps\api\Program.cs
app.MapPost("/api/ops/resync", async (
    DealIngestionService ingestion,
    ReconstructionScoringService scoring,
    ITradingStore store,
    LiveRuntimeStatus runtime,
    CancellationToken ct) =>
{
    // ...
        var logins = await store.ListLoginsAsync(brokerId, ct);
        var scored = 0;
        foreach (var login in logins)
        {
            await scoring.RebuildTraderAsync(code, login, ct);
            scored++;
        }
        // ...
        result[code] = new { catalog.Groups, catalog.Accounts, deals, scored, logins = logins.Count };
```

`ListLoginsAsync` is **all** `Mt5Accounts` for the broker, not “logins with `NetPnl > 0`”:

```339:341:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

This host path is **wider** than live ingest (`ListLoginsWithDealsAsync`). Starwave sitting at caller-measured `deals-done` / scored **0** is the hosted service, not a profit filter in `Program.cs`. A later `POST /api/ops/resync` would walk the whole 8463-account catalog and still never ask “did they make money?”.

### 3. Rebuild persists SHADOW without reading PnL

```126:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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

`PersistDemoShadowAsync` gates on **state token only**. It does not read `NetRealizedPnl`:

```267:293:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
        // dest quote required; then:
        foreach (var trade in completedXau.Where(t => t.Completed).OrderBy(t => t.ClosedAt))
        {
            // new CopyIntent { Status = "SHADOW_ONLY", ... }
            // new ShadowOrder { ... }
        }
```

A SHADOW book with a losing trade still writes a **negative-source** shadow row.

### 4. Scorer: NetPnl is a bonus, not a SHADOW predicate

```152:206:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
        if (features.ProfitFactor >= 1.8m) quality += 5;
        quality += behavior * 0.2m;
        quality -= risk * 0.25m;
        // ...
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        // ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

| Net sign | What the stub does |
|---|---|
| `NetPnl > 0` | `quality += 15`. Also unlocks PF addends when winners dominate. |
| `NetPnl <= 0` | No +15. PF `< 1` so no +10/+5. **Does not forbid SHADOW.** |
| `NetPnl < 0` **and** martingale **and** DD>0 | `RISK_BLOCKED` (this is the only negative-net **state** gate). |
| N≥3, SL used, no flags, `NetPnl <= 0` | `behavior=100`, `risk=0`, `quality=70` → **SHADOW**. |

Unit fixtures do **not** lock the losing-but-clean SHADOW hole. `Three_disciplined_winners_go_to_shadow_not_live` uses `+80/+70/+90`. `Martingale_after_losses_is_risk_blocked` is the losing **2×** book, not a flat loser.

### 5. Demo SHADOW already includes a losing source trade

`BuildAchieverDeals` for SHADOW login **10001**:

```114:116:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10001, 501, 1, t0, 2320.10m, 2335.40m, 0.10m, 153m, -1.2m, -0.4m));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10001, 502, 2, t0.AddHours(3), 2338.00m, 2329.20m, 0.10m, -88m, -1.1m, -0.3m, shortSide: true));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10001, 503, 3, t0.AddHours(6), 2325.50m, 2341.80m, 0.10m, 163m, -1.2m, -0.2m));
```

Position **502** is a source loser (`profit = -88`). D48 measured **3** `shadow_orders` for 10001 after rebuild. Trader-level `netSourcePnl` for 10001 is still **net positive** (~+223 after commission/swap). That is **not** a trader-level negative SHADOW proof. It **is** proof that SHADOW persist does not skip negative-PnL **rows**.

Honesty on trader-level negative SHADOW:

- Demo SHADOW logins **10001** / **99001** are net-positive at the header.
- Demo **10002** is large-negative (`-200/-500/-1400`) and lands `RISK_BLOCKED` via the martingale+loss clause — not a counter-example to the clean-loser SHADOW path.
- This slot did **not** re-GET `/api/traders?state=SHADOW` against the 8463 live catalog. Caller pin: SHADOW is **all demo**. Do not invent a live count of `netSourcePnl < 0 && state == SHADOW`.

The **code** plus the **losing-trade shadow row** plus the **quality=70 algebra** are the proof. A live Achiever loser with SL and flat size would paint as SHADOW + negative `netSourcePnl` the moment it is scored.

### 6. This file — dest P&L and copy flags stay off

Overview `destinationRealPnl` is a constructor **0** in the query this host calls (`GetOverviewAsync` args after `shadowPnl` are `0, 0, 0`):

```43:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
            _runtime.Brokers.Values.Count(b => b.Connected) > 0,
            // ...
            _runtime.RealCopyEnabled);
```

`Program.cs` publishes the flag; it does not arm it:

```62:82:D:\Prop\apps\api\Program.cs
app.MapGet("/api/reconciliation/status", () => Results.Ok(new
{
    lastReconciliation = DateTimeOffset.UtcNow,
    unknownPositions = 0,
    mismatches = 0,
    orphanFills = 0,
    note = "recon runs only after FIX TRADE logon; NewOrderSingle still off"
}));
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
```

Grep of this file: `NewOrderSingle` = 1 (English “still off”); `35=D` = **0**; `REAL_COPY` appears only as a **read** of `runtime.RealCopyEnabled`. DI / FIX host force that bool **false**. Caller-measured `REAL_COPY false` matches.

| Probe in `Program.cs` | Measured |
|---|---|
| `netSourcePnl` / `NetPnl` / profit filter | **0** |
| `SHADOW` token | **0** (state is downstream) |
| `RebuildTraderAsync` / `scored++` | **yes** — all logins |
| `35=D` | **0** |
| `NewOrderSingle` | warning string only |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded **false** |
| `REAL_COPY_EXECUTION_ENABLED` | mirrored from runtime (forced false elsewhere) |

---

## Profit implication

**No live profit from this file.** Scoring a loser as SHADOW does not book destination P&L. `destinationRealPnl` stays **0**. Starwave `deals-done` scored **0** means this host has not even rebuilt Starwave live books on the ingest path. Demo SHADOW (10001 / 99001) is Fake tape.

**Selection implication (if copy is later armed):** the pipeline will treat a disciplined losing first-three book as a SHADOW candidate. `quality >= 70` can be reached with `NetPnl <= 0`. Profit capture is **not** what SHADOW means here. Enabling `REAL_COPY_EXECUTION_ENABLED` without adding a real profit / sample / risk hop would copy **losers** as readily as winners. Do not market SHADOW as “proven profitable.”

---

## Lower-loss implication

**This leaf cannot open a losing live Pepperstone position.** `Program.cs` never builds `35=D`. Recon note and settings keep `NewOrderSingle` off. Combined with caller-measured `REAL_COPY false`, SHADOW-all-demo, and dest real P&L **0**, capital is **not** at risk from listing or rescoring traders.

**Residual (honesty, not a FAIL of “no send”):** absence of a profit gate is a **future** loss amplifier. Negative-source SHADOW rows already exist on the demo book (10001 / 502). The same persist path will fire for any live Achiever login that scores SHADOW. FIX **LoggedOn** elsewhere does not become a send because of this file — but it also does **not** make SHADOW a money filter.

Safety here is **`SAFE_BY_ABSENCE`**, not a unit-tested “refuse SHADOW when `netSourcePnl < 0`.”

---

## Direct answers

| Question | Answer |
|---|---|
| Is the scorer a profit gate? | **No.** `NetPnl > 0` is `+15` quality only. SHADOW = `quality >= 70 && risk < 40`. |
| Do negative-PnL SHADOW **rows** exist? | **Yes** at trade grain (demo 10001 pos 502, `profit = -88`, still `ShadowOrder`). **Not measured** as a trader-header `netSourcePnl < 0 && SHADOW` on the 8463 live catalog (SHADOW still all demo; those two headers are net-positive). |
| Does `Program.cs` add a profit gate? | **No.** Resync scores every login. Traders list has no `minNetSourcePnl`. |
| Does this file send live `35=D`? | **No.** |
| Would listing/resync put Pepperstone capital at risk today? | **No.** `REAL_COPY` false; dest P&L 0; no NOS builder. |
| Slot 9 verdict | **CONFIRMED** (angle). Capital: `SAFE_BY_ABSENCE`. |

---

## One-line close

**Slot 9 CONFIRMED: `Program.cs` scores every login and lists SHADOW with whatever `netSourcePnl` the book has; `NetPnl > 0` is a quality bonus, not a gate — losing source trades already become SHADOW rows, while Pepperstone capital stays uninvolved because this file never sends `35=D`.**
