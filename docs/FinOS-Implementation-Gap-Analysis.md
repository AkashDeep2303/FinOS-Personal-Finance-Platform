# FinOS Implementation Gap Analysis

## Audit scope and baseline

Audit date: 2026-07-26

This audit covers the 570 repository files visible at the audit date, excluding generated `bin`, `obj`, and `node_modules` content. The trace followed the repository convention:

`Vue view -> Pinia store -> API module -> YARP -> controller -> MediatR -> Dapper repository -> SQL Server procedure/view/table`.

No implementation files were changed before this document was created. The worktree already contained unrelated, uncommitted transaction and AI Assistant changes; those changes must be preserved.

The actual persistence baseline is **Dapper over ADO.NET connection abstractions and stored procedures**, not pure raw ADO.NET. New work should use the established Dapper repositories unless a specific existing repository uses lower-level ADO.NET.

Status legend:

- **EXISTS**: usable end-to-end capability already exists; extend only where the requested experience needs more.
- **PARTIAL**: meaningful pieces exist, but the requested workflow is incomplete.
- **MISSING**: no meaningful implementation was found.
- **NEEDS REFACTOR**: capability exists but its current contract, security, correctness, or composition should be improved before expansion.

## 1. Existing architecture and features

### Frontend

Vue 3/Vite SPA using Vue Router, Pinia, Axios, Tailwind CSS, Chart.js/vue-chartjs, and date-fns. Source is separated into `api`, `stores`, `views`, `components`, `router`, and `assets`.

Existing authenticated pages:

| Page | Route | Status |
|---|---|---|
| Dashboard | `/dashboard` | PARTIAL |
| Accounts | `/accounts` | EXISTS |
| Transactions (also reused for categories) | `/transactions`, `/categories` | PARTIAL |
| Budgets | `/budgets` | EXISTS |
| Investments | `/investments` | PARTIAL |
| Loans | `/loans` | PARTIAL |
| Goals | `/goals` | PARTIAL |
| Analytics | `/analytics` | PARTIAL |
| AI Assistant | `/ai-assistant` | PARTIAL |
| Settings | `/settings` | PARTIAL |
| Login/Register | `/login`, `/register` | EXISTS |

Existing reusable components are `Navbar`, `Sidebar`, `StatCard`, and `TransactionModal`. Shared loading, error, empty, explanation, chart-card, status, range-filter, and money-display primitives are missing.

Pinia stores and API modules exist for auth, accounts, transactions, budgets, investments, loans, goals, analytics, and AI Assistant. Most server calls are correctly isolated under `src/api`. State is domain-sized, but loading/error patterns and response normalization are inconsistent.

The dashboard currently fans out to seven stores and calculates net worth and savings rate in the client. That duplicates deterministic analytics and risks inconsistent definitions.

### Backend and gateway

YARP on port 6000 routes nine prefixes to nine .NET 8 services:

| Gateway prefix | Service/port | Existing capability |
|---|---|---|
| `/api/identity/*` | Identity/5001 | JWT auth, refresh, profile, roles, audit |
| `/api/corefinance/*` | CoreFinance/5002 | accounts, categories, transactions, schedules, subscriptions |
| `/api/budget/*` | Budget/5003 | budgets, categories, alerts, savings rules |
| `/api/investment/*` | Investment/5004 | portfolios, holdings, transactions, SIP, EPF, gold, summaries |
| `/api/loan/*` | Loan/5005 | loans, EMI schedule/payments, prepayment |
| `/api/goals/*` | Goals/5006 | goals, templates, contributions, projections |
| `/api/analytics/*` | Analytics/5007 | net worth, scores, aggregates, spending/trends |
| `/api/aiassistant/*` | AI Assistant/5008 | conversations, messages, feedback |
| `/api/notification/*` | Notification/5009 | notifications, types, preferences |

Services follow `API -> Application -> Domain`, with Infrastructure implementing repositories. MediatR/CQRS and FluentValidation are established. `FinOS.Common` supplies `ApiResponse<T>`, paging, exceptions, middleware, Dapper connection/unit-of-work abstractions, date helpers, and a financial calculator.

