using System.Net;
using Iris.Shared.Results;

namespace Iris.ModelGateway.Http;

internal static class ModelGatewayHttpErrorHandler
{
    public static Error FromStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => Error.Failure(
                "model_gateway.provider_not_found",
                "The configured local model provider endpoint or model was not found."),
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => Error.Failure(
                "model_gateway.provider_timeout",
                "The local model provider did not respond in time."),
            HttpStatusCode.TooManyRequests => Error.Failure(
                "model_gateway.provider_rate_limited",
                "The local model provider is temporarily rate limited."),
            >= HttpStatusCode.InternalServerError => Error.Failure(
                "model_gateway.provider_failure",
                "The local model provider returned an internal error."),
            _ => Error.Failure(
                "model_gateway.provider_http_error",
                "The local model provider returned an unsuccessful response.")
        };
    }
}
