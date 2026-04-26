using Iris.Domain.Conversations;

namespace Iris.Application.Abstractions.Persistence;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);
}
