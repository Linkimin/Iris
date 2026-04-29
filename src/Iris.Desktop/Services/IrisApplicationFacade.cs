using System.Threading;
using System.Threading.Tasks;
using Iris.Application.Chat.SendMessage;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Desktop.Services;

public sealed class IrisApplicationFacade : IIrisApplicationFacade
{
    private readonly SendMessageHandler _sendMessageHandler;

    public IrisApplicationFacade(SendMessageHandler sendMessageHandler)
    {
        _sendMessageHandler = sendMessageHandler;
    }

    public Task<Result<SendMessageResult>> SendMessageAsync(
        ConversationId? conversationId,
        string message,
        CancellationToken cancellationToken)
    {
        return _sendMessageHandler.HandleAsync(
            new SendMessageCommand(conversationId, message),
            cancellationToken);
    }
}
