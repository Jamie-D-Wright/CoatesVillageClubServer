param location string = resourceGroup().location
param environmentName string
param serviceName string = 'coatesvillageclub'

@secure()
@description('SQL Server administrator login')
param sqlAdminLogin string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@description('API Management publisher email')
param apimPublisherEmail string = 'wright.jamied@gmail.com'

@description('API Management publisher name')
param apimPublisherName string = 'Coates Village Club'

@description('JWT Issuer for token validation')
param jwtIssuer string = 'https://coatesvillageclub.dev'

@description('JWT Audience for token validation')
param jwtAudience string = 'coatesvillageclub-api'

@description('JWT Token expiry in minutes')
param jwtAccessTokenExpiryMinutes int = 60

@description('JWT Refresh token expiry in days')
param jwtRefreshTokenExpiryDays int = 30

// Naming Convention: Simplified - service name removed as redundant
// Format: {type}-{component?}-{environment}
// Storage accounts: st{component}{env} (no hyphens, max 24 chars, lowercase only)

var membershipFunctionAppName = 'func-membership-${environmentName}'
var appServicePlanName = 'plan-${environmentName}'
var appInsightsName = 'insights-${environmentName}'
var keyVaultName = 'vault-${environmentName}'  // vault-dev (max 24)
var functionStorageAccountName = 'stfunc${toLower(environmentName)}'  // stfuncdev (9 chars)
var membershipStorageAccountName = 'stmembership${toLower(environmentName)}'  // stmembershipdev (15 chars)
var eventsStorageAccountName = 'stevents${toLower(environmentName)}'    // steventsdev (11 chars)
var shiftsStorageAccountName = 'stshifts${toLower(environmentName)}'    // stshiftsdev (11 chars)
var stockStorageAccountName = 'ststock${toLower(environmentName)}'     // ststockdev (10 chars)
var financeStorageAccountName = 'stfinance${toLower(environmentName)}'   // stfinancedev (12 chars)
var sqlServerName = 'sqlserver-${environmentName}'
var apimName = 'apim-${environmentName}'

// Storage account for Azure Functions runtime
resource functionStorageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: functionStorageAccountName
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
    storageAccountName: functionStorageAccount.name
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
    maxCapacity: '1'  // Reduced from 2 to stay within vCore quota
    autoPauseDelay: 60
    tags: {
      Environment: environmentName
      Service: serviceName
    }
  }
}

// Membership Service Storage - Currently no storage requirements identified
module membershipStorage 'modules/blob-storage.bicep' = {
  name: 'membershipStorage'
  params: {
    storageAccountName: membershipStorageAccountName
    location: location
    storageAccountSku: 'Standard_LRS'
    containerNames: [
      'documents'     // Future use: member documents, certificates, etc.
    ]
    coolTierTransitionDays: 90
    deleteAfterDays: 2555  // 7 years retention
    tags: {
      Environment: environmentName
      Service: serviceName
      Purpose: 'Membership Service Storage'
    }
  }
}

// Events Service Storage - For event-related documents and media
module eventsStorage 'modules/blob-storage.bicep' = {
  name: 'eventsStorage'
  params: {
    storageAccountName: eventsStorageAccountName
    location: location
    storageAccountSku: 'Standard_LRS'
    containerNames: [
      'posters'       // Event promotional materials
      'documents'     // Event planning documents
      'photos'        // Event photos for history/gallery
    ]
    coolTierTransitionDays: 90
    deleteAfterDays: 2555  // 7 years retention
    tags: {
      Environment: environmentName
      Service: serviceName
      Purpose: 'Events Service Storage'
    }
  }
}

// Shifts Service Storage - For shift-related documents
module shiftsStorage 'modules/blob-storage.bicep' = {
  name: 'shiftsStorage'
  params: {
    storageAccountName: shiftsStorageAccountName
    location: location
    storageAccountSku: 'Standard_LRS'
    containerNames: [
      'schedules'     // Shift schedules and rotas
      'reports'       // Shift reports and logs
    ]
    coolTierTransitionDays: 90
    deleteAfterDays: 2555  // 7 years retention
    tags: {
      Environment: environmentName
      Service: serviceName
      Purpose: 'Shifts Service Storage'
    }
  }
}

// Stock Service Storage - For stock-related data
module stockStorage 'modules/blob-storage.bicep' = {
  name: 'stockStorage'
  params: {
    storageAccountName: stockStorageAccountName
    location: location
    storageAccountSku: 'Standard_LRS'
    containerNames: [
      'reports'       // Stock reports and analytics
      'exports'       // Data exports
    ]
    coolTierTransitionDays: 90
    deleteAfterDays: 2555  // 7 years retention
    tags: {
      Environment: environmentName
      Service: serviceName
      Purpose: 'Stock Service Storage'
    }
  }
}

// Finance Service Storage - For receipts and financial documents
module financeStorage 'modules/blob-storage.bicep' = {
  name: 'financeStorage'
  params: {
    storageAccountName: financeStorageAccountName
    location: location
    storageAccountSku: 'Standard_LRS'
    containerNames: [
      'receipts'      // Expense receipts (REQUIRED by FR-029)
      'invoices'      // Club invoices
      'statements'    // Financial statements
      'reports'       // Financial reports and audits
    ]
    coolTierTransitionDays: 90
    deleteAfterDays: 2555  // 7 years retention for financial records
    tags: {
      Environment: environmentName
      Service: serviceName
      Purpose: 'Finance Service Storage'
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

// Outputs
output membershipFunctionAppName string = membershipFunctionAppName
output membershipFunctionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlDatabase.outputs.sqlServerFqdn
output databaseName string = sqlDatabase.outputs.databaseName
output apimGatewayUrl string = apiManagement.outputs.apimGatewayUrl
output apimName string = apiManagement.outputs.apimName
output serviceRegistryUrl string = apiManagement.outputs.serviceRegistryUrl
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output functionStorageAccountName string = functionStorageAccount.name
output membershipStorageAccountName string = membershipStorage.outputs.storageAccountName
output membershipStorageAccountId string = membershipStorage.outputs.storageAccountId
output eventsStorageAccountName string = eventsStorage.outputs.storageAccountName
output eventsStorageAccountId string = eventsStorage.outputs.storageAccountId
output shiftsStorageAccountName string = shiftsStorage.outputs.storageAccountName
output shiftsStorageAccountId string = shiftsStorage.outputs.storageAccountId
output stockStorageAccountName string = stockStorage.outputs.storageAccountName
output stockStorageAccountId string = stockStorage.outputs.storageAccountId
output financeStorageAccountName string = financeStorage.outputs.storageAccountName
output financeStorageAccountId string = financeStorage.outputs.storageAccountId
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output resourceGroupName string = resourceGroup().name
