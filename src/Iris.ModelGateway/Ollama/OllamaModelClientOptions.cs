using Iris.Shared.Results;

namespace Iris.ModelGateway.Ollama;

public sealed class OllamaModelClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ChatModel { get; set; } = string.Empty;

    public TimeSpan Timeout { get; set; }

    public Result Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            return Result.Failure(Error.Validation(
                "model_gateway.ollama.base_url_required",
                "Ollama base URL is required."));
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? baseUri))
        {
            return Result.Failure(Error.Validation(
                "model_gateway.ollama.base_url_invalid",
                "Ollama base URL must be an absolute URI."));
        }

        if (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(Error.Validation(
                "model_gateway.ollama.base_url_scheme_invalid",
                "Ollama base URL must use HTTP or HTTPS."));
        }

        if (Timeout <= TimeSpan.Zero)
        {
            return Result.Failure(Error.Validation(
                "model_gateway.ollama.timeout_invalid",
                "Ollama timeout must be greater than zero."));
        }

        return Result.Success();
    }
}
