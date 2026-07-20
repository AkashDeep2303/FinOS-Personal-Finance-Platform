-- ============================================================================
-- FinOS Database - Loan Views
-- Target: Microsoft SQL Server (SSMS)
-- Description: Views for loan management — active loans, EMI calendar,
--              prepayment history, and debt-to-income ratio
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
-- View: Views.vw_ActiveLoans
-- Description: All active loans with type, EMI, outstanding, tenure remaining,
--              total interest paid, prepayment info, and progress metrics
-- ============================================================================
IF OBJECT_ID(N'Views.vw_ActiveLoans', N'V') IS NOT NULL
    DROP VIEW Views.vw_ActiveLoans;
GO

CREATE VIEW Views.vw_ActiveLoans
AS
SELECT
    l.Id                       AS LoanId,
    l.UserId,
    lt.Name                    AS LoanTypeName,
    lt.Icon                    AS LoanTypeIcon,

    -- Lender and account info
    l.LenderName,
    l.LoanAccountNumber,
    a.Name                     AS AccountName,

    -- Loan terms
    l.PrincipalAmount,
    l.OutstandingPrincipal,
    l.InterestRate,
    l.InterestType,
    l.TenureMonths,
    l.RemainingTenureMonths,
    l.EMI,
    l.EMIDayOfMonth,
    l.StartDate,
    l.MaturityDate,
    l.DisbursementDate,

    -- Financial summary
    l.ProcessingFee,
    l.TotalInterestPayable,
    l.TotalAmountPayable,
    l.TotalPaid,
    l.TotalInterestPaid,
    l.TotalPrepaid,
    l.Currency,

    -- Prepayment settings
    l.IsPrepaymentAllowed,
    l.PrepaymentPenaltyPct,

    -- Next EMI
    l.NextEMIDate,

    -- Computed metrics
    CAST(
        ROUND(l.OutstandingPrincipal / NULLIF(l.PrincipalAmount, 0) * 100, 2)
        AS DECIMAL(5,2))       AS OutstandingPct,

    CAST(
        ROUND(l.TotalPaid / NULLIF(l.TotalAmountPayable, 0) * 100, 2)
        AS DECIMAL(5,2))       AS PaidPct,

    -- Total interest saved through prepayments
    ISNULL(l.TotalInterestPayable, 0)
      - ISNULL(l.TotalInterestPaid, 0)
      - CASE
            WHEN l.RemainingTenureMonths > 0 AND l.InterestRate > 0
            THEN CAST(ROUND(
                 l.OutstandingPrincipal
                 * (l.InterestRate / 100.0 / 12.0)
                 * l.RemainingTenureMonths, 2) AS DECIMAL(18,2))
            ELSE 0
        END                    AS InterestSavedByPrepayments,

    -- EMI-to-principal ratio for the next EMI
    CASE
        WHEN l.EMI = 0 THEN 0
        ELSE CAST(ROUND(
            l.OutstandingPrincipal * (l.InterestRate / 100.0 / 12.0)
            / l.EMI * 100, 2)
            AS DECIMAL(5,2))
    END                        AS NextEMIInterestPct,

    l.Notes,
    l.CreatedAt,
    l.UpdatedAt

FROM Loan.Loans l
INNER JOIN Loan.LoanTypes lt        ON l.LoanTypeId = lt.Id
INNER JOIN Core.Accounts a          ON l.AccountId = a.Id
WHERE l.Status = N'Active'
  AND l.DeletedAt IS NULL
  AND a.DeletedAt IS NULL;
GO

PRINT 'View Views.vw_ActiveLoans created.';
GO

-- ============================================================================
-- View: Views.vw_EMICalendar
-- Description: Upcoming unpaid EMIs across all active loans with lender,
--              amount, due date, and principal/interest split
-- ============================================================================
IF OBJECT_ID(N'Views.vw_EMICalendar', N'V') IS NOT NULL
    DROP VIEW Views.vw_EMICalendar;
GO

