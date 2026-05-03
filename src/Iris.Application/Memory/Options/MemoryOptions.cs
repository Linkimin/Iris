namespace Iris.Application.Memory.Options;

public sealed record MemoryOptions(
    int PromptInjectionTopN = 5,
    int MaxListPageSize = 200,
    int RetrieveDefaultLimit = 10)
{
    public static MemoryOptions Default { get; } = new();
}
