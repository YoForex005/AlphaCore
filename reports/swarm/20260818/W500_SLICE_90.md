# W500_SLICE_90

- **slot:** 90
- **file:** `D:/Prop/src/Domain/Risk/RiskEngine.cs`
- **angle:** dummy or seeded data still reachable on the live path
- **read:** full file (190 lines) via `read_file` (lines 1–190); directory `D:/Prop/src/Domain/Risk` contains only this file; grep on `Domain/Risk` for `dummy|seed|fake|mock|sample|placeholder|hardcoded|test.?data|Demo|Stub|TODO|FIXME|Random|Guid\.New` returned **0 matches**; file-local grep for `10001|10_000|DemoSeeder|FakeMt5|BrokerCatalogSeed|synthetic|fixture` returned **0 matches**
- **call sites:** product `*.cs` `Evaluate(` / `new RiskEngine` exist only in this type; tests only in `D:/Prop/tests/Unit/RiskEngineTests.cs`; `apps/api/Controllers/SettingsController.cs` uses a **different** settings DTO also named `RiskEngine`
- **verdict:** PASS

## File (assigned)

`D:/Prop/src/Domain/Risk/RiskEngine.cs` is a pure in-process evaluator. The same compilation unit defines `RiskLimits`, `DestinationQuote`, `RiskEvaluationRequest`, `RiskDecision`, and `RiskEngine`. There is no I/O, no EF, no seeder import, no broker connector, and no FIX socket.

This is an independent re-read of the same type already covered by W500_SLICE_40 (same file, same angle, different slot). Line counts and quotes below are from the 2026-08-18 full read of the current 190-line file.

## Evidence quotes

### 1. No dummy / seed / fake symbols in the assigned file

Case-insensitive grep of `D:/Prop/src/Domain/Risk` for dummy/seed/fake/mock/sample/placeholder/Demo/Stub/`Random`/`Guid.New`/`TODO`/`FIXME` returned **zero** hits. File-local grep for `10001`, `10_000`, `DemoSeeder`, `FakeMt5`, `BrokerCatalogSeed`, `synthetic`, `fixture` also **zero**.

The file never names `DemoSeeder`, `BrokerCatalogSeed`, `FakeMt5BrokerConnector`, or `DemoBrokerFactory`. Seeded Achiever/Starwave demo logins (`10001` / `10002` / `10003` / `99001`) live only on the non-live seeder path and are **not referenced** here.

### 2. Engine does not invent quotes, PnL, logins, or books

`Evaluate` consumes a fully `required` request. Every market / book / identity field is caller-supplied. The engine never constructs a fallback `DestinationQuote`, never writes a default login, and never fills PnL/exposure from a fixture:

```32:56:D:/Prop/src/Domain/Risk/RiskEngine.cs
public sealed record RiskEvaluationRequest
{
    public required string CopyIntentId { get; init; }
    public required string BrokerId { get; init; }
    public required long SourceLogin { get; init; }
    public required CopyIntentAction Action { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal ExpectedPrice { get; init; }
    public required DateTimeOffset SourceEventTime { get; init; }
    public required DateTimeOffset DecisionTime { get; init; }
    public required DestinationQuote? Quote { get; init; }
    public required bool VenueHealthy { get; init; }
    public required bool RealExecutionEnabled { get; init; }
    public required bool Reconciled { get; init; }
    public required KillSwitchMode KillSwitch { get; init; }
    public required decimal TraderRealizedLoss { get; init; }
    public required decimal DailyExecutionPnl { get; init; }
    public required decimal PortfolioDrawdown { get; init; }
    public required decimal CurrentGrossXau { get; init; }
    public required decimal CurrentNetXau { get; init; }
    public required int OpenPositions { get; init; }
    public required decimal MarginUsage { get; init; }
    public required bool MartingaleFlag { get; init; }
    public required bool AbnormalSizing { get; init; }
}
```

Missing quote on an increasing action is **reject**, not a canned mid/bid/ask:

```95:96:D:/Prop/src/Domain/Risk/RiskEngine.cs
        if (request.Quote is null && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "QUOTE_MISSING");
```

`Reject` zeros size and forbids FIX. It copies `CopyIntentId` from the request; it does not mint a seed id:

```180:188:D:/Prop/src/Domain/Risk/RiskEngine.cs
    private static RiskDecision Reject(RiskEvaluationRequest request, RiskDecisionOutcome outcome, string reason) =>
        new()
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = outcome,
            ApprovedQuantity = 0,
            Reason = reason,
            AllowFixSend = false
        };
```

Approve paths pass through `request.RequestedQuantity` and `request.CopyIntentId` only:

```164:171:D:/Prop/src/Domain/Risk/RiskEngine.cs
        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
```

### 3. Live FIX gate does not substitute dummy data when shadow / flags fail

`RealExecutionEnabled == false` is an empty comment, then evaluation continues on the **caller’s** numbers. It does not swap in a demo quote or seed PnL:

```90:93:D:/Prop/src/Domain/Risk/RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

`AllowFixSend` is a conjunction of live flags only — still no fabricated book:

```147:150:D:/Prop/src/Domain/Risk/RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Quote checks that do run use `request.Quote` / `request.ExpectedPrice` / `request.DecisionTime`. They compare; they do not invent a book:

