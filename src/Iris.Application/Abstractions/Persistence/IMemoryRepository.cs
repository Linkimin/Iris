using Iris.Domain.Memories;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Abstractions.Persistence;

public interface IMemoryRepository
{
    Task<DomainMemory?> GetByIdAsync(MemoryId id, CancellationToken ct);

    Task AddAsync(DomainMemory memory, CancellationToken ct);

    Task UpdateAsync(DomainMemory memory, CancellationToken ct);

    Task<IReadOnlyList<DomainMemory>> ListActiveAsync(int limit, CancellationToken ct);

    Task<IReadOnlyList<DomainMemory>> SearchActiveAsync(string query, int limit, CancellationToken ct);
}
