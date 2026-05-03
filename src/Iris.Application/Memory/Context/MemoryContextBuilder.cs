using Iris.Application.Abstractions.Persistence;
using Iris.Application.Chat.Prompting;
using Iris.Application.Memory.Options;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Memory.Context;

public sealed class MemoryContextBuilder
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly MemoryOptions _memoryOptions;

    public MemoryContextBuilder(
        IMemoryRepository memoryRepository,
        MemoryOptions memoryOptions)
    {
        _memoryRepository = memoryRepository;
        _memoryOptions = memoryOptions;
    }

    public async Task<IReadOnlyList<DomainMemory>> SelectAsync(
        PromptBuildRequest request,
        CancellationToken cancellationToken)
    {
        return await _memoryRepository.ListActiveAsync(
            _memoryOptions.PromptInjectionTopN,
            cancellationToken);
    }
}
