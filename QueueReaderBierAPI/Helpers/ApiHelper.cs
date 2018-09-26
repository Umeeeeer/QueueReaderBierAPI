using QueueReaderBierAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QueueReaderBierAPI.Helpers
{
    class ApiHelper
    {
        public static async Task<HttpResponseMessage> GetMapFromAzureMapsAsync(QueueStorageMessage message)
        {
            //Azuremaps key ophalen uit local.settings.json
            string azuremapskey = Environment.GetEnvironmentVariable("AzuremapsKey");

            using (var client = new HttpClient())
            {
                var url = String.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", azuremapskey, message.Longtitude, message.Latitude);
                client.BaseAddress = new Uri(url);
                HttpResponseMessage response = await client.GetAsync(url);
                return response;
            }
        }

        public static async Task<HttpResponseMessage> GetWeatherAsync(QueueStorageMessage message)
        {
            using (var client = new HttpClient())
            {
                var url = String.Format("https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&APPID=77af417e896786a3d0ef59eac07b46da", message.Latitude, message.Longtitude);
                client.BaseAddress = new Uri(url);
                HttpResponseMessage response = await client.GetAsync(url);
                return response;
            }
        }
    }
}
