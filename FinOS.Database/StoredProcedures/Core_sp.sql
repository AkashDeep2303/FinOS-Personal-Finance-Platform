-- ============================================================================
-- FinOS Database - Core Finance Stored Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for transactions, accounts, recurring schedules,
--              and subscription detection
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- SP: Core.sp_CreateAccount
-- Description: Insert a new account for a user
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_CreateAccount', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_CreateAccount;
GO

CREATE PROCEDURE Core.sp_CreateAccount
    @UserId                BIGINT,
    @AccountTypeId         INT,
    @Name                  NVARCHAR(100),
    @InstitutionName       NVARCHAR(200)   = NULL,
    @AccountNumber         NVARCHAR(50)    = NULL,
    @Balance               DECIMAL(18,2)   = 0,
    @CreditLimit           DECIMAL(18,2)   = NULL,
    @Currency              NVARCHAR(3)     = N'INR',
    @Color                 NVARCHAR(7)     = NULL,
    @Icon                  NVARCHAR(50)    = NULL,
    @IsIncludedInNetWorth  BIT             = 1,
    @Notes                 NVARCHAR(500)   = NULL,
    @NewAccountId          BIGINT          OUTPUT
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

        -- Validate account type
        IF NOT EXISTS (SELECT 1 FROM Core.AccountTypes WHERE Id = @AccountTypeId)
        BEGIN
            RAISERROR('AccountType with Id %d does not exist.', 16, 1, @AccountTypeId);
            RETURN;
        END

        INSERT INTO Core.Accounts
        (
            UserId, AccountTypeId, Name, InstitutionName, AccountNumber,
            Balance, CreditLimit, Currency, Color, Icon,
            IsIncludedInNetWorth, Notes
        )
        VALUES
        (
            @UserId, @AccountTypeId, @Name, @InstitutionName, @AccountNumber,
            @Balance, @CreditLimit, @Currency, @Color, @Icon,
            @IsIncludedInNetWorth, @Notes
        );

        SET @NewAccountId = SCOPE_IDENTITY();

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'Account', CAST(@NewAccountId AS NVARCHAR(256)),
            N'{"Name":"' + @Name + N'","Balance":' + CAST(@Balance AS NVARCHAR(50)) + N'}'
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
-- SP: Core.sp_UpdateAccountBalance
-- Description: Adjust account balance by a delta amount
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_UpdateAccountBalance', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_UpdateAccountBalance;
GO

CREATE PROCEDURE Core.sp_UpdateAccountBalance
    @AccountId     BIGINT,
    @DeltaAmount   DECIMAL(18,2)   -- Positive to add, negative to subtract
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Core.Accounts WHERE Id = @AccountId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Account with Id %d does not exist or is deleted.', 16, 1, @AccountId);
            RETURN;
        END

        UPDATE Core.Accounts
        SET Balance   = Balance + @DeltaAmount,
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id      = @AccountId
          AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Failed to update account balance.', 16, 1);
            RETURN;
        END
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
-- SP: Core.sp_CreateTransaction
-- Description: Insert a transaction, update account balance, and check budget alert
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_CreateTransaction', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_CreateTransaction;
GO

