# Quickstart Guide: Village Club Service Scaffold

## Prerequisites
- .NET 8.0 SDK or later
- Azure Functions Core Tools v4
- Visual Studio 2022 or VS Code with Azure Functions extension
- Azure CLI (for deployment)
- PowerShell or Command Prompt

## Local Development Setup

1. Clone the repository:
```powershell
git clone <repository-url>
cd VillageClubService
```

2. Install Azure Functions Core Tools (if not already installed):
```powershell
npm install -g azure-functions-core-tools@4
```

3. Build the solution:
```powershell
dotnet build
```

4. Run the tests:
```powershell
dotnet test
```

5. Start the Functions locally:
```powershell
cd src/VillageClub.Functions
func start
```

The Functions will start on `http://localhost:7071` by default.

## Configuration

### local.settings.json
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceSettings:ApplicationName": "VillageClubService",
    "ServiceSettings:EnvironmentName": "Development",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "",
    "KeyVaultName": ""
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

## Verify Installation

1. Check the service health:
```http
GET http://localhost:7071/api/health
```

2. Get service information:
```http
GET http://localhost:7071/api/info
```

## Development Workflow

1. Create a new feature branch:
```powershell
git checkout -b feature/your-feature-name
```

2. Make your changes
3. Run tests:
```powershell
dotnet test
```

4. Run locally:
```powershell
func start
```

## Azure Deployment

1. Create Azure resources:
```powershell
az login
az group create --name rg-village-club --location westeurope
az deployment group create --resource-group rg-village-club --template-file infrastructure/main.bicep
```

2. Deploy the Functions:
```powershell
func azure functionapp publish village-club-function
```

## Common Tasks

### Update Configuration
- Local: Edit `local.settings.json`
- Azure: Use Azure Portal or Azure CLI:
```powershell
az functionapp config appsettings set --name village-club-function --resource-group rg-village-club --settings "ServiceSettings:EnvironmentName=Production"
```

### View Logs
- Local: Console output
- Azure: Application Insights or Azure Portal

### Debug
1. Open in Visual Studio or VS Code
2. Set breakpoints
3. Use F5 to debug locally
4. Use Azure Functions extension for remote debugging

## Support

For issues or questions:
1. Check the logs
2. Verify configuration
3. Ensure all prerequisites are met
4. Contact the development team