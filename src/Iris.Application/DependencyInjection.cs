using Iris.Application.Chat.Prompting;
using Iris.Application.Chat.SendMessage;
using Iris.Application.Persona.Language;
using Iris.Shared.Time;
using Iris.Shared.Time.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace Iris.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIrisApplication(
        this IServiceCollection services,
        SendMessageOptions sendMessageOptions,
        LanguageOptions languageOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sendMessageOptions);
        ArgumentNullException.ThrowIfNull(languageOptions);

        if (sendMessageOptions.MaxMessageLength <= 0)
        {
            throw new InvalidOperationException("Chat max message length must be greater than zero.");
        }

        services.AddSingleton(sendMessageOptions);
        services.AddSingleton(languageOptions);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<SendMessageValidator>();
        services.AddSingleton<LanguageInstructionBuilder>();
        services.AddSingleton<ILanguagePolicy, RussianDefaultLanguagePolicy>();
        services.AddSingleton<PromptBuilder>();
        services.AddScoped<SendMessageHandler>();

        return services;
    }
}
