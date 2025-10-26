# CoatesVillageClubServer Development Guidelines

**CRITICAL**: All development MUST follow the [Constitution](../.specify/memory/constitution.md) (v2.4.0).
The Constitution defines NON-NEGOTIABLE principles and mandatory standards for this project.

## Constitution Quick Reference

### Mandatory Principles (Must Follow)
1. **Test-First Development**: TDD with Red-Green-Refactor (NO exceptions)
2. **Code Quality Standards**: Zero warnings, <10 complexity, <30 line methods
3. **Performance First**: <200ms API response time (P95)
4. **Comprehensive Testing**: 80%+ coverage with realistic environments
5. **Maintainable Architecture**: Separation of concerns, DI, interfaces
6. **Service Boundaries**: No cross-service database access
7. **API and Data Format Standards**: camelCase JSON, ErrorResponse DTO
8. **Microservices Architecture**: Independent deployment, versioned APIs
9. **Library-First Development**: Features in `libs/` before service integration
10. **Local-First Development**: Full local testing before Azure deployment

### API Standards (MANDATORY)
- **JSON Serialization**: camelCase property names (e.g., `firstName`, `userId`)
  ```csharp
  services.Configure<JsonSerializerOptions>(options =>
  {
      options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
  });
  ```
- **Error Responses**: Use ErrorResponse DTO with `error`, `code`, `validationErrors`
- **HTTP Status Codes**: 200 (OK), 201 (Created), 204 (No Content), 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 500 (Internal Error)
- **Content-Type**: `application/json; charset=utf-8`
- **Versioning**: `/api/v1/...` in all endpoints

### Local Development (MANDATORY)
- Use Azure Functions Core Tools (`func start`) for local execution
- Use Azurite for blob/queue/table storage
- Use SQL Server in Docker or LocalDB for databases
- All features MUST work locally before Azure deployment
- NO cloud resources required for development/testing

### Testing Requirements
- Write tests FIRST (TDD - verify RED, then GREEN, then refactor)
- Test actual implementations, not mocks
- Use real databases in integration tests (in-memory or containerized)
- 80%+ code coverage required
- Test OUR code, not third-party libraries
- Document limitations when third-party internal APIs block testing

### Code Quality
- Production code: ZERO warnings
- Test code: Style warnings MAY be suppressed
- All public APIs: Complete XML documentation
- Max cyclomatic complexity: 10
- Max method length: 30 lines
- Follow functional programming principles (stateless, pure functions)

## Active Technologies
- C# 12 / .NET 8 (isolated worker model) (001-create-a-series)

## Project Structure
```
CoatesVillageClubServer/
├── services/           # Microservices (independently deployable)
│   ├── membership/    # User accounts, authentication, roles
│   ├── events/        # Calendar, special events, opening hours
│   ├── scheduling/    # Volunteer rota management
│   ├── bar/           # Point-of-Sale, inventory tracking
│   └── notifications/ # Email, SMS, push notifications
├── libs/              # Shared libraries (reusable across services)
│   ├── common-types/  # Shared data models and interfaces
│   ├── common-utils/  # Utility functions, helpers
│   └── contracts/     # Service contracts and API definitions
├── infrastructure/    # IaC (Bicep/Terraform) for Azure deployment
└── tests/            # Cross-service integration/E2E tests
```

## Development Workflow

1. **Verify local environment** (Azurite, SQL, func start)
2. **Design library first** in `libs/` (Library-First Development)
3. **Write test first** and verify it FAILS (TDD Red phase)
4. **Write minimal code** to make test pass (TDD Green phase)
5. **Refactor** while keeping tests green
6. **Test locally** (unit + integration + manual testing)
7. **Deploy to Azure** only after local verification passes

## Common Commands

### Local Development
```powershell
# Start Azure Functions locally
cd services/membership/src/VillageClub.Membership
func start --port 7071

# Run unit tests
cd services/membership/tests/VillageClub.Membership.Tests
dotnet test

# Run Newman integration tests
cd C:\Code\CoatesVillageClubServer
newman run tests/postman/membership-service.postman_collection.json -e tests/postman/local.postman_environment.json
```

### Running Services and E2E Tests (IMPORTANT)

**Background Job Management for Development**:
Services run as PowerShell background jobs to enable simultaneous service execution and test runs without terminal blocking.

