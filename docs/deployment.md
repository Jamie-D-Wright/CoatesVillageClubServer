# Deployment Guide: Village Club Service

## Prerequisites
- Azure CLI
- .NET 8.0 SDK
- Azure Functions Core Tools v4
- PowerShell 7.0 or later

## Environment Setup

1. Login to Azure:
```powershell
az login
```

2. Select subscription:
```powershell
az account set --subscription <subscription-id>
```

3. Create resource group:
```powershell
$env = "dev"  # or "prod", "staging", etc.
az group create --name rg-villageclub-$env --location westeurope
```

4. Deploy infrastructure:
```powershell
az deployment group create --resource-group rg-villageclub-$env --template-file infrastructure/main.bicep --parameters environmentName=$env
```

## Application Deployment

1. Build the application:
```powershell
dotnet build src/VillageClub.Functions/VillageClub.Functions.csproj -c Release
```

2. Run tests:
```powershell
dotnet test
```

3. Deploy to Azure Functions:
```powershell
cd src/VillageClub.Functions
func azure functionapp publish func-villageclub-$env
```

## Key Vault Setup

1. Add secrets to Key Vault:
```powershell
$kvName = "kv-villageclub-$env"
az keyvault secret set --vault-name $kvName --name "SecretName" --value "SecretValue"
```

2. Verify managed identity access:
```powershell
az keyvault show --name $kvName
```

## Environment Configuration

1. Application settings are managed in:
   - Local: `local.settings.json`
   - Azure: Function App Configuration

2. Required settings:
   - ServiceSettings:ApplicationName
   - ServiceSettings:EnvironmentName
   - ServiceSettings:LogLevel
   - ServiceSettings:Port
   - APPLICATIONINSIGHTS_CONNECTION_STRING
   - KeyVaultName

3. Update Azure settings:
```powershell
az functionapp config appsettings set --name func-villageclub-$env --resource-group rg-villageclub-$env --settings "ServiceSettings:LogLevel=Information"
```

## Local Debugging

1. Start the Functions host locally:
```powershell
cd src/VillageClub.Functions
func start --csharp
```

2. Debug in VS Code:
   - Press F5 or use Run > Start Debugging
   - Select ".NET 8 Functions" configuration
   - Set breakpoints in your code
   - Function endpoints will be available at `http://localhost:7071`

3. Attach debugger to running process:
   - Start the Functions host (step 1)
   - Run > Attach to Process
   - Select the `func.exe` process

4. Debug settings in `.vscode/launch.json`:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET 8 Functions",
            "type": "coreclr",
            "request": "attach",
            "processId": "${command:azureFunctions.pickProcess}",
            "justMyCode": true
        }
    ]
}
```

## Troubleshooting Guide

1. Check Application Insights:
   - View logs in Azure Portal
   - Monitor performance metrics
   - Track dependencies

2. Common issues:
   - Cold start delays: Check function timeout settings
   - Memory issues: Monitor `/health` endpoint
   - Configuration: Verify Key Vault access and settings
   - Missing local.settings.json: Copy from example and update values
   - Port conflicts: Change port in local.settings.json
   - Authentication errors: Check Azure credentials and Key Vault access

3. Debug logging:
   - Set "ServiceSettings:LogLevel": "Debug" in local.settings.json
   - View logs in VS Code Debug Console
   - Check Terminal window running Functions host

4. Health check:
```http
GET https://func-villageclub-{env}.azurewebsites.net/api/health
```

4. Service information:
```http
GET https://func-villageclub-{env}.azurewebsites.net/api/info
```

## Support

For issues:
1. Check Application Insights logs
2. Review `/health` endpoint status
3. Verify configuration in Azure Portal
4. Contact development team