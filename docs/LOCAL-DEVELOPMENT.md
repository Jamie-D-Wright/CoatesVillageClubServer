# Local Development Environment Setup

**Project**: Village Club Management Microservices  
**Last Updated**: 2025-10-25  
**Target Platform**: .NET 8 Azure Functions (Isolated Worker Model)

## Overview

This guide walks you through setting up a complete local development environment for the Village Club Management system. The local environment uses emulators and containerized services to replicate the Azure production environment without incurring cloud costs.

**Local-First Development Philosophy**: Per Constitution v2.3.0, ALL features MUST be fully testable locally before Azure deployment. This ensures rapid development cycles, offline capability, and cost efficiency.

---

## Prerequisites

### Required Software

| Tool | Version | Purpose | Installation |
|------|---------|---------|--------------|
| **.NET 8 SDK** | 8.0.100+ | Core runtime | https://dotnet.microsoft.com/download/dotnet/8.0 |
| **Azure Functions Core Tools** | 4.x | Local function execution | `npm install -g azure-functions-core-tools@4` |
| **Docker Desktop** | Latest | SQL Server containerization | https://www.docker.com/products/docker-desktop |
| **Git** | 2.x+ | Version control | https://git-scm.com/downloads |

**Note**: Azurite is NOT required as a global install - it's included with Azure Functions Core Tools v4.

### Verification Commands

```powershell
# Verify installations
dotnet --version          # Should show 8.0.x
func --version            # Should show 4.x
docker --version          # Should show version info
git --version             # Should show version info
```

---

## Step 1: Clone Repository

```powershell
git clone https://github.com/Jamie-D-Wright/CoatesVillageClubServer.git
cd CoatesVillageClubServer
git checkout 001-create-a-series
```

---

## Step 2: Start Local Infrastructure

### 2.1 Start SQL Server (Docker)

**⚠️ CRITICAL**: Docker Desktop must be running before executing this command.

```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" `
  -p 1433:1433 --name sqlserver-villageclub `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**Verify SQL Server is running**:
```powershell
docker ps --filter "name=sqlserver-villageclub"
```

**Expected Output**:
```
CONTAINER ID   IMAGE                                        STATUS
abc123def456   mcr.microsoft.com/mssql/server:2022-latest   Up 10 seconds
```

**Troubleshooting**:
- **Error: Docker Desktop is not running**: Start Docker Desktop and wait for it to fully initialize (look for green "Docker Desktop is running" in system tray).
- **Error: Container already exists**: Remove old container with `docker rm -f sqlserver-villageclub` and retry.
- **Error: Port 1433 already in use**: Another SQL Server instance is running. Stop it or change the port mapping (e.g., `-p 1434:1433`).

### 2.2 Start Azurite (Azure Storage Emulator)

Azurite is built into Azure Functions Core Tools v4 and starts automatically when you run `func start`. However, you can start it manually if needed:

```powershell
# Azurite starts automatically with Functions runtime
# No manual startup required unless running standalone
```

**For standalone Azurite** (optional, only if you need storage access outside Functions):
```powershell
# If you have Azurite installed globally
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

**Verify Azurite is accessible**:
```powershell
Test-NetConnection -ComputerName localhost -Port 10000 -InformationLevel Quiet
# Should return: True
```

---

## Step 3: Configure Local Settings

### 3.1 Create local.settings.json for Membership Service

**Location**: `services/membership/local.settings.json`

```powershell
# Copy template to actual file
Copy-Item services/membership/local.settings.json.example services/membership/local.settings.json
```

