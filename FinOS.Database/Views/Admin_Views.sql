-- ============================================================================
-- FinOS Database - Admin Views
-- Target: Microsoft SQL Server (SSMS)
-- Description: Views for admin/ops — user activity, data quality checks,
--              and system metrics
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
-- View: Views.vw_UserActivity
-- Description: User login activity, transaction counts, last active date,
--              and engagement metrics for admin monitoring
-- ============================================================================
IF OBJECT_ID(N'Views.vw_UserActivity', N'V') IS NOT NULL
    DROP VIEW Views.vw_UserActivity;
GO

CREATE VIEW Views.vw_UserActivity
AS
SELECT
    u.Id                       AS UserId,
    u.Email,
    u.FirstName,
    u.LastName,
    u.IsActive,
    u.EmailVerified,
    u.PhoneVerified,
    u.TwoFactorEnabled,
    u.Currency,
    u.TimeZone,
    u.CreatedAt                AS AccountCreatedDate,

    -- Last login
    u.LastLoginAt,

    -- Days since last login
    CASE
        WHEN u.LastLoginAt IS NULL THEN NULL
        ELSE DATEDIFF(DAY, u.LastLoginAt, SYSUTCDATETIME())
    END                        AS DaysSinceLastLogin,

    -- Account age in days
    DATEDIFF(DAY, u.CreatedAt, SYSUTCDATETIME())
                               AS AccountAgeDays,

    -- Login count (from audit log)
    ISNULL(login_ct.LoginCount, 0)
                               AS TotalLogins,

    -- Last login IP
    last_login.IpAddress       AS LastLoginIp,

    -- Last login user agent
    last_login.UserAgent       AS LastLoginUserAgent,

    -- Transaction stats
    ISNULL(tx_ct.TotalTransactionCount, 0)
                               AS TotalTransactions,
    ISNULL(tx_ct.ExpenseCount, 0)
                               AS ExpenseTransactions,
    ISNULL(tx_ct.IncomeCount, 0)
                               AS IncomeTransactions,
    ISNULL(tx_ct.LastTransactionDate, NULL)
                               AS LastTransactionDate,

    -- Days since last transaction
    CASE
        WHEN tx_ct.LastTransactionDate IS NULL THEN NULL
        ELSE DATEDIFF(DAY, tx_ct.LastTransactionDate, SYSUTCDATETIME())
    END                        AS DaysSinceLastTransaction,

    -- Account count
    ISNULL(ac_ct.AccountCount, 0)
                               AS ActiveAccountCount,

    -- Budget count
    ISNULL(b_ct.ActiveBudgetCount, 0)
                               AS ActiveBudgetCount,

    -- Goal count
    ISNULL(g_ct.ActiveGoalCount, 0)
                               AS ActiveGoalCount,

    -- AI conversation count
    ISNULL(ai_ct.ConversationCount, 0)
                               AS AIConversationCount,

    -- User engagement status
    CASE
        WHEN u.IsActive = 0 THEN N'Deactivated'
        WHEN u.LastLoginAt IS NULL THEN N'NeverLoggedIn'
        WHEN DATEDIFF(DAY, u.LastLoginAt, SYSUTCDATETIME()) > 90 THEN N'Dormant'
        WHEN DATEDIFF(DAY, u.LastLoginAt, SYSUTCDATETIME()) > 30 THEN N'Inactive'
        WHEN DATEDIFF(DAY, u.LastLoginAt, SYSUTCDATETIME()) > 7 THEN N'Occasional'
        ELSE N'Active'
    END                        AS EngagementStatus,

    -- Role
    ISNULL(r.RoleNames, N'NoRole')
                               AS Roles

FROM Security.Users u

-- Login count from audit log
LEFT JOIN
(
    SELECT
        UserId,
        COUNT(*)               AS LoginCount
    FROM Security.AuditLog
    WHERE ActionType = N'LOGIN'
    GROUP BY UserId
) login_ct ON login_ct.UserId = u.Id

-- Last login details
OUTER APPLY
(
    SELECT TOP 1
        al.IpAddress,
        al.UserAgent
    FROM Security.AuditLog al
    WHERE al.UserId = u.Id
      AND al.ActionType = N'LOGIN'
    ORDER BY al.CreatedAt DESC
) last_login

-- Transaction statistics
LEFT JOIN
(
    SELECT
        t.UserId,
        COUNT(*)               AS TotalTransactionCount,
        SUM(CASE WHEN t.Type = N'Expense' THEN 1 ELSE 0 END) AS ExpenseCount,
        SUM(CASE WHEN t.Type = N'Income' THEN 1 ELSE 0 END)  AS IncomeCount,
        MAX(t.TransactionDate) AS LastTransactionDate
    FROM Core.Transactions t
    WHERE t.DeletedAt IS NULL
      AND t.ParentTransactionId IS NULL
    GROUP BY t.UserId
) tx_ct ON tx_ct.UserId = u.Id

