using Iris.Application.Chat.Prompting;
using Iris.Application.Chat.SendMessage;
using Iris.Application.Memory.Commands;
using Iris.Application.Memory.Context;
using Iris.Application.Memory.Options;
using Iris.Application.Memory.Queries;
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
        LanguageOptions languageOptions,
        MemoryOptions memoryOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sendMessageOptions);
        ArgumentNullException.ThrowIfNull(languageOptions);
        ArgumentNullException.ThrowIfNull(memoryOptions);

        if (sendMessageOptions.MaxMessageLength <= 0)
        {
            throw new InvalidOperationException("Chat max message length must be greater than zero.");
        }

        services.AddSingleton(sendMessageOptions);
        services.AddSingleton(languageOptions);
        services.AddSingleton(memoryOptions);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<SendMessageValidator>();
        services.AddSingleton<LanguageInstructionBuilder>();
        services.AddSingleton<ILanguagePolicy, RussianDefaultLanguagePolicy>();
        services.AddSingleton<MemoryPromptFormatter>();
        services.AddScoped<MemoryContextBuilder>();
        services.AddScoped<PromptBuilder>();
        services.AddScoped<SendMessageHandler>();
        services.AddScoped<RememberExplicitFactHandler>();
        services.AddScoped<ForgetMemoryHandler>();
        services.AddScoped<UpdateMemoryHandler>();
        services.AddScoped<RetrieveRelevantMemoriesHandler>();
        services.AddScoped<ListActiveMemoriesHandler>();

        return services;
    }
}
