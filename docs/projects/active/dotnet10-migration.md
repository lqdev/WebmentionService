# .NET 10 Migration and CI/CD Implementation

**Start Date**: 2026-01-24  
**Status**: In Progress  
**Issue**: [#5](https://github.com/lqdev/WebmentionService/issues/5)  
**Branch**: `feature/dotnet10-upgrade-and-cicd`

## Goals

1. Upgrade from .NET 6.0 (EOL) to .NET 10.0 LTS (support until Nov 2028)
2. Migrate from in-process to isolated worker execution model
3. Implement GitHub Actions CI/CD for automated deployment on merge to main
4. Establish AI-friendly documentation structure

## Tasks

### Phase 1: Repository Setup ✓
- [x] Create feature branch
- [x] Create docs structure (adr/, projects/, changelog.md)
- [ ] Move .github/copilot-instructions.md → AGENTS.md
- [ ] Create ADR for migration decisions

### Phase 2: Code Migration
- [ ] Update WebmentionService.fsproj
- [ ] Create Program.fs (new entry point)
- [ ] Delete Startup.fs
- [ ] Update ReceiveWebmention.fs (HttpRequest → HttpRequestData)
- [ ] Update WebmentionToRss.fs (isolated worker bindings)
- [ ] Update host.json if needed
- [ ] Update .vscode/tasks.json

### Phase 3: CI/CD Pipeline
- [ ] Create .github/workflows/deploy.yml
- [ ] Create Azure Service Principal
- [ ] Add AZURE_CREDENTIALS secret to GitHub

### Phase 4: Azure Configuration
- [ ] Update Function App runtime to DOTNET-ISOLATED|10.0
- [ ] Set FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
- [ ] Verify deployment and test endpoints

## Acceptance Criteria

- [ ] `dotnet build` succeeds
- [ ] `dotnet publish --configuration Release` succeeds
- [ ] GitHub Actions workflow triggers on push to main
- [ ] Function App runs on .NET 10 isolated worker
- [ ] POST /api/inbox stores webmentions successfully
- [ ] Timer trigger generates RSS feed daily
- [ ] All documentation (ADR, AGENTS.md) is complete

## Technical Notes

### WebmentionFs Library Workaround
The WebmentionFs library uses `HttpRequest` (ASP.NET Core), but isolated worker provides `HttpRequestData`. Solution: Parse form body manually and call validation services directly.

Related: [WebmentionFs #10](https://github.com/lqdev/WebmentionFs/issues/10)

### F# Compilation Order
Files must be listed in dependency order in .fsproj:
1. Domain.fs
2. RssService.fs
3. Program.fs
4. ReceiveWebmention.fs
5. WebmentionToRss.fs

## References

- Issue: https://github.com/lqdev/WebmentionService/issues/5
- ADR: (to be created)
- [Azure Functions .NET Isolated Guide](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
