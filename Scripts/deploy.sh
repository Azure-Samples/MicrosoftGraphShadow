#!/bin/bash
set -u -e -o pipefail

echo "Building..."
source ./helpers.sh

basedir="$( dirname "$( readlink -f "$0" )" )"

#CONFIG_FILE="${basedir}/./config.json"
CONFIG_FILE="./temp/appSettings.local.json"

appName="$( get-value  ".functionEndpoint" | cut -d "." -f 1)"
resourceGroupName="$( get-value  ".initConfig.resourceGroupName" | cut -d "." -f 1)"

echo "App: ${appName}"
echo "Resource group :${resourceGroupName}"
(dotnet publish "../ShadowFunctions/ShadowFunctions.csproj" -c DEBUG --output ./temp/publishfunc && cd ./temp/publishfunc && zip -r ../functionApp.zip * .[^.]* && cd ../../ ) \
    || echo "failed to compile" \
        | exit 1
echo "Deploying functionapp..."
az functionapp deployment source config-zip -g "${resourceGroupName}" -n "${appName}" --src ./temp/functionApp.zip 

registerTenant="$( get-value  ".manage.signupUrl" )"
addTenent="$( get-value  ".manage.addTenantUrl" )"
updateTenant="$( get-value  ".manage.updateTenant" )"

echo -e "\n\nRegister tenant, needs consent: \n${registerTenant}\n"
echo -e "Add tenant to shadow: \n${addTenent}\n"
echo -e "Force update of tenant: \n${updateTenant}\n"
