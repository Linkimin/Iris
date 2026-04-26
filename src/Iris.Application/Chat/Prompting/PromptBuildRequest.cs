using Iris.Domain.Conversations;

namespace Iris.Application.Chat.Prompting;

public sealed record PromptBuildRequest(
    IReadOnlyList<Message> RecentMessages,
    MessageContent CurrentUserMessage);
