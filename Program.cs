using System;
using System.IO;

using SkiaSharp;

namespace ThumbnailCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load the background image
            using (var background = Helpers.LoadImage("res/Background.png".ToOS()))
            {
                // Create a surface of matching size
                var info = new SKImageInfo(background.Width, background.Height, Helpers.RGBA);
                using (var surface = SKSurface.Create(info))
                {
                    // Clear the canvas
                    var canvas = surface.Canvas;
                    canvas.Clear(new SKColor(0, 0, 0, 0));

                    // Draw the background
                    canvas.DrawImage(background, 0.0f, 0.0f);

                    // Draw some text at the center
                    using (var minecrafter3 = Helpers.LoadFont("res/Fonts/Minecrafter_3/Minecrafter_3.ttf".ToOS()))
                        canvas.DrawTextExt(
                            "Font Test\n(with an A)", info.Width * 0.5f, info.Height * 0.5f,
                            new SKPaint
                            {
                                Color = new SKColor(255, 255, 255, 255),
                                IsAntialias = false,
                                Style = SKPaintStyle.Fill,
                                TextSize = 96.0f,
                                Typeface = minecrafter3
                            },
                            HAlign.Center, VAlign.Middle, true
                        );

                    // Diagnostic marker
                    canvas.DrawCircle(
                        info.Width * 0.5f, info.Height * 0.5f, 25.0f,
                        new SKPaint
                        {
                            Color = new SKColor(255, 0, 0, 255),
                            IsAntialias = false,
                            Style = SKPaintStyle.Fill
                        }
                    );

                    // Save the surface
                    surface.SaveToFile("out/Test.png".ToOS());
                }
            }
        }
    }
}
