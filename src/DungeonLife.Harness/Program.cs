using System.CommandLine;
using DungeonLife.Harness.Hosting;
using DungeonLife.Harness.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DungeonLife.Harness;

internal static class Program
{
    public static int Main(string[] args)
    {
        Option<int?> ticksOption = new("--ticks", "-t")
        {
            Description = "Number of simulation ticks to run. Overrides Harness:Ticks in appsettings.yaml.",
        };
        Option<ulong?> seedOption = new("--seed", "-s")
        {
            Description = "Simulation RNG seed. Overrides Simulation:Seed in appsettings.yaml.",
        };
        Option<string?> generatorOption = new("--generator", "-g")
        {
            Description = "World generator to use: cavern or spacious. Overrides Harness:Generator.",
        };
        Option<string?> configOption = new("--config", "-c")
        {
            Description = "Path to an appsettings.yaml file instead of the repository-root default.",
        };
        Option<string?> logLevelOption = new("--log-level")
        {
            Description = "Logging minimum level override (Verbose, Debug, Information, Warning, Error, Fatal).",
        };

        RootCommand root = new("Headless DungeonLife simulation runner.")
        {
            ticksOption,
            seedOption,
            generatorOption,
            configOption,
            logLevelOption,
        };

        root.SetAction(parseResult =>
        {
            var options = new HarnessOptions(
                parseResult.GetValue(ticksOption),
                parseResult.GetValue(seedOption),
                parseResult.GetValue(generatorOption),
                parseResult.GetValue(configOption),
                parseResult.GetValue(logLevelOption));

            using IHost host = HarnessHostBuilder.Build(options);
            host.Start();

            try
            {
                return host.Services.GetRequiredService<SimulationRunner>().Run();
            }
            finally
            {
                host.StopAsync().GetAwaiter().GetResult();
            }
        });

        return root.Parse(args).Invoke();
    }
}
