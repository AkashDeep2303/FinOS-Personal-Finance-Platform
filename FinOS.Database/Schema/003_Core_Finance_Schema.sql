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

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Tax')
    EXEC('CREATE SCHEMA Tax');
GO

IF OBJECT_ID(N'Tax.RuleVersions', N'U') IS NULL
BEGIN
    CREATE TABLE Tax.RuleVersions
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        FinancialYear NVARCHAR(9) NOT NULL,
        AssessmentYear NVARCHAR(9) NOT NULL,
        Regime NVARCHAR(20) NOT NULL,
        Version NVARCHAR(30) NOT NULL,
        ConfigurationJson NVARCHAR(MAX) NOT NULL,
        EffectiveFrom DATE NOT NULL,
        EffectiveTo DATE NULL,
        IsPublished BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Tax_RuleVersions PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_Tax_RuleVersions UNIQUE NONCLUSTERED (FinancialYear, Regime, Version) ON FinOS_Index,
        CONSTRAINT CK_Tax_RuleVersions_ConfigJson CHECK (ISJSON(ConfigurationJson) = 1),
        CONSTRAINT CK_Tax_RuleVersions_Regime CHECK (Regime IN (N'Old', N'New'))
    );
END
GO

IF OBJECT_ID(N'Tax.Profiles', N'U') IS NULL
BEGIN
    CREATE TABLE Tax.Profiles
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        UserId BIGINT NOT NULL,
        FinancialYear NVARCHAR(9) NOT NULL,
        PreferredRegime NVARCHAR(20) NULL,
        InputJson NVARCHAR(MAX) NOT NULL DEFAULT N'{}',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt DATETIME2 NULL,
        CONSTRAINT PK_Tax_Profiles PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Tax_Profiles_Users FOREIGN KEY (UserId) REFERENCES Security.Users(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_Tax_Profiles_User_FY UNIQUE NONCLUSTERED (UserId, FinancialYear) ON FinOS_Index,
        CONSTRAINT CK_Tax_Profiles_InputJson CHECK (ISJSON(InputJson) = 1),
        CONSTRAINT CK_Tax_Profiles_Regime CHECK (PreferredRegime IS NULL OR PreferredRegime IN (N'Old', N'New'))
    );
END
GO

IF OBJECT_ID(N'Tax.Projections', N'U') IS NULL
BEGIN
    CREATE TABLE Tax.Projections
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        UserId BIGINT NOT NULL,
        TaxProfileId BIGINT NOT NULL,
        TaxRuleVersionId BIGINT NOT NULL,
        GrossIncome DECIMAL(18,2) NOT NULL,
        TaxableIncome DECIMAL(18,2) NOT NULL,
        EstimatedTax DECIMAL(18,2) NOT NULL,
        TaxesPaid DECIMAL(18,2) NOT NULL DEFAULT 0,
        EstimatedPayableOrRefund DECIMAL(18,2) NOT NULL,
        CalculationJson NVARCHAR(MAX) NOT NULL,
        CalculatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Tax_Projections PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Tax_Projections_Users FOREIGN KEY (UserId) REFERENCES Security.Users(Id),
        CONSTRAINT FK_Tax_Projections_Profiles FOREIGN KEY (TaxProfileId) REFERENCES Tax.Profiles(Id),
        CONSTRAINT FK_Tax_Projections_Rules FOREIGN KEY (TaxRuleVersionId) REFERENCES Tax.RuleVersions(Id),
        CONSTRAINT CK_Tax_Projections_CalculationJson CHECK (ISJSON(CalculationJson) = 1)
    );
    CREATE NONCLUSTERED INDEX IX_Tax_Projections_User_Calculated
        ON Tax.Projections(UserId, CalculatedAt DESC) ON FinOS_Index;
END
GO

