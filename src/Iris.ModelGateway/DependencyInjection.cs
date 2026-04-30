using Iris.Application.Abstractions.Models.Interfaces;
using Iris.ModelGateway.Http;
using Iris.ModelGateway.Ollama;
using Iris.Shared.Results;

using Microsoft.Extensions.DependencyInjection;

namespace Iris.ModelGateway;

public static class DependencyInjection
{
    public static IServiceCollection AddIrisModelGateway(
        this IServiceCollection services,
        Action<OllamaModelClientOptions> configureOllama)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOllama);

        var options = new OllamaModelClientOptions();
        configureOllama(options);

        Result validation = options.Validate();
        if (validation.IsFailure)
        {
            throw new InvalidOperationException(validation.Error.Message);
        }

        services.AddSingleton(options);
        services.AddHttpClient<IChatModelClient, OllamaChatModelClient>(
            ModelGatewayHttpClientNames.Ollama,
            (serviceProvider, httpClient) =>
            {
                OllamaModelClientOptions ollamaOptions = serviceProvider.GetRequiredService<OllamaModelClientOptions>();
                httpClient.BaseAddress = new Uri(ollamaOptions.BaseUrl, UriKind.Absolute);
                httpClient.Timeout = ollamaOptions.Timeout;
            });

        return services;
    }
}
