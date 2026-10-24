using DungeonLife.Harness.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DungeonLife.Harness.Simulation;

internal sealed class SimulationRunner(
    WorldSimulation simulation,
    HarnessStartupSettings settings,
    ILogger<SimulationRunner> logger)
{
    public int Run()
    {
        logger.LogInformation(
            "Starting headless simulation: generator {Generator}, configured seed {Seed} ({SeedSource}), ticks {Ticks}",
            settings.Generator,
            settings.Seed,
            settings.SeedSource,
            settings.Ticks);

        for (int i = 0; i < settings.Ticks; i++)
        {
            simulation.Tick();
        }

        RunSummary summary = CreateSummary();
        Console.Out.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        logger.LogInformation("Headless simulation completed after {Ticks} ticks", simulation.TickCount);
        return 0;
    }

    private RunSummary CreateSummary()
    {
        int cellCount = 0;
        int floorCellCount = 0;
        double algaeBiomass = 0;
        double totalTemperature = 0;
        double totalHumidity = 0;

        foreach (WorldCell? cell in simulation.Cells)
        {
            if (cell == null)
            {
                continue;
            }

            cellCount++;
            totalTemperature += cell.Temperature;
            totalHumidity += cell.Humidity;
            if (cell is FloorWorldCell floor)
            {
                floorCellCount++;
                algaeBiomass += floor.AlgaeLevel;
            }
        }

        int entityCount = 0;
        int livingEntityCount = 0;
        foreach (Entity entity in simulation.Entities)
        {
            entityCount++;
            if (!entity.IsDead)
            {
                livingEntityCount++;
            }
        }

        return new RunSummary(
            settings.Generator,
            settings.Seed,
            settings.SeedSource.ToString(),
            settings.Ticks,
            simulation.TickCount,
            (long)simulation.WorldTime.TotalMinutes,
            cellCount,
            floorCellCount,
            algaeBiomass,
            cellCount == 0 ? 0 : totalTemperature / cellCount,
            cellCount == 0 ? 0 : totalHumidity / cellCount,
            entityCount,
            livingEntityCount);
    }
}
