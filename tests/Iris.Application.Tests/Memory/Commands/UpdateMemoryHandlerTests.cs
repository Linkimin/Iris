using System;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Memory.Commands;
using Iris.Domain.Memories;
using Iris.Shared.Results;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Tests.Memory.Commands;

public sealed class UpdateMemoryHandlerTests
{
    // T-APP-MEM-12: Update on Forgotten memory → conflict error code; UpdateAsync NOT called.
    [Fact]
    public async Task HandleAsync_ForgottenMemory_ReturnsConflictError()
    {
        var now = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(now);
        var repository = new FakeMemoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var memoryId = MemoryId.New();

        var memory = DomainMemory.Create(
            memoryId,
            MemoryContent.Create("Original content"),
            MemoryKind.Fact,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            now.AddMinutes(-10));
        memory.Forget(now.AddMinutes(-5));

        await repository.AddAsync(memory, CancellationToken.None);
        var handler = new UpdateMemoryHandler(repository, unitOfWork, clock);
        var command = new UpdateMemoryCommand(memoryId, "New content");

        Result<UpdateMemoryResult> result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("memory.not_active", result.Error.Code);
        Assert.Equal(0, repository.UpdateCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }
}
