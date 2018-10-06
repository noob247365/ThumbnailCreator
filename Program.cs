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

                    // Configure padding
                    float paddingVertical = info.Height * 0.1f;
                    float paddingHorizontal = info.Width * 0.05f;
                    float paddingLogoBottom = info.Height * 0.05f;
                    float paddingTextTop = info.Height * 0.1f;

                    // Draw the game logo
                    using (var logo = Helpers.LoadImage("res/Games/Overwatch.png".ToOS()))
                    {
                        // Calculate the scale for the logo
                        var bottomCenter = new SKPoint(info.Width * 0.5f, (info.Height - paddingLogoBottom) * 0.5f);
                        float logoScale = Math.Min(
                            (bottomCenter.X - paddingHorizontal) / logo.Width * 2.0f,
                            (bottomCenter.Y - paddingVertical) / logo.Height
                        );

                        // Draw the logo
                        canvas.DrawImage(logo, new SKRect(
                            bottomCenter.X - logoScale * logo.Width * 0.5f,
                            bottomCenter.Y - logoScale * logo.Height,
                            bottomCenter.X + logoScale * logo.Width * 0.5f,
                            bottomCenter.Y
                        ));
                    }

                    // Draw some text at the center
                    using (var minecrafter3 = Helpers.LoadFont("res/Fonts/Minecrafter_3/Minecrafter_3.ttf".ToOS()))
                    {
                        // Determine the scale for the text
                        var paint = new SKPaint
                        {
                            Color = new SKColor(255, 255, 255, 255),
                            IsAntialias = false,
                            Style = SKPaintStyle.Fill,
                            TextSize = 128.0f, // Default is 128 font
                            Typeface = minecrafter3
                        };
                        var topCenter = new SKPoint(info.Width * 0.5f, (info.Height + paddingTextTop) * 0.5f);
                        string text = "#1 Sample Text Here";
                        var size = paint.MeasureTextExt(text, true);
                        paint.TextSize *= Math.Min(1.0f, Math.Min(
                            (topCenter.X - paddingHorizontal) / size.Width * 2.0f,
                            (info.Height - topCenter.Y - paddingVertical) / size.Height
                        ));

                        // Draw the text
                        canvas.DrawTextExt(
                            text, topCenter.X, topCenter.Y,
                            paint, HAlign.Center, VAlign.Top, true
                        );
                    }

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
