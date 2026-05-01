using System;

using Iris.Application;
using Iris.Application.Chat.SendMessage;
using Iris.Desktop.Models;
using Iris.Desktop.Services;
using Iris.Desktop.ViewModels;
using Iris.ModelGateway;
using Iris.Persistence;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Desktop;

internal static class DependencyInjection
{
    public static IServiceCollection AddIrisDesktop(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var maxMessageLength = GetRequiredPositiveInt32(
            configuration,
            "Application:Chat:MaxMessageLength",
            "Chat max message length must be configured and greater than zero.");

        var databaseConnectionString = GetRequiredString(
            configuration,
            "Database:ConnectionString",
            "Database connection string is required.");

        var ollamaBaseUrl = GetRequiredString(
            configuration,
            "ModelGateway:Ollama:BaseUrl",
            "Ollama base URL is required.");

        var ollamaChatModel = GetRequiredString(
            configuration,
            "ModelGateway:Ollama:ChatModel",
            "Ollama chat model is required.");

        var ollamaTimeoutSeconds = GetRequiredPositiveInt32(
            configuration,
            "ModelGateway:Ollama:TimeoutSeconds",
            "Ollama timeout seconds must be configured and greater than zero.");

        services.AddIrisApplication(new SendMessageOptions(maxMessageLength));
        services.AddIrisPersistence(options => options.ConnectionString = databaseConnectionString);
        services.AddIrisModelGateway(options =>
        {
            options.BaseUrl = ollamaBaseUrl;
            options.ChatModel = ollamaChatModel;
            options.Timeout = TimeSpan.FromSeconds(ollamaTimeoutSeconds);
        });

        services.AddSingleton<IIrisApplicationFacade, IrisApplicationFacade>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<MainWindowViewModel>();

        var avatarEnabled = configuration.GetValue<bool?>("Desktop:Avatar:Enabled") ?? true;
        var avatarSizeString = configuration.GetValue<string>("Desktop:Avatar:Size");
        var avatarPositionString = configuration.GetValue<string>("Desktop:Avatar:Position");
        var avatarDurationString = configuration.GetValue<string>("Desktop:Avatar:SuccessDisplayDurationSeconds");

        AvatarSize avatarSize = ParseEnumOrDefault<AvatarSize>(avatarSizeString, AvatarSize.Medium);
        AvatarPosition avatarPosition = ParseEnumOrDefault<AvatarPosition>(avatarPositionString, AvatarPosition.BottomRight);
        var avatarDuration = ParseDoubleOrDefault(avatarDurationString, 2.0);

        var avatarOptions = new AvatarOptions(avatarEnabled, avatarSize, avatarPosition, avatarDuration);
        services.AddSingleton(avatarOptions);
        services.AddTransient<AvatarViewModel>();

        return services;
    }

    private static string GetRequiredString(
        IConfiguration configuration,
        string key,
        string failureMessage)
    {
        var value = configuration.GetValue<string>(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(failureMessage);
        }

        return value;
    }

    private static int GetRequiredPositiveInt32(
        IConfiguration configuration,
        string key,
        string failureMessage)
    {
        var value = configuration.GetValue<int?>(key);
        if (value is null or <= 0)
        {
            throw new InvalidOperationException(failureMessage);
        }

        return value.Value;
    }

    internal static T ParseEnumOrDefault<T>(string? value, T defaultValue)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return Enum.TryParse<T>(value, ignoreCase: true, out T result) ? result : defaultValue;
    }

    internal static double ParseDoubleOrDefault(string? value, double defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return double.TryParse(value, out var result) && result > 0 ? result : defaultValue;
    }
}
