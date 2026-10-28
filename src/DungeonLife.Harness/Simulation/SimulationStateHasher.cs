using System.Numerics;
using System.Security.Cryptography;
using DungeonLife.Harness.Hosting;

namespace DungeonLife.Harness.Simulation;

/// <summary>Hashes run inputs and observable model state in stable collection order.</summary>
internal static class SimulationStateHasher
{
    public static string Compute(WorldSimulation simulation, HarnessStartupSettings settings)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            SimulationSaveState completeState = simulation.CaptureState(settings.Generator);
            writer.Write(WorldSettings.RulesVersion);
            writer.Write(settings.Generator);
            writer.Write(settings.Seed!.Value);
            writer.Write(settings.Ticks);
            writer.Write(simulation.Cells.Width);
            writer.Write(simulation.Cells.Height);
            writer.Write(TimeSpan.TicksPerMinute);
            writer.Write(simulation.TickCount);
            writer.Write(simulation.WorldTime.Ticks);
            writer.Write(simulation.ReproductionEvents.ConceptionAttempts);
            writer.Write(simulation.ReproductionEvents.Conceptions);
            writer.Write(simulation.ReproductionEvents.Births);
            writer.Write(simulation.ReproductionEvents.DeferredBirthAttempts);
            writer.Write(simulation.ReproductionEvents.PregnancyLosses);
            writer.Write(simulation.ReproductionEvents.FemaleDailyChecks);
            writer.Write(simulation.ReproductionEvents.FemaleReadyChecks);
            writer.Write(simulation.ReproductionEvents.FemalesSuppressedPregnant);
            writer.Write(simulation.ReproductionEvents.FemalesSuppressedCooldown);
            writer.Write(simulation.ReproductionEvents.FemalesSuppressedByNeeds);
            writer.Write(simulation.ReproductionEvents.NoEligibleMaleAnywhere);
            writer.Write(simulation.ReproductionEvents.EligibleMalesOnlyOutOfRange);
            writer.Write(simulation.ReproductionEvents.NearbyMalesNeedIneligible);
            writer.Write(simulation.ReproductionEvents.NearbyEligibleMalesAlreadyPaired);
            Write(writer, completeState.RunRandom);

            for (int x = 0; x < simulation.Cells.Width; x++)
            for (int y = 0; y < simulation.Cells.Height; y++)
            {
                WorldCell? cell = simulation.Cells[x, y];
                writer.Write(cell != null);
                if (cell == null) continue;
                writer.Write(cell.GetType().FullName ?? cell.GetType().Name);
                Write(writer, cell.Position);
                writer.Write(BitConverter.SingleToInt32Bits(cell.Temperature));
                writer.Write(BitConverter.SingleToInt32Bits(cell.Humidity));
                writer.Write(BitConverter.SingleToInt32Bits(cell.MovementSpeedMultiplier));
                writer.Write(BitConverter.SingleToInt32Bits(cell is FloorWorldCell floor ? floor.AlgaeLevel : 0));
                Write(writer, completeState.Cells[x * simulation.Cells.Height + y].RandomState);
            }

            Entity[] entities = simulation.Entities.OrderBy(entity => entity.Id).ToArray();
            EntitySavedState[] savedEntities = completeState.Entities;
            writer.Write(entities.Length);
            for (int index = 0; index < entities.Length; index++)
            {
                Entity entity = entities[index];
                EntitySavedState saved = savedEntities[index];
                writer.Write(entity.Id);
                writer.Write(entity.GetType().FullName ?? entity.GetType().Name);
                writer.Write((int)entity.Gender);
                Write(writer, entity.Position);
                Write(writer, entity.MovingDirection);
                writer.Write(entity.Age.Ticks);
                writer.Write(entity.MaturityAge.Ticks);
                writer.Write(entity.IsDead);
                writer.Write(saved.PregnancyDueWorldTimeTicks);
                writer.Write(saved.NextBirthAttemptWorldTimeTicks);
                writer.Write(saved.ReproductionCooldownUntilWorldTimeTicks);
                writer.Write(saved.LastMatingWorldDay);
                writer.Write(BitConverter.SingleToInt32Bits(saved.WanderChangeDirectionChance));
                Write(writer, saved.RandomState);
                writer.Write(BitConverter.SingleToInt32Bits(entity.Hunger));
                writer.Write(BitConverter.SingleToInt32Bits(entity.Thirst));
                writer.Write(BitConverter.SingleToInt32Bits(entity.Metabolism));
                writer.Write(entity.BasePerception);
                writer.Write(entity.BaseSpeed);
                writer.Write(entity.ActiveBehavior?.GetType().FullName ?? string.Empty);
            }
        }

        return Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0, checked((int)stream.Length))));
    }

    private static void Write(BinaryWriter writer, Vector2 value)
    {
        writer.Write(BitConverter.SingleToInt32Bits(value.X));
        writer.Write(BitConverter.SingleToInt32Bits(value.Y));
    }

    private static void Write(BinaryWriter writer, Pcg32RandomState value)
    {
        writer.Write(value.Seed);
        writer.Write(value.Sequence);
        writer.Write(value.State);
        writer.Write(value.Increment);
        writer.Write(BitConverter.DoubleToInt64Bits(value.SpareGaussian));
        writer.Write(value.HasSpareGaussian);
    }
}