IF OBJECT_ID(N'Core.InsurancePolicies', N'U') IS NULL
BEGIN
    CREATE TABLE Core.InsurancePolicies
    (
        Id BIGINT IDENTITY(1,1) NOT NULL, UserId BIGINT NOT NULL,
        PolicyType NVARCHAR(30) NOT NULL, Provider NVARCHAR(100) NOT NULL,
        PolicyNumber NVARCHAR(100) NULL, CoverageAmount DECIMAL(18,2) NOT NULL,
        PremiumAmount DECIMAL(18,2) NOT NULL, PremiumFrequency NVARCHAR(20) NOT NULL,
        StartDate DATE NULL, EndDate DATE NULL, RenewalDate DATE NULL,
        Nominee NVARCHAR(100) NULL, Notes NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT N'Active',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), DeletedAt DATETIME2 NULL,
        CONSTRAINT PK_Core_InsurancePolicies PRIMARY KEY CLUSTERED(Id) ON FinOS_Data,
        CONSTRAINT FK_Core_InsurancePolicies_Users FOREIGN KEY(UserId) REFERENCES Security.Users(Id) ON DELETE CASCADE,
        CONSTRAINT CK_Core_InsurancePolicies_Type CHECK(PolicyType IN(N'Life',N'Health',N'Vehicle',N'Property',N'Other'))
    );
    CREATE NONCLUSTERED INDEX IX_Core_InsurancePolicies_User ON Core.InsurancePolicies(UserId,Status) WHERE DeletedAt IS NULL ON FinOS_Index;
END
GO

IF OBJECT_ID(N'Core.Assets', N'U') IS NULL
BEGIN
    CREATE TABLE Core.Assets
    (
        Id BIGINT IDENTITY(1,1) NOT NULL, UserId BIGINT NOT NULL,
        AssetType NVARCHAR(30) NOT NULL, Name NVARCHAR(100) NOT NULL,
        PurchaseValue DECIMAL(18,2) NULL, PurchaseDate DATE NULL,
        CurrentEstimatedValue DECIMAL(18,2) NOT NULL, ValuationDate DATE NOT NULL,
        AssociatedLoanId BIGINT NULL, Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), DeletedAt DATETIME2 NULL,
        CONSTRAINT PK_Core_Assets PRIMARY KEY CLUSTERED(Id) ON FinOS_Data,
        CONSTRAINT FK_Core_Assets_Users FOREIGN KEY(UserId) REFERENCES Security.Users(Id) ON DELETE CASCADE,
        CONSTRAINT CK_Core_Assets_Type CHECK(AssetType IN(N'Property',N'Vehicle',N'Gold',N'Collectible',N'Business',N'Other'))
    );
    CREATE INDEX IX_Core_Assets_User ON Core.Assets(UserId,AssetType) WHERE DeletedAt IS NULL ON FinOS_Index;
END
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

IF OBJECT_ID(N'Core.CreditCardDetails', N'U') IS NULL
BEGIN
    CREATE TABLE Core.CreditCardDetails
    (
        AccountId BIGINT NOT NULL, UserId BIGINT NOT NULL,
        StatementDay TINYINT NULL, PaymentDueDay TINYINT NULL,
        MinimumAmountDue DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalAmountDue DECIMAL(18,2) NOT NULL DEFAULT 0,
        AnnualInterestRate DECIMAL(7,4) NOT NULL DEFAULT 0,
        LastPaymentDate DATE NULL, LastPaymentAmount DECIMAL(18,2) NULL,
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Core_CreditCardDetails PRIMARY KEY(AccountId),
        CONSTRAINT FK_Core_CreditCardDetails_Accounts FOREIGN KEY(AccountId) REFERENCES Core.Accounts(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Core_CreditCardDetails_Users FOREIGN KEY(UserId) REFERENCES Security.Users(Id),
        CONSTRAINT CK_Core_CreditCardDetails_Days CHECK((StatementDay IS NULL OR StatementDay BETWEEN 1 AND 31) AND (PaymentDueDay IS NULL OR PaymentDueDay BETWEEN 1 AND 31))
    );
    CREATE INDEX IX_Core_CreditCardDetails_User ON Core.CreditCardDetails(UserId) ON FinOS_Index;
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

IF COL_LENGTH(N'Core.Categories', N'CashFlowClassification') IS NULL
BEGIN
    ALTER TABLE Core.Categories
        ADD CashFlowClassification NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Categories_CashFlowClassification DEFAULT N'Other';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Categories_CashFlowClassification')
    ALTER TABLE Core.Categories ADD CONSTRAINT CK_Categories_CashFlowClassification
        CHECK (CashFlowClassification IN (N'Essential', N'Lifestyle', N'EMI', N'Investment', N'Other'));
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

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Transactions_User_Account_Reference'
      AND object_id = OBJECT_ID(N'Core.Transactions')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Transactions_User_Account_Reference
        ON Core.Transactions (UserId, AccountId, ReferenceNumber)
        INCLUDE (TransactionDate, Amount, Type)
        WHERE ReferenceNumber IS NOT NULL AND DeletedAt IS NULL ON FinOS_Index;
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

-- ---------------------------------------------------------------------------
-- Table: FinancialDocuments (metadata plus private-storage reference)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.FinancialDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE Core.FinancialDocuments
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        UserId          BIGINT NOT NULL,
        DocumentType    NVARCHAR(50) NOT NULL,
        Title           NVARCHAR(200) NOT NULL,
        Issuer          NVARCHAR(150) NULL,
        FinancialYear   NVARCHAR(7) NULL,
        DocumentDate    DATE NULL,
        Notes           NVARCHAR(500) NULL,
        Status          NVARCHAR(20) NOT NULL DEFAULT N'Recorded',
        CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt       DATETIME2 NULL,

        CONSTRAINT PK_FinancialDocuments PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_FinancialDocuments_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT CK_FinancialDocuments_Type CHECK (DocumentType IN (N'BankStatement', N'BrokerStatement', N'MutualFundStatement', N'SalarySlip', N'Form16', N'LoanStatement', N'EPF', N'Insurance', N'Tax', N'Other')),
        CONSTRAINT CK_FinancialDocuments_Status CHECK (Status IN (N'Recorded', N'Verified', N'Archived')),
        CONSTRAINT CK_FinancialDocuments_FY CHECK (FinancialYear IS NULL OR FinancialYear LIKE N'[1-2][0-9][0-9][0-9]-[0-9][0-9]')
    );

    CREATE NONCLUSTERED INDEX IX_FinancialDocuments_User_Date
        ON Core.FinancialDocuments (UserId, DocumentDate DESC, CreatedAt DESC)
        WHERE DeletedAt IS NULL ON FinOS_Index;
