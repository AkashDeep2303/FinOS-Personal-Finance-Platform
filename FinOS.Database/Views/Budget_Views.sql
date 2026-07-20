-- ============================================================================
-- FinOS Database - Budget Views
-- Target: Microsoft SQL Server (SSMS)
-- Description: Views for budget tracking — budget vs actual, budget alerts,
--              and subscription calendar
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
-- View: Views.vw_BudgetVsActual
-- Description: Budget categories with allocated vs spent amounts, remaining,
--              percentage used, and status (Under / OnTrack / Over)
-- ============================================================================
IF OBJECT_ID(N'Views.vw_BudgetVsActual', N'V') IS NOT NULL
    DROP VIEW Views.vw_BudgetVsActual;
GO

CREATE VIEW Views.vw_BudgetVsActual
AS
SELECT
    bc.Id                      AS BudgetCategoryId,
    b.Id                       AS BudgetId,
    b.UserId,
    b.Name                     AS BudgetName,
    b.PeriodType,
    b.StartDate                AS BudgetStartDate,
    b.EndDate                  AS BudgetEndDate,
    b.TotalBudgetAmount,
    b.AlertThresholdPct        AS BudgetAlertThresholdPct,
    b.IsActive                 AS BudgetIsActive,

    -- Category details
    bc.CategoryId,
    ISNULL(c.Name, bc.CustomLabel)  AS CategoryName,
    c.Type                     AS CategoryType,
    c.Icon                     AS CategoryIcon,
    c.Color                    AS CategoryColor,
    bc.CustomLabel,

    -- Amounts
    bc.AllocatedAmount,
    bc.SpentAmount,

    -- Remaining budget
    bc.AllocatedAmount - bc.SpentAmount
                               AS RemainingAmount,

    -- Percentage used
    CAST(
        ROUND(
            CASE WHEN bc.AllocatedAmount = 0 THEN 0
                 ELSE bc.SpentAmount / bc.AllocatedAmount * 100
            END, 2)
        AS DECIMAL(5,2))       AS PctUsed,

    -- Category-level alert threshold (overrides budget-level if set)
    ISNULL(bc.AlertThresholdPct, b.AlertThresholdPct)
                               AS EffectiveAlertThresholdPct,

    -- Status: Under / OnTrack / Over
    CASE
        WHEN bc.AllocatedAmount = 0 THEN N'N/A'
        WHEN bc.SpentAmount / bc.AllocatedAmount * 100
             > ISNULL(bc.AlertThresholdPct, b.AlertThresholdPct)
            THEN N'Over'
        WHEN bc.SpentAmount / bc.AllocatedAmount * 100
             >= ISNULL(bc.AlertThresholdPct, b.AlertThresholdPct) * 0.9
            THEN N'OnTrack'
        ELSE N'Under'
    END                        AS BudgetStatus,

    -- Overage amount (how much over budget)
    CASE
        WHEN bc.SpentAmount > bc.AllocatedAmount
            THEN bc.SpentAmount - bc.AllocatedAmount
        ELSE 0
    END                        AS OverageAmount,

    bc.SortOrder,
    bc.CreatedAt,
    bc.UpdatedAt

FROM Budget.BudgetCategories bc
INNER JOIN Budget.Budgets b        ON bc.BudgetId = b.Id
LEFT JOIN Core.Categories c        ON bc.CategoryId = c.Id
WHERE b.DeletedAt IS NULL
  AND b.IsActive = 1;
GO

PRINT 'View Views.vw_BudgetVsActual created.';
GO

-- ============================================================================
-- View: Views.vw_BudgetAlertsSummary
-- Description: Unread budget alerts with category and budget name for
--              quick notification display
-- ============================================================================
IF OBJECT_ID(N'Views.vw_BudgetAlertsSummary', N'V') IS NOT NULL
    DROP VIEW Views.vw_BudgetAlertsSummary;
GO

