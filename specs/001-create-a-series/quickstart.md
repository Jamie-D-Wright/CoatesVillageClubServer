# Quickstart Guide: Village Club Management System

**Feature**: 001-create-a-series  
**Date**: 2025-10-18  
**Audience**: Developers setting up local development environment

---

## Prerequisites

### Required Software

1. **.NET 8 SDK** (8.0.100 or later)
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify: `dotnet --version` should show 8.0.x

2. **Azure Functions Core Tools** v4
   - Install: `npm install -g azure-functions-core-tools@4`
   - Verify: `func --version` should show 4.x

3. **SQL Server** (choose one):
   - **Docker** (recommended): `docker pull mcr.microsoft.com/mssql/server:2022-latest`
   - **SQL Server Express**: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
   - **Azure SQL Database** (serverless tier for dev)

4. **Azurite** (local Azure Storage emulator)
   - Install: `npm install -g azurite`
   - Verify: `azurite --version`

5. **Git** (version control)
   - Download: https://git-scm.com/downloads
   - Verify: `git --version`

### Optional Tools

- **Visual Studio 2022** (Community/Professional/Enterprise) with Azure Development workload
- **VS Code** with extensions:
  - C# Dev Kit
  - Azure Functions
  - REST Client (for testing APIs)
- **Postman** or **Insomnia** (API testing)
- **Azure Storage Explorer** (view blob storage locally)

---

## Project Setup

### 1. Clone Repository

```powershell
git clone https://github.com/your-org/CoatesVillageClubServer.git
cd CoatesVillageClubServer
git checkout 001-create-a-series
```

### 2. Initialize Submodules (if any)

```powershell
git submodule update --init --recursive
```

### 3. Restore NuGet Packages

```powershell
dotnet restore CoatesVillageClubServer.sln
```

---

## Local Database Setup

### Option A: Docker (Recommended)

1. **Start SQL Server container**:
```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" `
  -p 1433:1433 --name sqlserver-villageclub `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

2. **Connection string** (add to each service's `local.settings.json`):
```
Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
```

### Option B: SQL Server Express

1. **Install SQL Server Express** with default instance name
2. **Connection string**:
```
Server=localhost\\SQLEXPRESS;Database=VillageClubDB;Integrated Security=true;TrustServerCertificate=True
```

### 3. Create Database and Apply Migrations

```powershell
# Create database
dotnet ef database update --project services/membership/src/VillageClub.Membership

# Apply migrations for each service
cd services/membership/src/VillageClub.Membership
dotnet ef database update

cd ../../../events/src/VillageClub.Events
dotnet ef database update

cd ../../../scheduling/src/VillageClub.Scheduling
dotnet ef database update

cd ../../../bar/src/VillageClub.Bar
dotnet ef database update

cd ../../../finance/src/VillageClub.Finance
dotnet ef database update

cd ../../../../..
```

---

## Local Storage Setup (Azurite)

### 1. Start Azurite

```powershell
# Start all services (Blob, Queue, Table)
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

**OR** run in VS Code:
- Open Command Palette (Ctrl+Shift+P)
- Type "Azurite: Start"

### 2. Connection String

Add to each service's `local.settings.json`:
```
UseDevelopmentStorage=true
```

---

## Service Configuration

Each service needs a `local.settings.json` file (not committed to Git). Create from template:

### Membership Service

**File**: `services/membership/src/VillageClub.Membership/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__VillageClubDB": "Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True",
    "Jwt__SecretKey": "THIS-IS-A-DEV-SECRET-KEY-CHANGE-IN-PRODUCTION-MINIMUM-32-CHARS",
    "Jwt__Issuer": "VillageClub.Membership",
    "Jwt__Audience": "VillageClub.API",
    "Jwt__ExpiryHours": 24,
    "Logging__LogLevel__Default": "Information",
    "Logging__LogLevel__Microsoft": "Warning"
  }
}
```

### Events Service

**File**: `services/events/src/VillageClub.Events/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__VillageClubDB": "Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True",
    "MembershipService__BaseUrl": "http://localhost:7071/api/v1",
    "Logging__LogLevel__Default": "Information"
  }
}
```

