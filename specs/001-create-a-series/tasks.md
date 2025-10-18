# Tasks: Village Club Management Microservices

**Input**: Design documents from `/specs/001-create-a-series/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Test-Driven Development (TDD) is REQUIRED per organizational constitution. All implementation must follow Red-Green-Refactor cycle.

**TDD Workflow**: For each feature:
1. Write failing test(s) first (Red)
2. Implement minimum code to pass tests (Green)
3. Refactor while keeping tests green
4. Commit with tests passing

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5, US6)
- Include exact file paths in descriptions

## Path Conventions
This is a microservices monorepo with 6 independently deployable Azure Function Apps:
- **Microservices**: `services/[service-name]/src/VillageClub.[ServiceName]/`
- **Tests**: `services/[service-name]/tests/`
- **Shared libraries**: `libs/VillageClub.Contracts/`
- **Infrastructure**: `infrastructure/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Repository structure, shared libraries, and infrastructure foundation

- [X] T001 Create monorepo directory structure: `services/`, `libs/`, `infrastructure/`, `docs/`
- [X] T002 [P] Create solution file `CoatesVillageClubServer.sln` at repository root
- [X] T003 [P] Create shared contracts library project `libs/VillageClub.Contracts/VillageClub.Contracts.csproj` with DTOs and interfaces
- [X] T004 [P] Create `.editorconfig` at root with C# code style rules (max complexity 10, methods ≤30 lines)
- [X] T005 [P] Add `Directory.Build.props` at root for shared NuGet package versions and analyzer configuration
- [X] T006 [P] Configure SonarAnalyzer.CSharp and code quality analyzers in `Directory.Build.props`
- [X] T007 Create `infrastructure/main.bicep` with Azure SQL Database (serverless tier, auto-pause enabled)
- [X] T008 [P] Add Bicep module `infrastructure/modules/function-app.bicep` for Azure Functions deployment template
- [X] T009 [P] Add Bicep module `infrastructure/modules/apim.bicep` for Azure API Management (Consumption tier)
- [X] T010 [P] Add Bicep module `infrastructure/modules/blob-storage.bicep` for receipt storage with lifecycle policies

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T011 Create database schemas SQL script `infrastructure/scripts/create-schemas.sql` (Membership, Events, Scheduling, Bar, Finance)
- [X] T012 Add shared DTO models to `libs/VillageClub.Contracts/Models/` (UserDto, RoleDto, ErrorResponse, PagedResult)
- [X] T013 [P] Add shared authentication interfaces to `libs/VillageClub.Contracts/Auth/` (IJwtTokenService, IAuthContext)
- [X] T014 [P] Add shared validation helpers to `libs/VillageClub.Contracts/Validation/` (FluentValidation base validators)
- [X] T015 Create Membership service project structure: `services/membership/src/VillageClub.Membership/VillageClub.Membership.csproj`
- [X] T016 Add EF Core packages and Azure Functions SDK to Membership service project
- [X] T017 Create `services/membership/src/VillageClub.Membership/Data/MembershipDbContext.cs` with Membership schema configuration
- [X] T018 Create `services/membership/src/VillageClub.Membership/Program.cs` with DI configuration (EF Core, Azure Identity, Serilog)
- [X] T019 Create `services/membership/host.json` and `local.settings.json` for Azure Functions configuration
- [X] T020 Add health check endpoint `services/membership/src/VillageClub.Membership/Functions/HealthFunctions.cs` returning JWT public key

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - User Management and Authentication (Priority: P1) 🎯 MVP

**Goal**: Committee members can manage users across three roles (Committee, Volunteer, Member) with JWT-based authentication and role-based access control

**Independent Test**: Create users with different roles, authenticate, and verify role-based permissions work correctly. No other services required.

### Implementation for User Story 1

