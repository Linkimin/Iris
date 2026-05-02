using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Persona.Language;
using Iris.Domain.Conversations;
using Iris.Shared.Results;

namespace Iris.Application.Chat.Prompting;

public sealed class PromptBuilder
{
    private readonly ILanguagePolicy _languagePolicy;

    public PromptBuilder(ILanguagePolicy languagePolicy)
    {
        ArgumentNullException.ThrowIfNull(languagePolicy);
        _languagePolicy = languagePolicy;
    }

    public Result<PromptBuildResult> Build(PromptBuildRequest request)
    {
        var messages = new List<ChatModelMessage>
        {
            new(ChatModelRole.System, _languagePolicy.GetSystemPrompt())
        };

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
