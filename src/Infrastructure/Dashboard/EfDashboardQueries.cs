using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Infrastructure.Persistence;

namespace TraderIntelligence.Infrastructure.Dashboard;

public sealed class EfDashboardQueries : IDashboardQueries
{
    private readonly TraderDbContext _db;

    public EfDashboardQueries(TraderDbContext db) => _db = db;

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

    public async Task<IReadOnlyList<BrokerStatusDto>> GetBrokersAsync(CancellationToken ct)
    {
        var brokers = await _db.Brokers.OrderBy(b => b.Code).ToListAsync(ct);
        var result = new List<BrokerStatusDto>();
        foreach (var b in brokers)
        {
            var groups = await _db.Mt5Groups.CountAsync(g => g.BrokerId == b.Id, ct);
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == b.Id, ct);
            result.Add(new BrokerStatusDto(b.Code, b.DisplayName, b.Server, MaskLogin(b.ManagerLogin), true, groups, accounts, DateTimeOffset.UtcNow));
        }

        return result;
    }

    public async Task<IReadOnlyList<GroupRowDto>> GetGroupsAsync(CancellationToken ct)
    {
        var groups = await _db.Mt5Groups.ToListAsync(ct);
        var brokers = await _db.Brokers.ToDictionaryAsync(b => b.Id, ct);
        var rows = new List<GroupRowDto>();
        foreach (var g in groups)
        {
            var code = brokers.TryGetValue(g.BrokerId, out var b) ? b.Code : g.BrokerId.ToString();
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == g.BrokerId && a.GroupName == g.Name, ct);
            rows.Add(new GroupRowDto(code, g.Name, accounts, g.EnabledForAnalysis, g.PlanMapping, g.LastDiscoveredAt, g.LastSyncedAt));
        }

        return rows;
    }

    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        var pnls = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.Completed)
            .GroupBy(t => new { t.BrokerId, t.Login })
            .Select(g => new { g.Key.BrokerId, g.Key.Login, Pnl = g.Sum(x => x.NetRealizedPnl) })
            .ToListAsync(ct);
        var pnlMap = pnls.ToDictionary(x => (x.BrokerId, x.Login), x => x.Pnl);

        var mapped = new List<TraderRowDto>();
        foreach (var s in scores)
        {
            if (!brokers.TryGetValue(s.BrokerId, out var b))
                continue;
            var account = accounts.FirstOrDefault(a => a.BrokerId == s.BrokerId && a.Login == s.Login);
            pnlMap.TryGetValue((s.BrokerId, s.Login), out var pnl);
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
        }

        IEnumerable<TraderRowDto> filtered = mapped;
        if (!string.IsNullOrWhiteSpace(broker))
            filtered = filtered.Where(t => t.Broker.Equals(broker, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(state) && Enum.TryParse<TraderState>(state, true, out var st))
            filtered = filtered.Where(t => t.State == st);

        return filtered.OrderByDescending(t => t.EarlyScore).ToList();
    }

    public async Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct)
    {
        var rows = await GetTradersAsync(broker, null, ct);
        return rows.FirstOrDefault(t => t.Login == login);
    }

    public async Task<TraderDetailDto?> GetTraderDetailAsync(string broker, long login, CancellationToken ct)
    {
        var header = await GetTraderAsync(broker, login, ct);
        if (header is null)
            return null;

        var b = await _db.Brokers.AsNoTracking().SingleOrDefaultAsync(x => x.Code == broker, ct);
        if (b is null)
            return new TraderDetailDto(header, Array.Empty<TradeHighlightDto>());

        var trades = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.BrokerId == b.Id && t.Login == login)
            .OrderBy(t => t.ClosedAt ?? t.OpenedAt)
            .ToListAsync(ct);

        var firstThree = 0;
        var highlights = trades.Select(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && firstThree < 3;
            if (first)
                firstThree++;
            return new TradeHighlightDto(
                t.PositionId,
                t.SourceSymbol,
                t.CanonicalSymbol,
                t.Direction,
                t.OpenedAt,
                t.ClosedAt,
                t.NetRealizedPnl,
                t.MaxVolumeLots,
                t.Completed,
                first);
        }).ToList();

        return new TraderDetailDto(header, highlights);
    }

    public async Task<IReadOnlyList<FixSessionDto>> GetFixSessionsAsync(CancellationToken ct)
    {
        var sessions = await _db.FixSessionStates.OrderBy(s => s.Qualifier).ToListAsync(ct);
        var quote = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);
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
    }

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

    private static long MaskLogin(long login)
    {
        if (login < 100)
            return login;
        return login / 100 * 100;
    }
}
