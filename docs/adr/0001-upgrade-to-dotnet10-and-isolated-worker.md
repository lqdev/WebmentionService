# 0001. Upgrade to .NET 10 LTS and Isolated Worker Model

**Date**: 2026-01-24  
**Status**: Accepted  
**Deciders**: Luis Quintanilla  
**Technical Story**: [Issue #5](https://github.com/lqdev/WebmentionService/issues/5)

## Context

WebmentionService has been running on .NET 6.0 with Azure Functions v4 in-process execution model since 2022. As of November 2024, .NET 6.0 has reached End of Life and no longer receives security updates or bug fixes from Microsoft. The application needs to be modernized to ensure continued security support and take advantage of performance improvements in newer .NET versions.

### Current State
- **Runtime**: .NET 6.0 (EOL November 2024)
- **Execution Model**: In-process Azure Functions
- **Packages**: Microsoft.NET.Sdk.Functions with WebJobs extensions
- **Entry Point**: FunctionsStartup attribute in Startup.fs
- **Deployment**: Manual via Azure Functions Core Tools
- **CI/CD**: None

### Constraints
- Small team (single developer)
- No existing automated tests
- Critical production service handling webmentions
- Dependency on WebmentionFs library (v0.0.7)
- Must maintain backward compatibility with existing stored data

### Research Findings

1. **.NET Support Timeline**:
   - .NET 6.0: EOL November 2024 ✗
   - .NET 8.0: LTS until November 2026 (shorter window)
   - .NET 10.0: LTS until November 2028 ✓ (recommended)

2. **Azure Functions Execution Models**:
   - In-process: Being deprecated, no .NET 10 support
   - Isolated worker: Required for .NET 10, better separation, more resilient

3. **WebmentionFs Compatibility**:
   - Targets .NET Standard 2.1 ✓ (compatible with .NET 10)
   - Uses ASP.NET Core HttpRequest ✗ (not available in isolated worker)
   - Workaround available: Manual form parsing + direct validation calls

## Decision

We will upgrade WebmentionService to **.NET 10.0 LTS** and migrate to the **isolated worker execution model**, implementing the following changes:

1. **Framework Upgrade**
   - Target net10.0 in project file
   - Update all Azure Functions packages to isolated worker versions
   - Replace Microsoft.NET.Sdk.Functions with Microsoft.Azure.Functions.Worker.Sdk

2. **Execution Model Migration**
   - Create Program.fs as new entry point with HostBuilder
   - Delete Startup.fs (FunctionsStartup no longer used)
   - Move dependency injection to Program.fs
   - Update function signatures to use HttpRequestData and FunctionContext

3. **WebmentionFs Integration**
   - Implement manual form parsing workaround
   - Call validation services directly instead of using ReceiveAsync
   - Track upstream issue for native isolated worker support

4. **CI/CD Implementation**
   - Create GitHub Actions workflow for automated deployment
   - Use Service Principal authentication with AZURE_CREDENTIALS secret
   - Trigger on merge to main branch

5. **Documentation Structure**
   - Establish docs/ with ADRs, project tracking, and changelog
   - Create AGENTS.md for AI assistant guidance
   - Follow Keep a Changelog format

## Consequences

### Positive

- **Extended Support**: Security updates until November 2028 (4+ years)
- **Performance**: .NET 10 offers 20-30% performance improvements over .NET 6
- **Resilience**: Isolated worker model provides better process isolation
- **Modern Patterns**: Aligns with current Azure Functions best practices
- **Automation**: CI/CD reduces deployment errors and manual toil
- **Maintainability**: Improved documentation structure aids future work
- **AI-Friendly**: AGENTS.md enables effective autonomous assistance

### Negative

- **Migration Effort**: ~8-12 hours for code changes, testing, and deployment
- **WebmentionFs Workaround**: Manual form parsing adds complexity until library updated
- **Breaking Changes**: Different function signatures require code updates
- **Learning Curve**: Isolated worker model has different patterns than in-process
- **Testing Gap**: No automated tests means manual validation required

### Risks

- **Deployment Failure**: Mitigated by feature branch workflow and rollback plan
- **Data Loss**: No risk - Table Storage schema unchanged
- **Downtime**: Minimal - Azure Functions supports zero-downtime deployments
- **Library Compatibility**: WebmentionFs works with workaround; tracking issue #10
- **Build Complexity**: Isolated worker requires OutputType=Exe; documented in AGENTS.md

## Alternatives Considered

### Option 1: Stay on .NET 6.0

**Description**: Continue using .NET 6.0 and apply security patches manually

**Pros**:
- No migration effort required
- No risk of breaking changes
- Existing code continues to work

**Cons**:
- No official security updates after EOL
- Growing security vulnerabilities over time
- Technical debt accumulation
- Performance gap vs newer runtimes

**Decision**: Rejected - unacceptable security risk for production service

### Option 2: Upgrade to .NET 8.0 LTS

**Description**: Target .NET 8.0 instead of .NET 10.0

**Pros**:
- Longer track record (released Nov 2023)
- More community resources and examples
- LTS support until November 2026

**Cons**:
- Only 2 years of support remaining vs 4+ for .NET 10
- Would require another migration in ~2 years
- .NET 10 offers better performance

**Decision**: Rejected - .NET 10 provides longer support window and better ROI

### Option 3: Rewrite in Different Language/Framework

**Description**: Migrate to Node.js, Python, or other Azure Functions runtime

**Pros**:
- Could avoid F# ecosystem constraints
- Potentially simpler async patterns
- Larger community for some languages

**Cons**:
- Complete rewrite (40+ hours)
- Loss of F# functional programming benefits
- Need to find equivalent to WebmentionFs
- Risk of introducing bugs in translation
- Team has F# expertise

**Decision**: Rejected - not justified for small codebase, F# works well

### Option 4: Containerize with Docker

**Description**: Run as containerized app on Azure Container Apps or AKS

**Pros**:
- More control over runtime environment
- Could continue using in-process model
- Easier local development

**Cons**:
- Higher operational complexity
- Increased cost (always-on vs consumption)
- Overkill for small service
- Still need to upgrade .NET version

**Decision**: Rejected - Azure Functions consumption plan is cost-effective and sufficient

## Implementation Plan

See [Issue #5](https://github.com/lqdev/WebmentionService/issues/5) for detailed implementation steps.

### Phase 1: Repository Setup
- Create feature branch
- Establish documentation structure
- Create AGENTS.md

### Phase 2: Code Migration
- Update project file to net10.0
- Create Program.fs entry point
- Delete Startup.fs
- Update function files for isolated worker

### Phase 3: CI/CD
- Create GitHub Actions workflow
- Set up Service Principal
- Configure secrets

### Phase 4: Deployment
- Update Azure Function App configuration
- Deploy and validate
- Monitor for issues

## References

- [Azure Functions .NET Isolated Process Guide](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [Migrate to Isolated Worker Model](https://learn.microsoft.com/en-us/azure/azure-functions/migrate-dotnet-to-isolated-model)
- [.NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- [.NET Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
- [ADR Process](https://adr.github.io/)
- [WebmentionFs Issue #10](https://github.com/lqdev/WebmentionFs/issues/10)