CREATE PROCEDURE Core.sp_CreateTransaction
    @UserId              BIGINT,
    @AccountId           BIGINT,
    @CategoryId          BIGINT           = NULL,
    @TransferAccountId   BIGINT           = NULL,
    @Type                NVARCHAR(20),       -- Income, Expense, Transfer
    @Amount              DECIMAL(18,2),
    @Currency            NVARCHAR(3)      = N'INR',
    @ExchangeRate        DECIMAL(18,6)    = NULL,
    @OriginalAmount      DECIMAL(18,2)    = NULL,
    @OriginalCurrency    NVARCHAR(3)      = NULL,
    @Description         NVARCHAR(500),
    @Notes               NVARCHAR(1000)   = NULL,
    @TransactionDate     DATE,
    @TransactionTime     TIME             = NULL,
    @ValueDate           DATE             = NULL,
    @ReferenceNumber     NVARCHAR(100)    = NULL,
    @MerchantName        NVARCHAR(200)    = NULL,
    @MerchantCategory    NVARCHAR(100)    = NULL,
    @IsRecurring         BIT              = 0,
    @RecurringScheduleId BIGINT           = NULL,
    @IsFlagged           BIT              = 0,
    @AttachmentUrls      NVARCHAR(MAX)    = NULL,
    @LocationLat         DECIMAL(10,7)    = NULL,
    @LocationLng         DECIMAL(10,7)    = NULL,
    @LocationName        NVARCHAR(200)    = NULL,
    @Source              NVARCHAR(50)     = N'Manual',
    @ImportBatchId       UNIQUEIDENTIFIER = NULL,
    @NewTransactionId    BIGINT           OUTPUT
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

        -- Validate account belongs to user
        IF NOT EXISTS (
            SELECT 1 FROM Core.Accounts
            WHERE Id = @AccountId AND UserId = @UserId AND DeletedAt IS NULL
        )
        BEGIN
            RAISERROR('Account with Id %d does not belong to user or does not exist.', 16, 1, @AccountId);
            RETURN;
        END

        -- Validate type
        IF @Type NOT IN (N'Income', N'Expense', N'Transfer')
        BEGIN
            RAISERROR('Transaction Type must be Income, Expense, or Transfer.', 16, 1);
            RETURN;
        END

        -- Validate amount
        IF @Amount <= 0
        BEGIN
            RAISERROR('Transaction amount must be greater than zero.', 16, 1);
            RETURN;
        END

        -- For transfer, validate destination account
        IF @Type = N'Transfer'
        BEGIN
            IF @TransferAccountId IS NULL
            BEGIN
                RAISERROR('TransferAccountId is required for Transfer transactions.', 16, 1);
                RETURN;
            END

            IF NOT EXISTS (
                SELECT 1 FROM Core.Accounts
                WHERE Id = @TransferAccountId AND UserId = @UserId AND DeletedAt IS NULL
            )
            BEGIN
                RAISERROR('Transfer destination account does not exist or does not belong to user.', 16, 1);
                RETURN;
            END
        END

        -- Insert the transaction
        INSERT INTO Core.Transactions
        (
            UserId, AccountId, CategoryId, TransferAccountId, Type, Amount,
            Currency, ExchangeRate, OriginalAmount, OriginalCurrency,
            Description, Notes, TransactionDate, TransactionTime, ValueDate,
            ReferenceNumber, MerchantName, MerchantCategory, IsRecurring,
            RecurringScheduleId, IsFlagged, AttachmentUrls,
            LocationLat, LocationLng, LocationName, Source, ImportBatchId
        )
        VALUES
        (
            @UserId, @AccountId, @CategoryId, @TransferAccountId, @Type, @Amount,
            @Currency, @ExchangeRate, @OriginalAmount, @OriginalCurrency,
            @Description, @Notes, @TransactionDate, @TransactionTime, @ValueDate,
            @ReferenceNumber, @MerchantName, @MerchantCategory, @IsRecurring,
            @RecurringScheduleId, @IsFlagged, @AttachmentUrls,
            @LocationLat, @LocationLng, @LocationName, @Source, @ImportBatchId
        );

        SET @NewTransactionId = SCOPE_IDENTITY();

        -- Update account balance based on transaction type
        DECLARE @Delta DECIMAL(18,2);
        IF @Type = N'Income'
        BEGIN
            SET @Delta = @Amount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @AccountId, @DeltaAmount = @Delta;
        END
        ELSE IF @Type = N'Expense'
        BEGIN
            SET @Delta = -@Amount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @AccountId, @DeltaAmount = @Delta;
        END
        ELSE IF @Type = N'Transfer'
        BEGIN
            -- Debit source account, credit destination account
            SET @Delta = -@Amount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @AccountId, @DeltaAmount = @Delta;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @TransferAccountId, @DeltaAmount = @Amount;
        END

        -- Check budget alerts if this is an expense
        IF @Type = N'Expense' AND @CategoryId IS NOT NULL
        BEGIN
            BEGIN TRY
                EXEC Budget.sp_CheckBudgetAlerts
                    @UserId     = @UserId,
                    @CategoryId = @CategoryId,
                    @Amount     = @Amount;
            END TRY
            BEGIN CATCH
                -- Budget alert check failure should not fail the transaction
                -- Log and continue
                DECLARE @BudgetAlertError NVARCHAR(4000) = ERROR_MESSAGE();
                INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, NewValues)
                VALUES (
                    @UserId,
                    N'ERROR',
                    N'BudgetAlertCheck',
                    N'{"Error":"' + REPLACE(@BudgetAlertError, '"', '"') + N'"}'
                );
            END CATCH
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'Transaction', CAST(@NewTransactionId AS NVARCHAR(256)),
            N'{"Type":"' + @Type + N'","Amount":' + CAST(@Amount AS NVARCHAR(50)) +
            N',"Description":"' + REPLACE(@Description, '"', '"') + N'"}'
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
-- SP: Core.sp_UpdateTransaction
-- Description: Update a transaction and recalculate account balance
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_UpdateTransaction', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_UpdateTransaction;
GO

