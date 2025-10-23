# Coates Village Club Server Constitution

<!--
Version Change: 2.2.0 → 2.2.1 (Patch Update)

PATCH changes:
- Clarified testing scope to exclude third-party libraries and middleware
- Added explicit guidance on what requires testing vs. what can be assumed functional

Modified sections:
- Test-First Development (Principle I): Added Testing Scope Exclusions section
- Comprehensive Testing (Principle IV): Added Third-Party Components section clarifying scope

Rationale:
Recent development revealed confusion about testing scope. Third-party libraries (Serilog, 
FluentValidation, Entity Framework, Azure Functions SDK) are professionally maintained with 
their own test suites. Our tests should focus on our business logic and integration points, 
not on verifying that third-party libraries work as documented. This clarification reduces 
unnecessary test complexity while maintaining quality standards for our code.

Examples of exclusions:
- Logging middleware (Serilog) - assume it logs correctly
- Validation frameworks (FluentValidation) - assume validation rules execute
- ORM functionality (Entity Framework) - assume queries execute correctly
- Framework middleware (Azure Functions HTTP pipeline) - assume request/response handling works
- Authentication libraries - assume token validation works per documentation

We DO test:
- Our business logic that uses these libraries
- Our configuration of these libraries
- Our integration points with these libraries
- Our custom middleware and extensions

Templates requiring updates:
✅ Updated: Constitution file
⚠️  Templates already align with this clarification (no changes needed)

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

## Development Process
1. Identify affected service(s) in feature specification
2. Create feature branch from main
3. **Design library interface first** (Library-First Development):
   - Define library interface and contracts in `libs/`
   - Document expected behavior and API surface
   - Create contract tests for library interface
4. **Follow strict TDD workflow** (Red-Green-Refactor):
   - Write unit test for next functionality
   - **Run test and verify it FAILS (RED phase)**
   - Write minimal implementation code
   - **Run test and verify it PASSES (GREEN phase)**
   - Refactor while keeping tests green
   - Repeat for each unit of functionality
5. Implement library with realistic test environments:
   - Use real databases in integration tests (in-memory or containerized)
   - Use actual service instances for inter-service tests
   - Mock only external third-party services
6. Verify library works in isolation before service integration
7. Integrate library into service(s)
8. Write service integration tests following TDD workflow
9. Update API contracts in `libs/contracts/` if service interfaces change
10. Verify quality metrics compliance for affected services
11. Address all build warnings before committing:
    - Fix documentation warnings in production code
    - Add XML documentation to all public APIs
    - Configure NoWarn for acceptable test project warnings
    - Document any intentional TODOs with context
12. Run service-specific test suite
13. Run cross-service integration tests if multiple services affected
14. Update Architecture Decision Records (ADRs) if architectural changes made
15. Conduct code review
16. Merge only if all checks pass (zero warnings in production code, all tests GREEN)

**For multi-service features**:
- Changes MUST be backward compatible OR coordinated deployment plan MUST be documented
- API versioning MUST be used for breaking changes
- Feature flags SHOULD be used for gradual rollout

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

**Version**: 2.2.1 | **Ratified**: 2025-10-12 | **Last Amended**: 2025-10-19
