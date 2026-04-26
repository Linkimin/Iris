using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed class Conversation
{
    private Conversation(
        ConversationId id,
        ConversationTitle? title,
        ConversationStatus status,
        ConversationMode mode,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Title = title;
        Status = status;
        Mode = mode;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public ConversationId Id { get; }

    public ConversationTitle? Title { get; private set; }

    public ConversationStatus Status { get; private set; }

    public ConversationMode Mode { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Conversation Create(
        ConversationId id,
        ConversationTitle? title,
        ConversationMode mode,
        DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException("conversation.invalid_mode", "Conversation mode is invalid.");
        }

        return new Conversation(
            id,
            title,
            ConversationStatus.Active,
            mode,
            createdAt,
            createdAt);
    }

    public static Conversation Rehydrate(
        ConversationId id,
        ConversationTitle? title,
        ConversationStatus status,
        ConversationMode mode,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("conversation.invalid_status", "Conversation status is invalid.");
        }

        if (!Enum.IsDefined(mode))
        {
            throw new DomainException("conversation.invalid_mode", "Conversation mode is invalid.");
        }

        if (updatedAt < createdAt)
        {
            throw new DomainException(
                "conversation.invalid_updated_at",
                "Conversation updated timestamp cannot be earlier than created timestamp.");
        }

        return new Conversation(
            id,
            title,
            status,
            mode,
            createdAt,
            updatedAt);
    }

    public void UpdateTitle(ConversationTitle title, DateTimeOffset updatedAt)
    {
        EnsureCanUpdate(updatedAt);
        Title = title;
        UpdatedAt = updatedAt;
    }

    public void Archive(DateTimeOffset updatedAt)
    {
        EnsureCanUpdate(updatedAt);
        Status = ConversationStatus.Archived;
        UpdatedAt = updatedAt;
    }

    public void Close(DateTimeOffset updatedAt)
    {
        EnsureCanUpdate(updatedAt);
        Status = ConversationStatus.Closed;
        UpdatedAt = updatedAt;
    }

    public void Touch(DateTimeOffset updatedAt)
    {
        EnsureCanUpdate(updatedAt);
        UpdatedAt = updatedAt;
    }

    private void EnsureCanUpdate(DateTimeOffset updatedAt)
    {
        if (updatedAt < UpdatedAt)
        {
            throw new DomainException(
                "conversation.invalid_updated_at",
                "Conversation updated timestamp cannot move backwards.");
        }
    }
}
