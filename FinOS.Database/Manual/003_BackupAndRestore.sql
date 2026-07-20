-- ============================================================================
-- FinOS Database - Manual Script: Backup and Restore Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Full, differential, and transaction log backup scripts with
--              point-in-time restore and backup integrity verification
-- IMPORTANT:   Adjust @BackupDirectory and database names to your environment.
--              Run backup scripts as sysadmin or backup operator.
-- ============================================================================

USE master;
GO

-- ============================================================================
-- CONFIGURATION
-- ============================================================================
-- Set your backup directory here (use raw path or UNC for network storage)
DECLARE @BackupDirectory NVARCHAR(512) = N'D:\SQLBackup\FinOS';
DECLARE @DatabaseName    NVARCHAR(128) = N'FinOS';

PRINT N'=================================================================';
PRINT N'  FinOS Backup & Restore Scripts';
PRINT N'  Database:   ' + @DatabaseName;
PRINT N'  Backup Dir: ' + @BackupDirectory;
PRINT N'=================================================================';
GO

-- ============================================================================
-- 1. FULL BACKUP
-- ============================================================================
-- Creates a full database backup with timestamp-based file naming.
-- Should be run daily (e.g., at midnight) or weekly depending on RPO.

PRINT N'';
PRINT N'>>> Full Backup Script';
PRINT N'    Copy and execute the script below:';
PRINT N'';

/*
-- ============================================================================
-- FULL BACKUP SCRIPT (Copy and Execute)
-- ============================================================================
DECLARE @BackupDirectory NVARCHAR(512) = N'D:\SQLBackup\FinOS';
DECLARE @DatabaseName    NVARCHAR(128) = N'FinOS';
DECLARE @Timestamp       NVARCHAR(20)  = FORMAT(SYSUTCDATETIME(), N'yyyyMMdd_HHmmss');
DECLARE @BackupFile     NVARCHAR(1024) = @BackupDirectory + N'\' + @DatabaseName + N'_Full_' + @Timestamp + N'.bak';

-- Ensure backup directory exists (requires xp_cmdshell)
-- EXEC xp_cmdshell N'mkdir "D:\SQLBackup\FinOS"', no_output;

BACKUP DATABASE FinOS
TO DISK = @BackupFile
WITH
    FORMAT,                    -- Overwrite existing media set
    INIT,                      -- Overwrite existing backup set
    NAME = N'FinOS-Full Backup',
    COMPRESSION,               -- Enable backup compression
    STATS = 10,                -- Show progress every 10%
    CHECKSUM,                  -- Enable backup checksums
    DESCRIPTION = N'FinOS Full Backup - ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);

-- Log the backup
DECLARE @BackupSizeMB DECIMAL(18,2);
SELECT @BackupSizeMB = backup_size / 1048576.0
FROM msdb.dbo.backupset
WHERE database_name = N'FinOS'
ORDER BY backup_finish_date DESC;

PRINT N'Full backup completed successfully.';
PRINT N'  File: ' + @BackupFile;
PRINT N'  Size: ' + CAST(@BackupSizeMB AS NVARCHAR(20)) + N' MB';
*/
GO

-- ============================================================================
-- 2. DIFFERENTIAL BACKUP
-- ============================================================================
-- Captures only changes since the last full backup.
-- Smaller and faster than full backup. Run more frequently (e.g., every 4-6 hours).

PRINT N'';
PRINT N'>>> Differential Backup Script';
PRINT N'    Copy and execute the script below:';
PRINT N'';

/*
-- ============================================================================
-- DIFFERENTIAL BACKUP SCRIPT (Copy and Execute)
-- ============================================================================
DECLARE @BackupDirectory NVARCHAR(512) = N'D:\SQLBackup\FinOS';
DECLARE @DatabaseName    NVARCHAR(128) = N'FinOS';
DECLARE @Timestamp       NVARCHAR(20)  = FORMAT(SYSUTCDATETIME(), N'yyyyMMdd_HHmmss');
DECLARE @BackupFile     NVARCHAR(1024) = @BackupDirectory + N'\' + @DatabaseName + N'_Diff_' + @Timestamp + N'.bak';

BACKUP DATABASE FinOS
TO DISK = @BackupFile
WITH
    DIFFERENTIAL,              -- Differential backup
    FORMAT,
    INIT,
    NAME = N'FinOS-Differential Backup',
    COMPRESSION,
    STATS = 10,
    CHECKSUM,
    DESCRIPTION = N'FinOS Differential Backup - ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);

PRINT N'Differential backup completed successfully.';
PRINT N'  File: ' + @BackupFile;
*/
GO

-- ============================================================================
-- 3. TRANSACTION LOG BACKUP
-- ============================================================================
-- Backs up the transaction log. Required for point-in-time recovery.
-- Run every 15-30 minutes for low RPO requirements.
-- NOTE: Database must be in FULL recovery model.

