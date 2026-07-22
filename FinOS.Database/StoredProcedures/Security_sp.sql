-- ============================================================================
-- FinOS Database - Security Stored Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for user management, authentication, and audit
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- SP: Security.sp_CreateUser
-- Description: Insert a new user with password hash, return the generated Id
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_CreateUser', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_CreateUser;
GO

CREATE PROCEDURE Security.sp_CreateUser
    @Email               NVARCHAR(256),
    @PasswordHash        NVARCHAR(512),
    @PasswordSalt        NVARCHAR(256),
    @FirstName           NVARCHAR(100),
    @LastName            NVARCHAR(100),
    @PhoneNumber         NVARCHAR(20)        = NULL,
    @ProfileImageUrl     NVARCHAR(512)       = NULL,
    @Currency            NVARCHAR(3)         = N'INR',
    @TimeZone            NVARCHAR(50)        = N'Asia/Kolkata',
    @Locale              NVARCHAR(10)        = N'en-IN',
    @OAuthProvider       NVARCHAR(50)        = NULL,
    @OAuthProviderId     NVARCHAR(256)       = NULL,
    @RoleId              INT                 = NULL,        -- Optional: assign role on creation
    @NewUserId           BIGINT              OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate required fields
        IF @Email IS NULL OR LTRIM(RTRIM(@Email)) = N''
        BEGIN
            RAISERROR('Email address is required.', 16, 1);
            RETURN;
        END

        IF @PasswordHash IS NULL OR LTRIM(RTRIM(@PasswordHash)) = N''
        BEGIN
            RAISERROR('Password hash is required.', 16, 1);
            RETURN;
        END

        IF @PasswordSalt IS NULL OR LTRIM(RTRIM(@PasswordSalt)) = N''
        BEGIN
            RAISERROR('Password salt is required.', 16, 1);
            RETURN;
        END

        -- Check for duplicate email
        IF EXISTS (SELECT 1 FROM Security.Users WHERE Email = @Email AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('A user with email ''%s'' already exists.', 16, 1, @Email);
            RETURN;
        END

        -- Check for duplicate OAuth provider ID if specified
        IF @OAuthProvider IS NOT NULL AND @OAuthProviderId IS NOT NULL
        BEGIN
            IF EXISTS (
                SELECT 1
                FROM Security.Users
                WHERE OAuthProvider = @OAuthProvider
                  AND OAuthProviderId = @OAuthProviderId
                  AND DeletedAt IS NULL
            )
            BEGIN
                RAISERROR('A user with this OAuth provider identity already exists.', 16, 1);
                RETURN;
            END
        END

        -- Insert the user
        INSERT INTO Security.Users
        (
            Email, PasswordHash, PasswordSalt, FirstName, LastName,
            PhoneNumber, ProfileImageUrl, Currency, TimeZone, Locale,
            OAuthProvider, OAuthProviderId
        )
        VALUES
        (
            @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName,
            @PhoneNumber, @ProfileImageUrl, @Currency, @TimeZone, @Locale,
            @OAuthProvider, @OAuthProviderId
        );

        SET @NewUserId = SCOPE_IDENTITY();

        -- Optionally assign a role
        IF @RoleId IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM Security.Roles WHERE Id = @RoleId)
            BEGIN
                RAISERROR('Specified RoleId %d does not exist.', 16, 1, @RoleId);
                RETURN;
            END

            INSERT INTO Security.UserRoles (UserId, RoleId)
            VALUES (@NewUserId, @RoleId);
        END

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (
            @NewUserId,
            N'CREATE',
            N'User',
            CAST(@NewUserId AS NVARCHAR(256)),
            (SELECT * FROM Security.Users WHERE Id = @NewUserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
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
-- ---------------------------------------------------------------------------
-- SP: Security.sp_UpdateUser
-- Description: Update user profile fields
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_UpdateUser', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_UpdateUser;
GO

CREATE PROCEDURE Security.sp_UpdateUser
    @UserId              BIGINT,
    @FirstName           NVARCHAR(100)       = NULL,
    @LastName            NVARCHAR(100)       = NULL,
    @PhoneNumber         NVARCHAR(20)        = NULL,
    @DateOfBirth         DATE                = NULL,
    @Bio                 NVARCHAR(2000)      = NULL,
    @ProfileImageUrl     NVARCHAR(512)       = NULL,
    @Currency            NVARCHAR(3)         = NULL,
    @TimeZone            NVARCHAR(50)        = NULL,
    @Locale              NVARCHAR(10)        = NULL,
    @TwoFactorEnabled    BIT                 = NULL,
    @TwoFactorSecret     NVARCHAR(256)       = NULL,
    @IsActive            BIT                 = NULL,
    @PhoneVerified       BIT                 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Id = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('User with Id %d does not exist or is deleted.', 16, 1, @UserId);
            RETURN;
        END

        DECLARE @OldValues NVARCHAR(MAX);
        SELECT @OldValues = (
            SELECT FirstName, LastName, PhoneNumber, DateOfBirth, Bio, ProfileImageUrl,
                   Currency, TimeZone, Locale, TwoFactorEnabled, IsActive, PhoneVerified
            FROM Security.Users
            WHERE Id = @UserId
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        UPDATE Security.Users
        SET
            FirstName        = ISNULL(@FirstName, FirstName),
            LastName         = ISNULL(@LastName, LastName),
            PhoneNumber      = @PhoneNumber,
            DateOfBirth      = @DateOfBirth,
            Bio              = @Bio,
            ProfileImageUrl  = @ProfileImageUrl,
            Currency         = ISNULL(@Currency, Currency),
            TimeZone         = ISNULL(@TimeZone, TimeZone),
            Locale           = ISNULL(@Locale, Locale),
            TwoFactorEnabled = ISNULL(@TwoFactorEnabled, TwoFactorEnabled),
            TwoFactorSecret  = @TwoFactorSecret,
            IsActive         = ISNULL(@IsActive, IsActive),
            PhoneVerified    = ISNULL(@PhoneVerified, PhoneVerified),
            UpdatedAt        = SYSUTCDATETIME()
        WHERE Id = @UserId
          AND DeletedAt IS NULL;

        DECLARE @NewValues NVARCHAR(MAX);
        SELECT @NewValues = (
            SELECT FirstName, LastName, PhoneNumber, DateOfBirth, Bio, ProfileImageUrl,
                   Currency, TimeZone, Locale, TwoFactorEnabled, IsActive, PhoneVerified
            FROM Security.Users
            WHERE Id = @UserId
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, OldValues, NewValues)
        VALUES (@UserId, N'UPDATE', N'User', CAST(@UserId AS NVARCHAR(256)), @OldValues, @NewValues);
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
-- SP: Security.sp_VerifyEmail
-- Description: Set EmailVerified = 1 for a user
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_VerifyEmail', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_VerifyEmail;
GO

CREATE PROCEDURE Security.sp_VerifyEmail
    @UserId  BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Id = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('User with Id %d does not exist or is deleted.', 16, 1, @UserId);
            RETURN;
        END

        DECLARE @AlreadyVerified BIT;
        SELECT @AlreadyVerified = EmailVerified FROM Security.Users WHERE Id = @UserId;

        IF @AlreadyVerified = 1
        BEGIN
            -- Idempotent - no error, just return
            RETURN;
        END

        UPDATE Security.Users
        SET EmailVerified = 1,
            UpdatedAt     = SYSUTCDATETIME()
        WHERE Id = @UserId
          AND DeletedAt IS NULL;

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (@UserId, N'UPDATE', N'User', CAST(@UserId AS NVARCHAR(256)), N'{"EmailVerified":true}');
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
-- SP: Security.sp_ChangePassword
-- Description: Validate old password hash, then update to new hash
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_ChangePassword', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_ChangePassword;
GO

CREATE PROCEDURE Security.sp_ChangePassword
    @UserId           BIGINT,
    @OldPasswordHash  NVARCHAR(512),
    @NewPasswordHash  NVARCHAR(512),
    @NewPasswordSalt  NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate user exists
        IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Id = @UserId AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('User with Id %d does not exist or is deleted.', 16, 1, @UserId);
            RETURN;
        END

        -- Validate current password
        DECLARE @CurrentPasswordHash NVARCHAR(512);
        SELECT @CurrentPasswordHash = PasswordHash
        FROM Security.Users
        WHERE Id = @UserId AND DeletedAt IS NULL;

        IF @CurrentPasswordHash <> @OldPasswordHash
        BEGIN
            RAISERROR('Current password is incorrect.', 16, 1);
            RETURN;
        END

        -- Validate new hash is different
        IF @OldPasswordHash = @NewPasswordHash
        BEGIN
            RAISERROR('New password must be different from the current password.', 16, 1);
            RETURN;
        END

        -- Update password
        UPDATE Security.Users
        SET PasswordHash = @NewPasswordHash,
            PasswordSalt = @NewPasswordSalt,
            UpdatedAt    = SYSUTCDATETIME()
        WHERE Id = @UserId
          AND DeletedAt IS NULL;

        -- Revoke all refresh tokens for security (force re-login)
        UPDATE Security.RefreshTokens
        SET IsRevoked  = 1,
            RevokedAt  = SYSUTCDATETIME()
        WHERE UserId   = @UserId
          AND IsRevoked = 0;

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, EntityId, NewValues)
        VALUES (@UserId, N'UPDATE', N'User', CAST(@UserId AS NVARCHAR(256)), N'{"PasswordChanged":true}');
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
-- SP: Security.sp_AddAuditLog
-- Description: Insert an audit log entry
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_AddAuditLog', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_AddAuditLog;
GO

CREATE PROCEDURE Security.sp_AddAuditLog
    @UserId      BIGINT          = NULL,
    @ActionType  NVARCHAR(50),
    @EntityType  NVARCHAR(100),
    @EntityId    NVARCHAR(256)   = NULL,
    @OldValues   NVARCHAR(MAX)   = NULL,
    @NewValues   NVARCHAR(MAX)   = NULL,
    @IpAddress   NVARCHAR(45)    = NULL,
    @UserAgent   NVARCHAR(512)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate ActionType
        IF @ActionType IS NULL OR LTRIM(RTRIM(@ActionType)) = N''
        BEGIN
            RAISERROR('ActionType is required for audit log.', 16, 1);
            RETURN;
        END

        -- Validate EntityType
        IF @EntityType IS NULL OR LTRIM(RTRIM(@EntityType)) = N''
        BEGIN
            RAISERROR('EntityType is required for audit log.', 16, 1);
            RETURN;
        END

        INSERT INTO Security.AuditLog
            (UserId, ActionType, EntityType, EntityId, OldValues, NewValues, IpAddress, UserAgent)
        VALUES
            (@UserId, @ActionType, @EntityType, @EntityId, @OldValues, @NewValues, @IpAddress, @UserAgent);
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
-- SP: Security.sp_CreateRefreshToken
-- Description: Insert a new refresh token for a user
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_CreateRefreshToken', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_CreateRefreshToken;
GO

CREATE PROCEDURE Security.sp_CreateRefreshToken
    @UserId              BIGINT,
    @Token               NVARCHAR(512),
    @JwtId               NVARCHAR(256),
    @ExpiresAt           DATETIME2,
    @IpAddress           NVARCHAR(45)        = NULL,
    @NewTokenId          BIGINT              OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate user exists and is active
        IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Id = @UserId AND IsActive = 1 AND DeletedAt IS NULL)
        BEGIN
            RAISERROR('User with Id %d does not exist or is inactive.', 16, 1, @UserId);
            RETURN;
        END

        -- Validate token uniqueness
        IF EXISTS (SELECT 1 FROM Security.RefreshTokens WHERE Token = @Token)
        BEGIN
            RAISERROR('Refresh token already exists.', 16, 1);
            RETURN;
        END

        -- Invalidate any previous token family for this user on the same device/IP
        -- (token rotation: mark old unused tokens as replaced)
        UPDATE Security.RefreshTokens
        SET ReplacedByToken = @Token
        WHERE UserId = @UserId
          AND IsRevoked = 0
          AND IsUsed = 0
          AND RevokedByIp = @IpAddress;

        -- Insert the new refresh token
        INSERT INTO Security.RefreshTokens (UserId, Token, JwtId, ExpiresAt, RevokedByIp)
        VALUES (@UserId, @Token, @JwtId, @ExpiresAt, @IpAddress);

        SET @NewTokenId = SCOPE_IDENTITY();

        -- Audit log
        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, IpAddress)
        VALUES (@UserId, N'CREATE', N'RefreshToken', @IpAddress);
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
-- SP: Security.sp_RevokeRefreshToken
-- Description: Revoke a specific refresh token by setting IsRevoked = 1
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_RevokeRefreshToken', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_RevokeRefreshToken;
GO

