using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Copy;
using TraderIntelligence.Application.Runtime;
using TraderIntelligence.Fix.CTrader.Sessions;
using TraderIntelligence.Domain.Copy;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Execution;
using TraderIntelligence.Domain.Risk;
using TraderIntelligence.Domain.Shadow;
using TraderIntelligence.Infrastructure.Persistence;

namespace TraderIntelligence.Infrastructure.Copy;

public sealed class CopyTradingService
{
    public const bool VenueReconciled = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
    public const decimal MaxAutoLots = 0.05m;

    private readonly TraderDbContext _db;
    private readonly LiveRuntimeStatus _runtime;
    private readonly IConfiguration _config;
    private readonly ILogger<CopyTradingService> _log;
    private readonly IBrokerRegistry _brokers;
    private readonly RiskEngine _risk = new();
    private readonly XauUsdOneToOneCopyPolicy _policy = new();
    private readonly CopyRosterEngine _roster = new();
    private readonly ShadowCopyEngine _shadow = new();

    public CopyTradingService(
        TraderDbContext db,
        LiveRuntimeStatus runtime,
        IConfiguration config,
        ILogger<CopyTradingService> log,
        IBrokerRegistry brokers)
    {
        _db = db;
        _runtime = runtime;
        _config = config;
        _log = log;
        _brokers = brokers;
    }

    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;

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
            VenueReconciled: DemoDest,
            NewOrderSingleImplemented: NewOrderSingleImplemented,
            LiveTraders: live,
            ShadowTraders: shadow,
            WatchTraders: watch,
            Intents: intents,
            ShadowFills: shadows,
            LiveSends: sends,
            Blockers: blockers,
            Summary: DemoDest
                ? "Demo dest auto-copy ON. Dest closes when the MT5 Manager book drops the master ticket, then 35=AN dest book is checked. Live 1369850 is never used."
                : "Copy pipeline ON. Shadow intents only. Live Pepperstone will not receive NewOrderSingle.");
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

    public async Task<int> TickRosterAsync(CancellationToken ct)
    {
        var scores = await _db.TraderScores.ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var changed = 0;
        foreach (var score in scores)
        {
            var account = await _db.Mt5Accounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.BrokerId == score.BrokerId && a.Login == score.Login, ct);
            var xau = await _db.ReconstructedTrades
                .Where(t => t.BrokerId == score.BrokerId && t.Login == score.Login && t.CanonicalSymbol == "XAUUSD")
                .ToListAsync(ct);
            var snapshot = new CopyTraderSnapshot
            {
                State = score.CurrentState,
                CompletedXauTrades = score.CompletedXauTrades,
                XauNetPnl = xau.Where(t => t.Completed).Sum(t => t.NetRealizedPnl),
                Martingale = score.Martingale,
                AveragingDown = score.AveragingDown,
                LotEscalation = score.LotEscalation,
                GroupName = account?.GroupName
            };
            var rosterKey = $"roster:{score.BrokerId}:{score.Login}";
            var row = await _db.CopyIntents.FirstOrDefaultAsync(c => c.IdempotencyKey == rosterKey, ct);
            var onRoster = row is not null && row.Status == "ADMITTED";
            var completed = xau.Where(t => t.Completed).Select(ToResult).ToList();
            var decision = _roster.Decide(snapshot, completed, onRoster);

            if (decision.Action == RosterAction.Admit)
            {
                if (row is null)
                {
                    _db.CopyIntents.Add(new CopyIntent
                    {
                        Id = Guid.NewGuid(),
                        BrokerId = score.BrokerId,
                        SourceLogin = score.Login,
                        CanonicalSymbol = "XAUUSD",
                        Action = CopyIntentAction.OpenExposure,
                        SourceEventTime = now,
                        CreatedAt = now,
                        ExpiresAt = now.AddYears(20),
                        Status = "ADMITTED",
                        IdempotencyKey = rosterKey
                    });
                }
                else
                {
                    row.Status = "ADMITTED";
                    row.CreatedAt = now;
                }
                changed++;
            }
            else if (decision.Action == RosterAction.RemoveAndFlatten)
            {
                if (row is null)
                {
                    _db.CopyIntents.Add(new CopyIntent
                    {
                        Id = Guid.NewGuid(),
                        BrokerId = score.BrokerId,
                        SourceLogin = score.Login,
                        CanonicalSymbol = "XAUUSD",
                        Action = CopyIntentAction.CloseExposure,
                        SourceEventTime = now,
                        CreatedAt = now,
                        ExpiresAt = now.AddYears(20),
                        Status = "REMOVED:" + decision.Reason,
                        IdempotencyKey = rosterKey
                    });
                }
                else
                {
                    row.Status = "REMOVED:" + decision.Reason;
                    row.Action = CopyIntentAction.CloseExposure;
                }

                if (decision.FlattenDestination)
                    changed += await FlattenOpenCopiesAsync(score.BrokerId, score.Login, now, ct);
                changed++;
            }
        }

        if (changed > 0)
            await _db.SaveChangesAsync(ct);
        return changed;
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
            var account = await _db.Mt5Accounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.BrokerId == score.BrokerId && a.Login == score.Login, ct);
            var xau = await _db.ReconstructedTrades
                .Where(t => t.BrokerId == score.BrokerId && t.Login == score.Login && t.CanonicalSymbol == "XAUUSD")
                .ToListAsync(ct);
            var snapshot = new CopyTraderSnapshot
            {
                State = score.CurrentState,
                CompletedXauTrades = score.CompletedXauTrades,
                XauNetPnl = xau.Where(t => t.Completed).Sum(t => t.NetRealizedPnl),
                Martingale = score.Martingale,
                AveragingDown = score.AveragingDown,
                LotEscalation = score.LotEscalation,
                GroupName = account?.GroupName
            };
            var rosterKey = $"roster:{score.BrokerId}:{score.Login}";
            var roster = await _db.CopyIntents.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdempotencyKey == rosterKey, ct);
            if (roster is null || roster.Status != "ADMITTED")
                continue;
            if (!_policy.IsTraderEligible(snapshot, out _))
                continue;

            foreach (var trade in xau.Where(t => !t.Completed))
            {
                var key = $"copy:{score.BrokerId}:{score.Login}:{trade.PositionId}:open";
                if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == key, ct))
                    continue;

                var instruction = _policy.Evaluate(snapshot, new CopySignal
                {
                    SourceSymbol = trade.SourceSymbol,
                    CanonicalSymbol = trade.CanonicalSymbol,
                    Action = CopyIntentAction.OpenExposure,
                    Direction = trade.Direction,
                    SourceLots = trade.MaxVolumeLots,
                    EntryPrice = trade.EntryVwap,
                    SourceEventTime = trade.OpenedAt,
                    SourceStillOpen = true,
                    StopLoss = trade.FinalSl ?? trade.InitialSl,
                    TakeProfit = trade.FinalTp ?? trade.InitialTp
                });
                if (!instruction.Accept)
                    continue;

                var qty = instruction.Lots;
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
                    IdempotencyKey = key,
                    StopLoss = instruction.StopLoss,
                    TakeProfit = instruction.TakeProfit,
                    OrdType = instruction.OrdType.ToString()
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

            foreach (var trade in xau.Where(t => t.Completed && t.ClosedAt.HasValue))
            {
                var openKey = $"copy:{score.BrokerId}:{score.Login}:{trade.PositionId}:open";
                var closeKey = $"copy:{score.BrokerId}:{score.Login}:{trade.PositionId}:close";
                if (!await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == openKey, ct))
                    continue;
                if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == closeKey, ct))
                    continue;

                var close = _policy.Evaluate(snapshot, new CopySignal
                {
                    SourceSymbol = trade.SourceSymbol,
                    CanonicalSymbol = trade.CanonicalSymbol,
                    Action = CopyIntentAction.CloseExposure,
                    Direction = trade.Direction,
                    SourceLots = trade.MaxVolumeLots,
                    EntryPrice = trade.ExitVwap ?? trade.EntryVwap,
                    SourceEventTime = trade.ClosedAt!.Value,
                    SourceStillOpen = false
                });
                if (!close.Accept)
                    continue;

                _db.CopyIntents.Add(new CopyIntent
                {
                    Id = Guid.NewGuid(),
                    BrokerId = score.BrokerId,
                    SourceLogin = score.Login,
                    SourcePositionId = trade.PositionId,
                    CanonicalSymbol = "XAUUSD",
                    Action = CopyIntentAction.CloseExposure,
                    Direction = trade.Direction,
                    RequestedQuantity = close.Lots,
                    ExpectedPrice = trade.ExitVwap ?? trade.EntryVwap,
                    SourceEventTime = trade.ClosedAt.Value,
                    CreatedAt = now,
                    ExpiresAt = now.AddSeconds(15),
                    Status = "SHADOW_ONLY",
                    IdempotencyKey = closeKey,
                    OrdType = "Market"
                });
                created++;
            }
        }

        if (created > 0)
            await _db.SaveChangesAsync(ct);
        return created;
    }

    private async Task<int> FlattenOpenCopiesAsync(Guid brokerId, long login, DateTimeOffset now, CancellationToken ct)
    {
        var opens = await _db.CopyIntents
            .Where(c => c.BrokerId == brokerId && c.SourceLogin == login
                        && c.Action == CopyIntentAction.OpenExposure
                        && c.IdempotencyKey.StartsWith("copy:"))
            .ToListAsync(ct);
        var n = 0;
        foreach (var open in opens)
        {
            var closeKey = $"copy:{brokerId}:{login}:{open.SourcePositionId}:close";
            if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == closeKey, ct))
                continue;
            _db.CopyIntents.Add(new CopyIntent
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                SourceLogin = login,
                SourcePositionId = open.SourcePositionId,
                CanonicalSymbol = "XAUUSD",
                Action = CopyIntentAction.CloseExposure,
                Direction = open.Direction,
                RequestedQuantity = open.RequestedQuantity,
                ExpectedPrice = open.ExpectedPrice,
                SourceEventTime = now,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(15),
                Status = "FLATTEN_LOSS_CUT",
                IdempotencyKey = closeKey,
                OrdType = "Market"
            });
            n++;
        }
        return n;
    }

    private static ReconstructedTradeResult ToResult(ReconstructedTrade t) =>
        new()
        {
            Id = t.Id.ToString(),
            BrokerId = t.BrokerId.ToString(),
            Login = t.Login,
            PositionId = t.PositionId,
            CanonicalSymbol = t.CanonicalSymbol,
            SourceSymbol = t.SourceSymbol,
            Direction = t.Direction,
            OpenedAt = t.OpenedAt,
            ClosedAt = t.ClosedAt,
            EntryVwap = t.EntryVwap,
            ExitVwap = t.ExitVwap,
            InitialVolumeLots = t.InitialVolumeLots,
            MaxVolumeLots = t.MaxVolumeLots,
            ClosedVolumeLots = t.ClosedVolumeLots,
            RemainingVolumeLots = 0,
            GrossRealizedPnl = t.GrossRealizedPnl,
            Commission = t.Commission,
            Swap = t.Swap,
            Fees = t.Fees,
            NetRealizedPnl = t.NetRealizedPnl,
            DealCount = t.DealCount,
            OrderCount = t.OrderCount,
            WasScaledIn = t.WasScaledIn,
            WasPartialClose = t.WasPartialClose,
            WasAveragedDown = t.WasAveragedDown,
            Completed = t.Completed
        };

    public async Task<int> ExecuteDemoCopyAsync(CancellationToken ct)
    {
        if (!DemoDest)
        {
            _log.LogInformation("Demo dest auto-copy skipped (host is not demo FIX).");
            return 0;
        }

        var host = _config["CTRADER_FIX_HOST"] ?? "";
        var sender = _config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "";
        var target = _config["CTRADER_FIX_TRADE_TARGET_COMP_ID"] ?? "cServer";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "";
        var password = _config["CTRADER_FIX_PASSWORD"] ?? "";
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        var ledger = DemoCopyLedger.Load();
        var sent = await ReconcileDestClosesAsync(host, sender, target, account, password, ledger, ct);
        DemoCopyLedger.Save(ledger);
        return sent;
    }

    public async Task<int> ReconcileDestClosesAsync(CancellationToken ct)
    {
        if (!DemoDest)
            return 0;
        var host = _config["CTRADER_FIX_HOST"] ?? "";
        var sender = _config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "";
        var target = _config["CTRADER_FIX_TRADE_TARGET_COMP_ID"] ?? "cServer";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "";
        var password = _config["CTRADER_FIX_PASSWORD"] ?? "";
        if (string.IsNullOrWhiteSpace(password))
            return 0;
        var ledger = DemoCopyLedger.Load();
        var sent = await ReconcileDestClosesAsync(host, sender, target, account, password, ledger, ct);
        DemoCopyLedger.Save(ledger);
        return sent;
    }

    private async Task<int> ReconcileDestClosesAsync(
        string host, string sender, string target, string account, string password,
        List<DemoCopyFill> ledger, CancellationToken ct)
    {
        var liveByBroker = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var conn in _brokers.All())
        {
            if (conn is not IMt5BulkPositionReader bulk)
                continue;
            try
            {
                var book = await bulk.GetGroupPositionsAsync("*", ct);
                if (!CopyLifecycle.TrustManagerBook(book.Count))
                {
                    _log.LogWarning("Manager book empty for {Broker}; skip dest closes this tick", conn.BrokerCode);
                    continue;
                }
                liveByBroker[conn.BrokerCode] = book.Select(p => p.PositionTicket.ToString()).ToHashSet();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Manager book failed for {Broker}; skip closes this tick", conn.BrokerCode);
            }
        }

        DestBookResult? destBook = null;
        try
        {
            destBook = await CTraderFixDestBook.RequestAsync(host, sender, target, account, password, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Dest 35=AN failed");
        }

        var ledgerOpen = ledger.Count(f => !f.DestClosed && !string.IsNullOrWhiteSpace(f.DestPositionId));
        var destOpen = destBook is not null
                       && CopyLifecycle.TrustDestVenueSnapshot(destBook.Complete, destBook.Positions.Count, ledgerOpen)
            ? destBook.Positions.Select(p => p.PosId).ToHashSet(StringComparer.Ordinal)
            : null;

        var sent = 0;
        foreach (var fill in ledger.Where(f => !f.DestClosed && !string.IsNullOrWhiteSpace(f.DestPositionId)).ToList())
        {
            if (destOpen is not null && !destOpen.Contains(fill.DestPositionId!))
            {
                fill.DestClosed = true;
                sent++;
                _log.LogInformation("Dest {Dest} already flat on cTrader for {Login}/{Pos}",
                    fill.DestPositionId, fill.SourceLogin, fill.SourcePositionId);
                continue;
            }

            var broker = fill.Broker ?? "ACHIEVER";
            if (!liveByBroker.TryGetValue(broker, out var live))
                continue;
            var masterLive = live.Contains(fill.SourcePositionId);
            if (!CopyLifecycle.ShouldCloseDestBecauseMasterGone(masterLive, true, fill.DestClosed))
                continue;

            var close = await CTraderFixCopyOpen.SendAsync(
                host, sender, target, account, password,
                fill.SourceLogin, fill.SourcePositionId, fill.IsLong, fill.Lots, ct, fill.DestPositionId);
            if (close.Filled || close.OrderSent || AlreadyFlat(close.Error))
            {
                fill.DestClosed = true;
                sent++;
                _log.LogInformation("Auto-closed dest {Dest} master gone {Login}/{Pos} filled={Filled} err={Err}",
                    fill.DestPositionId, fill.SourceLogin, fill.SourcePositionId, close.Filled, close.Error);
            }
            else
                _log.LogWarning("Auto-close failed {Login}/{Pos}: {Err}", fill.SourceLogin, fill.SourcePositionId, close.Error);
        }

        return sent;
    }

    private static bool AlreadyFlat(string? error) =>
        !string.IsNullOrWhiteSpace(error)
        && (error.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || error.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
            || error.Contains("UNKNOWN", StringComparison.OrdinalIgnoreCase)
            || error.Contains("closed", StringComparison.OrdinalIgnoreCase));

    private List<string> BuildBlockers(int liveTraders)
    {
        var blockers = new List<string>();
        if (!DemoDest)
        {
            blockers.Add("No NewOrderSingle sender — SAFE_BY_ABSENCE");
            blockers.Add("Venue not reconciled");
            if (liveTraders == 0)
                blockers.Add("0 traders in LIVE (promotion is manual; trade #3 cannot auto-LIVE)");
        }
        if (!_runtime.Quote.LoggedOn)
            blockers.Add("FIX QUOTE not logged on");
        if (!_runtime.Trade.LoggedOn)
            blockers.Add("FIX TRADE not logged on");
        if (!_runtime.RealCopyEnabled)
            blockers.Add("REAL_COPY_EXECUTION_ENABLED is false");
        return blockers;
    }
}
