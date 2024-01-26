
@maxLength(30)
param cosmosAccountId string
param location string
param cosmosDbName string
param keyVaultName string

param defaultConsistencyLevel string = 'Session'

@minValue(10)
@maxValue(2147483647)
@description('Max stale requests. Required for BoundedStaleness. Valid ranges, Single Region: 10 to 2147483647. Multi Region: 100000 to 2147483647.')
param maxStalenessPrefix int = 100000

@minValue(5)
@maxValue(86400)
@description('Max lag time (minutes). Required for BoundedStaleness. Valid ranges, Single Region: 5 to 84600. Multi Region: 300 to 86400.')
param maxIntervalInSeconds int = 300

@allowed([
  true
  false
])
@description('Enable system managed failover for regions')
param systemManagedFailover bool = true


//param throughput int = 400
param tags object = {
  deploymentGroup: 'cosmosdb'
}

var consistencyPolicy = {
  Eventual: {
    defaultConsistencyLevel: 'Eventual'
  }
  ConsistentPrefix: {
    defaultConsistencyLevel: 'ConsistentPrefix'
  }
  Session: {
    defaultConsistencyLevel: 'Session'
  }
  BoundedStaleness: {
    defaultConsistencyLevel: 'BoundedStaleness'
    maxStalenessPrefix: maxStalenessPrefix
    maxIntervalInSeconds: maxIntervalInSeconds
  }
  Strong: {
    defaultConsistencyLevel: 'Strong'
  }
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-09-15' = {
  name: toLower(cosmosAccountId)
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: consistencyPolicy[defaultConsistencyLevel]
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: systemManagedFailover
  }
}


resource cosmosDb_database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-09-15' = {
  name: cosmosDbName
  parent: cosmosAccount
  tags: tags
  properties: {
    resource: {
      id: cosmosDbName
    }
  }
}

// resource container_leases 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
//   name: '${cosmosDb_database.name}/leases'
//   tags: tags
//   dependsOn: [
//     cosmosAccount
//   ]
//   properties: {
//     resource: {
//       id: 'leases'
//       partitionKey: {
//         paths: [
//           '/id'
//         ]
//       }
//     }
//   }
// }


resource container_entities 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
  name: 'Entities'
  parent:cosmosDb_database
  tags: tags
  properties: {
    resource: {
      id: 'Entities'
      partitionKey: {
        paths: [
          '/tenantId' 
          '/odataType' 
        ]
        kind: 'MultiHash'
        version: 2
      }
      indexingPolicy: {
        // excludedPaths: [
        //   {
        //     path: '/additionalData/*'
        //   }
        //   {
        //     path: '/_etag/?'
        //   }
        // ]
        compositeIndexes: [
          [
            {
              path: '/tenantId'
              order: 'ascending'
            }
            {
              path: '/odataType'
              order: 'ascending'
            }
          ]
        ]
      }
      // uniqueKeyPolicy: {
      //   uniqueKeys: [
      //     {
      //       paths: [
      //         '/tenantId/opdataType/id'
      //       ]
      //     }
      //   ]
      // }
    }
  }
}




var cn = listConnectionStrings(resourceId('Microsoft.DocumentDB/databaseAccounts', cosmosAccount.name), '2023-09-15').connectionStrings[0].connectionString
module setCosmosConnectionString 'setSecret.bicep' = {
  name: 'setCosmosConnectionString'
  params: {
    keyVaultName: keyVaultName
    secretName: 'CosmosDbConnectionString'
    secretValue:  cn
    contentType: 'text/plain'
  }
}
// module setCosmosEndpoint 'setSecret.bicep' = {
//   name: 'setCosmosEndpoint'
//   params: {
//     keyVaultName: keyVaultName
//     secretName: 'CosmosEndpoint'
//     secretValue:  cosmosAccount.properties.documentEndpoint
//     contentType: 'text/plain'
//   }
// }

// module setCosmosConnectionString4 'setSecret.bicep' = {
//   name: 'setCosmosAccontkey'
//   params: {
//     keyVaultName: keyVaultName
//     secretName: 'CosmosDbAccountkey'
//     secretValue: cosmosAccount.listKeys().primaryMasterKey
//     contentType: 'text/plain'
//   }
// }


output cosmosAccountId string = cosmosAccountId
output cosmosUri string = cosmosAccount.properties.documentEndpoint
output cosmosAccountName string = cosmosAccount.name
output cosmosConnectionString string = cn
