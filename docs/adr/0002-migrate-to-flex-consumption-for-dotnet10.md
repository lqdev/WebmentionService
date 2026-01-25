# 0002. Migrate to Flex Consumption Plan for .NET 10 Support

**Date**: 2026-01-25  
**Status**: Accepted  
**Deciders**: Luis Quintanilla  
**Technical Story**: [Issue #6 Deployment Failure](https://github.com/lqdev/WebmentionService/actions/runs/21323306266/job/61376285942)

## Context

After successfully upgrading the codebase to .NET 10 and isolated worker model in PR #6, the deployment to Azure failed with a "sync trigger" error. Investigation revealed that the root cause was a platform limitation:

**Microsoft does not support .NET 10 on Linux Consumption plans.**

### Current State
- **Function App**: `lqdevwebmentions` on Linux Consumption plan (Dynamic SKU)
- **Runtime Configuration**: `linuxFxVersion: DOTNET|6.0` (old in-process model)
- **Code**: Successfully migrated to .NET 10 isolated worker model
- **Deployment Status**: Failed - platform incompatibility

### Key Findings

1. **.NET 10 Platform Support**:
   - ✅ Windows Consumption: Supported
   - ✅ Flex Consumption (Linux): Supported
   - ✅ Premium/Dedicated plans: Supported
   - ❌ Linux Consumption: **NOT SUPPORTED**

2. **Cost Analysis**:
   - Current usage: ~1,500 requests/month (10-50/day) + 1 daily timer trigger
   - Linux Consumption free tier: 1M executions, 400K GB-seconds
   - Flex Consumption free tier: 250K executions, 100K GB-seconds
   - **Both plans would cost $0/month for our workload**

3. **Microsoft's Direction**:
   - Flex Consumption is the "recommended serverless hosting option going forward"
   - Linux Consumption plan retirement announced for September 30, 2028
   - Future platform investments prioritize Flex Consumption

### Constraints
- Small single-developer team
- No automated test suite
- Production service must remain available
- Existing data in Table Storage must be preserved
- Simple low-traffic workload (~1,500 requests/month)

## Decision

We will migrate WebmentionService from the **Linux Consumption plan** to the **Flex Consumption plan**, enabling .NET 10 support while maintaining zero operating costs.

### Migration Approach

1. **Use Azure CLI Automated Migration**
   - Leverage `az functionapp flex-migration` commands
   - Automates app creation, configuration transfer, and validation
   - Minimizes manual configuration errors

2. **Create New Function App**
   - Name: `lqdevwebmentions-flex`
   - Plan: New Flex Consumption plan
   - Region: Same as current (for data locality)
   - Runtime: `DOTNET-ISOLATED|10.0`

3. **Parallel Operation Period**
   - Keep old app running during migration
   - Deploy and test new app thoroughly
   - Switch traffic only after full validation
   - Remove old app after stability confirmed

4. **Deployment Updates**
   - Update GitHub Actions workflow with new app name
   - Maintain same deployment credentials (Service Principal)
   - No changes to code or build process required

5. **DNS/Traffic Management**
   - Update any external references to new app URL
   - Consider custom domain for future flexibility

## Consequences

### Positive

- **✅ .NET 10 Support**: Can use latest LTS runtime (support until Nov 2028)
- **✅ Zero Cost**: Usage remains within free tier (250K executions >> 1.5K/month)
- **✅ Future-Proofed**: On Microsoft's recommended modern platform
- **✅ Better Performance**:
  - Faster scaling (up to 1,000 instances vs 200)
  - Optional always-ready instances to eliminate cold starts
  - Improved concurrency handling
- **✅ Advanced Features**:
  - VNet integration available (if ever needed)
  - Per-function scaling control
  - Flexible instance memory sizes (512 MB, 2048 MB, 4096 MB)
- **✅ Avoids Future Migration**: Linux Consumption retires in 2028 anyway
- **✅ Automated Migration**: Azure CLI commands minimize manual work

### Negative

- **Migration Effort**: ~1-2 hours for migration, testing, and validation
- **New Resource Names**: Different app and plan names in Azure
- **Documentation Updates**: Need to update all references to old app name
- **Testing Required**: Manual validation needed (no automated tests)
- **DNS/URL Changes**: May need to update external references

### Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Migration fails | Low | Medium | Keep old app running; automated migration tested by Microsoft |
| Data loss | Very Low | High | Table Storage unaffected; separate resource |
| Deployment breaks | Low | Medium | Test thoroughly before switching traffic |
| Cold starts worsen | Low | Low | Can enable always-ready instances if needed (still $0 for low traffic) |
| Hidden costs | Very Low | Medium | Monitor billing; well within free tier limits |

## Alternatives Considered

### Option 1: Downgrade to .NET 8

**Description**: Change code from .NET 10 to .NET 8, which IS supported on Linux Consumption

**Pros**:
- Quick fix (~15 minutes)
- No infrastructure changes
- No DNS/URL changes
- Still gets LTS support until Nov 2026

**Cons**:
- Not on latest .NET (defeats purpose of recent upgrade)
- Still need to migrate by 2026 (.NET 8 EOL) or 2028 (Linux Consumption retirement)
- Kicks the can down the road

**Decision**: Rejected - We just invested effort in .NET 10 upgrade; don't want to undo that work only to have to upgrade again in 1-2 years.

### Option 2: Move to Windows Consumption

**Description**: Keep Consumption plan but switch from Linux to Windows

**Pros**:
- .NET 10 IS supported on Windows Consumption
- Same plan type (familiar pricing model)
- No cost change
- Smaller migration than Flex

**Cons**:
- Platform change (Linux → Windows) could introduce subtle bugs
- Windows Consumption not the "recommended" path forward
- Missing modern features of Flex (VNet, better scaling)
- Still using older platform architecture

**Decision**: Rejected - Flex Consumption is the better long-term choice with same cost.

### Option 3: Migrate to Premium or Dedicated Plan

**Description**: Move to Premium (EP1) or Dedicated (B1) App Service plan

**Pros**:
- .NET 10 fully supported
- Guaranteed resources (no cold starts)
- VNet integration included

**Cons**:
- **Significant cost increase**: ~$70-150/month vs $0/month
- Overkill for 1,500 requests/month workload
- Always-on billing even with zero traffic

**Decision**: Rejected - Unjustified cost for our tiny workload. Flex Consumption provides same features at $0 for our usage.

### Option 4: Stay on Linux Consumption with .NET 6

**Description**: Revert all changes, stay on .NET 6 and current platform

**Pros**:
- No migration needed
- No risks
- Works today

**Cons**:
- .NET 6 reached EOL November 2024 (no security updates)
- Accumulating technical debt
- Linux Consumption retires in 2028 anyway
- Would still need to migrate eventually

**Decision**: Rejected - Unacceptable security risk to run EOL framework in production.

## Implementation Plan

See [docs/projects/active/flex-consumption-migration.md](../projects/active/flex-consumption-migration.md) for detailed implementation steps.

### Phase 1: Migration Preparation (15 min)
- ✅ Create feature branch
- ✅ Update .gitignore for test artifacts
- ✅ Create this ADR
- Create project tracking document
- Update changelog

### Phase 2: Azure Migration (20 min)
- Run `az functionapp flex-migration list` (eligibility check)
- Execute `az functionapp flex-migration start` (automated migration)
- Verify new app configuration
- Review migrated settings

### Phase 3: Deployment (20 min)
- Update GitHub Actions workflow with new app name
- Deploy .NET 10 code to new Flex app
- Verify deployment succeeds
- Test HTTP endpoint functionality
- Verify timer trigger registration

### Phase 4: Validation (15 min)
- Send test webmentions to new app
- Verify Table Storage writes
- Wait for timer trigger execution (or manually trigger)
- Verify RSS generation in Blob Storage
- Check Application Insights logs

### Phase 5: Cutover (10 min)
- Update any external DNS/URLs (if applicable)
- Monitor new app for 24-48 hours
- Disable old app (stop, don't delete yet)
- Observe for any issues

### Phase 6: Cleanup (5 min)
- After 1 week of stable operation
- Delete old Linux Consumption app
- Update all documentation with new app names
- Close migration project

## References

- [Azure Functions Flex Consumption Plan](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan)
- [Migrate Consumption to Flex Consumption](https://learn.microsoft.com/en-us/azure/azure-functions/migration/migrate-plan-consumption-to-flex?pivots=platform-linux)
- [.NET 10 Support in Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#supported-versions)
- [Azure Functions Pricing](https://azure.microsoft.com/pricing/details/functions/)
- [Linux Consumption Retirement Notice](https://go.microsoft.com/fwlink/?linkid=2335809)
- [PR #6: Upgrade to .NET 10](https://github.com/lqdev/WebmentionService/pull/6)
- [Failed Deployment Run](https://github.com/lqdev/WebmentionService/actions/runs/21323306266/job/61376285942)

## Success Criteria

- [ ] New Flex Consumption app created and configured
- [ ] .NET 10 code successfully deployed to Flex app
- [ ] HTTP endpoint accepts and processes webmentions
- [ ] Timer trigger executes daily and generates RSS feed
- [ ] Table Storage reads/writes function correctly
- [ ] Blob Storage outputs function correctly
- [ ] Application Insights logging operational
- [ ] GitHub Actions deployment pipeline updated
- [ ] Zero cost confirmed in Azure billing (within free tier)
- [ ] All documentation updated with new app names
- [ ] Old app safely removed after validation period

## Post-Migration Actions

1. Monitor Azure Cost Management for 30 days to confirm $0 billing
2. Update README.md with new deployment target information
3. Document any issues encountered for future reference
4. Consider enabling always-ready instances if cold starts become problematic
5. Explore VNet integration options for enhanced security (optional)

---

**Decision Date**: 2026-01-25  
**Expected Completion**: 2026-01-25 (same day)  
**Rollback Plan**: Re-enable old Linux Consumption app if critical issues arise
