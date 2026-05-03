using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Memories;
using Iris.Persistence.Database;
using Iris.Persistence.Entities;
using Iris.Persistence.Mapping;

using Microsoft.EntityFrameworkCore;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Persistence.Repositories;

public sealed class MemoryRepository : IMemoryRepository
{
    private readonly IrisDbContext _dbContext;

    public MemoryRepository(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DomainMemory?> GetByIdAsync(
        MemoryId id,
        CancellationToken ct)
    {
        MemoryEntity? entity = await _dbContext.Memories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                memory => memory.Id == id.Value,
                ct);

        return entity is null ? null : MemoryMapper.ToDomain(entity);
    }

    public async Task AddAsync(
        DomainMemory memory,
        CancellationToken ct)
    {
        MemoryEntity entity = MemoryMapper.ToEntity(memory);
        await _dbContext.Memories.AddAsync(entity, ct);
    }

    public async Task UpdateAsync(
        DomainMemory memory,
        CancellationToken ct)
    {
        MemoryEntity? entity = await _dbContext.Memories
            .FirstOrDefaultAsync(
                storedMemory => storedMemory.Id == memory.Id.Value,
                ct);

        if (entity is null)
        {
            throw new InvalidOperationException("Memory could not be found for update.");
        }

        entity.Content = memory.Content.Value;
        entity.Kind = (int)memory.Kind;
        entity.Importance = (int)memory.Importance;
        entity.Status = (int)memory.Status;
        entity.Source = (int)memory.Source;
        entity.CreatedAt = memory.CreatedAt;
        entity.UpdatedAt = memory.UpdatedAt;
    }

    public async Task<IReadOnlyList<DomainMemory>> ListActiveAsync(
        int limit,
        CancellationToken ct)
    {
        List<MemoryEntity> entities = await _dbContext.Memories
            .Where(memory => memory.Status == (int)MemoryStatus.Active)
            .OrderByDescending(memory => memory.UpdatedAt ?? memory.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(MemoryMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<DomainMemory>> SearchActiveAsync(
        string query,
        int limit,
        CancellationToken ct)
    {
        List<MemoryEntity> entities = await _dbContext.Memories
            .Where(memory =>
                memory.Status == (int)MemoryStatus.Active
                && EF.Functions.Like(memory.Content, $"%{query}%"))
            .OrderByDescending(memory => memory.UpdatedAt ?? memory.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(MemoryMapper.ToDomain).ToList();
    }
}
