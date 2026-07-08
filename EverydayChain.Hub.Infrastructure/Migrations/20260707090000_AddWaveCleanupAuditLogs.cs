using Microsoft.EntityFrameworkCore.Migrations;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 为波次清理敏感操作增加审计表。
    /// </summary>
    [DbContext(typeof(HubDbContext))]
    [Migration("20260707090000_AddWaveCleanupAuditLogs")]
    public partial class AddWaveCleanupAuditLogs : Migration
    {
        /// <summary>
        /// 执行迁移升级。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 步骤：仅在目标表不存在时创建审计表及索引，避免重复迁移影响已上线环境。
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[wave_cleanup_audit_logs]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[wave_cleanup_audit_logs]
                    (
                        [Id] NVARCHAR(32) NOT NULL,
                        [WaveCode] NVARCHAR(64) NOT NULL,
                        [TargetStatus] NVARCHAR(32) NOT NULL,
                        [ExecutionStage] NVARCHAR(16) NOT NULL,
                        [IdentifiedCount] INT NOT NULL,
                        [CleanedCount] INT NOT NULL,
                        [Message] NVARCHAR(512) NOT NULL,
                        [RequestedTimeLocal] DATETIME2 NOT NULL,
                        [CompletedTimeLocal] DATETIME2 NULL,
                        [TraceId] NVARCHAR(128) NOT NULL,
                        [RequestPath] NVARCHAR(128) NOT NULL,
                        [HttpMethod] NVARCHAR(16) NOT NULL,
                        [OperatorId] NVARCHAR(64) NOT NULL,
                        [ClientIp] NVARCHAR(64) NOT NULL,
                        [UserAgent] NVARCHAR(256) NOT NULL,
                        CONSTRAINT [PK_wave_cleanup_audit_logs] PRIMARY KEY CLUSTERED ([Id])
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_wave_cleanup_audit_logs_RequestedTimeLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[wave_cleanup_audit_logs]')
                )
                BEGIN
                    CREATE INDEX [IX_wave_cleanup_audit_logs_RequestedTimeLocal]
                        ON [dbo].[wave_cleanup_audit_logs] ([RequestedTimeLocal]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_wave_cleanup_audit_logs_WaveCode_RequestedTimeLocal'
                      AND [object_id] = OBJECT_ID(N'[dbo].[wave_cleanup_audit_logs]')
                )
                BEGIN
                    CREATE INDEX [IX_wave_cleanup_audit_logs_WaveCode_RequestedTimeLocal]
                        ON [dbo].[wave_cleanup_audit_logs] ([WaveCode], [RequestedTimeLocal]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_wave_cleanup_audit_logs_ExecutionStage'
                      AND [object_id] = OBJECT_ID(N'[dbo].[wave_cleanup_audit_logs]')
                )
                BEGIN
                    CREATE INDEX [IX_wave_cleanup_audit_logs_ExecutionStage]
                        ON [dbo].[wave_cleanup_audit_logs] ([ExecutionStage]);
                END;
                """);
        }

        /// <summary>
        /// 执行迁移回滚。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 步骤：审计记录属于敏感操作留痕，回滚时不自动删除，避免误伤已生成的审计数据。
        }
    }
}
