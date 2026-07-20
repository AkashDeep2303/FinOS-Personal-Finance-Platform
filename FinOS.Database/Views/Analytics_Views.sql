-- ============================================================================
-- FinOS Database - Analytics Views
-- Target: Microsoft SQL Server (SSMS)
-- Description: Views for analytics — net worth trend, financial score history,
--              spending patterns, merchant frequency, goal progress, and
--              yearly comparison
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
-- View: Views.vw_NetWorthTrend
-- Description: Net worth snapshots for the last 12 months with absolute and
--              percentage change from the previous snapshot
-- ============================================================================
IF OBJECT_ID(N'Views.vw_NetWorthTrend', N'V') IS NOT NULL
    DROP VIEW Views.vw_NetWorthTrend;
GO

CREATE VIEW Views.vw_NetWorthTrend
AS
SELECT
    nws.Id                     AS SnapshotId,
    nws.UserId,
    nws.SnapshotDate,

    -- Net worth composition
    nws.NetWorth,
    nws.TotalAssets,
    nws.TotalLiabilities,

    -- Asset breakdown
    nws.CashAndBank,
    nws.InvestmentValue,
    nws.RealEstateValue,
    nws.GoldValue,
    nws.OtherAssets,

    -- Liability breakdown
    nws.LoanOutstanding,
    nws.CreditCardOutstanding,
    nws.OtherLiabilities,

    -- Change from previous snapshot (already stored in table)
    ISNULL(nws.ChangeFromPrevious, 0)    AS ChangeAbsolute,
    ISNULL(nws.ChangePctFromPrevious, 0) AS ChangePct,

    -- Computed: 3-month moving average of net worth
    AVG(nws.NetWorth) OVER (
        PARTITION BY nws.UserId
        ORDER BY nws.SnapshotDate
        ROWS BETWEEN 2 PRECEDING AND CURRENT ROW
    )                                    AS NetWorth3MonthAvg,

    -- Highest and lowest net worth in the window
    MAX(nws.NetWorth) OVER (
        PARTITION BY nws.UserId
        ORDER BY nws.SnapshotDate
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    )                                    AS NetWorthHigh,
    MIN(nws.NetWorth) OVER (
        PARTITION BY nws.UserId
        ORDER BY nws.SnapshotDate
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    )                                    AS NetWorthLow,

    nws.CreatedAt

FROM Analytics.NetWorthSnapshots nws
WHERE nws.SnapshotDate >= DATEADD(MONTH, -12,
      DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
              CAST(SYSUTCDATETIME() AS DATE)));
GO

PRINT 'View Views.vw_NetWorthTrend created.';
GO

-- ============================================================================
-- View: Views.vw_FinancialScoreHistory
-- Description: Financial score history with sub-scores for savings rate,
--              debt-to-income, emergency fund, investment, and goal progress
-- ============================================================================
IF OBJECT_ID(N'Views.vw_FinancialScoreHistory', N'V') IS NOT NULL
    DROP VIEW Views.vw_FinancialScoreHistory;
GO

CREATE VIEW Views.vw_FinancialScoreHistory
AS
SELECT
    fs.Id                      AS ScoreId,
    fs.UserId,
    fs.ScoreDate,

    -- Overall score
    fs.OverallScore,
    fs.ScoreGrade,

    -- Sub-scores (each typically 0-200 for a 1000-point scale)
    fs.SavingsRateScore,
    fs.DebtToIncomeScore,
    fs.EmergencyFundScore,
    fs.InvestmentScore,
    fs.GoalProgressScore,

    -- Corresponding metric values
    fs.SavingsRatePct,
    fs.DebtToIncomeRatio,
    fs.EmergencyFundMonths,
    fs.InvestmentToIncomeRatio,

    -- Monthly financials used for the score
    fs.MonthlyIncome,
    fs.MonthlyExpenses,
    fs.MonthlySavings,
    fs.TotalDebt,
    fs.TotalInvestments,

    -- Change from previous score
    LAG(fs.OverallScore) OVER (
        PARTITION BY fs.UserId ORDER BY fs.ScoreDate
    )                                    AS PreviousOverallScore,

    fs.OverallScore
      - ISNULL(LAG(fs.OverallScore) OVER (
            PARTITION BY fs.UserId ORDER BY fs.ScoreDate
        ), 0)                            AS ScoreChange,

    -- Grade change
    LAG(fs.ScoreGrade) OVER (
        PARTITION BY fs.UserId ORDER BY fs.ScoreDate
    )                                    AS PreviousScoreGrade,

    -- Recommendations (JSON)
    fs.Recommendations,

    fs.CreatedAt

