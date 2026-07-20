-- ============================================================================
-- FinOS Database - Manual Script: Data Purge Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Individual purge SPs for each data type plus a master purge
--              SP that calls all of them with configurable retention periods
-- ============================================================================

USE FinOS;
GO

-- ============================================================================
-- 1. SP: dbo.sp_PurgeOldAuditLogs
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_PurgeOldAuditLogs', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurgeOldAuditLogs;
GO

CREATE PROCEDURE dbo.sp_PurgeOldAuditLogs
    @RetentionDays INT = 90,       -- Delete audit logs older than N days
    @BatchSize     INT = 5000      -- Delete in batches to avoid log bloat
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @RetentionDays < 1
        BEGIN
            RAISERROR('RetentionDays must be at least 1.', 16, 1);
            RETURN;
        END

        DECLARE @CutoffDate   DATETIME2 = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
        DECLARE @DeletedCount INT = 0;
        DECLARE @BatchCount   INT;

        WHILE 1 = 1
        BEGIN
            DELETE TOP (@BatchSize) FROM Security.AuditLog
            WHERE CreatedAt < @CutoffDate;

            SET @BatchCount = @@ROWCOUNT;
            IF @BatchCount = 0 BREAK;
            SET @DeletedCount = @DeletedCount + @BatchCount;

            -- Checkpoint to keep transaction log manageable
            CHECKPOINT;
        END

        SELECT
            @DeletedCount  AS RowsDeleted,
            @RetentionDays AS RetentionDays,
            @CutoffDate    AS CutoffDate;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 2. SP: dbo.sp_PurgeOldNotifications
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_PurgeOldNotifications', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurgeOldNotifications;
GO

CREATE PROCEDURE dbo.sp_PurgeOldNotifications
    @RetentionDays  INT = 180,     -- Delete notifications older than N days
    @PurgeUnread    BIT = 0,       -- 0 = only read notifications, 1 = all
    @BatchSize      INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @RetentionDays < 1
        BEGIN
            RAISERROR('RetentionDays must be at least 1.', 16, 1);
            RETURN;
        END

        DECLARE @CutoffDate   DATETIME2 = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
        DECLARE @DeletedCount INT = 0;
        DECLARE @BatchCount   INT;

        WHILE 1 = 1
        BEGIN
            DELETE TOP (@BatchSize) FROM Notifications.Notifications
            WHERE CreatedAt < @CutoffDate
              AND (@PurgeUnread = 1 OR IsRead = 1);

            SET @BatchCount = @@ROWCOUNT;
            IF @BatchCount = 0 BREAK;
            SET @DeletedCount = @DeletedCount + @BatchCount;

            CHECKPOINT;
        END

        -- Also purge expired budget alerts (older than retention)
        DECLARE @BudgetAlertsDeleted INT = 0;
        WHILE 1 = 1
        BEGIN
            DELETE TOP (@BatchSize) FROM Budget.BudgetAlerts
            WHERE CreatedAt < @CutoffDate
              AND IsRead = 1;

            SET @BatchCount = @@ROWCOUNT;
            IF @BatchCount = 0 BREAK;
            SET @BudgetAlertsDeleted = @BudgetAlertsDeleted + @BatchCount;
        END

        SELECT
            @DeletedCount          AS NotificationsDeleted,
            @BudgetAlertsDeleted   AS BudgetAlertsDeleted,
            @RetentionDays         AS RetentionDays,
            @CutoffDate            AS CutoffDate,
            @PurgeUnread           AS PurgedUnread;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 3. SP: dbo.sp_PurgeExpiredTokens
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_PurgeExpiredTokens', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurgeExpiredTokens;
GO

