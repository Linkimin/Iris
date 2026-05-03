using Iris.Domain.Memories;

namespace Iris.Desktop.Models;

public sealed record MemoryViewModelItem(
    MemoryId Id,
    string Content,
    string KindLabel,
    string ImportanceLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
