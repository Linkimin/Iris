using Microsoft.EntityFrameworkCore;

namespace Iris.Persistence.Database;

public sealed class IrisDatabaseInitializer : IIrisDatabaseInitializer
{
    private readonly IrisDbContext _dbContext;

    public IrisDatabaseInitializer(IrisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
