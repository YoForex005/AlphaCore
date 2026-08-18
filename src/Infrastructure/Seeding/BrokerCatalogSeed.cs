using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Domain.Brokers;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Infrastructure.Persistence;

namespace TraderIntelligence.Infrastructure.Seeding;

public static class BrokerCatalogSeed
{
    public static async Task EnsureAsync(TraderDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (!await db.Brokers.AnyAsync(b => b.Code == BrokerCodes.Achiever, ct))
        {
            db.Brokers.Add(new Broker
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                Code = BrokerCodes.Achiever,
                DisplayName = "Achiever",
                Server = "57.128.141.65",
                Port = 443,
                ManagerLogin = 2027,
                ServerName = "AchieverGlobalMarkets-Server",
                Mode = "local",
                PoolSize = 8,
                ProxyEnabled = true,
                ProxyHost = "81.29.145.69",
                ProxyPort = 49527,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (!await db.Brokers.AnyAsync(b => b.Code == BrokerCodes.StarwaveFx, ct))
        {
            db.Brokers.Add(new Broker
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
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
        }

        if (!await db.CanonicalInstruments.AnyAsync(ct))
        {
            db.CanonicalInstruments.Add(new CanonicalInstrument
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                Code = "XAUUSD",
                Description = "Gold vs US Dollar"
            });
        }

        if (!await db.KillSwitches.AnyAsync(ct))
        {
            db.KillSwitches.Add(new KillSwitch
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
                Mode = KillSwitchMode.None,
                SetBy = "system",
                Reason = "default",
                UpdatedAt = now
            });
        }

        if (!await db.FixSessionStates.AnyAsync(ct))
        {
            db.FixSessionStates.AddRange(
                new FixSessionState
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                    Qualifier = FixSessionQualifier.Quote,
                    Status = FixSessionStatus.Disconnected,
                    Host = "demo-us-eqx-01.p.c-trader.com",
                    Port = 5211,
                    SenderCompId = "demo.pepperstone.5328266",
                    TargetCompId = "cServer",
                    SenderSubId = "QUOTE",
                    TargetSubId = "QUOTE",
                    LastError = "not logged on yet",
                    UpdatedAt = now
                },
                new FixSessionState
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                    Qualifier = FixSessionQualifier.Trade,
                    Status = FixSessionStatus.Disconnected,
                    Host = "demo-us-eqx-01.p.c-trader.com",
                    Port = 5212,
                    SenderCompId = "demo.pepperstone.5328266",
                    TargetCompId = "cServer",
                    SenderSubId = "TRADE",
                    TargetSubId = "TRADE",
                    LastError = "session up for logon/recon only; NewOrderSingle off",
                    UpdatedAt = now
                });
        }

        await db.SaveChangesAsync(ct);
    }
}
