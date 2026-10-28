using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RogueLife
{
    /// <summary>Represents CavernGenerator in the simulation.</summary>
    public class CavernGenerator : WorldGenerator
    {
		#region Constants

		private const int SMOOTH_ITERATIONS = 4;

		/// <summary>
		/// The size below which regions will be culled.
		/// </summary>
		private const int THRESHOLD_SIZE = 20;

		private const int WALL_FILL_PERCENTAGE = 45;
		private const int WATER_FILL_PERCENTAGE = 35;

		#endregion

        #region Methods

        /// <summary>Executes the BuildWorld behavior.</summary>
        public override void BuildWorld(IWorldState world)
        {
			GenerateCavern(world);
			RemoveSmallRegions(world);
			ConnectRegions(world);
		}

		/// <summary>Executes the PlaceEntities behavior.</summary>
		public override void PlaceEntities(IWorldState world)
        {
			var numEntities = _random.Next(50, 100);
			for (var n = 0; n < numEntities; n++)
			{
				var isPlaced = false;
				var remainingAttempts = 10;
				while (!isPlaced && (remainingAttempts > 0))
				{
					var x = _random.Next(0, 256);
					var y = _random.Next(0, 256);
					var cell = world.Cells[x, y];
					if (!(cell is FloorWorldCell))
					{
						remainingAttempts--;
						continue;
					}

					var oink = new OinkEntity(x, y, world.Random);
					world.Entities.Add(oink);
					isPlaced = true;
				}
			}
		}

		#region Generate the cavern.

		private void GenerateCavern(IWorldState world)
		{
			RandomFillMap(world);

			for (var iteration = 0; iteration < SMOOTH_ITERATIONS; iteration++)
			{
				SmoothMap(world);
            }

            // This is not needed in infinite maps.
            EnsureMapBorder(world);
		}

		private void RandomFillMap(IWorldState world)
		{
			for (var x = 0; x < world.Cells.Width; x++)
			{
				for (var y = 0; y < world.Cells.Height; y++)
				{
					var percent = world.Random.Next(0, 100);
					if (percent < WALL_FILL_PERCENTAGE)
					{
						CreateWall(world, x, y);
					}
					else if (percent < WALL_FILL_PERCENTAGE + WATER_FILL_PERCENTAGE)
                    {
						CreateWater(world, x, y);
                    }
					else
					{
						CreateFloor(world, x, y);
					}
				}
			}
		}

		private void SmoothMap(IWorldState world)
		{
			for (var x = 0; x < world.Cells.Width; x++)
			{
				for (var y = 0; y < world.Cells.Height; y++)
				{
					var numWalls = CountNeighbors<WallWorldCell>(world, x, y);
					if (numWalls > 4)
					{
						CreateWall(world, x, y);
					}
					else if (numWalls < 4)
                    {
						var numWaters = CountNeighbors<WaterSourceCell>(world, x, y);
						if (numWaters > 4)
                        {
							CreateWater(world, x, y);
                        }
						else if (numWaters < 4)
                        {
							CreateFloor(world, x, y);
                        }
					}
				}
			}
		}

		private int CountNeighbors<TCell>(IWorldState world, int x, int y)
			where TCell : WorldCell
		{
			var count = 0;
			for (var neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
			{
				for (var neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
				{
					if ((neighbourX != x) || (neighbourY != y))
					{
						var cell = world.Cells[neighbourX, neighbourY];
						if ((cell != null) && (cell is TCell))
						{
							count++;
						}
					}
				}
			}

			return count;
		}

		private void EnsureMapBorder(IWorldState world)
		{
			for (var x = 0; x < world.Cells.Width; x++)
			{
				CreateBorderWall(world, x, 0);
				CreateBorderWall(world, x, world.Cells.Height - 1);
			}
			for (var y = 0; y < world.Cells.Height; y++)
			{
				CreateBorderWall(world, 0, y);
				CreateBorderWall(world, world.Cells.Width - 1, y);
			}
		}

		#endregion

		#region Remove small regions.

		private void RemoveSmallRegions(IWorldState world)
		{
			// We do this in two rounds, because the first round will probably remove some regions.
			EnsureMinimumSize(world, GetRegions<WallWorldCell>(world), THRESHOLD_SIZE, true);
			//EnsureMinimumSize(world, GetRegions<WaterSourceCell>(world), THRESHOLD_SIZE, false);
			EnsureMinimumSize(world, GetRegions<FloorWorldCell>(world), THRESHOLD_SIZE, false);
		}

		/// <summary>
		/// If a region size is &lt;= thresholdSize, change all of it's tiles to revertTileId.
		/// </summary>
		/// <remarks>
		/// Rooms that are too small will be removed from the regions list.
		/// </remarks>
		/// <param name="world">The world whose regions are updated.</param>
		/// <param name="regions">The regions to inspect.</param>
		/// <param name="thresholdSize">If a region is &lt;= this size, delete it.</param>
		/// <param name="checkWalls">Whether removed regions are replaced with floor.</param>
		private void EnsureMinimumSize(IWorldState world, List<List<Vector2>> regions, int thresholdSize, bool checkWalls)
		{
			for (var index = 0; index < regions.Count; index++)
			{
				var region = regions[index];
				if (region.Count < thresholdSize)
				{
					foreach (var point in region)
					{
						if (checkWalls)
						{
							CreateFloor(world, (int)point.X, (int)point.Y);
						}
						else
						{
							CreateWall(world, (int)point.X, (int)point.Y);
						}
					}
					regions.Remove(region);
					index--;
				}
			}
		}

		#endregion

		#region Locate regions.

		private List<List<Vector2>> GetRegions<TCell>(IWorldState world)
			where TCell : WorldCell
		{
			// This is the collection of regions we have found..
			var regions = new List<List<Vector2>>();

			// Track whether a position has been checked.
			var mapFlags = new bool[world.Cells.Height, world.Cells.Width];

			for (var x = 0; x < world.Cells.Width; x++)
			{
				for (var y = 0; y < world.Cells.Height; y++)
				{
					if (!mapFlags[y, x])
					{
						var cell = world.Cells[x, y];
						if (cell is TCell)
						{
							var newRegion = GetRegionPoints<TCell>(world, x, y);
							regions.Add(newRegion);

							// Mark each position in the newly-found region as checked, so we don't try putting a second region here.
							foreach (var point in newRegion)
							{
								mapFlags[(int)point.Y, (int)point.X] = true;
							}
						}
					}
				}
			}

			return regions;
		}

		private List<Vector2> GetRegionPoints<TCell>(IWorldState world, int startX, int startY)
			where TCell : WorldCell
		{
			// The list of tiles in this region.
			var points = new List<Vector2>();

			// Track whether a position has been checked.
			var mapFlags = new bool[world.Cells.Height, world.Cells.Width];

			// The list of tiles to check.
			var queue = new Queue<Vector2>();

			queue.Enqueue(new Vector2(startX, startY));
			mapFlags[startY, startX] = true;

			while (queue.Count > 0)
			{
				var chunkPos = queue.Dequeue();
				points.Add(chunkPos);

				for (var x = chunkPos.X - 1; x <= chunkPos.X + 1; x++)
				{
					for (var y = chunkPos.Y - 1; y <= chunkPos.Y + 1; y++)
					{
						if ((0 <= x) && (x < world.Cells.Width) && (0 <= y) && (y < world.Cells.Height) && ((y == chunkPos.Y) || (x == chunkPos.X)))
						{
							if (!mapFlags[(int)y, (int)x])
							{
								var cell = world.Cells[(int)x, (int)y];
								if (cell is TCell)
								{
									mapFlags[(int)y, (int)x] = true;
									queue.Enqueue(new Vector2(x, y));
								}
							}
						}
					}
				}
			}

			return points;
		}

		#endregion

		#region Connect isolated regions.

		private void ConnectRegions(IWorldState world)
		{
			var floorRegions = GetRegions<FloorWorldCell>(world).Select(x => new LevelRegion(x, world)).ToList();
			floorRegions.Sort();
			if (floorRegions.Count > 0)
			{
				floorRegions[0].IsMainRegion = true;
				floorRegions[0].IsAccessibleFromMainRegion = true;

				ConnectClosestRooms(world, floorRegions);
			}
		}

		private void ConnectClosestRooms(IWorldState world, List<LevelRegion> floorRegions, bool forceAccessibilityFromMainRegion = false)
		{
			var roomListA = new List<LevelRegion>(); // is not accessible from main region
			var roomListB = new List<LevelRegion>(); // is accessible from main region

			if (forceAccessibilityFromMainRegion)
			{
				foreach (var room in floorRegions)
				{
					if (room.IsAccessibleFromMainRegion)
					{
						roomListB.Add(room);
					}
					else
					{
						roomListA.Add(room);
					}
				}
			}
			else
			{
				roomListA = floorRegions;
				roomListB = floorRegions;
			}

			var bestDistance = 0;
			var bestTileA = new Vector2();
			var bestTileB = new Vector2();
			LevelRegion bestRegionA = null;
			LevelRegion bestRegionB = null;
			var possibleConnectionFound = false;

			foreach (var regionA in roomListA)
			{
				if (!forceAccessibilityFromMainRegion)
				{
					possibleConnectionFound = false;
					if (regionA.ConnectedRegions.Count > 0)
					{
						continue;
					}
				}

				foreach (var regionB in roomListB)
				{
					if ((regionA == regionB) || (regionA.IsConnected(regionB)))
					{
						continue;
					}

					// Find the closest points between the two rooms.
					for (var tileIndexA = 0; tileIndexA < regionA.EdgeTiles.Count; tileIndexA++)
					{
						for (var tileIndexB = 0; tileIndexB < regionB.EdgeTiles.Count; tileIndexB++)
						{
							var tileA = regionA.EdgeTiles[tileIndexA];
							var tileB = regionB.EdgeTiles[tileIndexB];
							var distanceBetweenRegions = (int)(Math.Pow(tileA.X - tileB.X, 2) + Math.Pow(tileA.Y - tileB.Y, 2));

							if ((distanceBetweenRegions < bestDistance) || !possibleConnectionFound)
							{
								bestDistance = distanceBetweenRegions;
								possibleConnectionFound = true;
								bestTileA = tileA;
								bestTileB = tileB;
								bestRegionA = regionA;
								bestRegionB = regionB;
							}
						}
					}
				}

				if (possibleConnectionFound && !forceAccessibilityFromMainRegion)
				{
					CreatePassage(world, bestRegionA, bestRegionB, bestTileA, bestTileB);
				}
			}

			if (possibleConnectionFound && forceAccessibilityFromMainRegion)
			{
				CreatePassage(world, bestRegionA, bestRegionB, bestTileA, bestTileB);
				ConnectClosestRooms(world, floorRegions, true);
			}

			if (!forceAccessibilityFromMainRegion)
			{
				ConnectClosestRooms(world, floorRegions, true);
			}
		}

		private void CreatePassage(IWorldState world, LevelRegion regionA, LevelRegion regionB, Vector2 pointA, Vector2 pointB)
		{
			LevelRegion.ConnectRegions(regionA, regionB);

			var line = GetLine(pointA, pointB);
			foreach (var c in line)
			{
				DrawCircle(world, c, 1);
			}
		}

		private void DrawCircle(IWorldState world, Vector2 c, int passageSize)
		{
			for (var x = -passageSize; x <= passageSize; x++)
			{
				for (var y = -passageSize; y <= passageSize; y++)
				{
					if (x * x + y * y <= passageSize * passageSize)
					{
						var drawX = c.X + x;
						var drawY = c.Y + y;
						if ((0 <= drawX) && (x < world.Cells.Width) && (0 <= y) && (y < world.Cells.Height))
						{
							CreateFloor(world, (int)drawX, (int)drawY);
						}
					}
				}
			}
		}

		private List<Vector2> GetLine(Vector2 from, Vector2 to)
		{
			var line = new List<Vector2>();

			var x = from.X;
			var y = from.Y;

			var dx = to.X - from.X;
			var dy = to.Y - from.Y;

			var inverted = false;
			var step = Math.Sign(dx);
			var gradientStep = Math.Sign(dy);

			var longest = Math.Abs(dx);
			var shortest = Math.Abs(dy);

			if (longest < shortest)
			{
				inverted = true;
				longest = Math.Abs(dy);
				shortest = Math.Abs(dx);

				step = Math.Sign(dy);
				gradientStep = Math.Sign(dx);
			}

			var gradientAccumulation = longest / 2;
			for (var i = 0; i < longest; i++)
			{
				line.Add(new Vector2(x, y));

				if (inverted)
				{
					y += step;
				}
				else
				{
					x += step;
				}

				gradientAccumulation += shortest;
				if (gradientAccumulation >= longest)
				{
					if (inverted)
					{
						x += gradientStep;
					}
					else
					{
						y += gradientStep;
					}
					gradientAccumulation -= longest;
				}
			}

			return line;
		}

		#endregion

		#endregion

		#region Helper Classes

		/// <summary>
		/// A level can be composed of many regions.
		/// </summary>
		[Serializable]
		private class LevelRegion : IComparable<LevelRegion>
		{
			#region Fields

			public readonly List<Vector2> EdgeTiles;
			public readonly List<LevelRegion> ConnectedRegions;

			#endregion

			#region Constructors

			public LevelRegion(List<Vector2> points, IWorldState world)
			{
				Points = points;
				EdgeTiles = new List<Vector2>();
				ConnectedRegions = new List<LevelRegion>();

				IsMainRegion = false;
				IsAccessibleFromMainRegion = false;

				LocateEdgeTiles(world);
			}

			#endregion

			#region Properties

			public bool IsMainRegion { get; set; }

			public bool IsAccessibleFromMainRegion { get; set; }

			public int Size
			{
				get
				{
					return Points.Count();
				}
			}

			public IEnumerable<Vector2> Points { get; private set; }

			#endregion

			#region Methods

			public static void ConnectRegions(LevelRegion regionA, LevelRegion regionB)
			{
				if (regionA.IsAccessibleFromMainRegion)
				{
					regionB.SetAccessibleFromMainRegion();
				}
				else if (regionB.IsAccessibleFromMainRegion)
				{
					regionA.SetAccessibleFromMainRegion();
				}

				regionA.ConnectedRegions.Add(regionB);
				regionB.ConnectedRegions.Add(regionA);
			}

			public bool IsConnected(LevelRegion otherRegion)
			{
				return ConnectedRegions.Contains(otherRegion);
			}

			public int CompareTo(LevelRegion other)
			{
				return Size.CompareTo(other.Size);
			}

			public void SetAccessibleFromMainRegion()
			{
				if (!IsAccessibleFromMainRegion)
				{
					IsAccessibleFromMainRegion = true;
					foreach (var region in ConnectedRegions)
					{
						region.SetAccessibleFromMainRegion();
					}
				}
			}

			private void LocateEdgeTiles(IWorldState world)
			{
				//var regionCell = world.Cells[Points.First()];

				// TODO: Make this configurable?
				Func<float, float, bool> isOnEdge = (x, y) => world.IsMovementBlocked(x, y);

				foreach (var pnt in Points)
				{
					for (var x = pnt.X - 1; x <= pnt.X + 1; x++)
					{
						for (var y = pnt.Y - 1; y <= pnt.Y + 1; y++)
						{
							if ((0 <= x) && (x < world.Cells.Width) && (0 <= y) && (y < world.Cells.Height) && ((x == pnt.X) || (y == pnt.Y)))
							{
								if (isOnEdge(x, y))
								{
									EdgeTiles.Add(pnt);
								}
							}
						}
					}
				}
			}

			public bool Contains(Vector2 point)
			{
				return Points.Any(x => x.Equals(point));
			}

			public bool Contains(LevelRegion region)
			{
				return region.Points.All(x => Contains(x));
			}

			public bool Intersects(LevelRegion region)
			{
				return region.Points.Any(x => Contains(x));
			}

			public Vector2 RandomPoint(IRandom random)
			{
				return Points.ElementAt(random.Next(Points.Count() - 1));
			}

			#endregion
		}

		#endregion
	}
}


