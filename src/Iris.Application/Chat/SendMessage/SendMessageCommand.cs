using Iris.Domain.Conversations;

namespace Iris.Application.Chat.SendMessage;

public sealed record SendMessageCommand(
    ConversationId? ConversationId,
    string Message);
