# Research: Village Club Management Microservices

**Feature**: 001-create-a-series  
**Date**: 2025-10-18  
**Status**: Complete

## Overview

This document consolidates research findings for implementing a microservices-based village club management system using .NET 8 Azure Functions with cost optimization for low-traffic scenarios.

---

## Decision 1: Azure Functions vs App Service vs Container Apps

**Decision**: Azure Functions Flex Consumption Plan for all services

**Rationale**:
- **Cost Profile**: Flex Consumption Plan charges per execution (~$0.20 per million executions + $0.000016/GB-s memory). With estimated 50-100 users and peak usage only Fri/Sat evenings, monthly cost projected at $5-15 vs $150+ for App Service Basic tier
- **Scale-to-Zero**: Automatic scale to zero during idle periods (Mon-Thu, daytime) eliminates charges when club is closed
- **Operational Simplicity**: Fully managed, no infrastructure management, automatic scaling
- **Cold Start Mitigation**: Pre-warming via timer trigger during operating hours (7:45pm Fri/Sat) keeps functions warm when needed

**Alternatives Considered**:
1. **Azure App Service (Basic B1)**: 
   - Cost: ~$55/month per service × 5 services = $275/month minimum
   - Rejected: Always-on costs not justified for 8 hours/week peak usage
   
2. **Azure Container Apps (Consumption)**:
   - Cost: Similar to Functions but requires more DevOps complexity
   - Rejected: No significant benefit over Functions for HTTP APIs, steeper learning curve

3. **Azure Functions Premium Plan**:
   - Cost: ~$200/month for EP1 tier
   - Rejected: Pre-warmed instances and VNET integration not needed for village club scale

**Implementation Notes**:
- Use .NET 8 isolated worker model (better testability, performance)
- Implement proper health checks for service discovery
- Use durable entities if complex state management needed later (currently not required)

---

## Decision 2: Database Strategy - Shared vs Separate

**Decision**: Single Azure SQL Database (Serverless tier) with schema-based isolation

**Rationale**:
- **Cost Optimization**: 
  - Serverless tier: ~$15/month (0.5 vCores min, auto-pause after 1 hour idle)
  - 5 separate databases: ~$75/month minimum (5 × $15) with no auto-pause benefit
  - Schema isolation maintains logical service boundaries
- **Simplified Management**: Single connection pool, unified backup/restore, centralized monitoring
- **Performance Adequate**: Estimated 100-200 queries/day total across all services, well within serverless limits
- **Migration Path**: Can split to separate databases if traffic grows or budget allows

**Schema Isolation Pattern**:
```sql
-- Each service owns its schema
CREATE SCHEMA Membership;
CREATE SCHEMA Events;
CREATE SCHEMA Scheduling;
CREATE SCHEMA Bar;
CREATE SCHEMA Finance;

-- Services only access their own schema via EF Core configuration
-- Cross-service data access ONLY via REST APIs, never direct SQL
```

**Alternatives Considered**:
1. **Separate Databases per Service**:
   - Cost: ~$75-300/month depending on tier
   - Rejected: Cost prohibitive for stated requirements, over-engineering for scale
   
2. **Azure Cosmos DB**:
   - Cost: ~$25/month minimum for serverless
   - Rejected: NoSQL not needed, relational model fits domain better, team likely more familiar with SQL
   
3. **SQLite with Azure Files**:
   - Cost: ~$5/month storage
   - Rejected: No built-in HA, difficult concurrent access from multiple function instances, limited query capabilities

**Implementation Notes**:
- Configure EF Core with `HasDefaultSchema()` per DbContext
- Use database migrations per service (separate migration history tables)
- Implement row-level security if services accidentally cross boundaries
- Monitor with Azure SQL Analytics for query performance

---

## Decision 3: JWT Authentication Strategy

**Decision**: Membership service issues JWT tokens, API Gateway validates via Azure APIM policies

