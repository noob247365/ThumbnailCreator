using System;
using System.IO;

using SkiaSharp;

namespace ThumbnailCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a simple image
            using (var surface = SKSurface.Create(new SKImageInfo(640, 480, SKColorType.Rgba8888)))
            {
                // Clear the canvas
                var canvas = surface.Canvas;
                var bounds = canvas.DeviceClipBounds;
                canvas.Clear(new SKColor(0, 0, 0, 0));

                // Draw a circle at the center
                float thickness = 10.0f;
                canvas.DrawCircle(
                    bounds.Width * 0.5f, bounds.Height * 0.5f,
                    (Math.Min(bounds.Width, bounds.Height) - thickness) * 0.5f,
                    new SKPaint
                    {
                        Color = new SKColor(255, 0, 0, 255),
                        IsAntialias = false, // Personal preference
                         StrokeWidth = thickness,
                        Style = SKPaintStyle.Stroke
                    }
                );

                // Save to an output image
                using (var image = surface.Snapshot())
                    image.SaveToFile(Path.Combine("out", "Test.png"));
            }
        }
    }
}
