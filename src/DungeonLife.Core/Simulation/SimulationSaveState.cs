namespace DungeonLife;

/// <summary>Defines the name value.</summary>
/// <param name="RulesVersion">Immutable ruleset version used by the run.</param>
/// <param name="Generator">Identifier of the world generator.</param>
/// <param name="RunSeed">Seed used to initialize the run.</param>
/// <param name="WorldWidth">Saved world width in cells.</param>
/// <param name="WorldHeight">Saved world height in cells.</param>
/// <param name="TickCount">Number of committed simulation ticks.</param>
/// <param name="WorldTimeTicks">World clock time in TimeSpan ticks.</param>
/// <param name="AnimationTimeTicks">Animation clock time in TimeSpan ticks.</param>
/// <param name="RunRandom">State of the run-level random stream.</param>
/// <param name="Cells">Cell state in stable coordinate order.</param>
/// <param name="Entities">Entity state in stable ID order.</param>
/// <param name="ReproductionEvents">Cumulative reproduction event counts.</param>
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
    EntitySavedState[] Entities,
    ReproductionEventState ReproductionEvents);

/// <summary>Reproduction outcomes accumulated by a simulation run.</summary>
/// <param name="ConceptionAttempts">Eligible female attempts with an available partner.</param>
/// <param name="Conceptions">Successful conceptions.</param>
/// <param name="Births">Successful births.</param>
/// <param name="DeferredBirthAttempts">Birth placements deferred for lack of floor space.</param>
/// <param name="PregnancyLosses">Pregnancies ended when the mother died.</param>
public sealed record ReproductionEventState(
    long ConceptionAttempts,
    long Conceptions,
    long Births,
    long DeferredBirthAttempts,
    long PregnancyLosses);

/// <summary>Defines the name value.</summary>
/// <param name="Type">Cell type identifier.</param>
/// <param name="Temperature">Current temperature.</param>
/// <param name="Humidity">Current humidity.</param>
/// <param name="MovementSpeedMultiplier">Terrain movement multiplier.</param>
/// <param name="AlgaeLevel">Current algae amount.</param>
/// <param name="RandomState">Cell-owned random stream state.</param>
public sealed record SavedCellState(
    string Type,
    float Temperature,
    float Humidity,
    float MovementSpeedMultiplier,
    float AlgaeLevel,
    Pcg32RandomState RandomState);

/// <summary>Defines the name value.</summary>
/// <param name="Id">Stable entity identifier.</param>
/// <param name="Type">Entity type identifier.</param>
/// <param name="PositionX">Horizontal world position.</param>
/// <param name="PositionY">Vertical world position.</param>
/// <param name="DirectionX">Horizontal movement direction.</param>
/// <param name="DirectionY">Vertical movement direction.</param>
/// <param name="AgeTicks">Current age in TimeSpan ticks.</param>
/// <param name="MaturityAgeTicks">Maturity age in TimeSpan ticks.</param>
/// <param name="Gender">Entity gender value.</param>
/// <param name="Hunger">Current hunger level.</param>
/// <param name="Thirst">Current thirst level.</param>
/// <param name="IsDead">Whether the entity has died.</param>
/// <param name="Metabolism">Metabolic rate.</param>
/// <param name="BasePerception">Base perception range.</param>
/// <param name="BaseSpeed">Base movement speed.</param>
/// <param name="HungerMultiplier">Hunger need multiplier.</param>
/// <param name="ThirstMultiplier">Thirst need multiplier.</param>
/// <param name="RangeOfSightMultiplier">Sight range multiplier.</param>
/// <param name="RangeOfSmellMultiplier">Smell range multiplier.</param>
/// <param name="RandomState">Entity-owned random stream state.</param>
/// <param name="LastDeathCheckTicks">Last old-age mortality check time.</param>
/// <param name="ActiveBehaviorIndex">Index of the currently active behavior.</param>
/// <param name="PregnancyDueWorldTimeTicks">Due time, or zero when not pregnant.</param>
/// <param name="NextBirthAttemptWorldTimeTicks">Next due/deferred placement attempt, or zero when not pregnant.</param>
/// <param name="ReproductionCooldownUntilWorldTimeTicks">Time until the post-birth cooldown ends.</param>
/// <param name="LastMatingWorldDay">World-day index on which this entity last paired, or -1.</param>
/// <param name="WanderChangeDirectionChance">Wander behavior's persisted random threshold.</param>
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
    int ActiveBehaviorIndex,
    long PregnancyDueWorldTimeTicks,
    long NextBirthAttemptWorldTimeTicks,
    long ReproductionCooldownUntilWorldTimeTicks,
    long LastMatingWorldDay,
    float WanderChangeDirectionChance);
