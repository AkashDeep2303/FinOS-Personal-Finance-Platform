# FinOS Agent Instructions

These rules cover the repository. A deeper `AGENTS.md` overrides them locally.

## Start here

FinOS is an Indian personal-finance platform with a Vue 3 client, YARP gateway, nine .NET 8 services, and SQL Server domain schemas. Read [ARCHITECTURE.md](ARCHITECTURE.md) before cross-cutting work. Skill-aware agents should use [skills/finos-project/SKILL.md](skills/finos-project/SKILL.md).

## Map

- `FinOS.Frontend/`: Vue, Vite, Pinia, Router, Axios, Tailwind, Chart.js.
- `FinOS.Backend/APIGateways/FinOS.Gateway/`: YARP entry point.
- `FinOS.Backend/Services/`: nine bounded contexts.
- `FinOS.Backend/Shared/`: common data/API and event abstractions.
- `FinOS.Database/`: schema, procedures, views, seeds, jobs, manual scripts.

## Rules

1. Trace frontend -> gateway -> service -> SQL before editing.
2. Preserve service ownership; keep business logic out of the gateway, controllers, views, and generic helpers.
3. Follow `API -> Application -> Domain`, with implementations in `Infrastructure`.
4. Use established MediatR, DTO, validation, repository, Dapper, Pinia, and API-module patterns.
5. Treat procedures, views, endpoints, DTOs, and response shapes as contracts.
6. Use decimal money, UTC timestamps, INR defaults, and existing soft deletes.
7. Preserve unrelated work and avoid broad cleanup.
8. Update `ARCHITECTURE.md` when architectural facts change.

## Security and data

Never commit `.env`, development settings, keys, certificates, tokens, real connection strings, or LLM credentials. `CHANGE_ME_*` values are placeholders. Do not log passwords, JWTs, account data, or sensitive bodies. Validate authenticated-user ownership; never trust a client user ID alone.

Apply SQL in schema -> seed -> procedures -> views order. Keep schema/seed scripts idempotent. Review jobs when changing recurring transactions, SIPs, EMIs, analytics, alerts, or retention. Never run purge, reset, migration, or manual scripts against an unknown environment.

## Validation

```powershell
dotnet build FinOS.Backend\FinOS.sln --no-restore
Set-Location FinOS.Frontend
npm run build
```

When available, test services directly and through `http://localhost:6000`; test SQL only on local/disposable databases.

## Git

`main` is protected. Use a feature branch and PR, obtain code-owner review, never force-push protected branches, and include validation in the PR.
