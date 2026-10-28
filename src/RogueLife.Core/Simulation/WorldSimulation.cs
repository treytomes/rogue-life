using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace RogueLife
{
    /// <summary>Represents WorldSimulation in the simulation.</summary>
    public class WorldSimulation : IWorldState
    {
        #region Constants

        private static readonly TimeSpan WorldDelta = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AnimationDelta = TimeSpan.FromMilliseconds(100);

        #endregion

        #region Constructors

        /// <summary>Creates an instance of WorldSimulation.</summary>
        public WorldSimulation(WorldGenerator generator, IRandom random, ulong runSeed)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
            RunSeed = runSeed;
            Cells = new WorldCellCollection(WorldSettings.WorldWidth, WorldSettings.WorldHeight);
            Entities = new EntityCollection();

            generator.Generate(this);
            InitializeComponentRandomStreams();
        }

        /// <summary>FromSavedState simulation state.</summary>
        public static WorldSimulation FromSavedState(SimulationSaveState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            WorldGenerator generator = state.Generator switch
            {
                "cavern" => new CavernGenerator(),
                "spacious" => new SpaciousCavernGenerator(),
                _ => throw new InvalidDataException($"Unsupported saved generator '{state.Generator}'."),
            };
            var simulation = new WorldSimulation(generator, new Pcg32Random(state.RunSeed), state.RunSeed);
            simulation.RestoreState(state, state.Generator);
            return simulation;
        }

        #endregion

        #region Properties

        /// <summary>Gets or sets the WorldTime value.</summary>
        public TimeSpan WorldTime { get; set; }
        /// <summary>Gets the Cells value.</summary>
        public WorldCellCollection Cells { get; }
        /// <summary>Gets the Random value.</summary>
        public IRandom Random { get; }
        /// <summary>Gets the RunSeed value.</summary>
        public ulong RunSeed { get; }
        /// <summary>Gets the Entities value.</summary>
        public EntityCollection Entities { get; }
        /// <summary>Gets cumulative reproduction event counts for this run.</summary>
        public ReproductionEventState ReproductionEvents { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        /// <summary>Gets or sets the IsRunning value.</summary>
        public bool IsRunning { get; set; } = true;
        /// <summary>Gets or sets the IsStepping value.</summary>
        public bool IsStepping { get; set; }

        /// <summary>
        /// Advances only when the simulation ticks, so animations freeze while paused.
        /// </summary>
        public TimeSpan AnimationTime { get; private set; } = TimeSpan.Zero;

        /// <summary>Gets or sets the TickCount value.</summary>
        public long TickCount { get; private set; }

        /// <summary>Gets or sets the LastTickDuration value.</summary>
        public TimeSpan LastTickDuration { get; private set; }

        /// <summary>CaptureState simulation state.</summary>
        public SimulationSaveState CaptureState(string generator)
        {
            if (Random is not Pcg32Random runRandom)
                throw new InvalidOperationException("Saved state requires a PCG32 run stream.");

            var cells = new SavedCellState[Cells.Width * Cells.Height];
            for (int x = 0; x < Cells.Width; x++)
            for (int y = 0; y < Cells.Height; y++)
            {
                WorldCell cell = Cells[x, y];
                if (cell != null) cells[x * Cells.Height + y] = cell.CaptureSavedState();
            }

            return new SimulationSaveState(WorldSettings.RulesVersion, generator, RunSeed,
                Cells.Width, Cells.Height, TickCount, WorldTime.Ticks, AnimationTime.Ticks,
                runRandom.CaptureState(), cells,
                Entities.OrderBy(entity => entity.Id).Select(entity => entity.CaptureSavedState()).ToArray(),
                ReproductionEvents);
        }

        /// <summary>RestoreState simulation state.</summary>
        public void RestoreState(SimulationSaveState state, string generator)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.RulesVersion != WorldSettings.RulesVersion)
                throw new InvalidDataException($"Save uses rules version {state.RulesVersion}; this build supports {WorldSettings.RulesVersion}.");
            if (state.Generator != generator || state.RunSeed != RunSeed
                || state.WorldWidth != Cells.Width || state.WorldHeight != Cells.Height)
                throw new InvalidDataException("Save configuration does not match this simulation's generator, seed, or world dimensions.");
            if (state.Cells.Length != Cells.Width * Cells.Height || state.ReproductionEvents == null)
                throw new InvalidDataException("Save topology or reproduction state is invalid.");

            EntitySavedState[] savedEntities = state.Entities.OrderBy(entity => entity.Id).ToArray();
            for (int index = 0; index < savedEntities.Length; index++)
            {
                EntitySavedState saved = savedEntities[index];
                if (saved.Id != index + 1 || saved.Type != (typeof(OinkEntity).FullName ?? nameof(OinkEntity))
                    || !Enum.IsDefined(typeof(Gender), saved.Gender))
                    throw new InvalidDataException("Saved entity IDs, type, or gender are invalid.");
            }
            for (int x = 0; x < Cells.Width; x++)
            for (int y = 0; y < Cells.Height; y++)
            {
                WorldCell cell = Cells[x, y];
                SavedCellState saved = state.Cells[x * Cells.Height + y];
                if ((cell == null) != (saved == null)
                    || (cell != null && saved!.Type != (cell.GetType().FullName ?? cell.GetType().Name)))
                    throw new InvalidDataException("Saved cell topology does not match the generated world.");
            }

            if (Random is not Pcg32Random runRandom)
                throw new InvalidOperationException("Saved state requires a PCG32 run stream.");
            runRandom.RestoreState(state.RunRandom);
            for (int x = 0; x < Cells.Width; x++)
            for (int y = 0; y < Cells.Height; y++)
                if (Cells[x, y] is WorldCell cell)
                    cell.RestoreSavedState(state.Cells[x * Cells.Height + y]!);
            Entities.ClearForRestore();
            foreach (EntitySavedState saved in savedEntities)
            {
                var entity = new OinkEntity(0, 0, CreateEntityRandom(RunSeed, saved.Id), (Gender)saved.Gender);
                Entities.Add(entity);
                entity.RestoreSavedState(saved);
            }
            ReproductionEvents = state.ReproductionEvents;
            TickCount = state.TickCount;
            WorldTime = TimeSpan.FromTicks(state.WorldTimeTicks);
            AnimationTime = TimeSpan.FromTicks(state.AnimationTimeTicks);
        }

        #endregion

        #region Methods

        /// <summary>Executes the IsMovementBlocked behavior.</summary>
        public bool IsMovementBlocked(float x, float y)
        {
            return IsMovementBlocked(new Vector2(x, y));
        }

        /// <summary>Executes the IsMovementBlocked behavior.</summary>
        public bool IsMovementBlocked(Vector2 position)
        {
            return Cells.IsMovementBlocked(position) || Entities.IsMovementBlocked(position);
        }

        private void InitializeComponentRandomStreams()
        {
            const ulong cellDomain = 0x43454C4CUL;
            const ulong entityDomain = 0x454E5449UL;

            foreach (WorldCell cell in Cells)
            {
                if (cell == null) continue;
                ulong cellId = ((ulong)(int)cell.Position.X << 32) | (uint)(int)cell.Position.Y;
                cell.Random = CreateComponentRandom(RunSeed, cellDomain, cellId);
            }

            foreach (Entity entity in Entities)
            {
                entity.Random = CreateComponentRandom(RunSeed, entityDomain, (ulong)entity.Id);
            }
        }

        private static IRandom CreateComponentRandom(ulong seed, ulong domain, ulong identity)
        {
            ulong componentSeed = Mix(seed ^ domain ^ identity);
            ulong sequence = Mix(seed + domain + (identity * 0x9E3779B97F4A7C15UL));
            return new Pcg32Random(componentSeed, sequence);
        }

        internal static IRandom CreateEntityRandom(ulong seed, long id) =>
            CreateComponentRandom(seed, 0x454E5449UL, (ulong)id);

        private static ulong Mix(ulong value)
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
        /// <summary>Advances the world by one simulation tick.</summary>
        public void Tick()
        {
            var start = Stopwatch.GetTimestamp();

            WorldTime += WorldDelta;
            AnimationTime += AnimationDelta;
            TickCount++;

            WorldCellStateSnapshot snapshot = Cells.CaptureSnapshot();
            WorldCellNextState[,] nextCellStates = new WorldCellNextState[Cells.Width, Cells.Height];
            Parallel.For(0, Cells.Width, x =>
            {
                for (int y = 0; y < Cells.Height; y++)
                {
                    WorldCell cell = Cells[x, y];
                    if (cell != null)
                    {
                        nextCellStates[x, y] = cell.CalculateNextState(snapshot, WorldTime);
                    }
                }
            });
            Cells.Commit(nextCellStates);
            Entity[] entitiesAtTickStart = Entities.ToArray();
            Entity[] priorityOrder = Entities.InPriorityOrder(TickCount - 1).ToArray();
            var entitySnapshot = new EntityDecisionSnapshot(Cells.CaptureSnapshot(), Entities);
            EntityIntent[] intents = new EntityIntent[priorityOrder.Length];
            for (int index = 0; index < priorityOrder.Length; index++)
            {
                intents[index] = priorityOrder[index].Decide(entitySnapshot);
            }

            // Resolve all effects only after every entity has decided from the same snapshot.
            // The priority order is rotated by tick; movement overlap remains allowed.
            foreach (EntityIntent intent in intents)
            {
                intent.Entity.Resolve(intent, this, WorldDelta);
            }

            ResolveReproduction(entitiesAtTickStart);

            LastTickDuration = Stopwatch.GetElapsedTime(start);
        }

        private void ResolveReproduction(Entity[] entitiesAtTickStart)
        {
            long currentWorldDay = WorldTime.Ticks / ReproductionRules.Day.Ticks;
            OinkEntity[] oinks = entitiesAtTickStart.OfType<OinkEntity>().ToArray();

            // Gestations and due births resolve after all entities present at tick start.
            foreach (OinkEntity female in oinks.Where(entity => entity.Gender == Gender.Female))
            {
                if (!female.IsPregnant)
                    continue;

                if (female.IsDead)
                {
                    female.PregnancyDueWorldTimeTicks = 0;
                    female.NextBirthAttemptWorldTimeTicks = 0;
                    ReproductionEvents = ReproductionEvents with
                    {
                        PregnancyLosses = ReproductionEvents.PregnancyLosses + 1,
                    };
                    continue;
                }

                if (WorldTime.Ticks < female.NextBirthAttemptWorldTimeTicks)
                    continue;

                Vector2? birthPosition = FindBirthPosition(female.Position);
                if (birthPosition is not Vector2 position)
                {
                    female.NextBirthAttemptWorldTimeTicks = WorldTime.Add(ReproductionRules.Day).Ticks;
                    ReproductionEvents = ReproductionEvents with
                    {
                        DeferredBirthAttempts = ReproductionEvents.DeferredBirthAttempts + 1,
                    };
                    continue;
                }

                var newborn = new OinkEntity((int)position.X, (int)position.Y,
                    CreateEntityRandom(RunSeed, Entities.NextId));
                Entities.Add(newborn);
                female.PregnancyDueWorldTimeTicks = 0;
                female.NextBirthAttemptWorldTimeTicks = 0;
                female.ReproductionCooldownUntilWorldTimeTicks =
                    WorldTime.Add(ReproductionRules.PostBirthCooldown).Ticks;
                ReproductionEvents = ReproductionEvents with { Births = ReproductionEvents.Births + 1 };
            }

            var pairedMaleIds = new HashSet<long>(oinks
                .Where(entity => entity.LastMatingWorldDay == currentWorldDay)
                .Select(entity => entity.Id));
            OinkEntity[] livingMatureMales = oinks
                .Where(entity => entity.Gender == Gender.Male && !entity.IsDead && entity.IsAdult)
                .ToArray();
            OinkEntity[] eligibleMales = livingMatureMales.Where(IsEligible).ToArray();
            IEnumerable<OinkEntity> femalePriority = Entities
                .InPriorityOrder(TickCount - 1)
                .OfType<OinkEntity>()
                .Where(entity => entity.Gender == Gender.Female && oinks.Contains(entity));

            foreach (OinkEntity female in femalePriority)
            {
                if (female.IsDead || !IsDailyAttempt(female))
                    continue;

                ReproductionEvents = ReproductionEvents with
                {
                    FemaleDailyChecks = ReproductionEvents.FemaleDailyChecks + 1,
                };

                if (female.IsPregnant)
                {
                    ReproductionEvents = ReproductionEvents with
                    {
                        FemalesSuppressedPregnant = ReproductionEvents.FemalesSuppressedPregnant + 1,
                    };
                    continue;
                }

                if (WorldTime.Ticks < female.ReproductionCooldownUntilWorldTimeTicks)
                {
                    ReproductionEvents = ReproductionEvents with
                    {
                        FemalesSuppressedCooldown = ReproductionEvents.FemalesSuppressedCooldown + 1,
                    };
                    continue;
                }

                if (!IsEligible(female))
                {
                    ReproductionEvents = ReproductionEvents with
                    {
                        FemalesSuppressedByNeeds = ReproductionEvents.FemalesSuppressedByNeeds + 1,
                    };
                    continue;
                }

                ReproductionEvents = ReproductionEvents with
                {
                    FemaleReadyChecks = ReproductionEvents.FemaleReadyChecks + 1,
                };
                OinkEntity[] nearbyMatureMales = livingMatureMales
                    .Where(candidate => DistanceSquared(candidate.Position, female.Position)
                        <= ReproductionRules.MatingRadius * ReproductionRules.MatingRadius)
                    .ToArray();
                OinkEntity[] nearbyEligibleMales = nearbyMatureMales.Where(IsEligible).ToArray();
                OinkEntity[] availableNearbyMales = nearbyEligibleMales
                    .Where(candidate => !pairedMaleIds.Contains(candidate.Id))
                    .ToArray();

                if (availableNearbyMales.Length == 0)
                {
                    if (nearbyMatureMales.Length > 0 && nearbyEligibleMales.Length == 0)
                    {
                        ReproductionEvents = ReproductionEvents with
                        {
                            NearbyMalesNeedIneligible = ReproductionEvents.NearbyMalesNeedIneligible + 1,
                        };
                    }
                    else if (nearbyEligibleMales.Length > 0)
                    {
                        ReproductionEvents = ReproductionEvents with
                        {
                            NearbyEligibleMalesAlreadyPaired = ReproductionEvents.NearbyEligibleMalesAlreadyPaired + 1,
                        };
                    }
                    else if (eligibleMales.Length > 0)
                    {
                        ReproductionEvents = ReproductionEvents with
                        {
                            EligibleMalesOnlyOutOfRange = ReproductionEvents.EligibleMalesOnlyOutOfRange + 1,
                        };
                    }
                    else
                    {
                        ReproductionEvents = ReproductionEvents with
                        {
                            NoEligibleMaleAnywhere = ReproductionEvents.NoEligibleMaleAnywhere + 1,
                        };
                    }

                    continue;
                }

                OinkEntity male = availableNearbyMales
                    .OrderBy(candidate => DistanceSquared(candidate.Position, female.Position))
                    .ThenBy(candidate => candidate.Id)
                    .First();

                female.LastMatingWorldDay = currentWorldDay;
                male.LastMatingWorldDay = currentWorldDay;
                pairedMaleIds.Add(male.Id);
                ReproductionEvents = ReproductionEvents with
                {
                    ConceptionAttempts = ReproductionEvents.ConceptionAttempts + 1,
                };

                int nearbyOinks = Entities.OfType<OinkEntity>().Count(candidate => !candidate.IsDead
                    && DistanceSquared(candidate.Position, female.Position)
                        <= ReproductionRules.DensityRadius * ReproductionRules.DensityRadius);
                double probability = ReproductionRules.BaselineConceptionProbability(
                    female.Age, female.MaturityAge) * ReproductionRules.DensityMultiplier(nearbyOinks);
                if (female.Random.NextDouble() < probability)
                {
                    female.PregnancyDueWorldTimeTicks = WorldTime.Add(ReproductionRules.Gestation).Ticks;
                    female.NextBirthAttemptWorldTimeTicks = female.PregnancyDueWorldTimeTicks;
                    ReproductionEvents = ReproductionEvents with
                    {
                        Conceptions = ReproductionEvents.Conceptions + 1,
                    };
                }
            }
        }

        private static bool IsEligible(OinkEntity entity) =>
            !entity.IsDead && entity.IsAdult
            && entity.Hunger <= ReproductionRules.MaximumNeed
            && entity.Thirst <= ReproductionRules.MaximumNeed;

        private static bool IsDailyAttempt(OinkEntity female)
        {
            long adultAgeTicks = female.Age.Ticks - female.MaturityAge.Ticks;
            return adultAgeTicks >= 0 && adultAgeTicks % ReproductionRules.Day.Ticks == 0;
        }

        private Vector2? FindBirthPosition(Vector2 motherPosition)
        {
            int minX = Math.Max(0, (int)Math.Ceiling(motherPosition.X - ReproductionRules.PlacementRadius));
            int maxX = Math.Min(Cells.Width - 1, (int)Math.Floor(motherPosition.X + ReproductionRules.PlacementRadius));
            int minY = Math.Max(0, (int)Math.Ceiling(motherPosition.Y - ReproductionRules.PlacementRadius));
            int maxY = Math.Min(Cells.Height - 1, (int)Math.Floor(motherPosition.Y + ReproductionRules.PlacementRadius));
            double radiusSquared = ReproductionRules.PlacementRadius * ReproductionRules.PlacementRadius;
            Vector2? result = null;
            double bestDistanceSquared = double.PositiveInfinity;

            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                if (Cells[x, y] is not FloorWorldCell)
                    continue;

                double distanceSquared = DistanceSquared(motherPosition, new Vector2(x, y));
                if (distanceSquared > radiusSquared)
                    continue;
                if (distanceSquared < bestDistanceSquared)
                {
                    result = new Vector2(x, y);
                    bestDistanceSquared = distanceSquared;
                }
            }

            return result;
        }

        private static double DistanceSquared(Vector2 first, Vector2 second)
        {
            double dx = (double)first.X - second.X;
            double dy = (double)first.Y - second.Y;
            return dx * dx + dy * dy;
        }

        #endregion
    }
}











