using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaskQueryOptimizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @SchemaName sysname;
                DECLARE @TableName sysname;
                DECLARE @QualifiedTable nvarchar(517);
                DECLARE @Sql nvarchar(max);

                DECLARE business_task_tables CURSOR LOCAL FAST_FORWARD FOR
                SELECT s.[name], t.[name]
                FROM sys.tables AS t
                INNER JOIN sys.schemas AS s ON s.[schema_id] = t.[schema_id]
                WHERE s.[name] = N'dbo'
                  AND (
                      t.[name] = N'business_tasks'
                      OR t.[name] LIKE N'business_tasks[_][0-9][0-9][0-9][0-9][0-9][0-9]'
                  );

                OPEN business_task_tables;
                FETCH NEXT FROM business_task_tables INTO @SchemaName, @TableName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @QualifiedTable = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);
                    SET @Sql = N'';

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_CreatedTimeLocal_WaveCode_TargetChuteCode_ActualChuteCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_CreatedTimeLocal_WaveCode_TargetChuteCode_ActualChuteCode] ON ' + @QualifiedTable + N';';
                    END;

                    IF COL_LENGTH(@QualifiedTable, N'NormalizedBarcode') IS NULL
                    BEGIN
                        SET @Sql += N'ALTER TABLE ' + @QualifiedTable + N' ADD [NormalizedBarcode] nvarchar(128) NULL;';
                    END;

                    IF COL_LENGTH(@QualifiedTable, N'NormalizedWaveCode') IS NULL
                    BEGIN
                        SET @Sql += N'ALTER TABLE ' + @QualifiedTable + N' ADD [NormalizedWaveCode] nvarchar(64) NULL;';
                    END;

                    IF COL_LENGTH(@QualifiedTable, N'ResolvedDockCode') IS NULL
                    BEGIN
                        SET @Sql += N'ALTER TABLE ' + @QualifiedTable + N' ADD [ResolvedDockCode] nvarchar(64) NOT NULL CONSTRAINT '
                            + QUOTENAME(N'DF_' + @TableName + N'_ResolvedDockCode') + N' DEFAULT N''未分配码头'';';
                    END;

                    SET @Sql += N'
                    UPDATE ' + @QualifiedTable + N'
                    SET [NormalizedBarcode] = CASE
                                                  WHEN [Barcode] IS NULL OR LTRIM(RTRIM([Barcode])) = N'''' THEN NULL
                                                  ELSE LTRIM(RTRIM([Barcode]))
                                              END,
                        [NormalizedWaveCode] = CASE
                                                   WHEN [WaveCode] IS NULL OR LTRIM(RTRIM([WaveCode])) = N'''' THEN NULL
                                                   ELSE LTRIM(RTRIM([WaveCode]))
                                               END,
                        [ResolvedDockCode] = CASE
                                                 WHEN [ActualChuteCode] IS NOT NULL AND LTRIM(RTRIM([ActualChuteCode])) <> N'''' THEN LTRIM(RTRIM([ActualChuteCode]))
                                                 WHEN [TargetChuteCode] IS NOT NULL AND LTRIM(RTRIM([TargetChuteCode])) <> N'''' THEN LTRIM(RTRIM([TargetChuteCode]))
                                                 ELSE N''未分配码头''
                                             END;';

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode_ResolvedDockCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode_ResolvedDockCode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_NormalizedBarcode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_NormalizedBarcode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_NormalizedWaveCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_NormalizedWaveCode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_NormalizedWaveCode_CreatedTimeLocal'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_NormalizedWaveCode_CreatedTimeLocal] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_ResolvedDockCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_ResolvedDockCode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_ResolvedDockCode_CreatedTimeLocal'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_ResolvedDockCode_CreatedTimeLocal] ON ' + @QualifiedTable + N';';
                    END;

                    SET @Sql += N'
                    CREATE INDEX [IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode_ResolvedDockCode]
                        ON ' + @QualifiedTable + N' ([CreatedTimeLocal], [NormalizedWaveCode], [ResolvedDockCode]);
                    CREATE INDEX [IX_business_tasks_NormalizedBarcode]
                        ON ' + @QualifiedTable + N' ([NormalizedBarcode]);
                    CREATE INDEX [IX_business_tasks_NormalizedWaveCode]
                        ON ' + @QualifiedTable + N' ([NormalizedWaveCode]);
                    CREATE INDEX [IX_business_tasks_NormalizedWaveCode_CreatedTimeLocal]
                        ON ' + @QualifiedTable + N' ([NormalizedWaveCode], [CreatedTimeLocal]);
                    CREATE INDEX [IX_business_tasks_ResolvedDockCode]
                        ON ' + @QualifiedTable + N' ([ResolvedDockCode]);
                    CREATE INDEX [IX_business_tasks_ResolvedDockCode_CreatedTimeLocal]
                        ON ' + @QualifiedTable + N' ([ResolvedDockCode], [CreatedTimeLocal]);';

                    EXEC sp_executesql @Sql;
                    FETCH NEXT FROM business_task_tables INTO @SchemaName, @TableName;
                END;

                CLOSE business_task_tables;
                DEALLOCATE business_task_tables;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @SchemaName sysname;
                DECLARE @TableName sysname;
                DECLARE @QualifiedTable nvarchar(517);
                DECLARE @Sql nvarchar(max);

                DECLARE business_task_tables CURSOR LOCAL FAST_FORWARD FOR
                SELECT s.[name], t.[name]
                FROM sys.tables AS t
                INNER JOIN sys.schemas AS s ON s.[schema_id] = t.[schema_id]
                WHERE s.[name] = N'dbo'
                  AND (
                      t.[name] = N'business_tasks'
                      OR t.[name] LIKE N'business_tasks[_][0-9][0-9][0-9][0-9][0-9][0-9]'
                  );

                OPEN business_task_tables;
                FETCH NEXT FROM business_task_tables INTO @SchemaName, @TableName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @QualifiedTable = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);
                    SET @Sql = N'';

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode_ResolvedDockCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode_ResolvedDockCode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_NormalizedBarcode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_NormalizedBarcode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_NormalizedWaveCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_NormalizedWaveCode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_NormalizedWaveCode_CreatedTimeLocal'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_NormalizedWaveCode_CreatedTimeLocal] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_ResolvedDockCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_ResolvedDockCode] ON ' + @QualifiedTable + N';';
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_ResolvedDockCode_CreatedTimeLocal'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'DROP INDEX [IX_business_tasks_ResolvedDockCode_CreatedTimeLocal] ON ' + @QualifiedTable + N';';
                    END;

                    IF COL_LENGTH(@QualifiedTable, N'NormalizedBarcode') IS NOT NULL
                    BEGIN
                        SET @Sql += N'ALTER TABLE ' + @QualifiedTable + N' DROP COLUMN [NormalizedBarcode];';
                    END;

                    IF COL_LENGTH(@QualifiedTable, N'NormalizedWaveCode') IS NOT NULL
                    BEGIN
                        SET @Sql += N'ALTER TABLE ' + @QualifiedTable + N' DROP COLUMN [NormalizedWaveCode];';
                    END;

                    IF COL_LENGTH(@QualifiedTable, N'ResolvedDockCode') IS NOT NULL
                    BEGIN
                        DECLARE @ResolvedDockDefaultConstraintName sysname;
                        SELECT @ResolvedDockDefaultConstraintName = dc.[name]
                        FROM sys.default_constraints AS dc
                        INNER JOIN sys.columns AS c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
                        WHERE dc.[parent_object_id] = OBJECT_ID(@QualifiedTable)
                          AND c.[name] = N'ResolvedDockCode';

                        IF @ResolvedDockDefaultConstraintName IS NOT NULL
                        BEGIN
                            SET @Sql += N'ALTER TABLE ' + @QualifiedTable + N' DROP CONSTRAINT ' + QUOTENAME(@ResolvedDockDefaultConstraintName) + N';';
                        END;

                        SET @Sql += N'ALTER TABLE ' + @QualifiedTable + N' DROP COLUMN [ResolvedDockCode];';
                    END;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_business_tasks_CreatedTimeLocal_WaveCode_TargetChuteCode_ActualChuteCode'
                          AND [object_id] = OBJECT_ID(@QualifiedTable)
                    )
                    BEGIN
                        SET @Sql += N'CREATE INDEX [IX_business_tasks_CreatedTimeLocal_WaveCode_TargetChuteCode_ActualChuteCode] ON '
                            + @QualifiedTable + N' ([CreatedTimeLocal], [WaveCode], [TargetChuteCode], [ActualChuteCode]);';
                    END;

                    EXEC sp_executesql @Sql;
                    FETCH NEXT FROM business_task_tables INTO @SchemaName, @TableName;
                END;

                CLOSE business_task_tables;
                DEALLOCATE business_task_tables;
                """);
        }
    }
}
