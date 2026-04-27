using Iris.Domain.Conversations;
using Iris.Persistence.Repositories;
using Iris.Persistence.UnitOfWork;

namespace Iris.Integration.Tests.Persistence;

public sealed class MessageRepositoryTests
{
    [Fact]
    public async Task AddAndListRecentAsync_PersistsMessagesInChronologicalOrder()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(
            ConversationId.New(),
            null,
            ConversationMode.Default,
            createdAt);
        var newest = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.Assistant,
            MessageContent.Create("Newest"),
            MessageMetadata.Empty,
            createdAt.AddMinutes(2));
        var oldest = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.User,
            MessageContent.Create("Oldest"),
            MessageMetadata.Empty,
            createdAt.AddMinutes(1));

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var conversationRepository = new ConversationRepository(writeContext);
            var messageRepository = new MessageRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await conversationRepository.AddAsync(conversation, CancellationToken.None);
            await messageRepository.AddAsync(newest, CancellationToken.None);
            await messageRepository.AddAsync(oldest, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new MessageRepository(readContext);

        var messages = await readRepository.ListRecentAsync(
            conversation.Id,
            limit: 10,
            CancellationToken.None);

        Assert.Collection(
            messages,
            message =>
            {
                Assert.Equal(MessageRole.User, message.Role);
                Assert.Equal("Oldest", message.Content.Value);
                Assert.Equal(MessageMetadata.Empty, message.Metadata);
            },
            message =>
            {
                Assert.Equal(MessageRole.Assistant, message.Role);
                Assert.Equal("Newest", message.Content.Value);
                Assert.Equal(MessageMetadata.Empty, message.Metadata);
            });
    }

    [Fact]
    public async Task AddAndListRecentAsync_PreservesUtcTicksForNonZeroOffsetTimestamps()
    {
        await using var factory = new PersistenceTestContextFactory();
        var conversationCreatedAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        var messageCreatedAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.FromHours(5)).AddTicks(9_876);
        var conversation = Conversation.Create(
            ConversationId.New(),
            null,
            ConversationMode.Default,
            conversationCreatedAt);
        var message = Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.User,
            MessageContent.Create("Offset timestamp"),
            MessageMetadata.Empty,
            messageCreatedAt);

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var conversationRepository = new ConversationRepository(writeContext);
            var messageRepository = new MessageRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await conversationRepository.AddAsync(conversation, CancellationToken.None);
            await messageRepository.AddAsync(message, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new MessageRepository(readContext);

        var messages = await readRepository.ListRecentAsync(conversation.Id, limit: 10, CancellationToken.None);

        var persisted = Assert.Single(messages);
        Assert.Equal(TimeSpan.Zero, persisted.CreatedAt.Offset);
        Assert.Equal(messageCreatedAt.UtcTicks, persisted.CreatedAt.UtcTicks);
    }

    [Fact]
    public async Task ListRecentAsync_WithIdenticalCreatedAt_ReturnsInsertionOrder()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero).AddTicks(321);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var conversationRepository = new ConversationRepository(writeContext);
            var messageRepository = new MessageRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await conversationRepository.AddAsync(conversation, CancellationToken.None);

            foreach (var content in new[] { "First", "Second", "Third" })
            {
                var message = Message.Create(
                    MessageId.New(),
                    conversation.Id,
                    MessageRole.User,
                    MessageContent.Create(content),
                    MessageMetadata.Empty,
                    createdAt);

                await messageRepository.AddAsync(message, CancellationToken.None);
            }

            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new MessageRepository(readContext);

        var messages = await readRepository.ListRecentAsync(conversation.Id, limit: 10, CancellationToken.None);

        Assert.Collection(
            messages,
            message => Assert.Equal("First", message.Content.Value),
            message => Assert.Equal("Second", message.Content.Value),
            message => Assert.Equal("Third", message.Content.Value));
    }

    [Fact]
    public async Task ListRecentAsync_RespectsLimitAndReturnsOldestToNewestWithinWindow()
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var conversationRepository = new ConversationRepository(writeContext);
            var messageRepository = new MessageRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);

            await conversationRepository.AddAsync(conversation, CancellationToken.None);

            for (var index = 0; index < 3; index++)
            {
                var message = Message.Create(
                    MessageId.New(),
                    conversation.Id,
                    MessageRole.User,
                    MessageContent.Create($"Message {index}"),
                    MessageMetadata.Empty,
                    createdAt.AddMinutes(index));

                await messageRepository.AddAsync(message, CancellationToken.None);
            }

            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new MessageRepository(readContext);

        var messages = await readRepository.ListRecentAsync(
            conversation.Id,
            limit: 2,
            CancellationToken.None);

        Assert.Collection(
            messages,
            message => Assert.Equal("Message 1", message.Content.Value),
            message => Assert.Equal("Message 2", message.Content.Value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListRecentAsync_WithNonPositiveLimit_ReturnsEmpty(int limit)
    {
        await using var factory = new PersistenceTestContextFactory();
        var createdAt = new DateTimeOffset(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, createdAt);

        await using (var writeContext = await factory.CreateInitializedContextAsync())
        {
            var conversationRepository = new ConversationRepository(writeContext);
            var messageRepository = new MessageRepository(writeContext);
            var unitOfWork = new EfUnitOfWork(writeContext);
            var message = Message.Create(
                MessageId.New(),
                conversation.Id,
                MessageRole.User,
                MessageContent.Create("Stored"),
                MessageMetadata.Empty,
                createdAt);

            await conversationRepository.AddAsync(conversation, CancellationToken.None);
            await messageRepository.AddAsync(message, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var readContext = factory.CreateContext();
        var readRepository = new MessageRepository(readContext);

        var messages = await readRepository.ListRecentAsync(conversation.Id, limit, CancellationToken.None);

        Assert.Empty(messages);
    }
}
