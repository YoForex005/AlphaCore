using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Brokers;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Mt5.Connectors;

namespace TraderIntelligence.Infrastructure.Seeding;

public static class DemoSeeder
{
    public static async Task SeedAsync(
        TraderDbContext db,
        ITradingStore store,
        ReconstructionScoringService scoring,
        CancellationToken ct)
    {
        if (await db.Brokers.AnyAsync(ct))
            return;

        var now = DateTimeOffset.UtcNow;
        var achieverId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var starwaveId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

        db.Brokers.AddRange(
            new Broker
            {
                Id = achieverId,
                Code = BrokerCodes.Achiever,
                DisplayName = "Achiever",
                Server = "57.128.141.65",
                Port = 443,
                ManagerLogin = 2027,
                ServerName = "AchieverGlobalMarkets-Server",
                Mode = "local",
                PoolSize = 8,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Broker
            {
                Id = starwaveId,
                Code = BrokerCodes.StarwaveFx,
                DisplayName = "StarwaveFX",
                Server = "84.201.6.142",
                Port = 443,
                ManagerLogin = 9904,
                ServerName = "StarwaveFX",
                Mode = "local",
                PoolSize = 4,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            });

        db.CanonicalInstruments.Add(new CanonicalInstrument
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            Code = "XAUUSD",
            Description = "Gold vs US Dollar"
        });

        db.FixSessionStates.AddRange(
            new FixSessionState
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                Qualifier = FixSessionQualifier.Quote,
                Status = FixSessionStatus.Disconnected,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5211,
                SenderCompId = "live.pepperstone.1369850",
                TargetCompId = "cServer",
                SenderSubId = null,
                TargetSubId = "QUOTE",
                InboundSeq = 1,
                OutboundSeq = 1,
                LastInboundAt = now,
                LastOutboundAt = now,
                LastError = "No live QUOTE socket. Demo seed only.",
                UpdatedAt = now
            },
            new FixSessionState
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                Qualifier = FixSessionQualifier.Trade,
                Status = FixSessionStatus.Disconnected,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5212,
                SenderCompId = "live.pepperstone.1369850",
                TargetCompId = "cServer",
                TargetSubId = "TRADE",
                InboundSeq = 1,
                OutboundSeq = 1,
                LastInboundAt = now,
                LastOutboundAt = now,
                LastError = "No live TRADE socket. NewOrderSingle off.",
                UpdatedAt = now
            });

        db.DestinationQuotes.Add(new DestinationQuoteSnapshot
        {
            Id = Guid.NewGuid(),
            CanonicalSymbol = "XAUUSD",
            VenueInstrumentId = null,
            Bid = 2399.45m,
            Ask = 2399.85m,
            ReceivedAt = now
        });

        db.KillSwitches.Add(new KillSwitch
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
            Mode = KillSwitchMode.None,
            SetBy = "system",
            Reason = "default",
            UpdatedAt = now
        });

        await db.SaveChangesAsync(ct);

        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        var registry = new BrokerRegistry(new IMt5BrokerConnector[] { achiever, starwave });
        var ingestion = new DealIngestionService(registry, store);
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
        await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, ct);
        await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, ct);

        foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        {
            var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
            await scoring.RebuildTraderAsync(code, login, ct);
        }
    }
}
