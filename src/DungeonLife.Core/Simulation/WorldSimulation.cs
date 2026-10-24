using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace DungeonLife
{
    public class WorldSimulation : IWorldState
    {
        #region Constants

        private static readonly TimeSpan WorldDelta = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AnimationDelta = TimeSpan.FromMilliseconds(100);

        #endregion

        #region Constructors

        public WorldSimulation(WorldGenerator generator, IRandom random, ulong runSeed)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
            RunSeed = runSeed;
            Cells = new WorldCellCollection(WorldSettings.WorldWidth, WorldSettings.WorldHeight);
            Entities = new EntityCollection();

            generator.Generate(this);
            InitializeComponentRandomStreams();
        }

        #endregion

        #region Properties

        public TimeSpan WorldTime { get; set; }
        public WorldCellCollection Cells { get; }
        public IRandom Random { get; }
        public ulong RunSeed { get; }
        public EntityCollection Entities { get; }
        public bool IsRunning { get; set; } = true;
        public bool IsStepping { get; set; }

        /// <summary>
        /// Advances only when the simulation ticks, so animations freeze while paused.
        /// </summary>
        public TimeSpan AnimationTime { get; private set; } = TimeSpan.Zero;

        public long TickCount { get; private set; }

        public TimeSpan LastTickDuration { get; private set; }

        #endregion

        #region Methods

        public bool IsMovementBlocked(float x, float y)
        {
            return IsMovementBlocked(new Vector2(x, y));
        }

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











