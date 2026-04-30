using System;
using System.Threading;

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Iris.Desktop.ViewModels;
using Iris.Desktop.Views;
using Iris.Persistence.Database;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using AvaloniaApplication = Avalonia.Application;

namespace Iris.Desktop
{
    internal class App : AvaloniaApplication
    {
        private ServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                IConfiguration configuration = BuildConfiguration();
                _serviceProvider = BuildServiceProvider(configuration);
                InitializeDatabase(_serviceProvider);

                desktop.ShutdownRequested += (_, _) => _serviceProvider?.Dispose();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .Build();
        }

        private static ServiceProvider BuildServiceProvider(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddIrisDesktop(configuration);

            return services.BuildServiceProvider();
        }

        private static void InitializeDatabase(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IIrisDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IIrisDatabaseInitializer>();
            initializer.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
