# Coates Village Club Server Constitution

<!--
Version Change: 2.2.1 → 2.3.0 (Minor Update)

MINOR changes:
- Added new Principle IX: Local-First Development (NON-NEGOTIABLE)
- Updated Development Process to require local testing before Azure deployment
- Added local development metrics to Quality Metrics section

Added sections:
- Principle IX: Local-First Development - requires all features to be fully testable locally
  using emulators, containerized services, and local testing tools before any Azure deployment

Modified sections:
- Development Process: Added local testing verification steps before deployment
- Quality Metrics: Added Local Development Metrics section

Rationale:
Current development workflow requires publishing to Azure for every test cycle, which is
inefficient, slow, and costly. Local-first development accelerates the development cycle,
reduces cloud costs, and enables reliable debugging. Developers must be able to run the
entire system locally using Azure emulators (Azurite, Functions Core Tools), containerized
databases (SQL Server in Docker), and local testing tools. This principle ensures rapid
iteration and quality validation before deployment.

Benefits:
- Faster development cycle (seconds vs. minutes for feedback)
- No cloud costs during development
- Reliable debugging with full access to logs and state
- Offline development capability
- Reduced risk of breaking shared Azure resources
- Easier onboarding for new developers

Templates requiring updates:
✅ Updated: Constitution file
⚠️  Review needed: plan-template.md, spec-template.md, tasks-template.md
    (ensure local testing guidance is included in development workflows)

Follow-up TODOs: None
-->


## Project Overview

The Coates Village Club Server is a **microservices-based monorepo** serving the operational and member management needs of a village social club. The system is decomposed into five core services plus an API Gateway, designed for independent deployment while sharing common code through the monorepo structure.

## Monorepo Structure

The project MUST follow this organizational structure:

```
CoatesVillageClubServer/
├── services/                  # Microservices (independently deployable)
│   ├── membership/           # User accounts, authentication, roles
│   ├── events/               # Calendar, special events, opening hours
│   ├── scheduling/           # Volunteer rota management
│   ├── bar/                  # Point-of-Sale, inventory tracking
│   ├── notifications/        # Email, SMS, push notifications
│   └── api-gateway/          # Request routing, authentication gateway
├── libs/                     # Shared libraries (reusable across services)
│   ├── common-types/         # Shared data models and interfaces
│   ├── common-utils/         # Utility functions, helpers
│   └── contracts/            # Service contracts and API definitions
├── infrastructure/           # IaC (Bicep/Terraform) for Azure deployment
├── docs/                     # Architecture, API specs, runbooks
├── .specify/                 # SpecKit governance and templates
└── tests/                    # Cross-service integration/E2E tests
```

## Service Catalog

### Membership Service ("Who's Who")
**Purpose**: Single source of truth for people and authentication  
**Responsibilities**:
- Member registration and subscription management
- Role and permission management (Member, Volunteer, Committee/Admin)
- Authentication (login/logout)
- User profile CRUD operations

### Events Service ("What's On")
**Purpose**: Club calendar and event management  
**Responsibilities**:
- CRUD operations for special events (quiz nights, live music, etc.)
- Regular opening hours management (Fri/Sat 8pm-12am)
- Event details, descriptions, timing
- Simple RSVP tracking

### Scheduling Service ("Who's Working")
**Purpose**: Volunteer rota for bar operations  
**Responsibilities**:
- Committee creates empty shifts (time slots)
- Volunteers view and sign up for shifts
- Coordination with Events Service for special event staffing
- Shift reminder generation (via Notifications Service)

### Bar Service ("The Till")
**Purpose**: Operational bar management during service  
**Responsibilities**:
- Point-of-Sale interface for volunteers
- Basic inventory tracking (drinks, consumables)
- Sales logging and reporting
- Committee-facing sales analytics

### Notification Service ("The Messenger")
**Purpose**: Outbound messaging utility (stateless)  
**Responsibilities**:
- Send emails, SMS, push notifications
- Template-based message rendering
- Delivery status tracking
- Receives commands from other services (does not initiate)

