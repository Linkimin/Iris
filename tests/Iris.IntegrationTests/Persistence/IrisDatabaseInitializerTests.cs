using Iris.Persistence;
using Iris.Persistence.Database;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Integration.Tests.Persistence;

public sealed class IrisDatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_CreatesSqliteSchema_ForCleanDatabase()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"iris-initializer-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            await using (ServiceProvider provider = CreateProvider(connectionString))
            await using (AsyncServiceScope scope = provider.CreateAsyncScope())
            {
                IIrisDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IIrisDatabaseInitializer>();
                await initializer.InitializeAsync(CancellationToken.None);

                IrisDbContext dbContext = scope.ServiceProvider.GetRequiredService<IrisDbContext>();

                Assert.True(await dbContext.Database.CanConnectAsync());
                Assert.True(File.Exists(databasePath));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static ServiceProvider CreateProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddIrisPersistence(options => options.ConnectionString = connectionString);

        return services.BuildServiceProvider();
    }
}
