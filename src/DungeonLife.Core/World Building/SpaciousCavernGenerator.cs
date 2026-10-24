namespace DungeonLife
{
    public class SpaciousCavernGenerator : WorldGenerator
    {
        public override void BuildWorld(IWorldState world)
        {
            // Fill the area with floors.
            for (var x = 1; x < world.Cells.Width - 1; x++)
            {
                for (var y = 1; y < world.Cells.Height - 1; y++)
                {
                    CreateFloor(world, x, y);
                }
            }

            // Make a small pond.
            DrawPond(world.Cells, 32, 32, 12);
            DrawPond(world.Cells, 200, 96, 24);
            DrawPond(world.Cells, 0, 255, 32);

            var numPillars = _random.Next(5, 10);
            for (var n = 0; n < numPillars; n++)
            {
                var radius = _random.Next(4, 12);
                var x = _random.Next(0, world.Cells.Width);
                var y = _random.Next(0, world.Cells.Height);
                DrawPillar(world.Cells, x, y, radius);
            }

            // Build the borders.
            for (var x = 0; x < world.Cells.Width; x++)
            {
                world.Cells[x, 0] = new BorderWorldCell(x, 0);
                world.Cells[x, world.Cells.Height - 1] = new BorderWorldCell(x, world.Cells.Height - 1);
            }
            for (var y = 0; y < world.Cells.Height; y++)
            {
                world.Cells[0, y] = new BorderWorldCell(0, y);
                world.Cells[world.Cells.Width - 1, y] = new BorderWorldCell(world.Cells.Width - 1, y);
            }
        }

        public override void PlaceEntities(IWorldState world)
        {
            var numEntities = _random.Next(50, 100);
            for (var n = 0; n < numEntities; n++)
            {
                var isPlaced = false;
                while (!isPlaced)
                {
                    var x = _random.Next(0, 256);
                    var y = _random.Next(0, 256);
                    var cell = world.Cells[x, y];
                    if (!(cell is FloorWorldCell))
                    {
                        continue;
                    }

                    var oink = new OinkEntity(x, y, world.Random);
                    world.Entities.Add(oink);
                    isPlaced = true;
                }
            }
        }

        private void DrawPond(WorldCellCollection cells, int pondX, int pondY, int radius)
        {
            var radius2 = radius * radius;

            for (var x = -radius; x <= radius; x++)
            {
                var dx = pondX + x;
                if ((dx < 0) || (dx >= cells.Width))
                {
                    continue;
                }

                for (var y = -radius; y <= radius; y++)
                {
                    var dy = pondY + y;
                    if ((dy < 0) || (dy >= cells.Height))
                    {
                        continue;
                    }

                    if (x * x + y * y <= radius2)
                    {
                        cells[dx, dy] = new WaterSourceCell(dx, dy);
                    }
                }
            }
        }

        private void DrawPillar(WorldCellCollection cells, int pillarX, int pillarY, int radius)
        {
            var radius2 = radius * radius;

            for (var x = -radius; x <= radius; x++)
            {
                var dx = pillarX + x;
                if ((dx < 0) || (dx >= cells.Width))
                {
                    continue;
                }

                for (var y = -radius; y <= radius; y++)
                {
                    var dy = pillarY + y;
                    if ((dy < 0) || (dy >= cells.Height))
                    {
                        continue;
                    }

                    if (x * x + y * y <= radius2)
                    {
                        cells[dx, dy] = new WallWorldCell(dx, dy);
                    }
                }
            }
        }
    }
}

