using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed record ConversationTitle
{
    private const int MaxLength = 120;

    private ConversationTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ConversationTitle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("conversation.empty_title", "Conversation title cannot be empty.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException("conversation.title_too_long", "Conversation title is too long.");
        }

        return new ConversationTitle(trimmed);
    }

    public override string ToString() => Value;
}
