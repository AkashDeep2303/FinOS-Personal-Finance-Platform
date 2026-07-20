-- ============================================================================
-- FinOS Database - Manual Script: Index Maintenance Procedures
-- Target: Microsoft SQL Server (SSMS)
-- Description: Stored procedures for identifying fragmented indexes,
--              rebuild/reorganize based on fragmentation level, update
--              statistics, identify missing indexes, and unused indexes
-- ============================================================================

USE FinOS;
GO

-- ============================================================================
-- 1. SP: dbo.sp_IdentifyFragmentedIndexes
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_IdentifyFragmentedIndexes', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_IdentifyFragmentedIndexes;
GO

CREATE PROCEDURE dbo.sp_IdentifyFragmentedIndexes
    @MinFragmentationPct DECIMAL(5,2) = 10.0,    -- Show indexes >= this fragmentation
    @MinPageCount        INT           = 1000,     -- Only indexes with >= this many pages
    @DatabaseName        NVARCHAR(128) = N'FinOS'
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            s.name                                   AS SchemaName,
            t.name                                   AS TableName,
            i.name                                   AS IndexName,
            i.type_desc                              AS IndexType,
            ps.avg_fragmentation_in_percent          AS FragmentationPct,
            ps.page_count                            AS PageCount,
            CASE
                WHEN ps.avg_fragmentation_in_percent > 30 THEN N'Rebuild'
                WHEN ps.avg_fragmentation_in_percent > 10 THEN N'Reorganize'
                ELSE N'OK'
            END                                      AS RecommendedAction,
            CASE
                WHEN ps.avg_fragmentation_in_percent > 30 THEN N'ALTER INDEX [' + i.name + N'] ON [' + s.name + N'].[' + t.name + N'] REBUILD WITH (ONLINE = ON);'
                WHEN ps.avg_fragmentation_in_percent > 10 THEN N'ALTER INDEX [' + i.name + N'] ON [' + s.name + N'].[' + t.name + N'] REORGANIZE;'
                ELSE N'-- No action needed'
            END                                      AS SqlCommand
        FROM sys.dm_db_index_physical_stats(
                DB_ID(@DatabaseName), NULL, NULL, NULL, N'LIMITED') ps
        INNER JOIN sys.indexes i  ON ps.object_id = i.object_id AND ps.index_id = i.index_id
        INNER JOIN sys.tables t   ON i.object_id = t.object_id
        INNER JOIN sys.schemas s  ON t.schema_id = s.schema_id
        WHERE ps.avg_fragmentation_in_percent >= @MinFragmentationPct
          AND i.name IS NOT NULL                        -- Skip heaps
          AND ps.page_count >= @MinPageCount
          AND s.name IN (N'Security', N'Core', N'Budget', N'Investment', N'Loan', N'Goals', N'Analytics', N'AI', N'Notifications', N'Subscriptions', N'Import', N'dbo')
        ORDER BY ps.avg_fragmentation_in_percent DESC;
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

-- ============================================================================
-- 2. SP: dbo.sp_RebuildReorganizeIndexes
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_RebuildReorganizeIndexes', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RebuildReorganizeIndexes;
GO

