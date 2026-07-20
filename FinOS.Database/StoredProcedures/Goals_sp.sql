-- ============================================================================
-- FinOS Database - Goals Stored Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for financial goals, contributions,
--              and projection calculations
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- SP: Goals.sp_CreateGoal
-- Description: Insert a new financial goal
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Goals.sp_CreateGoal', N'P') IS NOT NULL
    DROP PROCEDURE Goals.sp_CreateGoal;
GO

CREATE PROCEDURE Goals.sp_CreateGoal
    @UserId               BIGINT,
    @GoalTemplateId       INT              = NULL,
    @Name                 NVARCHAR(100),
    @Description          NVARCHAR(500)    = NULL,
    @Category             NVARCHAR(50),       -- Emergency, Retirement, Travel, Purchase, Education, Wedding
    @TargetAmount         DECIMAL(18,2),
    @CurrentAmount        DECIMAL(18,2)    = 0,
    @MonthlyContribution  DECIMAL(18,2)    = NULL,
    @StartDate            DATE,
    @TargetDate           DATE,
    @Priority             NVARCHAR(10)     = N'Medium',   -- Low, Medium, High, Critical
    @LinkedAccountIds     NVARCHAR(MAX)    = NULL,         -- JSON array
    @Icon                 NVARCHAR(50)     = NULL,
    @Color                NVARCHAR(7)      = NULL,
    @IsAutoContribute     BIT              = 0,
    @NewGoalId            BIGINT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate user
        IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Id = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('User with Id %d does not exist.', 16, 1, @UserId);
            RETURN;
        END

        -- Validate required fields
        IF @Name IS NULL OR LTRIM(RTRIM(@Name)) = N''
        BEGIN
            RAISERROR('Goal name is required.', 16, 1);
            RETURN;
        END

        IF @TargetAmount <= 0
        BEGIN
            RAISERROR('Target amount must be greater than zero.', 16, 1);
            RETURN;
        END

        IF @CurrentAmount < 0
        BEGIN
            RAISERROR('Current amount cannot be negative.', 16, 1);
            RETURN;
        END

        IF @TargetDate <= @StartDate
        BEGIN
            RAISERROR('Target date must be after start date.', 16, 1);
            RETURN;
        END

        -- Validate category
        IF @Category NOT IN (N'Emergency', N'Retirement', N'Travel', N'Purchase', N'Education', N'Wedding', N'Other')
        BEGIN
            RAISERROR('Invalid goal category. Must be Emergency, Retirement, Travel, Purchase, Education, Wedding, or Other.', 16, 1);
            RETURN;
        END

        -- Validate priority
        IF @Priority NOT IN (N'Low', N'Medium', N'High', N'Critical')
        BEGIN
            RAISERROR('Priority must be Low, Medium, High, or Critical.', 16, 1);
            RETURN;
        END

        -- Validate goal template if specified
        IF @GoalTemplateId IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM Goals.GoalTemplates WHERE Id = @GoalTemplateId)
            BEGIN
                RAISERROR('GoalTemplate with Id %d does not exist.', 16, 1, @GoalTemplateId);
                RETURN;
            END
        END

        -- Calculate projected date based on monthly contribution
        DECLARE @ProjectedDate DATE = NULL;

        IF @MonthlyContribution IS NOT NULL AND @MonthlyContribution > 0
        BEGIN
            DECLARE @RemainingAmount DECIMAL(18,2) = @TargetAmount - @CurrentAmount;
            IF @RemainingAmount > 0
            BEGIN
                DECLARE @MonthsNeeded INT = CEILING(@RemainingAmount / @MonthlyContribution);
                SET @ProjectedDate = DATEADD(MONTH, @MonthsNeeded, @StartDate);
            END
            ELSE
            BEGIN
                -- Already reached
                SET @ProjectedDate = @StartDate;
            END
        END

        -- Insert the goal
        INSERT INTO Goals.Goals
        (
            UserId, GoalTemplateId, Name, Description, Category,
            TargetAmount, CurrentAmount, MonthlyContribution,
            StartDate, TargetDate, Priority,
            LinkedAccountIds, Icon, Color, IsAutoContribute, ProjectedDate
        )
        VALUES
        (
            @UserId, @GoalTemplateId, @Name, @Description, @Category,
            @TargetAmount, @CurrentAmount, @MonthlyContribution,
            @StartDate, @TargetDate, @Priority,
            @LinkedAccountIds, @Icon, @Color, @IsAutoContribute, @ProjectedDate
        );

        SET @NewGoalId = SCOPE_IDENTITY();

        -- If already reached target, mark as completed
        IF @CurrentAmount >= @TargetAmount
        BEGIN
            UPDATE Goals.Goals
            SET Status        = N'Completed',
                CompletedDate = @StartDate,
                ProjectedDate = @StartDate
            WHERE Id = @NewGoalId;
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'Goal', CAST(@NewGoalId AS NVARCHAR(256)),
            N'{"Name":"' + REPLACE(@Name, '"', '"') + N'"' +
            N',"TargetAmount":' + CAST(@TargetAmount AS NVARCHAR(50)) +
            N',"Category":"' + @Category + N'"' +
            N',"TargetDate":"' + CONVERT(NVARCHAR(10), @TargetDate, 23) + N'"}'
        );
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