**Edit the file** and update values if needed (default values work for local development):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;",
    "JwtSecret": "CHANGE-THIS-TO-A-SECURE-RSA-KEY-PAIR-FOR-PRODUCTION-USE-MINIMUM-2048-BITS",
    "JwtIssuer": "https://villageclub.coates.local",
    "JwtAudience": "https://villageclub.coates.local",
    "JwtExpiryMinutes": "1440",
    "Logging__LogLevel__Default": "Information"
  }
}
```

**⚠️ Important**: `local.settings.json` is gitignored and should NEVER be committed to source control.

---

## Step 4: Initialize Database

### 4.1 Restore NuGet Packages

```powershell
dotnet restore CoatesVillageClubServer.sln
```

### 4.2 Apply EF Core Migrations

```powershell
# Navigate to Membership service
cd services/membership/src/VillageClub.Membership

# Apply migrations to create database and schema
dotnet ef database update

# Return to root
cd ../../../../..
```

**Expected Output**:
```
Build started...
Build succeeded.
Applying migration '20251018_InitialMembershipSchema'.
Done.
```

**Troubleshooting**:
- **Error: A network-related or instance-specific error**: SQL Server container is not running. Verify with `docker ps`.
- **Error: Login failed for user 'sa'**: Password mismatch. Ensure `local.settings.json` matches Docker container password.
- **Error: Database 'VillageClubDB' already exists**: Safe to ignore - migrations will update schema only.

---

## Step 5: Start Membership Service Locally

```powershell
cd services/membership
func start --port 7071
```

**Expected Output**:
```
Azure Functions Core Tools
Core Tools Version:       4.x
Function Runtime Version: 4.x

Functions:
  AuthFunctions_Login: [POST] http://localhost:7071/api/v1/auth/login
  AuthFunctions_Register: [POST] http://localhost:7071/api/v1/auth/register
  AuthFunctions_Refresh: [POST] http://localhost:7071/api/v1/auth/refresh
  UserFunctions_GetMe: [GET] http://localhost:7071/api/v1/users/me
  UserFunctions_GetAll: [GET] http://localhost:7071/api/v1/users
  HealthFunctions_Health: [GET] http://localhost:7071/api/v1/health
  ...17 functions total

For detailed output, run func with --verbose flag.
```

**Verify Service is Running**:
```powershell
# Test health endpoint
Invoke-WebRequest -Uri "http://localhost:7071/api/v1/health" -UseBasicParsing | Select-Object StatusCode

