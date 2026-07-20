-- ============================================================================
-- FinOS Database - Loan Schema
-- Target: Microsoft SQL Server (SSMS)
-- Description: Tables for loans, EMIs, prepayments, and amortization
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Loan
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Loan')
    EXEC('CREATE SCHEMA Loan');
GO

-- ---------------------------------------------------------------------------
-- Table: LoanTypes (reference)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.LoanTypes', N'U') IS NULL
BEGIN
    CREATE TABLE Loan.LoanTypes
    (
        Id          INT             IDENTITY(1,1)   NOT NULL,
        Name        NVARCHAR(50)                    NOT NULL,  -- HomeLoan, CarLoan, PersonalLoan, EducationLoan, CreditCard, GoldLoan
        Icon        NVARCHAR(50)                    NULL,
        SortOrder   INT                             NOT NULL DEFAULT 0,

        CONSTRAINT PK_LoanTypes PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_LoanTypes_Name UNIQUE NONCLUSTERED (Name) ON FinOS_Index
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: Loans
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.Loans', N'U') IS NULL
BEGIN
    CREATE TABLE Loan.Loans
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        LoanTypeId              INT                             NOT NULL,
        AccountId               BIGINT                          NOT NULL,  -- Core.Accounts
        LenderName              NVARCHAR(200)                   NOT NULL,
        LoanAccountNumber       NVARCHAR(50)                    NULL,
        PrincipalAmount         DECIMAL(18,2)                   NOT NULL,
        OutstandingPrincipal    DECIMAL(18,2)                   NOT NULL,
        InterestRate            DECIMAL(8,4)                    NOT NULL,
        InterestType            NVARCHAR(20)                    NOT NULL DEFAULT N'Fixed', -- Fixed, Floating
        TenureMonths            INT                             NOT NULL,
        RemainingTenureMonths   INT                             NOT NULL,
        EMI                     DECIMAL(18,2)                   NOT NULL,
        EMIDayOfMonth           INT                             NOT NULL,
        StartDate               DATE                            NOT NULL,
        MaturityDate            DATE                            NOT NULL,
        DisbursementDate        DATE                            NULL,
        ProcessingFee           DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        PrepaymentPenaltyPct    DECIMAL(5,2)                    NULL,
        IsPrepaymentAllowed     BIT                             NOT NULL DEFAULT 1,
        TotalInterestPayable    DECIMAL(18,2)                   NULL,
        TotalAmountPayable      DECIMAL(18,2)                   NULL,
        TotalPaid               DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        TotalInterestPaid       DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        TotalPrepaid            DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        NextEMIDate             DATE                            NOT NULL,
        Status                  NVARCHAR(20)                    NOT NULL DEFAULT N'Active', -- Active, Closed, Foreclosed
        Currency                NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        Notes                   NVARCHAR(500)                   NULL,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt               DATETIME2                       NULL,

        CONSTRAINT PK_Loans PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Loans_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Loans_LoanTypes FOREIGN KEY (LoanTypeId) REFERENCES Loan.LoanTypes (Id),
        CONSTRAINT FK_Loans_Accounts FOREIGN KEY (AccountId) REFERENCES Core.Accounts (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Loans_UserId
        ON Loan.Loans (UserId, Status) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Loans_NextEMI
        ON Loan.Loans (NextEMIDate, Status) WHERE Status = N'Active' ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: EMISchedule (Amortization table)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.EMISchedule', N'U') IS NULL
BEGIN
    CREATE TABLE Loan.EMISchedule
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        LoanId                  BIGINT                          NOT NULL,
        EMINumber               INT                             NOT NULL,
        EMIDate                 DATE                            NOT NULL,
        EMIAmount               DECIMAL(18,2)                   NOT NULL,
        PrincipalComponent      DECIMAL(18,2)                   NOT NULL,
        InterestComponent       DECIMAL(18,2)                   NOT NULL,
        OutstandingBefore       DECIMAL(18,2)                   NOT NULL,
        OutstandingAfter        DECIMAL(18,2)                   NOT NULL,
        IsPaid                  BIT                             NOT NULL DEFAULT 0,
        PaidDate                DATE                            NULL,
        PaidAmount              DECIMAL(18,2)                   NULL,
        ActualPrincipalPaid     DECIMAL(18,2)                   NULL,
        ActualInterestPaid      DECIMAL(18,2)                   NULL,
        LateFee                 DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_EMISchedule PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_EMISchedule_Loans FOREIGN KEY (LoanId) REFERENCES Loan.Loans (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_EMISchedule_LoanId
        ON Loan.EMISchedule (LoanId, EMINumber) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_EMISchedule_DueDate
        ON Loan.EMISchedule (EMIDate, IsPaid) WHERE IsPaid = 0 ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: LoanPrepayments
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.LoanPrepayments', N'U') IS NULL
BEGIN
    CREATE TABLE Loan.LoanPrepayments
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        LoanId                  BIGINT                          NOT NULL,
        PrepaymentDate          DATE                            NOT NULL,
        PrepaymentAmount        DECIMAL(18,2)                   NOT NULL,
        PenaltyAmount           DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        PrepaymentType          NVARCHAR(20)                    NOT NULL, -- Partial, Full
        TenureReduction         INT                             NULL,     -- Months reduced
        InterestSaved           DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        NewOutstanding          DECIMAL(18,2)                   NOT NULL,
        NewEMI                  DECIMAL(18,2)                   NULL,     -- If EMI reduced
        NewTenureMonths         INT                             NULL,
        Notes                   NVARCHAR(500)                   NULL,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_LoanPrepayments PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_LoanPrepayments_Loans FOREIGN KEY (LoanId) REFERENCES Loan.Loans (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_LoanPrepayments_LoanId
        ON Loan.LoanPrepayments (LoanId, PrepaymentDate DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: PrepaymentSimulations (what-if scenarios)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Loan.PrepaymentSimulations', N'U') IS NULL
BEGIN
    CREATE TABLE Loan.PrepaymentSimulations
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        LoanId                  BIGINT                          NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        SimulationName          NVARCHAR(100)                   NULL,
        PrepaymentAmount        DECIMAL(18,2)                   NOT NULL,
        PrepaymentDate          DATE                            NOT NULL,
        Strategy                NVARCHAR(30)                    NOT NULL, -- ReduceEMI, ReduceTenure
        OriginalTenureMonths    INT                             NOT NULL,
        NewTenureMonths         INT                             NOT NULL,
        TenureSaved             INT                             NOT NULL,
        OriginalTotalInterest   DECIMAL(18,2)                   NOT NULL,
        NewTotalInterest        DECIMAL(18,2)                   NOT NULL,
        InterestSaved           DECIMAL(18,2)                   NOT NULL,
        OriginalEMI             DECIMAL(18,2)                   NOT NULL,
        NewEMI                  DECIMAL(18,2)                   NULL,
        PenaltyEstimate         DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_PrepaymentSimulations PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_PrepaymentSimulations_Loans FOREIGN KEY (LoanId) REFERENCES Loan.Loans (Id) ON DELETE CASCADE,
        CONSTRAINT FK_PrepaymentSimulations_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE NO ACTION
    );
END
GO

PRINT 'Loan schema created successfully.';
GO
