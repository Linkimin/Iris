using System.Threading;
using System.Threading.Tasks;
using Iris.Application.Chat.SendMessage;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Desktop.Services;

public interface IIrisApplicationFacade
{
    Task<Result<SendMessageResult>> SendMessageAsync(
        ConversationId? conversationId,
        string message,
        CancellationToken cancellationToken);
}
