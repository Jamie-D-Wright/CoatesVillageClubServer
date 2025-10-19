param location string = resourceGroup().location
param environmentName string
param serviceName string = 'villageclub'

@secure()
@description('SQL Server administrator login')
param sqlAdminLogin string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@description('API Management publisher email')
param apimPublisherEmail string = 'admin@coatesvillageclub.org'

@description('API Management publisher name')
param apimPublisherName string = 'Coates Village Club'

@description('JWT Issuer for token validation')
param jwtIssuer string = 'https://villageclub.coates.local'

@description('JWT Audience for token validation')
param jwtAudience string = 'villageclub-api'

@description('JWT Token expiry in minutes')
param jwtAccessTokenExpiryMinutes int = 60

@description('JWT Refresh token expiry in days')
param jwtRefreshTokenExpiryDays int = 30

var membershipFunctionAppName = 'func-${serviceName}-membership-${environmentName}'
var appServicePlanName = 'asp-${serviceName}-${environmentName}'
var appInsightsName = 'appi-${serviceName}-${environmentName}'
var keyVaultName = 'kv-${take('${serviceName}${environmentName}', 21)}'
var storageAccountName = take('stfunc${serviceName}${environmentName}', 24)  // Minimum 8 chars with 'stfunc' prefix
var sqlServerName = 'sql-${serviceName}-${environmentName}'
var apimName = 'apim-${serviceName}-${environmentName}'
var receiptsStorageName = take('streceipts${serviceName}${environmentName}', 24)

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    DisableIpMasking: false
    Flow_Type: 'Bluefield'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
  }
}

// Membership Function App Module
module membershipFunctionApp 'modules/function-app.bicep' = {
  name: 'membershipFunctionApp'
  params: {
    functionAppName: membershipFunctionAppName
    location: location
    appServicePlanId: appServicePlan.id
    storageAccountName: storageAccount.name
    appInsightsConnectionString: appInsights.properties.ConnectionString
    keyVaultName: keyVault.name
    sqlServerFqdn: sqlDatabase.outputs.sqlServerFqdn
    databaseName: sqlDatabase.outputs.databaseName
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    additionalAppSettings: [
      {
        name: 'JwtSettings__Issuer'
        value: jwtIssuer
      }
      {
        name: 'JwtSettings__Audience'
        value: jwtAudience
      }
      {
        name: 'JwtSettings__AccessTokenExpiryMinutes'
        value: string(jwtAccessTokenExpiryMinutes)
      }
      {
        name: 'JwtSettings__RefreshTokenExpiryDays'
        value: string(jwtRefreshTokenExpiryDays)
      }
      {
        name: 'ServiceSettings__ApplicationName'
        value: 'VillageClub.Membership'
      }
      {
        name: 'ServiceSettings__EnvironmentName'
        value: environmentName
      }
      {
        name: 'WEBSITE_RUN_FROM_PACKAGE'
        value: '1'
      }
    ]
    tags: {
      Environment: environmentName
      Service: serviceName
      Component: 'Membership'
    }
  }
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' existing = {
  name: membershipFunctionAppName
  dependsOn: [
    membershipFunctionApp
  ]
}

// SQL Database Module
module sqlDatabase 'modules/sql-database.bicep' = {
  name: 'sqlDatabase'
  params: {
    sqlServerName: sqlServerName
    location: location
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    databaseName: 'VillageClubDB'
    enableServerless: true
    minCapacity: '0.5'
    maxCapacity: '2'
    autoPauseDelay: 60
    tags: {
      Environment: environmentName
      Service: serviceName
    }
  }
}

// Blob Storage Module for Receipts
module receiptsStorage 'modules/blob-storage.bicep' = {
  name: 'receiptsStorage'
  params: {
    storageAccountName: receiptsStorageName
    location: location
    storageAccountSku: 'Standard_LRS'
    receiptsContainerName: 'receipts'
    coolTierTransitionDays: 90
    tags: {
      Environment: environmentName
      Service: serviceName
      Purpose: 'Receipts'
    }
  }
}

// API Management Module
module apiManagement 'modules/apim.bicep' = {
  name: 'apiManagement'
  params: {
    apimName: apimName
    location: location
    publisherEmail: apimPublisherEmail
    publisherName: apimPublisherName
    sku: 'Consumption'
    tags: {
      Environment: environmentName
      Service: serviceName
    }
  }
}

// Grant the function app access to the Key Vault
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2022-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        objectId: functionApp.identity.principalId
        tenantId: subscription().tenantId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}
