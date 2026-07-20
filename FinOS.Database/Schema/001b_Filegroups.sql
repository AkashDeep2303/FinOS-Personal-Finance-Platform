-- ============================================================================
-- FinOS Database - Filegroup Setup (Docker-friendly)
-- Creates the FinOS_Data and FinOS_Index filegroups that the schema scripts
-- (002 through 008) reference. Uses SQL Server's default data directory so
-- it works in both Docker containers and local Windows installs.
--
-- This script REPLACES the filegroup portion of 001_CreateDatabase.sql for
-- the Docker/local-dev scenario. The 001 script hardcodes C:\SQLData\* paths
-- which don't exist inside the Docker container or on a default Windows
-- SQL Server install.
-- ============================================================================

USE FinOS;
GO

-- Add FinOS_Data filegroup if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.filegroups WHERE name = N'FinOS_Data')
BEGIN
    ALTER DATABASE FinOS ADD FILEGROUP FinOS_Data;
    PRINT 'Created filegroup FinOS_Data';
END
GO

-- Add FinOS_Index filegroup if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.filegroups WHERE name = N'FinOS_Index')
BEGIN
    ALTER DATABASE FinOS ADD FILEGROUP FinOS_Index;
    PRINT 'Created filegroup FinOS_Index';
END
GO

-- Add a data file to FinOS_Data (use the same directory as the primary
-- data file - this works in Docker and on local Windows installs).
USE FinOS;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_files WHERE name = N'FinOS_Data1')
BEGIN
    DECLARE @dataPath NVARCHAR(512);
    SELECT @dataPath = REPLACE(physical_name, N'FinOS.mdf', N'FinOS_Data1.ndf')
      FROM sys.database_files
     WHERE name = N'FinOS' OR (file_id = 1);

    -- Fallback if the primary file isn't named FinOS.mdf (e.g. when the DB
    -- was created by setup-database.ps1 with default naming). Use the same
    -- directory and a generic filename.
    IF @dataPath IS NULL OR @dataPath = N''
    BEGIN
        SELECT TOP 1 @dataPath = physical_name FROM sys.database_files WHERE type = 0;
        -- Strip the filename portion and append our own
        DECLARE @lastSlash INT = LEN(@dataPath);
        WHILE @lastSlash > 0 AND SUBSTRING(@dataPath, @lastSlash, 1) <> N'\' AND SUBSTRING(@dataPath, @lastSlash, 1) <> N'/'
            SET @lastSlash = @lastSlash - 1;
        SET @dataPath = LEFT(@dataPath, @lastSlash) + N'FinOS_Data1.ndf';
    END

    DECLARE @sql NVARCHAR(MAX) = N'ALTER DATABASE FinOS ADD FILE (NAME = N''FinOS_Data1'', FILENAME = ''' + @dataPath + N''', SIZE = 256MB, FILEGROWTH = 128MB) TO FILEGROUP FinOS_Data;';
    EXEC sp_executesql @sql;
    PRINT 'Added file FinOS_Data1 to FinOS_Data filegroup at: ' + @dataPath;
END
GO

-- Add a data file to FinOS_Index
USE FinOS;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_files WHERE name = N'FinOS_Index1')
BEGIN
    DECLARE @indexPath NVARCHAR(512);
    SELECT @indexPath = REPLACE(physical_name, N'FinOS.mdf', N'FinOS_Index1.ndf')
      FROM sys.database_files
     WHERE name = N'FinOS' OR (file_id = 1);

    IF @indexPath IS NULL OR @indexPath = N''
    BEGIN
        SELECT TOP 1 @indexPath = physical_name FROM sys.database_files WHERE type = 0;
        DECLARE @lastSlash2 INT = LEN(@indexPath);
        WHILE @lastSlash2 > 0 AND SUBSTRING(@indexPath, @lastSlash2, 1) <> N'\' AND SUBSTRING(@indexPath, @lastSlash2, 1) <> N'/'
            SET @lastSlash2 = @lastSlash2 - 1;
        SET @indexPath = LEFT(@indexPath, @lastSlash2) + N'FinOS_Index1.ndf';
    END

    DECLARE @sql2 NVARCHAR(MAX) = N'ALTER DATABASE FinOS ADD FILE (NAME = N''FinOS_Index1'', FILENAME = ''' + @indexPath + N''', SIZE = 128MB, FILEGROWTH = 64MB) TO FILEGROUP FinOS_Index;';
    EXEC sp_executesql @sql2;
    PRINT 'Added file FinOS_Index1 to FinOS_Index filegroup at: ' + @indexPath;
END
GO

PRINT 'Filegroup setup complete.';
GO