### API Gateway
**Purpose**: Unified entry point and request orchestration  
**Responsibilities**:
- Request routing to appropriate services
- Authentication validation (delegates to Membership Service)
- Rate limiting and throttling
- Request/response transformation

## Core Principles

### I. Test-First Development (NON-NEGOTIABLE)
All code MUST be developed following Test-Driven Development (TDD) principles with strict Red-Green-Refactor discipline:

**TDD Workflow (MANDATORY)**:
1. **Write Test First**: Write a test for the next unit of functionality
2. **Verify RED**: Run the test and confirm it FAILS (proves test validity)
3. **Write Minimal Code**: Implement just enough to make the test pass
4. **Verify GREEN**: Run the test and confirm it PASSES
5. **Refactor**: Clean up code while keeping tests green
6. **Repeat**: Continue cycle for next unit of functionality

**Test Requirements**:
- Tests MUST be written before implementation code (NO exceptions)
- Tests MUST fail initially to verify test validity (RED phase verification)
- Implementation MUST be written to make tests pass (GREEN phase)
- Only tested code can be committed to the repository
- Test coverage MUST be maintained at 80% or higher
- Tests MUST follow the Arrange-Act-Assert pattern:
  - Arrange: Set up the test conditions and inputs
  - Act: Call the specific method being tested
  - Assert: Verify the output or state changes
- Tests MUST test actual implementation methods, not mock behaviors
- Code MUST be structured to enable testing of concrete implementations
- Tests MUST NOT contain business logic or implementation details
- Each test MUST focus on a single method or behavior
- Test names MUST clearly describe the scenario being tested

**Testing Scope Exclusions**:
Third-party libraries and framework middleware are EXCLUDED from testing requirements:
- Logging frameworks (e.g., Serilog) - assume logging occurs as configured
- Validation frameworks (e.g., FluentValidation) - assume validation rules execute correctly
- ORM libraries (e.g., Entity Framework Core) - assume database operations work as documented
- Framework middleware (e.g., Azure Functions HTTP pipeline) - assume request/response handling works
- Authentication libraries (e.g., JWT validation) - assume token processing works per specification
- Serialization libraries (e.g., System.Text.Json) - assume serialization/deserialization works correctly
- HTTP clients (e.g., HttpClient) - assume HTTP communication works as documented

**What MUST be tested**:
- OUR business logic that uses these libraries
- OUR configuration and setup of these libraries
- OUR integration points and adapters
- OUR custom middleware and extensions
- OUR error handling around third-party components
- OUR domain models and services

**Testing blocked by internal APIs**: When third-party libraries expose internal/inaccessible APIs that prevent proper test setup, document the limitation and ensure:
- Production code compiles and runs correctly
- Integration tests at higher levels cover the functionality
- Manual testing confirms expected behavior
- The limitation is documented in test comments or ADRs

Rationale: TDD with explicit Red-Green-Refactor discipline ensures code reliability, maintains quality standards, and provides living documentation of intended behavior. Verifying tests fail before implementing prevents false positives and ensures tests actually validate the implementation. Proper test structure ensures tests validate actual implementation behavior rather than mocked responses. Testing scope exclusions prevent wasted effort on verifying third-party code works as documented while maintaining focus on our business logic quality.


### II. Code Quality Standards
Code MUST adhere to established quality metrics and practices:
- Follow consistent coding style and naming conventions
- Maximum cyclomatic complexity of 10 per function
- Methods MUST not exceed 30 lines of code
- Classes MUST have single responsibility
- Code duplication MUST be less than 5%
- All code MUST pass linting and static analysis
- Build warnings MUST be addressed:
  - Production code MUST have zero warnings
  - Test code MAY suppress style warnings via NoWarn configuration
  - Intentional TODO comments are acceptable with justification
  - Documentation warnings (SA1600, SA1601, etc.) MUST be fixed in production code
  - Style warnings (SA1124, SA1202, etc.) MAY be suppressed in test projects
  - All public APIs MUST have complete XML documentation
  - Documentation MUST follow StyleCop standards (periods, proper formatting)
