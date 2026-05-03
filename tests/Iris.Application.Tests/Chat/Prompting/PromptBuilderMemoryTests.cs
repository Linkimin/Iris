using System;
using System.Collections.Generic;
using System.Linq;

using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Abstractions.Persistence;
using Iris.Application.Chat.Prompting;
using Iris.Application.Memory.Context;
using Iris.Application.Memory.Options;
using Iris.Application.Persona.Language;
using Iris.Domain.Conversations;
using Iris.Domain.Memories;
using Iris.Shared.Results;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Tests.Chat.Prompting;

public sealed class PromptBuilderMemoryTests
{
    private const string _stubSystemPrompt = "TEST_SYSTEM_PROMPT";

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

    private static PromptBuilder CreateBuilder(IMemoryRepository? repository = null)
    {
        MemoryOptions memoryOptions = MemoryOptions.Default;
        IMemoryRepository memoryRepository = repository ?? new EmptyStubMemoryRepository();

        return new PromptBuilder(
            new StubLanguagePolicy(),
            new MemoryContextBuilder(memoryRepository, memoryOptions),
            new MemoryPromptFormatter());
    }

    // T-APP-PROMPT-01: Empty memory list → no "Известные факты:" in any message.
    [Fact]
    public void Build_EmptyMemoryList_DoesNotInjectMemoryBlock()
    {
        PromptBuilder builder = CreateBuilder();

        Result<PromptBuildResult> result = builder.Build(new PromptBuildRequest(
            Array.Empty<Message>(),
            MessageContent.Create("Hello")));

        Assert.True(result.IsSuccess);
        IReadOnlyList<ChatModelMessage> messages = result.Value.ModelRequest.Messages;

        // No message should contain "Известные факты:"
        Assert.DoesNotContain(messages, m => m.Content.Contains("Известные факты:"));
    }

    // T-APP-PROMPT-02: Non-empty memory list → exactly one System message with "Известные факты:" and both memory contents.
    [Fact]
    public void Build_WithMemories_InjectsMemoryBlockAsSystemMessage()
    {
        PromptBuilder builder = CreateBuilder();
        DomainMemory memory1 = CreateMemory("Айрис любит котиков");
        DomainMemory memory2 = CreateMemory("Пользователь программист");

        Result<PromptBuildResult> result = builder.Build(
            new PromptBuildRequest(
                Array.Empty<Message>(),
                MessageContent.Create("Hello")),
            new[] { memory1, memory2 });

        Assert.True(result.IsSuccess);
        IReadOnlyList<ChatModelMessage> messages = result.Value.ModelRequest.Messages;

        // Must have a System message that contains "Известные факты:" and both memory texts
        ChatModelMessage? memoryMessage = messages
            .FirstOrDefault(m => m.Role == ChatModelRole.System && m.Content.Contains("Известные факты:"));

        Assert.NotNull(memoryMessage);
        Assert.Contains("Айрис любит котиков", memoryMessage.Content);
        Assert.Contains("Пользователь программист", memoryMessage.Content);
    }

    // T-APP-PROMPT-03: Non-empty memory list → no User-role message contains memory content.
    [Fact]
    public void Build_WithMemory_DoesNotInjectMemoryIntoUserMessages()
    {
        PromptBuilder builder = CreateBuilder();
        DomainMemory memory1 = CreateMemory("Айрис любит котиков");

        Result<PromptBuildResult> result = builder.Build(
            new PromptBuildRequest(
                Array.Empty<Message>(),
                MessageContent.Create("Hello")),
            new[] { memory1 });

        Assert.True(result.IsSuccess);
        var userMessages = result.Value.ModelRequest.Messages
            .Where(m => m.Role == ChatModelRole.User)
            .ToList();

        Assert.DoesNotContain(userMessages, m => m.Content.Contains("Известные факты:"));
        Assert.DoesNotContain(userMessages, m => m.Content.Contains("Айрис любит котиков"));
    }

    private sealed class StubLanguagePolicy : ILanguagePolicy
    {
        public string GetSystemPrompt() => _stubSystemPrompt;
    }

    private sealed class EmptyStubMemoryRepository : IMemoryRepository
    {
        public Task<DomainMemory?> GetByIdAsync(MemoryId id, CancellationToken ct)
            => Task.FromResult<DomainMemory?>(null);

        public Task AddAsync(DomainMemory memory, CancellationToken ct)
            => Task.CompletedTask;

        public Task UpdateAsync(DomainMemory memory, CancellationToken ct)
            => Task.CompletedTask;

        public Task<IReadOnlyList<DomainMemory>> ListActiveAsync(int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DomainMemory>>(Array.Empty<DomainMemory>());

        public Task<IReadOnlyList<DomainMemory>> SearchActiveAsync(string query, int limit, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<DomainMemory>>(Array.Empty<DomainMemory>());
    }
}
