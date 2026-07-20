-- ============================================================================
-- FinOS Database - Job Execution Log Infrastructure
-- Target: Microsoft SQL Server (SSMS)
-- Description: Table, stored procedure, and view for logging SQL Server Agent
--              job execution details across all FinOS scheduled jobs
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Table: dbo.JobExecutionLog
-- Description: Central log table for all SQL Server Agent job executions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.JobExecutionLog', N'U') IS NOT NULL
    DROP TABLE dbo.JobExecutionLog;
GO

CREATE TABLE dbo.JobExecutionLog
(
    Id              BIGINT          IDENTITY(1,1)   NOT NULL,
    JobName         NVARCHAR(200)                   NOT NULL,   -- Name of the SQL Agent job
    StepName        NVARCHAR(200)                   NOT NULL,   -- Step within the job
    StartTime       DATETIME2                       NOT NULL,
    EndTime         DATETIME2                       NULL,
    DurationMs      INT                             NULL,       -- Computed: DATEDIFF(ms, StartTime, EndTime)
    Status          NVARCHAR(20)                    NOT NULL,   -- Running, Succeeded, Failed, Warning
    RowsAffected    INT                             NULL,
    ErrorMessage    NVARCHAR(4000)                  NULL,

    CONSTRAINT PK_JobExecutionLog PRIMARY KEY CLUSTERED (Id) ON FinOS_Data
);

CREATE NONCLUSTERED INDEX IX_JobExecutionLog_JobName
    ON dbo.JobExecutionLog (JobName, StartTime DESC) ON FinOS_Index;

CREATE NONCLUSTERED INDEX IX_JobExecutionLog_Status
    ON dbo.JobExecutionLog (Status, StartTime DESC) ON FinOS_Index;

CREATE NONCLUSTERED INDEX IX_JobExecutionLog_StartTime
    ON dbo.JobExecutionLog (StartTime DESC) ON FinOS_Index;
GO

-- ---------------------------------------------------------------------------
-- SP: dbo.sp_LogJobExecution
-- Description: Insert a record into JobExecutionLog. Returns the new Id.
--              Can be called at the start (Status='Running') and updated
--              at the end, or called once at the end with full details.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_LogJobExecution', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_LogJobExecution;
GO

CREATE PROCEDURE dbo.sp_LogJobExecution
    @JobName        NVARCHAR(200),
    @StepName       NVARCHAR(200),
    @StartTime      DATETIME2       = NULL,    -- Defaults to SYSUTCDATETIME()
    @EndTime        DATETIME2       = NULL,
    @DurationMs     INT             = NULL,
    @Status         NVARCHAR(20)    = N'Running',  -- Running, Succeeded, Failed, Warning
    @RowsAffected   INT             = NULL,
    @ErrorMessage   NVARCHAR(4000)  = NULL,
    @LogId          BIGINT          = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate required fields
        IF @JobName IS NULL OR LTRIM(RTRIM(@JobName)) = N''
        BEGIN
            RAISERROR('JobName is required.', 16, 1);
            RETURN;
        END

        IF @StepName IS NULL OR LTRIM(RTRIM(@StepName)) = N''
        BEGIN
            RAISERROR('StepName is required.', 16, 1);
            RETURN;
        END

        -- Validate status
        IF @Status NOT IN (N'Running', N'Succeeded', N'Failed', N'Warning')
        BEGIN
            RAISERROR('Status must be Running, Succeeded, Failed, or Warning.', 16, 1);
            RETURN;
        END

        -- Default StartTime to now
        IF @StartTime IS NULL
            SET @StartTime = SYSUTCDATETIME();

        -- Calculate DurationMs if both times provided and not explicitly set
        IF @EndTime IS NOT NULL AND @DurationMs IS NULL
            SET @DurationMs = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

        -- Insert the log entry
        INSERT INTO dbo.JobExecutionLog
            (JobName, StepName, StartTime, EndTime, DurationMs, Status, RowsAffected, ErrorMessage)
        VALUES
            (@JobName, @StepName, @StartTime, @EndTime, @DurationMs, @Status, @RowsAffected, @ErrorMessage);

        SET @LogId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH
        DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
        -- Don't fail the job just because logging failed; print the error
        PRINT N'Warning: Failed to log job execution: ' + @Err;
        SET @LogId = NULL;
    END CATCH;
END;
GO

-- ---------------------------------------------------------------------------
-- SP: dbo.sp_UpdateJobExecutionLog
-- Description: Update an existing log entry (e.g., to mark completion)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_UpdateJobExecutionLog', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateJobExecutionLog;
GO

