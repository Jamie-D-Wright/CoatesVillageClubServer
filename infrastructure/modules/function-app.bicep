@description('Name of the Function App')
param functionAppName string

@description('Location for the Function App')
param location string = resourceGroup().location

@description('App Service Plan ID')
param appServicePlanId string

@description('Storage Account Name for Function App')
param storageAccountName string

@description('Application Insights Connection String')
param appInsightsConnectionString string

@description('Key Vault Name for secrets')
param keyVaultName string

@description('SQL Server FQDN')
param sqlServerFqdn string

@description('Database Name')
param databaseName string

@description('SQL Admin Login (stored in Key Vault)')
@secure()
param sqlAdminLogin string

@description('SQL Admin Password (stored in Key Vault)')
@secure()
param sqlAdminPassword string

@description('Additional app settings')
param additionalAppSettings array = []

@description('Tags to apply to resources')
param tags object = {}

// Get existing storage account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    reserved: true  // Required for Linux
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: concat([
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
        {
          name: 'SqlConnection__Server'
          value: sqlServerFqdn
        }
        {
          name: 'SqlConnection__Database'
          value: databaseName
        }
        {
          name: 'SqlConnection__UserId'
          value: sqlAdminLogin
        }
        {
          name: 'SqlConnection__Password'
          value: sqlAdminPassword
        }
      ], additionalAppSettings)
    }
  }
}

@description('Function App Name')
output functionAppName string = functionApp.name

@description('Function App Default Hostname')
output functionAppHostname string = functionApp.properties.defaultHostName

@description('Function App Principal ID (Managed Identity)')
output functionAppPrincipalId string = functionApp.identity.principalId

@description('Function App Resource ID')
output functionAppId string = functionApp.id
