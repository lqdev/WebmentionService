# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Documentation structure with ADRs, project tracking, and changelog
- GitHub Actions CI/CD workflow for automated deployment
- AGENTS.md for AI assistant guidance

### Changed
- Upgraded from .NET 6.0 to .NET 10.0 LTS
- Migrated from in-process to isolated worker execution model
- Updated all Azure Functions packages to isolated worker versions

### Technical
- Created Program.fs as new entry point
- Removed Startup.fs (replaced by Program.fs)
- Updated ReceiveWebmention.fs for HttpRequestData
- Updated WebmentionToRss.fs for isolated worker model
- Modified WebmentionService.fsproj to target net10.0

## [1.0.0] - 2022-03-XX

### Added
- Initial release with .NET 6.0
- HTTP POST endpoint at /api/inbox for receiving webmentions
- Timer trigger for daily RSS feed generation
- Azure Table Storage integration for webmention persistence
- Azure Blob Storage integration for RSS feed output
