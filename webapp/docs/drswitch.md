# DR switch command

## Bash script (azure-cli)

```
#!/bin/bash

# set variables
rgname="<rgname>"
prefix="<prefix>"

tmname="${prefix}tm"
webname="${prefix}web"
schname="${prefix}sch"
apikey="<apikey>"

if [ $# -eq 0 ]
then
        echo "$0 [failover | failback ]"
else
        if [ $1 == "failover" ]
        then
                echo "executing failover"

                curl -H "api-key: $apikey" -H "Content-Type: application/json" \
                        -X post https://${schname}.search.windows.net/indexers/fotos-json-indexerdr/run?api-version=2016-09-01

                az resource update -g $rgname --namespace "Microsoft.Network/trafficManagerProfiles" \
                        --resource-type "azureEndpoints" --api-version 2015-04-28-preview \
                        --parent $tmname -n endpoint1 \
                        --set properties.priority=3 --verbose

                az appservice web config appsettings update -n $webname -g $rgname --settings FOTOS_READONLY=true
                az appservice web config appsettings update -n ${webname}dr -g ${rgname}dr --settings FOTOS_READONLY=false
        else
                echo "executing failback"

                curl -H "api-key: $apikey" -H "Content-Type: application/json" \
                        -X post https://${schname}.search.windows.net/indexers/fotos-json-indexer/run?api-version=2016-09-01

                az resource update -g $rgname --namespace "Microsoft.Network/trafficManagerProfiles" \
                        --resource-type "azureEndpoints" --api-version 2015-04-28-preview \
                        --parent $tmname -n endpoint1 \
                        --set properties.priority=1 --verbose

                az appservice web config appsettings update -n $webname -g $rgname --settings FOTOS_READONLY=false
                az appservice web config appsettings update -n ${webname}dr -g ${rgname}dr --settings FOTOS_READONLY=true
        fi
fi
```

## Powershell script (azure-cli)

```
# set variables
$rgname="<rgname>"
$prefix="<prefix>"
$tmname="$($prefix)tm"
$webname="$($prefix)web"
$schname="$($prefix)sch"
$apikey="<apikey>"

if ($Args.Length -eq 0)
{

        echo "./drswitch.ps1 [failover | failback ]"
}
else
{
        if ($Args[0] -eq "failover")
        {
        
                echo "executing failover"

                Invoke-WebRequest -Method POST `
                    -Headers @{"api-key" = $apikey; "Content-Type" = "application/json"} `
                    -Uri  "https://$schname.search.windows.net/indexers/fotos-json-indexerdr/run?api-version=2016-09-01"

                az resource update -g $rgname --namespace "Microsoft.Network/trafficManagerProfiles" `
                        --resource-type "azureEndpoints" --api-version 2015-04-28-preview `
                        --parent $tmname -n endpoint1 `
                        --set properties.priority=3 --verbose

                az appservice web config appsettings update -n $webname -g $rgname --settings FOTOS_READONLY=true
                az appservice web config appsettings update -n $($webname)dr -g $($rgname)dr --settings FOTOS_READONLY=false
        }
        else
        {
                echo "executing failback"

                Invoke-WebRequest -Method POST `
                    -Headers @{"api-key" = $apikey; "Content-Type" = "application/json"} `
                    -Uri  "https://$schname.search.windows.net/indexers/fotos-json-indexer/run?api-version=2016-09-01"

                az resource update -g $rgname --namespace "Microsoft.Network/trafficManagerProfiles" `
                        --resource-type "azureEndpoints" --api-version 2015-04-28-preview `
                        --parent $tmname -n endpoint1 `
                        --set properties.priority=1 --verbose

                az appservice web config appsettings update -n $webname -g $rgname --settings FOTOS_READONLY=false
                az appservice web config appsettings update -n $($webname)dr -g $($rgname)dr --settings FOTOS_READONLY=true
        }
        
}
```