using './main.bicep'

param systemName = 'wam-users-api'
param environmentName = 'tst'
param locationAbbreviation = 'ne'
param developersGroup = '0855534d-094e-40e7-9e19-d189578f69a9'
param integrationEnvironment = {
  resourceGroup: 'wam-tst-int-rg'
  containerAppsEnvironment: 'wam-tst-int-env'
  appConfiguration: 'wam-tst-int-appcfg'
}
