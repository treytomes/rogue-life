using System;

namespace DungeonLife.Views
{
    public class CellSelectedEventArgs : EventArgs
    {
        public CellSelectedEventArgs(int worldX, int worldY)
        {
            WorldX = worldX;
            WorldY = worldY;
        }

        public int WorldX { get; }
        public int WorldY { get; }
    }
}