PRINT N'';
PRINT N'>>> Transaction Log Backup Script';
PRINT N'    Copy and execute the script below:';
PRINT N'';

/*
-- ============================================================================
-- TRANSACTION LOG BACKUP SCRIPT (Copy and Execute)
-- ============================================================================
-- Ensure database is in FULL recovery model
ALTER DATABASE FinOS SET RECOVERY FULL;

DECLARE @BackupDirectory NVARCHAR(512) = N'D:\SQLBackup\FinOS';
DECLARE @DatabaseName    NVARCHAR(128) = N'FinOS';
DECLARE @Timestamp       NVARCHAR(20)  = FORMAT(SYSUTCDATETIME(), N'yyyyMMdd_HHmmss');
DECLARE @BackupFile     NVARCHAR(1024) = @BackupDirectory + N'\' + @DatabaseName + N'_Log_' + @Timestamp + N'.trn';

BACKUP LOG FinOS
TO DISK = @BackupFile
WITH
    FORMAT,
    INIT,
    NAME = N'FinOS-Log Backup',
    COMPRESSION,
    STATS = 10,
    CHECKSUM,
    DESCRIPTION = N'FinOS Transaction Log Backup - ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);

PRINT N'Transaction log backup completed successfully.';
PRINT N'  File: ' + @BackupFile;
*/
GO

-- ============================================================================
-- 4. POINT-IN-TIME RESTORE
-- ============================================================================
-- Restores the database to a specific point in time using full backup,
-- differential backup, and transaction log chain.
-- WARNING: This will overwrite the existing database!

PRINT N'';
PRINT N'>>> Point-in-Time Restore Script';
PRINT N'    Copy and execute the script below:';
PRINT N'';

/*
-- ============================================================================
-- POINT-IN-TIME RESTORE SCRIPT (Copy and Execute)
-- ============================================================================
-- IMPORTANT: Replace the file paths and STOPAT time with actual values!
-- The restore order is: Full -> Differential (optional) -> Log chain -> STOPAT

-- Step 1: Kill all connections to the database
DECLARE @KillSQL NVARCHAR(MAX) = N'';
SELECT @KillSQL = @KillSQL + N'KILL ' + CAST(session_id AS NVARCHAR(10)) + N';'
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID(N'FinOS');

IF LEN(@KillSQL) > 0
    EXEC sp_executesql @KillSQL;

-- Step 2: Restore full backup (NORECOVERY to allow further restores)
RESTORE DATABASE FinOS
FROM DISK = N'D:\SQLBackup\FinOS\FinOS_Full_20260304_000000.bak'
WITH
    NORECOVERY,                -- Leave in restoring state for further restores
    REPLACE,                   -- Replace existing database
    MOVE N'FinOS' TO N'D:\SQLData\FinOS.mdf',
    MOVE N'FinOS_Log' TO N'D:\SQLLog\FinOS_Log.ldf',
    STATS = 10;

-- Step 3: Restore differential backup (NORECOVERY)
RESTORE DATABASE FinOS
FROM DISK = N'D:\SQLBackup\FinOS\FinOS_Diff_20260304_120000.bak'
WITH
    NORECOVERY,
    STATS = 10;

-- Step 4: Restore transaction logs (NORECOVERY for all but last)
RESTORE LOG FinOS
FROM DISK = N'D:\SQLBackup\FinOS\FinOS_Log_20260304_123000.trn'
WITH NORECOVERY;

RESTORE LOG FinOS
FROM DISK = N'D:\SQLBackup\FinOS\FinOS_Log_20260304_130000.trn'
WITH NORECOVERY;

-- Step 5: Restore final log with STOPAT (RECOVERY)
RESTORE LOG FinOS
FROM DISK = N'D:\SQLBackup\FinOS\FinOS_Log_20260304_133000.trn'
WITH
    RECOVERY,                  -- Bring database online
    STOPAT = N'2026-03-04T13:15:00',  -- Target point in time (UTC)
    STATS = 10;

PRINT N'Point-in-time restore completed successfully.';
PRINT N'  Restored to: 2026-03-04 13:15:00 UTC';
*/
GO

-- ============================================================================
-- 5. VERIFY BACKUP INTEGRITY
-- ============================================================================
-- Verifies that a backup file is readable and not corrupted.
-- ALWAYS verify backups before relying on them for restore.

PRINT N'';
PRINT N'>>> Backup Verification Script';
PRINT N'    Copy and execute the script below:';
PRINT N'';

