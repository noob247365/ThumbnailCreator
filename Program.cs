using System;
using System.Diagnostics;
using System.IO;

using SkiaSharp;

namespace ThumbnailCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CreateThumbnail(args);
                Console.WriteLine("Thumbnail generated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating thumbnail: {0}", ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void CreateThumbnail(string[] args)
        {
            // Parse args
            if (args.Length < 1)
            {
                Console.WriteLine(
                    "Usage: {0} <Config-File>",
                    Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName)
                );
                return;
            }
            string configFile = args[0];
            if (!configFile.ToLower().EndsWith(".config"))
                configFile = Path.Combine("res", "Config", $"{configFile}.config");

            // Load in the requested config file
            var config = new Config(configFile);

            // Load the background image
            if (!config.Required("the background image", out string backgroundImage, "background"))
                return;
            using (var background = Helpers.LoadImage(backgroundImage.ToOS()))
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

                    // Determine the center-line
                    if (!config.Required("the center line's vertical position", out string configCenterLine, "centerline"))
                        return;
                    float centerLine = configCenterLine.ParseAsDistance(info.Height);

                    // Read padding from config
                    if (!config.Required("the padding on left/right", out string configPaddingHorizontal, "padding", "horizontal"))
                        return;
                    float paddingHorizontal = configPaddingHorizontal.ParseAsDistance(info.Width);
                    if (!config.Required("the padding below the logo", out string configPaddingLogoBottom, "padding", "logo.bottom"))
                        return;
                    float paddingLogoBottom = configPaddingLogoBottom.ParseAsDistance(info.Height);
                    if (!config.Required("the padding above the text", out string configPaddingTextTop, "padding", "text.top"))
                        return;
                    float paddingTextTop = configPaddingTextTop.ParseAsDistance(info.Height);
                    if (!config.Required("the padding on top/bottom", out string configPaddingVertical, "padding", "vertical"))
                        return;
                    float paddingVertical = configPaddingVertical.ParseAsDistance(info.Height);

                    // Draw the game logo
                    if (!config.Required("the game logo", out string logoImage, "logo"))
                        return;
                    if (!logoImage.ToLower().EndsWith(".png"))
                        logoImage = $"res/Logos/{logoImage}.png";
                    using (var logo = Helpers.LoadImage(logoImage.ToOS()))
                    {
                        // Calculate the scale for the logo
                        var bottomCenter = new SKPoint(info.Width * 0.5f, centerLine - paddingLogoBottom);
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

                    // Parse the font configuration
                    if (!config.Required("if the font displays in all caps", out string configAllCaps, "text", "all.caps"))
                        return;
                    bool allCaps = configAllCaps.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                    if (!config.Required("the font", out string fontFile, "text", "font"))
                        return;
                    if (!fontFile.ToLower().EndsWith(".ttf"))
                        fontFile = $"res/Fonts/{fontFile}/{fontFile}.ttf";
                    if (!config.Required("the maximum font size", out string configFontSize, "text", "size"))
                        return;
                    if (!configFontSize.EndsWith("px"))
                    {
                        Console.WriteLine("The font size must be in pixels");
                        return;
                    }
                    float fontSize = float.Parse(configFontSize.Substring(0, configFontSize.Length - 2));

                    // Get the video title and episode
                    if (!config.Required("the video's episode", out string configEpisode, "video", "episode"))
                        return;
                    int episode = int.Parse(configEpisode);
                    if (!config.Required("the video's title", out string title, "video", "title"))
                        return;
                    if (!config.Required("if the episode number should appear in the thumbnail", out string configIncludeEpisodeNumber, "text", "include.episode.number"))
                        return;
                    string content = bool.Parse(configIncludeEpisodeNumber) ? $"#{episode} {title}" : title;

                    // Draw some text at the center
                    using (var minecrafter3 = Helpers.LoadFont(fontFile.ToOS()))
                    {
                        // Determine the scale for the text
                        var paint = new SKPaint
                        {
                            Color = new SKColor(255, 255, 255, 255),
                            IsAntialias = false,
                            Style = SKPaintStyle.Fill,
                            TextSize = fontSize,
                            Typeface = minecrafter3
                        };
                        var topCenter = new SKPoint(info.Width * 0.5f, centerLine + paddingTextTop);
                        var size = paint.MeasureTextExt(content, allCaps);
                        paint.TextSize *= Math.Min(1.0f, Math.Min(
                            (topCenter.X - paddingHorizontal) / size.Width * 2.0f,
                            (info.Height - topCenter.Y - paddingVertical) / size.Height
                        ));

                        // Draw the text
                        canvas.DrawTextExt(
                            content, topCenter.X, topCenter.Y,
                            paint, HAlign.Center, VAlign.Top, allCaps
                        );
                    }

                    // Diagnostic marker
                    if (config.TryGet(out string configRenderCenter, "rendering", "center") &&
                            configRenderCenter.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                        canvas.DrawCircle(
                            info.Width * 0.5f, centerLine, 25.0f,
                            new SKPaint
                            {
                                Color = new SKColor(255, 0, 0, 255),
                                IsAntialias = false,
                                Style = SKPaintStyle.Fill
                            }
                        );

                    // Save to a file
                    if (!config.Required("the output directory", out string configOutputDir, "video", "output.directory"))
                        return;
                    string outputDir = Path.GetFullPath(configOutputDir.ToOS());
                    if (!Directory.Exists(outputDir))
                        Directory.CreateDirectory(outputDir);
                    surface.SaveToFile(Path.Combine(outputDir, $"Episode_{episode.ToString("D3")}.png"));
                }
            }
        }
    }
}
