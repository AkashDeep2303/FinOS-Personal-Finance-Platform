-- ============================================================================
-- FinOS Database - Loan Stored Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for loan management, EMI schedules,
--              prepayment simulation, and amortization
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- SP: Loan.sp_CreateLoan
-- Description: Insert a new loan with EMI details and compute totals
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.sp_CreateLoan', N'P') IS NOT NULL
    DROP PROCEDURE Loan.sp_CreateLoan;
GO

CREATE PROCEDURE Loan.sp_CreateLoan
    @UserId                  BIGINT,
    @LoanTypeId              INT,
    @AccountId               BIGINT,
    @LenderName              NVARCHAR(200),
    @LoanAccountNumber       NVARCHAR(50)     = NULL,
    @PrincipalAmount         DECIMAL(18,2),
    @InterestRate            DECIMAL(8,4),       -- Annual rate in %
    @InterestType            NVARCHAR(20)     = N'Fixed',   -- Fixed, Floating
    @TenureMonths            INT,
    @EMIDayOfMonth           INT,                -- Day of month EMI is due
    @StartDate               DATE,
    @ProcessingFee           DECIMAL(18,2)    = 0,
    @PrepaymentPenaltyPct    DECIMAL(5,2)     = NULL,
    @IsPrepaymentAllowed     BIT              = 1,
    @Notes                   NVARCHAR(500)    = NULL,
    @NewLoanId               BIGINT           OUTPUT
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

        -- Validate loan type
        IF NOT EXISTS (SELECT 1 FROM Loan.LoanTypes WHERE Id = @LoanTypeId)
        BEGIN
            RAISERROR('LoanType with Id %d does not exist.', 16, 1, @LoanTypeId);
            RETURN;
        END

        -- Validate account
        IF NOT EXISTS (SELECT 1 FROM Core.Accounts WHERE Id = @AccountId AND UserId = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Account does not exist or does not belong to user.', 16, 1);
            RETURN;
        END

        -- Validate inputs
        IF @PrincipalAmount <= 0
        BEGIN
            RAISERROR('Principal amount must be greater than zero.', 16, 1);
            RETURN;
        END

        IF @InterestRate < 0
        BEGIN
            RAISERROR('Interest rate cannot be negative.', 16, 1);
            RETURN;
        END

        IF @TenureMonths <= 0
        BEGIN
            RAISERROR('Tenure months must be greater than zero.', 16, 1);
            RETURN;
        END

        IF @EMIDayOfMonth < 1 OR @EMIDayOfMonth > 31
        BEGIN
            RAISERROR('EMI day of month must be between 1 and 31.', 16, 1);
            RETURN;
        END

        -- Calculate EMI using the formula: EMI = P * r * (1+r)^n / ((1+r)^n - 1)
        -- where r = monthly interest rate, n = number of months
        DECLARE @MonthlyRate DECIMAL(18,10) = @InterestRate / 1200.0;
        DECLARE @EMI DECIMAL(18,2);

        IF @MonthlyRate = 0
        BEGIN
            -- Zero interest: simple division
            SET @EMI = ROUND(@PrincipalAmount / @TenureMonths, 2);
        END
        ELSE
        BEGIN
            DECLARE @PowerTerm DECIMAL(18,6) = POWER(1.0 + @MonthlyRate, @TenureMonths);
            SET @EMI = ROUND(@PrincipalAmount * @MonthlyRate * @PowerTerm / (@PowerTerm - 1.0), 2);
        END

        -- Calculate total interest and total amount
        DECLARE @TotalAmountPayable DECIMAL(18,2) = @EMI * @TenureMonths;
        DECLARE @TotalInterestPayable DECIMAL(18,2) = @TotalAmountPayable - @PrincipalAmount;

        -- Calculate maturity date
        DECLARE @MaturityDate DATE = DATEADD(MONTH, @TenureMonths, @StartDate);

        -- Calculate first EMI date
        DECLARE @FirstEMIDate DATE;
        DECLARE @StartDay INT = DAY(@StartDate);
        IF @StartDay <= @EMIDayOfMonth
            SET @FirstEMIDate = DATEFROMPARTS(YEAR(@StartDate), MONTH(@StartDate), @EMIDayOfMonth);
        ELSE
            SET @FirstEMIDate = DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(@StartDate), MONTH(@StartDate), @EMIDayOfMonth));

        -- If first EMI date is before start date, push to next month
        IF @FirstEMIDate < @StartDate
            SET @FirstEMIDate = DATEADD(MONTH, 1, @FirstEMIDate);

        -- Insert the loan
        INSERT INTO Loan.Loans
        (
            UserId, LoanTypeId, AccountId, LenderName, LoanAccountNumber,
            PrincipalAmount, OutstandingPrincipal, InterestRate, InterestType,
            TenureMonths, RemainingTenureMonths, EMI, EMIDayOfMonth,
            StartDate, MaturityDate, ProcessingFee,
            PrepaymentPenaltyPct, IsPrepaymentAllowed,
            TotalInterestPayable, TotalAmountPayable,
            NextEMIDate, Notes
        )
        VALUES
        (
            @UserId, @LoanTypeId, @AccountId, @LenderName, @LoanAccountNumber,
            @PrincipalAmount, @PrincipalAmount, @InterestRate, @InterestType,
            @TenureMonths, @TenureMonths, @EMI, @EMIDayOfMonth,
            @StartDate, @MaturityDate, @ProcessingFee,
            @PrepaymentPenaltyPct, @IsPrepaymentAllowed,
            @TotalInterestPayable, @TotalAmountPayable,
            @FirstEMIDate, @Notes
        );

        SET @NewLoanId = SCOPE_IDENTITY();

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'CREATE', N'Loan', CAST(@NewLoanId AS NVARCHAR(256)),
            N'{"PrincipalAmount":' + CAST(@PrincipalAmount AS NVARCHAR(50)) +
            N',"EMI":' + CAST(@EMI AS NVARCHAR(50)) +
            N',"TenureMonths":' + CAST(@TenureMonths AS NVARCHAR(10)) +
            N',"InterestRate":' + CAST(@InterestRate AS NVARCHAR(50)) + N'}'
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
-- SP: Loan.sp_GenerateAmortizationSchedule
-- Description: Generate the full EMI schedule for a loan (principal/interest
--              split per EMI)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.sp_GenerateAmortizationSchedule', N'P') IS NOT NULL
    DROP PROCEDURE Loan.sp_GenerateAmortizationSchedule;