CREATE VIEW Views.vw_EMICalendar
AS
SELECT
    e.Id                       AS EMIScheduleId,
    e.LoanId,
    l.UserId,
    lt.Name                    AS LoanTypeName,
    l.LenderName,
    l.LoanAccountNumber,

    -- EMI details
    e.EMINumber,
    e.EMIDate                  AS DueDate,
    e.EMIAmount,
    e.PrincipalComponent,
    e.InterestComponent,

    -- Outstanding before and after this EMI
    e.OutstandingBefore,
    e.OutstandingAfter,

    -- Late fee if any
    e.LateFee,

    -- Is overdue?
    CASE
        WHEN e.EMIDate < CAST(SYSUTCDATETIME() AS DATE) AND e.IsPaid = 0
            THEN 1
        ELSE 0
    END                        AS IsOverdue,

    -- Days until due / days overdue
    CASE
        WHEN e.IsPaid = 0 AND e.EMIDate >= CAST(SYSUTCDATETIME() AS DATE)
            THEN DATEDIFF(DAY, CAST(SYSUTCDATETIME() AS DATE), e.EMIDate)
        WHEN e.IsPaid = 0 AND e.EMIDate < CAST(SYSUTCDATETIME() AS DATE)
            THEN DATEDIFF(DAY, e.EMIDate, CAST(SYSUTCDATETIME() AS DATE))
        ELSE NULL
    END                        AS DaysOffset,

    e.IsPaid,
    e.PaidDate,
    e.PaidAmount

FROM Loan.EMISchedule e
INNER JOIN Loan.Loans l             ON e.LoanId = l.Id
INNER JOIN Loan.LoanTypes lt        ON l.LoanTypeId = lt.Id
WHERE e.IsPaid = 0
  AND l.Status = N'Active'
  AND l.DeletedAt IS NULL;
GO

PRINT 'View Views.vw_EMICalendar created.';
GO

-- ============================================================================
-- View: Views.vw_LoanPrepaymentHistory
-- Description: Prepayment history with loan context, interest saved,
--              tenure reduction, and new EMI/tenure after prepayment
-- ============================================================================
IF OBJECT_ID(N'Views.vw_LoanPrepaymentHistory', N'V') IS NOT NULL
    DROP VIEW Views.vw_LoanPrepaymentHistory;
GO

CREATE VIEW Views.vw_LoanPrepaymentHistory
AS
SELECT
    lp.Id                      AS PrepaymentId,
    lp.LoanId,
    l.UserId,
    lt.Name                    AS LoanTypeName,
    l.LenderName,
    l.LoanAccountNumber,

    -- Prepayment details
    lp.PrepaymentDate,
    lp.PrepaymentAmount,
    lp.PenaltyAmount,
    lp.PrepaymentType,           -- Partial / Full

    -- Impact on loan
    lp.TenureReduction          AS TenureReducedMonths,
    lp.InterestSaved,
    lp.NewOutstanding,
    lp.NewEMI,
    lp.NewTenureMonths,

    -- Net benefit (interest saved minus penalty)
    lp.InterestSaved - lp.PenaltyAmount
                                AS NetBenefit,

    -- Effective prepayment (total minus penalty)
    lp.PrepaymentAmount - lp.PenaltyAmount
                                AS EffectivePrepayment,

    lp.Notes,
    lp.CreatedAt

FROM Loan.LoanPrepayments lp
INNER JOIN Loan.Loans l             ON lp.LoanId = l.Id
INNER JOIN Loan.LoanTypes lt        ON l.LoanTypeId = lt.Id
WHERE l.DeletedAt IS NULL;
GO

PRINT 'View Views.vw_LoanPrepaymentHistory created.';
GO

-- ============================================================================
-- View: Views.vw_DebtToIncomeRatio
-- Description: Total EMI / Monthly income ratio per user — a key indicator
--              of financial health. Uses latest financial score for income.
-- ============================================================================
IF OBJECT_ID(N'Views.vw_DebtToIncomeRatio', N'V') IS NOT NULL
    DROP VIEW Views.vw_DebtToIncomeRatio;
GO

