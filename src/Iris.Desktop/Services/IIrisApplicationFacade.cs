using System.Threading;
using System.Threading.Tasks;

using Iris.Application.Chat.SendMessage;
using Iris.Application.Memory.Commands;
using Iris.Application.Memory.Contracts;
using Iris.Application.Memory.Queries;
using Iris.Domain.Conversations;
using Iris.Domain.Memories;
using Iris.Shared.Results;

namespace Iris.Desktop.Services;

public interface IIrisApplicationFacade
{
    Task<Result<SendMessageResult>> SendMessageAsync(
        ConversationId? conversationId,
        string message,
        CancellationToken cancellationToken);

    Task<Result<RememberMemoryResult>> RememberAsync(
        string content,
        MemoryKind? kind,
        MemoryImportance? importance,
        CancellationToken cancellationToken);

    Task<Result> ForgetAsync(
        MemoryId id,
        CancellationToken cancellationToken);

    Task<Result<UpdateMemoryResult>> UpdateAsync(
        MemoryId id,
        string newContent,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<MemoryDto>>> ListActiveMemoriesAsync(
        int? limit,
        CancellationToken cancellationToken);
}
