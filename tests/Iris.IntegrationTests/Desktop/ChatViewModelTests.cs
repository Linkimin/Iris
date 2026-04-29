using Iris.Application.Chat.Contracts;
using Iris.Application.Chat.SendMessage;
using Iris.Desktop.Services;
using Iris.Desktop.ViewModels;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.IntegrationTests.Desktop;

public sealed class ChatViewModelTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void SendMessageCommand_IsDisabled_WhenInputIsEmptyOrWhitespace(string input)
    {
        var viewModel = new ChatViewModel(new FakeIrisApplicationFacade())
        {
            InputText = input
        };

        Assert.False(viewModel.SendMessageCommand.CanExecute(null));
        Assert.True(viewModel.CanEditInput);
    }

    [Fact]
    public async Task SendMessageCommand_AppendsMessagesClearsInputAndReusesConversationId_WhenFacadeSucceeds()
    {
        var firstConversationId = ConversationId.New();
        var fakeFacade = new FakeIrisApplicationFacade();
        fakeFacade.EnqueueSuccess(firstConversationId, "hello", "hi there");
        fakeFacade.EnqueueSuccess(firstConversationId, "again", "still here");
        var viewModel = new ChatViewModel(fakeFacade);

        viewModel.InputText = "hello";
        await viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Messages.Count);
        Assert.True(viewModel.Messages[0].IsUser);
        Assert.True(viewModel.Messages[1].IsAssistant);
        Assert.Equal("hello", viewModel.Messages[0].Content);
        Assert.Equal("hi there", viewModel.Messages[1].Content);
        Assert.Equal(string.Empty, viewModel.InputText);
        Assert.False(viewModel.HasError);
        Assert.Null(fakeFacade.Calls[0].ConversationId);

        viewModel.InputText = "again";
        await viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal(4, viewModel.Messages.Count);
        Assert.Equal(firstConversationId, fakeFacade.Calls[1].ConversationId);
        Assert.Equal("again", fakeFacade.Calls[1].Message);
    }

    [Fact]
    public async Task SendMessageCommand_ShowsReadableErrorAndPreservesInputAndMessages_WhenFacadeFails()
    {
        var fakeFacade = new FakeIrisApplicationFacade();
        fakeFacade.EnqueueSuccess(ConversationId.New(), "hello", "hi there");
        fakeFacade.EnqueueFailure(Error.Failure("model_gateway.provider_unavailable", "raw provider details"));
        var viewModel = new ChatViewModel(fakeFacade);

        viewModel.InputText = "hello";
        await viewModel.SendMessageCommand.ExecuteAsync(null);
        viewModel.InputText = "please answer";

        await viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Messages.Count);
        Assert.Equal("please answer", viewModel.InputText);
        Assert.True(viewModel.HasError);
        Assert.Equal("I could not reach Ollama. Check that Ollama is running.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SendMessageCommand_BlocksDuplicateSend_WhileSending()
    {
        var pendingResult = new TaskCompletionSource<Result<SendMessageResult>>();
        var fakeFacade = new FakeIrisApplicationFacade
        {
            PendingResult = pendingResult
        };
        var viewModel = new ChatViewModel(fakeFacade)
        {
            InputText = "hello"
        };

        var firstSend = viewModel.SendMessageCommand.ExecuteAsync(null);
        await fakeFacade.WaitForCallsAsync(1);

        Assert.True(viewModel.IsSending);
        Assert.False(viewModel.CanEditInput);
        Assert.False(viewModel.SendMessageCommand.CanExecute(null));

        await viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Single(fakeFacade.Calls);

        pendingResult.SetResult(FakeIrisApplicationFacade.CreateSuccessfulResult(null, "hello", "hi there"));
        await firstSend;
    }

    [Fact]
    public async Task SendMessageCommand_ClearsConversationId_WhenConversationNotFound()
    {
        var firstConversationId = ConversationId.New();
        var replacementConversationId = ConversationId.New();
        var fakeFacade = new FakeIrisApplicationFacade();
        fakeFacade.EnqueueSuccess(firstConversationId, "hello", "hi there");
        fakeFacade.EnqueueFailure(Error.Failure("chat.conversation_not_found", "missing"));
        fakeFacade.EnqueueSuccess(replacementConversationId, "start fresh", "fresh answer");
        var viewModel = new ChatViewModel(fakeFacade);

        viewModel.InputText = "hello";
        await viewModel.SendMessageCommand.ExecuteAsync(null);

        viewModel.InputText = "where did it go?";
        await viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Equal(firstConversationId, fakeFacade.Calls[1].ConversationId);
        Assert.True(viewModel.HasError);
        Assert.Equal("This conversation could not be found.", viewModel.ErrorMessage);

        viewModel.InputText = "start fresh";
        await viewModel.SendMessageCommand.ExecuteAsync(null);

        Assert.Null(fakeFacade.Calls[2].ConversationId);
        Assert.Equal(4, viewModel.Messages.Count);
    }

    private sealed class FakeIrisApplicationFacade : IIrisApplicationFacade
    {
        private readonly Queue<Result<SendMessageResult>> _results = new();

        public TaskCompletionSource<Result<SendMessageResult>>? PendingResult { get; set; }

        public List<SendCall> Calls { get; } = new();

        public void EnqueueSuccess(
            ConversationId conversationId,
            string userMessage,
            string assistantMessage)
        {
            _results.Enqueue(CreateSuccessfulResult(conversationId, userMessage, assistantMessage));
        }

        public void EnqueueFailure(Error error)
        {
            _results.Enqueue(Result<SendMessageResult>.Failure(error));
        }

        public Task<Result<SendMessageResult>> SendMessageAsync(
            ConversationId? conversationId,
            string message,
            CancellationToken cancellationToken)
        {
            Calls.Add(new SendCall(conversationId, message));

            return PendingResult is not null
                ? PendingResult.Task
                : Task.FromResult(_results.Dequeue());
        }

        public async Task WaitForCallsAsync(int expectedCalls)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            while (Calls.Count < expectedCalls)
            {
                timeout.Token.ThrowIfCancellationRequested();
                await Task.Delay(10, timeout.Token);
            }
        }

        public static Result<SendMessageResult> CreateSuccessfulResult(
            ConversationId? conversationId,
            string userMessage,
            string assistantMessage)
        {
            var resolvedConversationId = conversationId ?? ConversationId.New();
            var now = DateTimeOffset.UtcNow;
            var userDto = new ChatMessageDto(
                MessageId.New(),
                resolvedConversationId,
                MessageRole.User,
                userMessage,
                now);
            var assistantDto = new ChatMessageDto(
                MessageId.New(),
                resolvedConversationId,
                MessageRole.Assistant,
                assistantMessage,
                now.AddMilliseconds(1));

            return Result<SendMessageResult>.Success(
                new SendMessageResult(resolvedConversationId, userDto, assistantDto));
        }
    }

    private sealed record SendCall(ConversationId? ConversationId, string Message);
}