- Strict separation of concerns MUST be maintained:
  - Business logic MUST be separated from infrastructure concerns
  - Data access MUST be isolated from business logic
  - Cross-cutting concerns MUST be properly abstracted
  - Each layer MUST communicate through well-defined interfaces
  - Configuration MUST be separated from application code
- Functional programming principles MUST be followed:
  - Methods MUST be stateless and pure where possible
  - Side effects MUST be isolated and explicitly defined
  - Functions MUST return new state rather than modify existing state
  - Immutable data structures MUST be preferred
  - Methods MUST have predictable outputs for given inputs
  - State changes MUST be handled through explicit state management
  - Shared state MUST be avoided unless absolutely necessary
  - Functions MUST be composable and single-purpose

Rationale: Consistent code quality standards, clear separation of concerns, and functional programming principles ensure maintainability, testability, and reduce technical debt. Stateless, pure functions make testing more reliable and predictable by eliminating hidden dependencies and side effects. This approach naturally supports TDD by making behavior more predictable and isolated.

### III. Performance First
Performance requirements MUST be defined and validated:
- API response times MUST be under 200ms for 95% of requests
- Resource usage limits MUST be specified and monitored
- Performance tests MUST be automated and included in CI/CD
- Load testing MUST verify handling of 10x expected load
- Memory leaks and resource cleanup MUST be verified

Rationale: Performance is a feature that affects user experience and operational costs.

### IV. Comprehensive Testing
Multiple testing levels MUST be implemented and maintained with realistic environments:

**Testing Levels**:
- Unit tests for individual components
- Integration tests for component interactions
- End-to-end tests for critical user flows
- Load tests for performance validation
- Security tests for vulnerability detection
- Contract tests for service interfaces (MANDATORY before implementation)
- Tests MUST be automated and repeatable

**Realistic Testing Environments (MANDATORY)**:
- Tests MUST use real databases over mocks where practical
  - Use in-memory or containerized databases for integration tests
  - Connection strings and schemas MUST match production structure
  - Test data MUST represent realistic scenarios
- Tests MUST use actual service instances over stubs where practical
  - Integration tests MUST test real inter-service communication
  - Use test containers or dedicated test environments
  - Mock only external third-party services (payment gateways, SMS providers)
- Contract tests are MANDATORY before implementation
  - Service interfaces MUST be defined and tested before implementation begins
  - Consumer-driven contract tests MUST validate service agreements
  - Contract violations MUST fail the build
- Test environments MUST be isolated and reproducible
- Test data MUST be deterministic and resettable

**Third-Party Components (Testing Scope)**:
The following are EXCLUDED from our testing requirements (we assume they work as documented):
- **Framework Middleware**: Azure Functions pipeline, ASP.NET Core middleware stack
- **Logging Libraries**: Serilog, Application Insights SDK, logging infrastructure
- **Validation Libraries**: FluentValidation rule execution, built-in data annotations
- **ORM Libraries**: Entity Framework Core query generation and execution
- **Authentication Libraries**: JWT token parsing, cryptographic operations
- **Serialization**: System.Text.Json, Newtonsoft.Json serialization correctness
- **HTTP Infrastructure**: HttpClient, HTTP protocol handling
- **Cloud SDKs**: Azure SDK client libraries, AWS SDK operations

**What we DO test with third-party components**:
- Our configuration of these libraries (correct connection strings, options, settings)
- Our usage patterns and integration points
- Our custom extensions or wrappers around these libraries
- Error handling and edge cases in our code that uses these libraries
- Business logic that depends on results from these libraries

**When testing is blocked**: If third-party internal APIs prevent test setup:
- Document the limitation clearly
- Ensure production code works via manual or integration testing
- Focus on testing at higher integration levels
- Consider alternative approaches that are more testable

Rationale: Comprehensive testing with realistic environments ensures reliability and catches integration issues early. Real databases and services reveal actual behavior, connection issues, and performance characteristics that mocks cannot simulate. Contract testing before implementation prevents integration surprises and ensures clear service boundaries. Excluding third-party library internals from testing scope allows us to focus on our business logic while trusting professionally maintained libraries to work as documented.


### V. Maintainable Architecture
Architecture MUST follow proven design principles:
- Clear separation of concerns
- Dependency injection for loose coupling
- Interface-based design for flexibility
- Proper error handling and logging
- Documentation of architectural decisions via ADRs
- Version control of configuration

**Microservices-specific requirements**:
- Each service MUST be independently deployable
- Services MUST communicate via well-defined APIs (REST, message queues)
- Services MUST NOT share databases (each owns its data)
- Shared code MUST reside in `libs/` and be versioned
- API contracts MUST be documented in `libs/contracts/`
- Breaking changes MUST follow semantic versioning
- Services MUST implement health checks and graceful shutdown

Rationale: Maintainable architecture reduces long-term costs and enables rapid feature development. Microservices architecture provides scalability, independent deployment, and technology flexibility while the monorepo structure simplifies code sharing and cross-service refactoring.

### VI. Service Boundaries
Service boundaries MUST be respected to maintain system integrity:
- Services MUST NOT directly access another service's database
- Inter-service communication MUST use defined APIs or message queues
- Data ownership MUST be clear and exclusive to one service
- Shared types MUST be defined in `libs/common-types/`
- Services MUST be loosely coupled and highly cohesive
- Circular dependencies between services are FORBIDDEN
- Each service MUST have a single, well-defined responsibility

Rationale: Clear service boundaries prevent coupling, enable independent evolution, and maintain system modularity. This principle ensures each service can be developed, tested, and deployed without breaking others.

### VII. Microservices Architecture
The monorepo MUST support independent microservice lifecycle:
- Each service in `services/` MUST have its own `src/` and `Dockerfile`
- Services MUST be independently deployable to Azure Functions or containers
- Shared libraries in `libs/` MUST be versioned and published internally
- Cross-service changes MUST be coordinated via API versioning
- Infrastructure as Code MUST support per-service resource provisioning
- Monitoring and logging MUST be service-aware
- Feature development MUST specify which service(s) are affected

Rationale: Independent deployability enables faster iteration, reduces blast radius of changes, and allows horizontal scaling of individual services based on load patterns.

### VIII. Library-First Development (NON-NEGOTIABLE)
Every feature MUST begin as a standalone library before application integration:

**Library Extraction Requirements**:
- All new features MUST be implemented as libraries in `libs/` first
- Features MUST NOT be implemented directly in service code
- Libraries MUST be designed for reusability across multiple services
- Libraries MUST have clear, well-defined interfaces
- Libraries MUST be independently testable without service dependencies
- Application code MUST only orchestrate library components

**Library Design Principles**:
- Each library MUST have a single, well-defined responsibility
- Libraries MUST be framework-agnostic where possible
- Libraries MUST expose pure functions or stateless classes
- Dependencies MUST be injected, not hard-coded
- Libraries MUST include comprehensive unit tests
- Libraries MUST have complete API documentation

**Integration Process**:
1. Design library interface and contracts
2. Implement library with full test coverage
3. Verify library works in isolation
4. Integrate library into service(s)
5. Test service integration with library

Rationale: Library-first development ensures reusability, prevents tight coupling between features and application code, promotes better architecture through clear interface design, and enables testing features in isolation before service integration. This approach makes code more maintainable and reduces duplication across services.

### IX. Local-First Development (NON-NEGOTIABLE)
All features MUST be fully testable locally before deployment to Azure:

**Local Development Environment Requirements**:
- All Azure services MUST have local equivalents for development
- Local development setup MUST be documented and automated where possible
- Developers MUST be able to run the complete system on their workstation
- Local testing MUST verify functionality before any Azure deployment
- Cloud resources MUST NOT be required for development or testing

**Local Service Equivalents (MANDATORY)**:
- **Azure Functions**: Use Azure Functions Core Tools (`func start`) for local execution
- **Azure Storage**: Use Azurite emulator for blob, queue, and table storage
- **Azure SQL Database**: Use SQL Server in Docker or SQL Server Express with LocalDB
- **Azure Cosmos DB**: Use Cosmos DB emulator (Windows) or Docker container
- **Azure Service Bus**: Use local emulator or Docker container (Emulator for Azure Service Bus)
- **Application Insights**: Use console logging or local Application Insights emulator
- **Key Vault**: Use local.settings.json or environment variables for secrets during development

