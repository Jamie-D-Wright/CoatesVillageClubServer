# Implementation Plan: Village Club Management Microservices

**Branch**: `001-create-a-series` | **Date**: 2025-10-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-create-a-series/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a complete microservices-based system for village club management supporting user management (Committee/Volunteer/Member roles), event scheduling, volunteer shift coordination, stock alerts, and expense reimbursement. The system will be implemented using .NET 8 isolated Azure Functions deployed as separate function apps, with Azure API Management as the gateway, Azure SQL Database (serverless tier) for data persistence, and Azure Blob Storage for receipt images. Architecture optimized for low usage and cost efficiency with serverless-first approach.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (isolated worker model)  
**Primary Dependencies**: 
- Azure Functions SDK v4 (isolated process)
- Microsoft.EntityFrameworkCore 8.x (SQL provider)
- Azure.Identity & Azure.Storage.Blobs (receipt storage)
- Microsoft.AspNetCore.Authentication.JwtBearer (token validation)
- FluentValidation (request validation)
- Polly (circuit breaker, retry policies)
- Swashbuckle/NSwag (OpenAPI generation)

**Storage**: 
- Azure SQL Database (Serverless tier, auto-pause enabled, min 0.5 vCores)
- Azure Blob Storage (Hot tier for recent receipts, Cool tier lifecycle policy for >90 days)
- Each service has isolated schema within shared database (cost optimization)

**Testing**: 
- xUnit for unit tests
- Microsoft.AspNetCore.Mvc.Testing for integration tests
- Testcontainers for database integration tests
- Azure Functions local runtime for E2E testing

**Target Platform**: 
- Azure Functions Consumption Plan (pay-per-execution, auto-scale to zero)
- Azure API Management Consumption Tier (pay-per-call)
- Deployed to Azure UK South region

**Project Type**: Microservices monorepo with 6 independently deployable function apps

**Performance Goals**: 
- API response time <2s (95th percentile) per spec
- Handle 50 concurrent users per spec
- Cold start <5s for consumption plan functions

**Constraints**: 
- Serverless-first: Minimize always-on resources
- Database auto-pause when idle (5min threshold)
- No premium tiers or reserved capacity
- Maximum file upload 5MB (receipt images)
- JWT token expiry: 24 hours

**Scale/Scope**: 
- Expected 50-100 active users
- Peak load: Friday/Saturday 8pm-12am (4-6 concurrent users)
- ~10 events/month, ~8 bar shifts/month
- ~20 expense claims/month
- Data retention: 7 years for financial records, 2 years for operational data

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Principle I: Test-First Development
- **Status**: COMPLIANT
- **Plan**: TDD workflow will be followed for all services. xUnit tests written before implementation, AAA pattern enforced, 80%+ coverage required per service.

### ✅ Principle II: Code Quality Standards
- **Status**: COMPLIANT
- **Plan**: 
  - .editorconfig for consistent style
  - SonarAnalyzer.CSharp for static analysis
  - Max cyclomatic complexity 10 (enforced by analyzer)
  - Methods ≤30 lines, classes ≤200 lines
  - Strict layering: Models → Services → API (Functions)
  - Functional approach: immutable DTOs, pure service methods where possible

### ✅ Principle III: Performance First
- **Status**: COMPLIANT
- **Plan**: 
  - Success criteria SC-006: <2s response time already defined
  - Azure Application Insights for monitoring
  - Load testing with NBomber or k6 (targeting 10x = 500 concurrent users)
  - Database query optimization with EF Core query plan analysis

### ✅ Principle IV: Comprehensive Testing
- **Status**: COMPLIANT
- **Plan**: 
  - Unit tests (xUnit) for business logic
  - Integration tests with Testcontainers for database operations
  - Contract tests for inter-service API calls
  - E2E tests via Azure Functions local runtime
  - Load tests via NBomber targeting 10x load

