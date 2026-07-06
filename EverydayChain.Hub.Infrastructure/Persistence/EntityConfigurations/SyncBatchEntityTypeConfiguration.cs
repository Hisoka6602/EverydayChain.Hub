using EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncBatchEntityTypeConfiguration : IEntityTypeConfiguration<SyncBatchEntity>
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _schema;

    public SyncBatchEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<SyncBatchEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BatchId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ParentBatchId).HasMaxLength(64);
        builder.Property(x => x.TableCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1024);
        builder.HasIndex(x => x.BatchId).IsUnique();
        builder.HasIndex(x => new { x.TableCode, x.Status, x.CompletedTimeLocal });
        builder.HasIndex(x => new { x.Status, x.CompletedTimeLocal });
    }
}

