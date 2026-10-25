using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace DungeonLife
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
                runRandom.CaptureState(), cells, Entities.Select(entity => entity.CaptureSavedState()).ToArray());
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
            if (state.Cells.Length != Cells.Width * Cells.Height || state.Entities.Length != Entities.Count())
                throw new InvalidDataException("Save topology does not match the generated world.");

            Entity[] entities = Entities.ToArray();
            int entityIndex = 0;
            foreach (Entity entity in entities)
            {
                EntitySavedState saved = state.Entities[entityIndex++];
                if (saved.Id != entity.Id || saved.Type != (entity.GetType().FullName ?? entity.GetType().Name)
                    || saved.Gender != (int)entity.Gender)
                    throw new InvalidDataException("Saved entity identity or type does not match the generated world.");
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
            for (int index = 0; index < entities.Length; index++)
                entities[index].RestoreSavedState(state.Entities[index]);
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

            LastTickDuration = Stopwatch.GetElapsedTime(start);
        }

        #endregion
    }
}











