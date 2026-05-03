using Iris.Domain.Common;

namespace Iris.Domain.Memories;

public sealed class Memory
{
    private Memory(
        MemoryId id,
        MemoryContent content,
        MemoryKind kind,
        MemoryImportance importance,
        MemoryStatus status,
        MemorySource source,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
    {
        Id = id;
        Content = content;
        Kind = kind;
        Importance = importance;
        Status = status;
        Source = source;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public MemoryId Id { get; }

    public MemoryContent Content { get; private set; }

    public MemoryKind Kind { get; }

    public MemoryImportance Importance { get; }

    public MemoryStatus Status { get; private set; }

    public MemorySource Source { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Memory Create(
        MemoryId id,
        MemoryContent content,
        MemoryKind kind,
        MemoryImportance importance,
        MemorySource source,
        DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("memory.invalid_kind", "Memory kind is invalid.");
        }

        if (!Enum.IsDefined(importance))
        {
            throw new DomainException("memory.invalid_importance", "Memory importance is invalid.");
        }

        if (!Enum.IsDefined(source))
        {
            throw new DomainException("memory.invalid_source", "Memory source is invalid.");
        }

        return new Memory(
            id,
            content,
            kind,
            importance,
            MemoryStatus.Active,
            source,
            createdAt,
            updatedAt: null);
    }

    /// <summary>
    /// Rehydration-only factory for persistence mappers.
    /// Bypasses domain validation.
    /// </summary>
    public static Memory Rehydrate(
        MemoryId id,
        MemoryContent content,
        MemoryKind kind,
        MemoryImportance importance,
        MemoryStatus status,
        MemorySource source,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
    {
        return new Memory(
            id,
            content,
            kind,
            importance,
            status,
            source,
            createdAt,
            updatedAt);
    }

    /// <summary>
    /// Transitions Active → Forgotten. Idempotent on already-Forgotten.
    /// </summary>
    /// <returns>true if the status changed; false if already forgotten.</returns>
    public bool Forget(DateTimeOffset now)
    {
        if (Status == MemoryStatus.Forgotten)
        {
            return false;
        }

        Status = MemoryStatus.Forgotten;
        UpdatedAt = now;
        return true;
    }

    public void UpdateContent(MemoryContent newContent, DateTimeOffset now)
    {
        if (Status == MemoryStatus.Forgotten)
        {
            throw new DomainException("memory.not_active", "Memory is not active and cannot be updated.");
        }

        Content = newContent;
        UpdatedAt = now;
    }
}
