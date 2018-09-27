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
using QueueReaderBierAPI.Helpers;
using System.Collections.Generic;

namespace QueueReaderBierAPI
{
    public static class QueueReaer
    {
        [FunctionName("QueueReader")]
        public static async void RunAsync([QueueTrigger("bierapi-queue", Connection = "QueueString")]string myQueueItem, TraceWriter log)
        {
            try
            {
                //Queuemessage van json omzetten naar object
                QueueStorageMessage queueMessage = JsonConvert.DeserializeObject<QueueStorageMessage>(myQueueItem);

                // Retrieve storage account from connection string.
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString") + ";EndpointSuffix=core.windows.net");

                // Create the blob client.
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve a reference to a container.
                CloudBlobContainer container = blobClient.GetContainerReference(queueMessage.BlobContainerReference);

                // Create the container if it doesn't already exist.
                await container.CreateIfNotExistsAsync();

                BlobContainerPermissions permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };

                // create a blob in the path of the <container>/email/guid
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(queueMessage.BlobName);

                //Azuremaps image request sturen
                HttpResponseMessage response = await ApiHelper.GetMapFromAzureMapsAsync(queueMessage);

                //Als er een map opgehaald kon worden
                if(response.IsSuccessStatusCode)
                {
                    //De foto uitlezen in een stream
                    System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync();

                    //Het weer opvragen
                    HttpResponseMessage responseWeather = await ApiHelper.GetWeatherAsync(queueMessage);

                    if(responseWeather.IsSuccessStatusCode)
                    {
                        //Json uitlezen en in object zetten
                        string json = await responseWeather.Content.ReadAsStringAsync();
                        dynamic jsonobject = JsonConvert.DeserializeObject<dynamic>(json);

                        //Als de API key ongeldig blijkt te zijn
                        if (jsonobject.cod == 401)
                        {
                            log.Info("API KEY weathermapsapi ongeldig");
                        }

                        //Als er wel een geldige api key opgegeven was
                        else
                        {
                            List<Text> texts = new List<Text>();
                            double temp_min = (double)jsonobject.main.temp_min - 273;
                            double temp_max = (double)jsonobject.main.temp_max - 273;
                            double temp = (double)jsonobject.main.temp - 273;
                            Text text1 = new Text(String.Format("Min temp: {0}", temp_min), 10, 20);
                            Text text2 = new Text(String.Format("Gem temp: {0}", temp), 10, 50);
                            Text text3 = new Text(String.Format("Max temp: {0}", temp_max), 10, 80);
                            texts.Add(text1);
                            texts.Add(text2);
                            texts.Add(text3);

                            if (temp > 15)
                            {
                                Text text4 = new Text("Er wordt aangeraden om bier te drinken!", 10, 110);
                                texts.Add(text4);
                            }

                            else
                            {
                                Text text4 = new Text("Er wordt aangeraden om GEEN bier te drinken!", 10, 110);
                                texts.Add(text4);
                            }

                            Stream responseStreamFoto = ImageHelper.AddTextToImage(responseStream, texts);
                            await blockBlob.UploadFromStreamAsync(responseStreamFoto);
                        }
                    }

                    //Als er geen weer opgehaald kon worden
                    else
                    {
                        List<Text> texts = new List<Text>();
                        Text text = new Text("Er kon geen weerdata opgehaald worden, probeer het later opnieuw!", 10, 20);
                        texts.Add(text);
                        Stream responseStreamFoto = ImageHelper.AddTextToImage(responseStream, texts);
                        await blockBlob.UploadFromStreamAsync(responseStreamFoto);
                    }
                }
                
                //Als er geen map opgehaald kon worden
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

            catch
            {
                log.Error("exception occured");
            }
        }
    }
}