### ✅ Principle V: Maintainable Architecture
- **Status**: COMPLIANT
- **Plan**: 
  - Dependency injection via built-in .NET DI
  - Interface-based design (IUserService, IEventService, etc.)
  - Structured logging with Serilog to Application Insights
  - Health check endpoints per service
  - ADRs documented in docs/adr/

### ✅ Principle VI: Service Boundaries
- **Status**: COMPLIANT
- **Plan**: 
  - Each service owns its database schema (no cross-schema queries)
  - REST APIs for synchronous communication
  - Shared types in libs/VillageClub.Contracts
  - No circular dependencies (API Gateway → Services, never reverse)

### ✅ Principle VII: Microservices Architecture
- **Status**: COMPLIANT
- **Plan**: 
  - 6 independently deployable Azure Function Apps
  - Each service has separate Dockerfile and deployment pipeline
  - Shared libs published as internal NuGet packages (local feed)
  - API versioning via route prefixes (/api/v1/)
  - Bicep modules per service in infrastructure/modules/

### 🟡 Complexity Concerns
- **Database sharing**: Using single Azure SQL Database (serverless) with schema isolation instead of 6 separate databases for cost optimization
  - **Justification**: At this scale (50-100 users, low traffic), separate databases would cost ~$300/month vs $15/month for shared serverless DB. Constitution principle of service boundaries maintained via schema isolation.
  - **Alternative rejected**: 6 separate databases - cost prohibitive for stated "low usage and cost efficiency" requirement
  
- **Consumption Plan limitations**: Cold start latency (3-5s) may occasionally breach <2s target on first request after idle period
  - **Justification**: Acceptable tradeoff for cost savings (~$0 when idle vs $150+/month for always-on App Service)
  - **Mitigation**: Pre-warming via scheduled ping during peak hours (Fri/Sat 7:45pm-12:15am)
  - **Alternative rejected**: Premium Functions Plan - costs $200+/month, not aligned with cost efficiency requirement

## Project Structure

### Documentation (this feature)

