param defaultResourceName string
param location string
param storageAccountTables array
param containerVersion string
param environmentName string
param developersGroup string
param integrationEnvironment object

param acrLoginServer string
param acrUsername string
@secure()
param acrPassword string
param corsHostnames array

param containerPort int = 8080
param containerAppName string = 'wam-users-api'

resource containerAppEnvironments 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: integrationEnvironment.containerAppsEnvironment
  scope: resourceGroup(integrationEnvironment.resourceGroup)
}
resource appConfiguration 'Microsoft.AppConfiguration/configurationStores@2022-05-01' existing = {
  name: integrationEnvironment.appConfiguration
  scope: resourceGroup(integrationEnvironment.resourceGroup)
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: uniqueString(defaultResourceName)
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
resource storageAccountTableService 'Microsoft.Storage/storageAccounts/tableServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}
resource storageAccountTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2021-09-01' = [
  for table in storageAccountTables: {
    name: table
    parent: storageAccountTableService
  }
]

module storageAccountConfigurationValue 'configuration-value.bicep' = {
  name: 'storageAccountConfigurationValue'
  scope: resourceGroup(integrationEnvironment.resourceGroup)
  params: {
    appConfigurationName: integrationEnvironment.appConfiguration
    settingName: 'AzureServices:UsersStorageAccountName'
    settingValue: storageAccount.name
  }
}

module serviceNameConfigurationValue 'configuration-value.bicep' = {
  name: 'serviceNameConfigurationValue'
  scope: resourceGroup(integrationEnvironment.resourceGroup)
  params: {
    appConfigurationName: integrationEnvironment.appConfiguration
    settingName: 'Services:UsersService'
    settingValue: apiContainerApp.name
  }
}
resource apiContainerApp 'Microsoft.App/containerApps@2023-08-01-preview' = {
  name: '${defaultResourceName}-aca'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnvironments.id

    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: containerPort
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
        corsPolicy: {
          allowedOrigins: corsHostnames
          allowCredentials: true
          allowedHeaders: ['*']
          allowedMethods: ['*']
          maxAge: 0
        }
      }
      dapr: {
        enabled: true
        appPort: containerPort
        appId: '${defaultResourceName}-aca'
      }
      secrets: [
        {
          name: 'containerregistrypassword'
          value: acrPassword
        }
      ]
      registries: [
        {
          server: acrLoginServer
          username: acrUsername
          passwordSecretRef: 'containerregistrypassword'
        }
      ]
    }

    template: {
      containers: [
        {
          image: '${acrLoginServer}/${containerAppName}:${containerVersion}'
          name: containerAppName
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'Azure__StorageAccount'
              value: storageAccount.name
            }
            {
              name: 'AzureAppConfiguration'
              value: appConfiguration.properties.endpoint
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 6
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '20'
              }
            }
          }
        ]
      }
    }
  }
}

module roleAssignmentsModule 'all-role-assignments.bicep' = {
  name: 'roleAssignmentsModule'
  params: {
    containerAppPrincipalId: apiContainerApp.identity.principalId
    developersGroup: developersGroup
    integrationResourceGroupName: integrationEnvironment.resourceGroup
  }
}
