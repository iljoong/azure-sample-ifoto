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

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{

    // Get request body
    //dynamic body = await req.Content.ReadAsAsync<object>();
    SearchCommand body = await req.Content.ReadAsAsync<SearchCommand>();

    // blob conn string
    string connString = Environment.GetEnvironmentVariable("FOTOS_BLOBCONN"); // for test use '?? "value"'
    string blobContainer = Environment.GetEnvironmentVariable("FOTOS_BLOBCONT");

    string schApiKey = Environment.GetEnvironmentVariable("FOTOS_SCHAPIKEY");
    string schAcct = Environment.GetEnvironmentVariable("FOTOS_SCHACCT");
    string schIndex = Environment.GetEnvironmentVariable("FOTOS_SCHINDEX");


    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connString);
    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
    CloudBlobContainer containerSm = blobClient.GetContainerReference(blobContainer);

    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
    CloudTable table = tableClient.GetTableReference("fotoslog");
    table.CreateIfNotExists();


    // body json
    /*
{
    "value": [
        {
            "@search.action": "delete",
            "id":"4a998e83-24ae-4cfd-90b3-5ee6137f87ac"
        }
    ]
}
     */
    // add to search index
    var data = new
    {
        value = new[] { body }
    };

    string payload = Newtonsoft.Json.JsonConvert.SerializeObject(data);

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
    CloudBlockBlob blobLog = containerSm.GetBlockBlobReference($"log/image-index_{body.id}_{DateTime.Now.Ticks}.log");
    blobLog.Properties.ContentType = "text/plain";
    string indexval = Newtonsoft.Json.JsonConvert.SerializeObject(body);
    await blobLog.UploadTextAsync(indexval);
    /**/

    // save log to table
    // constraint (PK + RK) = unique
    DateTime dtNow = DateTime.Now;
    SearchIndexEntity tableLog = new SearchIndexEntity($"{dtNow.Year}-{dtNow.Month}-{dtNow.Day}", $"{DateTime.Now.Ticks}");
    tableLog.status = body.search_action;
    tableLog.uri = body.id;
    tableLog.log = indexval;

    TableOperation insertOperation = TableOperation.Insert(tableLog);
    table.Execute(insertOperation);

    return req.CreateResponse(HttpStatusCode.OK, "Ok");
    //req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
}
