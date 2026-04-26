namespace Iris.Application.Abstractions.Models.Contracts.Chat;

public sealed record ChatModelRequest(
    IReadOnlyList<ChatModelMessage> Messages,
    ChatModelOptions Options);