- [X] T021 [P] [US1] Create User entity `services/membership/src/VillageClub.Membership/Data/Entities/User.cs` with all fields from data-model.md
- [X] T022 [P] [US1] Create RefreshToken entity `services/membership/src/VillageClub.Membership/Data/Entities/RefreshToken.cs`
- [X] T023 [P] [US1] Create AuditLog entity `services/membership/src/VillageClub.Membership/Data/Entities/AuditLog.cs`
- [X] T024 [US1] Configure User entity in MembershipDbContext with indexes (IX_Users_Email, IX_Users_Role, IX_Users_Status)
- [X] T025 [US1] Generate and apply EF Core migrations for Membership schema using `dotnet ef migrations add InitialMembershipSchema`
- [X] T026 [P] [US1] Create UserDto, CreateUserRequest, UpdateUserRequest models in `services/membership/src/VillageClub.Membership/Models/`
- [X] T027 [P] [US1] Create LoginRequest, LoginResponse, RefreshTokenRequest DTOs in `services/membership/src/VillageClub.Membership/Models/`
- [X] T028 [P] [US1] Create FluentValidation validators for CreateUserRequest (email regex, password strength, name validation) in `services/membership/src/VillageClub.Membership/Validators/`
- [X] T029 [US1] Implement JwtTokenService in `services/membership/src/VillageClub.Membership/Services/JwtTokenService.cs` (generate JWT with role claims, validate, refresh)
- [X] T030 [US1] Implement PasswordHashService in `services/membership/src/VillageClub.Membership/Services/PasswordHashService.cs` using BCrypt
- [X] T031 [US1] Implement AuthService in `services/membership/src/VillageClub.Membership/Services/AuthService.cs` (login, logout, refresh token rotation)
- [X] T032 [US1] Implement UserService in `services/membership/src/VillageClub.Membership/Services/UserService.cs` (CRUD operations, role assignment, audit logging)
- [X] T033 [US1] Create AuthFunctions in `services/membership/src/VillageClub.Membership/Functions/AuthFunctions.cs` (POST /api/v1/auth/login, /refresh, /logout)
- [X] T034A [US1] [TDD] Create test project `services/membership/tests/VillageClub.Membership.Tests/VillageClub.Membership.Tests.csproj` with xUnit, Moq, FluentAssertions
- [X] T034B [US1] [TDD] Write unit tests for PasswordHashService (hash generation, verification, invalid passwords) - `tests/Services/PasswordHashServiceTests.cs`
- [X] T034C [US1] [TDD] Write unit tests for JwtTokenService (token generation, validation, expiration, invalid tokens, RSA key handling) - `tests/Services/JwtTokenServiceTests.cs`
- [X] T034D [US1] [TDD] Write unit tests for AuthService (login success/failure, registration, token refresh, revoke, password change) - `tests/Services/AuthServiceTests.cs`
- [X] T034E [US1] [TDD] Write unit tests for UserService (CRUD operations, duplicate email, audit logging, pagination) - `tests/Services/UserServiceTests.cs`
- [X] T034F [US1] [TDD] Write unit tests for FluentValidation validators (valid/invalid inputs, edge cases) - `tests/Validators/ValidatorTests.cs`
- [X] T034G [US1] [TDD] Write integration tests for AuthFunctions (HTTP requests, validation, status codes) - `tests/Functions/AuthFunctionsTests.cs` - 23 passing, 2 skipped pending JWT middleware
- [X] T034H [US1] Create UserFunctions in `services/membership/src/VillageClub.Membership/Functions/UserFunctions.cs` (GET/POST/PUT/DELETE /api/v1/users, GET /api/v1/users/me)
- [ ] T034I [US1] [TDD] Write integration tests for UserFunctions (CRUD endpoints, pagination, authorization) - `tests/Functions/UserFunctionsTests.cs`
- [ ] T035 [US1] Add JWT validation middleware/filter for protected endpoints in Membership service
- [ ] T035A [US1] [TDD] Write tests for JWT middleware (valid/invalid/expired tokens, missing tokens, role-based access)
- [ ] T036 [US1] Add role-based authorization attributes (Committee only for user management endpoints)
- [ ] T036A [US1] [TDD] Write tests for role-based authorization (correct roles allowed, incorrect roles denied)
- [ ] T037 [US1] Configure Serilog structured logging to Application Insights in Program.cs
- [ ] T038 [US1] Add exception handling middleware with proper HTTP status codes and ErrorResponse DTOs
- [ ] T038A [US1] [TDD] Write tests for exception handling middleware (unhandled exceptions, validation errors, proper error responses)
- [ ] T039 [US1] Create `services/membership/Dockerfile` for containerized deployment
- [ ] T040 [US1] Update `infrastructure/main.bicep` to deploy Membership function app with connection strings

**Checkpoint**: User Story 1 complete - Users can be created, authenticated, and role-based access control works. All tests passing.

---

## Phase 4: User Story 6 - Service Discovery and Documentation (Priority: P1)

**Goal**: Developers can discover services, access API documentation via OpenAPI specs, and integrate with the UI application

**Independent Test**: Query APIM service registry, retrieve OpenAPI specs for each service, verify documentation is complete and accurate

### Implementation for User Story 6

