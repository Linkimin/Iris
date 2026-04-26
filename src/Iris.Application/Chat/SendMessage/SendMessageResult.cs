using Iris.Application.Chat.Contracts;
using Iris.Domain.Conversations;

namespace Iris.Application.Chat.SendMessage;

public sealed record SendMessageResult(
    ConversationId ConversationId,
    ChatMessageDto UserMessage,
    ChatMessageDto AssistantMessage);
