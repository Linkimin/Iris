using Iris.Domain.Conversations;

namespace Iris.Application.Abstractions.Persistence;

public interface IMessageRepository
{
    /// <summary>
    /// Returns recent messages for the conversation in chronological order, oldest to newest.
    /// </summary>
    Task<IReadOnlyList<Message>> ListRecentAsync(
        ConversationId conversationId,
        int limit,
        CancellationToken cancellationToken);

    Task AddAsync(Message message, CancellationToken cancellationToken);
}
