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
        private double freeUpdate = 0.1;
        private double occupiedUpdate = 0.2;
        private double decayRate = 0.9;
        public static int gridCenter = 100;

        public static double[,] Grid { get; } = new double[201, 201];

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
            for (int x = 0; x < 201; x++)
                for (int y = 0; y < 201; y++)
                    Grid[x, y] *= decayRate;

            foreach (var (cx, cy) in points)
            {
                double angle = Math.Atan2(cx, cy) * 180.0 / Math.PI;
                if (Math.Abs(angle) > 120.0) continue;

                int gx = Math.Clamp((int)Math.Floor(gridCenter + cx / gridResolution), 0, 200);
                int gy = Math.Clamp((int)Math.Floor(gridCenter - cy / gridResolution), 0, 200);

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

        /*public BitmapSource RenderGrid()
        {
            int size = 201;
            int scale = 4;
            int sectors = 20;
            double span = 240;
            double sectorWidth = span / sectors;

            using var bitmap = new SKBitmap(size * scale, size * scale);
            using var canvas = new SKCanvas(bitmap);

            canvas.Clear(SKColors.White);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float value = (float)Grid[x, y];
                    byte gray = (byte)(255 - value * 255);
                    var color = new SKColor(gray, gray, gray);

                    using var paint = new SKPaint { Color = color };
                    canvas.DrawRect(x * scale, y * scale, scale, scale, paint);
                }
            }

            using var sectorPaint = new SKPaint
            {
                Color = SKColors.Blue.WithAlpha(120),
                StrokeWidth = 1f,
                IsAntialias = true,
                IsStroke = true,
            };

            float cx = gridCenter * scale;
            float cy = gridCenter * scale;
            float lineLen = size * scale * 0.5f;

            for (int i = 0; i <= sectors; i++)
            {
                double angleDeg = -120 + i * sectorWidth;
                double angleRad = (angleDeg - 90) * Math.PI / 180;
                float ex = cx + (float)(lineLen * Math.Cos(angleRad));
                float ey = cy + (float)(lineLen * Math.Sin(angleRad));
                canvas.DrawLine(cx, cy, ex, ey, sectorPaint);
            }

            using var robotPaint = new SKPaint { Color = SKColors.Red };

            canvas.DrawCircle(gridCenter * scale, gridCenter * scale, scale * 2, robotPaint);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }*/

        /*public BitmapSource RenderGrid()
        {
            int size = 201;
            int scale = 8;
            int sectors = 20;
            double span = 240;
            double sectorWidth = span / sectors;

            int width = size * scale;
            int height = size * scale;

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            // Draw grid
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float value = (float)Grid[x, y];
                    byte gray = (byte)(255 - value * 255);
                    var color = new SKColor(gray, gray, gray);
                    using var paint = new SKPaint { Color = color };
                    canvas.DrawRect(x * scale, y * scale, scale, scale, paint);
                }
            }

            // Draw sector lines
            using var sectorPaint = new SKPaint
            {
                Color = SKColors.Blue.WithAlpha(120),
                StrokeWidth = 1f,
                IsAntialias = true,
                IsStroke = true,
            };

            float cx = gridCenter * scale;
            float cy = gridCenter * scale;
            float lineLen = size * scale * 0.5f;

            for (int i = 0; i <= sectors; i++)
            {
                double angleDeg = -120 + i * sectorWidth;
                double angleRad = (angleDeg - 90) * Math.PI / 180;
                float ex = cx + (float)(lineLen * Math.Cos(angleRad));
                float ey = cy + (float)(lineLen * Math.Sin(angleRad));
                canvas.DrawLine(cx, cy, ex, ey, sectorPaint);
            }

            // Draw robot
            using var robotPaint = new SKPaint { Color = SKColors.Red };
            canvas.DrawCircle(gridCenter * scale, gridCenter * scale, scale * 2, robotPaint);

            // Convert directly to WriteableBitmap (much faster!)
            var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            writeableBitmap.Lock();

            // Copy pixels directly
            var pixels = bitmap.GetPixels();
            unsafe
            {
                Buffer.MemoryCopy(
                    pixels.ToPointer(),
                    writeableBitmap.BackBuffer.ToPointer(),
                    writeableBitmap.BackBufferStride * height,
                    bitmap.ByteCount
                );
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            writeableBitmap.Unlock();
            writeableBitmap.Freeze();

            return writeableBitmap;
        }*/

        public BitmapSource RenderGrid(double radius)
        {
            int size = 201;
            int scale = 8;
            int sectors = 20;
            double span = 240;
            double sectorWidth = span / sectors;
            int width = size * scale;
            int height = size * scale;
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            // Draw grid
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float value = (float)Grid[x, y];
                    byte gray = (byte)(255 - value * 255);
                    var color = new SKColor(gray, gray, gray);
                    using var paint = new SKPaint { Color = color };
                    canvas.DrawRect(x * scale, y * scale, scale, scale, paint);
                }
            }

            // Draw sector lines
            using var sectorPaint = new SKPaint
            {
                Color = SKColors.Blue.WithAlpha(120),
                StrokeWidth = 1f,
                IsAntialias = true,
                IsStroke = true,
            };
            float cx = gridCenter * scale;
            float cy = gridCenter * scale;
            float lineLen = size * scale * 0.5f;

            for (int i = 0; i <= sectors; i++)
            {
                double angleDeg = -120 + i * sectorWidth;
                double angleRad = (angleDeg - 90) * Math.PI / 180;
                float ex = cx + (float)(lineLen * Math.Cos(angleRad));
                float ey = cy + (float)(lineLen * Math.Sin(angleRad));
                canvas.DrawLine(cx, cy, ex, ey, sectorPaint);
            }

            // Draw radius circle using passed parameter
            using var radiusPaint = new SKPaint
            {
                Color = SKColors.Green.WithAlpha(100),
                StrokeWidth = 2f,
                IsAntialias = true,
                IsStroke = true,
                Style = SKPaintStyle.Stroke
            };
            float radiusPixels = (float)radius * scale;
            canvas.DrawCircle(cx, cy, radiusPixels, radiusPaint);

            // Draw robot
            using var robotPaint = new SKPaint { Color = SKColors.Red };
            canvas.DrawCircle(gridCenter * scale, gridCenter * scale, scale * 2, robotPaint);

            // Convert directly to WriteableBitmap
            var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            writeableBitmap.Lock();

            var pixels = bitmap.GetPixels();
            unsafe
            {
                Buffer.MemoryCopy(
                    pixels.ToPointer(),
                    writeableBitmap.BackBuffer.ToPointer(),
                    writeableBitmap.BackBufferStride * height,
                    bitmap.ByteCount
                );
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            writeableBitmap.Unlock();
            writeableBitmap.Freeze();

            return writeableBitmap;
        }
    }
}
