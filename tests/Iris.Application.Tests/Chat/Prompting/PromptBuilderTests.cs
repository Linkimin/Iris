using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Chat.Prompting;
using Iris.Application.Persona.Language;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Application.Tests.Chat.Prompting;

public sealed class PromptBuilderTests
{
    private const string _stubPrompt = "TEST_SYSTEM_PROMPT";

    [Fact]
    public void Build_IncludesSystemMessageHistoryAndCurrentUserMessage()
    {
        var builder = new PromptBuilder(new StubLanguagePolicy());
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
        PromptBuilder builder = new(new StubLanguagePolicy());

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
        PromptBuilder builder = new(new RussianDefaultLanguagePolicy(
            LanguageOptions.Default,
            new LanguageInstructionBuilder()));

        Result<PromptBuildResult> result = builder.Build(new PromptBuildRequest(
            Array.Empty<Message>(),
            MessageContent.Create("Hello")));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(
            "You are Iris, a local personal AI companion. Be helpful, clear, and respectful.",
            result.Value.ModelRequest.Messages[0].Content);
    }

    private sealed class StubLanguagePolicy : ILanguagePolicy
    {
        public string GetSystemPrompt() => _stubPrompt;
    }
}
