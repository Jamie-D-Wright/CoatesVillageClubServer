# Infrastructure Deployment - Quick Reference

## Prerequisites Check

```powershell
# Check Azure CLI
az --version

# Check .NET SDK
dotnet --version

# Check Azure Functions Core Tools
func --version

# Login to Azure
az login

# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription <subscription-id>
```

## Deployment Commands

### Deploy Infrastructure

```powershell
# Navigate to infrastructure directory
cd infrastructure

# Deploy to dev environment
.\deploy.ps1 -Environment dev -Location uksouth

# Deploy to staging
.\deploy.ps1 -Environment staging -Location uksouth

# Deploy to production
.\deploy.ps1 -Environment prod -Location uksouth

# What-if analysis (dry run)
.\deploy.ps1 -Environment dev -WhatIf

# Specify subscription explicitly
.\deploy.ps1 -Environment dev -SubscriptionId "your-sub-id"
```

**Prompts you'll see:**
- SQL Admin Login (default: sqladmin)
- SQL Admin Password (min 8 chars, complexity required)
- APIM Publisher Email (default: admin@coatesvillageclub.org)
- APIM Publisher Name (default: Coates Village Club)
- JWT Issuer (default: https://villageclub.coates.local)
- JWT Audience (default: villageclub-api)

**Time**: 10-15 minutes

### Deploy Membership Service

```powershell
# Navigate to Membership service
cd services\membership\src\VillageClub.Membership

# Build
dotnet build -c Release

# Run tests
cd ..\..\tests\VillageClub.Membership.Tests
dotnet test

# Deploy to Azure
cd ..\src\VillageClub.Membership
func azure functionapp publish func-villageclub-membership-dev
```

**Time**: 2-3 minutes

### Initialize Database

```powershell
# Navigate to Membership service
cd services\membership\src\VillageClub.Membership

# Update database with migrations
dotnet ef database update

# Or generate SQL script
dotnet ef migrations script --output ..\..\..\..\infrastructure\scripts\create-membership-schema.sql
```

**Time**: 1-2 minutes

## Verification Commands

### Check Deployment Status

```powershell
# Check resource group
az group show --name rg-villageclub-dev

# List all resources
az resource list --resource-group rg-villageclub-dev --output table

# Check deployment history
az deployment group list --resource-group rg-villageclub-dev --output table

# Get deployment outputs
az deployment group show `
  --resource-group rg-villageclub-dev `
  --name <deployment-name> `
  --query properties.outputs
```

### Test Endpoints

```powershell
# Health check (Function App)
curl https://func-villageclub-membership-dev.azurewebsites.net/api/v1/health

# Health check (via APIM)
curl https://apim-villageclub-dev.azure-api.net/membership/api/v1/health

# Service discovery
curl https://apim-villageclub-dev.azure-api.net/registry/v1/services

# Register user
curl -X POST https://apim-villageclub-dev.azure-api.net/membership/api/v1/auth/register `
  -H "Content-Type: application/json" `
  -d '{"email":"test@example.com","password":"Password123!","firstName":"Test","lastName":"User"}'

# Login
curl -X POST https://apim-villageclub-dev.azure-api.net/membership/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d '{"email":"test@example.com","password":"Password123!"}'
```

### View Logs

```powershell
# Function App logs (streaming)
func azure functionapp logstream func-villageclub-membership-dev

# Application Insights query
az monitor app-insights query `
  --app appi-villageclub-dev `
  --analytics-query "traces | where timestamp > ago(1h) | order by timestamp desc | take 50"

# SQL Database queries
az sql db show `
  --resource-group rg-villageclub-dev `
  --server sql-villageclub-dev `
  --name VillageClubDB
```

## Configuration Updates

### Update Function App Settings

```powershell
# Set app setting
az functionapp config appsettings set `
  --name func-villageclub-membership-dev `
  --resource-group rg-villageclub-dev `
  --settings "JwtSettings__AccessTokenExpiryMinutes=120"

# List current settings
az functionapp config appsettings list `
  --name func-villageclub-membership-dev `
  --resource-group rg-villageclub-dev
```

### Update APIM Backend

```powershell
# Get Function App URL
$membershipUrl = az functionapp show `
  --name func-villageclub-membership-dev `
  --resource-group rg-villageclub-dev `
  --query defaultHostName -o tsv

# Update APIM backend (re-deploy with parameter)
cd infrastructure
az deployment group create `
  --resource-group rg-villageclub-dev `
  --template-file main.bicep `
  --parameters @dev.parameters.json `
  --parameters membershipServiceUrl="https://$membershipUrl"
```

### Key Vault Secrets

```powershell
# Add secret
az keyvault secret set `
  --vault-name kv-villageclub-dev `
  --name "MySecret" `
  --value "SecretValue"

# Get secret
az keyvault secret show `
  --vault-name kv-villageclub-dev `
  --name "MySecret" `
  --query value -o tsv

# List secrets
az keyvault secret list `
  --vault-name kv-villageclub-dev `
  --output table
```

## Troubleshooting

### Common Issues

**Issue**: Deployment fails with "Location not available"
```powershell
# List available locations
az account list-locations --output table

# Use alternative location
.\deploy.ps1 -Environment dev -Location westeurope
```

**Issue**: SQL password doesn't meet complexity
```powershell
# Password requirements:
# - 8-128 characters
# - Uppercase letters (A-Z)
# - Lowercase letters (a-z)
# - Numbers (0-9)
# - Special characters (!@#$%^&*)
```

**Issue**: Function App can't access Key Vault
```powershell
# Check managed identity
az functionapp identity show `
  --name func-villageclub-membership-dev `
  --resource-group rg-villageclub-dev

# Grant access
az keyvault set-policy `
  --name kv-villageclub-dev `
  --object-id <principal-id> `
  --secret-permissions get list
```

**Issue**: Database connection fails
```powershell
# Check if database is paused
az sql db show `
  --resource-group rg-villageclub-dev `
  --server sql-villageclub-dev `
  --name VillageClubDB `
  --query status

# Check firewall rules
az sql server firewall-rule list `
  --resource-group rg-villageclub-dev `
  --server sql-villageclub-dev
```

**Issue**: Cold start too slow
```powershell
# Check function app metrics
az monitor metrics list `
  --resource /subscriptions/<sub-id>/resourceGroups/rg-villageclub-dev/providers/Microsoft.Web/sites/func-villageclub-membership-dev `
  --metric "AverageFunctionExecutionTime"

# Consider enabling Always On or Premium plan for production
```

## Cleanup

```powershell
# Delete entire environment
cd infrastructure
.\cleanup.ps1 -Environment dev

# Production requires force flag
.\cleanup.ps1 -Environment prod -Force
```

## Monitoring

### Azure Portal URLs

```powershell
# Resource group
https://portal.azure.com/#@/resource/subscriptions/<sub-id>/resourceGroups/rg-villageclub-dev

# Function App
https://portal.azure.com/#@/resource/subscriptions/<sub-id>/resourceGroups/rg-villageclub-dev/providers/Microsoft.Web/sites/func-villageclub-membership-dev

# Application Insights
https://portal.azure.com/#@/resource/subscriptions/<sub-id>/resourceGroups/rg-villageclub-dev/providers/Microsoft.Insights/components/appi-villageclub-dev

# APIM
https://portal.azure.com/#@/resource/subscriptions/<sub-id>/resourceGroups/rg-villageclub-dev/providers/Microsoft.ApiManagement/service/apim-villageclub-dev
```

### Key Metrics to Monitor

- **Function Executions**: Should be >0 after deployment
- **HTTP 5xx Errors**: Should be 0% or very low
- **Cold Start Duration**: First request after idle
- **Database DTU Usage**: Should be low in dev
- **APIM Gateway Requests**: Track API usage

## Next Steps After Deployment

1. ✅ Verify health endpoints return 200 OK
2. ✅ Test user registration and login
3. ✅ Verify JWT tokens are generated correctly
4. ✅ Check Application Insights for any errors
5. ✅ Test service discovery endpoint
6. ✅ Document deployed URLs in team wiki
7. 🔜 Set up monitoring alerts
8. 🔜 Configure custom domains (production)
9. 🔜 Enable DDoS protection (production)
10. 🔜 Set up backup policies (production)

## Reference Documentation

- [Infrastructure README](./README.md)
- [API Gateway Documentation](../docs/api-gateway.md)
- [Deployment Guide](../docs/deployment.md)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
