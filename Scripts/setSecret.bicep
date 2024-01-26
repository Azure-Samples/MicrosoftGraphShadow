param keyVaultName string

param secretName string
@secure()
param secretValue string

@allowed([
  'application/json'
  'text/plain'
])
param contentType string
resource kv 'Microsoft.KeyVault/vaults@2023-02-01' existing ={
  name:keyVaultName
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  parent:kv
  name: secretName
  properties: {
    value: secretValue
    contentType: contentType
  }
}
output keyVaultSecretName string = secret.name
