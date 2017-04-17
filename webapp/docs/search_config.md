# Azure Search

Azure search configuration

## index

Create index

### REST API
```
https://<schname>.search.windows.net/indexes?api-version=2016-09-01
```

### Body

```
{
    "name" : "fotos-index",
    "fields": [
        { "name": "id", "type": "Edm.String", "key": true, "searchable": false,  "filterable": false, "sortable": false, "facetable": false},
        { "name": "uri", "type": "Edm.String", "searchable": false, "filterable": false, "sortable": false, "facetable": false },
        { "name": "uri2", "type": "Edm.String", "searchable": false, "filterable": false, "sortable": false, "facetable": false },
        { "name": "thumbnail", "type": "Edm.String", "searchable": false, "filterable": false, "sortable": false, "facetable": false },
        { "name": "thumbnail2", "type": "Edm.String", "searchable": false, "filterable": false, "sortable": false, "facetable": false },
        { "name": "tags", "type": "Collection(Edm.String)", "searchable": true, "filterable": true, "sortable": false, "facetable": true },
        { "name": "caption", "type": "Edm.String", "searchable": true, "filterable": false, "sortable": false, "facetable": false }
    ]
}
```

## Data source

Create blob datasource

### REST API
```
https://<schname>.search.windows.net/datasources?api-version=2016-09-01
```

### Body

```
{
    "name" : "fotos-ds",
    "type" : "azureblob",
    "credentials" : { "connectionString" : "<connection string>" },
    "container" : { "name" : "images", "query" : "log" }
}
```

## Indexer

Create json indexer

### REST API
```
https://<schname>.search.windows.net/indexers?api-version=2015-02-28-Preview
```

### Body

```
{
  "name" : "fotos-indexer",
  "dataSourceName" : "fotos-ds",
  "targetIndexName" : "fotos-index",
  "parameters" : { "configuration" : { "parsingMode" : "json"} }
}
```