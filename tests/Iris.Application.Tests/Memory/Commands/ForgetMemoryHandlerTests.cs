using System;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Memory.Commands;
using Iris.Domain.Memories;
using Iris.Shared.Results;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Tests.Memory.Commands;

public sealed class ForgetMemoryHandlerTests
{
    private static DomainMemory CreateActiveMemory(MemoryId id, DateTimeOffset createdAt)
    {
        return DomainMemory.Create(
            id,
            MemoryContent.Create("Test memory content"),
            MemoryKind.Fact,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            createdAt);
    }

    // T-APP-MEM-06: Active memory → forgotten happy path (UpdateAsync + CommitAsync called).
    [Fact]
    public async Task HandleAsync_ActiveMemory_TransitionsToForgottenAndCommits()
    {
        var now = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(now);
        var repository = new FakeMemoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var memoryId = MemoryId.New();
        DomainMemory memory = CreateActiveMemory(memoryId, now.AddMinutes(-5));
        await repository.AddAsync(memory, CancellationToken.None);
        var handler = new ForgetMemoryHandler(repository, unitOfWork, clock);
        var command = new ForgetMemoryCommand(memoryId);

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.UpdateCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
    }

    // T-APP-MEM-07: Missing id → not_found error code; UpdateAsync NOT called.
    [Fact]
    public async Task HandleAsync_NonExistentId_ReturnsNotFoundError()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var repository = new FakeMemoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ForgetMemoryHandler(repository, unitOfWork, clock);
        var command = new ForgetMemoryCommand(MemoryId.New());

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("memory.not_found", result.Error.Code);
        Assert.Equal(0, repository.UpdateCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    // T-APP-MEM-08: Already-forgotten → idempotent Success; UpdateAsync and CommitAsync NOT called.
    [Fact]
    public async Task HandleAsync_AlreadyForgottenMemory_ReturnsSuccessIdempotently()
    {
        var now = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(now);
        var repository = new FakeMemoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var memoryId = MemoryId.New();
        DomainMemory memory = CreateActiveMemory(memoryId, now.AddMinutes(-10));
        memory.Forget(now.AddMinutes(-5)); // pre-forget
        await repository.AddAsync(memory, CancellationToken.None);
        // Reset counters after pre-seeding
        var freshRepository = new FakeMemoryRepository();
        await freshRepository.AddAsync(memory, CancellationToken.None);
        var handler = new ForgetMemoryHandler(freshRepository, unitOfWork, clock);
        var command = new ForgetMemoryCommand(memoryId);

        Result result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, freshRepository.UpdateCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }
}
