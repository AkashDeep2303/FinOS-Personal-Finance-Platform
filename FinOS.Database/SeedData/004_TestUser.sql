-- ============================================================================
-- FinOS Database - Seed Test User
-- Inserts a known test user so the frontend login flow can be verified
-- end-to-end without requiring a separate registration step.
--
-- Test credentials:
--   Email:     test@finos.app
--   Password:  Test@123
--
-- The PasswordHash is a BCrypt hash (work factor 12) of "Test@123".
-- The PasswordHasher in FinOS.Identity.Application uses BCrypt.Net.BCrypt
-- with the same work factor, so this hash will verify successfully.
--
-- Idempotent: uses IF NOT EXISTS check on Email so re-running is safe.
-- ============================================================================

USE FinOS;
GO

IF NOT EXISTS (SELECT 1 FROM Security.Users WHERE Email = N'test@finos.app')
BEGIN
    INSERT INTO Security.Users
        (Email, PasswordHash, PasswordSalt, FirstName, LastName,
         PhoneNumber, ProfileImageUrl, IsActive, EmailVerified, PhoneVerified,
         TwoFactorEnabled, TwoFactorSecret, LockoutEnd, LockoutEnabled,
         AccessFailedCount, LastLoginAt, Currency, TimeZone, Locale,
         OAuthProvider, OAuthProviderId, CreatedAt, UpdatedAt)
    VALUES
        (N'test@finos.app',
         N'$2b$12$B7mNibATvAx/amEt9s5XhuH5DFPqJXHZ.fme3gyUfL9FOlMG29Wmq',
         N'',
         N'Test', N'User',
         N'+919999999999', NULL,
         1, 1, 0,
         0, NULL, NULL, 1,
         0, NULL,
         N'INR', N'Asia/Kolkata', N'en-IN',
         NULL, NULL,
         SYSUTCDATETIME(), SYSUTCDATETIME());

    PRINT 'Test user created: test@finos.app / Test@123';
END
ELSE
BEGIN
    -- Update the password hash in case it was changed (ensures the known
    -- test credentials always work after re-running this script).
    UPDATE Security.Users
       SET PasswordHash = N'$2b$12$B7mNibATvAx/amEt9s5XhuH5DFPqJXHZ.fme3gyUfL9FOlMG29Wmq',
           PasswordSalt = N'',
           IsActive = 1,
           EmailVerified = 1,
           LockoutEnd = NULL,
           AccessFailedCount = 0,
           UpdatedAt = SYSUTCDATETIME()
     WHERE Email = N'test@finos.app';

    PRINT 'Test user already exists - password reset to Test@123';
END
GO

-- Assign the test user the "User" role if it exists (role is seeded by 002_RolesAndPermissions.sql)
IF EXISTS (SELECT 1 FROM Security.Roles WHERE Name = N'User')
   AND NOT EXISTS (
       SELECT 1
         FROM Security.UserRoles ur
         JOIN Security.Users u ON u.Id = ur.UserId
        WHERE u.Email = N'test@finos.app'
   )
BEGIN
    INSERT INTO Security.UserRoles (UserId, RoleId)
    SELECT u.Id, r.Id
      FROM Security.Users u
      CROSS JOIN Security.Roles r
     WHERE u.Email = N'test@finos.app'
       AND r.Name = N'User';

    PRINT 'Test user assigned to User role';
END
GO

PRINT 'Test user seed complete.';
GO
