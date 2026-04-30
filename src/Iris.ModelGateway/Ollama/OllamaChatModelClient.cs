using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Abstractions.Models.Interfaces;
using Iris.ModelGateway.Http;
using Iris.Shared.Results;

namespace Iris.ModelGateway.Ollama;

internal sealed class OllamaChatModelClient : IChatModelClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly OllamaModelClientOptions _options;

    public OllamaChatModelClient(
        HttpClient httpClient,
        OllamaModelClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<Result<ChatModelResponse>> SendAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken)
    {
        Result optionsValidation = _options.Validate();
        if (optionsValidation.IsFailure)
        {
            return Result<ChatModelResponse>.Failure(optionsValidation.Error);
        }

        Result<OllamaChatRequest> mappedRequest = OllamaRequestMapper.Map(request, _options);
        if (mappedRequest.IsFailure)
        {
            return Result<ChatModelResponse>.Failure(mappedRequest.Error);
        }

        try
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                "/api/chat",
                mappedRequest.Value,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<ChatModelResponse>.Failure(
                    ModelGatewayHttpErrorHandler.FromStatusCode(response.StatusCode));
            }

            OllamaChatResponse? ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                JsonOptions,
                cancellationToken);

            return OllamaResponseMapper.Map(ollamaResponse);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result<ChatModelResponse>.Failure(Error.Failure(
                "model_gateway.provider_timeout",
                "The local model provider did not respond in time."));
        }
        catch (HttpRequestException)
        {
            return Result<ChatModelResponse>.Failure(Error.Failure(
                "model_gateway.provider_unavailable",
                "The local model provider is unavailable."));
        }
        catch (JsonException)
        {
            return Result<ChatModelResponse>.Failure(Error.Failure(
                "model_gateway.provider_invalid_response",
                "The local model provider returned an invalid response."));
        }
    }
}
