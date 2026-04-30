using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Shared.Results;

namespace Iris.ModelGateway.Ollama;

internal static class OllamaRequestMapper
{
    private const double _minimumTemperature = 0.0;
    private const double _maximumTemperature = 2.0;

    public static Result<OllamaChatRequest> Map(
        ChatModelRequest? request,
        OllamaModelClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (request is null)
        {
            return Result<OllamaChatRequest>.Failure(Error.Validation(
                "model_gateway.request.required",
                "Chat model request is required."));
        }

        if (request.Messages is null || request.Messages.Count == 0)
        {
            return Result<OllamaChatRequest>.Failure(Error.Validation(
                "model_gateway.request.empty_messages",
                "Chat model request must contain at least one message."));
        }

        ChatModelOptions chatOptions = request.Options ?? new ChatModelOptions();
        var model = ResolveModel(chatOptions.Model, options.ChatModel);
        if (string.IsNullOrWhiteSpace(model))
        {
            return Result<OllamaChatRequest>.Failure(Error.Validation(
                "model_gateway.ollama.model_required",
                "Ollama chat model is required."));
        }

        if (chatOptions.Temperature is { } temperature &&
            (double.IsNaN(temperature) ||
             double.IsInfinity(temperature) ||
             temperature < _minimumTemperature ||
             temperature > _maximumTemperature))
        {
            return Result<OllamaChatRequest>.Failure(Error.Validation(
                "model_gateway.request.temperature_invalid",
                "Chat model temperature must be between 0.0 and 2.0."));
        }

        var messages = new List<OllamaChatMessage>(request.Messages.Count);
        foreach (ChatModelMessage message in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return Result<OllamaChatRequest>.Failure(Error.Validation(
                    "model_gateway.request.empty_message_content",
                    "Chat model messages must not contain empty content."));
            }

            var role = MapRole(message.Role);
            if (role is null)
            {
                return Result<OllamaChatRequest>.Failure(Error.Validation(
                    "model_gateway.request.role_invalid",
                    "Chat model message role is not supported."));
            }

            messages.Add(new OllamaChatMessage(role, message.Content));
        }

        OllamaChatOptions? requestOptions = chatOptions.Temperature is null
            ? null
            : new OllamaChatOptions(chatOptions.Temperature);

        var ollamaRequest = new OllamaChatRequest(
            model,
            messages,
            Stream: false,
            requestOptions);

        return Result<OllamaChatRequest>.Success(ollamaRequest);
    }

    private static string ResolveModel(string? requestModel, string configuredModel)
    {
        return string.IsNullOrWhiteSpace(requestModel)
            ? configuredModel.Trim()
            : requestModel.Trim();
    }

    private static string? MapRole(ChatModelRole role)
    {
        return role switch
        {
            ChatModelRole.System => "system",
            ChatModelRole.User => "user",
            ChatModelRole.Assistant => "assistant",
            _ => null
        };
    }
}
