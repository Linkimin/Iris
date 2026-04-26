using Iris.Domain.Common;

namespace Iris.Domain.Conversations;

public sealed record MessageContent
{
    private MessageContent(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MessageContent Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("message.empty_content", "Message content cannot be empty.");
        }

        return new MessageContent(value);
    }

    public override string ToString() => Value;
}
