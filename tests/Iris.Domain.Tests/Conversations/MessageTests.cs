using Iris.Domain.Common;
using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class MessageTests
{
    [Fact]
    public void Create_ReturnsMessageBelongingToConversation()
    {
        var conversationId = ConversationId.New();
        var messageId = MessageId.New();
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var content = MessageContent.Create("Hello");

        var message = Message.Create(
            messageId,
            conversationId,
            MessageRole.User,
            content,
            MessageMetadata.Empty,
            createdAt);

        Assert.Equal(messageId, message.Id);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal(content, message.Content);
        Assert.Equal(createdAt, message.CreatedAt);
    }

    [Theory]
    [InlineData(default(MessageRole))]
    [InlineData((MessageRole)999)]
    public void Create_WithUndefinedRole_ThrowsDomainException(MessageRole role)
    {
        DomainException exception = Assert.Throws<Iris.Domain.Common.DomainException>(() =>
            Message.Create(
                MessageId.New(),
                ConversationId.New(),
                role,
                MessageContent.Create("Hello"),
                MessageMetadata.Empty,
                new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero)));

        Assert.Equal("message.invalid_role", exception.Code);
    }
}