# Expected: StatusCode: 204 (No Content)
```

---

## Step 6: Test Local API

### 6.1 Register a New User

```powershell
$body = @{
    email = "test@example.com"
    password = "Test123!"
    firstName = "Test"
    lastName = "User"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/v1/auth/register" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

**Expected Response**:
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123...",
  "expiresIn": 86400,
  "user": {
    "id": "guid-here",
    "email": "test@example.com",
    "firstName": "Test",
    "lastName": "User",
    "role": "Member"
  }
}
```

### 6.2 Login with Created User

```powershell
$loginBody = @{
    email = "test@example.com"
    password = "Test123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:7071/api/v1/auth/login" `
    -Method Post `
    -Body $loginBody `
    -ContentType "application/json"

# Save token for authenticated requests
$token = $response.accessToken
```

### 6.3 Access Protected Endpoint

```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/v1/users/me" `
    -Headers @{ Authorization = "Bearer $token" }
```

---

## Step 7: Run Tests Locally

### 7.1 Run All Unit Tests

```powershell
cd services/membership/tests/VillageClub.Membership.Tests
dotnet test --verbosity normal
```

**Expected Output**:
```
Passed!  - Failed:     0, Passed:   144, Skipped:     0, Total:   144
```

### 7.2 Run Tests with Code Coverage

```powershell
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

---

## Automation Script

For convenience, use the automated startup script:

```powershell
.\scripts\start-local-env.ps1
```

**What it does**:
1. Checks if Docker Desktop is running
2. Starts SQL Server container if not running
3. Starts Azurite (if needed)
4. Verifies connectivity to both services
5. Displays status summary

---

## Troubleshooting Guide

### Issue: "Docker is not running"

**Symptoms**: `docker` commands fail with connection error  
**Solution**:
1. Open Docker Desktop
2. Wait for status to show "Docker Desktop is running" (green indicator)
3. Retry command

### Issue: "SQL Server connection timeout"

**Symptoms**: EF migrations fail with `A network-related or instance-specific error`  
**Solution**:
1. Verify container is running: `docker ps`
2. Check container logs: `docker logs sqlserver-villageclub`
3. Restart container: `docker restart sqlserver-villageclub`
4. Wait 10 seconds for SQL Server to fully initialize

### Issue: "Port 7071 already in use"

**Symptoms**: `func start` fails with "Failed to bind to address"  
**Solution**:
1. Find process using port: `Get-NetTCPConnection -LocalPort 7071 | Select-Object OwningProcess`
2. Kill process: `Stop-Process -Id <ProcessId>`
3. Retry `func start`

### Issue: "Azurite not found"

**Symptoms**: Functions fail to start with "Azure Storage connection error"  
**Solution**:
- Azurite is built into Functions Core Tools v4 - no separate install needed
- If using older tools, upgrade: `npm install -g azure-functions-core-tools@4`

### Issue: "EF Core migration not found"

**Symptoms**: `dotnet ef database update` shows "No migrations found"  
**Solution**:
1. Verify project path is correct (must be in `services/membership/src/VillageClub.Membership`)
2. Check if migrations exist: `dotnet ef migrations list`
3. If no migrations, create one: `dotnet ef migrations add InitialMembershipSchema`

### Issue: "JWT validation fails locally"

**Symptoms**: Protected endpoints return 401 Unauthorized  
**Solution**:
1. Ensure token is fresh (24-hour expiry)
2. Verify `Authorization` header format: `Bearer <token>`
3. Check `JwtSecret`, `JwtIssuer`, and `JwtAudience` match in `local.settings.json`
4. Restart function app after changing settings

---

## Daily Workflow

### Starting Your Day

```powershell
# 1. Ensure Docker Desktop is running
# 2. Start SQL Server if not running
docker start sqlserver-villageclub

# 3. Navigate to service and start
cd services/membership
func start --port 7071
```

### Stopping Services

```powershell
# Stop function app: Ctrl+C in terminal

# Stop SQL Server (optional, keeps data):
docker stop sqlserver-villageclub

# Remove SQL Server (deletes data):
docker rm -f sqlserver-villageclub
```

---

## Performance Expectations

| Metric | Expected Value |
|--------|----------------|
| **Function Cold Start** | <5 seconds |
| **Function Warm Start** | <1 second |
| **Database Connection** | <200ms |
| **API Response Time (simple)** | <100ms |
| **API Response Time (with DB)** | <500ms |
| **Full Test Suite Execution** | <30 seconds |

---

## Next Steps

Once local environment is running:

1. **Run Phase 3.5 Local Tests** (see `specs/001-create-a-series/tasks.md`)
2. **Explore Swagger UI**: http://localhost:7071/api/v1/swagger/ui
3. **Review API Contracts**: `specs/001-create-a-series/contracts/API-CONTRACTS.md`
4. **Start Implementing New Features** (always test locally first per Constitution v2.3.0)

---

## Additional Resources

- **Azure Functions Local Development**: https://learn.microsoft.com/azure/azure-functions/functions-develop-local
- **EF Core Migrations**: https://learn.microsoft.com/ef/core/managing-schemas/migrations/
- **Docker for SQL Server**: https://learn.microsoft.com/sql/linux/quickstart-install-connect-docker
- **Azurite Documentation**: https://learn.microsoft.com/azure/storage/common/storage-use-azurite

---

## Support

If you encounter issues not covered in this guide:

1. Check **tasks.md** Phase 0 checklist for prerequisites
2. Review **DEPLOYMENT-TROUBLESHOOTING.md** for Azure-specific issues
3. Consult **quickstart.md** for additional setup scenarios
4. Check project README.md for architecture overview