- [ ] T041 [P] [US6] Install Swashbuckle.AspNetCore (or NSwag) in Membership service for OpenAPI generation
- [ ] T042 [US6] Configure Swagger/OpenAPI generation in `services/membership/src/VillageClub.Membership/Program.cs` with JWT bearer auth
- [ ] T042A [US6] [TDD] Write tests for OpenAPI spec generation (verify all endpoints documented, schemas present, JWT security defined)
- [ ] T043 [US6] Add XML documentation comments to all Membership API endpoints and models
- [ ] T044 [US6] Create OpenAPI spec `specs/001-create-a-series/contracts/openapi/membership-api.yaml` (auto-generated or manual)
- [ ] T044A [US6] [TDD] Write tests to validate OpenAPI spec compliance (schema validation, required fields, response codes)
- [ ] T045 [US6] Configure APIM policies in `infrastructure/modules/apim.bicep` for service discovery endpoint
- [ ] T046 [US6] Create APIM backend definitions for Membership service with health check integration
- [ ] T046A [US6] [TDD] Write integration tests for APIM backend health check integration
- [ ] T047 [P] [US6] Add APIM JWT validation policy using public key from Membership /health endpoint
- [ ] T047A [US6] [TDD] Write tests for APIM JWT validation (valid tokens pass, invalid/expired tokens rejected)
- [ ] T048 [P] [US6] Configure APIM CORS policy for UI application access
- [ ] T048A [US6] [TDD] Write tests for CORS policy (allowed origins, methods, headers)
- [ ] T049 [US6] Create service registry endpoint in APIM returning all service metadata (name, version, health, OpenAPI URL)
- [ ] T049A [US6] [TDD] Write tests for service registry endpoint (returns all services, correct metadata format)
- [ ] T050 [US6] Document APIM gateway URL and authentication flow in `docs/api-gateway.md`

**Checkpoint**: Service discovery works - Developers can find services and view API documentation. MVP Core Ready (US1 + US6)

---

## Phase 5: User Story 2 - Event Management (Priority: P1)

**Goal**: Committee members can create and manage events; all users can view upcoming and past events

**Independent Test**: Create events, publish them, query as different user roles. Only depends on User Management (US1) for authentication.

### Implementation for User Story 2

- [ ] T051 Create Events service project structure: `services/events/src/VillageClub.Events/VillageClub.Events.csproj`
- [ ] T051A [US2] [TDD] Create test project `services/events/tests/VillageClub.Events.Tests/VillageClub.Events.Tests.csproj` with xUnit, Moq, FluentAssertions
- [ ] T052 Add EF Core packages, Azure Functions SDK, reference to VillageClub.Contracts
- [ ] T053 [P] [US2] Create Event entity `services/events/src/VillageClub.Events/Data/Entities/Event.cs` with all fields from data-model.md
- [ ] T054 [P] [US2] Create EventType enum (SpecialEvent, RegularBarNight, PrivateHire, Fundraiser) in `services/events/src/VillageClub.Events/Models/`
- [ ] T055 [US2] Create EventsDbContext `services/events/src/VillageClub.Events/Data/EventsDbContext.cs` with Events schema configuration
- [ ] T056 [US2] Configure Event entity indexes (IX_Events_StartDateTime, IX_Events_EventType, IX_Events_Status)
- [ ] T057 [US2] Generate and apply EF Core migrations for Events schema using `dotnet ef migrations add InitialEventsSchema`
- [ ] T058 [P] [US2] Create EventDto, CreateEventRequest, UpdateEventRequest models in `services/events/src/VillageClub.Events/Models/`
- [ ] T059 [P] [US2] Create FluentValidation validators for CreateEventRequest (title length, date validation, duration checks)
- [ ] T059A [US2] [TDD] Write unit tests for FluentValidation validators (valid/invalid inputs, date logic, edge cases) - `tests/Validators/ValidatorTests.cs`
- [ ] T060 [US2] Implement EventService in `services/events/src/VillageClub.Events/Services/EventService.cs` (CRUD, state transitions Draft→Published→Completed)
- [ ] T060A [US2] [TDD] Write unit tests for EventService (CRUD operations, state transitions, validation errors, pagination) - `tests/Services/EventServiceTests.cs`
- [ ] T061 [US2] Create EventFunctions in `services/events/src/VillageClub.Events/Functions/EventFunctions.cs` (POST/PUT/DELETE /api/v1/events)
- [ ] T061A [US2] [TDD] Write integration tests for EventFunctions (HTTP requests, validation, status codes, authorization) - `tests/Functions/EventFunctionsTests.cs`
- [ ] T062 [US2] Create EventQueryFunctions in `services/events/src/VillageClub.Events/Functions/EventQueryFunctions.cs` (GET /api/v1/events with filtering, GET /api/v1/events/{id})
- [ ] T062A [US2] [TDD] Write integration tests for EventQueryFunctions (filtering, pagination, not found scenarios) - `tests/Functions/EventQueryFunctionsTests.cs`
- [ ] T063 [US2] Add JWT validation and role-based authorization (Committee only for create/update/delete)
- [ ] T063A [US2] [TDD] Write tests for JWT validation and authorization (valid/invalid tokens, role permissions)
- [ ] T064 [US2] Configure DI and logging in `services/events/src/VillageClub.Events/Program.cs`
- [ ] T065 [US2] Create `services/events/host.json`, `local.settings.json`, health check endpoint
- [ ] T065A [US2] [TDD] Write integration tests for health check endpoint
- [ ] T066 [US2] Add Swagger/OpenAPI generation for Events service
- [ ] T066A [US2] [TDD] Write tests for OpenAPI spec generation and validation
- [ ] T067 [US2] Create `services/events/Dockerfile` for deployment
- [ ] T068 [US2] Update `infrastructure/main.bicep` to deploy Events function app
- [ ] T069 [US2] Add Events service to APIM backend definitions and service registry

