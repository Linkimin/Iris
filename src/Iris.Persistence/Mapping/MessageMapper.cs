using Iris.Domain.Conversations;
using Iris.Persistence.Entities;

namespace Iris.Persistence.Mapping;

public static class MessageMapper
{
    private const string _emptyMetadataJson = "{}";

    public static MessageEntity ToEntity(Message message)
    {
        return new MessageEntity
        {
            Id = message.Id.Value,
            ConversationId = message.ConversationId.Value,
            Role = (int)message.Role,
            Content = message.Content.Value,
            CreatedAt = message.CreatedAt,
            MetadataJson = _emptyMetadataJson
        };
    }

    public static Message ToDomain(MessageEntity entity)
    {
        return Message.Create(
            MessageId.From(entity.Id),
            ConversationId.From(entity.ConversationId),
            (MessageRole)entity.Role,
            MessageContent.Create(entity.Content),
            MessageMetadata.Empty,
            entity.CreatedAt);
    }
}
