# Overview

This sample app demonstrates how to implement BCDR on Azure with intelligent photo album application

- Azure App Service: hosts node.js web application
- Azure Blob: stores images in primary/secondary sites
- Azure Cognitive API: gets images caption and tags
- Azure Function: processes thumbnail, get image caption and tag and etc
- Azure Search: searchs images by keyword. it provides paging and facet
- Azure Traffic Manager: handles DR

## iFoto Webapp

Node.js based ifoto webapp

[Readme](./webapp/README.md)


## iFoto Fxapp

C# based ifoto functions

[Readme](./fxapp/README.md)

