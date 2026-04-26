using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed record ConversationId
{
    private ConversationId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static ConversationId New() => new(Guid.NewGuid());

    public static ConversationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("conversation.empty_id", "Conversation id cannot be empty.");
        }

        return new ConversationId(value);
    }

    public override string ToString() => Value.ToString();
}
