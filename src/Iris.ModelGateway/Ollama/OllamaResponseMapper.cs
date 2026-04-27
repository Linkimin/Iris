using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Shared.Results;

namespace Iris.ModelGateway.Ollama;

internal static class OllamaResponseMapper
{
    public static Result<ChatModelResponse> Map(OllamaChatResponse? response)
    {
        if (response is null || response.Message is null)
        {
            return Result<ChatModelResponse>.Failure(Error.Failure(
                "model_gateway.provider_invalid_response",
                "The local model provider returned an invalid response."));
        }

        if (!string.Equals(response.Message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ChatModelResponse>.Failure(Error.Failure(
                "model_gateway.provider_invalid_role",
                "The local model provider returned an invalid assistant role."));
        }

        if (string.IsNullOrWhiteSpace(response.Message.Content))
        {
            return Result<ChatModelResponse>.Failure(Error.Failure(
                "model_gateway.provider_empty_response",
                "The local model provider returned an empty response."));
        }

        return Result<ChatModelResponse>.Success(new ChatModelResponse(response.Message.Content));
    }
}
