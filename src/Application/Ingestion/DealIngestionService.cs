using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;

namespace TraderIntelligence.Application.Ingestion;

public interface ITradingStore
{
    Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct);
    Task UpsertAccountAsync(Guid brokerId, Mt5AccountDto account, DateTimeOffset now, CancellationToken ct);
    Task<bool> UpsertDealAsync(Guid brokerId, Mt5DealDto deal, DateTimeOffset now, CancellationToken ct);
    Task ReplacePositionsAsync(Guid brokerId, long login, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct);
    Task<IReadOnlyList<NormalizedDeal>> LoadDealsAsync(Guid brokerId, string brokerCode, long login, CancellationToken ct);
    Task ReplaceReconstructedAsync(Guid brokerId, long login, IReadOnlyList<ReconstructedTradeResult> trades, CancellationToken ct);
    Task UpsertScoreAsync(TraderScore score, CancellationToken ct);
    Task<Guid> ResolveBrokerIdAsync(string brokerCode, CancellationToken ct);
}

public sealed class DealIngestionService
{
    private readonly IBrokerRegistry _registry;
    private readonly ITradingStore _store;

    public DealIngestionService(IBrokerRegistry registry, ITradingStore store)
    {
        _registry = registry;
        _store = store;
    }

    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        foreach (var group in groups)
            await _store.UpsertGroupAsync(brokerId, group, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        foreach (var account in accounts)
        {
            await _store.UpsertAccountAsync(brokerId, account, now, ct);
            var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
            foreach (var deal in deals)
            {
                if (await _store.UpsertDealAsync(brokerId, deal, now, ct))
                    insertedDeals++;
            }

            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }

        return insertedDeals;
    }
}

public sealed class ReconstructionScoringService
{
    private readonly ITradingStore _store;
    private readonly TradeReconstructor _reconstructor;
    private readonly Domain.Scoring.BaselineScorer _scorer;

    public ReconstructionScoringService(
        ITradingStore store,
        TradeReconstructor reconstructor,
        Domain.Scoring.BaselineScorer scorer)
    {
        _store = store;
        _reconstructor = reconstructor;
        _scorer = scorer;
    }

    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            Login = login,
            RiskScore = score.RiskScore,
            BehaviorScore = score.BehaviorScore,
            EarlyQualityScore = score.EarlyQualityScore,
            CompletedXauTrades = score.Features.CompletedXauTrades,
            Martingale = score.Features.Martingale,
            AveragingDown = score.Features.AveragingDown,
            LotEscalation = score.Features.LotEscalation,
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);
    }
}
