namespace RogueLife.Harness.Hosting;

internal sealed record HarnessOptions(
    int? Ticks,
    ulong? Seed,
    string? Generator,
    string? ConfigPath,
    string? LogLevel,
    bool VerifyReplay,
    bool VerifySaveLoad);

internal enum SeedSource
{
    Unspecified,
    CommandLine,
    Configuration,
    Random,
}

internal sealed record HarnessStartupSettings(
    int Ticks,
    ulong? Seed,
    SeedSource SeedSource,
    string Generator,
    string LogLevel,
    string? ConfigurationFile,
    bool VerifyReplay,
    bool VerifySaveLoad);

