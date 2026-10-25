namespace DungeonLife
{
    /// <summary>Represents WorldGenerator in the simulation.</summary>
    public abstract class WorldGenerator
    {
        /// <summary>Defines the _random value.</summary>
        protected IRandom _random;

        /// <summary>Executes the Generate behavior.</summary>
        public void Generate(IWorldState world)
        {
            _random = world.Random;
            BuildWorld(world);
            PlaceEntities(world);
        }

        /// <summary>Executes the BuildWorld behavior.</summary>
        public abstract void BuildWorld(IWorldState world);

        /// <summary>Executes the PlaceEntities behavior.</summary>
        public abstract void PlaceEntities(IWorldState world);

        /// <summary>Executes the CreateWater behavior.</summary>
        protected void CreateWater(IWorldState world, int x, int y)
        {
            var water = new WaterSourceCell(x, y);
            water.Temperature = WorldSettings.IdealTemperature;
            water.Humidity = (float)_random.NextDouble() * 0.02f;
            world.Cells[x, y] = water;
        }

        /// <summary>Executes the CreateFloor behavior.</summary>
        protected void CreateFloor(IWorldState world, int x, int y)
        {
            var floor = new FloorWorldCell(x, y);
            floor.Temperature = WorldSettings.IdealTemperature;
            floor.Humidity = (float)_random.NextDouble() * 0.02f;
            floor.AlgaeLevel = (float)_random.NextDouble();
            world.Cells[x, y] = floor;
        }

        /// <summary>Executes the CreateWall behavior.</summary>
        protected void CreateWall(IWorldState world, int x, int y)
        {
            var wall = new WallWorldCell(x, y);
            wall.Temperature = WorldSettings.IdealTemperature;
            wall.Humidity = (float)_random.NextDouble() * 0.02f;
            world.Cells[x, y] = wall;
        }

        /// <summary>Executes the CreateBorderWall behavior.</summary>
        protected void CreateBorderWall(IWorldState world, int x, int y)
        {
            var wall = new BorderWorldCell(x, y);
            wall.Temperature = WorldSettings.IdealTemperature;
            wall.Humidity = (float)_random.NextDouble() * 0.02f;
            world.Cells[x, y] = wall;
        }
    }
}

