param managedIdentityName string
param location string
var roles = loadJsonContent('temp/roles.json')
//param operatorRoleDefinitionId string

resource msi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}



resource p1 'Microsoft.Authorization/roleAssignments@2022-04-01' ={
  name: guid(roles['Cosmos DB Operator'], msi.id)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles['Cosmos DB Operator'])
    principalId: msi.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource p2 'Microsoft.Authorization/roleAssignments@2022-04-01' ={
  name: guid(roles['DocumentDB Account Contributor'], msi.id)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles['DocumentDB Account Contributor'])
    principalId: msi.properties.principalId
    principalType: 'ServicePrincipal'
  }
}


// resource sqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
//   name: guid('00000000-0000-0000-0000-000000000002', msi.id)
  
//   properties: {
//     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '00000000-0000-0000-0000-000000000002')
//     principalId: msi.properties.principalId
//     principalType: 'ServicePrincipal'
//   }
// }

resource p3 'Microsoft.Authorization/roleAssignments@2022-04-01' ={
  name: guid(roles['Key Vault Secrets User'], msi.id)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles['Key Vault Secrets User'])
    principalId: msi.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource p4 'Microsoft.Authorization/roleAssignments@2022-04-01' ={
  name: guid(roles.Contributor, msi.id)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.Contributor)
    principalId: msi.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// module customRole 'operatorSetup.bicep' =  {
//  name: 'CustomRoleAssignment'
//  params: {
//   operatorPrincipalId: msi.properties.principalId
//  }
// }


output principalId string = msi.properties.principalId
output clientId string = msi.properties.clientId
output id string = msi.id