### Scheduling Service

**File**: `services/scheduling/src/VillageClub.Scheduling/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__VillageClubDB": "Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True",
    "MembershipService__BaseUrl": "http://localhost:7071/api/v1",
    "EventsService__BaseUrl": "http://localhost:7072/api/v1",
    "Logging__LogLevel__Default": "Information"
  }
}
```

### Bar Service

**File**: `services/bar/src/VillageClub.Bar/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__VillageClubDB": "Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True",
    "MembershipService__BaseUrl": "http://localhost:7071/api/v1",
    "Logging__LogLevel__Default": "Information"
  }
}
```

### Finance Service

**File**: `services/finance/src/VillageClub.Finance/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__VillageClubDB": "Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True",
    "BlobStorage__ConnectionString": "UseDevelopmentStorage=true",
    "BlobStorage__ContainerName": "expense-receipts",
    "MembershipService__BaseUrl": "http://localhost:7071/api/v1",
    "EventsService__BaseUrl": "http://localhost:7072/api/v1",
    "Logging__LogLevel__Default": "Information"
  }
}
```

---

## Running Services Locally

### Option 1: Run All Services (Recommended for E2E Testing)

**PowerShell script** (create `scripts/start-all-services.ps1`):

```powershell
# Start SQL Server (if using Docker)
docker start sqlserver-villageclub

# Start Azurite
Start-Process -NoNewWindow azurite

# Start each service in separate terminal
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/membership/src/VillageClub.Membership; func start --port 7071"
Start-Sleep -Seconds 5

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/events/src/VillageClub.Events; func start --port 7072"
Start-Sleep -Seconds 5

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/scheduling/src/VillageClub.Scheduling; func start --port 7073"
Start-Sleep -Seconds 5

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/bar/src/VillageClub.Bar; func start --port 7074"
Start-Sleep -Seconds 5

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd services/finance/src/VillageClub.Finance; func start --port 7075"

Write-Host "All services started!"
Write-Host "Membership: http://localhost:7071"
Write-Host "Events:     http://localhost:7072"
Write-Host "Scheduling: http://localhost:7073"
Write-Host "Bar:        http://localhost:7074"
Write-Host "Finance:    http://localhost:7075"
```

### Option 2: Run Individual Service

```powershell
# Navigate to service directory
cd services/membership/src/VillageClub.Membership

# Start function app
func start

# Service will be available at http://localhost:7071
```

### Option 3: Debug in Visual Studio

1. Open `CoatesVillageClubServer.sln`
2. Right-click service project → Set as Startup Project
3. Press F5 to debug

---

## Seeding Test Data

### Create Admin User

```powershell
# Run seed script (create this during implementation)
dotnet run --project tools/DataSeeder -- --admin
```

**OR** manually via SQL:

```sql
USE VillageClubDB;

-- Insert admin user (password: Admin123!)
INSERT INTO Membership.Users (Id, Email, PasswordHash, FirstName, LastName, Role, CommitteeRole, Status, CreatedAt, UpdatedAt)
VALUES (
  NEWID(),
  'admin@villageclub.local',
  '$2a$11$abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ', -- Replace with actual bcrypt hash
  'Admin',
  'User',
  'Committee',
  'Chairman',
  'Active',
  GETUTCDATE(),
  GETUTCDATE()
);
```

---

## Testing the Setup

### 1. Health Check

```powershell
# Test each service
curl http://localhost:7071/api/health  # Membership
curl http://localhost:7072/api/health  # Events
curl http://localhost:7073/api/health  # Scheduling
curl http://localhost:7074/api/health  # Bar
curl http://localhost:7075/api/health  # Finance
```

**Expected response**:
```json
{
  "status": "Healthy",
  "version": "1.0.0",
  "timestamp": "2025-10-18T20:00:00Z",
  "dependencies": {
    "database": "Healthy"
  }
}
```

### 2. Login (Get JWT Token)

