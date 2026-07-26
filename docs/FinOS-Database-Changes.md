# FinOS Database Changes

## Scenario Lab persistence

- Added idempotent `Analytics.Scenarios` in `007_Goals_Analytics_Schema.sql`.
- Stores user-owned names, scenario types, verdicts, and JSON snapshots of hypothetical inputs/results.
- JSON constraints reject malformed snapshots, an active-record index supports recent lists, and soft deletion preserves recoverability.
- This table has no foreign keys to accounts, transactions, loans, investments, or goals, preventing simulations from mutating real financial data.

## Tax Center foundation

- Added idempotent `Tax.RuleVersions`, `Tax.Profiles`, and `Tax.Projections` to the Core Finance schema script.
- Rule configuration is financial-year, assessment-year, regime, and version specific and stored as validated JSON.
- No tax slabs or deductions are seeded. A projection must reference the exact published rule version used.
- User profiles and projections enforce `UserId` ownership, decimal money, UTC audit timestamps, and recoverable profile deletion.

## Protection Center

- Added idempotent `Core.InsurancePolicies` with life, health, vehicle, property, and other policy types.
- Policies are user-owned, use decimal coverage/premium values, support renewal dates, and use soft deletion.

## Credit cards

- Added idempotent one-to-one `Core.CreditCardDetails` after `Core.Accounts` initialization.
- Reuses existing CreditCard accounts, balances, limits, and transactions; adds statement/due days, amounts due, interest rate, and last-payment metadata without duplicating accounts.

## General assets

- Added idempotent `Core.Assets` for property, vehicles, gold, collectibles, business assets, and other non-investment assets.
- Current value and valuation date are required; optional associated loan IDs are retained without creating a cross-script foreign-key dependency.
- `Analytics.sp_CalculateNetWorth` now adds registered property to real estate and other registered assets to other assets.

## Phase 1

No database objects changed.

The audit confirmed reusable Analytics snapshots/scores/aggregates, subscription detection, import batches/errors, loan simulations, and reporting views. Future work must extend these rather than duplicate them.

## Planned order

Every database capability will be applied in this order:

1. Idempotent schema/table/index changes.
2. Reference/configuration seed data.
3. Stored procedures.
4. Views/read models.
5. Optional jobs after recurring-impact review.

Potential future additions remain provisional until feature-level inspection: versioned tax configuration, protection policies, general assets, credit-card details, saved scenarios, documents/data sources, and reconciliation issues.

Purge, reset, migration, and manual scripts are not part of automated implementation or validation.

## Phase 2 command-center slice

No database objects changed. The consolidated query reuses:

- `Analytics.NetWorthSnapshots`
- `Analytics.MonthlyAggregates`
- `Analytics.FinancialScore`

Missing essential/lifestyle, EMI, and investment-flow classification is surfaced as incomplete data rather than inferred or stored inaccurately.

## Phase 3

No schema migration was required. Phase 3 reuses:

- `Views.vw_DebtToIncomeRatio`
- `Loan.Loans` and existing prepayment simulation procedure
- `Goals.Goals`
- `Investment.InvestmentTypes`, portfolios, and holdings

Retirement and strategy comparisons are intentionally non-persistent.

## Investment target allocation

Add `Investment.PortfolioTargetAllocations` after `Investment.Portfolios` and before application use.
The idempotent table enforces one target per portfolio/asset class, percentages from 0–100, and
cascade deletion with its owning portfolio. Target replacement is transactional and portfolio
ownership is enforced by the API/application layers.

`Investment.Transactions` gains nullable `CostBasis` and `RealizedGain` columns. The existing
record-transaction procedure fills both at sell time using the holding's pre-sale weighted cost.
Older sell rows remain null and are reported as incomplete rather than estimated.

`Investment.PortfolioValueSnapshots` stores one invested/current/unrealized value point per portfolio
and UTC date. `Investment.sp_CapturePortfolioValueSnapshots` idempotently upserts the daily point,
and the existing Daily Analytics job now runs it as step 5.

