namespace Iris.Persistence.Entities;

public sealed class MemoryEntity
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public int Kind { get; set; }

    public int Importance { get; set; }

    public int Status { get; set; }

    public int Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
