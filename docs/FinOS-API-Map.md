# FinOS API Map

| Prefix | Service | Current resources | Planned additive resources |
|---|---|---|---|
| `/api/identity` | Identity | auth, refresh, users, audit | profile/risk/family extensions |
| `/api/corefinance` | CoreFinance | accounts, categories, transactions, recurring schedules, subscriptions | assets, credit cards, imports/documents |
| `/api/budget` | Budget | budgets, alerts, savings rules | intelligence summaries |
| `/api/investment` | Investment | portfolios, holdings, SIP, EPF | target allocation, concentration, performance |
| `/api/loan` | Loan | loans, EMI schedule, prepayment | debt summary, strategy comparisons, rate history |
| `/api/goals` | Goals | goals, templates, contributions, progress | conflict detection, priority, schedule variance |
| `/api/analytics` | Analytics | aggregates, spending, net worth, score | command center, cash flow, health details, scenarios, reports |
| `/api/aiassistant` | AI Assistant | conversations, messages, feedback | evidence-backed advisor opportunities |
| `/api/notification` | Notification | notifications, types, preferences | renewal/financial-event delivery |

## Contract rules

- Preserve `ApiResponse<T>`.
- Prefer `/me` and JWT-derived user ownership.
- Use additive DTO changes or version new contracts.
- Use consolidated read APIs for dashboards and reports.
- Return decimal monetary values and UTC timestamps.
- Include calculation evidence, assumptions, and data-quality metadata for recommendations.

The authenticated decision-tools calculator contract supports deterministic
EMI, SIP, lumpsum, goal, inflation, FD, RD, CAGR, emergency-fund,
credit-card-payoff, and refinance calculations. XIRR uses its dated-cash-flow
contract.

## Core Finance category contracts

- `GET /api/corefinance/categories`: claim-scoped custom categories plus shared system taxonomy, including cash-flow classification.
- `POST /api/corefinance/categories`: creates a validated user-owned category.
- `PUT /api/corefinance/categories/{id}`: updates only a user-owned category, including its cash-flow classification; system categories remain immutable.

## Tax foundation contracts

- `GET/PUT /api/corefinance/tax/profiles/{financialYear}`: claim-scoped tax input profile storage; inputs remain configuration data and are not themselves a tax calculation.
- `GET /api/corefinance/tax/rules/{financialYear}`: published rule-version metadata only. Empty means calculation must remain unavailable.
- `POST /api/corefinance/tax/admin/rules`: Admin/SuperAdmin-only creation of an unpublished, structurally validated FY/AY and regime-specific rule version.
- `POST /api/corefinance/tax/admin/rules/{id}/publish`: Admin/SuperAdmin-only atomic publication; other published versions for the same FY and regime are retired.

## Implemented Phase 2 contracts

- `GET /api/analytics/command-center` returns consolidated metrics, category-configured money flow (essential, lifestyle, EMI, investment, and other), assets/liabilities, financial health, deterministic insights, and data completeness.
  Insight rules include their figures, calculation evidence, affected area,
  educational next action, and deep link; they do not invoke an LLM.
- `GET /api/analytics/cash-flow`: claim-scoped historical classified cash flow with bounded month/custom ranges, ratios, average/latest surplus, and coefficient-of-variation metrics.
- Analytics net-worth, monthly-aggregate, score, and spending endpoints now derive `UserId` from the authenticated JWT. Query/body user IDs are no longer trusted.

## Implemented Phase 3 contracts

