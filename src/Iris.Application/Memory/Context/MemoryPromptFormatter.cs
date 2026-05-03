using System.Globalization;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Memory.Context;

public class MemoryPromptFormatter
{
    public string Format(IReadOnlyList<DomainMemory> memories)
    {
        if (memories.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Известные факты:");

        foreach (DomainMemory memory in memories)
        {
            builder.Append("- ");
            builder.AppendLine(memory.Content.Value);
        }

        return builder.ToString().TrimEnd('\n', '\r');
    }
}
