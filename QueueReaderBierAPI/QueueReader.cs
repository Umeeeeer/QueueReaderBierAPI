using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using QueueReaderBierAPI.Models;

namespace QueueReaderBierAPI
{
    public static class QueueReader
    {
        [FunctionName("QueueReader")]
        public static async void RunAsync([QueueTrigger("bierapi-queue", Connection = "QueueString")]string myQueueItem, TraceWriter log)
        {
            try
            {
                log.Info("Queue triggered");

                //Queuemessage van json omzetten naar object
                QueueStorageMessage queueMessage = JsonConvert.DeserializeObject<QueueStorageMessage>(myQueueItem);

                //Azuremaps key ophalen uit local.settings.json
                string azuremapskey = Environment.GetEnvironmentVariable("AzuremapsKey");

                using (var client = new HttpClient())
                {
                    // Retrieve storage account from connection string.
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("QueueString"));

                    // Create the blob client.
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    // Retrieve a reference to a container.
                    CloudBlobContainer container = blobClient.GetContainerReference(queueMessage.BlobContainerReference);

                    // Create the container if it doesn't already exist.
                    await container.CreateIfNotExistsAsync();

                    // create a blob in the path of the <container>/email/guid
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(queueMessage.BlobName);

                    var url = String.Format("https://atlas.microsoft.com/map/static/png?subscription-key={0}&api-version=1.0&center={1},{2}", azuremapskey, queueMessage.Longtitude, queueMessage.Latitude);
                    client.BaseAddress = new Uri(url);
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        log.Info("Azure maps ophalen gelukt");
                        Helpers.WeatherHelper weatherHelper = new Helpers.WeatherHelper();
                        System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync();

                        using (var client2 = new HttpClient())
                        {
                            var url2 = String.Format("https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&APPID=77af417e896786a3d0ef59eac07b46da", queueMessage.Latitude, queueMessage.Longtitude);
                            client2.BaseAddress = new Uri(url2);
                            HttpResponseMessage response2 = await client2.GetAsync(url2);

                            if (response2.IsSuccessStatusCode)
                            {
                                string json = await response2.Content.ReadAsStringAsync();
                                dynamic jsonobject = JsonConvert.DeserializeObject<dynamic>(json);

                                if(jsonobject.cod == 401)
                                {
                                    log.Info("API KEY weathermapsapi ongeldig");
                                }

                                else
                                {
                                    log.Info("API KEY IS GELDIG");
                                    string temp_min = (string)jsonobject.main.temp_min;
                                    string temp_max = (string)jsonobject.main.temp_max;
                                    string temp = (string)jsonobject.main.temp;

                                    if (true)
                                    {
                                        responseStream = weatherHelper.AddTextToImage(responseStream, (String.Format("Min: {0} Gem: {1} Max: {2}", temp_min, temp, temp_max), (10, 20)), ("Hier wordt GEEN bier aangeraden!", (10, 40)));

                                    }

                                    else
                                    {
                                        responseStream = weatherHelper.AddTextToImage(responseStream, (String.Format("Min: {0} Gem: {1} Max: {2}", temp_min, temp, temp_max), (10, 20)), ("Hier wordt bier aangeraden!", (10, 40)));

                                    }
                                }


                            }

                            else
                            {
                                responseStream = weatherHelper.AddTextToImage(responseStream, ("Op dit moment kan de weerdata niet worden opgehaald, probeer het later opnieuw!", (10, 20)));
                            }
                        }

                        //Upload retrieved image to blobstorage
                        log.Info("Uploaden van blob");
                            await blockBlob.UploadFromStreamAsync(responseStream);

                        log.Info("Image retrieved from azuremaps and uploaded to blob succesfully");
                    }

                    else
                    {
                        try
                        {
                            HttpWebRequest errorimagerequest = (HttpWebRequest)WebRequest.Create(Environment.GetEnvironmentVariable("ErrorImageLink"));
                            HttpWebResponse errorimageresponse = (HttpWebResponse)errorimagerequest.GetResponse();
                            Stream inputStream = errorimageresponse.GetResponseStream();
                            await blockBlob.UploadFromStreamAsync(inputStream);

                            log.Info("Could not retrieve image from azuremaps, uploaded error image instead");
                        }

                        catch { log.Info("Error retrieving azure maps image and also error retrieving error image"); }
                    }
                }
            }

            catch
            {
                log.Error("try catch failed / could not parse jsonstring to object");
            }

        }
    }
}
