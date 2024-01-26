
param appName string
param location string = resourceGroup().location
var uniqueId = substring(uniqueString(resourceGroup().id), 0, 4)
var logAnalyticsWorkspaceName = 'log-${appName}-${uniqueId}'
var appInsightsName = '${'appi-'}-${uniqueId}'
// Log Analytics workspace is required for new Application Insights deployments
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  location: location
  name: logAnalyticsWorkspaceName
  properties: {
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    sku: {
      name: 'PerGB2018'
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// App Insights resource
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output instrumentationKey string = appInsights.properties.InstrumentationKey
