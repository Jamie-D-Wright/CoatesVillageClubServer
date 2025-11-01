# Azure Deployment URLs - Development Environment

**Last Updated**: 2025-11-01  
**Environment**: Development (dev)  
**Status**: ✅ Deployed and Verified

## Quick Reference

### API Gateway (Recommended Entry Point)
- **Base URL**: `https://cvc-apim-dev.azure-api.net`
- **Service Discovery**: `https://cvc-apim-dev.azure-api.net/registry/services`
- **Membership Health**: `https://cvc-apim-dev.azure-api.net/membership/api/v1/health`

### Direct Function App URLs
- **Membership Service**: `https://cvc-func-membership-dev.azurewebsites.net`
- **Health Check**: `https://cvc-func-membership-dev.azurewebsites.net/api/v1/health`
- **Authentication**: `https://cvc-func-membership-dev.azurewebsites.net/api/v1/auth/*`

## Detailed Service Information

### 1. API Management (APIM)

**Resource Name**: `cvc-apim-dev`  
**Base URL**: `https://cvc-apim-dev.azure-api.net`  
**Resource Group**: `rg-villageclub-dev`

#### Configured Endpoints

| Endpoint | Method | Purpose | Status |
|----------|--------|---------|--------|
| `/registry/services` | GET | Service discovery - list all services | ✅ Working |
| `/membership/api/v1/health` | GET | Membership service health check | ✅ Working |

**Response Example** (Service Discovery):
```json
{
  "services": [
    {
      "name": "Membership",
      "version": "v1",
      "basePath": "/membership/api/v1",
      "healthEndpoint": "/membership/api/v1/health",
      "openapiUrl": "/membership/swagger.json",
      "description": "User management, authentication, and authorization"
    }
  ],
  "timestamp": "2025-11-01T13:57:54.6096467Z"
}
```

**Notes**:
- CORS policy configured (currently `*` for development)
- JWT validation policy configured
- Rate limiting: Default (not yet configured)
- **Recommendation**: Add APIM operations for auth endpoints for complete gateway routing

### 2. Membership Service

**Function App**: `cvc-func-membership-dev`  
**Base URL**: `https://cvc-func-membership-dev.azurewebsites.net`  
**Resource Group**: `rg-villageclub-dev`  
**Runtime**: .NET 8 Isolated Worker (Flex Consumption Plan)

#### API Endpoints

| Endpoint | Method | Purpose | Status | Response |
|----------|--------|---------|--------|----------|
| `/api/v1/health` | GET | Health check | ✅ Working | 200 OK with JSON |
| `/api/v1/auth/register` | POST | User registration | ✅ Working | 201 Created |
| `/api/v1/auth/login` | POST | User authentication | ✅ Working | 200 OK |
| `/api/v1/auth/refresh` | POST | Token refresh | ⚠️ Not tested | - |
| `/api/v1/auth/logout` | POST | User logout | ⚠️ Not tested | - |
| `/api/v1/users/:id` | GET | Get user profile | ⚠️ Not tested | - |
| `/api/v1/users/:id` | PUT | Update user profile | ⚠️ Not tested | - |
| `/api/v1/users/:id` | DELETE | Delete user | ⚠️ Not tested | - |

**Authentication Requirements**:
- Registration: No authentication required (public endpoint)
- Login: No authentication required (public endpoint)
- All other endpoints: Require valid JWT Bearer token

**Request/Response Format**:
- Content-Type: `application/json; charset=utf-8`
- JSON property names: camelCase (e.g., `firstName`, `userId`)
- Error responses: Follow ErrorResponse DTO format

**Example Registration Request**:
```json
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Example Response** (201 Created):
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "8a7b9c6d-e5f4-3a2b-1c0d-9e8f7a6b5c4d",
  "expiresIn": 3600,
  "user": {
    "id": "f6e94ef5-d7b8-42af-b28d-1a81679febdd",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "roles": ["Member"],
    "createdAt": "2025-11-01T13:59:02.123Z"
  }
}
```

### 3. Application Insights