-- ---------------------------------------------------------------------------
-- SP: Goals.sp_AddGoalContribution
-- Description: Add a contribution to a goal and update current amount
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Goals.sp_AddGoalContribution', N'P') IS NOT NULL
    DROP PROCEDURE Goals.sp_AddGoalContribution;
GO

CREATE PROCEDURE Goals.sp_AddGoalContribution
    @GoalId              BIGINT,
    @Amount              DECIMAL(18,2),
    @ContributionDate    DATE           = NULL,   -- Defaults to today
    @Source              NVARCHAR(30)   = N'Manual',  -- Manual, AutoSave, Windfall
    @SourceAccountId     BIGINT         = NULL,
    @Notes               NVARCHAR(300)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate goal exists and is active
        DECLARE @UserId         BIGINT;
        DECLARE @CurrentAmount  DECIMAL(18,2);
        DECLARE @TargetAmount   DECIMAL(18,2);
        DECLARE @GoalStatus     NVARCHAR(20);
        DECLARE @GoalName       NVARCHAR(100);

        SELECT
            @UserId        = UserId,
            @CurrentAmount = CurrentAmount,
            @TargetAmount  = TargetAmount,
            @GoalStatus    = Status,
            @GoalName      = Name
        FROM Goals.Goals
        WHERE Id = @GoalId AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Goal with Id %d does not exist.', 16, 1, @GoalId);
            RETURN;
        END

        IF @GoalStatus NOT IN (N'InProgress', N'Paused')
        BEGIN
            RAISERROR('Cannot add contribution to a goal with status "%s".', 16, 1, @GoalStatus);
            RETURN;
        END

        IF @Amount <= 0
        BEGIN
            RAISERROR('Contribution amount must be greater than zero.', 16, 1);
            RETURN;
        END

        IF @ContributionDate IS NULL
            SET @ContributionDate = CAST(SYSUTCDATETIME() AS DATE);

        -- Insert the contribution
        INSERT INTO Goals.GoalContributions
            (GoalId, Amount, ContributionDate, Source, SourceAccountId, Notes)
        VALUES
            (@GoalId, @Amount, @ContributionDate, @Source, @SourceAccountId, @Notes);

        -- Update current amount
        DECLARE @NewCurrentAmount DECIMAL(18,2) = @CurrentAmount + @Amount;

        -- Check if goal is now completed
        DECLARE @NewStatus      NVARCHAR(20) = @GoalStatus;
        DECLARE @CompletedDate  DATE         = NULL;

        IF @NewCurrentAmount >= @TargetAmount
        BEGIN
            SET @NewStatus     = N'Completed';
            SET @CompletedDate = @ContributionDate;
        END

        -- Recalculate projected date
        DECLARE @ProjectedDate DATE = NULL;
        DECLARE @MonthlyContrib DECIMAL(18,2);
        SELECT @MonthlyContrib = MonthlyContribution FROM Goals.Goals WHERE Id = @GoalId;

        IF @NewStatus = N'Completed'
        BEGIN
            SET @ProjectedDate = @ContributionDate;
        END
        ELSE IF @MonthlyContrib IS NOT NULL AND @MonthlyContrib > 0
        BEGIN
            DECLARE @Remaining DECIMAL(18,2) = @TargetAmount - @NewCurrentAmount;
            IF @Remaining > 0
            BEGIN
                DECLARE @MonthsNeeded INT = CEILING(@Remaining / @MonthlyContrib);
                DECLARE @StartDate DATE;
                SELECT @StartDate = StartDate FROM Goals.Goals WHERE Id = @GoalId;
                SET @ProjectedDate = DATEADD(MONTH, @MonthsNeeded, @ContributionDate);
            END
            ELSE
                SET @ProjectedDate = @ContributionDate;
        END

        -- Update the goal
        UPDATE Goals.Goals
        SET
            CurrentAmount    = @NewCurrentAmount,
            Status           = @NewStatus,
            CompletedDate    = @CompletedDate,
            ProjectedDate    = @ProjectedDate,
            UpdatedAt        = SYSUTCDATETIME()
        WHERE Id = @GoalId;

        -- Debit source account if specified
        IF @SourceAccountId IS NOT NULL
        BEGIN
            DECLARE @Delta DECIMAL(18,2) = -@Amount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @SourceAccountId, @DeltaAmount = @Delta;
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'GoalContribution', CAST(@GoalId AS NVARCHAR(256)),
            N'{"Amount":' + CAST(@Amount AS NVARCHAR(50)) +
            N',"NewCurrentAmount":' + CAST(@NewCurrentAmount AS NVARCHAR(50)) +
            N',"Source":"' + @Source + N'"}'
        );

        -- Return contribution details
        SELECT
            @GoalId              AS GoalId,
            @Amount              AS ContributionAmount,
            @NewCurrentAmount    AS NewCurrentAmount,
            @TargetAmount        AS TargetAmount,
            CASE
                WHEN @TargetAmount > 0
                THEN ROUND((@NewCurrentAmount * 100.0) / @TargetAmount, 2)
                ELSE 0
            END                  AS CompletionPct,
            @NewStatus           AS GoalStatus,
            @ProjectedDate       AS ProjectedCompletionDate,
            @TargetAmount - @NewCurrentAmount AS RemainingAmount;
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

