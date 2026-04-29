using System;
using Iris.Application.Chat.Contracts;
using Iris.Domain.Conversations;

namespace Iris.Desktop.Models;

public sealed record ChatMessageViewModelItem(
    string Id,
    MessageRole Role,
    string Author,
    string Content,
    DateTimeOffset CreatedAt)
{
    public bool IsUser => Role == MessageRole.User;

    public bool IsAssistant => Role == MessageRole.Assistant;

    public static ChatMessageViewModelItem FromDto(ChatMessageDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ChatMessageViewModelItem(
            dto.Id.ToString(),
            dto.Role,
            GetAuthor(dto.Role),
            dto.Content,
            dto.CreatedAt);
    }

    private static string GetAuthor(MessageRole role)
    {
        return role switch
        {
            MessageRole.User => "You",
            MessageRole.Assistant => "Iris",
            MessageRole.System => "System",
            _ => "Unknown"
        };
    }
}
