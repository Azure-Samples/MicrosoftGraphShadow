#!/bin/bash

echo "Running az cli $(az version | jq '."azure-cli"' )"
echo "Running in subscription $( az account show | jq -r '.id') / $( az account show | jq -r '.name'), AAD Tenant $( az account show | jq -r '.tenantId')"

source ./helpers.sh

basedir="$( dirname "$( readlink -f "$0" )" )"

#CONFIG_FILE="${basedir}/./config.json"
CONFIG_FILE="./temp/appSettings.local.json"

if [ ! -f "$CONFIG_FILE" ]; then
    cp ./appSettings-template.json "${CONFIG_FILE}"
fi


jsonpath=".initConfig.location"
location="$( get-value  "${jsonpath}" )"
[ "${location}" == "" ] && { echo "Please configure ${jsonpath} in file ${CONFIG_FILE}" ; exit 1 ; }

jsonpath=".initConfig.resourceGroupName"
resourceGroupName="$( get-value  "${jsonpath}" )"

if [ "${resourceGroupName}" == "" ]; then
    read -p "Resource group name: " resourceGroupName
    put-value      '.initConfig.resourceGroupName' $resourceGroupName
fi

put-value      '.initConfig.subscriptionId' "$( az account show | jq -r '.id')" 
put-value      '.initConfig.tenantId' "$( az account show | jq -r '.tenantId')" 



#
# Create the resource group
#
( az group create --location "${location}"  --name "${resourceGroupName}"  \
    &&  echo "Creation of resource group ${resourceGroupName} complete." ) \
    || echo "Failed to create resource group ${resourceGroupName}."  \
        |  exit 1

#
# Deploy
#
deploymentResultJSON="$( az deployment group create \
    --resource-group "${resourceGroupName}" \
    --template-file "./main.bicep" \
    --parameters \
        location="${location}" \
    --output json )"

echo "ARM Deployment: $( echo "${deploymentResultJSON}" | jq -r .properties.provisioningState )"
echo "${deploymentResultJSON}" > results.json

if ! [ $( echo "${deploymentResultJSON}" | jq -r .properties.provisioningState ) = "Succeeded" ]; then
    echo "Deployment failed. Do not proceed"
    exit 1
fi


tid="$( get-value  ".initConfig.tenantId" | cut -d "." -f 1)"
echo "Tenant ID: ${tid}."

put-value      '.functionEndpoint' "$(echo "${deploymentResultJSON}" | jq -r '.properties.outputs.functionEndpoint.value' )" 
put-value      '.manage.signupUrl' "$(echo "${deploymentResultJSON}" | jq -r '.properties.outputs.signupUrl.value' )$tid"  
put-value      '.manage.addTenantUrl' "$(echo "${deploymentResultJSON}" | jq -r '.properties.outputs.addTenantUrl.value' )" 
put-value      '.manage.updateTenant' "$(echo "${deploymentResultJSON}" | jq -r '.properties.outputs.updateTenant.value' )" 
put-value      '.CosmosSettings.ConnectionString' "$(echo "${deploymentResultJSON}" | jq -r '.properties.outputs.cosmosConnectionString.value' )" 
# ./deploy.sh
sed  -i "s/replaceWithTenantId/$tid/g" $CONFIG_FILE 
#> temp.json && cat temp.json