```
specs/001-create-a-series/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── openapi/         # OpenAPI 3.0 specs per service
│   └── schemas/         # JSON schemas for shared DTOs
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
services/
├── membership/                    # User accounts, authentication, roles
│   ├── src/
│   │   ├── VillageClub.Membership/
│   │   │   ├── Functions/        # HTTP-triggered Azure Functions
│   │   │   │   ├── AuthFunctions.cs
│   │   │   │   ├── UserFunctions.cs
│   │   │   │   └── RoleFunctions.cs
│   │   │   ├── Services/         # Business logic (stateless, testable)
│   │   │   │   ├── IAuthService.cs
│   │   │   │   ├── AuthService.cs
│   │   │   │   ├── IUserService.cs
│   │   │   │   └── UserService.cs
│   │   │   ├── Data/             # EF Core DbContext and entities
│   │   │   │   ├── MembershipDbContext.cs
│   │   │   │   ├── Entities/
│   │   │   │   └── Migrations/
│   │   │   ├── Models/           # DTOs and request/response models
│   │   │   └── Program.cs        # DI configuration
│   │   └── VillageClub.Membership.csproj
│   ├── tests/
│   │   ├── VillageClub.Membership.UnitTests/
│   │   ├── VillageClub.Membership.IntegrationTests/
│   │   └── VillageClub.Membership.ContractTests/
│   ├── Dockerfile
│   └── host.json
│
├── events/                        # Event calendar and management
│   ├── src/VillageClub.Events/
│   │   ├── Functions/
│   │   │   ├── EventFunctions.cs
│   │   │   └── EventQueryFunctions.cs
│   │   ├── Services/
│   │   ├── Data/
│   │   └── Models/
│   ├── tests/
│   └── Dockerfile
│
├── scheduling/                    # Volunteer shift management
│   ├── src/VillageClub.Scheduling/
│   │   ├── Functions/
│   │   │   ├── ShiftFunctions.cs
│   │   │   └── AssignmentFunctions.cs
│   │   ├── Services/
│   │   ├── Data/
│   │   └── Models/
│   ├── tests/
│   └── Dockerfile
│
├── bar/                           # Stock alerts (future: POS & inventory)
│   ├── src/VillageClub.Bar/
│   │   ├── Functions/
│   │   │   └── StockAlertFunctions.cs
│   │   ├── Services/
│   │   ├── Data/
│   │   └── Models/
│   ├── tests/
│   └── Dockerfile
│
├── finance/                       # Expense management and reimbursement
│   ├── src/VillageClub.Finance/
│   │   ├── Functions/
│   │   │   ├── ExpenseFunctions.cs
│   │   │   └── ReceiptFunctions.cs
│   │   ├── Services/
│   │   │   ├── IExpenseService.cs
│   │   │   ├── ExpenseService.cs
│   │   │   ├── IReceiptStorageService.cs
│   │   │   └── ReceiptStorageService.cs
│   │   ├── Data/
│   │   └── Models/
│   ├── tests/
│   └── Dockerfile
│
└── api-gateway/                   # Azure APIM configuration (not a function app)
    ├── policies/                  # XML policy definitions
    │   ├── global.xml
    │   ├── jwt-validation.xml
    │   └── rate-limit.xml
    └── apim-config.bicep

libs/
├── VillageClub.Contracts/         # Shared DTOs and interfaces
│   ├── DTOs/
│   │   ├── Users/
│   │   ├── Events/
│   │   ├── Shifts/
│   │   ├── StockAlerts/
│   │   └── Expenses/
│   ├── Enums/
│   │   ├── UserRole.cs
│   │   ├── CommitteeRole.cs
│   │   ├── EventType.cs
│   │   └── ExpenseStatus.cs
│   └── VillageClub.Contracts.csproj
│
├── VillageClub.Common/            # Shared utilities
│   ├── Extensions/
│   ├── Validators/               # FluentValidation base classes
│   ├── Middleware/               # JWT validation, error handling
│   ├── Exceptions/               # Custom exceptions
│   └── VillageClub.Common.csproj
│
└── VillageClub.Testing/           # Shared test utilities
    ├── Fixtures/
    ├── Builders/                 # Test data builders
    └── VillageClub.Testing.csproj

infrastructure/
├── main.bicep                    # Entry point - orchestrates modules
├── parameters/
│   ├── dev.bicepparam
│   ├── staging.bicepparam
│   └── prod.bicepparam
└── modules/
    ├── function-app.bicep        # Reusable module for function apps
    ├── sql-database.bicep        # Shared Azure SQL serverless database
    ├── storage.bicep             # Blob storage for receipts
    ├── apim.bicep                # API Management configuration
    ├── monitoring.bicep          # Application Insights + Log Analytics
    └── identity.bicep            # Managed identities + Key Vault

tests/
└── e2e/
    ├── VillageClub.E2E.Tests/
    │   ├── Scenarios/
    │   │   ├── UserManagementTests.cs
    │   │   ├── EventManagementTests.cs
    │   │   ├── ShiftWorkflowTests.cs
    │   │   └── ExpenseWorkflowTests.cs
    │   └── Infrastructure/
    │       ├── TestServerFactory.cs
    │       └── TestDataSeeder.cs
    └── VillageClub.E2E.Tests.csproj

docs/
├── adr/                          # Architecture Decision Records
│   ├── 0001-use-azure-functions.md
│   ├── 0002-shared-database-schema-isolation.md
│   ├── 0003-consumption-plan-vs-premium.md
│   └── 0004-jwt-authentication-strategy.md
└── api/                          # Generated from OpenAPI specs
    └── [auto-generated docs]
```

**Structure Decision**: 
Microservices monorepo with 5 domain services (Membership, Events, Scheduling, Bar, Finance) plus API Gateway. Each service is an independent Azure Function App (.NET 8 isolated) with its own deployment pipeline. Shared code lives in `libs/` as internal NuGet packages. Infrastructure as Code in Bicep modules for composable deployment.

