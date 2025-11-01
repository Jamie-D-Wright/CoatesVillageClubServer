# Infrastructure Deployment

This directory contains the Azure infrastructure as code (IaC) using Bicep templates for the Village Club Management System.

## Architecture Overview

The infrastructure deploys the following Azure resources:

### Core Services
- **Azure Functions (Flex Consumption Plan)** - Serverless compute for microservices
  - Membership service
  - Events service (Phase 5)
  - Finance service (Phase 6)
  - Scheduling service (Phase 7)
  - Bar service (Phase 8)

### Data Storage
- **Azure SQL Database (Serverless)** - Shared database with schema isolation
  - Auto-pause after 60 minutes of inactivity
  - Scales from 0.5 to 2 vCores
  - Five schemas: Membership, Events, Scheduling, Bar, Finance

- **Blob Storage** - Receipt and document storage
  - Cool tier transition after 90 days
  - Lifecycle management policies

### Gateway & Management
- **API Management (Consumption)** - API gateway with:
  - Service discovery endpoint
  - JWT validation
  - CORS policies
  - Rate limiting (planned)

### Supporting Services
- **Application Insights** - Monitoring and logging
- **Key Vault** - Secrets management
- **Storage Account** - Function app storage

## Prerequisites

Before deploying, ensure you have:

1. **Azure CLI** (v2.50.0 or later)
   ```powershell
   winget install Microsoft.AzureCLI
   ```

2. **.NET 8.0 SDK**
   ```powershell
   winget install Microsoft.DotNet.SDK.8
   ```

3. **Azure Functions Core Tools v4**
   ```powershell
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

4. **Azure Subscription** with:
   - Contributor or Owner role
   - Resource providers registered:
     - Microsoft.Web
     - Microsoft.Storage
     - Microsoft.Sql
     - Microsoft.KeyVault
     - Microsoft.ApiManagement
     - Microsoft.Insights

## Quick Start

### 1. Deploy Infrastructure

Run the deployment script from the `infrastructure` directory:

```powershell
# Deploy to dev environment
.\deploy.ps1 -Environment dev -Location uksouth

# Deploy to staging
.\deploy.ps1 -Environment staging -Location uksouth

# Deploy to production (with confirmation prompts)
.\deploy.ps1 -Environment prod -Location uksouth

# Dry run (what-if analysis)
.\deploy.ps1 -Environment dev -WhatIf
```

The script will prompt you for:
- SQL Server admin credentials
- APIM publisher email and name
- JWT issuer and audience

**Deployment time**: Approximately 10-15 minutes (APIM takes the longest)

### 2. Deploy Membership Service

After infrastructure is deployed:

```powershell
cd ..\services\membership\src\VillageClub.Membership

# Build the application
dotnet build -c Release

# Deploy to Azure
func azure functionapp publish func-villageclub-membership-dev
```

### 3. Initialize Database Schemas

The database schemas are created via Entity Framework migrations:

```powershell
cd ..\services\membership\src\VillageClub.Membership

# Apply migrations
dotnet ef database update

# Or create a SQL script
dotnet ef migrations script --output ..\..\..\..\infrastructure\scripts\create-membership-schema.sql
```

### 4. Configure APIM Backend

Update the APIM backend with the Membership service URL:

```powershell
# Get the function app URL
$membershipUrl = az functionapp show `
  --name func-villageclub-membership-dev `
  --resource-group rg-villageclub-dev `
  --query defaultHostName -o tsv

# Re-deploy APIM with the backend URL
az deployment group create `
  --resource-group rg-villageclub-dev `
  --template-file main.bicep `
  --parameters membershipServiceUrl="https://$membershipUrl"
```

### 5. Verify Deployment

Test the health endpoint:

```powershell
# Via Function App directly
curl https://func-villageclub-membership-dev.azurewebsites.net/api/v1/health

