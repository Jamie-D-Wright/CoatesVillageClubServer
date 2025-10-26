---
description: "Task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: The examples below include test tasks. Tests are OPTIONAL - only include them if explicitly requested in the feature specification.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions
- **Microservices**: `services/[service-name]/src/`, `services/[service-name]/tests/`
- **Shared libraries**: `libs/[lib-name]/`
- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure
- For microservices features, group tasks by affected service

<!-- 
  ============================================================================
  IMPORTANT: The tasks below are SAMPLE TASKS for illustration purposes only.
  
  The /speckit.tasks command MUST replace these with actual tasks based on:
  - User stories from spec.md (with their priorities P1, P2, P3...)
  - Feature requirements from plan.md
  - Entities from data-model.md
  - Endpoints from contracts/
  
  Tasks MUST be organized by user story so each story can be:
  - Implemented independently
  - Tested independently
  - Delivered as an MVP increment
  
  DO NOT keep these sample tasks in the generated tasks.md file.
  ============================================================================
-->

## Phase 1: Library Design (Library-First Development)

**Purpose**: Design and create library structure BEFORE implementation

**⚠️ CRITICAL**: ALL features MUST begin as libraries. No direct service implementation allowed.

- [ ] T001 Create library structure in `libs/[feature-name]/`
- [ ] T002 Define library interface and contracts
- [ ] T003 [P] Write contract tests for library interface (MUST FAIL initially)
- [ ] T004 [P] Document library API and usage in README.md
- [ ] T005 Setup library project with dependencies

**Checkpoint**: Library interface designed and contract tests written (RED phase)

---

## Phase 2: Library Implementation (TDD Red-Green-Refactor)

**Purpose**: Implement library following strict TDD workflow

**⚠️ CRITICAL**: For EACH unit of functionality:
1. Write test FIRST
2. Verify test FAILS (RED)
3. Implement minimal code
4. Verify test PASSES (GREEN)
5. Refactor

### Library Core Functionality

- [ ] T006 [P] Write unit test for [function1] (verify FAILS)
- [ ] T007 Implement [function1] to pass test (verify PASSES)
- [ ] T008 [P] Write unit test for [function2] (verify FAILS)
- [ ] T009 Implement [function2] to pass test (verify PASSES)
- [ ] T010 [P] Write integration test with real database (verify FAILS)
- [ ] T011 Implement database integration (verify PASSES)
- [ ] T012 Refactor library code while keeping tests GREEN

**Checkpoint**: Library complete, fully tested in isolation, contract tests PASS

---

## Phase 3: Service Integration

**Purpose**: Integrate library into service(s)

- [ ] T013 Add library reference to service project
- [ ] T014 [P] Write service integration test (verify FAILS)
- [ ] T015 Integrate library into service (verify PASSES)
- [ ] T016 Update service configuration and dependency injection

**Checkpoint**: Library integrated, service tests PASS

---

## Phase 4: Local Testing & Verification (Local-First Development)

**Purpose**: Verify ALL functionality locally BEFORE Azure deployment

**⚠️ CRITICAL**: Do NOT deploy to Azure until ALL local tests pass

### Local Environment Setup
- [ ] T017 Configure local.settings.json with local connection strings
- [ ] T018 Start Azurite emulator for blob/queue/table storage
- [ ] T019 Start local SQL Server (Docker or Express/LocalDB)
- [ ] T020 Run database migrations against local SQL Server
- [ ] T021 Verify local environment setup completes successfully

