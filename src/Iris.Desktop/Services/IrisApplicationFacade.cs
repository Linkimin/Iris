using System;
using System.Threading;
using System.Threading.Tasks;
using Iris.Application.Chat.SendMessage;
using Iris.Domain.Conversations;
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
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sendMessageHandler = scope.ServiceProvider.GetRequiredService<SendMessageHandler>();

        return await sendMessageHandler.HandleAsync(
            new SendMessageCommand(conversationId, message),
            cancellationToken);
    }
}
