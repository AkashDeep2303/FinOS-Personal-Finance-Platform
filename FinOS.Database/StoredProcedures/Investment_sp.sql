-- ============================================================================
-- FinOS Database - Investment Stored Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for investment holdings, SIPs, EPF, XIRR,
--              and portfolio analytics
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- SP: Investment.sp_CreateHolding
-- Description: Insert a new investment holding
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_CreateHolding', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_CreateHolding;
GO

CREATE PROCEDURE Investment.sp_CreateHolding
    @PortfolioId       BIGINT,
    @InvestmentTypeId  INT,
    @Symbol            NVARCHAR(50),
    @Name              NVARCHAR(300),
    @Quantity          DECIMAL(18,4),
    @AvgPurchasePrice  DECIMAL(18,4),
    @CurrentPrice      DECIMAL(18,4)    = NULL,
    @CurrentValue      DECIMAL(18,2)    = NULL,
    @InvestedAmount    DECIMAL(18,2),
    @Currency          NVARCHAR(3)      = N'INR',
    @FundHouse         NVARCHAR(200)    = NULL,
    @FundCategory      NVARCHAR(100)    = NULL,
    @RiskLevel         NVARCHAR(20)     = NULL,
    @MaturityDate      DATE             = NULL,
    @InterestRate      DECIMAL(8,4)     = NULL,
    @LockInEndDate     DATE             = NULL,
    @Notes             NVARCHAR(500)    = NULL,
    @NewHoldingId      BIGINT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate portfolio
        IF NOT EXISTS (SELECT 1 FROM Investment.Portfolios WHERE Id = @PortfolioId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Portfolio with Id %d does not exist.', 16, 1, @PortfolioId);
            RETURN;
        END

        -- Validate investment type
        IF NOT EXISTS (SELECT 1 FROM Investment.InvestmentTypes WHERE Id = @InvestmentTypeId)
        BEGIN
            RAISERROR('InvestmentType with Id %d does not exist.', 16, 1, @InvestmentTypeId);
            RETURN;
        END

        -- Calculate current value if not provided
        IF @CurrentValue IS NULL AND @CurrentPrice IS NOT NULL
            SET @CurrentValue = @Quantity * @CurrentPrice;

        -- Calculate total return
        DECLARE @TotalReturn DECIMAL(18,2) = NULL;
        DECLARE @TotalReturnPct DECIMAL(8,4) = NULL;

        IF @CurrentValue IS NOT NULL AND @InvestedAmount > 0
        BEGIN
            SET @TotalReturn    = @CurrentValue - @InvestedAmount;
            SET @TotalReturnPct = (@TotalReturn / @InvestedAmount) * 100;
        END

        INSERT INTO Investment.Holdings
        (
            PortfolioId, InvestmentTypeId, Symbol, Name, Quantity,
            AvgPurchasePrice, CurrentPrice, CurrentValue, InvestedAmount,
            TotalReturn, TotalReturnPct, Currency, FundHouse, FundCategory,
            RiskLevel, MaturityDate, InterestRate, LockInEndDate, Notes
        )
        VALUES
        (
            @PortfolioId, @InvestmentTypeId, @Symbol, @Name, @Quantity,
            @AvgPurchasePrice, @CurrentPrice, @CurrentValue, @InvestedAmount,
            @TotalReturn, @TotalReturnPct, @Currency, @FundHouse, @FundCategory,
            @RiskLevel, @MaturityDate, @InterestRate, @LockInEndDate, @Notes
        );

        SET @NewHoldingId = SCOPE_IDENTITY();

        -- Audit log
        DECLARE @UserId BIGINT;
        SELECT @UserId = UserId FROM Investment.Portfolios WHERE Id = @PortfolioId;

        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'Holding', CAST(@NewHoldingId AS NVARCHAR(256)),
            N'{"Symbol":"' + @Symbol + N'","Name":"' + REPLACE(@Name, '"', '"') +
            N'","InvestedAmount":' + CAST(@InvestedAmount AS NVARCHAR(50)) + N'}'
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
-- SP: Investment.sp_UpdateHoldingPrice
-- Description: Update current price, value, and returns for a holding
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_UpdateHoldingPrice', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_UpdateHoldingPrice;
GO

CREATE PROCEDURE Investment.sp_UpdateHoldingPrice
    @HoldingId      BIGINT,
    @CurrentPrice   DECIMAL(18,4),
    @NAVDate        DATE             = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Investment.Holdings WHERE Id = @HoldingId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Holding with Id %d does not exist.', 16, 1, @HoldingId);
            RETURN;
        END

        -- Get current holding details
        DECLARE @Quantity          DECIMAL(18,4);
        DECLARE @InvestedAmount    DECIMAL(18,2);
        DECLARE @OldPrice          DECIMAL(18,4);
        DECLARE @DividendReceived  DECIMAL(18,2);

        SELECT
            @Quantity         = Quantity,
            @InvestedAmount   = InvestedAmount,
            @OldPrice         = ISNULL(CurrentPrice, AvgPurchasePrice),
            @DividendReceived = DividendReceived
        FROM Investment.Holdings
        WHERE Id = @HoldingId;

        -- Calculate new values
        DECLARE @CurrentValue    DECIMAL(18,2) = @Quantity * @CurrentPrice;
        DECLARE @DayChange       DECIMAL(18,2) = (@CurrentPrice - @OldPrice) * @Quantity;
        DECLARE @DayChangePct    DECIMAL(8,4)  = CASE
                                                    WHEN @OldPrice > 0
                                                    THEN ((@CurrentPrice - @OldPrice) / @OldPrice) * 100
                                                    ELSE 0
                                                 END;
        DECLARE @TotalReturn     DECIMAL(18,2) = @CurrentValue - @InvestedAmount + @DividendReceived;
        DECLARE @TotalReturnPct  DECIMAL(8,4)  = CASE
                                                    WHEN @InvestedAmount > 0
                                                    THEN (@TotalReturn / @InvestedAmount) * 100
                                                    ELSE 0
                                                 END;

        -- Update the holding
        UPDATE Investment.Holdings
        SET
            CurrentPrice       = @CurrentPrice,
            CurrentValue       = @CurrentValue,
            DayChange          = @DayChange,
            DayChangePct       = @DayChangePct,
            TotalReturn        = @TotalReturn,
            TotalReturnPct     = @TotalReturnPct,
            NAVDate            = ISNULL(@NAVDate, CAST(SYSUTCDATETIME() AS DATE)),
            LastPriceUpdateAt  = SYSUTCDATETIME(),
            UpdatedAt          = SYSUTCDATETIME()
        WHERE Id = @HoldingId;
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
-- SP: Investment.sp_RecordInvestmentTransaction
-- Description: Record a Buy/Sell with charges breakdown; update holding
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_RecordInvestmentTransaction', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_RecordInvestmentTransaction;
GO

CREATE PROCEDURE Investment.sp_RecordInvestmentTransaction
    @HoldingId          BIGINT,
    @TransactionType    NVARCHAR(30),       -- Buy, Sell, Dividend, SIP, Switch, STT, StampDuty
    @Quantity           DECIMAL(18,4),
    @PricePerUnit       DECIMAL(18,4),
    @TotalAmount        DECIMAL(18,2),
    @Charges            DECIMAL(18,2)   = 0,
    @STT                DECIMAL(18,2)   = 0,
    @StampDuty          DECIMAL(18,2)   = 0,
    @TransactionDate    DATE,
    @SettlementDate     DATE            = NULL,
    @SIPId              BIGINT          = NULL,
    @Notes              NVARCHAR(500)   = NULL,
    @SourceAccount      BIGINT          = NULL,
    @NewTransactionId   BIGINT          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate holding
        IF NOT EXISTS (SELECT 1 FROM Investment.Holdings WHERE Id = @HoldingId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Holding with Id %d does not exist.', 16, 1, @HoldingId);
            RETURN;
        END

        -- Validate transaction type
        IF @TransactionType NOT IN (N'Buy', N'Sell', N'Dividend', N'SIP', N'Switch', N'STT', N'StampDuty')
        BEGIN
            RAISERROR('Invalid TransactionType. Must be Buy, Sell, Dividend, SIP, Switch, STT, or StampDuty.', 16, 1);
            RETURN;
        END

        -- Insert the investment transaction
        INSERT INTO Investment.Transactions
        (
            HoldingId, TransactionType, Quantity, PricePerUnit, TotalAmount,
            Charges, STT, StampDuty, TransactionDate, SettlementDate,
            SIPId, Notes, SourceAccount
        )
        VALUES
        (
            @HoldingId, @TransactionType, @Quantity, @PricePerUnit, @TotalAmount,
            @Charges, @STT, @StampDuty, @TransactionDate, @SettlementDate,
            @SIPId, @Notes, @SourceAccount
        );

        SET @NewTransactionId = SCOPE_IDENTITY();

        -- Update the holding based on transaction type
        IF @TransactionType = N'Buy' OR @TransactionType = N'SIP'
        BEGIN
            -- Recalculate average purchase price (weighted average)
            DECLARE @OldQty          DECIMAL(18,4);
            DECLARE @OldAvgPrice     DECIMAL(18,4);
            DECLARE @OldInvested     DECIMAL(18,2);

            SELECT @OldQty = Quantity, @OldAvgPrice = AvgPurchasePrice, @OldInvested = InvestedAmount
            FROM Investment.Holdings WHERE Id = @HoldingId;

            DECLARE @NewQty          DECIMAL(18,4) = @OldQty + @Quantity;
            DECLARE @NewInvested     DECIMAL(18,2) = @OldInvested + @TotalAmount;
            DECLARE @NewAvgPrice     DECIMAL(18,4) = CASE
                                                        WHEN @NewQty > 0
                                                        THEN @NewInvested / @NewQty
                                                        ELSE @OldAvgPrice
                                                     END;

            -- Get current price for value calculation
            DECLARE @CurrPrice DECIMAL(18,4);
            SELECT @CurrPrice = ISNULL(CurrentPrice, @NewAvgPrice)
            FROM Investment.Holdings WHERE Id = @HoldingId;

            DECLARE @NewCurrValue DECIMAL(18,2) = @NewQty * @CurrPrice;
            DECLARE @NewTotalReturn DECIMAL(18,2) = @NewCurrValue - @NewInvested;
            DECLARE @NewTotalReturnPct DECIMAL(8,4) = CASE
                                                         WHEN @NewInvested > 0
                                                         THEN (@NewTotalReturn / @NewInvested) * 100
                                                         ELSE 0
                                                      END;

            UPDATE Investment.Holdings
            SET
                Quantity          = @NewQty,
                AvgPurchasePrice  = @NewAvgPrice,
                InvestedAmount    = @NewInvested,
                CurrentValue      = @NewCurrValue,
                TotalReturn       = @NewTotalReturn,
                TotalReturnPct    = @NewTotalReturnPct,
                UpdatedAt         = SYSUTCDATETIME()
            WHERE Id = @HoldingId;

            -- Debit source account
            IF @SourceAccount IS NOT NULL
                DECLARE @DebitDelta DECIMAL(18,2) = -@TotalAmount;
                EXEC Core.sp_UpdateAccountBalance @AccountId = @SourceAccount, @DeltaAmount = @DebitDelta;
        END
        ELSE IF @TransactionType = N'Sell'
        BEGIN
            -- Validate sufficient quantity
            DECLARE @HoldQty DECIMAL(18,4);
            SELECT @HoldQty = Quantity FROM Investment.Holdings WHERE Id = @HoldingId;

            IF @Quantity > @HoldQty
            BEGIN
                DECLARE @QuantityText NVARCHAR(50) = CAST(@Quantity AS NVARCHAR(50));
                DECLARE @HoldQtyText NVARCHAR(50) = CAST(@HoldQty AS NVARCHAR(50));
                RAISERROR('Cannot sell %s units; holding only has %s units.', 16, 1,
                    @QuantityText, @HoldQtyText);
                RETURN;
            END

            DECLARE @SellQty       DECIMAL(18,4) = @HoldQty - @Quantity;
            DECLARE @SellAvgPrice  DECIMAL(18,4) = (SELECT AvgPurchasePrice FROM Investment.Holdings WHERE Id = @HoldingId);
            DECLARE @SellInvested  DECIMAL(18,2) = @SellQty * @SellAvgPrice;
            DECLARE @DisposedCostBasis DECIMAL(18,2) = @Quantity * @SellAvgPrice;
            DECLARE @RealizedGain DECIMAL(18,2) = @TotalAmount - @Charges - @STT - @StampDuty - @DisposedCostBasis;
            DECLARE @SellPrice     DECIMAL(18,4);
            SELECT @SellPrice = ISNULL(CurrentPrice, @PricePerUnit)
            FROM Investment.Holdings WHERE Id = @HoldingId;

            DECLARE @SellValue     DECIMAL(18,2) = @SellQty * @SellPrice;
            DECLARE @SellReturn    DECIMAL(18,2) = @SellValue - @SellInvested;
            DECLARE @SellReturnPct DECIMAL(8,4)  = CASE
                                                       WHEN @SellInvested > 0
                                                       THEN (@SellReturn / @SellInvested) * 100
                                                       ELSE 0
                                                    END;

            UPDATE Investment.Holdings
            SET
                Quantity       = @SellQty,
                InvestedAmount = @SellInvested,
                CurrentValue   = CASE WHEN @SellQty = 0 THEN 0 ELSE @SellValue END,
                TotalReturn    = CASE WHEN @SellQty = 0 THEN 0 ELSE @SellReturn END,
                TotalReturnPct = CASE WHEN @SellQty = 0 THEN NULL ELSE @SellReturnPct END,
                IsActive       = CASE WHEN @SellQty = 0 THEN 0 ELSE 1 END,
                UpdatedAt      = SYSUTCDATETIME()
            WHERE Id = @HoldingId;

            UPDATE Investment.Transactions
            SET CostBasis = @DisposedCostBasis, RealizedGain = @RealizedGain
            WHERE Id = @NewTransactionId;

            -- Credit source account
            IF @SourceAccount IS NOT NULL
                EXEC Core.sp_UpdateAccountBalance @AccountId = @SourceAccount, @DeltaAmount = @TotalAmount;
        END
        ELSE IF @TransactionType = N'Dividend'
        BEGIN
            -- Add dividend to holding's dividend received total
            UPDATE Investment.Holdings
            SET DividendReceived = DividendReceived + @TotalAmount,
                TotalReturn      = ISNULL(CurrentValue, 0) - InvestedAmount + DividendReceived + @TotalAmount,
                UpdatedAt        = SYSUTCDATETIME()
            WHERE Id = @HoldingId;

            -- Credit source account (dividend payout)
            IF @SourceAccount IS NOT NULL
                EXEC Core.sp_UpdateAccountBalance @AccountId = @SourceAccount, @DeltaAmount = @TotalAmount;
        END

        -- Audit log
        DECLARE @UserId BIGINT;
        SELECT @UserId = p.UserId
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        WHERE h.Id = @HoldingId;

        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'InvestmentTransaction', CAST(@NewTransactionId AS NVARCHAR(256)),
            N'{"Type":"' + @TransactionType + N'","Quantity":' + CAST(@Quantity AS NVARCHAR(50)) +
            N',"PricePerUnit":' + CAST(@PricePerUnit AS NVARCHAR(50)) +
            N',"TotalAmount":' + CAST(@TotalAmount AS NVARCHAR(50)) + N'}'
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
-- SP: Investment.sp_ProcessSIPInstallments
-- Description: Process all due SIPs; create holdings/transactions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_ProcessSIPInstallments', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_ProcessSIPInstallments;
GO

CREATE PROCEDURE Investment.sp_ProcessSIPInstallments
    @AsOfDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @AsOfDate IS NULL
            SET @AsOfDate = CAST(SYSUTCDATETIME() AS DATE);

        -- Process each due SIP
        DECLARE @SIPId             BIGINT;
        DECLARE @UserId            BIGINT;
        DECLARE @HoldingId         BIGINT;
        DECLARE @Amount            DECIMAL(18,2);
        DECLARE @Frequency         NVARCHAR(20);
        DECLARE @DayOfMonth        INT;
        DECLARE @StartDate         DATE;
        DECLARE @EndDate           DATE;
        DECLARE @SourceAccountId   BIGINT;
        DECLARE @NextExecutionDate DATE;
        DECLARE @InstallmentsDone  INT;

        DECLARE sip_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT
                Id, UserId, HoldingId, Amount, Frequency, DayOfMonth,
                StartDate, EndDate, SourceAccountId, NextExecutionDate, InstallmentsDone
            FROM Investment.SIPs
            WHERE IsActive = 1
              AND NextExecutionDate <= @AsOfDate
              AND (EndDate IS NULL OR EndDate >= @AsOfDate);

        OPEN sip_cursor;
        FETCH NEXT FROM sip_cursor INTO
            @SIPId, @UserId, @HoldingId, @Amount, @Frequency, @DayOfMonth,
            @StartDate, @EndDate, @SourceAccountId, @NextExecutionDate, @InstallmentsDone;

        DECLARE @ProcessedCount INT = 0;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- If holding already exists, buy more units
            IF @HoldingId IS NOT NULL
            BEGIN
                DECLARE @CurrentPrice DECIMAL(18,4);
                SELECT @CurrentPrice = ISNULL(CurrentPrice, AvgPurchasePrice)
                FROM Investment.Holdings WHERE Id = @HoldingId;

                DECLARE @UnitsBought DECIMAL(18,4) = @Amount / NULLIF(@CurrentPrice, 0);
                DECLARE @NewTxnId BIGINT;
                DECLARE @QtyToPass DECIMAL(18,4) = ISNULL(@UnitsBought, 0);
                DECLARE @PriceToPass DECIMAL(18,4) = ISNULL(@CurrentPrice, 0);

                EXEC Investment.sp_RecordInvestmentTransaction
                    @HoldingId        = @HoldingId,
                    @TransactionType  = N'SIP',
                    @Quantity         = @QtyToPass,
                    @PricePerUnit     = @PriceToPass,
                    @TotalAmount      = @Amount,
                    @TransactionDate  = @NextExecutionDate,
                    @SIPId            = @SIPId,
                    @SourceAccount    = @SourceAccountId,
                    @NewTransactionId = @NewTxnId OUTPUT;
            END
            ELSE
            BEGIN
                -- First SIP installment - need to create the holding
                -- Use a placeholder price (NAV will be updated later)
                -- The holding will be linked back to the SIP
                DECLARE @PortfolioId BIGINT;
                SELECT @PortfolioId = Id FROM Investment.Portfolios
                WHERE UserId = @UserId AND IsDefault = 1 AND DeletedAt IS NULL;

                IF @PortfolioId IS NULL
                BEGIN
                    -- Create default portfolio if none exists
                    INSERT INTO Investment.Portfolios (UserId, Name, IsDefault)
                    VALUES (@UserId, N'Default', 1);

                    SET @PortfolioId = SCOPE_IDENTITY();
                END

                -- Get investment type for the SIP (default to MutualFund)
                DECLARE @InvTypeId INT;
                SELECT @InvTypeId = Id FROM Investment.InvestmentTypes WHERE Name = N'MutualFund';
                SET @InvTypeId = ISNULL(@InvTypeId, 1);

                DECLARE @NewHoldingId BIGINT;
                DECLARE @SymbolText NVARCHAR(50) = N'SIP-' + CAST(@SIPId AS NVARCHAR(20));
                DECLARE @NameText NVARCHAR(300) = N'SIP Investment #' + CAST(@SIPId AS NVARCHAR(20));
                EXEC Investment.sp_CreateHolding
                    @PortfolioId      = @PortfolioId,
                    @InvestmentTypeId = @InvTypeId,
                    @Symbol           = @SymbolText,
                    @Name             = @NameText,
                    @Quantity         = 0,   -- Will be updated by the transaction
                    @AvgPurchasePrice = 1,   -- Placeholder
                    @InvestedAmount   = 0,
                    @NewHoldingId     = @NewHoldingId OUTPUT;

                -- Link holding to SIP
                UPDATE Investment.SIPs
                SET HoldingId = @NewHoldingId
                WHERE Id = @SIPId;

                SET @HoldingId = @NewHoldingId;

                -- Now record the SIP buy transaction
                DECLARE @NewTxnId2 BIGINT;
                EXEC Investment.sp_RecordInvestmentTransaction
                    @HoldingId        = @HoldingId,
                    @TransactionType  = N'SIP',
                    @Quantity         = @Amount,  -- Units at price 1 (placeholder)
                    @PricePerUnit     = 1,
                    @TotalAmount      = @Amount,
                    @TransactionDate  = @NextExecutionDate,
                    @SIPId            = @SIPId,
                    @SourceAccount    = @SourceAccountId,
                    @NewTransactionId = @NewTxnId2 OUTPUT;
            END

            -- Calculate next execution date
            DECLARE @CalculatedNext DATE;
            IF @Frequency = N'Monthly'
            BEGIN
                DECLARE @TryDay INT = ISNULL(@DayOfMonth, DAY(@NextExecutionDate));
                DECLARE @NextMonth DATE = DATEADD(MONTH, 1, @NextExecutionDate);
                DECLARE @LastDay INT = DAY(EOMONTH(@NextMonth));
                IF @TryDay > @LastDay SET @TryDay = @LastDay;
                SET @CalculatedNext = DATEFROMPARTS(YEAR(@NextMonth), MONTH(@NextMonth), @TryDay);
            END
            ELSE IF @Frequency = N'Weekly'
                SET @CalculatedNext = DATEADD(WEEK, 1, @NextExecutionDate);
            ELSE IF @Frequency = N'Quarterly'
                SET @CalculatedNext = DATEADD(QUARTER, 1, @NextExecutionDate);
            ELSE
                SET @CalculatedNext = DATEADD(MONTH, 1, @NextExecutionDate);

            -- Update SIP
            UPDATE Investment.SIPs
            SET
                LastExecutedDate   = @NextExecutionDate,
                NextExecutionDate  = @CalculatedNext,
                InstallmentsDone   = InstallmentsDone + 1,
                TotalInvested      = TotalInvested + @Amount,
                IsActive           = CASE
                                        WHEN @EndDate IS NOT NULL AND @CalculatedNext > @EndDate
                                        THEN 0
                                        ELSE 1
                                     END,
                UpdatedAt          = SYSUTCDATETIME()
            WHERE Id = @SIPId;

            SET @ProcessedCount = @ProcessedCount + 1;

            FETCH NEXT FROM sip_cursor INTO
                @SIPId, @UserId, @HoldingId, @Amount, @Frequency, @DayOfMonth,
                @StartDate, @EndDate, @SourceAccountId, @NextExecutionDate, @InstallmentsDone;
        END

        CLOSE sip_cursor;
        DEALLOCATE sip_cursor;

        -- Return result
        SELECT @ProcessedCount AS ProcessedSIPs, @AsOfDate AS ProcessedAsOfDate;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS(N'local', N'sip_cursor') >= 0
        BEGIN
            CLOSE sip_cursor;
            DEALLOCATE sip_cursor;
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
-- SP: Investment.sp_UpdateEPFContribution
-- Description: Add a monthly EPF entry and update the EPF account balance
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_UpdateEPFContribution', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_UpdateEPFContribution;
GO

CREATE PROCEDURE Investment.sp_UpdateEPFContribution
    @EPFAccountId            BIGINT,
    @Month                   DATE,              -- First day of the month
    @EmployeeContribution    DECIMAL(18,2),
    @EmployerContribution    DECIMAL(18,2),
    @EPSContribution         DECIMAL(18,2)   = 0
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate EPF account
        IF NOT EXISTS (SELECT 1 FROM Investment.EPFAccounts WHERE Id = @EPFAccountId AND IsActive = 1)
        BEGIN
            RAISERROR('EPF Account with Id %d does not exist or is inactive.', 16, 1, @EPFAccountId);
            RETURN;
        END

        -- Check for duplicate entry for the same month
        IF EXISTS (
            SELECT 1 FROM Investment.EPFContributions
            WHERE EPFAccountId = @EPFAccountId AND Month = @Month
        )
        BEGIN
            RAISERROR('EPF contribution for this month already exists.', 16, 1);
            RETURN;
        END

        -- Get current balance and interest rate
        DECLARE @CurrentBalance DECIMAL(18,2);
        DECLARE @InterestRate   DECIMAL(8,4);
        DECLARE @UserId         BIGINT;

        SELECT
            @CurrentBalance = CurrentBalance,
            @InterestRate   = InterestRate,
            @UserId         = UserId
        FROM Investment.EPFAccounts
        WHERE Id = @EPFAccountId;

        -- Calculate interest for the month (annual rate / 12)
        DECLARE @MonthlyInterestRate DECIMAL(10,6) = @InterestRate / 1200.0;
        DECLARE @InterestEarned DECIMAL(18,2) = ROUND(@CurrentBalance * @MonthlyInterestRate, 2);

        -- Calculate opening and closing balances
        DECLARE @OpeningBalance DECIMAL(18,2) = @CurrentBalance;
        DECLARE @ClosingBalance DECIMAL(18,2) = @OpeningBalance
            + @EmployeeContribution
            + @EmployerContribution
            + @InterestEarned;

        -- Insert contribution record
        INSERT INTO Investment.EPFContributions
        (
            EPFAccountId, Month, EmployeeContribution, EmployerContribution,
            EPSContribution, InterestEarned, OpeningBalance, ClosingBalance
        )
        VALUES
        (
            @EPFAccountId, @Month, @EmployeeContribution, @EmployerContribution,
            @EPSContribution, @InterestEarned, @OpeningBalance, @ClosingBalance
        );

        -- Update EPF account balance
        UPDATE Investment.EPFAccounts
        SET
            CurrentBalance = @ClosingBalance,
            EPSCorpus      = EPSCorpus + @EPSContribution,
            UpdatedAt      = SYSUTCDATETIME()
        WHERE Id = @EPFAccountId;

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'EPFContribution', CAST(@EPFAccountId AS NVARCHAR(256)),
            N'{"Month":"' + CONVERT(NVARCHAR(10), @Month, 23) +
            N'","EmployeeContribution":' + CAST(@EmployeeContribution AS NVARCHAR(50)) +
            N'","EmployerContribution":' + CAST(@EmployerContribution AS NVARCHAR(50)) +
            N'","InterestEarned":' + CAST(@InterestEarned AS NVARCHAR(50)) +
            N'","ClosingBalance":' + CAST(@ClosingBalance AS NVARCHAR(50)) + N'}'
        );

        -- Return the contribution details
        SELECT
            @OpeningBalance       AS OpeningBalance,
            @EmployeeContribution AS EmployeeContribution,
            @EmployerContribution AS EmployerContribution,
            @EPSContribution      AS EPSContribution,
            @InterestEarned       AS InterestEarned,
            @ClosingBalance       AS ClosingBalance;
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
-- SP: Investment.sp_CalculateXIRR
-- Description: Calculate XIRR for a holding using Newton-Raphson method
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_CalculateXIRR', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_CalculateXIRR;
GO

CREATE PROCEDURE Investment.sp_CalculateXIRR
    @HoldingId   BIGINT,
    @MaxIter     INT = 100,
    @Tolerance   DECIMAL(18,10) = 0.0000000001
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Collect all cash flows: Buys are negative (outflow), Sells/Dividends are positive (inflow)
        -- Plus the current value as a positive terminal cash flow
        CREATE TABLE #CashFlows
        (
            FlowDate DATE NOT NULL,
            Amount   DECIMAL(18,2) NOT NULL  -- Negative for outflow, positive for inflow
        );

        -- Investment transactions (Buy/SIP = outflow, Sell/Dividend = inflow)
        INSERT INTO #CashFlows (FlowDate, Amount)
        SELECT
            TransactionDate,
            CASE
                WHEN TransactionType IN (N'Buy', N'SIP')   THEN -TotalAmount
                WHEN TransactionType IN (N'Sell', N'Dividend') THEN TotalAmount
                ELSE 0
            END
        FROM Investment.Transactions
        WHERE HoldingId = @HoldingId
          AND TransactionType IN (N'Buy', N'SIP', N'Sell', N'Dividend');

        -- Add current value as terminal inflow
        DECLARE @CurrentValue DECIMAL(18,2);
        DECLARE @HoldingExists BIT = 0;

        SELECT @CurrentValue = ISNULL(CurrentValue, 0), @HoldingExists = 1
        FROM Investment.Holdings WHERE Id = @HoldingId AND DeletedAt IS NULL;

        IF @HoldingExists = 0
        BEGIN
            RAISERROR('Holding with Id %d does not exist.', 16, 1, @HoldingId);
            RETURN;
        END

        IF @CurrentValue > 0
        BEGIN
            INSERT INTO #CashFlows (FlowDate, Amount)
            VALUES (CAST(SYSUTCDATETIME() AS DATE), @CurrentValue);
        END

        -- Need at least 2 cash flows
        DECLARE @FlowCount INT;
        SELECT @FlowCount = COUNT(*) FROM #CashFlows WHERE Amount <> 0;

        IF @FlowCount < 2
        BEGIN
            -- Cannot compute XIRR with less than 2 cash flows
            SELECT NULL AS XIRR, NULL AS XIRRPct, 0 AS Iterations;
            RETURN;
        END

        -- Newton-Raphson method to find XIRR
        -- XIRR is the rate r such that sum(Amount_i / (1+r)^((Date_i - Date_0)/365)) = 0
        DECLARE @BaseDate DATE;
        SELECT @BaseDate = MIN(FlowDate) FROM #CashFlows;

        -- Create table with day offsets
        CREATE TABLE #NormalizedFlows
        (
            DayOffset DECIMAL(18,6),
            Amount    DECIMAL(18,2)
        );

        INSERT INTO #NormalizedFlows (DayOffset, Amount)
        SELECT DATEDIFF(DAY, @BaseDate, FlowDate) * 1.0, Amount
        FROM #CashFlows
        WHERE Amount <> 0;

        -- Initial guess: 10% (0.10)
        DECLARE @Rate DECIMAL(18,10) = 0.10;
        DECLARE @Iter INT = 0;
        DECLARE @NPV  DECIMAL(18,6);
        DECLARE @Deriv DECIMAL(18,6);
        DECLARE @Converged BIT = 0;

        WHILE @Iter < @MaxIter AND @Converged = 0
        BEGIN
            -- Calculate NPV and its derivative
            SELECT
                @NPV   = SUM(Amount / POWER(1.0 + @Rate, DayOffset / 365.0)),
                @Deriv = SUM(-DayOffset / 365.0 * Amount / POWER(1.0 + @Rate, DayOffset / 365.0 + 1.0))
            FROM #NormalizedFlows;

            IF ABS(@Deriv) < 0.0000000001
            BEGIN
                -- Derivative too small, avoid division by zero
                BREAK;
            END

            -- Newton-Raphson update
            DECLARE @NewRate DECIMAL(18,10) = @Rate - @NPV / @Deriv;

            -- Check convergence
            IF ABS(@NewRate - @Rate) < @Tolerance
            BEGIN
                SET @Converged = 1;
                SET @Rate = @NewRate;
            END
            ELSE
            BEGIN
                SET @Rate = @NewRate;
            END

            SET @Iter = @Iter + 1;
        END

        -- Convert to annual percentage
        DECLARE @XIRR    DECIMAL(8,4) = NULL;
        DECLARE @XIRRPct DECIMAL(8,4) = NULL;

        IF @Converged = 1 AND @Rate > -1.0  -- Rate must be > -100%
        BEGIN
            SET @XIRR    = ROUND(@Rate * 100, 4);
            SET @XIRRPct = @XIRR;

            -- Update the holding's XIRR
            UPDATE Investment.Holdings
            SET XIRR     = @XIRR,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @HoldingId;
        END

        -- Return result
        SELECT @XIRR AS XIRR, @XIRRPct AS XIRRPct, @Iter AS Iterations, @Converged AS Converged;

        DROP TABLE #CashFlows;
        DROP TABLE #NormalizedFlows;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#CashFlows', N'U') IS NOT NULL
            DROP TABLE #CashFlows;
        IF OBJECT_ID(N'tempdb..#NormalizedFlows', N'U') IS NOT NULL
            DROP TABLE #NormalizedFlows;

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ---------------------------------------------------------------------------
-- SP: Investment.sp_GetPortfolioSummary
-- Description: Asset allocation, total invested, current value, returns by type
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_GetPortfolioSummary', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_GetPortfolioSummary;
GO

CREATE PROCEDURE Investment.sp_GetPortfolioSummary
    @UserId      BIGINT,
    @PortfolioId BIGINT = NULL   -- NULL = all portfolios for user
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

        -- Result Set 1: Overall portfolio summary
        SELECT
            COUNT(DISTINCT h.Id)                        AS TotalHoldings,
            ISNULL(SUM(h.InvestedAmount), 0)             AS TotalInvested,
            ISNULL(SUM(h.CurrentValue), 0)                AS CurrentValue,
            ISNULL(SUM(h.TotalReturn), 0)                 AS TotalReturn,
            CASE
                WHEN ISNULL(SUM(h.InvestedAmount), 0) > 0
                THEN ROUND((ISNULL(SUM(h.TotalReturn), 0) * 100.0) / SUM(h.InvestedAmount), 2)
                ELSE 0
            END                                          AS OverallReturnPct,
            ISNULL(SUM(h.DayChange), 0)                  AS DayChange,
            ISNULL(SUM(h.DividendReceived), 0)           AS TotalDividends
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        WHERE p.UserId     = @UserId
          AND p.DeletedAt  IS NULL
          AND h.DeletedAt  IS NULL
          AND h.IsActive   = 1
          AND (@PortfolioId IS NULL OR h.PortfolioId = @PortfolioId);

        -- Result Set 2: Asset allocation by investment type
        SELECT
            it.Id                    AS InvestmentTypeId,
            it.Name                  AS InvestmentTypeName,
            it.AssetClass,
            COUNT(h.Id)              AS HoldingCount,
            ISNULL(SUM(h.InvestedAmount), 0) AS TotalInvested,
            ISNULL(SUM(h.CurrentValue), 0)   AS CurrentValue,
            ISNULL(SUM(h.TotalReturn), 0)    AS TotalReturn,
            CASE
                WHEN SUM(h.InvestedAmount) > 0
                THEN ROUND((SUM(h.TotalReturn) * 100.0) / SUM(h.InvestedAmount), 2)
                ELSE 0
            END                       AS ReturnPct,
            CASE
                WHEN ISNULL(SUM(h.CurrentValue), 0) > 0
                THEN ROUND((SUM(h.CurrentValue) * 100.0) / (
                    SELECT ISNULL(SUM(h2.CurrentValue), 1)
                    FROM Investment.Holdings h2
                    INNER JOIN Investment.Portfolios p2 ON h2.PortfolioId = p2.Id
                    WHERE p2.UserId = @UserId AND p2.DeletedAt IS NULL AND h2.DeletedAt IS NULL AND h2.IsActive = 1
                      AND (@PortfolioId IS NULL OR h2.PortfolioId = @PortfolioId)
                ), 2)
                ELSE 0
            END                       AS AllocationPct
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        INNER JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
        WHERE p.UserId     = @UserId
          AND p.DeletedAt  IS NULL
          AND h.DeletedAt  IS NULL
          AND h.IsActive   = 1
          AND (@PortfolioId IS NULL OR h.PortfolioId = @PortfolioId)
        GROUP BY it.Id, it.Name, it.AssetClass
        ORDER BY CurrentValue DESC;

        -- Result Set 3: Top holdings by value
        SELECT TOP 10
            h.Id,
            h.Symbol,
            h.Name,
            it.Name              AS InvestmentType,
            h.Quantity,
            h.AvgPurchasePrice,
            h.CurrentPrice,
            h.CurrentValue,
            h.InvestedAmount,
            h.TotalReturn,
            h.TotalReturnPct,
            h.XIRR,
            h.DayChange,
            h.DayChangePct
        FROM Investment.Holdings h
        INNER JOIN Investment.Portfolios p ON h.PortfolioId = p.Id
        INNER JOIN Investment.InvestmentTypes it ON h.InvestmentTypeId = it.Id
        WHERE p.UserId     = @UserId
          AND p.DeletedAt  IS NULL
          AND h.DeletedAt  IS NULL
          AND h.IsActive   = 1
          AND (@PortfolioId IS NULL OR h.PortfolioId = @PortfolioId)
        ORDER BY h.CurrentValue DESC;
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
-- SP: Investment.sp_GetEPFProjection
-- Description: Project EPF corpus to retirement based on current salary &
--              contribution rates
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.sp_GetEPFProjection', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_GetEPFProjection;
GO

CREATE PROCEDURE Investment.sp_GetEPFProjection
    @EPFAccountId     BIGINT,
    @RetirementAge    INT    = 60,
    @SalaryGrowthPct  DECIMAL(5,2) = 8.00,     -- Expected annual salary hike %
    @InterestRateOverride DECIMAL(8,4) = NULL   -- Override current EPF interest rate
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get EPF account details
        DECLARE @UserId                  BIGINT;
        DECLARE @CurrentBalance          DECIMAL(18,2);
        DECLARE @MonthlySalary           DECIMAL(18,2);
        DECLARE @EmployeeContributionPct DECIMAL(5,2);
        DECLARE @EmployerContributionPct DECIMAL(5,2);
        DECLARE @InterestRate            DECIMAL(8,4);
        DECLARE @StartDate               DATE;

        SELECT
            @UserId                  = UserId,
            @CurrentBalance          = CurrentBalance,
            @MonthlySalary           = ISNULL(MonthlySalary, 50000),
            @EmployeeContributionPct = EmployeeContributionPct,
            @EmployerContributionPct = EmployerContributionPct,
            @InterestRate            = InterestRate,
            @StartDate               = StartDate
        FROM Investment.EPFAccounts
        WHERE Id = @EPFAccountId AND IsActive = 1;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('EPF Account with Id %d does not exist or is inactive.', 16, 1, @EPFAccountId);
            RETURN;
        END

        -- Apply interest rate override if specified
        IF @InterestRateOverride IS NOT NULL
            SET @InterestRate = @InterestRateOverride;

        -- Calculate years to retirement (assume user is 25 at EPF start if no DOB)
        DECLARE @YearsSinceStart INT = DATEDIFF(YEAR, @StartDate, CAST(SYSUTCDATETIME() AS DATE));
        DECLARE @AssumedCurrentAge INT = 25 + @YearsSinceStart;
        DECLARE @YearsToRetirement INT = @RetirementAge - @AssumedCurrentAge;

        IF @YearsToRetirement <= 0
        BEGIN
            -- Already at/past retirement
            SELECT
                @CurrentBalance      AS ProjectedCorpus,
                0                    AS YearsRemaining,
                0                    AS MonthsRemaining,
                0                    AS TotalEmployeeContribution,
                0                    AS TotalEmployerContribution,
                0                    AS TotalInterestEarned;
            RETURN;
        END

        -- Project year by year
        CREATE TABLE #YearlyProjection
        (
            YearNum            INT,
            Year               INT,
            OpeningBalance     DECIMAL(18,2),
            EmployeeYearly     DECIMAL(18,2),
            EmployerYearly     DECIMAL(18,2),
            InterestEarned     DECIMAL(18,2),
            ClosingBalance     DECIMAL(18,2),
            MonthlySalary      DECIMAL(18,2)
        );

        DECLARE @YearNum            INT = 1;
        DECLARE @ProjectedBalance   DECIMAL(18,2) = @CurrentBalance;
        DECLARE @ProjectedSalary    DECIMAL(18,2) = @MonthlySalary;
        DECLARE @TotalEmployeeYtd   DECIMAL(18,2) = 0;
        DECLARE @TotalEmployerYtd   DECIMAL(18,2) = 0;
        DECLARE @TotalInterestYtd   DECIMAL(18,2) = 0;
        DECLARE @CurrentYear        INT = YEAR(SYSUTCDATETIME());

        WHILE @YearNum <= @YearsToRetirement
        BEGIN
            DECLARE @OpeningBal DECIMAL(18,2) = @ProjectedBalance;
            DECLARE @EmpYearly DECIMAL(18,2)  = ROUND(@ProjectedSalary * @EmployeeContributionPct / 100.0 * 12, 2);
            DECLARE @ErYearly  DECIMAL(18,2)  = ROUND(@ProjectedSalary * @EmployerContributionPct / 100.0 * 12, 2);
            DECLARE @YearlyContribution DECIMAL(18,2) = @EmpYearly + @ErYearly;

            -- Interest calculated on opening balance + average contributions for the year
            -- Simplification: interest on (opening + contributions/2)
            DECLARE @Interest DECIMAL(18,2) = ROUND((@OpeningBal + @YearlyContribution / 2.0) * @InterestRate / 100.0, 2);
            DECLARE @ClosingBal DECIMAL(18,2) = @OpeningBal + @YearlyContribution + @Interest;

            INSERT INTO #YearlyProjection
            (YearNum, Year, OpeningBalance, EmployeeYearly, EmployerYearly, InterestEarned, ClosingBalance, MonthlySalary)
            VALUES
            (@YearNum, @CurrentYear + @YearNum - 1, @OpeningBal, @EmpYearly, @ErYearly, @Interest, @ClosingBal, @ProjectedSalary);

            SET @ProjectedBalance  = @ClosingBal;
            SET @TotalEmployeeYtd  = @TotalEmployeeYtd + @EmpYearly;
            SET @TotalEmployerYtd  = @TotalEmployerYtd + @ErYearly;
            SET @TotalInterestYtd  = @TotalInterestYtd + @Interest;

            -- Salary growth for next year
            SET @ProjectedSalary = ROUND(@ProjectedSalary * (1 + @SalaryGrowthPct / 100.0), 2);

            SET @YearNum = @YearNum + 1;
        END

        -- Return summary
        SELECT
            @ProjectedBalance   AS ProjectedCorpus,
            @YearsToRetirement  AS YearsRemaining,
            @YearsToRetirement * 12 AS MonthsRemaining,
            @TotalEmployeeYtd   AS TotalEmployeeContribution,
            @TotalEmployerYtd   AS TotalEmployerContribution,
            @TotalInterestYtd   AS TotalInterestEarned,
            @InterestRate       AS AssumedInterestRate,
            @SalaryGrowthPct    AS AssumedSalaryGrowthPct,
            @ProjectedSalary    AS FinalYearSalary;

        -- Return yearly projection
        SELECT
            YearNum,
            Year,
            OpeningBalance,
            EmployeeYearly,
            EmployerYearly,
            InterestEarned,
            ClosingBalance,
            MonthlySalary
        FROM #YearlyProjection
        ORDER BY YearNum;

        DROP TABLE #YearlyProjection;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#YearlyProjection', N'U') IS NOT NULL
            DROP TABLE #YearlyProjection;

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

PRINT 'Investment stored procedures created successfully.';
GO

IF OBJECT_ID(N'Investment.sp_CapturePortfolioValueSnapshots', N'P') IS NOT NULL
    DROP PROCEDURE Investment.sp_CapturePortfolioValueSnapshots;
GO
CREATE PROCEDURE Investment.sp_CapturePortfolioValueSnapshots
    @UserId BIGINT = NULL,
    @SnapshotDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @SnapshotDate = ISNULL(@SnapshotDate, CAST(SYSUTCDATETIME() AS DATE));

    MERGE Investment.PortfolioValueSnapshots AS target
    USING
    (
        SELECT p.Id PortfolioId, @SnapshotDate SnapshotDate,
               ISNULL(SUM(CASE WHEN h.DeletedAt IS NULL THEN h.InvestedAmount ELSE 0 END),0) InvestedValue,
               ISNULL(SUM(CASE WHEN h.DeletedAt IS NULL THEN h.CurrentValue ELSE 0 END),0) CurrentValue
        FROM Investment.Portfolios p
        LEFT JOIN Investment.Holdings h ON h.PortfolioId=p.Id
        WHERE p.DeletedAt IS NULL AND (@UserId IS NULL OR p.UserId=@UserId)
        GROUP BY p.Id
    ) source
    ON target.PortfolioId=source.PortfolioId AND target.SnapshotDate=source.SnapshotDate
    WHEN MATCHED THEN UPDATE SET
        InvestedValue=source.InvestedValue,
        CurrentValue=source.CurrentValue,
        UnrealizedGain=source.CurrentValue-source.InvestedValue,
        CreatedAt=SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (PortfolioId, SnapshotDate, InvestedValue, CurrentValue, UnrealizedGain)
        VALUES (source.PortfolioId, source.SnapshotDate, source.InvestedValue, source.CurrentValue,
                source.CurrentValue-source.InvestedValue);
END
GO



-- FinOS SIP/EPF tracker extensions
IF COL_LENGTH(N'Investment.SIPs', N'SIPName') IS NULL
    ALTER TABLE Investment.SIPs ADD SIPName NVARCHAR(300) NULL;
GO

CREATE OR ALTER PROCEDURE Investment.sp_CreateSIP
 @UserId BIGINT,@SIPName NVARCHAR(300),@HoldingId BIGINT=NULL,@Amount DECIMAL(18,2),@Frequency NVARCHAR(20),
 @DayOfMonth INT,@StartDate DATE,@EndDate DATE=NULL,@SourceAccountId BIGINT,@Id BIGINT OUTPUT
AS
BEGIN
 SET NOCOUNT ON;
 IF @Amount<=0 OR @DayOfMonth NOT BETWEEN 1 AND 31 THROW 50001,'Invalid SIP amount or debit day.',1;
 IF NOT EXISTS(SELECT 1 FROM Core.Accounts WHERE Id=@SourceAccountId AND UserId=@UserId AND DeletedAt IS NULL) THROW 50002,'Source account is invalid.',1;
 IF @HoldingId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM Investment.Holdings h JOIN Investment.Portfolios p ON p.Id=h.PortfolioId WHERE h.Id=@HoldingId AND p.UserId=@UserId AND h.DeletedAt IS NULL) THROW 50003,'Holding is invalid.',1;
 DECLARE @LastDay INT=DAY(EOMONTH(@StartDate));
 DECLARE @First DATE=DATEFROMPARTS(YEAR(@StartDate),MONTH(@StartDate),IIF(@DayOfMonth>@LastDay,@LastDay,@DayOfMonth));
 IF @First<@StartDate SET @First=DATEADD(MONTH,1,@First);
 INSERT Investment.SIPs(UserId,HoldingId,SIPName,Amount,Frequency,DayOfMonth,StartDate,EndDate,NextExecutionDate,SourceAccountId)
 VALUES(@UserId,@HoldingId,@SIPName,@Amount,@Frequency,@DayOfMonth,@StartDate,@EndDate,@First,@SourceAccountId);
 SET @Id=SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE Investment.sp_UpdateSIP
 @Id BIGINT,@UserId BIGINT,@SIPName NVARCHAR(300),@HoldingId BIGINT=NULL,@Amount DECIMAL(18,2),@Frequency NVARCHAR(20),
 @DayOfMonth INT,@StartDate DATE,@EndDate DATE=NULL,@SourceAccountId BIGINT
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM Investment.SIPs WHERE Id=@Id AND UserId=@UserId) THROW 50004,'SIP not found.',1;
 IF NOT EXISTS(SELECT 1 FROM Core.Accounts WHERE Id=@SourceAccountId AND UserId=@UserId AND DeletedAt IS NULL) THROW 50002,'Source account is invalid.',1;
 UPDATE Investment.SIPs SET SIPName=@SIPName,HoldingId=@HoldingId,Amount=@Amount,Frequency=@Frequency,DayOfMonth=@DayOfMonth,
 StartDate=@StartDate,EndDate=@EndDate,SourceAccountId=@SourceAccountId,UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND UserId=@UserId;
