-- ============================================================================
-- FinOS Database - AI Assistant, Notifications & Subscriptions Schema
-- Target: Microsoft SQL Server (SSMS)
-- Description: Tables for AI queries, notifications, subscriptions, and imports
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: AI
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'AI')
    EXEC('CREATE SCHEMA AI');
GO

-- ---------------------------------------------------------------------------
-- Table: AIConversations
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'AI.AIConversations', N'U') IS NULL
BEGIN
    CREATE TABLE AI.AIConversations
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT                          NOT NULL,
        Title               NVARCHAR(200)                   NULL,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AIConversations PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_AIConversations_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_AIConversations_UserId
        ON AI.AIConversations (UserId, UpdatedAt DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: AIMessages
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'AI.AIMessages', N'U') IS NULL
BEGIN
    CREATE TABLE AI.AIMessages
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        ConversationId      BIGINT                          NOT NULL,
        Role                NVARCHAR(20)                    NOT NULL,  -- User, Assistant, System
        Content             NVARCHAR(MAX)                   NOT NULL,
        QueryType           NVARCHAR(50)                    NULL,      -- Affordability, SpendingAnalysis, LoanPrepayment, General
        ReferencedEntityIds NVARCHAR(MAX)                   NULL,      -- JSON - entities referenced in response
        TokenCount          INT                             NULL,
        ResponseTimeMs      INT                             NULL,      -- AI response latency
        FeedbackRating      TINYINT                         NULL,      -- 1-5 thumbs up/down
        FeedbackComment     NVARCHAR(500)                   NULL,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AIMessages PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_AIMessages_AIConversations FOREIGN KEY (ConversationId) REFERENCES AI.AIConversations (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_AIMessages_ConversationId
        ON AI.AIMessages (ConversationId, CreatedAt) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Schema: Notifications
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Notifications')
    EXEC('CREATE SCHEMA Notifications');
GO

-- ---------------------------------------------------------------------------
-- Table: NotificationTypes (reference)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Notifications.NotificationTypes', N'U') IS NULL
BEGIN
    CREATE TABLE Notifications.NotificationTypes
    (
        Id              INT             IDENTITY(1,1)   NOT NULL,
        Name            NVARCHAR(100)                   NOT NULL,
        Description     NVARCHAR(500)                   NULL,
        Category        NVARCHAR(50)                    NOT NULL, -- Security, Budget, Loan, Investment, Goal, System
        IsEnabled       BIT                             NOT NULL DEFAULT 1,

        CONSTRAINT PK_NotificationTypes PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_NotificationTypes_Name UNIQUE NONCLUSTERED (Name) ON FinOS_Index
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: NotificationPreferences (per user)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Notifications.NotificationPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE Notifications.NotificationPreferences
    (
        Id                      BIGINT      IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                      NOT NULL,
        NotificationTypeId      INT                         NOT NULL,
        EmailEnabled            BIT                         NOT NULL DEFAULT 1,
        PushEnabled             BIT                         NOT NULL DEFAULT 1,
        SmsEnabled              BIT                         NOT NULL DEFAULT 0,
        InAppEnabled            BIT                         NOT NULL DEFAULT 1,
        QuietHoursStart         TIME                        NULL,
        QuietHoursEnd           TIME                        NULL,
        CreatedAt               DATETIME2                   NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2                   NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_NotificationPreferences PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_NotificationPrefs_User_Type UNIQUE NONCLUSTERED (UserId, NotificationTypeId) ON FinOS_Index,
        CONSTRAINT FK_NotificationPrefs_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_NotificationPrefs_Types FOREIGN KEY (NotificationTypeId) REFERENCES Notifications.NotificationTypes (Id)
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: Notifications
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Notifications.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE Notifications.Notifications
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        NotificationTypeId      INT                             NOT NULL,
        Title                   NVARCHAR(200)                   NOT NULL,
        Message                 NVARCHAR(1000)                  NOT NULL,
        DeepLink                NVARCHAR(512)                   NULL,
        EntityType              NVARCHAR(50)                    NULL,
        EntityId                NVARCHAR(256)                   NULL,
        IsRead                  BIT                             NOT NULL DEFAULT 0,
        ReadAt                  DATETIME2                       NULL,
        IsActionTaken           BIT                             NOT NULL DEFAULT 0,
        ActionTakenAt           DATETIME2                       NULL,
        ScheduledAt             DATETIME2                       NULL,  -- For scheduled notifications
        SentAt                  DATETIME2                       NULL,
        DeliveryChannel         NVARCHAR(30)                    NOT NULL DEFAULT N'InApp', -- InApp, Email, Push, SMS
        DeliveryStatus          NVARCHAR(20)                    NOT NULL DEFAULT N'Pending', -- Pending, Sent, Delivered, Failed
        ExpiresAt               DATETIME2                       NULL,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Notifications_Types FOREIGN KEY (NotificationTypeId) REFERENCES Notifications.NotificationTypes (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Notifications_UserId_Unread
        ON Notifications.Notifications (UserId, IsRead, CreatedAt DESC) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Notifications_Scheduled
        ON Notifications.Notifications (ScheduledAt, DeliveryStatus) WHERE DeliveryStatus = N'Pending' ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Schema: Subscriptions
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Subscriptions')
    EXEC('CREATE SCHEMA Subscriptions');
GO

-- ---------------------------------------------------------------------------
-- Table: DetectedSubscriptions
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Subscriptions.DetectedSubscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE Subscriptions.DetectedSubscriptions
    (
        Id                      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId                  BIGINT                          NOT NULL,
        MerchantName            NVARCHAR(200)                   NOT NULL,
        CategoryId              BIGINT                          NULL,
        Amount                  DECIMAL(18,2)                   NOT NULL,
        Currency                NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        Frequency               NVARCHAR(20)                    NOT NULL, -- Monthly, Yearly, Weekly
        NextExpectedDate        DATE                            NULL,
        LastTransactionDate     DATE                            NOT NULL,
        LastTransactionId       BIGINT                          NOT NULL,
        DetectionConfidence     DECIMAL(5,2)                    NOT NULL, -- 0-100 confidence
        TransactionCount        INT                             NOT NULL DEFAULT 1,
        IsConfirmed             BIT                             NOT NULL DEFAULT 0,
        IsActive                BIT                             NOT NULL DEFAULT 1,
        CreatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_DetectedSubscriptions PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_DetectedSubscriptions_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_DetectedSubscriptions_Categories FOREIGN KEY (CategoryId) REFERENCES Core.Categories (Id)
    );

    CREATE NONCLUSTERED INDEX IX_DetectedSubscriptions_UserId
        ON Subscriptions.DetectedSubscriptions (UserId, IsActive) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Schema: Import
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Import')
    EXEC('CREATE SCHEMA Import');
GO

-- ---------------------------------------------------------------------------
-- Table: ImportBatches
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Import.ImportBatches', N'U') IS NULL
BEGIN
    CREATE TABLE Import.ImportBatches
    (
        Id                  UNIQUEIDENTIFIER    DEFAULT NEWID()   NOT NULL,
        UserId              BIGINT                                NOT NULL,
        Source              NVARCHAR(50)                          NOT NULL,  -- CSV, Excel, BankStatement, PDF
        FileName            NVARCHAR(500)                         NULL,
        TotalRows           INT                                   NOT NULL DEFAULT 0,
        ProcessedRows       INT                                   NOT NULL DEFAULT 0,
        SuccessRows         INT                                   NOT NULL DEFAULT 0,
        FailedRows          INT                                   NOT NULL DEFAULT 0,
        DuplicateRows       INT                                   NOT NULL DEFAULT 0,
        Status              NVARCHAR(20)                          NOT NULL DEFAULT N'Pending', -- Pending, Processing, Completed, Failed
        ErrorMessage        NVARCHAR(MAX)                         NULL,
        ColumnMapping       NVARCHAR(MAX)                         NULL,  -- JSON mapping
        StartedAt           DATETIME2                             NULL,
        CompletedAt         DATETIME2                             NULL,
        CreatedAt           DATETIME2                             NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_ImportBatches PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_ImportBatches_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ImportBatches_UserId
        ON Import.ImportBatches (UserId, CreatedAt DESC) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: ImportErrors
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Import.ImportErrors', N'U') IS NULL
BEGIN
    CREATE TABLE Import.ImportErrors
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        BatchId             UNIQUEIDENTIFIER                NOT NULL,
        RowNumber           INT                             NOT NULL,
        RawData             NVARCHAR(MAX)                   NULL,
        ErrorReason         NVARCHAR(500)                   NOT NULL,
        IsResolved          BIT                             NOT NULL DEFAULT 0,
        ResolvedTransactionId BIGINT                        NULL,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_ImportErrors PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_ImportErrors_Batches FOREIGN KEY (BatchId) REFERENCES Import.ImportBatches (Id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ImportErrors_ResolvedTransaction'
      AND parent_object_id = OBJECT_ID(N'Import.ImportErrors')
)
AND NOT EXISTS
(
    SELECT 1
    FROM Import.ImportErrors e
    LEFT JOIN Core.Transactions t ON t.Id = e.ResolvedTransactionId
    WHERE e.ResolvedTransactionId IS NOT NULL AND t.Id IS NULL
)
BEGIN
    ALTER TABLE Import.ImportErrors WITH CHECK
        ADD CONSTRAINT FK_ImportErrors_ResolvedTransaction
        FOREIGN KEY (ResolvedTransactionId) REFERENCES Core.Transactions (Id);
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ImportErrors_Batch_Resolution'
      AND object_id = OBJECT_ID(N'Import.ImportErrors')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ImportErrors_Batch_Resolution
        ON Import.ImportErrors (BatchId, IsResolved, CreatedAt DESC)
        INCLUDE (RowNumber, ResolvedTransactionId) ON FinOS_Index;
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Transactions_ImportBatches'
      AND parent_object_id = OBJECT_ID(N'Core.Transactions')
)
AND NOT EXISTS
(
    SELECT 1
    FROM Core.Transactions t
    LEFT JOIN Import.ImportBatches b ON b.Id = t.ImportBatchId
    WHERE t.ImportBatchId IS NOT NULL AND b.Id IS NULL
)
BEGIN
    ALTER TABLE Core.Transactions WITH CHECK
        ADD CONSTRAINT FK_Transactions_ImportBatches
        FOREIGN KEY (ImportBatchId) REFERENCES Import.ImportBatches (Id);
END
GO

PRINT 'AI, Notifications, Subscriptions & Import schema created successfully.';
GO
