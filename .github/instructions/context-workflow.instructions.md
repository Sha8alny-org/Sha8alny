---
description: "Use when creating, editing, or removing any feature in the Sha8alny project. Covers context.md workflow, Onion Architecture rules, entity changes, endpoint updates, and migration commands."
---
# Sha8alny Feature Workflow

## Step 1 — Read context.md

Before touching any file, read `context.md` at the project root. It contains:

- Full Onion Architecture dependency rules (Section 2)
- All domain entity schemas with every property and type (Section 3)
- Complete API endpoint inventory with auth roles (Section 4)
- Pending features and technical debt (Section 5)
- Strict coding rules and patterns (Section 6)
- Enum definitions (Appendix B) and configuration keys (Appendix C)

Do not assume anything about the codebase. Verify against context.md.

## Step 2 — Make the Change

Follow the rules in Section 6 of context.md. Key reminders:

- **Onion Architecture**: Domain has zero dependencies. Service depends only on Abstraction + Domain. Never reverse the flow.
- **No IFormFile in DTOs**: File uploads go through `/api/Media` only. Other entities store URL strings.
- **Use IQueryable + .Include()**: Always eager-load navigation properties to avoid NullReferenceExceptions.
- **ServiceResponse<T>**: All service methods return `ServiceResponse<T>`.
- **Role authorization**: Respect `[Authorize(Roles = "...")]` on every controller action.

## Step 3 — Update context.md

After the change is complete, update the relevant sections of `context.md`:

| Change Type | Sections to Update |
|---|---|
| New/modified domain entity | Section 3 (schema table), Appendix B (if new enum) |
| New/modified API endpoint | Section 4 (endpoint table for the controller) |
| New/modified DI registration | Section 2 (DI Registration subsection) |
| New/modified SignalR hub/event | Section 4.17 (Real-time table) |
| New/modified config key | Appendix C (Configuration Sources) |
| New migration | Section 3 (Migration History table) |
| Architecture change | Section 2 (architecture diagram, dependency rules) |
| New pending feature / tech debt | Section 5 (Roadmap) |

## Step 4 — Output Migration Command (if entity changed)

If a domain model was modified, output the exact command:

```bash
dotnet ef migrations add <MigrationName> --startup-project ../Sh8lny.Web
```

Never create migration files manually.
