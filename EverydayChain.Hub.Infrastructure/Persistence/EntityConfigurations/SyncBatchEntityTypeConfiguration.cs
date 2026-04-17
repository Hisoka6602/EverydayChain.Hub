using EverydayChain.Hub.Domain.Aggregates.SyncBatchAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 同步批次实体映射配置。
/// </summary>
public class SyncBatchEntityTypeConfiguration : IEntityTypeConfiguration<SyncBatchEntity>
{
    /// <summary>表名。</summary>
    private readonly string _tableName;

    /// <summary>Schema。</summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化同步批次实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">Schema。</param>
    public SyncBatchEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <inheritdoc />
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
