using System;
using System.Collections.Generic;
using System.Numerics;

namespace DungeonLife
{
    public class Entity
    {
        #region Fields


        private Vector2 _movingDirection;
        private RgbColor _foregroundColor;

        #endregion

        #region Constructors

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

        public float HungerMultiplier { get; protected set; } = 1.5f;
        public float ThirstMultiplier { get; protected set; } = 3.0f;
        public float RangeOfSightMultiplier { get; protected set; } = 0.3f;
        public float RangeOfSmellMultiplier { get; protected set; } = 0.5f;

        /// <summary>
        /// Behaviors to be executed in order.  The first behavior that does something
        /// will be the active behavior for this round.
        /// </summary>
        public List<EntityBehavior> Behaviors { get; } = new List<EntityBehavior>();

        public EntityBehavior ActiveBehavior { get; private set; }

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
                _movingDirection = value;
            }
        }

        public IRandom Random { get; }
        public Vector2 Position { get; set; }
        public TimeSpan MaturityAge { get; protected set; }
        public TimeSpan Age { get; private set; }
        public Gender Gender { get; }

        /// <summary>
        /// Rate of energy consumption.  Indirectly increases hunger and thirst.
        /// </summary>
        public float Metabolism { get; protected set; }

        /// <summary>
        /// Affects the range of sight, and range of smell.
        /// </summary>
        public int BasePerception { get; protected set; }

        public int ActualPerception
        {
            get
            {
                return BasePerception;
            }
        }

        public int BaseSpeed { get; protected set; }

        public int ActualSpeed
        {
            get
            {
                return BaseSpeed;
            }
        }

        public float RangeOfSight
        {
            get
            {
                return ActualPerception * RangeOfSightMultiplier;
            }
        }

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

        public int BabyGlyph { get; protected set; }
        public int AdultGlyph { get; protected set; }

        public bool IsAdult
        {
            get
            {
                return Age >= MaturityAge;
            }
        }

        public bool IsChild
        {
            get
            {
                return !IsAdult;
            }
        }

        public bool IsDead { get; set; } = false;

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

        public virtual void Update(IWorldState world, TimeSpan worldDelta)
        {
            foreach (var state in Behaviors)
            {
                if (state.Update(world))
                {
                    // Stop at the first behavior that actually does something.
                    ActiveBehavior = state;
                    break;
                }
            }

            if (!IsDead)
            {
                Age += worldDelta;
                Hunger += Metabolism * HungerMultiplier;
                if (Hunger > 1)
                {
                    Hunger = 1;
                }
                else if (Hunger < 0)
                {
                    Hunger = 0;
                }
                Thirst += Metabolism * ThirstMultiplier;
                if (Thirst > 1)
                {
                    Thirst = 1;
                }
                else if (Thirst < 0)
                {
                    Thirst = 0;
                }

                var cell = world.Cells[(int)Position.X, (int)Position.Y];
                var newPosition = Position + MovingDirection * cell.MovementSpeedMultiplier;
                if (world.IsMovementBlocked(newPosition))
                {
                    MovingDirection = -MovingDirection;
                }
                else
                {
                    Position = newPosition;

                    // TODO: If the cell is wet, increase the wetness of this entity.
                    // TODO: Water should slow movement speed.
                }
            }
        }

        #endregion
    }
}

