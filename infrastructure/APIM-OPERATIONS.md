# API Management Operations Configuration

## Overview

The `modules/apim.bicep` template now automatically provisions **all API operations** for both Membership and Events services. This eliminates the need to manually configure APIM routes after deployment.

## Configured Operations

### Membership API (`/membership`)

| Operation | Method | Endpoint | Description |
|-----------|--------|----------|-------------|
| health | GET | `/api/v1/health` | Health check |
| ready | GET | `/api/v1/ready` | Readiness check |
| login | POST | `/api/v1/auth/login` | User authentication |
| register | POST | `/api/v1/auth/register` | User registration |
| refresh | POST | `/api/v1/auth/refresh` | Token refresh |

**Total**: 5 operations

### Events API (`/events`)

| Operation | Method | Endpoint | Description |
|-----------|--------|----------|-------------|
| health | GET | `/api/v1/health` | Health check |
| ready | GET | `/api/v1/ready` | Readiness check |
| create-event | POST | `/api/v1/events` | Create new event (Committee) |
| list-events | GET | `/api/v1/events` | List events (paginated, filterable) |
| get-event | GET | `/api/v1/events/{id}` | Get event by ID |
| update-event | PUT | `/api/v1/events/{id}` | Update event (Committee) |
| delete-event | DELETE | `/api/v1/events/{id}` | Delete event (Committee) |
| publish-event | POST | `/api/v1/events/{id}/publish` | Publish event (Committee) |
| complete-event | POST | `/api/v1/events/{id}/complete` | Complete event (Committee) |
| cancel-event | POST | `/api/v1/events/{id}/cancel` | Cancel event (Committee) |

**Total**: 10 operations

## Backend Configuration

Both APIs are configured with:
- **Backend routing**: Automatic routing to respective Azure Function apps
- **CORS**: Enabled for localhost development (ports 3000, 5173)
- **Error handling**: Standardized JSON error responses
- **No subscription required**: Public access (authentication handled by services)

## Deployment

Operations are automatically created when you deploy infrastructure:

```powershell
# From infrastructure/ directory
.\deploy.ps1 -Environment dev -Location uksouth
```

After deployment, all 15 operations (5 Membership + 10 Events) will be available immediately via:
- `https://cvc-apim-dev.azure-api.net/membership/*`
- `https://cvc-apim-dev.azure-api.net/events/*`

## Verification

To verify all operations are configured:

```powershell
# List Membership operations
az apim api operation list `
  --resource-group rg-villageclub-dev `
  --service-name cvc-apim-dev `
  --api-id membership-api `
  --query "[].{Name:name,Method:method,URL:urlTemplate}" `
  -o table

# List Events operations
az apim api operation list `
  --resource-group rg-villageclub-dev `
  --service-name cvc-apim-dev `
  --api-id events-api `
  --query "[].{Name:name,Method:method,URL:urlTemplate}" `
  -o table
```

Expected output:
- **Membership**: 5 operations
- **Events**: 10 operations

## Benefits

✅ **No manual configuration** - Operations created automatically  
✅ **Consistent deployment** - Same operations every time  
✅ **Version controlled** - All routes defined in Bicep  
✅ **Fast redeployment** - Rebuild environments in minutes  
✅ **Documentation** - Operations self-documented in template  

## Adding New Operations

To add a new operation to either API:

1. **Edit** `infrastructure/modules/apim.bicep`
2. **Add** new `resource` block following existing pattern:
   ```bicep
   resource eventsNewOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = if (eventsServiceUrl != '') {
     parent: eventsApi
     name: 'new-operation'
     properties: {
       displayName: 'New Operation'
       method: 'POST'
       urlTemplate: '/api/v1/events/{id}/newaction'
       description: 'Description of new operation'
       templateParameters: [
         {
           name: 'id'
           type: 'string'
           required: true
           description: 'Event ID (GUID)'
         }
       ]
     }
   }
   ```
3. **Deploy** infrastructure to apply changes
4. **Update** this documentation

## Template Parameters

APIM operations use these template parameters for dynamic routes:

- `{id}` - Event ID (GUID) - Used in all event-specific operations

Example expansion:
- Template: `/api/v1/events/{id}/publish`
- Actual: `/api/v1/events/a1b2c3d4-e5f6-7890-abcd-ef1234567890/publish`

## Troubleshooting

### Operations not appearing after deployment

1. Verify Bicep deployment succeeded:
   ```powershell
   az deployment group show --resource-group rg-villageclub-dev --name <deployment-name>
   ```

2. Check APIM service is in "Online" state:
   ```powershell
   az apim show --resource-group rg-villageclub-dev --name cvc-apim-dev --query "provisioningState"
   ```

3. Manually list operations (see Verification section above)

### Backend routing not working

1. Verify backend URLs are set correctly:
   ```powershell
   az apim backend list --resource-group rg-villageclub-dev --service-name cvc-apim-dev -o table
   ```

2. Test backend directly (bypass APIM):
   ```powershell
   curl https://cvc-func-membership-dev.azurewebsites.net/api/v1/health
   curl https://cvc-func-events-dev.azurewebsites.net/api/v1/health
   ```

3. Check APIM policy is applied:
   ```powershell
   az apim api policy show --resource-group rg-villageclub-dev --service-name cvc-apim-dev --api-id membership-api
   ```

## References

- [Azure APIM Bicep Documentation](https://learn.microsoft.com/azure/templates/microsoft.apimanagement/service)
- [APIM Policy Reference](https://learn.microsoft.com/azure/api-management/api-management-policies)
- Main Bicep template: `infrastructure/main.bicep`
- APIM module: `infrastructure/modules/apim.bicep`
