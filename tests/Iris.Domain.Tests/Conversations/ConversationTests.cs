using Iris.Domain.Conversations;

namespace Iris.Domain.Tests.Conversations;

public sealed class ConversationTests
{
    [Fact]
    public void Create_ReturnsActiveDefaultConversation()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

        var conversation = Conversation.Create(
            ConversationId.New(),
            title: null,
            ConversationMode.Default,
            createdAt);

        Assert.Equal(ConversationStatus.Active, conversation.Status);
        Assert.Equal(ConversationMode.Default, conversation.Mode);
        Assert.Equal(createdAt, conversation.CreatedAt);
        Assert.Equal(createdAt, conversation.UpdatedAt);
    }

    [Theory]
    [InlineData(default(ConversationMode))]
    [InlineData((ConversationMode)999)]
    public void Create_WithUndefinedMode_ThrowsDomainException(ConversationMode mode)
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

        var exception = Assert.Throws<Iris.Domain.Common.DomainException>(() =>
            Conversation.Create(ConversationId.New(), null, mode, createdAt));

        Assert.Equal("conversation.invalid_mode", exception.Code);
    }

    [Fact]
    public void UpdateTitle_ChangesTitleAndUpdatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(5);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        conversation.UpdateTitle(ConversationTitle.Create("New title"), updatedAt);

        Assert.Equal("New title", conversation.Title!.Value);
        Assert.Equal(updatedAt, conversation.UpdatedAt);
    }

    [Fact]
    public void Touch_UpdatesUpdatedAt()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var touchedAt = createdAt.AddMinutes(1);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        conversation.Touch(touchedAt);

        Assert.Equal(touchedAt, conversation.UpdatedAt);
    }

    [Fact]
    public void Touch_WithEarlierTimestamp_ThrowsDomainException()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        var exception = Assert.Throws<Iris.Domain.Common.DomainException>(() =>
            conversation.Touch(createdAt.AddTicks(-1)));

        Assert.Equal("conversation.invalid_updated_at", exception.Code);
    }

    [Fact]
    public void UpdateTitle_WithEarlierTimestamp_ThrowsDomainException()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        var exception = Assert.Throws<Iris.Domain.Common.DomainException>(() =>
            conversation.UpdateTitle(ConversationTitle.Create("New title"), createdAt.AddTicks(-1)));

        Assert.Equal("conversation.invalid_updated_at", exception.Code);
    }

    [Fact]
    public void Archive_WithEarlierTimestamp_ThrowsDomainException()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        var exception = Assert.Throws<Iris.Domain.Common.DomainException>(() =>
            conversation.Archive(createdAt.AddTicks(-1)));

        Assert.Equal("conversation.invalid_updated_at", exception.Code);
    }

    [Fact]
    public void Close_WithEarlierTimestamp_ThrowsDomainException()
    {
        var createdAt = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        var exception = Assert.Throws<Iris.Domain.Common.DomainException>(() =>
            conversation.Close(createdAt.AddTicks(-1)));

        Assert.Equal("conversation.invalid_updated_at", exception.Code);
    }
}
