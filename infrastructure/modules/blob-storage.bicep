@description('Storage Account Name for receipts')
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

@description('Blob container name for receipts')
param receiptsContainerName string = 'receipts'

@description('Days before transitioning to Cool tier')
param coolTierTransitionDays int = 90

@description('Tags to apply to resources')
param tags object = {}

// Storage Account
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
}

// Receipts Container
resource receiptsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: receiptsContainerName
  properties: {
    publicAccess: 'None'
  }
}

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
              }
            }
            filters: {
              blobTypes: [
                'blockBlob'
              ]
              prefixMatch: [
                '${receiptsContainerName}/'
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

@description('Storage Account Primary Endpoint')
output storageAccountPrimaryEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('Receipts Container Name')
output receiptsContainerName string = receiptsContainer.name

@description('Storage Account Resource ID')
output storageAccountId string = storageAccount.id
