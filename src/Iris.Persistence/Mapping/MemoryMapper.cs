using Iris.Domain.Memories;
using Iris.Persistence.Entities;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Persistence.Mapping;

public static class MemoryMapper
{
    public static MemoryEntity ToEntity(DomainMemory memory)
    {
        return new MemoryEntity
        {
            Id = memory.Id.Value,
            Content = memory.Content.Value,
            Kind = (int)memory.Kind,
            Importance = (int)memory.Importance,
            Status = (int)memory.Status,
            Source = (int)memory.Source,
            CreatedAt = memory.CreatedAt,
            UpdatedAt = memory.UpdatedAt
        };
    }

    public static DomainMemory ToDomain(MemoryEntity entity)
    {
        return DomainMemory.Rehydrate(
            MemoryId.From(entity.Id),
            MemoryContent.Create(entity.Content),
            (MemoryKind)entity.Kind,
            (MemoryImportance)entity.Importance,
            (MemoryStatus)entity.Status,
            (MemorySource)entity.Source,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
