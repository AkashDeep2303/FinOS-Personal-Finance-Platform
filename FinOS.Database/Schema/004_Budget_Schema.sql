-- ============================================================================
-- FinOS Database - Budget Schema
-- Target: Microsoft SQL Server (SSMS)
-- Description: Tables for budgets, budget categories, and budget tracking
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Budget
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Budget')
    EXEC('CREATE SCHEMA Budget');
GO

-- ---------------------------------------------------------------------------
-- Table: Budgets
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.Budgets', N'U') IS NULL
BEGIN
    CREATE TABLE Budget.Budgets
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        Name                NVARCHAR(100)                   NOT NULL,
        PeriodType          NVARCHAR(20)                    NOT NULL,  -- Weekly, Monthly, Quarterly, Yearly
        StartDate           DATE                            NOT NULL,
        EndDate             DATE                            NOT NULL,
        TotalBudgetAmount   DECIMAL(18,2)                   NOT NULL,
        Currency            NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        RolloverEnabled     BIT                             NOT NULL DEFAULT 0, -- Carry over unspent
        AlertThresholdPct   DECIMAL(5,2)                    NOT NULL DEFAULT 80.00,
        IsTemplate          BIT                             NOT NULL DEFAULT 0,
        IsActive            BIT                             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt           DATETIME2                       NULL,

        CONSTRAINT PK_Budgets PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Budgets_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_Budgets_UserId
        ON Budget.Budgets (UserId, IsActive, StartDate DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: BudgetCategories
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.BudgetCategories', N'U') IS NULL
BEGIN
    CREATE TABLE Budget.BudgetCategories
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        BudgetId            BIGINT                          NOT NULL,
        CategoryId          BIGINT                          NULL,      -- Links to Core.Categories
        CustomLabel         NVARCHAR(100)                   NULL,      -- If no specific category
        AllocatedAmount     DECIMAL(18,2)                   NOT NULL,
        SpentAmount         DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        AlertThresholdPct   DECIMAL(5,2)                    NULL,      -- Override budget-level alert
        SortOrder           INT                             NOT NULL DEFAULT 0,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_BudgetCategories PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_BudgetCategories_Budgets FOREIGN KEY (BudgetId) REFERENCES Budget.Budgets (Id) ON DELETE CASCADE,
        CONSTRAINT FK_BudgetCategories_Categories FOREIGN KEY (CategoryId) REFERENCES Core.Categories (Id)
    );

    CREATE NONCLUSTERED INDEX IX_BudgetCategories_BudgetId
        ON Budget.BudgetCategories (BudgetId) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: BudgetAlerts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.BudgetAlerts', N'U') IS NULL
BEGIN
    CREATE TABLE Budget.BudgetAlerts
    (
        Id                      BIGINT      IDENTITY(1,1)   NOT NULL,
        BudgetCategoryId        BIGINT                      NOT NULL,
        AlertType               NVARCHAR(30)                NOT NULL,  -- Threshold, Overspent, NearEnd
        ThresholdPercentage     DECIMAL(5,2)                NULL,
        Message                 NVARCHAR(500)               NOT NULL,
        IsRead                  BIT                         NOT NULL DEFAULT 0,
        CreatedAt               DATETIME2                   NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_BudgetAlerts PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_BudgetAlerts_BudgetCategories FOREIGN KEY (BudgetCategoryId) REFERENCES Budget.BudgetCategories (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_BudgetAlerts_Unread
        ON Budget.BudgetAlerts (BudgetCategoryId, IsRead) WHERE IsRead = 0 ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: SavingsRules (round-up, auto-save)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Budget.SavingsRules', N'U') IS NULL
BEGIN
    CREATE TABLE Budget.SavingsRules
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        RuleType            NVARCHAR(30)                    NOT NULL,  -- RoundUp, Percentage, FixedAmount, FiftyThirtyTwenty
        Name                NVARCHAR(100)                   NOT NULL,
        TargetAccountId     BIGINT                          NOT NULL,  -- Where to save
        SourceAccountId     BIGINT                          NULL,      -- From which account
        RoundUpTo           INT                             NULL,      -- e.g., 10, 50, 100 for round-up
        Percentage          DECIMAL(5,2)                    NULL,      -- For percentage-based rules
        FixedAmount         DECIMAL(18,2)                   NULL,      -- For fixed amount rules
        Frequency           NVARCHAR(20)                    NULL,      -- Daily, Weekly, Monthly
        DayOfMonth          INT                             NULL,
        IsActive            BIT                             NOT NULL DEFAULT 1,
        TotalSaved          DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_SavingsRules PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_SavingsRules_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_SavingsRules_TargetAccount FOREIGN KEY (TargetAccountId) REFERENCES Core.Accounts (Id),
        CONSTRAINT FK_SavingsRules_SourceAccount FOREIGN KEY (SourceAccountId) REFERENCES Core.Accounts (Id)
    );
END
GO

PRINT 'Budget schema created successfully.';
GO
