using Iris.Domain.Memories;

namespace Iris.Application.Memory.Contracts;

public sealed record MemoryDto(
    MemoryId Id,
    string Content,
    MemoryKind Kind,
    MemoryImportance Importance,
    MemoryStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
