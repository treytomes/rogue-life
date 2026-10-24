using System;
using System.Numerics;

namespace DungeonLife
{
    public interface IWorldState
    {
        TimeSpan WorldTime { get; set; }
        WorldCellCollection Cells { get; }
        EntityCollection Entities { get; }
        bool IsMovementBlocked(float x, float y);
        bool IsMovementBlocked(Vector2 position);
    }
}
