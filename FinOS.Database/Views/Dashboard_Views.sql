-- ============================================================================
-- FinOS Database - Dashboard Views
-- Target: Microsoft SQL Server (SSMS)
-- Description: Views for dashboard display - user summary, accounts, transactions,
--              monthly trends, and top spending categories
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Views
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Views')
    EXEC('CREATE SCHEMA Views');
GO

-- ============================================================================
-- View: Views.vw_UserDashboard
-- Description: Aggregated dashboard metrics for each user — total assets,
--              liabilities, net worth, monthly income/expense, savings rate,
--              financial score, active goals/loans, and investment value
-- ============================================================================
IF OBJECT_ID(N'Views.vw_UserDashboard', N'V') IS NOT NULL
    DROP VIEW Views.vw_UserDashboard;
GO

CREATE VIEW Views.vw_UserDashboard
AS
SELECT
    u.Id                                                                      AS UserId,
    u.FirstName,
    u.LastName,
    u.Currency,

    -- Total Assets: sum of positive balances from accounts included in net worth
    --               plus current value of investment holdings
    ISNULL(asset_bal.TotalAssetBalance, 0)
      + ISNULL(inv_val.TotalInvestmentValue, 0)                               AS TotalAssets,

    -- Total Liabilities: outstanding principal on active loans
    --                     plus negative balances (credit cards, overdrafts)
    ISNULL(loan_bal.TotalOutstanding, 0)
      + ISNULL(neg_bal.TotalNegativeBalance, 0)                               AS TotalLiabilities,

    -- Net Worth = Total Assets - Total Liabilities
    ISNULL(asset_bal.TotalAssetBalance, 0)
      + ISNULL(inv_val.TotalInvestmentValue, 0)
      - ISNULL(loan_bal.TotalOutstanding, 0)
      - ISNULL(neg_bal.TotalNegativeBalance, 0)                               AS NetWorth,

    -- Current month income / expense
    ISNULL(mi.MonthlyIncome, 0)                                               AS MonthlyIncome,
    ISNULL(me.MonthlyExpense, 0)                                              AS MonthlyExpense,

    -- Savings Rate = (Income - Expense) / Income * 100
    CASE
        WHEN ISNULL(mi.MonthlyIncome, 0) = 0 THEN 0
        ELSE CAST(
            ROUND(
                (ISNULL(mi.MonthlyIncome, 0) - ISNULL(me.MonthlyExpense, 0))
                / mi.MonthlyIncome * 100, 2)
            AS DECIMAL(5,2))
    END                                                                        AS SavingsRatePct,

    -- Latest financial score
    fs.OverallScore,
    fs.ScoreGrade,

    -- Active goals and loans
    ISNULL(g_ct.ActiveGoalsCount, 0)                                          AS ActiveGoalsCount,
    ISNULL(l_ct.ActiveLoansCount, 0)                                          AS ActiveLoansCount,

    -- Total investment value (duplicated for convenience)
    ISNULL(inv_val.TotalInvestmentValue, 0)                                   AS InvestmentValue

FROM Security.Users u
LEFT JOIN
(
    -- Positive balances on net-worth-included accounts
    SELECT
        a.UserId,
        SUM(a.Balance) AS TotalAssetBalance
    FROM Core.Accounts a
    INNER JOIN Core.AccountTypes at ON a.AccountTypeId = at.Id
    WHERE a.IsIncludedInNetWorth = 1
      AND a.Balance > 0
      AND a.DeletedAt IS NULL
      AND a.IsActive = 1
    GROUP BY a.UserId
) asset_bal ON asset_bal.UserId = u.Id

LEFT JOIN
(
    -- Negative balances (credit cards, overdrafts)
    SELECT
        a.UserId,
        SUM(ABS(a.Balance)) AS TotalNegativeBalance
    FROM Core.Accounts a
    WHERE a.Balance < 0
      AND a.IsIncludedInNetWorth = 1
      AND a.DeletedAt IS NULL
      AND a.IsActive = 1
    GROUP BY a.UserId
) neg_bal ON neg_bal.UserId = u.Id

LEFT JOIN
(
    -- Outstanding principal on active loans
    SELECT
        l.UserId,
        SUM(l.OutstandingPrincipal) AS TotalOutstanding
    FROM Loan.Loans l
    WHERE l.Status = N'Active'
      AND l.DeletedAt IS NULL
    GROUP BY l.UserId
) loan_bal ON loan_bal.UserId = u.Id

LEFT JOIN
(
    -- Current investment value from holdings
    SELECT
        p.UserId,
        SUM(ISNULL(h.CurrentValue, 0)) AS TotalInvestmentValue
    FROM Investment.Holdings h
    INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
    WHERE h.IsActive = 1
      AND h.DeletedAt IS NULL
      AND p.DeletedAt IS NULL
    GROUP BY p.UserId
) inv_val ON inv_val.UserId = u.Id

