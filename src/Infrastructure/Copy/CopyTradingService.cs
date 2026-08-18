using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Copy;
using TraderIntelligence.Application.Runtime;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Execution;
using TraderIntelligence.Domain.Risk;
using TraderIntelligence.Domain.Shadow;
using TraderIntelligence.Infrastructure.Persistence;

namespace TraderIntelligence.Infrastructure.Copy;

public sealed class CopyTradingService
{
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = 0.05m;

    private static readonly InstrumentQuantitySpec GoldSpec = new(0.01m, 5m, 0.01m, 2);

    private readonly TraderDbContext _db;
    private readonly LiveRuntimeStatus _runtime;
    private readonly RiskEngine _risk = new();
    private readonly QuantityNormalizer _qty = new();
    private readonly ShadowCopyEngine _shadow = new();

    public CopyTradingService(TraderDbContext db, LiveRuntimeStatus runtime)
    {
        _db = db;
        _runtime = runtime;
    }

    public async Task<CopyGateStatus> GetStatusAsync(CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var intents = await _db.CopyIntents.CountAsync(ct);
        var shadows = await _db.ShadowOrders.CountAsync(ct);
        var sends = await _db.ExecutionIntents.CountAsync(e => e.SentAt != null, ct);
        var live = scores.Count(s => s.CurrentState == TraderState.LIVE);
        var shadow = scores.Count(s => s.CurrentState == TraderState.SHADOW);
        var watch = scores.Count(s => s.CurrentState == TraderState.WATCH);
        var blockers = BuildBlockers(live);
        return new CopyGateStatus(
            FeatureCopyEnabled: true,
            RealCopyArmed: _runtime.RealCopyEnabled,
            QuoteLoggedOn: _runtime.Quote.LoggedOn,
            TradeLoggedOn: _runtime.Trade.LoggedOn,
            VenueReconciled: VenueReconciled,
            NewOrderSingleImplemented: NewOrderSingleImplemented,
            LiveTraders: live,
            ShadowTraders: shadow,
            WatchTraders: watch,
            Intents: intents,
            ShadowFills: shadows,
            LiveSends: sends,
            Blockers: blockers,
            Summary: blockers.Count == 0
                ? "All gates open — live send would be legal. Unexpected."
                : "Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle.");
    }

    public async Task<IReadOnlyList<CopyIntentRow>> ListIntentsAsync(int take, CancellationToken ct)
    {
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var rows = await _db.CopyIntents.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        var risks = await _db.RiskDecisions.AsNoTracking().ToListAsync(ct);
        var riskByIntent = risks
            .GroupBy(r => r.CopyIntentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.DecidedAt).First());