CREATE PROCEDURE Core.sp_UpdateTransaction
    @TransactionId       BIGINT,
    @UserId              BIGINT,
    @AccountId           BIGINT           = NULL,
    @CategoryId          BIGINT           = NULL,
    @TransferAccountId   BIGINT           = NULL,
    @Type                NVARCHAR(20)     = NULL,
    @Amount              DECIMAL(18,2)    = NULL,
    @Description         NVARCHAR(500)    = NULL,
    @Notes               NVARCHAR(1000)   = NULL,
    @TransactionDate     DATE             = NULL,
    @TransactionTime     TIME             = NULL,
    @MerchantName        NVARCHAR(200)    = NULL,
    @MerchantCategory    NVARCHAR(100)    = NULL,
    @IsFlagged           BIT              = NULL,
    @ReferenceNumber     NVARCHAR(100)    = NULL,
    @AttachmentUrls      NVARCHAR(MAX)    = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get current transaction
        DECLARE @CurrentAccountId         BIGINT;
        DECLARE @CurrentTransferAccountId BIGINT;
        DECLARE @CurrentType              NVARCHAR(20);
        DECLARE @CurrentAmount            DECIMAL(18,2);

        SELECT
            @CurrentAccountId         = AccountId,
            @CurrentTransferAccountId = TransferAccountId,
            @CurrentType              = Type,
            @CurrentAmount            = Amount
        FROM Core.Transactions
        WHERE Id     = @TransactionId
          AND UserId = @UserId
          AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Transaction with Id %d not found or access denied.', 16, 1, @TransactionId);
            RETURN;
        END

        -- Resolve effective values (use provided or keep current)
        DECLARE @EffectiveAccountId         BIGINT       = ISNULL(@AccountId, @CurrentAccountId);
        DECLARE @EffectiveTransferAccountId BIGINT       = ISNULL(@TransferAccountId, @CurrentTransferAccountId);
        DECLARE @EffectiveType              NVARCHAR(20) = ISNULL(@Type, @CurrentType);
        DECLARE @EffectiveAmount            DECIMAL(18,2)= ISNULL(@Amount, @CurrentAmount);

        -- Step 1: Reverse the old transaction's effect on account balance
        DECLARE @ReverseDelta DECIMAL(18,2);
        IF @CurrentType = N'Income'
        BEGIN
            SET @ReverseDelta = -@CurrentAmount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @CurrentAccountId, @DeltaAmount = @ReverseDelta;
        END
        ELSE IF @CurrentType = N'Expense'
        BEGIN
            SET @ReverseDelta = @CurrentAmount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @CurrentAccountId, @DeltaAmount = @ReverseDelta;
        END
        ELSE IF @CurrentType = N'Transfer'
        BEGIN
            SET @ReverseDelta = @CurrentAmount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @CurrentAccountId, @DeltaAmount = @ReverseDelta;
            IF @CurrentTransferAccountId IS NOT NULL
            BEGIN
                SET @ReverseDelta = -@CurrentAmount;
                EXEC Core.sp_UpdateAccountBalance @AccountId = @CurrentTransferAccountId, @DeltaAmount = @ReverseDelta;
            END
        END

        -- Step 2: Update the transaction record
        UPDATE Core.Transactions
        SET
            AccountId         = @EffectiveAccountId,
            CategoryId        = @CategoryId,                  -- Allow NULL
            TransferAccountId = @EffectiveTransferAccountId,
            Type              = @EffectiveType,
            Amount            = @EffectiveAmount,
            Description       = ISNULL(@Description, Description),
            Notes             = @Notes,                       -- Allow NULL
            TransactionDate   = ISNULL(@TransactionDate, TransactionDate),
            TransactionTime   = @TransactionTime,             -- Allow NULL
            MerchantName      = @MerchantName,                -- Allow NULL
            MerchantCategory  = @MerchantCategory,            -- Allow NULL
            IsFlagged         = ISNULL(@IsFlagged, IsFlagged),
            ReferenceNumber   = @ReferenceNumber,             -- Allow NULL
            AttachmentUrls    = @AttachmentUrls,               -- Allow NULL
            UpdatedAt         = SYSUTCDATETIME()
        WHERE Id     = @TransactionId
          AND UserId = @UserId;

        -- Step 3: Apply the new transaction's effect on account balance
        DECLARE @ApplyDelta DECIMAL(18,2);
        IF @EffectiveType = N'Income'
        BEGIN
            SET @ApplyDelta = @EffectiveAmount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @EffectiveAccountId, @DeltaAmount = @ApplyDelta;
        END
        ELSE IF @EffectiveType = N'Expense'
        BEGIN
            SET @ApplyDelta = -@EffectiveAmount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @EffectiveAccountId, @DeltaAmount = @ApplyDelta;
        END
        ELSE IF @EffectiveType = N'Transfer'
        BEGIN
            SET @ApplyDelta = -@EffectiveAmount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @EffectiveAccountId, @DeltaAmount = @ApplyDelta;
            IF @EffectiveTransferAccountId IS NOT NULL
                EXEC Core.sp_UpdateAccountBalance @AccountId = @EffectiveTransferAccountId, @DeltaAmount = @EffectiveAmount;
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId)
        VALUES (@UserId, N'UPDATE', N'Transaction', CAST(@TransactionId AS NVARCHAR(256)));
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
-- SP: Core.sp_DeleteTransaction
-- Description: Soft delete a transaction and revert account balance
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_DeleteTransaction', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_DeleteTransaction;
GO

CREATE PROCEDURE Core.sp_DeleteTransaction
    @TransactionId  BIGINT,
    @UserId         BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get current transaction details
        DECLARE @AccountId         BIGINT;
        DECLARE @TransferAccountId BIGINT;
        DECLARE @Type              NVARCHAR(20);
        DECLARE @Amount            DECIMAL(18,2);

        SELECT
            @AccountId         = AccountId,
            @TransferAccountId = TransferAccountId,
            @Type              = Type,
            @Amount            = Amount
        FROM Core.Transactions
        WHERE Id     = @TransactionId
          AND UserId = @UserId
          AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Transaction with Id %d not found or already deleted.', 16, 1, @TransactionId);
            RETURN;
        END

        -- Revert the account balance
        DECLARE @RevertDelta DECIMAL(18,2);
        IF @Type = N'Income'
        BEGIN
            SET @RevertDelta = -@Amount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @AccountId, @DeltaAmount = @RevertDelta;
        END
        ELSE IF @Type = N'Expense'
        BEGIN
            SET @RevertDelta = @Amount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @AccountId, @DeltaAmount = @RevertDelta;
        END
        ELSE IF @Type = N'Transfer'
        BEGIN
            SET @RevertDelta = @Amount;
            EXEC Core.sp_UpdateAccountBalance @AccountId = @AccountId, @DeltaAmount = @RevertDelta;
            IF @TransferAccountId IS NOT NULL
            BEGIN
                SET @RevertDelta = -@Amount;
                EXEC Core.sp_UpdateAccountBalance @AccountId = @TransferAccountId, @DeltaAmount = @RevertDelta;
            END
        END

        -- Soft delete the transaction
        UPDATE Core.Transactions
        SET DeletedAt = SYSUTCDATETIME(),
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id     = @TransactionId
          AND UserId = @UserId;

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId)
        VALUES (@UserId, N'DELETE', N'Transaction', CAST(@TransactionId AS NVARCHAR(256)));
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
-- SP: Core.sp_SplitTransaction
-- Description: Create child (split) transactions from a parent transaction
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_SplitTransaction', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_SplitTransaction;
GO

