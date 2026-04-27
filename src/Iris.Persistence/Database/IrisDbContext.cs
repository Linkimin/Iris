using Iris.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Database;

public sealed class IrisDbContext : DbContext
{
    public IrisDbContext(DbContextOptions<IrisDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();

    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IrisDbContext).Assembly);
    }
}
