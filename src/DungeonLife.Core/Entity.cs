using System;
using System.Collections.Generic;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>Represents Entity in the simulation.</summary>
    public class Entity
    {
        #region Fields

        private Vector2 _position;
        private Vector2 _movingDirection;
        private RgbColor _foregroundColor;

        #endregion

        #region Constructors

        /// <summary>Creates an instance of Entity.</summary>
        public Entity(int x, int y, IRandom random, Gender? gender = null)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Position = new Vector2(x, y);
            Gender = gender ?? Random.NextEnum<Gender>();
            MaturityAge = TimeSpan.Zero;
            Age = TimeSpan.Zero;
            MovingDirection = Random.NextDirection();
        }

        #endregion

        #region Properties

        /// <summary>
        /// A UI hint that the entity is currently being inspected.
        /// </summary>
        public bool IsSelected { get; set; }

        // A high thirst multiplier makes the entity favor water like a duck.

        /// <summary>Gets or sets the HungerMultiplier value.</summary>
        public float HungerMultiplier { get; protected set; } = 1.5f;
        /// <summary>Gets or sets the ThirstMultiplier value.</summary>
        public float ThirstMultiplier { get; protected set; } = 3.0f;
        /// <summary>Gets or sets the RangeOfSightMultiplier value.</summary>
        public float RangeOfSightMultiplier { get; protected set; } = 0.3f;
        /// <summary>Gets or sets the RangeOfSmellMultiplier value.</summary>
        public float RangeOfSmellMultiplier { get; protected set; } = 0.5f;

        /// <summary>
        /// Behaviors to be executed in order.  The first behavior that does something
        /// will be the active behavior for this round.
        /// </summary>
        public List<EntityBehavior> Behaviors { get; } = new List<EntityBehavior>();

        /// <summary>Gets or sets the ActiveBehavior value.</summary>
        public EntityBehavior ActiveBehavior { get; private set; }

        /// <summary>Gets or sets the normalized movement direction; zero means stationary.</summary>
        public Vector2 MovingDirection
        {
            get
            {
                if (IsDead)
                {
                    return Vector2.Zero;
                }
                else
                {
                    return _movingDirection;
                }
            }
            set
            {
                if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Movement direction components must be finite.");
                }

                if (value == Vector2.Zero)
                {
                    _movingDirection = Vector2.Zero;
                    return;
                }

                double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
                _movingDirection = new Vector2((float)(value.X / length), (float)(value.Y / length));
            }
        }

        /// <summary>Gets or sets the Id value.</summary>
        public long Id { get; internal set; }
        /// <summary>Gets or sets the Random value.</summary>
        public IRandom Random { get; internal set; }
        /// <summary>Gets or sets the Position value.</summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Entity position components must be finite.");
                }

                _position = value;
            }
        }
        /// <summary>Gets or sets the MaturityAge value.</summary>
        public TimeSpan MaturityAge { get; protected set; }
        /// <summary>Gets or sets the Age value.</summary>
        public TimeSpan Age { get; private set; }
        /// <summary>Gets the Gender value.</summary>
        public Gender Gender { get; }

        /// <summary>
        /// Rate of energy consumption.  Indirectly increases hunger and thirst.
        /// </summary>
        public float Metabolism { get; protected set; }

        /// <summary>
        /// Affects the range of sight, and range of smell.
        /// </summary>
        public int BasePerception { get; protected set; }

        /// <summary>Documents this simulation API member.</summary>
        public int ActualPerception
        {
            get
            {
                return BasePerception;
            }
        }

        /// <summary>Gets or sets the BaseSpeed value.</summary>
        public int BaseSpeed { get; protected set; }

        /// <summary>Documents this simulation API member.</summary>
        public int ActualSpeed
        {
            get
            {
                return BaseSpeed;
            }
        }

        /// <summary>Documents this simulation API member.</summary>
        public float RangeOfSight
        {
            get
            {
                return ActualPerception * RangeOfSightMultiplier;
            }
        }

        /// <summary>Documents this simulation API member.</summary>
        public float RangeOfSmell
        {
            get
            {
                return ActualPerception * RangeOfSmellMultiplier;
            }
        }

        /// <summary>
        /// 0% = no hunger.  100% = starvation.
        /// </summary>
        public float Hunger { get; set; }

        /// <summary>
        /// 0% = no thirst.  100% = dying of thirst.
        /// </summary>
        public float Thirst { get; set; }

        /// <summary>Gets or sets the BabyGlyph value.</summary>
        public int BabyGlyph { get; protected set; }
        /// <summary>Gets or sets the AdultGlyph value.</summary>
        public int AdultGlyph { get; protected set; }

        /// <summary>Documents this simulation API member.</summary>
        public bool IsAdult
        {
            get
            {
                return Age >= MaturityAge;
            }
        }

        /// <summary>Documents this simulation API member.</summary>
        public bool IsChild
        {
            get
            {
                return !IsAdult;
            }
        }

        /// <summary>Gets or sets the IsDead value.</summary>
        public bool IsDead { get; set; } = false;

        internal void RestoreSavedState(EntitySavedState state)
        {
            Id = state.Id;
            Position = new Vector2(state.PositionX, state.PositionY);
            MovingDirection = new Vector2(state.DirectionX, state.DirectionY);
            Age = TimeSpan.FromTicks(state.AgeTicks);
            MaturityAge = TimeSpan.FromTicks(state.MaturityAgeTicks);
            Hunger = state.Hunger;
            Thirst = state.Thirst;
            IsDead = state.IsDead;
            Metabolism = state.Metabolism;
            BasePerception = state.BasePerception;
            BaseSpeed = state.BaseSpeed;
            HungerMultiplier = state.HungerMultiplier;
            ThirstMultiplier = state.ThirstMultiplier;
            RangeOfSightMultiplier = state.RangeOfSightMultiplier;
            RangeOfSmellMultiplier = state.RangeOfSmellMultiplier;
            if (Random is not Pcg32Random random)
                throw new InvalidOperationException("Saved state requires PCG32 entity streams.");
            random.RestoreState(state.RandomState);
            foreach (EntityBehavior behavior in Behaviors)
            {
                if (behavior is DeathByOldAgeBehavior death)
                    death.LastDeathCheck = TimeSpan.FromTicks(state.LastDeathCheckTicks);
            }
            ActiveBehavior = state.ActiveBehaviorIndex >= 0 && state.ActiveBehaviorIndex < Behaviors.Count
                ? Behaviors[state.ActiveBehaviorIndex]
                : null;
        }

        internal EntitySavedState CaptureSavedState()
        {
            if (Random is not Pcg32Random random)
                throw new InvalidOperationException("Saved state requires PCG32 entity streams.");
            long lastDeathCheckTicks = 0;
            int activeBehaviorIndex = Behaviors.IndexOf(ActiveBehavior);
            foreach (EntityBehavior behavior in Behaviors)
                if (behavior is DeathByOldAgeBehavior death)
                {
                    lastDeathCheckTicks = death.LastDeathCheck.Ticks;
                    break;
                }
            return new EntitySavedState(Id, GetType().FullName ?? GetType().Name,
                Position.X, Position.Y, _movingDirection.X, _movingDirection.Y,
                Age.Ticks, MaturityAge.Ticks, (int)Gender, Hunger, Thirst, IsDead,
                Metabolism, BasePerception, BaseSpeed, HungerMultiplier, ThirstMultiplier,
                RangeOfSightMultiplier, RangeOfSmellMultiplier, random.CaptureState(),
                lastDeathCheckTicks, activeBehaviorIndex);
        }

        /// <summary>Documents this simulation API member.</summary>
        public int Glyph
        {
            get
            {
                if (IsDead)
                {
                    return '%';
                }
                else
                {
                    return (Age > MaturityAge) ? AdultGlyph : BabyGlyph;
                }
            }
        }

        /// <summary>Documents this simulation API member.</summary>
        public RgbColor ForegroundColor
        {
            get
            {
                if (IsDead)
                {
                    return RgbColor.White;
                }
                else
                {
                    return _foregroundColor;
                }
            }
            set
            {
                _foregroundColor = value;
            }
        }

        #endregion

        #region Methods

        internal EntityIntent Decide(EntityDecisionSnapshot snapshot)
        {
            EntityStateSnapshot self = snapshot.GetEntity(Id);
            if (self.IsDead) return EntityIntent.None(this);

            var context = new EntityDecisionContext(self, snapshot);
            foreach (EntityBehavior behavior in Behaviors)
            {
                if (behavior.TryDecide(context, out EntityIntent intent))
                {
                    return intent with { SelectedBehavior = behavior };
                }
            }

            return EntityIntent.None(this);
        }

        internal void Resolve(EntityIntent intent, IWorldState world, TimeSpan worldDelta)
        {
            if (IsDead)
            {
                ActiveBehavior = null;
                return;
            }

            ActiveBehavior = intent.SelectedBehavior;

            if (intent.Dies)
            {
                IsDead = true;
                return;
            }

            if (intent.MovingDirection.HasValue)
            {
                MovingDirection = intent.MovingDirection.Value;
            }

            ResolveAction(intent, world);

            Age += worldDelta;
            Hunger = Math.Clamp(Hunger + Metabolism * HungerMultiplier, 0.0f, 1.0f);
            Thirst = Math.Clamp(Thirst + Metabolism * ThirstMultiplier, 0.0f, 1.0f);

            WorldCell currentCell = world.Cells[(int)Position.X, (int)Position.Y];
            float movementSpeed = currentCell?.MovementSpeedMultiplier ?? 0;
            Vector2 newPosition = Position + MovingDirection * movementSpeed;
            if (world.IsMovementBlocked(newPosition))
            {
                MovingDirection = -MovingDirection;
            }
            else
            {
                Position = newPosition;
            }
        }

        private void ResolveAction(EntityIntent intent, IWorldState world)
        {
            WorldCell target = world.Cells[(int)intent.ActionTarget.X, (int)intent.ActionTarget.Y];
            switch (intent.Action)
            {
                case EntityAction.Eat when target is FloorWorldCell floor && floor.AlgaeLevel > 0:
                    float requestedAlgae = intent.ActionAmount / intent.ActionFactor;
                    float consumedAlgae = Math.Min(floor.AlgaeLevel, requestedAlgae);
                    floor.AlgaeLevel -= consumedAlgae;
                    Hunger = Math.Max(0, Hunger - consumedAlgae * intent.ActionFactor);
                    break;
                case EntityAction.Drink when target is WaterSourceCell:
                    Thirst = Math.Max(0, Thirst - intent.ActionAmount);
                    break;
            }
        }
        #endregion
    }
}






