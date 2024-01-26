@description('Application Name - change it!')
param appName string = 'ShadowGraph'

param location string = resourceGroup().location
param tenantId string = tenant().tenantId


var graph = loadJsonContent('temp/appsettings.local.json') 

@description('This is the object id of the user who will do the deployment on Azure. Can be your user id on AAD. Discover it running [az ad signed-in-user show] and get the [objectId] property.')
//param deploymentOperatorId string   = '81f402a1-f37f-405b-ba36-7148bd74106f'

// a 4-char suffix to add to the various names of azure resources to help them be unique, but still, previsible
var appSuffix = substring(uniqueString(resourceGroup().id), 0, 4)

// creates an user-assigned managed identity that will used by different azure resources to access each other.
module msi 'msi.bicep' = {
  name: 'msi-deployment'
  params: {
    location: location
    managedIdentityName: '${appName}Identity-${appSuffix}'

  }
}

// creates a key vault in this resource group
module keyvault 'keyvault.bicep' = {
  name: 'keyvault-deployment'
  params: {
    location: location
    appName: appName
    tenantId: tenantId
  }
}

// creates the cosmos db account and database with some containers configured. Saves connection string in keyvault.
module cosmos 'cosmosDb.bicep' = {
  name: 'cosmos-deployment'
  params: {
    cosmosAccountId: '${appName}-${appSuffix}'
    location: location
    cosmosDbName: appName
    keyVaultName: keyvault.outputs.keyVaultName
  }
}
module sqlRoleAssignment 'sqlRoleAssignment.bicep' = {
  name: 'sql-roleassignment'
  params: {
    databaseAccountName: cosmos.outputs.cosmosAccountName
    principalId: msi.outputs.principalId
  }
}
// creates a Log Analytics + Application Insights instance
module logAnalytics 'logAnalytics.bicep' = {
  name: 'log-analytics-deployment'
  params: {
    appName: appName
    location: location
  }
}

// creates an azure function, with secrets stored in the key vault
module azureFunctions_api 'functionApp.bicep' = {
  name: 'functions-app-deployment-api'
  params: {
    appName: appName
    appInternalServiceName: 'api'
    appNameSuffix: appSuffix
    appInsightsInstrumentationKey: logAnalytics.outputs.instrumentationKey
    keyVaultName: keyvault.outputs.keyVaultName
    msiRbacId: msi.outputs.id
    location: location
    clientId: msi.outputs.clientId
    cosmosUri: cosmos.outputs.cosmosUri
  }
  dependsOn: [
    keyvault
    logAnalytics
  ]
}

module setsecret 'setSecret.bicep' ={
  name: 'GraphSettingsSecret'
  params: {
    keyVaultName: keyvault.outputs.keyVaultName
     contentType: 'text/plain'
     secretName: 'GraphSettingsSecret'
     secretValue: graph.GraphSettings.Secret
  }
  dependsOn: [
    keyvault
  ]
}
module setclidentid 'setSecret.bicep' ={
  name: 'GraphSettingsClientId'
  params: {
    keyVaultName: keyvault.outputs.keyVaultName
     contentType: 'text/plain'
     secretName: 'GraphSettingsClientId'
     secretValue: graph.GraphSettings.ClientId
  }
  dependsOn: [
    keyvault
  ]
}



output functionEndpoint string = azureFunctions_api.outputs.functionAppUrl
output cosmosConnectionString string = cosmos.outputs.cosmosConnectionString
output notificationEndpoint string = azureFunctions_api.outputs.notificationEndpoint
output signupUrl string = azureFunctions_api.outputs.signupUrl
output addTenantUrl string = azureFunctions_api.outputs.addTenantUrl
output updateTenant string = azureFunctions_api.outputs.updateTenant