END;
GO

CREATE OR ALTER PROCEDURE Investment.sp_SetSIPStatus @Id BIGINT,@UserId BIGINT,@IsActive BIT
AS
BEGIN
 SET NOCOUNT ON;
 UPDATE Investment.SIPs SET IsActive=@IsActive,UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND UserId=@UserId;
 IF @@ROWCOUNT=0 THROW 50004,'SIP not found.',1;
END;
GO

CREATE OR ALTER PROCEDURE Investment.sp_DeleteSIP @Id BIGINT,@UserId BIGINT
AS
BEGIN
 SET NOCOUNT ON;
 IF NOT EXISTS(SELECT 1 FROM Investment.SIPs WHERE Id=@Id AND UserId=@UserId) THROW 50004,'SIP not found.',1;
 IF EXISTS(SELECT 1 FROM Investment.SIPs WHERE Id=@Id AND InstallmentsDone>0)
  UPDATE Investment.SIPs SET IsActive=0,EndDate=COALESCE(EndDate,CAST(SYSUTCDATETIME() AS DATE)),UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id;
 ELSE DELETE Investment.SIPs WHERE Id=@Id;
END;
GO

CREATE OR ALTER PROCEDURE Investment.sp_CreateEPFAccount
 @UserId BIGINT,@UAN NVARCHAR(20)=NULL,@EstablishmentCode NVARCHAR(20)=NULL,@EmployerName NVARCHAR(300)=NULL,
 @EmployeeContributionPct DECIMAL(5,2),@EmployerContributionPct DECIMAL(5,2),@MonthlySalary DECIMAL(18,2),
 @CurrentBalance DECIMAL(18,2),@InterestRate DECIMAL(8,4),@StartDate DATE,@Id BIGINT OUTPUT
AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS(SELECT 1 FROM Investment.EPFAccounts WHERE UserId=@UserId AND IsActive=1) THROW 50005,'An active EPF account already exists.',1;
 INSERT Investment.EPFAccounts(UserId,UAN,EstablishmentCode,EmployerName,EmployeeContributionPct,EmployerContributionPct,MonthlySalary,CurrentBalance,InterestRate,StartDate)
 VALUES(@UserId,@UAN,@EstablishmentCode,@EmployerName,@EmployeeContributionPct,@EmployerContributionPct,@MonthlySalary,@CurrentBalance,@InterestRate,@StartDate);
 SET @Id=SCOPE_IDENTITY();
END;
GO

CREATE OR ALTER PROCEDURE Investment.sp_AddEPFContribution
 @EPFAccountId BIGINT,@UserId BIGINT,@Month DATE,@MonthlySalary DECIMAL(18,2)
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @Month=DATEFROMPARTS(YEAR(@Month),MONTH(@Month),1);
 IF NOT EXISTS(SELECT 1 FROM Investment.EPFAccounts WHERE Id=@EPFAccountId AND UserId=@UserId AND IsActive=1) THROW 50006,'EPF account not found.',1;
 IF EXISTS(SELECT 1 FROM Investment.EPFContributions WHERE EPFAccountId=@EPFAccountId AND Month=@Month) THROW 50007,'Contribution already exists for this month.',1;
 DECLARE @EmployeePct DECIMAL(5,2),@EmployerPct DECIMAL(5,2),@Rate DECIMAL(8,4),@Opening DECIMAL(18,2);
 SELECT @EmployeePct=EmployeeContributionPct,@EmployerPct=EmployerContributionPct,@Rate=InterestRate,@Opening=CurrentBalance FROM Investment.EPFAccounts WHERE Id=@EPFAccountId;
 DECLARE @Employee DECIMAL(18,2)=ROUND(@MonthlySalary*@EmployeePct/100,2);
 DECLARE @Employer DECIMAL(18,2)=ROUND(@MonthlySalary*@EmployerPct/100,2);
 DECLARE @EPS DECIMAL(18,2)=ROUND(IIF(@MonthlySalary*8.33/100>1250,1250,@MonthlySalary*8.33/100),2);
 DECLARE @Interest DECIMAL(18,2)=ROUND((@Opening+@Employee+@Employer-@EPS)*@Rate/1200,2);
 DECLARE @Closing DECIMAL(18,2)=@Opening+@Employee+@Employer-@EPS+@Interest;
 INSERT Investment.EPFContributions(EPFAccountId,Month,EmployeeContribution,EmployerContribution,EPSContribution,InterestEarned,OpeningBalance,ClosingBalance)
 VALUES(@EPFAccountId,@Month,@Employee,@Employer,@EPS,@Interest,@Opening,@Closing);
 DECLARE @ContributionId BIGINT=SCOPE_IDENTITY();
 UPDATE Investment.EPFAccounts SET CurrentBalance=@Closing,EPSCorpus=EPSCorpus+@EPS,MonthlySalary=@MonthlySalary,UpdatedAt=SYSUTCDATETIME() WHERE Id=@EPFAccountId;
 SELECT Id,Month,EmployeeContribution,EmployerContribution,EPSContribution,InterestEarned,OpeningBalance,ClosingBalance FROM Investment.EPFContributions WHERE Id=@ContributionId;
END;
GO

