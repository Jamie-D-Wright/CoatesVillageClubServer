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

@description('Membership service backend URL')
param membershipServiceUrl string = ''

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

// Named value for Membership service URL (used in JWT validation)
resource membershipServiceUrlValue 'Microsoft.ApiManagement/service/namedValues@2023-05-01-preview' = if (membershipServiceUrl != '') {
  parent: apimService
  name: 'membership-service-url'
  properties: {
    displayName: 'membership-service-url'
    value: membershipServiceUrl
    secret: false
  }
}

// Membership Service Backend
resource membershipBackend 'Microsoft.ApiManagement/service/backends@2023-05-01-preview' = if (membershipServiceUrl != '') {
  parent: apimService
  name: 'membership-backend'
  properties: {
    description: 'Membership Service Backend'
    url: membershipServiceUrl
    protocol: 'http'
  }
}

// Membership API
resource membershipApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = if (membershipServiceUrl != '') {
  parent: apimService
  name: 'membership-api'
  properties: {
    displayName: 'Membership API'
    description: 'User management, authentication, and authorization'
    path: 'membership'
    protocols: [
      'https'
    ]
    subscriptionRequired: false
    serviceUrl: membershipServiceUrl
  }
}

// Membership API policy - route to backend with health check
resource membershipApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = if (membershipServiceUrl != '') {
  parent: membershipApi
  name: 'policy'
  properties: {
    value: '''
<policies>
  <inbound>
    <base />
    <set-backend-service backend-id="membership-backend" />
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
'''
    format: 'xml'
  }
}

// Health check operation for Membership API
resource membershipHealthOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = if (membershipServiceUrl != '') {
  parent: membershipApi
  name: 'health'
  properties: {
    displayName: 'Health Check'
    method: 'GET'
    urlTemplate: '/api/v1/health'
    description: 'Check health status of Membership service'
    responses: [
      {
        statusCode: 200
        description: 'Service is healthy'
        representations: [
          {
            contentType: 'application/json'
          }
        ]
      }
    ]
  }
}

// Service Discovery API
resource serviceDiscoveryApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  parent: apimService
  name: 'service-registry'
  properties: {
    displayName: 'Service Registry'
    description: 'Service discovery and health check aggregation'
    path: 'registry'
    protocols: [
      'https'
    ]
    subscriptionRequired: false
    isCurrent: true
  }
}

// Service Discovery Endpoint - List all services
resource serviceListOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: serviceDiscoveryApi
  name: 'list-services'
  properties: {
    displayName: 'List All Services'
    method: 'GET'
    urlTemplate: '/services'
    description: 'Returns metadata for all registered services including health status and OpenAPI URLs'
    responses: [
      {
        statusCode: 200
        description: 'Success'
        representations: [
          {
            contentType: 'application/json'
          }
        ]
      }
    ]
  }
}

// Policy for service list operation - returns hardcoded service registry
resource serviceListPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2023-05-01-preview' = {
  parent: serviceListOperation
  name: 'policy'
  properties: {
    value: '''
<policies>
  <inbound>
    <base />
    <return-response>
      <set-status code="200" reason="OK" />
      <set-header name="Content-Type" exists-action="override">
        <value>application/json</value>
      </set-header>
      <set-body>@{
        var services = new JArray();
        
        var membership = new JObject();
        membership["name"] = "Membership";
        membership["version"] = "v1";
        membership["basePath"] = "/membership/api/v1";
        membership["healthEndpoint"] = "/membership/api/v1/health";
        membership["openapiUrl"] = "/membership/swagger.json";
        membership["description"] = "User management, authentication, and authorization";
        services.Add(membership);
        
        var result = new JObject();
        result["services"] = services;
        result["timestamp"] = DateTime.UtcNow.ToString("o");
        
        return result.ToString();
      }</set-body>
    </return-response>
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
'''
    format: 'xml'
  }
}

// Global policy for all APIs - CORS and error handling
resource globalPolicy 'Microsoft.ApiManagement/service/policies@2023-05-01-preview' = {
  parent: apimService
  name: 'policy'
  properties: {
    value: '''
<policies>
  <inbound>
    <cors allow-credentials="true">
      <allowed-origins>
        <origin>http://localhost:3000</origin>
        <origin>http://localhost:5173</origin>
      </allowed-origins>
      <allowed-methods>
        <method>GET</method>
        <method>POST</method>
        <method>PUT</method>
        <method>DELETE</method>
        <method>OPTIONS</method>
      </allowed-methods>
      <allowed-headers>
        <header>*</header>
      </allowed-headers>
      <expose-headers>
        <header>*</header>
      </expose-headers>
    </cors>
  </inbound>
  <backend>
    <forward-request />
  </backend>
  <outbound>
  </outbound>
  <on-error>
    <set-header name="Content-Type" exists-action="override">
      <value>application/json</value>
    </set-header>
    <set-body>@{
      return new JObject(
        new JProperty("message", context.LastError.Message),
        new JProperty("code", "GATEWAY_ERROR"),
        new JProperty("timestamp", DateTime.UtcNow.ToString("o"))
      ).ToString();
    }</set-body>
  </on-error>
</policies>
'''
    format: 'xml'
  }
}

@description('API Management Service Name')
output apimName string = apimService.name

@description('API Management Gateway URL')
output apimGatewayUrl string = apimService.properties.gatewayUrl

@description('API Management Resource ID')
output apimId string = apimService.id

@description('Service Registry URL')
output serviceRegistryUrl string = '${apimService.properties.gatewayUrl}/registry/v1/services'