GO

CREATE PROCEDURE Loan.sp_GenerateAmortizationSchedule
    @LoanId  BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get loan details
        DECLARE @Principal      DECIMAL(18,2);
        DECLARE @InterestRate   DECIMAL(8,4);
        DECLARE @EMI            DECIMAL(18,2);
        DECLARE @TenureMonths   INT;
        DECLARE @EMIDayOfMonth  INT;
        DECLARE @StartDate      DATE;
        DECLARE @UserId         BIGINT;

        SELECT
            @Principal     = OutstandingPrincipal,
            @InterestRate  = InterestRate,
            @EMI           = EMI,
            @TenureMonths  = RemainingTenureMonths,
            @EMIDayOfMonth = EMIDayOfMonth,
            @StartDate     = NextEMIDate,
            @UserId        = UserId
        FROM Loan.Loans
        WHERE Id = @LoanId AND Status = N'Active' AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Loan with Id %d not found or is not active.', 16, 1, @LoanId);
            RETURN;
        END

        -- Delete any existing future (unpaid) EMI schedule entries
        DELETE FROM Loan.EMISchedule
        WHERE LoanId = @LoanId AND IsPaid = 0;

        -- Generate amortization schedule
        DECLARE @MonthlyRate   DECIMAL(18,10) = @InterestRate / 1200.0;
        DECLARE @EMINum        INT = 1;
        DECLARE @Outstanding   DECIMAL(18,2) = @Principal;
        DECLARE @EMIDate       DATE = @StartDate;
        DECLARE @TotalInterest DECIMAL(18,2) = 0;
        DECLARE @TotalPrincipal DECIMAL(18,2) = 0;

        WHILE @EMINum <= @TenureMonths AND @Outstanding > 0
        BEGIN
            -- Calculate interest and principal components
            DECLARE @InterestComponent DECIMAL(18,2) = ROUND(@Outstanding * @MonthlyRate, 2);
            DECLARE @PrincipalComponent DECIMAL(18,2);

            -- Last EMI: adjust to clear remaining outstanding
            IF @EMINum = @TenureMonths
            BEGIN
                SET @PrincipalComponent = @Outstanding;
                DECLARE @AdjustedEMI DECIMAL(18,2) = @PrincipalComponent + @InterestComponent;
            END
            ELSE
            BEGIN
                SET @PrincipalComponent = @EMI - @InterestComponent;
                SET @AdjustedEMI = @EMI;
            END

            -- Ensure principal component doesn't exceed outstanding
            IF @PrincipalComponent > @Outstanding
            BEGIN
                SET @PrincipalComponent = @Outstanding;
                SET @AdjustedEMI = @PrincipalComponent + @InterestComponent;
            END

            DECLARE @OutstandingAfter DECIMAL(18,2) = @Outstanding - @PrincipalComponent;

            -- Handle edge: negative outstanding due to rounding
            IF @OutstandingAfter < 0 SET @OutstandingAfter = 0;

            INSERT INTO Loan.EMISchedule
            (
                LoanId, EMINumber, EMIDate, EMIAmount,
                PrincipalComponent, InterestComponent,
                OutstandingBefore, OutstandingAfter
            )
            VALUES
            (
                @LoanId, @EMINum, @EMIDate, @AdjustedEMI,
                @PrincipalComponent, @InterestComponent,
                @Outstanding, @OutstandingAfter
            );

            SET @TotalInterest   = @TotalInterest + @InterestComponent;
            SET @TotalPrincipal  = @TotalPrincipal + @PrincipalComponent;
            SET @Outstanding     = @OutstandingAfter;
            SET @EMINum          = @EMINum + 1;

            -- Calculate next EMI date
            DECLARE @NextMonth DATE = DATEADD(MONTH, 1, @EMIDate);
            DECLARE @LastDay   INT  = DAY(EOMONTH(@NextMonth));
            DECLARE @DayToUse  INT  = CASE WHEN @EMIDayOfMonth > @LastDay THEN @LastDay ELSE @EMIDayOfMonth END;
            SET @EMIDate = DATEFROMPARTS(YEAR(@NextMonth), MONTH(@NextMonth), @DayToUse);
        END

        -- Return the generated schedule
        SELECT
            EMINumber, EMIDate, EMIAmount,
            PrincipalComponent, InterestComponent,
            OutstandingBefore, OutstandingAfter, IsPaid
        FROM Loan.EMISchedule
        WHERE LoanId = @LoanId
        ORDER BY EMINumber;

        -- Summary
        SELECT
            @TenureMonths     AS TotalEMIs,
            @TotalPrincipal   AS TotalPrincipal,
            @TotalInterest    AS TotalInterest,
            @TotalPrincipal + @TotalInterest AS TotalAmount;
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
-- SP: Loan.sp_RecordEMIPayment
-- Description: Mark an EMI as paid and update loan outstanding
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.sp_RecordEMIPayment', N'P') IS NOT NULL
    DROP PROCEDURE Loan.sp_RecordEMIPayment;
