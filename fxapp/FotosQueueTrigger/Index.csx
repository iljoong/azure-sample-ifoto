using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Microsoft.WindowsAzure.Storage.Table;
public class SearchIndex
{
    [JsonProperty("@search.action")]
    public string search_action { get; set; }
    public string id { get; set; }
    public string uri { get; set; }
    public string uri2 { get; set; }
    public string thumbnail { get; set; }
    public string thumbnail2 { get; set; }
    public JArray tags { get; set; }
    public string caption { get; set;  }
}


public class SearchIndexEntity: TableEntity
{
    public SearchIndexEntity(string day, string ymd)
    {
        this.PartitionKey = day;
        this.RowKey = ymd;
    }

    public SearchIndexEntity() { }
    public string status { get; set; }
    public string uri { get; set;  }
    public string log { get; set; }
}