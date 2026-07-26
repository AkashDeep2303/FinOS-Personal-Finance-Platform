# FinOS Evolution Architecture

## Direction

FinOS remains a Vue SPA behind a YARP gateway with nine .NET 8 bounded-context services and one SQL Server database separated by schemas. The evolution does not introduce a service per feature or replace Dapper/stored procedures.

```text
Vue view -> domain Pinia store -> API module -> YARP
 -> authorized controller -> MediatR command/query
 -> Dapper repository -> stored procedure/view -> SQL Server
```

## Ownership

- Identity owns users, credentials, roles, tokens, and profile.
- CoreFinance owns accounts, transactions, recurring activity, subscriptions, future general assets/cards/import administration.
- Budget owns budgets, allocations, alerts, and savings rules.
- Investment owns portfolios, holdings, investment transactions, SIP, EPF, gold, and target allocation.
- Loan owns loans, schedules, EMI payments, prepayments, and loan simulations.
- Goals owns goals, contributions, priorities, and goal projections.
- Analytics owns derived read models: command center, net worth, cash flow, financial health, scenarios, and reporting.
- AI Assistant explains and prioritizes deterministic results; it does not own financial math.
- Notification owns notification state and preferences.

## Cross-domain rule

Writes remain in the owning service. Analytics may compose read models from contract views/procedures in the shared database but must not write another service's domain records. Future extraction is justified only by scaling, deployment, or ownership evidence.

## Foundation decisions

- `FinOS.Common.Helpers.FinancialCalculator` is the canonical deterministic calculation layer.
- Money uses `decimal`; rates are absolute decimals unless a legacy method explicitly documents otherwise.
- Frontend values use centralized `en-IN` formatters.
- UI follows answer -> analysis -> details progressive disclosure.
- New endpoints derive user ownership from JWT claims.
- Existing client-supplied user routes are migrated additively to `/me` before removal.
- Tax rules require versioned configuration by financial year/assessment year; slabs are never invented in UI code.
- Scenarios never mutate real financial records.

The system boundary/ports and foundational technology remain unchanged, so root `ARCHITECTURE.md` requires no Phase 1 boundary update.
