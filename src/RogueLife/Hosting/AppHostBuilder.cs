using RogueLife.ViewModels;
using RogueLife.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

#nullable enable

namespace RogueLife.Hosting
{
    /// <summary>
    /// Composes the desktop application's services with the Microsoft Generic Host.
    /// </summary>
    internal static class AppHostBuilder
    {
        private const string LOG_FILE_STEM = "RogueLife-.log";
        private const string OUTPUT_TEMPLATE = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        public static IHost Build(StartupOptions options)
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = AppContext.BaseDirectory,
            });

            string? configFile = ResolveConfigFile(options.ConfigPath);
            string configDirectory = configFile == null
                ? AppContext.BaseDirectory
                : Path.GetDirectoryName(configFile)!;
            if (configFile != null)
            {
                builder.Configuration.AddYamlFile(configFile, optional: false, reloadOnChange: true);
            }

            var startupSettings = ResolveStartupSettings(options, builder.Configuration, configFile);
            var serilog = CreateSerilogLogger(builder.Configuration, startupSettings.LogLevel, configDirectory);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(serilog, dispose: true);

            builder.Services.AddSingleton(startupSettings);
            builder.Services.AddSingleton<IRandom>(_ => new Pcg32Random(startupSettings.Seed!.Value));

            builder.Services.AddSingleton<WorldGenerator, CavernGenerator>();
            builder.Services.AddSingleton(serviceProvider => new WorldSimulation(
                serviceProvider.GetRequiredService<WorldGenerator>(),
                serviceProvider.GetRequiredService<IRandom>(),
                startupSettings.Seed!.Value));
            builder.Services.AddSingleton<MainWindowViewModel>();
            builder.Services.AddSingleton(serviceProvider =>
                new MainWindow(serviceProvider.GetRequiredService<MainWindowViewModel>()));

            return builder.Build();
        }

        private static Serilog.ILogger CreateSerilogLogger(IConfiguration configuration, string logLevelOverride, string configDirectory)
        {
            if (!Enum.TryParse(logLevelOverride, ignoreCase: true, out LogEventLevel minimumLevel)
                || !Enum.IsDefined(minimumLevel))
            {
                throw new FormatException($"Invalid Serilog minimum level '{logLevelOverride}'.");
            }

            string logDirectorySetting = configuration["Serilog:LogDirectory"] ?? "logs";
            string logDirectory = Path.IsPathRooted(logDirectorySetting)
                ? logDirectorySetting
                : Path.Combine(configDirectory, logDirectorySetting);
            Directory.CreateDirectory(logDirectory);

            int retainedFileCountLimit = 14;
            if (configuration["Serilog:RetainedFileCountLimit"] is string configuredRetention
                && (!int.TryParse(configuredRetention, NumberStyles.Integer, CultureInfo.InvariantCulture, out retainedFileCountLimit)
                    || retainedFileCountLimit < 1))
            {
                throw new FormatException("Serilog:RetainedFileCountLimit must be a positive integer.");
            }

            return new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.Console(outputTemplate: OUTPUT_TEMPLATE)
                .WriteTo.File(
                    Path.Combine(logDirectory, LOG_FILE_STEM),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedFileCountLimit,
                    outputTemplate: OUTPUT_TEMPLATE)
                .CreateLogger();
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

        private static AppStartupSettings ResolveStartupSettings(StartupOptions options, IConfiguration configuration, string? configFile)
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

            if (!seed.HasValue)
            {
                seed = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong)));
                seedSource = SeedSource.Random;
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

            string logLevel = options.LogLevel ?? configuration["Serilog:MinimumLevel"] ?? "Information";
            return new AppStartupSettings(seed, seedSource, tickInterval, logLevel, configFile);
        }
    }
}