**Checkpoint**: User Story 2 complete - Events can be created and viewed. Works independently with US1 authentication.

---

## Phase 6: User Story 5 - Expense Management and Reimbursement (Priority: P2)

**Goal**: Committee members and volunteers can submit expense claims with receipt uploads linked to events; treasurers can approve/reject/reimburse

**Independent Test**: Submit expenses with receipts, link to events (US2), approve/reject as treasurer. Depends on US1 (auth) and US2 (events exist).

### Implementation for User Story 5

- [ ] T070 Create Finance service project structure: `services/finance/src/VillageClub.Finance/VillageClub.Finance.csproj`
- [ ] T070A [US5] [TDD] Create test project `services/finance/tests/VillageClub.Finance.Tests/VillageClub.Finance.Tests.csproj` with xUnit, Moq, FluentAssertions
- [ ] T071 Add EF Core, Azure Functions SDK, Azure.Storage.Blobs, reference to VillageClub.Contracts
- [ ] T072 [P] [US5] Create Expense entity `services/finance/src/VillageClub.Finance/Data/Entities/Expense.cs` with all fields from data-model.md
- [ ] T073 [P] [US5] Create Receipt entity `services/finance/src/VillageClub.Finance/Data/Entities/Receipt.cs`
- [ ] T074 [P] [US5] Create ExpenseStatus enum (PendingReview, Approved, Rejected, Reimbursed) in `services/finance/src/VillageClub.Finance/Models/`
- [ ] T075 [US5] Create FinanceDbContext `services/finance/src/VillageClub.Finance/Data/FinanceDbContext.cs` with Finance schema configuration
- [ ] T076 [US5] Configure Expense entity indexes (IX_Expenses_EventId, IX_Expenses_Status, IX_Expenses_SubmittedById)
- [ ] T077 [US5] Generate and apply EF Core migrations for Finance schema using `dotnet ef migrations add InitialFinanceSchema`
- [ ] T078 [P] [US5] Create ExpenseDto, CreateExpenseRequest, ExpenseSummaryDto models in `services/finance/src/VillageClub.Finance/Models/`
- [ ] T079 [P] [US5] Create FluentValidation validators for CreateExpenseRequest (amount >0, eventId required, receipt validation)
- [ ] T079A [US5] [TDD] Write unit tests for FluentValidation validators (valid/invalid inputs, amount validation, file validation) - `tests/Validators/ValidatorTests.cs`
- [ ] T080 [US5] Implement BlobStorageService in `services/finance/src/VillageClub.Finance/Services/BlobStorageService.cs` (upload, generate SAS token, validate file type via magic bytes)
- [ ] T080A [US5] [TDD] Write unit tests for BlobStorageService (upload success/failure, SAS token generation, magic byte validation, file type rejection) - `tests/Services/BlobStorageServiceTests.cs`
- [ ] T081 [US5] Implement EventValidationService in `services/finance/src/VillageClub.Finance/Services/EventValidationService.cs` (HTTP client to call Events service, validate event exists, circuit breaker with Polly)
- [ ] T081A [US5] [TDD] Write unit tests for EventValidationService (event exists, event not found, circuit breaker behavior, timeout handling) - `tests/Services/EventValidationServiceTests.cs`
- [ ] T082 [US5] Implement ExpenseService in `services/finance/src/VillageClub.Finance/Services/ExpenseService.cs` (submit, approve, reject, reimburse, calculate totals per event)
- [ ] T082A [US5] [TDD] Write unit tests for ExpenseService (submit with receipt, approve/reject/reimburse workflows, status transitions, calculate totals) - `tests/Services/ExpenseServiceTests.cs`
- [ ] T083 [US5] Create ExpenseFunctions in `services/finance/src/VillageClub.Finance/Functions/ExpenseFunctions.cs` (POST /api/v1/expenses with multipart receipt upload)
- [ ] T083A [US5] [TDD] Write integration tests for ExpenseFunctions (multipart upload, validation, authorization) - `tests/Functions/ExpenseFunctionsTests.cs`
- [ ] T084 [US5] Create ExpenseReviewFunctions in `services/finance/src/VillageClub.Finance/Functions/ExpenseReviewFunctions.cs` (PUT /api/v1/expenses/{id}/approve, /reject, /reimburse)
- [ ] T084A [US5] [TDD] Write integration tests for ExpenseReviewFunctions (approve/reject/reimburse, Treasurer authorization, invalid status transitions) - `tests/Functions/ExpenseReviewFunctionsTests.cs`
- [ ] T085 [US5] Create ExpenseQueryFunctions in `services/finance/src/VillageClub.Finance/Functions/ExpenseQueryFunctions.cs` (GET /api/v1/expenses, GET /api/v1/expenses/{id}/receipt returns SAS URL)
- [ ] T085A [US5] [TDD] Write integration tests for ExpenseQueryFunctions (pagination, filtering, SAS URL generation) - `tests/Functions/ExpenseQueryFunctionsTests.cs`
- [ ] T086 [US5] Add JWT validation and role-based authorization (Treasurer role for approve/reject/reimburse)
- [ ] T086A [US5] [TDD] Write tests for JWT validation and role-based authorization (Treasurer vs non-Treasurer access)
- [ ] T087 [US5] Configure DI, logging, and Azure Blob Storage client with managed identity in Program.cs
- [ ] T088 [US5] Create `services/finance/host.json`, `local.settings.json`, health check endpoint
- [ ] T088A [US5] [TDD] Write integration tests for health check endpoint
- [ ] T089 [US5] Add Swagger/OpenAPI generation for Finance service
- [ ] T089A [US5] [TDD] Write tests for OpenAPI spec generation and validation
- [ ] T090 [US5] Create `services/finance/Dockerfile` for deployment
- [ ] T091 [US5] Update `infrastructure/main.bicep` to deploy Finance function app with Blob Storage connection
- [ ] T092 [US5] Add Finance service to APIM backend definitions and service registry