LEFT JOIN
(
    -- Current month income (non-transfer)
    SELECT
        t.UserId,
        SUM(t.Amount) AS MonthlyIncome
    FROM Core.Transactions t
    WHERE t.Type = N'Income'
      AND t.DeletedAt IS NULL
      AND t.TransactionDate >= DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
                                       CAST(SYSUTCDATETIME() AS DATE))
      AND t.TransactionDate <  DATEADD(MONTH, 1,
                                       DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
                                               CAST(SYSUTCDATETIME() AS DATE)))
    GROUP BY t.UserId
) mi ON mi.UserId = u.Id

LEFT JOIN
(
    -- Current month expense (non-transfer)
    SELECT
        t.UserId,
        SUM(t.Amount) AS MonthlyExpense
    FROM Core.Transactions t
    WHERE t.Type = N'Expense'
      AND t.DeletedAt IS NULL
      AND t.TransactionDate >= DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
                                       CAST(SYSUTCDATETIME() AS DATE))
      AND t.TransactionDate <  DATEADD(MONTH, 1,
                                       DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
                                               CAST(SYSUTCDATETIME() AS DATE)))
    GROUP BY t.UserId
) me ON me.UserId = u.Id

LEFT JOIN
(
    -- Latest financial score per user
    SELECT
        fs1.UserId,
        fs1.OverallScore,
        fs1.ScoreGrade
    FROM Analytics.FinancialScore fs1
    INNER JOIN
    (
        SELECT UserId, MAX(ScoreDate) AS MaxDate
        FROM Analytics.FinancialScore
        GROUP BY UserId
    ) fs2 ON fs1.UserId = fs2.UserId AND fs1.ScoreDate = fs2.MaxDate
) fs ON fs.UserId = u.Id

LEFT JOIN
(
    -- Active goals count
    SELECT
        g.UserId,
        COUNT(*) AS ActiveGoalsCount
    FROM Goals.Goals g
    WHERE g.Status = N'InProgress'
      AND g.DeletedAt IS NULL
    GROUP BY g.UserId
) g_ct ON g_ct.UserId = u.Id

LEFT JOIN
(
    -- Active loans count
    SELECT
        l.UserId,
        COUNT(*) AS ActiveLoansCount
    FROM Loan.Loans l
    WHERE l.Status = N'Active'
      AND l.DeletedAt IS NULL
    GROUP BY l.UserId
) l_ct ON l_ct.UserId = u.Id

WHERE u.DeletedAt IS NULL
  AND u.IsActive = 1;
GO

PRINT 'View Views.vw_UserDashboard created.';
GO

-- ============================================================================
-- View: Views.vw_AccountSummary
-- Description: All active accounts with type name, balance, institution,
--              and net-worth inclusion flag
--              Note: SCHEMABINDING not used because tables reside in the Core
--              schema (not dbo); schema-binding requires the same owner and
--              two-part naming aligned with the schema.
-- ============================================================================
IF OBJECT_ID(N'Views.vw_AccountSummary', N'V') IS NOT NULL
    DROP VIEW Views.vw_AccountSummary;
GO

CREATE VIEW Views.vw_AccountSummary
AS
SELECT
    a.Id                       AS AccountId,
    a.UserId,
    a.Name                     AS AccountName,
    at.Name                    AS AccountTypeName,
    a.InstitutionName,
    a.AccountNumber,
    a.Balance,
    a.Currency,
    a.CreditLimit,
    a.IsIncludedInNetWorth,
    a.IsSynced,
    a.SyncProvider,
    a.LastSyncedAt,
    a.IsActive,
    a.Color                    AS AccountColor,
    a.Icon                     AS AccountIcon,
    a.Notes,
    a.CreatedAt,
    a.UpdatedAt
FROM Core.Accounts a
INNER JOIN Core.AccountTypes at ON a.AccountTypeId = at.Id
WHERE a.DeletedAt IS NULL;
GO

PRINT 'View Views.vw_AccountSummary created.';
GO

-- ============================================================================
-- View: Views.vw_RecentTransactions
-- Description: Last 50 transactions per user with category name, account name,
--              and concatenated tags for quick display
-- ============================================================================
IF OBJECT_ID(N'Views.vw_RecentTransactions', N'V') IS NOT NULL
    DROP VIEW Views.vw_RecentTransactions;
GO

CREATE VIEW Views.vw_RecentTransactions
AS
SELECT
    t.Id                       AS TransactionId,
    t.UserId,
    t.Type                     AS TransactionType,
    t.Amount,
    t.Currency,
    t.Description,
    t.Notes,
    t.TransactionDate,
    t.TransactionTime,
    t.MerchantName,
    t.MerchantCategory,
    t.IsRecurring,
    t.IsFlagged,
    t.IsSplit,
    t.Source,
    t.IsVerified,
    t.ReferenceNumber,

    -- Account info
    a.Name                     AS AccountName,
    at.Name                    AS AccountTypeName,

    -- Category info
    c.Name                     AS CategoryName,
    c.Type                     AS CategoryType,
    c.Icon                     AS CategoryIcon,
    c.Color                    AS CategoryColor,

    -- Transfer account info
    ta.Name                    AS TransferAccountName,

    -- Tags (comma-separated)
    STUFF(
        (SELECT N', ' + tg.Name
         FROM Core.TransactionTags tt
         INNER JOIN Core.Tags tg ON tt.TagId = tg.Id
         WHERE tt.TransactionId = t.Id
         FOR XML PATH(N''), TYPE).value(N'.[1]', N'NVARCHAR(MAX)'),
        1, 2, N'')            AS Tags,

    t.CreatedAt

