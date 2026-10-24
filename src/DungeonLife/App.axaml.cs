using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DungeonLife.Hosting;
using DungeonLife.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DungeonLife
{
    public partial class App : Application
    {
        private IHost _host;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _host = AppHostBuilder.Build();
                _host.Start();

                desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
                desktop.Exit += OnDesktopExit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnDesktopExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            _host = null;
        }
    }
}
