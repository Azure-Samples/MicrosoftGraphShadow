// The following will create an Azure Function app on
// a consumption plan, along with a storage account
// and application insights.

// note: https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations

param location string = resourceGroup().location
param functionRuntime string = 'dotnet-isolated'

@description('A name for this whole project, used to help name individual resources')
param appName string

@description('The name of the role or service of this function. Example: Api CommandHandler, EventHandler')
param appInternalServiceName string

@description('Id of a existing keyvault that will be used to store and retrieve keys in this deployment')
param keyVaultName string

param cosmosUri string

@description('User-assigned managed identity that will be attached to this function and will have power to connect to different resources.')
param msiRbacId string
param clientId string

@description('Application insights instrumentation key.')
param appInsightsInstrumentationKey string

param deploymentDate string = utcNow()

param appNameSuffix string

var functionAppName = 'func-${appName}-${appInternalServiceName}-${appNameSuffix}'
var appServiceName = 'ASP-${appName}${appInternalServiceName}-${appNameSuffix}'

// remove dashes for storage account name
var storageAccountName = toLower(format('st{0}', replace('${appInternalServiceName}-${appNameSuffix}', '-', '')))

var appTags = {
  AppID: '${appName}-${appInternalServiceName}'
  AppName: '${appName}-${appInternalServiceName}'
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
  scope: resourceGroup()
}

module setStorageAccountSecret 'setSecret.bicep' = {
  name: 'stgSecret-${appInternalServiceName}-${deploymentDate}'
  params: {
    keyVaultName: keyVault.name
    secretName: '${storageAccount.name}-${appInternalServiceName}-ConnectionString'
    secretValue: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
    contentType: 'text/plain'
  }
}

// App Service
resource appService 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServiceName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'S1'
    //tier: 'Dynamic'
    size: 'S1'
    //family: 'Y'
    capacity: 1
  }
  // properties: {
  //   maximumElasticWorkerCount: 1
  //   targetWorkerCount: 0
  //   targetWorkerSizeId: 0
  // }
  tags: appTags
}

//var StorageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id,storageAccount.apiVersion).keys[0].value}'

// Function App
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${msiRbacId}': {}
    }
  }
  kind: 'functionapp'
  properties: {
    keyVaultReferenceIdentity: msiRbacId
    enabled: true
    hostNameSslStates: [
      {
        name: '${functionAppName}.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
      {
        name: '${functionAppName}.scm.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
    ]
    serverFarmId: appService.id
    siteConfig: {
      alwaysOn: true
      netFrameworkVersion: 'v8.0'
      appSettings: []
      use32BitWorkerProcess: true
    }
    scmSiteAlsoStopped: false
    clientAffinityEnabled: false
    clientCertEnabled: false
    hostNamesDisabled: false
    dailyMemoryTimeQuota: 0
    httpsOnly: false
    redundancyMode: 'None'

  }
  tags: appTags
}

resource appSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'appsettings'
  parent: functionApp
  properties: {
    FUNCTIONS_WORKER_RUNTIME: functionRuntime
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED: '1'
    FUNCTIONS_EXTENSION_VERSION: '~4'

    AzureWebJobsStorage: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${storageAccount.name}-${appInternalServiceName}-ConnectionString)'

    APPINSIGHTS_INSTRUMENTATIONKEY: appInsightsInstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: 'InstrumentationKey=${appInsightsInstrumentationKey}'

    'GraphSettings:ClientId': '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=GraphSettingsClientId)'
    'GraphSettings:Secret': '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=GraphSettingsSecret)'
    'GraphSettings:NotificationUrl': notificationEndpoint

    KeyVaultName: keyVault.name
    KeyVaultUri: keyVault.properties.vaultUri

    'AppSettings:ManagedIdentityClientId': clientId

    'CosmosSettings:CosmosEndpoint': cosmosUri
  }
}

var functionKey = listKeys('${functionApp.id}/host/default', functionApp.apiVersion).functionKeys.default
var notificationEndpoint = 'https://${functionApp.properties.defaultHostName}/api/Receive?code=${functionKey}'
output functionAppUrl string = functionApp.properties.defaultHostName
output notificationEndpoint string = notificationEndpoint
output signupUrl string = 'https://${functionApp.properties.defaultHostName}/api/signup/'
output addTenantUrl string = 'https://${functionApp.properties.defaultHostName}/api/orchestrators/manage/replaceWithTenantId/addtenant?code=${functionKey}'
output updateTenant string = 'https://${functionApp.properties.defaultHostName}/api/orchestrators/manage/replaceWithTenantId/update?code=${functionKey}'
