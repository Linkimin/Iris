using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Conversations;
using Iris.Persistence.Database;
using Iris.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly IrisDbContext _dbContext;

    public ConversationRepository(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation?> GetByIdAsync(
        ConversationId id,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                conversation => conversation.Id == id.Value,
                cancellationToken);

        return entity is null ? null : ConversationMapper.ToDomain(entity);
    }

    public async Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        var entity = ConversationMapper.ToEntity(conversation);
        await _dbContext.Conversations.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Conversations
            .FirstOrDefaultAsync(
                storedConversation => storedConversation.Id == conversation.Id.Value,
                cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Conversation could not be found for update.");
        }

        entity.Title = conversation.Title?.Value;
        entity.Status = (int)conversation.Status;
        entity.Mode = (int)conversation.Mode;
        entity.CreatedAt = conversation.CreatedAt;
        entity.UpdatedAt = conversation.UpdatedAt;
    }
}
