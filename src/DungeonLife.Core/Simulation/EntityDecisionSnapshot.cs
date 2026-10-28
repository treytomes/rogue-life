#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DungeonLife;

internal sealed record EntityStateSnapshot(
    long Id,
    Type EntityType,
    Vector2 Position,
    Vector2 MovingDirection,
    float Hunger,
    float Thirst,
    bool IsDead,
    TimeSpan Age,
    float RangeOfSight,
    float RangeOfSmell);

internal sealed class EntityDecisionSnapshot
{
    private readonly Dictionary<long, EntityStateSnapshot> _entitiesById;

    public EntityDecisionSnapshot(WorldCellStateSnapshot cells, IEnumerable<Entity> entities)
    {
        Cells = cells;
        Entities = entities
            .Select(entity => new EntityStateSnapshot(
                entity.Id,
                entity.GetType(),
                entity.Position,
                entity.MovingDirection,
                entity.Hunger,
                entity.Thirst,
                entity.IsDead,
                entity.Age,
                entity.RangeOfSight,
                entity.RangeOfSmell))
            .OrderBy(entity => entity.Id)
            .ToArray();
        _entitiesById = Entities.ToDictionary(entity => entity.Id);
    }

    public WorldCellStateSnapshot Cells { get; }
    public IReadOnlyList<EntityStateSnapshot> Entities { get; }

    public EntityStateSnapshot GetEntity(long id) => _entitiesById[id];

    public IEnumerable<EntityStateSnapshot> InArea(Vector2 position, float radius, Type entityType)
    {
        float radiusSquared = radius * radius;
        foreach (EntityStateSnapshot entity in Entities)
        {
            if (entity.EntityType == entityType && Vector2.DistanceSquared(entity.Position, position) <= radiusSquared)
            {
                yield return entity;
            }
        }
    }
}

internal sealed record EntityDecisionContext(EntityStateSnapshot Self, EntityDecisionSnapshot World);

internal enum EntityAction
{
    None,
    Eat,
    Drink,
}

internal sealed record EntityIntent(
    Entity Entity,
    EntityBehavior? SelectedBehavior = null,
    Vector2? MovingDirection = null,
    EntityAction Action = EntityAction.None,
    Vector2 ActionTarget = default,
    float ActionAmount = 0,
    float ActionFactor = 1,
    bool Dies = false)
{
    public static EntityIntent None(Entity entity) => new(entity);
}






