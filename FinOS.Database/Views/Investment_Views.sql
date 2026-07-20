-- ============================================================================
-- FinOS Database - Investment Views
-- Target: Microsoft SQL Server (SSMS)
-- Description: Views for investment tracking — portfolio overview, SIP tracker,
--              EPF statement, and asset allocation
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Views (ensure it exists)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Views')
    EXEC('CREATE SCHEMA Views');
GO

-- ============================================================================
-- View: Views.vw_PortfolioOverview
-- Description: All active holdings with investment type, current value,
--              invested amount, absolute and percentage returns, XIRR, and
--              portfolio/user context
-- ============================================================================
IF OBJECT_ID(N'Views.vw_PortfolioOverview', N'V') IS NOT NULL
    DROP VIEW Views.vw_PortfolioOverview;
GO

CREATE VIEW Views.vw_PortfolioOverview
AS
SELECT
    h.Id                       AS HoldingId,
    p.Id                       AS PortfolioId,
    p.UserId,
    p.Name                     AS PortfolioName,

    -- Investment type details
    it.Name                    AS InvestmentTypeName,
    it.AssetClass,
    it.IsTaxSaving,

    -- Holding identity
    h.Symbol,
    h.Name                     AS HoldingName,
    h.FundHouse,
    h.FundCategory,
    h.RiskLevel,

    -- Quantities and prices
    h.Quantity,
    h.AvgPurchasePrice,
    h.CurrentPrice,
    h.Currency,

    -- Value and returns
    h.InvestedAmount,
    ISNULL(h.CurrentValue, 0)  AS CurrentValue,
    ISNULL(h.TotalReturn, 0)   AS AbsoluteReturn,
    ISNULL(h.TotalReturnPct, 0) AS ReturnPct,

    -- XIRR & CAGR
    ISNULL(h.XIRR, 0)          AS XIRR,
    ISNULL(h.CAGR, 0)          AS CAGR,

    -- Day change
    ISNULL(h.DayChange, 0)     AS DayChange,
    ISNULL(h.DayChangePct, 0)  AS DayChangePct,

    -- Dividends
    h.DividendReceived,

    -- Dates
    h.MaturityDate,
    h.InterestRate             AS FixedDepositRate,
    h.LockInEndDate,
    h.NAVDate,
    h.LastPriceUpdateAt,

    h.Notes,
    h.IsActive,
    h.CreatedAt,
    h.UpdatedAt

FROM Investment.Holdings h
INNER JOIN Investment.Portfolios p   ON h.PortfolioId = p.Id
INNER JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
WHERE h.DeletedAt IS NULL
  AND p.DeletedAt IS NULL
  AND h.IsActive = 1;
GO

PRINT 'View Views.vw_PortfolioOverview created.';
GO

-- ============================================================================
-- View: Views.vw_SIPTracker
-- Description: Active SIPs with fund name, amount, next execution date,
--              total invested, installments done/remaining, and source account
-- ============================================================================
IF OBJECT_ID(N'Views.vw_SIPTracker', N'V') IS NOT NULL
    DROP VIEW Views.vw_SIPTracker;
GO

CREATE VIEW Views.vw_SIPTracker
AS
SELECT
    s.Id                       AS SIPId,
    s.UserId,

    -- Fund / Holding details
    h.Name                     AS FundName,
    h.Symbol,
    it.Name                    AS InvestmentTypeName,
    it.AssetClass,

    -- SIP parameters
    s.Amount                   AS SIPAmount,
    s.Frequency,
    s.DayOfMonth,
    s.StartDate,
    s.EndDate,

    -- Execution tracking
    s.NextExecutionDate,
    s.LastExecutedDate,

    -- Source account
    sa.Name                    AS SourceAccountName,

    -- Progress
    s.TotalInvested,
    s.InstallmentsDone,

    -- Installments remaining calculation
    CASE
        WHEN s.EndDate IS NULL THEN NULL  -- Open-ended SIP
        WHEN s.Frequency = N'Monthly' THEN
            CASE
                WHEN DATEDIFF(MONTH, s.StartDate, s.EndDate) - s.InstallmentsDone < 0
                    THEN 0
                ELSE DATEDIFF(MONTH, s.StartDate, s.EndDate) - s.InstallmentsDone
            END
        WHEN s.Frequency = N'Quarterly' THEN
            CASE
                WHEN DATEDIFF(QUARTER, s.StartDate, s.EndDate) - s.InstallmentsDone < 0
                    THEN 0
                ELSE DATEDIFF(QUARTER, s.StartDate, s.EndDate) - s.InstallmentsDone
            END
        WHEN s.Frequency = N'Weekly' THEN
            CASE
                WHEN DATEDIFF(WEEK, s.StartDate, s.EndDate) - s.InstallmentsDone < 0
                    THEN 0
                ELSE DATEDIFF(WEEK, s.StartDate, s.EndDate) - s.InstallmentsDone
            END
        ELSE NULL
    END                        AS InstallmentsRemaining,

    -- Current value of the linked holding
    ISNULL(h.CurrentValue, 0)  AS CurrentHoldingValue,

    -- Returns on SIP investment
    CASE
        WHEN s.TotalInvested = 0 THEN 0
        ELSE CAST(ROUND(
            (ISNULL(h.CurrentValue, 0) - s.TotalInvested) / s.TotalInvested * 100, 2)
            AS DECIMAL(8,2))
    END                        AS SIPReturnPct,

    s.IsActive,
    s.CreatedAt,
    s.UpdatedAt

