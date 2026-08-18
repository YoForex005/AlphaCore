using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Domain.Entities;

namespace TraderIntelligence.Infrastructure.Persistence;

public sealed class TraderDbContext : DbContext
{
    public TraderDbContext(DbContextOptions<TraderDbContext> options) : base(options)
    {
    }

    public DbSet<Broker> Brokers => Set<Broker>();
    public DbSet<Mt5Group> Mt5Groups => Set<Mt5Group>();
    public DbSet<Mt5Account> Mt5Accounts => Set<Mt5Account>();
    public DbSet<Mt5Deal> Mt5Deals => Set<Mt5Deal>();
    public DbSet<Mt5Position> Mt5Positions => Set<Mt5Position>();
    public DbSet<ReconstructedTrade> ReconstructedTrades => Set<ReconstructedTrade>();
    public DbSet<CanonicalInstrument> CanonicalInstruments => Set<CanonicalInstrument>();
    public DbSet<SourceSymbolMapping> SourceSymbolMappings => Set<SourceSymbolMapping>();
    public DbSet<TraderScore> TraderScores => Set<TraderScore>();
    public DbSet<TraderScoreHistory> TraderScoreHistory => Set<TraderScoreHistory>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<SyncCheckpoint> SyncCheckpoints => Set<SyncCheckpoint>();
    public DbSet<CopyIntent> CopyIntents => Set<CopyIntent>();
    public DbSet<RiskDecisionRecord> RiskDecisions => Set<RiskDecisionRecord>();
    public DbSet<ExecutionIntent> ExecutionIntents => Set<ExecutionIntent>();
    public DbSet<ShadowOrder> ShadowOrders => Set<ShadowOrder>();
    public DbSet<DestinationQuoteSnapshot> DestinationQuotes => Set<DestinationQuoteSnapshot>();
    public DbSet<FixSessionState> FixSessionStates => Set<FixSessionState>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<KillSwitch> KillSwitches => Set<KillSwitch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Broker>(e =>
        {
            e.ToTable("brokers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<Mt5Group>(e =>
        {
            e.ToTable("mt5_groups");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Mt5Account>(e =>
        {
            e.ToTable("mt5_accounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.Login }).IsUnique();
        });

        modelBuilder.Entity<Mt5Deal>(e =>
        {
            e.ToTable("mt5_deals");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.DealTicket }).IsUnique();
            e.HasIndex(x => new { x.BrokerId, x.Login, x.DealTime });
        });

        modelBuilder.Entity<Mt5Position>(e =>
        {
            e.ToTable("mt5_positions_current");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.PositionTicket }).IsUnique();
        });

        modelBuilder.Entity<ReconstructedTrade>(e =>
        {
            e.ToTable("reconstructed_trades");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.Login, x.PositionId, x.OpenedAt });
        });

        modelBuilder.Entity<CanonicalInstrument>(e =>
        {
            e.ToTable("canonical_instruments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<SourceSymbolMapping>(e =>
        {
            e.ToTable("source_symbol_mappings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.SourceSymbol }).IsUnique();
        });

        modelBuilder.Entity<TraderScore>(e =>
        {
            e.ToTable("trader_scores");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.Login }).IsUnique();
        });

        modelBuilder.Entity<TraderScoreHistory>(e =>
        {
            e.ToTable("trader_score_history");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.Login, x.RecordedAt });
        });

        modelBuilder.Entity<OutboxEvent>(e =>
        {
            e.ToTable("outbox_events");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProcessedAt);
        });

        modelBuilder.Entity<SyncCheckpoint>(e =>
        {
            e.ToTable("sync_checkpoints");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BrokerId, x.Login, x.Stream }).IsUnique();
        });

        modelBuilder.Entity<CopyIntent>(e =>
        {
            e.ToTable("copy_intents");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<RiskDecisionRecord>(e =>
        {
            e.ToTable("risk_decisions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CopyIntentId);
        });

        modelBuilder.Entity<ExecutionIntent>(e =>
        {
            e.ToTable("execution_intents");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClOrdId).IsUnique();
        });

        modelBuilder.Entity<ShadowOrder>(e =>
        {
            e.ToTable("shadow_orders");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<DestinationQuoteSnapshot>(e =>
        {
            e.ToTable("destination_quotes");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<FixSessionState>(e =>
        {
            e.ToTable("fix_sessions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Qualifier).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<KillSwitch>(e =>
        {
            e.ToTable("kill_switches");
            e.HasKey(x => x.Id);
        });
    }
}