All financial service controllers are JWT-protected. Investment portfolio and
holding APIs now derive ownership from JWT claims, expose `/me`, reject
mismatched legacy route IDs, and protect system-wide SIP processing. Budget,
Goal listing and object mutations now enforce claim-scoped ownership in the
application handlers. Budget budget/savings-rule operations now use claim-scoped
ownership, including category ownership checks. Loan listing and all identified
loan-ID operations now enforce claim-scoped ownership. Deprecated user-ID routes
remain temporarily for compatible clients but reject claim mismatches.

### Authentication and authorization

JWT access/refresh authentication, route guards, Axios bearer injection/refresh, service authorization attributes, role claims, and admin audit authorization exist. Profile endpoints correctly use the authenticated claim for `/me`.

Gaps:

- Replace or strictly validate client-supplied user IDs.
- Confirm gateway route authorization policy rather than relying only on downstream services.
- Add upload safety/rate-limit policies when documents/imports are exposed.
- Avoid sensitive logging and ensure all cross-record operations include `UserId`.

### Database

SQL Server uses idempotent schema scripts in ordered initialization: schema -> seed -> stored procedures -> views. Schemas are Security, Core, Budget, Investment, Loan, Goals, Analytics, AI, Notifications, Subscriptions, and Import; shared `Views` are also used.

Existing entities/tables:

- Security: Users, Roles, UserRoles, RefreshTokens, AuditLog, PasswordResetTokens.
- Core: AccountTypes, Accounts, Categories, Tags, Transactions, TransactionTags, RecurringSchedules.
- Budget: Budgets, BudgetCategories, BudgetAlerts, SavingsRules.
- Investment: InvestmentTypes, Portfolios, Holdings, Transactions, SIPs, EPFAccounts, EPFContributions, GoldPriceHistory.
- Loan: LoanTypes, Loans, EMISchedule, LoanPrepayments, PrepaymentSimulations.
- Goals: GoalTemplates, Goals, GoalContributions.
- Analytics: NetWorthSnapshots, FinancialScore, MonthlyAggregates.
- AI/Notifications: AIConversations, AIMessages, NotificationTypes, NotificationPreferences, Notifications.
- Subscriptions/Import: DetectedSubscriptions, ImportBatches, ImportErrors.

Existing stored procedures:

- Security: user/profile/password/audit/refresh-token operations.
- Core: account creation/balance, transaction CRUD/split/range/monthly summary, recurring schedule processing, subscription detection.
- Budget: create/update-spent/alerts/budget-vs-actual.
- Investment: holding/price/transaction/SIP/EPF operations, XIRR, portfolio summary, EPF projection.
- Loan: create loan, amortization generation, EMI payment, prepayment simulation/execution, loan summary.
- Goals: create, contribute, projection, progress.
- Analytics: net worth, financial score, monthly aggregates, spending, income-vs-expense, category breakdown.

Existing views:

- Dashboard/account/recent transaction/monthly income-expense/top categories.
- Net worth/financial-score/yearly trends, day-of-week and merchant analysis, goal progress.
- Portfolio/SIP/EPF/asset allocation.
- Active loans/EMI calendar/prepayment history/DTI.
- Budget-vs-actual/alerts/subscription calendar.
- User activity and data quality.

Jobs exist for recurring transactions, daily analytics, monthly processing, execution logging, weekly maintenance, and index maintenance.

### Existing analytics and calculations

**EXISTS**:

- Persisted net-worth snapshots and trends.
- Financial score with savings, DTI, emergency-fund, investment, and goal components.
- Monthly income/expense/savings, category JSON aggregates, spending trends.
- Asset allocation view.
- DTI view.
- Goal projection/progress.
- Loan amortization and prepayment simulation.
- Investment XIRR procedure and portfolio summary.
- EPF projection.
- Shared `FinancialCalculator` with simple/compound interest, EMI, total interest, PV, NPV, IRR, XIRR, SIP-style compounding, percentages, and monetary rounding.

**NEEDS REFACTOR**:

