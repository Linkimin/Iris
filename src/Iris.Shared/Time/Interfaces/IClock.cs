namespace Iris.Shared.Time.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
