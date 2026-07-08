using EverydayChain.Hub.Domain.Aggregates.SyncDeletionLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义 SyncDeletionLogEntityTypeConfiguration 类型。
/// </summary>
public class SyncDeletionLogEntityTypeConfiguration : IEntityTypeConfiguration<SyncDeletionLogEntity>
{
    /// <summary>
    /// 存储 _tableName 字段。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储 _schema 字段。
    /// </summary>
    private readonly string _schema;

    public SyncDeletionLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<SyncDeletionLogEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BatchId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ParentBatchId).HasMaxLength(64);
        builder.Property(x => x.TableCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.BusinessKey).IsRequired().HasMaxLength(256);
        builder.Property(x => x.SourceEvidence).IsRequired().HasMaxLength(1024);
        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => new { x.TableCode, x.DeletedTimeLocal });
    }
}

