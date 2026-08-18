# W500_SLICE_60

- **slot:** 60
- **file:** `D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs`
- **angle:** dummy or seeded data still reachable on the live path
- **read:** full file (205 lines) via `read_file`; grep on this file for dummy/seed/fake/mock/sample/placeholder/hardcoded/in-memory/DemoSeeder returned **0 name matches**; DTO + DI + `LiveRuntimeStatus` cross-read to bind constructor literals
- **verdict:** **FAIL**

## Binding law (this angle)

Dummy, seeded, or fabricated dashboard values must not be reachable on the live `IDashboardQueries` path. A live operator must not see:

- canned dest / XAU / risk PnL painted as `0` when those books were not queried
- `Connected=true` / `LastEventAt=UtcNow` / `Mt5Healthy=brokers>0` as if they were heartbeats
- `RealCopyEnabled` / `ExecutionEnabled` constructor `false` that ignore `LiveRuntimeStatus`
- per-trader `ShadowPnl=0` while overview sums a different column

Empty-PASS is allowed only after a full read finds no such literals and no seed-login / FakeMt5 surface. That condition is **not** met.

## Evidence quotes

`EfDashboardQueries` is the **live** `IDashboardQueries` implementation. DI registers it unconditionally (no demo/live fork):

```49:49:D:/Prop/src/Infrastructure/DependencyInjection.cs
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
```

The class itself has no `dummy` / `seed` / `fake` tokens and does not construct `FakeMt5BrokerConnector` or logins `10001`/`10002`/`10003`/`99001`. It still **paints constructor literals** onto live DTOs and will surface whatever rows `TraderDbContext` holds (including any prior `DemoSeeder` catalog). There is no live-vs-demo discriminator.

### Overview — dest PnL, XAU books, real-copy flag are literals

```14:43:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<OverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var accounts = await _db.Mt5Accounts.CountAsync(ct);
        var brokers = await _db.Brokers.CountAsync(b => b.Enabled, ct);
        var scores = await _db.TraderScores.ToListAsync(ct);
        var xauTraders = scores.Count(s => s.CompletedXauTrades > 0);
        var three = scores.Count(s => s.CompletedXauTrades >= 3);
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        var quote = await _db.FixSessionStates.SingleOrDefaultAsync(s => s.Qualifier == FixSessionQualifier.Quote, ct);
        var trade = await _db.FixSessionStates.SingleOrDefaultAsync(s => s.Qualifier == FixSessionQualifier.Trade, ct);

        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
            shadowPnl,
            0,
            0,
            0,
            brokers > 0,
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution,
            false);
    }
```

Mapped to `OverviewDto` (`DashboardModels.cs` L5–22):

| Argument position | Field | Source |
|---|---|---|
| 11 | `DestinationRealPnl` | literal `0` — dest book never queried |
| 12 | `XauGross` | literal `0` |
| 13 | `XauNet` | literal `0` |
| 14 | `Mt5Healthy` | `brokers > 0` (enabled catalog count, not Manager heartbeat) |
| 17 | `RealCopyEnabled` | literal `false` |

`LiveRuntimeStatus.RealCopyEnabled` is set from `REAL_COPY_EXECUTION_ENABLED` in DI and is **never read** here:

```38:42:D:/Prop/src/Infrastructure/DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

### Brokers — always Connected, LastEventAt fabricated as now

```49:54:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        foreach (var b in brokers)
        {
            var groups = await _db.Mt5Groups.CountAsync(g => g.BrokerId == b.Id, ct);
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == b.Id, ct);
            result.Add(new BrokerStatusDto(b.Code, b.DisplayName, b.Server, MaskLogin(b.ManagerLogin), true, groups, accounts, DateTimeOffset.UtcNow));
        }
```

`Connected` is the literal `true`. `LastEventAt` is `DateTimeOffset.UtcNow` on every request, not last deal / checkpoint / `BrokerLiveStatus.UpdatedAt`. `LiveRuntimeStatus.Broker(code).Connected` is unused.

### Traders — per-row ShadowPnl is the literal 0; ML is null

```93:107:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
            mapped.Add(new TraderRowDto(
                b.Code,
                s.Login,
                account?.GroupName,
                s.CompletedXauTrades,
                pnl,
                s.EarlyQualityScore,
                null,
                s.RiskScore,
                s.Martingale,
                s.AveragingDown,
                s.LotEscalation,
                s.CurrentState,
                0,
                s.LastScoredAt));
