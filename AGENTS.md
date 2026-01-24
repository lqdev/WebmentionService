# AGENTS.md - AI Assistant Guide for WebmentionService

> **Note**: This guide is for AI coding assistants (GitHub Copilot, ChatGPT, Claude, etc.) working on the WebmentionService codebase. It provides essential context, patterns, and gotchas to enable effective autonomous contributions.

## Repository Overview

**WebmentionService** is an Azure Functions-based service written in **F#** that processes [Webmentions](https://www.w3.org/TR/webmention/) - a web standard for decentralized notifications between websites. The service validates incoming webmention requests, stores them in Azure Table Storage, and generates RSS feeds from collected mentions.

### Current Status (as of Jan 2026)
- **Runtime**: .NET 10.0 LTS (upgraded from .NET 6.0 EOL)
- **Execution Model**: Isolated worker process (migrated from in-process)
- **Framework**: Azure Functions v4
- **CI/CD**: GitHub Actions on push to main

### Project Characteristics
- **Language**: F# (functional programming paradigm)
- **Size**: Small codebase (~600 lines total, 6 F# source files)
- **Type**: Serverless microservice / Azure Functions application
- **Testing**: No automated test suite - validation is manual
- **Deployment**: Azure Function App `lqdevwebmentions` in resource group `luisquintanillamewm-rg`

### Key Dependencies
- `lqdev.WebmentionFs` (v0.0.7) - Core webmention validation library
  - **Important**: Uses ASP.NET Core `HttpRequest`, requires workaround for isolated worker model
  - See [WebmentionFs #10](https://github.com/lqdev/WebmentionFs/issues/10)
- Azure Functions Worker SDK for isolated process model
- Azure.Data.Tables for Table Storage operations

## Architecture

### Main Components

1. **ReceiveWebmention Function** ([ReceiveWebmention.fs](ReceiveWebmention.fs))
   - HTTP POST endpoint at `/api/inbox`
   - Validates and stores incoming webmentions
   - Uses Azure Table Storage with source/target URLs as keys
   - **Pattern**: HttpRequestData → Manual form parsing → Validation → Table Storage

2. **WebmentionToRss Function** ([WebmentionToRss.fs](WebmentionToRss.fs))
   - Timer-triggered (daily at 3 AM UTC: `0 0 3 * * *`)
   - Generates RSS feeds from stored webmentions
   - Outputs to Azure Blob Storage container `webmentions`

3. **Domain Models** ([Domain.fs](Domain.fs))
   - `WebmentionEntity`: Table entity inheriting from `ITableEntity`
   - Storage keys: PartitionKey = URL-encoded target, RowKey = URL-encoded source

4. **Services**
   - [RssService.fs](RssService.fs): RSS 2.0 feed generation logic
   - [Program.fs](Program.fs): Entry point with DI configuration

### File Structure
```
/
├── Domain.fs                  # Data models
├── RssService.fs             # RSS generation service
├── Program.fs                # Entry point & DI (isolated worker)
├── ReceiveWebmention.fs      # HTTP endpoint function
├── WebmentionToRss.fs        # Timer-triggered RSS function
├── WebmentionService.fsproj  # F# project file
├── host.json                 # Azure Functions host config
├── AGENTS.md                 # This file
├── docs/                     # Documentation
│   ├── adr/                  # Architecture Decision Records
│   ├── projects/             # Project tracking
│   └── changelog.md          # Version history
├── .github/workflows/        # CI/CD pipelines
└── .vscode/                  # VS Code tasks and settings
```

## Build & Development Instructions

### Prerequisites

**CRITICAL**: This project requires **.NET 10.0 runtime** to build successfully.

**Installation Command** (if .NET 10.0 is missing):
```bash
# Windows (PowerShell)
winget install Microsoft.DotNet.SDK.10

# Linux/Mac
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 10.0
```

Verify installation:
```bash
dotnet --list-sdks | grep "10.0"
```

### Build Commands

**IMPORTANT**: Always run commands in this exact order:

1. **Restore dependencies** (always run first):
   ```bash
   dotnet restore
   ```
   - Takes ~6-10 seconds on first run
   - Must complete before building

2. **Build** (Debug):
   ```bash
   dotnet build
   ```
   - Takes ~2-8 seconds after restore
   - Output: `bin/Debug/net10.0/WebmentionService.dll`

3. **Clean**:
   ```bash
   dotnet clean
   ```
   - Removes `bin/` and `obj/` directories

4. **Publish** (Release):
   ```bash
   dotnet publish --configuration Release
   ```
   - Output: `bin/Release/net10.0/publish/`
   - Used for Azure deployment

### VS Code Tasks

The repository includes pre-configured VS Code tasks in `.vscode/tasks.json`:
- `clean (functions)` - Clean build artifacts
- `build (functions)` - Build with dependencies (default build task)
- `publish (functions)` - Release build for deployment

### Local Development Setup

To run locally, create `local.settings.json` (git-ignored):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PERSONAL_WEBSITE_HOSTNAMES": "localhost:3000,127.0.0.1:3000"
  }
}
```

**Note**: Requires [Azurite](https://github.com/Azure/Azurite) for local storage emulation.

## Configuration

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `AzureWebJobsStorage` | Yes | Azure Storage connection string |
| `PERSONAL_WEBSITE_HOSTNAMES` | Yes | Comma-separated hostnames to accept webmentions for |
| `FUNCTIONS_WORKER_RUNTIME` | Yes | Must be `dotnet-isolated` for .NET 10 |

Configured in [Program.fs](Program.fs) during dependency injection setup.

### Azure Function App Settings

Current deployment target:
- **Function App**: `lqdevwebmentions`
- **Resource Group**: `luisquintanillamewm-rg`
- **Subscription**: `04edd16f-fa44-4e69-87c0-72a91e94a540`
- **Runtime Stack**: `DOTNET-ISOLATED|10.0`

## Testing & Validation

**No automated tests exist** in this repository. Validation is manual:

1. **Build validation**: `dotnet build` succeeds
2. **Publish validation**: `dotnet publish --configuration Release` succeeds
3. **Code review**:
   - No syntax errors in F# code
   - Function attributes remain intact
   - Azure bindings are not modified incorrectly
4. **Manual endpoint testing**: POST to /api/inbox with valid webmention
5. **Timer function testing**: Verify RSS generation (check blob storage)

## Common Issues & Workarounds

### Issue 1: WebmentionFs HttpRequest Incompatibility

**Problem**: WebmentionFs library expects `HttpRequest` (ASP.NET Core), but isolated worker provides `HttpRequestData`.

**Solution**: Manual form parsing workaround in [ReceiveWebmention.fs](ReceiveWebmention.fs):
```fsharp
use reader = new StreamReader(req.Body)
let! body = reader.ReadToEndAsync()
let formData = HttpUtility.ParseQueryString(body)
let source = formData.["source"]
let target = formData.["target"]
// Create UrlData and call validation services directly
```

**Tracking**: [WebmentionFs #10](https://github.com/lqdev/WebmentionFs/issues/10)

### Issue 2: F# Compilation Order

**Problem**: F# requires files to be compiled in dependency order.

**Solution**: The [WebmentionService.fsproj](WebmentionService.fsproj) lists files correctly:
1. Domain.fs (types first)
2. RssService.fs (service)
3. Program.fs (DI setup)
4. ReceiveWebmention.fs (function)
5. WebmentionToRss.fs (function)

**Never reorder files** in the project file unless you understand F# dependency requirements.

### Issue 3: local.settings.json Missing

**Problem**: Git-ignored file not present in fresh clones.

**Solution**: Create manually using the template in "Local Development Setup" section.

## Coding Guidelines

### F# Specific Patterns
- **Functional programming**: Immutable data, pure functions preferred
- **Pattern matching**: Use instead of if-else chains
- **Pipelines**: Use `|>` for data transformations
- **Type inference**: Let compiler infer types when possible
- **Async workflows**: Use `task { }` for async operations (not async { })

### Azure Functions Isolated Worker Conventions
- Function names: `[<Function("FunctionName")>]` attribute
- HTTP triggers: `[<HttpTrigger(AuthorizationLevel, "method", Route = "path")>]`
- Timer triggers: `[<TimerTrigger("cron expression")>]`
- Table bindings: `[<TableInput("tableName", Connection = "...")>]`
- Blob bindings: `[<BlobOutput("container/path", Connection = "...")>]`
- Logging: Use `FunctionContext.GetLogger("FunctionName")`
- Return types: `HttpResponseData`, not `IActionResult`

### Key Code Patterns

**Dependency Injection** ([Program.fs](Program.fs)):
```fsharp
HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(fun services ->
        services.AddScoped<ServiceType>() |> ignore
    )
    .Build()
