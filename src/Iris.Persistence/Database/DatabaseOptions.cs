namespace Iris.Persistence.Database;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("Database connection string is required.");
        }
    }
}
