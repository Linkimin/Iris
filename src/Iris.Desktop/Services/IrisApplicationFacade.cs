using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Chat.SendMessage;
using Iris.Application.Memory.Commands;
using Iris.Application.Memory.Contracts;
using Iris.Application.Memory.Queries;
using Iris.Domain.Conversations;
using Iris.Domain.Memories;
using Iris.Shared.Results;

using Microsoft.Extensions.DependencyInjection;

namespace Iris.Desktop.Services;

public sealed class IrisApplicationFacade : IIrisApplicationFacade
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IrisApplicationFacade(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task<Result<SendMessageResult>> SendMessageAsync(
        ConversationId? conversationId,
        string message,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        SendMessageHandler sendMessageHandler = scope.ServiceProvider.GetRequiredService<SendMessageHandler>();

        return await sendMessageHandler.HandleAsync(
            new SendMessageCommand(conversationId, message),
            cancellationToken);
    }

    public async Task<Result<RememberMemoryResult>> RememberAsync(
        string content,
        MemoryKind? kind,
        MemoryImportance? importance,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        RememberExplicitFactHandler handler = scope.ServiceProvider.GetRequiredService<RememberExplicitFactHandler>();

        return await handler.HandleAsync(
            new RememberExplicitFactCommand(content, kind, importance),
            cancellationToken);
    }

    public async Task<Result> ForgetAsync(
        MemoryId id,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        ForgetMemoryHandler handler = scope.ServiceProvider.GetRequiredService<ForgetMemoryHandler>();

        return await handler.HandleAsync(
            new ForgetMemoryCommand(id),
            cancellationToken);
    }

    public async Task<Result<UpdateMemoryResult>> UpdateAsync(
        MemoryId id,
        string newContent,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        UpdateMemoryHandler handler = scope.ServiceProvider.GetRequiredService<UpdateMemoryHandler>();

        return await handler.HandleAsync(
            new UpdateMemoryCommand(id, newContent),
            cancellationToken);
    }

    public async Task<Result<IReadOnlyList<MemoryDto>>> ListActiveMemoriesAsync(
        int? limit,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        ListActiveMemoriesHandler handler = scope.ServiceProvider.GetRequiredService<ListActiveMemoriesHandler>();

        return await handler.HandleAsync(
            new ListActiveMemoriesQuery(limit),
            cancellationToken);
    }
}
