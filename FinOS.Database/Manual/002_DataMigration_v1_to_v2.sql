-- ============================================================================
-- FinOS Database - Manual Migration Script: v1 to v2
-- Target: Microsoft SQL Server (SSMS)
-- Description: Template for migrating from schema version 1 to version 2.
--              Includes new columns, new tables, column modifications, data
--              transformations, and commented rollback sections.
-- IMPORTANT:   Run in a transaction. Review each section before executing.
--              Test on a staging environment first!
-- ============================================================================

USE FinOS;
GO

-- ============================================================================
-- PRE-MIGRATION CHECKS
-- ============================================================================
PRINT N'=================================================================';
PRINT N'  FinOS Data Migration: v1 -> v2';
PRINT N'  Started: ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);
PRINT N'=================================================================';

-- Verify we are on v1 (check a known v1 indicator)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'Core' AND TABLE_NAME = N'Tags' AND COLUMN_NAME = N'Color')
    PRINT N'  INFO: Tags.Color column does not exist - migration needed.';
ELSE
    PRINT N'  WARNING: Tags.Color column already exists - migration may have been applied.';
GO

-- ============================================================================
-- BEGIN TRANSACTION
-- ============================================================================
BEGIN TRANSACTION;
GO

PRINT N'';
PRINT N'>>> Starting migration in transaction...';
GO

-- ============================================================================
-- SECTION 1: ADD NEW COLUMNS
-- ============================================================================

-- 1a. Add Color column to Core.Tags (if missing)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'Core' AND TABLE_NAME = N'Tags' AND COLUMN_NAME = N'Color')
BEGIN
    ALTER TABLE Core.Tags ADD Color NVARCHAR(7) NULL;
    PRINT N'  [v2] Added Core.Tags.Color column.';
END
ELSE
BEGIN
    PRINT N'  [v2] Core.Tags.Color already exists, skipping.';
END
GO

-- 1b. Add Notes column to Core.Accounts (if missing)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'Core' AND TABLE_NAME = N'Accounts' AND COLUMN_NAME = N'Notes')
BEGIN
    ALTER TABLE Core.Accounts ADD Notes NVARCHAR(500) NULL;
    PRINT N'  [v2] Added Core.Accounts.Notes column.';
END
GO

-- 1c. Add LastPriceUpdateAt to Investment.Holdings (if missing)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'Investment' AND TABLE_NAME = N'Holdings' AND COLUMN_NAME = N'LastPriceUpdateAt')
BEGIN
    ALTER TABLE Investment.Holdings ADD LastPriceUpdateAt DATETIME2 NULL;
    PRINT N'  [v2] Added Investment.Holdings.LastPriceUpdateAt column.';
END
GO

-- 1d. Add ProjectedDate to Goals.Goals (if missing)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'Goals' AND TABLE_NAME = N'Goals' AND COLUMN_NAME = N'ProjectedDate')
BEGIN
    ALTER TABLE Goals.Goals ADD ProjectedDate DATE NULL;
    PRINT N'  [v2] Added Goals.Goals.ProjectedDate column.';
END
GO

-- 1e. Add FeedbackRating and FeedbackComment to AI.AIMessages (if missing)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'AI' AND TABLE_NAME = N'AIMessages' AND COLUMN_NAME = N'FeedbackRating')
BEGIN
    ALTER TABLE AI.AIMessages ADD FeedbackRating TINYINT NULL;
    PRINT N'  [v2] Added AI.AIMessages.FeedbackRating column.';
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'AI' AND TABLE_NAME = N'AIMessages' AND COLUMN_NAME = N'FeedbackComment')
BEGIN
    ALTER TABLE AI.AIMessages ADD FeedbackComment NVARCHAR(500) NULL;
    PRINT N'  [v2] Added AI.AIMessages.FeedbackComment column.';
END
GO

-- ============================================================================
-- SECTION 2: ADD NEW TABLES
-- ============================================================================

