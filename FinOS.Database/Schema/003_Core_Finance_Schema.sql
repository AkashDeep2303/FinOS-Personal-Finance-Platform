-- ============================================================================
-- FinOS Database - Core Finance Schema
-- Target: Microsoft SQL Server (SSMS)
-- Description: Tables for accounts, categories, transactions, and recurring items
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Core
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Core')
    EXEC('CREATE SCHEMA Core');
GO

-- ---------------------------------------------------------------------------
-- Table: AccountTypes (reference)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.AccountTypes', N'U') IS NULL
BEGIN
    CREATE TABLE Core.AccountTypes
    (
        Id          INT             IDENTITY(1,1)   NOT NULL,
        Name        NVARCHAR(50)                    NOT NULL,  -- Savings, Current, CreditCard, Loan, Investment, Cash, Wallet
        Icon        NVARCHAR(50)                    NULL,
        IsDefault   BIT                             NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AccountTypes PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_AccountTypes_Name UNIQUE NONCLUSTERED (Name) ON FinOS_Index
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: Accounts
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.Accounts', N'U') IS NULL
BEGIN
    CREATE TABLE Core.Accounts
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        AccountTypeId       INT                             NOT NULL,
        Name                NVARCHAR(100)                   NOT NULL,
        InstitutionName     NVARCHAR(200)                   NULL,      -- Bank name, broker name
        AccountNumber       NVARCHAR(50)                    NULL,      -- Masked/last 4 digits
        Balance             DECIMAL(18,2)                   NOT NULL DEFAULT 0,
        CreditLimit         DECIMAL(18,2)                   NULL,      -- For credit cards
        Currency            NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        Color               NVARCHAR(7)                     NULL,      -- Hex color
        Icon                NVARCHAR(50)                    NULL,
        IsIncludedInNetWorth BIT                            NOT NULL DEFAULT 1,
        IsSynced            BIT                             NOT NULL DEFAULT 0,  -- Bank integration
        SyncProvider        NVARCHAR(100)                   NULL,      -- Plaid, Yodlee, etc.
        SyncAccountId       NVARCHAR(256)                   NULL,
        LastSyncedAt        DATETIME2                       NULL,
        Notes               NVARCHAR(500)                   NULL,
        IsActive            BIT                             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt           DATETIME2                       NULL,

        CONSTRAINT PK_Accounts PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Accounts_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Accounts_AccountTypes FOREIGN KEY (AccountTypeId) REFERENCES Core.AccountTypes (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Accounts_UserId
        ON Core.Accounts (UserId, IsActive) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Accounts_SyncProvider
        ON Core.Accounts (SyncProvider, SyncAccountId) WHERE SyncProvider IS NOT NULL ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: Categories
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE Core.Categories
    (
        Id              BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId          BIGINT                          NULL,      -- NULL = system category
        ParentId        BIGINT                          NULL,
        Name            NVARCHAR(100)                   NOT NULL,
        Type            NVARCHAR(20)                    NOT NULL,  -- Income, Expense, Transfer
        Icon            NVARCHAR(50)                    NULL,
        Color           NVARCHAR(7)                     NULL,
        BudgetAmount    DECIMAL(18,2)                   NULL,
        IsSystem        BIT                             NOT NULL DEFAULT 0,
        IsActive        BIT                             NOT NULL DEFAULT 1,
        SortOrder       INT                             NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId) REFERENCES Core.Categories (Id),
        CONSTRAINT FK_Categories_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_Categories_UserId
        ON Core.Categories (UserId, Type, IsActive) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Categories_System
        ON Core.Categories (IsSystem, Type) WHERE IsSystem = 1 ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: Tags
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.Tags', N'U') IS NULL
BEGIN
    CREATE TABLE Core.Tags
    (
        Id          BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId      BIGINT                          NOT NULL,
        Name        NVARCHAR(50)                    NOT NULL,
        Color       NVARCHAR(7)                     NULL,
        CreatedAt   DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Tags PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_Tags_User_Name UNIQUE NONCLUSTERED (UserId, Name) ON FinOS_Index,
        CONSTRAINT FK_Tags_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: Transactions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.Transactions', N'U') IS NULL
BEGIN
    CREATE TABLE Core.Transactions
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        AccountId           BIGINT                          NOT NULL,
        CategoryId          BIGINT                          NULL,
        TransferAccountId   BIGINT                          NULL,      -- For transfers
        Type                NVARCHAR(20)                    NOT NULL,  -- Income, Expense, Transfer
        Amount              DECIMAL(18,2)                   NOT NULL,
        Currency            NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        ExchangeRate        DECIMAL(18,6)                   NULL,
        OriginalAmount      DECIMAL(18,2)                   NULL,
        OriginalCurrency    NVARCHAR(3)                     NULL,
        Description         NVARCHAR(500)                   NOT NULL,
        Notes               NVARCHAR(1000)                  NULL,
        TransactionDate     DATE                            NOT NULL,
        TransactionTime     TIME                            NULL,
        ValueDate           DATE                            NULL,      -- Bank value date
        ReferenceNumber     NVARCHAR(100)                   NULL,
        MerchantName        NVARCHAR(200)                   NULL,
        MerchantCategory    NVARCHAR(100)                   NULL,
        IsRecurring         BIT                             NOT NULL DEFAULT 0,
        RecurringScheduleId BIGINT                          NULL,
        IsFlagged           BIT                             NOT NULL DEFAULT 0,
        IsSplit             BIT                             NOT NULL DEFAULT 0,
        ParentTransactionId BIGINT                          NULL,      -- For split transactions
        SplitNote           NVARCHAR(200)                   NULL,
        AttachmentUrls      NVARCHAR(MAX)                   NULL,      -- JSON array of URLs
        LocationLat         DECIMAL(10,7)                   NULL,
        LocationLng         DECIMAL(10,7)                   NULL,
        LocationName        NVARCHAR(200)                   NULL,
        Source              NVARCHAR(50)                    NOT NULL DEFAULT N'Manual', -- Manual, Import, Sync, AI
        ImportBatchId       UNIQUEIDENTIFIER                NULL,
        IsVerified          BIT                             NOT NULL DEFAULT 1,
        VerifiedAt          DATETIME2                       NULL,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt           DATETIME2                       NULL,

        CONSTRAINT PK_Transactions PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Transactions_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Transactions_Accounts FOREIGN KEY (AccountId) REFERENCES Core.Accounts (Id),
        CONSTRAINT FK_Transactions_Categories FOREIGN KEY (CategoryId) REFERENCES Core.Categories (Id),
        CONSTRAINT FK_Transactions_TransferAccount FOREIGN KEY (TransferAccountId) REFERENCES Core.Accounts (Id),
        CONSTRAINT FK_Transactions_Parent FOREIGN KEY (ParentTransactionId) REFERENCES Core.Transactions (Id)
    );

    -- High-usage indexes for transactions
    CREATE NONCLUSTERED INDEX IX_Transactions_UserId_Date
        ON Core.Transactions (UserId, TransactionDate DESC) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Transactions_UserId_Type
        ON Core.Transactions (UserId, Type, TransactionDate DESC) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Transactions_UserId_Account
        ON Core.Transactions (UserId, AccountId, TransactionDate DESC) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Transactions_UserId_Category
        ON Core.Transactions (UserId, CategoryId) WHERE CategoryId IS NOT NULL ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Transactions_UserId_Recurring
        ON Core.Transactions (UserId, IsRecurring) WHERE IsRecurring = 1 ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Transactions_Merchant
        ON Core.Transactions (UserId, MerchantName) WHERE MerchantName IS NOT NULL ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Transactions_ImportBatch
        ON Core.Transactions (ImportBatchId) WHERE ImportBatchId IS NOT NULL ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Transactions_CreatedAt
        ON Core.Transactions (CreatedAt) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: TransactionTags (junction)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.TransactionTags', N'U') IS NULL
BEGIN
    CREATE TABLE Core.TransactionTags
    (
        TransactionId   BIGINT  NOT NULL,
        TagId           BIGINT  NOT NULL,

        CONSTRAINT PK_TransactionTags PRIMARY KEY CLUSTERED (TransactionId, TagId) ON FinOS_Data,
        CONSTRAINT FK_TransactionTags_Transactions FOREIGN KEY (TransactionId) REFERENCES Core.Transactions (Id) ON DELETE CASCADE,
        CONSTRAINT FK_TransactionTags_Tags FOREIGN KEY (TagId) REFERENCES Core.Tags (Id) ON DELETE NO ACTION
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: RecurringSchedules
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.RecurringSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE Core.RecurringSchedules
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        AccountId           BIGINT                          NOT NULL,
        CategoryId          BIGINT                          NULL,
        Type                NVARCHAR(20)                    NOT NULL,
        Amount              DECIMAL(18,2)                   NOT NULL,
        Description         NVARCHAR(500)                   NOT NULL,
        Frequency           NVARCHAR(20)                    NOT NULL,  -- Daily, Weekly, BiWeekly, Monthly, Quarterly, Yearly
        IntervalValue       INT                             NOT NULL DEFAULT 1,
        DayOfMonth          INT                             NULL,      -- For monthly
        DayOfWeek           INT                             NULL,      -- For weekly (0=Sun, 6=Sat)
        StartDate           DATE                            NOT NULL,
        EndDate             DATE                            NULL,
        NextOccurrenceDate  DATE                            NOT NULL,
        LastProcessedDate   DATE                            NULL,
        IsActive            BIT                             NOT NULL DEFAULT 1,
        AutoCreate          BIT                             NOT NULL DEFAULT 0, -- Auto-add transaction
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_RecurringSchedules PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_RecurringSchedules_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RecurringSchedules_Accounts FOREIGN KEY (AccountId) REFERENCES Core.Accounts (Id)
    );

    CREATE NONCLUSTERED INDEX IX_RecurringSchedules_NextDate
        ON Core.RecurringSchedules (NextOccurrenceDate, IsActive) WHERE IsActive = 1 ON FinOS_Index;
END
GO

PRINT 'Core Finance schema created successfully.';
GO