- `POST /api/analytics/retirement/project`: validated, non-persistent retirement projection.
- `GET /api/loan/debt/overview`: claim-scoped debt, EMI, DTI, weighted rate, and debt-free-date summary.
- `POST /api/loan/loan-strategy/compare`: claim-scoped, non-mutating prepay/invest/split comparison.
- `GET /api/goals/goal-planning/funding-analysis`: claim-scoped goal funding requirements and conflict analysis.
- Portfolio summaries now resolve real investment-type metadata and expose concentration metrics.
- `POST /api/investment/allocation/analyze`: claim-scoped target-versus-actual allocation deviation analysis; it never executes trades.
- `GET /api/investment/allocation/{portfolioId}/targets`: load persisted targets for an owned portfolio.
- `PUT /api/investment/allocation/{portfolioId}/targets`: transactionally replace persisted targets; percentages must total 100%.
- `GET /api/investment/allocation/{portfolioId}/performance`: claim-scoped unrealized/realized gains, income, charges, completeness, transaction trend, and persisted daily portfolio-value history.
- `GET /api/loan/debt/loans/{loanId}/rate-history`: claim-scoped interest-rate history.
- `POST /api/loan/debt/loans/{loanId}/rate-history`: record a validated rate change and update the current loan rate.
- `GET /api/loan/debt/loans/{loanId}/payment-analysis`: schedule-derived principal, interest, late-payment, and remaining-interest analysis.
- EMI schedule, schedule generation, payment recording, prepayment simulation/execution, and prepayment-history contracts now enforce JWT-derived loan ownership before reading or mutating data.
- `GET /api/loan/prepayment/loan/{loanId}/history`: claim-scoped executed-prepayment history with interest and tenure impact.

## Implemented Phase 4 contracts

- `POST /api/analytics/decision-tools/calculate`: authenticated deterministic EMI, SIP, lumpsum, goal contribution, inflation, FD, RD, and CAGR calculations with explicit result units and assumptions.
- `POST /api/analytics/decision-tools/xirr`: authenticated decimal XIRR for 2-200 dated cash flows, requiring investments and inflows on at least two dates.
- `POST /api/analytics/decision-tools/scenario`: authenticated, stateless scenario comparison that never mutates financial records.
- `GET/POST /api/analytics/decision-tools/scenarios`: claim-scoped saved hypothetical inputs and deterministic result snapshots.
- `DELETE /api/analytics/decision-tools/scenarios/{id}`: claim-scoped soft deletion; saved scenarios never write into operational finance tables.

## Implemented Phase 6 contracts

- `GET /api/analytics/reports/financial-year-review?startYear=YYYY`: claim-scoped FY income, expense, savings, net-worth growth, top spending, and deterministic win/weakness synthesis.
- `GET /api/corefinance/data-center/overview`: claim-scoped import summaries and deterministic data-quality findings. It returns aggregate import-error counts but never raw imported row data.
- `GET/POST /api/corefinance/data-center/documents`: list or record claim-scoped financial-document metadata. The contract does not accept binary content or storage URLs.
- `DELETE /api/corefinance/data-center/documents/{id}`: claim-scoped soft deletion of document metadata.
- `GET /api/corefinance/data-center/reconciliation-issues`: sanitized, claim-scoped import failures; `RawData` is never returned.
- `POST /api/corefinance/data-center/reconciliation-issues/{id}/resolve`: atomically acknowledges an owned import failure and optionally links an owned corrective transaction.
- `GET/POST /api/corefinance/data-center/sources`: list or register claim-scoped, manual-import data sources without credentials.
- `DELETE /api/corefinance/data-center/sources/{id}`: claim-scoped soft deletion of a source profile.
- `POST /api/corefinance/data-center/imports/csv/preview`: authenticated, stateless multipart CSV validation and preview. Uploaded bytes are neither persisted nor used to create transactions.
- `POST /api/corefinance/data-center/imports/csv/mapping/validate`: authenticated deterministic validation for date, description, amount or debit/credit, reference, and transaction-type column mappings.
- `POST /api/corefinance/data-center/imports/csv/transactions/validate`: authenticated, non-persistent row normalization with explicit amount-sign convention and sanitized row-numbered errors.
- `POST /api/corefinance/data-center/imports/csv/duplicates/check`: claim-scoped duplicate analysis against an owned destination account and repeated rows within the uploaded file; no transaction mutation occurs.
- `POST /api/corefinance/data-center/imports/csv/confirm`: atomically persists an import batch and normalized transactions, rechecks duplicates, applies the selected Skip/Include policy, updates the owned account balance, and writes an audit entry.
- `GET/POST/DELETE /api/corefinance/data-center/documents`: claim-scoped financial-document metadata catalog.
- `POST /api/corefinance/data-center/documents/{id}/file`: attaches one bounded, allow-listed, signature-validated private file to an owned document.
- `GET /api/corefinance/data-center/documents/{id}/file`: streams an owned document through authenticated CoreFinance; storage keys are never exposed.
# Tax

