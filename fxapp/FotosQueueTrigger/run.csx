#r "Microsoft.WindowsAzure.Storage"
#r "System.Drawing"
#r "Newtonsoft.Json"

#load "Index.csx"

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ImageResizer;

public static async Task Run(string myQueueItem, TraceWriter log)
{

    log.Info($"trigger function processed: {myQueueItem}");

    // blob conn string
    string connString = Environment.GetEnvironmentVariable("FOTOS_BLOBCONN");

    string cogApiKey = Environment.GetEnvironmentVariable("FOTOS_COGAPIKEY");
    string cogApiUrlBase = Environment.GetEnvironmentVariable("FOTOS_COGAPIURL");

    string schApiKey = Environment.GetEnvironmentVariable("FOTOS_SCHAPIKEY");
    string schAcct = Environment.GetEnvironmentVariable("FOTOS_SCHACCT");
    string schIndex = Environment.GetEnvironmentVariable("FOTOS_SCHINDEX");

    // for secondary site, put index reverse way
    bool isSecondary = ( (Environment.GetEnvironmentVariable("FOTOS_ISSECONDARY") ?? "false" ) == "true" );
    log.Info($"isSecondary = {isSecondary}");

    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connString);
    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

    string blobContainer = Environment.GetEnvironmentVariable("FOTOS_BLOBCONT");
    string blobUri = storageAccount.BlobStorageUri.PrimaryUri.AbsoluteUri;
    string blobSecUri = storageAccount.BlobStorageUri.SecondaryUri.AbsoluteUri;
    log.Info($"blobUri = {blobUri}, {blobSecUri}");

    CloudBlobContainer container = blobClient.GetContainerReference(blobContainer);

    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
    CloudTable table = tableClient.GetTableReference("fotoslog");
    table.CreateIfNotExists();

    string[] items = myQueueItem.Split(';');
    foreach (string item in items)
    {
        if (item == "") continue;
        log.Info($"queue item = {item}");

        DateTime dtNow = DateTime.Now;

        SearchIndexEntity tableLog = new SearchIndexEntity($"{dtNow.Year}-{dtNow.Month}-{dtNow.Day}", $"{DateTime.Now.Ticks}");
        tableLog.status = "start";
        tableLog.uri = $"{blobUri}images/{item}";
        tableLog.log = "";

        TableOperation insertOperation = TableOperation.Insert(tableLog);
        table.Execute(insertOperation);

        // get original image
        CloudBlockBlob blob = container.GetBlockBlobReference(item);
        MemoryStream image = new MemoryStream();
        blob.DownloadToStream(image);

        // resize image
        image.Position = 0;
        MemoryStream imageSmall = new MemoryStream();
        var imageBuilder = ImageResizer.ImageBuilder.Current;
        var size = imageDimensionsTable[ImageSize.ExtraSmall];

        imageBuilder.Build(
            image, imageSmall,
            new ResizeSettings(size.Width, size.Height, FitMode.Max, null), false);

        // save resized image
        imageSmall.Position = 0;
        CloudBlobContainer containerSm = blobClient.GetContainerReference(blobContainer);
        CloudBlockBlob blobSm = containerSm.GetBlockBlobReference("sm/" + item);
        blobSm.Properties.ContentType = "image/jpeg";
        await blobSm.UploadFromStreamAsync(imageSmall);


        // get tags & description
        image.Position = 0;

        dynamic json = null;
        using (var httpClient = new HttpClient())
        {
            httpClient.BaseAddress = new Uri(cogApiUrlBase);
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", cogApiKey);
            using (HttpContent content = new StreamContent(image))
            {
                //get response
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");
                var uri = "https://api.projectoxford.ai/vision/v1.0/analyze?visualFeatures=Description";
                var response = await httpClient.PostAsync(uri, content);

                //string responseBody = await response.Content.ReadAsStringAsync();
                //dynamic json = JObject.Parse(responseBody);
                json = await response.Content.ReadAsAsync<object>();
            }
        }

        // add to search index
        var index = new
        {
            value = new[] {
            new SearchIndex(){
                search_action = "upload",
                id = json.requestId,
                uri = isSecondary ? $"{blobSecUri}images/{item}" : $"{blobUri}images/{item}",
                uri2 = isSecondary ?  $"{blobUri}images/{item}" : $"{blobSecUri}images/{item}",
                thumbnail = isSecondary ? $"{blobSecUri}images/sm/{item}" : $"{blobUri}images/sm/{item}",
                thumbnail2 = isSecondary ? $"{blobUri}images/sm/{item}" : $"{blobSecUri}images/sm/{item}",
                tags = json.description.tags,
                caption = json.description.captions[0].text
                }
            }
        };

        
        string payload = Newtonsoft.Json.JsonConvert.SerializeObject(index);

        log.Info("index: " + payload);

        using (var httpClient = new HttpClient())
        {
            httpClient.BaseAddress = new Uri($"https://{schAcct}.search.windows.net");
            httpClient.DefaultRequestHeaders.Add("api-key", schApiKey);


            using (HttpContent content = new StringContent(payload))
            {
                //get response
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                var uri = $"https://{schAcct}.search.windows.net/indexes/{schIndex}/docs/index?api-version=2016-09-01";
                var response = await httpClient.PostAsync(uri, content);

                string resp = await response.Content.ReadAsStringAsync();
                log.Info("search response: " + resp);
            }
        }

        // save to each json obj to log file for recover or backup
        CloudBlockBlob blobLog = containerSm.GetBlockBlobReference($"log/image-index_{index.value[0].id}.log");
        blobLog.Properties.ContentType = "text/plain";

        string indexval = Newtonsoft.Json.JsonConvert.SerializeObject(index.value[0]);
        await blobLog.UploadTextAsync(indexval);
        /**/

        // save log to table
        // constraint (PK + RK) = unique
        tableLog = new SearchIndexEntity($"{dtNow.Year}-{dtNow.Month}-{dtNow.Day}", $"{DateTime.Now.Ticks}");
        tableLog.status = "completed";
        tableLog.uri = $"{blobUri}images/{item}";
        tableLog.log = indexval;

        insertOperation = TableOperation.Insert(tableLog);
        table.Execute(insertOperation);
        /**/
    }
}

#region Helpers

public enum ImageSize
{
    ExtraSmall, Small, Medium
}

private static Dictionary<ImageSize, Size> imageDimensionsTable = new Dictionary<ImageSize, Size>()
{
    { ImageSize.ExtraSmall, new Size(320, 200) },
    { ImageSize.Small, new Size(640, 400) },
    { ImageSize.Medium, new Size(800, 600) }
};

private static ImageFormat ScaleImage(Stream blobInput, Stream output, ImageSize imageSize)
{
    ImageFormat imageFormat;

    var size = imageDimensionsTable[imageSize];

    blobInput.Position = 0;

    using (var img = System.Drawing.Image.FromStream(blobInput))
    {
        var widthRatio = (double)size.Width / (double)img.Width;
        var heightRatio = (double)size.Height / (double)img.Height;
        var minAspectRatio = Math.Min(widthRatio, heightRatio);
        if (minAspectRatio > 1)
        {
            size.Width = img.Width;
            size.Width = img.Height;
        }
        else
        {
            size.Width = (int)(img.Width * minAspectRatio);
            size.Height = (int)(img.Height * minAspectRatio);
        }

        using (Bitmap bitmap = new Bitmap(img, size))
        {
            bitmap.Save(output, img.RawFormat);
            imageFormat = img.RawFormat;
        }
    }

    return imageFormat;
}

#endregion