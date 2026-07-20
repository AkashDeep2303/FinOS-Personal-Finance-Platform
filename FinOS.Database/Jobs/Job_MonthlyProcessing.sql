-- ============================================================================
-- FinOS Database - SQL Server Agent Job: Monthly Processing
-- Target: Microsoft SQL Server (SSMS)
-- Description: Monthly job that runs on the 1st of every month at 1:00 AM IST
--              (19:30 UTC previous day) to archive old goals, generate EPF
--              interest, update investment prices, purge AI history, and create
--              monthly financial snapshots
-- ============================================================================

USE msdb;
GO

-- ---------------------------------------------------------------------------
-- Remove the job if it already exists (idempotent deployment)
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'FinOS_MonthlyProcessing')
BEGIN
    EXEC msdb.dbo.sp_delete_job
        @job_name = N'FinOS_MonthlyProcessing',
        @delete_unused_schedule = 1;
END
GO

-- ---------------------------------------------------------------------------
-- Create the Job
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_job
    @job_name           = N'FinOS_MonthlyProcessing',
    @enabled            = 1,
    @description        = N'Monthly processing: archives completed goals, generates EPF interest, updates investment prices, purges old AI conversations, and creates monthly financial snapshots. Runs on 1st of every month at 1:00 AM IST.',
    @category_name      = N'Database Maintenance',
    @owner_login_name   = N'sa',
    @notify_level_eventlog = 2;   -- On failure
GO

-- ---------------------------------------------------------------------------
-- Step 1: Archive Completed/Abandoned Goals Older Than 6 Months
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_MonthlyProcessing',
    @step_name  = N'Archive Old Goals',
    @step_id    = 1,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_MonthlyProcessing'',
    @StepName = N''Archive Old Goals'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @CutoffDate DATETIME2 = DATEADD(MONTH, -6, SYSUTCDATETIME());
    DECLARE @ArchivedCount INT = 0;

    -- Create the archive table if it does not exist
    IF OBJECT_ID(N''Goals.GoalsArchive'', N''U'') IS NULL
    BEGIN
        SELECT *
        INTO Goals.GoalsArchive
        FROM Goals.Goals
        WHERE 1 = 0;  -- Schema only, no data

        -- Add archive-specific columns
        ALTER TABLE Goals.GoalsArchive ADD ArchivedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME();
    END

    -- Archive completed or abandoned goals older than 6 months
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
    WHERE Status IN (N''Completed'', N''Abandoned'')
      AND UpdatedAt < @CutoffDate
      AND DeletedAt IS NULL;

    SET @ArchivedCount = @@ROWCOUNT;

    -- Also archive contributions for archived goals
    IF OBJECT_ID(N''Goals.GoalContributionsArchive'', N''U'') IS NULL
    BEGIN
        SELECT *
        INTO Goals.GoalContributionsArchive
        FROM Goals.GoalContributions
        WHERE 1 = 0;
    END

    INSERT INTO Goals.GoalContributionsArchive
        (Id, GoalId, Amount, ContributionDate, Source, SourceAccountId, Notes, CreatedAt)
    SELECT
        gc.Id, gc.GoalId, gc.Amount, gc.ContributionDate,
        gc.Source, gc.SourceAccountId, gc.Notes, gc.CreatedAt
    FROM Goals.GoalContributions gc
    INNER JOIN Goals.GoalsArchive ga ON gc.GoalId = ga.Id;

    -- Soft-delete the archived goals (they remain in Goals table with DeletedAt set)
    UPDATE Goals.Goals
    SET DeletedAt = SYSUTCDATETIME(),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Status IN (N''Completed'', N''Abandoned'')
      AND UpdatedAt < @CutoffDate
      AND DeletedAt IS NULL;

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @ArchivedCount;
END TRY
BEGIN CATCH
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Failed'',
        @ErrorMessage = @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
