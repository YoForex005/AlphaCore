using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;

namespace TraderIntelligence.Infrastructure.Persistence;

public sealed class EfTradingStore : ITradingStore
{
    private readonly TraderDbContext _db;

    public EfTradingStore(TraderDbContext db) => _db = db;

    public async Task<Guid> ResolveBrokerIdAsync(string brokerCode, CancellationToken ct)
    {
        var broker = await _db.Brokers.SingleAsync(b => b.Code == brokerCode, ct);
        return broker.Id;
    }

    public async Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Mt5Groups.SingleOrDefaultAsync(
            g => g.BrokerId == brokerId && g.Name == group.Name, ct);
        if (existing is null)
        {
            _db.Mt5Groups.Add(new Mt5Group
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                Name = group.Name,
                Currency = group.Currency,
                CurrencyDigits = group.CurrencyDigits,
                Company = group.Company,
                MarginCall = group.MarginCall,
                MarginStopOut = group.MarginStopOut,
                ConnectionsAllowed = group.ConnectionsAllowed,
                EnabledForAnalysis = true,
                LastDiscoveredAt = now,
                LastSyncedAt = now
            });
        }
        else
        {
            existing.Currency = group.Currency;
            existing.LastSyncedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertAccountAsync(Guid brokerId, Mt5AccountDto account, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Mt5Accounts.SingleOrDefaultAsync(
            a => a.BrokerId == brokerId && a.Login == account.Login, ct);
        if (existing is null)
        {
            _db.Mt5Accounts.Add(new Mt5Account
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                Login = account.Login,
                GroupName = account.GroupName,
                Leverage = account.Leverage,
                Balance = account.Balance,
                Equity = account.Equity,
                Margin = account.Margin,
                MarginFree = account.MarginFree,
                Profit = account.Profit,
                LastSyncedAt = now
            });
        }
        else
        {
            existing.GroupName = account.GroupName;
            existing.Balance = account.Balance;
            existing.Equity = account.Equity;
            existing.LastSyncedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpsertDealAsync(Guid brokerId, Mt5DealDto deal, DateTimeOffset now, CancellationToken ct)
    {
        var exists = await _db.Mt5Deals.AnyAsync(
            d => d.BrokerId == brokerId && d.DealTicket == deal.DealTicket, ct);
        if (exists)
            return false;

        _db.Mt5Deals.Add(new Mt5Deal
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            DealTicket = deal.DealTicket,
            Login = deal.Login,
            OrderTicket = deal.OrderTicket,
            PositionId = deal.PositionId,
            Symbol = deal.Symbol,
            Action = deal.Action,
            Entry = deal.Entry,
            VolumeNative = deal.VolumeNative,
            Price = deal.Price,
            Profit = deal.Profit,
            Commission = deal.Commission,
            Swap = deal.Swap,
            DealTime = deal.Time,
            Comment = deal.Comment,
            IngestedAt = now
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ReplacePositionsAsync(Guid brokerId, long login, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct)
    {
        var existing = _db.Mt5Positions.Where(p => p.BrokerId == brokerId && p.Login == login);
        _db.Mt5Positions.RemoveRange(existing);
        foreach (var p in positions)
        {
            _db.Mt5Positions.Add(new Mt5Position
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                PositionTicket = p.PositionTicket,
                Login = p.Login,
                Symbol = p.Symbol,
                Direction = p.Direction,
                VolumeNative = p.VolumeNative,
                PriceOpen = p.PriceOpen,
                PriceCurrent = p.PriceCurrent,
                PriceSl = p.PriceSl,
                PriceTp = p.PriceTp,
                Profit = p.Profit,
                TimeCreate = p.TimeCreate,
                TimeUpdate = DateTimeOffset.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NormalizedDeal>> LoadDealsAsync(Guid brokerId, string brokerCode, long login, CancellationToken ct)
    {
        var rows = await _db.Mt5Deals
            .Where(d => d.BrokerId == brokerId && d.Login == login)
            .OrderBy(d => d.DealTime)
            .ThenBy(d => d.DealTicket)
            .ToListAsync(ct);

        return rows.Select(d => new NormalizedDeal
        {
            BrokerId = brokerCode,
            Login = d.Login,
            DealTicket = d.DealTicket,
            OrderTicket = d.OrderTicket,
            PositionId = d.PositionId,
            SourceSymbol = d.Symbol,
            Action = d.Action,
            Entry = d.Entry,
            VolumeNative = d.VolumeNative,
            Price = d.Price,
            Profit = d.Profit,
            Commission = d.Commission,
            Swap = d.Swap,
            Time = d.DealTime,
            Comment = d.Comment
        }).ToList();
    }

    public async Task ReplaceReconstructedAsync(Guid brokerId, long login, IReadOnlyList<ReconstructedTradeResult> trades, CancellationToken ct)
    {
        var existing = _db.ReconstructedTrades.Where(t => t.BrokerId == brokerId && t.Login == login);
        _db.ReconstructedTrades.RemoveRange(existing);
        foreach (var t in trades)
        {
            _db.ReconstructedTrades.Add(new ReconstructedTrade
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
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
                GrossRealizedPnl = t.GrossRealizedPnl,
                Commission = t.Commission,
                Swap = t.Swap,
                Fees = t.Fees,
                NetRealizedPnl = t.NetRealizedPnl,
                DealCount = t.DealCount,
                OrderCount = t.OrderCount,
                InitialSl = t.InitialSl,
                InitialTp = t.InitialTp,
                FinalSl = t.FinalSl,
                FinalTp = t.FinalTp,
                WasScaledIn = t.WasScaledIn,
                WasPartialClose = t.WasPartialClose,
                WasAveragedDown = t.WasAveragedDown,
                Completed = t.Completed
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertScoreAsync(TraderScore score, CancellationToken ct)
    {
        var existing = await _db.TraderScores.SingleOrDefaultAsync(
            s => s.BrokerId == score.BrokerId && s.Login == score.Login, ct);
        if (existing is null)
        {
            _db.TraderScores.Add(score);
        }
        else
        {
            existing.RiskScore = score.RiskScore;
            existing.BehaviorScore = score.BehaviorScore;
            existing.EarlyQualityScore = score.EarlyQualityScore;
            existing.CompletedXauTrades = score.CompletedXauTrades;
            existing.Martingale = score.Martingale;
            existing.AveragingDown = score.AveragingDown;
            existing.LotEscalation = score.LotEscalation;
            existing.CurrentState = score.CurrentState;
            existing.LastScoredAt = score.LastScoredAt;
        }

        _db.TraderScoreHistory.Add(new TraderScoreHistory
        {
            Id = Guid.NewGuid(),
            BrokerId = score.BrokerId,
            Login = score.Login,
            RiskScore = score.RiskScore,
            BehaviorScore = score.BehaviorScore,
            EarlyQualityScore = score.EarlyQualityScore,
            State = score.CurrentState,
            RecordedAt = score.LastScoredAt
        });

        await _db.SaveChangesAsync(ct);
    }

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
        var quote = new TraderIntelligence.Domain.Risk.DestinationQuote(
            quoteRow.CanonicalSymbol,
            quoteRow.VenueInstrumentId,
            quoteRow.Bid,
            quoteRow.Ask,
            quoteRow.ReceivedAt,
            quoteRow.VenueTimestamp);

        foreach (var trade in completedXau.Where(t => t.Completed).OrderBy(t => t.ClosedAt))
        {
            var key = $"shadow:{brokerId}:{login}:{trade.PositionId}";
            if (await _db.CopyIntents.AnyAsync(c => c.IdempotencyKey == key, ct))
                continue;

            var intent = new CopyIntent
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                SourceLogin = login,
                CanonicalSymbol = trade.CanonicalSymbol,
                Action = CopyIntentAction.OpenExposure,
                RequestedQuantity = trade.MaxVolumeLots,
                ExpectedPrice = trade.EntryVwap,
                SourceEventTime = trade.OpenedAt,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = trade.OpenedAt.AddSeconds(15),
                Status = "SHADOW_ONLY",
                IdempotencyKey = key
            };
            _db.CopyIntents.Add(intent);

            var fill = engine.SimulateEntry(
                intent.Id.ToString(),
                trade.Direction,
                trade.MaxVolumeLots,
                trade.EntryVwap,
                quote,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(80));

            _db.ShadowOrders.Add(new ShadowOrder
            {
                Id = Guid.NewGuid(),
                CopyIntentId = intent.Id,
                BrokerId = brokerId,
                SourceLogin = login,
                Direction = trade.Direction,
                Quantity = fill.Quantity,
                Price = fill.Price,
                Spread = fill.Spread,
                SourceVsShadowSlippage = fill.SourceVsShadowSlippage,
                FilledAt = fill.FilledAt
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
