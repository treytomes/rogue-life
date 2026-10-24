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
            writer.Write(WorldSettings.RulesVersion);
            writer.Write(settings.Generator);
            writer.Write(settings.Seed!.Value);
            writer.Write(settings.Ticks);
            writer.Write(simulation.Cells.Width);
            writer.Write(simulation.Cells.Height);
            writer.Write(TimeSpan.TicksPerMinute);
            writer.Write(simulation.TickCount);
            writer.Write(simulation.WorldTime.Ticks);

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
            }

            Entity[] entities = simulation.Entities.OrderBy(entity => entity.Id).ToArray();
            writer.Write(entities.Length);
            foreach (Entity entity in entities)
            {
                writer.Write(entity.Id);
                writer.Write(entity.GetType().FullName ?? entity.GetType().Name);
                writer.Write((int)entity.Gender);
                Write(writer, entity.Position);
                Write(writer, entity.MovingDirection);
                writer.Write(entity.Age.Ticks);
                writer.Write(entity.MaturityAge.Ticks);
                writer.Write(entity.IsDead);
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
}