GO

CREATE PROCEDURE Loan.sp_RecordEMIPayment
    @LoanId              BIGINT,
    @EMINumber           INT,
    @PaidDate            DATE           = NULL,   -- Defaults to today
    @PaidAmount          DECIMAL(18,2)  = NULL,   -- Defaults to scheduled EMI amount
    @LateFee             DECIMAL(18,2)  = 0
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate loan exists
        IF NOT EXISTS (SELECT 1 FROM Loan.Loans WHERE Id = @LoanId AND Status = N'Active' AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Loan with Id %d not found or is not active.', 16, 1, @LoanId);
            RETURN;
        END

        -- Get the EMI schedule entry
        DECLARE @EMIAmount           DECIMAL(18,2);
        DECLARE @PrincipalComponent  DECIMAL(18,2);
        DECLARE @InterestComponent   DECIMAL(18,2);
        DECLARE @OutstandingAfter    DECIMAL(18,2);
        DECLARE @IsPaid              BIT;
        DECLARE @UserId              BIGINT;

        SELECT
            @EMIAmount          = EMIAmount,
            @PrincipalComponent = PrincipalComponent,
            @InterestComponent  = InterestComponent,
            @OutstandingAfter   = OutstandingAfter,
            @IsPaid             = IsPaid
        FROM Loan.EMISchedule
        WHERE LoanId = @LoanId AND EMINumber = @EMINumber;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('EMI number %d not found for loan %d.', 16, 1, @EMINumber, @LoanId);
            RETURN;
        END

        IF @IsPaid = 1
        BEGIN
            RAISERROR('EMI number %d is already paid.', 16, 1, @EMINumber);
            RETURN;
        END

        -- Resolve defaults
        IF @PaidDate IS NULL SET @PaidDate = CAST(SYSUTCDATETIME() AS DATE);
        IF @PaidAmount IS NULL SET @PaidAmount = @EMIAmount + @LateFee;

        -- Determine actual principal and interest split if paid amount differs
        DECLARE @ActualPrincipal DECIMAL(18,2) = @PrincipalComponent;
        DECLARE @ActualInterest  DECIMAL(18,2) = @InterestComponent;

        IF @PaidAmount <> @EMIAmount + @LateFee
        BEGIN
            -- If different amount paid, allocate: interest first, then principal
            DECLARE @AmountAfterLateFee DECIMAL(18,2) = @PaidAmount - @LateFee;

            IF @AmountAfterLateFee <= @InterestComponent
            BEGIN
                SET @ActualInterest  = @AmountAfterLateFee;
                SET @ActualPrincipal = 0;
            END
            ELSE
            BEGIN
                SET @ActualInterest  = @InterestComponent;
                SET @ActualPrincipal = @AmountAfterLateFee - @InterestComponent;
                -- Cap at remaining principal
                IF @ActualPrincipal > @PrincipalComponent
                    SET @ActualPrincipal = @PrincipalComponent;
            END
        END

        -- Mark EMI as paid
        UPDATE Loan.EMISchedule
        SET
            IsPaid              = 1,
            PaidDate            = @PaidDate,
            PaidAmount          = @PaidAmount,
            ActualPrincipalPaid = @ActualPrincipal,
            ActualInterestPaid  = @ActualInterest,
            LateFee             = @LateFee,
            UpdatedAt           = SYSUTCDATETIME()
        WHERE LoanId = @LoanId AND EMINumber = @EMINumber;

        -- Update loan details
        SELECT @UserId = UserId FROM Loan.Loans WHERE Id = @LoanId;

        DECLARE @NewOutstanding DECIMAL(18,2);
        SELECT @NewOutstanding = OutstandingPrincipal - @ActualPrincipal
        FROM Loan.Loans WHERE Id = @LoanId;

        IF @NewOutstanding < 0 SET @NewOutstanding = 0;

        -- Find next unpaid EMI date
        DECLARE @NextEMIDate DATE;
        SELECT TOP 1 @NextEMIDate = EMIDate
        FROM Loan.EMISchedule
        WHERE LoanId = @LoanId AND IsPaid = 0
        ORDER BY EMINumber;

        -- Count remaining EMIs
        DECLARE @RemainingTenure INT;
        SELECT @RemainingTenure = COUNT(*)
        FROM Loan.EMISchedule
        WHERE LoanId = @LoanId AND IsPaid = 0;

        -- Update loan
        UPDATE Loan.Loans
        SET
            OutstandingPrincipal  = @NewOutstanding,
            TotalPaid             = TotalPaid + @PaidAmount,
            TotalInterestPaid     = TotalInterestPaid + @ActualInterest,
            RemainingTenureMonths = @RemainingTenure,
            NextEMIDate           = ISNULL(@NextEMIDate, MaturityDate),
            Status                = CASE WHEN @NewOutstanding <= 0 THEN N'Closed' ELSE Status END,
            UpdatedAt             = SYSUTCDATETIME()
        WHERE Id = @LoanId;

        -- Debit the linked account
        DECLARE @AccountId BIGINT;
        SELECT @AccountId = AccountId FROM Loan.Loans WHERE Id = @LoanId;
        DECLARE @PaidDelta DECIMAL(18,2) = -@PaidAmount;
        EXEC Core.sp_UpdateAccountBalance @AccountId = @AccountId, @DeltaAmount = @PaidDelta;

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'UPDATE', N'EMI', CAST(@EMINumber AS NVARCHAR(256)),
            N'{"LoanId":' + CAST(@LoanId AS NVARCHAR(20)) +
            N',"EMINumber":' + CAST(@EMINumber AS NVARCHAR(10)) +
            N',"PaidAmount":' + CAST(@PaidAmount AS NVARCHAR(50)) +
            N',"PrincipalPaid":' + CAST(@ActualPrincipal AS NVARCHAR(50)) +
            N',"InterestPaid":' + CAST(@ActualInterest AS NVARCHAR(50)) + N'}'
        );

        -- Return payment details
        SELECT
            @EMINumber          AS EMINumber,
            @PaidAmount         AS PaidAmount,
            @ActualPrincipal    AS PrincipalPaid,
            @ActualInterest     AS InterestPaid,
            @LateFee            AS LateFee,
            @NewOutstanding     AS RemainingOutstanding,
            @RemainingTenure    AS RemainingEMIs;
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
-- SP: Loan.sp_SimulatePrepayment
-- Description: Calculate what-if for partial/full prepayment (reduce EMI vs
--              reduce tenure) without persisting changes
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.sp_SimulatePrepayment', N'P') IS NOT NULL
    DROP PROCEDURE Loan.sp_SimulatePrepayment;