**Rationale**:
- **Centralized Token Issuing**: Membership service owns user identity, issues tokens on login
- **Gateway-Level Validation**: APIM validates JWT signature and expiry before routing to services
- **Stateless Services**: Downstream services trust pre-validated tokens, extract claims for authorization
- **Standard Pattern**: Follows OAuth2/OIDC principles, compatible with future external IdP integration (e.g., Azure AD B2C)

**Token Structure**:
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "role": "Committee",
  "committee_role": "Treasurer",
  "exp": 1234567890,
  "iss": "VillageClub.Membership",
  "aud": "VillageClub.API"
}
```

**Alternatives Considered**:
1. **Azure AD B2C**:
   - Cost: Free tier for <50k MAU
   - Rejected: Over-engineered for MVP, adds external dependency, team likely wants direct control initially
   
2. **API Keys per User**:
   - Cost: $0
   - Rejected: No fine-grained permissions, difficult rotation, not suitable for UI app authentication
   
3. **Session Cookies**:
   - Cost: $0
   - Rejected: Stateful, requires session storage (Redis), not suitable for serverless scale-to-zero

**Implementation Notes**:
- Use `System.IdentityModel.Tokens.Jwt` for token generation
- APIM policy: `<validate-jwt>` with public key from Membership service health endpoint
- Token expiry: 24 hours (configurable)
- Refresh token pattern for long-lived sessions (store in database with revocation)

---

## Decision 4: Receipt Image Storage

**Decision**: Azure Blob Storage (Hot tier, lifecycle policy to Cool after 90 days)

**Rationale**:
- **Cost**: Hot tier $0.018/GB/month, Cool tier $0.01/GB/month
- **Estimated Usage**: ~20 receipts/month × 500KB avg = 10MB/month = $0.0002/month (negligible)
- **Lifecycle Management**: Automatic transition to Cool tier for receipts >90 days old reduces long-term costs
- **Integration**: Native Azure SDK, SAS token generation for secure temporary access
- **Compliance**: Immutable storage available if audit requirements demand it

**Storage Pattern**:
```
container: expense-receipts
blob path: {year}/{month}/{expense-id}/{filename}
example: 2025/10/abc-123-def/receipt.jpg

SAS token for viewing: 1-hour read-only access
```

**Alternatives Considered**:
1. **Store in SQL Database as VARBINARY**:
   - Cost: Database storage costs ~$0.12/GB vs $0.018/GB for Blob
   - Rejected: Inflates database size, slower queries, not designed for binary large objects
   
2. **Azure Files**:
   - Cost: ~$0.06/GB/month
   - Rejected: More expensive than Blob, SMB/NFS features not needed for object storage
   
3. **Third-party (Cloudinary, ImgBB)**:
   - Cost: Variable, $0-25/month for hobby tiers
   - Rejected: External dependency, data sovereignty concerns for financial records

**Implementation Notes**:
- Validate file type via magic bytes (not just extension)
- Generate unique blob names to prevent collisions
- Implement soft-delete (30-day retention) for accidental deletions
- Use managed identity for Functions → Blob Storage authentication

---

## Decision 5: Inter-Service Communication

**Decision**: Synchronous REST over HTTP with Polly circuit breaker

**Rationale**:
- **Simplicity**: REST APIs are well-understood, easy to test, built-in HTTP client support
- **Latency Acceptable**: <50ms inter-service latency target, sufficient for user-facing workflows
- **Circuit Breaker**: Polly library provides resilience (retry, timeout, circuit breaker) without additional infrastructure
- **Service Discovery**: APIM provides unified endpoint, services call via internal routing

**Communication Patterns**:
```
Example: Submitting Expense (requires Event validation)

1. UI → APIM → Finance Service: POST /api/v1/expenses
2. Finance Service → APIM → Events Service: GET /api/v1/events/{id}
3. Events Service → Finance Service: Event details
4. Finance Service validates event exists, saves expense
5. Finance Service → UI: Expense created

