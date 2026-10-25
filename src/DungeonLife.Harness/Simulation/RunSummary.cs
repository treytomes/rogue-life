namespace DungeonLife.Harness.Simulation;

internal sealed record RunSummary(
    int RulesVersion,
    string Generator,
    ulong? RunSeed,
    string SeedSource,
    int RequestedTicks,
    long CompletedTicks,
    long WorldTimeMinutes,
    int CellCount,
    int FloorCellCount,
    double AlgaeBiomass,
    double AverageTemperature,
    double AverageHumidity,
    int EntityCount,
    int LivingEntityCount,
    string StateHash,
    bool? ReplayMatched,
    bool? SaveLoadMatched,
    long[] EntityIds,
    long[] FirstTickPriorityIds,
    long[] SecondTickPriorityIds);


