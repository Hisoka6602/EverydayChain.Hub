using Microsoft.EntityFrameworkCore.Migrations;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 为保留期自动清理增加审计表。
    /// </summary>
    [DbContext(typeof(HubDbContext))]
    [Migration("20260707150000_AddRetentionCleanupAuditLogs")]
    public partial class AddRetentionCleanupAuditLogs : Migration
    {
        /// <summary>
        /// 执行迁移升级。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 步骤：仅在目标表不存在时创建审计表与索引，避免上线环境重复迁移破坏已有数据。
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[retention_cleanup_audit_logs]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[retention_cleanup_audit_logs]
                    (
                        [Id] NVARCHAR(32) NOT NULL,
                        [BatchId] NVARCHAR(32) NOT NULL,
                        [TargetCode] NVARCHAR(64) NOT NULL,
                        [LogicalTableName] NVARCHAR(128) NOT NULL,
                        [RetentionMode] NVARCHAR(32) NOT NULL,
                        [TimeColumnName] NVARCHAR(64) NOT NULL,
                        [KeepMonths] INT NOT NULL,
                        [IsDryRun] BIT NOT NULL,
                        [AllowDelete] BIT NOT NULL,
                        [ThresholdTimeLocal] DATETIME2 NOT NULL,
                        [ExecutionStage] NVARCHAR(16) NOT NULL,
                        [ScannedCount] INT NOT NULL,
                        [CandidateCount] INT NOT NULL,
                        [DeletedCount] INT NOT NULL,
                        [Message] NVARCHAR(512) NOT NULL,
                        [InstanceId] NVARCHAR(128) NOT NULL,
                        [StartedTimeLocal] DATETIME2 NOT NULL,
                        [CompletedTimeLocal] DATETIME2 NULL,
                        CONSTRAINT [PK_retention_cleanup_audit_logs] PRIMARY KEY CLUSTERED ([Id])
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_retention_cleanup_audit_logs_StartedTimeLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[retention_cleanup_audit_logs]')
                )
                BEGIN
                    CREATE INDEX [IX_retention_cleanup_audit_logs_StartedTimeLocal]
                        ON [dbo].[retention_cleanup_audit_logs] ([StartedTimeLocal]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_retention_cleanup_audit_logs_ExecutionStage'
                      AND [object_id] = OBJECT_ID(N'[dbo].[retention_cleanup_audit_logs]')
                )
                BEGIN
                    CREATE INDEX [IX_retention_cleanup_audit_logs_ExecutionStage]
                        ON [dbo].[retention_cleanup_audit_logs] ([ExecutionStage]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_retention_cleanup_audit_logs_BatchId_StartedTimeLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[retention_cleanup_audit_logs]')
                )
                BEGIN
                    CREATE INDEX [IX_retention_cleanup_audit_logs_BatchId_StartedTimeLocal]
                        ON [dbo].[retention_cleanup_audit_logs] ([BatchId], [StartedTimeLocal]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_retention_cleanup_audit_logs_LogicalTableName_StartedTimeLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[retention_cleanup_audit_logs]')
                )
                BEGIN
                    CREATE INDEX [IX_retention_cleanup_audit_logs_LogicalTableName_StartedTimeLocal]
                        ON [dbo].[retention_cleanup_audit_logs] ([LogicalTableName], [StartedTimeLocal]);
                END;
                """);
        }

        /// <summary>
        /// 执行迁移回滚。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 步骤：自动清理审计表属于运维留痕数据，回滚时不自动删除，避免误伤历史审计信息。
        }
    }
}
