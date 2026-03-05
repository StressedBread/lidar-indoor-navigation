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
        private int gridResolution = 50;
        private float freeUpdate = 0.1f;
        private float occupiedUpdate = 0.2f;
        private float decayRate = 0.98f;
        public static int gridCenter = 100;

        public static float[,] Grid { get; } = new float[200, 200];

        private readonly object _lock = new();
        private List<(double x, double y)> _pendingPoints = new();
        private bool _dirty = false;

        public void EnqueueScan(List<(double x, double y)> points)
        {
            lock (_lock)
            {
                _pendingPoints = points.ToList();
                _dirty = true;
            }
        }

        public void StartBackgroundProcessing(CancellationToken token)
        {
            Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    List<(double x, double y)>? points = null;

                    lock (_lock)
                    {
                        if (_dirty)
                        {
                            points = _pendingPoints;
                            _dirty = false;
                        }
                    }

                    if (points != null)
                        ProcessScan(points);
                    else
                        Thread.Sleep(10);
                }
            }, token);
        }

        private void ProcessScan(List<(double x, double y)> points)
        {
            for (int x = 0; x < 200; x++)
                for (int y = 0; y < 200; y++)
                    Grid[x, y] *= decayRate;

            foreach (var (cx, cy) in points)
            {
                double angle = Math.Atan2(cx, cy) * 180.0 / Math.PI;
                if (Math.Abs(angle) > 120.0) continue;

                int gx = Math.Clamp((int)Math.Floor(gridCenter + cx / gridResolution), 0, 199);
                int gy = Math.Clamp((int)Math.Floor(gridCenter - cy / gridResolution), 0, 199);

                foreach (var (x, y) in BresenhamLine(gridCenter, gridCenter, gx, gy))
                {
                    if (x == gx && y == gy)
                        Grid[x, y] = Math.Min(1.0f, Grid[x, y] + occupiedUpdate);
                    else
                        Grid[x, y] = Math.Max(0.0f, Grid[x, y] - freeUpdate);
                }
            }
        }

        public static List<(int x, int y)> BresenhamLine(int x0, int y0, int x1, int y1)
        {
            var cells = new List<(int x, int y)>();
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                cells.Add((x0, y0));
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
            return cells;
        }
    }
}