CREATE PROCEDURE Security.sp_RevokeRefreshToken
    @Token           NVARCHAR(512),
    @RevokedByIp     NVARCHAR(45)  = NULL,
    @ReplacedByToken NVARCHAR(512) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Check token exists
        IF NOT EXISTS (SELECT 1 FROM Security.RefreshTokens WHERE Token = @Token)
        BEGIN
            RAISERROR('Refresh token not found.', 16, 1);
            RETURN;
        END

        -- Check if already revoked
        DECLARE @AlreadyRevoked BIT;
        SELECT @AlreadyRevoked = IsRevoked FROM Security.RefreshTokens WHERE Token = @Token;

        IF @AlreadyRevoked = 1
        BEGIN
            -- Idempotent - no error
            RETURN;
        END

        -- Revoke the token
        UPDATE Security.RefreshTokens
        SET IsRevoked       = 1,
            RevokedAt       = SYSUTCDATETIME(),
            RevokedByIp     = @RevokedByIp,
            ReplacedByToken = @ReplacedByToken
        WHERE Token = @Token
          AND IsRevoked = 0;

        -- Audit log
        DECLARE @UserId BIGINT;
        SELECT @UserId = UserId FROM Security.RefreshTokens WHERE Token = @Token;

        INSERT INTO Security.AuditLog (UserId, ActionType, EntityType, IpAddress)
        VALUES (@UserId, N'REVOKE', N'RefreshToken', @RevokedByIp);
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
-- SP: Security.sp_CleanExpiredTokens
-- Description: Delete expired and revoked refresh tokens older than a threshold
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'Security.sp_CleanExpiredTokens', N'P') IS NOT NULL
    DROP PROCEDURE Security.sp_CleanExpiredTokens;
