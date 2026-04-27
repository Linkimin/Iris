using Iris.Domain.Conversations;
using Iris.Persistence.Entities;

namespace Iris.Persistence.Mapping;

public static class ConversationMapper
{
    public static ConversationEntity ToEntity(Conversation conversation)
    {
        return new ConversationEntity
        {
            Id = conversation.Id.Value,
            Title = conversation.Title?.Value,
            Status = (int)conversation.Status,
            Mode = (int)conversation.Mode,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    public static Conversation ToDomain(ConversationEntity entity)
    {
        return Conversation.Rehydrate(
            ConversationId.From(entity.Id),
            entity.Title is null ? null : ConversationTitle.Create(entity.Title),
            (ConversationStatus)entity.Status,
            (ConversationMode)entity.Mode,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
