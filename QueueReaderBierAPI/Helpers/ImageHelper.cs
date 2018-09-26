// HELPER

using System.Collections.Generic;
using System.IO;
using QueueReaderBierAPI.Models;
using SixLabors.Fonts;

// pre-release packages!
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace QueueReaderBierAPI.Helpers
{
    public static class ImageHelper
    {
        public static Stream AddTextToImage(Stream imageStream, List<Text> texts)
        {
            var memoryStream = new MemoryStream();

            var image = Image.Load(imageStream);

            image
                .Clone(img =>
                {
                    foreach (Text text in texts)
                    {
                        img.DrawText(text.text, SystemFonts.CreateFont("Verdana", 24), Rgba32.OrangeRed, new PointF(text.x, text.y));
                    }
                })
                .SaveAsPng(memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}

