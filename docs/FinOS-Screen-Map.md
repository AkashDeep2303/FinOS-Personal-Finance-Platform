# FinOS Screen Map

- `/tax`: Tax Center with financial-year selection; Overview, Income, Deductions, Capital Gains, TDS, Tax Regime, and Documents tabs; published-rule readiness prevents unsupported estimates.
- `/protection`: recorded insurance coverage, annualized premium summary, renewals, and policy management without product recommendations.
- `/credit-cards`: outstanding, available credit, utilization, statement metadata, and payment-due details for existing CreditCard accounts.
- `/assets`: non-investment asset registry with current valuation, valuation date, and optional loan association.
- `/reports`: grouped Reports Center with Financial Year in Review and CSV export.
- `/data-center`: Connections, Imports, Documents, and Data Quality tabs. Supports source profiles, CSV preview/mapping/row validation, owned-account duplicate analysis, sanitized reconciliation, and document metadata; transaction persistence, live connections, and binary uploads remain unavailable.

Shared education disclosures currently cover net worth, savings rate, financial-health score, and debt-to-income. The component remains reusable for XIRR, CAGR, emergency fund, allocation, retirement, tax, liquidity, and utilization screens.

| Group | Screen | Route | Status |
|---|---|---|---|
| Home | Financial Command Center | `/dashboard` | DONE |
| Money | Accounts | `/accounts` | DONE |
| Money | Transactions | `/transactions` | DONE |
| Money | Categories | `/categories` | DONE |
| Money | Budget | `/budgets` | DONE |
| Money | Bills & Subscriptions | `/subscriptions` | DONE |
| Wealth | Net Worth | `/net-worth` | DONE |
| Wealth | Investments | `/investments` | PARTIAL |
| Wealth | EPF / PPF / NPS | investment subviews | PARTIAL |
| Wealth | Other Assets | `/assets` | DONE |
| Debt | Loans | `/loans` | PARTIAL |
| Debt | Credit Cards | `/credit-cards` | DONE |
| Debt | Debt Strategy | `/loan-strategy` | DONE |
| Plan | Goals | `/goals` | PARTIAL |
| Plan | Goal Funding | `/goal-planning` | DONE |
| Plan | Retirement | `/retirement` | DONE |
| Plan | Tax | `/tax` | PARTIAL |
| Plan | Protection | `/protection` | DONE |
| Insights | Analytics | `/analytics` | PARTIAL |
| Insights | Financial Health | `/financial-health` | DONE |
| Insights | Forecasts / Reports | `/reports` | PARTIAL |
| Tools | Calculators / Scenario Lab | `/calculators`, `/scenario-lab` | PARTIAL |
| Ask FinOS | AI Assistant | `/ai-assistant` | PARTIAL |
| Ask FinOS | Advisor | `/advisor` | DONE |
| Data | Imports / Connections / Documents / Quality | `/data-center` | PARTIAL |
| Settings | Profile / Preferences | `/settings` | PARTIAL |

Phase 1 groups existing working routes in the sidebar. Routes for unfinished screens are intentionally not exposed.
