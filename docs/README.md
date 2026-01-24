# WebmentionService Documentation

This directory contains comprehensive documentation for the WebmentionService project.

## Structure

### Architecture Decision Records (ADRs)
`adr/` - Documents significant architectural and technical decisions made throughout the project lifecycle.

- Use template: `0000-template.md`
- Naming convention: `NNNN-brief-title.md` (e.g., `0001-upgrade-to-dotnet10.md`)
- Status tracking: Proposed → Accepted → [Deprecated/Superseded]

### Project Tracking
`projects/active/` - Active initiatives and ongoing work  
`projects/completed/` - Finished projects with outcomes

### Version History
`changelog.md` - Chronological record of all changes following Keep a Changelog format

## Contributing

### Creating a New ADR

1. Copy `adr/0000-template.md`
2. Increment the number (check existing ADRs)
3. Fill in all sections with context, decision, and consequences
4. Submit for review as part of your PR
5. Update status from "Proposed" to "Accepted" when merged

### Updating Changelog

When making changes:
- Add entries under `[Unreleased]` section
- Use categories: Added, Changed, Deprecated, Removed, Fixed, Security
- Include technical implementation details in Technical subsection
- Update version number and date when releasing

### Project Documentation

Track significant initiatives in `projects/`:
- Create markdown file with goals, tasks, and acceptance criteria
- Move to `completed/` when done
- Reference related ADRs and issues

## AI Assistant Guidelines

For AI assistants working on this codebase, see `/AGENTS.md` in the repository root for:
- Build and development instructions
- Architecture overview
- Common patterns and conventions
- Testing and validation procedures