CREATE PROCEDURE dbo.sp_UpdateJobExecutionLog
    @LogId          BIGINT,
    @EndTime        DATETIME2       = NULL,
    @DurationMs     INT             = NULL,
    @Status         NVARCHAR(20)    = NULL,
    @RowsAffected   INT             = NULL,
    @ErrorMessage   NVARCHAR(4000)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @LogId IS NULL
        BEGIN
            RAISERROR('LogId is required.', 16, 1);
            RETURN;
        END

        -- Get StartTime for DurationMs calculation
        DECLARE @StartTime DATETIME2;
        SELECT @StartTime = StartTime FROM dbo.JobExecutionLog WHERE Id = @LogId;

        IF @StartTime IS NULL
        BEGIN
            RAISERROR('Log entry with Id %lld not found.', 16, 1, @LogId);
            RETURN;
        END

        -- Default EndTime to now
        IF @EndTime IS NULL
            SET @EndTime = SYSUTCDATETIME();

        -- Calculate DurationMs if not provided
        IF @DurationMs IS NULL
            SET @DurationMs = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

        -- Build dynamic UPDATE (only update provided columns)
        UPDATE dbo.JobExecutionLog
        SET
            EndTime      = @EndTime,
            DurationMs   = @DurationMs,
            Status       = ISNULL(@Status, Status),
            RowsAffected = ISNULL(@RowsAffected, RowsAffected),
            ErrorMessage = ISNULL(@ErrorMessage, ErrorMessage)
        WHERE Id = @LogId;
    END TRY
    BEGIN CATCH
        DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
        PRINT N'Warning: Failed to update job execution log: ' + @Err;
    END CATCH;
END;
GO

-- ---------------------------------------------------------------------------
-- View: dbo.vw_JobExecutionHistory
-- Description: Recent job execution history with computed duration and status
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.vw_JobExecutionHistory', N'V') IS NOT NULL
    DROP VIEW dbo.vw_JobExecutionHistory;
GO

CREATE VIEW dbo.vw_JobExecutionHistory
AS
SELECT
    Id,
    JobName,
    StepName,
    StartTime,
    EndTime,
    DurationMs,
    CASE
        WHEN DurationMs < 1000 THEN CAST(DurationMs AS NVARCHAR(20)) + N' ms'
        WHEN DurationMs < 60000 THEN CAST(DurationMs / 1000 AS NVARCHAR(20)) + N' sec'
        WHEN DurationMs < 3600000 THEN CAST(DurationMs / 60000 AS NVARCHAR(20)) + N' min '
            + CAST((DurationMs % 60000) / 1000 AS NVARCHAR(20)) + N' sec'
        ELSE CAST(DurationMs / 3600000 AS NVARCHAR(20)) + N' hr '
            + CAST((DurationMs % 3600000) / 60000 AS NVARCHAR(20)) + N' min'
    END AS DurationReadable,
    Status,
    RowsAffected,
    ErrorMessage,
    -- Flag recent failures (within last 24 hours)
    CASE
        WHEN Status = N'Failed' AND StartTime >= DATEADD(HOUR, -24, SYSUTCDATETIME())
        THEN 1
        ELSE 0
    END AS IsRecentFailure,
    -- Day of week (useful for weekly job analysis)
    DATENAME(WEEKDAY, StartTime) AS StartDayOfWeek,
    -- IST time offset (UTC+5:30)
    DATEADD(MINUTE, 330, StartTime) AS StartTimeIST,
    DATEADD(MINUTE, 330, EndTime) AS EndTimeIST
FROM dbo.JobExecutionLog
WHERE StartTime >= DATEADD(DAY, -90, SYSUTCDATETIME());  -- Only show last 90 days
GO

-- ---------------------------------------------------------------------------
-- SP: dbo.sp_GetJobExecutionSummary
-- Description: Summary of job executions for a given period
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.sp_GetJobExecutionSummary', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetJobExecutionSummary;
GO

CREATE PROCEDURE dbo.sp_GetJobExecutionSummary
    @DaysBack INT = 7    -- Look back N days
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff DATETIME2 = DATEADD(DAY, -@DaysBack, SYSUTCDATETIME());

    -- Per-job summary
    SELECT
        JobName,
        COUNT(*)                                        AS TotalExecutions,
        SUM(CASE WHEN Status = N'Succeeded' THEN 1 ELSE 0 END) AS SuccessCount,
        SUM(CASE WHEN Status = N'Failed'    THEN 1 ELSE 0 END) AS FailureCount,
        SUM(CASE WHEN Status = N'Warning'   THEN 1 ELSE 0 END) AS WarningCount,
        AVG(DurationMs)                                 AS AvgDurationMs,
        MIN(DurationMs)                                 AS MinDurationMs,
        MAX(DurationMs)                                 AS MaxDurationMs,
        MAX(StartTime)                                  AS LastRunTime,
        MAX(CASE WHEN Status = N'Failed' THEN StartTime ELSE NULL END) AS LastFailureTime
    FROM dbo.JobExecutionLog
    WHERE StartTime >= @Cutoff
      AND Status <> N'Running'
    GROUP BY JobName
    ORDER BY JobName;

    -- Recent failures detail
    SELECT
        JobName,
        StepName,
        StartTime,
        EndTime,
        DurationMs,
        ErrorMessage
    FROM dbo.JobExecutionLog
    WHERE StartTime >= @Cutoff
      AND Status = N'Failed'
    ORDER BY StartTime DESC;
END;
GO

PRINT 'Job execution log infrastructure created successfully.';
GO
