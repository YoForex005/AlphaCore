using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderIntelligence.Domain.Entities;

namespace TraderIntelligence.Infrastructure.Persistence.Configurations;

public sealed class Mt5DealsConfiguration : IEntityTypeConfiguration<Mt5Deals>
{
    public void Configure(EntityTypeBuilder<Mt5Deals> builder)
    {
        builder.ToTable("mt5_deals");

        builder.Property<Guid>("id").HasColumnName("id").HasColumnType("uuid");
        builder.HasKey("id");

        builder.Property<Guid>("broker_id").HasColumnName("broker_id").HasColumnType("uuid");
        builder.Property<long>("deal_ticket").HasColumnName("deal_ticket").HasColumnType("bigint");
        builder.Property<long>("login").HasColumnName("login").HasColumnType("bigint");
        builder.Property<string>("symbol").HasColumnName("symbol").HasColumnType("text");

        builder.Property<decimal>("volume").HasColumnName("volume").HasColumnType("bigint");
        builder.Property<decimal>("price").HasColumnName("price").HasColumnType("decimal(18,8)");
        builder.Property<decimal>("profit").HasColumnName("profit").HasColumnType("decimal(18,8)");
        builder.Property<decimal>("commission").HasColumnName("commission").HasColumnType("decimal(18,8)");

        builder.Property<DateTime>("open_time").HasColumnName("open_time").HasColumnType("timestamptz");
        builder.Property<DateTime?>("close_time").HasColumnName("close_time").HasColumnType("timestamptz");

        builder.Property<int>("entry_type").HasColumnName("entry_type").HasColumnType("integer");
        builder.Property<int>("reason").HasColumnName("reason").HasColumnType("integer");

        builder.HasIndex("broker_id");
        builder.HasIndex("login");
        builder.HasIndex("symbol");
        builder.HasIndex("broker_id", "deal_ticket").IsUnique();
    }
}

