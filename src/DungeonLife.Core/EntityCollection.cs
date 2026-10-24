using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace DungeonLife
{
    public class EntityCollection : IEnumerable<Entity>
    {
        private readonly List<Entity> _entities;
        private long _nextId = 1;

        public EntityCollection()
        {
            _entities = new List<Entity>();
        }

        public Entity this[float x, float y]
        {
            get
            {
                return At(x, y);
            }
        }

        public void Add(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!_entities.Contains(entity))
            {
                entity.Id = _nextId++;
                _entities.Add(entity);
            }
        }

        /// <summary>Enumerates entities in stable-ID order rotated by the zero-based tick.</summary>
        public IEnumerable<Entity> InPriorityOrder(long zeroBasedTick)
        {
            if (_entities.Count == 0) yield break;

            int start = (int)((ulong)zeroBasedTick % (ulong)_entities.Count);
            for (int offset = 0; offset < _entities.Count; offset++)
            {
                yield return _entities[(start + offset) % _entities.Count];
            }
        }
        public Entity At(float x, float y)
        {
            return At(new Vector2(x, y));
        }

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

        public bool IsMovementBlocked(Vector2 position)
        {
            // TODO: Implement entity sizes; how much of the cell is filled up?
            return false;
        }

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