```

`MlProbability=null` is an honest “ML not wired” hole. `ShadowPnl=0` is **not** honest: overview already sums `_db.ShadowOrders.SourceVsShadowSlippage`, but the leaderboard / detail header never groups that table per login. Every live trader tile therefore shows shadow PnL of zero.

`GetTraderAsync` / `GetTraderDetailAsync` reuse this mapping, so the dummy `0` is also on the detail path.

### FIX sessions — ExecutionEnabled is a literal false

```166:183:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        return sessions.Select(s => new FixSessionDto(
            s.Qualifier.ToString().ToUpperInvariant(),
            s.Host,
            s.Port,
            s.Status != FixSessionStatus.Disconnected && s.Status != FixSessionStatus.Error,
            s.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution or FixSessionStatus.Reconciling,
            s.Status.ToString(),
            s.LastInboundAt,
            s.LastOutboundAt,
            s.InboundSeq,
            s.OutboundSeq,
            s.ReconnectCount,
            s.LastError,
            quote?.VenueInstrumentId,
            quote?.Bid,
            quote?.Ask,
            quote is null ? null : (DateTimeOffset.UtcNow - quote.ReceivedAt).TotalSeconds,
            false)).ToList();
```

Last argument is `FixSessionDto.ExecutionEnabled` (`DashboardModels.cs` L92). Always `false`, independent of `LiveRuntimeStatus.RealCopyEnabled` and of `ReadyForExecution` on the same row.

### Risk dashboard — five capital tiles are literals

```187:197:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<RiskDashboardDto> GetRiskAsync(CancellationToken ct)
    {
        var ks = await _db.KillSwitches.OrderByDescending(k => k.UpdatedAt).FirstOrDefaultAsync(ct);
        var rejects = await _db.RiskDecisions
            .Where(r => r.Outcome != RiskDecisionOutcome.Approve)
            .OrderByDescending(r => r.DecidedAt)
            .Take(20)
            .Select(r => r.Reason)
            .ToListAsync(ct);

        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), false, rejects);
    }
```

Mapped to `RiskDashboardDto`: `DailyPnl=0`, `Drawdown=0`, `XauLong=0`, `XauShort=0`, `XauNet=0`, `RealCopyEnabled=false`. Kill-switch mode and reject reasons are real EF reads. The five exposure/PnL tiles are dummy zeros on the live risk page.

## What this file does **not** contain

- `FakeMt5BrokerConnector` / `DemoBrokerFactory` / `DemoSeeder` / logins `10001`–`10003` / `99001`
- an in-process canned book fallback when EF is empty (empty store → empty lists / zero counts)
- `Random()`, fixture GUIDs, or `NotImplemented` stubs that return sample rows

Those seed logins remain reachable **through** this class if they were written into `TraderDbContext` by another host path. This type will project them as live traders with no filter.

## No-loss implication

This class does not send FIX `NewOrderSingle` or Manager orders. The capital risk is **honesty / understated book**, not a send from these methods.

- Overview dest / XAU tiles always `0` hide real destination PnL and XAU gross/net if dest fills exist.
- Risk page `DailyPnl`/`Drawdown`/`XauLong`/`XauShort`/`XauNet` always `0` can look like a flat, unexposed book while live dest XAU is open. Kill-switch mode is shown; exposure is not.
- Broker `Connected=true` and `LastEventAt=UtcNow` plus `Mt5Healthy = enabled broker count > 0` can paint a dead Manager as healthy.
- Per-trader `ShadowPnl=0` understates shadow slippage already stored in `ShadowOrders` (overview uses a different, global sum).
- Hardcoded `RealCopyEnabled=false` / `ExecutionEnabled=false` can disagree with `LiveRuntimeStatus` if `REAL_COPY_EXECUTION_ENABLED=true`. Fail-closed on the DTO is safer than a false `true`, but it is still dummy: the live flag is ignored, so an armed process can look disarmed on the dashboard.

Dummy literals are on the live DI path. Not an empty-PASS.
