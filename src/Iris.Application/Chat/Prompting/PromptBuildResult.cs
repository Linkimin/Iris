using Iris.Application.Abstractions.Models.Contracts.Chat;

namespace Iris.Application.Chat.Prompting;

public sealed record PromptBuildResult(ChatModelRequest ModelRequest);
