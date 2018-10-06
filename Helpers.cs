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

        /// <summary>
        /// Draw lines of text
        /// </summary>
        /// <param name="canvas">The SKCanvas to draw on</param>
        /// <param name="lines">Lines of text to display</param>
        /// <param name="xpos">The x coordinate of the text (meaning is dependent on <paramref name="halign"/>)</param>
        /// <param name="ypos">The y coordinate of the text (meaning is dependent on <paramref name="valign"/>)</param>
        /// <param name="paint">The SKPaint controlling drawing</param>
        /// <param name="halign">How to horizontally align the text (default: Left)</param>
        /// <param name="valign">How to vertically align the text (default: Top)</param>
        /// <param name="allCapsFont">If the font is in all caps; will ignore space below baseline (default: false)</param>
        public static void DrawTextExt(this SKCanvas canvas, string[] lines, float xpos,
            float ypos, SKPaint paint, HAlign halign = HAlign.Left, VAlign valign = VAlign.Top,
            bool allCapsFont = false)
        {
            // Ignore empty lines
            if (lines == null || lines.Length == 0)
                return;

            // Determine the maximum size
            float totalHeight = MeasureTextExt(
                paint, lines, out float lineHeight, out float lineGap, allCapsFont
            ).Height;

            // Save the previous alignment
            var prevAlign = paint.TextAlign;
            try
            {
                // Handle the alignment
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
                float y = ypos - paint.FontMetrics.Top;
                switch (valign)
                {
                    case VAlign.Middle:
                        y -= totalHeight * 0.5f;
                        break;
                    case VAlign.Bottom:
                        y -= totalHeight;
                        break;
                }

                // Draw the text
                foreach (string line in lines)
                {
                    canvas.DrawText(line, xpos, y, paint);
                    y += lineHeight + lineGap;
                }
            }
            finally
            {
                // Restore the original text alignment
                paint.TextAlign = prevAlign;
            }
        }

        /// <summary>
        /// Draw a string of text (supports new line characters)
        /// </summary>
        /// <param name="canvas">The SKCanvas to draw on</param>
        /// <param name="text">Text string of text to display</param>
        /// <param name="xpos">The x coordinate of the text (meaning is dependent on <paramref name="halign"/>)</param>
        /// <param name="ypos">The y coordinate of the text (meaning is dependent on <paramref name="valign"/>)</param>
        /// <param name="paint">The SKPaint controlling drawing</param>
        /// <param name="halign">How to horizontally align the text (default: Left)</param>
        /// <param name="valign">How to vertically align the text (default: Top)</param>
        /// <param name="allCapsFont">If the font is in all caps; will ignore space below baseline (default: false)</param>
        public static void DrawTextExt(this SKCanvas canvas, string text, float xpos,
            float ypos, SKPaint paint, HAlign halign = HAlign.Left, VAlign valign = VAlign.Top,
            bool allCapsFont = false)
        {
            // Ignore blank text
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Draw after splitting into lines
            DrawTextExt(canvas, GetLines(text), xpos, ypos, paint, halign, valign, allCapsFont);
        }

        /// <summary>
        /// Split a string of text into lines
        /// </summary>
        /// <param name="text">Full text string</param>
        /// <returns>An array of lines</returns>
        public static string[] GetLines(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return new string[] { };
            return text.Replace("\r", "").Split('\n');
        }

        /// <summary>
        /// Measure the full bounds of a text string (supports new line characters)
        /// </summary>
        /// <param name="paint">The SKPaint controlling drawing</param>
        /// <param name="lines">Lines of text to measure</param>
        /// <param name="lineHeight">How tall one line of text is</param>
        /// <param name="lineGap">The gap between each line of text</param>
        /// <param name="allCapsFont">If the font is in all caps; will ignore space below baseline (default: false)</param>
        /// <returns>The full bounds of the text string</returns>
        private static SKSize MeasureTextExt(this SKPaint paint, string[] lines,
            out float lineHeight, out float lineGap, bool allCapsFont = false)
        {
            lineHeight = (allCapsFont ? 0 : paint.FontMetrics.Bottom) - paint.FontMetrics.Top;
            lineGap = paint.FontMetrics.Leading + (allCapsFont ? paint.FontMetrics.Bottom : 0);
            if (lines.Length == 0)
                return new SKSize(0, 0);
            return new SKSize(
                lines.Max(line => paint.MeasureText(line)),
                lineHeight * lines.Length + lineGap * (lines.Length - 1)
            );
        }

        /// <summary>
        /// Measure the full bounds of lines of text
        /// </summary>
        /// <param name="paint">The SKPaint controlling drawing</param>
        /// <param name="lines">Lines of text to measure</param>
        /// <param name="allCapsFont">If the font is in all caps; will ignore space below baseline (default: false)</param>
        /// <returns>The full bounds of the text string</returns>
        public static SKSize MeasureTextExt(this SKPaint paint, string[] lines, bool allCapsFont = false)
        {
            return MeasureTextExt(paint, lines, out float _, out float _, allCapsFont);
        }

        /// <summary>
        /// Measure the full bounds of a text string (supports new line characters)
        /// </summary>
        /// <param name="paint">The SKPaint controlling drawing</param>
        /// <param name="text">Text string of text to measure</param>
        /// <param name="allCapsFont">If the font is in all caps; will ignore space below baseline (default: false)</param>
        /// <returns>The full bounds of the text string</returns>
        public static SKSize MeasureTextExt(this SKPaint paint, string text, bool allCapsFont = false)
        {
            return MeasureTextExt(paint, GetLines(text), allCapsFont);
        }

        #endregion

        #region Image IO

        /// <summary>
        /// Save an image to a file
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="fileName">The path to the output file</param>
        /// <param name="format">The image format for encoding (default: Png)</param>
        /// <param name="quality">Quality level for the image; between 0 and 100 (default: 100)</param>
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

        /// <summary>
        /// Save a surface to a file
        /// </summary>
        /// <param name="surface">The surface to save</param>
        /// <param name="fileName">The path to the output file</param>
        /// <param name="format">The image format for encoding (default: Png)</param>
        /// <param name="quality">Quality level for the image; between 0 and 100 (default: 100)</param>
        public static void SaveToFile(this SKSurface surface, string fileName,
            SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            using (var image = surface.Snapshot())
                SaveToFile(image, fileName, format, quality);
        }

        #endregion

        #region Operating system

        /// <summary>
        /// Determine if the operating system is Windows
        /// </summary>
        /// <returns>If the operating system is Windows</returns>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// The supported SKColorType for 32-bit RGBA (OS dependent)
        /// </summary>
        public static SKColorType RGBA => IsWindows ? SKColorType.Bgra8888 : SKColorType.Rgba8888;

        #endregion

        #region Path helpers

        /// <summary>
        /// Change a forward-slash path into an OS-specific path
        /// </summary>
        /// <param name="path">Forward-slash path for conversion</param>
        /// <returns>The operating specific path</returns>
        public static string ToOS(this string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        #endregion

        #region Resource helpers

        /// <summary>
        /// Load a font from a file
        /// </summary>
        /// <param name="fileName">The path to the input file</param>
        /// <returns>The loaded font</returns>
        public static SKTypeface LoadFont(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            using (var data = SKData.Create(stream))
                return SKTypeface.FromData(data);
        }

        /// <summary>
        /// Load an image from a file
        /// </summary>
        /// <param name="fileName">The path to the input file</param>
        /// <returns>The resulting image</returns>
        public static SKImage LoadImage(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            using (var data = SKData.Create(stream))
                return SKImage.FromEncodedData(data);
        }

        #endregion
    }
}
