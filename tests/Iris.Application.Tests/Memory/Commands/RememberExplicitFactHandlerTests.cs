using System;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Memory.Commands;
using Iris.Application.Memory.Options;
using Iris.Domain.Memories;
using Iris.Shared.Results;

namespace Iris.Application.Tests.Memory.Commands;

public sealed class RememberExplicitFactHandlerTests
{
    // T-APP-MEM-01: Happy path — valid content → Memory added and committed, Result is Success, DTO has Active status.
    [Fact]
    public async Task HandleAsync_ValidContent_AddsMemoryAndCommitsAndReturnsSuccess()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero));
        var repository = new FakeMemoryRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RememberExplicitFactHandler(repository, unitOfWork, clock, MemoryOptions.Default);
        var command = new RememberExplicitFactCommand("Айрис любит котиков");

        Result<RememberMemoryResult> result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(MemoryStatus.Active, result.Value.Memory.Status);
        Assert.Equal("Айрис любит котиков", result.Value.Memory.Content);
    }
}