',
    @database_name      = N'FinOS',
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Step 2: Generate EPF Interest for the Month
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_MonthlyProcessing',
    @step_name  = N'Generate EPF Interest',
    @step_id    = 2,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @AccountsProcessed INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_MonthlyProcessing'',
    @StepName = N''Generate EPF Interest'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    -- Determine the previous month (the month that just ended)
    DECLARE @PrevMonth DATE = DATEFROMPARTS(
        YEAR(DATEADD(MONTH, -1, SYSUTCDATETIME())),
        MONTH(DATEADD(MONTH, -1, SYSUTCDATETIME())),
        1
    );

    DECLARE @EPFAccountId BIGINT;
    DECLARE @MonthlySalary DECIMAL(18,2);
    DECLARE @EmployeePct DECIMAL(5,2);
    DECLARE @EmployerPct DECIMAL(5,2);
    DECLARE @EPSPct DECIMAL(5,2);

    DECLARE epf_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            Id,
            ISNULL(MonthlySalary, 50000),
            EmployeeContributionPct,
            EmployerContributionPct,
            8.33   -- EPS contribution is 8.33% of basic (employer''s share)
        FROM Investment.EPFAccounts
        WHERE IsActive = 1;

    OPEN epf_cursor;
    FETCH NEXT FROM epf_cursor INTO @EPFAccountId, @MonthlySalary, @EmployeePct, @EmployerPct, @EPSPct;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- Check if contribution already exists for this month
            IF NOT EXISTS (
                SELECT 1 FROM Investment.EPFContributions
                WHERE EPFAccountId = @EPFAccountId AND Month = @PrevMonth
            )
            BEGIN
                -- Calculate contributions based on monthly salary
                DECLARE @BasicSalary DECIMAL(18,2) = @MonthlySalary * 0.40;  -- Assume 40% basic
                DECLARE @EmployeeContrib DECIMAL(18,2) = ROUND(@BasicSalary * @EmployeePct / 100.0, 2);
                DECLARE @EmployerContrib DECIMAL(18,2) = ROUND(@BasicSalary * @EmployerPct / 100.0, 2);
                DECLARE @EPSContrib DECIMAL(18,2) = ROUND(@BasicSalary * @EPSPct / 100.0, 2);

                -- Cap contributions at EPF ceiling (₹15,000 basic for EPS)
                IF @BasicSalary > 15000
                    SET @EPSContrib = ROUND(15000 * @EPSPct / 100.0, 2);

                EXEC Investment.sp_UpdateEPFContribution
                    @EPFAccountId         = @EPFAccountId,
                    @Month                = @PrevMonth,
                    @EmployeeContribution = @EmployeeContrib,
                    @EmployerContribution = @EmployerContrib,
                    @EPSContribution      = @EPSContrib;

                SET @AccountsProcessed = @AccountsProcessed + 1;
            END
        END TRY
        BEGIN CATCH
            DECLARE @UserErr NVARCHAR(4000) = ERROR_MESSAGE();
            PRINT N''Warning: Failed to process EPF for AccountId '' + CAST(@EPFAccountId AS NVARCHAR(20)) + N'': '' + @UserErr;
        END CATCH

        FETCH NEXT FROM epf_cursor INTO @EPFAccountId, @MonthlySalary, @EmployeePct, @EmployerPct, @EPSPct;
    END

    CLOSE epf_cursor;
    DEALLOCATE epf_cursor;

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @AccountsProcessed;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS(N''local'', N''epf_cursor'') >= 0
    BEGIN
        CLOSE epf_cursor;
        DEALLOCATE epf_cursor;
    END

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Failed'',
        @ErrorMessage = @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
