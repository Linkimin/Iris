using System.Text.Json.Serialization;

namespace Iris.ModelGateway.Ollama;

internal sealed record OllamaChatRequest(
    string Model,
    IReadOnlyList<OllamaChatMessage> Messages,
    bool Stream,
    OllamaChatOptions? Options);

internal sealed record OllamaChatMessage(string Role, string Content);

internal sealed record OllamaChatOptions(double? Temperature);

internal sealed record OllamaChatResponse(
    string? Model,
    [property: JsonPropertyName("created_at")] DateTimeOffset? CreatedAt,
    OllamaChatMessage? Message,
    bool? Done);
