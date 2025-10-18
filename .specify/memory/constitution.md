# Coates Village Club Server Constitution

<!--
Version Change: 1.1.0 → 2.0.0 (Major Architecture Update)

MAJOR changes (backward incompatible):
- Added Microservices Architecture principle (new governance requirement)
- Modified Maintainable Architecture to include microservices constraints
- Added Monorepo Structure section (new structural requirement)

MINOR changes:
- Added Service Boundaries principle
- Expanded Quality Metrics to include service-level metrics
- Added Architecture Decision Records requirement

Modified principles:
- Principle V: Maintainable Architecture (expanded to include microservices)

Added principles:
- Service Boundaries
- Microservices Architecture

Added sections:
- Monorepo Structure
- Service Catalog
- Architecture Decision Records (ADR)

Removed sections: None

Templates requiring updates:
✅ .specify/templates/plan-template.md (updated project structure options)
✅ .specify/templates/spec-template.md (verified compatibility)
✅ .specify/templates/tasks-template.md (verified service-aware task organization)

Follow-up TODOs:
- Establish ADR process and create initial ADRs for each microservice
- Define inter-service communication contracts
- Document API Gateway patterns
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
All code MUST be developed following Test-Driven Development (TDD) principles:
- Tests MUST be written before implementation code
- Tests MUST fail initially to verify test validity
- Implementation MUST be written to make tests pass
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

Rationale: TDD ensures code reliability, maintains quality standards, and provides living documentation of intended behavior. Proper test structure ensures tests validate actual implementation behavior rather than mocked responses.

### II. Code Quality Standards
Code MUST adhere to established quality metrics and practices:
- Follow consistent coding style and naming conventions
- Maximum cyclomatic complexity of 10 per function
- Methods MUST not exceed 30 lines of code
- Classes MUST have single responsibility
- Code duplication MUST be less than 5%
- All code MUST pass linting and static analysis
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
Multiple testing levels MUST be implemented and maintained:
- Unit tests for individual components
- Integration tests for component interactions
- End-to-end tests for critical user flows
- Load tests for performance validation
- Security tests for vulnerability detection
- Tests MUST be automated and repeatable

Rationale: Comprehensive testing ensures reliability and catches issues early.

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

## Quality Metrics
The following metrics MUST be tracked and maintained:

### Code Quality (Per Service)
- Test Coverage: ≥80%
- Code Duplication: <5%
- Cyclomatic Complexity: ≤10
- Method Length: ≤30 lines
- Class Size: ≤200 lines
- Documentation Coverage: ≥90%

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
3. Write tests following TDD principles (per-service and integration)
4. Implement feature to pass tests
5. Update API contracts in `libs/contracts/` if interfaces change
6. Verify quality metrics compliance for affected services
7. Run service-specific test suite
8. Run cross-service integration tests if multiple services affected
9. Update Architecture Decision Records (ADRs) if architectural changes made
10. Conduct code review
11. Merge only if all checks pass

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

**Version**: 2.0.0 | **Ratified**: 2025-10-12 | **Last Amended**: 2025-10-18