CREATE VIEW Views.vw_DebtToIncomeRatio
AS
WITH ActiveEMIs AS
(
    -- Total monthly EMI commitment from active loans
    SELECT
        l.UserId,
        SUM(l.EMI)              AS TotalMonthlyEMI,
        COUNT(*)                AS ActiveLoanCount,
        SUM(l.OutstandingPrincipal) AS TotalOutstandingDebt
    FROM Loan.Loans l
    WHERE l.Status = N'Active'
      AND l.DeletedAt IS NULL
    GROUP BY l.UserId
),
LatestIncome AS
(
    -- Latest monthly income from financial score
    SELECT
        fs.UserId,
        fs.MonthlyIncome,
        fs.MonthlyExpenses,
        fs.ScoreDate
    FROM Analytics.FinancialScore fs
    INNER JOIN
    (
        SELECT UserId, MAX(ScoreDate) AS MaxDate
        FROM Analytics.FinancialScore
        GROUP BY UserId
    ) latest ON fs.UserId = latest.UserId AND fs.ScoreDate = latest.MaxDate
),
CalculatedIncome AS
(
    -- Fallback: calculate from recent transactions if no financial score
    SELECT
        monthly.UserId,
        AVG(monthly.TotalIncome) AS AvgMonthlyIncome
    FROM
    (
        SELECT
            t.UserId,
            EOMONTH(t.TransactionDate) AS MonthEnd,
            SUM(CASE WHEN t.Type = N'Income' THEN t.Amount ELSE 0 END)
                                     AS TotalIncome
        FROM Core.Transactions t
        WHERE t.Type IN (N'Income', N'Expense')
          AND t.DeletedAt IS NULL
          AND t.TransactionDate >= DATEADD(MONTH, -6, SYSUTCDATETIME())
        GROUP BY t.UserId, EOMONTH(t.TransactionDate)
    ) monthly
    GROUP BY monthly.UserId
)
SELECT
    u.Id                       AS UserId,
    u.FirstName,
    u.LastName,
    u.Currency,

    -- EMI burden
    ISNULL(emi.TotalMonthlyEMI, 0)          AS TotalMonthlyEMI,
    ISNULL(emi.ActiveLoanCount, 0)          AS ActiveLoanCount,
    ISNULL(emi.TotalOutstandingDebt, 0)     AS TotalOutstandingDebt,

    -- Monthly income (from financial score, fallback to calculated)
    ISNULL(li.MonthlyIncome, ISNULL(ci.AvgMonthlyIncome, 0))
                                            AS MonthlyIncome,

    -- Debt-to-Income Ratio
    CASE
        WHEN ISNULL(li.MonthlyIncome, ISNULL(ci.AvgMonthlyIncome, 0)) = 0 THEN 0
        ELSE CAST(
            ROUND(
                ISNULL(emi.TotalMonthlyEMI, 0)
                / ISNULL(li.MonthlyIncome, ISNULL(ci.AvgMonthlyIncome, 0)) * 100,
                2)
            AS DECIMAL(5,2))
    END                                     AS DebtToIncomeRatioPct,

    -- Risk category (based on common Indian banking thresholds)
    CASE
        WHEN ISNULL(emi.TotalMonthlyEMI, 0) = 0 THEN N'No Debt'
        WHEN ISNULL(li.MonthlyIncome, ISNULL(ci.AvgMonthlyIncome, 0)) = 0 THEN N'Unknown'
        WHEN ISNULL(emi.TotalMonthlyEMI, 0)
             / ISNULL(li.MonthlyIncome, ISNULL(ci.AvgMonthlyIncome, 0)) * 100 <= 30
            THEN N'Healthy'            -- DTI <= 30%: generally comfortable
        WHEN ISNULL(emi.TotalMonthlyEMI, 0)
             / ISNULL(li.MonthlyIncome, ISNULL(ci.AvgMonthlyIncome, 0)) * 100 <= 50
            THEN N'Moderate'           -- DTI 30-50%: manageable but stretched
        ELSE N'High Risk'              -- DTI > 50%: over-leveraged
    END                                     AS RiskCategory,

    -- Available monthly surplus after EMI
    ISNULL(li.MonthlyIncome, ISNULL(ci.AvgMonthlyIncome, 0))
      - ISNULL(emi.TotalMonthlyEMI, 0)     AS MonthlySurplusAfterEMI

FROM Security.Users u
LEFT JOIN ActiveEMIs emi       ON emi.UserId = u.Id
LEFT JOIN LatestIncome li      ON li.UserId = u.Id
LEFT JOIN CalculatedIncome ci  ON ci.UserId = u.Id
WHERE u.DeletedAt IS NULL
  AND u.IsActive = 1;
GO

PRINT 'View Views.vw_DebtToIncomeRatio created.';
GO

PRINT '=== Loan Views created successfully. ===';
GO
