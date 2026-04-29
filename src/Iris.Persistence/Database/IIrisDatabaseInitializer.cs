namespace Iris.Persistence.Database;

public interface IIrisDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
