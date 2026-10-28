using Avalonia;
using System;

using System.CommandLine;
using RogueLife.Hosting;

#nullable enable

namespace RogueLife
{
    class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Option<ulong?> seedOption = new("--seed", "-s")
            {
                Description = "Simulation RNG seed. Overrides Simulation:Seed in appsettings.yaml.",
            };
            Option<string?> configOption = new("--config", "-c")
            {
                Description = "Path to an appsettings.yaml file instead of the repository-root default.",
            };
            Option<string?> logLevelOption = new("--log-level")
            {
                Description = "Logging minimum level override (Verbose, Debug, Information, Warning, Error, Fatal).",
            };

            RootCommand root = new("Rogue Life artificial life simulation.")
            {
                seedOption,
                configOption,
                logLevelOption,
            };

            root.SetAction(parseResult =>
            {
                App.StartupOptions = new StartupOptions(
                    parseResult.GetValue(seedOption),
                    parseResult.GetValue(configOption),
                    parseResult.GetValue(logLevelOption));

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                return 0;
            });

            return root.Parse(args).Invoke();
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
