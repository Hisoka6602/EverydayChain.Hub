using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 修复已上线环境中缺失的看板快照表与运行时租约表。
    /// </summary>
    public partial class RepairDashboardSnapshotAndRuntimeLeaseTables : Migration
    {
        /// <summary>
        /// 执行迁移升级。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 步骤：使用幂等 SQL 补齐已记录迁移但实际缺表的生产库对象，避免破坏历史数据。
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[dashboard_scan_snapshots]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[dashboard_scan_snapshots]
                    (
                        [Id] BIGINT IDENTITY(1,1) NOT NULL,
                        [BucketStartLocal] DATETIME2 NOT NULL,
                        [TotalScanCount] INT NOT NULL,
                        [MatchedScanCount] INT NOT NULL,
                        CONSTRAINT [PK_dashboard_scan_snapshots] PRIMARY KEY CLUSTERED ([Id])
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_scan_snapshots_BucketStartLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_scan_snapshots]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_dashboard_scan_snapshots_BucketStartLocal]
                        ON [dbo].[dashboard_scan_snapshots] ([BucketStartLocal]);
                END;

                IF OBJECT_ID(N'[dbo].[dashboard_snapshot_states]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[dashboard_snapshot_states]
                    (
                        [Id] INT NOT NULL,
                        [CoverageStartLocal] DATETIME2 NULL,
                        [CoverageEndLocal] DATETIME2 NULL,
                        [LastRefreshTimeLocal] DATETIME2 NULL,
                        CONSTRAINT [PK_dashboard_snapshot_states] PRIMARY KEY CLUSTERED ([Id])
                    );
                END;

                IF OBJECT_ID(N'[dbo].[dashboard_task_snapshots]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[dashboard_task_snapshots]
                    (
                        [Id] BIGINT IDENTITY(1,1) NOT NULL,
                        [WaveCode] NVARCHAR(64) NOT NULL,
                        [WaveRemark] NVARCHAR(128) NULL,
                        [ResolvedDockCode] NVARCHAR(64) NOT NULL,
                        [WorkingArea] NVARCHAR(32) NULL,
                        [BucketStartLocal] DATETIME2 NOT NULL,
                        [SourceType] INT NOT NULL,
                        [Status] INT NOT NULL,
                        [TotalCount] INT NOT NULL,
                        [ScannedCount] INT NOT NULL,
                        [RecirculatedCount] INT NOT NULL,
                        [ExceptionCount] INT NOT NULL,
                        [RequiredFeedbackCount] INT NOT NULL,
                        [CompletedFeedbackCount] INT NOT NULL,
                        [TotalVolumeMm3] DECIMAL(18,3) NOT NULL,
                        [TotalWeightGram] DECIMAL(18,3) NOT NULL,
                        [EarliestCreatedTimeLocal] DATETIME2 NOT NULL,
                        [LatestUpdatedTimeLocal] DATETIME2 NOT NULL,
                        CONSTRAINT [PK_dashboard_task_snapshots] PRIMARY KEY CLUSTERED ([Id])
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_task_snapshots_BucketStartLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_task_snapshots]')
                )
                BEGIN
                    CREATE INDEX [IX_dashboard_task_snapshots_BucketStartLocal]
                        ON [dbo].[dashboard_task_snapshots] ([BucketStartLocal]);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_task_snapshots_BucketStartLocal_ResolvedDockCode'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_task_snapshots]')
                )
                BEGIN
                    CREATE INDEX [IX_dashboard_task_snapshots_BucketStartLocal_ResolvedDockCode]
                        ON [dbo].[dashboard_task_snapshots] ([BucketStartLocal], [ResolvedDockCode]);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_task_snapshots_BucketStartLocal_WaveCode'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_task_snapshots]')
                )
                BEGIN
                    CREATE INDEX [IX_dashboard_task_snapshots_BucketStartLocal_WaveCode]
                        ON [dbo].[dashboard_task_snapshots] ([BucketStartLocal], [WaveCode]);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_ResolvedDockCode'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_task_snapshots]')
                )
                BEGIN
                    CREATE INDEX [IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_ResolvedDockCode]
                        ON [dbo].[dashboard_task_snapshots] ([BucketStartLocal], [WaveCode], [ResolvedDockCode]);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_SourceType_Status'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_task_snapshots]')
                )
                BEGIN
                    CREATE INDEX [IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_SourceType_Status]
                        ON [dbo].[dashboard_task_snapshots] ([BucketStartLocal], [WaveCode], [SourceType], [Status]);
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_WorkingArea_SourceType_Status'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_task_snapshots]')
                )
                BEGIN
                    CREATE INDEX [IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_WorkingArea_SourceType_Status]
                        ON [dbo].[dashboard_task_snapshots] ([BucketStartLocal], [WaveCode], [WorkingArea], [SourceType], [Status]);
                END;

                IF OBJECT_ID(N'[dbo].[runtime_leases]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[runtime_leases]
                    (
                        [Id] NVARCHAR(128) NOT NULL,
                        [OwnerId] NVARCHAR(64) NOT NULL,
                        [AcquiredTimeLocal] DATETIME2 NOT NULL,
                        [ExpiresAtLocal] DATETIME2 NOT NULL,
                        CONSTRAINT [PK_runtime_leases] PRIMARY KEY CLUSTERED ([Id])
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_runtime_leases_ExpiresAtLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[runtime_leases]')
                )
                BEGIN
                    CREATE INDEX [IX_runtime_leases_ExpiresAtLocal]
                        ON [dbo].[runtime_leases] ([ExpiresAtLocal]);
                END;

                IF OBJECT_ID(N'[dbo].[dashboard_current_wave_snapshots]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[dashboard_current_wave_snapshots]
                    (
                        [Id] BIGINT IDENTITY(1,1) NOT NULL,
                        [BucketStartLocal] DATETIME2 NOT NULL,
                        [ScannedAtLocal] DATETIME2 NOT NULL,
                        [WaveCode] NVARCHAR(64) NOT NULL,
                        [WaveRemark] NVARCHAR(128) NULL,
                        [Barcode] NVARCHAR(128) NOT NULL,
                        CONSTRAINT [PK_dashboard_current_wave_snapshots] PRIMARY KEY CLUSTERED ([Id])
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_current_wave_snapshots_BucketStartLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_current_wave_snapshots]')
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_dashboard_current_wave_snapshots_BucketStartLocal]
                        ON [dbo].[dashboard_current_wave_snapshots] ([BucketStartLocal]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_dashboard_current_wave_snapshots_BucketStartLocal_ScannedAtLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[dashboard_current_wave_snapshots]')
                )
                BEGIN
                    CREATE INDEX [IX_dashboard_current_wave_snapshots_BucketStartLocal_ScannedAtLocal]
                        ON [dbo].[dashboard_current_wave_snapshots] ([BucketStartLocal], [ScannedAtLocal]);
                END;
                """);
        }

        /// <summary>
        /// 执行迁移回滚。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 步骤：该补偿迁移只修复缺失对象，回滚时不做删除，避免误伤已上线历史数据。
        }
    }
}
