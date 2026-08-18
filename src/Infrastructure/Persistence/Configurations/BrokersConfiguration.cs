using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraderIntelligence.Domain.Entities;

namespace TraderIntelligence.Infrastructure.Persistence.Configurations;

public sealed class BrokersConfiguration : IEntityTypeConfiguration<Brokers>
{
    public void Configure(EntityTypeBuilder<Brokers> builder)
    {
        builder.ToTable("brokers");

        builder.Property<Guid>("id").HasColumnName("id").HasColumnType("uuid");
        builder.HasKey("id");

        builder.Property<string>("code").HasColumnName("code").HasColumnType("text");
        builder.Property<string>("name").HasColumnName("name").HasColumnType("text");
        builder.Property<DateTime>("created_at").HasColumnName("created_at").HasColumnType("timestamptz");

        builder.HasIndex("code").IsUnique();
    }
}