Circuit breaker: If Events Service down, Finance returns 503 "Cannot validate event, try later"
```

**Alternatives Considered**:
1. **Azure Service Bus (async messaging)**:
   - Cost: ~$10/month basic tier
   - Rejected: Adds complexity, async not required for user-facing operations, increases latency for no benefit
   
2. **gRPC**:
   - Cost: $0
   - Rejected: Binary protocol complicates debugging, overkill for internal APIs with low traffic
   
3. **Azure Event Grid**:
   - Cost: ~$1/month for low volume
   - Rejected: Designed for event-driven patterns, current workflows are request-response

**Implementation Notes**:
- Use `IHttpClientFactory` with Polly policies
- Configure retries: 3 attempts with exponential backoff
- Circuit breaker: Open after 5 consecutive failures, half-open after 30 seconds
- Timeout: 5 seconds per request
- Correlate requests with `X-Correlation-ID` header for distributed tracing

---

## Decision 6: API Documentation and Discovery

**Decision**: Swashbuckle (Swagger/OpenAPI) per service + APIM Developer Portal

**Rationale**:
- **Standard Format**: OpenAPI 3.0 is industry standard, consumable by UI developers
- **Auto-Generation**: Swashbuckle generates from C# attributes and XML comments
- **Developer Portal**: APIM provides hosted portal with try-it functionality
- **Service Registry**: Health check endpoints return service metadata

**Documentation Strategy**:
```
Each service exposes:
- GET /api/health → { "status": "healthy", "version": "1.0.0", "timestamp": "..." }
- GET /api/swagger.json → OpenAPI spec
- GET /api/docs → Swagger UI (dev environment only)

APIM aggregates into unified API catalog
```

**Alternatives Considered**:
1. **NSwag**:
   - Cost: $0
   - Rejected: Swashbuckle more commonly used in .NET community, better docs
   
2. **Manual API documentation (Markdown)**:
   - Cost: $0
   - Rejected: Out-of-sync risk, no try-it functionality, requires manual maintenance
   
3. **Postman Collections**:
   - Cost: Free for small teams
   - Rejected: Not machine-readable by UI apps, requires separate maintenance

**Implementation Notes**:
- Use XML comments for detailed endpoint descriptions
- Add example responses via `[ProducesResponseType]` attributes
- Version APIs in URL: `/api/v1/users`, `/api/v2/users`
- APIM portal visible to UI developers without requiring Azure access

---

## Decision 7: Monitoring and Observability

**Decision**: Azure Application Insights with structured logging via Serilog

**Rationale**:
- **Integrated**: Functions SDK has built-in App Insights support
- **Structured Logs**: Serilog formats logs as JSON with correlation IDs
- **Queries**: Kusto Query Language (KQL) for analyzing logs and metrics
- **Alerting**: Built-in alerts for error rate, response time, availability
- **Cost**: ~$2-5/month for estimated 5GB ingestion (first 5GB free)

**Key Metrics to Track**:
```
- Request rate (per service, per endpoint)
- Response time (p50, p95, p99)
- Error rate and exception types
- Authentication failures
- Database query duration
- Blob storage operations
- Cold start frequency and duration
```

**Alternatives Considered**:
1. **Azure Monitor Logs only**:
   - Cost: Similar to App Insights
   - Rejected: Less feature-rich, no distributed tracing, requires more manual setup
   
2. **Third-party (Datadog, New Relic)**:
   - Cost: ~$15-30/month minimum
   - Rejected: Unnecessary cost, Azure-native solution sufficient
   
3. **Self-hosted (Prometheus + Grafana)**:
   - Cost: $0 for software, but compute costs for hosting
   - Rejected: Operational overhead not justified, team likely prefers managed solution

**Implementation Notes**:
- Configure sampling for high-traffic endpoints (if needed)
- Use `ILogger<T>` for dependency injection
- Include `X-Correlation-ID` in all logs for request tracing
- Set up alerts for:
  - Error rate >5% over 5 minutes
  - P95 response time >3 seconds
  - Failed authentication attempts >10/minute
  - Database connection failures

---

## Decision 8: Testing Strategy

**Decision**: Multi-layered testing with xUnit, Testcontainers, and in-process Functions host

**Rationale**:
- **Unit Tests (xUnit)**: Fast, isolated, test business logic without external dependencies
- **Integration Tests (Testcontainers)**: Real SQL Server in Docker, validates EF Core queries
- **Contract Tests**: Verify service API contracts haven't broken downstream consumers
- **E2E Tests**: In-process Azure Functions host, validates full request flow

**Test Structure**:
```
Unit Tests (80% coverage target):
- Service layer: pure business logic
- Validators: FluentValidation rules
- Utilities: extension methods, helpers

