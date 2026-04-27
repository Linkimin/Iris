namespace Iris.Persistence.Entities;

public sealed class MessageEntity
{
    public long PersistenceId { get; set; }

    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public int Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public ConversationEntity? Conversation { get; set; }
}
