using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderIntelligence.Domain.Entities;

namespace TraderIntelligence.Infrastructure.Persistence.Configurations;

public sealed class Mt5AccountsConfiguration : IEntityTypeConfiguration<Mt5Accounts>
{
    public void Configure(EntityTypeBuilder<Mt5Accounts> builder)
    {
        builder.ToTable("mt5_accounts");

        builder.Property<Guid>("id").HasColumnName("id").HasColumnType("uuid");
        builder.HasKey("id");

        builder.Property<Guid>("broker_id").HasColumnName("broker_id").HasColumnType("uuid");
        builder.Property<long>("login").HasColumnName("login").HasColumnType("bigint");
        builder.Property<string>("name").HasColumnName("name").HasColumnType("text");
        builder.Property<DateTime>("created_at").HasColumnName("created_at").HasColumnType("timestamptz");

        builder.Property<bool>("is_active").HasColumnName("is_active").HasColumnType("boolean");

        builder.HasIndex("broker_id");
        builder.HasIndex("login");
        builder.HasIndex("broker_id", "login").IsUnique();
    }
}