GO

CREATE PROCEDURE Security.sp_CleanExpiredTokens
    @OlderThanDays INT = 30   -- Delete tokens that expired or were revoked more than N days ago
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF @OlderThanDays < 1
        BEGIN
            RAISERROR('OlderThanDays must be at least 1.', 16, 1);
            RETURN;
        END

        DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@OlderThanDays, SYSUTCDATETIME());
        DECLARE @DeletedCount INT = 0;

        -- Delete tokens that are both expired/revoked AND older than the cutoff
        DELETE FROM Security.RefreshTokens
        WHERE (ExpiresAt < @CutoffDate OR (IsRevoked = 1 AND RevokedAt < @CutoffDate))
          AND IsUsed = 1;  -- Only clean up tokens that have been used or are no longer needed

        SET @DeletedCount = @@ROWCOUNT;

        -- Also clean up password reset tokens
        DELETE FROM Security.PasswordResetTokens
        WHERE (ExpiresAt < @CutoffDate)
           OR (IsUsed = 1 AND CreatedAt < @CutoffDate);

        -- Return cleanup stats
        SELECT
            @DeletedCount                              AS RefreshTokensDeleted,
            @@ROWCOUNT                                 AS PasswordResetTokensDeleted,
            @CutoffDate                                AS CutoffDate;
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

PRINT 'Security stored procedures created successfully.';
GO