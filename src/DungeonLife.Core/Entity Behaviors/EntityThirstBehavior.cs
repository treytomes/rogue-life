using System.ComponentModel;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>
    /// Seek out water to satisfy thirst.
    /// </summary>
    [DisplayName("Thirsty")]
    class EntityThirstBehavior : EntityBehavior
    {
        /// <summary>
        /// Water is perfectly thirst-quenching.
        /// </summary>
        private const float WATER_QUENCH_FACTOR = 1.0f;

        private float _drinkSpeed;

        public EntityThirstBehavior(Entity entity, float drinkSpeed = 0.1f)
            : base(entity)
        {
            _drinkSpeed = drinkSpeed;
        }

        private bool IsThirsty { get => _random.NextDouble() < _entity.Thirst; }

        private bool Drink(WorldCell cell)
        {
            if (cell is WaterSourceCell)
            {
                _entity.Thirst -= _drinkSpeed * WATER_QUENCH_FACTOR;
                if (_entity.Thirst < 0)
                {
                    _entity.Thirst = 0;
                }

                _entity.MovingDirection = Vector2.Zero;

                return true;
            }
            return false;
        }

        public override bool Update(IWorldState world)
        {
            if ((_entity.Thirst == 0) || !IsThirsty)
            {
                return false;
            }

            // Seek out water

            var currentCell = world.Cells[(int)_entity.Position.X, (int)_entity.Position.Y];
            if (Drink(currentCell))
            {
                return true;
            }

            SearchResult result = FindClosest<WaterSourceCell>(world, _entity.Position, _entity.RangeOfSmell);
            if (result == null)
            {
                // If there's not water, find the most humid cell.
                result = FindClosest<WorldCell>(world, _entity.Position, _entity.RangeOfSmell, cell => cell.Humidity);
            }

            if (result != null)
            {
                var delta = Vector2.Zero;
                if ((result.Distance < 2) && Drink(world.Cells[result.Position]))
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

        }

            //public override bool Update(IWorldState world)
            //{
            //    if (_entity.Thirst == 0)
            //    {
            //        return false;
            //    }

            //    var isThirsty = _random.NextDouble() < _entity.Thirst;
            //    if (!isThirsty)
            //    {
            //        return false;
            //    }

            //    var currentCell = world.Cells[(int)_entity.Position.X, (int)_entity.Position.Y] as WaterSourceCell;
            //    if (currentCell != null)
            //    {
            //        if (Drink(currentCell))
            //        {
            //            return true;
            //        }
            //    }

            //    // Seek out water.
            //    var cells = world.Cells.IterateOver(_entity.Position, _entity.RangeOfSight);

            //    var closestWaterDistance = float.MaxValue;
            //    var closestWaterPosition = Vector2.One * float.MaxValue;
            //    var closestWaterValue = 0;

            //    var closestHumidDistance = float.MaxValue;
            //    var closestHumidPosition = Vector2.One * float.MaxValue;
            //    var closestHumidityValue = float.MinValue;

            //    foreach (var cell in cells)
            //    {
            //        if (cell is WaterSourceCell)
            //        {
            //            var waterValue = 1.0f;
            //            var dist = (_entity.Position - cell.Position).LengthSquared();

            //            if ((waterValue > closestWaterValue) || ((waterValue == closestWaterValue) && (dist < closestWaterDistance)))
            //            {
            //                closestWaterDistance = dist;
            //                closestWaterPosition = cell.Position;
            //                closestWaterValue = 1;

            //                if (closestWaterDistance < 2)
            //                {
            //                    break;
            //                }
            //            }
            //        }
            //        else if (closestWaterDistance == float.MaxValue)
            //        {
            //            // No water found yet, so look for the most humid cell.
            //            var humidValue = cell.Humidity;
            //            var dist = (_entity.Position - cell.Position).LengthSquared();

            //            // Is the cell either more humid than the current target, or just as humid and closer?
            //            if ((humidValue > closestHumidityValue) || ((cell.Humidity == closestHumidDistance) && (dist < closestHumidDistance)))
            //            {
            //                closestHumidDistance = dist;
            //                closestHumidPosition = cell.Position;
            //                closestHumidityValue = cell.Humidity;
            //            }
            //        }
            //    }

            //    var delta = Vector2.Zero;
            //    if (closestWaterDistance != float.MaxValue)
            //    {
            //        if (closestWaterDistance < 2)
            //        {
            //            // You're next to the water, so take a drink.
            //            if (Drink(world.Cells[(int)closestWaterPosition.X, (int)closestWaterPosition.Y]))
            //            {
            //                return true;
            //            }
            //        }

            //        delta = closestWaterPosition - _entity.Position;
            //    }
            //    else if (closestHumidDistance != float.MaxValue)
            //    {
            //        delta = closestHumidPosition - _entity.Position;
            //    }

            //    if (delta == Vector2.Zero)
            //    {
            //        return false;
            //    }
            //    else
            //    {
            //        _entity.MovingDirection = Vector2.Normalize(delta);
            //        return true;
            //    }
            //}
        }
    }
