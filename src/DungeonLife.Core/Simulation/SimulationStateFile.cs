using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DungeonLife;

/// <summary>Reads and writes versioned simulation checkpoints as JSON.</summary>
public static class SimulationStateFile
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static async Task SaveAsync(string path, SimulationSaveState state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(state);
        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, state, Options, cancellationToken);
    }

    public static async Task<SimulationSaveState> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SimulationSaveState>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException("Save file did not contain a simulation state.");
    }
}