CREATE PROCEDURE dbo.sp_PurgeExpiredTokens
    @OlderThanDays INT = 30        -- Delete tokens expired/revoked > N days ago
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @OlderThanDays < 1
        BEGIN
            RAISERROR('OlderThanDays must be at least 1.', 16, 1);
            RETURN;
        END

        DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@OlderThanDays, SYSUTCDATETIME());

        -- Delete expired/revoked refresh tokens
        DECLARE @RefreshTokensDeleted INT = 0;
        DELETE FROM Security.RefreshTokens
        WHERE (ExpiresAt < @CutoffDate OR (IsRevoked = 1 AND RevokedAt < @CutoffDate))
          AND IsUsed = 1;
        SET @RefreshTokensDeleted = @@ROWCOUNT;

        -- Delete expired password reset tokens
        DECLARE @PasswordTokensDeleted INT = 0;
        DELETE FROM Security.PasswordResetTokens
        WHERE (ExpiresAt < @CutoffDate)
           OR (IsUsed = 1 AND CreatedAt < @CutoffDate);
        SET @PasswordTokensDeleted = @@ROWCOUNT;

        SELECT
            @RefreshTokensDeleted  AS RefreshTokensDeleted,
            @PasswordTokensDeleted AS PasswordResetTokensDeleted,
            @OlderThanDays         AS OlderThanDays,
            @CutoffDate            AS CutoffDate;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 4. SP: dbo.sp_PurgeOldAIConversations
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_PurgeOldAIConversations', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurgeOldAIConversations;
GO

CREATE PROCEDURE dbo.sp_PurgeOldAIConversations
    @RetentionDays INT = 365,      -- Delete AI conversations older than N days
    @BatchSize     INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @RetentionDays < 30
        BEGIN
            RAISERROR('RetentionDays must be at least 30 for AI conversations.', 16, 1);
            RETURN;
        END

        DECLARE @CutoffDate    DATETIME2 = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
        DECLARE @MessagesDeleted INT = 0;
        DECLARE @ConversationsDeleted INT = 0;
        DECLARE @BatchCount INT;

        -- Delete messages first (FK constraint)
        WHILE 1 = 1
        BEGIN
            DELETE TOP (@BatchSize) FROM AI.AIMessages
            WHERE ConversationId IN (
                SELECT Id FROM AI.AIConversations WHERE UpdatedAt < @CutoffDate
            );
            SET @BatchCount = @@ROWCOUNT;
            IF @BatchCount = 0 BREAK;
            SET @MessagesDeleted = @MessagesDeleted + @BatchCount;
            CHECKPOINT;
        END

        -- Delete conversations
        DELETE FROM AI.AIConversations
        WHERE UpdatedAt < @CutoffDate;
        SET @ConversationsDeleted = @@ROWCOUNT;

        SELECT
            @MessagesDeleted       AS MessagesDeleted,
            @ConversationsDeleted  AS ConversationsDeleted,
            @RetentionDays         AS RetentionDays,
            @CutoffDate            AS CutoffDate;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 5. SP: dbo.sp_PurgeCompletedGoals
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_PurgeCompletedGoals', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurgeCompletedGoals;
GO