GO

CREATE PROCEDURE Loan.sp_SimulatePrepayment
    @LoanId              BIGINT,
    @PrepaymentAmount    DECIMAL(18,2),
    @PrepaymentDate      DATE           = NULL,   -- Defaults to today
    @Strategy            NVARCHAR(30)   = N'ReduceTenure'  -- ReduceEMI, ReduceTenure
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate loan
        DECLARE @OutstandingPrincipal DECIMAL(18,2);
        DECLARE @InterestRate         DECIMAL(8,4);
        DECLARE @EMI                  DECIMAL(18,2);
        DECLARE @RemainingTenure      INT;
        DECLARE @IsPrepaymentAllowed  BIT;
        DECLARE @PrepaymentPenaltyPct DECIMAL(5,2);
        DECLARE @UserId               BIGINT;

        SELECT
            @OutstandingPrincipal = OutstandingPrincipal,
            @InterestRate         = InterestRate,
            @EMI                  = EMI,
            @RemainingTenure      = RemainingTenureMonths,
            @IsPrepaymentAllowed  = IsPrepaymentAllowed,
            @PrepaymentPenaltyPct = ISNULL(PrepaymentPenaltyPct, 0),
            @UserId               = UserId
        FROM Loan.Loans
        WHERE Id = @LoanId AND Status = N'Active' AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Loan with Id %d not found or is not active.', 16, 1, @LoanId);
            RETURN;
        END

        IF @IsPrepaymentAllowed = 0
        BEGIN
            RAISERROR('Prepayment is not allowed for this loan.', 16, 1);
            RETURN;
        END

        IF @PrepaymentAmount <= 0
        BEGIN
            RAISERROR('Prepayment amount must be greater than zero.', 16, 1);
            RETURN;
        END

        IF @PrepaymentAmount > @OutstandingPrincipal
        BEGIN
            DECLARE @OutstandingText NVARCHAR(50) = CAST(@OutstandingPrincipal AS NVARCHAR(50));
            RAISERROR('Prepayment amount cannot exceed outstanding principal of %s.', 16, 1,
                @OutstandingText);
            RETURN;
        END

        IF @Strategy NOT IN (N'ReduceEMI', N'ReduceTenure')
        BEGIN
            RAISERROR('Strategy must be ReduceEMI or ReduceTenure.', 16, 1);
            RETURN;
        END

        IF @PrepaymentDate IS NULL
            SET @PrepaymentDate = CAST(SYSUTCDATETIME() AS DATE);

        -- Calculate penalty
        DECLARE @PenaltyAmount DECIMAL(18,2) = ROUND(@PrepaymentAmount * @PrepaymentPenaltyPct / 100.0, 2);

        -- New outstanding after prepayment
        DECLARE @NewOutstanding DECIMAL(18,2) = @OutstandingPrincipal - @PrepaymentAmount;

        -- Monthly interest rate
        DECLARE @MonthlyRate DECIMAL(18,10) = @InterestRate / 1200.0;

        -- Calculate original total interest (remaining)
        DECLARE @OriginalTotalInterest DECIMAL(18,2);
        IF @MonthlyRate = 0
            SET @OriginalTotalInterest = 0;
        ELSE
        BEGIN
            -- Sum of interest components from unpaid EMIs
            SELECT @OriginalTotalInterest = ISNULL(SUM(InterestComponent), 0)
            FROM Loan.EMISchedule
            WHERE LoanId = @LoanId AND IsPaid = 0;
        END

        -- Calculate new values based on strategy
        DECLARE @NewEMI           DECIMAL(18,2);
        DECLARE @NewTenureMonths  INT;
        DECLARE @NewTotalInterest DECIMAL(18,2);
        DECLARE @InterestSaved    DECIMAL(18,2);

        IF @NewOutstanding <= 0
        BEGIN
            -- Full prepayment
            SET @NewEMI           = 0;
            SET @NewTenureMonths  = 0;
            SET @NewTotalInterest = 0;
            SET @InterestSaved    = @OriginalTotalInterest;
        END
        ELSE IF @Strategy = N'ReduceTenure'
        BEGIN
            -- Keep EMI same, calculate new tenure
            SET @NewEMI = @EMI;

            IF @MonthlyRate = 0
                SET @NewTenureMonths = CEILING(@NewOutstanding / @EMI);
            ELSE
            BEGIN
                -- n = -log(1 - P*r/EMI) / log(1+r)
                DECLARE @Ratio DECIMAL(18,10) = 1.0 - (@NewOutstanding * @MonthlyRate / @EMI);
                IF @Ratio <= 0
                BEGIN
                    -- EMI too low to cover interest
                    SET @NewTenureMonths = 999;  -- Effectively impossible
                END
                ELSE
                    SET @NewTenureMonths = CEILING(-LOG(@Ratio) / LOG(1.0 + @MonthlyRate));
            END

            -- Calculate new total interest for reduced tenure
            SET @NewTotalInterest = (@NewEMI * @NewTenureMonths) - @NewOutstanding;
            IF @NewTotalInterest < 0 SET @NewTotalInterest = 0;

            SET @InterestSaved = @OriginalTotalInterest - @NewTotalInterest;
            IF @InterestSaved < 0 SET @InterestSaved = 0;
        END
        ELSE -- ReduceEMI
        BEGIN
            -- Keep tenure same, calculate new EMI
            SET @NewTenureMonths = @RemainingTenure;

            IF @MonthlyRate = 0
                SET @NewEMI = ROUND(@NewOutstanding / @NewTenureMonths, 2);
            ELSE
            BEGIN
                DECLARE @PowerTerm DECIMAL(18,6) = POWER(1.0 + @MonthlyRate, @NewTenureMonths);
                SET @NewEMI = ROUND(@NewOutstanding * @MonthlyRate * @PowerTerm / (@PowerTerm - 1.0), 2);
            END

            SET @NewTotalInterest = (@NewEMI * @NewTenureMonths) - @NewOutstanding;
            IF @NewTotalInterest < 0 SET @NewTotalInterest = 0;

            SET @InterestSaved = @OriginalTotalInterest - @NewTotalInterest;
            IF @InterestSaved < 0 SET @InterestSaved = 0;
        END

        -- Save simulation
        INSERT INTO Loan.PrepaymentSimulations
        (
            LoanId, UserId, PrepaymentAmount, PrepaymentDate, Strategy,
            OriginalTenureMonths, NewTenureMonths,
            TenureSaved, OriginalTotalInterest, NewTotalInterest,
            InterestSaved, OriginalEMI, NewEMI, PenaltyEstimate
        )
        VALUES
        (
            @LoanId, @UserId, @PrepaymentAmount, @PrepaymentDate, @Strategy,
            @RemainingTenure, @NewTenureMonths,
            @RemainingTenure - @NewTenureMonths, @OriginalTotalInterest, @NewTotalInterest,
            @InterestSaved, @EMI, @NewEMI, @PenaltyAmount
        );

        -- Return simulation results
        SELECT
            @PrepaymentAmount       AS PrepaymentAmount,
            @PenaltyAmount          AS PenaltyEstimate,
            @Strategy               AS Strategy,
            @OutstandingPrincipal   AS OriginalOutstanding,
            @NewOutstanding         AS NewOutstanding,
            @EMI                    AS OriginalEMI,
            @NewEMI                 AS NewEMI,
            @RemainingTenure        AS OriginalTenureMonths,
            @NewTenureMonths        AS NewTenureMonths,
            @RemainingTenure - @NewTenureMonths AS TenureSaved,
            @OriginalTotalInterest  AS OriginalTotalInterest,
            @NewTotalInterest       AS NewTotalInterest,
            @InterestSaved          AS InterestSaved,
            @PrepaymentAmount + @PenaltyAmount AS TotalCashRequired;
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
-- SP: Loan.sp_ExecutePrepayment
-- Description: Apply a prepayment and regenerate the amortization schedule
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.sp_ExecutePrepayment', N'P') IS NOT NULL
    DROP PROCEDURE Loan.sp_ExecutePrepayment;