        return rows.Select(c =>
        {
            brokers.TryGetValue(c.BrokerId, out var b);
            riskByIntent.TryGetValue(c.Id, out var risk);
            return new CopyIntentRow(
                b?.Code ?? c.BrokerId.ToString(),
                c.SourceLogin,
                c.SourcePositionId,
                c.Action.ToString(),
                c.Direction.ToString(),
                c.RequestedQuantity,
                c.ExpectedPrice,
                c.Status,
                risk?.Reason,
                risk?.AllowFixSend ?? false,
                c.CreatedAt);
        }).ToList();
    }

    public async Task<int> GenerateShadowIntentsAsync(CancellationToken ct)
    {
        var copyable = new[] { TraderState.SHADOW, TraderState.LIVE_CANDIDATE, TraderState.LIVE };
        var scores = await _db.TraderScores.Where(s => copyable.Contains(s.CurrentState)).ToListAsync(ct);
        if (scores.Count == 0)
            return 0;

        var now = DateTimeOffset.UtcNow;
        var created = 0;
        var quoteRow = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);

        foreach (var score in scores)
        {
            var trades = await _db.ReconstructedTrades
                .Where(t => t.BrokerId == score.BrokerId && t.Login == score.Login && t.Completed && t.CanonicalSymbol == "XAUUSD")
                .OrderBy(t => t.ClosedAt ?? t.OpenedAt)
                .ToListAsync(ct);

            foreach (var trade in trades)
            {
                var key = $"copy:{score.BrokerId}:{score.Login}:{trade.PositionId}";
                if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == key, ct))
                    continue;

                decimal qty;
                try
                {
                    qty = _qty.Normalize(trade.MaxVolumeLots, AllocationFactor, GoldSpec);
                }
                catch
                {
                    qty = 0m;
                }

                if (qty <= 0)
                    continue;

                var intent = new CopyIntent
                {
                    Id = Guid.NewGuid(),
                    BrokerId = score.BrokerId,
                    SourceLogin = score.Login,
                    SourcePositionId = trade.PositionId,
                    CanonicalSymbol = "XAUUSD",
                    Action = CopyIntentAction.OpenExposure,
                    Direction = trade.Direction,
                    RequestedQuantity = qty,
                    ExpectedPrice = trade.EntryVwap,
                    SourceEventTime = trade.OpenedAt,
                    CreatedAt = now,
                    ExpiresAt = now.AddSeconds(15),
                    Status = "PENDING_RISK",
                    IdempotencyKey = key
                };
                _db.CopyIntents.Add(intent);

                var quote = quoteRow is null
                    ? null
                    : new DestinationQuote(
                        quoteRow.CanonicalSymbol,
                        quoteRow.VenueInstrumentId,
                        quoteRow.Bid,
                        quoteRow.Ask,
                        quoteRow.ReceivedAt,
                        quoteRow.VenueTimestamp);

                var decision = _risk.Evaluate(new RiskEvaluationRequest
                {
                    CopyIntentId = intent.Id.ToString(),
                    BrokerId = score.BrokerId.ToString(),
                    SourceLogin = score.Login,
                    Action = intent.Action,
                    RequestedQuantity = qty,
                    ExpectedPrice = trade.EntryVwap,
                    SourceEventTime = trade.OpenedAt,
                    DecisionTime = now,
                    Quote = quote,
                    VenueHealthy = _runtime.Trade.LoggedOn && _runtime.Quote.LoggedOn,
                    RealExecutionEnabled = _runtime.RealCopyEnabled,
                    Reconciled = VenueReconciled,
                    KillSwitch = KillSwitchMode.None,
                    TraderRealizedLoss = Math.Min(0m, trade.NetRealizedPnl),
                    DailyExecutionPnl = 0,
                    PortfolioDrawdown = 0,
                    CurrentGrossXau = 0,
                    CurrentNetXau = 0,
                    OpenPositions = 0,
                    MarginUsage = 0,
                    MartingaleFlag = score.Martingale,
                    AbnormalSizing = score.LotEscalation
                });

                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
                    DecidedAt = now
                };
                _db.RiskDecisions.Add(rec);
                intent.RiskDecisionId = rec.Id;

                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
                    if (quote is not null && decision.Outcome == RiskDecisionOutcome.Approve)
                    {
                        var fill = _shadow.SimulateEntry(
                            intent.Id.ToString(),
                            trade.Direction,
                            qty,
                            trade.EntryVwap,
                            quote,
                            now,
                            TimeSpan.FromMilliseconds(80));
                        _db.ShadowOrders.Add(new ShadowOrder
                        {
                            Id = Guid.NewGuid(),
                            CopyIntentId = intent.Id,
                            BrokerId = score.BrokerId,
                            SourceLogin = score.Login,
                            Direction = trade.Direction,
                            Quantity = fill.Quantity,
                            Price = fill.Price,
                            Spread = fill.Spread,
                            SourceVsShadowSlippage = fill.SourceVsShadowSlippage,
                            FilledAt = fill.FilledAt
                        });
                    }
                }

                created++;
            }
        }

        if (created > 0)
            await _db.SaveChangesAsync(ct);
        return created;
    }

    private List<string> BuildBlockers(int liveTraders)
    {
        var blockers = new List<string>();
        if (!NewOrderSingleImplemented)
            blockers.Add("No NewOrderSingle sender — SAFE_BY_ABSENCE");
        if (!VenueReconciled)
            blockers.Add("Venue not reconciled");
        if (liveTraders == 0)
            blockers.Add("0 traders in LIVE (promotion is manual; trade #3 cannot auto-LIVE)");
        if (!_runtime.Quote.LoggedOn)
            blockers.Add("FIX QUOTE not logged on");
        if (!_runtime.Trade.LoggedOn)
            blockers.Add("FIX TRADE not logged on");
        if (!_runtime.RealCopyEnabled)
            blockers.Add("REAL_COPY_EXECUTION_ENABLED is false");
        return blockers;
    }
}
