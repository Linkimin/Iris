using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Chat.Contracts;
using Iris.Application.Chat.SendMessage;
using Iris.Desktop.Services;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.IntegrationTests.Testing;

internal sealed class FakeIrisApplicationFacade : IIrisApplicationFacade
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
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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
        ConversationId resolvedConversationId = conversationId ?? ConversationId.New();
        DateTimeOffset now = DateTimeOffset.UtcNow;
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

internal sealed record SendCall(ConversationId? ConversationId, string Message);
