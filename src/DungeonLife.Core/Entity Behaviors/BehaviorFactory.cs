using System;

namespace DungeonLife
{
    static class BehaviorFactory
    {
        public static EntityBehavior Wander(Entity entity)
        {
            return new EntityWanderBehavior(entity);
        }

        public static EntityBehavior Separation(Entity entity)
        {
            return new EntitySeparationBehavior(entity);
        }

        public static EntityBehavior Alignment(Entity entity)
        {
            return new EntityAlignmentBehavior(entity);
        }

        public static EntityBehavior Cohesion(Entity entity)
        {
            return new EntityCohesionBehavior(entity);
        }

        public static EntityBehavior Thirst(Entity entity)
        {
            return new EntityThirstBehavior(entity);
        }

        public static EntityBehavior Hunger(Entity entity)
        {
            return new EntityHungerBehavior(entity);
        }

        public static EntityBehavior Death(Entity entity, TimeSpan lifeSpan)
        {
            return new DeathByOldAgeBehavior(entity, lifeSpan);
        }
    }
}
