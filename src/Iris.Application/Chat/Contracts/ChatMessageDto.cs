using Iris.Domain.Conversations;

namespace Iris.Application.Chat.Contracts;

public sealed record ChatMessageDto(
    MessageId Id,
    ConversationId ConversationId,
    MessageRole Role,
    string Content,
    DateTimeOffset CreatedAt);
