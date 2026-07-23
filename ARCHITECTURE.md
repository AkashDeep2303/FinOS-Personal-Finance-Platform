# FinOS Architecture

## System overview

FinOS covers identity, accounts, transactions, budgets, investments, loans/EMIs, goals, analytics, notifications, and AI-assisted finance queries. INR is the default currency.

```text
Vue SPA :5173 -> YARP :6000
                    |-> Identity :5001
                    |-> CoreFinance :5002
                    |-> Budget :5003
                    |-> Investment :5004
                    |-> Loan :5005
                    |-> Goals :5006
                    |-> Analytics :5007
                    |-> AI Assistant :5008
                    |-> Notification :5009
                              |
                              v
                    SQL Server / FinOS :1433
                    Optional Redis :6379
```

SQL Server is required in the current Compose flow. Redis is optional. Event-bus projects exist, but local full-stack Compose does not require RabbitMQ.

## Stack

| Layer | Technology |
|---|---|
| Client | Vue 3, Vite, Router, Pinia, Axios, Tailwind, Chart.js |
| Gateway | ASP.NET Core 8, YARP |
| Services | ASP.NET Core 8, MediatR, FluentValidation |
| Data | Dapper/ADO.NET, SQL Server |
| Security | JWT access/refresh flow |
| Hosting | Docker Compose, Kestrel scripts, optional IIS |

## Frontend and gateway

`FinOS.Frontend/src` separates `api`, `stores`, `views`, `components`, `router`, and `assets`.

```text
View/component -> Pinia -> API module -> Axios -> YARP
```

Login/register are public; financial pages require authentication.

| Gateway prefix | Service |
|---|---|
| `/api/identity/users/*` | Identity users |
| `/api/identity/*` | Identity auth |
| `/api/corefinance/*` | CoreFinance |
| `/api/budget/*` | Budget |
| `/api/investment/*` | Investment |
| `/api/loan/*` | Loan |
| `/api/goals/*` | Goals |
| `/api/analytics/*` | Analytics |
| `/api/aiassistant/*` | AI Assistant |
| `/api/notification/*` | Notification |

The gateway authenticates/proxies; services own domain behavior.

## Services

Each bounded context uses `API -> Application -> Domain`, with Infrastructure implementing persistence and external interfaces. API hosts controllers/composition; Application owns commands, queries, DTOs, validators; Domain owns entities, enums, and interfaces; Infrastructure owns Dapper repositories.

| Service | Responsibility |
|---|---|
| Identity | Users, login, roles, passwords, tokens |
| CoreFinance | Accounts, categories, transactions, schedules, subscriptions |
| Budget | Budgets, allocations, alerts, savings rules |
| Investment | Portfolios, holdings, SIPs, EPF, gold, returns |
| Loan | Loans, EMIs, prepayments, simulations |
| Goals | Goals, templates, contributions, projections |
| Analytics | Net worth, aggregates, trends, score |
| AI Assistant | Conversations, queries, feedback |
| Notification | Preferences, types, delivery state |

## Persistence and flow

Services share `FinOS`, divided into `Security`, `Core`, `Budget`, `Investment`, `Loan`, `Goals`, `Analytics`, `AI`, `Notifications`, `Subscriptions`, and `Import` schemas.

```text
Schema -> SeedData -> StoredProcedures -> Views -> optional Jobs/Manual
```

Jobs process recurring activity, analytics, maintenance, and monthly tasks. Manual scripts may mutate or purge substantial data.

```text
Vue -> store -> API module -> YARP -> controller -> MediatR
 -> repository -> Dapper -> SQL -> ApiResponse DTO
```

Check payload, route, DTO, validation, repository parameters, and SQL result columns for every contract change.

## Constraints

Configure SQL, JWT, certificate, and LLM secrets outside Git. Validate authenticated-user ownership. Use decimal money and UTC storage. Preserve soft deletion, audit behavior, `ApiResponse<T>`, and centralized errors. Avoid sensitive data in logs.

## Development and decisions

Start infrastructure from `FinOS.Backend` with `docker compose -f docker-compose.infra.yml up -d`, services with `.\start-all.ps1`, and the frontend with `npm run dev`. Build with `dotnet build FinOS.Backend\FinOS.sln --no-restore` and `npm run build` in `FinOS.Frontend`.

Extend the owning service before adding one. Avoid direct service coupling and shared domain models. Treat endpoints, DTOs, procedures, and view columns as contracts. Update this document when boundaries, ports, routes, storage ownership, deployment order, or foundational technology changes.
