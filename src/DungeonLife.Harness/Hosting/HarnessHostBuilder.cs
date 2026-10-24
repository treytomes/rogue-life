using DungeonLife.Harness.Simulation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Globalization;
using System.Security.Cryptography;

namespace DungeonLife.Harness.Hosting;

internal static class HarnessHostBuilder
{
    private const string LogFileStem = "dungeonlife-harness-.log";
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static IHost Build(HarnessOptions options)
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

        var settings = ResolveSettings(options, builder.Configuration, configFile);
        var serilog = CreateLogger(builder.Configuration, settings.LogLevel, configDirectory);
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(serilog, dispose: true);

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton<IRandom>(_ => new Pcg32Random(settings.Seed!.Value));
        builder.Services.AddSingleton<WorldGenerator>(_ => settings.Generator switch
        {
            "cavern" => new CavernGenerator(),
            "spacious" => new SpaciousCavernGenerator(),
            _ => throw new InvalidOperationException($"Unsupported world generator '{settings.Generator}'."),
        });
        builder.Services.AddSingleton(serviceProvider => new WorldSimulation(
            serviceProvider.GetRequiredService<WorldGenerator>(),
            serviceProvider.GetRequiredService<IRandom>(),
            settings.Seed!.Value));
        builder.Services.AddSingleton<SimulationRunner>();

        return builder.Build();
    }

    private static HarnessStartupSettings ResolveSettings(HarnessOptions options, IConfiguration configuration, string? configFile)
    {
        int ticks = options.Ticks ?? ParseInt(configuration["Harness:Ticks"], defaultValue: 1, "Harness:Ticks");
        if (ticks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), ticks, "Ticks cannot be negative.");
        }

        ulong? seed = options.Seed;
        SeedSource seedSource = seed.HasValue ? SeedSource.CommandLine : SeedSource.Unspecified;
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

        string generator = (options.Generator ?? configuration["Harness:Generator"] ?? "cavern").Trim().ToLowerInvariant();
        if (generator is not ("cavern" or "spacious"))
        {
            throw new FormatException("Harness:Generator must be 'cavern' or 'spacious'.");
        }

        string logLevel = options.LogLevel ?? configuration["Serilog:MinimumLevel"] ?? "Information";
        return new HarnessStartupSettings(ticks, seed, seedSource, generator, logLevel, configFile, options.VerifyReplay);
    }

    private static int ParseInt(string? value, int defaultValue, string key)
    {
        if (value == null)
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            throw new FormatException($"{key} must be an integer.");
        }

        return parsed;
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

    private static Serilog.ILogger CreateLogger(IConfiguration configuration, string minimumLevelSetting, string configDirectory)
    {
        if (!Enum.TryParse(minimumLevelSetting, ignoreCase: true, out LogEventLevel minimumLevel)
            || !Enum.IsDefined(minimumLevel))
        {
            throw new FormatException($"Invalid Serilog minimum level '{minimumLevelSetting}'.");
        }

        string logDirectorySetting = configuration["Serilog:LogDirectory"] ?? "logs";
        string logDirectory = Path.IsPathRooted(logDirectorySetting)
            ? logDirectorySetting
            : Path.Combine(configDirectory, logDirectorySetting);
        Directory.CreateDirectory(logDirectory);

        int retentionLimit = ParseInt(configuration["Serilog:RetainedFileCountLimit"], 14, "Serilog:RetainedFileCountLimit");
        if (retentionLimit < 1)
        {
            throw new FormatException("Serilog:RetainedFileCountLimit must be a positive integer.");
        }

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .WriteTo.Console(
                standardErrorFromLevel: LogEventLevel.Verbose,
                outputTemplate: OutputTemplate)
            .WriteTo.File(
                Path.Combine(logDirectory, LogFileStem),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retentionLimit,
                outputTemplate: OutputTemplate)
            .CreateLogger();
    }
}


