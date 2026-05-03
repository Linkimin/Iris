using Iris.Domain.Memories;

namespace Iris.Application.Memory.Commands;

public sealed record ForgetMemoryCommand(MemoryId Id);