FROM Analytics.FinancialScore fs;
GO

PRINT 'View Views.vw_FinancialScoreHistory created.';
GO

-- ============================================================================
-- View: Views.vw_SpendingByDayOfWeek
-- Description: Average spending by day of week (Mon-Sun) for pattern analysis
--              — helps identify which days the user tends to spend more
-- ============================================================================
IF OBJECT_ID(N'Views.vw_SpendingByDayOfWeek', N'V') IS NOT NULL
    DROP VIEW Views.vw_SpendingByDayOfWeek;
GO

CREATE VIEW Views.vw_SpendingByDayOfWeek
AS
SELECT
    t.UserId,

    -- Day of week: 1=Monday ... 7=Sunday (ISO standard)
    DATEPART(WEEKDAY, t.TransactionDate) AS DayOfWeekNumber,

    -- Day name
    DATENAME(WEEKDAY, t.TransactionDate) AS DayName,

    -- Average spending on this day of week (over the last 6 months)
    CAST(ROUND(AVG(t.Amount), 2) AS DECIMAL(18,2))
                                AS AvgSpending,

    -- Total spending on this day of week
    SUM(t.Amount)               AS TotalSpending,

    -- Number of transactions
    COUNT(*)                    AS TransactionCount,

    -- Max single transaction on this day of week
    MAX(t.Amount)               AS MaxTransaction,

    -- Min single transaction on this day of week
    MIN(t.Amount)               AS MinTransaction,

    -- Median approximation (using PERCENTILE_CONT would be ideal but
    -- not allowed in views; we use a reasonable average as proxy)
    CAST(ROUND(
        SUM(t.Amount) * 1.0 / NULLIF(COUNT(*), 0), 2
    ) AS DECIMAL(18,2))         AS MeanTransactionAmount,

    -- Percentage of total weekly spending
    CAST(ROUND(
        SUM(t.Amount) * 100.0 / NULLIF(
            SUM(SUM(t.Amount)) OVER (PARTITION BY t.UserId), 0
        ), 2
    ) AS DECIMAL(5,2))          AS PctOfWeeklyTotal

FROM Core.Transactions t
WHERE t.Type = N'Expense'
  AND t.DeletedAt IS NULL
  AND t.ParentTransactionId IS NULL
  AND t.TransactionDate >= DATEADD(MONTH, -6, SYSUTCDATETIME())
GROUP BY
    t.UserId,
    DATEPART(WEEKDAY, t.TransactionDate),
    DATENAME(WEEKDAY, t.TransactionDate);
GO

PRINT 'View Views.vw_SpendingByDayOfWeek created.';
GO

-- ============================================================================
-- View: Views.vw_MerchantFrequency
-- Description: Top merchants by transaction count and total amount —
--              identifies the most frequented and most costly merchants
-- ============================================================================
IF OBJECT_ID(N'Views.vw_MerchantFrequency', N'V') IS NOT NULL
    DROP VIEW Views.vw_MerchantFrequency;
GO

