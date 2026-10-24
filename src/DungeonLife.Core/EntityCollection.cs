using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace DungeonLife
{
    public class EntityCollection : IEnumerable<Entity>
    {
        private readonly List<Entity> _entities;

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
            if (!_entities.Contains(entity))
            {
                _entities.Add(entity);
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
