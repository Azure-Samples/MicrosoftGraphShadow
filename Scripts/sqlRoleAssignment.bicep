
@description('CosmosDB Account to apply the role assignment to')
param databaseAccountName string



@description('Principal id to assign the role to')
param principalId string



var roleDefinitionId = guid('sql-role-definition-', principalId, databaseAccount.id)

var roleAssignmentId = guid(roleDefinitionId, principalId, databaseAccount.id)
var roles = loadJsonContent('temp/roles.json')
resource databaseAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' existing = {
  name: databaseAccountName
}

resource sqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-09-15' = {
  name: roleAssignmentId
  parent: databaseAccount
  properties: {
    principalId: principalId
    roleDefinitionId: sqlRoleDefinitionReadWrite.id
    scope: databaseAccount.id
  }
}
var appSuffix = substring(uniqueString(resourceGroup().id), 0, 4)
@description('Friendly name for the SQL Role Definition')
var  roleDefinitionReadWriteName = 'Read Write Role - ${appSuffix}'

@description('Data actions permitted by the Role Definition')
param dataActionsReadWrite array = [
  'Microsoft.DocumentDB/databaseAccounts/readMetadata'
  'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
  'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
]
resource sqlRoleDefinitionReadWrite 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2023-09-15' = {
  name: roleDefinitionId
  parent: databaseAccount
  properties: {
    roleName: roleDefinitionReadWriteName
    type: 'CustomRole'
    assignableScopes: [
      databaseAccount.id
      
    ]
    permissions: [
      {
        dataActions: dataActionsReadWrite
      }
    ]
  }
}
resource p2 'Microsoft.Authorization/roleAssignments@2022-04-01' ={
  name: guid(roles['DocumentDB Account Contributor'], principalId)
  scope: databaseAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles['DocumentDB Account Contributor'])
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
