@description('Storage Account Name')
param storageAccountName string

@description('Location for the storage account')
param location string = resourceGroup().location

@description('Storage Account SKU')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
  'Standard_ZRS'
  'Premium_LRS'
])
param storageAccountSku string = 'Standard_LRS'

@description('Array of container names to create')
param containerNames array = [
  'receipts'
  'documents'
  'exports'
]

@description('Days before transitioning to Cool tier')
param coolTierTransitionDays int = 90

@description('Days before deleting old files (0 = no deletion)')
param deleteAfterDays int = 2555  // 7 years default

@description('Tags to apply to resources')
param tags object = {}

// General Purpose Storage Account for Coates Village Club
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: storageAccountSku
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// Blob Service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 30
    }
  }
}

// Create containers dynamically
resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = [for containerName in containerNames: {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}]

// Lifecycle Management Policy
resource lifecyclePolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          enabled: true
          name: 'MoveToCoolTier'
          type: 'Lifecycle'
          definition: {
            actions: {
              baseBlob: {
                tierToCool: {
                  daysAfterModificationGreaterThan: coolTierTransitionDays
                }
                delete: deleteAfterDays > 0 ? {
                  daysAfterModificationGreaterThan: deleteAfterDays
                } : null
              }
            }
            filters: {
              blobTypes: [
                'blockBlob'
              ]
            }
          }
        }
      ]
    }
  }
}

@description('Storage Account Name')
output storageAccountName string = storageAccount.name

@description('Storage Account Primary Blob Endpoint')
output storageAccountPrimaryEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('Storage Account Resource ID')
output storageAccountId string = storageAccount.id

@description('Container Names')
output containerNames array = containerNames
