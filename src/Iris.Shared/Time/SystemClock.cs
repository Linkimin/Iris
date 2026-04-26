using Iris.Shared.Time.Interfaces;

namespace Iris.Shared.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