**Quick Start - Automated Testing**
```powershell
# Run complete E2E test suite (starts service, runs tests, stops service)
cd C:\Code\CoatesVillageClubServer
.\scripts\test-e2e.ps1

# Keep service running after tests for debugging
.\scripts\test-e2e.ps1 -KeepServiceRunning

# Use existing running service (faster test iterations)
.\scripts\test-e2e.ps1 -SkipServiceStart
```

**Manual Service Control**
```powershell
# Start membership service in background
.\scripts\start-membership-service.ps1
# Output: Job ID: 1

# Run E2E tests (service keeps running)
newman run tests/postman/membership-service.postman_collection.json -e tests/postman/local.postman_environment.json

# Stop service when done
.\scripts\stop-membership-service.ps1
```

**Debugging and Log Access**
```powershell
# Check service status (jobs, processes, port 7071)
.\scripts\debug-service.ps1 status

# View all service logs from background job
.\scripts\view-service-logs.ps1

# Show last 50 log lines
.\scripts\view-service-logs.ps1 -Tail -Lines 50

# Follow logs in real-time
.\scripts\view-service-logs.ps1 -Follow

# Test service health endpoint
.\scripts\debug-service.ps1 health

# Direct job log access
Receive-Job -Id 1 -Keep                      # View all logs
Receive-Job -Id 1 -Keep | Select-Object -Last 50   # Last 50 lines
Get-Job                                       # Check job status
```

**Available Scripts** (in `scripts/` directory):
- `test-e2e.ps1` - Complete automated test workflow
- `start-membership-service.ps1` - Start service as background job
- `stop-membership-service.ps1` - Stop background service
- `view-service-logs.ps1` - Query service logs from background job
- `debug-service.ps1` - Multi-purpose debugging tool
- `README.md` - Complete debugging guide and examples

**Why Background Jobs?**
- **Non-Blocking Execution**: Service runs in background, terminal remains available for tests
- **Full Log Access**: All service output captured via `Receive-Job` for debugging
- **Proper Isolation**: Service runs independently without terminal interference
- **CI/CD Ready**: Automated scripts enable consistent local testing workflow
- **Better Debugging**: Query logs at any time without stopping service

**Best Practices**:
- Use `test-e2e.ps1` for standard test runs (automated cleanup)
- Use `-KeepServiceRunning` when iterating on tests or debugging
- Check `.\scripts\debug-service.ps1 status` before starting new service
- View logs with `.\scripts\view-service-logs.ps1` when debugging failures
- Clean up stopped jobs with `.\scripts\stop-membership-service.ps1`
- See `scripts/README.md` for complete debugging guide and troubleshooting

**Multiple Services** (future - when other services are implemented):
```powershell
# Each service will have its own start script with unique port
.\scripts\start-membership-service.ps1   # Port 7071
.\scripts\start-events-service.ps1       # Port 7072
.\scripts\start-scheduling-service.ps1   # Port 7073
```

### Build
```powershell
# Clean and build
dotnet clean
dotnet build

# Publish for deployment
dotnet publish --configuration Release
```

## Code Examples

### Error Response (MANDATORY Format)
```csharp
// Use ErrorResponse DTO - defined in libs/contracts/
return new ErrorResponse
{
    Error = "User not found",
    Code = "NOT_FOUND"
};

// For validation errors
return new ErrorResponse
{
    Error = "Validation failed",
    Code = "VALIDATION_ERROR",
    ValidationErrors = new Dictionary<string, string[]>
    {
        { "email", new[] { "Email is required", "Email must be valid" } }
    }
};
```

### JSON Serialization Setup
```csharp
// In Program.cs or startup configuration
services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
```

## Quality Metrics (Per Service)
- Test Coverage: ≥80%
- API Response Time: P95 ≤200ms
- Build Warnings: 0 (production code)
- Cyclomatic Complexity: ≤10
- Method Length: ≤30 lines
- Documentation Coverage: ≥90%

## References
- **Constitution**: `./.specify/memory/constitution.md` (v2.4.0) - AUTHORITATIVE SOURCE
- **ADRs**: `docs/adr/` - Architectural Decision Records
- **API Contracts**: `libs/contracts/` - Service interface definitions

---

**Last Updated**: 2025-10-26 (Constitution v2.4.0)

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->