using EverydayChain.Hub.Domain.Aggregates.SyncChangeLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncChangeLogEntityTypeConfiguration : IEntityTypeConfiguration<SyncChangeLogEntity>
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _schema;

    public SyncChangeLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

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