CREATE VIEW Views.vw_MerchantFrequency
AS
SELECT
    t.UserId,
    t.MerchantName,
    ISNULL(t.MerchantCategory, N'Uncategorized')
                                AS MerchantCategory,

    -- Frequency metrics
    COUNT(*)                    AS TransactionCount,
    MIN(t.TransactionDate)      AS FirstTransactionDate,
    MAX(t.TransactionDate)      AS LastTransactionDate,

    -- Amount metrics
    SUM(t.Amount)               AS TotalAmount,
    CAST(ROUND(AVG(t.Amount), 2) AS DECIMAL(18,2))
                                AS AvgTransactionAmount,
    MAX(t.Amount)               AS MaxTransactionAmount,
    MIN(t.Amount)               AS MinTransactionAmount,

    -- Category breakdown for this merchant
    ISNULL(c.Name, N'Uncategorized')
                                AS TopCategoryName,

    -- Recency: days since last transaction
    DATEDIFF(DAY, MAX(t.TransactionDate), CAST(SYSUTCDATETIME() AS DATE))
                                AS DaysSinceLastTransaction,

    -- Rank by total amount and by frequency
    RANK() OVER (
        PARTITION BY t.UserId
        ORDER BY SUM(t.Amount) DESC
    )                           AS RankByAmount,

    RANK() OVER (
        PARTITION BY t.UserId
        ORDER BY COUNT(*) DESC
    )                           AS RankByFrequency,

    -- Percentage of total spending
    CAST(ROUND(
        SUM(t.Amount) * 100.0 / NULLIF(
            SUM(SUM(t.Amount)) OVER (PARTITION BY t.UserId), 0
        ), 2
    ) AS DECIMAL(5,2))          AS PctOfTotalSpending

FROM Core.Transactions t
LEFT JOIN Core.Categories c    ON t.CategoryId = c.Id
WHERE t.Type = N'Expense'
  AND t.DeletedAt IS NULL
  AND t.ParentTransactionId IS NULL
  AND t.MerchantName IS NOT NULL
GROUP BY
    t.UserId,
    t.MerchantName,
    t.MerchantCategory,
    ISNULL(c.Name, N'Uncategorized');
GO

PRINT 'View Views.vw_MerchantFrequency created.';
GO

-- ============================================================================
-- View: Views.vw_GoalProgressSummary
-- Description: All active goals with progress percentage, projected
--              completion date, and on-track status
-- ============================================================================
IF OBJECT_ID(N'Views.vw_GoalProgressSummary', N'V') IS NOT NULL
    DROP VIEW Views.vw_GoalProgressSummary;
GO

