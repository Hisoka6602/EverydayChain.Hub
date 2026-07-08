using EverydayChain.Hub.Domain.Aggregates.AuditLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 配置波次清理审计记录实体的数据库映射。
/// </summary>
public sealed class WaveCleanupAuditLogEntityTypeConfiguration : IEntityTypeConfiguration<WaveCleanupAuditLogEntity>
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
    /// 初始化波次清理审计记录实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">架构名。</param>
    public WaveCleanupAuditLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <summary>
    /// 配置波次清理审计记录实体映射。
    /// </summary>
    /// <param name="builder">实体构建器。</param>
    public void Configure(EntityTypeBuilder<WaveCleanupAuditLogEntity> builder)
    {
        // 步骤：配置表结构、字段长度和按波次及时间追溯所需索引。
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).HasMaxLength(32);
        builder.Property(x => x.WaveCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TargetStatus).IsRequired().HasMaxLength(32);
        builder.Property(x => x.ExecutionStage).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(512);
        builder.Property(x => x.RequestPath).IsRequired().HasMaxLength(128);
        builder.Property(x => x.HttpMethod).IsRequired().HasMaxLength(16);
        builder.Property(x => x.OperatorId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TraceId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ClientIp).IsRequired().HasMaxLength(64);
        builder.Property(x => x.UserAgent).IsRequired().HasMaxLength(256);
        builder.Property(x => x.RequestedTimeLocal).IsRequired();
        builder.Property(x => x.CompletedTimeLocal);
        builder.HasIndex(x => x.RequestedTimeLocal);
        builder.HasIndex(x => new { x.WaveCode, x.RequestedTimeLocal });
        builder.HasIndex(x => x.ExecutionStage);
    }
}
