using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Chat.Contracts;
using Iris.Application.Chat.SendMessage;
using Iris.Application.Memory.Commands;
using Iris.Application.Memory.Contracts;
using Iris.Application.Memory.Queries;
using Iris.Desktop.Services;
using Iris.Domain.Conversations;
using Iris.Domain.Memories;
using Iris.Shared.Results;

namespace Iris.IntegrationTests.Testing;

internal sealed class FakeIrisApplicationFacade : IIrisApplicationFacade
{
    private readonly Queue<Result<SendMessageResult>> _results = new();
    private readonly Queue<Result<IReadOnlyList<MemoryDto>>> _listMemoryResults = new();

    public TaskCompletionSource<Result<SendMessageResult>>? PendingResult { get; set; }

    public List<SendCall> Calls { get; } = new();

    public List<MemoryId> ForgetCalls { get; } = new();

    public void EnqueueListMemoriesSuccess(params MemoryDto[] memories)
    {
        _listMemoryResults.Enqueue(Result<IReadOnlyList<MemoryDto>>.Success(memories));
    }

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

    public Task<Result<RememberMemoryResult>> RememberAsync(
        string content,
        MemoryKind? kind,
        MemoryImportance? importance,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Result<RememberMemoryResult>.Success(
                new RememberMemoryResult(
                    new MemoryDto(
                        MemoryId.New(),
                        content,
                        kind ?? MemoryKind.Note,
                        importance ?? MemoryImportance.Normal,
                        MemoryStatus.Active,
                        DateTimeOffset.UtcNow,
                        null))));
    }

    public Task<Result> ForgetAsync(
        MemoryId id,
        CancellationToken cancellationToken)
    {
        ForgetCalls.Add(id);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<UpdateMemoryResult>> UpdateAsync(
        MemoryId id,
        string newContent,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Result<UpdateMemoryResult>.Success(
                new UpdateMemoryResult(
                    new MemoryDto(
                        id,
                        newContent,
                        MemoryKind.Note,
                        MemoryImportance.Normal,
                        MemoryStatus.Active,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow))));
    }

    public Task<Result<IReadOnlyList<MemoryDto>>> ListActiveMemoriesAsync(
        int? limit,
        CancellationToken cancellationToken)
    {
        if (_listMemoryResults.Count > 0)
        {
            return Task.FromResult(_listMemoryResults.Dequeue());
        }

        return Task.FromResult(Result<IReadOnlyList<MemoryDto>>.Success(Array.Empty<MemoryDto>()));
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
