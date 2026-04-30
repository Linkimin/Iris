using System.Net;
using System.Text.Json;

using Iris.Application.Abstractions.Models.Contracts.Chat;
using Iris.Application.Abstractions.Models.Interfaces;
using Iris.ModelGateway;
using Iris.ModelGateway.Ollama;
using Iris.Shared.Results;

using Microsoft.Extensions.DependencyInjection;

namespace Iris.Integration.Tests.ModelGateway;

public sealed class OllamaChatModelClientTests
{
    [Fact]
    public async Task SendAsync_PostsApiChatJsonWithStreamFalseAndMapsResponse()
    {
        string? requestJson = null;
        var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            requestJson = await request.Content!.ReadAsStringAsync();

            return JsonResponse("""
                {
                  "model": "llama3.1",
                  "created_at": "2026-04-27T00:00:00Z",
                  "message": {
                    "role": "assistant",
                    "content": "Hello from Ollama"
                  },
                  "done": true
                }
                """);
        });

        OllamaChatModelClient client = CreateClient(handler);

        Result<ChatModelResponse> result = await client.SendAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello from Ollama", result.Value.Content);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("http://localhost:11434/api/chat", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("application/json", handler.LastRequest.Content!.Headers.ContentType!.MediaType);

        using var document = JsonDocument.Parse(requestJson!);
        JsonElement root = document.RootElement;
        Assert.Equal("llama3.1", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        Assert.Equal("user", root.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("hello", root.GetProperty("messages")[0].GetProperty("content").GetString());
        Assert.Equal(0.5, root.GetProperty("options").GetProperty("temperature").GetDouble());
    }

    [Fact]
    public async Task SendAsync_ReturnsUnavailable_WhenHttpRequestFails()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new HttpRequestException("connection refused with private details"));
        OllamaChatModelClient client = CreateClient(handler);

        Result<ChatModelResponse> result = await client.SendAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_unavailable", result.Error.Code);
        Assert.DoesNotContain("private details", result.Error.Message);
    }

    [Fact]
    public async Task SendAsync_ReturnsTimeout_WhenProviderTimesOutWithoutCallerCancellation()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            throw new TaskCanceledException("provider timeout"));
        OllamaChatModelClient client = CreateClient(handler);

        Result<ChatModelResponse> result = await client.SendAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_timeout", result.Error.Code);
    }

    [Fact]
    public async Task SendAsync_Rethrows_WhenCallerCancellationIsRequested()
    {
        var handler = new FakeHttpMessageHandler((_, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(JsonResponse("{}"));
        });
        OllamaChatModelClient client = CreateClient(handler);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            client.SendAsync(CreateRequest(), cancellationTokenSource.Token));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "model_gateway.provider_not_found")]
    [InlineData(HttpStatusCode.InternalServerError, "model_gateway.provider_failure")]
    public async Task SendAsync_MapsHttpFailures(HttpStatusCode statusCode, string expectedCode)
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("raw provider body")
            }));
        OllamaChatModelClient client = CreateClient(handler);

        Result<ChatModelResponse> result = await client.SendAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.DoesNotContain("raw provider body", result.Error.Message);
    }

    [Fact]
    public async Task SendAsync_ReturnsInvalidResponse_WhenJsonIsInvalid()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid json")
            }));
        OllamaChatModelClient client = CreateClient(handler);

        Result<ChatModelResponse> result = await client.SendAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_invalid_response", result.Error.Code);
    }

    [Fact]
    public async Task SendAsync_ReturnsEmptyResponse_WhenAssistantContentIsEmpty()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(JsonResponse("""
                {
                  "message": {
                    "role": "assistant",
                    "content": " "
                  },
                  "done": true
                }
                """)));
        OllamaChatModelClient client = CreateClient(handler);

        Result<ChatModelResponse> result = await client.SendAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("model_gateway.provider_empty_response", result.Error.Code);
    }

    [Fact]
    public void AddIrisModelGateway_RegistersChatModelClient()
    {
        var services = new ServiceCollection();

        services.AddIrisModelGateway(options =>
        {
            options.BaseUrl = "http://localhost:11434";
            options.ChatModel = "llama3.1";
            options.Timeout = TimeSpan.FromSeconds(30);
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsAssignableFrom<IChatModelClient>(provider.GetRequiredService<IChatModelClient>());
    }

    [Fact]
    public void AddIrisModelGateway_Throws_WhenOptionsAreInvalid()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddIrisModelGateway(options =>
            {
                options.ChatModel = "llama3.1";
                options.Timeout = TimeSpan.FromSeconds(30);
            }));

        Assert.Equal("Ollama base URL is required.", exception.Message);
    }

    [Fact]
    public void AddIrisModelGateway_Throws_WhenBaseUrlSchemeIsUnsupported()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddIrisModelGateway(options =>
            {
                options.BaseUrl = "file:///tmp/ollama";
                options.ChatModel = "llama3.1";
                options.Timeout = TimeSpan.FromSeconds(30);
            }));

        Assert.Equal("Ollama base URL must use HTTP or HTTPS.", exception.Message);
    }

    private static OllamaChatModelClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        return new OllamaChatModelClient(
            httpClient,
            new OllamaModelClientOptions
            {
                BaseUrl = "http://localhost:11434",
                ChatModel = "llama3.1",
                Timeout = TimeSpan.FromSeconds(30)
            });
    }

    private static ChatModelRequest CreateRequest()
    {
        return new ChatModelRequest(
            [new ChatModelMessage(ChatModelRole.User, "hello")],
            new ChatModelOptions(Temperature: 0.5));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
