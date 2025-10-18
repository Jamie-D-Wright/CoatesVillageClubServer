# ADR 0002: Shared Database with Schema Isolation

**Date**: 2025-10-18  
**Status**: Accepted with conditions  
**Feature**: 001-create-a-series  
**Deciders**: Technical Architecture Team

---

## Context

The Village Club microservices architecture consists of 5 domain services (Membership, Events, Scheduling, Bar, Finance). The Constitution (Principle VI: Service Boundaries) states:

> "Services MUST NOT share databases (each owns its data)"

However, the project has strict cost constraints requiring "low usage and cost efficiency" with minimal monthly operational costs.

## Decision

We will use a **single Azure SQL Database (Serverless tier)** with **strict schema-based isolation** per service, rather than 5 separate database instances.

Each service will own its schema:
- `Membership` schema → Membership service
- `Events` schema → Events service
- `Scheduling` schema → Scheduling service
- `Bar` schema → Bar service
- `Finance` schema → Finance service

## Rationale

### Cost Analysis

**Single Shared Database** (Serverless tier):
- Cost: ~$15/month
- Configuration: 0.5 vCores min, 4 vCores max, auto-pause after 1 hour idle
- Tradeoff: 5-10 second resume time after pause (acceptable for club usage pattern)

**Five Separate Databases** (Serverless tier each):
- Cost: ~$75-150/month (5 × $15-30 depending on configuration)
- No cost benefit from pausing (each service wakes its own database independently)
- Increased operational complexity (5 backup schedules, 5 connection pools)

**Cost Savings**: $60-135/month (80-90% reduction in database costs)

### Service Boundary Preservation

While violating the letter of "no shared databases," we preserve the **intent** through:

1. **Schema-level isolation**: Each service owns its schema, enforced by EF Core DbContext configuration:
   ```csharp
   // Membership service
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.HasDefaultSchema("Membership");
       // No access to other schemas
   }
   ```

2. **No cross-schema queries**: Services CANNOT query other schemas
   - No foreign keys across schemas
   - No joins across service boundaries
   - Cross-service data access ONLY via REST APIs

3. **Independent migrations**: Each service maintains its own EF Core migration history
   - Separate `__EFMigrationsHistory` table per schema
   - Services can deploy database changes independently

4. **Logical data ownership**: Each service owns its data, schema is the boundary

### Scale Characteristics

Estimated database load:
- **Total queries/day**: 100-200 across all services
- **Peak concurrent connections**: 5-10 (one per function instance)
- **Data volume**: <1GB total (100 users, 10 events/month, 20 expenses/month)

This is **well within Azure SQL Serverless capacity** (up to 500GB, thousands of queries/second).

## Alternatives Considered

### Alternative 1: Separate Databases per Service (Pure Microservices)

**Pros**:
- True service boundary enforcement at database level
- Complete deployment independence
- No risk of schema conflicts

