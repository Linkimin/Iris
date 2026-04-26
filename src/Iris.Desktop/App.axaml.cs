using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Iris.Desktop.Views;
using AvaloniaApplication = Avalonia.Application;

namespace Iris.Desktop
{
    internal class App : AvaloniaApplication
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