',
    @database_name      = N'FinOS',
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Step 3: Update Investment Holdings with Latest Prices (Placeholder)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_MonthlyProcessing',
    @step_name  = N'Update Investment Prices',
    @step_id    = 3,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_MonthlyProcessing'',
    @StepName = N''Update Investment Prices'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @HoldingsUpdated INT = 0;

    -- ===================================================================
    -- PLACEHOLDER: This step integrates with external price APIs.
    -- In production, replace this section with actual API calls via:
    --   1. SQL Server Integration Services (SSIS) package
    --   2. Azure Function / Web Job that writes prices to a staging table
    --   3. CLR stored procedure calling external APIs
    --   4. External scheduler (e.g., Azure Functions) that calls
    --      Investment.sp_UpdateHoldingPrice for each holding
    --
    -- The following is a template that reads from a price staging table
    -- (Investment.PriceUpdates_Staging) that would be populated by the
    -- external API integration.
    -- ===================================================================

    IF OBJECT_ID(N''Investment.PriceUpdates_Staging'', N''U'') IS NOT NULL
    BEGIN
        DECLARE @HoldingId BIGINT;
        DECLARE @NewPrice DECIMAL(18,4);
        DECLARE @NAVDate DATE;

        DECLARE price_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT HoldingId, CurrentPrice, PriceDate
            FROM Investment.PriceUpdates_Staging
            WHERE IsProcessed = 0;

        OPEN price_cursor;
        FETCH NEXT FROM price_cursor INTO @HoldingId, @NewPrice, @NAVDate;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            BEGIN TRY
                EXEC Investment.sp_UpdateHoldingPrice
                    @HoldingId    = @HoldingId,
                    @CurrentPrice = @NewPrice,
                    @NAVDate      = @NAVDate;

                SET @HoldingsUpdated = @HoldingsUpdated + 1;
            END TRY
            BEGIN CATCH
                PRINT N''Warning: Failed to update price for HoldingId '' + CAST(@HoldingId AS NVARCHAR(20)) + N'': '' + ERROR_MESSAGE();
            END CATCH

            FETCH NEXT FROM price_cursor INTO @HoldingId, @NewPrice, @NAVDate;
        END

        CLOSE price_cursor;
        DEALLOCATE price_cursor;

        -- Mark staging records as processed
        UPDATE Investment.PriceUpdates_Staging
        SET IsProcessed = 1,
            ProcessedAt = SYSUTCDATETIME()
        WHERE IsProcessed = 0;
    END
    ELSE
    BEGIN
        PRINT N''Price staging table not found. External API integration required.'';
        PRINT N''Create Investment.PriceUpdates_Staging to enable automated price updates.'';
    END

    -- Update gold prices placeholder (read from staging or API)
    -- In production, this would fetch from MCX / RBI / gold price APIs
    PRINT N''Gold price update: Integrate with price API to populate Investment.GoldPriceHistory'';

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @HoldingsUpdated;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS(N''local'', N''price_cursor'') >= 0
    BEGIN
        CLOSE price_cursor;
        DEALLOCATE price_cursor;
    END

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Failed'',
        @ErrorMessage = @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
',
    @database_name      = N'FinOS',
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 0,
    @retry_interval     = 0;
GO

-- ---------------------------------------------------------------------------
-- Step 4: Purge Old AI Conversation History (> 365 days)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_MonthlyProcessing',
    @step_name  = N'Purge Old AI Conversations',
    @step_id    = 4,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_MonthlyProcessing'',
    @StepName = N''Purge Old AI Conversations'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -365, SYSUTCDATETIME());
    DECLARE @DeletedMessages INT = 0;
    DECLARE @DeletedConversations INT = 0;

    -- Delete messages for old conversations first (FK constraint)
    DELETE FROM AI.AIMessages
    WHERE ConversationId IN (
        SELECT Id FROM AI.AIConversations
        WHERE UpdatedAt < @CutoffDate
    );
    SET @DeletedMessages = @@ROWCOUNT;

    -- Delete the old conversations
    DELETE FROM AI.AIConversations
    WHERE UpdatedAt < @CutoffDate;
    SET @DeletedConversations = @@ROWCOUNT;

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @DeletedMessages + @DeletedConversations;
END TRY
BEGIN CATCH
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Failed'',
        @ErrorMessage = @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