-- 2a. Create Goals archive table (if missing)
IF OBJECT_ID(N'Goals.GoalsArchive', N'U') IS NULL
BEGIN
    SELECT *
    INTO Goals.GoalsArchive
    FROM Goals.Goals
    WHERE 1 = 0;

    ALTER TABLE Goals.GoalsArchive ADD ArchivedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
    PRINT N'  [v2] Created Goals.GoalsArchive table.';
END
GO

-- 2b. Create GoalContributions archive table (if missing)
IF OBJECT_ID(N'Goals.GoalContributionsArchive', N'U') IS NULL
BEGIN
    SELECT *
    INTO Goals.GoalContributionsArchive
    FROM Goals.GoalContributions
    WHERE 1 = 0;
    PRINT N'  [v2] Created Goals.GoalContributionsArchive table.';
END
GO

-- 2c. Create JobExecutionLog table (if missing)
IF OBJECT_ID(N'dbo.JobExecutionLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobExecutionLog
    (
        Id              BIGINT          IDENTITY(1,1)   NOT NULL,
        JobName         NVARCHAR(200)                   NOT NULL,
        StepName        NVARCHAR(200)                   NOT NULL,
        StartTime       DATETIME2                       NOT NULL,
        EndTime         DATETIME2                       NULL,
        DurationMs      INT                             NULL,
        Status          NVARCHAR(20)                    NOT NULL,
        RowsAffected    INT                             NULL,
        ErrorMessage    NVARCHAR(4000)                  NULL,

        CONSTRAINT PK_JobExecutionLog PRIMARY KEY CLUSTERED (Id) ON FinOS_Data
    );
    PRINT N'  [v2] Created dbo.JobExecutionLog table.';
END
GO

-- ============================================================================
-- SECTION 3: MODIFY EXISTING COLUMNS
-- ============================================================================

-- 3a. Widen Description column in Core.Transactions (if needed)
-- NOTE: Column width changes may require dropping and recreating indexes
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'Core' AND TABLE_NAME = N'Transactions'
      AND COLUMN_NAME = N'Description' AND CHARACTER_MAXIMUM_LENGTH < 500
)
BEGIN
    -- SQL Server allows widening NVARCHAR columns without dropping the table
    ALTER TABLE Core.Transactions ALTER COLUMN Description NVARCHAR(500) NOT NULL;
    PRINT N'  [v2] Widened Core.Transactions.Description to NVARCHAR(500).';
END
GO

-- 3b. Change Budget.Budgets.AlertThresholdPct default to 80.00
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'Budget' AND TABLE_NAME = N'Budgets'
      AND COLUMN_NAME = N'AlertThresholdPct' AND COLUMN_DEFAULT IS NULL
)
BEGIN
    ALTER TABLE Budget.Budgets ADD CONSTRAINT DF_Budgets_AlertThresholdPct DEFAULT 80.00 FOR AlertThresholdPct;
    PRINT N'  [v2] Added default constraint for Budget.Budgets.AlertThresholdPct.';
END
GO

-- ============================================================================
-- SECTION 4: DATA TRANSFORMATION SCRIPTS
-- ============================================================================

-- 4a. Assign default colors to existing tags that don't have one
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'Core' AND TABLE_NAME = N'Tags' AND COLUMN_NAME = N'Color')
BEGIN
    -- Set random colors for tags that don't have one
    UPDATE Core.Tags
    SET Color = CASE (Id % 8)
        WHEN 0 THEN N'#F44336'
        WHEN 1 THEN N'#E91E63'
        WHEN 2 THEN N'#9C27B0'
        WHEN 3 THEN N'#2196F3'
        WHEN 4 THEN N'#4CAF50'
        WHEN 5 THEN N'#FF9800'
        WHEN 6 THEN N'#795548'
        WHEN 7 THEN N'#607D8B'
    END
    WHERE Color IS NULL;

    PRINT N'  [v2] Assigned default colors to ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' tags.';
END
GO

-- 4b. Backfill ProjectedDate for goals that don't have one
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = N'Goals' AND TABLE_NAME = N'Goals' AND COLUMN_NAME = N'ProjectedDate')
BEGIN
    UPDATE Goals.Goals
    SET ProjectedDate = DATEADD(
        MONTH,
        CASE WHEN MonthlyContribution > 0 AND (TargetAmount - CurrentAmount) > 0
             THEN CEILING((TargetAmount - CurrentAmount) / MonthlyContribution)
             ELSE DATEDIFF(MONTH, StartDate, TargetDate)
        END,
        StartDate
    )
    WHERE ProjectedDate IS NULL
      AND Status = N'InProgress'
      AND DeletedAt IS NULL;

    PRINT N'  [v2] Backfilled ProjectedDate for ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' goals.';
END
GO

-- 4c. Set default account balance for existing users without accounts
PRINT N'  [v2] Data transformations complete.';
GO

-- ============================================================================
-- SECTION 5: ADD NEW INDEXES
-- ============================================================================

-- 5a. Add index on AI.AIMessages.FeedbackRating for analytics
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMessages_FeedbackRating')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AIMessages_FeedbackRating
        ON AI.AIMessages (FeedbackRating) WHERE FeedbackRating IS NOT NULL ON FinOS_Index;
    PRINT N'  [v2] Added index IX_AIMessages_FeedbackRating.';
END
GO

-- 5b. Add index on Core.Tags.Color
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tags_Color')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tags_Color
        ON Core.Tags (Color) WHERE Color IS NOT NULL ON FinOS_Index;
    PRINT N'  [v2] Added index IX_Tags_Color.';
END
GO

-- ============================================================================
-- SECTION 6: UPDATE STORED PROCEDURES (if any SP changes in v2)
-- ============================================================================

-- Example: If a stored procedure needs updating for v2, include the new version here
-- EXEC('ALTER PROCEDURE ...');
-- PRINT N'  [v2] Updated stored procedure X.';

PRINT N'  [v2] No stored procedure updates in this migration.';
GO

-- ============================================================================
-- COMMIT TRANSACTION
-- ============================================================================
COMMIT TRANSACTION;
GO

PRINT N'';
PRINT N'=================================================================';
PRINT N'  Migration v1 -> v2 COMPLETED SUCCESSFULLY';
PRINT N'  Finished: ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);
PRINT N'=================================================================';
GO

-- ============================================================================
-- ROLLBACK SECTION (COMMENTED OUT)
-- ============================================================================
-- To rollback, uncomment the following and run manually:
--
-- BEGIN TRANSACTION;
--
-- -- Drop new indexes
-- DROP INDEX IX_AIMessages_FeedbackRating ON AI.AIMessages;
-- DROP INDEX IX_Tags_Color ON Core.Tags;
--
-- -- Drop new columns
-- ALTER TABLE AI.AIMessages DROP COLUMN FeedbackComment;
-- ALTER TABLE AI.AIMessages DROP COLUMN FeedbackRating;
-- ALTER TABLE Goals.Goals DROP COLUMN ProjectedDate;
-- ALTER TABLE Investment.Holdings DROP COLUMN LastPriceUpdateAt;
-- ALTER TABLE Core.Accounts DROP COLUMN Notes;
-- ALTER TABLE Core.Tags DROP COLUMN Color;
--
-- -- Drop new tables
-- DROP TABLE Goals.GoalContributionsArchive;
-- DROP TABLE Goals.GoalsArchive;
-- DROP TABLE dbo.JobExecutionLog;
--
-- -- Revert column width
-- ALTER TABLE Core.Transactions ALTER COLUMN Description NVARCHAR(200) NOT NULL;
--
-- -- Remove default constraint
-- ALTER TABLE Budget.Budgets DROP CONSTRAINT DF_Budgets_AlertThresholdPct;
--
-- COMMIT TRANSACTION;
-- PRINT N'Rollback v2 -> v1 completed.';
GO
