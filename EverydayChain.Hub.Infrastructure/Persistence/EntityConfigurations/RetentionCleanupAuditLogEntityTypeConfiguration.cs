using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 配置保留期清理审计记录实体的数据库映射。
/// </summary>
public sealed class RetentionCleanupAuditLogEntityTypeConfiguration : IEntityTypeConfiguration<RetentionCleanupAuditLogEntity>
{
    /// <summary>
    /// 存储表名。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储架构名。
    /// </summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化保留期清理审计实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">架构名。</param>
    public RetentionCleanupAuditLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <summary>
    /// 配置保留期清理审计记录实体映射。
    /// </summary>
    /// <param name="builder">实体构建器。</param>
    public void Configure(EntityTypeBuilder<RetentionCleanupAuditLogEntity> builder)
    {
        // 步骤：配置表结构、字段长度以及按批次、逻辑表和时间追溯所需索引。
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).HasMaxLength(32);
        builder.Property(x => x.BatchId).IsRequired().HasMaxLength(32);
        builder.Property(x => x.TargetCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.LogicalTableName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.RetentionMode).IsRequired().HasMaxLength(32);
        builder.Property(x => x.TimeColumnName).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ExecutionStage).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(512);
        builder.Property(x => x.InstanceId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.StartedTimeLocal).IsRequired();
        builder.Property(x => x.CompletedTimeLocal);
        builder.Property(x => x.ThresholdTimeLocal).IsRequired();
        builder.HasIndex(x => x.StartedTimeLocal);
        builder.HasIndex(x => x.ExecutionStage);
        builder.HasIndex(x => new { x.BatchId, x.StartedTimeLocal });
        builder.HasIndex(x => new { x.LogicalTableName, x.StartedTimeLocal });
    }
}