FROM Core.Transactions t
INNER JOIN Core.Accounts a     ON t.AccountId = a.Id
INNER JOIN Core.AccountTypes at ON a.AccountTypeId = at.Id
LEFT JOIN Core.Categories c    ON t.CategoryId = c.Id
LEFT JOIN Core.Accounts ta     ON t.TransferAccountId = ta.Id
WHERE t.DeletedAt IS NULL
  AND t.ParentTransactionId IS NULL   -- Exclude split children from main list
  AND a.DeletedAt IS NULL;
GO

PRINT 'View Views.vw_RecentTransactions created.';
GO

-- ============================================================================
-- View: Views.vw_MonthlyIncomeExpense
-- Description: Monthly income vs expense totals for the last 12 months per user
-- ============================================================================
IF OBJECT_ID(N'Views.vw_MonthlyIncomeExpense', N'V') IS NOT NULL
    DROP VIEW Views.vw_MonthlyIncomeExpense;
GO

CREATE VIEW Views.vw_MonthlyIncomeExpense
AS
SELECT
    ma.UserId,
    ma.YearMonth,
    CAST(
        CAST(ma.YearMonth AS VARCHAR(6)) + N'01'
        AS DATE)                AS MonthStartDate,          -- First day of the month
    ma.TotalIncome,
    ma.TotalExpense,
    ma.TotalSavings,
    ma.SavingsRate              AS SavingsRatePct,
    ma.TransactionCount,
    ma.TopExpenseCategory,
    ma.TopExpenseAmount
FROM Analytics.MonthlyAggregates ma
WHERE ma.YearMonth >= CAST(
        FORMAT(DATEADD(MONTH, -11,
              DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
                      CAST(SYSUTCDATETIME() AS DATE))),
              N'yyyyMM') AS INT);
GO

PRINT 'View Views.vw_MonthlyIncomeExpense created.';
GO

-- ============================================================================
-- View: Views.vw_TopSpendingCategories
-- Description: Top 10 expense categories for the current month with amounts
--              and percentage of total spending
-- ============================================================================
IF OBJECT_ID(N'Views.vw_TopSpendingCategories', N'V') IS NOT NULL
    DROP VIEW Views.vw_TopSpendingCategories;
GO

CREATE VIEW Views.vw_TopSpendingCategories
AS
WITH CurrentMonthSpending AS
(
    SELECT
        t.UserId,
        ISNULL(t.CategoryId, 0)   AS CategoryId,
        c.Name                     AS CategoryName,
        c.Icon                     AS CategoryIcon,
        c.Color                    AS CategoryColor,
        SUM(t.Amount)              AS TotalSpent,
        COUNT(*)                   AS TransactionCount
    FROM Core.Transactions t
    LEFT JOIN Core.Categories c   ON t.CategoryId = c.Id
    WHERE t.Type = N'Expense'
      AND t.DeletedAt IS NULL
      AND t.ParentTransactionId IS NULL
      AND t.TransactionDate >= DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
                                       CAST(SYSUTCDATETIME() AS DATE))
      AND t.TransactionDate <  DATEADD(MONTH, 1,
                                       DATEADD(DAY, 1 - DAY(SYSUTCDATETIME()),
                                               CAST(SYSUTCDATETIME() AS DATE)))
    GROUP BY t.UserId, ISNULL(t.CategoryId, 0), c.Name, c.Icon, c.Color
),
MonthlyTotals AS
(
    SELECT
        UserId,
        SUM(TotalSpent) AS GrandTotal
    FROM CurrentMonthSpending
    GROUP BY UserId
),
Ranked AS
(
    SELECT
        cms.UserId,
        cms.CategoryId,
        ISNULL(cms.CategoryName, N'Uncategorized') AS CategoryName,
        cms.CategoryIcon,
        cms.CategoryColor,
        cms.TotalSpent,
        cms.TransactionCount,
        mt.GrandTotal,
        CAST(
            ROUND(
                CASE WHEN mt.GrandTotal = 0 THEN 0
                     ELSE cms.TotalSpent / mt.GrandTotal * 100
                END, 2)
            AS DECIMAL(5,2))    AS PercentageOfTotal,
        ROW_NUMBER() OVER (PARTITION BY cms.UserId ORDER BY cms.TotalSpent DESC)
                                AS Rank
    FROM CurrentMonthSpending cms
    INNER JOIN MonthlyTotals mt ON cms.UserId = mt.UserId
)
SELECT
    UserId,
    CategoryId,
    CategoryName,
    CategoryIcon,
    CategoryColor,
    TotalSpent,
    TransactionCount,
    GrandTotal,
    PercentageOfTotal,
    Rank
FROM Ranked
WHERE Rank <= 10;
GO

PRINT 'View Views.vw_TopSpendingCategories created.';
GO

PRINT '=== Dashboard Views created successfully. ===';
GO