GO

CREATE PROCEDURE Loan.sp_ExecutePrepayment
    @LoanId              BIGINT,
    @PrepaymentAmount    DECIMAL(18,2),
    @Strategy            NVARCHAR(30)   = N'ReduceTenure',  -- ReduceEMI, ReduceTenure
    @PrepaymentDate      DATE           = NULL,
    @Notes               NVARCHAR(500)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Get current loan state
        DECLARE @OutstandingPrincipal DECIMAL(18,2);
        DECLARE @InterestRate         DECIMAL(8,4);
        DECLARE @EMI                  DECIMAL(18,2);
        DECLARE @RemainingTenure      INT;
        DECLARE @EMIDayOfMonth        INT;
        DECLARE @IsPrepaymentAllowed  BIT;
        DECLARE @PrepaymentPenaltyPct DECIMAL(5,2);
        DECLARE @AccountId            BIGINT;
        DECLARE @UserId               BIGINT;
        DECLARE @NextEMIDate          DATE;

        SELECT
            @OutstandingPrincipal = OutstandingPrincipal,
            @InterestRate         = InterestRate,
            @EMI                  = EMI,
            @RemainingTenure      = RemainingTenureMonths,
            @EMIDayOfMonth        = EMIDayOfMonth,
            @IsPrepaymentAllowed  = IsPrepaymentAllowed,
            @PrepaymentPenaltyPct = ISNULL(PrepaymentPenaltyPct, 0),
            @AccountId            = AccountId,
            @UserId               = UserId,
            @NextEMIDate          = NextEMIDate
        FROM Loan.Loans
        WHERE Id = @LoanId AND Status = N'Active' AND DeletedAt IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Loan with Id %d not found or is not active.', 16, 1, @LoanId);
            RETURN;
        END

        IF @IsPrepaymentAllowed = 0
        BEGIN
            RAISERROR('Prepayment is not allowed for this loan.', 16, 1);
            RETURN;
        END

        IF @PrepaymentAmount <= 0
        BEGIN
            RAISERROR('Prepayment amount must be greater than zero.', 16, 1);
            RETURN;
        END

        IF @PrepaymentAmount > @OutstandingPrincipal
        BEGIN
            RAISERROR('Prepayment amount cannot exceed outstanding principal.', 16, 1);
            RETURN;
        END

        IF @PrepaymentDate IS NULL
            SET @PrepaymentDate = CAST(SYSUTCDATETIME() AS DATE);

        -- Calculate penalty
        DECLARE @PenaltyAmount DECIMAL(18,2) = ROUND(@PrepaymentAmount * @PrepaymentPenaltyPct / 100.0, 2);

        -- Debit the account for prepayment amount + penalty
        DECLARE @PrepaymentDelta DECIMAL(18,2) = -(@PrepaymentAmount + @PenaltyAmount);
        EXEC Core.sp_UpdateAccountBalance
            @AccountId   = @AccountId,
            @DeltaAmount = @PrepaymentDelta;

        -- New outstanding after prepayment
        DECLARE @NewOutstanding DECIMAL(18,2) = @OutstandingPrincipal - @PrepaymentAmount;

        -- Monthly interest rate
        DECLARE @MonthlyRate DECIMAL(18,10) = @InterestRate / 1200.0;

        -- Calculate original remaining interest
        DECLARE @OriginalTotalInterest DECIMAL(18,2);
        SELECT @OriginalTotalInterest = ISNULL(SUM(InterestComponent), 0)
        FROM Loan.EMISchedule
        WHERE LoanId = @LoanId AND IsPaid = 0;

        -- Calculate new EMI and tenure
        DECLARE @NewEMI          DECIMAL(18,2);
        DECLARE @NewTenureMonths INT;
        DECLARE @NewTotalInterest DECIMAL(18,2);
        DECLARE @InterestSaved   DECIMAL(18,2);
        DECLARE @PrepaymentType  NVARCHAR(20);

        IF @NewOutstanding <= 0
        BEGIN
            -- Full prepayment - loan closed
            SET @NewEMI           = 0;
            SET @NewTenureMonths  = 0;
            SET @NewTotalInterest = 0;
            SET @InterestSaved    = @OriginalTotalInterest;
            SET @PrepaymentType   = N'Full';
        END
        ELSE IF @Strategy = N'ReduceTenure'
        BEGIN
            SET @NewEMI = @EMI;
            SET @PrepaymentType = N'Partial';

            IF @MonthlyRate = 0
                SET @NewTenureMonths = CEILING(@NewOutstanding / @EMI);
            ELSE
            BEGIN
                DECLARE @Ratio DECIMAL(18,10) = 1.0 - (@NewOutstanding * @MonthlyRate / @EMI);
                IF @Ratio <= 0
                    SET @NewTenureMonths = @RemainingTenure;
                ELSE
                    SET @NewTenureMonths = CEILING(-LOG(@Ratio) / LOG(1.0 + @MonthlyRate));
            END

            SET @NewTotalInterest = (@NewEMI * @NewTenureMonths) - @NewOutstanding;
            IF @NewTotalInterest < 0 SET @NewTotalInterest = 0;
            SET @InterestSaved = @OriginalTotalInterest - @NewTotalInterest;
            IF @InterestSaved < 0 SET @InterestSaved = 0;
        END
        ELSE -- ReduceEMI
        BEGIN
            SET @NewTenureMonths = @RemainingTenure;
            SET @PrepaymentType  = N'Partial';

            IF @MonthlyRate = 0
                SET @NewEMI = ROUND(@NewOutstanding / @NewTenureMonths, 2);
            ELSE
            BEGIN
                DECLARE @PowerTerm DECIMAL(18,6) = POWER(1.0 + @MonthlyRate, @NewTenureMonths);
                SET @NewEMI = ROUND(@NewOutstanding * @MonthlyRate * @PowerTerm / (@PowerTerm - 1.0), 2);
            END

            SET @NewTotalInterest = (@NewEMI * @NewTenureMonths) - @NewOutstanding;
            IF @NewTotalInterest < 0 SET @NewTotalInterest = 0;
            SET @InterestSaved = @OriginalTotalInterest - @NewTotalInterest;
            IF @InterestSaved < 0 SET @InterestSaved = 0;
        END

        -- Record prepayment
        INSERT INTO Loan.LoanPrepayments
        (
            LoanId, PrepaymentDate, PrepaymentAmount, PenaltyAmount,
            PrepaymentType, TenureReduction, InterestSaved,
            NewOutstanding, NewEMI, NewTenureMonths, Notes
        )
        VALUES
        (
            @LoanId, @PrepaymentDate, @PrepaymentAmount, @PenaltyAmount,
            @PrepaymentType, @RemainingTenure - @NewTenureMonths, @InterestSaved,
            @NewOutstanding, @NewEMI, @NewTenureMonths, @Notes
        );

        -- Update loan
        UPDATE Loan.Loans
        SET
            OutstandingPrincipal  = @NewOutstanding,
            TotalPrepaid          = TotalPrepaid + @PrepaymentAmount,
            TotalPaid             = TotalPaid + @PrepaymentAmount + @PenaltyAmount,
            EMI                   = CASE WHEN @NewOutstanding <= 0 THEN EMI ELSE @NewEMI END,
            RemainingTenureMonths = @NewTenureMonths,
            Status                = CASE WHEN @NewOutstanding <= 0 THEN N'Closed' ELSE Status END,
            UpdatedAt             = SYSUTCDATETIME()
        WHERE Id = @LoanId;

        -- Delete old unpaid schedule and regenerate
        DELETE FROM Loan.EMISchedule WHERE LoanId = @LoanId AND IsPaid = 0;

        -- Regenerate amortization if loan still active
        IF @NewOutstanding > 0
        BEGIN
            -- Update loan EMI for schedule generation
            UPDATE Loan.Loans
            SET EMI = @NewEMI,
                RemainingTenureMonths = @NewTenureMonths,
                OutstandingPrincipal = @NewOutstanding
            WHERE Id = @LoanId;

            -- Generate new schedule
            EXEC Loan.sp_GenerateAmortizationSchedule @LoanId = @LoanId;
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @UserId, N'PREPAYMENT', N'Loan', CAST(@LoanId AS NVARCHAR(256)),
            N'{"PrepaymentAmount":' + CAST(@PrepaymentAmount AS NVARCHAR(50)) +
            N',"Strategy":"' + @Strategy + N'"' +
            N',"InterestSaved":' + CAST(@InterestSaved AS NVARCHAR(50)) +
            N',"NewOutstanding":' + CAST(@NewOutstanding AS NVARCHAR(50)) + N'}'
        );

        -- Return result
        SELECT
            @PrepaymentAmount       AS PrepaymentAmount,
            @PenaltyAmount          AS PenaltyAmount,
            @PrepaymentType         AS PrepaymentType,
            @Strategy               AS Strategy,
            @OutstandingPrincipal   AS PreviousOutstanding,
            @NewOutstanding         AS NewOutstanding,
            @EMI                    AS PreviousEMI,
            @NewEMI                 AS NewEMI,
            @RemainingTenure        AS PreviousTenureMonths,
            @NewTenureMonths        AS NewTenureMonths,
            @InterestSaved          AS InterestSaved,
            CASE WHEN @NewOutstanding <= 0 THEN N'Closed' ELSE N'Active' END AS LoanStatus;
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
-- SP: Loan.sp_GetLoanSummary
-- Description: Outstanding, total paid, interest saved, months remaining
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.sp_GetLoanSummary', N'P') IS NOT NULL
    DROP PROCEDURE Loan.sp_GetLoanSummary;