### Local Service Execution
- [ ] T022 Start Azure Functions locally with `func start`
- [ ] T023 Verify all functions mapped and routes configured correctly
- [ ] T024 Test health check endpoint locally (http://localhost:7071/api/v1/health)

### Local Testing
- [ ] T025 Run full unit test suite locally (all tests MUST PASS)
- [ ] T026 Run integration tests against local services (Azurite + local SQL)
- [ ] T027 Create Postman collection for API testing in `tests/postman/[service-name].postman_collection.json`
  - Include tests for all endpoints (health, authentication, CRUD operations)
  - Add test scripts with assertions (status codes, response structure, business logic)
  - Include validation tests (invalid inputs, edge cases)
  - Add pre-request scripts for dynamic data (unique emails, timestamps)
- [ ] T028 Create Postman environment file `tests/postman/local.postman_environment.json`
  - Configure baseUrl (e.g., http://localhost:7071/api/v1)
  - Define variables for tokens and IDs (auto-populated by tests)
- [ ] T029 Install newman CLI (`npm install -g newman`) for automated test execution
- [ ] T030 Run Postman collection with newman: `newman run [collection].json -e local.postman_environment.json`
- [ ] T031 Document test suite usage in `tests/postman/README.md` with:
  - How to run tests with newman
  - How to import collections into Postman Desktop
  - How to run specific folders or tests
  - How to generate HTML reports

### E2E Test Automation (MANDATORY - Constitution Principle XI)
- [ ] T032 Create `scripts/test-e2e.ps1` - Complete automated test workflow
  - Implement service startup as background job
  - Add health check verification
  - Execute newman test collection
  - Implement service shutdown and cleanup
  - Support -KeepServiceRunning and -SkipServiceStart flags
- [ ] T033 Create `scripts/start-[service]-service.ps1` - Start service as PowerShell background job
  - Build service before starting
  - Start func.exe in background job
  - Wait for service initialization
  - Verify service health endpoint
  - Output job ID for tracking
- [ ] T034 Create `scripts/stop-[service]-service.ps1` - Stop service and clean up
  - Stop PowerShell background job
  - Terminate func processes
  - Clean up resources
- [ ] T035 Create `scripts/run-e2e-tests.ps1` - Execute Newman test collection
  - Change to project root directory
  - Run newman with collection and environment
  - Report pass/fail status with exit codes
- [ ] T036 Create `scripts/view-service-logs.ps1` - Query service logs from background jobs
  - Support viewing all logs
  - Support tail mode (last N lines)
  - Support follow mode (real-time)
  - Support filtering by job ID
- [ ] T037 Create `scripts/debug-service.ps1` - Multi-purpose debugging tool
  - Implement status check (jobs, processes, ports)
  - Implement health endpoint test
  - Implement job listing
  - Implement process listing
- [ ] T038 Create `scripts/README.md` documenting:
  - Quick start guide (.\scripts\test-e2e.ps1)
  - Script descriptions and usage
  - Debugging workflows
  - Advanced options (keep-running, skip-start)
  - Troubleshooting guide
- [ ] T039 Test E2E automation workflow end-to-end
- [ ] T040 Verify all E2E scripts use UTF-8 encoding without BOM
- [ ] T041 Verify scripts handle errors gracefully with clear messages

### Manual Local Verification
- [ ] T042 Test error handling and edge cases locally
- [ ] T043 Test cross-service interactions locally (if applicable)
- [ ] T044 Debug any issues with breakpoints and local logs

**Checkpoint**: All local tests PASS (unit, integration, E2E automation via test-e2e.ps1), manual verification complete, NO Azure deployment yet

---

## Phase 5: Azure Deployment & Validation (After Local Success)

**Purpose**: Deploy to Azure ONLY for environment-specific validation

**Prerequisites**: ALL Phase 4 tasks MUST be complete and passing

- [ ] T031 Deploy service to Azure (func azure functionapp publish)
- [ ] T032 Verify Azure App Settings configuration
- [ ] T033 Test health check on Azure endpoint
- [ ] T034 Verify Azure-specific integrations (Application Insights, Key Vault)
- [ ] T035 Test cross-service communication in Azure environment
- [ ] T036 Monitor logs and metrics in Azure

**Checkpoint**: Service deployed and validated in Azure environment

---

## Phase 4: Setup (Additional Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T017 Create project structure per implementation plan
- [ ] T018 Initialize [language] project with [framework] dependencies
- [ ] T019 [P] Configure linting and formatting tools

---

## Phase 5: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

Examples of foundational tasks (adjust based on your project):

- [ ] T020 Setup database schema and migrations framework
- [ ] T021 [P] Implement authentication/authorization framework
- [ ] T022 [P] Setup API routing and middleware structure
- [ ] T023 Create base models/entities that all stories depend on
- [ ] T024 Configure error handling and logging infrastructure
- [ ] T025 Setup environment configuration management

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 6: User Story 1 - [Title] (Priority: P1) 🎯 MVP

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 1 (TDD - Write tests FIRST) ⚠️

**CRITICAL TDD Workflow for EACH task**:
1. Write test FIRST
2. Run test - verify it FAILS (RED)
3. Write implementation
4. Run test - verify it PASSES (GREEN)
5. Refactor while keeping GREEN

- [ ] T026 [P] [US1] Write contract test for [endpoint] (verify FAILS)
- [ ] T027 [P] [US1] Write integration test with real database for [user journey] (verify FAILS)
- [ ] T028 [P] [US1] Write unit test for [Entity1] (verify FAILS)
- [ ] T029 [P] [US1] Write unit test for [Entity2] (verify FAILS)

### Implementation for User Story 1

- [ ] T030 [P] [US1] Create [Entity1] model to pass test T028 (verify GREEN)
- [ ] T031 [P] [US1] Create [Entity2] model to pass test T029 (verify GREEN)
- [ ] T032 [US1] Implement [Service] to pass integration test (depends on T030, T031, verify GREEN)
- [ ] T033 [US1] Implement [endpoint/feature] to pass contract test (verify GREEN)
- [ ] T034 [US1] Add validation and error handling (update tests as needed)
- [ ] T035 [US1] Add logging for user story 1 operations
- [ ] T036 [US1] Refactor code while keeping all tests GREEN

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently, all tests GREEN

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 2 (OPTIONAL - only if tests requested) ⚠️

- [ ] T018 [P] [US2] Contract test for [endpoint] in tests/contract/test_[name].py
- [ ] T019 [P] [US2] Integration test for [user journey] in tests/integration/test_[name].py

### Implementation for User Story 2

- [ ] T020 [P] [US2] Create [Entity] model in src/models/[entity].py
- [ ] T021 [US2] Implement [Service] in src/services/[service].py
- [ ] T022 [US2] Implement [endpoint/feature] in src/[location]/[file].py
- [ ] T023 [US2] Integrate with User Story 1 components (if needed)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - [Title] (Priority: P3)

**Goal**: [Brief description of what this story delivers]

**Independent Test**: [How to verify this story works on its own]

### Tests for User Story 3 (OPTIONAL - only if tests requested) ⚠️

- [ ] T024 [P] [US3] Contract test for [endpoint] in tests/contract/test_[name].py
- [ ] T025 [P] [US3] Integration test for [user journey] in tests/integration/test_[name].py

### Implementation for User Story 3

- [ ] T026 [P] [US3] Create [Entity] model in src/models/[entity].py
- [ ] T027 [US3] Implement [Service] in src/services/[service].py
- [ ] T028 [US3] Implement [endpoint/feature] in src/[location]/[file].py

**Checkpoint**: All user stories should now be independently functional

---

[Add more user story phases as needed, following the same pattern]

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] TXXX [P] Documentation updates in docs/
- [ ] TXXX Code cleanup and refactoring
- [ ] TXXX Performance optimization across all stories
- [ ] TXXX [P] Additional unit tests (if requested) in tests/unit/
- [ ] TXXX Security hardening
- [ ] TXXX Run quickstart.md validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together (if tests requested):
Task: "Contract test for [endpoint] in tests/contract/test_[name].py"
Task: "Integration test for [user journey] in tests/integration/test_[name].py"

# Launch all models for User Story 1 together:
Task: "Create [Entity1] model in src/models/[entity1].py"
Task: "Create [Entity2] model in src/models/[entity2].py"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence


