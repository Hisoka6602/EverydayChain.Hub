using EverydayChain.Hub.Domain.Aggregates.SyncChangeLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 同步变更日志实体映射配置。
/// </summary>
public class SyncChangeLogEntityTypeConfiguration : IEntityTypeConfiguration<SyncChangeLogEntity>
{
    /// <summary>表名。</summary>
    private readonly string _tableName;

    /// <summary>Schema。</summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化同步变更日志实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">Schema。</param>
    public SyncChangeLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SyncChangeLogEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BatchId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ParentBatchId).HasMaxLength(64);
        builder.Property(x => x.TableCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.BusinessKey).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => new { x.TableCode, x.ChangedTimeLocal });
    }
}
