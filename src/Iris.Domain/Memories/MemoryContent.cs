using Iris.Domain.Common;

namespace Iris.Domain.Memories;

public sealed record MemoryContent
{
    public const int MaxLength = 4000;

    private MemoryContent(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MemoryContent Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("memory.empty_content", "Memory content cannot be empty.");
        }

        if (value.Length > MaxLength)
        {
            throw new DomainException("memory.content_too_long", $"Memory content cannot exceed {MaxLength} characters.");
        }

        return new MemoryContent(value);
    }

    public override string ToString() => Value;
}
