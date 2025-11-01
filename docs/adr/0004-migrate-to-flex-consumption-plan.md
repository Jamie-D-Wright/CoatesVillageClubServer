# ADR 0004: Migrate from Linux Consumption to Flex Consumption Plan

**Date**: 2025-11-01  
**Status**: Accepted  
**Supersedes**: ADR 0001  
**Feature**: Infrastructure Modernization  
**Deciders**: Technical Architecture Team

---

## Context

The Village Club management system was initially deployed using Azure Functions on the **Linux Consumption Plan (Y1 SKU)**. Since that decision, Microsoft has announced:

1. **Linux Consumption Plan retirement**: The Y1 SKU plan is being deprecated in favor of the new Flex Consumption Plan
2. **Flex Consumption Plan availability**: A new hosting option that provides improved performance, better scaling capabilities, and enhanced features while maintaining pay-per-use pricing
3. **Migration requirement**: All existing Linux Consumption Plan deployments must migrate to Flex Consumption Plan before the retirement deadline

Our current architecture has:
- 5 planned microservices (Membership, Events, Scheduling, Bar, Notifications) plus API Gateway
- Low usage pattern: Peak usage Friday/Saturday evenings (8pm-12am), ~4-6 concurrent users
- Cost efficiency requirement: <$50/month total operational costs
- .NET 8 isolated worker model

## Decision

We will **migrate all microservices from Linux Consumption Plan (Y1) to Flex Consumption Plan (FC1)**.

All future microservices MUST use Flex Consumption Plan as the standard hosting option.

## Rationale

### Migration Drivers
- **Platform retirement**: Linux Consumption Plan is being deprecated by Microsoft
- **No-choice requirement**: Migration is mandatory to avoid service disruption
- **Compatibility**: Our existing codebase (.NET 8 isolated) is fully compatible with Flex Consumption
- **Timing**: Early migration allows controlled transition before forced retirement

### Flex Consumption Plan Benefits

**Performance Improvements**:
- **Faster cold starts**: Improved cold start performance compared to legacy consumption plan
- **Better scaling**: More granular scaling with configurable instance memory (2048 MB default)
- **Concurrency control**: Configurable maximum instance count (default 100, up to 1000)
- **HTTP concurrency**: Better support for concurrent HTTP requests per instance

**Deployment Enhancements**:
- **Managed identity deployment**: Uses Azure managed identity for deployment package access (more secure)
- **Blob-based deployment**: Deployment packages stored in blob storage with managed identity authentication
- **Deployment slots**: Support for staging slots (not available in Y1)

**Cost Model** (unchanged):
- Pay-per-execution model maintained
- Scale-to-zero when idle
- Free grant of 1 million executions + 400,000 GB-s per month
- **Estimated cost**: $5-10/month for our projected usage (same as Y1)

**Operational Benefits**:
- **Virtual network integration**: Support for VNET integration (future-proofing)
- **Always ready instances**: Option to configure always-ready instances if cold starts become an issue
- **Better monitoring**: Enhanced metrics and diagnostics integration

### Cost Analysis Comparison

| Plan Type | Monthly Cost (Projected) | Cold Start | Scaling | VNET Support | Deployment |
|-----------|-------------------------|------------|---------|--------------|------------|
| **Linux Consumption (Y1)** | $5-10 | 3-5s | Auto (max 200) | ❌ | Connection string |
| **Flex Consumption (FC1)** | $5-10 | 2-4s | Auto (max 1000) | ✅ | Managed identity |
| **Premium EP1** | $200+ | None | Manual | ✅ | Managed identity |
| **App Service B1** | $55+ | None | Manual | ✅ | Multiple options |

**Verdict**: Flex Consumption maintains cost efficiency while providing improved capabilities.

## Technical Implementation

### Infrastructure Changes Required

**App Service Plan** (main.bicep):
```bicep
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true  // Required for Linux
  }
}
```

**Function App Configuration** (modules/function-app.bicep):
```bicep
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    reserved: true
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.properties.primaryEndpoints.blob}deploymentpackage'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 100
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '8.0'
      }
    }
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccount.name
        }
        // ... other app settings
      ]
    }
  }
}
```

**Key Differences from Y1**:
1. `sku.name` changes from `Y1` to `FC1`
2. `sku.tier` changes from `Dynamic` to `FlexConsumption`
3. New `functionAppConfig` section with deployment and scaling configuration
4. `AzureWebJobsStorage` becomes `AzureWebJobsStorage__accountName` (identity-based)
5. Removal of `WEBSITE_CONTENTAZUREFILECONNECTIONSTRING` and `WEBSITE_CONTENTSHARE`
6. Explicit `runtime` configuration in `functionAppConfig`
7. **Required**: Storage Blob Data Contributor role assignment for managed identity