/*
-- ============================================================================
-- BACKUP VERIFICATION SCRIPT (Copy and Execute)
-- ============================================================================
-- Verify a specific backup file
RESTORE VERIFYONLY
FROM DISK = N'D:\SQLBackup\FinOS\FinOS_Full_20260304_000000.bak'
WITH
    CHECKSUM,                  -- Verify backup checksums
    STATS = 10;

PRINT N'Backup verification completed successfully.';

-- ============================================================================
-- Verify ALL recent backups for the FinOS database
-- ============================================================================
SELECT
    bs.database_name,
    bs.type AS BackupType,
    CASE bs.type
        WHEN N'D' THEN N'Full'
        WHEN N'I' THEN N'Differential'
        WHEN N'L' THEN N'Transaction Log'
        ELSE N'Unknown'
    END AS BackupTypeDesc,
    bs.backup_start_date,
    bs.backup_finish_date,
    DATEDIFF(SECOND, bs.backup_start_date, bs.backup_finish_date) AS DurationSeconds,
    CAST(bs.backup_size / 1048576.0 AS DECIMAL(18,2)) AS SizeMB,
    CAST(bs.compressed_backup_size / 1048576.0 AS DECIMAL(18,2)) AS CompressedSizeMB,
    bm.physical_device_name AS BackupFile,
    bs.has_backup_checksums,
    bs.is_password_protected,
    bs.recovery_model
FROM msdb.dbo.backupset bs
INNER JOIN msdb.dbo.backupmediafamily bm ON bs.media_set_id = bm.media_set_id
WHERE bs.database_name = N'FinOS'
ORDER BY bs.backup_start_date DESC;
*/
GO

-- ============================================================================
-- 6. FULL RESTORE (Latest Full Backup Only)
-- ============================================================================
-- Simple restore from the latest full backup (no point-in-time).

PRINT N'';
PRINT N'>>> Full Restore Script (Latest Backup)';
PRINT N'    Copy and execute the script below:';
PRINT N'';

/*
-- ============================================================================
-- FULL RESTORE SCRIPT (Copy and Execute)
-- ============================================================================
-- Kill connections
DECLARE @KillSQL NVARCHAR(MAX) = N'';
SELECT @KillSQL = @KillSQL + N'KILL ' + CAST(session_id AS NVARCHAR(10)) + N';'
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID(N'FinOS');
IF LEN(@KillSQL) > 0 EXEC sp_executesql @KillSQL;

-- Restore from full backup
RESTORE DATABASE FinOS
FROM DISK = N'D:\SQLBackup\FinOS\FinOS_Full_20260304_000000.bak'
WITH
    RECOVERY,                  -- Bring database online immediately
    REPLACE,
    MOVE N'FinOS' TO N'D:\SQLData\FinOS.mdf',
    MOVE N'FinOS_Log' TO N'D:\SQLLog\FinOS_Log.ldf',
    STATS = 10;

-- Verify database is online
SELECT
    name,
    state_desc,
    recovery_model_desc
FROM sys.databases
WHERE name = N'FinOS';

PRINT N'Full restore completed successfully.';
*/
GO

-- ============================================================================
-- 7. AUTOMATED BACKUP VERIFICATION (Run as Agent Job)
-- ============================================================================
-- Checks all FinOS backups from the last 24 hours for integrity.

PRINT N'';
PRINT N'>>> Automated Backup Health Check';
PRINT N'    Copy and execute the script below:';
PRINT N'';

/*
-- Check if backups exist for the last 24 hours
DECLARE @LastFullBackup DATETIME2;
DECLARE @LastDiffBackup DATETIME2;
DECLARE @LastLogBackup  DATETIME2;

SELECT @LastFullBackup = MAX(backup_finish_date)
FROM msdb.dbo.backupset
WHERE database_name = N'FinOS' AND type = N'D';

SELECT @LastDiffBackup = MAX(backup_finish_date)
FROM msdb.dbo.backupset
WHERE database_name = N'FinOS' AND type = N'I';

SELECT @LastLogBackup = MAX(backup_finish_date)
FROM msdb.dbo.backupset
WHERE database_name = N'FinOS' AND type = N'L';

PRINT N'Last Full Backup:         ' + ISNULL(CONVERT(NVARCHAR(30), @LastFullBackup, 120), N'NONE');
PRINT N'Last Differential Backup: ' + ISNULL(CONVERT(NVARCHAR(30), @LastDiffBackup, 120), N'NONE');
PRINT N'Last Log Backup:          ' + ISNULL(CONVERT(NVARCHAR(30), @LastLogBackup, 120), N'NONE');

-- Alert if full backup is older than 24 hours
IF @LastFullBackup IS NULL OR DATEDIFF(HOUR, @LastFullBackup, SYSUTCDATETIME()) > 24
    PRINT N'WARNING: Full backup is missing or older than 24 hours!';

-- Alert if log backup is older than 30 minutes (for FULL recovery)
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'FinOS' AND recovery_model = 1)  -- 1 = FULL
BEGIN
    IF @LastLogBackup IS NULL OR DATEDIFF(MINUTE, @LastLogBackup, SYSUTCDATETIME()) > 30
        PRINT N'WARNING: Transaction log backup is missing or older than 30 minutes!';
END
*/
GO

PRINT N'';
PRINT N'=================================================================';
PRINT N'  Backup & Restore scripts generated.';
PRINT N'  Review, adjust paths, and uncomment sections as needed.';
PRINT N'=================================================================';
GO
