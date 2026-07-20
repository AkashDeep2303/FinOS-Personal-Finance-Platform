-- ============================================================================
-- FinOS Database - Seed Data: Roles & Permissions
-- Target: Microsoft SQL Server (SSMS)
-- Description: Inserts application roles into Security.Roles.
--              Roles: User, PremiumUser, Admin, SuperAdmin
-- ============================================================================

USE FinOS;
GO

PRINT N'Inserting Security.Roles...';

-- Insert roles idempotently using MERGE
MERGE INTO Security.Roles AS Target
USING (
    VALUES
        (N'User', N'Standard user with access to personal finance management features including expenses, budgets, investments, loans, and goals.'),
        (N'PremiumUser', N'Premium subscriber with all standard features plus AI assistant, advanced analytics, priority support, and unlimited data import.'),
        (N'Admin', N'Application administrator with access to user management, system configuration, and support tools. Cannot modify SuperAdmin settings.'),
        (N'SuperAdmin', N'Super administrator with unrestricted access to all system features, administrative functions, and security configurations.')
) AS Source(Name, Description)
ON Target.Name = Source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, Description, CreatedAt)
    VALUES (Source.Name, Source.Description, SYSUTCDATETIME());
GO

PRINT N'Roles seeded successfully.';
GO
