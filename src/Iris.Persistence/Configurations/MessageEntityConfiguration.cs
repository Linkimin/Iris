using Iris.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Iris.Persistence.Configurations;

public sealed class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    private static readonly ValueConverter<DateTimeOffset, long> _utcTicksConverter = new(
        value => value.UtcTicks,
        value => new DateTimeOffset(value, TimeSpan.Zero));

    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(message => message.PersistenceId);

        builder.Property(message => message.PersistenceId)
            .ValueGeneratedOnAdd();

        builder.Property(message => message.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(message => message.ConversationId)
            .IsRequired();

        builder.Property(message => message.Role)
            .IsRequired();

        builder.Property(message => message.Content)
            .IsRequired();

        builder.Property(message => message.CreatedAt)
            .HasConversion(_utcTicksConverter)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.Property(message => message.MetadataJson)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => message.Id)
            .IsUnique();

        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt, message.PersistenceId });
    }
}
