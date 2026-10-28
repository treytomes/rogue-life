using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RogueLife.Hosting;
using RogueLife.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RogueLife
{
    /// <summary>Represents App in the simulation.</summary>
    public partial class App : Application
    {
        private IHost _host;
        private ILogger<App> _logger;

        internal static StartupOptions StartupOptions { get; set; } = new StartupOptions(null, null, null);

        /// <summary>Executes the Initialize behavior.</summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>Executes the OnFrameworkInitializationCompleted behavior.</summary>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _host = AppHostBuilder.Build(StartupOptions);
                _host.Start();

                _logger = _host.Services.GetRequiredService<ILogger<App>>();
                var startupSettings = _host.Services.GetRequiredService<AppStartupSettings>();
                _logger.LogInformation(
                    "RogueLife started with run seed {Seed} ({SeedSource}), tick interval {TickIntervalMilliseconds} ms, configuration {ConfigurationFile}, and log level {LogLevel}",
                    startupSettings.Seed,
                    startupSettings.SeedSource,
                    startupSettings.TickIntervalMilliseconds,
                    startupSettings.ConfigurationFile ?? "defaults",
                    startupSettings.LogLevel);

                desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
                desktop.Exit += OnDesktopExit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnDesktopExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _logger?.LogInformation("RogueLife shutting down");
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            _host = null;
        }
    }
}

