-- ============================================================================
-- FinOS Database - Investment Schema
-- Target: Microsoft SQL Server (SSMS)
-- Description: Tables for investments - MF, stocks, FD, gold, crypto, EPF
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Investment
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Investment')
    EXEC('CREATE SCHEMA Investment');
GO

-- ---------------------------------------------------------------------------
-- Table: InvestmentTypes (reference)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.InvestmentTypes', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.InvestmentTypes
    (
        Id          INT             IDENTITY(1,1)   NOT NULL,
        Name        NVARCHAR(50)                    NOT NULL,  -- MutualFund, Stock, FD, Gold, Crypto, EPF, PPF, NPS, RealEstate, Bond
        AssetClass  NVARCHAR(30)                    NOT NULL,  -- Equity, Debt, Gold, RealEstate, Crypto, Mixed
        Icon        NVARCHAR(50)                    NULL,
        IsTaxSaving BIT                             NOT NULL DEFAULT 0,
        SortOrder   INT                             NOT NULL DEFAULT 0,

        CONSTRAINT PK_InvestmentTypes PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_InvestmentTypes_Name UNIQUE NONCLUSTERED (Name) ON FinOS_Index
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: Portfolios
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.Portfolios', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.Portfolios
    (
        Id              BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId          BIGINT                          NOT NULL,
        Name            NVARCHAR(100)                   NOT NULL DEFAULT N'Default',
        Description     NVARCHAR(500)                   NULL,
        Currency        NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        IsDefault       BIT                             NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt       DATETIME2                       NULL,

        CONSTRAINT PK_Portfolios PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Portfolios_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_Portfolios_UserId
        ON Investment.Portfolios (UserId) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: PortfolioTargetAllocations
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.PortfolioTargetAllocations', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.PortfolioTargetAllocations
    (
        Id          BIGINT IDENTITY(1,1) NOT NULL,
        PortfolioId BIGINT NOT NULL,
        AssetClass  NVARCHAR(30) NOT NULL,
        TargetPct   DECIMAL(5,2) NOT NULL,
        CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_PortfolioTargetAllocations PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_PortfolioTargetAllocations_Portfolios FOREIGN KEY (PortfolioId)
            REFERENCES Investment.Portfolios (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_PortfolioTargetAllocations UNIQUE NONCLUSTERED (PortfolioId, AssetClass) ON FinOS_Index,
        CONSTRAINT CK_PortfolioTargetAllocations_Pct CHECK (TargetPct >= 0 AND TargetPct <= 100)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: PortfolioValueSnapshots
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.PortfolioValueSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.PortfolioValueSnapshots
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        PortfolioId     BIGINT NOT NULL,
        SnapshotDate    DATE NOT NULL,
        InvestedValue   DECIMAL(18,2) NOT NULL,
        CurrentValue    DECIMAL(18,2) NOT NULL,
        UnrealizedGain  DECIMAL(18,2) NOT NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_PortfolioValueSnapshots PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_PortfolioValueSnapshots_Portfolios FOREIGN KEY (PortfolioId)
            REFERENCES Investment.Portfolios (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_PortfolioValueSnapshots_Date UNIQUE NONCLUSTERED (PortfolioId, SnapshotDate) ON FinOS_Index
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: Holdings (all investment holdings)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.Holdings', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.Holdings
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        PortfolioId             BIGINT                          NOT NULL,
        InvestmentTypeId        INT                             NOT NULL,
        Symbol                  NVARCHAR(50)                    NOT NULL,  -- ISIN, ticker, scheme code
        Name                    NVARCHAR(300)                   NOT NULL,  -- Fund/Stock name
        Quantity                DECIMAL(18,4)                   NOT NULL,
        AvgPurchasePrice        DECIMAL(18,4)                   NOT NULL,
        CurrentPrice            DECIMAL(18,4)                   NULL,
        CurrentValue            DECIMAL(18,2)                   NULL,
        InvestedAmount          DECIMAL(18,2)                   NOT NULL,
        DayChange               DECIMAL(18,2)                   NULL,
        DayChangePct            DECIMAL(8,4)                    NULL,
        TotalReturn             DECIMAL(18,2)                   NULL,
        TotalReturnPct          DECIMAL(8,4)                    NULL,
        XIRR                    DECIMAL(8,4)                    NULL,      -- Annualized return
        CAGR                    DECIMAL(8,4)                    NULL,
        DividendReceived        DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        Currency                NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        FundHouse               NVARCHAR(200)                   NULL,      -- For mutual funds
        FundCategory            NVARCHAR(100)                   NULL,      -- Large Cap, Mid Cap, etc.
        RiskLevel               NVARCHAR(20)                    NULL,      -- Low, Medium, High
        MaturityDate            DATE                            NULL,      -- For FD, bonds
        InterestRate            DECIMAL(8,4)                    NULL,      -- For FD
        LockInEndDate           DATE                            NULL,
        NAVDate                 DATE                            NULL,
        LastPriceUpdateAt       DATETIME2                       NULL,
        Notes                   NVARCHAR(500)                   NULL,
        IsActive                BIT                             NOT NULL DEFAULT 1,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt               DATETIME2                       NULL,

        CONSTRAINT PK_Holdings PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Holdings_Portfolios FOREIGN KEY (PortfolioId) REFERENCES Investment.Portfolios (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Holdings_InvestmentTypes FOREIGN KEY (InvestmentTypeId) REFERENCES Investment.InvestmentTypes (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Holdings_PortfolioId
        ON Investment.Holdings (PortfolioId, InvestmentTypeId, IsActive) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Holdings_Symbol
        ON Investment.Holdings (Symbol) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: Transactions (Investment-specific)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.Transactions', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.Transactions
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        HoldingId           BIGINT                          NOT NULL,
        TransactionType     NVARCHAR(30)                    NOT NULL,  -- Buy, Sell, Dividend, SIP, Switch, STT, StampDuty
        Quantity            DECIMAL(18,4)                   NOT NULL,
        PricePerUnit        DECIMAL(18,4)                   NOT NULL,
        TotalAmount         DECIMAL(18,2)                   NOT NULL,
        Charges             DECIMAL(18,2)                   NOT NULL DEFAULT 0,  -- Brokerage, STT, etc.
        STT                 DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        StampDuty           DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CostBasis           DECIMAL(18,2)                   NULL,
        RealizedGain        DECIMAL(18,2)                   NULL,
        TransactionDate     DATE                            NOT NULL,
        SettlementDate      DATE                            NULL,
        SIPId               BIGINT                          NULL,      -- Link to SIP schedule
        Notes               NVARCHAR(500)                   NULL,
        SourceAccount        BIGINT                          NULL,      -- Core.Accounts.Id
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_InvestmentTransactions PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_InvestmentTransactions_Holdings FOREIGN KEY (HoldingId) REFERENCES Investment.Holdings (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_InvestmentTransactions_HoldingId
        ON Investment.Transactions (HoldingId, TransactionDate DESC) ON FinOS_Index;
END
GO

IF COL_LENGTH(N'Investment.Transactions', N'CostBasis') IS NULL
    ALTER TABLE Investment.Transactions ADD CostBasis DECIMAL(18,2) NULL;
GO
IF COL_LENGTH(N'Investment.Transactions', N'RealizedGain') IS NULL
    ALTER TABLE Investment.Transactions ADD RealizedGain DECIMAL(18,2) NULL;
GO

-- ---------------------------------------------------------------------------
-- Table: SIPs (Systematic Investment Plans)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.SIPs', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.SIPs
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        HoldingId           BIGINT                          NULL,      -- Created holding once first SIP executes
        Amount              DECIMAL(18,2)                   NOT NULL,
        Frequency           NVARCHAR(20)                    NOT NULL DEFAULT N'Monthly',
        DayOfMonth          INT                             NOT NULL DEFAULT 1,
        StartDate           DATE                            NOT NULL,
        EndDate             DATE                            NULL,
        NextExecutionDate   DATE                            NOT NULL,
        LastExecutedDate    DATE                            NULL,
        SourceAccountId     BIGINT                          NOT NULL,
        IsActive            BIT                             NOT NULL DEFAULT 1,
        TotalInvested       DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        InstallmentsDone    INT                             NOT NULL DEFAULT 0,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_SIPs PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_SIPs_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SIPs_Holdings FOREIGN KEY (HoldingId) REFERENCES Investment.Holdings (Id),
        CONSTRAINT FK_SIPs_Accounts FOREIGN KEY (SourceAccountId) REFERENCES Core.Accounts (Id)
    );

    CREATE NONCLUSTERED INDEX IX_SIPs_NextExecution
        ON Investment.SIPs (NextExecutionDate, IsActive) WHERE IsActive = 1 ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: EPFAccounts (Employee Provident Fund)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.EPFAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.EPFAccounts
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        UAN                     NVARCHAR(20)                    NULL,     -- Universal Account Number
        EstablishmentCode       NVARCHAR(20)                    NULL,
        EmployerName            NVARCHAR(300)                   NULL,
        EmployeeContributionPct DECIMAL(5,2)                    NOT NULL DEFAULT 12.00,
        EmployerContributionPct DECIMAL(5,2)                    NOT NULL DEFAULT 3.67,  -- After EPS
        EPSCorpus               DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CurrentBalance          DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        MonthlySalary           DECIMAL(18,2)                   NULL,     -- For projection
        InterestRate            DECIMAL(8,4)                    NOT NULL DEFAULT 8.25, -- Current EPF rate
        StartDate               DATE                            NOT NULL,
        IsActive                BIT                             NOT NULL DEFAULT 1,
        LastUpdatedFromAPI      DATETIME2                       NULL,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_EPFAccounts PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_EPFAccounts_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: EPFContributions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.EPFContributions', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.EPFContributions
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        EPFAccountId            BIGINT                          NOT NULL,
        Month                   DATE                            NOT NULL,  -- First day of the month
        EmployeeContribution    DECIMAL(18,2)                   NOT NULL,
        EmployerContribution    DECIMAL(18,2)                   NOT NULL,
        EPSContribution         DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        InterestEarned          DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        OpeningBalance          DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        ClosingBalance          DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_EPFContributions PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_EPFContributions_EPFAccounts FOREIGN KEY (EPFAccountId) REFERENCES Investment.EPFAccounts (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_EPFContributions_Account_Month
        ON Investment.EPFContributions (EPFAccountId, Month DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: GoldPriceHistory
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Investment.GoldPriceHistory', N'U') IS NULL
BEGIN
    CREATE TABLE Investment.GoldPriceHistory
    (
        Id          BIGINT          IDENTITY(1,1)   NOT NULL,
        PriceDate   DATE                            NOT NULL,
        GoldType    NVARCHAR(20)                    NOT NULL DEFAULT N'24K',  -- 22K, 24K
        PricePer10g DECIMAL(18,2)                   NOT NULL,   -- INR per 10 grams
        CreatedAt   DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_GoldPriceHistory PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_GoldPrice_Date_Type UNIQUE NONCLUSTERED (PriceDate, GoldType) ON FinOS_Index
    );
END
GO

PRINT 'Investment schema created successfully.';
GO
