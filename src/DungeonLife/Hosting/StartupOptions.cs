#nullable enable

namespace DungeonLife.Hosting
{
    /// <summary>
    /// Values parsed from the command line before the host resolves configuration.
    /// </summary>
    internal sealed record StartupOptions(ulong? Seed, string? ConfigPath, string? LogLevel);

    /// <summary>Identifies where the active run seed was resolved from.</summary>
    public enum SeedSource
    {
        Unspecified,
        CommandLine,
        Configuration,
    }

    /// <summary>
    /// Runtime settings resolved from command-line overrides and appsettings.yaml.
    /// </summary>
    public sealed record AppStartupSettings(
        ulong? Seed,
        SeedSource SeedSource,
        int TickIntervalMilliseconds,
        string? LogLevel);
}
