-- ============================================================================
-- FinOS Database - Analytics Stored Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for net worth, financial score, aggregates,
--              and spending trend analysis
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- SP: Analytics.sp_CalculateNetWorth
-- Description: Calculate and snapshot net worth for a user
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.sp_CalculateNetWorth', N'P') IS NOT NULL
    DROP PROCEDURE Analytics.sp_CalculateNetWorth;
GO

CREATE PROCEDURE Analytics.sp_CalculateNetWorth
    @UserId        BIGINT,
    @SnapshotDate  DATE = NULL    -- Defaults to today
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Id = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('User with Id %d does not exist.', 16, 1, @UserId);
            RETURN;
        END

        IF @SnapshotDate IS NULL
            SET @SnapshotDate = CAST(SYSUTCDATETIME() AS DATE);

        -- ---------------------------------------------------------------
        -- Calculate Assets
        -- ---------------------------------------------------------------
        -- Cash and bank balances
        DECLARE @CashAndBank DECIMAL(18,2) = 0;
        SELECT @CashAndBank = ISNULL(SUM(Balance), 0)
        FROM Core.Accounts
        WHERE UserId = @UserId
          AND IsActive = 1
          AND DeletedAt IS NULL
          AND IsIncludedInNetWorth = 1
          AND AccountTypeId IN (
              SELECT Id FROM Core.AccountTypes
              WHERE Name IN (N'Savings', N'Current', N'Cash', N'Wallet')
          );

        -- Investment values
        DECLARE @InvestmentValue DECIMAL(18,2) = 0;
        SELECT @InvestmentValue = ISNULL(SUM(ISNULL(h.CurrentValue, h.InvestedAmount)), 0)
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        WHERE p.UserId = @UserId
          AND p.DeletedAt IS NULL
          AND h.DeletedAt IS NULL
          AND h.IsActive = 1;

        -- Gold value (from gold holdings - already in investment value above)
        DECLARE @GoldValue DECIMAL(18,2) = 0;
        SELECT @GoldValue = ISNULL(SUM(ISNULL(h.CurrentValue, h.InvestedAmount)), 0)
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        INNER JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
        WHERE p.UserId = @UserId
          AND p.DeletedAt IS NULL
          AND h.DeletedAt IS NULL
          AND h.IsActive = 1
          AND it.Name = N'Gold';

        -- EPF balance
        DECLARE @EPFValue DECIMAL(18,2) = 0;
        SELECT @EPFValue = ISNULL(SUM(CurrentBalance), 0)
        FROM Investment.EPFAccounts
        WHERE UserId = @UserId AND IsActive = 1;

        -- Real Estate value (from investment holdings)
        DECLARE @RealEstateValue DECIMAL(18,2) = 0;
        SELECT @RealEstateValue = ISNULL(SUM(ISNULL(h.CurrentValue, h.InvestedAmount)), 0)
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        INNER JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
        WHERE p.UserId = @UserId
          AND p.DeletedAt IS NULL
          AND h.DeletedAt IS NULL
          AND h.IsActive = 1
          AND it.Name = N'RealEstate';
        SELECT @RealEstateValue = @RealEstateValue + ISNULL(SUM(CurrentEstimatedValue), 0)
        FROM Core.Assets WHERE UserId=@UserId AND AssetType=N'Property' AND DeletedAt IS NULL;

        -- Other assets (FD, PPF, NPS, Bonds, Crypto)
        DECLARE @OtherAssets DECIMAL(18,2) = 0;
        SELECT @OtherAssets = ISNULL(SUM(ISNULL(h.CurrentValue, h.InvestedAmount)), 0)
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        INNER JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
        WHERE p.UserId = @UserId
          AND p.DeletedAt IS NULL
          AND h.DeletedAt IS NULL
          AND h.IsActive = 1
          AND it.Name NOT IN (N'Gold', N'RealEstate', N'MutualFund', N'Stock');
        SELECT @OtherAssets = @OtherAssets + ISNULL(SUM(CurrentEstimatedValue), 0)
        FROM Core.Assets WHERE UserId=@UserId AND AssetType<>N'Property' AND DeletedAt IS NULL;

        -- ---------------------------------------------------------------
        -- Calculate Liabilities
        -- ---------------------------------------------------------------
        -- Loan outstanding
        DECLARE @LoanOutstanding DECIMAL(18,2) = 0;
        SELECT @LoanOutstanding = ISNULL(SUM(OutstandingPrincipal), 0)
        FROM Loan.Loans
        WHERE UserId = @UserId
          AND Status = N'Active'
          AND DeletedAt IS NULL;

        -- Credit card outstanding
        DECLARE @CreditCardOutstanding DECIMAL(18,2) = 0;
        SELECT @CreditCardOutstanding = ISNULL(SUM(CASE WHEN Balance < 0 THEN ABS(Balance) ELSE 0 END), 0)
        FROM Core.Accounts
        WHERE UserId = @UserId
          AND IsActive = 1
          AND DeletedAt IS NULL
          AND AccountTypeId IN (SELECT Id FROM Core.AccountTypes WHERE Name = N'CreditCard');

        -- Other liabilities
        DECLARE @OtherLiabilities DECIMAL(18,2) = 0;

        -- ---------------------------------------------------------------
        -- Compute totals
        -- ---------------------------------------------------------------
        DECLARE @TotalAssets      DECIMAL(18,2) = @CashAndBank + @InvestmentValue + @EPFValue + @RealEstateValue + @OtherAssets;
        DECLARE @TotalLiabilities DECIMAL(18,2) = @LoanOutstanding + @CreditCardOutstanding + @OtherLiabilities;
        DECLARE @NetWorth         DECIMAL(18,2) = @TotalAssets - @TotalLiabilities;

        -- Calculate change from previous snapshot
        DECLARE @PreviousNetWorth     DECIMAL(18,2) = NULL;
        DECLARE @ChangeFromPrevious   DECIMAL(18,2) = NULL;
        DECLARE @ChangePctFromPrevious DECIMAL(8,4) = NULL;

        SELECT TOP 1 @PreviousNetWorth = NetWorth
        FROM Analytics.NetWorthSnapshots
        WHERE UserId = @UserId
          AND SnapshotDate < @SnapshotDate
        ORDER BY SnapshotDate DESC;

        IF @PreviousNetWorth IS NOT NULL
        BEGIN
            SET @ChangeFromPrevious = @NetWorth - @PreviousNetWorth;
            IF @PreviousNetWorth <> 0
                SET @ChangePctFromPrevious = (@ChangeFromPrevious * 100.0) / ABS(@PreviousNetWorth);
        END

        -- Upsert snapshot (one per user per date)
        IF EXISTS (
            SELECT 1 FROM Analytics.NetWorthSnapshots
            WHERE UserId = @UserId AND SnapshotDate = @SnapshotDate
        )
        BEGIN
            UPDATE Analytics.NetWorthSnapshots
            SET
                TotalAssets            = @TotalAssets,
                TotalLiabilities       = @TotalLiabilities,
                NetWorth               = @NetWorth,
                CashAndBank            = @CashAndBank,
                InvestmentValue        = @InvestmentValue,
                RealEstateValue        = @RealEstateValue,
                GoldValue              = @GoldValue,
                OtherAssets            = @OtherAssets,
                LoanOutstanding        = @LoanOutstanding,
                CreditCardOutstanding  = @CreditCardOutstanding,
                OtherLiabilities       = @OtherLiabilities,
                ChangeFromPrevious     = @ChangeFromPrevious,
                ChangePctFromPrevious  = @ChangePctFromPrevious
            WHERE UserId = @UserId AND SnapshotDate = @SnapshotDate;
        END
        ELSE
        BEGIN
            INSERT INTO Analytics.NetWorthSnapshots
            (
                UserId, SnapshotDate, TotalAssets, TotalLiabilities, NetWorth,
                CashAndBank, InvestmentValue, RealEstateValue, GoldValue, OtherAssets,
                LoanOutstanding, CreditCardOutstanding, OtherLiabilities,
                ChangeFromPrevious, ChangePctFromPrevious
            )
            VALUES
            (
                @UserId, @SnapshotDate, @TotalAssets, @TotalLiabilities, @NetWorth,
                @CashAndBank, @InvestmentValue, @RealEstateValue, @GoldValue, @OtherAssets,
                @LoanOutstanding, @CreditCardOutstanding, @OtherLiabilities,
                @ChangeFromPrevious, @ChangePctFromPrevious
            );
        END

        -- Return the snapshot
        SELECT
            @SnapshotDate          AS SnapshotDate,
            @TotalAssets           AS TotalAssets,
            @TotalLiabilities      AS TotalLiabilities,
            @NetWorth              AS NetWorth,
            @CashAndBank           AS CashAndBank,
            @InvestmentValue       AS InvestmentValue,
            @EPFValue              AS EPFValue,
            @RealEstateValue       AS RealEstateValue,
            @GoldValue             AS GoldValue,
            @OtherAssets           AS OtherAssets,
            @LoanOutstanding       AS LoanOutstanding,
            @CreditCardOutstanding AS CreditCardOutstanding,
            @ChangeFromPrevious    AS ChangeFromPrevious,
            @ChangePctFromPrevious AS ChangePctFromPrevious;

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, NewValues)
        VALUES (
            @UserId, N'CALCULATE', N'NetWorth',
            N'{"NetWorth":' + CAST(@NetWorth AS NVARCHAR(50)) +
            N',"TotalAssets":' + CAST(@TotalAssets AS NVARCHAR(50)) +
            N',"TotalLiabilities":' + CAST(@TotalLiabilities AS NVARCHAR(50)) + N'}'
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
-- SP: Analytics.sp_CalculateFinancialScore
-- Description: Compute composite financial score with sub-scores
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.sp_CalculateFinancialScore', N'P') IS NOT NULL
    DROP PROCEDURE Analytics.sp_CalculateFinancialScore;
GO

CREATE PROCEDURE Analytics.sp_CalculateFinancialScore
    @UserId     BIGINT,
    @ScoreDate  DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Id = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('User with Id %d does not exist.', 16, 1, @UserId);
            RETURN;
        END

        IF @ScoreDate IS NULL
            SET @ScoreDate = CAST(SYSUTCDATETIME() AS DATE);

        -- ---------------------------------------------------------------
        -- Gather financial data for scoring
        -- ---------------------------------------------------------------

        -- Monthly income & expenses (last 3 months average)
        DECLARE @MonthlyIncome  DECIMAL(18,2) = 0;
        DECLARE @MonthlyExpense DECIMAL(18,2) = 0;
        DECLARE @MonthlySavings DECIMAL(18,2) = 0;

        SELECT
            @MonthlyIncome  = ISNULL(AVG(CASE WHEN Type = N'Income'  THEN Amount ELSE 0 END), 0),
            @MonthlyExpense = ISNULL(AVG(CASE WHEN Type = N'Expense' THEN Amount ELSE 0 END), 0)
        FROM (
            SELECT
                YEAR(TransactionDate) * 100 + MONTH(TransactionDate) AS YM,
                Type,
                SUM(Amount) AS Amount
            FROM Core.Transactions
            WHERE UserId = @UserId
              AND DeletedAt IS NULL
              AND Type IN (N'Income', N'Expense')
              AND ParentTransactionId IS NULL
              AND TransactionDate >= DATEADD(MONTH, -3, @ScoreDate)
            GROUP BY YEAR(TransactionDate) * 100 + MONTH(TransactionDate), Type
        ) MonthlyData;

        SET @MonthlySavings = @MonthlyIncome - @MonthlyExpense;

        -- Savings rate
        DECLARE @SavingsRatePct DECIMAL(5,2) = 0;
        IF @MonthlyIncome > 0
            SET @SavingsRatePct = (@MonthlySavings * 100.0) / @MonthlyIncome;

        -- Total debt (active loans + credit card)
        DECLARE @TotalDebt DECIMAL(18,2) = 0;
        SELECT @TotalDebt = ISNULL(SUM(OutstandingPrincipal), 0)
        FROM Loan.Loans
        WHERE UserId = @UserId AND Status = N'Active' AND DeletedAt IS NULL;

        -- Debt-to-income ratio (monthly)
        DECLARE @DebtToIncomeRatio DECIMAL(5,2) = 0;
        IF @MonthlyIncome > 0
            SET @DebtToIncomeRatio = (@TotalDebt * 100.0) / (@MonthlyIncome * 12);  -- Annualized

        -- Emergency fund (cash & bank balance / monthly expenses)
        DECLARE @EmergencyFundMonths DECIMAL(5,2) = 0;
        DECLARE @CashBankBalance DECIMAL(18,2) = 0;
        SELECT @CashBankBalance = ISNULL(SUM(Balance), 0)
        FROM Core.Accounts
        WHERE UserId = @UserId AND IsActive = 1 AND DeletedAt IS NULL
          AND AccountTypeId IN (SELECT Id FROM Core.AccountTypes WHERE Name IN (N'Savings', N'Current', N'Cash', N'Wallet'));

        IF @MonthlyExpense > 0
            SET @EmergencyFundMonths = @CashBankBalance / @MonthlyExpense;

        -- Total investments
        DECLARE @TotalInvestments DECIMAL(18,2) = 0;
        SELECT @TotalInvestments = ISNULL(SUM(ISNULL(CurrentValue, InvestedAmount)), 0)
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        WHERE p.UserId = @UserId AND p.DeletedAt IS NULL AND h.DeletedAt IS NULL AND h.IsActive = 1;

        DECLARE @InvestmentToIncomeRatio DECIMAL(5,2) = 0;
        IF @MonthlyIncome > 0
            SET @InvestmentToIncomeRatio = (@TotalInvestments * 100.0) / (@MonthlyIncome * 12);

        -- Goal progress (average completion % of active goals)
        DECLARE @GoalProgressScore INT = 0;
        DECLARE @AvgGoalCompletion DECIMAL(5,2) = 0;
        SELECT @AvgGoalCompletion = ISNULL(AVG(
            CASE WHEN TargetAmount > 0 THEN (CurrentAmount * 100.0 / TargetAmount) ELSE 0 END
        ), 0)
        FROM Goals.Goals
        WHERE UserId = @UserId AND Status = N'InProgress' AND DeletedAt IS NULL;

        SET @GoalProgressScore = CAST(@AvgGoalCompletion AS INT);
        IF @GoalProgressScore > 200 SET @GoalProgressScore = 200;

        -- ---------------------------------------------------------------
        -- Calculate sub-scores (each 0-200, total 0-1000)
        -- ---------------------------------------------------------------

        -- 1. Savings Rate Score (0-200)
        -- Best: >30%, Good: 20-30%, Average: 10-20%, Poor: <10%
        DECLARE @SavingsRateScore INT = CASE
            WHEN @SavingsRatePct >= 30 THEN 200
            WHEN @SavingsRatePct >= 20 THEN 150 + CAST((@SavingsRatePct - 20) * 5 AS INT)
            WHEN @SavingsRatePct >= 10 THEN 80 + CAST((@SavingsRatePct - 10) * 7 AS INT)
            WHEN @SavingsRatePct >= 0  THEN CAST(@SavingsRatePct * 8 AS INT)
            ELSE 0   -- Negative savings
        END;

        -- 2. Debt-to-Income Score (0-200)
        -- Best: <20%, Good: 20-36%, Average: 36-50%, Poor: >50%
        DECLARE @DebtToIncomeScore INT = CASE
            WHEN @DebtToIncomeRatio = 0   THEN 200   -- No debt
            WHEN @DebtToIncomeRatio < 20  THEN 180 + CAST((20 - @DebtToIncomeRatio) AS INT)
            WHEN @DebtToIncomeRatio < 36  THEN 120 + CAST((36 - @DebtToIncomeRatio) * 3.75 AS INT)
            WHEN @DebtToIncomeRatio < 50  THEN 60 + CAST((50 - @DebtToIncomeRatio) * 4.29 AS INT)
            WHEN @DebtToIncomeRatio < 100 THEN 10 + CAST((100 - @DebtToIncomeRatio) * 1 AS INT)
            ELSE 0
        END;

        -- 3. Emergency Fund Score (0-200)
        -- Best: >=6 months, Good: 3-6, Average: 1-3, Poor: <1
        DECLARE @EmergencyFundScore INT = CASE
            WHEN @EmergencyFundMonths >= 6 THEN 200
            WHEN @EmergencyFundMonths >= 3 THEN 140 + CAST((@EmergencyFundMonths - 3) * 20 AS INT)
            WHEN @EmergencyFundMonths >= 1 THEN 60 + CAST((@EmergencyFundMonths - 1) * 40 AS INT)
            WHEN @EmergencyFundMonths > 0  THEN CAST(@EmergencyFundMonths * 60 AS INT)
            ELSE 0
        END;

        -- 4. Investment Score (0-200)
        -- Based on investment-to-income ratio and diversification
        DECLARE @InvestmentScore INT = CASE
            WHEN @InvestmentToIncomeRatio >= 100 THEN 200   -- Investments > annual income
            WHEN @InvestmentToIncomeRatio >= 50  THEN 150 + CAST((@InvestmentToIncomeRatio - 50) AS INT)
            WHEN @InvestmentToIncomeRatio >= 20  THEN 80 + CAST((@InvestmentToIncomeRatio - 20) * 2.33 AS INT)
            WHEN @InvestmentToIncomeRatio > 0    THEN CAST(@InvestmentToIncomeRatio * 4 AS INT)
            ELSE 0
        END;

        -- Cap goal progress score at 200
        IF @GoalProgressScore > 200 SET @GoalProgressScore = 200;

        -- ---------------------------------------------------------------
        -- Calculate overall score
        -- ---------------------------------------------------------------
        DECLARE @OverallScore INT = @SavingsRateScore + @DebtToIncomeScore + @EmergencyFundScore + @InvestmentScore + @GoalProgressScore;

        -- Cap at 1000
        IF @OverallScore > 1000 SET @OverallScore = 1000;
        IF @OverallScore < 0 SET @OverallScore = 0;

        -- Determine grade
        DECLARE @ScoreGrade NVARCHAR(2) = CASE
            WHEN @OverallScore >= 900 THEN N'A+'
            WHEN @OverallScore >= 800 THEN N'A'
            WHEN @OverallScore >= 700 THEN N'B+'
            WHEN @OverallScore >= 600 THEN N'B'
            WHEN @OverallScore >= 500 THEN N'C'
            WHEN @OverallScore >= 350 THEN N'D'
            ELSE N'E'
        END;

        -- Build recommendations JSON
        DECLARE @Recommendations NVARCHAR(MAX) = N'[';
        DECLARE @RecCount INT = 0;

        IF @SavingsRatePct < 20
        BEGIN
            SET @Recommendations = @Recommendations + N'{"Area":"Savings","Message":"Your savings rate is below 20%. Aim to save at least 20% of income."},';
            SET @RecCount = @RecCount + 1;
        END
        IF @DebtToIncomeRatio > 36
        BEGIN
            SET @Recommendations = @Recommendations + N'{"Area":"Debt","Message":"Your debt-to-income ratio is above 36%. Focus on reducing high-interest debt."},';
            SET @RecCount = @RecCount + 1;
        END
        IF @EmergencyFundMonths < 3
        BEGIN
            SET @Recommendations = @Recommendations + N'{"Area":"EmergencyFund","Message":"Build an emergency fund covering at least 3 months of expenses."},';
            SET @RecCount = @RecCount + 1;
        END
        IF @InvestmentToIncomeRatio < 20
        BEGIN
            SET @Recommendations = @Recommendations + N'{"Area":"Investment","Message":"Consider investing more. Start with SIPs for disciplined investing."},';
            SET @RecCount = @RecCount + 1;
        END

        -- Remove trailing comma if any and close array
        IF @RecCount > 0
            SET @Recommendations = LEFT(@Recommendations, LEN(@Recommendations) - 1) + N']';
        ELSE
            SET @Recommendations = N'[{"Area":"General","Message":"Your finances look healthy! Keep up the good work."}]';

        -- Insert/Update financial score
        IF EXISTS (
            SELECT 1 FROM Analytics.FinancialScore
            WHERE UserId = @UserId AND ScoreDate = @ScoreDate
        )
        BEGIN
            UPDATE Analytics.FinancialScore
            SET
                OverallScore          = @OverallScore,
                ScoreGrade            = @ScoreGrade,
                SavingsRateScore      = @SavingsRateScore,
                DebtToIncomeScore     = @DebtToIncomeScore,
                EmergencyFundScore    = @EmergencyFundScore,
                InvestmentScore       = @InvestmentScore,
                GoalProgressScore     = @GoalProgressScore,
                SavingsRatePct        = @SavingsRatePct,
                DebtToIncomeRatio     = @DebtToIncomeRatio,
                EmergencyFundMonths   = @EmergencyFundMonths,
                InvestmentToIncomeRatio = @InvestmentToIncomeRatio,
                MonthlyIncome         = @MonthlyIncome,
                MonthlyExpenses       = @MonthlyExpense,
                MonthlySavings        = @MonthlySavings,
                TotalDebt             = @TotalDebt,
                TotalInvestments      = @TotalInvestments,
                Recommendations       = @Recommendations
            WHERE UserId = @UserId AND ScoreDate = @ScoreDate;
        END
        ELSE
        BEGIN
            INSERT INTO Analytics.FinancialScore
            (
                UserId, ScoreDate, OverallScore, ScoreGrade,
                SavingsRateScore, DebtToIncomeScore, EmergencyFundScore,
                InvestmentScore, GoalProgressScore,
                SavingsRatePct, DebtToIncomeRatio, EmergencyFundMonths,
                InvestmentToIncomeRatio, MonthlyIncome, MonthlyExpenses,
                MonthlySavings, TotalDebt, TotalInvestments, Recommendations
            )
            VALUES
            (
                @UserId, @ScoreDate, @OverallScore, @ScoreGrade,
                @SavingsRateScore, @DebtToIncomeScore, @EmergencyFundScore,
                @InvestmentScore, @GoalProgressScore,
                @SavingsRatePct, @DebtToIncomeRatio, @EmergencyFundMonths,
                @InvestmentToIncomeRatio, @MonthlyIncome, @MonthlyExpense,
                @MonthlySavings, @TotalDebt, @TotalInvestments, @Recommendations
            );
        END

        -- Return the score
        SELECT
            @OverallScore       AS OverallScore,
            @ScoreGrade         AS ScoreGrade,
            @SavingsRateScore   AS SavingsRateScore,
            @DebtToIncomeScore  AS DebtToIncomeScore,
            @EmergencyFundScore AS EmergencyFundScore,
            @InvestmentScore    AS InvestmentScore,
            @GoalProgressScore  AS GoalProgressScore,
            @SavingsRatePct     AS SavingsRatePct,
            @DebtToIncomeRatio  AS DebtToIncomeRatio,
            @EmergencyFundMonths AS EmergencyFundMonths,
            @InvestmentToIncomeRatio AS InvestmentToIncomeRatio;
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
-- SP: Analytics.sp_GenerateMonthlyAggregates
-- Description: Build monthly aggregate for a user and year-month
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.sp_GenerateMonthlyAggregates', N'P') IS NOT NULL
    DROP PROCEDURE Analytics.sp_GenerateMonthlyAggregates;
GO

CREATE PROCEDURE Analytics.sp_GenerateMonthlyAggregates
    @UserId    BIGINT,
    @Year      INT,
    @Month     INT           -- 1-12; use 0 for all months of the year
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Month <> 0 AND (@Month < 1 OR @Month > 12)
        BEGIN
            RAISERROR('Month must be between 1 and 12, or 0 for all months.', 16, 1);
            RETURN;
        END

        -- Determine months to process
        CREATE TABLE #MonthsToProcess (YearMonth INT, StartDate DATE, EndDate DATE);

        IF @Month = 0
        BEGIN
            DECLARE @m INT = 1;
            WHILE @m <= 12
            BEGIN
                INSERT INTO #MonthsToProcess (YearMonth, StartDate, EndDate)
                VALUES (
                    @Year * 100 + @m,
                    DATEFROMPARTS(@Year, @m, 1),
                    EOMONTH(DATEFROMPARTS(@Year, @m, 1))
                );
                SET @m = @m + 1;
            END
        END
        ELSE
        BEGIN
            INSERT INTO #MonthsToProcess (YearMonth, StartDate, EndDate)
            VALUES (
                @Year * 100 + @Month,
                DATEFROMPARTS(@Year, @Month, 1),
                EOMONTH(DATEFROMPARTS(@Year, @Month, 1))
            );
        END

        -- Process each month
        DECLARE @YM        INT;
        DECLARE @StartDt   DATE;
        DECLARE @EndDt     DATE;

        DECLARE month_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT YearMonth, StartDate, EndDate FROM #MonthsToProcess;

        OPEN month_cursor;
        FETCH NEXT FROM month_cursor INTO @YM, @StartDt, @EndDt;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @TotalIncome     DECIMAL(18,2) = 0;
            DECLARE @TotalExpense    DECIMAL(18,2) = 0;
            DECLARE @TotalSavings    DECIMAL(18,2) = 0;
            DECLARE @SavingsRate     DECIMAL(5,2)  = NULL;
            DECLARE @TopCategory     NVARCHAR(100) = NULL;
            DECLARE @TopAmount       DECIMAL(18,2) = NULL;
            DECLARE @TxnCount        INT           = 0;
            DECLARE @BreakdownJSON   NVARCHAR(MAX) = NULL;

            -- Calculate aggregates
            SELECT
                @TotalIncome  = ISNULL(SUM(CASE WHEN Type = N'Income'  THEN Amount ELSE 0 END), 0),
                @TotalExpense = ISNULL(SUM(CASE WHEN Type = N'Expense' THEN Amount ELSE 0 END), 0),
                @TxnCount     = COUNT(*)
            FROM Core.Transactions
            WHERE UserId = @UserId
              AND DeletedAt IS NULL
              AND Type IN (N'Income', N'Expense')
              AND ParentTransactionId IS NULL
              AND TransactionDate BETWEEN @StartDt AND @EndDt;

            SET @TotalSavings = @TotalIncome - @TotalExpense;
            IF @TotalIncome > 0
                SET @SavingsRate = ROUND((@TotalSavings * 100.0) / @TotalIncome, 2);

            -- Top expense category
            SELECT TOP 1
                @TopCategory = ISNULL(c.Name, N'Uncategorized'),
                @TopAmount   = SUM(t.Amount)
            FROM Core.Transactions t
            LEFT JOIN Core.Categories c ON t.CategoryId = c.Id
            WHERE t.UserId = @UserId
              AND t.DeletedAt IS NULL
              AND t.Type = N'Expense'
              AND t.ParentTransactionId IS NULL
              AND t.TransactionDate BETWEEN @StartDt AND @EndDt
            GROUP BY c.Name
            ORDER BY SUM(t.Amount) DESC;

            -- Category breakdown JSON
            SET @BreakdownJSON = (
                SELECT c.Name AS Category, c.Type AS CategoryType, SUM(t.Amount) AS Total
                FROM Core.Transactions t
                LEFT JOIN Core.Categories c ON t.CategoryId = c.Id
                WHERE t.UserId = @UserId
                  AND t.DeletedAt IS NULL
                  AND t.Type IN (N'Income', N'Expense')
                  AND t.ParentTransactionId IS NULL
                  AND t.TransactionDate BETWEEN @StartDt AND @EndDt
                GROUP BY c.Name, c.Type
                ORDER BY Total DESC
                FOR JSON PATH
            );

            -- Upsert
            IF EXISTS (
                SELECT 1 FROM Analytics.MonthlyAggregates
                WHERE UserId = @UserId AND YearMonth = @YM
            )
            BEGIN
                UPDATE Analytics.MonthlyAggregates
                SET
                    TotalIncome        = @TotalIncome,
                    TotalExpense       = @TotalExpense,
                    TotalSavings       = @TotalSavings,
                    SavingsRate        = @SavingsRate,
                    TopExpenseCategory = @TopCategory,
                    TopExpenseAmount   = @TopAmount,
                    TransactionCount   = @TxnCount,
                    CategoryBreakdown  = @BreakdownJSON,
                    UpdatedAt          = SYSUTCDATETIME()
                WHERE UserId = @UserId AND YearMonth = @YM;
            END
            ELSE
            BEGIN
                INSERT INTO Analytics.MonthlyAggregates
                (UserId, YearMonth, TotalIncome, TotalExpense, TotalSavings,
                 SavingsRate, TopExpenseCategory, TopExpenseAmount,
                 TransactionCount, CategoryBreakdown)
                VALUES
                (@UserId, @YM, @TotalIncome, @TotalExpense, @TotalSavings,
                 @SavingsRate, @TopCategory, @TopAmount,
                 @TxnCount, @BreakdownJSON);
            END

            FETCH NEXT FROM month_cursor INTO @YM, @StartDt, @EndDt;
        END

        CLOSE month_cursor;
        DEALLOCATE month_cursor;

        -- Return generated aggregates
        SELECT
            YearMonth, TotalIncome, TotalExpense, TotalSavings,
            SavingsRate, TopExpenseCategory, TopExpenseAmount,
            TransactionCount, CreatedAt, UpdatedAt
        FROM Analytics.MonthlyAggregates
        WHERE UserId = @UserId
          AND YearMonth / 100 = @Year
          AND (@Month = 0 OR YearMonth = @Year * 100 + @Month)
        ORDER BY YearMonth;

        DROP TABLE #MonthsToProcess;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#MonthsToProcess', N'U') IS NOT NULL
            DROP TABLE #MonthsToProcess;

        IF CURSOR_STATUS(N'local', N'month_cursor') >= 0
        BEGIN
            CLOSE month_cursor;
            DEALLOCATE month_cursor;
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
-- SP: Analytics.sp_GetSpendingTrends
-- Description: 6-month spending trend by category
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.sp_GetSpendingTrends', N'P') IS NOT NULL
    DROP PROCEDURE Analytics.sp_GetSpendingTrends;
GO

CREATE PROCEDURE Analytics.sp_GetSpendingTrends
    @UserId    BIGINT,
    @Months    INT = 6       -- Number of months to look back
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Months < 1 OR @Months > 24
        BEGIN
            RAISERROR('Months must be between 1 and 24.', 16, 1);
            RETURN;
        END

        DECLARE @StartDate DATE = DATEADD(MONTH, -@Months, CAST(SYSUTCDATETIME() AS DATE));

        -- Monthly spending by category
        SELECT
            YEAR(t.TransactionDate) * 100 + MONTH(t.TransactionDate) AS YearMonth,
            ISNULL(c.Name, N'Uncategorized') AS CategoryName,
            c.Icon                           AS CategoryIcon,
            SUM(t.Amount)                    AS TotalSpent,
            COUNT(*)                         AS TransactionCount
        FROM Core.Transactions t
        LEFT JOIN Core.Categories c ON t.CategoryId = c.Id
        WHERE t.UserId          = @UserId
          AND t.DeletedAt       IS NULL
          AND t.Type            = N'Expense'
          AND t.ParentTransactionId IS NULL
          AND t.TransactionDate >= @StartDate
        GROUP BY YEAR(t.TransactionDate) * 100 + MONTH(t.TransactionDate),
                 c.Name, c.Icon
        ORDER BY YearMonth, TotalSpent DESC;

        -- Monthly total trend
        SELECT
            YEAR(t.TransactionDate) * 100 + MONTH(t.TransactionDate) AS YearMonth,
            SUM(t.Amount)    AS TotalSpent,
            COUNT(*)         AS TransactionCount,
            COUNT(DISTINCT t.CategoryId) AS CategoryCount
        FROM Core.Transactions t
        WHERE t.UserId          = @UserId
          AND t.DeletedAt       IS NULL
          AND t.Type            = N'Expense'
          AND t.ParentTransactionId IS NULL
          AND t.TransactionDate >= @StartDate
        GROUP BY YEAR(t.TransactionDate) * 100 + MONTH(t.TransactionDate)
        ORDER BY YearMonth;
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
-- SP: Analytics.sp_GetIncomeVsExpenseTrend
-- Description: Monthly income vs expense for N months
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.sp_GetIncomeVsExpenseTrend', N'P') IS NOT NULL
    DROP PROCEDURE Analytics.sp_GetIncomeVsExpenseTrend;
GO

CREATE PROCEDURE Analytics.sp_GetIncomeVsExpenseTrend
    @UserId    BIGINT,
    @Months    INT = 6       -- Number of months to look back
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Months < 1 OR @Months > 60
        BEGIN
            RAISERROR('Months must be between 1 and 60.', 16, 1);
            RETURN;
        END

        DECLARE @StartDate DATE = DATEADD(MONTH, -@Months, CAST(SYSUTCDATETIME() AS DATE));

        SELECT
            YEAR(t.TransactionDate) * 100 + MONTH(t.TransactionDate) AS YearMonth,
            ISNULL(SUM(CASE WHEN t.Type = N'Income'  THEN t.Amount ELSE 0 END), 0) AS TotalIncome,
            ISNULL(SUM(CASE WHEN t.Type = N'Expense' THEN t.Amount ELSE 0 END), 0) AS TotalExpense,
            ISNULL(SUM(CASE WHEN t.Type = N'Income'  THEN t.Amount ELSE 0 END), 0)
                - ISNULL(SUM(CASE WHEN t.Type = N'Expense' THEN t.Amount ELSE 0 END), 0) AS NetSavings,
            CASE
                WHEN SUM(CASE WHEN t.Type = N'Income' THEN t.Amount ELSE 0 END) > 0
                THEN ROUND(
                    (SUM(CASE WHEN t.Type = N'Income' THEN t.Amount ELSE 0 END)
                     - SUM(CASE WHEN t.Type = N'Expense' THEN t.Amount ELSE 0 END))
                    * 100.0 / SUM(CASE WHEN t.Type = N'Income' THEN t.Amount ELSE 0 END), 2)
                ELSE 0
            END AS SavingsRatePct,
            COUNT(*) AS TransactionCount
        FROM Core.Transactions t
        WHERE t.UserId          = @UserId
          AND t.DeletedAt       IS NULL
          AND t.Type            IN (N'Income', N'Expense')
          AND t.ParentTransactionId IS NULL
          AND t.TransactionDate >= @StartDate
        GROUP BY YEAR(t.TransactionDate) * 100 + MONTH(t.TransactionDate)
        ORDER BY YearMonth;
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
-- SP: Analytics.sp_GetCategoryWiseBreakdown
-- Description: Category spending for a period with percentages
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.sp_GetCategoryWiseBreakdown', N'P') IS NOT NULL
    DROP PROCEDURE Analytics.sp_GetCategoryWiseBreakdown;
GO

CREATE PROCEDURE Analytics.sp_GetCategoryWiseBreakdown
    @UserId     BIGINT,
    @StartDate  DATE,
    @EndDate    DATE,
    @Type       NVARCHAR(20) = N'Expense'   -- Income, Expense, or both
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @StartDate > @EndDate
        BEGIN
            RAISERROR('StartDate must be on or before EndDate.', 16, 1);
            RETURN;
        END

        -- Calculate total for percentage computation
        DECLARE @GrandTotal DECIMAL(18,2) = 0;

        SELECT @GrandTotal = ISNULL(SUM(Amount), 0)
        FROM Core.Transactions
        WHERE UserId = @UserId
          AND DeletedAt IS NULL
          AND (@Type = N'Both' OR Type = @Type)
          AND ParentTransactionId IS NULL
          AND TransactionDate BETWEEN @StartDate AND @EndDate;

        -- Category-wise breakdown with percentages
        SELECT
            c.Id                       AS CategoryId,
            ISNULL(c.Name, N'Uncategorized') AS CategoryName,
            c.Icon                     AS CategoryIcon,
            c.Type                     AS CategoryType,
            SUM(t.Amount)              AS TotalAmount,
            COUNT(*)                   AS TransactionCount,
            CASE
                WHEN @GrandTotal > 0
                THEN ROUND((SUM(t.Amount) * 100.0) / @GrandTotal, 2)
                ELSE 0
            END                        AS Percentage,
            -- Average per transaction
            CASE
                WHEN COUNT(*) > 0
                THEN ROUND(SUM(t.Amount) / COUNT(*), 2)
                ELSE 0
            END                        AS AvgPerTransaction,
            -- Comparison: same period last month
            ISNULL(LM.TotalAmount, 0)  AS LastPeriodAmount,
            CASE
                WHEN ISNULL(LM.TotalAmount, 0) > 0
                THEN ROUND(((SUM(t.Amount) - ISNULL(LM.TotalAmount, 0)) * 100.0) / LM.TotalAmount, 2)
                ELSE NULL
            END                        AS ChangePctFromLastPeriod
        FROM Core.Transactions t
        LEFT JOIN Core.Categories c ON t.CategoryId = c.Id
        -- Left join for same-category same-type last-period comparison
        OUTER APPLY (
            SELECT ISNULL(SUM(t2.Amount), 0) AS TotalAmount
            FROM Core.Transactions t2
            LEFT JOIN Core.Categories c2 ON t2.CategoryId = c2.Id
            WHERE t2.UserId = @UserId
              AND t2.DeletedAt IS NULL
              AND (@Type = N'Both' OR t2.Type = @Type)
              AND t2.ParentTransactionId IS NULL
              AND t2.TransactionDate BETWEEN
                  DATEADD(DAY, -DATEDIFF(DAY, @StartDate, @EndDate) - 1, @StartDate)
                  AND DATEADD(DAY, -1, @StartDate)
              AND ISNULL(c2.Name, N'Uncategorized') = ISNULL(c.Name, N'Uncategorized')
        ) LM
        WHERE t.UserId = @UserId
          AND t.DeletedAt IS NULL
          AND (@Type = N'Both' OR t.Type = @Type)
          AND t.ParentTransactionId IS NULL
          AND t.TransactionDate BETWEEN @StartDate AND @EndDate
        GROUP BY c.Id, c.Name, c.Icon, c.Type, LM.TotalAmount
        ORDER BY TotalAmount DESC;

        -- Grand summary
        SELECT
            @GrandTotal AS GrandTotal,
            @StartDate  AS PeriodStart,
            @EndDate    AS PeriodEnd;
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

PRINT 'Analytics stored procedures created successfully.';
GO