**Checkpoint**: User Story 5 complete - Expenses can be submitted with receipts, approved, and reimbursed. Integrates with US1 (auth) and US2 (event validation).

---

## Phase 7: User Story 3 - Shift Management for Bar Volunteers (Priority: P2)

**Goal**: Committee members create shifts for bar hours and events; volunteers sign up for shifts; committee members track assignments

**Independent Test**: Create shifts (linked to events from US2), volunteers sign up, validate no overlapping shifts. Depends on US1 (auth) and US2 (events).

### Implementation for User Story 3

- [ ] T093 Create Scheduling service project structure: `services/scheduling/src/VillageClub.Scheduling/VillageClub.Scheduling.csproj`
- [ ] T093A [US3] [TDD] Create test project `services/scheduling/tests/VillageClub.Scheduling.Tests/VillageClub.Scheduling.Tests.csproj` with xUnit, Moq, FluentAssertions
- [ ] T094 Add EF Core packages, Azure Functions SDK, reference to VillageClub.Contracts
- [ ] T095 [P] [US3] Create Shift entity `services/scheduling/src/VillageClub.Scheduling/Data/Entities/Shift.cs` with all fields from data-model.md
- [ ] T096 [P] [US3] Create ShiftAssignment entity `services/scheduling/src/VillageClub.Scheduling/Data/Entities/ShiftAssignment.cs`
- [ ] T097 [P] [US3] Create ShiftType enum (BarShift, EventShift) and ShiftStatus enum in `services/scheduling/src/VillageClub.Scheduling/Models/`
- [ ] T098 [US3] Create SchedulingDbContext `services/scheduling/src/VillageClub.Scheduling/Data/SchedulingDbContext.cs` with Scheduling schema configuration
- [ ] T099 [US3] Configure Shift entity indexes (IX_Shifts_StartDateTime, IX_Shifts_EventId, IX_Shifts_Status)
- [ ] T100 [US3] Generate and apply EF Core migrations for Scheduling schema using `dotnet ef migrations add InitialSchedulingSchema`
- [ ] T101 [P] [US3] Create ShiftDto, CreateShiftRequest, ShiftAssignmentDto models in `services/scheduling/src/VillageClub.Scheduling/Models/`
- [ ] T102 [P] [US3] Create FluentValidation validators for CreateShiftRequest (date validation, capacity >0, event validation if EventShift)
- [ ] T102A [US3] [TDD] Write unit tests for FluentValidation validators (valid/invalid inputs, capacity validation, date logic) - `tests/Validators/ValidatorTests.cs`
- [ ] T103 [US3] Implement EventValidationService in `services/scheduling/src/VillageClub.Scheduling/Services/EventValidationService.cs` (HTTP client to Events service with Polly circuit breaker)
- [ ] T103A [US3] [TDD] Write unit tests for EventValidationService (event exists, event not found, circuit breaker behavior, retry logic) - `tests/Services/EventValidationServiceTests.cs`
- [ ] T104 [US3] Implement ShiftService in `services/scheduling/src/VillageClub.Scheduling/Services/ShiftService.cs` (CRUD shifts, auto-update status to Filled when capacity reached)
- [ ] T104A [US3] [TDD] Write unit tests for ShiftService (CRUD operations, auto-status update when filled, capacity logic, pagination) - `tests/Services/ShiftServiceTests.cs`
- [ ] T105 [US3] Implement ShiftAssignmentService in `services/scheduling/src/VillageClub.Scheduling/Services/ShiftAssignmentService.cs` (assign volunteer, validate no overlapping shifts, cancel assignment)
- [ ] T105A [US3] [TDD] Write unit tests for ShiftAssignmentService (assign volunteer, detect overlapping shifts, cancel assignment, capacity checking) - `tests/Services/ShiftAssignmentServiceTests.cs`
- [ ] T106 [US3] Create ShiftFunctions in `services/scheduling/src/VillageClub.Scheduling/Functions/ShiftFunctions.cs` (POST/PUT/DELETE /api/v1/shifts)
- [ ] T106A [US3] [TDD] Write integration tests for ShiftFunctions (CRUD operations, validation, Committee authorization) - `tests/Functions/ShiftFunctionsTests.cs`
- [ ] T107 [US3] Create ShiftAssignmentFunctions in `services/scheduling/src/VillageClub.Scheduling/Functions/ShiftAssignmentFunctions.cs` (POST/DELETE /api/v1/shifts/{id}/assignments, GET /api/v1/shifts/my-assignments)
- [ ] T107A [US3] [TDD] Write integration tests for ShiftAssignmentFunctions (sign up, cancel, get my assignments, overlap detection) - `tests/Functions/ShiftAssignmentFunctionsTests.cs`
- [ ] T108 [US3] Add JWT validation and role-based authorization (Committee for create/update/delete shifts, Volunteer for sign up)
- [ ] T108A [US3] [TDD] Write tests for JWT validation and role-based authorization (Committee vs Volunteer permissions)
- [ ] T109 [US3] Configure DI and logging in `services/scheduling/src/VillageClub.Scheduling/Program.cs`
- [ ] T110 [US3] Create `services/scheduling/host.json`, `local.settings.json`, health check endpoint
- [ ] T110A [US3] [TDD] Write integration tests for health check endpoint
- [ ] T111 [US3] Add Swagger/OpenAPI generation for Scheduling service
- [ ] T111A [US3] [TDD] Write tests for OpenAPI spec generation and validation
- [ ] T112 [US3] Create `services/scheduling/Dockerfile` for deployment
- [ ] T113 [US3] Update `infrastructure/main.bicep` to deploy Scheduling function app
- [ ] T114 [US3] Add Scheduling service to APIM backend definitions and service registry