**Resource Name**: `cvc-insights-dev`  
**Resource Group**: `rg-villageclub-dev`  
**Portal URL**: [Application Insights Dashboard](https://portal.azure.com/#@/resource/subscriptions/aa442c43-6659-4c83-8951-4c151a8655fa/resourceGroups/rg-villageclub-dev/providers/microsoft.insights/components/cvc-insights-dev)

#### Configured Alerts

| Alert Name | Condition | Window | Frequency | Status |
|------------|-----------|--------|-----------|--------|
| High Error Rate - Membership | >5 failed requests | 5 minutes | 1 minute | ✅ Enabled |
| Slow API Response - Membership | Avg >2000ms | 5 minutes | 1 minute | ✅ Enabled |

**Monitoring Capabilities**:
- Request tracking and performance metrics
- Dependency tracking (SQL, HTTP)
- Exception and error tracking
- Custom events and metrics
- Live metrics stream

### 4. SQL Database

**Server**: `cvc-sql-dev.database.windows.net`  
**Database**: `cvc-db-dev`  
**Resource Group**: `rg-villageclub-dev`

**Connection String** (Application):
```
Server=tcp:cvc-sql-dev.database.windows.net,1433;Initial Catalog=cvc-db-dev;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication="Active Directory Default";
```

**Schema**: `membership`  
**Tables**:
- `membership.Users`
- `membership.RefreshTokens`

## Testing the Deployment

### 1. Health Check (APIM)
```powershell
Invoke-WebRequest -Uri "https://cvc-apim-dev.azure-api.net/membership/api/v1/health" -Method GET | ConvertFrom-Json
```

**Expected Response**:
```json
{
  "service": "Membership",
  "status": "Healthy",
  "version": "1.0.0",
  "timestamp": "2025-11-01T14:00:00.000Z"
}
```

### 2. Service Discovery
```powershell
Invoke-WebRequest -Uri "https://cvc-apim-dev.azure-api.net/registry/services" -Method GET | ConvertFrom-Json
```

### 3. User Registration (Direct Function App)
```powershell
$body = @{
    email = "test@example.com"
    password = "SecurePass123!"
    firstName = "Test"
    lastName = "User"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://cvc-func-membership-dev.azurewebsites.net/api/v1/auth/register" -Method POST -Body $body -ContentType "application/json"
```

**Note**: Direct Function App URLs return response content as byte arrays. Convert using:
```powershell
$response = Invoke-WebRequest -Uri "..." -Method POST -Body $body -ContentType "application/json"
$json = [System.Text.Encoding]::UTF8.GetString($response.Content) | ConvertFrom-Json
```

### 4. User Login
```powershell
$loginBody = @{
    email = "test@example.com"
    password = "SecurePass123!"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://cvc-func-membership-dev.azurewebsites.net/api/v1/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
```

## Known Issues and Limitations

### ✅ Resolved Issues
1. **Response Serialization** (Fixed)
   - **Issue**: Endpoints returned 204 No Content instead of JSON
   - **Resolution**: Added `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` package
   - **Change**: Updated `Program.cs` to use `ConfigureFunctionsWebApplication()` instead of `ConfigureFunctionsWorkerDefaults()`

2. **Route Mismatch** (Fixed)
   - **Issue**: Local used `/api/v1/*`, Azure used `/api/*`
   - **Resolution**: Added `AzureFunctionsJobHost__extensions__http__routePrefix=api/v1` to Azure app settings

3. **JWT Configuration** (Fixed)
   - **Issue**: Registration/login returned 500 Internal Server Error
   - **Resolution**: Added `JWT_PRIVATE_KEY` to Azure Function App settings

### ⚠️ Current Limitations
1. **APIM Operations Incomplete**
   - Only health and service registry endpoints configured in APIM
   - Auth endpoints (`/auth/register`, `/auth/login`) not routed through APIM gateway
   - Recommendation: Add APIM operations for complete gateway routing

2. **Direct Function App Access**
   - Function App may require function-level keys for direct access
   - APIM provides unified authentication and is the recommended entry point

3. **CORS Policy**
   - Currently configured to allow all origins (`*`) for development
   - **MUST** be restricted to specific origins before production deployment

## Configuration Summary

### Function App Settings
| Setting | Value | Purpose |
|---------|-------|---------|
| `AzureFunctionsJobHost__extensions__http__routePrefix` | `api/v1` | Route prefix alignment |
| `JWT_PRIVATE_KEY` | `<RSA private key>` | JWT token signing |
| `SqlConnectionString` | `Server=cvc-sql-dev...` | Database connection |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `InstrumentationKey=...` | Application monitoring |

## Next Steps

### Immediate Actions
- [X] D010: Verify health endpoint (direct) - ✅ Complete
- [X] D010A: Verify health endpoint (APIM) - ✅ Complete
- [X] D011: Verify authentication endpoints - ✅ Complete
- [X] D012: Verify service discovery - ✅ Complete
- [X] D013: Run deferred integration tests - ✅ Complete
- [X] D014: Configure Application Insights alerts - ✅ Complete
- [X] D015: Document deployed URLs - ✅ Complete

### Future Enhancements
- [ ] Add APIM operations for all Membership endpoints
- [ ] Configure APIM rate limiting and throttling
- [ ] Restrict CORS policy to specific frontend origins
- [ ] Configure custom domains for APIM and Function Apps
- [ ] Set up Azure Key Vault for secret management
- [ ] Configure auto-scaling rules for Function App
- [ ] Add more comprehensive Application Insights dashboards
- [ ] Configure alerts for SQL Database performance
- [ ] Set up continuous deployment from GitHub Actions

## Support and Troubleshooting

See `docs/DEPLOYMENT-TROUBLESHOOTING.md` for detailed troubleshooting guide.

**Common Issues**:
1. **401 Unauthorized**: Check JWT token validity and Function App authentication settings
2. **404 Not Found**: Verify route prefix configuration (`/api/v1/` vs `/api/`)
3. **500 Internal Server Error**: Check Application Insights logs and Function App logs
4. **CORS Errors**: Verify APIM CORS policy configuration

**Monitoring**:
- Application Insights: `https://portal.azure.com` → Search "cvc-insights-dev"
- Function App Logs: `https://portal.azure.com` → Search "cvc-func-membership-dev" → Logs
- APIM Analytics: `https://portal.azure.com` → Search "cvc-apim-dev" → Analytics

---

**Document Version**: 1.0  
**Last Verified**: 2025-11-01  
**Next Review**: Before production deployment