- `GET /api/corefinance/tax/profiles/{financialYear}` — user-scoped tax inputs.
- `PUT /api/corefinance/tax/profiles/{financialYear}` — save non-negative inputs.
- `GET /api/corefinance/tax/rules/{financialYear}` — published rule metadata.
- `POST /api/corefinance/tax/projections/{financialYear}/calculate` — calculate
  and persist deterministic Old/New estimates from published configurations.
- `POST /api/corefinance/tax/admin/rules` — create a rule version (Admin).
- `POST /api/corefinance/tax/admin/rules/{id}/publish` — publish a rule (Admin).

# Investment ownership

- `GET /api/investment/portfolios/me` — authenticated user's portfolios.
- `GET /api/investment/portfolios/user/{userId}` — deprecated compatibility
  route; rejects a route user different from the authenticated claim.
- Portfolio summaries and all holding reads/writes verify the portfolio owner
  before dispatching the operation.
- `POST /api/investment/sips/process` is restricted to Admin/SuperAdmin because
  it processes system-wide scheduled installments.

# Goal ownership

- `GET /api/goals/goals/me` — authenticated user's goals.
- `GET /api/goals/goals/user/{userId}` — deprecated compatibility route;
  rejects a user ID different from the authenticated claim.
- Goal progress, update, delete, contribution, pause, and resume requests carry
  the authenticated user into MediatR and enforce ownership in the handler.
- The contribution route goal ID overrides any goal ID supplied in the body.

# Budget ownership

- `GET /api/budget/budgets/me` — authenticated user's budgets.
- `GET /api/budget/savingsrules/me` — authenticated user's savings rules.
- Legacy user-ID list routes are deprecated and reject claim mismatches.
- Budget details, comparisons, alerts, updates, spent updates, alert checks, and
  deletion enforce ownership inside MediatR handlers.
- Savings-rule reads, creation, and updates derive or validate ownership from
  the authenticated claim.

# Loan ownership

- `GET /api/loan/loans/me` — authenticated user's loans.
- The deprecated user-ID route rejects a claim mismatch.
- Loan summaries, closure, schedules, upcoming EMIs, payment recording,
  schedule generation, simulations, prepayments, history, rate changes,
  payment analysis, and strategy comparisons enforce ownership in handlers.
- Cross-user and nonexistent loan IDs share the same not-found behavior.

# Identity authentication

- `POST /api/identity/login` returns `twoFactorRequired: true` without
  tokens after valid primary credentials when TOTP is enabled.
- Repeating login with the same credentials and a valid six-digit authenticator
  code completes authentication.
- TOTP uses a 30-second RFC 6238 step and accepts only the adjacent clock window.
- `POST /api/identity/refresh-token` rotates refresh tokens atomically.
- `POST /api/identity/logout` idempotently revokes the authenticated user's
  supplied refresh token; tokens belonging to another user are never modified.
- `GET /api/identity/sessions` lists only the authenticated user's active
  refresh sessions and identifies the session associated with the access JWT.
- `DELETE /api/identity/sessions/{sessionId}` revokes an owned non-current
  session and is idempotent for missing or unauthorized identifiers.
- `POST /api/identity/sessions/revoke-others` revokes all owned active sessions
  except the session associated with the current access JWT.
