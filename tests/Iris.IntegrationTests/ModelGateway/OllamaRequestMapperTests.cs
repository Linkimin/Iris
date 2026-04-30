using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.ModelGateway.Ollama;
using Iris.Shared.Results;

namespace Iris.Integration.Tests.ModelGateway;

public sealed class OllamaRequestMapperTests
{
    [Fact]
    public void Map_MapsRolesModelTemperatureAndDisablesStreaming()
    {
        var request = new ChatModelRequest(
            [
                new ChatModelMessage(ChatModelRole.System, "system prompt"),
                new ChatModelMessage(ChatModelRole.User, "hello"),
                new ChatModelMessage(ChatModelRole.Assistant, "hi")
            ],
            new ChatModelOptions(Model: "request-model", Temperature: 0.7));

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions(chatModel: "configured-model"));

        Assert.True(result.IsSuccess);
        Assert.Equal("request-model", result.Value.Model);
        Assert.False(result.Value.Stream);
        Assert.Equal(["system", "user", "assistant"], result.Value.Messages.Select(message => message.Role));
        Assert.Equal(["system prompt", "hello", "hi"], result.Value.Messages.Select(message => message.Content));
        Assert.NotNull(result.Value.Options);
        Assert.Equal(0.7, result.Value.Options.Temperature);
    }

    [Fact]
    public void Map_UsesConfiguredModel_WhenRequestModelIsMissing()
    {
        var request = new ChatModelRequest(
            [new ChatModelMessage(ChatModelRole.User, "hello")],
            new ChatModelOptions());

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions(chatModel: "configured-model"));

        Assert.True(result.IsSuccess);
        Assert.Equal("configured-model", result.Value.Model);
    }

    [Fact]
    public void Map_OmitsOptions_WhenTemperatureIsMissing()
    {
        var request = new ChatModelRequest(
            [new ChatModelMessage(ChatModelRole.User, "hello")],
            new ChatModelOptions(Model: "model"));

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Options);
    }

    [Fact]
    public void Map_Fails_WhenMessagesAreEmpty()
    {
        var request = new ChatModelRequest([], new ChatModelOptions(Model: "model"));

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions());

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.request.empty_messages", result.Error.Code);
    }

    [Fact]
    public void Map_Fails_WhenMessageContentIsEmpty()
    {
        var request = new ChatModelRequest(
            [new ChatModelMessage(ChatModelRole.User, " ")],
            new ChatModelOptions(Model: "model"));

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions());

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.request.empty_message_content", result.Error.Code);
    }

    [Fact]
    public void Map_Fails_WhenNoModelCanBeResolved()
    {
        var request = new ChatModelRequest(
            [new ChatModelMessage(ChatModelRole.User, "hello")],
            new ChatModelOptions());

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions(chatModel: " "));

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.ollama.model_required", result.Error.Code);
    }

    [Fact]
    public void Map_Fails_WhenRoleIsUnsupported()
    {
        var request = new ChatModelRequest(
            [new ChatModelMessage((ChatModelRole)999, "hello")],
            new ChatModelOptions(Model: "model"));

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions());

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.request.role_invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    public void Map_Fails_WhenTemperatureIsInvalid(double temperature)
    {
        var request = new ChatModelRequest(
            [new ChatModelMessage(ChatModelRole.User, "hello")],
            new ChatModelOptions(Model: "model", Temperature: temperature));

        Result<OllamaChatRequest> result = OllamaRequestMapper.Map(request, CreateOptions());

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.request.temperature_invalid", result.Error.Code);
    }

    private static OllamaModelClientOptions CreateOptions(string chatModel = "model")
    {
        return new OllamaModelClientOptions
        {
            BaseUrl = "http://localhost:11434",
            ChatModel = chatModel,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
