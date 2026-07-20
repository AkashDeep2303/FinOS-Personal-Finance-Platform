-- ============================================================================
-- FinOS Database - SQL Server Agent Job: Recurring Transactions Processing
-- Target: Microsoft SQL Server (SSMS)
-- Description: Daily job that runs at 6:00 AM IST (00:30 UTC) to process
--              recurring transactions, SIP installments, and check overdue EMIs
-- ============================================================================

USE msdb;
GO

-- ---------------------------------------------------------------------------
-- Step 0: Remove the job if it already exists (idempotent deployment)
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'FinOS_RecurringTransactions')
BEGIN
    EXEC msdb.dbo.sp_delete_job
        @job_name = N'FinOS_RecurringTransactions',
        @delete_unused_schedule = 1;
END
GO

-- ---------------------------------------------------------------------------
-- Create the Job
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_job
    @job_name           = N'FinOS_RecurringTransactions',
    @enabled            = 1,
    @description        = N'Processes due recurring transactions, SIP installments, and checks for overdue EMIs. Runs daily at 6:00 AM IST.',
    @category_name      = N'Database Maintenance',
    @owner_login_name   = N'sa',
    @notify_level_eventlog = 2;   -- On failure
GO

-- ---------------------------------------------------------------------------
-- Step 1: Process Recurring Transactions
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_RecurringTransactions',
    @step_name  = N'Process Recurring Transactions',
    @step_id    = 1,
    @subsystem  = N'TSQL',
    @command    = N'
-- Log start
DECLARE @LogId BIGINT;
EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_RecurringTransactions'',
    @StepName = N''Process Recurring Transactions'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    -- Process all due recurring schedules
    EXEC Core.sp_ProcessRecurringTransactions
        @AsOfDate = NULL;  -- Defaults to today

    -- Log success
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId    = @LogId,
        @Status   = N''Succeeded'',
        @RowsAffected = @@ROWCOUNT;
END TRY
BEGIN CATCH
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @Status       = N''Failed'',
        @ErrorMessage = @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
',
    @database_name      = N'FinOS',
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step (continue to process other items)
    @retry_attempts     = 2,
    @retry_interval     = 5;   -- Retry after 5 minutes
GO

-- ---------------------------------------------------------------------------
-- Step 2: Process SIP Installments
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_RecurringTransactions',
    @step_name  = N'Process SIP Installments',
    @step_id    = 2,
    @subsystem  = N'TSQL',
    @command    = N'
-- Log start
DECLARE @LogId BIGINT;
EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_RecurringTransactions'',
    @StepName = N''Process SIP Installments'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    -- Process all due SIP installments
    EXEC Investment.sp_ProcessSIPInstallments
        @AsOfDate = NULL;  -- Defaults to today

    -- Log success
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId    = @LogId,
        @Status   = N''Succeeded'',
        @RowsAffected = @@ROWCOUNT;
END TRY
BEGIN CATCH
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @Status       = N''Failed'',
        @ErrorMessage = @ErrMsg;
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
',
    @database_name      = N'FinOS',
    @on_success_action  = 3,   -- Go to next step
    @on_fail_action     = 3,   -- Go to next step
    @retry_attempts     = 2,
    @retry_interval     = 5;
GO

-- ---------------------------------------------------------------------------
-- Step 3: Check Overdue EMIs and Create Notifications
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobstep
    @job_name   = N'FinOS_RecurringTransactions',
    @step_name  = N'Check Overdue EMIs',
    @step_id    = 3,
    @subsystem  = N'TSQL',
    @command    = N'
-- Log start
DECLARE @LogId BIGINT;
EXEC dbo.sp_LogJobExecution
    @JobName  = N''FinOS_RecurringTransactions'',
    @StepName = N''Check Overdue EMIs'',
    @Status   = N''Running'',
    @LogId    = @LogId OUTPUT;

