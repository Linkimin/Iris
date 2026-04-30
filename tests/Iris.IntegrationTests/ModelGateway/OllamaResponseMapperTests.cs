using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.ModelGateway.Ollama;
using Iris.Shared.Results;

namespace Iris.Integration.Tests.ModelGateway;

public sealed class OllamaResponseMapperTests
{
    [Fact]
    public void Map_ReturnsChatModelResponse_WhenAssistantContentIsPresent()
    {
        var response = new OllamaChatResponse(
            "model",
            DateTimeOffset.UtcNow,
            new OllamaChatMessage("assistant", "hello back"),
            Done: true);

        Result<ChatModelResponse> result = OllamaResponseMapper.Map(response);

        Assert.True(result.IsSuccess);
        Assert.Equal("hello back", result.Value.Content);
    }

    [Fact]
    public void Map_Fails_WhenResponseIsNull()
    {
        Result<ChatModelResponse> result = OllamaResponseMapper.Map(null);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_invalid_response", result.Error.Code);
    }

    [Fact]
    public void Map_Fails_WhenMessageIsNull()
    {
        var response = new OllamaChatResponse("model", DateTimeOffset.UtcNow, null, Done: true);

        Result<ChatModelResponse> result = OllamaResponseMapper.Map(response);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_invalid_response", result.Error.Code);
    }

    [Fact]
    public void Map_Fails_WhenRoleIsNotAssistant()
    {
        var response = new OllamaChatResponse(
            "model",
            DateTimeOffset.UtcNow,
            new OllamaChatMessage("user", "hello"),
            Done: true);

        Result<ChatModelResponse> result = OllamaResponseMapper.Map(response);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_invalid_role", result.Error.Code);
    }

    [Fact]
    public void Map_Fails_WhenContentIsEmpty()
    {
        var response = new OllamaChatResponse(
            "model",
            DateTimeOffset.UtcNow,
            new OllamaChatMessage("assistant", " "),
            Done: true);

        Result<ChatModelResponse> result = OllamaResponseMapper.Map(response);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_empty_response", result.Error.Code);
    }
}
