using Iris.ModelGateway.Ollama;

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

        var result = OllamaResponseMapper.Map(response);

        Assert.True(result.IsSuccess);
        Assert.Equal("hello back", result.Value.Content);
    }

    [Fact]
    public void Map_Fails_WhenResponseIsNull()
    {
        var result = OllamaResponseMapper.Map(null);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_invalid_response", result.Error.Code);
    }

    [Fact]
    public void Map_Fails_WhenMessageIsNull()
    {
        var response = new OllamaChatResponse("model", DateTimeOffset.UtcNow, null, Done: true);

        var result = OllamaResponseMapper.Map(response);

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

        var result = OllamaResponseMapper.Map(response);

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

        var result = OllamaResponseMapper.Map(response);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_empty_response", result.Error.Code);
    }
}
