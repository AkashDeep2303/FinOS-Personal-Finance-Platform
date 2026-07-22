-- ============================================================================
-- FinOS Database - Security & Auth Schema
-- Target: Microsoft SQL Server (SSMS)
-- Description: Tables for user management, authentication, roles, and audit
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- Schema: Security
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'Security')
    EXEC('CREATE SCHEMA Security');
GO

-- ---------------------------------------------------------------------------
-- Table: Users
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.Users', N'U') IS NULL
BEGIN
    CREATE TABLE Security.Users
    (
        Id                  BIGINT          IDENTITY(1,1)   NOT NULL,
        Email               NVARCHAR(256)                   NOT NULL,
        PasswordHash        NVARCHAR(512)                   NOT NULL,
        PasswordSalt        NVARCHAR(256)                   NOT NULL,
        FirstName           NVARCHAR(100)                   NOT NULL,
        LastName            NVARCHAR(100)                   NOT NULL,
        PhoneNumber         NVARCHAR(20)                    NULL,
        ProfileImageUrl     NVARCHAR(512)                   NULL,
        IsActive            BIT                             NOT NULL DEFAULT 1,
        EmailVerified       BIT                             NOT NULL DEFAULT 0,
        PhoneVerified       BIT                             NOT NULL DEFAULT 0,
        TwoFactorEnabled    BIT                             NOT NULL DEFAULT 0,
        TwoFactorSecret     NVARCHAR(256)                   NULL,
        LockoutEnd          DATETIME2                       NULL,
        LockoutEnabled      BIT                             NOT NULL DEFAULT 1,
        AccessFailedCount   INT                             NOT NULL DEFAULT 0,
        LastLoginAt         DATETIME2                       NULL,
        Currency            NVARCHAR(3)                     NOT NULL DEFAULT N'INR',
        TimeZone            NVARCHAR(50)                    NOT NULL DEFAULT N'Asia/Kolkata',
        Locale              NVARCHAR(10)                    NOT NULL DEFAULT N'en-IN',
        OAuthProvider       NVARCHAR(50)                    NULL,   -- Google, Microsoft, etc.
        OAuthProviderId     NVARCHAR(256)                   NULL,
        CreatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        DeletedAt           DATETIME2                       NULL,   -- Soft delete

        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_Users_Email UNIQUE NONCLUSTERED (Email) ON FinOS_Index
    );

    CREATE NONCLUSTERED INDEX IX_Users_OAuthProvider
        ON Security.Users (OAuthProvider, OAuthProviderId) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Users_PhoneNumber
        ON Security.Users (PhoneNumber) WHERE PhoneNumber IS NOT NULL ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_Users_Active
        ON Security.Users (IsActive) WHERE IsActive = 1 ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Add optional profile fields to existing installations as well as fresh databases.
IF COL_LENGTH(N'Security.Users', N'DateOfBirth') IS NULL
    ALTER TABLE Security.Users ADD DateOfBirth DATE NULL;
IF COL_LENGTH(N'Security.Users', N'Bio') IS NULL
    ALTER TABLE Security.Users ADD Bio NVARCHAR(2000) NULL;
GO

-- ---------------------------------------------------------------------------
-- Table: Roles
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE Security.Roles
    (
        Id          INT             IDENTITY(1,1)   NOT NULL,
        Name        NVARCHAR(100)                   NOT NULL,
        Description NVARCHAR(500)                   NULL,
        CreatedAt   DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT UQ_Roles_Name UNIQUE NONCLUSTERED (Name) ON FinOS_Index
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: UserRoles
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE Security.UserRoles
    (
        UserId      BIGINT      NOT NULL,
        RoleId      INT         NOT NULL,
        AssignedAt  DATETIME2   NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserId, RoleId) ON FinOS_Data,
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Security.Roles (Id) ON DELETE CASCADE
    );
END
GO

-- ---------------------------------------------------------------------------
-- Table: RefreshTokens
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE Security.RefreshTokens
    (
        Id              BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId          BIGINT                          NOT NULL,
        Token           NVARCHAR(512)                   NOT NULL,
        JwtId           NVARCHAR(256)                   NOT NULL,
        IsRevoked       BIT                             NOT NULL DEFAULT 0,
        IsUsed          BIT                             NOT NULL DEFAULT 0,
        ExpiresAt       DATETIME2                       NOT NULL,
        CreatedAt       DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),
        RevokedAt       DATETIME2                       NULL,
        RevokedByIp     NVARCHAR(45)                    NULL,
        ReplacedByToken NVARCHAR(512)                   NULL,

        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_RefreshTokens_Token
        ON Security.RefreshTokens (Token) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId
        ON Security.RefreshTokens (UserId, IsRevoked, IsUsed) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: AuditLog
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE Security.AuditLog
    (
        Id              BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId          BIGINT                          NULL,
        ActionType      NVARCHAR(50)                    NOT NULL,  -- LOGIN, LOGOUT, CREATE, UPDATE, DELETE
        EntityType      NVARCHAR(100)                   NOT NULL,  -- User, Transaction, Account, etc.
        EntityId        NVARCHAR(256)                   NULL,
        OldValues       NVARCHAR(MAX)                   NULL,      -- JSON
        NewValues       NVARCHAR(MAX)                   NULL,      -- JSON
        IpAddress       NVARCHAR(45)                    NULL,
        UserAgent       NVARCHAR(512)                   NULL,
        CreatedAt       DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AuditLog PRIMARY KEY CLUSTERED (Id) ON FinOS_Data
    );

    CREATE NONCLUSTERED INDEX IX_AuditLog_UserId
        ON Security.AuditLog (UserId) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_AuditLog_ActionType
        ON Security.AuditLog (ActionType, EntityType) ON FinOS_Index;

    CREATE NONCLUSTERED INDEX IX_AuditLog_CreatedAt
        ON Security.AuditLog (CreatedAt) ON FinOS_Index;
END
GO

-- ---------------------------------------------------------------------------
-- Table: PasswordResetTokens
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE Security.PasswordResetTokens
    (
        Id          BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId      BIGINT                          NOT NULL,
        Token       NVARCHAR(256)                   NOT NULL,
        IsUsed      BIT                             NOT NULL DEFAULT 0,
        ExpiresAt   DATETIME2                       NOT NULL,
        CreatedAt   DATETIME2                       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY CLUSTERED (Id) ON FinOS_Data,
        CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES Security.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_PasswordResetTokens_Token
        ON Security.PasswordResetTokens (Token) ON FinOS_Index;
END
GO

PRINT 'Security schema created successfully.';
GO
