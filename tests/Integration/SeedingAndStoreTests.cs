using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Brokers;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;
using TraderIntelligence.Mt5.Connectors;

namespace TraderIntelligence.Tests.Integration;

public class SeedingAndStoreTests
{
    [Fact]
    public async Task Demo_seed_discovers_groups_reconstructs_and_scores()
    {
        var options = new DbContextOptionsBuilder<TraderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new TraderDbContext(options);
        var store = new EfTradingStore(db);
        var scoring = new ReconstructionScoringService(store, new TradeReconstructor(), new BaselineScorer());

        await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);

        db.Brokers.Should().HaveCount(2);
        db.Mt5Groups.Count().Should().BeGreaterThan(2);
        db.Mt5Deals.Count().Should().BeGreaterThan(0);
        db.ReconstructedTrades.Any(t => t.Completed && t.CanonicalSymbol == "XAUUSD").Should().BeTrue();
        db.TraderScores.Single(s => s.Login == 10001).CompletedXauTrades.Should().Be(3);
        db.TraderScores.Single(s => s.Login == 10001).CurrentState.Should().NotBe(TraderIntelligence.Domain.Enums.TraderState.LIVE);
        db.TraderScores.Single(s => s.Login == 10002).CurrentState.Should().Be(TraderIntelligence.Domain.Enums.TraderState.RISK_BLOCKED);
        db.FixSessionStates.Should().HaveCount(2);
        db.FixSessionStates.Select(s => s.TargetCompId).Distinct().Should().Equal("cServer");
    }

    [Fact]
    public async Task Deal_upsert_is_idempotent()
    {
        var options = new DbContextOptionsBuilder<TraderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new TraderDbContext(options);
        db.Brokers.Add(new TraderIntelligence.Domain.Entities.Broker
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
            Code = BrokerCodes.Achiever,
            DisplayName = "A",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var store = new EfTradingStore(db);
        var deal = new TraderIntelligence.Application.Contracts.Mt5DealDto(
            1, 1, 1, 1, "XAUUSD", TraderIntelligence.Domain.Enums.DealAction.Buy,
            TraderIntelligence.Domain.Enums.DealEntry.In, 1000, 1, 0, 0, 0, DateTimeOffset.UtcNow, null);
        var brokerId = await store.ResolveBrokerIdAsync(BrokerCodes.Achiever, CancellationToken.None);
        (await store.UpsertDealAsync(brokerId, deal, DateTimeOffset.UtcNow, CancellationToken.None)).Should().BeTrue();
        (await store.UpsertDealAsync(brokerId, deal, DateTimeOffset.UtcNow, CancellationToken.None)).Should().BeFalse();
        db.Mt5Deals.Should().HaveCount(1);
    }
}
