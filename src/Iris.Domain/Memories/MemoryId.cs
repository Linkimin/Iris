using Iris.Domain.Common;

namespace Iris.Domain.Memories;

public sealed record MemoryId
{
    private MemoryId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static MemoryId New() => new(Guid.NewGuid());

    public static MemoryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("memory.empty_id", "Memory id cannot be empty.");
        }

        return new MemoryId(value);
    }

    public override string ToString() => Value.ToString();
}