',
    @database_name      = N'FinOS',
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Step 5: Create Monthly Snapshot of Financial Data
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_MonthlyProcessing',
    @step_name  = N'Create Monthly Snapshot',
    @step_id    = 5,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @UsersProcessed INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_MonthlyProcessing'',
    @StepName = N''Create Monthly Snapshot'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    -- This step ensures a net worth snapshot exists for the last day of
    -- the previous month, and that monthly aggregates are fully populated
    -- for the completed month.
    DECLARE @PrevMonthStart DATE = DATEFROMPARTS(
        YEAR(DATEADD(MONTH, -1, SYSUTCDATETIME())),
        MONTH(DATEADD(MONTH, -1, SYSUTCDATETIME())),
        1
    );
    DECLARE @PrevMonthEnd DATE = EOMONTH(@PrevMonthStart);
    DECLARE @PrevYear INT = YEAR(@PrevMonthStart);
    DECLARE @PrevMonth INT = MONTH(@PrevMonthStart);

    DECLARE @UserId BIGINT;

    DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id
        FROM Security.Users
        WHERE IsActive = 1
          AND DeletedAt IS NULL;

    OPEN user_cursor;
    FETCH NEXT FROM user_cursor INTO @UserId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- Ensure net worth snapshot for end of previous month
            EXEC Analytics.sp_CalculateNetWorth
                @UserId       = @UserId,
                @SnapshotDate = @PrevMonthEnd;

            -- Generate/reconcile monthly aggregates for the completed month
            EXEC Analytics.sp_GenerateMonthlyAggregates
                @UserId = @UserId,
                @Year   = @PrevYear,
                @Month  = @PrevMonth;

            SET @UsersProcessed = @UsersProcessed + 1;
        END TRY
        BEGIN CATCH
            DECLARE @UserErr NVARCHAR(4000) = ERROR_MESSAGE();
            PRINT N''Warning: Failed to snapshot UserId '' + CAST(@UserId AS NVARCHAR(20)) + N'': '' + @UserErr;
        END CATCH

        FETCH NEXT FROM user_cursor INTO @UserId;
    END

    CLOSE user_cursor;
    DEALLOCATE user_cursor;

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @UsersProcessed;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS(N''local'', N''user_cursor'') >= 0
    BEGIN
        CLOSE user_cursor;
        DEALLOCATE user_cursor;
    END

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Failed'',
        @ErrorMessage = @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
',
    @database_name      = N'FinOS',
    @on_success_action  = 1,   -- Quit with success
    @on_fail_action     = 2,   -- Quit with failure
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Schedule: 1st of every month at 1:00 AM IST = 19:30 UTC previous day
-- 1:00 AM IST = 19:30 UTC (India is UTC+5:30)
-- active_start_time: 193000 = 19:30:00 UTC
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobschedule
    @job_name          = N'FinOS_MonthlyProcessing',
    @name              = N'Monthly 1st 1AM IST',
    @enabled           = 1,
    @freq_type         = 16,         -- Monthly
    @freq_interval     = 1,          -- On the 1st day of the month
    @freq_recurrence_factor = 1,     -- Every 1 month
    @freq_subday_type  = 1,          -- At the specified time
    @active_start_time = 193000;     -- 19:30 UTC = 1:00 AM IST (next day)
GO

-- ---------------------------------------------------------------------------
-- Assign the job to the local server
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobserver
    @job_name    = N'FinOS_MonthlyProcessing',
    @server_name = N'(LOCAL)';
GO

PRINT 'SQL Agent Job [FinOS_MonthlyProcessing] created successfully.';
PRINT 'Schedule: 1st of every month at 1:00 AM IST (19:30 UTC previous day)';
PRINT 'Steps: 1) Archive Old Goals, 2) Generate EPF Interest, 3) Update Investment Prices,';
PRINT '       4) Purge AI Conversations, 5) Create Monthly Snapshot';
GO
