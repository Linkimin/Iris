using Iris.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Iris.Persistence.Configurations;

public sealed class ConversationEntityConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    private static readonly ValueConverter<DateTimeOffset, long> _utcTicksConverter = new(
        value => value.UtcTicks,
        value => new DateTimeOffset(value, TimeSpan.Zero));

    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Id)
            .ValueGeneratedNever();

        builder.Property(conversation => conversation.Title)
            .HasMaxLength(120);

        builder.Property(conversation => conversation.Status)
            .IsRequired();

        builder.Property(conversation => conversation.Mode)
            .IsRequired();

        builder.Property(conversation => conversation.CreatedAt)
            .HasConversion(_utcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.Property(conversation => conversation.UpdatedAt)
            .HasConversion(_utcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.HasIndex(conversation => conversation.UpdatedAt);
    }
}
