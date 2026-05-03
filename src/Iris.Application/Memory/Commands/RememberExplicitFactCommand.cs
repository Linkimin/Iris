using Iris.Domain.Memories;

namespace Iris.Application.Memory.Commands;

public sealed record RememberExplicitFactCommand(
    string Content,
    MemoryKind? Kind = null,
    MemoryImportance? Importance = null);
