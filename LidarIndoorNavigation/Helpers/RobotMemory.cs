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

            for (int i = 0; i < DistancePointsStaticList.CartesianDistances.Count; i++)
            {
                (int gridX, int gridY) endCell = GetGridCoordinates(i);
                var raycast = BresenhamLine(gridCenter, gridCenter, endCell.gridX, endCell.gridY);

                grid[endCell.gridX, endCell.gridY] = Math.Min(1.0f, grid[endCell.gridX, endCell.gridY] + occupiedUpdate);
                foreach (var (x, y) in raycast)
                {
                    if ((x, y) != (endCell.gridX, endCell.gridY))
                    {
                        grid[x, y] = Math.Max(0.0f, grid[x, y] - freeUpdate);
                        grid[x, y] = grid[x,y] * decayRate + 0.5f*(1-decayRate);
                    }
                }

                grid[endCell.gridX, endCell.gridY] = grid[endCell.gridX, endCell.gridY] * decayRate + 0.5f * (1 - decayRate);
            }

            int width = 160;
            int height = 160;
            WriteableBitmap bitmap = new WriteableBitmap(
                width, height, 96, 96, PixelFormats.Bgra32, null);

            // Assume your grid is float[,] grid, 0 = free, 1 = occupied
            bitmap.Lock();

            unsafe
            {
                IntPtr pBackBuffer = bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        byte val = (byte)(grid[x, y] * 255); // 0..255
                                                             // BGRA32: Blue, Green, Red, Alpha
                        int color_data = val << 16 | val << 8 | val | (255 << 24);

                        *((int*)(pBackBuffer + y * stride + x * 4)) = color_data;
                    }
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();


            string filePath = "grid_debug.png";

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }

            System.Diagnostics.Debug.WriteLine($"Saved grid image to {filePath}");
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