**Local Testing Workflow (MANDATORY)**:
1. **Setup**: Developer runs local environment setup (one-time or scripted)
2. **Development**: Code and test using local services (Azurite, local SQL, func start)
3. **Unit Tests**: Run against in-memory or local containerized dependencies
4. **Integration Tests**: Run against local service instances (Functions Core Tools + Azurite + local DB)
5. **Local Verification**: Manually test complete user flows locally
6. **Only After Local Success**: Deploy to Azure for environment-specific validation
7. **Never**: Deploy to Azure to test a bug fix or new feature before local verification

**Local Configuration**:
- Each service MUST have `local.settings.json` for local Azure Functions configuration
- Local connection strings MUST point to local services (localhost, Azurite defaults)
- Local settings MUST NOT be committed to version control (use `.gitignore`)
- Example/template local settings MUST be provided (e.g., `local.settings.json.example`)
- Environment-specific settings MUST be documented in service README files

**Local Development Standards**:
- Local setup time MUST be under 30 minutes for new developers (including tool installation)
- Local test execution MUST complete in under 5 minutes for full suite
- Local services MUST start successfully without manual configuration steps
- Local debugging MUST provide full access to logs, breakpoints, and state inspection
- Documentation MUST include troubleshooting guide for common local setup issues

**Prohibited Practices**:
- Publishing to Azure to test code changes before local verification
- Requiring Azure resources for unit or integration tests
- Hard-coding Azure connection strings or endpoints in code
- Relying on cloud services for developer productivity

Rationale: Local-first development dramatically accelerates the development cycle, reduces cloud costs, and provides reliable debugging capabilities. Requiring Azure deployment for every test creates a slow, expensive, and frustrating workflow. Local emulators and containerized services provide a production-like environment without cloud dependencies, enabling rapid iteration and confident deployment.

## Quality Metrics
The following metrics MUST be tracked and maintained:

### Code Quality (Per Service)
- Test Coverage: ≥80%
- Code Duplication: <5%
- Cyclomatic Complexity: ≤10
- Method Length: ≤30 lines
- Class Size: ≤200 lines
- Documentation Coverage: ≥90%
- Build Warnings: 0 in production code
- Public API Documentation: 100%

### Performance Metrics (Per Service)
- API Response Time: P95 ≤200ms
- Database Query Time: P95 ≤100ms
- Memory Usage: ≤512MB per instance
- CPU Usage: ≤50% sustained
- Error Rate: ≤0.1%
- Service Uptime: ≥99.9%

### Test Metrics (Per Service)
- Unit Test Pass Rate: 100%
- Integration Test Pass Rate: 100%
- End-to-End Test Pass Rate: 100%
- Performance Test Pass Rate: 100%
- Test Execution Time: ≤10 minutes
- Contract Test Pass Rate: 100% (for service interfaces)

### Service-Level Metrics
- Inter-service latency: P95 ≤50ms
- API Gateway latency: P95 ≤20ms
- Service deployment frequency: ≥1 per week (per service)
- Mean Time to Recovery (MTTR): ≤30 minutes
- Change failure rate: ≤5%

### Local Development Metrics
- Local environment setup time: ≤30 minutes (new developer, including tools)
- Local test suite execution time: ≤5 minutes (full suite)
- Local service startup time: ≤60 seconds (all services)
- Local debugging success rate: 100% (breakpoints, logs, state inspection work)
- Local-to-Azure parity: 100% (features work identically in both environments)

## Development Process
1. **Verify local environment setup** (Local-First Development):
   - Ensure local emulators are running (Azurite, SQL Server, etc.)
   - Verify local.settings.json configuration for affected service(s)
   - Confirm local services start successfully
2. Identify affected service(s) in feature specification
3. Create feature branch from main
4. **Design library interface first** (Library-First Development):
   - Define library interface and contracts in `libs/`
   - Document expected behavior and API surface
   - Create contract tests for library interface
