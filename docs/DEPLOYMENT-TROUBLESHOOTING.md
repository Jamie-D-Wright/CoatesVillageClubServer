# Deployment Troubleshooting Guide

**Date**: 2025-10-25  
**Issue**: Azure Function App returning 204 No Content for all endpoints instead of proper JSON responses  
**Status**: Under Investigation

## Problem Summary

After deploying the Membership service to Azure (`cvc-func-membership-dev`), all HTTP endpoints return `204 No Content` instead of the expected JSON responses with proper status codes.

### Expected vs Actual Behavior

| Endpoint | Expected | Actual |
|----------|----------|--------|
| `POST /api/v1/auth/register` | `201 Created` with `AuthResponse` JSON | `204 No Content` (empty) |
| `POST /api/v1/auth/login` | `200 OK` with `AuthResponse` JSON | `400 Bad Request` (empty)|  
| `GET /api/v1/health` | `204 No Content` | `204 No Content` ✅ |

### Investigation Findings

#### ✅ Local Execution Works
- Functions start correctly with `func start`
- All 17 functions mapped successfully
- Routes configured properly (`api/v1/*`)
- .NET 8 isolated worker model functioning

#### ❌ Azure Deployment Issues
1. **Response Serialization**: All endpoints except health return empty 204
2. **Code Deployment**: Functions deployed successfully (17 functions)
3. **Configuration**: App settings configured correctly (JWT settings, connection string)
4. **Database**: Migrations applied successfully to Azure SQL

## Root Cause Analysis

### Hypothesis 1: Azure Function Response Handling (MOST LIKELY)
The `.NET 8 isolated worker` model in Azure may have different response handling than local runtime.

**Evidence**:
- Health endpoint returns 204 correctly (it's supposed to)
- Register should return 201 with body but returns 204
- Code uses `response.WriteAsJsonAsync(data)` which should work

**Potential Causes**:
- Missing `Content-Type` header configuration
- Azure runtime not awaiting async response writes
- Difference between local Core Tools (4.3.0) and Azure runtime (4.1042)

### Hypothesis 2: Middleware Interference  
Exception handling or JWT middleware could be short-circuiting responses.

**Evidence Against**: Health endpoint works, suggesting middleware isn't the issue

### Hypothesis 3: Build/Deployment Artifacts
Incorrect deployment package or missing dependencies.

**Evidence Against**: All 17 functions deployed and mapped correctly

## Action Plan

### Immediate Actions (Priority 1)

#### 1. Check Azure Function App Configuration
```powershell
# Connect to Azure
az login
az account set --subscription <subscription-id>

# Check function app settings
az functionapp config appsettings list --name cvc-func-membership-dev --resource-group <rg-name>

# Check if WEBSITE_CONTENTAZUREFILECONNECTIONSTRING is set (required for Linux)
# Check FUNCTIONS_WORKER_RUNTIME = "dotnet-isolated"
# Check FUNCTIONS_EXTENSION_VERSION = "~4"
```

#### 2. Review Application Insights Logs
```powershell
# Query Application Insights for errors
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where timestamp > ago(1h) | project timestamp, message, severityLevel" \
  --offset 1h
```

Look for:
- Serialization errors
- Response writing failures  
- Middleware exceptions

#### 3. Test with Minimal Function
Create a test function that returns a simple JSON response:

```csharp
[Function("TestJson")]
public async Task<HttpResponseData> TestJson(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test/json")] HttpRequestData req)
{
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(new { message = "Test", timestamp = DateTime.UtcNow });
    return response;
}
```

Deploy and test: `https://cvc-func-membership-dev.azurewebsites.net/api/v1/test/json`

If this returns 204, the issue is with Azure's JSON serialization in isolated worker model.

### Short-term Workaround

#### Option A: Use ASP.NET Core Integration (Recommended)
Switch to ASP.NET Core integration for isolated worker (more reliable):

1. Install package:
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="1.3.2" />
```

2. Update Program.cs:
```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // Changed from ConfigureFunctionsWorkerDefaults
    .ConfigureServices(services => {
        // existing services
    })
    .Build();
```

3. Update functions to use `HttpRequest` instead of `HttpRequestData`

#### Option B: Explicit Content-Type Headers
Add explicit headers before JSON write:

```csharp
var response = req.CreateResponse(statusCode);
response.Headers.Add("Content-Type", "application/json; charset=utf-8");
await response.WriteAsJsonAsync(data);
return response;
```

### Medium-term Actions (Priority 2)

#### 4. Complete APIM Operations Configuration

Add missing operations to `infrastructure/modules/apim.bicep`:

```bicep
// Auth - Register
resource membershipRegisterOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: membershipApi
  name: 'auth-register'
  properties: {
    displayName: 'Register User'
    method: 'POST'
    urlTemplate: '/api/v1/auth/register'
    request: {
      representations: [
        {
          contentType: 'application/json'
          schemaId: 'CreateUserRequest'
        }
      ]
    }
    responses: [
      {
        statusCode: 201
        representations: [
          {
            contentType: 'application/json'
          }
        ]
      }
    ]
  }
}

// Repeat for:
// - POST /api/v1/auth/login
// - POST /api/v1/auth/refresh
// - POST /api/v1/auth/logout  
// - GET /api/v1/users
// - POST /api/v1/users
// - GET/PUT/DELETE /api/v1/users/{id}
```

#### 5. Run Deferred Integration Tests

Once deployment is fixed, run:
- T046A: APIM backend health check integration
- T047A: APIM JWT validation policy
- T048A: APIM CORS policy  
- T049A: Service registry endpoint

### Long-term Improvements (Priority 3)

#### 6. Local Development Improvements
- Set up LocalDB migrations script
- Add docker-compose for local Azure SQL
- Create seed data scripts

#### 7. CI/CD Pipeline
- Add health check validation post-deployment
- Automated smoke tests for each endpoint
- Rollback on failed health checks

#### 8. Monitoring & Alerts
- Application Insights query for 204 responses on non-health endpoints
- Alert on high rate of 4xx/5xx responses  
- Dashboard showing endpoint success rates

## Testing Checklist

After applying fixes, verify:

- [ ] `POST /api/v1/auth/register` returns 201 with `AuthResponse`
- [ ] `POST /api/v1/auth/login` returns 200 with `AuthResponse`  
- [ ] `GET /api/v1/users` returns 200 with `PagedResult<UserDto>` (with auth)
- [ ] `GET /api/v1/health` still returns 204  
- [ ] Swagger UI accessible at `/api/v1/swagger/ui`
- [ ] APIM gateway routes requests correctly
- [ ] Service registry returns Membership service metadata

## References

- [Azure Functions .NET Isolated Worker Guide](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [ASP.NET Core Integration](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide#aspnet-core-integration)
- [HTTP Trigger Bindings](https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger)

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-10-25 | Investigate ASP.NET Core integration | More reliable HTTP handling, better documented |
| TBD | Apply fix | Pending investigation results |
