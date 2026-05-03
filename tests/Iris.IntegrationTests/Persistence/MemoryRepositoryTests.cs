using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Iris.Domain.Memories;
using Iris.Persistence.Database;
using Iris.Persistence.Repositories;
using Iris.Persistence.UnitOfWork;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Integration.Tests.Persistence;

public sealed class MemoryRepositoryTests
{
    // T-PERS-MEM-01: Round-trip — add memory with Cyrillic content, read back, assert all fields preserved.
    [Fact]
    public async Task AddAndGetByIdAsync_PersistsMemoryWithCyrillicContent()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var memoryId = MemoryId.New();
        var memory = DomainMemory.Create(
            memoryId,
            MemoryContent.Create("Айрис любит Котиков"),
            MemoryKind.Fact,
            MemoryImportance.High,
            MemorySource.UserExplicit,
            createdAt);

        await using (IrisDbContext writeContext = await factory.CreateInitializedContextAsync())
        {
            var repository = new MemoryRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await repository.AddAsync(memory, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using IrisDbContext readContext = factory.CreateContext();
        var readRepository = new MemoryRepository(readContext);

        DomainMemory? persisted = await readRepository.GetByIdAsync(memoryId, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(memoryId, persisted.Id);
        Assert.Equal("Айрис любит Котиков", persisted.Content.Value);
        Assert.Equal(MemoryKind.Fact, persisted.Kind);
        Assert.Equal(MemoryImportance.High, persisted.Importance);
        Assert.Equal(MemoryStatus.Active, persisted.Status);
        Assert.Equal(MemorySource.UserExplicit, persisted.Source);
        Assert.Equal(createdAt.UtcTicks, persisted.CreatedAt.UtcTicks);
        Assert.Null(persisted.UpdatedAt);
    }

    // T-PERS-MEM-03: ListActiveAsync excludes Forgotten memories.
    [Fact]
    public async Task ListActiveAsync_ExcludesForgottenMemories()
    {
        await using var factory = new PersistenceTestContextFactory();
        var now = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var activeId1 = MemoryId.New();
        var activeId2 = MemoryId.New();
        var forgottenId = MemoryId.New();

        var active1 = DomainMemory.Create(
            activeId1,
            MemoryContent.Create("Active memory 1"),
            MemoryKind.Note,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            now);

        var active2 = DomainMemory.Create(
            activeId2,
            MemoryContent.Create("Active memory 2"),
            MemoryKind.Note,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            now.AddMinutes(1));

        var forgotten = DomainMemory.Create(
            forgottenId,
            MemoryContent.Create("Forgotten memory"),
            MemoryKind.Note,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            now.AddMinutes(2));
        forgotten.Forget(now.AddMinutes(3));

        await using (IrisDbContext writeContext = await factory.CreateInitializedContextAsync())
        {
            var repository = new MemoryRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await repository.AddAsync(active1, CancellationToken.None);
            await repository.AddAsync(active2, CancellationToken.None);
            await repository.AddAsync(forgotten, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using IrisDbContext readContext = factory.CreateContext();
        var readRepository = new MemoryRepository(readContext);

        IReadOnlyList<DomainMemory> result = await readRepository.ListActiveAsync(100, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, m => m.Id == forgottenId);
        Assert.Contains(result, m => m.Id == activeId1);
        Assert.Contains(result, m => m.Id == activeId2);
    }

    // T-PERS-MEM-04: SearchActiveAsync returns matching memories for Cyrillic substring query.
    // Note: SQLite LIKE does not support case-insensitive Cyrillic matching (P2-004 — known limitation).
    // This test uses exact-case query to verify that SearchActiveAsync finds the memory.
    [Fact]
    public async Task SearchActiveAsync_CyrillicSubstringQuery_ReturnsMatchingMemory()
    {
        await using var factory = new PersistenceTestContextFactory();
        var now = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var memoryId = MemoryId.New();
        var memory = DomainMemory.Create(
            memoryId,
            MemoryContent.Create("Айрис помнит файлы"),
            MemoryKind.Fact,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            now);

        await using (IrisDbContext writeContext = await factory.CreateInitializedContextAsync())
        {
            var repository = new MemoryRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await repository.AddAsync(memory, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using IrisDbContext readContext = factory.CreateContext();
        var readRepository = new MemoryRepository(readContext);

        // Search with exact-case Cyrillic — confirms substring search works
        IReadOnlyList<DomainMemory> result = await readRepository.SearchActiveAsync("Айрис", 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(memoryId, result[0].Id);
        Assert.Contains("Айрис", result[0].Content.Value);
    }
}
