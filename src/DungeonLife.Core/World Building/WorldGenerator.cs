namespace DungeonLife
{
    public abstract class WorldGenerator
    {
        protected IRandom _random = ThreadSafeRandom.Instance;

        public void Generate(IWorldState world)
        {
            BuildWorld(world);
            PlaceEntities(world);
        }

        public abstract void BuildWorld(IWorldState world);

        public abstract void PlaceEntities(IWorldState world);

        protected void CreateWater(IWorldState world, int x, int y)
        {
            var water = new WaterSourceCell(x, y);
            water.Temperature = WorldSettings.IdealTemperature;
            water.Humidity = (float)_random.NextDouble() * 0.02f;
            world.Cells[x, y] = water;
        }

        protected void CreateFloor(IWorldState world, int x, int y)
        {
            var floor = new FloorWorldCell(x, y);
            floor.Temperature = WorldSettings.IdealTemperature;
            floor.Humidity = (float)_random.NextDouble() * 0.02f;
            floor.AlgaeLevel = (float)_random.NextDouble();
            world.Cells[x, y] = floor;
        }

        protected void CreateWall(IWorldState world, int x, int y)
        {
            var wall = new WallWorldCell(x, y);
            wall.Temperature = WorldSettings.IdealTemperature;
            wall.Humidity = (float)_random.NextDouble() * 0.02f;
            world.Cells[x, y] = wall;
        }

        protected void CreateBorderWall(IWorldState world, int x, int y)
        {
            var wall = new BorderWorldCell(x, y);
            wall.Temperature = WorldSettings.IdealTemperature;
            wall.Humidity = (float)_random.NextDouble() * 0.02f;
            world.Cells[x, y] = wall;
        }
    }
}
