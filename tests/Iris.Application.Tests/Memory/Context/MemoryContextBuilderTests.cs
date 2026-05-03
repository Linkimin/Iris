using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Chat.Prompting;
using Iris.Application.Memory.Context;
using Iris.Application.Memory.Options;
using Iris.Domain.Conversations;
using Iris.Domain.Memories;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Tests.Memory.Context;

public sealed class MemoryContextBuilderTests
{
    private static DomainMemory CreateMemory(string content)
    {
        return DomainMemory.Create(
            MemoryId.New(),
            MemoryContent.Create(content),
            MemoryKind.Fact,
            MemoryImportance.Normal,
            MemorySource.UserExplicit,
            DateTimeOffset.UtcNow);
    }

    // T-APP-MEM-21: empty active memories → IReadOnlyList.Count == 0, no exception.
    [Fact]
    public async Task SelectAsync_EmptyRepository_ReturnsEmptyList()
    {
        var repository = new FakeMemoryRepository();
        MemoryOptions options = MemoryOptions.Default;
        var builder = new MemoryContextBuilder(repository, options);
        var request = new PromptBuildRequest(
            Array.Empty<Message>(),
            MessageContent.Create("Hello"));

        IReadOnlyList<DomainMemory> result = await builder.SelectAsync(request, CancellationToken.None);

        Assert.Empty(result);
    }

    // T-APP-MEM-22: > top-N memories → result limited to top-N (PromptInjectionTopN).
    [Fact]
    public async Task SelectAsync_MoreThanTopN_ReturnsOnlyTopN()
    {
        var options = new MemoryOptions(PromptInjectionTopN: 3);
        var repository = new FakeMemoryRepository();

        // Add 5 memories — more than top-N (3)
        for (var i = 1; i <= 5; i++)
        {
            await repository.AddAsync(CreateMemory($"Memory content {i}"), CancellationToken.None);
        }

        var builder = new MemoryContextBuilder(repository, options);
        var request = new PromptBuildRequest(
            Array.Empty<Message>(),
            MessageContent.Create("Hello"));

        IReadOnlyList<DomainMemory> result = await builder.SelectAsync(request, CancellationToken.None);

        Assert.Equal(3, result.Count);
    }
}
