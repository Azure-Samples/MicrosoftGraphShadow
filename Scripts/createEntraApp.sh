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

jsonpath=".initConfig.resourceGroupName"
resourceGroupName="$( get-value  "${jsonpath}" )"

if [ "${resourceGroupName}" == "" ]; then
    read -p "Resource group name: " resourceGroupName
    put-value      '.initConfig.resourceGroupName' $resourceGroupName
fi

NAME="ShadowApp-$resourceGroupName"
echo $NAME

apiAppReg=$(az ad app create --display-name ${NAME} --sign-in-audience AzureADMultipleOrgs )
apiAppId=$(echo $apiAppReg | jq -r .appId)
apiId=$(echo $apiAppReg | jq -r .id)

#User.Read.All
az ad app permission add --id $apiAppId --api 00000003-0000-0000-c000-000000000000 --api-permissions 'df021288-bdef-4463-88db-98f22de89214=Role'
#Group.Read.All
az ad app permission add --id $apiAppId --api 00000003-0000-0000-c000-000000000000 --api-permissions '5b567255-7703-4780-807c-7be8301ae99b=Role'
#GroupMembers.Read.All
az ad app permission add --id $apiAppId --api 00000003-0000-0000-c000-000000000000 --api-permissions '98830695-27a2-44f7-8c18-0c3ebc9698f6=Role'



clientsecret="$( az  ad app credential reset \
    --id "${apiId}" \
    --display-name "Shadow" \
    --output json )"


echo $clientsecret
echo $apiAppId

sp=$(az ad sp create --id ${apiAppId})

echo "Sleeping for 30 seconds, hoping that the service principal is created. Then grant consent."
sleep 30
az ad app permission admin-consent --id $apiAppId

aadTenantId=$(echo $context | jq -r .tenantId)
environmentName=$(echo $context | jq -r .environmentName)    



put-value      '.GraphSettings.Secret'  "$(echo "${clientsecret}" | jq -r '.password' )" 

put-value      '.GraphSettings.ClientId'  "$(echo "${clientsecret}" | jq -r '.appId' )" 

