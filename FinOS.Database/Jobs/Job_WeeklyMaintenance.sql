-- ============================================================================
-- FinOS Database - SQL Server Agent Job: Weekly Maintenance
-- Target: Microsoft SQL Server (SSMS)
-- Description: Weekly job that runs every Sunday at 3:00 AM IST (21:30 UTC Sat)
--              to calculate financial scores, rebuild fragmented indexes, update
--              statistics, and purge old data
-- ============================================================================

USE msdb;
GO

-- ---------------------------------------------------------------------------
-- Remove the job if it already exists (idempotent deployment)
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'FinOS_WeeklyMaintenance')
BEGIN
    EXEC msdb.dbo.sp_delete_job
        @job_name = N'FinOS_WeeklyMaintenance',
        @delete_unused_schedule = 1;
END
GO

-- ---------------------------------------------------------------------------
-- Create the Job
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_job
    @job_name           = N'FinOS_WeeklyMaintenance',
    @enabled            = 1,
    @description        = N'Weekly maintenance: calculates financial scores, rebuilds fragmented indexes, updates statistics, purges old audit logs, notifications, and import batches. Runs every Sunday at 3:00 AM IST.',
    @category_name      = N'Database Maintenance',
    @owner_login_name   = N'sa',
    @notify_level_eventlog = 2;   -- On failure
GO