Integration Tests:
- Database operations: EF Core against real SQL Server
- Blob storage: against Azurite (local emulator)

Contract Tests:
- Service A calls Service B: verify response schema matches contract

E2E Tests:
- Full user scenarios: create user → create event → assign shift → submit expense
```

**Alternatives Considered**:
1. **Mocking everything (e.g., Moq for DB)**:
   - Cost: $0
   - Rejected: Mocks test mock behavior, not real database behavior (e.g., missing FK constraints)
   
2. **Manual testing only**:
   - Cost: $0
   - Rejected: Not scalable, violates constitution principle I (Test-First Development)
   
3. **Cloud-based test environments**:
   - Cost: ~$20-50/month for dedicated test resources
   - Rejected: Testcontainers provides ephemeral environments for free

**Implementation Notes**:
- Use `WebApplicationFactory<T>` for hosting Functions in tests
- Testcontainers spins up SQL Server 2022 container per test class
- Use test builders for readable test data creation
- Run tests in CI/CD pipeline before deployment
- Use `[Trait]` attributes to categorize tests (unit, integration, e2e)

---

## Decision 9: Cost Optimization Strategies

**Decision**: Multi-pronged approach targeting <$50/month total Azure costs

**Strategies**:

### 1. Azure SQL Serverless with Auto-Pause
- **Savings**: ~$10/month vs standard tier
- **Config**: Min 0.5 vCores, max 4 vCores, auto-pause after 1 hour idle
- **Tradeoff**: 5-10 second resume time on first query after pause (acceptable for club hours)

### 2. Blob Storage Lifecycle Policy
- **Savings**: ~50% on long-term storage
- **Config**: Move receipts to Cool tier after 90 days, Archive tier after 2 years
- **Tradeoff**: Higher access costs for old receipts (rarely accessed)

### 3. Application Insights Sampling
- **Savings**: ~50% on ingestion costs if traffic grows
- **Config**: Adaptive sampling at 5GB/month threshold
- **Tradeoff**: Some low-priority telemetry may be sampled out

### 4. APIM Consumption Tier
- **Savings**: ~$45/month vs Developer tier ($45 vs $0.0035/call)
- **Config**: Pay-per-call with no minimum commitment
- **Tradeoff**: No SLA, slower request processing (acceptable for club use)

### 5. Pre-warming Only During Operating Hours
- **Savings**: ~90% of cold start prevention costs
- **Config**: Timer trigger fires 7:45pm Fri/Sat only (not 24/7)
- **Tradeoff**: Cold starts during non-operating hours (acceptable)

### 6. Shared Database Instead of 5 Separate DBs
- **Savings**: ~$60/month (covered in Decision 2)

**Projected Monthly Costs**:
```
Azure SQL Serverless:     $15
Blob Storage:             $1
Application Insights:     $5
Functions Consumption:    $10
APIM Consumption:         $5
Azure DNS (optional):     $1
---------------------------------
Total:                    $37/month