END
GO

IF COL_LENGTH(N'Core.FinancialDocuments', N'StorageKey') IS NULL
    ALTER TABLE Core.FinancialDocuments ADD StorageKey NVARCHAR(300) NULL;
IF COL_LENGTH(N'Core.FinancialDocuments', N'OriginalFileName') IS NULL
    ALTER TABLE Core.FinancialDocuments ADD OriginalFileName NVARCHAR(255) NULL;
IF COL_LENGTH(N'Core.FinancialDocuments', N'ContentType') IS NULL
    ALTER TABLE Core.FinancialDocuments ADD ContentType NVARCHAR(100) NULL;
IF COL_LENGTH(N'Core.FinancialDocuments', N'FileSizeBytes') IS NULL
    ALTER TABLE Core.FinancialDocuments ADD FileSizeBytes BIGINT NULL;
IF COL_LENGTH(N'Core.FinancialDocuments', N'Sha256') IS NULL
    ALTER TABLE Core.FinancialDocuments ADD Sha256 CHAR(64) NULL;
GO

-- ---------------------------------------------------------------------------
-- Table: DataSources (non-secret source registry)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Core.DataSources', N'U') IS NULL
BEGIN
    CREATE TABLE Core.DataSources
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        UserId          BIGINT NOT NULL,
        SourceType      NVARCHAR(30) NOT NULL,
        DisplayName     NVARCHAR(150) NOT NULL,
        InstitutionName NVARCHAR(150) NULL,
        ConnectionMode  NVARCHAR(20) NOT NULL DEFAULT N'ManualImport',
        Status          NVARCHAR(20) NOT NULL DEFAULT N'Active',
        LastImportedAt  DATETIME2 NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt       DATETIME2 NULL,

        CONSTRAINT PK_DataSources PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_DataSources_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT CK_DataSources_Type CHECK (SourceType IN (N'Bank', N'Broker', N'MutualFund', N'Salary', N'Tax', N'Loan', N'EPF', N'Other')),
        CONSTRAINT CK_DataSources_Mode CHECK (ConnectionMode IN (N'ManualImport')),
        CONSTRAINT CK_DataSources_Status CHECK (Status IN (N'Active', N'Paused'))
    );

    CREATE NONCLUSTERED INDEX IX_DataSources_User_Status
        ON Core.DataSources (UserId, Status, CreatedAt DESC)
        WHERE DeletedAt IS NULL ON FinOS_Index;
END
GO

PRINT 'Core Finance schema created successfully.';
GO