**Checkpoint**: User Story 3 complete - Shifts can be created and volunteers can sign up. Integrates with US1 (auth) and US2 (event validation).

---

## Phase 8: User Story 4 - Stock Alert Management (Priority: P3)

**Goal**: Volunteers can report low stock items; committee members and bar managers can view and resolve alerts

**Independent Test**: Volunteers create stock alerts, committee members view and resolve them. Only depends on US1 for authentication - simplest story.

### Implementation for User Story 4

- [ ] T115 Create Bar service project structure: `services/bar/src/VillageClub.Bar/VillageClub.Bar.csproj`
- [ ] T115A [US4] [TDD] Create test project `services/bar/tests/VillageClub.Bar.Tests/VillageClub.Bar.Tests.csproj` with xUnit, Moq, FluentAssertions
- [ ] T116 Add EF Core packages, Azure Functions SDK, reference to VillageClub.Contracts
- [ ] T117 [P] [US4] Create StockAlert entity `services/bar/src/VillageClub.Bar/Data/Entities/StockAlert.cs` with all fields from data-model.md
- [ ] T118 [P] [US4] Create StockUrgency enum (Low, Medium, High) and StockAlertStatus enum in `services/bar/src/VillageClub.Bar/Models/`
- [ ] T119 [US4] Create BarDbContext `services/bar/src/VillageClub.Bar/Data/BarDbContext.cs` with Bar schema configuration
- [ ] T120 [US4] Configure StockAlert entity indexes (IX_StockAlerts_ItemName, IX_StockAlerts_Status, IX_StockAlerts_ReportedAt)
- [ ] T121 [US4] Generate and apply EF Core migrations for Bar schema using `dotnet ef migrations add InitialBarSchema`
- [ ] T122 [P] [US4] Create StockAlertDto, CreateStockAlertRequest models in `services/bar/src/VillageClub.Bar/Models/`
- [ ] T123 [P] [US4] Create FluentValidation validators for CreateStockAlertRequest (itemName required, urgency valid)
- [ ] T123A [US4] [TDD] Write unit tests for FluentValidation validators (valid/invalid inputs, urgency validation) - `tests/Validators/ValidatorTests.cs`
- [ ] T124 [US4] Implement StockAlertService in `services/bar/src/VillageClub.Bar/Services/StockAlertService.cs` (create alert, list active alerts, resolve, group by item)
- [ ] T124A [US4] [TDD] Write unit tests for StockAlertService (create alert, list/filter alerts, resolve alert, group by item logic) - `tests/Services/StockAlertServiceTests.cs`
- [ ] T125 [US4] Create StockAlertFunctions in `services/bar/src/VillageClub.Bar/Functions/StockAlertFunctions.cs` (GET/POST /api/v1/stock-alerts, PUT /api/v1/stock-alerts/{id}/resolve)
- [ ] T125A [US4] [TDD] Write integration tests for StockAlertFunctions (create, list, resolve, authorization by role) - `tests/Functions/StockAlertFunctionsTests.cs`
- [ ] T126 [US4] Add JWT validation and role-based authorization (Volunteer can create, Committee can resolve)
- [ ] T126A [US4] [TDD] Write tests for JWT validation and role-based authorization (Volunteer vs Committee permissions)
- [ ] T127 [US4] Configure DI and logging in `services/bar/src/VillageClub.Bar/Program.cs`
- [ ] T128 [US4] Create `services/bar/host.json`, `local.settings.json`, health check endpoint
- [ ] T128A [US4] [TDD] Write integration tests for health check endpoint
- [ ] T129 [US4] Add Swagger/OpenAPI generation for Bar service
- [ ] T129A [US4] [TDD] Write tests for OpenAPI spec generation and validation
- [ ] T130 [US4] Create `services/bar/Dockerfile` for deployment
- [ ] T131 [US4] Update `infrastructure/main.bicep` to deploy Bar function app
- [ ] T132 [US4] Add Bar service to APIM backend definitions and service registry