Peak usage (100 users):   $50-60/month
```

---

## Best Practices Research

### Azure Functions Best Practices

1. **Use Dependency Injection**: Register services in `Program.cs`, avoid static dependencies
2. **Async/Await Everywhere**: Functions runtime optimized for async, blocks thread pool otherwise
3. **Keep Functions Thin**: Functions should delegate to services, not contain business logic
4. **Use Structured Output Bindings**: Return `IActionResult` with proper HTTP status codes
5. **Health Checks**: Implement `/api/health` endpoint for monitoring and service discovery
6. **Graceful Degradation**: Return partial results if downstream service fails (circuit breaker pattern)

### Entity Framework Core Best Practices

1. **Explicit Loading**: Avoid lazy loading (not supported in .NET isolated), use `.Include()` or explicit loading
2. **No-Tracking Queries**: Use `.AsNoTracking()` for read-only operations (performance boost)
3. **Index Foreign Keys**: EF Core doesn't auto-index FKs, add explicitly in migration
4. **Compiled Queries**: Use `EF.CompileQuery()` for frequently-run queries
5. **Connection Resiliency**: Configure retry policy for transient SQL errors
6. **Migrations in CI/CD**: Apply migrations via script in deployment pipeline, not app startup

### Security Best Practices

1. **Principle of Least Privilege**: Functions use managed identity with minimal RBAC roles
2. **Validate All Input**: FluentValidation for request models, sanitize user input
3. **Rate Limiting**: APIM policies limit requests to 100/minute per user
4. **Content Security**: Validate file uploads by magic bytes, not extension
5. **Secrets Management**: Store connection strings in Key Vault, reference via managed identity
6. **HTTPS Only**: Enforce TLS 1.2+ for all endpoints

### Performance Best Practices

1. **Connection Pooling**: Reuse `HttpClient` and `DbContext` instances via DI
2. **Caching**: Use in-memory cache for reference data (roles, event types) with 5-minute TTL
3. **Pagination**: Return max 50 items per page, use offset/limit pattern
4. **Compression**: Enable gzip response compression in Functions host
5. **Database Indexes**: Index on frequently queried fields (user email, event date, expense status)
6. **Avoid N+1 Queries**: Use `.Include()` for related entities in single query

---

## Technology Decision Summary

| Component | Technology | Justification |
|-----------|-----------|---------------|
| Compute | Azure Functions (Consumption) | Scale-to-zero, pay-per-execution, cost-optimized |
| Database | Azure SQL Serverless (shared) | Cost optimization, schema isolation, relational model fits domain |
| Storage | Azure Blob Storage (Hot/Cool) | Cost-effective binary storage, lifecycle management |
| API Gateway | Azure APIM (Consumption) | JWT validation, service routing, developer portal |
| Authentication | JWT (Membership service issues) | Stateless, standard pattern, easy UI integration |
| Inter-Service | REST + Polly circuit breaker | Simple, well-understood, resilient |
| Documentation | Swashbuckle/OpenAPI | Auto-generated, standard format, APIM integration |
| Monitoring | Application Insights + Serilog | Integrated, structured logging, cost-effective |
| Testing | xUnit + Testcontainers | Fast, isolated, validates real behavior |
| IaC | Bicep | Azure-native, type-safe, modular |

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Cold start latency >5s | Users experience slow first request | Pre-warming timer during operating hours, Premium plan if issue persists |
| Database auto-pause delay | 5-10s resume time on first query | Configure 1-hour idle (reduces pauses), acceptable for club usage pattern |
| Single database bottleneck | Schema lock conflicts if migrations overlap | Coordinate deployments, use separate migration history per service |
| Storage costs exceed estimate | Receipt uploads 10x higher than expected | Monitor monthly, implement upload quotas if needed |
| APIM Consumption tier instability | No SLA, potential availability issues | Monitor uptime, upgrade to Basic tier ($$) if SLA needed |
| Serverless SQL throttling | DTU limits hit during peak usage | Monitor metrics, scale to higher vCore tier if needed |

---

## Next Steps (Phase 1)

1. ✅ Research complete
2. ⏭️ Generate `data-model.md` with entity definitions
3. ⏭️ Generate OpenAPI contracts in `contracts/openapi/`
4. ⏭️ Generate `quickstart.md` with setup instructions
5. ⏭️ Update agent context (Copilot instructions)
6. ⏭️ Re-evaluate Constitution Check

---

**Research Completed**: 2025-10-18  
**Next Phase**: Phase 1 - Design & Contracts