BEGIN TRY
    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @NotificationsCreated INT = 0;

    -- Find EMI schedule entries that are past due (EMIDate < today) and not paid
    -- These are overdue EMIs
    DECLARE @OverdueEMI TABLE
    (
        EMIId              BIGINT,
        LoanId             BIGINT,
        UserId             BIGINT,
        EMINumber          INT,
        EMIDate            DATE,
        EMIAmount          DECIMAL(18,2),
        DaysOverdue        INT
    );

    INSERT INTO @OverdueEMI (EMIId, LoanId, UserId, EMINumber, EMIDate, EMIAmount, DaysOverdue)
    SELECT
        e.Id,
        e.LoanId,
        l.UserId,
        e.EMINumber,
        e.EMIDate,
        e.EMIAmount,
        DATEDIFF(DAY, e.EMIDate, @Today)
    FROM Loan.EMISchedule e
    INNER JOIN Loan.Loans l ON e.LoanId = l.Id
    WHERE e.IsPaid = 0
      AND e.EMIDate < @Today
      AND l.Status = N''Active''
      AND l.DeletedAt IS NULL;

    -- Get or create the notification type for overdue EMI
    DECLARE @OverdueNotifTypeId INT;
    SELECT @OverdueNotifTypeId = Id
    FROM Notifications.NotificationTypes
    WHERE Name = N''EMIOverdue'';

    IF @OverdueNotifTypeId IS NULL
    BEGIN
        INSERT INTO Notifications.NotificationTypes (Name, Description, Category, IsEnabled)
        VALUES (N''EMIOverdue'', N''Notification for overdue EMI payments'', N''Loan'', 1);
        SET @OverdueNotifTypeId = SCOPE_IDENTITY();
    END

    -- Create notification for each user with overdue EMIs
    -- (one notification per user, not per EMI, to avoid spamming)
    DECLARE @UserId       BIGINT;
    DECLARE @OverdueCount INT;
    DECLARE @TotalOverdue DECIMAL(18,2);

    DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            UserId,
            COUNT(*),
            SUM(EMIAmount)
        FROM @OverdueEMI
        GROUP BY UserId;

    OPEN user_cursor;
    FETCH NEXT FROM user_cursor INTO @UserId, @OverdueCount, @TotalOverdue;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Avoid duplicate notifications: check if one already exists for this user today
        IF NOT EXISTS (
            SELECT 1 FROM Notifications.Notifications
            WHERE UserId = @UserId
              AND NotificationTypeId = @OverdueNotifTypeId
              AND CAST(CreatedAt AS DATE) = @Today
        )
        BEGIN
            INSERT INTO Notifications.Notifications
                (UserId, NotificationTypeId, Title, Message, EntityType, DeliveryChannel, DeliveryStatus)
            VALUES (
                @UserId,
                @OverdueNotifTypeId,
                N''EMI Payment Overdue'',
                N''You have '' + CAST(@OverdueCount AS NVARCHAR(10)) + N'' overdue EMI(s) totaling ₹''
                    + CAST(@TotalOverdue AS NVARCHAR(50)) + N''. Please make the payment to avoid late fees.'',
                N''Loan'',
                N''InApp'',
                N''Pending''
            );

            SET @NotificationsCreated = @NotificationsCreated + 1;
        END

        FETCH NEXT FROM user_cursor INTO @UserId, @OverdueCount, @TotalOverdue;
    END

    CLOSE user_cursor;
    DEALLOCATE user_cursor;

    -- Log success
    EXEC dbo.sp_UpdateJobExecutionLog
        @LogId        = @LogId,
        @Status       = N''Succeeded'',
        @RowsAffected = @NotificationsCreated;
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
-- Schedule: Daily at 6:00 AM IST = 00:30 UTC (India is UTC+5:30)
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobschedule
    @job_name          = N'FinOS_RecurringTransactions',
    @name              = N'Daily 6AM IST',
    @enabled           = 1,
    @freq_type         = 4,          -- Daily
    @freq_interval     = 1,          -- Every 1 day
    @freq_subday_type  = 1,          -- At the specified time
    @active_start_time = 30 | 0;     -- 00:30:00 = 00:30 UTC = 6:00 AM IST
GO

-- ---------------------------------------------------------------------------
-- Assign the job to the local server
-- ---------------------------------------------------------------------------
EXEC msdb.dbo.sp_add_jobserver
    @job_name    = N'FinOS_RecurringTransactions',
    @server_name = N'(LOCAL)';
GO

PRINT 'SQL Agent Job [FinOS_RecurringTransactions] created successfully.';
PRINT 'Schedule: Daily at 6:00 AM IST (00:30 UTC)';
PRINT 'Steps: 1) Process Recurring Transactions, 2) Process SIP Installments, 3) Check Overdue EMIs';
GO