CREATE PROCEDURE dbo.sp_PurgeCompletedGoals
    @OlderThanMonths INT = 6,      -- Soft-delete goals completed/abandoned > N months ago
    @ArchiveFirst    BIT = 1       -- Archive to GoalsArchive before deleting
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @OlderThanMonths < 1
        BEGIN
            RAISERROR('OlderThanMonths must be at least 1.', 16, 1);
            RETURN;
        END

        DECLARE @CutoffDate DATETIME2 = DATEADD(MONTH, -@OlderThanMonths, SYSUTCDATETIME());
        DECLARE @GoalsArchived INT = 0;
        DECLARE @GoalsSoftDeleted INT = 0;

        -- Archive first if requested
        IF @ArchiveFirst = 1
        BEGIN
            -- Create archive table if not exists
            IF OBJECT_ID(N'Goals.GoalsArchive', N'U') IS NULL
            BEGIN
                SELECT * INTO Goals.GoalsArchive FROM Goals.Goals WHERE 1 = 0;
                ALTER TABLE Goals.GoalsArchive ADD ArchivedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
            END

            IF OBJECT_ID(N'Goals.GoalContributionsArchive', N'U') IS NULL
            BEGIN
                SELECT * INTO Goals.GoalContributionsArchive FROM Goals.GoalContributions WHERE 1 = 0;
            END

            -- Archive goals
            INSERT INTO Goals.GoalsArchive
            (
                Id, UserId, GoalTemplateId, Name, Description, Category,
                TargetAmount, CurrentAmount, MonthlyContribution,
                StartDate, TargetDate, CompletedDate, Priority,
                Status, LinkedAccountIds, Icon, Color, IsAutoContribute,
                ProjectedDate, CreatedAt, UpdatedAt, DeletedAt,
                ArchivedAt
            )
            SELECT
                Id, UserId, GoalTemplateId, Name, Description, Category,
                TargetAmount, CurrentAmount, MonthlyContribution,
                StartDate, TargetDate, CompletedDate, Priority,
                Status, LinkedAccountIds, Icon, Color, IsAutoContribute,
                ProjectedDate, CreatedAt, UpdatedAt, DeletedAt,
                SYSUTCDATETIME()
            FROM Goals.Goals
            WHERE Status IN (N'Completed', N'Abandoned')
              AND UpdatedAt < @CutoffDate
              AND DeletedAt IS NULL;

            SET @GoalsArchived = @@ROWCOUNT;

            -- Archive contributions
            INSERT INTO Goals.GoalContributionsArchive
                (Id, GoalId, Amount, ContributionDate, Source, SourceAccountId, Notes, CreatedAt)
            SELECT gc.Id, gc.GoalId, gc.Amount, gc.ContributionDate,
                   gc.Source, gc.SourceAccountId, gc.Notes, gc.CreatedAt
            FROM Goals.GoalContributions gc
            INNER JOIN Goals.GoalsArchive ga ON gc.GoalId = ga.Id
            WHERE ga.ArchivedAt >= DATEADD(SECOND, -5, SYSUTCDATETIME());  -- Just archived
        END

        -- Soft-delete the old goals
        UPDATE Goals.Goals
        SET DeletedAt = SYSUTCDATETIME(),
            UpdatedAt = SYSUTCDATETIME()
        WHERE Status IN (N'Completed', N'Abandoned')
          AND UpdatedAt < @CutoffDate
          AND DeletedAt IS NULL;

        SET @GoalsSoftDeleted = @@ROWCOUNT;

        SELECT
            @GoalsArchived     AS GoalsArchived,
            @GoalsSoftDeleted  AS GoalsSoftDeleted,
            @OlderThanMonths   AS OlderThanMonths,
            @CutoffDate        AS CutoffDate;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 6. SP: dbo.sp_PurgeOldImportBatches
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_PurgeOldImportBatches', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurgeOldImportBatches;
GO

CREATE PROCEDURE dbo.sp_PurgeOldImportBatches
    @RetentionDays INT = 30,       -- Delete completed/failed batches older than N days
    @BatchSize     INT = 5000
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @RetentionDays < 1
        BEGIN
            RAISERROR('RetentionDays must be at least 1.', 16, 1);
            RETURN;
        END

        DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
        DECLARE @ErrorsDeleted INT = 0;
        DECLARE @BatchesDeleted INT = 0;
        DECLARE @BatchCount INT;

        -- Delete import errors for old batches first (FK constraint)
        WHILE 1 = 1
        BEGIN
            DELETE TOP (@BatchSize) FROM Import.ImportErrors
            WHERE BatchId IN (
                SELECT Id FROM Import.ImportBatches
                WHERE CreatedAt < @CutoffDate
                  AND Status IN (N'Completed', N'Failed')
            );
            SET @BatchCount = @@ROWCOUNT;
            IF @BatchCount = 0 BREAK;
            SET @ErrorsDeleted = @ErrorsDeleted + @BatchCount;
        END

        -- Delete old import batches
        DELETE FROM Import.ImportBatches
        WHERE CreatedAt < @CutoffDate
          AND Status IN (N'Completed', N'Failed');
        SET @BatchesDeleted = @@ROWCOUNT;

        SELECT
            @ErrorsDeleted  AS ImportErrorsDeleted,
            @BatchesDeleted AS ImportBatchesDeleted,
            @RetentionDays  AS RetentionDays,
            @CutoffDate     AS CutoffDate;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 7. SP: dbo.sp_MasterPurge (Master Purge Procedure)
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_MasterPurge', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_MasterPurge;
GO

