# Sha8alny — Agent Instructions

## Mandatory: Read context.md First

Before making **any** code change — creating, editing, or removing a feature — you **MUST** read `context.md` at the project root. It is the single source of truth for:

- Project architecture (Onion Architecture rules)
- Domain model schemas and relationships
- API endpoint inventory and auth requirements
- Coding conventions and naming rules
- DI registration patterns

## Mandatory: Update context.md After Changes

After **every** feature addition, modification, or removal, you **MUST** update `context.md` to reflect the change. This includes:

- New/modified/removed entities → update Section 3 (Domain & Database Schema)
- New/modified/removed endpoints → update Section 4 (Completed Capabilities)
- New/modified enums → update Appendix B
- New/modified configuration → update Appendix C
- Architecture or dependency changes → update Section 2
- If a domain entity changes, also output the `dotnet ef migrations add` command

`context.md` must always be accurate. An outdated context.md is worse than none.
