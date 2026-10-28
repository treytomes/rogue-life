using System;

namespace RogueLife.Views
{
    /// <summary>Represents CellSelectedEventArgs in the simulation.</summary>
    public class CellSelectedEventArgs : EventArgs
    {
        /// <summary>Creates an instance of CellSelectedEventArgs.</summary>
        public CellSelectedEventArgs(int worldX, int worldY)
        {
            WorldX = worldX;
            WorldY = worldY;
        }

        /// <summary>Gets the WorldX value.</summary>
        public int WorldX { get; }
        /// <summary>Gets the WorldY value.</summary>
        public int WorldY { get; }
    }
}
