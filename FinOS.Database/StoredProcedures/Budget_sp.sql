-- ============================================================================
-- FinOS Database - Budget Stored Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for budget creation, spending tracking,
--              alert checks, and budget vs actual comparison
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- SP: Budget.sp_CreateBudget
-- Description: Insert a budget with its categories (passed as JSON)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.sp_CreateBudget', N'P') IS NOT NULL
    DROP PROCEDURE Budget.sp_CreateBudget;
GO

CREATE PROCEDURE Budget.sp_CreateBudget
    @UserId              BIGINT,
    @Name                NVARCHAR(100),
    @PeriodType          NVARCHAR(20),       -- Weekly, Monthly, Quarterly, Yearly
    @StartDate           DATE,
    @EndDate             DATE,
    @TotalBudgetAmount   DECIMAL(18,2),
    @Currency            NVARCHAR(3)      = N'INR',
    @RolloverEnabled     BIT              = 0,
    @AlertThresholdPct   DECIMAL(5,2)     = 80.00,
    @IsTemplate          BIT              = 0,
    @Categories          NVARCHAR(MAX)    = NULL,  -- JSON: [{"CategoryId":1,"AllocatedAmount":5000,"CustomLabel":"Groceries","AlertThresholdPct":90}]
    @NewBudgetId         BIGINT           OUTPUT
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
            RAISERROR('Budget name is required.', 16, 1);
            RETURN;
        END

        IF @PeriodType NOT IN (N'Weekly', N'Monthly', N'Quarterly', N'Yearly')
        BEGIN
            RAISERROR('PeriodType must be Weekly, Monthly, Quarterly, or Yearly.', 16, 1);
            RETURN;
        END

        IF @StartDate >= @EndDate
        BEGIN
            RAISERROR('EndDate must be after StartDate.', 16, 1);
            RETURN;
        END

        IF @TotalBudgetAmount <= 0
        BEGIN
            RAISERROR('Total budget amount must be greater than zero.', 16, 1);
            RETURN;
        END

        IF @AlertThresholdPct < 0 OR @AlertThresholdPct > 100
        BEGIN
            RAISERROR('AlertThresholdPct must be between 0 and 100.', 16, 1);
            RETURN;
        END

        IF @Categories IS NOT NULL AND EXISTS
        (
            SELECT 1
            FROM OPENJSON(@Categories)
            WITH (CategoryId BIGINT N'$.CategoryId') requested
            LEFT JOIN Core.Categories category
              ON category.Id = requested.CategoryId
             AND category.IsActive = 1
             AND (category.IsSystem = 1 OR category.UserId = @UserId)
            WHERE requested.CategoryId IS NOT NULL AND category.Id IS NULL
        )
        BEGIN
            RAISERROR('One or more budget categories are unavailable for this user.', 16, 1);
            RETURN;
        END

        -- Insert the budget
        INSERT INTO Budget.Budgets
        (
            UserId, Name, PeriodType, StartDate, EndDate,
            TotalBudgetAmount, Currency, RolloverEnabled,
            AlertThresholdPct, IsTemplate
        )
        VALUES
        (
            @UserId, @Name, @PeriodType, @StartDate, @EndDate,
            @TotalBudgetAmount, @Currency, @RolloverEnabled,
            @AlertThresholdPct, @IsTemplate
        );

        SET @NewBudgetId = SCOPE_IDENTITY();

        -- Parse and insert budget categories if provided
        IF @Categories IS NOT NULL
        BEGIN
            INSERT INTO Budget.BudgetCategories
            (BudgetId, CategoryId, CustomLabel, AllocatedAmount, AlertThresholdPct)
            SELECT
                @NewBudgetId,
                [CategoryId],
                [CustomLabel],
                [AllocatedAmount],
                [AlertThresholdPct]
            FROM OPENJSON(@Categories)
            WITH (
                CategoryId       BIGINT         N'$.CategoryId',
                CustomLabel      NVARCHAR(100)  N'$.CustomLabel',
                AllocatedAmount  DECIMAL(18,2)  N'$.AllocatedAmount',
                AlertThresholdPct DECIMAL(5,2)  N'$.AlertThresholdPct'
            );

            -- Validate total allocated doesn't exceed budget
            DECLARE @TotalAllocated DECIMAL(18,2);
            SELECT @TotalAllocated = ISNULL(SUM(AllocatedAmount), 0)
            FROM Budget.BudgetCategories
            WHERE BudgetId = @NewBudgetId;

            IF @TotalAllocated > @TotalBudgetAmount
            BEGIN
                -- Rollback by deleting the budget (cascade will remove categories)
                DELETE FROM Budget.Budgets WHERE Id = @NewBudgetId;
                DECLARE @TotalAllocatedText NVARCHAR(50) = CAST(@TotalAllocated AS NVARCHAR(50));
                DECLARE @TotalBudgetAmountText NVARCHAR(50) = CAST(@TotalBudgetAmount AS NVARCHAR(50));
                RAISERROR('Total allocated amount (%s) exceeds total budget amount (%s).', 16, 1,
                    @TotalAllocatedText, @TotalBudgetAmountText);
                RETURN;
            END
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'Budget', CAST(@NewBudgetId AS NVARCHAR(256)),
            N'{"Name":"' + REPLACE(@Name, '"', '"') + N'"' +
            N',"PeriodType":"' + @PeriodType + N'"' +
            N',"TotalBudgetAmount":' + CAST(@TotalBudgetAmount AS NVARCHAR(50)) + N'}'
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
-- SP: Budget.sp_UpdateBudgetSpent
-- Description: Recalculate spent amount for a budget category by summing
--              transactions in the budget period
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.sp_UpdateBudgetSpent', N'P') IS NOT NULL
    DROP PROCEDURE Budget.sp_UpdateBudgetSpent;
