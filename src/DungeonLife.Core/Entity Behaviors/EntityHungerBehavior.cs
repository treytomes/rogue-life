using System;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>
    /// Seek out algae to satisfy hunger.
    /// </summary>
    [DisplayName("Hungry")]
    class EntityHungerBehavior : EntityBehavior
    {
        /// <summary>
        /// Reducing the food value of algae should force for travelling in search of food.
        /// </summary>
        private const float ALGAE_FOOD_FACTOR = 0.2f;

        private float _eatSpeed;

        public EntityHungerBehavior(Entity entity, float eatSpeed = 0.1f)
            : base(entity)
        {
            _eatSpeed = eatSpeed;
        }

        private bool IsHungry { get => _random.NextDouble() < _entity.Hunger; }

        private bool Eat(WorldCell cell)
        {
            if (cell is FloorWorldCell)
            {
                var floor = cell as FloorWorldCell;
                if (floor.AlgaeLevel > 0)
                {
                    // Consume the local flora.

                    if (floor.AlgaeLevel > _eatSpeed)
                    {
                        floor.AlgaeLevel -= _eatSpeed / ALGAE_FOOD_FACTOR;
                        _entity.Hunger -= _eatSpeed * ALGAE_FOOD_FACTOR;
                    }
                    else
                    {
                        _entity.Hunger -= floor.AlgaeLevel * ALGAE_FOOD_FACTOR;
                        floor.AlgaeLevel = 0;
                    }

                    if (_entity.Hunger < 0)
                    {
                        _entity.Hunger = 0;
                    }

                    _entity.MovingDirection = Vector2.Zero;

                    return true;
                }
            }
            return false;
        }

        public override bool Update(IWorldState world)
        {
            if ((_entity.Hunger == 0) || !IsHungry)
            {
                return false;
            }

            // Seek out food.

            if ((_entity.Position - Vector2.One).Length() <= 1)
            {
                var a = 1;
            }

            var currentCell = world.Cells[(int)_entity.Position.X, (int)_entity.Position.Y];
            if (Eat(currentCell))
            {
                return true;
            }

            var result = FindClosest<FloorWorldCell>(world, _entity.Position, _entity.RangeOfSmell, c => c.AlgaeLevel);
            if (result != null)
            {
                var delta = Vector2.Zero;
                if ((result.Distance < 2) && Eat(world.Cells[result.Position]))
                {
                    return true;
                }
                delta = result.Position - _entity.Position;

                if (delta != Vector2.Zero)
                {
                    _entity.MovingDirection = Vector2.Normalize(delta);
                    return true;
                }
            }
            return false;

            //var cells = world.Cells.IterateOver(_entity.Position, _entity.RangeOfSmell);

            //var closestFoodDistance = float.MaxValue;
            //var closestFoodPosition = Vector2.One * float.MaxValue;
            //var closestFoodValue = float.MinValue;

            //foreach (var cell in cells)
            //{
            //    var floor = cell as FloorWorldCell;
            //    if ((floor != null) && (floor.AlgaeLevel > 0))
            //    {
            //        // Grab either the closest floor cell or the one with the highest food level.
            //        var dist = (_entity.Position - cell.Position).LengthSquared();

            //        if ((dist < closestFoodDistance) || ((dist == closestFoodDistance) && (floor.AlgaeLevel > closestFoodValue)))
            //        {
            //            closestFoodDistance = dist;
            //            closestFoodPosition = cell.Position;
            //            closestFoodValue = floor.AlgaeLevel;
            //        }
            //    }
            //}

            //var delta = Vector2.Zero;
            //if (closestFoodDistance != float.MaxValue)
            //{
            //    if (closestFoodDistance < 2)
            //    {
            //        if (Eat(world.Cells[(int)closestFoodPosition.X, (int)closestFoodPosition.Y]))
            //        {
            //            return true;
            //        }
            //    }
            //    delta = closestFoodPosition - _entity.Position;
            //}

            //if (delta == Vector2.Zero)
            //{
            //    return false;
            //}
            //else
            //{
            //    _entity.MovingDirection = Vector2.Normalize(delta);
            //    return true;
            //}
        }
    }
}
