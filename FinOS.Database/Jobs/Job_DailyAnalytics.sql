-- ============================================================================
-- FinOS Database - SQL Server Agent Job: Daily Analytics Processing
-- Target: Microsoft SQL Server (SSMS)
-- Description: Daily job that runs at 2:00 AM IST (20:30 UTC previous day) to
--              calculate net worth, generate monthly aggregates, check budget
--              alerts, and detect subscriptions (weekly on Sundays)
-- ============================================================================

USE msdb;
GO

-- ---------------------------------------------------------------------------
-- Remove the job if it already exists (idempotent deployment)
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'FinOS_DailyAnalytics')
BEGIN
    EXEC msdb.dbo.sp_delete_job
        @job_name = N'FinOS_DailyAnalytics',
        @delete_unused_schedule = 1;
END
GO

-- ---------------------------------------------------------------------------
-- Create the Job
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_job
    @job_name           = N'FinOS_DailyAnalytics',
    @enabled            = 1,
    @description        = N'Calculates net worth snapshots, generates monthly aggregates, checks budget alerts, and detects subscriptions (Sundays). Runs daily at 2:00 AM IST.',
    @category_name      = N'Database Maintenance',
    @owner_login_name   = N'sa',
    @notify_level_eventlog = 2;   -- On failure
GO

-- ---------------------------------------------------------------------------
-- Step 1: Calculate Net Worth for All Active Users
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_DailyAnalytics',
    @step_name  = N'Calculate Net Worth Snapshots',
    @step_id    = 1,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
DECLARE @UsersProcessed INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_DailyAnalytics'',
    @StepName = N''Calculate Net Worth Snapshots'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @UserId BIGINT;
    DECLARE @SnapshotDate DATE = CAST(SYSUTCDATETIME() AS DATE);

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
            EXEC Analytics.sp_CalculateNetWorth
                @UserId       = @UserId,
                @SnapshotDate = @SnapshotDate;

            SET @UsersProcessed = @UsersProcessed + 1;
        END TRY
        BEGIN CATCH
            -- Log individual user failure but continue with others
            DECLARE @UserErr NVARCHAR(4000) = ERROR_MESSAGE();
            PRINT N''Warning: Failed to calculate net worth for UserId '' + CAST(@UserId AS NVARCHAR(20)) + N'': '' + @UserErr;
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
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Step 2: Generate Monthly Aggregates for Current Month
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_DailyAnalytics',
    @step_name  = N'Generate Monthly Aggregates',
    @step_id    = 2,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @UsersProcessed INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_DailyAnalytics'',
    @StepName = N''Generate Monthly Aggregates'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @CurrentYear INT  = YEAR(SYSUTCDATETIME());
    DECLARE @CurrentMonth INT = MONTH(SYSUTCDATETIME());
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
            EXEC Analytics.sp_GenerateMonthlyAggregates
                @UserId = @UserId,
                @Year   = @CurrentYear,
                @Month  = @CurrentMonth;

            SET @UsersProcessed = @UsersProcessed + 1;
        END TRY
        BEGIN CATCH
            DECLARE @UserErr NVARCHAR(4000) = ERROR_MESSAGE();
            PRINT N''Warning: Failed to generate aggregates for UserId '' + CAST(@UserId AS NVARCHAR(20)) + N'': '' + @UserErr;
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
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Step 3: Check Budget Alerts for All Active Budgets
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_DailyAnalytics',
    @step_name  = N'Check Budget Alerts',
    @step_id    = 3,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @UsersProcessed INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_DailyAnalytics'',
    @StepName = N''Check Budget Alerts'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @UserId BIGINT;

    -- Get distinct users with active budgets in the current period
    DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT UserId
        FROM Budget.Budgets
        WHERE IsActive = 1
          AND DeletedAt IS NULL
          AND CAST(SYSUTCDATETIME() AS DATE) BETWEEN StartDate AND EndDate;

    OPEN user_cursor;
    FETCH NEXT FROM user_cursor INTO @UserId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            EXEC Budget.sp_CheckBudgetAlerts
                @UserId = @UserId;

            SET @UsersProcessed = @UsersProcessed + 1;
        END TRY
        BEGIN CATCH
            DECLARE @UserErr NVARCHAR(4000) = ERROR_MESSAGE();
            PRINT N''Warning: Failed to check budget alerts for UserId '' + CAST(@UserId AS NVARCHAR(20)) + N'': '' + @UserErr;
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
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Step 4: Detect Subscriptions (Sundays Only)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_DailyAnalytics',
    @step_name  = N'Detect Subscriptions (Sundays Only)',
    @step_id    = 4,
    @subsystem  = N'TSQL',
    @command    = N'
-- Only run on Sundays
IF DATENAME(WEEKDAY, SYSUTCDATETIME()) = N''Sunday''
BEGIN
    DECLARE @LogId BIGINT;

    EXEC dbo.sp_LogJobExecution
        @JobName  = N''FinOS_DailyAnalytics'',
        @StepName = N''Detect Subscriptions (Sundays Only)'',
        @Status   = N''Running'',
        @LogId    = @LogId OUTPUT;

    BEGIN TRY
        EXEC Core.sp_DetectSubscriptions;

        EXEC dbo.sp_UpdateJobExecutionLog
            @LogId        = @LogId,
            @EndTime      = SYSUTCDATETIME(),
            @Status       = N''Succeeded'',
            @RowsAffected = @@ROWCOUNT;
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
END
ELSE
BEGIN
    PRINT N''Subscription detection skipped - today is not Sunday.'';
END
',
    @database_name      = N'FinOS',
    @on_success_action  = 1,   -- Quit with success
    @on_fail_action     = 2,   -- Quit with failure
    @retry_attempts     = 0,
    @retry_interval     = 0;
GO

-- ---------------------------------------------------------------------------
-- Schedule: Daily at 2:00 AM IST = 20:30 UTC previous day
-- 2:00 AM IST = 20:30 UTC (India is UTC+5:30)
-- active_start_time is in HHMMSS format: 203000 = 20:30:00 UTC
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobschedule
    @job_name          = N'FinOS_DailyAnalytics',
    @name              = N'Daily 2AM IST',
    @enabled           = 1,
    @freq_type         = 4,          -- Daily
    @freq_interval     = 1,          -- Every 1 day
    @freq_subday_type  = 1,          -- At the specified time
    @active_start_time = 203000;     -- 20:30 UTC = 2:00 AM IST (next day)
GO

-- ---------------------------------------------------------------------------
-- Assign the job to the local server
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobserver
    @job_name    = N'FinOS_DailyAnalytics',
    @server_name = N'(LOCAL)';
GO

PRINT 'SQL Agent Job [FinOS_DailyAnalytics] created successfully.';
PRINT 'Schedule: Daily at 2:00 AM IST (20:30 UTC)';
PRINT 'Steps: 1) Net Worth Snapshots, 2) Monthly Aggregates, 3) Budget Alerts, 4) Subscription Detection (Sundays)';
GO
