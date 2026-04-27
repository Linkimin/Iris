using Iris.Application.Abstractions.Persistence;
using Iris.Persistence.Database;

namespace Iris.Persistence.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly IrisDbContext _dbContext;

    public EfUnitOfWork(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