-- ---------------------------------------------------------------------------
-- SP: Goals.sp_CalculateGoalProjection
-- Description: Project when a goal will be reached based on current savings
--              rate and contribution history
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Goals.sp_CalculateGoalProjection', N'P') IS NOT NULL
    DROP PROCEDURE Goals.sp_CalculateGoalProjection;
GO

CREATE PROCEDURE Goals.sp_CalculateGoalProjection
    @GoalId  BIGINT,
    @AssumedMonthlyContrib DECIMAL(18,2) = NULL   -- Override for projection; uses history if NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get goal details
        DECLARE @UserId              BIGINT;
        DECLARE @TargetAmount        DECIMAL(18,2);
        DECLARE @CurrentAmount       DECIMAL(18,2);
        DECLARE @MonthlyContribution DECIMAL(18,2);
        DECLARE @StartDate           DATE;
        DECLARE @TargetDate          DATE;
        DECLARE @GoalStatus          NVARCHAR(20);
        DECLARE @GoalName            NVARCHAR(100);
        DECLARE @Category            NVARCHAR(50);

        SELECT
            @UserId              = UserId,
            @TargetAmount        = TargetAmount,
            @CurrentAmount       = CurrentAmount,
            @MonthlyContribution = MonthlyContribution,
            @StartDate           = StartDate,
            @TargetDate          = TargetDate,
            @GoalStatus          = Status,
            @GoalName            = Name,
            @Category            = Category
        FROM Goals.Goals
        WHERE Id = @GoalId AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Goal with Id %d does not exist.', 16, 1, @GoalId);
            RETURN;
        END

        -- If already completed, return immediately
        IF @GoalStatus = N'Completed'
        BEGIN
            SELECT
                @GoalId              AS GoalId,
                @GoalName            AS GoalName,
                @CurrentAmount       AS CurrentAmount,
                @TargetAmount        AS TargetAmount,
                100.0                AS CompletionPct,
                0                    AS RemainingAmount,
                0                    AS MonthsToReach,
                @StartDate           AS ProjectedCompletionDate,
                N'Already Completed' AS ProjectionStatus,
                0                    AS Shortfall;
            RETURN;
        END

        -- Determine monthly contribution to use
        DECLARE @EffectiveMonthlyContrib DECIMAL(18,2) = @AssumedMonthlyContrib;

        IF @EffectiveMonthlyContrib IS NULL
        BEGIN
            -- Calculate from contribution history (average over last 3 months)
            DECLARE @AvgContrib DECIMAL(18,2) = 0;

            SELECT @AvgContrib = ISNULL(AVG(MonthlyTotal), 0)
            FROM (
                SELECT
                    YEAR(ContributionDate) * 100 + MONTH(ContributionDate) AS YM,
                    SUM(Amount) AS MonthlyTotal
                FROM Goals.GoalContributions
                WHERE GoalId = @GoalId
                  AND ContributionDate >= DATEADD(MONTH, -3, CAST(SYSUTCDATETIME() AS DATE))
                GROUP BY YEAR(ContributionDate) * 100 + MONTH(ContributionDate)
            ) MonthlyContribs;

            -- Use the higher of: configured monthly contribution or average from history
            IF @AvgContrib > 0
                SET @EffectiveMonthlyContrib = @AvgContrib;
            ELSE IF @MonthlyContribution IS NOT NULL AND @MonthlyContribution > 0
                SET @EffectiveMonthlyContrib = @MonthlyContribution;
            ELSE
                SET @EffectiveMonthlyContrib = 0;
        END

        -- Calculate projection
        DECLARE @RemainingAmount  DECIMAL(18,2) = @TargetAmount - @CurrentAmount;
        DECLARE @MonthsToReach    INT = 0;
        DECLARE @ProjectedDate    DATE = NULL;
        DECLARE @ProjectionStatus NVARCHAR(50);
        DECLARE @Shortfall        DECIMAL(18,2) = 0;
        DECLARE @OnTrack          BIT = 0;

        IF @RemainingAmount <= 0
        BEGIN
            SET @ProjectedDate    = CAST(SYSUTCDATETIME() AS DATE);
            SET @ProjectionStatus = N'Already Reached';
            SET @OnTrack          = 1;
        END
        ELSE IF @EffectiveMonthlyContrib > 0
        BEGIN
            -- Simple linear projection
            SET @MonthsToReach = CEILING(@RemainingAmount / @EffectiveMonthlyContrib);
            SET @ProjectedDate = DATEADD(MONTH, @MonthsToReach, CAST(SYSUTCDATETIME() AS DATE));

            -- Check if on track
            IF @ProjectedDate <= @TargetDate
            BEGIN
                SET @ProjectionStatus = N'On Track';
                SET @OnTrack = 1;
            END
            ELSE
            BEGIN
                SET @ProjectionStatus = N'Behind Schedule';
                SET @Shortfall = @RemainingAmount - (@EffectiveMonthlyContrib *
                    DATEDIFF(MONTH, CAST(SYSUTCDATETIME() AS DATE), @TargetDate));
                IF @Shortfall < 0 SET @Shortfall = 0;
            END
        END
        ELSE
        BEGIN
            -- No contribution rate available
            SET @ProjectedDate    = NULL;
            SET @ProjectionStatus = N'No Savings Rate';
            SET @Shortfall        = @RemainingAmount;
        END

        -- Calculate required monthly contribution to meet target date
        DECLARE @RequiredMonthlyContrib DECIMAL(18,2) = 0;
        DECLARE @MonthsRemaining INT = DATEDIFF(MONTH, CAST(SYSUTCDATETIME() AS DATE), @TargetDate);

        IF @MonthsRemaining > 0 AND @RemainingAmount > 0
            SET @RequiredMonthlyContrib = ROUND(@RemainingAmount / @MonthsRemaining, 2);

        -- Update projected date on the goal
        UPDATE Goals.Goals
        SET ProjectedDate = @ProjectedDate,
            UpdatedAt     = SYSUTCDATETIME()
        WHERE Id = @GoalId AND DeletedAt IS NULL;

        -- Return projection results
        SELECT
            @GoalId                      AS GoalId,
            @GoalName                    AS GoalName,
            @Category                    AS Category,
            @TargetAmount                AS TargetAmount,
            @CurrentAmount               AS CurrentAmount,
            @RemainingAmount             AS RemainingAmount,
            CASE
                WHEN @TargetAmount > 0
                THEN ROUND((@CurrentAmount * 100.0) / @TargetAmount, 2)
                ELSE 0
            END                           AS CompletionPct,
            @EffectiveMonthlyContrib      AS CurrentMonthlyContribution,
            @RequiredMonthlyContrib       AS RequiredMonthlyContribution,
            @MonthsToReach                AS MonthsToReachGoal,
            @ProjectedDate                AS ProjectedCompletionDate,
            @TargetDate                   AS TargetDate,
            @ProjectionStatus             AS ProjectionStatus,
            @OnTrack                      AS IsOnTrack,
            @Shortfall                    AS Shortfall,
            CASE
                WHEN @EffectiveMonthlyContrib > 0 AND @RequiredMonthlyContrib > 0
                THEN ROUND((@EffectiveMonthlyContrib / @RequiredMonthlyContrib) * 100, 2)
                ELSE 0
            END                           AS SavingsRateVsRequiredPct;

        -- Contribution history trend (last 6 months)
        SELECT TOP 6
            YEAR(ContributionDate) * 100 + MONTH(ContributionDate) AS YearMonth,
            SUM(Amount)    AS MonthlyContribution,
            COUNT(*)       AS ContributionCount
        FROM Goals.GoalContributions
        WHERE GoalId = @GoalId
        GROUP BY YEAR(ContributionDate) * 100 + MONTH(ContributionDate)
        ORDER BY YearMonth DESC;
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

