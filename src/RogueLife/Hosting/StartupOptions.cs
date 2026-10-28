#nullable enable

namespace RogueLife.Hosting
{
    /// <summary>
    /// Values parsed from the command line before the host resolves configuration.
    /// </summary>
    internal sealed record StartupOptions(ulong? Seed, string? ConfigPath, string? LogLevel);

    /// <summary>Represents SeedSource in the simulation.</summary>
    public enum SeedSource
    {
        /// <summary>Defines the Unspecified value.</summary>
        Unspecified,
        /// <summary>Defines the CommandLine value.</summary>
        CommandLine,
        /// <summary>Defines the Configuration value.</summary>
        Configuration,
        /// <summary>Defines the Random value.</summary>
        Random,
    }

    /// <summary>
    /// Runtime settings resolved from command-line overrides and appsettings.yaml.
    /// </summary>
    public sealed record AppStartupSettings(
        ulong? Seed,
        SeedSource SeedSource,
        int TickIntervalMilliseconds,
        string LogLevel,
        string? ConfigurationFile);
}