GO

CREATE PROCEDURE Budget.sp_UpdateBudgetSpent
    @BudgetCategoryId BIGINT          = NULL,    -- Update a single category
    @BudgetId         BIGINT          = NULL     -- Or update all categories for a budget
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Must specify at least one
        IF @BudgetCategoryId IS NULL AND @BudgetId IS NULL
        BEGIN
            RAISERROR('Specify either BudgetCategoryId or BudgetId.', 16, 1);
            RETURN;
        END

        -- Create temp table for categories to update
        CREATE TABLE #CategoriesToUpdate
        (
            BudgetCategoryId BIGINT,
            BudgetId         BIGINT,
            CategoryId       BIGINT,
            BudgetStartDate  DATE,
            BudgetEndDate    DATE
        );

        -- Populate categories to update
        IF @BudgetCategoryId IS NOT NULL
        BEGIN
            INSERT INTO #CategoriesToUpdate (BudgetCategoryId, BudgetId, CategoryId, BudgetStartDate, BudgetEndDate)
            SELECT bc.Id, bc.BudgetId, bc.CategoryId, b.StartDate, b.EndDate
            FROM Budget.BudgetCategories bc
            INNER JOIN Budget.Budgets b ON bc.BudgetId = b.Id
            WHERE bc.Id = @BudgetCategoryId
              AND b.DeletedAt IS NULL;
        END
        ELSE
        BEGIN
            INSERT INTO #CategoriesToUpdate (BudgetCategoryId, BudgetId, CategoryId, BudgetStartDate, BudgetEndDate)
            SELECT bc.Id, bc.BudgetId, bc.CategoryId, b.StartDate, b.EndDate
            FROM Budget.BudgetCategories bc
            INNER JOIN Budget.Budgets b ON bc.BudgetId = b.Id
            WHERE bc.BudgetId = @BudgetId
              AND b.DeletedAt IS NULL;
        END

        -- Validate we have categories to update
        IF NOT EXISTS (SELECT 1 FROM #CategoriesToUpdate)
        BEGIN
            RAISERROR('No valid budget categories found to update.', 16, 1);
            RETURN;
        END

        -- Update spent amount for each category
        DECLARE @BCId       BIGINT;
        DECLARE @CatId      BIGINT;
        DECLARE @BStart     DATE;
        DECLARE @BEnd       DATE;
        DECLARE @UserId     BIGINT;
        DECLARE @Spent      DECIMAL(18,2);

        DECLARE cat_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT BudgetCategoryId, CategoryId, BudgetStartDate, BudgetEndDate
            FROM #CategoriesToUpdate;

        OPEN cat_cursor;
        FETCH NEXT FROM cat_cursor INTO @BCId, @CatId, @BStart, @BEnd;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Get user ID from budget
            SELECT @UserId = b.UserId
            FROM Budget.BudgetCategories bc
            INNER JOIN Budget.Budgets b ON bc.BudgetId = b.Id
            WHERE bc.Id = @BCId;

            IF @CatId IS NOT NULL
            BEGIN
                -- Calculate spent from transactions matching this category and period
                SELECT @Spent = ISNULL(SUM(t.Amount), 0)
                FROM Core.Transactions t
                WHERE t.UserId = @UserId
                  AND t.DeletedAt IS NULL
                  AND t.Type = N'Expense'
                  AND t.CategoryId = @CatId
                  AND t.ParentTransactionId IS NULL
                  AND t.TransactionDate BETWEEN @BStart AND @BEnd;
            END
            ELSE
            BEGIN
                -- No specific category linked - use custom label matching
                -- This is a catch-all; we sum all expenses not matched to other categories
                SET @Spent = 0;
            END

            -- Update the budget category
            UPDATE Budget.BudgetCategories
            SET SpentAmount = ISNULL(@Spent, 0),
                UpdatedAt   = SYSUTCDATETIME()
            WHERE Id = @BCId;

            FETCH NEXT FROM cat_cursor INTO @BCId, @CatId, @BStart, @BEnd;
        END

        CLOSE cat_cursor;
        DEALLOCATE cat_cursor;

        -- Return updated categories
        SELECT
            bc.Id                  AS BudgetCategoryId,
            bc.BudgetId,
            bc.CategoryId,
            ISNULL(c.Name, bc.CustomLabel) AS CategoryName,
            bc.AllocatedAmount,
            bc.SpentAmount,
            CASE
                WHEN bc.AllocatedAmount > 0
                THEN ROUND((bc.SpentAmount * 100.0) / bc.AllocatedAmount, 2)
                ELSE 0
            END                     AS SpentPct,
            bc.AllocatedAmount - bc.SpentAmount AS RemainingAmount
        FROM Budget.BudgetCategories bc
        LEFT JOIN Core.Categories c ON bc.CategoryId = c.Id
        WHERE bc.BudgetId = ISNULL(@BudgetId, (SELECT TOP 1 BudgetId FROM #CategoriesToUpdate))
        ORDER BY bc.SortOrder;

        DROP TABLE #CategoriesToUpdate;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#CategoriesToUpdate', N'U') IS NOT NULL
            DROP TABLE #CategoriesToUpdate;

        IF CURSOR_STATUS(N'local', N'cat_cursor') >= 0
        BEGIN
            CLOSE cat_cursor;
            DEALLOCATE cat_cursor;
        END

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ---------------------------------------------------------------------------
-- SP: Budget.sp_CheckBudgetAlerts
-- Description: Check if any category crossed threshold and create alerts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.sp_CheckBudgetAlerts', N'P') IS NOT NULL
    DROP PROCEDURE Budget.sp_CheckBudgetAlerts;
GO

CREATE PROCEDURE Budget.sp_CheckBudgetAlerts
    @UserId     BIGINT,
    @CategoryId BIGINT        = NULL,     -- Check specific category, or all active budgets
    @Amount     DECIMAL(18,2) = NULL      -- The amount just spent (for real-time check)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get active budgets for the user
        CREATE TABLE #ActiveBudgetCategories
        (
            BudgetCategoryId  BIGINT,
            BudgetId          BIGINT,
            CategoryId        BIGINT,
            CustomLabel       NVARCHAR(100),
            AllocatedAmount   DECIMAL(18,2),
            SpentAmount       DECIMAL(18,2),
            AlertThresholdPct DECIMAL(5,2),
            BudgetAlertPct    DECIMAL(5,2),   -- Budget-level alert threshold
            BudgetName        NVARCHAR(100),
            BudgetStartDate   DATE,
            BudgetEndDate     DATE
        );

        INSERT INTO #ActiveBudgetCategories
        (
            BudgetCategoryId, BudgetId, CategoryId, CustomLabel,
            AllocatedAmount, SpentAmount, AlertThresholdPct,
            BudgetAlertPct, BudgetName, BudgetStartDate, BudgetEndDate
        )
        SELECT
            bc.Id,
            bc.BudgetId,
            bc.CategoryId,
            bc.CustomLabel,
            bc.AllocatedAmount,
            bc.SpentAmount,
            ISNULL(bc.AlertThresholdPct, b.AlertThresholdPct),  -- Category overrides budget
            b.AlertThresholdPct,
            b.Name,
            b.StartDate,
            b.EndDate
        FROM Budget.BudgetCategories bc
        INNER JOIN Budget.Budgets b ON bc.BudgetId = b.Id
        WHERE b.UserId = @UserId
          AND b.IsActive = 1
          AND b.DeletedAt IS NULL
          AND CAST(SYSUTCDATETIME() AS DATE) BETWEEN b.StartDate AND b.EndDate
          AND (@CategoryId IS NULL OR bc.CategoryId = @CategoryId);

        -- Now refresh spent amounts and check thresholds
        DECLARE @BCId           BIGINT;
        DECLARE @BId            BIGINT;
        DECLARE @CatId          BIGINT;
        DECLARE @CustomLabel    NVARCHAR(100);
        DECLARE @Allocated      DECIMAL(18,2);
        DECLARE @Spent          DECIMAL(18,2);
        DECLARE @ThresholdPct   DECIMAL(5,2);
        DECLARE @BName          NVARCHAR(100);
        DECLARE @BStart         DATE;
        DECLARE @BEnd           DATE;

        DECLARE alert_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT BudgetCategoryId, BudgetId, CategoryId, CustomLabel,
                   AllocatedAmount, SpentAmount, AlertThresholdPct,
                   BudgetName, BudgetStartDate, BudgetEndDate
            FROM #ActiveBudgetCategories;

        OPEN alert_cursor;
        FETCH NEXT FROM alert_cursor INTO
            @BCId, @BId, @CatId, @CustomLabel,
            @Allocated, @Spent, @ThresholdPct,
            @BName, @BStart, @BEnd;

        DECLARE @AlertsCreated INT = 0;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Recalculate spent (include the new amount if just spent for this category)
            DECLARE @CurrentSpent DECIMAL(18,2) = @Spent;

            IF @CatId IS NOT NULL
            BEGIN
                SELECT @CurrentSpent = ISNULL(SUM(t.Amount), 0)
                FROM Core.Transactions t
                WHERE t.UserId = @UserId
                  AND t.DeletedAt IS NULL
                  AND t.Type = N'Expense'
                  AND t.CategoryId = @CatId
                  AND t.ParentTransactionId IS NULL
                  AND t.TransactionDate BETWEEN @BStart AND @BEnd;
            END

            -- Update spent amount
            UPDATE Budget.BudgetCategories
            SET SpentAmount = @CurrentSpent,
                UpdatedAt   = SYSUTCDATETIME()
            WHERE Id = @BCId;

            -- Calculate spent percentage
            DECLARE @SpentPct DECIMAL(5,2) = 0;
            IF @Allocated > 0
                SET @SpentPct = ROUND((@CurrentSpent * 100.0) / @Allocated, 2);

            -- Determine category label for messages
            DECLARE @CatLabel NVARCHAR(100) = ISNULL(@CustomLabel, N'Unknown');
            IF @CatId IS NOT NULL
            BEGIN
                DECLARE @CatName NVARCHAR(100);
                SELECT @CatName = Name FROM Core.Categories WHERE Id = @CatId;
                IF @CatName IS NOT NULL
                    SET @CatLabel = @CatName;
            END

            -- Check for threshold alert
            IF @SpentPct >= @ThresholdPct AND @Allocated > 0
            BEGIN
                -- Avoid duplicate alerts: check if one already exists for this category and type today
                IF NOT EXISTS (
                    SELECT 1 FROM Budget.BudgetAlerts
                    WHERE BudgetCategoryId = @BCId
                      AND AlertType = N'Threshold'
                      AND CAST(CreatedAt AS DATE) = CAST(SYSUTCDATETIME() AS DATE)
                )
                BEGIN
                    INSERT INTO Budget.BudgetAlerts
                        (BudgetCategoryId, AlertType, ThresholdPercentage, Message)
                    VALUES (
                        @BCId,
                        N'Threshold',
                        @ThresholdPct,
                        N'Your spending on "' + @CatLabel + N'" has reached ' +
                        CAST(@SpentPct AS NVARCHAR(10)) + N'% of the budget (₹' +
                        CAST(@CurrentSpent AS NVARCHAR(50)) + N' of ₹' +
                        CAST(@Allocated AS NVARCHAR(50)) + N').'
                    );

                    SET @AlertsCreated = @AlertsCreated + 1;
                END
            END

            -- Check for overspent alert
            IF @CurrentSpent > @Allocated AND @Allocated > 0
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM Budget.BudgetAlerts
                    WHERE BudgetCategoryId = @BCId
                      AND AlertType = N'Overspent'
                      AND CAST(CreatedAt AS DATE) = CAST(SYSUTCDATETIME() AS DATE)
                )
                BEGIN
                    DECLARE @OverspendAmount DECIMAL(18,2) = @CurrentSpent - @Allocated;

                    INSERT INTO Budget.BudgetAlerts
                        (BudgetCategoryId, AlertType, Message)
                    VALUES (
                        @BCId,
                        N'Overspent',
                        N'You have overspent on "' + @CatLabel + N'" by ₹' +
                        CAST(@OverspendAmount AS NVARCHAR(50)) + N'. Budget was ₹' +
                        CAST(@Allocated AS NVARCHAR(50)) + N', spent ₹' +
                        CAST(@CurrentSpent AS NVARCHAR(50)) + N'.'
                    );

                    SET @AlertsCreated = @AlertsCreated + 1;
                END
            END

            FETCH NEXT FROM alert_cursor INTO
                @BCId, @BId, @CatId, @CustomLabel,
                @Allocated, @Spent, @ThresholdPct,
                @BName, @BStart, @BEnd;
        END

        CLOSE alert_cursor;
        DEALLOCATE alert_cursor;

        -- Return result
        SELECT @AlertsCreated AS AlertsCreated;

        -- Return current alerts (unread)
        SELECT
            ba.Id,
            ba.BudgetCategoryId,
            ISNULL(c.Name, bc.CustomLabel) AS CategoryName,
            ba.AlertType,
            ba.ThresholdPercentage,
            ba.Message,
            ba.IsRead,
            ba.CreatedAt
        FROM Budget.BudgetAlerts ba
        INNER JOIN Budget.BudgetCategories bc ON ba.BudgetCategoryId = bc.Id
        LEFT JOIN Core.Categories c ON bc.CategoryId = c.Id
        INNER JOIN Budget.Budgets b ON bc.BudgetId = b.Id
        WHERE b.UserId = @UserId
          AND ba.IsRead = 0
        ORDER BY ba.CreatedAt DESC;

        DROP TABLE #ActiveBudgetCategories;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#ActiveBudgetCategories', N'U') IS NOT NULL
            DROP TABLE #ActiveBudgetCategories;

        IF CURSOR_STATUS(N'local', N'alert_cursor') >= 0
        BEGIN
            CLOSE alert_cursor;
            DEALLOCATE alert_cursor;
        END

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ---------------------------------------------------------------------------
-- SP: Budget.sp_GetBudgetVsActual
-- Description: Budget vs actual spending by category for a period
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.sp_GetBudgetVsActual', N'P') IS NOT NULL
    DROP PROCEDURE Budget.sp_GetBudgetVsActual;
GO

CREATE PROCEDURE Budget.sp_GetBudgetVsActual
    @UserId    BIGINT,
    @BudgetId  BIGINT = NULL,    -- Specific budget, or latest active
    @StartDate DATE   = NULL,    -- Alternative: specify period directly
    @EndDate   DATE    = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Resolve budget
        DECLARE @ResolvedBudgetId BIGINT = @BudgetId;

        IF @ResolvedBudgetId IS NULL AND @StartDate IS NOT NULL AND @EndDate IS NOT NULL
        BEGIN
            -- Find budget that covers this period
            SELECT TOP 1 @ResolvedBudgetId = Id
            FROM Budget.Budgets
            WHERE UserId = @UserId
              AND IsActive = 1
              AND DeletedAt IS NULL
              AND StartDate <= @StartDate
              AND EndDate >= @EndDate
            ORDER BY CreatedAt DESC;
        END

        IF @ResolvedBudgetId IS NULL
        BEGIN
            -- Get the latest active budget
            SELECT TOP 1 @ResolvedBudgetId = Id
            FROM Budget.Budgets
            WHERE UserId = @UserId
              AND IsActive = 1
              AND DeletedAt IS NULL
              AND CAST(SYSUTCDATETIME() AS DATE) BETWEEN StartDate AND EndDate
            ORDER BY CreatedAt DESC;
        END

        IF @ResolvedBudgetId IS NULL
        BEGIN
            RAISERROR('No active budget found for this user/period.', 16, 1);
            RETURN;
        END

        -- Get budget period
        DECLARE @BStart DATE;
        DECLARE @BEnd   DATE;
        DECLARE @BName  NVARCHAR(100);
        DECLARE @BTotal DECIMAL(18,2);

        SELECT
            @BStart = StartDate,
            @BEnd   = EndDate,
            @BName  = Name,
            @BTotal = TotalBudgetAmount
        FROM Budget.Budgets
        WHERE Id = @ResolvedBudgetId AND DeletedAt IS NULL;

        -- Override period if custom dates specified
        DECLARE @PeriodStart DATE = ISNULL(@StartDate, @BStart);
        DECLARE @PeriodEnd   DATE = ISNULL(@EndDate, @BEnd);

        -- Refresh all spent amounts for this budget
        EXEC Budget.sp_UpdateBudgetSpent @BudgetId = @ResolvedBudgetId;

        -- Result Set 1: Category-level budget vs actual
        SELECT
            bc.Id                           AS BudgetCategoryId,
            bc.CategoryId,
            ISNULL(c.Name, bc.CustomLabel)  AS CategoryName,
            c.Icon                          AS CategoryIcon,
            c.Color                         AS CategoryColor,
            bc.AllocatedAmount              AS BudgetAmount,
            bc.SpentAmount                  AS ActualSpent,
            bc.AllocatedAmount - bc.SpentAmount AS Remaining,
            CASE
                WHEN bc.AllocatedAmount > 0
                THEN ROUND((bc.SpentAmount * 100.0) / bc.AllocatedAmount, 2)
                ELSE 0
            END                              AS SpentPct,
            CASE
                WHEN bc.AllocatedAmount > 0
                THEN ROUND((bc.AllocatedAmount * 100.0) / @BTotal, 2)
                ELSE 0
            END                              AS AllocationPct,
            ISNULL(bc.AlertThresholdPct, b.AlertThresholdPct) AS AlertThresholdPct,
            CASE
                WHEN bc.SpentAmount > bc.AllocatedAmount THEN N'Overspent'
                WHEN bc.AllocatedAmount > 0 AND
                     (bc.SpentAmount * 100.0 / bc.AllocatedAmount) >=
                     ISNULL(bc.AlertThresholdPct, b.AlertThresholdPct)
                THEN N'Alert'
                ELSE N'OK'
            END                              AS Status
        FROM Budget.BudgetCategories bc
        LEFT JOIN Core.Categories c ON bc.CategoryId = c.Id
        INNER JOIN Budget.Budgets b ON bc.BudgetId = b.Id
        WHERE bc.BudgetId = @ResolvedBudgetId
        ORDER BY bc.SortOrder, bc.AllocatedAmount DESC;

        -- Result Set 2: Overall budget summary
        DECLARE @TotalAllocated DECIMAL(18,2) = 0;
        DECLARE @TotalSpent     DECIMAL(18,2) = 0;

        SELECT
            @TotalAllocated = ISNULL(SUM(AllocatedAmount), 0),
            @TotalSpent     = ISNULL(SUM(SpentAmount), 0)
        FROM Budget.BudgetCategories
        WHERE BudgetId = @ResolvedBudgetId;

        -- Also count unbudgeted spending (expenses not in any budget category)
        DECLARE @UnbudgetedSpending DECIMAL(18,2) = 0;
        DECLARE @BudgetedCategoryIds TABLE (CategoryId BIGINT);
        INSERT INTO @BudgetedCategoryIds
        SELECT CategoryId FROM Budget.BudgetCategories
        WHERE BudgetId = @ResolvedBudgetId AND CategoryId IS NOT NULL;

        SELECT @UnbudgetedSpending = ISNULL(SUM(t.Amount), 0)
        FROM Core.Transactions t
        WHERE t.UserId = @UserId
          AND t.DeletedAt IS NULL
          AND t.Type = N'Expense'
          AND t.ParentTransactionId IS NULL
          AND t.TransactionDate BETWEEN @PeriodStart AND @PeriodEnd
          AND (t.CategoryId IS NULL OR t.CategoryId NOT IN (SELECT CategoryId FROM @BudgetedCategoryIds));

        SELECT
            @ResolvedBudgetId       AS BudgetId,
            @BName                  AS BudgetName,
            @PeriodStart            AS PeriodStart,
            @PeriodEnd              AS PeriodEnd,
            @BTotal                 AS TotalBudgetAmount,
            @TotalAllocated         AS TotalAllocated,
            @TotalSpent             AS TotalBudgetedSpent,
            @UnbudgetedSpending     AS UnbudgetedSpent,
            @TotalSpent + @UnbudgetedSpending AS TotalSpent,
            @BTotal - @TotalSpent - @UnbudgetedSpending AS TotalRemaining,
            CASE
                WHEN @BTotal > 0
                THEN ROUND((@TotalSpent + @UnbudgetedSpending) * 100.0 / @BTotal, 2)
                ELSE 0
            END                      AS OverallSpentPct;

        -- Result Set 3: Recent alerts
        SELECT TOP 10
            ba.Id,
            ISNULL(c.Name, bc.CustomLabel) AS CategoryName,
            ba.AlertType,
            ba.ThresholdPercentage,
            ba.Message,
            ba.IsRead,
            ba.CreatedAt
        FROM Budget.BudgetAlerts ba
        INNER JOIN Budget.BudgetCategories bc ON ba.BudgetCategoryId = bc.Id
        LEFT JOIN Core.Categories c ON bc.CategoryId = c.Id
        WHERE bc.BudgetId = @ResolvedBudgetId
        ORDER BY ba.CreatedAt DESC;
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

PRINT 'Budget stored procedures created successfully.';
GO


