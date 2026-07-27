# FinOS Financial Calculations

Canonical implementation: `FinOS.Backend/Shared/FinOS.Common/Helpers/FinancialCalculator.cs`.

## Conventions

- Money is `decimal` and rounded to two decimals with `MidpointRounding.ToEven` at contract boundaries.
- Rates are absolute decimals: `0.10` means 10%, except the clearly documented legacy SIP-style `CompoundInterest` overload.
- Monthly rates are annual rate / 12.
- Invalid negative inputs throw `ArgumentException`.
- Zero-denominator ratios return zero where “not yet measurable” is preferable to an exception.
- UI components format values but do not calculate financial results.

## Implemented

Simple/compound interest, effective annual rate, EMI, total interest, PV, NPV, IRR, XIRR, SIP-style compounding, rounding, percentage change, percentage-of, net worth, savings rate, DTI, emergency-fund coverage, CAGR, and required monthly goal contribution.

Phase 3 adds inflation adjustment, future value with monthly contributions, and fixed-horizon inflation-linked retirement corpus calculations. The authenticated Calculators Hub exposes EMI, SIP, lumpsum, goal contribution, inflation, FD, RD, CAGR, decimal XIRR, emergency-fund target, credit-card payoff, and refinance comparison calculations through this backend layer. FD estimates use quarterly compounding and do not deduct tax; RD estimates use month-end deposits. XIRR requires dated cash flows containing at least one negative investment and one positive redemption or current value.

Credit-card payoff applies monthly interest followed by a fixed month-end
payment and rejects payments that do not exceed first-month interest.
Refinance comparison subtracts the new payment stream and explicit fees from
the existing payment stream; the new rate is an assumption.

## Required hardening

- Normalize the legacy SIP rate convention.
- Add amortization, outstanding principal, prepayment, future/present value annuity, retirement, inflation, allocation, and weighted-rate tests before feature use.

The calculation suite covers normal, zero, invalid, funded-goal, long-duration, decimal XIRR, fractional-year PV, CAGR boundaries, and quarterly-compounding cases.

## Cash-flow ratios and volatility

- `Expense Ratio = Expenses / Income × 100`
- `EMI Ratio = EMI-classified Expenses / Income × 100`
- `Fixed Cost Ratio = (Essential + EMI) / Income × 100`
- `Lifestyle Cost Ratio = Lifestyle Expenses / Income × 100`
- `Investment Rate = Investment-classified Expenses / Income × 100`
- `Average Surplus = Sum(Income - Expenses) / Months in Range`
- `Volatility = Population Standard Deviation / Absolute Monthly Average × 100`

Zero-activity months are included. Ratios return zero when recorded income is
zero rather than dividing by zero.
# Versioned tax projection

Tax estimates are calculated only from an active published rule for the selected
financial year and regime. Rule JSON must explicitly define `slabIncomeTypes`
and `slabs`; it may define `specialIncomeRates`, `deductionLimit`,
`rebateThreshold`, `rebateAmount`, and `cessRatePct`. Recorded income with no
configured treatment is excluded and returned as a warning. The backend persists
the calculation inputs, rule-version reference, result, and warnings in
`Tax.Projections`. Currency values use decimal arithmetic and are rounded to two
places, away from zero.