CREATE PROCEDURE Core.sp_SplitTransaction
    @ParentTransactionId  BIGINT,
    @UserId               BIGINT,
    @Splits               NVARCHAR(MAX)  -- JSON array: [{"CategoryId":..,"Amount":..,"Description":"..","Notes":".."}]
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate parent transaction
        DECLARE @AccountId       BIGINT;
        DECLARE @Type            NVARCHAR(20);
        DECLARE @ParentAmount    DECIMAL(18,2);
        DECLARE @TransactionDate DATE;
        DECLARE @Currency        NVARCHAR(3);
        DECLARE @Source          NVARCHAR(50);

        SELECT
            @AccountId       = AccountId,
            @Type            = Type,
            @ParentAmount    = Amount,
            @TransactionDate = TransactionDate,
            @Currency        = Currency,
            @Source          = Source
        FROM Core.Transactions
        WHERE Id     = @ParentTransactionId
          AND UserId = @UserId
          AND DeletedAt IS NULL
          AND ParentTransactionId IS NULL;  -- Cannot split a child

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Parent transaction not found, already split, or access denied.', 16, 1);
            RETURN;
        END

        -- Parse splits JSON and validate total
        DECLARE @SplitTotal DECIMAL(18,2) = 0;
        DECLARE @SplitCount INT = 0;

        -- Use OPENJSON to parse splits
        CREATE TABLE #SplitData
        (
            RowId       INT IDENTITY(1,1),
            CategoryId  BIGINT,
            Amount      DECIMAL(18,2),
            Description NVARCHAR(500),
            Notes       NVARCHAR(1000)
        );

        INSERT INTO #SplitData (CategoryId, Amount, Description, Notes)
        SELECT
            [CategoryId],
            [Amount],
            [Description],
            [Notes]
        FROM OPENJSON(@Splits)
        WITH (
            CategoryId  BIGINT         N'$.CategoryId',
            Amount      DECIMAL(18,2)  N'$.Amount',
            Description NVARCHAR(500)  N'$.Description',
            Notes       NVARCHAR(1000) N'$.Notes'
        );

        SELECT @SplitTotal = SUM(Amount), @SplitCount = COUNT(*)
        FROM #SplitData;

        -- Validate split total matches parent amount
        IF ABS(@SplitTotal - @ParentAmount) > 0.01
        BEGIN
            DECLARE @SplitTotalText NVARCHAR(50) = CAST(@SplitTotal AS NVARCHAR(50));
            DECLARE @ParentAmountText NVARCHAR(50) = CAST(@ParentAmount AS NVARCHAR(50));
            RAISERROR('Split total (%s) must equal parent transaction amount (%s).', 16, 1,
                @SplitTotalText, @ParentAmountText);
            RETURN;
        END

        IF @SplitCount < 2
        BEGIN
            RAISERROR('At least 2 splits are required.', 16, 1);
            RETURN;
        END

        -- Mark parent as split
        UPDATE Core.Transactions
        SET IsSplit  = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @ParentTransactionId;

        -- Create child transactions
        DECLARE @RowId       INT = 1;
        DECLARE @CatId       BIGINT;
        DECLARE @SplitAmt    DECIMAL(18,2);
        DECLARE @SplitDesc   NVARCHAR(500);
        DECLARE @SplitNotes  NVARCHAR(1000);
        DECLARE @ChildId     BIGINT;

        WHILE @RowId <= @SplitCount
        BEGIN
            SELECT @CatId = CategoryId, @SplitAmt = Amount, @SplitDesc = Description, @SplitNotes = Notes
            FROM #SplitData
            WHERE RowId = @RowId;

            INSERT INTO Core.Transactions
            (
                UserId, AccountId, CategoryId, Type, Amount, Currency,
                Description, Notes, TransactionDate, ParentTransactionId,
                IsSplit, SplitNote, Source
            )
            VALUES
            (
                @UserId, @AccountId, @CatId, @Type, @SplitAmt, @Currency,
                @SplitDesc, @SplitNotes, @TransactionDate, @ParentTransactionId,
                0, N'Split from parent #' + CAST(@ParentTransactionId AS NVARCHAR(20)), @Source
            );

            SET @RowId = @RowId + 1;
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'SPLIT', N'Transaction', CAST(@ParentTransactionId AS NVARCHAR(256)),
            N'{"SplitCount":' + CAST(@SplitCount AS NVARCHAR(10)) +
            N',"SplitTotal":' + CAST(@SplitTotal AS NVARCHAR(50)) + N'}'
        );

        DROP TABLE #SplitData;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#SplitData', N'U') IS NOT NULL
            DROP TABLE #SplitData;

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ---------------------------------------------------------------------------
-- SP: Core.sp_GetTransactionsByDateRange
-- Description: Paginated transaction listing with filters
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_GetTransactionsByDateRange', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_GetTransactionsByDateRange;
GO

