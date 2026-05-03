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

public sealed class PromptBuilderTests
{
    private const string _stubPrompt = "TEST_SYSTEM_PROMPT";

    private static readonly MemoryOptions _memoryOptions = MemoryOptions.Default;
    private static readonly MemoryPromptFormatter _memoryPromptFormatter = new();
    private static readonly StubMemoryRepository _stubMemoryRepository = new();

    [Fact]
    public void Build_IncludesSystemMessageHistoryAndCurrentUserMessage()
    {
        PromptBuilder builder = CreateBuilder();
        var conversationId = ConversationId.New();
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        Message[] history = new[]
        {
            Message.Create(
                MessageId.New(),
                conversationId,
                MessageRole.User,
                MessageContent.Create("Previous user"),
                MessageMetadata.Empty,
                createdAt),
            Message.Create(
                MessageId.New(),
                conversationId,
                MessageRole.Assistant,
                MessageContent.Create("Previous assistant"),
                MessageMetadata.Empty,
                createdAt.AddSeconds(1))
        };

        Result<PromptBuildResult> result = builder.Build(new PromptBuildRequest(
            history,
            MessageContent.Create("Current user")));

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value.ModelRequest.Messages,
            message =>
            {
                Assert.Equal(ChatModelRole.System, message.Role);
                Assert.Equal(_stubPrompt, message.Content);
            },
            message =>
            {
                Assert.Equal(ChatModelRole.User, message.Role);
                Assert.Equal("Previous user", message.Content);
            },
            message =>
            {
                Assert.Equal(ChatModelRole.Assistant, message.Role);
                Assert.Equal("Previous assistant", message.Content);
            },
            message =>
            {
                Assert.Equal(ChatModelRole.User, message.Role);
                Assert.Equal("Current user", message.Content);
            });
    }

    [Fact]
    public void Build_UsesInjectedLanguagePolicy_ForSystemMessage()
    {
        PromptBuilder builder = CreateBuilder();

        Result<PromptBuildResult> result = builder.Build(new PromptBuildRequest(
            Array.Empty<Message>(),
            MessageContent.Create("Hello")));

        Assert.True(result.IsSuccess);
        Assert.Equal(_stubPrompt, result.Value.ModelRequest.Messages[0].Content);
        Assert.Equal(ChatModelRole.System, result.Value.ModelRequest.Messages[0].Role);
    }

    [Fact]
    public void Build_WithDefaultRussianPolicy_DoesNotEmitLegacyEnglishBaseline()
    {
        PromptBuilder builder = new(
            new RussianDefaultLanguagePolicy(
                LanguageOptions.Default,
                new LanguageInstructionBuilder()),
            new MemoryContextBuilder(_stubMemoryRepository, _memoryOptions),
            _memoryPromptFormatter);

        Result<PromptBuildResult> result = builder.Build(new PromptBuildRequest(
            Array.Empty<Message>(),
            MessageContent.Create("Hello")));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(
            "You are Iris, a local personal AI companion. Be helpful, clear, and respectful.",
            result.Value.ModelRequest.Messages[0].Content);
    }

    private static PromptBuilder CreateBuilder()
    {
        return new PromptBuilder(
            new StubLanguagePolicy(),
            new MemoryContextBuilder(_stubMemoryRepository, _memoryOptions),
            _memoryPromptFormatter);
    }

    private sealed class StubLanguagePolicy : ILanguagePolicy
    {
        public string GetSystemPrompt() => _stubPrompt;
    }

    private sealed class StubMemoryRepository : IMemoryRepository
    {
        public Task<DomainMemory?> GetByIdAsync(MemoryId id, CancellationToken ct)
        {
            return Task.FromResult<DomainMemory?>(null);
        }

        public Task AddAsync(DomainMemory memory, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(DomainMemory memory, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DomainMemory>> ListActiveAsync(int limit, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<DomainMemory>>(Array.Empty<DomainMemory>());
        }

        public Task<IReadOnlyList<DomainMemory>> SearchActiveAsync(string query, int limit, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<DomainMemory>>(Array.Empty<DomainMemory>());
        }
    }
}
