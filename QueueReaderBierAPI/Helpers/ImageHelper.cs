// HELPER
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QueueReaderBierAPI.Models;


namespace QueueReaderBierAPI.Helpers
{
    public static class ImageHelper
    {
        public static Stream AddTextToImage(Stream imageStream, List<Text> texts)
        {
            Image image = Image.FromStream(imageStream);
            Bitmap b = new Bitmap(image);
            Graphics graphics = Graphics.FromImage(b);

            foreach (Text text in texts)
            {
                graphics.DrawString(text.text, SystemFonts.DefaultFont, Brushes.Black, text.x, text.y);
            }

            Stream ms = new MemoryStream();
            b.Save(ms, ImageFormat.Png);

            // If you're going to read from the stream, you may need to reset the position to the start
            ms.Position = 0;

            return ms;
        }
    }
}