- Shared calculator has inconsistent rate conventions (absolute decimal in most methods versus percentage in one overload).
- XIRR and SIP-style methods convert money to `double`; monetary flows should remain decimal and convergence behavior must be explicit.
- PV truncates fractional years due to integer iteration.
- IRR/XIRR convergence and invalid cash-flow validation need deterministic failure semantics.
- No calculation test project was found.
- Dashboard duplicates net-worth/savings math in Vue.

### AI Assistant

Conversation, message, query-type, feedback, repository, API, Pinia, and page foundations exist. LLM configuration abstraction exists, while local Compose uses placeholders.

The current experience is predominantly chat. Deterministic financial context, advisor opportunities, confidence/data-quality metadata, calculation evidence, deep links, and hallucination guards are **MISSING/PARTIAL**. AI should consume computed analytics DTOs and explain them; it should not calculate core finance values.

### Tests and local development

No backend unit/integration test projects and no frontend test framework/scripts were found. This is a major foundation gap.

Local development supports SQL Server 2022 Developer and optional Redis through Compose, a full-stack Compose definition, service/gateway Dockerfiles, PowerShell start/stop/quick-start/database setup, and optional IIS scripts. Event-bus projects exist, but RabbitMQ is not required by the current Compose flow.

The infra Compose uses environment-required SQL credentials; the older full-stack Compose still contains `CHANGE_ME_*` placeholders and should remain non-secret. Database initialization order is present, though script lists must be updated when new schema families are added.

## 2. Roadmap requirement classification

| Requirement | Status | Reuse / gap |
|---|---|---|
| Financial Command Center | PARTIAL | Dashboard and DB views exist; needs consolidated backend DTO, money flow, assets/liabilities, score, intelligence |
| Dedicated Net Worth | PARTIAL | Snapshots, proc, repository, trend API exist; screen, breakdown, periods, change explanation missing |
| Cash Flow Analytics | PARTIAL | Monthly aggregates/trends exist; ratios, volatility, EMI/investment/free-cash classification and screen missing |
| Financial Health Center | PARTIAL | Five-component score exists; insurance dimension, drill-down explanations and improvement plans missing |
| Tax Center | MISSING | Add versioned Tax-owned data inside an existing appropriate boundary; no hard-coded slabs |
| Protection Center | MISSING | No insurance tables/API/UI |
| Debt Control Center | PARTIAL | Strong loan/EMI/prepayment backend exists; broader summary, rate history, full analysis UI, credit cards missing |
| Loan Strategy Lab | PARTIAL | Prepayment simulation exists; investment/split comparison and explicit assumption model missing |
| Investment Analytics | PARTIAL | Holdings, SIP, EPF, XIRR, allocation exist; target allocation, concentration, realized/unrealized/capital gains UI incomplete |
| Retirement Planner | MISSING | EPF projection reusable; full multi-source retirement calculation missing |
| Advanced Goal Planning | PARTIAL | Projection/progress/contribution exist; prioritization, schedule variance, conflict detection missing |
| Calculators Hub | PARTIAL | Shared formulas exist; hub/UI, broader formulas and tests missing |
| Scenario Lab | MISSING | Loan prepayment simulation is a reusable precedent; isolated cross-domain scenarios missing |
| Reports Center | PARTIAL | SQL views provide several reports; orchestration/filter/export architecture and screens missing |
| Financial Year in Review | PARTIAL | Yearly/monthly analytics exist; synthesis and report page missing |
| Understand This | MISSING | No reusable metric explanation component/metadata contract |
| Data Center | PARTIAL | Import batches/errors and data-quality view exist; APIs/UI/documents/connections/reconciliation missing |
| Ask FinOS | PARTIAL | Chat exists; deterministic context/evidence missing |
| FinOS Advisor | MISSING | Proactive rule output and AI explanation workflow missing |
| Credit Cards | MISSING | Generic accounts may represent them but dedicated limit/statement/payment/liability model is absent |
| Bills & Subscriptions | PARTIAL | recurring schedules, detection proc/table/view exist; user management and rich UI incomplete |
| General Assets | MISSING | Investment holdings are insufficient for property/vehicle/collectible/business assets |
| Grouped navigation | MISSING | Current sidebar is a flat ten-link list |
| Reusable financial UI system | MISSING | Only StatCard is a partial MetricCard equivalent |
| Shared Indian money formatting | NEEDS REFACTOR | Multiple local formatters exist; no single utility |
| Loading/empty/error consistency | NEEDS REFACTOR | Basic empty states exist; standard components and permission/partial-data handling do not |
| Consolidated reporting DTOs | PARTIAL | Trend DTOs exist; command-center/cash-flow/health/report shapes incomplete |
| Redis caching | EXISTS (optional) | Infrastructure exists; use only for safe, bounded derived data if profiling justifies it |