**Cons**:
- **Cost**: $75-150/month (5× more expensive)
- Over-provisioning for actual load (most databases <99% idle)
- Increased operational overhead (5× backups, monitoring, connection pools)
- No benefit at this scale (services don't have conflicting queries)

**Rejected**: Cost prohibitive for stated "low usage and cost efficiency" requirement.

### Alternative 2: Azure Cosmos DB (NoSQL, Serverless)

**Pros**:
- Serverless pricing (pay per operation)
- Cost: ~$25/month for estimated usage
- Natural service isolation (separate containers)

**Cons**:
- NoSQL model doesn't fit relational domain well
- Team likely more familiar with SQL
- No strong consistency guarantees across partitions (complicates expense/event linking)
- Migration complexity if relational model needed later

**Rejected**: Relational model better fits domain (users, events, expenses with clear relationships).

### Alternative 3: SQLite with Azure Files

**Pros**:
- Very low cost (~$5/month for storage)
- Simple file-based database

**Cons**:
- No built-in high availability
- Difficult concurrent access from multiple function instances
- Limited query capabilities compared to SQL Server
- Not designed for cloud-native serverless

**Rejected**: Not suitable for multi-instance Functions, lacks HA.

## Consequences

### Positive
- ✅ **Operational costs reduced by 80%+** ($15/month vs $75-150/month)
- ✅ **Simplified management**: Single backup schedule, one connection pool, unified monitoring
- ✅ **Adequate performance**: Shared database easily handles projected load
- ✅ **Familiar technology**: SQL Server well-known, easier onboarding
- ✅ **Migration path exists**: Can split to separate DBs if usage increases

### Negative
- ⚠️ **Constitution violation**: Technically violates "no shared databases" principle
  - **Justification**: Pragmatic tradeoff for cost requirements
  - **Mitigation**: Schema isolation enforces logical boundaries
  
- ⚠️ **Single point of failure**: All services affected if database goes down
  - **Mitigation**: Azure SQL has 99.99% SLA, automatic backups, geo-replication available
  - **Acceptable**: Manual recovery within 15 minutes per requirements
  
- ⚠️ **Schema lock conflicts**: Migrations could conflict if deployed simultaneously
  - **Mitigation**: Coordinate deployments, use separate migration history per schema
  - **Low risk**: Development team is small, can schedule deployments

### Risks

**Risk**: Service accidentally queries another service's schema  
- **Mitigation**: EF Core DbContext scoped to single schema, no raw SQL allowed
- **Detection**: Code reviews, schema access auditing in SQL Server
- **Likelihood**: Low (requires intentional override of DbContext configuration)

**Risk**: Database becomes bottleneck as usage grows  
- **Mitigation**: 
  - Monitor database DTU/vCore usage (alert at 70% sustained)
  - Scale up to higher vCore tier if needed (no code changes)
  - Migration path to separate databases documented
- **Likelihood**: Low (projected load is <1% of serverless capacity)

**Risk**: Schema evolution conflicts between services  
- **Mitigation**: 
  - Each service owns its schema namespace (no collisions)
  - Separate migration history prevents conflicts
  - Infrastructure as Code (Bicep) creates schemas in deployment
- **Likelihood**: Very low (schemas are isolated by design)

## Conditions for Acceptance

This design is **conditionally accepted** with the following requirements:

1. ✅ **Schema isolation enforced**: EF Core DbContext MUST use `HasDefaultSchema()` per service
2. ✅ **No cross-schema queries**: Code reviews MUST reject any queries across schemas
3. ✅ **API-only inter-service data access**: Services MUST call other services via REST APIs for cross-service data
4. ✅ **Separate migration history**: Each service MUST maintain its own `__EFMigrationsHistory` table
5. ✅ **Monitoring**: Database query patterns MUST be monitored to detect accidental boundary violations
6. ✅ **Migration path documented**: Plan to separate databases if usage/budget increases MUST be maintained

**Violation of any condition requires immediate remediation or upgrade to separate databases.**

## Migration Path to Separate Databases

If usage increases or budget allows, migration is straightforward:

1. **Provision 5 new Azure SQL Databases** (one per service)
2. **Export schema and data** from each schema to its own database
3. **Update connection strings** in each service (configuration change only)
4. **No code changes required** (EF Core migrations work identically)
5. **Gradual migration**: Move one service at a time (low risk)

**Estimated migration time**: 4-8 hours per service (mostly testing)

## Monitoring & Review

**Key Metrics**:
- Database CPU/vCore usage (target <50% sustained)
- Query duration (target <100ms for 95th percentile)
- Connection pool exhaustion (target 0 occurrences)
- Cross-schema query attempts (target 0, audit log)

**Review triggers**:
- Database CPU exceeds 70% sustained for 1 week
- Query performance degrades (P95 >200ms)
- More than 3 deployment conflicts due to schema changes
- Organizational budget increases (allows for separate DBs)

**Next review date**: 2026-01-18 (3 months after initial deployment)

---

## Related Decisions
- ADR 0001: Use Azure Functions Consumption Plan
- ADR 0005: Entity Framework Core as ORM

---

**Approved By**: [Technical Lead Name]  
**Date**: 2025-10-18  
**Conditions**: Schema isolation enforced, API-only cross-service data access, monitoring in place
