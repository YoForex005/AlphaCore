using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderIntelligence.Domain.Entities;

namespace TraderIntelligence.Infrastructure.Persistence.Configurations;

public sealed class Mt5PositionsConfiguration : IEntityTypeConfiguration<Mt5Positions>
{
    public void Configure(EntityTypeBuilder<Mt5Positions> builder)
    {
        builder.ToTable("mt5_positions");

        builder.Property<Guid>("id").HasColumnName("id").HasColumnType("uuid");
        builder.HasKey("id");

        builder.Property<Guid>("broker_id").HasColumnName("broker_id").HasColumnType("uuid");
        builder.Property<long>("position_ticket").HasColumnName("position_ticket").HasColumnType("bigint");
        builder.Property<long>("login").HasColumnName("login").HasColumnType("bigint");
        builder.Property<string>("symbol").HasColumnName("symbol").HasColumnType("text");

        builder.Property<decimal>("volume").HasColumnName("volume").HasColumnType("bigint");
        builder.Property<decimal>("price_open").HasColumnName("price_open").HasColumnType("decimal(18,8)");

        builder.Property<decimal>("current_price").HasColumnName("current_price").HasColumnType("decimal(18,8)");
        builder.Property<decimal>("profit").HasColumnName("profit").HasColumnType("decimal(18,8)");
        builder.Property<decimal>("swap").HasColumnName("swap").HasColumnType("decimal(18,8)");
        builder.Property<decimal>("commission").HasColumnName("commission").HasColumnType("decimal(18,8)");

        builder.Property<DateTime>("open_time").HasColumnName("open_time").HasColumnType("timestamptz");

        builder.HasIndex("broker_id");
        builder.HasIndex("login");
        builder.HasIndex("symbol");
        builder.HasIndex("broker_id", "position_ticket").IsUnique();
    }
}

