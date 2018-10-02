using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using SkiaSharp;

namespace ThumbnailCreator
{
    public static class Helpers
    {
        #region Canvas helpers

        public static void DrawTextExt(this SKCanvas canvas, string text, float xpos,
            float ypos, SKPaint paint, HAlign halign = HAlign.Left, VAlign valign = VAlign.Top)
        {
            // Ignore blank text
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Split the text into lines
            var lines = text.Split('\n');

            // Determine the maximum size
            float lineHeight = paint.FontMetrics.Bottom - paint.FontMetrics.Top;
            float lineGap = paint.FontMetrics.Leading;
            float totalHeight = lineHeight * lines.Length + lineGap * (lines.Length - 1);

            // Handle the alignment
            var prevAlign = paint.TextAlign;
            try
            {
                switch (halign)
                {
                    case HAlign.Left:
                    default:
                        paint.TextAlign = SKTextAlign.Left;
                        break;
                    case HAlign.Center:
                        paint.TextAlign = SKTextAlign.Center;
                        break;
                    case HAlign.Right:
                        paint.TextAlign = SKTextAlign.Right;
                        break;
                }
                float y;
                switch (valign)
                {
                    case VAlign.Top:
                    default:
                        y = ypos;
                        break;
                    case VAlign.Middle:
                        y = ypos - totalHeight * 0.5f;
                        break;
                    case VAlign.Bottom:
                        y = ypos - totalHeight;
                        break;
                }
                y -= paint.FontMetrics.Top;

                // Draw the text
                foreach (string line in lines)
                {
                    canvas.DrawText(line, xpos, y, paint);
                    y += lineHeight + lineGap;
                }
            }
            finally
            {
                paint.TextAlign = prevAlign;
            }
        }

        #endregion

        #region Image IO

        public static SKImage LoadFromFile(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            using (var data = SKData.Create(stream))
                return SKImage.FromEncodedData(data);
        }

        public static void SaveToFile(this SKImage image, string fileName,
            SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            // Ensure the output path exists
            string fullPath = Path.GetFullPath(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Actually save to the file
            using (var stream = File.Create(fullPath))
            using (var data = image.Encode(format, quality))
                data.SaveTo(stream);
        }

        public static void SaveToFile(this SKSurface surface, string fileName,
            SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            using (var image = surface.Snapshot())
                SaveToFile(image, fileName, format, quality);
        }

        #endregion

        #region Path helpers

        public static string ToOS(this string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        #endregion

        #region Operating system

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static SKColorType RGBA => IsWindows ? SKColorType.Bgra8888 : SKColorType.Rgba8888;

        #endregion
    }
}