**Affected Services**: All services (greenfield implementation)
- **membership**: User accounts, authentication (JWT issuing), role management
- **events**: Event CRUD, event calendar, event-shift relationship
- **scheduling**: Shift creation, volunteer assignments, shift availability
- **bar**: Stock alerts (baseline for future POS/inventory features)
- **finance**: Expense claims, receipt upload to Blob Storage, approval workflow
- **api-gateway**: Azure APIM policies for JWT validation, routing, rate limiting

## Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Shared Azure SQL Database (violates strict service boundary principle of "no shared databases") | Cost optimization for low-traffic scenario: Single serverless Azure SQL DB costs ~$15/month vs 5 separate databases at ~$300/month | Separate databases per service would exceed budget constraints for "low usage and cost efficiency" requirement. Schema isolation maintains logical boundaries and prevents cross-service queries. Migration to separate DBs possible if traffic/budget increases. |
| Consumption Plan cold starts (may violate <2s response time for first request after idle) | Pay-per-execution model costs ~$0 when idle vs $150-200/month for Premium/App Service plans | Always-on hosting contradicts "low usage and cost efficiency" requirement. Cold start mitigated by pre-warming during peak hours (Fri/Sat evenings). Users expecting social club responsiveness, not sub-second SLA. |

---

## Phase 0: Research - ✅ COMPLETE

All technical unknowns have been resolved and documented in `research.md`:

- ✅ Compute platform: Azure Functions Consumption Plan
- ✅ Database strategy: Azure SQL Serverless with schema isolation
- ✅ Authentication: JWT tokens issued by Membership service, validated at API Gateway
- ✅ File storage: Azure Blob Storage with lifecycle policies
- ✅ Inter-service communication: REST over HTTP with Polly circuit breaker
- ✅ API documentation: Swashbuckle/OpenAPI per service + APIM Developer Portal
- ✅ Monitoring: Application Insights with structured logging via Serilog
- ✅ Testing strategy: xUnit + Testcontainers + in-process Functions host
- ✅ Cost optimization: Multi-pronged approach targeting <$50/month

**No remaining NEEDS CLARIFICATION items.**

---

## Phase 1: Design & Contracts - ✅ COMPLETE

### Data Model - ✅ COMPLETE
`data-model.md` includes:
- ✅ Entity definitions for all 5 services
- ✅ Relationships and validation rules
- ✅ State transitions for Event and Expense
- ✅ Enumerations (UserRole, EventType, ExpenseStatus, etc.)
- ✅ Cross-service validation strategy
- ✅ Data retention policies

### API Contracts - ✅ COMPLETE
`contracts/` directory includes:
- ✅ Full OpenAPI 3.0 spec for Membership service (`membership-api.yaml`)
- ✅ Summary of all service endpoints (`API-CONTRACTS.md`)
- ✅ Shared patterns (auth, pagination, errors)
- ✅ Inter-service communication patterns
- ✅ Contract testing requirements

### Developer Onboarding - ✅ COMPLETE
`quickstart.md` includes:
- ✅ Prerequisites and required software
- ✅ Local database setup (Docker + SQL Server Express)
- ✅ Azurite configuration for blob storage
- ✅ Service configuration templates (local.settings.json)
- ✅ Running services locally (single + all services)
- ✅ Test data seeding instructions
- ✅ Troubleshooting guide
- ✅ Development workflow (TDD approach)

### Agent Context - ✅ COMPLETE
- ✅ Updated Copilot instructions with C# 12 / .NET 8
- ✅ Preserved manual additions between markers

---

## Phase 1: Constitution Re-Check - ✅ PASS

**Re-evaluated after design phase:**

### ✅ Principle I: Test-First Development
- Design supports TDD: clear service boundaries, interface-based design
- Test strategy documented: Unit (xUnit) + Integration (Testcontainers) + E2E (in-process host)
- AAA pattern enforced, 80%+ coverage target per service

### ✅ Principle II: Code Quality Standards
- Layered architecture: Models → Services → Functions
- Functional approach: immutable DTOs, stateless services
- Static analysis planned: SonarAnalyzer.CSharp
- Strict validation: FluentValidation for all requests

