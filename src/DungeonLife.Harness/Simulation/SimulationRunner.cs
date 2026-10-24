using DungeonLife.Harness.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;

namespace DungeonLife.Harness.Simulation;

internal sealed class SimulationRunner(
    WorldSimulation simulation,
    HarnessStartupSettings settings,
    ILogger<SimulationRunner> logger)
{
    public int Run()
    {
        logger.LogInformation(
            "Starting headless simulation: generator {Generator}, run seed {Seed} ({SeedSource}), ticks {Ticks}",
            settings.Generator,
            settings.Seed,
            settings.SeedSource,
            settings.Ticks);

        for (int i = 0; i < settings.Ticks; i++)
        {
            simulation.Tick();
        }

        string stateHash = SimulationStateHasher.Compute(simulation, settings);
        bool? replayMatched = null;
        if (settings.VerifyReplay)
        {
            WorldGenerator generator = settings.Generator switch
            {
                "cavern" => new CavernGenerator(),
                "spacious" => new SpaciousCavernGenerator(),
                _ => throw new InvalidOperationException($"Unsupported world generator '{settings.Generator}'."),
            };
            var replay = new WorldSimulation(generator, new Pcg32Random(settings.Seed!.Value), settings.Seed.Value);
            for (int i = 0; i < settings.Ticks; i++) replay.Tick();
            string replayHash = SimulationStateHasher.Compute(replay, settings);
            replayMatched = CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(stateHash), Convert.FromHexString(replayHash));
            if (replayMatched != true)
            {
                throw new InvalidOperationException($"Replay state hash mismatch: {stateHash} != {replayHash}");
            }
        }

        RunSummary summary = CreateSummary(stateHash, replayMatched);
        Console.Out.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        logger.LogInformation("Headless simulation completed after {Ticks} ticks", simulation.TickCount);
        return 0;
    }

    private RunSummary CreateSummary(string stateHash, bool? replayMatched)
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

        Entity[] entities = simulation.Entities.ToArray();
        long[] entityIds = entities.Select(entity => entity.Id).ToArray();
        long[] expectedIds = Enumerable.Range(1, entities.Length).Select(id => (long)id).ToArray();
        if (!entityIds.SequenceEqual(expectedIds))
        {
            throw new InvalidOperationException("Entity IDs are not unique, contiguous, and in stable collection order.");
        }

        long[] firstTickPriorityIds = simulation.Entities.InPriorityOrder(0).Select(entity => entity.Id).ToArray();
        long[] secondTickPriorityIds = simulation.Entities.InPriorityOrder(1).Select(entity => entity.Id).ToArray();
        long[] expectedSecondTickIds = entityIds.Length == 0
            ? Array.Empty<long>()
            : entityIds.Skip(1).Concat(entityIds.Take(1)).ToArray();
        if (!firstTickPriorityIds.SequenceEqual(entityIds) || !secondTickPriorityIds.SequenceEqual(expectedSecondTickIds))
        {
            throw new InvalidOperationException("Entity tick priority does not follow the expected stable-ID rotation.");
        }

        int entityCount = 0;
        int livingEntityCount = 0;
        foreach (Entity entity in entities)
        {
            entityCount++;
            if (!entity.IsDead)
            {
                livingEntityCount++;
            }
        }
        return new RunSummary(
            WorldSettings.RulesVersion,
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
            livingEntityCount,
            stateHash,
            replayMatched,
            entityIds,
            firstTickPriorityIds,
            secondTickPriorityIds);
    }
}




