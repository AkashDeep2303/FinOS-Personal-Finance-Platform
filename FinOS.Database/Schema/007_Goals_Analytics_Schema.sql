-- ============================================================================
-- FinOS Database - Goals & Analytics Schema
-- Target: Microsoft SQL Server (SSMS)
-- Description: Tables for financial goals, net worth snapshots, and financial score
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Goals
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Goals')
    EXEC('CREATE SCHEMA Goals');
GO

-- ---------------------------------------------------------------------------
-- Table: GoalTemplates (system-provided)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Goals.GoalTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE Goals.GoalTemplates
    (
        Id                  INT             IDENTITY(1,1)   NOT NULL,
        Name                NVARCHAR(100)                   NOT NULL,
        Description         NVARCHAR(500)                   NOT NULL,
        Category            NVARCHAR(50)                    NOT NULL,  -- Emergency, Retirement, Travel, Purchase, Education, Wedding
        SuggestedAmount     DECIMAL(18,2)                   NULL,
        SuggestedMonths     INT                             NULL,
        Icon                NVARCHAR(50)                    NULL,
        Color               NVARCHAR(7)                     NULL,
        SortOrder           INT                             NOT NULL DEFAULT 0,

        CONSTRAINT PK_GoalTemplates PRIMARY KEY CLUSTERED (Id) ON FinOS_Data
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: Goals
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Goals.Goals', N'U') IS NULL
BEGIN
    CREATE TABLE Goals.Goals
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        GoalTemplateId      INT                             NULL,
        Name                NVARCHAR(100)                   NOT NULL,
        Description         NVARCHAR(500)                   NULL,
        Category            NVARCHAR(50)                    NOT NULL,
        TargetAmount        DECIMAL(18,2)                   NOT NULL,
        CurrentAmount       DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        MonthlyContribution DECIMAL(18,2)                   NULL,
        StartDate           DATE                            NOT NULL,
        TargetDate          DATE                            NOT NULL,
        CompletedDate       DATE                            NULL,
        Priority            NVARCHAR(10)                    NOT NULL DEFAULT N'Medium', -- Low, Medium, High, Critical
        Status              NVARCHAR(20)                    NOT NULL DEFAULT N'InProgress', -- InProgress, Completed, Paused, Abandoned
        LinkedAccountIds    NVARCHAR(MAX)                   NULL,      -- JSON array of account IDs
        Icon                NVARCHAR(50)                    NULL,
        Color               NVARCHAR(7)                     NULL,
        IsAutoContribute    BIT                             NOT NULL DEFAULT 0,
        ProjectedDate       DATE                            NULL,      -- When it'll actually be reached
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt           DATETIME2                       NULL,

        CONSTRAINT PK_Goals PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Goals_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Goals_GoalTemplates FOREIGN KEY (GoalTemplateId) REFERENCES Goals.GoalTemplates (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Goals_UserId
        ON Goals.Goals (UserId, Status) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Goals_TargetDate
        ON Goals.Goals (UserId, TargetDate) WHERE Status = N'InProgress' ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: GoalContributions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Goals.GoalContributions', N'U') IS NULL
BEGIN
    CREATE TABLE Goals.GoalContributions
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        GoalId              BIGINT                          NOT NULL,
        Amount              DECIMAL(18,2)                   NOT NULL,
        ContributionDate    DATE                            NOT NULL,
        Source              NVARCHAR(30)                    NOT NULL DEFAULT N'Manual', -- Manual, AutoSave, Windfall
        SourceAccountId     BIGINT                          NULL,
        Notes               NVARCHAR(300)                   NULL,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_GoalContributions PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_GoalContributions_Goals FOREIGN KEY (GoalId) REFERENCES Goals.Goals (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_GoalContributions_GoalId
        ON Goals.GoalContributions (GoalId, ContributionDate DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Schema: Analytics
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Analytics')
    EXEC('CREATE SCHEMA Analytics');
GO

-- ---------------------------------------------------------------------------
-- Table: NetWorthSnapshots
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.NetWorthSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE Analytics.NetWorthSnapshots
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        SnapshotDate            DATE                            NOT NULL,
        TotalAssets             DECIMAL(18,2)                   NOT NULL,
        TotalLiabilities        DECIMAL(18,2)                   NOT NULL,
        NetWorth                DECIMAL(18,2)                   NOT NULL,
        CashAndBank             DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        InvestmentValue         DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        RealEstateValue         DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        GoldValue               DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        OtherAssets             DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        LoanOutstanding         DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CreditCardOutstanding   DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        OtherLiabilities        DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        ChangeFromPrevious      DECIMAL(18,2)                   NULL,
        ChangePctFromPrevious   DECIMAL(8,4)                    NULL,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_NetWorthSnapshots PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_NetWorth_User_Date UNIQUE NONCLUSTERED (UserId, SnapshotDate) ON FinOS_Index,
        CONSTRAINT FK_NetWorthSnapshots_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_NetWorth_UserId_Date
        ON Analytics.NetWorthSnapshots (UserId, SnapshotDate DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: FinancialScore
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.FinancialScore', N'U') IS NULL
BEGIN
    CREATE TABLE Analytics.FinancialScore
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        ScoreDate               DATE                            NOT NULL,
        OverallScore            INT                             NOT NULL,  -- 0-1000
        ScoreGrade              NVARCHAR(2)                     NOT NULL,  -- A+, A, B, C, D, E
        SavingsRateScore        INT                             NOT NULL,  -- Sub-score
        DebtToIncomeScore       INT                             NOT NULL,
        EmergencyFundScore      INT                             NOT NULL,
        InvestmentScore         INT                             NOT NULL,
        GoalProgressScore       INT                             NOT NULL,
        SavingsRatePct          DECIMAL(5,2)                    NULL,
        DebtToIncomeRatio       DECIMAL(5,2)                    NULL,
        EmergencyFundMonths     DECIMAL(5,2)                    NULL,
        InvestmentToIncomeRatio DECIMAL(5,2)                    NULL,
        MonthlyIncome           DECIMAL(18,2)                   NULL,
        MonthlyExpenses         DECIMAL(18,2)                   NULL,
        MonthlySavings          DECIMAL(18,2)                   NULL,
        TotalDebt               DECIMAL(18,2)                   NULL,
        TotalInvestments        DECIMAL(18,2)                   NULL,
        Recommendations         NVARCHAR(MAX)                   NULL,  -- JSON
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_FinancialScore PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_FinancialScore_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_FinancialScore_UserId
        ON Analytics.FinancialScore (UserId, ScoreDate DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: MonthlyAggregates
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Analytics.MonthlyAggregates', N'U') IS NULL
BEGIN
    CREATE TABLE Analytics.MonthlyAggregates
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        YearMonth               INT                             NOT NULL,  -- 202601 for Jan 2026
        TotalIncome             DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        TotalExpense            DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        TotalSavings            DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        SavingsRate             DECIMAL(5,2)                    NULL,
        TopExpenseCategory      NVARCHAR(100)                   NULL,
        TopExpenseAmount        DECIMAL(18,2)                   NULL,
        TransactionCount        INT                             NOT NULL DEFAULT 0,
        CategoryBreakdown       NVARCHAR(MAX)                   NULL,  -- JSON
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_MonthlyAggregates PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_MonthlyAggregates_User_YearMonth UNIQUE NONCLUSTERED (UserId, YearMonth) ON FinOS_Index,
        CONSTRAINT FK_MonthlyAggregates_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );
END
GO

PRINT 'Goals & Analytics schema created successfully.';
GO
