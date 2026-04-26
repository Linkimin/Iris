using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed record MessageId
{
    private MessageId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static MessageId New() => new(Guid.NewGuid());

    public static MessageId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("message.empty_id", "Message id cannot be empty.");
        }

        return new MessageId(value);
    }

    public override string ToString() => Value.ToString();
}
