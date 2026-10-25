using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>Represents EntityCollection in the simulation.</summary>
    public class EntityCollection : IEnumerable<Entity>
    {
        private readonly List<Entity> _entities;
        private long _nextId = 1;

        /// <summary>Creates an instance of EntityCollection.</summary>
        public EntityCollection()
        {
            _entities = new List<Entity>();
        }

        /// <summary>Gets the cell or entity at the specified coordinates.</summary>
        public Entity this[float x, float y]
        {
            get
            {
                return At(x, y);
            }
        }

        /// <summary>Executes the Add behavior.</summary>
        public void Add(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!_entities.Contains(entity))
            {
                entity.Id = _nextId++;
                _entities.Add(entity);
            }
        }

        /// <summary>Executes the InPriorityOrder behavior.</summary>
        public IEnumerable<Entity> InPriorityOrder(long zeroBasedTick)
        {
            if (_entities.Count == 0) yield break;

            int start = (int)((ulong)zeroBasedTick % (ulong)_entities.Count);
            for (int offset = 0; offset < _entities.Count; offset++)
            {
                yield return _entities[(start + offset) % _entities.Count];
            }
        }
        /// <summary>Executes the At behavior.</summary>
        public Entity At(float x, float y)
        {
            return At(new Vector2(x, y));
        }

        /// <summary>Executes the At behavior.</summary>
        public Entity At(Vector2 position)
        {
            foreach (var entity in _entities)
            {
                var delta = entity.Position - position;
                if (delta.LengthSquared() <= 2)
                {
                    return entity;
                }
            }
            return null;
        }

        /// <summary>Executes the InArea behavior.</summary>
        public IEnumerable<Entity> InArea(Vector2 position, float radius, Type entityType = null)
        {
            var radiusSquare = radius * radius;
            foreach (var entity in _entities)
            {
                var delta = entity.Position - position;
                if (delta.LengthSquared() <= radiusSquare)
                {
                    if ((entityType == null) || (entityType == entity.GetType()))
                    {
                        yield return entity;
                    }
                }
            }
        }

        /// <summary>Executes the IsMovementBlocked behavior.</summary>
        public bool IsMovementBlocked(Vector2 position)
        {
            // TODO: Implement entity sizes; how much of the cell is filled up?
            return false;
        }

        /// <summary>Executes the GetEnumerator behavior.</summary>
        public IEnumerator<Entity> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}