**Checkpoint**: User Story 4 complete - Stock alerts work end-to-end. All user stories now implemented!

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Production readiness improvements affecting multiple services

- [ ] T133 [P] Add pre-warming timer trigger (Fri/Sat 7:45pm) to each service to mitigate cold starts
- [ ] T133A [P] [TDD] Write tests for pre-warming timer triggers (verify trigger schedule, execution success)
- [ ] T134 [P] Configure Application Insights connection for all services in Bicep deployment
- [ ] T135 Add correlation ID middleware to all services for distributed tracing across service calls
- [ ] T135A [TDD] Write tests for correlation ID middleware (verify ID propagation, header injection, logging context)
- [ ] T136 [P] Create integration test project `tests/VillageClub.IntegrationTests/` using Azure Functions local runtime
- [ ] T136A [P] [TDD] Write end-to-end integration tests for cross-service workflows (expense approval flow, shift assignment with events)
- [ ] T137 [P] Create contract test project `tests/VillageClub.ContegrationTests/` for inter-service API validation
- [ ] T137A [P] [TDD] Write contract tests for all inter-service API calls (Events validation from Finance/Scheduling)
- [ ] T138 Add rate limiting policies in APIM to prevent abuse (100 requests/minute per user)
- [ ] T138A [TDD] Write tests for rate limiting policies (verify throttling, 429 responses, rate limit headers)
- [ ] T139 Configure Azure SQL Database firewall rules and enable audit logging in Bicep
- [ ] T140 [P] Create deployment pipeline `azure-pipelines.yml` for CI/CD (build, test, deploy to staging/production)
- [ ] T140A [P] [TDD] Write tests for CI/CD pipeline stages (verify build, test execution, deployment gates)
- [ ] T141 [P] Document local development setup in `README.md` based on quickstart.md
- [ ] T142 Add API versioning strategy documentation in `docs/api-versioning.md`
- [ ] T143 Create runbook `docs/operations/incident-response.md` for 15-minute RTO scenarios
- [ ] T144 Validate all quickstart.md scenarios work end-to-end with deployed services
- [ ] T144A [TDD] Write automated acceptance tests for all quickstart scenarios (user registration to event creation flow)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational - Must complete first (authentication required by all)
- **User Story 6 (Phase 4)**: Depends on US1 - Service discovery needs at least one service (Membership)
- **User Story 2 (Phase 5)**: Depends on US1 - Requires authentication
- **User Story 5 (Phase 6)**: Depends on US1 + US2 - Expenses must link to events
- **User Story 3 (Phase 7)**: Depends on US1 + US2 - Shifts can be linked to events
- **User Story 4 (Phase 8)**: Depends on US1 only - Simplest integration
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Foundation - No dependencies on other stories
- **User Story 6 (P1)**: Depends on US1 (needs a service to document)
- **User Story 2 (P1)**: Depends on US1 (auth) - Can run in parallel with US6
- **User Story 5 (P2)**: Depends on US1 + US2 (expenses link to events)
- **User Story 3 (P2)**: Depends on US1 + US2 (shifts link to events) - Can run in parallel with US5
- **User Story 4 (P3)**: Depends on US1 only - Can run after any P1 story completes

### Within Each User Story

- Models/Entities created in parallel before services
- Services depend on models/entities
- Functions/endpoints depend on services
- Authorization/middleware after core implementation
- Deployment after all service code complete

### Parallel Opportunities

- **Phase 1 (Setup)**: All tasks marked [P] can run in parallel (T002-T010)
- **Phase 2 (Foundational)**: Tasks T012-T014 can run in parallel; T021-T023 can run in parallel
- **Phase 3 (US1)**: Entities T021-T023 parallel; Models T026-T028 parallel; Auth+User functions T033-T034 parallel
- **Phase 4 (US6)**: T041, T047-T048 can run in parallel
- **Phase 5 (US2)**: Entity+Enum T053-T054 parallel; Models+Validators T058-T059 parallel
- **Phase 6 (US5)**: Entities T072-T073 parallel; Models+Validators T078-T079 parallel
- **Phase 7 (US3)**: Entities T095-T096 parallel; Models+Validators T101-T102 parallel
- **Phase 8 (US4)**: Entity+Enums T117-T118 parallel; Models+Validators T122-T123 parallel
- **Phase 9 (Polish)**: Most tasks marked [P] can run in parallel

