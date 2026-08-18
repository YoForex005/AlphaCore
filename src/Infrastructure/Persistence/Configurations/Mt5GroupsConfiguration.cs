using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderIntelligence.Domain.Entities;

namespace TraderIntelligence.Infrastructure.Persistence.Configurations;

public sealed class Mt5GroupsConfiguration : IEntityTypeConfiguration<Mt5Groups>
{
    public void Configure(EntityTypeBuilder<Mt5Groups> builder)
    {
        builder.ToTable("mt5_groups");

        builder.Property<Guid>("id").HasColumnName("id").HasColumnType("uuid");
        builder.HasKey("id");

        builder.Property<Guid>("broker_id").HasColumnName("broker_id").HasColumnType("uuid");
        builder.Property<long>("group_id").HasColumnName("group_id").HasColumnType("bigint");
        builder.Property<string>("name").HasColumnName("name").HasColumnType("text");
        builder.Property<DateTime>("created_at").HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex("broker_id");
        builder.HasIndex("group_id");
        builder.HasIndex("broker_id", "group_id").IsUnique();
    }
}

