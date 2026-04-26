namespace Iris.Application.Abstractions.Models.Contracts.Chat;

public sealed record ChatModelMessage(ChatModelRole Role, string Content);
