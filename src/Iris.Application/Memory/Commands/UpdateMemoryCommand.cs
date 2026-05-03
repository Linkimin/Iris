using Iris.Domain.Memories;

namespace Iris.Application.Memory.Commands;

public sealed record UpdateMemoryCommand(MemoryId Id, string NewContent);