## 3. Proposed implementation mapping

| Capability | Owning boundary | Database reuse/addition | API/frontend direction |
|---|---|---|---|
| Command center, net worth, cash flow, health | Analytics | Reuse snapshots, scores, aggregates, views; extend procedures/views only | Consolidated claim-scoped queries; dashboard/net-worth/cash-flow/health stores and screens |
| Investment analytics/retirement sources | Investment + Analytics orchestration | Reuse portfolios/holdings/SIP/EPF; add target allocation only if absent | Extend summaries; deterministic shared calculations |
| Debt/strategy | Loan + Analytics orchestration | Reuse loans/schedule/prepayments/simulations | Claim-scoped debt summary and comparison DTOs |
| Goals | Goals | Extend existing goal tables only for priority/settings if needed | Projection/conflict queries; enhanced page |
| Calculators | FinOS.Common + thin owning APIs | No persistence for stateless calculations | Tested calculation library and grouped UI |
| Scenario Lab | Analytics as read-model/orchestrator | Add Scenario/Input/Result only if saved scenarios are required | Never mutate source records; before/after result DTO |
| Tax | Prefer a Tax schema owned initially through an existing planning/analytics host unless scale justifies a service | TaxRuleVersion, TaxProfile, projections/deductions/payments/gains only after contract design | Versioned FY/AY APIs; manual/configured inputs first |
| Protection, general assets, cards, subscriptions | CoreFinance unless domain complexity warrants later extraction | Extend Core/Subscriptions; add focused tables without duplicating accounts/holdings | Claim-scoped CRUD and summaries |
| Reports/year review | Analytics | Reuse views/aggregates; report metadata/generated artifacts only if needed | Reusable report query/export contracts |
| Data Center | CoreFinance/Import | Reuse ImportBatches/ImportErrors; add sources/documents/reconciliation | Staged imports, safe upload abstraction, quality dashboard |
| AI Advisor | AI Assistant consuming deterministic read models | Reuse AI tables; persist advisor state only if needed | Evidence-rich recommendation DTO; AI explains/prioritizes deterministic facts |

Cross-service composition should avoid direct domain database writes. The near-term pragmatic approach is to extend Analytics read models over existing shared SQL views while maintaining ownership of writes in each bounded context. A new microservice is not justified for each feature.

## 4. Required database work

Likely scripts, only after each feature's detailed inspection:

1. Extend existing Analytics procedures/views for consolidated command center, net-worth explanation, cash-flow ratios, and health component details.
2. Add calculation-independent configuration tables for versioned tax rules; do not seed invented slabs.
3. Add focused Protection, Asset, CreditCard, Scenario, FinancialDocument/DataSource/Reconciliation tables only where existing models cannot represent the contract.
4. Extend subscription/import models instead of duplicating them.
5. Add indexes for claim-scoped `UserId + date/status` query patterns and retain soft deletes/audit columns.
6. Update PowerShell and both Compose initialization lists in schema -> seed -> procedures -> views order.

All scripts must use existing idempotent guards. Manual/purge scripts must never be executed during implementation.

## 5. Required API work

