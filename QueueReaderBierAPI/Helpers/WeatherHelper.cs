using Newtonsoft.Json;
using QueueReaderBierAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.IO;
using SixLabors.Fonts;

// pre-release packages!
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;


namespace QueueReaderBierAPI.Helpers
{
    class WeatherHelper
    {
        public Stream AddTextToImage(Stream imageStream, params (string text, (float x, float y) position)[] texts)
        {
            var memoryStream = new MemoryStream();

            var foto = Image.Load(imageStream);

            foto
                .Clone(img =>
                {
                    foreach (var (text, (x, y)) in texts)
                    {
                        img.DrawText(text, SystemFonts.CreateFont("Verdana", 24), Rgba32.OrangeRed, new PointF(x, y));
                    }
                })
                .SaveAsPng(memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
