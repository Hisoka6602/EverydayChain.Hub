using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaskClosureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightMm",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsException",
                schema: "dbo",
                table: "business_tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeedbackReported",
                schema: "dbo",
                table: "business_tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthMm",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScanCount",
                schema: "dbo",
                table: "business_tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                schema: "dbo",
                table: "business_tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeMm3",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaveRemark",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightGram",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthMm",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_IsException",
                schema: "dbo",
                table: "business_tasks",
                column: "IsException");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_SourceType",
                schema: "dbo",
                table: "business_tasks",
                column: "SourceType");

            migrationBuilder.Sql(
                """
                DECLARE @schema sysname = N'dbo';
                DECLARE @tableName sysname;
                DECLARE @tableIdentifier nvarchar(260);
                DECLARE @qualifiedTable nvarchar(260);
                DECLARE @isExceptionIndexName sysname;
                DECLARE @sourceTypeIndexName sysname;
                DECLARE @sql nvarchar(max);

                DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT t.name
                FROM sys.tables AS t
                INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                WHERE s.name = @schema
                  AND t.name LIKE N'business\_tasks\_%' ESCAPE N'\';

                OPEN table_cursor;
                FETCH NEXT FROM table_cursor INTO @tableName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @tableIdentifier = @schema + N'.' + @tableName;
                    SET @qualifiedTable = QUOTENAME(@schema) + N'.' + QUOTENAME(@tableName);
                    SET @isExceptionIndexName = N'IX_' + @tableName + N'_IsException';
                    SET @sourceTypeIndexName = N'IX_' + @tableName + N'_SourceType';

                    IF COL_LENGTH(@tableIdentifier, N'FeedbackTimeLocal') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [FeedbackTimeLocal] datetime2 NULL;';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'HeightMm') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [HeightMm] decimal(18,3) NULL;';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'IsException') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [IsException] bit NOT NULL DEFAULT (0);';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'IsFeedbackReported') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [IsFeedbackReported] bit NOT NULL DEFAULT (0);';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'LengthMm') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [LengthMm] decimal(18,3) NULL;';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'ScanCount') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [ScanCount] int NOT NULL DEFAULT (0);';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'SourceType') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [SourceType] int NOT NULL DEFAULT (0);';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'VolumeMm3') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [VolumeMm3] decimal(18,3) NULL;';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'WaveRemark') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [WaveRemark] nvarchar(128) NULL;';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'WeightGram') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [WeightGram] decimal(18,3) NULL;';
                        EXEC (@sql);
                    END;

                    IF COL_LENGTH(@tableIdentifier, N'WidthMm') IS NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' ADD [WidthMm] decimal(18,3) NULL;';
                        EXEC (@sql);
                    END;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE object_id = OBJECT_ID(@tableIdentifier)
                          AND name = @isExceptionIndexName)
                    BEGIN
                        SET @sql = N'CREATE INDEX ' + QUOTENAME(@isExceptionIndexName) + N' ON ' + @qualifiedTable + N' ([IsException]);';
                        EXEC (@sql);
                    END;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE object_id = OBJECT_ID(@tableIdentifier)
                          AND name = @sourceTypeIndexName)
                    BEGIN
                        SET @sql = N'CREATE INDEX ' + QUOTENAME(@sourceTypeIndexName) + N' ON ' + @qualifiedTable + N' ([SourceType]);';
                        EXEC (@sql);
                    END;

                    FETCH NEXT FROM table_cursor INTO @tableName;
                END;

                CLOSE table_cursor;
                DEALLOCATE table_cursor;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_IsException",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_SourceType",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "FeedbackTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "HeightMm",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "IsException",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "IsFeedbackReported",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "LengthMm",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "ScanCount",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "SourceType",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "VolumeMm3",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "WaveRemark",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "WeightGram",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "WidthMm",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.Sql(
                """
                DECLARE @schema sysname = N'dbo';
                DECLARE @tableName sysname;
                DECLARE @tableIdentifier nvarchar(260);
                DECLARE @qualifiedTable nvarchar(260);
                DECLARE @isExceptionIndexName sysname;
                DECLARE @sourceTypeIndexName sysname;
                DECLARE @constraintName sysname;
                DECLARE @sql nvarchar(max);

                DECLARE table_cursor CURSOR LOCAL FAST_FORWARD FOR
                SELECT t.name
                FROM sys.tables AS t
                INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                WHERE s.name = @schema
                  AND t.name LIKE N'business\_tasks\_%' ESCAPE N'\';

                OPEN table_cursor;
                FETCH NEXT FROM table_cursor INTO @tableName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @tableIdentifier = @schema + N'.' + @tableName;
                    SET @qualifiedTable = QUOTENAME(@schema) + N'.' + QUOTENAME(@tableName);
                    SET @isExceptionIndexName = N'IX_' + @tableName + N'_IsException';
                    SET @sourceTypeIndexName = N'IX_' + @tableName + N'_SourceType';

                    IF EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE object_id = OBJECT_ID(@tableIdentifier)
                          AND name = @isExceptionIndexName)
                    BEGIN
                        SET @sql = N'DROP INDEX ' + QUOTENAME(@isExceptionIndexName) + N' ON ' + @qualifiedTable + N';';
                        EXEC (@sql);
                    END;

                    IF EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE object_id = OBJECT_ID(@tableIdentifier)
                          AND name = @sourceTypeIndexName)
                    BEGIN
                        SET @sql = N'DROP INDEX ' + QUOTENAME(@sourceTypeIndexName) + N' ON ' + @qualifiedTable + N';';
                        EXEC (@sql);
                    END;

                    DECLARE @columns TABLE ([Name] sysname);
                    INSERT INTO @columns ([Name])
                    VALUES
                        (N'IsException'),
                        (N'IsFeedbackReported'),
                        (N'ScanCount'),
                        (N'SourceType'),
                        (N'FeedbackTimeLocal'),
                        (N'HeightMm'),
                        (N'LengthMm'),
                        (N'VolumeMm3'),
                        (N'WaveRemark'),
                        (N'WeightGram'),
                        (N'WidthMm');

                    DECLARE @columnName sysname;
                    DECLARE column_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [Name] FROM @columns;

                    OPEN column_cursor;
                    FETCH NEXT FROM column_cursor INTO @columnName;
                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        IF COL_LENGTH(@tableIdentifier, @columnName) IS NOT NULL
                        BEGIN
                            SELECT TOP (1) @constraintName = dc.name
                            FROM sys.default_constraints AS dc
                            INNER JOIN sys.columns AS c
                                ON c.object_id = dc.parent_object_id
                               AND c.column_id = dc.parent_column_id
                            WHERE dc.parent_object_id = OBJECT_ID(@tableIdentifier)
                              AND c.name = @columnName;

                            IF @constraintName IS NOT NULL
                            BEGIN
                                SET @sql = N'ALTER TABLE ' + @qualifiedTable
                                    + N' DROP CONSTRAINT ' + QUOTENAME(@constraintName) + N';';
                                EXEC (@sql);
                                SET @constraintName = NULL;
                            END;

                            SET @sql = N'ALTER TABLE ' + @qualifiedTable + N' DROP COLUMN ' + QUOTENAME(@columnName) + N';';
                            EXEC (@sql);
                        END;

                        FETCH NEXT FROM column_cursor INTO @columnName;
                    END;

                    CLOSE column_cursor;
                    DEALLOCATE column_cursor;

                    FETCH NEXT FROM table_cursor INTO @tableName;
                END;

                CLOSE table_cursor;
                DEALLOCATE table_cursor;
                """);
        }
    }
}
