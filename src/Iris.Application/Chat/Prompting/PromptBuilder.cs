using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Memory.Context;
using Iris.Application.Persona.Language;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

using DomainMemory = Iris.Domain.Memories.Memory;

namespace Iris.Application.Chat.Prompting;

public sealed class PromptBuilder
{
    private readonly ILanguagePolicy _languagePolicy;
    private readonly MemoryContextBuilder _memoryContextBuilder;
    private readonly MemoryPromptFormatter _memoryPromptFormatter;

    public PromptBuilder(
        ILanguagePolicy languagePolicy,
        MemoryContextBuilder memoryContextBuilder,
        MemoryPromptFormatter memoryPromptFormatter)
    {
        ArgumentNullException.ThrowIfNull(languagePolicy);
        ArgumentNullException.ThrowIfNull(memoryContextBuilder);
        ArgumentNullException.ThrowIfNull(memoryPromptFormatter);

        _languagePolicy = languagePolicy;
        _memoryContextBuilder = memoryContextBuilder;
        _memoryPromptFormatter = memoryPromptFormatter;
    }

    public Result<PromptBuildResult> Build(PromptBuildRequest request)
    {
        return Build(request, Array.Empty<DomainMemory>());
    }

    public Result<PromptBuildResult> Build(PromptBuildRequest request, IReadOnlyList<DomainMemory> memories)
    {
        var messages = new List<ChatModelMessage>
        {
            new(ChatModelRole.System, _languagePolicy.GetSystemPrompt())
        };

        if (memories.Count > 0)
        {
            var memoryBlock = _memoryPromptFormatter.Format(memories);

            if (memoryBlock.Length > 0)
            {
                messages.Add(new ChatModelMessage(ChatModelRole.System, memoryBlock));
            }
        }

        messages.AddRange(request.RecentMessages.Select(MapHistoryMessage));
        messages.Add(new ChatModelMessage(ChatModelRole.User, request.CurrentUserMessage.Value));

        var modelRequest = new ChatModelRequest(messages, new ChatModelOptions());

        return Result<PromptBuildResult>.Success(new PromptBuildResult(modelRequest));
    }

    private static ChatModelMessage MapHistoryMessage(Message message)
    {
        ChatModelRole role = message.Role switch
        {
            MessageRole.System => ChatModelRole.System,
            MessageRole.User => ChatModelRole.User,
            MessageRole.Assistant => ChatModelRole.Assistant,
            _ => throw new InvalidOperationException($"Unsupported message role: {message.Role}")
        };

        return new ChatModelMessage(role, message.Content.Value);
    }
}
