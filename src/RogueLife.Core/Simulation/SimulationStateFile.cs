using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RogueLife;

/// <summary>Represents SimulationStateFile in the simulation.</summary>
public static class SimulationStateFile
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>SaveAsync simulation state.</summary>
    public static async Task SaveAsync(string path, SimulationSaveState state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(state);
        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, state, Options, cancellationToken);
    }

    /// <summary>LoadAsync simulation state.</summary>
    public static async Task<SimulationSaveState> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SimulationSaveState>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException("Save file did not contain a simulation state.");
    }
}