### ✅ Principle III: Performance First
- Performance targets defined: <2s response time, 50 concurrent users
- Monitoring: Application Insights with custom metrics
- Load testing planned: NBomber targeting 10x load (500 users)
- Database optimization: EF Core query plan analysis, proper indexing

### ✅ Principle IV: Comprehensive Testing
- Multi-layer testing strategy complete
- Unit tests for business logic (pure functions)
- Integration tests with real database (Testcontainers)
- Contract tests for service APIs
- E2E tests for critical workflows
- Load tests for performance validation

### ✅ Principle V: Maintainable Architecture
- Dependency injection via .NET DI container
- Interface-based design (IUserService, IEventService, etc.)
- Structured logging to Application Insights
- Health check endpoints per service
- ADRs documented in `docs/adr/` (to be created)
- Configuration externalized (local.settings.json, App Settings)

### ✅ Principle VI: Service Boundaries
- Each service owns its database schema (no cross-schema queries)
- Inter-service communication via REST APIs only
- Shared types in `libs/VillageClub.Contracts`
- No circular dependencies (API Gateway is edge, services don't call back)
- Cross-service validation via API calls, not direct database access

### ✅ Principle VII: Microservices Architecture
- 5 domain services + API Gateway, independently deployable
- Each service has separate Dockerfile
- Shared libraries in `libs/` (NuGet packages)
- API versioning via URL prefix (`/api/v1/`)
- Bicep modules per service for infrastructure
- Service-specific health checks and graceful shutdown

**Constitution Check Result: ✅ PASS WITH JUSTIFIED EXCEPTIONS**

Exceptions documented in Complexity Tracking table above.

---

## Next Steps (Phase 2 - NOT part of this command)

Phase 2 is executed via the `/speckit.tasks` command and will generate `tasks.md` with:
- Detailed implementation tasks per service
- Test scenarios mapped to user stories
- CI/CD pipeline configuration
- Deployment steps

**DO NOT PROCEED TO PHASE 2 UNTIL:**
1. ✅ Research reviewed and approved
2. ✅ Data model reviewed and approved
3. ✅ API contracts reviewed and approved
4. ✅ Constitution Check violations justified and approved

---

## Summary

### ✅ Deliverables Complete

| Artifact | Status | Location |
|----------|--------|----------|
| Implementation Plan | ✅ Complete | `plan.md` (this file) |
| Research Document | ✅ Complete | `research.md` |
| Data Model | ✅ Complete | `data-model.md` |
| API Contracts | ✅ Complete | `contracts/openapi/`, `contracts/API-CONTRACTS.md` |
| Quickstart Guide | ✅ Complete | `quickstart.md` |
| Agent Context | ✅ Updated | `.github/copilot-instructions.md` |

### Technical Summary

- **Architecture**: Microservices monorepo, 5 domain services + API Gateway
- **Compute**: Azure Functions (Consumption Plan, .NET 8 isolated)
- **Database**: Azure SQL Serverless with schema isolation
- **Storage**: Azure Blob Storage (Hot/Cool tiers)
- **Auth**: JWT tokens (issued by Membership, validated at APIM)
- **Monitoring**: Application Insights + Serilog
- **Cost Target**: <$50/month for low-traffic operation

### Project Structure Created

```
specs/001-create-a-series/
├── plan.md              ✅ Complete
├── research.md          ✅ Complete  
├── data-model.md        ✅ Complete
├── quickstart.md        ✅ Complete
└── contracts/           ✅ Complete
    ├── openapi/
    │   └── membership-api.yaml
    ├── schemas/         (empty, to be populated during implementation)
    └── API-CONTRACTS.md
```

### Branch Information

- **Branch**: `001-create-a-series`
- **Status**: Planning complete, ready for task breakdown (Phase 2)
- **Implementation Plan**: This file (`plan.md`)

---

**Planning Completed**: 2025-10-18  
**Ready for**: Phase 2 Task Breakdown (`/speckit.tasks` command)  
**Estimated Implementation Time**: 8-10 weeks (5 services + testing + deployment)
