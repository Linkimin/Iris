using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed class Message
{
    private Message(
        MessageId id,
        ConversationId conversationId,
        MessageRole role,
        MessageContent content,
        MessageMetadata metadata,
        DateTimeOffset createdAt)
    {
        Id = id;
        ConversationId = conversationId;
        Role = role;
        Content = content;
        Metadata = metadata;
        CreatedAt = createdAt;
    }

    public MessageId Id { get; }

    public ConversationId ConversationId { get; }

    public MessageRole Role { get; }

    public MessageContent Content { get; }

    public MessageMetadata Metadata { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Message Create(
        MessageId id,
        ConversationId conversationId,
        MessageRole role,
        MessageContent content,
        MessageMetadata metadata,
        DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException("message.invalid_role", "Message role is invalid.");
        }

        return new Message(
            id,
            conversationId,
            role,
            content,
            metadata,
            createdAt);
    }
}
