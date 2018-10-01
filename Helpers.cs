using System;
using System.IO;

using SkiaSharp;

namespace ThumbnailCreator
{
    public static class Helpers
    {
        #region Image IO

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

        #endregion
    }
}