CREATE VIEW Views.vw_GoalProgressSummary
AS
SELECT
    g.Id                       AS GoalId,
    g.UserId,

    -- Goal identity
    g.Name                     AS GoalName,
    g.Description,
    g.Category,
    gt.Name                    AS TemplateName,
    g.Icon,
    g.Color,

    -- Financial targets
    g.TargetAmount,
    g.CurrentAmount,

    -- Progress
    CAST(
        ROUND(
            CASE WHEN g.TargetAmount = 0 THEN 0
                 ELSE g.CurrentAmount * 100.0 / g.TargetAmount
            END, 2)
        AS DECIMAL(5,2))       AS ProgressPct,

    -- Amount remaining
    g.TargetAmount - g.CurrentAmount
                               AS AmountRemaining,

    -- Timeline
    g.StartDate,
    g.TargetDate,
    g.CompletedDate,

    -- Monthly contribution
    g.MonthlyContribution,
    g.IsAutoContribute,

    -- Priority and status
    g.Priority,
    g.Status,

    -- Projected completion date
    CASE
        WHEN g.MonthlyContribution IS NULL OR g.MonthlyContribution = 0 THEN NULL
        WHEN g.CurrentAmount >= g.TargetAmount THEN g.StartDate  -- Already complete
        ELSE DATEADD(MONTH,
              CEILING(
                  (g.TargetAmount - g.CurrentAmount)
                  / CAST(g.MonthlyContribution AS DECIMAL(18,2))
              ),
              CAST(SYSUTCDATETIME() AS DATE))
    END                        AS ProjectedCompletionDate,

    -- On-track status: is projected date on or before target date?
    CASE
        WHEN g.Status = N'Completed' THEN N'Completed'
        WHEN g.CurrentAmount >= g.TargetAmount THEN N'Completed'
        WHEN g.MonthlyContribution IS NULL OR g.MonthlyContribution = 0
            THEN N'Needs Setup'
        WHEN g.Status = N'Paused' THEN N'Paused'
        WHEN DATEADD(MONTH,
              CEILING(
                  (g.TargetAmount - g.CurrentAmount)
                  / CAST(g.MonthlyContribution AS DECIMAL(18,2))
              ),
              CAST(SYSUTCDATETIME() AS DATE)) <= g.TargetDate
            THEN N'OnTrack'
        ELSE N'Behind'
    END                        AS OnTrackStatus,

    -- Days remaining until target date
    DATEDIFF(DAY, CAST(SYSUTCDATETIME() AS DATE), g.TargetDate)
                               AS DaysToTargetDate,

    -- Months elapsed vs total months
    DATEDIFF(MONTH, g.StartDate, CAST(SYSUTCDATETIME() AS DATE))
                               AS MonthsElapsed,
    DATEDIFF(MONTH, g.StartDate, g.TargetDate)
                               AS TotalMonths,

    -- Time progress vs amount progress (are we keeping pace?)
    CASE
        WHEN DATEDIFF(MONTH, g.StartDate, g.TargetDate) = 0 THEN 0
        ELSE CAST(ROUND(
            DATEDIFF(MONTH, g.StartDate, SYSUTCDATETIME()) * 100.0
            / DATEDIFF(MONTH, g.StartDate, g.TargetDate), 2)
            AS DECIMAL(5,2))
    END                        AS TimeProgressPct,

    -- Total contributions made
    ISNULL(gc_stat.TotalContributions, 0)  AS TotalContributed,
    ISNULL(gc_stat.ContributionCount, 0)   AS ContributionCount

FROM Goals.Goals g
LEFT JOIN Goals.GoalTemplates gt    ON g.GoalTemplateId = gt.Id
OUTER APPLY
(
    -- Aggregate contributions for this goal
    SELECT
        SUM(gcc.Amount)        AS TotalContributions,
        COUNT(*)               AS ContributionCount
    FROM Goals.GoalContributions gcc
    WHERE gcc.GoalId = g.Id
) gc_stat
WHERE g.DeletedAt IS NULL
  AND g.Status IN (N'InProgress', N'Paused');
GO

PRINT 'View Views.vw_GoalProgressSummary created.';
GO

-- ============================================================================
-- View: Views.vw_YearlyComparison
-- Description: Month-by-month income and expense for current year vs
--              previous year — enables year-over-year trend analysis
-- ============================================================================
IF OBJECT_ID(N'Views.vw_YearlyComparison', N'V') IS NOT NULL
    DROP VIEW Views.vw_YearlyComparison;
GO

