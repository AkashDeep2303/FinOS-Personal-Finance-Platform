-- ============================================================================
-- FinOS Database - Database Creation Script
-- Target: Microsoft SQL Server (SSMS)
-- Description: Creates the FinOS database using SQL Server's default data paths.
-- Filegroups are created by 001b_Filegroups.sql so this script works in both
-- Docker/Linux SQL Server and local Windows SQL Server installations.
-- ============================================================================

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FinOS')
BEGIN
    CREATE DATABASE FinOS;
END
GO

-- Set database options
ALTER DATABASE FinOS SET COMPATIBILITY_LEVEL = 160;
ALTER DATABASE FinOS SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE FinOS SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE FinOS SET QUERY_STORE = ON;
ALTER DATABASE FinOS SET QUERY_STORE (OPERATION_MODE = READ_WRITE);
GO

PRINT 'Database FinOS created successfully.';
GO
