using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Abstractions.Models.Interfaces;
using Iris.Application.Abstractions.Persistence;
using Iris.Application.Chat.Prompting;
using Iris.Application.Chat.SendMessage;
using Iris.Domain.Conversations;
using Iris.Shared.Results;
using Iris.Shared.Time.Interfaces;

namespace Iris.Application.Tests.Chat.SendMessage;

public sealed class SendMessageHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithNewConversation_SavesUserAndAssistantMessagesAndCommits()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, " Hello "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(" Hello ", result.Value.UserMessage.Content);
        Assert.Equal("Assistant reply", result.Value.AssistantMessage.Content);
        Assert.Single(conversations.Added);
        Assert.Equal(2, messages.Added.Count);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(1, model.SendCalls);
    }

    [Fact]
    public async Task HandleAsync_WithExistingConversation_LoadsConversation()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, clock.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Stored[conversation.Id] = conversation;
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(conversation.Id, "Hello"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(conversation.Id, result.Value.ConversationId);
        Assert.Empty(conversations.Added);
        Assert.Equal(1, conversations.GetCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenExistingConversationLoadThrows_ReturnsControlledError()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository
        {
            GetByIdException = new InvalidOperationException("Database connection failed.")
        };
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(ConversationId.New(), "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("chat.conversation_load_failed", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(0, model.SendCalls);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownConversation_ReturnsControlledError()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(ConversationId.New(), "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("chat.conversation_not_found", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(0, model.SendCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenHistoryLoadThrows_ReturnsControlledError()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository
        {
            ListRecentException = new InvalidOperationException("History query failed.")
        };
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("chat.history_load_failed", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Empty(conversations.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(0, model.SendCalls);
    }

    [Fact]
    public async Task HandleAsync_WithNewestFirstHistory_SendsChronologicalHistoryToModel()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, clock.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Stored[conversation.Id] = conversation;
        var messages = new FakeMessageRepository();
        messages.History.Add(Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.Assistant,
            MessageContent.Create("Newest assistant"),
            MessageMetadata.Empty,
            clock.UtcNow.AddMinutes(2)));
        messages.History.Add(Message.Create(
            MessageId.New(),
            conversation.Id,
            MessageRole.User,
            MessageContent.Create("Oldest user"),
            MessageMetadata.Empty,
            clock.UtcNow.AddMinutes(1)));
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(conversation.Id, "Current user"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(model.LastRequest);
        Assert.Collection(
            model.LastRequest.Messages,
            message => Assert.Equal(ChatModelRole.System, message.Role),
            message => Assert.Equal("Oldest user", message.Content),
            message => Assert.Equal("Newest assistant", message.Content),
            message => Assert.Equal("Current user", message.Content));
    }

    [Fact]
    public async Task HandleAsync_WhenModelFailsForNewConversation_ReturnsControlledErrorAndDoesNotSaveMessages()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Failure(Error.Failure(
            "model.unavailable",
            "Local model is unavailable.")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model.unavailable", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Empty(conversations.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenModelReturnsBlankContent_ReturnsControlledErrorAndDoesNotSaveMessages()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse(" ")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model.empty_response", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Empty(conversations.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenMessageAddThrows_ReturnsControlledError()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository
        {
            AddException = new InvalidOperationException("Insert failed.")
        };
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("chat.message_save_failed", result.Error.Code);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenCommitThrows_ReturnsControlledError()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversations = new FakeConversationRepository();
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork
        {
            CommitException = new InvalidOperationException("Commit failed.")
        };
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Success(new ChatModelResponse("Assistant reply")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(null, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("chat.commit_failed", result.Error.Code);
        Assert.Equal(1, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenModelFailsForExistingConversation_ReturnsControlledErrorAndDoesNotSaveMessages()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero));
        var conversation = Conversation.Create(ConversationId.New(), null, ConversationMode.Default, clock.UtcNow);
        var conversations = new FakeConversationRepository();
        conversations.Stored[conversation.Id] = conversation;
        var messages = new FakeMessageRepository();
        var unitOfWork = new FakeUnitOfWork();
        var model = new FakeChatModelClient(Result<ChatModelResponse>.Failure(Error.Failure(
            "model.unavailable",
            "Local model is unavailable.")));
        var handler = CreateHandler(conversations, messages, unitOfWork, model, clock);

        var result = await handler.HandleAsync(new SendMessageCommand(conversation.Id, "Hello"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model.unavailable", result.Error.Code);
        Assert.Empty(messages.Added);
        Assert.Empty(conversations.Added);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    private static SendMessageHandler CreateHandler(
        FakeConversationRepository conversations,
        FakeMessageRepository messages,
        FakeUnitOfWork unitOfWork,
        FakeChatModelClient model,
        FakeClock clock)
    {
        return new SendMessageHandler(
            conversations,
            messages,
            unitOfWork,
            model,
            new PromptBuilder(),
            new SendMessageValidator(new SendMessageOptions(MaxMessageLength: 10_000)),
            clock);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        public Dictionary<ConversationId, Conversation> Stored { get; } = new();

        public List<Conversation> Added { get; } = new();

        public Exception? AddException { get; set; }

        public Exception? GetByIdException { get; set; }

        public int GetCalls { get; private set; }

        public Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken)
        {
            if (GetByIdException is not null)
            {
                throw GetByIdException;
            }

            GetCalls++;
            Stored.TryGetValue(id, out var conversation);
            return Task.FromResult(conversation);
        }

        public Task AddAsync(Conversation conversation, CancellationToken cancellationToken)
        {
            if (AddException is not null)
            {
                throw AddException;
            }

            Added.Add(conversation);
            Stored[conversation.Id] = conversation;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMessageRepository : IMessageRepository
    {
        public List<Message> Added { get; } = new();

        public List<Message> History { get; } = new();

        public Exception? AddException { get; set; }

        public Exception? ListRecentException { get; set; }

        public Task<IReadOnlyList<Message>> ListRecentAsync(
            ConversationId conversationId,
            int limit,
            CancellationToken cancellationToken)
        {
            if (ListRecentException is not null)
            {
                throw ListRecentException;
            }

            IReadOnlyList<Message> messages = History
                .Where(message => message.ConversationId == conversationId)
                .Take(limit)
                .ToList();

            return Task.FromResult(messages);
        }

        public Task AddAsync(Message message, CancellationToken cancellationToken)
        {
            if (AddException is not null)
            {
                throw AddException;
            }

            Added.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCalls { get; private set; }

        public Exception? CommitException { get; set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            CommitCalls++;

            if (CommitException is not null)
            {
                throw CommitException;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeChatModelClient : IChatModelClient
    {
        private readonly Result<ChatModelResponse> _response;

        public FakeChatModelClient(Result<ChatModelResponse> response)
        {
            _response = response;
        }

        public int SendCalls { get; private set; }

        public ChatModelRequest? LastRequest { get; private set; }

        public Task<Result<ChatModelResponse>> SendAsync(
            ChatModelRequest request,
            CancellationToken cancellationToken)
        {
            SendCalls++;
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }
}