**Critical Path**: Setup → Foundational → US1 → US2 → US5 (if prioritizing expenses) OR US3 (if prioritizing shifts) → US4 → Polish

---

## Parallel Example: User Story 2 (Event Management)

```bash
# After Foundational phase completes, launch User Story 2 entity creation:
Task T053: "Create Event entity" 
Task T054: "Create EventType enum" 

# Then launch models and validators together:
Task T058: "Create EventDto, CreateEventRequest, UpdateEventRequest models"
Task T059: "Create FluentValidation validators"

# Multiple developers can work on different user stories after US1 completes:
Developer A: User Story 2 (Events)
Developer B: User Story 4 (Stock Alerts) - simpler, no event dependency
```

---

## Implementation Strategy

### MVP First (Minimum Viable Product)

**Recommended MVP Scope**: User Stories 1, 6, and 2 only

1. **Phase 1**: Setup (T001-T010) - 1-2 days
2. **Phase 2**: Foundational (T011-T020) - 2-3 days
3. **Phase 3**: User Story 1 - Authentication & User Management (T021-T040) - 5-7 days
4. **Phase 4**: User Story 6 - Service Discovery (T041-T050) - 2-3 days
5. **Phase 5**: User Story 2 - Event Management (T051-T069) - 4-5 days
6. **STOP and VALIDATE**: MVP delivers user management, authentication, event creation/viewing, and API documentation
7. **Deploy to staging**, demonstrate to stakeholders, gather feedback

**MVP Value**: Establishes authentication foundation, enables event planning (core club activity), and provides API documentation for UI development to begin in parallel.

### Incremental Delivery (Recommended Approach)

1. **Foundation** (Setup + Foundational) → 3-5 days → Database and auth framework ready
2. **MVP Release** (US1 + US6 + US2) → 12-15 days → Users, events, API docs → Deploy & Demo
3. **Increment 2** (US5 OR US3) → 8-10 days → Add either expense management or shift scheduling → Deploy & Demo
4. **Increment 3** (US3 OR US5) → 8-10 days → Add the other P2 story → Deploy & Demo
5. **Increment 4** (US4) → 5-6 days → Stock alerts → Deploy & Demo
6. **Polish** (Phase 9) → 3-5 days → Production hardening → Final Release

**Total Estimate**: 40-55 days for complete implementation with all user stories

### Parallel Team Strategy

With **3 developers** after Foundational phase completes:

- **Developer A**: User Story 1 (critical path) → Then User Story 5 (Expenses)
- **Developer B**: Wait for US1, then User Story 6 (Service Discovery) → Then User Story 3 (Shifts)
- **Developer C**: Wait for US1, then User Story 2 (Events) → Then User Story 4 (Stock Alerts)

**Coordination points**:
- All wait for Foundational (Phase 2) to complete
- B & C wait for A to finish US1 (authentication)
- US5 and US3 both wait for US2 (events) to complete
- Regular sync on shared contracts library changes

---

## Notes

- **[P] tasks** = different files, no dependencies, can run in parallel
- **[Story] label** maps task to specific user story (US1-US6) for traceability
- **[TDD] label** = Test-Driven Development tasks - tests written before or alongside implementation
- Each user story is independently completable and testable (except dependencies noted)
- **TDD is REQUIRED** - All implementation follows Red-Green-Refactor cycle per organizational constitution
- Test tasks (marked [TDD]) should be completed immediately after their corresponding implementation tasks
- Commit after each task or logical group with all tests passing
- Stop at any checkpoint to validate story independently before proceeding
- **Cost optimization**: Serverless-first architecture scales to zero when idle
- **Security**: JWT validation at API Gateway, role-based authorization per service
- **Resilience**: Polly circuit breaker for inter-service HTTP calls
- **Observability**: Structured logging to Application Insights, health checks for service discovery

---

## Critical Success Factors

1. **Complete Foundational Phase 2 first** - Nothing else can work without authentication and database schemas
2. **User Story 1 is the foundation** - All other stories depend on authentication/authorization
3. **Event Management (US2) is a dependency** - US3 and US5 both need events to exist
4. **Validate inter-service communication** - Test circuit breakers and error handling when services are down
5. **Database schema isolation** - Ensure services never cross schema boundaries directly
6. **API Gateway configuration** - APIM must validate JWT before routing to services
7. **Receipt storage** - Test blob upload, SAS token generation, and file validation thoroughly
8. **Cold start mitigation** - Pre-warming timers during operating hours are essential for UX