-- Active accounts count
LEFT JOIN
(
    SELECT
        a.UserId,
        COUNT(*)               AS AccountCount
    FROM Core.Accounts a
    WHERE a.DeletedAt IS NULL
      AND a.IsActive = 1
    GROUP BY a.UserId
) ac_ct ON ac_ct.UserId = u.Id

-- Active budgets count
LEFT JOIN
(
    SELECT
        b.UserId,
        COUNT(*)               AS ActiveBudgetCount
    FROM Budget.Budgets b
    WHERE b.DeletedAt IS NULL
      AND b.IsActive = 1
    GROUP BY b.UserId
) b_ct ON b_ct.UserId = u.Id

-- Active goals count
LEFT JOIN
(
    SELECT
        g.UserId,
        COUNT(*)               AS ActiveGoalCount
    FROM Goals.Goals g
    WHERE g.DeletedAt IS NULL
      AND g.Status = N'InProgress'
    GROUP BY g.UserId
) g_ct ON g_ct.UserId = u.Id

-- AI conversation count
LEFT JOIN
(
    SELECT
        ac.UserId,
        COUNT(*)               AS ConversationCount
    FROM AI.AIConversations ac
    GROUP BY ac.UserId
) ai_ct ON ai_ct.UserId = u.Id

-- Roles (comma-separated)
OUTER APPLY
(
    SELECT STUFF(
        (SELECT N', ' + r2.Name
         FROM Security.UserRoles ur2
         INNER JOIN Security.Roles r2 ON ur2.RoleId = r2.Id
         WHERE ur2.UserId = u.Id
         FOR XML PATH(N''), TYPE).value(N'.[1]', N'NVARCHAR(MAX)'),
        1, 2, N'') AS RoleNames
) r

WHERE u.DeletedAt IS NULL;
GO

PRINT 'View Views.vw_UserActivity created.';
GO

-- ============================================================================
-- View: Views.vw_DataQuality
-- Description: Data quality checks — orphaned transactions, accounts with no
--              transactions, categories with no transactions, and other
--              integrity indicators
-- ============================================================================
IF OBJECT_ID(N'Views.vw_DataQuality', N'V') IS NOT NULL
    DROP VIEW Views.vw_DataQuality;
GO

CREATE VIEW Views.vw_DataQuality
AS
-- This view returns multiple types of data quality issues using UNION ALL.
-- Each row represents a single quality issue with a type, entity, and details.

-- 1. Orphaned Transactions: transactions referencing non-existent or
--    deleted accounts
SELECT
    N'OrphanedTransaction'     AS IssueType,
    N'Core.Transactions'       AS EntityType,
    CAST(t.Id AS NVARCHAR(256)) AS EntityId,
    t.UserId,
    N'Transaction Id ' + CAST(t.Id AS NVARCHAR(20))
      + N' references Account Id ' + CAST(t.AccountId AS NVARCHAR(20))
      + N' which is deleted or missing.'
                               AS IssueDescription,
    t.CreatedAt                AS IssueDetectedAt

FROM Core.Transactions t
LEFT JOIN Core.Accounts a     ON t.AccountId = a.Id
WHERE t.DeletedAt IS NULL
  AND (a.Id IS NULL OR a.DeletedAt IS NOT NULL)

UNION ALL

-- 2. Transactions with no category (expense type only)
SELECT
    N'MissingCategory'         AS IssueType,
    N'Core.Transactions'       AS EntityType,
    CAST(t.Id AS NVARCHAR(256)) AS EntityId,
    t.UserId,
    N'Expense transaction Id ' + CAST(t.Id AS NVARCHAR(20))
      + N' (₹' + CAST(CAST(t.Amount AS DECIMAL(18,2)) AS NVARCHAR(30)) + N')'
      + N' has no category assigned.'
                               AS IssueDescription,
    t.CreatedAt                AS IssueDetectedAt

FROM Core.Transactions t
WHERE t.DeletedAt IS NULL
  AND t.Type = N'Expense'
  AND t.CategoryId IS NULL
  AND t.ParentTransactionId IS NULL

UNION ALL

-- 3. Active accounts with no transactions (potential stale accounts)
SELECT
    N'AccountNoTransactions'   AS IssueType,
    N'Core.Accounts'           AS EntityType,
    CAST(a.Id AS NVARCHAR(256)) AS EntityId,
    a.UserId,
    N'Account "' + a.Name + N'" (Id: ' + CAST(a.Id AS NVARCHAR(20)) + N')'
      + N' has no transactions and is older than 30 days.'
                               AS IssueDescription,
    a.CreatedAt                AS IssueDetectedAt