1. Introduce a reusable authenticated-user accessor and replace/validate `user/{userId}` routes with `/me` contracts.
2. Add consolidated Analytics command-center and purpose-built net-worth/cash-flow/health read DTOs to avoid N+1 dashboard calls.
3. Preserve `ApiResponse<T>`, MediatR, validators, Dapper repositories, decimal money, UTC timestamps, and stored-procedure conventions.
4. Centralize and test financial calculations in `FinOS.Common`; remove formula duplication from handlers and Vue.
5. Add stable time-series, breakdown, explanation/evidence, data-quality, and scenario result DTOs.
6. Keep tax/loan/retirement/scenario calculations deterministic; AI receives results rather than raw authority to invent values.
7. Add safe upload constraints, authorization, validation, and rate limiting before document/import endpoints.

## 6. Required frontend work

1. Create centralized `en-IN` INR full/compact money, percentage, and date utilities.
2. Build shared primitives: MetricCard, TrendIndicator, ChartCard, EmptyState, LoadingState, ErrorState, FinancialMetricExplanation, InsightCard, ScenarioComparison, DateRangeFilter, MoneyDisplay, PercentageDisplay, FinancialStatusBadge.
3. Replace the flat sidebar with grouped, collapsible navigation and progressive disclosure; routes may land on “coming next” modules only after a usable page exists.
4. Refactor dashboard to a consolidated server-backed command-center store.
5. Add dedicated screens in roadmap order, each with loading, empty, partial, failure, permission, and validation states.
6. Keep formulas outside components and avoid duplicating server state across giant stores.
7. Use shared Chart.js options with INR tooltips, accessible colors, responsive sizing, and proper empty states.

## 7. Potential breaking changes and risks

- Claim-scoping user routes may break existing frontend API calls; add `/me` routes first and migrate clients before retiring old routes.
- Existing procedures/views are contracts; column changes must be additive or versioned.
- Financial-score definition changes affect historical comparability. Add score version metadata before changing weights.
- Rate-unit inconsistencies in `FinancialCalculator` can silently alter outputs. Introduce explicit naming/contracts and characterization tests before migration.
- Net-worth classification must reconcile account balances, investments, EPF/gold, general assets, loans, and cards without double counting.
- Investment return calculations need dated cash flows; current XIRR implementation uses floating point and unclear failure behavior.
- Shared-database analytics reads can blur service ownership; limit Analytics to read models and keep writes in owning services.
- Existing mojibake/encoding artifacts are visible in UI/source comments and should be corrected only in touched files, not through broad cleanup.
- Full-stack Compose uses placeholder credentials and certificate configuration; builds can be validated without deploying secrets.
- No existing automated test infrastructure means Phase 1 must add the smallest compatible .NET test project; frontend framework introduction should wait until reusable logic warrants it.

## 8. Recommended implementation order

The requested order is valid with two dependency adjustments:

1. **Phase 1 – Foundation:** preserve baseline, characterize/fix shared calculator contracts, add tests, shared frontend formatters/components, grouped navigation, and documentation maps.
2. **Phase 2 – Core visibility:** claim-scoped consolidated Analytics API; command center; net worth; cash flow; versioned financial health.
3. **Phase 3 – Domain improvements:** investment analytics, debt center, strategy lab, goals, retirement.
4. **Phase 4 – Decision system:** calculator hub, non-mutating scenario lab, deterministic advisor evidence then AI explanations.
5. **Phase 5 – Administration:** versioned tax framework, protection, credit cards, general assets, subscription management.
6. **Phase 6 – Intelligence/reporting:** reports/export, FY review, metric education, Data Center/data quality.

Security route hardening should begin in Phase 1 and be completed opportunistically as each domain is touched. Database features should always be implemented schema -> seed/config -> procedures -> views -> repository -> application -> API/gateway -> frontend.

## 9. Audit conclusion

FinOS already has a credible Personal Finance OS foundation. The best technical approach is an incremental expansion of the nine bounded contexts, with Analytics acting as a consolidated read-model/reporting boundary and `FinOS.Common` hosting deterministic, tested financial math. The highest-value first changes are calculation correctness/tests, claim-based ownership, shared frontend presentation primitives, grouped navigation, and a consolidated command-center API. Creating many new services or replacing Dapper/stored procedures would add risk without improving the current architecture.
