using System;
using System.Numerics;

namespace DungeonLife
{
    /// <summary>Represents IWorldState in the simulation.</summary>
    public interface IWorldState
    {
        /// <summary>Gets or sets the WorldTime value.</summary>
        TimeSpan WorldTime { get; set; }
        /// <summary>Gets the Cells value.</summary>
        WorldCellCollection Cells { get; }
        /// <summary>Gets the Random value.</summary>
        IRandom Random { get; }
        /// <summary>Gets the Entities value.</summary>
        EntityCollection Entities { get; }
        /// <summary>Executes the IsMovementBlocked behavior.</summary>
        bool IsMovementBlocked(float x, float y);
        /// <summary>Executes the IsMovementBlocked behavior.</summary>
        bool IsMovementBlocked(Vector2 position);
    }
}



