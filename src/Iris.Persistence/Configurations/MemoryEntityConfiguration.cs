using Iris.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Iris.Persistence.Configurations;

public sealed class MemoryEntityConfiguration : IEntityTypeConfiguration<MemoryEntity>
{
    private static readonly ValueConverter<DateTimeOffset, long> _utcTicksConverter = new(
        value => value.UtcTicks,
        value => new DateTimeOffset(value, TimeSpan.Zero));

    private static readonly ValueConverter<DateTimeOffset?, long?> _nullableUtcTicksConverter = new(
        value => value.HasValue ? value.Value.UtcTicks : null,
        value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);

    public void Configure(EntityTypeBuilder<MemoryEntity> builder)
    {
        builder.ToTable("memories");

        builder.HasKey(memory => memory.Id);

        builder.Property(memory => memory.Id)
            .ValueGeneratedNever();

        builder.Property(memory => memory.Content)
            .IsRequired()
            .UseCollation("NOCASE");

        builder.Property(memory => memory.Kind)
            .IsRequired();

        builder.Property(memory => memory.Importance)
            .IsRequired();

        builder.Property(memory => memory.Status)
            .IsRequired();

        builder.Property(memory => memory.Source)
            .IsRequired();

        builder.Property(memory => memory.CreatedAt)
            .HasConversion(_utcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.Property(memory => memory.UpdatedAt)
            .HasConversion(_nullableUtcTicksConverter)
            .HasColumnType("INTEGER");

        builder.HasIndex(memory => memory.Status);
        builder.HasIndex(memory => memory.UpdatedAt);
    }
}
