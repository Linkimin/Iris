using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Memories;
using Iris.Shared.Time.Interfaces;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Tests.Memory;

internal sealed class FakeClock : IClock
{
    private readonly DateTimeOffset[] _times;
    private int _index;

    public FakeClock(DateTimeOffset time)
    {
        _times = new[] { time };
    }

    public FakeClock(params DateTimeOffset[] times)
    {
        _times = times;
    }

    public DateTimeOffset UtcNow => _times[Math.Min(_index++, _times.Length - 1)];
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int CommitCalls { get; private set; }

    public Exception? CommitException { get; set; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        CommitCalls++;

        if (CommitException is not null)
        {
            throw CommitException;
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeMemoryRepository : IMemoryRepository
{
    private readonly Dictionary<MemoryId, DomainMemory> _store = new();

    public int AddCalls { get; private set; }
    public int UpdateCalls { get; private set; }

    public Task<DomainMemory?> GetByIdAsync(MemoryId id, CancellationToken ct)
    {
        _store.TryGetValue(id, out DomainMemory? memory);
        return Task.FromResult(memory);
    }

    public Task AddAsync(DomainMemory memory, CancellationToken ct)
    {
        AddCalls++;
        _store[memory.Id] = memory;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DomainMemory memory, CancellationToken ct)
    {
        UpdateCalls++;
        _store[memory.Id] = memory;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainMemory>> ListActiveAsync(int limit, CancellationToken ct)
    {
        IReadOnlyList<DomainMemory> result = _store.Values
            .Where(m => m.Status == MemoryStatus.Active)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DomainMemory>> SearchActiveAsync(string query, int limit, CancellationToken ct)
    {
        IReadOnlyList<DomainMemory> result = _store.Values
            .Where(m => m.Status == MemoryStatus.Active
                        && m.Content.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }

    public IReadOnlyDictionary<MemoryId, DomainMemory> Store => _store;
}
