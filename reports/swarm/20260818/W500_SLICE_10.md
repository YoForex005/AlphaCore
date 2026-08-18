# W500_SLICE_10

- **slot:** 10
- **file:** `D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs`
- **angle:** dummy or seeded data still reachable on the live path
- **read:** full file (205 lines) via `read_file`; grep on this file for dummy/seed/fake/mock/sample returned **0 name matches**; DTO + DI cross-read to bind constructor literals
- **verdict:** **FAIL**

## Evidence quotes

`EfDashboardQueries` is the **live** `IDashboardQueries` implementation. DI registers it unconditionally:

```41:41:D:/Prop/src/Infrastructure/DependencyInjection.cs
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
```

The class has no `dummy` / `seed` / `fake` tokens, but the live methods **paint constructor literals** and will also surface whatever `DemoSeeder` wrote into `TraderDbContext` (API + both workers call `DemoSeeder.SeedAsync` after `EnsureCreated`). There is no live-vs-demo discriminator on this path.

### Overview — dest PnL, XAU books, real-copy flag are literals

```26:43:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
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
```

Mapped to `OverviewDto`: `DestinationRealPnl=0`, `XauGross=0`, `XauNet=0`, `RealCopyEnabled=false`. `Mt5Healthy` is `brokers > 0` (enabled catalog count), not a heartbeat. `LiveRuntimeStatus.RealCopyEnabled` is never read.

### Brokers — always Connected, LastEventAt fabricated as now

```53:53:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
            result.Add(new BrokerStatusDto(b.Code, b.DisplayName, b.Server, MaskLogin(b.ManagerLogin), true, groups, accounts, DateTimeOffset.UtcNow));
```

`Connected` is the literal `true`. `LastEventAt` is `DateTimeOffset.UtcNow` on every request, not last deal / checkpoint.

### Traders — per-row shadow PnL is a dummy 0

```93:106:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
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

`MlProbability=null` (honest while ML is absent). `ShadowPnl=0` is **not** honest: overview already sums `ShadowOrders.SourceVsShadowSlippage`, but the live trader card never uses it. `GetTraderAsync` reuses this list.

### FIX sessions — ExecutionEnabled hardcoded false

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

Last ctor arg is `ExecutionEnabled=false`. Flag is not read from `CTraderFixOptions.RealCopyExecutionEnabled` or `LiveRuntimeStatus`.

### Risk — all capital KPIs are dummy zeros

```196:196:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), false, rejects);
```

`DailyPnl`, `Drawdown`, `XauLong`, `XauShort`, `XauNet` are `0`. `RealCopyEnabled` is `false`. Only kill-switch mode + last 20 reject reason strings are queried.

### Seeded rows ride the same live queries

This type does not insert seed rows, but it `ToListAsync`s `TraderScores`, `Brokers`, `Mt5Accounts`, `ReconstructedTrades`, `ShadowOrders`, `FixSessionStates`, `DestinationQuotes`, `KillSwitches` with **no** environment / quality / `isDemo` gate. Hosts still call `DemoSeeder.SeedAsync` (`apps/api/Program.cs`, `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`). Those demo logins (10001 / 10002 / 10003 / 99001) and catalog brokers are therefore **reachable on the live dashboard path** through this class.

Literal inventory on the live ctor path:

| Method | Dummy / fabricated fields |
|---|---|
| `GetOverviewAsync` | `DestinationRealPnl=0`, `XauGross=0`, `XauNet=0`, `RealCopyEnabled=false`; `Mt5Healthy=brokers>0` |
| `GetBrokersAsync` | `Connected=true`, `LastEventAt=UtcNow` |
| `GetTradersAsync` / `GetTraderAsync` / detail header | `ShadowPnl=0`, `MlProbability=null` |
| `GetFixSessionsAsync` | `ExecutionEnabled=false` |
| `GetRiskAsync` | `DailyPnl=Drawdown=XauLong=XauShort=XauNet=0`, `RealCopyEnabled=false` |

Empty PASS is **not** allowed: the file was fully read and the dummy literals are on the registered live implementation.

## No-loss implication

A desk using this live dashboard **cannot see capital risk**. Destination PnL, XAU gross/net, daily PnL, and drawdown are always zero, so a real dest book (if later armed) would look flat. `RealCopyEnabled` and `ExecutionEnabled` stay `false` even if another process flips `LiveRuntimeStatus` / `CTraderFixOptions.RealCopyExecutionEnabled`. Brokers always paint `Connected=true` with a fresh `LastEventAt`, so a dead Manager session is hidden. Seeded Fake/demo traders can appear as the live book. No-loss halt / size-down decisions that depend on these tiles would be made from **painted zeros and fabricated health**, not venue truth.