## Loan rate history

Add `Loan.LoanInterestRateHistory` after `Loan.Loans`. It records the previous and new rate,
effective date, reason, and UTC creation time. Adding a rate change and updating the loan's current
rate occur in one SQL transaction.

## Data Center reuse

- The Data Center overview introduced no duplicate import or quality tables.
- `Import.ImportBatches`, `Import.ImportErrors`, and `Views.vw_DataQuality` are reused as existing contracts.
- Raw `Import.ImportErrors.RawData` is intentionally excluded from the API.
- Added an idempotent import-error resolution index. A resolved-transaction foreign key is added only when legacy data has no orphaned references, preserving initialization safety for existing databases.

## Financial document catalog

- Added idempotent `Core.FinancialDocuments` for user-owned document metadata and optional private-storage references.
- Supports bank, broker, mutual-fund, salary, Form 16, loan, EPF, insurance, tax, and other classifications.
- Stores no binary content or extracted sensitive contents. Opaque storage keys,
  original names, MIME types, byte lengths, and SHA-256 hashes support the
  provider-neutral private storage abstraction.
- Uses UTC audit timestamps, a constrained status, indexed active records, and recoverable soft deletion.

## Data source registry

- Added idempotent `Core.DataSources` for user-owned manual-import source profiles.
- Supports bank, broker, mutual fund, salary, tax, loan, EPF, and other classifications.
- Connection mode is constrained to `ManualImport`; no credentials, access tokens, account numbers, or provider secrets are stored.
- Uses UTC timestamps, indexed active records, and recoverable soft deletion.

CSV preview, mapping, and normalized transaction-row validation introduce no database changes. Uploaded bytes are validated in memory and discarded without creating an import batch or financial transaction.

Duplicate analysis also introduces no schema changes. Candidate rows are passed as bounded JSON to one parameterized, account-scoped SQL query; no candidate data is persisted.

`Core.sp_ImportTransactions` performs authoritative duplicate detection, including repeated rows within the uploaded file, and atomically writes the import batch, accepted transactions, one net account-balance adjustment, and an audit record. `Skip` and `Include` are the only accepted duplicate policies.

Import hardening adds an idempotent filtered index on
`Core.Transactions(UserId, AccountId, ReferenceNumber)` for reference-based
duplicate checks. An idempotent `Core.Transactions.ImportBatchId` foreign key
is also added when the existing database contains no orphaned legacy batch
references; otherwise deployment leaves the data untouched for explicit
reconciliation.

## Cash-flow classification

- `Core.Categories.CashFlowClassification` adds the constrained values
  `Essential`, `Lifestyle`, `EMI`, `Investment`, and `Other`.
- Existing and custom categories default to `Other`, preventing guessed
  classifications.
- Reference-data updates assign conservative classifications to existing
  system expense categories and propagate them to system subcategories.
- The Command Center aggregates only claim-scoped, active Core transactions
  and treats uncategorized expenses as `Other`.

## Tax rule administration

No new tax tables were introduced. Administration reuses
`Tax.RuleVersions`. Draft configurations require contiguous slabs beginning
at zero, rates from 0% through 100%, a following assessment year, a valid
Old/New regime, and coherent effective dates. Publishing is transactional and
leaves at most one published version per financial year and regime.

No slab amounts or rates are seeded by FinOS.
# Tax projection usage

No additional table is required for deterministic regime comparison. The
existing `Tax.Projections` table now receives one immutable calculation record
per available published regime, referencing both the user profile and exact rule
version. Apply the existing schema scripts before using this endpoint.

# Budget category ownership

Reapply `FinOS.Database/StoredProcedures/Budget_sp.sql` after the schema scripts.
`Budget.sp_CreateBudget` now rejects category IDs that are neither active system
categories nor categories owned by the budget user. Category replacement applies
the same validation transactionally before deleting existing allocations.
