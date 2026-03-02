using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LidarIndoorNavigation.Helpers
{
    public class RobotMemory
    {
        int gridResolution = 50;
        float[,] grid = new float[200, 200];
        float freeUpdate = 0.1f;
        float occupiedUpdate = 0.2f;
        float decayRate = 0.98f;
        int gridCenter = 100;
        int gridHeight = 200;
        int gridWidth = 200;

        TestDataGenerator testDataGenerator = new TestDataGenerator();

        public RobotMemory()
        {
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    grid[i, j] = 0.5f;
                }
            }
        }

        public void UpdateMemory()
        {
            testDataGenerator.GenerateTestData();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = grid[x, y] * decayRate + 0.5f * (1 - decayRate);
                }
            }

            for (int i = 0; i < DistancePointsStaticList.CartesianDistances.Count; i++)
            {
                (int gridX, int gridY) endCell = GetGridCoordinates(i);
                var raycast = BresenhamLine(gridCenter, gridCenter, endCell.gridX, endCell.gridY);

                foreach (var (x, y) in raycast)
                {
                    if ((x, y) != (endCell.gridX, endCell.gridY))
                    {
                        grid[x, y] = Math.Max(0.0f, grid[x, y] - freeUpdate);
                    }
                }

                grid[endCell.gridX, endCell.gridY] = Math.Min(1.0f, grid[endCell.gridX, endCell.gridY] + occupiedUpdate);
            }
        }

        private (int GridX, int GridY) GetGridCoordinates(int index)
        {
            int gridX = (int)Math.Floor(gridCenter + DistancePointsStaticList.CartesianDistances[index].x / gridResolution);
            int gridY = (int)Math.Floor(gridCenter + DistancePointsStaticList.CartesianDistances[index].y / gridResolution);

            return (gridX, gridY);
        }

        public static List<(int x, int y)> BresenhamLine(int x0, int y0, int x1, int y1)
        {
            var cells = new List<(int x, int y)>();

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);

            int sx = (x0 < x1) ? 1 : -1;
            int sy = (y0 < y1) ? 1 : -1;

            int err = dx - dy;

            while (true)
            {
                cells.Add((x0, y0));

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;

                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return cells;
        }       
    }
}