FROM Core.Accounts a
WHERE a.DeletedAt IS NULL
  AND a.IsActive = 1
  AND NOT EXISTS
  (
      SELECT 1 FROM Core.Transactions t
      WHERE t.AccountId = a.Id AND t.DeletedAt IS NULL
  )
  AND DATEDIFF(DAY, a.CreatedAt, SYSUTCDATETIME()) > 30

UNION ALL

-- 4. Categories with no transactions (unused categories)
SELECT
    N'UnusedCategory'          AS IssueType,
    N'Core.Categories'         AS EntityType,
    CAST(c.Id AS NVARCHAR(256)) AS EntityId,
    c.UserId,
    N'Category "' + c.Name + N'" (Type: ' + c.Type + N')'
      + N' has no transactions.'
                               AS IssueDescription,
    c.CreatedAt                AS IssueDetectedAt

FROM Core.Categories c
WHERE c.IsActive = 1
  AND NOT EXISTS
  (
      SELECT 1 FROM Core.Transactions t
      WHERE t.CategoryId = c.Id AND t.DeletedAt IS NULL
  )
  AND DATEDIFF(DAY, c.CreatedAt, SYSUTCDATETIME()) > 30

UNION ALL

-- 5. Split parent transactions with no children
SELECT
    N'BrokenSplit'             AS IssueType,
    N'Core.Transactions'       AS EntityType,
    CAST(t.Id AS NVARCHAR(256)) AS EntityId,
    t.UserId,
    N'Transaction Id ' + CAST(t.Id AS NVARCHAR(20))
      + N' is marked as split but has no child transactions.'
                               AS IssueDescription,
    t.CreatedAt                AS IssueDetectedAt

FROM Core.Transactions t
WHERE t.DeletedAt IS NULL
  AND t.IsSplit = 1
  AND NOT EXISTS
  (
      SELECT 1 FROM Core.Transactions child
      WHERE child.ParentTransactionId = t.Id AND child.DeletedAt IS NULL
  )

UNION ALL

-- 6. Active loans with no EMI schedule
SELECT
    N'MissingEMISchedule'      AS IssueType,
    N'Loan.Loans'              AS EntityType,
    CAST(l.Id AS NVARCHAR(256)) AS EntityId,
    l.UserId,
    N'Loan Id ' + CAST(l.Id AS NVARCHAR(20))
      + N' ("' + l.LenderName + N'") is active but has no EMI schedule entries.'
                               AS IssueDescription,
    l.CreatedAt                AS IssueDetectedAt

FROM Loan.Loans l
WHERE l.Status = N'Active'
  AND l.DeletedAt IS NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM Loan.EMISchedule e
      WHERE e.LoanId = l.Id
  )

UNION ALL

-- 7. Holdings with no investment transactions
SELECT
    N'HoldingNoTransactions'   AS IssueType,
    N'Investment.Holdings'     AS EntityType,
    CAST(h.Id AS NVARCHAR(256)) AS EntityId,
    p.UserId,
    N'Holding "' + h.Name + N'" (Id: ' + CAST(h.Id AS NVARCHAR(20)) + N')'
      + N' is active but has no investment transactions.'
                               AS IssueDescription,
    h.CreatedAt                AS IssueDetectedAt

FROM Investment.Holdings h
INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
WHERE h.IsActive = 1
  AND h.DeletedAt IS NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM Investment.Transactions it
      WHERE it.HoldingId = h.Id
  )

UNION ALL

-- 8. Recurring schedules that are past due (NextOccurrenceDate in the past)
SELECT
    N'OverdueRecurring'        AS IssueType,
    N'Core.RecurringSchedules' AS EntityType,
    CAST(rs.Id AS NVARCHAR(256)) AS EntityId,
    rs.UserId,
    N'Recurring schedule Id ' + CAST(rs.Id AS NVARCHAR(20))
      + N' ("' + rs.Description + N'")'
      + N' has NextOccurrenceDate in the past ('
      + CAST(rs.NextOccurrenceDate AS NVARCHAR(20)) + N').'
                               AS IssueDescription,
    rs.CreatedAt               AS IssueDetectedAt

FROM Core.RecurringSchedules rs
WHERE rs.IsActive = 1
  AND rs.NextOccurrenceDate < CAST(SYSUTCDATETIME() AS DATE);
GO

PRINT 'View Views.vw_DataQuality created.';
GO

-- vw_SystemMetrics creation temporarily disabled due to DMV compatibility issues in automated setup.
-- Review and re-enable manually if needed.
-- IF OBJECT_ID(N'Views.vw_SystemMetrics', N'V') IS NOT NULL
--     DROP VIEW Views.vw_SystemMetrics;
-- GO

-- (View creation skipped by automated fixes script.)


PRINT 'View Views.vw_SystemMetrics created.';
GO

PRINT '=== Admin Views created successfully. ===';
GO