```98:110:D:/Prop/src/Domain/Risk/RiskEngine.cs
        if (request.Quote is not null)
        {
            var age = request.DecisionTime - request.Quote.ReceivedAt;
            if (age > _limits.MaxQuoteAge && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "QUOTE_STALE");

            var spread = request.Quote.Ask - request.Quote.Bid;
            if (spread > _limits.MaxAllowedSpread && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "SPREAD_TOO_WIDE");

            var mid = (request.Quote.Bid + request.Quote.Ask) / 2m;
            if (Math.Abs(mid - request.ExpectedPrice) > _limits.MaxPriceMove && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "PRICE_MOVED_TOO_FAR");
        }
```

### 4. `RiskLimits` compile defaults are policy numbers, not dummy market data

```5:22:D:/Prop/src/Domain/Risk/RiskEngine.cs
public sealed class RiskLimits
{
    public decimal MaxLossPerTrader { get; init; } = 500m;
    public decimal MaxDailyExecutionLoss { get; init; } = 2_000m;
    public decimal MaxPortfolioDrawdown { get; init; } = 3_000m;
    public decimal MaxXauGrossExposure { get; init; } = 20m;
    public decimal MaxXauNetExposure { get; init; } = 10m;
    public decimal MaxPositionQuantity { get; init; } = 5m;
    public int MaxOpenPositions { get; init; } = 20;
    public decimal MaxAllowedSpread { get; init; } = 2.0m;
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
    public decimal MaxPriceMove { get; init; } = 3.0m;
    public decimal MaxSlippage { get; init; } = 1.5m;
    public decimal MaxMarginUsage { get; init; } = 0.70m;
    public bool BlockMartingale { get; init; } = true;
    public bool BlockAbnormalSizing { get; init; } = true;
}
```

```71:74:D:/Prop/src/Domain/Risk/RiskEngine.cs
    public RiskEngine(RiskLimits? limits = null)
    {
        _limits = limits ?? new RiskLimits();
    }
```

These are **threshold** defaults (lab policy). They are not canned Achiever/Starwave logins, not `10_000` demo balances, not synthetic XAU fills. They do not populate `Quote` / `TraderRealizedLoss` / `CurrentGrossXau`. If a caller omits `RiskLimits`, the engine is stricter or looser than a future operator document, but it still evaluates the caller’s request — it does not seed a fake book.

`MaxSlippage` is declared and **never read** in `Evaluate` (dead field). That is a missing guard, not dummy data.

### 5. Product live path does not construct this type or feed it seeder rows

Grep of product `*.cs` for `new RiskEngine` / `Evaluate(` / `RiskLimits`:

| Location | What it is |
|---|---|
| `src/Domain/Risk/RiskEngine.cs` | this type |
| `src/Domain/Entities/RiskDecisionRecord.cs` | persisted `AllowFixSend` column; nobody writes one from `Evaluate` |
| `tests/Unit/RiskEngineTests.cs` | `private readonly RiskEngine _e = new();` plus a **test-only** `Base()` fixture (`CopyIntentId = "c1"`, `SourceLogin = 1`, frozen `2026-08-18T12:00:00Z` quote at 2399.9/2400.1) |
| `apps/api/Controllers/SettingsController.cs` | anonymous/DTO property named `RiskEngine` (`MaxDailyDrawdownPct` etc.) — **not** `TraderIntelligence.Domain.Risk.RiskEngine` |

No Application worker, FIX host, or ingest host calls `Evaluate(`. Test fixture data therefore **cannot** reach a live `AllowFixSend` through this file: it lives only under `tests/`.

This file does not contain:

- `FakeMt5BrokerConnector` / `DemoBrokerFactory` / `DemoSeeder` / `BrokerCatalogSeed`
- hardcoded logins, groups, XAU round-trips, or balance books
- `Random()`, `Guid` fixtures, or `NotImplemented` stubs that return sample quotes
- a fallback from missing quote / missing PnL to any in-process demo book
- any substitution that would make shadow or seed rows look like a live destination quote

## No-loss implication

`RiskEngine.Evaluate` can return `AllowFixSend = true` **only** when the caller already set `RealExecutionEnabled`, `KillSwitch == None`, `Reconciled`, and `VenueHealthy`, and every increasing-action quote/spread/age/exposure check passed. That is a live-capital **gate**, not a data source.

This type cannot publish canned demo traders, fabricated deals, or seeded quotes onto that gate. Worst case inside the assigned file:

1. **Missing quote on increase** → `QUOTE_MISSING`, `ApprovedQuantity = 0`, `AllowFixSend = false`.
2. **Shadow / flag off** → evaluation continues on caller numbers; `allowSend` stays false.
3. **Unbound `RiskLimits`** → lab thresholds apply. That is a config-binding gap (A23 / D13), not dummy books treated as live market data.
4. **Reduce/close with null quote** → no quote checks; still no dummy mid; FIX still requires the live-flag conjunction.

Dummy / seeded books cannot reach live FIX send through `RiskEngine.cs`. Product hosts do not even call this type today (SAFE_BY_ABSENCE on the send path). Residual defects (unused `MaxSlippage`, exclusive kill-switch, unbound limits) are **not** this angle.

Empty-PASS justification: the assigned file was fully read (190/190 lines). The angle (dummy or seeded data on the live path) is **absent by construction** in this type — no skipped review.

## Verdict rationale

PASS: `RiskEngine` is a pure evaluator over required request fields. It never synthesizes dummy quotes, seeded PnL, or demo logins; missing increase quotes fail closed; `AllowFixSend` does not flip true on seed data created in this file. Compile-time `RiskLimits` are policy defaults, not live-path dummy books.
