---
name: finos-project
description: Work safely in the FinOS Personal Finance Platform. Use when an AI agent must understand, diagnose, implement, review, test, or document its Vue 3 frontend, .NET 8 microservices, YARP gateway, Dapper/SQL Server persistence, Docker environment, or finance domains.
---

# Work on FinOS

Read `/AGENTS.md` before editing and `/ARCHITECTURE.md` for cross-service, API, database, or system-level work.

## Workflow

1. Identify the owning domain.
2. Trace Vue view -> Pinia store -> API module -> YARP -> controller -> command/query -> repository -> SQL.
3. Find existing DTOs, validators, routes, procedures, and response shapes; extend established patterns.
4. Keep work in one bounded context unless it genuinely spans services.
5. Keep dependencies `API -> Application -> Domain`; put implementations in `Infrastructure`.
6. Use MediatR commands for writes, queries for reads, thin controllers, and existing Dapper repositories.
7. Preserve `ApiResponse<T>`, UTC timestamps, decimal money, INR defaults, and soft deletion.
8. Keep HTTP in `src/api`, reusable state/workflows in Pinia, and UI in views/components.
9. Never commit credentials, `.env`, certificates, tokens, or generated output.

## Database-backed changes

Update, as applicable: idempotent schema -> procedures/views -> repository mapping -> DTO/validation/command/query -> controller/gateway -> frontend API/store/view. Document compatibility and ordering.

## Validate

```powershell
dotnet build FinOS.Backend\FinOS.sln --no-restore
Set-Location FinOS.Frontend
npm run build
```

For SQL, verify schema -> seed -> procedures -> views. For APIs, test directly and through YARP. Review the diff for secrets and generated files. Update `/ARCHITECTURE.md` when boundaries, ports, routes, storage ownership, or foundational technology changes.