-- ---------------------------------------------------------------------------
-- Step 1: Calculate Financial Score for All Active Users
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_WeeklyMaintenance',
    @step_name  = N'Calculate Financial Scores',
    @step_id    = 1,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @UsersProcessed INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_WeeklyMaintenance'',
    @StepName = N''Calculate Financial Scores'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @UserId BIGINT;
    DECLARE @ScoreDate DATE = CAST(SYSUTCDATETIME() AS DATE);

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
            EXEC Analytics.sp_CalculateFinancialScore
                @UserId    = @UserId,
                @ScoreDate = @ScoreDate;

            SET @UsersProcessed = @UsersProcessed + 1;
        END TRY
        BEGIN CATCH
            DECLARE @UserErr NVARCHAR(4000) = ERROR_MESSAGE();
            PRINT N''Warning: Failed to calculate financial score for UserId '' + CAST(@UserId AS NVARCHAR(20)) + N'': '' + @UserErr;
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
-- Step 2: Rebuild Fragmented Indexes (> 30% fragmentation)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_WeeklyMaintenance',
    @step_name  = N'Rebuild Fragmented Indexes',
    @step_id    = 2,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @IndexesRebuilt INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_WeeklyMaintenance'',
    @StepName = N''Rebuild Fragmented Indexes'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    -- Find indexes with > 30% fragmentation in the FinOS database
    DECLARE @RebuildSQL NVARCHAR(MAX) = N'''';
    DECLARE @SchemaName NVARCHAR(128);
    DECLARE @TableName  NVARCHAR(128);
    DECLARE @IndexName  NVARCHAR(128);
    DECLARE @FragPct    DECIMAL(5,2);

    DECLARE idx_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            s.name   AS SchemaName,
            t.name   AS TableName,
            i.name   AS IndexName,
            ips.avg_fragmentation_in_percent
        FROM sys.dm_db_index_physical_stats(
                DB_ID(N''FinOS''), NULL, NULL, NULL, N''LIMITED'') ips
        INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
        INNER JOIN sys.tables t ON i.object_id = t.object_id
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE ips.avg_fragmentation_in_percent > 30.0
          AND i.name IS NOT NULL               -- Skip heap
          AND i.is_primary_key = 0              -- Skip PK (handled separately if needed)
          AND ips.page_count > 1000;            -- Only indexes with meaningful size

    OPEN idx_cursor;
    FETCH NEXT FROM idx_cursor INTO @SchemaName, @TableName, @IndexName, @FragPct;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @SQL NVARCHAR(MAX) = N''ALTER INDEX ['' + @IndexName + N''] ON ['' + @SchemaName + N''].['' + @TableName + N''] REBUILD WITH (ONLINE = ON, SORT_IN_TEMPDB = ON);'';

        BEGIN TRY
            EXEC sp_executesql @SQL;
            SET @IndexesRebuilt = @IndexesRebuilt + 1;
            PRINT N''Rebuilt: ['' + @SchemaName + N''].['' + @TableName + N''].['' + @IndexName + N''] ('' + CAST(@FragPct AS NVARCHAR(10)) + N''% fragmented)'';
        END TRY
        BEGIN CATCH
            PRINT N''Warning: Failed to rebuild ['' + @SchemaName + N''].['' + @TableName + N''].['' + @IndexName + N'']: '' + ERROR_MESSAGE();
            -- Try offline rebuild as fallback
            BEGIN TRY
                SET @SQL = N''ALTER INDEX ['' + @IndexName + N''] ON ['' + @SchemaName + N''].['' + @TableName + N''] REBUILD WITH (SORT_IN_TEMPDB = ON);'';
                EXEC sp_executesql @SQL;
                SET @IndexesRebuilt = @IndexesRebuilt + 1;
                PRINT N''Rebuilt (offline fallback): ['' + @SchemaName + N''].['' + @TableName + N''].['' + @IndexName + N'']'';
            END TRY
            BEGIN CATCH
                PRINT N''Error: Could not rebuild ['' + @SchemaName + N''].['' + @TableName + N''].['' + @IndexName + N'']: '' + ERROR_MESSAGE();
            END CATCH
        END CATCH

        FETCH NEXT FROM idx_cursor INTO @SchemaName, @TableName, @IndexName, @FragPct;
    END

    CLOSE idx_cursor;
    DEALLOCATE idx_cursor;

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @IndexesRebuilt;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS(N''local'', N''idx_cursor'') >= 0
    BEGIN
        CLOSE idx_cursor;
        DEALLOCATE idx_cursor;
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
-- Step 3: Update Statistics on All User Tables
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_WeeklyMaintenance',
    @step_name  = N'Update Statistics',
    @step_id    = 3,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;
DECLARE @TablesUpdated INT = 0;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_WeeklyMaintenance'',
    @StepName = N''Update Statistics'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @SchemaName NVARCHAR(128);
    DECLARE @TableName  NVARCHAR(128);
    DECLARE @SQL        NVARCHAR(MAX);

    DECLARE tbl_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT s.name, t.name
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE s.name IN (N''Security'', N''Core'', N''Budget'', N''Investment'', N''Loan'', N''Goals'', N''Analytics'', N''AI'', N''Notifications'', N''Subscriptions'', N''Import'', N''dbo'')
        ORDER BY s.name, t.name;

    OPEN tbl_cursor;
    FETCH NEXT FROM tbl_cursor INTO @SchemaName, @TableName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @SQL = N''UPDATE STATISTICS ['' + @SchemaName + N''].['' + @TableName + N''] WITH FULLSCAN;'';

        BEGIN TRY
            EXEC sp_executesql @SQL;
            SET @TablesUpdated = @TablesUpdated + 1;
        END TRY
        BEGIN CATCH
            PRINT N''Warning: Failed to update stats for ['' + @SchemaName + N''].['' + @TableName + N'']: '' + ERROR_MESSAGE();
        END CATCH

        FETCH NEXT FROM tbl_cursor INTO @SchemaName, @TableName;
    END

    CLOSE tbl_cursor;
    DEALLOCATE tbl_cursor;

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @TablesUpdated;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS(N''local'', N''tbl_cursor'') >= 0
    BEGIN
        CLOSE tbl_cursor;
        DEALLOCATE tbl_cursor;
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
-- Step 4: Purge Old Audit Logs (> 90 days)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_WeeklyMaintenance',
    @step_name  = N'Purge Old Audit Logs',
    @step_id    = 4,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_WeeklyMaintenance'',
    @StepName = N''Purge Old Audit Logs'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -90, SYSUTCDATETIME());
    DECLARE @DeletedCount INT = 0;

    -- Delete in batches to avoid transaction log bloat
    WHILE 1 = 1
    BEGIN
        DELETE TOP (5000) FROM Security.AuditLog
        WHERE CreatedAt < @CutoffDate;

        IF @@ROWCOUNT = 0 BREAK;
        SET @DeletedCount = @DeletedCount + @@ROWCOUNT;
    END

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @DeletedCount;
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
-- Step 5: Purge Old Notifications (> 180 days, already read)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_WeeklyMaintenance',
    @step_name  = N'Purge Old Read Notifications',
    @step_id    = 5,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_WeeklyMaintenance'',
    @StepName = N''Purge Old Read Notifications'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -180, SYSUTCDATETIME());
    DECLARE @DeletedCount INT = 0;

    -- Delete read notifications older than 180 days
    WHILE 1 = 1
    BEGIN
        DELETE TOP (5000) FROM Notifications.Notifications
        WHERE CreatedAt < @CutoffDate
          AND IsRead = 1;

        IF @@ROWCOUNT = 0 BREAK;
        SET @DeletedCount = @DeletedCount + @@ROWCOUNT;
    END

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @DeletedCount;
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
-- Step 6: Purge Expired Import Batches and Errors (> 30 days)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_WeeklyMaintenance',
    @step_name  = N'Purge Old Import Batches',
    @step_id    = 6,
    @subsystem  = N'TSQL',
    @command    = N'
DECLARE @LogId BIGINT;

EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_WeeklyMaintenance'',
    @StepName = N''Purge Old Import Batches'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -30, SYSUTCDATETIME());
    DECLARE @DeletedBatches INT = 0;
    DECLARE @DeletedErrors INT = 0;

    -- Delete import errors for expired batches first (FK constraint)
    DELETE FROM Import.ImportErrors
    WHERE BatchId IN (
        SELECT Id FROM Import.ImportBatches
        WHERE CreatedAt < @CutoffDate
          AND Status IN (N''Completed'', N''Failed'')
    );
    SET @DeletedErrors = @@ROWCOUNT;

    -- Delete the expired import batches
    DELETE FROM Import.ImportBatches
    WHERE CreatedAt < @CutoffDate
      AND Status IN (N''Completed'', N''Failed'');
    SET @DeletedBatches = @@ROWCOUNT;

    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @EndTime      = SYSUTCDATETIME(),
        @Status       = N''Succeeded'',
        @RowsAffected = @DeletedBatches + @DeletedErrors;
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
    @on_success_action  = 1,   -- Quit with success
    @on_fail_action     = 2,   -- Quit with failure
    @retry_attempts     = 1,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Schedule: Every Sunday at 3:00 AM IST = 21:30 UTC Saturday
-- 3:00 AM IST = 21:30 UTC (India is UTC+5:30)
-- active_start_time: 213000 = 21:30:00 UTC
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobschedule
    @job_name          = N'FinOS_WeeklyMaintenance',
    @name              = N'Sunday 3AM IST',
    @enabled           = 1,
    @freq_type         = 8,          -- Weekly
    @freq_interval     = 1,          -- Sunday (1 = Sunday in SQL Server weekly schedule)
    @freq_recurrence_factor = 1,     -- Every 1 week
    @freq_subday_type  = 1,          -- At the specified time
    @active_start_time = 213000;     -- 21:30 UTC = 3:00 AM IST (next day, Sunday)
GO

-- ---------------------------------------------------------------------------
-- Assign the job to the local server
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobserver
    @job_name    = N'FinOS_WeeklyMaintenance',
    @server_name = N'(LOCAL)';
GO

PRINT 'SQL Agent Job [FinOS_WeeklyMaintenance] created successfully.';
PRINT 'Schedule: Every Sunday at 3:00 AM IST (21:30 UTC Saturday)';
PRINT 'Steps: 1) Financial Scores, 2) Rebuild Indexes, 3) Update Stats,';
PRINT '       4) Purge Audit Logs, 5) Purge Notifications, 6) Purge Import Batches';
GO