FROM Investment.SIPs s
LEFT JOIN Investment.Holdings h     ON s.HoldingId = h.Id
LEFT JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
INNER JOIN Core.Accounts sa         ON s.SourceAccountId = sa.Id
WHERE s.IsActive = 1;
GO

PRINT 'View Views.vw_SIPTracker created.';
GO

-- ============================================================================
-- View: Views.vw_EPFStatement
-- Description: EPF contributions with running balance, employee/employer
--              split, interest earned, and account-level context
-- ============================================================================
IF OBJECT_ID(N'Views.vw_EPFStatement', N'V') IS NOT NULL
    DROP VIEW Views.vw_EPFStatement;
GO

CREATE VIEW Views.vw_EPFStatement
AS
SELECT
    ec.Id                      AS ContributionId,
    ea.Id                      AS EPFAccountId,
    ea.UserId,

    -- EPF Account context
    ea.UAN,
    ea.EstablishmentCode,
    ea.EmployerName,
    ea.InterestRate            AS EPFInterestRate,
    ea.CurrentBalance          AS AccountCurrentBalance,

    -- Contribution period
    ec.Month                   AS ContributionMonth,

    -- Employee and employer breakdown
    ec.EmployeeContribution,
    ea.EmployeeContributionPct AS EmployeeContributionPct,
    ec.EmployerContribution,
    ea.EmployerContributionPct AS EmployerContributionPct,
    ec.EPSContribution,

    -- Total contribution for the month
    ec.EmployeeContribution + ec.EmployerContribution
                               AS TotalContribution,

    -- Interest
    ec.InterestEarned,

    -- Running balance
    ec.OpeningBalance,
    ec.ClosingBalance          AS RunningBalance,

    ec.CreatedAt

FROM Investment.EPFContributions ec
INNER JOIN Investment.EPFAccounts ea ON ec.EPFAccountId = ea.Id
WHERE ea.IsActive = 1;
GO

PRINT 'View Views.vw_EPFStatement created.';
GO

-- ============================================================================
-- View: Views.vw_AssetAllocation
-- Description: Investment value grouped by asset class (Equity, Debt, Gold,
--              RealEstate, Crypto, Mixed) with percentage of total portfolio
-- ============================================================================
IF OBJECT_ID(N'Views.vw_AssetAllocation', N'V') IS NOT NULL
    DROP VIEW Views.vw_AssetAllocation;
GO

CREATE VIEW Views.vw_AssetAllocation
AS
WITH HoldingValues AS
(
    SELECT
        p.UserId,
        it.AssetClass,
        SUM(ISNULL(h.CurrentValue, 0))   AS AssetClassValue,
        SUM(h.InvestedAmount)             AS AssetClassInvested,
        COUNT(*)                          AS HoldingCount
    FROM Investment.Holdings h
    INNER JOIN Investment.Portfolios p    ON h.PortfolioId = p.Id
    INNER JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
    WHERE h.IsActive = 1
      AND h.DeletedAt IS NULL
      AND p.DeletedAt IS NULL
    GROUP BY p.UserId, it.AssetClass
),
PortfolioTotals AS
(
    SELECT
        UserId,
        SUM(AssetClassValue) AS TotalPortfolioValue
    FROM HoldingValues
    GROUP BY UserId
)
SELECT
    hv.UserId,
    hv.AssetClass,
    hv.AssetClassValue,
    hv.AssetClassInvested,
    hv.HoldingCount,
    pt.TotalPortfolioValue,
    CAST(
        ROUND(
            CASE WHEN pt.TotalPortfolioValue = 0 THEN 0
                 ELSE hv.AssetClassValue / pt.TotalPortfolioValue * 100
            END, 2)
        AS DECIMAL(5,2))       AS AllocationPct,

    -- Returns at asset class level
    CAST(
        ROUND(
            CASE WHEN hv.AssetClassInvested = 0 THEN 0
                 ELSE (hv.AssetClassValue - hv.AssetClassInvested)
                      / hv.AssetClassInvested * 100
            END, 2)
        AS DECIMAL(8,2))       AS AssetClassReturnPct

FROM HoldingValues hv
INNER JOIN PortfolioTotals pt ON hv.UserId = pt.UserId;
GO

PRINT 'View Views.vw_AssetAllocation created.';
GO

PRINT '=== Investment Views created successfully. ===';
GO
