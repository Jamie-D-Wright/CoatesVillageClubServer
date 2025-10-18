@description('API Management service name')
param apimName string

@description('Location for API Management')
param location string = resourceGroup().location

@description('Publisher email')
param publisherEmail string

@description('Publisher name')
param publisherName string

@description('SKU for API Management')
@allowed([
  'Consumption'
  'Developer'
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Consumption'

@description('Tags to apply to resources')
param tags object = {}

// API Management Service
resource apimService 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: apimName
  location: location
  tags: tags
  sku: {
    name: sku
    capacity: sku == 'Consumption' ? 0 : 1
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
  }
}

@description('API Management Service Name')
output apimName string = apimService.name

@description('API Management Gateway URL')
output apimGatewayUrl string = apimService.properties.gatewayUrl

@description('API Management Resource ID')
output apimId string = apimService.id
