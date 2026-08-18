using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Infrastructure.Persistence.Configurations;

namespace TraderIntelligence.Infrastructure.Persistence;

public class TraderDbContext : DbContext
{
    public TraderDbContext(DbContextOptions<TraderDbContext> options) : base(options)
    {
    }

    public DbSet<Brokers> Brokers => Set<Brokers>();
    public DbSet<Mt5Groups> Mt5Groups => Set<Mt5Groups>();
    public DbSet<Mt5Accounts> Mt5Accounts => Set<Mt5Accounts>();
    public DbSet<Mt5Deals> Mt5Deals => Set<Mt5Deals>();
    public DbSet<Mt5Positions> Mt5Positions => Set<Mt5Positions>();
    public DbSet<ReconstructedTrades> ReconstructedTrades => Set<ReconstructedTrades>();
    public DbSet<CanonicalInstruments> CanonicalInstruments => Set<CanonicalInstruments>();
    public DbSet<SourceSymbolMappings> SourceSymbolMappings => Set<SourceSymbolMappings>();
    public DbSet<TraderScores> TraderScores => Set<TraderScores>();
    public DbSet<TraderScoreHistory> TraderScoreHistory => Set<TraderScoreHistory>();
    public DbSet<TraderRiskFlags> TraderRiskFlags => Set<TraderRiskFlags>();
    public DbSet<OutboxEvents> OutboxEvents => Set<OutboxEvents>();
    public DbSet<SyncCheckpoints> SyncCheckpoints => Set<SyncCheckpoints>();
    public DbSet<CopyIntents> CopyIntents => Set<CopyIntents>();
    public DbSet<RiskDecisions> RiskDecisions => Set<RiskDecisions>();
    public DbSet<ExecutionIntents> ExecutionIntents => Set<ExecutionIntents>();
    public DbSet<ShadowOrders> ShadowOrders => Set<ShadowOrders>();
    public DbSet<ShadowFills> ShadowFills => Set<ShadowFills>();
    public DbSet<DestinationQuotes> DestinationQuotes => Set<DestinationQuotes>();
    public DbSet<FixSessionStates> FixSessionStates => Set<FixSessionStates>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BrokersConfiguration());
        modelBuilder.ApplyConfiguration(new Mt5GroupsConfiguration());
        modelBuilder.ApplyConfiguration(new Mt5AccountsConfiguration());
        modelBuilder.ApplyConfiguration(new Mt5DealsConfiguration());
        modelBuilder.ApplyConfiguration(new Mt5PositionsConfiguration());
        modelBuilder.ApplyConfiguration(new ReconstructedTradesConfiguration());
        modelBuilder.ApplyConfiguration(new CanonicalInstrumentsConfiguration());
        modelBuilder.ApplyConfiguration(new SourceSymbolMappingsConfiguration());
        modelBuilder.ApplyConfiguration(new TraderScoresConfiguration());
        modelBuilder.ApplyConfiguration(new TraderScoreHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new TraderRiskFlagsConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxEventsConfiguration());
        modelBuilder.ApplyConfiguration(new SyncCheckpointsConfiguration());
        modelBuilder.ApplyConfiguration(new CopyIntentsConfiguration());
        modelBuilder.ApplyConfiguration(new RiskDecisionsConfiguration());
        modelBuilder.ApplyConfiguration(new ExecutionIntentsConfiguration());
        modelBuilder.ApplyConfiguration(new ShadowOrdersConfiguration());
        modelBuilder.ApplyConfiguration(new ShadowFillsConfiguration());
        modelBuilder.ApplyConfiguration(new DestinationQuotesConfiguration());
        modelBuilder.ApplyConfiguration(new FixSessionStatesConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}

