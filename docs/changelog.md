# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Documentation structure with ADRs, project tracking, and changelog
- GitHub Actions CI/CD workflow for automated deployment
- AGENTS.md for AI assistant guidance
- New Azure Flex Consumption Function App: `lqdevwebmentions-flex`
- ADR 0002: Migration to Flex Consumption plan for .NET 10 support

### Changed
- Upgraded from .NET 6.0 to .NET 10.0 LTS
- Migrated from in-process to isolated worker execution model
- Updated all Azure Functions packages to isolated worker versions
- **Migrated from Linux Consumption to Flex Consumption plan** (required for .NET 10)
- Updated GitHub Actions workflow to deploy to new Flex app

### Technical
- Created Program.fs as new entry point
- Removed Startup.fs (replaced by Program.fs)
- Updated ReceiveWebmention.fs for HttpRequestData
- Updated WebmentionToRss.fs for isolated worker model
- Modified WebmentionService.fsproj to target net10.0
- Created new Function App with runtime: `dotnet-isolated` version `10.0`
- Configured app settings: PERSONAL_WEBSITE_HOSTNAMES (all domains)
- Updated deployment target in GitHub Actions from `lqdevwebmentions` to `lqdevwebmentions-flex`

## [1.0.0] - 2022-03-XX

### Added
- Initial release with .NET 6.0
- HTTP POST endpoint at /api/inbox for receiving webmentions
- Timer trigger for daily RSS feed generation
- Azure Table Storage integration for webmention persistence
- Azure Blob Storage integration for RSS feed output