**Role Assignment** (main.bicep):
```bicep
resource functionAppStorageBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(functionStorageAccount.id, functionApp.id, 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  scope: functionStorageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe') // Storage Blob Data Contributor
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
```

### Application Code Changes

**No application code changes required**:
- .NET 8 isolated worker model is fully compatible
- Existing HTTP triggers, bindings, and dependencies work unchanged
- `local.settings.json` for local development remains the same
- Function attributes and signatures are identical

### Deployment Process

**Migration Steps**:
1. Update Bicep templates (main.bicep, modules/function-app.bicep)
2. Deploy infrastructure changes to dev environment
3. Deploy application code (no code changes needed)
4. Validate functionality with E2E tests
5. Monitor performance and costs
6. Replicate to staging/production environments

**Zero-downtime migration**:
- Deployment slots can be used for blue-green deployment
- DNS cutover through API Management ensures no user impact

## Alternatives Considered

### Alternative 1: Stay on Linux Consumption Plan (Y1)
**Pros**:
- No migration effort required
- Known performance characteristics

**Cons**:
- **Platform retirement**: Microsoft is deprecating this plan
- **Forced migration**: Will be required to migrate eventually
- **No new features**: Missing out on Flex Consumption improvements
- **Risk**: Service disruption if not migrated before retirement

**Rejected**: Not viable due to Microsoft retirement announcement.

### Alternative 2: Upgrade to Premium Plan (EP1)
**Pros**:
- No cold starts (always-warm)
- VNET integration
- Unlimited execution time

**Cons**:
- **Cost**: ~$200/month vs $5-10/month (20x cost increase)
- **Over-provisioned**: Always-warm not needed for 8 hours/week usage
- **Complexity**: More configuration options than needed

**Rejected**: Cost not justified by requirements. Flex Consumption provides sufficient capabilities.

### Alternative 3: Move to Container Apps
**Pros**:
- Modern container-based platform
- Kubernetes compatibility
- Scale-to-zero available

**Cons**:
- **Complexity**: Requires Docker images, container registry management
- **Migration effort**: Significant code and infrastructure changes
- **Similar cost**: No cost advantage over Flex Consumption
- **Team learning curve**: More DevOps skills required

**Rejected**: Unnecessary complexity without corresponding benefit.

### Alternative 4: Move to Azure App Service (B1 tier)
**Pros**:
- No cold starts
- Predictable performance
- Simple deployment model

**Cons**:
- **Cost**: $55/month per service × 5 services = $275/month
- **Always-on**: Wastes resources during 90%+ idle time
- **No scale-to-zero**: Against cost efficiency requirement

**Rejected**: Cost prohibitive and doesn't align with usage pattern.

## Consequences

### Positive
- ✅ **Platform compliance**: Aligned with Microsoft's platform direction
- ✅ **Improved performance**: Faster cold starts, better scaling
- ✅ **Enhanced security**: Managed identity for deployment (no storage keys)
- ✅ **Cost maintained**: Same pay-per-use model as Y1
- ✅ **Future-ready**: Access to new features and capabilities
- ✅ **Better scaling**: Configurable instance count and memory
- ✅ **VNET support**: Option for private networking if needed
- ✅ **Minimal migration effort**: No code changes required
- ✅ **Zero-downtime migration**: Can use deployment slots

### Negative
- ⚠️ **Cold starts still exist**: Though improved, not eliminated
  - Mitigation: Pre-warming during operating hours (if needed)
  - Acceptable: Users expect social club responsiveness, not millisecond SLAs
  
- ⚠️ **New platform**: Less mature than Y1 (fewer years in production)
  - Mitigation: Monitor closely during initial deployment
  - Microsoft commitment: This is the strategic platform direction
  
- ⚠️ **Different configuration model**: Infrastructure as code needs updates
  - Mitigation: One-time update, well-documented by Microsoft

### Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Unexpected behavior differences between Y1 and FC1 | Medium | Low | Thorough testing in dev environment before production rollout |
| Cold start performance worse than Y1 | Low | Very Low | Microsoft documents improvements; can configure always-ready instances if needed |
| Cost increase due to different billing model | Medium | Low | Set up cost alerts at $30/month; monitor weekly execution metrics |
| Deployment issues with managed identity | Medium | Low | Test deployment process in dev; fallback to connection string if critical |
| Team unfamiliarity with new configuration | Low | Medium | Document changes; provide training materials; gradual rollout |