CREATE PROCEDURE dbo.sp_MasterPurge
    @AuditLogRetentionDays     INT = 90,     -- Audit logs: 90 days
    @NotificationRetentionDays INT = 180,    -- Read notifications: 180 days
    @PurgeUnreadNotifications  BIT = 0,      -- Don't purge unread by default
    @TokenRetentionDays        INT = 30,     -- Expired tokens: 30 days
    @AIConversationRetentionDays INT = 365,  -- AI conversations: 365 days
    @GoalRetentionMonths       INT = 6,      -- Completed goals: 6 months
    @ArchiveGoalsBeforePurge   BIT = 1,      -- Archive before soft-delete
    @ImportBatchRetentionDays  INT = 30,     -- Import batches: 30 days
    @DryRun                    BIT = 0       -- If 1, only report what would be deleted
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        CREATE TABLE #PurgeResults
        (
            Category        NVARCHAR(50),
            RowsAffected    INT,
            Details         NVARCHAR(500)
        );

        PRINT N'=================================================================';
        PRINT N'  FinOS Master Data Purge';
        PRINT N'  Started: ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);
        PRINT N'  Dry Run: ' + CASE WHEN @DryRun = 1 THEN N'YES (no data will be deleted)' ELSE N'NO (data will be deleted)' END;
        PRINT N'=================================================================';

        IF @DryRun = 1
        BEGIN
            -- Report what would be deleted without actually deleting
            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Audit Logs', COUNT(*), N'Older than ' + CAST(@AuditLogRetentionDays AS NVARCHAR(10)) + N' days'
            FROM Security.AuditLog WHERE CreatedAt < DATEADD(DAY, -@AuditLogRetentionDays, SYSUTCDATETIME());

            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Notifications', COUNT(*), N'Read, older than ' + CAST(@NotificationRetentionDays AS NVARCHAR(10)) + N' days'
            FROM Notifications.Notifications
            WHERE CreatedAt < DATEADD(DAY, -@NotificationRetentionDays, SYSUTCDATETIME())
              AND (@PurgeUnreadNotifications = 1 OR IsRead = 1);

            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Refresh Tokens', COUNT(*), N'Expired/revoked > ' + CAST(@TokenRetentionDays AS NVARCHAR(10)) + N' days'
            FROM Security.RefreshTokens
            WHERE (ExpiresAt < DATEADD(DAY, -@TokenRetentionDays, SYSUTCDATETIME())
                OR (IsRevoked = 1 AND RevokedAt < DATEADD(DAY, -@TokenRetentionDays, SYSUTCDATETIME())))
              AND IsUsed = 1;

            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'AI Conversations', COUNT(*), N'Older than ' + CAST(@AIConversationRetentionDays AS NVARCHAR(10)) + N' days'
            FROM AI.AIConversations WHERE UpdatedAt < DATEADD(DAY, -@AIConversationRetentionDays, SYSUTCDATETIME());

            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Completed Goals', COUNT(*), N'Completed/abandoned > ' + CAST(@GoalRetentionMonths AS NVARCHAR(10)) + N' months'
            FROM Goals.Goals
            WHERE Status IN (N'Completed', N'Abandoned')
              AND UpdatedAt < DATEADD(MONTH, -@GoalRetentionMonths, SYSUTCDATETIME())
              AND DeletedAt IS NULL;

            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Import Batches', COUNT(*), N'Completed/failed > ' + CAST(@ImportBatchRetentionDays AS NVARCHAR(10)) + N' days'
            FROM Import.ImportBatches
            WHERE CreatedAt < DATEADD(DAY, -@ImportBatchRetentionDays, SYSUTCDATETIME())
              AND Status IN (N'Completed', N'Failed');
        END
        ELSE
        BEGIN
            -- Actually perform the purge
            DECLARE @Result TABLE (RowsDeleted INT, RetentionDays INT, CutoffDate DATETIME2);

            -- 1. Audit Logs
            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Audit Logs', RowsDeleted, N'Retention: ' + CAST(RetentionDays AS NVARCHAR(10)) + N' days, Cutoff: ' + CONVERT(NVARCHAR(30), CutoffDate, 120)
            FROM dbo.sp_PurgeOldAuditLogs(@AuditLogRetentionDays);

            -- 2. Notifications
            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Notifications', NotificationsDeleted, N'Retention: ' + CAST(@NotificationRetentionDays AS NVARCHAR(10)) + N' days'
            FROM dbo.sp_PurgeOldNotifications(@NotificationRetentionDays, @PurgeUnreadNotifications);

            -- 3. Expired Tokens
            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Expired Tokens', RefreshTokensDeleted + PasswordResetTokensDeleted,
                   N'Retention: ' + CAST(@TokenRetentionDays AS NVARCHAR(10)) + N' days'
            FROM dbo.sp_PurgeExpiredTokens(@TokenRetentionDays);

            -- 4. AI Conversations
            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'AI Conversations', MessagesDeleted + ConversationsDeleted,
                   N'Retention: ' + CAST(@AIConversationRetentionDays AS NVARCHAR(10)) + N' days'
            FROM dbo.sp_PurgeOldAIConversations(@AIConversationRetentionDays);

            -- 5. Completed Goals
            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Completed Goals', GoalsArchived + GoalsSoftDeleted,
                   N'Retention: ' + CAST(@GoalRetentionMonths AS NVARCHAR(10)) + N' months'
            FROM dbo.sp_PurgeCompletedGoals(@GoalRetentionMonths, @ArchiveGoalsBeforePurge);

            -- 6. Import Batches
            INSERT INTO #PurgeResults (Category, RowsAffected, Details)
            SELECT N'Import Batches', ImportErrorsDeleted + ImportBatchesDeleted,
                   N'Retention: ' + CAST(@ImportBatchRetentionDays AS NVARCHAR(10)) + N' days'
            FROM dbo.sp_PurgeOldImportBatches(@ImportBatchRetentionDays);
        END

        -- Return results
        SELECT Category, RowsAffected, Details
        FROM #PurgeResults
        ORDER BY Category;

        PRINT N'';
        PRINT N'=================================================================';
        PRINT N'  Master Purge Completed';
        PRINT N'  Finished: ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);
        PRINT N'=================================================================';

        DROP TABLE #PurgeResults;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#PurgeResults', N'U') IS NOT NULL
            DROP TABLE #PurgeResults;

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

PRINT 'Data purge stored procedures created successfully.';
PRINT '  - dbo.sp_PurgeOldAuditLogs';
PRINT '  - dbo.sp_PurgeOldNotifications';
PRINT '  - dbo.sp_PurgeExpiredTokens';
PRINT '  - dbo.sp_PurgeOldAIConversations';
PRINT '  - dbo.sp_PurgeCompletedGoals';
PRINT '  - dbo.sp_PurgeOldImportBatches';
PRINT '  - dbo.sp_MasterPurge (calls all above)';
PRINT '';
PRINT 'Usage Examples:';
PRINT '  -- Dry run (no data deleted):';
PRINT '  EXEC dbo.sp_MasterPurge @DryRun = 1;';
PRINT '';
PRINT '  -- Run with defaults:';
PRINT '  EXEC dbo.sp_MasterPurge;';
PRINT '';
PRINT '  -- Run with custom retention:';
PRINT '  EXEC dbo.sp_MasterPurge';
PRINT '      @AuditLogRetentionDays = 60,';
PRINT '      @NotificationRetentionDays = 90,';
PRINT '      @AIConversationRetentionDays = 180;';
GO