# Via API Gateway (once APIM configured)
curl https://apim-villageclub-dev.azure-api.net/membership/api/v1/health
```

## Infrastructure Components

### Main Template (`main.bicep`)

The main template orchestrates all resources and includes:
- Resource naming conventions
- Resource group deployment
- Module composition
- Outputs for dependent deployments

### Modules

#### `modules/function-app.bicep`
- Azure Function App (Consumption plan)
- Managed identity
- Application settings
- Key Vault integration
- CORS configuration

#### `modules/sql-database.bicep`
- Azure SQL Server
- Serverless SQL Database
- Firewall rules (Azure services only)
- Auto-pause configuration
- Audit logging (optional)

#### `modules/blob-storage.bicep`
- Storage account
- Blob containers
- Lifecycle management policies
- Cool tier transition
- Soft delete (30 days)

#### `modules/apim.bicep`
- API Management service (Consumption tier)
- Service discovery API
- Backend definitions
- Global policies (JWT validation, CORS)
- Named values

## Environments

### Development (`dev`)
- Single region deployment
- Auto-pause enabled
- Minimal capacity
- Development CORS settings

### Staging (`staging`)
- Production-like configuration
- Testing environment
- Blue-green deployment target

### Production (`prod`)
- High availability
- Backup policies
- Monitoring alerts
- Rate limiting
- DDoS protection

## Cost Optimization

The infrastructure is designed for cost efficiency:

1. **Consumption Plans**: Pay only for execution time
2. **Serverless SQL**: Auto-pause when idle, scales to 0.5 vCore
3. **Lifecycle Policies**: Move old receipts to cool storage
4. **APIM Consumption**: Pay per API call
5. **Dev Environment**: Auto-pause saves ~£50/month when not in use

**Estimated monthly costs** (dev environment, low usage):
- Azure Functions: £5-10
- SQL Database: £5-15 (with auto-pause)
- Storage: £1-2
- APIM: £2-5
- Application Insights: £5
- **Total: £18-37/month**

## Security

### Managed Identity
All services use managed identities (no connection strings in code):
- Function apps access Key Vault via managed identity
- SQL connections use Azure AD authentication
- Blob storage access uses managed identity

### Secrets Management
- Secrets stored in Azure Key Vault
- No secrets in source code or configuration
- Automatic rotation support

### Network Security
- SQL firewall allows Azure services only
- HTTPS only on all endpoints
- CORS configured per environment
- API Management acts as security gateway

### Authentication & Authorization
- JWT-based authentication using VillageClub.Auth library
- RSA 2048-bit key signing
- Role-based access control (Committee, Volunteer, Member)
- Token validation at API Gateway

## Monitoring

### Application Insights
All services log to Application Insights:
- Structured logging with Serilog
- Correlation IDs for distributed tracing
- Custom metrics and events
- Performance telemetry

### Health Checks
Each service exposes a health endpoint:
- `/api/v1/health` - Service health status
- Includes JWT public key for validation
- Monitored by APIM backend health checks

### Alerts (Planned)
- High error rate (>5% failed requests)
- Long response times (>2s P95)
- Database throttling
- Cold start frequency

## Troubleshooting

### Deployment Fails

**Issue**: SQL admin password doesn't meet complexity requirements
**Solution**: Password must be 8-128 characters with uppercase, lowercase, numbers, and special characters

**Issue**: APIM deployment timeout
**Solution**: APIM Consumption tier can take 5-10 minutes. Use `--no-wait` flag for async deployment

**Issue**: Function app can't access Key Vault
**Solution**: Check managed identity is enabled and Key Vault access policy is configured

### Function App Issues

**Issue**: 500 Internal Server Error on startup
**Solution**: Check Application Insights for exceptions. Common causes:
- Missing app settings
- Database connection string incorrect
- Key Vault secrets not accessible

**Issue**: Cold start taking too long
**Solution**: 
- Enable pre-warming timer trigger (Fri/Sat 7:45pm)
- Consider upgrading to Premium plan for production
- Check function initialization code

### Database Issues

**Issue**: Cannot connect to SQL Server
**Solution**: 
- Check firewall rules include Azure services
- Verify connection string format
- Check if database is paused (serverless)

**Issue**: Database auto-paused during working hours
**Solution**: Increase `autoPauseDelay` parameter or disable auto-pause

## Manual Steps

Some steps require manual intervention:

1. **Database Schemas**: Run EF migrations after initial deployment
2. **SSL Certificates**: Configure custom domains in APIM
3. **DDoS Protection**: Enable in production
4. **Backup Policies**: Configure SQL backup retention
5. **Monitoring Alerts**: Set up alert rules in Azure Monitor

## Cleanup

To delete all resources:

```powershell
# Delete resource group (removes all resources)
az group delete --name rg-villageclub-dev --yes --no-wait

# Or use the cleanup script
.\cleanup.ps1 -Environment dev
```

⚠️ **Warning**: This is irreversible and will delete all data!

## References

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Azure SQL Serverless](https://docs.microsoft.com/azure/azure-sql/database/serverless-tier-overview)
- [API Management](https://docs.microsoft.com/azure/api-management/)
- [Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [API Gateway Documentation](../docs/api-gateway.md)
- [Deployment Guide](../docs/deployment.md)

## Support

For issues or questions:
1. Check Application Insights logs
2. Review Azure Portal resource health
3. Consult `docs/operations/incident-response.md`
4. Contact the platform team