5. **Follow strict TDD workflow** (Red-Green-Refactor):
   - Write unit test for next functionality
   - **Run test and verify it FAILS (RED phase)**
   - Write minimal implementation code
   - **Run test and verify it PASSES (GREEN phase)**
   - Refactor while keeping tests green
   - Repeat for each unit of functionality
6. Implement library with realistic test environments:
   - Use real databases in integration tests (in-memory or containerized)
   - Use actual service instances for inter-service tests
   - Mock only external third-party services
7. Verify library works in isolation before service integration
8. Integrate library into service(s)
9. **Run and test locally (MANDATORY before Azure deployment)**:
   - Start local Azure Functions with `func start`
   - Run full local test suite (unit + integration + E2E)
   - Manually verify user flows using local endpoints (http://localhost:7071)
   - Test service-to-service interactions locally
   - Debug issues with full access to logs and breakpoints
   - **DO NOT proceed to Azure deployment until all local tests pass**
10. Write service integration tests following TDD workflow
11. Update API contracts in `libs/contracts/` if service interfaces change
12. Verify quality metrics compliance for affected services
13. Address all build warnings before committing:
    - Fix documentation warnings in production code
    - Add XML documentation to all public APIs
    - Configure NoWarn for acceptable test project warnings
    - Document any intentional TODOs with context
14. Run service-specific test suite (locally)
15. Run cross-service integration tests if multiple services affected (locally)
16. Update Architecture Decision Records (ADRs) if architectural changes made
17. Conduct code review
18. **Deploy to Azure only after local verification**:
    - All local tests passing
    - Manual local testing complete
    - Zero warnings in production code
    - All quality metrics met
19. **Azure deployment validation** (environment-specific testing):
    - Verify Azure-specific configuration (App Settings, connection strings)
    - Test Azure-specific integrations (Application Insights, Key Vault)
    - Confirm service health checks pass
    - Validate cross-service communication in Azure environment
20. Merge only if all checks pass (local AND Azure validation complete)

**For multi-service features**:
- Changes MUST be backward compatible OR coordinated deployment plan MUST be documented
- API versioning MUST be used for breaking changes
- Feature flags SHOULD be used for gradual rollout

**Local-First Verification Checklist**:
- [ ] Local emulators configured and running
- [ ] Local.settings.json populated with correct local connection strings
- [ ] Service starts locally with `func start` (no errors)
- [ ] All unit tests pass locally
- [ ] All integration tests pass locally (with local services)
- [ ] Manual testing complete via local endpoints
- [ ] Cross-service communication verified locally (if applicable)
- [ ] No cloud resources required for development or testing
- [ ] Only after all above: Deploy to Azure for environment validation

## Architecture Decision Records (ADR)

All significant architectural decisions MUST be documented as ADRs in `docs/adr/`:
- Use format: `NNNN-short-title.md` (e.g., `0001-use-microservices.md`)
- Include: Context, Decision, Consequences, Alternatives Considered
- ADRs are immutable once approved (new ADR to supersede)
- Service-specific ADRs go in `services/<name>/docs/adr/`
- Cross-cutting ADRs go in `docs/adr/`

## Governance
This constitution supersedes all other development practices and guidelines. Amendments require:
1. Documentation of proposed changes
2. Impact analysis on existing codebase
3. Approval from technical leadership
4. Clear migration plan for affected components
5. Version number increment following semver

All pull requests MUST verify compliance with these principles. Exceptions require explicit approval and documentation.

**Microservices-specific governance**:
- Service ownership MUST be clearly assigned
- Inter-service API changes MUST be reviewed by affected service owners
- Breaking changes require migration plan and version bump
- New services require ADR documenting justification and boundaries

**Local-First Development governance**:
- Features MUST be fully testable locally before Azure deployment
- Local development setup MUST be documented and maintained
- Azure deployment is for environment validation, not primary development/testing
- Violations of local-first development require explicit approval with documented justification

**Version**: 2.3.0 | **Ratified**: 2025-10-12 | **Last Amended**: 2025-10-25