```

**Table Storage Keys**:
- PartitionKey = `Uri.EscapeDataString(targetUrl)`
- RowKey = `Uri.EscapeDataString(sourceUrl)`

**Error Handling**:
```fsharp
let response = req.CreateResponse(HttpStatusCode.BadRequest)
do! response.WriteStringAsync("Error message")
return response
```

## Deployment

### Automated (Preferred)
GitHub Actions workflow triggers on push to `main`:
- Runs `dotnet build` and `dotnet publish`
- Authenticates with Azure using Service Principal
- Deploys to Function App using `Azure/functions-action@v1`

See [.github/workflows/deploy.yml](.github/workflows/deploy.yml)

### Manual (Fallback)
```bash
func azure functionapp publish lqdevwebmentions
```

**Pre-deployment**: VS Code setting `azureFunctions.preDeployTask` runs `publish (functions)` automatically.

## Documentation Practices

### When to Create ADRs
Create an ADR in `docs/adr/` for:
- Significant architectural changes
- Technology stack decisions
- Breaking changes to APIs or data models
- Performance or security trade-offs

Use template: `docs/adr/0000-template.md`

### When to Update Changelog
Update `docs/changelog.md` for every PR:
- Add entries under `[Unreleased]` section
- Categories: Added, Changed, Deprecated, Removed, Fixed, Security, Technical
- Include issue/PR references

### Project Tracking
For multi-step initiatives:
1. Create markdown file in `docs/projects/active/`
2. Include goals, tasks (checkboxes), acceptance criteria
3. Reference related issues and ADRs
4. Move to `docs/projects/completed/` when done

## Quick Reference

### Most Common Workflow
1. Ensure .NET 10.0 SDK installed
2. Create feature branch: `git checkout -b feature/description`
3. `dotnet restore` (if dependencies changed)
4. Make code changes
5. `dotnet build` (verify compilation)
6. Update `docs/changelog.md` under `[Unreleased]`
7. Commit and push
8. Create PR to `main`

### Critical Reminders
- **Always target .NET 10.0** (not .NET 6, 8, or other versions)
- **F# file order matters** in .fsproj
- **No tests exist** - validate manually
- **WebmentionFs workaround** required for HttpRequest
- **local.settings.json is git-ignored** - create manually
- **Document significant decisions** in ADRs
- **Update changelog** for all changes

### Pre-Commit Checklist
- [ ] `dotnet build` succeeds without warnings
- [ ] `dotnet publish --configuration Release` succeeds
- [ ] Changelog updated in `[Unreleased]` section
- [ ] ADR created if architectural decision made
- [ ] F# file order preserved in .fsproj
- [ ] No hardcoded secrets or connection strings

## Additional Resources

- [README.md](README.md) - User-facing documentation and API examples
- [docs/README.md](docs/README.md) - Documentation structure guide
- [docs/changelog.md](docs/changelog.md) - Version history
- [docs/adr/](docs/adr/) - Architecture Decision Records
- [.vscode/tasks.json](.vscode/tasks.json) - Build task definitions
- [host.json](host.json) - Azure Functions runtime configuration
- [Issue #5](https://github.com/lqdev/WebmentionService/issues/5) - .NET 10 Migration tracking

## Need Help?

- **Build errors**: Check .NET 10.0 SDK is installed, run `dotnet restore`
- **F# compilation errors**: Verify file order in .fsproj matches dependencies
- **Azure deployment errors**: Check Function App settings match required config
- **WebmentionFs errors**: See Issue #10 workaround above
- **Documentation questions**: See docs/README.md for structure guide

---

**Last Updated**: 2026-01-24  
**Version**: 2.0.0 (.NET 10, Isolated Worker Model)