**Request**:
```powershell
curl -X POST http://localhost:7071/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "admin@villageclub.local",
    "password": "Admin123!"
  }'
```

**Expected response**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "8f5d2a1b-3c4e-5f6g-7h8i-9j0k1l2m3n4o",
  "expiresIn": 86400,
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "admin@villageclub.local",
    "firstName": "Admin",
    "lastName": "User",
    "role": "Committee",
    "committeeRole": "Chairman",
    "status": "Active"
  }
}
```

### 3. Create an Event (Authenticated)

**Request**:
```powershell
curl -X POST http://localhost:7072/api/v1/events `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" `
  -d '{
    "title": "Friday Bar Night",
    "description": "Regular bar opening",
    "eventType": "RegularBarNight",
    "startDateTime": "2025-10-24T20:00:00Z",
    "endDateTime": "2025-10-25T00:00:00Z",
    "status": "Published"
  }'
```

---

## Running Tests

### Unit Tests

```powershell
# Run all unit tests
dotnet test --filter "Category=Unit"

# Run tests for specific service
dotnet test services/membership/tests/VillageClub.Membership.UnitTests
```

### Integration Tests

```powershell
# Ensure Docker is running (for Testcontainers)
docker info

# Run integration tests
dotnet test --filter "Category=Integration"
```

### E2E Tests

```powershell
# Ensure all services are running
scripts/start-all-services.ps1

# Run E2E tests
dotnet test tests/e2e/VillageClub.E2E.Tests
```

---

## Troubleshooting

### Issue: "Database connection failed"

**Solution**:
- Verify SQL Server is running: `docker ps` or check SQL Server Express service
- Test connection: `sqlcmd -S localhost,1433 -U sa -P YourStrong!Passw0rd`
- Check connection string in `local.settings.json`

### Issue: "Azurite not found"

**Solution**:
```powershell
npm install -g azurite
# Verify installation
azurite --version
```

### Issue: "Port already in use"

**Solution**:
```powershell
# Find process using port
netstat -ano | findstr :7071

# Kill process
taskkill /PID <process_id> /F

# Or change port in func start command
func start --port 8080
```

### Issue: "JWT token validation failed"

**Solution**:
- Ensure `Jwt__SecretKey` is identical across all services
- Check token hasn't expired (24-hour default)
- Verify `Authorization: Bearer <token>` header format

### Issue: "Blob storage connection failed"

**Solution**:
- Start Azurite: `azurite`
- Verify connection string: `UseDevelopmentStorage=true`
- Check Azurite logs: `c:\azurite\debug.log`

---

## Development Workflow

### 1. Create Feature Branch

```powershell
git checkout -b feature/user-management
```

### 2. Write Tests First (TDD)

```csharp
// Example: services/membership/tests/VillageClub.Membership.UnitTests/Services/UserServiceTests.cs

[Fact]
public async Task CreateUser_ValidInput_ReturnsCreatedUser()
{
    // Arrange
    var request = new CreateUserRequest { ... };
    
    // Act
    var result = await _userService.CreateUserAsync(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(request.Email, result.Email);
}
```

### 3. Implement Functionality

```csharp
// services/membership/src/VillageClub.Membership/Services/UserService.cs
public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
{
    // Implementation
}
```

### 4. Run Tests

```powershell
dotnet test services/membership/tests/VillageClub.Membership.UnitTests
```

### 5. Commit and Push

```powershell
git add .
git commit -m "feat(membership): implement user creation"
git push origin feature/user-management
```

---

## Additional Resources

### Documentation
- [Azure Functions .NET isolated guide](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Polly resilience patterns](https://www.pollydocs.org/)

### Project Documentation
- [Architecture Overview](../../docs/architecture.md)
- [API Contracts](./contracts/API-CONTRACTS.md)
- [Data Model](./data-model.md)
- [Deployment Guide](../../docs/deployment.md)

### Support
- Create an issue: https://github.com/your-org/CoatesVillageClubServer/issues
- Team chat: [Slack/Teams channel]

---

**Last Updated**: 2025-10-18  
**Maintained By**: Village Club Development Team
