using Newtonsoft.Json;
using QueueReaderBierAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.
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
        public async System.Threading.Tasks.Task<Stream> PaintWeatherAsync(System.IO.Stream image, QueueStorageMessage message)
        {
            using (var client = new HttpClient())
            {
                var url = String.Format("http://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}", message.Latitude, message.Longtitude);
                client.BaseAddress = new Uri(url);
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic jsonobject = JsonConvert.DeserializeObject<dynamic>(json);
                    double temp_min = (double)jsonobject.main.temp_min - 273.15;
                    double temp_max = (double)jsonobject.main.temp_max - 273.15;
                    double temp = (double)jsonobject.main.temp - 273.15;
                    if(temp < 15)
                    {
                        image = AddTextToImage(image, (String.Format("Min: {0} Gem: {1} Max: {2}", temp_min, temp, temp_max), (10, 20)), ("Hier wordt GEEN bier aangeraden!", (10, 40)));

                    }

                    else
                    {
                        image = AddTextToImage(image, (String.Format("Min: {0} Gem: {1} Max: {2}", temp_min, temp, temp_max), (10, 20)), ("Hier wordt bier aangeraden!", (10, 40)));

                    }
                }

                else
                {
                    image = AddTextToImage(image, ("Op dit moment kan de weerdata niet worden opgehaald, probeer het later opnieuw!", (10, 20)));
                }
            }
            return image;
        }

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