CREATE VIEW Views.vw_BudgetAlertsSummary
AS
SELECT
    ba.Id                      AS AlertId,
    b.UserId,
    b.Id                       AS BudgetId,
    b.Name                     AS BudgetName,
    b.PeriodType,
    b.StartDate                AS BudgetStartDate,
    b.EndDate                  AS BudgetEndDate,

    -- Category context
    bc.Id                      AS BudgetCategoryId,
    ISNULL(c.Name, bc.CustomLabel)  AS CategoryName,
    c.Icon                     AS CategoryIcon,

    -- Alert details
    ba.AlertType,
    ba.ThresholdPercentage,
    ba.Message,
    ba.IsRead,
    ba.CreatedAt               AS AlertCreatedAt,

    -- Budget amounts for context
    bc.AllocatedAmount,
    bc.SpentAmount,
    CASE
        WHEN bc.AllocatedAmount = 0 THEN 0
        ELSE CAST(ROUND(bc.SpentAmount / bc.AllocatedAmount * 100, 2)
             AS DECIMAL(5,2))
    END                        AS CurrentPctUsed,

    -- Time since alert
    DATEDIFF(MINUTE, ba.CreatedAt, SYSUTCDATETIME())
                               AS MinutesSinceAlert

FROM Budget.BudgetAlerts ba
INNER JOIN Budget.BudgetCategories bc ON ba.BudgetCategoryId = bc.Id
INNER JOIN Budget.Budgets b          ON bc.BudgetId = b.Id
LEFT JOIN Core.Categories c          ON bc.CategoryId = c.Id
WHERE ba.IsRead = 0
  AND b.DeletedAt IS NULL
  AND b.IsActive = 1;
GO

PRINT 'View Views.vw_BudgetAlertsSummary created.';
GO

-- ============================================================================
-- View: Views.vw_SubscriptionCalendar
-- Description: All active detected subscriptions with next expected date,
--              amount, frequency, merchant, and category for calendar display
-- ============================================================================
IF OBJECT_ID(N'Views.vw_SubscriptionCalendar', N'V') IS NOT NULL
    DROP VIEW Views.vw_SubscriptionCalendar;
GO

CREATE VIEW Views.vw_SubscriptionCalendar
AS
SELECT
    ds.Id                      AS SubscriptionId,
    ds.UserId,
    ds.MerchantName,
    ds.Amount,
    ds.Currency,
    ds.Frequency,
    ds.NextExpectedDate,
    ds.LastTransactionDate,

    -- Category details
    ds.CategoryId,
    c.Name                     AS CategoryName,
    c.Icon                     AS CategoryIcon,
    c.Color                    AS CategoryColor,

    -- Detection confidence
    ds.DetectionConfidence,
    ds.TransactionCount,
    ds.IsConfirmed,

    -- Computed: days until next expected date
    CASE
        WHEN ds.NextExpectedDate IS NOT NULL
            THEN DATEDIFF(DAY, CAST(SYSUTCDATETIME() AS DATE), ds.NextExpectedDate)
        ELSE NULL
    END                        AS DaysUntilNext,

    -- Computed: annual cost estimate
    CASE
        WHEN ds.Frequency = N'Weekly'   THEN ds.Amount * 52
        WHEN ds.Frequency = N'Monthly'  THEN ds.Amount * 12
        WHEN ds.Frequency = N'Quarterly' THEN ds.Amount * 4
        WHEN ds.Frequency = N'Yearly'   THEN ds.Amount * 1
        ELSE NULL
    END                        AS EstimatedAnnualCost,

    -- Computed: monthly cost estimate
    CASE
        WHEN ds.Frequency = N'Weekly'   THEN CAST(ROUND(ds.Amount * 52.0 / 12.0, 2) AS DECIMAL(18,2))
        WHEN ds.Frequency = N'Monthly'  THEN ds.Amount
        WHEN ds.Frequency = N'Quarterly' THEN CAST(ROUND(ds.Amount * 4.0 / 12.0, 2) AS DECIMAL(18,2))
        WHEN ds.Frequency = N'Yearly'   THEN CAST(ROUND(ds.Amount / 12.0, 2) AS DECIMAL(18,2))
        ELSE NULL
    END                        AS EstimatedMonthlyCost,

    ds.IsActive,
    ds.CreatedAt,
    ds.UpdatedAt

FROM Subscriptions.DetectedSubscriptions ds
LEFT JOIN Core.Categories c        ON ds.CategoryId = c.Id
WHERE ds.IsActive = 1;
GO

PRINT 'View Views.vw_SubscriptionCalendar created.';
GO

PRINT '=== Budget Views created successfully. ===';
GO
