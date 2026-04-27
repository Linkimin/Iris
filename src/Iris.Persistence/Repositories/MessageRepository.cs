using Iris.Application.Abstractions.Persistence;
using Iris.Domain.Conversations;
using Iris.Persistence.Database;
using Iris.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private readonly IrisDbContext _dbContext;

    public MessageRepository(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Message>> ListRecentAsync(
        ConversationId conversationId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Message>();
        }

        var newestEntities = await _dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId.Value)
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.PersistenceId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return newestEntities
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.PersistenceId)
            .Select(MessageMapper.ToDomain)
            .ToList();
    }

    public async Task AddAsync(
        Message message,
        CancellationToken cancellationToken)
    {
        var entity = MessageMapper.ToEntity(message);
        await _dbContext.Messages.AddAsync(entity, cancellationToken);
    }
}
