namespace DungeonLife;

/// <summary>Versioned, JSON-serializable data required to resume a simulation.</summary>
public sealed record SimulationSaveState(
    int RulesVersion,
    string Generator,
    ulong RunSeed,
    int WorldWidth,
    int WorldHeight,
    long TickCount,
    long WorldTimeTicks,
    long AnimationTimeTicks,
    Pcg32RandomState RunRandom,
    SavedCellState[] Cells,
    EntitySavedState[] Entities);

public sealed record SavedCellState(
    string Type,
    float Temperature,
    float Humidity,
    float MovementSpeedMultiplier,
    float AlgaeLevel,
    Pcg32RandomState RandomState);

public sealed record EntitySavedState(
    long Id,
    string Type,
    float PositionX,
    float PositionY,
    float DirectionX,
    float DirectionY,
    long AgeTicks,
    long MaturityAgeTicks,
    int Gender,
    float Hunger,
    float Thirst,
    bool IsDead,
    float Metabolism,
    int BasePerception,
    int BaseSpeed,
    float HungerMultiplier,
    float ThirstMultiplier,
    float RangeOfSightMultiplier,
    float RangeOfSmellMultiplier,
    Pcg32RandomState RandomState,
    long LastDeathCheckTicks,
    int ActiveBehaviorIndex);
