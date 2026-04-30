using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Chat.Prompting;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Application.Tests.Chat.Prompting;

public sealed class PromptBuilderTests
{
    [Fact]
    public void Build_IncludesSystemMessageHistoryAndCurrentUserMessage()
    {
        var builder = new PromptBuilder();
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
                Assert.NotEqual(string.Empty, message.Content);
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
}
