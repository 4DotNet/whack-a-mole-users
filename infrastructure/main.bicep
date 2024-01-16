targetScope = 'subscription'

param systemName string

@allowed([
  'dev'
  'tst'
  'prd'
])
param environmentName string
param location string = deployment().location
param locationAbbreviation string
param containerVersion string = '1.0.0'
param developersGroup string
param integrationEnvironment object

var apiResourceGroupName = toLower('${systemName}-${environmentName}-${locationAbbreviation}')

var storageAccountTables = [
  'users'
]

resource apiResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: apiResourceGroupName
  location: location
}

module resourcesModule 'resources.bicep' = {
  name: 'ResourceModule'
  scope: apiResourceGroup
  params: {
    defaultResourceName: apiResourceGroupName
    location: location
    storageAccountTables: storageAccountTables
    containerVersion: containerVersion
    integrationEnvironment: integrationEnvironment
    environmentName: environmentName
    developersGroup: developersGroup
  }
}