CREATE VIEW Views.vw_YearlyComparison
AS
WITH CurrentYearData AS
(
    -- Current year monthly aggregates
    SELECT
        ma.UserId,
        ma.YearMonth,
        MONTH(CAST(CAST(ma.YearMonth AS VARCHAR(6)) + N'01' AS DATE))
                                AS MonthNum,
        DATENAME(MONTH, CAST(CAST(ma.YearMonth AS VARCHAR(6)) + N'01' AS DATE))
                                AS MonthName,
        ma.TotalIncome          AS CurrentYearIncome,
        ma.TotalExpense         AS CurrentYearExpense,
        ma.TotalSavings         AS CurrentYearSavings,
        ma.SavingsRate          AS CurrentYearSavingsRate,
        ma.TransactionCount     AS CurrentYearTxCount
    FROM Analytics.MonthlyAggregates ma
    WHERE ma.YearMonth / 100 = YEAR(SYSUTCDATETIME())
),
PreviousYearData AS
(
    -- Previous year monthly aggregates
    SELECT
        ma.UserId,
        ma.YearMonth,
        MONTH(CAST(CAST(ma.YearMonth AS VARCHAR(6)) + N'01' AS DATE))
                                AS MonthNum,
        ma.TotalIncome          AS PreviousYearIncome,
        ma.TotalExpense         AS PreviousYearExpense,
        ma.TotalSavings         AS PreviousYearSavings,
        ma.SavingsRate          AS PreviousYearSavingsRate,
        ma.TransactionCount     AS PreviousYearTxCount
    FROM Analytics.MonthlyAggregates ma
    WHERE ma.YearMonth / 100 = YEAR(SYSUTCDATETIME()) - 1
)
SELECT
    cy.UserId,
    cy.MonthNum,
    cy.MonthName,

    -- Current year
    ISNULL(cy.CurrentYearIncome, 0)      AS CurrentYearIncome,
    ISNULL(cy.CurrentYearExpense, 0)     AS CurrentYearExpense,
    ISNULL(cy.CurrentYearSavings, 0)     AS CurrentYearSavings,
    ISNULL(cy.CurrentYearSavingsRate, 0) AS CurrentYearSavingsRate,
    ISNULL(cy.CurrentYearTxCount, 0)     AS CurrentYearTxCount,

    -- Previous year
    ISNULL(py.PreviousYearIncome, 0)     AS PreviousYearIncome,
    ISNULL(py.PreviousYearExpense, 0)    AS PreviousYearExpense,
    ISNULL(py.PreviousYearSavings, 0)    AS PreviousYearSavings,
    ISNULL(py.PreviousYearSavingsRate, 0)AS PreviousYearSavingsRate,
    ISNULL(py.PreviousYearTxCount, 0)    AS PreviousYearTxCount,

    -- Year-over-year change (absolute)
    ISNULL(cy.CurrentYearIncome, 0)
      - ISNULL(py.PreviousYearIncome, 0) AS IncomeChangeAbsolute,
    ISNULL(cy.CurrentYearExpense, 0)
      - ISNULL(py.PreviousYearExpense, 0)AS ExpenseChangeAbsolute,
    ISNULL(cy.CurrentYearSavings, 0)
      - ISNULL(py.PreviousYearSavings, 0)AS SavingsChangeAbsolute,

    -- Year-over-year change (percentage)
    CASE
        WHEN ISNULL(py.PreviousYearIncome, 0) = 0 THEN NULL
        ELSE CAST(ROUND(
            (ISNULL(cy.CurrentYearIncome, 0) - ISNULL(py.PreviousYearIncome, 0))
            * 100.0 / py.PreviousYearIncome, 2)
            AS DECIMAL(5,2))
    END                                   AS IncomeChangePct,

    CASE
        WHEN ISNULL(py.PreviousYearExpense, 0) = 0 THEN NULL
        ELSE CAST(ROUND(
            (ISNULL(cy.CurrentYearExpense, 0) - ISNULL(py.PreviousYearExpense, 0))
            * 100.0 / py.PreviousYearExpense, 2)
            AS DECIMAL(5,2))
    END                                   AS ExpenseChangePct,

    CASE
        WHEN ISNULL(py.PreviousYearSavings, 0) = 0 THEN NULL
        ELSE CAST(ROUND(
            (ISNULL(cy.CurrentYearSavings, 0) - ISNULL(py.PreviousYearSavings, 0))
            * 100.0 / py.PreviousYearSavings, 2)
            AS DECIMAL(5,2))
    END                                   AS SavingsChangePct

FROM CurrentYearData cy
LEFT JOIN PreviousYearData py
    ON cy.UserId = py.UserId
    AND cy.MonthNum = py.MonthNum;
GO

PRINT 'View Views.vw_YearlyComparison created.';
GO

PRINT '=== Analytics Views created successfully. ===';
GO