CREATE PROCEDURE dbo.sp_RebuildReorganizeIndexes
    @RebuildThresholdPct  DECIMAL(5,2) = 30.0,   -- Rebuild if fragmentation >= this
    @ReorganizeThresholdPct DECIMAL(5,2) = 10.0,  -- Reorganize if fragmentation >= this
    @MinPageCount         INT           = 1000,    -- Only indexes with >= this many pages
    @OnlineRebuild        BIT           = 1,       -- Use ONLINE = ON for rebuilds
    @DatabaseName         NVARCHAR(128) = N'FinOS',
    @SchemaName           NVARCHAR(128) = NULL,    -- Filter by schema (NULL = all)
    @TableName            NVARCHAR(128) = NULL     -- Filter by table (NULL = all)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        CREATE TABLE #IndexMaintenance
        (
            Id              INT IDENTITY(1,1),
            SchemaName      NVARCHAR(128),
            TableName       NVARCHAR(128),
            IndexName       NVARCHAR(128),
            FragmentationPct DECIMAL(5,2),
            Action          NVARCHAR(20),   -- Rebuild or Reorganize
            SqlCommand      NVARCHAR(MAX)
        );

        -- Identify indexes that need attention
        INSERT INTO #IndexMaintenance (SchemaName, TableName, IndexName, FragmentationPct, Action, SqlCommand)
        SELECT
            s.name,
            t.name,
            i.name,
            ps.avg_fragmentation_in_percent,
            CASE
                WHEN ps.avg_fragmentation_in_percent >= @RebuildThresholdPct THEN N'Rebuild'
                WHEN ps.avg_fragmentation_in_percent >= @ReorganizeThresholdPct THEN N'Reorganize'
            END,
            CASE
                WHEN ps.avg_fragmentation_in_percent >= @RebuildThresholdPct THEN
                    N'ALTER INDEX [' + i.name + N'] ON [' + s.name + N'].[' + t.name + N'] REBUILD'
                    + CASE WHEN @OnlineRebuild = 1 THEN N' WITH (ONLINE = ON, SORT_IN_TEMPDB = ON)' ELSE N' WITH (SORT_IN_TEMPDB = ON)' END + N';'
                WHEN ps.avg_fragmentation_in_percent >= @ReorganizeThresholdPct THEN
                    N'ALTER INDEX [' + i.name + N'] ON [' + s.name + N'].[' + t.name + N'] REORGANIZE;'
            END
        FROM sys.dm_db_index_physical_stats(
                DB_ID(@DatabaseName), NULL, NULL, NULL, N'LIMITED') ps
        INNER JOIN sys.indexes i  ON ps.object_id = i.object_id AND ps.index_id = i.index_id
        INNER JOIN sys.tables t   ON i.object_id = t.object_id
        INNER JOIN sys.schemas s  ON t.schema_id = s.schema_id
        WHERE ps.avg_fragmentation_in_percent >= @ReorganizeThresholdPct
          AND i.name IS NOT NULL
          AND ps.page_count >= @MinPageCount
          AND (@SchemaName IS NULL OR s.name = @SchemaName)
          AND (@TableName IS NULL OR t.name = @TableName)
          AND s.name IN (N'Security', N'Core', N'Budget', N'Investment', N'Loan', N'Goals', N'Analytics', N'AI', N'Notifications', N'Subscriptions', N'Import', N'dbo')
        ORDER BY ps.avg_fragmentation_in_percent DESC;

        -- Process each index
        DECLARE @Id               INT;
        DECLARE @SQL              NVARCHAR(MAX);
        DECLARE @Schema           NVARCHAR(128);
        DECLARE @Table            NVARCHAR(128);
        DECLARE @Index            NVARCHAR(128);
        DECLARE @FragPct          DECIMAL(5,2);
        DECLARE @Action           NVARCHAR(20);
        DECLARE @ProcessedCount   INT = 0;
        DECLARE @FailedCount      INT = 0;

        DECLARE idx_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT Id, SqlCommand, SchemaName, TableName, IndexName, FragmentationPct, Action
            FROM #IndexMaintenance
            ORDER BY Id;

        OPEN idx_cursor;
        FETCH NEXT FROM idx_cursor INTO @Id, @SQL, @Schema, @Table, @Index, @FragPct, @Action;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            BEGIN TRY
                EXEC sp_executesql @SQL;
                SET @ProcessedCount = @ProcessedCount + 1;
                PRINT N'  [' + @Action + N'] [' + @Schema + N'].[' + @Table + N'].[' + @Index + N'] (' + CAST(@FragPct AS NVARCHAR(10)) + N'% fragmentation)';
            END TRY
            BEGIN CATCH
                SET @FailedCount = @FailedCount + 1;
                PRINT N'  [FAILED] [' + @Action + N'] [' + @Schema + N'].[' + @Table + N'].[' + @Index + N']: ' + ERROR_MESSAGE();

                -- If online rebuild failed, try offline
                IF @Action = N'Rebuild' AND @OnlineRebuild = 1
                BEGIN
                    BEGIN TRY
                        SET @SQL = N'ALTER INDEX [' + @Index + N'] ON [' + @Schema + N'].[' + @Table + N'] REBUILD WITH (SORT_IN_TEMPDB = ON);';
                        EXEC sp_executesql @SQL;
                        SET @ProcessedCount = @ProcessedCount + 1;
                        PRINT N'  [Rebuild-OFFLINE fallback] [' + @Schema + N'].[' + @Table + N'].[' + @Index + N']';
                    END TRY
                    BEGIN CATCH
                        PRINT N'  [FAILED offline too] [' + @Schema + N'].[' + @Table + N'].[' + @Index + N']: ' + ERROR_MESSAGE();
                    END CATCH
                END
            END CATCH

            FETCH NEXT FROM idx_cursor INTO @Id, @SQL, @Schema, @Table, @Index, @FragPct, @Action;
        END

        CLOSE idx_cursor;
        DEALLOCATE idx_cursor;

        -- Return summary
        SELECT
            @ProcessedCount       AS IndexesProcessed,
            @FailedCount          AS IndexesFailed,
            (SELECT COUNT(*) FROM #IndexMaintenance) AS TotalIdentified;

        DROP TABLE #IndexMaintenance;
    END TRY
    BEGIN CATCH
        IF OBJECT_ID(N'tempdb..#IndexMaintenance', N'U') IS NOT NULL
            DROP TABLE #IndexMaintenance;

        IF CURSOR_STATUS(N'local', N'idx_cursor') >= 0
        BEGIN
            CLOSE idx_cursor;
            DEALLOCATE idx_cursor;
        END

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 3. SP: dbo.sp_UpdateAllStatistics
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_UpdateAllStatistics', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateAllStatistics;
GO

CREATE PROCEDURE dbo.sp_UpdateAllStatistics
    @DatabaseName NVARCHAR(128) = N'FinOS',
    @FullScan     BIT           = 1,      -- 1 = FULLSCAN, 0 = sample default
    @SchemaName   NVARCHAR(128) = NULL,   -- Filter by schema
    @TableName    NVARCHAR(128) = NULL    -- Filter by table
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DECLARE @SQL           NVARCHAR(MAX);
        DECLARE @TablesUpdated INT = 0;

        DECLARE @Schema NVARCHAR(128);
        DECLARE @Table  NVARCHAR(128);

        DECLARE tbl_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT s.name, t.name
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name IN (N'Security', N'Core', N'Budget', N'Investment', N'Loan', N'Goals', N'Analytics', N'AI', N'Notifications', N'Subscriptions', N'Import', N'dbo')
              AND (@SchemaName IS NULL OR s.name = @SchemaName)
              AND (@TableName IS NULL OR t.name = @TableName)
            ORDER BY s.name, t.name;

        OPEN tbl_cursor;
        FETCH NEXT FROM tbl_cursor INTO @Schema, @Table;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @FullScan = 1
                SET @SQL = N'UPDATE STATISTICS [' + @Schema + N'].[' + @Table + N'] WITH FULLSCAN;';
            ELSE
                SET @SQL = N'UPDATE STATISTICS [' + @Schema + N'].[' + @Table + N'];';

            BEGIN TRY
                EXEC sp_executesql @SQL;
                SET @TablesUpdated = @TablesUpdated + 1;
            END TRY
            BEGIN CATCH
                PRINT N'Warning: Failed to update stats for [' + @Schema + N'].[' + @Table + N']: ' + ERROR_MESSAGE();
            END CATCH

            FETCH NEXT FROM tbl_cursor INTO @Schema, @Table;
        END

        CLOSE tbl_cursor;
        DEALLOCATE tbl_cursor;

        SELECT @TablesUpdated AS TablesUpdated;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS(N'local', N'tbl_cursor') >= 0
        BEGIN
            CLOSE tbl_cursor;
            DEALLOCATE tbl_cursor;
        END

        DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT            = ERROR_SEVERITY();
        DECLARE @ErrorState    INT            = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN;
    END CATCH;
END;
GO

-- ============================================================================
-- 4. SP: dbo.sp_IdentifyMissingIndexes
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_IdentifyMissingIndexes', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_IdentifyMissingIndexes;
GO

CREATE PROCEDURE dbo.sp_IdentifyMissingIndexes
    @MinAvgTotalUserCost DECIMAL(18,2) = 5.0,    -- Minimum average total cost
    @MinAvgUserImpact    DECIMAL(18,2) = 50.0,    -- Minimum average impact %
    @TopCount            INT           = 25        -- Top N recommendations
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT TOP (@TopCount)
            ROUND(migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans), 2)
                                                         AS ImprovementScore,
            DB_NAME(mid.database_id)                     AS DatabaseName,
            SCHEMA_NAME(t.schema_id)                     AS SchemaName,
            OBJECT_NAME(mid.object_id)                   AS TableName,
            mid.equality_columns                         AS EqualityColumns,
            mid.inequality_columns                       AS InequalityColumns,
            mid.included_columns                          AS IncludedColumns,
            migs.user_seeks                               AS UserSeeks,
            migs.user_scans                               AS UserScans,
            migs.avg_total_user_cost                      AS AvgTotalUserCost,
            migs.avg_user_impact                          AS AvgUserImpactPct,
            N'CREATE NONCLUSTERED INDEX [IX_' + OBJECT_NAME(mid.object_id) + N'_'
                + REPLACE(ISNULL(REPLACE(mid.equality_columns, N', ', N'_'), N''), N'[', N'') + N'] ON ['
                + SCHEMA_NAME(t.schema_id) + N'].[' + OBJECT_NAME(mid.object_id) + N'] ('
                + ISNULL(mid.equality_columns, N'') + ISNULL(N', ' + mid.inequality_columns, N'')
                + N')' + ISNULL(N' INCLUDE (' + mid.included_columns + N')', N'') + N';'
                                                         AS CreateIndexStatement
        FROM sys.dm_db_missing_index_groups mig
        INNER JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
        INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
        INNER JOIN sys.tables t ON mid.object_id = t.object_id
        WHERE mid.database_id = DB_ID()
          AND SCHEMA_NAME(t.schema_id) IN (N'Security', N'Core', N'Budget', N'Investment', N'Loan', N'Goals', N'Analytics', N'AI', N'Notifications', N'Subscriptions', N'Import', N'dbo')
          AND migs.avg_total_user_cost >= @MinAvgTotalUserCost
          AND migs.avg_user_impact >= @MinAvgUserImpact
        ORDER BY ImprovementScore DESC;
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

