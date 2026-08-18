using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TraderIntelligence.Infrastructure.Persistence;

public sealed class TraderDbContextFactory : IDesignTimeDbContextFactory<TraderDbContext>
{
    public TraderDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__TraderIntelligence")
                         ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                         ?? "Host=127.0.0.1;Port=5432;Database=trader_intelligence;Username=ti;Password=ti_dev_only";

        var options = new DbContextOptionsBuilder<TraderDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new TraderDbContext(options);
    }
}
