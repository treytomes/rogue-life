using System;
using System.Diagnostics;
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

        public WorldSimulation(WorldGenerator generator, IRandom random)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Cells = new WorldCellCollection(WorldSettings.WorldWidth, WorldSettings.WorldHeight);
            Entities = new EntityCollection();

            generator.Generate(this);
        }

        #endregion

        #region Properties

        public TimeSpan WorldTime { get; set; }
        public WorldCellCollection Cells { get; }
        public IRandom Random { get; }
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

        public void Tick()
        {
            var start = Stopwatch.GetTimestamp();

            WorldTime += WorldDelta;
            AnimationTime += AnimationDelta;
            TickCount++;

            Parallel.ForEach(Cells, cell => cell.Update(this));
            Parallel.ForEach(Entities, entity => entity.Update(this, WorldDelta));

            LastTickDuration = Stopwatch.GetElapsedTime(start);
        }

        #endregion
    }
}