GO

CREATE PROCEDURE Loan.sp_GetLoanSummary
    @LoanId  BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate loan
        IF NOT EXISTS (SELECT 1 FROM Loan.Loans WHERE Id = @LoanId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('Loan with Id %d not found.', 16, 1, @LoanId);
            RETURN;
        END

        -- Loan details summary
        SELECT
            l.Id                        AS LoanId,
            lt.Name                     AS LoanTypeName,
            l.LenderName,
            l.LoanAccountNumber,
            l.PrincipalAmount,
            l.OutstandingPrincipal,
            l.InterestRate,
            l.InterestType,
            l.TenureMonths              AS OriginalTenure,
            l.RemainingTenureMonths,
            l.EMI,
            l.EMIDayOfMonth,
            l.StartDate,
            l.MaturityDate,
            l.Status,
            l.TotalPaid,
            l.TotalInterestPaid,
            l.TotalPrepaid,
            l.ProcessingFee,
            l.PrepaymentPenaltyPct,
            l.IsPrepaymentAllowed,
            l.NextEMIDate,
            -- Calculated fields
            l.PrincipalAmount - l.OutstandingPrincipal AS PrincipalPaid,
            CASE
                WHEN l.TotalInterestPayable IS NOT NULL
                THEN l.TotalInterestPayable - l.TotalInterestPaid
                ELSE NULL
            END                         AS InterestRemaining,
            CASE
                WHEN l.TotalInterestPayable IS NOT NULL AND l.TotalInterestPayable > 0
                THEN ROUND((l.TotalInterestPaid * 100.0) / l.TotalInterestPayable, 2)
                ELSE 0
            END                         AS InterestPaidPct,
            CASE
                WHEN l.PrincipalAmount > 0
                THEN ROUND(((l.PrincipalAmount - l.OutstandingPrincipal) * 100.0) / l.PrincipalAmount, 2)
                ELSE 0
            END                         AS PrincipalPaidPct
        FROM Loan.Loans l
        INNER JOIN Loan.LoanTypes lt ON l.LoanTypeId = lt.Id
        WHERE l.Id = @LoanId;

        -- EMI payment history (last 12)
        SELECT TOP 12
            EMINumber,
            EMIDate,
            EMIAmount,
            ISNULL(ActualPrincipalPaid, PrincipalComponent) AS PrincipalPaid,
            ISNULL(ActualInterestPaid, InterestComponent)   AS InterestPaid,
            PaidDate,
            PaidAmount,
            LateFee,
            IsPaid
        FROM Loan.EMISchedule
        WHERE LoanId = @LoanId
        ORDER BY EMINumber DESC;

        -- Prepayment history
        SELECT
            PrepaymentDate,
            PrepaymentAmount,
            PenaltyAmount,
            PrepaymentType,
            TenureReduction,
            InterestSaved,
            NewOutstanding
        FROM Loan.LoanPrepayments
        WHERE LoanId = @LoanId
        ORDER BY PrepaymentDate DESC;
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

PRINT 'Loan stored procedures created successfully.';
GO

