using DungeonLife.ViewModels;
using DungeonLife.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;

#nullable enable

namespace DungeonLife.Hosting
{
    /// <summary>
    /// Composes the desktop application's services with the Microsoft Generic Host.
    /// </summary>
    internal static class AppHostBuilder
    {
        public static IHost Build(StartupOptions options)
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory,
            });

            string? configFile = ResolveConfigFile(options.ConfigPath);
            if (configFile != null)
            {
                builder.Configuration.AddYamlFile(configFile, optional: false, reloadOnChange: true);
            }

            var startupSettings = ResolveStartupSettings(options, builder.Configuration);
            builder.Services.AddSingleton(startupSettings);

            builder.Services.AddSingleton<WorldGenerator, CavernGenerator>();
            builder.Services.AddSingleton<WorldSimulation>();
            builder.Services.AddSingleton<MainWindowViewModel>();
            builder.Services.AddSingleton(serviceProvider =>
                new MainWindow(serviceProvider.GetRequiredService<MainWindowViewModel>()));

            return builder.Build();
        }

        private static string? ResolveConfigFile(string? overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                string fullPath = Path.GetFullPath(overridePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {fullPath}", fullPath);
                }

                return fullPath;
            }

            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, "appsettings.yaml");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static AppStartupSettings ResolveStartupSettings(StartupOptions options, IConfiguration configuration)
        {
            ulong? seed = options.Seed;
            SeedSource seedSource = options.Seed.HasValue ? SeedSource.CommandLine : SeedSource.Unspecified;

            if (!seed.HasValue && configuration["Simulation:Seed"] is string configuredSeed)
            {
                if (!ulong.TryParse(configuredSeed, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsedSeed))
                {
                    throw new FormatException("Simulation:Seed must be an unsigned integer.");
                }

                seed = parsedSeed;
                seedSource = SeedSource.Configuration;
            }

            int tickInterval = 100;
            if (configuration["Runtime:TickIntervalMilliseconds"] is string configuredInterval)
            {
                if (!int.TryParse(configuredInterval, NumberStyles.Integer, CultureInfo.InvariantCulture, out tickInterval)
                    || tickInterval < 1)
                {
                    throw new FormatException("Runtime:TickIntervalMilliseconds must be a positive integer.");
                }
            }

            string? logLevel = options.LogLevel ?? configuration["Serilog:MinimumLevel"];
            return new AppStartupSettings(seed, seedSource, tickInterval, logLevel);
        }
    }
}
