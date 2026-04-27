namespace Iris.Persistence.Entities;

public sealed class ConversationEntity
{
    public Guid Id { get; set; }

    public string? Title { get; set; }

    public int Status { get; set; }

    public int Mode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MessageEntity> Messages { get; } = new List<MessageEntity>();
}