-- ============================================================================
-- 5. SP: dbo.sp_IdentifyUnusedIndexes
-- ============================================================================
IF OBJECT_ID(N'dbo.sp_IdentifyUnusedIndexes', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_IdentifyUnusedIndexes;
GO

CREATE PROCEDURE dbo.sp_IdentifyUnusedIndexes
    @MinSizePages INT = 1000,           -- Only indexes with >= this many pages
    @DaysSinceLastUse INT = 30          -- Unused for at least N days
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        SELECT
            s.name                                AS SchemaName,
            t.name                                AS TableName,
            i.name                                AS IndexName,
            i.type_desc                           AS IndexType,
            i.is_primary_key                      AS IsPrimaryKey,
            i.is_unique_constraint                AS IsUniqueConstraint,
            ps.used_page_count                    AS UsedPages,
            CAST(ps.used_page_count * 8.0 / 1024 AS DECIMAL(18,2)) AS SizeMB,
            ius.user_seeks                        AS UserSeeks,
            ius.user_scans                        AS UserScans,
            ius.user_lookups                      AS UserLookups,
            ius.user_updates                      AS UserUpdates,
            ius.last_user_seek                    AS LastUserSeek,
            ius.last_user_scan                    AS LastUserScan,
            ius.last_user_lookup                  AS LastUserLookup,
            -- Recommendation
            CASE
                WHEN i.is_primary_key = 1 OR i.is_unique_constraint = 1 THEN N'Keep (constraint)'
                WHEN ius.user_seeks + ius.user_scans + ius.user_lookups = 0
                     AND ius.user_updates > 0
                     AND DATEDIFF(DAY, ISNULL(ius.last_user_seek, ISNULL(ius.last_user_scan, ius.last_user_lookup)), SYSUTCDATETIME()) >= @DaysSinceLastUse
                     OR (ius.user_seeks IS NULL AND ius.user_scans IS NULL)
                THEN N'Consider dropping'
                ELSE N'Review'
            END AS Recommendation,
            -- Drop command (DO NOT execute blindly!)
            CASE
                WHEN i.is_primary_key = 0 AND i.is_unique_constraint = 0
                THEN N'DROP INDEX [' + i.name + N'] ON [' + s.name + N'].[' + t.name + N'];'
                ELSE N'-- Cannot drop constraint index directly'
            END AS DropCommand
        FROM sys.indexes i
        INNER JOIN sys.tables t   ON i.object_id = t.object_id
        INNER JOIN sys.schemas s  ON t.schema_id = s.schema_id
        LEFT JOIN sys.dm_db_index_usage_stats ius ON i.object_id = ius.object_id AND i.index_id = ius.index_id AND ius.database_id = DB_ID()
        LEFT JOIN sys.dm_db_partition_stats ps ON i.object_id = ps.object_id AND i.index_id = ps.index_id
        WHERE i.name IS NOT NULL                           -- Skip heaps
          AND i.is_primary_key = 0                          -- Exclude PKs from drop consideration
          AND i.is_unique_constraint = 0                    -- Exclude unique constraints
          AND s.name IN (N'Security', N'Core', N'Budget', N'Investment', N'Loan', N'Goals', N'Analytics', N'AI', N'Notifications', N'Subscriptions', N'Import', N'dbo')
          AND ps.used_page_count >= @MinSizePages
          AND (ius.user_seeks IS NULL OR (ius.user_seeks + ius.user_scans + ius.user_lookups) < 5)
        ORDER BY ps.used_page_count DESC;
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

PRINT 'Index maintenance stored procedures created successfully.';
PRINT '  - dbo.sp_IdentifyFragmentedIndexes';
PRINT '  - dbo.sp_RebuildReorganizeIndexes';
PRINT '  - dbo.sp_UpdateAllStatistics';
PRINT '  - dbo.sp_IdentifyMissingIndexes';
PRINT '  - dbo.sp_IdentifyUnusedIndexes';
GO
