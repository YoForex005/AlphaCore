using TraderIntelligence.FixWorker;
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ReconstructionScoringService>(),
        CancellationToken.None);
}

host.Run();