## Monitoring & Success Criteria

### Metrics to Track (Post-Migration)
- **Cold start frequency**: Number of cold starts per week (target: <10)
- **Cold start duration**: P95 cold start time (target: <3 seconds, improvement from Y1)
- **Monthly execution cost**: Total compute costs (target: <$10/month, same as Y1)
- **API response time**: P95 response time including cold starts (target: <2 seconds)
- **Error rate**: Function execution errors (target: <0.1%)
- **Deployment success rate**: Successful deployments (target: 100%)

### Success Criteria
- ✅ All services successfully migrated to Flex Consumption
- ✅ Zero user-impacting incidents during migration
- ✅ Cold start performance improved or maintained vs Y1
- ✅ Monthly costs remain within $5-10 range
- ✅ All E2E tests passing post-migration
- ✅ No degradation in API response times

### Review Schedule
- **First week**: Daily monitoring of metrics and error rates
- **First month**: Weekly review of performance and costs
- **Quarterly**: Full review of platform fit, cost trends, and feature utilization
- **Next review date**: 2026-02-01 (3 months after migration)

## Migration Timeline

| Phase | Duration | Activities | Success Criteria |
|-------|----------|-----------|------------------|
| **Phase 1: Infrastructure Update** | 1 day | Update Bicep templates, deploy to dev | Dev infrastructure deployed successfully |
| **Phase 2: Dev Testing** | 2 days | Deploy app, run E2E tests, validate functionality | All E2E tests passing, no errors |
| **Phase 3: Monitoring** | 1 week | Monitor dev performance, costs, cold starts | Metrics within acceptable ranges |
| **Phase 4: Production Migration** | 1 day | Deploy to production, validate, monitor | Production functional, metrics stable |
| **Phase 5: Post-Migration** | 1 month | Close monitoring, optimization, documentation | All success criteria met |

**Total Timeline**: ~2 weeks from start to production migration completion

## Documentation Updates Required

The following documentation MUST be updated to reflect Flex Consumption Plan:
- ✅ `infrastructure/main.bicep` - App Service Plan configuration
- ✅ `infrastructure/modules/function-app.bicep` - Function App resource definition
- ✅ `docs/adr/0001-use-azure-functions-consumption-plan.md` - Add superseded notice
- ⏳ `docs/deployment.md` - Update deployment instructions
- ⏳ `docs/LOCAL-DEVELOPMENT.md` - Update platform references
- ⏳ `docs/infrastructure/README.md` - Update architecture overview
- ⏳ `docs/AZURE-DEPLOYMENT-URLS.md` - Update runtime notes
- ⏳ `specs/001-create-a-series/plan.md` - Update technical architecture
- ⏳ `.specify/memory/constitution.md` - Update if needed

## Future Considerations

**Flex Consumption Features to Explore**:
1. **Always-ready instances**: If cold starts become problematic
   - Cost: Additional ~$30/month per always-ready instance
   - Use case: If <2s response time becomes hard requirement
   
2. **VNET integration**: If private networking needed
   - Use case: If connecting to on-premises resources or private databases
   
3. **Deployment slots**: For blue-green deployments
   - Use case: Zero-downtime production deployments with validation
   
4. **Instance memory scaling**: If 2048 MB insufficient
   - Options: 2048 MB, 4096 MB (higher memory = higher cost)
   
5. **HTTP concurrency tuning**: If request throughput needs optimization

**Migration Path (if requirements change)**:
- **To Premium Plan**: Change SKU from FC1 to EP1 (same code)
- **To Container Apps**: Containerize functions (moderate effort)
- **To App Service**: Host functions in App Service (supported)

All migrations preserve existing application code and business logic.

---

## Related Decisions
- **ADR 0001**: Use Azure Functions Consumption Plan for Microservices (SUPERSEDED by this ADR)
- **ADR 0002**: Shared Database with Schema Isolation
- **ADR 0003**: Functional Programming with Entity Framework Core

---

## References
- [Azure Functions Flex Consumption Plan documentation](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan)
- [Migrate from Consumption to Flex Consumption](https://learn.microsoft.com/azure/azure-functions/migrate-functions-consumption-to-flex-consumption)
- [Linux Consumption Plan retirement announcement](https://azure.microsoft.com/updates/)

---

**Approved By**: [Technical Lead Name]  
**Date**: 2025-11-01
