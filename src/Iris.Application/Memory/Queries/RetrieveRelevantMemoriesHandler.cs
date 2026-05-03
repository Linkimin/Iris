using Iris.Application.Abstractions.Persistence;
using Iris.Application.Memory.Contracts;
using Iris.Application.Memory.Options;
using Iris.Shared.Results;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Memory.Queries;

public sealed class RetrieveRelevantMemoriesHandler
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly MemoryOptions _memoryOptions;

    public RetrieveRelevantMemoriesHandler(
        IMemoryRepository memoryRepository,
        MemoryOptions memoryOptions)
    {
        _memoryRepository = memoryRepository;
        _memoryOptions = memoryOptions;
    }

    public async Task<Result<IReadOnlyList<MemoryDto>>> HandleAsync(
        RetrieveRelevantMemoriesQuery query,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = query.Limit ?? _memoryOptions.RetrieveDefaultLimit;

        IReadOnlyList<DomainMemory> memories;

        try
        {
            memories = await _memoryRepository.SearchActiveAsync(query.Query, effectiveLimit, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<IReadOnlyList<MemoryDto>>.Failure(Error.Failure(
                "memory.persistence_failed",
                "Memories could not be retrieved."));
        }

        return Result<IReadOnlyList<MemoryDto>>.Success(memories.Select(MapDto).ToList());
    }

    private static MemoryDto MapDto(DomainMemory memory)
    {
        return new MemoryDto(
            memory.Id,
            memory.Content.Value,
            memory.Kind,
            memory.Importance,
            memory.Status,
            memory.CreatedAt,
            memory.UpdatedAt);
    }
}
