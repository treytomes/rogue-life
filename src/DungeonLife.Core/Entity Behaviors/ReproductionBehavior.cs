namespace DungeonLife
{
    /// <summary>Reproduction is not enabled in ruleset version 1.</summary>
    class ReproductionBehavior : EntityBehavior
    {
        public ReproductionBehavior(Entity entity) : base(entity) { }

        internal override bool TryDecide(EntityDecisionContext context, out EntityIntent intent)
        {
            intent = EntityIntent.None(_entity);
            return false;
        }
    }
}

