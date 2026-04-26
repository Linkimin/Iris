namespace Iris.Domain.Conversations;

public sealed record MessageMetadata
{
    public static MessageMetadata Empty { get; } = new();
}
