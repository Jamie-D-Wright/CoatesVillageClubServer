# Azure Infrastructure Deployment - In Progress

**Date**: October 19, 2025  
**Environment**: dev  
**Location**: uksouth  
**Status**: 🚀 **DEPLOYMENT IN PROGRESS**

## Deployment Progress

### ✅ Completed Steps

1. **Prerequisites Check** - Azure CLI, .NET SDK, Functions Core Tools verified
2. **Azure Login** - Authenticated as wright.jamied@gmail.com
3. **Subscription Selected** - Coates Village Club (aa442c43-6659-4c83-8951-4c151a8655fa)
4. **Resource Group Created** - `rg-villageclub-dev` in uksouth

### ⏳ In Progress

**Step 6: Collecting Deployment Parameters**
- Prompting for SQL Server administrator credentials
- Prompting for APIM publisher details
- Prompting for JWT configuration

### ⏭️ Next Steps (After Parameters)

7. Deploy Azure Resources (10-15 minutes)
   - Azure SQL Database (Serverless)
   - Azure Functions (Membership service)
   - API Management (Consumption tier)
   - Key Vault
   - Blob Storage (Receipts)
   - Storage Account (Function app)
   - Application Insights

8. Configure Access Policies
   - Grant Function App access to Key Vault
   - Configure managed identities

9. Display Deployment Outputs
   - Function App URLs
   - SQL Server FQDN
   - APIM Gateway URL
   - Key Vault name
   - Storage account details

10. Provide Next Steps
    - Deploy Membership service code
    - Run database migrations
    - Configure APIM backend
    - Verify endpoints

## Resource Details

### Resource Group
- **Name**: rg-villageclub-dev
- **Location**: uksouth
- **ID**: /subscriptions/aa442c43-6659-4c83-8951-4c151a8655fa/resourceGroups/rg-villageclub-dev
- **Provisioning State**: Succeeded ✅

### Resources to be Created

| Resource Type | Name Pattern | Purpose |
|--------------|-------------|---------|
| Function App | func-villageclub-membership-dev | Membership service |
| App Service Plan | asp-villageclub-dev | Consumption plan for Functions |
| Application Insights | appi-villageclub-dev | Monitoring and logging |
| Key Vault | kv-villageclub-dev | Secrets management |
| Storage Account | stfuncvillageclub-dev | Function app storage |
| SQL Server | sql-villageclub-dev | Database server |
| SQL Database | VillageClubDB | Application database |
| API Management | apim-villageclub-dev | API Gateway |
| Storage Account | streceiptsvillageclub-dev | Receipt storage |

## Interactive Prompts

The deployment script will prompt for:

1. **SQL Admin Login** (default: sqladmin)
   - Username for SQL Server administrator

2. **SQL Admin Password**
   - Must be 8-128 characters
   - Requires uppercase, lowercase, numbers, and special characters

3. **APIM Publisher Email** (default: admin@coatesvillageclub.org)
   - Contact email for API Management

4. **APIM Publisher Name** (default: Coates Village Club)
   - Organization name for API Management

5. **JWT Issuer** (default: https://villageclub.coates.local)
   - Token issuer URL for JWT validation

6. **JWT Audience** (default: villageclub-api)
   - Expected audience in JWT tokens

## Estimated Timeline

- **Resource Group Creation**: 30 seconds ✅
- **Parameter Collection**: 2-3 minutes ⏳
- **Infrastructure Deployment**: 10-15 minutes ⏭️
- **Access Policy Configuration**: 1-2 minutes ⏭️
- **Total**: ~15-20 minutes

## Cost Estimate (Dev Environment)

| Resource | Estimated Monthly Cost |
|----------|----------------------|
| Azure Functions (Consumption) | £5-10 |
| SQL Database (Serverless, auto-pause) | £5-15 |
| Storage Accounts (2x) | £1-2 |
| APIM (Consumption) | £2-5 |
| Application Insights | £5 |
| **Total** | **£18-37/month** |

*Note: Dev environment with auto-pause enabled saves ~£50/month*

## Post-Deployment Tasks

After infrastructure deployment completes:

### 1. Deploy Membership Service
```powershell
cd services\membership\src\VillageClub.Membership
func azure functionapp publish func-villageclub-membership-dev
```

### 2. Initialize Database
```powershell
dotnet ef database update
```

### 3. Configure APIM Backend
Update APIM with the deployed Function App URL

### 4. Verify Deployment
- Test health endpoint
- Register a test user
- Login and verify JWT token
- Test service discovery endpoint

## Monitoring

Once deployed, monitor at:
- **Azure Portal**: https://portal.azure.com
- **Resource Group**: rg-villageclub-dev
- **Application Insights**: appi-villageclub-dev

## Troubleshooting

If deployment fails:
1. Check Azure Portal for detailed error messages
2. Review deployment logs in terminal
3. Verify subscription has sufficient permissions
4. Check resource provider registrations
5. See `infrastructure/QUICKSTART.md` for common issues

## References

- [Infrastructure README](./README.md)
- [Quick Reference Guide](./QUICKSTART.md)
- [API Gateway Documentation](../docs/api-gateway.md)
- [Deployment Summary](./DEPLOYMENT-SUMMARY.md)

---

**Status**: Deployment in progress - waiting for parameter input ⏳  
**Last Updated**: October 19, 2025 14:57 UTC
