using Iris.Application.Abstractions.Persistence;
using Iris.Persistence.Database;
using Iris.Persistence.Repositories;
using Iris.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddIrisPersistence(
        this IServiceCollection services,
        Action<DatabaseOptions> configureOptions)
    {
        var options = new DatabaseOptions();
        configureOptions(options);
        options.Validate();

        services.AddSingleton(options);

        services.AddDbContext<IrisDbContext>(dbContextOptions =>
        {
            dbContextOptions.UseSqlite(options.ConnectionString);
        });

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