CREATE PROCEDURE Core.sp_GetTransactionsByDateRange
    @UserId         BIGINT,
    @StartDate      DATE             = NULL,
    @EndDate        DATE             = NULL,
    @Type           NVARCHAR(20)     = NULL,      -- Income, Expense, Transfer
    @CategoryId     BIGINT           = NULL,
    @AccountId      BIGINT           = NULL,
    @SearchTerm     NVARCHAR(500)    = NULL,      -- Searches Description, MerchantName, Notes
    @PageNumber     INT              = 1,
    @PageSize       INT              = 20
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @PageNumber < 1 SET @PageNumber = 1;
        IF @PageSize < 1 OR @PageSize > 100 SET @PageSize = 20;

        DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

        -- Main result set
        SELECT
            t.Id,
            t.AccountId,
            a.Name                  AS AccountName,
            t.CategoryId,
            c.Name                  AS CategoryName,
            c.Icon                  AS CategoryIcon,
            t.TransferAccountId,
            ta.Name                 AS TransferAccountName,
            t.Type,
            t.Amount,
            t.Currency,
            t.OriginalAmount,
            t.OriginalCurrency,
            t.Description,
            t.Notes,
            t.TransactionDate,
            t.TransactionTime,
            t.MerchantName,
            t.MerchantCategory,
            t.IsRecurring,
            t.IsFlagged,
            t.IsSplit,
            t.ParentTransactionId,
            t.ReferenceNumber,
            t.Source,
            t.IsVerified,
            t.CreatedAt,
            t.UpdatedAt
        FROM Core.Transactions t
        INNER JOIN Core.Accounts a ON t.AccountId = a.Id
        LEFT JOIN Core.Categories c ON t.CategoryId = c.Id
        LEFT JOIN Core.Accounts ta ON t.TransferAccountId = ta.Id
        WHERE t.UserId     = @UserId
          AND t.DeletedAt  IS NULL
          AND t.ParentTransactionId IS NULL   -- Hide split children from main list
          AND (@StartDate IS NULL OR t.TransactionDate >= @StartDate)
          AND (@EndDate   IS NULL OR t.TransactionDate <= @EndDate)
          AND (@Type      IS NULL OR t.Type = @Type)
          AND (@CategoryId IS NULL OR t.CategoryId = @CategoryId)
          AND (@AccountId  IS NULL OR t.AccountId = @AccountId)
          AND (@SearchTerm IS NULL
               OR t.Description    LIKE N'%' + @SearchTerm + N'%'
               OR t.MerchantName  LIKE N'%' + @SearchTerm + N'%'
               OR t.Notes         LIKE N'%' + @SearchTerm + N'%'
               OR t.ReferenceNumber LIKE N'%' + @SearchTerm + N'%')
        ORDER BY t.TransactionDate DESC, t.CreatedAt DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

        -- Total count for pagination
        SELECT COUNT(*) AS TotalCount
        FROM Core.Transactions t
        WHERE t.UserId     = @UserId
          AND t.DeletedAt  IS NULL
          AND t.ParentTransactionId IS NULL
          AND (@StartDate IS NULL OR t.TransactionDate >= @StartDate)
          AND (@EndDate   IS NULL OR t.TransactionDate <= @EndDate)
          AND (@Type      IS NULL OR t.Type = @Type)
          AND (@CategoryId IS NULL OR t.CategoryId = @CategoryId)
          AND (@AccountId  IS NULL OR t.AccountId = @AccountId)
          AND (@SearchTerm IS NULL
               OR t.Description    LIKE N'%' + @SearchTerm + N'%'
               OR t.MerchantName  LIKE N'%' + @SearchTerm + N'%'
               OR t.Notes         LIKE N'%' + @SearchTerm + N'%'
               OR t.ReferenceNumber LIKE N'%' + @SearchTerm + N'%');
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
-- SP: Core.sp_GetMonthlySummary
-- Description: Aggregate income/expense by category for a given month
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_GetMonthlySummary', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_GetMonthlySummary;
GO

CREATE PROCEDURE Core.sp_GetMonthlySummary
    @UserId    BIGINT,
    @Year      INT,
    @Month     INT         -- 1-12
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @Month < 1 OR @Month > 12
        BEGIN
            RAISERROR('Month must be between 1 and 12.', 16, 1);
            RETURN;
        END

        DECLARE @StartDate DATE = DATEFROMPARTS(@Year, @Month, 1);
        DECLARE @EndDate   DATE = EOMONTH(@StartDate);

        -- Category-wise breakdown
        SELECT
            c.Id                    AS CategoryId,
            ISNULL(c.Name, N'Uncategorized') AS CategoryName,
            c.Icon                  AS CategoryIcon,
            c.Type                  AS CategoryType,
            t.Type                  AS TransactionType,
            COUNT(*)                AS TransactionCount,
            SUM(t.Amount)           AS TotalAmount
        FROM Core.Transactions t
        LEFT JOIN Core.Categories c ON t.CategoryId = c.Id
        WHERE t.UserId          = @UserId
          AND t.DeletedAt       IS NULL
          AND t.TransactionDate BETWEEN @StartDate AND @EndDate
          AND t.Type            IN (N'Income', N'Expense')
          AND t.ParentTransactionId IS NULL
        GROUP BY c.Id, c.Name, c.Icon, c.Type, t.Type
        ORDER BY t.Type, TotalAmount DESC;

        -- Overall summary
        SELECT
            ISNULL(SUM(CASE WHEN Type = N'Income'  THEN Amount ELSE 0 END), 0) AS TotalIncome,
            ISNULL(SUM(CASE WHEN Type = N'Expense' THEN Amount ELSE 0 END), 0) AS TotalExpense,
            ISNULL(SUM(CASE WHEN Type = N'Income'  THEN Amount ELSE 0 END), 0)
                - ISNULL(SUM(CASE WHEN Type = N'Expense' THEN Amount ELSE 0 END), 0) AS NetSavings,
            COUNT(*) AS TotalTransactions
        FROM Core.Transactions
        WHERE UserId          = @UserId
          AND DeletedAt       IS NULL
          AND TransactionDate BETWEEN @StartDate AND @EndDate
          AND Type            IN (N'Income', N'Expense')
          AND ParentTransactionId IS NULL;

        -- Top 5 merchants
        SELECT TOP 5
            MerchantName,
            COUNT(*)    AS TransactionCount,
            SUM(Amount) AS TotalSpent
        FROM Core.Transactions
        WHERE UserId          = @UserId
          AND DeletedAt       IS NULL
          AND TransactionDate BETWEEN @StartDate AND @EndDate
          AND Type            = N'Expense'
          AND MerchantName    IS NOT NULL
          AND ParentTransactionId IS NULL
        GROUP BY MerchantName
        ORDER BY TotalSpent DESC;
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
-- SP: Core.sp_CreateRecurringSchedule
-- Description: Insert a new recurring schedule
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_CreateRecurringSchedule', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_CreateRecurringSchedule;
GO