-- ---------------------------------------------------------------------------
-- SP: Goals.sp_GetGoalProgress
-- Description: Progress percentage, projected date, shortfall for a goal
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Goals.sp_GetGoalProgress', N'P') IS NOT NULL
    DROP PROCEDURE Goals.sp_GetGoalProgress;
GO

CREATE PROCEDURE Goals.sp_GetGoalProgress
    @GoalId  BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get goal details
        DECLARE @UserId              BIGINT;
        DECLARE @GoalName            NVARCHAR(100);
        DECLARE @Category            NVARCHAR(50);
        DECLARE @TargetAmount        DECIMAL(18,2);
        DECLARE @CurrentAmount       DECIMAL(18,2);
        DECLARE @MonthlyContribution DECIMAL(18,2);
        DECLARE @StartDate           DATE;
        DECLARE @TargetDate          DATE;
        DECLARE @CompletedDate       DATE;
        DECLARE @ProjectedDate       DATE;
        DECLARE @GoalStatus          NVARCHAR(20);
        DECLARE @Priority            NVARCHAR(10);
        DECLARE @IsAutoContribute    BIT;

        SELECT
            @UserId              = UserId,
            @GoalName            = Name,
            @Category            = Category,
            @TargetAmount        = TargetAmount,
            @CurrentAmount       = CurrentAmount,
            @MonthlyContribution = MonthlyContribution,
            @StartDate           = StartDate,
            @TargetDate          = TargetDate,
            @CompletedDate       = CompletedDate,
            @ProjectedDate       = ProjectedDate,
            @GoalStatus          = Status,
            @Priority            = Priority,
            @IsAutoContribute    = IsAutoContribute
        FROM Goals.Goals
        WHERE Id = @GoalId AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Goal with Id %d does not exist.', 16, 1, @GoalId);
            RETURN;
        END

        -- Calculate progress metrics
        DECLARE @RemainingAmount DECIMAL(18,2) = @TargetAmount - @CurrentAmount;
        IF @RemainingAmount < 0 SET @RemainingAmount = 0;

        DECLARE @CompletionPct DECIMAL(5,2) = 0;
        IF @TargetAmount > 0
            SET @CompletionPct = CASE
                WHEN @CurrentAmount >= @TargetAmount THEN 100.00
                ELSE ROUND((@CurrentAmount * 100.0) / @TargetAmount, 2)
            END;

        -- Time progress
        DECLARE @TotalDays     INT = DATEDIFF(DAY, @StartDate, @TargetDate);
        DECLARE @DaysElapsed   INT = DATEDIFF(DAY, @StartDate, CAST(SYSUTCDATETIME() AS DATE));
        DECLARE @DaysRemaining INT = DATEDIFF(DAY, CAST(SYSUTCDATETIME() AS DATE), @TargetDate);

        IF @DaysElapsed < 0 SET @DaysElapsed = 0;
        IF @DaysRemaining < 0 SET @DaysRemaining = 0;

        DECLARE @TimeProgressPct DECIMAL(5,2) = 0;
        IF @TotalDays > 0
            SET @TimeProgressPct = CASE
                WHEN @DaysElapsed >= @TotalDays THEN 100.00
                ELSE ROUND((@DaysElapsed * 100.0) / @TotalDays, 2)
            END;

        -- Pace: amount progress vs time progress
        DECLARE @Pace NVARCHAR(20) = N'On Track';
        IF @CompletionPct >= 100
            SET @Pace = N'Completed';
        ELSE IF @CompletionPct >= @TimeProgressPct + 5
            SET @Pace = N'Ahead';
        ELSE IF @CompletionPct < @TimeProgressPct - 10
            SET @Pace = N'Behind';
        ELSE
            SET @Pace = N'On Track';

        -- Required monthly contribution to meet target
        DECLARE @RequiredMonthly DECIMAL(18,2) = 0;
        IF @DaysRemaining > 0 AND @RemainingAmount > 0
        BEGIN
            DECLARE @MonthsLeft DECIMAL(10,2) = @DaysRemaining / 30.0;
            IF @MonthsLeft > 0
                SET @RequiredMonthly = ROUND(@RemainingAmount / @MonthsLeft, 2);
        END

        -- Shortfall at current rate
        DECLARE @Shortfall DECIMAL(18,2) = 0;
        IF @MonthlyContribution IS NOT NULL AND @MonthlyContribution > 0 AND @DaysRemaining > 0
        BEGIN
            DECLARE @ExpectedTotal DECIMAL(18,2) = @CurrentAmount +
                (@MonthlyContribution * (@DaysRemaining / 30.0));
            SET @Shortfall = @TargetAmount - @ExpectedTotal;
            IF @Shortfall < 0 SET @Shortfall = 0;
        END
        ELSE IF @RemainingAmount > 0 AND (@MonthlyContribution IS NULL OR @MonthlyContribution = 0)
        BEGIN
            SET @Shortfall = @RemainingAmount;
        END

        -- Contribution stats
        DECLARE @TotalContributions DECIMAL(18,2) = 0;
        DECLARE @ContributionCount  INT = 0;
        DECLARE @AvgContribution    DECIMAL(18,2) = 0;

        SELECT
            @TotalContributions = ISNULL(SUM(Amount), 0),
            @ContributionCount  = COUNT(*)
        FROM Goals.GoalContributions
        WHERE GoalId = @GoalId;

        IF @ContributionCount > 0
            SET @AvgContribution = ROUND(@TotalContributions / @ContributionCount, 2);

        -- Return progress report
        SELECT
            @GoalId               AS GoalId,
            @GoalName             AS GoalName,
            @Category             AS Category,
            @GoalStatus           AS Status,
            @Priority             AS Priority,
            @TargetAmount         AS TargetAmount,
            @CurrentAmount        AS CurrentAmount,
            @RemainingAmount      AS RemainingAmount,
            @CompletionPct        AS CompletionPct,
            @TotalDays            AS TotalDays,
            @DaysElapsed          AS DaysElapsed,
            @DaysRemaining        AS DaysRemaining,
            @TimeProgressPct      AS TimeProgressPct,
            @Pace                 AS Pace,
            @StartDate            AS StartDate,
            @TargetDate           AS TargetDate,
            @ProjectedDate        AS ProjectedDate,
            @CompletedDate        AS CompletedDate,
            @MonthlyContribution  AS CurrentMonthlyContribution,
            @RequiredMonthly      AS RequiredMonthlyContribution,
            @Shortfall            AS Shortfall,
            @TotalContributions   AS TotalContributed,
            @ContributionCount    AS ContributionCount,
            @AvgContribution      AS AvgContributionAmount,
            @IsAutoContribute     AS IsAutoContribute;

        -- Recent contributions (last 10)
        SELECT TOP 10
            gc.Id,
            gc.Amount,
            gc.ContributionDate,
            gc.Source,
            gc.Notes
        FROM Goals.GoalContributions gc
        WHERE gc.GoalId = @GoalId
        ORDER BY gc.ContributionDate DESC, gc.Id DESC;
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

PRINT 'Goals stored procedures created successfully.';
GO


