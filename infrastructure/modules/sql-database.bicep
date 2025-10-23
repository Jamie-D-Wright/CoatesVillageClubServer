@description('Name of the SQL Server')
param sqlServerName string

@description('Location for the SQL Server')
param location string = resourceGroup().location

@description('SQL Administrator login name')
@secure()
param sqlAdminLogin string

@description('SQL Administrator password')
@secure()
param sqlAdminPassword string

@description('Name of the database')
param databaseName string = 'VillageClubDB'

@description('SQL Server Edition')
@allowed([
  'Basic'
  'Standard'
  'Premium'
  'GeneralPurpose'
])
param edition string = 'GeneralPurpose'

@description('Enable serverless compute')
param enableServerless bool = true

@description('Minimum vCores for serverless (0.5, 0.75, 1, etc.)')
param minCapacity string = '0.5'

@description('Maximum vCores for serverless')
param maxCapacity string = '2'

@description('Auto-pause delay in minutes (-1 to disable)')
param autoPauseDelay int = 60

@description('Tags to apply to resources')
param tags object = {}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Firewall rule to allow Azure services
resource firewallRuleAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Database
resource database 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: enableServerless ? 'GP_S_Gen5' : 'GP_Gen5'
    tier: edition
    capacity: enableServerless ? int(maxCapacity) : 2
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    autoPauseDelay: enableServerless ? autoPauseDelay : -1
    minCapacity: enableServerless ? json(minCapacity) : null
  }
}

// Enable auditing
resource auditingSettings 'Microsoft.Sql/servers/auditingSettings@2023-05-01-preview' = {
  parent: sqlServer
  name: 'default'
  properties: {
    state: 'Enabled'
    isAzureMonitorTargetEnabled: true
    retentionDays: 90
  }
}

@description('SQL Server FQDN')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Database name')
output databaseName string = database.name

@description('SQL Server resource ID')
output sqlServerId string = sqlServer.id

@description('Database resource ID')
output databaseId string = database.id
