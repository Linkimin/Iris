using Iris.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Iris.Integration.Tests.Persistence;

internal sealed class PersistenceTestContextFactory : IAsyncDisposable
{
    private readonly string _databasePath;

    public PersistenceTestContextFactory()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"iris-persistence-{Guid.NewGuid():N}.db");
    }

    public async Task<IrisDbContext> CreateInitializedContextAsync()
    {
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public IrisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IrisDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        return new IrisDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