CREATE PROCEDURE Core.sp_CreateRecurringSchedule
    @UserId              BIGINT,
    @AccountId           BIGINT,
    @CategoryId          BIGINT           = NULL,
    @Type                NVARCHAR(20),       -- Income, Expense, Transfer
    @Amount              DECIMAL(18,2),
    @Description         NVARCHAR(500),
    @Frequency           NVARCHAR(20),       -- Daily, Weekly, BiWeekly, Monthly, Quarterly, Yearly
    @IntervalValue       INT              = 1,
    @DayOfMonth          INT              = NULL,
    @DayOfWeek           INT              = NULL,
    @StartDate           DATE,
    @EndDate             DATE             = NULL,
    @AutoCreate          BIT              = 0,
    @NewScheduleId       BIGINT           OUTPUT
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

        -- Validate account
        IF NOT EXISTS (SELECT 1 FROM Core.Accounts WHERE Id = @AccountId AND UserId = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Account does not exist or does not belong to user.', 16, 1);
            RETURN;
        END

        -- Validate frequency
        IF @Frequency NOT IN (N'Daily', N'Weekly', N'BiWeekly', N'Monthly', N'Quarterly', N'Yearly')
        BEGIN
            RAISERROR('Invalid frequency. Must be Daily, Weekly, BiWeekly, Monthly, Quarterly, or Yearly.', 16, 1);
            RETURN;
        END

        -- Validate amount
        IF @Amount <= 0
        BEGIN
            RAISERROR('Amount must be greater than zero.', 16, 1);
            RETURN;
        END

        -- Compute NextOccurrenceDate based on frequency and start date
        DECLARE @NextOccurrenceDate DATE = @StartDate;

        INSERT INTO Core.RecurringSchedules
        (
            UserId, AccountId, CategoryId, Type, Amount, Description,
            Frequency, IntervalValue, DayOfMonth, DayOfWeek,
            StartDate, EndDate, NextOccurrenceDate, AutoCreate
        )
        VALUES
        (
            @UserId, @AccountId, @CategoryId, @Type, @Amount, @Description,
            @Frequency, @IntervalValue, @DayOfMonth, @DayOfWeek,
            @StartDate, @EndDate, @NextOccurrenceDate, @AutoCreate
        );

        SET @NewScheduleId = SCOPE_IDENTITY();

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'RecurringSchedule', CAST(@NewScheduleId AS NVARCHAR(256)),
            N'{"Frequency":"' + @Frequency + N'","Amount":' + CAST(@Amount AS NVARCHAR(50)) + N'}'
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
-- SP: Core.sp_ProcessRecurringTransactions
-- Description: Process all due recurring schedules and create transactions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_ProcessRecurringTransactions', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_ProcessRecurringTransactions;
GO

CREATE PROCEDURE Core.sp_ProcessRecurringTransactions
    @AsOfDate DATE = NULL   -- Defaults to today; allows backfill/testing
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @AsOfDate IS NULL
            SET @AsOfDate = CAST(SYSUTCDATETIME() AS DATE);

        -- Process each due schedule
        DECLARE @ScheduleId    BIGINT;
        DECLARE @UserId        BIGINT;
        DECLARE @AccountId     BIGINT;
        DECLARE @CategoryId    BIGINT;
        DECLARE @Type          NVARCHAR(20);
        DECLARE @Amount        DECIMAL(18,2);
        DECLARE @Description   NVARCHAR(500);
        DECLARE @Frequency     NVARCHAR(20);
        DECLARE @IntervalValue INT;
        DECLARE @DayOfMonth    INT;
        DECLARE @DayOfWeek     INT;
        DECLARE @EndDate       DATE;
        DECLARE @AutoCreate    BIT;
        DECLARE @NextDate      DATE;
        DECLARE @NewTxnId      BIGINT;

        DECLARE schedule_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT
                Id, UserId, AccountId, CategoryId, Type, Amount,
                Description, Frequency, IntervalValue, DayOfMonth, DayOfWeek,
                EndDate, AutoCreate, NextOccurrenceDate
            FROM Core.RecurringSchedules
            WHERE IsActive = 1
              AND NextOccurrenceDate <= @AsOfDate
              AND (EndDate IS NULL OR EndDate >= @AsOfDate);

        OPEN schedule_cursor;
        FETCH NEXT FROM schedule_cursor INTO
            @ScheduleId, @UserId, @AccountId, @CategoryId, @Type, @Amount,
            @Description, @Frequency, @IntervalValue, @DayOfMonth, @DayOfWeek,
            @EndDate, @AutoCreate, @NextDate;

        DECLARE @ProcessedCount INT = 0;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Only auto-create if configured; otherwise just update schedule
            IF @AutoCreate = 1
            BEGIN
                -- Create the transaction
                EXEC Core.sp_CreateTransaction
                    @UserId              = @UserId,
                    @AccountId           = @AccountId,
                    @CategoryId          = @CategoryId,
                    @Type                = @Type,
                    @Amount              = @Amount,
                    @Description         = @Description,
                    @TransactionDate     = @NextDate,
                    @IsRecurring         = 1,
                    @RecurringScheduleId = @ScheduleId,
                    @Source              = N'Recurring',
                    @NewTransactionId    = @NewTxnId OUTPUT;
            END

            -- Calculate next occurrence date
            DECLARE @CalculatedNext DATE;

            IF @Frequency = N'Daily'
                SET @CalculatedNext = DATEADD(DAY, @IntervalValue, @NextDate);
            ELSE IF @Frequency = N'Weekly'
                SET @CalculatedNext = DATEADD(WEEK, @IntervalValue, @NextDate);
            ELSE IF @Frequency = N'BiWeekly'
                SET @CalculatedNext = DATEADD(WEEK, 2 * @IntervalValue, @NextDate);
            ELSE IF @Frequency = N'Monthly'
            BEGIN
                -- Try same day of month; fall back to last day of month
                DECLARE @TryDay INT = ISNULL(@DayOfMonth, DAY(@NextDate));
                DECLARE @NextMonth DATE = DATEADD(MONTH, @IntervalValue, @NextDate);
                DECLARE @LastDayOfNextMonth INT = DAY(EOMONTH(@NextMonth));

                IF @TryDay > @LastDayOfNextMonth
                    SET @TryDay = @LastDayOfNextMonth;

                SET @CalculatedNext = DATEFROMPARTS(YEAR(@NextMonth), MONTH(@NextMonth), @TryDay);
            END
            ELSE IF @Frequency = N'Quarterly'
                SET @CalculatedNext = DATEADD(QUARTER, @IntervalValue, @NextDate);
            ELSE IF @Frequency = N'Yearly'
                SET @CalculatedNext = DATEADD(YEAR, @IntervalValue, @NextDate);
            ELSE
                SET @CalculatedNext = DATEADD(MONTH, 1, @NextDate);

            -- Update schedule
            UPDATE Core.RecurringSchedules
            SET LastProcessedDate   = @NextDate,
                NextOccurrenceDate  = @CalculatedNext,
                IsActive            = CASE
                                        WHEN @EndDate IS NOT NULL AND @CalculatedNext > @EndDate
                                        THEN 0
                                        ELSE 1
                                     END,
                UpdatedAt           = SYSUTCDATETIME()
            WHERE Id = @ScheduleId;

            SET @ProcessedCount = @ProcessedCount + 1;

            FETCH NEXT FROM schedule_cursor INTO
                @ScheduleId, @UserId, @AccountId, @CategoryId, @Type, @Amount,
                @Description, @Frequency, @IntervalValue, @DayOfMonth, @DayOfWeek,
                @EndDate, @AutoCreate, @NextDate;
        END

        CLOSE schedule_cursor;
        DEALLOCATE schedule_cursor;

        -- Return result
        SELECT @ProcessedCount AS ProcessedSchedules, @AsOfDate AS ProcessedAsOfDate;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS(N'local', N'schedule_cursor') >= 0
        BEGIN
            CLOSE schedule_cursor;
            DEALLOCATE schedule_cursor;
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
-- SP: Core.sp_DetectSubscriptions
-- Description: Analyze transactions to find recurring merchants via frequency
--              analysis and populate Subscriptions.DetectedSubscriptions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.sp_DetectSubscriptions', N'P') IS NOT NULL
    DROP PROCEDURE Core.sp_DetectSubscriptions;
GO

CREATE PROCEDURE Core.sp_DetectSubscriptions
    @UserId        BIGINT,
    @MinOccurrences INT = 3   -- Minimum times a merchant must appear to be considered
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @MinOccurrences < 2
        BEGIN
            RAISERROR('MinOccurrences must be at least 2.', 16, 1);
            RETURN;
        END

        -- Look at last 12 months of expense transactions
        DECLARE @LookbackDate DATE = DATEADD(MONTH, -12, CAST(SYSUTCDATETIME() AS DATE));

        -- Find merchants with recurring patterns
        -- Group by merchant name, look for regular intervals and similar amounts
        CREATE TABLE #CandidateSubscriptions
        (
            MerchantName        NVARCHAR(200),
            CategoryId          BIGINT,
            AvgAmount           DECIMAL(18,2),
            MinAmount           DECIMAL(18,2),
            MaxAmount           DECIMAL(18,2),
            TransactionCount    INT,
            FirstTransaction    DATE,
            LastTransaction     DATE,
            LastTransactionId   BIGINT,
            AvgDaysBetween      DECIMAL(10,2),
            Frequency           NVARCHAR(20),
            Confidence          DECIMAL(5,2)
        );

        -- Step 1: Find merchants with >= MinOccurrences expense transactions
        INSERT INTO #CandidateSubscriptions (MerchantName, CategoryId, AvgAmount, MinAmount, MaxAmount,
            TransactionCount, FirstTransaction, LastTransaction, LastTransactionId, AvgDaysBetween)
        SELECT
            t.MerchantName,
            t.CategoryId,
            AVG(t.Amount),
            MIN(t.Amount),
            MAX(t.Amount),
            COUNT(*),
            MIN(t.TransactionDate),
            MAX(t.TransactionDate),
            -- Last transaction id for this merchant (latest by date)
            (SELECT TOP 1 t2.Id FROM Core.Transactions t2 WHERE t2.UserId = @UserId AND t2.MerchantName = t.MerchantName AND t2.DeletedAt IS NULL AND t2.ParentTransactionId IS NULL ORDER BY t2.TransactionDate DESC, t2.CreatedAt DESC) AS LastTransactionId,
            -- Calculate average days between consecutive transactions
            CASE WHEN COUNT(*) > 1
                 THEN DATEDIFF(DAY, MIN(t.TransactionDate), MAX(t.TransactionDate)) * 1.0 / (COUNT(*) - 1)
                 ELSE 0
            END
        FROM Core.Transactions t
        WHERE t.UserId       = @UserId
          AND t.DeletedAt    IS NULL
          AND t.Type         = N'Expense'
          AND t.MerchantName IS NOT NULL
          AND t.TransactionDate >= @LookbackDate
          AND t.ParentTransactionId IS NULL
        GROUP BY t.MerchantName, t.CategoryId
        HAVING COUNT(*) >= @MinOccurrences;

        -- Step 2: Classify frequency and compute confidence
        UPDATE #CandidateSubscriptions
        SET
            Frequency = CASE
                WHEN AvgDaysBetween BETWEEN 25 AND 35  THEN N'Monthly'
                WHEN AvgDaysBetween BETWEEN 6 AND 8    THEN N'Weekly'
                WHEN AvgDaysBetween BETWEEN 12 AND 16   THEN N'BiWeekly'
                WHEN AvgDaysBetween BETWEEN 85 AND 95   THEN N'Quarterly'
                WHEN AvgDaysBetween BETWEEN 350 AND 380 THEN N'Yearly'
                ELSE N'Unknown'
            END,
            Confidence = CASE
                WHEN Frequency = N'Unknown' THEN 30.0
                WHEN (MaxAmount - MinAmount) / NULLIF(AvgAmount, 0) < 0.1 THEN 90.0   -- Very consistent amounts
                WHEN (MaxAmount - MinAmount) / NULLIF(AvgAmount, 0) < 0.25 THEN 75.0   -- Somewhat consistent
                ELSE 55.0   -- Amount varies significantly
            END;

        -- Filter out unknown frequency with low confidence
        DELETE FROM #CandidateSubscriptions
        WHERE Frequency = N'Unknown' AND Confidence < 50;

        -- Step 3: Upsert into DetectedSubscriptions
        DECLARE @Merchant   NVARCHAR(200);
        DECLARE @CatId      BIGINT;
        DECLARE @Avg        DECIMAL(18,2);
        DECLARE @Freq       NVARCHAR(20);
        DECLARE @Conf       DECIMAL(5,2);
        DECLARE @LastTxnId  BIGINT;
        DECLARE @LastTxnDt  DATE;
        DECLARE @NextExpDt  DATE;

        DECLARE sub_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT MerchantName, CategoryId, AvgAmount, Frequency, Confidence,
                   LastTransactionId, LastTransaction
            FROM #CandidateSubscriptions
            WHERE Frequency <> N'Unknown';

        OPEN sub_cursor;
        FETCH NEXT FROM sub_cursor INTO
            @Merchant, @CatId, @Avg, @Freq, @Conf, @LastTxnId, @LastTxnDt;

        DECLARE @DetectedCount INT = 0;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Calculate next expected date
            IF @Freq = N'Monthly'
                SET @NextExpDt = DATEADD(MONTH, 1, @LastTxnDt);
            ELSE IF @Freq = N'Weekly'
                SET @NextExpDt = DATEADD(WEEK, 1, @LastTxnDt);
            ELSE IF @Freq = N'BiWeekly'
                SET @NextExpDt = DATEADD(WEEK, 2, @LastTxnDt);
            ELSE IF @Freq = N'Quarterly'
                SET @NextExpDt = DATEADD(QUARTER, 1, @LastTxnDt);
            ELSE IF @Freq = N'Yearly'
                SET @NextExpDt = DATEADD(YEAR, 1, @LastTxnDt);
            ELSE
                SET @NextExpDt = NULL;

            -- Upsert: update if exists, insert if not
            IF EXISTS (
                SELECT 1 FROM Subscriptions.DetectedSubscriptions
                WHERE UserId = @UserId AND MerchantName = @Merchant AND IsActive = 1
            )
            BEGIN
                UPDATE Subscriptions.DetectedSubscriptions
                SET Amount               = @Avg,
                    CategoryId           = @CatId,
                    Frequency            = @Freq,
                    NextExpectedDate     = @NextExpDt,
                    LastTransactionDate  = @LastTxnDt,
                    LastTransactionId    = @LastTxnId,
                    DetectionConfidence  = @Conf,
                    TransactionCount     = (SELECT TransactionCount FROM #CandidateSubscriptions WHERE MerchantName = @Merchant),
                    UpdatedAt            = SYSUTCDATETIME()
                WHERE UserId = @UserId AND MerchantName = @Merchant AND IsActive = 1;
            END
            ELSE
            BEGIN
                INSERT INTO Subscriptions.DetectedSubscriptions
                (UserId, MerchantName, CategoryId, Amount, Frequency,
                 NextExpectedDate, LastTransactionDate, LastTransactionId,
                 DetectionConfidence, TransactionCount)
                VALUES
                (@UserId, @Merchant, @CatId, @Avg, @Freq,
                 @NextExpDt, @LastTxnDt, @LastTxnId,
                 @Conf, (SELECT TransactionCount FROM #CandidateSubscriptions WHERE MerchantName = @Merchant));

                SET @DetectedCount = @DetectedCount + 1;
            END

            FETCH NEXT FROM sub_cursor INTO
                @Merchant, @CatId, @Avg, @Freq, @Conf, @LastTxnId, @LastTxnDt;
        END

        CLOSE sub_cursor;
        DEALLOCATE sub_cursor;

        -- Return detected subscriptions
        SELECT
            Id, MerchantName, CategoryId, Amount, Currency, Frequency,
            NextExpectedDate, LastTransactionDate, DetectionConfidence,
            TransactionCount, IsConfirmed, IsActive
        FROM Subscriptions.DetectedSubscriptions
        WHERE UserId  = @UserId
          AND IsActive = 1
        ORDER BY DetectionConfidence DESC, Amount DESC;

        DROP TABLE #CandidateSubscriptions;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#CandidateSubscriptions', N'U') IS NOT NULL
            DROP TABLE #CandidateSubscriptions;

        IF CURSOR_STATUS(N'local', N'sub_cursor') >= 0
        BEGIN
            CLOSE sub_cursor;
            DEALLOCATE sub_cursor;
        END

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();

        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

PRINT 'Core Finance stored procedures created successfully.';
GO


